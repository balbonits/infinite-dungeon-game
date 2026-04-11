using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonGame;

/// <summary>
/// Per-zone farming difficulty dial. Saturation builds per kill and decays over time.
/// Higher saturation = harder enemies + better loot. Pure logic — no Godot dependency.
/// Spec: docs/systems/zone-saturation.md
/// </summary>
public class ZoneSaturation
{
    private const float BaseGainPerKill = 0.15f;
    private const float ZoneGainScaling = 0.05f; // gain_mult += 0.05 per zone above 1
    private const float DecayPerMinute = 0.25f;
    private const float MaxSaturation = 100f;

    // Stat multiplier caps at 100% saturation
    private const float HpMultMax = 0.50f;
    private const float DamageMultMax = 0.35f;
    private const float SpeedMultMax = 0.15f;

    // Reward bonuses at 100% saturation
    private const float XpBonusMax = 0.30f;
    private const float MaterialDropBonusMax = 0.40f;
    private const float QualityShiftMax = 20f; // floors
    private const float EquipDropBonusMax = 0.20f;

    /// <summary>Per-zone saturation values (zone number → saturation 0-100).</summary>
    private readonly Dictionary<int, float> _saturation = new();

    /// <summary>Timestamp of last decay calculation (Unix seconds).</summary>
    public double LastDecayTimestamp { get; set; }

    /// <summary>Get current saturation for a zone (0-100).</summary>
    public float GetSaturation(int zone) =>
        _saturation.TryGetValue(zone, out float val) ? val : 0f;

    /// <summary>Record a kill in the given zone, increasing saturation.</summary>
    public void RecordKill(int zone)
    {
        float zoneMult = 1.0f + (zone - 1) * ZoneGainScaling;
        float gain = BaseGainPerKill * zoneMult;
        float current = GetSaturation(zone);
        _saturation[zone] = Math.Min(MaxSaturation, current + gain);
    }

    /// <summary>Apply time-based decay to all zones. Call on session load or periodically.</summary>
    public void ApplyDecay(double currentTimestamp)
    {
        if (LastDecayTimestamp <= 0)
        {
            LastDecayTimestamp = currentTimestamp;
            return;
        }

        double elapsedMinutes = (currentTimestamp - LastDecayTimestamp) / 60.0;
        if (elapsedMinutes <= 0) return;

        float totalDecay = (float)(DecayPerMinute * elapsedMinutes);
        var zones = _saturation.Keys.ToList();
        foreach (int zone in zones)
        {
            _saturation[zone] = Math.Max(0f, _saturation[zone] - totalDecay);
            if (_saturation[zone] <= 0f)
                _saturation.Remove(zone);
        }

        LastDecayTimestamp = currentTimestamp;
    }

    /// <summary>Decay all zones except the one the player is currently in.</summary>
    public void ApplyDecayExcluding(int currentZone, double currentTimestamp)
    {
        if (LastDecayTimestamp <= 0)
        {
            LastDecayTimestamp = currentTimestamp;
            return;
        }

        double elapsedMinutes = (currentTimestamp - LastDecayTimestamp) / 60.0;
        if (elapsedMinutes <= 0) return;

        float totalDecay = (float)(DecayPerMinute * elapsedMinutes);
        var zones = _saturation.Keys.ToList();
        foreach (int zone in zones)
        {
            if (zone == currentZone) continue;
            _saturation[zone] = Math.Max(0f, _saturation[zone] - totalDecay);
            if (_saturation[zone] <= 0f)
                _saturation.Remove(zone);
        }

        LastDecayTimestamp = currentTimestamp;
    }

    // --- Stat multipliers (applied to enemies in the zone) ---

    private float Ratio(int zone) => GetSaturation(zone) / MaxSaturation;

    public float GetHpMultiplier(int zone) => 1.0f + Ratio(zone) * HpMultMax;
    public float GetDamageMultiplier(int zone) => 1.0f + Ratio(zone) * DamageMultMax;
    public float GetSpeedMultiplier(int zone) => 1.0f + Ratio(zone) * SpeedMultMax;

    // --- Reward bonuses ---

    /// <summary>Additive XP bonus (0.0 to 0.30).</summary>
    public float GetXpBonus(int zone) => Ratio(zone) * XpBonusMax;

    /// <summary>Material drop chance multiplier (1.0 to 1.40).</summary>
    public float GetMaterialDropMultiplier(int zone) => 1.0f + Ratio(zone) * MaterialDropBonusMax;

    /// <summary>Floor offset for quality distribution lookup.</summary>
    public int GetQualityShiftFloors(int zone) => (int)(Ratio(zone) * QualityShiftMax);

    /// <summary>Equipment drop chance multiplier (1.0 to 1.20).</summary>
    public float GetEquipDropMultiplier(int zone) => 1.0f + Ratio(zone) * EquipDropBonusMax;

    // --- Serialization ---

    public Dictionary<int, float> ExportState() => new(_saturation);

    public void ImportState(Dictionary<int, float>? state, double lastTimestamp)
    {
        _saturation.Clear();
        if (state != null)
        {
            foreach (var kvp in state)
                if (kvp.Value > 0f)
                    _saturation[kvp.Key] = Math.Min(MaxSaturation, kvp.Value);
        }
        LastDecayTimestamp = lastTimestamp;
    }

    public void Reset()
    {
        _saturation.Clear();
        LastDecayTimestamp = 0;
    }
}
