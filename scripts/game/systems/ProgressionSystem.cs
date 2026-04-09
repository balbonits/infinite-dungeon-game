using System;

/// <summary>
/// XP awards, level-ups, and stat allocation for any entity.
/// All methods are static and take EntityData as first parameter.
/// XP threshold: Level * Level * 45. Supports multi-level-up from a single award.
/// </summary>
public static class ProgressionSystem
{
    /// <summary>
    /// Award XP to an entity. Handles multi-level-up via while loop.
    /// On level up: increment Level, recalculate derived stats,
    /// award 3 stat points + 2 skill points (bonus at milestones divisible by 10).
    /// </summary>
    public static (bool leveledUp, int levelsGained) AwardXP(EntityData entity, int amount)
    {
        if (amount <= 0)
            return (false, 0);

        entity.XP += amount;
        int levelsGained = 0;

        while (entity.XP >= GetXPToNext(entity))
        {
            entity.XP -= GetXPToNext(entity);
            entity.Level++;
            levelsGained++;

            StatSystem.RecalculateDerived(entity);

            // Base awards per level
            int statPts = 3;
            int skillPts = 2;

            // Bonus at milestone levels (multiples of 10)
            if (entity.Level % 10 == 0)
            {
                statPts += 2;
                skillPts += 1;
            }

            entity.StatPoints += statPts;
            entity.SkillPoints += skillPts;
        }

        return (levelsGained > 0, levelsGained);
    }

    /// <summary>
    /// Spend 1 stat point to increment a named stat (STR, DEX, INT, VIT).
    /// Recalculates derived stats after allocation.
    /// </summary>
    public static (bool success, string message) AllocateStat(EntityData entity, string stat)
    {
        if (entity.StatPoints <= 0)
            return (false, "No stat points available");

        switch (stat.ToUpperInvariant())
        {
            case "STR":
                entity.StatPoints--;
                entity.STR++;
                StatSystem.RecalculateDerived(entity);
                return (true, $"STR -> {entity.STR}");

            case "DEX":
                entity.StatPoints--;
                entity.DEX++;
                StatSystem.RecalculateDerived(entity);
                return (true, $"DEX -> {entity.DEX}");

            case "INT":
                entity.StatPoints--;
                entity.INT++;
                StatSystem.RecalculateDerived(entity);
                return (true, $"INT -> {entity.INT}");

            case "VIT":
                entity.StatPoints--;
                entity.VIT++;
                StatSystem.RecalculateDerived(entity);
                return (true, $"VIT -> {entity.VIT}");

            default:
                return (false, $"Invalid stat: {stat}");
        }
    }

    /// <summary>
    /// Returns the total XP needed to reach the next level.
    /// Formula: Level * Level * 45.
    /// </summary>
    public static int GetXPToNext(EntityData entity)
    {
        return entity.Level * entity.Level * 45;
    }

    /// <summary>
    /// Returns XP progress toward the next level as a float from 0 to 1.
    /// </summary>
    public static float GetXPProgress(EntityData entity)
    {
        int xpToNext = GetXPToNext(entity);
        if (xpToNext <= 0)
            return 0f;

        return (float)entity.XP / xpToNext;
    }
}
