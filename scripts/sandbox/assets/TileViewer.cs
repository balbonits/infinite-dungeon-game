using Godot;

namespace DungeonGame.Sandbox;

/// <summary>
/// Sandbox: Tile Viewer
/// Shows all dungeon tile variants in a grid. Toggle 4×4 tiling to check seams.
/// Headless: verify all tiles load without null.
/// Run: make sandbox SCENE=tile-viewer
/// </summary>
public partial class TileViewer : SandboxBase
{
    protected override string SandboxTitle => "🧱  Tile Viewer";

    private static readonly (string Name, string Path)[] Tiles =
    [
        ("Floor",         "res://assets/tiles/dungeon/floor.png"),
        ("Floor Cracked", "res://assets/tiles/dungeon/floor_cracked.png"),
        ("Floor Flagstone","res://assets/tiles/dungeon/floor_flagstone.png"),
        ("Floor Worn",    "res://assets/tiles/dungeon/floor_worn.png"),
        ("Wall",          "res://assets/tiles/dungeon/wall.png"),
        ("Stairs Up",     "res://assets/tiles/dungeon/stairs_up.png"),
        ("Stairs Down",   "res://assets/tiles/dungeon/stairs_down.png"),
    ];

    private bool _showTiling = false;

    protected override void _SandboxReady()
    {
        AddSectionLabel("View");
        AddButton("Single tiles", () => { _showTiling = false; RenderGrid(); });
        AddButton("4×4 tiling", () => { _showTiling = true; RenderGrid(); });
        RenderGrid();
    }

    protected override void _Reset() { _showTiling = false; RenderGrid(); }

    private void RenderGrid()
    {
        foreach (var child in GetChildren())
            if (child is TextureRect) child.QueueFree();

        const int size = 80;
        const int pad = 12;
        const int startX = 340;
        const int startY = 80;
        int repeat = _showTiling ? 4 : 1;

        for (int t = 0; t < Tiles.Length; t++)
        {
            var (name, path) = Tiles[t];
            var tex = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;

            int col = t % 4;
            int row = t / 4;
            int baseX = startX + col * (size * repeat + pad);
            int baseY = startY + row * (size * repeat + pad + 20);

            // Label
            var lbl = new Label
            {
                Text = name,
                Position = new Vector2(baseX, baseY - 18),
                ZIndex = 1,
            };
            lbl.AddThemeFontSizeOverride("font_size"", Ui.UiTheme.FontSizes.Small);
            AddChild(lbl);

            for (int r = 0; r < repeat; r++)
                for (int c = 0; c < repeat; c++)
                {
                    var rect = new TextureRect
                    {
                        Texture = tex,
                        CustomMinimumSize = new Vector2(size, size),
                        Size = new Vector2(size, size),
                        Position = new Vector2(baseX + c * size, baseY + r * size),
                        StretchMode = TextureRect.StretchModeEnum.Scale,
                    };
                    AddChild(rect);
                }

            Log($"{name}: {(tex != null ? $"✅ {tex.GetWidth()}×{tex.GetHeight()}px" : "❌ missing")}");
        }
    }

    protected override void RunHeadlessChecks()
    {
        Log("── Headless checks ──");
        foreach (var (name, path) in Tiles)
        {
            bool exists = ResourceLoader.Exists(path);
            Assert(exists, $"{name}: file exists at {path}");
            if (exists)
            {
                var tex = GD.Load<Texture2D>(path);
                Assert(tex != null, $"{name}: loads as Texture2D");
            }
        }
        FinishHeadless();
    }
}
