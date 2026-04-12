using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

public partial class Hud : Control
{
    private Label _statsLabel = null!;
    private OrbDisplay _hpOrb = null!;
    private OrbDisplay _mpOrb = null!;

    private const float OrbSize = 64f;
    private const float OrbMargin = 8f;
    private const float BottomOffset = 6f;

    public override void _Ready()
    {
        _statsLabel = GetNode<Label>("PanelContainer/MarginContainer/VBoxContainer/StatsLabel");
        MouseFilter = MouseFilterEnum.Ignore;

        BuildOrbs();

        GameState.Instance.Connect(
            GameState.SignalName.StatsChanged,
            new Callable(this, MethodName.OnStatsChanged));
        OnStatsChanged();
    }

    private void BuildOrbs()
    {
        // HP orb — bottom-left, flush with screen edge (Diablo style)
        _hpOrb = new OrbDisplay();
        _hpOrb.Configure(OrbSize, new Color("CC2222"), Constants.Assets.OrbHp);
        _hpOrb.SetAnchorsPreset(LayoutPreset.BottomLeft);
        _hpOrb.OffsetLeft = OrbMargin;
        _hpOrb.OffsetBottom = -BottomOffset;
        _hpOrb.OffsetTop = -BottomOffset - OrbSize;
        _hpOrb.OffsetRight = OrbMargin + OrbSize;
        AddChild(_hpOrb);

        // MP orb — bottom-right, flush with screen edge (Diablo style)
        _mpOrb = new OrbDisplay();
        _mpOrb.Configure(OrbSize, new Color("2244CC"), Constants.Assets.OrbMp);
        _mpOrb.SetAnchorsPreset(LayoutPreset.BottomRight);
        _mpOrb.OffsetRight = -OrbMargin;
        _mpOrb.OffsetBottom = -BottomOffset;
        _mpOrb.OffsetTop = -BottomOffset - OrbSize;
        _mpOrb.OffsetLeft = -OrbMargin - OrbSize;
        AddChild(_mpOrb);
    }

    private void OnStatsChanged()
    {
        var gs = GameState.Instance;

        // Text stats (top-left panel — XP, level, floor, gold)
        _statsLabel.Text = $"XP: {gs.Xp} | LVL: {gs.Level} | Floor: {gs.FloorNumber} | Gold: {gs.PlayerInventory.Gold}";

        // Orbs
        _hpOrb.SetRatio(gs.Hp, gs.MaxHp, $"{gs.Hp}/{gs.MaxHp}");
        _mpOrb.SetRatio(gs.Mana, gs.MaxMana, $"{gs.Mana}/{gs.MaxMana}");
    }
}
