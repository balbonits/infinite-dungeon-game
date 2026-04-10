using System;
using System.Collections.Generic;

public static class MonsterSpawner
{
    public static int GetRoomBudget(int roomWidth, int roomHeight)
    {
        int area = roomWidth * roomHeight;
        return Math.Max(1, area / 12);
    }

    public static MonsterRarity RollRarity(Random rng)
    {
        double roll = rng.NextDouble();
        if (roll < 0.02) return MonsterRarity.Named;
        if (roll < 0.22) return MonsterRarity.Empowered;
        return MonsterRarity.Normal;
    }

    public static float GetHPMultiplier(MonsterRarity rarity) => rarity switch
    {
        MonsterRarity.Normal => 1.0f,
        MonsterRarity.Empowered => 2.0f,
        MonsterRarity.Named => 3.0f,
        _ => 1.0f
    };

    public static float GetRewardMultiplier(MonsterRarity rarity) => rarity switch
    {
        MonsterRarity.Normal => 1.0f,
        MonsterRarity.Empowered => 1.5f,
        MonsterRarity.Named => 3.0f,
        _ => 1.0f
    };

    public static int GetModifierCount(MonsterRarity rarity, int zone)
    {
        return rarity switch
        {
            MonsterRarity.Empowered => 1,
            MonsterRarity.Named => Math.Min(1 + zone / 2, 3),
            _ => 0
        };
    }

    public static Dictionary<MonsterArchetype, int> GetArchetypeMix(int budget, Random rng)
    {
        var mix = new Dictionary<MonsterArchetype, int>();
        if (budget <= 0) return mix;

        int melee = Math.Max(1, (int)(budget * 0.30f));
        int swarmer = (int)(budget * 0.35f);
        int ranged = (int)(budget * 0.20f);
        int bruiser = budget - melee - swarmer - ranged;

        if (melee > 0) mix[MonsterArchetype.Melee] = melee;
        if (swarmer > 0) mix[MonsterArchetype.Swarmer] = swarmer;
        if (ranged > 0) mix[MonsterArchetype.Ranged] = ranged;
        if (bruiser > 0) mix[MonsterArchetype.Bruiser] = bruiser;

        return mix;
    }
}
