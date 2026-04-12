using Godot;

namespace DungeonGame.Ui;

/// <summary>
/// Static tutorial reference. Shows keybindings, combat basics, and game info.
/// No guided tour — just a readable reference the player can open anytime.
/// </summary>
public partial class TutorialPanel : Control
{
    public static TutorialPanel? ActiveInstance { get; private set; }

    private bool _isOpen;
    private int _currentTab;

    private ScrollContainer _scrollContainer = null!;
    private VBoxContainer _content = null!;
    private readonly Button[] _tabButtons = new Button[4];

    private static readonly string[] TabNames = { "Movement", "Combat", "Menus", "Town" };

    public bool IsOpen => _isOpen;

    public static TutorialPanel Open(Node parent, System.Action? onClose = null)
    {
        var help = new TutorialPanel();
        help._onClose = onClose;
        // Add to UILayer (sibling level) so input doesn't bleed to parent
        var uiLayer = parent.GetTree().Root.GetNode("Main/UILayer");
        uiLayer.AddChild(help);
        return help;
    }

    private System.Action? _onClose;

    public override void _Ready()
    {
        ActiveInstance = this;
        _isOpen = true;
        WindowStack.Push(this);
        ProcessMode = ProcessModeEnum.Always;
        AnchorsPreset = (int)LayoutPreset.FullRect;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        Size = GetViewportRect().Size;

        var (overlay, vbox) = UiTheme.CreateDialogWindow(560f, 0.7f);
        AddChild(overlay);

        // Title
        var title = new Label();
        title.Text = "TUTORIAL";
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        // Tabs
        var tabBar = new HBoxContainer();
        tabBar.AddThemeConstantOverride("separation", 0);
        tabBar.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.AddChild(tabBar);

        for (int i = 0; i < TabNames.Length; i++)
        {
            var btn = new Button();
            btn.Text = TabNames[i];
            btn.CustomMinimumSize = new Vector2(0, 28);
            btn.FocusMode = FocusModeEnum.None;
            int idx = i;
            btn.Connect(BaseButton.SignalName.Pressed, Callable.From(() => BuildTab(idx)));
            tabBar.AddChild(btn);
            _tabButtons[i] = btn;
        }

        string lKey = GetKey(Constants.InputActions.ShoulderLeft);
        string rKey = GetKey(Constants.InputActions.ShoulderRight);
        var tabHint = new Label();
        tabHint.Text = $"[{lKey}] / [{rKey}] switch tabs | [D] close";
        UiTheme.StyleLabel(tabHint, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        tabHint.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(tabHint);

        vbox.AddChild(new HSeparator());

        _scrollContainer = new ScrollContainer();
        _scrollContainer.CustomMinimumSize = new Vector2(0, 400);
        vbox.AddChild(_scrollContainer);

        _content = new VBoxContainer();
        _content.AddThemeConstantOverride("separation", 6);
        _scrollContainer.AddChild(_content);

        BuildTab(0);
    }

    private void BuildTab(int tab)
    {
        _currentTab = tab;

        for (int i = 0; i < _tabButtons.Length; i++)
        {
            bool active = i == tab;
            _tabButtons[i].AddThemeStyleboxOverride("normal", CreateTabStyle(active));
            _tabButtons[i].AddThemeStyleboxOverride("hover", CreateTabStyle(active));
            _tabButtons[i].AddThemeColorOverride("font_color", active ? UiTheme.Colors.BgDark : UiTheme.Colors.Muted);
            _tabButtons[i].AddThemeColorOverride("font_hover_color", active ? UiTheme.Colors.BgDark : UiTheme.Colors.Ink);
            _tabButtons[i].AddThemeFontSizeOverride("font_size", UiTheme.FontSizes.Body);
        }

        foreach (Node child in _content.GetChildren())
            child.QueueFree();

        switch (tab)
        {
            case 0: BuildMovementTab(); break;
            case 1: BuildCombatTab(); break;
            case 2: BuildMenusTab(); break;
            case 3: BuildTownTab(); break;
        }
    }

    private void BuildMovementTab()
    {
        AddSection("Getting Around");
        AddBinding("Move", "Arrow Keys");
        AddBinding("Walk into stairs", "Enter next floor / return to town");
        AddNote("Movement is screen-space — Up goes up, not diagonal.");

        AddSection("Map");
        AddBinding(GetKey(Constants.InputActions.MapToggle), "Toggle map overlay");
        AddNote("Map cycles: overlay → full → off.");
    }

    private void BuildCombatTab()
    {
        AddSection("Auto-Attack");
        AddNote("Your character auto-attacks the nearest enemy in range.");
        AddNote("Warriors: staff/sword melee. Rangers: arrow shot. Mages: staff melee.");

        AddSection("Skills (Hotbar)");
        string l = GetKey(Constants.InputActions.ShoulderLeft);
        string r = GetKey(Constants.InputActions.ShoulderRight);
        string tri = GetKey(Constants.InputActions.ActionTriangle);
        string cross = GetKey(Constants.InputActions.ActionCross);
        AddBinding($"{l}+{tri}", "Skill Slot 1");
        AddBinding($"{l}+{cross}", "Skill Slot 2");
        AddBinding($"{r}+{tri}", "Skill Slot 3");
        AddBinding($"{r}+{cross}", "Skill Slot 4");
        AddNote("Assign skills from the Skill Tree (Pause → Skills → ▶ button).");
        AddNote("Skills cost mana. Mana regenerates over time (INT stat).");

        AddSection("Targeting");
        AddNote("Skills auto-target the nearest enemy within the skill's range.");
        AddNote("AoE skills hit all enemies in the area. Self skills need no target.");
    }

    private void BuildMenusTab()
    {
        AddSection("Core Controls");
        AddBinding(GetKey(Constants.InputActions.ActionCross), "Confirm / Select / Press button");
        AddBinding(GetKey(Constants.InputActions.ActionCircle), "Cancel / Back / Close panel");
        AddBinding("Esc", "Pause menu");
        AddBinding("Arrow Keys", "Navigate menus");

        AddSection("Pause Menu");
        AddNote("Resume, Backpack, Stats, Skills, Fated Ledger, Settings.");
        AddNote("Backpack is accessible anywhere — dungeon or town.");

        AddSection("Tab Navigation");
        AddBinding(GetKey(Constants.InputActions.ShoulderLeft), "Previous tab");
        AddBinding(GetKey(Constants.InputActions.ShoulderRight), "Next tab");
        AddNote("Used in: Skills, Settings, and other tabbed windows.");

        AddSection("NPC Interaction");
        AddBinding(GetKey(Constants.InputActions.ActionCross), "Open NPC service (walk close first)");
    }

    private void BuildTownTab()
    {
        AddSection("Town NPCs");
        AddBinding("Item Shop", "Buy potions, scrolls, backpack expansions");
        AddBinding("Blacksmith", "Craft affixes onto gear, recycle unwanted items");
        AddBinding("Adventure Guild", "View and claim quests");
        AddBinding("Level Teleporter", "Travel to previously visited floors");
        AddBinding("Banker", "Safe storage — items here survive death");

        AddSection("Dungeon Entrance");
        AddNote("Walk south to the cave mouth to enter the dungeon.");
        AddNote("You start on floor 1. Descend via stairs on each floor.");

        AddSection("Death");
        AddNote("On death: lose XP, risk losing backpack items.");
        AddNote("Bank items are always safe. Buy Sacrificial Idols for protection.");
    }

    // --- Helpers ---

    private void AddSection(string title)
    {
        if (_content.GetChildCount() > 0)
            _content.AddChild(new HSeparator());
        var lbl = new Label();
        lbl.Text = title;
        UiTheme.StyleLabel(lbl, UiTheme.Colors.Accent, UiTheme.FontSizes.Body);
        _content.AddChild(lbl);
    }

    private void AddBinding(string key, string action)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);

        var keyLabel = new Label();
        keyLabel.Text = key;
        keyLabel.CustomMinimumSize = new Vector2(120, 0);
        UiTheme.StyleLabel(keyLabel, UiTheme.Colors.Info, UiTheme.FontSizes.Body);
        row.AddChild(keyLabel);

        var actionLabel = new Label();
        actionLabel.Text = action;
        UiTheme.StyleLabel(actionLabel, UiTheme.Colors.Ink, UiTheme.FontSizes.Body);
        actionLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(actionLabel);

        _content.AddChild(row);
    }

    private void AddNote(string text)
    {
        var lbl = new Label();
        lbl.Text = $"  {text}";
        UiTheme.StyleLabel(lbl, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        lbl.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _content.AddChild(lbl);
    }

    public void Close()
    {
        _isOpen = false;
        WindowStack.Pop(this);
        ActiveInstance = null;
        _onClose?.Invoke();
        QueueFree();
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
            BuildTab((_currentTab - 1 + TabNames.Length) % TabNames.Length);
            GetViewport().SetInputAsHandled();
            return;
        }
        if (@event.IsActionPressed(Constants.InputActions.ShoulderRight))
        {
            BuildTab((_currentTab + 1) % TabNames.Length);
            GetViewport().SetInputAsHandled();
            return;
        }

        // Scroll with arrow keys
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

    private static string GetKey(string action)
    {
        var events = InputMap.ActionGetEvents(action);
        foreach (var ev in events)
            if (ev is InputEventKey keyEv)
                return OS.GetKeycodeString(keyEv.Keycode);
        return "?";
    }

    private static StyleBoxFlat CreateTabStyle(bool active)
    {
        var style = new StyleBoxFlat();
        style.BgColor = active ? UiTheme.Colors.Action : new Color(UiTheme.Colors.BgPanel, 0.6f);
        style.BorderColor = active ? UiTheme.Colors.Action : new Color(UiTheme.Colors.Muted, 0.3f);
        style.SetBorderWidthAll(1);
        style.BorderWidthBottom = active ? 3 : 1;
        style.CornerRadiusTopLeft = 4;
        style.CornerRadiusTopRight = 4;
        style.ContentMarginLeft = 8;
        style.ContentMarginRight = 8;
        style.ContentMarginTop = 4;
        style.ContentMarginBottom = 4;
        return style;
    }
}
