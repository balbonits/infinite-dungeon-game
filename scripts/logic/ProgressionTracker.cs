using System.Collections.Generic;
using System.Linq;

namespace DungeonGame;

/// <summary>
/// Manages all mastery and ability states for one player.
/// Handles XP gain, SP/AP allocation, use tracking, and category AP.
/// Pure logic — no Godot dependency. Testable with xUnit.
/// </summary>
public class ProgressionTracker
{
    private readonly Dictionary<string, MasteryState> _masteries = new();
    private readonly Dictionary<string, AbilityState> _abilities = new();
    private readonly Dictionary<string, int> _categoryUseCounts = new();
    private readonly Dictionary<string, int> _categoryApSpent = new();
    private readonly PlayerClass _class;

    public int SkillPoints { get; set; }
    public int AbilityPoints { get; set; }

    public ProgressionTracker(PlayerClass playerClass)
    {
        _class = playerClass;
        Initialize();
    }

    private void Initialize()
    {
        // Class masteries + abilities
        foreach (var def in SkillAbilityDatabase.GetMasteriesByClass(_class))
            _masteries[def.Id] = new MasteryState(def.Id);
        foreach (var def in SkillAbilityDatabase.GetAbilitiesByClass(_class))
            _abilities[def.Id] = new AbilityState(def.Id);

        // Innate masteries (available to all classes)
        foreach (var def in SkillAbilityDatabase.GetInnateMasteries())
            _masteries[def.Id] = new MasteryState(def.Id);
    }

    // --- State Access ---

    public MasteryState? GetMastery(string id) => _masteries.GetValueOrDefault(id);
    public AbilityState? GetAbility(string id) => _abilities.GetValueOrDefault(id);
    public IEnumerable<MasteryState> AllMasteries => _masteries.Values;
    public IEnumerable<AbilityState> AllAbilities => _abilities.Values;

    // --- Ability Use (Combat) ---

    /// <summary>
    /// Record an ability use in combat. Awards XP to ability + parent mastery.
    /// Increments use count (for affinity) and category counter (for category AP).
    /// </summary>
    public void RecordAbilityUse(string abilityId, int floorNumber)
    {
        var def = SkillAbilityDatabase.GetAbility(abilityId);
        if (def == null || def.Class != _class) return;

        float floorMultiplier = 1 + (floorNumber - 1) * 0.5f;

        // XP to ability
        if (_abilities.TryGetValue(abilityId, out var abilityState))
        {
            int xp = (int)(def.BaseXpPerUse * floorMultiplier);
            abilityState.AddXp(xp);
            abilityState.IncrementUse();
        }

        // XP to parent mastery
        var masteryDef = SkillAbilityDatabase.GetMastery(def.ParentMasteryId);
        if (masteryDef != null && _masteries.TryGetValue(def.ParentMasteryId, out var masteryState))
        {
            int masteryXp = (int)(masteryDef.BaseXpPerUse * floorMultiplier);
            masteryState.AddXp(masteryXp);
        }

        // Category use counter (for per-category AP)
        var categoryId = def.CategoryId;
        _categoryUseCounts.TryAdd(categoryId, 0);
        _categoryUseCounts[categoryId]++;
    }

    // --- SP Allocation (Masteries Only) ---

    /// <summary>
    /// Allocate one SP to a mastery. Returns true if successful.
    /// SP can only be spent on masteries, not abilities.
    /// </summary>
    public bool AllocateSP(string masteryId)
    {
        if (SkillPoints <= 0) return false;

        var def = SkillAbilityDatabase.GetMastery(masteryId);
        if (def == null) return false;
        // Must be a class mastery or innate
        if (def.Class != _class && def.CategoryId != "innate") return false;

        if (!_masteries.TryGetValue(masteryId, out var state)) return false;

        SkillPoints--;
        state.AddSkillPoint();
        return true;
    }

    // --- AP Allocation (Abilities Only) ---

    /// <summary>
    /// Allocate one AP to an ability. Returns true if successful.
    /// Requires parent mastery at level 1+.
    /// </summary>
    public bool AllocateAP(string abilityId)
    {
        if (AbilityPoints <= 0) return false;

        var def = SkillAbilityDatabase.GetAbility(abilityId);
        if (def == null || def.Class != _class) return false;

        if (!IsUnlocked(abilityId)) return false;
        if (!_abilities.TryGetValue(abilityId, out var state)) return false;

        AbilityPoints--;
        state.AddAbilityPoint();
        return true;
    }

    /// <summary>
    /// Allocate one category-earned AP to an ability within that category.
    /// Category AP is earned by using abilities (1 AP per 100 uses in a category).
    /// </summary>
    public bool AllocateCategoryAP(string abilityId)
    {
        var def = SkillAbilityDatabase.GetAbility(abilityId);
        if (def == null || def.Class != _class) return false;
        if (!IsUnlocked(abilityId)) return false;

        var categoryId = def.CategoryId;
        if (GetCategoryApAvailable(categoryId) <= 0) return false;
        if (!_abilities.TryGetValue(abilityId, out var state)) return false;

        _categoryApSpent.TryAdd(categoryId, 0);
        _categoryApSpent[categoryId]++;
        state.AddAbilityPoint();
        return true;
    }

    // --- Category AP ---

    /// <summary>Total category AP earned (1 per 100 ability uses in category).</summary>
    public int GetCategoryApEarned(string categoryId)
    {
        return _categoryUseCounts.TryGetValue(categoryId, out var uses) ? uses / 100 : 0;
    }

    /// <summary>Category AP available to spend (earned minus spent).</summary>
    public int GetCategoryApAvailable(string categoryId)
    {
        int earned = GetCategoryApEarned(categoryId);
        int spent = _categoryApSpent.TryGetValue(categoryId, out var s) ? s : 0;
        return earned - spent;
    }

    // --- Unlock Check ---

    /// <summary>
    /// Check if an ability is unlocked (parent mastery at level 1+).
    /// </summary>
    public bool IsUnlocked(string abilityId)
    {
        var def = SkillAbilityDatabase.GetAbility(abilityId);
        if (def == null) return false;

        var masteryState = GetMastery(def.ParentMasteryId);
        return masteryState != null && masteryState.Level >= 1;
    }

    // --- Passive Bonuses ---

    /// <summary>
    /// Get total passive bonus of a given type from all masteries.
    /// </summary>
    public float GetTotalPassiveBonus(PassiveBonusType type)
    {
        float total = 0;
        foreach (var (id, state) in _masteries)
        {
            if (state.Level <= 0) continue;
            var def = SkillAbilityDatabase.GetMastery(id);
            if (def == null || def.PassiveType != type) continue;
            total += state.GetPassiveBonus(def.PassiveMultiplier);
        }
        return total;
    }

    // --- Save/Load ---

    public SavedMasteryState[] CaptureMasteries()
    {
        return _masteries.Values
            .Where(s => s.Level > 0 || s.Xp > 0)
            .Select(s => new SavedMasteryState { MasteryId = s.MasteryId, Level = s.Level, Xp = s.Xp })
            .ToArray();
    }

    public SavedAbilityState[] CaptureAbilities()
    {
        return _abilities.Values
            .Where(s => s.Level > 0 || s.Xp > 0 || s.UseCount > 0)
            .Select(s => new SavedAbilityState { AbilityId = s.AbilityId, Level = s.Level, Xp = s.Xp, UseCount = s.UseCount })
            .ToArray();
    }

    public Dictionary<string, int> CaptureCategoryUseCounts() => new(_categoryUseCounts);
    public Dictionary<string, int> CaptureCategoryApSpent() => new(_categoryApSpent);

    public void RestoreMasteries(SavedMasteryState[]? saved)
    {
        if (saved == null) return;
        foreach (var s in saved)
        {
            if (_masteries.TryGetValue(s.MasteryId, out var state))
                state.SetState(s.Level, s.Xp);
        }
    }

    public void RestoreAbilities(SavedAbilityState[]? saved)
    {
        if (saved == null) return;
        foreach (var s in saved)
        {
            if (_abilities.TryGetValue(s.AbilityId, out var state))
                state.SetState(s.Level, s.Xp, s.UseCount);
        }
    }

    public void RestoreCategoryTracking(Dictionary<string, int>? useCounts, Dictionary<string, int>? apSpent)
    {
        if (useCounts != null)
            foreach (var kv in useCounts)
                _categoryUseCounts[kv.Key] = kv.Value;
        if (apSpent != null)
            foreach (var kv in apSpent)
                _categoryApSpent[kv.Key] = kv.Value;
    }
}

// --- Save data records ---

public record SavedMasteryState
{
    public string MasteryId { get; init; } = "";
    public int Level { get; init; }
    public int Xp { get; init; }
}

public record SavedAbilityState
{
    public string AbilityId { get; init; } = "";
    public int Level { get; init; }
    public int Xp { get; init; }
    public int UseCount { get; init; }
}
