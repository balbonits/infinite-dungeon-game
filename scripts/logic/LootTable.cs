using System;
using System.Collections.Generic;

namespace DungeonGame;

/// <summary>
/// Determines what enemies drop on death. Gold + chance-based item drops.
/// Pure logic — no Godot dependency.
/// </summary>
public static class LootTable
{
    /// <summary>
    /// Calculate gold dropped by an enemy of the given level.
    /// </summary>
    public static int GetGoldDrop(int enemyLevel)
    {
        int baseGold = 2 + enemyLevel;
        int variance = Math.Max(1, enemyLevel / 2);
        return baseGold + Random.Shared.Next(0, variance + 1);
    }

    /// <summary>
    /// Roll for an item drop. Returns null if no drop.
    /// Drop chance increases with enemy level.
    /// </summary>
    public static ItemDef? RollItemDrop(int enemyLevel)
    {
        // Base drop chance: 8% + 1% per enemy level, capped at 30%
        float dropChance = Math.Min(0.30f, 0.08f + enemyLevel * 0.01f);

        if (Random.Shared.NextSingle() > dropChance)
            return null;

        // Pick from droppable items weighted by category
        var candidates = GetDropCandidates(enemyLevel);
        if (candidates.Count == 0)
            return null;

        return candidates[Random.Shared.Next(candidates.Count)];
    }

    private static List<ItemDef> GetDropCandidates(int enemyLevel)
    {
        var candidates = new List<ItemDef>();

        foreach (var item in ItemDatabase.All)
        {
            // Skip quest items and items far above enemy level
            if (item.Category == ItemCategory.QuestItem)
                continue;
            if (item.LevelRequirement > enemyLevel + 3)
                continue;

            candidates.Add(item);
        }

        return candidates;
    }
}
