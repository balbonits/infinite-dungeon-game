# Godot 4 Basics for Web Developers

## Summary

A comprehensive bridge document mapping web development concepts to Godot 4 equivalents. If you know HTML, CSS, JavaScript, and the DOM, you already have the mental models — they just need to be translated.

## Current State

This document covers every major Godot 4 concept a front-end developer needs to understand before reading or writing GDScript. Each section maps a familiar web concept to its Godot equivalent with runnable GDScript code examples.

## Design

### Scene Tree vs DOM

**What you already know:** The browser has a DOM — a tree of HTML elements. The `<body>` contains `<div>`s, which contain `<p>`s, and so on. You traverse it, add/remove elements, and the browser renders the current state.

**The Godot equivalent:** Godot has a **Scene Tree** — a tree of **Nodes**. The root `Node` contains child nodes, which contain their own children. You traverse it, add/remove nodes, and Godot renders the current state.

```gdscript
# Traversing the tree (like document.querySelector / DOM traversal)
var player = get_node("Player")          # like document.getElementById("Player")
var player = $Player                     # shorthand for get_node("Player")
var label = $UI/HUD/HPLabel              # nested path, like querySelector("UI > HUD > HPLabel")

# Adding a node (like document.createElement + appendChild)
var enemy = enemy_scene.instantiate()    # like document.createElement("div")
add_child(enemy)                         # like parent.appendChild(child)

# Removing a node (like element.remove())
enemy.queue_free()                       # schedules removal at end of frame (safe)
enemy.free()                             # immediate removal (dangerous, can crash if referenced)

# Getting all nodes in a group (like document.querySelectorAll(".enemies"))
var all_enemies = get_tree().get_nodes_in_group("enemies")
```

**Key differences to watch out for:**
- DOM paths use CSS selectors (`querySelector`). Godot uses **node paths** relative to the current node (`$Parent/Child/Grandchild`).
- In the DOM, elements exist on the page and are hidden/shown with CSS. In Godot, nodes can be truly removed from the tree with `queue_free()` — they are destroyed, not hidden.
- The DOM is one global tree. Godot scenes are **separate trees** that get merged. Your `player.tscn` is its own little tree that gets instanced into the `dungeon.tscn` tree.
- `queue_free()` is almost always preferred over `free()`. It waits until the end of the current frame to remove the node, preventing crashes from dangling references mid-frame.

---

### Nodes vs HTML Elements

**What you already know:** HTML has element types — `<div>`, `<p>`, `<canvas>`, `<button>`, `<input>`. Each has specific behavior and properties. A `<button>` is clickable. A `<canvas>` is drawable. A `<div>` is a generic container.

**The Godot equivalent:** Godot has **Node types** — `Node2D`, `CharacterBody2D`, `Sprite2D`, `Area2D`, `Label`, `Button`, `Camera2D`. Each has specific behavior and properties. A `CharacterBody2D` handles physics movement. A `Sprite2D` displays an image. A `Node2D` is a generic 2D container.

```gdscript
# Common node types and their web equivalents:

# Node2D — like <div> (generic 2D container, has position/rotation/scale)
# Sprite2D — like <img> (displays a texture)
# Label — like <p> or <span> (displays text)
# Button — like <button> (clickable, emits "pressed" signal)
# Panel — like <div> with background (styled container)
# Camera2D — like viewport/scroll position (controls what part of the world is visible)
# CharacterBody2D — no web equivalent (physics-aware object that moves and collides)
# Area2D — no web equivalent (invisible detection zone, triggers when something enters)
# TileMapLayer — no web equivalent (renders a grid of tiles efficiently)
# Timer — like setTimeout/setInterval (emits "timeout" signal)
# AudioStreamPlayer — like <audio> (plays sound)
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

**The Godot equivalent:** Godot has **Scenes** — `.tscn` files that define a tree of nodes. A scene is like a component: it has its own node structure, attached script (logic), and configured properties. You instantiate it with `@export` properties (like props).

```gdscript
# --- enemy.gd (attached to the root node of enemy.tscn) ---

# @export is like React props — configurable from the editor Inspector
# or when instantiating from code
@export var max_hp: int = 30
@export var move_speed: float = 60.0
@export var danger_tier: int = 1
@export var enemy_color: Color = Color.GREEN

# Scene structure in enemy.tscn:
# Enemy (CharacterBody2D)         ← root node, enemy.gd attached here
#   ├── Polygon2D                 ← visual shape (the colored circle)
#   ├── CollisionShape2D          ← physics collision boundary
#   ├── AttackRange (Area2D)      ← detection zone for when enemy can hit player
#   │   └── CollisionShape2D      ← shape of the detection zone
#   └── HitCooldownTimer (Timer)  ← prevents damage every frame
```

```gdscript
# --- dungeon.gd (instantiating enemy scenes) ---

# preload() loads the scene at compile time (like a static import)
const EnemyScene: PackedScene = preload("res://scenes/enemies/enemy.tscn")

func spawn_enemy() -> void:
    # instantiate() creates a new copy (like React rendering a component)
    var enemy: CharacterBody2D = EnemyScene.instantiate()

    # Set "props" before adding to tree
    enemy.max_hp = 50
    enemy.move_speed = 80.0
    enemy.danger_tier = 2
    enemy.enemy_color = Color.YELLOW

    # Set position
    enemy.position = Vector2(100, 200)

    # Add to the scene tree (like ReactDOM.render or appendChild)
    $EnemyContainer.add_child(enemy)
```

**Key differences to watch out for:**
- React components re-render when props/state change. Godot scenes do NOT re-render — you update properties directly and Godot renders the current state every frame.
- React components are functions or classes. Godot scenes are **files on disk** (`.tscn`) that you edit visually in the editor and instantiate from code.
- `preload("res://path.tscn")` runs at parse time (like a static ES6 import). `load("res://path.tscn")` runs at call time (like a dynamic `import()`). Prefer `preload` when the path is known at write time.
- A PackedScene is like a component class. Calling `.instantiate()` is like calling `new Component()` or `<Component />`. You get a new independent instance each time.

---

### GDScript vs JavaScript

**What you already know:** JavaScript syntax — `let`, `const`, `function`, `class`, `=>`, `switch`, `for...of`, `async/await`.

**The Godot equivalent:** GDScript — `var`, `const`, `func`, `class` (rare), no arrow functions, `match` instead of `switch`, `for...in`, `await` (with signals).

#### Variable Declarations

```gdscript
# JavaScript:                    # GDScript:
# let hp = 100                   var hp: int = 100
# const MAX = 50                 const MAX: int = 50
# let name = "Hero"              var name: String = "Hero"
# let pos = {x: 0, y: 0}        var pos: Vector2 = Vector2.ZERO

# Type annotations are optional but strongly recommended:
var hp: int = 100                 # typed (preferred — catches errors early)
var hp = 100                      # untyped (works, but no editor auto-complete)

# GDScript has no let/const distinction for mutability at the variable level.
# var = mutable, const = compile-time constant (must be known at parse time).
# For runtime "constants," just use var and don't reassign.
```

#### Functions

```gdscript
# JavaScript:                    # GDScript:
# function damage(amount) {      func damage(amount: int) -> void:
#   hp -= amount                     hp -= amount
# }

# Arrow functions don't exist in GDScript. Use Callable for callbacks:
# const cb = () => print("hi")   var cb: Callable = func(): print("hi")

# Default parameters work the same:
func spawn_enemy(tier: int = 1, count: int = 1) -> void:
    for i in count:
        _create_enemy(tier)

# Return types are declared with ->
func get_damage() -> int:
    return 12 + level * 2

# void means no return value (like TypeScript's void)
func take_damage(amount: int) -> void:
    hp -= amount
```

#### Signals (unique to GDScript)

```gdscript
# Signals are declared at the top of the script.
# They are like custom events in JavaScript (EventEmitter pattern).

signal health_changed(new_hp: int, max_hp: int)
signal enemy_defeated(enemy: CharacterBody2D)
signal player_died

# Emitting a signal (like eventEmitter.emit())
func take_damage(amount: int) -> void:
    hp -= amount
    health_changed.emit(hp, max_hp)       # notify all listeners
    if hp <= 0:
        player_died.emit()

# Connecting to a signal (like addEventListener)
func _ready() -> void:
    # Connect in code:
    player.health_changed.connect(_on_health_changed)
    player.player_died.connect(_on_player_died)

func _on_health_changed(new_hp: int, max_hp: int) -> void:
    $HPLabel.text = "HP: %d / %d" % [new_hp, max_hp]

func _on_player_died() -> void:
    show_death_screen()

# Disconnecting (like removeEventListener)
player.health_changed.disconnect(_on_health_changed)
```

#### @onready and @export

```gdscript
# @onready — initializes the variable when _ready() is called.
# Without it, $NodePath would fail because child nodes aren't available yet.

# JavaScript equivalent: getting a DOM reference in DOMContentLoaded
# const label = document.getElementById("hp-label")

@onready var hp_label: Label = $UI/HUD/HPLabel
@onready var collision_shape: CollisionShape2D = $CollisionShape2D
@onready var attack_range: Area2D = $AttackRange

# @export — exposes the variable in the Godot Inspector panel.
# Like React props — set per-instance from the parent/editor.

@export var move_speed: float = 200.0       # editable in Inspector
@export var max_hp: int = 100               # editable in Inspector
@export_range(0.0, 1.0) var armor: float = 0.0  # slider in Inspector, 0-1 range
@export var enemy_color: Color = Color.RED  # color picker in Inspector
```

#### Control Flow

```gdscript
# if/elif/else (same as Python, not JS)
if hp <= 0:
    die()
elif hp < 20:
    flash_warning()
else:
    pass  # do nothing (like {} in JS)

# match (like switch, but more powerful — pattern matching)
match danger_tier:
    1:
        color = Color.GREEN
    2:
        color = Color.YELLOW
    3:
        color = Color.RED
    _:
        color = Color.WHITE  # default case (like switch default)
# No break needed — match does not fall through.

# for loops
for i in 10:                  # like: for (let i = 0; i < 10; i++)
    print(i)

for enemy in enemies:         # like: for (const enemy of enemies)
    enemy.update_ai()

for key in stats_dict:        # like: for (const key in obj)
    print(key, stats_dict[key])

# while loops (same concept)
while hp > 0:
    take_tick_damage()

# Ternary (conditional expression)
# JS:  const label = hp > 50 ? "healthy" : "injured"
var label: String = "healthy" if hp > 50 else "injured"
```

#### Data Structures

```gdscript
# Arrays (like JS arrays)
var enemies: Array = []
enemies.append(enemy)          # like push()
enemies.erase(enemy)           # like splice(indexOf(enemy), 1)
enemies.size()                 # like length
enemies[0]                     # same indexing
enemies.is_empty()             # like length === 0

# Dictionaries (like JS objects / Map)
var stats: Dictionary = {
    "hp": 100,
    "xp": 0,
    "level": 1
}
stats["hp"]                    # bracket access (like obj["key"])
stats.hp                       # dot access (like obj.key) — only for string keys
stats.get("hp", 0)             # with default value (like obj.hp ?? 0)

# String formatting
var text: String = "HP: %d / %d" % [hp, max_hp]       # printf-style
var text: String = "Level: " + str(level)              # concatenation
```

**Key differences to watch out for:**
- GDScript uses **indentation** for blocks (like Python), not braces.
- No semicolons. Ever.
- `null` is called `null` in GDScript (same keyword, actually).
- `==` does value comparison (no `===` needed — GDScript is strongly typed).
- `and` / `or` / `not` instead of `&&` / `||` / `!` (though `&&` `||` `!` also work).
- No hoisting. Variables must be declared before use.
- No destructuring. You can't do `var {hp, xp} = stats`.
- `print()` is the debug output (like `console.log()`). Output appears in the Godot Output panel.

---

### _ready() vs DOMContentLoaded

**What you already know:** In web dev, `DOMContentLoaded` fires when the HTML is parsed and the DOM tree is built. You use it to safely access DOM elements.

**The Godot equivalent:** `_ready()` fires when a node and **all its children** are added to the scene tree and initialized. You use it to safely access child nodes via `$`.

```gdscript
# The full node lifecycle:

# 1. _init() — Constructor. Called when the node is created in memory.
#    Like: class constructor in JS
#    DO: Initialize variables that don't depend on the scene tree.
#    DON'T: Access child nodes ($Child) — they don't exist yet.
func _init() -> void:
    hp = max_hp  # safe — just setting a variable

# 2. _enter_tree() — Called when the node is added to the scene tree.
#    Like: connectedCallback() in Web Components
#    The node is in the tree, but children may not be ready yet.
func _enter_tree() -> void:
    add_to_group("enemies")  # safe — node is in the tree

# 3. _ready() — Called after the node AND all its children are in the tree.
#    Like: DOMContentLoaded or React componentDidMount
#    This is where you do most initialization.
func _ready() -> void:
    $HPLabel.text = str(hp)              # safe — children are ready
    $AttackRange.body_entered.connect(_on_attack_range_entered)
    velocity = Vector2.ZERO

# 4. _process(delta) / _physics_process(delta) — Called every frame.
#    (Covered in the next section)

# 5. _exit_tree() — Called when the node is removed from the scene tree.
#    Like: disconnectedCallback() in Web Components or componentWillUnmount
#    Clean up connections, save state, etc.
func _exit_tree() -> void:
    # Clean up anything that persists beyond this node's lifetime
    pass
```

**Key differences to watch out for:**
- `_ready()` is called **bottom-up** — children's `_ready()` fires before their parent's `_ready()`. This guarantees that when your `_ready()` runs, all `$Child` references are valid.
- `_ready()` runs only **once** per node instance (unless the node is removed and re-added to the tree).
- `@onready var label: Label = $HPLabel` is syntactic sugar for setting the variable inside `_ready()`. It is identical to writing `var label: Label` then assigning it in `_ready()`.
- There is no equivalent of `window.onload` (waiting for all assets). Godot preloads resources at scene load time or uses `preload()` / `load()` in scripts.

---

### _process() / _physics_process() vs requestAnimationFrame

**What you already know:** `requestAnimationFrame(callback)` runs your callback before the next browser repaint, ~60fps. You use it for animations and game loops.

**The Godot equivalent:** Godot has TWO frame callbacks, and understanding the difference is critical:

```gdscript
# _process(delta) — runs every RENDER frame
# Like: requestAnimationFrame
# Frame rate varies (60fps, 144fps, etc.) depending on the monitor and GPU load.
# Use for: visual effects, animations, UI updates, camera smoothing
func _process(delta: float) -> void:
    # delta = seconds since last frame (e.g., 0.016 at 60fps)
    # Rotate a visual effect
    $SlashEffect.rotation += 5.0 * delta

# _physics_process(delta) — runs every PHYSICS frame
# Like: setInterval(callback, 1000/60) but built into the engine
# ALWAYS runs at a fixed rate: 60 times per second (configurable in project settings).
# delta is always ~0.01667 (1/60th of a second).
# Use for: movement, collision, AI, anything that affects gameplay.
func _physics_process(delta: float) -> void:
    # Handle input and movement
    var input_vector: Vector2 = Input.get_vector(
        "move_left", "move_right", "move_up", "move_down"
    )
    velocity = input_vector * move_speed
    move_and_slide()  # applies velocity, handles collision, uses delta internally
```

**Why two loops?** Consider a player on a 144fps monitor fighting a player on a 60fps monitor. If movement is in `_process()`, the 144fps player's code runs 2.4x more often. Even with delta time, floating-point accumulation causes tiny differences. `_physics_process()` runs at the same fixed rate for everyone, ensuring deterministic gameplay.

```gdscript
# Practical rule of thumb:
# _physics_process(delta) — player movement, enemy AI, combat logic, anything that
#                           affects game state or uses move_and_slide()
# _process(delta)         — particle effects, camera smoothing, UI animations,
#                           anything purely visual

# Example: a complete player movement script
extends CharacterBody2D

@export var move_speed: float = 200.0

func _physics_process(delta: float) -> void:
    var input_vector: Vector2 = Input.get_vector(
        "move_left", "move_right", "move_up", "move_down"
    )
    velocity = input_vector * move_speed
    move_and_slide()
```

**Key differences to watch out for:**
- `delta` in `_process()` varies frame-to-frame. `delta` in `_physics_process()` is (nearly) constant.
- `move_and_slide()` must be called inside `_physics_process()`. It uses the physics engine which runs on the physics tick.
- You do NOT multiply velocity by delta when using `move_and_slide()` — it handles delta internally. This is the biggest gotcha for web devs.
- You CAN put everything in `_physics_process()` for simplicity. The game won't break. Separating them is an optimization and correctness concern that matters more as the game grows.
- To disable processing: `set_process(false)` / `set_physics_process(false)`. Like pausing a `requestAnimationFrame` loop.

---

### Signals vs Events

**What you already know:** DOM events (`addEventListener`, `dispatchEvent`), Node.js EventEmitter (`on`, `emit`), or React callbacks (`onClick`, `onChange`).

**The Godot equivalent:** Signals — a built-in observer pattern. Every node can declare signals, emit them, and connect to other nodes' signals.

```gdscript
# --- Declaring signals (like defining custom event types) ---
signal health_changed(new_hp: int, max_hp: int)
signal died
signal level_up(new_level: int)

# --- Emitting signals (like dispatchEvent or eventEmitter.emit) ---
func take_damage(amount: int) -> void:
    hp -= amount
    health_changed.emit(hp, max_hp)
    if hp <= 0:
        died.emit()

# --- Connecting in code (like addEventListener) ---
func _ready() -> void:
    # Object.signal.connect(callable)
    $Player.health_changed.connect(_on_player_health_changed)
    $Player.died.connect(_on_player_died)

# --- Connecting in the editor ---
# In the Godot editor, select a node → Node tab (right panel) → Signals tab
# Double-click a signal → select the target node → Godot generates the callback.
# Editor-connected signals show a green icon in the script margin.

# --- Handler functions ---
# Convention: _on_<NodeName>_<signal_name>
func _on_player_health_changed(new_hp: int, max_hp: int) -> void:
    hp_bar.value = new_hp

func _on_player_died() -> void:
    show_death_screen()

# --- Disconnecting (like removeEventListener) ---
$Player.health_changed.disconnect(_on_player_health_changed)

# --- One-shot connection (like { once: true }) ---
$Player.died.connect(_on_player_died, CONNECT_ONE_SHOT)

# --- Built-in signals (like built-in DOM events) ---
# Area2D has: body_entered, body_exited, area_entered, area_exited
# Button has: pressed, toggled, mouse_entered, mouse_exited
# Timer has: timeout
# CharacterBody2D: no built-in signals (uses move_and_slide() return value)
# AnimationPlayer has: animation_finished

# --- await with signals (like JS await with Promises) ---
# Wait for a timer to finish (like await new Promise(resolve => setTimeout(resolve, 1000)))
await get_tree().create_timer(1.0).timeout
print("1 second has passed")

# Wait for a signal from another node
await $AnimationPlayer.animation_finished
print("animation done")
```

**Key differences to watch out for:**
- DOM events bubble up the tree. Godot signals do NOT bubble — they go directly from emitter to connected receiver.
- DOM events carry an Event object with properties. Godot signals carry typed arguments declared in the signal definition.
- You can connect signals in the **editor** (Node tab > Signals) or in **code**. Editor connections are stored in the `.tscn` file. Code connections are set up in `_ready()`.
- Convention: handler functions are named `_on_<SourceNode>_<signal_name>`. The leading underscore indicates it's a callback, not called directly.

---

### Resources (.tres) vs Assets

**What you already know:** In web dev, you have CSS files, JSON config files, image files, and font files. They are separate from your HTML/JS and loaded as needed.

**The Godot equivalent:** Resources (`.tres` files) are Godot's configuration/data objects. They store data that nodes use — like a TileSet defining your tile grid, a Theme defining your UI colors, or a StyleBox defining a panel's appearance.

```gdscript
# Common resource types:

# TileSet (.tres) — defines tiles, their textures, physics shapes, and properties
# Like: a CSS sprite sheet definition + collision map

# Theme (.tres) — defines UI styling (fonts, colors, margins, StyleBoxes)
# Like: a CSS stylesheet

# StyleBox (.tres) — defines how a UI panel/button looks (background, border, padding)
# Like: a single CSS rule for a component

# PackedScene (.tscn) — a scene IS a resource (can be loaded and instantiated)
# Like: a component module

# Loading resources in code:
var tileset: TileSet = preload("res://resources/tile_sets/dungeon_tileset.tres")
var theme: Theme = preload("res://resources/themes/game_theme.tres")

# Creating resources in code (rare, usually done in editor):
var style: StyleBoxFlat = StyleBoxFlat.new()
style.bg_color = Color(0.1, 0.12, 0.18, 0.85)
style.border_color = Color(0.96, 0.78, 0.42, 0.3)
style.set_border_width_all(1)
style.set_corner_radius_all(10)
$Panel.add_theme_stylebox_override("panel", style)
```

**The `res://` and `user://` paths:**

```gdscript
# res:// — project directory (read-only in exported builds)
# Like: the /public or /static directory in a web project
# Everything in your Godot project folder is accessible via res://
var scene = preload("res://scenes/player/player.tscn")
var tileset = load("res://resources/tile_sets/dungeon_tileset.tres")

# user:// — user data directory (read/write, persists across sessions)
# Like: localStorage or IndexedDB
# Platform-specific location:
#   macOS: ~/Library/Application Support/Godot/app_userdata/<ProjectName>/
#   Windows: %APPDATA%/Godot/app_userdata/<ProjectName>/
#   Linux: ~/.local/share/godot/app_userdata/<ProjectName>/
var save_file = FileAccess.open("user://save.json", FileAccess.WRITE)
```

**Key differences to watch out for:**
- Web assets are loaded over HTTP. Godot resources are loaded from the local file system (or embedded in the export binary).
- CSS is applied globally by selectors. Godot Themes are applied to a Control node and inherited by its children. You can override individual properties per-node.
- Resources can be shared between nodes. If two enemies reference the same TileSet resource, they share one instance in memory (like a cached CSS file).

---

### Inspector vs CSS

**What you already know:** In web dev, you style elements with CSS properties (`color`, `font-size`, `padding`, `background`). You inspect and modify them in browser DevTools.

**The Godot equivalent:** In Godot, you configure node properties in the **Inspector** panel (right side of the editor). Position, color, speed, collision shapes — everything is a property you can see and edit visually.

```gdscript
# Properties you set in the Inspector can also be set in code:

# Position (like CSS top/left with position: absolute)
position = Vector2(500, 300)

# Scale (like CSS transform: scale())
scale = Vector2(2.0, 2.0)

# Rotation (like CSS transform: rotate(), but in radians)
rotation = PI / 4  # 45 degrees

# Visibility (like CSS display: none / visibility: hidden)
visible = false    # hides the node and all children

# Modulate (like CSS color/opacity — tints the node)
modulate = Color(1, 0, 0, 0.5)  # red tint at 50% opacity

# Z-index (like CSS z-index — draw order)
z_index = 10  # higher = drawn on top

# @export makes YOUR custom properties appear in the Inspector:
@export var move_speed: float = 200.0   # appears as a number field
@export var max_hp: int = 100           # appears as an integer field
@export var enemy_color: Color = Color.RED    # appears as a color picker
@export var enemy_name: String = "Goblin"     # appears as a text field
@export_range(1, 3) var danger_tier: int = 1  # appears as a slider, 1-3
@export_enum("Melee", "Ranged", "Magic") var attack_type: int = 0  # dropdown
```

**Key differences to watch out for:**
- CSS is declarative (you say what it should look like and the browser figures out how). Godot properties are imperative (you set exact values and the engine uses them directly).
- CSS has cascading and inheritance. Godot Inspector properties do NOT cascade — each node's properties are independent. The exception is Theme, which does inherit down the Control node tree.
- Browser DevTools let you live-edit CSS. Godot's Inspector lets you live-edit properties while the game is running (in the Remote tab), but changes are lost when you stop the game unless you use "Save Branch as Scene."

---

### Input System vs Event Listeners

**What you already know:** `document.addEventListener("keydown", handler)`, `event.key`, `event.preventDefault()`. You listen for specific key events and respond.

**The Godot equivalent:** Godot uses an **Input Map** — named actions that can be bound to multiple keys/buttons. You check action state rather than raw keys.

```gdscript
# Step 1: Define actions in Input Map (Project → Project Settings → Input Map)
# Action "move_left"  → bound to: A key, Left Arrow key, Gamepad Left
# Action "move_right" → bound to: D key, Right Arrow key, Gamepad Right
# Action "move_up"    → bound to: W key, Up Arrow key, Gamepad Up
# Action "move_down"  → bound to: S key, Down Arrow key, Gamepad Down

# Step 2: Check actions in code

# Method 1: Polling (check every frame in _physics_process)
# Like: checking a "keys currently pressed" set each animation frame
func _physics_process(delta: float) -> void:
    # Input.get_vector() returns a normalized Vector2 from 4 directional actions
    # Like: building a direction vector from WASD key states
    var input_vector: Vector2 = Input.get_vector(
        "move_left", "move_right", "move_up", "move_down"
    )
    velocity = input_vector * move_speed
    move_and_slide()

# Method 2: Event-based (respond to individual input events)
# Like: addEventListener("keydown", handler)
func _input(event: InputEvent) -> void:
    # Handles ALL input events (keyboard, mouse, gamepad)
    if event.is_action_pressed("restart"):
        get_tree().reload_current_scene()

func _unhandled_input(event: InputEvent) -> void:
    # Like _input(), but only receives events not consumed by UI.
    # Use this for game-world input so that clicking a UI button
    # doesn't also trigger a game action.
    if event.is_action_pressed("attack"):
        perform_attack()

# Individual checks:
Input.is_action_pressed("move_left")        # is the key currently held? (like keydown state)
Input.is_action_just_pressed("attack")      # was the key pressed THIS frame? (like keydown event)
Input.is_action_just_released("attack")     # was the key released THIS frame? (like keyup event)
Input.get_action_strength("move_right")     # 0.0 to 1.0 (for analog stick support)
```

**Key differences to watch out for:**
- Web: you listen for specific keys (`event.key === "ArrowLeft"`). Godot: you check named actions (`Input.is_action_pressed("move_left")`). This means rebinding keys is free — change the Input Map, code stays the same.
- Web: events bubble and can be stopped with `stopPropagation()`. Godot: `_input()` receives events first, then `_unhandled_input()`. You can "consume" an event with `get_viewport().set_input_as_handled()`.
- `_input()` vs `_unhandled_input()`: Use `_unhandled_input()` for gameplay input. UI Control nodes (Button, etc.) consume their events in `_input()`, so `_unhandled_input()` only fires if no UI element handled it. This prevents clicking a UI button from also triggering a game action.

---

### Autoloads vs Global Variables

**What you already know:** In JavaScript, you might have a global state object (`window.gameState`), a module-level singleton (`export const store = new Store()`), or a React Context provider wrapping the app.

**The Godot equivalent:** **Autoloads** — scripts (or scenes) that Godot loads automatically when the game starts and keeps alive for the entire session. They are accessible from any script by name.

```gdscript
# Step 1: Create the autoload script
# --- scripts/autoloads/game_state.gd ---
extends Node

# Signals for reactive updates (like a store's subscription)
signal stats_changed
signal player_died
signal floor_changed(new_floor: int)

# State variables
var hp: int = 100:
    set(value):
        hp = value
        stats_changed.emit()    # auto-notify on change
var max_hp: int = 100
var xp: int = 0
var level: int = 1
var floor_number: int = 1

func reset() -> void:
    hp = 100
    max_hp = 100
    xp = 0
    level = 1
    floor_number = 1

# Step 2: Register as Autoload
# Project → Project Settings → Autoload tab
# Path: res://scripts/autoloads/game_state.gd
# Name: GameState (this becomes the global access name)

# Step 3: Access from ANY script, anywhere
# --- scenes/ui/hud.gd ---
extends Control

@onready var hp_label: Label = $HPLabel

func _ready() -> void:
    # Access the autoload by its registered name — no import needed
    GameState.stats_changed.connect(_update_display)
    _update_display()

func _update_display() -> void:
    hp_label.text = "HP: %d / %d" % [GameState.hp, GameState.max_hp]
```

**Key differences to watch out for:**
- JavaScript modules use `import`/`require`. GDScript autoloads require NO import — they are available as global names everywhere.
- Autoloads are **always in the tree**. When you change scenes (`get_tree().change_scene_to_file()`), autoloads persist. They live above the scene tree root.
- Autoloads are processed in order. If `GameState` depends on `EventBus`, register `EventBus` first in the Autoload list.
- Don't overuse autoloads. Only truly global state belongs there (game state, event bus, audio manager). Scene-specific logic belongs in scene scripts.

---

### TileMapLayer vs Canvas Drawing

**What you already know:** In the Phaser prototype, the background is drawn with `this.add.graphics()` — procedurally drawing grid lines on a canvas. In web dev, you might draw on a `<canvas>` element with `ctx.fillRect()`.

**The Godot equivalent:** **TileMapLayer** — a node that efficiently renders a grid of tiles. You define tile types in a **TileSet** resource, then paint tiles onto the map in the editor or set them in code.

```gdscript
# TileSet resource (dungeon_tileset.tres) defines:
# - Tile size: 64x32 pixels (isometric diamond)
# - Tile shape: Diamond (isometric)
# - Tile layout: Diamond Down
# - Tile offset axis: Horizontal
# - Physics layers: wall tiles have CollisionPolygon, floor tiles don't
# - Each tile has an ID (atlas coords) used to place it

# TileMapLayer node configuration (in the scene):
# - Assigned TileSet: dungeon_tileset.tres
# - Y-sort enabled: true (tiles drawn in correct depth order for isometric)

# Placing tiles in code:
func generate_floor() -> void:
    var tilemap: TileMapLayer = $TileMapLayer

    # Clear existing tiles
    tilemap.clear()

    # Set a floor tile at grid position (5, 3)
    # Parameters: coords, source_id, atlas_coords, alternative_tile
    tilemap.set_cell(Vector2i(5, 3), 0, Vector2i(0, 0))  # floor tile

    # Set a wall tile at grid position (5, 0)
    tilemap.set_cell(Vector2i(5, 0), 0, Vector2i(1, 0))  # wall tile

    # Check what tile is at a position
    var tile_coords: Vector2i = tilemap.get_cell_atlas_coords(Vector2i(5, 3))

    # Convert between world coordinates and tile coordinates
    var world_pos: Vector2 = tilemap.map_to_local(Vector2i(5, 3))  # grid → world
    var grid_pos: Vector2i = tilemap.local_to_map(Vector2(320, 96)) # world → grid

# Generate a simple room:
func generate_room(width: int, height: int) -> void:
    var tilemap: TileMapLayer = $TileMapLayer
    for x in width:
        for y in height:
            if x == 0 or x == width - 1 or y == 0 or y == height - 1:
                # Wall tile on edges
                tilemap.set_cell(Vector2i(x, y), 0, Vector2i(1, 0))
            else:
                # Floor tile inside
                tilemap.set_cell(Vector2i(x, y), 0, Vector2i(0, 0))
```

**Key differences to watch out for:**
- Canvas drawing is immediate mode (draw every frame). TileMapLayer is retained mode (set tiles once, engine renders them every frame automatically). You only call `set_cell()` when the map changes, not every frame.
- TileMaps handle isometric coordinate conversion automatically. In canvas drawing, you'd need to calculate diamond positions manually.
- Physics collision for walls is defined in the TileSet resource — each tile type can have its own collision polygon. No need to create separate physics bodies for every wall tile.
- TileMaps are dramatically more performant than drawing individual shapes. The engine batches all tiles into a single draw call.

---

### Camera2D vs Viewport

**What you already know:** In web dev, the browser viewport shows a portion of the page. You scroll to see different parts. CSS `scroll-behavior: smooth` adds smooth scrolling. In the Phaser prototype, the camera is fixed (the entire game world fits on screen).

**The Godot equivalent:** **Camera2D** — a node that controls which part of the 2D world is visible. It follows the player, can zoom in/out, and has limits to prevent showing areas outside the map.

```gdscript
# Camera2D is typically a child of the player node:
# Player (CharacterBody2D)
#   ├── Polygon2D
#   ├── CollisionShape2D
#   └── Camera2D            ← follows player automatically because it's a child

# Camera configuration (set in Inspector or code):
extends Camera2D

func _ready() -> void:
    # Make this the active camera
    make_current()

    # Smooth follow (like CSS scroll-behavior: smooth)
    position_smoothing_enabled = true
    position_smoothing_speed = 8.0      # higher = snappier follow

    # Zoom level (1.0 = normal, 2.0 = zoomed in 2x, 0.5 = zoomed out 2x)
    zoom = Vector2(2.0, 2.0)

    # Camera limits (prevent seeing beyond the map)
    limit_left = 0
    limit_top = 0
    limit_right = 3200      # map width in pixels
    limit_bottom = 1800      # map height in pixels
    limit_smoothed = true    # smooth when hitting limits

    # Drag margins (camera moves only when player is near viewport edge)
    drag_horizontal_enabled = true
    drag_vertical_enabled = true
    drag_left_margin = 0.2   # camera starts moving when player is in outer 20%
    drag_right_margin = 0.2
    drag_top_margin = 0.2
    drag_bottom_margin = 0.2

# Camera shake (like the Phaser this.cameras.main.shake() in our prototype):
func shake(duration: float = 0.15, strength: float = 4.0) -> void:
    var tween: Tween = create_tween()
    for i in int(duration / 0.03):
        tween.tween_property(self, "offset",
            Vector2(randf_range(-strength, strength), randf_range(-strength, strength)),
            0.03)
    tween.tween_property(self, "offset", Vector2.ZERO, 0.03)
```

**Key differences to watch out for:**
- In web dev, scrolling moves the viewport over the content. In Godot, Camera2D moves through the world — the world stays still, the camera moves.
- Only one Camera2D can be "current" (active) at a time per viewport. Setting `make_current()` on one camera deactivates the previous one.
- Camera2D only affects Node2D nodes. **Control nodes (UI) are NOT affected by Camera2D.** This is by design — HUD elements stay in place while the world scrolls. To achieve this, put UI in a CanvasLayer.
- `zoom = Vector2(2.0, 2.0)` means zoomed IN (objects appear larger). This is the opposite of what you might expect. Think of it as "2x magnification."

---

### Groups vs CSS Classes

**What you already know:** CSS classes let you tag elements and select them collectively. `.enemy { color: red; }` styles all elements with class "enemy". `document.querySelectorAll(".enemy")` gets them all.

**The Godot equivalent:** **Groups** — a tagging system for nodes. You add nodes to named groups and query them.

```gdscript
# Adding to a group (like element.classList.add("enemy"))
func _ready() -> void:
    add_to_group("enemies")
    add_to_group("damageable")

# Or add in the editor: select node → Node tab → Groups tab → type name → Add

# Checking group membership (like element.classList.contains("enemy"))
if node.is_in_group("enemies"):
    node.take_damage(10)

# Getting all nodes in a group (like document.querySelectorAll(".enemies"))
var all_enemies: Array[Node] = get_tree().get_nodes_in_group("enemies")
for enemy in all_enemies:
    enemy.queue_free()  # destroy all enemies

# Calling a method on all nodes in a group (no web equivalent — batch operation)
get_tree().call_group("enemies", "freeze")  # calls freeze() on every enemy

# Removing from a group (like element.classList.remove("enemy"))
remove_from_group("enemies")
```

**Key differences to watch out for:**
- CSS classes affect styling. Godot groups do NOT affect appearance — they are purely for identification and batch operations.
- CSS classes are defined in stylesheets. Godot groups are defined per-node instance, either in code or in the editor. There is no "group definition file."
- `get_tree().get_nodes_in_group()` returns nodes in **tree order** (top to bottom). This is deterministic.
- Groups are commonly used for collision filtering: "Is the body that entered my Area2D in the 'enemies' group?"

---

### Coordinate System

**What you already know:** In CSS and Canvas, (0,0) is the top-left corner. X increases rightward, Y increases downward. This is the same in Godot.

**The Godot equivalent:** Identical for 2D — Y-down coordinate system. But isometric rendering adds a transformation layer.

```gdscript
# Standard 2D coordinates (same as web):
# (0,0) ─── X+ →
#   │
#   Y+
#   ↓

# A node at position Vector2(100, 200) is 100px right, 200px down from its parent.

# Isometric adds complexity:
# In isometric view, the grid is rotated 45 degrees and squashed vertically.
# A 64x32 diamond tile means:
#   - Tile width (horizontal diagonal): 64 pixels
#   - Tile height (vertical diagonal): 32 pixels
#   - This gives the classic 2:1 isometric ratio

# Grid coordinates (integer, logical) vs World coordinates (pixel, visual):
# Grid (0,0) maps to the top-center of the map
# Grid (1,0) is one tile to the lower-right
# Grid (0,1) is one tile to the lower-left

# Converting between grid and world coordinates:
var tilemap: TileMapLayer = $TileMapLayer

# Grid → World (where to place a sprite on the screen for grid cell 3,5)
var world_pos: Vector2 = tilemap.map_to_local(Vector2i(3, 5))

# World → Grid (which grid cell did the player click on)
var grid_pos: Vector2i = tilemap.local_to_map(get_global_mouse_position())

# Important: "local" in map_to_local means the TileMapLayer's local coordinate space.
# If the TileMapLayer is at (0,0) in the scene, local = global.

# Isometric sorting (draw order):
# In isometric view, objects closer to the "camera" (lower on screen) must be drawn
# on top of objects further away. Godot handles this with:
# - TileMapLayer: y_sort_enabled = true (tiles sort automatically)
# - Parent Node2D: y_sort_enabled = true (child nodes sort by Y position)
# - Individual nodes: z_index for manual override
```

**Key differences to watch out for:**
- In a flat 2D game, screen coordinates = world coordinates. In isometric, they diverge — a tile at grid (5, 3) is NOT at pixel (5, 3). Always use `map_to_local` / `local_to_map` for conversion.
- Y-sorting is essential for isometric. Without it, a character walking behind a wall might be drawn in front of it. Enable `y_sort_enabled` on the parent container of all entities.
- Rotation in Godot is in **radians**, not degrees. `PI` = 180 degrees. Use `deg_to_rad()` and `rad_to_deg()` for conversion.

## Open Questions

- Should this document cover Godot's shader language (for visual effects) or is that too advanced for a basics doc?
- Would animated GIFs or screenshots from the Godot editor help illustrate the scene tree and Inspector concepts?
- Should we add a "common mistakes" section with GDScript pitfalls specific to JavaScript developers?
- Is there value in documenting GDScript static typing more deeply (typed arrays, custom classes, enums)?
