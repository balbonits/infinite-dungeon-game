using Godot;

namespace DungeonGame.Autoloads;

/// <summary>
/// Production <see cref="ISaveStorage"/> implementation backed by Godot's FileAccess.
/// Keys are Godot resource paths (e.g. <c>user://saves/save_0.json</c>).
/// </summary>
public class GodotFileSaveStorage : ISaveStorage
{
    public bool Exists(string key) => FileAccess.FileExists(key);

    public string? Read(string key)
    {
        if (!FileAccess.FileExists(key)) return null;
        using var file = FileAccess.Open(key, FileAccess.ModeFlags.Read);
        if (file == null) return null;
        return file.GetAsText();
    }

    public bool Write(string key, string content)
    {
        using var file = FileAccess.Open(key, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PrintErr($"Failed to save to {key}: {FileAccess.GetOpenError()}");
            return false;
        }
        file.StoreString(content);
        return true;
    }

    public void Delete(string key)
    {
        if (!FileAccess.FileExists(key)) return;
        DirAccess.RemoveAbsolute(key);
    }
}
