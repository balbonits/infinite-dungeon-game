using Godot;

/// <summary>
/// Gameplay HUD overlay. Renders HP/MP orbs, floor indicator,
/// gold counter, and level/XP display. Uses responsive anchors
/// and margins — no hardcoded pixel positions.
/// </summary>
public partial class GameplayHud : Control
{
    private HpMpOrbs _orbs;
    private Label _floorLabel;
    private Label _goldLabel;
    private Label _levelLabel;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

        // HP/MP orbs (self-positioning via viewport size in _Draw)
        _orbs = new HpMpOrbs();
        AddChild(_orbs);

        // Top bar: MarginContainer with HBoxContainer for responsive layout
        var topMargin = new MarginContainer();
        topMargin.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);
        topMargin.OffsetBottom = 40;
        topMargin.AddThemeConstantOverride("margin_left", 16);
        topMargin.AddThemeConstantOverride("margin_right", 16);
        topMargin.AddThemeConstantOverride("margin_top", 10);
        topMargin.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(topMargin);

        var topBar = new HBoxContainer();
        topBar.MouseFilter = MouseFilterEnum.Ignore;
        topMargin.AddChild(topBar);

        // Level/XP (left)
        _levelLabel = CreateHudLabel(HorizontalAlignment.Left);
        _levelLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        topBar.AddChild(_levelLabel);

        // Floor (center)
        _floorLabel = CreateHudLabel(HorizontalAlignment.Center);
        _floorLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        topBar.AddChild(_floorLabel);

        // Gold (right)
        _goldLabel = CreateHudLabel(HorizontalAlignment.Right);
        _goldLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        topBar.AddChild(_goldLabel);
    }

    public override void _Process(double delta)
    {
        var p = GameState.Player;
        _orbs.UpdateValues(p.HP, p.MaxHP, p.MP, p.MaxMP);
        _floorLabel.Text = GameState.Location == GameLocation.Dungeon
            ? $"Floor {GameState.DungeonFloor}" : "Town";
        _goldLabel.Text = $"{p.Gold} Gold";
        _levelLabel.Text = $"Lv.{p.Level}  XP:{p.XP}/{p.XPToNextLevel}";
    }

    private Label CreateHudLabel(HorizontalAlignment align)
    {
        var label = new Label();
        label.HorizontalAlignment = align;
        label.MouseFilter = MouseFilterEnum.Ignore;
        label.AddThemeColorOverride("font_color", new Color("#ecf0ff"));
        label.AddThemeFontSizeOverride("font_size", 14);
        label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.8f));
        label.AddThemeConstantOverride("shadow_offset_x", 1);
        label.AddThemeConstantOverride("shadow_offset_y", 1);
        return label;
    }
}
