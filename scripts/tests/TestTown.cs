using Godot;
using System.Collections.Generic;

public partial class TestTown : Node2D
{
    private const string TownDir = "res://assets/isometric/tiles/town/";

    private List<(string file, string name)> _sheetFiles = new();
    private int _currentIndex;
    private Node2D _displayContainer;
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

        // Scan town sheets
        var diskDir = ProjectSettings.GlobalizePath(TownDir);
        if (DirAccess.DirExistsAbsolute(diskDir))
        {
            var dir = DirAccess.Open(diskDir);
            dir.ListDirBegin();
            string file;
            while ((file = dir.GetNext()) != "")
            {
                if (file.EndsWith(".png") && !file.StartsWith("."))
                {
                    var name = file.Replace(".png", "").Replace("_", " ").Replace("-", " ");
                    _sheetFiles.Add((file, name));
                }
            }
            dir.ListDirEnd();
            _sheetFiles.Sort((a, b) => string.Compare(a.file, b.file));
        }

        GD.Print($"[TOWN] Found {_sheetFiles.Count} town sheets");

        // UI
        var ui = new CanvasLayer();
        AddChild(ui);

        var helpPanel = TestHelper.CreateStyledPanel("SBS TOWN BUILDINGS", new Vector2(12, 12), new Vector2(360, 160));
        helpPanel.Visible = true;
        helpPanel.GetNode<Label>("Content").Text =
            "Left/Right: cycle town sheet\n" +
            "Arrow Up/Down: pan camera\n" +
            "+/-: zoom in/out\n" +
            "F12: screenshot | Esc: quit";
        ui.AddChild(helpPanel);

        _infoLabel = new Label();
        _infoLabel.Position = new Vector2(12, 190);
        _infoLabel.AddThemeColorOverride("font_color", new Color(0.925f, 0.941f, 1.0f));
        _infoLabel.AddThemeFontSizeOverride("font_size", 14);
        ui.AddChild(_infoLabel);

        if (_sheetFiles.Count > 0)
            LoadSheet(0);
        else
            _infoLabel.Text = "No town sheets found!";
    }

    private void LoadSheet(int index)
    {
        _currentIndex = index;
        _displayContainer?.QueueFree();
        _displayContainer = new Node2D();
        AddChild(_displayContainer);

        var entry = _sheetFiles[index];
        var tex = TestHelper.LoadIssPng(TownDir + entry.file);
        if (tex == null) { _infoLabel.Text = $"Failed to load: {entry.file}"; return; }

        int sheetW = tex.GetWidth();
        int sheetH = tex.GetHeight();

        // Show the full sheet
        var sprite = new Sprite2D();
        sprite.Texture = tex;
        sprite.Centered = false;
        sprite.Position = new Vector2(20, 20);
        sprite.TextureFilter = TextureFilterEnum.Nearest;
        _displayContainer.AddChild(sprite);

        // Detect tile size from filename and show grid overlay info
        string tileInfo = "";
        if (entry.file.Contains("64x96"))
        {
            int cols = sheetW / 64;
            int rows = sheetH / 96;
            tileInfo = $"  |  {cols}x{rows} grid of 64x96 buildings ({cols * rows} total)";
        }
        else if (entry.file.Contains("143x92"))
        {
            int cols = sheetW / 143;
            int rows = sheetH / 92;
            tileInfo = $"  |  {cols}x{rows} grid of 143x92 roofs ({cols * rows} total)";
        }

        _infoLabel.Text = $"{entry.name}  |  {sheetW}x{sheetH}{tileInfo}  [{index + 1}/{_sheetFiles.Count}]";
        GD.Print($"[TOWN] {entry.file}: {sheetW}x{sheetH}{tileInfo}");
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (_sheetFiles.Count == 0) return;
        if (ev is InputEventKey key && key.Pressed)
        {
            switch (key.Keycode)
            {
                case Key.Right:
                    LoadSheet((_currentIndex + 1) % _sheetFiles.Count);
                    break;
                case Key.Left:
                    LoadSheet((_currentIndex - 1 + _sheetFiles.Count) % _sheetFiles.Count);
                    break;
                case Key.Equal: _camera.Zoom *= 1.25f; break;
                case Key.Minus: _camera.Zoom /= 1.25f; break;
                case Key.F12:
                    var name = _sheetFiles[_currentIndex].file.Replace(".png", "").ToLower();
                    TestHelper.CaptureScreenshot(this, $"town_{name}");
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
