using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

/// <summary>
/// Tests for DeathPenalty.cs.
/// Covers XP loss, item loss, gold costs, idol detection, and apply item loss.
/// </summary>
public class DeathPenaltyTests
{
    // ── GetExpLossPercent ─────────────────────────────────────────────────────

    [Fact]
    public void GetExpLossPercent_Floor1_Returns0Point4()
    {
        DeathPenalty.GetExpLossPercent(1).Should().BeApproximately(0.4f, 0.001f);
    }

    [Fact]
    public void GetExpLossPercent_ScalesWithFloor()
    {
        DeathPenalty.GetExpLossPercent(10).Should().BeApproximately(4.0f, 0.001f);
        DeathPenalty.GetExpLossPercent(25).Should().BeApproximately(10.0f, 0.001f);
    }

    [Fact]
    public void GetExpLossPercent_CapsAt50()
    {
        DeathPenalty.GetExpLossPercent(200).Should().Be(50.0f);
        DeathPenalty.GetExpLossPercent(1000).Should().Be(50.0f);
    }

    [Fact]
    public void GetExpLossPercent_AtCap_Floor125()
    {
        // 125 * 0.4 = 50 — exactly at the cap
        DeathPenalty.GetExpLossPercent(125).Should().Be(50.0f);
    }

    // ── GetItemsLost ──────────────────────────────────────────────────────────

    [Fact]
    public void GetItemsLost_Floor1_Returns1()
    {
        DeathPenalty.GetItemsLost(1).Should().Be(1);
    }

    [Fact]
    public void GetItemsLost_Floor10_Returns2()
    {
        DeathPenalty.GetItemsLost(10).Should().Be(2);
    }

    [Fact]
    public void GetItemsLost_Floor20_Returns3()
    {
        DeathPenalty.GetItemsLost(20).Should().Be(3);
    }

    [Fact]
    public void GetItemsLost_IncreasesWithFloor()
    {
        DeathPenalty.GetItemsLost(50).Should().BeGreaterThan(DeathPenalty.GetItemsLost(10));
    }

    // ── GetExpProtectionCost ──────────────────────────────────────────────────

    [Fact]
    public void GetExpProtectionCost_Floor1_Returns15()
    {
        DeathPenalty.GetExpProtectionCost(1).Should().Be(15);
    }

    [Fact]
    public void GetExpProtectionCost_ScalesLinearly()
    {
        DeathPenalty.GetExpProtectionCost(10).Should().Be(150);
        DeathPenalty.GetExpProtectionCost(20).Should().Be(300);
    }

    // ── GetBackpackProtectionCost ─────────────────────────────────────────────

    [Fact]
    public void GetBackpackProtectionCost_Floor1_Returns25()
    {
        DeathPenalty.GetBackpackProtectionCost(1).Should().Be(25);
    }

    [Fact]
    public void GetBackpackProtectionCost_ScalesLinearly()
    {
        DeathPenalty.GetBackpackProtectionCost(10).Should().Be(250);
    }

    [Fact]
    public void BackpackCostIsMoreThanExpCost()
    {
        // Backpack protection should always cost more than XP protection
        for (int floor = 1; floor <= 50; floor++)
            DeathPenalty.GetBackpackProtectionCost(floor)
                .Should().BeGreaterThan(DeathPenalty.GetExpProtectionCost(floor));
    }

    // ── CalculateXpLoss ───────────────────────────────────────────────────────

    [Fact]
    public void CalculateXpLoss_ZeroXp_ReturnsZero()
    {
        DeathPenalty.CalculateXpLoss(0, 10).Should().Be(0);
    }

    [Fact]
    public void CalculateXpLoss_CorrectAmount()
    {
        // Floor 10 = 4% loss. 1000 xp → lose 40
        DeathPenalty.CalculateXpLoss(1000, 10).Should().Be(40);
    }

    [Fact]
    public void CalculateXpLoss_CappedAt50Percent()
    {
        // Floor 200 = 50% cap. 1000 xp → lose 500
        DeathPenalty.CalculateXpLoss(1000, 200).Should().Be(500);
    }

    // ── HasSacrificialIdol ────────────────────────────────────────────────────

    [Fact]
    public void HasSacrificialIdol_EmptyInventory_ReturnsFalse()
    {
        var inv = new Inventory();
        DeathPenalty.HasSacrificialIdol(inv).Should().BeFalse();
    }

    [Fact]
    public void HasSacrificialIdol_WithIdol_ReturnsTrue()
    {
        var inv = new Inventory();
        var idol = new ItemDef { Id = "idol_sacrificial", Name = "Sacrificial Idol", Category = ItemCategory.Consumable };
        inv.TryAdd(idol);
        DeathPenalty.HasSacrificialIdol(inv).Should().BeTrue();
    }

    [Fact]
    public void HasSacrificialIdol_WithOtherItems_ReturnsFalse()
    {
        var inv = new Inventory();
        var sword = new ItemDef { Id = "sword", Name = "Sword", Category = ItemCategory.Weapon };
        inv.TryAdd(sword);
        DeathPenalty.HasSacrificialIdol(inv).Should().BeFalse();
    }

    // ── ConsumeSacrificialIdol ────────────────────────────────────────────────

    [Fact]
    public void ConsumeSacrificialIdol_RemovesIdolFromInventory()
    {
        var inv = new Inventory();
        var idol = new ItemDef { Id = "idol_sacrificial", Name = "Sacrificial Idol", Category = ItemCategory.Consumable };
        inv.TryAdd(idol);
        DeathPenalty.ConsumeSacrificialIdol(inv);
        DeathPenalty.HasSacrificialIdol(inv).Should().BeFalse();
        inv.UsedSlots.Should().Be(0);
    }

    [Fact]
    public void ConsumeSacrificialIdol_EmptyInventory_DoesNotThrow()
    {
        var inv = new Inventory();
        var act = () => DeathPenalty.ConsumeSacrificialIdol(inv);
        act.Should().NotThrow();
    }

    // ── ApplyItemLoss ─────────────────────────────────────────────────────────

    [Fact]
    public void ApplyItemLoss_RemovesCorrectNumberOfItems()
    {
        var inv = new Inventory();
        for (int i = 0; i < 5; i++)
            inv.TryAdd(new ItemDef { Id = $"item_{i}", Name = $"Item {i}", Category = ItemCategory.Weapon });

        DeathPenalty.ApplyItemLoss(inv, 2);
        inv.UsedSlots.Should().Be(3);
    }

    [Fact]
    public void ApplyItemLoss_EmptyInventory_DoesNotThrow()
    {
        var inv = new Inventory();
        var act = () => DeathPenalty.ApplyItemLoss(inv, 3);
        act.Should().NotThrow();
    }

    [Fact]
    public void ApplyItemLoss_MoreThanAvailable_RemovesAll()
    {
        var inv = new Inventory();
        inv.TryAdd(new ItemDef { Id = "sword", Name = "Sword", Category = ItemCategory.Weapon });
        DeathPenalty.ApplyItemLoss(inv, 10); // only 1 item
        inv.UsedSlots.Should().Be(0);
    }
}
