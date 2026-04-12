using System.Collections.Generic;

namespace DungeonGame.Ui;

/// <summary>
/// Central modal stack. Only the topmost modal receives input.
/// Every dialog/window pushes itself on open and pops on close.
/// Any parent can check WindowStack.IsTopmost(this) before processing input.
///
/// This eliminates input bleed-through permanently — no more per-window
/// checks for "is ActionMenu open? is SettingsPanel open?" etc.
/// </summary>
public static class WindowStack
{
    private static readonly Stack<Godot.Control> _stack = new();

    /// <summary>Push a modal onto the stack. Call in your Open/Show method.</summary>
    public static void Push(Godot.Control modal) => _stack.Push(modal);

    /// <summary>Pop the topmost modal. Call in your Close method.</summary>
    public static void Pop(Godot.Control modal)
    {
        if (_stack.Count > 0 && _stack.Peek() == modal)
            _stack.Pop();
        else
        {
            // Safety: rebuild stack without this modal (handles out-of-order closes)
            var temp = new Stack<Godot.Control>();
            foreach (var item in _stack)
                if (item != modal)
                    temp.Push(item);
            _stack.Clear();
            foreach (var item in temp)
                _stack.Push(item);
        }
    }

    /// <summary>Check if this modal is the topmost (should receive input).</summary>
    public static bool IsTopmost(Godot.Control modal) =>
        _stack.Count > 0 && _stack.Peek() == modal;

    /// <summary>Check if ANY modal is open (parent should block input).</summary>
    public static bool HasModal => _stack.Count > 0;

    /// <summary>Check if this specific modal or anything above it is blocking.</summary>
    public static bool IsBlocked(Godot.Control potentialParent) =>
        _stack.Count > 0 && _stack.Peek() != potentialParent;

    /// <summary>Clear the stack (on scene reload).</summary>
    public static void Clear() => _stack.Clear();
}
