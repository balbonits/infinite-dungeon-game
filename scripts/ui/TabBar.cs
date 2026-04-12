using Godot;
using System;

namespace DungeonGame.Ui;

/// <summary>
/// Reusable tab bar component. Creates styled tab buttons in a row.
/// Q/E to switch tabs. Active tab highlighted. Calls onTabChanged when switched.
/// </summary>
public partial class TabBar : HBoxContainer
{
    private Button[] _buttons = Array.Empty<Button>();
    private int _currentTab;
    private Action<int>? _onTabChanged;

    public int CurrentTab => _currentTab;

    public static TabBar Create(string[] tabNames, Action<int> onTabChanged)
    {
        var bar = new TabBar();
        bar._onTabChanged = onTabChanged;
        bar.AddThemeConstantOverride("separation", 0);
        bar.Alignment = AlignmentMode.Center;

        bar._buttons = new Button[tabNames.Length];
        for (int i = 0; i < tabNames.Length; i++)
        {
            var btn = new Button();
            btn.Text = tabNames[i];
            btn.CustomMinimumSize = new Vector2(0, 28);
            btn.FocusMode = FocusModeEnum.None; // Q/E only, not in nav
            int idx = i;
            btn.Connect(BaseButton.SignalName.Pressed, Callable.From(() => bar.SetTab(idx)));
            bar.AddChild(btn);
            bar._buttons[i] = btn;
        }

        bar.StyleTabs(0);
        return bar;
    }

    public void SetTab(int index)
    {
        _currentTab = index;
        StyleTabs(index);
        _onTabChanged?.Invoke(index);
    }

    /// <summary>Handle Q/E tab switching. Call from parent's input handler. Returns true if handled.</summary>
    public bool HandleTabInput(InputEvent @event)
    {
        if (@event.IsActionPressed(Constants.InputActions.ShoulderLeft))
        {
            SetTab((_currentTab - 1 + _buttons.Length) % _buttons.Length);
            return true;
        }
        if (@event.IsActionPressed(Constants.InputActions.ShoulderRight))
        {
            SetTab((_currentTab + 1) % _buttons.Length);
            return true;
        }
        return false;
    }

    private void StyleTabs(int activeIndex)
    {
        for (int i = 0; i < _buttons.Length; i++)
        {
            bool active = i == activeIndex;
            _buttons[i].AddThemeStyleboxOverride("normal", CreateTabStyle(active));
            _buttons[i].AddThemeStyleboxOverride("hover", CreateTabStyle(active));
            _buttons[i].AddThemeStyleboxOverride("focus", CreateTabStyle(active));
            _buttons[i].AddThemeColorOverride("font_color", active ? UiTheme.Colors.BgDark : UiTheme.Colors.Muted);
            _buttons[i].AddThemeColorOverride("font_hover_color", active ? UiTheme.Colors.BgDark : UiTheme.Colors.Ink);
            _buttons[i].AddThemeFontSizeOverride("font_size", UiTheme.FontSizes.Body);
        }
    }

    private static StyleBoxFlat CreateTabStyle(bool active)
    {
        var style = new StyleBoxFlat();
        style.BgColor = active ? UiTheme.Colors.Action : new Color(UiTheme.Colors.BgPanel, 0.6f);
        style.BorderColor = active ? UiTheme.Colors.Action : new Color(UiTheme.Colors.Muted, 0.3f);
        style.SetBorderWidthAll(1);
        style.BorderWidthBottom = active ? 3 : 1;
        style.SetCornerRadiusAll(0);
        style.CornerRadiusTopLeft = 4;
        style.CornerRadiusTopRight = 4;
        style.ContentMarginLeft = 8;
        style.ContentMarginRight = 8;
        style.ContentMarginTop = 4;
        style.ContentMarginBottom = 4;
        return style;
    }
}
