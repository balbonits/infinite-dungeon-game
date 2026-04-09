using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// Visual test for SBS isometric doorway sprite strips.
/// Displays full strip and extracted individual frames with auto-animation.
///
/// Controls:
///   Left/Right  — cycle doorway file
///   Up/Down     — switch flat / thick style
///   Space       — toggle auto-animation
///   +/-         — zoom in/out
///   F12         — screenshot
///   Esc         — quit
/// </summary>
public partial class TestDoors : Node2D
{
    // --- Asset directories ---
    private const string FlatDir = "res://assets/isometric/objects/doorways/flat/";
    private const string ThickDir = "res://assets/isometric/objects/doorways/thick/";

    // Flat frames: 64x96 each, 6 per strip (384x96 total)
    private const int FlatFrameW = 64;
    private const int FlatFrameH = 96;
    // Thick frames: 72x100 each, 6 per strip (432x100 total)
    private const int ThickFrameW = 72;
    private const int ThickFrameH = 100;
    private const int FrameCount = 6;

    // --- Style switching ---
    private static readonly string[] Styles = { "flat", "thick" };
    private int _styleIdx;

    // --- File lists ---
    private List<string> _flatFiles = new();
    private List<string> _thickFiles = new();
    private int _fileIdx;

    // --- Scene nodes ---
    private Camera2D _camera;
    private Label _infoLabel;
    private Node2D _displayRoot;

    // --- Display sprites ---
    private Sprite2D _stripSprite;
    private readonly Sprite2D[] _frameSprites = new Sprite2D[FrameCount];
    private ColorRect _frameHighlight;

    // --- Auto-animation ---
    private bool _autoAnimate = true;
    private float _animTimer;
    private int _animFrame;
    private const float AnimInterval = 0.3f;

    // --- Filename parsing ---
    private static readonly Regex MaterialRx = new(@"(stone|brick|wood)", RegexOptions.IgnoreCase);
    private static readonly Regex DirectionRx = new(@"-(se|sw)\b", RegexOptions.IgnoreCase);

    // =========================================================================
    //  Lifecycle
    // =========================================================================

    public override void _Ready()
    {
        // Dark background
        var bg = new ColorRect();
        bg.Color = new Color(0.12f, 0.12f, 0.15f);
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var bgLayer = new CanvasLayer { Layer = -1 };
        bgLayer.AddChild(bg);
        AddChild(bgLayer);

        _camera = GetNode<Camera2D>("Camera2D");

        // ISS floor grid backdrop
        TestHelper.CreateFloorGrid(this, new Vector2(-192, 80), 8, 6);

        // Scan both doorway directories
        _flatFiles = ScanPngs(FlatDir);
        _thickFiles = ScanPngs(ThickDir);
        GD.Print($"[DOORS] Found {_flatFiles.Count} flat, {_thickFiles.Count} thick doorway sprites");

        if (_flatFiles.Count == 0 && _thickFiles.Count == 0)
        {
            GD.PrintErr("[DOORS] No doorway PNGs found in either directory");
            return;
        }

        // Container for the current doorway display (rebuilt on every file change)
        _displayRoot = new Node2D();
        AddChild(_displayRoot);

        // --- UI overlay ---
        var ui = new CanvasLayer();
        AddChild(ui);

        var helpPanel = TestHelper.CreateStyledPanel(
            "DOORWAYS & PASSAGES", new Vector2(12, 12), new Vector2(340, 180));
        helpPanel.Visible = true;
        helpPanel.GetNode<Label>("Content").Text =
            "Left/Right: cycle doorway file\n" +
            "Up/Down: switch flat/thick style\n" +
            "Space: toggle auto-animation\n" +
            "+/-: zoom in/out\n" +
            "F12: screenshot | Esc: quit";
        ui.AddChild(helpPanel);

        _infoLabel = new Label();
        _infoLabel.Position = new Vector2(12, 210);
        _infoLabel.AddThemeColorOverride("font_color", new Color(0.925f, 0.941f, 1.0f));
        _infoLabel.AddThemeFontSizeOverride("font_size", 14);
        ui.AddChild(_infoLabel);

        ShowDoorway();
    }

    public override void _Process(double delta)
    {
        if (!_autoAnimate) return;

        _animTimer += (float)delta;
        if (_animTimer >= AnimInterval)
        {
            _animTimer = 0f;
            _animFrame = (_animFrame + 1) % FrameCount;
            UpdateHighlight();
        }
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (ev is not InputEventKey key || !key.Pressed) return;
        var files = ActiveFiles;

        switch (key.Keycode)
        {
            case Key.Right:
                if (files.Count > 0)
                {
                    _fileIdx = (_fileIdx + 1) % files.Count;
                    ShowDoorway();
                }
                break;

            case Key.Left:
                if (files.Count > 0)
                {
                    _fileIdx = (_fileIdx - 1 + files.Count) % files.Count;
                    ShowDoorway();
                }
                break;

            case Key.Up:
                _styleIdx = (_styleIdx - 1 + Styles.Length) % Styles.Length;
                _fileIdx = 0;
                ShowDoorway();
                GD.Print($"[DOORS] Style: {Styles[_styleIdx]}");
                break;

            case Key.Down:
                _styleIdx = (_styleIdx + 1) % Styles.Length;
                _fileIdx = 0;
                ShowDoorway();
                GD.Print($"[DOORS] Style: {Styles[_styleIdx]}");
                break;

            case Key.Space:
                _autoAnimate = !_autoAnimate;
                GD.Print($"[DOORS] Auto-animate: {(_autoAnimate ? "ON" : "OFF")}");
                break;

            case Key.Equal:
                _camera.Zoom *= 1.25f;
                break;

            case Key.Minus:
                _camera.Zoom /= 1.25f;
                break;

            case Key.F12:
                var tag = files.Count > 0
                    ? files[_fileIdx].Replace(".png", "")
                    : "empty";
                TestHelper.CaptureScreenshot(this, $"doorway_{tag}");
                break;

            case Key.Escape:
                GetTree().Quit();
                break;
        }
    }

    // =========================================================================
    //  Helpers — file scanning & metadata
    // =========================================================================

    private List<string> ActiveFiles => _styleIdx == 0 ? _flatFiles : _thickFiles;
    private string ActiveDir => _styleIdx == 0 ? FlatDir : ThickDir;
    private int ActiveFrameW => _styleIdx == 0 ? FlatFrameW : ThickFrameW;
    private int ActiveFrameH => _styleIdx == 0 ? FlatFrameH : ThickFrameH;

    /// <summary>Scan a res:// directory for *.png files, sorted alphabetically.</summary>
    private static List<string> ScanPngs(string resDir)
    {
        var diskPath = ProjectSettings.GlobalizePath(resDir);
        var results = new List<string>();
        var dir = DirAccess.Open(diskPath);
        if (dir == null)
        {
            GD.PrintErr($"[DOORS] Cannot open directory: {diskPath}");
            return results;
        }

        dir.ListDirBegin();
        string name = dir.GetNext();
        while (name != "")
        {
            if (!dir.CurrentIsDir() && name.EndsWith(".png"))
                results.Add(name);
            name = dir.GetNext();
        }
        dir.ListDirEnd();
        results.Sort();
        return results;
    }

    private static string ParseMaterial(string fileName)
    {
        var m = MaterialRx.Match(fileName);
        if (!m.Success) return "Unknown";
        return m.Value[0].ToString().ToUpper() + m.Value.Substring(1).ToLower();
    }

    private static string ParseDirection(string fileName)
    {
        var m = DirectionRx.Match(fileName);
        return m.Success ? m.Groups[1].Value.ToUpper() : "??";
    }

    // =========================================================================
    //  Display
    // =========================================================================

    /// <summary>Build the visual display for the currently selected doorway.</summary>
    private void ShowDoorway()
    {
        // Tear down previous display
        foreach (var child in _displayRoot.GetChildren())
            child.QueueFree();

        var files = ActiveFiles;
        if (files.Count == 0)
        {
            _infoLabel.Text = $"No {Styles[_styleIdx]} doorway files found";
            return;
        }

        _fileIdx = Mathf.Clamp(_fileIdx, 0, files.Count - 1);
        string fileName = files[_fileIdx];
        string resPath = ActiveDir + fileName;

        // Load with magenta stripping
        var tex = TestHelper.LoadIssPng(resPath);
        if (tex == null)
        {
            _infoLabel.Text = $"Failed to load: {fileName}";
            return;
        }

        int fw = ActiveFrameW;
        int fh = ActiveFrameH;
        string material = ParseMaterial(fileName);
        string direction = ParseDirection(fileName);

        // ----- Full sprite strip at top -----
        _stripSprite = new Sprite2D
        {
            Texture = tex,
            Centered = false,
            Position = new Vector2(-tex.GetWidth() / 2f, -180),
            Scale = new Vector2(2, 2),
            TextureFilter = TextureFilterEnum.Nearest,
        };
        _displayRoot.AddChild(_stripSprite);

        // Outline around the full strip
        var outline = new ColorRect
        {
            Color = new Color(0.961f, 0.784f, 0.420f, 0.3f),
            Size = new Vector2(tex.GetWidth() * 2 + 4, fh * 2 + 4),
            Position = _stripSprite.Position - new Vector2(2, 2),
            ZIndex = -1,
        };
        _displayRoot.AddChild(outline);

        AddSectionLabel("Full Strip (6 arch variants)",
            new Vector2(_stripSprite.Position.X, _stripSprite.Position.Y - 20));

        // ----- Extracted individual frames -----
        float framesY = _stripSprite.Position.Y + fh * 2 + 40;
        float spacing = fw * 2 + 12;
        float totalW = FrameCount * spacing - 12;
        float startX = -totalW / 2f;

        // Highlight rect that follows the animated frame
        _frameHighlight = new ColorRect
        {
            Color = new Color(0.961f, 0.784f, 0.420f, 0.15f),
            Size = new Vector2(fw * 2 + 8, fh * 2 + 8),
            ZIndex = -1,
        };
        _displayRoot.AddChild(_frameHighlight);

        for (int i = 0; i < FrameCount; i++)
        {
            var atlas = new AtlasTexture
            {
                Atlas = tex,
                Region = new Rect2(i * fw, 0, fw, fh),
            };

            var sprite = new Sprite2D
            {
                Texture = atlas,
                Centered = false,
                Position = new Vector2(startX + i * spacing, framesY),
                Scale = new Vector2(2, 2),
                TextureFilter = TextureFilterEnum.Nearest,
            };
            _displayRoot.AddChild(sprite);
            _frameSprites[i] = sprite;

            // Frame number label beneath each frame
            var numLabel = new Label
            {
                Text = $"{i + 1}",
                HorizontalAlignment = HorizontalAlignment.Center,
                Position = new Vector2(sprite.Position.X + fw - 6, framesY + fh * 2 + 4),
            };
            numLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.8f, 0.9f, 0.7f));
            numLabel.AddThemeFontSizeOverride("font_size", 10);
            _displayRoot.AddChild(numLabel);
        }

        AddSectionLabel("Individual Frames", new Vector2(startX, framesY - 20));

        // Reset animation state
        _animFrame = 0;
        _animTimer = 0f;
        UpdateHighlight();

        // Update info bar
        string style = Styles[_styleIdx];
        _infoLabel.Text =
            $"{fileName}  |  {tex.GetWidth()}x{tex.GetHeight()}  |  {style}  |  " +
            $"{material}  |  {direction}  |  {fw}x{fh}/frame  " +
            $"[{_fileIdx + 1}/{files.Count}]";

        GD.Print($"[DOORS] Loaded: {fileName} ({tex.GetWidth()}x{tex.GetHeight()}, " +
                 $"{style}, {material}, {direction})");
    }

    /// <summary>Move the highlight rect to the active animation frame and dim others.</summary>
    private void UpdateHighlight()
    {
        if (_frameHighlight == null || _animFrame >= FrameCount) return;
        var active = _frameSprites[_animFrame];
        if (active == null) return;

        _frameHighlight.Position = active.Position - new Vector2(4, 4);

        for (int i = 0; i < FrameCount; i++)
        {
            if (_frameSprites[i] != null)
                _frameSprites[i].Modulate = i == _animFrame
                    ? Colors.White
                    : new Color(0.5f, 0.5f, 0.5f);
        }
    }

    /// <summary>Add a small grey section label at the given world position.</summary>
    private void AddSectionLabel(string text, Vector2 pos)
    {
        var label = new Label
        {
            Text = text,
            Position = pos,
        };
        label.AddThemeColorOverride("font_color", new Color(0.75f, 0.8f, 0.9f));
        label.AddThemeFontSizeOverride("font_size", 11);
        _displayRoot.AddChild(label);
    }
}
