# High-DPI / Retina Scaling

## Summary

Pixel-art games need crisp integer-multiple scaling so individual pixels stay visible. This spec locks the scaling strategy: **canvas-items mode + integer-only scale factors + letterbox black bars when the screen doesn't divide cleanly**. SPEC-UI-HIGH-DPI-01 (Phase H). Depends on [SPEC-UI-FONT-01](font.md) — bitmap fonts require the same integer-scale discipline.

## Current State

**Spec status: LOCKED** via SPEC-UI-HIGH-DPI-01 (Phase H). The project currently uses Godot's default scaling behavior (stretch mode `disabled`) — this spec changes it to `canvas_items` + `integer` scale factor.

## Design

### The problem with non-integer scaling on pixel art

Pixel art relies on crisp 1-pixel-thick edges. When a 16-pixel-wide sprite is drawn at 1.5× zoom, each source pixel occupies 1.5 destination pixels — which is impossible, so the GPU bilinearly interpolates, blurring the edges into gradient fringes. The visual result is fuzzy sprites that betray the pixel-art aesthetic.

Integer scaling (1×, 2×, 3×, 4×) keeps each source pixel mapping to an exact NxN block on the output, preserving crisp edges. This is non-negotiable for pixel art.

### Locked scaling strategy

**Godot project settings:**

| Setting | Value | Reason |
|---------|-------|--------|
| `display/window/stretch/mode` | `canvas_items` | Scales 2D render (sprites + UI) together; preserves aspect ratio automatically. |
| `display/window/stretch/aspect` | `keep` | Maintains designed aspect ratio regardless of window size; letterbox on mismatch. |
| `display/window/stretch/scale_mode` | `integer` | Forces integer-multiple scale — the crisp-pixel guarantee. |
| `display/window/stretch/scale` | `1.0` | Base scale; integer mode rounds up from here. |
| `rendering/textures/canvas_textures/default_texture_filter` | `nearest` | No bilinear blur on texture sampling. |

**Design resolution:** 1280 × 720 (the canvas size the game is authored for). The game targets this as the "1×" reference; larger windows render at 2× / 3× / etc.

**Integer-scale behavior at common display sizes:**

| Display | Scale applied | Rendered canvas | Letterbox |
|---------|---------------|-----------------|-----------|
| 1280 × 720 (authoring target) | 1× | 1280 × 720 | None |
| 1920 × 1080 | 1× | 1280 × 720 centered | Black bars: 320 left + 320 right, 180 top + 180 bottom |
| 2560 × 1440 (1440p) | 2× | 2560 × 1440 | None |
| 3840 × 2160 (4K) | 3× | 3840 × 2160 | None |
| 2880 × 1800 (Retina 15") | 2× | 2560 × 1440 centered | Small letterbox: 160 each side, 180 top/bottom |

**Why letterbox beats cropping or stretching:**
- Cropping cuts content — players lose HUD elements at odd aspect ratios.
- Stretching makes non-integer scale — returns the blur problem.
- Letterbox preserves every pixel of the authoring canvas exactly, at the cost of unused screen edges. Players with huge monitors see the game floated in black; it's the honest answer.

### UI text + integer-scale alignment

Press Start 2P (per [font.md](font.md)) is an 8-pixel bitmap font. At 1× design scale, an 8-px character occupies 8 physical pixels. At 3× (4K), it's 24 physical pixels. Both are crisp because the scale factor is an integer.

If this spec EVER allows non-integer scaling, the font ladder in `font.md` breaks (the 8-px Small tooltip size would fuzz at 1.5×). The two specs are locked together.

### Window modes

- **Windowed:** respects the integer-scale constraint — window snaps to 1280×720, 2560×1440, 3840×2160, etc. If the player resizes to a non-integer-friendly size, Godot's letterbox kicks in.
- **Fullscreen:** picks the largest integer scale that fits the monitor; letterbox the rest. No exclusive-fullscreen special casing; borderless-windowed fullscreen is the mode (matches modern convention).
- **Maximize:** treats like a resize — picks largest integer scale that fits.

### Settings impact

- **A "window size" setting in Options menu** (per-monitor choices). Exposes 1×, 2×, 3× options only — no fractional options.
- **A "fullscreen toggle"** that picks the largest integer scale automatically.
- **A "pixel-perfect" hint** in the Options UI: "Stretching pixel art to non-integer sizes blurs the art. This setting is locked to keep the visual crisp." — prevents user confusion about why non-integer options don't exist.

---

## Acceptance Criteria

- [ ] Godot project settings locked to canvas_items + integer + keep-aspect + nearest filter per the table above.
- [ ] Design resolution is 1280×720; UI layouts in the Pause Menu, HUD, etc. all fit in that space.
- [ ] At 1440p, 4K, and common Retina sizes, the game renders crisp (no blur) at integer scale with letterbox when needed.
- [ ] Options menu exposes window-size choices limited to integer multiples only.
- [ ] Fullscreen mode uses borderless-windowed with largest-fitting integer scale.
- [ ] No spec downstream (HUD layout, splash, menus) assumes a non-integer scale factor.

## Implementation Notes

- **Project settings file:** `project.godot`. Set `display/window/stretch/*` values per the table.
- **Per-scene overrides:** don't add any. The whole game uses the same scaling rule — inconsistent per-scene scaling causes tearing between screens.
- **Testing at small window sizes:** below 1280×720 (if the user forces a tiny window), the game renders at 1× with clipping. This is acceptable per PO direction — 1280×720 is the minimum supported resolution; sub-minimum is best-effort.
- **FloatingText scale:** [FloatingText.cs](../../scripts/ui/FloatingText.cs) uses a font-size override; confirm at 1× + 2× + 3× that the text renders crisp (no half-pixel Y offsets from damage-number jitter).

## Open Questions

None — spec is locked.
