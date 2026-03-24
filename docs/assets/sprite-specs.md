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

**Setting enemy color in GDScript:**
```gdscript
# In enemy.gd, after setting danger_tier:
func _set_tier_color() -> void:
    match danger_tier:
        1: polygon.color = Color(0.420, 1.0, 0.537, 1.0)   # Green
        2: polygon.color = Color(1.0, 0.871, 0.400, 1.0)    # Yellow
        3: polygon.color = Color(1.0, 0.435, 0.435, 1.0)    # Red
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
| Rotation | Random per instance: `randf_range(-1.2, 1.2)` radians (approximately -69 to +69 degrees) |
| Lifetime | ~120ms (destroyed after tween completes) |

**Slash animation tween:**
```gdscript
func draw_slash(target_pos: Vector2) -> void:
    var slash := Polygon2D.new()
    slash.polygon = PackedVector2Array([
        Vector2(-13, -2), Vector2(13, -2),
        Vector2(13, 2), Vector2(-13, 2)
    ])
    slash.color = Color(0.961, 0.784, 0.420, 0.95)
    slash.position = target_pos
    slash.rotation = randf_range(-1.2, 1.2)
    get_parent().add_child(slash)

    var tween := create_tween()
    tween.tween_property(slash, "modulate:a", 0.0, 0.12)
    tween.parallel().tween_property(slash, "position:y", target_pos.y - 8.0, 0.12)
    tween.tween_callback(slash.queue_free)
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
| Dynamic color | Enemy tier color is set via `polygon.color` -- no need for separate sprite sheets per tier |
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
4. Update script to call `animated_sprite.play("walk_down")` based on movement direction
5. Collision shapes remain unchanged -- they are siblings of the sprite node, not children

## Implementation Notes

- All Polygon2D vertices are defined relative to origin (0, 0), which is the CharacterBody2D's position in world space.
- The player diamond is intentionally asymmetric in aspect ratio (wider:taller = 24:32 = 3:4) to create a "tall" look fitting for a character in isometric view.
- Enemy diamonds are proportionally similar (20:28 = 5:7) but ~17% smaller in each dimension, making them visually subordinate to the player.
- Slash effect Polygon2D nodes are added as children of the Entities node (not the player or enemy) so their position is in world space, not relative to a moving entity.
- The `queue_free()` call at the end of the slash tween ensures no memory leak from accumulated slash nodes.

## Open Questions

- Should enemy diamonds have a subtle pulsing animation (modulate alpha oscillation) to feel more alive?
- Should the player diamond have a faint glow or outline to make it easier to spot among enemies?
- Should defeated enemies have a death animation (shrink + fade) before removal, or just disappear?
- At what project milestone should we switch from Polygon2D to AnimatedSprite2D?
- Should different enemy tiers have different sizes (larger = more dangerous) or just different colors?
