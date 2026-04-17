namespace DungeonGame;

/// <summary>
/// Immutable definition of an Ability (active combat action). Pure data — no state, no Godot dependency.
/// Each Ability belongs to exactly one parent Mastery. Using an Ability in combat grants XP
/// to both the Ability and its parent Mastery.
/// </summary>
public record AbilityDef
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public string ParentMasteryId { get; init; } = "";
    public string CategoryId { get; init; } = "";
    public PlayerClass Class { get; init; }

    /// <summary>Base XP earned per use of this ability.</summary>
    public int BaseXpPerUse { get; init; } = 10;

    /// <summary>Mana cost to cast. 0 = free.</summary>
    public int ManaCost { get; init; }

    /// <summary>Cooldown in seconds between casts.</summary>
    public float Cooldown { get; init; } = 2.0f;

    /// <summary>Attack config for combat execution. Null = non-combat ability.</summary>
    public AttackConfig? CombatConfig { get; init; }
}
