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
    ///
    /// Narrowed (Copilot PR #38 round-2) to only search within
    /// DismissToast's body rather than the whole file, so the guard can't
    /// be falsely satisfied by the same substring appearing in a comment
    /// or a different method elsewhere in Toast.cs.
    /// </summary>
    [Fact]
    public void ToastCs_DismissToastMethodContainsDoubleDismissGuard()
    {
        string toastPath = FindRepoFile("scripts/ui/Toast.cs");
        string src = File.ReadAllText(toastPath);
        string body = ExtractMethodBody(src, "DismissToast");
        body.Should().Contain("!_activeToasts.Remove(toast)",
            "DismissToast must keep the AUDIT-06 guard expression. If you renamed the field or refactored, update this test to match the new invariant.");
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

    /// <summary>
    /// Locate the actual method declaration (not a call site) via a regex
    /// that matches "<c>[modifier ] return-type methodName(</c>" patterns,
    /// then return the slice from that method's opening brace to the
    /// matching closing brace. Copilot PR #38 round-3 caught that the
    /// earlier "first <paramref name="methodName"/>(" heuristic could land
    /// on a call site (e.g., a timer callback invocation) if the definition
    /// came later — a small refactor to Toast.cs could silently break the
    /// guard test for unrelated reasons.
    ///
    /// Brace counting is literal-aware (line/block comments, strings, chars
    /// don't count their braces) so a comment like "// }" or a string
    /// with { won't fool the counter.
    /// </summary>
    private static string ExtractMethodBody(string src, string methodName)
    {
        // Declaration: one or more modifier/whitespace tokens, then a
        // return-type token (any identifier, possibly generic / nullable),
        // then the method name, then '('. Excludes bare call-site syntax
        // (which is just `methodName(` with no return type in front).
        var declRegex = new System.Text.RegularExpressions.Regex(
            @"\b(?:public|private|protected|internal|static|async|sealed|override|virtual|new)\s+(?:[A-Za-z_][A-Za-z0-9_<>?.,\s]*\s+)?"
            + System.Text.RegularExpressions.Regex.Escape(methodName)
            + @"\s*\(");
        var match = declRegex.Match(src);
        if (!match.Success)
            throw new System.InvalidOperationException($"method '{methodName}' declaration not found in source");
        int methodIdx = match.Index;
        int openBrace = src.IndexOf('{', methodIdx);
        if (openBrace < 0)
            throw new System.InvalidOperationException($"no opening brace after '{methodName}' declaration");

        int depth = 1;
        int i = openBrace + 1;
        bool inLineComment = false, inBlockComment = false, inString = false, inChar = false;
        for (; i < src.Length && depth > 0; i++)
        {
            char c = src[i];
            char prev = i > 0 ? src[i - 1] : '\0';

            if (inLineComment) { if (c == '\n') inLineComment = false; continue; }
            if (inBlockComment) { if (c == '/' && prev == '*') inBlockComment = false; continue; }
            if (inString) { if (c == '"' && prev != '\\') inString = false; continue; }
            if (inChar) { if (c == '\'' && prev != '\\') inChar = false; continue; }

            if (c == '/' && i + 1 < src.Length && src[i + 1] == '/') { inLineComment = true; continue; }
            if (c == '/' && i + 1 < src.Length && src[i + 1] == '*') { inBlockComment = true; continue; }
            if (c == '"') { inString = true; continue; }
            if (c == '\'') { inChar = true; continue; }
            if (c == '{') depth++;
            else if (c == '}') depth--;
        }
        return src.Substring(openBrace, i - openBrace);
    }
}
