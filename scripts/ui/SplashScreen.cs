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

        // Continue button (only if save exists)
        if (SaveManager.Instance != null && SaveManager.Instance.HasSave())
        {
            var saveData = SaveManager.Instance.PeekSave();
            string continueText = saveData != null
                ? $"Continue (Lv.{saveData.Level} {saveData.SelectedClass}, Floor {saveData.FloorNumber})"
                : Strings.Splash.Continue;

            var continueBtn = new Button();
            continueBtn.Text = continueText;
            continueBtn.CustomMinimumSize = new Vector2(300, 44);
            continueBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            continueBtn.FocusMode = FocusModeEnum.All;
            continueBtn.Connect(BaseButton.SignalName.Pressed,
                Callable.From(() => EmitSignal(SignalName.ContinuePressed)));
            btnBox.AddChild(continueBtn);
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

    private void OpenSettings()
    {
        var overlay = new ColorRect();
        overlay.Color = new Color(0, 0, 0, 0.7f);
        overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(overlay);

        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        overlay.AddChild(center);

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", UiTheme.CreatePanelStyle(0.95f, true));
        panel.CustomMinimumSize = new Vector2(350, 0);
        center.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 12);
        panel.AddChild(vbox);

        var heading = new Label();
        heading.Text = "Settings";
        UiTheme.StyleLabel(heading, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        heading.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(heading);

        // Combat numbers toggle
        var combatRow = new HBoxContainer();
        combatRow.AddThemeConstantOverride("separation", 12);
        vbox.AddChild(combatRow);
        var combatLabel = new Label();
        combatLabel.Text = "Show Damage Numbers";
        UiTheme.StyleLabel(combatLabel, UiTheme.Colors.Ink, UiTheme.FontSizes.Body);
        combatLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        combatRow.AddChild(combatLabel);
        var combatToggle = new CheckButton();
        combatToggle.ButtonPressed = GameSettings.ShowCombatNumbers;
        combatToggle.FocusMode = FocusModeEnum.All;
        combatToggle.Connect(BaseButton.SignalName.Toggled, Callable.From((bool on) =>
            GameSettings.ShowCombatNumbers = on));
        combatRow.AddChild(combatToggle);

        vbox.AddChild(new HSeparator());

        // Back button
        var backBtn = new Button();
        backBtn.Text = "Back";
        backBtn.CustomMinimumSize = new Vector2(200, 38);
        backBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        backBtn.FocusMode = FocusModeEnum.All;
        UiTheme.StyleSecondaryButton(backBtn, UiTheme.FontSizes.Body);
        backBtn.Connect(BaseButton.SignalName.Pressed,
            Callable.From(() => overlay.QueueFree()));
        vbox.AddChild(backBtn);

        // Focus first interactive element
        combatToggle.CallDeferred(Control.MethodName.GrabFocus);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_ready || !Visible)
            return;

        if (Visible && KeyboardNav.HandleInput(@event, this))
            GetViewport().SetInputAsHandled();
    }
}
