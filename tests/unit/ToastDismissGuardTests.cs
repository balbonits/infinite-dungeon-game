using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

/// <summary>
/// Regression for AUDIT-06. The Toast double-dismiss guard uses
/// <c>List.Remove</c>'s return value as the "already dismissed" signal
/// so the production DismissToast path can bail before spinning up a
/// duplicate fade tween. The actual Toast class has a Godot dependency
/// so this xUnit suite exercises the guard shape on a plain List — the
/// same semantics the production code relies on.
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
}
