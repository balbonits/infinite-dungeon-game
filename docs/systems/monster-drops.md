# Monster Drop Tables

## Summary

Every enemy species has a bespoke drop table that mixes tiered generic materials, one species-signature material, and a floor-appropriate base-equipment pool. Drop-rate and quality curves are inherited from SPEC-06b (previously inline in [items.md](../inventory/items.md)); this spec is now the source of truth for drop behavior and supersedes the inline text.

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

Every species owns one `DropTable`. A table has three independent channels that roll per kill:

| Channel | What it rolls | Rate | Notes |
|---|---|---|---|
| **Equipment** | One base item, slot chosen with equal weight from the species' allowed-slot pool | See **Equipment Drop Rates** | Subject to floor quality roll ([depth-gear-tiers.md](depth-gear-tiers.md)) |
| **Generic material** | One tiered generic material (ore / bone / hide) sized to the current floor bracket | 25% flat | Tier 1–5 gated by floor (see Material Tiers below) |
| **Signature material** | The species' one unique material | 5–10% bonus roll (species-dependent) | Rolled **in addition** to the generic material — they don't displace each other |

Channels are independent: a single kill can drop equipment + generic mat + signature mat in the same frame (rare but possible). This replaces the current sequential `LootTable.RollItemDrop()` short-circuit.

### Species → Material & Archetype Map

Locked per [item-catalog.md](../inventory/item-catalog.md) — this table is the binding between species and their loot silhouette. Each species has a **signature material** and a **preferred slot pool** that tilts which base items drop most often from that species.

| Species | Zone (primary) | Signature Material | Signature Rate | Preferred Slots (weighted) | All Slots Allowed? |
|---|---|---|---|---|---|
| Skeleton | 1 | Bone Dust | 10% | Head, Body, Main Hand (Warrior-voice swords) | Yes |
| Goblin | 2 | Goblin Tooth | 10% | Main Hand (axe), Arms, Ring | Yes |
| Bat | 1 | Echo Shard | 8% | Neck, Ring, Ammo (quivers) | Yes |
| Wolf | 2 | Wolf Pelt | 10% | Body, Legs, Feet | Yes |
| Orc | 3 | Orc Tusk | 10% | Main Hand (hammer), Body, Off Hand (shield) | Yes |
| Dark Mage | 4 | Arcane Residue | 7% | Head (crown), Off Hand (spellbook), Main Hand (wand) | Yes |
| Spider | 3 | Chitin Fragment | 8% | Arms, Legs, Ammo (quivers — poison imbues tilted) | Yes |

**"Preferred slots" weighting — currently uniform, reserved for future tuning.** Per-species equipment drop is **uniform across all valid slots at Phase 1** — every species rolls every allowed slot with equal weight. The **Preferred Slots** column above documents the *thematic intent* for each species (a Skeleton "should" feel like it drops helmets and swords), and the 2× weighting will turn on when monster variety per zone expands beyond the current ~2 species/zone. Until then, going thematic with so few species would make drops feel repetitive.

When the expansion happens: preferred slots get **2×** the roll weight of non-preferred valid slots; each species still drops every slot occasionally to preserve "anything can drop" feel.

### Equipment Drop Rates (from SPEC-06b)

Restated here verbatim — this is the source of truth. [items.md](../inventory/items.md) now links here.

```
drop_chance(tier) = base_rate(tier) + floor_bonus
base_rate: Tier 1 = 8%, Tier 2 = 12%, Tier 3 = 18%
floor_bonus = floor_number * 0.1% (caps at +5% at floor 50)
```

| Floor | Tier 1 Drop % | Tier 2 Drop % | Tier 3 Drop % |
|---|---|---|---|
| 1 | 8.1% | 12.1% | 18.1% |
| 10 | 9.0% | 13.0% | 19.0% |
| 25 | 10.5% | 14.5% | 20.5% |
| 50+ | 13.0% (capped) | 17.0% | 23.0% |

**Monster tier** is derived from species difficulty and zone. Tier mapping (locked here, matches [spawning.md](spawning.md) / [floor-scaling.md](floor-scaling.md) intent):

| Tier | Species examples | Zones |
|---|---|---|
| Tier 1 | Skeleton, Goblin, Bat | 1–2 |
| Tier 2 | Wolf, Spider | 2–3 |
| Tier 3 | Orc, Dark Mage | 3–4+ |

Tier is species-property, not per-spawn — a Skeleton is always Tier 1 regardless of zone saturation. Zone Saturation and Dungeon Intelligence modifiers act multiplicatively on these base rates (see [zone-saturation.md](zone-saturation.md), [dungeon-intelligence.md](dungeon-intelligence.md)).

### Base Quality Distribution (from SPEC-06b)

Rolled **after** the equipment channel succeeds. Extended to include [depth-gear-tiers.md](depth-gear-tiers.md) tiers.

Brackets align to the unified Floor-Bracket tier system from [item-generation.md](item-generation.md) § Floor Brackets = Tiers.

| Floor Range | Tier | Normal | Superior | Elite | Masterwork | Mythic | Transcendent |
|---|---|---|---|---|---|---|---|
| 1–10 | 1 | 100% | 0% | 0% | 0% | 0% | 0% |
| 11–25 | 2 | 80% | 20% | 0% | 0% | 0% | 0% |
| 26–50 | 3 | 60% | 35% | 5% | 0% | 0% | 0% |
| 51–100 | 4 | 30% | 45% | 20% | 5% | 0% | 0% |
| 100+ | 5 | 10% | 25% | 35% | 25% | 5% | 0% |

**Transcendent quality** does not appear in the distribution above — it has an additional floor-150 gate per [depth-gear-tiers.md](depth-gear-tiers.md). Once the player crosses floor 150 the distribution shifts to: Normal 5% / Superior 15% / Elite 30% / Masterwork 30% / Mythic 15% / Transcendent 5%.

### Slot Rolling Rule

When the equipment channel succeeds:

1. Filter the species' allowed slot pool to slots valid at the current floor (always all 10 slot types for now; reserved for future floor-gated slot restrictions).
2. Build a weighted pool: preferred slots count 2x, others 1x.
3. Roll a slot uniformly from the weighted pool.
4. Pick a **base item** from [item-catalog.md](../inventory/item-catalog.md) whose slot matches and whose tier is appropriate for the current floor's item-level bracket. If multiple candidates exist (e.g., 3 Warrior-voice Main Hand archetypes at Tier 2), roll uniformly across them.
5. Apply quality tier from the distribution table above.
6. Emit the item with zero affixes (magic comes from the Blacksmith).

**Class-affinity items drop for every class.** Matched class gets +25% per [equipment.md](equipment.md). Mismatched gear is still droppable; it's recyclable at the Blacksmith. This is intentional and encourages cross-class material flow.

### Material Tiers (generic)

Five tiers, binding directly to floor brackets. Names are locked in [item-catalog.md](../inventory/item-catalog.md) → Materials section; summarized here:

| Tier | Floor Range | Ore | Bone | Hide |
|---|---|---|---|---|
| 1 | 1–10 | Iron Ore | Rough Bone | Rough Hide |
| 2 | 11–25 | Steel Ingot | Standard Bone | Standard Hide |
| 3 | 26–50 | Mithril Ore | Fine Bone | Fine Hide |
| 4 | 51–100 | Orichalcum Ore | Masterwork Bone | Masterwork Hide |
| 5 | 100+ | Dragonite Ore | Top-Shelf Bone | Top-Shelf Hide |

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

Both channels roll independently — a lucky kill can yield both generic and signature material at once. A single kill **can** therefore produce up to 3 items (equipment + generic mat + signature mat). This is intentional ammo for the loot-chase dopamine loop; the low signature rate keeps it rare-feeling.

### Boss First-Kill Drops

Boss encounters (first kill per save slot, per boss) follow a **guaranteed-bundle** rule rather than the probability tables.

| Boss depth | Guaranteed rare material bundle | Guaranteed equipment |
|---|---|---|
| Floor 10 miniboss | 3× Tier 2 generics, 1× signature (zone's dominant species) | 1 item, quality roll shifted +1 bracket (so Superior guaranteed at floor 10) |
| Floor 25 miniboss | 4× Tier 3 generics, 2× signature | 1 item, Elite guaranteed |
| Floor 50 elite | 5× Tier 4 generics, 3× signature, 1× Bone Dust (Skeleton King callback) | 1 item, Masterwork guaranteed |
| Floor 100 elite | 5× Tier 5 generics, 3× signature, 1× of every Zone 1–3 signature | 1 item, Mythic guaranteed |
| Floor 150+ boss | 5× Arcane materials per type, 5× of any 3 signatures | 1 item, Transcendent guaranteed |

**One-time only per save slot.** Subsequent kills of the same boss instance use the normal species drop table with a +1 quality bracket shift.

### Treasure Room Chests

Treasure rooms exist in the dungeon generation (see [spawning.md](spawning.md) future hooks). They do **not** use the per-species tables — they're curated static bundles.

Chest tier aligns to the unified floor brackets from [item-generation.md](item-generation.md) § Floor Brackets = Tiers.

| Chest tier | Floor Range | Contents |
|---|---|---|
| Small | 1–25 (Tiers 1–2) | 5–8 generic materials (floor tier), 20% chance of 1 equipment at current quality, 10% chance of 1 signature from any zone-available species |
| Large | 26–50 (Tier 3) | 8–12 generic materials, 60% chance of 1 equipment, 30% chance of 1 signature, 5% chance of 1 consumable from the Guild Maid's pool |
| Grand | 51+ (Tiers 4–5) | 12–18 generic materials (Tier 4–5), **guaranteed** 1 equipment at current quality distribution, 50% chance of 2+ signatures, guaranteed 1 consumable |

Treasure rooms are also a **primary regurgitation channel** — see [dungeon-regurgitation.md](dungeon-regurgitation.md).

### Recycling Hook

Dropped base items that don't match the player's class can be fed to the Blacksmith for materials. Yield is defined in [items.md](../inventory/items.md) (`Recycling:` section) and [depth-gear-tiers.md](depth-gear-tiers.md). This spec only governs *what drops* — recycling is downstream.

### Data Model Sketch

The per-species table will live in a new `MonsterDropTable` module. Sketch (C# — for implementation guidance only, the ITEM-02 team writes the real thing):

```csharp
public record DropTable
{
    public EnemySpecies Species { get; init; }
    public int MonsterTier { get; init; }               // 1..3

    // Material channels
    public string SignatureMaterialId { get; init; } = "";
    public float SignatureMaterialChance { get; init; } // 0.07–0.10
    public MaterialType ThematicGeneric { get; init; }  // Ore | Bone | Hide

    // Equipment channel
    public EquipSlot[] PreferredSlots { get; init; } = System.Array.Empty<EquipSlot>();
    // AllowedSlots = all 10 slot types; filtered via catalog availability at roll time
}

public enum MaterialType { Ore, Bone, Hide }

public static class MonsterDropTables
{
    public static DropTable ForSpecies(EnemySpecies s);
    public static DropResult Roll(EnemySpecies s, int floor, Random rng);
}

public record DropResult
{
    public string? EquipmentItemId { get; init; }       // null if no drop
    public BaseQuality? EquipmentQuality { get; init; }
    public string? GenericMaterialId { get; init; }
    public string? SignatureMaterialId { get; init; }
}
```

`MonsterDropTables.Roll` replaces the current `LootTable.RollItemDrop(int enemyLevel)` entrypoint. The existing `LootTable.GetGoldDrop` stays as-is — gold is not part of drop tables.

## Acceptance Criteria

- [ ] Every `EnemySpecies` has a populated `DropTable` with signature material, signature rate, thematic generic, and preferred slots.
- [ ] Equipment drop chances match the SPEC-06b curves above (±0.5% tolerance for rounding).
- [ ] Quality distribution by floor range matches [depth-gear-tiers.md](depth-gear-tiers.md) extended table.
- [ ] Slot roll respects the 2x preferred-slot weighting.
- [ ] Generic material and signature material roll independently; a single kill can produce both.
- [ ] Dropped equipment has zero affixes, correct `ClassAffinity`, and correct `ItemLevel = max(1, floor)`.
- [ ] Boss first-kill bundles fire exactly once per save slot per boss, gated by a save flag.
- [ ] Treasure room chests pull from the curated tables, not the per-species pool.
- [ ] Zone Saturation and Dungeon Intelligence modifiers apply multiplicatively to equipment drop rates without breaking the cap.

## Implementation Notes

- The species → table map should be a `Dictionary<EnemySpecies, DropTable>` initialized at startup in a new `scripts/logic/MonsterDropTables.cs`. Keep it pure-logic (no Godot dependency) for test coverage.
- Item-catalog IDs referenced by the tables are defined in [item-catalog.md](../inventory/item-catalog.md) and will live in `ItemDatabase.cs` once ITEM-01 lands. Until then, the table references are strings that can be unit-tested against the catalog doc.
- Floor → item-level is already settled: `itemLevel = max(1, floorNumber)` (see [item-generation.md](item-generation.md)). Don't re-derive it here.
- Boss first-kill flag storage: extend `SaveData` with a `HashSet<string> BossFirstKillsConsumed` (string key like `"floor10_miniboss"`). Backward-compat: absent field defaults to empty set.
- When the equipment channel succeeds but no catalog base exists for the rolled slot at the current floor (e.g., a catalog gap), the roll falls through to "no drop" rather than crashing. Log a warning; this is a content-authoring bug surface.
- Respect [equipment.md](equipment.md) ammo-slot semantics: quiver drops only for species whose preferred pool includes Ammo (Bat, Spider per the table above). Other species never roll Ammo even when all slots are nominally allowed — prevents the weird UX of a Skeleton dropping a fire-imbued quiver on floor 3.

## Open Questions

None.
