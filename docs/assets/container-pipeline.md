# Container Pipeline — Loot Container Sprites (ART-SPEC-05)

## Summary

Locks the full generation recipe for loot container sprites (Jar / Crate / Chest) implementing the two-state visual contract from [docs/systems/loot-containers.md](../systems/loot-containers.md) (SPEC-LOOT-01). Extends the `OBJ-ISO` block from [prompt-templates.md](prompt-templates.md) (ART-SPEC-01 v2). Spec strategy: author the **Dungeon biome deeply** as the exemplar (complete prompts for every closed + open state across all 3 types, plus one worked variant per type), then rely on runtime `Modulate` tint for per-biome palette variance rather than authoring a full per-biome sprite matrix (keeps asset count manageable; biome tint alone is enough visual variance for container props).

All prompts use `create_map_object` with hand-authored canvas + mask dimensions per `OBJ-ISO`. All prompts follow [prompt-templates.md §11 IP Protection](prompt-templates.md) — zero named-IP invocation, genre-generic container framing (dungeon container, adventurer's chest, pottery jar), cartoonish pixel-art, stone-catacomb framing only.

Consumed by: ART-15 (container sprite implementation). Preceded by a future Bucket F delete sweep from [asset-inventory.md §Bucket F](asset-inventory.md) — bucket is currently empty, so the first regeneration creates; subsequent iterations follow delete-first protocol. Engine binding: [docs/systems/iso-rendering.md](../systems/iso-rendering.md) SPEC-ISO-01 (bottom-center anchor), [docs/systems/loot-containers.md](../systems/loot-containers.md) SPEC-LOOT-01 (state-swap on loot).

## Current State

**Spec status: DRAFT (2026-04-17).** Awaiting product-owner review.

No container sprites exist in the repo. [asset-inventory.md §Bucket F](asset-inventory.md) is a placeholder awaiting this spec's output. `assets/objects/containers/` directory does not exist yet — the ART-15 PR creates it.

The `Container.cs` scene (LOOT-01, not yet authored) consumes these sprites via a simple `Sprite2D.texture` swap on loot-trigger (§6 below). No per-biome sprite variants are authored — runtime `Modulate` tint handles biome color variance per the `OBJ-ISO` block's palette clamp.

## Design

### 1. Container Family Matrix (3 types × 2 states × 2 variants)

All container sprites follow this locked matrix. Canvas and anchor rules are inherited from `OBJ-ISO` §3 in [prompt-templates.md](prompt-templates.md).

| Type | State | Canvas (w × h) | Anchor | File path |
|---|---|---|---|---|
| Jar | closed | 64 × 64 | bottom-center (x=32, y=64) | `jar_closed.png` |
| Jar | closed (variant) | 64 × 64 | bottom-center | `jar_closed_v2.png` |
| Jar | open | 64 × 64 | bottom-center | `jar_open.png` |
| Jar | open (variant) | 64 × 64 | bottom-center | `jar_open_v2.png` |
| Crate | closed | 64 × 64 | bottom-center | `crate_closed.png` |
| Crate | closed (variant) | 64 × 64 | bottom-center | `crate_closed_v2.png` |
| Crate | open | 64 × 64 | bottom-center | `crate_open.png` |
| Crate | open (variant) | 64 × 64 | bottom-center | `crate_open_v2.png` |
| Chest | closed | 64 × 64 | bottom-center (x=32, y=64) | `chest_closed.png` |
| Chest | closed (variant) | 64 × 64 | bottom-center | `chest_closed_v2.png` |
| Chest | open | 64 × 96 | bottom-center (x=32, y=96) | `chest_open.png` |
| Chest | open (variant) | 64 × 96 | bottom-center | `chest_open_v2.png` |

**Asset count lock: 6 required sprites minimum (3 types × 2 states), 12 with 2 variants per slot for floor-level visual variety.**

**Canvas rationale:**
- **Jar = 64×64** (small prop — occupies lower half of canvas, sits on one tile diamond).
- **Crate = 64×64** (standard short prop per `OBJ-ISO` — occupies lower ~90% of canvas).
- **Chest closed = 64×64** (standard short prop — lid-down silhouette fits within 64-tall canvas).
- **Chest open = 64×96** (taller canvas because the raised lid extends above the base — the extra 32px holds the upright lid without shrinking the base). Anchor stays bottom-center; extra height extends upward per §6 state-swap rule.

**Bottom-center anchor (inherited from `OBJ-ISO`).** Every container sprite places the base of the container at canvas bottom-center so the footprint aligns with the top vertex of the tile diamond. Sprite import offset: `Sprite2D.offset = Vector2(0, -(H/2) - 16)` (see `OBJ-ISO` derived-offset formula). For 64×64 → offset = `(0, -48)`. For 64×96 chest-open → offset = `(0, -64)`.

**Per-biome tint — no per-biome sprite variants.** Container sprites are authored against the universal palette clamp only (deep blue-gray / warm gold / neutral wood-brown). Biome tint is applied at runtime via `Sprite2D.Modulate` on the `Container.cs` scene when the floor biome is known. This is a deliberate trade: authoring 6 × 8 biomes = 48 sprites before variants is not worth the per-biome visual fidelity for a container prop; tint is good enough.

### 2. Closed-State Prompt Skeleton

Extends `OBJ-ISO` §Fill-in-the-blank skeleton from [prompt-templates.md](prompt-templates.md). Fill-in slots per container type:

**Jar (closed):**
- **Material axis:** `ceramic` / `clay` / `glazed` / `rough unglazed` / `fired-earthenware`.
- **Shape variants:** `fat-belly round jar` / `tall-slim amphora-style jar` / `wide-mouth open-neck jar` / `sealed-top stoppered jar with cork or wax seal`.
- **Defining feature:** subtle glaze-sheen highlight / hairline cracks (decorative, not broken) / rope binding around the neck / carved shape-only decoration around the belly (no letterforms).
- **State phrase:** `intact and closed, standing upright, unbroken`.

**Crate (closed):**
- **Material axis:** `dark wood` / `pale pine` / `iron-banded heavy oak` / `rope-tied planked crate`.
- **Shape variants:** `standard cube crate (square planks)` / `slatted crate with gaps between planks` / `reinforced crate with iron corner brackets`.
- **Defining feature:** nailed plank seams / iron corner brackets / diagonal cross-plank bracing / rope wrapping across the lid.
- **State phrase:** `intact and closed, lid sealed on top, all planks straight and unbroken`.

**Chest (closed):**
- **Material axis:** `dark stained wood with iron bands` / `oak with brass lock` / `bound with chains` / `carved with rune-shapes (non-letter, shape-only)`.
- **Shape variants:** `domed-lid chest` / `flat-lid chest` / `reinforced chest with ornate metal corners`.
- **Defining feature:** heavy iron lock plate on the front / brass-trim corners / rivets along the top band / warm gold (#f5c86b) trim on the lock or handles.
- **State phrase:** `intact and closed, lid down, lock visible on the front, weighty and grounded`.

### 3. Open-State Prompt Skeleton

Extends `OBJ-ISO` §Fill-in-the-blank skeleton. Explicit rules for how "open" reads in-engine as a "you already looted this" marker:

**Jar (open) — broken / shattered:**
- **State phrase:** `broken and looted, ceramic shards scattered around the base, bottom half of the jar remaining intact on the ground as a stump, cracks radiating outward`.
- **Footprint rule:** scattered shards stay within the 64×64 canvas footprint — no shards extend past the tile diamond; the player must still read "a container was here" when looking at the tile.
- **Contents:** no visible loot inside (loot already taken); interior reads as dark / empty.

**Crate (open) — splintered / lid-off:**
- **State phrase:** `lid off and askew, leaning against the base at an angle OR lying flat next to the crate, splintered edges where the lid was pried open, interior visible from above as a dark cavity`.
- **Footprint rule:** lid positioned within the 64×64 canvas — may lean or tilt but does not extend past the tile diamond footprint.
- **Contents:** no visible loot; interior dark cavity reads as "emptied".

**Chest (open) — lid raised, interior visible:**
- **State phrase:** `lid raised upright, hinges visible at the back, interior visible as a dark cavity with a faint warm ambient glow suggesting the moment-after-loot, base of the chest unchanged from the closed state`.
- **Canvas rule:** canvas is **64×96** (not 64×64) to accommodate the raised lid. Base of the chest (bottom 48–56px of canvas) matches the closed-state silhouette; raised lid extends upward into the top 40–48px of canvas.
- **Contents:** interior is dark + faint warm glow (torch-amber accent, `#f5c86b` muted). No gold coins or gems rendered — loot already taken; the glow is an afterimage.

### 4. Full Copy-Paste Prompts — Base Dungeon Biome

9 concrete prompts: 6 required (3 types × 2 states) + 3 worked variants (1 per type). All open with the universal preamble verbatim; all close with the palette clause + negative-prompt clause verbatim. Authored against `OBJ-ISO` defaults (`view: high top-down`, `outline: single color outline`, `shading: medium shading`, `detail: medium detail`).

#### Prompt 1 — Jar, Closed (base)

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A dungeon pottery jar rendered in cartoonish isometric pixel-art style, intact and closed, standing upright, unbroken. Fired-earthenware ceramic with a subtle glaze-sheen highlight on the shoulder, fat-belly round shape with a narrow neck and a small sealed-top stopper. Hairline cracks as decoration only (not broken), neutral warm-brown clay tone with cool shadow on the underside. Object sits at canvas bottom-center, occupies the lower ~90% of the canvas, transparent background, no ground shadow baked in.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing. No background terrain, no ground, no floor tile — transparent PNG only.
```

#### Prompt 2 — Jar, Open (broken, base)

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A dungeon pottery jar rendered in cartoonish isometric pixel-art style, broken and looted. Ceramic shards scattered around the base, bottom half of the jar remaining intact on the ground as a stump, cracks radiating outward from the break line. Fired-earthenware ceramic, neutral warm-brown clay, dark interior cavity visible inside the remaining stump reading as emptied. All shards stay within the tile footprint — nothing extends past the canvas. Object sits at canvas bottom-center, occupies the lower ~90% of the canvas, transparent background, no ground shadow baked in.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing. No background terrain, no ground, no floor tile — transparent PNG only.
```

#### Prompt 3 — Jar, Closed (variant: tall-slim amphora)

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A dungeon pottery jar rendered in cartoonish isometric pixel-art style, intact and closed, standing upright, unbroken. Glazed ceramic in a tall-slim amphora-style shape with narrow base, elongated body, and a wide sealed top stopper. Rope binding wrapped twice around the neck as a carrying strap. Cool dark-blue glaze with subtle shape-only carved decoration around the belly (no letterforms). Object sits at canvas bottom-center, occupies the lower ~90% of the canvas, transparent background, no ground shadow baked in.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing. No background terrain, no ground, no floor tile — transparent PNG only.
```

#### Prompt 4 — Crate, Closed (base)

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A dungeon wooden crate rendered in cartoonish isometric pixel-art style, intact and closed, lid sealed on top, all planks straight and unbroken. Dark wood standard cube crate with square planks, nailed plank seams running top-to-bottom, iron corner brackets on the four vertical edges with rivets. Neutral wood-brown with cool shadow on the lower-facing planes, iron hardware in gunmetal gray. Object sits at canvas bottom-center, occupies the lower ~90% of the canvas, transparent background, no ground shadow baked in.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing. No background terrain, no ground, no floor tile — transparent PNG only.
```

#### Prompt 5 — Crate, Open (splintered, lid-off, base)

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A dungeon wooden crate rendered in cartoonish isometric pixel-art style, opened and looted. Dark wood standard cube crate with the lid off and askew, leaning against the base at a shallow angle, splintered edges where the lid was pried open. Interior visible from above as a dark empty cavity — no loot inside. Iron corner brackets still intact on the base, one or two nails visibly bent from the pry. Neutral wood-brown with cool shadow on the lower-facing planes, interior cavity rendered as deep shadow. Object sits at canvas bottom-center, occupies the lower ~90% of the canvas, transparent background, no ground shadow baked in.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing. No background terrain, no ground, no floor tile — transparent PNG only.
```

#### Prompt 6 — Crate, Closed (variant: slatted, rope-tied)

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A dungeon wooden crate rendered in cartoonish isometric pixel-art style, intact and closed, lid sealed on top, all planks straight and unbroken. Pale pine slatted crate with visible gaps between the vertical slats revealing a dark interior. Rope wrapping crosswise over the top lid as a seal, knotted once on the front face. Diagonal cross-plank bracing on the visible sides. Pale wood tone with cool shadow in the gaps, hemp-beige rope. Object sits at canvas bottom-center, occupies the lower ~90% of the canvas, transparent background, no ground shadow baked in.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing. No background terrain, no ground, no floor tile — transparent PNG only.
```

#### Prompt 7 — Chest, Closed (base)

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

An adventurer's dungeon treasure chest rendered in cartoonish isometric pixel-art style, intact and closed, lid down, lock visible on the front, weighty and grounded. Dark stained wood with heavy iron bands wrapping the top, front, and corners, a prominent iron lock plate centered on the front face, rivets along the top band. Warm gold (#f5c86b) trim on the lock faceplate and the two front corner accents. Domed-lid shape reads as solid and heavy. Object sits at canvas bottom-center, occupies the lower ~90% of the canvas, transparent background, no ground shadow baked in.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing. No background terrain, no ground, no floor tile — transparent PNG only.
```

#### Prompt 8 — Chest, Open (lid raised, base — 64×96 canvas)

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

An adventurer's dungeon treasure chest rendered in cartoonish isometric pixel-art style, opened and looted. Lid raised fully upright, hinges visible at the back of the lid, interior visible as a dark empty cavity with a faint warm torch-amber (#f5c86b) ambient glow along the inner rim suggesting the moment-after-loot. No coins or gems inside — already taken. Base of the chest (dark stained wood with iron bands and the iron lock plate, warm gold corner accents) unchanged from the closed state. Canvas is taller than standard to accommodate the raised lid extending upward. Object sits at canvas bottom-center, base occupies the lower half of the canvas and the raised lid occupies the upper half, transparent background, no ground shadow baked in.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing. No background terrain, no ground, no floor tile — transparent PNG only.
```

#### Prompt 9 — Chest, Closed (variant: rune-carved, chain-bound)

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

An adventurer's dungeon treasure chest rendered in cartoonish isometric pixel-art style, intact and closed, lid down, lock visible on the front, weighty and grounded. Dark stained wood carved with non-letter rune-shapes (pure shape-only geometric glyphs — triangles, circles, branching lines, no letterforms) across the front face and lid. Heavy iron chain wrapping once around the body horizontally, locked at the front with a heavy padlock. Flat-lid shape with ornate metal corners. Cold muted palette with faint cool-blue rune glow hint on two of the carved glyphs, gold (#f5c86b) trim restrained to the padlock faceplate only. Object sits at canvas bottom-center, occupies the lower ~90% of the canvas, transparent background, no ground shadow baked in.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing. No background terrain, no ground, no floor tile — transparent PNG only.
```

### 5. State-Pair Visual Consistency

Closed and open states must read as a **clearly paired before/after** for the same container type. The pairing contract:

- Closed and open of the **same type** share the same material axis (a dark-wood closed chest must have a dark-wood open chest; a fat-belly clay jar must have a fat-belly clay jar stump when broken).
- Closed and open of the **same variant** share the same defining features where applicable (a rope-tied crate's open state shows the rope cut / discarded; a rune-carved chest's open state keeps the rune carvings visible on the base).
- Silhouette overlap: for 64×64 → 64×64 state pairs (jar, crate), the base of the open sprite must sit in the same vertical band of the canvas as the closed sprite. The player must recognize at a glance that this is the same container, now looted.

When authoring variant pairs beyond the 9 prompts above, keep the material + variant axis consistent across the state pair.

### 6. Loot-State-Transition Convention (Container.cs contract)

When `Container.cs` (future LOOT-01 implementation) is triggered on loot-open, it performs a texture swap:

1. Current sprite is `closed.png` (e.g., `jar_closed.png`).
2. On open: `Sprite2D.Texture = load("res://assets/objects/containers/<type>_open.png")`.
3. Position stays identical (same `Node2D.Position` on the iso grid). The `Area2D` interact zone is disabled so the container can't be re-opened.
4. The open prop persists on the floor until the floor unloads (SPEC-LOOT-01 §Spawn placement §4). Then it is freed with the rest of the floor.

**Anchor stability across state swap.** Both closed and open textures are authored with bottom-center as the canvas anchor. The `Sprite2D.Offset` on the scene is authored **once** for the closed sprite and must still place the open sprite correctly:

- **Jar closed 64×64 → Jar open 64×64**: identical canvas; anchor identical; offset unchanged. No work required at swap time.
- **Crate closed 64×64 → Crate open 64×64**: identical canvas; anchor identical; offset unchanged. No work required at swap time.
- **Chest closed 64×64 → Chest open 64×96**: canvas grows by +32px vertically. Because the anchor is **bottom-center**, the extra height extends **upward** — the base of the chest stays on the same tile; the raised lid rises above. The scene's `Sprite2D.Offset.y` must update on swap:
  - Closed offset: `Vector2(0, -48)` (for 64×64 canvas, `-(64/2) - 16 = -48`).
  - Open offset: `Vector2(0, -64)` (for 64×96 canvas, `-(96/2) - 16 = -64`).
  - `Container.cs` must apply the new offset simultaneously with the texture swap. This is the single place the implementation is not a pure texture swap.

### 7. Asset Directory Layout

Target layout (created by ART-15 PR, does not exist yet):

```
assets/objects/
└── containers/
    ├── jar_closed.png
    ├── jar_closed_v2.png
    ├── jar_open.png
    ├── jar_open_v2.png
    ├── crate_closed.png
    ├── crate_closed_v2.png
    ├── crate_open.png
    ├── crate_open_v2.png
    ├── chest_closed.png
    ├── chest_closed_v2.png
    ├── chest_open.png          # 64×96 canvas
    ├── chest_open_v2.png       # 64×96 canvas
    └── metadata.json
```

**metadata.json schema:**

```json
{
  "containers": {
    "jar": {
      "closed": {
        "default": "jar_closed.png",
        "variants": ["jar_closed_v2.png"],
        "canvas": { "w": 64, "h": 64 },
        "sprite_offset": { "x": 0, "y": -48 }
      },
      "open": {
        "default": "jar_open.png",
        "variants": ["jar_open_v2.png"],
        "canvas": { "w": 64, "h": 64 },
        "sprite_offset": { "x": 0, "y": -48 }
      }
    },
    "crate": {
      "closed": {
        "default": "crate_closed.png",
        "variants": ["crate_closed_v2.png"],
        "canvas": { "w": 64, "h": 64 },
        "sprite_offset": { "x": 0, "y": -48 }
      },
      "open": {
        "default": "crate_open.png",
        "variants": ["crate_open_v2.png"],
        "canvas": { "w": 64, "h": 64 },
        "sprite_offset": { "x": 0, "y": -48 }
      }
    },
    "chest": {
      "closed": {
        "default": "chest_closed.png",
        "variants": ["chest_closed_v2.png"],
        "canvas": { "w": 64, "h": 64 },
        "sprite_offset": { "x": 0, "y": -48 }
      },
      "open": {
        "default": "chest_open.png",
        "variants": ["chest_open_v2.png"],
        "canvas": { "w": 64, "h": 96 },
        "sprite_offset": { "x": 0, "y": -64 }
      }
    }
  }
}
```

The `sprite_offset` is what `Container.cs` reads on state swap to apply the correct `Sprite2D.Offset` (per §6 anchor stability rule). Variants are randomly selected at floor-generation time for visual variety.

### 8. Delete-Before-Regen Hook

Per [asset-inventory.md §Bucket F](asset-inventory.md) — bucket is **empty** at spec-lock time. First regeneration under ART-15 creates the `assets/objects/containers/` directory and all sprites from scratch — no delete step required on the first run.

**Subsequent iterations** (v2 regenerations, variant additions, material-axis expansions) follow the standard delete-first protocol:

1. Delete `assets/objects/containers/<target>.png` + `<target>.png.import` before regenerating.
2. Run the PixelLab prompt from §4.
3. Place the new sprite at the same path.
4. Verify `metadata.json` entries are unchanged (unless canvas size changed — e.g., if a future chest-open variant uses 64×128 instead of 64×96, update the `canvas` + `sprite_offset` fields in the same PR).

### 9. IP-Clean Check

Every prompt in §4 uses genre-generic language only:

- "dungeon container" / "pottery jar" / "wooden crate" / "adventurer's dungeon treasure chest"
- Zero named-IP references (no game titles, no character names, no franchise markers).
- "Non-letter rune-shapes" / "shape-only geometric glyphs" — explicit negative against letterform rendering (defensive against PixelLab's tendency to render text on carved surfaces, per [prompt-templates.md §1](prompt-templates.md) negative-prompt rationale).
- Warm gold + torch-amber accent colors are generic dungeon-crawler palette, not lifted from any specific title.

Container props sit comfortably inside the "generic dungeon furniture" IP-clean zone. No additional sensitivity beyond the universal negative-prompt clause.

### 10. Acceptance Criteria

1. **9 copy-paste prompts produce 9 distinct valid sprites.** Every prompt in §4 runs end-to-end through `create_map_object` and returns a pixel-clean PNG at the declared canvas size with the container fully inside the canvas and nothing extending past the tile footprint.
2. **Closed + open states read as clearly paired.** For each container type, a viewer can glance at the closed sprite next to the open sprite and immediately identify them as the same container type in before/after states (same material, same variant features, consistent silhouette overlap per §5).
3. **Canvas anchors match `OBJ-ISO` §3 anchor rule from ART-SPEC-01.** Every sprite places the container base at canvas bottom-center; the derived `Sprite2D.Offset` formula produces the values in §7 `metadata.json` `sprite_offset` entries.
4. **Asset directory layout matches spec.** `assets/objects/containers/` contains all 12 PNGs plus `metadata.json` at the paths in §7.
5. **State-swap contract enforced.** `Container.cs` implementation (future LOOT-01) reads `metadata.json` `sprite_offset` on state transition and applies it alongside the texture swap per §6.
6. **Palette clamp satisfied.** Every sprite passes the §1c palette-subset check from [prompt-templates.md](prompt-templates.md) — neutral wood/ceramic/iron tones plus warm gold (#f5c86b) accents only, no out-of-clamp pixels.
7. **IP-clean check passes per §9.** No named-IP invocation, no letterform rendering on carved surfaces.

## Open Questions

None at lock time.
