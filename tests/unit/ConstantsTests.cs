using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

/// <summary>
/// Tests for <see cref="Constants.PlayerStats"/> closed-form helpers.
/// Specifically AUDIT-08: GetMaxHp was an O(level) loop that re-ran on
/// every StatsChanged signal. Replaced with a closed-form O(1). These
/// tests pin the output against the old loop across 0..200 so the
/// rewrite is provably identical.
/// </summary>
public class ConstantsTests
{
    /// <summary>
    /// The pre-AUDIT-08 loop implementation — kept here only as the
    /// reference oracle for regression. Do NOT call from production.
    /// </summary>
    private static int GetMaxHpReference(int level)
    {
        int total = Constants.PlayerStats.StartingHp;
        for (int l = 1; l <= level; l++)
            total += (int)(8 + l * 0.5f);
        return total;
    }

    [Fact]
    public void GetMaxHp_Level0_ReturnsStartingHp()
    {
        Constants.PlayerStats.GetMaxHp(0).Should().Be(Constants.PlayerStats.StartingHp);
    }

    [Fact]
    public void GetMaxHp_Level1_Adds8()
    {
        // floor(8 + 1*0.5) = 8. Closed form: 8*1 + 1/4 = 8.
        Constants.PlayerStats.GetMaxHp(1).Should().Be(Constants.PlayerStats.StartingHp + 8);
    }

    [Fact]
    public void GetMaxHp_Level5_MatchesLoopSum()
    {
        // Sum of floor(8 + l*0.5) for l=1..5 = 8+9+9+10+10 = 46.
        Constants.PlayerStats.GetMaxHp(5).Should().Be(Constants.PlayerStats.StartingHp + 46);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(25)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(199)]
    [InlineData(500)]
    [InlineData(1000)]
    public void GetMaxHp_MatchesLoopImplementation(int level)
    {
        int closedForm = Constants.PlayerStats.GetMaxHp(level);
        int loopForm = GetMaxHpReference(level);
        closedForm.Should().Be(loopForm,
            $"closed-form must match the pre-AUDIT-08 loop at level {level}");
    }

    [Fact]
    public void GetMaxHp_ExhaustiveMatchesLoop_0To200()
    {
        // Exhaustive: every level in 0..200 agrees with the reference loop.
        // Guards against the "off-by-one at even/odd boundary" mistake that's
        // typical when going loop → closed-form with integer truncation.
        for (int level = 0; level <= 200; level++)
        {
            int closedForm = Constants.PlayerStats.GetMaxHp(level);
            int loopForm = GetMaxHpReference(level);
            closedForm.Should().Be(loopForm, $"mismatch at level {level}");
        }
    }

    [Fact]
    public void GetMaxHp_IsMonotonicallyNonDecreasing()
    {
        // Leveling up must never decrease MaxHp.
        int prev = Constants.PlayerStats.GetMaxHp(0);
        for (int level = 1; level <= 200; level++)
        {
            int curr = Constants.PlayerStats.GetMaxHp(level);
            (curr >= prev).Should().BeTrue($"GetMaxHp({level}) = {curr} < GetMaxHp({level - 1}) = {prev}");
            prev = curr;
        }
    }

    // AUDIT-08 Copilot round 2: guard the int-overflow boundary.
    // level * level overflows int32 past √(int.MaxValue) ≈ 46340. The
    // closed-form computes level * (long)level to stay safe; these tests
    // pin that behavior well past the realistic player-level ceiling.

    // Copilot PR #41 round-2 asked for exact-value assertions at the
    // overflow boundary — a bare "> 0" check could pass with wrong
    // arithmetic that stays positive. Expected values computed in long
    // space so the assertion itself isn't subject to int32 overflow.
    private static int ExpectedMaxHpAt(int level)
    {
        long total = (long)Constants.PlayerStats.StartingHp + 8L * level + (long)level * level / 4L;
        if (total > int.MaxValue) return int.MaxValue;
        return (int)total;
    }

    [Fact]
    public void GetMaxHp_AtLevel46340_MatchesLongSpaceReference()
    {
        // 46340² = 2,147,395,600 — last product that fits int32.
        int expected = ExpectedMaxHpAt(46340);
        Constants.PlayerStats.GetMaxHp(46340).Should().Be(expected,
            "matches the int64-space reference at the last in-int32-range level");
    }

    [Fact]
    public void GetMaxHp_AtLevel46341_MatchesLongSpaceReference()
    {
        // 46341² = 2,147,488,281 — would overflow int32 multiplication.
        // The closed form must compute in long space to stay correct here.
        int expected = ExpectedMaxHpAt(46341);
        Constants.PlayerStats.GetMaxHp(46341).Should().Be(expected,
            "matches the int64-space reference just past the int32-mult overflow point");
    }

    [Fact]
    public void GetMaxHp_StaysMonotonic_Across46340Boundary()
    {
        // If the long-space math is wrong, adjacent values across the
        // overflow boundary would produce a non-monotonic jump.
        int hp46339 = Constants.PlayerStats.GetMaxHp(46339);
        int hp46340 = Constants.PlayerStats.GetMaxHp(46340);
        int hp46341 = Constants.PlayerStats.GetMaxHp(46341);
        (hp46340 >= hp46339).Should().BeTrue("no decrease at overflow-adjacent level 46340");
        (hp46341 >= hp46340).Should().BeTrue("no decrease at overflow-adjacent level 46341");
    }

    // Copilot PR #41 round-2 findings — negative levels + int saturation.

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void GetMaxHp_NegativeLevel_ReturnsStartingHp(int level)
    {
        // Preserves the pre-AUDIT-08 loop contract: the loop body didn't run
        // for level <= 0, so any corrupted/debug state fell through as
        // StartingHp. Without this guard the closed form goes negative.
        Constants.PlayerStats.GetMaxHp(level).Should().Be(Constants.PlayerStats.StartingHp);
    }

    [Fact]
    public void GetMaxHp_AtUnboundedLevel_SaturatesToIntMax()
    {
        // The leveling spec has no level cap. Past level ≈ 92 k the closed
        // form exceeds int.MaxValue even in long space; HP is stored as int,
        // so the implementation must saturate rather than wrap.
        int result = Constants.PlayerStats.GetMaxHp(int.MaxValue);
        result.Should().Be(int.MaxValue, "saturates to int.MaxValue for unbounded level");
    }

    // GetEffectiveMaxHp is the caller-safe wrapper: MaxHp + bonus in long
    // space, clamped to int range. Without it, callers that compute
    // GetMaxHp(level) + BonusMaxHp would wrap a saturated GetMaxHp to a
    // negative int once the bonus is positive. Copilot PR #41 round-2.

    [Fact]
    public void GetEffectiveMaxHp_NormalCase_JustAddsBonus()
    {
        int baseMax = Constants.PlayerStats.GetMaxHp(50);
        Constants.PlayerStats.GetEffectiveMaxHp(50, 200).Should().Be(baseMax + 200);
    }

    [Fact]
    public void GetEffectiveMaxHp_SaturatedBaseWithPositiveBonus_ClampsToIntMax()
    {
        // GetMaxHp(int.MaxValue) saturates to int.MaxValue. A naive
        // int addition with a positive bonus would wrap negative;
        // the helper must clamp instead.
        int effective = Constants.PlayerStats.GetEffectiveMaxHp(int.MaxValue, 1000);
        effective.Should().Be(int.MaxValue, "positive bonus on saturated base can't push past int.MaxValue");
    }

    [Fact]
    public void GetEffectiveMaxHp_NegativeBonusBelowBase_ClampsToZero()
    {
        // Defensive: a bonus that would take effective below zero clamps to 0
        // rather than going negative. The production callsite doesn't produce
        // this today (BonusMaxHp comes from Sta and is always >= 0), but
        // leaving the clamp documents the contract.
        Constants.PlayerStats.GetEffectiveMaxHp(level: 1, bonus: -9999).Should().Be(0);
    }
}
