# Movement System

## Summary

The player moves in 8 directions using WASD or arrow keys. Input is transformed through an isometric projection matrix so that pressing "up" moves the player diagonally up-left in screen space (into the screen in isometric perspective, like Diablo 1). Movement uses Godot's CharacterBody2D with `move_and_slide()` for built-in wall collision and sliding.

## Current State

Keyboard-only movement is implemented. The Phaser prototype also supported click/drag-to-move via pointer input; this is deferred in the Godot version. The isometric transform matrix is the key difference from the Phaser version, which used screen-aligned movement on a flat grid.

## Design

### Input Reading

| Property | Value |
|----------|-------|
| Method | `Input.get_vector("move_left", "move_right", "move_up", "move_down")` |
| Return type | `Vector2` with x in [-1, 1] and y in [-1, 1] |
| Diagonal normalization | Automatic (get_vector normalizes the result when both axes are active) |
| Fallback method | `Input.is_key_pressed(KEY_W)`, etc. (not used if Input Map is configured) |
| Dead zone | 0.0 (keyboard is binary; dead zone applies to joystick input if added later) |

**Input Map Configuration (project.godot):**

| Action Name | Primary Key | Secondary Key | Description |
|-------------|-------------|--------------|-------------|
| `move_up` | W | Up Arrow | Move toward screen top (iso: into the screen) |
| `move_down` | S | Down Arrow | Move toward screen bottom (iso: toward camera) |
| `move_left` | A | Left Arrow | Move toward screen left (iso: northwest) |
| `move_right` | D | Right Arrow | Move toward screen right (iso: southeast) |

Each action is defined in `project.godot` under `[input]`. Example:
```
[input]
move_up={
"deadzone": 0.0,
"events": [Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":87,"physical_keycode":0,"key_label":0,"unicode":119,"location":0,"echo":false,"script":null), Object(InputEventKey,"resource_local_to_scene":false,"resource_name":"","device":-1,"window_id":0,"alt_pressed":false,"shift_pressed":false,"ctrl_pressed":false,"meta_pressed":false,"pressed":false,"keycode":4194320,"physical_keycode":0,"key_label":0,"unicode":0,"location":0,"echo":false,"script":null)]
}
```

### Isometric Transform Matrix

The core of the isometric movement system is a 2x2 affine transform that converts screen-space input directions into isometric world directions.

**Matrix definition:**
```gdscript
const ISO_TRANSFORM := Transform2D(Vector2(1, 0.5), Vector2(-1, 0.5), Vector2.ZERO)
```

This Transform2D has:
- **x-basis (column 0):** `Vector2(1, 0.5)` -- screen-right maps to iso-southeast
- **y-basis (column 1):** `Vector2(-1, 0.5)` -- screen-down maps to iso-southwest
- **origin:** `Vector2.ZERO` -- no translation offset

**Matrix in standard notation:**
```
| 1   -1 |
| 0.5  0.5 |
```

When applied to an input vector `(ix, iy)`:
```
result_x = 1 * ix + (-1) * iy
result_y = 0.5 * ix + 0.5 * iy
```

### Direction Mapping Table

Complete mapping of all 8 input directions through the isometric transform:

| Input | Keys | Screen Vector | After ISO Transform | Normalized | Iso Direction | Screen Movement |
|-------|------|---------------|-------------------|-----------|---------------|-----------------|
| Up | W / Up Arrow | (0, -1) | (1, -0.5) | (0.894, -0.447) | Northeast | Up-right diagonal |
| Down | S / Down Arrow | (0, 1) | (-1, 0.5) | (-0.894, 0.447) | Southwest | Down-left diagonal |
| Left | A / Left Arrow | (-1, 0) | (-1, -0.5) | (-0.894, -0.447) | Northwest | Up-left diagonal |
| Right | D / Right Arrow | (1, 0) | (1, 0.5) | (0.894, 0.447) | Southeast | Down-right diagonal |
| Up+Right | W+D | (0.707, -0.707) | (1.414, 0) | (1, 0) | East | Pure right |
| Up+Left | W+A | (-0.707, -0.707) | (0, -0.707) | (0, -1) | North | Pure up |
| Down+Right | S+D | (0.707, 0.707) | (0, 0.707) | (0, 1) | South | Pure down |
| Down+Left | S+A | (-0.707, 0.707) | (-1.414, 0) | (-1, 0) | West | Pure left |

**Note on diagonal screen vectors:** `Input.get_vector()` automatically normalizes diagonal input, so W+D produces `(0.707, -0.707)` rather than `(1, -1)`. The 0.707 value is `1 / sqrt(2)`.

### Isometric Direction Diagram

```
                    N (W+A)
                    |
                    |
        NW (A)     |     NE (W)
            \      |      /
             \     |     /
              \    |    /
               \   |   /
    W (S+A) ----[Player]---- E (W+D)
               /   |   \
              /    |    \
             /     |     \
            /      |      \
        SW (S)     |     SE (D)
                    |
                    |
                    S (S+D)
```

This shows the isometric compass directions and which keyboard keys produce each direction. Note that single keys (W, A, S, D) produce diagonal screen movement, while two-key combinations (W+D, W+A, S+D, S+A) produce cardinal screen movement. This is the expected behavior for isometric games.

### Movement Algorithm

Step-by-step, executed every physics frame in `_physics_process(delta)`:

```gdscript
func _physics_process(delta: float) -> void:
    # Step 1: Read raw input as a screen-space direction vector
    var input_dir := Input.get_vector("move_left", "move_right", "move_up", "move_down")
    # Result: Vector2 with components in [-1, 1], normalized for diagonals
    # Example: W pressed -> (0, -1), W+D pressed -> (0.707, -0.707)

    # Step 2: Transform screen input to isometric world direction
    var iso_dir := ISO_TRANSFORM * input_dir
    # Result: Vector2 in isometric world space
    # Example: (0, -1) -> (1, -0.5), (0.707, -0.707) -> (1.414, 0)

    # Step 3: Normalize and scale to movement speed
    velocity = iso_dir.normalized() * MOVE_SPEED
    # Result: Vector2 with magnitude = MOVE_SPEED (or zero if no input)
    # Example: (1, -0.5).normalized() * 190 = (169.7, -84.9)

    # Step 4: Apply movement with collision resolution
    move_and_slide()
    # CharacterBody2D handles delta time internally
    # Resolves collisions with physics bodies (walls)
    # Applies sliding along walls rather than stopping dead
```

**Important details:**
- `iso_dir.normalized()` is safe even when `input_dir` is `Vector2.ZERO` because `Vector2.ZERO.normalized()` returns `Vector2.ZERO` in Godot (no division by zero).
- `move_and_slide()` uses the `velocity` property of CharacterBody2D directly. It does NOT take a velocity parameter in Godot 4.
- `move_and_slide()` handles delta time internally -- do not multiply velocity by delta.
- When no keys are pressed, `input_dir` is `(0, 0)`, `iso_dir` is `(0, 0)`, `velocity` is `(0, 0)`, and the player stands still.

### Movement Speed

| Property | Value | Unit |
|----------|-------|------|
| MOVE_SPEED | 190.0 | pixels per second |

This value is taken directly from the Phaser prototype:
```javascript
const normalized = new Phaser.Math.Vector2(vx, vy).normalize().scale(190);
body.setVelocity(normalized.x, normalized.y);
```

**Perceived speed note:** In the Phaser prototype, movement was screen-aligned (no isometric transform). The player moved at 190 px/s in screen space. In the Godot version, the isometric transform changes the relationship between input and screen movement. Pressing a single key (e.g., W) produces a diagonal screen movement at 190 px/s, but the screen-horizontal and screen-vertical components are different:
- Horizontal component: `190 * 0.894 = 169.9 px/s`
- Vertical component: `190 * 0.447 = 84.9 px/s`

This means single-key movement covers more horizontal ground than vertical ground, which is correct for a 2:1 isometric ratio. However, the perceived speed may feel different from the Phaser version and might need tuning.

**Max velocity (Phaser comparison):**
The Phaser prototype also sets `body.setMaxVelocity(220, 220)`, which caps velocity at 220 px/s per axis. In Godot, CharacterBody2D does not have a built-in max velocity property. The velocity is set explicitly each frame, so it is naturally clamped to `MOVE_SPEED` (190) after normalization. No additional capping is needed.

### Wall Collision

| Property | Detail |
|----------|--------|
| Player body type | CharacterBody2D |
| Collision method | `move_and_slide()` |
| Wall physics | TileMapLayer collision polygons on physics layer 1 |
| Player collision_layer | 2 (player layer) |
| Player collision_mask | 1 (walls layer) -- player collides with walls |
| Slide behavior | Player slides along walls at reduced speed rather than stopping completely |
| Collision shape | CircleShape2D, radius 12.0 px (see sprite-specs.md) |

**How move_and_slide() resolves collisions:**
1. Applies velocity as a motion vector for the frame.
2. If the motion would cause overlap with a physics body, computes the collision normal.
3. Subtracts the component of velocity along the collision normal (the "into the wall" component).
4. Applies the remaining velocity (the "along the wall" component) as a slide.
5. Repeats for up to `max_slides` iterations (default 6) to handle corners.
6. Updates the `velocity` property with the post-slide velocity.

This means a player running diagonally into a wall will slide along it smoothly, which feels natural.

### Phaser Prototype Movement (for reference)

The Phaser version uses screen-aligned movement with no isometric transform:

```javascript
handleMovement() {
    const body = this.player.body;
    body.setVelocity(0, 0);

    const left = this.cursors.left.isDown || this.keys.A.isDown;
    const right = this.cursors.right.isDown || this.keys.D.isDown;
    const up = this.cursors.up.isDown || this.keys.W.isDown;
    const down = this.cursors.down.isDown || this.keys.S.isDown;

    let vx = 0, vy = 0;
    if (left) vx -= 1;
    if (right) vx += 1;
    if (up) vy -= 1;
    if (down) vy += 1;

    // Pointer movement (click/drag)
    if (this.movePointer && this.movePointer.isDown) {
        const dx = this.movePointer.worldX - this.player.x;
        const dy = this.movePointer.worldY - this.player.y;
        const dist = Math.hypot(dx, dy);
        if (dist > 12) {
            vx = dx / dist;
            vy = dy / dist;
        }
    }

    if (vx !== 0 || vy !== 0) {
        const normalized = new Phaser.Math.Vector2(vx, vy).normalize().scale(190);
        body.setVelocity(normalized.x, normalized.y);
    }
}
```

Key differences from the Godot version:
| Aspect | Phaser | Godot |
|--------|--------|-------|
| Coordinate system | Screen-aligned (up = -Y) | Isometric (up = into-screen via transform) |
| Input method | Manual key checks + pointer | `Input.get_vector()` + ISO_TRANSFORM |
| Collision | Arcade physics world bounds | CharacterBody2D + TileMapLayer polygons |
| Pointer movement | Supported (click/drag) | Deferred (not implemented) |
| Normalization | Manual `new Vector2(vx, vy).normalize()` | `Input.get_vector()` auto-normalizes |
| Speed | 190 px/s screen space | 190 px/s world space (appears different due to iso transform) |

### Pointer Movement (Deferred)

The Phaser prototype supports click/drag-to-move:
```javascript
if (this.movePointer && this.movePointer.isDown) {
    const dx = this.movePointer.worldX - this.player.x;
    const dy = this.movePointer.worldY - this.player.y;
    const dist = Math.hypot(dx, dy);
    if (dist > 12) {
        vx = dx / dist;
        vy = dy / dist;
    }
}
```

This is deferred in the Godot version because:
1. **Screen-to-iso conversion:** In isometric view, the screen position of a click must be converted to isometric world coordinates before computing a direction vector. This requires the inverse of the isometric transform.
2. **Camera offset:** The Camera2D position and zoom must be accounted for when converting screen coordinates to world coordinates (using `get_global_mouse_position()` helps, but the direction still needs iso-awareness).
3. **Complexity budget:** Adding pointer movement is a polish feature. Keyboard works for all gameplay. Will be added when touch/mobile support is prioritized.

**When implemented, the algorithm would be:**
```gdscript
var mouse_world := get_global_mouse_position()
var direction := (mouse_world - global_position)
if direction.length() > 12.0:
    velocity = direction.normalized() * MOVE_SPEED
    move_and_slide()
```

Note: since `get_global_mouse_position()` returns world-space coordinates, and the player's `global_position` is also in world space, the direction vector is already in the correct coordinate system. No additional isometric conversion is needed for pointer movement -- the iso transform is only needed for keyboard input because keyboard directions are screen-relative.

## Implementation Notes

- The `ISO_TRANSFORM` constant should be defined at the top of `player.gd` as a class-level constant.
- `_physics_process()` is used instead of `_process()` because movement involves physics collision via `move_and_slide()`. This ensures movement is synced with the physics engine's fixed timestep.
- The default physics tick rate is 60 Hz (`physics/common/physics_ticks_per_second = 60` in project.godot). Movement at 190 px/s means ~3.17 pixels per physics frame.
- If the player is dead (`GameState.is_dead`), movement input should be ignored (set velocity to zero and return early from `_physics_process`).
- Enemy movement uses `NavigationAgent2D` or direct `move_and_slide()` toward the player, NOT the isometric transform. Enemies move in world space, not screen space.

## Open Questions

- Should the MOVE_SPEED value be adjusted for the isometric view? 190 may feel faster or slower than the Phaser version due to the coordinate transform.
- Should diagonal movement (two keys) feel the same speed as cardinal movement (one key)? Currently it does due to normalization, but the screen distance covered is different.
- Should there be acceleration/deceleration (easing into movement) or is instant full-speed acceptable?
- When pointer movement is implemented, should it override keyboard input or combine with it?
- Should controller (gamepad) input be supported? Godot's Input Map can map joystick axes to the same actions.
