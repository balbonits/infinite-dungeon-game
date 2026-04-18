# Monster Drop Tables

> **2026-04-17 supersession notice — equipment channel removed.** Per [SPEC-LOOT-01 (Loot Containers)](loot-containers.md), monsters no longer drop equipment under any circumstance. Equipment is exclusively a container drop (Jars / Crates / Chests). The sections below have been pruned to material + gold + XP channels only. Previously the equipment channel and Preferred-Slot weighting were defined here; both are now obsolete and not reinstated.

## Summary

Every enemy species has a bespoke drop table that mixes tiered generic materials with one species-signature material. Drop-rate curves for the material channels stay as defined here. Equipment drops are owned by [loot-containers.md](loot-containers.md).

## Current State

**Spec status: DRAFT.**

Related code:
- `scripts/logic/LootTable.cs` — current naive implementation (flat level-scaled roll across `ItemDatabase.All`). ITEM-02 will re-align this with the per-species tables defined below.
- `scripts/logic/EnemySpecies.cs` — the 7 species enum values referenced throughout.
- `scripts/Constants.cs` → `Zones.GetZoneSpecies(floor)` — zone-exclusive species assignment per [SYS-10](../../docs/dev-tracker.md).
- `scripts/logic/ItemGenerator.cs` — floor→item-level mapping and quality roll; remains the authority on stat scaling.

Depends on: [items.md](../inventory/items.md) (data model), [equipment.md](equipment.md) (slot layout + class affinity), [item-generation.md](item-generation.md) (stat scaling + quality roll), [depth-gear-tiers.md](depth-gear-tiers.md) (Masterwork/Mythic/Transcendent), [item-catalog.md](../inventory/item-catalog.md) (the concrete item IDs referenced by tables), [spawning.md](spawning.md), [floor-scaling.md](floor-scaling.md).

## Design

### Goals

- **Zone identity.** Each species feels different to farm. Killing skeletons on floor 8 should not produce the same loot silhouette as killing goblins on floor 18.
- **Predictable curves.** Drop rates and quality stay on the SPEC-06b curves — players can metagame depth.
- **No affixes from mobs.** Monsters drop **base items only**. All magic is poured in at the Blacksmith. This is a locked rule from [items.md](../inventory/items.md) and is re-stated here because it governs table structure.

### Per-Species Drop Table Structure

Every species owns one `DropTable`. A table has two independent channels that roll per kill (equipment removed per SPEC-LOOT-01):

| Channel | What it rolls | Rate | Notes |
|---|---|---|---|
| **Generic material** | One tiered generic material (ore / bone / hide) sized to the current floor bracket | 25% flat | Tier 1–5 gated by floor (see Material Tiers below) |
| **Signature material** | The species' one unique material | 5–10% bonus roll (species-dependent) | Rolled **in addition** to the generic material — they don't displace each other |

Channels are independent: a single kill can drop generic mat + signature mat in the same frame. Gold + XP are emitted by the existing kill-reward path; they are not part of this drop table.

### Species → Material & Archetype Map

Locked per [item-catalog.md](../inventory/item-catalog.md) — this table is the binding between species and their loot silhouette. Each species has a **signature material** and a **preferred slot pool** that tilts which base items drop most often from that species.

| Species | Zone (primary) | Signature Material | Signature Rate |
|---|---|---|---|
| Skeleton | 1 | Bone Dust | 10% |
| Goblin | 2 | Goblin Tooth | 10% |
| Bat | 1 | Echo Shard | 8% |
| Wolf | 2 | Wolf Pelt | 10% |
| Orc | 3 | Orc Tusk | 10% |
| Dark Mage | 4 | Arcane Residue | 7% |
| Spider | 3 | Chitin Fragment | 8% |

(Slot affinity removed — equipment is no longer a monster drop. The Slot Rolling Rule, Equipment Drop Rates, Base Quality Distribution, and Class-affinity sections from earlier drafts are obsolete and have been removed; container loot rolls quality + slot per [item-generation.md](item-generation.md) instead.)

### Material Tiers (generic)

Five tiers, binding directly to floor brackets. Names are locked in [item-catalog.md](../inventory/item-catalog.md) → Materials section; summarized here:

| Tier | Floor Range | Ore | Bone | Hide |
|---|---|---|---|---|
| 1 | 1–10 | Iron Ore | Rough Bone | Rough Hide |
| 2 | 11–25 | Steel Ingot | Standard Bone | Standard Hide |
| 3 | 26–50 | Mithril Ore | Fine Bone | Fine Hide |
| 4 | 51–100 | Orichalcum Ore | Masterwork Bone | Masterwork Hide |
| 5 | 101+ | Dragonite Ore | Top-Shelf Bone | Top-Shelf Hide |

The species-signature material type (ore vs bone vs hide) biases the generic roll: each species rolls its **thematic generic** 60% of the time and one of the other two 20% each.

| Species | Thematic generic (60%) |
|---|---|
| Skeleton | Bone |
| Goblin | Ore (crude weapons = metal scraps) |
| Bat | Hide (leathery wings) |
| Wolf | Hide |
| Orc | Ore |
| Dark Mage | Bone (desiccated remnants) |
| Spider | Hide (silk-chitin) |

### Material Drop Rate

```
generic_material_chance = 25% flat (per kill, independent of equipment roll)
signature_material_chance = species-specific (7–10%, per kill, independent bonus roll)
```

Both channels roll independently — a lucky kill can yield both generic and signature material at once. A single kill produces at most 2 drops (generic + signature material). Equipment is a container drop; see [loot-containers.md](loot-containers.md).

### Boss First-Kill Drops

Boss encounters (first kill per save slot, per boss) follow a **guaranteed-bundle** rule for materials, plus a **guaranteed bonus container spawn** at the boss tile for equipment (since bosses no longer drop equipment directly).

| Boss depth | Guaranteed rare material bundle | Bonus container spawned at boss tile |
|---|---|---|
| Floor 10 miniboss | 3× Tier 2 generics, 1× signature (zone's dominant species) | 1× Crate |
| Floor 25 miniboss | 4× Tier 3 generics, 2× signature | 1× Crate |
| Floor 50 elite | 5× Tier 4 generics, 3× signature, 1× Bone Dust (Skeleton King callback) | 1× Chest |
| Floor 101 elite | 5× Tier 5 generics, 3× signature, 1× of every Zone 1–3 signature | 2× Chest |
| Floor 150+ boss | 5× Arcane materials per type, 5× of any 3 signatures | 3× Chest |

The bonus container is a normal container per [loot-containers.md](loot-containers.md) — its equipment count + quality come from the normal floor-scaled rolls. The boss bundle is what carries the "first-kill is special" feel; the bonus container guarantees an equipment drop without violating the "monsters don't drop equipment" rule.

**One-time only per save slot.** Subsequent kills of the same boss instance produce only the normal material channels; no bonus container.

### Treasure Room Chests

Superseded by [loot-containers.md](loot-containers.md) — treasure rooms now spawn Chests from the standard container system. Curated static bundles are out; chests roll their normal floor-scaled tables.

### Recycling Hook

Container-dropped base items that don't match the player's class can be fed to the Blacksmith for materials. Yield is defined in [items.md](../inventory/items.md) (`Recycling:` section) and [depth-gear-tiers.md](depth-gear-tiers.md). This spec only governs *monster* material drops — recycling and equipment-flow are downstream.

### Data Model Sketch

The per-species table lives in `MonsterDropTable`. Equipment fields are removed — material channels only:

```csharp
public record DropTable
{
    public EnemySpecies Species { get; init; }
    public int MonsterTier { get; init; }               // 1..3

    // Material channels
    public string SignatureMaterialId { get; init; } = "";
    public float SignatureMaterialChance { get; init; } // 0.07–0.10
    public MaterialType ThematicGeneric { get; init; }  // Ore | Bone | Hide
}

public enum MaterialType { Ore, Bone, Hide }

public static class MonsterDropTables
{
    public static DropTable ForSpecies(EnemySpecies s);
    public static DropResult Roll(EnemySpecies s, int floor, Random rng);
}

public record DropResult
{
    public string? GenericMaterialId { get; init; }
    public string? SignatureMaterialId { get; init; }
}
```

`MonsterDropTables.Roll` returns only material results. The existing `LootTable.GetGoldDrop` stays as-is — gold is not part of drop tables. The old `RollItemDrop(int enemyLevel)` equipment entrypoint is removed; equipment routing lives in [loot-containers.md](loot-containers.md) → `ContainerLootTable`.

## Acceptance Criteria

- [ ] Every `EnemySpecies` has a populated `DropTable` with signature material, signature rate, and thematic generic.
- [ ] Generic material and signature material roll independently; a single kill can produce both.
- [ ] **No monster ever drops equipment** — the `MonsterDropTable.Roll` return type does not include any equipment field.
- [ ] Boss first-kill bundles fire exactly once per save slot per boss, gated by a save flag, and spawn the bonus container per the table above at the boss's death tile.
- [ ] Treasure room chests use the standard `ContainerLootTable` from [loot-containers.md](loot-containers.md) (no separate curated tables).
- [ ] Zone Saturation and Dungeon Intelligence modifiers apply multiplicatively to material drop rates without breaking the cap.

## Implementation Notes

- The species → table map lives in a `Dictionary<EnemySpecies, DropTable>` initialized at startup in `scripts/logic/MonsterDropTable.cs`. Pure-logic (no Godot dependency) for test coverage.
- Boss first-kill flag storage: `SaveData.BossFirstKillsConsumed` (`HashSet<string>`, key like `"floor10_miniboss"`). Backward-compat: absent field defaults to empty set.

## Open Questions

None.
