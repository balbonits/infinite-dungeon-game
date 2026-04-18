# Projectile Pipeline (ART-SPEC-07) — 9-Projectile True-Iso Redraw

## Summary

Family-specific extension of [`ART-SPEC-01`](prompt-templates.md) block `PROJ-ISO-8DIR` (commit `5e9e70f`), covering the 9 projectile sprites that currently ship in `assets/projectiles/`. Each projectile is a single `{name}_8dir.png` atlas, 256×32, laid out as 8 horizontal frames of 32×32 at canvas-center anchor. This spec locks the atlas format, the PixelLab generation strategy (Option C — rotate-for-symmetric / per-direction for asymmetric), a copy-paste prompt for each of the 9 projectiles, a concrete Pillow compositing recipe for the 8-directional asymmetric cases, and the delete-before-regen list per [`asset-inventory.md`](asset-inventory.md) Bucket H.

This spec does **not** define projectile behavior (speed, damage, visual effects) — that lives in skill specs and `Projectile.cs`. This spec only controls the pixel output and file layout so `Sprite2D.Hframes = 8 + auto frame-index-by-angle` in `Projectile.cs:47-50` continues to work unchanged.

Binding rules from ART-SPEC-01: the universal preamble (§1), the palette clause (§1c), the negative-prompt clause (§1), the 2×32 canvas contract (§1a), the cartoonish style note (§11 IP Protection), and the iso rotation table (§1a). This spec never overrides them.

## Current State

9 projectile sprites exist under `assets/projectiles/` (committed 2026-04-16), each a 256×32 horizontal 8-frame strip. They were authored pre-iso-pivot under v1's low-top-down assumption and share the perspective mismatch that SPEC-ISO-01 / ART-SPEC-01 are redrawing across the whole repo. Projectiles are arguably the *least* broken family in the asset inventory — because `Projectile.cs` rotates (single-sprite case) or selects-by-angle (multi-frame case), the shipped art functions in-engine. But the visual style (silhouette weight, palette, cartoonish chunk) does not match the new ART-SPEC-01 direction, and the atlas dimensions assumed a 32-px projectile at v1's `view: "low top-down"`, which is not true 2:1 dimetric. All 9 get redrawn.

No `metadata.json` exists for the current atlases — dimensions live only in the PNG headers. This spec adds a `metadata.json` per atlas at regen time.

## Atlas Format Contract

**File layout:** `assets/projectiles/{name}_8dir.png`, dimensions **256 × 32**, 8 frames laid horizontally, each frame **32 × 32**. No vertical rows. No padding between frames. Transparent background.

**Frame order (engine-authoritative, derived from `Projectile.cs:49`):**

```
Frame index:   0      1       2      3       4      5       6      7
Direction:     east   south-  south  south-  west   north-  north  north-
                      east                   west                  east
Screen angle:  0°     45°     90°    135°    180°   225°    270°   315°
```

This is **east-indexed**, not north-indexed. Frame 0 is the projectile traveling screen-right (east). The projectile picker in `Projectile.cs:47-50` is:

```csharp
sprite.Hframes = frameCount;                                 // 8
float angle = (direction.Angle() + Mathf.Tau) % Mathf.Tau;   // 0..2π, 0 = east
sprite.Frame = (int)Mathf.Round(angle / (Mathf.Tau / 8)) % 8;
```

At `angle = 0` (screen-east), `Frame = 0`. At `angle = π/2` (screen-south, Godot screen-Y is down-positive), `Frame = 2`. At `angle = π` (screen-west), `Frame = 4`. The 8-way mapping is locked — do not introduce a different ordering for new projectiles.

**Alignment with `DirectionalSprite.cs:50-62`:** character sprites use the same east-indexed `angle / 45°` mapping, so projectile frame order matches character rotation order. Authors labeling individual direction PNGs before compositing should use PixelLab's 8 rotation names (`east`, `south-east`, `south`, `south-west`, `west`, `north-west`, `north`, `north-east`) in that exact order.

**Anchor:** canvas center (x=16, y=16 within each 32×32 frame). Projectiles fly through the air — they have no ground contact, so the character family's bottom-center anchor rule does **not** apply. `Sprite2D.offset = Vector2(0, 0)` for every projectile (centered), which is what `Projectile.cs` already assumes (no per-sprite offset code).

**Canvas fill:** projectile silhouette occupies ~60–85% of each 32×32 frame, centered in both axes. Leave 2–4 px of transparent margin on all sides to prevent rotation clipping when the engine future-proofs additional in-flight effects. The arrow's longest axis (tip to fletching) is the governing dimension — up to 28 px for long-and-straight arrows, ~20 px for compact orbs.

## PixelLab Generation Strategy

PixelLab's `create_character` tool generates 8-direction rotations in a single job, but its output is a per-direction PNG set (not a pre-composited horizontal atlas) and the tool is tuned for humanoid bipedal subjects. Applying it to a 32-px projectile is likely to produce inconsistent rotations (the humanoid skeleton is not projectile-shaped). `create_map_object` is the correct tool for projectiles per ART-SPEC-01 §1a projectile row, but it produces one image per call — no built-in 8-dir batch.

Three options evaluated:

- **Option A (per-direction × 8):** call `create_map_object` 8 times per projectile with direction-specific prompts, composite the 8 outputs into a 256×32 horizontal strip via a Pillow script. Works for every projectile. Cost: 8 generations × 9 projectiles = 72 generations. Authoring time per projectile: ~8 min PixelLab + ~1 min compositing.

- **Option B (single-source + programmatic rotation × 8):** author one PixelLab sprite, rotate it 7 more times in Pillow to fill the atlas. Works for **rotationally-symmetric** projectiles (a flame orb looks the same from any angle). Fails for **asymmetric** projectiles — an arrow rotated 45° CCW in pixel space does not correctly depict the arrow as seen from the iso NE direction; perspective foreshortening and fletching visibility change per angle. Cost: 1 generation × symmetric-projectile count. Authoring time: ~2 min per projectile.

- **Option C (mixed — recommended [rec]):** classify each projectile as symmetric or asymmetric; use Option B for symmetric, Option A for asymmetric. Minimizes PixelLab spend without sacrificing visual correctness on directional projectiles.

### Per-projectile symmetry classification

| Projectile | Symmetric? | Strategy | Generations |
|---|---|---|---|
| `arrow_8dir` | ❌ Asymmetric (shaft direction, fletching-at-back) | Option A | 8 |
| `magic_arrow_8dir` | ❌ Asymmetric (same arrow geometry, glowing) | Option A | 8 |
| `stone_spike_8dir` | ❌ Asymmetric (pointed tip, direction matters) | Option A | 8 |
| `lightning_8dir` | ❌ Asymmetric (forked tail trails behind travel direction) | Option A | 8 |
| `frost_bolt_8dir` | ❌ Asymmetric (jagged ice shard, elongated point forward) | Option A | 8 |
| `fireball_8dir` | ✅ Symmetric (compact orb, embers trail all sides equally) | Option B | 1 |
| `magic_bolt_8dir` | ✅ Symmetric (raw energy orb, glow is radial) | Option B | 1 |
| `energy_blast_8dir` | ✅ Symmetric (rotating gold orb) | Option B | 1 |
| `shadow_bolt_8dir` | ✅ Symmetric (dark orb with red accent core) | Option B | 1 |

**Total PixelLab generations: (5 × 8) + (4 × 1) = 44 generations for the full 9-projectile regen.**

**Symmetric-projectile rotation note (Option B).** For the 4 symmetric projectiles, PixelLab generates one 32×32 frame (any direction prompt — recommend "south" for consistency). The compositing script duplicates this frame 8 times into the strip without rotation, since the sprite genuinely reads the same in every iso direction. Do **not** Pillow-rotate the frame 45° per slot — at 32 px, rotation introduces anti-aliasing artifacts that violate the "pixel-perfect, no anti-aliasing" preamble rule. The 8 identical frames are correct because the engine still needs 8 slots for `Projectile.cs:47-50` to index into.

**Asymmetric-projectile per-direction generation (Option A).** Generate 8 separate PixelLab calls, one per direction. Each prompt names the direction explicitly (see §Per-projectile prompts). The compositing script concatenates the 8 outputs in the east-indexed order locked by the atlas format contract.

## Per-Projectile Prompt Skeletons

Every prompt follows this structure, inheriting from `PROJ-ISO-8DIR` §468–478:

```
[PREAMBLE (verbatim §1 of ART-SPEC-01)]

A [PROJECTILE-NAME] rendered in cartoonish isometric pixel-art style, shown in flight.
[MATERIAL/EFFECT token]. [LENGTH/SHAPE token]. [COLOR-BEAT token].
Projectile fills most of the 32×32 canvas, oriented for the [DIRECTION] iso rotation,
transparent background.

[PALETTE CLAUSE (verbatim §1c of ART-SPEC-01)]

[NEGATIVE-PROMPT CLAUSE (verbatim §1 of ART-SPEC-01)]
No background, no hand gripping the projectile, no ground shadow.
```

For symmetric projectiles (Option B), the `[DIRECTION]` token is fixed to `south` and the prompt is run once. For asymmetric projectiles (Option A), the prompt is run 8 times with `[DIRECTION]` cycling through `east`, `south-east`, `south`, `south-west`, `west`, `north-west`, `north`, `north-east` in the east-indexed atlas order.

## Full Copy-Paste Prompts (All 9 Projectiles)

The preamble, palette clause, and negative-prompt clause below are **verbatim** from ART-SPEC-01 §1 / §1c / §1. Never edit them. Edit only the asset-specific paragraph between them.

### 1. `arrow_8dir` — Ranger Primary (Option A, 8 directions)

Run this prompt 8 times, cycling `[DIRECTION]` through the east-indexed order. Direction-specific orientation hints in the parenthetical help PixelLab produce geometrically distinct rotations rather than 8 visually-identical outputs.

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

An arrow rendered in cartoonish isometric pixel-art style, shown in flight. Wooden shaft stained dark brown with a gray-steel arrowhead and two-feather fletching at the nock. Long and straight, roughly 28 pixels tip-to-nock, thick chunky silhouette (not a thin line). Steel tip picks up a subtle warm gold (#f5c86b) highlight; fletching is warm brown-gray. Projectile fills most of the 32×32 canvas, oriented for the [DIRECTION] iso rotation (tip pointing in the screen-[DIRECTION] direction of travel, fletching trailing at the opposite end), transparent background.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing. No background, no hand gripping the projectile, no ground shadow.
```

### 2. `magic_arrow_8dir` — Ranger Magical Ammo (Option A, 8 directions)

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A magical arrow rendered in cartoonish isometric pixel-art style, shown in flight. Arrow geometry — thick chunky shaft with fletching — but the shaft glows with soft player-blue (#8ed6ff) magical energy, and the arrowhead is a tapered crystalline tip emitting a faint blue halo. Long and straight, roughly 28 pixels tip-to-nock. Energy glow wraps the shaft in a soft 1-pixel bloom (kept simple — not a trail). Projectile fills most of the 32×32 canvas, oriented for the [DIRECTION] iso rotation (tip pointing in the screen-[DIRECTION] direction of travel, fletching trailing), transparent background.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing. No background, no hand gripping the projectile, no ground shadow.
```

### 3. `magic_bolt_8dir` — Mage Primary (Option B, 1 generation × 8 duplicates)

Run once with `[DIRECTION]=south`, then duplicate the output into all 8 atlas slots.

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A magic bolt rendered in cartoonish isometric pixel-art style, shown in flight. Compact round orb of raw magical energy, roughly 16 pixels across, with a bright blue-purple (#8ed6ff core fading into violet) inner core and a darker purple (#5c4a9c) corona. Chunky bold silhouette — not wispy — with 3 short arcane sparks radiating outward in the cardinal directions (up, down, left, right). Orb is rotationally symmetric in appearance. Projectile fills most of the 32×32 canvas, centered, transparent background.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing. No background, no hand gripping the projectile, no ground shadow.
```

### 4. `fireball_8dir` — Mage Fire Mastery (Option B, 1 generation × 8 duplicates)

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A fireball rendered in cartoonish isometric pixel-art style, shown in flight. Compact chunky orb of flame, roughly 18 pixels across, with a bright warm-gold (#f5c86b) core, a mid-orange body, and 3 exaggerated flame tongues curling outward at 120° intervals (top, lower-left, lower-right). A few trailing embers in warm amber scattered in the margin — kept sparse, not a tail. Bold cartoonish flame shapes — not realistic wispy fire. Projectile fills most of the 32×32 canvas, centered, transparent background.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing. No background, no hand gripping the projectile, no ground shadow.
```

### 5. `frost_bolt_8dir` — Mage Water/Ice (Option A, 8 directions)

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A frost bolt rendered in cartoonish isometric pixel-art style, shown in flight. Jagged angular shard of ice, roughly 22 pixels long, elongated teardrop pointed forward toward the direction of travel, broader crystalline back. Bright blue accent (#8ed6ff) highlights on the leading tip and edges, deeper cyan (#4aa4c8) body, pale near-white (#ecf0ff) inner core. Chunky crystal-faceted silhouette — not wispy — with 2–3 small ice chip fragments trailing behind. Projectile fills most of the 32×32 canvas, oriented for the [DIRECTION] iso rotation (pointed tip leading the screen-[DIRECTION] direction of travel, chip fragments trailing at the opposite end), transparent background.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing. No background, no hand gripping the projectile, no ground shadow.
```

### 6. `lightning_8dir` — Mage Air (Option A, 8 directions)

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A lightning bolt rendered in cartoonish isometric pixel-art style, shown in flight. Crackling zigzag bolt of electricity, roughly 26 pixels tip-to-tail, forward-pointing jagged tip with 2 short forked branches trailing behind in the opposite direction. Bright yellow-white (#fff6c2) hot core, pale yellow (#f5c86b) outer glow, with 2–3 small spark flecks in the margin. Bold chunky zigzag silhouette — not a thin line — with clearly defined segments. Projectile fills most of the 32×32 canvas, oriented for the [DIRECTION] iso rotation (forward tip pointing screen-[DIRECTION], forked tail trailing opposite), transparent background.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing. No background, no hand gripping the projectile, no ground shadow.
```

### 7. `stone_spike_8dir` — Mage Earth (Option A, 8 directions)

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A stone spike rendered in cartoonish isometric pixel-art style, shown in flight. Pointed rock projectile, roughly 24 pixels long, elongated teardrop with a sharp pointed tip forward and a broader jagged base. Earthy brown (#6b5130) and mid-gray (#7a7a82) faceted stone, with darker #3c4664 crevices between facets and lighter gray highlights on the leading edges. Chunky hand-hewn silhouette — clearly a chiseled rock, not a smooth crystal — with 2 small pebble fragments trailing behind. Projectile fills most of the 32×32 canvas, oriented for the [DIRECTION] iso rotation (pointed tip leading screen-[DIRECTION], pebble fragments trailing opposite), transparent background.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing. No background, no hand gripping the projectile, no ground shadow.
```

### 8. `energy_blast_8dir` — Mage Aether/Overcharge (Option B, 1 generation × 8 duplicates)

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

An energy blast rendered in cartoonish isometric pixel-art style, shown in flight. Rotating compact orb of raw golden energy, roughly 20 pixels across, with a bright warm-gold (#f5c86b) hot core, a concentric outer ring of softer amber, and 4 short arcane sparks radiating in the diagonal directions (upper-left, upper-right, lower-left, lower-right) giving the impression of rotation. Chunky bold silhouette — not wispy. A few scattered gold motes in the margin. Projectile fills most of the 32×32 canvas, centered, transparent background.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing. No background, no hand gripping the projectile, no ground shadow.
```

### 9. `shadow_bolt_8dir` — Mage Shadow/Dark-Magic (Option B, 1 generation × 8 duplicates)

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A shadow bolt rendered in cartoonish isometric pixel-art style, shown in flight. Compact orb of dark-magic energy, roughly 18 pixels across, with a dark purple-black (#1a0f2e) core, a deep-violet (#3a1f5c) body, and a single danger-red (#ff6f6f) pinpoint accent at the exact center of the core — the red-eye-in-darkness silhouette. Wispy dark purple curls escape the orb in 3 directions (asymmetric wisps emphasize the corrupt energy motif, not rotationally uniform sparks). Chunky bold silhouette — the darkness reads solid even at 32 px. Projectile fills most of the 32×32 canvas, centered, transparent background.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing. No background, no hand gripping the projectile, no ground shadow.
```

## Compositing Recipe (Pillow)

After PixelLab generations complete, use the following Python/Pillow script to build the `{name}_8dir.png` atlas. Save this under `scripts/art/composite_projectile.py` (separate commit — code change is out of scope for this doc-only spec ticket). The recipe is documented here so the script is reproducible from the spec alone.

```python
#!/usr/bin/env python3
"""
Composite 8 direction PNGs into a single 256x32 horizontal atlas.

Usage:
    # Asymmetric (Option A) — 8 distinct PNGs in east-indexed order
    python composite_projectile.py \
        --name arrow \
        --mode asymmetric \
        --inputs east.png south-east.png south.png south-west.png \
                 west.png north-west.png north.png north-east.png \
        --out assets/projectiles/arrow_8dir.png

    # Symmetric (Option B) — single PNG duplicated 8 times
    python composite_projectile.py \
        --name fireball \
        --mode symmetric \
        --inputs fireball_source.png \
        --out assets/projectiles/fireball_8dir.png

Each input frame must be exactly 32x32 RGBA. The script validates size
and fails loud on mismatch — no silent resize. Output is a 256x32 RGBA
horizontal strip with no padding between frames.
"""

import argparse
from pathlib import Path
from PIL import Image

FRAME = 32
FRAMES = 8
ATLAS_W = FRAME * FRAMES  # 256
ATLAS_H = FRAME            # 32

def load_and_validate(path: Path) -> Image.Image:
    im = Image.open(path).convert("RGBA")
    if im.size != (FRAME, FRAME):
        raise SystemExit(
            f"{path}: expected {FRAME}x{FRAME}, got {im.size}. "
            f"Re-generate from PixelLab at width=32 height=32."
        )
    return im

def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--name", required=True)
    ap.add_argument("--mode", choices=["symmetric", "asymmetric"], required=True)
    ap.add_argument("--inputs", nargs="+", required=True, type=Path)
    ap.add_argument("--out", required=True, type=Path)
    args = ap.parse_args()

    if args.mode == "symmetric":
        if len(args.inputs) != 1:
            raise SystemExit("symmetric mode requires exactly 1 input")
        src = load_and_validate(args.inputs[0])
        frames = [src] * FRAMES
    else:  # asymmetric
        if len(args.inputs) != FRAMES:
            raise SystemExit(
                f"asymmetric mode requires exactly {FRAMES} inputs "
                f"in east-indexed order: east, south-east, south, "
                f"south-west, west, north-west, north, north-east"
            )
        frames = [load_and_validate(p) for p in args.inputs]

    atlas = Image.new("RGBA", (ATLAS_W, ATLAS_H), (0, 0, 0, 0))
    for i, frame in enumerate(frames):
        atlas.paste(frame, (i * FRAME, 0))

    args.out.parent.mkdir(parents=True, exist_ok=True)
    atlas.save(args.out)
    print(f"wrote {args.out} ({ATLAS_W}x{ATLAS_H})")

if __name__ == "__main__":
    main()
```

**Validation after compositing:** every atlas must report `pixelWidth: 256, pixelHeight: 32` via `sips -g pixelWidth -g pixelHeight assets/projectiles/{name}_8dir.png`. Any deviation is a regen.

**No rotation, no resampling.** The script deliberately does not rotate or resize. PixelLab must produce 32×32 frames directly; rotation in pixel space at 32 px destroys the pixel-perfect discipline. If a generation comes back at a different size, re-call PixelLab with corrected params — do not scale in Pillow.

## Cartoonish Style Note

Per ART-SPEC-01 §11 IP Protection, the word **cartoonish** is load-bearing in every prompt. It is the primary stylistic differentiator between this pipeline and licensed iso-dungeon-crawler art. For projectiles specifically:

- A cartoonish fireball = compact chunky orb with **3 exaggerated flame tongues** in clear pixel shapes — not a gradient-shaded realistic flickering fire with particle-systems feel.
- A cartoonish arrow = thick visible shaft, clearly-readable fletching with 2–3 feather barbs, steel tip rendered as a distinct triangle — not a hairline shaft with a razor-thin metal point.
- A cartoonish lightning bolt = jagged segmented zigzag with 2 forked branches at chunky pixel resolution — not a wispy electrical arc with glow halos.

Every prompt above includes the phrase **"chunky bold silhouette"** or **"chunky"** / **"bold"** / **"compact"** exactly to nudge PixelLab's output toward the cartoonish end of the spectrum. When a generation comes back looking too smooth / too realistic / too gradient-heavy, the fix is to strengthen those tokens and re-roll, not to accept and down-step the quality bar.

## IP-Clean Verification

Every prompt uses **genre-generic descriptors only**. Prohibited language (and the reason it is prohibited):

- "Diablo-style fireball" / "WoW frostbolt" / "Path of Exile energy blast" — direct IP references.
- "Like the arrow in [any game]" — implicit IP reference, equally prohibited.
- Specific character or spell names from any copyrighted game.
- Proprietary color palettes referenced by brand (e.g., "the Blizzard cyan").

Permitted language (present throughout this spec):

- "classic dungeon-crawler", "classic iso", "genre reference" — structural / proportional reference, not replication.
- "#8ed6ff player blue" / "#f5c86b warm gold" — our palette, derived from `docs/assets/ui-theme.md`, not licensed.
- "compact orb of flame" / "jagged shard of ice" / "crackling zigzag bolt" — generic fantasy archetypes, not IP-specific.

Per ART-SPEC-01 §11, the review checklist for every generated projectile before commit:

1. Does the sprite resemble any specific copyrighted game's projectile beyond genre-archetype? If yes, reject and re-prompt.
2. Does the palette stay inside the §1c clamp plus exempt colors named in this spec (player blue for magic arrow, danger red for shadow bolt, warm gold for energy blast/fireball)? If no, reject.
3. Is the "cartoonish" quality visible — chunky silhouette, bold shapes, no photorealistic gradient? If no, strengthen tokens and regen.

## Asset Directory + Filename Convention

- Location: `assets/projectiles/` (unchanged from current).
- Filename: `{name}_8dir.png` — preserve the 9 existing names exactly. No new names added by this spec.
- Per-atlas companion: `assets/projectiles/{name}_8dir.metadata.json`. Schema:

```json
{
  "name": "arrow",
  "atlas_dimensions": {"width": 256, "height": 32},
  "frame_dimensions": {"width": 32, "height": 32},
  "frame_count": 8,
  "frame_order": [
    "east", "south-east", "south", "south-west",
    "west", "north-west", "north", "north-east"
  ],
  "anchor": "center",
  "generation_strategy": "asymmetric_per_direction",
  "pixellab_tool": "create_map_object",
  "spec": "ART-SPEC-07",
  "prompt_block": "PROJ-ISO-8DIR"
}
```

For symmetric projectiles, set `"generation_strategy": "symmetric_duplicated"`. This metadata is machine-readable — future tooling (validation scripts, automated regen) keys off it.

## Delete-Before-Regen

Per [`asset-inventory.md`](asset-inventory.md) Bucket H, the following 9 files must be deleted from disk before the regen begins. Committing old-and-new side-by-side is forbidden — the regen commit must replace.

```
assets/projectiles/arrow_8dir.png
assets/projectiles/energy_blast_8dir.png
assets/projectiles/fireball_8dir.png
assets/projectiles/frost_bolt_8dir.png
assets/projectiles/lightning_8dir.png
assets/projectiles/magic_arrow_8dir.png
assets/projectiles/magic_bolt_8dir.png
assets/projectiles/shadow_bolt_8dir.png
assets/projectiles/stone_spike_8dir.png
```

Associated `.import` sidecars Godot generates alongside each PNG must also be deleted. The regen PR re-imports fresh.

## Acceptance Criteria

1. All 9 projectiles have complete copy-paste prompts in §Full Copy-Paste Prompts — no fill-in-the-blank remnants (`[TOKEN]` remaining) in any of the 9 final prompts except `[DIRECTION]` for Option A projectiles, which is explicitly parameterized.
2. 8-direction atlas format (256×32, 8 × 32×32, east-indexed order matching `Projectile.cs:47-50`) is documented and implementable without reading engine code.
3. Compositing recipe (Pillow script template) is complete, end-to-end runnable, and validates frame dimensions.
4. Zero IP violations — no copyrighted game/spell/character names referenced in prompts or prose.
5. Every prompt uses the verbatim preamble, verbatim palette clause, and verbatim negative-prompt clause from ART-SPEC-01 §1 / §1c.
6. Symmetry classification decision for each of the 9 projectiles is explicit and justified.
7. `metadata.json` schema is defined and applied to both Option A and Option B outputs.
8. Delete-before-regen list names all 9 files that must be removed before commit.

## Open Questions

None at lock time.
