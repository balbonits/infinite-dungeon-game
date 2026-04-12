using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Diablo-style skill hotbar displayed at the bottom of the screen.
/// Shows 4 skill slots with controller-aware keybind labels and cooldown overlays.
/// Slots triggered by shoulder + face button combos (L1+△, L1+✕, R1+△, R1+○).
/// </summary>
public partial class SkillBarHud : Control
{
    public static SkillBarHud? Instance { get; private set; }

    private const float SkillCooldown = 2.0f;

    // Slot bindings: (shoulder action, face action) combos
    private static readonly (string shoulder, string face)[] SlotBindings =
    {
        (Constants.InputActions.ShoulderLeft, Constants.InputActions.ActionTriangle),  // Slot 1: L1+△
        (Constants.InputActions.ShoulderLeft, Constants.InputActions.ActionCross),     // Slot 2: L1+✕
        (Constants.InputActions.ShoulderRight, Constants.InputActions.ActionTriangle), // Slot 3: R1+△
        (Constants.InputActions.ShoulderRight, Constants.InputActions.ActionCross),    // Slot 4: R1+✕
    };

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

            // Key label — reads actual bindings from InputMap
            var keyLabel = new Label();
            keyLabel.Text = GetSlotLabel(i);
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

        // Detect shoulder + face button combos
        int slotIndex = DetectSlot();
        if (slotIndex < 0) return;

        var bar = GameState.Instance.SkillHotbar;
        var def = bar.GetSlot(slotIndex) != null ? SkillDatabase.Get(bar.GetSlot(slotIndex)!) : null;
        if (def?.CombatConfig == null)
            return;

        // Check cooldown
        if (!bar.IsReady(slotIndex))
            return;

        // Execute through Player — handles mana deduction, targeting, damage
        var player = GetTree().GetFirstNodeInGroup(Constants.Groups.Player) as Scenes.Player;
        if (player == null)
            return;

        if (player.ExecuteSkill(def.CombatConfig, def.ManaCost))
        {
            // Skill cast succeeded — start cooldown and record use
            bar.TryActivate(slotIndex, def.Cooldown);
            GameState.Instance.Skills.RecordUse(def.Id, GameState.Instance.FloorNumber);
        }
        GetViewport().SetInputAsHandled();
    }

    /// <summary>Detect which slot combo is active (shoulder held + face pressed).</summary>
    private static int DetectSlot()
    {
        for (int i = 0; i < SkillBar.SlotCount; i++)
        {
            var (shoulder, face) = SlotBindings[i];
            if (Input.IsActionPressed(shoulder) && Input.IsActionJustPressed(face))
                return i;
        }
        return -1;
    }

    /// <summary>Get display label for a slot based on actual InputMap bindings.</summary>
    private static string GetSlotLabel(int index)
    {
        var (shoulder, face) = SlotBindings[index];
        string sKey = GetActionKeyName(shoulder);
        string fKey = GetActionKeyName(face);
        return $"{sKey}+{fKey}";
    }

    /// <summary>Read the first keyboard key bound to an action from InputMap.</summary>
    private static string GetActionKeyName(string action)
    {
        var events = InputMap.ActionGetEvents(action);
        foreach (var ev in events)
        {
            if (ev is InputEventKey keyEv)
                return OS.GetKeycodeString(keyEv.Keycode);
        }
        return "?";
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
