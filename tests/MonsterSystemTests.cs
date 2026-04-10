using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace DungeonGame.Tests;

public class MonsterBehaviorTests
{
    [Theory]
    [InlineData(MonsterArchetype.Melee, 384f)]
    [InlineData(MonsterArchetype.Ranged, 512f)]
    [InlineData(MonsterArchetype.Bruiser, 320f)]
    [InlineData(MonsterArchetype.Swarmer, 448f)]
    public void AggroRange_PerArchetype(MonsterArchetype arch, float expected)
        => Assert.Equal(expected, MonsterBehavior.GetAggroRange(arch));

    [Fact] public void Swarmer_SkipsAlert()
        => Assert.Equal(0f, MonsterBehavior.GetAlertDuration(MonsterArchetype.Swarmer));

    [Fact] public void Bruiser_SlowerThanSwarmer()
        => Assert.True(MonsterBehavior.GetSpeedMultiplier(MonsterArchetype.Bruiser) <
                        MonsterBehavior.GetSpeedMultiplier(MonsterArchetype.Swarmer));

    // State machine
    [Fact] public void Idle_ToAlert_WhenPlayerInRange()
    {
        var next = MonsterBehavior.GetNextState(MonsterAIState.Idle, MonsterArchetype.Melee,
            distanceToPlayer: 300f, currentHP: 100, maxHP: 100, alertTimer: 0.3f, cooldownTimer: 0);
        Assert.Equal(MonsterAIState.Alert, next);
    }

    [Fact] public void Idle_StaysIdle_WhenPlayerFar()
    {
        var next = MonsterBehavior.GetNextState(MonsterAIState.Idle, MonsterArchetype.Melee,
            distanceToPlayer: 1000f, currentHP: 100, maxHP: 100, alertTimer: 0, cooldownTimer: 0);
        Assert.Equal(MonsterAIState.Idle, next);
    }

    [Fact] public void Swarmer_Idle_DirectlyToChase()
    {
        var next = MonsterBehavior.GetNextState(MonsterAIState.Idle, MonsterArchetype.Swarmer,
            distanceToPlayer: 400f, currentHP: 100, maxHP: 100, alertTimer: 0, cooldownTimer: 0);
        Assert.Equal(MonsterAIState.Chase, next); // Swarmer alert duration = 0
    }

    [Fact] public void Alert_ToChase_WhenTimerExpires()
    {
        var next = MonsterBehavior.GetNextState(MonsterAIState.Alert, MonsterArchetype.Melee,
            distanceToPlayer: 300f, currentHP: 100, maxHP: 100, alertTimer: 0f, cooldownTimer: 0);
        Assert.Equal(MonsterAIState.Chase, next);
    }

    [Fact] public void Chase_ToAttack_WhenInRange()
    {
        var next = MonsterBehavior.GetNextState(MonsterAIState.Chase, MonsterArchetype.Melee,
            distanceToPlayer: 20f, currentHP: 100, maxHP: 100, alertTimer: 0, cooldownTimer: 0);
        Assert.Equal(MonsterAIState.Attack, next);
    }

    [Fact] public void Chase_ToIdle_WhenLeashed()
    {
        float aggroRange = MonsterBehavior.GetAggroRange(MonsterArchetype.Melee);
        var next = MonsterBehavior.GetNextState(MonsterAIState.Chase, MonsterArchetype.Melee,
            distanceToPlayer: aggroRange * 1.6f, currentHP: 100, maxHP: 100, alertTimer: 0, cooldownTimer: 0);
        Assert.Equal(MonsterAIState.Idle, next);
    }

    [Fact] public void Attack_AlwaysToCooldown()
    {
        var next = MonsterBehavior.GetNextState(MonsterAIState.Attack, MonsterArchetype.Melee,
            distanceToPlayer: 20f, currentHP: 100, maxHP: 100, alertTimer: 0, cooldownTimer: 0);
        Assert.Equal(MonsterAIState.Cooldown, next);
    }

    [Fact] public void Cooldown_MeleeToChase_WhenExpired()
    {
        var next = MonsterBehavior.GetNextState(MonsterAIState.Cooldown, MonsterArchetype.Melee,
            distanceToPlayer: 50f, currentHP: 100, maxHP: 100, alertTimer: 0, cooldownTimer: 0f);
        Assert.Equal(MonsterAIState.Chase, next);
    }

    [Fact] public void Cooldown_RangedToRetreat_WhenPlayerClose()
    {
        float preferred = MonsterBehavior.GetPreferredDistance(MonsterArchetype.Ranged);
        var next = MonsterBehavior.GetNextState(MonsterAIState.Cooldown, MonsterArchetype.Ranged,
            distanceToPlayer: preferred * 0.5f, currentHP: 100, maxHP: 100, alertTimer: 0, cooldownTimer: 0f);
        Assert.Equal(MonsterAIState.Retreat, next);
    }

    [Fact] public void Retreat_ToChase_WhenAtDistance()
    {
        float preferred = MonsterBehavior.GetPreferredDistance(MonsterArchetype.Ranged);
        var next = MonsterBehavior.GetNextState(MonsterAIState.Retreat, MonsterArchetype.Ranged,
            distanceToPlayer: preferred + 10f, currentHP: 100, maxHP: 100, alertTimer: 0, cooldownTimer: 0);
        Assert.Equal(MonsterAIState.Chase, next);
    }

    [Fact] public void Dead_FromAnyState()
    {
        foreach (MonsterAIState state in Enum.GetValues<MonsterAIState>())
        {
            var next = MonsterBehavior.GetNextState(state, MonsterArchetype.Melee,
                distanceToPlayer: 100f, currentHP: 0, maxHP: 100, alertTimer: 0, cooldownTimer: 0);
            Assert.Equal(MonsterAIState.Dead, next);
        }
    }

    [Fact] public void Dead_StaysDead()
    {
        var next = MonsterBehavior.GetNextState(MonsterAIState.Dead, MonsterArchetype.Melee,
            distanceToPlayer: 10f, currentHP: 0, maxHP: 100, alertTimer: 0, cooldownTimer: 0);
        Assert.Equal(MonsterAIState.Dead, next);
    }
}

public class MonsterModifierTests
{
    [Fact] public void ExtraFast_SpeedMultiplier()
        => Assert.Equal(1.33f, MonsterModifiers.GetSpeedMultiplier(MonsterModifierType.ExtraFast));

    [Fact] public void ExtraStrong_DamageMultiplier()
        => Assert.Equal(1.5f, MonsterModifiers.GetDamageMultiplier(MonsterModifierType.ExtraStrong));

    [Fact] public void StoneSkin_DefenseBonus()
        => Assert.Equal(80, MonsterModifiers.GetDefenseBonus(MonsterModifierType.StoneSkin));

    [Fact] public void RollModifiers_ReturnsRequestedCount()
    {
        var mods = MonsterModifiers.RollModifiers(3, new Random(42));
        Assert.Equal(3, mods.Count);
    }

    [Fact] public void RollModifiers_NoDuplicates()
    {
        var mods = MonsterModifiers.RollModifiers(5, new Random(42));
        Assert.Equal(mods.Count, mods.Distinct().Count());
    }

    [Fact] public void RollModifiers_CapsAtAvailable()
    {
        int total = Enum.GetValues<MonsterModifierType>().Length;
        var mods = MonsterModifiers.RollModifiers(total + 5, new Random(42));
        Assert.Equal(total, mods.Count);
    }

    [Fact] public void CombinedEffects_MultiplyCorrectly()
    {
        var mods = new List<MonsterModifierType> { MonsterModifierType.ExtraFast, MonsterModifierType.ExtraStrong };
        var (speed, damage, defense) = MonsterModifiers.GetCombinedEffects(mods);
        Assert.Equal(1.33f, speed, 2);
        Assert.Equal(1.5f, damage, 2);
        Assert.Equal(0, defense);
    }

    [Fact] public void CombinedEffects_WithStoneSkin()
    {
        var mods = new List<MonsterModifierType> { MonsterModifierType.StoneSkin, MonsterModifierType.ExtraFast };
        var (speed, damage, defense) = MonsterModifiers.GetCombinedEffects(mods);
        Assert.Equal(1.33f, speed, 2);
        Assert.Equal(1.0f, damage, 2);
        Assert.Equal(80, defense);
    }

    [Fact] public void TenModifierTypes_Exist()
        => Assert.Equal(10, Enum.GetValues<MonsterModifierType>().Length);
}

public class MonsterSpawnerTests
{
    [Theory]
    [InlineData(12, 12, 12)]  // 144/12 = 12
    [InlineData(6, 6, 3)]     // 36/12 = 3
    [InlineData(3, 3, 1)]     // 9/12 = 0, min 1
    [InlineData(24, 24, 48)]  // 576/12 = 48
    public void RoomBudget_ScalesWithArea(int w, int h, int expected)
        => Assert.Equal(expected, MonsterSpawner.GetRoomBudget(w, h));

    [Fact] public void RoomBudget_MinimumOne()
        => Assert.Equal(1, MonsterSpawner.GetRoomBudget(1, 1));

    [Fact] public void Rarity_Distribution()
    {
        int normal = 0, empowered = 0, named = 0;
        var rng = new Random(42);
        for (int i = 0; i < 50000; i++)
        {
            switch (MonsterSpawner.RollRarity(rng))
            {
                case MonsterRarity.Normal: normal++; break;
                case MonsterRarity.Empowered: empowered++; break;
                case MonsterRarity.Named: named++; break;
            }
        }
        Assert.InRange(normal / 50000f, 0.74f, 0.82f);     // ~78%
        Assert.InRange(empowered / 50000f, 0.17f, 0.24f);   // ~20%
        Assert.InRange(named / 50000f, 0.01f, 0.04f);       // ~2%
    }

    [Fact] public void HPMultiplier_ByRarity()
    {
        Assert.Equal(1.0f, MonsterSpawner.GetHPMultiplier(MonsterRarity.Normal));
        Assert.Equal(2.0f, MonsterSpawner.GetHPMultiplier(MonsterRarity.Empowered));
        Assert.Equal(3.0f, MonsterSpawner.GetHPMultiplier(MonsterRarity.Named));
    }

    [Fact] public void RewardMultiplier_ByRarity()
    {
        Assert.Equal(1.0f, MonsterSpawner.GetRewardMultiplier(MonsterRarity.Normal));
        Assert.Equal(1.5f, MonsterSpawner.GetRewardMultiplier(MonsterRarity.Empowered));
        Assert.Equal(3.0f, MonsterSpawner.GetRewardMultiplier(MonsterRarity.Named));
    }

    [Theory]
    [InlineData(MonsterRarity.Normal, 1, 0)]
    [InlineData(MonsterRarity.Empowered, 1, 1)]
    [InlineData(MonsterRarity.Named, 1, 1)]
    [InlineData(MonsterRarity.Named, 3, 2)]
    [InlineData(MonsterRarity.Named, 5, 3)]
    [InlineData(MonsterRarity.Named, 10, 3)] // capped at 3
    public void ModifierCount_ByRarityAndZone(MonsterRarity rarity, int zone, int expected)
        => Assert.Equal(expected, MonsterSpawner.GetModifierCount(rarity, zone));

    [Fact] public void ArchetypeMix_HasMelee()
    {
        var mix = MonsterSpawner.GetArchetypeMix(10, new Random(42));
        Assert.True(mix.ContainsKey(MonsterArchetype.Melee));
        Assert.True(mix[MonsterArchetype.Melee] >= 1);
    }

    [Fact] public void ArchetypeMix_SumsTobudget()
    {
        var mix = MonsterSpawner.GetArchetypeMix(10, new Random(42));
        int total = mix.Values.Sum();
        Assert.Equal(10, total);
    }

    [Fact] public void ArchetypeMix_BudgetOne_OneMelee()
    {
        var mix = MonsterSpawner.GetArchetypeMix(1, new Random(42));
        Assert.Single(mix);
        Assert.Equal(1, mix[MonsterArchetype.Melee]);
    }

    [Fact] public void ArchetypeMix_BudgetZero_Empty()
    {
        var mix = MonsterSpawner.GetArchetypeMix(0, new Random(42));
        Assert.Empty(mix);
    }
}
