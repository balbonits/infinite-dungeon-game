using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Abilities tab — class-specific active combat actions.
/// Tab name: Warrior Arts / Ranger Crafts / Arcane Spells.
/// </summary>
public partial class AbilitiesDialog : GameWindow
{
    public static AbilitiesDialog Instance { get; private set; } = null!;

    private Label _titleLabel = null!;
    private Label _pointsLabel = null!;
    private Label _detailLabel = null!;
    private HBoxContainer _tabBar = null!;

    private string[] _categories = System.Array.Empty<string>();
    private Button[] _tabButtons = System.Array.Empty<Button>();
    private int _currentTab;
    private readonly System.Collections.Generic.List<Button> _allocButtons = new();

    private static string GetTabName(PlayerClass cls) => cls switch
    {
        PlayerClass.Warrior => "WARRIOR ARTS",
        PlayerClass.Ranger => "RANGER CRAFTS",
        PlayerClass.Mage => "ARCANE SPELLS",
        _ => "ABILITIES",
    };

    public override void _Ready()
    {
        Instance = this;
        base._Ready();
    }

    protected override void BuildContent(VBoxContainer content)
    {
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
        tabHint.Text = "[Q] / [E] switch tabs | [D] close";
        UiTheme.StyleLabel(tabHint, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        tabHint.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(tabHint);

        content.AddChild(new HSeparator());

        _detailLabel = new Label();
        UiTheme.StyleLabel(_detailLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        _detailLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _detailLabel.CustomMinimumSize = new Vector2(0, 48);
        content.AddChild(_detailLabel);

        content.AddChild(Scroll);
    }

    protected override void OnShow()
    {
        var cls = GameState.Instance.SelectedClass;
        _titleLabel.Text = GetTabName(cls);
        _categories = SkillAbilityDatabase.GetCategories(cls);
        RebuildTabs();
        BuildTab(0);
    }

    protected override bool HandleTabInput(InputEvent @event)
    {
        if (@event.IsActionPressed(Constants.InputActions.ShoulderLeft))
        {
            BuildTab((_currentTab - 1 + _categories.Length) % _categories.Length);
            return true;
        }
        if (@event.IsActionPressed(Constants.InputActions.ShoulderRight))
        {
            BuildTab((_currentTab + 1) % _categories.Length);
            return true;
        }
        return false;
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
            _tabButtons[i].AddThemeStyleboxOverride("normal", UiTheme.CreateTabStyle(active));
            _tabButtons[i].AddThemeStyleboxOverride("hover", UiTheme.CreateTabStyle(active));
            _tabButtons[i].AddThemeStyleboxOverride("focus", UiTheme.CreateTabStyle(active));
            _tabButtons[i].AddThemeColorOverride("font_color", active ? UiTheme.Colors.BgDark : UiTheme.Colors.Muted);
            _tabButtons[i].AddThemeColorOverride("font_hover_color", active ? UiTheme.Colors.BgDark : UiTheme.Colors.Ink);
            _tabButtons[i].AddThemeFontSizeOverride("font_size", UiTheme.FontSizes.Small);
        }
        RefreshAbilityList();
    }

    private void RefreshAbilityList()
    {
        _allocButtons.Clear();
        foreach (Node child in ScrollContent.GetChildren())
            child.QueueFree();

        var tracker = GameState.Instance.Progression;
        _pointsLabel.Text = $"AP: {tracker.AbilityPoints} available";
        _detailLabel.Text = "Select an ability to view details";

        if (_currentTab >= _categories.Length) return;
        string catId = _categories[_currentTab];

        foreach (var mastery in SkillAbilityDatabase.GetMasteriesInCategory(catId))
        {
            var masteryState = tracker.GetMastery(mastery.Id);
            int masteryLevel = masteryState?.Level ?? 0;

            var header = new Label();
            header.Text = $"── {mastery.Name} (Lv.{masteryLevel}) ──";
            UiTheme.StyleLabel(header, masteryLevel > 0 ? UiTheme.Colors.Accent : UiTheme.Colors.Muted, UiTheme.FontSizes.Body);
            ScrollContent.AddChild(header);

            foreach (var ability in SkillAbilityDatabase.GetAbilitiesForMastery(mastery.Id))
            {
                var abilityState = tracker.GetAbility(ability.Id);
                bool unlocked = tracker.IsUnlocked(ability.Id);
                var row = CreateAbilityRow(ability, abilityState, tracker, unlocked, mastery.Name);
                ScrollContent.AddChild(row);
            }
        }

        Scroll.ScrollVertical = 0;
        CallDeferred(MethodName.FocusFirst);
    }

    private void FocusFirst() => UiTheme.FocusFirstButton(ScrollContent);

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
            nameLabel.Text = $"  🔒 {def.Name}";
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
        UiTheme.StyleButton(allocBtn, UiTheme.FontSizes.Small);
        allocBtn.Disabled = !unlocked || tracker.AbilityPoints <= 0;
        string capturedId = def.Id;
        string capturedParent = parentName;
        allocBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(() =>
        {
            if (!tracker.AllocateAP(capturedId)) return;
            var s = tracker.GetAbility(capturedId);
            int lvl = s?.Level ?? 0;
            nameLabel.Text = $"  {def.Name} [Lv.{lvl}]";
            nameLabel.AddThemeColorOverride("font_color", lvl > 0 ? UiTheme.Colors.Ink : UiTheme.Colors.Muted);
            _pointsLabel.Text = $"AP: {tracker.AbilityPoints} available";
            _detailLabel.Text = BuildAbilityDetail(def, s, true, capturedParent);
            if (tracker.AbilityPoints <= 0)
                foreach (var btn in _allocButtons) btn.Disabled = true;
        }));
        row.AddChild(allocBtn);
        _allocButtons.Add(allocBtn);

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
                ShowAssignMenu(assignId, assignName, assignBtn.GlobalPosition)));
            row.AddChild(assignBtn);
        }

        string detail = BuildAbilityDetail(def, state, unlocked, parentName);
        allocBtn.FocusEntered += () => _detailLabel.Text = detail;
        row.MouseEntered += () => _detailLabel.Text = detail;

        return row;
    }

    private static string BuildAbilityDetail(AbilityDef def, AbilityState? state, bool unlocked, string parentName)
    {
        if (!unlocked) return $"{def.Name}\nRequires {parentName} Lv.1";

        int level = state?.Level ?? 0;
        string text = $"{def.Name}\n{def.Description}";
        if (def.ManaCost > 0) text += $"\nCost: {def.ManaCost} MP | CD: {def.Cooldown:F1}s";
        if (level > 0 && state != null)
        {
            int xpNeeded = state.XpToNextLevel;
            if (xpNeeded > 0) text += $"\nXP: {state.Xp} / {xpNeeded}";
            if (state.UseCount > 0)
            {
                string tier = state.AffinityTier switch { 4 => " (Mastered)", 3 => " (Expert)", 2 => " (Practiced)", 1 => " (Familiar)", _ => "" };
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
}
