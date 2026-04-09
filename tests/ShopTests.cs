using Xunit;

namespace DungeonGame.Tests;

public class ShopTests
{
    public ShopTests()
    {
        GameState.Reset();
        GameState.Player.Gold = 100;
    }

    [Fact]
    public void BuyItem_DeductsGold()
    {
        var item = GameSystems.CreateItem("Sword", ItemType.Weapon, EquipSlot.MainHand, value: 50);

        var (success, _) = GameSystems.BuyItem(item);

        Assert.True(success);
        Assert.Equal(50, GameState.Player.Gold);
        Assert.Single(GameState.Player.Inventory);
    }

    [Fact]
    public void BuyItem_FailsWithoutGold()
    {
        GameState.Player.Gold = 10;
        var item = GameSystems.CreateItem("Expensive", ItemType.Weapon, EquipSlot.MainHand, value: 500);

        var (success, reason) = GameSystems.BuyItem(item);

        Assert.False(success);
        Assert.Contains("Not enough gold", reason);
        Assert.Equal(10, GameState.Player.Gold); // gold unchanged
        Assert.Empty(GameState.Player.Inventory);
    }

    [Fact]
    public void BuyItem_MultipleCopies()
    {
        var potion = GameSystems.CreateItem("Potion", ItemType.Consumable, EquipSlot.None, hpBonus: 30, value: 10, stackable: true);

        var (success, _) = GameSystems.BuyItem(potion, 3);

        Assert.True(success);
        Assert.Equal(70, GameState.Player.Gold);
        Assert.Single(GameState.Player.Inventory);
        Assert.Equal(3, GameState.Player.Inventory[0].StackCount);
    }

    [Fact]
    public void SellItem_GivesHalfValue()
    {
        var item = GameSystems.CreateItem("Sword", ItemType.Weapon, EquipSlot.MainHand, value: 40);
        GameSystems.AddToInventory(item);

        var (gold, _) = GameSystems.SellItem(item);

        Assert.Equal(20, gold);
        Assert.Equal(120, GameState.Player.Gold); // 100 start + 20 sell
        Assert.Empty(GameState.Player.Inventory);
    }

    [Fact]
    public void SellItem_MinimumOneGold()
    {
        var junk = GameSystems.CreateItem("Junk", ItemType.Material, EquipSlot.None, value: 1);
        GameSystems.AddToInventory(junk);

        var (gold, _) = GameSystems.SellItem(junk);

        Assert.True(gold >= 1);
    }
}
