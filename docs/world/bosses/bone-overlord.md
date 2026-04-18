# Boss — Bone Overlord (Zone 1)

## Summary

Zone 1 capstone boss (floor 10). Paired species: [Skeleton](../species/skeleton.md). A skeleton that soaked bone-dust from a pre-dungeon mass grave buried one stratum below until its rib cage became a furnace of magicule-saturated calcium. The player's first "this is a real boss" moment — short fight (25–35 s), two phases, burst-down-fast.

## Current State

**Spec status: LOCKED** via SPEC-BOSS-BONE-OVERLORD-01. Lifted from [boss-art.md §Zone 1 — Bone Overlord (fully worked example)](../boss-art.md) on 2026-04-18 and expanded with acceptance criteria, impl notes, and FlashFx hook specifics. Paired art: `ART-SPEC-BOSS-01` ([boss-pipeline.md](../../assets/boss-pipeline.md)).

## Design

### 1. Identity

- **Fiction beat:** A skeleton that has sat at the bottom of the first zone since before the frontier-settlement expedition arrived, soaking bone-dust from a mass grave in the floor below until its rib cage became a furnace of magicule-saturated calcium.
- **Zone + floor ruled:** Zone 1 capstone — floor 10.
- **What corrupts this floor:** A hairline crack in the arena floor vents bone-dust up from a pre-dungeon mass grave one stratum deeper; the Overlord has inhaled it for every year it has stood here.
- **Primary reaction:** `burst-down-fast`.

### 2. Stats

Base species Skeleton at floor 10 (standard-tier interpolated from [skeleton.md §Stats](../species/skeleton.md)): HP ≈ 60, Contact Dmg ≈ 8, Move Speed ≈ 60 px/s, XP ≈ 35. Boss multiplier: **×5 HP / ×2 damage / ×0.8 speed / ×10 XP** (burst-down-fast band from [boss-art.md §2 Stats](../boss-art.md)).

| Stat | Value |
|---|---|
| Base HP | **300** (60 × 5) |
| Base Contact Damage | **16** (8 × 2) |
| Base Move Speed | **48 px/s** (60 × 0.8) |
| Base XP Yield | **350** (35 × 10) |

Target TTK (class-appropriate melee at level 10): **25–35 seconds.** Fight fits in ~1 minute; player learns the phase-2 tell and finishes.

### 3. AI Pattern

- **Primary pattern:** `melee-chase` with heavy windup (lumbers, does not rush).
- **Telegraph (contact):** N/A — 700 ms hit cooldown per [monsters.md](../monsters.md).
- **Phase 2 trigger: 50% HP.** Plants feet and channels a **900 ms ground-slam telegraph**; ground cracks visible around the boss; AOE damage in a 3-tile radius on release. Visual cue: aura shifts dim purple → bright red; `FlashFx.Flash(Color.White, 120ms)` on threshold crossing.
- **Phase 3:** N/A — 2-phase boss (burst-down-fast tier).
- **Aggro range:** arena-bound (arena radius defined in paired ART-SPEC-BOSS-01).
- **Leash range:** arena-bound; if player leaves the arena, Overlord regenerates HP at 5%/s until full.

### 4. Drop-Table Hook

- **First-kill bundle** (from [monster-drops.md](../../systems/monster-drops.md) floor-10 row): 3× Tier 2 generics, 1× Bone Dust, + 1× Crate bonus container at the boss tile.
- **First-kill guaranteed unique:** 1× unique rolled uniformly from the **FORGE-01 Tier 1 pool** (3 uniques — see [blacksmith-forge-rng.md §10 Tier 1 roster](../../systems/blacksmith-forge-rng.md)). Drops pre-forged.
- **Save-flag key:** `floor10_boss_skeleton`.
- **Repeat-kill:** standard Skeleton species drops (Bone Dust signature 10%, Bone thematic generic 25%). No bundle, no unique.
- **Every-kill standard species drops:** yes, both channels — independent of first-kill bundle.

### 5. Silhouette Readability Constraint

**The Bone Overlord must read as "a skeleton that has *become an altar*" at 8 tiles away** — not merely a large skeleton. Rationale: the zone-1 boss is the player's first "this is a real boss" moment; if it reads as a taller skeleton, the burst-down-fast fantasy collapses into "another mob."

**Bigger-and-badder cues (three used):** scale 1.8×, left arm ends in a fused bone-club larger than its own torso (asymmetry), persistent bone-dust emission rising from the rib cage (aura — tiny particles, slow upward drift).

Distinguishable from: standard Skeleton (symmetric, no aura, no club), and every other starter-roster boss (only boss with bone-dust emission + club asymmetry).

### 6. Size / Scale Rule

- **Multiplier:** 1.8× (Boss band)
- **Hitbox radius:** 22 px (round(12 × 1.8))
- **Z-offset:** 0 (grounded)
- **Canvas size:** 160 × 160

### 7. Color-Coding Contract

- **Base tint surface:** body only; features exempt.
- **Exempt pixels:** rib-cage furnace-glow core (3×3 cluster at chest-center — the bone-dust emission source), eye sockets (2 pixels each), club's bone-dust shimmer (sparse pixels on the club head).
- **Heightened palette:** shadows 2 steps darker than standard Skeleton palette; skeleton-species signature purple joint-glow 2 steps brighter AND appears on 4× as many joints (every rib, both shoulders, pelvis) vs standard Skeleton's single spine-cluster glow.
- **Aura:** slow-rising bone-dust particles from rib cage — pale off-white, ~2–3 particles/sec, drift upward ~40 px before fading. Compatible with `FlashFx.Flash()` — aura tint modulates via the same sprite-tint channel so the on-hit white flash overrides cleanly.
- **Phase color cues:** Phase 1 dim purple aura; Phase 2 aura shifts to bright red on the 50% HP threshold pulse.

### 8. Art-Spec Pairing

- Paired art ticket: **ART-SPEC-BOSS-01** ([boss-pipeline.md](../../assets/boss-pipeline.md)).
- Pairing status: `[ ] art spec locked  [ ] art assets delivered`.

---

## Acceptance Criteria

- [ ] Engineer can implement the Bone Overlord from this spec + [skeleton.md](../species/skeleton.md) + the paired ART-SPEC-BOSS-01 without a clarifying question.
- [ ] Phase 2 trigger fires exactly once on crossing 50% HP (not re-triggerable if HP bounces above/below via healing/damage).
- [ ] 900 ms ground-slam telegraph is visible and dodgeable on first watch; AOE radius 3 tiles matches visual crack pattern.
- [ ] FlashFx.Flash fires on Phase 2 threshold with White color, 120ms duration.
- [ ] First-kill bundle drops exactly once per save slot; save-flag `floor10_boss_skeleton` prevents re-drop.
- [ ] Repeat-kill drops match standard Skeleton (no bundle, no unique).
- [ ] Leash regen at 5%/s while player outside arena; resets when player re-enters.
- [ ] Silhouette test: at 8 tiles and 1× zoom, the Bone Overlord is distinguishable from standard Skeletons and from every other starter-roster boss.

## Implementation Notes

- **Phase-shift trigger:** gate on `hp_previous > 0.5 * max_hp && hp_current <= 0.5 * max_hp`. Use a boolean `_phase2Entered` flag to make the transition one-shot.
- **FlashFx hook:** call `FlashFx.Flash(Color.White, 120)` on phase transition before switching AI state; the aura tint swap (purple → red) happens as a separate sprite-modulate animation over ~400 ms after the flash.
- **Save-flag:** `SaveData.FirstKillFlags["floor10_boss_skeleton"] = true` on first defeat; gate the bundle + unique drop on `!FirstKillFlags["floor10_boss_skeleton"]` at spawn time (not kill time, to let the drop table be precomputed).
- **Arena-bound logic:** check if player's current tile is inside the floor-10 arena rect (defined in ART-SPEC-BOSS-01); if outside, enable regen timer; if inside, disable.
- **Existing C# hooks:** `EnemySpecies.Skeleton` enum exists; `SpeciesDatabase` hitbox needs override to 22 px for boss; `MonsterDropTable.RollMaterials` handles base-species drops, but the first-kill bundle bypasses it (hardcoded drop list tied to save-flag).
- **Aura particle FX:** new particle emitter attached to the boss sprite's rib-cage anchor point; ~2–3 particles/sec, upward drift 40 px, fade out. Can disable during Phase 2 or let it persist — designer's call at polish time.

## Open Questions

None — spec is locked.
