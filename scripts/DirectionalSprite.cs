using Godot;
using System.Collections.Generic;

namespace DungeonGame;

/// <summary>
/// Loads 8 directional rotation textures and switches the sprite
/// based on movement direction. Attach to any entity with a Sprite2D.
/// </summary>
public static class DirectionalSprite
{
    private static readonly string[] DirectionNames =
    {
        "south", "south-west", "west", "north-west",
        "north", "north-east", "east", "south-east"
    };

    /// <summary>
    /// Load all 8 rotation textures from a directory.
    /// Returns a dictionary mapping direction name to texture.
    /// </summary>
    public static Dictionary<string, Texture2D> LoadRotations(string basePath)
    {
        var textures = new Dictionary<string, Texture2D>();
        foreach (string dir in DirectionNames)
        {
            string path = $"{basePath}/{dir}.png";
            if (ResourceLoader.Exists(path))
                textures[dir] = GD.Load<Texture2D>(path);
        }
        return textures;
    }

    /// <summary>
    /// Get the direction name for a given velocity vector.
    /// Uses 8-way snapping based on angle.
    /// </summary>
    public static string GetDirection(Vector2 velocity)
    {
        if (velocity.LengthSquared() < 0.01f)
            return "south"; // idle default

        // Atan2 gives angle in radians, convert to 0-360 range
        float angle = Mathf.RadToDeg(Mathf.Atan2(velocity.Y, velocity.X));
        if (angle < 0) angle += 360;

        // Snap to 8 directions (each covers 45 degrees)
        // 0° = east, 90° = south, 180° = west, 270° = north
        int index = ((int)Mathf.Round(angle / 45.0f)) % 8;

        return index switch
        {
            0 => "east",
            1 => "south-east",
            2 => "south",
            3 => "south-west",
            4 => "west",
            5 => "north-west",
            6 => "north",
            7 => "north-east",
            _ => "south"
        };
    }

    /// <summary>
    /// Update a sprite's texture based on movement velocity.
    /// Call this every physics frame.
    /// </summary>
    public static void UpdateSprite(Sprite2D sprite, Vector2 velocity,
        Dictionary<string, Texture2D> textures, ref string lastDirection)
    {
        if (velocity.LengthSquared() < 0.01f)
            return; // Keep last direction when idle

        string dir = GetDirection(velocity);
        if (dir == lastDirection)
            return; // No change needed

        lastDirection = dir;
        if (textures.TryGetValue(dir, out var texture))
        {
            sprite.Texture = texture;
            sprite.FlipH = false; // Rotations handle facing, no flip needed
        }
    }
}
