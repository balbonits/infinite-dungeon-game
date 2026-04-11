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

    // --- Derived stats from STR ---
    /// <summary>Flat melee damage bonus from STR.</summary>
    public float MeleeFlatBonus => GetEffective(Str) * 1.5f;
    /// <summary>Percentage melee damage boost from STR.</summary>
    public float MeleePercentBoost => GetEffective(Str) * 0.8f;

    // --- Derived stats from DEX ---
    /// <summary>Attack speed multiplier from DEX (1.0 = base speed).</summary>
    public float AttackSpeedMultiplier => 1.0f + GetEffective(Dex) * 0.005f;
    /// <summary>Dodge chance from DEX (0-1).</summary>
    public float DodgeChance => MathF.Min(0.4f, GetEffective(Dex) * 0.003f);

    // --- Derived stats from STA ---
    /// <summary>Bonus max HP from STA.</summary>
    public int BonusMaxHp => (int)(GetEffective(Sta) * 3.0f);
    /// <summary>HP regen per second from STA.</summary>
    public float HpRegen => GetEffective(Sta) * 0.1f;

    // --- Derived stats from INT ---
    /// <summary>Bonus max mana from INT.</summary>
    public int BonusMaxMana => (int)(GetEffective(Int) * 2.5f);
    /// <summary>Mana regen per second from INT.</summary>
    public float ManaRegen => GetEffective(Int) * 0.15f;
    /// <summary>Spell damage multiplier from INT.</summary>
    public float SpellDamageMultiplier => 1.0f + GetEffective(Int) * 0.01f;

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
