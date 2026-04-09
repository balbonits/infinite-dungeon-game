using System;
using System.Collections.Generic;

public static class BankSystem
{
    public static (bool success, string message) Deposit(BankData bank, PlayerState player, ItemData item)
    {
        if (!player.Inventory.Contains(item))
            return (false, "Item not in inventory");

        // Try stacking in the bank first
        if (item.Stackable)
        {
            var existing = bank.Items.Find(i => i.Name == item.Name && i.Stackable);
            if (existing != null)
            {
                existing.StackCount += item.StackCount;
                player.Inventory.Remove(item);
                return (true, $"Deposited {item.StackCount}x {item.Name} (stacked)");
            }
        }

        if (IsFull(bank))
            return (false, "Bank is full");

        player.Inventory.Remove(item);
        bank.Items.Add(item);
        return (true, $"Deposited {item.Name}");
    }

    public static (bool success, string message) Withdraw(BankData bank, PlayerState player, ItemData item)
    {
        if (!bank.Items.Contains(item))
            return (false, "Item not in bank");

        // Try stacking in inventory first
        if (item.Stackable)
        {
            var existing = player.Inventory.Find(i => i.Name == item.Name && i.Stackable);
            if (existing != null)
            {
                existing.StackCount += item.StackCount;
                bank.Items.Remove(item);
                return (true, $"Withdrew {item.StackCount}x {item.Name} (stacked)");
            }
        }

        if (player.Inventory.Count >= player.InventorySize)
            return (false, "Inventory is full");

        bank.Items.Remove(item);
        player.Inventory.Add(item);
        return (true, $"Withdrew {item.Name}");
    }

    public static (bool success, string message) Expand(BankData bank, PlayerState player)
    {
        int cost = GetExpansionCost(bank);
        if (player.Gold < cost)
            return (false, $"Not enough gold (need {cost}, have {player.Gold})");

        player.Gold -= cost;
        bank.ExpansionCount++;
        bank.MaxSlots += BankData.SlotsPerExpansion;
        return (true, $"Bank expanded to {bank.MaxSlots} slots for {cost}g");
    }

    public static int GetExpansionCost(BankData bank)
    {
        int n = bank.ExpansionCount + 1;
        return BankData.BaseCostMultiplier * n * n;
    }

    public static bool IsFull(BankData bank)
    {
        return bank.Items.Count >= bank.MaxSlots;
    }
}
