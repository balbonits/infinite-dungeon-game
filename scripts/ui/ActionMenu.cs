using Godot;
using System;
using System.Collections.Generic;

namespace DungeonGame.Ui;

/// <summary>
/// FF/SNES-style contextual action menu. Appears as a small popup with a list of
/// actions when the player confirms on an item, skill, or interactable.
/// Keyboard navigable: Up/Down to select, S/Cross to confirm, D/Circle to cancel.
///
/// Extends GameWindow for lifecycle/WindowStack/input blocking, but uses custom
/// positioning (not the standard centered overlay panel).
/// </summary>
public partial class ActionMenu : GameWindow
{
    public static ActionMenu? Instance { get; private set; }

    private PanelContainer _panel = null!;
    private VBoxContainer _buttonList = null!;

    private Action? _onClose;

    public override void _Ready()
    {
        Instance = this;
        ReturnToPauseMenu = false;
        base._Ready();
        Visible = true;

        // Custom positioned panel (not using GameWindow's centered ContentBox)
        _panel = new PanelContainer();
        _panel.AddThemeStyleboxOverride("panel", CreateMenuStyle());
        _panel.Visible = false;
        _panel.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_panel);

        _buttonList = new VBoxContainer();
        _buttonList.AddThemeConstantOverride("separation", 2);
        _panel.AddChild(_buttonList);

        // Hide the standard centered content — we use our own positioned panel
        ContentBox.Visible = false;
    }

    /// <summary>
    /// Show the action menu at a position with the given actions.
    /// Each action is (label, callback). Menu closes after any action or cancel.
    /// </summary>
    public void Show(Vector2 position, (string label, Action action)[] actions, Action? onClose = null)
    {
        if (IsOpen) CloseMenu();

        _onClose = onClose;

        // Clear old buttons
        foreach (Node child in _buttonList.GetChildren())
            child.QueueFree();

        // Build action buttons
        foreach (var (label, action) in actions)
        {
            var btn = new Button();
            btn.Text = label;
            btn.CustomMinimumSize = new Vector2(140, 30);
            btn.FocusMode = FocusModeEnum.All;
            UiTheme.StyleButton(btn, UiTheme.FontSizes.Body);
            btn.Alignment = HorizontalAlignment.Left;

            Action capturedAction = action;
            btn.Connect(BaseButton.SignalName.Pressed, Callable.From(() =>
            {
                CloseMenu();
                capturedAction();
            }));
            _buttonList.AddChild(btn);
        }

        // Position the panel near the trigger
        _panel.Position = position;
        _panel.Visible = true;

        // GameWindow.Show() handles overlay visibility, WindowStack, pause
        Show();

        // Focus first button on next frame (after buttons are in tree)
        CallDeferred(MethodName.FocusFirst);
    }

    private void FocusFirst()
    {
        UiTheme.FocusFirstButton(_buttonList);
    }

    /// <summary>
    /// Close the menu and invoke the onClose callback.
    /// </summary>
    public void CloseMenu()
    {
        if (!IsOpen) return;
        _panel.Visible = false;
        var callback = _onClose;
        _onClose = null;
        Close();
        callback?.Invoke();
    }

    protected override bool HandleExtraInput(InputEvent @event)
    {
        if (KeyboardNav.HandleInput(@event, _buttonList))
            return true;

        return false;
    }

    /// <summary>
    /// Override to use CloseMenu (with _onClose callback) instead of plain Close.
    /// </summary>
    public override void _UnhandledInput(InputEvent @event)
    {
        if (!IsOpen) return;
        if (KeyboardNav.BlockIfNotTopmost(this, @event)) return;

        if (KeyboardNav.IsCancelPressed(@event))
        {
            CloseMenu();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (HandleExtraInput(@event))
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventKey k && k.Pressed)
            GetViewport().SetInputAsHandled();
    }

    private static StyleBoxFlat CreateMenuStyle()
    {
        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.06f, 0.06f, 0.10f, 0.95f);
        style.BorderColor = UiTheme.Colors.Accent;
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(4);
        style.ContentMarginLeft = 8;
        style.ContentMarginRight = 8;
        style.ContentMarginTop = 6;
        style.ContentMarginBottom = 6;
        return style;
    }
}
