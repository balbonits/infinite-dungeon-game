using Godot;
using System.Collections.Generic;

namespace DungeonGame.Ui;

/// <summary>
/// Reusable keyboard navigation for dialog/panel UIs.
/// Arrow Up/Down or Bumper buttons (Q/E) cycle through focusable buttons.
/// Enter/Space/S activates the focused button.
/// </summary>
public static class KeyboardNav
{
    /// <summary>
    /// Process input for a container of buttons. Call from _UnhandledInput.
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

        // Bumpers (Q/E) or Up/Down arrows cycle through buttons
        if (@event.IsActionPressed(Constants.InputActions.ShoulderLeft) ||
            keyEvent.Keycode == Key.Up)
        {
            int nextIndex = currentIndex <= 0 ? buttons.Count - 1 : currentIndex - 1;
            buttons[nextIndex].GrabFocus();
            return true;
        }

        if (@event.IsActionPressed(Constants.InputActions.ShoulderRight) ||
            keyEvent.Keycode == Key.Down)
        {
            int nextIndex = currentIndex >= buttons.Count - 1 ? 0 : currentIndex + 1;
            buttons[nextIndex].GrabFocus();
            return true;
        }

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
