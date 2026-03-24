# Monsters

## Summary

Enemies spawn from screen edges with one of three danger tiers. They chase the player and deal damage on contact. Defeated enemies respawn on timers. In Godot 4, enemies are `CharacterBody2D` scenes with `Area2D` hit detection and timer-based cooldowns.

## Current State

Migrating from Phaser 3 prototype to Godot 4:
- Enemies spawn from random screen edges
- Three danger tiers with distinct colors: green (low), yellow (mid), red (high)
- Each tier has scaling HP (`18 + tier * 12`) and speed (`48 + tier * 18`)
- Enemies chase the player using direct pathfinding (straight-line pursuit)
- Deal `3 + dangerTier` damage on overlap (700ms cooldown per enemy)
- Defeated enemies respawn after 1.4s delay
- Soft cap: spawn timer only adds new enemies if fewer than 14 are active
- 10 enemies spawned on initial game start

## Design

### Danger Tiers

| Tier | Name | Color (Hex) | Color (Godot) | HP Formula | HP | Speed Formula | Speed | Damage Formula | Damage | XP Formula | XP Reward |
|------|------|-------------|---------------|-----------|-----|---------------|-------|----------------|--------|------------|-----------|
| 1 | Low | `#6bff89` | `Color("#6bff89")` | 18 + 1 * 12 | 30 | 48 + 1 * 18 | 66 | 3 + 1 | 4 | 10 + 1 * 4 | 14 |
| 2 | Mid | `#ffde66` | `Color("#ffde66")` | 18 + 2 * 12 | 42 | 48 + 2 * 18 | 84 | 3 + 2 | 5 | 10 + 2 * 4 | 18 |
| 3 | High | `#ff6f6f` | `Color("#ff6f6f")` | 18 + 3 * 12 | 54 | 48 + 3 * 18 | 102 | 3 + 3 | 6 | 10 + 3 * 4 | 22 |

### Stat Formulas (General)

All formulas are parameterized by `danger_tier` (integer, 1-3):

| Stat | Formula | Notes |
|------|---------|-------|
| HP | `18 + danger_tier * 12` | Linear scaling, tier 1 starts at 30 |
| Speed | `48 + danger_tier * 18` | Pixels per second, higher tiers are faster |
| Contact Damage | `3 + danger_tier` | Damage dealt to player per hit |
| XP Reward | `10 + danger_tier * 4` | XP awarded to player on kill |

### Spawning Rules

| Parameter | Value | Notes |
|-----------|-------|-------|
| Initial spawn count | 10 | Spawned immediately when game/floor starts |
| Soft cap | 14 | Periodic timer will not spawn above this count |
| Periodic spawn interval | 2.8 seconds | Repeating timer checks cap and spawns one enemy |
| Respawn delay (after kill) | 1.4 seconds | Individual timer per killed enemy |
| Tier assignment | Random 1-3 | `randi_range(1, 3)` -- equal probability |

#### Spawn Positions

Enemies spawn at random positions along the four edges of the play area:
- **Edge 0 (Top):** random X across full width, Y = 10
- **Edge 1 (Right):** X = world_width - 10, random Y across full height
- **Edge 2 (Bottom):** random X across full width, Y = world_height - 10
- **Edge 3 (Left):** X = 10, random Y across full height

Edge selection is random with equal probability: `randi_range(0, 3)`.

### AI Behavior

Currently simple: all enemies chase the player in a straight line. No pathfinding, no special behaviors, no state machine.

**Chase Logic:**
1. Every physics frame, get the player's global position
2. Calculate direction vector from enemy to player: `(player.global_position - global_position).normalized()`
3. Set velocity to `direction * move_speed`
4. Call `move_and_slide()` for physics-based movement with collision

**No avoidance:** Enemies do not avoid each other or obstacles. They stack on the player position if unimpeded.

### Damage Dealing

**Contact damage model:** Enemies deal damage when their `HitArea` (Area2D) overlaps the player's body. A per-enemy cooldown timer prevents damage from being dealt every frame.

**Cooldown flow:**
1. `HitArea.body_entered` signal fires when player enters the enemy's hit zone
2. If `HitCooldownTimer` is stopped (not on cooldown): deal `3 + danger_tier` damage, start cooldown timer (0.7s)
3. When `HitCooldownTimer.timeout` fires: check if player is still overlapping via `get_overlapping_bodies()`
4. If player is still overlapping: deal damage again, restart the cooldown timer
5. If player has left the hit zone: do nothing, timer stays stopped

This creates continuous damage at 0.7s intervals while the player remains in contact with an enemy, matching the Phaser prototype behavior where `onPlayerHit` checked `now - enemy.lastHitAt < 700`.

### Player Attack vs. Enemies

Player attacks are auto-targeted (see `docs/systems/combat.md`):

| Player Stat | Value |
|-------------|-------|
| Attack cooldown | 0.42 seconds (420ms) |
| Attack range | 78 pixels |
| Damage formula | `12 + floor(level * 1.5)` |
| Target selection | Nearest enemy within range |

**Damage per level (first 10 levels):**

| Level | Damage Formula | Damage |
|-------|---------------|--------|
| 1 | 12 + floor(1 * 1.5) | 13 |
| 2 | 12 + floor(2 * 1.5) | 15 |
| 3 | 12 + floor(3 * 1.5) | 16 |
| 4 | 12 + floor(4 * 1.5) | 18 |
| 5 | 12 + floor(5 * 1.5) | 19 |
| 6 | 12 + floor(6 * 1.5) | 21 |
| 7 | 12 + floor(7 * 1.5) | 22 |
| 8 | 12 + floor(8 * 1.5) | 24 |
| 9 | 12 + floor(9 * 1.5) | 25 |
| 10 | 12 + floor(10 * 1.5) | 27 |

**Hits to kill by tier and level:**

| Level | Damage | Tier 1 (30 HP) | Tier 2 (42 HP) | Tier 3 (54 HP) |
|-------|--------|-----------------|-----------------|-----------------|
| 1 | 13 | 3 hits | 4 hits | 5 hits |
| 2 | 15 | 2 hits | 3 hits | 4 hits |
| 3 | 16 | 2 hits | 3 hits | 4 hits |
| 5 | 19 | 2 hits | 3 hits | 3 hits |
| 10 | 27 | 2 hits | 2 hits | 2 hits |

### Slash Visual Effect

When the player attacks an enemy, a gold-colored slash mark appears at the enemy's position:
- Color: `#f5c86b` (accent gold), 95% opacity
- Size: 26 x 4 pixels (thin rectangle)
- Rotation: random between -1.2 and +1.2 radians
- Animation: fades to 0 alpha while moving 8px upward over 120ms, then destroyed
- One slash per attack (appears at the targeted enemy's position)

### Future Enemy Types

As the dungeon deepens, enemies should become more varied:
- **Ranged enemies** -- shoot projectiles instead of chasing
- **Fast enemies** -- low HP but very high speed
- **Tank enemies** -- slow but high HP and damage
- **Boss enemies** -- appear on milestone floors, unique mechanics
- **Elemental variants** -- resist certain damage types

### Floor-Based Scaling

Enemy stats should scale with floor depth:
- Deeper floors spawn higher-tier enemies more frequently
- Enemy base stats (HP, speed, damage) increase with floor number
- New enemy types unlock at certain floor thresholds

## Implementation Notes

### Enemy Scene (enemy.tscn)

```
Enemy (CharacterBody2D) [enemy.gd]
├── CollisionShape2D (CircleShape2D, radius=10)
│   Purpose: Physics body for move_and_slide() wall/body collisions
├── Sprite (Polygon2D, diamond shape)
│   Points: [Vector2(0, -10), Vector2(10, 0), Vector2(0, 10), Vector2(-10, 0)]
│   Color: set dynamically based on danger_tier
├── HitArea (Area2D, collision_layer=0, collision_mask=2)
│   Purpose: Detect overlap with player for contact damage
│   └── HitShape (CollisionShape2D, CircleShape2D, radius=15)
│       Slightly larger than body collision to make contact damage reliable
└── HitCooldownTimer (Timer, wait_time=0.7, one_shot=true)
    Purpose: 700ms cooldown between contact damage hits
```

### Collision Layer Setup

| Layer | Name | Used By |
|-------|------|---------|
| 1 | World | Walls, floor boundaries |
| 2 | Player | Player's CharacterBody2D |
| 3 | Enemies | Enemy CharacterBody2D nodes |

The enemy's `HitArea` has:
- `collision_layer = 0` (does not report itself on any layer)
- `collision_mask = 2` (detects bodies on layer 2 = player)

This means the HitArea only detects the player, never other enemies or walls.

### enemy.gd Script

```gdscript
extends CharacterBody2D

@export var danger_tier: int = 1

var hp: int
var move_speed: float
var damage: int
var xp_reward: int

@onready var hit_area: Area2D = $HitArea
@onready var hit_cooldown: Timer = $HitCooldownTimer
@onready var sprite: Polygon2D = $Sprite

const TIER_COLORS := {
    1: Color("#6bff89"),  # Green - low danger
    2: Color("#ffde66"),  # Yellow - mid danger
    3: Color("#ff6f6f"),  # Red - high danger
}

func _ready() -> void:
    hp = 18 + danger_tier * 12
    move_speed = 48.0 + danger_tier * 18.0
    damage = 3 + danger_tier
    xp_reward = 10 + danger_tier * 4
    sprite.color = TIER_COLORS.get(danger_tier, Color.WHITE)

    hit_area.body_entered.connect(_on_hit_area_body_entered)
    hit_cooldown.timeout.connect(_on_hit_cooldown_timeout)

func _physics_process(_delta: float) -> void:
    var player = get_tree().get_first_node_in_group("player")
    if not player:
        return
    var direction := (player.global_position - global_position).normalized()
    velocity = direction * move_speed
    move_and_slide()

func take_damage(amount: int) -> void:
    hp -= amount
    if hp <= 0:
        die()

func die() -> void:
    queue_free()

func _on_hit_area_body_entered(body: Node2D) -> void:
    if body.is_in_group("player") and hit_cooldown.is_stopped():
        _deal_damage_to_player(body)

func _on_hit_cooldown_timeout() -> void:
    # Check if player is still overlapping
    for body in hit_area.get_overlapping_bodies():
        if body.is_in_group("player"):
            _deal_damage_to_player(body)
            return

func _deal_damage_to_player(_player: Node2D) -> void:
    GameState.take_damage(damage)
    hit_cooldown.start()
```

### Spawning System

Enemy spawning is managed by the main game scene (not by individual enemies). The spawn manager:

1. **On scene start:** Spawns 10 enemies at random edge positions with random tiers
2. **Periodic timer (2.8s):** Checks if active enemy count < 14; if so, spawns one new enemy
3. **On enemy death:** Starts a 1.4s one-shot timer; on timeout, spawns a replacement enemy

```gdscript
# In the main game scene or spawn manager:

func _ready() -> void:
    # Initial spawn
    for i in range(10):
        spawn_enemy()

    # Periodic spawn timer
    var spawn_timer := Timer.new()
    spawn_timer.wait_time = 2.8
    spawn_timer.autostart = true
    spawn_timer.timeout.connect(_on_spawn_timer)
    add_child(spawn_timer)

func spawn_enemy() -> void:
    var enemy_scene := preload("res://scenes/enemy.tscn")
    var enemy := enemy_scene.instantiate()
    enemy.danger_tier = randi_range(1, 3)
    enemy.global_position = _random_edge_position()
    add_child(enemy)
    enemy.tree_exiting.connect(_on_enemy_died.bind(enemy))

func _on_spawn_timer() -> void:
    if get_tree().get_nodes_in_group("enemies").size() < 14:
        spawn_enemy()

func _on_enemy_died(_enemy: Node) -> void:
    # Respawn after 1.4s delay
    get_tree().create_timer(1.4).timeout.connect(spawn_enemy)

func _random_edge_position() -> Vector2:
    var edge := randi_range(0, 3)
    match edge:
        0:  # Top
            return Vector2(randf_range(0, world_width), 10)
        1:  # Right
            return Vector2(world_width - 10, randf_range(0, world_height))
        2:  # Bottom
            return Vector2(randf_range(0, world_width), world_height - 10)
        3:  # Left
            return Vector2(10, randf_range(0, world_height))
    return Vector2.ZERO
```

### Camera Shake on Hit

When the player takes damage from an enemy, the camera shakes:
- Intensity: +/-3 pixels displacement
- Duration: 0.045 seconds (approximately 90ms total -- 45ms offset, 45ms return)
- Phaser equivalent: `this.cameras.main.shake(90, 0.0035)` -- 90ms duration, 0.0035 intensity (which is ~3.85 pixels on a 1100px canvas)

In Godot, camera shake can be implemented on the `Camera2D` node:
```gdscript
func shake(intensity: float = 3.0, duration: float = 0.045) -> void:
    var tween := create_tween()
    tween.tween_property(self, "offset", Vector2(randf_range(-intensity, intensity), randf_range(-intensity, intensity)), duration)
    tween.tween_property(self, "offset", Vector2.ZERO, duration)
```

### Comparison to Phaser Prototype

| Aspect | Phaser 3 | Godot 4 |
|--------|----------|---------|
| Enemy node | `this.add.circle(x, y, 10, tint)` | `CharacterBody2D` + `Polygon2D` (diamond) |
| Physics body | `this.physics.add.existing(enemy)` | Built into `CharacterBody2D` |
| Movement | `this.physics.moveToObject(enemy, player, speed)` | Manual `velocity = direction * speed` + `move_and_slide()` |
| Hit detection | `this.physics.add.overlap(player, enemies, callback)` | `Area2D.body_entered` signal |
| Hit cooldown | `enemy.lastHitAt` timestamp comparison | `Timer` node (one_shot, 0.7s) |
| Respawn delay | `this.time.delayedCall(1400, callback)` | `get_tree().create_timer(1.4).timeout` or Timer node |
| Spawn cap check | `this.enemies.countActive(true) < 14` | `get_tree().get_nodes_in_group("enemies").size() < 14` |
| Random tier | `Phaser.Math.Between(1, 3)` | `randi_range(1, 3)` |
| Death | `enemy.disableBody(true, true)` | `queue_free()` |

## Open Questions

- Should enemies have individual visual identities (different shapes/emoji)?
- How should enemy density scale with floor depth?
- At what floor depth should boss enemies appear?
- Should enemies drop loot directly, or is loot handled separately?
- Should enemy AI ever include pathfinding around obstacles?
- Should enemies be in a "enemies" group for easy counting/querying?
- Should enemy death play a visual effect (fade out, particles)?
- Should there be a maximum total enemy count (hard cap) beyond the soft cap?
