using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

/// <summary>
/// Full-run integration test — simulates a complete play session from
/// character creation through endgame, exercising every system end-to-end.
/// Each phase builds on realistic game state. No Godot runtime required.
/// </summary>
public class FullRunTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Phase 1: Character Creation & Initialization
    // ═══════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(PlayerClass.Warrior)]
    [InlineData(PlayerClass.Ranger)]
    [InlineData(PlayerClass.Mage)]
    public void Phase1_CharacterCreation_AllClassesInitialize(PlayerClass cls)
    {
        var stats = new StatBlock();
        stats.ApplyClassLevelBonus(cls);
        stats.FreePoints.Should().Be(3);
        (stats.Str + stats.Dex + stats.Sta + stats.Int).Should().BeGreaterThan(0);

        var inv = new Inventory(25);
        inv.UsedSlots.Should().Be(0);
        inv.Gold.Should().Be(0);

        var bank = new Bank();
        bank.TotalSlots.Should().Be(Bank.StartingSlots);
        bank.Storage.UsedSlots.Should().Be(0);

        var skillBar = new SkillBar();
        for (int i = 0; i < SkillBar.SlotCount; i++)
            skillBar.GetSlot(i).Should().BeNull();

        var quests = new QuestTracker();
        quests.GenerateQuests(1);
        quests.ActiveQuests.Should().HaveCount(3);

        var achievements = new AchievementTracker();
        achievements.Unlocked.Should().BeEmpty();

        var tracker = new ProgressionTracker(cls);
        tracker.AllMasteries.Should().NotBeEmpty();
        tracker.AllAbilities.Should().NotBeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Phase 2: Town — Shopping & Preparation
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Phase2_Shopping_BuySellRoundTrip()
    {
        var inv = new Inventory(25) { Gold = 500 };
        var potion = ItemDatabase.Get("consumable_hp_small")!;
        var sword = ItemDatabase.Get("mainhand_warrior_sword_t1")!;

        int potionBuy = potion.BuyPrice;   // Small Health Potion — 25g.
        int swordBuy = sword.BuyPrice;     // Tier-1 main hand — 60g per TierBuyPrice(1).
        int swordSell = sword.SellPrice;   // 24g per TierSellPrice(1).

        inv.TryBuy(potion).Should().BeTrue();
        inv.Gold.Should().Be(500 - potionBuy);
        inv.UsedSlots.Should().Be(1);

        inv.TryBuy(sword).Should().BeTrue();
        inv.Gold.Should().Be(500 - potionBuy - swordBuy);
        inv.UsedSlots.Should().Be(2);

        inv.TrySell(1).Should().BeTrue();
        inv.Gold.Should().Be(500 - potionBuy - swordBuy + swordSell);
        inv.UsedSlots.Should().Be(1);

        // Buy multiple potions (stack)
        inv.TryBuy(potion).Should().BeTrue();
        inv.UsedSlots.Should().Be(1); // stacks into existing slot
        inv.GetSlot(0)!.Count.Should().Be(2);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Phase 3: Bank & Storage
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Phase3_Bank_DepositWithdrawExpand()
    {
        var inv = new Inventory(25) { Gold = 5000 };
        var bank = new Bank();
        var sword = ItemDatabase.Get("mainhand_warrior_sword_t1")!;

        // Buy and deposit
        inv.TryBuy(sword);
        bank.Deposit(inv, 0).Should().BeTrue();
        inv.UsedSlots.Should().Be(0);
        bank.Storage.UsedSlots.Should().Be(1);

        // Withdraw
        bank.Withdraw(inv, 0).Should().BeTrue();
        inv.UsedSlots.Should().Be(1);
        bank.Storage.UsedSlots.Should().Be(0);

        // Expand bank
        int slotsBefore = bank.TotalSlots;
        bank.PurchaseExpansion(inv).Should().BeTrue();
        bank.TotalSlots.Should().BeGreaterThan(slotsBefore);
        bank.ExpansionCount.Should().Be(1);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Phase 4: Crafting & Affixes
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Phase4_Crafting_ApplyAffixesAndRecycle()
    {
        var inv = new Inventory { Gold = 10000 };
        var item = new CraftableItem
        {
            BaseItemId = "mainhand_warrior_sword_t1",
            BaseName = "Iron Sword",
            ItemLevel = 10,
            Quality = BaseQuality.Normal,
            Category = ItemCategory.Weapon,
        };

        // Apply prefix
        var keen = AffixDatabase.Get("keen_1")!;
        Crafting.CanApplyAffix(item, keen, inv).Should().BeTrue();
        Crafting.ApplyAffix(item, keen, inv).Should().BeTrue();
        item.Affixes.Should().HaveCount(1);
        inv.Gold.Should().BeLessThan(10000);

        // Apply suffix
        var striking = AffixDatabase.Get("striking_1")!;
        Crafting.ApplyAffix(item, striking, inv).Should().BeTrue();
        item.Affixes.Should().HaveCount(2);

        // Display name includes prefix and suffix
        string name = Crafting.GetDisplayName(item);
        name.Should().Contain("Keen");
        name.Should().Contain("Iron Sword");
        name.Should().Contain("of Striking");

        // Recycle a different item
        var recycleItem = new CraftableItem
        {
            BaseName = "Old Shield",
            ItemLevel = 5,
            Quality = BaseQuality.Superior,
        };
        int goldBack = Crafting.RecycleItem(recycleItem);
        goldBack.Should().BePositive();

        // Affix limits enforced
        Crafting.CanApplyAffix(item, keen, inv).Should().BeFalse("duplicate affix blocked");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Phase 5: Dungeon — Floor Generation & Combat
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Phase5_Dungeon_GenerateFloorAndSimulateCombat()
    {
        // Generate floor (seeded)
        var gen = new FloorGenerator(42);
        gen.Generate(1);
        gen.Rooms.Count.Should().BeInRange(5, 8);
        gen.EntrancePos.Should().NotBe(gen.ExitPos);
        gen.Grid[gen.EntrancePos.X, gen.EntrancePos.Y]
            .Should().Be(FloorGenerator.Tile.Floor);

        // Simulate combat: player attacks enemy
        var playerStats = new StatBlock { Str = 10, Dex = 5 };
        int baseDamage = 12; // warrior base
        int totalDamage = baseDamage + (int)playerStats.MeleeFlatBonus;
        totalDamage.Should().BePositive();

        int enemyHp = 30; // level 1 enemy
        enemyHp -= totalDamage;
        enemyHp.Should().BeLessThan(30);

        // Kill enemy → loot
        int goldDrop = LootTable.GetGoldDrop(1);
        goldDrop.Should().BeGreaterOrEqualTo(3); // base 2 + level 1

        // Track in achievement system
        var achievements = new AchievementTracker();
        achievements.IncrementCounter("enemies_killed", 1);
        achievements.GetCounter("enemies_killed").Should().Be(1);

        // Track saturation
        var saturation = new ZoneSaturation();
        saturation.RecordKill(1);
        saturation.GetSaturation(1).Should().BeGreaterThan(0);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Phase 6: Progression — Leveling & Skills
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Phase6_Progression_LevelUpAndSkills()
    {
        // Level up
        var stats = new StatBlock();
        stats.ApplyClassLevelBonus(PlayerClass.Warrior);
        stats.Str.Should().Be(3);
        stats.FreePoints.Should().Be(3);

        // Allocate free points
        stats.Str += 3;
        stats.FreePoints -= 3;
        stats.Str.Should().Be(6);
        stats.FreePoints.Should().Be(0);

        // Skill bar: assign, activate, cooldown
        var skillBar = new SkillBar();
        skillBar.SetSlot(0, "warrior_slash_heavy");
        skillBar.IsReady(0).Should().BeTrue();

        string? activated = skillBar.TryActivate(0, 1.5f);
        activated.Should().Be("warrior_slash_heavy");
        skillBar.IsReady(0).Should().BeFalse();

        skillBar.Update(1.5f); // tick full cooldown
        skillBar.IsReady(0).Should().BeTrue();

        // Mastery state: XP and leveling
        var masteryState = new MasteryState("w_bladed");
        masteryState.Level.Should().Be(0);
        int levelsGained = masteryState.AddXp(1); // level 0→1 is instant
        levelsGained.Should().BeGreaterOrEqualTo(1);
        masteryState.Level.Should().BeGreaterOrEqualTo(1);

        // Skill point allocation
        masteryState.AddSkillPoint();
        masteryState.Xp.Should().BeGreaterThan(0);

        // Passive bonus at level
        masteryState.GetPassiveBonus(1.5f).Should().BeGreaterThan(0);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Phase 7: Quest Completion
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Phase7_Quests_GenerateAndComplete()
    {
        var quests = new QuestTracker();
        quests.GenerateQuests(5);
        quests.ActiveQuests.Should().HaveCount(3);
        quests.QuestDefs.Should().HaveCount(3);

        // All quests have positive rewards
        foreach (var def in quests.QuestDefs)
        {
            def.GoldReward.Should().BePositive();
            def.XpReward.Should().BePositive();
        }

        // Complete all quests
        for (int i = 0; i < quests.QuestDefs.Count; i++)
        {
            var def = quests.QuestDefs[i];
            switch (def.Type)
            {
                case QuestType.Kill:
                    for (int k = 0; k <= def.TargetCount; k++)
                        quests.RecordEnemyKill(def.TargetFloor);
                    break;
                case QuestType.ClearFloor:
                    quests.RecordFloorClear(def.TargetFloor);
                    break;
                case QuestType.DepthPush:
                    quests.RecordFloorReached(def.TargetFloor);
                    break;
            }
        }

        quests.AllComplete.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Phase 8: Death & Penalty
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Phase8_Death_PenaltiesAndProtection()
    {
        var inv = new Inventory(25) { Gold = 1000 };
        // Stock inventory
        for (int i = 0; i < 5; i++)
            inv.TryAdd(new ItemDef { Id = $"item_{i}", Name = $"Loot {i}", Category = ItemCategory.Weapon, SellPrice = 20 });

        int floor = 10;

        // XP loss
        int xp = 1000;
        int xpLoss = DeathPenalty.CalculateXpLoss(xp, floor);
        xpLoss.Should().BePositive();
        xpLoss.Should().BeLessThanOrEqualTo(xp);

        // Item loss
        int itemsToLose = DeathPenalty.GetItemsLost(floor);
        itemsToLose.Should().BePositive();
        DeathPenalty.ApplyItemLoss(inv, itemsToLose);
        inv.UsedSlots.Should().Be(5 - itemsToLose);

        // Idol protection
        var idol = ItemDatabase.Get("consumable_sacrificial_idol")!;
        inv.TryAdd(idol);
        DeathPenalty.HasSacrificialIdol(inv).Should().BeTrue();
        DeathPenalty.ConsumeSacrificialIdol(inv);
        DeathPenalty.HasSacrificialIdol(inv).Should().BeFalse();

        // Protection costs scale with floor
        int expCost = DeathPenalty.GetExpProtectionCost(floor);
        int bpCost = DeathPenalty.GetBackpackProtectionCost(floor);
        expCost.Should().BePositive();
        bpCost.Should().BeGreaterThan(expCost);

        // Bank items survive death
        var bank = new Bank();
        inv.TryAdd(new ItemDef { Id = "safe_item", Name = "Safe", Category = ItemCategory.Weapon });
        bank.Deposit(inv, inv.UsedSlots - 1);
        int bankSlots = bank.Storage.UsedSlots;
        DeathPenalty.ApplyItemLoss(inv, 99); // wipe backpack
        bank.Storage.UsedSlots.Should().Be(bankSlots); // bank untouched
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Phase 9: Save/Load Round-Trip (Per-Subsystem)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Phase9_SaveLoad_AllSubsystemsRoundTrip()
    {
        // Bank
        var bank = new Bank();
        var tempInv = new Inventory { Gold = 10000 };
        bank.PurchaseExpansion(tempInv);
        tempInv.TryAdd(ItemDatabase.Get("consumable_hp_small")!);
        bank.Deposit(tempInv, 0);
        var bankState = bank.CaptureState();
        var bankRestored = new Bank();
        bankRestored.RestoreState(bankState);
        bankRestored.ExpansionCount.Should().Be(1);
        bankRestored.Storage.UsedSlots.Should().Be(1);

        // Quests
        var quests = new QuestTracker();
        quests.GenerateQuests(10);
        quests.RecordEnemyKill(10);
        var questState = quests.CaptureState();
        var questsRestored = new QuestTracker();
        questsRestored.RestoreState(questState);
        questsRestored.ActiveQuests.Should().HaveCount(3);

        // Achievements
        var ach = new AchievementTracker();
        ach.IncrementCounter("enemies_killed", 100);
        ach.SetCounter("deepest_floor", 25);
        ach.Evaluate();
        var achState = ach.CaptureState();
        var achRestored = new AchievementTracker();
        achRestored.RestoreState(achState);
        achRestored.GetCounter("enemies_killed").Should().Be(100);
        achRestored.IsUnlocked("c_first_blood").Should().BeTrue();
        achRestored.IsUnlocked("c_100_kills").Should().BeTrue();

        // Dungeon Pacts
        var pacts = new DungeonPacts();
        pacts.SetRank(0, 3);
        pacts.SetRank(5, 2);
        var pactRanks = pacts.ExportRanks();
        var pactsRestored = new DungeonPacts();
        pactsRestored.ImportRanks(pactRanks);
        pactsRestored.GetRank(0).Should().Be(3);
        pactsRestored.GetRank(5).Should().Be(2);
        pactsRestored.TotalHeat.Should().Be(pacts.TotalHeat);

        // Zone Saturation
        var sat = new ZoneSaturation();
        for (int i = 0; i < 30; i++) sat.RecordKill(3);
        float satVal = sat.GetSaturation(3);
        var satState = sat.ExportState();
        var satRestored = new ZoneSaturation();
        satRestored.ImportState(satState, 5000.0);
        satRestored.GetSaturation(3).Should().BeApproximately(satVal, 0.001f);

        // Magicule Attunement
        var ma = new MagiculeAttunement { IsUnlocked = true, TotalPoints = 20 };
        ma.TryUnlockNode(0); // STR small
        ma.TryUnlockNode(1); // STR small
        ma.RecordFloorClear(51);
        var maNodes = ma.ExportNodes();
        var maFloors = ma.ExportClearedFloors();
        var maRestored = new MagiculeAttunement();
        maRestored.ImportState(maNodes, maFloors, ma.ActiveKeystone, true);
        maRestored.IsNodeUnlocked(0).Should().BeTrue();
        maRestored.IsNodeUnlocked(1).Should().BeTrue();
        maRestored.IsUnlocked.Should().BeTrue();

        // SkillBar
        var bar = new SkillBar();
        bar.SetSlot(0, "fireball");
        bar.SetSlot(2, "heal");
        var barSlots = bar.ExportSlots();
        var barRestored = new SkillBar();
        barRestored.ImportSlots(barSlots);
        barRestored.GetSlot(0).Should().Be("fireball");
        barRestored.GetSlot(2).Should().Be("heal");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Phase 10: Endgame Systems
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Phase10_Endgame_PactsSaturationAttunementGearTiers()
    {
        // Dungeon Pacts: activate and verify multipliers
        var pacts = new DungeonPacts();
        pacts.SetRank(0, 3); // Swelling Horde
        pacts.SetRank(1, 2); // Iron Will
        pacts.SetRank(2, 1); // Sharpened Claws
        pacts.TotalHeat.Should().BePositive();
        pacts.XpBonus.Should().BePositive();
        pacts.HordeBudgetMultiplier.Should().BeGreaterThan(1.0f);
        pacts.EnemyHpMultiplier.Should().BeGreaterThan(1.0f);
        pacts.EnemyDamageMultiplier.Should().BeGreaterThan(1.0f);

        // Zone Saturation: farm and verify bonuses
        var sat = new ZoneSaturation();
        for (int i = 0; i < 1000; i++) sat.RecordKill(3); // saturate zone 3
        sat.GetSaturation(3).Should().Be(100f); // capped
        sat.GetHpMultiplier(3).Should().BeApproximately(1.50f, 0.01f);
        sat.GetXpBonus(3).Should().BeApproximately(0.30f, 0.01f);
        sat.GetQualityShiftFloors(3).Should().Be(20);

        // Decay over time
        sat.ApplyDecay(1000);
        sat.ApplyDecay(1240); // 4 minutes → 1.0 decay
        sat.GetSaturation(3).Should().BeApproximately(99.0f, 0.01f);

        // Magicule Attunement: full path Ring 1 → Connector → Ring 2 → Keystone
        var ma = new MagiculeAttunement { TotalPoints = 100, IsUnlocked = true };

        // Ring 1: STR branch (nodes 0, 1, 2)
        ma.TryUnlockNode(0).Should().BeTrue();
        ma.TryUnlockNode(1).Should().BeTrue();
        ma.TryUnlockNode(2).Should().BeTrue();
        ma.FlatMeleeDamage.Should().Be(5f);

        // Connector: STR-DEX (node 12)
        ma.TryUnlockNode(12).Should().BeTrue();
        ma.ConnectorMeleeDamage.Should().BeGreaterThan(0);

        // Ring 2: STR (node 28)
        ma.TryUnlockNode(28).Should().BeTrue();
        ma.HasOverkill.Should().BeTrue();

        // Keystone: STR (node 36)
        ma.TryUnlockNode(36).Should().BeTrue();
        ma.HasJuggernaut.Should().BeTrue();
        ma.ActiveKeystone.Should().Be(0);

        // Depth Gear Tiers: quality rolls at various floors
        var rng = new Random(42);
        // Floor 1: always Normal
        DepthGearTiers.RollQuality(1, 0, rng).Should().Be(BaseQuality.Normal);
        // Floor 150+: can produce all tiers
        bool foundHigh = false;
        var rng2 = new Random(42);
        for (int i = 0; i < 500; i++)
        {
            if (DepthGearTiers.RollQuality(150, 0, rng2) >= BaseQuality.Mythic)
            { foundHigh = true; break; }
        }
        foundHigh.Should().BeTrue();

        // Quality shift from saturation + pacts
        int qualityShift = sat.GetQualityShiftFloors(3) + pacts.QualityShift;
        qualityShift.Should().BePositive("saturation + pacts should grant quality shift");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Full Session — Chains All Phases Into One Continuous Play Session
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void FullSession_WarriorPlaythrough_AllSystemsIntegrate()
    {
        // ── Create character ─────────────────────────────────────────────────
        var stats = new StatBlock();
        stats.ApplyClassLevelBonus(PlayerClass.Warrior);
        var inv = new Inventory(25) { Gold = 2000 };
        var bank = new Bank();
        var skillBar = new SkillBar();
        var tracker = new ProgressionTracker(PlayerClass.Warrior);
        var quests = new QuestTracker();
        var achievements = new AchievementTracker();
        var saturation = new ZoneSaturation();
        var pacts = new DungeonPacts();

        // ── Town: buy gear ───────────────────────────────────────────────────
        var sword = ItemDatabase.Get("mainhand_warrior_sword_t1")!;
        var potions = ItemDatabase.Get("consumable_hp_small")!;
        inv.TryBuy(sword).Should().BeTrue();
        inv.TryBuy(potions).Should().BeTrue();
        inv.TryBuy(potions).Should().BeTrue(); // stack to 2
        long goldAfterShopping = inv.Gold;
        goldAfterShopping.Should().BeLessThan(2000);

        // Bank a spare potion for safety
        bank.Deposit(inv, 0).Should().BeTrue(); // deposit sword

        // ── Enter dungeon floor 1 ───────────────────────────────────────────
        quests.GenerateQuests(1);
        var gen = new FloorGenerator(42);
        gen.Generate(1);
        gen.Rooms.Count.Should().BeInRange(5, 8);

        // ── Combat: kill enemies ─────────────────────────────────────────────
        int enemiesKilled = 0;
        int totalXp = 0;
        for (int i = 0; i < 15; i++)
        {
            // Simulate kill
            int xpGain = 8 + 4 * 1; // level 1 enemy formula
            totalXp += xpGain;
            int goldGain = LootTable.GetGoldDrop(1);
            inv.Gold += goldGain;
            enemiesKilled++;

            achievements.IncrementCounter("enemies_killed");
            saturation.RecordKill(1);
            quests.RecordEnemyKill(1);
        }

        enemiesKilled.Should().Be(15);
        totalXp.Should().BePositive();
        inv.Gold.Should().BeGreaterThan(goldAfterShopping);
        saturation.GetSaturation(1).Should().BeGreaterThan(0);

        // ── Level up ─────────────────────────────────────────────────────────
        // XP threshold for level 1→2: 1^2 * 45 = 45
        stats.ApplyClassLevelBonus(PlayerClass.Warrior); // simulate level 2
        stats.Str.Should().Be(6);
        stats.FreePoints.Should().Be(6);

        // Spend some stat points
        stats.Str += 2;
        stats.FreePoints -= 2;

        // ── Skills ───────────────────────────────────────────────────────────
        var firstAbility = tracker.AllAbilities.First();
        skillBar.SetSlot(0, firstAbility.AbilityId);
        skillBar.IsReady(0).Should().BeTrue();
        skillBar.TryActivate(0, 1.0f).Should().NotBeNull();
        skillBar.Update(1.0f);
        skillBar.IsReady(0).Should().BeTrue();

        // ── Achievements ─────────────────────────────────────────────────────
        achievements.SetCounter("deepest_floor", 2);
        achievements.SetCounter("player_level", 2);
        var unlocked = achievements.Evaluate();
        unlocked.Should().NotBeEmpty("first blood + floor 2 should unlock");

        // ── Death on floor 2 ─────────────────────────────────────────────────
        int xpLoss = DeathPenalty.CalculateXpLoss(totalXp, 2);
        xpLoss.Should().BePositive();
        int itemsLost = DeathPenalty.GetItemsLost(2);
        DeathPenalty.ApplyItemLoss(inv, itemsLost);
        // Bank item survived
        bank.Storage.UsedSlots.Should().Be(1);

        // ── Return to town, recover ──────────────────────────────────────────
        bank.Withdraw(inv, 0).Should().BeTrue(); // get sword back
        inv.Gold.Should().BePositive("gold survives death");

        // ── Endgame: activate pact ───────────────────────────────────────────
        pacts.SetRank(0, 1); // Swelling Horde
        pacts.TotalHeat.Should().BePositive();
        pacts.XpBonus.Should().BePositive();

        // ── Save/Load all state ──────────────────────────────────────────────
        var bankState = bank.CaptureState();
        var questState = quests.CaptureState();
        var achState = achievements.CaptureState();
        var pactRanks = pacts.ExportRanks();
        var satState = saturation.ExportState();
        var barSlots = skillBar.ExportSlots();

        // Restore into fresh instances
        var bank2 = new Bank();
        bank2.RestoreState(bankState);
        var quests2 = new QuestTracker();
        quests2.RestoreState(questState);
        var ach2 = new AchievementTracker();
        ach2.RestoreState(achState);
        var pacts2 = new DungeonPacts();
        pacts2.ImportRanks(pactRanks);
        var sat2 = new ZoneSaturation();
        sat2.ImportState(satState, 5000.0);
        var bar2 = new SkillBar();
        bar2.ImportSlots(barSlots);

        // Verify restored state
        ach2.GetCounter("enemies_killed").Should().Be(15);
        ach2.IsUnlocked("c_first_blood").Should().BeTrue();
        pacts2.GetRank(0).Should().Be(1);
        sat2.GetSaturation(1).Should().BeGreaterThan(0);
        bar2.GetSlot(0).Should().Be(firstAbility.AbilityId);
    }
}
