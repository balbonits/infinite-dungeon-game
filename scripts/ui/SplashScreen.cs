using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Title screen. Shows game title, New Game and Continue buttons.
/// Flow: Splash → New Game → Class Select → Town
///       Splash → Continue → Load save → Town
/// </summary>
public partial class SplashScreen : Control
{
    [Signal] public delegate void NewGamePressedEventHandler();
    [Signal] public delegate void ContinuePressedEventHandler();

    private bool _ready;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        var bg = new ColorRect();
        bg.Color = UiTheme.Colors.BgDark;
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 16);
        center.AddChild(vbox);

        // Game title
        var title = new Label();
        title.Text = Strings.Ui.GameTitle;
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, 32);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        // Subtitle
        var subtitle = new Label();
        subtitle.Text = Strings.Splash.Subtitle;
        UiTheme.StyleLabel(subtitle, UiTheme.Colors.Muted, UiTheme.FontSizes.Heading);
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(subtitle);

        // Spacer
        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(0, 30);
        vbox.AddChild(spacer);

        // Buttons
        var btnBox = new VBoxContainer();
        btnBox.AddThemeConstantOverride("separation", 10);
        vbox.AddChild(btnBox);

        // Character card (only if save exists)
        if (SaveManager.Instance != null && SaveManager.Instance.HasSave())
        {
            var saveData = SaveManager.Instance.PeekSave();
            if (saveData != null)
            {
                var card = BuildCharacterCard(saveData);
                card.Connect(BaseButton.SignalName.Pressed,
                    Callable.From(() => EmitSignal(SignalName.ContinuePressed)));
                btnBox.AddChild(card);
            }
        }

        // New Game button
        var newGameBtn = new Button();
        newGameBtn.Text = Strings.Splash.NewGame;
        newGameBtn.CustomMinimumSize = new Vector2(300, 44);
        newGameBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        newGameBtn.FocusMode = FocusModeEnum.All;
        newGameBtn.Connect(BaseButton.SignalName.Pressed,
            Callable.From(() => EmitSignal(SignalName.NewGamePressed)));
        btnBox.AddChild(newGameBtn);

        // Controls button
        var controlsBtn = new Button();
        controlsBtn.Text = "Controls";
        controlsBtn.CustomMinimumSize = new Vector2(300, 44);
        controlsBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        controlsBtn.FocusMode = FocusModeEnum.All;
        UiTheme.StyleSecondaryButton(controlsBtn, UiTheme.FontSizes.Button);
        controlsBtn.Connect(BaseButton.SignalName.Pressed,
            Callable.From(() => ControlsHelp.Open(this)));
        btnBox.AddChild(controlsBtn);

        // Settings button
        var settingsBtn = new Button();
        settingsBtn.Text = "Settings";
        settingsBtn.CustomMinimumSize = new Vector2(300, 44);
        settingsBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        settingsBtn.FocusMode = FocusModeEnum.All;
        UiTheme.StyleSecondaryButton(settingsBtn, UiTheme.FontSizes.Button);
        settingsBtn.Connect(BaseButton.SignalName.Pressed,
            Callable.From(OpenSettings));
        btnBox.AddChild(settingsBtn);

        // Exit Game button
        var exitBtn = new Button();
        exitBtn.Text = "Exit Game";
        exitBtn.CustomMinimumSize = new Vector2(300, 44);
        exitBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        exitBtn.FocusMode = FocusModeEnum.All;
        UiTheme.StyleDangerButton(exitBtn, UiTheme.FontSizes.Button);
        exitBtn.Connect(BaseButton.SignalName.Pressed,
            Callable.From(() => GetTree().Quit()));
        btnBox.AddChild(exitBtn);

        // Auto-focus first button after short delay
        var timer = GetTree().CreateTimer(0.3);
        timer.Connect(SceneTreeTimer.SignalName.Timeout, Callable.From(() =>
        {
            _ready = true;
            UiTheme.FocusFirstButton(btnBox);
        }));
    }

    private Button BuildCharacterCard(SaveData save)
    {
        var card = new Button();
        card.CustomMinimumSize = new Vector2(340, 0);
        card.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        card.FocusMode = FocusModeEnum.All;

        // Style: dark panel look, not a regular button
        var normalStyle = UiTheme.CreateSlotStyle(new Color(0.10f, 0.12f, 0.18f, 0.9f), false);
        normalStyle.ContentMarginLeft = 12;
        normalStyle.ContentMarginRight = 12;
        normalStyle.ContentMarginTop = 10;
        normalStyle.ContentMarginBottom = 10;
        var focusStyle = UiTheme.CreateSlotStyle(new Color(0.12f, 0.14f, 0.22f, 0.95f), true);
        focusStyle.ContentMarginLeft = 12;
        focusStyle.ContentMarginRight = 12;
        focusStyle.ContentMarginTop = 10;
        focusStyle.ContentMarginBottom = 10;

        card.AddThemeStyleboxOverride("normal", normalStyle);
        card.AddThemeStyleboxOverride("hover", focusStyle);
        card.AddThemeStyleboxOverride("focus", focusStyle);
        card.AddThemeStyleboxOverride("pressed", focusStyle);

        // Build card content as a formatted text block
        string className = save.SelectedClass.ToString();
        int xpToNext = Constants.Leveling.GetXpToLevel(save.Level);
        float xpPct = xpToNext > 0 ? (float)save.Xp / xpToNext * 100 : 0;

        string cardText =
            $"{className}  Lv.{save.Level}\n" +
            $"HP: {save.Hp}/{save.MaxHp}  MP: {save.Mana}/{save.MaxMana}\n" +
            $"STR: {save.Str}  DEX: {save.Dex}  STA: {save.Sta}  INT: {save.Int}\n" +
            $"Floor: {save.FloorNumber}  Deepest: {save.DeepestFloor}  Gold: {save.Gold}\n" +
            $"XP: {xpPct:F0}%  |  {save.SaveDate}";

        card.Text = cardText;
        card.AddThemeColorOverride("font_color", UiTheme.Colors.Ink);
        card.AddThemeColorOverride("font_hover_color", UiTheme.Colors.Ink);
        card.AddThemeColorOverride("font_focus_color", UiTheme.Colors.Ink);
        card.AddThemeFontSizeOverride("font_size", UiTheme.FontSizes.Small);
        card.Alignment = HorizontalAlignment.Left;

        return card;
    }

    private void OpenSettings()
    {
        SettingsPanel.Open(this);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_ready || !Visible)
            return;

        // Child panels handle their own input when open
        if (SettingsPanel.ActiveInstance?.IsOpen == true) return;
        if (ControlsHelp.ActiveInstance?.IsOpen == true) return;

        if (KeyboardNav.HandleInput(@event, this))
            GetViewport().SetInputAsHandled();
    }
}
