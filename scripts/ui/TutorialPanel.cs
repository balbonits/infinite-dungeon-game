using Godot;
using System;

namespace DungeonGame.Ui;

/// <summary>
/// Static tutorial reference. Uses GameWindow + TabBar + ScrollList + ContentSection.
/// No guided tour — readable reference accessible from title screen and pause menu.
/// </summary>
public partial class TutorialPanel : Control
{
    public static TutorialPanel? ActiveInstance { get; private set; }

    private bool _isOpen;
    private TabBar _tabBar = null!;
    private ScrollList _scrollList = null!;
    private Action? _onClose;

    private static readonly string[] TabNames = { "Movement", "Combat", "Menus", "Town" };

    public bool IsOpen => _isOpen;

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
        _isOpen = true;
        WindowStack.Push(this);
        ProcessMode = ProcessModeEnum.Always;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        Size = GetViewportRect().Size;

        var (overlay, content) = UiTheme.CreateDialogWindow(560f, 0.7f);
        AddChild(overlay);

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

        BuildTab(0);
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
        ContentSection.AddNote(list, "Resume, Backpack, Stats, Skills, Fated Ledger, Settings.");
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
        if (KeyboardNav.BlockIfNotTopmost(this, @event)) return;

        if (KeyboardNav.IsCancelPressed(@event))
        {
            Close();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (_tabBar.HandleTabInput(@event))
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        if (_scrollList.HandleScrollInput(@event))
        {
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
}
