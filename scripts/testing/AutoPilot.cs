using System;
using System.Threading.Tasks;
using Godot;

namespace DungeonGame.Testing;

/// <summary>
/// Player emulation testing library — core node.
/// Attach to GetTree().Root to survive scene changes.
/// Provides step runner, logging, assertions, and access to Actions/Verify subsystems.
///
/// Usage:
///   var pilot = new AutoPilot();
///   GetTree().Root.AddChild(pilot);
///   await pilot.Run("step name", async () => { ... });
///   pilot.Finish();
/// </summary>
public partial class AutoPilot : Node
{
    private int _passCount;
    private int _failCount;
    private int _stepIndex;

    public AutoPilotActions Actions { get; private set; } = null!;
    public AutoPilotAssertions Verify { get; private set; } = null!;

    private bool _walkthroughStarted;

    public override void _Ready()
    {
        Name = "AutoPilot";
        ProcessMode = ProcessModeEnum.Always; // Run during pause (splash/class select are paused)
        Actions = new AutoPilotActions(this);
        Verify = new AutoPilotAssertions(this);
    }

    public override void _PhysicsProcess(double delta)
    {
        Actions.ProcessPendingReleases();

        // (walkthrough started via SetWalkthrough async)
    }

    /// <summary>Register a walkthrough. Launches after this node enters the tree.</summary>
    public void SetWalkthrough(Func<AutoPilot, Task> walkthrough)
    {
        _pendingWalkthrough = walkthrough;
    }

    private Func<AutoPilot, Task>? _pendingWalkthrough;

    /// <summary>Called when the node enters the tree. Safe to use GetTree() here.</summary>
    public override void _EnterTree()
    {
        if (_pendingWalkthrough != null)
        {
            var wt = _pendingWalkthrough;
            _pendingWalkthrough = null;
            _ = LaunchWalkthrough(wt);
        }
    }

    private async Task LaunchWalkthrough(Func<AutoPilot, Task> walkthrough)
    {
        // Wait for the scene change to complete
        Log("Waiting for scene to load...");
        await ToSignal(GetTree().CreateTimer(2.0f), SceneTreeTimer.SignalName.Timeout);

        var scene = GetTree().CurrentScene;
        Log($"Scene ready: {scene?.Name ?? "null"} — starting walkthrough");
        _walkthroughStarted = true;

        // Start telemetry recording
#if DEBUG
        var telemetry = new DebugTelemetry();
        GetTree().Root.AddChild(telemetry);
        telemetry.StartRecording();
#endif

        try
        {
            await walkthrough(this);
        }
        catch (Exception ex)
        {
            Assert(false, $"Walkthrough crashed: {ex.Message}");
        }

#if DEBUG
        telemetry.StopRecording();
#endif

        Finish();
    }

    // ── Step runner ──────────────────────────────────────────────────────────

    /// <summary>Run a named step. Logs start/end and catches exceptions.</summary>
    public async Task Run(string label, Func<Task> step)
    {
        _stepIndex++;
        Log($"");
        Log($"── Step {_stepIndex}: {label} ──");
        try
        {
            await step();
            Log($"   ✓ done");
        }
        catch (TimeoutException ex)
        {
            Assert(false, $"TIMEOUT: {ex.Message}");
        }
        catch (Exception ex)
        {
            Assert(false, $"ERROR: {ex.Message}");
        }
    }

    // ── Logging ──────────────────────────────────────────────────────────────

    public void Log(string message)
    {
        GD.Print($"[AUTOPILOT] {message}");
    }

    // ── Assertions ───────────────────────────────────────────────────────────

    public void Assert(bool condition, string description)
    {
        if (condition)
        {
            _passCount++;
            Log($"  ✅ {description}");
        }
        else
        {
            _failCount++;
            Log($"  ❌ FAIL: {description}");
        }
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────

    /// <summary>Print summary and exit. Call at the end of your walkthrough.</summary>
    public void Finish()
    {
        Log("");
        Log($"═══ AutoPilot Results: {_passCount} passed, {_failCount} failed ═══");
        int exitCode = _failCount > 0 ? 1 : 0;
        Log($"Exiting with code {exitCode}");
        GetTree().Quit(exitCode);
    }
}
