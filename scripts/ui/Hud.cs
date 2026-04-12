using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

public partial class Hud : Control
{
    private Label _statsLabel = null!;

    public override void _Ready()
    {
        _statsLabel = GetNode<Label>("PanelContainer/MarginContainer/VBoxContainer/StatsLabel");
        MouseFilter = MouseFilterEnum.Ignore;

        GameState.Instance.Connect(
            GameState.SignalName.StatsChanged,
            new Callable(this, MethodName.OnStatsChanged));
        OnStatsChanged();
    }

    private void OnStatsChanged()
    {
        var gs = GameState.Instance;
        _statsLabel.Text = Strings.Hud.Stats(gs.Hp, gs.MaxHp, gs.Mana, gs.MaxMana, gs.Xp, gs.Level, gs.FloorNumber, gs.PlayerInventory.Gold);
    }
}
