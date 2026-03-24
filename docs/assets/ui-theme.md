# UI Theme Specification

## Summary

Complete visual theme for all UI elements in the game. Every color, font size, spacing value, and style property is documented here. The theme is ported from the CSS custom properties in the Phaser prototype's `index.html` and adapted for Godot's Control node theming system.

## Current State

The color palette and layout values are fully defined and ported from the Phaser prototype. For the Godot prototype, theme values are applied programmatically via GDScript constants rather than a `.tres` theme resource file. This keeps all styling in one editable location and avoids complex theme resource hand-editing during early development.

## Design

### Color Palette

Ported directly from the CSS `:root` custom properties in `index.html`. Every color is provided in hex, RGBA (0-255 integer), RGBA (0.0-1.0 float), and Godot `Color()` constructor format.

| Name | CSS Variable | Hex | RGBA (0-255) | RGBA (0.0-1.0) | Usage |
|------|-------------|-----|--------------|-----------------|-------|
| bg-0 | `--bg-0` | `#0f1117` | (15, 17, 23, 255) | (0.059, 0.067, 0.090, 1.0) | Darkest background, scene clear color |
| bg-1 | `--bg-1` | `#1b2130` | (27, 33, 48, 255) | (0.106, 0.129, 0.188, 1.0) | Dark background, secondary panels |
| ink | `--ink` | `#ecf0ff` | (236, 240, 255, 255) | (0.925, 0.941, 1.0, 1.0) | Primary text color (stat values, important info) |
| muted | `--muted` | `#b6bfdb` | (182, 191, 219, 255) | (0.714, 0.749, 0.859, 1.0) | Secondary text (labels, descriptions, instructions) |
| accent | `--accent` | `#f5c86b` | (245, 200, 107, 255) | (0.961, 0.784, 0.420, 1.0) | Gold accent (titles, highlights, borders, slash effect) |
| danger | `--danger` | `#ff6f6f` | (255, 111, 111, 255) | (1.0, 0.435, 0.435, 1.0) | Red (danger, high-tier enemies, low HP warning) |
| safe | `--safe` | `#76f79f` | (118, 247, 159, 255) | (0.463, 0.969, 0.624, 1.0) | Green (safe zones, low-tier enemies, full HP) |
| panel-bg | `--panel` | N/A (has alpha) | (22, 27, 40, 191) | (0.086, 0.106, 0.157, 0.75) | Panel background (75% opacity) |
| panel-border | `--panel-border` | N/A (has alpha) | (245, 200, 107, 77) | (0.961, 0.784, 0.420, 0.3) | Panel border (30% opacity gold) |

**Additional colors (not in CSS, derived from Phaser code):**

| Name | Hex | RGBA (0.0-1.0) | Source | Usage |
|------|-----|-----------------|--------|-------|
| player | `#8ed6ff` | (0.557, 0.839, 1.0, 1.0) | `COLORS.player` in JS | Player sprite color |
| enemy-low | `#6bff89` | (0.420, 1.0, 0.537, 1.0) | `COLORS.enemyLow` in JS | Tier 1 enemy color |
| enemy-mid | `#ffde66` | (1.0, 0.871, 0.400, 1.0) | `COLORS.enemyMid` in JS | Tier 2 enemy color |
| enemy-high | `#ff6f6f` | (1.0, 0.435, 0.435, 1.0) | `COLORS.enemyHigh` in JS | Tier 3 enemy color |
| sword | `#f5c86b` | (0.961, 0.784, 0.420, 0.95) | `COLORS.sword` in JS | Slash effect (same as accent, 95% alpha) |
| death-text | `#ffe1b0` | (1.0, 0.882, 0.690, 1.0) | Death screen JS | "You Died" title text (warm light gold) |
| bg-scene | `#131927` | (0.075, 0.098, 0.153, 1.0) | `createBackground()` JS | Scene background fill (Phaser only) |

### Godot Color Constants

All colors as GDScript constants for use in scripts:

```gdscript
# ui_theme.gd -- centralized color constants

# Core UI palette (from CSS custom properties)
const COLOR_BG_0 := Color(0.059, 0.067, 0.090, 1.0)        # #0f1117
const COLOR_BG_1 := Color(0.106, 0.129, 0.188, 1.0)        # #1b2130
const COLOR_INK := Color(0.925, 0.941, 1.0, 1.0)            # #ecf0ff
const COLOR_MUTED := Color(0.714, 0.749, 0.859, 1.0)        # #b6bfdb
const COLOR_ACCENT := Color(0.961, 0.784, 0.420, 1.0)       # #f5c86b
const COLOR_DANGER := Color(1.0, 0.435, 0.435, 1.0)         # #ff6f6f
const COLOR_SAFE := Color(0.463, 0.969, 0.624, 1.0)         # #76f79f
const COLOR_PANEL_BG := Color(0.086, 0.106, 0.157, 0.75)    # rgba(22,27,40,0.75)
const COLOR_PANEL_BORDER := Color(0.961, 0.784, 0.420, 0.3) # rgba(245,200,107,0.3)

# Entity colors (from Phaser COLORS object)
const COLOR_PLAYER := Color(0.557, 0.839, 1.0, 1.0)         # #8ed6ff
const COLOR_ENEMY_LOW := Color(0.420, 1.0, 0.537, 1.0)      # #6bff89
const COLOR_ENEMY_MID := Color(1.0, 0.871, 0.400, 1.0)      # #ffde66
const COLOR_ENEMY_HIGH := Color(1.0, 0.435, 0.435, 1.0)     # #ff6f6f
const COLOR_SWORD := Color(0.961, 0.784, 0.420, 0.95)       # #f5c86b @ 95%

# Special colors
const COLOR_DEATH_TEXT := Color(1.0, 0.882, 0.690, 1.0)     # #ffe1b0
const COLOR_DEATH_OVERLAY := Color(0.0, 0.0, 0.0, 0.75)     # black @ 75%
```

### Font Specification

| Property | Value | Notes |
|----------|-------|-------|
| Family (Phaser) | "Trebuchet MS", "Segoe UI", sans-serif | CSS font-family stack from `index.html` |
| Family (Godot) | System default (Godot built-in) | No custom font for prototype |
| Future font | Custom dark fantasy pixel font | Pixel art style, 8x8 or 16x16 base grid |
| Title size | 13px | HUD panel title ("A Dungeon in the Middle of Nowhere") |
| Body size | 12px | HUD stat text, instructions |
| Death title size | 28px (Phaser) / 48px (Godot) | "You Died" text, larger in Godot for impact |
| Death subtitle size | 20px | Restart instructions |

### HUD Panel Style

The HUD panel is a PanelContainer in the top-left corner of the viewport displaying game title, controls, and stats.

**Phaser CSS source (`#overlay`):**
```css
#overlay {
    position: absolute;
    left: 12px;
    top: 12px;
    z-index: 5;
    background: var(--panel);              /* rgba(22, 27, 40, 0.75) */
    border: 1px solid var(--panel-border); /* rgba(245, 200, 107, 0.3) */
    border-radius: 10px;
    padding: 10px 12px;
    backdrop-filter: blur(5px);
    pointer-events: none;
    min-width: 200px;
}
```

**Godot PanelContainer equivalent:**

| Property | Value | CSS Equivalent |
|----------|-------|---------------|
| Position (anchors) | Top-left, offset (12, 12) from viewport edge | `left: 12px; top: 12px` |
| Min width | 200px | `min-width: 200px` |
| Z-index | 5 (or CanvasLayer) | `z-index: 5` |

**StyleBoxFlat for the PanelContainer:**

| StyleBoxFlat Property | Value | CSS Equivalent |
|----------------------|-------|---------------|
| `bg_color` | `Color(0.086, 0.106, 0.157, 0.75)` | `background: rgba(22, 27, 40, 0.75)` |
| `border_color` | `Color(0.961, 0.784, 0.420, 0.3)` | `border: 1px solid rgba(245, 200, 107, 0.3)` |
| `border_width_left` | 1 | `border: 1px` |
| `border_width_right` | 1 | `border: 1px` |
| `border_width_top` | 1 | `border: 1px` |
| `border_width_bottom` | 1 | `border: 1px` |
| `corner_radius_top_left` | 10 | `border-radius: 10px` |
| `corner_radius_top_right` | 10 | `border-radius: 10px` |
| `corner_radius_bottom_left` | 10 | `border-radius: 10px` |
| `corner_radius_bottom_right` | 10 | `border-radius: 10px` |
| `content_margin_left` | 12 | `padding: 10px 12px` (horizontal) |
| `content_margin_right` | 12 | `padding: 10px 12px` (horizontal) |
| `content_margin_top` | 10 | `padding: 10px 12px` (vertical) |
| `content_margin_bottom` | 10 | `padding: 10px 12px` (vertical) |

**GDScript to create the StyleBoxFlat programmatically:**
```gdscript
func _create_panel_style() -> StyleBoxFlat:
    var style := StyleBoxFlat.new()
    style.bg_color = Color(0.086, 0.106, 0.157, 0.75)
    style.border_color = Color(0.961, 0.784, 0.420, 0.3)
    style.border_width_left = 1
    style.border_width_right = 1
    style.border_width_top = 1
    style.border_width_bottom = 1
    style.corner_radius_top_left = 10
    style.corner_radius_top_right = 10
    style.corner_radius_bottom_left = 10
    style.corner_radius_bottom_right = 10
    style.content_margin_left = 12
    style.content_margin_right = 12
    style.content_margin_top = 10
    style.content_margin_bottom = 10
    return style
```

**Note on backdrop blur:** Phaser's CSS uses `backdrop-filter: blur(5px)` for a frosted glass effect. Godot's Control nodes do not natively support backdrop blur. To approximate this, use a BackBufferCopy node or a custom shader. Deferred for prototype -- the semi-transparent panel background is sufficient.

### Title Label Style

The game title at the top of the HUD panel.

| Property | Value | CSS Equivalent |
|----------|-------|---------------|
| Node type | Label | `<h1>` |
| `text` | "A DUNGEON IN THE MIDDLE OF NOWHERE" | `text-transform: uppercase` applied |
| Font size override | 13 | `font-size: 13px` |
| Font color override | `Color(0.961, 0.784, 0.420, 1.0)` | `color: var(--accent)` / `#f5c86b` |
| Uppercase | Applied via `.to_upper()` on text string or theme override | `text-transform: uppercase` |
| Font weight | Bold (700) | `font-weight: 700` |
| Letter spacing | ~0.78px (0.06em at 13px) | `letter-spacing: 0.06em` |
| Margin bottom | 4px | `margin-bottom: 4px` |

**Note on letter spacing:** Godot's Label node does not have a direct `letter_spacing` property in the same way CSS does. The closest approach is to use a custom font resource with adjusted spacing, or accept the default. For the prototype, default spacing is acceptable.

### Body Text Style

Instruction and stat text lines in the HUD panel.

| Property | Label Type | Value |
|----------|-----------|-------|
| Font size | All | 12 |
| Font color | Instructions/labels | `Color(0.714, 0.749, 0.859, 1.0)` / COLOR_MUTED |
| Font color | Stat values (HP, XP, LVL) | `Color(0.925, 0.941, 1.0, 1.0)` / COLOR_INK |
| Line height | All | 1.35x font size (~16.2px) |
| Margin top (hud section) | Stats line | 6px above stat line (Phaser: `margin-top: 6px` on `#hud`) |

### Death Screen Style

Displayed when player HP reaches 0.

**Overlay:**

| Property | Value | Phaser Equivalent |
|----------|-------|-------------------|
| Node type | ColorRect (full viewport) | `this.add.rectangle(... 0x000000, 0.75)` |
| Size | Full viewport (1920x1080 or current resolution) | 380x160 centered rectangle in Phaser |
| Color | `Color(0.0, 0.0, 0.0, 0.75)` | Black at 75% opacity |
| Z-index | Above all game elements | Added last to scene |

**"You Died" title:**

| Property | Value | Phaser Equivalent |
|----------|-------|-------------------|
| Node type | Label (centered in viewport) | `this.add.text(...)` |
| Text | "You Died" | "You Died\nTap / Press R to restart" |
| Font size | 48 | 28px in Phaser (scaled up for Godot viewport) |
| Font color | `Color(1.0, 0.882, 0.690, 1.0)` / `#ffe1b0` | `color: "#ffe1b0"` |
| Horizontal alignment | Center | `align: "center"` + `setOrigin(0.5)` |
| Vertical alignment | Center of screen, offset -40px (above center) | Centered via `setOrigin(0.5)` |

**Subtitle (restart instructions):**

| Property | Value |
|----------|-------|
| Node type | Label (below title) |
| Text | "Press R to restart" |
| Font size | 20 |
| Font color | COLOR_MUTED / `#b6bfdb` |
| Horizontal alignment | Center |
| Position | Below title, ~20px gap |

**Restart button (optional, for mouse/touch):**

| Property | Value |
|----------|-------|
| Node type | Button |
| Text | "Restart" |
| StyleBoxFlat bg_color | COLOR_ACCENT / `#f5c86b` |
| Font color | COLOR_BG_0 / `#0f1117` (dark text on gold button) |
| Font size | 18 |
| Corner radius (all) | 8 |
| Padding | 12px horizontal, 8px vertical |
| Position | Below subtitle, centered, ~16px gap |

### Background Gradient

The Phaser prototype's HTML body has a multi-layer background gradient:

```css
background:
    radial-gradient(circle at 15% 20%, #2a3550 0%, transparent 45%),
    radial-gradient(circle at 85% 80%, #3d2d1f 0%, transparent 35%),
    linear-gradient(160deg, var(--bg-0), var(--bg-1));
```

**Gradient colors:**
| Layer | Type | Position | Color 1 | Color 2 |
|-------|------|----------|---------|---------|
| 1 (top) | Radial | 15% x, 20% y | `#2a3550` (dark blue) | Transparent at 45% radius |
| 2 (mid) | Radial | 85% x, 80% y | `#3d2d1f` (dark brown) | Transparent at 35% radius |
| 3 (base) | Linear | 160 degrees | `#0f1117` (bg-0) | `#1b2130` (bg-1) |

This background is visible behind the game canvas. In Godot, this could be replicated with:
- A CanvasLayer behind the game world with a TextureRect using a gradient texture, or
- A shader on the viewport background, or
- Simply setting the project clear color to COLOR_BG_0 for the prototype.

For the prototype, a flat `Color(0.059, 0.067, 0.090, 1.0)` clear color is sufficient. The gradient is a polish detail for later.

### Godot Theme Resource Strategy

**Prototype approach (current):**
- All colors and style values live as constants in `ui_theme.gd` (autoloaded singleton or static class).
- StyleBoxFlat objects are created programmatically in `_ready()` and applied via `add_theme_stylebox_override()`.
- Font sizes applied via `add_theme_font_size_override()`.
- Font colors applied via `add_theme_color_override()`.

**Future approach (when UI stabilizes):**
- Create `game_theme.tres` (Godot Theme resource).
- Define all type variations (Label, PanelContainer, Button, etc.) in the .tres file.
- Assign the theme to the root Control node; all children inherit it.
- Reduces per-node scripting and allows visual editing in the Godot editor's Theme Editor.

### Mobile Responsive Notes

The Phaser prototype has a responsive breakpoint at 900px:
```css
@media (max-width: 900px) {
    #app { width: 100vw; height: 100vh; border: 0; border-radius: 0; }
    #overlay { left: 8px; right: 8px; top: 8px; }
    .mobile-note { display: inline; }
}
```

In Godot, responsive UI is handled differently:
- Use anchor presets and stretch modes on Control nodes.
- The HUD PanelContainer should use `ANCHOR_PRESET_TOP_LEFT` with offsets.
- On smaller viewports (mobile exports), reduce font sizes and panel margins.
- Mobile touch controls (virtual joystick) would be shown only when touch input is detected.
- This is deferred for the prototype -- desktop only for now.

## Implementation Notes

- The `ui_theme.gd` script should be added as an Autoload singleton so all scenes can access `UiTheme.COLOR_ACCENT`, etc.
- When creating StyleBoxFlat objects, create them once in `_ready()` and cache them. Do not create new StyleBoxFlat instances every frame.
- Godot's default font is adequate for the prototype. When a custom font is added, load it as a `.tres` FontFile resource and set it via `add_theme_font_override("font", custom_font)`.
- The death screen should be a separate scene (or CanvasLayer) instanced on top of the game scene, not a replacement of the game scene.

## Open Questions

- Should the backdrop blur effect (`backdrop-filter: blur(5px)`) be replicated in Godot, or is the semi-transparent panel sufficient?
- Should there be a "game_theme.tres" from day one, or is the scripted approach better for rapid iteration?
- How should the UI scale on high-DPI displays (Retina, 4K)? Godot has stretch mode settings, but which configuration?
- Should the mobile-responsive breakpoint logic be ported, or is desktop-only fine for the Godot prototype?
- What custom font should be used for the dark fantasy theme? Candidates: "Press Start 2P", "Silkscreen", "Alagard".
