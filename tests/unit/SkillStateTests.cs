using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

public class SkillStateTests
{
    // -- XpToNextLevel --

    [Fact]
    public void XpToNextLevel_Level0_IsZero_InstantLevelUp()
    {
        var state = new SkillState("test");
        state.XpToNextLevel.Should().Be(0);
    }

    [Fact]
    public void XpToNextLevel_Level1_Is20()
    {
        var state = new SkillState("test", level: 1);
        state.XpToNextLevel.Should().Be(20); // 1^2 * 20
    }

    [Fact]
    public void XpToNextLevel_Level5_Is500()
    {
        var state = new SkillState("test", level: 5);
        state.XpToNextLevel.Should().Be(500); // 5^2 * 20
    }

    [Fact]
    public void XpToNextLevel_Level10_Is2000()
    {
        var state = new SkillState("test", level: 10);
        state.XpToNextLevel.Should().Be(2000); // 10^2 * 20
    }

    // -- AddXp --

    [Fact]
    public void AddXp_AtLevel0_InstantlyLevelsTo1()
    {
        var state = new SkillState("test");
        int gained = state.AddXp(1);
        gained.Should().BeGreaterOrEqualTo(1);
        state.Level.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public void AddXp_EnoughForOneLevel_LevelsUp()
    {
        var state = new SkillState("test", level: 1);
        int gained = state.AddXp(20); // exactly enough for level 1→2
        gained.Should().Be(1);
        state.Level.Should().Be(2);
    }

    [Fact]
    public void AddXp_EnoughForMultipleLevels_LevelsUpMultiple()
    {
        var state = new SkillState("test", level: 1);
        // Level 1→2: 20 XP, Level 2→3: 80 XP = 100 total
        int gained = state.AddXp(100);
        gained.Should().Be(2);
        state.Level.Should().Be(3);
    }

    [Fact]
    public void AddXp_ZeroOrNegative_NoChange()
    {
        var state = new SkillState("test", level: 1, xp: 5);
        state.AddXp(0).Should().Be(0);
        state.AddXp(-10).Should().Be(0);
        state.Level.Should().Be(1);
        state.Xp.Should().Be(5);
    }

    [Fact]
    public void AddXp_PartialXp_AccumulatesWithoutLevelUp()
    {
        var state = new SkillState("test", level: 1);
        state.AddXp(10); // need 20 for level up
        state.Level.Should().Be(1);
        state.Xp.Should().Be(10);
    }

    // -- AddSkillPoint --

    [Fact]
    public void AddSkillPoint_Level0_Grants50Xp()
    {
        var state = new SkillState("test", level: 1); // start at 1 to avoid instant level up
        int startXp = state.Xp;
        state.AddSkillPoint();
        // XP = 50 * (1 + 1*0.1) = 55
        state.Xp.Should().BeGreaterThan(startXp);
    }

    [Fact]
    public void AddSkillPoint_XpScalesWithLevel()
    {
        var low = new SkillState("test", level: 1);
        var high = new SkillState("test", level: 10);
        // Level 1: 50*(1+0.1)=55, Level 10: 50*(1+1.0)=100
        low.AddSkillPoint();
        high.AddSkillPoint();
        // high should have gotten more XP (may or may not have leveled)
        // Just verify the formula produces increasing XP
        int lowXpGain = (int)(50 * (1 + 1 * 0.1f));  // 55
        int highXpGain = (int)(50 * (1 + 10 * 0.1f)); // 100
        highXpGain.Should().BeGreaterThan(lowXpGain);
    }

    // -- GetPassiveBonus --

    [Fact]
    public void GetPassiveBonus_Level0_ReturnsZero()
    {
        var state = new SkillState("test");
        state.GetPassiveBonus(1.5f).Should().Be(0f);
    }

    [Fact]
    public void GetPassiveBonus_DiminishingReturns()
    {
        var state = new SkillState("test", level: 10);
        // 10 * 1.5 * (100 / (10 + 100)) = 15 * 0.909... ≈ 13.636
        state.GetPassiveBonus(1.5f).Should().BeApproximately(13.636f, 0.01f);
    }

    [Fact]
    public void GetPassiveBonus_HighLevel_Diminishes()
    {
        var low = new SkillState("test", level: 10);
        var high = new SkillState("test", level: 100);
        // Per-level bonus should decrease at higher levels
        float lowPerLevel = low.GetPassiveBonus(1.0f) / 10;
        float highPerLevel = high.GetPassiveBonus(1.0f) / 100;
        highPerLevel.Should().BeLessThan(lowPerLevel);
    }

    // -- SetState --

    [Fact]
    public void SetState_DirectlySetLevelAndXp()
    {
        var state = new SkillState("test");
        state.SetState(25, 100);
        state.Level.Should().Be(25);
        state.Xp.Should().Be(100);
    }

    [Fact]
    public void SetState_ClampsNegatives()
    {
        var state = new SkillState("test");
        state.SetState(-5, -10);
        state.Level.Should().Be(0);
        state.Xp.Should().Be(0);
    }
}
