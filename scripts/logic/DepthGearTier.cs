using System;

namespace DungeonGame;

/// <summary>
/// Extended base quality tiers gated by floor depth milestones.
/// Spec: docs/systems/depth-gear-tiers.md
/// </summary>
public enum BaseQuality
{
    Normal = 0,
    Superior = 1,     // Floor 10+
    Elite = 2,        // Floor 25+
    Masterwork = 3,   // Floor 50+
    Mythic = 4,       // Floor 100+
    Transcendent = 5, // Floor 150+
}

public static class DepthGearTiers
{
    /// <summary>Minimum floor required to drop each quality tier.</summary>
    public static int GetMinFloor(BaseQuality quality) => quality switch
    {
        BaseQuality.Normal => 1,
        BaseQuality.Superior => 10,
        BaseQuality.Elite => 25,
        BaseQuality.Masterwork => 50,
        BaseQuality.Mythic => 100,
        BaseQuality.Transcendent => 150,
        _ => 1,
    };

    /// <summary>Base stat bonus range (min%, max%) for each quality tier.</summary>
    public static (float min, float max) GetStatBonusRange(BaseQuality quality) => quality switch
    {
        BaseQuality.Normal => (0f, 0f),
        BaseQuality.Superior => (0.10f, 0.20f),
        BaseQuality.Elite => (0.25f, 0.40f),
        BaseQuality.Masterwork => (0.45f, 0.60f),
        BaseQuality.Mythic => (0.65f, 0.85f),
        BaseQuality.Transcendent => (0.90f, 1.20f),
        _ => (0f, 0f),
    };

    /// <summary>Maximum total affixes (prefix + suffix) for each quality tier.</summary>
    public static int GetMaxAffixes(BaseQuality quality) => quality switch
    {
        BaseQuality.Normal or BaseQuality.Superior or BaseQuality.Elite => 6,
        BaseQuality.Masterwork => 8,
        BaseQuality.Mythic => 10,
        BaseQuality.Transcendent => 12,
        _ => 6,
    };

    /// <summary>Max prefix and suffix slots for each quality tier.</summary>
    public static (int prefix, int suffix) GetAffixSlots(BaseQuality quality) => quality switch
    {
        BaseQuality.Normal or BaseQuality.Superior or BaseQuality.Elite => (3, 3),
        BaseQuality.Masterwork => (4, 4),
        BaseQuality.Mythic => (5, 5),
        BaseQuality.Transcendent => (6, 6),
        _ => (3, 3),
    };

    /// <summary>Crafting cost multiplier for each quality tier.</summary>
    public static float GetCraftCostMultiplier(BaseQuality quality) => quality switch
    {
        BaseQuality.Normal => 1.0f,
        BaseQuality.Superior => 1.2f,
        BaseQuality.Elite => 1.5f,
        BaseQuality.Masterwork => 2.0f,
        BaseQuality.Mythic => 3.0f,
        BaseQuality.Transcendent => 5.0f,
        _ => 1.0f,
    };

    /// <summary>
    /// Roll a quality tier based on floor depth and optional floor shift.
    /// Floor shift comes from saturation and pact quality bonuses.
    /// </summary>
    public static BaseQuality RollQuality(int floor, int floorShift, Random? rng = null)
    {
        int effectiveFloor = floor + floorShift;
        rng ??= Random.Shared;
        float roll = (float)rng.NextDouble();

        // Distribution table (spec: depth-gear-tiers.md)
        if (effectiveFloor >= 150)
        {
            if (roll < 0.05f) return BaseQuality.Normal;
            if (roll < 0.20f) return BaseQuality.Superior;
            if (roll < 0.50f) return BaseQuality.Elite;
            if (roll < 0.80f) return BaseQuality.Masterwork;
            if (roll < 0.95f) return BaseQuality.Mythic;
            return BaseQuality.Transcendent;
        }
        if (effectiveFloor >= 100)
        {
            if (roll < 0.10f) return BaseQuality.Normal;
            if (roll < 0.35f) return BaseQuality.Superior;
            if (roll < 0.70f) return BaseQuality.Elite;
            if (roll < 0.95f) return BaseQuality.Masterwork;
            return BaseQuality.Mythic;
        }
        if (effectiveFloor >= 75)
        {
            if (roll < 0.20f) return BaseQuality.Normal;
            if (roll < 0.55f) return BaseQuality.Superior;
            if (roll < 0.85f) return BaseQuality.Elite;
            return BaseQuality.Masterwork;
        }
        if (effectiveFloor >= 50)
        {
            if (roll < 0.35f) return BaseQuality.Normal;
            if (roll < 0.75f) return BaseQuality.Superior;
            if (roll < 0.95f) return BaseQuality.Elite;
            return BaseQuality.Masterwork;
        }
        if (effectiveFloor >= 25)
        {
            if (roll < 0.60f) return BaseQuality.Normal;
            if (roll < 0.95f) return BaseQuality.Superior;
            return BaseQuality.Elite;
        }
        if (effectiveFloor >= 10)
        {
            if (roll < 0.80f) return BaseQuality.Normal;
            return BaseQuality.Superior;
        }
        return BaseQuality.Normal;
    }
}
