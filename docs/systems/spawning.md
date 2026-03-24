# Enemy Spawn System

## Summary

Enemies spawn at the edges of the dungeon room. An initial batch spawns when the room loads, then a periodic timer adds more up to a soft cap. Defeated enemies are replaced after a short delay. Spawn tier is uniformly random across the three danger tiers.

## Current State

The spawn system is fully implemented in the Phaser prototype with all parameters below. The Godot migration preserves the same values and logic but uses Godot-native Timer nodes and signals instead of Phaser's `time.addEvent()` and `time.delayedCall()`.

## Design

### Spawn Parameters

| Parameter | Value | Type | Purpose |
|-----------|-------|------|---------|
| `INITIAL_ENEMIES` | 10 | int | Number of enemies spawned immediately on room load (`_ready()`) |
| `ENEMY_SOFT_CAP` | 14 | int | Maximum active enemies before periodic spawning pauses |
| `SPAWN_INTERVAL` | 2.8 | float (seconds) | Timer period for periodic spawn checks |
| `RESPAWN_DELAY` | 1.4 | float (seconds) | Delay before a replacement enemy spawns after one is defeated |

**Phaser source for these values:**
```javascript
// Initial spawn (create):
for (let i = 0; i < 10; i += 1) {
    this.spawnEnemy();
}

// Periodic spawn timer:
this.time.addEvent({
    delay: 2800,        // 2.8 seconds = 2800ms
    loop: true,
    callback: () => {
        if (this.enemies.countActive(true) < 14) {  // soft cap 14
            this.spawnEnemy();
        }
    }
});

// Replacement spawn on defeat:
this.time.delayedCall(1400, () => {  // 1.4 seconds = 1400ms
    if (!this.scene.isActive()) return;
    this.spawnEnemy();
});
```

### Spawn Flow

There are three spawn triggers. They operate independently and can overlap.

#### 1. Initial Spawn (on room load)

Fires once when the room scene enters the tree.

```gdscript
func _ready() -> void:
    for i in range(INITIAL_ENEMIES):
        _spawn_enemy()
```

- Spawns exactly 10 enemies instantly (synchronously, in one frame).
- No delay between individual spawns -- all 10 appear on the first frame.
- This means the player sees 10 enemies immediately upon entering the room.

#### 2. Periodic Spawn (timer-driven)

A repeating timer that attempts to add one enemy per interval.

**Timer node configuration:**

| Property | Value |
|----------|-------|
| Node name | `SpawnTimer` |
| wait_time | 2.8 |
| one_shot | false (repeating) |
| autostart | true |
| process_callback | `TIMER_PROCESS_PHYSICS` (optional, for consistency with physics) |

**Signal handler:**
```gdscript
func _on_spawn_timer_timeout() -> void:
    var enemy_count := get_tree().get_nodes_in_group("enemies").size()
    if enemy_count < ENEMY_SOFT_CAP:
        _spawn_enemy()
```

- Checks the current number of active enemies in the "enemies" group.
- Only spawns if the count is strictly less than `ENEMY_SOFT_CAP` (14).
- Spawns at most one enemy per timer tick.
- The timer runs continuously -- even if the cap is reached, it keeps ticking and checking.

**Spawns per minute (theoretical):**
- Timer fires every 2.8 seconds = ~21.4 times per minute.
- If always below cap, that is ~21 enemies per minute.
- In practice, the player kills enemies and the cap is rarely sustained, so the effective spawn rate is lower.

#### 3. Replacement Spawn (on enemy defeat)

When an enemy is defeated, a replacement spawns after a short delay.

**Via EventBus signal:**
```gdscript
func _on_enemy_defeated(enemy: Node) -> void:
    await get_tree().create_timer(RESPAWN_DELAY).timeout
    _spawn_enemy()
```

**Via direct call (alternative approach):**
```gdscript
# In the enemy defeat handler:
func _defeat_enemy(enemy: CharacterBody2D) -> void:
    enemy.queue_free()
    EventBus.enemy_defeated.emit(enemy)
    # SpawnManager listens for this signal and spawns replacement after delay
```

- The delay is 1.4 seconds from the moment of defeat.
- The replacement always spawns -- it does NOT check the soft cap.
- This means the replacement can push the enemy count above the soft cap temporarily.

### _spawn_enemy() Algorithm

Step-by-step enemy creation:

```gdscript
func _spawn_enemy() -> void:
    # Step 1: Instance the enemy scene
    var enemy: CharacterBody2D = ENEMY_SCENE.instantiate()

    # Step 2: Assign a random danger tier (1, 2, or 3)
    enemy.danger_tier = randi_range(1, 3)

    # Step 3: Calculate spawn position (room edge)
    enemy.global_position = _get_spawn_position()

    # Step 4: Add to the scene tree under the Entities node
    entities_node.add_child(enemy)

    # Step 5: Notify other systems
    EventBus.enemy_spawned.emit(enemy)
```

**Enemy scene reference:**
```gdscript
const ENEMY_SCENE: PackedScene = preload("res://scenes/enemy.tscn")
```

**Entities node:** All enemies are added as children of a designated `Entities` Node2D in the room scene. This keeps the scene tree organized and allows batch operations (e.g., `entities_node.get_children()` to iterate all enemies).

### Spawn Position Algorithm

Enemies spawn at the edges of the dungeon room, on one of the four sides chosen at random.

```gdscript
const ROOM_SIZE := 10  # Room is 10x10 tiles

func _get_spawn_position() -> Vector2:
    var edge := randi() % 4  # 0=top, 1=right, 2=bottom, 3=left
    var t := randi() % ROOM_SIZE  # Random tile index along the chosen edge

    var coords: Vector2i
    match edge:
        0:  # Top edge: row 0, column varies
            coords = Vector2i(t, 0)
        1:  # Right edge: column ROOM_SIZE-1, row varies
            coords = Vector2i(ROOM_SIZE - 1, t)
        2:  # Bottom edge: row ROOM_SIZE-1, column varies
            coords = Vector2i(t, ROOM_SIZE - 1)
        _:  # Left edge: column 0, row varies
            coords = Vector2i(0, t)

    # Convert tile coordinates to world (pixel) position
    return tile_map.map_to_local(coords)
```

**Edge selection probability:**
| Edge | Index | Probability | Tile positions |
|------|-------|-------------|----------------|
| Top | 0 | 25% | (0,0) through (9,0) |
| Right | 1 | 25% | (9,0) through (9,9) |
| Bottom | 2 | 25% | (0,9) through (9,9) |
| Left | 3 | 25% | (0,0) through (0,9) |

**Note:** Corner tiles (0,0), (9,0), (0,9), (9,9) can be selected by two different edges, making them slightly more likely spawn positions. This is a minor non-uniformity that does not affect gameplay.

**Spawn-in-wall behavior:**
Enemies spawn AT wall tile positions (the room perimeter IS the wall). This means they initially overlap with wall collision geometry. This is intentional and works because:
1. Enemy AI immediately starts chasing the player (moving inward).
2. `move_and_slide()` resolves the wall overlap on the first physics frame.
3. The enemy slides out of the wall toward the player within 1-2 frames.
4. Visually, enemies appear to "emerge from the walls," which is thematically appropriate for a dungeon.

**Phaser comparison:**
```javascript
spawnEnemy() {
    const edge = Phaser.Math.Between(0, 3);
    let x = 0, y = 0;
    if (edge === 0) { x = Phaser.Math.Between(0, GAME_WIDTH); y = 10; }
    else if (edge === 1) { x = GAME_WIDTH - 10; y = Phaser.Math.Between(0, GAME_HEIGHT); }
    else if (edge === 2) { x = Phaser.Math.Between(0, GAME_WIDTH); y = GAME_HEIGHT - 10; }
    else { x = 10; y = Phaser.Math.Between(0, GAME_HEIGHT); }
    // ...
}
```

The Phaser version spawns at pixel coordinates 10px from the screen edge. The Godot version spawns at tile coordinates on the room perimeter, which maps to specific pixel positions via `map_to_local()`.

### Tier Distribution

| Tier | Probability | Selection Method |
|------|-------------|-----------------|
| 1 (Green) | 33.3% | `randi_range(1, 3)` -- uniform random integer |
| 2 (Yellow) | 33.3% | Same |
| 3 (Red) | 33.3% | Same |

**Tier stats (set when enemy is spawned):**

| Tier | HP | Speed (px/s) | Damage | XP Reward | Color |
|------|------|------|--------|-----------|-------|
| 1 | 18 + 1*12 = 30 | 48 + 1*18 = 66 | 3 + 1 = 4 | 10 + 1*4 = 14 | #6bff89 (green) |
| 2 | 18 + 2*12 = 42 | 48 + 2*18 = 84 | 3 + 2 = 5 | 10 + 2*4 = 18 | #ffde66 (yellow) |
| 3 | 18 + 3*12 = 54 | 48 + 3*18 = 102 | 3 + 3 = 6 | 10 + 3*4 = 22 | #ff6f6f (red) |

**Formulas:**
```
hp = 18 + danger_tier * 12
speed = 48 + danger_tier * 18
damage = 3 + danger_tier
xp_reward = 10 + danger_tier * 4
```

**Future tier scaling:** Currently, all floors have the same uniform tier distribution. Planned enhancement: shift tier weights toward higher tiers on deeper floors. Example formula:
```
tier_1_weight = max(0.1, 0.33 - floor_number * 0.03)
tier_3_weight = min(0.6, 0.33 + floor_number * 0.03)
tier_2_weight = 1.0 - tier_1_weight - tier_3_weight
```

### Soft Cap Behavior

The soft cap (14) limits periodic spawning but NOT replacement spawning.

**Scenario walkthrough:**

| Time | Event | Enemy Count | Cap Check | Result |
|------|-------|-------------|-----------|--------|
| 0.0s | Room loads, initial spawn | 0 -> 10 | N/A | 10 spawned |
| 2.8s | SpawnTimer fires | 10 | 10 < 14 | 1 spawned (11 total) |
| 3.2s | Enemy defeated | 11 -> 10 | N/A | Replacement queued |
| 4.6s | Replacement spawns (3.2 + 1.4) | 10 -> 11 | N/A (no cap check) | 1 spawned |
| 5.6s | SpawnTimer fires | 11 | 11 < 14 | 1 spawned (12 total) |
| ... | ... | ... | ... | ... |
| 14.0s | SpawnTimer fires | 14 | 14 < 14 is FALSE | No spawn |
| 14.1s | Enemy defeated | 14 -> 13 | N/A | Replacement queued |
| 14.2s | Another enemy defeated | 13 -> 12 | N/A | Replacement queued |
| 15.5s | First replacement spawns | 12 -> 13 | N/A | Spawned |
| 15.6s | Second replacement spawns | 13 -> 14 | N/A | Spawned |
| 16.8s | SpawnTimer fires | 14 | 14 < 14 is FALSE | No spawn |

**Temporary overcap scenario:**
If the SpawnTimer fires at the same moment a replacement spawn resolves (both on the same frame), and the count is exactly 13:
1. SpawnTimer: 13 < 14 is TRUE -> spawn (now 14)
2. Replacement: no cap check -> spawn (now 15)

The count can temporarily reach 15 (or even higher if multiple replacements queue up). The periodic timer will stop adding more until enough die to bring the count below 14. The system self-corrects.

### Enemy Group Management

All enemies are added to the `"enemies"` group for easy counting and iteration:

```gdscript
# In enemy.gd _ready():
add_to_group("enemies")

# In spawn manager, to count:
get_tree().get_nodes_in_group("enemies").size()

# In player.gd, to find attack targets:
attack_range.get_overlapping_bodies()  # Area2D approach, filtered by group
```

### Edge Cases

| Scenario | Behavior |
|----------|----------|
| Player is dead | Spawning continues. Enemies do not care about player state. The periodic timer keeps running and replacements keep spawning. |
| Scene is reloading | All spawned enemies are freed with the scene. SpawnTimer is a child of the scene and is freed too. The new scene's `_ready()` starts fresh with 10 initial enemies. |
| All enemies killed simultaneously | Up to 14 replacement timers queue up. They will all fire after 1.4s, potentially spawning 14 enemies at once. The periodic timer was likely below cap and may also spawn. Worst case: 15 enemies in one burst. |
| `await` interrupted by scene change | `create_timer().timeout` will not fire if the SceneTree changes. This is safe -- the timer is garbage collected with the old scene. |
| Timer paused (tree paused) | If `process_mode` is not set, Timer nodes pause with the tree. If the game has a pause menu, spawning stops during pause. Set `process_mode = PROCESS_MODE_ALWAYS` on the SpawnTimer if spawning should continue during pause (not recommended). |

## Implementation Notes

- The spawn manager should be a Node (or Node2D) in the room scene, not an autoloaded singleton. Each room has its own spawn state.
- `ENEMY_SCENE` is preloaded at the top of the script to avoid repeated file system access.
- `randi_range(1, 3)` is inclusive on both ends in GDScript, producing 1, 2, or 3 with equal probability.
- `randi() % 4` produces 0, 1, 2, or 3. An alternative is `randi_range(0, 3)`.
- The `tile_map` reference should be obtained via `@onready var tile_map: TileMapLayer = $TileMapLayer` or passed as an export variable.
- The `entities_node` reference should be obtained via `@onready var entities_node: Node2D = $Entities`.

## Open Questions

- Should the soft cap scale with floor depth (more enemies on deeper floors)?
- Should spawn intervals decrease on deeper floors (faster spawning = harder)?
- Should there be a "spawn wave" mechanic (burst of enemies at intervals) instead of steady trickle?
- Should enemies spawn with a visual effect (fade in, portal animation) instead of appearing instantly?
- Should the player have a brief safe period after entering a room (delay before first enemies activate)?
- Should tier distribution be weighted rather than uniform? (e.g., 50% tier 1, 30% tier 2, 20% tier 3 for floor 1)
