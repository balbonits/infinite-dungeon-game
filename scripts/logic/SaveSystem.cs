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
                items.Add(new SavedItemStack { ItemId = stack.Item.Id, Count = stack.Count, Locked = stack.Locked });
        }

        return new SaveData
        {
            SaveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            SelectedClass = gs.SelectedClass,
            Level = gs.Level,
            Hp = gs.Hp,
            MaxHp = gs.MaxHp,
            Mana = gs.Mana,
            MaxMana = gs.MaxMana,
            Xp = gs.Xp,
            FloorNumber = gs.FloorNumber,
            DeepestFloor = gs.DeepestFloor,
            Str = gs.Stats.Str,
            Dex = gs.Stats.Dex,
            Sta = gs.Stats.Sta,
            Int = gs.Stats.Int,
            FreePoints = gs.Stats.FreePoints,
            Gold = gs.PlayerInventory.Gold,
            Items = items.ToArray(),
            SkillPoints = gs.Progression.SkillPoints,
            AbilityPoints = gs.Progression.AbilityPoints,
            MasteryStates = gs.Progression.CaptureMasteries(),
            AbilityStates = gs.Progression.CaptureAbilities(),
            CategoryUseCounts = gs.Progression.CaptureCategoryUseCounts(),
            CategoryApSpent = gs.Progression.CaptureCategoryApSpent(),
            BankData = gs.PlayerBank.CaptureState(),
            QuestData = gs.Quests.CaptureState(),
            AchievementData = gs.Achievements.CaptureState(),
            // Skill hotbar
            SkillBarSlots = gs.SkillHotbar.ExportSlots(),
            // Endgame systems
            SaturationData = new SavedSaturationData
            {
                Zones = gs.Saturation.ExportState(),
                LastDecayTimestamp = gs.Saturation.LastDecayTimestamp,
            },
            PactRanks = gs.Pacts.ExportRanks(),
            AttunementData = new SavedAttunementData
            {
                IsUnlocked = gs.Attunement.IsUnlocked,
                Nodes = gs.Attunement.ExportNodes(),
                ClearedFloors = gs.Attunement.ExportClearedFloors(),
                ActiveKeystone = gs.Attunement.ActiveKeystone,
            },
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
        int deepest = System.Math.Max(floor, data.DeepestFloor);

        gs.DeepestFloor = deepest;
        gs.MaxHp = maxHp;
        gs.Hp = hp;
        int maxMana = System.Math.Max(0, data.MaxMana);
        int mana = System.Math.Clamp(data.Mana, 0, maxMana > 0 ? maxMana : 999);
        gs.MaxMana = maxMana > 0 ? maxMana : Constants.PlayerStats.GetClassBaseMana(data.SelectedClass);
        gs.Mana = mana > 0 ? mana : gs.MaxMana;
        gs.Xp = xp;
        gs.Level = level;
        gs.FloorNumber = floor;

        gs.PlayerInventory = new Inventory(Constants.PlayerStats.BackpackStartingSlots);
        gs.PlayerInventory.Gold = System.Math.Max(0L, data.Gold);

        foreach (var savedItem in data.Items)
        {
            var itemDef = ItemDatabase.Get(savedItem.ItemId);
            if (itemDef != null)
            {
                gs.PlayerInventory.TryAdd(itemDef, System.Math.Max(1L, savedItem.Count));
                if (savedItem.Locked)
                {
                    // SetLocked is idempotent — if the save file has multiple entries for
                    // the same item Id (e.g., migrated from older stack-split saves), TryAdd
                    // merges them into one slot and repeated toggles would flip the flag.
                    int idx = gs.PlayerInventory.FindSlot(itemDef.Id);
                    if (idx >= 0) gs.PlayerInventory.SetLocked(idx, true);
                }
            }
        }

        // Restore progression (skills + abilities)
        gs.Progression = new ProgressionTracker(gs.SelectedClass);
        gs.Progression.SkillPoints = System.Math.Max(0, data.SkillPoints);
        gs.Progression.AbilityPoints = System.Math.Max(0, data.AbilityPoints);
        gs.Progression.RestoreMasteries(data.MasteryStates);
        gs.Progression.RestoreAbilities(data.AbilityStates);
        gs.Progression.RestoreCategoryTracking(data.CategoryUseCounts, data.CategoryApSpent);

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

        // Restore skill hotbar
        gs.SkillHotbar = new SkillBar();
        gs.SkillHotbar.ImportSlots(data.SkillBarSlots);

        // Restore endgame systems
        gs.Saturation = new ZoneSaturation();
        if (data.SaturationData != null)
        {
            gs.Saturation.ImportState(data.SaturationData.Zones, data.SaturationData.LastDecayTimestamp);
            // Apply time-based decay for offline time
            double now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            gs.Saturation.ApplyDecay(now);
        }

        gs.Pacts = new DungeonPacts();
        gs.Pacts.ImportRanks(data.PactRanks);

        gs.Attunement = new MagiculeAttunement();
        if (data.AttunementData != null)
            gs.Attunement.ImportState(
                data.AttunementData.Nodes,
                data.AttunementData.ClearedFloors,
                data.AttunementData.ActiveKeystone,
                data.AttunementData.IsUnlocked);

        // Intelligence is session-scoped, always starts fresh
        gs.Intelligence = new DungeonIntelligence();
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
