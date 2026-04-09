using System;
using System.Collections.Generic;

/// <summary>
/// Pure C# serialization logic for game state. No Godot dependency.
/// Godot file I/O is handled by SaveFileIO (thin wrapper).
/// </summary>
public static class SaveSerializer
{
    public const int SaveVersion = 1;

    /// <summary>
    /// Serialize the current GameState into a nested Dictionary suitable for JSON.
    /// </summary>
    public static Dictionary<string, object> Serialize(int slot)
    {
        var p = GameState.Player;

        var data = new Dictionary<string, object>
        {
            ["version"] = SaveVersion,
            ["slot"] = slot,
            ["location"] = (int)GameState.Location,
            ["dungeon_floor"] = GameState.DungeonFloor,
            ["character"] = SerializePlayer(p),
            ["inventory"] = SerializeInventory(p.Inventory),
            ["equipment"] = SerializeEquipment(p.Equipment),
        };

        return data;
    }

    /// <summary>
    /// Deserialize a Dictionary (from JSON) back into GameState.
    /// Returns true on success, false on any failure.
    /// </summary>
    public static bool Deserialize(Dictionary<string, object> data)
    {
        try
        {
            if (!data.ContainsKey("version") || !data.ContainsKey("character"))
                return false;

            // Location and floor
            if (data.ContainsKey("location"))
                GameState.Location = (GameLocation)Convert.ToInt32(data["location"]);
            if (data.ContainsKey("dungeon_floor"))
                GameState.DungeonFloor = Convert.ToInt32(data["dungeon_floor"]);

            // Character
            var charDict = data["character"] as Dictionary<string, object>;
            if (charDict == null) return false;
            if (!DeserializePlayer(charDict, GameState.Player)) return false;

            // Inventory
            if (data.ContainsKey("inventory"))
            {
                var invList = data["inventory"] as List<object>;
                if (invList != null)
                {
                    GameState.Player.Inventory.Clear();
                    foreach (var itemObj in invList)
                    {
                        var itemDict = itemObj as Dictionary<string, object>;
                        if (itemDict != null)
                        {
                            var item = DeserializeItem(itemDict);
                            if (item != null)
                                GameState.Player.Inventory.Add(item);
                        }
                    }
                }
            }

            // Equipment
            if (data.ContainsKey("equipment"))
            {
                var equipDict = data["equipment"] as Dictionary<string, object>;
                if (equipDict != null)
                {
                    GameState.Player.Equipment.Clear();
                    foreach (var kvp in equipDict)
                    {
                        if (Enum.TryParse<EquipSlot>(kvp.Key, out var slot))
                        {
                            var itemDict = kvp.Value as Dictionary<string, object>;
                            if (itemDict != null)
                            {
                                var item = DeserializeItem(itemDict);
                                if (item != null)
                                    GameState.Player.Equipment[slot] = item;
                            }
                        }
                    }
                }
            }

            GameState.Player.InvalidateStats();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Extract a slot summary (name, level, floor) without full deserialization.
    /// </summary>
    public static Dictionary<string, string> ExtractSummary(Dictionary<string, object> data)
    {
        var summary = new Dictionary<string, string>();
        try
        {
            var charDict = data["character"] as Dictionary<string, object>;
            if (charDict == null) return summary;

            summary["name"] = charDict.ContainsKey("name") ? charDict["name"].ToString() : "Unknown";
            summary["level"] = charDict.ContainsKey("level") ? charDict["level"].ToString() : "1";
            summary["floor"] = data.ContainsKey("dungeon_floor") ? data["dungeon_floor"].ToString() : "0";
        }
        catch
        {
            // Return whatever we gathered
        }
        return summary;
    }

    // ---- Private serialization helpers ----

    private static Dictionary<string, object> SerializePlayer(PlayerState p)
    {
        return new Dictionary<string, object>
        {
            ["name"] = p.Name,
            ["level"] = p.Level,
            ["xp"] = p.XP,
            ["hp"] = p.HP,
            ["max_hp"] = p.MaxHP,
            ["mp"] = p.MP,
            ["max_mp"] = p.MaxMP,
            ["str"] = p.STR,
            ["dex"] = p.DEX,
            ["int"] = p.INT,
            ["vit"] = p.VIT,
            ["gold"] = p.Gold,
            ["stat_points"] = p.StatPoints,
            ["skill_points"] = p.SkillPoints,
            ["inventory_size"] = p.InventorySize,
            ["backpack_expansions"] = p.BackpackExpansions,
        };
    }

    private static bool DeserializePlayer(Dictionary<string, object> d, PlayerState p)
    {
        try
        {
            p.Name = d.ContainsKey("name") ? d["name"].ToString() : "Hero";
            p.Level = Convert.ToInt32(d["level"]);
            p.XP = Convert.ToInt32(d["xp"]);
            p.HP = Convert.ToInt32(d["hp"]);
            p.MaxHP = Convert.ToInt32(d["max_hp"]);
            p.MP = Convert.ToInt32(d["mp"]);
            p.MaxMP = Convert.ToInt32(d["max_mp"]);
            p.STR = Convert.ToInt32(d["str"]);
            p.DEX = Convert.ToInt32(d["dex"]);
            p.INT = Convert.ToInt32(d["int"]);
            p.VIT = Convert.ToInt32(d["vit"]);
            p.Gold = Convert.ToInt32(d["gold"]);
            p.StatPoints = Convert.ToInt32(d["stat_points"]);
            p.SkillPoints = Convert.ToInt32(d["skill_points"]);
            p.InventorySize = d.ContainsKey("inventory_size") ? Convert.ToInt32(d["inventory_size"]) : 25;
            p.BackpackExpansions = d.ContainsKey("backpack_expansions") ? Convert.ToInt32(d["backpack_expansions"]) : 0;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static Dictionary<string, object> SerializeItem(ItemData item)
    {
        var dict = new Dictionary<string, object>
        {
            ["name"] = item.Name ?? "",
            ["type"] = (int)item.Type,
            ["slot"] = (int)item.Slot,
            ["damage"] = item.Damage,
            ["defense"] = item.Defense,
            ["hp_bonus"] = item.HPBonus,
            ["mp_bonus"] = item.MPBonus,
            ["value"] = item.Value,
            ["stackable"] = item.Stackable,
            ["stack_count"] = item.StackCount,
            ["description"] = item.Description ?? "",
            ["item_level"] = item.ItemLevel,
            ["quality"] = (int)item.Quality,
        };

        // Serialize affixes
        if (item.Prefixes.Count > 0)
        {
            var prefixes = new List<object>();
            foreach (var affix in item.Prefixes)
                prefixes.Add(SerializeAffix(affix));
            dict["prefixes"] = prefixes;
        }

        if (item.Suffixes.Count > 0)
        {
            var suffixes = new List<object>();
            foreach (var affix in item.Suffixes)
                suffixes.Add(SerializeAffix(affix));
            dict["suffixes"] = suffixes;
        }

        return dict;
    }

    public static ItemData DeserializeItem(Dictionary<string, object> d)
    {
        try
        {
            var item = new ItemData
            {
                Name = d.ContainsKey("name") ? d["name"].ToString() : "",
                Type = (ItemType)Convert.ToInt32(d["type"]),
                Slot = (EquipSlot)Convert.ToInt32(d["slot"]),
                Damage = Convert.ToInt32(d["damage"]),
                Defense = Convert.ToInt32(d["defense"]),
                HPBonus = Convert.ToInt32(d["hp_bonus"]),
                MPBonus = Convert.ToInt32(d["mp_bonus"]),
                Value = Convert.ToInt32(d["value"]),
                Stackable = Convert.ToBoolean(d["stackable"]),
                StackCount = Convert.ToInt32(d["stack_count"]),
                Description = d.ContainsKey("description") ? d["description"].ToString() : "",
                ItemLevel = d.ContainsKey("item_level") ? Convert.ToInt32(d["item_level"]) : 0,
                Quality = d.ContainsKey("quality") ? (ItemQuality)Convert.ToInt32(d["quality"]) : ItemQuality.Normal,
            };

            // Deserialize affixes
            if (d.ContainsKey("prefixes") && d["prefixes"] is List<object> prefixList)
            {
                foreach (var obj in prefixList)
                {
                    if (obj is Dictionary<string, object> affixDict)
                    {
                        var affix = DeserializeAffix(affixDict);
                        if (affix != null) item.Prefixes.Add(affix);
                    }
                }
            }

            if (d.ContainsKey("suffixes") && d["suffixes"] is List<object> suffixList)
            {
                foreach (var obj in suffixList)
                {
                    if (obj is Dictionary<string, object> affixDict)
                    {
                        var affix = DeserializeAffix(affixDict);
                        if (affix != null) item.Suffixes.Add(affix);
                    }
                }
            }

            return item;
        }
        catch
        {
            return null;
        }
    }

    private static Dictionary<string, object> SerializeAffix(AffixData affix)
    {
        return new Dictionary<string, object>
        {
            ["name"] = affix.Name ?? "",
            ["tier"] = affix.Tier,
            ["bonus_damage"] = affix.BonusDamage,
            ["bonus_defense"] = affix.BonusDefense,
            ["bonus_hp"] = affix.BonusHP,
            ["bonus_mp"] = affix.BonusMP,
            ["bonus_str"] = affix.BonusSTR,
            ["bonus_dex"] = affix.BonusDEX,
            ["bonus_int"] = affix.BonusINT,
            ["bonus_vit"] = affix.BonusVIT,
        };
    }

    private static AffixData DeserializeAffix(Dictionary<string, object> d)
    {
        try
        {
            return new AffixData
            {
                Name = d.ContainsKey("name") ? d["name"].ToString() : "",
                Tier = Convert.ToInt32(d["tier"]),
                BonusDamage = Convert.ToInt32(d["bonus_damage"]),
                BonusDefense = Convert.ToInt32(d["bonus_defense"]),
                BonusHP = Convert.ToInt32(d["bonus_hp"]),
                BonusMP = Convert.ToInt32(d["bonus_mp"]),
                BonusSTR = Convert.ToInt32(d["bonus_str"]),
                BonusDEX = Convert.ToInt32(d["bonus_dex"]),
                BonusINT = Convert.ToInt32(d["bonus_int"]),
                BonusVIT = Convert.ToInt32(d["bonus_vit"]),
            };
        }
        catch
        {
            return null;
        }
    }

    private static List<object> SerializeInventory(List<ItemData> inventory)
    {
        var list = new List<object>();
        foreach (var item in inventory)
            list.Add(SerializeItem(item));
        return list;
    }

    private static Dictionary<string, object> SerializeEquipment(Dictionary<EquipSlot, ItemData> equipment)
    {
        var dict = new Dictionary<string, object>();
        foreach (var kvp in equipment)
            dict[kvp.Key.ToString()] = SerializeItem(kvp.Value);
        return dict;
    }
}
