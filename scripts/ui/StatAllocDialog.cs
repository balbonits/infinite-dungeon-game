using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Stat allocation dialog. Spend free stat points on STR/DEX/STA/INT.
/// Opens from pause menu or level-up notification.
/// </summary>
public partial class StatAllocDialog : GameWindow
{
    public static StatAllocDialog Instance { get; private set; } = null!;

    private Label _freePointsLabel = null!;

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
        WindowWidth = 340f;
        base._Ready();
    }

    protected override void BuildContent(VBoxContainer content)
    {
        content.AddThemeConstantOverride("separation", 10);
        // ContentBox is dynamically populated by Rebuild in OnShow
    }

    protected override void OnShow()
    {
        Rebuild();
    }

    private void Rebuild()
    {
        foreach (Node child in ContentBox.GetChildren())
            child.QueueFree();

        var stats = GameState.Instance.Stats;

        // Title
        var title = new Label();
        title.Text = Strings.Stats.Title;
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        ContentBox.AddChild(title);

        // Free points
        _freePointsLabel = new Label();
        _freePointsLabel.Text = Strings.Stats.FreePoints(stats.FreePoints);
        UiTheme.StyleLabel(_freePointsLabel, UiTheme.Colors.Safe, UiTheme.FontSizes.Button);
        _freePointsLabel.HorizontalAlignment = HorizontalAlignment.Center;
        ContentBox.AddChild(_freePointsLabel);

        ContentBox.AddChild(new HSeparator());

        // Stat rows
        AddStatRow("STR", stats.Str, StatInfo[0].description, () => { stats.Str++; stats.FreePoints--; OnStatChanged(); });
        AddStatRow("DEX", stats.Dex, StatInfo[1].description, () => { stats.Dex++; stats.FreePoints--; OnStatChanged(); });
        AddStatRow("STA", stats.Sta, StatInfo[2].description, () => { stats.Sta++; stats.FreePoints--; OnStatChanged(); });
        AddStatRow("INT", stats.Int, StatInfo[3].description, () => { stats.Int++; stats.FreePoints--; OnStatChanged(); });

        ContentBox.AddChild(new HSeparator());

        // Close button
        var closeBtn = new Button();
        closeBtn.Text = Strings.Ui.Cancel;
        closeBtn.CustomMinimumSize = new Vector2(140, 38);
        closeBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        UiTheme.StyleSecondaryButton(closeBtn, UiTheme.FontSizes.Body);
        closeBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(Close));
        ContentBox.AddChild(closeBtn);
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

        ContentBox.AddChild(row);
    }

    private void OnStatChanged()
    {
        // COMBAT-01 §5: unified recompute covers MaxHp + MaxMana and folds
        // in equipment overlays — replaces the stat-only recomputation that
        // used to live inline here.
        var gs = GameState.Instance;
        gs.RecomputeDerivedStats();
        gs.EmitSignal(GameState.SignalName.StatsChanged);
        Rebuild();
        UiTheme.FocusFirstButton(ContentBox);
    }

}
