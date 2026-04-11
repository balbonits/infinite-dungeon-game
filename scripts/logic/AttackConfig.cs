using Godot;

namespace DungeonGame;

/// <summary>
/// Data-driven attack definition. Melee and ranged are the same system —
/// the only difference is whether a projectile is spawned or damage is applied instantly.
///
/// IsProjectile = false → instant hit at range (melee slash, staff smack)
/// IsProjectile = true  → spawn projectile that travels to target (arrow, magic bolt)
///
/// Weapons, skills, and quiver imbues all produce AttackConfigs.
/// The combat system reads one and executes — no class-specific branching.
/// </summary>
public record AttackConfig
{
    // --- Core ---
    public float Range { get; init; }
    public float Cooldown { get; init; }
    public float DamageMultiplier { get; init; } = 1.0f;

    // --- Projectile (null = instant melee hit) ---
    public bool IsProjectile { get; init; }
    public float ProjectileSpeed { get; init; }
    public string ProjectileTexture { get; init; } = "";
    public float ProjectileScale { get; init; } = 1.0f;
    public Color? ProjectileTint { get; init; }

    // --- Visual feedback ---
    public VisualEffect Effect { get; init; } = VisualEffect.Slash;
    public Color EffectColor { get; init; } = new("f5c86b");
}

/// <summary>
/// Visual effect type played on attack hit.
/// </summary>
public enum VisualEffect
{
    Slash,
    Projectile,
    None,
}
