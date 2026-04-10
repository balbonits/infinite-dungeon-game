using System;

public enum WeaponType
{
    // Warrior
    Dagger, Sword, Axe, Club, Mace, Spear, Halberd,
    // Rogue
    Bow, Crossbow, ThrowingKnife, ThrowingAxe, Pistol, Rifle,
    // Mage
    Staff, Wand,
    // Default
    Unarmed
}

public static class CritSystem
{
    public const float DefaultCritMultiplier = 150f; // 1.5x = 50% bonus damage
    public const float MaxCritChance = 75f;

    public static float GetBaseCritChance(WeaponType type) => type switch
    {
        WeaponType.Dagger => 8f,
        WeaponType.Sword => 6f,
        WeaponType.Axe => 4f,
        WeaponType.Club => 3f,
        WeaponType.Mace => 4f,
        WeaponType.Spear => 5f,
        WeaponType.Halberd => 3f,
        WeaponType.Bow => 6f,
        WeaponType.Crossbow => 8f,
        WeaponType.ThrowingKnife => 7f,
        WeaponType.ThrowingAxe => 4f,
        WeaponType.Pistol => 5f,
        WeaponType.Rifle => 9f,
        WeaponType.Staff => 4f,
        WeaponType.Wand => 7f,
        WeaponType.Unarmed => 3f,
        _ => 5f
    };

    public static float CalculateCritChance(
        WeaponType weaponType,
        float increasedCritPercent = 0f,
        float flatCritBonus = 0f)
    {
        float baseCrit = GetBaseCritChance(weaponType);
        float finalCrit = baseCrit * (1f + increasedCritPercent / 100f) + flatCritBonus;
        return Math.Min(Math.Max(finalCrit, 0f), MaxCritChance);
    }

    public static float CalculateCritMultiplier(float bonusCritMulti = 0f)
    {
        return DefaultCritMultiplier + bonusCritMulti;
    }

    public static CritResult RollCrit(
        int baseDamage, WeaponType weaponType, Random rng,
        float increasedCritPercent = 0f, float flatCritBonus = 0f, float bonusCritMulti = 0f)
    {
        float critChance = CalculateCritChance(weaponType, increasedCritPercent, flatCritBonus);
        bool isCrit = (rng.NextDouble() * 100.0) < critChance;

        float critMulti = CalculateCritMultiplier(bonusCritMulti);
        int finalDamage = isCrit
            ? Math.Max(1, (int)(baseDamage * critMulti / 100f))
            : baseDamage;

        return new CritResult
        {
            BaseDamage = baseDamage,
            FinalDamage = finalDamage,
            IsCrit = isCrit,
            CritChance = critChance,
            CritMultiplier = critMulti
        };
    }
}

public struct CritResult
{
    public int BaseDamage;
    public int FinalDamage;
    public bool IsCrit;
    public float CritChance;
    public float CritMultiplier;
}
