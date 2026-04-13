using GdUnit4;
using static GdUnit4.Assertions;

namespace DungeonGame.Tests.E2E.Mechanics;

// ── Movement ──────────────────────────────────────────────────────────────────

[TestSuite]
public class MovementSandboxTests
{
    [TestCase]
    [RequireGodotRuntime]
    public void DirectionalSprite_AllEightDirections_CorrectlyMapped()
    {
        var cases = new (Godot.Vector2 Input, string Expected)[]
        {
            (new Godot.Vector2(1, 0),                "east"),
            (new Godot.Vector2(-1, 0),               "west"),
            (new Godot.Vector2(0, -1),               "north"),
            (new Godot.Vector2(0, 1),                "south"),
            (new Godot.Vector2(1, 1).Normalized(),   "south-east"),
            (new Godot.Vector2(-1, 1).Normalized(),  "south-west"),
            (new Godot.Vector2(1, -1).Normalized(),  "north-east"),
            (new Godot.Vector2(-1,-1).Normalized(),  "north-west"),
        };

        foreach (var (input, expected) in cases)
        {
            string got = DirectionalSprite.GetDirection(input * 100f);
            AssertThat(got).IsEqual(expected);
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void DirectionalSprite_ZeroInput_ReturnsSouth()
    {
        AssertThat(DirectionalSprite.GetDirection(Godot.Vector2.Zero)).IsEqual("south");
    }
}

// ── Combat ────────────────────────────────────────────────────────────────────

[TestSuite]
public class CombatSandboxTests
{
    [TestCase]
    public void AllClasses_HaveValidPrimaryAttackConfig()
    {
        foreach (var cls in System.Enum.GetValues<PlayerClass>())
        {
            var cfg = ClassAttacks.GetPrimary(cls);
            AssertThat(cfg).IsNotNull();
            AssertThat(cfg!.Range).IsGreater(0f);
            AssertThat(cfg.Cooldown).IsGreater(0f);
            AssertThat(cfg.DamageMultiplier).IsGreater(0f);
        }
    }

    [TestCase]
    public void DamageFormula_ScalesWithMultiplier()
    {
        float baseDmg = 20f;
        var warrior = ClassAttacks.GetPrimary(PlayerClass.Warrior)!;
        var mage = ClassAttacks.GetPrimary(PlayerClass.Mage)!;

        float warriorDmg = baseDmg * warrior.DamageMultiplier;
        float mageDmg = baseDmg * mage.DamageMultiplier;

        AssertThat(warriorDmg).IsGreater(0f);
        AssertThat(mageDmg).IsGreater(0f);
    }
}

// ── Stats ─────────────────────────────────────────────────────────────────────

[TestSuite]
public class StatsSandboxTests
{
    [TestCase]
    public void StatBlock_DiminishingReturns_AtK()
    {
        AssertThat(StatBlock.GetEffective(100)).IsEqualApprox(50f, 0.01f);
    }

    [TestCase]
    public void StatBlock_AllClasses_BonusesStack()
    {
        foreach (var cls in System.Enum.GetValues<PlayerClass>())
        {
            var sb = new StatBlock();
            for (int i = 0; i < 5; i++)
                sb.ApplyClassLevelBonus(cls);
            AssertThat(sb.FreePoints).IsEqual(15);
        }
    }
}

// ── Enemy ─────────────────────────────────────────────────────────────────────

[TestSuite]
public class EnemySandboxTests
{
    [TestCase]
    public void AllSpecies_HaveValidConfigs()
    {
        foreach (var species in System.Enum.GetValues<EnemySpecies>())
        {
            var cfg = SpeciesDatabase.Get(species);
            AssertThat(cfg).IsNotNull($"{species}: config exists");
            if (cfg == null) continue;
            AssertThat(cfg.MoveSpeed).IsGreater(0f);
            AssertThat(cfg.BaseHp).IsGreater(0);
            AssertThat(cfg.CollisionRadius).IsGreater(0f);
        }
    }
}
