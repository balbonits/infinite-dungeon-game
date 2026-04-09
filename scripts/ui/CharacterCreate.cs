using Godot;

/// <summary>
/// Character creation screen. Name entry + stat preview.
/// Class selection is deferred to a future ticket.
/// Builds its entire UI programmatically in _Ready().
/// </summary>
public partial class CharacterCreate : Control
{
    private LineEdit _nameInput;

    public override void _Ready()
    {
        // Dark background
        var bg = new ColorRect();
        bg.Color = new Color(0.05f, 0.05f, 0.08f);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        // Center container
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        // Panel background
        var panel = new PanelContainer();
        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = new Color(0.086f, 0.106f, 0.157f, 0.88f);
        panelStyle.BorderColor = new Color(0.961f, 0.784f, 0.420f, 0.3f);
        panelStyle.SetBorderWidthAll(1);
        panelStyle.SetCornerRadiusAll(10);
        panelStyle.SetContentMarginAll(32);
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        center.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.AddThemeConstantOverride("separation", 16);
        panel.AddChild(vbox);

        // Title
        var title = new Label();
        title.Text = "CREATE YOUR CHARACTER";
        title.AddThemeColorOverride("font_color", new Color("#f5c86b"));
        title.AddThemeFontSizeOverride("font_size", 22);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        // Name row
        var nameRow = new HBoxContainer();
        nameRow.Alignment = BoxContainer.AlignmentMode.Center;
        nameRow.AddThemeConstantOverride("separation", 12);
        vbox.AddChild(nameRow);

        var nameLabel = new Label();
        nameLabel.Text = "Name:";
        nameLabel.AddThemeColorOverride("font_color", new Color("#ecf0ff"));
        nameLabel.AddThemeFontSizeOverride("font_size", 16);
        nameRow.AddChild(nameLabel);

        _nameInput = new LineEdit();
        _nameInput.Text = "Hero";
        _nameInput.MaxLength = 20;
        _nameInput.CustomMinimumSize = new Vector2(220, 36);
        _nameInput.SelectAllOnFocus = true;

        // Style the LineEdit
        var lineEditStyle = new StyleBoxFlat();
        lineEditStyle.BgColor = new Color(0.05f, 0.06f, 0.10f, 0.9f);
        lineEditStyle.BorderColor = new Color(0.961f, 0.784f, 0.420f, 0.3f);
        lineEditStyle.SetBorderWidthAll(1);
        lineEditStyle.SetCornerRadiusAll(4);
        lineEditStyle.SetContentMarginAll(6);
        _nameInput.AddThemeStyleboxOverride("normal", lineEditStyle);
        _nameInput.AddThemeStyleboxOverride("focus", lineEditStyle.Duplicate() as StyleBoxFlat);

        _nameInput.AddThemeColorOverride("font_color", new Color("#ecf0ff"));
        _nameInput.AddThemeColorOverride("caret_color", new Color("#f5c86b"));
        _nameInput.AddThemeFontSizeOverride("font_size", 16);
        nameRow.AddChild(_nameInput);

        // Stat summary
        var stats = new Label();
        var p = new PlayerState();
        stats.Text = $"HP: {p.MaxHP} | MP: {p.MaxMP} | STR: {p.STR} | DEX: {p.DEX} | INT: {p.INT} | VIT: {p.VIT}";
        stats.AddThemeColorOverride("font_color", new Color("#b6bfdb"));
        stats.AddThemeFontSizeOverride("font_size", 14);
        stats.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(stats);

        // Spacing
        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(0, 12);
        vbox.AddChild(spacer);

        // Button row
        var btnRow = new HBoxContainer();
        btnRow.Alignment = BoxContainer.AlignmentMode.Center;
        btnRow.AddThemeConstantOverride("separation", 16);
        vbox.AddChild(btnRow);

        var backBtn = CreateStyledButton("Back");
        backBtn.Pressed += OnBackPressed;
        btnRow.AddChild(backBtn);

        var beginBtn = CreateStyledButton("Begin Adventure");
        beginBtn.Pressed += OnBeginPressed;
        btnRow.AddChild(beginBtn);
    }

    private void OnBackPressed()
    {
        SceneManager.Instance.GoToMainMenu();
    }

    private void OnBeginPressed()
    {
        string name = _nameInput.Text.Trim();
        if (name.Length == 0)
            name = "Hero";

        GameState.Player.Name = name;
        SceneManager.Instance.GoToTown();
    }

    private Button CreateStyledButton(string text)
    {
        var btn = new Button();
        btn.Text = text;
        btn.CustomMinimumSize = new Vector2(180, 45);

        var normal = new StyleBoxFlat();
        normal.BgColor = new Color(0.086f, 0.106f, 0.157f, 0.9f);
        normal.BorderColor = new Color(0.961f, 0.784f, 0.420f, 0.3f);
        normal.SetBorderWidthAll(1);
        normal.SetCornerRadiusAll(6);
        normal.SetContentMarginAll(8);
        btn.AddThemeStyleboxOverride("normal", normal);

        var hover = normal.Duplicate() as StyleBoxFlat;
        hover.BorderColor = new Color(0.961f, 0.784f, 0.420f, 0.7f);
        btn.AddThemeStyleboxOverride("hover", hover);

        var pressed = normal.Duplicate() as StyleBoxFlat;
        pressed.BgColor = new Color(0.12f, 0.14f, 0.2f, 0.9f);
        btn.AddThemeStyleboxOverride("pressed", pressed);

        btn.AddThemeColorOverride("font_color", new Color("#ecf0ff"));
        btn.AddThemeColorOverride("font_hover_color", new Color("#f5c86b"));
        btn.AddThemeFontSizeOverride("font_size", 16);

        return btn;
    }
}
