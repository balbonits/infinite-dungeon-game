using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

public class DungeonPactsTests
{
    // -- Rank access --

    [Fact]
    public void NewPacts_AllRanksAreZero()
    {
        var pacts = new DungeonPacts();
        for (int i = 0; i < DungeonPacts.PactCount; i++)
            pacts.GetRank(i).Should().Be(0);
    }

    [Fact]
    public void SetRank_GetRank_RoundTrips()
    {
        var pacts = new DungeonPacts();
        pacts.SetRank(0, 3);
        pacts.GetRank(0).Should().Be(3);
    }

    [Fact]
    public void SetRank_ClampsAtMaxRank()
    {
        var pacts = new DungeonPacts();
        pacts.SetRank(0, 999); // Pact 0 max is 5
        pacts.GetRank(0).Should().Be(5);
    }

    [Fact]
    public void SetRank_ClampsAtZero()
    {
        var pacts = new DungeonPacts();
        pacts.SetRank(0, -5);
        pacts.GetRank(0).Should().Be(0);
    }

    [Fact]
    public void GetRank_ClampsIndexOutOfRange()
    {
        var pacts = new DungeonPacts();
        pacts.SetRank(0, 3);
        pacts.GetRank(-1).Should().Be(3);  // clamped to index 0
        pacts.GetRank(99).Should().Be(pacts.GetRank(9)); // clamped to last
    }

    // -- Heat calculation --

    [Fact]
    public void TotalHeat_AllZero_IsZero()
    {
        new DungeonPacts().TotalHeat.Should().Be(0);
    }

    [Fact]
    public void TotalHeat_SinglePact_MatchesTable()
    {
        var pacts = new DungeonPacts();
        pacts.SetRank(0, 1); // Pact 0 rank 1 = 3 heat
        pacts.TotalHeat.Should().Be(3);

        pacts.SetRank(0, 3); // rank 3 = 7 heat
        pacts.TotalHeat.Should().Be(7);
    }

    [Fact]
    public void TotalHeat_MultiplePacts_SumsCorrectly()
    {
        var pacts = new DungeonPacts();
        pacts.SetRank(0, 1); // 3
        pacts.SetRank(1, 1); // 3
        pacts.SetRank(2, 1); // 4
        pacts.TotalHeat.Should().Be(10);
    }

    // -- Reward scaling --

    [Fact]
    public void XpBonus_Is1Point5PercentPerHeat()
    {
        var pacts = new DungeonPacts();
        pacts.SetRank(0, 1); // 3 heat
        pacts.XpBonus.Should().BeApproximately(3 * 0.015f, 0.001f);
    }

    [Fact]
    public void GoldBonus_Is1PercentPerHeat()
    {
        var pacts = new DungeonPacts();
        pacts.SetRank(0, 1);
        pacts.GoldBonus.Should().BeApproximately(3 * 0.01f, 0.001f);
    }

    [Fact]
    public void QualityShift_Per30Heat()
    {
        var pacts = new DungeonPacts();
        pacts.TotalHeat.Should().Be(0);
        pacts.QualityShift.Should().Be(0);

        // Max all pacts to get high heat
        for (int i = 0; i < DungeonPacts.PactCount; i++)
            pacts.SetRank(i, pacts.GetMaxRank(i));
        int heat = pacts.TotalHeat;
        pacts.QualityShift.Should().Be(heat / 30);
    }

    // -- Pact-specific effects --

    [Fact]
    public void HordeBudgetMultiplier_ScalesWith20PercentPerRank()
    {
        var pacts = new DungeonPacts();
        pacts.HordeBudgetMultiplier.Should().Be(1.0f);
        pacts.SetRank(0, 3);
        pacts.HordeBudgetMultiplier.Should().BeApproximately(1.6f, 0.001f);
    }

    [Fact]
    public void EnemyHpMultiplier_LookupTable()
    {
        var pacts = new DungeonPacts();
        pacts.EnemyHpMultiplier.Should().Be(1.0f);
        pacts.SetRank(1, 4); // max rank for pact 1
        pacts.EnemyHpMultiplier.Should().Be(2.20f);
    }

    [Fact]
    public void EnemyDamageMultiplier_LookupTable()
    {
        var pacts = new DungeonPacts();
        pacts.SetRank(2, 2);
        pacts.EnemyDamageMultiplier.Should().Be(1.40f);
    }

    [Fact]
    public void EnemySpeedAndCooldown_LinkedToPact3()
    {
        var pacts = new DungeonPacts();
        pacts.SetRank(3, 2);
        pacts.EnemySpeedMultiplier.Should().Be(1.30f);
        pacts.EnemyCooldownMultiplier.Should().Be(0.80f);
    }

    [Fact]
    public void RarityThresholds_Rank0_DefaultDistribution()
    {
        var pacts = new DungeonPacts();
        var (normal, empowered, named) = pacts.GetRarityThresholds();
        normal.Should().BeApproximately(0.78f, 0.001f);
        empowered.Should().BeApproximately(0.20f, 0.001f);
        named.Should().BeApproximately(0.02f, 0.001f);
    }

    [Fact]
    public void SafeZone_Rank3_DisablesSafeZones()
    {
        var pacts = new DungeonPacts();
        pacts.SafeZonesDisabled.Should().BeFalse();
        pacts.SetRank(8, 3);
        pacts.SafeZonesDisabled.Should().BeTrue();
        pacts.SafeZoneRadiusMultiplier.Should().Be(0.0f);
    }

    [Fact]
    public void BossModifiers_MaxRank()
    {
        var pacts = new DungeonPacts();
        pacts.SetRank(9, 3);
        pacts.BossHpMultiplier.Should().Be(2.50f);
        pacts.BossDamageMultiplier.Should().Be(1.50f);
        pacts.BossSpeedMultiplier.Should().Be(1.20f);
        pacts.BossExtraModifiers.Should().Be(3);
    }

    // -- Serialization --

    [Fact]
    public void ExportImportRanks_RoundTrips()
    {
        var pacts = new DungeonPacts();
        pacts.SetRank(0, 3);
        pacts.SetRank(5, 2);
        var exported = pacts.ExportRanks();

        var restored = new DungeonPacts();
        restored.ImportRanks(exported);
        restored.GetRank(0).Should().Be(3);
        restored.GetRank(5).Should().Be(2);
    }

    [Fact]
    public void ImportRanks_Null_SetsAllToZero()
    {
        var pacts = new DungeonPacts();
        pacts.SetRank(0, 3);
        pacts.ImportRanks(null);
        pacts.GetRank(0).Should().Be(0);
    }

    [Fact]
    public void ImportRanks_ClampsOutOfRange()
    {
        var pacts = new DungeonPacts();
        pacts.ImportRanks(new[] { 99, 99, 99, 99, 99, 99, 99, 99, 99, 99 });
        for (int i = 0; i < DungeonPacts.PactCount; i++)
            pacts.GetRank(i).Should().Be(pacts.GetMaxRank(i));
    }

    [Fact]
    public void Reset_ClearsAllRanks()
    {
        var pacts = new DungeonPacts();
        for (int i = 0; i < DungeonPacts.PactCount; i++)
            pacts.SetRank(i, pacts.GetMaxRank(i));
        pacts.Reset();
        pacts.TotalHeat.Should().Be(0);
    }
}
