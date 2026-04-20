using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

/// <summary>
/// AUDIT-12 regression coverage. DungeonIntelligence is 199 LOC of adaptive-AI-director
/// logic with time-based decay, rolling damage windows, and multi-metric pressure-score
/// blending — completely untested before this file. Covers the metric inputs, pressure
/// clamping, grace-period freeze, modifier bounds, loot-bonus gating, and reset.
/// </summary>
public class DungeonIntelligenceTests
{
    // -- Initial state --

    [Fact]
    public void New_PressureScore_IsBalanced()
    {
        var di = new DungeonIntelligence();
        di.PressureScore.Should().Be(1.0f);
    }

    [Fact]
    public void New_IsInGracePeriod()
    {
        var di = new DungeonIntelligence();
        di.InGracePeriod.Should().BeTrue();
    }

    [Fact]
    public void New_Modifiers_AreBaseline()
    {
        var di = new DungeonIntelligence();
        di.SpawnRateModifier.Should().Be(1.0f);
        di.AggressionModifier.Should().Be(1.0f);
        di.EliteFrequencyShift.Should().Be(0f);
        di.LootQualityBonus.Should().Be((0f, 0f));
    }

    // -- Grace period --

    [Fact]
    public void GracePeriod_LastsUnder60Seconds()
    {
        var di = new DungeonIntelligence();
        di.Update(30f);
        di.InGracePeriod.Should().BeTrue();
    }

    [Fact]
    public void GracePeriod_EndsAt60Seconds()
    {
        var di = new DungeonIntelligence();
        di.Update(60f);
        di.InGracePeriod.Should().BeFalse();
    }

    [Fact]
    public void GracePeriod_FreezesModifiers_EvenWithExtremeInputs()
    {
        // Flood the director with kills to push pressure up, but stay in grace period.
        var di = new DungeonIntelligence();
        for (int i = 0; i < 1000; i++) di.RecordKill();
        di.Update(30f);

        // All modifiers must remain at baseline during grace.
        di.SpawnRateModifier.Should().Be(1.0f);
        di.AggressionModifier.Should().Be(1.0f);
        di.EliteFrequencyShift.Should().Be(0f);
        di.LootQualityBonus.Should().Be((0f, 0f));
    }

    // -- Pressure bounds --

    [Fact]
    public void PressureScore_ClampsBelow18()
    {
        // Extreme over-performance: many kills, high damage-dealt, zero damage-taken,
        // fast floor pace. Pressure should saturate at MaxPressure = 1.8.
        var di = new DungeonIntelligence();
        di.SetCurrentFloor(1);
        for (int i = 0; i < 10000; i++) di.RecordKill();
        for (int i = 0; i < 100; i++) di.RecordFloorCleared();
        di.RecordDamageDealt(100000f);
        di.RecordDamageTaken(1f);
        TickPastGraceAndRecalc(di);
        di.PressureScore.Should().BeLessThanOrEqualTo(1.8f);
    }

    [Fact]
    public void PressureScore_ClampsAbove05()
    {
        // Extreme under-performance: no kills, no floors, many deaths, damage-taken
        // dominates. Pressure should floor at MinPressure = 0.5.
        var di = new DungeonIntelligence();
        di.SetCurrentFloor(10);
        for (int i = 0; i < 20; i++) di.RecordDeath();
        di.RecordDamageTaken(10000f);
        TickPastGraceAndRecalc(di);
        di.PressureScore.Should().BeGreaterThanOrEqualTo(0.5f);
    }

    // -- Modifier bounds --

    [Fact]
    public void SpawnRateModifier_ClampsToRange()
    {
        // SpawnRateModifier is bounded to [0.80, 1.20]. Regardless of extreme pressure,
        // it should stay in that range once grace ends.
        var di = new DungeonIntelligence();
        for (int i = 0; i < 5000; i++) di.RecordKill();
        TickPastGraceAndRecalc(di);
        di.SpawnRateModifier.Should().BeInRange(0.80f, 1.20f);
    }

    [Fact]
    public void AggressionModifier_ClampsToRange()
    {
        var di = new DungeonIntelligence();
        for (int i = 0; i < 5000; i++) di.RecordKill();
        TickPastGraceAndRecalc(di);
        di.AggressionModifier.Should().BeInRange(0.85f, 1.15f);
    }

    [Fact]
    public void EliteFrequencyShift_ClampsToRange()
    {
        var di = new DungeonIntelligence();
        for (int i = 0; i < 5000; i++) di.RecordKill();
        TickPastGraceAndRecalc(di);
        di.EliteFrequencyShift.Should().BeInRange(0f, 0.04f);
    }

    // -- Loot quality bonus --

    [Fact]
    public void LootQualityBonus_ZeroWhenPressureAbove085()
    {
        // Performing OK (pressure ≥ 0.85) → no loot pity bonus.
        var di = new DungeonIntelligence();
        di.SetCurrentFloor(1);
        for (int i = 0; i < 100; i++) di.RecordKill();
        TickPastGraceAndRecalc(di);
        di.PressureScore.Should().BeGreaterThan(0.85f);
        di.LootQualityBonus.Should().Be((0f, 0f));
    }

    [Fact]
    public void LootQualityBonus_PositiveWhenPressureBelow085()
    {
        // Struggling player (pressure < 0.85) gets a small Superior + Elite roll bonus.
        var di = new DungeonIntelligence();
        di.SetCurrentFloor(10);
        for (int i = 0; i < 10; i++) di.RecordDeath();
        di.RecordDamageTaken(5000f);
        TickPastGraceAndRecalc(di);
        var (sup, elite) = di.LootQualityBonus;
        sup.Should().BeGreaterThan(0f);
        elite.Should().BeGreaterThan(0f);
        elite.Should().BeLessThan(sup); // Elite bonus is always smaller than Superior (0.43× scaling).
    }

    // -- Death penalty --

    [Fact]
    public void RecordDeath_PushesPressureDown()
    {
        var di = new DungeonIntelligence();
        di.SetCurrentFloor(5);
        // Match kills so only the deaths differentiate.
        for (int i = 0; i < 30; i++) di.RecordKill();
        di.RecordDamageDealt(1000f);
        di.RecordDamageTaken(500f);
        TickPastGraceAndRecalc(di);
        float pressureNoDeath = di.PressureScore;

        var di2 = new DungeonIntelligence();
        di2.SetCurrentFloor(5);
        for (int i = 0; i < 30; i++) di2.RecordKill();
        di2.RecordDamageDealt(1000f);
        di2.RecordDamageTaken(500f);
        for (int i = 0; i < 5; i++) di2.RecordDeath();
        TickPastGraceAndRecalc(di2);
        float pressureWithDeath = di2.PressureScore;

        pressureWithDeath.Should().BeLessThan(pressureNoDeath, "deaths apply a negative pressure weight");
    }

    // -- Reset --

    [Fact]
    public void Reset_RestoresInitialState()
    {
        var di = new DungeonIntelligence();
        di.SetCurrentFloor(50);
        for (int i = 0; i < 500; i++) di.RecordKill();
        for (int i = 0; i < 10; i++) di.RecordFloorCleared();
        di.RecordDamageDealt(5000f);
        di.RecordDamageTaken(100f);
        di.RecordDeath();
        TickPastGraceAndRecalc(di);

        di.Reset();

        di.PressureScore.Should().Be(1.0f);
        di.InGracePeriod.Should().BeTrue();
        di.SpawnRateModifier.Should().Be(1.0f);
        di.AggressionModifier.Should().Be(1.0f);
        di.EliteFrequencyShift.Should().Be(0f);
        di.LootQualityBonus.Should().Be((0f, 0f));
    }

    [Fact]
    public void Reset_AfterGrace_RestartsGracePeriod()
    {
        var di = new DungeonIntelligence();
        di.Update(120f);
        di.InGracePeriod.Should().BeFalse();
        di.Reset();
        di.InGracePeriod.Should().BeTrue();
    }

    // -- Update tick behavior --

    [Fact]
    public void Update_AccumulatesSessionSeconds()
    {
        var di = new DungeonIntelligence();
        di.InGracePeriod.Should().BeTrue();
        // Three 20s ticks = 60s → grace ends exactly at the threshold (< check in code).
        di.Update(20f);
        di.Update(20f);
        di.Update(20f);
        di.InGracePeriod.Should().BeFalse();
    }

    [Fact]
    public void Update_SmallDeltas_EventuallyLeaveGrace()
    {
        var di = new DungeonIntelligence();
        // Simulate a realistic frame-rate tick pattern: ~60 fps for 61s.
        for (int i = 0; i < 3700; i++) di.Update(1f / 60f);
        di.InGracePeriod.Should().BeFalse();
    }

    // -- Helper: tick past grace + at least one recalc so modifiers take effect --

    private static void TickPastGraceAndRecalc(DungeonIntelligence di)
    {
        // 60s grace + 5s recalc interval. Small sub-window tick avoids zeroing the
        // damage-window decay in a single frame (decayFactor clamps to 0 when delta
        // exceeds the rolling window).
        for (int i = 0; i < 14; i++) di.Update(5f);
    }
}
