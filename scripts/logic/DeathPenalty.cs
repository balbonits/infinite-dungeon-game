using System;

namespace DungeonGame;

/// <summary>
/// Death penalty calculations. Pure logic — no Godot dependency.
/// Formulas from docs/systems/death.md.
/// </summary>
public static class DeathPenalty
{
    /// <summary>
    /// Percentage of current level's XP progress lost on death.
    /// Scales with deepest floor achieved, capped at 50%.
    /// </summary>
    public static float GetExpLossPercent(int deepestFloor)
    {
        return MathF.Min(deepestFloor * 0.4f, 50.0f);
    }

    /// <summary>
    /// Number of random backpack items lost on death.
    /// </summary>
    public static int GetItemsLost(int deepestFloor)
    {
        return deepestFloor / 10 + 1;
    }

    /// <summary>
    /// Gold cost to protect XP from loss.
    /// </summary>
    public static int GetExpProtectionCost(int deepestFloor)
    {
        return deepestFloor * 15;
    }

    /// <summary>
    /// Gold cost to protect backpack items from loss.
    /// </summary>
    public static int GetBackpackProtectionCost(int deepestFloor)
    {
        return deepestFloor * 25;
    }

    /// <summary>
    /// Calculate actual XP to lose based on current progress within the level.
    /// </summary>
    public static int CalculateXpLoss(int currentXp, int deepestFloor)
    {
        float percent = GetExpLossPercent(deepestFloor) / 100f;
        return (int)(currentXp * percent);
    }

    /// <summary>
    /// Check if the player has a Sacrificial Idol in their backpack.
    /// </summary>
    public static bool HasSacrificialIdol(Inventory inventory)
    {
        for (int i = 0; i < inventory.SlotCount; i++)
        {
            var stack = inventory.GetSlot(i);
            if (stack != null && stack.Item.Id == "idol_sacrificial")
                return true;
        }
        return false;
    }

    /// <summary>
    /// Consume one Sacrificial Idol from the inventory.
    /// </summary>
    public static void ConsumeSacrificialIdol(Inventory inventory)
    {
        for (int i = 0; i < inventory.SlotCount; i++)
        {
            var stack = inventory.GetSlot(i);
            if (stack != null && stack.Item.Id == "idol_sacrificial")
            {
                inventory.RemoveAt(i);
                return;
            }
        }
    }

    /// <summary>
    /// Remove random items from inventory (death penalty).
    /// </summary>
    public static void ApplyItemLoss(Inventory inventory, int itemsToLose)
    {
        var random = new Random();
        int lost = 0;

        // Collect occupied slot indices
        var occupiedSlots = new System.Collections.Generic.List<int>();
        for (int i = 0; i < inventory.SlotCount; i++)
        {
            if (inventory.GetSlot(i) != null)
                occupiedSlots.Add(i);
        }

        while (lost < itemsToLose && occupiedSlots.Count > 0)
        {
            int pick = random.Next(occupiedSlots.Count);
            int slotIndex = occupiedSlots[pick];
            inventory.RemoveAt(slotIndex);
            occupiedSlots.RemoveAt(pick);
            lost++;
        }
    }
}
