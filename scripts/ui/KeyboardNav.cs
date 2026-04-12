using Godot;
using System.Collections.Generic;

namespace DungeonGame.Ui;

/// <summary>
/// FF SNES-style keyboard navigation for all UI panels.
/// Up/Down (or Q/E shoulders) = move cursor between buttons.
/// S (action_cross) = confirm / press focused button.
/// Focused button shows highlight. Description panels update via FocusEntered.
/// </summary>
public static class KeyboardNav
{
    /// <summary>
    /// Process input for a container of buttons. Call from _UnhandledInput.
    /// Handles: Up/Down cursor movement, S/cross confirm (presses focused button).
    /// Auto-scrolls parent ScrollContainer to keep focused button visible.
    /// Returns true if the event was handled.
    /// </summary>
    public static bool HandleInput(InputEvent @event, Control container)
    {
        if (@event is not InputEventKey keyEvent || !keyEvent.Pressed)
            return false;

        var buttons = GetFocusableButtons(container);
        if (buttons.Count == 0)
            return false;

        int currentIndex = GetFocusedIndex(buttons);

        // Up arrow = move cursor up
        if (@event.IsActionPressed(Constants.InputActions.MoveUp) ||
            keyEvent.Keycode == Key.Up)
        {
            int nextIndex = currentIndex <= 0 ? buttons.Count - 1 : currentIndex - 1;
            buttons[nextIndex].GrabFocus();
            EnsureVisible(buttons[nextIndex]);
            return true;
        }

        // Down arrow = move cursor down
        if (@event.IsActionPressed(Constants.InputActions.MoveDown) ||
            keyEvent.Keycode == Key.Down)
        {
            int nextIndex = currentIndex >= buttons.Count - 1 ? 0 : currentIndex + 1;
            buttons[nextIndex].GrabFocus();
            EnsureVisible(buttons[nextIndex]);
            return true;
        }

        // S (cross) = confirm / press the focused button
        if (@event.IsActionPressed(Constants.InputActions.ActionCross))
        {
            if (currentIndex >= 0)
            {
                buttons[currentIndex].EmitSignal(BaseButton.SignalName.Pressed);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Scroll the nearest parent ScrollContainer so that the control is visible.
    /// </summary>
    public static void EnsureVisible(Control control)
    {
        var scroll = FindParentScroll(control);
        scroll?.EnsureControlVisible(control);
    }

    private static ScrollContainer? FindParentScroll(Node node)
    {
        Node? current = node.GetParent();
        while (current != null)
        {
            if (current is ScrollContainer sc)
                return sc;
            current = current.GetParent();
        }
        return null;
    }

    /// <summary>
    /// Check if this window should skip input because another window is on top.
    /// Call at the top of _UnhandledInput. Returns true if input was blocked.
    /// </summary>
    public static bool BlockIfNotTopmost(Godot.Control window, Godot.InputEvent @event)
    {
        if (WindowStack.IsBlocked(window))
        {
            if (@event is Godot.InputEventKey k && k.Pressed)
                window.GetViewport().SetInputAsHandled();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Check if the event is a D/circle cancel press. Panels call this for FF-style back/cancel.
    /// </summary>
    public static bool IsCancelPressed(InputEvent @event)
    {
        return @event.IsActionPressed(Constants.InputActions.ActionCircle) ||
               (@event is InputEventKey key && key.Pressed && key.Keycode == Key.Escape);
    }

    /// <summary>
    /// Consume movement input so the player doesn't walk behind open panels.
    /// Returns true if consumed.
    /// </summary>
    public static bool ConsumeMovement(InputEvent @event)
    {
        if (@event.IsActionPressed(Constants.InputActions.MoveUp) ||
            @event.IsActionPressed(Constants.InputActions.MoveDown) ||
            @event.IsActionPressed(Constants.InputActions.MoveLeft) ||
            @event.IsActionPressed(Constants.InputActions.MoveRight))
            return true;
        return false;
    }

    private static List<Button> GetFocusableButtons(Control container)
    {
        var buttons = new List<Button>();
        CollectButtons(container, buttons);
        return buttons;
    }

    private static void CollectButtons(Node node, List<Button> buttons)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is Button btn && !btn.Disabled && btn.Visible)
                buttons.Add(btn);
            else if (child is Control)
                CollectButtons(child, buttons);
        }
    }

    private static int GetFocusedIndex(List<Button> buttons)
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i].HasFocus())
                return i;
        }
        return -1;
    }
}
