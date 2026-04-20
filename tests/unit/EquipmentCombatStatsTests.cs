using System;
using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

/// <summary>
/// COMBAT-01 coverage — cache behavior + ring-focus accumulation.
/// The cache lives on <see cref="EquipmentSet"/> and must invalidate on
/// every mutator so downstream consumers (Player.ExecuteAttack,
/// GameState.RecomputeDerivedStats) never see stale values.
/// </summary>
public class EquipmentCombatStatsTests
{
    private static ItemDef NonStatBody() => new()
    {
        Id = "body_test_plain",
        Name = "Plain Body",
        Category = ItemCategory.Body,
        Slot = EquipSlot.Body,
        Tier = 1,
    };

    private static ItemDef StatStrNeck() => new()
    {
        Id = "neck_test_str",
        Name = "STR Neck",
        Category = ItemCategory.Neck,
        Slot = EquipSlot.Neck,
        Tier = 1,
        BonusStr = 5,
    };

    private static ItemDef CritRing(int tier) => new()
    {
        Id = $"ring_test_crit_t{tier}",
        Name = $"Test Crit Ring T{tier}",
        Category = ItemCategory.Ring,
        Slot = EquipSlot.Ring,
        Tier = tier,
        RingFocus = RingFocus.Crit,
    };

    private static ItemDef HasteRing(int tier) => new()
    {
        Id = $"ring_test_haste_t{tier}",
        Name = $"Test Haste Ring T{tier}",
        Category = ItemCategory.Ring,
        Slot = EquipSlot.Ring,
        Tier = tier,
        RingFocus = RingFocus.Haste,
    };

    // ── Ring-focus accumulation (core spec §7) ───────────────────────────

    [Fact]
    public void GetCombatStats_NoRings_AllRawValuesZero()
    {
        var eq = new EquipmentSet();
        var stats = eq.GetCombatStats(PlayerClass.Warrior);
        stats.CritRaw.Should().Be(0f);
        stats.HasteRaw.Should().Be(0f);
        stats.DodgeRaw.Should().Be(0f);
        stats.BlockRaw.Should().Be(0f);
    }

    [Fact]
    public void GetCombatStats_SingleT1CritRing_Contributes2Percent()
    {
        // Per-tier crit rate = 2%, so T1 = 2 raw%.
        var eq = new EquipmentSet();
        eq.ForceEquip(EquipSlot.Ring, CritRing(1), ringIndex: 0);
        var stats = eq.GetCombatStats(PlayerClass.Warrior);
        stats.CritRaw.Should().BeApproximately(2f, 0.001f);
    }

    [Fact]
    public void GetCombatStats_TenT5CritRings_Produces100PercentRaw()
    {
        // Spec worked example: 10 × T5 Precision = 100% raw crit.
        var eq = new EquipmentSet();
        for (int i = 0; i < 10; i++)
            eq.ForceEquip(EquipSlot.Ring, CritRing(5), ringIndex: i);
        var stats = eq.GetCombatStats(PlayerClass.Warrior);
        stats.CritRaw.Should().BeApproximately(100f, 0.001f);
    }

    [Fact]
    public void GetCombatStats_MixedFocuses_EachAccumulatesIndependently()
    {
        var eq = new EquipmentSet();
        eq.ForceEquip(EquipSlot.Ring, CritRing(3), ringIndex: 0);   // +6% crit
        eq.ForceEquip(EquipSlot.Ring, HasteRing(2), ringIndex: 1);  // +6% haste
        var stats = eq.GetCombatStats(PlayerClass.Warrior);
        stats.CritRaw.Should().BeApproximately(6f, 0.001f);
        stats.HasteRaw.Should().BeApproximately(6f, 0.001f);
        stats.DodgeRaw.Should().Be(0f);
        stats.BlockRaw.Should().Be(0f);
    }

    [Fact]
    public void GetCombatStats_UntieredRing_ContributesNothing()
    {
        // A ring with Tier=0 shouldn't contribute any raw% even if its
        // RingFocus is set. Guards against a catalog entry forgetting Tier.
        var eq = new EquipmentSet();
        eq.ForceEquip(EquipSlot.Ring, new ItemDef
        {
            Id = "ring_t0_broken",
            Category = ItemCategory.Ring,
            Slot = EquipSlot.Ring,
            Tier = 0,
            RingFocus = RingFocus.Crit,
        }, ringIndex: 0);
        eq.GetCombatStats(PlayerClass.Warrior).CritRaw.Should().Be(0f);
    }

    // ── Cache invalidation ──────────────────────────────────────────────

    [Fact]
    public void Cache_ReturnsSameStatsAcrossReadsWhenUnchanged()
    {
        var eq = new EquipmentSet();
        eq.ForceEquip(EquipSlot.Ring, CritRing(3), ringIndex: 0);
        var first = eq.GetCombatStats(PlayerClass.Warrior);
        var second = eq.GetCombatStats(PlayerClass.Warrior);
        first.Should().BeEquivalentTo(second);
    }

    [Fact]
    public void Cache_InvalidatesOnForceEquip()
    {
        var eq = new EquipmentSet();
        var first = eq.GetCombatStats(PlayerClass.Warrior);
        first.CritRaw.Should().Be(0f);

        eq.ForceEquip(EquipSlot.Ring, CritRing(5), ringIndex: 0);
        var second = eq.GetCombatStats(PlayerClass.Warrior);
        second.CritRaw.Should().BeApproximately(10f, 0.001f);
    }

    [Fact]
    public void Cache_InvalidatesOnTryEquipFromBackpack()
    {
        var eq = new EquipmentSet();
        var bp = new Inventory(20);
        bp.TryAdd(StatStrNeck());

        eq.GetCombatStats(PlayerClass.Warrior).Str.Should().Be(0f);
        eq.TryEquip(EquipSlot.Neck, StatStrNeck(), bp).Should().BeTrue();
        eq.GetCombatStats(PlayerClass.Warrior).Str.Should().BeApproximately(5f, 0.001f);
    }

    [Fact]
    public void Cache_InvalidatesOnUnequip()
    {
        var eq = new EquipmentSet();
        eq.ForceEquip(EquipSlot.Ring, CritRing(5), ringIndex: 0);
        eq.GetCombatStats(PlayerClass.Warrior).CritRaw.Should().BeApproximately(10f, 0.001f);

        var bp = new Inventory(20);
        eq.Unequip(EquipSlot.Ring, bp, ringIndex: 0);
        eq.GetCombatStats(PlayerClass.Warrior).CritRaw.Should().Be(0f);
    }

    [Fact]
    public void Cache_InvalidatesOnRestoreState()
    {
        // Use a real catalog ring (ring_t5_crit) so RestoreState's
        // ItemDatabase.Get lookup resolves to a non-null item. Confirms
        // RestoreState calls InvalidateCache so the first read after
        // restore reflects the new state, not a stale empty cache.
        var eq1 = new EquipmentSet();
        var realT5Crit = ItemDatabase.Get("ring_t5_crit");
        realT5Crit.Should().NotBeNull("test requires ring_t5_crit in ItemDatabase");
        eq1.ForceEquip(EquipSlot.Ring, realT5Crit!, ringIndex: 0);
        eq1.GetCombatStats(PlayerClass.Warrior).CritRaw.Should().BeApproximately(10f, 0.001f);

        var data = eq1.CaptureState();

        var eq2 = new EquipmentSet();
        var preRestore = eq2.GetCombatStats(PlayerClass.Warrior);
        preRestore.CritRaw.Should().Be(0f);
        eq2.RestoreState(data);
        var postRestore = eq2.GetCombatStats(PlayerClass.Warrior);
        postRestore.CritRaw.Should().BeApproximately(10f, 0.001f);
    }

    [Fact]
    public void Cache_InvalidatesOnDestroyRandomEquipped()
    {
        var eq = new EquipmentSet();
        eq.ForceEquip(EquipSlot.Ring, CritRing(5), ringIndex: 0);
        eq.GetCombatStats(PlayerClass.Warrior).CritRaw.Should().BeApproximately(10f, 0.001f);

        eq.DestroyRandomEquipped(new Random(1));
        eq.GetCombatStats(PlayerClass.Warrior).CritRaw.Should().Be(0f);
    }

    [Fact]
    public void Cache_RebuildsWhenPlayerClassChanges()
    {
        // Future-proofing: if the player reclasses, cache should rebuild
        // so class-affinity math applies to the new class.
        var eq = new EquipmentSet();
        var warriorBody = new ItemDef
        {
            Id = "body_warrior_test",
            Name = "Warrior Body",
            Category = ItemCategory.Body,
            Slot = EquipSlot.Body,
            Tier = 1,
            BonusStr = 10,
            ClassAffinity = PlayerClass.Warrior,
        };
        eq.ForceEquip(EquipSlot.Body, warriorBody);

        var asWarrior = eq.GetCombatStats(PlayerClass.Warrior);
        var asRanger = eq.GetCombatStats(PlayerClass.Ranger);

        // Warrior affinity = 1.25× → STR 12.5; Ranger = 1.0× → STR 10.
        asWarrior.Str.Should().BeApproximately(12.5f, 0.001f);
        asRanger.Str.Should().BeApproximately(10f, 0.001f);
    }

    [Fact]
    public void InvalidateCache_ExplicitCallForcesRebuild()
    {
        var eq = new EquipmentSet();
        eq.ForceEquip(EquipSlot.Ring, CritRing(5), ringIndex: 0);
        var first = eq.GetCombatStats(PlayerClass.Warrior);

        // Simulate external dirty — user changes a field on an equipped
        // item reference after equip (shouldn't happen with records, but
        // the invalidate hook is part of the public contract).
        eq.InvalidateCache();
        var second = eq.GetCombatStats(PlayerClass.Warrior);
        second.Should().BeEquivalentTo(first); // Same state → same values.
    }
}
