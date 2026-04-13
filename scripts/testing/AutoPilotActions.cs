using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Chickensoft.GodotTestDriver.Input;

namespace DungeonGame.Testing;

/// <summary>
/// Input simulation and waiting primitives for AutoPilot.
/// Wraps GodotTestDriver for input injection. Game-specific helpers on top.
/// Public API unchanged — walkthrough scripts don't reference GodotTestDriver directly.
/// </summary>
public class AutoPilotActions
{
    private readonly AutoPilot _pilot;
    private readonly HashSet<string> _heldActions = new();

    public AutoPilotActions(AutoPilot pilot)
    {
        _pilot = pilot;
    }

    // ── Input injection (via GodotTestDriver) ────────────────────────────────

    /// <summary>Single tap: press this frame, release next frame.</summary>
    public async Task Press(string action)
    {
        _pilot.StartAction(action);
        await WaitFrames(2);
        _pilot.EndAction(action);

#if DEBUG
        DebugTelemetry.Instance?.LogInput(action, true);
#endif
    }

    /// <summary>Start holding an action. Stays held until Release() or ReleaseAll().</summary>
    public void Hold(string action)
    {
        if (_heldActions.Add(action))
        {
            _pilot.StartAction(action);
#if DEBUG
            DebugTelemetry.Instance?.LogInput(action, true);
#endif
        }
    }

    /// <summary>Stop holding a specific action.</summary>
    public void Release(string action)
    {
        if (_heldActions.Remove(action))
        {
            _pilot.EndAction(action);
#if DEBUG
            DebugTelemetry.Instance?.LogInput(action, false);
#endif
        }
    }

    /// <summary>Release all held actions.</summary>
    public void ReleaseAll()
    {
        foreach (string action in _heldActions)
            _pilot.EndAction(action);
        _heldActions.Clear();
    }

    /// <summary>No-op — GodotTestDriver handles release timing internally.</summary>
    internal void ProcessPendingReleases() { }

    // ── Movement ─────────────────────────────────────────────────────────────

    /// <summary>Hold movement keys for a direction over N seconds, then release.</summary>
    public async Task MoveDirection(Vector2 dir, float seconds)
    {
        if (dir.X > 0) Hold(Constants.InputActions.MoveRight);
        else if (dir.X < 0) Hold(Constants.InputActions.MoveLeft);

        if (dir.Y > 0) Hold(Constants.InputActions.MoveDown);
        else if (dir.Y < 0) Hold(Constants.InputActions.MoveUp);

        await WaitSeconds(seconds);
        ReleaseAll();
    }

    /// <summary>Move toward a world position until within threshold or timeout.</summary>
    public async Task MoveToward(Vector2 target, float timeout = 10f, float threshold = 40f)
    {
        float elapsed = 0f;
        while (elapsed < timeout)
        {
            var player = GetPlayerNode();
            if (player == null)
            {
                await WaitFrames(10);
                elapsed += 0.16f;
                continue;
            }

            Vector2 delta = target - player.GlobalPosition;
            if (delta.Length() < threshold)
            {
                ReleaseAll();
                return;
            }

            ReleaseAll();
            Vector2 dir = delta.Normalized();
            if (Math.Abs(dir.X) > 0.3f)
                Hold(dir.X > 0 ? Constants.InputActions.MoveRight : Constants.InputActions.MoveLeft);
            if (Math.Abs(dir.Y) > 0.3f)
                Hold(dir.Y > 0 ? Constants.InputActions.MoveDown : Constants.InputActions.MoveUp);

            await WaitFrames(5);
            elapsed += 5f / 60f;
        }

        ReleaseAll();
        throw new TimeoutException($"Could not reach {target} within {timeout}s");
    }

    // ── UI interaction ───────────────────────────────────────────────────────

    /// <summary>Click a button by emitting its pressed signal.</summary>
    public void ClickButton(Button button)
    {
        button.EmitSignal(BaseButton.SignalName.Pressed);
    }

    /// <summary>Find a Button by its text label within a parent node tree.</summary>
    public Button? FindButton(Node parent, string text)
    {
        return SearchForButton(parent, text);
    }

    private static Button? SearchForButton(Node parent, string text)
    {
        if (parent is Button btn && btn.Text == text && btn.Visible)
            return btn;

        foreach (var child in parent.GetChildren())
        {
            var found = SearchForButton(child, text);
            if (found != null) return found;
        }
        return null;
    }

    // ── Waiting ──────────────────────────────────────────────────────────────

    /// <summary>Wait N physics frames.</summary>
    public async Task WaitFrames(int count)
    {
        for (int i = 0; i < count; i++)
            await _pilot.ToSignal(_pilot.GetTree(), SceneTree.SignalName.PhysicsFrame);
    }

    /// <summary>Wait real-time seconds.</summary>
    public async Task WaitSeconds(float seconds)
    {
        await _pilot.ToSignal(
            _pilot.GetTree().CreateTimer(seconds),
            SceneTreeTimer.SignalName.Timeout);
    }

    /// <summary>Poll until condition is true, or throw TimeoutException.</summary>
    public async Task WaitUntil(Func<bool> condition, float timeout = 10f)
    {
        float elapsed = 0f;
        while (elapsed < timeout)
        {
            if (condition()) return;
            await WaitFrames(3);
            elapsed += 3f / 60f;
        }
        throw new TimeoutException($"Condition not met within {timeout}s");
    }

    /// <summary>Wait for ScreenTransition to finish (if active).</summary>
    public async Task WaitForTransition(float timeout = 10f)
    {
        await WaitFrames(5);
        await WaitUntil(() =>
        {
            var st = Ui.ScreenTransition.Instance;
            return st == null || !st.IsTransitioning;
        }, timeout);
        await WaitFrames(5);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Node2D? GetPlayerNode()
    {
        var players = _pilot.GetTree().GetNodesInGroup(Constants.Groups.Player);
        return players.Count > 0 ? players[0] as Node2D : null;
    }
}
