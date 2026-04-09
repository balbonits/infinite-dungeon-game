using Xunit;

namespace DungeonGame.Tests;

public class BackpackTests
{
    public BackpackTests()
    {
        GameState.Reset();
    }

    // ---------- EXPAND ----------

    [Fact]
    public void Expand_IncreasesInventorySizeByFive()
    {
        GameState.Player.Gold = 10000;
        int before = GameState.Player.InventorySize;

        var (success, _) = BackpackSystem.Expand(GameState.Player);

        Assert.True(success);
        Assert.Equal(before + 5, GameState.Player.InventorySize);
    }

    [Fact]
    public void Expand_FirstCosts300()
    {
        int cost = BackpackSystem.GetExpansionCost(GameState.Player);
        Assert.Equal(300, cost); // 300 * 1^2
    }

    [Fact]
    public void Expand_DeductsCorrectGold()
    {
        GameState.Player.Gold = 1000;

        var (success, _) = BackpackSystem.Expand(GameState.Player);

        Assert.True(success);
        Assert.Equal(700, GameState.Player.Gold); // 1000 - 300
    }

    [Fact]
    public void Expand_FailsWithoutEnoughGold()
    {
        GameState.Player.Gold = 100;

        var (success, message) = BackpackSystem.Expand(GameState.Player);

        Assert.False(success);
        Assert.Contains("Not enough gold", message);
        Assert.Equal(25, GameState.Player.InventorySize); // unchanged
        Assert.Equal(100, GameState.Player.Gold); // unchanged
    }

    [Fact]
    public void Expand_MultipleExpansionsCostScales()
    {
        // Expansion cost: 300 * N^2 where N is the next expansion number
        // 1st: 300*1=300, 2nd: 300*4=1200, 3rd: 300*9=2700
        GameState.Player.Gold = 50000;

        BackpackSystem.Expand(GameState.Player);
        Assert.Equal(30, GameState.Player.InventorySize);
        Assert.Equal(49700, GameState.Player.Gold); // 50000 - 300

        BackpackSystem.Expand(GameState.Player);
        Assert.Equal(35, GameState.Player.InventorySize);
        Assert.Equal(48500, GameState.Player.Gold); // 49700 - 1200

        BackpackSystem.Expand(GameState.Player);
        Assert.Equal(40, GameState.Player.InventorySize);
        Assert.Equal(45800, GameState.Player.Gold); // 48500 - 2700
    }

    [Fact]
    public void Expand_TracksExpansionCount()
    {
        GameState.Player.Gold = 50000;

        Assert.Equal(0, GameState.Player.BackpackExpansions);

        BackpackSystem.Expand(GameState.Player);
        Assert.Equal(1, GameState.Player.BackpackExpansions);

        BackpackSystem.Expand(GameState.Player);
        Assert.Equal(2, GameState.Player.BackpackExpansions);
    }

    // ---------- HELPERS ----------

    [Fact]
    public void IsFull_ReturnsTrueWhenAtCapacity()
    {
        GameState.Player.InventorySize = 2;
        GameSystems.AddToInventory(GameSystems.CreateItem("A", ItemType.Material, EquipSlot.None));
        GameSystems.AddToInventory(GameSystems.CreateItem("B", ItemType.Material, EquipSlot.None));

        Assert.True(BackpackSystem.IsFull(GameState.Player));
    }

    [Fact]
    public void IsFull_ReturnsFalseWhenSlotsAvailable()
    {
        Assert.False(BackpackSystem.IsFull(GameState.Player)); // 25 slots, 0 items
    }

    [Fact]
    public void GetExpansionCost_MatchesSpecTable()
    {
        // Spec table: N=1 -> 300, N=2 -> 1200 (300*4), N=3 -> 2700 (300*9)
        Assert.Equal(300, BackpackSystem.GetExpansionCost(GameState.Player));   // N=1

        GameState.Player.BackpackExpansions = 1;
        Assert.Equal(1200, BackpackSystem.GetExpansionCost(GameState.Player));  // N=2

        GameState.Player.BackpackExpansions = 2;
        Assert.Equal(2700, BackpackSystem.GetExpansionCost(GameState.Player));  // N=3
    }
}
