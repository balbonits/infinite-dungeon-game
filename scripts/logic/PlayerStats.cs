using System;

namespace DungeonGame;

/// <summary>
/// Player stat block. STR/DEX/STA/INT with diminishing returns.
/// Pure logic — no Godot dependency. Testable with xUnit.
/// </summary>
public class StatBlock
{
    private const float DiminishingK = 100.0f;

    // Raw stat values (before diminishing returns)
    public int Str { get; set; }
    public int Dex { get; set; }
    public int Sta { get; set; }
    public int Int { get; set; }
    public int FreePoints { get; set; }

    /// <summary>Diminishing returns curve: raw * (K / (raw + K))</summary>
    public static float GetEffective(int raw) => raw * (DiminishingK / (raw + DiminishingK));

    // ─── Static derivations (COMBAT-01 §1 overlay path) ─────────────────
    //
    // Callers that need to fold equipment stat overlays into the DR curve
    // use these helpers with `raw = allocated + equipment` instead of the
    // instance properties below. The overlay must stack BEFORE the DR
    // curve per spec §1 (single effective value); these methods are the
    // single source of truth for the derivation formulas so the instance
    // properties (allocated-only) and the combat path (allocated+overlay)
    // can't diverge.

    public static float ComputeMeleeFlatBonus(int effectiveStr) => GetEffective(effectiveStr) * 1.5f;
    public static float ComputeMeleePercentBoost(int effectiveStr) => GetEffective(effectiveStr) * 0.8f;
    public static float ComputeAttackSpeedMultiplier(int effectiveDex) => 1.0f + GetEffective(effectiveDex) * 0.01f;
    public static float ComputeDodgeChance(int effectiveDex) => GetEffective(effectiveDex) * 0.005f;
    public static float ComputeSpellDamageMultiplier(int effectiveInt) => 1.0f + GetEffective(effectiveInt) * 0.012f;

    // --- Derived stats from STR (spec: stats.md) ---
    // flat_melee_bonus = effective_str * 1.5
    public float MeleeFlatBonus => GetEffective(Str) * 1.5f;
    // percent_melee_boost = effective_str * 0.8%
    public float MeleePercentBoost => GetEffective(Str) * 0.8f;

    // --- Derived stats from DEX (spec: stats.md) ---
    // attack_speed_bonus = effective_dex * 1.0%
    public float AttackSpeedMultiplier => 1.0f + GetEffective(Dex) * 0.01f;
    // dodge_chance = effective_dex * 0.5%
    public float DodgeChance => GetEffective(Dex) * 0.005f;

    // --- Derived stats from STA (spec: stats.md) ---
    // bonus_max_hp = effective_sta * 5.0
    public int BonusMaxHp => (int)(GetEffective(Sta) * 5.0f);
    // hp_regen_per_sec = effective_sta * 0.15
    public float HpRegen => GetEffective(Sta) * 0.15f;

    // --- Derived stats from INT (spec: stats.md) ---
    // bonus_max_mana = effective_int * 4.0
    public int BonusMaxMana => (int)(GetEffective(Int) * 4.0f);
    // mana_regen_per_sec = effective_int * 0.2
    public float ManaRegen => GetEffective(Int) * 0.2f;
    // processing_efficiency = effective_int * 0.6% (mana cost reduction)
    public float ProcessingEfficiency => GetEffective(Int) * 0.006f;
    // spell_damage_bonus = effective_int * 1.2%
    public float SpellDamageMultiplier => 1.0f + GetEffective(Int) * 0.012f;

    /// <summary>
    /// Apply per-level class stat bonuses.
    /// </summary>
    public void ApplyClassLevelBonus(PlayerClass playerClass)
    {
        switch (playerClass)
        {
            case PlayerClass.Warrior:
                Str += 3; Sta += 2;
                break;
            case PlayerClass.Ranger:
                Dex += 3; Str += 1; Sta += 1;
                break;
            case PlayerClass.Mage:
                Int += 3; Sta += 1; Dex += 1;
                break;
        }
        FreePoints += 3; // 3 free stat points per level
    }

    public void Reset()
    {
        Str = 0; Dex = 0; Sta = 0; Int = 0;
        FreePoints = 0;
    }
}
