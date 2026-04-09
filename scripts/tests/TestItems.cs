using Godot;
using System.Collections.Generic;

public partial class TestItems : Node2D
{
    private static readonly (string dir, string label, bool isStrip, int frameW, int frameH)[] Categories = {
        ("res://assets/isometric/objects/crates/sheets/", "Crates (64x64 sheets)", true, 64, 64),
        ("res://assets/isometric/objects/crates/textures/", "Crate Textures (64x64)", false, 64, 64),
        ("res://assets/isometric/objects/stairs/", "Stairs (128x128)", false, 0, 0),
        ("res://assets/isometric/objects/copings/", "Wall Copings", false, 0, 0),
        ("res://assets/isometric/objects/temple/", "Temple Kit", false, 0, 0),
    };

    private int _catIndex;
    private int _itemIndex;
    private List<string> _currentFiles = new();
    private Node2D _displayContainer;
    private Label _infoLabel;
    private Label _catLabel;
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

        // Floor grid for scale reference
        TestHelper.CreateFloorGrid(this, new Vector2(-100, 200), 8, 4);

        // UI
        var ui = new CanvasLayer();
        AddChild(ui);

        var helpPanel = TestHelper.CreateStyledPanel("DUNGEON ITEMS & OBJECTS", new Vector2(12, 12), new Vector2(360, 180));
        helpPanel.Visible = true;
        helpPanel.GetNode<Label>("Content").Text =
            "Up/Down: switch category\n" +
            "Left/Right: cycle items in category\n" +
            "+/-: zoom in/out\n" +
            "F12: screenshot | Esc: quit";
        ui.AddChild(helpPanel);

        _catLabel = new Label();
        _catLabel.Position = new Vector2(12, 210);
        _catLabel.AddThemeColorOverride("font_color", new Color(0.961f, 0.784f, 0.420f));
        _catLabel.AddThemeFontSizeOverride("font_size", 14);
        ui.AddChild(_catLabel);

        _infoLabel = new Label();
        _infoLabel.Position = new Vector2(12, 232);
        _infoLabel.AddThemeColorOverride("font_color", new Color(0.925f, 0.941f, 1.0f));
        _infoLabel.AddThemeFontSizeOverride("font_size", 13);
        ui.AddChild(_infoLabel);

        LoadCategory(0);
    }

    private void LoadCategory(int catIdx)
    {
        _catIndex = catIdx;
        _itemIndex = 0;
        _currentFiles.Clear();

        var cat = Categories[catIdx];
        var diskDir = ProjectSettings.GlobalizePath(cat.dir);
        if (DirAccess.DirExistsAbsolute(diskDir))
        {
            var dir = DirAccess.Open(diskDir);
            dir.ListDirBegin();
            string file;
            while ((file = dir.GetNext()) != "")
            {
                if (file.EndsWith(".png") && !file.StartsWith("."))
                    _currentFiles.Add(file);
            }
            dir.ListDirEnd();
            _currentFiles.Sort();
        }

        _catLabel.Text = $"Category: {cat.label} ({_currentFiles.Count} files)";
        GD.Print($"[ITEMS] Category: {cat.label}, {_currentFiles.Count} files");

        if (_currentFiles.Count > 0)
            LoadItem(0);
        else
            _infoLabel.Text = "No files found";
    }

    private void LoadItem(int idx)
    {
        _itemIndex = idx;
        _displayContainer?.QueueFree();
        _displayContainer = new Node2D();
        AddChild(_displayContainer);

        var cat = Categories[_catIndex];
        var fileName = _currentFiles[idx];
        var tex = TestHelper.LoadIssPng(cat.dir + fileName);
        if (tex == null) { _infoLabel.Text = $"Failed: {fileName}"; return; }

        int texW = tex.GetWidth();
        int texH = tex.GetHeight();

        if (cat.isStrip && cat.frameW > 0)
        {
            // Sprite strip — show full strip + extract individual frames
            var fullSprite = new Sprite2D();
            fullSprite.Texture = tex;
            fullSprite.Centered = false;
            fullSprite.Position = new Vector2(20, 20);
            fullSprite.TextureFilter = TextureFilterEnum.Nearest;
            _displayContainer.AddChild(fullSprite);

            // Extract individual frames below
            int cols = texW / cat.frameW;
            int rows = texH / cat.frameH;
            float y = texH + 40;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var atlas = new AtlasTexture();
                    atlas.Atlas = tex;
                    atlas.Region = new Rect2(c * cat.frameW, r * cat.frameH, cat.frameW, cat.frameH);

                    var sprite = new Sprite2D();
                    sprite.Texture = atlas;
                    sprite.Centered = false;
                    sprite.Position = new Vector2(20 + c * (cat.frameW + 8), y + r * (cat.frameH + 8));
                    sprite.TextureFilter = TextureFilterEnum.Nearest;
                    _displayContainer.AddChild(sprite);
                }
            }
        }
        else
        {
            // Single image — display at center with reasonable scale
            float maxDim = Mathf.Max(texW, texH);
            float scale = maxDim > 200 ? 200f / maxDim : 1f;

            var sprite = new Sprite2D();
            sprite.Texture = tex;
            sprite.Position = new Vector2(200, 150);
            sprite.Scale = new Vector2(scale, scale);
            sprite.TextureFilter = TextureFilterEnum.Nearest;
            _displayContainer.AddChild(sprite);

            // Size label
            var dimLabel = new Label();
            dimLabel.Text = $"{texW}x{texH}";
            dimLabel.Position = new Vector2(170, 280);
            dimLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.8f, 0.9f, 0.6f));
            dimLabel.AddThemeFontSizeOverride("font_size", 10);
            _displayContainer.AddChild(dimLabel);
        }

        var cleanName = fileName.Replace(".png", "").Replace("_", " ");
        _infoLabel.Text = $"{cleanName}  ({texW}x{texH})  [{idx + 1}/{_currentFiles.Count}]";
        GD.Print($"[ITEMS] {fileName}: {texW}x{texH}");
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (ev is InputEventKey key && key.Pressed)
        {
            switch (key.Keycode)
            {
                case Key.Right:
                    if (_currentFiles.Count > 0) LoadItem((_itemIndex + 1) % _currentFiles.Count);
                    break;
                case Key.Left:
                    if (_currentFiles.Count > 0) LoadItem((_itemIndex - 1 + _currentFiles.Count) % _currentFiles.Count);
                    break;
                case Key.Down:
                    LoadCategory((_catIndex + 1) % Categories.Length);
                    break;
                case Key.Up:
                    LoadCategory((_catIndex - 1 + Categories.Length) % Categories.Length);
                    break;
                case Key.Equal: _camera.Zoom *= 1.25f; break;
                case Key.Minus: _camera.Zoom /= 1.25f; break;
                case Key.F12: TestHelper.CaptureScreenshot(this, $"items_{_currentFiles[_itemIndex].Replace(".png", "")}"); break;
                case Key.Escape: GetTree().Quit(); break;
            }
        }
    }
}
