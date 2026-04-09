using Xunit;

namespace DungeonGame.Tests;

public class BankTests
{
    private readonly BankData _bank;

    public BankTests()
    {
        GameState.Reset();
        _bank = new BankData();
    }

    // ---------- DEPOSIT ----------

    [Fact]
    public void Deposit_MovesItemFromInventoryToBank()
    {
        var sword = GameSystems.CreateItem("Sword", ItemType.Weapon, EquipSlot.MainHand);
        GameSystems.AddToInventory(sword);

        var (success, _) = BankSystem.Deposit(_bank, GameState.Player, sword);

        Assert.True(success);
        Assert.Empty(GameState.Player.Inventory);
        Assert.Single(_bank.Items);
        Assert.Equal("Sword", _bank.Items[0].Name);
    }

    [Fact]
    public void Deposit_StacksConsumables()
    {
        var potion1 = GameSystems.CreateItem("Potion", ItemType.Consumable, EquipSlot.None, hpBonus: 30, stackable: true);
        potion1.StackCount = 3;
        _bank.Items.Add(potion1);

        var potion2 = GameSystems.CreateItem("Potion", ItemType.Consumable, EquipSlot.None, hpBonus: 30, stackable: true);
        potion2.StackCount = 2;
        GameSystems.AddToInventory(potion2);

        var (success, message) = BankSystem.Deposit(_bank, GameState.Player, potion2);

        Assert.True(success);
        Assert.Contains("stacked", message);
        Assert.Single(_bank.Items);
        Assert.Equal(5, _bank.Items[0].StackCount);
        Assert.Empty(GameState.Player.Inventory);
    }

    [Fact]
    public void Deposit_FailsWhenBankFull()
    {
        _bank.MaxSlots = 1;
        _bank.Items.Add(GameSystems.CreateItem("Existing", ItemType.Material, EquipSlot.None));

        var item = GameSystems.CreateItem("New", ItemType.Material, EquipSlot.None);
        GameSystems.AddToInventory(item);

        var (success, message) = BankSystem.Deposit(_bank, GameState.Player, item);

        Assert.False(success);
        Assert.Contains("full", message);
        Assert.Single(GameState.Player.Inventory); // item still in inventory
    }

    [Fact]
    public void Deposit_FailsIfItemNotInInventory()
    {
        var item = GameSystems.CreateItem("Ghost", ItemType.Material, EquipSlot.None);

        var (success, _) = BankSystem.Deposit(_bank, GameState.Player, item);

        Assert.False(success);
    }

    // ---------- WITHDRAW ----------

    [Fact]
    public void Withdraw_MovesItemFromBankToInventory()
    {
        var sword = GameSystems.CreateItem("Sword", ItemType.Weapon, EquipSlot.MainHand);
        _bank.Items.Add(sword);

        var (success, _) = BankSystem.Withdraw(_bank, GameState.Player, sword);

        Assert.True(success);
        Assert.Empty(_bank.Items);
        Assert.Single(GameState.Player.Inventory);
        Assert.Equal("Sword", GameState.Player.Inventory[0].Name);
    }

    [Fact]
    public void Withdraw_StacksConsumablesInInventory()
    {
        var potion1 = GameSystems.CreateItem("Potion", ItemType.Consumable, EquipSlot.None, hpBonus: 30, stackable: true);
        potion1.StackCount = 2;
        GameSystems.AddToInventory(potion1);

        var potion2 = GameSystems.CreateItem("Potion", ItemType.Consumable, EquipSlot.None, hpBonus: 30, stackable: true);
        potion2.StackCount = 3;
        _bank.Items.Add(potion2);

        var (success, message) = BankSystem.Withdraw(_bank, GameState.Player, potion2);

        Assert.True(success);
        Assert.Contains("stacked", message);
        Assert.Single(GameState.Player.Inventory);
        Assert.Equal(5, GameState.Player.Inventory[0].StackCount);
        Assert.Empty(_bank.Items);
    }

    [Fact]
    public void Withdraw_FailsWhenInventoryFull()
    {
        GameState.Player.InventorySize = 1;
        GameSystems.AddToInventory(GameSystems.CreateItem("Filler", ItemType.Material, EquipSlot.None));

        var sword = GameSystems.CreateItem("Sword", ItemType.Weapon, EquipSlot.MainHand);
        _bank.Items.Add(sword);

        var (success, message) = BankSystem.Withdraw(_bank, GameState.Player, sword);

        Assert.False(success);
        Assert.Contains("full", message);
        Assert.Single(_bank.Items); // item still in bank
    }

    // ---------- EXPAND ----------

    [Fact]
    public void Expand_IncreasesSlotsByTen()
    {
        GameState.Player.Gold = 10000;
        int before = _bank.MaxSlots;

        var (success, _) = BankSystem.Expand(_bank, GameState.Player);

        Assert.True(success);
        Assert.Equal(before + 10, _bank.MaxSlots);
    }

    [Fact]
    public void Expand_FirstCosts500()
    {
        int cost = BankSystem.GetExpansionCost(_bank);
        Assert.Equal(500, cost); // 500 * 1^2
    }

    [Fact]
    public void Expand_DeductsCorrectGold()
    {
        GameState.Player.Gold = 1000;

        var (success, _) = BankSystem.Expand(_bank, GameState.Player);

        Assert.True(success);
        Assert.Equal(500, GameState.Player.Gold); // 1000 - 500
    }

    [Fact]
    public void Expand_FailsWithoutEnoughGold()
    {
        GameState.Player.Gold = 100;

        var (success, message) = BankSystem.Expand(_bank, GameState.Player);

        Assert.False(success);
        Assert.Contains("Not enough gold", message);
        Assert.Equal(50, _bank.MaxSlots); // unchanged
        Assert.Equal(100, GameState.Player.Gold); // unchanged
    }

    [Fact]
    public void Expand_MultipleExpansionsCostScales()
    {
        // Expansion cost: 500 * N^2 where N is the next expansion number
        // 1st: 500*1=500, 2nd: 500*4=2000, 3rd: 500*9=4500
        GameState.Player.Gold = 50000;

        BankSystem.Expand(_bank, GameState.Player);
        Assert.Equal(60, _bank.MaxSlots);
        Assert.Equal(49500, GameState.Player.Gold); // 50000 - 500

        BankSystem.Expand(_bank, GameState.Player);
        Assert.Equal(70, _bank.MaxSlots);
        Assert.Equal(47500, GameState.Player.Gold); // 49500 - 2000

        BankSystem.Expand(_bank, GameState.Player);
        Assert.Equal(80, _bank.MaxSlots);
        Assert.Equal(43000, GameState.Player.Gold); // 47500 - 4500
    }

    // ---------- HELPERS ----------

    [Fact]
    public void IsFull_ReturnsTrueWhenAtCapacity()
    {
        _bank.MaxSlots = 2;
        _bank.Items.Add(GameSystems.CreateItem("A", ItemType.Material, EquipSlot.None));
        _bank.Items.Add(GameSystems.CreateItem("B", ItemType.Material, EquipSlot.None));

        Assert.True(BankSystem.IsFull(_bank));
    }

    [Fact]
    public void IsFull_ReturnsFalseWhenSlotsAvailable()
    {
        Assert.False(BankSystem.IsFull(_bank)); // 50 slots, 0 items
    }

    [Fact]
    public void GetExpansionCost_MatchesSpecTable()
    {
        // Spec table: N=1 -> 500, N=2 -> 2000 (500*4), N=3 -> 4500 (500*9), N=4 -> 8000 (500*16)
        Assert.Equal(500, BankSystem.GetExpansionCost(_bank));   // N=1

        _bank.ExpansionCount = 1;
        Assert.Equal(2000, BankSystem.GetExpansionCost(_bank));  // N=2

        _bank.ExpansionCount = 2;
        Assert.Equal(4500, BankSystem.GetExpansionCost(_bank));  // N=3

        _bank.ExpansionCount = 3;
        Assert.Equal(8000, BankSystem.GetExpansionCost(_bank));  // N=4
    }
}
