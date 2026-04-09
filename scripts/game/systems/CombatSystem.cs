using System;

/// <summary>
/// Damage result returned from combat calculations.
/// </summary>
public struct DamageResult
{
    public int RawDamage;
    public int MitigatedDamage;
    public bool IsCrit;
    public bool TargetDied;
}

/// <summary>
/// Damage calculation for all combat — same function for player attacking enemy
/// and enemy attacking player. No entity-type branching.
/// </summary>
public static class CombatSystem
{
    private static readonly Random Rng = new();

    /// <summary>
    /// Deal damage from attacker to target. Calculates raw damage, crit, defense,
    /// applies damage via VitalSystem. Returns full result.
    /// </summary>
    public static DamageResult DealDamage(EntityData attacker, EntityData target)
    {
        // 1. Raw damage from attacker's TotalDamage
        int rawDamage = attacker.TotalDamage;

        // 2. Crit roll: 15% chance, 1.5x multiplier
        bool isCrit = Rng.NextDouble() < 0.15;
        if (isCrit)
            rawDamage = (int)(rawDamage * 1.5f);

        // 3. Apply target defense reduction
        float defenseReduction = StatSystem.GetDefenseReduction(target);
        int mitigated = rawDamage - (int)(rawDamage * defenseReduction);

        // 4. Minimum 1 damage
        mitigated = Math.Max(1, mitigated);

        // 5. Apply damage via VitalSystem
        var (_, died) = VitalSystem.TakeDamage(target, mitigated);

        // 6. Return result
        return new DamageResult
        {
            RawDamage = rawDamage,
            MitigatedDamage = mitigated,
            IsCrit = isCrit,
            TargetDied = died
        };
    }

    /// <summary>
    /// Check if an attacker can attack (must be alive).
    /// </summary>
    public static bool CanAttack(EntityData attacker)
    {
        return VitalSystem.IsAlive(attacker);
    }

    /// <summary>
    /// Preview expected damage without applying it. Shows non-crit damage after defense.
    /// </summary>
    public static int GetDamagePreview(EntityData attacker, EntityData target)
    {
        int rawDamage = attacker.TotalDamage;
        float defenseReduction = StatSystem.GetDefenseReduction(target);
        int mitigated = rawDamage - (int)(rawDamage * defenseReduction);
        return Math.Max(1, mitigated);
    }
}
