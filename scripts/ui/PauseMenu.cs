using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Diablo 2-style tabbed pause menu. 8 tabs with content inline.
/// Replaces the old flat button list. Uses GameWindow + GameTabPanel.
/// Esc toggles open/close. Q/E switches tabs. Resume always visible.
/// </summary>
public partial class PauseMenu : GameWindow
{
    public static PauseMenu Instance { get; private set; } = null!;

    private GameTabPanel _tabs = null!;
    private Label _pointsLabel = null!;
    private Label _detailLabel = null!;

    // Inventory tab state
    private Label _invHeader = null!;
    private Label _invGold = null!;
    private Label _invDetail = null!;
    private VBoxContainer _invList = null!;

    // Stats tab state
    private Label _statPoints = null!;

    // Abilities tab — track alloc buttons for disabling
    private readonly System.Collections.Generic.List<Button> _abilityAllocButtons = new();

    public override void _Ready()
    {
        Instance = this;
        WindowWidth = 620f;
        ReturnToPauseMenu = false;
        base._Ready();
    }

    protected override void BuildContent(VBoxContainer content)
    {
        var title = new Label();
        title.Text = "PAUSED";
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, UiTheme.FontSizes.Title);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(title);

        _pointsLabel = new Label();
        UiTheme.StyleLabel(_pointsLabel, UiTheme.Colors.Safe, UiTheme.FontSizes.Body);
        _pointsLabel.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(_pointsLabel);

        _detailLabel = new Label();
        UiTheme.StyleLabel(_detailLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        _detailLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _detailLabel.CustomMinimumSize = new Vector2(0, 36);
        content.AddChild(_detailLabel);

        _tabs = new GameTabPanel();

        content.AddChild(new HSeparator());

        var resumeBtn = new Button();
        resumeBtn.Text = "Resume";
        resumeBtn.CustomMinimumSize = new Vector2(160, 38);
        resumeBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        UiTheme.StyleButton(resumeBtn, UiTheme.FontSizes.Body);
        resumeBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(() => Close()));
        content.AddChild(resumeBtn);
    }

    protected override void OnShow()
    {
        // Rebuild tabs each time (class may have changed)
        _tabs.GetParent()?.RemoveChild(_tabs);
        _tabs.Free();

        var cls = GameState.Instance.SelectedClass;
        string abilityTabName = cls switch
        {
            PlayerClass.Warrior => "Arts",
            PlayerClass.Ranger => "Crafts",
            PlayerClass.Mage => "Spells",
            _ => "Abilities",
        };

        _tabs = new GameTabPanel();
        _tabs.AddTab("Inventory", BuildInventoryTab);
        _tabs.AddTab("Equip", BuildEquipmentTab);
        _tabs.AddTab("Skills", BuildSkillsTab);
        _tabs.AddTab(abilityTabName, BuildAbilitiesTab);
        _tabs.AddTab("Quests", BuildQuestsTab);
        _tabs.AddTab("Ledger", BuildLedgerTab);
        _tabs.AddTab("Stats", BuildStatsTab);
        _tabs.AddTab("System", BuildSystemTab);

        // Insert tabs before the separator (second-to-last position)
        int sepIndex = ContentBox.GetChildCount() - 2;
        ContentBox.AddChild(_tabs);
        ContentBox.MoveChild(_tabs, sepIndex);

        _tabs.SelectTab(0);
    }

    protected override bool HandleTabInput(InputEvent @event) => _tabs?.HandleInput(@event) ?? false;

    public override void _UnhandledInput(InputEvent @event)
    {
        // Esc toggles the pause menu
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
        {
            var deathScreen = GetNodeOrNull<Control>("../DeathScreen");
            if (deathScreen != null && deathScreen.Visible) return;

            if (IsOpen)
                Close();
            else
                Show();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (!IsOpen) return;
        if (KeyboardNav.BlockIfNotTopmost(this, @event)) return;

        if (KeyboardNav.IsCancelPressed(@event))
        {
            Close();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (HandleTabInput(@event))
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        if (HandleExtraInput(@event))
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        if (KeyboardNav.HandleConfirm(@event, GetViewport()))
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventKey k && k.Pressed)
            GetViewport().SetInputAsHandled();
    }

    // ─── Tab 1: Inventory ───────────────────────────────────────────────

    private void BuildInventoryTab()
    {
        var inv = GameState.Instance.PlayerInventory;
        _pointsLabel.Text = "";

        _invHeader = new Label();
        _invHeader.Text = $"Backpack ({inv.UsedSlots}/{inv.SlotCount})";
        UiTheme.StyleLabel(_invHeader, UiTheme.Colors.Accent, UiTheme.FontSizes.Body);
        _invHeader.HorizontalAlignment = HorizontalAlignment.Center;
        _tabs.ScrollContent.AddChild(_invHeader);

        _invGold = new Label();
        _invGold.Text = $"Gold: {inv.Gold}";
        UiTheme.StyleLabel(_invGold, UiTheme.Colors.Accent, UiTheme.FontSizes.Small);
        _invGold.HorizontalAlignment = HorizontalAlignment.Center;
        _tabs.ScrollContent.AddChild(_invGold);

        _invDetail = new Label();
        UiTheme.StyleLabel(_invDetail, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        _invDetail.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _invDetail.CustomMinimumSize = new Vector2(0, 30);
        _invDetail.Text = "Select an item to view details";
        _tabs.ScrollContent.AddChild(_invDetail);

        var grid = new GridContainer();
        grid.Columns = 5;
        grid.AddThemeConstantOverride("h_separation", 4);
        grid.AddThemeConstantOverride("v_separation", 4);
        _tabs.ScrollContent.AddChild(grid);

        for (int i = 0; i < inv.SlotCount; i++)
        {
            var stack = inv.GetSlot(i);
            var btn = new Button();
            btn.CustomMinimumSize = new Vector2(64, 64);
            btn.FocusMode = FocusModeEnum.All;

            if (stack != null)
            {
                string name = stack.Item.Name.Length > 6 ? stack.Item.Name[..6] : stack.Item.Name;
                btn.Text = stack.Count > 1 ? $"{name}\nx{stack.Count}" : name;
                btn.AddThemeFontSizeOverride("font_size", UiTheme.FontSizes.Small);

                Color slotColor = stack.Item.Category switch
                {
                    ItemCategory.Consumable => new Color(0.2f, 0.5f, 0.2f, 0.4f),
                    ItemCategory.Material => new Color(0.2f, 0.3f, 0.5f, 0.4f),
                    ItemCategory.Weapon => new Color(0.5f, 0.3f, 0.15f, 0.4f),
                    ItemCategory.Armor => new Color(0.4f, 0.2f, 0.5f, 0.4f),
                    _ => new Color(0.15f, 0.15f, 0.2f, 0.4f),
                };
                btn.AddThemeStyleboxOverride("normal", UiTheme.CreateSlotStyle(slotColor, false));
                btn.AddThemeStyleboxOverride("focus", UiTheme.CreateSlotStyle(slotColor, true));
                btn.AddThemeColorOverride("font_color", UiTheme.Colors.Ink);

                var capturedItem = stack.Item;
                int capturedIdx = i;
                btn.FocusEntered += () =>
                {
                    string detail = $"{capturedItem.Name}\n{capturedItem.Description}";
                    if (capturedItem.HealAmount > 0) detail += $"\nHeals: {capturedItem.HealAmount} HP";
                    if (capturedItem.SellPrice > 0) detail += $"\nSell: {capturedItem.SellPrice}g";
                    _invDetail.Text = detail;
                };
                btn.Connect(BaseButton.SignalName.Pressed, Callable.From(() =>
                {
                    var menuActions = new System.Collections.Generic.List<(string label, System.Action action)>();
                    if (capturedItem.Category == ItemCategory.Consumable)
                    {
                        menuActions.Add(("Use", () =>
                        {
                            var gs = GameState.Instance;
                            if (capturedItem.HealAmount > 0)
                                gs.Hp = System.Math.Min(gs.MaxHp, gs.Hp + capturedItem.HealAmount);
                            if (capturedItem.ManaAmount > 0)
                                gs.Mana = System.Math.Min(gs.MaxMana, gs.Mana + capturedItem.ManaAmount);
                            gs.PlayerInventory.RemoveAt(capturedIdx);
                            Toast.Instance?.Success($"Used {capturedItem.Name}");
                            RefreshInventoryTab();
                        }
                        ));
                    }
                    menuActions.Add(("Drop", () => { inv.RemoveAt(capturedIdx); RefreshInventoryTab(); }));
                    ActionMenu.Instance?.Show(btn.GlobalPosition, menuActions.ToArray());
                }));
            }
            else
            {
                btn.Disabled = true;
                btn.AddThemeStyleboxOverride("normal", UiTheme.CreateEmptySlotStyle());
                btn.AddThemeFontSizeOverride("font_size", UiTheme.FontSizes.Small);
            }
            grid.AddChild(btn);
        }
    }

    private void RefreshInventoryTab()
    {
        var inv = GameState.Instance.PlayerInventory;
        _invHeader.Text = $"Backpack ({inv.UsedSlots}/{inv.SlotCount})";
        _invGold.Text = $"Gold: {inv.Gold}";
        _tabs.SelectTab(_tabs.CurrentTab);
    }

    // ─── Tab 2: Equipment ───────────────────────────────────────────────

    private void BuildEquipmentTab()
    {
        var gs = GameState.Instance;
        var eq = gs.Equipment;
        _pointsLabel.Text = $"Equipped: {eq.EquippedCount} / 19";
        _detailLabel.Text = "Select a slot to equip or unequip an item";

        AddEquipSlotRow("Main Hand", EquipSlot.MainHand);
        AddEquipSlotRow("Off Hand", EquipSlot.OffHand);
        AddEquipSlotRow("Ammo", EquipSlot.Ammo);
        _tabs.ScrollContent.AddChild(new HSeparator());
        AddEquipSlotRow("Head", EquipSlot.Head);
        AddEquipSlotRow("Body", EquipSlot.Body);
        AddEquipSlotRow("Arms", EquipSlot.Arms);
        AddEquipSlotRow("Legs", EquipSlot.Legs);
        AddEquipSlotRow("Feet", EquipSlot.Feet);
        _tabs.ScrollContent.AddChild(new HSeparator());
        AddEquipSlotRow("Neck", EquipSlot.Neck);

        var ringHeader = new Label();
        int ringsEquipped = 0;
        for (int i = 0; i < EquipmentSet.RingSlotCount; i++)
            if (eq.Rings[i] != null) ringsEquipped++;
        ringHeader.Text = $"Rings ({ringsEquipped} / {EquipmentSet.RingSlotCount})";
        UiTheme.StyleLabel(ringHeader, UiTheme.Colors.Accent, UiTheme.FontSizes.Body);
        _tabs.ScrollContent.AddChild(ringHeader);

        for (int i = 0; i < EquipmentSet.RingSlotCount; i++)
            AddEquipSlotRow($"  Ring {i + 1}", EquipSlot.Ring, ringIndex: i);

        _tabs.ScrollContent.AddChild(new HSeparator());

        var bonuses = eq.GetTotalBonuses(gs.SelectedClass);
        var summary = new Label();
        summary.Text = BuildBonusSummary(bonuses);
        UiTheme.StyleLabel(summary, UiTheme.Colors.Info, UiTheme.FontSizes.Small);
        summary.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _tabs.ScrollContent.AddChild(summary);
    }

    private void AddEquipSlotRow(string label, EquipSlot slot, int ringIndex = 0)
    {
        var gs = GameState.Instance;
        var eq = gs.Equipment;
        var item = eq.GetSlot(slot, ringIndex);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);

        var nameLabel = new Label();
        nameLabel.Text = label;
        UiTheme.StyleLabel(nameLabel, UiTheme.Colors.Ink, UiTheme.FontSizes.Body);
        nameLabel.CustomMinimumSize = new Vector2(100, 0);
        row.AddChild(nameLabel);

        var btn = new Button();
        btn.Text = item?.Name ?? "[empty]";
        btn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        btn.FocusMode = FocusModeEnum.All;
        UiTheme.StyleSecondaryButton(btn, UiTheme.FontSizes.Small);
        if (item == null)
            btn.AddThemeColorOverride("font_color", UiTheme.Colors.Muted);

        int capturedRingIndex = ringIndex;
        EquipSlot capturedSlot = slot;
        btn.Connect(BaseButton.SignalName.Pressed, Callable.From(() =>
            ShowEquipActionMenu(btn, capturedSlot, capturedRingIndex)));

        if (item != null)
        {
            string detail = $"{item.Name}\n{item.Description}";
            if (item.ClassAffinity.HasValue)
                detail += $"\nAffinity: {item.ClassAffinity} (+25% when matched)";
            btn.FocusEntered += () => _detailLabel.Text = detail;
        }

        row.AddChild(btn);
        _tabs.ScrollContent.AddChild(row);
    }

    private void ShowEquipActionMenu(Control anchor, EquipSlot slot, int ringIndex)
    {
        var gs = GameState.Instance;
        var eq = gs.Equipment;
        var current = eq.GetSlot(slot, ringIndex);
        var actions = new System.Collections.Generic.List<(string label, System.Action action)>();

        if (current != null)
        {
            actions.Add(("Unequip", () =>
            {
                if (eq.Unequip(slot, gs.PlayerInventory, ringIndex) != null)
                {
                    Toast.Instance?.Success($"Unequipped {current.Name}");
                    RefreshEquipmentTab();
                }
                else
                {
                    Toast.Instance?.Error("Backpack full");
                }
            }
            ));
        }
        else
        {
            // Empty slot — list compatible backpack items.
            var compatibles = FindCompatibleItems(slot);
            if (compatibles.Count == 0)
            {
                actions.Add(("(no compatible items)", () => { }));
            }
            else
            {
                foreach (var itemDef in compatibles)
                {
                    var captured = itemDef;
                    actions.Add(($"Equip {captured.Name}", () =>
                    {
                        if (eq.TryEquip(slot, captured, gs.PlayerInventory, ringIndex))
                        {
                            Toast.Instance?.Success($"Equipped {captured.Name}");
                            RefreshEquipmentTab();
                        }
                        else
                        {
                            Toast.Instance?.Error("Equip failed (backpack full?)");
                        }
                    }
                    ));
                }
            }
        }

        ActionMenu.Instance?.Show(anchor.GlobalPosition, actions.ToArray());
    }

    private static System.Collections.Generic.List<ItemDef> FindCompatibleItems(EquipSlot slot)
    {
        var results = new System.Collections.Generic.List<ItemDef>();
        var inv = GameState.Instance.PlayerInventory;
        var seen = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < inv.SlotCount; i++)
        {
            var stack = inv.GetSlot(i);
            if (stack == null) continue;
            if (seen.Contains(stack.Item.Id)) continue;
            if (EquipmentSet.IsCompatible(slot, stack.Item))
            {
                results.Add(stack.Item);
                seen.Add(stack.Item.Id);
            }
        }
        return results;
    }

    private void RefreshEquipmentTab()
    {
        _tabs.SelectTab(_tabs.CurrentTab);
    }

    private static string BuildBonusSummary(EquipmentBonuses b)
    {
        if (b.Str == 0 && b.Dex == 0 && b.Sta == 0 && b.Int == 0 && b.Hp == 0 && b.Damage == 0)
            return "No equipment bonuses.";
        var parts = new System.Collections.Generic.List<string>();
        if (b.Str != 0) parts.Add($"STR {b.Str:+0.#;-0.#}");
        if (b.Dex != 0) parts.Add($"DEX {b.Dex:+0.#;-0.#}");
        if (b.Sta != 0) parts.Add($"STA {b.Sta:+0.#;-0.#}");
        if (b.Int != 0) parts.Add($"INT {b.Int:+0.#;-0.#}");
        if (b.Hp != 0) parts.Add($"HP {b.Hp:+0.#;-0.#}");
        if (b.Damage != 0) parts.Add($"DMG {b.Damage:+0.#;-0.#}");
        return "Equipment bonuses: " + string.Join(", ", parts);
    }

    // ─── Tab 3: Skills ──────────────────────────────────────────────────

    private void BuildSkillsTab()
    {
        var tracker = GameState.Instance.Progression;
        _pointsLabel.Text = $"SP: {tracker.SkillPoints} available";
        _detailLabel.Text = "Select a mastery to view details";

        var categories = SkillAbilityDatabase.GetCategories(GameState.Instance.SelectedClass);
        foreach (string catId in categories)
        {
            var catLabel = new Label();
            catLabel.Text = $"── {SkillAbilityDatabase.GetCategoryName(catId)} ──";
            UiTheme.StyleLabel(catLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Body);
            catLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _tabs.ScrollContent.AddChild(catLabel);

            foreach (var def in SkillAbilityDatabase.GetMasteriesInCategory(catId))
            {
                var state = tracker.GetMastery(def.Id);
                _tabs.ScrollContent.AddChild(CreateMasteryRow(def, state, tracker));
            }
        }

        // Innate section
        var innateLabel = new Label();
        innateLabel.Text = "── Innate ──";
        UiTheme.StyleLabel(innateLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Body);
        innateLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _tabs.ScrollContent.AddChild(innateLabel);

        foreach (var def in SkillAbilityDatabase.GetInnateMasteries())
        {
            var state = tracker.GetMastery(def.Id);
            _tabs.ScrollContent.AddChild(CreateMasteryRow(def, state, tracker));
        }
    }

    private HBoxContainer CreateMasteryRow(MasteryDef def, MasteryState? state, ProgressionTracker tracker)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        int level = state?.Level ?? 0;

        var nameLabel = new Label();
        nameLabel.Text = $"▸ {def.Name} [Lv.{level}]";
        UiTheme.StyleLabel(nameLabel, level > 0 ? UiTheme.Colors.Ink : UiTheme.Colors.Muted, UiTheme.FontSizes.Body);
        nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(nameLabel);

        if (level > 0 && state != null)
        {
            var bonusLabel = new Label();
            bonusLabel.Text = $"+{state.GetPassiveBonus(def.PassiveMultiplier):F1}%";
            UiTheme.StyleLabel(bonusLabel, UiTheme.Colors.Safe, UiTheme.FontSizes.Small);
            row.AddChild(bonusLabel);
        }

        var allocBtn = new Button();
        allocBtn.Text = "+";
        allocBtn.CustomMinimumSize = new Vector2(32, 28);
        allocBtn.FocusMode = FocusModeEnum.All;
        allocBtn.AddThemeFontSizeOverride("font_size", UiTheme.FontSizes.Small);
        allocBtn.Disabled = tracker.SkillPoints <= 0;
        string capturedId = def.Id;
        float capturedMult = def.PassiveMultiplier;
        allocBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(() =>
        {
            if (!tracker.AllocateSP(capturedId)) return;
            var s = tracker.GetMastery(capturedId);
            int lvl = s?.Level ?? 0;
            nameLabel.Text = $"▸ {def.Name} [Lv.{lvl}]";
            nameLabel.AddThemeColorOverride("font_color", lvl > 0 ? UiTheme.Colors.Ink : UiTheme.Colors.Muted);
            _pointsLabel.Text = $"SP: {tracker.SkillPoints} available";
            _detailLabel.Text = BuildMasteryDetail(def, s);
        }));
        row.AddChild(allocBtn);

        string detail = BuildMasteryDetail(def, state);
        allocBtn.FocusEntered += () => _detailLabel.Text = detail;
        row.MouseEntered += () => _detailLabel.Text = detail;
        return row;
    }

    private static string BuildMasteryDetail(MasteryDef def, MasteryState? state)
    {
        int level = state?.Level ?? 0;
        string text = $"{def.Name}\n{def.Description}";
        if (level > 0 && state != null)
        {
            text += $"\nPassive: +{state.GetPassiveBonus(def.PassiveMultiplier):F1}% {def.PassiveType}";
            if (state.XpToNextLevel > 0) text += $"\nXP: {state.Xp} / {state.XpToNextLevel}";
        }
        return text;
    }

    // ─── Tab 4: Abilities ───────────────────────────────────────────────

    private void BuildAbilitiesTab()
    {
        _abilityAllocButtons.Clear();
        var tracker = GameState.Instance.Progression;
        var cls = GameState.Instance.SelectedClass;
        _pointsLabel.Text = $"AP: {tracker.AbilityPoints} available";
        _detailLabel.Text = "Select an ability to view details";

        var categories = SkillAbilityDatabase.GetCategories(cls);
        foreach (string catId in categories)
        {
            foreach (var mastery in SkillAbilityDatabase.GetMasteriesInCategory(catId))
            {
                var masteryState = tracker.GetMastery(mastery.Id);
                int masteryLevel = masteryState?.Level ?? 0;

                var header = new Label();
                header.Text = $"── {mastery.Name} (Lv.{masteryLevel}) ──";
                UiTheme.StyleLabel(header, masteryLevel > 0 ? UiTheme.Colors.Accent : UiTheme.Colors.Muted, UiTheme.FontSizes.Body);
                _tabs.ScrollContent.AddChild(header);

                foreach (var ability in SkillAbilityDatabase.GetAbilitiesForMastery(mastery.Id))
                {
                    var abilityState = tracker.GetAbility(ability.Id);
                    bool unlocked = tracker.IsUnlocked(ability.Id);
                    _tabs.ScrollContent.AddChild(CreateAbilityRow(ability, abilityState, tracker, unlocked, mastery.Name));
                }
            }
        }
    }

    private HBoxContainer CreateAbilityRow(AbilityDef def, AbilityState? state,
        ProgressionTracker tracker, bool unlocked, string parentName)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);
        int level = state?.Level ?? 0;

        var indent = new Control();
        indent.CustomMinimumSize = new Vector2(16, 0);
        row.AddChild(indent);

        var nameLabel = new Label();
        if (!unlocked)
        {
            nameLabel.Text = $"  {def.Name} (locked)";
            UiTheme.StyleLabel(nameLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Body);
        }
        else
        {
            nameLabel.Text = $"  {def.Name} [Lv.{level}]";
            UiTheme.StyleLabel(nameLabel, level > 0 ? UiTheme.Colors.Ink : UiTheme.Colors.Muted, UiTheme.FontSizes.Body);
        }
        nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(nameLabel);

        if (def.ManaCost > 0)
        {
            var costLabel = new Label();
            costLabel.Text = $"{def.ManaCost}MP";
            UiTheme.StyleLabel(costLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
            row.AddChild(costLabel);
        }

        var allocBtn = new Button();
        allocBtn.Text = "+";
        allocBtn.CustomMinimumSize = new Vector2(32, 28);
        allocBtn.FocusMode = FocusModeEnum.All;
        allocBtn.AddThemeFontSizeOverride("font_size", UiTheme.FontSizes.Small);
        allocBtn.Disabled = !unlocked || tracker.AbilityPoints <= 0;
        string capturedId = def.Id;
        allocBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(() =>
        {
            if (!tracker.AllocateAP(capturedId)) return;
            var s = tracker.GetAbility(capturedId);
            int lvl = s?.Level ?? 0;
            nameLabel.Text = $"  {def.Name} [Lv.{lvl}]";
            nameLabel.AddThemeColorOverride("font_color", lvl > 0 ? UiTheme.Colors.Ink : UiTheme.Colors.Muted);
            _pointsLabel.Text = $"AP: {tracker.AbilityPoints} available";
            if (tracker.AbilityPoints <= 0)
                foreach (var btn in _abilityAllocButtons) btn.Disabled = true;
        }));
        row.AddChild(allocBtn);
        _abilityAllocButtons.Add(allocBtn);

        if (unlocked && level >= 1)
        {
            var assignBtn = new Button();
            assignBtn.Text = "▶";
            assignBtn.CustomMinimumSize = new Vector2(28, 28);
            assignBtn.FocusMode = FocusModeEnum.All;
            UiTheme.StyleSecondaryButton(assignBtn, UiTheme.FontSizes.Small);
            string assignId = def.Id;
            string assignName = def.Name;
            assignBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(() =>
            {
                var actions = new (string label, System.Action action)[]
                {
                    ($"Assign to [1]", () => AssignToSlot(0, assignId, assignName)),
                    ($"Assign to [2]", () => AssignToSlot(1, assignId, assignName)),
                    ($"Assign to [3]", () => AssignToSlot(2, assignId, assignName)),
                    ($"Assign to [4]", () => AssignToSlot(3, assignId, assignName)),
                };
                ActionMenu.Instance?.Show(assignBtn.GlobalPosition, actions);
            }));
            row.AddChild(assignBtn);
        }

        string detail = BuildAbilityDetail(def, state, unlocked, parentName);
        allocBtn.FocusEntered += () => _detailLabel.Text = detail;
        row.MouseEntered += () => _detailLabel.Text = detail;
        return row;
    }

    private void AssignToSlot(int slot, string abilityId, string abilityName)
    {
        GameState.Instance.SkillHotbar.SetSlot(slot, abilityId);
        SkillBarHud.Instance?.RefreshDisplay();
        Toast.Instance?.Success($"{abilityName} → Slot [{slot + 1}]");
    }

    private static string BuildAbilityDetail(AbilityDef def, AbilityState? state, bool unlocked, string parentName)
    {
        if (!unlocked) return $"{def.Name}\nRequires {parentName} Lv.1";
        int level = state?.Level ?? 0;
        string text = $"{def.Name}\n{def.Description}";
        if (def.ManaCost > 0) text += $"\nCost: {def.ManaCost} MP | CD: {def.Cooldown:F1}s";
        if (level > 0 && state != null)
        {
            if (state.XpToNextLevel > 0) text += $"\nXP: {state.Xp} / {state.XpToNextLevel}";
            if (state.UseCount > 0) text += $"\nUses: {state.UseCount}";
        }
        return text;
    }

    // ─── Tab 5: Quests ──────────────────────────────────────────────────

    private void BuildQuestsTab()
    {
        _pointsLabel.Text = "";
        var tracker = GameState.Instance.Quests;

        if (tracker.ActiveQuests.Count == 0)
            tracker.GenerateQuests(GameState.Instance.FloorNumber);

        int complete = 0;
        for (int i = 0; i < tracker.ActiveQuests.Count; i++)
            if (tracker.ActiveQuests[i].IsComplete) complete++;
        _detailLabel.Text = $"{complete}/{tracker.ActiveQuests.Count} quests completed";

        for (int i = 0; i < tracker.ActiveQuests.Count && i < tracker.QuestDefs.Count; i++)
        {
            var def = tracker.QuestDefs[i];
            var state = tracker.ActiveQuests[i];

            var questBox = new VBoxContainer();
            questBox.AddThemeConstantOverride("separation", 4);

            var titleRow = new HBoxContainer();
            var titleLabel = new Label();
            titleLabel.Text = def.Title;
            UiTheme.StyleLabel(titleLabel, state.IsComplete ? UiTheme.Colors.Safe : UiTheme.Colors.Ink, UiTheme.FontSizes.Body);
            titleLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            titleRow.AddChild(titleLabel);

            if (state.IsComplete)
            {
                var claimBtn = new Button();
                claimBtn.Text = Strings.Quests.Claim;
                claimBtn.CustomMinimumSize = new Vector2(80, 28);
                claimBtn.FocusMode = FocusModeEnum.All;
                UiTheme.StyleButton(claimBtn, UiTheme.FontSizes.Small);
                int idx = i;
                claimBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(() =>
                {
                    var d = tracker.QuestDefs[idx];
                    GameState.Instance.PlayerInventory.Gold += d.GoldReward;
                    GameState.Instance.AwardXp(d.XpReward);
                    Toast.Instance?.Success($"Quest complete! +{d.GoldReward}g +{d.XpReward}XP");
                    tracker.GenerateQuests(GameState.Instance.FloorNumber);
                    _tabs.SelectTab(_tabs.CurrentTab);
                }));
                titleRow.AddChild(claimBtn);
            }
            questBox.AddChild(titleRow);

            var descLabel = new Label();
            descLabel.Text = def.Description;
            UiTheme.StyleLabel(descLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
            descLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            questBox.AddChild(descLabel);

            if (!state.IsComplete && def.TargetCount > 1)
            {
                var progressLabel = new Label();
                progressLabel.Text = $"Progress: {state.Progress}/{def.TargetCount}";
                UiTheme.StyleLabel(progressLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Small);
                questBox.AddChild(progressLabel);
            }

            var rewardLabel = new Label();
            rewardLabel.Text = $"Reward: {def.GoldReward}g + {def.XpReward} XP";
            UiTheme.StyleLabel(rewardLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Small);
            questBox.AddChild(rewardLabel);

            _tabs.ScrollContent.AddChild(questBox);

            if (i < tracker.ActiveQuests.Count - 1)
                _tabs.ScrollContent.AddChild(new HSeparator());
        }
    }

    // ─── Tab 6: Ledger ──────────────────────────────────────────────────

    private void BuildLedgerTab()
    {
        _pointsLabel.Text = "";
        var tracker = GameState.Instance.Achievements;
        var allDefs = AchievementTracker.GetAll();

        int unlocked = 0;
        foreach (var def in allDefs)
            if (tracker.IsUnlocked(def.Id)) unlocked++;
        _detailLabel.Text = $"{unlocked}/{allDefs.Count} achievements unlocked";

        AchievementCategory[] categories = {
            AchievementCategory.Combat, AchievementCategory.Exploration,
            AchievementCategory.Progression, AchievementCategory.Economy,
            AchievementCategory.Mastery
        };
        foreach (var cat in categories)
        {
            var catLabel = new Label();
            catLabel.Text = $"── {cat} ──";
            UiTheme.StyleLabel(catLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Body);
            catLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _tabs.ScrollContent.AddChild(catLabel);

            foreach (var def in AchievementTracker.GetByCategory(cat))
            {
                bool isUnlocked = tracker.IsUnlocked(def.Id);

                var row = new VBoxContainer();
                row.AddThemeConstantOverride("separation", 2);

                var nameLabel = new Label();
                nameLabel.Text = (isUnlocked ? "✓ " : "  ") + def.Name;
                UiTheme.StyleLabel(nameLabel, isUnlocked ? UiTheme.Colors.Safe : UiTheme.Colors.Ink, UiTheme.FontSizes.Body);
                row.AddChild(nameLabel);

                var descLabel = new Label();
                descLabel.Text = def.Description;
                UiTheme.StyleLabel(descLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
                row.AddChild(descLabel);

                if (!isUnlocked)
                {
                    float pct = tracker.GetProgress(def) * 100;
                    var progressLabel = new Label();
                    progressLabel.Text = $"{pct:F0}%";
                    UiTheme.StyleLabel(progressLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
                    row.AddChild(progressLabel);
                }

                _tabs.ScrollContent.AddChild(row);
            }
        }
    }

    // ─── Tab 7: Stats ───────────────────────────────────────────────────

    private void BuildStatsTab()
    {
        _detailLabel.Text = "";
        var gs = GameState.Instance;
        var stats = gs.Stats;
        _pointsLabel.Text = $"Free points: {stats.FreePoints}";

        AddStatRow("STR", stats.Str, "Melee damage bonus", stats, s => s.Str++);
        AddStatRow("DEX", stats.Dex, "Attack speed & dodge", stats, s => s.Dex++);
        AddStatRow("STA", stats.Sta, "Max HP & HP regen", stats, s => s.Sta++);
        AddStatRow("INT", stats.Int, "Spell damage & max mana", stats, s => s.Int++);

        _tabs.ScrollContent.AddChild(new HSeparator());

        var derivedLabel = new Label();
        derivedLabel.Text =
            $"Melee+: {stats.MeleeFlatBonus:F0}  Atk spd: {stats.AttackSpeedMultiplier:F2}x\n" +
            $"Dodge: {stats.DodgeChance * 100:F1}%  HP regen: {stats.HpRegen:F1}/s\n" +
            $"Spell dmg: {stats.SpellDamageMultiplier:F2}x";
        UiTheme.StyleLabel(derivedLabel, UiTheme.Colors.Info, UiTheme.FontSizes.Small);
        _tabs.ScrollContent.AddChild(derivedLabel);
    }

    private void AddStatRow(string name, int value, string desc, StatBlock stats, System.Action<StatBlock> increment)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);

        var nameLabel = new Label();
        nameLabel.Text = $"{name}: {value}";
        nameLabel.CustomMinimumSize = new Vector2(80, 0);
        UiTheme.StyleLabel(nameLabel, UiTheme.Colors.Ink, UiTheme.FontSizes.Button);
        row.AddChild(nameLabel);

        var effLabel = new Label();
        effLabel.Text = $"({StatBlock.GetEffective(value):F0} eff)";
        effLabel.CustomMinimumSize = new Vector2(70, 0);
        UiTheme.StyleLabel(effLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        row.AddChild(effLabel);

        var descLabel = new Label();
        descLabel.Text = desc;
        UiTheme.StyleLabel(descLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        descLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(descLabel);

        var addBtn = new Button();
        addBtn.Text = "+";
        addBtn.CustomMinimumSize = new Vector2(36, 36);
        addBtn.FocusMode = FocusModeEnum.All;
        addBtn.AddThemeFontSizeOverride("font_size", UiTheme.FontSizes.Small);
        addBtn.Disabled = stats.FreePoints <= 0;
        addBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(() =>
        {
            if (stats.FreePoints <= 0) return;
            stats.FreePoints--;
            increment(stats);
            var gs = GameState.Instance;
            gs.MaxHp = Constants.PlayerStats.GetMaxHp(gs.Level) + gs.Stats.BonusMaxHp;
            gs.EmitSignal(GameState.SignalName.StatsChanged);
            _tabs.SelectTab(_tabs.CurrentTab);
        }));
        row.AddChild(addBtn);

        _tabs.ScrollContent.AddChild(row);
    }

    // ─── Tab 8: System ──────────────────────────────────────────────────

    private void BuildSystemTab()
    {
        _pointsLabel.Text = "";
        _detailLabel.Text = "";

        AddSystemButton("Tutorial", () =>
        {
            TutorialPanel.Open(GetParent());
        });

        AddSystemButton("Settings", () =>
        {
            SettingsPanel.Open(GetParent());
        });

        _tabs.ScrollContent.AddChild(new HSeparator());

        AddSystemButton("Back to Main Menu", () =>
        {
            Close();
            // ?? false — a missing SaveManager autoload also counts as failure;
            // we never want to silently bypass the toast and lose progress (Copilot
            // R2 finding on PR #16).
            bool ok = SaveManager.Instance?.Save() ?? false;
            if (ok)
            {
                GetTree().ReloadCurrentScene();
                return;
            }
            // Save failed: surface the toast and defer the reload long enough for
            // the player to read it. Toast is mounted under Main's scene tree, so
            // calling ReloadCurrentScene immediately would tear it down before
            // it ever rendered (Copilot R1 finding on PR #16).
            Toast.Instance?.Error("Save failed — progress may be lost");
            var t = GetTree().CreateTimer(3.0);
            t.Connect(SceneTreeTimer.SignalName.Timeout,
                Callable.From(() => GetTree().ReloadCurrentScene()));
        });

        var quitBtn = new Button();
        quitBtn.Text = "Quit Game";
        quitBtn.CustomMinimumSize = new Vector2(280, 38);
        quitBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        UiTheme.StyleDangerButton(quitBtn, UiTheme.FontSizes.Body);
        quitBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(() => GetTree().Quit()));
        _tabs.ScrollContent.AddChild(quitBtn);
    }

    private void AddSystemButton(string text, System.Action action)
    {
        var btn = new Button();
        btn.Text = text;
        btn.CustomMinimumSize = new Vector2(280, 38);
        btn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        UiTheme.StyleButton(btn, UiTheme.FontSizes.Body);
        btn.Connect(BaseButton.SignalName.Pressed, Callable.From(action));
        _tabs.ScrollContent.AddChild(btn);
    }
}
