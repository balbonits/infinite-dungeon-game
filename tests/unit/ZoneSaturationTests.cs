using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

public class ZoneSaturationTests
{
    // -- Default state --

    [Fact]
    public void NewZone_HasZeroSaturation()
    {
        new ZoneSaturation().GetSaturation(1).Should().Be(0f);
    }

    // -- RecordKill --

    [Fact]
    public void RecordKill_Zone1_IncreasesByBaseGain()
    {
        var zs = new ZoneSaturation();
        zs.RecordKill(1);
        // Zone 1: mult = 1.0 + (1-1)*0.05 = 1.0; gain = 0.15 * 1.0 = 0.15
        zs.GetSaturation(1).Should().BeApproximately(0.15f, 0.001f);
    }

    [Fact]
    public void RecordKill_HigherZone_GainsMore()
    {
        var zs = new ZoneSaturation();
        zs.RecordKill(1);
        float zone1 = zs.GetSaturation(1);

        var zs2 = new ZoneSaturation();
        zs2.RecordKill(10);
        float zone10 = zs2.GetSaturation(10);

        zone10.Should().BeGreaterThan(zone1);
    }

    [Fact]
    public void RecordKill_CapsAt100()
    {
        var zs = new ZoneSaturation();
        for (int i = 0; i < 10000; i++)
            zs.RecordKill(1);
        zs.GetSaturation(1).Should().Be(100f);
    }

    [Fact]
    public void RecordKill_MultipleZones_Independent()
    {
        var zs = new ZoneSaturation();
        zs.RecordKill(1);
        zs.RecordKill(5);
        zs.GetSaturation(1).Should().BeGreaterThan(0);
        zs.GetSaturation(5).Should().BeGreaterThan(0);
        zs.GetSaturation(3).Should().Be(0);
    }

    // -- Decay --

    [Fact]
    public void ApplyDecay_FirstCall_SetsTimestampOnly()
    {
        var zs = new ZoneSaturation();
        zs.RecordKill(1);
        float before = zs.GetSaturation(1);
        zs.ApplyDecay(1000.0);
        zs.GetSaturation(1).Should().Be(before); // no decay on first call
    }

    [Fact]
    public void ApplyDecay_ReducesSaturation()
    {
        var zs = new ZoneSaturation();
        for (int i = 0; i < 100; i++) zs.RecordKill(1);
        float before = zs.GetSaturation(1);

        zs.ApplyDecay(1000); // set initial timestamp (must be > 0)
        zs.ApplyDecay(1060); // 1 minute later → 0.25 decay
        zs.GetSaturation(1).Should().BeApproximately(before - 0.25f, 0.01f);
    }

    [Fact]
    public void ApplyDecay_RemovesZeroedZones()
    {
        var zs = new ZoneSaturation();
        zs.RecordKill(1); // small amount (~0.15)
        zs.ApplyDecay(1000);
        zs.ApplyDecay(1600); // 10 minutes → 2.5 decay, more than 0.15
        zs.GetSaturation(1).Should().Be(0f);
    }

    [Fact]
    public void ApplyDecayExcluding_SkipsCurrentZone()
    {
        var zs = new ZoneSaturation();
        for (int i = 0; i < 50; i++) { zs.RecordKill(1); zs.RecordKill(2); }
        float zone1Before = zs.GetSaturation(1);
        float zone2Before = zs.GetSaturation(2);

        zs.ApplyDecayExcluding(1, 1000); // set timestamp (must be > 0)
        zs.ApplyDecayExcluding(1, 1600); // 10 min

        zs.GetSaturation(1).Should().Be(zone1Before); // excluded
        zs.GetSaturation(2).Should().BeLessThan(zone2Before); // decayed
    }

    // -- Stat multipliers --

    [Fact]
    public void GetHpMultiplier_ZeroSaturation_Returns1()
    {
        new ZoneSaturation().GetHpMultiplier(1).Should().Be(1.0f);
    }

    [Fact]
    public void GetHpMultiplier_MaxSaturation_Returns1Point5()
    {
        var zs = new ZoneSaturation();
        for (int i = 0; i < 10000; i++) zs.RecordKill(1);
        zs.GetHpMultiplier(1).Should().BeApproximately(1.50f, 0.01f);
    }

    [Fact]
    public void GetDamageMultiplier_MaxSaturation_Returns1Point35()
    {
        var zs = new ZoneSaturation();
        for (int i = 0; i < 10000; i++) zs.RecordKill(1);
        zs.GetDamageMultiplier(1).Should().BeApproximately(1.35f, 0.01f);
    }

    [Fact]
    public void GetSpeedMultiplier_MaxSaturation_Returns1Point15()
    {
        var zs = new ZoneSaturation();
        for (int i = 0; i < 10000; i++) zs.RecordKill(1);
        zs.GetSpeedMultiplier(1).Should().BeApproximately(1.15f, 0.01f);
    }

    // -- Reward bonuses --

    [Fact]
    public void GetXpBonus_MaxSaturation_Returns0Point30()
    {
        var zs = new ZoneSaturation();
        for (int i = 0; i < 10000; i++) zs.RecordKill(1);
        zs.GetXpBonus(1).Should().BeApproximately(0.30f, 0.01f);
    }

    [Fact]
    public void GetMaterialDropMultiplier_MaxSaturation_Returns1Point40()
    {
        var zs = new ZoneSaturation();
        for (int i = 0; i < 10000; i++) zs.RecordKill(1);
        zs.GetMaterialDropMultiplier(1).Should().BeApproximately(1.40f, 0.01f);
    }

    [Fact]
    public void GetQualityShiftFloors_MaxSaturation_Returns20()
    {
        var zs = new ZoneSaturation();
        for (int i = 0; i < 10000; i++) zs.RecordKill(1);
        zs.GetQualityShiftFloors(1).Should().Be(20);
    }

    [Fact]
    public void GetEquipDropMultiplier_MaxSaturation_Returns1Point20()
    {
        var zs = new ZoneSaturation();
        for (int i = 0; i < 10000; i++) zs.RecordKill(1);
        zs.GetEquipDropMultiplier(1).Should().BeApproximately(1.20f, 0.01f);
    }

    // -- Serialization --

    [Fact]
    public void ExportImportState_RoundTrips()
    {
        var zs = new ZoneSaturation();
        for (int i = 0; i < 20; i++) zs.RecordKill(3);
        float expected = zs.GetSaturation(3);

        var exported = zs.ExportState();
        var restored = new ZoneSaturation();
        restored.ImportState(exported, 500.0);
        restored.GetSaturation(3).Should().BeApproximately(expected, 0.001f);
        restored.LastDecayTimestamp.Should().Be(500.0);
    }

    [Fact]
    public void ImportState_Null_ClearsAll()
    {
        var zs = new ZoneSaturation();
        zs.RecordKill(1);
        zs.ImportState(null, 0);
        zs.GetSaturation(1).Should().Be(0f);
    }

    [Fact]
    public void Reset_ClearsEverything()
    {
        var zs = new ZoneSaturation();
        zs.RecordKill(1);
        zs.ApplyDecay(100);
        zs.Reset();
        zs.GetSaturation(1).Should().Be(0f);
        zs.LastDecayTimestamp.Should().Be(0);
    }
}
