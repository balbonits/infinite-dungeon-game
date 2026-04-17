using System.Linq;
using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

public class ProgressionTrackerTests
{
    private ProgressionTracker CreateWarriorTracker() => new(PlayerClass.Warrior);
    private ProgressionTracker CreateRangerTracker() => new(PlayerClass.Ranger);
    private ProgressionTracker CreateMageTracker() => new(PlayerClass.Mage);

    // -- Initialization --

    [Fact]
    public void NewTracker_HasClassMasteries()
    {
        var tracker = CreateWarriorTracker();
        tracker.AllMasteries.Count().Should().BeGreaterOrEqualTo(8, "Warrior has 8 class masteries");
    }

    [Fact]
    public void NewTracker_HasClassAbilities()
    {
        var tracker = CreateWarriorTracker();
        tracker.AllAbilities.Count().Should().Be(33, "Warrior has 33 abilities");
    }

    [Fact]
    public void NewTracker_IncludesInnateMasteries()
    {
        var tracker = CreateWarriorTracker();
        tracker.GetMastery("innate_haste").Should().NotBeNull();
        tracker.GetMastery("innate_sense").Should().NotBeNull();
        tracker.GetMastery("innate_fortify").Should().NotBeNull();
        tracker.GetMastery("innate_armor").Should().NotBeNull();
    }

    [Fact]
    public void NewTracker_ZeroPoints()
    {
        var tracker = CreateWarriorTracker();
        tracker.SkillPoints.Should().Be(0);
        tracker.AbilityPoints.Should().Be(0);
    }

    // -- SP Allocation (Masteries Only) --

    [Fact]
    public void AllocateSP_OnMastery_Succeeds()
    {
        var tracker = CreateWarriorTracker();
        tracker.SkillPoints = 5;
        tracker.AllocateSP("w_bladed").Should().BeTrue();
        tracker.SkillPoints.Should().Be(4);
        tracker.GetMastery("w_bladed")!.Level.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public void AllocateSP_NoPoints_Fails()
    {
        var tracker = CreateWarriorTracker();
        tracker.SkillPoints = 0;
        tracker.AllocateSP("w_bladed").Should().BeFalse();
    }

    [Fact]
    public void AllocateSP_OnInnateMastery_Succeeds()
    {
        var tracker = CreateWarriorTracker();
        tracker.SkillPoints = 1;
        tracker.AllocateSP("innate_haste").Should().BeTrue();
    }

    [Fact]
    public void AllocateSP_OnWrongClassMastery_Fails()
    {
        var tracker = CreateWarriorTracker();
        tracker.SkillPoints = 5;
        tracker.AllocateSP("r_bowmanship").Should().BeFalse();
    }

    [Fact]
    public void AllocateSP_OnAbilityId_Fails()
    {
        var tracker = CreateWarriorTracker();
        tracker.SkillPoints = 5;
        // Ability IDs should not be accepted by AllocateSP
        tracker.AllocateSP("w_slash").Should().BeFalse();
    }

    // -- AP Allocation (Abilities Only) --

    [Fact]
    public void AllocateAP_OnUnlockedAbility_Succeeds()
    {
        var tracker = CreateWarriorTracker();
        // First unlock the parent mastery
        tracker.SkillPoints = 1;
        tracker.AllocateSP("w_bladed");
        tracker.GetMastery("w_bladed")!.Level.Should().BeGreaterOrEqualTo(1);

        tracker.AbilityPoints = 3;
        tracker.AllocateAP("w_slash").Should().BeTrue();
        tracker.AbilityPoints.Should().Be(2);
    }

    [Fact]
    public void AllocateAP_OnLockedAbility_Fails()
    {
        var tracker = CreateWarriorTracker();
        tracker.AbilityPoints = 5;
        // Parent mastery at level 0 — ability is locked
        tracker.GetMastery("w_bladed")!.Level.Should().Be(0);
        tracker.AllocateAP("w_slash").Should().BeFalse();
    }

    [Fact]
    public void AllocateAP_NoPoints_Fails()
    {
        var tracker = CreateWarriorTracker();
        tracker.SkillPoints = 1;
        tracker.AllocateSP("w_bladed");
        tracker.AbilityPoints = 0;
        tracker.AllocateAP("w_slash").Should().BeFalse();
    }

    [Fact]
    public void AllocateAP_WrongClass_Fails()
    {
        var tracker = CreateWarriorTracker();
        tracker.AbilityPoints = 5;
        tracker.AllocateAP("r_dead_eye").Should().BeFalse();
    }

    // -- Ability Use & XP Tracking --

    [Fact]
    public void RecordAbilityUse_GrantsXpToAbilityAndParent()
    {
        var tracker = CreateWarriorTracker();
        // Unlock mastery first
        tracker.SkillPoints = 1;
        tracker.AllocateSP("w_bladed");

        tracker.RecordAbilityUse("w_slash", 1);

        tracker.GetAbility("w_slash")!.Xp.Should().BeGreaterThan(0);
        tracker.GetAbility("w_slash")!.UseCount.Should().Be(1);
        // Parent mastery should also gain XP (beyond what SP gave)
    }

    [Fact]
    public void RecordAbilityUse_FloorMultiplier_IncreasesXp()
    {
        var tracker1 = CreateWarriorTracker();
        tracker1.RecordAbilityUse("w_slash", 1);
        int xpFloor1 = tracker1.GetAbility("w_slash")!.Xp;

        var tracker2 = CreateWarriorTracker();
        tracker2.RecordAbilityUse("w_slash", 10);
        int xpFloor10 = tracker2.GetAbility("w_slash")!.Xp;

        xpFloor10.Should().BeGreaterThan(xpFloor1, "deeper floor = more XP");
    }

    [Fact]
    public void RecordAbilityUse_WrongClass_DoesNothing()
    {
        var tracker = CreateWarriorTracker();
        tracker.RecordAbilityUse("r_dead_eye", 1);
        tracker.GetAbility("r_dead_eye").Should().BeNull();
    }

    [Fact]
    public void RecordAbilityUse_IncrementsCategoryUseCount()
    {
        var tracker = CreateWarriorTracker();
        for (int i = 0; i < 100; i++)
            tracker.RecordAbilityUse("w_slash", 1);

        tracker.GetCategoryApEarned("warrior_body").Should().Be(1, "100 uses = 1 category AP");
    }

    // -- Category AP --

    [Fact]
    public void CategoryAP_100Uses_Earns1AP()
    {
        var tracker = CreateRangerTracker();
        for (int i = 0; i < 250; i++)
            tracker.RecordAbilityUse("r_dead_eye", 1);

        tracker.GetCategoryApEarned("ranger_weaponry").Should().Be(2, "250 uses = 2 category AP");
        tracker.GetCategoryApAvailable("ranger_weaponry").Should().Be(2);
    }

    [Fact]
    public void AllocateCategoryAP_Succeeds_WhenAvailable()
    {
        var tracker = CreateRangerTracker();
        // Earn category AP
        for (int i = 0; i < 100; i++)
            tracker.RecordAbilityUse("r_dead_eye", 1);
        // Unlock mastery
        tracker.SkillPoints = 1;
        tracker.AllocateSP("r_bowmanship");

        tracker.AllocateCategoryAP("r_dead_eye").Should().BeTrue();
        tracker.GetCategoryApAvailable("ranger_weaponry").Should().Be(0);
    }

    [Fact]
    public void AllocateCategoryAP_WrongCategory_Fails()
    {
        var tracker = CreateRangerTracker();
        // Earn AP in Weaponry...
        for (int i = 0; i < 100; i++)
            tracker.RecordAbilityUse("r_dead_eye", 1);
        // ...but try to spend on Survival ability
        tracker.SkillPoints = 1;
        tracker.AllocateSP("r_awareness");
        tracker.AllocateCategoryAP("r_keen_senses").Should().BeFalse();
    }

    [Fact]
    public void AllocateCategoryAP_NoneEarned_Fails()
    {
        var tracker = CreateRangerTracker();
        tracker.SkillPoints = 1;
        tracker.AllocateSP("r_bowmanship");
        tracker.AllocateCategoryAP("r_dead_eye").Should().BeFalse();
    }

    // -- Unlock Check --

    [Fact]
    public void IsUnlocked_ParentAtLevel0_ReturnsFalse()
    {
        var tracker = CreateWarriorTracker();
        tracker.IsUnlocked("w_slash").Should().BeFalse();
    }

    [Fact]
    public void IsUnlocked_ParentAtLevel1_ReturnsTrue()
    {
        var tracker = CreateWarriorTracker();
        tracker.SkillPoints = 1;
        tracker.AllocateSP("w_bladed");
        tracker.IsUnlocked("w_slash").Should().BeTrue();
    }

    [Fact]
    public void IsUnlocked_AllChildrenUnlockAtLevel1()
    {
        var tracker = CreateWarriorTracker();
        tracker.SkillPoints = 1;
        tracker.AllocateSP("w_bladed");

        // All Bladed abilities should unlock at once
        tracker.IsUnlocked("w_slash").Should().BeTrue();
        tracker.IsUnlocked("w_thrust").Should().BeTrue();
        tracker.IsUnlocked("w_cleave").Should().BeTrue();
        tracker.IsUnlocked("w_parry").Should().BeTrue();
    }

    // -- Passive Bonus --

    [Fact]
    public void GetTotalPassiveBonus_SumsAcrossMasteries()
    {
        var tracker = CreateWarriorTracker();
        tracker.SkillPoints = 10;
        // Invest in two Damage-type masteries
        for (int i = 0; i < 5; i++)
        {
            tracker.AllocateSP("w_bladed");
            tracker.AllocateSP("w_blunt");
        }

        float bonus = tracker.GetTotalPassiveBonus(PassiveBonusType.Damage);
        bonus.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetTotalPassiveBonus_Level0Masteries_ContributeNothing()
    {
        var tracker = CreateWarriorTracker();
        tracker.GetTotalPassiveBonus(PassiveBonusType.Damage).Should().Be(0);
    }

    // -- Save/Load Round Trip --

    [Fact]
    public void SaveRestore_MasteriesRoundTrip()
    {
        var tracker = CreateWarriorTracker();
        tracker.SkillPoints = 5;
        tracker.AllocateSP("w_bladed");
        tracker.AllocateSP("w_bladed");

        var saved = tracker.CaptureMasteries();

        var tracker2 = CreateWarriorTracker();
        tracker2.RestoreMasteries(saved);

        tracker2.GetMastery("w_bladed")!.Level.Should().Be(tracker.GetMastery("w_bladed")!.Level);
    }

    [Fact]
    public void SaveRestore_AbilitiesRoundTrip()
    {
        var tracker = CreateWarriorTracker();
        for (int i = 0; i < 50; i++)
            tracker.RecordAbilityUse("w_slash", 1);

        var saved = tracker.CaptureAbilities();

        var tracker2 = CreateWarriorTracker();
        tracker2.RestoreAbilities(saved);

        tracker2.GetAbility("w_slash")!.UseCount.Should().Be(50);
        tracker2.GetAbility("w_slash")!.Level.Should().Be(tracker.GetAbility("w_slash")!.Level);
    }

    [Fact]
    public void SaveRestore_CategoryTrackingRoundTrip()
    {
        var tracker = CreateWarriorTracker();
        for (int i = 0; i < 150; i++)
            tracker.RecordAbilityUse("w_slash", 1);

        var useCounts = tracker.CaptureCategoryUseCounts();
        var apSpent = tracker.CaptureCategoryApSpent();

        var tracker2 = CreateWarriorTracker();
        tracker2.RestoreCategoryTracking(useCounts, apSpent);

        tracker2.GetCategoryApEarned("warrior_body").Should().Be(1);
    }

    [Fact]
    public void SaveRestore_NullSavedData_DoesNotCrash()
    {
        var tracker = CreateWarriorTracker();
        tracker.RestoreMasteries(null);
        tracker.RestoreAbilities(null);
        tracker.RestoreCategoryTracking(null, null);
        // Should not throw
    }

    // -- All Three Classes Initialize --

    [Fact]
    public void AllClasses_InitializeWithCorrectCounts()
    {
        var warrior = CreateWarriorTracker();
        var ranger = CreateRangerTracker();
        var mage = CreateMageTracker();

        // Class masteries + 4 innate
        warrior.AllMasteries.Count().Should().Be(8 + 4);
        ranger.AllMasteries.Count().Should().Be(7 + 4);
        mage.AllMasteries.Count().Should().Be(8 + 4);

        warrior.AllAbilities.Count().Should().Be(33);
        ranger.AllAbilities.Count().Should().Be(37);
        mage.AllAbilities.Count().Should().Be(33);
    }
}
