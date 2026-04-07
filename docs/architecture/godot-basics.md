# Godot 4 Basics for Web Developers

## Summary

A comprehensive bridge document mapping web development concepts to Godot 4 equivalents. If you know HTML, CSS, JavaScript, and the DOM, you already have the mental models — they just need to be translated.

## Current State

This document covers every major Godot 4 concept a front-end developer needs to understand before reading or writing C# for Godot. Each section maps a familiar web concept to its Godot equivalent with runnable C# code examples.

## Design

### Scene Tree vs DOM

**What you already know:** The browser has a DOM — a tree of HTML elements. The `<body>` contains `<div>`s, which contain `<p>`s, and so on. You traverse it, add/remove elements, and the browser renders the current state.

**The Godot equivalent:** Godot has a **Scene Tree** — a tree of **Nodes**. The root `Node` contains child nodes, which contain their own children. You traverse it, add/remove nodes, and Godot renders the current state.

```csharp
// Traversing the tree (like document.querySelector / DOM traversal)
var player = GetNode<Node>("Player");          // like document.getElementById("Player")
var player = GetNode<Node>("Player");          // no $ shorthand in C#, use GetNode<T>()
var label = GetNode<Label>("UI/HUD/HPLabel");  // nested path, like querySelector("UI > HUD > HPLabel")

// Adding a node (like document.createElement + appendChild)
var enemy = _enemyScene.Instantiate<CharacterBody2D>();  // like document.createElement("div")
AddChild(enemy);                               // like parent.appendChild(child)

// Removing a node (like element.remove())
enemy.QueueFree();                       // schedules removal at end of frame (safe)
enemy.Free();                            // immediate removal (dangerous, can crash if referenced)

// Getting all nodes in a group (like document.querySelectorAll(".enemies"))
var allEnemies = GetTree().GetNodesInGroup("enemies");
```

**Key differences to watch out for:**
- DOM paths use CSS selectors (`querySelector`). Godot uses **node paths** relative to the current node (`GetNode<T>("Parent/Child/Grandchild")`).
- In the DOM, elements exist on the page and are hidden/shown with CSS. In Godot, nodes can be truly removed from the tree with `QueueFree()` — they are destroyed, not hidden.
- The DOM is one global tree. Godot scenes are **separate trees** that get merged. Your `player.tscn` is its own little tree that gets instanced into the `dungeon.tscn` tree.
- `QueueFree()` is almost always preferred over `Free()`. It waits until the end of the current frame to remove the node, preventing crashes from dangling references mid-frame.

---

### Nodes vs HTML Elements

**What you already know:** HTML has element types — `<div>`, `<p>`, `<canvas>`, `<button>`, `<input>`. Each has specific behavior and properties. A `<button>` is clickable. A `<canvas>` is drawable. A `<div>` is a generic container.

**The Godot equivalent:** Godot has **Node types** — `Node2D`, `CharacterBody2D`, `Sprite2D`, `Area2D`, `Label`, `Button`, `Camera2D`. Each has specific behavior and properties. A `CharacterBody2D` handles physics movement. A `Sprite2D` displays an image. A `Node2D` is a generic 2D container.

```csharp
// Common node types and their web equivalents:

// Node2D — like <div> (generic 2D container, has position/rotation/scale)
// Sprite2D — like <img> (displays a texture)
// Label — like <p> or <span> (displays text)
// Button — like <button> (clickable, emits "Pressed" signal)
// Panel — like <div> with background (styled container)
// Camera2D — like viewport/scroll position (controls what part of the world is visible)
// CharacterBody2D — no web equivalent (physics-aware object that moves and collides)
// Area2D — no web equivalent (invisible detection zone, triggers when something enters)
// TileMapLayer — no web equivalent (renders a grid of tiles efficiently)
// Timer — like setTimeout/setInterval (emits "Timeout" signal)
// AudioStreamPlayer — like <audio> (plays sound)
```

**The inheritance hierarchy** (understanding this is critical):

```
Node                          — Base of everything (like EventTarget)
├── Node2D                    — 2D game objects (has position, rotation, scale)
│   ├── Sprite2D              — Displays a texture
│   ├── Polygon2D             — Draws a colored polygon (our current player/enemy shapes)
│   ├── Camera2D              — Controls the viewport
│   ├── TileMapLayer          — Renders tile grids
│   ├── CollisionBody2D       — (abstract) Physics body base
│   │   ├── CharacterBody2D   — Player/enemy movement with collision
│   │   ├── StaticBody2D      — Immovable walls, floors
│   │   └── RigidBody2D       — Physics-simulated objects (crates, projectiles)
│   └── Area2D                — Detection zones (attack range, pickups, triggers)
├── Control                   — UI elements (has anchors, margins, themes)
│   ├── Label                 — Text display
│   ├── Button                — Clickable button
│   ├── Panel                 — Styled container
│   ├── TextureRect           — Image in UI
│   ├── VBoxContainer         — Vertical flex layout (like flexbox column)
│   ├── HBoxContainer         — Horizontal flex layout (like flexbox row)
│   └── GridContainer         — Grid layout (like CSS Grid)
└── CanvasLayer               — Rendering layer (puts children on a separate layer)
```

**Key differences to watch out for:**
- HTML elements are styled with CSS (external). Godot nodes are configured via the **Inspector** panel (editor) or via code (setting properties directly).
- HTML elements are all rectangles. Godot Node2D nodes have position/rotation/scale and can be any shape.
- **Node2D vs Control:** This is the most important distinction. Node2D is for game world objects (player, enemies, tiles). Control is for UI (labels, buttons, panels). They use different coordinate systems and different layout rules. Never mix them up — a Label (Control) in the game world will not move with the camera. A Sprite2D in the UI layer will not respect UI layout.

---

### Scenes (.tscn) vs Components

**What you already know:** In React, you build reusable components. A `<PlayerCard>` component has its own markup, logic, and styling. You instantiate it with props: `<PlayerCard name="Hero" hp={100} />`. Components compose into larger components.

**The Godot equivalent:** Godot has **Scenes** — `.tscn` files that define a tree of nodes. A scene is like a component: it has its own node structure, attached script (logic), and configured properties. You instantiate it with `[Export]` properties (like props).

```csharp
// --- Enemy.cs (attached to the root node of enemy.tscn) ---

// [Export] is like React props — configurable from the editor Inspector
// or when instantiating from code
public partial class Enemy : CharacterBody2D
{
    [Export] public int MaxHp { get; set; } = 30;
    [Export] public float MoveSpeed { get; set; } = 60.0f;
    [Export] public int DangerTier { get; set; } = 1;
    [Export] public Color EnemyColor { get; set; } = Colors.Green;
}

// Scene structure in enemy.tscn:
// Enemy (CharacterBody2D)         <- root node, Enemy.cs attached here
//   +-- Polygon2D                 <- visual shape (the colored circle)
//   +-- CollisionShape2D          <- physics collision boundary
//   +-- AttackRange (Area2D)      <- detection zone for when enemy can hit player
//   |   +-- CollisionShape2D      <- shape of the detection zone
//   +-- HitCooldownTimer (Timer)  <- prevents damage every frame
```

```csharp
// --- Dungeon.cs (instantiating enemy scenes) ---

// Load the scene (like a static import)
public partial class Dungeon : Node2D
{
    private PackedScene _enemyScene = GD.Load<PackedScene>("res://scenes/enemies/enemy.tscn");

    private void SpawnEnemy()
    {
        // Instantiate<T>() creates a new copy (like React rendering a component)
        var enemy = _enemyScene.Instantiate<Enemy>();

        // Set "props" before adding to tree
        enemy.MaxHp = 50;
        enemy.MoveSpeed = 80.0f;
        enemy.DangerTier = 2;
        enemy.EnemyColor = Colors.Yellow;

        // Set position
        enemy.Position = new Vector2(100, 200);

        // Add to the scene tree (like ReactDOM.render or appendChild)
        GetNode<Node>("EnemyContainer").AddChild(enemy);
    }
}
```

**Key differences to watch out for:**
- React components re-render when props/state change. Godot scenes do NOT re-render — you update properties directly and Godot renders the current state every frame.
- React components are functions or classes. Godot scenes are **files on disk** (`.tscn`) that you edit visually in the editor and instantiate from code.
- `GD.Load<PackedScene>("res://path.tscn")` loads the scene at call time. In C#, there is no `preload()` equivalent; use `GD.Load<T>()` and cache the result in a field.
- A PackedScene is like a component class. Calling `.Instantiate<T>()` is like calling `new Component()` or `<Component />`. You get a new independent instance each time.

---

### C# (Godot) vs JavaScript

**What you already know:** JavaScript syntax — `let`, `const`, `function`, `class`, `=>`, `switch`, `for...of`, `async/await`.

**The Godot equivalent:** C# — `var`, `const`/`readonly`, methods, `class` (always), lambda expressions, `switch` (same keyword), `foreach`, `await` (with signals).

#### Variable Declarations

```csharp
// JavaScript:                    // C#:
// let hp = 100                   int hp = 100;
// const MAX = 50                 const int Max = 50;
// let name = "Hero"              string name = "Hero";
// let pos = {x: 0, y: 0}        Vector2 pos = Vector2.Zero;

// C# is statically typed — every variable has a declared type:
int hp = 100;                     // explicit type (preferred for fields)
var hp = 100;                     // inferred type (only valid for local variables)

// C# has const (compile-time constant) and readonly (runtime constant).
// const int Max = 50;            // must be known at compile time
// readonly int _max = 50;        // set once, at declaration or in constructor
```

#### Functions

```csharp
// JavaScript:                    // C#:
// function damage(amount) {      private void Damage(int amount)
//   hp -= amount                 {
// }                                  _hp -= amount;
                                  // }

// Lambda expressions replace arrow functions:
// const cb = () => print("hi")   Action cb = () => GD.Print("hi");

// Default parameters work the same:
private void SpawnEnemy(int tier = 1, int count = 1)
{
    for (int i = 0; i < count; i++)
        CreateEnemy(tier);
}

// Return types are declared before the method name:
private int GetDamage()
{
    return 12 + _level * 2;
}

// void means no return value (same as TypeScript's void)
private void TakeDamage(int amount)
{
    _hp -= amount;
}
```

#### Signals (Godot C#)

```csharp
// Signals are declared as delegate types with the [Signal] attribute.
// They are like custom events in JavaScript (EventEmitter pattern).

[Signal] public delegate void HealthChangedEventHandler(int newHp, int maxHp);
[Signal] public delegate void EnemyDefeatedEventHandler(CharacterBody2D enemy);
[Signal] public delegate void PlayerDiedEventHandler();

// Emitting a signal (like eventEmitter.emit())
private void TakeDamage(int amount)
{
    _hp -= amount;
    EmitSignal(SignalName.HealthChanged, _hp, _maxHp);  // notify all listeners
    if (_hp <= 0)
        EmitSignal(SignalName.PlayerDied);
}

// Connecting to a signal (like addEventListener)
public override void _Ready()
{
    // Connect in code:
    var player = GetNode<Node>("Player");
    player.Connect(Player.SignalName.HealthChanged,
        new Callable(this, MethodName.OnHealthChanged));
    player.Connect(Player.SignalName.PlayerDied,
        new Callable(this, MethodName.OnPlayerDied));
}

private void OnHealthChanged(int newHp, int maxHp)
{
    GetNode<Label>("HPLabel").Text = $"HP: {newHp} / {maxHp}";
}

private void OnPlayerDied()
{
    ShowDeathScreen();
}

// Disconnecting (like removeEventListener)
// player.Disconnect(Player.SignalName.HealthChanged,
//     new Callable(this, MethodName.OnHealthChanged));
```

#### Node references and [Export]

```csharp
// In C#, there is no @onready. Declare fields and assign them in _Ready().
// Without _Ready(), child nodes aren't available yet.

// JavaScript equivalent: getting a DOM reference in DOMContentLoaded
// const label = document.getElementById("hp-label")

private Label _hpLabel = null!;
private CollisionShape2D _collisionShape = null!;
private Area2D _attackRange = null!;

public override void _Ready()
{
    _hpLabel = GetNode<Label>("UI/HUD/HPLabel");
    _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
    _attackRange = GetNode<Area2D>("AttackRange");
}

// [Export] — exposes the property in the Godot Inspector panel.
// Like React props — set per-instance from the parent/editor.

[Export] public float MoveSpeed { get; set; } = 200.0f;       // editable in Inspector
[Export] public int MaxHp { get; set; } = 100;                 // editable in Inspector
[Export(PropertyHint.Range, "0,1")] public float Armor { get; set; } = 0.0f;  // slider in Inspector, 0-1 range
[Export] public Color EnemyColor { get; set; } = Colors.Red;   // color picker in Inspector
```

#### Control Flow

```csharp
// if/else if/else (same as JS)
if (hp <= 0)
    Die();
else if (hp < 20)
    FlashWarning();
else
{
    // do nothing (like {} in JS)
}

// switch (same keyword as JS, C# does not fall through by default)
switch (dangerTier)
{
    case 1:
        color = Colors.Green;
        break;
    case 2:
        color = Colors.Yellow;
        break;
    case 3:
        color = Colors.Red;
        break;
    default:
        color = Colors.White;  // default case
        break;
}

// for loops
for (int i = 0; i < 10; i++)   // like: for (let i = 0; i < 10; i++)
    GD.Print(i);

foreach (var enemy in enemies)  // like: for (const enemy of enemies)
    enemy.UpdateAi();

foreach (var key in statsDict.Keys)  // like: for (const key in obj)
    GD.Print(key, statsDict[key]);

// while loops (same concept)
while (hp > 0)
    TakeTickDamage();

// Ternary (conditional expression — same as JS)
// JS:  const label = hp > 50 ? "healthy" : "injured"
string label = hp > 50 ? "healthy" : "injured";
```

#### Data Structures

```csharp
// Lists (like JS arrays — use Godot.Collections or System.Collections.Generic)
var enemies = new Godot.Collections.Array<CharacterBody2D>();
enemies.Add(enemy);            // like push()
enemies.Remove(enemy);         // like splice(indexOf(enemy), 1)
enemies.Count;                 // like length
enemies[0];                    // same indexing
enemies.Count == 0;            // like length === 0

// Dictionaries (like JS objects / Map)
var stats = new Godot.Collections.Dictionary
{
    { "hp", 100 },
    { "xp", 0 },
    { "level", 1 }
};
stats["hp"];                   // bracket access (like obj["key"])
// no dot access in C# dictionaries
stats.TryGetValue("hp", out var hpVal);  // with fallback check

// String formatting
string text = $"HP: {hp} / {maxHp}";             // interpolated string
string text2 = "Level: " + level.ToString();      // concatenation
```

**Key differences to watch out for:**
- C# uses **braces** for blocks (like JavaScript), not indentation.
- Semicolons are required after every statement.
- `null` is the same keyword in C#.
- `==` does value comparison for value types and reference comparison for reference types (use `.Equals()` for custom comparison).
- `&&` / `||` / `!` are the logical operators (same as JavaScript).
- No hoisting. Variables must be declared before use.
- No destructuring of dictionaries. You can deconstruct tuples and records though.
- `GD.Print()` is the debug output (like `console.log()`). Output appears in the Godot Output panel.

---

### _Ready() vs DOMContentLoaded

**What you already know:** In web dev, `DOMContentLoaded` fires when the HTML is parsed and the DOM tree is built. You use it to safely access DOM elements.

**The Godot equivalent:** `_Ready()` fires when a node and **all its children** are added to the scene tree and initialized. You use it to safely access child nodes via `GetNode<T>()`.

```csharp
// The full node lifecycle:

// 1. Constructor — Called when the node is created in memory.
//    Like: class constructor in JS
//    DO: Initialize variables that don't depend on the scene tree.
//    DON'T: Access child nodes (GetNode) — they don't exist yet.
public MyNode()
{
    _hp = _maxHp;  // safe — just setting a variable
}

// 2. _EnterTree() — Called when the node is added to the scene tree.
//    Like: connectedCallback() in Web Components
//    The node is in the tree, but children may not be ready yet.
public override void _EnterTree()
{
    AddToGroup("enemies");  // safe — node is in the tree
}

// 3. _Ready() — Called after the node AND all its children are in the tree.
//    Like: DOMContentLoaded or React componentDidMount
//    This is where you do most initialization.
public override void _Ready()
{
    GetNode<Label>("HPLabel").Text = _hp.ToString();  // safe — children are ready
    GetNode<Area2D>("AttackRange").BodyEntered += OnAttackRangeEntered;
    Velocity = Vector2.Zero;
}

// 4. _Process(delta) / _PhysicsProcess(delta) — Called every frame.
//    (Covered in the next section)

// 5. _ExitTree() — Called when the node is removed from the scene tree.
//    Like: disconnectedCallback() in Web Components or componentWillUnmount
//    Clean up connections, save state, etc.
public override void _ExitTree()
{
    // Clean up anything that persists beyond this node's lifetime
}
```

**Key differences to watch out for:**
- `_Ready()` is called **bottom-up** — children's `_Ready()` fires before their parent's `_Ready()`. This guarantees that when your `_Ready()` runs, all `GetNode<T>()` references are valid.
- `_Ready()` runs only **once** per node instance (unless the node is removed and re-added to the tree).
- In C#, there is no `@onready` shorthand. Declare fields and assign them inside `_Ready()`.
- There is no equivalent of `window.onload` (waiting for all assets). Godot loads resources at scene load time or uses `GD.Load<T>()` in scripts.

---

### _Process() / _PhysicsProcess() vs requestAnimationFrame

**What you already know:** `requestAnimationFrame(callback)` runs your callback before the next browser repaint, ~60fps. You use it for animations and game loops.

**The Godot equivalent:** Godot has TWO frame callbacks, and understanding the difference is critical:

```csharp
// _Process(delta) — runs every RENDER frame
// Like: requestAnimationFrame
// Frame rate varies (60fps, 144fps, etc.) depending on the monitor and GPU load.
// Use for: visual effects, animations, UI updates, camera smoothing
public override void _Process(double delta)
{
    // delta = seconds since last frame (e.g., 0.016 at 60fps)
    // Rotate a visual effect
    GetNode<Node2D>("SlashEffect").Rotation += 5.0f * (float)delta;
}

// _PhysicsProcess(delta) — runs every PHYSICS frame
// Like: setInterval(callback, 1000/60) but built into the engine
// ALWAYS runs at a fixed rate: 60 times per second (configurable in project settings).
// delta is always ~0.01667 (1/60th of a second).
// Use for: movement, collision, AI, anything that affects gameplay.
public override void _PhysicsProcess(double delta)
{
    // Handle input and movement
    Vector2 inputVector = Input.GetVector(
        "move_left", "move_right", "move_up", "move_down"
    );
    Velocity = inputVector * _moveSpeed;
    MoveAndSlide();  // applies velocity, handles collision, uses delta internally
}
```

**Why two loops?** Consider a player on a 144fps monitor fighting a player on a 60fps monitor. If movement is in `_Process()`, the 144fps player's code runs 2.4x more often. Even with delta time, floating-point accumulation causes tiny differences. `_PhysicsProcess()` runs at the same fixed rate for everyone, ensuring deterministic gameplay.

```csharp
// Practical rule of thumb:
// _PhysicsProcess(delta) — player movement, enemy AI, combat logic, anything that
//                          affects game state or uses MoveAndSlide()
// _Process(delta)        — particle effects, camera smoothing, UI animations,
//                          anything purely visual

// Example: a complete player movement script
public partial class Player : CharacterBody2D
{
    [Export] public float MoveSpeed { get; set; } = 200.0f;

    public override void _PhysicsProcess(double delta)
    {
        Vector2 inputVector = Input.GetVector(
            "move_left", "move_right", "move_up", "move_down"
        );
        Velocity = inputVector * MoveSpeed;
        MoveAndSlide();
    }
}
```

**Key differences to watch out for:**
- `delta` in `_Process()` varies frame-to-frame. `delta` in `_PhysicsProcess()` is (nearly) constant.
- `MoveAndSlide()` must be called inside `_PhysicsProcess()`. It uses the physics engine which runs on the physics tick.
- You do NOT multiply velocity by delta when using `MoveAndSlide()` — it handles delta internally. This is the biggest gotcha for web devs.
- You CAN put everything in `_PhysicsProcess()` for simplicity. The game won't break. Separating them is an optimization and correctness concern that matters more as the game grows.
- To disable processing: `SetProcess(false)` / `SetPhysicsProcess(false)`. Like pausing a `requestAnimationFrame` loop.

---

### Signals vs Events

**What you already know:** DOM events (`addEventListener`, `dispatchEvent`), Node.js EventEmitter (`on`, `emit`), or React callbacks (`onClick`, `onChange`).

**The Godot equivalent:** Signals — a built-in observer pattern. Every node can declare signals, emit them, and connect to other nodes' signals.

```csharp
// --- Declaring signals (like defining custom event types) ---
[Signal] public delegate void HealthChangedEventHandler(int newHp, int maxHp);
[Signal] public delegate void DiedEventHandler();
[Signal] public delegate void LevelUpEventHandler(int newLevel);

// --- Emitting signals (like dispatchEvent or eventEmitter.emit) ---
private void TakeDamage(int amount)
{
    _hp -= amount;
    EmitSignal(SignalName.HealthChanged, _hp, _maxHp);
    if (_hp <= 0)
        EmitSignal(SignalName.Died);
}

// --- Connecting in code (like addEventListener) ---
public override void _Ready()
{
    // Using Callable:
    var player = GetNode<Node>("Player");
    player.Connect(Player.SignalName.HealthChanged,
        new Callable(this, MethodName.OnPlayerHealthChanged));
    player.Connect(Player.SignalName.Died,
        new Callable(this, MethodName.OnPlayerDied));
}

// --- Connecting in the editor ---
// In the Godot editor, select a node -> Node tab (right panel) -> Signals tab
// Double-click a signal -> select the target node -> Godot generates the callback.
// Editor-connected signals show a green icon in the script margin.

// --- Handler methods ---
// Convention: On<NodeName><SignalName>
private void OnPlayerHealthChanged(int newHp, int maxHp)
{
    _hpBar.Value = newHp;
}

private void OnPlayerDied()
{
    ShowDeathScreen();
}

// --- Disconnecting (like removeEventListener) ---
// GetNode<Node>("Player").Disconnect(Player.SignalName.HealthChanged,
//     new Callable(this, MethodName.OnPlayerHealthChanged));

// --- One-shot connection (like { once: true }) ---
// GetNode<Node>("Player").Connect(Player.SignalName.Died,
//     new Callable(this, MethodName.OnPlayerDied), (uint)ConnectFlags.OneShot);

// --- Built-in signals (like built-in DOM events) ---
// Area2D has: BodyEntered, BodyExited, AreaEntered, AreaExited
// Button has: Pressed, Toggled, MouseEntered, MouseExited
// Timer has: Timeout
// CharacterBody2D: no built-in signals (uses MoveAndSlide() return value)
// AnimationPlayer has: AnimationFinished

// --- await with signals (like JS await with Promises) ---
// Wait for a timer to finish (like await new Promise(resolve => setTimeout(resolve, 1000)))
await ToSignal(GetTree().CreateTimer(1.0), SceneTreeTimer.SignalName.Timeout);
GD.Print("1 second has passed");

// Wait for a signal from another node
await ToSignal(GetNode<AnimationPlayer>("AnimationPlayer"),
    AnimationPlayer.SignalName.AnimationFinished);
GD.Print("animation done");
```

**Key differences to watch out for:**
- DOM events bubble up the tree. Godot signals do NOT bubble — they go directly from emitter to connected receiver.
- DOM events carry an Event object with properties. Godot signals carry typed arguments declared in the signal definition.
- You can connect signals in the **editor** (Node tab > Signals) or in **code**. Editor connections are stored in the `.tscn` file. Code connections are set up in `_Ready()`.
- Convention: handler methods are named `On<SourceNode><SignalName>` using PascalCase.

---

### Resources (.tres) vs Assets

**What you already know:** In web dev, you have CSS files, JSON config files, image files, and font files. They are separate from your HTML/JS and loaded as needed.

**The Godot equivalent:** Resources (`.tres` files) are Godot's configuration/data objects. They store data that nodes use — like a TileSet defining your tile grid, a Theme defining your UI colors, or a StyleBox defining a panel's appearance.

```csharp
// Common resource types:

// TileSet (.tres) — defines tiles, their textures, physics shapes, and properties
// Like: a CSS sprite sheet definition + collision map

// Theme (.tres) — defines UI styling (fonts, colors, margins, StyleBoxes)
// Like: a CSS stylesheet

// StyleBox (.tres) — defines how a UI panel/button looks (background, border, padding)
// Like: a single CSS rule for a component

// PackedScene (.tscn) — a scene IS a resource (can be loaded and instantiated)
// Like: a component module

// Loading resources in code:
var tileset = GD.Load<TileSet>("res://resources/tile_sets/dungeon_tileset.tres");
var theme = GD.Load<Theme>("res://resources/themes/game_theme.tres");

// Creating resources in code (rare, usually done in editor):
var style = new StyleBoxFlat();
style.BgColor = new Color(0.1f, 0.12f, 0.18f, 0.85f);
style.BorderColor = new Color(0.96f, 0.78f, 0.42f, 0.3f);
style.SetBorderWidthAll(1);
style.SetCornerRadiusAll(10);
GetNode<Panel>("Panel").AddThemeStyleboxOverride("panel", style);
```

**The `res://` and `user://` paths:**

```csharp
// res:// — project directory (read-only in exported builds)
// Like: the /public or /static directory in a web project
// Everything in your Godot project folder is accessible via res://
var scene = GD.Load<PackedScene>("res://scenes/player/player.tscn");
var tileset = GD.Load<TileSet>("res://resources/tile_sets/dungeon_tileset.tres");

// user:// — user data directory (read/write, persists across sessions)
// Like: localStorage or IndexedDB
// Platform-specific location:
//   macOS: ~/Library/Application Support/Godot/app_userdata/<ProjectName>/
//   Windows: %APPDATA%/Godot/app_userdata/<ProjectName>/
//   Linux: ~/.local/share/godot/app_userdata/<ProjectName>/
var saveFile = FileAccess.Open("user://save.json", FileAccess.ModeFlags.Write);
```

**Key differences to watch out for:**
- Web assets are loaded over HTTP. Godot resources are loaded from the local file system (or embedded in the export binary).
- CSS is applied globally by selectors. Godot Themes are applied to a Control node and inherited by its children. You can override individual properties per-node.
- Resources can be shared between nodes. If two enemies reference the same TileSet resource, they share one instance in memory (like a cached CSS file).

---

### Inspector vs CSS

**What you already know:** In web dev, you style elements with CSS properties (`color`, `font-size`, `padding`, `background`). You inspect and modify them in browser DevTools.

**The Godot equivalent:** In Godot, you configure node properties in the **Inspector** panel (right side of the editor). Position, color, speed, collision shapes — everything is a property you can see and edit visually.

```csharp
// Properties you set in the Inspector can also be set in code:

// Position (like CSS top/left with position: absolute)
Position = new Vector2(500, 300);

// Scale (like CSS transform: scale())
Scale = new Vector2(2.0f, 2.0f);

// Rotation (like CSS transform: rotate(), but in radians)
Rotation = Mathf.Pi / 4;  // 45 degrees

// Visibility (like CSS display: none / visibility: hidden)
Visible = false;    // hides the node and all children

// Modulate (like CSS color/opacity — tints the node)
Modulate = new Color(1, 0, 0, 0.5f);  // red tint at 50% opacity

// Z-index (like CSS z-index — draw order)
ZIndex = 10;  // higher = drawn on top

// [Export] makes YOUR custom properties appear in the Inspector:
[Export] public float MoveSpeed { get; set; } = 200.0f;   // appears as a number field
[Export] public int MaxHp { get; set; } = 100;             // appears as an integer field
[Export] public Color EnemyColor { get; set; } = Colors.Red;     // appears as a color picker
[Export] public string EnemyName { get; set; } = "Goblin";       // appears as a text field
[Export(PropertyHint.Range, "1,3")] public int DangerTier { get; set; } = 1;  // appears as a slider, 1-3
[Export(PropertyHint.Enum, "Melee,Ranged,Magic")] public int AttackType { get; set; } = 0;  // dropdown
```

**Key differences to watch out for:**
- CSS is declarative (you say what it should look like and the browser figures out how). Godot properties are imperative (you set exact values and the engine uses them directly).
- CSS has cascading and inheritance. Godot Inspector properties do NOT cascade — each node's properties are independent. The exception is Theme, which does inherit down the Control node tree.
- Browser DevTools let you live-edit CSS. Godot's Inspector lets you live-edit properties while the game is running (in the Remote tab), but changes are lost when you stop the game unless you use "Save Branch as Scene."

---

### Input System vs Event Listeners

**What you already know:** `document.addEventListener("keydown", handler)`, `event.key`, `event.preventDefault()`. You listen for specific key events and respond.

**The Godot equivalent:** Godot uses an **Input Map** — named actions that can be bound to multiple keys/buttons. You check action state rather than raw keys.

```csharp
// Step 1: Define actions in Input Map (Project -> Project Settings -> Input Map)
// Action "move_left"  -> bound to: A key, Left Arrow key, Gamepad Left
// Action "move_right" -> bound to: D key, Right Arrow key, Gamepad Right
// Action "move_up"    -> bound to: W key, Up Arrow key, Gamepad Up
// Action "move_down"  -> bound to: S key, Down Arrow key, Gamepad Down

// Step 2: Check actions in code

// Method 1: Polling (check every frame in _PhysicsProcess)
// Like: checking a "keys currently pressed" set each animation frame
public override void _PhysicsProcess(double delta)
{
    // Input.GetVector() returns a normalized Vector2 from 4 directional actions
    // Like: building a direction vector from WASD key states
    Vector2 inputVector = Input.GetVector(
        "move_left", "move_right", "move_up", "move_down"
    );
    Velocity = inputVector * _moveSpeed;
    MoveAndSlide();
}

// Method 2: Event-based (respond to individual input events)
// Like: addEventListener("keydown", handler)
public override void _Input(InputEvent @event)
{
    // Handles ALL input events (keyboard, mouse, gamepad)
    if (@event.IsActionPressed("restart"))
        GetTree().ReloadCurrentScene();
}

public override void _UnhandledInput(InputEvent @event)
{
    // Like _Input(), but only receives events not consumed by UI.
    // Use this for game-world input so that clicking a UI button
    // doesn't also trigger a game action.
    if (@event.IsActionPressed("attack"))
        PerformAttack();
}

// Individual checks:
Input.IsActionPressed("move_left");        // is the key currently held? (like keydown state)
Input.IsActionJustPressed("attack");       // was the key pressed THIS frame? (like keydown event)
Input.IsActionJustReleased("attack");      // was the key released THIS frame? (like keyup event)
Input.GetActionStrength("move_right");     // 0.0 to 1.0 (for analog stick support)
```

**Key differences to watch out for:**
- Web: you listen for specific keys (`event.key === "ArrowLeft"`). Godot: you check named actions (`Input.IsActionPressed("move_left")`). This means rebinding keys is free — change the Input Map, code stays the same.
- Web: events bubble and can be stopped with `stopPropagation()`. Godot: `_Input()` receives events first, then `_UnhandledInput()`. You can "consume" an event with `GetViewport().SetInputAsHandled()`.
- `_Input()` vs `_UnhandledInput()`: Use `_UnhandledInput()` for gameplay input. UI Control nodes (Button, etc.) consume their events in `_Input()`, so `_UnhandledInput()` only fires if no UI element handled it. This prevents clicking a UI button from also triggering a game action.

---

### Autoloads vs Global Variables

**What you already know:** In JavaScript, you might have a global state object (`window.gameState`), a module-level singleton (`export const store = new Store()`), or a React Context provider wrapping the app.

**The Godot equivalent:** **Autoloads** — scripts (or scenes) that Godot loads automatically when the game starts and keeps alive for the entire session. They are accessible from any script by name.

```csharp
// Step 1: Create the autoload script
// --- scripts/autoloads/GameState.cs ---
public partial class GameState : Node
{
    // Signals for reactive updates (like a store's subscription)
    [Signal] public delegate void StatsChangedEventHandler();
    [Signal] public delegate void PlayerDiedEventHandler();
    [Signal] public delegate void FloorChangedEventHandler(int newFloor);

    // State variables with setter that emits signal on change
    private int _hp = 100;
    public int Hp
    {
        get => _hp;
        set
        {
            _hp = value;
            EmitSignal(SignalName.StatsChanged);  // auto-notify on change
        }
    }
    public int MaxHp { get; set; } = 100;
    public int Xp { get; set; } = 0;
    public int Level { get; set; } = 1;
    public int FloorNumber { get; set; } = 1;

    public void Reset()
    {
        Hp = 100;
        MaxHp = 100;
        Xp = 0;
        Level = 1;
        FloorNumber = 1;
    }
}

// Step 2: Register as Autoload
// Project -> Project Settings -> Autoload tab
// Path: res://scripts/autoloads/GameState.cs
// Name: GameState (this becomes the global access name)

// Step 3: Access from ANY script, anywhere
// --- scenes/ui/Hud.cs ---
public partial class Hud : Control
{
    private Label _hpLabel = null!;

    public override void _Ready()
    {
        _hpLabel = GetNode<Label>("HPLabel");
        // Access the autoload by its registered name
        var gameState = GetNode<GameState>("/root/GameState");
        gameState.Connect(GameState.SignalName.StatsChanged,
            new Callable(this, MethodName.UpdateDisplay));
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        var gameState = GetNode<GameState>("/root/GameState");
        _hpLabel.Text = $"HP: {gameState.Hp} / {gameState.MaxHp}";
    }
}
```

**Key differences to watch out for:**
- JavaScript modules use `import`/`require`. In Godot C#, autoloads are accessed via `GetNode<T>("/root/AutoloadName")`. There is no automatic global access by name as in GDScript (if you were using GDScript instead).
- Autoloads are **always in the tree**. When you change scenes (`GetTree().ChangeSceneToFile()`), autoloads persist. They live above the scene tree root.
- Autoloads are processed in order. If `GameState` depends on `EventBus`, register `EventBus` first in the Autoload list.
- Don't overuse autoloads. Only truly global state belongs there (game state, event bus, audio manager). Scene-specific logic belongs in scene scripts.

---

### TileMapLayer vs Canvas Drawing

**What you already know:** In the Phaser prototype, the background is drawn with `this.add.graphics()` — procedurally drawing grid lines on a canvas. In web dev, you might draw on a `<canvas>` element with `ctx.fillRect()`.

**The Godot equivalent:** **TileMapLayer** — a node that efficiently renders a grid of tiles. You define tile types in a **TileSet** resource, then paint tiles onto the map in the editor or set them in code.

```csharp
// TileSet resource (dungeon_tileset.tres) defines:
// - Tile size: 64x32 pixels (isometric diamond)
// - Tile shape: Diamond (isometric)
// - Tile layout: Diamond Down
// - Tile offset axis: Horizontal
// - Physics layers: wall tiles have CollisionPolygon, floor tiles don't
// - Each tile has an ID (atlas coords) used to place it

// TileMapLayer node configuration (in the scene):
// - Assigned TileSet: dungeon_tileset.tres
// - Y-sort enabled: true (tiles drawn in correct depth order for isometric)

// Placing tiles in code:
private void GenerateFloor()
{
    var tilemap = GetNode<TileMapLayer>("TileMapLayer");

    // Clear existing tiles
    tilemap.Clear();

    // Set a floor tile at grid position (5, 3)
    // Parameters: coords, source_id, atlas_coords, alternative_tile
    tilemap.SetCell(new Vector2I(5, 3), 0, new Vector2I(0, 0));  // floor tile

    // Set a wall tile at grid position (5, 0)
    tilemap.SetCell(new Vector2I(5, 0), 0, new Vector2I(1, 0));  // wall tile

    // Check what tile is at a position
    Vector2I tileCoords = tilemap.GetCellAtlasCoords(new Vector2I(5, 3));

    // Convert between world coordinates and tile coordinates
    Vector2 worldPos = tilemap.MapToLocal(new Vector2I(3, 5));     // grid -> world
    Vector2I gridPos = tilemap.LocalToMap(new Vector2(320, 96));   // world -> grid
}

// Generate a simple room:
private void GenerateRoom(int width, int height)
{
    var tilemap = GetNode<TileMapLayer>("TileMapLayer");
    for (int x = 0; x < width; x++)
    {
        for (int y = 0; y < height; y++)
        {
            if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                // Wall tile on edges
                tilemap.SetCell(new Vector2I(x, y), 0, new Vector2I(1, 0));
            else
                // Floor tile inside
                tilemap.SetCell(new Vector2I(x, y), 0, new Vector2I(0, 0));
        }
    }
}
```

**Key differences to watch out for:**
- Canvas drawing is immediate mode (draw every frame). TileMapLayer is retained mode (set tiles once, engine renders them every frame automatically). You only call `SetCell()` when the map changes, not every frame.
- TileMaps handle isometric coordinate conversion automatically. In canvas drawing, you'd need to calculate diamond positions manually.
- Physics collision for walls is defined in the TileSet resource — each tile type can have its own collision polygon. No need to create separate physics bodies for every wall tile.
- TileMaps are dramatically more performant than drawing individual shapes. The engine batches all tiles into a single draw call.

---

### Camera2D vs Viewport

**What you already know:** In web dev, the browser viewport shows a portion of the page. You scroll to see different parts. CSS `scroll-behavior: smooth` adds smooth scrolling. In the Phaser prototype, the camera is fixed (the entire game world fits on screen).

**The Godot equivalent:** **Camera2D** — a node that controls which part of the 2D world is visible. It follows the player, can zoom in/out, and has limits to prevent showing areas outside the map.

```csharp
// Camera2D is typically a child of the player node:
// Player (CharacterBody2D)
//   +-- Polygon2D
//   +-- CollisionShape2D
//   +-- Camera2D            <- follows player automatically because it's a child

// Camera configuration (set in Inspector or code):
public partial class GameCamera : Camera2D
{
    public override void _Ready()
    {
        // Make this the active camera
        MakeCurrent();

        // Smooth follow (like CSS scroll-behavior: smooth)
        PositionSmoothingEnabled = true;
        PositionSmoothingSpeed = 8.0f;      // higher = snappier follow

        // Zoom level (1.0 = normal, 2.0 = zoomed in 2x, 0.5 = zoomed out 2x)
        Zoom = new Vector2(2.0f, 2.0f);

        // Camera limits (prevent seeing beyond the map)
        LimitLeft = 0;
        LimitTop = 0;
        LimitRight = 3200;      // map width in pixels
        LimitBottom = 1800;      // map height in pixels
        LimitSmoothed = true;    // smooth when hitting limits

        // Drag margins (camera moves only when player is near viewport edge)
        DragHorizontalEnabled = true;
        DragVerticalEnabled = true;
        DragLeftMargin = 0.2f;   // camera starts moving when player is in outer 20%
        DragRightMargin = 0.2f;
        DragTopMargin = 0.2f;
        DragBottomMargin = 0.2f;
    }

    // Camera shake (like the Phaser this.cameras.main.shake() in our prototype):
    public void Shake(float duration = 0.15f, float strength = 4.0f)
    {
        var tween = CreateTween();
        for (int i = 0; i < (int)(duration / 0.03f); i++)
        {
            tween.TweenProperty(this, "offset",
                new Vector2((float)GD.RandRange(-strength, strength),
                            (float)GD.RandRange(-strength, strength)),
                0.03f);
        }
        tween.TweenProperty(this, "offset", Vector2.Zero, 0.03f);
    }
}
```

**Key differences to watch out for:**
- In web dev, scrolling moves the viewport over the content. In Godot, Camera2D moves through the world — the world stays still, the camera moves.
- Only one Camera2D can be "current" (active) at a time per viewport. Setting `MakeCurrent()` on one camera deactivates the previous one.
- Camera2D only affects Node2D nodes. **Control nodes (UI) are NOT affected by Camera2D.** This is by design — HUD elements stay in place while the world scrolls. To achieve this, put UI in a CanvasLayer.
- `Zoom = new Vector2(2.0f, 2.0f)` means zoomed IN (objects appear larger). This is the opposite of what you might expect. Think of it as "2x magnification."

---

### Groups vs CSS Classes

**What you already know:** CSS classes let you tag elements and select them collectively. `.enemy { color: red; }` styles all elements with class "enemy". `document.querySelectorAll(".enemy")` gets them all.

**The Godot equivalent:** **Groups** — a tagging system for nodes. You add nodes to named groups and query them.

```csharp
// Adding to a group (like element.classList.add("enemy"))
public override void _Ready()
{
    AddToGroup("enemies");
    AddToGroup("damageable");
}

// Or add in the editor: select node -> Node tab -> Groups tab -> type name -> Add

// Checking group membership (like element.classList.contains("enemy"))
if (node.IsInGroup("enemies"))
    node.Call("TakeDamage", 10);

// Getting all nodes in a group (like document.querySelectorAll(".enemies"))
var allEnemies = GetTree().GetNodesInGroup("enemies");
foreach (var enemy in allEnemies)
    enemy.QueueFree();  // destroy all enemies

// Calling a method on all nodes in a group (no web equivalent — batch operation)
GetTree().CallGroup("enemies", "Freeze");  // calls Freeze() on every enemy

// Removing from a group (like element.classList.remove("enemy"))
RemoveFromGroup("enemies");
```

**Key differences to watch out for:**
- CSS classes affect styling. Godot groups do NOT affect appearance — they are purely for identification and batch operations.
- CSS classes are defined in stylesheets. Godot groups are defined per-node instance, either in code or in the editor. There is no "group definition file."
- `GetTree().GetNodesInGroup()` returns nodes in **tree order** (top to bottom). This is deterministic.
- Groups are commonly used for collision filtering: "Is the body that entered my Area2D in the 'enemies' group?"

---

### Coordinate System

**What you already know:** In CSS and Canvas, (0,0) is the top-left corner. X increases rightward, Y increases downward. This is the same in Godot.

**The Godot equivalent:** Identical for 2D — Y-down coordinate system. But isometric rendering adds a transformation layer.

```csharp
// Standard 2D coordinates (same as web):
// (0,0) --- X+ ->
//   |
//   Y+
//   v

// A node at position new Vector2(100, 200) is 100px right, 200px down from its parent.

// Isometric adds complexity:
// In isometric view, the grid is rotated 45 degrees and squashed vertically.
// A 64x32 diamond tile means:
//   - Tile width (horizontal diagonal): 64 pixels
//   - Tile height (vertical diagonal): 32 pixels
//   - This gives the classic 2:1 isometric ratio

// Grid coordinates (integer, logical) vs World coordinates (pixel, visual):
// Grid (0,0) maps to the top-center of the map
// Grid (1,0) is one tile to the lower-right
// Grid (0,1) is one tile to the lower-left

// Converting between grid and world coordinates:
var tilemap = GetNode<TileMapLayer>("TileMapLayer");

// Grid -> World (where to place a sprite on the screen for grid cell 3,5)
Vector2 worldPos = tilemap.MapToLocal(new Vector2I(3, 5));

// World -> Grid (which grid cell did the player click on)
Vector2I gridPos = tilemap.LocalToMap(GetGlobalMousePosition());

// Important: "local" in MapToLocal means the TileMapLayer's local coordinate space.
// If the TileMapLayer is at (0,0) in the scene, local = global.

// Isometric sorting (draw order):
// In isometric view, objects closer to the "camera" (lower on screen) must be drawn
// on top of objects further away. Godot handles this with:
// - TileMapLayer: YSortEnabled = true (tiles sort automatically)
// - Parent Node2D: YSortEnabled = true (child nodes sort by Y position)
// - Individual nodes: ZIndex for manual override
```

**Key differences to watch out for:**
- In a flat 2D game, screen coordinates = world coordinates. In isometric, they diverge — a tile at grid (5, 3) is NOT at pixel (5, 3). Always use `MapToLocal()` / `LocalToMap()` for conversion.
- Y-sorting is essential for isometric. Without it, a character walking behind a wall might be drawn in front of it. Enable `YSortEnabled` on the parent container of all entities.
- Rotation in Godot is in **radians**, not degrees. `Mathf.Pi` = 180 degrees. Use `Mathf.DegToRad()` and `Mathf.RadToDeg()` for conversion.

## Open Questions

- Should this document cover Godot's shader language (for visual effects) or is that too advanced for a basics doc?
- Would animated GIFs or screenshots from the Godot editor help illustrate the scene tree and Inspector concepts?
- Should we add a "common mistakes" section with C# pitfalls specific to JavaScript developers?
- Is there value in documenting C# typing more deeply (generics, custom classes, enums)?
