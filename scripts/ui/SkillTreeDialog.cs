using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Skill tree browser with tabbed categories (Q/E to switch).
/// Each tab shows base skills and their specific skills for one category.
/// Players can spend skill points and assign skills to the hotbar.
/// </summary>
public partial class SkillTreeDialog : Control
{
    public static SkillTreeDialog Instance { get; private set; } = null!;

    private ColorRect _overlay = null!;
    private CenterContainer _center = null!;
    private Label _pointsLabel = null!;
    private Label _detailLabel = null!;
    private HBoxContainer _tabBar = null!;
    private ScrollContainer _scrollContainer = null!;
    private VBoxContainer _skillList = null!;
    private bool _isOpen;

    private string[] _categories = System.Array.Empty<string>();
    private Button[] _tabButtons = System.Array.Empty<Button>();
    private int _currentTab;

    public bool IsOpen => _isOpen;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
        MouseFilter = MouseFilterEnum.Ignore;
        BuildUi();
    }

    private void BuildUi()
    {
        _overlay = new ColorRect();
        _overlay.Color = new Color(0, 0, 0, 0.6f);
        _overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        _overlay.MouseFilter = MouseFilterEnum.Stop;
        _overlay.Visible = false;
        AddChild(_overlay);

        _center = new CenterContainer();
        _center.SetAnchorsPreset(LayoutPreset.FullRect);
        _center.Visible = false;
        AddChild(_center);

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", UiTheme.CreatePanelStyle(0.95f, true));
        panel.CustomMinimumSize = new Vector2(440, 0);
        _center.AddChild(panel);

        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 6);
        panel.AddChild(content);

        // Header
        var title = new Label();
        title.Text = Strings.Skills.Title;
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(title);

        _pointsLabel = new Label();
        UiTheme.StyleLabel(_pointsLabel, UiTheme.Colors.Safe, UiTheme.FontSizes.Body);
        _pointsLabel.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(_pointsLabel);

        // Tab bar — one tab per category
        _tabBar = new HBoxContainer();
        _tabBar.AddThemeConstantOverride("separation", 0);
        _tabBar.Alignment = BoxContainer.AlignmentMode.Center;
        content.AddChild(_tabBar);

        // Q/E hint
        var tabHint = new Label();
        string lKey = GetActionKeyName(Constants.InputActions.ShoulderLeft);
        string rKey = GetActionKeyName(Constants.InputActions.ShoulderRight);
        tabHint.Text = $"[{lKey}] / [{rKey}] switch tabs | [D] close";
        UiTheme.StyleLabel(tabHint, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        tabHint.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(tabHint);

        content.AddChild(new HSeparator());

        // Detail area
        _detailLabel = new Label();
        UiTheme.StyleLabel(_detailLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        _detailLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _detailLabel.CustomMinimumSize = new Vector2(0, 36);
        content.AddChild(_detailLabel);

        // Scrollable skill list — ONLY skills, no Cancel button
        _scrollContainer = new ScrollContainer();
        _scrollContainer.CustomMinimumSize = new Vector2(0, 300);
        content.AddChild(_scrollContainer);

        _skillList = new VBoxContainer();
        _skillList.AddThemeConstantOverride("separation", 4);
        _scrollContainer.AddChild(_skillList);
    }

    public new void Show()
    {
        if (_isOpen) return;
        _isOpen = true;
        GetTree().Paused = true;

        // Build tabs from player's class categories
        _categories = SkillDatabase.GetCategories(GameState.Instance.SelectedClass);
        RebuildTabs();
        BuildTab(0);

        _overlay.Visible = true;
        _center.Visible = true;
    }

    public void Close()
    {
        _isOpen = false;
        _overlay.Visible = false;
        _center.Visible = false;
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
            btn.Text = SkillDatabase.GetCategoryName(_categories[i]);
            btn.CustomMinimumSize = new Vector2(0, 28);
            btn.FocusMode = FocusModeEnum.None; // tabs not focusable — Q/E only
            int idx = i;
            btn.Connect(BaseButton.SignalName.Pressed, Callable.From(() => BuildTab(idx)));
            _tabBar.AddChild(btn);
            _tabButtons[i] = btn;
        }
    }

    private void BuildTab(int tabIndex)
    {
        _currentTab = tabIndex;

        // Style active/inactive tabs
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

        RefreshSkillList();
    }

    private void RefreshSkillList()
    {
        foreach (Node child in _skillList.GetChildren())
            child.QueueFree();

        var tracker = GameState.Instance.Skills;
        _pointsLabel.Text = Strings.Skills.PointsAvailable(tracker.SkillPoints);
        _detailLabel.Text = Strings.Skills.SelectSkill;

        if (_currentTab >= _categories.Length) return;
        string catId = _categories[_currentTab];

        foreach (var baseDef in SkillDatabase.GetBaseSkillsInCategory(catId))
        {
            var baseState = tracker.GetState(baseDef.Id);
            int baseLevel = baseState?.Level ?? 0;

            var baseRow = CreateSkillRow(baseDef, baseState, tracker, isBase: true);
            _skillList.AddChild(baseRow);

            if (baseLevel >= 1)
            {
                foreach (var specDef in SkillDatabase.GetSpecificSkills(baseDef.Id))
                {
                    var specState = tracker.GetState(specDef.Id);
                    var specRow = CreateSkillRow(specDef, specState, tracker, isBase: false);
                    _skillList.AddChild(specRow);
                }
            }
        }

        _scrollContainer.ScrollVertical = 0;
        CallDeferred(MethodName.FocusFirst);
    }

    private void FocusFirst()
    {
        UiTheme.FocusFirstButton(_skillList);
    }

    private HBoxContainer CreateSkillRow(SkillDef def, SkillState? state, SkillTracker tracker, bool isBase)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);

        if (!isBase)
        {
            var indent = new Control();
            indent.CustomMinimumSize = new Vector2(20, 0);
            row.AddChild(indent);
        }

        int level = state?.Level ?? 0;
        var nameLabel = new Label();
        string prefix = isBase ? "▸ " : "  ";
        nameLabel.Text = $"{prefix}{def.Name} [Lv.{level}]";
        Color nameColor = level > 0 ? UiTheme.Colors.Ink : UiTheme.Colors.Muted;
        UiTheme.StyleLabel(nameLabel, nameColor, UiTheme.FontSizes.Body);
        nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(nameLabel);

        if (level > 0 && state != null)
        {
            var xpLabel = new Label();
            int xpNeeded = state.XpToNextLevel;
            xpLabel.Text = xpNeeded > 0 ? $"{state.Xp}/{xpNeeded}" : "";
            UiTheme.StyleLabel(xpLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
            row.AddChild(xpLabel);
        }

        // Allocate point button
        bool canAllocate = tracker.SkillPoints > 0 && tracker.IsUnlocked(def.Id);
        var allocBtn = new Button();
        allocBtn.Text = "+";
        allocBtn.CustomMinimumSize = new Vector2(32, 28);
        allocBtn.FocusMode = FocusModeEnum.All;
        UiTheme.StyleButton(allocBtn, UiTheme.FontSizes.Small);
        allocBtn.Disabled = !canAllocate;
        string capturedId = def.Id;
        allocBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(() =>
        {
            tracker.AllocatePoint(capturedId);
            RefreshSkillList();
        }));
        row.AddChild(allocBtn);

        // Assign-to-hotbar button (specific skills that are unlocked)
        if (def.Type == SkillType.Specific && level >= 1)
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

        // Detail on focus
        string detailText = BuildDetailText(def, state);
        allocBtn.FocusEntered += () => _detailLabel.Text = detailText;
        row.MouseEntered += () => _detailLabel.Text = detailText;

        return row;
    }

    private static string BuildDetailText(SkillDef def, SkillState? state)
    {
        int level = state?.Level ?? 0;
        string text = $"{def.Name}\n{def.Description}";

        if (def.ManaCost > 0)
            text += $"\nCost: {def.ManaCost} MP | CD: {def.Cooldown:F1}s";

        if (def.Type == SkillType.Base && level > 0 && state != null)
        {
            float bonus = state.GetPassiveBonus(def.PassiveMultiplier);
            text += $"\nPassive: +{bonus:F1}% {def.PassiveType}";
        }

        if (level > 0 && state != null)
        {
            int xpNeeded = state.XpToNextLevel;
            if (xpNeeded > 0)
                text += $"\nXP: {state.Xp} / {xpNeeded}";
        }

        return text;
    }

    private void ShowAssignMenu(string skillId, string skillName, Vector2 position)
    {
        var actions = new (string label, System.Action action)[]
        {
            ($"Assign to [1]", () => AssignToSlot(0, skillId, skillName)),
            ($"Assign to [2]", () => AssignToSlot(1, skillId, skillName)),
            ($"Assign to [3]", () => AssignToSlot(2, skillId, skillName)),
            ($"Assign to [4]", () => AssignToSlot(3, skillId, skillName)),
        };
        ActionMenu.Instance?.Show(position, actions);
    }

    private void AssignToSlot(int slot, string skillId, string skillName)
    {
        GameState.Instance.SkillHotbar.SetSlot(slot, skillId);
        SkillBarHud.Instance?.RefreshDisplay();
        Toast.Instance?.Success($"{skillName} → Slot [{slot + 1}]");
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

        // Q/E switch tabs
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

        // Navigate ONLY within skill list — Cancel button is gone, D closes
        if (KeyboardNav.HandleInput(@event, _skillList))
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
