using Godot;

/// <summary>
/// Gameplay HUD overlay. Renders HP/MP orbs, floor indicator,
/// gold counter, and level/XP display. Lives on a CanvasLayer
/// so it stays fixed on screen regardless of camera position.
/// Mouse input passes through to the game world.
/// </summary>
public partial class GameplayHud : Control
{
    private HpMpOrbs _orbs;
    private Label _floorLabel;
    private Label _goldLabel;
    private Label _levelLabel;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;

        // HP/MP orbs at bottom corners (self-positioning via viewport size)
        _orbs = new HpMpOrbs();
        AddChild(_orbs);

        // Floor number (top center)
        _floorLabel = CreateHudLabel(HorizontalAlignment.Center);
        _floorLabel.SetAnchorsPreset(LayoutPreset.CenterTop);
        _floorLabel.Position = new Vector2(0, 12);
        AddChild(_floorLabel);

        // Gold counter (top right)
        _goldLabel = CreateHudLabel(HorizontalAlignment.Right);
        _goldLabel.SetAnchorsPreset(LayoutPreset.TopRight);
        _goldLabel.Position = new Vector2(-12, 12);
        AddChild(_goldLabel);

        // Level and XP (top left)
        _levelLabel = CreateHudLabel(HorizontalAlignment.Left);
        _levelLabel.SetAnchorsPreset(LayoutPreset.TopLeft);
        _levelLabel.Position = new Vector2(12, 12);
        AddChild(_levelLabel);
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

    /// <summary>
    /// Create a HUD label with the standard ink-white color, text shadow,
    /// and 14px font size from docs/ui/hud.md.
    /// </summary>
    private Label CreateHudLabel(HorizontalAlignment align)
    {
        var label = new Label();
        label.HorizontalAlignment = align;
        label.AddThemeColorOverride("font_color", new Color("#ecf0ff"));     // ink white
        label.AddThemeFontSizeOverride("font_size", 14);
        label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.8f));
        label.AddThemeConstantOverride("shadow_offset_x", 1);
        label.AddThemeConstantOverride("shadow_offset_y", 1);
        return label;
    }
}
