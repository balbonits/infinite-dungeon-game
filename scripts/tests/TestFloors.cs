using Godot;
using System.Collections.Generic;

public partial class TestFloors : Node2D
{
    private const string FloorDir = "res://assets/isometric/tiles/stone-soup/floors/";
    private const int TileW = 64;
    private const int TileH = 32;
    private const int GridCols = 12;
    private const int GridRows = 8;

    private List<string> _floorNames = new();
    private int _currentIndex;
    private TileMapLayer _tileMap;
    private Label _infoLabel;
    private Camera2D _camera;

    public override void _Ready()
    {
        var bg = new ColorRect();
        bg.Color = new Color(0.12f, 0.12f, 0.15f);
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var bgLayer = new CanvasLayer { Layer = -1 };
        bgLayer.AddChild(bg);
        AddChild(bgLayer);

        _camera = GetNode<Camera2D>("Camera2D");

        // Scan for floor PNGs
        var diskDir = ProjectSettings.GlobalizePath(FloorDir);
        if (DirAccess.DirExistsAbsolute(diskDir))
        {
            var dir = DirAccess.Open(diskDir);
            dir.ListDirBegin();
            string file;
            while ((file = dir.GetNext()) != "")
            {
                if (file.EndsWith(".png") && !file.StartsWith("."))
                    _floorNames.Add(file);
            }
            dir.ListDirEnd();
            _floorNames.Sort();
        }

        GD.Print($"[FLOORS] Found {_floorNames.Count} floor sheets");

        // UI
        var ui = new CanvasLayer();
        AddChild(ui);

        var helpPanel = TestHelper.CreatePanel("ISS FLOORS", new Vector2(12, 12), new Vector2(340, 160));
        helpPanel.Visible = true;
        helpPanel.GetNode<Label>("Content").Text =
            "Left/Right: cycle floor theme\n" +
            "Arrow Up/Down: pan camera\n" +
            "+/-: zoom in/out\n" +
            "F12: screenshot | Esc: quit";
        ui.AddChild(helpPanel);

        _infoLabel = new Label();
        _infoLabel.Position = new Vector2(12, 190);
        _infoLabel.AddThemeColorOverride("font_color", new Color(0.92f, 0.94f, 1.0f));
        _infoLabel.AddThemeFontSizeOverride("font_size", 13);
        ui.AddChild(_infoLabel);

        if (_floorNames.Count > 0)
            LoadFloor(0);
        else
            _infoLabel.Text = "No floor sheets found!";
    }

    private void LoadFloor(int index)
    {
        _currentIndex = index;
        if (_tileMap != null) { _tileMap.QueueFree(); _tileMap = null; }

        var fileName = _floorNames[index];
        var tex = TestHelper.LoadIssPng(FloorDir + fileName);
        if (tex == null) { GD.PrintErr($"Could not load {fileName}"); return; }

        int cols = tex.GetWidth() / TileW;
        int rows = tex.GetHeight() / TileH;
        int variants = cols * rows;

        // Build TileSet from this sheet
        var tileSet = new TileSet();
        tileSet.TileShape = TileSet.TileShapeEnum.Isometric;
        tileSet.TileSize = new Vector2I(TileW, TileH);

        var source = new TileSetAtlasSource();
        source.Texture = tex;
        source.TextureRegionSize = new Vector2I(TileW, TileH);
        int sourceId = tileSet.AddSource(source);

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

        // Paint grid, cycling through variants
        int v = 0;
        for (int gx = 0; gx < GridCols; gx++)
            for (int gy = 0; gy < GridRows; gy++)
            {
                int tx = v % cols;
                int ty = (v / cols) % rows;
                _tileMap.SetCell(new Vector2I(gx, gy), sourceId, new Vector2I(tx, ty));
                v = (v + 1) % variants;
            }

        // Extract clean theme name from filename
        var theme = fileName
            .Replace("ISS_Floor_", "").Replace("ISS_Water_", "Water: ")
            .Replace("-64x32.png", "").Replace("_", " ");

        _infoLabel.Text = $"{theme}  ({cols}x{rows} = {variants} variants)  [{index + 1}/{_floorNames.Count}]";
        GD.Print($"[FLOORS] {fileName}: {tex.GetWidth()}x{tex.GetHeight()}, {variants} variants, painted {GridCols}x{GridRows} grid");
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (_floorNames.Count == 0) return;
        if (ev is InputEventKey key && key.Pressed)
        {
            switch (key.Keycode)
            {
                case Key.Right:
                    LoadFloor((_currentIndex + 1) % _floorNames.Count);
                    break;
                case Key.Left:
                    LoadFloor((_currentIndex - 1 + _floorNames.Count) % _floorNames.Count);
                    break;
                case Key.Equal: _camera.Zoom *= 1.25f; break;
                case Key.Minus: _camera.Zoom /= 1.25f; break;
                case Key.F12:
                    var name = _floorNames[_currentIndex].Replace(".png", "").ToLower();
                    TestHelper.CaptureScreenshot(this, $"floor_{name}");
                    break;
                case Key.Escape: GetTree().Quit(); break;
            }
        }
    }

    public override void _Process(double delta)
    {
        var pan = Vector2.Zero;
        if (Input.IsKeyPressed(Key.Up)) pan.Y -= 200 * (float)delta;
        if (Input.IsKeyPressed(Key.Down)) pan.Y += 200 * (float)delta;
        if (pan != Vector2.Zero) _camera.Position += pan;
    }
}
