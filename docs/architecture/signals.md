# Signal Architecture

## Summary

Every signal connection in the game, documented as a complete registry with flow diagrams showing how signals propagate through the system. Signals are the primary communication mechanism between decoupled systems in Godot -- they replace the Phaser prototype's direct function calls and callback registrations.

## Current State

The game uses 9 distinct signal connections across 2 autoload singletons, 3 Timer nodes, 1 Area2D, and 1 Button. All connections are established in `_ready()` methods (never in `_process()` or conditionally at runtime), making the signal graph static and predictable.

## Design

### Why Signals

In the Phaser prototype, communication was direct:
- `this.updateHud()` -- the scene directly called the HUD update function
- `this.defeatEnemy(enemy)` -- the scene directly managed enemy death
- `state.hp -= damage` -- state was mutated directly with no notification

This worked because everything was in one file, one scope. In Godot, code is split across many scripts attached to different nodes in different scenes. Direct calls require references, and references create coupling.

**Signals solve three problems:**

1. **Decoupling:** The emitter doesn't know who's listening. `GameState` emits `stats_changed` without knowing that `hud.gd` exists. If the HUD is removed, GameState doesn't break. If a new listener is added (e.g., a floating damage number system), GameState doesn't change.

2. **Testability:** Each system can be tested in isolation. `GameState` can be tested by calling `take_damage()` and checking that `player_died` was emitted, without needing a real HUD or death screen.

3. **Flexibility:** New consumers can connect to existing signals without modifying emitters. A sound system can connect to `EventBus.enemy_defeated` to play a death sound -- no changes to `enemy.gd` or `dungeon.gd` required.

---

### Signal Registry

Complete table of every signal in the game:

| # | Signal | Declared On | Emitted By | Parameters | Connected In | Handler Method | Purpose |
|---|--------|-------------|------------|------------|-------------|----------------|---------|
| 1 | `stats_changed` | `GameState` | GameState property setters (hp, max_hp, xp, level, floor_number) | (none) | `hud.gd._ready()` | `hud.gd._on_stats_changed()` | Update HUD display with current stats |
| 2 | `player_died` | `GameState` | `GameState.hp` setter when hp reaches 0 | (none) | `main.gd._ready()` | `main.gd._on_player_died()` | Show death screen, pause scene tree |
| 3 | `enemy_defeated` | `EventBus` | `enemy.gd.take_damage()` when enemy hp <= 0 | `position: Vector2, tier: int` | `dungeon.gd._ready()` | `dungeon.gd._on_enemy_defeated()` | Schedule enemy respawn after 1.4s delay |
| 4 | `enemy_spawned` | `EventBus` | `dungeon.gd._spawn_enemy()` after adding to tree | `enemy: Node` | (none currently) | -- | Future: sound effects, minimap |
| 5 | `player_attacked` | `EventBus` | `player.gd.handle_attack()` on successful hit | `target: Node` | (none currently) | -- | Future: sound effects, combo tracking |
| 6 | `timeout` | `SpawnTimer` (Timer) | Timer node after 2.8s elapse | (none) | `dungeon.gd._ready()` | `dungeon.gd._on_spawn_timer_timeout()` | Spawn enemy if under soft cap (14) |
| 7 | `timeout` | `HitCooldownTimer` (Timer) | Timer node after 0.7s elapse | (none) | `enemy.gd._ready()` | `enemy.gd._on_hit_cooldown_timer_timeout()` | Re-check player overlap, deal damage again |
| 8 | `body_entered` | `HitArea` (Area2D) | Godot physics engine on overlap | `body: Node2D` | `enemy.gd._ready()` | `enemy.gd._on_hit_area_body_entered()` | Initial contact damage when player enters hit zone |
| 9 | `pressed` | `RestartButton` (Button) | Button node on click/tap | (none) | `death_screen.gd._ready()` | `death_screen.gd._on_restart_button_pressed()` | Restart game from death screen |

---

### Connection Methods

Signals are connected in two ways:

**1. Code connections in `_ready()` -- for autoload and cross-scene signals:**
```gdscript
# In hud.gd._ready():
GameState.stats_changed.connect(_on_stats_changed)

# In main.gd._ready():
GameState.player_died.connect(_on_player_died)

# In dungeon.gd._ready():
EventBus.enemy_defeated.connect(_on_enemy_defeated)
spawn_timer.timeout.connect(_on_spawn_timer_timeout)
```

**2. Code connections in `_ready()` -- for child node signals:**
```gdscript
# In enemy.gd._ready():
hit_area.body_entered.connect(_on_hit_area_body_entered)
hit_cooldown.timeout.connect(_on_hit_cooldown_timer_timeout)

# In death_screen.gd._ready():
restart_button.pressed.connect(_on_restart_button_pressed)
```

**Why code connections over editor connections:** The Godot editor can visually connect signals (green icons in the Node dock). However, code connections are preferred here because:
- They are visible in the script file (easier to review, search, and understand)
- They survive scene restructuring (editor connections break if nodes are renamed or moved)
- They make the signal graph greppable: searching for `.connect(` reveals all connections
- Autoload signals cannot be connected in the editor (autoloads don't exist in the scene tree at edit time)

---

### Signal Flow Diagrams

#### Flow 1: Player Takes Damage

```
Enemy._on_hit_area_body_entered(player_body)
│
├── Guard: Is body in group "player"? If not → return
├── Guard: Is hit_cooldown timer stopped? If not → return (still on cooldown)
│
└── Enemy._deal_damage_to(player_body)
    │
    ├── Calculate damage: 3 + danger_tier
    │   ├── Tier 1: 3 + 1 = 4 damage
    │   ├── Tier 2: 3 + 2 = 5 damage
    │   └── Tier 3: 3 + 3 = 6 damage
    │
    ├── GameState.take_damage(damage)
    │   │
    │   ├── Guard: is_dead? If yes → return (no damage after death)
    │   │
    │   └── hp -= damage (triggers hp setter)
    │       │
    │       ├── hp = clampi(hp, 0, max_hp)
    │       │
    │       ├── stats_changed.emit()
    │       │   └── → HUD._on_stats_changed()
    │       │       └── Update StatsLabel.text:
    │       │           "HP: {hp} | XP: {xp} | LVL: {level} | Floor: {floor}"
    │       │
    │       └── if hp <= 0 and not is_dead:
    │           ├── is_dead = true
    │           └── player_died.emit()
    │               └── → Main._on_player_died()
    │                   ├── death_screen.visible = true
    │                   └── get_tree().paused = true
    │                       └── All nodes with default process_mode stop
    │                       └── DeathScreen continues (PROCESS_MODE_ALWAYS)
    │
    ├── player_body.shake_camera() (if method exists)
    │   └── Tween camera.offset ±3px over 0.045s each direction
    │
    └── hit_cooldown.start() (begin 0.7s cooldown)
        │
        └── After 0.7s: HitCooldownTimer.timeout.emit()
            └── → Enemy._on_hit_cooldown_timer_timeout()
                └── For each body in hit_area.get_overlapping_bodies():
                    └── If body in group "player":
                        └── Enemy._deal_damage_to(body)
                            └── (Recurse: same flow as above)
```

**Key timing:** The hit cooldown loop continues as long as the player overlaps the HitArea. Each timeout re-checks overlap and deals damage if the player is still there. When the player moves out of the HitArea, `get_overlapping_bodies()` returns an empty array and the loop stops (the timer is one_shot, so it doesn't restart).

---

#### Flow 2: Enemy Defeated

```
Player.handle_attack(delta)
│
├── Guard: attack_timer > 0? → return (still on cooldown)
│
├── Find nearest enemy:
│   ├── attack_range.get_overlapping_bodies()
│   ├── Filter to group "enemies"
│   └── Find minimum distance
│
├── Guard: No enemy found? → return
│
├── attack_timer = ATTACK_COOLDOWN (0.42s)
│
├── Calculate damage: 12 + floor(GameState.level * 1.5)
│   ├── Level 1: 12 + 1 = 13
│   ├── Level 2: 12 + 3 = 15
│   ├── Level 5: 12 + 7 = 19
│   └── Level 10: 12 + 15 = 27
│
├── nearest_enemy.take_damage(damage)
│   │
│   ├── enemy.hp -= damage
│   │
│   └── if enemy.hp <= 0:
│       │
│       ├── EventBus.enemy_defeated.emit(global_position, danger_tier)
│       │   │
│       │   └── → Dungeon._on_enemy_defeated(position, tier)
│       │       │
│       │       └── Create one-shot timer (1.4s):
│       │           await get_tree().create_timer(1.4).timeout
│       │           └── _spawn_enemy()
│       │               ├── Instance enemy scene
│       │               ├── Set danger_tier (random 1-3)
│       │               ├── Position at room edge tile
│       │               ├── Add to Entities container
│       │               └── EventBus.enemy_spawned.emit(enemy)
│       │                   └── (no current listeners)
│       │
│       ├── GameState.award_xp(10 + danger_tier * 4)
│       │   │
│       │   ├── xp += amount (triggers xp setter)
│       │   │   └── stats_changed.emit()
│       │   │       └── → HUD._on_stats_changed() → update label
│       │   │
│       │   └── if xp >= level * 90 (level up):
│       │       ├── xp -= level * 90 (triggers xp setter → stats_changed)
│       │       ├── level += 1 (triggers level setter → stats_changed)
│       │       ├── max_hp = 100 + level * 8 (triggers max_hp setter → stats_changed)
│       │       └── hp = min(max_hp, hp + 18) (triggers hp setter → stats_changed)
│       │           └── → HUD._on_stats_changed() → update label (multiple times)
│       │
│       └── queue_free() → enemy node removed from scene tree
│
├── draw_slash(nearest_enemy.global_position)
│   ├── Create Polygon2D at target position
│   ├── Random rotation -1.2 to 1.2 rad
│   ├── Tween: fade out + rise 8px over 0.12s
│   └── On tween complete: queue_free()
│
└── EventBus.player_attacked.emit(nearest_enemy)
    └── (no current listeners)
```

**Key detail about queue_free timing:** `queue_free()` defers node removal to the end of the current frame. This means the enemy node still exists when `EventBus.enemy_defeated.emit()` propagates to all listeners within the same frame. The enemy is removed only after all signal handlers have completed. This is why `enemy_defeated` passes `global_position` and `danger_tier` as values rather than the enemy node -- by the time the respawn timer fires 1.4s later, the enemy node is long gone.

---

#### Flow 3: Periodic Enemy Spawning

```
SpawnTimer.timeout (every 2.8 seconds)
│
└── → Dungeon._on_spawn_timer_timeout()
    │
    ├── Count active enemies:
    │   get_tree().get_nodes_in_group("enemies").size()
    │
    ├── Guard: count >= 14 (soft cap)? → return (do nothing)
    │
    └── _spawn_enemy()
        ├── Instance EnemyScene
        ├── Set danger_tier = randi_range(1, 3)
        ├── Position at random edge tile of the room
        ├── Add to Entities container node
        └── EventBus.enemy_spawned.emit(enemy)
            └── (no current listeners)
```

**Spawn cap interaction:** The initial spawn creates 10 enemies. The SpawnTimer adds more every 2.8s up to the soft cap of 14. When enemies are defeated, they respawn individually after 1.4s (via the `enemy_defeated` handler), independent of the SpawnTimer. Both sources respect the soft cap -- the SpawnTimer checks before spawning, and the individual respawn timer also checks (or always spawns since a death reduced the count).

---

#### Flow 4: Game Restart

```
DeathScreen receives input (R key or button click)
│
├── Path A: _unhandled_input(event)
│   └── if event is KEY_R pressed:
│       └── _restart()
│
├── Path B: RestartButton.pressed
│   └── → _on_restart_button_pressed()
│       └── _restart()
│
└── _restart()
    │
    ├── GameState.reset()
    │   ├── is_dead = false
    │   ├── max_hp = 100 (setter → stats_changed.emit())
    │   ├── hp = 100 (setter → stats_changed.emit())
    │   ├── xp = 0 (setter → stats_changed.emit())
    │   ├── level = 1 (setter → stats_changed.emit())
    │   └── floor_number = 1 (setter → stats_changed.emit())
    │
    ├── get_tree().paused = false
    │   └── All nodes resume processing
    │
    └── get_tree().reload_current_scene()
        ├── Current main.tscn and all children are freed
        │   ├── All signal connections from scene nodes are cleaned up
        │   ├── All enemies are freed
        │   └── Player is freed
        ├── main.tscn is re-instanced from disk
        │   ├── Dungeon._ready() → creates tiles, spawns player, spawns 10 enemies
        │   ├── HUD._ready() → connects to GameState.stats_changed
        │   ├── Main._ready() → connects to GameState.player_died
        │   └── DeathScreen starts with visible = false
        └── GameState and EventBus persist (autoloads survive reload)
            └── GameState now has fresh default values from reset()
```

**Critical ordering:** `GameState.reset()` must be called BEFORE `reload_current_scene()`. If reset is called after, the new scene's `_ready()` methods would read stale values (dead state, old HP). Since autoloads persist across reloads, the reset prepares clean state for the new scene tree.

---

#### Flow 5: Initial Game Start (Scene Tree Initialization Order)

```
Application starts
│
├── Autoloads instantiated (in project.godot order):
│   ├── GameState._ready() → (no custom _ready needed, properties have defaults)
│   └── EventBus._ready() → (no custom _ready needed, only signal declarations)
│
└── Main scene (main.tscn) instantiated:
    │
    ├── Main._ready()
    │   └── GameState.player_died.connect(_on_player_died)
    │
    ├── Dungeon._ready()
    │   ├── Create TileSet programmatically
    │   ├── Paint tiles (floor + walls)
    │   ├── Instance Player scene
    │   │   └── Player._ready()
    │   │       └── Add to group "player"
    │   ├── Add Player to Entities container
    │   ├── Spawn 10 initial enemies (loop):
    │   │   └── _spawn_enemy() × 10
    │   │       ├── Instance EnemyScene
    │   │       │   └── Enemy._ready()
    │   │       │       ├── Add to group "enemies"
    │   │       │       ├── Set hp, speed, color from danger_tier
    │   │       │       ├── hit_area.body_entered.connect(...)
    │   │       │       └── hit_cooldown.timeout.connect(...)
    │   │       ├── Add to Entities container
    │   │       └── EventBus.enemy_spawned.emit(enemy)
    │   ├── spawn_timer.timeout.connect(_on_spawn_timer_timeout)
    │   └── EventBus.enemy_defeated.connect(_on_enemy_defeated)
    │
    ├── HUD._ready()
    │   ├── GameState.stats_changed.connect(_on_stats_changed)
    │   └── _on_stats_changed() → initial HUD text
    │
    └── DeathScreen._ready()
        └── restart_button.pressed.connect(_on_restart_button_pressed)

SpawnTimer starts automatically (autostart=true)
└── First timeout fires after 2.8s
```

---

### Signal Lifetime and Cleanup

**Autoload signals (GameState, EventBus):**
- Signals persist for the lifetime of the application
- Connections from scene nodes are cleaned up when those nodes are freed (e.g., on `reload_current_scene()`)
- New connections are established when the scene is re-instanced (in `_ready()`)
- Autoload signals are never disconnected manually

**Node signals (Timer.timeout, Area2D.body_entered, Button.pressed):**
- These signals exist on nodes within the scene tree
- When the owning node is freed, the signal and all its connections are destroyed
- On scene reload, new nodes create new signals, and new `_ready()` calls establish new connections

**Memory safety:** Godot's signal system is reference-counted and safe. If a listener node is freed while a signal is emitting, the freed connection is skipped (no crash, no dangling pointer). This matters for `EventBus.enemy_defeated` -- if the dungeon scene is somehow being freed while an enemy emits the signal, the handler simply doesn't run.

---

### Signal Debugging

Godot provides tools for inspecting signal connections at runtime:

1. **Remote Scene Tree (F5 debugger):** Shows all instanced nodes and their signal connections in the "Node" dock's "Signals" tab
2. **`print()` in handlers:** Adding `print("stats_changed")` to `_on_stats_changed()` confirms the signal is firing
3. **`is_connected()` check:** `GameState.stats_changed.is_connected(_on_stats_changed)` returns `true` if the connection exists

**Common debugging pattern:**
```gdscript
func _ready() -> void:
    GameState.stats_changed.connect(_on_stats_changed)
    # Verify connection was established
    assert(GameState.stats_changed.is_connected(_on_stats_changed), "HUD failed to connect to stats_changed")
```

## Implementation Notes

- All signal connections use the `signal.connect(callable)` syntax (Godot 4 style). The Godot 3 `connect("signal_name", object, "method_name")` syntax is deprecated and not used.
- No signals use `CONNECT_DEFERRED` flag. All handlers run synchronously within the emitting frame. If deferred execution is ever needed (e.g., freeing nodes inside a signal handler), `call_deferred()` can be used inside the handler.
- The `EventBus.enemy_spawned` and `EventBus.player_attacked` signals currently have no listeners. They are declared proactively for future systems. This is intentional: defining the signal interface early establishes a contract that future systems can rely on.
- Signal emission order within a single frame is deterministic: handlers are called in the order they were connected. Since each signal has at most one listener currently, ordering is not a concern.

## Open Questions

- Should there be a `player_leveled_up(new_level: int)` signal on GameState or EventBus for a dedicated level-up VFX system?
- Should `stats_changed` pass the changed property name as a parameter for selective updates, or is "update everything" acceptable?
- Should `enemy_defeated` include the XP amount awarded, so a damage number / XP popup system can use it directly?
- Should a `game_restarted` signal exist on GameState (emitted by `reset()`) for systems that need to clean up on restart?
- Should the HitArea `body_entered` / `body_exited` signals be used to maintain an overlap list, or is polling `get_overlapping_bodies()` on cooldown timeout sufficient?
