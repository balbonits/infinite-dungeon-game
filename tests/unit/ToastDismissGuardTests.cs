using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

/// <summary>
/// Regression for AUDIT-06. The Toast double-dismiss guard uses
/// <c>List.Remove</c>'s return value as the "already dismissed" signal
/// so the production DismissToast path can bail before spinning up a
/// duplicate fade tween.
///
/// Two kinds of test live here (Copilot PR #38 called out that earlier
/// versions of this file only did kind 1, which wouldn't catch a regression
/// in Toast.cs itself):
///   1. Contract tests — assert the <c>List.Remove</c> semantics the guard
///      is built on (false-on-already-removed, false-on-never-tracked).
///   2. Source guard — read <c>scripts/ui/Toast.cs</c> as a string and
///      assert the guard expression still exists. Dumb but effective:
///      if anyone deletes or rewrites the guard, this test fails and the
///      author has to re-justify it. The Toast class itself has a Godot
///      dependency that the unit test project can't compile against, so
///      this is the cheapest way to pin the production call site.
/// </summary>
public class ToastDismissGuardTests
{
    [Fact]
    public void ListRemove_OnFirstCall_ReturnsTrue()
    {
        var tracked = new List<string> { "toast_a", "toast_b" };
        tracked.Remove("toast_a").Should().BeTrue();
    }

    [Fact]
    public void ListRemove_OnAlreadyDismissed_ReturnsFalse()
    {
        var tracked = new List<string> { "toast_a" };
        tracked.Remove("toast_a");           // first: true
        tracked.Remove("toast_a").Should().BeFalse("second Remove on already-gone item returns false");
    }

    [Fact]
    public void ListRemove_OnNeverTracked_ReturnsFalse()
    {
        var tracked = new List<string>();
        tracked.Remove("ghost").Should().BeFalse("missing item yields false, not an exception");
    }

    /// <summary>
    /// Models the production DismissToast shape:
    ///   if (!_activeToasts.Remove(toast)) return;
    ///   // else proceed to spin up tween
    /// Under double-dismiss, only the first caller gets past the guard.
    /// </summary>
    [Fact]
    public void DoubleDismissGuard_OnlyOneCallerProceeds()
    {
        var activeToasts = new List<string> { "toast_x" };
        int proceedCount = 0;

        // First call — Remove returns true, guard passes, work runs.
        if (activeToasts.Remove("toast_x")) proceedCount++;
        // Second call — Remove returns false, guard fails, no work.
        if (activeToasts.Remove("toast_x")) proceedCount++;

        proceedCount.Should().Be(1, "exactly one caller should proceed past the guard");
    }

    /// <summary>
    /// Source-level guard: Toast.DismissToast must still contain the
    /// <c>!_activeToasts.Remove(toast)</c> early-exit. If someone rewrites
    /// DismissToast without it — the AUDIT-06 regression shape — this test
    /// fails. Cheaper than instantiating a Godot node in a unit project
    /// that can't link Godot.
    /// </summary>
    [Fact]
    public void ToastCs_StillContainsDoubleDismissGuard()
    {
        string toastPath = FindRepoFile("scripts/ui/Toast.cs");
        string src = File.ReadAllText(toastPath);
        src.Should().Contain("!_activeToasts.Remove(toast)",
            "Toast.DismissToast must keep the AUDIT-06 guard expression. If you renamed the field or refactored, update this test to match the new invariant.");
    }

    /// <summary>Walk up from the test binary to find a repo-relative file.</summary>
    private static string FindRepoFile(string relPath)
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            string candidate = Path.Combine(dir.FullName, relPath);
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new FileNotFoundException($"Could not locate {relPath} walking up from cwd");
    }
}
