using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Floor selection dialog for the Teleporter NPC.
/// Shows all previously visited floors (1 to deepest).
/// Free teleportation to any visited floor.
/// </summary>
public partial class TeleportDialog : Control
{
    public static TeleportDialog Instance { get; private set; } = null!;

    private ColorRect _overlay = null!;
    private CenterContainer _center = null!;
    private VBoxContainer _buttonContainer = null!;
    private ScrollContainer _scrollContainer = null!;
    private bool _isOpen;

    public bool IsOpen => _isOpen;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
        MouseFilter = MouseFilterEnum.Ignore;
        BuildUi();
    }

    private void BuildUi()
    {
        _overlay = new ColorRect();
        _overlay.Color = new Color(0, 0, 0, 0.5f);
        _overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        _overlay.MouseFilter = MouseFilterEnum.Stop;
        _overlay.Visible = false;
        AddChild(_overlay);

        _center = new CenterContainer();
        _center.SetAnchorsPreset(LayoutPreset.FullRect);
        _center.Visible = false;
        AddChild(_center);

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", UiTheme.CreatePanelStyle(0.95f, true));
        panel.CustomMinimumSize = new Vector2(320, 0);
        _center.AddChild(panel);

        var margin = new MarginContainer();
        panel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 12);
        margin.AddChild(vbox);

        var title = new Label();
        title.Text = Strings.Teleport.Title;
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        var subtitle = new Label();
        subtitle.Text = Strings.Teleport.Subtitle;
        UiTheme.StyleLabel(subtitle, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        subtitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        vbox.AddChild(subtitle);

        vbox.AddChild(new HSeparator());

        _scrollContainer = new ScrollContainer();
        _scrollContainer.CustomMinimumSize = new Vector2(0, 280);
        vbox.AddChild(_scrollContainer);

        _buttonContainer = new VBoxContainer();
        _buttonContainer.AddThemeConstantOverride("separation", 6);
        _scrollContainer.AddChild(_buttonContainer);

        // Cancel at bottom (outside scroll)
        var cancelBtn = new Button();
        cancelBtn.Text = Strings.Ui.Cancel;
        cancelBtn.CustomMinimumSize = new Vector2(260, 38);
        cancelBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        UiTheme.StyleSecondaryButton(cancelBtn, UiTheme.FontSizes.Body);
        cancelBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(() => Close()));
        vbox.AddChild(cancelBtn);
    }

    public new void Show()
    {
        if (_isOpen) return;
        _isOpen = true;
        GetTree().Paused = true;

        // Clear old buttons
        foreach (Node child in _buttonContainer.GetChildren())
            child.QueueFree();

        int deepestFloor = GameState.Instance.FloorNumber;

        // Only show if the player has been deeper than floor 1
        if (deepestFloor <= 1)
        {
            AddFloorButton(Strings.Teleport.NoFloorsVisited, UiTheme.Colors.Muted, null);
        }
        else
        {
            // List floors from deepest down to 1
            for (int floor = deepestFloor; floor >= 1; floor--)
            {
                int targetFloor = floor;
                string label = Strings.Floor.FloorNumber(targetFloor);
                int zone = Constants.Zones.GetZone(targetFloor);
                string zoneLabel = $"{label}  (Zone {zone})";
                AddFloorButton(zoneLabel, UiTheme.Colors.Ink, () => TeleportToFloor(targetFloor));
            }
        }

        _overlay.Visible = true;
        _center.Visible = true;
        UiTheme.FocusFirstButton(_buttonContainer);
    }

    private void TeleportToFloor(int floor)
    {
        Close();
        GameState.Instance.FloorNumber = floor;
        ScreenTransition.Instance.Play(
            Strings.Floor.FloorNumber(floor),
            () => Scenes.Main.Instance.LoadDungeon(),
            Strings.Teleport.Teleporting);
    }

    public void Close()
    {
        _isOpen = false;
        GetTree().Paused = false;
        _overlay.Visible = false;
        _center.Visible = false;
    }

    private void AddFloorButton(string text, Color color, System.Action? action)
    {
        var button = new Button();
        button.Text = text;
        button.CustomMinimumSize = new Vector2(280, 34);
        UiTheme.StyleButton(button, UiTheme.FontSizes.Body);
        if (action != null)
            button.Connect(BaseButton.SignalName.Pressed, Callable.From(action));
        else
            button.Disabled = true;
        _buttonContainer.AddChild(button);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_isOpen) return;

        if (KeyboardNav.IsCancelPressed(@event))
        {
            Close();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (KeyboardNav.HandleInput(@event, _buttonContainer))
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        if (KeyboardNav.ConsumeMovement(@event))
            GetViewport().SetInputAsHandled();
    }
}
