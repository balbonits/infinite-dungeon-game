# Combat System

## Summary

Real-time auto-targeting combat. The player automatically attacks the nearest enemy within range, with a cooldown between attacks.

## Current State

Implemented in the prototype:
- Auto-targeting: finds nearest enemy within `ATTACK_RANGE` (78px)
- Attack cooldown: `ATTACK_COOLDOWN` (420ms)
- Damage: `12 + floor(level * 1.5)` per hit
- Slash visual effect (tweened rectangle)
- Enemy damage to player: `3 + dangerTier` on overlap, with 700ms hit cooldown per enemy
- Camera shake on hit

## Design

### Auto-Targeting

The combat system is intentionally simple: "easy targeting." Each frame (during `tryAutoAttack()`):
1. Check if the attack cooldown has elapsed
2. Find the nearest active enemy within range
3. If found, deal damage and show the slash effect
4. If the enemy's HP drops to 0, defeat it

The player does not need to aim or click to attack — proximity triggers combat automatically.

### Damage Formula

Currently simple and linear:
```
playerDamage = 12 + floor(level * 1.5)
enemyDamage = 3 + dangerTier  (tier 1–3)
```

This will evolve to incorporate stats (STR for melee, DEX for ranged, INT for spells) once the stat system is wired in.

### Attack Cooldown

Fixed at 420ms. Future plans:
- DEX could reduce cooldown
- Different weapon types could have different base cooldowns
- Abilities may have their own independent cooldowns

### Range

Fixed at 78px for melee. Future plans:
- Marksman class could have longer range
- Mage spells could have variable range
- Melee range might scale slightly with weapon type

### Visual Feedback

- **Slash effect:** a small rotated rectangle that fades upward (120ms tween)
- **Camera shake:** 90ms, low intensity (0.0035) on player hit
- **Enemy defeat:** enemy is disabled and fades out (currently instant disable)

## Godot Implementation

### Player Auto-Attack (Godot)

The auto-attack system is implemented using an Area2D node for range detection and a float-based cooldown timer.

**Scene tree structure:**
```
Player (CharacterBody2D)
  +-- Polygon2D (player sprite)
  +-- CollisionShape2D (body, radius 12px)
  +-- AttackRange (Area2D)
  |     +-- CollisionShape2D (CircleShape2D, radius 78px)
  +-- Camera2D
```

**AttackRange Area2D configuration:**

| Property | Value |
|----------|-------|
| Node type | Area2D |
| CollisionShape2D shape | CircleShape2D |
| Shape radius | 78.0 px (matches Phaser `ATTACK_RANGE = 78`) |
| collision_layer | 4 (sensors) |
| collision_mask | 3 (enemies) |
| monitoring | true (detects bodies entering) |
| monitorable | false (other areas do not need to detect this) |

**Target finding algorithm (every physics frame):**
```gdscript
const ATTACK_COOLDOWN := 0.42  # seconds (420ms)
var attack_cooldown_remaining := 0.0

func _physics_process(delta: float) -> void:
    # Decrement cooldown
    attack_cooldown_remaining -= delta
    if attack_cooldown_remaining > 0.0:
        return

    # Get all enemy bodies overlapping the AttackRange Area2D
    var bodies := attack_range.get_overlapping_bodies()

    # Find the nearest enemy
    var best_enemy: CharacterBody2D = null
    var best_distance := INF

    for body in bodies:
        if not body.is_in_group("enemies"):
            continue
        if not body.is_inside_tree():
            continue
        var dist := global_position.distance_to(body.global_position)
        if dist < best_distance:
            best_distance = dist
            best_enemy = body

    if best_enemy == null:
        return

    # Attack the nearest enemy
    attack_cooldown_remaining = ATTACK_COOLDOWN
    var damage := 12 + int(GameState.level * 1.5)
    best_enemy.take_damage(damage)
    draw_slash(best_enemy.global_position)
```

**Damage calculation:**
```
damage = 12 + int(GameState.level * 1.5)
```

| Player Level | Damage | Calculation |
|-------------|--------|-------------|
| 1 | 13 | 12 + int(1 * 1.5) = 12 + 1 |
| 2 | 15 | 12 + int(2 * 1.5) = 12 + 3 |
| 3 | 16 | 12 + int(3 * 1.5) = 12 + 4 |
| 5 | 19 | 12 + int(5 * 1.5) = 12 + 7 |
| 10 | 27 | 12 + int(10 * 1.5) = 12 + 15 |

**Slash visual effect (Godot):**
```gdscript
func draw_slash(target_pos: Vector2) -> void:
    var slash := Polygon2D.new()
    slash.polygon = PackedVector2Array([
        Vector2(-13, -2), Vector2(13, -2),
        Vector2(13, 2), Vector2(-13, 2)
    ])
    slash.color = Color(0.961, 0.784, 0.420, 0.95)  # #f5c86b @ 95%
    slash.position = target_pos
    slash.rotation = randf_range(-1.2, 1.2)
    get_parent().add_child(slash)

    var tween := create_tween()
    tween.tween_property(slash, "modulate:a", 0.0, 0.12)  # Fade out over 120ms
    tween.parallel().tween_property(slash, "position:y", target_pos.y - 8.0, 0.12)  # Drift up 8px
    tween.tween_callback(slash.queue_free)  # Clean up
```

### Enemy Damage to Player (Godot)

Enemies deal contact damage through an Area2D "hit area" that detects the player. A cooldown timer prevents rapid-fire damage.

**Enemy scene tree structure:**
```
Enemy (CharacterBody2D)
  +-- Polygon2D (enemy sprite, color set by tier)
  +-- CollisionShape2D (body, radius 10px)
  +-- HitArea (Area2D)
  |     +-- CollisionShape2D (CircleShape2D, radius 15px)
  +-- HitCooldownTimer (Timer)
```

**HitArea Area2D configuration:**

| Property | Value |
|----------|-------|
| Node type | Area2D |
| CollisionShape2D shape | CircleShape2D |
| Shape radius | 15.0 px |
| collision_layer | 5 (damage) |
| collision_mask | 2 (player) |
| monitoring | true |

**HitCooldownTimer configuration:**

| Property | Value |
|----------|-------|
| Node type | Timer |
| wait_time | 0.7 seconds (700ms, matches Phaser `enemy.lastHitAt` check) |
| one_shot | true |
| autostart | false |

**Damage flow:**

1. **Initial hit:** When the player enters the HitArea, `body_entered` signal fires.
   ```gdscript
   func _on_hit_area_body_entered(body: Node2D) -> void:
       if body.is_in_group("player") and hit_cooldown_timer.is_stopped():
           _deal_damage_to_player(body)
           hit_cooldown_timer.start()
   ```

2. **Continuous damage:** While the player remains in the HitArea, the cooldown timer repeats.
   ```gdscript
   func _on_hit_cooldown_timer_timeout() -> void:
       var overlapping := hit_area.get_overlapping_bodies()
       for body in overlapping:
           if body.is_in_group("player"):
               _deal_damage_to_player(body)
               hit_cooldown_timer.start()  # Restart for next tick
               return
       # Player left the area -- do not restart timer
   ```

3. **Damage calculation:**
   ```gdscript
   func _deal_damage_to_player(player: CharacterBody2D) -> void:
       var damage := 3 + danger_tier
       player.take_damage(damage)
   ```

**Damage values by tier:**

| Tier | Damage | Phaser Source |
|------|--------|---------------|
| 1 | 4 | `3 + 1` |
| 2 | 5 | `3 + 2` |
| 3 | 6 | `3 + 3` |

**Player take_damage() handler:**
```gdscript
func take_damage(amount: int) -> void:
    GameState.hp -= amount
    shake_camera()
    EventBus.player_damaged.emit(amount)

    if GameState.hp <= 0:
        GameState.hp = 0
        _die()
```

### Key Differences from Phaser

| Aspect | Phaser Prototype | Godot Implementation |
|--------|-----------------|---------------------|
| Attack range check | Manual `Phaser.Math.Distance.Between()` every frame for every enemy | Area2D `get_overlapping_bodies()` -- physics engine maintains the overlap list; script just reads it |
| Nearest enemy search | `this.enemies.children.iterate()` with distance comparison | Same logic, but over `attack_range.get_overlapping_bodies()` (pre-filtered to in-range enemies only) |
| Enemy-player overlap | `this.physics.add.overlap(player, enemies, callback)` -- fires every frame while overlapping | Area2D `body_entered` signal (event-based, fires once on enter) + Timer for sustained damage |
| Cooldown tracking | `time - this.lastAttackAt < ATTACK_COOLDOWN` using scene time in milliseconds | Float variable decremented by delta each frame; attack fires when <= 0 |
| Enemy hit cooldown | `now - enemy.lastHitAt < 700` per-enemy timestamp | Timer node per enemy, 0.7s one_shot |
| Slash effect | `this.add.rectangle(x, y, 26, 4, color, alpha)` + `this.tweens.add(...)` | `Polygon2D.new()` with vertices + `create_tween()` property animations |
| Camera shake | `this.cameras.main.shake(90, 0.0035)` -- built-in method, oscillating | Tween `Camera2D.offset` -- single offset + return, 2 phases at 45ms each |
| Damage application | Direct property mutation: `bestEnemy.hp -= damage` | Method call: `enemy.take_damage(damage)` -- allows encapsulation, signals, death checks |
| Death check | Inline: `if (bestEnemy.hp <= 0) this.defeatEnemy(bestEnemy)` | Inside `take_damage()`: enemy checks its own HP and handles its own death |

### Attack Timing Analysis

At `ATTACK_COOLDOWN = 0.42s`, the player attacks ~2.38 times per second.

**DPS (damage per second) by level:**

| Level | Damage/Hit | Attacks/Sec | DPS | Tier 1 Kill Time (30 HP) | Tier 2 Kill Time (42 HP) | Tier 3 Kill Time (54 HP) |
|-------|-----------|-------------|-----|-------------------------|-------------------------|-------------------------|
| 1 | 13 | 2.38 | 30.9 | 0.97s (3 hits) | 1.39s (4 hits) | 1.81s (5 hits) |
| 2 | 15 | 2.38 | 35.7 | 0.84s (2 hits) | 1.26s (3 hits) | 1.68s (4 hits) |
| 3 | 16 | 2.38 | 38.1 | 0.84s (2 hits) | 1.26s (3 hits) | 1.68s (4 hits) |
| 5 | 19 | 2.38 | 45.2 | 0.84s (2 hits) | 1.26s (3 hits) | 1.26s (3 hits) |
| 10 | 27 | 2.38 | 64.3 | 0.42s (2 hits) | 0.84s (2 hits) | 0.84s (2 hits) |

**Hits to kill formula:** `ceil(enemy_hp / damage)`

## Open Questions

- Should different classes have fundamentally different attack patterns (not just stat scaling)?
- How should area-of-effect attacks work?
- Should there be a manual attack option alongside auto-attack?
- How does weapon type affect combat (speed, range, damage)?
- Should the Area2D approach be replaced with a manual distance check for more control over targeting priority?
- Should there be a visual indicator showing which enemy is currently targeted (outline, highlight)?
- Should the attack cooldown be displayed as a small UI element (cooldown arc) near the player?
