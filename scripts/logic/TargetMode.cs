namespace DungeonGame;

/// <summary>
/// How an attack/skill selects its targets.
/// Every AttackConfig has a TargetMode — the combat system reads it
/// and resolves targets accordingly. No special-case branching per skill.
/// </summary>
public enum TargetMode
{
    /// <summary>Affects the caster only (buffs, self-heals, defensive auras).</summary>
    Self,

    /// <summary>Hits exactly one target — nearest or locked (basic attacks, single-target spells).</summary>
    SingleTarget,

    /// <summary>Hits all enemies in a radius around a point on the ground (meteor, fireball, ground slam).</summary>
    AreaOfEffect,

    /// <summary>Chains between multiple targets automatically (chain lightning, smart missiles).</summary>
    MultiTarget,

    /// <summary>Hits all enemies in a radius around the caster (wide swing, whirlwind, shockwave).</summary>
    PlayerCentricAoe,

    /// <summary>Hits all enemies in a line from caster to max range (laser, beam, charge attack).</summary>
    Line,

    /// <summary>Hits all enemies in a cone in front of the caster (breath attack, shotgun spread, cleave).</summary>
    Cone,

    /// <summary>Projectile tracks and follows a target (homing missile, seeking bolt). Slower but guaranteed hit.</summary>
    Homing,
}

