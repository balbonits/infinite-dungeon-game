using System.Collections.Generic;
using Xunit;

namespace DungeonGame.Tests;

public class EntityFactoryTests
{
    // ── Player defaults ──────────────────────────────────────────────

    [Fact]
    public void CreatePlayer_HasCorrectDefaults()
    {
        var p = EntityFactory.CreatePlayer();

        Assert.Equal("Hero", p.Name);
        Assert.Equal(EntityType.Player, p.Type);
        Assert.Equal(1, p.Level);
        Assert.Equal(108, p.HP);
        Assert.Equal(108, p.MaxHP);
        Assert.Equal(65, p.MP);
        Assert.Equal(65, p.MaxMP);
        Assert.Equal(5, p.STR);
        Assert.Equal(5, p.DEX);
        Assert.Equal(5, p.INT);
        Assert.Equal(5, p.VIT);
        Assert.Equal(12, p.BaseDamage);
        Assert.Equal(0, p.BaseDefense);
        Assert.Equal(0.42f, p.AttackSpeed);
        Assert.Equal(78f, p.AttackRange);
        Assert.Equal(12f, p.HitboxRadius);
        Assert.Equal(190f, p.MoveSpeed);
        Assert.Equal(100, p.Gold);
        Assert.Equal(25, p.InventorySize);
        Assert.False(p.IsDead);
        Assert.Equal(0, p.XP);
        Assert.Equal(0, p.StatPoints);
        Assert.Equal(0, p.SkillPoints);
    }

    [Fact]
    public void CreatePlayer_AcceptsCustomName()
    {
        var p = EntityFactory.CreatePlayer("Gandalf");
        Assert.Equal("Gandalf", p.Name);
    }

    [Fact]
    public void CreatePlayer_DefaultName_IsHero()
    {
        var p = EntityFactory.CreatePlayer();
        Assert.Equal("Hero", p.Name);
    }

    [Fact]
    public void CreatePlayer_HasEmptyEquipmentAndInventory()
    {
        var p = EntityFactory.CreatePlayer();
        Assert.Empty(p.Equipment);
        Assert.Empty(p.Inventory);
    }

    [Fact]
    public void CreatePlayer_HasEmptyEffectsList()
    {
        var p = EntityFactory.CreatePlayer();
        Assert.Empty(p.Effects);
    }

    // ── Unique IDs ───────────────────────────────────────────────────

    [Fact]
    public void CreatePlayer_GeneratesUniqueIds()
    {
        var ids = new HashSet<string>();
        for (int i = 0; i < 100; i++)
            ids.Add(EntityFactory.CreatePlayer().Id);

        Assert.Equal(100, ids.Count);
    }

    [Fact]
    public void AllFactoryMethods_GenerateUniqueIds()
    {
        var p = EntityFactory.CreatePlayer();
        var e = EntityFactory.CreateEnemy("Rat", 1);
        var n = EntityFactory.CreateNPC("Bob");

        Assert.NotEqual(p.Id, e.Id);
        Assert.NotEqual(p.Id, n.Id);
        Assert.NotEqual(e.Id, n.Id);
    }

    [Fact]
    public void AllFactoryMethods_IdIsNonEmptyGuid()
    {
        var p = EntityFactory.CreatePlayer();
        var e = EntityFactory.CreateEnemy("Rat", 1);
        var n = EntityFactory.CreateNPC("Bob");

        Assert.False(string.IsNullOrWhiteSpace(p.Id));
        Assert.False(string.IsNullOrWhiteSpace(e.Id));
        Assert.False(string.IsNullOrWhiteSpace(n.Id));

        // Verify they're valid GUIDs
        Assert.True(System.Guid.TryParse(p.Id, out _));
        Assert.True(System.Guid.TryParse(e.Id, out _));
        Assert.True(System.Guid.TryParse(n.Id, out _));
    }

    // ── Entity types ─────────────────────────────────────────────────

    [Fact]
    public void CreatePlayer_HasPlayerType()
    {
        Assert.Equal(EntityType.Player, EntityFactory.CreatePlayer().Type);
    }

    [Fact]
    public void CreateEnemy_HasEnemyType()
    {
        Assert.Equal(EntityType.Enemy, EntityFactory.CreateEnemy("Rat", 1).Type);
    }

    [Fact]
    public void CreateNPC_HasNPCType()
    {
        Assert.Equal(EntityType.NPC, EntityFactory.CreateNPC("Bob").Type);
    }

    // ── Enemy tier stats ─────────────────────────────────────────────

    [Fact]
    public void CreateEnemy_Tier1_HasCorrectBaseStats()
    {
        var e = EntityFactory.CreateEnemy("Rat", 1, floorNumber: 1);

        Assert.Equal(30, e.HP);       // baseHP 30 * floorMult 1.0
        Assert.Equal(30, e.MaxHP);
        Assert.Equal(4, e.BaseDamage); // 3 + tier(1)
        Assert.Equal(2, e.BaseDefense); // tier(1) * 2
        Assert.Equal(1, e.Tier);
        Assert.Equal(1, e.Level);
        Assert.Equal(10, e.XPReward);  // baseXP 10 * floorMult 1.0
        Assert.Equal(5, e.GoldReward); // 5 * 1.0 * 1
    }

    [Fact]
    public void CreateEnemy_Tier2_HasCorrectBaseStats()
    {
        var e = EntityFactory.CreateEnemy("Goblin", 2, floorNumber: 1);

        Assert.Equal(42, e.HP);        // baseHP 42 * 1.0
        Assert.Equal(42, e.MaxHP);
        Assert.Equal(5, e.BaseDamage);  // 3 + 2
        Assert.Equal(4, e.BaseDefense); // 2 * 2
        Assert.Equal(2, e.Tier);
        Assert.Equal(2, e.Level);
        Assert.Equal(15, e.XPReward);   // 15 * 1.0
        Assert.Equal(10, e.GoldReward); // 5 * 1.0 * 2
    }

    [Fact]
    public void CreateEnemy_Tier3_HasCorrectBaseStats()
    {
        var e = EntityFactory.CreateEnemy("Orc", 3, floorNumber: 1);

        Assert.Equal(54, e.HP);        // baseHP 54 * 1.0
        Assert.Equal(54, e.MaxHP);
        Assert.Equal(6, e.BaseDamage);  // 3 + 3
        Assert.Equal(6, e.BaseDefense); // 3 * 2
        Assert.Equal(3, e.Tier);
        Assert.Equal(3, e.Level);
        Assert.Equal(20, e.XPReward);   // 20 * 1.0
        Assert.Equal(15, e.GoldReward); // 5 * 1.0 * 3
    }

    [Fact]
    public void CreateEnemy_UnknownTier_DefaultsToTier1Stats()
    {
        var e = EntityFactory.CreateEnemy("Unknown", 99, floorNumber: 1);

        Assert.Equal(30, e.HP);  // default branch: baseHP 30
        Assert.Equal(10, e.XPReward); // default branch: baseXP 10
    }

    // ── Enemy floor scaling ──────────────────────────────────────────

    [Fact]
    public void CreateEnemy_Floor1_MultiplierIs1()
    {
        var e = EntityFactory.CreateEnemy("Rat", 1, floorNumber: 1);
        // floorMult = 1 + (1-1) * 0.5 = 1.0
        Assert.Equal(30, e.HP);
        Assert.Equal(10, e.XPReward);
    }

    [Fact]
    public void CreateEnemy_Floor5_MultiplierIs3()
    {
        var e = EntityFactory.CreateEnemy("Rat", 1, floorNumber: 5);
        // floorMult = 1 + (5-1) * 0.5 = 3.0
        Assert.Equal(90, e.HP);       // 30 * 3.0
        Assert.Equal(90, e.MaxHP);
        Assert.Equal(30, e.XPReward);  // 10 * 3.0
        Assert.Equal(15, e.GoldReward); // 5 * 3.0 * 1
    }

    [Fact]
    public void CreateEnemy_Floor10_MultiplierIs5_5()
    {
        var e = EntityFactory.CreateEnemy("Orc", 3, floorNumber: 10);
        // floorMult = 1 + (10-1) * 0.5 = 5.5
        Assert.Equal((int)(54 * 5.5f), e.HP);       // 54 * 5.5
        Assert.Equal((int)(54 * 5.5f), e.MaxHP);
        Assert.Equal((int)(20 * 5.5f), e.XPReward);
        Assert.Equal((int)(5 * 5.5f * 3), e.GoldReward);
    }

    [Fact]
    public void CreateEnemy_HigherFloors_HaveMoreHP()
    {
        var floor1 = EntityFactory.CreateEnemy("Rat", 1, floorNumber: 1);
        var floor5 = EntityFactory.CreateEnemy("Rat", 1, floorNumber: 5);
        var floor10 = EntityFactory.CreateEnemy("Rat", 1, floorNumber: 10);

        Assert.True(floor5.HP > floor1.HP);
        Assert.True(floor10.HP > floor5.HP);
    }

    [Fact]
    public void CreateEnemy_SharedProperties()
    {
        var e = EntityFactory.CreateEnemy("Rat", 1);

        Assert.Equal(1.0f, e.AttackSpeed);
        Assert.Equal(60f, e.AttackRange);
        Assert.Equal(14f, e.HitboxRadius);
        Assert.Equal(120f, e.MoveSpeed);
        Assert.Equal(EntityType.Enemy, e.Type);
    }

    // ── NPC ──────────────────────────────────────────────────────────

    [Fact]
    public void CreateNPC_HasCorrectDefaults()
    {
        var n = EntityFactory.CreateNPC("Shopkeeper");

        Assert.Equal("Shopkeeper", n.Name);
        Assert.Equal(EntityType.NPC, n.Type);
        Assert.Equal(1, n.Level);
        Assert.Equal(100, n.HP);
        Assert.Equal(100, n.MaxHP);
        Assert.Equal(0, n.MP);
        Assert.Equal(0, n.MaxMP);
        Assert.Equal(80f, n.MoveSpeed);
        Assert.Equal(12f, n.HitboxRadius);
    }

    [Fact]
    public void CreateNPC_HasNoCombatStats()
    {
        var n = EntityFactory.CreateNPC("Bob");

        Assert.Equal(0, n.BaseDamage);
        Assert.Equal(0, n.BaseDefense);
        Assert.Equal(0, n.STR);
        Assert.Equal(0, n.DEX);
        Assert.Equal(0, n.INT);
        Assert.Equal(0, n.VIT);
        Assert.Equal(0f, n.AttackSpeed);
        Assert.Equal(0f, n.AttackRange);
    }

    [Fact]
    public void CreateNPC_HasNoEnemyRewards()
    {
        var n = EntityFactory.CreateNPC("Bob");

        Assert.Equal(0, n.XPReward);
        Assert.Equal(0, n.GoldReward);
        Assert.Equal(0, n.Tier);
    }

    // ── XP curve regression guard ────────────────────────────────────

    [Theory]
    [InlineData(1, 45)]       // 1*1*45
    [InlineData(2, 180)]      // 2*2*45
    [InlineData(5, 1125)]     // 5*5*45
    [InlineData(10, 4500)]    // 10*10*45
    [InlineData(50, 112500)]  // 50*50*45
    [InlineData(100, 450000)] // 100*100*45
    public void XPToNextLevel_MatchesCurve_L2x45(int level, int expected)
    {
        var p = EntityFactory.CreatePlayer();
        p.Level = level;
        Assert.Equal(expected, p.XPToNextLevel);
    }
}
