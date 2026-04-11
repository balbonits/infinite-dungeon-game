using System;

namespace DungeonGame;

/// <summary>
/// Mutable state for a single skill instance owned by the player.
/// Tracks level, XP progress, and provides passive bonus calculations.
/// Pure logic — no Godot dependency.
/// </summary>
public class SkillState
{
    private const float DiminishingK = 100.0f;

    public string SkillId { get; }
    public int Level { get; private set; }
    public int Xp { get; private set; }

    public SkillState(string skillId, int level = 0, int xp = 0)
    {
        SkillId = skillId;
        Level = level;
        Xp = xp;
    }

    /// <summary>
    /// XP required to reach the next level.
    /// Formula: floor(skill_level^2 * 20)
    /// Level 0→1 is instant (0 XP required).
    /// </summary>
    public int XpToNextLevel => Level == 0 ? 0 : Level * Level * 20;

    /// <summary>
    /// Add XP from skill use. Returns number of levels gained.
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
    /// Add XP from a skill point allocation.
    /// Formula: 50 * (1 + target_skill_level * 0.1)
    /// </summary>
    public int AddSkillPoint()
    {
        int xpGain = (int)(50 * (1 + Level * 0.1f));
        return AddXp(xpGain);
    }

    /// <summary>
    /// Calculate the passive bonus this skill provides at its current level.
    /// Formula: skill_level * multiplier * (K / (skill_level + K))
    /// </summary>
    public float GetPassiveBonus(float multiplier)
    {
        if (Level <= 0) return 0f;
        return Level * multiplier * (DiminishingK / (Level + DiminishingK));
    }

    /// <summary>
    /// Set state directly (for save/load).
    /// </summary>
    public void SetState(int level, int xp)
    {
        Level = Math.Max(0, level);
        Xp = Math.Max(0, xp);
    }
}
