using System.Text.Json;
using System.Text.Json.Serialization;

namespace DungeonGame;

/// <summary>
/// Serializable save data. Contains everything needed to restore a play session.
/// Saved as JSON to user://saves/slot_N.json.
/// </summary>
public record SaveData
{
    public int Version { get; init; } = 1;
    public string SaveDate { get; init; } = "";

    // Character
    public PlayerClass SelectedClass { get; init; }
    public int Level { get; init; } = 1;
    public int Hp { get; init; } = 100;
    public int MaxHp { get; init; } = 100;
    public int Xp { get; init; }
    public int FloorNumber { get; init; } = 1;
    public int DeepestFloor { get; init; } = 1;

    // Stats
    public int Str { get; init; }
    public int Dex { get; init; }
    public int Sta { get; init; }
    public int Int { get; init; }
    public int FreePoints { get; init; }

    // Inventory
    public int Gold { get; init; } = 100;
    public SavedItemStack[] Items { get; init; } = System.Array.Empty<SavedItemStack>();

    // Skills
    public int SkillPoints { get; init; }
    public SavedSkillState[] SkillStates { get; init; } = System.Array.Empty<SavedSkillState>();

    // Bank
    public SavedBankData? BankData { get; init; }

    // Quests
    public SavedQuestData? QuestData { get; init; }

    // Achievements
    public SavedAchievementData? AchievementData { get; init; }
}

public record SavedItemStack
{
    public string ItemId { get; init; } = "";
    public int Count { get; init; } = 1;
}

public record SavedSkillState
{
    public string SkillId { get; init; } = "";
    public int Level { get; init; }
    public int Xp { get; init; }
}

public record SavedBankData
{
    public int ExpansionCount { get; init; }
    public SavedItemStack[] Items { get; init; } = System.Array.Empty<SavedItemStack>();
}
