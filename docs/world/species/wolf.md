# Species — Wolf

## Summary

Pack-hunter canid species of zone 2. Tier 2 mid-threat that defines the player's first encounter with **group tactics** — individually moderate, collectively lethal. The spec locks Wolf's reaction as `pack-management`, AI pattern as `pack` (coordinated flanking), and Large-band scale. Feeds the zone 2 boss **Howling Pack-Father** as its species base body plan.

## Current State

**Spec status: LOCKED** — authored 2026-04-18 per SPEC-SPECIES-WOLF-01 (Phase E). Second species spec to lift from `species-template.md` after Bat. Paired art ticket ART-14 covers the re-art batch (Bat / Spider / Wolf) that will deliver the flanking-pack silhouette required by §5.

Paired with [species-template.md](../species-template.md) (structural contract) and [species-pipeline.md](../../assets/species-pipeline.md) (art pipeline ART-SPEC-02). Neither half ships without the other.

## Design

### 1. Identity

- **Fiction beat:** An ordinary timber wolf warped by zone-2 magicule bloom into a lean, over-muscled hunter whose pack instincts have sharpened into near-tactical coordination — the alpha signal is now chemical, not vocal, and a pack reads each other's intent across a room.
- **Role in dungeon ecology:** Immune system — Wolves patrol zone 2 in packs of 3–5, coordinating flanks against anything that isn't wolf-kin; they're the first species the dungeon fields that thinks beyond "see intruder, charge intruder."
- **Intended player emotional reaction:** `pack-management` (the player must read the group, not the individual — a single Wolf is a soft target, three Wolves with flank-spacing is a kill box).

  *Also* `kite-from-range` when the pack is down to one — a lone survivor commits to a straight rush and must be stopped quickly. Secondary only; §3 AI pattern must serve the primary reaction.

### 2. Stats

Anchored against Bat's Tier 1 baseline (floor 3: HP 22, dmg 4, speed 90, XP 12). Wolf is Tier 2 — notably more dangerous per-unit than a Bat but still a midfield threat, not a tank. Per-unit stats are moderate; the scary number is **pack count** (3–5 on spawn per monsters.md pack rules), which §3 leverages.

| Field | Floor 3 (Early) | Floor 28 (Mid) | Floor 75 (Deep) |
|---|---|---|---|
| Base HP | 40 | 215 | 960 |
| Base Contact Damage | 7 | 24 | 66 |
| Base Move Speed (px/s) | 110 | 125 | 140 |
| Base XP Yield | 20 | 78 | 290 |

Per-unit ratios vs Bat at floor 3: HP ≈1.8×, damage ≈1.75×, speed ≈1.22×, XP ≈1.67×. Wolves are the class of enemy that **outpaces the player's walk speed** from first encounter — you cannot simply back away from a Wolf pack without cover.

Target TTK (class-appropriate melee, level matching floor): **1–2 hits per Wolf**, every floor — per template's pack-management row. The feel is "thin them fast; a full pack will eat you in seconds, but the individual goes down in one committed swing." If TTK creeps to 3+ hits per unit, Wolves stop feeling like a pack problem and start feeling like a single tanky-squad problem — that's a Tier 3 feel, not a Tier 2 one.

### 3. AI Pattern

- Pattern: **pack** — Wolves coordinate with nearby same-species units. On aggro, the pack splits: the nearest Wolf charges head-on (the "anchor"), the remaining 2–4 peel wide to flank, aiming to arrive on the player's flanks/rear within ~1.5 s of the anchor's commit. When reduced to one survivor, that Wolf drops pack behavior and reverts to straight `melee-chase`.
- Telegraph: N/A — contact damage, 700 ms hit cooldown per [monsters.md](../monsters.md). Pack-split is not a telegraph per se, but the **flanking arc is the read** — a player who sees the pack fan wide knows the next hit is coming from outside their forward cone.
- Aggro range: `chase-always` (current engine default). Pack-split trigger fires when any pack member crosses aggro; the whole visible pack commits together (they share aggro within pack-radius).
- Leash range: `never-leash` (current engine default). Future tuning knob: if pack leashing becomes necessary, it should leash as a whole pack, not per-unit, so the group never splits into "aggro'd" vs "idle" halves mid-fight.

Pack cohesion spec (for the impl team): "nearby same-species" = Wolves within 8 tiles of each other at aggro moment; this defines the coordinating group and does not reshuffle mid-fight (a Wolf that joins via respawn is a new fight, not an addition to the current pack).

### 4. Drop-Table Hook

- Signature material: **Wolf Pelt Sigil** (`material_sig_wolf` — placeholder name; id locked in `scripts/logic/MonsterDropTable.cs:44`; see [item-catalog.md § Materials](../../inventory/item-catalog.md)), **10%** per kill.
- Thematic generic: **Hide** (60% hide, 20% ore, 20% bone on the generic channel — thick pelts dominate the drop).
- Special drop: None. (Howling Pack-Father gets its own first-kill drop via SPEC-BOSS-HOWLING-PACK-FATHER-01 in Phase F.)

Matches locked entry in `scripts/logic/MonsterDropTable.cs`: `Wolf → MonsterTier.Two, material_sig_wolf, 0.10f, MaterialType.Hide`.

The 10% signature rate (vs Bat's 8%) is design-intentional: Wolves come in packs of 3–5, so over an average pack kill the player sees 0.3–0.5 signature drops per pack — same expected yield per encounter as a single-spawn Tier 1 species. Per-kill rate and per-encounter rate are balanced to different axes on purpose.

### 5. Silhouette Readability Constraint

**A pack of 3–5 Wolves must read as a *pack* from 8 tiles away — each Wolf must have a distinct forward-leaning silhouette with visible spacing between pack members, not a clumped blob or a line of identical dogs.** Rationale: `pack-management` only works as a reaction if the player can count the group and see the flank-split happening at aggro-range. If the pack clumps visually or reads as one shape, the player defaults to "charge the blob" (wrong reaction, kite-from-range territory) or "panic-swing" (also wrong). The constraint binds §1 reaction and §3 flanking AI to the visual: spacing is the read.

Sub-constraints that fall out of this:
- Each Wolf sprite must have a clearly forward-leaning stance (reads as a closer, not a turret — matches Tier 2 melee).
- The pack as rendered on screen must not exceed ~60% of a tile row in total horizontal footprint when mid-charge — tighter than that, it blobs.
- Color-coding tint (§7) must not erase the silhouette edges between adjacent Wolves. If the level-gap tint makes two Wolves bleed into each other visually, the pack-read fails. This is why §7's exempt-pixel list is non-empty.

### 6. Size / Scale Rule

- Multiplier: **1.25×** (Large band per template — bottom of the Large band, deliberately smaller than Orc's combat size so Wolves still feel "fast and dangerous" rather than "tanky and dangerous").
- Hitbox radius: **15 px** (round(12 × 1.25) = 15).
- Z-offset: **0 px** (ground-dweller — standard iso Y-sort).

Rationale for 1.25× (not higher): a Wolf must be visibly larger than the player sprite (so a pack doesn't get visually lost behind the player mid-flank) but noticeably smaller than an Orc (so the player's Tier-2 vs Tier-3 instinct-read stays clean). 1.25× is the smallest value that still reads as "Large" against a 1.0× player in iso projection — tested against the scale-band example in [iso-rendering.md](../../systems/iso-rendering.md).

### 7. Color-Coding Contract

- Base tint surface: **body only, features exempt**.
- Exempt pixels:
  - **Eye glow** (two bright pixels per Wolf face) — the pack-predator cue. A Wolf with eye-glow visible across level gaps reads as "hunter," a fully-tinted Wolf can blur into "dog."
  - **Fang highlights** (mouth-line bright pixels) — sells the forward-leaning aggression stance at any tint, critical for §5's forward-lean read.
- Exempt-pixel implementation note: separate sprite sub-node modulated `Color.White` stacked above the tinted body sprite. Same technique as Bat's eye highlights — reuse the implementation convention.

Rule reminder per template §7: eye-glow and fang highlights are species-identity pixels. At high level gaps (e.g. a level-80 player walking back into zone 2 to farm), the body-tint desaturates heavily toward grey — the exempt pixels keep Wolves recognizable as Wolves rather than "generic grey quadrupeds."

### 8. Art-Spec Pairing

- Paired art ticket: **ART-14** (Bat / Spider / Wolf rework batch).
- Pairing status: `[ ] art spec locked  [ ] art assets delivered`.

---

## Acceptance Criteria

- [ ] Engineer can implement the Wolf from this spec + `species-template.md` + `EnemySpecies.cs` conventions without a clarifying question. (Wolf enum, `SpeciesDatabase` entry, `MonsterDropTable` entry already exist; this spec documents what's there plus the pack-coordination rule for the AI author.)
- [ ] Art-lead can compose the PixelLab prompt for a flanking-pack Wolf from this spec + ART-SPEC-02's prompt skeleton without a clarifying question. Key prompt cues: Large-band scale 1.25×, forward-leaning quadruped stance, glowing eyes + fang highlights held as exempt pixels, 3–5-unit silhouette distinctness at 96-px thumbnail.
- [ ] §1 reaction (`pack-management`) matches §2 TTK (1–2 hits per Wolf) matches §3 AI (`pack` coordinated flank). Verification: a reviewer reading only this spec can confirm the three are coherent.
- [ ] §5 silhouette constraint is what ART-14 tests against at deliverable review — specifically the "3–5 Wolves at 8 tiles must read as a pack, not a blob" rule.
- [ ] Drop-table entry matches `MonsterDropTable.cs:44` exactly: Tier Two, `material_sig_wolf`, 10%, Hide.

## Implementation Notes

- **Existing C# hooks:** `EnemySpecies.Wolf` enum value exists; `SpeciesDatabase` hitbox entry exists; `MonsterDropTable` entry exists (locked). No new enum or database additions needed — the spec documents what's already there.
- **New AI work for pack coordination.** The current melee-chase AI does not implement the pack-split flanking described in §3. Implementation should live in a pack-behavior helper shared only among `pack`-pattern species (Wolves today; pack-pattern Goblin variants may reuse later). The pack-radius (8 tiles for "nearby same-species") and arrive-within-1.5s flanking window are the load-bearing tuning values; both live here and should be mirrored as named constants in the impl.
- **Pack-aggro sharing.** When one Wolf aggros, all Wolves within pack-radius aggro simultaneously and commit to the split. This is the mechanical payoff of the `pack` AI pattern — players get punished for stepping into a pack's cone, not just a single Wolf's cone.
- **Lone-survivor fallback.** When pack count reaches 1, the surviving Wolf drops pack behavior and reverts to `melee-chase` (straight-line pursuit). This keeps the endgame of a pack fight fast and readable — no pointless solo flanking against an already-engaged player.
- **Zone appearance:** Wolves spawn in zone 2 (Tier 2 per `Constants.Zones`) and feed into the zone 2 boss **Howling Pack-Father** (see SPEC-BOSS-HOWLING-PACK-FATHER-01 in Phase F — do NOT expand that boss here). The boss spec inherits Wolf's body plan, scale band (upgraded to Boss band 1.8–2.2×), and pack-coordination AI as its base — boss-specific overrides (summoning mechanic, phase-shift pack-call) belong in the boss spec.

## Open Questions

None — spec is locked.
