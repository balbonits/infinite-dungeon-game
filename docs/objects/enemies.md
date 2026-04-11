# Enemies

## Summary

Enemies are CharacterBody2D instances with a configurable `Level` that determines all their stats: HP, speed, damage, and XP reward. They chase the player in a straight line, deal contact damage on a 0.7s cooldown, and emit a signal on death that triggers respawning. Color is determined by a smooth gradient based on the gap between enemy level and player level, updating dynamically when the player levels up. Two species (skeleton, goblin) use the same `enemy.tscn` scene with different sprite sets.

## Current State

Implemented. `scenes/enemy.tscn` and `scripts/Enemy.cs` are live. Enemies use Sprite2D with PixelLab-generated art at 0.7x scale, 8-directional rotations via DirectionalSprite, a level label above each enemy, and a color gradient system with 8 anchors and smooth interpolation.

## Design

### Node Tree

```
Enemy (CharacterBody2D) [Enemy.cs]
  collision_layer = 4, collision_mask = 3, motion_mode = Floating
├── CollisionShape2D -> CircleShape2D(radius=10.0)
├── Sprite (Sprite2D) -> texture_filter=Nearest, scale=(0.7,0.7), offset=(0,-26)
├── LevelLabel (Label) -> "Lv.X", font_size=11, outline_size=3, centered
├── HitArea (Area2D) -> collision_layer=0, collision_mask=2, monitoring=true
│   └── HitShape (CollisionShape2D) -> CircleShape2D(radius=15.0)
└── HitCooldownTimer (Timer) -> wait_time=0.7, one_shot=true
```

### Sprite System

Enemies use Sprite2D with PixelLab-generated pixel art at 0.7x scale (smaller than the player). Each species has 8-directional rotation textures stored at paths defined in `Constants.Assets.EnemySpeciesRotations`:

- Skeleton (index 0): `res://assets/characters/enemies/skeleton/rotations/{direction}.png`
- Goblin (index 1): `res://assets/characters/enemies/goblin/rotations/{direction}.png`

`DirectionalSprite.LoadRotations()` loads textures in `_Ready()` based on `SpeciesIndex`. `DirectionalSprite.UpdateSprite()` updates the sprite each physics frame based on movement velocity toward the player.

---

### Exported Properties

| Name | Type | Default | Purpose |
|------|------|---------|---------|
| `Level` | `int` | `1` | Determines all enemy stats. Set by `Dungeon.cs` before adding to scene tree. |
| `SpeciesIndex` | `int` | `0` (Skeleton) | Which species sprite set to use. Maps to `EnemySpecies` enum and `Constants.Assets.EnemySpeciesRotations` array. |

---

### Level-Based Stats

All stats scale linearly with level:

| Stat | Formula | Lv.1 | Lv.3 | Lv.5 | Lv.10 | Lv.20 |
|------|---------|------|------|------|-------|-------|
| HP | `20 + level * 10` | 30 | 50 | 70 | 120 | 220 |
| Speed (px/s) | `50 + level * 5` | 55 | 65 | 75 | 100 | 150 |
| Damage | `2 + level * 1` | 3 | 5 | 7 | 12 | 22 |
| XP Reward | `8 + level * 4` | 12 | 20 | 28 | 48 | 88 |

Constants are defined in `Constants.EnemyStats`:

| Constant | Value |
|----------|-------|
| `BaseHp` | `20` |
| `HpPerLevel` | `10` |
| `BaseSpeed` | `50.0f` |
| `SpeedPerLevel` | `5.0f` |
| `BaseDamage` | `2` |
| `DamagePerLevel` | `1` |
| `BaseXp` | `8` |
| `XpPerLevel` | `4` |
| `HitCooldown` | `0.7f` |

---

### Color Gradient System

Enemy color is based on the level gap (`enemyLevel - playerLevel`) with 8 anchor points and smooth linear interpolation between them:

| Gap | Color | Hex | Meaning |
|-----|-------|-----|---------|
| -10 | Grey | `#9D9D9D` | Trivial |
| -6 | Blue | `#4A7DFF` | Low |
| -3 | Cyan | `#4AE8E8` | Low-mid |
| 0 | Green | `#6BFF89` | Even |
| +3 | Yellow | `#FFDE66` | Mid-high |
| +6 | Gold | `#F5C86B` | High |
| +8 | Orange | `#FF9340` | Very high |
| +10 | Red | `#FF6F6F` | Extreme |

**Interpolation:** For gaps between anchors, `Color.Lerp()` blends smoothly. For example, gap +1.5 produces a color halfway between Green and Yellow. Gaps beyond the extreme anchors (-10 or +10) clamp to the nearest anchor color.

**Dynamic updates:** `UpdateColor()` is connected to `GameState.StatsChanged`. When the player levels up, all living enemies recalculate their color to reflect the new gap. An enemy that was Yellow (gap +3) may become Green (gap +2) or Cyan (gap 0) after player level-ups.

The color is applied to both `_sprite.Modulate` and `_levelLabel.Modulate`, tinting the entire enemy consistently.

---

### Level Label

Each enemy displays a level label ("Lv.X") above the sprite:
- Font size 11, black outline (size 3), white text at 90% alpha
- Centered horizontally, positioned at Y=-56 (above the sprite)
- Tinted with the same gradient color as the sprite
- Text set once in `_Ready()` via `Strings.Enemy.LevelLabel(Level)`

---

### Variables

| Name | Type | Default | Set In | Purpose |
|------|------|---------|--------|---------|
| `_hp` | `int` | from formula | `_Ready()` | Current hit points. |
| `_moveSpeed` | `float` | from formula | `_Ready()` | Movement speed in px/s. |
| `_damage` | `int` | from formula | `_Ready()` | Contact damage per hit. |
| `_xpReward` | `int` | from formula | `_Ready()` | XP awarded on kill. |
| `_currentColor` | `Color` | from gradient | `UpdateColor()` | Current tint, used for flash recovery. |
| `_lastDirection` | `string` | `"south"` | per frame | Last sprite direction. |

### Node References

```csharp
private Sprite2D _sprite = null!;
private Label _levelLabel = null!;
private Area2D _hitArea = null!;
private Timer _hitCooldown = null!;
private Dictionary<string, Texture2D> _rotations = null!;
```

---

### Methods

#### `_Ready()`

1. Adds to `"enemies"` group
2. Computes stats from level formulas
3. Gets node references (Sprite, LevelLabel, HitArea, HitCooldownTimer)
4. Loads species-specific directional sprites via `DirectionalSprite.LoadRotations()`
5. Sets initial sprite to south-facing texture
6. Calls `UpdateColor()` to set initial gradient color
7. Sets level label text
8. Connects signals: HitArea.BodyEntered, HitCooldownTimer.Timeout, GameState.StatsChanged

#### `_PhysicsProcess(double delta)`

Finds the player via group lookup. Calculates direction vector toward player, sets velocity, calls `MoveAndSlide()`. Despawns if distance to player exceeds 800px (safety net for escaped enemies). Updates directional sprite based on movement velocity.

#### `TakeDamage(int amount)`

1. Reduces `_hp` by `amount`
2. Triggers white flash via `FlashFx.Flash()`
3. Spawns floating damage number via `FloatingText.Damage()`
4. If HP <= 0:
   - Spawns floating XP text
   - Emits `EventBus.EnemyDefeated` with position and level
   - Awards XP via `GameState.Instance.AwardXp()`
   - Calls `QueueFree()`
5. If alive: tweens sprite modulate back to `_currentColor` over 0.1s

#### `UpdateColor()`

Called on init and whenever `GameState.StatsChanged` fires. Computes `gap = Level - GameState.Instance.Level`, resolves the gradient color via `GetGradientColor()`, and applies to both sprite and label modulate.

#### `GetGradientColor(int gap)`

Static method. Clamps to extreme anchors if gap is outside range. For gaps between anchors, finds the bounding pair and linearly interpolates using `Color.Lerp()`. Falls back to the Green anchor (index 3, gap=0) if no bracket is found.

#### `DealDamageTo(Node2D playerNode)`

Casts to Player, checks `player.IsInvincible`, then:
1. Calls `GameState.Instance.TakeDamage(_damage)`
2. Calls `player.DamageFlash()` (red flash, not camera shake)
3. Spawns floating damage number on player
4. Starts hit cooldown timer

---

### AI Behavior

All enemies use straight-line chase AI: compute direction to player, multiply by speed, `MoveAndSlide()`. There is no pathfinding, obstacle avoidance, steering, or state machine. Enemies slide along walls when blocked.

### Enemy-Enemy Interaction

Enemies do NOT collide with each other (collision_mask `0b011` does not include bit 2). Enemies freely overlap and stack on the player's position. Multiple enemies deal damage independently via separate HitCooldownTimers.

---

### Collision Setup

**CharacterBody2D (Enemy body):**

| Property | Value | Binary | Description |
|----------|-------|--------|-------------|
| `collision_layer` | `4` | `0b100` | Enemy layer. Detected by Player AttackRange (mask bit 2). |
| `collision_mask` | `3` | `0b011` | Collides with walls (bit 0) and player (bit 1). |

**HitArea (Area2D):**

| Property | Value | Binary | Description |
|----------|-------|--------|-------------|
| `collision_layer` | `0` | `0b000` | Nothing detects the HitArea. |
| `collision_mask` | `2` | `0b010` | Detects player (layer 2). |
| `monitoring` | `true` | -- | Actively checks for overlapping bodies. |
| `monitorable` | `false` | -- | Not detectable by other areas. |

---

### Spawning (managed by Dungeon.cs)

Enemies are instanced and configured by `Dungeon.cs`:

1. `EnemyScene.Instantiate<Enemy>()`
2. Level set from floor scaling: `min = max(1, floor-1)`, `max = floor+2`
3. Random species: `GD.Randi() % EnemySpeciesRotations.Length`
4. Position from `GetRandomEdgePosition()` with wall margin and safe-spawn radius check
5. `_entities.AddChild(enemy)` triggers `_Ready()`
6. Emits `EventBus.EnemySpawned`

| Constant | Value | Purpose |
|----------|-------|---------|
| `InitialEnemies` | `8` | Spawned on floor entry |
| `EnemySoftCap` | `14` | Max active enemies |
| `SpawnInterval` | `2.8s` | Timer-based spawn check |
| `RespawnDelay` | `1.4s` | Delay after enemy defeat before respawn |
| `SafeSpawnRadius` | `150px` | Minimum distance from player to spawn |
| `SpawnWallMargin` | `5 tiles` | Spawn this far from walls |
| `DespawnDistance` | `800px` | Despawn if this far from player |

## Implementation Notes

- Enemy scene preloaded by `Dungeon.cs`: `GD.Load<PackedScene>(Constants.Assets.EnemyScene)`.
- `Level` and `SpeciesIndex` must be set BEFORE `AddChild()`. Once `AddChild()` runs, `_Ready()` reads them.
- `QueueFree()` defers removal to end of frame, safe for same-frame references.
- White damage flash (via `FlashFx.Flash`) followed by tween back to gradient color provides visual hit feedback.
- Enemy collision_mask includes player (bit 1), meaning enemies physically collide with the player body via `MoveAndSlide()`.

## Open Questions

- Should the AI be upgraded to NavigationAgent2D for pathfinding around obstacles?
- Should enemies have a spawning animation (fade in, grow) instead of instant appearance?
- Should there be more than 2 species with different visual variety?
