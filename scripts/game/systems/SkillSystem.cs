using System;
using System.Collections.Generic;

/// <summary>
/// Result of a skill execution attempt.
/// </summary>
public struct SkillResult
{
    public bool Success;
    public int Damage;
    public string Message;
}

/// <summary>
/// Skill execution, validation, and cooldown management.
/// All methods are static. UseSkill and CanUse take EntityData as first parameter.
/// </summary>
public static class SkillSystem
{
    /// <summary>
    /// Attempt to use a skill. Checks alive, MP, cooldown.
    /// On success: spends MP, sets cooldown, calculates damage (BaseDamage + INT * 2),
    /// applies via VitalSystem.TakeDamage.
    /// </summary>
    public static SkillResult UseSkill(EntityData caster, SkillData skill, EntityData target)
    {
        if (caster.IsDead)
            return new SkillResult { Success = false, Damage = 0, Message = "Caster is dead" };

        if (caster.MP < skill.ManaCost)
            return new SkillResult { Success = false, Damage = 0, Message = "Not enough MP" };

        if (skill.CooldownRemaining > 0)
            return new SkillResult { Success = false, Damage = 0, Message = "Skill is on cooldown" };

        // Spend MP
        caster.MP -= skill.ManaCost;

        // Set cooldown
        skill.CooldownRemaining = skill.Cooldown;

        // Calculate damage: skill base + INT scaling
        int damage = skill.BaseDamage + caster.INT * 2;

        // Apply damage to target
        VitalSystem.TakeDamage(target, damage);

        string killMsg = target.IsDead ? $" {target.Name} was defeated!" : "";
        return new SkillResult
        {
            Success = true,
            Damage = damage,
            Message = $"{skill.Name} deals {damage} damage!{killMsg}"
        };
    }

    /// <summary>
    /// Check whether a caster can use a skill right now.
    /// Requires: alive, enough MP, cooldown ready.
    /// </summary>
    public static bool CanUse(EntityData caster, SkillData skill)
    {
        if (caster.IsDead)
            return false;
        if (caster.MP < skill.ManaCost)
            return false;
        if (skill.CooldownRemaining > 0)
            return false;

        return true;
    }

    /// <summary>
    /// Reduce CooldownRemaining on all skills by delta time.
    /// Clamps to zero — cooldowns never go negative.
    /// </summary>
    public static void TickCooldowns(List<SkillData> skills, float delta)
    {
        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i].CooldownRemaining > 0)
                skills[i].CooldownRemaining = Math.Max(0, skills[i].CooldownRemaining - delta);
        }
    }
}
