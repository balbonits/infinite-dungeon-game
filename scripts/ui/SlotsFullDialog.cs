using Godot;

namespace DungeonGame.Ui;

/// <summary>
/// Modal shown when the user clicks New Game while all 3 save slots are full.
/// Industry-standard "your inventory is full" pattern: explain the constraint
/// + route the user to the place they can resolve it (Load Game to delete),
/// instead of silently blocking the click with only a transient toast.
///
/// Use <see cref="Create"/> to build; add to tree, then call <see cref="Open"/>.
/// </summary>
public partial class SlotsFullDialog : GameWindow
{
    private System.Action? _onOpenLoadGame;

    public static SlotsFullDialog Create(System.Action onOpenLoadGame)
    {
        var dialog = new SlotsFullDialog();
        dialog._onOpenLoadGame = onOpenLoadGame;
        return dialog;
    }

    public override void _Ready()
    {
        WindowWidth = 480f;
        // Splash-triggered dialogs must NOT fall through to PauseMenu on close
        // — GameWindow.ReturnToPauseMenu defaults to true, which opens the
        // pause menu via its sibling path. Splash has no PauseMenu sibling,
        // so the path resolves wrong and leaks state. (Copilot PR #33 finding.)
        ReturnToPauseMenu = false;
        base._Ready();
    }

    protected override void BuildContent(VBoxContainer content)
    {
        var title = new Label { Text = "ALL SAVE SLOTS ARE FULL" };
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, UiTheme.FontSizes.Title);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(title);

        content.AddChild(new HSeparator());

        var body = new Label
        {
            Text = "You already have 3 characters — the maximum. To start a new one, " +
                   "delete an existing character from the Load Game screen first.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        UiTheme.StyleLabel(body, UiTheme.Colors.Ink, UiTheme.FontSizes.Body);
        body.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(body);

        content.AddChild(new HSeparator());

        var row = new HBoxContainer();
        row.Alignment = BoxContainer.AlignmentMode.Center;
        row.AddThemeConstantOverride("separation", 16);
        content.AddChild(row);

        var cancel = new Button { Text = "Cancel" };
        cancel.CustomMinimumSize = new Vector2(160, 40);
        cancel.FocusMode = FocusModeEnum.All;
        UiTheme.StyleSecondaryButton(cancel, UiTheme.FontSizes.Button);
        // Close + free so repeat-blocked-clicks don't accumulate hidden
        // SlotsFullDialog instances under splash. (Copilot PR #33 finding.)
        cancel.Pressed += () => { Close(); QueueFree(); };
        row.AddChild(cancel);

        var openLoad = new Button { Text = "Open Load Game" };
        openLoad.CustomMinimumSize = new Vector2(200, 40);
        openLoad.FocusMode = FocusModeEnum.All;
        UiTheme.StyleButton(openLoad, UiTheme.FontSizes.Button);
        openLoad.Pressed += () =>
        {
            Close();
            QueueFree();
            _onOpenLoadGame?.Invoke();
        };
        row.AddChild(openLoad);

        // Default focus goes to the affirmative action so keyboard users can
        // press Enter to resolve the block immediately.
        openLoad.CallDeferred(Control.MethodName.GrabFocus);
    }

    public void Open() => Show();

    public override void _UnhandledInput(InputEvent @event)
    {
        // GameWindow.Close() on Cancel hides the Overlay but leaves this node
        // in the splash's child list. Repeated blocked-New-Game clicks would
        // then accumulate hidden dialogs even though the button handlers
        // QueueFree correctly — the keyboard-cancel path bypassed that.
        // (Copilot PR #33 round-4.)
        if (IsOpen && KeyboardNav.IsCancelPressed(@event))
        {
            Close();
            QueueFree();
            GetViewport()?.SetInputAsHandled();
            return;
        }
        base._UnhandledInput(@event);
    }
}
