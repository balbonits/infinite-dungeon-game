using System.Collections.Generic;

namespace DungeonGame;

/// <summary>
/// Central item registry. All items defined here, looked up by Id.
/// </summary>
public static class ItemDatabase
{
    private static readonly Dictionary<string, ItemDef> Items = new();

    static ItemDatabase()
    {
        // --- Consumables ---
        Register(new ItemDef
        {
            Id = "potion_hp_small",
            Name = "Small Health Potion",
            Description = "Restores 30 HP.",
            Category = ItemCategory.Consumable,
            BuyPrice = 25,
            SellPrice = 10,
            HealAmount = 30,
            IconPath = "",
        });
        Register(new ItemDef
        {
            Id = "potion_hp_medium",
            Name = "Health Potion",
            Description = "Restores 80 HP.",
            Category = ItemCategory.Consumable,
            BuyPrice = 75,
            SellPrice = 30,
            HealAmount = 80,
            IconPath = "",
        });
        Register(new ItemDef
        {
            Id = "idol_sacrificial",
            Name = "Sacrificial Idol",
            Description = "Negates backpack loss on death. Consumed on use.",
            Category = ItemCategory.Consumable,
            BuyPrice = 200,
            SellPrice = 80,
            IconPath = "",
        });

        // --- Quivers (Ranger) ---
        Register(new ItemDef
        {
            Id = "quiver_basic",
            Name = "Basic Quiver",
            Description = "Standard arrows. No special effect.",
            Category = ItemCategory.Quiver,
            BuyPrice = 50,
            SellPrice = 20,
            ProjectileDamageMultiplier = 1.0f,
        });
        Register(new ItemDef
        {
            Id = "quiver_fire",
            Name = "Fire Quiver",
            Description = "Arrows imbued with flame. +20% damage.",
            Category = ItemCategory.Quiver,
            BuyPrice = 150,
            SellPrice = 60,
            Element = "fire",
            ProjectileDamageMultiplier = 1.2f,
        });
        Register(new ItemDef
        {
            Id = "quiver_poison",
            Name = "Poison Quiver",
            Description = "Arrows tipped with venom. Applies poison.",
            Category = ItemCategory.Quiver,
            BuyPrice = 120,
            SellPrice = 50,
            Element = "poison",
            ProjectileDamageMultiplier = 1.0f,
        });

        // --- Weapons ---
        Register(new ItemDef
        {
            Id = "sword_iron",
            Name = "Iron Sword",
            Description = "A sturdy iron blade.",
            Category = ItemCategory.Weapon,
            BuyPrice = 100,
            SellPrice = 40,
            LevelRequirement = 1,
            BonusDamage = 3,
            BonusStr = 1,
            Slot = EquipSlot.MainHand,
            ClassAffinity = PlayerClass.Warrior,
        });
        Register(new ItemDef
        {
            Id = "staff_oak",
            Name = "Oak Staff",
            Description = "A simple wooden staff for channeling magic.",
            Category = ItemCategory.Weapon,
            BuyPrice = 80,
            SellPrice = 35,
            LevelRequirement = 1,
            BonusDamage = 2,
            BonusInt = 2,
            Slot = EquipSlot.MainHand,
            ClassAffinity = PlayerClass.Mage,
        });
        Register(new ItemDef
        {
            Id = "bow_short",
            Name = "Short Bow",
            Description = "A compact bow for quick shots.",
            Category = ItemCategory.Weapon,
            BuyPrice = 90,
            SellPrice = 38,
            LevelRequirement = 1,
            BonusDamage = 2,
            BonusDex = 1,
            Slot = EquipSlot.MainHand,
            ClassAffinity = PlayerClass.Ranger,
        });

        // --- Starting gear (SYS-11) — per docs/systems/equipment.md §Starting Equipment ---
        Register(new ItemDef
        {
            Id = "sword_iron_short",
            Name = "Iron Short Sword",
            Description = "A basic short sword. Warrior starting weapon.",
            Category = ItemCategory.Weapon,
            BuyPrice = 80,
            SellPrice = 32,
            LevelRequirement = 1,
            BonusDamage = 2,
            Slot = EquipSlot.MainHand,
            ClassAffinity = PlayerClass.Warrior,
        });
        Register(new ItemDef
        {
            Id = "staff_short",
            Name = "Short Staff",
            Description = "A novice mage's wooden staff. Mage starting weapon.",
            Category = ItemCategory.Weapon,
            BuyPrice = 70,
            SellPrice = 28,
            LevelRequirement = 1,
            BonusDamage = 1,
            BonusInt = 1,
            Slot = EquipSlot.MainHand,
            ClassAffinity = PlayerClass.Mage,
        });
        Register(new ItemDef
        {
            Id = "shield_wooden_small",
            Name = "Wooden Small Shield",
            Description = "A rough wooden buckler. Warrior starting off-hand.",
            Category = ItemCategory.Shield,
            BuyPrice = 60,
            SellPrice = 24,
            LevelRequirement = 1,
            BonusSta = 1,
            Slot = EquipSlot.OffHand,
            ClassAffinity = PlayerClass.Warrior,
        });
        Register(new ItemDef
        {
            Id = "spellbook_basic",
            Name = "Basic Spellbook",
            Description = "A thin grimoire with starter incantations. Mage starting off-hand.",
            Category = ItemCategory.Spellbook,
            BuyPrice = 60,
            SellPrice = 24,
            LevelRequirement = 1,
            BonusInt = 1,
            Slot = EquipSlot.OffHand,
            ClassAffinity = PlayerClass.Mage,
        });
        Register(new ItemDef
        {
            Id = "quiver_iron_arrows",
            Name = "Iron Arrows Quiver",
            Description = "A quiver of simple iron-tipped arrows. Ranger starting ammo.",
            Category = ItemCategory.Quiver,
            BuyPrice = 40,
            SellPrice = 16,
            LevelRequirement = 1,
            ProjectileDamageMultiplier = 1.0f,
            Slot = EquipSlot.Ammo,
            ClassAffinity = PlayerClass.Ranger,
        });

        // Tag existing quivers with their Slot so they can be swapped at runtime.
        UpdateSlot("quiver_basic", EquipSlot.Ammo);
        UpdateSlot("quiver_fire", EquipSlot.Ammo);
        UpdateSlot("quiver_poison", EquipSlot.Ammo);

        // --- Minimal armor / accessory sample set (for testing & early loot) ---
        // Note: a full base-item catalog is deferred to ITEM-01.
        Register(new ItemDef
        {
            Id = "helm_leather_cap",
            Name = "Leather Cap",
            Description = "A padded leather cap.",
            Category = ItemCategory.Head,
            BuyPrice = 50,
            SellPrice = 20,
            LevelRequirement = 1,
            BonusSta = 1,
            Slot = EquipSlot.Head,
        });
        Register(new ItemDef
        {
            Id = "body_leather_vest",
            Name = "Leather Vest",
            Description = "Basic leather body armor.",
            Category = ItemCategory.Body,
            BuyPrice = 90,
            SellPrice = 36,
            LevelRequirement = 1,
            BonusSta = 2,
            Slot = EquipSlot.Body,
        });
        Register(new ItemDef
        {
            Id = "arms_leather_braces",
            Name = "Leather Braces",
            Description = "Simple arm guards.",
            Category = ItemCategory.Arms,
            BuyPrice = 40,
            SellPrice = 16,
            LevelRequirement = 1,
            BonusSta = 1,
            Slot = EquipSlot.Arms,
        });
        Register(new ItemDef
        {
            Id = "legs_leather_breeches",
            Name = "Leather Breeches",
            Description = "Lightweight leg armor.",
            Category = ItemCategory.Legs,
            BuyPrice = 50,
            SellPrice = 20,
            LevelRequirement = 1,
            BonusSta = 1,
            Slot = EquipSlot.Legs,
        });
        Register(new ItemDef
        {
            Id = "feet_leather_boots",
            Name = "Leather Boots",
            Description = "Soft leather boots.",
            Category = ItemCategory.Feet,
            BuyPrice = 40,
            SellPrice = 16,
            LevelRequirement = 1,
            BonusDex = 1,
            Slot = EquipSlot.Feet,
        });
        Register(new ItemDef
        {
            Id = "neck_copper_chain",
            Name = "Copper Chain",
            Description = "A simple copper chain.",
            Category = ItemCategory.Neck,
            BuyPrice = 60,
            SellPrice = 24,
            LevelRequirement = 1,
            BonusHp = 5,
            Slot = EquipSlot.Neck,
        });
        Register(new ItemDef
        {
            Id = "ring_copper",
            Name = "Copper Ring",
            Description = "A plain copper ring.",
            Category = ItemCategory.Ring,
            BuyPrice = 50,
            SellPrice = 20,
            LevelRequirement = 1,
            BonusStr = 1,
            Slot = EquipSlot.Ring,
        });
    }

    private static void UpdateSlot(string id, EquipSlot slot)
    {
        if (Items.TryGetValue(id, out var def))
            Items[id] = def with { Slot = slot };
    }

    private static void Register(ItemDef item)
    {
        Items[item.Id] = item;
    }

    public static ItemDef? Get(string id) => Items.GetValueOrDefault(id);

    public static IEnumerable<ItemDef> GetByCategory(ItemCategory category)
    {
        foreach (var item in Items.Values)
        {
            if (item.Category == category)
                yield return item;
        }
    }

    public static IEnumerable<ItemDef> All => Items.Values;
}
