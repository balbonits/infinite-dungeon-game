using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Skill tree browser and allocation UI. Accessed from pause menu.
/// Shows categories → base skills → specific skills in a hierarchical layout.
/// Players can spend skill points to level skills.
/// </summary>
public partial class SkillTreeDialog : Control
{
    public static SkillTreeDialog Instance { get; private set; } = null!;

    private ColorRect _overlay = null!;
    private CenterContainer _center = null!;
    private VBoxContainer _contentContainer = null!;
    private Label _pointsLabel = null!;
    private Label _detailLabel = null!;
    private ScrollContainer _scrollContainer = null!;
    private VBoxContainer _skillList = null!;
    private bool _isOpen;

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
        panel.CustomMinimumSize = new Vector2(420, 0);
        _center.AddChild(panel);

        var margin = new MarginContainer();
        panel.AddChild(margin);

        _contentContainer = new VBoxContainer();
        _contentContainer.AddThemeConstantOverride("separation", 8);
        margin.AddChild(_contentContainer);

        // Header
        var title = new Label();
        title.Text = Strings.Skills.Title;
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        _contentContainer.AddChild(title);

        _pointsLabel = new Label();
        UiTheme.StyleLabel(_pointsLabel, UiTheme.Colors.Safe, UiTheme.FontSizes.Body);
        _pointsLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _contentContainer.AddChild(_pointsLabel);

        _contentContainer.AddChild(new HSeparator());

        // Detail area (shows selected skill info)
        _detailLabel = new Label();
        UiTheme.StyleLabel(_detailLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        _detailLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _detailLabel.CustomMinimumSize = new Vector2(0, 40);
        _contentContainer.AddChild(_detailLabel);

        // Scrollable skill list
        _scrollContainer = new ScrollContainer();
        _scrollContainer.CustomMinimumSize = new Vector2(0, 320);
        _contentContainer.AddChild(_scrollContainer);

        _skillList = new VBoxContainer();
        _skillList.AddThemeConstantOverride("separation", 4);
        _scrollContainer.AddChild(_skillList);

        // Close button
        var closeBtn = new Button();
        closeBtn.Text = Strings.Ui.Cancel;
        closeBtn.CustomMinimumSize = new Vector2(200, 38);
        closeBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        UiTheme.StyleSecondaryButton(closeBtn, UiTheme.FontSizes.Body);
        closeBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(() => Close()));
        _contentContainer.AddChild(closeBtn);
    }

    public new void Show()
    {
        if (_isOpen) return;
        _isOpen = true;
        GetTree().Paused = true;
        RefreshSkillList();
        _overlay.Visible = true;
        _center.Visible = true;
    }

    public void Close()
    {
        _isOpen = false;
        _overlay.Visible = false;
        _center.Visible = false;
        // Return to PauseMenu (which opened us) — keep game paused
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

    private void RefreshSkillList()
    {
        foreach (Node child in _skillList.GetChildren())
            child.QueueFree();

        var tracker = GameState.Instance.Skills;
        var playerClass = GameState.Instance.SelectedClass;

        _pointsLabel.Text = Strings.Skills.PointsAvailable(tracker.SkillPoints);
        _detailLabel.Text = Strings.Skills.SelectSkill;

        string[] categories = SkillDatabase.GetCategories(playerClass);
        foreach (string catId in categories)
        {
            // Category header
            string catName = SkillDatabase.GetCategoryName(catId);
            var catLabel = new Label();
            catLabel.Text = $"─── {catName} ───";
            UiTheme.StyleLabel(catLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Body);
            catLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _skillList.AddChild(catLabel);

            // Base skills in this category
            foreach (var baseDef in SkillDatabase.GetBaseSkillsInCategory(catId))
            {
                var baseState = tracker.GetState(baseDef.Id);
                int baseLevel = baseState?.Level ?? 0;
                float passiveBonus = baseState?.GetPassiveBonus(baseDef.PassiveMultiplier) ?? 0;

                // Base skill row
                var baseRow = CreateSkillRow(baseDef, baseState, tracker, isBase: true);
                _skillList.AddChild(baseRow);

                // Specific skills under this base (only if base >= 1)
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

            // Spacer between categories
            var spacer = new Control();
            spacer.CustomMinimumSize = new Vector2(0, 8);
            _skillList.AddChild(spacer);
        }

        UiTheme.FocusFirstButton(_skillList);
    }

    private HBoxContainer CreateSkillRow(SkillDef def, SkillState? state, SkillTracker tracker, bool isBase)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);

        // Indent specific skills
        if (!isBase)
        {
            var indent = new Control();
            indent.CustomMinimumSize = new Vector2(20, 0);
            row.AddChild(indent);
        }

        // Skill name + level
        int level = state?.Level ?? 0;
        var nameLabel = new Label();
        string prefix = isBase ? "▸ " : "  ";
        nameLabel.Text = $"{prefix}{def.Name} [Lv.{level}]";
        Color nameColor = level > 0 ? UiTheme.Colors.Ink : UiTheme.Colors.Muted;
        UiTheme.StyleLabel(nameLabel, nameColor, UiTheme.FontSizes.Body);
        nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(nameLabel);

        // XP progress (if leveled)
        if (level > 0 && state != null)
        {
            var xpLabel = new Label();
            int xpNeeded = state.XpToNextLevel;
            xpLabel.Text = xpNeeded > 0 ? $"{state.Xp}/{xpNeeded}" : "";
            UiTheme.StyleLabel(xpLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
            row.AddChild(xpLabel);
        }

        // Allocate button (if player has points and skill is available)
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

        // Assign-to-hotbar button (only for specific skills that are unlocked)
        if (def.Type == SkillType.Specific && (state?.Level ?? 0) >= 1)
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

        // FF-style: detail updates on focus (cursor move), not just hover
        string detailText = BuildDetailText(def, state);
        allocBtn.FocusEntered += () => _detailLabel.Text = detailText;
        row.MouseEntered += () => _detailLabel.Text = detailText;

        return row;
    }

    private static string BuildDetailText(SkillDef def, SkillState? state)
    {
        int level = state?.Level ?? 0;
        string text = $"{def.Name}\n{def.Description}";

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
        if (ActionMenu.Instance?.IsOpen == true) return; // let action menu handle input

        if (KeyboardNav.IsCancelPressed(@event))
        {
            Close();
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
}
