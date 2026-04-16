using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Multi-step death screen per docs/systems/death.md.
/// Step 1: Choose destination (Town or Last Safe Spot)
/// Step 2: Toggle mitigations (buy XP protection, buy backpack protection, idol auto-applies)
/// Step 3: Review penalties + confirm
/// </summary>
public partial class DeathScreen : Control
{
    private VBoxContainer _content = null!;
    private bool _protectXp;
    private bool _protectBackpack;
    private bool _hasIdol;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        // Full-screen overlay
        var overlay = new ColorRect();
        overlay.Color = new Color(0, 0, 0, 0.8f);
        overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(overlay);

        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", UiTheme.CreatePanelStyle(0.95f, true));
        panel.CustomMinimumSize = new Vector2(400, 0);
        center.AddChild(panel);

        var margin = new MarginContainer();
        panel.AddChild(margin);

        _content = new VBoxContainer();
        _content.AddThemeConstantOverride("separation", 12);
        margin.AddChild(_content);
    }

    public void ShowDeathFlow()
    {
        Visible = true;
        _protectXp = false;
        _protectBackpack = false;
        _hasIdol = DeathPenalty.HasSacrificialIdol(GameState.Instance.PlayerInventory);
        ShowStep1();
    }

    private void ClearContent()
    {
        foreach (Node child in _content.GetChildren())
            child.QueueFree();
    }

    // --- Step 1: Choose destination ---
    private void ShowStep1()
    {
        ClearContent();

        AddTitle(Strings.Death.Title);
        AddLabel(Strings.Death.Subtitle, UiTheme.Colors.Muted);
        _content.AddChild(new HSeparator());
        AddLabel(Strings.Death.ChooseDestination, UiTheme.Colors.Ink);

        AddButton(Strings.Death.ReturnToTown, () => ShowStep2(), autoFocus: true);

        // Future: "Respawn at Last Safe Spot" option
        // AddButton(Strings.Death.RespawnAtSafeSpot, () => ShowStep2(false));
    }

    // --- Step 2: Mitigation options ---
    private void ShowStep2()
    {
        ClearContent();

        // Spec (death.md): penalties scale with DEEPEST floor ever reached, not current
        int floor = GameState.Instance.DeepestFloor;
        int gold = GameState.Instance.PlayerInventory.Gold;
        int expCost = DeathPenalty.GetExpProtectionCost(floor);
        int backpackCost = DeathPenalty.GetBackpackProtectionCost(floor);
        float expLossPercent = DeathPenalty.GetExpLossPercent(floor);
        int itemsLost = DeathPenalty.GetItemsLost(floor);

        AddTitle(Strings.Death.MitigationTitle);
        _content.AddChild(new HSeparator());

        // Penalties preview
        AddLabel($"XP loss: {expLossPercent:F1}% of current level progress", UiTheme.Colors.Danger);
        AddLabel($"Items lost: {itemsLost} random backpack item(s)", UiTheme.Colors.Danger);
        _content.AddChild(new HSeparator());

        // XP protection toggle
        string xpText = gold >= expCost
            ? $"Protect XP ({expCost}g)"
            : $"Protect XP ({expCost}g) — not enough gold";
        var xpBtn = AddToggleButton(xpText, gold >= expCost, (toggled) =>
        {
            _protectXp = toggled;
        });

        // Backpack protection toggle
        if (_hasIdol)
        {
            AddLabel("Sacrificial Idol found — backpack protected automatically", UiTheme.Colors.Safe);
            _protectBackpack = true;
        }
        else
        {
            string bpText = gold >= backpackCost
                ? $"Protect Backpack ({backpackCost}g)"
                : $"Protect Backpack ({backpackCost}g) — not enough gold";
            AddToggleButton(bpText, gold >= backpackCost, (toggled) =>
            {
                _protectBackpack = toggled;
            });
        }

        _content.AddChild(new HSeparator());
        AddButton(Strings.Death.Confirm, () => ApplyPenaltiesAndRespawn());
    }

    // --- Apply penalties and respawn ---
    private void ApplyPenaltiesAndRespawn()
    {
        var gs = GameState.Instance;
        int floor = gs.DeepestFloor; // Spec: penalties use deepest floor
        var inventory = gs.PlayerInventory;

        // XP penalty
        if (!_protectXp)
        {
            int xpLoss = DeathPenalty.CalculateXpLoss(gs.Xp, floor);
            gs.Xp = System.Math.Max(0, gs.Xp - xpLoss);
        }
        else
        {
            int cost = DeathPenalty.GetExpProtectionCost(floor);
            inventory.Gold -= cost;
        }

        // Backpack penalty
        if (_hasIdol)
        {
            DeathPenalty.ConsumeSacrificialIdol(inventory);
        }
        else if (!_protectBackpack)
        {
            int itemsLost = DeathPenalty.GetItemsLost(floor);
            DeathPenalty.ApplyItemLoss(inventory, itemsLost);
        }
        else
        {
            int cost = DeathPenalty.GetBackpackProtectionCost(floor);
            inventory.Gold -= cost;
        }

        // Reset combat state (not inventory/gold/xp)
        gs.IsDead = false;
        gs.Hp = gs.MaxHp;
        gs.FloorNumber = 1;

        // Use ScreenTransition so the dungeon→town swap is hidden behind the fade-to-black.
        // Hide/unpause happen at midpoint when overlay is fully opaque.
        Ui.ScreenTransition.Instance.Play(
            Strings.Town.Title,
            () =>
            {
                Visible = false;
                GetTree().Paused = false;
                Scenes.Main.Instance.LoadTown();
            },
            Strings.Town.Arriving);
    }

    // --- UI helpers ---
    private void AddTitle(string text)
    {
        var label = new Label();
        label.Text = text;
        UiTheme.StyleLabel(label, UiTheme.Colors.Accent, UiTheme.FontSizes.Title);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        _content.AddChild(label);
    }

    private void AddLabel(string text, Color color)
    {
        var label = new Label();
        label.Text = text;
        UiTheme.StyleLabel(label, color, UiTheme.FontSizes.Body);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _content.AddChild(label);
    }

    private void AddButton(string text, System.Action action, bool autoFocus = false)
    {
        var btn = new Button();
        btn.Text = text;
        btn.CustomMinimumSize = new Vector2(260, 40);
        btn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        UiTheme.StyleButton(btn);
        btn.Connect(BaseButton.SignalName.Pressed, Callable.From(action));
        _content.AddChild(btn);
        if (autoFocus)
            btn.CallDeferred(Control.MethodName.GrabFocus);
    }

    private Button AddToggleButton(string text, bool enabled, System.Action<bool> onToggle)
    {
        var btn = new Button();
        btn.Text = $"[ ] {text}";
        btn.CustomMinimumSize = new Vector2(300, 36);
        btn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        btn.Disabled = !enabled;
        UiTheme.StyleButton(btn, UiTheme.FontSizes.Body);

        bool toggled = false;
        btn.Connect(BaseButton.SignalName.Pressed, Callable.From(() =>
        {
            toggled = !toggled;
            btn.Text = toggled ? $"[X] {text}" : $"[ ] {text}";
            onToggle(toggled);
        }));
        _content.AddChild(btn);
        return btn;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible)
            return;

        if (KeyboardNav.HandleConfirm(@event, GetViewport()))
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
        {
            GetTree().Quit();
        }
    }
}
