#if DEBUG
using System;
using Chickensoft.GodotTestDriver.Drivers;
using DungeonGame.Ui;
using Godot;

namespace DungeonGame.Testing.Drivers;

/// <summary>
/// GodotTestDriver page-object for <see cref="SlotsFullDialog"/>.
/// Modal shown when New Game is clicked with all 3 save slots occupied.
/// </summary>
public sealed class SlotsFullDialogDriver : ControlDriver<SlotsFullDialog>
{
    public SlotsFullDialogDriver(Func<SlotsFullDialog?> producer) : base(producer!)
    {
        OpenLoadGame = new ButtonDriver(() => FindButton("Open Load Game"));
        Cancel = new ButtonDriver(() => FindButton("Cancel"));
    }

    public ButtonDriver OpenLoadGame { get; }
    public ButtonDriver Cancel { get; }

    /// <summary>
    /// True only when the dialog root is in-tree AND visible-in-tree AND the
    /// "Open Load Game" button is reachable. Just checking <c>Button.Visible</c>
    /// produced false positives because GameWindow.Close() hides only the
    /// Overlay, not the root. (Copilot PR #33 finding.)
    /// </summary>
    public bool IsShown =>
        Root is not null
        && Root.IsInsideTree()
        && Root.IsVisibleInTree()
        && FindButton("Open Load Game") is not null;

    public void ClickOpenLoadGame() => OpenLoadGame.ClickCenter();
    public void ClickCancel() => Cancel.ClickCenter();

    private Button? FindButton(string text) => SearchButton(Root, text);

    private static Button? SearchButton(Node? root, string text)
    {
        if (root is null) return null;
        if (root is Button btn && btn.IsVisibleInTree() && btn.Text == text) return btn;
        foreach (var child in root.GetChildren())
        {
            var found = SearchButton(child, text);
            if (found is not null) return found;
        }
        return null;
    }
}
#endif
