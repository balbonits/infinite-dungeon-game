using System.Collections.Generic;
using System.Linq;

namespace DungeonGame;

/// <summary>
/// Central registry of all available affixes, organized by tier.
/// Affix availability is gated by item level (floor depth of the base item).
/// Pure data — no Godot dependency.
/// </summary>
public static class AffixDatabase
{
    private static readonly Dictionary<string, AffixDef> Affixes = new();

    static AffixDatabase()
    {
        // --- Tier 1 (Item Level 1+) ---
        RegisterPrefix("keen_1", "Keen", AffixCategory.Offensive, "damage", 1, 1, 3, false, 50, 2);
        RegisterPrefix("sturdy_1", "Sturdy", AffixCategory.Defensive, "defense", 1, 1, 3, false, 50, 2);
        RegisterPrefix("energizing_1", "Energizing", AffixCategory.Utility, "mana", 1, 1, 5, false, 40, 2);
        RegisterPrefix("fiery_1", "Fiery", AffixCategory.Elemental, "fire_damage", 1, 1, 2, false, 60, 3);
        RegisterSuffix("striking_1", "of Striking", AffixCategory.Offensive, "crit_chance", 1, 1, 2, true, 60, 3);
        RegisterSuffix("bear_1", "of the Bear", AffixCategory.Defensive, "max_hp", 1, 1, 8, false, 45, 2);
        RegisterSuffix("swiftness_1", "of Swiftness", AffixCategory.Utility, "move_speed", 1, 1, 2, true, 55, 2);
        RegisterSuffix("learning_1", "of Learning", AffixCategory.Utility, "xp_bonus", 1, 1, 3, true, 70, 3);

        // --- Tier 2 (Item Level 10+) ---
        RegisterPrefix("keen_2", "Keen", AffixCategory.Offensive, "damage", 2, 10, 7, false, 150, 5);
        RegisterPrefix("sturdy_2", "Sturdy", AffixCategory.Defensive, "defense", 2, 10, 7, false, 150, 5);
        RegisterPrefix("vicious_2", "Vicious", AffixCategory.Offensive, "damage_percent", 2, 10, 6, true, 200, 6);
        RegisterPrefix("fortified_2", "Fortified", AffixCategory.Defensive, "max_hp", 2, 10, 20, false, 160, 5);
        RegisterPrefix("frozen_2", "Frozen", AffixCategory.Elemental, "frost_damage", 2, 10, 5, false, 180, 6);
        RegisterSuffix("striking_2", "of Striking", AffixCategory.Offensive, "crit_chance", 2, 10, 5, true, 180, 6);
        RegisterSuffix("ruin_2", "of Ruin", AffixCategory.Offensive, "crit_damage", 2, 10, 8, true, 200, 7);
        RegisterSuffix("evasion_2", "of Evasion", AffixCategory.Defensive, "dodge", 2, 10, 4, true, 170, 5);

        // --- Tier 3 (Item Level 25+) ---
        RegisterPrefix("keen_3", "Keen", AffixCategory.Offensive, "damage", 3, 25, 13, false, 400, 10);
        RegisterPrefix("sturdy_3", "Sturdy", AffixCategory.Defensive, "defense", 3, 25, 13, false, 400, 10);
        RegisterPrefix("vicious_3", "Vicious", AffixCategory.Offensive, "damage_percent", 3, 25, 12, true, 500, 12);
        RegisterPrefix("swift_3", "Swift", AffixCategory.Offensive, "attack_speed", 3, 25, 8, true, 450, 11);
        RegisterPrefix("shocking_3", "Shocking", AffixCategory.Elemental, "lightning_damage", 3, 25, 10, false, 480, 12);
        RegisterSuffix("striking_3", "of Striking", AffixCategory.Offensive, "crit_chance", 3, 25, 10, true, 450, 11);
        RegisterSuffix("bear_3", "of the Bear", AffixCategory.Defensive, "max_hp", 3, 25, 35, false, 380, 10);
        RegisterSuffix("flame_resist_3", "of Flame Resist", AffixCategory.Elemental, "fire_resist", 3, 25, 12, true, 350, 9);

        // --- Tier 4 (Item Level 50+) ---
        RegisterPrefix("keen_4", "Keen", AffixCategory.Offensive, "damage", 4, 50, 22, false, 800, 18);
        RegisterPrefix("vicious_4", "Vicious", AffixCategory.Offensive, "damage_percent", 4, 50, 20, true, 1000, 20);
        RegisterPrefix("warding_4", "Warding", AffixCategory.Defensive, "damage_resist", 4, 50, 15, true, 900, 18);
        RegisterSuffix("ruin_4", "of Ruin", AffixCategory.Offensive, "crit_damage", 4, 50, 22, true, 950, 20);
        RegisterSuffix("bear_4", "of the Bear", AffixCategory.Defensive, "max_hp", 4, 50, 60, false, 750, 16);
        RegisterSuffix("swiftness_4", "of Swiftness", AffixCategory.Utility, "move_speed", 4, 50, 8, true, 700, 15);
    }

    public static AffixDef? Get(string id) => Affixes.GetValueOrDefault(id);

    /// <summary>
    /// Get all affixes available for an item of the given level.
    /// </summary>
    public static IEnumerable<AffixDef> GetAvailable(int itemLevel, AffixType? typeFilter = null)
    {
        return Affixes.Values.Where(a =>
            a.MinItemLevel <= itemLevel &&
            (typeFilter == null || a.Type == typeFilter));
    }

    /// <summary>
    /// Get the best tier available for a given item level.
    /// </summary>
    public static int GetMaxTier(int itemLevel)
    {
        if (itemLevel >= 100) return 6;
        if (itemLevel >= 75) return 5;
        if (itemLevel >= 50) return 4;
        if (itemLevel >= 25) return 3;
        if (itemLevel >= 10) return 2;
        return 1;
    }

    private static void RegisterPrefix(string id, string name, AffixCategory category,
        string stat, int tier, int minLevel, float value, bool isPercent, int gold, int materials)
    {
        Affixes[id] = new AffixDef
        {
            Id = id,
            Name = name,
            Type = AffixType.Prefix,
            Category = category,
            StatModified = stat,
            Tier = tier,
            MinItemLevel = minLevel,
            Value = value,
            IsPercent = isPercent,
            GoldCost = gold,
            MaterialCost = materials,
        };
    }

    private static void RegisterSuffix(string id, string name, AffixCategory category,
        string stat, int tier, int minLevel, float value, bool isPercent, int gold, int materials)
    {
        Affixes[id] = new AffixDef
        {
            Id = id,
            Name = name,
            Type = AffixType.Suffix,
            Category = category,
            StatModified = stat,
            Tier = tier,
            MinItemLevel = minLevel,
            Value = value,
            IsPercent = isPercent,
            GoldCost = gold,
            MaterialCost = materials,
        };
    }
}
