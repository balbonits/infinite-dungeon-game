using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Floor selection dialog for the Teleporter NPC.
/// Shows all previously visited floors (1 to deepest).
/// Free teleportation to any visited floor.
/// </summary>
public partial class TeleportDialog : GameWindow
{
    public static TeleportDialog Instance { get; private set; } = null!;

    public override void _Ready()
    {
        Instance = this;
        ReturnToPauseMenu = false;
        base._Ready();
    }

    protected override void BuildContent(VBoxContainer content)
    {
        var title = new Label();
        title.Text = Strings.Teleport.Title;
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(title);

        var subtitle = new Label();
        subtitle.Text = Strings.Teleport.Subtitle;
        UiTheme.StyleLabel(subtitle, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        subtitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        content.AddChild(subtitle);

        content.AddChild(new HSeparator());

        Scroll.CustomMinimumSize = new Vector2(0, 280);
        ScrollContent.AddThemeConstantOverride("separation", 6);
        content.AddChild(Scroll);

        // Cancel at bottom (outside scroll)
        var cancelBtn = new Button();
        cancelBtn.Text = Strings.Ui.Cancel;
        cancelBtn.CustomMinimumSize = new Vector2(260, 38);
        cancelBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        UiTheme.StyleSecondaryButton(cancelBtn, UiTheme.FontSizes.Body);
        cancelBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(() => Close()));
        content.AddChild(cancelBtn);
    }

    protected override void OnShow()
    {
        // Clear old buttons
        foreach (Node child in ScrollContent.GetChildren())
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

        // Fall back to the Cancel button if the floor list is empty.
        UiTheme.FocusFirstButtonOrFallback(ScrollContent, ContentBox);
    }

    private void TeleportToFloor(int floor)
    {
        GameState.Instance.FloorNumber = floor;
        // Keep dialog visible so ScreenTransition fades over it; close inside midpoint
        // callback (when overlay is opaque) to prevent any flash of empty viewport.
        ScreenTransition.Instance.Play(
            Strings.Floor.FloorNumber(floor),
            () =>
            {
                Close();
                Scenes.Main.Instance.LoadDungeon();
            },
            Strings.Teleport.Teleporting);
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
        ScrollContent.AddChild(button);
    }
}
