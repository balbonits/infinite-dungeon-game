# Character and Enemy Sprite Specifications

## Summary

All player, enemy, and effect sprites used in the dungeon. The prototype uses Polygon2D nodes with diamond shapes instead of PNG sprite sheets. This matches the Phaser prototype's approach of colored geometric shapes and keeps the asset pipeline minimal for a learning project.

## Current State

Three entity types are rendered as Polygon2D diamonds: the player (light blue), enemies (green/yellow/red by tier), and slash effects (gold rectangle). No external sprite image files are needed. Future milestone: replace Polygon2D nodes with AnimatedSprite2D using hand-drawn or AI-generated sprite sheets.

## Design

### Player Sprite

| Property | Value |
|----------|-------|
| Node type | Polygon2D |
| Shape | Diamond (4-vertex rhombus) |
| Vertices | `PackedVector2Array[(0, -16), (12, 0), (0, 16), (-12, 0)]` |
| Width | 24 pixels (from x=-12 to x=12) |
| Height | 32 pixels (from y=-16 to y=16) |
| Color hex | `#8ed6ff` (light blue) |
| Color Godot | `Color(0.557, 0.839, 1.0, 1.0)` |
| Origin | Center (0, 0) -- the diamond is centered on the CharacterBody2D position |
| Z-index | Default (0) -- same layer as enemies |

**Vertex diagram (player diamond, 24x32):**
```
        (0, -16)         <- top vertex
       /        \
      /          \
(-12, 0)      (12, 0)   <- left and right vertices (horizontal midline)
      \          /
       \        /
        (0, 16)          <- bottom vertex
```

The player diamond is taller and slightly wider than enemy diamonds, making the player visually distinguishable in a crowd of enemies.

**Phaser equivalent:**
```javascript
this.add.circle(x, y, 12, 0x8ed6ff);
```
The Phaser prototype uses a circle with radius 12. The Godot version uses a diamond shape to better match the isometric tile aesthetic. The bounding dimensions are similar (24px wide in both cases).

### Enemy Sprites (by Tier)

All enemies share the same diamond shape but are slightly smaller than the player. Color indicates danger tier.

| Property | Value |
|----------|-------|
| Node type | Polygon2D |
| Shape | Diamond (4-vertex rhombus) |
| Vertices | `PackedVector2Array[(0, -14), (10, 0), (0, 14), (-10, 0)]` |
| Width | 20 pixels (from x=-10 to x=10) |
| Height | 28 pixels (from y=-14 to y=14) |
| Origin | Center (0, 0) |
| Z-index | Default (0) |

**Vertex diagram (enemy diamond, 20x28):**
```
       (0, -14)          <- top vertex
      /        \
     /          \
(-10, 0)     (10, 0)    <- left and right vertices
     \          /
      \        /
       (0, 14)           <- bottom vertex
```

**Tier color table:**

| Tier | Name | Hex | Godot Color | RGBA (0-255) | RGBA (0.0-1.0) |
|------|------|-----|-------------|--------------|-----------------|
| 1 | Low (Green) | `#6bff89` | `Color(0.420, 1.0, 0.537, 1.0)` | (107, 255, 137, 255) | (0.420, 1.0, 0.537, 1.0) |
| 2 | Mid (Yellow) | `#ffde66` | `Color(1.0, 0.871, 0.400, 1.0)` | (255, 222, 102, 255) | (1.0, 0.871, 0.400, 1.0) |
| 3 | High (Red) | `#ff6f6f` | `Color(1.0, 0.435, 0.435, 1.0)` | (255, 111, 111, 255) | (1.0, 0.435, 0.435, 1.0) |

**Setting enemy color in C#:**
```csharp
// In Enemy.cs, after setting DangerTier:
private void SetTierColor()
{
    _polygon.Color = DangerTier switch
    {
        1 => new Color(0.420f, 1.0f, 0.537f, 1.0f),   // Green
        2 => new Color(1.0f, 0.871f, 0.400f, 1.0f),    // Yellow
        3 => new Color(1.0f, 0.435f, 0.435f, 1.0f),    // Red
        _ => new Color(0.420f, 1.0f, 0.537f, 1.0f),    // Default to green
    };
}
```

**Phaser equivalent:**
```javascript
this.add.circle(x, y, 10, tint);
// tint = 0x6bff89 (tier 1), 0xffde66 (tier 2), 0xff6f6f (tier 3)
```

### Collision Shapes

Each entity has one or more CollisionShape2D nodes with CircleShape2D resources. Circles are used for collision rather than diamond polygons because circle-circle collision is cheaper and produces smoother sliding behavior.

| Entity | Shape Node | Shape Type | Radius | Collision Layer | Collision Mask | Purpose |
|--------|-----------|------------|--------|-----------------|----------------|---------|
| Player body | CollisionShape2D | CircleShape2D | 12.0 px | Layer 2 (player) | Mask 1 (walls) | Physical collision with walls and enemies |
| Enemy body | CollisionShape2D | CircleShape2D | 10.0 px | Layer 3 (enemies) | Mask 1 (walls) | Physical collision with walls |
| Player AttackRange | Area2D > CollisionShape2D | CircleShape2D | 78.0 px | Layer 4 (sensors) | Mask 3 (enemies) | Detects enemies within auto-attack range |
| Enemy HitArea | Area2D > CollisionShape2D | CircleShape2D | 15.0 px | Layer 5 (damage) | Mask 2 (player) | Deals contact damage to player |

**Collision radius rationale:**
- Player body radius (12.0) matches the Phaser circle radius: `this.add.circle(x, y, 12, ...)`.
- Enemy body radius (10.0) matches the Phaser circle radius: `this.add.circle(x, y, 10, ...)`.
- Attack range radius (78.0) matches the Phaser `ATTACK_RANGE = 78` constant used in distance checks.
- Enemy HitArea radius (15.0) is slightly larger than the enemy body (10.0) to create a small "damage aura" that triggers on overlap. The Phaser prototype used physics overlap between player and enemy circles, which effectively meant damage occurred when circles touched (combined radii = 12 + 10 = 22px). The 15px HitArea achieves similar overlap behavior.

**Collision layer assignments:**

| Layer Number | Name | Used By |
|-------------|------|---------|
| 1 | Walls | TileMapLayer wall physics polygons |
| 2 | Player | Player CharacterBody2D |
| 3 | Enemies | Enemy CharacterBody2D bodies |
| 4 | Sensors | Attack range Area2D, interaction zones |
| 5 | Damage | Enemy HitArea Area2D |

### Slash Effect Sprite

The slash effect is a brief visual indicator that plays at the target enemy's position when the player attacks.

| Property | Value |
|----------|-------|
| Node type | Polygon2D (created dynamically, not part of a scene) |
| Shape | Thin rectangle (4 vertices) |
| Vertices | `[Vector2(-13, -2), Vector2(13, -2), Vector2(13, 2), Vector2(-13, 2)]` |
| Width | 26 pixels |
| Height | 4 pixels |
| Color hex | `#f5c86b` at 95% opacity |
| Color Godot | `Color(0.961, 0.784, 0.420, 0.95)` |
| Rotation | Random per instance: `(float)GD.RandRange(-1.2, 1.2)` radians (approximately -69 to +69 degrees) |
| Lifetime | ~120ms (destroyed after tween completes) |

**Slash animation tween:**
```csharp
private void DrawSlash(Vector2 targetPos)
{
    var slash = new Polygon2D();
    slash.Polygon = new Vector2[]
    {
        new Vector2(-13, -2), new Vector2(13, -2),
        new Vector2(13, 2), new Vector2(-13, 2)
    };
    slash.Color = new Color(0.961f, 0.784f, 0.420f, 0.95f);
    slash.Position = targetPos;
    slash.Rotation = (float)GD.RandRange(-1.2, 1.2);
    GetParent().AddChild(slash);

    Tween tween = CreateTween();
    tween.TweenProperty(slash, "modulate:a", 0.0f, 0.12);
    tween.Parallel().TweenProperty(slash, "position:y", targetPos.Y - 8.0f, 0.12);
    tween.TweenCallback(Callable.From(slash.QueueFree));
}
```

**Phaser equivalent:**
```javascript
const slash = this.add.rectangle(x, y, 26, 4, 0xf5c86b, 0.95);
slash.rotation = Phaser.Math.FloatBetween(-1.2, 1.2);
this.tweens.add({
    targets: slash,
    alpha: 0,
    y: y - 8,
    duration: 120,
    onComplete: () => slash.destroy()
});
```

The Godot version replicates the exact same behavior: a 26x4 gold rectangle, random rotation, fading to transparent while drifting 8 pixels upward over 120ms.

### Why Polygon2D Instead of Sprites

| Reason | Detail |
|--------|--------|
| Matches Phaser prototype | The Phaser version uses `this.add.circle()` and `this.add.rectangle()` -- colored geometric primitives, not image sprites |
| No external assets needed | Zero PNG files to manage for characters; the entire visual is defined in code |
| Dynamic color | Enemy tier color is set via `_polygon.Color` -- no need for separate sprite sheets per tier |
| Minimal scope | This is a learning project; adding sprite art is deferred until gameplay systems are solid |
| Easy to swap later | Replace the Polygon2D node with an AnimatedSprite2D node in the scene; the parent CharacterBody2D, collision shapes, and scripts remain unchanged |

### Future Sprite Plans

When the project moves past the prototype phase, Polygon2D nodes will be replaced with AnimatedSprite2D nodes using sprite sheet textures.

| Entity | Current Node | Future Node | Sprite Sheet Spec |
|--------|-------------|-------------|-------------------|
| Player | Polygon2D (24x32 diamond) | AnimatedSprite2D | 32x32 per frame, 4-direction walk cycle (4 frames each = 16 walk frames), idle (1 frame per direction = 4), attack (3 frames per direction = 12). Total: 32 frames on a 256x128 sheet (8 columns x 4 rows). |
| Enemy | Polygon2D (20x28 diamond) | AnimatedSprite2D | 24x24 per frame, walk cycle (4 frames x 4 directions = 16), death (4 frames = 4). Total: 20 frames on a 120x96 sheet (5 columns x 4 rows). One sheet per tier or recolor via modulate. |
| Slash | Polygon2D (26x4 rect) | AnimatedSprite2D | 64x16 per frame, 3-frame slash animation (arc sweep), total 192x16 strip. Gold color with glow. |

**AnimatedSprite2D migration checklist:**
1. Create SpriteFrames resource with named animations ("walk_down", "walk_up", "idle", "attack", "death")
2. Replace Polygon2D node with AnimatedSprite2D in the scene tree
3. Set sprite sheet texture and frame grid (hframes/vframes or atlas regions)
4. Update script to call `_animatedSprite.Play("walk_down")` based on movement direction
5. Collision shapes remain unchanged -- they are siblings of the sprite node, not children

## Implementation Notes

- All Polygon2D vertices are defined relative to origin (0, 0), which is the CharacterBody2D's position in world space.
- The player diamond is intentionally asymmetric in aspect ratio (wider:taller = 24:32 = 3:4) to create a "tall" look fitting for a character in isometric view.
- Enemy diamonds are proportionally similar (20:28 = 5:7) but ~17% smaller in each dimension, making them visually subordinate to the player.
- Slash effect Polygon2D nodes are added as children of the Entities node (not the player or enemy) so their position is in world space, not relative to a moving entity.
- The `QueueFree()` call at the end of the slash tween ensures no memory leak from accumulated slash nodes.

## Asset Generation Strategy

The game uses a **template + runtime recoloring** pipeline to visually differentiate entities by element type, ability, and level-relative threat (see [color-system.md](../systems/color-system.md)). Base shapes are pre-made; accent colors are applied dynamically at runtime.

### SVG as Base Assets

Godot 4 imports SVG files natively — they are rasterized to `CompressedTexture2D` at import time. SVGs provide clean vector source art that scales to any resolution.

For runtime use (Godot 4.2+), `Image.load_svg_from_string(svg_text, scale)` rasterizes an SVG string into an Image on the fly. This works in exported builds.

**Limitations:** SVG `<text>` elements don't render (known Godot bug). Runtime rasterization is CPU-bound — fine if cached, not suitable per-frame.

### Runtime Recoloring Approaches

Ranked by complexity. Start simple, upgrade as needed.

#### 1. Multi-Layer Sprites (Primary — Start Here)

Split each sprite into two layers:
- **Base layer:** The entity's shape and details (neutral/greyscale)
- **Accent layer:** A white silhouette of the colored region (element band, glow area, trim)

Apply `modulate` to the accent layer only — `accent_sprite.modulate = element_color`. The base layer stays unchanged.

**Strengths:** No shader knowledge needed. Fits the current Polygon2D placeholder pattern. Easy to understand and maintain.

**Use for:** Enemy element tinting, item accent bands (e.g., a ring with a colored gemstone), weapon element glow.

#### 2. Shader Palette Swap (Advanced — When Needed)

For finer control over which regions get recolored:

1. Author base sprites with **marker colors** in accent regions (e.g., pure magenta `#FF00FF` for "recolor this region")
2. A `canvas_item` shader replaces marker colors with the actual element/gradient color
3. Set colors from C# via shader uniforms: `material.Set("shader_parameter/accent_color", Colors.Red)`

**Strengths:** GPU-accelerated, trivial cost even with 50+ entities. Can recolor multiple independent regions in one sprite. Supports smooth gradient transitions.

**Community resource:** [Godot-Palette-Swap-Shader](https://github.com/KoBeWi/Godot-Palette-Swap-Shader) — open-source, Godot 4 compatible.

**Use for:** Complex sprites with multiple recolorable zones, smooth color transitions tied to the level-relative gradient.

#### 3. SVG Template Swap (UI/Icons)

For inventory icons, item tooltips, and UI elements:

1. Store SVG template strings with placeholder fills (e.g., `fill="#ACCENT1"`)
2. `String.replace("#ACCENT1", actual_hex)` to swap colors
3. `Image.load_svg_from_string(modified_svg, scale)` to rasterize
4. Cache result as `ImageTexture` — generate once per item type + color combo

**Strengths:** Clean vector art at any resolution. Full control over SVG structure. Good for procedurally varied icons.

**Use for:** Inventory item icons, skill icons with element coloring, UI badges.

### Build-Time Generation

The existing `scripts/generate_tiles.py` pattern (pure Python, zero dependencies, generates PNGs pixel-by-pixel) can be extended for batch sprite generation:
- Sprite sheet variants with pre-baked color accents
- Tile variants for floor depth visual changes
- Icon sets with element-colored variants

Run via `make tiles` (already in Makefile).

### Recommended Pipeline

| Phase | Approach | When |
|-------|----------|------|
| Prototype (current) | Polygon2D with `color` property | Now — already working |
| Early sprites | Multi-layer sprites + `modulate` | When first real sprite art is added |
| Element/gradient system | Shader palette swap | When the color gradient system is implemented |
| Inventory UI | SVG template swap | When inventory UI is built |
| Batch assets | Extend `generate_tiles.py` | For tilemap variants, icon sets |

### Color Sources

Runtime recoloring draws from two systems:
- **Element type colors:** Fire (red), Water (blue), Air (cyan), Earth (brown), Light (gold), Dark (purple) — fixed per element, defined in [color-system.md](../systems/color-system.md)
- **Level-relative gradient:** Grey → Blue → Cyan → Green → Yellow → Gold → Orange → Red — computed from `entity_level - player_level`, see [color-system.md](../systems/color-system.md)

Both can be combined: a fire monster on an even-level floor might have red element accents AND a green/yellow threat tint.

## Licensing

All sprite assets must use one of the following licenses: **CC0**, **CC-BY 3.0**, or **CC-BY 4.0**. Any asset requiring attribution must be listed in [`assets/ATTRIBUTION.md`](../../assets/ATTRIBUTION.md).
