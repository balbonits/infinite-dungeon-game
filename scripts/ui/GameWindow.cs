using Godot;
using System;

namespace DungeonGame.Ui;

/// <summary>
/// Reusable game window base. Handles:
/// - Dark overlay + centered panel + content VBox
/// - WindowStack push/pop
/// - Input blocking (cancel, keyboard nav, block remaining)
/// - Open/close with optional pause/unpause
/// - Optional return-to-PauseMenu on close
///
/// Usage: subclass and override BuildContent(VBoxContainer content),
/// or use GameWindow.Create() for one-off windows.
/// </summary>
public partial class GameWindow : Control
{
    private ColorRect _overlay = null!;
    private VBoxContainer _contentBox = null!;
    private bool _isOpen;
    private bool _pauseOnOpen;
    private bool _returnToPauseMenu;
    private Action? _onClose;

    public bool IsOpen => _isOpen;
    public VBoxContainer Content => _contentBox;

    /// <summary>Create a GameWindow, add it to UILayer, and open it.</summary>
    public static GameWindow Create(float width = 420f, bool pauseOnOpen = true,
        bool returnToPauseMenu = false, Action? onClose = null)
    {
        var win = new GameWindow();
        win._pauseOnOpen = pauseOnOpen;
        win._returnToPauseMenu = returnToPauseMenu;
        win._onClose = onClose;
        win._width = width;

        var uiLayer = win.GetUILayer();
        if (uiLayer != null)
            uiLayer.AddChild(win);
        return win;
    }

    private float _width = 420f;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var (overlay, content) = UiTheme.CreateDialogWindow(_width);
        _overlay = overlay;
        _contentBox = content;
        AddChild(_overlay);

        _isOpen = true;
        WindowStack.Push(this);
        if (_pauseOnOpen)
            GetTree().Paused = true;

        BuildContent(_contentBox);
    }

    /// <summary>Override to populate the window content.</summary>
    protected virtual void BuildContent(VBoxContainer content) { }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;
        WindowStack.Pop(this);

        if (_returnToPauseMenu)
        {
            var pauseMenu = GetTree().Root.GetNodeOrNull<Control>("Main/UILayer/PauseMenu");
            if (pauseMenu != null)
            {
                pauseMenu.Visible = true;
                UiTheme.FocusFirstButton(pauseMenu.GetNode<VBoxContainer>(
                    "CenterContainer/PanelContainer/MarginContainer/VBoxContainer"));
            }
            else
            {
                GetTree().Paused = false;
            }
        }
        else if (_pauseOnOpen)
        {
            GetTree().Paused = false;
        }

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

        if (HandleWindowInput(@event))
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventKey k && k.Pressed)
            GetViewport().SetInputAsHandled();
    }

    /// <summary>Override for custom input handling (tab switching, etc.). Return true if handled.</summary>
    protected virtual bool HandleWindowInput(InputEvent @event) => false;

    private Node? GetUILayer()
    {
        return GetTree()?.Root?.GetNodeOrNull("Main/UILayer");
    }
}
