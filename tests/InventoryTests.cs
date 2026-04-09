using Xunit;

namespace DungeonGame.Tests;

public class InventoryTests
{
    public InventoryTests()
    {
        GameState.Reset();
    }

    [Fact]
    public void AddToInventory_AddsItem()
    {
        var item = GameSystems.CreateItem("Sword", ItemType.Weapon, EquipSlot.MainHand);
        bool added = GameSystems.AddToInventory(item);

        Assert.True(added);
        Assert.Single(GameState.Player.Inventory);
    }

    [Fact]
    public void AddToInventory_StacksConsumables()
    {
        var potion1 = GameSystems.CreateItem("Potion", ItemType.Consumable, EquipSlot.None, hpBonus: 30, stackable: true);
        var potion2 = GameSystems.CreateItem("Potion", ItemType.Consumable, EquipSlot.None, hpBonus: 30, stackable: true);

        GameSystems.AddToInventory(potion1);
        GameSystems.AddToInventory(potion2);

        Assert.Single(GameState.Player.Inventory);
        Assert.Equal(2, GameState.Player.Inventory[0].StackCount);
    }

    [Fact]
    public void AddToInventory_FailsWhenFull()
    {
        GameState.Player.InventorySize = 2;
        GameSystems.AddToInventory(GameSystems.CreateItem("A", ItemType.Material, EquipSlot.None));
        GameSystems.AddToInventory(GameSystems.CreateItem("B", ItemType.Material, EquipSlot.None));

        var third = GameSystems.CreateItem("C", ItemType.Material, EquipSlot.None);
        bool added = GameSystems.AddToInventory(third);

        Assert.False(added);
        Assert.Equal(2, GameState.Player.Inventory.Count);
    }

    [Fact]
    public void EquipItem_MovesToSlot()
    {
        var sword = GameSystems.CreateItem("Sword", ItemType.Weapon, EquipSlot.MainHand, damage: 8);
        GameSystems.AddToInventory(sword);

        var (success, previous) = GameSystems.EquipItem(sword);

        Assert.True(success);
        Assert.Null(previous);
        Assert.Empty(GameState.Player.Inventory);
        Assert.True(GameState.Player.Equipment.ContainsKey(EquipSlot.MainHand));
    }

    [Fact]
    public void EquipItem_SwapsPrevious()
    {
        var sword1 = GameSystems.CreateItem("Iron Sword", ItemType.Weapon, EquipSlot.MainHand, damage: 5);
        var sword2 = GameSystems.CreateItem("Steel Sword", ItemType.Weapon, EquipSlot.MainHand, damage: 10);
        GameSystems.AddToInventory(sword1);
        GameSystems.EquipItem(sword1);
        GameSystems.AddToInventory(sword2);

        var (success, previous) = GameSystems.EquipItem(sword2);

        Assert.True(success);
        Assert.NotNull(previous);
        Assert.Equal("Iron Sword", previous.Name);
        Assert.Single(GameState.Player.Inventory); // old sword back in bag
    }

    [Fact]
    public void EquipItem_AppliesHpBonus()
    {
        int baseMhp = GameState.Player.MaxHP;
        var ring = GameSystems.CreateItem("Ring", ItemType.Accessory, EquipSlot.Ring, hpBonus: 15);
        GameSystems.AddToInventory(ring);
        GameSystems.EquipItem(ring);

        Assert.Equal(baseMhp + 15, GameState.Player.MaxHP);
    }

    [Fact]
    public void UnequipItem_RemovesBonuses()
    {
        int baseMhp = GameState.Player.MaxHP;
        var ring = GameSystems.CreateItem("Ring", ItemType.Accessory, EquipSlot.Ring, hpBonus: 15);
        GameSystems.AddToInventory(ring);
        GameSystems.EquipItem(ring);
        Assert.Equal(baseMhp + 15, GameState.Player.MaxHP);

        GameSystems.UnequipItem(EquipSlot.Ring);
        Assert.Equal(baseMhp, GameState.Player.MaxHP);
    }

    [Fact]
    public void UseItem_RestoresHp()
    {
        GameState.Player.HP = 50;
        var potion = GameSystems.CreateItem("Potion", ItemType.Consumable, EquipSlot.None, hpBonus: 30, stackable: true);
        GameSystems.AddToInventory(potion);

        var (success, effect) = GameSystems.UseItem(potion);

        Assert.True(success);
        Assert.Equal(80, GameState.Player.HP);
        Assert.Contains("Restored 30 HP", effect);
    }

    [Fact]
    public void UseItem_RemovesFromInventoryWhenDepleted()
    {
        var potion = GameSystems.CreateItem("Potion", ItemType.Consumable, EquipSlot.None, hpBonus: 10, stackable: true);
        potion.StackCount = 1;
        GameSystems.AddToInventory(potion);

        GameState.Player.HP = 50;
        GameSystems.UseItem(potion);

        Assert.Empty(GameState.Player.Inventory);
    }

    [Fact]
    public void UseItem_FailsOnNonConsumable()
    {
        var sword = GameSystems.CreateItem("Sword", ItemType.Weapon, EquipSlot.MainHand);
        GameSystems.AddToInventory(sword);

        var (success, _) = GameSystems.UseItem(sword);

        Assert.False(success);
    }
}
