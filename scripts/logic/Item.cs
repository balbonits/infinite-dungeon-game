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

    // Equipment slotting (SYS-11). Defaults to None (not equippable).
    public EquipSlot Slot { get; init; } = EquipSlot.None;

    // Class affinity — null means generic gear (no class bonus). When the player's
    // class matches, all stat bonuses on this item are multiplied by 1.25.
    public PlayerClass? ClassAffinity { get; init; }

    /// <summary>
    /// Catalog tier (1..5) — maps to floor brackets per item-generation.md § Floor
    /// Brackets = Tiers. 0 = untiered (e.g., universal consumables, signature materials).
    /// Used by MonsterDropTable for floor-gated slot-roll selection.
    /// </summary>
    public int Tier { get; init; }
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
    // SYS-11 equipment categories
    Head,
    Body,
    Arms,
    Legs,
    Feet,
    Neck,
    Ring,
    Shield,
    Spellbook,
    DefensiveMelee,
}

/// <summary>
/// Equipment slot types. Rings have 10 sub-slots (ringIndex 0..9); all other slots are single.
/// See docs/systems/equipment.md.
/// </summary>
public enum EquipSlot
{
    None,
    Head,
    Body,
    Arms,
    Legs,
    Feet,
    Neck,
    Ring,
    MainHand,
    OffHand,
    Ammo,
}

/// <summary>
/// A stack of items in an inventory slot. Unlimited quantity per slot — one item type per slot per storage.
/// See docs/inventory/items.md for stacking rules.
/// </summary>
public record ItemStack
{
    public ItemDef Item { get; init; } = null!;
    public long Count { get; init; } = 1;

    /// <summary>
    /// Locked items cannot be Sold or Dropped (and cannot be accidentally unequipped).
    /// Lock does NOT protect from death-loss — see docs/systems/death.md.
    /// </summary>
    public bool Locked { get; init; }
}
