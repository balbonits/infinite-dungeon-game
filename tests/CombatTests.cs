using Xunit;

namespace DungeonGame.Tests;

public class CombatTests
{
    public CombatTests()
    {
        GameState.Reset();
    }

    [Fact]
    public void AttackMonster_DealsDamage()
    {
        GameSystems.EnterDungeon();
        var monster = GameSystems.SpawnMonster("Rat", MonsterTier.Tier1);
        int startHp = monster.HP;

        var (damage, _) = GameSystems.AttackMonster(monster);

        Assert.True(damage > 0);
        Assert.True(monster.HP < startHp);
    }

    [Fact]
    public void AttackMonster_KillsAtZeroHp()
    {
        GameSystems.EnterDungeon();
        var monster = GameSystems.SpawnMonster("Rat", MonsterTier.Tier1);
        monster.HP = 1;

        GameSystems.AttackMonster(monster);

        Assert.True(monster.IsDead);
        Assert.Equal(0, monster.HP);
    }

    [Fact]
    public void MonsterAttackPlayer_DealsDamage()
    {
        GameSystems.EnterDungeon();
        var monster = GameSystems.SpawnMonster("Rat", MonsterTier.Tier1);
        int startHp = GameState.Player.HP;

        int damage = GameSystems.MonsterAttackPlayer(monster);

        Assert.True(damage > 0);
        Assert.True(GameState.Player.HP < startHp);
    }

    [Fact]
    public void MonsterAttackPlayer_KillsPlayerAtZeroHp()
    {
        GameSystems.EnterDungeon();
        var monster = GameSystems.SpawnMonster("Orc", MonsterTier.Tier3);
        GameState.Player.HP = 1;

        GameSystems.MonsterAttackPlayer(monster);

        Assert.True(GameState.Player.IsDead);
        Assert.Equal(0, GameState.Player.HP);
    }

    [Fact]
    public void PlayerDamage_ScalesWithLevel()
    {
        int dmgAtLevel1 = GameState.Player.TotalDamage;

        GameState.Player.Level = 10;
        GameState.Player.InvalidateStats();
        int dmgAtLevel10 = GameState.Player.TotalDamage;

        Assert.True(dmgAtLevel10 > dmgAtLevel1);
    }

    [Fact]
    public void PlayerDamage_IncludesWeapon()
    {
        int baseDmg = GameState.Player.TotalDamage;

        var sword = GameSystems.CreateItem("Sword", ItemType.Weapon, EquipSlot.MainHand, damage: 10);
        GameSystems.AddToInventory(sword);
        GameSystems.EquipItem(sword);

        Assert.Equal(baseDmg + 10, GameState.Player.TotalDamage);
    }

    [Fact]
    public void DefenseReducesDamage()
    {
        GameSystems.EnterDungeon();
        var monster = GameSystems.SpawnMonster("Orc", MonsterTier.Tier3);

        // Without armor
        int startHp1 = 200;
        GameState.Player.HP = startHp1;
        GameState.Player.MaxHP = startHp1;
        int dmgNoArmor = GameSystems.MonsterAttackPlayer(monster);

        // With armor
        GameState.Player.HP = startHp1;
        var armor = GameSystems.CreateItem("Plate", ItemType.Armor, EquipSlot.Body, defense: 20);
        GameSystems.AddToInventory(armor);
        GameSystems.EquipItem(armor);
        int dmgWithArmor = GameSystems.MonsterAttackPlayer(monster);

        Assert.True(dmgWithArmor <= dmgNoArmor);
    }
}
