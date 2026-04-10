using Godot;
using System.Collections.Generic;

/// <summary>
/// Sprite alignment/debugging tool.
/// Shows a sprite on top of an isometric tile diamond frame so you can
/// adjust offset and scale until the sprite sits correctly on the tile.
///
/// Controls:
///   Up/Down      = switch sprite
///   Left/Right   = switch animation frame
///   1-8          = change direction (row)
///   WASD         = nudge sprite offset (1px per press, hold Shift for 5px)
///   +/-          = adjust scale (0.025 per press)
///   R            = reset offset and scale to defaults
///   Space        = toggle auto-animate
///   Tab          = toggle tile diamond visibility
///   G            = toggle grid (3x3 tile neighborhood)
///   F12          = screenshot
///   Esc          = quit
///
/// The gold diamond outline is the tile footprint (64x32 isometric).
/// The sprite should have its feet at the diamond center and its body
/// filling roughly 60-80% of the diamond width.
/// </summary>
public partial class TestSpriteAlign : Node2D
{
    private const int TileW = 64;
    private const int TileH = 32;

    // Sprite configs (same creature list as TestEntity)
    private struct SpriteConfig
    {
        public string Name;
        public string Path;
        public int Hframes;
        public int Vframes;
        public float DefaultScale;
    }

    private readonly List<SpriteConfig> _sprites = new()
    {
        new() { Name = "Slime",      Path = "res://assets/isometric/enemies/creatures/slime.png",     Hframes = 8, Vframes = 8, DefaultScale = 0.3125f },
        new() { Name = "Goblin",     Path = "res://assets/isometric/enemies/creatures/goblin.png",    Hframes = 8, Vframes = 8, DefaultScale = 0.3125f },
        new() { Name = "Skeleton",   Path = "res://assets/isometric/enemies/creatures/skeleton.png",  Hframes = 8, Vframes = 8, DefaultScale = 0.3125f },
        new() { Name = "Zombie",     Path = "res://assets/isometric/enemies/creatures/zombie.png",    Hframes = 8, Vframes = 8, DefaultScale = 0.3125f },
        new() { Name = "Ogre",       Path = "res://assets/isometric/enemies/creatures/ogre.png",      Hframes = 8, Vframes = 8, DefaultScale = 0.3125f },
        new() { Name = "Werewolf",   Path = "res://assets/isometric/enemies/creatures/werewolf.png",  Hframes = 8, Vframes = 8, DefaultScale = 0.3125f },
        new() { Name = "Elemental",  Path = "res://assets/isometric/enemies/creatures/elemental.png", Hframes = 8, Vframes = 8, DefaultScale = 0.3125f },
        new() { Name = "Magician",   Path = "res://assets/isometric/enemies/creatures/magician.png",  Hframes = 8, Vframes = 8, DefaultScale = 0.3125f },
        new() { Name = "Hero",       Path = "res://assets/isometric/characters/hero/male_base.png",   Hframes = 32, Vframes = 8, DefaultScale = 0.625f },
        new() { Name = "Heroine",    Path = "res://assets/isometric/characters/heroine/clothes.png",  Hframes = 32, Vframes = 8, DefaultScale = 0.625f },
    };

    private int _currentIdx;
    private int _currentFrame;
    private int _currentRow; // direction (0-7)
    private float _scale;
    private Vector2 _offset = Vector2.Zero;
    private bool _autoAnimate;
    private bool _showDiamond = true;
    private bool _showGrid;
    private float _animTimer;

    private Sprite2D _sprite;
    private Camera2D _camera;
    private Label _infoLabel;
    private Label _controlsLabel;
    private string _statusMessage = "";
    private float _statusTimer;

    public override void _Ready()
    {
        // Dark background
        var bg = new ColorRect();
        bg.Color = new Color(0.08f, 0.08f, 0.1f);
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var bgLayer = new CanvasLayer { Layer = -1 };
        bgLayer.AddChild(bg);
        AddChild(bgLayer);

        // Camera
        _camera = new Camera2D();
        _camera.Zoom = new Vector2(4, 4); // zoomed in so you can see pixel detail
        AddChild(_camera);

        // The sprite
        _sprite = new Sprite2D();
        _sprite.TextureFilter = TextureFilterEnum.Nearest;
        AddChild(_sprite);

        // UI
        var ui = new CanvasLayer { Layer = 10 };
        AddChild(ui);

        _infoLabel = new Label();
        _infoLabel.Position = new Vector2(12, 12);
        _infoLabel.AddThemeColorOverride("font_color", new Color("#ecf0ff"));
        _infoLabel.AddThemeFontSizeOverride("font_size", 13);
        ui.AddChild(_infoLabel);

        _controlsLabel = new Label();
        _controlsLabel.Position = new Vector2(12, 160);
        _controlsLabel.AddThemeColorOverride("font_color", new Color(1, 1, 1, 0.4f));
        _controlsLabel.AddThemeFontSizeOverride("font_size", 11);
        _controlsLabel.Text =
            "Up/Down: switch sprite\n" +
            "Left/Right: frame\n" +
            "1-8: direction\n" +
            "WASD: nudge offset (Shift=5px)\n" +
            "+/-: scale\n" +
            "R: reset  Space: auto-anim\n" +
            "Tab: diamond  G: grid\n" +
            "F12: screenshot  Esc: quit";
        ui.AddChild(_controlsLabel);

        LoadSprite();
    }

    private void LoadSprite()
    {
        var config = _sprites[_currentIdx];
        _scale = config.DefaultScale;
        _offset = Vector2.Zero;
        _currentFrame = 0;
        _currentRow = 0;

        var tex = ResourceLoader.Load<Texture2D>(config.Path);
        if (tex == null)
        {
            // Try loading with magenta strip
            tex = TestHelper.LoadIssPng(config.Path);
        }

        if (tex != null)
        {
            _sprite.Texture = tex;
            _sprite.Hframes = config.Hframes;
            _sprite.Vframes = config.Vframes;
            _sprite.Visible = true;
        }
        else
        {
            _sprite.Visible = false;
            GD.PrintErr($"[SPRITE-ALIGN] Could not load: {config.Path}");
        }

        UpdateSpriteTransform();
        UpdateInfo();
    }

    private void UpdateSpriteTransform()
    {
        if (!_sprite.Visible) return;

        var config = _sprites[_currentIdx];
        int frame = _currentRow * config.Hframes + _currentFrame;
        if (frame < config.Hframes * config.Vframes)
            _sprite.Frame = frame;

        _sprite.Scale = new Vector2(_scale, _scale);

        // Position: offset from tile center. The diamond center is (0,0).
        // A positive Y offset moves the sprite DOWN (feet lower).
        _sprite.Position = _offset;
    }

    private void UpdateInfo()
    {
        var config = _sprites[_currentIdx];
        int frameW = 0, frameH = 0;
        if (_sprite.Texture != null)
        {
            frameW = _sprite.Texture.GetWidth() / config.Hframes;
            frameH = _sprite.Texture.GetHeight() / config.Vframes;
        }
        float renderedW = frameW * _scale;
        float renderedH = frameH * _scale;

        _infoLabel.Text =
            $"Sprite: {config.Name} ({_currentIdx + 1}/{_sprites.Count})\n" +
            $"Frame: {_currentFrame}/{config.Hframes - 1}  Dir: {_currentRow}\n" +
            $"Sheet: {config.Hframes}x{config.Vframes}  Frame: {frameW}x{frameH}px\n" +
            $"Scale: {_scale:F4}  Rendered: {renderedW:F0}x{renderedH:F0}px\n" +
            $"Offset: ({_offset.X:F0}, {_offset.Y:F0})\n" +
            $"Tile: {TileW}x{TileH}  Diamond: visible={_showDiamond}\n" +
            $"Auto-anim: {(_autoAnimate ? "ON" : "OFF")}" +
            (_statusMessage.Length > 0 ? $"\n\n>> {_statusMessage}" : "");
    }

    public override void _Process(double delta)
    {
        if (_autoAnimate)
        {
            _animTimer += (float)delta;
            if (_animTimer >= 0.15f)
            {
                _animTimer = 0;
                var config = _sprites[_currentIdx];
                _currentFrame = (_currentFrame + 1) % config.Hframes;
                UpdateSpriteTransform();
                UpdateInfo();
            }
        }

        if (_statusTimer > 0)
        {
            _statusTimer -= (float)delta;
            if (_statusTimer <= 0)
            {
                _statusMessage = "";
                UpdateInfo();
            }
        }
    }

    private void SaveScreenshot(string name)
    {
        var img = GetViewport().GetTexture().GetImage();
        string dir = ProjectSettings.GlobalizePath("res://docs/evidence/sprite-align/");
        if (!DirAccess.DirExistsAbsolute(dir))
            DirAccess.MakeDirRecursiveAbsolute(dir);
        string path = dir + name + ".png";
        img.SavePng(path);
        GD.Print($"[SCREENSHOT] Saved: {path}");
    }

    public override void _Draw()
    {
        if (_showGrid)
        {
            // Draw 3x3 grid of tile diamonds
            for (int gx = -1; gx <= 1; gx++)
            {
                for (int gy = -1; gy <= 1; gy++)
                {
                    float cx = (gx - gy) * TileW / 2f;
                    float cy = (gx + gy) * TileH / 2f;
                    DrawIsoDiamond(cx, cy, gx == 0 && gy == 0
                        ? new Color("#f5c86b")
                        : new Color(0.5f, 0.5f, 0.5f, 0.3f));
                }
            }
        }
        else if (_showDiamond)
        {
            DrawIsoDiamond(0, 0, new Color("#f5c86b"));
        }

        // Crosshair at tile center
        DrawLine(new Vector2(-6, 0), new Vector2(6, 0), new Color(1, 0, 0, 0.6f), 1);
        DrawLine(new Vector2(0, -6), new Vector2(0, 6), new Color(0, 1, 0, 0.6f), 1);
    }

    private void DrawIsoDiamond(float cx, float cy, Color color)
    {
        var top = new Vector2(cx, cy - TileH / 2f);
        var right = new Vector2(cx + TileW / 2f, cy);
        var bottom = new Vector2(cx, cy + TileH / 2f);
        var left = new Vector2(cx - TileW / 2f, cy);

        DrawLine(top, right, color, 1.5f);
        DrawLine(right, bottom, color, 1.5f);
        DrawLine(bottom, left, color, 1.5f);
        DrawLine(left, top, color, 1.5f);
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (ev is not InputEventKey key || !key.Pressed) return;

        bool shift = key.ShiftPressed;
        float nudge = shift ? 5f : 1f;
        var config = _sprites[_currentIdx];

        switch (key.Keycode)
        {
            // Switch sprite
            case Key.Up:
                _currentIdx = (_currentIdx - 1 + _sprites.Count) % _sprites.Count;
                LoadSprite();
                break;
            case Key.Down:
                _currentIdx = (_currentIdx + 1) % _sprites.Count;
                LoadSprite();
                break;

            // Switch frame
            case Key.Left:
                _currentFrame = (_currentFrame - 1 + config.Hframes) % config.Hframes;
                UpdateSpriteTransform(); UpdateInfo();
                break;
            case Key.Right:
                _currentFrame = (_currentFrame + 1) % config.Hframes;
                UpdateSpriteTransform(); UpdateInfo();
                break;

            // Direction (1-8 keys)
            case Key.Key1: _currentRow = 0; UpdateSpriteTransform(); UpdateInfo(); break;
            case Key.Key2: _currentRow = 1; UpdateSpriteTransform(); UpdateInfo(); break;
            case Key.Key3: _currentRow = 2; UpdateSpriteTransform(); UpdateInfo(); break;
            case Key.Key4: _currentRow = 3; UpdateSpriteTransform(); UpdateInfo(); break;
            case Key.Key5: _currentRow = 4; UpdateSpriteTransform(); UpdateInfo(); break;
            case Key.Key6: _currentRow = 5; UpdateSpriteTransform(); UpdateInfo(); break;
            case Key.Key7: _currentRow = 6; UpdateSpriteTransform(); UpdateInfo(); break;
            case Key.Key8: _currentRow = 7; UpdateSpriteTransform(); UpdateInfo(); break;

            // Nudge offset (WASD)
            case Key.W: _offset.Y -= nudge; UpdateSpriteTransform(); UpdateInfo(); QueueRedraw(); break;
            case Key.S: _offset.Y += nudge; UpdateSpriteTransform(); UpdateInfo(); QueueRedraw(); break;
            case Key.A: _offset.X -= nudge; UpdateSpriteTransform(); UpdateInfo(); QueueRedraw(); break;
            case Key.D: _offset.X += nudge; UpdateSpriteTransform(); UpdateInfo(); QueueRedraw(); break;

            // Scale
            case Key.Equal:
                _scale += 0.025f;
                UpdateSpriteTransform(); UpdateInfo();
                break;
            case Key.Minus:
                _scale = Mathf.Max(0.05f, _scale - 0.025f);
                UpdateSpriteTransform(); UpdateInfo();
                break;

            // Reset
            case Key.R:
                _offset = Vector2.Zero;
                _scale = _sprites[_currentIdx].DefaultScale;
                UpdateSpriteTransform(); UpdateInfo(); QueueRedraw();
                break;

            // Toggle auto-animate
            case Key.Space:
                _autoAnimate = !_autoAnimate;
                UpdateInfo();
                break;

            // Toggle diamond
            case Key.Tab:
                _showDiamond = !_showDiamond;
                UpdateInfo(); QueueRedraw();
                break;

            // Toggle grid
            case Key.G:
                _showGrid = !_showGrid;
                UpdateInfo(); QueueRedraw();
                break;

            // Screenshot — saved with sprite name, direction, frame, offset, scale for reference
            case Key.F12:
                var c = _sprites[_currentIdx];
                string timestamp = Time.GetDatetimeStringFromSystem().Replace(":", "").Replace("-", "").Replace("T", "_");
                string shotName = $"{c.Name}_dir{_currentRow}_f{_currentFrame}_s{_scale:F3}_x{_offset.X:F0}y{_offset.Y:F0}_{timestamp}";
                SaveScreenshot(shotName);
                _statusMessage = $"Saved: {shotName}.png";
                _statusTimer = 2.0f;
                UpdateInfo();
                break;

            // Quit
            case Key.Escape:
                GetTree().Quit();
                break;
        }
    }
}
