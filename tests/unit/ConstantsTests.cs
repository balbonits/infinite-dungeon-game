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

    [Fact]
    public void GetMaxHp_AtLevel46340_NoOverflow()
    {
        // 46340² = 2,147,395,600 — last product that fits int32.
        int result = Constants.PlayerStats.GetMaxHp(46340);
        (result > 0).Should().BeTrue("MaxHp at level 46340 must remain positive (no overflow)");
    }

    [Fact]
    public void GetMaxHp_AtLevel46341_NoOverflow()
    {
        // 46341² = 2,147,488,281 — would overflow int32 multiplication.
        // The closed form must compute in long space to stay correct here.
        int result = Constants.PlayerStats.GetMaxHp(46341);
        (result > 0).Should().BeTrue("MaxHp at level 46341 must remain positive (no overflow)");
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
}
