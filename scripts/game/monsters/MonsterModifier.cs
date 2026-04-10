using System;
using System.Collections.Generic;

public enum MonsterModifierType
{
    ExtraFast,
    ExtraStrong,
    StoneSkin,
    FireEnchanted,
    ColdEnchanted,
    LightningAura,
    Teleporting,
    ManaBurn,
    Summoner,
    Vampiric
}

public static class MonsterModifiers
{
    public static float GetSpeedMultiplier(MonsterModifierType mod) => mod switch
    {
        MonsterModifierType.ExtraFast => 1.33f,
        _ => 1.0f
    };

    public static float GetDamageMultiplier(MonsterModifierType mod) => mod switch
    {
        MonsterModifierType.ExtraStrong => 1.5f,
        _ => 1.0f
    };

    public static int GetDefenseBonus(MonsterModifierType mod) => mod switch
    {
        MonsterModifierType.StoneSkin => 80,
        _ => 0
    };

    public static List<MonsterModifierType> RollModifiers(int count, Random rng)
    {
        var allMods = Enum.GetValues<MonsterModifierType>();
        var available = new List<MonsterModifierType>(allMods);
        var result = new List<MonsterModifierType>();

        for (int i = 0; i < count && available.Count > 0; i++)
        {
            int idx = rng.Next(available.Count);
            result.Add(available[idx]);
            available.RemoveAt(idx);
        }
        return result;
    }

    public static (float speed, float damage, int defense) GetCombinedEffects(List<MonsterModifierType> modifiers)
    {
        float speed = 1.0f;
        float damage = 1.0f;
        int defense = 0;
        foreach (var mod in modifiers)
        {
            speed *= GetSpeedMultiplier(mod);
            damage *= GetDamageMultiplier(mod);
            defense += GetDefenseBonus(mod);
        }
        return (speed, damage, defense);
    }
}
