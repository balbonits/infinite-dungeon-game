using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Thin Godot wrapper for file I/O. Uses SaveSerializer for all data logic.
/// This is the only file with Godot dependencies (FileAccess, Json, DirAccess).
/// </summary>
public static class SaveSystem
{
    private const string SaveDir = "user://saves/";

    private static string SlotPath(int slot) => $"{SaveDir}slot_{slot}.json";

    /// <summary>
    /// Serialize current GameState and write to a save slot file.
    /// </summary>
    public static bool SaveToSlot(int slot)
    {
        try
        {
            DirAccess.MakeDirRecursiveAbsolute(SaveDir);

            var data = SaveSerializer.Serialize(slot);
            var godotDict = ToGodotDict(data);
            string json = Json.Stringify(godotDict, "\t");

            using var file = FileAccess.Open(SlotPath(slot), FileAccess.ModeFlags.Write);
            if (file == null)
            {
                GD.PushError($"SaveSystem: Failed to open {SlotPath(slot)} for writing: {FileAccess.GetOpenError()}");
                return false;
            }

            file.StoreString(json);
            return true;
        }
        catch (Exception ex)
        {
            GD.PushError($"SaveSystem: SaveToSlot({slot}) failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Read a save slot file and populate GameState.
    /// </summary>
    public static bool LoadFromSlot(int slot)
    {
        try
        {
            string path = SlotPath(slot);
            if (!FileAccess.FileExists(path))
                return false;

            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PushError($"SaveSystem: Failed to open {path} for reading: {FileAccess.GetOpenError()}");
                return false;
            }

            string text = file.GetAsText();
            var json = new Json();
            Error parseResult = json.Parse(text);
            if (parseResult != Error.Ok)
            {
                GD.PushError($"SaveSystem: JSON parse error in {path}: {json.GetErrorMessage()} at line {json.GetErrorLine()}");
                return false;
            }

            var godotData = json.Data.AsGodotDictionary();
            if (godotData == null)
                return false;

            var data = FromGodotDict(godotData);
            return SaveSerializer.Deserialize(data);
        }
        catch (Exception ex)
        {
            GD.PushError($"SaveSystem: LoadFromSlot({slot}) failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Check if a save file exists for the given slot.
    /// </summary>
    public static bool SlotExists(int slot)
    {
        return FileAccess.FileExists(SlotPath(slot));
    }

    /// <summary>
    /// Get a brief summary of a save slot (name, level, floor) for display.
    /// </summary>
    public static Dictionary<string, string> GetSlotSummary(int slot)
    {
        try
        {
            string path = SlotPath(slot);
            if (!FileAccess.FileExists(path))
                return new Dictionary<string, string>();

            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            if (file == null)
                return new Dictionary<string, string>();

            string text = file.GetAsText();
            var json = new Json();
            if (json.Parse(text) != Error.Ok)
                return new Dictionary<string, string>();

            var godotData = json.Data.AsGodotDictionary();
            if (godotData == null)
                return new Dictionary<string, string>();

            var data = FromGodotDict(godotData);
            return SaveSerializer.ExtractSummary(data);
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Delete a save slot file.
    /// </summary>
    public static bool DeleteSlot(int slot)
    {
        string path = SlotPath(slot);
        if (!FileAccess.FileExists(path))
            return false;

        var err = DirAccess.RemoveAbsolute(path);
        return err == Error.Ok;
    }

    // ---- Godot Dictionary <-> C# Dictionary conversion ----

    private static Godot.Collections.Dictionary ToGodotDict(Dictionary<string, object> data)
    {
        var result = new Godot.Collections.Dictionary();
        foreach (var kvp in data)
        {
            result[kvp.Key] = ConvertToGodotVariant(kvp.Value);
        }
        return result;
    }

    private static Variant ConvertToGodotVariant(object value)
    {
        if (value == null) return default;
        if (value is int i) return i;
        if (value is long l) return (int)l;
        if (value is bool b) return b;
        if (value is string s) return s;
        if (value is float f) return f;
        if (value is double d) return (float)d;
        if (value is Dictionary<string, object> dict) return ToGodotDict(dict);
        if (value is List<object> list)
        {
            var arr = new Godot.Collections.Array();
            foreach (var item in list)
                arr.Add(ConvertToGodotVariant(item));
            return arr;
        }
        return value.ToString();
    }

    private static Dictionary<string, object> FromGodotDict(Godot.Collections.Dictionary godotDict)
    {
        var result = new Dictionary<string, object>();
        foreach (Variant key in godotDict.Keys)
        {
            string keyStr = key.AsString();
            result[keyStr] = ConvertFromGodotVariant(godotDict[keyStr]);
        }
        return result;
    }

    private static object ConvertFromGodotVariant(Variant value)
    {
        switch (value.VariantType)
        {
            case Variant.Type.Bool:
                return (bool)value;
            case Variant.Type.Int:
                return (int)(long)value;
            case Variant.Type.Float:
                return (double)value;
            case Variant.Type.String:
                return (string)value;
            case Variant.Type.Dictionary:
                return FromGodotDict(value.AsGodotDictionary());
            case Variant.Type.Array:
                var godotArr = value.AsGodotArray();
                var list = new List<object>();
                foreach (var item in godotArr)
                    list.Add(ConvertFromGodotVariant(item));
                return list;
            default:
                return value.ToString();
        }
    }
}
