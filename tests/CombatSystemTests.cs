using System;
using Xunit;

namespace DungeonGame.Tests;

public class CombatSystemTests
{
    private EntityData MakeAttacker(int baseDamage = 20, int baseDefense = 0)
    {
        return new EntityData
        {
            Id = "attacker",
            Name = "Attacker",
            Type = EntityType.Player,
            HP = 200,
            MaxHP = 200,
            MP = 50,
            MaxMP = 50,
            BaseDamage = baseDamage,
            BaseDefense = baseDefense,
            Level = 1,
        };
    }

    private EntityData MakeTarget(int hp = 100, int maxHp = 100, int baseDefense = 0)
    {
        return new EntityData
        {
            Id = "target",
            Name = "Target",
            Type = EntityType.Enemy,
            HP = hp,
            MaxHP = maxHp,
            BaseDamage = 10,
            BaseDefense = baseDefense,
            Level = 1,
        };
    }

    // ═══════════════════════════════════════════════════════════════
    //  DealDamage — basic damage application
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void DealDamage_ReducesTargetHP()
    {
        var attacker = MakeAttacker(baseDamage: 20);
        var target = MakeTarget(hp: 100, baseDefense: 0);

        int startHP = target.HP;
        CombatSystem.DealDamage(attacker, target);

        Assert.True(target.HP < startHP, "Target HP should decrease");
    }

    [Fact]
    public void DealDamage_ReturnsCorrectResult()
    {
        var attacker = MakeAttacker(baseDamage: 20);
        var target = MakeTarget(hp: 100, baseDefense: 0);

        var result = CombatSystem.DealDamage(attacker, target);

        Assert.True(result.MitigatedDamage >= 1, "Damage should be at least 1");
        Assert.True(result.RawDamage >= attacker.TotalDamage, "Raw damage should be >= TotalDamage");
    }

    // ═══════════════════════════════════════════════════════════════
    //  Symmetry: player→enemy uses same code as enemy→player
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void DealDamage_Symmetry_BothDirectionsUseSameFunction()
    {
        // Create a player and enemy with identical stats
        var player = new EntityData
        {
            Id = "player", Name = "Player", Type = EntityType.Player,
            HP = 200, MaxHP = 200, BaseDamage = 15, BaseDefense = 10, Level = 1,
        };
        var enemy = new EntityData
        {
            Id = "enemy", Name = "Enemy", Type = EntityType.Enemy,
            HP = 200, MaxHP = 200, BaseDamage = 15, BaseDefense = 10, Level = 1,
        };

        // Damage preview (deterministic, no crit) should be identical both ways
        int playerToEnemy = CombatSystem.GetDamagePreview(player, enemy);
        int enemyToPlayer = CombatSystem.GetDamagePreview(enemy, player);

        Assert.Equal(playerToEnemy, enemyToPlayer);
    }

    [Fact]
    public void DealDamage_Symmetry_DifferentStats_PreviewReflectsAttacker()
    {
        var strong = MakeAttacker(baseDamage: 50);
        var weak = MakeTarget(hp: 200, baseDefense: 0);
        weak.BaseDamage = 5;

        int strongHitsWeak = CombatSystem.GetDamagePreview(strong, weak);
        int weakHitsStrong = CombatSystem.GetDamagePreview(weak, strong);

        Assert.True(strongHitsWeak > weakHitsStrong);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Defense reduces damage
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void DealDamage_DefenseReducesMitigatedDamage()
    {
        var attacker = MakeAttacker(baseDamage: 20);
        var noArmor = MakeTarget(hp: 200, baseDefense: 0);
        var armored = MakeTarget(hp: 200, baseDefense: 50);

        int dmgNoArmor = CombatSystem.GetDamagePreview(attacker, noArmor);
        int dmgArmored = CombatSystem.GetDamagePreview(attacker, armored);

        Assert.True(dmgArmored < dmgNoArmor, "Armored target should take less damage");
    }

    [Fact]
    public void DealDamage_HighDefense_StillDealsMinimum1()
    {
        var attacker = MakeAttacker(baseDamage: 1);
        var fortress = MakeTarget(hp: 200, baseDefense: 999999);

        int preview = CombatSystem.GetDamagePreview(attacker, fortress);
        Assert.Equal(1, preview);

        // Also verify with actual DealDamage — many runs to account for crits
        for (int i = 0; i < 50; i++)
        {
            var target = MakeTarget(hp: 200, baseDefense: 999999);
            var result = CombatSystem.DealDamage(attacker, target);
            Assert.True(result.MitigatedDamage >= 1, "Even high defense should result in at least 1 damage");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Minimum 1 damage
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void DealDamage_ZeroBaseDamage_StillDeals1()
    {
        var attacker = MakeAttacker(baseDamage: 0);
        var target = MakeTarget(hp: 100, baseDefense: 0);

        // TotalDamage = 0, so raw = 0, but minimum is 1
        // Actually: with baseDamage=0, TotalDamage=0, rawDamage=0,
        // mitigated = 0 - 0 = 0, Math.Max(1, 0) = 1
        int preview = CombatSystem.GetDamagePreview(attacker, target);
        Assert.Equal(1, preview);
    }

    // ═══════════════════════════════════════════════════════════════
    //  CanAttack
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void CanAttack_Alive_ReturnsTrue()
    {
        var e = MakeAttacker();
        Assert.True(CombatSystem.CanAttack(e));
    }

    [Fact]
    public void CanAttack_Dead_ReturnsFalse()
    {
        var e = MakeAttacker();
        e.IsDead = true;
        Assert.False(CombatSystem.CanAttack(e));
    }

    [Fact]
    public void CanAttack_HP0_ReturnsFalse()
    {
        var e = MakeAttacker();
        e.HP = 0;
        Assert.False(CombatSystem.CanAttack(e));
    }

    [Fact]
    public void CanAttack_HP0_IsDeadTrue_ReturnsFalse()
    {
        var e = MakeAttacker();
        e.HP = 0;
        e.IsDead = true;
        Assert.False(CombatSystem.CanAttack(e));
    }

    // ═══════════════════════════════════════════════════════════════
    //  GetDamagePreview — matches actual non-crit damage
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void GetDamagePreview_MatchesFormula()
    {
        var attacker = MakeAttacker(baseDamage: 30);
        var target = MakeTarget(hp: 200, baseDefense: 20);

        int preview = CombatSystem.GetDamagePreview(attacker, target);

        // Manual calc: rawDamage = TotalDamage = 30
        // defenseReduction = GetEffective(20) / 100 = (20 * 100/120) / 100
        float defReduction = StatSystem.GetDefenseReduction(target);
        int expected = 30 - (int)(30 * defReduction);
        expected = Math.Max(1, expected);

        Assert.Equal(expected, preview);
    }

    [Fact]
    public void GetDamagePreview_DoesNotApplyDamage()
    {
        var attacker = MakeAttacker(baseDamage: 20);
        var target = MakeTarget(hp: 100);

        CombatSystem.GetDamagePreview(attacker, target);

        Assert.Equal(100, target.HP); // HP unchanged
    }

    // ═══════════════════════════════════════════════════════════════
    //  Target dies when HP reaches 0
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void DealDamage_KillsTarget_WhenHPReachesZero()
    {
        var attacker = MakeAttacker(baseDamage: 200);
        var target = MakeTarget(hp: 1, baseDefense: 0);

        var result = CombatSystem.DealDamage(attacker, target);

        Assert.True(result.TargetDied);
        Assert.True(target.IsDead);
        Assert.Equal(0, target.HP);
    }

    [Fact]
    public void DealDamage_DoesNotKill_WhenTargetSurvives()
    {
        var attacker = MakeAttacker(baseDamage: 1);
        var target = MakeTarget(hp: 1000, baseDefense: 0);

        var result = CombatSystem.DealDamage(attacker, target);

        Assert.False(result.TargetDied);
        Assert.False(target.IsDead);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Multi-hit scenarios
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void DealDamage_MultipleHits_AccumulateDamage()
    {
        var attacker = MakeAttacker(baseDamage: 5);
        var target = MakeTarget(hp: 100, baseDefense: 0);

        int totalMitigated = 0;
        for (int i = 0; i < 200; i++)
        {
            if (target.IsDead) break;
            var result = CombatSystem.DealDamage(attacker, target);
            totalMitigated += result.MitigatedDamage;
        }

        Assert.Equal(0, target.HP);
        Assert.True(target.IsDead);
        // MitigatedDamage is pre-HP-clamp: the killing blow may report more
        // than remaining HP (due to crits), so total >= starting HP
        Assert.True(totalMitigated >= 100, $"Total mitigated {totalMitigated} should be >= 100");
    }

    // ═══════════════════════════════════════════════════════════════
    //  Crit detection over many runs
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void DealDamage_CritHappensOverManyTrials()
    {
        var attacker = MakeAttacker(baseDamage: 20);
        int critCount = 0;
        int trials = 500;

        for (int i = 0; i < trials; i++)
        {
            var target = MakeTarget(hp: 10000, baseDefense: 0);
            var result = CombatSystem.DealDamage(attacker, target);
            if (result.IsCrit)
                critCount++;
        }

        // 15% crit rate, over 500 trials: expect ~75. Allow wide margin.
        Assert.True(critCount > 10, $"Expected some crits but got {critCount}/{trials}");
        Assert.True(critCount < 200, $"Expected ~15% crits but got {critCount}/{trials}");
    }

    [Fact]
    public void DealDamage_CritDamage_IsHigherThanNonCrit()
    {
        var attacker = MakeAttacker(baseDamage: 100);

        // Collect crit and non-crit raw damage values
        int? critRaw = null;
        int? noncritRaw = null;

        for (int i = 0; i < 1000 && (critRaw == null || noncritRaw == null); i++)
        {
            var target = MakeTarget(hp: 10000, baseDefense: 0);
            var result = CombatSystem.DealDamage(attacker, target);
            if (result.IsCrit && critRaw == null)
                critRaw = result.RawDamage;
            else if (!result.IsCrit && noncritRaw == null)
                noncritRaw = result.RawDamage;
        }

        Assert.NotNull(critRaw);
        Assert.NotNull(noncritRaw);
        Assert.True(critRaw > noncritRaw, $"Crit ({critRaw}) should be > non-crit ({noncritRaw})");
    }

    // ═══════════════════════════════════════════════════════════════
    //  Defense formula regression
    // ═══════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(0, 20, 20)]    // 0 defense => no reduction => full damage
    [InlineData(100, 20, 10)]  // 50% reduction => 20 - 10 = 10
    public void GetDamagePreview_DefenseFormula_RegressionGuard(int defense, int baseDamage, int expectedDmg)
    {
        var attacker = MakeAttacker(baseDamage: baseDamage);
        var target = MakeTarget(hp: 200, baseDefense: defense);

        int preview = CombatSystem.GetDamagePreview(attacker, target);
        Assert.Equal(expectedDmg, preview);
    }
}
