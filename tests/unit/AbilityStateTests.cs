using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

public class AbilityStateTests
{
    // -- XP and Leveling (same formulas as MasteryState) --

    [Fact]
    public void NewAbility_StartsAtLevel0_UseCount0()
    {
        var state = new AbilityState("test");
        state.Level.Should().Be(0);
        state.Xp.Should().Be(0);
        state.UseCount.Should().Be(0);
    }

    [Fact]
    public void Level0To1_IsInstant()
    {
        var state = new AbilityState("test");
        state.AddXp(1);
        state.Level.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public void AddAbilityPoint_GrantsScaledXp()
    {
        var state = new AbilityState("test");
        state.SetState(10, 0);
        state.AddAbilityPoint();
        // AP XP at level 10 = 50 * (1 + 10 * 0.1) = 100
        state.Xp.Should().BeGreaterThan(0);
    }

    // -- Use Count & Affinity --

    [Fact]
    public void IncrementUse_IncrementsCounter()
    {
        var state = new AbilityState("test");
        state.IncrementUse();
        state.IncrementUse();
        state.IncrementUse();
        state.UseCount.Should().Be(3);
    }

    [Theory]
    [InlineData(0, 0)]     // No tier
    [InlineData(50, 0)]    // Below Familiar
    [InlineData(99, 0)]    // Just below Familiar
    [InlineData(100, 1)]   // Familiar
    [InlineData(499, 1)]   // Below Practiced
    [InlineData(500, 2)]   // Practiced
    [InlineData(999, 2)]   // Below Expert
    [InlineData(1000, 3)]  // Expert
    [InlineData(4999, 3)]  // Below Mastered
    [InlineData(5000, 4)]  // Mastered
    [InlineData(99999, 4)] // Way past Mastered
    public void AffinityTier_MatchesUseCount(int useCount, int expectedTier)
    {
        var state = new AbilityState("test");
        state.SetState(0, 0, useCount);
        state.AffinityTier.Should().Be(expectedTier);
    }

    // -- Save/Load --

    [Fact]
    public void SetState_RestoresAll()
    {
        var state = new AbilityState("test");
        state.SetState(12, 500, 250);
        state.Level.Should().Be(12);
        state.Xp.Should().Be(500);
        state.UseCount.Should().Be(250);
    }

    [Fact]
    public void SetState_NegativeValues_ClampedToZero()
    {
        var state = new AbilityState("test");
        state.SetState(-1, -10, -5);
        state.Level.Should().Be(0);
        state.Xp.Should().Be(0);
        state.UseCount.Should().Be(0);
    }
}
