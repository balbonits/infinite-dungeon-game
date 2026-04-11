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

        // Auto-focus first button after short delay
        var timer = GetTree().CreateTimer(0.3);
        timer.Connect(SceneTreeTimer.SignalName.Timeout, Callable.From(() =>
        {
            _ready = true;
            UiTheme.FocusFirstButton(btnBox);
        }));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_ready || !Visible)
            return;

        if (Visible && KeyboardNav.HandleInput(@event, this))
            GetViewport().SetInputAsHandled();
    }
}
