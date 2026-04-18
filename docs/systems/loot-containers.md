# Loot Containers (SPEC-LOOT-01)

## Summary

Equipment drops shift from monster kills to world containers. Three container tiers — Jar, Crate, Chest — spawn during floor generation. Monsters keep gold + XP + materials but never drop equipment. Containers are the **sole** equipment source.

## Current State

**Spec status: DRAFT (2026-04-17).** Supersedes the equipment channel in [monster-drops.md](monster-drops.md) and the ITEM-02b ammo-channel exception.

Related code (none yet):
- New file: `scripts/Container.cs` (interactable scene)
- New file: `scripts/logic/ContainerLootTable.cs` (pure rolling)
- Modified: `scripts/Dungeon.cs` (floor-gen container placement)
- Modified: `scripts/logic/MonsterDropTable.cs` (strip equipment channel)

Depends on: [item-generation.md](item-generation.md) (item-level + affix scaling), [item-catalog.md](../inventory/item-catalog.md) (the IDs to roll), [monster-drops.md](monster-drops.md) (material channels — unchanged), [floor-scaling.md](floor-scaling.md).

## Why

Spec direction (2026-04-17): *"monsters, even elites, won't drop equipments. we're updating the specs for equipment to only appear on chests, crates, jars, etc."*

Splitting equipment from monster kills:
- Lets monsters own a fast, predictable kill→gold/XP/mats loop.
- Makes equipment a moment-of-discovery event tied to the world, not the kill count.
- Matches the genre reference (Diablo, PoE) where world containers are the loot heartbeat alongside boss/elite drops.

## Design

### Loot stream split

| Source | Drops |
|---|---|
| **Monster kill** | Body parts (fangs, skin, pelts — i.e., signature materials) + generic materials (ores, plants, wood) + gold + XP. **No equipment.** |
| **Container** | Same as monsters (body parts, mats, gold) **plus equipment** — equipment is container-exclusive. Spawn count and contents both RNG, scaling on size AND floor depth. |

### Container hierarchy

Three sizes with distinct loot profiles. Spawn counts AND contents both scale on **size + floor depth**.

| Size | Visual | Interaction | Loot identity |
|---|---|---|---|
| **Jar / Pot** | Small ground prop | Walk up + press [S] Interact | Mostly materials + small things (rings, necks, consumables) |
| **Crate / Barrel** | Mid-size ground prop | Walk up + press [S] Interact | Bulk of everything — heavy on materials |
| **Chest** | Large ornate prop | Walk up + press [S] Interact | Equipment-heavy + rare materials |

**All containers use interact (`[S]`) — none are breakable via combat.** Single-use: container is consumed (despawned) once opened.

### Spawn counts per floor

Random in range. Upper bound increases with depth so deeper floors feel meaningfully loot-richer (generous scaling):

| Size | Base range (floor 1) | Bonus | Floor 50 | Floor 100 |
|---|---|---|---|---|
| Jar | `randi(4,8)` | `+1` to upper every 10 floors | `randi(4,13)` | `randi(4,18)` |
| Crate | `randi(2,5)` | `+1` to upper every 10 floors | `randi(2,10)` | `randi(2,15)` |
| Chest | `randi(0,2)` | `+1` to upper every 10 floors | `randi(0,7)` | `randi(0,12)` |

**Min-floor guarantee:** if total = 0, force-spawn 1 jar so every floor has at least one container.

### Loot tables (per container)

Each container rolls multiple **independent channels**. Material and gold channels roll a per-channel chance; the equipment channel rolls a *count* of distinct equipment items, all from the size-eligible slot pool, with **no slot-type duplicates per container** (no two rings, no two chestpieces).

Floor-scaling rules common to all sizes:
- **Equipment item-level:** floor → item-level per [item-generation.md](item-generation.md) (existing).
- **Material tier:** floor → tier per [monster-drops.md](monster-drops.md) Material Tiers (existing).
- **Gold range:** scales linearly with floor — values shown are floor-1 base; multiply by `1 + floor × 0.05` (floor 20 = 2×, floor 100 = 6×).

#### Jar
| Channel | Chance | What |
|---|---|---|
| Gold | 60% | 5–25g (floor-scaled) |
| Generic material | 30% | 1× material |
| Small thing | 25% | 1× from {Ring, Neck, Consumable} pool only — no armor / weapons / quivers / off-hand / ammo |

#### Crate (bulk)
| Channel | Chance | What |
|---|---|---|
| Gold | 70% | 50–100g (floor-scaled) |
| Generic material | 100% | **2–4×** generic mats (the bulk feature) |
| Signature material | 30% | 0–1× signature mat (any species in the floor's zone) |
| Equipment | 60% | **0–2×** items, distinct slots, drawn from the full equipment pool |

#### Chest (equipment-heavy)
| Channel | Chance | What |
|---|---|---|
| Gold | 100% | 100–500g (floor-scaled) |
| Generic material | 50% | 1× material |
| Signature material | 50% | 1× signature mat (rare-feeling, weighted toward floor's zone) |
| Equipment | 100% | **1–3×** items, distinct slots, drawn from the full equipment pool |

### Spawn placement

During `Dungeon` floor generation, after room carve + wall placement:

1. Compute per-floor counts (table above), apply min-1 guarantee.
2. Scatter on random valid floor tiles. Exclude:
   - Player spawn tile + 1-tile buffer
   - Stairs-up / stairs-down + 1-tile buffer
   - NPC tiles
   - Other already-placed container tiles (no overlap)
3. Each container is a Godot scene with:
   - `Sprite2D` (visuals)
   - `Area2D` for the interact zone (reuses NPC `[S] Interact` prompt model)
   - `StaticBody2D` so the player can't walk through it
   - **No** combat-damage path; containers are not in the `Enemies` group and ignore `TakeDamage`.
4. On open: pop loot via the same `FloatingText` + `Item.cs` drop path used today by enemy drops, then `QueueFree` the container (single-use).

## Removed from monster-drops.md

- The **Equipment** channel row on every per-species drop table.
- The **Preferred Slots** column (no longer meaningful for monsters).
- The **ITEM-02b** species-gated quiver channel (Bat/Spider) — superseded entirely. Quivers now drop from containers like any other equipment.
- The **All Slots Allowed?** column.

## Retained in monster-drops.md

- Generic material channel (25% flat per kill).
- Signature material channel (5–10% per species).
- The species → signature material binding (Echo Shard / Chitin Fragment / etc.).
- Gold/XP drops (existing behavior).

## Acceptance Criteria

1. No monster ever drops equipment — `MonsterDropTable` returns only materials/gold/XP.
2. Every floor has ≥1 container at gen time; counts within the size + floor table above.
3. Container types are visually + behaviorally distinct (jars/crates breakable via attack, chests via interact).
4. Equipment from containers routes through `ItemGenerator` with the same floor item-level / affix curves the old monster equipment drops used.
5. **Slot-type uniqueness:** no container ever drops two items of the same slot type (no 2 rings, no 2 chestpieces from one chest).
6. Jar equipment pool is **strictly** {Ring, Neck, Consumable}. Crate + Chest draw from the full equipment pool.
7. Crate generic-material channel rolls **2–4** mats (the "bulk" feature), distinct from Jar/Chest's 1×.
8. Existing tests for monster material/signature drops still pass; new tests cover container roll distributions, the min-1-per-floor guarantee, and the slot-type uniqueness rule.

## Implementation Tickets

- **SPEC-LOOT-01** — this doc.
- **LOOT-01** — implement `Container.cs` + `ContainerLootTable.cs`; floor-gen integration; rip equipment channel from `MonsterDropTable`. Add unit tests for table distributions + min-1 guarantee.
- **ART-15** — sprites for Jar (1–3 visual variants), Crate (1–2 variants), Chest (closed + open visual states).

## Locked Decisions (2026-04-17 sign-off)

1. **All containers interact-only.** No combat-break path; jars/crates/chests all use `[S] Interact`. Containers are single-use (consumed on open).
2. **Signature material zone tilt.** Containers weight signature mats toward the floor's zone species (e.g., chest on floor 5 more likely to drop Bone Dust + Echo Shard than Orc Tusk). Mirrors monster signature drops.
3. **Generous spawn scaling.** `+1 to upper bound every 10 floors` (floor 100 = `0–12` chests, `2–15` crates, `4–18` jars). Drives the deep-floor loot-rich feel without changing the size hierarchy.
