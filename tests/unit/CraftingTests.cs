using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

public class CraftingTests
{
    private static CraftableItem MakeItem(int level = 10, BaseQuality quality = BaseQuality.Normal) => new()
    {
        BaseItemId = "sword",
        BaseName = "Iron Sword",
        ItemLevel = level,
        Quality = quality,
        Category = ItemCategory.Weapon,
    };

    private static AffixDef MakePrefix(string id = "keen_1", int minLevel = 1, int gold = 50) =>
        AffixDatabase.Get(id) ?? new AffixDef
        {
            Id = id,
            Name = "Keen",
            Type = AffixType.Prefix,
            MinItemLevel = minLevel,
            GoldCost = gold,
            Value = 3,
        };

    private static AffixDef MakeSuffix(string id = "striking_1", int minLevel = 1, int gold = 60) =>
        AffixDatabase.Get(id) ?? new AffixDef
        {
            Id = id,
            Name = "of Striking",
            Type = AffixType.Suffix,
            MinItemLevel = minLevel,
            GoldCost = gold,
            Value = 2,
        };

    // -- CanApplyAffix --

    [Fact]
    public void CanApplyAffix_ValidConditions_ReturnsTrue()
    {
        var item = MakeItem(level: 10);
        var affix = MakePrefix("keen_1");
        var inv = new Inventory { Gold = 1000 };
        Crafting.CanApplyAffix(item, affix, inv).Should().BeTrue();
    }

    [Fact]
    public void CanApplyAffix_ItemLevelTooLow_ReturnsFalse()
    {
        var item = MakeItem(level: 1);
        var affix = MakePrefix("keen_2"); // requires level 10
        var inv = new Inventory { Gold = 10000 };
        Crafting.CanApplyAffix(item, affix, inv).Should().BeFalse();
    }

    [Fact]
    public void CanApplyAffix_PrefixLimitReached_ReturnsFalse()
    {
        var item = MakeItem(level: 50);
        var inv = new Inventory { Gold = 100000 };

        // Fill 3 prefixes
        item.Affixes.Add(new AppliedAffix { AffixId = "keen_1" });
        item.Affixes.Add(new AppliedAffix { AffixId = "sturdy_1" });
        item.Affixes.Add(new AppliedAffix { AffixId = "energizing_1" });

        var fourthPrefix = MakePrefix("fiery_1");
        Crafting.CanApplyAffix(item, fourthPrefix, inv).Should().BeFalse();
    }

    [Fact]
    public void CanApplyAffix_SuffixLimitReached_ReturnsFalse()
    {
        var item = MakeItem(level: 50);
        var inv = new Inventory { Gold = 100000 };

        item.Affixes.Add(new AppliedAffix { AffixId = "striking_1" });
        item.Affixes.Add(new AppliedAffix { AffixId = "bear_1" });
        item.Affixes.Add(new AppliedAffix { AffixId = "swiftness_1" });

        var fourthSuffix = MakeSuffix("learning_1");
        Crafting.CanApplyAffix(item, fourthSuffix, inv).Should().BeFalse();
    }

    [Fact]
    public void CanApplyAffix_DuplicateAffix_ReturnsFalse()
    {
        var item = MakeItem(level: 10);
        var inv = new Inventory { Gold = 10000 };

        var affix = MakePrefix("keen_1");
        item.Affixes.Add(new AppliedAffix { AffixId = "keen_1" });

        Crafting.CanApplyAffix(item, affix, inv).Should().BeFalse();
    }

    [Fact]
    public void CanApplyAffix_NotEnoughGold_ReturnsFalse()
    {
        var item = MakeItem(level: 10);
        var affix = MakePrefix("keen_1"); // costs 50
        var inv = new Inventory { Gold = 10 };
        Crafting.CanApplyAffix(item, affix, inv).Should().BeFalse();
    }

    // -- ApplyAffix --

    [Fact]
    public void ApplyAffix_DeductsGold()
    {
        var item = MakeItem(level: 10);
        var affix = MakePrefix("keen_1");
        var inv = new Inventory { Gold = 1000 };
        int cost = affix.GoldCost;

        Crafting.ApplyAffix(item, affix, inv);
        inv.Gold.Should().Be(1000 - cost);
    }

    [Fact]
    public void ApplyAffix_AddsAffixToItem()
    {
        var item = MakeItem(level: 10);
        var affix = MakePrefix("keen_1");
        var inv = new Inventory { Gold = 1000 };

        Crafting.ApplyAffix(item, affix, inv).Should().BeTrue();
        item.Affixes.Should().ContainSingle(a => a.AffixId == "keen_1");
    }

    [Fact]
    public void ApplyAffix_FailsIfCannotApply()
    {
        var item = MakeItem(level: 1);
        var affix = MakePrefix("keen_2"); // needs level 10
        var inv = new Inventory { Gold = 10000 };
        Crafting.ApplyAffix(item, affix, inv).Should().BeFalse();
    }

    // -- RecycleItem --

    [Fact]
    public void RecycleItem_BaseFormula()
    {
        var item = MakeItem(level: 10);
        // base = 5 + 10*2 = 25, quality Normal = 0 bonus, 0 affixes
        Crafting.RecycleItem(item).Should().Be(25);
    }

    [Fact]
    public void RecycleItem_SuperiorQualityBonus()
    {
        var item = MakeItem(level: 10, quality: BaseQuality.Superior);
        int baseGold = 5 + 10 * 2; // 25
        int qualityBonus = baseGold / 4; // 6
        Crafting.RecycleItem(item).Should().Be(baseGold + qualityBonus);
    }

    [Fact]
    public void RecycleItem_EliteQualityBonus()
    {
        var item = MakeItem(level: 10, quality: BaseQuality.Elite);
        int baseGold = 5 + 10 * 2; // 25
        int qualityBonus = baseGold / 2; // 12
        Crafting.RecycleItem(item).Should().Be(baseGold + qualityBonus);
    }

    [Fact]
    public void RecycleItem_AffixCountBonus()
    {
        var item = MakeItem(level: 10);
        item.Affixes.Add(new AppliedAffix { AffixId = "keen_1" });
        item.Affixes.Add(new AppliedAffix { AffixId = "striking_1" });
        // base=25, quality=0, affixes=2*10=20
        Crafting.RecycleItem(item).Should().Be(45);
    }

    // AUDIT-11: quality ladder covers all 6 tiers, not just Superior + Elite.
    // Ratios locked in SPEC-CRAFTING-QUALITY-LADDER-01 (geometric).

    [Fact]
    public void RecycleItem_MasterworkQualityBonus()
    {
        var item = MakeItem(level: 10, quality: BaseQuality.Masterwork);
        int baseGold = 5 + 10 * 2; // 25
        // Masterwork = ×1.00 → bonus equals baseGold
        Crafting.RecycleItem(item).Should().Be(baseGold + baseGold); // 50
    }

    [Fact]
    public void RecycleItem_MythicQualityBonus()
    {
        var item = MakeItem(level: 10, quality: BaseQuality.Mythic);
        int baseGold = 5 + 10 * 2; // 25
        // Mythic = ×2.00
        Crafting.RecycleItem(item).Should().Be(baseGold + baseGold * 2); // 75
    }

    [Fact]
    public void RecycleItem_TranscendentQualityBonus()
    {
        var item = MakeItem(level: 10, quality: BaseQuality.Transcendent);
        int baseGold = 5 + 10 * 2; // 25
        // Transcendent = ×4.00
        Crafting.RecycleItem(item).Should().Be(baseGold + baseGold * 4); // 125
    }

    [Fact]
    public void RecycleItem_QualityLadder_IsStrictlyMonotonic()
    {
        // Spec guarantees each tier yields more than the previous at the same
        // item level + affix count. Regression guard.
        int LevelOnly(BaseQuality q) => Crafting.RecycleItem(MakeItem(level: 10, quality: q));

        int normal = LevelOnly(BaseQuality.Normal);
        int superior = LevelOnly(BaseQuality.Superior);
        int elite = LevelOnly(BaseQuality.Elite);
        int masterwork = LevelOnly(BaseQuality.Masterwork);
        int mythic = LevelOnly(BaseQuality.Mythic);
        int transcendent = LevelOnly(BaseQuality.Transcendent);

        (normal < superior).Should().BeTrue();
        (superior < elite).Should().BeTrue();
        (elite < masterwork).Should().BeTrue();
        (masterwork < mythic).Should().BeTrue();
        (mythic < transcendent).Should().BeTrue();
    }

    // -- GetDisplayName --

    [Fact]
    public void GetDisplayName_NoAffixes_ReturnsBaseName()
    {
        var item = MakeItem();
        Crafting.GetDisplayName(item).Should().Be("Iron Sword");
    }

    [Fact]
    public void GetDisplayName_PrefixAndSuffix()
    {
        var item = MakeItem();
        item.Affixes.Add(new AppliedAffix { AffixId = "keen_1" });
        item.Affixes.Add(new AppliedAffix { AffixId = "striking_1" });
        string name = Crafting.GetDisplayName(item);
        name.Should().Contain("Keen");
        name.Should().Contain("Iron Sword");
        name.Should().Contain("of Striking");
    }
}
