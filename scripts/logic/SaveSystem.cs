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
            SkillPoints = gs.Skills.SkillPoints,
            SkillStates = gs.Skills.CaptureStates(),
            BankData = gs.PlayerBank.CaptureState(),
            QuestData = gs.Quests.CaptureState(),
            AchievementData = gs.Achievements.CaptureState(),
        };
    }

    /// <summary>
    /// Restore game state from a SaveData record.
    /// Validates all fields to prevent corrupted saves from creating impossible states.
    /// </summary>
    public static void RestoreState(Autoloads.GameState gs, SaveData data)
    {
        gs.SelectedClass = data.SelectedClass;
        gs.IsDead = false;

        gs.Stats.Reset();
        gs.Stats.Str = System.Math.Max(0, data.Str);
        gs.Stats.Dex = System.Math.Max(0, data.Dex);
        gs.Stats.Sta = System.Math.Max(0, data.Sta);
        gs.Stats.Int = System.Math.Max(0, data.Int);
        gs.Stats.FreePoints = System.Math.Max(0, data.FreePoints);

        int level = System.Math.Max(1, data.Level);
        int maxHp = System.Math.Max(1, data.MaxHp);
        int hp = System.Math.Clamp(data.Hp, 1, maxHp);
        int xp = System.Math.Max(0, data.Xp);
        int floor = System.Math.Max(1, data.FloorNumber);

        gs.MaxHp = maxHp;
        gs.Hp = hp;
        gs.Xp = xp;
        gs.Level = level;
        gs.FloorNumber = floor;

        gs.PlayerInventory = new Inventory(25);
        gs.PlayerInventory.Gold = System.Math.Max(0, data.Gold);

        foreach (var savedItem in data.Items)
        {
            var itemDef = ItemDatabase.Get(savedItem.ItemId);
            if (itemDef != null)
                gs.PlayerInventory.TryAdd(itemDef, System.Math.Max(1, savedItem.Count));
        }

        // Restore skills
        gs.Skills = new SkillTracker(gs.SelectedClass);
        gs.Skills.SkillPoints = System.Math.Max(0, data.SkillPoints);
        if (data.SkillStates.Length > 0)
            gs.Skills.RestoreStates(data.SkillStates);

        // Restore bank
        gs.PlayerBank = new Bank();
        if (data.BankData != null)
            gs.PlayerBank.RestoreState(data.BankData);

        // Restore quests
        gs.Quests = new QuestTracker();
        if (data.QuestData != null)
            gs.Quests.RestoreState(data.QuestData);
        else
            gs.Quests.GenerateQuests(floor);

        // Restore achievements
        gs.Achievements = new AchievementTracker();
        if (data.AchievementData != null)
            gs.Achievements.RestoreState(data.AchievementData);
    }

    public static string Serialize(SaveData data) => JsonSerializer.Serialize(data, JsonOptions);

    public static SaveData? Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<SaveData>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            Godot.GD.PrintErr($"Save data corrupted: {ex.Message}");
            return null;
        }
    }
}
