# Autoload Singletons

## Summary

Two autoload singletons provide global state and cross-system communication: `GameState` holds all player stats and emits signals when they change, and `EventBus` provides decoupled signals for combat and spawning events. Together, they replace the Phaser prototype's plain `const state = {}` object and ad-hoc function calls.

## Current State

Both autoloads are registered in `project.godot` and available from any script by name. `GameState` manages HP, XP, level, floor number, and death state. `EventBus` carries event signals that don't belong to any single node (enemy defeated, enemy spawned, player attacked).

## Design

### Why Autoloads

In Godot, autoloads are scripts (or scenes) that are automatically instanced when the game starts and persist across scene changes. They are accessible from any script via their registered name (e.g., `GameState.hp`).

**Problem they solve:** In the Phaser prototype, the `state` object was a closure variable inside the IIFE. Any function in the same scope could read and write it. In Godot, scripts are attached to individual nodes in separate scene files -- there is no shared scope. Without autoloads, every node that needs to read HP would need a reference to the node that owns HP, creating tight coupling and fragile reference chains.

**Why two autoloads instead of one:** Separation of concerns. `GameState` is about data (what are the current stats?). `EventBus` is about events (what just happened?). Some signals (like `stats_changed`) naturally belong to the state owner. Others (like `enemy_defeated`) are about gameplay events that multiple unrelated systems might care about. Keeping them separate means a node can connect to `EventBus` without pulling in state dependencies, and vice versa.

---

### GameState (scripts/autoloads/game_state.gd)

#### Registration

In `project.godot`:
```ini
[autoload]
GameState="*res://scripts/autoloads/game_state.gd"
```

The `*` prefix means "load as a singleton" (this is Godot's convention for autoload entries). The script is attached to an automatically-created Node that persists for the lifetime of the application.

#### Purpose

Centralized, reactive game state. Replaces the Phaser prototype's `const state = { hp: 100, xp: 0, level: 1 }` with a GDScript singleton that emits signals on every change. Any node in the game can read `GameState.hp` or connect to `GameState.stats_changed` without needing a direct reference to another node.

#### Signals

| Signal | Parameters | Emitted When | Listeners |
|--------|------------|--------------|-----------|
| `stats_changed` | (none) | Any stat property changes (hp, max_hp, xp, level, floor_number) | `hud.gd` -- updates the stats label text |
| `player_died` | (none) | HP reaches 0 for the first time (not emitted on subsequent damage while dead) | `main.gd` -- shows death screen, pauses tree |

**Why no parameters on stats_changed:** The HUD reads all stats every time it updates (it formats a single string with all values). Passing individual changed values would add complexity without benefit -- the HUD doesn't conditionally update.

**Why player_died is separate from stats_changed:** Death is a one-time catastrophic event that triggers specific behavior (pause game, show death screen). It should not be conflated with routine stat updates. Separate signals allow `main.gd` to connect only to `player_died` without processing every HP change.

#### Properties

| Property | Type | Default | Setter | Description |
|----------|------|---------|--------|-------------|
| `hp` | `int` | `100` | Custom setter | Current hit points. Clamped to `[0, max_hp]`. Setter emits `stats_changed`. If HP reaches 0 and `is_dead` is false, sets `is_dead = true` and emits `player_died`. |
| `max_hp` | `int` | `100` | Custom setter | Maximum hit points. Increases on level up via formula `100 + level * 8`. Setter emits `stats_changed`. |
| `xp` | `int` | `0` | Custom setter | Current experience points toward next level. Setter emits `stats_changed`. |
| `level` | `int` | `1` | Custom setter | Current character level. Setter emits `stats_changed`. |
| `floor_number` | `int` | `1` | Custom setter | Current dungeon floor. Setter emits `stats_changed`. |
| `is_dead` | `bool` | `false` | Direct assignment | Death flag. Prevents `player_died` from being emitted multiple times if damage continues after death (e.g., enemies still overlapping during the frame death occurs). Not emitted via signal -- it's an internal guard. |

**Why custom setters:** GDScript supports the `set` keyword on properties to run code whenever a value is assigned. This is the reactive pattern: assigning `GameState.hp = 80` automatically emits `stats_changed` without the caller needing to remember to emit it. This prevents bugs where a stat changes but the HUD doesn't update.

**Setter behavior detail -- hp:**
```gdscript
var hp: int = 100:
    set(value):
        hp = clampi(value, 0, max_hp)
        stats_changed.emit()
        if hp <= 0 and not is_dead:
            is_dead = true
            player_died.emit()
```
- `clampi(value, 0, max_hp)` ensures HP never goes negative or exceeds max_hp
- `stats_changed` is emitted on every change, even if the clamped value equals the old value (simplicity over optimization)
- The `is_dead` guard ensures `player_died` fires exactly once per death

**Setter behavior detail -- max_hp, xp, level, floor_number:**
```gdscript
var max_hp: int = 100:
    set(value):
        max_hp = value
        stats_changed.emit()

var xp: int = 0:
    set(value):
        xp = value
        stats_changed.emit()

var level: int = 1:
    set(value):
        level = value
        stats_changed.emit()

var floor_number: int = 1:
    set(value):
        floor_number = value
        stats_changed.emit()
```

#### Methods

##### `reset() -> void`

Resets all state to initial values. Called by the death screen before reloading the scene.

```gdscript
func reset() -> void:
    is_dead = false
    # Set max_hp before hp so the clamp in hp's setter uses the correct max
    max_hp = 100
    hp = 100
    xp = 0
    level = 1
    floor_number = 1
```

**Note on ordering:** `max_hp` must be set before `hp` because `hp`'s setter clamps to `max_hp`. If `hp` were set first while `max_hp` was still a higher value from a previous run, `hp` would be clamped incorrectly. Setting `is_dead = false` first (without a setter) ensures the `player_died` signal is not re-emitted if `hp` passes through 0 during reset.

**Note on signal emission:** Each property assignment triggers `stats_changed.emit()`. This means `reset()` emits `stats_changed` five times (once per property). This is acceptable because `reset()` is called immediately before `reload_current_scene()`, which destroys all listeners anyway. If performance ever matters, a batch pattern could be introduced (suppress signals during reset, emit once at the end).

##### `award_xp(amount: int) -> void`

Awards experience points and handles level-up if the threshold is met.

```gdscript
func award_xp(amount: int) -> void:
    xp += amount  # Triggers xp setter -> stats_changed

    var xp_to_level: int = level * 90
    if xp >= xp_to_level:
        xp -= xp_to_level  # Triggers xp setter -> stats_changed
        level += 1          # Triggers level setter -> stats_changed
        max_hp = 100 + level * 8  # Triggers max_hp setter -> stats_changed
        hp = mini(max_hp, hp + 18)  # Triggers hp setter -> stats_changed
```

**XP threshold formula:** `level * 90`. At level 1, need 90 XP to reach level 2. At level 2, need 180 XP to reach level 3. This is a linear scaling curve that matches the Phaser prototype's `state.level * 90`.

**Level-up effects (all happen in sequence):**
1. Subtract the threshold from current XP (leftover XP carries over)
2. Increment level by 1
3. Recalculate max_hp: `100 + level * 8` (at level 2: 116, level 3: 124, level 10: 180)
4. Heal: `min(max_hp, hp + 18)` -- heals 18 HP but never exceeds max_hp

**Why `mini()` instead of `min()`:** In GDScript 4, `mini()` is the integer-specific version of `min()`. Both work, but `mini()` is explicit about integer comparison.

**Edge case -- multiple level-ups:** The current implementation only checks for one level-up per `award_xp()` call. If an enemy awards enough XP to skip a level (unlikely with current values, but possible with future changes), only one level-up occurs. A `while` loop could be used instead of `if` to handle multi-level-ups:
```gdscript
while xp >= level * 90:
    xp -= level * 90
    level += 1
    max_hp = 100 + level * 8
    hp = mini(max_hp, hp + 18)
```
This is listed as a potential improvement but not implemented in the initial version to match the Phaser prototype exactly.

##### `take_damage(amount: int) -> void`

Reduces HP by the given amount. Death detection is handled by the `hp` setter.

```gdscript
func take_damage(amount: int) -> void:
    if is_dead:
        return
    hp -= amount  # Triggers hp setter -> clamps to 0, emits stats_changed, may emit player_died
```

**Why the is_dead guard here AND in the setter:** Belt and suspenders. The setter guard prevents `player_died` from double-emitting. The `take_damage` guard prevents any HP modification after death. Both are cheap checks that prevent subtle bugs.

**Why not `hp = max(0, hp - amount)`:** The `hp` setter already clamps to `[0, max_hp]`, so explicit clamping in `take_damage` would be redundant. The subtraction can safely produce a negative intermediate value because the setter handles it.

#### Full GDScript Pseudocode

```gdscript
extends Node

# --- Signals ---
signal stats_changed
signal player_died

# --- Properties with setters ---
var hp: int = 100:
    set(value):
        hp = clampi(value, 0, max_hp)
        stats_changed.emit()
        if hp <= 0 and not is_dead:
            is_dead = true
            player_died.emit()

var max_hp: int = 100:
    set(value):
        max_hp = value
        stats_changed.emit()

var xp: int = 0:
    set(value):
        xp = value
        stats_changed.emit()

var level: int = 1:
    set(value):
        level = value
        stats_changed.emit()

var floor_number: int = 1:
    set(value):
        floor_number = value
        stats_changed.emit()

var is_dead: bool = false

# --- Methods ---
func reset() -> void:
    is_dead = false
    max_hp = 100
    hp = 100
    xp = 0
    level = 1
    floor_number = 1

func award_xp(amount: int) -> void:
    xp += amount
    var xp_to_level: int = level * 90
    if xp >= xp_to_level:
        xp -= xp_to_level
        level += 1
        max_hp = 100 + level * 8
        hp = mini(max_hp, hp + 18)

func take_damage(amount: int) -> void:
    if is_dead:
        return
    hp -= amount
```

---

### EventBus (scripts/autoloads/event_bus.gd)

#### Registration

In `project.godot`:
```ini
[autoload]
GameState="*res://scripts/autoloads/game_state.gd"
EventBus="*res://scripts/autoloads/event_bus.gd"
```

**Load order:** GameState is listed first and loaded before EventBus. This doesn't currently matter (they don't reference each other at load time), but it establishes a convention: state before events.

#### Purpose

Decoupled signal hub for gameplay events that don't belong to any single node. While `GameState` signals are about stat changes (reactive data), `EventBus` signals are about gameplay events (things that happened in the world).

**Why separate from GameState:** Consider `enemy_defeated`. This event is emitted by `enemy.gd`, but it's consumed by `dungeon.gd` (to schedule a respawn) and potentially by a future sound system (to play a death sound) and a future particle system (to spawn death particles). None of these consumers care about GameState. If `enemy_defeated` were on GameState, those systems would need to import/reference GameState for no reason. EventBus keeps event routing orthogonal to state management.

**Why not direct function calls:** When an enemy dies, it could directly call `dungeon.respawn_enemy()`. But then the enemy needs a reference to the dungeon. And if a sound system also needs to know about deaths, the enemy needs a reference to that too. Every new consumer adds a dependency to the enemy script. With EventBus, the enemy emits a signal and doesn't know or care who's listening. New consumers connect to EventBus without modifying the enemy at all.

#### Signals

| Signal | Parameters | Emitted By | Current Listeners | Future Listeners |
|--------|------------|-----------|-------------------|------------------|
| `enemy_defeated` | `position: Vector2, tier: int` | `enemy.gd.take_damage()` when HP <= 0 | `dungeon.gd` -- schedules respawn after 1.4s | Sound system (death SFX), particle system (death VFX), achievement tracker |
| `enemy_spawned` | `enemy: Node` | `dungeon.gd._spawn_enemy()` after adding enemy to tree | (none currently) | Sound system (spawn SFX), minimap (enemy blip) |
| `player_attacked` | `target: Node` | `player.gd.handle_attack()` on successful hit | (none currently) | Sound system (attack SFX), combo tracker, statistics |

**Signal parameter design choices:**

`enemy_defeated(position: Vector2, tier: int)`:
- `position` is passed explicitly (not as the enemy node) because the enemy calls `queue_free()` immediately after emitting. By the time listeners process the signal, the enemy node may already be freed. Passing the position as a raw Vector2 ensures it remains valid.
- `tier` is passed so listeners can vary their response by enemy difficulty (e.g., different death sounds for different tiers, more particles for higher tiers).

`enemy_spawned(enemy: Node)`:
- The full enemy node is passed because the enemy continues to exist after spawning. Listeners may need to reference the node (e.g., a minimap adding a tracking dot).

`player_attacked(target: Node)`:
- The target enemy node is passed so listeners can determine which enemy was hit (e.g., for hit confirmation sounds that vary by enemy type).
- The target node is guaranteed to still exist at signal emission time (it may die from the damage, but `queue_free()` happens after the signal propagates within the same frame).

#### Full GDScript Pseudocode

```gdscript
extends Node

# --- Signals ---

## Emitted when an enemy's HP reaches 0 and it is about to be freed.
## @param position: The enemy's global_position at the moment of death.
## @param tier: The enemy's danger_tier (1, 2, or 3).
signal enemy_defeated(position: Vector2, tier: int)

## Emitted after a new enemy instance is added to the scene tree.
## @param enemy: The enemy Node (CharacterBody2D) that was just spawned.
signal enemy_spawned(enemy: Node)

## Emitted when the player successfully attacks an enemy (after cooldown, within range).
## @param target: The enemy Node that was attacked.
signal player_attacked(target: Node)
```

That's it -- the EventBus script contains only signal declarations. No methods, no state, no `_ready()`, no `_process()`. It is the thinnest possible script: just a place for signals to live.

**Why such a minimal script:** EventBus is a pure signal relay point. It doesn't process or transform events. It doesn't store state. Adding logic to EventBus would defeat its purpose -- it would become a "god object" that knows about everything. By keeping it to pure signal declarations, each system retains ownership of its own logic.

---

### How Autoloads Are Accessed

From any script in the game, autoloads are accessed by their registered name:

```gdscript
# Reading state
var current_hp = GameState.hp
var player_level = GameState.level

# Modifying state (triggers setters and signals)
GameState.hp -= 10
GameState.award_xp(14)

# Connecting to state signals
GameState.stats_changed.connect(_on_stats_changed)
GameState.player_died.connect(_on_player_died)

# Emitting events
EventBus.enemy_defeated.emit(global_position, danger_tier)
EventBus.player_attacked.emit(nearest_enemy)

# Connecting to event signals
EventBus.enemy_defeated.connect(_on_enemy_defeated)
```

**No imports needed:** Autoloads are registered as global names in the ScriptServer. GDScript does not require `import` or `require` statements -- `GameState` and `EventBus` are available everywhere, like built-in singletons (`Input`, `Engine`, `OS`).

**Lifecycle:** Autoloads are created before the main scene loads and destroyed after the main scene is freed. They survive `reload_current_scene()` -- their state persists across scene reloads. This is why `GameState.reset()` must be called explicitly before reloading: the old HP/XP/level values would otherwise carry over.

---

### project.godot Autoload Section

The complete autoload configuration block:

```ini
[autoload]

GameState="*res://scripts/autoloads/game_state.gd"
EventBus="*res://scripts/autoloads/event_bus.gd"
```

**Key details:**
- The `*` prefix is required for singleton autoloads (scripts that are instanced automatically)
- Paths are relative to `res://` (the project root)
- Order matters for load sequence: GameState loads first, EventBus second
- Both scripts extend Node (the simplest base class -- no spatial, no rendering, no physics)

## Implementation Notes

- Autoloads survive `get_tree().reload_current_scene()`. Always call `GameState.reset()` before reloading to clear stale state.
- Signal connections from scene nodes to autoloads are one-directional: scene nodes connect to autoload signals in their `_ready()`. When the scene is freed (on reload), those connections are automatically cleaned up. Autoloads never need to disconnect manually.
- The `stats_changed` signal fires frequently (every stat change, including during `reset()` and `award_xp()`). This is acceptable for the current single-listener (HUD) but should be reviewed if more listeners are added. A "batch update" pattern (suppress signals, emit once) could be introduced if profiling shows a bottleneck.
- `EventBus` signals use typed parameters in their declarations. This provides autocompletion in the Godot editor and catches type mismatches at parse time.

## Open Questions

- Should `GameState` support multiple save slots, or is single-state sufficient for the MVP?
- Should `award_xp()` use a `while` loop to handle multi-level-ups, or is single-level-up-per-call acceptable?
- Should `EventBus` include a `player_damaged(amount: int, source: Node)` signal for future damage number display?
- Should autoloads be scripts (current approach) or scenes (`.tscn` with a root Node)? Scripts are simpler; scenes allow adding child nodes (e.g., a Timer for auto-save on GameState).
- Should `floor_number` live on GameState or on a separate `DungeonState` autoload to separate character state from world state?
