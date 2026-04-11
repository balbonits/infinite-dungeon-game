using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Quest panel for the Adventure Guild NPC.
/// Shows active quests, progress, and rewards.
/// Allows claiming completed quests and generating new ones.
/// </summary>
public partial class QuestPanel : Control
{
    public static QuestPanel Instance { get; private set; } = null!;

    private ColorRect _overlay = null!;
    private CenterContainer _center = null!;
    private VBoxContainer _questList = null!;
    private Label _headerLabel = null!;
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
        panel.CustomMinimumSize = new Vector2(400, 0);
        _center.AddChild(panel);

        var margin = new MarginContainer();
        panel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);
        margin.AddChild(vbox);

        var title = new Label();
        title.Text = Strings.Quests.Title;
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        _headerLabel = new Label();
        UiTheme.StyleLabel(_headerLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        _headerLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(_headerLabel);

        vbox.AddChild(new HSeparator());

        var scroll = new ScrollContainer();
        scroll.CustomMinimumSize = new Vector2(0, 280);
        vbox.AddChild(scroll);

        _questList = new VBoxContainer();
        _questList.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(_questList);

        // Bottom buttons
        var bottomRow = new HBoxContainer();
        bottomRow.AddThemeConstantOverride("separation", 12);
        bottomRow.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.AddChild(bottomRow);

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
        if (_isOpen) return;
        _isOpen = true;
        GetTree().Paused = true;

        // Auto-generate quests if none exist
        var tracker = GameState.Instance.Quests;
        if (tracker.ActiveQuests.Count == 0)
            tracker.GenerateQuests(GameState.Instance.FloorNumber);

        Refresh();
        _overlay.Visible = true;
        _center.Visible = true;
    }

    public void Close()
    {
        _isOpen = false;
        GetTree().Paused = false;
        _overlay.Visible = false;
        _center.Visible = false;
    }

    private void Refresh()
    {
        foreach (Node child in _questList.GetChildren())
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

            _questList.AddChild(questBox);

            if (i < tracker.ActiveQuests.Count - 1)
                _questList.AddChild(new HSeparator());
        }

        UiTheme.FocusFirstButton(_questList);
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

        if (KeyboardNav.HandleInput(@event, _questList))
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        if (KeyboardNav.ConsumeMovement(@event))
            GetViewport().SetInputAsHandled();
    }
}
