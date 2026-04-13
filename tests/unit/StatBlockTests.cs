using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

/// <summary>
/// Tests for StatBlock (PlayerStats.cs).
/// Covers diminishing returns curve, all derived stats, class bonuses, and reset.
/// </summary>
public class StatBlockTests
{
    // ── GetEffective (diminishing returns) ───────────────────────────────────

    [Fact]
    public void GetEffective_ZeroStat_ReturnsZero()
    {
        StatBlock.GetEffective(0).Should().Be(0f);
    }

    [Fact]
    public void GetEffective_AtK_ReturnsHalfOfRaw()
    {
        // raw=100, K=100 → 100 * (100/200) = 50
        StatBlock.GetEffective(100).Should().BeApproximately(50f, 0.01f);
    }

    [Fact]
    public void GetEffective_AlwaysLessThanRaw()
    {
        foreach (var raw in new[] { 1, 10, 50, 100, 200, 500 })
            StatBlock.GetEffective(raw).Should().BeLessThan(raw);
    }

    [Fact]
    public void GetEffective_Increases_WithHigherRaw()
    {
        StatBlock.GetEffective(10).Should().BeLessThan(StatBlock.GetEffective(20));
        StatBlock.GetEffective(50).Should().BeLessThan(StatBlock.GetEffective(100));
    }

    // ── STR derived stats ────────────────────────────────────────────────────

    [Fact]
    public void MeleeFlatBonus_IsEffectiveStr_Times1Point5()
    {
        var sb = new StatBlock { Str = 50 };
        sb.MeleeFlatBonus.Should().BeApproximately(StatBlock.GetEffective(50) * 1.5f, 0.01f);
    }

    [Fact]
    public void MeleePercentBoost_IsEffectiveStr_Times0Point8()
    {
        var sb = new StatBlock { Str = 50 };
        sb.MeleePercentBoost.Should().BeApproximately(StatBlock.GetEffective(50) * 0.8f, 0.01f);
    }

    [Fact]
    public void MeleeFlatBonus_ZeroStr_ReturnsZero()
    {
        new StatBlock().MeleeFlatBonus.Should().Be(0f);
    }

    // ── DEX derived stats ────────────────────────────────────────────────────

    [Fact]
    public void AttackSpeedMultiplier_BaseIs1_WithZeroDex()
    {
        new StatBlock().AttackSpeedMultiplier.Should().BeApproximately(1.0f, 0.001f);
    }

    [Fact]
    public void AttackSpeedMultiplier_IncreasesWithDex()
    {
        var low = new StatBlock { Dex = 10 };
        var high = new StatBlock { Dex = 50 };
        high.AttackSpeedMultiplier.Should().BeGreaterThan(low.AttackSpeedMultiplier);
    }

    [Fact]
    public void DodgeChance_ZeroDex_ReturnsZero()
    {
        new StatBlock().DodgeChance.Should().Be(0f);
    }

    [Fact]
    public void DodgeChance_IsEffectiveDex_Times0Point005()
    {
        var sb = new StatBlock { Dex = 40 };
        sb.DodgeChance.Should().BeApproximately(StatBlock.GetEffective(40) * 0.005f, 0.001f);
    }

    // ── STA derived stats ────────────────────────────────────────────────────

    [Fact]
    public void BonusMaxHp_ZeroSta_ReturnsZero()
    {
        new StatBlock().BonusMaxHp.Should().Be(0);
    }

    [Fact]
    public void BonusMaxHp_IsEffectiveSta_Times5()
    {
        var sb = new StatBlock { Sta = 20 };
        int expected = (int)(StatBlock.GetEffective(20) * 5.0f);
        sb.BonusMaxHp.Should().Be(expected);
    }

    [Fact]
    public void HpRegen_ZeroSta_ReturnsZero()
    {
        new StatBlock().HpRegen.Should().Be(0f);
    }

    // ── INT derived stats ────────────────────────────────────────────────────

    [Fact]
    public void BonusMaxMana_ZeroInt_ReturnsZero()
    {
        new StatBlock().BonusMaxMana.Should().Be(0);
    }

    [Fact]
    public void SpellDamageMultiplier_BaseIs1_WithZeroInt()
    {
        new StatBlock().SpellDamageMultiplier.Should().BeApproximately(1.0f, 0.001f);
    }

    [Fact]
    public void SpellDamageMultiplier_IncreasesWithInt()
    {
        var low = new StatBlock { Int = 10 };
        var high = new StatBlock { Int = 100 };
        high.SpellDamageMultiplier.Should().BeGreaterThan(low.SpellDamageMultiplier);
    }

    [Fact]
    public void ProcessingEfficiency_IncreasesWithInt()
    {
        var low = new StatBlock { Int = 10 };
        var high = new StatBlock { Int = 50 };
        high.ProcessingEfficiency.Should().BeGreaterThan(low.ProcessingEfficiency);
    }

    [Fact]
    public void ManaRegen_IsEffectiveInt_Times0Point2()
    {
        var sb = new StatBlock { Int = 30 };
        sb.ManaRegen.Should().BeApproximately(StatBlock.GetEffective(30) * 0.2f, 0.001f);
    }

    // ── ApplyClassLevelBonus ─────────────────────────────────────────────────

    [Fact]
    public void Warrior_LevelBonus_IncreasesStrAndSta()
    {
        var sb = new StatBlock();
        sb.ApplyClassLevelBonus(PlayerClass.Warrior);
        sb.Str.Should().Be(3);
        sb.Sta.Should().Be(2);
        sb.Dex.Should().Be(0);
        sb.Int.Should().Be(0);
    }

    [Fact]
    public void Ranger_LevelBonus_IncreasesDexStrSta()
    {
        var sb = new StatBlock();
        sb.ApplyClassLevelBonus(PlayerClass.Ranger);
        sb.Dex.Should().Be(3);
        sb.Str.Should().Be(1);
        sb.Sta.Should().Be(1);
        sb.Int.Should().Be(0);
    }

    [Fact]
    public void Mage_LevelBonus_IncreasesIntStaDex()
    {
        var sb = new StatBlock();
        sb.ApplyClassLevelBonus(PlayerClass.Mage);
        sb.Int.Should().Be(3);
        sb.Sta.Should().Be(1);
        sb.Dex.Should().Be(1);
        sb.Str.Should().Be(0);
    }

    [Fact]
    public void LevelBonus_AlwaysGives3FreePoints()
    {
        foreach (var cls in new[] { PlayerClass.Warrior, PlayerClass.Ranger, PlayerClass.Mage })
        {
            var sb = new StatBlock();
            sb.ApplyClassLevelBonus(cls);
            sb.FreePoints.Should().Be(3, because: $"{cls} should give 3 free points");
        }
    }

    [Fact]
    public void LevelBonus_Stacks_AcrossMultipleLevels()
    {
        var sb = new StatBlock();
        sb.ApplyClassLevelBonus(PlayerClass.Warrior);
        sb.ApplyClassLevelBonus(PlayerClass.Warrior);
        sb.Str.Should().Be(6);
        sb.Sta.Should().Be(4);
        sb.FreePoints.Should().Be(6);
    }

    // ── Reset ────────────────────────────────────────────────────────────────

    [Fact]
    public void Reset_ClearsAllStats()
    {
        var sb = new StatBlock { Str = 10, Dex = 5, Sta = 8, Int = 12, FreePoints = 3 };
        sb.Reset();
        sb.Str.Should().Be(0);
        sb.Dex.Should().Be(0);
        sb.Sta.Should().Be(0);
        sb.Int.Should().Be(0);
        sb.FreePoints.Should().Be(0);
    }
}
