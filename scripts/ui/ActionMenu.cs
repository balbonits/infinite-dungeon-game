using Godot;
using System;
using System.Collections.Generic;

namespace DungeonGame.Ui;

/// <summary>
/// FF/SNES-style contextual action menu. Appears as a small popup with a list of
/// actions when the player confirms on an item, skill, or interactable.
/// Keyboard navigable: Up/Down to select, S/Cross to confirm, D/Circle to cancel.
/// </summary>
public partial class ActionMenu : Control
{
    public static ActionMenu? Instance { get; private set; }

    private ColorRect _overlay = null!;
    private PanelContainer _panel = null!;
    private VBoxContainer _buttonList = null!;
    private bool _isOpen;

    private Action? _onClose;

    public bool IsOpen => _isOpen;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = true;

        _overlay = new ColorRect();
        _overlay.Color = new Color(0, 0, 0, 0.3f);
        _overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        _overlay.MouseFilter = MouseFilterEnum.Stop;
        _overlay.Visible = false;
        AddChild(_overlay);

        _panel = new PanelContainer();
        _panel.AddThemeStyleboxOverride("panel", CreateMenuStyle());
        _panel.Visible = false;
        _panel.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_panel);

        _buttonList = new VBoxContainer();
        _buttonList.AddThemeConstantOverride("separation", 2);
        _panel.AddChild(_buttonList);
    }

    /// <summary>
    /// Show the action menu at a position with the given actions.
    /// Each action is (label, callback). Menu closes after any action or cancel.
    /// </summary>
    public void Show(Vector2 position, (string label, Action action)[] actions, Action? onClose = null)
    {
        if (_isOpen) Close();

        _onClose = onClose;
        _isOpen = true;

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
                Close();
                capturedAction();
            }));
            _buttonList.AddChild(btn);
        }

        // Position the panel near the trigger
        _panel.Position = position;
        _overlay.Visible = true;
        _panel.Visible = true;

        // Focus first button on next frame (after buttons are in tree)
        CallDeferred(MethodName.FocusFirst);
    }

    private void FocusFirst()
    {
        UiTheme.FocusFirstButton(_buttonList);
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;
        _overlay.Visible = false;
        _panel.Visible = false;
        _onClose?.Invoke();
        _onClose = null;
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

        if (KeyboardNav.HandleInput(@event, _buttonList))
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        // Block all input while open
        if (@event is InputEventKey key && key.Pressed)
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
