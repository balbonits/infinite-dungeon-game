using System;
using Xunit;

namespace DungeonGame.Tests;

public class ResistancesTests
{
    [Fact] public void DefaultResistances_AllZero()
    {
        var r = new Resistances();
        Assert.Equal(0, r.Fire); Assert.Equal(0, r.Water); Assert.Equal(0, r.Air);
        Assert.Equal(0, r.Earth); Assert.Equal(0, r.Light); Assert.Equal(0, r.Dark);
    }

    [Fact] public void GetResistance_ReturnsCorrectElement()
    {
        var r = new Resistances { Fire = 30, Water = 20, Air = 10, Earth = 15, Light = 25, Dark = 5 };
        Assert.Equal(30, r.GetResistance(DamageType.Fire));
        Assert.Equal(20, r.GetResistance(DamageType.Water));
        Assert.Equal(10, r.GetResistance(DamageType.Air));
        Assert.Equal(15, r.GetResistance(DamageType.Earth));
        Assert.Equal(25, r.GetResistance(DamageType.Light));
        Assert.Equal(5, r.GetResistance(DamageType.Dark));
    }

    [Fact] public void Physical_ReturnsZeroResistance()
        => Assert.Equal(0, new Resistances { Fire = 50 }.GetResistance(DamageType.Physical));

    [Theory]
    [InlineData(1, 0)]    [InlineData(10, 5)]   [InlineData(50, 25)]
    [InlineData(100, 50)] [InlineData(200, 100)]
    public void FloorPenalty_CorrectValues(int floor, int expectedPenalty)
    {
        var r = new Resistances { Fire = 75 };
        Assert.Equal(75 - expectedPenalty, r.GetEffective(DamageType.Fire, floor));
    }

    [Fact] public void EffectiveResistance_FloorsAtNegative100()
    {
        var r = new Resistances { Fire = 0 };
        Assert.Equal(-100, r.GetEffective(DamageType.Fire, 250)); // penalty=125, 0-125=-125, clamped to -100
    }

    [Fact] public void NegativeFloor_NoPenalty()
    {
        var r = new Resistances { Fire = 50 };
        Assert.Equal(50, r.GetEffective(DamageType.Fire, -5));
    }
}

public class ElementalCombatTests
{
    private EntityData MakeTarget(int defense = 0, int fireRes = 0, int darkRes = 0)
    {
        var e = new EntityData { BaseDefense = defense };
        e.Resistances.Fire = fireRes;
        e.Resistances.Dark = darkRes;
        return e;
    }

    [Fact] public void Physical_UsesDefenseNotResistance()
    {
        var target = MakeTarget(defense: 100, fireRes: 75);
        var result = ElementalCombat.CalculateDamage(100, DamageType.Physical, target, 1);
        // Defense 100 → DR = 100*(100/(100+100)) = 50 → 50% reduction → 50 damage
        Assert.True(result.FinalDamage < 100 && result.FinalDamage > 0);
        Assert.Equal(0, result.EffectiveResistance); // Physical doesn't report resistance
    }

    [Fact] public void Elemental_UsesResistanceNotDefense()
    {
        var target = MakeTarget(defense: 1000, fireRes: 50);
        var result = ElementalCombat.CalculateDamage(100, DamageType.Fire, target, 1);
        Assert.Equal(50, result.FinalDamage); // 50% resistance → 50 damage
    }

    [Theory]
    [InlineData(0, 100)]   // 0% res = full damage
    [InlineData(50, 50)]   // 50% res = half
    [InlineData(75, 25)]   // 75% cap = quarter
    public void ResistanceReducesDamage(int resistance, int expectedDamage)
    {
        var target = MakeTarget(fireRes: resistance);
        var result = ElementalCombat.CalculateDamage(100, DamageType.Fire, target, 1);
        Assert.Equal(expectedDamage, result.FinalDamage);
    }

    [Fact] public void NegativeResistance_IncreasesDamage()
    {
        var target = MakeTarget(fireRes: -50);
        var result = ElementalCombat.CalculateDamage(100, DamageType.Fire, target, 1);
        Assert.Equal(150, result.FinalDamage); // -50% = 1.5x damage
    }

    [Fact] public void ResistanceCappedAt75()
    {
        var target = MakeTarget(fireRes: 95);
        var result = ElementalCombat.CalculateDamage(100, DamageType.Fire, target, 1);
        Assert.Equal(25, result.FinalDamage); // capped at 75% reduction
    }

    [Fact] public void MinimumOneDamage()
    {
        var target = MakeTarget(fireRes: 75);
        var result = ElementalCombat.CalculateDamage(1, DamageType.Fire, target, 1);
        Assert.True(result.FinalDamage >= 1);
    }

    [Fact] public void Crit_AppliesAfterResistance()
    {
        var target = MakeTarget(fireRes: 50);
        var normal = ElementalCombat.CalculateDamage(100, DamageType.Fire, target, 1, isCrit: false);
        var crit = ElementalCombat.CalculateDamage(100, DamageType.Fire, target, 1, isCrit: true);
        Assert.Equal(50, normal.FinalDamage);
        Assert.Equal(75, crit.FinalDamage); // 50 * 1.5 = 75
    }

    [Fact] public void FloorScaling_ErodesResistance()
    {
        var target = MakeTarget(fireRes: 75);
        var f1 = ElementalCombat.CalculateDamage(100, DamageType.Fire, target, 1);
        var f50 = ElementalCombat.CalculateDamage(100, DamageType.Fire, target, 50);
        var f150 = ElementalCombat.CalculateDamage(100, DamageType.Fire, target, 150);
        Assert.Equal(25, f1.FinalDamage);   // 75% res
        Assert.Equal(50, f50.FinalDamage);  // 75-25=50% res
        Assert.Equal(100, f150.FinalDamage); // 75-75=0% res
    }

    [Fact] public void DoubleResistanceAtMinusHundred()
    {
        var target = MakeTarget(fireRes: 0);
        var result = ElementalCombat.CalculateDamage(100, DamageType.Fire, target, 200);
        // Penalty=100, effective = 0-100 = -100 (capped), so 1.0 - (-100/100) = 2.0x
        Assert.Equal(200, result.FinalDamage);
    }

    // Ambient dark damage
    [Theory]
    [InlineData(1, 0)]  [InlineData(50, 0)]  [InlineData(75, 0)]
    [InlineData(76, 2)] [InlineData(100, 50)] [InlineData(150, 150)] [InlineData(200, 250)]
    public void AmbientDarkDPS(int floor, int expected)
        => Assert.Equal(expected, ElementalCombat.GetAmbientDarkDPS(floor));

    [Fact] public void AmbientDark_ReducedByResistance()
    {
        var target = MakeTarget(darkRes: 50);
        int dps = ElementalCombat.GetAmbientDarkDamagePerSecond(100, target);
        // Raw=50, dark res 50, floor penalty=50, effective=50-50=0%, so full 50 DPS
        Assert.Equal(50, dps);
    }

    [Fact] public void EntityData_HasDefaultResistances()
    {
        var e = new EntityData();
        Assert.NotNull(e.Resistances);
        Assert.Equal(0, e.Resistances.Fire);
    }
}
