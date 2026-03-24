# Camera System

## Summary

A Camera2D node follows the player through the isometric dungeon. The camera is a child of the player's CharacterBody2D, providing automatic tracking with smoothing. Camera shake triggers on player damage for visceral feedback.

## Current State

The Phaser prototype uses the built-in camera with `cameras.main.shake()` for damage feedback. The Godot version replaces this with a Camera2D child node on the player, using position smoothing for follow behavior and a tween-based offset animation for shake.

## Design

### Camera Node Configuration

The Camera2D is a direct child of the Player CharacterBody2D in the scene tree:

```
Player (CharacterBody2D)
  +-- Polygon2D (player sprite)
  +-- CollisionShape2D (body collision)
  +-- AttackRange (Area2D)
  |     +-- CollisionShape2D (78px radius)
  +-- Camera2D          <-- camera node
```

**Camera2D property values:**

| Property | Value | Default | Purpose |
|----------|-------|---------|---------|
| `enabled` | true | true | This is the active camera for the viewport |
| `position_smoothing_enabled` | true | false | Smooth follow with slight lag |
| `position_smoothing_speed` | 5.0 | 5.0 | How fast the camera catches up (units: factor per second) |
| `zoom` | Vector2(2, 2) | Vector2(1, 1) | 2x magnification for isometric room |
| `offset` | Vector2(0, 0) | Vector2(0, 0) | Used for shake effect, reset to zero after shake |
| `anchor_mode` | ANCHOR_MODE_DRAG_CENTER | ANCHOR_MODE_DRAG_CENTER | Camera centers on the node's position |
| `limit_left` | -10000000 (not set) | -10000000 | No left boundary for prototype |
| `limit_right` | 10000000 (not set) | 10000000 | No right boundary for prototype |
| `limit_top` | -10000000 (not set) | -10000000 | No top boundary for prototype |
| `limit_bottom` | 10000000 (not set) | 10000000 | No bottom boundary for prototype |
| `drag_horizontal_enabled` | false | false | Not using drag margins |
| `drag_vertical_enabled` | false | false | Not using drag margins |
| `ignore_rotation` | true | true | Camera stays upright even if player rotates |
| `process_callback` | CAMERA2D_PROCESS_PHYSICS | CAMERA2D_PROCESS_IDLE | Sync with physics for smooth movement |

### Camera as Child of Player

By making Camera2D a child of the Player node:
- The camera automatically follows the player's position every frame.
- No manual follow logic is needed (no `camera.position = player.position` in a script).
- The `position_smoothing_enabled` property creates a slight lag -- the camera "catches up" to the player rather than being rigidly locked. This feels cinematic.
- The `offset` property is relative to the player's position, making it ideal for shake effects (temporarily offset from player, then return to zero).

**Position smoothing behavior:**
At `position_smoothing_speed = 5.0`, the camera closes ~63% of the gap between its current position and the target (player) position every 0.2 seconds. In practice, this creates a very subtle trailing effect that is almost imperceptible during normal movement but smooths out sudden direction changes.

| Speed Value | Feel | Use Case |
|------------|------|----------|
| 1.0 | Very laggy, camera trails far behind | Cinematic, exploration |
| 3.0 | Noticeable but smooth lag | RPGs, adventure games |
| 5.0 | Subtle lag, feels natural | **Current setting** |
| 8.0 | Barely perceptible lag | Fast-paced action |
| 10.0+ | Nearly instant, almost no lag | Twitch gameplay |

### Zoom Configuration

| Property | Value | Calculation |
|----------|-------|-------------|
| `zoom` | Vector2(2, 2) | 2x magnification on both axes |
| Effective viewport | 960 x 540 world pixels | 1920/2 x 1080/2 |

**Zoom rationale:**

The isometric room is 10x10 tiles at 64x32 pixels per tile. In world space, the room spans approximately:
- Width: ~640 pixels (10 tiles x 64px, but isometric layout shifts rows)
- Height: ~320 pixels (10 tiles x 32px, but isometric layout stacks)
- Actual bounding box depends on isometric layout, roughly 640x480 for a 10x10 grid

At the default viewport resolution of 1920x1080:
- **1x zoom:** The room occupies roughly a 640x480 area in the center of a 1920x1080 viewport. That is only ~33% of the screen width and ~44% of the screen height. The room looks small and distant.
- **2x zoom:** The room occupies roughly a 1280x960 area in a 960x540 effective viewport. The room fills most of the screen. Tiles are clearly visible.
- **3x zoom:** The room overflows the viewport. Only ~6-7 tiles are visible in each direction. Player can not see the full room.

2x zoom is the balance point where the full room is visible but fills the viewport well.

**Adjusting zoom at runtime:**
```gdscript
# Smooth zoom transition:
var tween := create_tween()
tween.tween_property(camera, "zoom", Vector2(2.5, 2.5), 0.5)
```

### Camera Shake Algorithm

Triggered when the player takes damage from an enemy contact. Provides tactile feedback that the player was hit.

**Implementation:**
```gdscript
func shake_camera() -> void:
    var tween := create_tween()
    var shake_offset := Vector2(
        randf_range(-3.0, 3.0),
        randf_range(-3.0, 3.0)
    )
    tween.tween_property(camera, "offset", shake_offset, 0.045)
    tween.tween_property(camera, "offset", Vector2.ZERO, 0.045)
```

**Step-by-step breakdown:**
1. Create a new tween (each shake gets its own tween instance).
2. Generate a random offset: x in [-3, 3] pixels, y in [-3, 3] pixels.
3. Phase 1 (0.045s): Animate `camera.offset` from current position to `shake_offset`.
4. Phase 2 (0.045s): Animate `camera.offset` from `shake_offset` back to `Vector2.ZERO`.
5. Total duration: 0.09 seconds (90ms).
6. The tween is automatically freed when it completes.

**Shake parameters:**

| Parameter | Value | Unit | Phaser Equivalent |
|-----------|-------|------|-------------------|
| Offset range (X) | -3.0 to +3.0 | pixels | `shake(90, 0.0035)` intensity * viewport width ~= 3.5px |
| Offset range (Y) | -3.0 to +3.0 | pixels | Same |
| Phase 1 duration | 0.045 | seconds | N/A (Phaser uses oscillation) |
| Phase 2 duration | 0.045 | seconds | N/A |
| Total duration | 0.090 | seconds | `shake(90, ...)` = 90ms |
| Shape | Single offset + return | -- | Continuous oscillation (sine-like) |
| Cooldown | None | -- | None |

**Phaser shake comparison:**
```javascript
// Phaser prototype:
this.cameras.main.shake(90, 0.0035);
```

Phaser's `shake(duration, intensity)`:
- `duration`: 90ms -- the camera shakes for this period.
- `intensity`: 0.0035 -- fraction of the camera viewport size. At 1100px wide viewport: `1100 * 0.0035 = 3.85px`. At 700px tall: `700 * 0.0035 = 2.45px`.
- Phaser applies a continuous random offset each frame during the shake duration, creating an oscillating/jittery effect.

The Godot version is simpler: one offset and one return. This produces a single "jolt" rather than continuous jitter. The visual effect is similar at 90ms total but feels slightly cleaner.

**Overlapping shakes:**
If the player is hit again while a shake tween is still running:
- A new tween is created independently.
- The new tween overwrites the `offset` property.
- The old tween continues but its writes are immediately overwritten by the new tween.
- Net effect: the camera shakes again from whatever offset it was at. This looks natural -- rapid hits produce rapid jolts.

**Potential enhancement -- multi-oscillation shake:**
For a closer match to Phaser's continuous oscillation:
```gdscript
func shake_camera_oscillating() -> void:
    var tween := create_tween()
    for i in range(4):  # 4 oscillations
        var offset := Vector2(
            randf_range(-3.0, 3.0),
            randf_range(-3.0, 3.0)
        )
        tween.tween_property(camera, "offset", offset, 0.0225)
    tween.tween_property(camera, "offset", Vector2.ZERO, 0.0225)
    # Total: 5 * 0.0225 = 0.1125s (~112ms)
```

### Trigger Integration

Camera shake is called from the player's damage handler:

```gdscript
# In player.gd:
func take_damage(amount: int) -> void:
    GameState.hp -= amount
    shake_camera()
    # ... update HUD, check death, etc.
```

The `shake_camera()` function accesses the Camera2D node:
```gdscript
@onready var camera: Camera2D = $Camera2D

func shake_camera() -> void:
    var tween := create_tween()
    var shake_offset := Vector2(randf_range(-3.0, 3.0), randf_range(-3.0, 3.0))
    tween.tween_property(camera, "offset", shake_offset, 0.045)
    tween.tween_property(camera, "offset", Vector2.ZERO, 0.045)
```

### Process Callback

The Camera2D `process_callback` should be set to `CAMERA2D_PROCESS_PHYSICS` to match the player's `_physics_process()` movement. This prevents the camera from updating on a different frame than the player moves, which can cause micro-jitter.

| Setting | Updates During | Best For |
|---------|---------------|----------|
| `CAMERA2D_PROCESS_IDLE` (default) | `_process()` frame | UI cameras, non-physics games |
| `CAMERA2D_PROCESS_PHYSICS` | `_physics_process()` frame | **Physics-based movement (use this)** |

### Viewport and Resolution

| Property | Value |
|----------|-------|
| Project viewport size | 1920 x 1080 |
| Stretch mode | `canvas_items` |
| Stretch aspect | `keep` |
| Camera zoom | Vector2(2, 2) |
| Effective world view | 960 x 540 pixels |

The viewport is configured in `project.godot`:
```
[display]
window/size/viewport_width=1920
window/size/viewport_height=1080
window/stretch/mode="canvas_items"
window/stretch/aspect="keep"
```

### Future Camera Features

| Feature | Description | Priority | Complexity |
|---------|-------------|----------|------------|
| Zoom transitions | Smooth zoom in/out on floor change (e.g., zoom to 1x during transition, then back to 2x) | Medium | Low -- single tween on `zoom` property |
| Camera limits | Bound camera to room edges so it does not show void beyond the dungeon | Medium | Low -- set `limit_left/right/top/bottom` based on room size |
| Look-ahead | Camera leads slightly in the player's movement direction, showing more of what is ahead | Low | Medium -- offset based on velocity direction |
| Cinematic death | Slow zoom out when player dies, dramatic framing | Low | Low -- tween zoom to Vector2(1.5, 1.5) over 1 second |
| Room transition pan | Camera pans from old room to new room during floor change | Low | Medium -- detach from player, tween to new position, reattach |
| Minimap camera | Secondary Camera2D at lower zoom for a minimap viewport | Low | Medium -- SubViewport + secondary Camera2D |

**Camera limits calculation (for future):**
```gdscript
# Given a 10x10 tile room:
var room_min := tile_map.map_to_local(Vector2i(0, 0))
var room_max := tile_map.map_to_local(Vector2i(ROOM_SIZE - 1, ROOM_SIZE - 1))
camera.limit_left = int(room_min.x) - 64   # Padding
camera.limit_top = int(room_min.y) - 32
camera.limit_right = int(room_max.x) + 64
camera.limit_bottom = int(room_max.y) + 32
```

## Implementation Notes

- Camera2D must be a child of the Player scene (not the room scene) so it automatically follows the player.
- Only one Camera2D should be `enabled = true` at a time per viewport. If multiple cameras exist (e.g., minimap), use separate SubViewports.
- The `offset` property is in local coordinates relative to the camera's parent (the player). A shake offset of `Vector2(3, 0)` moves the camera 3 pixels right of the player in world space.
- At 2x zoom, a 3-pixel shake offset appears as 6 pixels of movement on screen. This is subtle but noticeable.
- `create_tween()` called on the player node creates a tween bound to that node. If the player is freed (e.g., scene change), the tween is automatically killed. No manual cleanup needed.

## Open Questions

- Should the camera zoom be configurable by the player (accessibility setting)?
- Should position_smoothing_speed be higher (8-10) for a tighter feel, or is 5.0 good for the isometric aesthetic?
- Should camera shake intensity scale with damage amount (bigger hit = bigger shake)?
- Should there be screen flash (white overlay flash) in addition to camera shake on damage?
- Should the camera have a slight vertical offset (looking slightly ahead of the player's facing direction)?
