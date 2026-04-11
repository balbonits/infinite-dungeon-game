using Godot;

namespace DungeonGame.Ui;

/// <summary>
/// Game splash/title screen. Shows game title, press any key to continue.
/// Flow: Splash → Class Select → Town.
/// </summary>
public partial class SplashScreen : Control
{
    [Signal] public delegate void ContinuePressedEventHandler();

    private bool _ready;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        // Dark background
        var bg = new ColorRect();
        bg.Color = UiTheme.Colors.BgDark;
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 20);
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
        spacer.CustomMinimumSize = new Vector2(0, 40);
        vbox.AddChild(spacer);

        // "Press any key" prompt (fades in/out)
        var prompt = new Label();
        prompt.Text = Strings.Splash.PressAnyKey;
        UiTheme.StyleLabel(prompt, UiTheme.Colors.Ink, UiTheme.FontSizes.Button);
        prompt.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(prompt);

        // Pulse animation on prompt
        var tween = CreateTween();
        tween.SetLoops();
        tween.TweenProperty(prompt, "modulate:a", 0.3f, 1.0f);
        tween.TweenProperty(prompt, "modulate:a", 1.0f, 1.0f);

        // Short delay before accepting input (prevents accidental skip)
        var timer = GetTree().CreateTimer(0.5);
        timer.Connect(SceneTreeTimer.SignalName.Timeout, Callable.From(() => _ready = true));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_ready || !Visible)
            return;

        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            EmitSignal(SignalName.ContinuePressed);
            GetViewport().SetInputAsHandled();
        }
    }
}
