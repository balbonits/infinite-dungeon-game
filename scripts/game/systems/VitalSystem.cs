using System;

/// <summary>
/// HP and MP management for any entity.
/// All methods are static and take EntityData as first parameter.
/// No entity-type branching — behavior differs through stat values only.
/// </summary>
public static class VitalSystem
{
    /// <summary>
    /// Apply damage to an entity. Clamps HP to 0 and sets IsDead if HP reaches 0.
    /// </summary>
    public static (int actualDamage, bool died) TakeDamage(EntityData entity, int amount)
    {
        if (amount < 0) amount = 0;

        int actualDamage = Math.Min(amount, entity.HP);
        entity.HP -= actualDamage;

        bool died = false;
        if (entity.HP <= 0)
        {
            entity.HP = 0;
            entity.IsDead = true;
            died = true;
        }

        return (actualDamage, died);
    }

    /// <summary>
    /// Heal an entity's HP. Clamps to MaxHP.
    /// </summary>
    public static int Heal(EntityData entity, int amount)
    {
        if (amount < 0) amount = 0;

        int actualHealed = Math.Min(amount, entity.MaxHP - entity.HP);
        entity.HP += actualHealed;
        return actualHealed;
    }

    /// <summary>
    /// Restore an entity's MP. Clamps to MaxMP.
    /// </summary>
    public static int RestoreMP(EntityData entity, int amount)
    {
        if (amount < 0) amount = 0;

        int actualRestored = Math.Min(amount, entity.MaxMP - entity.MP);
        entity.MP += actualRestored;
        return actualRestored;
    }

    /// <summary>
    /// Spend MP if the entity has enough. Returns false if MP is insufficient.
    /// </summary>
    public static bool SpendMP(EntityData entity, int amount)
    {
        if (entity.MP < amount)
            return false;

        entity.MP -= amount;
        return true;
    }

    /// <summary>
    /// Check if an entity is alive (!IsDead and HP > 0).
    /// </summary>
    public static bool IsAlive(EntityData entity)
    {
        return !entity.IsDead && entity.HP > 0;
    }

    /// <summary>
    /// Revive a dead entity. Resets IsDead and sets HP to (MaxHP * hpPercent).
    /// </summary>
    public static void Revive(EntityData entity, float hpPercent = 0.5f)
    {
        entity.IsDead = false;
        entity.HP = Math.Max(1, (int)(entity.MaxHP * hpPercent));
    }

    /// <summary>
    /// Regenerate HP and MP over time. Call each frame with delta time.
    /// </summary>
    public static void RegenTick(EntityData entity, float hpPerSec, float mpPerSec, float delta)
    {
        if (entity.IsDead) return;

        int hpRegen = (int)(hpPerSec * delta);
        int mpRegen = (int)(mpPerSec * delta);

        if (hpRegen > 0)
            Heal(entity, hpRegen);
        if (mpRegen > 0)
            RestoreMP(entity, mpRegen);
    }
}
