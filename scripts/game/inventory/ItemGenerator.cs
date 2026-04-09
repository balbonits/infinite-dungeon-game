using System;
using System.Collections.Generic;

/// <summary>
/// Static item generation and loot drop system.
/// Generates base equipment, materials, consumables, and loot rolls.
/// All items drop without affixes — affixes are added by the Blacksmith.
/// </summary>
public static class ItemGenerator
{
    // ==================== EQUIPMENT GENERATION ====================

    /// <summary>
    /// Generate a random piece of base equipment appropriate for the given floor.
    /// Items drop without affixes (Blacksmith adds those).
    /// </summary>
    public static ItemData GenerateEquipment(int floorNumber, Random rng)
    {
        int itemLevel = Math.Max(1, floorNumber);
        ItemQuality quality = RollQuality(floorNumber, rng);

        // Pick random equipment category: weapon or armor
        bool isWeapon = rng.Next(2) == 0;

        if (isWeapon)
            return GenerateWeapon(itemLevel, quality, rng);
        else
            return GenerateArmor(itemLevel, quality, rng);
    }

    private static ItemData GenerateWeapon(int itemLevel, ItemQuality quality, Random rng)
    {
        // Weapon base damage scales with item level.
        // Formula derived from spec table:
        //   Level 1:   5-8     Level 10:  15-22
        //   Level 25:  35-50   Level 50:  70-100
        //   Level 100: 140-200
        // Linear scaling: minDmg ~ 1.4*level + 3.6, maxDmg ~ 2.0*level + 6.0
        // Simplified: minDmg = max(5, (int)(1.4 * level + 3.6))
        //             maxDmg = max(8, (int)(2.0 * level + 6.0))
        int minDmg = Math.Max(5, (int)(1.4 * itemLevel + 3.6));
        int maxDmg = Math.Max(8, (int)(2.0 * itemLevel + 6.0));

        int baseDamage = rng.Next(minDmg, maxDmg + 1);

        // Apply quality multiplier (spec: Superior +10-20%, Elite +25-40%)
        baseDamage = ApplyQualityBonus(baseDamage, quality, rng);

        // Pick a weapon subtype name
        string name = PickWeaponName(itemLevel, rng);

        return new ItemData
        {
            Name = name,
            Type = ItemType.Weapon,
            Slot = EquipSlot.MainHand,
            Damage = baseDamage,
            Defense = 0,
            HPBonus = 0,
            MPBonus = 0,
            Value = CalculateValue(itemLevel, quality),
            Stackable = false,
            StackCount = 1,
            Description = $"A {quality.ToString().ToLower()} weapon (iLvl {itemLevel})",
            ItemLevel = itemLevel,
            Quality = quality,
        };
    }

    private static ItemData GenerateArmor(int itemLevel, ItemQuality quality, Random rng)
    {
        // Pick a random armor slot
        EquipSlot[] armorSlots = { EquipSlot.Head, EquipSlot.Body, EquipSlot.Legs, EquipSlot.Feet, EquipSlot.OffHand };
        EquipSlot slot = armorSlots[rng.Next(armorSlots.Length)];

        // Armor base defense scales with item level.
        // Roughly: minDef ~ 0.8*level + 2, maxDef ~ 1.2*level + 4
        int minDef = Math.Max(2, (int)(0.8 * itemLevel + 2));
        int maxDef = Math.Max(4, (int)(1.2 * itemLevel + 4));

        int baseDefense = rng.Next(minDef, maxDef + 1);

        // Body armor gets a bonus (primary slot)
        if (slot == EquipSlot.Body)
            baseDefense = (int)(baseDefense * 1.3);

        // Apply quality multiplier
        baseDefense = ApplyQualityBonus(baseDefense, quality, rng);

        string name = PickArmorName(slot, itemLevel, rng);

        return new ItemData
        {
            Name = name,
            Type = ItemType.Armor,
            Slot = slot,
            Damage = 0,
            Defense = baseDefense,
            HPBonus = 0,
            MPBonus = 0,
            Value = CalculateValue(itemLevel, quality),
            Stackable = false,
            StackCount = 1,
            Description = $"A {quality.ToString().ToLower()} armor piece (iLvl {itemLevel})",
            ItemLevel = itemLevel,
            Quality = quality,
        };
    }

    // ==================== MATERIAL GENERATION ====================

    /// <summary>
    /// Generate a crafting material appropriate for the given floor depth.
    /// </summary>
    public static ItemData GenerateMaterial(int floorNumber, Random rng)
    {
        int itemLevel = Math.Max(1, floorNumber);

        // Material tier based on floor depth
        string name;
        int value;
        if (floorNumber >= 75)
        {
            string[] rare = { "Enchanted Crystal", "Dragon Scale", "Mythril Shard" };
            name = rare[rng.Next(rare.Length)];
            value = 80 + itemLevel;
        }
        else if (floorNumber >= 50)
        {
            string[] high = { "Dark Iron Ore", "Wyvern Bone", "Arcane Dust" };
            name = high[rng.Next(high.Length)];
            value = 50 + itemLevel;
        }
        else if (floorNumber >= 25)
        {
            string[] mid = { "Steel Ingot", "Monster Hide", "Fire Crystal" };
            name = mid[rng.Next(mid.Length)];
            value = 25 + itemLevel;
        }
        else if (floorNumber >= 10)
        {
            string[] low = { "Iron Ore", "Monster Bone", "Rough Gem" };
            name = low[rng.Next(low.Length)];
            value = 10 + itemLevel;
        }
        else
        {
            string[] basic = { "Scrap Metal", "Tattered Hide", "Bone Fragment" };
            name = basic[rng.Next(basic.Length)];
            value = 5 + itemLevel;
        }

        return new ItemData
        {
            Name = name,
            Type = ItemType.Material,
            Slot = EquipSlot.None,
            Damage = 0,
            Defense = 0,
            HPBonus = 0,
            MPBonus = 0,
            Value = value,
            Stackable = true,
            StackCount = 1,
            Description = $"A crafting material (floor {floorNumber})",
            ItemLevel = itemLevel,
            Quality = ItemQuality.Normal,
        };
    }

    // ==================== CONSUMABLE GENERATION ====================

    /// <summary>
    /// Generate a random consumable item.
    /// </summary>
    public static ItemData GenerateConsumable(Random rng)
    {
        int roll = rng.Next(100);

        if (roll < 45) // 45% Health Potion
        {
            return new ItemData
            {
                Name = "Health Potion",
                Type = ItemType.Consumable,
                Slot = EquipSlot.None,
                Damage = 0,
                Defense = 0,
                HPBonus = 50,
                MPBonus = 0,
                Value = 25,
                Stackable = true,
                StackCount = 1,
                Description = "Restores 50 HP",
                ItemLevel = 0,
                Quality = ItemQuality.Normal,
            };
        }
        else if (roll < 85) // 40% Mana Potion
        {
            return new ItemData
            {
                Name = "Mana Potion",
                Type = ItemType.Consumable,
                Slot = EquipSlot.None,
                Damage = 0,
                Defense = 0,
                HPBonus = 0,
                MPBonus = 30,
                Value = 25,
                Stackable = true,
                StackCount = 1,
                Description = "Restores 30 MP",
                ItemLevel = 0,
                Quality = ItemQuality.Normal,
            };
        }
        else // 15% Sacrificial Idol
        {
            return new ItemData
            {
                Name = "Sacrificial Idol",
                Type = ItemType.Consumable,
                Slot = EquipSlot.None,
                Damage = 0,
                Defense = 0,
                HPBonus = 0,
                MPBonus = 0,
                Value = 200,
                Stackable = true,
                StackCount = 1,
                Description = "Negates backpack item loss on death",
                ItemLevel = 0,
                Quality = ItemQuality.Normal,
            };
        }
    }

    // ==================== LOOT DROP ROLL ====================

    /// <summary>
    /// Roll for a loot drop from a killed monster.
    /// Returns null if nothing drops. Equipment and material drops are independent rolls.
    /// If both hit, returns the equipment (material is secondary — caller can roll separately).
    /// </summary>
    public static ItemData? RollLootDrop(int monsterTier, int floorNumber, Random rng)
    {
        // Equipment drop check
        double baseRate = monsterTier switch
        {
            1 => 0.08,
            2 => 0.12,
            3 => 0.18,
            _ => 0.08,
        };
        double floorBonus = Math.Min(floorNumber * 0.001, 0.05); // cap at +5%
        double dropChance = baseRate + floorBonus;

        if (rng.NextDouble() < dropChance)
            return GenerateEquipment(floorNumber, rng);

        // Independent material drop check (25% flat)
        if (rng.NextDouble() < 0.25)
            return GenerateMaterial(floorNumber, rng);

        return null;
    }

    // ==================== CRATE / CONTAINER LOOT ====================

    /// <summary>
    /// Generate loot from a crate or container. Returns 1-3 items.
    /// Mix of materials with a small chance of equipment.
    /// </summary>
    public static List<ItemData> GenerateCrateLoot(int floorNumber, Random rng)
    {
        int itemCount = rng.Next(1, 4); // 1-3 items
        var loot = new List<ItemData>(itemCount);

        for (int i = 0; i < itemCount; i++)
        {
            // 15% chance for equipment, 85% materials
            if (rng.NextDouble() < 0.15)
                loot.Add(GenerateEquipment(floorNumber, rng));
            else
                loot.Add(GenerateMaterial(floorNumber, rng));
        }

        return loot;
    }

    // ==================== QUALITY ROLL ====================

    /// <summary>
    /// Roll item quality based on floor depth.
    /// Follows the spec quality distribution table.
    /// </summary>
    private static ItemQuality RollQuality(int floorNumber, Random rng)
    {
        int roll = rng.Next(100);

        if (floorNumber >= 100)
        {
            // Normal 15%, Superior 50%, Elite 35%
            if (roll < 15) return ItemQuality.Normal;
            if (roll < 65) return ItemQuality.Superior;
            return ItemQuality.Elite;
        }
        if (floorNumber >= 75)
        {
            // Normal 25%, Superior 50%, Elite 25%
            if (roll < 25) return ItemQuality.Normal;
            if (roll < 75) return ItemQuality.Superior;
            return ItemQuality.Elite;
        }
        if (floorNumber >= 50)
        {
            // Normal 40%, Superior 45%, Elite 15%
            if (roll < 40) return ItemQuality.Normal;
            if (roll < 85) return ItemQuality.Superior;
            return ItemQuality.Elite;
        }
        if (floorNumber >= 25)
        {
            // Normal 60%, Superior 35%, Elite 5%
            if (roll < 60) return ItemQuality.Normal;
            if (roll < 95) return ItemQuality.Superior;
            return ItemQuality.Elite;
        }
        if (floorNumber >= 10)
        {
            // Normal 80%, Superior 20%, Elite 0%
            if (roll < 80) return ItemQuality.Normal;
            return ItemQuality.Superior;
        }

        // Floor 1-9: 100% Normal
        return ItemQuality.Normal;
    }

    // ==================== HELPERS ====================

    /// <summary>
    /// Apply quality bonus to a base stat value.
    /// Superior: +10-20%, Elite: +25-40%
    /// </summary>
    private static int ApplyQualityBonus(int baseStat, ItemQuality quality, Random rng)
    {
        if (quality == ItemQuality.Normal)
            return baseStat;

        double multiplier;
        if (quality == ItemQuality.Superior)
        {
            // +10-20%
            multiplier = 1.0 + (rng.Next(10, 21) / 100.0);
        }
        else // Elite
        {
            // +25-40%
            multiplier = 1.0 + (rng.Next(25, 41) / 100.0);
        }

        return Math.Max(baseStat + 1, (int)(baseStat * multiplier));
    }

    /// <summary>
    /// Calculate item gold value based on item level and quality.
    /// </summary>
    private static int CalculateValue(int itemLevel, ItemQuality quality)
    {
        int baseValue = 10 + itemLevel * 3;
        int qualityMult = quality switch
        {
            ItemQuality.Superior => 2,
            ItemQuality.Elite => 4,
            _ => 1,
        };
        return baseValue * qualityMult;
    }

    private static string PickWeaponName(int itemLevel, Random rng)
    {
        string[] low = { "Rusty Sword", "Wooden Club", "Dull Dagger", "Crude Axe" };
        string[] mid = { "Iron Sword", "Steel Mace", "Short Bow", "War Hammer" };
        string[] high = { "Tempered Blade", "Dark Halberd", "Longbow", "Battle Axe" };
        string[] elite = { "Runed Greatsword", "Shadow Glaive", "Dragonbone Bow", "Mythril Saber" };

        string[] pool;
        if (itemLevel >= 75) pool = elite;
        else if (itemLevel >= 40) pool = high;
        else if (itemLevel >= 15) pool = mid;
        else pool = low;

        return pool[rng.Next(pool.Length)];
    }

    private static string PickArmorName(EquipSlot slot, int itemLevel, Random rng)
    {
        // Tier prefix based on item level
        string prefix;
        if (itemLevel >= 75) prefix = "Mythril";
        else if (itemLevel >= 50) prefix = "Darksteel";
        else if (itemLevel >= 25) prefix = "Steel";
        else if (itemLevel >= 10) prefix = "Iron";
        else prefix = "Leather";

        string piece = slot switch
        {
            EquipSlot.Head => "Helm",
            EquipSlot.Body => "Chestplate",
            EquipSlot.Legs => "Greaves",
            EquipSlot.Feet => "Boots",
            EquipSlot.OffHand => "Shield",
            _ => "Armor",
        };

        return $"{prefix} {piece}";
    }
}
