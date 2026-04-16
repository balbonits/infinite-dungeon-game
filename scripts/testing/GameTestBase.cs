#if DEBUG
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;

namespace DungeonGame.Testing;

/// <summary>
/// Base class for all game integration tests. Provides:
/// - InputHelper: keyboard/action input simulation
/// - UiHelper: focus, window, pause state queries
/// - WaitUntil: async condition polling
/// - Pass/fail tracking with pretty output
///
/// Tests extend this instead of TestClass directly to get all helpers ready.
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
