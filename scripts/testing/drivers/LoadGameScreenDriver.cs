#if DEBUG
using System;
using Chickensoft.GodotTestDriver.Drivers;
using DungeonGame.Ui;
using Godot;

namespace DungeonGame.Testing.Drivers;

/// <summary>
/// GodotTestDriver page-object for <see cref="LoadGameScreen"/>.
/// Minimal surface for current flow tests — load/back buttons + visibility.
/// Expand as more flow tests need to exercise slots or the delete dialog.
/// </summary>
public sealed class LoadGameScreenDriver : ControlDriver<LoadGameScreen>
{
    public LoadGameScreenDriver(Func<LoadGameScreen?> producer) : base(producer!)
    {
        Load = new ButtonDriver(() => FindButton("Load"));
        Back = new ButtonDriver(() => FindButton("Back"));
    }

    public ButtonDriver Load { get; }
    public ButtonDriver Back { get; }

    /// <summary>
    /// True only when the LoadGameScreen root is in-tree AND visible-in-tree.
    /// Just <c>IsInsideTree</c> was a false positive source since
    /// <c>Main.ShowLoadGameScreen</c> sets <c>splash.Visible = false</c> while
    /// keeping it in the tree — parent-visibility matters. (Copilot PR #33.)
    /// </summary>
    public bool IsShown =>
        Root is not null && Root.IsInsideTree() && Root.IsVisibleInTree();

    public void ClickBack() => Back.ClickCenter();

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
