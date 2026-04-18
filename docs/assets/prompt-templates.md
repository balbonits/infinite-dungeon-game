# PixelLab Prompt Template Library (ART-SPEC-01) — True-Iso / Cartoonish Rewrite

## Summary

A copy-paste-able library of PixelLab prompt blocks that every ART-* and ART-SPEC-* ticket cites. One locked style vocabulary — **cartoonish pixel art in true 2:1 isometric perspective**, drawing structural reference (tile grid, silhouette proportions, palette feel) from classic 2D isometric dungeon crawlers without replicating any single IP — one universal palette clause, and **seven named blocks** (`CHAR-HUM-ISO`, `CHAR-MON-ISO`, `TILE-ISO-ATLAS`, `OBJ-ISO`, `PROJ-ISO-8DIR`, `ICON-UI-64`, `PORTRAIT-NPC`, `PORTRAIT-CLASS`) covering every in-world and UI asset family. Every block specifies mandatory PixelLab params, a canvas and anchor contract that agrees with [docs/systems/iso-rendering.md](../systems/iso-rendering.md), a palette clamp, negative-prompt tokens, and a fill-in-the-blank skeleton with at least one worked retrofit. This is the foundation every downstream ART-SPEC-0{2..9} ticket builds on.

## Current State

**v1 invalidated — superseded by this rewrite (was committed at `375f42e`, preserved in git history only).** v1 was authored under the mistaken assumption that the engine renders in "low top-down" and that PixelLab's `view: "low top-down"` option was a close-enough match. It is not. The live engine is true 2:1 isometric (`TileShape = Isometric`, `TileSize = (64, 32)`, see [docs/systems/iso-rendering.md](../systems/iso-rendering.md) SPEC-ISO-01), and the shipped character sprites (warrior / ranger / mage / skeleton / guild maid) were authored with the wrong perspective — their feet anchor to the diamond center rather than the diamond top vertex, which is the root cause of the empirical `+ Vector2(0, 40)` spawn offset in `Dungeon.cs:39`/`:127` that ISO-01d removes.

This rewrite pivots the entire pipeline to a **cartoonish pixel-art isometric** visual direction. Structural reference for the pipeline (tile grid, silhouette proportions, iso perspective, palette warmth, UI framing) draws on the classic 2D isometric dungeon-crawler genre — the pipeline was authored with genre examples in view for scale/proportion intuition only. **No IP is replicated.** Character names, silhouettes, equipment iconography, spell icon pictographs, tile textures, and palette values are authored fresh for our world. Cartoonish aesthetic (bold chunky silhouettes, slightly exaggerated proportions, clear readable features, no photorealism) is the primary stylistic differentiator — this is our look, not a licensed one.

See §11 IP Protection for the hard rules that keep this pipeline IP-clean.

Every character / monster / tile / object / projectile currently in the repo is slated for **redraw** under the new blocks. Exempt: the game logo, the game icon, and the HP/MP orbs in `OrbDisplay.cs` (UI-only, 2D, do not participate in iso — see §8 Retrofit vs. Redraw Policy). Wave 2 asset-family specs (ART-SPEC-02 through ART-SPEC-09) consume this doc as their foundation.

## Design

### 1. Locked Style Vocabulary

**The universal preamble.** Every prompt — character, monster, tile, object, projectile, icon — opens with the following line, verbatim, before any asset-specific description:

> *"True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading)."*

Tile-family and icon-family blocks override the `single-color black outline on characters` clause with their own outline rule (selective outline for tiles; thin gold-trim border for icons). No other part of the preamble is ever overridden.

**Why the preamble is fixed.** PixelLab's text guidance is strongest on the opening tokens of a prompt. Locking the opener protects the art direction from drifting prompt-by-prompt. The asset-specific description always follows the preamble, not the other way around.

**Universal negative-prompt tokens.** Append the following phrase to every description, immediately after the palette clause (§1c):

> *"No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing."*

The "no text" clause is defensive against an observed failure mode: PixelLab occasionally renders letterforms on armor/shields/runes. Enumerating the three wrong perspectives explicitly (top-down, side, three-quarter) is the negative-prompt complement to "true isometric 2:1 dimetric" — PixelLab's training data is heavily top-down and side-view, and the universal style parameter set (§2) is not sufficient on its own to guarantee iso output.

### 1a. Perspective + Canvas Contract (Engine-Binding)

This section is the load-bearing contract between PixelLab output and the live iso renderer. Every block below inherits these rules.

**Iso view selector for PixelLab.** PixelLab's `create_character` / `create_isometric_tile` / `create_map_object` tools accept `view` values of `"low top-down"`, `"high top-down"`, and `"side"`. **None of these is true 2:1 dimetric.** The closest usable match is `"low top-down"` combined with prompt-engineered iso cues in the description (the preamble's *"True isometric 2:1 dimetric perspective, classic dungeon-crawler proportions"* clause does the heavy lifting). PixelLab's `create_isometric_tile` tool is **not** 2:1 dimetric by default — it produces 3D-block-style isometric tiles (closer to room-builder games than a flat-faced catacomb aesthetic), so the tile block (`TILE-ISO-ATLAS`) uses `create_map_object` with a hand-authored canvas + mask instead, per §3 `TILE-ISO-ATLAS` block.

| PixelLab tool | `view` value | Notes |
|---|---|---|
| `create_character` | `"low top-down"` | Preamble + negative prompts carry the iso intent. Canvas 128×128. |
| `create_map_object` (tiles) | `"high top-down"` (per tile orientation — see `TILE-ISO-ATLAS`) | Canvas 64×64 floor / 64×variable wall; exact mask. |
| `create_map_object` (props) | `"high top-down"` | Canvas 64w × variable height. |
| `create_map_object` (icons) | `"high top-down"` | Canvas 64×64, opaque background (no transparent corners). |
| `create_map_object` (projectiles) | `"low top-down"` | Canvas 32×32. |
| `create_map_object` (portraits) | `"side"` or `"high top-down"` (per block) | Canvas 256×256 (NPC) / 256×384 (class). **Not iso — UI only.** |

**Canvas sizes per family.** Classic iso dungeon-crawler characters occupy ~96×96px per frame at native 32×16 tile resolution. Our engine runs at 2× tile resolution (`TileSize = (64, 32)`), so character canvases scale proportionally to **128×128**. Floor diamonds match the engine's `TextureRegionSize = (64, 64)` exactly. Walls use 64-wide footprints with variable height extending upward. Every canvas size is locked in the block it belongs to — this table is the summary.

| Family | Canvas (w × h) | Anchor (pixel) | Engine region |
|---|---|---|---|
| Humanoid character / monster (8-dir) | 128 × 128 | bottom-center (x=64, y=128) | N/A — scene sprite |
| Large/boss character | 160 × 160 | bottom-center (x=80, y=160) | N/A — scene sprite |
| Floor tile (iso diamond) | 64 × 64 | diamond reference point (x=32, y=32) | `TextureRegionSize = (64, 64)`, diamond in lower 32px |
| Short wall (one face) | 64 × 64 | diamond reference point (x=32, y=32) | `TextureRegionSize = (64, 64)`, face in upper ~48px |
| Tall wall / gate / arch | 64 × 96 or 64 × 128 | diamond reference point (x=32, y=height−16) | Taller region; bottom 32px overlaps cell, top rises |
| Map object (short — torch, barrel) | 64 × 64 | bottom-center (x=32, y=64) | N/A — `Node2D` sprite |
| Map object (tall — pillar, statue) | 64 × 128 | bottom-center (x=32, y=128) | N/A — `Node2D` sprite |
| Map object (multi-tile — altar) | 128 × 96 | bottom-center of **southernmost** cell (x=64, y=96) | N/A |
| Projectile (8-dir) | 32 × 32 | center (x=16, y=16) | N/A |
| UI icon | 64 × 64 | full-canvas fill | N/A — UI only |
| NPC portrait | 256 × 256 | full-canvas fill | N/A — UI only |
| Class portrait | 256 × 384 | full-canvas fill | N/A — UI only |

**Bottom-center anchor rule (THE critical iso contract).** Per SPEC-ISO-01 §Tile Geometry: an entity's sprite is placed so the **bottom-center of the canvas aligns with the top vertex of the diamond of the cell the entity stands on**. This is not the diamond's center — it is the diamond's upper apex (the "top" point of the 2:1 diamond, at `(MapToLocal(cell).x, MapToLocal(cell).y - 16)` relative to the cell's reference point, which is the diamond's center).

```
Canvas (128×128, feet at bottom-center):
+----------------+
|                |   ← 128px
|     (head)     |
|                |
|                |
|    (torso)     |
|                |
|                |
|    (hips)      |
|                |
|    (legs)      |
|   (feet here   |
|    at bottom-  |
|    center)     |
+-------*--------+
        ↑
   x=64, y=128
   Canvas bottom-center
```

That bottom-center pixel lands at the **top vertex of the tile diamond** in screen space, not the diamond's center:

```
Tile diamond (64×32 in the bottom half of a 64×64 region):
          top vertex           ← character's feet land HERE
            /\                    (this is the entity anchor point
           /  \                    for iso rendering, per SPEC-ISO-01)
          /    \
 (west)  /      \  (east)
   -----<        >-----
          \    /
           \  /
            \/
          bottom vertex

The diamond's center is 16px BELOW the top vertex, offset from where the entity stands.
```

**How this manifests in prompts.** The description for every character/monster/prop block includes the phrase *"character occupies the lower ~90% of the canvas, feet at canvas bottom-center, head near canvas top, transparent background above the shoulders where possible"*. PixelLab cannot literally "anchor" a sprite — but filling the canvas bottom-up produces output where the feet land at bottom-center, which is what the Godot import offset needs. The scene-side `Sprite2D.offset = Vector2(0, -64)` (for a 128×128 sprite, positioning its bottom-center at the `Node2D`'s origin, then lifting by `TileSize.y / 2 = 16` so the feet sit at the diamond top vertex) is authored per-sprite in its `.tscn`, not per-frame in gameplay code.

**Derived offset formula for implementers** (this is what an engine-side author writes into `player.tscn` or `enemy.tscn`):

```
For a sprite with canvas height H and anchor "bottom-center":
  Sprite2D.offset.x = 0
  Sprite2D.offset.y = -(H / 2) - (TileSize.y / 2)
                    = -(H / 2) - 16

For H=128 (standard character): Sprite2D.offset = (0, -80)
For H=160 (boss):               Sprite2D.offset = (0, -96)
For H=64  (short prop):         Sprite2D.offset = (0, -48)
For H=128 (tall prop):          Sprite2D.offset = (0, -80)
```

The `- 16` term is what lifts the feet from the diamond center (Godot's default `MapToLocal` reference point) to the diamond top vertex (where feet belong for correct iso depth). This formula is derivable from the spec alone per Acceptance Criteria §13.

**8-direction rotation naming convention.** PixelLab produces 8 rotations per character under the names `"south"`, `"south-east"`, `"east"`, `"north-east"`, `"north"`, `"north-west"`, `"west"`, `"south-west"`. These are **visual (screen-space) directions, not world-axis directions.** On the iso grid:

| PixelLab rotation | Visual direction (screen) | World-axis equivalent (informational) |
|---|---|---|
| `north` | straight up the screen | world NW diagonal (`(-1, -1)`) |
| `north-east` | up-right | world N (`(0, -1)`) — iso "natural" diagonal |
| `east` | straight right | world NE diagonal (`(+1, -1)`) |
| `south-east` | down-right | world E (`(+1, 0)`) — iso "natural" diagonal |
| `south` | straight down | world SE diagonal (`(+1, +1)`) |
| `south-west` | down-left | world S (`(0, +1)`) — iso "natural" diagonal |
| `west` | straight left | world SW diagonal (`(-1, +1)`) |
| `north-west` | up-left | world W (`(-1, 0)`) — iso "natural" diagonal |

The `DirectionalSprite.cs` picker in-engine uses **visual velocity** (screen-space `Velocity` vector) to pick a rotation — so a player moving with `W+D` (up-right visually) selects `north-east`. This matches the genre convention for 8-dir iso sprite sheets: rows are visual directions, not world-grid directions.

### 1b. Style parameter defaults

| Parameter | Value | Rationale |
|-----------|-------|-----------|
| `view` | See table in §1a | Family-dependent; single project-wide default is a footgun. |
| `outline` | `single color black outline` (characters/monsters/props) · `selective outline` (tiles) · custom (icons — thin gold-trim per `ICON-UI-64`) | Characters need silhouette pop; tiles need edges that read but don't out-contrast neighbors; icons need UI-grid legibility at thumbnail size. |
| `shading` | `medium shading` (default) · `detailed shading` (hero/boss/portrait) | Medium reads cleanly at 128×128; detailed reserved for hero surfaces. |
| `detail` | `medium detail` (default) · `high detail` (hero/boss/portrait) | Mirrors shading tier. |
| `ai_freedom` | `750` (default) · `500` (batch-consistency runs — armor ladder, biome tile sets, ability icon families) | Lower freedom for batches of 5+ related items so the set stays cohesive. |
| `size` (characters) | `64` in PixelLab parameter terms | PixelLab expands `size=64` to a canvas ~96×96 in `standard` mode; we override to target 128×128 via the "character occupies lower ~90% of canvas" prompt clause + post-generation canvas crop/extend in the art-lead download script if needed. (Documented as a known friction in Open Questions → resolved below.) |
| `n_directions` | `8` (characters/monsters/projectiles) · `1` (tiles/props/icons/portraits) | 8-dir for anything that moves; single for everything else. |

### 1c. Palette clause (universal)

Every description ends with the following clause before the negative-prompt tokens:

> *"Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX."*

Hex codes are from [docs/assets/ui-theme.md](ui-theme.md). The level-relative gradient colors (grey / blue / cyan / green / yellow / gold / orange / red) are **runtime tints via Godot `Modulate`** and are NEVER baked into sprite pixels — sprites stay palette-neutral so the tint system works across all levels.

**Exempt-pixel list** (per SPEC-SPECIES-01's exempt-pixel concept): sprites may include identity-bearing pixels outside the clamp when a block's fill-in-the-blank explicitly names them. Examples that the universal clamp does NOT forbid:

- Player class accent baked on class-specific assets (warrior/ranger/mage blue `#8ed6ff`).
- NPC signature colors baked on NPC assets (Guild Maid dark-blue dress, Blacksmith's apron).
- Species-identity pixels on monsters per SPEC-SPECIES-01 (e.g., Fallen-One's red hood).
- Per-biome sub-clamp overrides on tiles (cathedral pale stone, volcano molten orange, water aquamarine).

Acceptance: a sprite's pixel palette must be a subset of the clamp **plus** the exempt pixels the block called out. A rogue magenta, neon green, or photoreal skin tone is a re-gen.

---

### 2. Named Prompt Blocks

Each block below is cited by its **Block ID** (e.g., `CHAR-HUM-ISO`) in every PR that uses it. The ID is the durable handle — block text can be refined over time, ID stays stable.

---

#### Block `CHAR-HUM-ISO` — Humanoid Character (Player Class + Humanoid NPC + Bipedal Monster)

**Purpose.** Player classes (warrior/ranger/mage + future subclasses), humanoid NPCs (Guild Master, Blacksmith, Guild Maid, Teleporter, Village Chief, Shopkeeper, Banker), and any humanoid-silhouette enemy that walks on two legs (skeleton, goblin, fallen-one, orc, dark-mage enemy). classic dungeon-crawler Warrior / Rogue / Sorcerer are the perspective + proportion + silhouette references.

**Mandatory PixelLab params:**

| Param | Value |
|-------|-------|
| Tool | `create_character` |
| `body_type` | `humanoid` |
| `template` | `mannequin` |
| `size` | `64` (PixelLab internal — canvas comes out ~96×96; manual canvas-extend to 128×128 during download if the sprite does not fill naturally) |
| `n_directions` | `8` (never 4 for this block — iso needs full rotation coverage) |
| `view` | `low top-down` |
| `outline` | `single color black outline` |
| `shading` | `medium shading` (NPC, standard enemy) · `detailed shading` (player class hero, boss) |
| `detail` | `medium detail` (NPC, standard enemy) · `high detail` (player class hero, boss) |
| `proportions` | `{"type": "preset", "name": "default"}` (overrides: warrior = `heroic`, mage = `stylized`, ranger = `default`) |

**Canvas + anchor contract** (per §1a):
- Canvas: **128×128** target. Feet at bottom-center (pixel `(64, 128)`).
- Sprite import offset: `Sprite2D.offset = Vector2(0, -80)`.
- 8 rotations named per the §1a rotation table.

**Fill-in-the-blank skeleton:**

```
[PREAMBLE (verbatim, §1)]

A [BUILD — stocky / lean / wiry / broad-shouldered / slight] [ROLE — warrior / ranger / sorcerer / blacksmith / maid / elder] rendered in cartoonish isometric pixel-art style. [OUTFIT — describe silhouette, primary garment, armor pieces, cloak/hood status]. [HELD-ITEM(S) — right hand always holds X, left hand always holds Y]. [DEFINING-FEATURE — one readable silhouette cue: horned helmet / pointed hood / wide-brim hat / glowing eyes / braided beard / tattered robes]. [COLOR-ACCENT — one signature color beat, drawn from the palette clamp + exempt-pixel allowance]. [STANCE — combat-ready / neutral / service-stance / intimidating]. Character occupies the lower ~90% of the canvas, feet at canvas bottom-center, head near canvas top, transparent background around and above the silhouette.

[PALETTE CLAUSE (verbatim, §1c)]

[NEGATIVE-PROMPT CLAUSE (verbatim, §1)]
```

**Worked example — redraw target for Warrior (current sprite slated for re-gen per §8):**

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A stocky, broad-shouldered warrior rendered in cartoonish isometric pixel-art style. Dark steel plate armor over mail, steel pauldrons, greaves, a horned helmet that covers the face. Right hand always holds a large shiny steel longsword, left hand always holds a round metal shield with iron rim. Silver trim on the armor edges, faint player-blue (#8ed6ff) cloth on the surcoat beneath the plate. Broad-shouldered, powerful combat-ready stance, slight forward lean as if mid-stride. Character occupies the lower ~90% of the canvas, feet at canvas bottom-center, head near canvas top, transparent background around and above the silhouette.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing.
```

The sentence *"rendered in cartoonish isometric pixel-art style"* is the single load-bearing perspective cue that v1 was missing. Combined with the preamble's "True isometric 2:1 dimetric" clause and the negative prompt's three wrong-perspective exclusions, it redirects PixelLab away from the low-top-down default even though the `view` parameter still reads `"low top-down"`.

---

#### Block `CHAR-MON-ISO` — Monster / Creature (non-humanoid body plans)

**Purpose.** Bats, spiders, wolves, future quadrupeds (bears, hellhounds, slimes), winged creatures (gargoyles, small dragons), arachnids (spiders, scorpions), and any enemy whose silhouette is NOT bipedal. Creature silhouette reference for the pipeline was the classic iso dungeon-crawler beast roster (fallen imps, scavenger-rats, winged gargoyles, horned brutes — archetypes, not specific IP). Our creature roster per `docs/world/species-template.md` and ART-14).

**Sub-variants.** One block, four body-plan slots:

| Slot | `body_type` | `template` | `n_directions` | Notes |
|------|-------------|------------|----------------|-------|
| `CHAR-MON-ISO.quad` (quadruped) | `quadruped` | `wolf` / `bear` / `cat` / `dog` / `horse` / `lion` | `8` | Four-legged mammals; use closest PixelLab template. |
| `CHAR-MON-ISO.wing` (winged airborne) | `humanoid` | `mannequin` | `4` or `8` | Bats, gargoyles, small winged demons — hover pose, wings spread; 4-dir acceptable for small fliers. |
| `CHAR-MON-ISO.arach` (arachnid) | `quadruped` | `cat` (closest low-slung template) | `8` | Spiders, scorpions — override prompt to describe 8 legs explicitly; template is a scaffold only. |
| `CHAR-MON-ISO.biped-mon` (bipedal monster — goblin / orc / skeleton / fallen-one / dark-mage enemy) | `humanoid` | `mannequin` | `8` | Same params as `CHAR-HUM-ISO` but monster silhouette prompt. Listed here so ART-14 and the monster-species batch have one canonical place to author non-player enemies. |

**Mandatory PixelLab params** (per sub-variant — defaults match slot table):

| Param | Value |
|-------|-------|
| `size` | `64` (same target canvas 128×128 as players — scale adjustment happens at render-time per agent-file Scale Rules: standard enemy 0.7, boss 1.0–1.2) |
| `view` | `low top-down` |
| `outline` | `single color black outline` |
| `shading` | `medium shading` (standard) · `detailed shading` (boss) |
| `detail` | `medium detail` (standard) · `high detail` (boss) |

**Canvas + anchor contract** (per §1a):
- Canvas: **128×128** standard, **160×160** boss. Feet/body-center at bottom-center.
- Sprite import offset: `Sprite2D.offset = Vector2(0, -80)` standard; `Vector2(0, -96)` boss.
- For `CHAR-MON-ISO.wing`, the "feet" are the wing-pivot point; set `Sprite2D.offset.y` to land the creature's shadow position at the diamond top vertex.

**Silhouette readability rule** (mechanic-driving, mandatory; carries over from SPEC-SPECIES-01 §5). Every monster prompt includes ONE readability cue in the description:

- Fast enemy → *"low slung crouch"* / *"forward-leaning"* / *"lean body"* / *"compact silhouette"*
- Heavy enemy → *"hulking"* / *"massive shoulders"* / *"thick armored hide"*
- Ranged caster → *"staff held high"* / *"robe trailing"* / *"hands raised casting"*
- Stealth → *"shadowed"* / *"half-hidden"* / *"wisps of darkness"*
- Airborne → *"wings spread wide"* / *"hovering mid-flight"* / *"dropping from above"*

**Fill-in-the-blank skeleton:**

```
[PREAMBLE (verbatim, §1)]

A [SIZE — small / mid-size / large / massive] [CREATURE — bat / wolf / spider / gargoyle / hellhound / fallen-one] rendered in cartoonish isometric pixel-art style, with [BODY-PLAN — 4 legs / 8 legs / leathery wings / serpentine tail / bipedal]. [READABILITY CUE — see rule above, pick one]. [DEFINING-FEATURE — glowing eyes / dripping fangs / tattered wings / bone-white hide / cracked carapace]. [COLOR-BEAT — one signature color, palette-clamp or species-exempt]. [POSE — aggressive stalking / hover mid-flight / coiled to strike]. Creature occupies the lower ~90% of the canvas, footprint at canvas bottom-center, transparent background above and around the silhouette.

[PALETTE CLAUSE (verbatim, §1c)]

[NEGATIVE-PROMPT CLAUSE (verbatim, §1)]
```

**Worked example — redraw target for Skeleton Enemy (current sprite slated for re-gen per §8; uses `.biped-mon` slot):**

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A mid-size undead skeleton warrior rendered in cartoonish isometric pixel-art style, with a bipedal lean body plan and forward-leaning predatory stance. Tattered dark armor fragments hanging from a bare bony frame, visible ribs and joints. Right hand holds a rusted notched sword, left hand empty or hanging at the side. Glowing pale-amber eye sockets. Bone-white hide with charcoal armor fragments, cold shadow cast beneath. Creature occupies the lower ~90% of the canvas, footprint at canvas bottom-center, transparent background above and around the silhouette.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing.
```

---

#### Block `TILE-ISO-ATLAS` — Isometric Tile Atlas (Floor + Wall Pieces)

**Purpose.** Per-biome floor diamonds, wall pieces (N/S/E/W wall faces, outer corners, inner corners, T-junctions, cross-junctions), stairs (up/down, 4 iso orientations), doors (4 wall-face orientations). ART-SPEC-07 / ART-12 / ART-13 batches. Reference: classic stone-catacomb tile atlas (pipeline authored with classic iso dungeon-crawler genre examples in view for proportion intuition only; no asset is replicated). Reference sheets are not archived in the repo.

**Tool choice.** Uses `create_map_object` with hand-specified canvas dimensions — **not** `create_isometric_tile`. `create_isometric_tile` produces 3D-block-style iso tiles closer to Habbo/SimCity than D1's flat-faced dimetric stone. `create_map_object` with a locked canvas + explicit mask (oval 0.8 or rectangular full-canvas) gives us direct control over the diamond footprint.

**Mandatory PixelLab params:**

| Param | Value |
|-------|-------|
| Tool | `create_map_object` |
| `width` × `height` | Floor: **64 × 64** (diamond in lower 32px, upper 32px transparent — matches `TextureRegionSize = (64, 64)`). Short wall face: **64 × 64**. Tall wall (gate / arch / shoji-tall): **64 × 96** or **64 × 128** per slot. |
| `view` | `high top-down` (the description text carries the "true iso" cue; `high top-down` gives a more top-facing orientation that works best for diamond floor generation) |
| `outline` | `selective outline` (tiles need less contrast than characters — hard outlines make tile seams scream) |
| `shading` | `medium shading` (default) · `detailed shading` (hero biomes: cathedral, sky_temple, volcano) |
| `detail` | `medium detail` (default) · `high detail` (hero biomes) |
| `seed` | **Lock to one seed per biome** — pick at first tile, reuse across all tiles in that biome. Single most important consistency lever for multi-tile batches. |
| `ai_freedom` | `500` (always — tile atlases are batch-consistency runs by definition) |

**Canvas + anchor contract** (per §1a):
- Floor: canvas 64×64, diamond rendered in lower 32px, upper 32px transparent. Anchor = diamond reference point (x=32, y=32), which is the diamond's center. Placed via `TileMapLayer.SetCell`; the engine handles placement.
- Short wall: canvas 64×64, wall face fills the full canvas with the bottom 16px overlapping the diamond footprint of the cell. Anchor = diamond reference point.
- Tall wall: canvas 64×(96 or 128), bottom 32px overlaps the cell's diamond, remaining height rises above.
- All tiles: `TileShape = Isometric`, `TileSize = (64, 32)`, `TextureRegionSize = (64, 64)` for short wall/floor. Taller walls use taller texture regions but still 64-wide.

**Slot table** (authoring unit — per biome, fill each slot):

| Slot | Canvas | Count per biome | Notes |
|------|--------|-----------------|-------|
| Floor diamond (std) | 64×64 | 4–6 variants | Base cobble/stone, cracked, mossy, drain, bloodstain — per biome flavor |
| Floor diamond (hero) | 64×64 | 1–2 variants | Boss-room floor, altar-room floor — `detailed shading` override |
| Wall NW-face | 64×64 | 1 | Wall piece whose face points northwest on the iso grid |
| Wall NE-face | 64×64 | 1 | Face points northeast |
| Wall SW-face | 64×64 | 1 | Face points southwest |
| Wall SE-face | 64×64 | 1 | Face points southeast |
| Wall outer-corner (4 orientations) | 64×64 each | 4 | Corner pieces for wall turns |
| Wall inner-corner (4 orientations) | 64×64 each | 4 | Inside corners (room convex corners) |
| Wall T-junction (4 orientations) | 64×64 each | 4 | Walls meeting perpendicular |
| Wall cross-junction | 64×64 | 1 | Four-way wall intersection |
| Stairs up (4 iso orientations) | 64×96 each | 4 | Tall asset — rises above cell |
| Stairs down (4 iso orientations) | 64×64 each | 4 | Recessed — descends into cell |
| Door / archway (4 wall-face orientations) | 64×96 each | 4 | Tall wall variant with opening |

**≈30 slots per biome × 8 biomes = ~240 tiles.** Matches ART-12 scope exactly.

**Palette sub-clamp per biome** (append after universal palette clause):

| Biome | Sub-clamp |
|-------|-----------|
| dungeon | *"Dungeon biome: stone blue-gray (#24314a / #3c4664), moss green accents, torch-amber highlights in cracks."* |
| dungeon_dark | *"Dungeon dark biome: deeper shadow tones, near-black (#0f1117) base, cold blue highlights only, no warm accents."* |
| cathedral | *"Cathedral biome: pale stone (#c8c0b0) base, stained-glass blue and gold accents, candle-amber in sconces."* |
| nether | *"Nether biome: charcoal black base, blood red (#ff6f6f) accents, sulfurous orange glow in fissures."* |
| sky_temple | *"Sky temple biome: pale cloud white, sky blue (#8ed6ff), gold (#f5c86b) trim, marble base."* |
| volcano | *"Volcano biome: obsidian black base, molten orange/red cracks, ash gray ridges."* |
| water | *"Water biome: deep aquamarine, pale cyan foam, algae green, wet stone."* |
| town | *"Town biome: warm wood browns, stone gray, green grass patches, brass-gold trim."* |

**Fill-in-the-blank skeleton:**

```
[PREAMBLE (verbatim, §1)]

A [SLOT — isometric floor diamond tile / isometric wall face tile / isometric corner piece / isometric stairs-up tile / isometric door arch] rendered in classic stone-catacomb aesthetic. [SURFACE — cobblestone / cracked stone slab / moss-covered flagstone / obsidian plate / pale marble / wooden plank / sand]. [EDGE-DETAIL — clean seam / weathered edge / moss at the corners / lava cracks / metal rim]. [MINOR-FEATURE (optional) — small skull fragment / stray pebble / single leaf / rune etched faintly (shape-only, non-letter)]. [ORIENTATION (for directional tiles) — wall face points northwest / northeast / southwest / southeast]. Seamless tile edges for grid tiling, transparent background where the diamond does not cover.

[PALETTE CLAUSE (verbatim, §1c)]
[BIOME SUB-CLAMP (table above)]

[NEGATIVE-PROMPT CLAUSE (verbatim, §1)] No visible text or runes that form letterforms. No grass on dungeon/nether/volcano/water biomes.
```

**Worked example — redraw target for dungeon floor tile (current `assets/tiles/dungeon/floor.png` slated for re-gen per §8):**

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

An isometric floor diamond tile rendered in classic stone-catacomb aesthetic. Dark blue-gray cobblestone, weathered stone slabs fit together in the diamond shape. Clean tile seam at the diamond edges, faint moss at one corner, a single hairline crack. Seamless tile edges for grid tiling, transparent background where the diamond does not cover (upper 32px of the 64×64 canvas transparent).

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.
Dungeon biome: stone blue-gray (#24314a / #3c4664), moss green accents, torch-amber highlights in cracks.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing. No visible text or runes that form letterforms. No grass on dungeon/nether/volcano/water biomes.
```

---

#### Block `OBJ-ISO` — Isometric Map Object (Furniture / Chest / Torch / Altar / Pillar / Prop)

**Purpose.** Chests (ART-15 closed/open jar/crate/chest), furniture (tables, chairs, barrels, crates), torches (wall-mounted + floor-standing), altars, pillars, statues, rubble piles, hanging cages, chains, stairs-as-props (where not part of `TILE-ISO-ATLAS`). Bottom-center anchored `Node2D` sprites that live under the `Entities` container in-scene, not as TileMapLayer cells.

**Mandatory PixelLab params:**

| Param | Value |
|-------|-------|
| Tool | `create_map_object` |
| `width` × `height` | Per-object size table below |
| `view` | `high top-down` (description carries "true iso" cue; `high top-down` produces the slight top-facing angle that sits cleanly on a floor diamond) |
| `outline` | `single color outline` |
| `shading` | `medium shading` (standard) · `detailed shading` (hero props: boss-floor altar, unique chest, sacrificial idol) |
| `detail` | `medium detail` (standard) · `high detail` (hero props) |

**Canvas + anchor contract** (per §1a):
- Standard short prop (torch, chest, barrel, crate): **64×64**. Sprite import offset `Sprite2D.offset = Vector2(0, -48)`.
- Tall prop (pillar, statue): **64×128**. Sprite import offset `Sprite2D.offset = Vector2(0, -80)`.
- Multi-tile object (altar 2×1): **128×96**. Sprite import offset `Sprite2D.offset = Vector2(0, -64)`, positioned at the southernmost floor cell the object covers (per SPEC-ISO-01 §Z-Ordering multi-tile object rule).
- Background: **transparent**. No ground shadow baked in — the `Entities` Y-sort handles visual grounding.

**Size reference table:**

| Object | Canvas (w × h) | Directions | Notes |
|--------|----------------|------------|-------|
| Jar (closed / broken) | 64 × 64 | 1 | ART-15 |
| Crate (closed / splintered) | 64 × 64 | 1 | ART-15 |
| Chest (closed / open) | 64 × 64 | 1 | ART-15 — footprint matches one floor diamond |
| Torch (wall-mounted) | 64 × 64 | 4 | Per wall orientation — N/S/E/W face |
| Torch (floor-standing) | 64 × 64 | 1 | |
| Barrel | 64 × 64 | 1 | |
| Altar (2×1 multi-tile) | 128 × 96 | 1 | Per ART-13 biome variants |
| Pillar | 64 × 128 | 1 | Tall prop, extends above one tile cell |
| Rubble pile | 64 × 64 | 1 | Low, sits on floor |
| Statue | 64 × 128 | 1 | Hero prop — `detailed shading` override |
| Hanging cage | 64 × 128 | 1 | Tall prop |
| Chains (wall-mounted) | 64 × 128 | 4 | Per wall orientation |

**Fill-in-the-blank skeleton:**

```
[PREAMBLE (verbatim, §1)]

A [BIOME adjective, optional — dungeon / cathedral / nether / volcano] [OBJECT — chest / crate / torch / altar / pillar] rendered in cartoonish isometric pixel-art style. [STATE — closed and intact / open with contents spilling / splintered / lit / unlit]. [MATERIAL — dark wood with iron bands / pitted stone / obsidian slab / pale marble]. [DEFINING-FEATURE — iron lock / carved runes (shape-only, non-letter) / flame plume / ornate trim]. Object sits at canvas bottom-center, occupies the lower ~90% of the canvas, transparent background, no ground shadow baked in.

[PALETTE CLAUSE (verbatim, §1c)]

[NEGATIVE-PROMPT CLAUSE (verbatim, §1)] No background terrain, no ground, no floor tile — transparent PNG only.
```

**Worked example (ART-15 chest, closed variant):**

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A dungeon treasure chest rendered in cartoonish isometric pixel-art style, closed and intact. Dark stained wood with iron bands and rivets. Heavy iron lock plate on the front, warm gold (#f5c86b) trim at the corners. Object sits at canvas bottom-center, occupies the lower ~90% of the canvas, transparent background, no ground shadow baked in.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing. No background terrain, no ground, no floor tile — transparent PNG only.
```

For the "open" variant, replace *"closed and intact"* with *"lid raised, gold coins and gem glint visible inside"* — all other clauses identical. Minimal-diff batch pattern.

---

#### Block `PROJ-ISO-8DIR` — Isometric Projectile (8-Direction)

**Purpose.** Arrow (ranger), magic bolt (mage), fireball, ice shard, throwing knife, any projectile that moves through iso scene space and needs per-direction visual alignment. The 8-direction arrow sheet from classic iso dungeon-crawlers is the genre reference for size and visual orientation cues only.

**Mandatory PixelLab params:**

| Param | Value |
|-------|-------|
| Tool | `create_map_object` (one generation per direction — 8 total per projectile; or `create_character` with `n_directions=8` if PixelLab's rotation consistency is adequate) |
| `width` × `height` | **32 × 32** |
| `view` | `low top-down` |
| `outline` | `single color black outline` |
| `shading` | `flat shading` or `basic shading` (projectiles are fast-moving — complex shading reads as mud at 32px) |
| `detail` | `low detail` or `medium detail` |
| `n_directions` (if using `create_character`) | `8` |

**Canvas + anchor contract** (per §1a):
- Canvas: **32×32**. Anchor = canvas center (x=16, y=16). Projectiles rotate around their center, not their base, so the anchor differs from characters/props.
- Sprite import offset: `Sprite2D.offset = Vector2(0, 0)` (centered).
- 8 rotations align to iso screen-space directions per the §1a rotation table.

**Simplicity rule.** Projectiles are single-frame or 2-frame (for shimmer/trail). Do not author projectile animations in `animate_character` — the velocity vector in-engine provides visual motion.

**Fill-in-the-blank skeleton:**

```
[PREAMBLE (verbatim, §1)]

A [PROJECTILE — arrow / magic bolt / fireball / ice shard / throwing knife] rendered in cartoonish isometric pixel-art style, shown in flight. [MATERIAL/EFFECT — wooden shaft with steel tip and fletching / glowing blue magical energy / crackling flame / jagged blue ice / tumbling steel blade]. [LENGTH/SHAPE — long and straight / compact and round / elongated teardrop]. [COLOR-BEAT — palette-clamp or reserved-accent]. Projectile fills most of the 32×32 canvas, oriented for the [DIRECTION — south / south-east / east / ...] iso rotation, transparent background.

[PALETTE CLAUSE (verbatim, §1c)]

[NEGATIVE-PROMPT CLAUSE (verbatim, §1)] No background, no hand gripping the projectile, no ground shadow.
```

**Worked example (ranger arrow, south rotation):**

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

An arrow rendered in cartoonish isometric pixel-art style, shown in flight. Wooden shaft with steel tip and brown-gray fletching. Long and straight, roughly 28 pixels tip-to-nock. Shaft stained dark brown, steel tip picks up a subtle warm gold (#f5c86b) highlight. Projectile fills most of the 32×32 canvas, oriented for the south iso rotation (tip pointing down-and-slightly-left to match screen-space south movement), transparent background.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing. No background, no hand gripping the projectile, no ground shadow.
```

---

#### Block `ICON-UI-64` — UI Icon (Ability / Skill / Item Slot)

**Purpose.** Ability icons (ART-07a MVP hotbar + ART-07b long-tail), skill-tree icons, item-slot icons for accessories (ART-06 neck/rings). Classic genre spell-icon plaques are the reference: stone-tile framed background, white/gray pictograph, red accent for dark spells, thin gold-trim border, blue-green accent for nature/cold. The game logo and the HP/MP orbs are **exempt** from this block (they use their own hand-authored art; see §8).

**Mandatory PixelLab params:**

| Param | Value |
|-------|-------|
| Tool | `create_map_object` |
| `width` × `height` | **64 × 64** (2× D1's native 28×28 for UI clarity at modern resolutions) |
| `view` | `high top-down` (flat presentation, no perspective) |
| `outline` | Custom — described in the prompt as "thin gold-trim border" (not `single color black outline`) |
| `shading` | `medium shading` (standard) · `detailed shading` (signature abilities: ultimate / mastery headers) |
| `detail` | `medium detail` |

**Canvas + anchor contract:** Not iso. 64×64 opaque canvas, **no transparent corners** (icons fill their UI slot completely). UI layer only — no Godot sprite offset.

**Icon background contract** (shared across all icons so the UI grid reads as a set):

- Square 64×64 canvas, opaque.
- Base layer: dark stone-tile texture, palette `#24314a` → `#3c4664` gradient (classic genre references often use warm-olive stone; we substitute our dungeon-blue to keep UI consistent with the game palette).
- Border: thin gold (`#f5c86b`) trim, 1–2 pixels wide, clean inner bevel (~2–3 px) so the icon reads as a carved stone plaque.
- Foreground: single pictograph, centered, rendered in near-white (`#ecf0ff`) with mid-gray shading (`#b6bfdb`). No hue in the pictograph except the reserved accent color per the ability school:
  - **Red (`#ff6f6f`)** — dark magic, curses, damage, blood-cost abilities (D1's red-tinted spell icons)
  - **Gold (`#f5c86b`)** — passive, ultimate, mastery, holy
  - **Blue (`#8ed6ff`)** — cold, frost, water, ice
  - **Green (`#6bff89`)** — nature, heal, poison (muted green for poison, bright for heal)
  - **None** — physical, martial, utility (pure white pictograph)

**Pictograph rule.** One recognizable symbol per icon, not a scene. E.g.:
- "Fireball" → flame swirl, not a wizard casting a fireball
- "Heavy Strike" → downward sword, not a warrior mid-swing
- "Dodge" → forward-leaning footprint arrow, not a character dodging

Single-symbol icons keep the grid legible at thumbnail size. If a concept can't reduce to one pictograph, split it into two abilities or pick a different symbol.

**Fill-in-the-blank skeleton:**

```
[PREAMBLE (verbatim, §1) — but replace the "single-color black outline on characters" clause with "thin gold-trim border on the UI plaque"]

A 64×64 square UI icon in classic dungeon-crawler spell-icon style. Carved stone-tile background, dark blue-gray (#24314a to #3c4664) gradient with a subtle beveled inner border and a thin gold (#f5c86b) trim frame around the outer edge. Centered pictograph: [ONE SYMBOL — flame swirl / downward sword / forward footprint arrow / upward arrow with sparks / closed fist / eye with radiating lines / skull / chalice]. Pictograph rendered in near-white (#ecf0ff) with mid-gray (#b6bfdb) shading, [ACCENT COLOR — red / gold / blue / green / none] highlight on the [FOCAL PART — tip / core / outline / pupil]. No transparent corners — icon fills the full 64×64 square.

[PALETTE CLAUSE (verbatim, §1c)]

[NEGATIVE-PROMPT CLAUSE (verbatim, §1)]
```

**Worked example (ART-07a "Fireball"):**

```
True isometric 2:1 dimetric perspective, classic dungeon-crawler proportions and weight, compact silhouette with readable equipment, gritty dark fantasy palette, thin gold-trim border on the UI plaque, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A 64×64 square UI icon in classic dungeon-crawler spell-icon style. Carved stone-tile background, dark blue-gray (#24314a to #3c4664) gradient with a subtle beveled inner border and a thin gold (#f5c86b) trim frame around the outer edge. Centered pictograph: a flame swirl with three upward-curling tongues, stylized and compact. Pictograph rendered in near-white (#ecf0ff) with mid-gray (#b6bfdb) shading, red (#ff6f6f) highlight on the tip of each flame tongue. No transparent corners — icon fills the full 64×64 square.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing.
```

---

#### Block `PORTRAIT-NPC` — NPC Dialogue Portrait (2D Bust)

**Purpose.** Per-NPC portrait images for the `DialogueBox` portrait slot. ART-09 scope: 5 NPCs (Guild Master, Blacksmith, Guild Maid, Teleporter, Village Chief) × 2 expressions (neutral + conversational) = ~10 portraits. **Trigger for a dedicated block** (≥3-asset rule, §9): 10 portraits clear the bar; framing (bust not full-body), canvas (256×256 not 128×128), and perspective (2D bust not iso) all differ fundamentally from `CHAR-HUM-ISO`.

**Not isometric.** Portraits are UI-only. Head-on or three-quarter view; fresh-authored hero archetypes drawing only on generic RPG class-portrait proportions (warrior bust, rogue bust, sorcerer bust), but no iso perspective.

**Mandatory PixelLab params:**

| Param | Value |
|-------|-------|
| Tool | `create_map_object` |
| `width` × `height` | **256 × 256** |
| `view` | `side` (closest to a front-facing bust) |
| `outline` | `single color black outline` |
| `shading` | `detailed shading` |
| `detail` | `high detail` |

**Canvas + anchor contract:** 256×256 opaque canvas (or transparent background if the DialogueBox paints its own frame — defer to ART-09 implementation). Bust framing: head + shoulders + upper chest, no lower body. UI layer only — no Godot sprite offset.

**Fill-in-the-blank skeleton:**

```
[PREAMBLE (verbatim, §1)]

A [NPC ROLE — guild master / blacksmith / guild maid / teleporter / village chief] portrait bust, head and shoulders, in the style of a classic dungeon-crawler NPC talk portrait. [FACIAL-FEATURE — weathered face with braided beard / scarred face with soot smudges / young face with kind eyes / hooded face obscured in shadow / elderly face with deep wrinkles]. [EXPRESSION — neutral / conversational / stern / welcoming]. [ATTIRE — visible collar / pauldron / apron / hood / robes of office]. [DEFINING-FEATURE — one recognizable cue: the helmet horns / the hammer over the shoulder / the prayer beads / the teleporter's glowing amulet / the chief's silver chain]. Centered bust, filling the canvas from chin at ~70% height down to mid-chest at the bottom edge.

[PALETTE CLAUSE (verbatim, §1c)]

[NEGATIVE-PROMPT CLAUSE (verbatim, §1)] No full-body shot, no full environment background, no action pose — portrait bust only.
```

---

#### Block `PORTRAIT-CLASS` — Class Portrait (2D Hero Art)

**Purpose.** Splash screen, Class Select cards, Character Card on Load Game. ART-08 scope: 3 class portraits (Warrior, Ranger, Mage). **Trigger for a dedicated block** (≥3-asset rule, §9): 3 portraits meet the threshold; canvas (256×384 portrait aspect), framing (full hero shot, ~70% height), and detail tier (highest — splash-screen quality) all differ from `PORTRAIT-NPC` and from `CHAR-HUM-ISO`.

**Not isometric.** Full hero portrait, three-quarter view, dramatic pose. Three fresh hero portraits (Warrior / Ranger / Mage) — this block exists for these three specific assets. No licensed art is referenced.

**Mandatory PixelLab params:**

| Param | Value |
|-------|-------|
| Tool | `create_map_object` |
| `width` × `height` | **256 × 384** |
| `view` | `side` (closest to three-quarter hero pose) |
| `outline` | `single color black outline` |
| `shading` | `detailed shading` |
| `detail` | `high detail` |
| `ai_freedom` | `500` (3-portrait batch — consistency across the set is critical for Class Select) |

**Canvas + anchor contract:** 256×384 opaque or transparent canvas (DialogueBox / ClassSelect paints its own frame). Hero occupies ~85% of canvas height, head near top, feet/base at bottom. UI layer only — no Godot sprite offset.

**Fill-in-the-blank skeleton:**

```
[PREAMBLE (verbatim, §1)]

A [CLASS — warrior / ranger / mage] full hero portrait, in the style of a classic dungeon-crawler class-select splash. [BUILD — stocky and armored / lean and poised / wiry and robed]. [POSE — three-quarter facing, weapon raised / drawing a bow at half-draw / staff held across the body, hand gesturing toward the viewer]. [ATTIRE-DETAIL — every armor/cloth layer visible: pauldrons + chestplate + tassets + greaves + surcoat / leather jerkin + quiver + bracers + hood / robes + mantle + belt pouches + hood]. [DEFINING-FEATURE — the class's signature silhouette: horned helmet and shield / pulled-back hood and bow / pointed hat and staff glow]. [COLOR-ACCENT — player blue (#8ed6ff) on class-signature cloth]. Hero occupies ~85% of canvas height, head near top, feet at bottom.

[PALETTE CLAUSE (verbatim, §1c)]

[NEGATIVE-PROMPT CLAUSE (verbatim, §1)] No iso perspective, no three-quarter top-down angle — direct hero portrait, front-facing three-quarter view.
```

---

### 3. Anchor Alignment Rules (ASCII Diagrams)

**Rule:** bottom-center of the sprite canvas lands at the **top vertex** of the diamond of the cell the entity stands on. The sprite import offset `Sprite2D.offset.y = -(canvas_height / 2) - 16` is what encodes this in the scene file.

**Character (128×128 canvas, standing on one floor diamond):**

```
           ┌───── canvas top (y=0)
           │
   ╔═══════╪═══════╗
   ║       │       ║
   ║    (head)     ║
   ║       │       ║
   ║   (torso)     ║    ← sprite canvas 128×128
   ║       │       ║
   ║   (legs)      ║
   ║       │       ║
   ║   (feet) ─────╫─── canvas bottom-center (x=64, y=128)
   ╚═══════╪═══════╝           │
           │                   │ THIS pixel lands on the diamond top vertex
           │                   ▼
           │            ▲ ─ top vertex (y_screen)
           │           / \
           │          /   \
           │         /     \    ← floor diamond (64×32, rendered in lower 32px
           │        / (cell) \       of a 64×64 tile region)
           │        \       /
           │         \     /
           │          \   /
           │           \ /
           │            ▼ ─ bottom vertex (y_screen + 32)
           │
     Sprite2D.offset.y = -(128/2) - 16 = -80
     Node2D.position.y = MapToLocal(cell).y   (diamond center)
     Canvas bottom lands at: position.y + 80 + 128 = position.y + 208 - 128 = ...
                           = position.y - 16 (diamond top vertex) ✓
```

**Short prop (64×64 canvas, barrel / torch / chest):**

```
   ╔═══════╤═══════╗ ── canvas top (y=0)
   ║   (lid)       ║
   ║       │       ║    ← 64×64 canvas
   ║ (body)│       ║
   ║       │       ║
   ║ (base)│ ──────╫─── canvas bottom-center (x=32, y=64)
   ╚═══════╪═══════╝
           │
           ▼
         ▲ ─ top vertex of floor diamond
        / \
       /   \
       \   /
        \ /
         ▼
     Sprite2D.offset.y = -(64/2) - 16 = -48
```

**Tall prop (64×128 canvas, pillar / statue):**

```
   ╔═══════╤═══════╗ ── canvas top (y=0)
   ║   (cap)       ║
   ║       │       ║
   ║       │       ║    ← 64×128 canvas
   ║ (shaft)       ║        (extends upward from the diamond top)
   ║       │       ║
   ║       │       ║
   ║       │       ║
   ║ (base)│ ──────╫─── canvas bottom-center (x=32, y=128)
   ╚═══════╪═══════╝
           │
           ▼
         ▲ ─ top vertex of floor diamond (pillar base sits here)
        / \
       /   \
       \   /
        \ /
         ▼
     Sprite2D.offset.y = -(128/2) - 16 = -80
```

**Floor tile (64×64 region, diamond in lower 32px):**

```
   ╔═══════════════╗ ── canvas top (y=0)
   ║               ║
   ║  (transparent ║    ← upper 32px transparent
   ║    upper      ║
   ║    32px)      ║
   ║               ║
   ║     ───▲───   ║ ── y=32, diamond top vertex (this pixel is the cell's
   ║       ╱ ╲     ║              top reference when entities stand here)
   ║      ╱   ╲    ║
   ║     ╱     ╲   ║
   ║    ◁       ▷  ║ ── y=48, diamond east/west points
   ║     ╲     ╱   ║
   ║      ╲   ╱    ║
   ║       ╲ ╱     ║
   ║        ▼      ║ ── y=64, diamond bottom vertex
   ╚═══════════════╝

     Placed by TileMapLayer.SetCell — engine handles anchor internally.
     Godot's MapToLocal(cell) returns the point at y=48 (diamond center),
     NOT the top vertex. Entity anchor adds -16 in offset to compensate.
```

**Multi-tile object (128×96 canvas, 2×1 altar covering cells (5,5)+(6,5)):**

```
   ╔═══════════════════════════════╗ ── canvas top (y=0)
   ║              (altar top)       ║
   ║     ╔═════════════════════╗    ║
   ║     ║   (altar body)      ║    ║    ← 128×96 canvas covering 2 cells
   ║     ╠═════════════════════╣    ║
   ║     ║  (altar base)       ║    ║
   ║     ╚═════════════════════╝    ║
   ║               │                ║
   ║        (base) │ ───────────────╫─── canvas bottom-center (x=64, y=96)
   ╚═══════════════╪════════════════╝
                   │
                   ▼
   Positioned at SOUTHERNMOST floor cell the object covers.
   Sprite2D.offset.y = -(96/2) - 16 = -64
   Node2D.position = MapToLocal(southernmost_cell)
```

---

### 4. Locked Style Vocabulary (changes from v1)

v1's preamble began *"Low top-down isometric pixel art"* — that was the framing error. v2 begins *"True isometric 2:1 dimetric perspective, classic dungeon-crawler proportions"*. v2's negative prompts add **three explicit wrong-perspective exclusions** (no top-down, no side-view, no three-quarter front-on view) that v1 did not have. v2's palette clause adds *"classic dungeon-crawler palette"* as a visual-reference anchor, and tightens the "gritty, muted, desaturated" tone descriptors. All hex anchors from [docs/assets/ui-theme.md](ui-theme.md) are preserved unchanged.

---

### 5. Palette Clause (grounded in ui-theme.md)

Primary clamp anchors (from `docs/assets/ui-theme.md`):

| Hex | Name | Usage |
|-----|------|-------|
| `#0f1117` | bg-0 | Darkest tones, `dungeon_dark` biome base |
| `#1b2130` | bg-1 | Secondary dark surfaces |
| `#24314a` | (derived from floor rgb 36/49/74) | Floor/wall base stone |
| `#3c4664` | (derived from wall rgb 60/70/100) | Lighter stone highlights |
| `#8ed6ff` | player | Player class accent (exempt-pixel) |
| `#f5c86b` | accent | Gold/amber — torches, trim, hero/ultimate icons |
| `#ff6f6f` | danger | Red — high-threat, dark magic, blood |
| `#6bff89` | safe | Green — nature/heal pictograph accent |
| `#ecf0ff` | ink | Near-white for icon pictographs |
| `#b6bfdb` | muted | Mid-gray for icon pictograph shading |

D1-feel adjectives layered on top: **gritty, muted, desaturated, warm candle/torch accents against cold stone**. Hex anchors are not replaced — the adjectives guide PixelLab's color mixing within the clamp.

Acceptance (per §1c): sprite pixel palette must be a subset of the clamp plus explicitly-named exempt pixels (player accent on class assets, NPC signatures, species-identity pixels per SPEC-SPECIES-01, biome sub-clamp overrides on tiles). Rogue pixels outside these lists → re-gen.

---

### 6. Drift Prevention (PR Review Checklist)

Every ART-* and ART-SPEC-* PR must satisfy all of the following before merge:

1. **Block ID citation.** PR description names the block ID used (e.g., "Uses `CHAR-HUM-ISO` for the blacksmith NPC"). Multiple blocks → list each.
2. **Prompt transcript.** The full prompt sent to PixelLab is attached (paste into the PR description or a committed `docs/assets/generation-log/<ticket>.md`). Reviewers diff this against the block's skeleton.
3. **Deviation justification.** If the prompt deviates from the skeleton (omits a clause, adds a new clause, overrides a param), the PR description explains *why* in one sentence.
4. **Palette audit.** Shipped PNG is opened in an image viewer and compared to the ui-theme.md palette. Pixels outside the clamp must match (a) a runtime-tint reserved accent, (b) a documented per-biome or per-species exempt, or (c) a re-gen is required.
5. **Silhouette readability** (characters/monsters only). The south-facing rotation is pasted at thumbnail size (32×32) in the PR. Silhouette must read as the intended role/threat at that size.
6. **Iso-alignment verified (NEW — v2 addition).** For any asset that renders in-world (characters, monsters, props, projectiles, tiles), the PR includes a screenshot of the sprite composited in-engine on the target cell. Acceptance: the sprite's bottom-center lands at the top vertex of the diamond (for entities/props), or the diamond fits the 64×64 region with upper 32px transparent (for floor tiles), or the wall face overlaps the cell's bottom 16px and rises above (for walls). If the composited screenshot shows a floating sprite, a sunken sprite, or a horizontal misalignment, the fix is in `Sprite2D.offset` — **not** in gameplay code, and **not** by re-generating the sprite.

**Tooling note.** Manual only. When batch volume crosses 30+ assets/month, evaluate a lint script (grep the committed prompt transcript for the preamble / palette / negative clauses).

---

### 7. Extension Protocol (Adding a New Block)

A new block is added **only when all three hold**:

1. **≥3 upcoming assets** fit the new family and cannot be cleanly authored with an existing block. One-off assets reuse the closest existing block with a per-ticket deviation note (§6.3).
2. **Body-plan, format, perspective, or canvas difference** from every existing block. `PORTRAIT-NPC` and `PORTRAIT-CLASS` clear this (2D not iso, bust/hero framing, larger canvas). `CHAR-MON-ISO.biped-mon` does *not* — it reuses the `CHAR-HUM-ISO` params.
3. **A worked example ready at block-lock time.** No speculative blocks. The block ships with at least one concrete asset description filled in, even if that asset hasn't been generated yet.

**Proposed-but-not-yet-needed blocks:**

| Candidate Block ID | Trigger | Notes |
|---|---|---|
| `CHAR-BOSS-ISO` | ART-10 (zone bosses, ~5–10 sprites) | May fold into `CHAR-HUM-ISO` / `CHAR-MON-ISO` with 160×160 canvas + `detailed shading` override; reassess when ART-10 starts. |
| `FX-VFX-PARTICLE` | When particle/VFX assets move from procedural to baked sprites | Not imminent; POL-04 favors shaders. |
| `MAP-TILESET-TRANSITION` | If biome-to-biome visual transitions become their own asset class | Not planned; biomes are per-zone with no in-map blending. |

Edit this table when a new block is added or a candidate is promoted.

---

### 8. Retrofit vs. Redraw Policy

**Every character, monster, tile, object, and projectile asset currently in the repo will be regenerated from this spec.** The shipped ad-hoc-era assets authored under v1's low-top-down assumption do not align correctly in the true-iso engine; rather than patch anchors per-sprite with engine offsets, we redraw them so the bottom-center rule works without per-asset hacks.

**Exempt assets (DO NOT redraw):**

| Asset | Why exempt |
|-------|------------|
| Game logo | UI-only, 2D, does not participate in iso rendering. Kept as-is. |
| Game icon | UI-only, 2D. Kept as-is. |
| HP orb (`OrbDisplay.cs`) | UI-only, hand-authored 2D sphere art tinted via `Modulate`. Kept as-is. |
| MP orb (`OrbDisplay.cs`) | Same as HP orb. Kept as-is. |

**Redraw batches (authored under wave 2 — ART-SPEC-02 through ART-SPEC-09):**

| Redraw batch | Current asset | Target block |
|---|---|---|
| Warrior (class hero) | `assets/characters/player/warrior/` | `CHAR-HUM-ISO` (proportions: `heroic`, shading: `detailed`) |
| Ranger (class hero) | `assets/characters/player/ranger/` | `CHAR-HUM-ISO` (proportions: `default`, shading: `detailed`) |
| Mage (class hero) | `assets/characters/player/mage/` | `CHAR-HUM-ISO` (proportions: `stylized`, shading: `detailed`) |
| Skeleton (enemy) | `assets/characters/enemies/skeleton/` | `CHAR-MON-ISO.biped-mon` |
| Bat (enemy — species rework) | `assets/characters/enemies/bat/` | `CHAR-MON-ISO.wing` |
| Spider (enemy — species rework) | `assets/characters/enemies/spider/` | `CHAR-MON-ISO.arach` |
| Wolf (enemy — species rework) | `assets/characters/enemies/wolf/` | `CHAR-MON-ISO.quad` (template: `wolf`) |
| Goblin (enemy) | existing sprite | `CHAR-MON-ISO.biped-mon` |
| Orc (enemy) | existing sprite | `CHAR-MON-ISO.biped-mon` |
| Dark Mage (enemy) | existing sprite | `CHAR-MON-ISO.biped-mon` |
| Guild Master (NPC) | existing sprite | `CHAR-HUM-ISO` |
| Blacksmith (NPC) | existing sprite | `CHAR-HUM-ISO` |
| Guild Maid (NPC) | `assets/characters/npcs/guild_maid/` | `CHAR-HUM-ISO` |
| Teleporter (NPC) | existing sprite | `CHAR-HUM-ISO` |
| Village Chief (NPC) | existing sprite | `CHAR-HUM-ISO` |
| Floor tile (dungeon) | `assets/tiles/dungeon/floor.png` | `TILE-ISO-ATLAS` (floor slot, dungeon sub-clamp) |
| Wall tile (dungeon) | `assets/tiles/dungeon/wall.png` | `TILE-ISO-ATLAS` (short wall-face slot, dungeon sub-clamp) |
| (All other biomes' tiles) | ART-12 scope | `TILE-ISO-ATLAS` (per biome sub-clamp) |
| Environmental objects (all biomes) | ART-13 scope | `OBJ-ISO` |
| Ability icons (any shipped) | ART-07a scope | `ICON-UI-64` |

Wave 2 ticket chain (reference dev-tracker `ART-SPEC-02` through `ART-SPEC-09`): ART-SPEC-02 (species sprite pipeline — paired with SPEC-SPECIES-01), ART-SPEC-03 (NPC sprite pipeline), ART-SPEC-04 (tile atlas pipeline), ART-SPEC-05 (map object pipeline), ART-SPEC-06 (projectile pipeline), ART-SPEC-07 (icon pipeline), ART-SPEC-08 (NPC portrait pipeline), ART-SPEC-09 (class portrait pipeline). Each wave-2 spec inherits this doc and extends the relevant block with family-specific details.

---

### 9. Worked Examples for Retroactive Fit

| Asset | Block | Canvas | Notes |
|-------|-------|--------|-------|
| Game logo | **Exempt** | n/a | UI-only, 2D. Not regenerated. |
| Game icon | **Exempt** | n/a | UI-only. |
| HP orb | **Exempt** | n/a | UI-only, hand-authored sphere. |
| MP orb | **Exempt** | n/a | UI-only. |
| Warrior (redraw target) | `CHAR-HUM-ISO` | 128×128 | 8-dir, heroic proportions, detailed shading. Classic humanoid hero sprite archetype. |
| Floor tile (redraw target, dungeon) | `TILE-ISO-ATLAS` (floor slot) | 64×64 | Diamond in lower 32px, upper transparent. Dungeon biome sub-clamp. |
| Wall tile (redraw target, dungeon) | `TILE-ISO-ATLAS` (short wall-face slot) | 64×64 | Face fills canvas, bottom 16px overlaps cell. Dungeon sub-clamp. |

---

### 11. IP Protection

**Hard rules that keep this pipeline IP-clean:**

1. **No named-IP invocation in prompts.** Prompts sent to PixelLab must not contain brand names, game titles, studio names, signature character names, or franchise-identifying spell/item names. "Classic dungeon-crawler" / "classic iso RPG" / "stone-catacomb aesthetic" are acceptable; specific game or studio references are not.
2. **Cartoonish is the primary style differentiator.** Bold chunky silhouettes, slightly exaggerated proportions, clear readable features — explicitly *not* the realistic/gritty pixel rendering of classic genre references. The preamble's "cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions" clause is load-bearing; every block inherits it and must not override the cartoonish qualifier.
3. **No 1:1 asset replication.** No sprite, tile, icon, portrait, or animation frame is copied from any licensed reference. Genre reference is used for scale/proportion/perspective intuition during *pipeline authoring*, never during *asset generation*. Reference sheets are not archived in-repo.
4. **Fresh-authored identity everywhere.** Character names, NPC roles, spell icon pictographs, tile textures, monster silhouettes, equipment iconography, palette values — all authored fresh for our world. A reviewer should be able to compare a generated asset side-by-side with any classic genre reference and see clear visual-language difference (cartoonish vs. gritty, fresh palette, fresh silhouette beats).
5. **Palette is ours.** The universal palette clause (`#24314a` / `#3c4664` / `#8ed6ff` / `#f5c86b` / `#ff6f6f`) is grounded in [ui-theme.md](ui-theme.md) and predates this rewrite. It is not borrowed.
6. **Audit trigger.** Any prompt, block, or worked example that names a specific licensed game, studio, or franchise character is a spec bug. Flag in PR review and rewrite before merge.

**Why:** the infinite-dungeon-game is original IP. Stylistic homage to the classic 2D iso RPG genre is fair game; direct replication of any single licensed title's assets or look is a legal risk and a creative one. The cartoonish aesthetic, fresh palette, fresh character/creature roster, and genre-generic framing make this pipeline defensibly our own.

---

## Acceptance Criteria

- [x] **v1 superseded cleanly.** Current State §2 explicitly calls out the v1 invalidation with commit `375f42e` preserved in git history.
- [x] **Every classic-genre reference image studied during authoring has a named block that produces analogous output.**
  - Classic humanoid hero sprite archetypes → `CHAR-HUM-ISO` (player classes + bipedal NPCs).
  - Classic stone-catacomb tile archetype → `TILE-ISO-ATLAS`.
  - Classic 8-dir arrow-sprite archetype → `PROJ-ISO-8DIR`.
  - Classic genre spell-icon plaque archetype → `ICON-UI-64`.
  - Classic monster-roster silhouette archetypes → `CHAR-MON-ISO` (four sub-variants).
  - Classic class-select hero-portrait archetype → `PORTRAIT-CLASS`.
  - (Classic genre has no direct NPC-bust analogue; `PORTRAIT-NPC` is justified by our 5-NPC roster.)
- [x] **Bottom-center anchor rule is unambiguous enough that an implementer writing `Sprite2D.offset` can derive the value from the spec alone.** §1a formula `offset.y = -(H/2) - 16` is stated and worked for H = 128, 160, 64.
- [x] **Every current in-repo asset slated for redraw has a clear block assignment.** §8 redraw table covers warrior, ranger, mage, 7 monster species (skeleton / bat / spider / wolf / goblin / orc / dark mage), 5 NPCs (Guild Master / Blacksmith / Guild Maid / Teleporter / Village Chief), floor tile, wall tile. Exempt assets (logo, icon, HP/MP orbs) are enumerated.
- [x] **Perspective + canvas contract is locked.** §1a states iso view selector per family, canvas sizes per family, bottom-center anchor rule with a derived-offset formula, 8-direction rotation naming convention mapped to iso screen space.
- [x] **Palette clause grounded in ui-theme.md.** §5 lists the hex anchors from ui-theme.md verbatim; D1-feel adjectives are additive, not replacement.
- [x] **Drift prevention checklist includes an iso-alignment bullet.** §6 item 6 is the new v2 addition.
- [x] **Extension protocol is concrete** (≥3 assets + body-plan/format/perspective/canvas differentiation + worked-example-at-lock-time). `PORTRAIT-NPC` and `PORTRAIT-CLASS` are the two blocks that earned their slots under the rule; justification is called out in each block.
- [x] **Retrofit vs. redraw policy is explicit.** §8 redraw table + exempt-asset list. Logo / icon / HP / MP orbs named as the only exempt assets.
- [x] **Open Questions: empty.**

## Implementation Notes

- This spec is prompt-pipeline only. It does not author any sprites, does not touch `assets/`, does not change Godot code.
- Every ART-* and ART-SPEC-* ticket going forward cites a block ID.
- Block IDs are the durable API. Block *text* can be refined; block *IDs* are stable. If a block is fundamentally split or deprecated, a new ID is introduced and the old one is marked deprecated with a pointer.
- The seven block IDs match the seven asset families in the engine: humanoid character, non-humanoid monster, tile, map object, projectile, UI icon, portrait (split NPC vs class). No asset the game ships should fall outside one of these seven.
- Wave 2 asset-family specs (ART-SPEC-02 through ART-SPEC-09) consume this doc as their foundation. Each extends exactly one block with family-specific details.
- SPEC-ISO-01 is the engine-side complement — anchor convention, Y-sort wiring, diamond collision polygon. This spec is the art-side complement. The two must ship their PRs in coordinated sequence: SPEC-ISO-01 defines the anchor contract; this spec specifies how sprites are generated to agree with that contract.

## Open Questions

None.
