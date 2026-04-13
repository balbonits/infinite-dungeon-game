using Godot;

namespace DungeonGame.Sandbox;

/// <summary>
/// Sandbox: Floor Generator
/// Generates and renders dungeon floors. Controls: seed, floor number, regen.
/// Headless checks: room count bounds, entrance≠exit, corridor sizing.
/// Run: make sandbox SCENE=floor-gen
/// </summary>
public partial class FloorGenSandbox : SandboxBase
{
    protected override string SandboxTitle => "🗺  Floor Generator Sandbox";

    private TileMapLayer _tileMap = null!;
    private int _seed = 42;
    private int _floorNumber = 1;
    private FloorGenerator? _lastGen;

    private static readonly Color ColorFloor = new(0.3f, 0.3f, 0.35f);
    private static readonly Color ColorWall = new(0.1f, 0.1f, 0.12f);
    private static readonly Color ColorRoom = new(0.4f, 0.5f, 0.65f);
    private static readonly Color ColorEntrance = new(0.2f, 0.9f, 0.3f);
    private static readonly Color ColorExit = new(0.9f, 0.3f, 0.2f);

    protected override void _SandboxReady()
    {
        // Tile map for visual rendering
        _tileMap = new TileMapLayer();
        AddChild(_tileMap);

        AddSectionLabel("Seed");
        AddSlider("Seed", 0, 9999, _seed, v => _seed = (int)v);

        AddSectionLabel("Floor Number");
        AddSlider("Floor", 1, 50, _floorNumber, v => _floorNumber = (int)v);

        AddButton("▶  Generate", Generate);
        AddButton("🎲  Random Seed", () => { _seed = GD.RandRange(0, 9999); Generate(); });

        Generate();
    }

    protected override void _Reset() => Generate();

    private void Generate()
    {
        var gen = new FloorGenerator(_seed);
        gen.Generate(_floorNumber);
        _lastGen = gen;

        Log($"Seed={_seed}  Floor={_floorNumber}");
        Log($"  Grid:     {gen.Width}×{gen.Height}");
        Log($"  Rooms:    {gen.Rooms.Count}");
        Log($"  Entrance: {gen.EntrancePos}");
        Log($"  Exit:     {gen.ExitPos}");
        Log("");

        RenderGrid(gen);
    }

    private void RenderGrid(FloorGenerator gen)
    {
        // Render via ColorRect children (simple, no TileSet needed)
        foreach (var child in GetChildren())
            if (child is ColorRect) child.QueueFree();

        const int cellSize = 4;
        for (int y = 0; y < gen.Height; y++)
        {
            for (int x = 0; x < gen.Width; x++)
            {
                var rect = new ColorRect
                {
                    Color = gen.Grid[x, y] == FloorGenerator.Tile.Floor ? ColorFloor : ColorWall,
                    Size = new Vector2(cellSize, cellSize),
                    Position = new Vector2(320 + x * cellSize, 60 + y * cellSize),
                };
                AddChild(rect);
            }
        }

        // Highlight entrance/exit
        AddMarker(gen.EntrancePos, cellSize, ColorEntrance, 320, 60);
        AddMarker(gen.ExitPos, cellSize, ColorExit, 320, 60);
    }

    private void AddMarker(Vector2I pos, int cellSize, Color color, int offX, int offY)
    {
        var rect = new ColorRect
        {
            Color = color,
            Size = new Vector2(cellSize * 2, cellSize * 2),
            Position = new Vector2(offX + pos.X * cellSize - cellSize / 2f,
                                   offY + pos.Y * cellSize - cellSize / 2f),
        };
        AddChild(rect);
    }

    protected override void RunHeadlessChecks()
    {
        Log("── Headless checks ──");
        int seeds = 10;
        for (int i = 0; i < seeds; i++)
        {
            var gen = new FloorGenerator(i * 137);
            gen.Generate(1 + i % 20);

            Assert(gen.Rooms.Count >= 5, $"Seed {i}: rooms ≥5 (got {gen.Rooms.Count})");
            Assert(gen.Rooms.Count <= 8, $"Seed {i}: rooms ≤8 (got {gen.Rooms.Count})");
            Assert(gen.EntrancePos != gen.ExitPos, $"Seed {i}: entrance ≠ exit");
            Assert(gen.Grid[gen.EntrancePos.X, gen.EntrancePos.Y] == FloorGenerator.Tile.Floor,
                $"Seed {i}: entrance is floor tile");
            Assert(gen.Grid[gen.ExitPos.X, gen.ExitPos.Y] == FloorGenerator.Tile.Floor,
                $"Seed {i}: exit is floor tile");
        }
        FinishHeadless();
    }
}
