using System;
using Xunit;

namespace DungeonGame.Tests;

public class CritSystemTests
{
    // Base crit per weapon
    [Theory]
    [InlineData(WeaponType.Dagger, 8f)]   [InlineData(WeaponType.Sword, 6f)]
    [InlineData(WeaponType.Axe, 4f)]      [InlineData(WeaponType.Club, 3f)]
    [InlineData(WeaponType.Rifle, 9f)]     [InlineData(WeaponType.Staff, 4f)]
    [InlineData(WeaponType.Wand, 7f)]      [InlineData(WeaponType.Unarmed, 3f)]
    [InlineData(WeaponType.Crossbow, 8f)]  [InlineData(WeaponType.ThrowingKnife, 7f)]
    public void BaseCrit_CorrectPerWeapon(WeaponType type, float expected)
        => Assert.Equal(expected, CritSystem.GetBaseCritChance(type));

    [Fact] public void AllWeaponTypes_HaveBaseCrit_3to9()
    {
        foreach (WeaponType wt in Enum.GetValues<WeaponType>())
        {
            float crit = CritSystem.GetBaseCritChance(wt);
            Assert.True(crit >= 3f && crit <= 9f, $"{wt} has crit {crit}");
        }
    }

    // Crit chance calculation
    [Fact] public void CritChance_NoBonuses_EqualsBase()
        => Assert.Equal(8f, CritSystem.CalculateCritChance(WeaponType.Dagger));

    [Fact] public void CritChance_WithIncreasedPercent()
    {
        // Dagger 8% * (1 + 100/100) = 16%
        Assert.Equal(16f, CritSystem.CalculateCritChance(WeaponType.Dagger, increasedCritPercent: 100f));
    }

    [Fact] public void CritChance_WithFlatBonus()
        => Assert.Equal(13f, CritSystem.CalculateCritChance(WeaponType.Dagger, flatCritBonus: 5f));

    [Fact] public void CritChance_WithBoth()
    {
        // 8% * (1 + 100/100) + 5% = 21%
        Assert.Equal(21f, CritSystem.CalculateCritChance(WeaponType.Dagger, 100f, 5f));
    }

    [Fact] public void CritChance_CappedAt75()
        => Assert.Equal(75f, CritSystem.CalculateCritChance(WeaponType.Rifle, 5000f, 500f));

    [Fact] public void CritChance_NeverNegative()
        => Assert.True(CritSystem.CalculateCritChance(WeaponType.Unarmed, -200f) >= 0f);

    // Crit multiplier
    [Fact] public void CritMultiplier_Default150()
        => Assert.Equal(150f, CritSystem.CalculateCritMultiplier());

    [Fact] public void CritMultiplier_WithBonus()
        => Assert.Equal(200f, CritSystem.CalculateCritMultiplier(50f));

    [Fact] public void CritMultiplier_NoCap()
        => Assert.Equal(650f, CritSystem.CalculateCritMultiplier(500f));

    // Rolling
    [Fact] public void RollCrit_CritDamage_CorrectMultiplier()
    {
        // Force a crit by using a seeded RNG and finding a crit seed
        for (int seed = 0; seed < 1000; seed++)
        {
            var rng = new Random(seed);
            var result = CritSystem.RollCrit(100, WeaponType.Rifle, rng); // 9% base
            if (result.IsCrit)
            {
                Assert.Equal(150, result.FinalDamage); // 100 * 150 / 100
                return;
            }
        }
        Assert.Fail("No crit found in 1000 seeds for Rifle");
    }

    [Fact] public void RollCrit_NonCrit_BaseDamage()
    {
        for (int seed = 0; seed < 1000; seed++)
        {
            var rng = new Random(seed);
            var result = CritSystem.RollCrit(100, WeaponType.Club, rng); // 3% base
            if (!result.IsCrit)
            {
                Assert.Equal(100, result.FinalDamage);
                return;
            }
        }
        Assert.Fail("All 1000 seeds were crits for Club (3%)");
    }

    [Fact] public void RollCrit_Statistical_DaggerVsClub()
    {
        int daggerCrits = 0, clubCrits = 0;
        int trials = 50000;
        var rng = new Random(42);
        for (int i = 0; i < trials; i++)
        {
            if (CritSystem.RollCrit(100, WeaponType.Dagger, rng).IsCrit) daggerCrits++;
            if (CritSystem.RollCrit(100, WeaponType.Club, rng).IsCrit) clubCrits++;
        }
        float daggerRate = daggerCrits / (float)trials * 100f;
        float clubRate = clubCrits / (float)trials * 100f;
        Assert.InRange(daggerRate, 6f, 10f); // Expected ~8%
        Assert.InRange(clubRate, 1.5f, 4.5f); // Expected ~3%
        Assert.True(daggerRate > clubRate, "Dagger should crit more than Club");
    }

    [Fact] public void RollCrit_WithBonusMulti()
    {
        for (int seed = 0; seed < 1000; seed++)
        {
            var rng = new Random(seed);
            var result = CritSystem.RollCrit(100, WeaponType.Dagger, rng, bonusCritMulti: 50f);
            if (result.IsCrit)
            {
                Assert.Equal(200, result.FinalDamage); // 100 * 200 / 100
                Assert.Equal(200f, result.CritMultiplier);
                return;
            }
        }
    }

    [Fact] public void ItemData_HasWeaponType()
    {
        var item = new ItemData();
        Assert.Equal(WeaponType.Unarmed, item.WeaponType);
    }
}
