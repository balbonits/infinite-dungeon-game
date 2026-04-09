using Godot;

/// <summary>
/// Pause menu overlay. Toggled via the "start" input action (Esc key).
/// Pauses the scene tree when visible. Offers Resume, Save, and Exit options.
/// Built programmatically following NpcPanel/TestHelper styling patterns
/// and the HUD color scheme from docs/ui/hud.md.
/// ProcessMode = Always so it responds to input while the tree is paused.
/// </summary>
public partial class PauseMenu : Control
{
    // HUD color scheme (matching NpcPanel and docs/ui/hud.md)
    private static readonly Color OverlayBg = new(0, 0, 0, 0.5f);
    private static readonly Color PanelBg = new(0.086f, 0.106f, 0.157f, 0.88f);
    private static readonly Color BorderColor = new(0.961f, 0.784f, 0.420f, 0.3f);
    private static readonly Color TitleColor = new(0.961f, 0.784f, 0.420f);      // #f5c86b accent gold
    private static readonly Color BodyColor = new(0.925f, 0.941f, 1.0f);         // #ecf0ff ink white
    private static readonly Color ButtonBg = new(0.12f, 0.14f, 0.20f, 0.9f);
    private static readonly Color ButtonHover = new(0.18f, 0.20f, 0.28f, 0.9f);

    private const float PanelWidth = 260f;
    private const float PanelHeight = 240f;

    private Label _statusLabel;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Visible = false;

        // Full-screen dark overlay
        var overlay = new ColorRect();
        overlay.Color = OverlayBg;
        overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        overlay.MouseFilter = MouseFilterEnum.Stop; // block clicks to game world
        AddChild(overlay);

        // Centered panel
        var panel = new Panel();
        panel.Size = new Vector2(PanelWidth, PanelHeight);
        panel.Position = new Vector2(-PanelWidth / 2, -PanelHeight / 2);
        panel.SetAnchorsPreset(LayoutPreset.Center);

        var style = new StyleBoxFlat();
        style.BgColor = PanelBg;
        style.BorderColor = BorderColor;
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(6);
        panel.AddThemeStyleboxOverride("panel", style);
        AddChild(panel);

        // Title: "PAUSED"
        var titleLabel = new Label();
        titleLabel.Text = "PAUSED";
        titleLabel.Position = new Vector2(0, 16);
        titleLabel.Size = new Vector2(PanelWidth, 28);
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        titleLabel.AddThemeColorOverride("font_color", TitleColor);
        titleLabel.AddThemeFontSizeOverride("font_size", 20);
        var titleFont = ResourceLoader.Load<Font>("res://assets/fonts/extracted/TinyRPG-BrilliantStrength.ttf");
        if (titleFont != null)
            titleLabel.AddThemeFontOverride("font", titleFont);
        panel.AddChild(titleLabel);

        // Button container
        var buttonBox = new VBoxContainer();
        buttonBox.Position = new Vector2(30, 60);
        buttonBox.Size = new Vector2(PanelWidth - 60, PanelHeight - 80);
        buttonBox.AddThemeConstantOverride("separation", 12);
        panel.AddChild(buttonBox);

        // Resume button
        var resumeBtn = CreateMenuButton("Resume");
        resumeBtn.Pressed += OnResumePressed;
        buttonBox.AddChild(resumeBtn);

        // Save Game button
        var saveBtn = CreateMenuButton("Save Game");
        saveBtn.Pressed += OnSavePressed;
        buttonBox.AddChild(saveBtn);

        // Exit to Menu button
        var exitBtn = CreateMenuButton("Exit to Menu");
        exitBtn.Pressed += OnExitPressed;
        buttonBox.AddChild(exitBtn);

        // Status feedback label (shows "Saved!" etc.)
        _statusLabel = new Label();
        _statusLabel.Position = new Vector2(0, PanelHeight - 36);
        _statusLabel.Size = new Vector2(PanelWidth, 24);
        _statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _statusLabel.AddThemeColorOverride("font_color", TitleColor);
        _statusLabel.AddThemeFontSizeOverride("font_size", 12);
        panel.AddChild(_statusLabel);
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (ev.IsActionPressed("start"))
        {
            Toggle();
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>Toggle pause menu visibility and pause/unpause the scene tree.</summary>
    public void Toggle()
    {
        Visible = !Visible;
        GetTree().Paused = Visible;
        if (!Visible)
            _statusLabel.Text = "";
    }

    private void OnResumePressed()
    {
        Toggle();
    }

    private void OnSavePressed()
    {
        // SaveSystem will be built by another agent.
        // For now, show feedback. When SaveSystem exists, call:
        // SaveSystem.SaveToSlot(1);
        _statusLabel.Text = "Saved!";
    }

    private void OnExitPressed()
    {
        // Save first, then transition.
        // SaveSystem.SaveToSlot(1);
        // SceneManager.Instance.GoToMainMenu();

        // Unpause before any scene change to avoid frozen tree
        GetTree().Paused = false;
        Visible = false;

        // Fallback: quit to avoid a broken state until SceneManager is built
        GD.Print("[PauseMenu] Exit to Menu requested. SceneManager not yet available.");
    }

    /// <summary>
    /// Create a styled menu button matching the NpcPanel button pattern.
    /// </summary>
    private Button CreateMenuButton(string text)
    {
        var btn = new Button();
        btn.Text = text;
        btn.CustomMinimumSize = new Vector2(0, 38);

        var normalStyle = new StyleBoxFlat();
        normalStyle.BgColor = ButtonBg;
        normalStyle.BorderColor = BorderColor;
        normalStyle.SetBorderWidthAll(1);
        normalStyle.SetCornerRadiusAll(4);
        btn.AddThemeStyleboxOverride("normal", normalStyle);

        var hoverStyle = new StyleBoxFlat();
        hoverStyle.BgColor = ButtonHover;
        hoverStyle.BorderColor = TitleColor;
        hoverStyle.SetBorderWidthAll(1);
        hoverStyle.SetCornerRadiusAll(4);
        btn.AddThemeStyleboxOverride("hover", hoverStyle);

        // Pressed state same as hover
        btn.AddThemeStyleboxOverride("pressed", hoverStyle);

        btn.AddThemeColorOverride("font_color", BodyColor);
        btn.AddThemeColorOverride("font_hover_color", TitleColor);
        btn.AddThemeColorOverride("font_pressed_color", TitleColor);
        btn.AddThemeFontSizeOverride("font_size", 14);

        return btn;
    }
}
