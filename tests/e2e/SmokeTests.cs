using GdUnit4;
using static GdUnit4.Assertions;

namespace DungeonGame.Tests.E2E;

/// <summary>
/// Smoke tests — verify the game project loads without crashing.
/// These run headless via: make test-gdunit
/// Pure C# tests (no [RequireGodotRuntime]) run without launching Godot.
/// Scene runner tests require Godot binary and are tagged [RequireGodotRuntime].
/// </summary>
[TestSuite]
public class SmokeTests
{
    // ── Pure C# smoke tests (no Godot runtime needed) ────────────────────────

    [TestCase]
    public void ItemDatabase_Loads_WithoutError()
    {
        // ItemDatabase has a static constructor — accessing it triggers load
        var item = ItemDatabase.Get("potion_hp_small");
        AssertThat(item).IsNotNull();
        AssertThat(item!.Name).IsNotEmpty();
    }

    [TestCase]
    public void ItemDatabase_AllItems_HaveUniqueIds()
    {
        var ids = new System.Collections.Generic.HashSet<string>();
        foreach (var item in ItemDatabase.All)
        {
            AssertThat(ids.Contains(item.Id))
                .IsFalse($"Duplicate item ID found: {item.Id}");
            ids.Add(item.Id);
        }
    }

    [TestCase]
    public void StatBlock_DefaultConstruction_DoesNotThrow()
    {
        var sb = new StatBlock();
        AssertThat(sb.Str).IsEqual(0);
        AssertThat(sb.GetEffective(0)).IsEqual(0f);
    }

    [TestCase]
    public void Inventory_DefaultConstruction_DoesNotThrow()
    {
        var inv = new Inventory();
        AssertThat(inv.SlotCount).IsEqual(25);
        AssertThat(inv.UsedSlots).IsEqual(0);
    }

    [TestCase]
    public void Bank_DefaultConstruction_DoesNotThrow()
    {
        var bank = new Bank();
        AssertThat(bank.TotalSlots).IsEqual(Bank.StartingSlots);
    }

    [TestCase]
    public void DeathPenalty_FloorCap_IsCorrect()
    {
        AssertThat(DeathPenalty.GetExpLossPercent(1000)).IsEqual(50f);
    }

    // ── Scene runner smoke tests (need Godot) ────────────────────────────────

    [TestCase]
    [RequireGodotRuntime]
    public void MainScene_Loads_WithoutCrashing()
    {
        var runner = ISceneRunner.Load("res://scenes/main.tscn", verbose: true);
        AssertThat(runner).IsNotNull();
        AssertThat(runner.Scene()).IsNotNull();
    }

    [TestCase]
    [RequireGodotRuntime]
    public async Task DungeonScene_Loads_WithoutCrashing()
    {
        var runner = ISceneRunner.Load("res://scenes/dungeon.tscn");
        AssertThat(runner.Scene()).IsNotNull();
        await runner.SimulateFrames(5);
    }
}
