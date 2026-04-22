using Godot;

namespace DungeonGame.Ui;

/// <summary>
/// Pre-game splash shown once per boot before <see cref="SplashScreen"/>.
/// Carries the copyright + AI-disclosure legal notice required by
/// SPEC-AI-SAFETY-01. Auto-advances after <see cref="HoldDuration"/> seconds,
/// or immediately on any key / mouse press.
/// </summary>
public partial class LegalSplash : Control
{
    [Signal] public delegate void ContinuePressedEventHandler();

    private const float FadeInDuration = 0.4f;
    private const float HoldDuration = 3.5f;
    // Swallow input for the first frames after _Ready — macOS window focus
    // can deliver a queued keyboard/mouse event from whatever the user did
    // to launch the game (Enter in terminal, click on the Godot icon), which
    // would otherwise dismiss the splash before it's visible.
    private const float InputIgnoreWindow = 0.5f;

    private bool _dismissed;
    private bool _acceptsInput;
    private Control _content = null!;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        MouseFilter = MouseFilterEnum.Stop;

        var bg = new ColorRect { Color = UiTheme.Colors.BgDark };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        bg.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(bg);

        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        center.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(center);

        _content = new VBoxContainer();
        _content.AddThemeConstantOverride("separation", 24);
        _content.Modulate = new Color(1, 1, 1, 0);
        center.AddChild(_content);

        var title = new Label { Text = Strings.Ui.GameTitle };
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        _content.AddChild(title);

        var copyright = new Label { Text = Strings.Legal.Copyright };
        UiTheme.StyleLabel(copyright, UiTheme.Colors.Muted, UiTheme.FontSizes.Body);
        copyright.HorizontalAlignment = HorizontalAlignment.Center;
        _content.AddChild(copyright);

        var disclosure = new Label { Text = Strings.Legal.AiDisclosure };
        UiTheme.StyleLabel(disclosure, UiTheme.Colors.Muted, UiTheme.FontSizes.Body);
        disclosure.HorizontalAlignment = HorizontalAlignment.Center;
        _content.AddChild(disclosure);

        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(0, 20);
        _content.AddChild(spacer);

        var hint = new Label { Text = Strings.Legal.PressAnyKey };
        UiTheme.StyleLabel(hint, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        _content.AddChild(hint);

        var tween = CreateTween();
        tween.TweenProperty(_content, "modulate:a", 1.0f, FadeInDuration);

        var inputTimer = GetTree().CreateTimer(InputIgnoreWindow);
        inputTimer.Connect(SceneTreeTimer.SignalName.Timeout,
            Callable.From(() => _acceptsInput = true));

        var timer = GetTree().CreateTimer(HoldDuration);
        timer.Connect(SceneTreeTimer.SignalName.Timeout, Callable.From(Dismiss));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_dismissed) return;

        // During the input-ignore window, still CONSUME key/mouse-press events
        // so they can't leak past this splash to the screens underneath
        // (e.g., the queued Enter from the terminal launch would otherwise
        // propagate to the future SplashScreen and press its focused button).
        if (!_acceptsInput)
        {
            if (@event is InputEventKey || @event is InputEventMouseButton)
                GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            Dismiss();
            GetViewport().SetInputAsHandled();
        }
        else if (@event is InputEventMouseButton mouse && mouse.Pressed)
        {
            Dismiss();
            GetViewport().SetInputAsHandled();
        }
    }

    private void Dismiss()
    {
        if (_dismissed || !IsInstanceValid(this) || !IsInsideTree()) return;
        _dismissed = true;
        EmitSignal(SignalName.ContinuePressed);
    }
}
