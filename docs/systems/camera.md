# Camera System

## Summary

A Camera2D node follows the player through the dungeon. The camera is a child of the player's CharacterBody2D, providing automatic tracking with smoothing. Damage feedback uses a sprite flash effect (FlashFx) instead of camera shake.

## Current State

Camera2D is a child node of the Player scene, following the player automatically via position smoothing. Camera shake was removed due to motion sickness concerns. All damage/status feedback is handled by `FlashFx` (`scripts/ui/FlashFx.cs`) which tints the player sprite with colored flashes.

## Design

### Camera Node Configuration

The Camera2D is a direct child of the Player CharacterBody2D in the scene tree:

```
Player (CharacterBody2D)
  +-- Sprite (Sprite2D, directional character art)
  +-- CollisionShape2D (body collision)
  +-- AttackRange (Area2D)
  |     +-- AttackShape (CollisionShape2D, radius from primary attack range)
  +-- Camera2D
```

**Camera2D property values:**

| Property | Value | Default | Purpose |
|----------|-------|---------|---------|
| `enabled` | true | true | This is the active camera for the viewport |
| `position_smoothing_enabled` | true | false | Smooth follow with slight lag |
| `position_smoothing_speed` | 5.0 | 5.0 | How fast the camera catches up (units: factor per second) |
| `zoom` | Vector2(2, 2) | Vector2(1, 1) | 2x magnification for room |
| `ignore_rotation` | true | true | Camera stays upright even if player rotates |
| `process_callback` | CAMERA2D_PROCESS_PHYSICS | CAMERA2D_PROCESS_IDLE | Sync with physics for smooth movement |

### Camera as Child of Player

By making Camera2D a child of the Player node:
- The camera automatically follows the player's position every frame.
- No manual follow logic is needed.
- The `position_smoothing_enabled` property creates a slight lag -- the camera "catches up" to the player rather than being rigidly locked.

**Position smoothing behavior:**
At `position_smoothing_speed = 5.0`, the camera closes ~63% of the gap between its current position and the target (player) position every 0.2 seconds. This creates a very subtle trailing effect that smooths out sudden direction changes.

### Zoom Configuration

| Property | Value | Calculation |
|----------|-------|-------------|
| `zoom` | Vector2(2, 2) | 2x magnification on both axes |
| Effective viewport | 960 x 540 world pixels | 1920/2 x 1080/2 |

Room sizes range from 18-30+ tiles. At 2x zoom the room fills the viewport well and tiles are clearly visible.

### Damage Feedback: FlashFx System

Camera shake was removed. All damage and status feedback is delivered through `FlashFx` (`scripts/ui/FlashFx.cs`), which applies tween-based color modulation to any CanvasItem (typically the player's Sprite2D).

**FlashFx color definitions:**

| Name | Color | RGB | Usage |
|------|-------|-----|-------|
| Damage | Red | (1.0, 0.3, 0.3) | Player takes damage |
| Poison | Green | (0.4, 0.9, 0.3) | Poison tick |
| Curse | Purple | (0.7, 0.3, 0.9) | Curse effect |
| Boost | Yellow | (1.0, 0.9, 0.3) | Level up, buff |
| Shield | White | (1.0, 1.0, 1.0) | Shield/invincibility |
| Freeze | Ice blue | (0.4, 0.7, 1.0) | Freeze effect |
| Heal | Mint green | (0.3, 1.0, 0.6) | Healing |
| Crazed | Orange | (1.0, 0.4, 0.0) | Berserk/frenzy |

**FlashFx animation methods:**

| Method | Behavior | Parameters |
|--------|----------|------------|
| `Flash()` | Snap to color, fade back to white | `color`, `duration` (default 0.15s) |
| `DoublePulse()` | Flash twice quickly (good for buffs, crits) | `color`, `speed` (default 0.08s per phase) |
| `AlternateFlash()` | Two colors pulse back and forth | `colorA`, `colorB`, `pulses` (default 3), `speed` (default 0.06s) |
| `Linger()` | Snap to color, hold, then fade out | `color`, `holdTime`, `fadeTime` |

**Player flash methods (in Player.cs):**

| Method | FlashFx Call | Effect |
|--------|-------------|--------|
| `DamageFlash()` | `Flash(Damage)` | Red flash, 0.15s fade |
| `PoisonFlash()` | `Linger(Poison, 0.2, 0.3)` | Green hold + fade |
| `CurseFlash()` | `Linger(Curse, 0.15, 0.25)` | Purple hold + fade |
| `BoostFlash()` | `AlternateFlash(Boost, Shield)` | Yellow/white alternating |
| `ShieldFlash()` | `DoublePulse(Shield)` | White double pulse |
| `FreezeFlash()` | `Linger(Freeze, 0.3, 0.4)` | Ice blue hold + fade |
| `HealFlash()` | `DoublePulse(Heal)` | Mint green double pulse |
| `CrazedFlash()` | `AlternateFlash(Crazed, Damage, 4, 0.04)` | Orange/red rapid alternating |

**Trigger integration:**

`DamageFlash()` is called from `Enemy.DealDamageTo()` when the player takes contact damage (replaces the old `ShakeCamera()` call). `BoostFlash()` is called on level up via `OnStatsChanged()`.

### Process Callback

The Camera2D `process_callback` should be set to `CAMERA2D_PROCESS_PHYSICS` to match the player's `_PhysicsProcess()` movement. This prevents the camera from updating on a different frame than the player moves, which can cause micro-jitter.

### Viewport and Resolution

| Property | Value |
|----------|-------|
| Project viewport size | 1920 x 1080 |
| Stretch mode | `canvas_items` |
| Stretch aspect | `keep` |
| Camera zoom | Vector2(2, 2) |
| Effective world view | 960 x 540 pixels |

### Future Camera Features

| Feature | Description | Priority | Complexity |
|---------|-------------|----------|------------|
| Zoom transitions | Smooth zoom in/out on floor change | Medium | Low -- single tween on `zoom` property |
| Camera limits | Bound camera to room edges so it does not show void beyond the dungeon | Medium | Low -- set `limit_left/right/top/bottom` based on room size |
| Look-ahead | Camera leads slightly in the player's movement direction | Low | Medium -- offset based on velocity direction |
| Cinematic death | Slow zoom out when player dies | Low | Low -- tween zoom over 1 second |

## Implementation Notes

- Camera2D must be a child of the Player scene (not the room scene) so it automatically follows the player.
- Only one Camera2D should be `Enabled = true` at a time per viewport.
- FlashFx methods are all static and create self-cleaning tweens. Each call creates a new tween on the owner node, which is automatically freed on completion or when the owner is freed.
- Enemy sprites also use `FlashFx.Flash()` (white flash, 0.1s) when they take damage.

## Open Questions

- Should the camera zoom be configurable by the player (accessibility setting)?
- Should position_smoothing_speed be higher (8-10) for a tighter feel?
- Should the camera have a slight vertical offset (looking slightly ahead of the player's facing direction)?
