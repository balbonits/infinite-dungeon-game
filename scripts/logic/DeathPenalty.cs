using System;

namespace DungeonGame;

/// <summary>
/// Death penalty calculations. Pure logic — no Godot dependency.
/// Formulas from docs/systems/death.md (post-redesign: 5-option sacrifice dialog).
/// </summary>
public static class DeathPenalty
{
    // ── Cost formulas (new spec — see docs/systems/death.md#buyout-cost-formulas) ──

    /// <summary>Gold cost to save equipment (1 random equipped item otherwise lost).</summary>
    public static long GetEquipmentBuyoutCost(int deepestFloor) => deepestFloor * 25L;

    /// <summary>Gold cost to save backpack (all items + backpack gold otherwise lost).</summary>
    public static long GetBackpackBuyoutCost(int deepestFloor) => deepestFloor * 60L;

    /// <summary>Combined cost to save both equipment and backpack.</summary>
    public static long GetBothBuyoutCost(int deepestFloor) =>
        GetEquipmentBuyoutCost(deepestFloor) + GetBackpackBuyoutCost(deepestFloor);

    // ── EXP loss (unavoidable — applies in all paths) ──

    /// <summary>Percentage of current level's XP progress lost on death.</summary>
    public static float GetExpLossPercent(int deepestFloor) =>
        MathF.Min(deepestFloor * 0.4f, 50.0f);

    /// <summary>XP amount to lose from current progress.</summary>
    public static int CalculateXpLoss(int currentXp, int deepestFloor)
    {
        float percent = GetExpLossPercent(deepestFloor) / 100f;
        return (int)(currentXp * percent);
    }

    // ── Sacrificial Idol ──

    public static bool HasSacrificialIdol(Inventory inventory)
    {
        return inventory.FindSlot("consumable_sacrificial_idol") >= 0;
    }

    public static void ConsumeSacrificialIdol(Inventory inventory)
    {
        int idx = inventory.FindSlot("consumable_sacrificial_idol");
        if (idx >= 0) inventory.RemoveAt(idx, 1);
    }

    // ── Backpack loss (full wipe — items + gold) ──

    /// <summary>Wipe all backpack items and backpack gold. Used when backpack is not saved.</summary>
    public static void WipeBackpack(Inventory inventory)
    {
        inventory.Clear();
        inventory.Gold = 0;
    }

    /// <summary>
    /// Pay gold buyout, drawing from backpack first then bank. Returns true if paid in full.
    ///
    /// MVP note: the full spec (docs/systems/death.md#payment-sourcing) describes a player-
    /// chosen pocket split sub-dialog — the player can override the default to pay bank-first
    /// or any combination. This MVP implementation auto-splits backpack-first, which is the
    /// default the spec shows. The split sub-dialog is tracked as a follow-up polish ticket.
    /// </summary>
    public static bool PayBuyout(Inventory backpack, Bank bank, long cost)
    {
        if (cost <= 0) return true;
        long total = backpack.Gold + bank.Gold;
        if (total < cost) return false;

        long fromBackpack = Math.Min(backpack.Gold, cost);
        backpack.Gold -= fromBackpack;
        long remaining = cost - fromBackpack;
        if (remaining > 0) bank.Gold -= remaining;
        return true;
    }

    // ── Legacy (pre-redesign — retained for test compat) ──

    public static int GetItemsLost(int deepestFloor) => deepestFloor / 10 + 1;
    public static int GetExpProtectionCost(int deepestFloor) => deepestFloor * 15;
    public static int GetBackpackProtectionCost(int deepestFloor) => deepestFloor * 25;

    /// <summary>Legacy helper: remove <paramref name="itemsToLose"/> random items from the backpack.</summary>
    public static void ApplyItemLoss(Inventory inventory, int itemsToLose)
    {
        int lost = 0;
        var occupiedSlots = new System.Collections.Generic.List<int>();
        for (int i = 0; i < inventory.SlotCount; i++)
        {
            if (inventory.GetSlot(i) != null)
                occupiedSlots.Add(i);
        }

        while (lost < itemsToLose && occupiedSlots.Count > 0)
        {
            int pick = Random.Shared.Next(occupiedSlots.Count);
            int slotIndex = occupiedSlots[pick];
            inventory.RemoveAt(slotIndex);
            occupiedSlots.RemoveAt(pick);
            lost++;
        }
    }
}
