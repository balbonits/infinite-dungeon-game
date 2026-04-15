using GdUnit4;
using static GdUnit4.Assertions;

namespace DungeonGame.Tests.E2E.Assets;

// ── Sprite Viewer ─────────────────────────────────────────────────────────────

[TestSuite]
public class SpriteViewerTests
{
    private static readonly string[] Directions =
        ["south", "south-west", "west", "north-west", "north", "north-east", "east", "south-east"];

    private static readonly (string Label, string Path)[] Subjects =
    [
        ("Warrior",  "res://assets/characters/player/warrior/rotations"),
        ("Ranger",   "res://assets/characters/player/ranger/rotations"),
        ("Mage",     "res://assets/characters/player/mage/rotations"),
        ("Goblin",   "res://assets/characters/enemies/goblin/rotations"),
        ("Orc",      "res://assets/characters/enemies/orc/rotations"),
        ("Skeleton", "res://assets/characters/enemies/skeleton/rotations"),
    ];

    [TestCase]
    [RequireGodotRuntime]
    public void AllSubjects_Have8DirectionTextures()
    {
        foreach (var (label, path) in Subjects)
        {
            var textures = DirectionalSprite.LoadRotations(path);
            AssertThat(textures.Count).IsEqual(8, $"{label} should have 8 direction textures");
            foreach (var dir in Directions)
                AssertThat(textures.ContainsKey(dir)).IsTrue($"{label}/{dir} missing");
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public async System.Threading.Tasks.Task SpriteViewerScene_LoadsWithoutError()
    {
        var runner = ISceneRunner.Load("res://scenes/sandbox/assets/SpriteViewer.tscn");
        AssertThat(runner.Scene()).IsNotNull();
        await runner.SimulateFrames(5);
    }
}

// ── Tile Viewer ───────────────────────────────────────────────────────────────

[TestSuite]
public class TileViewerTests
{
    private static readonly string[] TilePaths =
    [
        "res://assets/tiles/dungeon/floor.png",
        "res://assets/tiles/dungeon/floor_cracked.png",
        "res://assets/tiles/dungeon/floor_flagstone.png",
        "res://assets/tiles/dungeon/floor_worn.png",
        "res://assets/tiles/dungeon/wall.png",
        "res://assets/tiles/dungeon/stairs_up.png",
        "res://assets/tiles/dungeon/stairs_down.png",
    ];

    [TestCase]
    [RequireGodotRuntime]
    public void AllTiles_ExistAndLoad()
    {
        foreach (var path in TilePaths)
        {
            AssertThat(Godot.ResourceLoader.Exists(path)).IsTrue($"Tile missing: {path}");
            var tex = Godot.GD.Load<Godot.Texture2D>(path);
            AssertThat(tex).IsNotNull($"Failed to load: {path}");
        }
    }
}

// ── Projectile Viewer ─────────────────────────────────────────────────────────

[TestSuite]
public class ProjectileViewerTests
{
    private static readonly string[] ProjectilePaths =
    [
        "res://assets/projectiles/arrow_8dir.png",
        "res://assets/projectiles/magic_arrow_8dir.png",
        "res://assets/projectiles/magic_bolt_8dir.png",
        "res://assets/projectiles/fireball_8dir.png",
        "res://assets/projectiles/frost_bolt_8dir.png",
        "res://assets/projectiles/lightning_8dir.png",
        "res://assets/projectiles/stone_spike_8dir.png",
        "res://assets/projectiles/energy_blast_8dir.png",
        "res://assets/projectiles/shadow_bolt_8dir.png",
    ];

    [TestCase]
    [RequireGodotRuntime]
    public void AllProjectiles_ExistAndLoad()
    {
        foreach (var path in ProjectilePaths)
        {
            AssertThat(Godot.ResourceLoader.Exists(path)).IsTrue($"Projectile missing: {path}");
            var tex = Godot.GD.Load<Godot.Texture2D>(path);
            AssertThat(tex).IsNotNull();
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void AllProjectiles_Are8DirectionSheets()
    {
        foreach (var path in ProjectilePaths)
        {
            var tex = Godot.GD.Load<Godot.Texture2D>(path);
            AssertThat(tex.GetWidth()).IsGreater(tex.GetHeight());
            AssertThat(tex.GetWidth() / tex.GetHeight()).IsEqual(8);
        }
    }
}

// ── Effect Sprites ───────────────────────────────────────────────────────────

[TestSuite]
public class EffectSpriteTests
{
    private static readonly string[] EffectPaths =
    [
        "res://assets/effects/fire_tile.png",
        "res://assets/effects/ice_ground.png",
        "res://assets/effects/poison_pool.png",
        "res://assets/effects/lava_ground.png",
        "res://assets/effects/shadow_void.png",
        "res://assets/effects/magic_circle.png",
        "res://assets/effects/heal_aura.png",
        "res://assets/effects/shield_bubble.png",
        "res://assets/effects/explosion.png",
        "res://assets/effects/water_puddle.png",
        "res://assets/effects/torch.png",
        "res://assets/effects/lightning_strike.png",
        "res://assets/effects/dust_debris.png",
        "res://assets/effects/nether_wisp.png",
        "res://assets/effects/sparkle.png",
        "res://assets/effects/poison_cloud.png",
        "res://assets/effects/cathedral_light.png",
        "res://assets/effects/volcanic_ash.png",
    ];

    [TestCase]
    [RequireGodotRuntime]
    public void AllEffects_ExistAndLoad()
    {
        foreach (var path in EffectPaths)
        {
            AssertThat(Godot.ResourceLoader.Exists(path)).IsTrue();
            var tex = Godot.GD.Load<Godot.Texture2D>(path);
            AssertThat(tex).IsNotNull();
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void AllEffects_Are6FrameSheets()
    {
        foreach (var path in EffectPaths)
        {
            var tex = Godot.GD.Load<Godot.Texture2D>(path);
            AssertThat(tex.GetWidth()).IsEqual(384);
            AssertThat(tex.GetHeight()).IsEqual(64);
        }
    }
}
