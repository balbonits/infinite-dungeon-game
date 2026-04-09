using System.Collections.Generic;
using Xunit;

namespace DungeonGame.Tests;

/// <summary>
/// Tests for SaveSerializer (pure C# serialization logic).
/// Godot FileAccess is not available in xUnit, so we test the Dictionary
/// round-trip which is the critical data path.
/// </summary>
public class SaveSystemTests
{
    public SaveSystemTests()
    {
        GameState.Reset();
    }

    // ---- Basic Serialize/Deserialize ----

    [Fact]
    public void Serialize_ReturnsValidDictionary()
    {
        var data = SaveSerializer.Serialize(1);

        Assert.Equal(SaveSerializer.SaveVersion, data["version"]);
        Assert.Equal(1, data["slot"]);
        Assert.True(data.ContainsKey("character"));
        Assert.True(data.ContainsKey("inventory"));
        Assert.True(data.ContainsKey("equipment"));
        Assert.True(data.ContainsKey("location"));
        Assert.True(data.ContainsKey("dungeon_floor"));
    }

    [Fact]
    public void Serialize_CapturesPlayerName()
    {
        GameState.Player.Name = "TestHero";
        var data = SaveSerializer.Serialize(1);
        var character = data["character"] as Dictionary<string, object>;

        Assert.Equal("TestHero", character["name"]);
    }

    [Fact]
    public void Serialize_CapturesSlotNumber()
    {
        var data = SaveSerializer.Serialize(7);
        Assert.Equal(7, data["slot"]);
    }

    // ---- Round-trip: player stats ----

    [Fact]
    public void RoundTrip_PlayerStats_AllFieldsPreserved()
    {
        GameState.Player.Name = "Aragorn";
        GameState.Player.Level = 42;
        GameState.Player.XP = 1234;
        GameState.Player.HP = 350;
        GameState.Player.MaxHP = 500;
        GameState.Player.MP = 75;
        GameState.Player.MaxMP = 200;
        GameState.Player.STR = 20;
        GameState.Player.DEX = 15;
        GameState.Player.INT = 10;
        GameState.Player.VIT = 25;
        GameState.Player.Gold = 9999;
        GameState.Player.StatPoints = 5;
        GameState.Player.SkillPoints = 3;
        GameState.Player.InventorySize = 30;
        GameState.Player.BackpackExpansions = 2;

        var data = SaveSerializer.Serialize(1);

        // Reset and deserialize
        GameState.Reset();
        bool result = SaveSerializer.Deserialize(data);

        Assert.True(result);
        Assert.Equal("Aragorn", GameState.Player.Name);
        Assert.Equal(42, GameState.Player.Level);
        Assert.Equal(1234, GameState.Player.XP);
        Assert.Equal(350, GameState.Player.HP);
        Assert.Equal(500, GameState.Player.MaxHP);
        Assert.Equal(75, GameState.Player.MP);
        Assert.Equal(200, GameState.Player.MaxMP);
        Assert.Equal(20, GameState.Player.STR);
        Assert.Equal(15, GameState.Player.DEX);
        Assert.Equal(10, GameState.Player.INT);
        Assert.Equal(25, GameState.Player.VIT);
        Assert.Equal(9999, GameState.Player.Gold);
        Assert.Equal(5, GameState.Player.StatPoints);
        Assert.Equal(3, GameState.Player.SkillPoints);
        Assert.Equal(30, GameState.Player.InventorySize);
        Assert.Equal(2, GameState.Player.BackpackExpansions);
    }

    // ---- Round-trip: location ----

    [Fact]
    public void RoundTrip_Location_Town()
    {
        GameState.Location = GameLocation.Town;
        GameState.DungeonFloor = 0;

        var data = SaveSerializer.Serialize(1);
        GameState.Reset();
        SaveSerializer.Deserialize(data);

        Assert.Equal(GameLocation.Town, GameState.Location);
        Assert.Equal(0, GameState.DungeonFloor);
    }

    [Fact]
    public void RoundTrip_Location_Dungeon()
    {
        GameState.Location = GameLocation.Dungeon;
        GameState.DungeonFloor = 15;

        var data = SaveSerializer.Serialize(1);
        GameState.Reset();
        SaveSerializer.Deserialize(data);

        Assert.Equal(GameLocation.Dungeon, GameState.Location);
        Assert.Equal(15, GameState.DungeonFloor);
    }

    // ---- Round-trip: inventory ----

    [Fact]
    public void RoundTrip_EmptyInventory()
    {
        var data = SaveSerializer.Serialize(1);
        GameState.Reset();
        SaveSerializer.Deserialize(data);

        Assert.Empty(GameState.Player.Inventory);
    }

    [Fact]
    public void RoundTrip_InventoryWithWeapon()
    {
        var sword = GameSystems.CreateItem("Iron Sword", ItemType.Weapon, EquipSlot.MainHand,
            damage: 15, value: 100, desc: "A sturdy blade");
        sword.ItemLevel = 5;
        sword.Quality = ItemQuality.Superior;
        GameState.Player.Inventory.Add(sword);

        var data = SaveSerializer.Serialize(1);
        GameState.Reset();
        SaveSerializer.Deserialize(data);

        Assert.Single(GameState.Player.Inventory);
        var loaded = GameState.Player.Inventory[0];
        Assert.Equal("Iron Sword", loaded.Name);
        Assert.Equal(ItemType.Weapon, loaded.Type);
        Assert.Equal(EquipSlot.MainHand, loaded.Slot);
        Assert.Equal(15, loaded.Damage);
        Assert.Equal(100, loaded.Value);
        Assert.Equal("A sturdy blade", loaded.Description);
        Assert.Equal(5, loaded.ItemLevel);
        Assert.Equal(ItemQuality.Superior, loaded.Quality);
    }

    [Fact]
    public void RoundTrip_InventoryWithStackableConsumable()
    {
        var potion = GameSystems.CreateItem("Health Potion", ItemType.Consumable, EquipSlot.None,
            hpBonus: 50, value: 25, stackable: true);
        potion.StackCount = 10;
        GameState.Player.Inventory.Add(potion);

        var data = SaveSerializer.Serialize(1);
        GameState.Reset();
        SaveSerializer.Deserialize(data);

        Assert.Single(GameState.Player.Inventory);
        var loaded = GameState.Player.Inventory[0];
        Assert.Equal("Health Potion", loaded.Name);
        Assert.True(loaded.Stackable);
        Assert.Equal(10, loaded.StackCount);
        Assert.Equal(50, loaded.HPBonus);
    }

    [Fact]
    public void RoundTrip_InventoryMultipleItems()
    {
        GameState.Player.Inventory.Add(GameSystems.CreateItem("Sword", ItemType.Weapon, EquipSlot.MainHand, damage: 10));
        GameState.Player.Inventory.Add(GameSystems.CreateItem("Shield", ItemType.Armor, EquipSlot.OffHand, defense: 5));
        GameState.Player.Inventory.Add(GameSystems.CreateItem("Gem", ItemType.Material, EquipSlot.None, value: 200));

        var data = SaveSerializer.Serialize(1);
        GameState.Reset();
        SaveSerializer.Deserialize(data);

        Assert.Equal(3, GameState.Player.Inventory.Count);
        Assert.Equal("Sword", GameState.Player.Inventory[0].Name);
        Assert.Equal("Shield", GameState.Player.Inventory[1].Name);
        Assert.Equal("Gem", GameState.Player.Inventory[2].Name);
    }

    [Fact]
    public void RoundTrip_InventoryAllItemTypes()
    {
        GameState.Player.Inventory.Add(GameSystems.CreateItem("Blade", ItemType.Weapon, EquipSlot.MainHand));
        GameState.Player.Inventory.Add(GameSystems.CreateItem("Plate", ItemType.Armor, EquipSlot.Body));
        GameState.Player.Inventory.Add(GameSystems.CreateItem("Ring", ItemType.Accessory, EquipSlot.Ring));
        GameState.Player.Inventory.Add(GameSystems.CreateItem("Potion", ItemType.Consumable, EquipSlot.None, stackable: true));
        GameState.Player.Inventory.Add(GameSystems.CreateItem("Ore", ItemType.Material, EquipSlot.None));

        var data = SaveSerializer.Serialize(1);
        GameState.Reset();
        SaveSerializer.Deserialize(data);

        Assert.Equal(5, GameState.Player.Inventory.Count);
        Assert.Equal(ItemType.Weapon, GameState.Player.Inventory[0].Type);
        Assert.Equal(ItemType.Armor, GameState.Player.Inventory[1].Type);
        Assert.Equal(ItemType.Accessory, GameState.Player.Inventory[2].Type);
        Assert.Equal(ItemType.Consumable, GameState.Player.Inventory[3].Type);
        Assert.Equal(ItemType.Material, GameState.Player.Inventory[4].Type);
    }

    // ---- Round-trip: equipment ----

    [Fact]
    public void RoundTrip_EmptyEquipment()
    {
        var data = SaveSerializer.Serialize(1);
        GameState.Reset();
        SaveSerializer.Deserialize(data);

        Assert.Empty(GameState.Player.Equipment);
    }

    [Fact]
    public void RoundTrip_EquipmentSingleSlot()
    {
        var helmet = new ItemData
        {
            Name = "Iron Helm",
            Type = ItemType.Armor,
            Slot = EquipSlot.Head,
            Defense = 8,
            Value = 75,
            Description = "Protects your head",
        };
        GameState.Player.Equipment[EquipSlot.Head] = helmet;

        var data = SaveSerializer.Serialize(1);
        GameState.Reset();
        SaveSerializer.Deserialize(data);

        Assert.Single(GameState.Player.Equipment);
        Assert.True(GameState.Player.Equipment.ContainsKey(EquipSlot.Head));
        var loaded = GameState.Player.Equipment[EquipSlot.Head];
        Assert.Equal("Iron Helm", loaded.Name);
        Assert.Equal(8, loaded.Defense);
        Assert.Equal(EquipSlot.Head, loaded.Slot);
    }

    [Fact]
    public void RoundTrip_EquipmentMultipleSlots()
    {
        GameState.Player.Equipment[EquipSlot.MainHand] = new ItemData
        {
            Name = "Steel Sword", Type = ItemType.Weapon, Slot = EquipSlot.MainHand, Damage = 20,
        };
        GameState.Player.Equipment[EquipSlot.Body] = new ItemData
        {
            Name = "Chain Mail", Type = ItemType.Armor, Slot = EquipSlot.Body, Defense = 12,
        };
        GameState.Player.Equipment[EquipSlot.Ring] = new ItemData
        {
            Name = "Gold Ring", Type = ItemType.Accessory, Slot = EquipSlot.Ring, HPBonus = 10, MPBonus = 5,
        };

        var data = SaveSerializer.Serialize(1);
        GameState.Reset();
        SaveSerializer.Deserialize(data);

        Assert.Equal(3, GameState.Player.Equipment.Count);
        Assert.Equal("Steel Sword", GameState.Player.Equipment[EquipSlot.MainHand].Name);
        Assert.Equal(20, GameState.Player.Equipment[EquipSlot.MainHand].Damage);
        Assert.Equal("Chain Mail", GameState.Player.Equipment[EquipSlot.Body].Name);
        Assert.Equal(12, GameState.Player.Equipment[EquipSlot.Body].Defense);
        Assert.Equal("Gold Ring", GameState.Player.Equipment[EquipSlot.Ring].Name);
        Assert.Equal(10, GameState.Player.Equipment[EquipSlot.Ring].HPBonus);
        Assert.Equal(5, GameState.Player.Equipment[EquipSlot.Ring].MPBonus);
    }

    // ---- Round-trip: items with affixes ----

    [Fact]
    public void RoundTrip_ItemWithPrefixes()
    {
        var sword = GameSystems.CreateItem("Keen Sword", ItemType.Weapon, EquipSlot.MainHand, damage: 12);
        sword.Prefixes.Add(new AffixData
        {
            Name = "Keen",
            Tier = 3,
            BonusDamage = 5,
            BonusSTR = 2,
        });
        GameState.Player.Inventory.Add(sword);

        var data = SaveSerializer.Serialize(1);
        GameState.Reset();
        SaveSerializer.Deserialize(data);

        Assert.Single(GameState.Player.Inventory);
        var loaded = GameState.Player.Inventory[0];
        Assert.Single(loaded.Prefixes);
        Assert.Equal("Keen", loaded.Prefixes[0].Name);
        Assert.Equal(3, loaded.Prefixes[0].Tier);
        Assert.Equal(5, loaded.Prefixes[0].BonusDamage);
        Assert.Equal(2, loaded.Prefixes[0].BonusSTR);
    }

    [Fact]
    public void RoundTrip_ItemWithSuffixes()
    {
        var ring = GameSystems.CreateItem("Ring of Vitality", ItemType.Accessory, EquipSlot.Ring, hpBonus: 20);
        ring.Suffixes.Add(new AffixData
        {
            Name = "of Vitality",
            Tier = 2,
            BonusHP = 15,
            BonusVIT = 3,
        });
        GameState.Player.Inventory.Add(ring);

        var data = SaveSerializer.Serialize(1);
        GameState.Reset();
        SaveSerializer.Deserialize(data);

        var loaded = GameState.Player.Inventory[0];
        Assert.Single(loaded.Suffixes);
        Assert.Equal("of Vitality", loaded.Suffixes[0].Name);
        Assert.Equal(15, loaded.Suffixes[0].BonusHP);
        Assert.Equal(3, loaded.Suffixes[0].BonusVIT);
    }

    [Fact]
    public void RoundTrip_ItemWithMultipleAffixes()
    {
        var weapon = GameSystems.CreateItem("Epic Blade", ItemType.Weapon, EquipSlot.MainHand, damage: 25);
        weapon.Prefixes.Add(new AffixData { Name = "Flaming", Tier = 4, BonusDamage = 8 });
        weapon.Prefixes.Add(new AffixData { Name = "Sharp", Tier = 2, BonusDamage = 3 });
        weapon.Suffixes.Add(new AffixData { Name = "of Power", Tier = 3, BonusSTR = 5 });
        GameState.Player.Inventory.Add(weapon);

        var data = SaveSerializer.Serialize(1);
        GameState.Reset();
        SaveSerializer.Deserialize(data);

        var loaded = GameState.Player.Inventory[0];
        Assert.Equal(2, loaded.Prefixes.Count);
        Assert.Single(loaded.Suffixes);
        Assert.Equal("Flaming", loaded.Prefixes[0].Name);
        Assert.Equal("Sharp", loaded.Prefixes[1].Name);
        Assert.Equal("of Power", loaded.Suffixes[0].Name);
    }

    // ---- Round-trip: complex full state ----

    [Fact]
    public void RoundTrip_FullGameState()
    {
        // Set up a realistic game state
        GameState.Player.Name = "Gandalf";
        GameState.Player.Level = 25;
        GameState.Player.XP = 500;
        GameState.Player.HP = 300;
        GameState.Player.MaxHP = 400;
        GameState.Player.MP = 180;
        GameState.Player.MaxMP = 250;
        GameState.Player.STR = 12;
        GameState.Player.DEX = 18;
        GameState.Player.INT = 30;
        GameState.Player.VIT = 15;
        GameState.Player.Gold = 5000;
        GameState.Player.StatPoints = 2;
        GameState.Player.SkillPoints = 1;
        GameState.Player.InventorySize = 35;
        GameState.Player.BackpackExpansions = 4;
        GameState.Location = GameLocation.Dungeon;
        GameState.DungeonFloor = 10;

        // Equipment
        GameState.Player.Equipment[EquipSlot.MainHand] = new ItemData
        {
            Name = "Staff of Wisdom", Type = ItemType.Weapon, Slot = EquipSlot.MainHand,
            Damage = 30, MPBonus = 50, ItemLevel = 20, Quality = ItemQuality.Elite,
        };
        GameState.Player.Equipment[EquipSlot.Body] = new ItemData
        {
            Name = "Wizard Robe", Type = ItemType.Armor, Slot = EquipSlot.Body,
            Defense = 15, MPBonus = 25, ItemLevel = 18, Quality = ItemQuality.Superior,
        };

        // Inventory
        var potion = GameSystems.CreateItem("Mana Potion", ItemType.Consumable, EquipSlot.None,
            mpBonus: 100, value: 50, stackable: true);
        potion.StackCount = 5;
        GameState.Player.Inventory.Add(potion);
        GameState.Player.Inventory.Add(GameSystems.CreateItem("Spare Helm", ItemType.Armor, EquipSlot.Head, defense: 6));

        var data = SaveSerializer.Serialize(3);
        GameState.Reset();
        bool result = SaveSerializer.Deserialize(data);

        Assert.True(result);
        Assert.Equal("Gandalf", GameState.Player.Name);
        Assert.Equal(25, GameState.Player.Level);
        Assert.Equal(GameLocation.Dungeon, GameState.Location);
        Assert.Equal(10, GameState.DungeonFloor);
        Assert.Equal(2, GameState.Player.Equipment.Count);
        Assert.Equal("Staff of Wisdom", GameState.Player.Equipment[EquipSlot.MainHand].Name);
        Assert.Equal(ItemQuality.Elite, GameState.Player.Equipment[EquipSlot.MainHand].Quality);
        Assert.Equal(2, GameState.Player.Inventory.Count);
        Assert.Equal(5, GameState.Player.Inventory[0].StackCount);
        Assert.Equal(35, GameState.Player.InventorySize);
        Assert.Equal(4, GameState.Player.BackpackExpansions);
    }

    // ---- Slot summary ----

    [Fact]
    public void ExtractSummary_ReturnsNameLevelFloor()
    {
        GameState.Player.Name = "TestChar";
        GameState.Player.Level = 10;
        GameState.DungeonFloor = 5;

        var data = SaveSerializer.Serialize(1);
        var summary = SaveSerializer.ExtractSummary(data);

        Assert.Equal("TestChar", summary["name"]);
        Assert.Equal("10", summary["level"]);
        Assert.Equal("5", summary["floor"]);
    }

    // ---- Error handling ----

    [Fact]
    public void Deserialize_ReturnsFalse_OnMissingVersion()
    {
        var data = new Dictionary<string, object>
        {
            ["character"] = new Dictionary<string, object> { ["level"] = 1 },
        };
        // Missing "version" key
        data.Remove("version");

        bool result = SaveSerializer.Deserialize(data);
        Assert.False(result);
    }

    [Fact]
    public void Deserialize_ReturnsFalse_OnMissingCharacter()
    {
        var data = new Dictionary<string, object>
        {
            ["version"] = 1,
        };

        bool result = SaveSerializer.Deserialize(data);
        Assert.False(result);
    }

    [Fact]
    public void Deserialize_ReturnsFalse_OnNullCharacter()
    {
        var data = new Dictionary<string, object>
        {
            ["version"] = 1,
            ["character"] = null,
        };

        bool result = SaveSerializer.Deserialize(data);
        Assert.False(result);
    }

    [Fact]
    public void Deserialize_ReturnsFalse_OnInvalidCharacterData()
    {
        var data = new Dictionary<string, object>
        {
            ["version"] = 1,
            ["character"] = new Dictionary<string, object>
            {
                // Missing required fields like "level", "xp", etc.
                ["name"] = "Broken",
            },
        };

        bool result = SaveSerializer.Deserialize(data);
        Assert.False(result);
    }

    // ---- Item serialization edge cases ----

    [Fact]
    public void SerializeItem_HandlesNullNameAndDescription()
    {
        var item = new ItemData
        {
            Name = null,
            Type = ItemType.Material,
            Slot = EquipSlot.None,
            Description = null,
        };

        var dict = SaveSerializer.SerializeItem(item);
        Assert.Equal("", dict["name"]);
        Assert.Equal("", dict["description"]);
    }

    [Fact]
    public void DeserializeItem_HandlesMinimalData()
    {
        var dict = new Dictionary<string, object>
        {
            ["name"] = "Ore",
            ["type"] = (int)ItemType.Material,
            ["slot"] = (int)EquipSlot.None,
            ["damage"] = 0,
            ["defense"] = 0,
            ["hp_bonus"] = 0,
            ["mp_bonus"] = 0,
            ["value"] = 10,
            ["stackable"] = false,
            ["stack_count"] = 1,
        };

        var item = SaveSerializer.DeserializeItem(dict);

        Assert.NotNull(item);
        Assert.Equal("Ore", item.Name);
        Assert.Equal(ItemType.Material, item.Type);
        Assert.Equal(10, item.Value);
    }

    [Fact]
    public void DeserializeItem_ReturnsNull_OnInvalidData()
    {
        var dict = new Dictionary<string, object>
        {
            ["name"] = "Bad Item",
            // Missing required numeric fields
        };

        var item = SaveSerializer.DeserializeItem(dict);
        Assert.Null(item);
    }

    // ---- Default player state round-trip ----

    [Fact]
    public void RoundTrip_DefaultPlayerState()
    {
        // Fresh player with no modifications
        var data = SaveSerializer.Serialize(1);
        GameState.Reset();
        SaveSerializer.Deserialize(data);

        Assert.Equal("Hero", GameState.Player.Name);
        Assert.Equal(1, GameState.Player.Level);
        Assert.Equal(0, GameState.Player.XP);
        Assert.Equal(5, GameState.Player.STR);
        Assert.Equal(5, GameState.Player.DEX);
        Assert.Equal(5, GameState.Player.INT);
        Assert.Equal(5, GameState.Player.VIT);
        Assert.Equal(100, GameState.Player.Gold);
        Assert.Equal(25, GameState.Player.InventorySize);
        Assert.Equal(0, GameState.Player.BackpackExpansions);
    }

    // ---- Equipment with bonuses round-trip ----

    [Fact]
    public void RoundTrip_EquipmentBonuses()
    {
        GameState.Player.Equipment[EquipSlot.Ring] = new ItemData
        {
            Name = "Ring of Power",
            Type = ItemType.Accessory,
            Slot = EquipSlot.Ring,
            HPBonus = 25,
            MPBonus = 15,
            Value = 500,
        };

        var data = SaveSerializer.Serialize(1);
        GameState.Reset();
        SaveSerializer.Deserialize(data);

        var ring = GameState.Player.Equipment[EquipSlot.Ring];
        Assert.Equal(25, ring.HPBonus);
        Assert.Equal(15, ring.MPBonus);
        Assert.Equal(500, ring.Value);
    }

    // ---- Affix edge cases ----

    [Fact]
    public void RoundTrip_ItemWithAllAffixBonuses()
    {
        var item = GameSystems.CreateItem("Test Item", ItemType.Weapon, EquipSlot.MainHand);
        item.Prefixes.Add(new AffixData
        {
            Name = "Omni",
            Tier = 6,
            BonusDamage = 10,
            BonusDefense = 5,
            BonusHP = 20,
            BonusMP = 15,
            BonusSTR = 3,
            BonusDEX = 4,
            BonusINT = 5,
            BonusVIT = 6,
        });
        GameState.Player.Inventory.Add(item);

        var data = SaveSerializer.Serialize(1);
        GameState.Reset();
        SaveSerializer.Deserialize(data);

        var affix = GameState.Player.Inventory[0].Prefixes[0];
        Assert.Equal("Omni", affix.Name);
        Assert.Equal(6, affix.Tier);
        Assert.Equal(10, affix.BonusDamage);
        Assert.Equal(5, affix.BonusDefense);
        Assert.Equal(20, affix.BonusHP);
        Assert.Equal(15, affix.BonusMP);
        Assert.Equal(3, affix.BonusSTR);
        Assert.Equal(4, affix.BonusDEX);
        Assert.Equal(5, affix.BonusINT);
        Assert.Equal(6, affix.BonusVIT);
    }
}
