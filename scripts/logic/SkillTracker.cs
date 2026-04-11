using System.Collections.Generic;
using System.Linq;

namespace DungeonGame;

/// <summary>
/// Manages all skill states for one player. Handles XP gain from use,
/// skill point allocation, and passive bonus calculation.
/// Pure logic — no Godot dependency. Testable with xUnit.
/// </summary>
public class SkillTracker
{
    private readonly Dictionary<string, SkillState> _states = new();
    private readonly PlayerClass _class;

    public int SkillPoints { get; set; }

    public SkillTracker(PlayerClass playerClass)
    {
        _class = playerClass;
        InitializeSkills();
    }

    private void InitializeSkills()
    {
        foreach (var def in SkillDatabase.GetByClass(_class))
        {
            _states[def.Id] = new SkillState(def.Id);
        }
    }

    public SkillState? GetState(string skillId) => _states.GetValueOrDefault(skillId);

    public IEnumerable<SkillState> AllStates => _states.Values;

    /// <summary>
    /// Record a skill use in combat. Awards XP to both the specific skill
    /// and its parent base skill (base skills gain XP when any child is used).
    /// </summary>
    public void RecordUse(string skillId, int floorNumber)
    {
        var def = SkillDatabase.Get(skillId);
        if (def == null || def.Class != _class) return;

        float floorMultiplier = 1 + (floorNumber - 1) * 0.5f;

        // Award XP to the used skill
        if (_states.TryGetValue(skillId, out var state))
        {
            int xp = (int)(def.BaseXpPerUse * floorMultiplier);
            state.AddXp(xp);
        }

        // If it's a specific skill, also award XP to the parent base skill
        if (def.Type == SkillType.Specific && def.ParentBaseSkillId != null)
        {
            var parentDef = SkillDatabase.Get(def.ParentBaseSkillId);
            if (parentDef != null && _states.TryGetValue(def.ParentBaseSkillId, out var parentState))
            {
                int parentXp = (int)(parentDef.BaseXpPerUse * floorMultiplier);
                parentState.AddXp(parentXp);
            }
        }
    }

    /// <summary>
    /// Allocate a skill point to a specific skill. Returns true if successful.
    /// </summary>
    public bool AllocatePoint(string skillId)
    {
        if (SkillPoints <= 0) return false;

        var def = SkillDatabase.Get(skillId);
        if (def == null || def.Class != _class) return false;

        // Specific skills require parent base skill at level 1+
        if (def.Type == SkillType.Specific && def.ParentBaseSkillId != null)
        {
            var parentState = GetState(def.ParentBaseSkillId);
            if (parentState == null || parentState.Level < 1) return false;
        }

        if (!_states.TryGetValue(skillId, out var state)) return false;

        SkillPoints--;
        state.AddSkillPoint();
        return true;
    }

    /// <summary>
    /// Check if a specific skill is unlocked (parent base skill at level 1+).
    /// </summary>
    public bool IsUnlocked(string skillId)
    {
        var def = SkillDatabase.Get(skillId);
        if (def == null) return false;

        if (def.Type == SkillType.Base) return true; // Always available

        if (def.ParentBaseSkillId == null) return true;
        var parentState = GetState(def.ParentBaseSkillId);
        return parentState != null && parentState.Level >= 1;
    }

    /// <summary>
    /// Get total passive bonus of a given type from all base skills.
    /// </summary>
    public float GetTotalPassiveBonus(PassiveBonusType type)
    {
        float total = 0;
        foreach (var def in SkillDatabase.GetByClass(_class))
        {
            if (def.Type != SkillType.Base || def.PassiveType != type) continue;
            var state = GetState(def.Id);
            if (state == null || state.Level <= 0) continue;
            total += state.GetPassiveBonus(def.PassiveMultiplier);
        }
        return total;
    }

    /// <summary>
    /// Serialize skill states for saving.
    /// </summary>
    public SavedSkillState[] CaptureStates()
    {
        return _states.Values
            .Where(s => s.Level > 0 || s.Xp > 0)
            .Select(s => new SavedSkillState { SkillId = s.SkillId, Level = s.Level, Xp = s.Xp })
            .ToArray();
    }

    /// <summary>
    /// Restore skill states from save data.
    /// </summary>
    public void RestoreStates(SavedSkillState[] saved)
    {
        foreach (var s in saved)
        {
            if (_states.TryGetValue(s.SkillId, out var state))
                state.SetState(s.Level, s.Xp);
        }
    }
}
