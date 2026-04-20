using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

/// <summary>
/// State-mutation tests for <see cref="DeathPenalty"/>: seed a realistic
/// character fixture (populated backpack, banked gold, optional idol),
/// call each primitive the death flow eventually hits, and assert the
/// post-mutation state.
///
/// Pure-logic edge cases (XP-loss formulas, idol bool, item-count math)
/// are in <see cref="DeathPenaltyTests"/>. These tests cover:
///
/// - <c>WipeBackpack</c> — used by the Accept Fate / Quit Game paths per
///   docs/systems/death.md §"The Five Options".
/// - <c>PayBuyout</c> — used by Save Equipment / Save Backpack / Save Both
///   buyout options.
/// - <c>HasSacrificialIdol</c> / <c>ConsumeSacrificialIdol</c> — used by
///   the idol-branch shortcut.
/// - <c>ApplyItemLoss</c> — legacy partial-loss helper retained on the
///   class; not currently wired to the primary DeathScreen flow (which
///   uses WipeBackpack), but exercised here so refactors don't silently
///   drop it.
/// - <c>CalculateXpLoss</c> — the one arithmetic call the flow makes.
/// </summary>
public class DeathPenaltyIntegrationTests
{
    private const string IdolId = "consumable_sacrificial_idol";

    // ── Test fixtures ────────────────────────────────────────────────────────

    /// <summary>Deterministic ItemDefs — not routed through ItemDatabase so the
    /// tests don't assume catalog stability.</summary>
    private static ItemDef Potion(string id = "potion", int tier = 1) => new()
    {
        Id = id,
        Name = id,
        Category = ItemCategory.Consumable,
        Tier = tier,
        BuyPrice = 25,
        SellPrice = 10,
    };

    private static ItemDef Weapon(string id = "sword", int tier = 2) => new()
    {
        Id = id,
        Name = id,
        Category = ItemCategory.Weapon,
        Tier = tier,
        BuyPrice = 100,
        SellPrice = 40,
    };

    private static ItemDef Material(string id = "ore", int tier = 1) => new()
    {
        Id = id,
        Name = id,
        Category = ItemCategory.Material,
        Tier = tier,
        BuyPrice = 15,
        SellPrice = 5,
    };

    /// <summary>Build a character-like fixture: 20-slot backpack with 10
    /// varied items + gold, plus a Bank at its default capacity. Returns
    /// both for direct inspection by the tests below. (Bank slot count
    /// isn't parameterized because Bank has no public constructor for
    /// capacity — PurchaseExpansion is the only growth API.)</summary>
    private static (Inventory backpack, Bank bank) SeedCharacter(long backpackGold = 500)
    {
        var backpack = new Inventory(20) { Gold = backpackGold };
        // Populate varied categories so item-loss semantics exercise the mix.
        backpack.TryAdd(Potion("potion_small"), 3);
        backpack.TryAdd(Potion("potion_large"), 2);
        backpack.TryAdd(Weapon("sword_t1"));
        backpack.TryAdd(Weapon("sword_t2"));
        backpack.TryAdd(Material("iron_ore"), 5);
        backpack.TryAdd(Material("wood"), 10);
        backpack.TryAdd(Potion("elixir"));
        backpack.TryAdd(Weapon("dagger"));
        backpack.TryAdd(Material("gem"));
        backpack.TryAdd(Weapon("bow"));

        var bank = new Bank();
        // Bank starts with default capacity; extra slots via Purchase API.
        return (backpack, bank);
    }

    // ── Full-inventory death ─────────────────────────────────────────────────

    [Fact]
    public void ApplyItemLoss_OnFloor10_RemovesExpectedCount()
    {
        var (backpack, _) = SeedCharacter();
        long before = CountUnits(backpack);
        int shouldLose = DeathPenalty.GetItemsLost(deepestFloor: 10);

        DeathPenalty.ApplyItemLoss(backpack, shouldLose);

        long after = CountUnits(backpack);
        // ApplyItemLoss removes up to shouldLose total units in one call —
        // it walks up to shouldLose distinct occupied slots and subtracts one
        // unit from each (a stacked slot may decrement without emptying).
        // Assert on the cumulative unit delta rather than slot count.
        (before - after).Should().Be(shouldLose);
        backpack.Gold.Should().Be(500, "gold isn't touched by item loss alone");
    }

    [Fact]
    public void ApplyItemLoss_NeverRemovesMoreThanExist()
    {
        var backpack = new Inventory(20) { Gold = 0 };
        backpack.TryAdd(Potion("only_one"));  // single item
        DeathPenalty.ApplyItemLoss(backpack, itemsToLose: 999);
        CountOccupied(backpack).Should().Be(0, "loss capped at what exists");
    }

    [Fact]
    public void ApplyItemLoss_ZeroItemsToLose_IsNoOp()
    {
        var (backpack, _) = SeedCharacter();
        int before = CountOccupied(backpack);
        DeathPenalty.ApplyItemLoss(backpack, 0);
        CountOccupied(backpack).Should().Be(before);
    }

    // ── Sacrificial Idol protection ──────────────────────────────────────────

    [Fact]
    public void HasIdol_ReturnsTrue_WhenIdolInBackpack()
    {
        var (backpack, _) = SeedCharacter();
        backpack.TryAdd(Potion(IdolId));
        DeathPenalty.HasSacrificialIdol(backpack).Should().BeTrue();
    }

    [Fact]
    public void HasIdol_ReturnsFalse_WhenIdolAbsent()
    {
        var (backpack, _) = SeedCharacter();
        DeathPenalty.HasSacrificialIdol(backpack).Should().BeFalse();
    }

    [Fact]
    public void ConsumeIdol_RemovesOne_LeavesRestIntact()
    {
        var (backpack, _) = SeedCharacter();
        backpack.TryAdd(Potion(IdolId));
        int countBefore = CountOccupied(backpack);

        DeathPenalty.ConsumeSacrificialIdol(backpack);

        DeathPenalty.HasSacrificialIdol(backpack).Should().BeFalse();
        CountOccupied(backpack).Should().Be(countBefore - 1,
            "idol is consumed; other items unchanged");
    }

    [Fact]
    public void ConsumeIdol_WhenNoIdol_IsNoOp()
    {
        var (backpack, _) = SeedCharacter();
        int countBefore = CountOccupied(backpack);
        DeathPenalty.ConsumeSacrificialIdol(backpack);
        CountOccupied(backpack).Should().Be(countBefore, "no idol → no change");
    }

    // ── Full backpack wipe ───────────────────────────────────────────────────

    [Fact]
    public void WipeBackpack_ClearsAllItems_AndZerosGold()
    {
        var (backpack, _) = SeedCharacter(backpackGold: 1200);
        DeathPenalty.WipeBackpack(backpack);
        CountOccupied(backpack).Should().Be(0);
        backpack.Gold.Should().Be(0, "gold wiped alongside items");
    }

    // ── Buyout gold accounting ───────────────────────────────────────────────

    [Fact]
    public void PayBuyout_FromBackpackOnly_WhenSufficient()
    {
        var (backpack, bank) = SeedCharacter(backpackGold: 1000);
        bank.Gold = 0;
        long cost = 300;

        bool ok = DeathPenalty.PayBuyout(backpack, bank, cost);

        ok.Should().BeTrue();
        backpack.Gold.Should().Be(700, "cost subtracted from backpack first");
        bank.Gold.Should().Be(0, "bank untouched when backpack covers cost");
    }

    [Fact]
    public void PayBuyout_SpillsFromBank_WhenBackpackShort()
    {
        var (backpack, bank) = SeedCharacter(backpackGold: 100);
        bank.Gold = 500;
        long cost = 300;

        bool ok = DeathPenalty.PayBuyout(backpack, bank, cost);

        ok.Should().BeTrue();
        backpack.Gold.Should().Be(0, "backpack drained first");
        bank.Gold.Should().Be(300, "remainder (200) pulled from bank");
    }

    [Fact]
    public void PayBuyout_FailsCleanly_WhenCombinedInsufficient()
    {
        var (backpack, bank) = SeedCharacter(backpackGold: 50);
        bank.Gold = 100;
        long cost = 500;

        bool ok = DeathPenalty.PayBuyout(backpack, bank, cost);

        ok.Should().BeFalse();
        backpack.Gold.Should().Be(50, "failed payment doesn't deduct anything");
        bank.Gold.Should().Be(100, "bank also untouched on failure");
    }

    [Fact]
    public void PayBuyout_ExactAmount_LeavesBothAtZero()
    {
        var (backpack, bank) = SeedCharacter(backpackGold: 200);
        bank.Gold = 300;
        long cost = 500;

        bool ok = DeathPenalty.PayBuyout(backpack, bank, cost);

        ok.Should().BeTrue();
        backpack.Gold.Should().Be(0);
        bank.Gold.Should().Be(0);
    }

    // ── XP loss integration ──────────────────────────────────────────────────

    [Fact]
    public void CalculateXpLoss_FloorOneCharacter_LosesMinimum()
    {
        // Floor 1 = 0.4% loss. A character with 10,000 XP loses 40.
        DeathPenalty.CalculateXpLoss(currentXp: 10_000, deepestFloor: 1).Should().Be(40);
    }

    [Fact]
    public void CalculateXpLoss_MidCharacter_ScalesWithFloor()
    {
        // Floor 25 = 10% loss. 10,000 XP → 1000 lost.
        DeathPenalty.CalculateXpLoss(currentXp: 10_000, deepestFloor: 25).Should().Be(1000);
    }

    [Fact]
    public void CalculateXpLoss_DeepFloor_CapsAt50Percent()
    {
        // Floor 500 would compute to 200% loss — cap at 50% = 5000 from 10,000.
        DeathPenalty.CalculateXpLoss(currentXp: 10_000, deepestFloor: 500).Should().Be(5000);
    }

    [Fact]
    public void CalculateXpLoss_ZeroXp_StillZero()
    {
        DeathPenalty.CalculateXpLoss(currentXp: 0, deepestFloor: 25).Should().Be(0);
    }

    // ── Full flow: seeded character dies at floor 15 ─────────────────────────

    [Fact]
    public void DeathFlow_Floor15_NoIdol_AppliesExpectedLoss()
    {
        // Floor 15 Accept Fate scenario.
        // Character has 10 distinct items, 500 backpack gold, 1000 bank gold.
        var (backpack, bank) = SeedCharacter(backpackGold: 500);
        bank.Gold = 1000;

        // Accept Fate (per docs/systems/death.md §"The Five Options"):
        //   equipment = -1 random piece, backpack items = all lost,
        //   backpack gold = lost, bank gold = untouched.
        // The logic-layer primitive that implements the backpack side is
        // WipeBackpack (not ApplyItemLoss — that's the older partial-loss
        // helper kept for testing only, Blacksmith-recycle paths, etc.).
        DeathPenalty.HasSacrificialIdol(backpack).Should().BeFalse();
        DeathPenalty.WipeBackpack(backpack);

        CountUnits(backpack).Should().Be(0, "Accept Fate wipes the whole backpack");
        backpack.Gold.Should().Be(0, "Accept Fate zeros backpack gold");
        bank.Gold.Should().Be(1000, "Accept Fate does not touch bank gold");
    }

    [Fact]
    public void DeathFlow_Floor15_WithIdol_ConsumesIdol_KeepsItems()
    {
        // Idol path: consume the idol, skip the item loss. This mirrors the
        // death-screen flow's branch when HasSacrificialIdol returns true.
        // Per spec (docs/systems/death.md §"Sacrificial Idol"), the idol acts
        // as a free "Save Both" — so backpack gold must also survive
        // consumption, not just the items.
        //
        // Copilot PR #35 round-2 asked us to remove the else-fallback: if
        // the fixture seed is ever broken and HasSacrificialIdol returns
        // false, we want the test to fail loudly rather than silently fall
        // back to ApplyItemLoss (which only removes one unit from the 10-
        // item seed, still passing the itemsBefore - 1 slot assertion).
        var (backpack, _) = SeedCharacter(backpackGold: 500);
        backpack.TryAdd(Potion(IdolId));
        int itemsBefore = CountOccupied(backpack);

        DeathPenalty.HasSacrificialIdol(backpack).Should().BeTrue(
            "test fixture seeds an idol before exercising the idol branch");
        DeathPenalty.ConsumeSacrificialIdol(backpack);

        CountOccupied(backpack).Should().Be(itemsBefore - 1, "only the idol is gone");
        DeathPenalty.HasSacrificialIdol(backpack).Should().BeFalse();
        backpack.Gold.Should().Be(500, "idol is a free Save Both — backpack gold survives");
    }

    // ── Equipment-loss primitive (EquipmentSet.DestroyRandomEquipped) ───────
    // Copilot PR #35 round-2 flagged that "gear" was in the PR title but no
    // test covered the equipment-mutation primitive. Deterministic here by
    // giving EquipmentSet exactly one equipped item — rng.Next(1) always
    // returns 0, so DestroyRandomEquipped has no choice in which slot to hit.

    [Fact]
    public void DestroyRandomEquipped_WithOneItem_RemovesThatItemDeterministically()
    {
        var equip = new EquipmentSet();
        var chest = ItemDatabase.Get("body_warrior_armor_t1");
        chest.Should().NotBeNull("fixture item must exist in ItemDatabase");
        equip.ForceEquip(EquipSlot.Body, chest!);

        var rng = new System.Random(12345);
        var destroyed = equip.DestroyRandomEquipped(rng);

        destroyed.Should().NotBeNull("an item was equipped, so the primitive must destroy something");
        destroyed!.Id.Should().Be(chest!.Id, "the only equipped item was the one destroyed");
        equip.Body.Should().BeNull("Body slot is cleared after destruction");
    }

    [Fact]
    public void DestroyRandomEquipped_WithNothingEquipped_ReturnsNull()
    {
        var equip = new EquipmentSet();
        var rng = new System.Random(12345);
        equip.DestroyRandomEquipped(rng).Should().BeNull(
            "primitive must no-op when the caller's character has no equipment");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static int CountOccupied(Inventory inv)
    {
        int n = 0;
        for (int i = 0; i < inv.SlotCount; i++)
            if (inv.GetSlot(i) != null) n++;
        return n;
    }

    /// <summary>Total items across all slots (sum of stack counts).
    /// Differs from CountOccupied because ApplyItemLoss decrements stack
    /// counts one at a time — so a slot may still be "occupied" with fewer
    /// items after a lossy call.</summary>
    private static long CountUnits(Inventory inv)
    {
        long n = 0;
        for (int i = 0; i < inv.SlotCount; i++)
        {
            var stack = inv.GetSlot(i);
            if (stack != null) n += stack.Count;
        }
        return n;
    }
}
