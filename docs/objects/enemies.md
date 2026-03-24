# Enemies

## Summary

Enemies are CharacterBody2D instances with a configurable `danger_tier` (1-3) that determines all their stats: HP, speed, color, damage, and XP reward. They chase the player in a straight line, deal contact damage on a 0.7s cooldown, and emit a signal on death that triggers respawning. All three tiers use the same `enemy.tscn` scene with different property values.

## Current State

Three enemy tiers are fully functional:
- Tier 1 (green) -- low danger, 30 HP, slow, 4 damage, 14 XP
- Tier 2 (yellow) -- medium danger, 42 HP, moderate speed, 5 damage, 18 XP
- Tier 3 (red) -- high danger, 54 HP, fast, 6 damage, 22 XP

Enemies spawn at room edges, chase the player with straight-line movement (no pathfinding), deal damage on body overlap with a per-enemy cooldown, and respawn 1.4 seconds after defeat.

## Design

### Node Tree

See `docs/architecture/scene-tree.md` for the full `enemy.tscn` hierarchy with every property value.

```
Enemy (CharacterBody2D) [enemy.gd]
├── CollisionShape2D → CircleShape2D(radius=10.0)
├── Sprite (Polygon2D) → color set by tier, diamond shape
├── HitArea (Area2D) → CircleShape2D(radius=15.0), detects player layer
└── HitCooldownTimer (Timer) → wait_time=0.7, one_shot=true
```

---

### Exported Properties

| Name | Type | Default | Export | Inspector | Purpose |
|------|------|---------|--------|-----------|---------|
| `danger_tier` | `int` | `1` | `@export` | Editable in Inspector when instanced | Determines all enemy stats. Set by `dungeon.gd` before adding to scene tree. Valid values: 1, 2, 3. |

**Why @export:** The same `enemy.tscn` scene is used for all three tiers. The `@export` annotation exposes `danger_tier` in the Godot Inspector and allows `dungeon.gd` to set it per-instance:
```gdscript
var enemy = EnemyScene.instantiate()
enemy.danger_tier = randi_range(1, 3)
entities.add_child(enemy)
```

**Why a single export instead of separate scenes per tier:** Three nearly identical scenes would be tedious to maintain. Any change (collision shape size, polygon shape, node structure) would need to be replicated across all three. A single scene with a tier parameter is the DRY approach.

---

### Tier Stats Table

| Property | Formula | Tier 1 | Tier 2 | Tier 3 |
|----------|---------|--------|--------|--------|
| HP | `18 + tier * 12` | 30 | 42 | 54 |
| Speed (px/s) | `48 + tier * 18` | 66 | 84 | 102 |
| Color (hex) | `TIER_COLORS[tier]` | `#6bff89` (green) | `#ffde66` (yellow) | `#ff6f6f` (red) |
| Color (Godot) | `TIER_COLORS[tier]` | `Color(0.420, 1.0, 0.537, 1.0)` | `Color(1.0, 0.871, 0.400, 1.0)` | `Color(1.0, 0.435, 0.435, 1.0)` |
| Contact Damage | `3 + tier` | 4 | 5 | 6 |
| XP Reward | `10 + tier * 4` | 14 | 18 | 22 |

**Phaser prototype source values:**
```javascript
const hp = 18 + dangerTier * 12;
const speed = 48 + dangerTier * 18;
// Damage: 3 + enemy.dangerTier
// XP: 10 + enemy.dangerTier * 4
```

**Stat scaling rationale:**
- **HP scaling (12 per tier):** Tier 3 has 80% more HP than tier 1 (54 vs 30). At level 1 damage of 13, tier 1 dies in 3 hits, tier 2 in 4 hits, tier 3 in 5 hits. The difference is significant but not overwhelming.
- **Speed scaling (18 per tier):** Tier 3 is 55% faster than tier 1 (102 vs 66). Player speed is 190, so even tier 3 can be outrun. The speed difference affects how quickly enemies close distance, not whether they catch the player.
- **Damage scaling (1 per tier):** Small increment (4/5/6). At 100 HP, tier 1 kills in 25 hits, tier 3 in ~17 hits. The real danger of higher tiers is speed (harder to avoid) combined with slightly higher damage.
- **XP scaling (4 per tier):** Higher tiers are worth more XP, proportional to their difficulty. Risk/reward is linear.

---

### Constants

```gdscript
const TIER_COLORS: Dictionary = {
    1: Color("#6bff89"),  # Green - low danger
    2: Color("#ffde66"),  # Yellow - medium danger
    3: Color("#ff6f6f"),  # Red - high danger
}
```

| Tier | Hex | RGB | Color Name | Phaser Constant |
|------|-----|-----|------------|-----------------|
| 1 | `#6bff89` | (107, 255, 137) | Bright green | `COLORS.enemyLow = 0x6bff89` |
| 2 | `#ffde66` | (255, 222, 102) | Golden yellow | `COLORS.enemyMid = 0xffde66` |
| 3 | `#ff6f6f` | (255, 111, 111) | Coral red | `COLORS.enemyHigh = 0xff6f6f` |

**Color design rationale:** The traffic-light pattern (green/yellow/red) is universally understood as low/medium/high danger. Players instantly assess threat level without reading stats. The specific hex values are bright and saturated to be visible against the dark dungeon floor tiles (`#24314a`).

---

### Variables

| Name | Type | Default | Set In | Purpose |
|------|------|---------|--------|---------|
| `hp` | `int` | `30` | `_ready()` | Current hit points. Decremented by `take_damage()`. When <= 0, the enemy is defeated and freed. Default of 30 is for tier 1; overwritten in `_ready()` based on `danger_tier`. |
| `move_speed` | `float` | `66.0` | `_ready()` | Movement speed in pixels per second. Default of 66.0 is for tier 1; overwritten in `_ready()` based on `danger_tier`. |

### Node References (@onready)

```gdscript
@onready var sprite: Polygon2D = $Sprite
@onready var hit_area: Area2D = $HitArea
@onready var hit_cooldown: Timer = $HitCooldownTimer
```

---

### Methods

#### `_ready() -> void`

Called once when the Enemy node enters the scene tree. Initializes all tier-dependent stats and connects signals.

```gdscript
func _ready() -> void:
    # Step 1: Register in group
    add_to_group("enemies")

    # Step 2: Apply tier-based stats
    hp = 18 + danger_tier * 12
    move_speed = 48.0 + danger_tier * 18.0

    # Step 3: Apply tier-based color
    sprite.color = TIER_COLORS.get(danger_tier, TIER_COLORS[1])

    # Step 4: Connect signals
    hit_area.body_entered.connect(_on_hit_area_body_entered)
    hit_cooldown.timeout.connect(_on_hit_cooldown_timer_timeout)
```

**Step-by-step breakdown:**

**Step 1 -- Group registration:** Adds this enemy to the `"enemies"` group. This is used by:
- `player.gd` to filter `AttackRange.get_overlapping_bodies()` to only enemies
- `dungeon.gd` to count active enemies: `get_tree().get_nodes_in_group("enemies").size()`

**Step 2 -- Stat calculation:** HP and speed are computed from formulas using `danger_tier`. The `@export danger_tier` is already set by `dungeon.gd` before the enemy enters the scene tree (set between `instantiate()` and `add_child()`), so it's available in `_ready()`.

**Step 3 -- Color application:** The sprite's fill color is set from the `TIER_COLORS` dictionary. The `.get()` fallback to tier 1 color is a safety guard in case an invalid tier is somehow set.

**Step 4 -- Signal connections:** Two signals are connected:
- `hit_area.body_entered` fires when the player's CharacterBody2D first enters the HitArea. This triggers initial contact damage.
- `hit_cooldown.timeout` fires 0.7s after the timer starts. This triggers a re-check for ongoing contact damage.

---

#### `_physics_process(_delta: float) -> void`

Called every physics frame. Moves the enemy toward the player.

```gdscript
func _physics_process(_delta: float) -> void:
    # Step 1: Find the player
    var player: Node2D = get_tree().get_first_node_in_group("player")
    if player == null:
        return

    # Step 2: Calculate chase direction
    var direction: Vector2 = (player.global_position - global_position).normalized()

    # Step 3: Set velocity and move
    velocity = direction * move_speed
    move_and_slide()
```

**Step-by-step breakdown:**

**Step 1 -- Player lookup:**
`get_tree().get_first_node_in_group("player")` finds the player node by its group tag. This runs every physics frame, which may seem expensive, but Godot's group lookup is O(1) -- it returns a cached list.

If the player is null (shouldn't happen in normal gameplay, but possible if the player is freed during death handling), the enemy stops moving.

**Step 2 -- Direction calculation:**
The direction vector points from the enemy's current position to the player's current position. `.normalized()` scales it to length 1.0 so that multiplying by `move_speed` gives the correct speed regardless of distance.

**Step 3 -- Movement:**
`velocity` is set to the direction scaled by `move_speed`, then `move_and_slide()` applies the movement with wall collision. If the enemy hits a wall, it slides along the wall surface. This is the entirety of the enemy AI -- a straight-line chase.

**Why no delta multiplication:** `move_and_slide()` handles frame-rate independence internally. The `velocity` property is in pixels per second; `move_and_slide()` multiplies by delta internally.

**Phaser equivalent:** `this.physics.moveToObject(enemy, this.player, enemy.speed)` -- Phaser's helper does the same direction+speed calculation.

---

#### `take_damage(amount: int) -> void`

Called by `player.gd.handle_attack()` when the player auto-attacks this enemy.

```gdscript
func take_damage(amount: int) -> void:
    # Step 1: Reduce HP
    hp -= amount

    # Step 2: Check for death
    if hp <= 0:
        # Step 2a: Emit death signal with position and tier
        EventBus.enemy_defeated.emit(global_position, danger_tier)

        # Step 2b: Award XP to player
        GameState.award_xp(10 + danger_tier * 4)

        # Step 2c: Remove from scene
        queue_free()
```

**Step-by-step breakdown:**

**Step 1 -- HP reduction:** Simple subtraction. No clamping needed -- if HP goes negative, the death check still triggers.

**Step 2 -- Death check:** If HP drops to 0 or below:

**Step 2a -- Signal emission:** `EventBus.enemy_defeated.emit(global_position, danger_tier)` notifies the dungeon to schedule a respawn. The `global_position` is passed as a Vector2 value (not the node itself) because the node is about to be freed -- by the time the respawn timer fires 1.4s later, this node no longer exists.

**Step 2b -- XP award:** `GameState.award_xp(10 + danger_tier * 4)` adds XP and may trigger a level-up. The XP values per tier:
- Tier 1: 10 + 4 = 14 XP
- Tier 2: 10 + 8 = 18 XP
- Tier 3: 10 + 12 = 22 XP

At level 1 (threshold 90 XP), it takes ~7 tier 1 kills, ~5 tier 2 kills, or ~5 tier 3 kills to level up (accounting for the single-if-check, not while loop).

**Step 2c -- Node removal:** `queue_free()` defers the node's removal to the end of the current frame. This is safe to call inside a signal handler or method called from another node. The enemy is removed from all groups, all signal connections are severed, and the node is freed from memory.

**Why `queue_free()` instead of `free()`:** `free()` removes the node immediately, which can crash if other code still references it within the same frame. `queue_free()` defers removal to a safe point after all processing for the current frame is complete.

---

#### `_on_hit_area_body_entered(body: Node2D) -> void`

Signal handler for `HitArea.body_entered`. Called when the player's CharacterBody2D first overlaps the enemy's HitArea.

```gdscript
func _on_hit_area_body_entered(body: Node2D) -> void:
    # Guard: Only damage the player
    if not body.is_in_group("player"):
        return

    # Guard: Don't damage during cooldown
    if not hit_cooldown.is_stopped():
        return

    # Deal damage
    _deal_damage_to(body)
```

**Guard explanations:**

1. **Group check:** The HitArea's collision mask is set to layer 2 (player), so in theory only the player should trigger this signal. The group check is a safety net -- if collision layers are misconfigured, this prevents damage to non-player bodies.

2. **Cooldown check:** `hit_cooldown.is_stopped()` returns `true` when the timer is not running. If the timer IS running (not stopped), it means the enemy recently dealt damage and is on cooldown. This prevents double-damage when the player rapidly exits and re-enters the HitArea within the cooldown window.

**When this signal fires:**
- Player walks into an enemy's HitArea for the first time
- Player was previously outside the HitArea and enters again (after the cooldown)
- `body_entered` does NOT fire for bodies already overlapping when the area is first created or enabled

---

#### `_on_hit_cooldown_timer_timeout() -> void`

Signal handler for `HitCooldownTimer.timeout`. Called 0.7 seconds after the enemy last dealt damage.

```gdscript
func _on_hit_cooldown_timer_timeout() -> void:
    # Check if the player is still overlapping
    for body in hit_area.get_overlapping_bodies():
        if body.is_in_group("player"):
            _deal_damage_to(body)
            return  # Only damage once per timeout
```

**Behavior:**
When the cooldown expires, the enemy checks if the player is still inside the HitArea. If yes, damage is dealt again (which restarts the cooldown). If the player has moved away, no damage is dealt and the timer stays stopped until the next `body_entered` event.

**The damage loop:**
```
Player enters HitArea
  → body_entered fires → _deal_damage_to() → starts 0.7s timer
    → 0.7s later: timeout fires → player still overlapping? → _deal_damage_to() → restart timer
      → 0.7s later: timeout fires → player left? → no damage, timer stops
```

This creates a repeating damage tick every 0.7s while the player stands in the HitArea, matching the Phaser prototype's behavior where `enemy.lastHitAt` was checked every frame.

**Why `return` after first match:** The `for` loop iterates all overlapping bodies, but only one player exists. The `return` after dealing damage prevents redundant iterations and ensures exactly one damage application per timeout.

---

#### `_deal_damage_to(player_node: Node2D) -> void`

Private helper method that applies damage to the player and starts the cooldown.

```gdscript
func _deal_damage_to(player_node: Node2D) -> void:
    # Step 1: Calculate and apply damage
    var damage: int = 3 + danger_tier
    GameState.take_damage(damage)

    # Step 2: Visual feedback (camera shake)
    if player_node.has_method("shake_camera"):
        player_node.shake_camera()

    # Step 3: Start cooldown
    hit_cooldown.start()
```

**Step-by-step breakdown:**

**Step 1 -- Damage application:**
Damage formula: `3 + danger_tier`
- Tier 1: 3 + 1 = 4 damage
- Tier 2: 3 + 2 = 5 damage
- Tier 3: 3 + 3 = 6 damage

`GameState.take_damage(damage)` handles HP reduction, death detection, and signal emission. The enemy doesn't need to check if the player died -- GameState handles it.

**Step 2 -- Camera shake:**
`has_method("shake_camera")` is a duck-typing check. If the player node has a `shake_camera()` method, it's called. This avoids a hard dependency on the player script's API -- if `shake_camera` is removed or renamed, the enemy simply doesn't call it (no crash).

**Why duck typing instead of casting:** `(player_node as Player).shake_camera()` would require the enemy script to know about the Player class. Duck typing keeps the dependency one-directional: the enemy knows about the "player" group and the "shake_camera" method name, but not the Player class itself.

**Step 3 -- Cooldown start:**
`hit_cooldown.start()` begins the 0.7-second one-shot timer. While the timer is running:
- `hit_cooldown.is_stopped()` returns `false`
- New `body_entered` events are ignored (cooldown guard in `_on_hit_area_body_entered`)
- After 0.7s, `timeout` fires and the overlap re-check runs

---

### AI Behavior

#### Current AI: Straight-Line Chase

All enemies use the same trivial AI: compute the direction vector from enemy to player, multiply by speed, call `move_and_slide()`. There is:

- **No pathfinding:** Enemies do not navigate around obstacles. If a wall is between the enemy and the player, the enemy slides along the wall (due to `move_and_slide()`) but doesn't find a path around it.
- **No obstacle avoidance:** Enemies don't detect or avoid walls proactively. They bump into walls and slide.
- **No steering behaviors:** No separation force, no cohesion, no alignment. Enemies converge into a single clump on the player's position.
- **No attack patterns:** Enemies don't strafe, retreat, circle, or use ranged attacks. They beeline toward the player.
- **No state machine:** No idle/patrol/chase/attack states. Enemies are always chasing.

#### Enemy-Enemy Interaction

Enemies do NOT collide with each other (collision_mask does not include bit 2 for enemy layer). This means:
- Enemies freely overlap, forming a stack on the player's position
- Multiple enemies can deal damage simultaneously (each has an independent HitCooldownTimer)
- Visually, stacked enemies appear as a single multi-colored blob

**Why no enemy-enemy collision:** Adding enemy-enemy collision would cause problems:
1. Enemies would push each other around, creating chaotic movement
2. Enemies in the back couldn't reach the player (blocked by front-row enemies)
3. Pathfinding would be needed to prevent traffic jams
4. Performance cost increases quadratically with enemy count

The current stack-and-damage approach is simple, fair (all enemies can damage), and matches the Phaser prototype exactly.

---

### Collision Setup

**CharacterBody2D (Enemy body):**

| Property | Value | Binary | Description |
|----------|-------|--------|-------------|
| `collision_layer` | `4` | `0b100` | Bit 2 set. Identifies this body as "enemy." Detected by Player AttackRange (mask bit 2). |
| `collision_mask` | `1` | `0b001` | Bit 0 set. Collides with walls/tile physics (layer 1). Slides along walls via `move_and_slide()`. |

**HitArea (Area2D):**

| Property | Value | Binary | Description |
|----------|-------|--------|-------------|
| `collision_layer` | `0` | `0b000` | No bits set. Nothing detects the HitArea. |
| `collision_mask` | `2` | `0b010` | Bit 1 set. Detects the player (layer 2). Fires `body_entered` when the player overlaps. |
| `monitoring` | `true` | -- | Actively checks for overlapping bodies each physics frame. |
| `monitorable` | `false` | -- | Other Area2D nodes cannot detect this HitArea. Performance optimization. |

**Collision interaction summary:**

| Enemy Component | Interacts With | Type | Result |
|-----------------|---------------|------|--------|
| CharacterBody2D | Wall tiles (layer 1) | Physics collision | Enemy slides along walls |
| CharacterBody2D | Player AttackRange (Area2D, mask 4) | Area2D detection | Player's `get_overlapping_bodies()` returns this enemy |
| HitArea (Area2D) | Player body (layer 2) | Area2D detection | `body_entered` signal fires, damage applied |
| CharacterBody2D | Other enemies (layer 4) | None | No collision, free overlap |
| CharacterBody2D | Player body (layer 2) | None | No collision, free overlap |

---

### Spawning (managed by dungeon.gd)

Enemies are instanced and configured by `dungeon.gd`, not by the enemy script itself. The full spawn flow:

```
dungeon.gd._spawn_enemy():
    1. var enemy = EnemyScene.instantiate()
    2. enemy.danger_tier = randi_range(1, 3)    # Random tier
    3. enemy.global_position = <edge tile position>
    4. entities.add_child(enemy)                 # Triggers enemy._ready()
    5. EventBus.enemy_spawned.emit(enemy)
```

**Spawn position:** Enemies spawn at the edges of the tile map room (border tiles that are NOT wall tiles -- they spawn just inside the walkable area near the walls). The Phaser prototype spawned at literal screen edges; the Godot version spawns at room boundary tiles.

**Tier distribution:** `randi_range(1, 3)` gives equal probability (33.3% each) for all three tiers. Future: deeper floors could weight toward higher tiers.

**Initial spawn count:** 10 enemies on game start (matching the Phaser prototype's `for (let i = 0; i < 10; i += 1) { this.spawnEnemy(); }`).

**Soft cap:** 14 active enemies maximum. The SpawnTimer (2.8s interval) checks `get_tree().get_nodes_in_group("enemies").size() < 14` before spawning.

**Respawn after defeat:** When an enemy is defeated, `EventBus.enemy_defeated` triggers a 1.4-second delay in `dungeon.gd`, after which `_spawn_enemy()` is called again:
```gdscript
func _on_enemy_defeated(position: Vector2, tier: int) -> void:
    await get_tree().create_timer(1.4).timeout
    _spawn_enemy()
```

This means defeated enemies respawn individually after 1.4s, independent of the SpawnTimer. Both the SpawnTimer and the respawn timer can call `_spawn_enemy()`, and both respect the soft cap implicitly (a new enemy is spawned regardless, but the total stabilizes around the cap due to the spawn rate matching the kill rate).

---

### Hits-to-Kill Table

How many player hits to kill each enemy tier, at various player levels:

| Player Level | Damage | Tier 1 (30 HP) | Tier 2 (42 HP) | Tier 3 (54 HP) |
|-------------|--------|-----------------|-----------------|-----------------|
| 1 | 13 | 3 hits | 4 hits | 5 hits |
| 2 | 15 | 2 hits | 3 hits | 4 hits |
| 3 | 16 | 2 hits | 3 hits | 4 hits |
| 5 | 19 | 2 hits | 3 hits | 3 hits |
| 10 | 27 | 2 hits | 2 hits | 2 hits |
| 20 | 42 | 1 hit | 1 hit | 2 hits |

**Time-to-kill at level 1 (0.42s attack cooldown):**
- Tier 1: 3 hits * 0.42s = 1.26 seconds
- Tier 2: 4 hits * 0.42s = 1.68 seconds
- Tier 3: 5 hits * 0.42s = 2.10 seconds

**Time-to-die at level 1 (100 HP, 0.7s enemy hit cooldown):**
- Tier 1 (4 damage): 25 hits * 0.7s = 17.5 seconds
- Tier 2 (5 damage): 20 hits * 0.7s = 14.0 seconds
- Tier 3 (6 damage): 17 hits * 0.7s = 11.9 seconds
- But enemies stack: 3 tier 3 enemies = 18 damage/0.7s = ~3.9 seconds to die

## Implementation Notes

- The enemy scene is preloaded by `dungeon.gd`: `const EnemyScene = preload("res://scenes/enemy.tscn")`. Preloading ensures instant instancing without disk I/O at spawn time.
- `danger_tier` must be set BEFORE `add_child()` is called. Once `add_child()` runs, `_ready()` fires and reads `danger_tier` to compute stats. Setting `danger_tier` after `add_child()` would leave the enemy with default tier 1 stats.
- `queue_free()` in `take_damage()` means the enemy node is freed at the end of the current frame. Any code in `player.gd` that runs after `take_damage()` within the same frame can still reference the enemy (e.g., `draw_slash(nearest_enemy.global_position)` works because the enemy still exists during that frame).
- The `TIER_COLORS` dictionary uses integer keys (1, 2, 3) matching the `danger_tier` values. Using a Dictionary instead of an Array avoids off-by-one errors (no tier 0 to skip).

## Open Questions

- Should enemies have a brief hit flash (white modulate) when taking damage, for visual feedback?
- Should higher-tier enemies have slightly larger collision shapes or sprites to appear more threatening?
- Should the AI be upgraded to use NavigationAgent2D for pathfinding when procedural floor generation is added?
- Should enemies have a "leash distance" -- a maximum range beyond which they stop chasing and return to their spawn point?
- Should floor depth affect enemy tier distribution (more tier 3 on deeper floors)?
- Should enemies play different death effects based on tier (larger explosion for higher tiers)?
- Should there be an enemy spawning animation (fade in, grow from point) instead of instant appearance?
