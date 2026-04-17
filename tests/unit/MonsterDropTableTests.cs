using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

/// <summary>
/// Tests for MonsterDropTable (ITEM-02). Covers floor→tier mapping, per-species
/// drop-table presence, deterministic rolls via seeded Random, and material-type
/// thematic bias.
/// </summary>
public class MonsterDropTableTests
{
    // ── FloorToTier ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(1, 1)]
    [InlineData(10, 1)]
    [InlineData(11, 2)]
    [InlineData(25, 2)]
    [InlineData(26, 3)]
    [InlineData(50, 3)]
    [InlineData(51, 4)]
    [InlineData(100, 4)]
    [InlineData(101, 5)]
    [InlineData(999, 5)]
    public void FloorToTier_Maps_Correctly(int floor, int expectedTier)
    {
        MonsterDropTable.FloorToTier(floor).Should().Be(expectedTier);
    }

    // ── DropTable lookup ─────────────────────────────────────────────────

    [Theory]
    [InlineData(EnemySpecies.Skeleton, "material_sig_skeleton")]
    [InlineData(EnemySpecies.Goblin, "material_sig_goblin")]
    [InlineData(EnemySpecies.Bat, "material_sig_bat")]
    [InlineData(EnemySpecies.Wolf, "material_sig_wolf")]
    [InlineData(EnemySpecies.Orc, "material_sig_orc")]
    [InlineData(EnemySpecies.DarkMage, "material_sig_darkmage")]
    [InlineData(EnemySpecies.Spider, "material_sig_spider")]
    public void AllSpecies_HaveSignatureMaterialInCatalog(EnemySpecies species, string expectedId)
    {
        var table = MonsterDropTable.Get(species);
        table.Should().NotBeNull();
        table!.SignatureMaterialId.Should().Be(expectedId);
        ItemDatabase.Get(expectedId).Should().NotBeNull();
    }

    // ── RollEquipment ────────────────────────────────────────────────────

    [Fact]
    public void RollEquipment_NeverExceedsTierForFloor()
    {
        // Seed for determinism; sample many rolls and assert no out-of-bracket items.
        var rng = new Random(42);
        int floor = 15; // Tier 2 bracket.
        int expectedTier = MonsterDropTable.FloorToTier(floor);

        for (int i = 0; i < 200; i++)
        {
            var drop = MonsterDropTable.RollEquipment(EnemySpecies.Goblin, floor, rng);
            if (drop != null)
                drop.Tier.Should().Be(expectedTier);
        }
    }

    [Fact]
    public void RollEquipment_ReturnsNullForUnmappedSpecies()
    {
        // No species outside the 7-member enum is currently unmapped — all are in Tables.
        // But confirm the guard returns null gracefully if one is.
        var rng = new Random(0);
        var unknown = (EnemySpecies)99;
        MonsterDropTable.RollEquipment(unknown, 5, rng).Should().BeNull();
    }

    [Fact]
    public void RollEquipment_EventuallyReturnsItemForSufficientRolls()
    {
        var rng = new Random(7);
        bool gotOne = false;
        for (int i = 0; i < 500; i++)
        {
            if (MonsterDropTable.RollEquipment(EnemySpecies.Orc, 30, rng) != null)
            {
                gotOne = true;
                break;
            }
        }
        gotOne.Should().BeTrue("base + floor rate should produce a drop within 500 Orc rolls");
    }

    // ── RollMaterials ────────────────────────────────────────────────────

    [Fact]
    public void RollMaterials_UnmappedSpecies_ReturnsEmpty()
    {
        var rng = new Random(0);
        var unknown = (EnemySpecies)99;
        MonsterDropTable.RollMaterials(unknown, 5, rng).Should().BeEmpty();
    }

    [Fact]
    public void RollMaterials_SignatureAppears_AtRoughlyExpectedFrequency()
    {
        // Skeleton signature rate = 0.10. Over 1000 rolls at seed 42, expect ~100 ±50.
        var rng = new Random(42);
        int sigCount = 0;
        for (int i = 0; i < 1000; i++)
        {
            var mats = MonsterDropTable.RollMaterials(EnemySpecies.Skeleton, 5, rng);
            if (mats.Any(m => m.Id == "material_sig_skeleton")) sigCount++;
        }
        sigCount.Should().BeInRange(50, 170,
            "signature material should appear at roughly the species signature rate (10%)");
    }

    [Fact]
    public void RollMaterials_GenericTypes_PrefersThematic()
    {
        // Bat thematic = Hide (leathery wings, per monster-drops.md). Over 5000 generic
        // drops, Hide should be the plurality type (60/20/20 split per spec).
        var rng = new Random(99);
        int ore = 0, bone = 0, hide = 0;
        for (int i = 0; i < 5000; i++)
        {
            var mats = MonsterDropTable.RollMaterials(EnemySpecies.Bat, 5, rng);
            foreach (var m in mats)
            {
                if (m.Id.StartsWith("material_ore_")) ore++;
                else if (m.Id.StartsWith("material_bone_")) bone++;
                else if (m.Id.StartsWith("material_hide_")) hide++;
            }
        }
        hide.Should().BeGreaterThan(ore);
        hide.Should().BeGreaterThan(bone);
    }

    [Fact]
    public void RollMaterials_TierMatchesFloorBracket()
    {
        var rng = new Random(123);
        int floor = 40;            // Tier 3.
        int expectedTier = MonsterDropTable.FloorToTier(floor);
        for (int i = 0; i < 200; i++)
        {
            var mats = MonsterDropTable.RollMaterials(EnemySpecies.Orc, floor, rng);
            foreach (var m in mats)
            {
                if (m.Id.StartsWith("material_ore_") ||
                    m.Id.StartsWith("material_bone_") ||
                    m.Id.StartsWith("material_hide_"))
                {
                    m.Tier.Should().Be(expectedTier);
                }
            }
        }
    }
}
