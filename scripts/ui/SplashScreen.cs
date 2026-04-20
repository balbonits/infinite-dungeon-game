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
    private VBoxContainer _btnBox = null!;

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
        _btnBox = new VBoxContainer();
        _btnBox.AddThemeConstantOverride("separation", 10);
        vbox.AddChild(_btnBox);
        var btnBox = _btnBox;

        // Continue button (opens Load Game screen). Disabled when no saves exist.
        var continueBtn = new Button();
        continueBtn.Text = "Continue";
        continueBtn.CustomMinimumSize = new Vector2(300, 44);
        continueBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        continueBtn.FocusMode = FocusModeEnum.All;
        UiTheme.StyleButton(continueBtn, UiTheme.FontSizes.Button);
        bool anySave = SaveManager.Instance?.AnySaveExists() == true;
        continueBtn.Disabled = !anySave;
        continueBtn.Connect(BaseButton.SignalName.Pressed,
            Callable.From(() => EmitSignal(SignalName.ContinuePressed)));
        btnBox.AddChild(continueBtn);

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
        controlsBtn.Text = "Tutorial";
        controlsBtn.CustomMinimumSize = new Vector2(300, 44);
        controlsBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        controlsBtn.FocusMode = FocusModeEnum.All;
        UiTheme.StyleSecondaryButton(controlsBtn, UiTheme.FontSizes.Button);
        controlsBtn.Connect(BaseButton.SignalName.Pressed,
            Callable.From(() => TutorialPanel.Open(this)));
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

        // Control hints
        var hints = UiTheme.CreateHintBar(
            ("Up/Down", "Navigate"),
            (Constants.InputActions.ActionCross, "Select"),
            ("Enter", "Select"));
        vbox.AddChild(hints);

        // Auto-focus first button after short delay. Guard the callback:
        // Godot tests queue-free + reload scene between runs, so by the time
        // the 0.3s timer fires, this SplashScreen may already have been
        // disposed. Without the guard, the callback raises
        // ObjectDisposedException, which CI UI-tests surfaced as
        // "Timeout waiting for: SplashScreen to appear".
        //
        // Preserve pre-existing focus: if anything inside _btnBox already has
        // focus (e.g., a test called newGameBtn.GrabFocus() within the 0.3s
        // window), don't clobber it. Without this, tests that grab New Game
        // after a prior test populated save slots were racing the timer —
        // timer would re-focus the first enabled button (Continue) and the
        // subsequent PressEnter would open the Load Game screen instead of
        // Class Select, cascading into "Timeout waiting for: ClassSelect to
        // appear" across every suite that reaches Town.
        var timer = GetTree().CreateTimer(0.3);
        timer.Connect(SceneTreeTimer.SignalName.Timeout, Callable.From(() =>
        {
            if (!IsInstanceValid(this) || !IsInsideTree())
                return;
            _ready = true;
            var focused = GetViewport()?.GuiGetFocusOwner();
            if (focused != null && _btnBox.IsAncestorOf(focused))
                return;
            UiTheme.FocusFirstButton(btnBox);
        }));
    }

    private void OpenSettings()
    {
        SettingsPanel.Open(this);
    }

    /// <summary>
    /// Re-grabs focus on the first enabled button. Call after the splash is
    /// un-hidden (e.g., returning from the Load Game screen) — keyboard focus
    /// is otherwise orphaned on a now-freed control, leaving nav dead and
    /// New Game unreachable via Enter/S.
    /// </summary>
    public void FocusFirstButton()
    {
        UiTheme.FocusFirstButton(_btnBox);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_ready || !Visible)
            return;

        // Block input when any child window is open (Settings, Controls, etc.)
        if (WindowStack.HasModal)
        {
            if (@event is InputEventKey k && k.Pressed)
                GetViewport().SetInputAsHandled();
            return;
        }

        if (KeyboardNav.HandleConfirm(@event, GetViewport()))
            GetViewport().SetInputAsHandled();
    }
}
