using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Integration;

/// <summary>
/// Integration tests: DeathPenalty + Inventory + Bank working together.
/// Tests full death flows as a player would experience them.
/// </summary>
public class DeathFlowTests
{
    private static ItemDef MakeItem(string id) => new()
    {
        Id = id,
        Name = id,
        Category = ItemCategory.Weapon,
        SellPrice = 20,
        BuyPrice = 80
    };

    private static ItemDef SacrificialIdol => new()
    {
        Id = "idol_sacrificial",
        Name = "Sacrificial Idol",
        Category = ItemCategory.Consumable,
        BuyPrice = 200,
        SellPrice = 80,
    };

    // ── Full death penalty flow ───────────────────────────────────────────────

    [Fact]
    public void DeathOnFloor10_LosesCorrectXpAndItems()
    {
        var inv = new Inventory();
        for (int i = 0; i < 5; i++)
            inv.TryAdd(MakeItem($"item_{i}"));

        int startXp = 1000;
        int itemsToLose = DeathPenalty.GetItemsLost(10);  // floor 10 → 2
        int xpLoss = DeathPenalty.CalculateXpLoss(startXp, 10); // 4% → 40

        DeathPenalty.ApplyItemLoss(inv, itemsToLose);

        inv.UsedSlots.Should().Be(3);
        xpLoss.Should().Be(40);
    }

    [Fact]
    public void SacrificialIdol_PreventsItemLoss_WhenConsumed()
    {
        var inv = new Inventory();
        inv.TryAdd(SacrificialIdol);
        for (int i = 0; i < 3; i++)
            inv.TryAdd(MakeItem($"item_{i}"));

        bool hasIdol = DeathPenalty.HasSacrificialIdol(inv);
        if (hasIdol)
        {
            DeathPenalty.ConsumeSacrificialIdol(inv);
            // skip item loss — idol absorbed it
        }
        else
        {
            DeathPenalty.ApplyItemLoss(inv, DeathPenalty.GetItemsLost(5));
        }

        // Idol consumed, 3 items remain
        DeathPenalty.HasSacrificialIdol(inv).Should().BeFalse();
        inv.UsedSlots.Should().Be(3);
    }

    // ── Bank protects items on death ──────────────────────────────────────────

    [Fact]
    public void ItemsInBank_AreNotLostOnDeath()
    {
        var bank = new Bank();
        var backpack = new Inventory();

        // Deposit valuable items into bank before dying
        for (int i = 0; i < 3; i++)
        {
            backpack.TryAdd(MakeItem($"valuable_{i}"));
            bank.Deposit(backpack, 0);
        }

        // Die and lose backpack items (backpack is empty)
        DeathPenalty.ApplyItemLoss(backpack, DeathPenalty.GetItemsLost(20));

        // Bank items untouched
        bank.Storage.UsedSlots.Should().Be(3);
    }

    // ── Gold interactions ─────────────────────────────────────────────────────

    [Fact]
    public void PlayerCannotAffordExpProtection_OnLowFloor_IfBroke()
    {
        var inv = new Inventory { Gold = 0 };
        int cost = DeathPenalty.GetExpProtectionCost(1); // 15 gold
        inv.CanAfford(cost).Should().BeFalse();
    }

    [Fact]
    public void PlayerCanAffordExpProtection_WithEnoughGold()
    {
        var inv = new Inventory { Gold = 500 };
        int cost = DeathPenalty.GetExpProtectionCost(10); // 150 gold
        inv.CanAfford(cost).Should().BeTrue();
    }

    // ── Bank expansion before death ───────────────────────────────────────────

    [Fact]
    public void ExpandingBank_ThenDying_ItemsInBankSurvive()
    {
        var bank = new Bank();
        var backpack = new Inventory { Gold = 10_000 };

        bank.PurchaseExpansion(backpack);
        bank.TotalSlots.Should().Be(Bank.StartingSlots + Bank.SlotsPerExpansion);

        // Store items in bank
        backpack.TryAdd(MakeItem("heirloom"));
        bank.Deposit(backpack, 0);

        // Simulate death — backpack cleared
        backpack.Clear();
        DeathPenalty.ApplyItemLoss(backpack, 5);

        // Bank still intact
        bank.Storage.UsedSlots.Should().Be(1);
    }
}

/// <summary>
/// Integration tests: StatBlock + PlayerClass leveling.
/// </summary>
public class StatProgressionTests
{
    [Fact]
    public void Warrior_After10Levels_HasCorrectStats()
    {
        var stats = new StatBlock();
        for (int i = 0; i < 10; i++)
            stats.ApplyClassLevelBonus(PlayerClass.Warrior);

        stats.Str.Should().Be(30);
        stats.Sta.Should().Be(20);
        stats.Dex.Should().Be(0);
        stats.FreePoints.Should().Be(30);
    }

    [Fact]
    public void Mage_After10Levels_HasHigherSpellDamage_ThanWarrior()
    {
        var mage = new StatBlock();
        var warrior = new StatBlock();

        for (int i = 0; i < 10; i++)
        {
            mage.ApplyClassLevelBonus(PlayerClass.Mage);
            warrior.ApplyClassLevelBonus(PlayerClass.Warrior);
        }

        mage.SpellDamageMultiplier.Should().BeGreaterThan(warrior.SpellDamageMultiplier);
    }

    [Fact]
    public void Ranger_After10Levels_HasHigherDodge_ThanWarrior()
    {
        var ranger = new StatBlock();
        var warrior = new StatBlock();

        for (int i = 0; i < 10; i++)
        {
            ranger.ApplyClassLevelBonus(PlayerClass.Ranger);
            warrior.ApplyClassLevelBonus(PlayerClass.Warrior);
        }

        ranger.DodgeChance.Should().BeGreaterThan(warrior.DodgeChance);
    }
}

/// <summary>
/// Integration tests: Inventory buy/sell economy flow.
/// </summary>
public class EconomyFlowTests
{
    [Fact]
    public void BuyThenSell_ResultsInGoldLoss_DueToPriceDifference()
    {
        var inv = new Inventory { Gold = 1000 };
        var potion = new ItemDef
        {
            Id = "potion",
            Name = "Potion",
            Category = ItemCategory.Consumable,
            BuyPrice = 25,
            SellPrice = 10,
        };

        inv.TryBuy(potion);
        inv.TrySell(0);

        inv.Gold.Should().Be(985); // lost 15 gold to the spread
    }

    [Fact]
    public void FullInventory_ThenSellAll_RestoresGoldAboveZero()
    {
        // Spec change (docs/inventory/items.md): one type per slot per storage, unlimited stacking.
        // Buying 5 of the same item goes into ONE slot with Count=5. To exercise full-inventory
        // behavior, use 5 distinct item IDs.
        int slots = 5;
        var inv = new Inventory(slots) { Gold = 500 };

        ItemDef MakeSword(int idx) => new()
        {
            Id = $"sword_{idx}",
            Name = $"Sword {idx}",
            Category = ItemCategory.Weapon,
            BuyPrice = 100,
            SellPrice = 40,
        };

        for (int i = 0; i < slots; i++)
            inv.TryBuy(MakeSword(i));

        inv.UsedSlots.Should().Be(slots);

        for (int i = slots - 1; i >= 0; i--)
            inv.TrySell(i);

        inv.Gold.Should().BePositive();
        inv.UsedSlots.Should().Be(0);
    }
}
