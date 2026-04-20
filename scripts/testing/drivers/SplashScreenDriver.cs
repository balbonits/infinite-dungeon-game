#if DEBUG
using System;
using Chickensoft.GodotTestDriver.Drivers;
using DungeonGame.Ui;
using Godot;

namespace DungeonGame.Testing.Drivers;

/// <summary>
/// GodotTestDriver page-object for <see cref="SplashScreen"/>.
/// Exposes the interactive controls (New Game, Continue, Tutorial, Settings,
/// Exit) as typed child drivers + flow-shaped verbs (ClickNewGame, etc.).
///
/// Tests should go through the verbs, not reach through to inner controls —
/// that keeps scene-structure knowledge in this one file and lets assertions
/// read as flows ("after ClickNewGame, ClassSelect appears") rather than
/// as tree-poking.
///
/// Per docs/testing/godot-test-driver.md § SplashScreenDriver.
/// </summary>
public sealed class SplashScreenDriver : ControlDriver<SplashScreen>
{
    public SplashScreenDriver(Func<SplashScreen?> producer) : base(producer!)
    {
        NewGame = new ButtonDriver(() => FindButton("New Game"));
        Continue = new ButtonDriver(() => FindButton("Continue"));
        Tutorial = new ButtonDriver(() => FindButton("Tutorial"));
        Settings = new ButtonDriver(() => FindButton("Settings"));
        Exit = new ButtonDriver(() => FindButton("Exit Game"));
    }

    public ButtonDriver NewGame { get; }
    public ButtonDriver Continue { get; }
    public ButtonDriver Tutorial { get; }
    public ButtonDriver Settings { get; }
    public ButtonDriver Exit { get; }

    /// <summary>Click the New Game button (whatever its current enabled state).</summary>
    public void ClickNewGame() => NewGame.ClickCenter();

    /// <summary>Click Continue. Callers should have ensured at least one save exists.</summary>
    public void ClickContinue() => Continue.ClickCenter();

    /// <summary>Recursively finds a visible Button with exact-matching text under the splash subtree.</summary>
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
