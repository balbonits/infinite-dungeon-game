using System;
using System.Text.Json;

namespace DungeonGame;

/// <summary>
/// Pure-logic JSON codec for <see cref="SaveData"/>. Extracted from
/// <see cref="SaveSystem"/> so it can be included in the xUnit test project
/// without pulling in Godot-coupled GameState references.
/// </summary>
public static class SaveDataJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string Serialize(SaveData data) =>
        JsonSerializer.Serialize(data, Options);

    public static SaveData? Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<SaveData>(json, Options);
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"Save data corrupted: {ex.Message}");
            return null;
        }
    }
}
