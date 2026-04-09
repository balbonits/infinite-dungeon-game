using Godot;
using System.Collections.Generic;

public partial class TestButtons : Node2D
{
    private static readonly (string path, string label, int frameW, int frameH)[] ButtonSets = {
        ("res://assets/isometric/ui/buttons/buttons_round_a-24x24.png", "Round A (24px)", 24, 24),
        ("res://assets/isometric/ui/buttons/buttons_round_b-24x24.png", "Round B (24px)", 24, 24),
        ("res://assets/isometric/ui/buttons/buttons_square_a-24x24.png", "Square A (24px)", 24, 24),
        ("res://assets/isometric/ui/buttons/buttons_round_a-16x16.png", "Round A (16px)", 16, 16),
        ("res://assets/isometric/ui/buttons/buttons_round_b-16x16.png", "Round B (16px)", 16, 16),
        ("res://assets/isometric/ui/buttons/buttons_square_a-16x16.png", "Square A (16px)", 16, 16),
    };

    private int _currentSet;
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

        // UI
        var ui = new CanvasLayer();
        AddChild(ui);
        var helpPanel = TestHelper.CreatePanel("UI BUTTONS & CONTROLS", new Vector2(12, 12), new Vector2(340, 160));
        helpPanel.Visible = true;
        helpPanel.GetNode<Label>("Content").Text =
            "Left/Right: cycle button set\n" +
            "Arrow Up/Down: pan camera\n" +
            "+/-: zoom in/out\n" +
            "F12: screenshot | Esc: quit";
        ui.AddChild(helpPanel);

        _infoLabel = new Label();
        _infoLabel.Position = new Vector2(12, 190);
        _infoLabel.AddThemeColorOverride("font_color", new Color(0.92f, 0.94f, 1.0f));
        _infoLabel.AddThemeFontSizeOverride("font_size", 13);
        ui.AddChild(_infoLabel);

        LoadSet(0);
    }

    private void LoadSet(int index)
    {
        _currentSet = index;
        _displayContainer?.QueueFree();
        _displayContainer = new Node2D();
        AddChild(_displayContainer);

        var set = ButtonSets[index];
        var tex = TestHelper.LoadIssPng(set.path);
        if (tex == null) { _infoLabel.Text = $"Failed: {set.label}"; return; }

        int cols = tex.GetWidth() / set.frameW;
        int rows = tex.GetHeight() / set.frameH;
        int total = cols * rows;
        float scale = set.frameW == 16 ? 4f : 3f;
        float spacing = set.frameW * scale + 8;

        // Display each button frame individually
        for (int i = 0; i < total; i++)
        {
            int c = i % cols;
            int r = i / cols;

            var atlas = new AtlasTexture();
            atlas.Atlas = tex;
            atlas.Region = new Rect2(c * set.frameW, r * set.frameH, set.frameW, set.frameH);

            var sprite = new Sprite2D();
            sprite.Texture = atlas;
            sprite.Centered = false;
            sprite.Position = new Vector2(20 + c * spacing, 20 + r * spacing);
            sprite.Scale = new Vector2(scale, scale);
            sprite.TextureFilter = TextureFilterEnum.Nearest;
            _displayContainer.AddChild(sprite);
        }

        // Column labels (states)
        string[] stateLabels = { "Normal", "Hover", "Pressed", "Disabled" };
        for (int c = 0; c < Mathf.Min(cols, stateLabels.Length); c++)
        {
            var label = new Label();
            label.Text = stateLabels[c];
            label.Position = new Vector2(20 + c * spacing, 2);
            label.AddThemeColorOverride("font_color", new Color(0.78f, 0.67f, 0.43f, 0.6f));
            label.AddThemeFontSizeOverride("font_size", 9);
            _displayContainer.AddChild(label);
        }

        _infoLabel.Text = $"{set.label}  |  {cols}×{rows} = {total} buttons  [{index + 1}/{ButtonSets.Length}]";
        GD.Print($"[BUTTONS] {set.label}: {tex.GetWidth()}x{tex.GetHeight()}, {total} frames at {set.frameW}x{set.frameH}");
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (ev is InputEventKey key && key.Pressed)
        {
            switch (key.Keycode)
            {
                case Key.Right: LoadSet((_currentSet + 1) % ButtonSets.Length); break;
                case Key.Left: LoadSet((_currentSet - 1 + ButtonSets.Length) % ButtonSets.Length); break;
                case Key.Equal: _camera.Zoom *= 1.25f; break;
                case Key.Minus: _camera.Zoom /= 1.25f; break;
                case Key.F12: TestHelper.CaptureScreenshot(this, $"buttons_{ButtonSets[_currentSet].label.ToLower().Replace(" ", "_")}"); break;
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
