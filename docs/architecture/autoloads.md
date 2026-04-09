# Autoload Singletons

## Summary

Two autoload singletons provide global state and cross-system communication: `GameState` holds all player stats and emits signals when they change, and `EventBus` provides decoupled signals for combat and spawning events. Together, they replace the Phaser prototype's plain `const state = {}` object and ad-hoc function calls.

## Current State

> **Entity Framework:** The Godot `GameState` autoload will wrap and delegate to the entity framework's `EntityData` and systems rather than implementing HP/XP/leveling logic directly. The framework is pure C# with no Godot dependencies -- `GameState` serves as the bridge layer between the framework and the Godot scene tree (emitting signals, persisting across scene reloads). Core logic like damage calculation, XP awards, and stat derivation now lives in `VitalSystem`, `ProgressionSystem`, and `StatSystem`. See [entity-framework.md](../architecture/entity-framework.md) for the full spec.

Both autoloads are registered in `project.godot` and available from any script by name. `GameState` manages HP, XP, level, floor number, and death state. `EventBus` carries event signals that don't belong to any single node (enemy defeated, enemy spawned, player attacked).

## Design

### Why Autoloads

In Godot, autoloads are scripts (or scenes) that are automatically instanced when the game starts and persist across scene changes. They are accessible from any script via their registered name (e.g., `GameState.Hp`).

**Problem they solve:** In the Phaser prototype, the `state` object was a closure variable inside the IIFE. Any function in the same scope could read and write it. In Godot, scripts are attached to individual nodes in separate scene files -- there is no shared scope. Without autoloads, every node that needs to read HP would need a reference to the node that owns HP, creating tight coupling and fragile reference chains.

**Why two autoloads instead of one:** Separation of concerns. `GameState` is about data (what are the current stats?). `EventBus` is about events (what just happened?). Some signals (like `StatsChanged`) naturally belong to the state owner. Others (like `EnemyDefeated`) are about gameplay events that multiple unrelated systems might care about. Keeping them separate means a node can connect to `EventBus` without pulling in state dependencies, and vice versa.

---

### GameState (scripts/autoloads/GameState.cs)

#### Registration

In `project.godot`:
```ini
[autoload]
GameState="*res://scripts/autoloads/GameState.cs"
```

The `*` prefix means "load as a singleton" (this is Godot's convention for autoload entries). The script is attached to an automatically-created Node that persists for the lifetime of the application.

#### Purpose

Centralized, reactive game state. Replaces the Phaser prototype's `const state = { hp: 100, xp: 0, level: 1 }` with a C# singleton that emits signals on every change. Any node in the game can read `GameState.Hp` or connect to `GameState.StatsChanged` without needing a direct reference to another node.

#### Signals

| Signal | Parameters | Emitted When | Listeners |
|--------|------------|--------------|-----------|
| `StatsChanged` | (none) | Any stat property changes (Hp, MaxHp, Xp, Level, FloorNumber) | HUD -- updates the stats label text |
| `PlayerDied` | (none) | HP reaches 0 for the first time (not emitted on subsequent damage while dead) | Main -- shows death screen, pauses tree |

**Why no parameters on StatsChanged:** The HUD reads all stats every time it updates (it formats a single string with all values). Passing individual changed values would add complexity without benefit -- the HUD doesn't conditionally update.

**Why PlayerDied is separate from StatsChanged:** Death is a one-time catastrophic event that triggers specific behavior (pause game, show death screen). It should not be conflated with routine stat updates. Separate signals allow the Main scene to connect only to `PlayerDied` without processing every HP change.

#### Properties

| Property | Type | Default | Setter | Description |
|----------|------|---------|--------|-------------|
| `Hp` | `int` | `100` | Custom setter | Current hit points. Clamped to `[0, MaxHp]`. Setter emits `StatsChanged`. If HP reaches 0 and `IsDead` is false, sets `IsDead = true` and emits `PlayerDied`. |
| `MaxHp` | `int` | `100` | Custom setter | Maximum hit points. Increases on level up via formula `100 + Level * 8`. Setter emits `StatsChanged`. |
| `Xp` | `int` | `0` | Custom setter | Current experience points toward next level. Setter emits `StatsChanged`. |
| `Level` | `int` | `1` | Custom setter | Current character level. Setter emits `StatsChanged`. |
| `FloorNumber` | `int` | `1` | Custom setter | Current dungeon floor. Setter emits `StatsChanged`. |
| `IsDead` | `bool` | `false` | Auto-property | Death flag. Prevents `PlayerDied` from being emitted multiple times if damage continues after death (e.g., enemies still overlapping during the frame death occurs). Not emitted via signal -- it's an internal guard. |

**Why custom setters:** C# supports property accessors (`get`/`set`) to run code whenever a value is assigned. This is the reactive pattern: assigning `GameState.Hp = 80` automatically emits `StatsChanged` without the caller needing to remember to emit it. This prevents bugs where a stat changes but the HUD doesn't update.

**Setter behavior detail -- Hp:**
```csharp
private int _hp = 100;
public int Hp
{
    get => _hp;
    set
    {
        _hp = Math.Clamp(value, 0, MaxHp);
        EmitSignal(SignalName.StatsChanged);
        if (_hp <= 0 && !IsDead)
        {
            IsDead = true;
            EmitSignal(SignalName.PlayerDied);
        }
    }
}
```
- `Math.Clamp(value, 0, MaxHp)` ensures HP never goes negative or exceeds MaxHp
- `StatsChanged` is emitted on every change, even if the clamped value equals the old value (simplicity over optimization)
- The `IsDead` guard ensures `PlayerDied` fires exactly once per death

**Setter behavior detail -- MaxHp, Xp, Level, FloorNumber:**
```csharp
private int _maxHp = 100;
public int MaxHp
{
    get => _maxHp;
    set
    {
        _maxHp = value;
        EmitSignal(SignalName.StatsChanged);
    }
}

private int _xp = 0;
public int Xp
{
    get => _xp;
    set
    {
        _xp = value;
        EmitSignal(SignalName.StatsChanged);
    }
}

private int _level = 1;
public int Level
{
    get => _level;
    set
    {
        _level = value;
        EmitSignal(SignalName.StatsChanged);
    }
}

private int _floorNumber = 1;
public int FloorNumber
{
    get => _floorNumber;
    set
    {
        _floorNumber = value;
        EmitSignal(SignalName.StatsChanged);
    }
}
```

#### Methods

##### `Reset()`

Resets all state to initial values. Called by the death screen before reloading the scene.

```csharp
public void Reset()
{
    IsDead = false;
    // Set MaxHp before Hp so the clamp in Hp's setter uses the correct max
    MaxHp = 100;
    Hp = 100;
    Xp = 0;
    Level = 1;
    FloorNumber = 1;
}
```

**Note on ordering:** `MaxHp` must be set before `Hp` because `Hp`'s setter clamps to `MaxHp`. If `Hp` were set first while `MaxHp` was still a higher value from a previous run, `Hp` would be clamped incorrectly. Setting `IsDead = false` first (without a setter) ensures the `PlayerDied` signal is not re-emitted if `Hp` passes through 0 during reset.

**Note on signal emission:** Each property assignment triggers `EmitSignal(SignalName.StatsChanged)`. This means `Reset()` emits `StatsChanged` five times (once per property). This is acceptable because `Reset()` is called immediately before `ReloadCurrentScene()`, which destroys all listeners anyway. If performance ever matters, a batch pattern could be introduced (suppress signals during reset, emit once at the end).

##### `AwardXp(int amount)`

Awards experience points and handles level-up if the threshold is met.

```csharp
public void AwardXp(int amount)
{
    Xp += amount; // Triggers Xp setter -> StatsChanged

    int xpToLevel = Level * 90;
    while (Xp >= xpToLevel)
    {
        Xp -= xpToLevel;          // Triggers Xp setter -> StatsChanged
        Level += 1;               // Triggers Level setter -> StatsChanged
        MaxHp = 100 + Level * 8;  // Triggers MaxHp setter -> StatsChanged
        Hp = Math.Min(MaxHp, Hp + 18); // Triggers Hp setter -> StatsChanged
        xpToLevel = Level * 90;   // Recalculate for next iteration
    }
}
```

**XP threshold formula:** `Level * 90`. At level 1, need 90 XP to reach level 2. At level 2, need 180 XP to reach level 3. This is a linear scaling curve that matches the Phaser prototype's `state.level * 90`.

**Level-up effects (all happen in sequence per iteration):**
1. Subtract the threshold from current XP (leftover XP carries over)
2. Increment level by 1
3. Recalculate MaxHp: `100 + Level * 8` (at level 2: 116, level 3: 124, level 10: 180)
4. Heal: `Math.Min(MaxHp, Hp + 18)` -- heals 18 HP but never exceeds MaxHp

**Multi-level-ups:** Uses a `while` loop to handle cases where a single XP award crosses multiple level thresholds (e.g., boss kills, rested XP bonuses). Each iteration awards full level-up benefits (max HP increase, heal) and recalculates the threshold for the new level.

##### `TakeDamage(int amount)`

Reduces HP by the given amount. Death detection is handled by the `Hp` setter.

```csharp
public void TakeDamage(int amount)
{
    if (IsDead)
        return;
    Hp -= amount; // Triggers Hp setter -> clamps to 0, emits StatsChanged, may emit PlayerDied
}
```

**Why the IsDead guard here AND in the setter:** Belt and suspenders. The setter guard prevents `PlayerDied` from double-emitting. The `TakeDamage` guard prevents any HP modification after death. Both are cheap checks that prevent subtle bugs.

**Why not `Hp = Math.Max(0, Hp - amount)`:** The `Hp` setter already clamps to `[0, MaxHp]`, so explicit clamping in `TakeDamage` would be redundant. The subtraction can safely produce a negative intermediate value because the setter handles it.

#### Full C# Pseudocode

```csharp
using Godot;
using System;

public partial class GameState : Node
{
    // --- Signals ---
    [Signal] public delegate void StatsChangedEventHandler();
    [Signal] public delegate void PlayerDiedEventHandler();

    // --- Properties with setters ---
    private int _hp = 100;
    public int Hp
    {
        get => _hp;
        set
        {
            _hp = Math.Clamp(value, 0, MaxHp);
            EmitSignal(SignalName.StatsChanged);
            if (_hp <= 0 && !IsDead)
            {
                IsDead = true;
                EmitSignal(SignalName.PlayerDied);
            }
        }
    }

    private int _maxHp = 100;
    public int MaxHp
    {
        get => _maxHp;
        set
        {
            _maxHp = value;
            EmitSignal(SignalName.StatsChanged);
        }
    }

    private int _xp = 0;
    public int Xp
    {
        get => _xp;
        set
        {
            _xp = value;
            EmitSignal(SignalName.StatsChanged);
        }
    }

    private int _level = 1;
    public int Level
    {
        get => _level;
        set
        {
            _level = value;
            EmitSignal(SignalName.StatsChanged);
        }
    }

    private int _floorNumber = 1;
    public int FloorNumber
    {
        get => _floorNumber;
        set
        {
            _floorNumber = value;
            EmitSignal(SignalName.StatsChanged);
        }
    }

    public bool IsDead { get; set; } = false;

    // --- Methods ---
    public void Reset()
    {
        IsDead = false;
        MaxHp = 100;
        Hp = 100;
        Xp = 0;
        Level = 1;
        FloorNumber = 1;
    }

    public void AwardXp(int amount)
    {
        Xp += amount;
        int xpToLevel = Level * 90;
        while (Xp >= xpToLevel)
        {
            Xp -= xpToLevel;
            Level += 1;
            MaxHp = 100 + Level * 8;
            Hp = Math.Min(MaxHp, Hp + 18);
            xpToLevel = Level * 90;
        }
    }

    public void TakeDamage(int amount)
    {
        if (IsDead)
            return;
        Hp -= amount;
    }
}
```

---

### EventBus (scripts/autoloads/EventBus.cs)

#### Registration

In `project.godot`:
```ini
[autoload]
GameState="*res://scripts/autoloads/GameState.cs"
EventBus="*res://scripts/autoloads/EventBus.cs"
```

**Load order:** GameState is listed first and loaded before EventBus. This doesn't currently matter (they don't reference each other at load time), but it establishes a convention: state before events.

#### Purpose

Decoupled signal hub for gameplay events that don't belong to any single node. While `GameState` signals are about stat changes (reactive data), `EventBus` signals are about gameplay events (things that happened in the world).

**Why separate from GameState:** Consider `EnemyDefeated`. This event is emitted by the Enemy script, but it's consumed by the Dungeon script (to schedule a respawn) and potentially by a future sound system (to play a death sound) and a future particle system (to spawn death particles). None of these consumers care about GameState. If `EnemyDefeated` were on GameState, those systems would need to import/reference GameState for no reason. EventBus keeps event routing orthogonal to state management.

**Why not direct function calls:** When an enemy dies, it could directly call `dungeon.RespawnEnemy()`. But then the enemy needs a reference to the dungeon. And if a sound system also needs to know about deaths, the enemy needs a reference to that too. Every new consumer adds a dependency to the enemy script. With EventBus, the enemy emits a signal and doesn't know or care who's listening. New consumers connect to EventBus without modifying the enemy at all.

#### Signals

| Signal | Parameters | Emitted By | Current Listeners | Future Listeners |
|--------|------------|-----------|-------------------|------------------|
| `EnemyDefeated` | `Vector2 position, int tier` | `Enemy.TakeDamage()` when HP <= 0 | Dungeon -- schedules respawn after 1.4s | Sound system (death SFX), particle system (death VFX), achievement tracker |
| `EnemySpawned` | `Node enemy` | `Dungeon.SpawnEnemy()` after adding enemy to tree | (none currently) | Sound system (spawn SFX), minimap (enemy blip) |
| `PlayerAttacked` | `Node target` | `Player.HandleAttack()` on successful hit | (none currently) | Sound system (attack SFX), combo tracker, statistics |
| `PlayerDamaged` | `int amount, Node source` | `GameState.TakeDamage()` or enemy damage logic | (none currently) | Damage number display, damage log, analytics |

**Signal parameter design choices:**

`EnemyDefeated(Vector2 position, int tier)`:
- `position` is passed explicitly (not as the enemy node) because the enemy calls `QueueFree()` immediately after emitting. By the time listeners process the signal, the enemy node may already be freed. Passing the position as a raw Vector2 ensures it remains valid.
- `tier` is passed so listeners can vary their response by enemy difficulty (e.g., different death sounds for different tiers, more particles for higher tiers).

`EnemySpawned(Node enemy)`:
- The full enemy node is passed because the enemy continues to exist after spawning. Listeners may need to reference the node (e.g., a minimap adding a tracking dot).

`PlayerAttacked(Node target)`:
- The target enemy node is passed so listeners can determine which enemy was hit (e.g., for hit confirmation sounds that vary by enemy type).
- The target node is guaranteed to still exist at signal emission time (it may die from the damage, but `QueueFree()` happens after the signal propagates within the same frame).

`PlayerDamaged(int amount, Node source)`:
- `amount` is the raw damage dealt (before any future mitigation), useful for floating damage numbers.
- `source` is the enemy or hazard that caused the damage, so listeners can vary effects by source type (e.g., different hit sounds for different enemy tiers).

#### Full C# Pseudocode

```csharp
using Godot;

public partial class EventBus : Node
{
    // --- Signals ---

    /// <summary>
    /// Emitted when an enemy's HP reaches 0 and it is about to be freed.
    /// </summary>
    /// <param name="position">The enemy's GlobalPosition at the moment of death.</param>
    /// <param name="tier">The enemy's DangerTier (1, 2, or 3).</param>
    [Signal] public delegate void EnemyDefeatedEventHandler(Vector2 position, int tier);

    /// <summary>
    /// Emitted after a new enemy instance is added to the scene tree.
    /// </summary>
    /// <param name="enemy">The enemy Node (CharacterBody2D) that was just spawned.</param>
    [Signal] public delegate void EnemySpawnedEventHandler(Node enemy);

    /// <summary>
    /// Emitted when the player successfully attacks an enemy (after cooldown, within range).
    /// </summary>
    /// <param name="target">The enemy Node that was attacked.</param>
    [Signal] public delegate void PlayerAttackedEventHandler(Node target);

    /// <summary>
    /// Emitted when the player takes damage from any source.
    /// </summary>
    /// <param name="amount">The raw damage amount dealt.</param>
    /// <param name="source">The Node that caused the damage.</param>
    [Signal] public delegate void PlayerDamagedEventHandler(int amount, Node source);
}
```

That's it -- the EventBus script contains only signal declarations. No methods, no state, no `_Ready()`, no `_Process()`. It is the thinnest possible script: just a place for signals to live.

**Why such a minimal script:** EventBus is a pure signal relay point. It doesn't process or transform events. It doesn't store state. Adding logic to EventBus would defeat its purpose -- it would become a "god object" that knows about everything. By keeping it to pure signal declarations, each system retains ownership of its own logic.

---

### How Autoloads Are Accessed

From any script in the game, autoloads are accessed via `GetNode<T>()` or a cached singleton pattern:

```csharp
// Getting a reference to the autoload singleton
var gameState = GetNode<GameState>("/root/GameState");
var eventBus = GetNode<EventBus>("/root/EventBus");

// Reading state
int currentHp = gameState.Hp;
int playerLevel = gameState.Level;

// Modifying state (triggers setters and signals)
gameState.Hp -= 10;
gameState.AwardXp(14);

// Connecting to state signals
gameState.Connect(GameState.SignalName.StatsChanged, new Callable(this, MethodName.OnStatsChanged));
gameState.Connect(GameState.SignalName.PlayerDied, new Callable(this, MethodName.OnPlayerDied));

// Emitting events
eventBus.EmitSignal(EventBus.SignalName.EnemyDefeated, GlobalPosition, dangerTier);
eventBus.EmitSignal(EventBus.SignalName.PlayerAttacked, nearestEnemy);

// Connecting to event signals
eventBus.Connect(EventBus.SignalName.EnemyDefeated, new Callable(this, MethodName.OnEnemyDefeated));
```

**Accessing autoloads in C#:** Unlike GDScript, C# does not have implicit global access to autoloads by name. Use `GetNode<T>("/root/AutoloadName")` to get a typed reference. A common pattern is to cache the reference in `_Ready()` or use a static `Instance` property on the autoload class.

**Lifecycle:** Autoloads are created before the main scene loads and destroyed after the main scene is freed. They survive `ReloadCurrentScene()` -- their state persists across scene reloads. This is why `GameState.Reset()` must be called explicitly before reloading: the old HP/XP/level values would otherwise carry over.

---

### project.godot Autoload Section

The complete autoload configuration block:

```ini
[autoload]

GameState="*res://scripts/autoloads/GameState.cs"
EventBus="*res://scripts/autoloads/EventBus.cs"
```

**Key details:**
- The `*` prefix is required for singleton autoloads (scripts that are instanced automatically)
- Paths are relative to `res://` (the project root)
- Order matters for load sequence: GameState loads first, EventBus second
- Both classes inherit from Node (the simplest base class -- no spatial, no rendering, no physics)

## Implementation Notes

- Autoloads survive `GetTree().ReloadCurrentScene()`. Always call `GameState.Reset()` before reloading to clear stale state.
- Signal connections from scene nodes to autoloads are one-directional: scene nodes connect to autoload signals in their `_Ready()`. When the scene is freed (on reload), those connections are automatically cleaned up. Autoloads never need to disconnect manually.
- The `StatsChanged` signal fires frequently (every stat change, including during `Reset()` and `AwardXp()`). This is acceptable for the current single-listener (HUD) but should be reviewed if more listeners are added. A "batch update" pattern (suppress signals, emit once) could be introduced if profiling shows a bottleneck.
- `EventBus` signals use typed parameters in their delegate declarations. This provides autocompletion in the IDE and catches type mismatches at compile time.

## Resolved Questions

1. **Save slots:** Design for multi-slot, build single-slot. The GameState interface is slot-aware (Reset clears to defaults, state is serializable), but only one slot is implemented for MVP. Multi-slot support can be added later without changing GameState's public API.
2. **Multi-level-up:** `AwardXp()` uses a `while` loop so that a single large XP award (e.g., boss kill, rested XP bonus) correctly processes multiple level-ups in sequence, each awarding full benefits (MaxHp increase, heal).
3. **player_damaged signal:** Added `PlayerDamagedEventHandler(int amount, Node source)` to EventBus. Supports future damage number display, damage logs, and analytics without coupling the damage source to the display system.
4. **Script vs scene:** Autoloads use plain scripts (no `.tscn`). Scripts are simpler and sufficient -- autoloads don't need child nodes. If a Timer or other child is ever needed, the autoload can add children programmatically in `_Ready()`.
5. **FloorNumber location:** Stays on GameState. Separating into a `DungeonState` autoload would add complexity without benefit at this scale. If world state grows significantly (multiple dungeon types, biome tracking, etc.), it can be split later.
