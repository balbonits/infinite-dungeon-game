using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

/// <summary>
/// Tests for Bank.cs.
/// Covers expansion costs, deposits, withdrawals, and state capture/restore.
/// </summary>
public class BankTests
{
    private static ItemDef MakeItem(string id, ItemCategory cat = ItemCategory.Weapon) => new()
    {
        Id = id,
        Name = id,
        Category = cat,
        SellPrice = 10,
        BuyPrice = 50
    };

    private static ItemDef MakeConsumable(string id = "potion") => new()
    {
        Id = id,
        Name = id,
        Category = ItemCategory.Consumable,
        SellPrice = 5,
        BuyPrice = 20
    };

    // ── Initial state ─────────────────────────────────────────────────────────

    [Fact]
    public void NewBank_HasCorrectStartingSlots()
    {
        new Bank().TotalSlots.Should().Be(Bank.StartingSlots);
    }

    [Fact]
    public void NewBank_HasZeroExpansions()
    {
        new Bank().ExpansionCount.Should().Be(0);
    }

    [Fact]
    public void NewBank_StorageIsEmpty()
    {
        new Bank().Storage.UsedSlots.Should().Be(0);
    }

    // ── GetNextExpansionCost ──────────────────────────────────────────────────

    [Fact]
    public void FirstExpansionCost_Is500()
    {
        // N=1: 500 * 1^2 = 500
        new Bank().GetNextExpansionCost().Should().Be(500);
    }

    [Fact]
    public void ExpansionCost_ScalesQuadratically()
    {
        var bank = new Bank();
        var playerInv = new Inventory { Gold = 100_000 };

        bank.PurchaseExpansion(playerInv); // N=1: cost 500
        bank.GetNextExpansionCost().Should().Be(2000); // N=2: 500 * 4

        bank.PurchaseExpansion(playerInv); // N=2: cost 2000
        bank.GetNextExpansionCost().Should().Be(4500); // N=3: 500 * 9
    }

    // ── PurchaseExpansion ─────────────────────────────────────────────────────

    [Fact]
    public void PurchaseExpansion_Success_DeductsGold()
    {
        var bank = new Bank();
        var inv = new Inventory { Gold = 1000 };
        bank.PurchaseExpansion(inv);
        inv.Gold.Should().Be(500); // 1000 - 500
    }

    [Fact]
    public void PurchaseExpansion_Success_IncreasesSlots()
    {
        var bank = new Bank();
        var inv = new Inventory { Gold = 1000 };
        bank.PurchaseExpansion(inv);
        bank.TotalSlots.Should().Be(Bank.StartingSlots + Bank.SlotsPerExpansion);
    }

    [Fact]
    public void PurchaseExpansion_Success_IncrementsExpansionCount()
    {
        var bank = new Bank();
        var inv = new Inventory { Gold = 1000 };
        bank.PurchaseExpansion(inv);
        bank.ExpansionCount.Should().Be(1);
    }

    [Fact]
    public void PurchaseExpansion_NotEnoughGold_ReturnsFalse()
    {
        var bank = new Bank();
        var inv = new Inventory { Gold = 100 }; // need 500
        bank.PurchaseExpansion(inv).Should().BeFalse();
    }

    [Fact]
    public void PurchaseExpansion_NotEnoughGold_DoesNotChangeSlots()
    {
        var bank = new Bank();
        var inv = new Inventory { Gold = 100 };
        bank.PurchaseExpansion(inv);
        bank.TotalSlots.Should().Be(Bank.StartingSlots);
    }

    [Fact]
    public void PurchaseExpansion_PreservesExistingItems()
    {
        var bank = new Bank();
        var inv = new Inventory { Gold = 100_000 };
        var sword = MakeItem("sword");
        inv.TryAdd(sword);
        bank.Deposit(inv, 0);

        bank.PurchaseExpansion(inv);
        bank.Storage.UsedSlots.Should().Be(1);
    }

    // ── Deposit ───────────────────────────────────────────────────────────────

    [Fact]
    public void Deposit_Success_MovesItemToBank()
    {
        var bank = new Bank();
        var inv = new Inventory();
        inv.TryAdd(MakeItem("sword"));

        bank.Deposit(inv, 0).Should().BeTrue();
        bank.Storage.UsedSlots.Should().Be(1);
        inv.UsedSlots.Should().Be(0);
    }

    [Fact]
    public void Deposit_EmptySlot_ReturnsFalse()
    {
        var bank = new Bank();
        var inv = new Inventory();
        bank.Deposit(inv, 0).Should().BeFalse();
    }

    [Fact]
    public void Deposit_BankFull_ReturnsFalse()
    {
        var bank = new Bank();
        var inv = new Inventory { Gold = 0 };

        // Fill bank completely
        for (int i = 0; i < Bank.StartingSlots; i++)
        {
            var item = MakeItem($"sword_{i}");
            inv.TryAdd(item);
            bank.Deposit(inv, 0);
        }

        // Next deposit should fail
        inv.TryAdd(MakeItem("extra_sword"));
        bank.Deposit(inv, 0).Should().BeFalse();
    }

    // ── Withdraw ──────────────────────────────────────────────────────────────

    [Fact]
    public void Withdraw_Success_MovesItemToBackpack()
    {
        var bank = new Bank();
        var inv = new Inventory();
        inv.TryAdd(MakeItem("sword"));
        bank.Deposit(inv, 0);

        bank.Withdraw(inv, 0).Should().BeTrue();
        inv.UsedSlots.Should().Be(1);
        bank.Storage.UsedSlots.Should().Be(0);
    }

    [Fact]
    public void Withdraw_EmptyBankSlot_ReturnsFalse()
    {
        var bank = new Bank();
        var inv = new Inventory();
        bank.Withdraw(inv, 0).Should().BeFalse();
    }

    [Fact]
    public void Withdraw_BackpackFull_ReturnsFalse()
    {
        var bank = new Bank();
        var inv = new Inventory(1);
        var sword = MakeItem("sword");
        var shield = MakeItem("shield");

        inv.TryAdd(sword);
        bank.Storage.TryAdd(shield);

        bank.Withdraw(inv, 0).Should().BeFalse();
        bank.Storage.UsedSlots.Should().Be(1); // still in bank
    }

    // ── CaptureState / RestoreState ───────────────────────────────────────────

    [Fact]
    public void CaptureState_ReflectsExpansionCount()
    {
        var bank = new Bank();
        var inv = new Inventory { Gold = 10_000 };
        bank.PurchaseExpansion(inv);

        var state = bank.CaptureState();
        state.ExpansionCount.Should().Be(1);
    }

    [Fact]
    public void CaptureState_EmptyBank_HasNoItems()
    {
        var state = new Bank().CaptureState();
        state.Items.Should().BeEmpty();
    }
}
