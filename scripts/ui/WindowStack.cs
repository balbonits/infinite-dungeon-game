using System.Collections.Generic;
using Godot;

namespace DungeonGame.Ui;

/// <summary>
/// Central modal stack. Only the topmost modal receives input.
/// Every dialog/window pushes itself on open and pops on close.
/// Any parent can check WindowStack.IsTopmost(this) before processing input.
///
/// Also owns the tree-pause lifecycle: when the FIRST modal pushes onto an
/// empty stack, WindowStack records whether the tree was already paused.
/// When the LAST modal pops and the stack becomes empty, WindowStack only
/// unpauses if IT was the one that paused. This prevents the "stuck paused"
/// bug when modals chain (NpcPanel → ShopWindow → ...) and close out of order,
/// AND prevents leaking an unpause onto a parent screen (splash, class-select,
/// pause menu) that already had the tree paused before any modal opened.
/// See docs/dev-journal.md Session 20 for the bug that led to this design.
/// </summary>
public static class WindowStack
{
    private static readonly Stack<Control> _stack = new();
    // True when WindowStack was the one that transitioned the tree from
    // running → paused. Set on first push into an empty stack, cleared
    // on the last pop.
    private static bool _ownsPause;

    /// <summary>
    /// Push a modal onto the stack. If this is the first modal AND the tree
    /// was running, WindowStack claims ownership of the pause and pauses the
    /// tree. Call from your Show/Open method.
    /// </summary>
    public static void Push(Control modal)
    {
        if (_stack.Count == 0)
        {
            var tree = modal.GetTree();
            if (!tree.Paused)
            {
                _ownsPause = true;
                tree.Paused = true;
            }
        }
        _stack.Push(modal);
    }

    /// <summary>
    /// Pop a modal. If this empties the stack AND WindowStack owns the pause,
    /// unpauses the tree. Handles out-of-order closes (modal in the middle
    /// of the stack closing first) by rebuilding without that modal.
    /// </summary>
    public static void Pop(Control modal)
    {
        if (_stack.Count > 0 && _stack.Peek() == modal)
            _stack.Pop();
        else
        {
            // Safety: rebuild stack without this modal (handles out-of-order closes)
            var temp = new Stack<Control>();
            foreach (var item in _stack)
                if (item != modal)
                    temp.Push(item);
            _stack.Clear();
            foreach (var item in temp)
                _stack.Push(item);
        }

        if (_stack.Count == 0 && _ownsPause)
        {
            modal.GetTree().Paused = false;
            _ownsPause = false;
        }
    }

    /// <summary>Check if this modal is the topmost (should receive input).</summary>
    public static bool IsTopmost(Godot.Control modal) =>
        _stack.Count > 0 && _stack.Peek() == modal;

    /// <summary>Check if ANY modal is open (parent should block input).</summary>
    public static bool HasModal => _stack.Count > 0;

    /// <summary>Number of modals currently in the stack.</summary>
    public static int Count => _stack.Count;

    /// <summary>Type name of the topmost modal, or null if empty. For test assertions.</summary>
    public static string? TopTypeName => _stack.Count > 0 ? _stack.Peek().GetType().Name : null;

    /// <summary>Check if this specific modal or anything above it is blocking.</summary>
    public static bool IsBlocked(Godot.Control potentialParent) =>
        _stack.Count > 0 && _stack.Peek() != potentialParent;

    /// <summary>Clear the stack (on scene reload). Also clears pause ownership.</summary>
    public static void Clear()
    {
        _stack.Clear();
        _ownsPause = false;
    }
}
