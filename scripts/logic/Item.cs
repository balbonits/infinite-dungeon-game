namespace DungeonGame;

/// <summary>
/// Item definition — pure data, no Godot dependency. Testable with xUnit.
/// Items are identified by Id. Stats, price, and behavior are all configuration.
/// </summary>
public record ItemDef
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public ItemCategory Category { get; init; }
    public int BuyPrice { get; init; }
    public int SellPrice { get; init; }
    public int LevelRequirement { get; init; }
    public string IconPath { get; init; } = "";

    // Equipment stats (0 = not applicable)
    public int BonusStr { get; init; }
    public int BonusDex { get; init; }
    public int BonusSta { get; init; }
    public int BonusInt { get; init; }
    public int BonusHp { get; init; }
    public int BonusDamage { get; init; }

    // Consumable effect
    public int HealAmount { get; init; }
    public int ManaAmount { get; init; }

    // Quiver properties (Ranger)
    public string Element { get; init; } = "";
    public float ProjectileDamageMultiplier { get; init; } = 1.0f;
}

public enum ItemCategory
{
    Weapon,
    Armor,
    Accessory,
    Consumable,
    Quiver,
    Material,
    QuestItem,
}

/// <summary>
/// A stack of items in an inventory slot.
/// </summary>
public record ItemStack
{
    public ItemDef Item { get; init; } = null!;
    public int Count { get; init; } = 1;

    public bool IsStackable => Item.Category == ItemCategory.Consumable ||
                                Item.Category == ItemCategory.Material;
    public int MaxStack => IsStackable ? 99 : 1;
}
