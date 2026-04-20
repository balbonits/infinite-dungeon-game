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

    /// <summary>
    /// Ring combat focus — base-item identity for combat-ring catalog (Precision /
    /// Haste / Evasion / Bulwark). Feeds COMBAT-01's ring-focus formulas. None
    /// for stat-focus rings (those use BonusStr/Dex/Sta/Int) and every non-ring
    /// item. See docs/systems/combat-equipment-integration.md §7.
    /// </summary>
    public RingFocus RingFocus { get; init; } = RingFocus.None;
}

/// <summary>
/// Combat-ring focus category per COMBAT-01. Determines which soft-capped
/// combat stat a ring contributes to (and is computed as Tier × per-tier%
/// at read-time). Stat-focus rings (Str/Dex/Sta/Int) don't use this field —
/// they carry flat bonuses on the BonusStr/etc fields instead.
/// </summary>
public enum RingFocus
{
    None = 0,
    Crit = 1,      // Precision rings — +2% raw crit per tier
    Haste = 2,     // Haste rings — +3% raw attack speed per tier
    Dodge = 3,     // Evasion rings — +1.5% raw dodge per tier
    Block = 4,     // Bulwark rings — +2% raw block per tier
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
