using Godot;

namespace DungeonGame;

/// <summary>
/// Default attack configurations per class.
/// Each class has a primary attack. Mage has a secondary (staff melee fallback).
/// Weapons and skills can override these at runtime.
/// </summary>
public static class ClassAttacks
{
    // Warrior: melee sword slash
    public static readonly AttackConfig WarriorSlash = new()
    {
        Range = Constants.ClassCombat.WarriorMeleeRange,
        Cooldown = Constants.ClassCombat.WarriorCooldown,
        DamageMultiplier = 1.0f,
        IsProjectile = false,
        Effect = VisualEffect.Slash,
        EffectColor = new Color("f5c86b"),
    };

    // Ranger: arrow shot (projectile)
    public static readonly AttackConfig RangerArrowShot = new()
    {
        Range = Constants.ClassCombat.RangerProjectileRange,
        Cooldown = Constants.ClassCombat.RangerCooldown,
        DamageMultiplier = 1.0f,
        IsProjectile = true,
        ProjectileSpeed = Constants.ClassCombat.ArrowSpeed,
        ProjectileTexture = Constants.Assets.ArrowProjectile,
        ProjectileScale = Constants.ClassCombat.ArrowScale,
        Effect = VisualEffect.Projectile,
    };

    // Mage: magic bolt (projectile, higher damage, slower cooldown)
    public static readonly AttackConfig MageMagicBolt = new()
    {
        Range = Constants.ClassCombat.MageSpellRange,
        Cooldown = Constants.ClassCombat.MageSpellCooldown,
        DamageMultiplier = 1.3f,
        IsProjectile = true,
        ProjectileSpeed = Constants.ClassCombat.MagicBoltSpeed,
        ProjectileTexture = Constants.Assets.MagicBoltProjectile,
        ProjectileScale = Constants.ClassCombat.MagicBoltScale,
        ProjectileTint = new Color("4AE8E8"),
        Effect = VisualEffect.Projectile,
        EffectColor = new Color("4AE8E8"),
    };

    // Mage fallback: staff melee (when enemy is within melee range)
    public static readonly AttackConfig MageStaffMelee = new()
    {
        Range = Constants.ClassCombat.MageMeleeRange,
        Cooldown = Constants.ClassCombat.MageMeleeCooldown,
        DamageMultiplier = 0.8f,
        IsProjectile = false,
        Effect = VisualEffect.Slash,
        EffectColor = new Color("9B6BFF"),
    };

    /// <summary>
    /// Get the primary attack config for a class.
    /// </summary>
    public static AttackConfig GetPrimary(PlayerClass playerClass) => playerClass switch
    {
        PlayerClass.Warrior => WarriorSlash,
        PlayerClass.Ranger => RangerArrowShot,
        PlayerClass.Mage => MageMagicBolt,
        _ => WarriorSlash,
    };

    /// <summary>
    /// Get the melee fallback for a class (null if class has no fallback).
    /// </summary>
    public static AttackConfig? GetMeleeFallback(PlayerClass playerClass) => playerClass switch
    {
        PlayerClass.Mage => MageStaffMelee,
        _ => null,
    };
}
