using System.Linq;
using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

public class AchievementSystemTests
{
    // -- Counter management --

    [Fact]
    public void GetCounter_Default_ReturnsZero()
    {
        new AchievementTracker().GetCounter("enemies_killed").Should().Be(0);
    }

    [Fact]
    public void IncrementCounter_IncreasesValue()
    {
        var tracker = new AchievementTracker();
        tracker.IncrementCounter("enemies_killed");
        tracker.GetCounter("enemies_killed").Should().Be(1);
    }

    [Fact]
    public void IncrementCounter_ByAmount_Accumulates()
    {
        var tracker = new AchievementTracker();
        tracker.IncrementCounter("enemies_killed", 5);
        tracker.IncrementCounter("enemies_killed", 3);
        tracker.GetCounter("enemies_killed").Should().Be(8);
    }

    [Fact]
    public void SetCounter_SetsMaxOnly()
    {
        var tracker = new AchievementTracker();
        tracker.SetCounter("deepest_floor", 10);
        tracker.SetCounter("deepest_floor", 5); // lower, should be ignored
        tracker.GetCounter("deepest_floor").Should().Be(10);
    }

    [Fact]
    public void SetCounter_HigherValue_Updates()
    {
        var tracker = new AchievementTracker();
        tracker.SetCounter("deepest_floor", 5);
        tracker.SetCounter("deepest_floor", 15);
        tracker.GetCounter("deepest_floor").Should().Be(15);
    }

    // -- Evaluate --

    [Fact]
    public void Evaluate_UnlocksWhenThresholdReached()
    {
        var tracker = new AchievementTracker();
        tracker.IncrementCounter("enemies_killed", 1);
        var unlocked = tracker.Evaluate();
        unlocked.Should().Contain(a => a.Id == "c_first_blood");
    }

    [Fact]
    public void Evaluate_ReturnsOnlyNewlyUnlocked()
    {
        var tracker = new AchievementTracker();
        tracker.IncrementCounter("enemies_killed", 1);
        tracker.Evaluate(); // first call unlocks it
        var second = tracker.Evaluate(); // second call should return nothing new
        second.Should().NotContain(a => a.Id == "c_first_blood");
    }

    [Fact]
    public void Evaluate_DoesNotDoubleUnlock()
    {
        var tracker = new AchievementTracker();
        tracker.IncrementCounter("enemies_killed", 100);
        tracker.Evaluate();
        tracker.IsUnlocked("c_first_blood").Should().BeTrue();
        tracker.IsUnlocked("c_100_kills").Should().BeTrue();

        var again = tracker.Evaluate();
        again.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_MultipleAchievementsSameCounter()
    {
        var tracker = new AchievementTracker();
        tracker.IncrementCounter("enemies_killed", 1000);
        var unlocked = tracker.Evaluate();
        unlocked.Should().Contain(a => a.Id == "c_first_blood");
        unlocked.Should().Contain(a => a.Id == "c_100_kills");
        unlocked.Should().Contain(a => a.Id == "c_1000_kills");
    }

    // -- GetProgress --

    [Fact]
    public void GetProgress_ZeroProgress_ReturnsZero()
    {
        var tracker = new AchievementTracker();
        var def = AchievementTracker.GetAll()[0];
        tracker.GetProgress(def).Should().Be(0f);
    }

    [Fact]
    public void GetProgress_HalfWay_ReturnsHalf()
    {
        var tracker = new AchievementTracker();
        // c_100_kills needs 100
        var def = AchievementTracker.GetAll().First(a => a.Id == "c_100_kills");
        tracker.IncrementCounter("enemies_killed", 50);
        tracker.GetProgress(def).Should().BeApproximately(0.5f, 0.01f);
    }

    [Fact]
    public void GetProgress_Over100Percent_CapsAt1()
    {
        var tracker = new AchievementTracker();
        var def = AchievementTracker.GetAll().First(a => a.Id == "c_first_blood");
        tracker.IncrementCounter("enemies_killed", 999);
        tracker.GetProgress(def).Should().Be(1f);
    }

    // -- IsUnlocked --

    [Fact]
    public void IsUnlocked_BeforeEval_ReturnsFalse()
    {
        var tracker = new AchievementTracker();
        tracker.IncrementCounter("enemies_killed", 999);
        tracker.IsUnlocked("c_first_blood").Should().BeFalse(); // not evaluated yet
    }

    [Fact]
    public void IsUnlocked_AfterEval_ReturnsTrue()
    {
        var tracker = new AchievementTracker();
        tracker.IncrementCounter("enemies_killed", 1);
        tracker.Evaluate();
        tracker.IsUnlocked("c_first_blood").Should().BeTrue();
    }

    // -- GetAll / GetByCategory --

    [Fact]
    public void GetAll_ReturnsNonEmpty()
    {
        AchievementTracker.GetAll().Should().NotBeEmpty();
    }

    [Fact]
    public void GetByCategory_FiltersByCategory()
    {
        var combat = AchievementTracker.GetByCategory(AchievementCategory.Combat).ToList();
        combat.Should().NotBeEmpty();
        combat.Should().OnlyContain(a => a.Category == AchievementCategory.Combat);
    }

    // -- Save/Load --

    [Fact]
    public void CaptureRestore_RoundTrips()
    {
        var tracker = new AchievementTracker();
        tracker.IncrementCounter("enemies_killed", 50);
        tracker.IncrementCounter("deepest_floor", 10);
        tracker.Evaluate();

        var state = tracker.CaptureState();

        var restored = new AchievementTracker();
        restored.RestoreState(state);
        restored.GetCounter("enemies_killed").Should().Be(50);
        restored.GetCounter("deepest_floor").Should().Be(10);
        restored.IsUnlocked("c_first_blood").Should().BeTrue();
    }

    [Fact]
    public void CaptureState_EmptyTracker_HasEmptyArrays()
    {
        var state = new AchievementTracker().CaptureState();
        state.Counters.Should().BeEmpty();
        state.UnlockedIds.Should().BeEmpty();
    }
}
