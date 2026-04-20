using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

/// <summary>
/// COMBAT-01 §7 + §8 soft-cap math. Pure pure — no Godot deps; these
/// formulas underwrite every combat-ring calculation in the game.
/// </summary>
public class CombatFormulasTests
{
    // ── SoftCap curve ────────────────────────────────────────────────────

    [Fact]
    public void SoftCap_ZeroRaw_ReturnsZero()
    {
        CombatFormulas.SoftCap(0f).Should().Be(0f);
    }

    [Fact]
    public void SoftCap_NegativeRaw_ReturnsZero()
    {
        // Defensive — negative raw shouldn't happen but guard against it
        // propagating into a negative effective chance.
        CombatFormulas.SoftCap(-10f).Should().Be(0f);
    }

    [Fact]
    public void SoftCap_At60Raw_Returns30()
    {
        // raw = K = 60 → effective = 60 * (60 / 120) = 30.
        CombatFormulas.SoftCap(60f).Should().BeApproximately(30f, 0.001f);
    }

    [Fact]
    public void SoftCap_At300Raw_Returns50()
    {
        // raw = 300 = 5×K → effective = 300 * (60 / 360) = 50.
        CombatFormulas.SoftCap(300f).Should().BeApproximately(50f, 0.001f);
    }

    [Fact]
    public void SoftCap_AsymptotesAt60ButNeverReaches()
    {
        // At very high raw, effective should approach but not reach 60.
        float eff10k = CombatFormulas.SoftCap(10_000f);
        eff10k.Should().BeLessThan(60f);
        eff10k.Should().BeGreaterThan(59.5f);
    }

    // ── Overflow ─────────────────────────────────────────────────────────

    [Fact]
    public void Overflow_ZeroRaw_ReturnsZero()
    {
        CombatFormulas.Overflow(0f).Should().Be(0f);
    }

    [Fact]
    public void Overflow_At60Raw_Returns30()
    {
        // raw 60 → eff 30 → overflow 30.
        CombatFormulas.Overflow(60f).Should().BeApproximately(30f, 0.001f);
    }

    [Fact]
    public void Overflow_At300Raw_Returns250()
    {
        // raw 300 → eff 50 → overflow 250.
        CombatFormulas.Overflow(300f).Should().BeApproximately(250f, 0.001f);
    }

    [Fact]
    public void Overflow_SumOfEffectivePlusOverflowEqualsRaw()
    {
        // Invariant: effective + overflow = raw for any non-negative raw.
        foreach (float raw in new[] { 1f, 24f, 60f, 100f, 150f, 500f })
        {
            float sum = CombatFormulas.SoftCap(raw) + CombatFormulas.Overflow(raw);
            sum.Should().BeApproximately(raw, 0.001f);
        }
    }

    // ── Crit damage multiplier (unbounded) ───────────────────────────────

    [Fact]
    public void CritDamageMultiplier_ZeroRaw_IsBase15()
    {
        CombatFormulas.CritDamageMultiplier(0f).Should().BeApproximately(1.5f, 0.001f);
    }

    [Fact]
    public void CritDamageMultiplier_At100Raw_Is275()
    {
        // Spec worked example: raw = 100 → overflow 62.5 → bonus 1.25 → 2.75× total.
        CombatFormulas.CritDamageMultiplier(100f).Should().BeApproximately(2.75f, 0.001f);
    }

    [Fact]
    public void CritDamageMultiplier_GrowsWithoutBound()
    {
        // Crit damage has NO hard cap per spec — bigger raw = bigger multiplier.
        float m100 = CombatFormulas.CritDamageMultiplier(100f);
        float m500 = CombatFormulas.CritDamageMultiplier(500f);
        m500.Should().BeGreaterThan(m100);
    }

    // ── Flurry (40% hard cap) ────────────────────────────────────────────

    [Fact]
    public void FlurryChance_ZeroRaw_IsZero()
    {
        CombatFormulas.FlurryChance(0f).Should().Be(0f);
    }

    [Fact]
    public void FlurryChance_CapsAt40Percent()
    {
        // Overflow * 0.005 reaches 0.40 at overflow 80; very high haste
        // must not exceed 40%.
        CombatFormulas.FlurryChance(10_000f).Should().BeApproximately(0.40f, 0.001f);
    }

    [Fact]
    public void FlurryChance_AtSoftCapReach_IsProportional()
    {
        // raw 60 → overflow 30 → Flurry = 30 * 0.005 = 0.15.
        CombatFormulas.FlurryChance(60f).Should().BeApproximately(0.15f, 0.001f);
    }

    // ── Phase (500 ms hard cap) ─────────────────────────────────────────

    [Fact]
    public void PhaseDurationMs_ZeroRaw_IsZero()
    {
        CombatFormulas.PhaseDurationMs(0f).Should().Be(0f);
    }

    [Fact]
    public void PhaseDurationMs_CapsAt500Ms()
    {
        // Overflow ms 1:1, hard cap 500. High raw cannot exceed.
        CombatFormulas.PhaseDurationMs(10_000f).Should().Be(500f);
    }

    [Fact]
    public void PhaseDurationMs_At60Raw_Is30Ms()
    {
        // raw 60 → overflow 30 → 30 ms Phase.
        CombatFormulas.PhaseDurationMs(60f).Should().BeApproximately(30f, 0.001f);
    }

    // ── Block (80% total hard cap) ──────────────────────────────────────

    [Fact]
    public void BlockReduction_ZeroRaw_IsBase50()
    {
        CombatFormulas.BlockReduction(0f).Should().BeApproximately(0.50f, 0.001f);
    }

    [Fact]
    public void BlockReduction_CapsAt80Percent()
    {
        // Baseline 0.5 + overflow × 0.005 capped at +0.30 → 0.80 total.
        CombatFormulas.BlockReduction(10_000f).Should().BeApproximately(0.80f, 0.001f);
    }

    [Fact]
    public void BlockReduction_At60Raw_Is65Percent()
    {
        // raw 60 → overflow 30 → +0.15 → 0.65 total.
        CombatFormulas.BlockReduction(60f).Should().BeApproximately(0.65f, 0.001f);
    }
}
