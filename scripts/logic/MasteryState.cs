using System;

namespace DungeonGame;

/// <summary>
/// Mutable per-mastery state. Tracks level, XP, and calculates passive bonuses.
/// Pure logic — no Godot dependency. Testable with xUnit.
/// </summary>
public class MasteryState
{
    public string MasteryId { get; }
    public int Level { get; private set; }
    public int Xp { get; private set; }

    public MasteryState(string masteryId)
    {
        MasteryId = masteryId;
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
                // Level 0→1 is instant
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
    /// Allocate one SP. Grants XP: 50 * (1 + level * 0.1).
    /// Returns levels gained.
    /// </summary>
    public int AddSkillPoint()
    {
        int xp = (int)(50 * (1 + Level * 0.1));
        return AddXp(xp);
    }

    /// <summary>
    /// Calculate passive bonus at current level using diminishing returns.
    /// Formula: level * multiplier * (100 / (level + 100))
    /// </summary>
    public float GetPassiveBonus(float multiplier)
    {
        if (Level <= 0) return 0;
        return Level * multiplier * (100f / (Level + 100f));
    }

    /// <summary>Set state from save data.</summary>
    public void SetState(int level, int xp)
    {
        Level = Math.Max(0, level);
        Xp = Math.Max(0, xp);
    }
}
