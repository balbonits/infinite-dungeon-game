using System;
using System.Collections.Generic;
using System.Text.Json;

namespace DungeonGame;

/// <summary>
/// Save/load system. Serializes GameState to JSON, reads it back.
/// Pure logic — Godot FileAccess handled by the caller.
/// </summary>
public static class SaveSystem
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Capture current game state into a SaveData record.
    /// </summary>
    public static SaveData CaptureState(Autoloads.GameState gs)
    {
        var items = new List<SavedItemStack>();
        for (int i = 0; i < gs.PlayerInventory.SlotCount; i++)
        {
            var stack = gs.PlayerInventory.GetSlot(i);
            if (stack != null)
                items.Add(new SavedItemStack { ItemId = stack.Item.Id, Count = stack.Count });
        }

        return new SaveData
        {
            SaveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            SelectedClass = gs.SelectedClass,
            Level = gs.Level,
            Hp = gs.Hp,
            MaxHp = gs.MaxHp,
            Xp = gs.Xp,
            FloorNumber = gs.FloorNumber,
            Str = gs.Stats.Str,
            Dex = gs.Stats.Dex,
            Sta = gs.Stats.Sta,
            Int = gs.Stats.Int,
            FreePoints = gs.Stats.FreePoints,
            Gold = gs.PlayerInventory.Gold,
            Items = items.ToArray(),
        };
    }

    /// <summary>
    /// Restore game state from a SaveData record.
    /// </summary>
    public static void RestoreState(Autoloads.GameState gs, SaveData data)
    {
        gs.SelectedClass = data.SelectedClass;
        gs.IsDead = false;

        gs.Stats.Reset();
        gs.Stats.Str = data.Str;
        gs.Stats.Dex = data.Dex;
        gs.Stats.Sta = data.Sta;
        gs.Stats.Int = data.Int;
        gs.Stats.FreePoints = data.FreePoints;

        gs.MaxHp = data.MaxHp;
        gs.Hp = data.Hp;
        gs.Xp = data.Xp;
        gs.Level = data.Level;
        gs.FloorNumber = data.FloorNumber;

        gs.PlayerInventory = new Inventory(25);
        gs.PlayerInventory.Gold = data.Gold;

        foreach (var savedItem in data.Items)
        {
            var itemDef = ItemDatabase.Get(savedItem.ItemId);
            if (itemDef != null)
                gs.PlayerInventory.TryAdd(itemDef, savedItem.Count);
        }
    }

    public static string Serialize(SaveData data) => JsonSerializer.Serialize(data, JsonOptions);

    public static SaveData? Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<SaveData>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
