using System;

public static class BackpackSystem
{
    public const int SlotsPerExpansion = 5;
    public const int BaseCostMultiplier = 300;

    public static (bool success, string message) Expand(PlayerState player)
    {
        int cost = GetExpansionCost(player);
        if (player.Gold < cost)
            return (false, $"Not enough gold (need {cost}, have {player.Gold})");

        player.Gold -= cost;
        player.BackpackExpansions++;
        player.InventorySize += SlotsPerExpansion;
        return (true, $"Backpack expanded to {player.InventorySize} slots for {cost}g");
    }

    public static int GetExpansionCost(PlayerState player)
    {
        int n = player.BackpackExpansions + 1;
        return BaseCostMultiplier * n * n;
    }

    public static bool IsFull(PlayerState player)
    {
        return player.Inventory.Count >= player.InventorySize;
    }
}
