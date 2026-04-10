using System;

public static class ElementalCombat
{
    public const int MaxResistance = 75;
    public const int MinResistance = -100;

    public static ElementalDamageResult CalculateDamage(
        int rawDamage, DamageType type, EntityData target, int floorNumber, bool isCrit = false)
    {
        int effectiveRes = 0;
        int mitigated;

        if (type == DamageType.Physical)
        {
            float defReduction = StatSystem.GetDefenseReduction(target);
            mitigated = Math.Max(1, (int)(rawDamage * (1.0f - defReduction)));
        }
        else
        {
            effectiveRes = target.Resistances.GetEffective(type, floorNumber);
            effectiveRes = Math.Clamp(effectiveRes, MinResistance, MaxResistance);
            mitigated = Math.Max(1, (int)(rawDamage * (1.0f - effectiveRes / 100.0f)));
        }

        if (isCrit)
            mitigated = (int)(mitigated * 1.5f);

        return new ElementalDamageResult
        {
            RawDamage = rawDamage,
            FinalDamage = Math.Max(1, mitigated),
            DamageType = type,
            IsCrit = isCrit,
            EffectiveResistance = effectiveRes
        };
    }

    public static int GetAmbientDarkDPS(int floorNumber)
    {
        return Math.Max(0, (floorNumber - 75) * 2);
    }

    public static int GetAmbientDarkDamagePerSecond(int floorNumber, EntityData target)
    {
        int rawDPS = GetAmbientDarkDPS(floorNumber);
        if (rawDPS <= 0) return 0;
        int effectiveRes = target.Resistances.GetEffective(DamageType.Dark, floorNumber);
        effectiveRes = Math.Clamp(effectiveRes, MinResistance, MaxResistance);
        return Math.Max(0, (int)(rawDPS * (1.0f - effectiveRes / 100.0f)));
    }
}

public struct ElementalDamageResult
{
    public int RawDamage;
    public int FinalDamage;
    public DamageType DamageType;
    public bool IsCrit;
    public int EffectiveResistance;
}
