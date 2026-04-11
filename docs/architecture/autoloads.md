# Autoload Singletons

## Summary

Two autoload singletons provide global state and cross-system communication: `GameState` holds all player stats and emits signals when they change, and `EventBus` provides decoupled signals for combat and spawning events. Together, they replace the Phaser prototype's plain `const state = {}` object and ad-hoc function calls.

## Current State

> **Implemented.** Both autoloads are functional and registered in `project.godot`. Last verified against code as of Session 10+.

Both autoloads are registered in `project.godot` and available from any script via the static `Instance` property. `GameState` manages HP, XP, level, floor number, death state, selected class, and player inventory. `EventBus` carries event signals that don't belong to any single node (enemy defeated, enemy spawned, player attacked).

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
| `MaxHp` | `int` | `100` | Custom setter | Maximum hit points. Increases on level up via `Constants.PlayerStats.GetMaxHp(Level)` = `100 + Level * 8`. Setter emits `StatsChanged`. |
| `Xp` | `int` | `0` | Custom setter | Current experience points toward next level. Setter emits `StatsChanged`. |
| `Level` | `int` | `1` | Custom setter | Current character level. Setter emits `StatsChanged`. |
| `FloorNumber` | `int` | `1` | Custom setter | Current dungeon floor. Setter emits `StatsChanged`. |
| `IsDead` | `bool` | `false` | Auto-property | Death flag. Prevents `PlayerDied` from being emitted multiple times. Internal guard. |
| `SelectedClass` | `PlayerClass` | `Warrior` | Auto-property | The class chosen at the class selection screen. Set by `ClassSelect.OnConfirmPressed()`. Read by `Player._Ready()` to load class-specific attack configs and sprites. |
| `PlayerInventory` | `Inventory` | `new(25)` | Private set | Player backpack. 25-slot inventory with gold tracker. Used by ShopWindow for buy/sell. Reset to a fresh instance with 100 starting gold on `Reset()`. |

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

Resets all state to initial values. Called by `DeathScreen.OnRestartPressed()` before loading town.

```csharp
public void Reset()
{
    IsDead = false;
    MaxHp = Constants.PlayerStats.StartingHp; // 100
    Hp = Constants.PlayerStats.StartingHp;    // 100
    Xp = 0;
    Level = 1;
    FloorNumber = 1;
    PlayerInventory = new Inventory(25);
    PlayerInventory.Gold = 100; // Starting gold
}
```

**Note on ordering:** `MaxHp` must be set before `Hp` because `Hp`'s setter clamps to `MaxHp`. Setting `IsDead = false` first (without a setter) ensures `PlayerDied` is not re-emitted.

**Note on inventory:** A fresh `Inventory(25)` is created with 100 starting gold. The old inventory object is discarded (no persistence across deaths in current implementation).

**Note on Constants:** Uses `Constants.PlayerStats.StartingHp` (value 100) instead of hardcoded values.

##### `AwardXp(int amount)`

Awards experience points and handles level-up if the threshold is met.

```csharp
public void AwardXp(int amount)
{
    Xp += amount;
    int xpToLevel = Constants.Leveling.GetXpToLevel(Level);
    while (Xp >= xpToLevel)
    {
        Xp -= xpToLevel;
        Level += 1;
        MaxHp = Constants.PlayerStats.GetMaxHp(Level);
        Hp = Math.Min(MaxHp, Hp + Constants.PlayerStats.HealOnLevelUp);
        xpToLevel = Constants.Leveling.GetXpToLevel(Level);
    }
}
```

**XP threshold formula:** `Constants.Leveling.GetXpToLevel(level)` = `level * 90`. At level 1, need 90 XP. At level 2, need 180 XP. Linear scaling.

**Level-up effects (all happen in sequence per iteration):**
1. Subtract the threshold from current XP (leftover carries over)
2. Increment level by 1
3. Recalculate MaxHp via `Constants.PlayerStats.GetMaxHp(level)` = `100 + level * 8` (level 2: 116, level 10: 180)
4. Heal via `Constants.PlayerStats.HealOnLevelUp` (18 HP) -- never exceeds MaxHp

**Multi-level-ups:** Uses a `while` loop. Each iteration awards full benefits and recalculates the threshold.

**All formulas are in `Constants`:** No magic numbers in GameState. `Constants.PlayerStats` holds `StartingHp`, `HpPerLevel`, `HealOnLevelUp`. `Constants.Leveling` holds `XpPerLevelMultiplier`.

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

#### Full C# (actual implementation)

See `scripts/autoloads/GameState.cs` for the complete source. Key additions beyond the pseudocode:

- **Namespace:** `DungeonGame.Autoloads`
- **Static Instance:** `public static GameState Instance { get; private set; }` set in `_Ready()`
- **`SelectedClass`:** `public PlayerClass SelectedClass { get; set; } = PlayerClass.Warrior;`
- **`PlayerInventory`:** `public Inventory PlayerInventory { get; private set; } = new(25);`
- **`Reset()`** uses `Constants.PlayerStats.StartingHp` and creates a new `Inventory(25)` with `Gold = 100`
- **`AwardXp()`** uses `Constants.Leveling.GetXpToLevel()`, `Constants.PlayerStats.GetMaxHp()`, and `Constants.PlayerStats.HealOnLevelUp`
- All other properties (Hp/MaxHp/Xp/Level/FloorNumber/IsDead/TakeDamage) match the pseudocode exactly

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

#### Full C# (actual implementation)

See `scripts/autoloads/EventBus.cs` for the complete source. Key details:

- **Namespace:** `DungeonGame.Autoloads`
- **Static Instance:** `public static EventBus Instance { get; private set; }` set in `_Ready()`
- **Signals:** Same 4 signals as pseudocode. Note: `EnemyDefeated` second param is `int tier` in the declaration but `Dungeon.SpawnEnemy()` passes `Level` (enemy level, not a 1-3 tier).
- **_Ready():** Only sets `Instance = this`. No other logic.

EventBus is near-minimal: signal declarations + Instance pattern + `_Ready()`. No methods, no state, no `_Process()`.

**Why such a minimal script:** EventBus is a pure signal relay point. It doesn't process or transform events. Adding logic to EventBus would defeat its purpose. Each system retains ownership of its own logic.

---

### How Autoloads Are Accessed

Both autoloads use a **static `Instance` property** set in `_Ready()`. This is the project convention -- no `GetNode<T>("/root/...")` calls needed:

```csharp
// Reading state
int currentHp = GameState.Instance.Hp;
int playerLevel = GameState.Instance.Level;
PlayerClass cls = GameState.Instance.SelectedClass;

// Modifying state (triggers setters and signals)
GameState.Instance.AwardXp(14);
GameState.Instance.TakeDamage(10);

// Connecting to signals — uses Connect(), NOT C# += syntax
GameState.Instance.Connect(
    GameState.SignalName.StatsChanged,
    new Callable(this, MethodName.OnStatsChanged));

GameState.Instance.Connect(
    GameState.SignalName.PlayerDied,
    new Callable(this, MethodName.OnPlayerDied));

// Emitting events
EventBus.Instance.EmitSignal(EventBus.SignalName.EnemyDefeated, GlobalPosition, Level);
EventBus.Instance.EmitSignal(EventBus.SignalName.PlayerAttacked, nearestEnemy);

// Connecting to event signals
EventBus.Instance.Connect(
    EventBus.SignalName.EnemyDefeated,
    new Callable(this, MethodName.OnEnemyDefeated));
```

**Signal connection convention:** The project uses `Connect()` with `new Callable(this, MethodName.X)` throughout, NOT the C# `+=` event syntax. This is consistent across all scripts.

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
