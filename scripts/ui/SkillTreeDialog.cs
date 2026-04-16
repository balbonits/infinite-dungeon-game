using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Skills tab — shows masteries (passive) grouped by category.
/// Players spend SP here. No hotbar assignment (moved to AbilitiesDialog).
/// Includes Innate sub-section at the bottom.
/// </summary>
public partial class SkillTreeDialog : Control
{
    public static SkillTreeDialog Instance { get; private set; } = null!;

    private ColorRect _overlay = null!;
    private Label _pointsLabel = null!;
    private Label _detailLabel = null!;
    private HBoxContainer _tabBar = null!;
    private ScrollContainer _scrollContainer = null!;
    private VBoxContainer _skillList = null!;
    private bool _isOpen;

    private string[] _categories = System.Array.Empty<string>();
    private Button[] _tabButtons = System.Array.Empty<Button>();
    private int _currentTab;

    // Extra tab index for Innate
    private bool _showingInnate;
    private readonly System.Collections.Generic.List<Button> _allocButtons = new();

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
        var (overlay, content) = UiTheme.CreateDialogWindow(440f);
        _overlay = overlay;
        _overlay.Visible = false;
        AddChild(_overlay);

        var title = new Label();
        title.Text = "SKILLS";
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(title);

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
        _detailLabel.CustomMinimumSize = new Vector2(0, 36);
        content.AddChild(_detailLabel);

        _scrollContainer = new ScrollContainer { FollowFocus = true };
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
        WindowStack.Push(this);
        GetTree().Paused = true;

        // Class categories + "Innate" as last tab
        _categories = SkillAbilityDatabase.GetCategories(GameState.Instance.SelectedClass);
        _showingInnate = false;
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

        int totalTabs = _categories.Length + 1; // +1 for Innate
        _tabButtons = new Button[totalTabs];

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

        // Innate tab
        var innateBtn = new Button();
        innateBtn.Text = "Innate";
        innateBtn.CustomMinimumSize = new Vector2(0, 28);
        innateBtn.FocusMode = FocusModeEnum.None;
        innateBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(() => BuildTab(_categories.Length)));
        _tabBar.AddChild(innateBtn);
        _tabButtons[_categories.Length] = innateBtn;
    }

    private void BuildTab(int tabIndex)
    {
        _currentTab = tabIndex;
        _showingInnate = tabIndex >= _categories.Length;

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

        RefreshMasteryList();
    }

    private void RefreshMasteryList()
    {
        _allocButtons.Clear();
        foreach (Node child in _skillList.GetChildren())
            child.QueueFree();

        var tracker = GameState.Instance.Progression;
        _pointsLabel.Text = $"SP: {tracker.SkillPoints} available";
        _detailLabel.Text = "Select a mastery to view details";

        System.Collections.Generic.IEnumerable<MasteryDef> masteries;
        if (_showingInnate)
            masteries = SkillAbilityDatabase.GetInnateMasteries();
        else if (_currentTab < _categories.Length)
            masteries = SkillAbilityDatabase.GetMasteriesInCategory(_categories[_currentTab]);
        else
            return;

        foreach (var def in masteries)
        {
            var state = tracker.GetMastery(def.Id);
            var row = CreateMasteryRow(def, state, tracker);
            _skillList.AddChild(row);

            // Show child ability count as sub-info
            int abilityCount = 0;
            foreach (var _ in SkillAbilityDatabase.GetAbilitiesForMastery(def.Id))
                abilityCount++;

            if (abilityCount > 0)
            {
                int level = state?.Level ?? 0;
                var abilityInfo = new Label();
                string status = level >= 1 ? $"{abilityCount} abilities unlocked" : $"{abilityCount} abilities (requires Lv.1)";
                abilityInfo.Text = $"    → {status}";
                UiTheme.StyleLabel(abilityInfo, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
                _skillList.AddChild(abilityInfo);
            }
        }

        _scrollContainer.ScrollVertical = 0;
        CallDeferred(MethodName.FocusFirst);
    }

    private void FocusFirst()
    {
        UiTheme.FocusFirstButton(_skillList);
    }



    private HBoxContainer CreateMasteryRow(MasteryDef def, MasteryState? state, ProgressionTracker tracker)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);

        int level = state?.Level ?? 0;

        // Name + level
        var nameLabel = new Label();
        nameLabel.Text = $"▸ {def.Name} [Lv.{level}]";
        Color nameColor = level > 0 ? UiTheme.Colors.Ink : UiTheme.Colors.Muted;
        UiTheme.StyleLabel(nameLabel, nameColor, UiTheme.FontSizes.Body);
        nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(nameLabel);

        // XP progress (always create, hide if level 0)
        var xpLabel = new Label();
        UiTheme.StyleLabel(xpLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        if (level > 0 && state != null)
        {
            int xpNeeded = state.XpToNextLevel;
            xpLabel.Text = xpNeeded > 0 ? $"{state.Xp}/{xpNeeded}" : "";
        }
        row.AddChild(xpLabel);

        // Passive bonus display (always create, hide if level 0)
        var bonusLabel = new Label();
        UiTheme.StyleLabel(bonusLabel, UiTheme.Colors.Safe, UiTheme.FontSizes.Small);
        if (level > 0 && state != null)
        {
            float bonus = state.GetPassiveBonus(def.PassiveMultiplier);
            bonusLabel.Text = $"+{bonus:F1}%";
        }
        row.AddChild(bonusLabel);

        // [+] SP allocate button
        var allocBtn = new Button();
        allocBtn.Text = "+";
        allocBtn.CustomMinimumSize = new Vector2(32, 28);
        allocBtn.FocusMode = FocusModeEnum.All;
        UiTheme.StyleButton(allocBtn, UiTheme.FontSizes.Small);
        allocBtn.Disabled = tracker.SkillPoints <= 0;
        string capturedId = def.Id;
        float capturedMult = def.PassiveMultiplier;
        allocBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(() =>
        {
            if (!tracker.AllocateSP(capturedId)) return;

            // Update this row's labels in-place — no rebuild
            var s = tracker.GetMastery(capturedId);
            int lvl = s?.Level ?? 0;
            nameLabel.Text = $"▸ {def.Name} [Lv.{lvl}]";
            nameLabel.AddThemeColorOverride("font_color", lvl > 0 ? UiTheme.Colors.Ink : UiTheme.Colors.Muted);

            if (s != null && lvl > 0)
            {
                int xpNeeded = s.XpToNextLevel;
                xpLabel.Text = xpNeeded > 0 ? $"{s.Xp}/{xpNeeded}" : "";
                bonusLabel.Text = $"+{s.GetPassiveBonus(capturedMult):F1}%";
            }

            _pointsLabel.Text = $"SP: {tracker.SkillPoints} available";
            _detailLabel.Text = BuildMasteryDetail(def, s);

            // Disable all [+] buttons if SP ran out
            if (tracker.SkillPoints <= 0)
                foreach (var btn in _allocButtons)
                    btn.Disabled = true;
        }));
        row.AddChild(allocBtn);
        _allocButtons.Add(allocBtn);

        // Detail on focus/hover
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
            float bonus = state.GetPassiveBonus(def.PassiveMultiplier);
            text += $"\nPassive: +{bonus:F1}% {def.PassiveType}";
            int xpNeeded = state.XpToNextLevel;
            if (xpNeeded > 0)
                text += $"\nXP: {state.Xp} / {xpNeeded}";
        }

        return text;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_isOpen) return;

        if (KeyboardNav.IsCancelPressed(@event))
        {
            Close();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed(Constants.InputActions.ShoulderLeft))
        {
            int totalTabs = _categories.Length + 1;
            BuildTab((_currentTab - 1 + totalTabs) % totalTabs);
            GetViewport().SetInputAsHandled();
            return;
        }
        if (@event.IsActionPressed(Constants.InputActions.ShoulderRight))
        {
            int totalTabs = _categories.Length + 1;
            BuildTab((_currentTab + 1) % totalTabs);
            GetViewport().SetInputAsHandled();
            return;
        }

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
