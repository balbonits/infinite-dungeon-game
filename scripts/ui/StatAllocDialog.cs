using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Stat allocation dialog. Spend free stat points on STR/DEX/STA/INT.
/// Opens from pause menu or level-up notification.
/// </summary>
public partial class StatAllocDialog : Control
{
    public static StatAllocDialog Instance { get; private set; } = null!;

    private ColorRect _overlay = null!;
    private CenterContainer _center = null!;
    private VBoxContainer _content = null!;
    private Label _freePointsLabel = null!;
    private bool _isOpen;

    public bool IsOpen => _isOpen;

    private static readonly (string name, string description)[] StatInfo =
    {
        ("STR", "Melee damage"),
        ("DEX", "Attack speed, dodge"),
        ("STA", "Max HP, HP regen"),
        ("INT", "Spell damage, mana"),
    };

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
        MouseFilter = MouseFilterEnum.Ignore;

        _overlay = new ColorRect();
        _overlay.Color = new Color(0, 0, 0, 0.5f);
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
        panel.CustomMinimumSize = new Vector2(340, 0);
        _center.AddChild(panel);

        var margin = new MarginContainer();
        panel.AddChild(margin);

        _content = new VBoxContainer();
        _content.AddThemeConstantOverride("separation", 10);
        margin.AddChild(_content);
    }

    public new void Show()
    {
        if (_isOpen) return;
        _isOpen = true;
        GetTree().Paused = true;
        Rebuild();
        _overlay.Visible = true;
        _center.Visible = true;
    }

    private void Rebuild()
    {
        foreach (Node child in _content.GetChildren())
            child.QueueFree();

        var stats = GameState.Instance.Stats;

        // Title
        var title = new Label();
        title.Text = Strings.Stats.Title;
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        _content.AddChild(title);

        // Free points
        _freePointsLabel = new Label();
        _freePointsLabel.Text = Strings.Stats.FreePoints(stats.FreePoints);
        UiTheme.StyleLabel(_freePointsLabel, UiTheme.Colors.Safe, UiTheme.FontSizes.Button);
        _freePointsLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _content.AddChild(_freePointsLabel);

        _content.AddChild(new HSeparator());

        // Stat rows
        AddStatRow("STR", stats.Str, StatInfo[0].description, () => { stats.Str++; stats.FreePoints--; OnStatChanged(); });
        AddStatRow("DEX", stats.Dex, StatInfo[1].description, () => { stats.Dex++; stats.FreePoints--; OnStatChanged(); });
        AddStatRow("STA", stats.Sta, StatInfo[2].description, () => { stats.Sta++; stats.FreePoints--; OnStatChanged(); });
        AddStatRow("INT", stats.Int, StatInfo[3].description, () => { stats.Int++; stats.FreePoints--; OnStatChanged(); });

        _content.AddChild(new HSeparator());

        // Close button
        var closeBtn = new Button();
        closeBtn.Text = Strings.Ui.Cancel;
        closeBtn.CustomMinimumSize = new Vector2(140, 38);
        closeBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        UiTheme.StyleSecondaryButton(closeBtn, UiTheme.FontSizes.Body);
        closeBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(Close));
        _content.AddChild(closeBtn);
    }

    private void AddStatRow(string name, int value, string desc, System.Action onAllocate)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);

        var nameLabel = new Label();
        nameLabel.Text = $"{name}: {value}";
        nameLabel.CustomMinimumSize = new Vector2(80, 0);
        UiTheme.StyleLabel(nameLabel, UiTheme.Colors.Ink, UiTheme.FontSizes.Button);
        row.AddChild(nameLabel);

        var effLabel = new Label();
        effLabel.Text = $"({StatBlock.GetEffective(value):F0} eff)";
        effLabel.CustomMinimumSize = new Vector2(70, 0);
        UiTheme.StyleLabel(effLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Body);
        row.AddChild(effLabel);

        var descLabel = new Label();
        descLabel.Text = desc;
        descLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        UiTheme.StyleLabel(descLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        row.AddChild(descLabel);

        var addBtn = new Button();
        addBtn.Text = "+";
        addBtn.CustomMinimumSize = new Vector2(36, 36);
        addBtn.Disabled = GameState.Instance.Stats.FreePoints <= 0;
        addBtn.FocusMode = FocusModeEnum.All;
        addBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(onAllocate));
        row.AddChild(addBtn);

        _content.AddChild(row);
    }

    private void OnStatChanged()
    {
        // Recalculate MaxHp with new STA
        var gs = GameState.Instance;
        gs.MaxHp = Constants.PlayerStats.GetMaxHp(gs.Level) + gs.Stats.BonusMaxHp;
        gs.EmitSignal(GameState.SignalName.StatsChanged);
        Rebuild();
        UiTheme.FocusFirstButton(_content);
    }

    private void Close()
    {
        _isOpen = false;
        _overlay.Visible = false;
        _center.Visible = false;
        GetTree().Paused = false;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_isOpen) return;

        if (KeyboardNav.HandleInput(@event, _content))
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
    }
}
