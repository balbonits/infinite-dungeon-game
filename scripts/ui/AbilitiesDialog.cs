using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Abilities tab — class-specific dialog showing active combat actions.
/// Players spend AP and assign abilities to the hotbar.
/// Tab name changes per class: Warrior Arts / Ranger Crafts / Arcane Spells.
/// </summary>
public partial class AbilitiesDialog : Control
{
    public static AbilitiesDialog Instance { get; private set; } = null!;

    private ColorRect _overlay = null!;
    private Label _titleLabel = null!;
    private Label _pointsLabel = null!;
    private Label _detailLabel = null!;
    private HBoxContainer _tabBar = null!;
    private ScrollContainer _scrollContainer = null!;
    private VBoxContainer _abilityList = null!;
    private bool _isOpen;

    private string[] _categories = System.Array.Empty<string>();
    private Button[] _tabButtons = System.Array.Empty<Button>();
    private readonly System.Collections.Generic.List<Button> _allocButtons = new();
    private int _currentTab;

    public bool IsOpen => _isOpen;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
        MouseFilter = MouseFilterEnum.Ignore;
        BuildUi();
    }

    private static string GetTabName(PlayerClass cls) => cls switch
    {
        PlayerClass.Warrior => "WARRIOR ARTS",
        PlayerClass.Ranger => "RANGER CRAFTS",
        PlayerClass.Mage => "ARCANE SPELLS",
        _ => "ABILITIES",
    };

    private void BuildUi()
    {
        var (overlay, content) = UiTheme.CreateDialogWindow(440f);
        _overlay = overlay;
        _overlay.Visible = false;
        AddChild(_overlay);

        _titleLabel = new Label();
        UiTheme.StyleLabel(_titleLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        _titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(_titleLabel);

        _pointsLabel = new Label();
        UiTheme.StyleLabel(_pointsLabel, UiTheme.Colors.Safe, UiTheme.FontSizes.Body);
        _pointsLabel.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(_pointsLabel);

        _tabBar = new HBoxContainer();
        _tabBar.AddThemeConstantOverride("separation", 0);
        _tabBar.Alignment = BoxContainer.AlignmentMode.Center;
        content.AddChild(_tabBar);

        var tabHint = new Label();
        string lKey = GetActionKeyName(Constants.InputActions.ShoulderLeft);
        string rKey = GetActionKeyName(Constants.InputActions.ShoulderRight);
        tabHint.Text = $"[{lKey}] / [{rKey}] switch tabs | [D] close";
        UiTheme.StyleLabel(tabHint, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        tabHint.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(tabHint);

        content.AddChild(new HSeparator());

        _detailLabel = new Label();
        UiTheme.StyleLabel(_detailLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        _detailLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _detailLabel.CustomMinimumSize = new Vector2(0, 48);
        content.AddChild(_detailLabel);

        _scrollContainer = new ScrollContainer();
        _scrollContainer.CustomMinimumSize = new Vector2(0, 300);
        content.AddChild(_scrollContainer);

        _abilityList = new VBoxContainer();
        _abilityList.AddThemeConstantOverride("separation", 4);
        _scrollContainer.AddChild(_abilityList);
    }

    public new void Show()
    {
        if (_isOpen) return;
        _isOpen = true;
        WindowStack.Push(this);
        GetTree().Paused = true;

        var cls = GameState.Instance.SelectedClass;
        _titleLabel.Text = GetTabName(cls);
        _categories = SkillAbilityDatabase.GetCategories(cls);
        RebuildTabs();
        BuildTab(0);

        _overlay.Visible = true;
    }

    public void Close()
    {
        _isOpen = false;
        WindowStack.Pop(this);
        _overlay.Visible = false;
        var pauseMenu = GetNodeOrNull<Control>("../PauseMenu");
        if (pauseMenu != null)
        {
            pauseMenu.Visible = true;
            UiTheme.FocusFirstButton(pauseMenu.GetNode<VBoxContainer>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer"));
        }
        else
        {
            GetTree().Paused = false;
        }
    }

    private void RebuildTabs()
    {
        foreach (Node child in _tabBar.GetChildren())
            child.QueueFree();

        _tabButtons = new Button[_categories.Length];
        for (int i = 0; i < _categories.Length; i++)
        {
            var btn = new Button();
            btn.Text = SkillAbilityDatabase.GetCategoryName(_categories[i]);
            btn.CustomMinimumSize = new Vector2(0, 28);
            btn.FocusMode = FocusModeEnum.None;
            int idx = i;
            btn.Connect(BaseButton.SignalName.Pressed, Callable.From(() => BuildTab(idx)));
            _tabBar.AddChild(btn);
            _tabButtons[i] = btn;
        }
    }

    private void BuildTab(int tabIndex)
    {
        _currentTab = tabIndex;

        for (int i = 0; i < _tabButtons.Length; i++)
        {
            bool active = i == tabIndex;
            _tabButtons[i].AddThemeStyleboxOverride("normal", CreateTabStyle(active));
            _tabButtons[i].AddThemeStyleboxOverride("hover", CreateTabStyle(active));
            _tabButtons[i].AddThemeStyleboxOverride("focus", CreateTabStyle(active));
            _tabButtons[i].AddThemeColorOverride("font_color", active ? UiTheme.Colors.BgDark : UiTheme.Colors.Muted);
            _tabButtons[i].AddThemeColorOverride("font_hover_color", active ? UiTheme.Colors.BgDark : UiTheme.Colors.Ink);
            _tabButtons[i].AddThemeFontSizeOverride("font_size", UiTheme.FontSizes.Small);
        }

        RefreshAbilityList();
    }

    private void RefreshAbilityList()
    {
        _allocButtons.Clear();
        foreach (Node child in _abilityList.GetChildren())
            child.QueueFree();

        var tracker = GameState.Instance.Progression;
        _pointsLabel.Text = $"AP: {tracker.AbilityPoints} available";
        _detailLabel.Text = "Select an ability to view details";

        if (_currentTab >= _categories.Length) return;
        string catId = _categories[_currentTab];

        // Group abilities by parent mastery
        foreach (var mastery in SkillAbilityDatabase.GetMasteriesInCategory(catId))
        {
            var masteryState = tracker.GetMastery(mastery.Id);
            int masteryLevel = masteryState?.Level ?? 0;

            // Mastery header
            var header = new Label();
            header.Text = $"── {mastery.Name} (Lv.{masteryLevel}) ──";
            Color headerColor = masteryLevel > 0 ? UiTheme.Colors.Accent : UiTheme.Colors.Muted;
            UiTheme.StyleLabel(header, headerColor, UiTheme.FontSizes.Body);
            _abilityList.AddChild(header);

            // Abilities under this mastery
            foreach (var ability in SkillAbilityDatabase.GetAbilitiesForMastery(mastery.Id))
            {
                var abilityState = tracker.GetAbility(ability.Id);
                bool unlocked = tracker.IsUnlocked(ability.Id);
                var row = CreateAbilityRow(ability, abilityState, tracker, unlocked, mastery.Name);
                _abilityList.AddChild(row);
            }
        }

        _scrollContainer.ScrollVertical = 0;
        CallDeferred(MethodName.FocusFirst);
    }

    private void FocusFirst()
    {
        UiTheme.FocusFirstButton(_abilityList);
    }



    private HBoxContainer CreateAbilityRow(AbilityDef def, AbilityState? state,
        ProgressionTracker tracker, bool unlocked, string parentName)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);

        int level = state?.Level ?? 0;

        // Indent
        var indent = new Control();
        indent.CustomMinimumSize = new Vector2(16, 0);
        row.AddChild(indent);

        // Name + level (or locked state)
        var nameLabel = new Label();
        if (!unlocked)
        {
            nameLabel.Text = $"  🔒 {def.Name}";
            UiTheme.StyleLabel(nameLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Body);
        }
        else
        {
            nameLabel.Text = $"  {def.Name} [Lv.{level}]";
            Color nameColor = level > 0 ? UiTheme.Colors.Ink : UiTheme.Colors.Muted;
            UiTheme.StyleLabel(nameLabel, nameColor, UiTheme.FontSizes.Body);
        }
        nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(nameLabel);

        // Mana cost
        Label? costLabelRef = null;
        if (def.ManaCost > 0)
        {
            costLabelRef = new Label();
            costLabelRef.Text = $"{def.ManaCost}MP";
            UiTheme.StyleLabel(costLabelRef, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
            row.AddChild(costLabelRef);
        }

        // [+] AP allocate button
        var allocBtn = new Button();
        allocBtn.Text = "+";
        allocBtn.CustomMinimumSize = new Vector2(32, 28);
        allocBtn.FocusMode = FocusModeEnum.All;
        UiTheme.StyleButton(allocBtn, UiTheme.FontSizes.Small);
        allocBtn.Disabled = !unlocked || tracker.AbilityPoints <= 0;
        string capturedId = def.Id;
        string capturedParent = parentName;
        allocBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(() =>
        {
            if (!tracker.AllocateAP(capturedId)) return;

            // Update this row's labels in-place
            var s = tracker.GetAbility(capturedId);
            int lvl = s?.Level ?? 0;
            nameLabel.Text = $"  {def.Name} [Lv.{lvl}]";
            nameLabel.AddThemeColorOverride("font_color", lvl > 0 ? UiTheme.Colors.Ink : UiTheme.Colors.Muted);

            _pointsLabel.Text = $"AP: {tracker.AbilityPoints} available";
            _detailLabel.Text = BuildAbilityDetail(def, s, true, capturedParent);

            if (tracker.AbilityPoints <= 0)
                foreach (var btn in _allocButtons)
                    btn.Disabled = true;
        }));
        row.AddChild(allocBtn);
        _allocButtons.Add(allocBtn);

        // [▶] Hotbar assign button (only if unlocked and leveled)
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
                ShowAssignMenu(assignId, assignName, assignBtn.GlobalPosition);
            }));
            row.AddChild(assignBtn);
        }

        // Detail on focus/hover
        string detail = BuildAbilityDetail(def, state, unlocked, parentName);
        allocBtn.FocusEntered += () => _detailLabel.Text = detail;
        row.MouseEntered += () => _detailLabel.Text = detail;

        return row;
    }

    private static string BuildAbilityDetail(AbilityDef def, AbilityState? state,
        bool unlocked, string parentName)
    {
        if (!unlocked)
            return $"{def.Name}\nRequires {parentName} Lv.1";

        int level = state?.Level ?? 0;
        string text = $"{def.Name}\n{def.Description}";

        if (def.ManaCost > 0)
            text += $"\nCost: {def.ManaCost} MP | CD: {def.Cooldown:F1}s";

        if (level > 0 && state != null)
        {
            int xpNeeded = state.XpToNextLevel;
            if (xpNeeded > 0)
                text += $"\nXP: {state.Xp} / {xpNeeded}";

            if (state.UseCount > 0)
            {
                string tier = state.AffinityTier switch
                {
                    4 => " (Mastered)",
                    3 => " (Expert)",
                    2 => " (Practiced)",
                    1 => " (Familiar)",
                    _ => "",
                };
                text += $"\nUses: {state.UseCount}{tier}";
            }
        }

        return text;
    }

    private void ShowAssignMenu(string abilityId, string abilityName, Vector2 position)
    {
        var actions = new (string label, System.Action action)[]
        {
            ($"Assign to [1]", () => AssignToSlot(0, abilityId, abilityName)),
            ($"Assign to [2]", () => AssignToSlot(1, abilityId, abilityName)),
            ($"Assign to [3]", () => AssignToSlot(2, abilityId, abilityName)),
            ($"Assign to [4]", () => AssignToSlot(3, abilityId, abilityName)),
        };
        ActionMenu.Instance?.Show(position, actions);
    }

    private void AssignToSlot(int slot, string abilityId, string abilityName)
    {
        GameState.Instance.SkillHotbar.SetSlot(slot, abilityId);
        SkillBarHud.Instance?.RefreshDisplay();
        Toast.Instance?.Success($"{abilityName} → Slot [{slot + 1}]");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_isOpen) return;
        if (ActionMenu.Instance?.IsOpen == true) return;

        if (KeyboardNav.IsCancelPressed(@event))
        {
            Close();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed(Constants.InputActions.ShoulderLeft))
        {
            BuildTab((_currentTab - 1 + _categories.Length) % _categories.Length);
            GetViewport().SetInputAsHandled();
            return;
        }
        if (@event.IsActionPressed(Constants.InputActions.ShoulderRight))
        {
            BuildTab((_currentTab + 1) % _categories.Length);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (KeyboardNav.HandleInput(@event, _abilityList))
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventKey k && k.Pressed)
            GetViewport().SetInputAsHandled();
    }

    private static StyleBoxFlat CreateTabStyle(bool active)
    {
        var style = new StyleBoxFlat();
        if (active)
        {
            style.BgColor = UiTheme.Colors.Action;
            style.BorderColor = UiTheme.Colors.Action;
        }
        else
        {
            style.BgColor = new Color(UiTheme.Colors.BgPanel, 0.6f);
            style.BorderColor = new Color(UiTheme.Colors.Muted, 0.3f);
        }
        style.SetBorderWidthAll(1);
        style.BorderWidthBottom = active ? 3 : 1;
        style.SetCornerRadiusAll(0);
        style.CornerRadiusTopLeft = 4;
        style.CornerRadiusTopRight = 4;
        style.ContentMarginLeft = 8;
        style.ContentMarginRight = 8;
        style.ContentMarginTop = 4;
        style.ContentMarginBottom = 4;
        return style;
    }

    private static string GetActionKeyName(string action)
    {
        var events = InputMap.ActionGetEvents(action);
        foreach (var ev in events)
            if (ev is InputEventKey keyEv)
                return OS.GetKeycodeString(keyEv.Keycode);
        return "?";
    }
}
