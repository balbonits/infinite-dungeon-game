using System.Collections.Generic;
using Godot;

public partial class TestHero : Node2D
{
    private static readonly string[] AnimNames = { "Stance", "Run", "Melee", "Block", "Hit+Die", "Cast", "Shoot" };
    private static readonly int[][] AnimFrameRanges = {
        new[] { 0, 3 },    // stance
        new[] { 4, 11 },   // run
        new[] { 12, 15 },  // melee
        new[] { 16, 17 },  // block
        new[] { 18, 23 },  // hit+die
        new[] { 24, 27 },  // cast
        new[] { 28, 31 },  // shoot
    };

    private static readonly string[] LayerNames = { "clothes", "leather_armor", "steel_armor", "longsword", "shield", "male_head1" };
    private static readonly string[] HeroineLayerNames = { "clothes", "leather_armor", "steel_armor", "longsword", "shield", "head_long" };

    private readonly List<Sprite2D> _layers = new();
    private readonly List<bool> _layerVisible = new() { true, false, false, false, false, true };
    private Label _animLabel;
    private Label _dirLabel;
    private Label _layerLabel;
    private int _direction;
    private int _animIndex = 1; // start on Run
    private int _frameInAnim;
    private float _animTimer;
    private bool _autoAnimate = true;
    private bool _isHeroine;

    public override void _Ready()
    {
        // Dark background
        var bg = new ColorRect();
        bg.Color = new Color(0.12f, 0.12f, 0.15f);
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var bgLayer = new CanvasLayer { Layer = -1 };
        bgLayer.AddChild(bg);
        AddChild(bgLayer);

        LoadLayers();

        // UI
        var ui = new CanvasLayer();
        AddChild(ui);

        var helpPanel = TestHelper.CreateStyledPanel("HERO EQUIPMENT VIEWER", new Vector2(12, 12), new Vector2(320, 220));
        helpPanel.Visible = true;
        helpPanel.GetNode<Label>("Content").Text =
            "1-6: toggle equipment layers\n" +
            "Left/Right: change animation\n" +
            "Up/Down: change direction (0-7)\n" +
            "Space: toggle auto-animate\n" +
            "Tab: switch hero/heroine\n" +
            "F12: screenshot | Esc: quit";
        ui.AddChild(helpPanel);

        _animLabel = new Label();
        _animLabel.Position = new Vector2(880, 600);
        _animLabel.AddThemeColorOverride("font_color", new Color(0.961f, 0.784f, 0.420f));
        _animLabel.AddThemeFontSizeOverride("font_size", 20);
        ui.AddChild(_animLabel);

        _dirLabel = new Label();
        _dirLabel.Position = new Vector2(860, 640);
        _dirLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.8f, 0.9f));
        _dirLabel.AddThemeFontSizeOverride("font_size", 16);
        ui.AddChild(_dirLabel);

        _layerLabel = new Label();
        _layerLabel.Position = new Vector2(1400, 12);
        _layerLabel.AddThemeColorOverride("font_color", new Color(0.925f, 0.941f, 1.0f));
        _layerLabel.AddThemeFontSizeOverride("font_size", 14);
        ui.AddChild(_layerLabel);

        UpdateLabels();
        UpdateFrame();
    }

    private void LoadLayers()
    {
        foreach (var s in _layers) s.QueueFree();
        _layers.Clear();

        string[] names = _isHeroine ? HeroineLayerNames : LayerNames;
        string folder = _isHeroine ? "heroine" : "hero";

        for (int i = 0; i < names.Length; i++)
        {
            var tex = TestHelper.LoadPng($"res://assets/isometric/characters/{folder}/{names[i]}.png");
            if (tex == null)
            {
                GD.Print($"  WARNING: Could not load {names[i]}.png");
                _layers.Add(null);
                continue;
            }

            var sprite = new Sprite2D();
            sprite.Texture = tex;
            sprite.Hframes = 32;
            sprite.Vframes = 8;
            sprite.TextureFilter = TextureFilterEnum.Nearest;
            sprite.Position = new Vector2(960, 440);
            sprite.Scale = new Vector2(3, 3); // 3x for visibility (128px frames are small)
            sprite.Visible = _layerVisible[i];
            AddChild(sprite);
            _layers.Add(sprite);

            GD.Print($"[HERO] Layer '{names[i]}': {tex.GetWidth()}x{tex.GetHeight()} -> {tex.GetWidth()/32}x{tex.GetHeight()/8} frames");
        }
    }

    public override void _Process(double delta)
    {
        if (_autoAnimate)
        {
            int start = AnimFrameRanges[_animIndex][0];
            int end = AnimFrameRanges[_animIndex][1];
            int len = end - start + 1;

            _animTimer += (float)delta;
            if (_animTimer >= 0.125f)
            {
                _animTimer = 0;
                _frameInAnim = (_frameInAnim + 1) % len;
                UpdateFrame();
            }
        }
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (ev is InputEventKey key && key.Pressed)
        {
            switch (key.Keycode)
            {
                case Key.Key1: case Key.Key2: case Key.Key3:
                case Key.Key4: case Key.Key5: case Key.Key6:
                    int idx = (int)key.Keycode - (int)Key.Key1;
                    if (idx < _layerVisible.Count)
                    {
                        _layerVisible[idx] = !_layerVisible[idx];
                        if (idx < _layers.Count && _layers[idx] != null)
                            _layers[idx].Visible = _layerVisible[idx];
                        UpdateLabels();
                        string[] names = _isHeroine ? HeroineLayerNames : LayerNames;
                        GD.Print($"[HERO] Layer '{names[idx]}': {(_layerVisible[idx] ? "ON" : "OFF")}");
                    }
                    break;
                case Key.Right:
                    _animIndex = (_animIndex + 1) % AnimNames.Length;
                    _frameInAnim = 0;
                    UpdateLabels();
                    UpdateFrame();
                    GD.Print($"[HERO] Animation: {AnimNames[_animIndex]}");
                    break;
                case Key.Left:
                    _animIndex = (_animIndex - 1 + AnimNames.Length) % AnimNames.Length;
                    _frameInAnim = 0;
                    UpdateLabels();
                    UpdateFrame();
                    GD.Print($"[HERO] Animation: {AnimNames[_animIndex]}");
                    break;
                case Key.Down:
                    _direction = (_direction + 1) % 8;
                    UpdateLabels();
                    UpdateFrame();
                    break;
                case Key.Up:
                    _direction = (_direction - 1 + 8) % 8;
                    UpdateLabels();
                    UpdateFrame();
                    break;
                case Key.Space:
                    _autoAnimate = !_autoAnimate;
                    GD.Print($"[HERO] Auto-animate: {_autoAnimate}");
                    break;
                case Key.Tab:
                    _isHeroine = !_isHeroine;
                    GD.Print($"[HERO] Switched to: {(_isHeroine ? "Heroine" : "Hero")}");
                    LoadLayers();
                    UpdateFrame();
                    break;
                case Key.F12:
                    TestHelper.CaptureScreenshot(this, $"hero_{AnimNames[_animIndex].ToLower()}_dir{_direction}");
                    break;
                case Key.Escape:
                    GetTree().Quit();
                    break;
            }
        }
    }

    private void UpdateFrame()
    {
        int start = AnimFrameRanges[_animIndex][0];
        int col = start + _frameInAnim;
        int frame = _direction * 32 + col;
        foreach (var s in _layers)
            if (s != null) s.Frame = frame;
    }

    private void UpdateLabels()
    {
        if (_animLabel != null) _animLabel.Text = $"Animation: {AnimNames[_animIndex]}";
        if (_dirLabel != null) _dirLabel.Text = $"Direction: {_direction} | {(_isHeroine ? "Heroine" : "Hero")}";
        if (_layerLabel != null)
        {
            string[] names = _isHeroine ? HeroineLayerNames : LayerNames;
            var lines = new List<string> { "Equipment Layers:" };
            for (int i = 0; i < names.Length; i++)
                lines.Add($"  [{(i+1)}] {names[i]}: {(_layerVisible[i] ? "ON" : "off")}");
            _layerLabel.Text = string.Join("\n", lines);
        }
    }
}
