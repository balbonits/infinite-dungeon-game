using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Diablo-style skill hotbar displayed at the bottom of the screen.
/// Shows 4 skill slots with keybinds (1-4), skill names, and cooldown overlays.
/// </summary>
public partial class SkillBarHud : Control
{
    public static SkillBarHud? Instance { get; private set; }

    private const float SkillCooldown = 2.0f; // Default cooldown in seconds
    private static readonly string[] SlotKeys = { "1", "2", "3", "4" };

    private HBoxContainer _barContainer = null!;
    private readonly PanelContainer[] _slotPanels = new PanelContainer[SkillBar.SlotCount];
    private readonly Label[] _keyLabels = new Label[SkillBar.SlotCount];
    private readonly Label[] _nameLabels = new Label[SkillBar.SlotCount];
    private readonly ColorRect[] _cooldownOverlays = new ColorRect[SkillBar.SlotCount];

    public override void _Ready()
    {
        Instance = this;
        MouseFilter = MouseFilterEnum.Ignore;
        BuildBar();
        RefreshDisplay();
    }

    private void BuildBar()
    {
        // Anchor to bottom-center
        SetAnchorsPreset(LayoutPreset.BottomWide);
        OffsetTop = -52;

        _barContainer = new HBoxContainer();
        _barContainer.Alignment = BoxContainer.AlignmentMode.Center;
        _barContainer.AddThemeConstantOverride("separation", 4);
        _barContainer.SetAnchorsPreset(LayoutPreset.BottomWide);
        _barContainer.OffsetTop = -48;
        _barContainer.OffsetBottom = -4;
        _barContainer.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_barContainer);

        for (int i = 0; i < SkillBar.SlotCount; i++)
        {
            var panel = new PanelContainer();
            panel.CustomMinimumSize = new Vector2(90, 44);
            panel.AddThemeStyleboxOverride("panel", CreateSlotStyle());
            panel.MouseFilter = MouseFilterEnum.Ignore;
            _barContainer.AddChild(panel);
            _slotPanels[i] = panel;

            var vbox = new VBoxContainer();
            vbox.AddThemeConstantOverride("separation", 0);
            vbox.MouseFilter = MouseFilterEnum.Ignore;
            panel.AddChild(vbox);

            // Key label (e.g., "[1]")
            var keyLabel = new Label();
            keyLabel.Text = $"[{SlotKeys[i]}]";
            keyLabel.HorizontalAlignment = HorizontalAlignment.Center;
            UiTheme.StyleLabel(keyLabel, UiTheme.Colors.Accent, 10);
            keyLabel.MouseFilter = MouseFilterEnum.Ignore;
            vbox.AddChild(keyLabel);
            _keyLabels[i] = keyLabel;

            // Skill name
            var nameLabel = new Label();
            nameLabel.Text = "---";
            nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
            UiTheme.StyleLabel(nameLabel, UiTheme.Colors.Ink, 11);
            nameLabel.MouseFilter = MouseFilterEnum.Ignore;
            vbox.AddChild(nameLabel);
            _nameLabels[i] = nameLabel;

            // Cooldown overlay
            var overlay = new ColorRect();
            overlay.Color = new Color(0, 0, 0, 0.5f);
            overlay.SetAnchorsPreset(LayoutPreset.FullRect);
            overlay.Visible = false;
            overlay.MouseFilter = MouseFilterEnum.Ignore;
            panel.AddChild(overlay);
            _cooldownOverlays[i] = overlay;
        }
    }

    private static StyleBoxFlat CreateSlotStyle()
    {
        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.08f, 0.08f, 0.12f, 0.85f);
        style.BorderColor = UiTheme.Colors.Accent;
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(3);
        style.ContentMarginLeft = 4;
        style.ContentMarginRight = 4;
        style.ContentMarginTop = 2;
        style.ContentMarginBottom = 2;
        return style;
    }

    public override void _Process(double delta)
    {
        var bar = GameState.Instance.SkillHotbar;
        bar.Update((float)delta);

        // Update cooldown overlays
        for (int i = 0; i < SkillBar.SlotCount; i++)
            _cooldownOverlays[i].Visible = bar.GetCooldown(i) > 0;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (GameState.Instance.IsDead) return;
        if (@event is not InputEventKey key || !key.Pressed || key.Echo) return;

        int slotIndex = key.Keycode switch
        {
            Key.Key1 => 0,
            Key.Key2 => 1,
            Key.Key3 => 2,
            Key.Key4 => 3,
            _ => -1,
        };

        if (slotIndex < 0) return;

        var bar = GameState.Instance.SkillHotbar;
        string? skillId = bar.TryActivate(slotIndex, SkillCooldown);
        if (skillId != null)
        {
            var def = SkillDatabase.Get(skillId);
            if (def != null)
            {
                // Record use for skill XP
                GameState.Instance.Skills.RecordUse(skillId, GameState.Instance.FloorNumber);

                // Visual feedback
                FloatingText.Spawn(
                    GetTree().Root,
                    GetViewport().GetVisibleRect().Size / 2 + new Vector2(0, -60),
                    def.Name, UiTheme.Colors.Accent, 14, 0.8f);

                Toast.Instance?.Info($"{def.Name} [{SlotKeys[slotIndex]}]");
            }
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>Refresh slot display (call after assigning skills).</summary>
    public void RefreshDisplay()
    {
        var bar = GameState.Instance.SkillHotbar;
        for (int i = 0; i < SkillBar.SlotCount; i++)
        {
            string? skillId = bar.GetSlot(i);
            if (skillId != null)
            {
                var def = SkillDatabase.Get(skillId);
                _nameLabels[i].Text = def?.Name ?? "???";
                _nameLabels[i].AddThemeColorOverride("font_color", UiTheme.Colors.Ink);
            }
            else
            {
                _nameLabels[i].Text = "---";
                _nameLabels[i].AddThemeColorOverride("font_color", UiTheme.Colors.Muted);
            }
        }
    }
}
