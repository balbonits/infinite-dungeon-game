# Boss — Volcano Tyrant (Zone 8)

## Summary

Zone 8 capstone boss (floor 80, deepest in starter roster). Base species form: [Orc](../species/orc.md) (deep-zone mix — the Tyrant is an orc reshaped by extreme magicule density into a humanoid furnace). **Three-phase fight** with the most dangerous endgame mechanic: Phase 3 passive heat-aura damages the player over time within 2 tiles of the boss.

## Current State

**Spec status: LOCKED** via SPEC-BOSS-VOLCANO-TYRANT-01. Lifted from [boss-art.md §Zone 8 — Volcano Tyrant (skeleton fill)](../boss-art.md) on 2026-04-18 and expanded.

Note: this boss is one of TWO Phase F bosses that inherit the Orc species base (the other is [Warlord of the Fifth](warlord-fifth.md) at zone 5). The roadmap flagged Zone 8 as "deep-zone mix, orc-form base" — interpreted here as Orc body plan + magma-furnace transformation unique to this encounter.

## Design

### 1. Identity

- **Fiction beat:** A creature born orc, reshaped by magicules into a humanoid furnace. Stands on a permanent fissure of superheated rock in the deepest explored stratum.
- **Zone + floor ruled:** Zone 8 capstone — floor 80 (deepest in starter roster).
- **What corrupts this floor:** A magma vent from below feeds the fissure directly; the Tyrant has stood on it long enough to become it.
- **Primary reaction:** `close-the-gap`.

### 2. Stats

Base species (deep-zone mix, orc-form) at floor 80: HP ≈ 600, Dmg ≈ 45, Speed ≈ 55, XP ≈ 200. Boss multiplier **×8 / ×3 / ×0.8 / ×10**.

| Stat | Value |
|---|---|
| Base HP | **4800** |
| Base Contact Damage | **135** |
| Base Move Speed | **44 px/s** |
| Base XP Yield | **2000** |

Target TTK: **120–180 s**.

### 3. AI Pattern

- **Primary (Phase 1):** `melee-chase` with delayed heavy swings (900 ms telegraph on hammer-swing).
- **Phase 2 trigger: 50% HP.** Ground erupts in three telegraphed fissure-lines that leave persistent lava tiles for 3 s; aura shifts magma-red → incandescent-orange. `FlashFx.Flash(Color.White, 120ms)`.
- **Phase 3 trigger: 25% HP.** The Tyrant's body cracks open, revealing its core; emits a **passive heat-aura that damages the player over time if within 2 tiles**; aura raw white-gold. Player must balance attack uptime against burn damage.
- **Aggro range:** arena-bound.
- **Leash:** regen 5%/s outside arena.

### 4. Drop-Table Hook

- **First-kill bundle:** 5× Arcane materials per type, **5× of any 3 signatures** (zone 8 uses floor-150+ row baseline interpolated), + 2× Chest.
- **First-kill unique:** **FORGE-01 Tier 5 pool**.
- **Save-flag:** `floor80_boss_orc` (note: shares "orc" suffix with Warlord at floor 50 but different floor prefix prevents collision).
- **Repeat-kill:** standard deep-zone drops.

### 5. Silhouette Readability Constraint

**Must read as "a volcano that stood up" at 8 tiles.** Cues: scale 2.2×; one shoulder is a chunk of raw erupting rock, visibly larger than the other (asymmetry); persistent magma-red emission from body cracks (aura); jagged obsidian mantle across shoulders (crown/regalia).

### 6. Size / Scale Rule

- **Multiplier:** 2.2× (**largest boss in the starter roster**)
- **Hitbox radius:** 26 px
- **Z-offset:** 0
- **Canvas:** 160 × 160

### 7. Color-Coding Contract

- **Exempt pixels:** body-crack magma (multiple thin bright lines across torso + limbs), obsidian mantle highlights (scattered pixels), eye glow (4 pixels each, bright).
- **Heightened palette:** shadows **3 steps darker** (this is the deepest boss — the only one in the roster at 3-step shadow depth); Orc signature green-brown supplanted by a new char-black + magma-red duotone.
- **Aura:** magma-red body-crack emission.
- **Phase colors:** Phase 1 magma-red; Phase 2 incandescent-orange; Phase 3 raw white-gold.

### 8. Art-Spec Pairing

- Paired art ticket: **ART-SPEC-BOSS-01** — special note: Phase 3 needs a body-crack-open pose revealing a glowing core + the 2-tile heat-aura visual.
- Pairing status: `[ ] art spec locked  [ ] art assets delivered`.

---

## Acceptance Criteria

- [ ] 900 ms hammer-swing telegraph is visible and dodgeable in Phase 1.
- [ ] Phase 2 spawns exactly 3 fissure-lines; telegraph visible before lava tiles appear; lava persists 3 s.
- [ ] Phase 3 passive heat-aura damages player at a sustainable tick rate (e.g., ~5% max HP per second within 2 tiles) — punishes camping close, doesn't auto-kill.
- [ ] FlashFx.Flash fires on both Phase 2 AND Phase 3 threshold crossings.
- [ ] First-kill bundle drops exactly the listed items; `floor80_boss_orc` gate works and does not collide with `floor50_boss_orc`.
- [ ] Silhouette test at 8 tiles reads as "volcano that stood up" — distinct from Warlord of the Fifth (same species base) and every other boss.
- [ ] Shadow-depth (3 steps darker) renders correctly — visually darker than all other bosses.

## Implementation Notes

- **Three-phase boss:** two one-shot flags.
- **Phase 2 fissure-lines:** three visible line-telegraphs emerging from the boss in chosen directions; after telegraph duration, lava tiles appear along each line and persist 3 s. Damage ticks on any player tile overlap.
- **Phase 3 passive heat-aura:** compute 2-tile radius around boss each frame; if player inside, apply DOT at ~5% max HP/s. This is the signature endgame mechanic — the fight becomes a dance between getting close enough to hit and pulling back to survive.
- **Species-base disambiguation:** impl should share Orc body plan with Warlord of the Fifth but swap:
  - Aura color scheme (orange-iron → magma-red + char-black)
  - Silhouette constraint (trophy-regalia → obsidian-mantle + rock-shoulder)
  - Phase mechanics (thrown axes → ground fissures + passive heat)
  - Stat multipliers (×6/2.5 at floor 50 vs ×8/3 at floor 80 per band shift from kite-from-range to close-the-gap)
- **Save-flag collision note:** `floor50_boss_orc` and `floor80_boss_orc` are distinct strings — the floor number prefix prevents collision. If the save-flag scheme is ever refactored to drop the floor prefix, both bosses need renamed flags.

## Open Questions

None — spec is locked.
