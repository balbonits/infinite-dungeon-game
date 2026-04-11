using Godot;

namespace DungeonGame.Autoloads;

/// <summary>
/// Godot-side save file management. Reads/writes JSON to user://saves/.
/// Auto-saves on floor descent and town entry. Manual save from pause menu.
/// </summary>
public partial class SaveManager : Node
{
    public static SaveManager Instance { get; private set; } = null!;

    private const string SaveDir = "user://saves";
    private const string AutoSaveFile = "user://saves/autosave.json";

    public override void _Ready()
    {
        Instance = this;
        DirAccess.MakeDirAbsolute(SaveDir);
    }

    public void Save(string path = AutoSaveFile)
    {
        var data = SaveSystem.CaptureState(GameState.Instance);
        string json = SaveSystem.Serialize(data);

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PrintErr($"Failed to save: {FileAccess.GetOpenError()}");
            return;
        }
        file.StoreString(json);
        GD.Print($"Game saved to {path}");
    }

    public bool Load(string path = AutoSaveFile)
    {
        if (!FileAccess.FileExists(path))
            return false;

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
            return false;

        string json = file.GetAsText();
        var data = SaveSystem.Deserialize(json);
        if (data == null)
            return false;

        SaveSystem.RestoreState(GameState.Instance, data);
        GD.Print($"Game loaded from {path}");
        return true;
    }

    public bool HasSave(string path = AutoSaveFile) => FileAccess.FileExists(path);

    public SaveData? PeekSave(string path = AutoSaveFile)
    {
        if (!FileAccess.FileExists(path))
            return null;

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
            return null;

        return SaveSystem.Deserialize(file.GetAsText());
    }
}
