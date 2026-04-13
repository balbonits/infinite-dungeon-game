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
        "res://assets/projectiles/arrow.png",
        "res://assets/projectiles/energy_blast.png",
        "res://assets/projectiles/fireball.png",
        "res://assets/projectiles/frost_bolt.png",
        "res://assets/projectiles/lightning.png",
        "res://assets/projectiles/magic_bolt.png",
        "res://assets/projectiles/shadow_bolt.png",
        "res://assets/projectiles/stone_spike.png",
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
}
