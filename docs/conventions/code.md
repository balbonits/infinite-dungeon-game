# Code Patterns and Conventions

Quick-reference for every AI session and human reader. Rules, not essays.

---

## 1. Philosophy

- **DRY** -- extract when repetition appears, not before.
- **KISS** -- simplest approach that satisfies the spec. No clever abstractions.
- **Spec-driven** -- every system is documented in `docs/` before code exists. If code and docs disagree, one needs updating.
- **Test-driven** -- tests define "done." Write test cases before implementation.
- **Old-school game dev** -- no frameworks, no middleware, no architecture astronautics. Clean code that does what the spec says and nothing more.
- **AI-readable** -- all code is written by AI, picked up cold by new sessions. Self-documenting names, consistent patterns, zero tribal knowledge.

---

## 2. C# Conventions

### Naming

| Identifier | Convention | Example |
|---|---|---|
| Classes | `PascalCase`, always `partial` for nodes | `public partial class Player : CharacterBody2D` |
| Public properties/methods | `PascalCase` | `MaxHp`, `HandleMovement()` |
| Private fields | `_camelCase` | `_attackTimer`, `_isDead` |
| Constants | `PascalCase` | `AttackCooldown`, `EnemySoftCap` |
| Enums | `PascalCase` name and values | `enum DangerTier { Low, Medium, High }` |
| Signals | `PascalCase` + `EventHandler` suffix | `EnemyDefeatedEventHandler` |
| Namespaces | `PascalCase` by directory | `DungeonGame.Autoloads` |
| Groups | `snake_case` | `"player"`, `"enemies"`, `"damageable"` |
| No abbreviations | Spell it out | `player` not `plr`, `level` not `lvl` |

Exceptions for universally understood abbreviations: `HP`, `XP`, `AI`, `UI`, `HUD`.

### File Organization

- One class per file.
- File name matches class name: `GameState.cs` contains `public partial class GameState`.
- Max ~300 lines per script. If it grows past that, split behavior into child nodes.
- Nullable enabled (`<Nullable>enable</Nullable>` in `.csproj`). Use `null!` for fields initialized in `_Ready()`.

### Script Ordering (top to bottom)

1. `using` directives
2. `namespace` declaration
3. `[Signal]` delegates
4. Enums
5. Constants
6. `[Export]` properties
7. Public properties
8. Private fields (`_camelCase`)
9. Lifecycle overrides (`_Ready`, `_Process`, `_PhysicsProcess`)
10. Public methods
11. Private methods

### Documentation

- XML doc comments only where behavior is non-obvious.
- No `#region` blocks.
- No inline `// TODO` or `// HACK` comments. If something needs doing, it goes in the tracker.

---

## 3. Godot Patterns

### Call Down, Signal Up

Parents call methods on children. Children emit signals to parents. Never reach up the tree.

```
Parent --calls--> Child.DoThing()
Child  --emits--> SignalName.ThingHappened --> Parent handles it
```

### Node References

Cache in `_Ready()` with `GetNode<T>()`. Never call `GetNode` in `_Process` or `_PhysicsProcess`.

```csharp
private Sprite2D _sprite = null!;
public override void _Ready() { _sprite = GetNode<Sprite2D>("Sprite"); }
```

### Process Methods

| Method | Use for | Tick rate |
|---|---|---|
| `_PhysicsProcess(double delta)` | Movement, collision, game logic | Fixed 60fps |
| `_Process(double delta)` | Visuals, animations, UI updates | Display refresh rate |

### Scene Instancing

Each entity is a self-contained `.tscn` + `.cs` pair. Load scenes with `GD.Load<PackedScene>()`, cache the result, instantiate with `Instantiate<T>()`.

### UI Layer

All UI lives in a `CanvasLayer` (layer 100), separate from the game world. UI uses `Control` nodes (Label, Panel, Button). Game world uses `Node2D` nodes. Never mix them.

### Groups for Tagging

Use groups like CSS classes -- tag nodes for batch queries.

```csharp
// Tag
enemy.AddToGroup("enemies");
// Query
var allEnemies = GetTree().GetNodesInGroup("enemies");
```

### Autoloads

Sparingly -- only for truly global state. Access via static `Instance` property.

| Autoload | Purpose |
|---|---|
| `GameState` | Owns player data (HP, XP, level, floor). Emits signals on change. |
| `EventBus` | Pure signal relay for decoupled cross-system events. Owns no data. |

### Signals

Use `[Signal]` attribute with `EventHandler` suffix. Prefer `Connect()` over `+=` (auto-disconnects on node free).

```csharp
[Signal] public delegate void StatsChangedEventHandler();
EmitSignal(SignalName.StatsChanged);
source.Connect(SignalName.StatsChanged, new Callable(this, MethodName.OnStatsChanged));
```

---

## 4. Code Quality Standards

| Rule | Details |
|---|---|
| Test coverage target | 95% for game logic. xUnit for pure logic, GdUnit4 for scene tests. |
| Tests before code | Test cases define "done." Write them first. |
| No TODO comments | Track work in `docs/dev-tracker.md`, not in source. |
| No placeholder code | If it is not in the spec, it does not exist in the codebase. |
| No suppressed warnings | Fix the warning or fix the code. Never `#pragma warning disable`. |
| No dead code | Delete unused methods, fields, imports. No commented-out blocks. |
| No magic numbers | Use named constants. `const int EnemySoftCap = 12;` not `if (count > 12)`. |
| Static typing everywhere | C# enforces this. Use explicit types for fields; `var` only for locals where the type is obvious. |

---

## 5. Performance Conventions

These apply from the start -- they are habits, not optimizations to add later.

| Pattern | When to use | Example |
|---|---|---|
| Integer math | Stats, damage, XP, levels, floors | `int damage = baseDamage + strength * 2;` |
| Dirty-flag caching | Computed properties that recalculate only on change | Set `_statsDirty = true` on change, recompute on next read |
| Skip-redraw on unchanged | UI labels, progress bars | Only update label text when the value actually changed |
| String pre-caching | Any string used in draw/process loops | Cache `StringName` or formatted strings outside the loop |
| Object pooling | High-frequency spawns (enemies, effects, projectiles) | `Microsoft.Extensions.ObjectPool` -- return to pool on "death" |
| Avoid allocations in hot paths | `_Process`, `_PhysicsProcess`, signal handlers | No `new` in per-frame code. Pre-allocate collections. |
| Background generation | Floor generation, proc-gen pipelines | `System.Threading.Channels` producer/consumer pattern |

---

## 6. Architecture Rules

### Static Game Logic Layer

All formulas, stat calculations, and game rules live in plain C# classes with no Godot dependency. Testable with xUnit, no engine required.

```
scripts/
  logic/        <-- Pure C#, no Godot imports, tested with xUnit
  autoloads/    <-- Godot singletons (GameState, EventBus)
  scenes/       <-- Node scripts, tested with GdUnit4
```

### Reactive State

`GameState` autoload holds canonical data. Changes emit signals. UI and other systems subscribe -- they never poll.

### EventBus for Decoupled Events

Systems that do not own each other communicate through `EventBus`. The combat system emits `EnemyDefeated`; the loot system and XP system both listen. Neither knows about the other.

### Entity = Scene + Script

Every game entity (player, enemy, dungeon floor, UI panel) is a self-contained `.tscn` + `.cs` pair. The scene defines structure; the script defines behavior. They live in matching directory paths under `scenes/` and `scripts/`.

### Composition Over Inheritance

Build entities by combining specialized child nodes/scenes. Deep inheritance chains are not used. If an entity needs new behavior, add a child node with its own script.

### One Responsibility Per Script

A script does one thing. If it handles movement AND combat AND inventory, it is three scripts attached to child nodes.

---

## 7. What We Don't Do

| Avoided | Why |
|---|---|
| ECS (Entity Component System) | Game scope does not justify it. Godot's scene/node model is sufficient. |
| Heavy frameworks (Chickensoft, etc.) in game code | Adds indirection and learning curve with no payoff at this scale. Exception: Chickensoft packages allowed for testing tools only (`scripts/testing/`). |
| GDScript | C# for type safety, IDE tooling, NuGet ecosystem, and AI code generation quality. |
| Multiple scripting languages | C# everywhere. No mixing. |
| Paid assets | Free/open-source only (CC0, CC-BY 3.0, CC-BY 4.0). |
| Web deployment | C# web export not supported in Godot 4.6. Desktop native only. |
| Premature abstraction | No interfaces, generics, or patterns "for the future." Add them when the second use case appears. |
| Speculative features | If it is not in a locked spec doc, it does not get built. |
| C++ / GDExtension | C# with object pooling and async generation is sufficient for this game. |
| 3D rendering | 2D isometric. All depth is faked via tile stacking and draw order. |
| External physics engines | Godot built-in 2D physics handles everything. |

---

## 8. AI Coding Standards

### Every Session Starts the Same Way

1. Read the relevant spec doc(s) in `docs/`.
2. Read the existing code if modifying.
3. Confirm understanding before writing anything.

### Dev Ticket Cycle

```
branch --> research --> plan --> cross-check --> test --> code --> verify --> commit --> push
```

One ticket = one branch = one focused change = one squashed commit. See `docs/conventions/ai-workflow.md` for the full protocol.

### Scope Rules

| Tier | Action |
|---|---|
| Always do | Read specs first, use static typing, follow naming conventions, run tests, keep scripts under 300 lines |
| Ask first | Adding new files/scenes, changing autoloads, modifying project.godot, adding dependencies, any refactoring beyond current task |
| Never do | Add unspecified features, make assumptions about intent, skip tests, change code outside task scope, add TODO/placeholder code, suppress warnings |

### Commit Discipline

- Atomic commits -- one logical change per commit.
- Squash checkpoint commits before pushing.
- Commit message describes what changed and why.
- Evidence (screenshots/recordings) committed with the ticket when visual changes are involved.

### Context Hygiene

- Start fresh for each unrelated task.
- Reference files by path, not by pasting large blocks.
- If a task touches many files, delegate to a subagent to preserve main context.
- After two failed corrections, start over with a better prompt rather than accumulating confusion.

---

## 9. Accessibility

- Keyboard navigation must work for all menus and dialogs.
- Color alone must never be the only indicator — pair with text or shape.
- Death screen navigable via keyboard (R key) and mouse (button).
- Future: screen reader support for menu systems.
