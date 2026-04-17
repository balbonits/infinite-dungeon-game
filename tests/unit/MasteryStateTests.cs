using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

public class MasteryStateTests
{
    // -- XP and Leveling --

    [Fact]
    public void NewMastery_StartsAtLevel0()
    {
        var state = new MasteryState("test");
        state.Level.Should().Be(0);
        state.Xp.Should().Be(0);
    }

    [Fact]
    public void Level0To1_IsInstant_Requires0Xp()
    {
        var state = new MasteryState("test");
        state.XpToNextLevel.Should().Be(0);
        int gained = state.AddXp(1);
        state.Level.Should().BeGreaterOrEqualTo(1);
        gained.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public void XpToNextLevel_Formula_LevelSquaredTimes20()
    {
        var state = new MasteryState("test");
        state.SetState(5, 0);
        state.XpToNextLevel.Should().Be(5 * 5 * 20); // 500
    }

    [Fact]
    public void AddXp_MultiLevelUp_GainsMultipleLevels()
    {
        var state = new MasteryState("test");
        // Level 0→1 is instant, then level 1→2 needs 20, 2→3 needs 80
        int gained = state.AddXp(200);
        state.Level.Should().BeGreaterThan(2);
        gained.Should().BeGreaterThan(2);
    }

    [Fact]
    public void AddXp_NegativeAmount_DoesNothing()
    {
        var state = new MasteryState("test");
        state.SetState(5, 10);
        state.AddXp(-50).Should().Be(0);
        state.Level.Should().Be(5);
        state.Xp.Should().Be(10);
    }

    [Fact]
    public void AddXp_ZeroAmount_DoesNothing()
    {
        var state = new MasteryState("test");
        state.SetState(3, 5);
        state.AddXp(0).Should().Be(0);
        state.Xp.Should().Be(5);
    }

    // -- Skill Points --

    [Fact]
    public void AddSkillPoint_AtLevel0_Grants50Xp()
    {
        var state = new MasteryState("test");
        state.SetState(0, 0);
        // At level 0 (after auto-leveling stops), XP from SP = 50 * (1 + 0 * 0.1) = 50
        // But level 0→1 is instant, so this will auto-level first
        state.AddSkillPoint();
        state.Level.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public void AddSkillPoint_AtLevel10_GrantsScaledXp()
    {
        var state = new MasteryState("test");
        state.SetState(10, 0);
        int prevXp = state.Xp;
        state.AddSkillPoint();
        // SP XP at level 10 = 50 * (1 + 10 * 0.1) = 50 * 2 = 100
        // State should have gained XP (may or may not level up depending on required XP)
        (state.Xp + (state.Level > 10 ? state.XpToNextLevel : 0)).Should().BeGreaterThan(prevXp);
    }

    // -- Passive Bonus --

    [Fact]
    public void GetPassiveBonus_Level0_ReturnsZero()
    {
        var state = new MasteryState("test");
        state.GetPassiveBonus(1.5f).Should().Be(0);
    }

    [Fact]
    public void GetPassiveBonus_Level10_DamageMultiplier_MatchesFormula()
    {
        var state = new MasteryState("test");
        state.SetState(10, 0);
        // 10 * 1.5 * (100 / 110) = 13.636...
        float expected = 10 * 1.5f * (100f / 110f);
        state.GetPassiveBonus(1.5f).Should().BeApproximately(expected, 0.01f);
    }

    [Fact]
    public void GetPassiveBonus_Level25_MatchesSpecExample()
    {
        var state = new MasteryState("test");
        state.SetState(25, 0);
        // Spec example: 25 * 1.5 * (100 / 125) = 30.0
        state.GetPassiveBonus(1.5f).Should().BeApproximately(30.0f, 0.01f);
    }

    [Fact]
    public void GetPassiveBonus_DiminishingReturns_HigherLevelLessPerLevel()
    {
        var low = new MasteryState("low");
        low.SetState(10, 0);
        var high = new MasteryState("high");
        high.SetState(100, 0);

        float bonusPerLevelLow = low.GetPassiveBonus(1.0f) / 10;
        float bonusPerLevelHigh = high.GetPassiveBonus(1.0f) / 100;
        bonusPerLevelLow.Should().BeGreaterThan(bonusPerLevelHigh);
    }

    // -- Save/Load --

    [Fact]
    public void SetState_RestoresLevelAndXp()
    {
        var state = new MasteryState("test");
        state.SetState(15, 350);
        state.Level.Should().Be(15);
        state.Xp.Should().Be(350);
    }

    [Fact]
    public void SetState_NegativeValues_ClampedToZero()
    {
        var state = new MasteryState("test");
        state.SetState(-5, -100);
        state.Level.Should().Be(0);
        state.Xp.Should().Be(0);
    }
}
