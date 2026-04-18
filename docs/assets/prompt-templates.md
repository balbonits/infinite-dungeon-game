# PixelLab Prompt Template Library (ART-SPEC-01)

## Summary

A copy-paste-able library of PixelLab prompt blocks that every ART-* and ART-SPEC-* ticket cites. One locked style vocabulary, five named blocks (Character / Monster / Tile / Map Object / UI Icon), one extension rule. Every block specifies mandatory PixelLab params, a palette clamp, negative-prompt tokens, and a fill-in-the-blank skeleton with a worked example that maps back to an existing shipped asset. This is the foundation that lets ART-03 (75 armor sprites), ART-12 (~240 iso tiles), ART-14 (beast/insect rework), and every future batch author consistently without re-negotiating style per ticket.

## Current State

Five shipped character families (Warrior, Mage, Ranger, Skeleton, Guild Maid) and two tile families (floor, wall) were authored ad-hoc. Their PixelLab parameters are preserved in `metadata.json` files alongside the sprites and agree with the agent-file guidance in `.claude/agents/art-lead.md` and the theme constants in `docs/assets/ui-theme.md` — but no single doc captures the *reusable prompt skeleton* behind them. New batches (armor, weapons, quivers, accessories, ability icons, biome tile-variants, environmental objects, boss sprites, NPC portraits, beast/insect rework) are all blocked on this library. The ad-hoc era ends here; every asset from this point forward must cite a block ID.

## Design

### 1. Locked Style Vocabulary

**The style preamble.** Every prompt — character, monster, tile, object, icon — opens with the following line, verbatim, before any asset-specific description:

> *"Low top-down isometric pixel art, dark fantasy dungeon aesthetic, nearest-neighbor rendering (no anti-aliasing, no smoothing), palette rooted in deep blues and grays with warm gold/amber accents."*

**Why the preamble is fixed.** PixelLab's text guidance is strongest on the opening tokens of a prompt. Locking the opener protects the art direction from drifting prompt-by-prompt. The asset-specific description always follows the preamble, not the other way around.

**Style parameter defaults** (apply to every block unless overridden in that block's table):

| Parameter | Value | Rationale |
|-----------|-------|-----------|
| `view` | `low top-down` | Matches live iso renderer; Diablo-style oblique angle |
| `outline` | `single color black outline` (characters/monsters/objects) · `selective outline` (tiles) | Characters need silhouette pop against tiles; tiles need edges that read but don't out-contrast their neighbors |
| `shading` | `medium shading` (default) · `detailed shading` (hero/boss/portrait) | Medium reads cleanly at 92×92; detailed reserved for hero surfaces |
| `detail` | `medium detail` (default) · `high detail` (hero/boss/portrait) | Mirrors shading tier |
| `ai_freedom` | `750` (default) · `500` (batch-consistency runs) | Lower freedom for batches of 5+ related items (ring tiers, armor ladder) |

**Palette constraint (applies to every block).** Every description ends with the following clause before any negative-prompt tokens:

> *"Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) or amber accents only. Player blue accent (#8ed6ff) reserved for player class assets. Red (#ff6f6f) reserved for danger/high-threat assets."*

Hex codes are from `docs/assets/ui-theme.md`. The level-relative gradient colors (grey/blue/cyan/green/yellow/gold/orange/red) are **runtime tints via Godot `Modulate`** and are NEVER baked into sprite pixels — sprites stay palette-neutral so the tint system works across all levels. Exception: player class accent tints (`#8ed6ff` warrior/ranger/mage blue) and NPC signature colors (Guild Maid dark-blue dress, etc.) *can* be baked because those assets don't participate in the level-relative gradient.

**Universal negative-prompt tokens.** Append the following phrase to every description, immediately after the palette clause:

> *"No text, no letters, no logos, no watermarks, no modern elements, no firearms, no photorealism, no anime chibi face, no off-perspective angles."*

The "no text" clause is defensive against an observed failure mode: PixelLab occasionally renders letterforms on armor/shields/runes, and that shipping artifact (see ART-01 journal note — a rune-glyph sprite came back with a literal "T" letterform and had to be discarded) is exactly what this library exists to prevent.

---

### 2. Named Prompt Blocks

Each block below is cited by its **Block ID** (e.g., `CHAR-HUM-STD`) in every PR that uses it. The ID is the durable handle — block text can be refined over time, ID stays stable.

---

#### Block `CHAR-HUM-STD` — Humanoid Character (Player Class, NPC)

**Purpose.** Player classes (warrior/ranger/mage + future subclasses), humanoid NPCs (guild master, blacksmith, guild maid, teleporter, village chief, shopkeeper, banker), and any humanoid-silhouette enemy that walks on two legs (skeleton, goblin, orc, dark mage).

**Mandatory PixelLab params:**

| Param | Value |
|-------|-------|
| `body_type` | `humanoid` |
| `template` | `mannequin` |
| `size` | `64` (yields ~92×92 canvas with padding — matches every shipped character) |
| `n_directions` | `8` (never 4 for this block — iso needs full rotation coverage) |
| `view` | `low top-down` |
| `outline` | `single color black outline` |
| `shading` | `medium shading` (NPC) · `detailed shading` (player class hero) |
| `detail` | `medium detail` (NPC) · `high detail` (player class hero) |
| `proportions` | `{"type": "preset", "name": "default"}` (heroic-class overrides allowed: warrior = `heroic`, mage = `stylized`, ranger = `default`) |

**Fill-in-the-blank skeleton:**

```
[STYLE PREAMBLE (verbatim, §1)]

A [BUILD adjective — stocky / lean / wiry / broad-shouldered / slight] [ROLE noun — warrior / ranger / mage / blacksmith / maid / elder] in [OUTFIT — describe silhouette, primary garment, armor pieces]. [HELD-ITEM(S) — right hand holds X, left hand holds Y; use "always" to lock hand assignments across rotations]. [DEFINING-FEATURE — one readable silhouette cue: horned helmet / pointed hood / wide-brim hat / glowing eyes / braided beard]. [COLOR-ACCENT — one signature color beat, drawn from the palette clamp]. [STANCE — combat-ready / neutral / service-stance / intimidating].

[PALETTE CLAUSE (verbatim, §1)]

[NEGATIVE-PROMPT CLAUSE (verbatim, §1)]
```

**Worked example — retroactive fit for Warrior (shipped):**

```
Low top-down isometric pixel art, dark fantasy dungeon aesthetic, nearest-neighbor rendering (no anti-aliasing, no smoothing), palette rooted in deep blues and grays with warm gold/amber accents.

A stocky, heavily armored warrior in dark steel plate armor with silver trim. Right hand always holds a large shiny steel sword, left hand always holds a round metal shield. Steel pauldrons, greaves, and a horned helmet that covers the face. Broad-shouldered, powerful combat-ready stance.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) or amber accents only. Player blue accent (#8ed6ff) reserved for player class assets. Red (#ff6f6f) reserved for danger/high-threat assets.

No text, no letters, no logos, no watermarks, no modern elements, no firearms, no photorealism, no anime chibi face, no off-perspective angles.
```

This matches the shipped prompt in `assets/characters/player/warrior/metadata.json` word-for-word on the character-specific sentence. The preamble and palette/negative clauses are additions that all future warrior-family re-gens will include.

---

#### Block `CHAR-MON-VAR` — Monster / Creature (non-humanoid variants)

**Purpose.** Bats, spiders, wolves, future quadrupeds (bears, hellhounds, slimes), winged creatures (gargoyles, dragons-small), arachnids (spiders, scorpions), and any enemy whose silhouette is NOT bipedal. Separate block because body plan drives template choice, animation set, and silhouette rules.

**Sub-variants.** One block, four body-plan slots:

| Slot | `body_type` | `template` | n_directions | Notes |
|------|-------------|------------|--------------|-------|
| `CHAR-MON-VAR.quad` (quadruped) | `quadruped` | `wolf`/`bear`/`cat`/`dog`/`horse`/`lion` | `8` | Four-legged mammals; use closest PixelLab template |
| `CHAR-MON-VAR.wing` (winged airborne) | `humanoid` | `mannequin` | `4` or `8` | Bats, gargoyles — hover pose, wings spread; 4-dir acceptable for small fliers |
| `CHAR-MON-VAR.arach` (arachnid) | `quadruped` | `cat` (closest low-slung template) | `8` | Spiders, scorpions — override prompt to describe 8 legs explicitly; template is a scaffold only |
| `CHAR-MON-VAR.biped-mon` (bipedal monster — goblin/orc/skeleton/dark-mage-enemy) | `humanoid` | `mannequin` | `8` | Same params as `CHAR-HUM-STD` but with monster silhouette prompt; listed here so species rework (ART-14) has one canonical place to author non-player enemies |

**Mandatory PixelLab params** (per sub-variant — defaults match slot table above):

| Param | Value |
|-------|-------|
| `size` | `64` (same canvas as players — scale adjustment happens at render-time per agent-file Scale Rules: standard enemy 0.7, boss 1.0–1.2) |
| `view` | `low top-down` |
| `outline` | `single color black outline` |
| `shading` | `medium shading` (standard) · `detailed shading` (boss) |
| `detail` | `medium detail` (standard) · `high detail` (boss) |

**Silhouette readability rule** (mechanic-driving, mandatory). Per `.claude/agents/design-lead.md` tag-team guidance: a fast enemy must read as fast at a glance. Every monster prompt must include ONE readability cue in the description:

- Fast enemy → "low slung crouch" / "forward-leaning" / "lean body" / "compact silhouette"
- Heavy enemy → "hulking" / "massive shoulders" / "thick armored hide"
- Ranged caster → "staff held high" / "robe trailing" / "hands raised casting"
- Stealth → "shadowed" / "half-hidden" / "wisps of darkness"

**Fill-in-the-blank skeleton:**

```
[STYLE PREAMBLE (verbatim, §1)]

A [SIZE adjective — small / mid-size / large / massive] [CREATURE noun — bat / wolf / spider / gargoyle / hellhound] with [BODY-PLAN — 4 legs / 8 legs / leathery wings / serpentine tail]. [READABILITY CUE — see rule above, pick one]. [DEFINING-FEATURE — one silhouette cue: glowing eyes / dripping fangs / tattered wings / bone-white hide]. [COLOR-BEAT — one signature color within the palette clamp; default to creature-natural if not specified (bat = brown-black, spider = charcoal, wolf = gray)]. [POSE — aggressive stalking / hover mid-flight / coiled to strike].

[PALETTE CLAUSE (verbatim, §1)]

[NEGATIVE-PROMPT CLAUSE (verbatim, §1)]
```

**Worked example — retroactive fit for Skeleton Enemy (shipped — uses the `.biped-mon` slot):**

```
Low top-down isometric pixel art, dark fantasy dungeon aesthetic, nearest-neighbor rendering (no anti-aliasing, no smoothing), palette rooted in deep blues and grays with warm gold/amber accents.

A menacing undead skeleton warrior with glowing eyes, wearing tattered dark armor fragments. Bony frame with visible ribs. Carries a rusted sword. Dark dungeon creature aesthetic. Pale bone white and dark gray tones.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) or amber accents only. Player blue accent (#8ed6ff) reserved for player class assets. Red (#ff6f6f) reserved for danger/high-threat assets.

No text, no letters, no logos, no watermarks, no modern elements, no firearms, no photorealism, no anime chibi face, no off-perspective angles.
```

Matches `assets/characters/enemies/skeleton/metadata.json` on the character-specific sentence; the preamble and clamp/negative are the library additions.

---

#### Block `TILE-ISO-FLOOR-WALL` — Isometric Tile (Floor / Wall)

**Purpose.** Per-biome floor diamonds, wall blocks, and structural variants for ART-12 (~240 tiles across 8 biomes: dungeon / dungeon_dark / cathedral / nether / sky_temple / volcano / water / town). Also covers one-off decorative tiles (stairs, door-openings, T-junctions, corners).

**Mandatory PixelLab params:**

| Param | Value |
|-------|-------|
| Tool | `create_isometric_tile` |
| `size` | `64` (character/agent file spec — yields 64×32 floor diamonds via `thin tile`, 64×64 wall blocks via `block`) |
| `tile_shape` | `thin tile` (floors — ~10% canvas height, matches 64×32 floor grid) · `block` (walls — ~50% canvas height, matches 64×64 wall grid) |
| `outline` | `selective outline` (tiles need less contrast than characters; hard outlines make tile seams scream) |
| `shading` | `medium shading` (default) · `detailed shading` (hero biomes: cathedral, sky_temple, volcano) |
| `detail` | `medium detail` (default) · `highly detailed` (hero biomes) |
| `text_guidance_scale` | `8.0` (default) — lower (6.0) if biome comes back over-literal; higher (10+) if it drifts |
| `seed` | **Lock to one seed per biome** (pick at first tile of a biome, reuse across all tiles in that biome — this is the single most important consistency lever for multi-tile batches) |

**Palette constraint (per-biome override).** The universal palette clamp still applies, but each biome gets a narrower sub-clamp listed in its description:

| Biome | Sub-clamp override (append after universal palette clause) |
|-------|-----------------------------------------------------------|
| dungeon | *"Dungeon biome: stone blue-gray (#24314a / #3c4664), moss green accents, torch-amber highlights."* |
| dungeon_dark | *"Dungeon dark biome: deeper shadow tones, near-black (#0f1117) base, cold blue highlights only."* |
| cathedral | *"Cathedral biome: pale stone (#c8c0b0) base, stained-glass blue and gold accents, candle-amber."* |
| nether | *"Nether biome: charcoal black base, blood red (#ff6f6f) accents, sulfurous orange glow."* |
| sky_temple | *"Sky temple biome: pale cloud white, sky blue (#8ed6ff), gold (#f5c86b) trim, marble base."* |
| volcano | *"Volcano biome: obsidian black base, molten orange/red cracks, ash gray."* |
| water | *"Water biome: deep aquamarine, pale cyan foam, algae green."* |
| town | *"Town biome: warm wood browns, stone gray, green grass patches, brass-gold trim."* |

**Fill-in-the-blank skeleton:**

```
[STYLE PREAMBLE (verbatim, §1)]

An isometric [FLOOR or WALL] tile: [SURFACE — cobblestone / cracked stone slab / moss-covered flagstone / obsidian plate / pale marble / wooden plank / sand]. [EDGE-DETAIL — clean seam / weathered edge / moss at the corners / lava cracks / metal rim]. [MINOR-FEATURE (optional) — small skull fragment / stray pebble / single leaf / rune etched faintly]. Seamless tile edges for grid tiling.

[PALETTE CLAUSE (verbatim, §1)]
[BIOME SUB-CLAMP (table above)]

[NEGATIVE-PROMPT CLAUSE (verbatim, §1)] No visible text or runes that form letterforms. No grass on dungeon/nether/volcano/water biomes.
```

**Worked example — retroactive fit for current dungeon floor tile (shipped, `assets/tiles/dungeon/floor.png`):**

```
Low top-down isometric pixel art, dark fantasy dungeon aesthetic, nearest-neighbor rendering (no anti-aliasing, no smoothing), palette rooted in deep blues and grays with warm gold/amber accents.

An isometric FLOOR tile: dark blue-gray cobblestone, weathered stone slabs fit together. Clean tile seam at the diamond edges, faint moss at one corner. Seamless tile edges for grid tiling.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) or amber accents only. Player blue accent (#8ed6ff) reserved for player class assets. Red (#ff6f6f) reserved for danger/high-threat assets.
Dungeon biome: stone blue-gray (#24314a / #3c4664), moss green accents, torch-amber highlights.

No text, no letters, no logos, no watermarks, no modern elements, no firearms, no photorealism, no anime chibi face, no off-perspective angles. No visible text or runes that form letterforms. No grass on dungeon/nether/volcano/water biomes.
```

Produces a 64×64 canvas with the isometric diamond centered. The `#24314a` fill in the current placeholder `floor.png` (see `docs/assets/tile-specs.md`) is exactly the palette anchor this block targets.

---

#### Block `OBJ-MAP` — Map Object (Chest / Furniture / Stairs / Prop)

**Purpose.** Chests (ART-15 closed/open jar/crate/chest), furniture (tables, chairs, barrels, crates), stairs, doors, torches, altars, pillars, statues, rubble piles, hanging cages, chains. Most are single-direction (player interacts from any side, sprite doesn't rotate). Stairs and doors are 4-direction (orientation matters for the iso grid).

**Mandatory PixelLab params:**

| Param | Value |
|-------|-------|
| Tool | `create_map_object` |
| `width` × `height` | Per-object size table below (object fits the iso grid, does not exceed one cell unless "multi-tile" is called out) |
| `view` | `high top-down` (matches object sprites in shipped ART-13 decorations — more legible at small size than `low top-down`) |
| `outline` | `single color outline` |
| `shading` | `medium shading` (standard) · `detailed shading` (hero props: boss-floor altar, unique chest, sacrificial idol) |
| `detail` | `medium detail` (standard) · `high detail` (hero props) |
| `n_directions` equivalent | Single-direction default; 4-direction for stairs / doors / wall-mounted torches / anything the iso grid rotates around |

**Size reference table** (use these as defaults; override when called out in the ticket):

| Object | Canvas (w × h) | Directions | Notes |
|--------|----------------|------------|-------|
| Jar (closed / broken) | 64 × 64 | 1 | ART-15 |
| Crate (closed / splintered) | 64 × 64 | 1 | ART-15 |
| Chest (closed / open) | 96 × 64 | 1 | ART-15; wider than tall — sits on a diamond cell |
| Torch (wall-mounted) | 32 × 64 | 4 | Per wall orientation |
| Torch (floor-standing) | 32 × 64 | 1 | |
| Altar | 128 × 96 | 1 | 2×1 multi-tile; per ART-13 biome variants |
| Pillar | 64 × 128 | 1 | Tall prop, extends above one tile cell |
| Rubble pile | 64 × 48 | 1 | Low, sits on floor |
| Stairs (up / down) | 96 × 96 | 4 | One per cardinal iso direction (NE/SE/SW/NW) |
| Door | 64 × 96 | 4 | Per wall-face orientation |
| Banner / sign | 64 × 128 | 1 | |
| Statue | 96 × 128 | 1 | Hero prop; use detailed shading |

**Fill-in-the-blank skeleton:**

```
[STYLE PREAMBLE (verbatim, §1)]

A [BIOME adjective, optional — dungeon / cathedral / nether / volcano] [OBJECT noun — chest / crate / torch / altar]. [STATE — closed and intact / open with contents spilling / splintered / lit / unlit]. [MATERIAL — dark wood with iron bands / pitted stone / obsidian slab / pale marble]. [DEFINING-FEATURE — one silhouette cue: iron lock / carved runes (shape-only, non-letter) / flame plume / ornate trim]. Object sits on a transparent background, no ground shadow.

[PALETTE CLAUSE (verbatim, §1)]

[NEGATIVE-PROMPT CLAUSE (verbatim, §1)] No background terrain, no ground, no floor tile — transparent PNG only.
```

**Worked example (representative — no shipped chest yet, this is the prompt ART-15 will use for the chest variant):**

```
Low top-down isometric pixel art, dark fantasy dungeon aesthetic, nearest-neighbor rendering (no anti-aliasing, no smoothing), palette rooted in deep blues and grays with warm gold/amber accents.

A dungeon treasure chest, closed and intact. Dark stained wood with iron bands and rivets. Heavy iron lock plate on the front, warm gold trim at the corners. Object sits on a transparent background, no ground shadow.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) or amber accents only. Player blue accent (#8ed6ff) reserved for player class assets. Red (#ff6f6f) reserved for danger/high-threat assets.

No text, no letters, no logos, no watermarks, no modern elements, no firearms, no photorealism, no anime chibi face, no off-perspective angles. No background terrain, no ground, no floor tile — transparent PNG only.
```

For the "open" state variant, replace *"closed and intact"* with *"lid raised, gold coins and gem glint visible inside"* — all other clauses stay identical. This minimal-diff pattern (one sentence changes between closed/open) is the batch-authoring pattern every ART-15-style pair uses.

---

#### Block `ICON-UI-64` — UI Icon (Ability / Skill / Item Slot)

**Purpose.** Ability icons (ART-07a MVP hotbar set + ART-07b long-tail), skill-tree icons, item-slot icons for accessories (ART-06 neck/rings — the "small icon, no rotation" subset). Shared template: square stone-tile background + centered white/grey pictograph; Diablo / Hellfire **aesthetic inspiration only** (no 1:1 port — see art-pipeline memory note).

**Mandatory PixelLab params:**

| Param | Value |
|-------|-------|
| Tool | `create_map_object` (single-frame transparent PNG — same tool as OBJ-MAP, different template) |
| `width` × `height` | `64 × 64` |
| `view` | `high top-down` (flat presentation, no perspective) |
| `outline` | `single color outline` |
| `shading` | `medium shading` (standard) · `detailed shading` (signature abilities: ultimate/mastery headers) |
| `detail` | `medium detail` |

**Icon background contract (shared across all icons, so the UI grid reads as a set):**

- Square 64×64 canvas, **no transparent corners** (icons sit in UI slots; they fill their slot).
- Base layer: dark stone-tile texture, palette `#24314a` → `#3c4664` gradient, subtle diamond beveling at the edges (~2–3 px) so the icon reads as carved/set.
- Foreground: single pictograph, centered, rendered in near-white (`#ecf0ff`) with mid-gray shading (`#b6bfdb`). No hue in the pictograph except the reserved accent colors per the palette clamp (gold for ultimate/passive, red for damage/curse, blue for cold/frost, green for nature/heal).

**Pictograph rule.** One recognizable symbol per icon, not a scene. E.g.:

- "Fireball" → flame swirl, not a wizard casting a fireball
- "Heavy Strike" → downward sword, not a warrior mid-swing
- "Dodge" → forward-leaning footprint arrow, not a character dodging

This keeps the icon grid readable at thumbnail size. If a concept can't be reduced to one pictograph, split it into two abilities or pick a different symbol.

**Fill-in-the-blank skeleton:**

```
[STYLE PREAMBLE (verbatim, §1)]

A 64×64 square UI icon with a carved stone-tile background, dark blue-gray (#24314a to #3c4664) gradient with a subtle beveled inner border. Centered pictograph: [ONE SYMBOL — flame swirl / downward sword / forward footprint arrow / upward arrow + sparks / closed fist / eye with radiating lines]. Pictograph rendered in near-white (#ecf0ff) with mid-gray shading, [ACCENT COLOR — gold / red / blue / green / none] highlight on the [FOCAL PART — tip / core / outline]. No text, no letters, no numbers.

[PALETTE CLAUSE (verbatim, §1)]

[NEGATIVE-PROMPT CLAUSE (verbatim, §1)] No transparent corners — icon fills the full 64×64 square.
```

**Worked example — target for ART-07a "Fireball" ability icon:**

```
Low top-down isometric pixel art, dark fantasy dungeon aesthetic, nearest-neighbor rendering (no anti-aliasing, no smoothing), palette rooted in deep blues and grays with warm gold/amber accents.

A 64×64 square UI icon with a carved stone-tile background, dark blue-gray (#24314a to #3c4664) gradient with a subtle beveled inner border. Centered pictograph: a flame swirl with three upward-curling tongues. Pictograph rendered in near-white (#ecf0ff) with mid-gray shading, red (#ff6f6f) highlight on the tip of each flame tongue. No text, no letters, no numbers.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) or amber accents only. Player blue accent (#8ed6ff) reserved for player class assets. Red (#ff6f6f) reserved for danger/high-threat assets.

No text, no letters, no logos, no watermarks, no modern elements, no firearms, no photorealism, no anime chibi face, no off-perspective angles. No transparent corners — icon fills the full 64×64 square.
```

No icon is shipped yet — this example is the target template for ART-07a's first icon. Once ART-07a ships its first icon, this doc updates to point at the shipped PNG as the retroactive fit.

---

### 3. Drift Prevention (PR Review Checklist)

Every ART-* and ART-SPEC-* PR must satisfy all of the following before merge:

1. **Block ID citation.** The PR description names the block ID used (e.g., "Uses `CHAR-HUM-STD` for the blacksmith NPC"). If multiple blocks are used, list each.
2. **Prompt transcript.** The full prompt sent to PixelLab is attached (paste into the PR description or a committed `docs/assets/generation-log/<ticket>.md`). Reviewers can diff this against the block's skeleton.
3. **Deviation justification.** If the prompt deviates from the block's fill-in-the-blank structure (omits a clause, adds a new clause, overrides a param), the PR description explains *why* in one sentence. Examples of legitimate deviations: "Used `detailed shading` instead of `medium` because this is the zone-4 boss and is held to hero-prop standard" or "Omitted the palette sub-clamp because this is the cathedral biome and the universal clamp already covers the pale-stone tones."
4. **Palette audit.** The shipped PNG is opened in an image viewer and visually compared to the `docs/assets/ui-theme.md` palette. Any pixel outside the clamp (e.g., a rogue magenta, neon green, photoreal skin tone) is either (a) the runtime-tint reserved accent, (b) a conscious per-biome override documented in the PR, or (c) a re-gen is required.
5. **Silhouette readability** (characters/monsters only). The sprite's south-facing rotation is pasted at thumbnail size (32×32) in the PR. If the silhouette does not read as the intended role/threat at that size, re-gen with a stronger readability cue per `CHAR-MON-VAR`'s silhouette rule.

**Tooling note.** No automation yet. This is convention-only — reviewer (art-lead or PR author self-review) walks the checklist manually. When the batch volume crosses 30+ assets/month, evaluate whether a lint script (grep the committed prompt transcript for the preamble and palette clauses) is worth writing. Not blocking on it now.

---

### 4. Extension Protocol (Adding a New Block)

A new block is added **only when all three hold**:

1. **≥3 upcoming assets** fit the new family and cannot be cleanly authored with an existing block. One-off assets reuse the closest existing block with a per-ticket deviation note (§3.3).
2. **Body-plan or format difference** from every existing block. Example: "ability portraits" (large-format hero shots for ability tooltips) differ from `ICON-UI-64` in size (256×256), from `CHAR-HUM-STD` in framing (bust not full-body), and from `OBJ-MAP` in subject. That's a new block. Counter-example: "skeleton archer" fits `CHAR-MON-VAR.biped-mon` — no new block, just a fill-in the skeleton.
3. **A worked example ready at block-lock time.** No speculative blocks. The block ships with at least one concrete asset description filled in, even if that asset hasn't been generated yet. Prevents blocks from rotting into abstract guidance no one uses.

**Proposed-but-not-yet-needed blocks** (listed here so the library has an extension backlog; author when the ≥3 rule fires):

| Candidate Block ID | Trigger | Notes |
|--------------------|---------|-------|
| `PORT-NPC-BUST` | ART-09 (NPC dialogue portraits — 5 NPCs × 2 expressions = 10 portraits) | Bust framing, higher detail, larger canvas (~256×256) |
| `PORT-CLASS-HERO` | ART-08 (3 class portraits for splash / class-select / character-card) | Even larger canvas (~256×384), most detailed tier |
| `CHAR-BOSS` | ART-10 (zone bosses, ~5–10 sprites) | Still humanoid or quadruped underneath — extends `CHAR-HUM-STD` / `CHAR-MON-VAR` with detailed-shading + scale-1.0-to-1.2 rule; may not need its own block if the detail-shading override covers it |
| `FX-PARTICLE` | When particle/VFX assets move from procedural to baked sprites | Not imminent; POL-04 still favors shaders |

Edit this table when a new block is added or a candidate is promoted.

---

## Acceptance Criteria

- [x] **Style vocabulary is locked.** One preamble, one palette clause, one negative-prompt clause — all quoted verbatim. Any edit to these three strings is a versioned change to this spec and requires re-authoring any in-flight prompts.
- [x] **Five named blocks are defined** — `CHAR-HUM-STD`, `CHAR-MON-VAR` (with four sub-variants), `TILE-ISO-FLOOR-WALL`, `OBJ-MAP`, `ICON-UI-64` — each with mandatory params, fill-in-the-blank skeleton, negative prompts, and a worked example.
- [x] **Retroactive-fit check.** The warrior sprite (`assets/characters/player/warrior/metadata.json`), skeleton sprite (`assets/characters/enemies/skeleton/metadata.json`), dungeon floor tile, and dungeon wall tile all map back to a block (`CHAR-HUM-STD`, `CHAR-MON-VAR.biped-mon`, `TILE-ISO-FLOOR-WALL`, `TILE-ISO-FLOOR-WALL` respectively) with no style gaps — the only additions are the preamble and palette/negative clauses, which are cumulative not contradictory.
- [x] **ART-03 authorability.** A reviewer can produce any of the 75 upcoming armor sprites using only `CHAR-HUM-STD` + a fill-in the description slot for the armor piece (slot + tier + class). The batch-consistency override (`ai_freedom: 500`) is called out for the ladder runs.
- [x] **Drift prevention defined.** PR checklist is explicit, actionable, and manual (no tooling dependency).
- [x] **Extension protocol defined.** ≥3-asset rule + body-plan/format gate + worked-example requirement.
- [x] **Two different authors converge.** Given the same ticket (e.g., "skeleton archer in zone 2, CHAR-MON-VAR.biped-mon"), two reviewers independently filling in the skeleton would produce prompts that differ only in the adjective slots — not in params, palette, or structure.

## Implementation Notes

- This spec is prompt-pipeline only. It does not author any sprites, does not touch `assets/`, does not change Godot code.
- Every ART-* and ART-SPEC-* ticket going forward cites a block ID in its `Notes` column or PR description. Retroactive citation of shipped assets is optional (tracker rows for done tickets are already written) but recommended for any re-gen.
- The five block IDs are the durable API. Block *text* can be refined over time (tighter phrasing, new negative tokens, a fresh worked example). Block *IDs* are stable — if a block is ever fundamentally split or deprecated, a new ID is introduced and the old one is marked deprecated with a pointer.
- When the ISO-01 content contract (per-biome directory structure) lands, `TILE-ISO-FLOOR-WALL` gets a structural addition (per-biome subdirectory table) but the prompt skeleton itself does not change. Tracked as a follow-up edit, not a new block.
- The `docs/assets/pixellab.md` "Existing Account Assets" table is authoritative for past character UUIDs and params. When re-generating a shipped character, cite that UUID + this spec's block ID together.
- No per-ticket reauthentication with PixelLab is required — the MCP connection persists within a session per the art-lead agent file.

## Open Questions

None. All five blocks retroactively fit shipped assets; extension protocol is concrete (≥3 assets + worked example); drift prevention is manual-reviewable without tooling dependency. If a real blocker emerges during ART-03 authoring (the first post-lock batch), re-open this section and document the gap.
