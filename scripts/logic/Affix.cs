namespace DungeonGame;

/// <summary>
/// An affix that can be applied to equipment via Blacksmith crafting.
/// Affixes are deterministic — player picks exact affix and pays materials + gold.
/// Max 3 prefix + 3 suffix per item.
/// Pure data — no Godot dependency.
/// </summary>
public record AffixDef
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public AffixType Type { get; init; }
    public AffixCategory Category { get; init; }
    public string StatModified { get; init; } = "";
    public int Tier { get; init; } = 1;
    public int MinItemLevel { get; init; } = 1;
    public float Value { get; init; }
    public bool IsPercent { get; init; }
    public int GoldCost { get; init; }
    public int MaterialCost { get; init; }
}

public enum AffixType
{
    Prefix,
    Suffix,
}

public enum AffixCategory
{
    Offensive,
    Defensive,
    Utility,
    Elemental,
}

/// <summary>
/// An applied affix instance on an item.
/// </summary>
public record AppliedAffix
{
    public string AffixId { get; init; } = "";
    public float Value { get; init; }
}
