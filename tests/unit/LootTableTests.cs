using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

public class LootTableTests
{
    // -- GetGoldDrop --

    [Fact]
    public void GetGoldDrop_Level1_AtLeast3()
    {
        // base = 2 + 1 = 3, variance = max(1, 0) = 1, range [3, 4]
        for (int i = 0; i < 50; i++)
            LootTable.GetGoldDrop(1).Should().BeGreaterOrEqualTo(3);
    }

    [Fact]
    public void GetGoldDrop_IncreasesWithLevel()
    {
        // Over many samples, average for level 50 should exceed level 1
        int lowSum = 0, highSum = 0;
        for (int i = 0; i < 200; i++)
        {
            lowSum += LootTable.GetGoldDrop(1);
            highSum += LootTable.GetGoldDrop(50);
        }
        highSum.Should().BeGreaterThan(lowSum);
    }

    [Fact]
    public void GetGoldDrop_Level50_MinIs52()
    {
        // base = 2 + 50 = 52, variance = 25, range [52, 77]
        for (int i = 0; i < 50; i++)
            LootTable.GetGoldDrop(50).Should().BeGreaterOrEqualTo(52);
    }

    [Fact]
    public void GetGoldDrop_AlwaysPositive()
    {
        for (int level = 1; level <= 100; level++)
            LootTable.GetGoldDrop(level).Should().BePositive();
    }
}
