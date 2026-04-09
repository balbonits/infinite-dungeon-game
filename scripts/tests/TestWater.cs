using Godot;
using System.Collections.Generic;

public partial class TestWater : Node2D
{
    private const string WaterDir = "res://assets/isometric/tiles/water/";
    private const string AutotileDir = "res://assets/isometric/tiles/autotiles/";

    private List<(string file, string name)> _waterFiles = new();
    private List<(string file, string name)> _autotileFiles = new();
    private int _categoryIndex; // 0 = water, 1 = autotiles
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

        // Scan water tiles
        ScanDirectory(WaterDir, _waterFiles);
        // Scan autotiles
        ScanDirectory(AutotileDir, _autotileFiles);

        GD.Print($"[WATER] Found {_waterFiles.Count} water tiles, {_autotileFiles.Count} autotile sheets");

        // UI
        var ui = new CanvasLayer();
        AddChild(ui);

        var helpPanel = TestHelper.CreateStyledPanel("SBS WATER & AUTOTILES", new Vector2(12, 12), new Vector2(360, 160));
        helpPanel.Visible = true;
        helpPanel.GetNode<Label>("Content").Text =
            "Left/Right: cycle between Water / Autotiles\n" +
            "Arrow Up/Down: pan camera\n" +
            "+/-: zoom in/out\n" +
            "F12: screenshot | Esc: quit";
        ui.AddChild(helpPanel);

        _infoLabel = new Label();
        _infoLabel.Position = new Vector2(12, 190);
        _infoLabel.AddThemeColorOverride("font_color", new Color(0.925f, 0.941f, 1.0f));
        _infoLabel.AddThemeFontSizeOverride("font_size", 14);
        ui.AddChild(_infoLabel);

        LoadCategory(0);
    }

    private void ScanDirectory(string resDir, List<(string file, string name)> list)
    {
        var diskDir = ProjectSettings.GlobalizePath(resDir);
        if (!DirAccess.DirExistsAbsolute(diskDir)) return;

        var dir = DirAccess.Open(diskDir);
        dir.ListDirBegin();
        string file;
        while ((file = dir.GetNext()) != "")
        {
            if (file.EndsWith(".png") && !file.StartsWith("."))
            {
                var name = file.Replace(".png", "").Replace("_", " ");
                list.Add((file, name));
            }
        }
        dir.ListDirEnd();
        list.Sort((a, b) => string.Compare(a.file, b.file));
    }

    private void LoadCategory(int catIndex)
    {
        _categoryIndex = catIndex;
        _displayContainer?.QueueFree();
        _displayContainer = new Node2D();
        AddChild(_displayContainer);

        var isWater = _categoryIndex == 0;
        var files = isWater ? _waterFiles : _autotileFiles;
        var baseDir = isWater ? WaterDir : AutotileDir;
        var catName = isWater ? "Water Tiles" : "Autotiles";

        if (files.Count == 0)
        {
            _infoLabel.Text = $"{catName}: no files found!";
            return;
        }

        float startX = 20;
        float currentY = 20;

        foreach (var entry in files)
        {
            var tex = TestHelper.LoadIssPng(baseDir + entry.file);
            if (tex == null) continue;

            int sheetW = tex.GetWidth();
            int sheetH = tex.GetHeight();

            // Label for this tile/sheet
            var label = new Label();
            label.Text = $"{entry.name}  ({sheetW}x{sheetH})";
            label.Position = new Vector2(startX, currentY);
            label.AddThemeColorOverride("font_color", new Color(0.961f, 0.784f, 0.420f, 0.8f));
            label.AddThemeFontSizeOverride("font_size", 10);
            _displayContainer.AddChild(label);

            currentY += 16;

            // Show the full sheet/tile
            var sprite = new Sprite2D();
            sprite.Texture = tex;
            sprite.Centered = false;
            sprite.Position = new Vector2(startX, currentY);
            sprite.TextureFilter = TextureFilterEnum.Nearest;
            _displayContainer.AddChild(sprite);

            currentY += sheetH + 20;
        }

        _infoLabel.Text = $"{catName}  |  {files.Count} sheets  [Left/Right to switch category]";
        GD.Print($"[WATER] Showing {catName}: {files.Count} sheets");
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (ev is InputEventKey key && key.Pressed)
        {
            switch (key.Keycode)
            {
                case Key.Right:
                    LoadCategory((_categoryIndex + 1) % 2);
                    break;
                case Key.Left:
                    LoadCategory((_categoryIndex + 1) % 2);
                    break;
                case Key.Equal: _camera.Zoom *= 1.25f; break;
                case Key.Minus: _camera.Zoom /= 1.25f; break;
                case Key.F12:
                    var cat = _categoryIndex == 0 ? "water" : "autotiles";
                    TestHelper.CaptureScreenshot(this, $"water_{cat}");
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
