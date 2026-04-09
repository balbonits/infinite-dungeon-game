using Godot;

public partial class TestCombat : Node2D
{
    private Sprite2D _skeleton;
    private int _hitCount;
    private Label _infoLabel;

    public override void _Ready()
    {
        // Dark background
        var bg = new ColorRect();
        bg.Color = new Color(0.12f, 0.12f, 0.15f);
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var bgLayer = new CanvasLayer { Layer = -1 };
        bgLayer.AddChild(bg);
        AddChild(bgLayer);

        // Skeleton
        var tex = TestHelper.LoadPng("res://assets/isometric/enemies/creatures/skeleton.png");
        if (tex != null)
        {
            _skeleton = new Sprite2D();
            _skeleton.Texture = tex;
            _skeleton.Hframes = 8;
            _skeleton.Vframes = 8;
            _skeleton.TextureFilter = TextureFilterEnum.Nearest;
            _skeleton.Position = new Vector2(960, 440);
            _skeleton.Scale = new Vector2(2, 2);
            _skeleton.Frame = 0; // stance
            AddChild(_skeleton);
        }

        // UI
        var ui = new CanvasLayer();
        AddChild(ui);
        var helpPanel = TestHelper.CreateStyledPanel("COMBAT EFFECTS", new Vector2(12, 12), new Vector2(300, 140));
        helpPanel.Visible = true;
        helpPanel.GetNode<Label>("Content").Text =
            "Space: attack skeleton\n" +
            "R: reset skeleton\n" +
            "F12: screenshot | Esc: quit";
        ui.AddChild(helpPanel);

        _infoLabel = new Label();
        _infoLabel.Position = new Vector2(12, 180);
        _infoLabel.AddThemeColorOverride("font_color", new Color(0.925f, 0.941f, 1.0f));
        _infoLabel.AddThemeFontSizeOverride("font_size", 14);
        ui.AddChild(_infoLabel);
        UpdateInfo();
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (ev is InputEventKey key && key.Pressed)
        {
            switch (key.Keycode)
            {
                case Key.Space:
                    Attack();
                    break;
                case Key.R:
                    _hitCount = 0;
                    if (_skeleton != null) { _skeleton.Frame = 0; _skeleton.Modulate = Colors.White; }
                    UpdateInfo();
                    GD.Print("[COMBAT] Reset");
                    break;
                case Key.F12:
                    TestHelper.CaptureScreenshot(this, $"combat_hit{_hitCount}");
                    break;
                case Key.Escape:
                    GetTree().Quit();
                    break;
            }
        }
    }

    private void Attack()
    {
        if (_skeleton == null) return;
        _hitCount++;

        var pos = _skeleton.Position;
        TestHelper.ShowSlashEffect(this, pos);

        int damage = 12 + (int)(1 * 1.5f) + _hitCount * 3;
        var color = _hitCount % 3 == 0 ? new Color(1, 0.9f, 0.3f) : new Color(1, 0.3f, 0.3f);
        TestHelper.ShowFloatingText(this, pos + new Vector2(GD.Randf() * 30 - 15, -20), damage.ToString(), color);

        // Flash
        var tween = CreateTween();
        tween.TweenProperty(_skeleton, "modulate", Colors.Red, 0.06);
        tween.TweenProperty(_skeleton, "modulate", Colors.White, 0.14);

        if (_hitCount >= 5)
        {
            _skeleton.Frame = 7; // dead
            _skeleton.Modulate = new Color(1, 1, 1, 0.5f);
            GD.Print("[COMBAT] Skeleton killed!");
        }
        else
        {
            _skeleton.Frame = 6; // hit
            GetTree().CreateTimer(0.3).Timeout += () => { if (_skeleton != null && _hitCount < 5) _skeleton.Frame = 0; };
        }

        UpdateInfo();
        GD.Print($"[COMBAT] Hit #{_hitCount}, damage: {damage}");
    }

    private void UpdateInfo()
    {
        if (_infoLabel != null)
            _infoLabel.Text = $"Hits: {_hitCount}/5 | {(_hitCount >= 5 ? "DEAD" : "ALIVE")}";
    }
}
