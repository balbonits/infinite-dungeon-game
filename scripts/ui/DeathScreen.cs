using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Death screen with the 5-option sacrifice dialog per docs/systems/death.md.
///
/// Flow: cinematic (YOU DIED fade-in/hold/fade-out) → dialog appears with 5 options:
///   - Save Both (equip + backpack costs) — keep equipment AND backpack
///   - Save Equipment — keep gear, lose backpack (all items + gold)
///   - Save Backpack — keep pack, lose 1 random equipped piece
///   - Accept Fate — lose 1 equip + all backpack + all backpack gold, respawn in town
///   - Quit Game — same penalty as Accept Fate, quit to main menu (with confirmation)
///
/// Sacrificial Idol in backpack = free "Save Both" (idol consumed, EXP loss still applies).
/// EXP loss applies in all paths — it's the unavoidable tax.
/// </summary>
public partial class DeathScreen : Control
{
    private VBoxContainer _content = null!;
    private ColorRect _overlay = null!;
    private PanelContainer _panel = null!;
    private Label _youDiedLabel = null!;
    private bool _hasIdol;

    // SoulsBorne-style "YOU DIED" cinematic constants
    private const float DeathFadeInDuration = 1.2f;
    private const float YouDiedFadeInDuration = 1.5f;
    private const float YouDiedHoldDuration = 2.5f;
    private const float YouDiedFadeOutDuration = 0.8f;

    /// <summary>True while the "YOU DIED" cinematic is playing (before menu shows). For tests.</summary>
    public bool IsPlayingCinematic { get; private set; }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        _overlay = new ColorRect();
        _overlay.Color = new Color(0, 0, 0, 0);
        _overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_overlay);

        var youDiedCenter = new CenterContainer();
        youDiedCenter.SetAnchorsPreset(LayoutPreset.FullRect);
        youDiedCenter.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(youDiedCenter);

        _youDiedLabel = new Label();
        _youDiedLabel.Text = "YOU DIED";
        _youDiedLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _youDiedLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.05f, 0.05f, 1.0f));
        _youDiedLabel.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 1));
        _youDiedLabel.AddThemeConstantOverride("outline_size", 12);
        _youDiedLabel.AddThemeFontSizeOverride("font_size", 96);
        _youDiedLabel.Modulate = new Color(1, 1, 1, 0);
        youDiedCenter.AddChild(_youDiedLabel);

        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(center);

        _panel = new PanelContainer();
        _panel.AddThemeStyleboxOverride("panel", UiTheme.CreatePanelStyle(0.95f, true));
        _panel.CustomMinimumSize = new Vector2(440, 0);
        _panel.Modulate = new Color(1, 1, 1, 0);
        _panel.Visible = false;
        center.AddChild(_panel);

        var margin = new MarginContainer();
        _panel.AddChild(margin);

        _content = new VBoxContainer();
        _content.AddThemeConstantOverride("separation", 10);
        margin.AddChild(_content);
    }

    public void ShowDeathFlow()
    {
        Visible = true;
        _hasIdol = DeathPenalty.HasSacrificialIdol(GameState.Instance.PlayerInventory);

        _overlay.Color = new Color(0, 0, 0, 0);
        _youDiedLabel.Modulate = new Color(1, 1, 1, 0);
        _panel.Modulate = new Color(1, 1, 1, 0);
        _panel.Visible = false;

        PlayYouDiedCinematic();
    }

    private void PlayYouDiedCinematic()
    {
        IsPlayingCinematic = true;

        var tween = CreateTween();
        tween.SetPauseMode(Tween.TweenPauseMode.Process);

        tween.TweenProperty(_overlay, "color",
            new Color(0.08f, 0.02f, 0.02f, 0.95f), DeathFadeInDuration);
        tween.TweenProperty(_youDiedLabel, "modulate:a", 1.0f, YouDiedFadeInDuration);
        tween.TweenInterval(YouDiedHoldDuration);
        tween.TweenProperty(_youDiedLabel, "modulate:a", 0.0f, YouDiedFadeOutDuration);
        tween.TweenCallback(Callable.From(() =>
        {
            IsPlayingCinematic = false;
            _panel.Visible = true;
            ShowSacrificeDialog();
        }));
        tween.TweenProperty(_panel, "modulate:a", 1.0f, 0.3f);
    }

    private void ClearContent()
    {
        foreach (Node child in _content.GetChildren())
            child.QueueFree();
    }

    // ──────────────────── 5-OPTION SACRIFICE DIALOG ────────────────────

    private void ShowSacrificeDialog()
    {
        ClearContent();

        var gs = GameState.Instance;
        int floor = gs.DeepestFloor;
        long totalGold = gs.PlayerInventory.Gold + gs.PlayerBank.Gold;

        long equipCost = DeathPenalty.GetEquipmentBuyoutCost(floor);
        long backpackCost = DeathPenalty.GetBackpackBuyoutCost(floor);
        long bothCost = DeathPenalty.GetBothBuyoutCost(floor);
        float expLossPercent = DeathPenalty.GetExpLossPercent(floor);

        AddTitle(Strings.Death.Title);
        AddLabel($"Deepest Floor: {floor}", UiTheme.Colors.Muted);
        AddLabel($"EXP loss (unavoidable): {expLossPercent:F1}%", UiTheme.Colors.Danger);
        AddLabel($"Your gold: backpack {gs.PlayerInventory.Gold}g + bank {gs.PlayerBank.Gold}g = {totalGold}g",
            UiTheme.Colors.Muted);
        _content.AddChild(new HSeparator());

        if (_hasIdol)
        {
            // Spec (death.md): when idol is present, Save* buyouts become free (no reason
            // to pay). We present ONE free "Save Both" button plus the fall-through options.
            AddLabel("Sacrificial Idol found — it will be consumed to Save Both (free).",
                UiTheme.Colors.Safe);
            AddButton($"Consume Idol — {Strings.Death.SaveBoth} (free)",
                () => ApplySacrifice(SacrificeOption.SaveBoth, useIdol: true));
            _content.AddChild(new HSeparator());
            AddLabel("Or skip the idol:", UiTheme.Colors.Muted);

            AddButton($"{Strings.Death.AcceptFate}  (lose 1 equip + all backpack + backpack gold)",
                () => ConfirmAcceptFate(quitAfter: false));
            AddButton($"{Strings.Death.QuitGame}  (same penalty, quit to main menu)",
                () => ConfirmAcceptFate(quitAfter: true));
        }
        else
        {
            AddLabel("Choose your bargain:", UiTheme.Colors.Ink);

            AddSacrificeButton(Strings.Death.SaveBoth, "Keep equipment + all backpack contents",
                bothCost, totalGold, () => ApplySacrifice(SacrificeOption.SaveBoth));
            AddSacrificeButton(Strings.Death.SaveEquipment, "Keep gear. Lose all backpack items + gold.",
                equipCost, totalGold, () => ApplySacrifice(SacrificeOption.SaveEquipment));
            AddSacrificeButton(Strings.Death.SaveBackpack, "Keep pack. Lose 1 random equipped piece.",
                backpackCost, totalGold, () => ApplySacrifice(SacrificeOption.SaveBackpack));

            _content.AddChild(new HSeparator());

            AddButton($"{Strings.Death.AcceptFate}  (lose 1 equip + all backpack + backpack gold)",
                () => ConfirmAcceptFate(quitAfter: false));
            AddButton($"{Strings.Death.QuitGame}  (same penalty, quit to main menu)",
                () => ConfirmAcceptFate(quitAfter: true));
        }

        // Spec: keyboard focus lands on the first enabled option so arrow-nav + S/Enter
        // work immediately without the player having to click.
        CallDeferred(MethodName.FocusFirstContentButton);
    }

    private void FocusFirstContentButton() => UiTheme.FocusFirstButton(_content);

    private enum SacrificeOption { SaveBoth, SaveEquipment, SaveBackpack }

    private void ApplySacrifice(SacrificeOption option, bool useIdol = false)
    {
        var gs = GameState.Instance;
        int floor = gs.DeepestFloor;
        var backpack = gs.PlayerInventory;
        var bank = gs.PlayerBank;

        bool saveEquip = option == SacrificeOption.SaveBoth || option == SacrificeOption.SaveEquipment;
        bool savePack = option == SacrificeOption.SaveBoth || option == SacrificeOption.SaveBackpack;

        if (useIdol)
        {
            DeathPenalty.ConsumeSacrificialIdol(backpack);
            // Idol = free Save Both
            ApplyExpLoss();
            RespawnInTown();
            return;
        }

        long cost = option switch
        {
            SacrificeOption.SaveBoth => DeathPenalty.GetBothBuyoutCost(floor),
            SacrificeOption.SaveEquipment => DeathPenalty.GetEquipmentBuyoutCost(floor),
            SacrificeOption.SaveBackpack => DeathPenalty.GetBackpackBuyoutCost(floor),
            _ => 0L,
        };

        if (!DeathPenalty.PayBuyout(backpack, bank, cost))
        {
            Toast.Instance?.Error("Not enough gold across pockets!");
            return;
        }

        // Apply losses for un-saved targets
        if (!savePack) DeathPenalty.WipeBackpack(backpack);
        if (!saveEquip) DestroyRandomEquippedAndReport(gs);

        ApplyExpLoss();
        RespawnInTown();
    }

    /// <summary>
    /// Roll a random equipped-item slot and destroy it (SYS-12 equipment-on-death).
    /// Surfaces the loss via a toast so the player knows what vanished. Spec: equipment.md
    /// § "Equipment on Death" — uniform roll over currently-equipped slots, Lock flag
    /// provides no protection, destroyed items do not enter bank/loot tables.
    /// </summary>
    private static void DestroyRandomEquippedAndReport(Autoloads.GameState gs)
    {
        var destroyed = gs.Equipment.DestroyRandomEquipped(new System.Random());
        if (destroyed != null)
            Toast.Instance?.Warning($"Lost equipped: {destroyed.Name}");
    }

    private void ConfirmAcceptFate(bool quitAfter)
    {
        // Second confirmation per spec — lists exact losses
        ClearContent();
        AddTitle(quitAfter ? "Quit Game?" : "Accept Fate?");
        _content.AddChild(new HSeparator());
        AddLabel("You will lose:", UiTheme.Colors.Danger);
        AddLabel("• 1 random equipped item", UiTheme.Colors.Danger);
        AddLabel("• All backpack items", UiTheme.Colors.Danger);
        var gs = GameState.Instance;
        AddLabel($"• {gs.PlayerInventory.Gold}g backpack gold",
            UiTheme.Colors.Danger);
        AddLabel("(Bank gold and bank items are safe.)", UiTheme.Colors.Muted);
        _content.AddChild(new HSeparator());

        AddButton(quitAfter ? "Confirm — Quit" : "Confirm — Accept", () =>
        {
            // Apply the full penalty: lose 1 equip + wipe backpack (items + gold) + exp loss
            var gs = GameState.Instance;
            DeathPenalty.WipeBackpack(gs.PlayerInventory);
            DestroyRandomEquippedAndReport(gs);
            ApplyExpLoss();

            gs.IsDead = false;
            gs.Hp = gs.MaxHp;
            gs.FloorNumber = 1;

            if (quitAfter)
            {
                // Spec (docs/systems/death.md): Quit Game applies the same penalty as Accept
                // Fate, then returns to main menu. Save so the penalty is preserved on next
                // load, then reload to the splash screen. (PauseMenu's separate "Quit Game"
                // button exits the application — that path is NOT used here.)
                bool ok = Autoloads.SaveManager.Instance?.Save() ?? true;
                GetTree().Paused = false;
                if (ok)
                {
                    GetTree().ReloadCurrentScene();
                    return;
                }
                // Save failed: surface the toast and defer the reload long enough for
                // the player to read it. Toast is mounted under Main's scene tree, so
                // calling ReloadCurrentScene immediately would tear it down before
                // it ever rendered (Copilot R1 finding on PR #16).
                Toast.Instance?.Error("Save failed — death penalty may not persist");
                var t = GetTree().CreateTimer(3.0);
                t.Connect(SceneTreeTimer.SignalName.Timeout,
                    Callable.From(() => GetTree().ReloadCurrentScene()));
            }
            else
            {
                RespawnInTown();
            }
        });
        AddButton("Back", () => ShowSacrificeDialog());
    }

    private void ApplyExpLoss()
    {
        var gs = GameState.Instance;
        int xpLoss = DeathPenalty.CalculateXpLoss(gs.Xp, gs.DeepestFloor);
        gs.Xp = System.Math.Max(0, gs.Xp - xpLoss);
    }

    private void RespawnInTown()
    {
        var gs = GameState.Instance;
        gs.IsDead = false;
        gs.Hp = gs.MaxHp;
        gs.FloorNumber = 1;

        ScreenTransition.Instance.Play(
            Strings.Town.Title,
            () =>
            {
                Visible = false;
                GetTree().Paused = false;
                Scenes.Main.Instance.LoadTown();
            },
            Strings.Town.Arriving);
    }

    // ──────────────────── UI HELPERS ────────────────────

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

    private void AddButton(string text, System.Action action)
    {
        var btn = new Button();
        btn.Text = text;
        btn.CustomMinimumSize = new Vector2(360, 40);
        btn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        UiTheme.StyleButton(btn);
        btn.Connect(BaseButton.SignalName.Pressed, Callable.From(action));
        _content.AddChild(btn);
    }

    private void AddSacrificeButton(string title, string detail, long cost, long totalGold, System.Action action)
    {
        var btn = new Button();
        btn.Text = $"{title}  ({cost}g)\n{detail}";
        btn.CustomMinimumSize = new Vector2(380, 56);
        btn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        btn.Disabled = totalGold < cost;
        UiTheme.StyleButton(btn, UiTheme.FontSizes.Body);
        btn.Connect(BaseButton.SignalName.Pressed, Callable.From(action));
        _content.AddChild(btn);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible) return;
        if (KeyboardNav.HandleConfirm(@event, GetViewport()))
        {
            GetViewport().SetInputAsHandled();
        }
    }
}
