using Godot;
using System.Collections.Generic;

public partial class TestRoads : Node2D
{
    private const string RoadDir = "res://assets/isometric/tiles/roads/";
    private const string PathwayDir = "res://assets/isometric/tiles/pathways/";
    private const int TileW = 128;
    private const int TileH = 64;
    private const int TilesPerRow = 6;

    private List<(string file, string name)> _roadFiles = new();
    private List<(string file, string name)> _pathwayFiles = new();
    private int _categoryIndex; // 0 = roads, 1 = pathways
    private int _pageIndex;
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

        // Scan roads
        ScanDirectory(RoadDir, _roadFiles);
        // Scan pathways
        ScanDirectory(PathwayDir, _pathwayFiles);

        GD.Print($"[ROADS] Found {_roadFiles.Count} road sheets, {_pathwayFiles.Count} pathway sheets");

        // UI
        var ui = new CanvasLayer();
        AddChild(ui);

        var helpPanel = TestHelper.CreateStyledPanel("SBS ROADS & PATHWAYS", new Vector2(12, 12), new Vector2(360, 160));
        helpPanel.Visible = true;
        helpPanel.GetNode<Label>("Content").Text =
            "Left/Right: cycle between Roads / Pathways\n" +
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

        var isRoads = _categoryIndex == 0;
        var files = isRoads ? _roadFiles : _pathwayFiles;
        var baseDir = isRoads ? RoadDir : PathwayDir;
        var catName = isRoads ? "Roads" : "Pathways";

        if (files.Count == 0)
        {
            _infoLabel.Text = $"{catName}: no files found!";
            return;
        }

        // Each file is a sheet (e.g. 512x192 = 4x3 grid of 128x64 tiles)
        // Show all sheets, extracting tiles into a gallery
        float startX = 20;
        float groupY = 20;

        foreach (var entry in files)
        {
            var tex = TestHelper.LoadIssPng(baseDir + entry.file);
            if (tex == null) continue;

            int sheetW = tex.GetWidth();
            int sheetH = tex.GetHeight();
            int cols = sheetW / TileW;
            int rows = sheetH / TileH;

            // Show sheet as a whole sprite
            var sheetSprite = new Sprite2D();
            sheetSprite.Texture = tex;
            sheetSprite.Centered = false;
            sheetSprite.Position = new Vector2(startX, groupY);
            sheetSprite.TextureFilter = TextureFilterEnum.Nearest;
            _displayContainer.AddChild(sheetSprite);

            // Sheet label
            var label = new Label();
            label.Text = $"{entry.name}  ({sheetW}x{sheetH})";
            label.Position = new Vector2(startX + sheetW + 12, groupY + 4);
            label.AddThemeColorOverride("font_color", new Color(0.961f, 0.784f, 0.420f, 0.8f));
            label.AddThemeFontSizeOverride("font_size", 10);
            _displayContainer.AddChild(label);

            groupY += sheetH + 16;
        }

        _infoLabel.Text = $"{catName}  |  {files.Count} sheets  [Left/Right to switch category]";
        GD.Print($"[ROADS] Showing {catName}: {files.Count} sheets");
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
                    var cat = _categoryIndex == 0 ? "roads" : "pathways";
                    TestHelper.CaptureScreenshot(this, $"roads_{cat}");
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
