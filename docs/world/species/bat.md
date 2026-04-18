# Species — Bat

## Summary

Airborne harasser species native to the dungeon's upper strata. Fast, frail, and defined by the need to read "airborne" from silhouette at aggro-range so players can prep the right counter rather than trade hits on contact. First-locked species spec via SPEC-SPECIES-BAT-01 (lifted from the worked example in [species-template.md](../species-template.md)).

## Current State

**Spec status: LOCKED** — lifted from the `species-template.md` worked example on 2026-04-18 per SPEC-SPECIES-BAT-01 (Phase E). Paired art ticket ART-14 covers the re-art batch that fixes the "reads as grounded biped" placeholder problem flagged in §5 Silhouette Readability Constraint.

Paired with [species-template.md](../species-template.md) (structural contract) and [species-pipeline.md](../../assets/species-pipeline.md) (art pipeline ART-SPEC-02). Neither half ships without the other.

## Design

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
- Telegraph: N/A — contact damage, 700 ms hit cooldown per [monsters.md](../monsters.md).
- Aggro range: `chase-always` (current engine default).
- Leash range: `never-leash` (current engine default).

### 4. Drop-Table Hook

- Signature material: **Echo Shard** (`mat_echo_shard` — placeholder id; see [item-catalog.md § Materials](../../inventory/item-catalog.md)), 8% per kill.
- Thematic generic: **Hide** (60% hide, 20% ore, 20% bone on the generic channel — leathery wings).
- Special drop: None.

Matches locked entry in `scripts/logic/MonsterDropTable.cs`: `Bat → MonsterTier.One, material_sig_bat, 0.08f, MaterialType.Hide`.

### 5. Silhouette Readability Constraint

**Must read as airborne from 8 tiles away — silhouette must show spread wings and a lifted (off-ground) pose, not a grounded biped.** Rationale: distinguishing flyers from grounded enemies at aggro-range is what lets the player pre-empt a dive rather than trade damage on contact; the current bipedal placeholder fails this test, which is the core driver of ART-14.

### 6. Size / Scale Rule

- Multiplier: **0.70×** (Small band per template)
- Hitbox radius: **8 px** (round(12 × 0.70) = 8)
- Z-offset: **+28 px** (airborne — must render above ground-level sprites in iso Y-sort per [iso-rendering.md](../../systems/iso-rendering.md))

### 7. Color-Coding Contract

- Base tint surface: **full sprite**.
- Exempt pixels: **eye highlights only** (two small bright pixels on the face) — keeps a readable gaze at any level gap.
- Exempt-pixel implementation note: separate sprite sub-node modulated `Color.White` on top of the tinted body.

### 8. Art-Spec Pairing

- Paired art ticket: **ART-14** (Bat / Spider / Wolf rework batch).
- Pairing status: `[ ] art spec locked  [ ] art assets delivered`.

---

## Acceptance Criteria

- [ ] Engineer can implement the Bat from this spec + `species-template.md` + `EnemySpecies.cs` conventions without a clarifying question.
- [ ] Art-lead can compose the PixelLab prompt from this spec + ART-SPEC-02's prompt skeleton without a clarifying question.
- [ ] §1 reaction (`kite-from-range`) matches §2 TTK (2–3 hits) matches §3 AI (melee-chase with swoop).
- [ ] §5 silhouette constraint is what ART-14 will test against at deliverable review.

## Implementation Notes

- **Existing C# hooks:** `EnemySpecies.Bat` enum value exists; `SpeciesDatabase` hitbox entry exists; `MonsterDropTable` entry exists. No new enum or database additions needed — the spec just documents what's already there.
- **The z-offset +28 px** is the load-bearing iso-rendering number. It must match the shader/Y-sort threshold in [iso-rendering.md](../../systems/iso-rendering.md) for airborne species; any tuning of that threshold requires a revisit here.
- **Zone appearance:** Bats spawn in zone 1 (Tier 1) and reappear at zone 6 as part of the "Screaming Flight" swarm-fused boss encounter. The swarm boss is a separate encounter-level spec, not a species variant — it cites this spec as its base body plan but adds its own rules. Do not expand this spec to cover boss behavior.

## Open Questions

None — spec is locked.
