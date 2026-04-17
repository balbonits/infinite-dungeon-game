namespace DungeonGame;

/// <summary>
/// Immutable definition of a Skill mastery (passive). Pure data — no state, no Godot dependency.
/// Masteries provide passive bonuses and gate access to child Abilities.
/// </summary>
public record MasteryDef
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public string CategoryId { get; init; } = "";
    public PlayerClass Class { get; init; }
    public PassiveBonusType PassiveType { get; init; }
    public float PassiveMultiplier { get; init; }

    /// <summary>Base XP earned when any child Ability is used.</summary>
    public int BaseXpPerUse { get; init; } = 5;
}

/// <summary>
/// Types of passive bonuses a mastery can provide.
/// Each type has a different multiplier per the spec (skills.md).
/// </summary>
public enum PassiveBonusType
{
    Damage,      // multiplier 1.5
    AttackSpeed, // multiplier 0.8
    Defense,     // multiplier 1.2
    Chance,      // multiplier 0.5 (crit, dodge, block)
    Regen,       // multiplier 0.3
}
