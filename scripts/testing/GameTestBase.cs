#if DEBUG
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

    private int _passCount;
    private int _failCount;

    protected GameTestBase(Node testScene) : base(testScene)
    {
        Input = new InputHelper(testScene);
        Ui = new UiHelper(testScene);
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
