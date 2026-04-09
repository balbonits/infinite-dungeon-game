using Godot;
using System.Collections.Generic;

public partial class TestWalls : Node2D
{
    private const int BlockW = 64;
    private const int BlockH = 64;
    private const int TorchW = 32;
    private const int TorchH = 48;

    private static readonly (string file, string name)[] Themes = {
        ("stone_1", "Stone 1"),
        ("stone_2", "Stone 2"),
        ("stone_3", "Stone 3"),
        ("brick_brown", "Brick Brown"),
        ("brick_dark", "Brick Dark"),
        ("brick_gray", "Brick Gray"),
        ("catacombs", "Catacombs"),
        ("church", "Church"),
        ("crystal_wall", "Crystal"),
        ("crystal_wall_colors", "Crystal Colors"),
        ("emerald", "Emerald"),
        ("hell", "Hell"),
        ("lair", "Lair"),
        ("marble_wall", "Marble"),
        ("orc", "Orc"),
        ("sandstone_wall", "Sandstone"),
        ("slime", "Slime"),
        ("snake", "Snake"),
        ("tomb", "Tomb"),
        ("undead", "Undead"),
        ("vault", "Vault"),
        ("volcanic_wall", "Volcanic"),
    };

    private const string WallDir = "res://assets/isometric/tiles/stone-soup/walls/";
    private const string TorchPath = "res://assets/isometric/tiles/stone-soup/torches/torch_anim.png";

    private int _themeIndex;
    private Camera2D _camera;
    private Label _infoLabel;
    private Node2D _blockContainer;

    // Torch animation
    private Sprite2D _torchSprite;
    private Sprite2D _torchFrameDisplay;
    private Label _torchFrameLabel;
    private int _torchFrame;
    private float _torchTimer;
    private bool _torchAnimating = true;

    public override void _Ready()
    {
        var bg = new ColorRect();
        bg.Color = new Color(0.12f, 0.12f, 0.15f);
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var bgLayer = new CanvasLayer { Layer = -1 };
        bgLayer.AddChild(bg);
        AddChild(bgLayer);

        _camera = GetNode<Camera2D>("Camera2D");

        // Torch - animated (right side)
        var torchTex = TestHelper.LoadIssPng(TorchPath);
        if (torchTex != null)
        {
            // Animated torch
            _torchSprite = new Sprite2D();
            _torchSprite.Texture = torchTex;
            _torchSprite.Hframes = 4;
            _torchSprite.Vframes = 2;
            _torchSprite.Position = new Vector2(560, 120);
            _torchSprite.Scale = new Vector2(3, 3);
            _torchSprite.TextureFilter = TextureFilterEnum.Nearest;
            AddChild(_torchSprite);

            // Torch frame strip (all 8 frames shown)
            _torchFrameDisplay = new Sprite2D();
            _torchFrameDisplay.Texture = torchTex;
            _torchFrameDisplay.Centered = false;
            _torchFrameDisplay.Position = new Vector2(460, 200);
            _torchFrameDisplay.Scale = new Vector2(2, 2);
            _torchFrameDisplay.TextureFilter = TextureFilterEnum.Nearest;
            AddChild(_torchFrameDisplay);

            _torchFrameLabel = new Label();
            _torchFrameLabel.Position = new Vector2(540, 70);
            _torchFrameLabel.AddThemeColorOverride("font_color", new Color(0.961f, 0.784f, 0.420f));
            _torchFrameLabel.AddThemeFontSizeOverride("font_size", 12);
            AddChild(_torchFrameLabel);
        }

        // UI
        var ui = new CanvasLayer();
        AddChild(ui);

        var helpPanel = TestHelper.CreateStyledPanel("ISS WALL BLOCKS", new Vector2(12, 12), new Vector2(340, 180));
        helpPanel.Visible = true;
        helpPanel.GetNode<Label>("Content").Text =
            "Left/Right: cycle wall theme\n" +
            "Space: toggle torch animation\n" +
            "Arrow Up/Down: pan camera\n" +
            "+/-: zoom in/out\n" +
            "F12: screenshot | Esc: quit";
        ui.AddChild(helpPanel);

        _infoLabel = new Label();
        _infoLabel.Position = new Vector2(12, 210);
        _infoLabel.AddThemeColorOverride("font_color", new Color(0.925f, 0.941f, 1.0f));
        _infoLabel.AddThemeFontSizeOverride("font_size", 14);
        ui.AddChild(_infoLabel);

        LoadTheme(0);
    }

    private void LoadTheme(int index)
    {
        _themeIndex = index;
        _blockContainer?.QueueFree();
        _blockContainer = new Node2D();
        AddChild(_blockContainer);

        var theme = Themes[index];
        var tex = TestHelper.LoadIssPng(WallDir + theme.file + ".png");
        if (tex == null) { _infoLabel.Text = $"Failed to load: {theme.file}"; return; }

        int cols = tex.GetWidth() / BlockW;
        int totalRows = tex.GetHeight() / BlockH;

        // Extract all block variants (skip top-face overlay rows — every other row)
        var blocks = new List<AtlasTexture>();
        for (int row = 0; row < totalRows; row += 2) // block rows at 0, 2, 4...
        {
            for (int col = 0; col < cols; col++)
            {
                var region = new Rect2(col * BlockW, row * BlockH, BlockW, BlockH);
                // Skip empty cells (check if region has any non-transparent pixels)
                var atlas = new AtlasTexture();
                atlas.Atlas = tex;
                atlas.Region = region;
                blocks.Add(atlas);
            }
        }

        // Also collect top-face overlays
        var topFaces = new List<AtlasTexture>();
        for (int row = 1; row < totalRows; row += 2)
        {
            for (int col = 0; col < cols; col++)
            {
                var atlas = new AtlasTexture();
                atlas.Atlas = tex;
                atlas.Region = new Rect2(col * BlockW, row * BlockH, BlockW, BlockH);
                topFaces.Add(atlas);
            }
        }

        // --- Per-frame display: show all block variants in a grid ---
        float startX = 20;
        float startY = 20;
        int dispCols = cols;
        for (int i = 0; i < blocks.Count; i++)
        {
            int dx = i % dispCols;
            int dy = i / dispCols;

            var sprite = new Sprite2D();
            sprite.Texture = blocks[i];
            sprite.Centered = false;
            sprite.Position = new Vector2(startX + dx * (BlockW + 8), startY + dy * (BlockH + 8));
            sprite.TextureFilter = TextureFilterEnum.Nearest;
            _blockContainer.AddChild(sprite);

            // Frame index label
            var label = new Label();
            label.Text = $"{i}";
            label.Position = sprite.Position + new Vector2(24, -2);
            label.AddThemeColorOverride("font_color", new Color(0.961f, 0.784f, 0.420f, 0.6f));
            label.AddThemeFontSizeOverride("font_size", 9);
            _blockContainer.AddChild(label);
        }

        // --- Assembled wall section below the variants ---
        float wallY = startY + ((blocks.Count / dispCols) + 1) * (BlockH + 8) + 20;
        int wallLen = Mathf.Min(blocks.Count, 6);
        for (int i = 0; i < wallLen; i++)
        {
            // Isometric placement: each block offset to form a wall line
            float isoX = startX + 60 + i * (BlockW / 2);
            float isoY = wallY + i * (BlockH / 4);

            var sprite = new Sprite2D();
            sprite.Texture = blocks[i % blocks.Count];
            sprite.Centered = false;
            sprite.Position = new Vector2(isoX, isoY);
            sprite.TextureFilter = TextureFilterEnum.Nearest;
            sprite.ZIndex = i;
            _blockContainer.AddChild(sprite);

            // Top face overlay on each block
            if (i < topFaces.Count)
            {
                var top = new Sprite2D();
                top.Texture = topFaces[i % topFaces.Count];
                top.Centered = false;
                top.Position = new Vector2(isoX, isoY);
                top.TextureFilter = TextureFilterEnum.Nearest;
                top.ZIndex = i;
                _blockContainer.AddChild(top);
            }
        }

        // Section label
        var wallLabel = new Label();
        wallLabel.Text = "Assembled Wall";
        wallLabel.Position = new Vector2(startX, wallY - 18);
        wallLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.8f, 0.9f));
        wallLabel.AddThemeFontSizeOverride("font_size", 11);
        _blockContainer.AddChild(wallLabel);

        int blockSets = (totalRows + 1) / 2;
        _infoLabel.Text = $"{theme.name}  |  {blocks.Count} blocks ({blockSets} sets × {cols})  |  {topFaces.Count} top faces  [{index + 1}/{Themes.Length}]";
        GD.Print($"[WALLS] {theme.name}: {tex.GetWidth()}x{tex.GetHeight()}, {cols}×{totalRows} cells, {blocks.Count} block variants");
    }

    public override void _Process(double delta)
    {
        // Torch animation
        if (_torchAnimating && _torchSprite != null)
        {
            _torchTimer += (float)delta;
            if (_torchTimer >= 0.1f) // 10 fps
            {
                _torchTimer = 0;
                _torchFrame = (_torchFrame + 1) % 8;
                _torchSprite.Frame = _torchFrame;
                if (_torchFrameLabel != null)
                    _torchFrameLabel.Text = $"Torch: frame {_torchFrame}/7";
            }
        }

        // Camera pan
        var pan = Vector2.Zero;
        if (Input.IsKeyPressed(Key.Up)) pan.Y -= 200 * (float)delta;
        if (Input.IsKeyPressed(Key.Down)) pan.Y += 200 * (float)delta;
        if (pan != Vector2.Zero) _camera.Position += pan;
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (ev is InputEventKey key && key.Pressed)
        {
            switch (key.Keycode)
            {
                case Key.Right:
                    LoadTheme((_themeIndex + 1) % Themes.Length);
                    break;
                case Key.Left:
                    LoadTheme((_themeIndex - 1 + Themes.Length) % Themes.Length);
                    break;
                case Key.Space:
                    _torchAnimating = !_torchAnimating;
                    GD.Print($"[WALLS] Torch animation: {(_torchAnimating ? "ON" : "OFF")}");
                    break;
                case Key.Equal: _camera.Zoom *= 1.25f; break;
                case Key.Minus: _camera.Zoom /= 1.25f; break;
                case Key.F12:
                    TestHelper.CaptureScreenshot(this, $"walls_{Themes[_themeIndex].name.ToLower().Replace(" ", "_")}");
                    break;
                case Key.Escape: GetTree().Quit(); break;
            }
        }
    }
}
