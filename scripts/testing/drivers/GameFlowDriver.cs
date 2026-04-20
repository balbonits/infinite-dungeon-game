#if DEBUG
using System;
using DungeonGame.Ui;
using Godot;

namespace DungeonGame.Testing.Drivers;

/// <summary>
/// Composition root for GodotTestDriver-based flow tests. Owns one driver
/// per screen the test flow may touch; each is built with a lazy producer
/// that resolves the node from the live scene tree at use-time, so
/// scene swaps and queue-frees don't invalidate the driver.
///
/// Tests receive a single GameFlowDriver and compose verbs:
///   flow.Splash.ClickNewGame();
///   await flow.Tree.WithinSeconds(2, () => Expect(flow.ClassSelect.IsVisible, "..."));
/// </summary>
public sealed class GameFlowDriver
{
    private readonly SceneTree _tree;

    public GameFlowDriver(SceneTree tree, Func<InputHelper> input)
    {
        _tree = tree;
        Splash = new SplashScreenDriver(() => FindFirstOfType<SplashScreen>(tree.Root));
        SlotsFull = new SlotsFullDialogDriver(() => FindFirstOfType<SlotsFullDialog>(tree.Root));
        LoadGame = new LoadGameScreenDriver(() => FindFirstOfType<LoadGameScreen>(tree.Root));
        ClassSelect = new ClassSelectDriver(() => FindFirstOfType<ClassSelect>(tree.Root), input);
    }

    public SceneTree Tree => _tree;

    public SplashScreenDriver Splash { get; }
    public SlotsFullDialogDriver SlotsFull { get; }
    public LoadGameScreenDriver LoadGame { get; }
    public ClassSelectDriver ClassSelect { get; }

    private static T? FindFirstOfType<T>(Node root) where T : Node
    {
        if (root is T match) return match;
        foreach (var child in root.GetChildren())
        {
            var found = FindFirstOfType<T>(child);
            if (found is not null) return found;
        }
        return null;
    }
}
#endif
