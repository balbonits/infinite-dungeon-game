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
public partial class AscendDialog : GameWindow
{
    public static AscendDialog Instance { get; private set; } = null!;

    private VBoxContainer _buttonContainer = null!;

    public override void _Ready()
    {
        Instance = this;
        ReturnToPauseMenu = false;
        WindowWidth = 300;
        base._Ready();
    }

    protected override void BuildContent(VBoxContainer content)
    {
        var title = new Label();
        title.Text = Strings.Ascend.Title;
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(title);

        content.AddChild(new HSeparator());

        _buttonContainer = new VBoxContainer();
        _buttonContainer.AddThemeConstantOverride("separation", 8);
        content.AddChild(_buttonContainer);
    }

    protected override void OnShow()
    {
        // Clear old buttons
        foreach (Node child in _buttonContainer.GetChildren())
            child.QueueFree();

        int currentFloor = GameState.Instance.FloorNumber;

        // Option: Return to Town (always available)
        AddButton(Strings.Ascend.ReturnToTown, UiTheme.Colors.Safe, () =>
        {
            ScreenTransition.Instance.Play(
                Strings.Town.DungeonEntrance,
                () =>
                {
                    Close();
                    Scenes.Main.Instance.LoadTown();
                },
                Strings.Ascend.ReturningToTown);
        });

        // Option: Go up one floor (only if floor 2+)
        if (currentFloor > 1)
        {
            int targetFloor = currentFloor - 1;
            AddButton(Strings.Ascend.GoUpOneFloor(targetFloor), UiTheme.Colors.Ink, () =>
            {
                GameState.Instance.FloorNumber = targetFloor;
                ScreenTransition.Instance.Play(
                    Strings.Floor.FloorNumber(targetFloor),
                    () =>
                    {
                        Close();
                        Scenes.Main.Instance.LoadDungeon();
                    },
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
                    () =>
                    {
                        Close();
                        Scenes.Main.Instance.LoadDungeon();
                    },
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

    private void AddButton(string text, Color textColor, System.Action action)
    {
        var button = new Button();
        button.Text = text;
        button.CustomMinimumSize = new Vector2(260, 38);
        UiTheme.StyleButton(button, UiTheme.FontSizes.Body);
        button.Connect(BaseButton.SignalName.Pressed, Callable.From(action));
        _buttonContainer.AddChild(button);
    }
}
