using Godot;

public partial class TestCreature : Node2D
{
    [Export] public string CreatureName = "skeleton";

    private static readonly string[] AnimNames = { "Stance", "Walk", "Attack", "Hit", "Dead" };
    private static readonly int[][] AnimFrames = {
        new[] { 0 },          // stance
        new[] { 1, 2, 3, 4 }, // walk
        new[] { 5 },          // attack
        new[] { 6 },          // hit
        new[] { 7 },          // dead
    };

    private Sprite2D _sprite;
    private Label _animLabel;
    private Label _dirLabel;
    private int _direction;
    private int _animIndex;
    private int _frameInAnim;
    private float _animTimer;
    private bool _autoAnimate = true;

    public override void _Ready()
    {
        // Dark background
        var bg = new ColorRect();
        bg.Color = new Color(0.12f, 0.12f, 0.15f);
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var bgLayer = new CanvasLayer { Layer = -1 };
        bgLayer.AddChild(bg);
        AddChild(bgLayer);

        // Load sprite
        var tex = TestHelper.LoadPng($"res://assets/isometric/enemies/creatures/{CreatureName}.png");
        if (tex == null)
        {
            GD.PrintErr($"Could not load creature: {CreatureName}");
            return;
        }

        _sprite = new Sprite2D();
        _sprite.Texture = tex;
        _sprite.Hframes = 8;
        _sprite.Vframes = 8;
        _sprite.TextureFilter = TextureFilterEnum.Nearest;
        _sprite.Position = new Vector2(960, 440);
        _sprite.Scale = new Vector2(2, 2); // 2x for visibility
        AddChild(_sprite);

        GD.Print($"[CREATURE] {CreatureName}: {tex.GetWidth()}x{tex.GetHeight()}, frame={tex.GetWidth()/8}x{tex.GetHeight()/8}");

        // UI
        var ui = new CanvasLayer();
        AddChild(ui);

        var helpPanel = TestHelper.CreateStyledPanel(CreatureName.ToUpper(), new Vector2(12, 12), new Vector2(300, 180));
        helpPanel.Visible = true;
        helpPanel.GetNode<Label>("Content").Text =
            "Left/Right: change animation\n" +
            "Up/Down: change direction (0-7)\n" +
            "Space: toggle auto-animate\n" +
            "F12: screenshot\n" +
            "Esc: quit";
        ui.AddChild(helpPanel);

        _animLabel = new Label();
        _animLabel.Position = new Vector2(900, 600);
        _animLabel.AddThemeColorOverride("font_color", new Color(0.961f, 0.784f, 0.420f));
        _animLabel.AddThemeFontSizeOverride("font_size", 20);
        ui.AddChild(_animLabel);

        _dirLabel = new Label();
        _dirLabel.Position = new Vector2(880, 640);
        _dirLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.8f, 0.9f));
        _dirLabel.AddThemeFontSizeOverride("font_size", 16);
        ui.AddChild(_dirLabel);

        _animIndex = 1; // start on walk
        UpdateLabels();
        UpdateFrame();
    }

    public override void _Process(double delta)
    {
        if (_sprite == null) return;

        if (_autoAnimate && AnimFrames[_animIndex].Length > 1)
        {
            _animTimer += (float)delta;
            if (_animTimer >= 0.125f) // 8fps
            {
                _animTimer = 0;
                _frameInAnim = (_frameInAnim + 1) % AnimFrames[_animIndex].Length;
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
                case Key.Right:
                    _animIndex = (_animIndex + 1) % AnimNames.Length;
                    _frameInAnim = 0;
                    UpdateLabels();
                    UpdateFrame();
                    GD.Print($"[CREATURE] Animation: {AnimNames[_animIndex]} (frames: {string.Join(",", AnimFrames[_animIndex])})");
                    break;
                case Key.Left:
                    _animIndex = (_animIndex - 1 + AnimNames.Length) % AnimNames.Length;
                    _frameInAnim = 0;
                    UpdateLabels();
                    UpdateFrame();
                    GD.Print($"[CREATURE] Animation: {AnimNames[_animIndex]}");
                    break;
                case Key.Down:
                    _direction = (_direction + 1) % 8;
                    UpdateLabels();
                    UpdateFrame();
                    GD.Print($"[CREATURE] Direction: {_direction}");
                    break;
                case Key.Up:
                    _direction = (_direction - 1 + 8) % 8;
                    UpdateLabels();
                    UpdateFrame();
                    GD.Print($"[CREATURE] Direction: {_direction}");
                    break;
                case Key.Space:
                    _autoAnimate = !_autoAnimate;
                    GD.Print($"[CREATURE] Auto-animate: {_autoAnimate}");
                    break;
                case Key.F12:
                    TestHelper.CaptureScreenshot(this, $"{CreatureName}_dir{_direction}_{AnimNames[_animIndex].ToLower()}");
                    break;
                case Key.Escape:
                    GetTree().Quit();
                    break;
            }
        }
    }

    private void UpdateFrame()
    {
        if (_sprite == null) return;
        int col = AnimFrames[_animIndex][_frameInAnim % AnimFrames[_animIndex].Length];
        _sprite.Frame = _direction * 8 + col;
    }

    private void UpdateLabels()
    {
        if (_animLabel != null) _animLabel.Text = $"Animation: {AnimNames[_animIndex]}";
        if (_dirLabel != null) _dirLabel.Text = $"Direction: {_direction}";
    }
}
