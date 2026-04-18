# Species — Orc

## Summary

Mid-late dungeon brute species — a slow, over-muscled melee tank whose presence forces the player to stop kiting and actually commit to a fight. Orcs are the first species in the zone sequence where "just walk backwards and shoot" stops working: they hit hard enough that you can't ignore them, move slow enough that you can read their swings, and soak enough damage that you have to pick your moment. Authored via SPEC-SPECIES-ORC-01 (Phase E). Base body plan for two future bosses: the Warlord of the Fifth (zone 5) and the Volcano Tyrant (zone 8).

## Current State

**Spec status: LOCKED** — authored 2026-04-18 per SPEC-SPECIES-ORC-01 (Phase E). The C# hooks already exist: `EnemySpecies.Orc` enum entry, `SpeciesDatabase` hitbox entry, and a locked `MonsterDropTable` entry (`EnemySpecies.Orc, MonsterTier.Three, "material_sig_orc", 0.10f, MaterialType.Ore`) per [scripts/logic/MonsterDropTable.cs:46](../../../scripts/logic/MonsterDropTable.cs). This spec documents the design intent behind those existing hooks and pins the stat/silhouette/AI contract every downstream author (boss spec, art spec, combat tuner) inherits.

Paired with [species-template.md](../species-template.md) (structural contract) and [species-pipeline.md](../../assets/species-pipeline.md) (art pipeline ART-SPEC-02). Art ticket: ART-ORC proposed as placeholder — no existing ART-* ticket covers the Orc redraw, so ART-ORC is introduced here for tracker dispatch.

## Design

### 1. Identity

- **Fiction beat:** An ordinary tusked tribesman whose muscle fibers have been thickened by zone-5 magicule saturation into dense slabs that can shrug off a sword cut and answer with a crushing overhead swing.
- **Role in dungeon ecology:** Immune system — Orcs are the dungeon's heavy defenders, stationed where the mid-late strata begin. If a Bat draws first blood and a Wolf harries the flanks, the Orc is what walks out of the corridor at the other end to finish the job. They commit damage; they do not harass.
- **Intended player emotional reaction:** `cautious-approach` (the player must slow down, watch for the wind-up, time their hits around the telegraphed heavy swing — not a trade-hits-and-walk-away fight).

### 2. Stats

| Field | Floor 3 (Early) | Floor 28 (Mid) | Floor 75 (Deep) |
|---|---|---|---|
| Base HP | 80 | 520 | 2,600 |
| Base Contact Damage | 10 | 30 | 70 |
| Base Move Speed (px/s) | 55 | 62 | 70 |
| Base XP Yield | 32 | 120 | 440 |

Floor 3 values are the species's absolute floor for interpolation purposes; Orcs do not actually spawn in zone 1 (they first appear in zone 5 per `Constants.Zones` and the roadmap). The row exists so the stat curve is continuous and any out-of-zone spawn (e.g. a dungeon-intelligence pressure event that borrows a species) behaves.

Target TTK (class-appropriate melee, level matching floor): **5–8 hits** every floor — consistent with `cautious-approach` per template. The feel is "he will get to you eventually; make sure when he does, you have already landed four hits and can trade the fifth." HP is the highest of any Tier 3 species at any given floor (Dark Mage has comparable HP but dies in 1–2 hits once reached because it's a `burst-down-fast` caster); damage is moderate-to-high but not spike-damage; speed is slower than Wolf (85 px/s @ floor 28) and slightly slower than Spider (70 px/s @ floor 28) so the player can outpace them in open rooms but not in corridors.

### 3. AI Pattern

- Pattern: **melee-chase** with a telegraphed heavy-swing ability.
- Telegraph: **600 ms wind-up on the heavy-swing**, visible as a raised-overhead pose plus a brief color flash on the forward arm (per `cautious-approach` reaction — the telegraph is the core counterplay window). Contact damage outside the swing uses the standard 700 ms hit cooldown per [monsters.md](../monsters.md).
- Aggro range: `chase-always` (current engine default).
- Leash range: `never-leash` (current engine default).

The telegraph window is load-bearing for the "cautious-approach" feel: too short and the Orc is just a high-HP wolf, too long and he becomes trivially kiteable by any ranged class. 600 ms is deliberately wider than the standard 450 ms range-attack tell used by caster species so a melee player can read the swing mid-trade and back off one tile. On-hit with the heavy-swing deals **1.5× contact damage** (per the rule-of-thumb in monsters.md — any ability-damage enemy states a multiplier on contact as its damage baseline).

### 4. Drop-Table Hook

- Signature material: **material_sig_orc** (as locked in `MonsterDropTable.cs:46`), **10% per kill**.
- Thematic generic: **Ore** (60% ore, 20% bone, 20% hide on the generic channel — orcs are the metal-working aesthetic lineage of the dungeon; their corpses litter ore shards from the hammered iron they fought with).
- Special drop: None at species tier. First-kill boss drops are defined in the Warlord of the Fifth and Volcano Tyrant boss specs (Phase F) — this species spec does not expand those.

Matches locked entry in `scripts/logic/MonsterDropTable.cs`: `Orc → MonsterTier.Three, "material_sig_orc", 0.10f, MaterialType.Ore`.

### 5. Silhouette Readability Constraint

**Top-heavy and visibly wider than the player's horizontal bounding box — shoulders, upper arms, and hunched torso must read as "a wall with fists" from 8 tiles away, and one hand must be visibly larger than the other (the swinging arm).** Rationale: wider-than-player silhouette is the canonical `cautious-approach` cue per template — it tells the player at aggro-range "you cannot just run through this one." The asymmetric hand is the pre-telegraph: even when the Orc is idle, the player should be able to *see* which hand is about to come up. This turns the 600 ms wind-up into a readable event instead of a surprise.

### 6. Size / Scale Rule

- Multiplier: **1.40×** (Large band per template — above Wolf's 1.20× but below boss threshold 1.70×; distinguishes Orc silhouette from any other large ground mob in the current roster).
- Hitbox radius: **17 px** (round(12 × 1.40) = 17).
- Z-offset: **0 px** (ground — not airborne).

### 7. Color-Coding Contract

- Base tint surface: **body only, features exempt**.
- Exempt pixels:
  - **Tusk highlight** — 2 pixels per tusk (4 pixels total), stay bright-cream regardless of level-relative tint. Preserves species identity at every level gap; tusks are the single most-recognizable Orc cue and must never wash out.
  - **Weapon glint** — 3 pixels along the blade/haft edge of the equipped weapon, stay cold-steel regardless of tint. Signals "this one is armed" vs. any unarmed humanoid mob at a glance, and is the visual thread that the Warlord of the Fifth boss variant extends into full iron-regalia seams.
- Exempt-pixel implementation note: separate sprite sub-node modulated `Color.White` on top of the tinted body for the tusk + weapon-glint clusters. This flags the carve-out for ART-ORC; technique selection is art-lead's call per ART-SPEC-02.

Rationale for carve-outs: Orcs at a deep level-gap where the tint shifts toward desaturated grey would lose the tusk/weapon reads that distinguish them from a generic humanoid shadow. The two carve-outs preserve the "armed brute" identity across the full color range.

### 8. Art-Spec Pairing

- Paired art ticket: **ART-ORC** (proposed placeholder — no existing ticket covers Orc redraw).
- Pairing status: `[ ] art spec locked  [ ] art assets delivered`.

**Note on boss pairings.** This species spec is the base body plan for **two** future bosses (rare among species — most feed exactly one):
- **Warlord of the Fifth** (zone 5, SPEC-BOSS-WARLORD-FIFTH-01, Phase F) — inherits the Orc silhouette, scale, and color contract; the boss spec adds layered-trophy regalia, a throwing-weapon ranged ability that shifts the AI to `ranged-kite`, and hot-iron-orange aura cues. This species spec does not expand those overrides.
- **Volcano Tyrant** (zone 8, SPEC-BOSS-VOLCANO-TYRANT-01, Phase F) — inherits the Orc body plan as its "humanoid furnace" base form; the boss spec adds asymmetric obsidian/magma shoulder, body-crack heat emission, phase-3 passive heat-aura, and keeps the Orc base's `melee-chase` pattern with a heavier 900 ms telegraph on the hammer-swing. This species spec does not expand those overrides.

Both boss specs reference this species spec as their body-plan source. Any future rework of the Orc silhouette or stat curve cascades to both bosses — coordinate changes via Phase F co-locks.

---

## Acceptance Criteria

- [ ] Engineer can implement / tune the Orc from this spec + `species-template.md` + `EnemySpecies.cs` conventions without a clarifying question.
- [ ] Art-lead can compose the ART-ORC PixelLab prompt from this spec + ART-SPEC-02's prompt skeleton without a clarifying question.
- [ ] §1 reaction (`cautious-approach`) matches §2 TTK (5–8 hits) matches §3 AI (melee-chase + 600 ms heavy-swing telegraph).
- [ ] §5 silhouette constraint is what ART-ORC will test against at deliverable review (top-heavy + asymmetric-hand readable at 8 tiles).
- [ ] Drop-table fields (signature material id, rate, thematic generic) match the locked entry in `scripts/logic/MonsterDropTable.cs:46`.
- [ ] §8 boss pairing note names both downstream bosses and flags the Phase F co-lock coordination.

## Implementation Notes

- **Existing C# hooks:** `EnemySpecies.Orc` enum value, `SpeciesDatabase` hitbox entry, and `MonsterDropTable` entry are already in place. No new enum or database additions required — this spec just documents what's there and pins the numbers authored teams (boss specs, combat tuning) depend on.
- **The 600 ms telegraph on the heavy-swing** is the single load-bearing number for the `cautious-approach` feel. If telegraph-windows are retuned globally (e.g. a future accessibility pass lengthening all tells), revisit this value and re-state the TTK target — reducing the telegraph below ~450 ms collapses the reaction into `close-the-gap` territory and breaks the §1↔§2↔§3 coherence check.
- **Heavy-swing damage multiplier** (1.5× contact) is stated here per the monsters.md convention that any ability-damage enemy pins its multiplier on contact baseline. Actual implementation can live in `ClassAttacks`/`SpeciesConfig` conventions — this spec does not prescribe the code path.
- **Zone appearance:** Orcs spawn starting in zone 5 per roadmap + `Constants.Zones`. The floor-3 row in the stat table is interpolation baseline only (for cross-zone borrow events), not an actual spawn.
- **Scale 1.40× interacts with corridor pathing.** A 17 px hitbox radius is larger than Bat (8 px) or Wolf (~14 px); narrow corridors generated by `FloorGenerator` at zone 5 should be wide enough to fit one Orc + combat space. If corridor-width tuning changes in Phase H/I, verify Orc collision still allows player-side flanking room.
- **Do not expand boss behavior in this spec.** Warlord of the Fifth (ranged-kite throw, iron-regalia aura, phase shifts) and Volcano Tyrant (magma phase-shift, heat-aura phase 3) are Phase F tickets. Both inherit §1/§2/§5/§6/§7 from this species spec as their base; both override §3 AI to layer in boss-tier abilities. Any retuning request from a boss author that would require changing the Orc base must come back here first.

## Open Questions

None — spec is locked.
