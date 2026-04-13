#if DEBUG
using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace DungeonGame.Testing;

/// <summary>
/// Full audit trail for debugging and testing.
/// Tracks input consumption, signal emissions, state snapshots, and scene changes.
/// Writes to user://debug_telemetry_{timestamp}.jsonl (per-session file).
/// Compiled out for release builds (#if DEBUG).
///
/// Usage:
///   - Auto-starts when added to scene tree
///   - Debug console: telemetry start/stop/dump
///   - AutoPilot injects it automatically during walkthroughs
/// </summary>
public partial class DebugTelemetry : Node
{
    public static DebugTelemetry? Instance { get; private set; }

    private readonly List<Dictionary<string, object>> _events = new();
    private bool _recording;
    private string _sessionFile = "";
    private double _startTime;

    public bool IsRecording => _recording;
    public int EventCount => _events.Count;

    public override void _Ready()
    {
        Instance = this;
        Name = "DebugTelemetry";
        ProcessMode = ProcessModeEnum.Always;
    }

    // ── Recording control ────────────────────────────────────────────────────

    public void StartRecording()
    {
        if (_recording) return;
        _recording = true;
        _events.Clear();
        _startTime = Time.GetTicksMsec() / 1000.0;

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        _sessionFile = $"user://debug_telemetry_{timestamp}.jsonl";

        LogEvent("session", "start", new Dictionary<string, object>
        {
            ["timestamp"] = timestamp,
        });

        ConnectSignals();
        GD.Print("[TELEMETRY] Recording started → " + _sessionFile);
    }

    public void StopRecording()
    {
        if (!_recording) return;
        LogEvent("session", "stop", null);
        _recording = false;
        DisconnectSignals();
        FlushToDisk();
        GD.Print($"[TELEMETRY] Recording stopped. {_events.Count} events → {_sessionFile}");
    }

    public void DumpToConsole()
    {
        GD.Print($"[TELEMETRY] === Dump ({_events.Count} events) ===");
        foreach (var evt in _events)
        {
            string json = JsonSerializer.Serialize(evt);
            GD.Print($"  {json}");
        }
        GD.Print("[TELEMETRY] === End dump ===");
    }

    // ── Event logging ────────────────────────────────────────────────────────

    public void LogEvent(string category, string action, Dictionary<string, object>? data)
    {
        if (!_recording) return;

        double elapsed = Time.GetTicksMsec() / 1000.0 - _startTime;
        var evt = new Dictionary<string, object>
        {
            ["t"] = Math.Round(elapsed, 3),
            ["cat"] = category,
            ["act"] = action,
        };
        if (data != null)
        {
            foreach (var kvp in data)
                evt[kvp.Key] = kvp.Value;
        }
        _events.Add(evt);
    }

    public void LogInput(string action, bool pressed)
    {
        LogEvent("input", pressed ? "press" : "release", new Dictionary<string, object>
        {
            ["action"] = action,
        });
    }

    public void LogSceneChange(string scenePath)
    {
        LogEvent("scene", "change", new Dictionary<string, object>
        {
            ["path"] = scenePath,
        });
    }

    public void LogSignal(string signalName, string source)
    {
        LogEvent("signal", signalName, new Dictionary<string, object>
        {
            ["source"] = source,
        });
    }

    /// <summary>Snapshot current GameState values.</summary>
    public void LogStateSnapshot()
    {
        var gs = Autoloads.GameState.Instance;
        if (gs == null) return;

        LogEvent("state", "snapshot", new Dictionary<string, object>
        {
            ["hp"] = gs.Hp,
            ["maxHp"] = gs.MaxHp,
            ["mana"] = gs.Mana,
            ["maxMana"] = gs.MaxMana,
            ["xp"] = gs.Xp,
            ["level"] = gs.Level,
            ["floor"] = gs.FloorNumber,
            ["gold"] = gs.PlayerInventory.Gold,
            ["invSlots"] = gs.PlayerInventory.UsedSlots,
            ["isDead"] = gs.IsDead,
            ["class"] = gs.SelectedClass.ToString(),
        });
    }

    // ── Signal connections ────────────────────────────────────────────────────

    private readonly List<(GodotObject source, StringName signal, Callable callable)> _connections = new();

    private void ConnectSignals()
    {
        var gs = Autoloads.GameState.Instance;
        if (gs != null)
        {
            ConnectSafe(gs, Autoloads.GameState.SignalName.StatsChanged,
                Callable.From(() => LogSignal("StatsChanged", "GameState")));
            ConnectSafe(gs, Autoloads.GameState.SignalName.PlayerDied,
                Callable.From(() =>
                {
                    LogSignal("PlayerDied", "GameState");
                    LogStateSnapshot();
                }));
        }

        var bus = Autoloads.EventBus.Instance;
        if (bus != null)
        {
            ConnectSafe(bus, "EnemyDefeated",
                Callable.From((Vector2 pos, int tier) =>
                    LogSignal("EnemyDefeated", $"tier={tier}")));
        }
    }

    private void ConnectSafe(GodotObject source, StringName signal, Callable callable)
    {
        if (source.HasSignal(signal) && !source.IsConnected(signal, callable))
        {
            source.Connect(signal, callable);
            _connections.Add((source, signal, callable));
        }
    }

    private void DisconnectSignals()
    {
        foreach (var (source, signal, callable) in _connections)
        {
            if (GodotObject.IsInstanceValid(source) && source.IsConnected(signal, callable))
                source.Disconnect(signal, callable);
        }
        _connections.Clear();
    }

    // ── Periodic state snapshots ─────────────────────────────────────────────

    private double _snapshotTimer;
    private const double SnapshotInterval = 2.0; // every 2 seconds

    public override void _Process(double delta)
    {
        if (!_recording) return;

        _snapshotTimer += delta;
        if (_snapshotTimer >= SnapshotInterval)
        {
            _snapshotTimer = 0;
            LogStateSnapshot();
        }
    }

    // ── File output ──────────────────────────────────────────────────────────

    private void FlushToDisk()
    {
        using var file = FileAccess.Open(_sessionFile, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PrintErr($"[TELEMETRY] Failed to write: {_sessionFile}");
            return;
        }

        foreach (var evt in _events)
        {
            string line = JsonSerializer.Serialize(evt);
            file.StoreLine(line);
        }
    }

    // ── Cleanup ──────────────────────────────────────────────────────────────

    public override void _ExitTree()
    {
        if (_recording)
            StopRecording();
        Instance = null;
    }
}
#endif
