# Species — Goblin

## Summary

Zone 2 ground-based pack unit — small, cowardly, numerous. Individually laughable; a room of five is the threat. First species spec to instantiate SPEC-SPECIES-01's `pack-management` reaction and the `pack` AI pattern. Feeds the zone 7 boss Iron-Gut Goblin King as its base body plan.

## Current State

**Spec status: LOCKED** — authored 2026-04-18 per SPEC-SPECIES-GOBLIN-01 (Phase E). Goblin is a shipping species: `EnemySpecies.Goblin` enum value exists, `SpeciesDatabase` hitbox entry exists, `MonsterDropTable` entry exists at [`scripts/logic/MonsterDropTable.cs:43`](../../../scripts/logic/MonsterDropTable.cs). This spec documents and locks what those entries mean in design terms — no new C# required.

Paired with [species-template.md](../species-template.md) (structural contract) and [species-pipeline.md](../../assets/species-pipeline.md) (art pipeline ART-SPEC-02). Art ticket ART-GOBLIN is proposed as a placeholder (not yet authored) — the current Goblin sprite predates the pack-management silhouette constraint in §5 and will need a redraw pass against it.

Zone appearance per `Constants.Zones`: primary spawn in zone 2 (Tier 1 pack unit alongside Wolf Tier 2); reappears at zone 7 where the boss Iron-Gut Goblin King lives. The zone 7 return is lore-coded as "goblins have colonized the Iron-Gut's warren en masse" — expect higher per-room goblin counts there. Boss behavior is not expanded here; see SPEC-BOSS-IRON-GUT-GOBLIN-KING-01 (Phase F).

## Design

### 1. Identity

- **Fiction beat:** Small cave-scavengers whose squabbling tribes were drawn into the dungeon's upper warrens by the smell of leaked magicules and have since bred far past their old population ceiling.
- **Role in dungeon ecology:** Immune system — goblins swarm intruders in loose packs, slowing the player's advance with volume and flanking angles so the dungeon's deeper predators have time to wake.
- **Intended player emotional reaction:** `pack-management` (the player must read the group, not the individual — a single goblin is trivial, five goblins flanking is a positioning problem).

### 2. Stats

| Field | Floor 3 (Early) | Floor 28 (Mid) | Floor 75 (Deep) |
|---|---|---|---|
| Base HP | 20 | 112 | 505 |
| Base Contact Damage | 3 | 11 | 32 |
| Base Move Speed (px/s) | 80 | 92 | 104 |
| Base XP Yield | 10 | 42 | 160 |

Target TTK (class-appropriate melee, level matching floor): **1–2 hits** every floor — goblins are frail by design; the feel is "delete each one in a single swing, but there are always more incoming from the flank."

**Comparison to Bat (same tier, same TTK target).** Both species are Tier 1 pack-adjacent fodder that should die in one or two hits, but they threaten the player differently, which their numbers reflect:

| Dimension | Bat | Goblin |
|---|---|---|
| Threat model | Airborne harasser — erratic approach vector, dives onto the player | Ground-based flanker — spreads around the player, attacks from sides |
| HP | slightly higher (22/120/540) | slightly lower (20/112/505) |
| Contact damage | slightly higher (4/14/38) — the dive stings | slightly lower (3/11/32) — each goblin chips, volume aggregates |
| Speed | faster (90/105/120) — the kiting pressure | slower (80/92/104) — the positioning pressure |
| Numbers per spawn | singles or pairs (airborne) | 3–5 per cluster (ground pack) |

The per-unit frailty is the same feel. The difference is in how the encounter asks the player to stand: Bat asks "swat it out of the air before it reaches you"; Goblin asks "pick the right arc of attack so the pack doesn't wrap you."

### 3. AI Pattern

- Pattern: **pack** — goblins coordinate with nearby same-species units. Target behavior: when 3+ goblins are within a short radius of the player, they spread along an arc (not a line) to attack from multiple angles; when outnumbered or isolated (only 1–2 within range), they cluster toward the nearest sibling goblin before committing to the player, giving a visible "regrouping" beat that rewards the player for breaking the pack apart.
- Telegraph: N/A — contact damage, 700 ms hit cooldown per [monsters.md](../monsters.md).
- Aggro range: `chase-always` (current engine default) for individual pursuit; the flank-spread / cluster behavior layers on top of chase rather than replacing it.
- Leash range: `never-leash` (current engine default).

Prior art only — the `pack` pattern is not yet implemented in `scripts/logic/`. Impl ticket (not this spec) will decide whether the spread-arc is a nearest-neighbor steering offset or a per-pack coordinator. Either is acceptable as long as §5's silhouette constraint survives the result.

### 4. Drop-Table Hook

- Signature material: **Goblin Tooth** (`material_sig_goblin`), **10% per kill**.
- Thematic generic: **Ore** (60% ore, 20% bone, 20% hide on the generic channel — goblins arm themselves with scavenged scrap-metal weapons and iron-banded clubs; their generic drops represent what the player harvests from the pile after the fight).
- Special drop: None at the species level. (The zone 7 boss Iron-Gut Goblin King gets its own first-kill drop in Phase F — not this spec.)

Matches locked entry in `scripts/logic/MonsterDropTable.cs:43`: `Goblin → MonsterTier.One, "material_sig_goblin", 0.10f, MaterialType.Ore`.

### 5. Silhouette Readability Constraint

**Pack unit must be individually simple and collectively legible — a cluster of five goblins must read as five distinct bodies at 8 tiles away, never as one blob.** Rationale: `pack-management` as a reaction only works if the player can count the group and pick a target at a glance; any silhouette detail that makes two adjacent goblins read as a single shape (wide capes, overlapping weapons held high, thick horizontal belts that cross between bodies) defeats the entire encounter. Keep silhouettes narrow and vertically compact, with arms held close to the body so two goblins standing shoulder-to-shoulder still read as two.

### 6. Size / Scale Rule

- Multiplier: **0.80×** (Small band per template — goblins are smaller than humanoid PCs / NPCs by fantasy convention, but larger than Bats so the ground-pack crowd still reads as threatening when it surrounds the player).
- Hitbox radius: **10 px** (round(12 × 0.80) = 10).
- Z-offset: **0 px** (ground-based — no airborne adjustment).

### 7. Color-Coding Contract

- Base tint surface: **body only, features exempt** — goblins are a green-skinned species by fantasy convention and the green must survive the level-relative modulate; a deep-floor goblin tinted full-red would lose its species identity.
- Exempt pixels: **skin (face + hands + any exposed limb) kept at canonical goblin-green**, and **eye highlights** (two small bright pixels) kept unmodulated for gaze readability. Scrap-metal weapons and clothing are NOT exempt — they tint with the rest of the body so the level-relative danger signal still lands on the majority of the silhouette.
- Exempt-pixel implementation note: separate sprite sub-layer for skin + eyes modulated `Color.White` on top of the tinted body-and-gear base. Art pipeline should produce the skin pass as its own PNG atlas layer so the shader / sub-node can composite them at runtime per [color-system.md](../../systems/color-system.md).

### 8. Art-Spec Pairing

- Paired art ticket: **ART-GOBLIN** (placeholder name — not yet authored as a dev-tracker entry; propose on next spec pass that this is created to cover the pack-management silhouette-constraint redraw).
- Pairing status: `[ ] art spec locked  [ ] art assets delivered`.

---

## Acceptance Criteria

- [ ] Engineer can implement the Goblin's stats and `pack` AI from this spec + `species-template.md` + existing `EnemySpecies.cs` / `SpeciesDatabase.cs` / `MonsterDropTable.cs:43` conventions without a clarifying question.
- [ ] Art-lead can compose the PixelLab prompt from this spec + ART-SPEC-02's prompt skeleton (CHAR-MON-ISO `.biped-mon` sub-variant) without a clarifying question.
- [ ] §1 reaction (`pack-management`) matches §2 TTK (1–2 hits) matches §3 AI (`pack`).
- [ ] §5 silhouette constraint is what ART-GOBLIN will test against at deliverable review: a 5-goblin cluster screenshot must show five distinct bodies at 64×64 thumbnail.
- [ ] §4 drop-table fields match `MonsterDropTable.cs:43` byte-for-byte (species enum, tier, signature id, signature rate, thematic generic).

## Implementation Notes

- **Existing C# hooks:** `EnemySpecies.Goblin` enum value exists; `SpeciesDatabase` hitbox entry exists; `MonsterDropTable` entry exists. No new enum or database additions needed — this spec documents what is already there plus the `pack` AI which is not yet implemented.
- **The `pack` AI pattern is net-new.** `monsters.md` baseline and all current species use `melee-chase`. Adding `pack` requires either a per-enemy steering offset read from nearest-N siblings (simpler, cheaper) or a pack-coordinator node (more flexible, costlier). Either is acceptable — the impl ticket picks based on cost. The design constraint is only that both the **spread-when-outnumbering** and **cluster-when-outnumbered** beats must be observable to a player watching the pack from across the room.
- **Spawn grouping.** Current `Dungeon.cs` spawn logic spawns enemies individually. To get the 3–5-per-cluster feel §2 implies, the goblin spawn path should either (a) pick a spawn tile and place 3–5 goblins in adjacent tiles in one call, or (b) keep individual spawns but bias goblin spawn-tile selection toward existing goblin positions. This is an impl decision, not a design one — either produces the right feel if tuned.
- **Zone 7 re-appearance.** Per `Constants.Zones`, goblins spawn in both zone 2 (primary) and zone 7 (boss-flavor). Per-room counts in zone 7 should lean toward the high end of the cluster range (4–5) to lore-code "the Goblin King's warren." No stat change between zones — same base numbers, scaled by floor via the Phase B density curve.
- **Iron-Gut Goblin King base.** The zone 7 boss inherits this species's body plan + stat baseline and adds its own rules (HP ×5–8, damage ×2–3, phase-shift at 50%/25%, unique silhouette cue per SPEC-BOSS-ART-01). Do not expand this spec to cover boss behavior — that is SPEC-BOSS-IRON-GUT-GOBLIN-KING-01 (Phase F).
- **Color contract sanity check.** If a future color-system revision changes the exempt-pixel technique (e.g., shader carve-out instead of sub-layer), re-verify that goblin skin-green still survives at both extremes of level gap (very-easy grey tint and very-hard red tint). The skin color is the species's single most identifying feature at thumbnail scale.

## Open Questions

None — spec is locked.
