using GdUnit4;
using static GdUnit4.Assertions;

namespace DungeonGame.Tests.E2E.Assets;

// ── Sprite Viewer ─────────────────────────────────────────────────────────────

[TestSuite]
public class SpriteViewerTests
{
    private static readonly string[] Directions =
        ["south", "south-west", "west", "north-west", "north", "north-east", "east", "south-east"];

    // Tuple: (label, atlas path, atlas layout). After the LPC pivot (ADR-007),
    // sprites load via DirectionalSprite.LoadFromAtlas from single-sheet PNGs
    // at known region offsets; the legacy per-direction PNG directories are
    // archived at assets/archive/pixellab-iso-sprites/.
    private static readonly (string Label, string Path, DirectionalSprite.AtlasLayout Layout)[] Subjects =
    [
        ("Warrior",  "res://assets/characters/player/warrior/warrior_full_sheet.png", DirectionalSprite.LpcCharacterWalk),
        ("Ranger",   "res://assets/characters/player/ranger/ranger_full_sheet.png",   DirectionalSprite.LpcCharacterWalk),
        ("Mage",     "res://assets/characters/player/mage/mage_full_sheet.png",       DirectionalSprite.LpcCharacterWalk),
        ("Goblin",   "res://assets/downloaded/lpc_monsters/lpc-monsters/small_worm.png", DirectionalSprite.LpcMonster()),
        ("Orc",      "res://assets/downloaded/lpc_monsters/lpc-monsters/pumpking.png",   DirectionalSprite.LpcMonster()),
        ("Skeleton", "res://assets/downloaded/lpc_monsters/lpc-monsters/ghost.png",      DirectionalSprite.LpcMonster()),
    ];

    [TestCase]
    [RequireGodotRuntime]
    public void AllSubjects_Have8DirectionTextures()
    {
        foreach (var (label, path, layout) in Subjects)
        {
            var textures = DirectionalSprite.LoadFromAtlas(path, layout);
            AssertThat(textures.Count).OverrideFailureMessage($"{label} should have 8 direction textures").IsEqual(8);
            foreach (var dir in Directions)
                AssertThat(textures.ContainsKey(dir)).OverrideFailureMessage($"{label}/{dir} missing").IsTrue();
        }
    }

    // Guards the LPC atlas contract (ADR-007): each direction must resolve to
    // an AtlasTexture with non-zero region inside the source atlas bounds.
    // Catches silent fallbacks (e.g., returning the full sheet instead of a
    // cropped region) that DirectionalSprite.LoadFromAtlas could regress into.
    [TestCase]
    [RequireGodotRuntime]
    public void AllSubjects_AtlasRegions_AreBounded()
    {
        foreach (var (label, path, layout) in Subjects)
        {
            var textures = DirectionalSprite.LoadFromAtlas(path, layout);
            var source = Godot.GD.Load<Godot.Texture2D>(path);
            int sw = source.GetWidth(), sh = source.GetHeight();
            foreach (var dir in Directions)
            {
                var tex = textures[dir];
                AssertThat(tex).IsInstanceOf<Godot.AtlasTexture>();
                var atlas = (Godot.AtlasTexture)tex;
                AssertThat(atlas.Region.Size.X).IsGreater(0);
                AssertThat(atlas.Region.Size.Y).IsGreater(0);
                AssertThat(atlas.Region.Position.X + atlas.Region.Size.X).IsLessEqual(sw);
                AssertThat(atlas.Region.Position.Y + atlas.Region.Size.Y).IsLessEqual(sh);
            }
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
            AssertThat(Godot.ResourceLoader.Exists(path)).OverrideFailureMessage($"Tile missing: {path}").IsTrue();
            var tex = Godot.GD.Load<Godot.Texture2D>(path);
            AssertThat(tex).OverrideFailureMessage($"Failed to load: {path}").IsNotNull();
        }
    }

    // Guards against an accidental re-import of 64x32 iso-era tiles
    // (ADR-007 locked the grid to 32x32 top-down).
    [TestCase]
    [RequireGodotRuntime]
    public void AllTiles_Are32x32_TopDown()
    {
        var townPaths = new[]
        {
            "res://assets/tiles/town/town_floor.png",
            "res://assets/tiles/town/town_wall.png",
        };
        foreach (var path in System.Linq.Enumerable.Concat(TilePaths, townPaths))
        {
            var tex = Godot.GD.Load<Godot.Texture2D>(path);
            AssertThat(tex.GetWidth()).IsEqual(32);
            AssertThat(tex.GetHeight()).IsEqual(32);
        }
    }
}

// ── NPC Atlas Contract ────────────────────────────────────────────────────────

[TestSuite]
public class NpcAtlasTests
{
    private static readonly (string Npc, string Path)[] Sheets =
    [
        ("guild_maid",    "res://assets/characters/npcs/guild_maid/guild_maid_full_sheet.png"),
        ("blacksmith",    "res://assets/characters/npcs/blacksmith/blacksmith_full_sheet.png"),
        ("village_chief", "res://assets/characters/npcs/village_chief/village_chief_full_sheet.png"),
    ];

    [TestCase]
    [RequireGodotRuntime]
    public void AllNpcSheets_LoadSouthFrame()
    {
        foreach (var (npc, path) in Sheets)
        {
            AssertThat(Godot.ResourceLoader.Exists(path)).OverrideFailureMessage($"{npc} sheet missing: {path}").IsTrue();
            var textures = DirectionalSprite.LoadFromAtlas(path, DirectionalSprite.LpcCharacterWalk);
            AssertThat(textures.ContainsKey("south")).OverrideFailureMessage($"{npc}/south region missing").IsTrue();
            AssertThat(textures["south"]).IsInstanceOf<Godot.AtlasTexture>();
        }
    }

    // Regression guard: iso-era NPC sprite paths (`rotations/south.png`)
    // are archived; nothing at runtime should resolve under the archive dir.
    [TestCase]
    [RequireGodotRuntime]
    public void IsoArchive_IsNotReferenced_ByNpcSheets()
    {
        foreach (var (_, path) in Sheets)
            AssertThat(path.Contains("archive")).IsFalse();
        foreach (var (_, path) in Sheets)
            AssertThat(path.Contains("rotations/")).IsFalse();
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
            AssertThat(Godot.ResourceLoader.Exists(path)).OverrideFailureMessage($"Projectile missing: {path}").IsTrue();
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
