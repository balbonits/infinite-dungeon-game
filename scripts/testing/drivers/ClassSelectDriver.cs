#if DEBUG
using System;
using Chickensoft.GodotTestDriver.Drivers;
using DungeonGame.Autoloads;
using DungeonGame.Ui;
using Godot;

namespace DungeonGame.Testing.Drivers;

/// <summary>
/// GodotTestDriver page-object for <see cref="ClassSelect"/>.
/// Wraps the card-select zone navigation that was duplicated (and bugged)
/// across DeathTests / DeathCinematicTests / NpcTests / TownTests setups.
///
/// IMPORTANT: the setup bug those tests had — PressEnter without first
/// moving focus Right to land on (and auto-select) a card — returned
/// early in OnConfirmPressed because _selectedCard stayed null. Town
/// never loaded, so every downstream test timed out.
///
/// This driver's SelectWarriorAndConfirm() does the correct 3-step
/// sequence: NavRight (auto-selects first card) → NavDown (focus Confirm)
/// → PressEnter (fires OnConfirmPressed → LoadTown).
/// </summary>
public sealed class ClassSelectDriver : ControlDriver<ClassSelect>
{
    private readonly Func<InputHelper> _input;

    public ClassSelectDriver(Func<ClassSelect?> producer, Func<InputHelper> input) : base(producer!)
    {
        _input = input;
        Confirm = new ButtonDriver(() => FindButton("Confirm Selection"));
        Back = new ButtonDriver(() => FindButton("Back to Main Menu"));
    }

    public ButtonDriver Confirm { get; }
    public ButtonDriver Back { get; }

    // IsVisibleInTree, not just IsInsideTree: ClassSelect stays in the tree
    // briefly between Visible=false and QueueFree during the confirm-to-town
    // transition, and driver-level assertions shouldn't see that window as
    // "shown". (Copilot PR #33 round-4.)
    public bool IsShown => Root is not null && Root.IsInsideTree() && Root.IsVisibleInTree();

    /// <summary>
    /// Full keyboard flow to pick Warrior and hit Confirm. Mirrors the
    /// sequence a user would do: Right to auto-select the first card,
    /// Down to the Confirm zone, Enter to fire. Returns after the Confirm
    /// keystroke is dispatched + 5 frames of settle time; callers that
    /// need to assert on GameState.SelectedClass or Town-scene-loaded
    /// should follow this with their own WaitUntil.
    /// </summary>
    public async System.Threading.Tasks.Task SelectWarriorAndConfirm()
    {
        var input = _input();
        await input.NavRight();           // focus + auto-select first card (Warrior)
        await input.WaitFrames(5);
        await input.NavDown();            // zone 0 (cards) → zone 1 (Confirm)
        await input.WaitFrames(5);
        await input.PressEnter();         // fire Confirm → OnConfirmPressed → LoadTown
        await input.WaitFrames(5);
    }

    private Button? FindButton(string text) => SearchButton(Root, text);

    private static Button? SearchButton(Node? root, string text)
    {
        if (root is null) return null;
        // IsVisibleInTree — a button with Visible=true under a hidden parent
        // is not actually interactable. (Copilot PR #33 round-4.)
        if (root is Button btn && btn.IsInsideTree() && btn.IsVisibleInTree() && btn.Text == text) return btn;
        foreach (var child in root.GetChildren())
        {
            var found = SearchButton(child, text);
            if (found is not null) return found;
        }
        return null;
    }
}
#endif
