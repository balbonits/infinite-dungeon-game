# Monster Species Authoring Template

## Summary

Canonical template for every monster-species spec in the game. This is a **meta-spec** — it does not define any single species. It defines the fill-in-the-blank structure every future species spec (e.g. `SPEC-SPECIES-BAT-01`, new species added in P2) must follow, so that adding or reworking a species is a disciplined authoring task, not a redesign-from-zero.

Paired with [ART-SPEC-02 — species sprite pipeline](../assets/species-pipeline.md), which defines the generation-side half. Neither half is a complete species spec on its own; both must be locked for a species ticket to move to "Ready-for-impl."

## Current State

Seven species ship today (Skeleton, Goblin, Bat, Wolf, Orc, Dark Mage, Spider) but were authored ad-hoc before this template existed. Their data lives across `scripts/logic/EnemySpecies.cs`, `scripts/logic/SpeciesDatabase.cs` (collision), `scripts/logic/MonsterDropTable.cs` (drops), `Constants.Zones` (zone gating), and art assets per-species. There is no single source-of-truth spec per species, which is why the upcoming ART-14 rework (Bat / Spider / Wolf) and any new species in P2 risk re-arguing settled design ground. This template fixes that.

Related specs this template pulls from:
- [docs/world/monsters.md](monsters.md) — tier/spawn/AI baseline
- [docs/systems/stats.md](../systems/stats.md) — player-side stat curves (used for target-TTK math)
- [docs/systems/monster-drops.md](../systems/monster-drops.md) — drop-table structure and signature-material rule
- [docs/systems/color-system.md](../systems/color-system.md) — level-relative tint system
- [docs/systems/iso-rendering.md](../systems/iso-rendering.md) — sprite anchor + footprint rules
- [docs/assets/species-pipeline.md](../assets/species-pipeline.md) — paired art half (ART-SPEC-02)

## Design

### Required Sections Every Species Spec Must Have

A species spec is a single markdown file under `docs/world/species/<species-id>.md` (directory to be created on the first species ticket). It must contain, **in this order**, the eight sections below. Any section with no content required is marked "optional, N/A acceptable" — but the heading must be present so diffs across species are uniform.

---

#### 1. Identity

Three sub-beats, each one to three sentences. No more.

- **Fiction beat (one sentence).** What is this creature in our world, in-lore. Must be consistent with [docs/world/monsters.md § Magicule-Warped Creatures](monsters.md). Template: "A {prior-form creature} warped by {magicule-exposure phrase} into {what it is now}."
- **Role in dungeon ecology.** Which of the two dungeon-body roles this species serves (immune-system / defense vs digestive / harvesting) per monsters.md. One sentence. If the species does both, pick the dominant role and say so.
- **Intended player emotional reaction.** One of the five canonical reactions, picked from this locked vocabulary so ability and AI design stay aligned with feel:
  - `cautious-approach` — player should slow down, check surroundings, commit carefully (tank/heavy)
  - `burst-down-fast` — glass-cannon target, high priority, dangerous if ignored (caster, elite ranged)
  - `kite-from-range` — player should create and hold distance (melee rusher, fast chaser)
  - `close-the-gap` — player should charge in, avoid sustained fire (ranged, turret-like)
  - `pack-management` — player must read the group, not the individual (swarm, pack units)

  A species must pick **exactly one** as its primary reaction. Secondary reactions are allowed but listed as "also" with a one-line reason. This choice drives AI pattern (§3) and silhouette constraint (§5) more than any other field.

---

#### 2. Stats

All values anchored to the same three sample-floor snapshots so species are balanced against each other numerically, not hand-wavy.

The three sample floors:
- **Early** — floor 3 (zone 1, early exposure)
- **Mid** — floor 28 (zone 3, mid-game)
- **Deep** — floor 75 (zone 8, late P2)

For each sample floor, the spec must state:

| Field | Units | Notes |
|---|---|---|
| Base HP | int | Before tier multiplier (Fodder/Standard/Elite/Boss per monsters.md § Enemy Tiers P2). Standard-tier number. |
| Base Contact Damage | int | Standard-tier number. Does not include Dungeon Intelligence modifier. |
| Base Move Speed | int px/s | Standard-tier number. |
| Base XP Yield | int | Standard-tier number, before color-system level-gap modifier from [color-system.md § XP Bonus Scaling](../systems/color-system.md#xp-bonus-scaling). |

**Target time-to-kill (TTK) for a class-appropriate player at floor:** one sentence per floor stating expected hits-to-kill against an average-built melee attacker at player level that roughly matches the floor. The intent is to pin ability designers and the drop-table author to a feel target, not to encode a formula. Rule of thumb derived from monsters.md § Player Attack vs. Enemies:

| Reaction | Floor-matched TTK target |
|---|---|
| cautious-approach | 5–8 hits (these are stall fights) |
| burst-down-fast | 1–2 hits (glass cannons die instantly if reached) |
| kite-from-range | 2–3 hits (dangerous if they catch you, quick if you stop them) |
| close-the-gap | 3–5 hits (solid bodies, but not tanks) |
| pack-management | 1–2 hits individually (volume is the threat, not the unit) |

If a species spec's §1 reaction and §2 TTK disagree, the spec is not lockable.

---

#### 3. AI Pattern

One of five canonical patterns. Reuse these names — do not invent new ones without a new ticket to update this template:

| Pattern | Behavior | Maps well to reaction |
|---|---|---|
| `melee-chase` | Straight-line pursuit + contact damage. The monsters.md baseline. | close-the-gap, pack-management |
| `ranged-kite` | Maintains preferred distance; fires projectiles; flees if player gets too close. | burst-down-fast, close-the-gap |
| `ambush` | Idle / hidden until LOS or proximity trigger; then aggressive burst. | burst-down-fast |
| `pack` | Coordinates with nearby same-species units (spread to flank, or cluster for cover). | pack-management |
| `caster` | Stationary or slow; charges spells with telegraph; interruptible. | burst-down-fast, cautious-approach |

Spec must state:
- Which pattern (one of the five).
- **Telegraph window** in ms for any attack that is not plain contact damage (e.g. "450 ms wind-up with a flash before ranged shot"). Contact-damage-only enemies state "N/A — contact damage, 700 ms hit cooldown per monsters.md."
- **Aggro range** in pixels, or `chase-always` for the current monsters.md default.
- **Leash range** in pixels, or `never-leash` for current default. A leash range becomes meaningful once boss / elite fights land — stubbed in now so retrofitting is mechanical.

No C# in this section. Reference the existing `scripts/logic/` AI behavior as prior art but keep the spec engine-agnostic.

---

#### 4. Drop-Table Hook

The species's contract with [docs/systems/monster-drops.md](../systems/monster-drops.md). Three fields:

| Field | Value |
|---|---|
| Signature material ID | The `item_id` from [item-catalog.md § Materials](../inventory/item-catalog.md). Unique per species. |
| Signature drop rate | 7–10% per kill, per monster-drops.md. |
| Thematic generic (Ore/Bone/Hide) | Drives the 60/20/20 thematic bias on the generic channel. |

**Plus one optional field:**
- **Special drop** — one line naming any boss-only, first-kill, or species-unique drop beyond the signature material. If none, write "None." Subject to monster-drops.md § Boss First-Kill Drops.

No equipment fields. Per SPEC-LOOT-01 equipment is container-only.

---

#### 5. Silhouette Readability Constraint

The game-mechanic-driven visual constraint. This section is what the art-lead reads to know what NOT to abstract away when composing a PixelLab prompt. It tells art which feature of the silhouette is load-bearing for gameplay.

Required format: one-sentence constraint + one-sentence rationale.

Examples by reaction (illustrative — each species writes its own):
- "Winged enemy must read as airborne from 8 tiles away — silhouette must show wingspan + lifted pose." Rationale: players need to distinguish flyers from grounded enemies at aggro-range so they can prep the right counter.
- "Fast enemy must have forward-leaning silhouette with visible motion lines or extended forelimb." Rationale: tells the player at a glance this is a closer, not a turret.
- "Caster must stand apart from the melee crowd — upright stance, distinct headpiece, hands visible and raised." Rationale: casters are the highest-priority target; the player must identify them across a group.
- "Tank must have a top-heavy silhouette wider than the player sprite's horizontal bounding box." Rationale: wider = reads as "commit carefully."
- "Pack unit must be individually simple and collectively legible — group of 5 must not read as one blob." Rationale: pack-management requires counting the group at a glance.

The constraint here binds §1 (reaction) and §3 (AI pattern) to the visual. It is the single hardest section to get right; spec reviews should prioritize it.

---

#### 6. Size / Scale Rule

Scale multiplier where player = 1.0. Drives both sprite scale and hitbox. Canonical bands:

| Band | Multiplier | Typical species |
|---|---|---|
| Small | 0.6–0.8× | Bat, Spider, small swarm units |
| Standard | 0.9–1.1× | Skeleton, Goblin |
| Large | 1.2–1.6× | Orc, Wolf (combat size), tanks |
| Boss | 1.7–2.5× | Floor bosses, unique elites |

Spec must state:
- Numeric multiplier (one value, two decimals).
- Hitbox radius (px) — must be consistent with `scripts/logic/SpeciesConfig.cs` convention (reference only, not prescriptive). Default = `round(12 * scale_multiplier)` unless explicitly overridden.
- Z-offset (px, for airborne species). `0` for ground. Bats and future flyers state an airborne offset (e.g. `+28`) so the iso Y-sort and occlusion shader treat them consistently per [iso-rendering.md](../systems/iso-rendering.md).

---

#### 7. Color-Coding Contract

How this species interacts with [color-system.md](../systems/color-system.md). The color system applies a **modulate tint** to the sprite derived from the level gap between player and monster; the tint is applied to the sprite node as a whole. But some pixels must remain unmodulated so the level-relative color does not wash out species identity.

Required fields:
- **Base tint surface** — "full sprite" (default) OR "body only, features exempt" (if the species has identifying glow / eyes / signature color).
- **Exempt pixels list** — enumerate any pixel clusters that must stay unmodulated (e.g. "eye glow," "staff head," "elemental core"). Empty list is acceptable and means full-sprite tint.
- **Exempt-pixel implementation note** — one line pointing art to the technique (separate sprite layer with `modulate=Color.White`, or shader carve-out). This flags the constraint for ART-SPEC-02 but does not prescribe the technique — that's art-lead's half.

Rule: if a species is defined by a color (Dark Mage's purple glow, a fire-element mob's red core), that color must be on the exempt list. Otherwise the level-relative tint will erase the species's visual identity at certain level gaps.

---

#### 8. Art-Spec Pairing

One line identifying the paired `ART-SPEC-*` or `ART-*` ticket that delivers the sprites, plus a lock-state stamp:

```
Paired art ticket: ART-14 (Bat re-art batch)
Pairing status: [ ] art spec locked  [ ] art assets delivered
```

A species spec reaches **"Ready-for-impl"** only when both the art-pairing checkboxes are ticked and the design spec has no Open Questions. If the art half is in a different ticket than the design half, both tickets must be referenced.

---

### Section-by-Section Acceptance Matrix

Every species spec must pass this checklist to lock:

| # | Section | Lock condition |
|---|---|---|
| 1 | Identity | All three sub-beats filled; reaction is one of the five canonical values |
| 2 | Stats | Values at three sample floors (3 / 28 / 75); TTK target stated for each; §1-reaction ↔ §2-TTK consistency verified |
| 3 | AI Pattern | Pattern is one of the five canonical values; telegraph, aggro, leash fields filled |
| 4 | Drop-Table Hook | Signature material ID, rate (7–10%), thematic generic picked; special-drop field present (may say "None") |
| 5 | Silhouette Readability Constraint | Exactly one binding sentence + one rationale sentence |
| 6 | Size / Scale Rule | Multiplier, hitbox radius, z-offset all present |
| 7 | Color-Coding Contract | Base surface stated; exempt list present (may be empty) |
| 8 | Art-Spec Pairing | Paired ticket cited; lock-state stamp present |

### Standard Spec Wrapping

A species spec using this template still wraps the eight sections above with the standard spec-doc skeleton:

```
# Species — {Name}

## Summary
## Current State
## Design
  ### 1. Identity
  ### 2. Stats
  ### 3. AI Pattern
  ### 4. Drop-Table Hook
  ### 5. Silhouette Readability Constraint
  ### 6. Size / Scale Rule
  ### 7. Color-Coding Contract
  ### 8. Art-Spec Pairing
## Acceptance Criteria
## Implementation Notes
## Open Questions
```

---

## Worked Example — Species Spec for Bat (illustrative, ticket: `SPEC-SPECIES-BAT-01`)

The example below shows what a filled template looks like. This content lives here as illustration only; when `SPEC-SPECIES-BAT-01` is written as a real ticket, it will be lifted out into `docs/world/species/bat.md`.

---

**Species — Bat**

### 1. Identity
- **Fiction beat:** An ordinary cave bat whose flight muscles have been gorged on raw magicules until they power a body that can no longer land comfortably.
- **Role in dungeon ecology:** Immune system — Bats harass intruders the moment they cross into the dungeon's upper strata, drawing first blood so slower predators can close.
- **Intended player emotional reaction:** `kite-from-range` (the player must stop them before they close; fast and erratic, not dangerous to trade hits with once cornered).

### 2. Stats

| Field | Floor 3 (Early) | Floor 28 (Mid) | Floor 75 (Deep) |
|---|---|---|---|
| Base HP | 22 | 120 | 540 |
| Base Contact Damage | 4 | 14 | 38 |
| Base Move Speed (px/s) | 90 | 105 | 120 |
| Base XP Yield | 12 | 48 | 180 |

Target TTK (class-appropriate melee, level matching floor): **2–3 hits** every floor — Bats are frail by design; the feel is "swat them out of the air fast or eat the dive damage."

### 3. AI Pattern
- Pattern: **melee-chase** with a swooping motion curve (sinusoidal Y on approach to sell the airborne feel; functionally still straight-line pursuit).
- Telegraph: N/A — contact damage, 700 ms hit cooldown per monsters.md.
- Aggro range: `chase-always` (current engine default).
- Leash range: `never-leash` (current engine default).

### 4. Drop-Table Hook
- Signature material: **Echo Shard** (`mat_echo_shard`), 8% per kill.
- Thematic generic: **Hide** (60% hide, 20% ore, 20% bone on the generic channel — leathery wings).
- Special drop: None.

### 5. Silhouette Readability Constraint
**Must read as airborne from 8 tiles away — silhouette must show spread wings and a lifted (off-ground) pose, not a grounded biped.** Rationale: distinguishing flyers from grounded enemies at aggro-range is what lets the player pre-empt a dive rather than trade damage on contact; the current bipedal placeholder fails this test, which is the core driver of ART-14.

### 6. Size / Scale Rule
- Multiplier: **0.70×**
- Hitbox radius: **8 px** (round(12 × 0.70) = 8)
- Z-offset: **+28 px** (airborne — must render above ground-level sprites in iso Y-sort)

### 7. Color-Coding Contract
- Base tint surface: **full sprite**.
- Exempt pixels: **eye highlights only** (two small bright pixels on the face) — keeps a readable gaze at any level gap.
- Exempt-pixel implementation note: separate sprite sub-node modulated `Color.White` on top of the tinted body.

### 8. Art-Spec Pairing
- Paired art ticket: **ART-14** (Bat / Spider / Wolf rework batch).
- Pairing status: `[ ] art spec locked  [ ] art assets delivered`.

---

## Acceptance Criteria

- [ ] An engineer implementing a new species can read this template plus one filled species spec and write the C# (`EnemySpecies` enum entry, `SpeciesDatabase` entry, `MonsterDropTable` entry, stat-floor scaling hook) without asking design-lead a clarifying question.
- [ ] Art-lead can read this template plus one filled species spec and author a PixelLab prompt (via ART-SPEC-02's prompt skeleton) without asking design-lead a clarifying question.
- [ ] All seven currently-shipping species (Skeleton, Goblin, Bat, Wolf, Orc, Dark Mage, Spider) can be retroactively described using this template with no contradictions (the existing `SpeciesDatabase` hitboxes, `MonsterDropTable` signature materials, and `Zones.GetZoneSpecies` assignments all map cleanly onto fields 2, 4, 6).
- [ ] The §1-reaction ↔ §2-TTK ↔ §3-AI-pattern consistency check is machine-verifiable from the spec text alone (a reviewer can confirm the three values are coherent without running the game).
- [ ] The eight-section Acceptance Matrix (above) is the review checklist for every species PR.

## Implementation Notes

- **Pairing with ART-SPEC-02 is load-bearing.** Neither this template nor the art-pipeline spec ([docs/assets/species-pipeline.md](../assets/species-pipeline.md)) locks without the other. A species spec authored against this template but with no corresponding ART-SPEC-02 prompt artifact is not shippable, and vice versa. Treat the two half-specs as a single logical unit — any change to one that breaks the other's assumptions (e.g. adding a `Large` size band without a matching PixelLab scale prompt) triggers an edit on both.
- **Species files live at `docs/world/species/<species-id>.md`.** The directory is created on the first species ticket. The current seven species should be back-filled opportunistically when each one is next touched (Bat first, via ART-14).
- **The five reactions and five AI patterns are a locked vocabulary.** Adding to either list requires a separate ticket against this template — do not silently invent a sixth value inside a species spec.
- **Stat sample floors (3 / 28 / 75) are deliberately spread across zones** (zone 1, zone 3, zone 8) to force authors to think about scaling across the full game, not just the zone where the species first appears.
- **Do not use this template to define multi-species content** (boss encounters, pack-leader + minion pairs). Those get their own encounter-level spec that references the underlying species specs.

## Open Questions

None — template is locked.
