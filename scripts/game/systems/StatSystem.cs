using System;

/// <summary>
/// Stat calculation with diminishing returns for any entity.
/// All methods are static and take EntityData as first parameter.
/// No entity-type branching — behavior differs through stat values only.
/// </summary>
public static class StatSystem
{
    /// <summary>
    /// Core diminishing returns formula: rawStat * (100 / (rawStat + 100)).
    /// Returns effective stat value as a float.
    /// </summary>
    public static float GetEffective(int rawStat)
    {
        return rawStat * (100f / (rawStat + 100f));
    }

    /// <summary>
    /// Calculate melee damage: base damage + STR contribution + equipment damage.
    /// </summary>
    public static int GetMeleeDamage(EntityData entity)
    {
        return entity.BaseDamage + (int)(GetEffective(entity.STR) * 0.5f) + entity.TotalDamage;
    }

    /// <summary>
    /// Calculate defense reduction as a 0-1 float based on TotalDefense with diminishing returns.
    /// </summary>
    public static float GetDefenseReduction(EntityData entity)
    {
        return GetEffective(entity.TotalDefense) / 100f;
    }

    /// <summary>
    /// Calculate max HP: 100 + Level * 8 + VIT * 3 + equipment HP bonuses.
    /// </summary>
    public static int GetMaxHP(EntityData entity)
    {
        int equipHPBonus = 0;
        foreach (var item in entity.Equipment.Values)
            equipHPBonus += item.HPBonus;

        return 100 + entity.Level * 8 + entity.VIT * 3 + equipHPBonus;
    }

    /// <summary>
    /// Calculate max MP: 50 + INT * 3 + equipment MP bonuses.
    /// </summary>
    public static int GetMaxMP(EntityData entity)
    {
        int equipMPBonus = 0;
        foreach (var item in entity.Equipment.Values)
            equipMPBonus += item.MPBonus;

        return 50 + entity.INT * 3 + equipMPBonus;
    }

    /// <summary>
    /// Calculate attack speed. Base speed modified by DEX — faster with more DEX.
    /// Lower return value = faster attacks (seconds between attacks).
    /// </summary>
    public static float GetAttackSpeed(EntityData entity)
    {
        return entity.AttackSpeed * (1f - GetEffective(entity.DEX) * 0.003f);
    }

    /// <summary>
    /// Calculate move speed. Base speed + small DEX bonus.
    /// </summary>
    public static float GetMoveSpeed(EntityData entity)
    {
        return entity.MoveSpeed + GetEffective(entity.DEX) * 0.02f;
    }

    /// <summary>
    /// Recalculate MaxHP and MaxMP from current stats and equipment.
    /// Clamps HP/MP if they exceed the new maximums.
    /// </summary>
    public static void RecalculateDerived(EntityData entity)
    {
        entity.MaxHP = GetMaxHP(entity);
        entity.MaxMP = GetMaxMP(entity);

        if (entity.HP > entity.MaxHP)
            entity.HP = entity.MaxHP;
        if (entity.MP > entity.MaxMP)
            entity.MP = entity.MaxMP;
    }
}
