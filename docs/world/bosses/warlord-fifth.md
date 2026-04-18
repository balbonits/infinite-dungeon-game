# Boss — Warlord of the Fifth (Zone 5)

## Summary

Zone 5 capstone boss (floor 50). Paired species: [Orc](../species/orc.md). An orc that has survived every champion of zone 5 for generations, wearing the combined regalia of every previous warlord it killed stacked in layers. Kite-from-range with thrown axes, plus a Phase-2 charge that halves throw cooldown.

## Current State

**Spec status: LOCKED** via SPEC-BOSS-WARLORD-FIFTH-01. Lifted from [boss-art.md §Zone 5 — Warlord of the Fifth (skeleton fill)](../boss-art.md) on 2026-04-18 and expanded.

## Design

### 1. Identity

- **Fiction beat:** An orc that has survived every champion of zone 5 for generations, wearing the combined regalia of every previous warlord it killed stacked in layers.
- **Zone + floor ruled:** Zone 5 capstone — floor 50.
- **What corrupts this floor:** Centuries of accumulated blood-soaked trophy iron exhale magicule-iron fumes.
- **Primary reaction:** `kite-from-range` (throws weapons, closes briefly).

### 2. Stats

Base species Orc at floor 50: HP ≈ 340, Dmg ≈ 32, Speed ≈ 55, XP ≈ 110. Boss multiplier **×6 / ×2.5 / ×0.8 / ×10**.

| Stat | Value |
|---|---|
| Base HP | **2040** |
| Base Contact Damage | **80** |
| Base Move Speed | **44 px/s** |
| Base XP Yield | **1100** |

Target TTK: **65–80 s**.

### 3. AI Pattern

- **Primary:** `ranged-kite` — throws axes from mid-range, advances briefly to slam if cornered.
- **Telegraph:** 500 ms throw wind-up with arm-cock tell.
- **Phase 2 trigger: 50% HP.** Throw cooldown halved; adds a charge attack on a 700 ms wind-up. Aura shifts iron-grey → hot-iron-orange. `FlashFx.Flash(Color.White, 120ms)`.
- **Phase 3:** N/A.
- **Aggro range:** arena-bound.
- **Leash:** regen 5%/s outside arena.

### 4. Drop-Table Hook

- **First-kill bundle:** (floor-50 row) 5× Tier 4 generics, 3× Orc Tusk, 1× Bone Dust, + 1× Chest.
- **First-kill unique:** **FORGE-01 Tier 5 pool** (10 uniques).
- **Save-flag:** `floor50_boss_orc`.
- **Repeat-kill:** standard Orc drops.

### 5. Silhouette Readability Constraint

**Must read as "a warrior dragging its own history behind it" at 8 tiles.** Cues: scale 2.0×; stacked-trophy armor — visibly mismatched plates layered on shoulders and back (crown/regalia); hot-iron-orange glow from the layered plates' seams (aura).

### 6. Size / Scale Rule

- **Multiplier:** 2.0×
- **Hitbox radius:** 24 px
- **Z-offset:** 0
- **Canvas:** 160 × 160

### 7. Color-Coding Contract

- **Exempt pixels:** iron-glow seams (thin bright lines between every regalia layer), tusk highlight (2 pixels per tusk), eye glow (2 pixels).
- **Heightened palette:** shadows 2 steps darker; Orc species signature green-brown skin 1 step darker (the Warlord is *older* than other orcs); iron regalia introduces a new bright orange accent carried by the aura.
- **Aura:** slow orange heat-shimmer from armor seams.
- **Phase colors:** Phase 1 iron-grey; Phase 2 hot-iron-orange.

### 8. Art-Spec Pairing

- Paired art ticket: **ART-SPEC-BOSS-01**.
- Pairing status: `[ ] art spec locked  [ ] art assets delivered`.

---

## Acceptance Criteria

- [ ] 500 ms throw wind-up telegraph is visible and dodgeable.
- [ ] Phase 2 halves throw cooldown and adds the 700 ms charge wind-up.
- [ ] FlashFx.Flash fires on Phase 2 threshold.
- [ ] First-kill bundle + FORGE-01 Tier 5 unique drop once per save slot; `floor50_boss_orc` gate works.
- [ ] Silhouette test at 8 tiles distinguishes Warlord from standard Orcs + from other bosses (esp. Volcano Tyrant, same species family).
- [ ] Thrown axe projectile is a distinct entity with its own hitbox and travel path (not an instant hitscan).

## Implementation Notes

- **Thrown axes:** use the existing `Projectile` class with an orc-axe sprite variant. Travel speed ~200 px/s; disappears after 800 ms if nothing is hit.
- **Phase 2 cooldown halving:** multiply the base throw cooldown by 0.5 on phase transition; store the original as a constant so the curve is readable.
- **Charge attack:** Phase 2's 700 ms charge wind-up telegraph uses a forward-lean pose; on release, boss moves ~3 tiles forward in 400 ms for a slam hit.
- **Dual-boss note:** this species feeds both Warlord (zone 5) and Volcano Tyrant (zone 8). Impl should share the base Orc body plan; silhouette + aura + Phase-2 mechanics differ per boss.

## Open Questions

None — spec is locked.
