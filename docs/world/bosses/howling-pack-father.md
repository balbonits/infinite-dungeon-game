# Boss — Howling Pack-Father (Zone 2)

## Summary

Zone 2 capstone boss (floor 20). Paired species: [Wolf](../species/wolf.md). A wolf that has led the zone-2 pack long enough to grow as large as the pack it rules. Two-phase burst-down-fast fight — Phase 2 summons phantom wolf-sprites as visual-only pack minions.

## Current State

**Spec status: LOCKED** via SPEC-BOSS-HOWLING-PACK-FATHER-01. Lifted from [boss-art.md §Zone 2 — Howling Pack-Father (skeleton fill)](../boss-art.md) on 2026-04-18 and expanded with acceptance criteria + impl notes.

## Design

### 1. Identity

- **Fiction beat:** A wolf that has led the zone-2 pack long enough to grow as large as the pack it rules.
- **Zone + floor ruled:** Zone 2 capstone — floor 20.
- **What corrupts this floor:** A den of layered scavenged kills where ambient magicule pools in drying blood.
- **Primary reaction:** `burst-down-fast`.

### 2. Stats

Base species Wolf at floor 20: HP ≈ 90, Dmg ≈ 12, Speed ≈ 70, XP ≈ 45. Boss multiplier **×5 / ×2 / ×0.8 / ×10** (burst-down-fast band).

| Stat | Value |
|---|---|
| Base HP | **450** |
| Base Contact Damage | **24** |
| Base Move Speed | **56 px/s** |
| Base XP Yield | **450** |

**Speed override allowance:** Pack-Father may hold species speed of 70 px/s if art-lead's silhouette shows an agile wolf-shape; spec recommends default 0.8× multiplier to preserve "boss heft." Final call at paired-art review time.

Target TTK: **30–40 s**.

### 3. AI Pattern

- **Primary:** `melee-chase` with lunge attacks — 400 ms short-dash telegraph.
- **Phase 2 trigger: 50% HP.** Summons 2× phantom wolf-sprites (visual-only minions dealing contact damage but 1 HP each — the "pack" fantasy). Aura shifts neutral grey → blood-red. `FlashFx.Flash(Color.White, 120ms)` on threshold.
- **Phase 3:** N/A.
- **Aggro range:** arena-bound.
- **Leash:** regen 5%/s outside arena.

### 4. Drop-Table Hook

- **First-kill bundle:** (interpolated floor-25 row) 3× Tier 2 generics, 2× Wolf Pelt, + 1× Crate.
- **First-kill unique:** **FORGE-01 Tier 2 pool** (4 uniques).
- **Save-flag:** `floor20_boss_wolf`.
- **Repeat-kill:** standard Wolf drops.

### 5. Silhouette Readability Constraint

**Must read as "a wolf the size of a small cave bear" at 8 tiles.** Cues: scale 1.8×; one massively overgrown shoulder hump (asymmetry); low red breath-steam emission at the muzzle (aura).

### 6. Size / Scale Rule

- **Multiplier:** 1.8×
- **Hitbox radius:** 22 px
- **Z-offset:** 0
- **Canvas:** 160 × 160

### 7. Color-Coding Contract

- **Exempt pixels:** eye glow (both eyes, bright yellow), muzzle breath-steam source (3×2 cluster).
- **Heightened palette:** shadows 1 step darker; Wolf species signature grey-brown accent 2 steps brighter on the back-ridge fur.
- **Aura:** red breath-steam from muzzle, slow pulse.
- **Phase colors:** Phase 1 pale red; Phase 2 deep red.

### 8. Art-Spec Pairing

- Paired art ticket: **ART-SPEC-BOSS-01**.
- Pairing status: `[ ] art spec locked  [ ] art assets delivered`.

---

## Acceptance Criteria

- [ ] Phase 2 summons exactly 2 phantom wolf-sprites, each 1 HP, each dealing standard contact damage.
- [ ] Phantom wolves despawn if Pack-Father is defeated (no orphan visuals).
- [ ] 400 ms lunge telegraph is visible and dodgeable.
- [ ] FlashFx.Flash fires on Phase 2 threshold.
- [ ] First-kill bundle + FORGE-01 Tier 2 unique drop once per save slot; `floor20_boss_wolf` gate works.
- [ ] Silhouette test at 8 tiles distinguishes Pack-Father from standard Wolves + from other bosses.

## Implementation Notes

- **Phantom wolves are visual adds, not full Wolf enemies.** Use a lightweight sprite + contact-damage hitbox; do not spawn full `EnemySpecies.Wolf` entities (avoids pack-AI entanglement + XP-farming exploit).
- **Phase-shift trigger:** one-shot at 50% HP, same pattern as Bone Overlord.
- **Speed override flag:** if art-lead selects the agile-silhouette variant, override Speed from 56 → 70 px/s in SpeciesDatabase boss entry; document which variant shipped in the impl PR.

## Open Questions

None — spec is locked.
