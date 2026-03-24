# Player

## Summary

The player is a CharacterBody2D with isometric movement, auto-targeting melee combat, and a following camera. Movement input (WASD / arrow keys) is transformed through an isometric matrix to produce Diablo-style directional movement. The player automatically attacks the nearest enemy within a 78px radius on a 0.42s cooldown.

## Current State

Fully functional placeholder player with:
- Isometric 8-directional movement at 190 pixels/second
- Auto-attack system targeting nearest enemy within range
- Gold slash visual effect on hit
- Camera shake when taking damage
- Diamond-shaped Polygon2D placeholder sprite (light blue, `#8ed6ff`)
- Camera2D child with 2x zoom and position smoothing

## Design

### Node Tree

See `docs/architecture/scene-tree.md` for the full `player.tscn` hierarchy with every property value.

```
Player (CharacterBody2D) [player.gd]
├── CollisionShape2D → CircleShape2D(radius=12.0)
├── Sprite (Polygon2D) → color #8ed6ff, diamond shape
├── Camera2D → zoom 2x, smoothing speed 5.0
└── AttackRange (Area2D) → CircleShape2D(radius=78.0), detects enemy layer
```

### Constants

| Name | Type | Value | Phaser Equivalent | Purpose |
|------|------|-------|-------------------|---------|
| `MOVE_SPEED` | `float` | `190.0` | `new Phaser.Math.Vector2(vx, vy).normalize().scale(190)` | Movement speed in pixels per second. Applied after normalizing the input direction vector so diagonal movement isn't faster than cardinal. |
| `ATTACK_COOLDOWN` | `float` | `0.42` | `const ATTACK_COOLDOWN = 420` (milliseconds) | Minimum seconds between auto-attacks. The Phaser value was 420ms; Godot uses seconds natively. |
| `ATTACK_RANGE` | `float` | `78.0` | `const ATTACK_RANGE = 78` | Radius in pixels of the auto-attack detection circle. Enemies within this distance from the player's origin are candidates for auto-attack. This value also defines the AttackRange Area2D's CollisionShape2D radius. |

### Variables

| Name | Type | Default | Purpose |
|------|------|---------|---------|
| `attack_timer` | `float` | `0.0` | Countdown in seconds until the next attack is allowed. Decremented each `_physics_process`. When <= 0, the player can attack. Reset to `ATTACK_COOLDOWN` on each attack. |
| `iso_transform` | `Transform2D` | `Transform2D(Vector2(1, 0.5), Vector2(-1, 0.5), Vector2.ZERO)` | Matrix that converts screen-space input direction to isometric world direction. See "Isometric Movement Deep Dive" below. |

### Node References (@onready)

```gdscript
@onready var attack_range: Area2D = $AttackRange
@onready var sprite: Polygon2D = $Sprite
@onready var camera: Camera2D = $Camera2D
```

**Why @onready:** These references are resolved when the node enters the scene tree (`_ready()` timing). Using `$NodeName` in variable declarations without `@onready` would fail because the node tree isn't built yet at script parse time. `@onready` defers the assignment to `_ready()`.

---

### Methods

#### `_ready() -> void`

Called once when the Player node enters the scene tree.

```gdscript
func _ready() -> void:
    add_to_group("player")
```

**What it does:**
1. Adds the Player node to the `"player"` group so enemies can find it via `get_tree().get_first_node_in_group("player")`

**Why group instead of export/reference:** Enemies are instanced dynamically at runtime -- they can't hold a pre-set reference to the player. Groups provide a runtime lookup mechanism that doesn't require any node to hold a direct reference to any other.

---

#### `_physics_process(delta: float) -> void`

Called every physics frame (60 times per second by default). This is the player's main update loop.

```gdscript
func _physics_process(delta: float) -> void:
    handle_movement()
    handle_attack(delta)
```

**Why `_physics_process` instead of `_process`:** Movement uses `move_and_slide()` which interacts with the physics engine. Physics queries (like Area2D overlap detection) are updated on the physics tick. Running movement on the physics tick ensures collision detection is consistent and frame-rate independent.

**Why delta is only passed to handle_attack:** Movement speed is applied via `velocity` + `move_and_slide()`, which handles delta internally. The attack timer is a manual float countdown that must be decremented by delta explicitly.

---

#### `handle_movement() -> void`

Reads input and moves the player in isometric space.

```gdscript
func handle_movement() -> void:
    # Step 1: Read input as a screen-space direction vector
    var input_dir: Vector2 = Input.get_vector(
        "move_left",   # negative X (A key / Left arrow)
        "move_right",  # positive X (D key / Right arrow)
        "move_up",     # negative Y (W key / Up arrow)
        "move_down"    # positive Y (S key / Down arrow)
    )

    # Step 2: Transform to isometric and apply speed
    if input_dir.length() > 0:
        var iso_dir: Vector2 = iso_transform * input_dir
        velocity = iso_dir.normalized() * MOVE_SPEED
    else:
        velocity = Vector2.ZERO

    # Step 3: Move with wall collision
    move_and_slide()
```

**Step-by-step breakdown:**

**Step 1 — Input reading:**
`Input.get_vector()` returns a Vector2 based on four named input actions. It handles:
- Single key: pressing W alone returns `Vector2(0, -1)` (up)
- Diagonal: pressing W+D returns `Vector2(0.707, -0.707)` (up-right, already normalized)
- No input: returns `Vector2.ZERO`

The input actions ("move_left", "move_right", "move_up", "move_down") are defined in `project.godot` under `[input]`. Each action maps to both a WASD key and an arrow key:
- `move_left`: A key, Left arrow
- `move_right`: D key, Right arrow
- `move_up`: W key, Up arrow
- `move_down`: S key, Down arrow

**Step 2 — Isometric transform:**
The raw input vector represents screen-space direction (up on screen = negative Y). This must be converted to isometric world direction where "up on screen" means "toward the back-left" of the isometric grid.

The `iso_transform` matrix performs this conversion. After transformation, the result is normalized (to prevent diagonal speed boost from the transform changing vector length) and scaled by `MOVE_SPEED`.

**Step 3 — Physics movement:**
`move_and_slide()` is a CharacterBody2D method that:
1. Attempts to move the node by `velocity * delta` (delta is applied internally)
2. If a collision occurs (wall tile), slides along the wall surface
3. Updates `velocity` to reflect the slide direction
4. Returns `true` if a collision occurred (unused here)

**Why not `move_and_collide()`:** `move_and_collide()` stops at the collision point and doesn't slide. `move_and_slide()` automatically projects the velocity along the collision surface, so the player slides along walls instead of stopping dead. This matches the Phaser prototype's `setCollideWorldBounds(true)` behavior.

---

#### `handle_attack(delta: float) -> void`

Handles the auto-attack cooldown and targets the nearest enemy.

```gdscript
func handle_attack(delta: float) -> void:
    # Step 1: Decrement cooldown
    attack_timer -= delta
    if attack_timer > 0:
        return

    # Step 2: Find nearest enemy in range
    var bodies: Array = attack_range.get_overlapping_bodies()
    var nearest_enemy: Node2D = null
    var nearest_distance: float = INF

    for body in bodies:
        if not body.is_in_group("enemies"):
            continue
        var distance: float = global_position.distance_to(body.global_position)
        if distance < nearest_distance:
            nearest_distance = distance
            nearest_enemy = body

    # Step 3: No target? Skip
    if nearest_enemy == null:
        return

    # Step 4: Attack
    attack_timer = ATTACK_COOLDOWN

    var damage: int = 12 + int(GameState.level * 1.5)
    nearest_enemy.take_damage(damage)

    draw_slash(nearest_enemy.global_position)

    EventBus.player_attacked.emit(nearest_enemy)
```

**Step-by-step breakdown:**

**Step 1 — Cooldown management:**
`attack_timer` starts at 0.0 (allowing immediate first attack). Each frame, delta is subtracted. If the timer is still positive, the function returns early. When the timer reaches 0 (or below), an attack can occur. After attacking, the timer resets to `ATTACK_COOLDOWN` (0.42 seconds).

The timer can go negative between attacks (e.g., timer = -0.05 when delta = 0.016). This doesn't cause double-attacks because the reset always sets it to the full cooldown value, not `timer + ATTACK_COOLDOWN`. This means the effective cooldown is always at least 0.42s.

**Step 2 — Target selection:**
`attack_range.get_overlapping_bodies()` returns all CharacterBody2D nodes whose collision shapes overlap the AttackRange's CircleShape2D (radius 78). This includes any node on collision layer 4 (enemies), since AttackRange's mask is set to 4.

The function iterates all overlapping bodies, filters to those in the "enemies" group (safety check -- in case non-enemy bodies ever end up on layer 4), and finds the one closest to the player by Euclidean distance.

**Why Euclidean distance and not isometric distance:** The AttackRange collision circle is already circular in world space. Isometric transform affects rendering, not physics. An enemy that is 78px away in world space is 78px away in the physics simulation, regardless of how it appears on screen. Using `global_position.distance_to()` is correct.

**Step 3 — Guard clause:**
If no enemy is in range, return without attacking. The cooldown timer is NOT reset here, so the player attacks immediately when an enemy enters range (no wasted cooldown).

**Step 4 — Execute attack:**

Damage formula: `12 + int(GameState.level * 1.5)`

| Level | Calculation | Damage |
|-------|-------------|--------|
| 1 | 12 + int(1.5) = 12 + 1 | 13 |
| 2 | 12 + int(3.0) = 12 + 3 | 15 |
| 3 | 12 + int(4.5) = 12 + 4 | 16 |
| 4 | 12 + int(6.0) = 12 + 6 | 18 |
| 5 | 12 + int(7.5) = 12 + 7 | 19 |
| 10 | 12 + int(15.0) = 12 + 15 | 27 |
| 20 | 12 + int(30.0) = 12 + 30 | 42 |

**Note:** The Phaser prototype used `Math.floor(state.level * 1.5)` which is equivalent to GDScript's `int()` truncation for positive values.

After calculating damage:
1. Call `nearest_enemy.take_damage(damage)` -- the enemy handles its own HP reduction and potential death
2. Call `draw_slash(nearest_enemy.global_position)` -- visual feedback
3. Emit `EventBus.player_attacked.emit(nearest_enemy)` -- notify other systems

---

#### `draw_slash(target_pos: Vector2) -> void`

Creates a temporary visual effect at the attack target location.

```gdscript
func draw_slash(target_pos: Vector2) -> void:
    # Step 1: Create slash polygon
    var slash: Polygon2D = Polygon2D.new()
    slash.polygon = PackedVector2Array([
        Vector2(-13, -2),
        Vector2(13, -2),
        Vector2(13, 2),
        Vector2(-13, 2)
    ])
    slash.color = Color("#f5c86b", 0.95)
    slash.global_position = target_pos
    slash.rotation = randf_range(-1.2, 1.2)

    # Step 2: Add to Entities container (for y-sort ordering)
    get_parent().add_child(slash)

    # Step 3: Animate fade + rise
    var tween: Tween = create_tween()
    tween.set_parallel(true)
    tween.tween_property(slash, "modulate:a", 0.0, 0.12)
    tween.tween_property(slash, "position:y", slash.position.y - 8, 0.12)
    tween.set_parallel(false)
    tween.tween_callback(slash.queue_free)
```

**Detailed parameter breakdown:**

| Parameter | Value | Explanation |
|-----------|-------|-------------|
| Polygon shape | `[(-13,-2), (13,-2), (13,2), (-13,2)]` | A 26x4 pixel thin rectangle. Represents a sword slash mark. 26px wide (±13 from center), 4px tall (±2 from center). |
| Color | `Color("#f5c86b", 0.95)` | Gold/accent color at 95% opacity. `#f5c86b` matches CSS `var(--accent)` and Phaser's `COLORS.sword = 0xf5c86b`. |
| Position | `target_pos` (enemy's `global_position`) | The slash appears directly on the enemy, not between player and enemy. |
| Rotation | `randf_range(-1.2, 1.2)` | Random angle in radians (approximately ±69 degrees). Each slash appears at a different angle, giving visual variety to repeated attacks. 1.2 radians was chosen to avoid fully vertical slashes (which would look like thin lines). |
| Parent node | `get_parent()` | The player's parent is the Entities Node2D container, which has `y_sort_enabled = true`. Adding the slash here ensures it sorts correctly with other entities. |
| Fade duration | `0.12` seconds | 120 milliseconds. Matches Phaser's `duration: 120`. Fast enough to feel snappy, slow enough to be visible. |
| Rise amount | `position.y - 8` | The slash drifts 8 pixels upward during the fade. This creates a "rising impact" effect. In isometric view, upward movement suggests the slash is floating away. |
| Cleanup | `slash.queue_free()` | After the tween completes (0.12s), the slash node is freed from memory. Without this, slash nodes would accumulate indefinitely. |

**Why Polygon2D instead of Sprite2D:** No sprite assets exist yet. Polygon2D renders a filled shape from vertex data, matching the Phaser prototype's `this.add.rectangle()`. When pixel art assets are available, the slash could become a Sprite2D or AnimatedSprite2D.

**Why `set_parallel(true)` for the tween:** Both the fade (`modulate:a`) and rise (`position:y`) should happen simultaneously, not sequentially. `set_parallel(true)` makes subsequent tween calls run at the same time. `set_parallel(false)` is then called before the cleanup callback to ensure `queue_free` only runs after both parallel tweens complete.

See `docs/objects/effects.md` for complete effect specification.

---

#### `shake_camera() -> void`

Applies a brief camera shake when the player takes damage. Called by `enemy.gd._deal_damage_to()`.

```gdscript
func shake_camera() -> void:
    var tween: Tween = create_tween()
    tween.tween_property(
        camera, "offset",
        Vector2(randf_range(-3, 3), randf_range(-3, 3)),
        0.045
    )
    tween.tween_property(
        camera, "offset",
        Vector2.ZERO,
        0.045
    )
```

**Detailed parameter breakdown:**

| Parameter | Value | Explanation |
|-----------|-------|-------------|
| Target node | `camera` (Camera2D child) | The `offset` property displaces the camera view without moving the camera node in the scene tree. |
| Shake offset | `Vector2(randf_range(-3, 3), randf_range(-3, 3))` | Random displacement of ±3 pixels in both X and Y. Each shake has a different direction. |
| Shake duration | `0.045` seconds (45ms) | Time to move camera to the displaced position. |
| Return duration | `0.045` seconds (45ms) | Time to return camera to center. |
| Total duration | `0.09` seconds (90ms) | Matches Phaser's `this.cameras.main.shake(90, 0.0035)`. |
| Phaser intensity mapping | 0.0035 * viewport ≈ 3-4px | Phaser's shake intensity is a fraction of viewport size. At 1100px wide: 1100 * 0.0035 ≈ 3.85px. The ±3px range is a close match. |

**Why tween offset instead of position:** Camera2D.offset is a rendering-only displacement. It doesn't affect the camera's logical position in the scene tree. Tweening offset means the camera's position smoothing continues to work normally -- the shake is layered on top of the follow behavior, not fighting against it.

**Edge case -- overlapping shakes:** If the player takes damage rapidly (within 90ms), a new tween is created while the old one is still running. Godot's `create_tween()` creates independent tweens; both will modify `offset` simultaneously. In practice, the most recent tween "wins" the final offset value, and the visual result is a slightly longer, more intense shake. This is acceptable -- rapid damage should feel more impactful.

---

### Isometric Movement Deep Dive

#### The Transform2D Matrix

```gdscript
var iso_transform: Transform2D = Transform2D(
    Vector2(1, 0.5),    # X basis vector (where screen-right maps to)
    Vector2(-1, 0.5),   # Y basis vector (where screen-down maps to)
    Vector2.ZERO         # Origin (no translation)
)
```

This matrix defines how screen-space input directions map to isometric world-space movement directions.

#### What the Matrix Does

A Transform2D multiplied by a Vector2 applies the linear transformation:
```
result.x = input.x * basis_x.x + input.y * basis_y.x
result.y = input.x * basis_x.y + input.y * basis_y.y
```

In matrix form:
```
| 1  -1 |   | input.x |   | input.x - input.y     |
|       | × |         | = |                         |
| 0.5 0.5|   | input.y |   | 0.5*input.x + 0.5*input.y |
```

#### Direction Mapping Table

| Key(s) | Screen Input | After iso_transform | After normalize | Visual Direction |
|--------|-------------|--------------------:|----------------:|-----------------|
| W | (0, -1) | (0 - (-1), 0 + (-0.5)) = (1, -0.5) | (0.894, -0.447) | Northeast (into screen) |
| S | (0, 1) | (0 - 1, 0 + 0.5) = (-1, 0.5) | (-0.894, 0.447) | Southwest (toward camera) |
| A | (-1, 0) | (-1 - 0, -0.5 + 0) = (-1, -0.5) | (-0.894, -0.447) | Northwest (left into screen) |
| D | (1, 0) | (1 - 0, 0.5 + 0) = (1, 0.5) | (0.894, 0.447) | Southeast (right toward camera) |
| W+D | (0.707, -0.707) | (1.414, 0) | (1, 0) | East (right along iso axis) |
| W+A | (-0.707, -0.707) | (0, -0.707) | (0, -1) | North (up along iso axis) |
| S+D | (0.707, 0.707) | (0, 0.707) | (0, 1) | South (down along iso axis) |
| S+A | (-0.707, 0.707) | (-1.414, 0) | (-1, 0) | West (left along iso axis) |

#### Why This Feels Like Diablo

In a standard top-down 2D game, pressing W moves the character straight up on screen. In isometric view, "up" on screen corresponds to moving diagonally into the world (northeast). The iso_transform remaps WASD so that:

- W moves the character "away" from the camera (into the screen), which visually moves up-right along the isometric grid
- S moves toward the camera (down-left)
- A and D move along the perpendicular isometric axis

This creates the iconic Diablo/isometric ARPG feel where WASD corresponds to the four isometric cardinal directions rather than screen-space directions.

#### Why Normalize After Transform

The isometric transform changes vector length. For example:
- Input `(0, -1)` (W key, length 1.0) becomes `(1, -0.5)` (length 1.118)
- Input `(0.707, -0.707)` (W+D diagonal, length 1.0) becomes `(1.414, 0)` (length 1.414)

Without normalization, diagonal movement would be 26% faster than cardinal. Normalizing after transform ensures all directions move at exactly `MOVE_SPEED` pixels per second, regardless of the iso_transform's scaling effect.

---

### Collision Setup

| Property | Value | Binary | Description |
|----------|-------|--------|-------------|
| `collision_layer` | `2` | `0b010` | Bit 1 set. Identifies this body as being on the "player" layer. |
| `collision_mask` | `1` | `0b001` | Bit 0 set. This body collides with objects on layer 1 (walls/tile physics). |

**What collides with the player:**
- Wall tiles (layer 1) block player movement via `move_and_slide()`
- Player AttackRange (Area2D, mask 4) detects enemies overlapping the player's vicinity
- Enemy HitArea (Area2D, mask 2) detects the player entering enemy hit zones

**What does NOT collide with the player:**
- Enemy bodies (layer 4) -- enemies pass through the player. There is no physics push/block between player and enemies. This is intentional: enemies deal damage via Area2D overlap, not physics collision. If enemies physically blocked the player, getting surrounded would be a frustrating stun-lock.
- Other player bodies -- only one player exists, but the mask excludes layer 2 (own layer) anyway.

**Why collision_layer 2 (not 1 or 4):**
Collision layers are a bit mask system. Keeping player on a unique layer (2) allows fine-grained control. Enemies can detect the player (mask bit 1 = layer 2) without also detecting walls or other enemies. If the player shared layer 1 with walls, enemies' Area2D would detect both the player and wall tiles.

---

### Input Map Configuration

The following input actions must be defined in `project.godot` under `[input]`:

| Action Name | Keys | Used By |
|-------------|------|---------|
| `move_left` | A, Left Arrow | `Input.get_vector()` negative X |
| `move_right` | D, Right Arrow | `Input.get_vector()` positive X |
| `move_up` | W, Up Arrow | `Input.get_vector()` negative Y |
| `move_down` | S, Down Arrow | `Input.get_vector()` positive Y |

**Why Input Map instead of raw key checking:** Godot's Input Map system provides:
1. **Multiple keys per action:** W and Up Arrow both trigger "move_up" without branching code
2. **Rebinding support:** Actions can be remapped at runtime without code changes
3. **Gamepad support:** Future analog stick input maps to the same actions
4. **`Input.get_vector()`:** Returns a normalized Vector2 from four actions, handling diagonal input automatically

The Phaser prototype used explicit key checks (`this.keys.W.isDown || this.cursors.up.isDown`). Godot's `Input.get_vector()` replaces all of that with a single function call.

## Implementation Notes

- The player scene is preloaded by `dungeon.gd`: `const PlayerScene = preload("res://scenes/player.tscn")`. It is instanced once in `_ready()` and added to the Entities container.
- The player's initial position is set by `dungeon.gd` to the center of the tile map: `player.global_position = tile_map.map_to_local(Vector2i(ROOM_SIZE / 2, ROOM_SIZE / 2))`.
- The Camera2D is the only active camera in the scene. Since it's a child of the Player, it automatically follows player movement with the configured smoothing.
- The `attack_timer` starting at 0.0 means the player can attack on the very first frame if an enemy is in range. This matches the Phaser prototype where `lastAttackAt` starts at 0 and `time` is always greater than 0.
- `create_tween()` on the player node ties the tween's lifetime to the player. If the player is freed (unlikely in current design), active tweens are cleaned up automatically.

## Open Questions

- Should the player have a `_process()` method for non-physics tasks (animation updates, UI indicators)?
- Should the attack target be cached between frames to avoid re-scanning every physics frame?
- Should the isometric transform be a constant instead of a variable, since it never changes at runtime?
- Should there be a brief invincibility window after taking damage (in addition to the per-enemy 0.7s cooldown)?
- Should the player have a `max_speed` that differs from `MOVE_SPEED` for future movement buffs/debuffs?
- How will the movement system adapt when pixel art sprites are added (animation direction selection, sprite flipping)?
