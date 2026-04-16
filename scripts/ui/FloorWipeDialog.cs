using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Shown when all enemies on a floor are killed (floor wipe).
/// Awards bonus loot and presents navigation choices.
/// </summary>
public partial class FloorWipeDialog : GameWindow
{
    public static FloorWipeDialog Instance { get; private set; } = null!;

    private VBoxContainer _content = null!;

    public override void _Ready()
    {
        Instance = this;
        ReturnToPauseMenu = false;
        WindowWidth = 360;
        base._Ready();
    }

    protected override void BuildContent(VBoxContainer content)
    {
        _content = new VBoxContainer();
        _content.AddThemeConstantOverride("separation", 10);
        content.AddChild(_content);
    }

    public void ShowWipe()
    {
        int floor = GameState.Instance.FloorNumber;

        // Award bonus rewards
        int bonusGold = Constants.FloorWipe.GetBonusGold(floor);
        int bonusXp = Constants.FloorWipe.GetBonusXp(floor);
        GameState.Instance.PlayerInventory.Gold += bonusGold;
        GameState.Instance.AwardXp(bonusXp);

        // Bonus item drop (50% chance)
        ItemDef? bonusItem = null;
        if (GD.Randf() < Constants.FloorWipe.BonusItemDropChance)
        {
            bonusItem = LootTable.RollItemDrop(floor + 2); // Slightly better drops
            if (bonusItem != null)
                GameState.Instance.PlayerInventory.TryAdd(bonusItem);
        }

        // Build UI
        ClearContent();

        AddLabel(Strings.FloorWipe.Title, UiTheme.Colors.Accent, UiTheme.FontSizes.Title);
        AddLabel(Strings.FloorWipe.Subtitle, UiTheme.Colors.Muted, UiTheme.FontSizes.Body);

        _content.AddChild(new HSeparator());

        // Rewards summary
        AddLabel(Strings.FloorWipe.BonusGold(bonusGold), UiTheme.Colors.Accent, UiTheme.FontSizes.Button);
        AddLabel($"+{bonusXp} XP", UiTheme.Colors.Safe, UiTheme.FontSizes.Body);
        if (bonusItem != null)
            AddLabel($"Found: {bonusItem.Name}", UiTheme.Colors.Safe, UiTheme.FontSizes.Body);

        _content.AddChild(new HSeparator());

        // Choices
        AddButton(Strings.FloorWipe.NextFloor(floor + 1), () =>
        {
            Close();
            GameState.Instance.FloorNumber = floor + 1;
            ScreenTransition.Instance.Play(
                Strings.Floor.FloorNumber(floor + 1),
                () => Scenes.Main.Instance.LoadDungeon(),
                Strings.Floor.Descending);
        });

        AddButton(Strings.FloorWipe.StayOnFloor, () =>
        {
            Close();
            // Enemies will respawn via the spawn timer
        });

        if (floor > 1)
        {
            AddButton(Strings.FloorWipe.SelectFloor, () =>
            {
                Close();
                AscendDialog.Instance.Show();
            });
        }

        AddButton(Strings.FloorWipe.ReturnToTown, () =>
        {
            Close();
            ScreenTransition.Instance.Play(
                Strings.Town.DungeonEntrance,
                () => Scenes.Main.Instance.LoadTown(),
                Strings.Ascend.ReturningToTown);
        });

        // Show the window (GameWindow handles overlay, WindowStack, pause)
        Show();
        UiTheme.FocusFirstButton(_content);
    }

    private void ClearContent()
    {
        foreach (Node child in _content.GetChildren())
            child.QueueFree();
    }

    private void AddLabel(string text, Color color, int fontSize)
    {
        var label = new Label();
        label.Text = text;
        UiTheme.StyleLabel(label, color, fontSize);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        _content.AddChild(label);
    }

    private void AddButton(string text, System.Action action)
    {
        var btn = new Button();
        btn.Text = text;
        btn.CustomMinimumSize = new Vector2(280, 38);
        btn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        UiTheme.StyleButton(btn, UiTheme.FontSizes.Body);
        btn.Connect(BaseButton.SignalName.Pressed, Callable.From(action));
        _content.AddChild(btn);
    }
}
