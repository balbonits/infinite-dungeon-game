# Boss — Chitin Matriarch (Zone 3)

## Summary

Zone 3 capstone boss (floor 30). Paired species: [Spider](../species/spider.md). A spider that has brooded in zone 3's deepest web-weave so long its egg sacs have fossilized into armor plating. Kite-from-range two-phase fight with summons and a ground-web AOE on Phase 2.

## Current State

**Spec status: LOCKED** via SPEC-BOSS-CHITIN-MATRIARCH-01. Lifted from [boss-art.md §Zone 3 — Chitin Matriarch (skeleton fill)](../boss-art.md) on 2026-04-18 and expanded.

## Design

### 1. Identity

- **Fiction beat:** A spider that has brooded in zone 3's deepest web-weave so long its egg sacs have fossilized into armor plating.
- **Zone + floor ruled:** Zone 3 capstone — floor 30.
- **What corrupts this floor:** A collapsed ceiling cavity where magicule-fog pools and settles on silk.
- **Primary reaction:** `kite-from-range`.

### 2. Stats

Base species Spider at floor 30: HP ≈ 150, Dmg ≈ 18, Speed ≈ 65, XP ≈ 60. Boss multiplier **×6 / ×2.5 / ×0.8 / ×10** (kite-from-range band).

| Stat | Value |
|---|---|
| Base HP | **900** |
| Base Contact Damage | **45** |
| Base Move Speed | **52 px/s** |
| Base XP Yield | **600** |

Target TTK: **55–70 s**.

### 3. AI Pattern

- **Primary:** `ranged-kite` — throws web-globules from a perch; backs off when player closes.
- **Telegraph:** 500 ms web-wind-up with a leg-raise tell.
- **Phase 2 trigger: 50% HP.** Summons 3× spiderlings (1-HP adds) + adds a ground-web AOE that slows player by 50% for 2 s. Aura shifts pale-green → bright toxic-green. `FlashFx.Flash(Color.White, 120ms)`.
- **Phase 3:** N/A.
- **Aggro range:** arena-bound.
- **Leash:** regen 5%/s outside arena.

### 4. Drop-Table Hook

- **First-kill bundle:** (interpolated floor-25 row) 4× Tier 3 generics, 2× Chitin Fragment, + 1× Crate.
- **First-kill unique:** **FORGE-01 Tier 3 pool** (5 uniques).
- **Save-flag:** `floor30_boss_spider`.
- **Repeat-kill:** standard Spider drops.

### 5. Silhouette Readability Constraint

**Must read as "a fortress with legs" at 8 tiles.** Cues: scale 1.8×; egg-sac-armor plating visibly layered on back (additional body part); toxic-green drip particles from fangs (aura).

### 6. Size / Scale Rule

- **Multiplier:** 1.8×
- **Hitbox radius:** 22 px
- **Z-offset:** 0
- **Canvas:** 160 × 160

### 7. Color-Coding Contract

- **Exempt pixels:** fang drip-source (2×1 each fang), 8 compound-eye pixels (cluster).
- **Heightened palette:** shadows 2 steps darker; Spider species signature chitin-black 1 step brighter with iridescent green highlights 2× denser than standard.
- **Aura:** slow toxic-green droplet emission from fangs.
- **Phase colors:** Phase 1 pale green; Phase 2 bright toxic green.

### 8. Art-Spec Pairing

- Paired art ticket: **ART-SPEC-BOSS-01**.
- Pairing status: `[ ] art spec locked  [ ] art assets delivered`.

---

## Acceptance Criteria

- [ ] 500 ms web-wind-up telegraph is visible and dodgeable.
- [ ] Phase 2 summons exactly 3 spiderlings; ground-web AOE applies 50% slow for 2 s.
- [ ] Spiderlings despawn on Matriarch defeat.
- [ ] FlashFx.Flash fires on Phase 2 threshold.
- [ ] First-kill bundle + FORGE-01 Tier 3 unique drop once per save slot; `floor30_boss_spider` gate works.
- [ ] Silhouette test at 8 tiles distinguishes Matriarch from standard Spiders + from other bosses.
- [ ] Ground-web AOE tile is clearly telegraphed before application.

## Implementation Notes

- **Spiderlings:** lightweight visual adds like Pack-Father's phantom wolves — 1 HP, contact damage, no full Spider AI.
- **Ground-web AOE:** apply `MoveSpeed *= 0.5` for 2 seconds when player enters tile; use a visual overlay (matches aura color shift to signal "this is the boss's web").
- **Phase-shift trigger:** one-shot at 50% HP.

## Open Questions

None — spec is locked.
