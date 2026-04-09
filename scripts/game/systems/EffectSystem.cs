using System;
using System.Collections.Generic;

/// <summary>
/// Status effect management for any entity.
/// All methods are static and take EntityData as first parameter.
/// Effects do not stack — reapplying the same type refreshes duration.
/// </summary>
public static class EffectSystem
{
    /// <summary>
    /// Apply an effect to an entity. If the same EffectType already exists,
    /// refresh its duration instead of stacking.
    /// </summary>
    public static void Apply(EntityData entity, EffectData effect)
    {
        for (int i = 0; i < entity.Effects.Count; i++)
        {
            if (entity.Effects[i].Data.Type == effect.Type)
            {
                // Replace existing effect with fresh one
                entity.Effects[i] = new ActiveEffect(effect);
                return;
            }
        }

        entity.Effects.Add(new ActiveEffect(effect));
    }

    /// <summary>
    /// Remove an effect of the given type. Returns true if found and removed.
    /// </summary>
    public static bool Remove(EntityData entity, EffectType type)
    {
        for (int i = 0; i < entity.Effects.Count; i++)
        {
            if (entity.Effects[i].Data.Type == type)
            {
                entity.Effects.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Remove all active effects from an entity.
    /// </summary>
    public static void RemoveAll(EntityData entity)
    {
        entity.Effects.Clear();
    }

    /// <summary>
    /// Check whether the entity currently has an effect of the given type.
    /// </summary>
    public static bool HasEffect(EntityData entity, EffectType type)
    {
        for (int i = 0; i < entity.Effects.Count; i++)
        {
            if (entity.Effects[i].Data.Type == type)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Process all active effects for one frame. Reduces durations, triggers
    /// tick-based effects (Poison/Regen), and removes expired effects.
    /// Returns a list of event messages for the UI/log.
    /// </summary>
    public static List<string> Tick(EntityData entity, float delta)
    {
        var messages = new List<string>();

        for (int i = entity.Effects.Count - 1; i >= 0; i--)
        {
            var active = entity.Effects[i];
            active.RemainingDuration -= delta;

            // Check expiration
            if (active.RemainingDuration <= 0)
            {
                entity.Effects.RemoveAt(i);
                messages.Add($"{active.Data.Type} wore off");
                continue;
            }

            // Process tick-based effects
            if (active.Data.TickInterval > 0)
            {
                active.TimeSinceLastTick += delta;

                if (active.TimeSinceLastTick >= active.Data.TickInterval)
                {
                    active.TimeSinceLastTick -= active.Data.TickInterval;

                    switch (active.Data.Type)
                    {
                        case EffectType.Poison:
                            VitalSystem.TakeDamage(entity, active.Data.Magnitude);
                            messages.Add($"Poison deals {active.Data.Magnitude} damage");
                            break;

                        case EffectType.Regen:
                            VitalSystem.Heal(entity, active.Data.Magnitude);
                            messages.Add($"Regen restores {active.Data.Magnitude} HP");
                            break;

                        // StatBuff, StatDebuff, Stun, Slow, Haste, DamageBoost,
                        // DefenseBoost are passive — no per-tick action needed.
                    }
                }
            }
        }

        return messages;
    }
}
