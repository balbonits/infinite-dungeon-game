using System;
using Xunit;

namespace DungeonGame.Tests;

public class StatSystemTests
{
    private EntityData MakeEntity(int level = 1, int str = 5, int dex = 5, int intStat = 5, int vit = 5,
        int baseDamage = 12, int baseDefense = 0)
    {
        return new EntityData
        {
            Id = "test",
            Name = "TestEntity",
            Type = EntityType.Player,
            Level = level,
            HP = 100,
            MaxHP = 100,
            MP = 50,
            MaxMP = 50,
            STR = str,
            DEX = dex,
            INT = intStat,
            VIT = vit,
            BaseDamage = baseDamage,
            BaseDefense = baseDefense,
            AttackSpeed = 0.42f,
            MoveSpeed = 190f,
        };
    }

    // ═══════════════════════════════════════════════════════════════
    //  GetEffective — diminishing returns: rawStat * (100 / (rawStat + 100))
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void GetEffective_Zero_ReturnsZero()
    {
        Assert.Equal(0f, StatSystem.GetEffective(0));
    }

    [Fact]
    public void GetEffective_100_Returns50()
    {
        // 100 * (100 / 200) = 50
        Assert.Equal(50f, StatSystem.GetEffective(100));
    }

    [Fact]
    public void GetEffective_200_Returns66_67()
    {
        // 200 * (100 / 300) = 66.6667
        float expected = 200f * (100f / 300f);
        Assert.Equal(expected, StatSystem.GetEffective(200), 3);
    }

    [Fact]
    public void GetEffective_1000_Returns90_9()
    {
        // 1000 * (100 / 1100) = 90.909...
        float expected = 1000f * (100f / 1100f);
        Assert.Equal(expected, StatSystem.GetEffective(1000), 2);
    }

    [Fact]
    public void GetEffective_DiminishingReturns_AlwaysLessThan100()
    {
        // Even at absurd values, effective stat < 100
        float high = StatSystem.GetEffective(999999);
        Assert.True(high < 100f);
        Assert.True(high > 99f); // but close
    }

    [Fact]
    public void GetEffective_MonotonicallyIncreasing()
    {
        float prev = 0f;
        for (int i = 1; i <= 500; i += 10)
        {
            float current = StatSystem.GetEffective(i);
            Assert.True(current > prev, $"GetEffective({i}) = {current} should be > {prev}");
            prev = current;
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(500)]
    public void GetEffective_MatchesFormula_Exactly(int raw)
    {
        float expected = raw * (100f / (raw + 100f));
        Assert.Equal(expected, StatSystem.GetEffective(raw));
    }

    // ═══════════════════════════════════════════════════════════════
    //  GetMeleeDamage — BaseDamage + (GetEffective(STR) * 0.5) + TotalDamage
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void GetMeleeDamage_BaseCase()
    {
        var e = MakeEntity(str: 5, baseDamage: 12);
        // BaseDamage=12, STR contribution = (int)(GetEffective(5) * 0.5f),
        // TotalDamage = BaseDamage (no equipment) = 12
        float effSTR = StatSystem.GetEffective(5);
        int expected = 12 + (int)(effSTR * 0.5f) + 12;
        Assert.Equal(expected, StatSystem.GetMeleeDamage(e));
    }

    [Fact]
    public void GetMeleeDamage_WithWeapon_IncludesEquipment()
    {
        var e = MakeEntity(str: 5, baseDamage: 12);
        var weapon = new ItemData { Name = "Sword", Damage = 10, Slot = EquipSlot.MainHand };
        e.Equipment[EquipSlot.MainHand] = weapon;
        e.InvalidateStats();

        // TotalDamage = BaseDamage(12) + weapon.Damage(10) = 22
        float effSTR = StatSystem.GetEffective(5);
        int expected = 12 + (int)(effSTR * 0.5f) + 22;
        Assert.Equal(expected, StatSystem.GetMeleeDamage(e));
    }

    [Fact]
    public void GetMeleeDamage_ZeroSTR()
    {
        var e = MakeEntity(str: 0, baseDamage: 10);
        // STR contribution: (int)(0 * 0.5f) = 0
        // TotalDamage = 10
        Assert.Equal(10 + 0 + 10, StatSystem.GetMeleeDamage(e));
    }

    // ═══════════════════════════════════════════════════════════════
    //  GetDefenseReduction — GetEffective(TotalDefense) / 100
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void GetDefenseReduction_ZeroDefense_ReturnsZero()
    {
        var e = MakeEntity(baseDefense: 0);
        Assert.Equal(0f, StatSystem.GetDefenseReduction(e));
    }

    [Fact]
    public void GetDefenseReduction_HighDefense_ApproachesBut_NeverReaches1()
    {
        var e = MakeEntity(baseDefense: 100000);
        float reduction = StatSystem.GetDefenseReduction(e);

        Assert.True(reduction < 1.0f);
        Assert.True(reduction > 0.9f);
    }

    [Fact]
    public void GetDefenseReduction_ModerateDefense()
    {
        var e = MakeEntity(baseDefense: 100);
        // TotalDefense = 100 (no equipment defense)
        // GetEffective(100) = 50, / 100 = 0.5
        Assert.Equal(0.5f, StatSystem.GetDefenseReduction(e));
    }

    [Fact]
    public void GetDefenseReduction_IncludesEquipmentDefense()
    {
        var e = MakeEntity(baseDefense: 0);
        var armor = new ItemData { Name = "Plate", Defense = 100, Slot = EquipSlot.Body };
        e.Equipment[EquipSlot.Body] = armor;
        e.InvalidateStats();

        // TotalDefense = 0 + 100 = 100
        float expected = StatSystem.GetEffective(100) / 100f;
        Assert.Equal(expected, StatSystem.GetDefenseReduction(e));
    }

    // ═══════════════════════════════════════════════════════════════
    //  GetMaxHP — 100 + Level*8 + VIT*3 + equipment HP bonuses
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void GetMaxHP_Level1_VIT5_NoEquipment()
    {
        var e = MakeEntity(level: 1, vit: 5);
        // 100 + 1*8 + 5*3 = 100 + 8 + 15 = 123
        Assert.Equal(123, StatSystem.GetMaxHP(e));
    }

    [Fact]
    public void GetMaxHP_Level10_VIT20()
    {
        var e = MakeEntity(level: 10, vit: 20);
        // 100 + 10*8 + 20*3 = 100 + 80 + 60 = 240
        Assert.Equal(240, StatSystem.GetMaxHP(e));
    }

    [Fact]
    public void GetMaxHP_IncludesEquipmentHPBonus()
    {
        var e = MakeEntity(level: 1, vit: 0);
        var ring = new ItemData { Name = "Ring of Life", HPBonus = 50, Slot = EquipSlot.Ring };
        e.Equipment[EquipSlot.Ring] = ring;

        // 100 + 1*8 + 0*3 + 50 = 158
        Assert.Equal(158, StatSystem.GetMaxHP(e));
    }

    [Fact]
    public void GetMaxHP_MultipleEquipmentBonuses()
    {
        var e = MakeEntity(level: 1, vit: 0);
        e.Equipment[EquipSlot.Body] = new ItemData { Name = "Armor", HPBonus = 20, Slot = EquipSlot.Body };
        e.Equipment[EquipSlot.Ring] = new ItemData { Name = "Ring", HPBonus = 30, Slot = EquipSlot.Ring };

        // 100 + 8 + 0 + 20 + 30 = 158
        Assert.Equal(158, StatSystem.GetMaxHP(e));
    }

    // ═══════════════════════════════════════════════════════════════
    //  GetMaxMP — 50 + INT*3 + equipment MP bonuses
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void GetMaxMP_INT5_NoEquipment()
    {
        var e = MakeEntity(intStat: 5);
        // 50 + 5*3 = 65
        Assert.Equal(65, StatSystem.GetMaxMP(e));
    }

    [Fact]
    public void GetMaxMP_INT0()
    {
        var e = MakeEntity(intStat: 0);
        Assert.Equal(50, StatSystem.GetMaxMP(e));
    }

    [Fact]
    public void GetMaxMP_IncludesEquipmentMPBonus()
    {
        var e = MakeEntity(intStat: 0);
        e.Equipment[EquipSlot.Ring] = new ItemData { Name = "Mana Ring", MPBonus = 25, Slot = EquipSlot.Ring };

        // 50 + 0 + 25 = 75
        Assert.Equal(75, StatSystem.GetMaxMP(e));
    }

    // ═══════════════════════════════════════════════════════════════
    //  RecalculateDerived
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void RecalculateDerived_UpdatesMaxValues()
    {
        var e = MakeEntity(level: 1, vit: 5, intStat: 5);
        e.MaxHP = 999;
        e.MaxMP = 999;

        StatSystem.RecalculateDerived(e);

        Assert.Equal(StatSystem.GetMaxHP(e), e.MaxHP);
        Assert.Equal(StatSystem.GetMaxMP(e), e.MaxMP);
    }

    [Fact]
    public void RecalculateDerived_ClampsHP_WhenExceedsNewMax()
    {
        var e = MakeEntity(level: 1, vit: 0);
        e.HP = 200;
        e.MaxHP = 200;

        // After recalc: MaxHP = 100 + 8 + 0 = 108, so HP should be clamped
        StatSystem.RecalculateDerived(e);

        Assert.Equal(108, e.MaxHP);
        Assert.Equal(108, e.HP);
    }

    [Fact]
    public void RecalculateDerived_ClampsMP_WhenExceedsNewMax()
    {
        var e = MakeEntity(level: 1, intStat: 0);
        e.MP = 200;
        e.MaxMP = 200;

        // After recalc: MaxMP = 50 + 0 = 50, so MP should be clamped
        StatSystem.RecalculateDerived(e);

        Assert.Equal(50, e.MaxMP);
        Assert.Equal(50, e.MP);
    }

    [Fact]
    public void RecalculateDerived_DoesNotClamp_WhenBelowMax()
    {
        var e = MakeEntity(level: 1, vit: 5, intStat: 5);
        e.HP = 10;
        e.MP = 5;

        StatSystem.RecalculateDerived(e);

        Assert.Equal(10, e.HP);
        Assert.Equal(5, e.MP);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Equipment changes — TotalDamage / TotalDefense
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void EquipWeapon_IncreasesTotalDamage()
    {
        var e = MakeEntity(baseDamage: 12);
        int before = e.TotalDamage; // 12

        e.Equipment[EquipSlot.MainHand] = new ItemData { Name = "Sword", Damage = 10, Slot = EquipSlot.MainHand };
        e.InvalidateStats();

        Assert.Equal(before + 10, e.TotalDamage);
    }

    [Fact]
    public void EquipArmor_IncreasesTotalDefense()
    {
        var e = MakeEntity(baseDefense: 0);
        Assert.Equal(0, e.TotalDefense);

        e.Equipment[EquipSlot.Body] = new ItemData { Name = "Plate", Defense = 15, Slot = EquipSlot.Body };
        e.InvalidateStats();

        Assert.Equal(15, e.TotalDefense);
    }

    [Fact]
    public void MultipleArmor_DefenseStacks()
    {
        var e = MakeEntity(baseDefense: 5);

        e.Equipment[EquipSlot.Body] = new ItemData { Name = "Plate", Defense = 10, Slot = EquipSlot.Body };
        e.Equipment[EquipSlot.Head] = new ItemData { Name = "Helm", Defense = 5, Slot = EquipSlot.Head };
        e.Equipment[EquipSlot.Legs] = new ItemData { Name = "Greaves", Defense = 3, Slot = EquipSlot.Legs };
        e.InvalidateStats();

        // BaseDefense(5) + Body(10) + Head(5) + Legs(3) = 23
        Assert.Equal(23, e.TotalDefense);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Stat dirty flag — caching behavior
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void TotalDamage_ReturnsCached_WithoutInvalidate()
    {
        var e = MakeEntity(baseDamage: 12);
        int first = e.TotalDamage;
        int second = e.TotalDamage;

        Assert.Equal(first, second);
    }

    [Fact]
    public void InvalidateStats_ForcesRecalc()
    {
        var e = MakeEntity(baseDamage: 12);
        int before = e.TotalDamage;

        // Mutate BaseDamage directly without invalidating
        e.BaseDamage = 50;
        int cached = e.TotalDamage; // still returns old cached value
        Assert.Equal(before, cached);

        // Now invalidate
        e.InvalidateStats();
        int recalced = e.TotalDamage;
        Assert.Equal(50, recalced); // now reflects new BaseDamage
    }

    [Fact]
    public void TotalDefense_CachesCorrectly()
    {
        var e = MakeEntity(baseDefense: 10);
        int first = e.TotalDefense;

        e.BaseDefense = 20;
        Assert.Equal(first, e.TotalDefense); // still cached

        e.InvalidateStats();
        Assert.Equal(20, e.TotalDefense); // now updated
    }

    // ═══════════════════════════════════════════════════════════════
    //  GetAttackSpeed / GetMoveSpeed
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void GetAttackSpeed_DecreasesWithDEX()
    {
        var lowDex = MakeEntity(dex: 5);
        var highDex = MakeEntity(dex: 100);

        float slow = StatSystem.GetAttackSpeed(lowDex);
        float fast = StatSystem.GetAttackSpeed(highDex);

        Assert.True(fast < slow, "Higher DEX should reduce attack interval");
    }

    [Fact]
    public void GetMoveSpeed_IncreasesWithDEX()
    {
        var lowDex = MakeEntity(dex: 5);
        var highDex = MakeEntity(dex: 100);

        float slow = StatSystem.GetMoveSpeed(lowDex);
        float fast = StatSystem.GetMoveSpeed(highDex);

        Assert.True(fast > slow, "Higher DEX should increase movement speed");
    }

    [Fact]
    public void GetAttackSpeed_ZeroDEX_ReturnsBaseSpeed()
    {
        var e = MakeEntity(dex: 0);
        // GetEffective(0) = 0, so multiplier = (1 - 0*0.003) = 1.0
        Assert.Equal(e.AttackSpeed, StatSystem.GetAttackSpeed(e));
    }

    [Fact]
    public void GetMoveSpeed_ZeroDEX_ReturnsBaseSpeed()
    {
        var e = MakeEntity(dex: 0);
        // GetEffective(0) = 0, so bonus = 0*0.02 = 0
        Assert.Equal(e.MoveSpeed, StatSystem.GetMoveSpeed(e));
    }

    // ═══════════════════════════════════════════════════════════════
    //  Edge combo: equip item changing MaxHP below current HP
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void RecalculateDerived_EquipRemovalDropsMaxBelowCurrent_ClampsHP()
    {
        var e = MakeEntity(level: 1, vit: 0);
        // Add HP bonus equipment
        e.Equipment[EquipSlot.Ring] = new ItemData { Name = "Ring of Life", HPBonus = 100, Slot = EquipSlot.Ring };
        StatSystem.RecalculateDerived(e);
        // MaxHP = 100 + 8 + 0 + 100 = 208
        Assert.Equal(208, e.MaxHP);
        e.HP = 208;

        // Remove the ring
        e.Equipment.Remove(EquipSlot.Ring);
        StatSystem.RecalculateDerived(e);
        // MaxHP = 100 + 8 + 0 = 108, HP was 208 -> clamped to 108
        Assert.Equal(108, e.MaxHP);
        Assert.Equal(108, e.HP);
    }
}
