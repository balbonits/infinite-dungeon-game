using System;

namespace DungeonGame;

/// <summary>
/// Mutable per-ability state. Tracks level, XP, and use count (for affinity).
/// Pure logic — no Godot dependency. Testable with xUnit.
/// </summary>
public class AbilityState
{
    public string AbilityId { get; }
    public int Level { get; private set; }
    public int Xp { get; private set; }
    public int UseCount { get; private set; }

    public AbilityState(string abilityId)
    {
        AbilityId = abilityId;
    }

    /// <summary>XP required to reach the next level. Formula: level^2 * 20</summary>
    public int XpToNextLevel => Level == 0 ? 0 : Level * Level * 20;

    /// <summary>
    /// Add XP and auto-level. Level 0→1 is instant (0 XP required).
    /// Returns number of levels gained.
    /// </summary>
    public int AddXp(int amount)
    {
        if (amount <= 0) return 0;
        Xp += amount;

        int levelsGained = 0;
        while (true)
        {
            int required = XpToNextLevel;
            if (required == 0)
            {
                Level++;
                levelsGained++;
                continue;
            }
            if (Xp < required) break;
            Xp -= required;
            Level++;
            levelsGained++;
        }
        return levelsGained;
    }

    /// <summary>
    /// Allocate one AP. Grants XP: 50 * (1 + level * 0.1).
    /// Returns levels gained.
    /// </summary>
    public int AddAbilityPoint()
    {
        int xp = (int)(50 * (1 + Level * 0.1));
        return AddXp(xp);
    }

    /// <summary>Record one use for affinity tracking.</summary>
    public void IncrementUse()
    {
        UseCount++;
    }

    /// <summary>
    /// Get affinity tier based on use count.
    /// 0 = none, 1 = Familiar (100), 2 = Practiced (500), 3 = Expert (1000), 4 = Mastered (5000)
    /// </summary>
    public int AffinityTier => UseCount switch
    {
        >= 5000 => 4,
        >= 1000 => 3,
        >= 500 => 2,
        >= 100 => 1,
        _ => 0,
    };

    /// <summary>Set state from save data.</summary>
    public void SetState(int level, int xp, int useCount = 0)
    {
        Level = Math.Max(0, level);
        Xp = Math.Max(0, xp);
        UseCount = Math.Max(0, useCount);
    }
}
