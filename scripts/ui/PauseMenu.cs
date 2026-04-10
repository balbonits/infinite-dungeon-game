using Godot;
using System.Linq;

/// <summary>
/// Full game window opened by Esc/Start. Tabbed panel with:
/// Inventory, Equipment, Stats, Skills, Settings, Pause.
/// Uses CenterContainer for reliable centering.
/// ProcessMode = Always so it works while tree is paused.
/// </summary>
public partial class PauseMenu : Control
{
    private static readonly Color PanelBg = new(0.086f, 0.106f, 0.157f, 0.92f);
    private static readonly Color BorderColor = new(0.961f, 0.784f, 0.420f, 0.3f);
    private static readonly Color TitleColor = new(0.961f, 0.784f, 0.420f);
    private static readonly Color BodyColor = new(0.925f, 0.941f, 1.0f);
    private static readonly Color MutedColor = new(0.714f, 0.749f, 0.859f);
    private static readonly Color ButtonBg = new(0.12f, 0.14f, 0.20f, 0.9f);
    private static readonly Color ButtonHover = new(0.18f, 0.20f, 0.28f, 0.9f);
    private static readonly Color GreenColor = new(0.42f, 1.0f, 0.54f);
    private static readonly Color RedColor = new(0.9f, 0.3f, 0.3f);

    private const float WindowW = 700f;
    private const float WindowH = 500f;

    private TabContainer _tabs;
    private Label _statusLabel;

    // Tabs that need refreshing
    private VBoxContainer _inventoryList;
    private VBoxContainer _equipList;
    private Label _statsContent;
    private Label _skillsContent;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Visible = false;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

        // Dark overlay
        var overlay = new ColorRect();
        overlay.Color = new Color(0, 0, 0, 0.6f);
        overlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        overlay.MouseFilter = MouseFilterEnum.Stop;
        AddChild(overlay);

        // CenterContainer for reliable centering
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        center.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(center);

        // Main panel
        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(WindowW, WindowH);
        var style = new StyleBoxFlat();
        style.BgColor = PanelBg;
        style.BorderColor = BorderColor;
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(8);
        style.SetContentMarginAll(8);
        panel.AddThemeStyleboxOverride("panel", style);
        center.AddChild(panel);

        // Vertical layout: tabs + status bar
        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);
        panel.AddChild(vbox);

        // Tab container
        _tabs = new TabContainer();
        _tabs.SizeFlagsVertical = SizeFlags.ExpandFill;

        // Style the tab bar
        var tabPanelStyle = new StyleBoxFlat();
        tabPanelStyle.BgColor = new Color(0.06f, 0.07f, 0.10f, 0.95f);
        tabPanelStyle.SetCornerRadiusAll(4);
        tabPanelStyle.SetContentMarginAll(12);
        _tabs.AddThemeStyleboxOverride("panel", tabPanelStyle);
        _tabs.AddThemeColorOverride("font_selected_color", TitleColor);
        _tabs.AddThemeColorOverride("font_unselected_color", MutedColor);
        _tabs.AddThemeFontSizeOverride("font_size", 14);

        vbox.AddChild(_tabs);

        // Build each tab
        _tabs.AddChild(BuildInventoryTab());
        _tabs.AddChild(BuildEquipmentTab());
        _tabs.AddChild(BuildStatsTab());
        _tabs.AddChild(BuildSkillsTab());
        _tabs.AddChild(BuildSettingsTab());
        _tabs.AddChild(BuildPauseTab());

        // Status bar at bottom
        _statusLabel = new Label();
        _statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _statusLabel.AddThemeColorOverride("font_color", TitleColor);
        _statusLabel.AddThemeFontSizeOverride("font_size", 11);
        vbox.AddChild(_statusLabel);

        // Tab change refreshes content
        _tabs.TabChanged += (_) => RefreshCurrentTab();
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (ev.IsActionPressed("start"))
        {
            Toggle();
            GetViewport().SetInputAsHandled();
        }
    }

    public void Toggle()
    {
        Visible = !Visible;
        GetTree().Paused = Visible;
        if (Visible)
            RefreshCurrentTab();
        else
            _statusLabel.Text = "";
    }

    private void RefreshCurrentTab()
    {
        int tab = _tabs.CurrentTab;
        string tabName = _tabs.GetTabTitle(tab);
        if (tabName == "Inventory") RefreshInventory();
        else if (tabName == "Equipment") RefreshEquipment();
        else if (tabName == "Stats") RefreshStats();
        else if (tabName == "Skills") RefreshSkills();
    }

    // ═══════════════════════════════════════════════════
    // INVENTORY TAB
    // ═══════════════════════════════════════════════════
    private Control BuildInventoryTab()
    {
        var tab = new MarginContainer();
        tab.Name = "Inventory";
        tab.AddThemeConstantOverride("margin_left", 4);
        tab.AddThemeConstantOverride("margin_right", 4);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);
        tab.AddChild(vbox);

        var header = MakeLabel($"Backpack (0/{GameState.Player.InventorySize})", 14, TitleColor);
        header.Name = "Header";
        vbox.AddChild(header);

        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        vbox.AddChild(scroll);

        _inventoryList = new VBoxContainer();
        _inventoryList.AddThemeConstantOverride("separation", 2);
        _inventoryList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(_inventoryList);

        return tab;
    }

    private void RefreshInventory()
    {
        foreach (var c in _inventoryList.GetChildren()) c.QueueFree();

        var p = GameState.Player;
        var header = _tabs.GetChild(0).GetNode<Label>("Header") as Label;
        if (header != null) header.Text = $"Backpack ({p.Inventory.Count}/{p.InventorySize})";

        if (p.Inventory.Count == 0)
        {
            _inventoryList.AddChild(MakeLabel("  (empty)", 12, MutedColor));
            return;
        }

        foreach (var item in p.Inventory)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);

            var name = MakeLabel(item.Name, 12, BodyColor);
            name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(name);

            var info = item.Type == ItemType.Consumable
                ? $"x{item.StackCount}"
                : $"{item.Quality} {item.Type}";
            row.AddChild(MakeLabel(info, 11, MutedColor));

            if (item.Type == ItemType.Weapon || item.Type == ItemType.Armor || item.Type == ItemType.Accessory)
            {
                var equipBtn = MakeSmallButton("Equip");
                var capturedItem = item;
                equipBtn.Pressed += () => {
                    GameSystems.EquipItem(capturedItem);
                    _statusLabel.Text = $"Equipped {capturedItem.Name}";
                    RefreshInventory();
                    RefreshEquipment();
                };
                row.AddChild(equipBtn);
            }
            else if (item.Type == ItemType.Consumable)
            {
                var useBtn = MakeSmallButton("Use");
                var capturedItem = item;
                useBtn.Pressed += () => {
                    var (ok, msg) = GameSystems.UseItem(capturedItem);
                    _statusLabel.Text = msg;
                    RefreshInventory();
                };
                row.AddChild(useBtn);
            }

            _inventoryList.AddChild(row);
        }
    }

    // ═══════════════════════════════════════════════════
    // EQUIPMENT TAB
    // ═══════════════════════════════════════════════════
    private Control BuildEquipmentTab()
    {
        var tab = new MarginContainer();
        tab.Name = "Equipment";
        tab.AddThemeConstantOverride("margin_left", 4);
        tab.AddThemeConstantOverride("margin_right", 4);

        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        tab.AddChild(scroll);

        _equipList = new VBoxContainer();
        _equipList.AddThemeConstantOverride("separation", 4);
        _equipList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(_equipList);

        return tab;
    }

    private void RefreshEquipment()
    {
        foreach (var c in _equipList.GetChildren()) c.QueueFree();

        var slots = new[] { EquipSlot.MainHand, EquipSlot.OffHand, EquipSlot.Head,
                            EquipSlot.Body, EquipSlot.Legs, EquipSlot.Feet, EquipSlot.Ring };

        foreach (var slot in slots)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);

            var slotLabel = MakeLabel($"[{slot}]", 12, MutedColor);
            slotLabel.CustomMinimumSize = new Vector2(100, 0);
            row.AddChild(slotLabel);

            if (GameState.Player.Equipment.TryGetValue(slot, out var item))
            {
                var name = MakeLabel(item.Name, 12, BodyColor);
                name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                row.AddChild(name);

                var stats = "";
                if (item.Damage > 0) stats += $"+{item.Damage} Dmg ";
                if (item.Defense > 0) stats += $"+{item.Defense} Def ";
                if (item.HPBonus > 0) stats += $"+{item.HPBonus} HP ";
                if (item.MPBonus > 0) stats += $"+{item.MPBonus} MP ";
                row.AddChild(MakeLabel(stats.Trim(), 11, GreenColor));

                var unequipBtn = MakeSmallButton("Unequip");
                var capturedSlot = slot;
                unequipBtn.Pressed += () => {
                    GameSystems.UnequipItem(capturedSlot);
                    _statusLabel.Text = $"Unequipped {item.Name}";
                    RefreshEquipment();
                    RefreshInventory();
                };
                row.AddChild(unequipBtn);
            }
            else
            {
                var empty = MakeLabel("(empty)", 12, new Color(0.4f, 0.4f, 0.5f));
                empty.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                row.AddChild(empty);
            }

            _equipList.AddChild(row);
        }

        // Summary
        var summary = new HBoxContainer();
        summary.AddThemeConstantOverride("separation", 16);
        summary.AddChild(MakeLabel($"Total Damage: {GameState.Player.TotalDamage}", 12, RedColor));
        summary.AddChild(MakeLabel($"Total Defense: {GameState.Player.TotalDefense}", 12, GreenColor));
        _equipList.AddChild(summary);
    }

    // ═══════════════════════════════════════════════════
    // STATS TAB
    // ═══════════════════════════════════════════════════
    private Control BuildStatsTab()
    {
        var tab = new MarginContainer();
        tab.Name = "Stats";
        tab.AddThemeConstantOverride("margin_left", 8);
        tab.AddThemeConstantOverride("margin_right", 8);

        _statsContent = MakeLabel("", 13, BodyColor);
        tab.AddChild(_statsContent);

        return tab;
    }

    private void RefreshStats()
    {
        var p = GameState.Player;
        float defDR = p.TotalDefense * (100f / (p.TotalDefense + 100f));

        _statsContent.Text =
            $"{p.Name}  —  Level {p.Level}\n" +
            $"XP: {p.XP} / {p.XPToNextLevel}    Stat Points: {p.StatPoints}\n\n" +
            $"HP: {p.HP} / {p.MaxHP}\n" +
            $"MP: {p.MP} / {p.MaxMP}\n\n" +
            $"STR: {p.STR}    (melee damage scaling)\n" +
            $"DEX: {p.DEX}    (attack speed, crit)\n" +
            $"INT: {p.INT}    (spell power, max MP)\n" +
            $"VIT: {p.VIT}    (max HP, regen)\n\n" +
            $"Damage: {p.TotalDamage}    Defense: {p.TotalDefense} ({defDR:F1}% DR)\n" +
            $"Gold: {p.Gold}    Kills: {GameState.ActiveMonsters.Count(m => m.IsDead)}\n\n";

        if (p.StatPoints > 0)
            _statsContent.Text += $"[{p.StatPoints} stat points available — allocate in a future update]";
    }

    // ═══════════════════════════════════════════════════
    // SKILLS TAB
    // ═══════════════════════════════════════════════════
    private Control BuildSkillsTab()
    {
        var tab = new MarginContainer();
        tab.Name = "Skills";
        tab.AddThemeConstantOverride("margin_left", 8);
        tab.AddThemeConstantOverride("margin_right", 8);

        _skillsContent = MakeLabel("", 13, BodyColor);
        tab.AddChild(_skillsContent);

        return tab;
    }

    private void RefreshSkills()
    {
        var p = GameState.Player;
        string text = $"Skill Points: {p.SkillPoints}\n\n";

        if (GameState.PlayerSkills.Count == 0)
        {
            text += "No skills learned yet.\n\n" +
                    "Skills are learned through use — the more you\n" +
                    "practice an action, the better you get at it.\n\n" +
                    "(Skill system coming in a future update)";
        }
        else
        {
            foreach (var skill in GameState.PlayerSkills)
            {
                text += $"{skill.Name}  Lv.{skill.Level}\n" +
                        $"  {skill.Description}\n" +
                        $"  Mana: {skill.ManaCost}  Cooldown: {skill.Cooldown:F1}s  Damage: {skill.BaseDamage}\n\n";
            }
        }

        _skillsContent.Text = text;
    }

    // ═══════════════════════════════════════════════════
    // SETTINGS TAB
    // ═══════════════════════════════════════════════════
    private Control BuildSettingsTab()
    {
        var tab = new MarginContainer();
        tab.Name = "Settings";
        tab.AddThemeConstantOverride("margin_left", 8);
        tab.AddThemeConstantOverride("margin_right", 8);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 12);
        tab.AddChild(vbox);

        // Target priority
        vbox.AddChild(MakeLabel("Target Priority", 14, TitleColor));
        var priorityBox = new HBoxContainer();
        priorityBox.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(priorityBox);

        foreach (TargetPriority tp in System.Enum.GetValues<TargetPriority>())
        {
            var btn = MakeSmallButton(tp.ToString());
            var captured = tp;
            btn.Pressed += () => {
                GameState.Settings.TargetMode = captured;
                _statusLabel.Text = $"Target priority: {captured}";
            };
            priorityBox.AddChild(btn);
        }

        // Music/SFX placeholders
        vbox.AddChild(MakeLabel("Audio", 14, TitleColor));
        vbox.AddChild(MakeLabel("  Music and SFX controls coming soon", 12, MutedColor));

        // Controls
        vbox.AddChild(MakeLabel("Controls", 14, TitleColor));
        vbox.AddChild(MakeLabel("  Key rebinding coming soon", 12, MutedColor));

        return tab;
    }

    // ═══════════════════════════════════════════════════
    // PAUSE TAB (Resume / Save / Exit)
    // ═══════════════════════════════════════════════════
    private Control BuildPauseTab()
    {
        var tab = new MarginContainer();
        tab.Name = "Game";
        tab.AddThemeConstantOverride("margin_left", 8);
        tab.AddThemeConstantOverride("margin_right", 8);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 16);
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        tab.AddChild(vbox);

        var resumeBtn = MakeMenuButton("Resume");
        resumeBtn.Pressed += () => Toggle();
        vbox.AddChild(resumeBtn);

        var saveBtn = MakeMenuButton("Save Game");
        saveBtn.Pressed += () => {
            bool ok = SaveSystem.SaveToSlot(1);
            _statusLabel.Text = ok ? "Game saved!" : "Save failed";
        };
        vbox.AddChild(saveBtn);

        var exitBtn = MakeMenuButton("Exit to Menu");
        exitBtn.Pressed += () => {
            SaveSystem.SaveToSlot(1);
            GetTree().Paused = false;
            Visible = false;
            SceneManager.Instance.GoToMainMenu();
        };
        vbox.AddChild(exitBtn);

        return tab;
    }

    // ═══════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════
    private static Label MakeLabel(string text, int size, Color color)
    {
        var lbl = new Label();
        lbl.Text = text;
        lbl.AddThemeFontSizeOverride("font_size", size);
        lbl.AddThemeColorOverride("font_color", color);
        return lbl;
    }

    private Button MakeSmallButton(string text)
    {
        var btn = new Button();
        btn.Text = text;
        btn.CustomMinimumSize = new Vector2(70, 28);

        var s = new StyleBoxFlat();
        s.BgColor = ButtonBg;
        s.BorderColor = BorderColor;
        s.SetBorderWidthAll(1);
        s.SetCornerRadiusAll(3);
        s.SetContentMarginAll(4);
        btn.AddThemeStyleboxOverride("normal", s);

        var h = s.Duplicate() as StyleBoxFlat;
        h.BorderColor = TitleColor;
        h.BgColor = ButtonHover;
        btn.AddThemeStyleboxOverride("hover", h);
        btn.AddThemeStyleboxOverride("pressed", h);

        btn.AddThemeColorOverride("font_color", BodyColor);
        btn.AddThemeColorOverride("font_hover_color", TitleColor);
        btn.AddThemeFontSizeOverride("font_size", 11);
        return btn;
    }

    private Button MakeMenuButton(string text)
    {
        var btn = new Button();
        btn.Text = text;
        btn.CustomMinimumSize = new Vector2(200, 42);

        var s = new StyleBoxFlat();
        s.BgColor = ButtonBg;
        s.BorderColor = BorderColor;
        s.SetBorderWidthAll(1);
        s.SetCornerRadiusAll(6);
        s.SetContentMarginAll(8);
        btn.AddThemeStyleboxOverride("normal", s);

        var h = s.Duplicate() as StyleBoxFlat;
        h.BorderColor = TitleColor;
        h.BgColor = ButtonHover;
        btn.AddThemeStyleboxOverride("hover", h);
        btn.AddThemeStyleboxOverride("pressed", h);

        btn.AddThemeColorOverride("font_color", BodyColor);
        btn.AddThemeColorOverride("font_hover_color", TitleColor);
        btn.AddThemeFontSizeOverride("font_size", 15);
        return btn;
    }
}
