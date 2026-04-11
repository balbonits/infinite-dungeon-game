using System;

namespace DungeonGame;

/// <summary>
/// Adaptive AI Director. Monitors player performance and adjusts dungeon parameters.
/// Session-scoped — resets on town return or save load. Invisible to the player.
/// Spec: docs/systems/dungeon-intelligence.md
/// </summary>
public class DungeonIntelligence
{
    // Metric weights
    private const float KillSpeedWeight = 0.35f;
    private const float DamageEffWeight = 0.25f;
    private const float FloorPaceWeight = 0.25f;
    private const float DeathWeight = -0.15f;

    // Pressure bounds
    private const float MinPressure = 0.5f;
    private const float MaxPressure = 1.8f;

    // Adjustment bounds
    private const float MinSpawnMod = 0.80f;
    private const float MaxSpawnMod = 1.20f;
    private const float MinAggroMod = 0.85f;
    private const float MaxAggroMod = 1.15f;
    private const float MaxEliteShift = 0.04f;

    // Rolling window (60 seconds for damage efficiency)
    private const float DamageWindowSeconds = 60f;
    private const float GracePeriodSeconds = 60f;
    private const float RecalcIntervalSeconds = 5f;
    private const float DeathDecayMinutes = 10f;

    // Session state
    private float _sessionSeconds;
    private int _totalKills;
    private float _damageDealtWindow;
    private float _damageTakenWindow;
    private int _floorsCleared;
    private float _deathWeight;
    private float _lastDeathDecayCheck;
    private int _currentFloor;
    private float _lastRecalcTime;

    /// <summary>Current pressure score (0.5 to 1.8). 1.0 = balanced.</summary>
    public float PressureScore { get; private set; } = 1.0f;

    /// <summary>Whether the grace period is active (no adjustments applied).</summary>
    public bool InGracePeriod => _sessionSeconds < GracePeriodSeconds;

    // --- Input events (called by game systems) ---

    public void RecordKill()
    {
        _totalKills++;
    }

    public void RecordDamageDealt(float amount)
    {
        _damageDealtWindow += amount;
    }

    public void RecordDamageTaken(float amount)
    {
        _damageTakenWindow += amount;
    }

    public void RecordFloorCleared()
    {
        _floorsCleared++;
    }

    public void RecordDeath()
    {
        _deathWeight += 2.0f;
    }

    public void SetCurrentFloor(int floor)
    {
        _currentFloor = floor;
    }

    /// <summary>Tick the session timer and recalculate if needed. Call every frame with delta.</summary>
    public void Update(float delta)
    {
        _sessionSeconds += delta;

        // Death weight decay: halve every 10 minutes of survival
        if (_sessionSeconds - _lastDeathDecayCheck >= DeathDecayMinutes * 60f)
        {
            _deathWeight *= 0.5f;
            _lastDeathDecayCheck = _sessionSeconds;
        }

        // Recalculate pressure every 5 seconds
        if (_sessionSeconds - _lastRecalcTime >= RecalcIntervalSeconds)
        {
            RecalculatePressure();
            _lastRecalcTime = _sessionSeconds;
        }

        // Decay the rolling damage window (simple exponential decay approximation)
        float decayFactor = 1.0f - (delta / DamageWindowSeconds);
        _damageDealtWindow *= Math.Max(0f, decayFactor);
        _damageTakenWindow *= Math.Max(0f, decayFactor);
    }

    private void RecalculatePressure()
    {
        if (_sessionSeconds < 1f) return;

        // Kill speed ratio
        float sessionMinutes = _sessionSeconds / 60f;
        float expectedKpm = 6f + _currentFloor * 0.05f;
        float actualKpm = _totalKills / Math.Max(1f, sessionMinutes);
        float ksRatio = actualKpm / Math.Max(1f, expectedKpm);

        // Damage efficiency
        float deRatio = _damageDealtWindow / Math.Max(1f, _damageTakenWindow);
        // Normalize: 5.0 ratio = 1.0 score, scale linearly
        float deNormalized = deRatio / 5.0f;

        // Floor pace
        float expectedPace = 1f / Math.Max(5f, 10f - _currentFloor * 0.02f);
        float actualPace = _floorsCleared / Math.Max(1f, sessionMinutes);
        float fpRatio = actualPace / Math.Max(0.001f, expectedPace);

        // Combine
        float raw = (ksRatio * KillSpeedWeight) +
                    (deNormalized * DamageEffWeight) +
                    (fpRatio * FloorPaceWeight) +
                    (_deathWeight * DeathWeight);

        PressureScore = Math.Clamp(raw, MinPressure, MaxPressure);
    }

    // --- Adjustment modifiers (read by game systems) ---

    /// <summary>Spawn rate multiplier (0.80 to 1.20). Applied to room budget and respawn timer.</summary>
    public float SpawnRateModifier
    {
        get
        {
            if (InGracePeriod) return 1.0f;
            float mod = 0.8f + (PressureScore - 0.5f) * 0.31f;
            return Math.Clamp(mod, MinSpawnMod, MaxSpawnMod);
        }
    }

    /// <summary>Enemy aggression multiplier (0.85 to 1.15). Applied to aggro range and attack cooldown.</summary>
    public float AggressionModifier
    {
        get
        {
            if (InGracePeriod) return 1.0f;
            float mod = 0.85f + (PressureScore - 0.5f) * 0.23f;
            return Math.Clamp(mod, MinAggroMod, MaxAggroMod);
        }
    }

    /// <summary>Elite frequency shift (0.0 to 0.04). Additive to Named spawn threshold.</summary>
    public float EliteFrequencyShift
    {
        get
        {
            if (InGracePeriod) return 0f;
            float shift = (PressureScore - 1.0f) * 0.05f;
            return Math.Clamp(shift, 0f, MaxEliteShift);
        }
    }

    /// <summary>Loot quality bonus for struggling players. (Superior bonus, Elite bonus).</summary>
    public (float superior, float elite) LootQualityBonus
    {
        get
        {
            if (InGracePeriod || PressureScore >= 0.85f)
                return (0f, 0f);
            float deficit = 0.85f - PressureScore;
            return (deficit * 20f / 100f, deficit * 20f * 0.43f / 100f);
        }
    }

    /// <summary>Reset all metrics. Called on town return, save load, or new session.</summary>
    public void Reset()
    {
        _sessionSeconds = 0;
        _totalKills = 0;
        _damageDealtWindow = 0;
        _damageTakenWindow = 0;
        _floorsCleared = 0;
        _deathWeight = 0;
        _lastDeathDecayCheck = 0;
        _currentFloor = 1;
        _lastRecalcTime = 0;
        PressureScore = 1.0f;
    }
}
