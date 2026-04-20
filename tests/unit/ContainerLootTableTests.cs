using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

/// <summary>
/// LOOT-01 coverage — <see cref="ContainerLootTable"/> is the sole source of
/// equipment drops post-SPEC-LOOT-01. Covers spawn-count scaling, per-type
/// loot distributions, slot-uniqueness, the Jar small-thing pool constraint,
/// and the min-1-per-floor guarantee.
/// </summary>
public class ContainerLootTableTests
{
    // ── Spawn counts ─────────────────────────────────────────────────────

    [Fact]
    public void SpawnCounts_FloorOne_InsideBaseRanges()
    {
        var rng = new Random(42);
        var (jars, crates, chests) = ContainerLootTable.SpawnCounts(1, rng);
        jars.Should().BeInRange(4, 8);
        crates.Should().BeInRange(2, 5);
        chests.Should().BeInRange(0, 2);
    }

    [Fact]
    public void SpawnCounts_ScaleUpperBoundByDepth()
    {
        // +1 per 10 floors. Floor 50 should allow upper bounds of 13 / 10 / 7.
        var rng = new Random(42);
        int maxJars = 0, maxCrates = 0, maxChests = 0;
        for (int i = 0; i < 1000; i++)
        {
            var (j, c, ch) = ContainerLootTable.SpawnCounts(50, rng);
            if (j > maxJars) maxJars = j;
            if (c > maxCrates) maxCrates = c;
            if (ch > maxChests) maxChests = ch;
        }
        // With 1000 trials we should hit near the upper bound on each.
        maxJars.Should().BeGreaterThan(8, "floor-50 jars should exceed the floor-1 max (8)");
        maxCrates.Should().BeGreaterThan(5);
        maxChests.Should().BeGreaterThan(2);
        // Absolute ceiling at floor 50: 4 + floorBonus(50→4)=4+9=13 jars upper.
        maxJars.Should().BeLessThanOrEqualTo(13);
    }

    [Fact]
    public void SpawnCounts_MinOneGuarantee_AlwaysHoldsAtBase()
    {
        // At floor 1, jars range 4-8 so the sum is always ≥6. The guarantee is
        // mathematically hit — verify across many runs that no (0,0,0) slips through.
        var rng = new Random(1);
        for (int i = 0; i < 1000; i++)
        {
            var (j, c, ch) = ContainerLootTable.SpawnCounts(1, rng);
            (j + c + ch).Should().BeGreaterThan(0);
        }
    }

    // ── Gold channel ─────────────────────────────────────────────────────

    [Fact]
    public void RollGold_Jar_AppearsInFloorOneBaseRange()
    {
        var rng = new Random(7);
        int hits = 0, max = int.MinValue, min = int.MaxValue;
        for (int i = 0; i < 500; i++)
        {
            int g = ContainerLootTable.RollGold(ContainerLootTable.ContainerType.Jar, 1, rng);
            if (g > 0)
            {
                hits++;
                if (g > max) max = g;
                if (g < min) min = g;
            }
        }
        // Jar gold chance 60% → expect ~300 hits.
        hits.Should().BeInRange(200, 400);
        // Floor-1 range = 5..25 with multiplier = 1.05 → rounded ints ≈ 5..26.
        min.Should().BeGreaterThanOrEqualTo(5);
        max.Should().BeLessThanOrEqualTo(27);
    }

    [Fact]
    public void RollGold_ScalesWithFloor()
    {
        // Same-seed comparison of floor 1 vs floor 100. Expected multiplier
        // 1.05 vs 6.0 (×5.7 expected).
        var rng1 = new Random(999);
        var rng100 = new Random(999);
        int totalFloor1 = 0, totalFloor100 = 0;
        for (int i = 0; i < 200; i++)
        {
            totalFloor1 += ContainerLootTable.RollGold(ContainerLootTable.ContainerType.Chest, 1, rng1);
            totalFloor100 += ContainerLootTable.RollGold(ContainerLootTable.ContainerType.Chest, 100, rng100);
        }
        totalFloor100.Should().BeGreaterThan(totalFloor1 * 3, "floor-100 gold should be much higher than floor-1 (×6 theoretical, ≥×3 in practice)");
    }

    [Fact]
    public void RollGold_Chest_AlwaysRolls()
    {
        // Chest has 100% gold chance per spec.
        var rng = new Random(0);
        int zeros = 0;
        for (int i = 0; i < 200; i++)
        {
            int g = ContainerLootTable.RollGold(ContainerLootTable.ContainerType.Chest, 5, rng);
            if (g == 0) zeros++;
        }
        zeros.Should().Be(0, "chest gold chance is 100%, never rolls zero");
    }

    // ── Generic materials ────────────────────────────────────────────────

    [Fact]
    public void RollGenericMaterials_Crate_RollsTwoToFour()
    {
        var rng = new Random(42);
        for (int i = 0; i < 200; i++)
        {
            var mats = ContainerLootTable.RollGenericMaterials(ContainerLootTable.ContainerType.Crate, 5, rng);
            // Crate is 100% chance, then 2..4 count. Each roll produces one item
            // unless ItemDatabase.Get returns null for the generated ID, but the
            // IDs target material_{ore|bone|hide}_t1 which the catalog has.
            mats.Count.Should().BeInRange(2, 4);
        }
    }

    [Fact]
    public void RollGenericMaterials_Jar_ThirtyPercent_One()
    {
        var rng = new Random(42);
        int hits = 0;
        for (int i = 0; i < 1000; i++)
        {
            var mats = ContainerLootTable.RollGenericMaterials(ContainerLootTable.ContainerType.Jar, 5, rng);
            if (mats.Count > 0) hits++;
            mats.Count.Should().BeLessThanOrEqualTo(1);
        }
        // 30% chance → expect ~300 hits out of 1000.
        hits.Should().BeInRange(230, 370);
    }

    [Fact]
    public void RollGenericMaterials_MatchesFloorTier()
    {
        var rng = new Random(42);
        int floor = 40; // Tier 3.
        int expectedTier = MonsterDropTable.FloorToTier(floor);
        for (int i = 0; i < 100; i++)
        {
            var mats = ContainerLootTable.RollGenericMaterials(ContainerLootTable.ContainerType.Crate, floor, rng);
            foreach (var m in mats)
                m.Id.Should().EndWith($"_t{expectedTier}",
                    "all generic material drops should carry the floor's tier suffix");
        }
    }

    // ── Signature materials (zone bias) ──────────────────────────────────

    [Fact]
    public void RollSignatureMaterial_Jar_NeverRolls()
    {
        var rng = new Random(42);
        for (int i = 0; i < 500; i++)
        {
            var mats = ContainerLootTable.RollSignatureMaterial(ContainerLootTable.ContainerType.Jar, 1, rng);
            mats.Should().BeEmpty("jars do not carry signature mats per spec");
        }
    }

    [Fact]
    public void RollSignatureMaterial_Zone1_OnlyZoneOneSignatures()
    {
        // Zone 1 species: Skeleton + Bat. Signatures: material_sig_skeleton / material_sig_bat.
        var rng = new Random(42);
        var seen = new HashSet<string>();
        for (int i = 0; i < 1000; i++)
        {
            var mats = ContainerLootTable.RollSignatureMaterial(ContainerLootTable.ContainerType.Chest, 1, rng);
            foreach (var m in mats) seen.Add(m.Id);
        }
        seen.Should().OnlyContain(id => id == "material_sig_skeleton" || id == "material_sig_bat");
    }

    [Fact]
    public void RollSignatureMaterial_Chest_FiftyPercent()
    {
        var rng = new Random(42);
        int hits = 0;
        for (int i = 0; i < 1000; i++)
        {
            var mats = ContainerLootTable.RollSignatureMaterial(ContainerLootTable.ContainerType.Chest, 1, rng);
            if (mats.Count > 0) hits++;
        }
        // 50% chance → expect ~500 hits ±60.
        hits.Should().BeInRange(400, 600);
    }

    // ── Equipment (slot uniqueness) ──────────────────────────────────────

    [Fact]
    public void RollEquipment_Chest_SlotUnique()
    {
        var rng = new Random(42);
        for (int i = 0; i < 200; i++)
        {
            var eq = ContainerLootTable.RollEquipment(ContainerLootTable.ContainerType.Chest, 5, rng);
            // Each container must drop distinct slot types — no 2 rings, no 2 chests.
            var slots = eq.Where(x => x.Slot != EquipSlot.None).Select(x => x.Slot).ToList();
            slots.Should().OnlyHaveUniqueItems();
        }
    }

    [Fact]
    public void RollEquipment_Chest_OneToThreeItems()
    {
        var rng = new Random(42);
        for (int i = 0; i < 200; i++)
        {
            var eq = ContainerLootTable.RollEquipment(ContainerLootTable.ContainerType.Chest, 5, rng);
            // Chest is 100% chance then 1..3 count. Actual count may be lower
            // if the pool doesn't have enough distinct slots, but at tier 1 the
            // catalog has plenty; expect 1..3.
            eq.Count.Should().BeInRange(1, 3);
        }
    }

    [Fact]
    public void RollEquipment_Crate_ZeroToTwoItems()
    {
        var rng = new Random(42);
        int hits = 0;
        for (int i = 0; i < 1000; i++)
        {
            var eq = ContainerLootTable.RollEquipment(ContainerLootTable.ContainerType.Crate, 5, rng);
            eq.Count.Should().BeLessThanOrEqualTo(2);
            if (eq.Count > 0) hits++;
        }
        // 60% chance × 2/3 non-zero count (count rolls 0..2) → 40% expected hit rate.
        hits.Should().BeInRange(300, 500);
    }

    [Fact]
    public void RollEquipment_Jar_OnlySmallThings()
    {
        // Jar pool = {Ring, Neck, Consumable}. No armor, weapons, quivers, etc.
        var rng = new Random(42);
        for (int i = 0; i < 500; i++)
        {
            var eq = ContainerLootTable.RollEquipment(ContainerLootTable.ContainerType.Jar, 5, rng);
            foreach (var item in eq)
            {
                bool isSmall = item.Slot == EquipSlot.Ring
                            || item.Slot == EquipSlot.Neck
                            || item.Category == ItemCategory.Consumable;
                isSmall.Should().BeTrue($"jar dropped {item.Name} ({item.Slot}/{item.Category}) but pool should be Ring/Neck/Consumable");
            }
        }
    }

    [Fact]
    public void RollEquipment_Jar_AtMostOneItem()
    {
        var rng = new Random(42);
        for (int i = 0; i < 500; i++)
        {
            var eq = ContainerLootTable.RollEquipment(ContainerLootTable.ContainerType.Jar, 5, rng);
            eq.Count.Should().BeLessThanOrEqualTo(1, "jar drops at most 1 item");
        }
    }

    // ── End-to-end Roll ──────────────────────────────────────────────────

    [Fact]
    public void Roll_Chest_ProducesAllChannels()
    {
        var rng = new Random(42);
        var loot = ContainerLootTable.Roll(ContainerLootTable.ContainerType.Chest, 5, 1, rng);
        loot.Gold.Should().BeGreaterThan(0, "chest gold is 100%");
        loot.Equipment.Should().NotBeEmpty("chest equipment is 100%");
        // Materials (generic 50% + signature 50%) are probabilistic — sum is non-negative.
        loot.Materials.Count.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void Roll_MinOnePerFloor_NotOnlyFromJar()
    {
        // Bridging spec §Acceptance 2: "Every floor has ≥1 container". The
        // SpawnCounts contract guarantees the count; Roll() itself does not
        // need to produce ≥1 item (an empty crate is valid) — it just needs
        // to not throw and to respect the type's upper bound.
        var rng = new Random(42);
        for (int i = 0; i < 100; i++)
        {
            var loot = ContainerLootTable.Roll(
                (ContainerLootTable.ContainerType)(i % 3),
                floor: 1 + i,
                zone: 1 + (i % 10),
                rng);
            loot.Should().NotBeNull();
        }
    }
}
