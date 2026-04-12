using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Dialog shown when stepping on stairs-up. Options:
/// - Return to Town (always available)
/// - Go up one floor (floor 2+)
/// - Select a specific upper floor (floor 3+, dropdown)
/// - Cancel
/// </summary>
public partial class AscendDialog : Control
{
    public static AscendDialog Instance { get; private set; } = null!;

    private PanelContainer _panel = null!;
    private VBoxContainer _buttonContainer = null!;
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
        var overlay = new ColorRect();
        overlay.Color = new Color(0, 0, 0, 0.5f);
        overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        overlay.Visible = false;
        overlay.MouseFilter = MouseFilterEnum.Stop;
        AddChild(overlay);

        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        center.Visible = false;
        AddChild(center);

        _panel = new PanelContainer();
        _panel.AddThemeStyleboxOverride("panel", UiTheme.CreatePanelStyle(0.95f, true));
        _panel.CustomMinimumSize = new Vector2(300, 0);
        center.AddChild(_panel);

        var margin = new MarginContainer();
        _panel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 12);
        margin.AddChild(vbox);

        var title = new Label();
        title.Text = Strings.Ascend.Title;
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        vbox.AddChild(new HSeparator());

        _buttonContainer = new VBoxContainer();
        _buttonContainer.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(_buttonContainer);
    }

    public new void Show()
    {
        if (_isOpen)
            return;

        _isOpen = true;
        GetTree().Paused = true;

        // Clear old buttons
        foreach (Node child in _buttonContainer.GetChildren())
            child.QueueFree();

        int currentFloor = GameState.Instance.FloorNumber;

        // Option: Return to Town (always available)
        AddButton(Strings.Ascend.ReturnToTown, UiTheme.Colors.Safe, () =>
        {
            Close();
            ScreenTransition.Instance.Play(
                Strings.Town.DungeonEntrance,
                () => Scenes.Main.Instance.LoadTown(),
                Strings.Ascend.ReturningToTown);
        });

        // Option: Go up one floor (only if floor 2+)
        if (currentFloor > 1)
        {
            int targetFloor = currentFloor - 1;
            AddButton(Strings.Ascend.GoUpOneFloor(targetFloor), UiTheme.Colors.Ink, () =>
            {
                Close();
                GameState.Instance.FloorNumber = targetFloor;
                ScreenTransition.Instance.Play(
                    Strings.Floor.FloorNumber(targetFloor),
                    () => Scenes.Main.Instance.LoadDungeon(),
                    Strings.Ascend.Ascending);
            });
        }

        // Option: Select specific floor (only if floor 3+, gives dropdown-like list)
        if (currentFloor > 2)
        {
            AddButton(Strings.Ascend.SelectFloor, UiTheme.Colors.Muted, () =>
            {
                ShowFloorList(currentFloor);
            });
        }

        // Cancel
        AddButton(Strings.Ui.Cancel, UiTheme.Colors.Muted, Close);

        // Show
        GetChild<ColorRect>(0).Visible = true;
        GetChild<CenterContainer>(1).Visible = true;
        UiTheme.FocusFirstButton(_buttonContainer);
    }

    private void ShowFloorList(int currentFloor)
    {
        foreach (Node child in _buttonContainer.GetChildren())
            child.QueueFree();

        // List all floors from current-1 down to 1
        for (int floor = currentFloor - 1; floor >= 1; floor--)
        {
            int targetFloor = floor;
            string label = targetFloor == 1
                ? Strings.Ascend.Floor1Town
                : Strings.Floor.FloorNumber(targetFloor);
            AddButton(label, UiTheme.Colors.Ink, () =>
            {
                Close();
                if (targetFloor == 1)
                {
                    GameState.Instance.FloorNumber = 1;
                }
                else
                {
                    GameState.Instance.FloorNumber = targetFloor;
                }
                ScreenTransition.Instance.Play(
                    Strings.Floor.FloorNumber(targetFloor),
                    () => Scenes.Main.Instance.LoadDungeon(),
                    Strings.Ascend.Ascending);
            });
        }

        // Back to main options
        AddButton(Strings.Ascend.Back, UiTheme.Colors.Muted, () =>
        {
            // Rebuild main options
            Close();
            Show();
        });
    }

    public void Close()
    {
        _isOpen = false;
        GetTree().Paused = false;
        GetChild<ColorRect>(0).Visible = false;
        GetChild<CenterContainer>(1).Visible = false;
    }

    private void AddButton(string text, Color textColor, System.Action action)
    {
        var button = new Button();
        button.Text = text;
        button.CustomMinimumSize = new Vector2(260, 38);
        UiTheme.StyleButton(button, UiTheme.FontSizes.Body);
        button.Connect(BaseButton.SignalName.Pressed, Callable.From(action));
        _buttonContainer.AddChild(button);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_isOpen)
            return;

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

        if (@event is InputEventKey k && k.Pressed)
            GetViewport().SetInputAsHandled();
    }
}
