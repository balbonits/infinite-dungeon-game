using System;
using System.Collections.Generic;

namespace DungeonGame;

/// <summary>
/// Container loot rolls (LOOT-01) — implements docs/systems/loot-containers.md.
///
/// Pure-logic, no Godot dependency. Containers are the sole source of equipment
/// drops (monsters keep gold + XP + materials only). Three sizes with distinct
/// loot profiles scale on floor depth.
///
/// Spawn counts and contents both scale on size + floor. Floor-scaling gold and
/// material tier follow the same curves MonsterDropTable uses (re-used via
/// <see cref="MonsterDropTable.FloorToTier"/>).
/// </summary>
public static class ContainerLootTable
{
    public enum ContainerType { Jar, Crate, Chest }

    /// <summary>Loot produced by a single container roll.</summary>
    public record LootRoll(
        int Gold,
        List<ItemDef> Materials,
        List<ItemDef> Equipment);

    // ─── Spawn counts per floor ──────────────────────────────────────────

    /// <summary>
    /// Per-floor container spawn counts. Upper bound grows by +1 per 10 floors.
    /// Min-1 guarantee: if total rolls to 0, force one jar so every floor has
    /// at least one container. Spec: loot-containers.md §Spawn counts per floor.
    /// </summary>
    public static (int jars, int crates, int chests) SpawnCounts(int floor, Random? rng = null)
    {
        rng ??= Random.Shared;
        int depthBonus = Math.Max(0, (floor - 1) / 10);

        int jars = rng.Next(4, 8 + depthBonus + 1);
        int crates = rng.Next(2, 5 + depthBonus + 1);
        int chests = rng.Next(0, 2 + depthBonus + 1);

        // Guarantee from the spec: every floor has ≥1 container. In practice
        // this only triggers when jars + crates + chests all rolled 0, which
        // is mathematically impossible since jars start at 4 — but guard
        // anyway so a future rebalance that lowers the jar floor can't break
        // the spec contract.
        if (jars + crates + chests == 0)
            jars = 1;

        return (jars, crates, chests);
    }

    // ─── Top-level roll ──────────────────────────────────────────────────

    /// <summary>
    /// Roll the full loot payload for a container of the given type on a
    /// specific floor (with its zone's species for signature-material bias).
    /// Slot-uniqueness guarantee is enforced inside <see cref="RollEquipment"/>.
    /// </summary>
    public static LootRoll Roll(ContainerType type, int floor, int zone, Random? rng = null)
    {
        rng ??= Random.Shared;
        int gold = RollGold(type, floor, rng);
        var materials = new List<ItemDef>();
        materials.AddRange(RollGenericMaterials(type, floor, rng));
        materials.AddRange(RollSignatureMaterial(type, zone, rng));
        var equipment = RollEquipment(type, floor, rng);
        return new LootRoll(gold, materials, equipment);
    }

    // ─── Gold ────────────────────────────────────────────────────────────

    /// <summary>
    /// Gold channel. Chance + per-type base range + floor-linear multiplier
    /// (×(1 + floor * 0.05) — floor 20 = 2×, floor 100 = 6×).
    /// </summary>
    public static int RollGold(ContainerType type, int floor, Random rng)
    {
        float chance = type switch
        {
            ContainerType.Jar => 0.60f,
            ContainerType.Crate => 0.70f,
            ContainerType.Chest => 1.00f,
            _ => 0f,
        };
        if (rng.NextSingle() >= chance) return 0;

        (int min, int max) = type switch
        {
            ContainerType.Jar => (5, 25),
            ContainerType.Crate => (50, 100),
            ContainerType.Chest => (100, 500),
            _ => (0, 0),
        };
        int baseGold = rng.Next(min, max + 1);
        float floorMult = 1f + floor * 0.05f;
        return (int)Math.Round(baseGold * floorMult);
    }

    // ─── Generic materials ───────────────────────────────────────────────

    /// <summary>
    /// Generic material channel. Per spec: Jar 30%×1, Crate 100%×(2-4), Chest 50%×1.
    /// Material type rolled uniformly across {Ore, Bone, Hide} — no thematic bias
    /// here (thematic bias is for signature mats in zone-tilted mode).
    /// Tier follows <see cref="MonsterDropTable.FloorToTier"/>.
    /// </summary>
    public static List<ItemDef> RollGenericMaterials(ContainerType type, int floor, Random rng)
    {
        var result = new List<ItemDef>();
        int tier = MonsterDropTable.FloorToTier(floor);

        switch (type)
        {
            case ContainerType.Jar:
                if (rng.NextSingle() < 0.30f) AddGeneric(result, tier, rng);
                break;
            case ContainerType.Crate:
                int crateCount = rng.Next(2, 4 + 1);
                for (int i = 0; i < crateCount; i++) AddGeneric(result, tier, rng);
                break;
            case ContainerType.Chest:
                if (rng.NextSingle() < 0.50f) AddGeneric(result, tier, rng);
                break;
        }
        return result;
    }

    private static void AddGeneric(List<ItemDef> bucket, int tier, Random rng)
    {
        // Uniform across {Ore, Bone, Hide} — spec leaves thematic bias off
        // generics here and puts it on signature mats.
        var type = (MonsterDropTable.MaterialType)rng.Next(3);
        string id = $"material_{type.ToString().ToLowerInvariant()}_t{tier}";
        var def = ItemDatabase.Get(id);
        if (def != null) bucket.Add(def);
    }

    // ─── Signature material ──────────────────────────────────────────────

    /// <summary>
    /// Signature material channel — only Crate (30%, 0-1×) and Chest (50%, 1×)
    /// roll on this channel. Jars never roll signatures. Zone-tilted: the
    /// signature is weighted toward the floor's zone species per spec
    /// §Locked Decisions #3.
    /// </summary>
    public static List<ItemDef> RollSignatureMaterial(ContainerType type, int zone, Random rng)
    {
        var result = new List<ItemDef>();
        float chance = type switch
        {
            ContainerType.Crate => 0.30f,
            ContainerType.Chest => 0.50f,
            _ => 0f,
        };
        if (rng.NextSingle() >= chance) return result;

        var candidates = ZoneSignatureCandidates(zone);
        if (candidates.Count == 0) return result;

        string sigId = candidates[rng.Next(candidates.Count)];
        var def = ItemDatabase.Get(sigId);
        if (def != null) result.Add(def);
        return result;
    }

    /// <summary>
    /// Map a zone to its expected signature-material IDs. Mirrors the per-species
    /// signature mappings in MonsterDropTable.Tables, collapsed to zones per the
    /// existing zone→species assignments in Constants.Zones.
    /// </summary>
    private static List<string> ZoneSignatureCandidates(int zone) => zone switch
    {
        1 => new List<string> { "material_sig_skeleton", "material_sig_bat" },
        2 => new List<string> { "material_sig_goblin", "material_sig_wolf" },
        3 => new List<string> { "material_sig_orc", "material_sig_spider" },
        4 => new List<string> { "material_sig_darkmage" },
        // Zones 5+ — all species can appear; surface every signature.
        _ => new List<string>
        {
            "material_sig_skeleton", "material_sig_bat",
            "material_sig_goblin", "material_sig_wolf",
            "material_sig_orc", "material_sig_spider",
            "material_sig_darkmage",
        },
    };

    // ─── Equipment ───────────────────────────────────────────────────────

    /// <summary>
    /// Equipment channel. Per spec: Jar 25% × 1 from {Ring,Neck,Consumable}
    /// only, Crate 60% × 0-2 full pool, Chest 100% × 1-3 full pool. Slot-
    /// uniqueness enforced: a container never drops two items of the same
    /// slot type (two rings, two chests, etc).
    ///
    /// Returns empty list on no-drop or empty pool.
    /// </summary>
    public static List<ItemDef> RollEquipment(ContainerType type, int floor, Random rng)
    {
        var result = new List<ItemDef>();
        int tier = MonsterDropTable.FloorToTier(floor);

        (float chance, int minCount, int maxCount, bool smallThingPool) cfg = type switch
        {
            ContainerType.Jar => (0.25f, 1, 1, true),
            ContainerType.Crate => (0.60f, 0, 2, false),
            ContainerType.Chest => (1.00f, 1, 3, false),
            _ => (0f, 0, 0, false),
        };
        if (rng.NextSingle() >= cfg.chance) return result;

        var pool = cfg.smallThingPool
            ? SmallThingCandidates(tier)
            : FullEquipmentCandidates(tier);
        if (pool.Count == 0) return result;

        // Shuffle a local copy to avoid re-rolling collisions; walk until we've
        // drawn the target count of distinct-slot items or exhausted the pool.
        int count = rng.Next(cfg.minCount, cfg.maxCount + 1);
        var shuffled = new List<ItemDef>(pool);
        FisherYatesShuffle(shuffled, rng);

        var seenSlots = new HashSet<EquipSlot>();
        foreach (var item in shuffled)
        {
            if (result.Count >= count) break;
            // Consumables have EquipSlot.None; they're single-stack-per-container
            // so we use Category as the de-dupe key for them.
            var slotKey = item.Slot != EquipSlot.None
                ? item.Slot
                : EquipSlot.None;
            if (slotKey != EquipSlot.None && seenSlots.Contains(slotKey)) continue;
            if (slotKey == EquipSlot.None && result.Exists(x => x.Category == item.Category)) continue;

            result.Add(item);
            if (slotKey != EquipSlot.None) seenSlots.Add(slotKey);
        }
        return result;
    }

    private static List<ItemDef> FullEquipmentCandidates(int tier)
    {
        var list = new List<ItemDef>();
        foreach (var item in ItemDatabase.All)
        {
            if (item.Tier != tier) continue;
            if (item.Slot == EquipSlot.None) continue;
            list.Add(item);
        }
        return list;
    }

    private static List<ItemDef> SmallThingCandidates(int tier)
    {
        // Jar "small thing" pool: Ring, Neck, or Consumable.
        // Consumables are Tier=0 (untiered); rings + necks follow tier.
        var list = new List<ItemDef>();
        foreach (var item in ItemDatabase.All)
        {
            bool isSmallRingOrNeck = (item.Slot == EquipSlot.Ring || item.Slot == EquipSlot.Neck)
                && item.Tier == tier;
            bool isConsumable = item.Category == ItemCategory.Consumable;
            if (isSmallRingOrNeck || isConsumable)
                list.Add(item);
        }
        return list;
    }

    private static void FisherYatesShuffle<T>(List<T> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
