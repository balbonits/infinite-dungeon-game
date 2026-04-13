using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

/// <summary>
/// Tests for Inventory.cs.
/// Covers adding, stacking, removing, buying, selling, and edge cases.
/// </summary>
public class InventoryTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ItemDef MakeConsumable(string id = "potion", int buyPrice = 25, int sellPrice = 10) => new()
    {
        Id = id,
        Name = id,
        Category = ItemCategory.Consumable,
        BuyPrice = buyPrice,
        SellPrice = sellPrice,
    };

    private static ItemDef MakeWeapon(string id = "sword", int buyPrice = 100, int sellPrice = 40) => new()
    {
        Id = id,
        Name = id,
        Category = ItemCategory.Weapon,
        BuyPrice = buyPrice,
        SellPrice = sellPrice,
    };

    private static ItemDef MakeMaterial(string id = "ore") => new()
    {
        Id = id,
        Name = id,
        Category = ItemCategory.Material,
        SellPrice = 5,
    };

    // ── Constructor / defaults ────────────────────────────────────────────────

    [Fact]
    public void NewInventory_HasCorrectSlotCount()
    {
        var inv = new Inventory(25);
        inv.SlotCount.Should().Be(25);
    }

    [Fact]
    public void NewInventory_HasZeroUsedSlots()
    {
        new Inventory().UsedSlots.Should().Be(0);
    }

    [Fact]
    public void NewInventory_HasZeroGold()
    {
        new Inventory().Gold.Should().Be(0);
    }

    [Fact]
    public void NewInventory_AllSlotsAreEmpty()
    {
        var inv = new Inventory(5);
        for (int i = 0; i < 5; i++)
            inv.GetSlot(i).Should().BeNull();
    }

    // ── TryAdd ───────────────────────────────────────────────────────────────

    [Fact]
    public void TryAdd_ToEmptySlot_ReturnsTrue()
    {
        var inv = new Inventory();
        inv.TryAdd(MakeWeapon()).Should().BeTrue();
    }

    [Fact]
    public void TryAdd_IncreasesUsedSlots()
    {
        var inv = new Inventory();
        inv.TryAdd(MakeWeapon());
        inv.UsedSlots.Should().Be(1);
    }

    [Fact]
    public void TryAdd_Consumable_StacksIntoExistingSlot()
    {
        var inv = new Inventory();
        var potion = MakeConsumable("potion");
        inv.TryAdd(potion, 1);
        inv.TryAdd(potion, 1);
        inv.UsedSlots.Should().Be(1); // still 1 slot used
        inv.GetSlot(0)!.Count.Should().Be(2);
    }

    [Fact]
    public void TryAdd_Weapon_DoesNotStack()
    {
        var inv = new Inventory();
        var sword = MakeWeapon("sword");
        inv.TryAdd(sword);
        inv.TryAdd(sword);
        inv.UsedSlots.Should().Be(2); // weapons don't stack
    }

    [Fact]
    public void TryAdd_WhenFull_ReturnsFalse()
    {
        var inv = new Inventory(1);
        inv.TryAdd(MakeWeapon("sword1"));
        inv.TryAdd(MakeWeapon("sword2")).Should().BeFalse();
    }

    [Fact]
    public void TryAdd_Material_StacksLikeConsumable()
    {
        var inv = new Inventory();
        var ore = MakeMaterial("iron_ore");
        inv.TryAdd(ore, 10);
        inv.TryAdd(ore, 5);
        inv.GetSlot(0)!.Count.Should().Be(15);
        inv.UsedSlots.Should().Be(1);
    }

    [Fact]
    public void TryAdd_StackOverflow_UsesNextSlot()
    {
        var inv = new Inventory(2);
        var potion = MakeConsumable("potion");
        inv.TryAdd(potion, 99); // fills slot 0 to max
        inv.TryAdd(potion, 1);  // should spill to slot 1
        inv.UsedSlots.Should().Be(2);
    }

    // ── RemoveAt ─────────────────────────────────────────────────────────────

    [Fact]
    public void RemoveAt_EmptySlot_ReturnsNull()
    {
        var inv = new Inventory();
        inv.RemoveAt(0).Should().BeNull();
    }

    [Fact]
    public void RemoveAt_FullStack_ClearsSlot()
    {
        var inv = new Inventory();
        inv.TryAdd(MakeWeapon());
        inv.RemoveAt(0);
        inv.GetSlot(0).Should().BeNull();
        inv.UsedSlots.Should().Be(0);
    }

    [Fact]
    public void RemoveAt_PartialCount_LeavesRemainder()
    {
        var inv = new Inventory();
        var potion = MakeConsumable("potion");
        inv.TryAdd(potion, 5);
        var removed = inv.RemoveAt(0, 3);
        removed!.Count.Should().Be(3);
        inv.GetSlot(0)!.Count.Should().Be(2);
    }

    [Fact]
    public void RemoveAt_ReturnsRemovedStack()
    {
        var inv = new Inventory();
        var sword = MakeWeapon("sword");
        inv.TryAdd(sword);
        var removed = inv.RemoveAt(0);
        removed!.Item.Id.Should().Be("sword");
    }

    // ── CanAfford / Gold ──────────────────────────────────────────────────────

    [Fact]
    public void CanAfford_WhenGoldEquals_ReturnsTrue()
    {
        var inv = new Inventory { Gold = 100 };
        inv.CanAfford(100).Should().BeTrue();
    }

    [Fact]
    public void CanAfford_WhenGoldLess_ReturnsFalse()
    {
        var inv = new Inventory { Gold = 50 };
        inv.CanAfford(100).Should().BeFalse();
    }

    // ── TryBuy ────────────────────────────────────────────────────────────────

    [Fact]
    public void TryBuy_Success_DeductsGoldAndAddsItem()
    {
        var inv = new Inventory { Gold = 100 };
        var item = MakeConsumable("potion", buyPrice: 25);
        inv.TryBuy(item).Should().BeTrue();
        inv.Gold.Should().Be(75);
        inv.UsedSlots.Should().Be(1);
    }

    [Fact]
    public void TryBuy_NotEnoughGold_ReturnsFalse()
    {
        var inv = new Inventory { Gold = 10 };
        inv.TryBuy(MakeConsumable("potion", buyPrice: 25)).Should().BeFalse();
        inv.Gold.Should().Be(10); // unchanged
    }

    [Fact]
    public void TryBuy_InventoryFull_ReturnsFalse()
    {
        var inv = new Inventory(1) { Gold = 1000 };
        inv.TryAdd(MakeWeapon("sword1"));
        inv.TryBuy(MakeWeapon("sword2", buyPrice: 100)).Should().BeFalse();
        inv.Gold.Should().Be(1000); // gold not deducted
    }

    // ── TrySell ───────────────────────────────────────────────────────────────

    [Fact]
    public void TrySell_Success_AddsGoldAndClearsSlot()
    {
        var inv = new Inventory();
        inv.TryAdd(MakeWeapon("sword", sellPrice: 40));
        inv.TrySell(0).Should().BeTrue();
        inv.Gold.Should().Be(40);
        inv.GetSlot(0).Should().BeNull();
    }

    [Fact]
    public void TrySell_EmptySlot_ReturnsFalse()
    {
        var inv = new Inventory();
        inv.TrySell(0).Should().BeFalse();
    }

    // ── Clear ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Clear_RemovesAllItems()
    {
        var inv = new Inventory();
        inv.TryAdd(MakeWeapon("sword"));
        inv.TryAdd(MakeConsumable("potion"));
        inv.Clear();
        inv.UsedSlots.Should().Be(0);
        inv.GetSlot(0).Should().BeNull();
        inv.GetSlot(1).Should().BeNull();
    }

    [Fact]
    public void Clear_DoesNotResetGold()
    {
        var inv = new Inventory { Gold = 500 };
        inv.Clear();
        inv.Gold.Should().Be(500);
    }
}
