# Tile Pipeline — Per-Biome Atlas Redraw (ART-SPEC-03)

## Summary

Locks the full per-biome tile atlas generation recipe that implements the `TILE-ISO-ATLAS` block from [prompt-templates.md](prompt-templates.md) (ART-SPEC-01 v2). Spec strategy: author the **Dungeon biome deeply** as the exemplar (complete prompts for every one of ~30 atlas slots), then **template the other 7 biomes** (`dungeon_dark`, `cathedral`, `nether`, `sky_temple`, `volcano`, `water`, `town`) via a palette + motif substitution table. The slot grid is identical across all 8 biomes; only palette hook and motif hook substitute.

All prompts use `create_map_object` with hand-authored canvas + mask dimensions (**not** `create_isometric_tile` — see [prompt-templates.md §3 `TILE-ISO-ATLAS`](prompt-templates.md) for rationale). All prompts follow [prompt-templates.md §11 IP Protection](prompt-templates.md) — zero named-IP invocation, cartoonish pixel-art, stone-catacomb framing only.

Consumed by: ART-12 (iso tile-atlas implementation). Preceded by the Bucket D delete sweep from [asset-inventory.md §Bucket D](asset-inventory.md). Engine binding: [docs/systems/iso-rendering.md](../systems/iso-rendering.md) SPEC-ISO-01 (tile geometry, wall collision, Y-sort).

## Current State

**Spec status: DRAFT.** Awaiting product-owner review.

Current tile assets under `assets/tiles/<biome>/` are the v1 top-down-era atlas: single floor + single wall per biome (some biomes have 4–6 floor variants, none have the full wall/corner/junction/stair coverage the iso renderer needs). [asset-inventory.md §Bucket D](asset-inventory.md) enumerates the v1 files to burn down in the first commit of the ART-12 redraw PR.

No biome currently has outer-corner / inner-corner / T-junction / cross-junction wall pieces or stairs tiles. Hand-placing walls in a true-iso scene without these pieces produces visible diamond-corner gaps. This spec is what unblocks ART-12 to produce the full ~240-tile atlas (30 slots × 8 biomes).

## Design

### 1. Tile Atlas Slot Grid (Dungeon — exemplar, identical across biomes)

All 8 biomes use the **same slot grid** described here. Only palette + motif substitute (§3).

**Canvas conventions (per [prompt-templates.md §1a](prompt-templates.md)):**

- **Floor tile**: 64 × 64 canvas — diamond rendered in lower 32px, upper 32px fully transparent. Matches `TextureRegionSize = (64, 64)`.
- **Short wall / corner / junction**: 64 × 64 canvas — wall face fills the canvas, bottom 16px overlaps the floor diamond footprint.
- **Tall wall / stairs-up / door-arch**: 64 × 128 canvas — bottom 32px overlaps the cell's diamond, remaining 96px rises above.
- **Stairs-down**: 64 × 64 canvas — recessed into the cell; upper 32px transparent.
- Seamless tiling requirement: **floor variants must tile 2×2** and **wall-face variants must tile horizontally** without visible seams. Corners, junctions, stairs, and doors are **standalone** (no tiling requirement).

**PixelLab params (locked across all slots, all biomes):**

| Param | Value |
|---|---|
| Tool | `create_map_object` |
| `view` | `high top-down` |
| `outline` | `selective outline` |
| `shading` | `medium shading` (hero biomes `cathedral` / `sky_temple` / `volcano` override to `detailed shading`) |
| `detail` | `medium detail` (hero biomes override to `high detail`) |
| `ai_freedom` | `500` (tile atlases are batch-consistency runs) |
| `seed` | **Lock one seed per biome**, reuse across every slot in that biome. Single most important consistency lever for the batch. |

Canvas `width` × `height` varies per slot per the slot grid below.

**Slot grid (30 slots — Dungeon biome exemplar):**

| # | Slot ID | Canvas (w × h) | Purpose / Adjacency | Tiling Requirement |
|---|---------|---------------|---------------------|---------------------|
| 1 | `floor-0` | 64 × 64 | Base cobblestone floor diamond (most common) | Tiles 2×2 seamless |
| 2 | `floor-1` | 64 × 64 | Cracked stone variant | Tiles 2×2 seamless |
| 3 | `floor-2` | 64 × 64 | Mossy flagstone variant | Tiles 2×2 seamless |
| 4 | `floor-3` | 64 × 64 | Drain / grate variant | Tiles 2×2 seamless |
| 5 | `floor-4` | 64 × 64 | Bloodstain / dark-splotch variant | Tiles 2×2 seamless |
| 6 | `floor-hero` | 64 × 64 | Boss-room / altar-room hero floor (`detailed shading` override) | Tiles 2×2 seamless |
| 7 | `wall-face-NW` | 64 × 64 | Wall face pointing northwest on the iso grid | Tiles horizontally (against another `wall-face-NW`) |
| 8 | `wall-face-NE` | 64 × 64 | Wall face pointing northeast | Tiles horizontally |
| 9 | `wall-face-SW` | 64 × 64 | Wall face pointing southwest | Tiles horizontally |
| 10 | `wall-face-SE` | 64 × 64 | Wall face pointing southeast | Tiles horizontally |
| 11 | `outer-corner-N` | 64 × 64 | Convex corner where two wall faces meet facing north (visual top) | Standalone |
| 12 | `outer-corner-E` | 64 × 64 | Convex corner facing east (visual right) | Standalone |
| 13 | `outer-corner-S` | 64 × 64 | Convex corner facing south (visual bottom) | Standalone |
| 14 | `outer-corner-W` | 64 × 64 | Convex corner facing west (visual left) | Standalone |
| 15 | `inner-corner-N` | 64 × 64 | Concave corner (inside of a room) facing north | Standalone |
| 16 | `inner-corner-E` | 64 × 64 | Concave corner facing east | Standalone |
| 17 | `inner-corner-S` | 64 × 64 | Concave corner facing south | Standalone |
| 18 | `inner-corner-W` | 64 × 64 | Concave corner facing west | Standalone |
| 19 | `t-junction-N` | 64 × 64 | T-junction where a wall branches off to the north | Standalone |
| 20 | `t-junction-E` | 64 × 64 | T-junction branching east | Standalone |
| 21 | `t-junction-S` | 64 × 64 | T-junction branching south | Standalone |
| 22 | `t-junction-W` | 64 × 64 | T-junction branching west | Standalone |
| 23 | `cross-junction` | 64 × 64 | Four-way wall intersection | Standalone |
| 24 | `stairs-up-N` | 64 × 128 | Stairs ascending, treads facing north (visual up) | Standalone |
| 25 | `stairs-up-E` | 64 × 128 | Stairs ascending, treads facing east | Standalone |
| 26 | `stairs-down-S` | 64 × 64 | Stairs descending, treads facing south (visual down) | Standalone |
| 27 | `stairs-down-W` | 64 × 64 | Stairs descending, treads facing west | Standalone |
| 28 | `door-arch-NW` | 64 × 128 | Archway / door in a NW-facing wall | Standalone |
| 29 | `door-arch-NE` | 64 × 128 | Archway / door in a NE-facing wall | Standalone |
| 30 | `door-arch-SW` | 64 × 128 | Archway / door in a SW-facing wall | Standalone |

**Total: 30 slots per biome × 8 biomes = 240 tiles.** Matches ART-12 scope and [iso-rendering.md §Sprite Atlas Layout per Biome](../systems/iso-rendering.md).

**Iso-adjacency coverage check.** The wall set (faces × 4, outer corners × 4, inner corners × 4, T-junctions × 4, cross × 1 = 17 pieces) covers every adjacency case a 2D tile map can produce when placed on a diamond grid. Floors (6 variants) cover surface variety. Stairs (4 orientations) and doors (3 common orientations — the SE arch is intentionally omitted because iso cameras almost never face it; add if level-design needs it) cover vertical and portal transitions.

### 2. Dungeon Biome — Full Prompt Library

Every prompt opens with the ART-SPEC-01 universal preamble verbatim and closes with the universal palette clause + negative-prompt clause verbatim. The dungeon sub-clamp is the one in [prompt-templates.md §3 `TILE-ISO-ATLAS`](prompt-templates.md): `"Dungeon biome: stone blue-gray (#24314a / #3c4664), moss green accents, torch-amber highlights in cracks."`

**Dungeon motif vocabulary** (the substitution target for other biomes — §3):

- Surface: dark blue-gray cobblestone, weathered stone slabs
- Edge detail: moss at the corners, hairline cracks, faint rune etchings (shape-only, non-letter)
- Structural: iron-banded wooden doors, iron-rimmed arches
- Cartoonish descriptors: chunky block stones, slightly exaggerated crack lines, readable mossy edges (not photorealistic)

#### 2.1 Floor prompts (slots 1–6)

**`floor-0` — base cobblestone:**

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, selective outline on tiles, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

An isometric floor diamond tile rendered in classic stone-catacomb aesthetic. Dark blue-gray cobblestone, chunky block stones fitted together in the diamond shape with readable grout lines between them. Clean tile seam at the diamond edges for seamless grid tiling. Slightly exaggerated edge bevels give the stones a cartoonish readable weight. Seamless tile edges — the diamond must tile in a 2×2 grid without visible seams. Transparent background where the diamond does not cover (upper 32px of the 64×64 canvas fully transparent).

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.
Dungeon biome: stone blue-gray (#24314a / #3c4664), moss green accents, torch-amber highlights in cracks.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing. No visible text or runes that form letterforms. No grass on dungeon/nether/volcano/water biomes.
```

**`floor-1` — cracked stone:** identical to `floor-0` with the surface clause replaced by *"Dark blue-gray cracked stone slabs, two or three slightly exaggerated hairline cracks radiating across the diamond face. Chunky block stones with one broken corner and visible aged edges."* All other clauses identical.

**`floor-2` — mossy flagstone:** surface clause *"Dark blue-gray flagstone with moss-green patches creeping in from two corners of the diamond. Chunky block stones, one stone largely covered by muted moss (shape-only, no foliage detail), grout lines clean on the rest of the diamond."*

**`floor-3` — drain / grate:** surface clause *"Dark blue-gray cobblestone diamond with a small iron drain grate recessed in the center. Chunky block stones around the grate, four slotted grate bars in the middle, faint torch-amber glint in the slot shadows."*

**`floor-4` — bloodstain variant:** surface clause *"Dark blue-gray cobblestone diamond with a muted dark-red (#ff6f6f dimmed to #8a3030) stain spreading across the lower half of the diamond. Chunky block stones, stain shape irregular and cartoonish, not photoreal."*

**`floor-hero` — boss/altar room hero floor (detailed shading override):** surface clause *"Dark blue-gray polished cobblestone diamond with a faint etched sigil (shape-only geometric motif, never letterforms) in the center, torch-amber highlights along the grout lines, a single chunky block stone per edge with a slightly raised bevel."* Use `shading: detailed shading`, `detail: high detail` on this slot.

#### 2.2 Wall-face prompts (slots 7–10)

**`wall-face-NW`:**

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, selective outline on tiles, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

An isometric wall face tile rendered in classic stone-catacomb aesthetic, wall face pointing northwest on the iso grid. Chunky block stones stacked in horizontal courses, readable mortar lines between each course, slightly exaggerated cartoonish block bevels. Dark blue-gray stone with faint moss-green accents clinging to the lower courses, torch-amber highlight picking out one crack in the middle course. Seamless edges — the wall face must tile horizontally against another copy of the same tile without visible seams. The bottom 16px of the canvas overlaps the floor-diamond footprint of the cell, the remaining canvas rises above as the wall face.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.
Dungeon biome: stone blue-gray (#24314a / #3c4664), moss green accents, torch-amber highlights in cracks.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing. No visible text or runes that form letterforms. No grass on dungeon/nether/volcano/water biomes.
```

**`wall-face-NE` / `wall-face-SW` / `wall-face-SE`:** identical to `wall-face-NW` with the orientation clause rewritten to *"wall face pointing northeast"* / *"southwest"* / *"southeast"* on the iso grid. All other clauses verbatim.

#### 2.3 Outer-corner prompts (slots 11–14)

**`outer-corner-N`:**

```
[PREAMBLE — verbatim]

An isometric wall outer-corner tile rendered in classic stone-catacomb aesthetic, convex corner piece where two wall faces meet facing north on the iso grid (visual top of the tile). Chunky corner quoin stones stacked in courses, readable mortar lines, slightly exaggerated cartoonish stone bevels. Dark blue-gray stone with a small moss-green patch at the base of the corner, torch-amber highlight picking out the top quoin. The two visible wall faces converge to a single chunky corner stone column. Standalone tile — no tiling requirement. Bottom 16px overlaps the floor-diamond footprint of the cell, remainder rises above.

[PALETTE CLAUSE + DUNGEON SUB-CLAMP — verbatim]

[NEGATIVE-PROMPT CLAUSE + TILE RIDER — verbatim]
```

**`outer-corner-E` / `-S` / `-W`:** identical with the orientation rewritten to *"facing east (visual right)"* / *"facing south (visual bottom)"* / *"facing west (visual left)"*.

#### 2.4 Inner-corner prompts (slots 15–18)

**`inner-corner-N`:**

```
[PREAMBLE — verbatim]

An isometric wall inner-corner tile rendered in classic stone-catacomb aesthetic, concave corner piece (inside of a room) facing north on the iso grid. Chunky block stones converge into a recessed corner with the mortar line reading as a shallow V-seam at the back of the corner, slightly exaggerated cartoonish bevels. Dark blue-gray stone with moss-green in the shadowed recess, a faint torch-amber glint on one raised stone. The two wall faces recede away from the viewer into the inner corner. Standalone tile — no tiling requirement. Bottom 16px overlaps the floor-diamond footprint of the cell, remainder rises above.

[PALETTE CLAUSE + DUNGEON SUB-CLAMP — verbatim]

[NEGATIVE-PROMPT CLAUSE + TILE RIDER — verbatim]
```

**`inner-corner-E` / `-S` / `-W`:** identical with orientation substitution.

#### 2.5 T-junction prompts (slots 19–22)

**`t-junction-N`:**

```
[PREAMBLE — verbatim]

An isometric wall T-junction tile rendered in classic stone-catacomb aesthetic, T-shaped intersection where a wall branches off to the north (visual up) on the iso grid. Chunky block stones in horizontal courses across the main wall, the branching wall meeting it at a perpendicular joint with a single chunky keystone at the intersection. Readable mortar lines, slightly exaggerated cartoonish stone bevels, dark blue-gray with moss-green accents at the joint base and torch-amber highlight on the keystone. Standalone tile. Bottom 16px overlaps the floor-diamond footprint of the cell, remainder rises above.

[PALETTE CLAUSE + DUNGEON SUB-CLAMP — verbatim]

[NEGATIVE-PROMPT CLAUSE + TILE RIDER — verbatim]
```

**`t-junction-E` / `-S` / `-W`:** identical with orientation substitution.

#### 2.6 Cross-junction prompt (slot 23)

**`cross-junction`:**

```
[PREAMBLE — verbatim]

An isometric wall cross-junction tile rendered in classic stone-catacomb aesthetic, four-way intersection where walls meet from all four iso directions. Chunky block stones converge into a central keystone column, four perpendicular wall segments meeting at the center with readable mortar joints. Slightly exaggerated cartoonish stone bevels, dark blue-gray with moss-green at the base of each segment and torch-amber highlight on the central keystone. Standalone tile. Bottom 16px overlaps the floor-diamond footprint of the cell, remainder rises above.

[PALETTE CLAUSE + DUNGEON SUB-CLAMP — verbatim]

[NEGATIVE-PROMPT CLAUSE + TILE RIDER — verbatim]
```

#### 2.7 Stairs prompts (slots 24–27)

**`stairs-up-N` (64 × 128):**

```
[PREAMBLE — verbatim]

An isometric stairs-up tile rendered in classic stone-catacomb aesthetic, stone steps ascending with treads facing north (visual up) on the iso grid. Four to six chunky cobblestone steps stacked upward, each tread slightly exaggerated in depth for cartoonish readability, readable mortar joints between courses. Dark blue-gray stone with moss-green at the lowest tread's edges and torch-amber highlight picking out the topmost tread's front edge. Tall tile — bottom 32px overlaps the floor-diamond footprint of the cell, remaining ~96px rises above as the ascending flight. Standalone tile.

[PALETTE CLAUSE + DUNGEON SUB-CLAMP — verbatim]

[NEGATIVE-PROMPT CLAUSE + TILE RIDER — verbatim]
```

**`stairs-up-E`:** identical with *"treads facing east (visual right)"*.

**`stairs-down-S` (64 × 64):**

```
[PREAMBLE — verbatim]

An isometric stairs-down tile rendered in classic stone-catacomb aesthetic, stone steps descending into the floor with treads facing south (visual down) on the iso grid. Four to six chunky cobblestone steps recessed downward into the diamond footprint, each tread slightly exaggerated in depth for cartoonish readability, readable mortar joints. Dark blue-gray stone with moss-green at the edges of the topmost tread and shadow deepening toward the bottom steps. Upper 32px of the canvas transparent; the diamond-shaped staircase is recessed into the lower 32px. Standalone tile.

[PALETTE CLAUSE + DUNGEON SUB-CLAMP — verbatim]

[NEGATIVE-PROMPT CLAUSE + TILE RIDER — verbatim]
```

**`stairs-down-W`:** identical with *"treads facing west (visual left)"*.

#### 2.8 Door-arch prompts (slots 28–30)

**`door-arch-NW` (64 × 128):**

```
[PREAMBLE — verbatim]

An isometric door-arch tile rendered in classic stone-catacomb aesthetic, archway cut into a northwest-facing wall on the iso grid. Chunky block stones framing a rounded arch opening, iron-banded dark wooden door set slightly recessed within the arch, visible iron rivets and a heavy iron ring handle. Keystone at the top of the arch picks up a torch-amber highlight, moss-green at the base stones. Slightly exaggerated cartoonish stone bevels and iron band thickness for readability. Tall tile — bottom 32px overlaps the floor-diamond footprint of the cell, remaining ~96px rises above as the arch and wall. Standalone tile.

[PALETTE CLAUSE + DUNGEON SUB-CLAMP — verbatim]

[NEGATIVE-PROMPT CLAUSE + TILE RIDER — verbatim]
```

**`door-arch-NE` / `door-arch-SW`:** identical with orientation substitution (*"northeast-facing wall"* / *"southwest-facing wall"*).

### 3. Per-Biome Substitution Table

The slot grid (§1) and prompt skeleton (§2) are **identical across all 8 biomes**. Only the **biome palette hook** and **motif hook** substitute. The mechanism: in any Dungeon prompt above, replace the dungeon-specific motif phrasing with the biome's motif column, and replace the dungeon sub-clamp line with the biome's palette hook column.

| Biome | Palette Hook (replaces Dungeon sub-clamp line) | Motif Hook (replaces `"Dark blue-gray cobblestone + moss + iron bands + torch-amber"` vocabulary) |
|---|---|---|
| `dungeon` | *"Dungeon biome: stone blue-gray (#24314a / #3c4664), moss green accents, torch-amber highlights in cracks."* | Dark blue-gray cobblestone + moss at corners + faint rune etchings (shape-only) + cracked slabs + iron-banded wooden doors. **Cartoonish descriptors: chunky block stones, slightly exaggerated crack lines, readable mossy edges.** |
| `dungeon_dark` | *"Dungeon dark biome: near-black (#0f1117) base, deeper shadow (#1a1f2e), cold blue highlight (#3c4664) only, no warm accents, no moss."* | Near-black charred cobblestone + deep shadow recesses + cold-blue cracks only + iron-banded black doors. **Desaturated and darker than Dungeon — reads as sealed lightless catacomb; no torch-amber, no green.** |
| `cathedral` | *"Cathedral biome: pale marble (#c8c0b0) base, warm gold (#f5c86b) trim, stained-glass blue (#8ed6ff) accents, candle-amber glow."* | Pale marble slabs + gold-trim inlay + angel / winged motif reliefs (shape-only, geometric wing shapes, never letterforms) + ornate arches + bronze-banded light-wood doors. **Cartoonish descriptors: chunky marble blocks, slightly exaggerated gold trim thickness, readable carved relief shapes.** |
| `nether` | *"Nether biome: charcoal black base (#1a0d12), blood red (#ff6f6f) accents, sulfurous orange (#d47a20) glow in fissures, sickly green (#6b8a4a) drip."* | Charred / bleeding stone + purple-red rune etchings (shape-only) + dripping sickly green + fissure-cracks glowing orange + blackened-iron doors. **Cartoonish descriptors: chunky volcanic-black stones, slightly exaggerated glow-crack lines, readable drip shapes.** |
| `sky_temple` | *"Sky temple biome: pale cloud-white (#ecf0ff) base, sky blue (#8ed6ff) veining, gold (#f5c86b) trim, marble."* | White marble slabs + cloud-mist wisps at corners (shape-only) + gold-trim arches + sky-blue cracks + gold-banded white-wood doors. **Cartoonish descriptors: chunky marble blocks, slightly exaggerated gold-trim thickness, readable cloud-wisp shapes.** |
| `volcano` | *"Volcano biome: obsidian black (#1a1a20) base, molten orange/red (#d47a20 / #ff6f6f) cracks, ash-gray (#6b6f7a) ridges, rusted metal brown."* | Obsidian slabs + glowing lava cracks + ash ridges at edges + rusted-metal doors with red-hot rivets. **Cartoonish descriptors: chunky obsidian blocks, slightly exaggerated lava-crack glow, readable ash-ridge shapes.** |
| `water` | *"Water biome: deep aquamarine (#2a5a6e) base, pale cyan (#8ed6ff) foam, algae green (#4a7a4a) accents, wet stone sheen."* | Wet dark stones with rippled surface + algae-green patches at edges + seaweed tendrils (shape-only) + barnacle-crusted bronze doors. **Cartoonish descriptors: chunky wet stones, slightly exaggerated ripple highlights, readable algae patches.** |
| `town` | *"Town biome: warm wood browns (#5a3a20), cobbled path gray (#6b6f7a), green grass (#4a7a4a) patches, brass-gold (#f5c86b) trim."* | Wooden plank floors or cobbled paths + market-stall cloth accents (shape-only) + grass patches at corners + brass-banded wooden doors. **Cartoonish descriptors: chunky wood planks, slightly exaggerated plank grain, readable cobble shapes. Town biome walls are building exteriors (plaster + timber frame), not catacomb stone.** |

**Dungeon-dark distinctness note.** `dungeon_dark` uses the same *slot grid and motif family* as Dungeon, but the palette hook is a hard override: no moss-green, no torch-amber, cold-blue only, deeper near-black base. The visual read is "sealed / abandoned / lightless" vs Dungeon's "active / torchlit / lived-in." The biome sub-clamp line above is the load-bearing differentiator; if a generated `dungeon_dark` tile includes any moss-green or torch-amber, it is a re-gen.

### 4. Per-Slot Prompt Skeleton for the Other 7 Biomes

Rather than author 7 × 30 = 210 additional prompts, the pipeline uses a single fill-in-the-blank template. Take any Dungeon prompt from §2, swap the two hooks, keep everything else verbatim:

```
[PREAMBLE — verbatim, §1 of prompt-templates.md]

[SLOT DESCRIPTION — verbatim from §2 Dungeon prompt, with the Dungeon motif vocabulary replaced by the biome's MOTIF HOOK from §3].

[PALETTE CLAUSE — verbatim, §1c of prompt-templates.md]
[BIOME PALETTE HOOK — from §3 substitution table, replacing the Dungeon sub-clamp line]

[NEGATIVE-PROMPT CLAUSE — verbatim, §1 of prompt-templates.md] No visible text or runes that form letterforms. No grass on dungeon/nether/volcano/water biomes.
```

**Worked example — Cathedral `floor-0`** (applying the template to the Dungeon `floor-0` prompt from §2.1):

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, selective outline on tiles, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

An isometric floor diamond tile rendered in classic stone-catacomb aesthetic. Pale marble slabs with gold-trim inlay, chunky marble blocks fitted together in the diamond shape with readable grout lines between them. Clean tile seam at the diamond edges for seamless grid tiling. Slightly exaggerated edge bevels give the marble slabs a cartoonish readable weight, gold-trim picking out one slab's edge with candle-amber glow. Seamless tile edges — the diamond must tile in a 2×2 grid without visible seams. Transparent background where the diamond does not cover (upper 32px of the 64×64 canvas fully transparent).

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.
Cathedral biome: pale marble (#c8c0b0) base, warm gold (#f5c86b) trim, stained-glass blue (#8ed6ff) accents, candle-amber glow.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing. No visible text or runes that form letterforms. No grass on dungeon/nether/volcano/water biomes.
```

The template is proven: the slot prompt structure survives the substitution intact; only the motif phrasing (`"Dark blue-gray cobblestone … moss … torch-amber"` → `"Pale marble slabs with gold-trim inlay … marble blocks … candle-amber"`) and the biome sub-clamp line change. Every other byte of the prompt is the same Dungeon prompt.

### 5. Download + Extraction Recipe

Flat per-biome layout under `assets/tiles/<biome>/`. No separate `walls/` / `floors/` subdirectories — every slot lives at the biome root. Slot ID is the filename stem.

```
assets/tiles/
  dungeon/
    floor-0.png         floor-1.png         floor-2.png
    floor-3.png         floor-4.png         floor-hero.png
    wall-face-NW.png    wall-face-NE.png    wall-face-SW.png    wall-face-SE.png
    outer-corner-N.png  outer-corner-E.png  outer-corner-S.png  outer-corner-W.png
    inner-corner-N.png  inner-corner-E.png  inner-corner-S.png  inner-corner-W.png
    t-junction-N.png    t-junction-E.png    t-junction-S.png    t-junction-W.png
    cross-junction.png
    stairs-up-N.png     stairs-up-E.png
    stairs-down-S.png   stairs-down-W.png
    door-arch-NW.png    door-arch-NE.png    door-arch-SW.png
    metadata.json
  dungeon_dark/         # same 30 files + metadata.json
  cathedral/            # same
  nether/               # same
  sky_temple/           # same
  volcano/              # same
  water/                # same
  town/                 # same — "wall-face-*" are building exteriors, not catacomb
```

**Rationale for flat layout (not `walls/` + `floors/`):** The tile atlas is consumed by a single Godot `TileSet` resource per biome. Keeping all 30 tiles in one directory matches how Godot builds TileSets (one source per texture) and simplifies the `metadata.json` consumer. Subdivision into `walls/` / `floors/` adds no value and costs one import-path indirection per slot.

**Per-biome `metadata.json`:** auto-authored by the ART-12 implementation alongside the sprite downloads. Shape:

```json
{
  "biome": "dungeon",
  "seed": <locked-biome-seed>,
  "generated_at": "2026-04-17T…",
  "slot_count": 30,
  "tiles": {
    "floor-0": { "width": 64, "height": 64, "tiles_2x2": true },
    "wall-face-NW": { "width": 64, "height": 64, "tiles_horizontally": true },
    "…": "…"
  }
}
```

**Download protocol:** use `curl --fail -o assets/tiles/<biome>/<slot-id>.png "<pixellab_url>"` per [art-lead CLAUDE.md guidance](../../.claude/agents/art-lead/CLAUDE.md). Verify file sizes > 0 and dimensions match the slot spec (`sips -g pixelWidth -g pixelHeight`) before committing.

### 6. Delete-Before-Regen Hook

Per [asset-inventory.md §Bucket D](asset-inventory.md): commit 1 of the ART-12 redraw PR **deletes every path currently under `assets/tiles/<biome>/`** before any new tile is generated. Git preserves the v1 art in history. The delete sweep covers:

- `assets/tiles/dungeon/` — 7 files
- `assets/tiles/dungeon_dark/` — 12 files
- `assets/tiles/cathedral/` — 6 files (no walls)
- `assets/tiles/nether/` — 12 files
- `assets/tiles/sky_temple/` — 12 files
- `assets/tiles/volcano/` — 12 files
- `assets/tiles/water/` — 6 files (no walls)
- `assets/tiles/town/` — 7 files

All removed in commit 1. Commit 2 of the PR lands the regenerated 240-tile atlas + 8 `metadata.json` files. Commit 3 (or bundled with 2) fixes scene-file references (`dungeon.tscn`, `town.tscn`) to point at the new slot IDs.

### 7. Seamless-Tiling Verification

PixelLab does not guarantee seamless tiling output — the prompt clauses and `seed` lock reduce drift but do not eliminate it. Every ART-12 PR MUST include a manual seam check as a PR-checklist item:

**Floor tile seam check:**
1. Drop the generated `floor-0.png` into a test Godot scene.
2. Compose 4 copies in a 2×2 grid using a TileMapLayer with `TileShape = Isometric`, `TileSize = (64, 32)`.
3. Zoom to 100% and look at the four-tile junction. Seams MUST be invisible (no color banding, no outline break, no pattern discontinuity across the diamond edge).

**Wall-face tile seam check:**
1. Drop the generated `wall-face-NW.png` into a test Godot scene.
2. Compose 3 copies horizontally along a wall run on the TileMapLayer.
3. Seams between adjacent wall tiles MUST be invisible (no mortar-line break, no course-height mismatch, no color step).

Any visible seam is a re-gen trigger — regenerate with a different `seed` or tighter prompt clause until the seam passes. Corner / junction / stair / door slots have **no tiling requirement** (they are standalone) and skip this check.

## Acceptance Criteria

- [ ] Copy-paste any Dungeon slot prompt from §2 into PixelLab's `create_map_object` → get a valid tile matching the slot's canvas spec.
- [ ] Copy-paste the §4 substitution template for any biome → get a valid tile in that biome's palette, visually distinct from the Dungeon exemplar.
- [ ] All 30 Dungeon slots (§1 slot grid) have complete prompts in §2 (6 floors + 4 wall faces + 4 outer corners + 4 inner corners + 4 T-junctions + 1 cross + 2 stairs-up + 2 stairs-down + 3 door arches = 30).
- [ ] Zero IP-violating terms in any prompt — no game titles, studio names, franchise character/spell names. Only "stone-catacomb aesthetic" / "classic dungeon-crawler palette" / "classic iso RPG genre" phrasing.
- [ ] Wall slots (faces × 4 + outer corners × 4 + inner corners × 4 + T-junctions × 4 + cross × 1 = 17 pieces) cover every iso-adjacency case the SPEC-ISO-01 tile atlas produces when placing walls on a diamond grid.
- [ ] `dungeon_dark` palette hook is a strict override of the Dungeon sub-clamp — a side-by-side test render of `dungeon/floor-0` and `dungeon_dark/floor-0` reads as visually distinct biomes (no moss-green, no torch-amber in dungeon_dark).
- [ ] Per-biome `seed` is locked at first tile and reused across all 30 slots for that biome — `metadata.json` records it.
- [ ] ART-12 PR commit 1 deletes every path listed in §6 before any new tile is generated.
- [ ] Seamless-tiling verification (§7) is a PR-checklist item for every ART-12 batch.
- [ ] Flat per-biome layout under `assets/tiles/<biome>/` — no `walls/` / `floors/` subdirectories.

## Implementation Notes

- **Tool is `create_map_object`, not `create_isometric_tile`** — [prompt-templates.md §3 `TILE-ISO-ATLAS`](prompt-templates.md) explains why (the iso-tile tool produces 3D-block-style Habbo/SimCity output, not flat-faced dimetric stone).
- **`seed` lock is the single most important consistency lever.** Pick the seed from the first successful `floor-0` of a biome, record in `metadata.json`, reuse for every other slot in that biome. Drift between tiles comes mostly from seed drift, not prompt drift.
- **Hero-biome shading override.** `cathedral`, `sky_temple`, `volcano` use `detailed shading` + `high detail`. All other biomes use `medium shading` + `medium detail`. This is the one PixelLab-param variation across biomes; everything else (tool, view, outline, ai_freedom) is identical.
- **Hero floor variant (`floor-hero`) is also `detailed shading` override** regardless of biome — it's the boss-room / altar-room floor and reads as more ornate than the base cobble.
- **Town biome is the lone outlier** on motif: walls are building exteriors (plaster + timber frame + market-stall cloth), not catacomb stone. The slot grid still applies (same 30 slots); only the motif vocabulary diverges. Level-design consumers of `town/wall-face-*` should treat them as shopfront faces.
- **SE door-arch intentionally omitted.** Iso cameras almost never face a southeast-facing door; adding it doubles authoring cost for rare use. If level-design needs it during ART-12, add as a 31st slot and note in the biome's `metadata.json`.
- **`metadata.json` is authored by the ART-12 implementation**, not pre-populated by this spec. Shape is fixed in §5.

## Open Questions

None at lock time.
