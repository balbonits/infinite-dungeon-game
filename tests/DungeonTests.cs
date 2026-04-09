using Xunit;

namespace DungeonGame.Tests;

public class DungeonTests
{
    public DungeonTests()
    {
        GameState.Reset();
    }

    [Fact]
    public void EnterDungeon_SetsLocationAndFloor()
    {
        GameSystems.EnterDungeon();

        Assert.Equal(GameLocation.Dungeon, GameState.Location);
        Assert.Equal(1, GameState.DungeonFloor);
    }

    [Fact]
    public void ExitDungeon_ReturnToTown()
    {
        GameSystems.EnterDungeon();
        GameSystems.SpawnMonster("Rat", MonsterTier.Tier1);

        GameSystems.ExitDungeon();

        Assert.Equal(GameLocation.Town, GameState.Location);
        Assert.Equal(0, GameState.DungeonFloor);
        Assert.Empty(GameState.ActiveMonsters);
    }

    [Fact]
    public void SpawnMonster_CreatesWithCorrectStats()
    {
        GameSystems.EnterDungeon();
        var monster = GameSystems.SpawnMonster("Rat", MonsterTier.Tier1);

        Assert.Equal("Rat", monster.Name);
        Assert.Equal(MonsterTier.Tier1, monster.Tier);
        Assert.Equal(30, monster.HP); // Base Tier1 HP at floor 1
        Assert.Equal(30, monster.MaxHP);
        Assert.Equal(10, monster.XPReward); // Base Tier1 XP at floor 1
        Assert.False(monster.IsDead);
        Assert.Single(GameState.ActiveMonsters);
    }

    [Fact]
    public void SpawnMonster_ScalesWithFloor()
    {
        GameSystems.EnterDungeon();
        GameState.DungeonFloor = 10;

        var monster = GameSystems.SpawnMonster("Skeleton", MonsterTier.Tier2);

        // Floor multiplier: 1 + (10-1)*0.5 = 5.5
        // Base Tier2 HP: 42, scaled: 42 * 5.5 = 231
        Assert.Equal(231, monster.HP);
    }

    [Fact]
    public void SpawnMonster_Tier3HasHigherStats()
    {
        GameSystems.EnterDungeon();
        var t1 = GameSystems.SpawnMonster("Rat", MonsterTier.Tier1);
        var t3 = GameSystems.SpawnMonster("Boss", MonsterTier.Tier3);

        Assert.True(t3.HP > t1.HP);
        Assert.True(t3.XPReward > t1.XPReward);
        Assert.True(t3.GoldReward > t1.GoldReward);
    }
}

public class DeathRespawnTests
{
    public DeathRespawnTests()
    {
        GameState.Reset();
    }

    [Fact]
    public void PlayerDie_SetsDeadFlag()
    {
        GameSystems.PlayerDie();

        Assert.True(GameState.Player.IsDead);
    }

    [Fact]
    public void PlayerRespawn_RestoresHalfStats()
    {
        GameState.Player.MaxHP = 200;
        GameState.Player.MaxMP = 100;
        GameSystems.EnterDungeon();
        GameSystems.PlayerDie();

        GameSystems.PlayerRespawn();

        Assert.False(GameState.Player.IsDead);
        Assert.Equal(100, GameState.Player.HP); // half of 200
        Assert.Equal(50, GameState.Player.MP);  // half of 100
        Assert.Equal(GameLocation.Town, GameState.Location);
        Assert.Equal(0, GameState.DungeonFloor);
    }

    [Fact]
    public void PlayerRespawn_ClearsPoison()
    {
        GameSystems.ApplyPoison(5, 3);
        Assert.Equal(StatusEffect.Poison, GameState.Player.Status);

        GameSystems.PlayerRespawn();

        Assert.Equal(StatusEffect.None, GameState.Player.Status);
        Assert.Equal(0, GameState.Player.PoisonTicksLeft);
    }
}

public class StatusEffectTests
{
    public StatusEffectTests()
    {
        GameState.Reset();
    }

    [Fact]
    public void ApplyPoison_SetsStatus()
    {
        GameSystems.ApplyPoison(5, 3);

        Assert.Equal(StatusEffect.Poison, GameState.Player.Status);
        Assert.Equal(5, GameState.Player.PoisonDamagePerTick);
        Assert.Equal(3, GameState.Player.PoisonTicksLeft);
    }

    [Fact]
    public void TickPoison_DealsDamage()
    {
        GameSystems.ApplyPoison(5, 3);
        int startHp = GameState.Player.HP;

        int dmg = GameSystems.TickPoison();

        Assert.Equal(5, dmg);
        Assert.Equal(startHp - 5, GameState.Player.HP);
        Assert.Equal(2, GameState.Player.PoisonTicksLeft);
    }

    [Fact]
    public void TickPoison_ClearsAfterAllTicks()
    {
        GameSystems.ApplyPoison(5, 2);

        GameSystems.TickPoison();
        GameSystems.TickPoison();

        Assert.Equal(StatusEffect.None, GameState.Player.Status);
        Assert.Equal(0, GameState.Player.PoisonTicksLeft);
    }

    [Fact]
    public void TickPoison_ReturnsZeroWhenNotPoisoned()
    {
        int dmg = GameSystems.TickPoison();

        Assert.Equal(0, dmg);
    }
}

public class SkillTests
{
    public SkillTests()
    {
        GameState.Reset();
    }

    [Fact]
    public void UseSkill_CostsMana()
    {
        GameSystems.EnterDungeon();
        var monster = GameSystems.SpawnMonster("Rat", MonsterTier.Tier1);
        var skill = new SkillData { Name = "Slash", ManaCost = 15, BaseDamage = 25, Cooldown = 3f };
        int startMp = GameState.Player.MP;

        var (dmg, success) = GameSystems.UseSkill(skill, monster);

        Assert.True(success);
        Assert.Equal(startMp - 15, GameState.Player.MP);
        Assert.True(dmg > 0);
    }

    [Fact]
    public void UseSkill_FailsWithoutMana()
    {
        GameSystems.EnterDungeon();
        var monster = GameSystems.SpawnMonster("Rat", MonsterTier.Tier1);
        var skill = new SkillData { Name = "Slash", ManaCost = 15, BaseDamage = 25, Cooldown = 3f };
        GameState.Player.MP = 0;

        var (dmg, success) = GameSystems.UseSkill(skill, monster);

        Assert.False(success);
        Assert.Equal(0, dmg);
    }

    [Fact]
    public void UseSkill_FailsOnCooldown()
    {
        GameSystems.EnterDungeon();
        var monster = GameSystems.SpawnMonster("Rat", MonsterTier.Tier1);
        var skill = new SkillData { Name = "Slash", ManaCost = 15, BaseDamage = 25, Cooldown = 3f };
        skill.CooldownRemaining = 2f;

        var (_, success) = GameSystems.UseSkill(skill, monster);

        Assert.False(success);
    }

    [Fact]
    public void RegenMana_RestoresMp()
    {
        GameState.Player.MP = 30;
        GameState.Player.MaxMP = 65;

        int restored = GameSystems.RegenMana(10);

        Assert.Equal(10, restored);
        Assert.Equal(40, GameState.Player.MP);
    }

    [Fact]
    public void RegenMana_CapsAtMax()
    {
        GameState.Player.MP = 60;
        GameState.Player.MaxMP = 65;

        int restored = GameSystems.RegenMana(10);

        Assert.Equal(5, restored);
        Assert.Equal(65, GameState.Player.MP);
    }
}

public class SettingsTests
{
    public SettingsTests()
    {
        GameState.Reset();
    }

    [Fact]
    public void ChangeTargetPriority_UpdatesSetting()
    {
        Assert.Equal(TargetPriority.Nearest, GameState.Settings.TargetMode);

        GameSystems.ChangeTargetPriority(TargetPriority.Strongest);

        Assert.Equal(TargetPriority.Strongest, GameState.Settings.TargetMode);
    }
}

public class SaveTests
{
    public SaveTests()
    {
        GameState.Reset();
    }

    [Fact]
    public void SaveGame_CapturesState()
    {
        GameState.Player.Name = "TestHero";
        GameState.Player.Level = 5;
        GameState.Player.Gold = 999;

        var save = GameSystems.SaveGame();

        Assert.Equal("TestHero", save["name"]);
        Assert.Equal("5", save["level"]);
        Assert.Equal("999", save["gold"]);
    }
}
