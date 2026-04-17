using Godot;
using DungeonGame.Ui;

namespace DungeonGame.Testing;

/// <summary>
/// Query helpers for UI state: focus, windows, pause, scene tree.
/// Used by tests to assert UI invariants without touching implementation details.
/// </summary>
public class UiHelper
{
    private readonly Node _node;

    public UiHelper(Node node)
    {
        _node = node;
    }

    // ── Focus ────────────────────────────────────────────────────────────────

    /// <summary>The control currently holding focus, or null.</summary>
    public Control? FocusedControl => _node.GetViewport().GuiGetFocusOwner();

    /// <summary>True if any control has focus.</summary>
    public bool HasFocus => FocusedControl != null;

    /// <summary>Text of the focused button, or null if no button focused.</summary>
    public string? FocusedButtonText =>
        FocusedControl is Button btn ? btn.Text : null;

    // ── Windows ──────────────────────────────────────────────────────────────

    /// <summary>Number of modals currently in the WindowStack.</summary>
    public int ModalCount => WindowStack.Count;

    /// <summary>True if any modal is open.</summary>
    public bool AnyModalOpen => WindowStack.HasModal;

    /// <summary>Type name of the topmost modal, or null.</summary>
    public string? TopmostWindowName => WindowStack.TopTypeName;

    /// <summary>True if pause menu is open.</summary>
    public bool PauseMenuOpen => PauseMenu.Instance?.IsOpen == true;

    /// <summary>True if a specific window type is open (by Instance property).</summary>
    public bool IsOpen<T>() where T : GameWindow
    {
        return TopmostWindowName == typeof(T).Name;
    }

    // ── Pause state ──────────────────────────────────────────────────────────

    /// <summary>True if the game tree is paused.</summary>
    public bool Paused => _node.GetTree().Paused;

    /// <summary>True if player input should be blocked (paused or modal open).</summary>
    public bool InputBlocked => Paused || AnyModalOpen;

    // ── Scene queries ────────────────────────────────────────────────────────

    /// <summary>Find a button by exact text in the scene tree (for verification, not clicking).</summary>
    public Button? FindButton(string text)
    {
        return SearchButton(_node.GetTree().Root, b => b.Text == text);
    }

    /// <summary>Find a button matching a predicate (e.g., text.Contains/StartsWith).</summary>
    public Button? FindButton(System.Func<Button, bool> predicate)
    {
        return SearchButton(_node.GetTree().Root, predicate);
    }

    private static Button? SearchButton(Node root, System.Func<Button, bool> predicate)
    {
        if (root is Button btn && btn.Visible && predicate(btn))
            return btn;
        foreach (var child in root.GetChildren())
        {
            var found = SearchButton(child, predicate);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>Find a node by name anywhere in the tree (recursive).</summary>
    public Node? FindNode(string name) => SearchNode(_node.GetTree().Root, name);

    private static Node? SearchNode(Node root, string name)
    {
        if (root.Name == name) return root;
        foreach (var child in root.GetChildren())
        {
            var found = SearchNode(child, name);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>True if any node of the given type exists in the tree.</summary>
    public bool HasNodeOfType<T>() where T : Node => FindNodeOfType<T>() != null;

    /// <summary>Find the first node of the given type, anywhere in the tree.</summary>
    public T? FindNodeOfType<T>() where T : Node => SearchType<T>(_node.GetTree().Root);

    private static T? SearchType<T>(Node root) where T : Node
    {
        if (root is T match) return match;
        foreach (var child in root.GetChildren())
        {
            var found = SearchType<T>(child);
            if (found != null) return found;
        }
        return null;
    }
}
