using Xunit;

namespace DungeonGame.Tests;

public class LevelingTests
{
    public LevelingTests()
    {
        GameState.Reset();
    }

    [Fact]
    public void XpCurve_IsQuadratic()
    {
        // floor(L^2 * 45)
        GameState.Player.Level = 1;
        Assert.Equal(45, GameState.Player.XPToNextLevel);

        GameState.Player.Level = 2;
        Assert.Equal(180, GameState.Player.XPToNextLevel);

        GameState.Player.Level = 10;
        Assert.Equal(4500, GameState.Player.XPToNextLevel);
    }

    [Fact]
    public void GainXP_AccumulatesXp()
    {
        var (leveled, gained) = GameSystems.GainXP(20);

        Assert.False(leveled);
        Assert.Equal(20, gained);
        Assert.Equal(20, GameState.Player.XP);
    }

    [Fact]
    public void GainXP_TriggersLevelUp()
    {
        // Level 1 needs 45 XP
        var (leveled, _) = GameSystems.GainXP(45);

        Assert.True(leveled);
        Assert.Equal(2, GameState.Player.Level);
    }

    [Fact]
    public void LevelUp_IncreasesMaxHp()
    {
        int oldMaxHp = GameState.Player.MaxHP;

        GameSystems.GainXP(GameState.Player.XPToNextLevel);

        Assert.True(GameState.Player.MaxHP > oldMaxHp);
    }

    [Fact]
    public void LevelUp_GrantsStatPoints()
    {
        Assert.Equal(0, GameState.Player.StatPoints);

        GameSystems.GainXP(GameState.Player.XPToNextLevel);

        Assert.Equal(3, GameState.Player.StatPoints);
    }

    [Fact]
    public void LevelUp_GrantsSkillPoints()
    {
        Assert.Equal(0, GameState.Player.SkillPoints);

        GameSystems.GainXP(GameState.Player.XPToNextLevel);

        Assert.Equal(2, GameState.Player.SkillPoints);
    }

    [Fact]
    public void LevelUp_Heals15Percent()
    {
        GameState.Player.HP = 50;
        int oldHp = GameState.Player.HP;

        GameSystems.GainXP(GameState.Player.XPToNextLevel);

        Assert.True(GameState.Player.HP > oldHp);
    }

    [Fact]
    public void AllocateStatPoint_IncreasesSTR()
    {
        GameState.Player.StatPoints = 1;
        int oldStr = GameState.Player.STR;

        GameSystems.AllocateStatPoint("STR");

        Assert.Equal(oldStr + 1, GameState.Player.STR);
        Assert.Equal(0, GameState.Player.StatPoints);
    }

    [Fact]
    public void AllocateStatPoint_VIT_IncreasesMaxHp()
    {
        GameState.Player.StatPoints = 1;
        int oldMaxHp = GameState.Player.MaxHP;

        GameSystems.AllocateStatPoint("VIT");

        Assert.Equal(oldMaxHp + 3, GameState.Player.MaxHP);
    }

    [Fact]
    public void AllocateStatPoint_FailsWithoutPoints()
    {
        GameState.Player.StatPoints = 0;

        string result = GameSystems.AllocateStatPoint("STR");

        Assert.Contains("No stat points", result);
    }
}
