# Visual Effects

## Summary

All visual effects in the game: slash marks on enemy hits and camera shake on player damage. Both effects are transient (created, animated, destroyed) and use Godot's tween system for animation. This document specifies every parameter value, the trigger condition, the node type, and the full lifecycle for each effect.

## Current State

Two effects are implemented:
- **Slash effect:** gold rectangle on the attacked enemy, fades and rises over 120ms
- **Camera shake:** ±3px camera offset displacement over 90ms when the player takes damage

Both match the Phaser prototype's visual behavior exactly.

## Design

### Effect 1: Slash Effect

#### Overview

A thin golden rectangle that appears on an enemy when the player attacks it. The slash rotates to a random angle, fades to transparent, and drifts upward over 120 milliseconds before being freed from memory.

#### Trigger Condition

The slash effect is created in `player.gd.handle_attack()` immediately after a successful auto-attack:

```
player.handle_attack(delta)
  → Attack cooldown elapsed? Yes
  → Nearest enemy in AttackRange? Yes
  → nearest_enemy.take_damage(damage)
  → draw_slash(nearest_enemy.global_position)  ← TRIGGER
  → EventBus.player_attacked.emit(nearest_enemy)
```

The slash fires on EVERY successful attack, regardless of whether the enemy dies from the hit. One attack = one slash.

#### Node Type

`Polygon2D` -- a filled polygon rendered with a solid color.

**Why Polygon2D:** Matches the Phaser prototype's `this.add.rectangle()` approach. Polygon2D draws an arbitrary filled shape without requiring a texture asset. When pixel art is available, the slash could become a Sprite2D with an animated texture or an AnimatedSprite2D with a slash animation spritesheet.

#### Creation Parameters

| Parameter | Value | Explanation |
|-----------|-------|-------------|
| **Node type** | `Polygon2D` | Filled polygon, no texture needed |
| **polygon** | `PackedVector2Array([Vector2(-13, -2), Vector2(13, -2), Vector2(13, 2), Vector2(-13, 2)])` | A 26x4 pixel thin horizontal rectangle. 26px wide (±13 from center), 4px tall (±2 from center). Represents a quick sword slash mark. |
| **color** | `Color("#f5c86b", 0.95)` | Gold/accent color at 95% opacity. Hex `#f5c86b` = RGB(245, 200, 107). Matches CSS `var(--accent)` and Phaser's `COLORS.sword = 0xf5c86b`. The 0.95 opacity (not 1.0) adds a subtle translucency that makes the slash feel like a light effect rather than a solid object. |
| **global_position** | `target_pos` (enemy's `global_position` at the moment of the attack) | The slash appears centered on the enemy. In isometric view, this places the slash directly over the enemy's diamond sprite. |
| **rotation** | `randf_range(-1.2, 1.2)` radians | Random angle between approximately -68.75 degrees and +68.75 degrees. Each slash appears at a different angle, preventing the visual from looking repetitive when the player attacks the same enemy multiple times in succession. The range of 1.2 radians was chosen to keep slashes roughly horizontal -- they never go fully vertical (which would appear as a thin line and look broken). |
| **parent** | `get_parent()` (Entities Node2D container) | The slash is added to the player's parent, which is the Entities container with `y_sort_enabled = true`. This ensures the slash sorts correctly with other entities in the isometric view. If it were added as a child of the player, it would move with the player instead of staying at the target position. |

#### Polygon Shape Detail

```
(-13, -2) ────────── (13, -2)
    │                     │
    │   26px × 4px rect   │
    │                     │
(-13,  2) ────────── (13,  2)
```

**Why 26x4 and not bigger/smaller:**
- Width (26px): Approximately twice the enemy's collision radius (10px). The slash extends slightly beyond the enemy's body on both sides, creating a satisfying "through slash" visual. Much smaller would be invisible; much larger would look disproportionate.
- Height (4px): Thin enough to look like a slash mark rather than a bar. Thick enough to be visible at 2x camera zoom (renders as ~8 screen pixels).

#### Animation Parameters

| Property | Start Value | End Value | Duration | Easing |
|----------|-------------|-----------|----------|--------|
| `modulate:a` (alpha) | `1.0` (fully opaque, via Polygon2D color's 0.95 alpha combined with modulate) | `0.0` (fully transparent) | `0.12` seconds (120ms) | Default (linear) |
| `position:y` | `slash.position.y` (enemy's Y position in local space) | `slash.position.y - 8` (8 pixels upward) | `0.12` seconds (120ms) | Default (linear) |

**Both animations run in parallel** -- the fade and the rise happen simultaneously, not sequentially.

#### Tween Configuration

```gdscript
var tween: Tween = create_tween()
tween.set_parallel(true)
tween.tween_property(slash, "modulate:a", 0.0, 0.12)
tween.tween_property(slash, "position:y", slash.position.y - 8, 0.12)
tween.set_parallel(false)
tween.tween_callback(slash.queue_free)
```

**`create_tween()`:** Creates a new tween bound to the player node. If the player is freed, the tween is automatically killed (cleaning up the slash).

**`set_parallel(true)`:** Subsequent tween calls run at the same time. Without this, the rise would happen AFTER the fade (0.12s fade, then 0.12s rise = 0.24s total), which would look wrong -- a fully transparent slash would be rising.

**`tween_property(slash, "modulate:a", 0.0, 0.12)`:** Tweens the `modulate` Color's alpha channel from its current value (1.0) to 0.0 over 0.12 seconds. `modulate` is a Color that multiplies the node's base color. Setting alpha to 0 makes the node invisible.

**`tween_property(slash, "position:y", slash.position.y - 8, 0.12)`:** Tweens the Y position 8 pixels upward (in screen space, "upward" is negative Y). This creates a "floating away" visual.

**`set_parallel(false)`:** Switches back to sequential mode so the callback runs AFTER both parallel tweens complete.

**`tween_callback(slash.queue_free)`:** When the tween finishes (after 0.12s), the slash node is freed from memory. Without this cleanup, slash nodes would accumulate indefinitely, leaking memory and potentially affecting performance after hundreds of attacks.

#### Lifecycle

```
Frame N: Player attacks enemy
  1. Polygon2D created (26x4 gold rectangle)
  2. Position set to enemy's global_position
  3. Rotation randomized: -1.2 to 1.2 radians
  4. Added to Entities container
  5. Tween created: fade + rise over 0.12s

Frame N to N+~7 (0.12s at 60fps ≈ 7 frames):
  6. Each frame: alpha decreases, position.y decreases
  7. Slash becomes progressively more transparent and higher

Frame N+~7: Tween completes
  8. tween_callback fires
  9. slash.queue_free() called
  10. Slash node removed from scene tree and freed from memory
```

**Total lifetime:** ~0.12 seconds (7 frames at 60fps). Multiple slashes can exist simultaneously if the player attacks faster than 0.12s (impossible with 0.42s cooldown) or if old slashes haven't finished fading yet (also impossible at current cooldown).

#### Phaser Equivalent

```javascript
// Phaser prototype
drawSlash(x, y) {
    const slash = this.add.rectangle(x, y, 26, 4, COLORS.sword, 0.95);
    slash.rotation = Phaser.Math.FloatBetween(-1.2, 1.2);
    this.tweens.add({
        targets: slash,
        alpha: 0,
        y: y - 8,
        duration: 120,
        onComplete: () => slash.destroy()
    });
}
```

**Mapping:**
| Phaser | Godot |
|--------|-------|
| `this.add.rectangle(x, y, 26, 4, color, 0.95)` | `Polygon2D.new()` with rectangle polygon, color, position |
| `slash.rotation = FloatBetween(-1.2, 1.2)` | `slash.rotation = randf_range(-1.2, 1.2)` |
| `this.tweens.add({targets, alpha, y, duration})` | `create_tween().tween_property(...)` |
| `onComplete: () => slash.destroy()` | `tween_callback(slash.queue_free)` |
| `duration: 120` (milliseconds) | `0.12` (seconds) |

---

### Effect 2: Camera Shake

#### Overview

A brief camera displacement that occurs when the player takes damage. The Camera2D's `offset` property is tweened to a random position, then back to center, creating a quick "jolt" effect. Total duration is 90 milliseconds.

#### Trigger Condition

Camera shake is triggered in `enemy.gd._deal_damage_to()` after dealing damage to the player:

```
Enemy._deal_damage_to(player_node)
  → GameState.take_damage(3 + danger_tier)
  → player_node.shake_camera()  ← TRIGGER
  → hit_cooldown.start()
```

The shake fires on EVERY successful damage tick, including:
- First contact (player enters HitArea)
- Subsequent ticks (HitCooldownTimer timeout while player still overlapping)
- Multiple enemies can trigger shakes in the same frame (overlapping shakes)

#### Target Node

`Camera2D` -- child of the Player node.

**Property tweened:** `offset` (Vector2) -- a rendering-only displacement that shifts the camera's view without moving the camera node in the scene tree.

**Why offset and not position:** `position` is affected by the camera's position smoothing (it interpolates toward the parent's position). Tweening `position` would fight against the smoothing system, creating jittery movement. `offset` is applied AFTER position smoothing, so the shake is cleanly layered on top of the follow behavior.

#### Animation Parameters

**Phase 1: Displacement (0.045 seconds)**

| Property | Start Value | End Value | Duration |
|----------|-------------|-----------|----------|
| `offset` | Current value (normally `Vector2.ZERO`) | `Vector2(randf_range(-3, 3), randf_range(-3, 3))` | `0.045` seconds (45ms) |

**Phase 2: Return (0.045 seconds)**

| Property | Start Value | End Value | Duration |
|----------|-------------|-----------|----------|
| `offset` | (wherever Phase 1 ended) | `Vector2.ZERO` | `0.045` seconds (45ms) |

**Phases are sequential** -- the return happens AFTER the displacement completes.

#### Shake Parameters Detail

| Parameter | Value | Explanation |
|-----------|-------|-------------|
| **Displacement X** | `randf_range(-3, 3)` | Random value between -3 and +3 pixels. Each shake has a different horizontal direction. |
| **Displacement Y** | `randf_range(-3, 3)` | Random value between -3 and +3 pixels. Each shake has a different vertical direction. Independent of X. |
| **Max displacement** | ~4.24 pixels (diagonal: sqrt(3^2 + 3^2)) | The maximum possible displacement when both X and Y are at their extremes. |
| **Displacement duration** | `0.045` seconds (45ms) | Time to move camera from center to the displaced position. |
| **Return duration** | `0.045` seconds (45ms) | Time to move camera from displaced position back to center. |
| **Total duration** | `0.09` seconds (90ms) | Matches Phaser's `this.cameras.main.shake(90, 0.0035)` exactly. |
| **Easing** | Linear (default) | No easing applied. The displacement and return are constant-speed movements. |

#### Intensity Comparison with Phaser

Phaser's `shake(duration, intensity)`:
- `duration = 90` milliseconds
- `intensity = 0.0035` (fraction of viewport dimensions)
- Phaser viewport: 1100x700
- Horizontal shake range: 1100 * 0.0035 = 3.85 pixels
- Vertical shake range: 700 * 0.0035 = 2.45 pixels

Godot implementation:
- Total duration: 0.09 seconds (90ms) -- exact match
- Horizontal shake range: ±3 pixels (6px total range)
- Vertical shake range: ±3 pixels (6px total range)
- Slightly less horizontal range, slightly more vertical range than Phaser

The ±3 pixel range was chosen as a clean round number that closely approximates Phaser's intensity. At 2x camera zoom, the on-screen displacement is ±6 pixels, which is noticeable without being disorienting.

#### Tween Configuration

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

**`create_tween()`:** Creates a tween bound to the player node. Sequential by default (no `set_parallel`), so Phase 2 starts after Phase 1 completes.

**First `tween_property`:** Animates `camera.offset` from its current value to a random displacement over 0.045 seconds.

**Second `tween_property`:** Animates `camera.offset` from the displaced position back to `Vector2.ZERO` over 0.045 seconds.

**No cleanup callback needed:** The tween completes and is automatically freed. The camera offset ends at `Vector2.ZERO`, so no residual displacement remains.

#### Edge Cases

**Overlapping shakes (rapid damage):**

If the player takes damage twice within 90ms (e.g., two enemies hit simultaneously), two tweens are created. Both modify `camera.offset` concurrently. Behavior:
- The second tween's Phase 1 overwrites whatever the first tween is doing to `offset`
- Both tweens' Phase 2 target `Vector2.ZERO`, so the camera always returns to center
- Visual result: slightly more intense shake (two rapid jolts), which appropriately signals heavier damage
- No crash or stuck offset -- the last tween to complete sets offset to ZERO

**Shake during death:**

When the player takes lethal damage, `shake_camera()` fires and then `GameState.take_damage()` triggers the death sequence (pause game, show death screen). The tree is paused (`get_tree().paused = true`), which pauses the tween. The camera offset may be stuck at a non-zero value during the death screen. This is acceptable because:
1. The death screen covers the game view with a semi-transparent overlay
2. On restart, the scene is reloaded, creating a new Camera2D with default offset `Vector2.ZERO`

**Shake when no camera exists:**

The `camera` variable is set via `@onready`. If `shake_camera()` is somehow called before `_ready()` (shouldn't happen), `camera` would be null and the tween creation would fail silently. The `has_method("shake_camera")` check in `enemy.gd._deal_damage_to()` ensures the method exists, but doesn't guard against null camera.

#### Lifecycle

```
Frame N: Player takes damage
  1. enemy._deal_damage_to() calls player.shake_camera()
  2. Tween created (sequential: Phase 1 then Phase 2)

Frame N to N+~3 (Phase 1: 0.045s ≈ 2-3 frames at 60fps):
  3. camera.offset animates from (0, 0) to random (±3, ±3)
  4. Screen view shifts suddenly

Frame N+~3 to N+~5 (Phase 2: 0.045s ≈ 2-3 frames at 60fps):
  5. camera.offset animates from displaced position back to (0, 0)
  6. Screen view returns to center

Frame N+~5: Tween completes
  7. Tween auto-freed by Godot
  8. Camera offset is exactly Vector2.ZERO
```

**Total lifetime:** ~0.09 seconds (5-6 frames at 60fps). The shake is extremely fast -- just enough to register as a "hit" visual cue without disrupting gameplay or causing motion sickness.

#### Phaser Equivalent

```javascript
// Phaser prototype
this.cameras.main.shake(90, 0.0035);
```

Phaser's `shake()` is a single function call that handles the entire effect internally. Godot has no built-in camera shake method, so it's implemented manually via a tween on the `offset` property.

**Mapping:**
| Phaser | Godot |
|--------|-------|
| `this.cameras.main.shake(90, 0.0035)` | Manual tween on Camera2D.offset |
| Duration: 90ms | Two phases: 45ms + 45ms = 90ms |
| Intensity: 0.0035 of viewport | ±3 pixels (approximation) |
| Continuous random shake | Single displacement + return |

**Behavioral difference:** Phaser's `shake()` continuously randomizes the camera position throughout the duration (multiple random offsets over 90ms). The Godot implementation uses a single displacement + return, creating a cleaner "jolt" motion. The visual result is similar but not identical -- Phaser's shake is more chaotic, Godot's is more directed.

---

### Future Effects (Planned)

These effects are not yet implemented but are listed for future reference. Each includes a preliminary specification.

#### Hit Flash

**Purpose:** Brief white flash on an enemy sprite when it takes damage, confirming the hit visually.

| Parameter | Proposed Value | Rationale |
|-----------|---------------|-----------|
| Trigger | `enemy.take_damage()` | Every damage event |
| Method | Tween `modulate` to `Color.WHITE` then back to original | Modulate multiplies the sprite color; white = fully bright |
| Flash duration | 0.05 seconds (50ms) | Quick enough to feel responsive, long enough to notice |
| Return duration | 0.05 seconds (50ms) | Smooth return to normal color |
| Total | 0.1 seconds (100ms) | Slightly shorter than the slash effect |

```gdscript
# Pseudocode
func flash_hit() -> void:
    var original_modulate: Color = sprite.modulate
    var tween = create_tween()
    tween.tween_property(sprite, "modulate", Color.WHITE, 0.05)
    tween.tween_property(sprite, "modulate", original_modulate, 0.05)
```

#### Death Particles

**Purpose:** Small colored fragments that burst outward when an enemy dies, reinforcing the "defeated" moment.

| Parameter | Proposed Value | Rationale |
|-----------|---------------|-----------|
| Trigger | `enemy.take_damage()` when HP <= 0 | Only on death, not every hit |
| Method | GPUParticles2D (one-shot) or multiple tiny Polygon2D nodes with tweens | GPUParticles2D is more performant for many particles |
| Particle count | 6-10 | Enough to be visible, few enough to be performant |
| Particle color | Enemy's tier color | Thematic consistency |
| Particle shape | Small diamonds (4x4 px) | Match the enemy's diamond shape |
| Spread | 360 degrees, velocity 50-150 px/s | Burst in all directions |
| Lifetime | 0.3 seconds | Quick burst that doesn't linger |
| Gravity | Slight downward pull | Particles arc down naturally |

#### Level-Up Glow

**Purpose:** Brief golden aura around the player when leveling up, celebrating the achievement.

| Parameter | Proposed Value | Rationale |
|-----------|---------------|-----------|
| Trigger | `GameState.award_xp()` when level increases | Only on level-up |
| Method | Expanding circle Polygon2D that fades out | Simple, no particle system needed |
| Color | `Color("#f5c86b", 0.5)` | Gold accent, semi-transparent |
| Start radius | 20 pixels | Slightly larger than player |
| End radius | 60 pixels | Expands noticeably |
| Duration | 0.4 seconds | Slow enough to appreciate |
| Fade | Alpha 0.5 to 0.0 over the duration | Gradual disappearance |

#### Heal Effect

**Purpose:** Green particles rising from the player when healed on level-up.

| Parameter | Proposed Value | Rationale |
|-----------|---------------|-----------|
| Trigger | `GameState.award_xp()` when heal occurs (level up) | Combined with level-up |
| Method | 4-6 small green Polygon2D nodes rising upward with tween | Simple, matches slash effect pattern |
| Color | `Color("#76f79f", 0.8)` | Green safe color from CSS `var(--safe)` |
| Shape | Small plus signs or circles (4px) | Medical/healing association |
| Rise distance | 20-30 pixels upward | Drift away from player |
| Duration | 0.5 seconds | Slightly longer than slash for a "floating" feel |
| Spread | Random X offset ±10 pixels | Particles don't all go straight up |

#### Damage Numbers (Optional)

**Purpose:** Floating text showing damage dealt when the player attacks an enemy.

| Parameter | Proposed Value | Rationale |
|-----------|---------------|-----------|
| Trigger | `player.handle_attack()` after dealing damage | Every attack |
| Method | Label node with tween (rise + fade) | Same pattern as slash effect |
| Text | String of the damage number (e.g., "13") | Direct damage feedback |
| Font color | `Color("#f5c86b")` for player damage, `Color("#ff6f6f")` for enemy damage to player | Color-coded by source |
| Font size | 14 | Small but readable at 2x zoom |
| Position | Above the target entity | Clear association with the hit |
| Rise distance | 20 pixels | Float above the entity |
| Duration | 0.6 seconds | Long enough to read |
| Fade | Start fading at 0.3s, fully transparent by 0.6s | Visible for half the duration, then fades |

**Note:** Damage numbers are optional and may be toggled via a setting. They add visual noise that not all players want.

## Implementation Notes

- All transient effects (slash, particles, etc.) should be added to the Entities container (not as children of the player or enemy) so they y-sort correctly and don't move with the entity after creation.
- Tweens created via `create_tween()` on a node are tied to that node's lifetime. If the node is freed, the tween stops. For slash effects, the tween is created on the player (not the slash) because the player persists -- if it were on the slash, freeing the slash would kill the tween prematurely.
- `queue_free()` in tween callbacks is safe because the callback runs at a point where the node is not being iterated over by the scene tree. Godot defers the actual freeing to the end of the frame.
- Camera shake uses the Camera2D's `offset` property specifically to avoid interfering with `position_smoothing`. The smoothing system controls `position`; the shake controls `offset`. They don't conflict.
- All effect durations are intentionally short (0.09s - 0.12s). The game runs at 60fps, so these effects last only 5-7 frames. They provide visual feedback without slowing down the fast-paced combat flow.

## Open Questions

- Should the slash effect use an AnimatedSprite2D with a proper slash animation when pixel art is available?
- Should camera shake intensity scale with damage amount (bigger hit = bigger shake)?
- Should there be a screen flash (red tint) in addition to camera shake on player damage?
- Should effects have a pooling system (pre-create and reuse) instead of create/free each time?
- Should the damage number effect be included in the MVP or deferred?
- Should the camera shake implementation be moved to a dedicated utility function / autoload for reuse by future effects (e.g., boss attacks)?
