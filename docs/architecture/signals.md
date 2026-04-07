# Signal Architecture

## Summary

Every signal connection in the game, documented as a complete registry with flow diagrams showing how signals propagate through the system. Signals are the primary communication mechanism between decoupled systems in Godot -- they replace the Phaser prototype's direct function calls and callback registrations.

## Current State

The game uses 9 distinct signal connections across 2 autoload singletons, 3 Timer nodes, 1 Area2D, and 1 Button. All connections are established in `_Ready()` methods (never in `_Process()` or conditionally at runtime), making the signal graph static and predictable.

## Design

### Why Signals

In the Phaser prototype, communication was direct:
- `this.updateHud()` -- the scene directly called the HUD update function
- `this.defeatEnemy(enemy)` -- the scene directly managed enemy death
- `state.hp -= damage` -- state was mutated directly with no notification

This worked because everything was in one file, one scope. In Godot, code is split across many scripts attached to different nodes in different scenes. Direct calls require references, and references create coupling.

**Signals solve three problems:**

1. **Decoupling:** The emitter doesn't know who's listening. `GameState` emits `StatsChanged` without knowing that the HUD exists. If the HUD is removed, GameState doesn't break. If a new listener is added (e.g., a floating damage number system), GameState doesn't change.

2. **Testability:** Each system can be tested in isolation. `GameState` can be tested by calling `TakeDamage()` and checking that `PlayerDied` was emitted, without needing a real HUD or death screen.

3. **Flexibility:** New consumers can connect to existing signals without modifying emitters. A sound system can connect to `EventBus.EnemyDefeated` to play a death sound -- no changes to the enemy or dungeon scripts required.

---

### Signal Registry

Complete table of every signal in the game:

| # | Signal | Declared On | Emitted By | Parameters | Connected In | Handler Method | Purpose |
|---|--------|-------------|------------|------------|-------------|----------------|---------|
| 1 | `StatsChanged` | `GameState` | GameState property setters (Hp, MaxHp, Xp, Level, FloorNumber) | (none) | `Hud._Ready()` | `Hud.OnStatsChanged()` | Update HUD display with current stats |
| 2 | `PlayerDied` | `GameState` | `GameState.Hp` setter when Hp reaches 0 | (none) | `Main._Ready()` | `Main.OnPlayerDied()` | Show death screen, pause scene tree |
| 3 | `EnemyDefeated` | `EventBus` | `Enemy.TakeDamage()` when enemy hp <= 0 | `Vector2 position, int tier` | `Dungeon._Ready()` | `Dungeon.OnEnemyDefeated()` | Schedule enemy respawn after 1.4s delay |
| 4 | `EnemySpawned` | `EventBus` | `Dungeon.SpawnEnemy()` after adding to tree | `Node enemy` | (none currently) | -- | Future: sound effects, minimap |
| 5 | `PlayerAttacked` | `EventBus` | `Player.HandleAttack()` on successful hit | `Node target` | (none currently) | -- | Future: sound effects, combo tracking |
| 6 | `Timeout` | `SpawnTimer` (Timer) | Timer node after 2.8s elapse | (none) | `Dungeon._Ready()` | `Dungeon.OnSpawnTimerTimeout()` | Spawn enemy if under soft cap (14) |
| 7 | `Timeout` | `HitCooldownTimer` (Timer) | Timer node after 0.7s elapse | (none) | `Enemy._Ready()` | `Enemy.OnHitCooldownTimerTimeout()` | Re-check player overlap, deal damage again |
| 8 | `BodyEntered` | `HitArea` (Area2D) | Godot physics engine on overlap | `Node2D body` | `Enemy._Ready()` | `Enemy.OnHitAreaBodyEntered()` | Initial contact damage when player enters hit zone |
| 9 | `Pressed` | `RestartButton` (Button) | Button node on click/tap | (none) | `DeathScreen._Ready()` | `DeathScreen.OnRestartButtonPressed()` | Restart game from death screen |

---

### Connection Methods

Signals are connected in two ways:

**1. Code connections in `_Ready()` -- for autoload and cross-scene signals:**
```csharp
// In Hud._Ready():
var gameState = GetNode<GameState>("/root/GameState");
gameState.Connect(GameState.SignalName.StatsChanged, new Callable(this, MethodName.OnStatsChanged));

// In Main._Ready():
var gameState = GetNode<GameState>("/root/GameState");
gameState.Connect(GameState.SignalName.PlayerDied, new Callable(this, MethodName.OnPlayerDied));

// In Dungeon._Ready():
var eventBus = GetNode<EventBus>("/root/EventBus");
eventBus.Connect(EventBus.SignalName.EnemyDefeated, new Callable(this, MethodName.OnEnemyDefeated));
spawnTimer.Connect("timeout", new Callable(this, MethodName.OnSpawnTimerTimeout));
```

**2. Code connections in `_Ready()` -- for child node signals:**
```csharp
// In Enemy._Ready():
hitArea.Connect("body_entered", new Callable(this, MethodName.OnHitAreaBodyEntered));
hitCooldown.Connect("timeout", new Callable(this, MethodName.OnHitCooldownTimerTimeout));

// In DeathScreen._Ready():
restartButton.Connect("pressed", new Callable(this, MethodName.OnRestartButtonPressed));
```

**Why code connections over editor connections:** The Godot editor can visually connect signals (green icons in the Node dock). However, code connections are preferred here because:
- They are visible in the script file (easier to review, search, and understand)
- They survive scene restructuring (editor connections break if nodes are renamed or moved)
- They make the signal graph greppable: searching for `.Connect(` reveals all connections
- Autoload signals cannot be connected in the editor (autoloads don't exist in the scene tree at edit time)

**`Connect()` vs `+=` (C# events):** Godot C# supports both `source.Connect(SignalName.X, callable)` and `source.X += handler`. Prefer `Connect()` because connections made this way are automatically cleaned up when either the source or target node is freed. With `+=`, you must manually unsubscribe to avoid dangling references if the listener is freed before the emitter.

---

### Signal Flow Diagrams

#### Flow 1: Player Takes Damage

```
Enemy.OnHitAreaBodyEntered(playerBody)
│
├── Guard: Is body in group "player"? If not → return
├── Guard: Is hitCooldown timer stopped? If not → return (still on cooldown)
│
└── Enemy.DealDamageTo(playerBody)
    │
    ├── Calculate damage: 3 + dangerTier
    │   ├── Tier 1: 3 + 1 = 4 damage
    │   ├── Tier 2: 3 + 2 = 5 damage
    │   └── Tier 3: 3 + 3 = 6 damage
    │
    ├── GameState.TakeDamage(damage)
    │   │
    │   ├── Guard: IsDead? If yes → return (no damage after death)
    │   │
    │   └── Hp -= damage (triggers Hp setter)
    │       │
    │       ├── _hp = Math.Clamp(value, 0, MaxHp)
    │       │
    │       ├── EmitSignal(SignalName.StatsChanged)
    │       │   └── → Hud.OnStatsChanged()
    │       │       └── Update StatsLabel.Text:
    │       │           "HP: {Hp} | XP: {Xp} | LVL: {Level} | Floor: {FloorNumber}"
    │       │
    │       └── if _hp <= 0 && !IsDead:
    │           ├── IsDead = true
    │           └── EmitSignal(SignalName.PlayerDied)
    │               └── → Main.OnPlayerDied()
    │                   ├── deathScreen.Visible = true
    │                   └── GetTree().Paused = true
    │                       └── All nodes with default ProcessMode stop
    │                       └── DeathScreen continues (ProcessModeEnum.Always)
    │
    ├── playerBody.ShakeCamera() (if method exists)
    │   └── Tween camera.Offset ±3px over 0.045s each direction
    │
    └── hitCooldown.Start() (begin 0.7s cooldown)
        │
        └── After 0.7s: HitCooldownTimer.Timeout emits
            └── → Enemy.OnHitCooldownTimerTimeout()
                └── For each body in hitArea.GetOverlappingBodies():
                    └── If body in group "player":
                        └── Enemy.DealDamageTo(body)
                            └── (Recurse: same flow as above)
```

**Key timing:** The hit cooldown loop continues as long as the player overlaps the HitArea. Each timeout re-checks overlap and deals damage if the player is still there. When the player moves out of the HitArea, `GetOverlappingBodies()` returns an empty array and the loop stops (the timer is OneShot, so it doesn't restart).

---

#### Flow 2: Enemy Defeated

```
Player.HandleAttack(delta)
│
├── Guard: attackTimer > 0? → return (still on cooldown)
│
├── Find nearest enemy:
│   ├── attackRange.GetOverlappingBodies()
│   ├── Filter to group "enemies"
│   └── Find minimum distance
│
├── Guard: No enemy found? → return
│
├── attackTimer = AttackCooldown (0.42s)
│
├── Calculate damage: 12 + (int)Math.Floor(GameState.Level * 1.5)
│   ├── Level 1: 12 + 1 = 13
│   ├── Level 2: 12 + 3 = 15
│   ├── Level 5: 12 + 7 = 19
│   └── Level 10: 12 + 15 = 27
│
├── nearestEnemy.TakeDamage(damage)
│   │
│   ├── enemy.Hp -= damage
│   │
│   └── if enemy.Hp <= 0:
│       │
│       ├── EventBus.EmitSignal(SignalName.EnemyDefeated, GlobalPosition, DangerTier)
│       │   │
│       │   └── → Dungeon.OnEnemyDefeated(position, tier)
│       │       │
│       │       └── Create one-shot timer (1.4s):
│       │           await ToSignal(GetTree().CreateTimer(1.4), "timeout")
│       │           └── SpawnEnemy()
│       │               ├── Instance enemy scene
│       │               ├── Set DangerTier (random 1-3)
│       │               ├── Position at room edge tile
│       │               ├── Add to Entities container
│       │               └── EventBus.EmitSignal(SignalName.EnemySpawned, enemy)
│       │                   └── (no current listeners)
│       │
│       ├── GameState.AwardXp(10 + DangerTier * 4)
│       │   │
│       │   ├── Xp += amount (triggers Xp setter)
│       │   │   └── EmitSignal(SignalName.StatsChanged)
│       │   │       └── → Hud.OnStatsChanged() → update label
│       │   │
│       │   └── while Xp >= Level * 90 (level up):
│       │       ├── Xp -= Level * 90 (triggers Xp setter → StatsChanged)
│       │       ├── Level += 1 (triggers Level setter → StatsChanged)
│       │       ├── MaxHp = 100 + Level * 8 (triggers MaxHp setter → StatsChanged)
│       │       └── Hp = Math.Min(MaxHp, Hp + 18) (triggers Hp setter → StatsChanged)
│       │           └── → Hud.OnStatsChanged() → update label (multiple times)
│       │
│       └── QueueFree() → enemy node removed from scene tree
│
├── DrawSlash(nearestEnemy.GlobalPosition)
│   ├── Create Polygon2D at target position
│   ├── Random rotation -1.2 to 1.2 rad
│   ├── Tween: fade out + rise 8px over 0.12s
│   └── On tween complete: QueueFree()
│
└── EventBus.EmitSignal(SignalName.PlayerAttacked, nearestEnemy)
    └── (no current listeners)
```

**Key detail about QueueFree timing:** `QueueFree()` defers node removal to the end of the current frame. This means the enemy node still exists when `EventBus.EmitSignal(SignalName.EnemyDefeated, ...)` propagates to all listeners within the same frame. The enemy is removed only after all signal handlers have completed. This is why `EnemyDefeated` passes `GlobalPosition` and `DangerTier` as values rather than the enemy node -- by the time the respawn timer fires 1.4s later, the enemy node is long gone.

---

#### Flow 3: Periodic Enemy Spawning

```
SpawnTimer.Timeout (every 2.8 seconds)
│
└── → Dungeon.OnSpawnTimerTimeout()
    │
    ├── Count active enemies:
    │   GetTree().GetNodesInGroup("enemies").Count
    │
    ├── Guard: count >= 14 (soft cap)? → return (do nothing)
    │
    └── SpawnEnemy()
        ├── Instance EnemyScene
        ├── Set DangerTier = GD.RandRange(1, 3)
        ├── Position at random edge tile of the room
        ├── Add to Entities container node
        └── EventBus.EmitSignal(SignalName.EnemySpawned, enemy)
            └── (no current listeners)
```

**Spawn cap interaction:** The initial spawn creates 10 enemies. The SpawnTimer adds more every 2.8s up to the soft cap of 14. When enemies are defeated, they respawn individually after 1.4s (via the `EnemyDefeated` handler), independent of the SpawnTimer. Both sources respect the soft cap -- the SpawnTimer checks before spawning, and the individual respawn timer also checks (or always spawns since a death reduced the count).

---

#### Flow 4: Game Restart

```
DeathScreen receives input (R key or button click)
│
├── Path A: _UnhandledInput(InputEvent @event)
│   └── if @event is KEY_R pressed:
│       └── Restart()
│
├── Path B: RestartButton.Pressed
│   └── → OnRestartButtonPressed()
│       └── Restart()
│
└── Restart()
    │
    ├── GameState.Reset()
    │   ├── IsDead = false
    │   ├── MaxHp = 100 (setter → EmitSignal(SignalName.StatsChanged))
    │   ├── Hp = 100 (setter → EmitSignal(SignalName.StatsChanged))
    │   ├── Xp = 0 (setter → EmitSignal(SignalName.StatsChanged))
    │   ├── Level = 1 (setter → EmitSignal(SignalName.StatsChanged))
    │   └── FloorNumber = 1 (setter → EmitSignal(SignalName.StatsChanged))
    │
    ├── GetTree().Paused = false
    │   └── All nodes resume processing
    │
    └── GetTree().ReloadCurrentScene()
        ├── Current main.tscn and all children are freed
        │   ├── All signal connections from scene nodes are cleaned up
        │   ├── All enemies are freed
        │   └── Player is freed
        ├── main.tscn is re-instanced from disk
        │   ├── Dungeon._Ready() → creates tiles, spawns player, spawns 10 enemies
        │   ├── Hud._Ready() → connects to GameState.StatsChanged
        │   ├── Main._Ready() → connects to GameState.PlayerDied
        │   └── DeathScreen starts with Visible = false
        └── GameState and EventBus persist (autoloads survive reload)
            └── GameState now has fresh default values from Reset()
```

**Critical ordering:** `GameState.Reset()` must be called BEFORE `ReloadCurrentScene()`. If Reset is called after, the new scene's `_Ready()` methods would read stale values (dead state, old HP). Since autoloads persist across reloads, the reset prepares clean state for the new scene tree.

---

#### Flow 5: Initial Game Start (Scene Tree Initialization Order)

```
Application starts
│
├── Autoloads instantiated (in project.godot order):
│   ├── GameState._Ready() → (no custom _Ready needed, properties have defaults)
│   └── EventBus._Ready() → (no custom _Ready needed, only signal declarations)
│
└── Main scene (main.tscn) instantiated:
    │
    ├── Main._Ready()
    │   └── gameState.Connect(SignalName.PlayerDied, new Callable(this, MethodName.OnPlayerDied))
    │
    ├── Dungeon._Ready()
    │   ├── Create TileSet programmatically
    │   ├── Paint tiles (floor + walls)
    │   ├── Instance Player scene
    │   │   └── Player._Ready()
    │   │       └── Add to group "player"
    │   ├── Add Player to Entities container
    │   ├── Spawn 10 initial enemies (loop):
    │   │   └── SpawnEnemy() × 10
    │   │       ├── Instance EnemyScene
    │   │       │   └── Enemy._Ready()
    │   │       │       ├── Add to group "enemies"
    │   │       │       ├── Set Hp, Speed, Color from DangerTier
    │   │       │       ├── hitArea.Connect("body_entered", ...)
    │   │       │       └── hitCooldown.Connect("timeout", ...)
    │   │       ├── Add to Entities container
    │   │       └── EventBus.EmitSignal(SignalName.EnemySpawned, enemy)
    │   ├── spawnTimer.Connect("timeout", new Callable(this, MethodName.OnSpawnTimerTimeout))
    │   └── eventBus.Connect(SignalName.EnemyDefeated, new Callable(this, MethodName.OnEnemyDefeated))
    │
    ├── Hud._Ready()
    │   ├── gameState.Connect(SignalName.StatsChanged, new Callable(this, MethodName.OnStatsChanged))
    │   └── OnStatsChanged() → initial HUD text
    │
    └── DeathScreen._Ready()
        └── restartButton.Connect("pressed", new Callable(this, MethodName.OnRestartButtonPressed))

SpawnTimer starts automatically (Autostart=true)
└── First timeout fires after 2.8s
```

---

### Signal Lifetime and Cleanup

**Autoload signals (GameState, EventBus):**
- Signals persist for the lifetime of the application
- Connections from scene nodes are cleaned up when those nodes are freed (e.g., on `ReloadCurrentScene()`)
- New connections are established when the scene is re-instanced (in `_Ready()`)
- Autoload signals are never disconnected manually

**Node signals (Timer.Timeout, Area2D.BodyEntered, Button.Pressed):**
- These signals exist on nodes within the scene tree
- When the owning node is freed, the signal and all its connections are destroyed
- On scene reload, new nodes create new signals, and new `_Ready()` calls establish new connections

**Memory safety:** Godot's signal system is reference-counted and safe. If a listener node is freed while a signal is emitting, the freed connection is skipped (no crash, no dangling pointer). This matters for `EventBus.EnemyDefeated` -- if the dungeon scene is somehow being freed while an enemy emits the signal, the handler simply doesn't run.

---

### Signal Debugging

Godot provides tools for inspecting signal connections at runtime:

1. **Remote Scene Tree (F5 debugger):** Shows all instanced nodes and their signal connections in the "Node" dock's "Signals" tab
2. **`GD.Print()` in handlers:** Adding `GD.Print("StatsChanged")` to `OnStatsChanged()` confirms the signal is firing
3. **`IsConnected()` check:** `gameState.IsConnected(GameState.SignalName.StatsChanged, new Callable(this, MethodName.OnStatsChanged))` returns `true` if the connection exists

**Common debugging pattern:**
```csharp
public override void _Ready()
{
    var gameState = GetNode<GameState>("/root/GameState");
    gameState.Connect(GameState.SignalName.StatsChanged, new Callable(this, MethodName.OnStatsChanged));
    // Verify connection was established
    System.Diagnostics.Debug.Assert(
        gameState.IsConnected(GameState.SignalName.StatsChanged, new Callable(this, MethodName.OnStatsChanged)),
        "HUD failed to connect to StatsChanged"
    );
}
```

## Implementation Notes

- All signal connections use the `Connect(signalName, callable)` syntax (Godot 4 C# style). Prefer `Connect()` over `+=` for automatic cleanup on node free (see Connection Methods above).
- No signals use `ConnectFlags.Deferred`. All handlers run synchronously within the emitting frame. If deferred execution is ever needed (e.g., freeing nodes inside a signal handler), `CallDeferred()` can be used inside the handler.
- The `EventBus.EnemySpawned` and `EventBus.PlayerAttacked` signals currently have no listeners. They are declared proactively for future systems. This is intentional: defining the signal interface early establishes a contract that future systems can rely on.
- Signal emission order within a single frame is deterministic: handlers are called in the order they were connected. Since each signal has at most one listener currently, ordering is not a concern.

## Open Questions

- Should there be a `PlayerLeveledUpEventHandler(int newLevel)` signal on GameState or EventBus for a dedicated level-up VFX system?
- Should `StatsChanged` pass the changed property name as a parameter for selective updates, or is "update everything" acceptable?
- Should `EnemyDefeated` include the XP amount awarded, so a damage number / XP popup system can use it directly?
- Should a `GameRestartedEventHandler` signal exist on GameState (emitted by `Reset()`) for systems that need to clean up on restart?
- Should the HitArea `BodyEntered` / `BodyExited` signals be used to maintain an overlap list, or is polling `GetOverlappingBodies()` on cooldown timeout sufficient?
