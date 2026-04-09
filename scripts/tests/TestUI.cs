using Godot;

public partial class TestUI : Node2D
{
    private HpMpOrbs _orbs;
    private int _hp = 100, _maxHp = 100, _mp = 65, _maxMp = 65;
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

        var ui = new CanvasLayer();
        AddChild(ui);

        // HUD panel
        var hudPanel = TestHelper.CreateStyledPanel("A DUNGEON IN THE MIDDLE OF NOWHERE", new Vector2(12, 12), new Vector2(320, 120));
        hudPanel.Visible = true;
        hudPanel.GetNode<Label>("Content").Text = $"HP: {_hp}/{_maxHp} | MP: {_mp}/{_maxMp}\nLVL: 1 | XP: 0 | Floor: 1";
        ui.AddChild(hudPanel);

        // Icon bar
        var iconBar = new HBoxContainer();
        iconBar.Position = new Vector2(12, 150);
        ui.AddChild(iconBar);

        string[] icons = { "Icon-sword.png", "Icon-potion.png", "Icon-potionmana.png", "Icon-coin.png", "Icon-gear.png", "Icon-axe.png", "Icon-sandglass.png" };
        foreach (var iconFile in icons)
        {
            var tex = TestHelper.LoadPng("res://assets/isometric/ui/" + iconFile);
            if (tex != null)
            {
                var rect = new TextureRect();
                rect.Texture = tex;
                rect.CustomMinimumSize = new Vector2(48, 48);
                rect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
                iconBar.AddChild(rect);
            }
        }

        // HP/MP orbs
        _orbs = new HpMpOrbs();
        _orbs.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        ui.AddChild(_orbs);
        _orbs.UpdateValues(_hp, _maxHp, _mp, _maxMp);

        // Controls help
        var helpPanel = TestHelper.CreateStyledPanel("UI TEST CONTROLS", new Vector2(12, 220), new Vector2(320, 120));
        helpPanel.Visible = true;
        helpPanel.GetNode<Label>("Content").Text =
            "1: damage HP (-20)\n" +
            "2: heal HP (+20)\n" +
            "3: spend MP (-15)\n" +
            "4: restore MP (+15)\n" +
            "F12: screenshot | Esc: quit";
        ui.AddChild(helpPanel);

        _infoLabel = new Label();
        _infoLabel.Position = new Vector2(12, 360);
        _infoLabel.AddThemeColorOverride("font_color", new Color(0.925f, 0.941f, 1.0f));
        _infoLabel.AddThemeFontSizeOverride("font_size", 14);
        ui.AddChild(_infoLabel);
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (ev is InputEventKey key && key.Pressed)
        {
            switch (key.Keycode)
            {
                case Key.Key1: _hp = Mathf.Max(0, _hp - 20); UpdateOrbs(); break;
                case Key.Key2: _hp = Mathf.Min(_maxHp, _hp + 20); UpdateOrbs(); break;
                case Key.Key3: _mp = Mathf.Max(0, _mp - 15); UpdateOrbs(); break;
                case Key.Key4: _mp = Mathf.Min(_maxMp, _mp + 15); UpdateOrbs(); break;
                case Key.F12: TestHelper.CaptureScreenshot(this, $"ui_hp{_hp}_mp{_mp}"); break;
                case Key.Escape: GetTree().Quit(); break;
            }
        }
    }

    private void UpdateOrbs()
    {
        _orbs?.UpdateValues(_hp, _maxHp, _mp, _maxMp);
        if (_infoLabel != null)
            _infoLabel.Text = $"HP: {_hp}/{_maxHp} | MP: {_mp}/{_maxMp}";
        GD.Print($"[UI] HP: {_hp}/{_maxHp} | MP: {_mp}/{_maxMp}");
    }
}
