using Godot;
using System.Collections.Generic;

public partial class TestCrates : Node2D
{
    private const string CrateDir = "res://assets/isometric/objects/crates/sheets/";
    private const int SpriteW = 64;
    private const int SpriteH = 64;

    private List<string> _sheetNames = new();
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

        // Scan for crate sheet PNGs
        var diskDir = ProjectSettings.GlobalizePath(CrateDir);
        if (DirAccess.DirExistsAbsolute(diskDir))
        {
            var dir = DirAccess.Open(diskDir);
            dir.ListDirBegin();
            string file;
            while ((file = dir.GetNext()) != "")
            {
                if (file.EndsWith(".png") && !file.StartsWith("."))
                    _sheetNames.Add(file);
            }
            dir.ListDirEnd();
            _sheetNames.Sort();
        }

        GD.Print($"[CRATES] Found {_sheetNames.Count} crate sheets");

        // UI
        var ui = new CanvasLayer();
        AddChild(ui);

        var helpPanel = TestHelper.CreatePanel("SBS CRATES", new Vector2(12, 12), new Vector2(340, 160));
        helpPanel.Visible = true;
        helpPanel.GetNode<Label>("Content").Text =
            "Left/Right: cycle crate sheet\n" +
            "Arrow Up/Down: pan camera\n" +
            "+/-: zoom in/out\n" +
            "F12: screenshot | Esc: quit";
        ui.AddChild(helpPanel);

        _infoLabel = new Label();
        _infoLabel.Position = new Vector2(12, 190);
        _infoLabel.AddThemeColorOverride("font_color", new Color(0.92f, 0.94f, 1.0f));
        _infoLabel.AddThemeFontSizeOverride("font_size", 13);
        ui.AddChild(_infoLabel);

        if (_sheetNames.Count > 0)
            LoadSheet(0);
        else
            _infoLabel.Text = "No crate sheets found!";
    }

    private void LoadSheet(int index)
    {
        _currentIndex = index;
        _displayContainer?.QueueFree();
        _displayContainer = new Node2D();
        AddChild(_displayContainer);

        var fileName = _sheetNames[index];
        var tex = TestHelper.LoadIssPng(CrateDir + fileName);
        if (tex == null) { _infoLabel.Text = $"Failed to load: {fileName}"; return; }

        int sheetW = tex.GetWidth();
        int sheetH = tex.GetHeight();
        int cols = sheetW / SpriteW;
        int rows = sheetH / SpriteH;

        // Show full sprite sheet at the top
        var sheetLabel = new Label();
        sheetLabel.Text = "Full Sheet";
        sheetLabel.Position = new Vector2(20, 0);
        sheetLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.8f, 0.9f));
        sheetLabel.AddThemeFontSizeOverride("font_size", 11);
        _displayContainer.AddChild(sheetLabel);

        var sheetSprite = new Sprite2D();
        sheetSprite.Texture = tex;
        sheetSprite.Centered = false;
        sheetSprite.Position = new Vector2(20, 18);
        sheetSprite.TextureFilter = TextureFilterEnum.Nearest;
        _displayContainer.AddChild(sheetSprite);

        // Extract and display individual 64x64 crates below
        float extractY = 18 + sheetH + 30;

        var extractLabel = new Label();
        extractLabel.Text = $"Individual Crates ({cols * rows} total)";
        extractLabel.Position = new Vector2(20, extractY - 18);
        extractLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.8f, 0.9f));
        extractLabel.AddThemeFontSizeOverride("font_size", 11);
        _displayContainer.AddChild(extractLabel);

        int spriteIndex = 0;
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                var atlas = new AtlasTexture();
                atlas.Atlas = tex;
                atlas.Region = new Rect2(col * SpriteW, row * SpriteH, SpriteW, SpriteH);

                var sprite = new Sprite2D();
                sprite.Texture = atlas;
                sprite.Centered = false;
                sprite.Position = new Vector2(20 + spriteIndex * (SpriteW + 12), extractY);
                sprite.TextureFilter = TextureFilterEnum.Nearest;
                _displayContainer.AddChild(sprite);

                // Index label
                var idxLabel = new Label();
                idxLabel.Text = $"{spriteIndex}";
                idxLabel.Position = sprite.Position + new Vector2(24, -2);
                idxLabel.AddThemeColorOverride("font_color", new Color(0.78f, 0.67f, 0.43f, 0.6f));
                idxLabel.AddThemeFontSizeOverride("font_size", 9);
                _displayContainer.AddChild(idxLabel);

                spriteIndex++;

                // Wrap to next row after 8 per row
                if (spriteIndex % 8 == 0 && spriteIndex < cols * rows)
                {
                    extractY += SpriteH + 20;
                }
            }
        }

        // Clean display name
        var displayName = fileName.Replace(".png", "").Replace("-64x64", "").Replace("crates-", "").Replace("_", " ").Replace("-", " ");

        _infoLabel.Text = $"{displayName}  |  {sheetW}x{sheetH} = {cols}x{rows} grid ({cols * rows} sprites)  [{index + 1}/{_sheetNames.Count}]";
        GD.Print($"[CRATES] {fileName}: {sheetW}x{sheetH}, {cols}x{rows} grid, {cols * rows} sprites");
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (_sheetNames.Count == 0) return;
        if (ev is InputEventKey key && key.Pressed)
        {
            switch (key.Keycode)
            {
                case Key.Right:
                    LoadSheet((_currentIndex + 1) % _sheetNames.Count);
                    break;
                case Key.Left:
                    LoadSheet((_currentIndex - 1 + _sheetNames.Count) % _sheetNames.Count);
                    break;
                case Key.Equal: _camera.Zoom *= 1.25f; break;
                case Key.Minus: _camera.Zoom /= 1.25f; break;
                case Key.F12:
                    var name = _sheetNames[_currentIndex].Replace(".png", "").ToLower();
                    TestHelper.CaptureScreenshot(this, $"crates_{name}");
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
