using Godot;
using System.Collections.Generic;

public partial class TestObjects : Node2D
{
    private static readonly (string dir, string label)[] Categories = {
        ("res://assets/isometric/objects/stairs/", "Stairs"),
        ("res://assets/isometric/objects/copings/", "Copings"),
        ("res://assets/isometric/objects/temple/", "Temple"),
    };

    private List<List<(string file, string name)>> _categoryFiles = new();
    private int _categoryIndex;
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

        // Scan each category
        foreach (var cat in Categories)
        {
            var files = new List<(string file, string name)>();
            var diskDir = ProjectSettings.GlobalizePath(cat.dir);
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
                        files.Add((file, name));
                    }
                }
                dir.ListDirEnd();
                files.Sort((a, b) => string.Compare(a.file, b.file));
            }
            _categoryFiles.Add(files);
            GD.Print($"[OBJECTS] {cat.label}: found {files.Count} files");
        }

        // UI
        var ui = new CanvasLayer();
        AddChild(ui);

        var helpPanel = TestHelper.CreateStyledPanel("SBS OBJECTS", new Vector2(12, 12), new Vector2(380, 160));
        helpPanel.Visible = true;
        helpPanel.GetNode<Label>("Content").Text =
            "Left/Right: cycle Stairs / Copings / Temple\n" +
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

    private void LoadCategory(int catIndex)
    {
        _categoryIndex = catIndex;
        _displayContainer?.QueueFree();
        _displayContainer = new Node2D();
        AddChild(_displayContainer);

        var catLabel = Categories[catIndex].label;
        var baseDir = Categories[catIndex].dir;
        var files = _categoryFiles[catIndex];

        if (files.Count == 0)
        {
            _infoLabel.Text = $"{catLabel}: no files found!";
            return;
        }

        float startX = 20;
        float currentY = 20;

        foreach (var entry in files)
        {
            var tex = TestHelper.LoadIssPng(baseDir + entry.file);
            if (tex == null) continue;

            int w = tex.GetWidth();
            int h = tex.GetHeight();

            // Label
            var label = new Label();
            label.Text = $"{entry.name}  ({w}x{h})";
            label.Position = new Vector2(startX, currentY);
            label.AddThemeColorOverride("font_color", new Color(0.961f, 0.784f, 0.420f, 0.8f));
            label.AddThemeFontSizeOverride("font_size", 10);
            _displayContainer.AddChild(label);

            currentY += 16;

            // Show the sprite sheet
            var sprite = new Sprite2D();
            sprite.Texture = tex;
            sprite.Centered = false;
            sprite.Position = new Vector2(startX, currentY);
            sprite.TextureFilter = TextureFilterEnum.Nearest;
            _displayContainer.AddChild(sprite);

            currentY += h + 20;
        }

        _infoLabel.Text = $"{catLabel}  |  {files.Count} assets  [{catIndex + 1}/{Categories.Length}]";
        GD.Print($"[OBJECTS] Showing {catLabel}: {files.Count} assets");
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (ev is InputEventKey key && key.Pressed)
        {
            switch (key.Keycode)
            {
                case Key.Right:
                    LoadCategory((_categoryIndex + 1) % Categories.Length);
                    break;
                case Key.Left:
                    LoadCategory((_categoryIndex - 1 + Categories.Length) % Categories.Length);
                    break;
                case Key.Equal: _camera.Zoom *= 1.25f; break;
                case Key.Minus: _camera.Zoom /= 1.25f; break;
                case Key.F12:
                    var cat = Categories[_categoryIndex].label.ToLower();
                    TestHelper.CaptureScreenshot(this, $"objects_{cat}");
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
