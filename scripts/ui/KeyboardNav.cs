using Godot;

namespace DungeonGame.Ui;

/// <summary>
/// Minimal keyboard helpers for the UI layer.
/// Navigation (Up/Down) is handled by Godot's built-in focus system (ui_up/ui_down).
/// ScrollContainers with FollowFocus=true auto-scroll to the focused control.
/// This class only handles: S/cross confirm, cancel detection, and WindowStack gating.
/// </summary>
public static class KeyboardNav
{
    /// <summary>
    /// Press the currently focused button when S/action_cross is pressed.
    /// Godot's built-in focus system handles arrow key navigation;
    /// this bridges the game's S-key confirm to the focused button.
    /// Returns true if a button was pressed.
    /// </summary>
    public static bool HandleConfirm(InputEvent @event, Viewport viewport)
    {
        if (@event is not InputEventKey keyEvent || !keyEvent.Pressed)
            return false;

        if (!@event.IsActionPressed(Constants.InputActions.ActionCross))
            return false;

        if (viewport.GuiGetFocusOwner() is Button btn && !btn.Disabled)
        {
            btn.EmitSignal(BaseButton.SignalName.Pressed);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Check if this window should skip input because another window is on top.
    /// Call at the top of _UnhandledInput. Returns true if input was blocked.
    /// </summary>
    public static bool BlockIfNotTopmost(Control window, InputEvent @event)
    {
        if (WindowStack.IsBlocked(window))
        {
            if (@event is InputEventKey k && k.Pressed)
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
}
