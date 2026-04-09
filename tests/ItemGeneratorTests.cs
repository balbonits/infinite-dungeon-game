using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace DungeonGame.Tests;

public class ItemGeneratorTests
{
    // Use a fixed seed for deterministic tests where exact values matter,
    // and loop-based random seeds for statistical distribution tests.

    // ═══════════════════════════════════════════════════════════════
    //  GenerateEquipment — ItemLevel matches floor
    // ═══════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(25)]
    [InlineData(50)]
    [InlineData(100)]
    public void GenerateEquipment_ItemLevel_MatchesFloor(int floor)
    {
        var rng = new Random(42);
        var item = ItemGenerator.GenerateEquipment(floor, rng);
        Assert.Equal(floor, item.ItemLevel);
    }

    [Fact]
    public void GenerateEquipment_FloorZero_ItemLevelIsAtLeast1()
    {
        var rng = new Random(42);
        var item = ItemGenerator.GenerateEquipment(0, rng);
        Assert.True(item.ItemLevel >= 1);
    }

    // ═══════════════════════════════════════════════════════════════
    //  GenerateEquipment — Quality distribution
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void GenerateEquipment_Floor1_AlwaysNormal()
    {
        int normalCount = 0;
        for (int i = 0; i < 500; i++)
        {
            var rng = new Random(i);
            var item = ItemGenerator.GenerateEquipment(1, rng);
            if (item.Quality == ItemQuality.Normal) normalCount++;
        }
        // Floor 1-9: 100% Normal per spec
        Assert.Equal(500, normalCount);
    }

    [Fact]
    public void GenerateEquipment_Floor5_AlwaysNormal()
    {
        for (int i = 0; i < 200; i++)
        {
            var rng = new Random(i);
            var item = ItemGenerator.GenerateEquipment(5, rng);
            Assert.Equal(ItemQuality.Normal, item.Quality);
        }
    }

    [Fact]
    public void GenerateEquipment_Floor10_CanBeSuperior()
    {
        int superiorCount = 0;
        for (int i = 0; i < 1000; i++)
        {
            var rng = new Random(i);
            var item = ItemGenerator.GenerateEquipment(10, rng);
            if (item.Quality == ItemQuality.Superior) superiorCount++;
        }
        // Floor 10-24: 20% Superior. With 1000 rolls, expect ~200.
        Assert.True(superiorCount > 100, $"Expected some Superior items at floor 10, got {superiorCount}");
        Assert.True(superiorCount < 350, $"Too many Superior items at floor 10: {superiorCount}");
    }

    [Fact]
    public void GenerateEquipment_Floor10_NeverElite()
    {
        for (int i = 0; i < 500; i++)
        {
            var rng = new Random(i);
            var item = ItemGenerator.GenerateEquipment(10, rng);
            Assert.NotEqual(ItemQuality.Elite, item.Quality);
        }
    }

    [Fact]
    public void GenerateEquipment_Floor25_CanBeElite()
    {
        int eliteCount = 0;
        for (int i = 0; i < 2000; i++)
        {
            var rng = new Random(i);
            var item = ItemGenerator.GenerateEquipment(25, rng);
            if (item.Quality == ItemQuality.Elite) eliteCount++;
        }
        // Floor 25-49: 5% Elite. With 2000 rolls, expect ~100.
        Assert.True(eliteCount > 30, $"Expected some Elite items at floor 25, got {eliteCount}");
        Assert.True(eliteCount < 200, $"Too many Elite items at floor 25: {eliteCount}");
    }

    [Fact]
    public void GenerateEquipment_NormalMostCommon_EliteRarest()
    {
        // Use floor 50 where all qualities are possible
        int normal = 0, superior = 0, elite = 0;
        for (int i = 0; i < 2000; i++)
        {
            var rng = new Random(i);
            var item = ItemGenerator.GenerateEquipment(50, rng);
            switch (item.Quality)
            {
                case ItemQuality.Normal: normal++; break;
                case ItemQuality.Superior: superior++; break;
                case ItemQuality.Elite: elite++; break;
            }
        }
        // Floor 50-74: Normal 40%, Superior 45%, Elite 15%
        Assert.True(elite < superior, $"Elite ({elite}) should be rarer than Superior ({superior})");
        Assert.True(elite < normal, $"Elite ({elite}) should be rarer than Normal ({normal})");
    }

    // ═══════════════════════════════════════════════════════════════
    //  GenerateEquipment — Stats correctness
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void GenerateEquipment_Weapon_HasDamage_NoDefense()
    {
        // Generate many items to ensure we hit a weapon
        for (int i = 0; i < 100; i++)
        {
            var rng = new Random(i);
            var item = ItemGenerator.GenerateEquipment(10, rng);
            if (item.Type == ItemType.Weapon)
            {
                Assert.True(item.Damage > 0, "Weapon should have positive damage");
                Assert.Equal(0, item.Defense);
                Assert.Equal(EquipSlot.MainHand, item.Slot);
                return; // found and verified
            }
        }
        Assert.Fail("No weapon generated in 100 attempts");
    }

    [Fact]
    public void GenerateEquipment_Armor_HasDefense_NoDamage()
    {
        for (int i = 0; i < 100; i++)
        {
            var rng = new Random(i);
            var item = ItemGenerator.GenerateEquipment(10, rng);
            if (item.Type == ItemType.Armor)
            {
                Assert.True(item.Defense > 0, "Armor should have positive defense");
                Assert.Equal(0, item.Damage);
                Assert.NotEqual(EquipSlot.None, item.Slot);
                return;
            }
        }
        Assert.Fail("No armor generated in 100 attempts");
    }

    [Fact]
    public void GenerateEquipment_HighFloor_HigherStatsThanLowFloor()
    {
        // Collect average damage/defense for floor 1 vs floor 50
        int lowFloorTotal = 0, highFloorTotal = 0;
        int lowCount = 0, highCount = 0;
        for (int i = 0; i < 500; i++)
        {
            var rng1 = new Random(i);
            var low = ItemGenerator.GenerateEquipment(1, rng1);
            lowFloorTotal += low.Damage + low.Defense;
            lowCount++;

            var rng2 = new Random(i + 10000);
            var high = ItemGenerator.GenerateEquipment(50, rng2);
            highFloorTotal += high.Damage + high.Defense;
            highCount++;
        }
        double lowAvg = (double)lowFloorTotal / lowCount;
        double highAvg = (double)highFloorTotal / highCount;
        Assert.True(highAvg > lowAvg * 2,
            $"Floor 50 avg stats ({highAvg:F1}) should be much higher than floor 1 ({lowAvg:F1})");
    }

    [Fact]
    public void GenerateEquipment_ItemLevelNeverExceedsFloor()
    {
        for (int floor = 1; floor <= 100; floor += 10)
        {
            var rng = new Random(floor);
            var item = ItemGenerator.GenerateEquipment(floor, rng);
            Assert.True(item.ItemLevel <= floor,
                $"ItemLevel {item.ItemLevel} should not exceed floor {floor}");
        }
    }

    [Fact]
    public void GenerateEquipment_ValueScalesWithQualityAndLevel()
    {
        // Normal floor 1 should be cheaper than Elite floor 50
        var rng1 = new Random(42);
        var cheapItem = ItemGenerator.GenerateEquipment(1, rng1);

        // Find an Elite item on floor 50
        ItemData? expensiveItem = null;
        for (int i = 0; i < 10000; i++)
        {
            var rng = new Random(i);
            var item = ItemGenerator.GenerateEquipment(50, rng);
            if (item.Quality == ItemQuality.Elite)
            {
                expensiveItem = item;
                break;
            }
        }

        Assert.NotNull(expensiveItem);
        Assert.True(expensiveItem.Value > cheapItem.Value,
            $"Elite floor-50 item (value {expensiveItem.Value}) should be worth more than Normal floor-1 (value {cheapItem.Value})");
    }

    [Fact]
    public void GenerateEquipment_IsNotStackable()
    {
        var rng = new Random(42);
        var item = ItemGenerator.GenerateEquipment(10, rng);
        Assert.False(item.Stackable);
        Assert.Equal(1, item.StackCount);
    }

    [Fact]
    public void GenerateEquipment_HasNoPrefixesOrSuffixes()
    {
        var rng = new Random(42);
        var item = ItemGenerator.GenerateEquipment(50, rng);
        Assert.Empty(item.Prefixes);
        Assert.Empty(item.Suffixes);
    }

    // ═══════════════════════════════════════════════════════════════
    //  GenerateMaterial
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void GenerateMaterial_IsStackable()
    {
        var rng = new Random(42);
        var mat = ItemGenerator.GenerateMaterial(5, rng);
        Assert.True(mat.Stackable);
        Assert.Equal(ItemType.Material, mat.Type);
        Assert.Equal(EquipSlot.None, mat.Slot);
    }

    [Fact]
    public void GenerateMaterial_ItemLevelMatchesFloor()
    {
        var rng = new Random(42);
        var mat = ItemGenerator.GenerateMaterial(30, rng);
        Assert.Equal(30, mat.ItemLevel);
    }

    [Fact]
    public void GenerateMaterial_HighFloor_HigherValue()
    {
        var rng1 = new Random(42);
        var lowMat = ItemGenerator.GenerateMaterial(1, rng1);
        var rng2 = new Random(42);
        var highMat = ItemGenerator.GenerateMaterial(80, rng2);
        Assert.True(highMat.Value > lowMat.Value,
            $"Floor 80 material value ({highMat.Value}) should exceed floor 1 ({lowMat.Value})");
    }

    [Fact]
    public void GenerateMaterial_HasNoDamageOrDefense()
    {
        var rng = new Random(42);
        var mat = ItemGenerator.GenerateMaterial(50, rng);
        Assert.Equal(0, mat.Damage);
        Assert.Equal(0, mat.Defense);
    }

    // ═══════════════════════════════════════════════════════════════
    //  GenerateConsumable
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void GenerateConsumable_ReturnsValidConsumable()
    {
        var rng = new Random(42);
        var item = ItemGenerator.GenerateConsumable(rng);
        Assert.Equal(ItemType.Consumable, item.Type);
        Assert.Equal(EquipSlot.None, item.Slot);
        Assert.True(item.Stackable);
    }

    [Fact]
    public void GenerateConsumable_ProducesAllTypes()
    {
        bool foundHP = false, foundMP = false, foundIdol = false;
        for (int i = 0; i < 1000; i++)
        {
            var rng = new Random(i);
            var item = ItemGenerator.GenerateConsumable(rng);
            if (item.Name == "Health Potion") foundHP = true;
            else if (item.Name == "Mana Potion") foundMP = true;
            else if (item.Name == "Sacrificial Idol") foundIdol = true;
        }
        Assert.True(foundHP, "Should produce Health Potions");
        Assert.True(foundMP, "Should produce Mana Potions");
        Assert.True(foundIdol, "Should produce Sacrificial Idols");
    }

    [Fact]
    public void GenerateConsumable_HealthPotion_HasHPBonus()
    {
        // Find a health potion
        for (int i = 0; i < 100; i++)
        {
            var rng = new Random(i);
            var item = ItemGenerator.GenerateConsumable(rng);
            if (item.Name == "Health Potion")
            {
                Assert.True(item.HPBonus > 0);
                Assert.Equal(0, item.MPBonus);
                return;
            }
        }
        Assert.Fail("No Health Potion found in 100 attempts");
    }

    [Fact]
    public void GenerateConsumable_ManaPotion_HasMPBonus()
    {
        for (int i = 0; i < 100; i++)
        {
            var rng = new Random(i);
            var item = ItemGenerator.GenerateConsumable(rng);
            if (item.Name == "Mana Potion")
            {
                Assert.True(item.MPBonus > 0);
                Assert.Equal(0, item.HPBonus);
                return;
            }
        }
        Assert.Fail("No Mana Potion found in 100 attempts");
    }

    // ═══════════════════════════════════════════════════════════════
    //  RollLootDrop — Tier-based drop rates
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void RollLootDrop_RespectsDropRates_Tier1()
    {
        int drops = 0;
        int trials = 10000;
        for (int i = 0; i < trials; i++)
        {
            var rng = new Random(i);
            var loot = ItemGenerator.RollLootDrop(1, 1, rng);
            if (loot != null) drops++;
        }
        // Tier1 floor 1: equipment 8.1% + material 25% (independent, only on equip miss ~23%)
        // Total drop rate: ~8.1% + (1-0.081)*25% = ~31%
        double rate = (double)drops / trials;
        Assert.True(rate > 0.20, $"Tier 1 floor 1 drop rate too low: {rate:P1}");
        Assert.True(rate < 0.45, $"Tier 1 floor 1 drop rate too high: {rate:P1}");
    }

    [Fact]
    public void RollLootDrop_HigherTier_HigherEquipmentDropRate()
    {
        int tier1Equip = 0, tier3Equip = 0;
        int trials = 10000;
        for (int i = 0; i < trials; i++)
        {
            var rng1 = new Random(i);
            var loot1 = ItemGenerator.RollLootDrop(1, 10, rng1);
            if (loot1 != null && (loot1.Type == ItemType.Weapon || loot1.Type == ItemType.Armor))
                tier1Equip++;

            var rng3 = new Random(i + 50000);
            var loot3 = ItemGenerator.RollLootDrop(3, 10, rng3);
            if (loot3 != null && (loot3.Type == ItemType.Weapon || loot3.Type == ItemType.Armor))
                tier3Equip++;
        }
        Assert.True(tier3Equip > tier1Equip,
            $"Tier 3 equipment drops ({tier3Equip}) should exceed Tier 1 ({tier1Equip})");
    }

    [Fact]
    public void RollLootDrop_CanReturnNull()
    {
        int nullCount = 0;
        for (int i = 0; i < 1000; i++)
        {
            var rng = new Random(i);
            if (ItemGenerator.RollLootDrop(1, 1, rng) == null)
                nullCount++;
        }
        Assert.True(nullCount > 500, $"Expected many null drops, got {nullCount}/1000");
    }

    [Fact]
    public void RollLootDrop_CanDropEquipment()
    {
        bool foundEquipment = false;
        for (int i = 0; i < 1000; i++)
        {
            var rng = new Random(i);
            var loot = ItemGenerator.RollLootDrop(3, 50, rng);
            if (loot != null && (loot.Type == ItemType.Weapon || loot.Type == ItemType.Armor))
            {
                foundEquipment = true;
                break;
            }
        }
        Assert.True(foundEquipment, "Should drop equipment sometimes");
    }

    [Fact]
    public void RollLootDrop_CanDropMaterial()
    {
        bool foundMaterial = false;
        for (int i = 0; i < 1000; i++)
        {
            var rng = new Random(i);
            var loot = ItemGenerator.RollLootDrop(1, 5, rng);
            if (loot != null && loot.Type == ItemType.Material)
            {
                foundMaterial = true;
                break;
            }
        }
        Assert.True(foundMaterial, "Should drop materials sometimes");
    }

    [Fact]
    public void RollLootDrop_FloorBonusCapsAt5Percent()
    {
        // At floor 100, floor bonus = 100*0.1% = 10%, but capped at 5%
        // So tier1 rate = 8% + 5% = 13%
        int equipDrops = 0;
        int trials = 20000;
        for (int i = 0; i < trials; i++)
        {
            var rng = new Random(i);
            var loot = ItemGenerator.RollLootDrop(1, 100, rng);
            if (loot != null && (loot.Type == ItemType.Weapon || loot.Type == ItemType.Armor))
                equipDrops++;
        }
        double rate = (double)equipDrops / trials;
        // Should be around 13% (8% base + 5% cap), not 18% (uncapped)
        Assert.True(rate < 0.18, $"Equipment drop rate at floor 100 ({rate:P1}) suggests floor bonus not capped");
        Assert.True(rate > 0.08, $"Equipment drop rate at floor 100 ({rate:P1}) too low");
    }

    // ═══════════════════════════════════════════════════════════════
    //  GenerateCrateLoot
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void GenerateCrateLoot_Returns1To3Items()
    {
        bool found1 = false, found2 = false, found3 = false;
        for (int i = 0; i < 200; i++)
        {
            var rng = new Random(i);
            var loot = ItemGenerator.GenerateCrateLoot(10, rng);
            Assert.True(loot.Count >= 1, $"Crate should have at least 1 item, got {loot.Count}");
            Assert.True(loot.Count <= 3, $"Crate should have at most 3 items, got {loot.Count}");
            if (loot.Count == 1) found1 = true;
            if (loot.Count == 2) found2 = true;
            if (loot.Count == 3) found3 = true;
        }
        Assert.True(found1, "Should sometimes drop 1 item");
        Assert.True(found2, "Should sometimes drop 2 items");
        Assert.True(found3, "Should sometimes drop 3 items");
    }

    [Fact]
    public void GenerateCrateLoot_MostlyMaterials()
    {
        int materialCount = 0;
        int totalItems = 0;
        for (int i = 0; i < 500; i++)
        {
            var rng = new Random(i);
            var loot = ItemGenerator.GenerateCrateLoot(10, rng);
            foreach (var item in loot)
            {
                totalItems++;
                if (item.Type == ItemType.Material) materialCount++;
            }
        }
        double matRate = (double)materialCount / totalItems;
        // 85% materials expected
        Assert.True(matRate > 0.70, $"Expected mostly materials in crates, got {matRate:P1}");
    }

    [Fact]
    public void GenerateCrateLoot_FloorAppropriateItemLevels()
    {
        var rng = new Random(42);
        var loot = ItemGenerator.GenerateCrateLoot(30, rng);
        foreach (var item in loot)
        {
            Assert.Equal(30, item.ItemLevel);
        }
    }

    [Fact]
    public void GenerateCrateLoot_CanContainEquipment()
    {
        bool foundEquip = false;
        for (int i = 0; i < 500; i++)
        {
            var rng = new Random(i);
            var loot = ItemGenerator.GenerateCrateLoot(10, rng);
            foreach (var item in loot)
            {
                if (item.Type == ItemType.Weapon || item.Type == ItemType.Armor)
                {
                    foundEquip = true;
                    break;
                }
            }
            if (foundEquip) break;
        }
        Assert.True(foundEquip, "Crates should sometimes contain equipment");
    }

    // ═══════════════════════════════════════════════════════════════
    //  ItemData extensions — defaults don't break existing code
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void ItemData_NewFields_HaveDefaults()
    {
        var item = new ItemData { Name = "Test", Type = ItemType.Weapon, Slot = EquipSlot.MainHand };
        Assert.Equal(0, item.ItemLevel);
        Assert.Equal(ItemQuality.Normal, item.Quality);
        Assert.NotNull(item.Prefixes);
        Assert.NotNull(item.Suffixes);
        Assert.Empty(item.Prefixes);
        Assert.Empty(item.Suffixes);
    }

    [Fact]
    public void CreateItem_StillWorks_WithNewFields()
    {
        // Ensure the existing GameSystems.CreateItem factory still works
        var item = GameSystems.CreateItem("Sword", ItemType.Weapon, EquipSlot.MainHand, damage: 10);
        Assert.Equal("Sword", item.Name);
        Assert.Equal(10, item.Damage);
        // New fields should be at defaults
        Assert.Equal(0, item.ItemLevel);
        Assert.Equal(ItemQuality.Normal, item.Quality);
        Assert.Empty(item.Prefixes);
        Assert.Empty(item.Suffixes);
    }

    // ═══════════════════════════════════════════════════════════════
    //  AffixData structure
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void AffixData_CanBeCreated()
    {
        var affix = new AffixData
        {
            Name = "Keen",
            Tier = 2,
            BonusDamage = 5,
            BonusDefense = 0,
            BonusHP = 0,
            BonusMP = 0,
            BonusSTR = 0,
            BonusDEX = 0,
            BonusINT = 0,
            BonusVIT = 0,
        };
        Assert.Equal("Keen", affix.Name);
        Assert.Equal(2, affix.Tier);
        Assert.Equal(5, affix.BonusDamage);
    }

    [Fact]
    public void ItemData_CanHoldAffixes()
    {
        var item = new ItemData
        {
            Name = "Test Sword",
            Type = ItemType.Weapon,
            Slot = EquipSlot.MainHand,
        };
        item.Prefixes.Add(new AffixData { Name = "Keen", Tier = 1, BonusDamage = 3 });
        item.Suffixes.Add(new AffixData { Name = "of Strength", Tier = 1, BonusSTR = 2 });

        Assert.Single(item.Prefixes);
        Assert.Single(item.Suffixes);
        Assert.Equal("Keen", item.Prefixes[0].Name);
        Assert.Equal("of Strength", item.Suffixes[0].Name);
    }

    [Fact]
    public void ItemData_MaxAffixes_CanHold3Plus3()
    {
        var item = new ItemData
        {
            Name = "Test",
            Type = ItemType.Weapon,
            Slot = EquipSlot.MainHand,
        };
        for (int i = 0; i < 3; i++)
        {
            item.Prefixes.Add(new AffixData { Name = $"Prefix{i}", Tier = 1 });
            item.Suffixes.Add(new AffixData { Name = $"Suffix{i}", Tier = 1 });
        }
        Assert.Equal(3, item.Prefixes.Count);
        Assert.Equal(3, item.Suffixes.Count);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Quality bonus correctness
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void GenerateEquipment_SuperiorItem_HasHigherStatsThanNormal()
    {
        // On the same floor, Superior items should average higher stats
        int normalTotal = 0, superiorTotal = 0;
        int normalCount = 0, superiorCount = 0;

        for (int i = 0; i < 5000; i++)
        {
            var rng = new Random(i);
            var item = ItemGenerator.GenerateEquipment(30, rng);
            int stat = item.Damage + item.Defense;
            if (item.Quality == ItemQuality.Normal)
            {
                normalTotal += stat;
                normalCount++;
            }
            else if (item.Quality == ItemQuality.Superior)
            {
                superiorTotal += stat;
                superiorCount++;
            }
        }

        Assert.True(normalCount > 0, "Should have Normal items");
        Assert.True(superiorCount > 0, "Should have Superior items");

        double normalAvg = (double)normalTotal / normalCount;
        double superiorAvg = (double)superiorTotal / superiorCount;
        Assert.True(superiorAvg > normalAvg,
            $"Superior avg ({superiorAvg:F1}) should exceed Normal avg ({normalAvg:F1})");
    }

    // ═══════════════════════════════════════════════════════════════
    //  Edge cases
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void GenerateEquipment_NegativeFloor_ClampsTo1()
    {
        var rng = new Random(42);
        var item = ItemGenerator.GenerateEquipment(-5, rng);
        Assert.True(item.ItemLevel >= 1);
    }

    [Fact]
    public void GenerateEquipment_VeryHighFloor_ProducesHighLevelItem()
    {
        var rng = new Random(42);
        var item = ItemGenerator.GenerateEquipment(200, rng);
        Assert.Equal(200, item.ItemLevel);
        Assert.True(item.Damage + item.Defense > 0);
    }

    [Fact]
    public void RollLootDrop_InvalidTier_UsesDefaultRate()
    {
        // Tier 0 and tier 99 should default to Tier 1 base rate (8%)
        int drops = 0;
        for (int i = 0; i < 5000; i++)
        {
            var rng = new Random(i);
            if (ItemGenerator.RollLootDrop(0, 1, rng) != null) drops++;
        }
        double rate = (double)drops / 5000;
        // Should be similar to tier 1 (~31% total with material fallback)
        Assert.True(rate > 0.15, $"Invalid tier drop rate ({rate:P1}) should use default");
        Assert.True(rate < 0.50, $"Invalid tier drop rate ({rate:P1}) too high");
    }

    [Fact]
    public void GenerateMaterial_Floor0_ClampsTo1()
    {
        var rng = new Random(42);
        var mat = ItemGenerator.GenerateMaterial(0, rng);
        Assert.True(mat.ItemLevel >= 1);
    }

    [Fact]
    public void GenerateEquipment_HasDescription()
    {
        var rng = new Random(42);
        var item = ItemGenerator.GenerateEquipment(10, rng);
        Assert.False(string.IsNullOrEmpty(item.Description));
    }

    [Fact]
    public void GenerateEquipment_HasName()
    {
        var rng = new Random(42);
        var item = ItemGenerator.GenerateEquipment(10, rng);
        Assert.False(string.IsNullOrEmpty(item.Name));
    }

    [Fact]
    public void GenerateEquipment_ValueIsPositive()
    {
        for (int floor = 1; floor <= 100; floor += 20)
        {
            var rng = new Random(floor);
            var item = ItemGenerator.GenerateEquipment(floor, rng);
            Assert.True(item.Value > 0, $"Item value on floor {floor} should be positive");
        }
    }
}
