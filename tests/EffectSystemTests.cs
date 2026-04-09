using System.Collections.Generic;
using Xunit;

namespace DungeonGame.Tests;

public class EffectSystemTests
{
    private EntityData MakeEntity(int hp = 100, int maxHp = 100)
    {
        return new EntityData
        {
            Id = "test",
            Name = "TestEntity",
            Type = EntityType.Player,
            HP = hp,
            MaxHP = maxHp,
            MP = 50,
            MaxMP = 50,
        };
    }

    private EffectData MakeEffect(EffectType type, int magnitude = 5, float duration = 10f,
        float tickInterval = 1f, string source = "test")
    {
        return new EffectData
        {
            Type = type,
            Magnitude = magnitude,
            Duration = duration,
            TickInterval = tickInterval,
            Source = source,
        };
    }

    // ═══════════════════════════════════════════════════════════════
    //  Apply
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Apply_AddsEffectToEntity()
    {
        var e = MakeEntity();
        var effect = MakeEffect(EffectType.Poison);

        EffectSystem.Apply(e, effect);

        Assert.Single(e.Effects);
        Assert.Equal(EffectType.Poison, e.Effects[0].Data.Type);
    }

    [Fact]
    public void Apply_SameType_RefreshesDuration_DoesNotStack()
    {
        var e = MakeEntity();
        var poison1 = MakeEffect(EffectType.Poison, magnitude: 5, duration: 10f);
        var poison2 = MakeEffect(EffectType.Poison, magnitude: 8, duration: 15f);

        EffectSystem.Apply(e, poison1);
        Assert.Single(e.Effects);
        Assert.Equal(10f, e.Effects[0].RemainingDuration);

        EffectSystem.Apply(e, poison2);
        Assert.Single(e.Effects); // still just one
        Assert.Equal(15f, e.Effects[0].RemainingDuration);
        Assert.Equal(8, e.Effects[0].Data.Magnitude); // updated to new effect's magnitude
    }

    [Fact]
    public void Apply_DifferentTypes_BothCoexist()
    {
        var e = MakeEntity();
        EffectSystem.Apply(e, MakeEffect(EffectType.Poison));
        EffectSystem.Apply(e, MakeEffect(EffectType.Regen));

        Assert.Equal(2, e.Effects.Count);
    }

    [Fact]
    public void Apply_ManyDifferentTypes_AllCoexist()
    {
        var e = MakeEntity();
        EffectSystem.Apply(e, MakeEffect(EffectType.Poison));
        EffectSystem.Apply(e, MakeEffect(EffectType.Regen));
        EffectSystem.Apply(e, MakeEffect(EffectType.Stun));
        EffectSystem.Apply(e, MakeEffect(EffectType.Slow));
        EffectSystem.Apply(e, MakeEffect(EffectType.Haste));

        Assert.Equal(5, e.Effects.Count);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Remove
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Remove_ExistingType_ReturnsTrue()
    {
        var e = MakeEntity();
        EffectSystem.Apply(e, MakeEffect(EffectType.Poison));

        bool removed = EffectSystem.Remove(e, EffectType.Poison);

        Assert.True(removed);
        Assert.Empty(e.Effects);
    }

    [Fact]
    public void Remove_NonExistent_ReturnsFalse()
    {
        var e = MakeEntity();

        bool removed = EffectSystem.Remove(e, EffectType.Poison);

        Assert.False(removed);
    }

    [Fact]
    public void Remove_OnlyRemovesTargetType()
    {
        var e = MakeEntity();
        EffectSystem.Apply(e, MakeEffect(EffectType.Poison));
        EffectSystem.Apply(e, MakeEffect(EffectType.Regen));

        EffectSystem.Remove(e, EffectType.Poison);

        Assert.Single(e.Effects);
        Assert.Equal(EffectType.Regen, e.Effects[0].Data.Type);
    }

    // ═══════════════════════════════════════════════════════════════
    //  RemoveAll
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void RemoveAll_ClearsAllEffects()
    {
        var e = MakeEntity();
        EffectSystem.Apply(e, MakeEffect(EffectType.Poison));
        EffectSystem.Apply(e, MakeEffect(EffectType.Regen));
        EffectSystem.Apply(e, MakeEffect(EffectType.Stun));

        EffectSystem.RemoveAll(e);

        Assert.Empty(e.Effects);
    }

    [Fact]
    public void RemoveAll_OnEmpty_NoError()
    {
        var e = MakeEntity();
        EffectSystem.RemoveAll(e); // should not throw
        Assert.Empty(e.Effects);
    }

    // ═══════════════════════════════════════════════════════════════
    //  HasEffect
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void HasEffect_WhenPresent_ReturnsTrue()
    {
        var e = MakeEntity();
        EffectSystem.Apply(e, MakeEffect(EffectType.Poison));

        Assert.True(EffectSystem.HasEffect(e, EffectType.Poison));
    }

    [Fact]
    public void HasEffect_WhenAbsent_ReturnsFalse()
    {
        var e = MakeEntity();
        Assert.False(EffectSystem.HasEffect(e, EffectType.Poison));
    }

    [Fact]
    public void HasEffect_AfterRemoval_ReturnsFalse()
    {
        var e = MakeEntity();
        EffectSystem.Apply(e, MakeEffect(EffectType.Poison));
        EffectSystem.Remove(e, EffectType.Poison);

        Assert.False(EffectSystem.HasEffect(e, EffectType.Poison));
    }

    // ═══════════════════════════════════════════════════════════════
    //  Tick — Poison
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Tick_Poison_DealsDamageOnInterval()
    {
        var e = MakeEntity(hp: 100);
        var poison = MakeEffect(EffectType.Poison, magnitude: 10, duration: 5f, tickInterval: 1f);
        EffectSystem.Apply(e, poison);

        // Tick 1 second — should trigger one poison tick
        var msgs = EffectSystem.Tick(e, 1.0f);

        Assert.Equal(90, e.HP);
        Assert.Contains(msgs, m => m.Contains("Poison deals 10 damage"));
    }

    [Fact]
    public void Tick_Poison_NoTickBeforeInterval()
    {
        var e = MakeEntity(hp: 100);
        var poison = MakeEffect(EffectType.Poison, magnitude: 10, duration: 5f, tickInterval: 2f);
        EffectSystem.Apply(e, poison);

        // Tick 1 second — interval is 2s, so no tick yet
        EffectSystem.Tick(e, 1.0f);

        Assert.Equal(100, e.HP);
    }

    [Fact]
    public void Tick_Poison_MultipleTicks()
    {
        var e = MakeEntity(hp: 100);
        var poison = MakeEffect(EffectType.Poison, magnitude: 5, duration: 10f, tickInterval: 1f);
        EffectSystem.Apply(e, poison);

        // Tick 3 times
        EffectSystem.Tick(e, 1.0f); // HP: 100 -> 95
        EffectSystem.Tick(e, 1.0f); // HP: 95 -> 90
        EffectSystem.Tick(e, 1.0f); // HP: 90 -> 85

        Assert.Equal(85, e.HP);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Tick — Regen
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Tick_Regen_HealsOnInterval()
    {
        var e = MakeEntity(hp: 50, maxHp: 100);
        var regen = MakeEffect(EffectType.Regen, magnitude: 10, duration: 5f, tickInterval: 1f);
        EffectSystem.Apply(e, regen);

        var msgs = EffectSystem.Tick(e, 1.0f);

        Assert.Equal(60, e.HP);
        Assert.Contains(msgs, m => m.Contains("Regen restores 10 HP"));
    }

    [Fact]
    public void Tick_Regen_CapsAtMaxHP()
    {
        var e = MakeEntity(hp: 95, maxHp: 100);
        var regen = MakeEffect(EffectType.Regen, magnitude: 20, duration: 5f, tickInterval: 1f);
        EffectSystem.Apply(e, regen);

        EffectSystem.Tick(e, 1.0f);

        Assert.Equal(100, e.HP); // capped at MaxHP
    }

    // ═══════════════════════════════════════════════════════════════
    //  Tick — Expiration
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Tick_EffectExpires_AfterDuration()
    {
        var e = MakeEntity();
        var buff = MakeEffect(EffectType.StatBuff, duration: 3f, tickInterval: 0f);
        EffectSystem.Apply(e, buff);

        Assert.True(EffectSystem.HasEffect(e, EffectType.StatBuff));

        // Tick past the duration
        var msgs = EffectSystem.Tick(e, 3.5f);

        Assert.False(EffectSystem.HasEffect(e, EffectType.StatBuff));
        Assert.Contains(msgs, m => m.Contains("StatBuff wore off"));
    }

    [Fact]
    public void Tick_ExactDuration_Expires()
    {
        var e = MakeEntity();
        var buff = MakeEffect(EffectType.DamageBoost, duration: 5f, tickInterval: 0f);
        EffectSystem.Apply(e, buff);

        // Tick exactly the duration
        var msgs = EffectSystem.Tick(e, 5.0f);

        Assert.False(EffectSystem.HasEffect(e, EffectType.DamageBoost));
        Assert.Contains(msgs, m => m.Contains("DamageBoost wore off"));
    }

    [Fact]
    public void Tick_PartialDuration_DoesNotExpire()
    {
        var e = MakeEntity();
        var buff = MakeEffect(EffectType.DefenseBoost, duration: 5f, tickInterval: 0f);
        EffectSystem.Apply(e, buff);

        EffectSystem.Tick(e, 2.0f);

        Assert.True(EffectSystem.HasEffect(e, EffectType.DefenseBoost));
    }

    // ═══════════════════════════════════════════════════════════════
    //  Kill by poison tick
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Tick_Poison_KillsEntity()
    {
        var e = MakeEntity(hp: 5);
        var poison = MakeEffect(EffectType.Poison, magnitude: 10, duration: 10f, tickInterval: 1f);
        EffectSystem.Apply(e, poison);

        EffectSystem.Tick(e, 1.0f);

        Assert.Equal(0, e.HP);
        Assert.True(e.IsDead);
    }

    [Fact]
    public void Tick_Poison_ExactKill()
    {
        var e = MakeEntity(hp: 10);
        var poison = MakeEffect(EffectType.Poison, magnitude: 10, duration: 10f, tickInterval: 1f);
        EffectSystem.Apply(e, poison);

        EffectSystem.Tick(e, 1.0f);

        Assert.Equal(0, e.HP);
        Assert.True(e.IsDead);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Multiple effects ticking simultaneously
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Tick_MultipleEffects_AllProcess()
    {
        var e = MakeEntity(hp: 80, maxHp: 100);
        EffectSystem.Apply(e, MakeEffect(EffectType.Poison, magnitude: 5, duration: 10f, tickInterval: 1f));
        EffectSystem.Apply(e, MakeEffect(EffectType.Regen, magnitude: 3, duration: 10f, tickInterval: 1f));

        var msgs = EffectSystem.Tick(e, 1.0f);

        // Poison: -5, Regen: +3 => net -2
        Assert.Equal(78, e.HP);
        Assert.Equal(2, msgs.Count);
    }

    [Fact]
    public void Tick_MixedExpirationAndTick()
    {
        var e = MakeEntity(hp: 80, maxHp: 100);
        // Short duration buff that expires
        EffectSystem.Apply(e, MakeEffect(EffectType.StatBuff, duration: 1f, tickInterval: 0f));
        // Long duration poison that ticks
        EffectSystem.Apply(e, MakeEffect(EffectType.Poison, magnitude: 5, duration: 10f, tickInterval: 1f));

        var msgs = EffectSystem.Tick(e, 1.5f);

        // StatBuff expires (1.5 > 1.0)
        Assert.False(EffectSystem.HasEffect(e, EffectType.StatBuff));
        // Poison still active (1.5 < 10.0) and ticked
        Assert.True(EffectSystem.HasEffect(e, EffectType.Poison));
        Assert.Equal(75, e.HP);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Effect with 0 tick interval — no per-tick action
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Tick_ZeroTickInterval_NoPerTickAction()
    {
        var e = MakeEntity(hp: 100);
        var buff = MakeEffect(EffectType.DamageBoost, magnitude: 10, duration: 5f, tickInterval: 0f);
        EffectSystem.Apply(e, buff);

        var msgs = EffectSystem.Tick(e, 1.0f);

        Assert.Equal(100, e.HP); // no damage or heal
        Assert.Empty(msgs);      // no tick messages (only expiry messages)
    }

    // ═══════════════════════════════════════════════════════════════
    //  Rapid tick — small intervals, many ticks per frame
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Tick_SmallInterval_TicksOncePerInterval()
    {
        var e = MakeEntity(hp: 100);
        // Poison: 1 dmg every 0.5s, lasts 5s
        var poison = MakeEffect(EffectType.Poison, magnitude: 1, duration: 5f, tickInterval: 0.5f);
        EffectSystem.Apply(e, poison);

        // Tick 1.0s — should accumulate 0.5s twice but only tick once per threshold
        // The implementation: TimeSinceLastTick += 1.0, >= 0.5, so tick once, subtract 0.5,
        // TimeSinceLastTick = 0.5, but no second check in same frame
        var msgs = EffectSystem.Tick(e, 1.0f);

        Assert.Equal(99, e.HP); // one tick of 1 damage
        Assert.Single(msgs);
    }

    [Fact]
    public void Tick_ManySmallFrames_PoisonTicksCorrectly()
    {
        var e = MakeEntity(hp: 100);
        var poison = MakeEffect(EffectType.Poison, magnitude: 2, duration: 10f, tickInterval: 1.0f);
        EffectSystem.Apply(e, poison);

        // 10 small frames of 0.1s each = 1.0s total
        for (int i = 0; i < 10; i++)
            EffectSystem.Tick(e, 0.1f);

        // After 1.0s, one tick should have fired
        Assert.Equal(98, e.HP);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Messages
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Tick_Poison_CorrectMessage()
    {
        var e = MakeEntity(hp: 100);
        EffectSystem.Apply(e, MakeEffect(EffectType.Poison, magnitude: 7, duration: 5f, tickInterval: 1f));

        var msgs = EffectSystem.Tick(e, 1.0f);

        Assert.Single(msgs);
        Assert.Equal("Poison deals 7 damage", msgs[0]);
    }

    [Fact]
    public void Tick_Regen_CorrectMessage()
    {
        var e = MakeEntity(hp: 50, maxHp: 100);
        EffectSystem.Apply(e, MakeEffect(EffectType.Regen, magnitude: 3, duration: 5f, tickInterval: 1f));

        var msgs = EffectSystem.Tick(e, 1.0f);

        Assert.Single(msgs);
        Assert.Equal("Regen restores 3 HP", msgs[0]);
    }

    [Fact]
    public void Tick_Expiration_CorrectMessage()
    {
        var e = MakeEntity();
        EffectSystem.Apply(e, MakeEffect(EffectType.Stun, duration: 1f, tickInterval: 0f));

        var msgs = EffectSystem.Tick(e, 2f);

        Assert.Single(msgs);
        Assert.Equal("Stun wore off", msgs[0]);
    }

    [Fact]
    public void Tick_NoEffects_EmptyMessages()
    {
        var e = MakeEntity();
        var msgs = EffectSystem.Tick(e, 1.0f);
        Assert.Empty(msgs);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Stress — many effects ticking
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Stress_ManyEffects_AllProcess()
    {
        var e = MakeEntity(hp: 10000, maxHp: 10000);

        // Apply one of each effect type
        var types = new[] {
            EffectType.Poison, EffectType.Regen, EffectType.StatBuff,
            EffectType.StatDebuff, EffectType.Stun, EffectType.Slow,
            EffectType.Haste, EffectType.DamageBoost, EffectType.DefenseBoost
        };
        foreach (var t in types)
            EffectSystem.Apply(e, MakeEffect(t, magnitude: 1, duration: 10f, tickInterval: 1f));

        Assert.Equal(types.Length, e.Effects.Count);

        // Tick — should process all without error
        var msgs = EffectSystem.Tick(e, 1.0f);

        // Tick iterates in reverse order: Regen heals +1 first (capped at 10000),
        // then Poison deals -1. Net: 10000 - 1 = 9999.
        Assert.Equal(9999, e.HP);
        // Should have 2 messages: regen tick + poison tick
        Assert.Equal(2, msgs.Count);
    }

    [Fact]
    public void Stress_RapidApplyRemoveCycle()
    {
        var e = MakeEntity();

        for (int i = 0; i < 100; i++)
        {
            EffectSystem.Apply(e, MakeEffect(EffectType.Poison, duration: 1f));
            EffectSystem.Remove(e, EffectType.Poison);
        }

        Assert.Empty(e.Effects);
    }

    [Fact]
    public void Stress_ManyTickFrames_EffectsExpireCorrectly()
    {
        var e = MakeEntity(hp: 1000, maxHp: 1000);
        EffectSystem.Apply(e, MakeEffect(EffectType.Poison, magnitude: 1, duration: 3f, tickInterval: 1f));

        // Tick 50 frames at 0.1s each = 5.0s total
        int totalMessages = 0;
        for (int i = 0; i < 50; i++)
        {
            var msgs = EffectSystem.Tick(e, 0.1f);
            totalMessages += msgs.Count;
        }

        // Poison should have expired around frame 30 (3.0s)
        Assert.False(EffectSystem.HasEffect(e, EffectType.Poison));
        Assert.True(totalMessages > 0); // at least some ticks and the expiry message
    }

    // ═══════════════════════════════════════════════════════════════
    //  Multi-step scenario: buff → attack → verify → expire → attack → verify
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void MultiStep_BuffExpiresCorrectly()
    {
        var e = MakeEntity(hp: 100);

        // Apply a DamageBoost buff (passive — no tick action)
        var buff = MakeEffect(EffectType.DamageBoost, magnitude: 10, duration: 3f, tickInterval: 0f);
        EffectSystem.Apply(e, buff);

        Assert.True(EffectSystem.HasEffect(e, EffectType.DamageBoost));

        // Tick 2 seconds — buff still active
        EffectSystem.Tick(e, 2f);
        Assert.True(EffectSystem.HasEffect(e, EffectType.DamageBoost));

        // Tick 2 more seconds — buff expires (total 4s > 3s duration)
        var msgs = EffectSystem.Tick(e, 2f);
        Assert.False(EffectSystem.HasEffect(e, EffectType.DamageBoost));
        Assert.Contains(msgs, m => m.Contains("DamageBoost wore off"));
    }

    [Fact]
    public void MultiStep_PoisonThenRegen_NetEffect()
    {
        var e = MakeEntity(hp: 100, maxHp: 100);

        // Apply poison first
        EffectSystem.Apply(e, MakeEffect(EffectType.Poison, magnitude: 5, duration: 3f, tickInterval: 1f));
        EffectSystem.Tick(e, 1.0f); // -5 HP -> 95
        Assert.Equal(95, e.HP);

        // Apply regen — Tick iterates in reverse: regen (index 1) heals first,
        // then poison (index 0) damages. Net: +10 -5 = +5 from 95, but heal
        // caps at MaxHP(100), then poison hits: 100 - 5 = 95
        EffectSystem.Apply(e, MakeEffect(EffectType.Regen, magnitude: 10, duration: 3f, tickInterval: 1f));
        EffectSystem.Tick(e, 1.0f);
        Assert.Equal(95, e.HP);
    }
}
