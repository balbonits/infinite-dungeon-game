# Boss — The Screaming Flight (Zone 6)

## Summary

Zone 6 capstone boss (floor 60). Paired species: [Bat](../species/bat.md) (swarm-fused variant). A bat-swarm that has fused at the edges over decades into a single many-winged, many-mouthed creature — no longer individuals, yet still a flight. Unique in the starter roster: **three-phase fight** (the only 3-phase boss in zones 1–8) with a ground-collapse transition on Phase 3.

## Current State

**Spec status: LOCKED** via SPEC-BOSS-SCREAMING-FLIGHT-01. Lifted from [boss-art.md §Zone 6 — The Screaming Flight (skeleton fill)](../boss-art.md) on 2026-04-18 and expanded.

The roadmap noted this boss "may need its own species sub-spec first" because it's not a single bat but a swarm-fused creature. Decision on 2026-04-18: use the base Bat species spec + document the fusion as a boss-only body plan in this file. No separate species spec needed — the fusion is unique to this encounter.

## Design

### 1. Identity

- **Fiction beat:** A bat-swarm that has fused at the edges over decades into a single many-winged, many-mouthed creature — no longer individuals, yet still a flight.
- **Zone + floor ruled:** Zone 6 capstone — floor 60.
- **What corrupts this floor:** A wind-tunnel floor with updrafts strong enough that magicule-fog never settles; everything here breathes it continuously.
- **Primary reaction:** `close-the-gap`.

### 2. Stats

Base species Bat at floor 60 (interpolated): HP ≈ 400, Dmg ≈ 38, Speed ≈ 110, XP ≈ 140. Boss multiplier **×8 / ×3 / ×0.8 / ×10** (close-the-gap band).

| Stat | Value |
|---|---|
| Base HP | **3200** |
| Base Contact Damage | **114** |
| Base Move Speed | **88 px/s** |
| Base XP Yield | **1400** |

**Speed override note:** 88 px/s is higher than other bosses but below species baseline of 110 — the fusion-boss is heavier than an individual bat but not as slow as a grounded boss. Acceptable override of the 0.8× base-speed rule; documented.

Target TTK: **100–130 s**.

### 3. AI Pattern

- **Primary (Phase 1):** `ranged-kite` inverted — flies above the player and dives periodically. Player must close to hit; the boss evades by flying higher rather than retreating laterally.
- **Telegraph:** 450 ms dive wind-up with a visible tilt.
- **Phase 2 trigger: 50% HP.** Starts detaching smaller bat-fragments (1-HP adds) that swarm the player; aura shifts pale-grey → screeching-yellow. `FlashFx.Flash(Color.White, 120ms)`.
- **Phase 3 trigger: 25% HP.** Boss collapses into a ground-level thrashing mass — switches AI pattern to `melee-chase` with fast sweeping attacks; aura shifts to raw white-hot. **Art requirement:** a ground-collapse frame must be delivered at the Phase 3 transition (z-offset drops from +40 to 0).
- **Aggro range:** arena-bound.
- **Leash:** regen 5%/s outside arena.

### 4. Drop-Table Hook

- **First-kill bundle:** (floor-50 row baseline, scaled) 5× Tier 4 generics, 3× Echo Shard, + 1× Chest.
- **First-kill unique:** **FORGE-01 Tier 5 pool**.
- **Save-flag:** `floor60_boss_bat`.
- **Repeat-kill:** standard Bat drops.

### 5. Silhouette Readability Constraint

**Must read as "a cloud that has teeth" at 8 tiles.** Cues: scale 2.0×; many independent wing-pairs radiating outward (additional body part); audible-feel screeching-yellow shimmer around the wing-fringe (aura).

### 6. Size / Scale Rule

- **Multiplier:** 2.0×
- **Hitbox radius:** 24 px
- **Z-offset:** **+40 px** (airborne Phase 1 + 2); **drops to 0 on Phase 3 transition** — art must deliver a ground-collapse frame at the threshold.
- **Canvas:** 160 × 160

### 7. Color-Coding Contract

- **Exempt pixels:** many-eye cluster at the core (8+ pixels), wing-fringe shimmer (sparse pixels along every wing edge).
- **Heightened palette:** shadows 2 steps darker; Bat species signature leathery-brown 1 step darker on the core; many-eye cluster carries a new saturated yellow.
- **Aura:** wing-fringe yellow shimmer.
- **Phase colors:** Phase 1 pale grey; Phase 2 screeching yellow; Phase 3 raw white-hot.

### 8. Art-Spec Pairing

- Paired art ticket: **ART-SPEC-BOSS-01** — special note: this boss needs a Phase-3 ground-collapse frame in addition to the standard boss sprite set.
- Pairing status: `[ ] art spec locked  [ ] art assets delivered`.

---

## Acceptance Criteria

- [ ] 450 ms dive wind-up telegraph is visible and dodgeable in Phase 1.
- [ ] Phase 2 spawns bat-fragment adds at a sustainable rate (not swarm-overwhelming, not trivial — balance target ~3-4 adds alive at any time).
- [ ] Phase 3 AI switch from `ranged-kite` to `melee-chase` fires exactly once at 25% HP.
- [ ] Phase 3 ground-collapse animation plays before AI switches (z-offset lerp from +40 to 0 over ~600 ms).
- [ ] FlashFx.Flash fires on both Phase 2 AND Phase 3 threshold crossings.
- [ ] First-kill bundle + FORGE-01 Tier 5 unique drop once per save slot; `floor60_boss_bat` gate works.
- [ ] Silhouette at 8 tiles reads as a cloud-with-teeth, not a large single bat.

## Implementation Notes

- **Three-phase boss:** this is the first and only starter-roster boss with Phase 3. Phase-shift trigger needs TWO one-shot flags (`_phase2Entered`, `_phase3Entered`) to prevent re-triggering on HP bounce.
- **Bat-fragment adds (Phase 2):** lightweight visual + 1-HP contact-damage entities. Spawn from the boss body at a capped rate (e.g., one every 800 ms, cap 4 alive). Despawn on boss defeat.
- **Z-offset lerp (Phase 3):** tween `ZOffset` from 40 to 0 over 600 ms at Phase 3 transition; during the lerp, disable AI actions so the visual reads clean. Resume `melee-chase` when ZOffset hits 0.
- **Arena regen:** applies at all phases; regen rate is constant 5%/s while player outside arena, even mid-phase.
- **Species spec decision:** no standalone "bat-swarm-fused" species spec needed. The fusion is boss-only; base Bat body plan still feeds this encounter. If the Phase 3 ground-thrashing mass ever becomes a reusable pattern, open a spec then.

## Open Questions

None — spec is locked.
