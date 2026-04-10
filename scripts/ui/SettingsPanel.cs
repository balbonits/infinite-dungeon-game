using Godot;

/// <summary>
/// Reusable Settings panel. Used from both MainMenu and in-game PauseMenu.
/// Covers: audio, gameplay, display, controls. All settings persist in GameState.Settings.
/// Uses CenterContainer for reliable centering, responsive internal layout.
/// </summary>
public partial class SettingsPanel : Control
{
    private static readonly Color PanelBg = new(0.086f, 0.106f, 0.157f, 0.92f);
    private static readonly Color BorderColor = new(0.961f, 0.784f, 0.420f, 0.3f);
    private static readonly Color TitleColor = new(0.961f, 0.784f, 0.420f);
    private static readonly Color BodyColor = new(0.925f, 0.941f, 1.0f);
    private static readonly Color MutedColor = new(0.714f, 0.749f, 0.859f);
    private static readonly Color SectionColor = new(0.8f, 0.75f, 0.55f);

    private const float WindowW = 550f;
    private const float WindowH = 480f;

    // Controls that read/write GameSettings
    private HSlider _masterVolumeSlider;
    private Label _masterVolumeLabel;
    private CheckButton _damageNumbersToggle;
    private CheckButton _minimapToggle;
    private OptionButton _targetPriorityOption;
    private OptionButton _windowModeOption;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Visible = false;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

        // Dark overlay
        var overlay = new ColorRect();
        overlay.Color = new Color(0, 0, 0, 0.6f);
        overlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        overlay.MouseFilter = MouseFilterEnum.Stop;
        AddChild(overlay);

        // Centered panel
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        center.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(center);

        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(WindowW, WindowH);
        var style = new StyleBoxFlat();
        style.BgColor = PanelBg;
        style.BorderColor = BorderColor;
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(8);
        style.SetContentMarginAll(0);
        panel.AddThemeStyleboxOverride("panel", style);
        center.AddChild(panel);

        // Main layout
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 24);
        margin.AddThemeConstantOverride("margin_right", 24);
        margin.AddThemeConstantOverride("margin_top", 20);
        margin.AddThemeConstantOverride("margin_bottom", 20);
        panel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 12);
        margin.AddChild(vbox);

        // Title
        var title = MakeLabel("SETTINGS", 20, TitleColor);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        // Scroll container for settings
        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        vbox.AddChild(scroll);

        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 10);
        content.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(content);

        // ─── AUDIO ───
        content.AddChild(MakeSection("Audio"));

        var volRow = new HBoxContainer();
        volRow.AddThemeConstantOverride("separation", 12);
        volRow.AddChild(MakeLabel("Master Volume", 13, BodyColor, 140));
        _masterVolumeSlider = new HSlider();
        _masterVolumeSlider.MinValue = 0;
        _masterVolumeSlider.MaxValue = 100;
        _masterVolumeSlider.Step = 1;
        _masterVolumeSlider.Value = GameState.Settings.MasterVolume * 100;
        _masterVolumeSlider.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _masterVolumeSlider.CustomMinimumSize = new Vector2(200, 0);
        _masterVolumeSlider.ValueChanged += OnVolumeChanged;
        volRow.AddChild(_masterVolumeSlider);
        _masterVolumeLabel = MakeLabel($"{(int)(GameState.Settings.MasterVolume * 100)}%", 13, MutedColor, 45);
        volRow.AddChild(_masterVolumeLabel);
        content.AddChild(volRow);

        content.AddChild(MakeLabel("  Music & SFX share master volume (individual controls coming soon)", 10, MutedColor));

        // ─── GAMEPLAY ───
        content.AddChild(MakeSection("Gameplay"));

        // Target priority
        var targetRow = new HBoxContainer();
        targetRow.AddThemeConstantOverride("separation", 12);
        targetRow.AddChild(MakeLabel("Target Priority", 13, BodyColor, 140));
        _targetPriorityOption = new OptionButton();
        foreach (TargetPriority tp in System.Enum.GetValues<TargetPriority>())
            _targetPriorityOption.AddItem(tp.ToString());
        _targetPriorityOption.Selected = (int)GameState.Settings.TargetMode;
        _targetPriorityOption.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _targetPriorityOption.AddThemeFontSizeOverride("font_size", 12);
        _targetPriorityOption.ItemSelected += OnTargetPriorityChanged;
        targetRow.AddChild(_targetPriorityOption);
        content.AddChild(targetRow);

        // Damage numbers
        _damageNumbersToggle = MakeToggle("Show Damage Numbers", GameState.Settings.ShowDamageNumbers);
        _damageNumbersToggle.Toggled += (on) => GameState.Settings.ShowDamageNumbers = on;
        content.AddChild(_damageNumbersToggle);

        // Minimap
        _minimapToggle = MakeToggle("Show Minimap", GameState.Settings.ShowMinimap);
        _minimapToggle.Toggled += (on) => GameState.Settings.ShowMinimap = on;
        content.AddChild(_minimapToggle);

        // ─── DISPLAY ───
        content.AddChild(MakeSection("Display"));

        var winRow = new HBoxContainer();
        winRow.AddThemeConstantOverride("separation", 12);
        winRow.AddChild(MakeLabel("Window Mode", 13, BodyColor, 140));
        _windowModeOption = new OptionButton();
        _windowModeOption.AddItem("Windowed");
        _windowModeOption.AddItem("Borderless Fullscreen");
        _windowModeOption.AddItem("Fullscreen");
        _windowModeOption.Selected = (int)DisplayServer.WindowGetMode();
        _windowModeOption.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _windowModeOption.AddThemeFontSizeOverride("font_size", 12);
        _windowModeOption.ItemSelected += OnWindowModeChanged;
        winRow.AddChild(_windowModeOption);
        content.AddChild(winRow);

        content.AddChild(MakeLabel("  Viewport: 1920x1080 (scales to window size)", 10, MutedColor));

        // ─── CONTROLS ───
        content.AddChild(MakeSection("Controls"));
        content.AddChild(MakeLabel("  Arrow keys: Move", 12, BodyColor));
        content.AddChild(MakeLabel("  S: Attack / Interact    D: Cancel / Close", 12, BodyColor));
        content.AddChild(MakeLabel("  M: Map overlay    Esc: Game menu", 12, BodyColor));
        content.AddChild(MakeLabel("  Q/E: Target cycle (tap) / Shortcuts (hold)", 12, BodyColor));
        content.AddChild(MakeLabel("  Key rebinding coming soon", 10, MutedColor));

        // ─── BUTTONS ───
        var btnRow = new HBoxContainer();
        btnRow.AddThemeConstantOverride("separation", 12);
        btnRow.Alignment = BoxContainer.AlignmentMode.Center;

        var closeBtn = MakeButton("Close");
        closeBtn.Pressed += () => Hide();
        btnRow.AddChild(closeBtn);

        var resetBtn = MakeButton("Reset Defaults");
        resetBtn.Pressed += OnResetDefaults;
        btnRow.AddChild(resetBtn);

        vbox.AddChild(btnRow);
    }

    public new void Show()
    {
        // Sync UI to current settings
        _masterVolumeSlider.SetValueNoSignal(GameState.Settings.MasterVolume * 100);
        _masterVolumeLabel.Text = $"{(int)(GameState.Settings.MasterVolume * 100)}%";
        _damageNumbersToggle.SetPressedNoSignal(GameState.Settings.ShowDamageNumbers);
        _minimapToggle.SetPressedNoSignal(GameState.Settings.ShowMinimap);
        _targetPriorityOption.Selected = (int)GameState.Settings.TargetMode;
        Visible = true;
    }

    public new void Hide()
    {
        Visible = false;
    }

    private void OnVolumeChanged(double value)
    {
        GameState.Settings.MasterVolume = (float)(value / 100.0);
        _masterVolumeLabel.Text = $"{(int)value}%";
        // When audio is implemented: AudioServer.SetBusVolumeDb(0, Mathf.LinearToDb((float)(value / 100.0)));
    }

    private void OnTargetPriorityChanged(long idx)
    {
        GameState.Settings.TargetMode = (TargetPriority)(int)idx;
    }

    private void OnWindowModeChanged(long idx)
    {
        var mode = (DisplayServer.WindowMode)(int)idx;
        DisplayServer.WindowSetMode(mode);
    }

    private void OnResetDefaults()
    {
        GameState.Settings = new GameSettings();
        // Re-sync UI
        Show();
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (Visible && ev is InputEventKey key && key.Pressed && key.Keycode == Key.Escape)
        {
            Hide();
            GetViewport().SetInputAsHandled();
        }
    }

    // ─── Helpers ───

    private static Label MakeLabel(string text, int size, Color color, float minW = 0)
    {
        var lbl = new Label();
        lbl.Text = text;
        lbl.AddThemeFontSizeOverride("font_size", size);
        lbl.AddThemeColorOverride("font_color", color);
        if (minW > 0) lbl.CustomMinimumSize = new Vector2(minW, 0);
        return lbl;
    }

    private static Label MakeSection(string text)
    {
        var lbl = new Label();
        lbl.Text = $"── {text} ──";
        lbl.AddThemeFontSizeOverride("font_size", 14);
        lbl.AddThemeColorOverride("font_color", SectionColor);
        return lbl;
    }

    private static CheckButton MakeToggle(string text, bool initialValue)
    {
        var toggle = new CheckButton();
        toggle.Text = $"  {text}";
        toggle.ButtonPressed = initialValue;
        toggle.AddThemeFontSizeOverride("font_size", 13);
        toggle.AddThemeColorOverride("font_color", BodyColor);
        return toggle;
    }

    private static Button MakeButton(string text)
    {
        var btn = new Button();
        btn.Text = text;
        btn.CustomMinimumSize = new Vector2(150, 38);

        var s = new StyleBoxFlat();
        s.BgColor = new Color(0.12f, 0.14f, 0.20f, 0.9f);
        s.BorderColor = new Color(0.961f, 0.784f, 0.420f, 0.3f);
        s.SetBorderWidthAll(1);
        s.SetCornerRadiusAll(5);
        s.SetContentMarginAll(6);
        btn.AddThemeStyleboxOverride("normal", s);

        var h = s.Duplicate() as StyleBoxFlat;
        h.BorderColor = new Color(0.961f, 0.784f, 0.420f, 0.7f);
        h.BgColor = new Color(0.18f, 0.20f, 0.28f, 0.9f);
        btn.AddThemeStyleboxOverride("hover", h);
        btn.AddThemeStyleboxOverride("pressed", h);

        btn.AddThemeColorOverride("font_color", new Color("#ecf0ff"));
        btn.AddThemeColorOverride("font_hover_color", new Color("#f5c86b"));
        btn.AddThemeFontSizeOverride("font_size", 14);
        return btn;
    }
}
