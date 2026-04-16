using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Quest panel for the Adventure Guild NPC.
/// Shows active quests, progress, and rewards.
/// Allows claiming completed quests and generating new ones.
/// </summary>
public partial class QuestPanel : GameWindow
{
    public static QuestPanel Instance { get; private set; } = null!;

    private Label _headerLabel = null!;

    public override void _Ready()
    {
        Instance = this;
        ReturnToPauseMenu = false;
        base._Ready();
    }

    protected override void BuildContent(VBoxContainer content)
    {
        var title = new Label();
        title.Text = Strings.Quests.Title;
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(title);

        _headerLabel = new Label();
        UiTheme.StyleLabel(_headerLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        _headerLabel.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(_headerLabel);

        content.AddChild(new HSeparator());

        Scroll.CustomMinimumSize = new Vector2(0, 280);
        ScrollContent.AddThemeConstantOverride("separation", 8);
        content.AddChild(Scroll);

        // Bottom buttons
        var bottomRow = new HBoxContainer();
        bottomRow.AddThemeConstantOverride("separation", 12);
        bottomRow.Alignment = BoxContainer.AlignmentMode.Center;
        content.AddChild(bottomRow);

        var refreshBtn = new Button();
        refreshBtn.Text = Strings.Quests.NewQuests;
        refreshBtn.CustomMinimumSize = new Vector2(160, 38);
        UiTheme.StyleButton(refreshBtn, UiTheme.FontSizes.Body);
        refreshBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(() =>
        {
            var tracker = GameState.Instance.Quests;
            tracker.GenerateQuests(GameState.Instance.FloorNumber);
            Toast.Instance?.Info("New quests available!");
            Refresh();
        }));
        bottomRow.AddChild(refreshBtn);

        var closeBtn = new Button();
        closeBtn.Text = Strings.Ui.Cancel;
        closeBtn.CustomMinimumSize = new Vector2(120, 38);
        UiTheme.StyleSecondaryButton(closeBtn, UiTheme.FontSizes.Body);
        closeBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(() => Close()));
        bottomRow.AddChild(closeBtn);
    }

    public void Open()
    {
        // Auto-generate quests if none exist
        var tracker = GameState.Instance.Quests;
        if (tracker.ActiveQuests.Count == 0)
            tracker.GenerateQuests(GameState.Instance.FloorNumber);

        Show();
    }

    protected override void OnShow()
    {
        Refresh();
    }

    private void Refresh()
    {
        foreach (Node child in ScrollContent.GetChildren())
            child.QueueFree();

        var tracker = GameState.Instance.Quests;
        int complete = 0;
        for (int i = 0; i < tracker.ActiveQuests.Count; i++)
            if (tracker.ActiveQuests[i].IsComplete) complete++;

        _headerLabel.Text = $"{complete}/{tracker.ActiveQuests.Count} quests completed";

        for (int i = 0; i < tracker.ActiveQuests.Count && i < tracker.QuestDefs.Count; i++)
        {
            var def = tracker.QuestDefs[i];
            var state = tracker.ActiveQuests[i];

            var questBox = new VBoxContainer();
            questBox.AddThemeConstantOverride("separation", 4);

            // Title row
            var titleRow = new HBoxContainer();
            var titleLabel = new Label();
            titleLabel.Text = def.Title;
            Color titleColor = state.IsComplete ? UiTheme.Colors.Safe : UiTheme.Colors.Ink;
            UiTheme.StyleLabel(titleLabel, titleColor, UiTheme.FontSizes.Body);
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
                    Refresh();
                }));
                titleRow.AddChild(claimBtn);
            }
            questBox.AddChild(titleRow);

            // Description
            var descLabel = new Label();
            descLabel.Text = def.Description;
            UiTheme.StyleLabel(descLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
            descLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            questBox.AddChild(descLabel);

            // Progress
            if (!state.IsComplete && def.TargetCount > 1)
            {
                var progressLabel = new Label();
                progressLabel.Text = $"Progress: {state.Progress}/{def.TargetCount}";
                UiTheme.StyleLabel(progressLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Small);
                questBox.AddChild(progressLabel);
            }

            // Rewards
            var rewardLabel = new Label();
            rewardLabel.Text = $"Reward: {def.GoldReward}g + {def.XpReward} XP";
            UiTheme.StyleLabel(rewardLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Small);
            questBox.AddChild(rewardLabel);

            ScrollContent.AddChild(questBox);

            if (i < tracker.ActiveQuests.Count - 1)
                ScrollContent.AddChild(new HSeparator());
        }

        // Fall back to bottom-row buttons (Refresh/Close) if no claim buttons exist.
        UiTheme.FocusFirstButtonOrFallback(ScrollContent, ContentBox);
    }
}
