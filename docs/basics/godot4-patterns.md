# Godot 4 C# Patterns

## Why This Matters
Godot + C# has specific patterns and pitfalls that don't exist in pure C# or pure GDScript. Understanding the lifecycle, resource system, and common gotchas prevents mysterious crashes and silent failures.

## Core Concepts

### Node Lifecycle
Nodes go through a specific sequence:

```
_EnterTree()     ← Added to scene tree (before ready)
_Ready()         ← All children are ready. Initialize here.
_Process(delta)  ← Every render frame (visual updates)
_PhysicsProcess(delta) ← Fixed timestep (movement, physics)
_ExitTree()      ← Removed from scene tree (cleanup)
```

**Rules:**
- Initialize in `_Ready()`, not constructor. Child nodes don't exist in constructor.
- Movement in `_PhysicsProcess()`. Visual updates in `_Process()`.
- Cleanup connections in `_ExitTree()` if needed.
- `_Ready()` is called ONCE, when first added to tree. If removed and re-added, only `_EnterTree()` fires again.

### The `partial` Requirement
Every C# class that's a Godot node MUST be `partial`:

```csharp
public partial class Player : CharacterBody2D { }  // ✓
public class Player : CharacterBody2D { }           // ✗ Won't generate signal code
```

Godot's source generator creates a companion partial class for signal wiring, exported properties, etc.

### Signals in C#
```csharp
// Define
[Signal] public delegate void HealthChangedEventHandler(int newHealth);

// Emit
EmitSignal(SignalName.HealthChanged, currentHP);

// Connect (option 1: C# event syntax)
player.HealthChanged += OnPlayerHealthChanged;

// Connect (option 2: Godot Connect — auto-disconnects on node free)
player.Connect(SignalName.HealthChanged, new Callable(this, MethodName.OnPlayerHealthChanged));
```

**Prefer Godot's Connect()** over C# += for node signals — it automatically disconnects when the listening node is freed, preventing "calling method on disposed object" errors.

### Resource Loading
```csharp
// Synchronous (blocks until loaded — fine for small resources)
var tex = ResourceLoader.Load<Texture2D>("res://assets/image.png");

// Check existence first (avoids error spam)
if (ResourceLoader.Exists("res://assets/font.ttf"))
    var font = ResourceLoader.Load<Font>("res://assets/font.ttf");

// Preload equivalent (loads at parse time in GDScript; C# has no true preload)
// Use ResourceLoader.Load in _Ready() or class initialization
```

**The Import Cache:** Godot doesn't load PNGs directly — it imports them into `.godot/imported/`. If the cache is stale, resources fail to load. Fix: `make import` or open the project in the editor.

### Export Variables
```csharp
[Export] public float Speed { get; set; } = 190f;
[Export] public int MaxHP { get; set; } = 100;
[Export(PropertyHint.Range, "0,100,1")] public int Armor { get; set; }
[ExportGroup("Combat")]
[Export] public float AttackSpeed { get; set; } = 0.42f;
```

Exports appear in the Inspector. Use them for values you'll tweak during development.

### The `null!` Pattern
Fields initialized in `_Ready()` should use `null!` to suppress nullable warnings:

```csharp
private Sprite2D _sprite = null!;  // Will be set in _Ready
private Label _label = null!;

public override void _Ready()
{
    _sprite = GetNode<Sprite2D>("Sprite");
    _label = GetNode<Label>("Label");
}
```

### Autoload / Singleton Pattern
```csharp
public partial class SceneManager : Node
{
    public static SceneManager Instance { get; private set; }
    
    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;  // Works while paused
    }
}
```

Register in `project.godot`:
```
[autoload]
SceneManager="*res://scripts/autoloads/SceneManager.cs"
```

The `*` prefix means "create instance automatically."

### Scene Instancing
```csharp
// Load a scene
var enemyScene = ResourceLoader.Load<PackedScene>("res://scenes/enemy.tscn");

// Instance it
var enemy = enemyScene.Instantiate<Node2D>();
enemy.Position = spawnPosition;
AddChild(enemy);

// Or build programmatically (our current approach — no .tscn file needed)
var enemy = new EnemyEntity();
enemy.Init(monsterData);
AddChild(enemy);
```

### Tree Pausing
```csharp
GetTree().Paused = true;   // Pauses all nodes
GetTree().Paused = false;  // Unpauses

// Nodes that should work while paused:
ProcessMode = ProcessModeEnum.Always;  // PauseMenu, SceneManager

// Nodes that should always pause:
ProcessMode = ProcessModeEnum.Pausable;  // Default for most nodes
```

### QueueFree vs Free
- `QueueFree()` — removes at end of frame (SAFE during _Process)
- `Free()` — removes immediately (UNSAFE during iteration, signals)
- **Always use QueueFree()** unless you have a specific reason for immediate removal.

### GodotObject Disposal
C# garbage collector doesn't know about Godot objects. If you create a Resource or Node and don't add it to the tree, it leaks:

```csharp
// Leak — Image is never freed
var img = new Image();  // Godot object, not tracked by C# GC

// Fix — explicitly dispose
img.Dispose();  // Or img.Free()

// Or — add to tree (tree manages lifecycle)
AddChild(node);  // Freed when parent is freed
```

## Common Mistakes
1. **Missing `partial` keyword** — signals and exports silently fail
2. **Initializing in constructor** — child nodes don't exist yet; use `_Ready()`
3. **Movement in `_Process`** — frame-rate dependent; use `_PhysicsProcess`
4. **C# += for signals** — doesn't auto-disconnect; use `Connect()`
5. **Calling methods on freed nodes** — check `IsInstanceValid(node)` first
6. **ResourceLoader.Load without Exists check** — error spam in console
7. **Free() during iteration** — crashes; use QueueFree()
8. **Not setting ProcessMode.Always on pause menu** — can't unpause
9. **Forgetting make import** — asset cache stale, textures don't load

## Checklist
- [ ] All node classes are `partial`
- [ ] Initialization in `_Ready()`, not constructor
- [ ] Movement/physics in `_PhysicsProcess()`
- [ ] Visual updates in `_Process()`
- [ ] Signals connected via `Connect()` (not +=) for node-to-node
- [ ] `ResourceLoader.Exists()` before `Load()`
- [ ] `QueueFree()`, never `Free()`, in normal code
- [ ] Pause menu has `ProcessMode = Always`

## Sources
- [Godot C# Basics](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_basics.html)
- [Godot C# Signals](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_signals.html)
- [Godot C# Style Guide](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_style_guide.html)
- [Godot Node Lifecycle](https://docs.godotengine.org/en/stable/tutorials/scripting/overridable_functions.html)
