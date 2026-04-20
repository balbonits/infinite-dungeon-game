#if DEBUG
using System.Diagnostics;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using DungeonGame.Autoloads;
using DungeonGame.Ui;
using Godot;

namespace DungeonGame.Testing;

/// <summary>
/// Base class for all game integration tests. Provides:
/// - InputHelper: keyboard/action input simulation
/// - UiHelper: focus, window, pause state queries
/// - WaitUntil: async condition polling
/// - Pass/fail tracking with pretty output
/// - Cross-suite state isolation (TEST-09) via <see cref="ResetToFreshSplash"/>
///
/// Tests extend this instead of TestClass directly to get all helpers ready.
/// Each suite should call <see cref="ResetToFreshSplash"/> in its <c>[Setup]</c>
/// method (or before reasoning about splash state) to ensure earlier suites
/// leave no residue — e.g., a leftover PauseMenu, a Town scene, or a GameState
/// mid-run. See docs/testing/ui-tests.md.
/// </summary>
public abstract class GameTestBase : TestClass
{
    protected InputHelper Input { get; private set; } = null!;
    protected UiHelper Ui { get; private set; } = null!;

    /// <summary>
    /// GodotTestDriver composition root — per-screen drivers with lazy
    /// producers. Use this for flow tests ("click New Game → ClassSelect
    /// appears → click Warrior → Town loads"); ad-hoc Ui.FindButton should
    /// only be used for quick assertions, not flow steps.
    /// Per docs/testing/godot-test-driver.md.
    /// </summary>
    protected Drivers.GameFlowDriver Flow { get; private set; } = null!;

    private int _passCount;
    private int _failCount;
    private int _screenshotStep;
    private string _currentTestName = "unknown";

    /// <summary>
    /// Tell the test base which test is currently executing so screenshots
    /// land under the right directory. Call at the top of each [Test].
    /// </summary>
    protected void StartTest(string testName)
    {
        _currentTestName = testName;
        _screenshotStep = 0;
    }

    /// <summary>
    /// Capture the current viewport to <c>tests/e2e/screenshots/&lt;suite&gt;/&lt;test&gt;/NN_&lt;step&gt;.png</c>.
    /// Unconditional capture — does not compare against a baseline.
    /// Use for step-by-step flow documentation screenshots.
    /// </summary>
    protected async Task Screenshot(string stepName)
    {
        _screenshotStep++;
        await ScreenshotHelper.Capture(
            TestScene,
            GetType().Name,
            _currentTestName,
            _screenshotStep,
            stepName);
    }

    /// <summary>
    /// Run the accessibility linter on a subtree and Expect zero
    /// Error-severity violations. Warnings are logged but don't fail.
    /// Per AccessibilityLinter.cs — covers focus reachability, touch
    /// targets, contrast ratios, modal-close reachability.
    /// </summary>
    protected void ExpectNoAccessibilityViolations(Node root, string what = "subtree")
    {
        var violations = AccessibilityLinter.Lint(root);
        int errors = 0, warnings = 0;
        foreach (var v in violations)
        {
            if (v.Severity == AccessibilityLinter.Severity.Error) errors++;
            else warnings++;
            GD.Print($"    [a11y:{v.Severity}] {v.Rule} @ {v.NodePath} — {v.Detail}");
        }
        Expect(errors == 0,
            $"{what} has no accessibility Errors (found {errors} errors, {warnings} warnings)");
    }

    /// <summary>
    /// Capture the viewport AND compare against the stored baseline under
    /// <c>tests/e2e/screenshots/baselines/&lt;suite&gt;/&lt;test&gt;/</c>.
    /// - First run seeds the baseline (marked BaselineSeeded in logs).
    /// - Subsequent runs Expect the diff is within <paramref name="tolerancePercent"/>.
    /// - On mismatch, a <c>.received.png</c> + <c>.diff.png</c> are written under
    ///   <c>tests/e2e/screenshots/received/</c> for review.
    /// </summary>
    protected async Task VerifyScreenshot(string stepName, double tolerancePercent = 1.0)
    {
        _screenshotStep++;
        var report = await ScreenshotHelper.VerifyAgainstBaseline(
            TestScene,
            GetType().Name,
            _currentTestName,
            _screenshotStep,
            stepName,
            tolerancePercent);

        switch (report.Status)
        {
            case ScreenshotHelper.VerifyStatus.Match:
                Expect(true, $"screenshot {stepName} matches baseline ({report.PixelDifferencePercent:F2}% diff ≤ {tolerancePercent}%)");
                break;
            case ScreenshotHelper.VerifyStatus.BaselineSeeded:
                // Treat as pass — first run of a new test creates the baseline.
                // Reviewer promotes it intentionally, so we don't hard-fail here.
                Expect(true, $"screenshot {stepName} seeded new baseline at {report.VerifiedPath}");
                break;
            case ScreenshotHelper.VerifyStatus.Skipped:
                // Headless run — capture unavailable. Log without failing;
                // visual assertions only run in windowed test jobs (xvfb on CI).
                GD.Print($"[VerifyScreenshot] {stepName} skipped — headless mode");
                break;
            case ScreenshotHelper.VerifyStatus.Mismatch:
                Expect(false, $"screenshot {stepName} DIFFERS from baseline by {report.PixelDifferencePercent:F2}% (> {tolerancePercent}%). Diff image: {report.DiffPath}");
                break;
            case ScreenshotHelper.VerifyStatus.Failed:
                Expect(false, $"screenshot {stepName} capture failed (no viewport / empty image)");
                break;
        }
    }

    protected GameTestBase(Node testScene) : base(testScene)
    {
        Input = new InputHelper(testScene);
        Ui = new UiHelper(testScene);
        Flow = new Drivers.GameFlowDriver(testScene.GetTree(), () => Input);
    }

    /// <summary>
    /// Return the game to a fresh splash-screen state (TEST-09). Resets the
    /// singleton <see cref="GameState"/> and reloads the current scene so
    /// Main._Ready re-runs and the splash screen shows again. Awaits the
    /// splash screen's reappearance with a short timeout.
    /// </summary>
    /// <remarks>
    /// Safe to call at the top of any <c>[Setup]</c> or <c>[SetupAll]</c>. It's
    /// a heavy reset — use it once per suite or once per test, not per assertion.
    /// </remarks>
    protected async Task<bool> ResetToFreshSplash()
    {
        var tree = TestScene.GetTree();
        if (tree == null) return false;

        GameState.Instance?.Reset();
        tree.Paused = false;
        tree.ReloadCurrentScene();
        await Input.WaitFrames(3);

        return await WaitUntil(
            () => Ui.HasNodeOfType<SplashScreen>(),
            timeout: 3f,
            what: "SplashScreen to re-appear after reset");
    }

    /// <summary>Log a passing or failing assertion.</summary>
    protected void Expect(bool condition, string description)
    {
        UpdateCurrentTestFromStack();
        if (condition)
        {
            _passCount++;
            GD.Print($"    ✓ {description}");
        }
        else
        {
            _failCount++;
            GD.PrintErr($"    ✗ {description}");
        }
    }

    // Cache the declaring type of the last-identified test (sync method or
    // async state-machine). While the current Expect is still inside that
    // same frame, we skip the reflection walk entirely. Invalidated when the
    // top stack frame's declaring type changes.
    private System.Type? _lastTestDeclaringType;

    // Walks the call stack to find the [Test] method and, when it changes,
    // updates the on-screen overlay + prints a RUNNING banner. Handles both
    // sync methods (attribute is on the method itself) and async state
    // machines (attribute is on the outer method whose name is embedded in
    // the compiler-generated nested type's name "<TestName>d__N").
    //
    // Cached fast-path means reflection only runs once per test, not per
    // Expect — addresses PR #33 round-6 Copilot concern about overhead.
    private void UpdateCurrentTestFromStack()
    {
        var stack = new StackTrace();
        if (stack.FrameCount < 2) return;

        // Fast path: if the immediate caller's declaring type matches the
        // cached one, we're still in the same test — no walk needed.
        var directCallerType = stack.GetFrame(1)?.GetMethod()?.DeclaringType;
        if (_lastTestDeclaringType != null && directCallerType == _lastTestDeclaringType)
            return;

        for (int i = 1; i < stack.FrameCount; i++)
        {
            var method = stack.GetFrame(i)?.GetMethod();
            if (method is null) continue;

            // Sync test: [Test] is on the method itself.
            if (method.GetCustomAttributes(typeof(TestAttribute), false).Length > 0)
            {
                MaybeUpdateTestName(method.Name, method.DeclaringType);
                return;
            }

            // Async test: the runtime frame is MoveNext on a compiler-
            // generated nested state-machine type. The original method name
            // is embedded in the nested type's name between angle brackets.
            var declaringType = method.DeclaringType;
            if (method.Name == "MoveNext" && declaringType is { IsNested: true })
            {
                var typeName = declaringType.Name;
                int lt = typeName.IndexOf('<');
                int gt = typeName.IndexOf('>');
                if (lt >= 0 && gt > lt)
                {
                    var testName = typeName.Substring(lt + 1, gt - lt - 1);
                    var outer = declaringType.DeclaringType;
                    var outerMethod = outer?.GetMethod(testName);
                    if (outerMethod?.GetCustomAttributes(typeof(TestAttribute), false).Length > 0)
                    {
                        MaybeUpdateTestName(testName, declaringType);
                        return;
                    }
                }
            }
        }
    }

    private void MaybeUpdateTestName(string testName, System.Type? declaringType)
    {
        _lastTestDeclaringType = declaringType;
        if (_currentTestName == testName) return;
        _currentTestName = testName;
        _screenshotStep = 0;
        TestProgressOverlay.SetCurrentTest(GetType().Name, testName);
        GD.Print($"\n▶▶▶ RUNNING: {GetType().Name}::{testName}\n");
    }

    /// <summary>Poll a condition until true, or fail after timeout. Logs pass/fail if `what` provided.</summary>
    protected async Task<bool> WaitUntil(System.Func<bool> condition, float timeout = 5f, string? what = null)
    {
        float elapsed = 0f;
        while (elapsed < timeout)
        {
            if (condition())
            {
                if (what != null) Expect(true, $"{what}");
                return true;
            }
            await Input.WaitFrames(3);
            elapsed += 3f / 60f;
        }
        if (what != null) Expect(false, $"Timeout waiting for: {what}");
        return false;
    }

    /// <summary>Count pass/fail counts available to the test runner.</summary>
    protected (int pass, int fail) GetCounts() => (_passCount, _failCount);

    /// <summary>Print the final tally for this test class.</summary>
    protected void PrintSummary(string suiteName)
    {
        var total = _passCount + _failCount;
        var status = _failCount == 0 ? "✓" : "✗";
        GD.Print($"  {status} {suiteName}: {_passCount}/{total} assertions passed");
    }
}
#endif
