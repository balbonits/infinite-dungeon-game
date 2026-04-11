using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonGame;

/// <summary>
/// Achievement system — the Fated Ledger.
/// Tracks player milestones across combat, exploration, progression, and economy.
/// Counter-based: progress derived from persistent stat counters.
/// Pure logic — no Godot dependency.
/// </summary>
public enum AchievementCategory
{
    Combat,
    Exploration,
    Progression,
    Economy,
    Mastery,
}

public record AchievementDef
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public AchievementCategory Category { get; init; }
    public string CounterName { get; init; } = "";
    public int TargetValue { get; init; }
    public int GoldReward { get; init; }
    public string? TitleReward { get; init; }
}

public class AchievementTracker
{
    private static readonly List<AchievementDef> AllAchievements = new();
    private readonly Dictionary<string, int> _counters = new();
    private readonly HashSet<string> _unlocked = new();

    static AchievementTracker()
    {
        RegisterAchievements();
    }

    public IReadOnlySet<string> Unlocked => _unlocked;

    public int GetCounter(string name) => _counters.GetValueOrDefault(name);

    public void IncrementCounter(string name, int amount = 1)
    {
        _counters[name] = _counters.GetValueOrDefault(name) + amount;
    }

    public void SetCounter(string name, int value)
    {
        int current = _counters.GetValueOrDefault(name);
        if (value > current)
            _counters[name] = value;
    }

    /// <summary>
    /// Check all achievements against current counters. Returns newly unlocked achievements.
    /// </summary>
    public List<AchievementDef> Evaluate()
    {
        var newlyUnlocked = new List<AchievementDef>();
        foreach (var def in AllAchievements)
        {
            if (_unlocked.Contains(def.Id)) continue;
            int progress = _counters.GetValueOrDefault(def.CounterName);
            if (progress >= def.TargetValue)
            {
                _unlocked.Add(def.Id);
                newlyUnlocked.Add(def);
            }
        }
        return newlyUnlocked;
    }

    /// <summary>Get progress for a specific achievement (0.0 to 1.0).</summary>
    public float GetProgress(AchievementDef def)
    {
        if (def.TargetValue <= 0) return 1f;
        int current = _counters.GetValueOrDefault(def.CounterName);
        return Math.Min(1f, (float)current / def.TargetValue);
    }

    public bool IsUnlocked(string id) => _unlocked.Contains(id);

    public static IReadOnlyList<AchievementDef> GetAll() => AllAchievements;

    public static IEnumerable<AchievementDef> GetByCategory(AchievementCategory cat) =>
        AllAchievements.Where(a => a.Category == cat);

    // --- Save/Load ---

    public SavedAchievementData CaptureState()
    {
        return new SavedAchievementData
        {
            Counters = _counters.Select(kvp => new SavedCounter { Name = kvp.Key, Value = kvp.Value }).ToArray(),
            UnlockedIds = _unlocked.ToArray(),
        };
    }

    public void RestoreState(SavedAchievementData data)
    {
        _counters.Clear();
        _unlocked.Clear();
        foreach (var c in data.Counters)
            _counters[c.Name] = c.Value;
        foreach (var id in data.UnlockedIds)
            _unlocked.Add(id);
    }

    // --- Achievement Registry ---

    private static void Register(string id, string name, string desc,
        AchievementCategory cat, string counter, int target, int gold, string? title = null)
    {
        AllAchievements.Add(new AchievementDef
        {
            Id = id,
            Name = name,
            Description = desc,
            Category = cat,
            CounterName = counter,
            TargetValue = target,
            GoldReward = gold,
            TitleReward = title,
        });
    }

    private static void RegisterAchievements()
    {
        // Combat
        Register("c_first_blood", "First Blood", "Defeat your first enemy", AchievementCategory.Combat, "enemies_killed", 1, 10);
        Register("c_100_kills", "Centurion", "Defeat 100 enemies", AchievementCategory.Combat, "enemies_killed", 100, 100);
        Register("c_1000_kills", "Slaughter", "Defeat 1,000 enemies", AchievementCategory.Combat, "enemies_killed", 1000, 500, "Slayer");
        Register("c_10000_kills", "Genocide", "Defeat 10,000 enemies", AchievementCategory.Combat, "enemies_killed", 10000, 2000, "Annihilator");
        Register("c_first_wipe", "Floor Domination", "Clear all enemies on a floor", AchievementCategory.Combat, "floor_wipes", 1, 50);
        Register("c_10_wipes", "Exterminator", "Clear 10 floors completely", AchievementCategory.Combat, "floor_wipes", 10, 300);

        // Exploration
        Register("e_floor_2", "First Steps", "Reach floor 2", AchievementCategory.Exploration, "deepest_floor", 2, 10);
        Register("e_floor_10", "Zone Breaker", "Reach floor 10", AchievementCategory.Exploration, "deepest_floor", 10, 100);
        Register("e_floor_25", "Deep Diver", "Reach floor 25", AchievementCategory.Exploration, "deepest_floor", 25, 300, "Deep Diver");
        Register("e_floor_50", "Abyss Walker", "Reach floor 50", AchievementCategory.Exploration, "deepest_floor", 50, 1000, "Abyss Walker");
        Register("e_floor_100", "Centurion Depth", "Reach floor 100", AchievementCategory.Exploration, "deepest_floor", 100, 5000, "The Relentless");
        Register("e_town_returns", "Frequent Flyer", "Return to town 50 times", AchievementCategory.Exploration, "town_returns", 50, 200);

        // Progression
        Register("p_level_5", "Novice", "Reach level 5", AchievementCategory.Progression, "player_level", 5, 25);
        Register("p_level_10", "Apprentice", "Reach level 10", AchievementCategory.Progression, "player_level", 10, 75);
        Register("p_level_25", "Journeyman", "Reach level 25", AchievementCategory.Progression, "player_level", 25, 200);
        Register("p_level_50", "Expert", "Reach level 50", AchievementCategory.Progression, "player_level", 50, 500, "Expert");
        Register("p_level_100", "Master", "Reach level 100", AchievementCategory.Progression, "player_level", 100, 2000, "Master");
        Register("p_first_skill", "Student", "Level any skill to 1", AchievementCategory.Progression, "skills_leveled", 1, 20);
        Register("p_10_skills", "Scholar", "Level 10 different skills", AchievementCategory.Progression, "skills_leveled", 10, 150);

        // Economy
        Register("ec_first_buy", "Customer", "Buy your first item", AchievementCategory.Economy, "items_bought", 1, 10);
        Register("ec_1000_gold", "Coin Hoarder", "Earn 1,000 total gold", AchievementCategory.Economy, "gold_earned", 1000, 50);
        Register("ec_10000_gold", "Gold Baron", "Earn 10,000 total gold", AchievementCategory.Economy, "gold_earned", 10000, 500);
        Register("ec_first_craft", "Apprentice Smith", "Apply your first affix", AchievementCategory.Economy, "affixes_applied", 1, 30);
        Register("ec_first_recycle", "Recycler", "Recycle your first item", AchievementCategory.Economy, "items_recycled", 1, 15);
        Register("ec_bank_expand", "Investor", "Expand your bank once", AchievementCategory.Economy, "bank_expansions", 1, 50);

        // Mastery
        Register("m_warrior_50", "Warrior's Path", "Reach level 50 as Warrior", AchievementCategory.Mastery, "warrior_level", 50, 1000, "Warlord");
        Register("m_ranger_50", "Ranger's Path", "Reach level 50 as Ranger", AchievementCategory.Mastery, "ranger_level", 50, 1000, "Deadeye");
        Register("m_mage_50", "Mage's Path", "Reach level 50 as Mage", AchievementCategory.Mastery, "mage_level", 50, 1000, "Archmage");
        Register("m_survived_10", "Unkillable", "Survive 10 deaths", AchievementCategory.Mastery, "deaths", 10, 200);
        Register("m_quest_10", "Errand Runner", "Complete 10 quests", AchievementCategory.Mastery, "quests_completed", 10, 300);
    }
}

public record SavedAchievementData
{
    public SavedCounter[] Counters { get; init; } = Array.Empty<SavedCounter>();
    public string[] UnlockedIds { get; init; } = Array.Empty<string>();
}

public record SavedCounter
{
    public string Name { get; init; } = "";
    public int Value { get; init; }
}
