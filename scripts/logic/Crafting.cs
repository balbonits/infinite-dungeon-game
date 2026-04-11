using System.Collections.Generic;
using System.Linq;

namespace DungeonGame;

/// <summary>
/// Blacksmith crafting system. Deterministic affix application to equipment.
/// Max 3 prefix + 3 suffix per item. No RNG — player picks the exact affix.
/// Also handles gear recycling into materials.
/// Pure logic — no Godot dependency.
/// </summary>
public static class Crafting
{
    public const int MaxPrefixes = 3;
    public const int MaxSuffixes = 3;

    /// <summary>
    /// Check if an affix can be applied to an item.
    /// </summary>
    public static bool CanApplyAffix(CraftableItem item, AffixDef affix, Inventory playerInventory)
    {
        // Check item level requirement
        if (item.ItemLevel < affix.MinItemLevel)
            return false;

        // Check affix slot limits
        int prefixCount = item.Affixes.Count(a => AffixDatabase.Get(a.AffixId)?.Type == AffixType.Prefix);
        int suffixCount = item.Affixes.Count(a => AffixDatabase.Get(a.AffixId)?.Type == AffixType.Suffix);

        if (affix.Type == AffixType.Prefix && prefixCount >= MaxPrefixes)
            return false;
        if (affix.Type == AffixType.Suffix && suffixCount >= MaxSuffixes)
            return false;

        // Check duplicate affixes (can't stack same affix id)
        if (item.Affixes.Any(a => a.AffixId == affix.Id))
            return false;

        // Check gold
        if (playerInventory.Gold < affix.GoldCost)
            return false;

        return true;
    }

    /// <summary>
    /// Apply an affix to an item. Deducts gold from inventory.
    /// Returns true if successful.
    /// </summary>
    public static bool ApplyAffix(CraftableItem item, AffixDef affix, Inventory playerInventory)
    {
        if (!CanApplyAffix(item, affix, playerInventory))
            return false;

        playerInventory.Gold -= affix.GoldCost;
        item.Affixes.Add(new AppliedAffix { AffixId = affix.Id, Value = affix.Value });
        return true;
    }

    /// <summary>
    /// Recycle an item into materials. Returns gold value.
    /// </summary>
    public static int RecycleItem(CraftableItem item)
    {
        // Gold return scales with item level and quality
        int baseGold = 5 + item.ItemLevel * 2;
        int qualityBonus = item.Quality switch
        {
            BaseQuality.Superior => baseGold / 4,
            BaseQuality.Elite => baseGold / 2,
            _ => 0,
        };
        return baseGold + qualityBonus + item.Affixes.Count * 10;
    }

    /// <summary>
    /// Build the display name for a crafted item.
    /// Prefix + BaseName + Suffix (e.g., "Keen Iron Sword of Striking")
    /// </summary>
    public static string GetDisplayName(CraftableItem item)
    {
        var prefixes = new List<string>();
        var suffixes = new List<string>();

        foreach (var applied in item.Affixes)
        {
            var def = AffixDatabase.Get(applied.AffixId);
            if (def == null) continue;
            if (def.Type == AffixType.Prefix)
                prefixes.Add(def.Name);
            else
                suffixes.Add(def.Name);
        }

        string result = item.BaseName;
        if (prefixes.Count > 0)
            result = string.Join(" ", prefixes) + " " + result;
        if (suffixes.Count > 0)
            result += " " + string.Join(" ", suffixes);

        return result;
    }
}

/// <summary>
/// A craftable equipment item with applied affixes.
/// </summary>
public class CraftableItem
{
    public string BaseItemId { get; init; } = "";
    public string BaseName { get; init; } = "";
    public int ItemLevel { get; init; } = 1;
    public BaseQuality Quality { get; init; } = BaseQuality.Normal;
    public ItemCategory Category { get; init; }
    public PlayerClass ClassRestriction { get; init; }
    public int BaseDamage { get; init; }
    public int BaseDefense { get; init; }
    public List<AppliedAffix> Affixes { get; init; } = new();
}

public enum BaseQuality
{
    Normal,
    Superior,
    Elite,
}
