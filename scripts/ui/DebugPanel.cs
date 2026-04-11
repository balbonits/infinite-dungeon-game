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
        var s = gs.Stats;
        int enemyCount = GetTree().GetNodesInGroup(Constants.Groups.Enemies).Count;
        int xpToNext = Constants.Leveling.GetXpToLevel(gs.Level);
        float xpPercent = xpToNext > 0 ? (float)gs.Xp / xpToNext * 100 : 0;
        int baseDamage = Constants.PlayerStats.GetDamage(gs.Level);
        int minutes = (int)(_sessionTime / 60);
        int seconds = (int)(_sessionTime % 60);

        _statsLabel.Text =
            $"HP: {gs.Hp}/{gs.MaxHp}  Gold: {gs.PlayerInventory.Gold}\n" +
            $"Level: {gs.Level}  Class: {gs.SelectedClass}\n" +
            $"XP: {gs.Xp}/{xpToNext} ({xpPercent:F0}%)\n" +
            $"Floor: {gs.FloorNumber}\n" +
            $"---STATS---\n" +
            $"STR: {s.Str} ({StatBlock.GetEffective(s.Str):F0}eff)\n" +
            $"DEX: {s.Dex} ({StatBlock.GetEffective(s.Dex):F0}eff)\n" +
            $"STA: {s.Sta} ({StatBlock.GetEffective(s.Sta):F0}eff)\n" +
            $"INT: {s.Int} ({StatBlock.GetEffective(s.Int):F0}eff)\n" +
            $"Free pts: {s.FreePoints}\n" +
            $"---DERIVED---\n" +
            $"Base dmg: {baseDamage}  Melee+: {s.MeleeFlatBonus:F0}\n" +
            $"Atk spd: {s.AttackSpeedMultiplier:F2}x\n" +
            $"Dodge: {s.DodgeChance * 100:F1}%\n" +
            $"HP regen: {s.HpRegen:F1}/s\n" +
            $"Spell dmg: {s.SpellDamageMultiplier:F2}x\n" +
            $"---COMBAT---\n" +
            $"Enemies: {enemyCount}  Kills: {_killCount}\n" +
            $"Session: {minutes}:{seconds:D2}";
    }
}
