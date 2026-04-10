using Godot;
using System.Collections.Generic;

/// <summary>
/// Sprite animation frame debugger.
/// Displays the full sprite sheet with a frame grid overlay, highlights
/// the current frame, and lets you step through or play animations.
/// Shows each frame individually alongside the sheet for pixel inspection.
///
/// Controls:
///   Up/Down       = switch sprite
///   Left/Right    = step frame (within current animation)
///   1-8           = change direction (row)
///   Space         = play/pause animation
///   [/]           = animation speed (slower/faster)
///   Tab           = cycle animation (Stance/Walk/Attack/Hit/Dead)
///   G             = toggle grid overlay on sheet
///   Z             = toggle zoom (1x / 2x / 4x on frame preview)
///   F12           = screenshot (saves all 8 frames of current anim as strip)
///   Esc           = quit
/// </summary>
public partial class TestSpriteFrames : Node2D
{
    private const int TileW = 64;
    private const int TileH = 32;

    private struct SpriteConfig
    {
        public string Name;
        public string Path;
        public int Hframes;
        public int Vframes;
        public bool IsHero;
    }

    private struct AnimDef
    {
        public string Name;
        public int[] Frames;
        public AnimDef(string name, params int[] frames) { Name = name; Frames = frames; }
    }

    private static readonly AnimDef[] CreatureAnims = {
        new("Stance", 0),
        new("Walk", 1, 2, 3),
        new("Attack", 4, 5),
        new("Hit", 6),
        new("Dead", 7),
    };

    private static readonly AnimDef[] HeroAnims = {
        new("Stance", 0, 1, 2, 3),
        new("Run", 4, 5, 6, 7, 8, 9, 10, 11),
        new("Melee", 12, 13, 14, 15),
        new("Block", 16, 17),
        new("Hit", 18, 19, 20),
        new("Die", 21, 22, 23),
        new("Cast", 24, 25, 26, 27),
        new("Shoot", 28, 29, 30, 31),
    };

    private readonly List<SpriteConfig> _sprites = new()
    {
        new() { Name = "Slime",      Path = "res://assets/isometric/enemies/creatures/slime.png",     Hframes = 8, Vframes = 8, IsHero = false },
        new() { Name = "Goblin",     Path = "res://assets/isometric/enemies/creatures/goblin.png",    Hframes = 8, Vframes = 8, IsHero = false },
        new() { Name = "Skeleton",   Path = "res://assets/isometric/enemies/creatures/skeleton.png",  Hframes = 8, Vframes = 8, IsHero = false },
        new() { Name = "Zombie",     Path = "res://assets/isometric/enemies/creatures/zombie.png",    Hframes = 8, Vframes = 8, IsHero = false },
        new() { Name = "Ogre",       Path = "res://assets/isometric/enemies/creatures/ogre.png",      Hframes = 8, Vframes = 8, IsHero = false },
        new() { Name = "Werewolf",   Path = "res://assets/isometric/enemies/creatures/werewolf.png",  Hframes = 8, Vframes = 8, IsHero = false },
        new() { Name = "Elemental",  Path = "res://assets/isometric/enemies/creatures/elemental.png", Hframes = 8, Vframes = 8, IsHero = false },
        new() { Name = "Magician",   Path = "res://assets/isometric/enemies/creatures/magician.png",  Hframes = 8, Vframes = 8, IsHero = false },
        new() { Name = "Hero",       Path = "res://assets/isometric/characters/hero/male_base.png",   Hframes = 32, Vframes = 8, IsHero = true },
        new() { Name = "Heroine",    Path = "res://assets/isometric/characters/heroine/clothes.png",  Hframes = 32, Vframes = 8, IsHero = true },
    };

    private int _currentIdx;
    private int _currentRow;       // direction 0-7
    private int _currentAnimIdx;   // which animation
    private int _currentFrameInAnim; // frame within animation
    private bool _playing;
    private float _animSpeed = 0.15f;
    private float _animTimer;
    private bool _showGrid = true;
    private int _zoomLevel = 1;    // 1x, 2x, 4x

    private Texture2D _texture;
    private int _frameW;
    private int _frameH;

    // UI
    private Camera2D _camera;
    private Label _infoLabel;
    private Label _controlsLabel;
    private Label _frameDetailLabel;
    private string _statusMsg = "";
    private float _statusTimer;

    public override void _Ready()
    {
        var bg = new ColorRect();
        bg.Color = new Color(0.06f, 0.06f, 0.08f);
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var bgLayer = new CanvasLayer { Layer = -1 };
        bgLayer.AddChild(bg);
        AddChild(bgLayer);

        _camera = new Camera2D();
        _camera.Zoom = new Vector2(1, 1);
        _camera.Position = new Vector2(500, 300);
        AddChild(_camera);

        var ui = new CanvasLayer { Layer = 10 };
        AddChild(ui);

        _infoLabel = new Label();
        _infoLabel.Position = new Vector2(12, 12);
        _infoLabel.AddThemeColorOverride("font_color", new Color("#ecf0ff"));
        _infoLabel.AddThemeFontSizeOverride("font_size", 13);
        ui.AddChild(_infoLabel);

        _frameDetailLabel = new Label();
        _frameDetailLabel.Position = new Vector2(12, 680);
        _frameDetailLabel.AddThemeColorOverride("font_color", new Color("#f5c86b"));
        _frameDetailLabel.AddThemeFontSizeOverride("font_size", 12);
        ui.AddChild(_frameDetailLabel);

        _controlsLabel = new Label();
        _controlsLabel.Position = new Vector2(12, 760);
        _controlsLabel.AddThemeColorOverride("font_color", new Color(1, 1, 1, 0.35f));
        _controlsLabel.AddThemeFontSizeOverride("font_size", 10);
        _controlsLabel.Text =
            "Up/Down: sprite  Left/Right: step frame  1-8: direction  Space: play/pause  " +
            "[/]: speed  Tab: animation  G: grid  Z: zoom  F12: screenshot strip  Esc: quit";
        ui.AddChild(_controlsLabel);

        LoadSprite();
    }

    private AnimDef[] CurrentAnims => _sprites[_currentIdx].IsHero ? HeroAnims : CreatureAnims;
    private AnimDef CurrentAnim => CurrentAnims[_currentAnimIdx];

    private void LoadSprite()
    {
        var config = _sprites[_currentIdx];
        _currentAnimIdx = 0;
        _currentFrameInAnim = 0;
        _currentRow = 0;

        _texture = ResourceLoader.Load<Texture2D>(config.Path);
        if (_texture == null)
            _texture = TestHelper.LoadIssPng(config.Path);

        if (_texture != null)
        {
            _frameW = _texture.GetWidth() / config.Hframes;
            _frameH = _texture.GetHeight() / config.Vframes;
        }
        else
        {
            _frameW = 0;
            _frameH = 0;
        }

        QueueRedraw();
        UpdateInfo();
    }

    public override void _Process(double delta)
    {
        if (_playing && _texture != null)
        {
            _animTimer += (float)delta;
            if (_animTimer >= _animSpeed)
            {
                _animTimer = 0;
                _currentFrameInAnim = (_currentFrameInAnim + 1) % CurrentAnim.Frames.Length;
                QueueRedraw();
                UpdateInfo();
            }
        }

        if (_statusTimer > 0)
        {
            _statusTimer -= (float)delta;
            if (_statusTimer <= 0) { _statusMsg = ""; UpdateInfo(); }
        }
    }

    public override void _Draw()
    {
        if (_texture == null) return;

        var config = _sprites[_currentIdx];

        // ═══ LEFT SIDE: Full sheet (scaled to fit) ═══
        float sheetScale = Mathf.Min(500f / _texture.GetWidth(), 600f / _texture.GetHeight());
        float sheetW = _texture.GetWidth() * sheetScale;
        float sheetH = _texture.GetHeight() * sheetScale;
        Vector2 sheetPos = new(20, 20);

        DrawTextureRect(_texture, new Rect2(sheetPos, new Vector2(sheetW, sheetH)), false);

        // Grid overlay
        if (_showGrid)
        {
            float cellW = sheetW / config.Hframes;
            float cellH = sheetH / config.Vframes;

            // Vertical lines
            for (int i = 0; i <= config.Hframes; i++)
            {
                float x = sheetPos.X + i * cellW;
                DrawLine(new Vector2(x, sheetPos.Y), new Vector2(x, sheetPos.Y + sheetH),
                    new Color(1, 1, 1, 0.15f), 1);
            }
            // Horizontal lines
            for (int j = 0; j <= config.Vframes; j++)
            {
                float y = sheetPos.Y + j * cellH;
                DrawLine(new Vector2(sheetPos.X, y), new Vector2(sheetPos.X + sheetW, y),
                    new Color(1, 1, 1, 0.15f), 1);
            }

            // Highlight current row
            float rowY = sheetPos.Y + _currentRow * cellH;
            DrawRect(new Rect2(sheetPos.X, rowY, sheetW, cellH), new Color(0.3f, 0.6f, 1.0f, 0.1f));

            // Highlight current frame cell
            int absFrame = CurrentAnim.Frames[_currentFrameInAnim];
            float frameX = sheetPos.X + absFrame * cellW;
            DrawRect(new Rect2(frameX, rowY, cellW, cellH), new Color(1.0f, 0.8f, 0.2f, 0.3f));
            DrawRect(new Rect2(frameX, rowY, cellW, cellH), new Color(1.0f, 0.8f, 0.2f, 0.8f), false, 2);
        }

        // ═══ RIGHT SIDE: Current frame zoomed + tile diamond ═══
        float rightX = sheetW + 60;
        float rightY = 40;

        int absF = CurrentAnim.Frames[_currentFrameInAnim];
        int srcX = absF * _frameW;
        int srcY = _currentRow * _frameH;
        var srcRect = new Rect2(srcX, srcY, _frameW, _frameH);

        float zoom = _zoomLevel == 0 ? 1f : _zoomLevel == 1 ? 2f : 4f;
        float dstW = _frameW * zoom;
        float dstH = _frameH * zoom;
        var dstRect = new Rect2(rightX, rightY, dstW, dstH);

        // Draw the single frame
        DrawTextureRectRegion(_texture, dstRect, srcRect);

        // Frame border
        DrawRect(dstRect, new Color(0.961f, 0.784f, 0.420f, 0.5f), false, 1);

        // Tile diamond at bottom-center of frame (where feet should be)
        float diamondCX = rightX + dstW / 2;
        float diamondCY = rightY + dstH - (TileH * zoom / 4); // near bottom

        float dTileW = TileW * zoom / 2;
        float dTileH = TileH * zoom / 2;

        var top = new Vector2(diamondCX, diamondCY - dTileH / 2);
        var right = new Vector2(diamondCX + dTileW / 2, diamondCY);
        var bottom = new Vector2(diamondCX, diamondCY + dTileH / 2);
        var left = new Vector2(diamondCX - dTileW / 2, diamondCY);

        DrawLine(top, right, new Color("#f5c86b"), 2);
        DrawLine(right, bottom, new Color("#f5c86b"), 2);
        DrawLine(bottom, left, new Color("#f5c86b"), 2);
        DrawLine(left, top, new Color("#f5c86b"), 2);

        // Center crosshair
        DrawLine(new Vector2(diamondCX - 4 * zoom, diamondCY), new Vector2(diamondCX + 4 * zoom, diamondCY), new Color(1, 0, 0, 0.5f), 1);
        DrawLine(new Vector2(diamondCX, diamondCY - 4 * zoom), new Vector2(diamondCX, diamondCY + 4 * zoom), new Color(0, 1, 0, 0.5f), 1);

        // ═══ BOTTOM: Animation strip (all frames in current anim, current direction) ═══
        float stripY = Mathf.Max(sheetPos.Y + sheetH + 20, rightY + dstH + 30);
        float stripScale = 1.5f;
        float stripFrameW = _frameW * stripScale;
        float stripFrameH = _frameH * stripScale;
        float stripX = 20;

        for (int i = 0; i < CurrentAnim.Frames.Length; i++)
        {
            int f = CurrentAnim.Frames[i];
            var sSrc = new Rect2(f * _frameW, _currentRow * _frameH, _frameW, _frameH);
            var sDst = new Rect2(stripX + i * (stripFrameW + 4), stripY, stripFrameW, stripFrameH);

            DrawTextureRectRegion(_texture, sDst, sSrc);

            // Highlight current
            if (i == _currentFrameInAnim)
                DrawRect(sDst, new Color(1.0f, 0.8f, 0.2f, 0.8f), false, 2);
            else
                DrawRect(sDst, new Color(1, 1, 1, 0.2f), false, 1);

            // Frame number label
            DrawString(ThemeDB.FallbackFont, new Vector2(sDst.Position.X + 2, sDst.Position.Y + 12),
                $"{f}", HorizontalAlignment.Left, -1, 10, new Color(1, 1, 1, 0.6f));
        }
    }

    private void UpdateInfo()
    {
        var config = _sprites[_currentIdx];
        var anim = CurrentAnim;
        int absFrame = anim.Frames[_currentFrameInAnim];
        string dirName = _currentRow switch
        {
            0 => "S", 1 => "SW", 2 => "W", 3 => "NW",
            4 => "N", 5 => "NE", 6 => "E", 7 => "SE", _ => "?"
        };

        _infoLabel.Text =
            $"Sprite: {config.Name} ({_currentIdx + 1}/{_sprites.Count})\n" +
            $"Sheet: {config.Hframes}x{config.Vframes}  Frame: {_frameW}x{_frameH}px\n" +
            $"Direction: {_currentRow} ({dirName})\n" +
            $"Animation: {anim.Name} ({_currentAnimIdx + 1}/{CurrentAnims.Length})\n" +
            $"Frame: {_currentFrameInAnim + 1}/{anim.Frames.Length} (col {absFrame})\n" +
            $"Playing: {(_playing ? "YES" : "NO")}  Speed: {_animSpeed:F2}s\n" +
            $"Zoom: {(_zoomLevel == 0 ? "1x" : _zoomLevel == 1 ? "2x" : "4x")}" +
            (_statusMsg.Length > 0 ? $"\n\n>> {_statusMsg}" : "");

        _frameDetailLabel.Text =
            $"Current: row={_currentRow} col={absFrame}  " +
            $"src=({absFrame * _frameW}, {_currentRow * _frameH}, {_frameW}, {_frameH})";
    }

    private void SaveAnimStrip()
    {
        if (_texture == null) return;

        var anim = CurrentAnim;
        var config = _sprites[_currentIdx];
        int stripW = _frameW * anim.Frames.Length;
        int stripH = _frameH;

        var srcImg = _texture.GetImage();
        var stripImg = Image.CreateEmpty(stripW, stripH, false, srcImg.GetFormat());

        for (int i = 0; i < anim.Frames.Length; i++)
        {
            int col = anim.Frames[i];
            var srcRect = new Rect2I(col * _frameW, _currentRow * _frameH, _frameW, _frameH);
            stripImg.BlitRect(srcImg, srcRect, new Vector2I(i * _frameW, 0));
        }

        string dir = ProjectSettings.GlobalizePath("res://docs/evidence/sprite-frames/");
        if (!DirAccess.DirExistsAbsolute(dir))
            DirAccess.MakeDirRecursiveAbsolute(dir);

        string timestamp = Time.GetDatetimeStringFromSystem().Replace(":", "").Replace("-", "").Replace("T", "_");
        string name = $"{config.Name}_dir{_currentRow}_{anim.Name}_{anim.Frames.Length}f_{timestamp}";
        string path = dir + name + ".png";

        stripImg.SavePng(path);
        GD.Print($"[SCREENSHOT] Strip saved: {path}");
        _statusMsg = $"Strip saved: {name}.png";
        _statusTimer = 3.0f;
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (ev is not InputEventKey key || !key.Pressed) return;

        switch (key.Keycode)
        {
            case Key.Up:
                _currentIdx = (_currentIdx - 1 + _sprites.Count) % _sprites.Count;
                LoadSprite();
                break;
            case Key.Down:
                _currentIdx = (_currentIdx + 1) % _sprites.Count;
                LoadSprite();
                break;

            case Key.Left:
                _currentFrameInAnim = (_currentFrameInAnim - 1 + CurrentAnim.Frames.Length) % CurrentAnim.Frames.Length;
                QueueRedraw(); UpdateInfo();
                break;
            case Key.Right:
                _currentFrameInAnim = (_currentFrameInAnim + 1) % CurrentAnim.Frames.Length;
                QueueRedraw(); UpdateInfo();
                break;

            case Key.Key1: _currentRow = 0; QueueRedraw(); UpdateInfo(); break;
            case Key.Key2: _currentRow = 1; QueueRedraw(); UpdateInfo(); break;
            case Key.Key3: _currentRow = 2; QueueRedraw(); UpdateInfo(); break;
            case Key.Key4: _currentRow = 3; QueueRedraw(); UpdateInfo(); break;
            case Key.Key5: _currentRow = 4; QueueRedraw(); UpdateInfo(); break;
            case Key.Key6: _currentRow = 5; QueueRedraw(); UpdateInfo(); break;
            case Key.Key7: _currentRow = 6; QueueRedraw(); UpdateInfo(); break;
            case Key.Key8: _currentRow = 7; QueueRedraw(); UpdateInfo(); break;

            case Key.Space:
                _playing = !_playing;
                UpdateInfo();
                break;

            case Key.Tab:
                _currentAnimIdx = (_currentAnimIdx + 1) % CurrentAnims.Length;
                _currentFrameInAnim = 0;
                QueueRedraw(); UpdateInfo();
                break;

            case Key.Bracketleft:
                _animSpeed = Mathf.Min(1.0f, _animSpeed + 0.025f);
                UpdateInfo();
                break;
            case Key.Bracketright:
                _animSpeed = Mathf.Max(0.025f, _animSpeed - 0.025f);
                UpdateInfo();
                break;

            case Key.G:
                _showGrid = !_showGrid;
                QueueRedraw(); UpdateInfo();
                break;

            case Key.Z:
                _zoomLevel = (_zoomLevel + 1) % 3;
                QueueRedraw(); UpdateInfo();
                break;

            case Key.F12:
                SaveAnimStrip();
                UpdateInfo();
                break;

            case Key.Escape:
                GetTree().Quit();
                break;
        }
    }
}
