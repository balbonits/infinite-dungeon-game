namespace DungeonGame;

/// <summary>
/// Immutable definition of a skill. Pure data — no state, no Godot dependency.
/// Skills are organized: Category → BaseSkill → SpecificSkill.
/// Categories are organizational labels only (no level). Base and Specific skills
/// have levels, XP, and passive bonuses.
/// </summary>
public record SkillDef
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public string CategoryId { get; init; } = "";
    public string? ParentBaseSkillId { get; init; }
    public SkillType Type { get; init; }
    public PlayerClass Class { get; init; }
    public PassiveBonusType PassiveType { get; init; }
    public float PassiveMultiplier { get; init; }

    /// <summary>Base XP earned per use of this skill.</summary>
    public int BaseXpPerUse { get; init; } = 10;
}

public enum SkillType
{
    Base,
    Specific,
}

/// <summary>
/// Types of passive bonuses a base skill can provide.
/// Each type has a different multiplier per the spec.
/// </summary>
public enum PassiveBonusType
{
    Damage,      // multiplier 1.5
    AttackSpeed, // multiplier 0.8
    Defense,     // multiplier 1.2
    Chance,      // multiplier 0.5 (crit, dodge, block)
    Regen,       // multiplier 0.3
}
