using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

public partial class Hud : Control
{
    private Label _statsLabel = null!;
    private OrbDisplay _hpOrb = null!;
    private OrbDisplay _mpOrb = null!;
    private Label _hpText = null!;
    private Label _mpText = null!;

    private const float OrbSize = 56f;

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
        // HP orb — bottom-left
        var hpContainer = new VBoxContainer();
        hpContainer.SetAnchorsPreset(LayoutPreset.BottomLeft);
        hpContainer.OffsetLeft = 12;
        hpContainer.OffsetBottom = -60; // above skill bar
        hpContainer.OffsetTop = hpContainer.OffsetBottom - OrbSize - 16;
        hpContainer.AddThemeConstantOverride("separation", 2);
        hpContainer.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(hpContainer);

        _hpOrb = new OrbDisplay();
        _hpOrb.Configure(OrbSize, new Color("CC2222"), Constants.Assets.OrbHp);
        hpContainer.AddChild(_hpOrb);

        _hpText = new Label();
        _hpText.HorizontalAlignment = HorizontalAlignment.Center;
        _hpText.MouseFilter = MouseFilterEnum.Ignore;
        UiTheme.StyleLabel(_hpText, UiTheme.Colors.Danger, 10);
        hpContainer.AddChild(_hpText);

        // MP orb — bottom-left, next to HP
        var mpContainer = new VBoxContainer();
        mpContainer.SetAnchorsPreset(LayoutPreset.BottomLeft);
        mpContainer.OffsetLeft = 12 + OrbSize + 8;
        mpContainer.OffsetBottom = -60;
        mpContainer.OffsetTop = mpContainer.OffsetBottom - OrbSize - 16;
        mpContainer.AddThemeConstantOverride("separation", 2);
        mpContainer.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(mpContainer);

        _mpOrb = new OrbDisplay();
        _mpOrb.Configure(OrbSize, new Color("2244CC"), Constants.Assets.OrbMp);
        mpContainer.AddChild(_mpOrb);

        _mpText = new Label();
        _mpText.HorizontalAlignment = HorizontalAlignment.Center;
        _mpText.MouseFilter = MouseFilterEnum.Ignore;
        UiTheme.StyleLabel(_mpText, UiTheme.Colors.Info, 10);
        mpContainer.AddChild(_mpText);
    }

    private void OnStatsChanged()
    {
        var gs = GameState.Instance;

        // Text stats (top-left panel — XP, level, floor, gold)
        _statsLabel.Text = $"XP: {gs.Xp} | LVL: {gs.Level} | Floor: {gs.FloorNumber} | Gold: {gs.PlayerInventory.Gold}";

        // Orbs
        _hpOrb.SetRatio(gs.Hp, gs.MaxHp, $"{gs.Hp}/{gs.MaxHp}");
        _mpOrb.SetRatio(gs.Mana, gs.MaxMana, $"{gs.Mana}/{gs.MaxMana}");
        _hpText.Text = "HP";
        _mpText.Text = "MP";
    }
}
