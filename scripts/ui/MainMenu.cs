using Godot;
using System.Collections.Generic;

/// <summary>
/// Main menu screen. Provides New Game, Load Game, and Exit buttons.
/// Builds its entire UI programmatically in _Ready().
/// </summary>
public partial class MainMenu : Control
{
    private Button _loadBtn;
    private Label _saveInfoLabel;

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

        var vbox = new VBoxContainer();
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.AddThemeConstantOverride("separation", 8);
        center.AddChild(vbox);

        // Title
        var title = new Label();
        title.Text = "A DUNGEON IN THE MIDDLE OF NOWHERE";
        title.AddThemeColorOverride("font_color", new Color("#f5c86b"));
        title.AddThemeFontSizeOverride("font_size", 24);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        // Subtitle
        var subtitle = new Label();
        subtitle.Text = "A tale of infinite descent";
        subtitle.AddThemeColorOverride("font_color", new Color("#b6bfdb"));
        subtitle.AddThemeFontSizeOverride("font_size", 14);
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(subtitle);

        // Spacing
        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(0, 32);
        vbox.AddChild(spacer);

        // Button container (centered)
        var btnBox = new VBoxContainer();
        btnBox.Alignment = BoxContainer.AlignmentMode.Center;
        btnBox.AddThemeConstantOverride("separation", 12);
        btnBox.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        vbox.AddChild(btnBox);

        // New Game button
        var newGameBtn = CreateStyledButton("New Game");
        newGameBtn.Pressed += OnNewGamePressed;
        btnBox.AddChild(newGameBtn);

        // Load Game button
        _loadBtn = CreateStyledButton("Load Game");
        _loadBtn.Pressed += OnLoadGamePressed;
        btnBox.AddChild(_loadBtn);

        // Save info label (shown below Load Game if save exists)
        _saveInfoLabel = new Label();
        _saveInfoLabel.AddThemeColorOverride("font_color", new Color("#b6bfdb"));
        _saveInfoLabel.AddThemeFontSizeOverride("font_size", 12);
        _saveInfoLabel.HorizontalAlignment = HorizontalAlignment.Center;
        btnBox.AddChild(_saveInfoLabel);

        // Settings button
        var settingsBtn = CreateStyledButton("Settings");
        settingsBtn.Pressed += OnSettingsPressed;
        btnBox.AddChild(settingsBtn);

        // Exit Game button
        var exitBtn = CreateStyledButton("Exit Game");
        exitBtn.Pressed += OnExitPressed;
        btnBox.AddChild(exitBtn);

        // Settings panel (shared, hidden by default)
        _settingsPanel = new SettingsPanel();
        AddChild(_settingsPanel);

        // Update load button state based on save existence
        UpdateLoadButton();
    }

    private SettingsPanel _settingsPanel;

    private void UpdateLoadButton()
    {
        bool hasSave = SaveSystem.SlotExists(1);

        if (hasSave)
        {
            _loadBtn.Disabled = false;
            _loadBtn.Modulate = Colors.White;

            Dictionary<string, string> summary = SaveSystem.GetSlotSummary(1);
            string name = summary.ContainsKey("name") ? summary["name"] : "Hero";
            string level = summary.ContainsKey("level") ? summary["level"] : "1";
            string floor = summary.ContainsKey("floor") ? summary["floor"] : "0";
            _saveInfoLabel.Text = $"{name} Lv.{level} - Floor {floor}";
            _saveInfoLabel.Visible = true;
        }
        else
        {
            _loadBtn.Disabled = true;
            _loadBtn.Modulate = new Color(1f, 1f, 1f, 0.4f);
            _saveInfoLabel.Visible = false;
        }
    }

    private void OnNewGamePressed()
    {
        GameState.Reset();
        SceneManager.Instance.GoToCharacterCreate();
    }

    private void OnLoadGamePressed()
    {
        if (!SaveSystem.SlotExists(1))
            return;

        SaveSystem.LoadFromSlot(1);

        if (GameState.Location == GameLocation.Dungeon)
            SceneManager.Instance.GoToDungeon(GameState.DungeonFloor);
        else
            SceneManager.Instance.GoToTown();
    }

    private void OnSettingsPressed()
    {
        _settingsPanel.Show();
    }

    private void OnExitPressed()
    {
        GetTree().Quit();
    }

    private Button CreateStyledButton(string text)
    {
        var btn = new Button();
        btn.Text = text;
        btn.CustomMinimumSize = new Vector2(280, 45);

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

        // Disabled style: same as normal but dimmer
        var disabled = normal.Duplicate() as StyleBoxFlat;
        disabled.BgColor = new Color(0.06f, 0.07f, 0.10f, 0.7f);
        disabled.BorderColor = new Color(0.961f, 0.784f, 0.420f, 0.1f);
        btn.AddThemeStyleboxOverride("disabled", disabled);

        btn.AddThemeColorOverride("font_color", new Color("#ecf0ff"));
        btn.AddThemeColorOverride("font_hover_color", new Color("#f5c86b"));
        btn.AddThemeColorOverride("font_disabled_color", new Color(0.7f, 0.7f, 0.7f, 0.4f));
        btn.AddThemeFontSizeOverride("font_size", 16);

        return btn;
    }
}
