# Boss — Iron-Gut Goblin King (Zone 7)

## Summary

Zone 7 capstone boss (floor 70). Paired species: [Goblin](../species/goblin.md). A goblin that has eaten everything — every weapon, every armor scrap, every carcass, every other goblin. Its stomach is a magicule-forge, and its skin has re-grown around the iron it has swallowed. **Three-phase fight** with a Phase-3 turret-mode transformation.

## Current State

**Spec status: LOCKED** via SPEC-BOSS-IRON-GUT-GOBLIN-KING-01. Lifted from [boss-art.md §Zone 7 — Iron-Gut Goblin King (skeleton fill)](../boss-art.md) on 2026-04-18 and expanded.

## Design

### 1. Identity

- **Fiction beat:** A goblin that has eaten everything. Every weapon, every armor scrap, every carcass, every other goblin. Its stomach is a magicule-forge, and its skin has re-grown around the iron it has swallowed.
- **Zone + floor ruled:** Zone 7 capstone — floor 70.
- **What corrupts this floor:** The zone's magicule current pools into a specific tile the King has never left; he eats the floor itself.
- **Primary reaction:** `close-the-gap`.

### 2. Stats

Base species Goblin at floor 70 (interpolated): HP ≈ 480, Dmg ≈ 42, Speed ≈ 60, XP ≈ 170. Boss multiplier **×8 / ×3 / ×0.8 / ×10**.

| Stat | Value |
|---|---|
| Base HP | **3840** |
| Base Contact Damage | **126** |
| Base Move Speed | **48 px/s** |
| Base XP Yield | **1700** |

Target TTK: **110–150 s**.

### 3. AI Pattern

- **Primary (Phase 1):** `melee-chase` — slow lumber with heavy sweeps.
- **Telegraph:** 700 ms belly-slam wind-up.
- **Phase 2 trigger: 50% HP.** Vomits a pool of iron-slag that persists as a damage-over-time zone for 4 s on a 1000 ms telegraph; aura shifts dim-iron-grey → molten-orange. `FlashFx.Flash(Color.White, 120ms)`.
- **Phase 3 trigger: 25% HP.** King splits its stomach open and becomes a turret — stationary, firing projectiles on a 400 ms telegraph; aura raw-white-hot.
- **Aggro range:** arena-bound.
- **Leash:** regen 5%/s outside arena.

### 4. Drop-Table Hook

- **First-kill bundle:** 5× Tier 5 generics, 3× Goblin Tooth, **1× signature of every Zone 1–3 species** (the King has eaten all of them — fiction-into-mechanic), + 1× Chest.
- **First-kill unique:** **FORGE-01 Tier 5 pool**.
- **Save-flag:** `floor70_boss_goblin`.
- **Repeat-kill:** standard Goblin drops.

### 5. Silhouette Readability Constraint

**Must read as "a goblin that has become a siege engine" at 8 tiles.** Cues: scale 2.0×; distended iron-scab belly that visibly bulges more than its torso (additional body part); crude iron-circlet melted into the skull (crown); iron-grey heat-shimmer from belly seams (aura).

### 6. Size / Scale Rule

- **Multiplier:** 2.0×
- **Hitbox radius:** 24 px
- **Z-offset:** 0
- **Canvas:** 160 × 160

### 7. Color-Coding Contract

- **Exempt pixels:** belly-seam iron-glow (thin bright horizontal line), circlet highlights (6 pixels), eye glow (2 pixels).
- **Heightened palette:** shadows 2 steps darker; Goblin species signature yellow-green 1 step sicklier; iron-slag accent is a new molten-orange.
- **Aura:** belly heat-shimmer.
- **Phase colors:** Phase 1 dim-iron-grey; Phase 2 molten-orange; Phase 3 raw white-hot.

### 8. Art-Spec Pairing

- Paired art ticket: **ART-SPEC-BOSS-01** — special note: Phase 3 turret-mode needs a distinct stationary-fire pose frame.
- Pairing status: `[ ] art spec locked  [ ] art assets delivered`.

---

## Acceptance Criteria

- [ ] 700 ms belly-slam telegraph is visible and dodgeable in Phase 1.
- [ ] Phase 2 iron-slag pool has a 1000 ms telegraph before it lands; DOT zone persists 4 s.
- [ ] Phase 3 turret-mode: boss becomes stationary; projectile telegraph is 400 ms.
- [ ] FlashFx.Flash fires on both Phase 2 AND Phase 3 threshold crossings.
- [ ] First-kill bundle drops Zone 1–3 species signatures (Bone Dust, Wolf Pelt, Chitin Fragment — per [item-catalog.md](../../inventory/item-catalog.md) zone-1-3 thematic generics) alongside Tier 5 generics.
- [ ] First-kill bundle + FORGE-01 Tier 5 unique drop once per save slot; `floor70_boss_goblin` gate works.
- [ ] Silhouette test at 8 tiles distinguishes King from standard Goblins + from other bosses.

## Implementation Notes

- **Three-phase boss:** two one-shot flags (`_phase2Entered`, `_phase3Entered`).
- **Iron-slag DOT:** ground overlay tile with a tick timer (e.g., 20 damage every 0.5 s for 4 s). Overlay visuals match aura color shift.
- **Phase 3 turret-mode:** set `MoveSpeed = 0` on Phase 3 transition; swap AI routine to a pure projectile firer with 400 ms telegraph. Projectile travels ~300 px/s, deals ~90% of contact damage per hit.
- **First-kill bundle "King has eaten all of them" mechanic:** hardcode the Zone 1–3 signature material drops into the bundle alongside the standard Tier 5 generics. Implementation: add a `BossFirstKillBundle` override list per boss that supplements the floor-based drop roll.

## Open Questions

None — spec is locked.
