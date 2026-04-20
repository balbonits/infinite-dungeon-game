using System;

namespace DungeonGame;

/// <summary>
/// Pure math for COMBAT-01's split chance-vs-power soft-cap model.
/// No Godot dependency; trivially unit-testable.
///
/// Spec: docs/systems/combat-equipment-integration.md §7.
///
/// The soft-cap curve is the same shape as stats.md's universal DR
/// (`raw * (K / (raw + K))`) with K=60 — so players who already
/// understand stat DR immediately understand combat DR.
/// </summary>
public static class CombatFormulas
{
    /// <summary>
    /// Soft-cap at 60% — curve asymptotes toward 60, never reaches it,
    /// no hard wall. Overflow = raw - effective (grows without bound).
    /// </summary>
    public const float SoftCapK = 60f;

    /// <summary>Effective % after soft-cap diminishing returns.</summary>
    public static float SoftCap(float raw) =>
        raw <= 0f ? 0f : raw * (SoftCapK / (raw + SoftCapK));

    /// <summary>Portion of raw% above the soft-cap curve — always ≥ 0.</summary>
    public static float Overflow(float raw) =>
        raw <= 0f ? 0f : raw - SoftCap(raw);

    // ─── Per-focus overflow conversions (all bounded where the spec demands) ───

    /// <summary>
    /// Crit damage multiplier = 1.5 (base) + overflow × 0.02 (no hard cap).
    /// Applied to final damage on a successful crit roll.
    /// </summary>
    public static float CritDamageMultiplier(float critRaw) =>
        1.5f + Overflow(critRaw) * 0.02f;

    /// <summary>
    /// Flurry (free extra swing) chance. Overflow × 0.005 clamped to 0.40.
    /// Hard cap at 40% is spec-mandated (§7): higher would override enemy
    /// telegraph timing.
    /// </summary>
    public const float FlurryHardCap = 0.40f;
    public static float FlurryChance(float hasteRaw) =>
        Math.Min(Overflow(hasteRaw) * 0.005f, FlurryHardCap);

    /// <summary>
    /// Phase (i-frame) duration in milliseconds after a successful dodge.
    /// Overflow (raw %) mapped 1:1 to ms; hard-capped at 500 ms per §7.
    /// </summary>
    public const float PhaseHardCapMs = 500f;
    public static float PhaseDurationMs(float dodgeRaw) =>
        Math.Min(Overflow(dodgeRaw), PhaseHardCapMs);

    /// <summary>
    /// Block damage reduction — base 50% on a successful block, plus
    /// overflow × 0.005 (up to an additional 30%). Total reduction
    /// hard-capped at 80% per §7. Returned as a 0..1 fraction.
    /// </summary>
    public const float BlockBaseReduction = 0.50f;
    public const float BlockOverflowCap = 0.30f;
    public static float BlockReduction(float blockRaw) =>
        BlockBaseReduction + Math.Min(Overflow(blockRaw) * 0.005f, BlockOverflowCap);
}
