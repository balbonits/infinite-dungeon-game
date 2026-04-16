using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Dungeon Ledger UI — the achievement browser.
/// Accessible from pause menu. Shows all achievements by category
/// with progress bars and unlock status.
/// </summary>
public partial class DungeonLedger : Control
{
    public static DungeonLedger Instance { get; private set; } = null!;

    private ColorRect _overlay = null!;
    private CenterContainer _center = null!;
    private ScrollContainer _scrollContainer = null!;
    private VBoxContainer _achievementList = null!;
    private Label _countLabel = null!;
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
        panel.CustomMinimumSize = new Vector2(450, 0);
        _center.AddChild(panel);

        var margin = new MarginContainer();
        panel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        margin.AddChild(vbox);

        var title = new Label();
        title.Text = Strings.Achievements.Title;
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        _countLabel = new Label();
        UiTheme.StyleLabel(_countLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Body);
        _countLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(_countLabel);

        vbox.AddChild(new HSeparator());

        _scrollContainer = new ScrollContainer { FollowFocus = true };
        _scrollContainer.CustomMinimumSize = new Vector2(0, 360);
        vbox.AddChild(_scrollContainer);

        _achievementList = new VBoxContainer();
        _achievementList.AddThemeConstantOverride("separation", 4);
        _scrollContainer.AddChild(_achievementList);

        var closeBtn = new Button();
        closeBtn.Text = Strings.Ui.Cancel;
        closeBtn.CustomMinimumSize = new Vector2(200, 38);
        closeBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        UiTheme.StyleSecondaryButton(closeBtn, UiTheme.FontSizes.Body);
        closeBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(() => Close()));
        vbox.AddChild(closeBtn);
    }

    public new void Show()
    {
        if (_isOpen) return;
        _isOpen = true;
        WindowStack.Push(this);
        GetTree().Paused = true;
        Refresh();
        _overlay.Visible = true;
        _center.Visible = true;
    }

    public void Close()
    {
        _isOpen = false;
        WindowStack.Pop(this);
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

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_isOpen) return;

        if (KeyboardNav.IsCancelPressed(@event))
        {
            Close();
            GetViewport().SetInputAsHandled();
            return;
        }

        // Scroll with Up/Down arrows
        if (@event.IsActionPressed(Constants.InputActions.MoveUp))
        {
            _scrollContainer.ScrollVertical -= 40;
            GetViewport().SetInputAsHandled();
            return;
        }
        if (@event.IsActionPressed(Constants.InputActions.MoveDown))
        {
            _scrollContainer.ScrollVertical += 40;
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventKey k && k.Pressed)
            GetViewport().SetInputAsHandled();
    }
}
