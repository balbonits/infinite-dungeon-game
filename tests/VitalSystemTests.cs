using Xunit;

namespace DungeonGame.Tests;

public class VitalSystemTests
{
    private EntityData MakeEntity(int hp = 100, int maxHp = 100, int mp = 50, int maxMp = 50)
    {
        return new EntityData
        {
            Id = "test",
            Name = "TestEntity",
            Type = EntityType.Player,
            HP = hp,
            MaxHP = maxHp,
            MP = mp,
            MaxMP = maxMp,
        };
    }

    // ═══════════════════════════════════════════════════════════════
    //  TakeDamage
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void TakeDamage_Normal_ReducesHP()
    {
        var e = MakeEntity(hp: 100);
        var (actual, died) = VitalSystem.TakeDamage(e, 30);

        Assert.Equal(30, actual);
        Assert.Equal(70, e.HP);
        Assert.False(died);
        Assert.False(e.IsDead);
    }

    [Fact]
    public void TakeDamage_ExactKill_DamageEqualsHP()
    {
        var e = MakeEntity(hp: 50);
        var (actual, died) = VitalSystem.TakeDamage(e, 50);

        Assert.Equal(50, actual);
        Assert.Equal(0, e.HP);
        Assert.True(died);
        Assert.True(e.IsDead);
    }

    [Fact]
    public void TakeDamage_Overkill_ClampsToZero()
    {
        var e = MakeEntity(hp: 20);
        var (actual, died) = VitalSystem.TakeDamage(e, 999);

        Assert.Equal(20, actual);  // actual damage capped to remaining HP
        Assert.Equal(0, e.HP);
        Assert.True(died);
        Assert.True(e.IsDead);
    }

    [Fact]
    public void TakeDamage_ZeroDamage_NoChange()
    {
        var e = MakeEntity(hp: 100);
        var (actual, died) = VitalSystem.TakeDamage(e, 0);

        Assert.Equal(0, actual);
        Assert.Equal(100, e.HP);
        Assert.False(died);
    }

    [Fact]
    public void TakeDamage_NegativeDamage_TreatedAsZero()
    {
        var e = MakeEntity(hp: 100);
        var (actual, died) = VitalSystem.TakeDamage(e, -50);

        Assert.Equal(0, actual);
        Assert.Equal(100, e.HP);
        Assert.False(died);
    }

    [Fact]
    public void TakeDamage_HP1_SingleDamageKills()
    {
        var e = MakeEntity(hp: 1);
        var (actual, died) = VitalSystem.TakeDamage(e, 1);

        Assert.Equal(1, actual);
        Assert.Equal(0, e.HP);
        Assert.True(died);
    }

    [Fact]
    public void TakeDamage_HPNeverBelowZero()
    {
        var e = MakeEntity(hp: 5);
        VitalSystem.TakeDamage(e, 100);
        Assert.Equal(0, e.HP);

        // Hit the corpse again
        VitalSystem.TakeDamage(e, 100);
        Assert.Equal(0, e.HP);
    }

    [Fact]
    public void TakeDamage_AlreadyDead_StillProcesses()
    {
        var e = MakeEntity(hp: 0);
        e.IsDead = true;
        var (actual, died) = VitalSystem.TakeDamage(e, 50);

        // With HP=0, actualDamage is min(50, 0) = 0
        Assert.Equal(0, actual);
        Assert.Equal(0, e.HP);
        Assert.True(died); // HP <= 0 so IsDead set true again
    }

    // ═══════════════════════════════════════════════════════════════
    //  Heal
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Heal_Normal_IncreasesHP()
    {
        var e = MakeEntity(hp: 50, maxHp: 100);
        int healed = VitalSystem.Heal(e, 30);

        Assert.Equal(30, healed);
        Assert.Equal(80, e.HP);
    }

    [Fact]
    public void Heal_Overheal_ClampsToMaxHP()
    {
        var e = MakeEntity(hp: 90, maxHp: 100);
        int healed = VitalSystem.Heal(e, 50);

        Assert.Equal(10, healed);
        Assert.Equal(100, e.HP);
    }

    [Fact]
    public void Heal_AtFullHP_ReturnsZero()
    {
        var e = MakeEntity(hp: 100, maxHp: 100);
        int healed = VitalSystem.Heal(e, 50);

        Assert.Equal(0, healed);
        Assert.Equal(100, e.HP);
    }

    [Fact]
    public void Heal_ZeroAmount_NoChange()
    {
        var e = MakeEntity(hp: 50, maxHp: 100);
        int healed = VitalSystem.Heal(e, 0);

        Assert.Equal(0, healed);
        Assert.Equal(50, e.HP);
    }

    [Fact]
    public void Heal_NegativeAmount_TreatedAsZero()
    {
        var e = MakeEntity(hp: 50, maxHp: 100);
        int healed = VitalSystem.Heal(e, -30);

        Assert.Equal(0, healed);
        Assert.Equal(50, e.HP);
    }

    [Fact]
    public void Heal_DeadEntity_StillHeals()
    {
        // Heal does NOT check IsDead — this is intentional (revive sets IsDead separately)
        var e = MakeEntity(hp: 0, maxHp: 100);
        e.IsDead = true;
        int healed = VitalSystem.Heal(e, 50);

        Assert.Equal(50, healed);
        Assert.Equal(50, e.HP);
        // Note: IsDead remains true — Heal doesn't reset it
        Assert.True(e.IsDead);
    }

    // ═══════════════════════════════════════════════════════════════
    //  RestoreMP
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void RestoreMP_Normal_IncreasesMP()
    {
        var e = MakeEntity(mp: 20, maxMp: 50);
        int restored = VitalSystem.RestoreMP(e, 15);

        Assert.Equal(15, restored);
        Assert.Equal(35, e.MP);
    }

    [Fact]
    public void RestoreMP_Overflow_ClampsToMaxMP()
    {
        var e = MakeEntity(mp: 45, maxMp: 50);
        int restored = VitalSystem.RestoreMP(e, 20);

        Assert.Equal(5, restored);
        Assert.Equal(50, e.MP);
    }

    [Fact]
    public void RestoreMP_AtFull_ReturnsZero()
    {
        var e = MakeEntity(mp: 50, maxMp: 50);
        int restored = VitalSystem.RestoreMP(e, 10);

        Assert.Equal(0, restored);
        Assert.Equal(50, e.MP);
    }

    [Fact]
    public void RestoreMP_ZeroAmount_NoChange()
    {
        var e = MakeEntity(mp: 20, maxMp: 50);
        int restored = VitalSystem.RestoreMP(e, 0);

        Assert.Equal(0, restored);
        Assert.Equal(20, e.MP);
    }

    [Fact]
    public void RestoreMP_NegativeAmount_TreatedAsZero()
    {
        var e = MakeEntity(mp: 20, maxMp: 50);
        int restored = VitalSystem.RestoreMP(e, -10);

        Assert.Equal(0, restored);
        Assert.Equal(20, e.MP);
    }

    // ═══════════════════════════════════════════════════════════════
    //  SpendMP
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void SpendMP_Sufficient_ReturnsTrue()
    {
        var e = MakeEntity(mp: 50);
        bool ok = VitalSystem.SpendMP(e, 20);

        Assert.True(ok);
        Assert.Equal(30, e.MP);
    }

    [Fact]
    public void SpendMP_ExactMP_ReturnsTrue_DropsToZero()
    {
        var e = MakeEntity(mp: 30, maxMp: 50);
        bool ok = VitalSystem.SpendMP(e, 30);

        Assert.True(ok);
        Assert.Equal(0, e.MP);
    }

    [Fact]
    public void SpendMP_Insufficient_ReturnsFalse_NoChange()
    {
        var e = MakeEntity(mp: 10, maxMp: 50);
        bool ok = VitalSystem.SpendMP(e, 20);

        Assert.False(ok);
        Assert.Equal(10, e.MP);
    }

    [Fact]
    public void SpendMP_ZeroMP_SpendZero_ReturnsTrue()
    {
        var e = MakeEntity(mp: 0, maxMp: 50);
        bool ok = VitalSystem.SpendMP(e, 0);

        Assert.True(ok);
        Assert.Equal(0, e.MP);
    }

    [Fact]
    public void SpendMP_ZeroMP_SpendAny_ReturnsFalse()
    {
        var e = MakeEntity(mp: 0, maxMp: 50);
        bool ok = VitalSystem.SpendMP(e, 1);

        Assert.False(ok);
    }

    // ═══════════════════════════════════════════════════════════════
    //  IsAlive
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void IsAlive_HealthyEntity_True()
    {
        var e = MakeEntity(hp: 100);
        Assert.True(VitalSystem.IsAlive(e));
    }

    [Fact]
    public void IsAlive_DeadByIsDead_False()
    {
        var e = MakeEntity(hp: 50);
        e.IsDead = true;
        Assert.False(VitalSystem.IsAlive(e));
    }

    [Fact]
    public void IsAlive_DeadByHP0_False()
    {
        var e = MakeEntity(hp: 0);
        // HP=0 alone is enough for !IsAlive
        Assert.False(VitalSystem.IsAlive(e));
    }

    [Fact]
    public void IsAlive_HP0_IsDeadNotSetManually_StillFalse()
    {
        var e = MakeEntity(hp: 0);
        e.IsDead = false; // explicitly not set
        // IsAlive checks !IsDead && HP > 0, so HP=0 => false
        Assert.False(VitalSystem.IsAlive(e));
    }

    [Fact]
    public void IsAlive_HP1_True()
    {
        var e = MakeEntity(hp: 1);
        Assert.True(VitalSystem.IsAlive(e));
    }

    // ═══════════════════════════════════════════════════════════════
    //  Revive
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Revive_Default50Percent()
    {
        var e = MakeEntity(hp: 0, maxHp: 100);
        e.IsDead = true;

        VitalSystem.Revive(e);

        Assert.False(e.IsDead);
        Assert.Equal(50, e.HP);  // 100 * 0.5
    }

    [Fact]
    public void Revive_CustomPercentage()
    {
        var e = MakeEntity(hp: 0, maxHp: 200);
        e.IsDead = true;

        VitalSystem.Revive(e, 0.25f);

        Assert.False(e.IsDead);
        Assert.Equal(50, e.HP);  // 200 * 0.25
    }

    [Fact]
    public void Revive_AtMinimum1HP()
    {
        var e = MakeEntity(hp: 0, maxHp: 1);
        e.IsDead = true;

        // 1 * 0.01 = 0.01 => (int) = 0, but Math.Max(1, ...) => 1
        VitalSystem.Revive(e, 0.01f);

        Assert.False(e.IsDead);
        Assert.Equal(1, e.HP);
    }

    [Fact]
    public void Revive_FullHP()
    {
        var e = MakeEntity(hp: 0, maxHp: 100);
        e.IsDead = true;

        VitalSystem.Revive(e, 1.0f);

        Assert.False(e.IsDead);
        Assert.Equal(100, e.HP);
    }

    [Fact]
    public void Revive_SetsHPCorrectly_OddMax()
    {
        var e = MakeEntity(hp: 0, maxHp: 77);
        e.IsDead = true;

        VitalSystem.Revive(e);  // 50%

        Assert.False(e.IsDead);
        Assert.Equal(System.Math.Max(1, (int)(77 * 0.5f)), e.HP);
    }

    // ═══════════════════════════════════════════════════════════════
    //  RegenTick
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void RegenTick_SmallDelta_NoRegen()
    {
        var e = MakeEntity(hp: 50, maxHp: 100);
        // 5 hp/sec * 0.1 delta = 0.5 => (int)0.5 = 0 => no heal
        VitalSystem.RegenTick(e, 5f, 0f, 0.1f);
        Assert.Equal(50, e.HP);
    }

    [Fact]
    public void RegenTick_LargeDelta_Heals()
    {
        var e = MakeEntity(hp: 50, maxHp: 100);
        e.MP = 20;
        e.MaxMP = 50;

        // 10 hp/sec * 1.0 delta = 10 => heal 10
        // 5 mp/sec * 1.0 delta = 5 => restore 5
        VitalSystem.RegenTick(e, 10f, 5f, 1.0f);

        Assert.Equal(60, e.HP);
        Assert.Equal(25, e.MP);
    }

    [Fact]
    public void RegenTick_Dead_NoRegen()
    {
        var e = MakeEntity(hp: 0, maxHp: 100);
        e.IsDead = true;
        e.MP = 0;
        e.MaxMP = 50;

        VitalSystem.RegenTick(e, 100f, 100f, 10f);

        Assert.Equal(0, e.HP);
        Assert.Equal(0, e.MP);
    }

    [Fact]
    public void RegenTick_HPCappedAtMax()
    {
        var e = MakeEntity(hp: 95, maxHp: 100);
        VitalSystem.RegenTick(e, 100f, 0f, 1.0f);  // 100 hp regen

        Assert.Equal(100, e.HP);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Multi-step / rapid cycle scenarios
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void RapidDamageHealCycle_HPCorrect()
    {
        var e = MakeEntity(hp: 100, maxHp: 100);

        VitalSystem.TakeDamage(e, 30);  // 100 -> 70
        Assert.Equal(70, e.HP);

        VitalSystem.Heal(e, 20);        // 70 -> 90
        Assert.Equal(90, e.HP);

        VitalSystem.TakeDamage(e, 50);  // 90 -> 40
        Assert.Equal(40, e.HP);

        VitalSystem.Heal(e, 60);        // 40 -> 100 (capped)
        Assert.Equal(100, e.HP);

        VitalSystem.TakeDamage(e, 100); // 100 -> 0 (dead)
        Assert.Equal(0, e.HP);
        Assert.True(e.IsDead);
    }

    [Fact]
    public void Kill_ThenHeal_HPRestoresButIsDeadRemains()
    {
        var e = MakeEntity(hp: 50, maxHp: 100);

        VitalSystem.TakeDamage(e, 50); // kill
        Assert.True(e.IsDead);
        Assert.Equal(0, e.HP);

        // Heal doesn't check IsDead — intentional design
        VitalSystem.Heal(e, 30);
        Assert.Equal(30, e.HP);
        Assert.True(e.IsDead); // still dead until Revive is called
    }

    [Fact]
    public void Kill_Revive_FullCycle()
    {
        var e = MakeEntity(hp: 100, maxHp: 100);

        VitalSystem.TakeDamage(e, 100);
        Assert.True(e.IsDead);
        Assert.False(VitalSystem.IsAlive(e));

        VitalSystem.Revive(e, 0.5f);
        Assert.False(e.IsDead);
        Assert.Equal(50, e.HP);
        Assert.True(VitalSystem.IsAlive(e));
    }

    [Fact]
    public void ManySmallDamageHits_AccumulateCorrectly()
    {
        var e = MakeEntity(hp: 100, maxHp: 100);

        for (int i = 0; i < 99; i++)
            VitalSystem.TakeDamage(e, 1);

        Assert.Equal(1, e.HP);
        Assert.False(e.IsDead);

        VitalSystem.TakeDamage(e, 1);
        Assert.Equal(0, e.HP);
        Assert.True(e.IsDead);
    }

    [Fact]
    public void SpendMP_ThenRestoreMP_Cycle()
    {
        var e = MakeEntity(mp: 50, maxMp: 50);

        Assert.True(VitalSystem.SpendMP(e, 20));   // 50 -> 30
        Assert.Equal(30, e.MP);

        Assert.Equal(20, VitalSystem.RestoreMP(e, 20)); // 30 -> 50
        Assert.Equal(50, e.MP);

        Assert.True(VitalSystem.SpendMP(e, 50));   // 50 -> 0
        Assert.Equal(0, e.MP);

        Assert.False(VitalSystem.SpendMP(e, 1));   // can't spend
        Assert.Equal(0, e.MP);

        Assert.Equal(50, VitalSystem.RestoreMP(e, 100)); // 0 -> 50 (capped)
        Assert.Equal(50, e.MP);
    }
}
