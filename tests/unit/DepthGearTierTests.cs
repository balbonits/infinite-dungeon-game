using System;
using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

public class DepthGearTierTests
{
    // -- GetMinFloor --

    [Theory]
    [InlineData(BaseQuality.Normal, 1)]
    [InlineData(BaseQuality.Superior, 10)]
    [InlineData(BaseQuality.Elite, 25)]
    [InlineData(BaseQuality.Masterwork, 50)]
    [InlineData(BaseQuality.Mythic, 100)]
    [InlineData(BaseQuality.Transcendent, 150)]
    public void GetMinFloor_AllTiers(BaseQuality quality, int expected)
    {
        DepthGearTiers.GetMinFloor(quality).Should().Be(expected);
    }

    // -- GetStatBonusRange --

    [Fact]
    public void GetStatBonusRange_Normal_IsZero()
    {
        var (min, max) = DepthGearTiers.GetStatBonusRange(BaseQuality.Normal);
        min.Should().Be(0f);
        max.Should().Be(0f);
    }

    [Fact]
    public void GetStatBonusRange_IncreasesWithQuality()
    {
        var superior = DepthGearTiers.GetStatBonusRange(BaseQuality.Superior);
        var elite = DepthGearTiers.GetStatBonusRange(BaseQuality.Elite);
        var masterwork = DepthGearTiers.GetStatBonusRange(BaseQuality.Masterwork);

        elite.max.Should().BeGreaterThan(superior.max);
        masterwork.max.Should().BeGreaterThan(elite.max);
    }

    [Fact]
    public void GetStatBonusRange_Transcendent_MaxIs1Point20()
    {
        var (_, max) = DepthGearTiers.GetStatBonusRange(BaseQuality.Transcendent);
        max.Should().BeApproximately(1.20f, 0.001f);
    }

    // -- GetMaxAffixes --

    [Theory]
    [InlineData(BaseQuality.Normal, 6)]
    [InlineData(BaseQuality.Superior, 6)]
    [InlineData(BaseQuality.Elite, 6)]
    [InlineData(BaseQuality.Masterwork, 8)]
    [InlineData(BaseQuality.Mythic, 10)]
    [InlineData(BaseQuality.Transcendent, 12)]
    public void GetMaxAffixes_AllTiers(BaseQuality quality, int expected)
    {
        DepthGearTiers.GetMaxAffixes(quality).Should().Be(expected);
    }

    // -- GetAffixSlots --

    [Fact]
    public void GetAffixSlots_Normal_Is3And3()
    {
        var (prefix, suffix) = DepthGearTiers.GetAffixSlots(BaseQuality.Normal);
        prefix.Should().Be(3);
        suffix.Should().Be(3);
    }

    [Fact]
    public void GetAffixSlots_Transcendent_Is6And6()
    {
        var (prefix, suffix) = DepthGearTiers.GetAffixSlots(BaseQuality.Transcendent);
        prefix.Should().Be(6);
        suffix.Should().Be(6);
    }

    // -- GetCraftCostMultiplier --

    [Fact]
    public void GetCraftCostMultiplier_Normal_Is1()
    {
        DepthGearTiers.GetCraftCostMultiplier(BaseQuality.Normal).Should().Be(1.0f);
    }

    [Fact]
    public void GetCraftCostMultiplier_IncreasesWithQuality()
    {
        float superior = DepthGearTiers.GetCraftCostMultiplier(BaseQuality.Superior);
        float elite = DepthGearTiers.GetCraftCostMultiplier(BaseQuality.Elite);
        float transcendent = DepthGearTiers.GetCraftCostMultiplier(BaseQuality.Transcendent);

        superior.Should().BeGreaterThan(1.0f);
        elite.Should().BeGreaterThan(superior);
        transcendent.Should().Be(5.0f);
    }

    // -- RollQuality --

    [Fact]
    public void RollQuality_Floor1_AlwaysNormal()
    {
        var rng = new Random(42);
        for (int i = 0; i < 100; i++)
            DepthGearTiers.RollQuality(1, 0, rng).Should().Be(BaseQuality.Normal);
    }

    [Fact]
    public void RollQuality_Floor10_CanBeSuperior()
    {
        var rng = new Random(42);
        bool foundSuperior = false;
        for (int i = 0; i < 1000; i++)
        {
            if (DepthGearTiers.RollQuality(10, 0, rng) == BaseQuality.Superior)
            { foundSuperior = true; break; }
        }
        foundSuperior.Should().BeTrue();
    }

    [Fact]
    public void RollQuality_Floor150_CanBeTranscendent()
    {
        var rng = new Random(42);
        bool foundTranscendent = false;
        for (int i = 0; i < 1000; i++)
        {
            if (DepthGearTiers.RollQuality(150, 0, rng) == BaseQuality.Transcendent)
            { foundTranscendent = true; break; }
        }
        foundTranscendent.Should().BeTrue();
    }

    [Fact]
    public void RollQuality_FloorShift_IncreasesTier()
    {
        // Floor 5 + shift 20 = effective floor 25 → can get Elite
        var rng = new Random(42);
        bool foundElite = false;
        for (int i = 0; i < 1000; i++)
        {
            if (DepthGearTiers.RollQuality(5, 20, rng) >= BaseQuality.Elite)
            { foundElite = true; break; }
        }
        foundElite.Should().BeTrue();
    }

    [Fact]
    public void RollQuality_SeededRng_IsDeterministic()
    {
        var rng1 = new Random(123);
        var rng2 = new Random(123);
        for (int i = 0; i < 50; i++)
        {
            var q1 = DepthGearTiers.RollQuality(100, 0, rng1);
            var q2 = DepthGearTiers.RollQuality(100, 0, rng2);
            q1.Should().Be(q2);
        }
    }

    // -- GetMaxTier (AffixDatabase) --

    [Theory]
    [InlineData(1, 1)]
    [InlineData(9, 1)]
    [InlineData(10, 2)]
    [InlineData(24, 2)]
    [InlineData(25, 3)]
    [InlineData(49, 3)]
    [InlineData(50, 4)]
    [InlineData(74, 4)]
    [InlineData(75, 5)]
    [InlineData(100, 6)]
    public void AffixDatabase_GetMaxTier_ByItemLevel(int itemLevel, int expected)
    {
        AffixDatabase.GetMaxTier(itemLevel).Should().Be(expected);
    }
}
