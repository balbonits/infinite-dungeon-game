using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Skills tab — masteries grouped by category + Innate tab.
/// Uses GameWindow + GameTabPanel.
/// </summary>
public partial class SkillTreeDialog : GameWindow
{
    public static SkillTreeDialog Instance { get; private set; } = null!;

    private Label _pointsLabel = null!;
    private Label _detailLabel = null!;
    private GameTabPanel _tabs = null!;
    private readonly System.Collections.Generic.List<Button> _allocButtons = new();

    public override void _Ready()
    {
        Instance = this;
        base._Ready();
    }

    protected override void BuildContent(VBoxContainer content)
    {
        var title = new Label();
        title.Text = "SKILLS";
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
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

        _tabs = new GameTabPanel();
        // Tabs will be added in OnShow when we know the class
    }

    protected override void OnShow()
    {
        // Clear old tabs and rebuild for current class
        _tabs.GetParent()?.RemoveChild(_tabs);
        _detailLabel.GetParent()?.RemoveChild(_detailLabel);

        var categories = SkillAbilityDatabase.GetCategories(GameState.Instance.SelectedClass);

        // Remove old tabs, create fresh
        _tabs = new GameTabPanel();
        foreach (string catId in categories)
        {
            string captured = catId;
            _tabs.AddTab(SkillAbilityDatabase.GetCategoryName(catId), () => BuildCategoryContent(captured));
        }
        _tabs.AddTab("Innate", BuildInnateContent);

        ContentBox.AddChild(_detailLabel);
        ContentBox.AddChild(_tabs);

        _tabs.SelectTab(0);
    }

    protected override bool HandleTabInput(InputEvent @event) => _tabs?.HandleInput(@event) ?? false;

    private void BuildCategoryContent(string categoryId)
    {
        _allocButtons.Clear();
        var tracker = GameState.Instance.Progression;
        _pointsLabel.Text = $"SP: {tracker.SkillPoints} available";
        _detailLabel.Text = "Select a mastery to view details";

        foreach (var def in SkillAbilityDatabase.GetMasteriesInCategory(categoryId))
        {
            var state = tracker.GetMastery(def.Id);
            _tabs.ScrollContent.AddChild(CreateMasteryRow(def, state, tracker));

            int abilityCount = 0;
            foreach (var _ in SkillAbilityDatabase.GetAbilitiesForMastery(def.Id))
                abilityCount++;

            if (abilityCount > 0)
            {
                int level = state?.Level ?? 0;
                var info = new Label();
                info.Text = level >= 1 ? $"    → {abilityCount} abilities unlocked" : $"    → {abilityCount} abilities (requires Lv.1)";
                UiTheme.StyleLabel(info, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
                _tabs.ScrollContent.AddChild(info);
            }
        }
    }

    private void BuildInnateContent()
    {
        _allocButtons.Clear();
        var tracker = GameState.Instance.Progression;
        _pointsLabel.Text = $"SP: {tracker.SkillPoints} available";
        _detailLabel.Text = "Select a mastery to view details";

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

        var xpLabel = new Label();
        UiTheme.StyleLabel(xpLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        if (level > 0 && state != null)
        {
            int xpNeeded = state.XpToNextLevel;
            xpLabel.Text = xpNeeded > 0 ? $"{state.Xp}/{xpNeeded}" : "";
        }
        row.AddChild(xpLabel);

        var bonusLabel = new Label();
        UiTheme.StyleLabel(bonusLabel, UiTheme.Colors.Safe, UiTheme.FontSizes.Small);
        if (level > 0 && state != null)
            bonusLabel.Text = $"+{state.GetPassiveBonus(def.PassiveMultiplier):F1}%";
        row.AddChild(bonusLabel);

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
            var s = tracker.GetMastery(capturedId);
            int lvl = s?.Level ?? 0;
            nameLabel.Text = $"▸ {def.Name} [Lv.{lvl}]";
            nameLabel.AddThemeColorOverride("font_color", lvl > 0 ? UiTheme.Colors.Ink : UiTheme.Colors.Muted);
            if (s != null && lvl > 0)
            {
                xpLabel.Text = s.XpToNextLevel > 0 ? $"{s.Xp}/{s.XpToNextLevel}" : "";
                bonusLabel.Text = $"+{s.GetPassiveBonus(capturedMult):F1}%";
            }
            _pointsLabel.Text = $"SP: {tracker.SkillPoints} available";
            _detailLabel.Text = BuildDetail(def, s);
            if (tracker.SkillPoints <= 0)
                foreach (var btn in _allocButtons) btn.Disabled = true;
        }));
        row.AddChild(allocBtn);
        _allocButtons.Add(allocBtn);

        string detail = BuildDetail(def, state);
        allocBtn.FocusEntered += () => _detailLabel.Text = detail;
        row.MouseEntered += () => _detailLabel.Text = detail;
        return row;
    }

    private static string BuildDetail(MasteryDef def, MasteryState? state)
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
}
