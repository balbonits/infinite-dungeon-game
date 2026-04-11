# Player

## Summary

The player is a CharacterBody2D with screen-space movement, data-driven class-based combat, and a following camera. Movement uses direct input mapping (up=up on screen, no isometric transform). The player auto-attacks the nearest enemy within range using an AttackConfig system that supports melee (instant hit + slash effect) and ranged (projectile spawn) without class-specific branching. Visual feedback uses a FlashFx system for damage, poison, boost, and other effects. See [controls.md](../ui/controls.md) for the full control scheme.

## Current State

Implemented. `scenes/player.tscn` and `scripts/Player.cs` are live. The player uses Sprite2D with PixelLab-generated art and 8-directional rotations via the DirectionalSprite utility. Three classes (Warrior, Ranger, Mage) are supported through AttackConfig records defined in `scripts/logic/ClassAttacks.cs`.

## Design

### Node Tree

```
Player (CharacterBody2D) [Player.cs]
  collision_layer = 2, collision_mask = 5, motion_mode = Floating
├── CollisionShape2D -> CircleShape2D(radius=12.0)
├── Sprite (Sprite2D) -> texture_filter=Nearest, offset=(0,-30), PixelLab art
├── Camera2D -> zoom=2x, smoothing=true, speed=5.0, process_callback=Physics
├── AttackRange (Area2D) -> collision_layer=0, collision_mask=4, monitoring=true
│   └── AttackShape (CollisionShape2D) -> CircleShape2D(radius=78.0, resized at runtime)
```

### Sprite System

The player uses Sprite2D with PixelLab-generated pixel art (not Polygon2D placeholders). Each class has a set of 8 directional rotation textures stored at paths defined in `Constants.Assets.PlayerClassRotations`:

- Warrior: `res://assets/characters/player/warrior/rotations/{direction}.png`
- Ranger: `res://assets/characters/player/ranger/rotations/{direction}.png`
- Mage: `res://assets/characters/player/mage/rotations/{direction}.png`

Directions are: south, south-west, west, north-west, north, north-east, east, south-east.

`DirectionalSprite.LoadRotations()` loads all 8 textures on `_Ready()`. `DirectionalSprite.UpdateSprite()` is called each physics frame to swap the sprite texture based on current velocity, using angle-based 8-way snapping. The sprite retains the last direction when idle.

### Constants

| Name | Value | Purpose |
|------|-------|---------|
| `MoveSpeed` | `190.0f` | Movement speed in pixels/second. Applied after normalizing input direction. |
| `GracePeriod` | `1.5f` | Seconds of invincibility on floor entry. |
| `GraceFlickerAlpha` | `0.4f` | Alpha during grace period flicker. |
| `BaseDamage` | `12` | Base attack damage before level scaling. |
| `DamagePerLevel` | `1.5f` | Damage added per player level. |

Damage formula: `BaseDamage + (int)(level * DamagePerLevel)`

| Level | Damage |
|-------|--------|
| 1 | 13 |
| 5 | 19 |
| 10 | 27 |
| 20 | 42 |

### Variables

| Name | Type | Default | Purpose |
|------|------|---------|---------|
| `_attackTimer` | `float` | `0.0f` | Countdown until next attack allowed. |
| `_graceTimer` | `float` | set in `StartGracePeriod()` | Grace period remaining. |
| `_isInvincible` | `bool` | `false` | True during grace period. |
| `_lastLevel` | `int` | from GameState | Tracks level for level-up detection. |
| `_lastDirection` | `string` | `"south"` | Last movement direction for sprite. |
| `_primaryAttack` | `AttackConfig` | class-dependent | Primary attack config loaded from ClassAttacks. |
| `_meleeFallback` | `AttackConfig?` | null (Mage: staff) | Melee fallback when enemy is close. Only Mage has one. |

### Node References

```csharp
private Area2D _attackArea = null!;
private Sprite2D _sprite = null!;
private Camera2D _camera = null!;
private Dictionary<string, Texture2D> _rotations = null!;
```

---

### Methods

#### `_Ready()`

1. Adds to `"player"` group
2. Gets node references (AttackRange, Sprite, Camera2D)
3. Loads class attack configs from `ClassAttacks.GetPrimary()` and `ClassAttacks.GetMeleeFallback()` based on `GameState.Instance.SelectedClass`
4. Resizes AttackRange collision shape radius to match primary attack range
5. Loads 8-directional class sprites via `DirectionalSprite.LoadRotations()`
6. Sets initial sprite to south-facing texture
7. Stores current level and connects to `GameState.StatsChanged` signal
8. Calls `StartGracePeriod()` for floor-entry invincibility

#### `_PhysicsProcess(double delta)`

Stops all movement if dead. Otherwise runs: `UpdateGracePeriod(delta)`, `HandleMovement()`, `HandleAttack(delta)`.

#### `HandleMovement()`

Reads input via `Input.GetVector()` using four directional actions. Movement is screen-space: pressing up moves the player up on screen (no isometric transform matrix). The input direction is normalized and multiplied by `MoveSpeed`. `DirectionalSprite.UpdateSprite()` swaps the sprite texture based on velocity direction. `MoveAndSlide()` handles physics movement and wall sliding.

#### `HandleAttack(double delta)`

Decrements `_attackTimer` by delta. When ready, finds the nearest enemy in the AttackRange Area2D. If the Mage's melee fallback exists and the target is within melee range, uses the fallback; otherwise uses the primary attack. Emits `EventBus.PlayerAttacked` after each attack.

#### `ExecuteAttack(AttackConfig attack, Node2D target)`

Unified attack execution that reads an AttackConfig record:
- Calculates `finalDamage = GetDamage(level) * attack.DamageMultiplier`
- Sets cooldown from `attack.Cooldown`
- If `attack.IsProjectile`: spawns a `Projectile` with configured speed, texture, scale, and tint
- If melee: calls `target.TakeDamage(damage)` directly and draws a slash effect

No class-specific branching -- the AttackConfig data drives all behavior.

#### `FindNearestEnemy()`

Iterates `_attackArea.GetOverlappingBodies()`, filters to `"enemies"` group, returns the closest by Euclidean distance. Returns null if none in range.

#### `DrawSlash(Vector2 targetPos, Color? color)`

Creates a temporary Polygon2D slash effect at the target position:
- 26x4 pixel rectangle, randomly rotated +/-1.2 radians
- Color from `attack.EffectColor` (default gold `#f5c86b`) at 95% alpha
- Fades out and rises 8px over 0.12 seconds via tween
- Auto-freed after animation

---

### Class-Based Combat (AttackConfig System)

All combat is data-driven via `AttackConfig` records in `scripts/logic/ClassAttacks.cs`. Each record defines range, cooldown, damage multiplier, projectile settings, and visual effects.

| Class | Attack | Type | Range | Cooldown | Dmg Mult | Notes |
|-------|--------|------|-------|----------|----------|-------|
| Warrior | Slash | Melee | 78px | 0.42s | 1.0x | Gold slash effect |
| Ranger | Arrow Shot | Projectile | 250px | 0.55s | 1.0x | Arrow texture, 400px/s, 0.6x scale |
| Mage | Magic Bolt | Projectile | 200px | 0.80s | 1.3x | Cyan tint `#4AE8E8`, 300px/s, 0.8x scale |
| Mage | Staff Melee | Melee (fallback) | 78px | 0.50s | 0.8x | Purple slash `#9B6BFF`, used when enemy is close |

The Mage automatically switches between Magic Bolt (ranged primary) and Staff Melee (close-range fallback) based on distance to target. Only the Mage has a melee fallback; Warrior and Ranger return null from `GetMeleeFallback()`.

The AttackRange Area2D radius is resized at runtime to match the primary attack range (78px for Warrior, 250px for Ranger, 200px for Mage).

---

### Grace Period (Floor-Entry Invincibility)

On floor entry (initial spawn or descent), `StartGracePeriod()` sets `_isInvincible = true` with a 1.5-second timer. During the grace period:
- Player cannot take damage (enemies check `player.IsInvincible`)
- Sprite flickers between full alpha and 0.4 alpha at ~10Hz

When the timer expires, invincibility ends and sprite modulate resets to white.

---

### Flash Effects System

All visual feedback delegates to the `FlashFx` utility for reusability. Available flash methods:

| Method | FlashFx Call | Color | Purpose |
|--------|-------------|-------|---------|
| `DamageFlash()` | `Flash` | Red (Damage) | Player takes hit |
| `PoisonFlash()` | `Linger(0.2s, 0.3s)` | Green (Poison) | Poison status |
| `CurseFlash()` | `Linger(0.15s, 0.25s)` | Purple (Curse) | Curse status |
| `BoostFlash()` | `AlternateFlash` | Gold+Blue (Boost+Shield) | Level up, buffs |
| `ShieldFlash()` | `DoublePulse` | Blue (Shield) | Shield active |
| `FreezeFlash()` | `Linger(0.3s, 0.4s)` | Cyan (Freeze) | Freeze status |
| `HealFlash()` | `DoublePulse` | Green (Heal) | Healing |
| `CrazedFlash()` | `AlternateFlash(4, 0.04s)` | Red+Red (Crazed+Damage) | Berserk status |

On level up, `OnStatsChanged()` detects the level increase, spawns a "Level Up" floating text, and triggers `BoostFlash()`.

---

### Collision Setup

| Property | Value | Binary | Description |
|----------|-------|--------|-------------|
| `collision_layer` | `2` | `0b010` | Player layer. Detected by enemy HitArea (mask bit 1). |
| `collision_mask` | `5` | `0b101` | Collides with walls (bit 0) and enemies (bit 2). |

- Wall tiles (layer 1) block player movement via `MoveAndSlide()`
- AttackRange (Area2D, mask 4) detects enemies for auto-targeting
- Enemy HitArea (Area2D, mask 2) detects player for contact damage
- `motion_mode = Floating` (1) for top-down style physics

## Implementation Notes

- The player scene is preloaded by `Dungeon.cs`: `GD.Load<PackedScene>(Constants.Assets.PlayerScene)`. Instanced once in `_Ready()` and added to the Entities container.
- Player spawns at the stairs-up position (where they "arrived" from): `_tileMap.MapToLocal(_stairsUpPosition)`.
- Camera2D is a child of Player, auto-follows with smoothing. `process_callback = Physics` ensures smooth tracking.
- `_attackTimer` starts at 0.0 allowing immediate first attack.
- `CreateTween()` ties tween lifetime to the player node.
- The `collision_mask` includes enemies (bit 2) in addition to walls, enabling `MoveAndSlide()` interaction with enemy bodies.

## Open Questions

- Should the attack target be cached between frames to avoid re-scanning every physics frame?
- Should there be a `MaxSpeed` that differs from `MoveSpeed` for future movement buffs/debuffs?
