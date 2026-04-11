using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Shown when all enemies on a floor are killed (floor wipe).
/// Awards bonus loot and presents navigation choices.
/// </summary>
public partial class FloorWipeDialog : Control
{
    public static FloorWipeDialog Instance { get; private set; } = null!;

    private ColorRect _overlay = null!;
    private CenterContainer _center = null!;
    private VBoxContainer _content = null!;
    private bool _isOpen;

    public bool IsOpen => _isOpen;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
        MouseFilter = MouseFilterEnum.Ignore;

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
        panel.CustomMinimumSize = new Vector2(360, 0);
        _center.AddChild(panel);

        var margin = new MarginContainer();
        panel.AddChild(margin);

        _content = new VBoxContainer();
        _content.AddThemeConstantOverride("separation", 10);
        margin.AddChild(_content);
    }

    public void ShowWipe()
    {
        if (_isOpen)
            return;

        _isOpen = true;
        GetTree().Paused = true;

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

        _overlay.Visible = true;
        _center.Visible = true;
        UiTheme.FocusFirstButton(_content);
    }

    private void Close()
    {
        _isOpen = false;
        _overlay.Visible = false;
        _center.Visible = false;
        GetTree().Paused = false;
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

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_isOpen)
            return;

        if (KeyboardNav.HandleInput(@event, _content))
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
    }
}
