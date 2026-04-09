using Godot;

public partial class TestTilemap : Node2D
{
    private static readonly string[] GroundNames = {
        "stone_path", "dirt", "dirt_dark", "sand",
        "grass_dry", "grass_green", "grass_medium", "forest_ground"
    };

    private TileMapLayer _tileMap;
    private Label _infoLabel;
    private int _currentGround;
    private Camera2D _camera;

    public override void _Ready()
    {
        _camera = GetNode<Camera2D>("Camera2D");

        var ui = new CanvasLayer();
        AddChild(ui);
        var helpPanel = TestHelper.CreateStyledPanel("ISOMETRIC TILEMAP", new Vector2(12, 12), new Vector2(320, 160));
        helpPanel.Visible = true;
        helpPanel.GetNode<Label>("Content").Text =
            "1-8: switch ground type\n" +
            "Arrow keys: pan camera\n" +
            "+/-: zoom in/out\n" +
            "F12: screenshot | Esc: quit";
        ui.AddChild(helpPanel);

        _infoLabel = new Label();
        _infoLabel.Position = new Vector2(12, 200);
        _infoLabel.AddThemeColorOverride("font_color", new Color(0.925f, 0.941f, 1.0f));
        _infoLabel.AddThemeFontSizeOverride("font_size", 14);
        ui.AddChild(_infoLabel);

        LoadGround(0);
    }

    private void LoadGround(int index)
    {
        _currentGround = index;
        if (_tileMap != null) { _tileMap.QueueFree(); _tileMap = null; }

        var name = GroundNames[index];
        var tex = TestHelper.LoadPng($"res://assets/isometric/tiles/ground/{name}_64x32.png");
        if (tex == null) { GD.PrintErr($"Could not load {name}"); return; }

        var tileSet = new TileSet();
        tileSet.TileShape = TileSet.TileShapeEnum.Isometric;
        tileSet.TileSize = new Vector2I(64, 32);

        var source = new TileSetAtlasSource();
        source.Texture = tex;
        source.TextureRegionSize = new Vector2I(64, 32);
        int sourceId = tileSet.AddSource(source);

        int cols = tex.GetWidth() / 64;
        int rows = tex.GetHeight() / 32;
        for (int x = 0; x < cols; x++)
            for (int y = 0; y < rows; y++)
            {
                var c = new Vector2I(x, y);
                if (!source.HasTile(c)) source.CreateTile(c);
            }

        _tileMap = new TileMapLayer();
        _tileMap.TileSet = tileSet;
        _tileMap.YSortEnabled = true;
        AddChild(_tileMap);

        for (int x = 0; x < 12; x++)
            for (int y = 0; y < 8; y++)
                _tileMap.SetCell(new Vector2I(x, y), sourceId, new Vector2I(x % cols, 0));

        _infoLabel.Text = $"Ground: {name} ({tex.GetWidth()}x{tex.GetHeight()}, {cols}x{rows} tiles)";
        GD.Print($"[TILEMAP] Loaded {name}: {cols}x{rows} grid, painted 12x8 floor");
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (ev is InputEventKey key && key.Pressed)
        {
            if (key.Keycode >= Key.Key1 && key.Keycode <= Key.Key8)
            {
                LoadGround((int)key.Keycode - (int)Key.Key1);
            }
            else switch (key.Keycode)
            {
                case Key.Equal: _camera.Zoom *= 1.25f; break;
                case Key.Minus: _camera.Zoom /= 1.25f; break;
                case Key.F12: TestHelper.CaptureScreenshot(this, $"tilemap_{GroundNames[_currentGround]}"); break;
                case Key.Escape: GetTree().Quit(); break;
            }
        }
    }

    public override void _Process(double delta)
    {
        var pan = Vector2.Zero;
        if (Input.IsKeyPressed(Key.Left)) pan.X -= 200 * (float)delta;
        if (Input.IsKeyPressed(Key.Right)) pan.X += 200 * (float)delta;
        if (Input.IsKeyPressed(Key.Up)) pan.Y -= 200 * (float)delta;
        if (Input.IsKeyPressed(Key.Down)) pan.Y += 200 * (float)delta;
        if (pan != Vector2.Zero) _camera.Position += pan;
    }
}
