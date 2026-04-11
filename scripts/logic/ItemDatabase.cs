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
        });
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
