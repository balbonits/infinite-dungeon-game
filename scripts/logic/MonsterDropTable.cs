using System;
using System.Collections.Generic;

namespace DungeonGame;

/// <summary>
/// Per-species drop tables (ITEM-02) — implements the monster-side of
/// docs/systems/monster-drops.md. Material-only after LOOT-01; the equipment
/// channel moved to <see cref="ContainerLootTable"/> per SPEC-LOOT-01.
///
/// Design highlights:
/// - **Per-species signature material** on top of generic tiered drops.
/// - **Floor-tiered material** type (ore/bone/hide) is species-biased: each
///   species's thematic generic rolls 60%, others 20% each.
/// </summary>
public static class MonsterDropTable
{
    /// <summary>Monster tier — locked in monster-drops.md §Equipment Drop Rates.</summary>
    public enum MonsterTier { One = 1, Two = 2, Three = 3 }

    /// <summary>Material type bias for per-species generic material rolls.</summary>
    public enum MaterialType { Ore, Bone, Hide }

    public record DropTable(
        EnemySpecies Species,
        MonsterTier Tier,
        string SignatureMaterialId,
        float SignatureRate,     // 0..1 — probability of the signature drop on top of generic
        MaterialType ThematicGeneric
    );

    private static readonly Dictionary<EnemySpecies, DropTable> Tables = new()
    {
        // Zone 1 — Tier 1.
        [EnemySpecies.Skeleton] = new(EnemySpecies.Skeleton, MonsterTier.One, "material_sig_skeleton", 0.10f, MaterialType.Bone),
        [EnemySpecies.Bat] = new(EnemySpecies.Bat, MonsterTier.One, "material_sig_bat", 0.08f, MaterialType.Hide),
        // Zone 2 — Tier 1-2.
        [EnemySpecies.Goblin] = new(EnemySpecies.Goblin, MonsterTier.One, "material_sig_goblin", 0.10f, MaterialType.Ore),
        [EnemySpecies.Wolf] = new(EnemySpecies.Wolf, MonsterTier.Two, "material_sig_wolf", 0.10f, MaterialType.Hide),
        // Zone 3 — Tier 2-3.
        [EnemySpecies.Orc] = new(EnemySpecies.Orc, MonsterTier.Three, "material_sig_orc", 0.10f, MaterialType.Ore),
        [EnemySpecies.Spider] = new(EnemySpecies.Spider, MonsterTier.Two, "material_sig_spider", 0.08f, MaterialType.Hide),
        // Zone 4+ — Tier 3.
        [EnemySpecies.DarkMage] = new(EnemySpecies.DarkMage, MonsterTier.Three, "material_sig_darkmage", 0.07f, MaterialType.Bone),
    };

    public static DropTable? Get(EnemySpecies species) =>
        Tables.TryGetValue(species, out var t) ? t : null;

    // ─── Equipment drop ──────────────────────────────────────────────────
    //
    // LOOT-01 / SPEC-LOOT-01: the equipment channel was removed from
    // monsters. Equipment now drops exclusively from world containers
    // (see ContainerLootTable). Monsters keep gold + XP + materials.

    // ─── Material drop ───────────────────────────────────────────────────

    /// <summary>
    /// Roll for material drops on this species kill. Returns an empty list on no-drop.
    /// Per monster-drops.md: 25% flat per kill for a generic tiered material (with
    /// 60% thematic bias / 20% / 20% split), plus an independent signature-material
    /// chance (species-specific — see <see cref="DropTable.SignatureRate"/>).
    /// </summary>
    public static List<ItemDef> RollMaterials(EnemySpecies species, int floorNumber, Random? rng = null)
    {
        rng ??= Random.Shared;
        var result = new List<ItemDef>();
        var table = Get(species);
        if (table == null) return result;

        // Generic material roll (25% flat).
        if (rng.NextSingle() < 0.25f)
        {
            var materialType = RollMaterialType(table.ThematicGeneric, rng);
            int tier = FloorToTier(floorNumber);
            string id = $"material_{materialType.ToString().ToLowerInvariant()}_t{tier}";
            var def = ItemDatabase.Get(id);
            if (def != null) result.Add(def);
        }

        // Signature material roll (independent).
        if (rng.NextSingle() < table.SignatureRate)
        {
            var sig = ItemDatabase.Get(table.SignatureMaterialId);
            if (sig != null) result.Add(sig);
        }

        return result;
    }

    private static MaterialType RollMaterialType(MaterialType thematic, Random rng)
    {
        // 60% thematic, 20% each of the other two.
        float r = rng.NextSingle();
        if (r < 0.60f) return thematic;
        var others = new List<MaterialType>();
        foreach (MaterialType m in Enum.GetValues(typeof(MaterialType)))
            if (m != thematic) others.Add(m);
        // Split the remaining 40% across "others" — 50/50 between them.
        return (r < 0.80f) ? others[0] : others[1];
    }

    // ─── Catalog helpers ─────────────────────────────────────────────────

    /// <summary>Map a floor number to a catalog tier (1..5). Matches floor-bracket table.</summary>
    public static int FloorToTier(int floor) => floor switch
    {
        <= 10 => 1,
        <= 25 => 2,
        <= 50 => 3,
        <= 100 => 4,
        _ => 5,
    };

}
