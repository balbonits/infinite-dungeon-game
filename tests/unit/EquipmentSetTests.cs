using System;
using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

/// <summary>
/// Tests for EquipmentSet — SYS-11 equipment slotting and stat aggregation.
/// </summary>
public class EquipmentSetTests
{
    // ── Helpers ──────────────────────────────────────────────────────────

    private static ItemDef MakeItem(string id, EquipSlot slot, ItemCategory category,
        int str = 0, int dex = 0, int sta = 0, int @int = 0, int dmg = 0,
        PlayerClass? affinity = null) => new()
        {
            Id = id,
            Name = id,
            Category = category,
            Slot = slot,
            ClassAffinity = affinity,
            BonusStr = str,
            BonusDex = dex,
            BonusSta = sta,
            BonusInt = @int,
            BonusDamage = dmg,
        };

    private static ItemDef Helm(string id, PlayerClass? affinity = null, int sta = 0) =>
        MakeItem(id, EquipSlot.Head, ItemCategory.Head, sta: sta, affinity: affinity);
    private static ItemDef Weapon(string id, PlayerClass? affinity = null, int dmg = 0) =>
        MakeItem(id, EquipSlot.MainHand, ItemCategory.Weapon, dmg: dmg, affinity: affinity);
    private static ItemDef Ring(string id, int str = 0) =>
        MakeItem(id, EquipSlot.Ring, ItemCategory.Ring, str: str);
    private static ItemDef Quiver(string id) =>
        MakeItem(id, EquipSlot.Ammo, ItemCategory.Quiver);
    private static ItemDef Shield(string id) =>
        MakeItem(id, EquipSlot.OffHand, ItemCategory.Shield);

    // ── Defaults ─────────────────────────────────────────────────────────

    [Fact]
    public void NewEquipmentSet_IsEmpty()
    {
        var eq = new EquipmentSet();
        eq.EquippedCount.Should().Be(0);
        eq.HasQuiver().Should().BeFalse();
        eq.Rings.Should().HaveCount(EquipmentSet.RingSlotCount);
    }

    // ── IsCompatible ─────────────────────────────────────────────────────

    [Fact]
    public void IsCompatible_RespectsSlotToCategoryMapping()
    {
        EquipmentSet.IsCompatible(EquipSlot.Head, Helm("h")).Should().BeTrue();
        EquipmentSet.IsCompatible(EquipSlot.Body, Helm("h")).Should().BeFalse();
        EquipmentSet.IsCompatible(EquipSlot.MainHand, Weapon("w")).Should().BeTrue();
        EquipmentSet.IsCompatible(EquipSlot.OffHand, Shield("s")).Should().BeTrue();
        EquipmentSet.IsCompatible(EquipSlot.Ring, Ring("r")).Should().BeTrue();
        EquipmentSet.IsCompatible(EquipSlot.Ammo, Quiver("q")).Should().BeTrue();
    }

    [Fact]
    public void IsCompatible_OffHand_AcceptsShieldSpellbookAndDefensiveMelee()
    {
        var book = MakeItem("b", EquipSlot.OffHand, ItemCategory.Spellbook);
        var dagger = MakeItem("d", EquipSlot.OffHand, ItemCategory.DefensiveMelee);
        EquipmentSet.IsCompatible(EquipSlot.OffHand, book).Should().BeTrue();
        EquipmentSet.IsCompatible(EquipSlot.OffHand, dagger).Should().BeTrue();
        EquipmentSet.IsCompatible(EquipSlot.OffHand, Quiver("q")).Should().BeFalse();
    }

    // ── ForceEquip ───────────────────────────────────────────────────────

    [Fact]
    public void ForceEquip_SetsSlotAndIncrementsCount()
    {
        var eq = new EquipmentSet();
        eq.ForceEquip(EquipSlot.Head, Helm("leather_cap"));
        eq.Head.Should().NotBeNull();
        eq.EquippedCount.Should().Be(1);
    }

    [Fact]
    public void ForceEquip_IgnoresIncompatibleItem()
    {
        var eq = new EquipmentSet();
        eq.ForceEquip(EquipSlot.Head, Weapon("sword"));
        eq.Head.Should().BeNull();
    }

    [Fact]
    public void ForceEquip_RingPlacesAtRingIndex()
    {
        var eq = new EquipmentSet();
        eq.ForceEquip(EquipSlot.Ring, Ring("r"), ringIndex: 3);
        eq.Rings[3].Should().NotBeNull();
        eq.Rings[0].Should().BeNull();
    }

    [Fact]
    public void ForceEquip_RingOutOfBounds_Ignored()
    {
        var eq = new EquipmentSet();
        eq.ForceEquip(EquipSlot.Ring, Ring("r"), ringIndex: 99);
        eq.EquippedCount.Should().Be(0);
    }

    // ── TryEquip (backpack swap) ─────────────────────────────────────────

    [Fact]
    public void TryEquip_MovesItemFromBackpackToSlot()
    {
        var eq = new EquipmentSet();
        var backpack = new Inventory(4);
        var helm = Helm("leather_cap");
        backpack.TryAdd(helm);

        bool ok = eq.TryEquip(EquipSlot.Head, helm, backpack);

        ok.Should().BeTrue();
        eq.Head.Should().Be(helm);
        backpack.FindSlot(helm.Id).Should().Be(-1);
    }

    [Fact]
    public void TryEquip_ReturnsPreviousItemToBackpack()
    {
        var eq = new EquipmentSet();
        var backpack = new Inventory(4);
        var oldHelm = Helm("old");
        var newHelm = Helm("new");
        eq.ForceEquip(EquipSlot.Head, oldHelm);
        backpack.TryAdd(newHelm);

        eq.TryEquip(EquipSlot.Head, newHelm, backpack).Should().BeTrue();

        eq.Head.Should().Be(newHelm);
        backpack.FindSlot(oldHelm.Id).Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void TryEquip_RefusesIfItemNotInBackpack()
    {
        var eq = new EquipmentSet();
        var backpack = new Inventory(4);
        var helm = Helm("leather_cap");
        // helm NOT added to backpack

        eq.TryEquip(EquipSlot.Head, helm, backpack).Should().BeFalse();
        eq.Head.Should().BeNull();
    }

    [Fact]
    public void TryEquip_RefusesIfSlotIncompatible()
    {
        var eq = new EquipmentSet();
        var backpack = new Inventory(4);
        var sword = Weapon("sword");
        backpack.TryAdd(sword);

        eq.TryEquip(EquipSlot.Head, sword, backpack).Should().BeFalse();
        eq.Head.Should().BeNull();
        backpack.FindSlot(sword.Id).Should().BeGreaterThanOrEqualTo(0); // Still in backpack.
    }

    [Fact]
    public void TryEquip_RefusesIfBackpackCannotAcceptReturnedItem()
    {
        var eq = new EquipmentSet();
        // Backpack has 1 slot, filled with the new helm.
        var backpack = new Inventory(1);
        var oldHelm = Helm("old_unique");
        var newHelm = Helm("new_unique");
        eq.ForceEquip(EquipSlot.Head, oldHelm);
        backpack.TryAdd(newHelm);

        // Backpack is full with newHelm; swapping would require returning oldHelm
        // (different Id, so no merge possible). Must refuse.
        eq.TryEquip(EquipSlot.Head, newHelm, backpack).Should().BeFalse();
        eq.Head.Should().Be(oldHelm);
        backpack.FindSlot(newHelm.Id).Should().BeGreaterThanOrEqualTo(0);
    }

    // ── Unequip ──────────────────────────────────────────────────────────

    [Fact]
    public void Unequip_ReturnsItemToBackpack()
    {
        var eq = new EquipmentSet();
        var backpack = new Inventory(4);
        var helm = Helm("leather_cap");
        eq.ForceEquip(EquipSlot.Head, helm);

        var returned = eq.Unequip(EquipSlot.Head, backpack);

        returned.Should().Be(helm);
        eq.Head.Should().BeNull();
        backpack.FindSlot(helm.Id).Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void Unequip_ReturnsNullIfSlotEmpty()
    {
        var eq = new EquipmentSet();
        var backpack = new Inventory(4);
        eq.Unequip(EquipSlot.Head, backpack).Should().BeNull();
    }

    [Fact]
    public void Unequip_RefusesIfBackpackFull()
    {
        var eq = new EquipmentSet();
        var backpack = new Inventory(1);
        // Fill backpack with something unrelated so no merge possible.
        backpack.TryAdd(MakeItem("filler", EquipSlot.None, ItemCategory.Material));
        var helm = Helm("leather_cap");
        eq.ForceEquip(EquipSlot.Head, helm);

        eq.Unequip(EquipSlot.Head, backpack).Should().BeNull();
        eq.Head.Should().Be(helm); // Still equipped.
    }

    // ── HasQuiver ────────────────────────────────────────────────────────

    [Fact]
    public void HasQuiver_TrueOnlyWhenQuiverInAmmoSlot()
    {
        var eq = new EquipmentSet();
        eq.HasQuiver().Should().BeFalse();

        eq.ForceEquip(EquipSlot.Ammo, Quiver("basic"));
        eq.HasQuiver().Should().BeTrue();
    }

    // ── GetTotalBonuses ──────────────────────────────────────────────────

    [Fact]
    public void GetTotalBonuses_NoAffinity_AppliesFlatMultiplier()
    {
        var eq = new EquipmentSet();
        eq.ForceEquip(EquipSlot.Head, Helm("cap", sta: 4)); // No affinity.
        var b = eq.GetTotalBonuses(PlayerClass.Warrior);
        b.Sta.Should().Be(4f);
    }

    [Fact]
    public void GetTotalBonuses_MatchingAffinity_Applies125xMultiplier()
    {
        var eq = new EquipmentSet();
        eq.ForceEquip(EquipSlot.Head, Helm("warrior_helm", affinity: PlayerClass.Warrior, sta: 4));
        var b = eq.GetTotalBonuses(PlayerClass.Warrior);
        b.Sta.Should().Be(5f); // 4 × 1.25
    }

    [Fact]
    public void GetTotalBonuses_MismatchedAffinity_Applies1xMultiplier()
    {
        var eq = new EquipmentSet();
        eq.ForceEquip(EquipSlot.Head, Helm("mage_crown", affinity: PlayerClass.Mage, sta: 4));
        var b = eq.GetTotalBonuses(PlayerClass.Warrior);
        b.Sta.Should().Be(4f); // No bonus.
    }

    [Fact]
    public void GetTotalBonuses_SumsAcrossSlots()
    {
        var eq = new EquipmentSet();
        eq.ForceEquip(EquipSlot.Head, Helm("h", sta: 2));
        eq.ForceEquip(EquipSlot.Body, MakeItem("b", EquipSlot.Body, ItemCategory.Body, sta: 3));
        eq.ForceEquip(EquipSlot.Ring, Ring("r1", str: 1), 0);
        eq.ForceEquip(EquipSlot.Ring, Ring("r2", str: 2), 1);

        var b = eq.GetTotalBonuses(PlayerClass.Warrior);

        b.Sta.Should().Be(5f);
        b.Str.Should().Be(3f);
    }

    // ── DestroyRandomEquipped ────────────────────────────────────────────

    [Fact]
    public void DestroyRandomEquipped_RemovesExactlyOneItem()
    {
        var eq = new EquipmentSet();
        eq.ForceEquip(EquipSlot.Head, Helm("h"));
        eq.ForceEquip(EquipSlot.MainHand, Weapon("w"));
        eq.ForceEquip(EquipSlot.Ring, Ring("r"), 0);

        var destroyed = eq.DestroyRandomEquipped(new Random(0));

        destroyed.Should().NotBeNull();
        eq.EquippedCount.Should().Be(2);
    }

    [Fact]
    public void DestroyRandomEquipped_ReturnsNullWhenNothingEquipped()
    {
        new EquipmentSet().DestroyRandomEquipped(new Random(0)).Should().BeNull();
    }

    // ── Capture / Restore round-trip ─────────────────────────────────────

    [Fact]
    public void CaptureAndRestore_PreservesAllSlots()
    {
        // Use real catalog IDs (ITEM-01) so RestoreState can resolve them via ItemDatabase.
        // The previous version of this test used legacy IDs (leather_cap, vest, ...) and
        // silently returned when those weren't found, masking regressions in CaptureState /
        // RestoreState. Asserting catalog presence up-front is now part of the test contract.
        const string headId = "head_warrior_helmet_t1";
        const string bodyId = "body_warrior_armor_t1";
        const string mainId = "mainhand_warrior_sword_t1";
        const string offId = "offhand_warrior_shield_small_t1";
        const string ammoId = "ammo_quiver_basic";
        const string ringId = "ring_t1_str";

        foreach (var id in new[] { headId, bodyId, mainId, offId, ammoId, ringId })
            ItemDatabase.Get(id).Should().NotBeNull($"catalog must contain {id} for the round-trip to mean anything");

        var original = new EquipmentSet();
        original.ForceEquip(EquipSlot.Head, ItemDatabase.Get(headId)!);
        original.ForceEquip(EquipSlot.Body, ItemDatabase.Get(bodyId)!);
        original.ForceEquip(EquipSlot.MainHand, ItemDatabase.Get(mainId)!);
        original.ForceEquip(EquipSlot.OffHand, ItemDatabase.Get(offId)!);
        original.ForceEquip(EquipSlot.Ammo, ItemDatabase.Get(ammoId)!);
        original.ForceEquip(EquipSlot.Ring, ItemDatabase.Get(ringId)!, 2);
        original.ForceEquip(EquipSlot.Ring, ItemDatabase.Get(ringId)!, 7);

        var data = original.CaptureState();
        var restored = new EquipmentSet();
        restored.RestoreState(data);

        restored.Head?.Id.Should().Be(headId);
        restored.Body?.Id.Should().Be(bodyId);
        restored.MainHand?.Id.Should().Be(mainId);
        restored.OffHand?.Id.Should().Be(offId);
        restored.Ammo?.Id.Should().Be(ammoId);
        restored.Rings[2]?.Id.Should().Be(ringId);
        restored.Rings[7]?.Id.Should().Be(ringId);
    }

    [Fact]
    public void CaptureAndRestore_RoundTripsEmptyState()
    {
        var original = new EquipmentSet();
        var data = original.CaptureState();
        var restored = new EquipmentSet();
        restored.RestoreState(data);
        restored.EquippedCount.Should().Be(0);
    }
}
