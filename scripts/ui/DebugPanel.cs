using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Debug stats overlay — toggle with F3.
/// </summary>
public partial class DebugPanel : Control
{
    private Label _statsLabel = null!;
    private int _killCount;
    private double _sessionTime;

    public override void _Ready()
    {
        Visible = false;

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", UiTheme.CreatePanelStyle(0.85f));
        panel.SetAnchorsPreset(LayoutPreset.TopRight);
        panel.Position = new Vector2(-260, 12);
        panel.CustomMinimumSize = new Vector2(240, 0);
        AddChild(panel);

        var margin = new MarginContainer();
        panel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 2);
        margin.AddChild(vbox);

        var title = new Label();
        title.Text = Strings.Ui.DebugToggle;
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, UiTheme.FontSizes.Label);
        vbox.AddChild(title);

        _statsLabel = new Label();
        UiTheme.StyleLabel(_statsLabel, UiTheme.Colors.Ink, UiTheme.FontSizes.Small);
        vbox.AddChild(_statsLabel);

        GameState.Instance.Connect(
            GameState.SignalName.StatsChanged,
            new Callable(this, MethodName.UpdateStats));
        EventBus.Instance.Connect(
            EventBus.SignalName.EnemyDefeated,
            new Callable(this, MethodName.OnEnemyDefeated));

        UpdateStats();
    }

    public override void _Process(double delta)
    {
        if (!Visible)
            return;

        _sessionTime += delta;
        UpdateStats();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.F3)
        {
            Visible = !Visible;
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnEnemyDefeated(Vector2 position, int level)
    {
        _killCount++;
    }

    private void UpdateStats()
    {
        var gs = GameState.Instance;
        int enemyCount = GetTree().GetNodesInGroup(Constants.Groups.Enemies).Count;
        int xpToNext = Constants.Leveling.GetXpToLevel(gs.Level);
        float xpPercent = xpToNext > 0 ? (float)gs.Xp / xpToNext * 100 : 0;
        int playerDamage = Constants.PlayerStats.GetDamage(gs.Level);
        int minutes = (int)(_sessionTime / 60);
        int seconds = (int)(_sessionTime % 60);

        _statsLabel.Text =
            $"HP: {gs.Hp}/{gs.MaxHp}\n" +
            $"Level: {gs.Level}\n" +
            $"XP: {gs.Xp}/{xpToNext} ({xpPercent:F0}%)\n" +
            $"Floor: {gs.FloorNumber}\n" +
            $"Damage: {playerDamage}/hit\n" +
            $"---\n" +
            $"Enemies alive: {enemyCount}\n" +
            $"Total kills: {_killCount}\n" +
            $"Kills/level: {(_killCount > 0 && gs.Level > 1 ? _killCount / (gs.Level - 1) : 0)}\n" +
            $"Session: {minutes}:{seconds:D2}";
    }
}
