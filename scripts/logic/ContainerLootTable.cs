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
        // +1 to upper bound per 10 floors. Spec milestone targets anchored
        // in loot-containers.md §Spawn counts per floor:
        //   floor 1   → bonus 0 → jars 4-8,   crates 2-5,   chests 0-2
        //   floor 10  → bonus 1 → jars 4-9,   crates 2-6,   chests 0-3
        //   floor 50  → bonus 5 → jars 4-13,  crates 2-10,  chests 0-7
        //   floor 100 → bonus 10 → jars 4-18, crates 2-15,  chests 0-12
        int depthBonus = Math.Max(0, floor / 10);

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
    /// signature is WEIGHTED toward the floor's zone species (per spec
    /// §Locked Decisions #3 + its example: "chest on floor 5 more likely to
    /// drop Bone Dust + Echo Shard than Orc Tusk" — the Orc case implies
    /// off-zone signatures still appear, just less often).
    ///
    /// Implementation: on-zone species get weight 5; off-zone species get
    /// weight 1. With the zone-1 case (2 species) and 5 off-zone species,
    /// that means ~66% on-zone / ~33% off-zone (2*5 = 10, 5*1 = 5, split
    /// 10/15). Adjust the on-zone weight constant if playtest shifts.
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

        string? sigId = PickWeightedSignature(zone, rng);
        if (sigId == null) return result;
        var def = ItemDatabase.Get(sigId);
        if (def != null) result.Add(def);
        return result;
    }

    /// <summary>
    /// Pick a signature-material ID using the zone-tilt weighting scheme.
    /// Full signature roster comes from
    /// <see cref="MonsterDropTable.AllSignatureMaterialIds"/> (single source
    /// of truth, can't desync). On-zone species are derived from
    /// <see cref="Constants.Zones.GetZoneSpecies"/> so the weighting tracks
    /// the game's actual species roster per zone.
    ///
    /// Weighting: 5× for on-zone species signatures, 1× for off-zone.
    /// Zone-5+ returns a uniform-weighted pick because every species is
    /// reachable at that depth — "on-zone" is the empty set, so all
    /// signatures get base weight 1.
    /// </summary>
    private static string? PickWeightedSignature(int zone, Random rng)
    {
        var all = MonsterDropTable.AllSignatureMaterialIds;
        if (all.Count == 0) return null;

        var onZone = OnZoneSignatures(zone);
        int totalWeight = 0;
        foreach (var sig in all)
            totalWeight += onZone.Contains(sig) ? 5 : 1;
        if (totalWeight <= 0) return null;

        int pick = rng.Next(totalWeight);
        int running = 0;
        foreach (var sig in all)
        {
            running += onZone.Contains(sig) ? 5 : 1;
            if (pick < running) return sig;
        }
        return all[^1]; // unreachable if totalWeight > 0; compiler appeasement
    }

    /// <summary>
    /// On-zone signature IDs derived from Constants.Zones.GetZoneSpecies —
    /// avoids hard-coding zone→signature mappings that could desync from
    /// the game's species roster (e.g., zone 4 ships DarkMage + Skeleton +
    /// Orc, not just DarkMage). Uses a representative floor inside each
    /// zone (floor = (zone-1)*10 + 1, i.e. the first floor of that zone).
    /// </summary>
    private static HashSet<string> OnZoneSignatures(int zone)
    {
        int representativeFloor = Math.Max(1, (zone - 1) * 10 + 1);
        int[] speciesIndices = Constants.Zones.GetZoneSpecies(representativeFloor);

        var result = new HashSet<string>();
        foreach (int idx in speciesIndices)
        {
            var table = MonsterDropTable.Get((EnemySpecies)idx);
            if (table != null) result.Add(table.SignatureMaterialId);
        }
        return result;
    }

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
        // Floor-tiered gear must match the floor's tier bracket. Untiered
        // equipment (Tier=0 — currently only quivers per ItemDef.Tier
        // XML doc) is eligible at every floor: ITEM-01 doesn't tier
        // quivers, so filtering them out here would deny Ranger ammo
        // from containers entirely.
        var list = new List<ItemDef>();
        foreach (var item in ItemDatabase.All)
        {
            if (item.Slot == EquipSlot.None) continue;
            bool isMatchedTier = item.Tier == tier;
            bool isUntieredEquipment = item.Tier == 0;
            if (!isMatchedTier && !isUntieredEquipment) continue;
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
