# HUD (Heads-Up Display)

## Summary

An overlay displaying essential player information: HP, XP, level, and current floor. Rendered as Godot Control nodes inside a `CanvasLayer` to ensure it stays on top of the game world and does not move with the camera.

## Current State

Migrating from Phaser 3 prototype to Godot 4:
- **Phaser prototype:** HTML overlay (`#overlay`) positioned absolutely over the canvas with CSS styling
- **Godot implementation:** `CanvasLayer` (layer 10) containing a `Control` node tree with `PanelContainer`, `VBoxContainer`, and `Label` nodes
- Displays: game title, control hints, and stats line (`HP: {hp} | XP: {xp} | LVL: {level} | Floor: {floor}`)
- Stats update reactively via signal from `GameState` autoload
- Click-through enabled (`mouse_filter = MOUSE_FILTER_IGNORE`) so input passes to the game world

## Design

### Display Elements

| Element | Position | Content | Font Color | Font Size |
|---------|----------|---------|------------|-----------|
| Title | Top of panel | "A DUNGEON IN THE MIDDLE OF NOWHERE" | `#f5c86b` (accent gold) | 13px |
| Controls | Below title | "Move: WASD / Arrow keys\nAuto-attack: nearest enemy in range" | `#b6bfdb` (muted) | 12px |
| Stats | Bottom of panel | "HP: {hp} \| XP: {xp} \| LVL: {level} \| Floor: {floor}" | `#ecf0ff` (ink white) | 12px |

### Future HUD Elements

As systems are added, the HUD will expand:
- **HP bar** -- visual health bar (not just a number)
- **XP bar** -- progress toward next level
- **Gold counter** -- current gold amount
- **Minimap** -- small floor overview (if floors become larger than viewport)
- **Status effects** -- icons for active buffs/debuffs
- **Backpack quick-view** -- slot count indicator

### Styling

- Semi-transparent dark panel: `rgba(22, 27, 40, 0.75)` background
- Gold border: `rgba(245, 200, 107, 0.3)`, 1px all sides
- Rounded corners: 10px all corners
- Content margin: 10px all sides
- VBoxContainer separation: 4px between labels
- Panel offset from top-left corner: 12px left, 12px top
- No backdrop blur in Godot (was present in Phaser CSS prototype)

### Accessibility

- **Phaser prototype** used `aria-live="polite"` for screen reader updates
- **Godot** has limited built-in accessibility; Godot's focus system can be used for future keyboard-navigable UI elements
- High contrast text on dark background maintained
- Font sizes remain readable at all supported resolutions

## Implementation Notes

### Node Hierarchy

The complete scene tree for the HUD:

```
UILayer (CanvasLayer, layer=10)
└── HUD (Control, full_rect) [hud.gd]
    │ mouse_filter = MOUSE_FILTER_IGNORE
    └── PanelContainer
        │ anchors: top=0, left=0 (top-left corner)
        │ offsets: left=12, top=12
        │ StyleBoxFlat override:
        │   bg_color = Color(0.086, 0.106, 0.157, 0.75)
        │   border_color = Color(0.961, 0.784, 0.420, 0.3)
        │   border_width = 1 (all sides)
        │   corner_radius = 10 (all corners)
        │   content_margin = 10 (all sides)
        └── VBoxContainer (separation=4)
            ├── TitleLabel (Label)
            │   text = "A DUNGEON IN THE MIDDLE OF NOWHERE"
            │   font_color_override = Color("#f5c86b")
            │   font_size_override = 13
            ├── ControlsLabel (Label)
            │   text = "Move: WASD / Arrow keys\nAuto-attack: nearest enemy in range"
            │   font_color_override = Color("#b6bfdb")
            │   font_size_override = 12
            └── StatsLabel (Label)
                text = "HP: 100 | XP: 0 | LVL: 1 | Floor: 1"
                font_color_override = Color("#ecf0ff")
                font_size_override = 12
```

### Node Details

#### UILayer (CanvasLayer)

| Property | Value | Reason |
|----------|-------|--------|
| `layer` | 10 | Renders on top of all game world nodes (default world is layer 0) |
| Type | `CanvasLayer` | Separates HUD from game world coordinate system; HUD does not move with camera |

#### HUD (Control) -- root of hud.tscn

| Property | Value | Reason |
|----------|-------|--------|
| `layout` | Full Rect | Fills the entire viewport (anchors 0,0 to 1,1) |
| `mouse_filter` | `MOUSE_FILTER_IGNORE` | Clicks pass through to the game world below |
| Script | `hud.gd` | Handles signal connection and stats label updates |

#### PanelContainer

| Property | Value | Reason |
|----------|-------|--------|
| Anchors | top=0, left=0 | Pinned to top-left corner |
| Offset left | 12 | Matches Phaser prototype's `left: 12px` |
| Offset top | 12 | Matches Phaser prototype's `top: 12px` |
| Theme override | `StyleBoxFlat` (see below) | Custom dark panel with gold border |
| `size_flags_horizontal` | None (default) | Panel sizes to fit content, does not stretch |
| `size_flags_vertical` | None (default) | Panel sizes to fit content, does not stretch |

#### StyleBoxFlat (PanelContainer theme override)

| Property | Value | CSS Equivalent |
|----------|-------|----------------|
| `bg_color` | `Color(0.086, 0.106, 0.157, 0.75)` | `rgba(22, 27, 40, 0.75)` |
| `border_color` | `Color(0.961, 0.784, 0.420, 0.3)` | `rgba(245, 200, 107, 0.3)` |
| `border_width_left` | 1 | `border: 1px solid` |
| `border_width_top` | 1 | `border: 1px solid` |
| `border_width_right` | 1 | `border: 1px solid` |
| `border_width_bottom` | 1 | `border: 1px solid` |
| `corner_radius_top_left` | 10 | `border-radius: 10px` |
| `corner_radius_top_right` | 10 | `border-radius: 10px` |
| `corner_radius_bottom_left` | 10 | `border-radius: 10px` |
| `corner_radius_bottom_right` | 10 | `border-radius: 10px` |
| `content_margin_left` | 10 | `padding: 10px 12px` |
| `content_margin_top` | 10 | `padding: 10px 12px` |
| `content_margin_right` | 10 | `padding: 10px 12px` |
| `content_margin_bottom` | 10 | `padding: 10px 12px` |

The StyleBoxFlat can be created either:
1. **In the editor:** Select PanelContainer > Theme Overrides > Styles > Panel > New StyleBoxFlat > set properties
2. **Programmatically in `_ready()`:** Create a `StyleBoxFlat` resource and call `add_theme_stylebox_override("panel", style_box)`

#### VBoxContainer

| Property | Value | Reason |
|----------|-------|--------|
| `theme_override_constants/separation` | 4 | 4px gap between labels |

#### TitleLabel (Label)

| Property | Value |
|----------|-------|
| `text` | `"A DUNGEON IN THE MIDDLE OF NOWHERE"` |
| `theme_override_colors/font_color` | `Color("#f5c86b")` -- accent gold |
| `theme_override_font_sizes/font_size` | 13 |
| `uppercase` | Not needed (text is already uppercase in the string) |

#### ControlsLabel (Label)

| Property | Value |
|----------|-------|
| `text` | `"Move: WASD / Arrow keys\nAuto-attack: nearest enemy in range"` |
| `theme_override_colors/font_color` | `Color("#b6bfdb")` -- muted |
| `theme_override_font_sizes/font_size` | 12 |

#### StatsLabel (Label)

| Property | Value |
|----------|-------|
| `text` | `"HP: 100 | XP: 0 | LVL: 1 | Floor: 1"` (initial/default) |
| `theme_override_colors/font_color` | `Color("#ecf0ff")` -- ink white |
| `theme_override_font_sizes/font_size` | 12 |

### hud.gd Script

```gdscript
extends Control

@onready var stats_label: Label = $PanelContainer/VBoxContainer/StatsLabel

func _ready() -> void:
    mouse_filter = Control.MOUSE_FILTER_IGNORE
    GameState.stats_changed.connect(_on_stats_changed)
    _on_stats_changed()  # Initialize display with current values

func _on_stats_changed() -> void:
    stats_label.text = "HP: %d | XP: %d | LVL: %d | Floor: %d" % [
        GameState.hp, GameState.xp, GameState.level, GameState.floor_number
    ]
```

#### Script Behavior

1. **On `_ready()`:** Connects to `GameState.stats_changed` signal and immediately calls the handler to populate the label with current values.
2. **On `_on_stats_changed()`:** Rebuilds the stats string using `GameState` properties. This fires whenever HP, XP, level, or floor changes.
3. **`mouse_filter`:** Set to `IGNORE` so the entire HUD Control tree does not intercept mouse events. This is critical -- without it, the HUD would block clicks/taps on the game world.

#### Signal Flow

```
GameState.take_damage()   ──┐
GameState.award_xp()      ──┤── emit stats_changed signal
GameState.reset()          ──┘
                               │
                               v
                    hud.gd._on_stats_changed()
                               │
                               v
                    StatsLabel.text = "HP: ..."
```

### CanvasLayer Rendering

- **Layer 10** ensures the HUD renders on top of all game world nodes (player, enemies, floor tiles, effects)
- The `CanvasLayer` has its own coordinate system independent of the game camera. When the camera follows the player, the HUD stays fixed on screen.
- If additional UI layers are added later (e.g., pause menu at layer 20, death screen at layer 15), they can be ordered by adjusting layer numbers.

### Comparison to Phaser Prototype

| Aspect | Phaser 3 (HTML/CSS) | Godot 4 (Control Nodes) |
|--------|---------------------|-------------------------|
| Rendering | HTML `<section>` element overlaid on `<canvas>` | `Control` node in `CanvasLayer` (layer 10) |
| Positioning | CSS `position: absolute; left: 12px; top: 12px` | Anchor top-left + offset 12, 12 |
| Background | `background: var(--panel)` + `backdrop-filter: blur(5px)` | `StyleBoxFlat` with `bg_color` (no blur) |
| Border | `border: 1px solid var(--panel-border)` | `StyleBoxFlat` with `border_color` + `border_width` |
| Corner radius | `border-radius: 10px` | `StyleBoxFlat` with `corner_radius` = 10 |
| Click-through | `pointer-events: none` | `mouse_filter = MOUSE_FILTER_IGNORE` |
| Title color | CSS `color: var(--accent)` | `theme_override_colors/font_color = Color("#f5c86b")` |
| Stats update | Direct DOM: `hud.textContent = ...` | Signal from `GameState` -> `Label.text` property |
| Responsive | `@media (max-width: 900px)` CSS changes | Godot anchors auto-adapt; manual responsive logic if needed |
| Screen reader | `aria-live="polite"` announces changes | Godot's built-in focus system (limited accessibility) |
| Fonts | `font-family: "Trebuchet MS", ...` via CSS | Godot default font or custom `.ttf` loaded as theme resource |

### Color Reference (Quick Lookup)

| Name | Hex | Godot Color | Used For |
|------|-----|-------------|----------|
| Panel background | `#161b28` at 75% alpha | `Color(0.086, 0.106, 0.157, 0.75)` | PanelContainer StyleBoxFlat bg |
| Panel border | `#f5c86b` at 30% alpha | `Color(0.961, 0.784, 0.420, 0.3)` | PanelContainer StyleBoxFlat border |
| Accent gold | `#f5c86b` | `Color("#f5c86b")` | TitleLabel font color |
| Muted gray-blue | `#b6bfdb` | `Color("#b6bfdb")` | ControlsLabel font color |
| Ink white | `#ecf0ff` | `Color("#ecf0ff")` | StatsLabel font color |

## Open Questions

- Should the HUD be configurable (toggle elements on/off)?
- Where should new elements (gold, minimap) be positioned?
- Should the HUD auto-hide during certain events (cutscenes, NPC interaction)?
- Should there be a detailed stats panel accessible via a hotkey?
- What font should be used? Godot's default, Trebuchet MS (to match prototype), or a pixel/fantasy font?
- Should the panel have a backdrop blur effect? (Godot's `BackBufferCopy` can approximate this but may impact performance.)
- How should the HUD adapt for different window sizes (responsive behavior)?
