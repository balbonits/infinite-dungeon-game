using System.Linq;
using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

public class SkillAbilityDatabaseTests
{
    // -- Counts --

    [Fact]
    public void TotalMasteries_Is27()
    {
        // 8 Warrior + 7 Ranger + 8 Mage + 4 Innate = 27
        SkillAbilityDatabase.MasteryCount.Should().Be(27);
    }

    [Fact]
    public void TotalAbilities_Is103()
    {
        // 33 Warrior + 37 Ranger + 33 Mage = 103
        SkillAbilityDatabase.AbilityCount.Should().Be(103);
    }

    [Fact]
    public void Warrior_Has8ClassMasteries_33Abilities()
    {
        // Warrior has 8 class masteries + 4 innate (registered under Warrior class)
        var allWarrior = SkillAbilityDatabase.GetMasteriesByClass(PlayerClass.Warrior).ToList();
        var classOnly = allWarrior.Where(m => m.CategoryId != "innate").ToList();
        classOnly.Count.Should().Be(8);
        SkillAbilityDatabase.GetAbilitiesByClass(PlayerClass.Warrior).Count().Should().Be(33);
    }

    [Fact]
    public void Ranger_Has7Masteries_37Abilities()
    {
        SkillAbilityDatabase.GetMasteriesByClass(PlayerClass.Ranger).Count().Should().Be(7);
        SkillAbilityDatabase.GetAbilitiesByClass(PlayerClass.Ranger).Count().Should().Be(37);
    }

    [Fact]
    public void Mage_Has8Masteries_33Abilities()
    {
        SkillAbilityDatabase.GetMasteriesByClass(PlayerClass.Mage).Count().Should().Be(8);
        SkillAbilityDatabase.GetAbilitiesByClass(PlayerClass.Mage).Count().Should().Be(33);
    }

    [Fact]
    public void Innate_Has4Masteries()
    {
        SkillAbilityDatabase.GetInnateMasteries().Count().Should().Be(4);
    }

    // -- Categories --

    [Fact]
    public void Warrior_HasBodyAndMindCategories()
    {
        var cats = SkillAbilityDatabase.GetCategories(PlayerClass.Warrior);
        cats.Should().BeEquivalentTo(new[] { "warrior_body", "warrior_mind" });
    }

    [Fact]
    public void Ranger_HasWeaponryAndSurvivalCategories()
    {
        var cats = SkillAbilityDatabase.GetCategories(PlayerClass.Ranger);
        cats.Should().BeEquivalentTo(new[] { "ranger_weaponry", "ranger_survival" });
    }

    [Fact]
    public void Mage_HasElementalAetherAttunementCategories()
    {
        var cats = SkillAbilityDatabase.GetCategories(PlayerClass.Mage);
        cats.Should().BeEquivalentTo(new[] { "mage_elemental", "mage_aether", "mage_attunement" });
    }

    [Fact]
    public void CategoryNames_AllResolve()
    {
        var allCats = new[] { "warrior_body", "warrior_mind", "ranger_weaponry", "ranger_survival",
            "mage_elemental", "mage_aether", "mage_attunement", "innate" };
        foreach (var cat in allCats)
            SkillAbilityDatabase.GetCategoryName(cat).Should().NotBe(cat, $"category {cat} should have a display name");
    }

    // -- Parent Links --

    [Fact]
    public void AllAbilities_HaveValidParentMastery()
    {
        foreach (PlayerClass cls in new[] { PlayerClass.Warrior, PlayerClass.Ranger, PlayerClass.Mage })
        {
            foreach (var ability in SkillAbilityDatabase.GetAbilitiesByClass(cls))
            {
                var parent = SkillAbilityDatabase.GetMastery(ability.ParentMasteryId);
                parent.Should().NotBeNull($"ability '{ability.Name}' ({ability.Id}) references parent '{ability.ParentMasteryId}' which must exist");
            }
        }
    }

    [Fact]
    public void AllAbilities_ShareCategoryWithParentMastery()
    {
        foreach (PlayerClass cls in new[] { PlayerClass.Warrior, PlayerClass.Ranger, PlayerClass.Mage })
        {
            foreach (var ability in SkillAbilityDatabase.GetAbilitiesByClass(cls))
            {
                var parent = SkillAbilityDatabase.GetMastery(ability.ParentMasteryId)!;
                ability.CategoryId.Should().Be(parent.CategoryId, $"ability '{ability.Name}' should share category with parent '{parent.Name}'");
            }
        }
    }

    // -- No Duplicates --

    [Fact]
    public void AllMasteryIds_AreUnique()
    {
        // Innate masteries are registered under Warrior class but accessed via GetInnateMasteries()
        // GetMasteriesByClass(Warrior) includes innate, so just check all masteries have unique IDs
        var allIds = new[] { PlayerClass.Warrior, PlayerClass.Ranger, PlayerClass.Mage }
            .SelectMany(SkillAbilityDatabase.GetMasteriesByClass)
            .Select(m => m.Id)
            .ToList();
        allIds.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AllAbilityIds_AreUnique()
    {
        var allIds = new[] { PlayerClass.Warrior, PlayerClass.Ranger, PlayerClass.Mage }
            .SelectMany(SkillAbilityDatabase.GetAbilitiesByClass)
            .Select(a => a.Id)
            .ToList();
        allIds.Should().OnlyHaveUniqueItems();
    }

    // -- Specific Masteries Exist (spot checks) --

    [Fact]
    public void Warrior_DualWield_Exists()
    {
        SkillAbilityDatabase.GetMastery("w_dual_wield").Should().NotBeNull();
        SkillAbilityDatabase.GetAbilitiesForMastery("w_dual_wield").Count().Should().Be(4);
    }

    [Fact]
    public void Ranger_Sapping_Exists()
    {
        SkillAbilityDatabase.GetMastery("r_sapping").Should().NotBeNull();
        SkillAbilityDatabase.GetAbilitiesForMastery("r_sapping").Count().Should().Be(5);
    }

    [Fact]
    public void Mage_Aether_Exists_With5Abilities()
    {
        SkillAbilityDatabase.GetMastery("m_aether").Should().NotBeNull();
        SkillAbilityDatabase.GetAbilitiesForMastery("m_aether").Count().Should().Be(5);
    }

    [Fact]
    public void Ranger_Awareness_Has8Abilities()
    {
        SkillAbilityDatabase.GetAbilitiesForMastery("r_awareness").Count().Should().Be(8);
    }

    // -- Ability Counts Per Mastery (3-8 range) --

    [Fact]
    public void AllClassMasteries_HaveAbilitiesInValidRange()
    {
        foreach (PlayerClass cls in new[] { PlayerClass.Warrior, PlayerClass.Ranger, PlayerClass.Mage })
        {
            foreach (var mastery in SkillAbilityDatabase.GetMasteriesByClass(cls))
            {
                // Skip innate masteries — they don't have child abilities
                if (mastery.CategoryId == "innate") continue;

                int count = SkillAbilityDatabase.GetAbilitiesForMastery(mastery.Id).Count();
                count.Should().BeInRange(3, 8, $"mastery '{mastery.Name}' should have 3-8 abilities, has {count}");
            }
        }
    }

    [Fact]
    public void InnateMasteries_HaveNoChildAbilities()
    {
        foreach (var innate in SkillAbilityDatabase.GetInnateMasteries())
        {
            SkillAbilityDatabase.GetAbilitiesForMastery(innate.Id).Count().Should().Be(0,
                $"innate mastery '{innate.Name}' should have no child abilities — it IS the skill");
        }
    }

    // -- Renamed Abilities Exist (verify renames applied) --

    [Fact]
    public void Ranger_RenamedAbilities_Exist()
    {
        SkillAbilityDatabase.GetAbility("r_dead_eye").Should().NotBeNull("Dead Eye (was Power Shot)");
        SkillAbilityDatabase.GetAbility("r_pepper").Should().NotBeNull("Pepper (was Rapid Fire)");
        SkillAbilityDatabase.GetAbility("r_lob").Should().NotBeNull("Lob (was Arc Shot)");
        SkillAbilityDatabase.GetAbility("r_flick").Should().NotBeNull("Flick (was Knife Throw)");
        SkillAbilityDatabase.GetAbility("r_chuck").Should().NotBeNull("Chuck (was Axe Throw)");
        SkillAbilityDatabase.GetAbility("r_bead").Should().NotBeNull("Bead (was Steady Shot)");
        SkillAbilityDatabase.GetAbility("r_spray").Should().NotBeNull("Spray (was Burst Fire)");
        SkillAbilityDatabase.GetAbility("r_hunker").Should().NotBeNull("Hunker (was Guard)");
        SkillAbilityDatabase.GetAbility("r_shiv").Should().NotBeNull("Shiv (was Disarm)");
    }

    [Fact]
    public void Warrior_RenamedAbilities_Exist()
    {
        SkillAbilityDatabase.GetAbility("w_focus").Should().NotBeNull("Focus (was Battle Focus)");
        SkillAbilityDatabase.GetAbility("w_endure").Should().NotBeNull("Endure (was Pain Tolerance + Iron Will)");
        SkillAbilityDatabase.GetAbility("w_deep_breaths").Should().NotBeNull("Deep Breaths (was Second Wind)");
        SkillAbilityDatabase.GetAbility("w_blood_lust").Should().NotBeNull("Blood Lust (new)");
        SkillAbilityDatabase.GetAbility("w_shout").Should().NotBeNull("Shout (was War Cry)");
        SkillAbilityDatabase.GetAbility("w_ugly_mug").Should().NotBeNull("Ugly Mug (was Menacing Presence)");
    }

    [Fact]
    public void Mage_RenamedAbilities_Exist()
    {
        SkillAbilityDatabase.GetAbility("m_resonance").Should().NotBeNull("Resonance (was Attunement)");
        SkillAbilityDatabase.GetAbility("m_pain_gate").Should().NotBeNull("Pain Gate (was Pain Conduit)");
        SkillAbilityDatabase.GetAbility("m_nova").Should().NotBeNull("Nova (Aether, was in Light)");
        SkillAbilityDatabase.GetAbility("m_weld").Should().NotBeNull("Weld (Aether, was Heal in Light)");
        SkillAbilityDatabase.GetAbility("m_drain").Should().NotBeNull("Drain (Aether, was Drain Life in Dark)");
        SkillAbilityDatabase.GetAbility("m_singularity").Should().NotBeNull("Singularity (Aether, was Void Zone in Dark)");
    }

    // -- Old Abilities Do NOT Exist --

    [Fact]
    public void OldAbilityIds_DoNotExist()
    {
        SkillAbilityDatabase.GetAbility("w_battle_focus").Should().BeNull("old ID should not exist");
        SkillAbilityDatabase.GetAbility("w_pain_tolerance").Should().BeNull("old ID should not exist");
        SkillAbilityDatabase.GetAbility("w_second_wind").Should().BeNull("old ID should not exist");
        SkillAbilityDatabase.GetAbility("w_iron_will").Should().BeNull("old ID should not exist");
        SkillAbilityDatabase.GetAbility("w_war_cry").Should().BeNull("old ID should not exist");
        SkillAbilityDatabase.GetAbility("w_menacing").Should().BeNull("old ID should not exist");
        SkillAbilityDatabase.GetAbility("r_power_shot").Should().BeNull("old ID should not exist");
        SkillAbilityDatabase.GetAbility("r_steady_shot").Should().BeNull("old ID should not exist");
        SkillAbilityDatabase.GetAbility("r_burst_fire").Should().BeNull("old ID should not exist");
        SkillAbilityDatabase.GetAbility("m_attunement").Should().BeNull("old ID should not exist");
        SkillAbilityDatabase.GetAbility("m_pain_conduit").Should().BeNull("old ID should not exist");
    }
}
