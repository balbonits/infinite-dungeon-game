using Godot;
using System;

namespace DungeonGame.Ui;

/// <summary>
/// Base class for ALL game windows and dialogs. Handles:
/// - Dark overlay + centered panel + scrollable content
/// - WindowStack push/pop (input routing)
/// - Pause on open, unpause on close
/// - Cancel (D/Esc) to close
/// - Keyboard navigation (Up/Down + S confirm) within scrollable content
/// - Q/E tab switching (override HandleTabInput)
/// - Return to PauseMenu on close (optional)
/// - Block all key events so nothing bleeds through
///
/// Subclass and override: BuildContent(), OnShow(), OnRefresh(), HandleTabInput()
/// </summary>
public partial class GameWindow : Control
{
    protected ColorRect Overlay = null!;
    protected VBoxContainer ContentBox = null!;
    protected ScrollContainer Scroll = null!;
    protected VBoxContainer ScrollContent = null!;

    private bool _isOpen;
    private bool _returnToPauseMenu = true;
    private float _windowWidth = 440f;

    public bool IsOpen => _isOpen;

    /// <summary>Set in subclass constructor or _Ready before base._Ready.</summary>
    protected float WindowWidth { get => _windowWidth; set => _windowWidth = value; }
    protected bool ReturnToPauseMenu { get => _returnToPauseMenu; set => _returnToPauseMenu = value; }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        MouseFilter = MouseFilterEnum.Ignore;

        var (overlay, content) = UiTheme.CreateDialogWindow(_windowWidth);
        Overlay = overlay;
        ContentBox = content;
        Overlay.Theme = UiTheme.CreateGameTheme();
        Overlay.Visible = false;
        AddChild(Overlay);

        // Scrollable content area — subclasses add to ScrollContent
        Scroll = new ScrollContainer { FollowFocus = true };
        Scroll.CustomMinimumSize = new Vector2(0, 300);

        ScrollContent = new VBoxContainer();
        ScrollContent.AddThemeConstantOverride("separation", 4);
        Scroll.AddChild(ScrollContent);

        BuildContent(ContentBox);
    }

    public override void _ExitTree()
    {
        // Free scroll container if it was never added to the scene tree by a subclass
        if (Scroll is { } scroll && !scroll.IsInsideTree())
        {
            GD.Print($"[GameWindow] Freed orphaned Scroll in {GetType().Name}");
            scroll.Free();
        }
    }

    /// <summary>Override to build the window UI. Add Scroll to ContentBox when you need scrolling.</summary>
    protected virtual void BuildContent(VBoxContainer content) { }

    /// <summary>Override for logic when the window is shown (refresh data, etc.).</summary>
    protected virtual void OnShow() { }

    /// <summary>Override for Q/E tab handling. Return true if handled.</summary>
    protected virtual bool HandleTabInput(InputEvent @event) => false;

    /// <summary>Override for any additional input handling. Return true if handled.</summary>
    protected virtual bool HandleExtraInput(InputEvent @event) => false;

    public new void Show()
    {
        if (_isOpen) return;
        _isOpen = true;
        WindowStack.Push(this);
        GetTree().Paused = true;

        OnShow();
        Overlay.Visible = true;
        GD.Print($"[Window] Open: {GetType().Name} | Stack: {WindowStack.Count} | Paused: {GetTree().Paused}");
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;
        WindowStack.Pop(this);
        Overlay.Visible = false;
        GD.Print($"[Window] Close: {GetType().Name} | Stack: {WindowStack.Count} | Paused: {GetTree().Paused}");

        if (_returnToPauseMenu)
        {
            var pauseMenu = GetNodeOrNull<PauseMenu>("../PauseMenu");
            if (pauseMenu != null)
            {
                pauseMenu.Show();
                return;
            }
        }
        if (!WindowStack.HasModal)
            GetTree().Paused = false;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_isOpen) return;
        if (KeyboardNav.BlockIfNotTopmost(this, @event)) return;

        // Cancel closes the window
        if (KeyboardNav.IsCancelPressed(@event))
        {
            Close();
            GetViewport().SetInputAsHandled();
            return;
        }

        // Tab switching (Q/E)
        if (HandleTabInput(@event))
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        // Custom input
        if (HandleExtraInput(@event))
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        // S/cross confirms the focused button (arrow key nav is built-in)
        if (KeyboardNav.HandleConfirm(@event, GetViewport()))
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        // Block ALL remaining key events
        if (@event is InputEventKey k && k.Pressed)
            GetViewport().SetInputAsHandled();
    }
}
