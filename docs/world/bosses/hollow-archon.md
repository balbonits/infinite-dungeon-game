# Boss — Hollow Archon (Zone 4)

## Summary

Zone 4 capstone boss (floor 40). Paired species: [Dark Mage](../species/darkmage.md). A dark mage whose body has mostly disintegrated — now a floating robe held up by the magicule-current it once spent lifetimes channeling. Caster kite-from-range two-phase fight with a Phase-2 ground-wave AOE.

## Current State

**Spec status: LOCKED** via SPEC-BOSS-HOLLOW-ARCHON-01. Lifted from [boss-art.md §Zone 4 — Hollow Archon (skeleton fill)](../boss-art.md) on 2026-04-18 and expanded.

## Design

### 1. Identity

- **Fiction beat:** A dark mage whose body has mostly disintegrated, now a floating robe held up by the magicule-current it once spent lifetimes channeling.
- **Zone + floor ruled:** Zone 4 capstone — floor 40.
- **What corrupts this floor:** An ancient spell-circle etched into the floor still faintly active, concentrating ambient magicules vertically through this tile.
- **Primary reaction:** `kite-from-range`.

### 2. Stats

Base species Dark Mage at floor 40: HP ≈ 210, Dmg ≈ 25, Speed ≈ 45, XP ≈ 80. Boss multiplier **×6 / ×2.5 / ×0.8 / ×10**.

| Stat | Value |
|---|---|
| Base HP | **1260** |
| Base Contact Damage | **62** |
| Base Move Speed | **36 px/s** |
| Base XP Yield | **800** |

Target TTK: **60–75 s**.

### 3. AI Pattern

- **Primary:** `caster` — floats in place, channels ranged bolts.
- **Telegraph:** 600 ms bolt wind-up with robe-flare tell.
- **Phase 2 trigger: 50% HP.** Adds a ground-wave AOE attack on a 1200 ms telegraph (visible floor-glyph); aura shifts dim purple → bright violet. `FlashFx.Flash(Color.White, 120ms)`.
- **Phase 3:** N/A.
- **Aggro range:** arena-bound.
- **Leash:** regen 5%/s outside arena.

### 4. Drop-Table Hook

- **First-kill bundle:** (floor-50 row interpolated at floor 40) 5× Tier 4 generics, 3× Arcane Residue, 1× Bone Dust, + 1× Chest.
- **First-kill unique:** **FORGE-01 Tier 4 pool** (6 uniques).
- **Save-flag:** `floor40_boss_darkmage`.
- **Repeat-kill:** standard Dark Mage drops.

### 5. Silhouette Readability Constraint

**Must read as "an empty robe that moves with intent" at 8 tiles.** Cues: scale 1.8×; floating broken staff orbiting independently of the robe (additional body part); persistent purple-violet magicule haze (aura).

### 6. Size / Scale Rule

- **Multiplier:** 1.8×
- **Hitbox radius:** 22 px
- **Z-offset:** **+24 px** (airborne — hovers above ground)
- **Canvas:** 160 × 160

### 7. Color-Coding Contract

- **Exempt pixels:** staff-head gem (3×3 cluster), robe-interior void (4×4 cluster at "chest" — the hollow), eye void (4 pixels).
- **Heightened palette:** shadows 2 steps darker; Dark Mage species signature purple 2 steps brighter and extended to the staff orbit particle trail.
- **Aura:** dense violet magicule haze surrounding the robe.
- **Phase colors:** Phase 1 dim purple; Phase 2 bright violet.

### 8. Art-Spec Pairing

- Paired art ticket: **ART-SPEC-BOSS-01**.
- Pairing status: `[ ] art spec locked  [ ] art assets delivered`.

---

## Acceptance Criteria

- [ ] 600 ms bolt wind-up telegraph is visible and dodgeable.
- [ ] Phase 2 adds a 1200 ms ground-wave telegraph with visible floor-glyph before AOE lands.
- [ ] FlashFx.Flash fires on Phase 2 threshold.
- [ ] First-kill bundle + FORGE-01 Tier 4 unique drop once per save slot; `floor40_boss_darkmage` gate works.
- [ ] Silhouette test at 8 tiles distinguishes Archon from standard Dark Mages + from other bosses.
- [ ] Airborne z-offset +24 px renders correctly in iso Y-sort (flyer above grounded sprites per [iso-rendering.md](../../systems/iso-rendering.md)).

## Implementation Notes

- **Ground-wave AOE:** telegraph fires a glyph-visual on the floor; after 1200 ms, damage applies in the glyph's radius. Player has the full wind-up window to step out.
- **Floating staff orbit:** separate sprite sub-node with a slow orbital rotation around the robe's anchor. Purely cosmetic; hitbox stays on the robe-body center.
- **Phase-shift trigger:** one-shot at 50% HP.
- **Airborne status:** set `ZOffset = 24` on spawn; iso-rendering Y-sort reads `ZOffset` to render above ground-level sprites.

## Open Questions

None — spec is locked.
