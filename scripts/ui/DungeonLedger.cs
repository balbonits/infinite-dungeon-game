using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Dungeon Ledger UI — the achievement browser.
/// Accessible from pause menu. Shows all achievements by category
/// with progress bars and unlock status.
/// </summary>
public partial class DungeonLedger : GameWindow
{
    public static DungeonLedger Instance { get; private set; } = null!;

    private ScrollContainer _scrollContainer = null!;
    private VBoxContainer _achievementList = null!;
    private Label _countLabel = null!;

    public override void _Ready()
    {
        Instance = this;
        WindowWidth = 450f;
        base._Ready();
    }

    protected override void BuildContent(VBoxContainer content)
    {
        var title = new Label();
        title.Text = Strings.Achievements.Title;
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(title);

        _countLabel = new Label();
        UiTheme.StyleLabel(_countLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Body);
        _countLabel.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(_countLabel);

        content.AddChild(new HSeparator());

        _scrollContainer = new ScrollContainer { FollowFocus = true };
        _scrollContainer.CustomMinimumSize = new Vector2(0, 360);
        content.AddChild(_scrollContainer);

        _achievementList = new VBoxContainer();
        _achievementList.AddThemeConstantOverride("separation", 4);
        _scrollContainer.AddChild(_achievementList);

        var closeBtn = new Button();
        closeBtn.Text = Strings.Ui.Cancel;
        closeBtn.CustomMinimumSize = new Vector2(200, 38);
        closeBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        UiTheme.StyleSecondaryButton(closeBtn, UiTheme.FontSizes.Body);
        closeBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(Close));
        content.AddChild(closeBtn);
    }

    protected override void OnShow()
    {
        Refresh();
    }

    private void Refresh()
    {
        foreach (Node child in _achievementList.GetChildren())
            child.QueueFree();

        var tracker = GameState.Instance.Achievements;
        var allAchievements = AchievementTracker.GetAll();
        int totalUnlocked = tracker.Unlocked.Count;

        _countLabel.Text = $"{totalUnlocked}/{allAchievements.Count} achievements unlocked";

        var categories = new[]
        {
            AchievementCategory.Combat, AchievementCategory.Exploration,
            AchievementCategory.Progression, AchievementCategory.Economy,
            AchievementCategory.Mastery,
        };

        foreach (var cat in categories)
        {
            var catLabel = new Label();
            catLabel.Text = $"─── {cat} ───";
            UiTheme.StyleLabel(catLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Body);
            catLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _achievementList.AddChild(catLabel);

            foreach (var def in AchievementTracker.GetByCategory(cat))
            {
                bool unlocked = tracker.IsUnlocked(def.Id);
                float progress = tracker.GetProgress(def);

                var row = new VBoxContainer();
                row.AddThemeConstantOverride("separation", 2);

                // Name + status
                var nameRow = new HBoxContainer();
                var nameLabel = new Label();
                nameLabel.Text = unlocked ? $"✓ {def.Name}" : def.Name;
                Color nameColor = unlocked ? UiTheme.Colors.Safe : UiTheme.Colors.Ink;
                UiTheme.StyleLabel(nameLabel, nameColor, UiTheme.FontSizes.Body);
                nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                nameRow.AddChild(nameLabel);

                if (!unlocked)
                {
                    var progressLabel = new Label();
                    progressLabel.Text = $"{(int)(progress * 100)}%";
                    UiTheme.StyleLabel(progressLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
                    nameRow.AddChild(progressLabel);
                }
                else if (def.GoldReward > 0)
                {
                    var rewardLabel = new Label();
                    rewardLabel.Text = $"+{def.GoldReward}g";
                    UiTheme.StyleLabel(rewardLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Small);
                    nameRow.AddChild(rewardLabel);
                }
                row.AddChild(nameRow);

                // Description
                var descLabel = new Label();
                descLabel.Text = def.Description;
                UiTheme.StyleLabel(descLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
                row.AddChild(descLabel);

                // Title reward if any
                if (unlocked && def.TitleReward != null)
                {
                    var titleLabel = new Label();
                    titleLabel.Text = $"Title: {def.TitleReward}";
                    UiTheme.StyleLabel(titleLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Small);
                    row.AddChild(titleLabel);
                }

                _achievementList.AddChild(row);
            }

            var spacer = new Control();
            spacer.CustomMinimumSize = new Vector2(0, 6);
            _achievementList.AddChild(spacer);
        }
    }

    protected override bool HandleExtraInput(InputEvent @event)
    {
        // Scroll with Up/Down arrows
        if (@event.IsActionPressed(Constants.InputActions.MoveUp))
        {
            _scrollContainer.ScrollVertical -= 40;
            return true;
        }
        if (@event.IsActionPressed(Constants.InputActions.MoveDown))
        {
            _scrollContainer.ScrollVertical += 40;
            return true;
        }
        return false;
    }
}
