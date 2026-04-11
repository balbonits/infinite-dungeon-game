using System;

namespace DungeonGame;

/// <summary>
/// Voluntary difficulty modifiers toggled at the Pact Altar.
/// Each pact has ranks that contribute heat; heat drives reward scaling.
/// Spec: docs/systems/dungeon-pacts.md
/// </summary>
public class DungeonPacts
{
    public const int PactCount = 10;

    // Max ranks per pact (indexed 0-9)
    private static readonly int[] MaxRanks = { 5, 4, 4, 3, 4, 3, 3, 3, 3, 3 };

    // Heat values per rank for each pact (cumulative per rank, not per level)
    private static readonly int[][] HeatPerRank =
    {
        new[] { 3, 5, 7, 10, 15 },  // PACT-01 Swelling Horde
        new[] { 3, 5, 8, 12 },       // PACT-02 Iron Will
        new[] { 4, 7, 10, 14 },      // PACT-03 Sharpened Claws
        new[] { 3, 5, 8 },           // PACT-04 Quicksilver Blood
        new[] { 4, 7, 10, 14 },      // PACT-05 Empowered Masses
        new[] { 3, 5, 8 },           // PACT-06 Waning Light
        new[] { 3, 6, 9 },           // PACT-07 Fading Vitality
        new[] { 2, 4, 6 },           // PACT-08 Relentless Pursuit
        new[] { 3, 5, 8 },           // PACT-09 Hollow Ground
        new[] { 5, 8, 12 },          // PACT-10 Dungeon's Favor
    };

    /// <summary>Current rank per pact (0 = inactive).</summary>
    private readonly int[] _ranks = new int[PactCount];

    // --- Rank access ---

    public int GetRank(int pactIndex) => _ranks[Math.Clamp(pactIndex, 0, PactCount - 1)];

    public void SetRank(int pactIndex, int rank)
    {
        int idx = Math.Clamp(pactIndex, 0, PactCount - 1);
        _ranks[idx] = Math.Clamp(rank, 0, MaxRanks[idx]);
    }

    public int GetMaxRank(int pactIndex) => MaxRanks[Math.Clamp(pactIndex, 0, PactCount - 1)];

    // --- Heat calculation ---

    /// <summary>Total heat from all active pacts.</summary>
    public int TotalHeat
    {
        get
        {
            int heat = 0;
            for (int i = 0; i < PactCount; i++)
            {
                int rank = _ranks[i];
                if (rank > 0)
                    heat += HeatPerRank[i][rank - 1];
            }
            return heat;
        }
    }

    // --- Reward scaling (from heat) ---

    public float XpBonus => TotalHeat * 0.015f;              // 1.5% per heat
    public float GoldBonus => TotalHeat * 0.01f;             // 1.0% per heat
    public float MaterialDropBonus => TotalHeat * 0.008f;    // 0.8% per heat
    public float EquipDropBonus => TotalHeat * 0.005f;       // 0.5% per heat
    public int QualityShift => TotalHeat / 30;               // +1 tier per 30 heat

    // --- Pact effects (applied to enemies/player) ---

    // PACT-01: Swelling Horde — room budget multiplier
    public float HordeBudgetMultiplier => 1.0f + GetRank(0) * 0.20f;

    // PACT-02: Iron Will — enemy HP multiplier
    private static readonly float[] IronWillMults = { 1.0f, 1.25f, 1.50f, 1.80f, 2.20f };
    public float EnemyHpMultiplier => IronWillMults[GetRank(1)];

    // PACT-03: Sharpened Claws — enemy damage multiplier
    private static readonly float[] ClawsMults = { 1.0f, 1.20f, 1.40f, 1.65f, 2.00f };
    public float EnemyDamageMultiplier => ClawsMults[GetRank(2)];

    // PACT-04: Quicksilver Blood — enemy speed/cooldown
    private static readonly float[] SpeedMults = { 1.0f, 1.15f, 1.30f, 1.50f };
    private static readonly float[] CooldownMults = { 1.0f, 0.90f, 0.80f, 0.70f };
    public float EnemySpeedMultiplier => SpeedMults[GetRank(3)];
    public float EnemyCooldownMultiplier => CooldownMults[GetRank(3)];

    // PACT-05: Empowered Masses — rarity thresholds (Normal/Empowered/Named)
    private static readonly (float normal, float empowered, float named)[] RarityThresholds =
    {
        (0.78f, 0.20f, 0.02f), // rank 0
        (0.65f, 0.30f, 0.05f), // rank 1
        (0.50f, 0.40f, 0.10f), // rank 2
        (0.35f, 0.45f, 0.20f), // rank 3
        (0.20f, 0.50f, 0.30f), // rank 4
    };
    public (float normal, float empowered, float named) GetRarityThresholds() =>
        RarityThresholds[GetRank(4)];

    // PACT-06: Waning Light — resistance penalty
    private static readonly int[] ResistPenalties = { 0, 10, 20, 35 };
    public int ResistancePenalty => ResistPenalties[GetRank(5)];

    // PACT-07: Fading Vitality — HP regen reduction (0.0 to 1.0)
    private static readonly float[] RegenReductions = { 0f, 0.30f, 0.60f, 1.00f };
    public float HpRegenReduction => RegenReductions[GetRank(6)];

    // PACT-08: Relentless Pursuit — aggro/leash modifiers
    private static readonly float[] AggroMults = { 1.0f, 1.25f, 1.50f, 1.75f };
    private static readonly float[] LeashMults = { 1.0f, 1.0f, 1.25f, 1.50f };
    public float AggroRangeMultiplier => AggroMults[GetRank(7)];
    public float LeashRangeMultiplier => LeashMults[GetRank(7)];

    // PACT-09: Hollow Ground — safe zone reduction
    private static readonly float[] SafeZoneRadiusMults = { 1.0f, 0.60f, 0.30f, 0.0f };
    public float SafeZoneRadiusMultiplier => SafeZoneRadiusMults[GetRank(8)];
    public bool SafeZonesDisabled => GetRank(8) >= 3;

    // PACT-10: Dungeon's Favor — boss buffs
    private static readonly float[] BossHpMults = { 1.0f, 1.50f, 2.00f, 2.50f };
    private static readonly float[] BossDmgMults = { 1.0f, 1.0f, 1.25f, 1.50f };
    private static readonly float[] BossSpeedMults = { 1.0f, 1.0f, 1.0f, 1.20f };
    private static readonly int[] BossExtraMods = { 0, 1, 2, 3 };
    public float BossHpMultiplier => BossHpMults[GetRank(9)];
    public float BossDamageMultiplier => BossDmgMults[GetRank(9)];
    public float BossSpeedMultiplier => BossSpeedMults[GetRank(9)];
    public int BossExtraModifiers => BossExtraMods[GetRank(9)];

    // --- Serialization ---

    public int[] ExportRanks() => (int[])_ranks.Clone();

    public void ImportRanks(int[]? ranks)
    {
        Array.Clear(_ranks, 0, PactCount);
        if (ranks == null) return;
        for (int i = 0; i < Math.Min(ranks.Length, PactCount); i++)
            _ranks[i] = Math.Clamp(ranks[i], 0, MaxRanks[i]);
    }

    public void Reset() => Array.Clear(_ranks, 0, PactCount);
}
