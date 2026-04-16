using Godot;
using System;
using System.Collections.Generic;

namespace DungeonGame.Ui;

/// <summary>
/// Reusable tab system for GameWindow subclasses.
/// Handles tab bar UI, Q/E switching, active tab styling,
/// and content panel swapping. Drop into any GameWindow.
///
/// Usage:
///   var tabs = new GameTabPanel();
///   tabs.AddTab("Body", () => BuildBodyContent());
///   tabs.AddTab("Mind", () => BuildMindContent());
///   content.AddChild(tabs);       // add to GameWindow content
///   tabs.SelectTab(0);            // show first tab
///
/// Tab switching via Q/E:
///   protected override bool HandleTabInput(InputEvent e) => _tabs.HandleInput(e);
/// </summary>
public partial class GameTabPanel : VBoxContainer
{
    private HBoxContainer _tabBar = null!;
    private ScrollContainer _scroll = null!;
    private VBoxContainer _scrollContent = null!;

    private readonly List<TabDef> _tabs = new();
    private Button[] _tabButtons = Array.Empty<Button>();
    private int _currentTab = -1;

    /// <summary>The scroll content container — subclasses populate this in their tab builder.</summary>
    public VBoxContainer ScrollContent => _scrollContent;

    /// <summary>The scroll container — for resetting scroll position.</summary>
    public ScrollContainer Scroll => _scroll;

    /// <summary>Current active tab index.</summary>
    public int CurrentTab => _currentTab;

    /// <summary>Number of tabs.</summary>
    public int TabCount => _tabs.Count;

    public override void _Ready()
    {
        _tabBar = new HBoxContainer();
        _tabBar.AddThemeConstantOverride("separation", 0);
        _tabBar.Alignment = BoxContainer.AlignmentMode.Center;
        AddChild(_tabBar);

        var tabHint = new Label();
        tabHint.Text = "[Q] / [E] switch tabs | [D] close";
        UiTheme.StyleLabel(tabHint, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        tabHint.HorizontalAlignment = HorizontalAlignment.Center;
        AddChild(tabHint);

        AddChild(new HSeparator());

        _scroll = new ScrollContainer { FollowFocus = true };
        _scroll.CustomMinimumSize = new Vector2(0, 300);
        _scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        AddChild(_scroll);

        _scrollContent = new VBoxContainer();
        _scrollContent.AddThemeConstantOverride("separation", 4);
        _scroll.AddChild(_scrollContent);

        RebuildTabBar();
    }

    /// <summary>Add a tab. Call before the node is added to the tree, or call RebuildTabBar() after.</summary>
    public void AddTab(string label, Action buildContent)
    {
        _tabs.Add(new TabDef { Label = label, BuildContent = buildContent });
    }

    /// <summary>Rebuild the tab bar buttons. Called automatically in _Ready, or manually after adding tabs.</summary>
    public void RebuildTabBar()
    {
        if (_tabBar == null) return;

        foreach (Node child in _tabBar.GetChildren())
            child.QueueFree();

        _tabButtons = new Button[_tabs.Count];
        for (int i = 0; i < _tabs.Count; i++)
        {
            var btn = new Button();
            btn.Text = _tabs[i].Label;
            btn.CustomMinimumSize = new Vector2(0, 28);
            btn.FocusMode = FocusModeEnum.None;
            int idx = i;
            btn.Connect(BaseButton.SignalName.Pressed, Callable.From(() => SelectTab(idx)));
            _tabBar.AddChild(btn);
            _tabButtons[i] = btn;
        }
    }

    /// <summary>Select and display a tab by index.</summary>
    public void SelectTab(int index)
    {
        if (index < 0 || index >= _tabs.Count) return;
        _currentTab = index;

        // Style tabs
        for (int i = 0; i < _tabButtons.Length; i++)
        {
            bool active = i == index;
            _tabButtons[i].AddThemeStyleboxOverride("normal", UiTheme.CreateTabStyle(active));
            _tabButtons[i].AddThemeStyleboxOverride("hover", UiTheme.CreateTabStyle(active));
            _tabButtons[i].AddThemeStyleboxOverride("focus", UiTheme.CreateTabStyle(active));
            _tabButtons[i].AddThemeColorOverride("font_color", active ? UiTheme.Colors.BgDark : UiTheme.Colors.Muted);
            _tabButtons[i].AddThemeColorOverride("font_hover_color", active ? UiTheme.Colors.BgDark : UiTheme.Colors.Ink);
            _tabButtons[i].AddThemeFontSizeOverride("font_size", UiTheme.FontSizes.Small);
        }

        // Clear and rebuild content
        foreach (Node child in _scrollContent.GetChildren())
            child.QueueFree();

        _tabs[index].BuildContent?.Invoke();

        _scroll.ScrollVertical = 0;
        CallDeferred(MethodName.FocusFirst);
    }

    /// <summary>Handle Q/E tab switching. Call from GameWindow.HandleTabInput.</summary>
    public bool HandleInput(InputEvent @event)
    {
        if (_tabs.Count <= 1) return false;

        if (@event.IsActionPressed(Constants.InputActions.ShoulderLeft))
        {
            SelectTab((_currentTab - 1 + _tabs.Count) % _tabs.Count);
            return true;
        }
        if (@event.IsActionPressed(Constants.InputActions.ShoulderRight))
        {
            SelectTab((_currentTab + 1) % _tabs.Count);
            return true;
        }
        return false;
    }

    /// <summary>Update a tab's label text.</summary>
    public void SetTabLabel(int index, string label)
    {
        if (index >= 0 && index < _tabs.Count)
        {
            _tabs[index] = _tabs[index] with { Label = label };
            if (index < _tabButtons.Length)
                _tabButtons[index].Text = label;
        }
    }

    private void FocusFirst() => UiTheme.FocusFirstButton(_scrollContent);

    private record struct TabDef
    {
        public string Label { get; set; }
        public Action? BuildContent { get; set; }
    }
}
