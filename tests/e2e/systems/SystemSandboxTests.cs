using GdUnit4;
using static GdUnit4.Assertions;

namespace DungeonGame.Tests.E2E.Systems;

/// <summary>
/// GdUnit4 scene-runner tests for system sandboxes.
/// Each test loads the sandbox scene and verifies it initialises without errors.
/// Pure-logic assertions run without [RequireGodotRuntime].
/// </summary>

// ── Floor Generator ──────────────────────────────────────────────────────────

[TestSuite]
public class FloorGenSandboxTests
{
    [TestCase]
    [RequireGodotRuntime]
    public void FloorGen_TenSeeds_AllProduceValidLayouts()
    {
        for (int seed = 0; seed < 10; seed++)
        {
            var gen = new FloorGenerator(seed * 137);
            gen.Generate(1 + seed % 15);

            AssertThat(gen.Rooms.Count).IsBetween(5, 8);
            AssertThat(gen.EntrancePos).IsNotEqual(gen.ExitPos);
            AssertThat((int)gen.Grid[gen.EntrancePos.X, gen.EntrancePos.Y])
                .IsEqual((int)FloorGenerator.Tile.Floor);
            AssertThat((int)gen.Grid[gen.ExitPos.X, gen.ExitPos.Y])
                .IsEqual((int)FloorGenerator.Tile.Floor);
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void FloorGen_SameSeed_ProducesSameLayout()
    {
        var a = new FloorGenerator(42); a.Generate(5);
        var b = new FloorGenerator(42); b.Generate(5);
        AssertThat(a.Rooms.Count).IsEqual(b.Rooms.Count);
        AssertThat(a.EntrancePos).IsEqual(b.EntrancePos);
        AssertThat(a.ExitPos).IsEqual(b.ExitPos);
    }

    [TestCase]
    [RequireGodotRuntime]
    public async System.Threading.Tasks.Task FloorGenSandbox_Scene_LoadsWithoutError()
    {
        var runner = ISceneRunner.Load("res://scenes/sandbox/systems/FloorGenSandbox.tscn");
        AssertThat(runner.Scene()).IsNotNull();
        await runner.SimulateFrames(10);
    }
}

// ── Inventory ────────────────────────────────────────────────────────────────

[TestSuite]
public class InventorySandboxTests
{
    [TestCase]
    public void Inventory_StackingAndOverflow_WorkCorrectly()
    {
        var inv = new Inventory(3);
        var potion = new ItemDef { Id = "p", Name = "Potion", Category = ItemCategory.Consumable };
        var sword = new ItemDef { Id = "s", Name = "Sword", Category = ItemCategory.Weapon };

        AssertThat(inv.TryAdd(potion, 99)).IsTrue();
        AssertThat(inv.TryAdd(potion, 1)).IsTrue();  // spills to slot 1
        AssertThat(inv.TryAdd(sword)).IsTrue();       // fills slot 2
        AssertThat(inv.TryAdd(sword)).IsFalse();      // full
        AssertThat(inv.UsedSlots).IsEqual(3);
    }

    [TestCase]
    public void Inventory_BuySell_GoldAccurate()
    {
        var item = new ItemDef { Id = "x", Name = "X", Category = ItemCategory.Weapon, BuyPrice = 100, SellPrice = 40 };
        var inv = new Inventory { Gold = 200 };
        AssertThat(inv.TryBuy(item)).IsTrue();
        AssertThat(inv.Gold).IsEqual(100);
        inv.TrySell(0);
        AssertThat(inv.Gold).IsEqual(140);
    }

    [TestCase]
    [RequireGodotRuntime]
    public async System.Threading.Tasks.Task InventorySandbox_Scene_LoadsWithoutError()
    {
        var runner = ISceneRunner.Load("res://scenes/sandbox/systems/InventorySandbox.tscn");
        AssertThat(runner.Scene()).IsNotNull();
        await runner.SimulateFrames(5);
    }
}

// ── Loot Table ───────────────────────────────────────────────────────────────

[TestSuite]
public class LootTableSandboxTests
{
    [TestCase]
    public void LootTable_GoldDrop_AlwaysAboveBase()
    {
        for (int level = 1; level <= 30; level++)
        {
            int gold = LootTable.GetGoldDrop(level);
            AssertThat(gold).IsGreaterEqual(2 + level);
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void LootTable_DropRate_WithinExpectedBounds()
    {
        const int kills = 2000;
        int drops = 0;
        for (int i = 0; i < kills; i++)
            if (LootTable.RollItemDrop(10) != null) drops++;

        float actual = (float)drops / kills * 100f;
        float expected = Godot.Mathf.Min(30f, 8f + 10f);
        AssertThat(actual).IsBetween(expected - 8f, expected + 8f);
    }
}

// ── Bank ─────────────────────────────────────────────────────────────────────

[TestSuite]
public class BankSandboxTests
{
    [TestCase]
    public void Bank_ExpansionCost_FollowsLinearFormula()
    {
        // Bank.GetNextExpansionCost returns 50 * (n+1) — see Bank.cs:39.
        // (Previously an N² formula; simplified to linear per the 2025 economy
        // rebalance. Keep this test aligned with the real code.)
        var bank = new Bank();
        var inv = new Inventory { Gold = 1_000_000 };

        AssertThat(bank.GetNextExpansionCost()).IsEqual(50L);
        bank.PurchaseExpansion(inv);
        AssertThat(bank.GetNextExpansionCost()).IsEqual(100L);
        bank.PurchaseExpansion(inv);
        AssertThat(bank.GetNextExpansionCost()).IsEqual(150L);
    }

    [TestCase]
    public void Bank_DepositWithdraw_RoundTrip()
    {
        var bank = new Bank();
        var inv = new Inventory();
        var item = new ItemDef { Id = "sword", Name = "Sword", Category = ItemCategory.Weapon };
        inv.TryAdd(item);

        AssertThat(bank.Deposit(inv, 0)).IsTrue();
        AssertThat(inv.UsedSlots).IsEqual(0);
        AssertThat(bank.Storage.UsedSlots).IsEqual(1);

        AssertThat(bank.Withdraw(inv, 0)).IsTrue();
        AssertThat(inv.UsedSlots).IsEqual(1);
        AssertThat(bank.Storage.UsedSlots).IsEqual(0);
    }
}

// ── Death Penalty ─────────────────────────────────────────────────────────────

[TestSuite]
public class DeathPenaltySandboxTests
{
    [TestCase]
    public void DeathPenalty_XpLoss_CapsAt50Percent()
    {
        AssertThat(DeathPenalty.GetExpLossPercent(125)).IsEqual(50f);
        AssertThat(DeathPenalty.GetExpLossPercent(500)).IsEqual(50f);
    }

    [TestCase]
    public void DeathPenalty_Idol_ConsumedAndProtects()
    {
        var inv = new Inventory();
        var idol = new ItemDef { Id = "consumable_sacrificial_idol", Name = "Idol", Category = ItemCategory.Consumable };
        inv.TryAdd(idol);

        AssertThat(DeathPenalty.HasSacrificialIdol(inv)).IsTrue();
        DeathPenalty.ConsumeSacrificialIdol(inv);
        AssertThat(DeathPenalty.HasSacrificialIdol(inv)).IsFalse();
        AssertThat(inv.UsedSlots).IsEqual(0);
    }
}
