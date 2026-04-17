using Godot;
using System;

namespace DungeonGame.Ui;

/// <summary>
/// Static tutorial reference. Uses GameWindow + TabBar + ScrollList + ContentSection.
/// No guided tour — readable reference accessible from title screen and pause menu.
/// </summary>
public partial class TutorialPanel : GameWindow
{
    public static TutorialPanel? ActiveInstance { get; private set; }

    private TabBar _tabBar = null!;
    private ScrollList _scrollList = null!;
    private Action? _onClose;

    private static readonly string[] TabNames = { "Movement", "Combat", "Menus", "Town" };

    public static TutorialPanel Open(Node parent, Action? onClose = null)
    {
        var panel = new TutorialPanel();
        panel._onClose = onClose;
        var uiLayer = parent.GetTree().Root.GetNode("Main/UILayer");
        uiLayer.AddChild(panel);
        return panel;
    }

    public override void _Ready()
    {
        ActiveInstance = this;
        WindowWidth = 560f;
        base._Ready();

        // Auto-show on ready since this is dynamically created
        Show();
        BuildTab(0);
    }

    protected override void BuildContent(VBoxContainer content)
    {
        // Title
        var title = new Label();
        title.Text = "TUTORIAL";
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(title);

        // Tabs
        _tabBar = TabBar.Create(TabNames, BuildTab);
        content.AddChild(_tabBar);

        // Tab hint
        string lKey = GetKey(Constants.InputActions.ShoulderLeft);
        string rKey = GetKey(Constants.InputActions.ShoulderRight);
        var hint = new Label();
        hint.Text = $"[{lKey}] / [{rKey}] switch tabs | Esc close";
        UiTheme.StyleLabel(hint, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(hint);

        // Scrollable content
        _scrollList = ScrollList.Create(400f, 6);
        content.AddChild(_scrollList);
    }

    private void BuildTab(int tab)
    {
        _scrollList.Clear();
        var list = _scrollList.List;

        switch (tab)
        {
            case 0: BuildMovementTab(list); break;
            case 1: BuildCombatTab(list); break;
            case 2: BuildMenusTab(list); break;
            case 3: BuildTownTab(list); break;
        }

        _scrollList.ScrollToTop();
    }

    private void BuildMovementTab(VBoxContainer list)
    {
        ContentSection.AddHeading(list, "Getting Around");
        ContentSection.AddBinding(list, "Arrow Keys", "Move character");
        ContentSection.AddBinding(list, "Walk into stairs", "Enter next floor / return to town");
        ContentSection.AddNote(list, "Movement is screen-space — Up goes up, not diagonal.");
        ContentSection.AddDivider(list);

        ContentSection.AddHeading(list, "Map");
        ContentSection.AddBinding(list, GetKey(Constants.InputActions.MapToggle), "Toggle map overlay");
        ContentSection.AddNote(list, "Map cycles: overlay → full → off.");
    }

    private void BuildCombatTab(VBoxContainer list)
    {
        ContentSection.AddHeading(list, "Auto-Attack");
        ContentSection.AddNote(list, "Your character auto-attacks the nearest enemy in range.");
        ContentSection.AddNote(list, "Warriors: melee slash. Rangers: arrow shot. Mages: staff melee.");
        ContentSection.AddDivider(list);

        ContentSection.AddHeading(list, "Skills (Hotbar)");
        string l = GetKey(Constants.InputActions.ShoulderLeft);
        string r = GetKey(Constants.InputActions.ShoulderRight);
        string tri = GetKey(Constants.InputActions.ActionTriangle);
        string cross = GetKey(Constants.InputActions.ActionCross);
        ContentSection.AddBinding(list, $"{l}+{tri}", "Skill Slot 1");
        ContentSection.AddBinding(list, $"{l}+{cross}", "Skill Slot 2");
        ContentSection.AddBinding(list, $"{r}+{tri}", "Skill Slot 3");
        ContentSection.AddBinding(list, $"{r}+{cross}", "Skill Slot 4");
        ContentSection.AddNote(list, "Assign skills from the Skill Tree (Pause → Skills → ▶ button).");
        ContentSection.AddNote(list, "Skills cost mana. Mana regenerates over time (INT stat).");
        ContentSection.AddDivider(list);

        ContentSection.AddHeading(list, "Targeting");
        ContentSection.AddNote(list, "Skills auto-target the nearest enemy within the skill's range.");
        ContentSection.AddNote(list, "AoE skills hit all enemies in the area. Self skills need no target.");
    }

    private void BuildMenusTab(VBoxContainer list)
    {
        ContentSection.AddHeading(list, "Core Controls");
        ContentSection.AddBinding(list, GetKey(Constants.InputActions.ActionCross), "Confirm / Select / Press button");
        ContentSection.AddBinding(list, GetKey(Constants.InputActions.ActionCircle), "Cancel / Back / Close panel");
        ContentSection.AddBinding(list, "Esc", "Pause menu");
        ContentSection.AddBinding(list, "Arrow Keys", "Navigate menus");
        ContentSection.AddDivider(list);

        ContentSection.AddHeading(list, "Pause Menu");
        ContentSection.AddNote(list, "Resume, Backpack, Stats, Skills, Dungeon Ledger, Settings.");
        ContentSection.AddNote(list, "Backpack is accessible anywhere — dungeon or town.");
        ContentSection.AddDivider(list);

        ContentSection.AddHeading(list, "Tab Navigation");
        ContentSection.AddBinding(list, GetKey(Constants.InputActions.ShoulderLeft), "Previous tab");
        ContentSection.AddBinding(list, GetKey(Constants.InputActions.ShoulderRight), "Next tab");
        ContentSection.AddNote(list, "Used in: Skills, Settings, and other tabbed windows.");
        ContentSection.AddDivider(list);

        ContentSection.AddHeading(list, "NPC Interaction");
        ContentSection.AddBinding(list, GetKey(Constants.InputActions.ActionCross), "Open NPC service (walk close first)");
    }

    private void BuildTownTab(VBoxContainer list)
    {
        ContentSection.AddHeading(list, "Town NPCs");
        ContentSection.AddBinding(list, "Item Shop", "Buy potions, scrolls, backpack expansions");
        ContentSection.AddBinding(list, "Blacksmith", "Craft affixes onto gear, recycle items");
        ContentSection.AddBinding(list, "Adventure Guild", "View and claim quests");
        ContentSection.AddBinding(list, "Level Teleporter", "Travel to previously visited floors");
        ContentSection.AddBinding(list, "Banker", "Safe storage — items here survive death");
        ContentSection.AddDivider(list);

        ContentSection.AddHeading(list, "Dungeon Entrance");
        ContentSection.AddNote(list, "Walk south to the cave mouth to enter the dungeon.");
        ContentSection.AddNote(list, "You start on floor 1. Descend via stairs on each floor.");
        ContentSection.AddDivider(list);

        ContentSection.AddHeading(list, "Death");
        ContentSection.AddNote(list, "On death: lose XP, risk losing backpack items.");
        ContentSection.AddNote(list, "Bank items are always safe. Buy Sacrificial Idols for protection.");
    }

    public new void Close()
    {
        ActiveInstance = null;
        base.Close();
        _onClose?.Invoke();
        QueueFree();
    }

    protected override bool HandleTabInput(InputEvent @event)
    {
        return _tabBar.HandleTabInput(@event);
    }

    protected override bool HandleExtraInput(InputEvent @event)
    {
        return _scrollList.HandleScrollInput(@event);
    }

    private static string GetKey(string action)
    {
        var events = InputMap.ActionGetEvents(action);
        foreach (var ev in events)
            if (ev is InputEventKey keyEv)
                return OS.GetKeycodeString(keyEv.Keycode);
        return "?";
    }
}
