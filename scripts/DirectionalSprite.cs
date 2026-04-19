using Godot;
using System.Collections.Generic;

namespace DungeonGame;

/// <summary>
/// Maps movement direction to a sprite texture. Supports two load modes:
///   (1) A directory of 8 per-direction PNGs (legacy, no longer used in-repo).
///   (2) A single atlas sheet (LPC-style), region-cropped at known walk-row
///       offsets. No pre-slicing of source art required.
/// </summary>
public static class DirectionalSprite
{
    private static readonly string[] DirectionNames =
    {
        "south", "south-west", "west", "north-west",
        "north", "north-east", "east", "south-east"
    };

    /// <summary>Region layout for an atlas sheet. One Rect2 per cardinal.</summary>
    public sealed class AtlasLayout
    {
        public Rect2 South { get; init; }
        public Rect2 North { get; init; }
        public Rect2 West { get; init; }
        public Rect2 East { get; init; }
    }

    /// <summary>
    /// LPC character sheet layout (walk rows, frame 0 per direction).
    /// Standard walk Y offsets: north=512, west=576, south=640, east=704.
    /// All frames 64x64.
    /// </summary>
    public static readonly AtlasLayout LpcCharacterWalk = new()
    {
        North = new Rect2(0, 512, 64, 64),
        West = new Rect2(0, 576, 64, 64),
        South = new Rect2(0, 640, 64, 64),
        East = new Rect2(0, 704, 64, 64),
    };

    /// <summary>
    /// LPC monster sheet layout. The LPC Monsters pack uses 4 rows of
    /// animation frames, one row per cardinal (convention: south=0, west=1,
    /// north=2, east=3 — matches most sheets in the pack). Frame 0 (x=0) is
    /// the standing pose. Default frame size 64x64; override if needed.
    /// </summary>
    public static AtlasLayout LpcMonster(int frameW = 64, int frameH = 64) => new()
    {
        South = new Rect2(0, 0, frameW, frameH),
        West = new Rect2(0, frameH, frameW, frameH),
        North = new Rect2(0, frameH * 2, frameW, frameH),
        East = new Rect2(0, frameH * 3, frameW, frameH),
    };

    /// <summary>
    /// Load a directional sprite set from a single atlas PNG. Returns a
    /// dictionary of 8 AtlasTexture views into the same source image —
    /// diagonals share their nearest cardinal's region (acceptable for the
    /// single-frame tech-demo loop; swap in AnimatedSprite2D later for
    /// animated diagonals).
    /// </summary>
    public static Dictionary<string, Texture2D> LoadFromAtlas(
        string atlasPath, AtlasLayout layout)
    {
        var result = new Dictionary<string, Texture2D>();
        if (!ResourceLoader.Exists(atlasPath))
        {
            GD.PushWarning($"DirectionalSprite.LoadFromAtlas: missing {atlasPath}");
            return result;
        }

        var source = GD.Load<Texture2D>(atlasPath);

        AtlasTexture Atlas(Rect2 region)
        {
            var t = new AtlasTexture { Atlas = source, Region = region };
            return t;
        }

        var south = Atlas(layout.South);
        var north = Atlas(layout.North);
        var west = Atlas(layout.West);
        var east = Atlas(layout.East);

        // Cardinals
        result["south"] = south;
        result["north"] = north;
        result["west"] = west;
        result["east"] = east;
        // Diagonals → nearest cardinal (single-frame simplification)
        result["south-west"] = south;
        result["south-east"] = south;
        result["north-west"] = north;
        result["north-east"] = north;

        return result;
    }

    /// <summary>
    /// Load a single south-facing portrait frame from an LPC full-sheet atlas.
    /// Returns an AtlasTexture cropped to the walk-row south frame 0 (the
    /// neutral standing pose). UI-only helper — use this wherever a
    /// TextureRect/Sprite2D should display a character "avatar" without
    /// rendering the entire multi-row animation sheet. Returns null if the
    /// path is missing (caller decides whether to skip the node or fall back).
    /// </summary>
    public static AtlasTexture? LoadPortraitFrame(string atlasPath)
    {
        if (!ResourceLoader.Exists(atlasPath))
        {
            GD.PushWarning($"DirectionalSprite.LoadPortraitFrame: missing {atlasPath}");
            return null;
        }
        var source = GD.Load<Texture2D>(atlasPath);
        return new AtlasTexture { Atlas = source, Region = LpcCharacterWalk.South };
    }

    /// <summary>
    /// Legacy loader: 8 per-direction PNGs in a directory. Retained as a
    /// fallback path; current builds use LoadFromAtlas instead.
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

    /// <summary>Update a sprite's texture based on movement velocity.</summary>
    public static void UpdateSprite(Sprite2D sprite, Vector2 velocity,
        Dictionary<string, Texture2D> textures, ref string lastDirection)
    {
        if (velocity.LengthSquared() < 0.01f)
            return; // Keep last direction when idle

        string dir = GetDirection(velocity);
        if (dir == lastDirection)
            return;

        lastDirection = dir;
        if (textures.TryGetValue(dir, out var texture))
        {
            sprite.Texture = texture;
            sprite.FlipH = false;
        }
    }
}
