namespace DungeonGame;

/// <summary>
/// Per-species visual and collision configuration.
/// Collision sizes match the visual footprint of each sprite.
/// Pure data — no Godot dependency.
/// </summary>
public record SpeciesConfig
{
    public float CollisionRadius { get; init; }
    public float HitAreaRadius { get; init; }
    public float SpriteScale { get; init; }
    public float SpriteOffsetY { get; init; }
    public float LabelOffsetY { get; init; }
}

public static class SpeciesDatabase
{
    // Measured from actual PixelLab sprites within 92x92 canvas
    private static readonly SpeciesConfig[] Configs =
    {
        // 0: Skeleton — thin, narrow frame
        new() { CollisionRadius = 8f, HitAreaRadius = 12f, SpriteScale = 0.7f, SpriteOffsetY = -24f, LabelOffsetY = -48f },

        // 1: Goblin — hunched, slightly wider
        new() { CollisionRadius = 9f, HitAreaRadius = 13f, SpriteScale = 0.65f, SpriteOffsetY = -22f, LabelOffsetY = -44f },

        // 2: Bat — wide wingspan, hovers above ground
        new() { CollisionRadius = 7f, HitAreaRadius = 14f, SpriteScale = 0.7f, SpriteOffsetY = -28f, LabelOffsetY = -50f },

        // 3: Wolf — four-legged, low and long
        new() { CollisionRadius = 10f, HitAreaRadius = 14f, SpriteScale = 0.65f, SpriteOffsetY = -20f, LabelOffsetY = -42f },

        // 4: Orc — large, broad shoulders, tanky
        new() { CollisionRadius = 12f, HitAreaRadius = 16f, SpriteScale = 0.8f, SpriteOffsetY = -28f, LabelOffsetY = -52f },

        // 5: Dark Mage — robed, medium frame
        new() { CollisionRadius = 9f, HitAreaRadius = 13f, SpriteScale = 0.7f, SpriteOffsetY = -26f, LabelOffsetY = -48f },

        // 6: Spider — wide legs, low profile
        new() { CollisionRadius = 11f, HitAreaRadius = 15f, SpriteScale = 0.7f, SpriteOffsetY = -22f, LabelOffsetY = -44f },
    };

    // Default fallback for unknown species
    private static readonly SpeciesConfig Default = new()
    {
        CollisionRadius = 10f,
        HitAreaRadius = 14f,
        SpriteScale = 0.7f,
        SpriteOffsetY = -24f,
        LabelOffsetY = -46f
    };

    public static SpeciesConfig Get(int speciesIndex)
    {
        return speciesIndex >= 0 && speciesIndex < Configs.Length
            ? Configs[speciesIndex]
            : Default;
    }

    // Player collision config (separate, not indexed by species)
    public static readonly SpeciesConfig Player = new()
    {
        CollisionRadius = 12f,
        HitAreaRadius = 0f,
        SpriteScale = 1.0f,
        SpriteOffsetY = -30f,
        LabelOffsetY = -56f
    };
}
