# Game Dev Concepts for Web Developers

## Summary

A bridge from web development to game development, mapping familiar concepts to their Godot 4 / GDScript equivalents. Every concept includes a web analogy, a GDScript code example, and key differences.

## Current State

Understanding these concepts will help make sense of the Godot 4 codebase. The game is being migrated from a Phaser 3 browser prototype to native Godot 4 — see `archive/phaser-prototype/index.html` for the original implementation of each concept.

## Design

### The Game Loop

**Web analogy:** `requestAnimationFrame` — but managed by Godot, split into two separate loops.

In web development, the browser renders the DOM and you respond to events. In game development, you have a **game loop** that runs every frame. Each frame:

1. **Process input** — check what keys are pressed, read action states
2. **Update state** — move objects, run AI, check collisions, apply damage
3. **Render** — Godot draws everything automatically (you don't call a render function)

Godot provides TWO game loop callbacks. Understanding when to use each is critical:

```gdscript
# _physics_process(delta) — runs at a FIXED rate (60 times per second, always)
# Use for: movement, collision, AI, combat — anything that affects gameplay
# This is where most game logic goes.
# Replaces: Phaser's update(time, delta) for gameplay logic
func _physics_process(delta: float) -> void:
    handle_movement()
    handle_enemy_ai()
    try_auto_attack()

# _process(delta) — runs every RENDER frame (varies: 60fps, 144fps, etc.)
# Use for: visual effects, animations, UI updates, camera smoothing
# Replaces: Phaser's update(time, delta) for visual-only concerns
func _process(delta: float) -> void:
    update_slash_effect()
    smooth_camera()
```

**Why two loops instead of one?** In the Phaser prototype, `update(time, delta)` handled everything — movement, AI, combat, and visuals — all in one callback that ran at the browser's frame rate. This means a player on a 144Hz monitor would have their game loop run 2.4x more often than a player on a 60Hz monitor. Even with delta time compensation, floating-point accumulation causes tiny gameplay differences.

Godot's `_physics_process()` runs at a **fixed** 60fps regardless of monitor refresh rate. This makes gameplay deterministic — the same inputs produce the same results on every machine. `_process()` runs at the display's refresh rate for smooth visuals.

**Migration mapping from Phaser prototype:**

| Phaser (old) | Godot (new) | Why it moved |
|-------------|-------------|-------------|
| `update()` → `handleMovement()` | `_physics_process()` → `handle_movement()` | Movement uses physics (move_and_slide) |
| `update()` → `handleEnemyAI()` | `_physics_process()` → `handle_enemy_ai()` | AI chase uses velocity/physics |
| `update()` → `tryAutoAttack(time)` | `_physics_process()` → `try_auto_attack()` | Combat timing must be deterministic |
| `create()` → setup code | `_ready()` → setup code | One-time initialization |

---

### Delta Time

**Web analogy:** The timestamp parameter in `requestAnimationFrame`, used to make animations frame-rate-independent.

Frames don't always take the same amount of time. If you move a character 5 pixels per frame, they'll move faster on a 120fps monitor than a 60fps one. **Delta time** tells you how many seconds elapsed since the last frame. Use it to make movement frame-independent:

```gdscript
# Bad: speed depends on frame rate
position.x += 5

# Good: consistent speed regardless of frame rate
position.x += 300.0 * delta  # 300 pixels per second

# Best (for CharacterBody2D): use velocity + move_and_slide()
# move_and_slide() handles delta internally — do NOT multiply by delta yourself
velocity = direction * move_speed  # move_speed is in pixels per second
move_and_slide()                   # applies velocity, handles delta, resolves collisions
```

**The biggest gotcha for web developers:** When using `move_and_slide()` (which you almost always should for CharacterBody2D), do NOT multiply by delta. The function handles it internally. This is different from the Phaser prototype where `body.setVelocity()` also handled delta internally via the Arcade physics engine, but the principle was less visible.

```gdscript
# WRONG — double-applies delta, character moves in slow motion
velocity = direction * move_speed * delta
move_and_slide()

# RIGHT — move_and_slide applies delta for you
velocity = direction * move_speed
move_and_slide()

# ALSO RIGHT — manual position update when NOT using move_and_slide
# (rare, used for non-physics objects like visual effects)
position += direction * move_speed * delta
```

**Delta values:**
- In `_physics_process(delta)`: delta is ~0.01667 seconds (1/60th), nearly constant
- In `_process(delta)`: delta varies per frame (0.0069 at 144fps, 0.0167 at 60fps, higher if frame drops)

---

### Sprites vs DOM Elements

**Web analogy:** There are no DOM elements in the game world. Everything is a **node in the scene tree**, rendered by the engine.

In web dev, everything is an HTML element positioned by CSS. In Godot, everything is a **Node** with x/y coordinates, rendered by the engine each frame. You can't inspect them in browser DevTools — but you CAN inspect them in the Godot editor's Remote scene tree (while the game is running).

```gdscript
# Phaser prototype used:
# this.add.circle(x, y, radius, color)     → colored circle on canvas
# this.add.rectangle(x, y, w, h, color)    → colored rectangle on canvas

# Godot equivalents:

# Option 1: Polygon2D — draw a colored shape (our current approach)
# Like the Phaser circles, but defined as a polygon with vertices
var polygon: Polygon2D = Polygon2D.new()
polygon.polygon = PackedVector2Array([
    Vector2(0, -12), Vector2(12, 0), Vector2(0, 12), Vector2(-12, 0)
])  # diamond shape
polygon.color = Color(0.56, 0.84, 1.0)  # light blue, like COLORS.player
add_child(polygon)

# Option 2: Sprite2D — display a texture/image (future, when art exists)
var sprite: Sprite2D = Sprite2D.new()
sprite.texture = preload("res://assets/sprites/player.png")
sprite.position = Vector2(100, 200)
add_child(sprite)

# Option 3: ColorRect — a colored rectangle (for UI elements, not game world)
var rect: ColorRect = ColorRect.new()
rect.color = Color(0.0, 0.0, 0.0, 0.75)
rect.size = Vector2(380, 160)
add_child(rect)
```

**Critical distinction — Node2D vs Control:**

```gdscript
# Node2D and its children (Sprite2D, Polygon2D, CharacterBody2D, Area2D):
# → Used for GAME WORLD objects (player, enemies, tiles, effects)
# → Positioned by x/y coordinates (position = Vector2(100, 200))
# → Affected by Camera2D (scroll with the world)
# → Not affected by UI layout containers
# → Like: absolutely positioned canvas drawings

# Control and its children (Label, Button, Panel, TextureRect):
# → Used for UI elements (HUD, menus, death screen)
# → Positioned by anchors, margins, and layout containers
# → NOT affected by Camera2D (stay fixed on screen)
# → Affected by Theme resources for styling
# → Like: HTML elements with CSS layout

# NEVER put a Control node as a child of a Node2D expecting it to move with the camera.
# NEVER put a Node2D inside a VBoxContainer expecting it to follow UI layout.
```

---

### Collision Detection

**Web analogy:** No real equivalent. Maybe `IntersectionObserver`, but much more precise and physics-based.

The Phaser prototype used `this.physics.add.overlap(player, enemies, onPlayerHit)` — a single callback for when player and enemy circles intersected. Godot uses a fundamentally different approach: **collision shapes** on **physics bodies** and **areas**, with **signals** for notification.

```gdscript
# Godot collision system has three key pieces:

# 1. CollisionShape2D — defines the SHAPE of the collider
#    Attached as a child of a physics body or area.
#    Shape types: CircleShape2D, RectangleShape2D, CapsuleShape2D, ConvexPolygonShape2D

# 2. Physics bodies — objects that participate in collision RESOLUTION
#    CharacterBody2D: moves with code control, collides and slides (player, enemies)
#    StaticBody2D: doesn't move, blocks other bodies (walls)
#    RigidBody2D: moves with physics simulation (thrown objects, ragdolls)

# 3. Area2D — objects that DETECT overlaps but don't physically collide
#    Like: invisible trigger zones. "Something entered my space."
#    Used for: attack range detection, item pickups, room transitions

# --- Setting up collision (replaces Phaser's physics.add.overlap) ---

# In the player scene (player.tscn):
# Player (CharacterBody2D)          ← moves and collides with walls/enemies
#   ├── CollisionShape2D             ← player's physical body (circle, radius 12)
#   └── AttackRange (Area2D)         ← invisible detection zone for auto-attack
#       └── CollisionShape2D         ← attack range shape (circle, radius 78)

# In player.gd:
func _ready() -> void:
    # Connect the Area2D signal (like addEventListener)
    $AttackRange.body_entered.connect(_on_attack_range_body_entered)
    $AttackRange.body_exited.connect(_on_attack_range_body_exited)

var enemies_in_range: Array[CharacterBody2D] = []

func _on_attack_range_body_entered(body: Node2D) -> void:
    if body.is_in_group("enemies"):
        enemies_in_range.append(body)

func _on_attack_range_body_exited(body: Node2D) -> void:
    enemies_in_range.erase(body)
```

**Collision layers and masks** (no Phaser equivalent — this is new):

```gdscript
# Godot has 32 collision layers. Each physics body/area exists on layers
# and scans other layers. This replaces Phaser's simple "group A overlaps group B."

# Layer setup (in Project Settings → 2D Physics → Layer Names):
# Layer 1: "player"       — the player's body
# Layer 2: "enemies"      — enemy bodies
# Layer 3: "walls"        — static wall collision
# Layer 4: "player_attack"— player's attack detection area
# Layer 5: "enemy_attack" — enemy damage detection area

# Player CharacterBody2D:
#   Collision Layer: 1 (player)        — "I exist on layer 1"
#   Collision Mask: 2, 3 (enemies, walls) — "I collide with layers 2 and 3"

# Enemy CharacterBody2D:
#   Collision Layer: 2 (enemies)       — "I exist on layer 2"
#   Collision Mask: 1, 3 (player, walls) — "I collide with layers 1 and 3"

# Player AttackRange Area2D:
#   Collision Layer: 4 (player_attack) — "I exist on layer 4"
#   Collision Mask: 2 (enemies)        — "I detect things on layer 2"

# This means:
# ✓ Player collides with enemies and walls
# ✓ Enemies collide with player and walls
# ✓ Player attack range detects enemies
# ✗ Enemies don't collide with each other (no friendly fire)
# ✗ Player attack range doesn't detect the player itself
```

**Migration mapping from Phaser prototype:**

| Phaser (old) | Godot (new) |
|-------------|-------------|
| `physics.add.overlap(player, enemies, onPlayerHit)` | Enemy Area2D `body_entered` signal → `_on_body_entered()` |
| `physics.moveToObject(enemy, player, speed)` | `velocity = position.direction_to(target) * speed` + `move_and_slide()` |
| `player.body.setMaxVelocity(220, 220)` | Not needed — CharacterBody2D velocity is set directly |
| `player.setCollideWorldBounds(true)` | Camera2D limits + physics walls at map edges |
| `physics.world.setBounds(0, 0, w, h)` | StaticBody2D walls at map edges or TileMap wall tiles |

---

### State Management

**Web analogy:** React state or a Vuex/Pinia store — but using Godot's Autoload singleton pattern with signal-based reactivity.

The Phaser prototype used a plain JavaScript object:
```js
// Old Phaser approach:
const state = { hp: 100, xp: 0, level: 1 };
// Mutated directly, HUD updated by manually calling updateHud()
```

Godot uses an **Autoload singleton** with **property setters that emit signals** — similar to a reactive store:

```gdscript
# --- scripts/autoloads/game_state.gd ---
# Registered as Autoload "GameState" in Project Settings
extends Node

# Signals — listeners subscribe to these for reactive updates
signal stats_changed
signal player_died
signal floor_changed(new_floor: int)

# State with reactive setters — changing a value auto-emits a signal
var hp: int = 100:
    set(value):
        hp = clampi(value, 0, max_hp)
        stats_changed.emit()
        if hp <= 0:
            player_died.emit()

var max_hp: int = 100:
    set(value):
        max_hp = value
        stats_changed.emit()

var xp: int = 0:
    set(value):
        xp = value
        _check_level_up()
        stats_changed.emit()

var level: int = 1:
    set(value):
        level = value
        stats_changed.emit()

var floor_number: int = 1:
    set(value):
        floor_number = value
        floor_changed.emit(floor_number)
        stats_changed.emit()

func _check_level_up() -> void:
    var xp_to_level: int = level * 90
    if xp >= xp_to_level:
        xp -= xp_to_level  # triggers setter recursion (safe, xp decreases)
        level += 1
        hp = mini(max_hp, hp + 18)

func reset() -> void:
    hp = 100
    max_hp = 100
    xp = 0
    level = 1
    floor_number = 1


# --- scenes/ui/hud.gd ---
# The HUD listens to state changes — no manual updateHud() calls needed
extends Control

@onready var hp_label: Label = $VBoxContainer/HPLabel
@onready var xp_label: Label = $VBoxContainer/XPLabel
@onready var level_label: Label = $VBoxContainer/LevelLabel
@onready var floor_label: Label = $VBoxContainer/FloorLabel

func _ready() -> void:
    GameState.stats_changed.connect(_update_display)
    _update_display()  # initial render

func _update_display() -> void:
    hp_label.text = "HP: %d / %d" % [GameState.hp, GameState.max_hp]
    xp_label.text = "XP: %d" % GameState.xp
    level_label.text = "LVL: %d" % GameState.level
    floor_label.text = "Floor: %d" % GameState.floor_number
```

**Key difference from the Phaser prototype:** In the prototype, `updateHud()` was called manually after every state change (in `defeatEnemy`, `onPlayerHit`, `gameOver`). If you forgot to call it, the HUD showed stale data. In Godot, the signal-based approach makes it **impossible** to forget — changing `GameState.hp` automatically triggers `stats_changed`, which automatically updates the HUD.

---

### Scenes

**Web analogy:** Routes in a single-page app (React Router, Vue Router) — each "page" has its own component tree, and you navigate between them.

The Phaser prototype had a single `DungeonScene` class with `create()` and `update()` methods, and `scene.restart()` for restarting. Godot scenes are fundamentally different — they are **files on disk** (`.tscn`) that you edit visually in the editor.

```gdscript
# --- Key scene operations ---

# Loading a scene (like importing a component)
const EnemyScene: PackedScene = preload("res://scenes/enemies/enemy.tscn")

# Instantiating a scene (like rendering a component)
var enemy: CharacterBody2D = EnemyScene.instantiate()
enemy.position = Vector2(500, 300)
add_child(enemy)  # adds to the scene tree, triggers _ready()

# Removing a scene instance (like unmounting a component)
enemy.queue_free()  # safe removal at end of frame

# Switching scenes entirely (like route navigation)
get_tree().change_scene_to_file("res://scenes/dungeon/dungeon.tscn")

# Restarting the current scene (replaces Phaser's scene.restart())
get_tree().reload_current_scene()

# Pausing the scene tree (like disabling requestAnimationFrame)
get_tree().paused = true   # everything stops
get_tree().paused = false  # everything resumes
# Individual nodes can opt out of pausing via process_mode property:
# Node.PROCESS_MODE_ALWAYS — keeps running when paused (for pause menu UI)
```

**Scene transitions in our game:**

```gdscript
# The main.tscn scene manages transitions:
# Main (Node2D)
#   ├── CurrentScene (Node2D)    ← dungeon or town scene loaded here
#   └── UILayer (CanvasLayer)    ← HUD stays across scene changes
#       └── HUD (instanced)

# --- scenes/main.gd ---
extends Node2D

func change_to_dungeon() -> void:
    _change_scene("res://scenes/dungeon/dungeon.tscn")

func change_to_town() -> void:
    _change_scene("res://scenes/world/town.tscn")

func _change_scene(scene_path: String) -> void:
    # Remove current scene
    for child in $CurrentScene.get_children():
        child.queue_free()

    # Load and add new scene
    var new_scene: PackedScene = load(scene_path)
    var instance: Node = new_scene.instantiate()
    $CurrentScene.add_child(instance)
```

**Migration mapping from Phaser prototype:**

| Phaser (old) | Godot (new) |
|-------------|-------------|
| `class DungeonScene extends Phaser.Scene` | `dungeon.tscn` + `dungeon.gd` (extends Node2D) |
| `constructor() { super("dungeon") }` | Scene name is the file name |
| `create()` | `_ready()` |
| `update(time, delta)` | `_physics_process(delta)` |
| `this.scene.restart()` | `get_tree().reload_current_scene()` |
| `this.scene.start("town")` | `get_tree().change_scene_to_file("res://scenes/world/town.tscn")` |

---

### Coordinate System

**Web analogy:** Same as CSS `position: absolute` — (0,0) is top-left, Y increases downward.

The Phaser prototype used a fixed 1100x700 pixel game world with a grid background. Godot's coordinate system is identical in principle but adds **isometric transformation** for the tile-based dungeon.

```gdscript
# Standard 2D coordinates (same as web):
# (0,0) is top-left
# X increases rightward →
# Y increases downward ↓
# This is identical to CSS absolute positioning and Canvas 2D.

# Our Phaser prototype:
# - Game world: 1100 x 700 pixels, fixed size
# - Player spawned at (550, 350) — center of the world
# - Enemy spawned at edges (x=10, y=10, etc.)
# - All coordinates were screen pixels

# Our Godot game:
# - Game world: much larger, scrolled by Camera2D
# - Viewport: 1920 x 1080 (what's visible on screen)
# - The TileMapLayer uses isometric coordinates

# Isometric coordinate conversion:
# In isometric view, the grid is rotated 45° and squashed vertically.
# Grid position (5, 3) does NOT correspond to pixel position (5, 3).
# The TileMapLayer handles conversion:

var tilemap: TileMapLayer = $TileMapLayer

# Grid → Screen: "Where does tile (5, 3) appear on screen?"
var screen_pos: Vector2 = tilemap.map_to_local(Vector2i(5, 3))

# Screen → Grid: "Which tile did the player click on?"
var grid_pos: Vector2i = tilemap.local_to_map(get_global_mouse_position())

# Player spawn position (center of the map):
# DON'T: player.position = Vector2(550, 350)  ← pixel guess
# DO: player.position = tilemap.map_to_local(Vector2i(map_width / 2, map_height / 2))

# Enemy spawn positions (edges of the map):
# DON'T: enemy.position = Vector2(10, randi_range(0, 700))  ← pixel guess
# DO: enemy.position = tilemap.map_to_local(Vector2i(0, randi_range(0, map_height)))
```

**Isometric draw order (Y-sorting):**

```gdscript
# In a top-down game, draw order doesn't matter much.
# In isometric view, objects "closer to the camera" (lower on screen)
# must be drawn on TOP of objects further away.
# Without this, a character behind a wall appears in front of it.

# Godot handles this with Y-sorting:
# Enable y_sort_enabled on the parent Node2D container.
# All children are automatically sorted by Y position each frame.

# In dungeon.tscn:
# Dungeon (Node2D)
#   ├── TileMapLayer                    ← tiles have their own Y-sort
#   └── EntityContainer (Node2D)        ← y_sort_enabled = true
#       ├── Player (CharacterBody2D)    ← sorted by position.y
#       ├── Enemy1 (CharacterBody2D)    ← sorted by position.y
#       └── Enemy2 (CharacterBody2D)    ← sorted by position.y
```

---

### Object Pooling

**Web analogy:** Reusing DOM elements instead of creating/destroying them (like virtualized lists in React).

The Phaser prototype used Phaser's group system to manage enemies:
```js
// Old Phaser approach:
enemy.disableBody(true, true);  // hide + remove from physics (but keep in memory)
// Later: re-enable and reposition (Phaser groups support this natively)
```

Godot does not have a built-in object pool. The standard approach is `queue_free()` + `instantiate()`:

```gdscript
# Simple approach: destroy and recreate (sufficient for our scale)
func defeat_enemy(enemy: CharacterBody2D) -> void:
    EventBus.enemy_defeated.emit(enemy)
    enemy.queue_free()  # destroyed, memory freed

    # Spawn a replacement after a delay
    await get_tree().create_timer(1.4).timeout
    spawn_enemy()  # creates a new instance from PackedScene

func spawn_enemy() -> void:
    var enemy: CharacterBody2D = EnemyScene.instantiate()
    enemy.position = _get_random_edge_position()
    enemy.danger_tier = randi_range(1, 3)
    $EntityContainer.add_child(enemy)


# Advanced approach: object pool pattern (for when performance matters)
# Reuse nodes instead of destroying/creating them.
var enemy_pool: Array[CharacterBody2D] = []

func get_enemy_from_pool() -> CharacterBody2D:
    if enemy_pool.size() > 0:
        var enemy: CharacterBody2D = enemy_pool.pop_back()
        enemy.visible = true
        enemy.set_physics_process(true)
        enemy.set_process(true)
        return enemy
    else:
        return EnemyScene.instantiate()  # pool empty, create new

func return_enemy_to_pool(enemy: CharacterBody2D) -> void:
    enemy.visible = false
    enemy.set_physics_process(false)
    enemy.set_process(false)
    enemy.position = Vector2(-9999, -9999)  # move off-screen
    enemy_pool.append(enemy)
```

**When to use which approach:**
- **Destroy + recreate** (`queue_free()` + `instantiate()`): Use for our dungeon crawler. We have ~10-14 enemies at a time. Instantiation cost is negligible. Simpler code, no pooling bugs.
- **Object pool** (hide + reuse): Use when spawning/destroying hundreds of objects per second (bullet hell games, particle systems). Not needed for our scale.

---

### Node Types

**Web analogy:** Like HTML element types (`<div>`, `<button>`, `<canvas>`) — each node type has specific capabilities and behaviors built in.

These are the node types used in our game, what they do, and why:

```gdscript
# CharacterBody2D — physics-aware object that YOU control
# Used for: player, enemies (anything that moves and collides)
# Key method: move_and_slide() — applies velocity, slides along walls
# Web analogy: a draggable DOM element with collision detection
extends CharacterBody2D

@export var move_speed: float = 200.0

func _physics_process(delta: float) -> void:
    velocity = Input.get_vector("move_left", "move_right", "move_up", "move_down") * move_speed
    move_and_slide()
    # After move_and_slide(), the node has moved and resolved all collisions.
    # velocity is updated to reflect the actual movement (after sliding).


# Area2D — invisible detection zone (does NOT block movement)
# Used for: attack range, item pickup zone, room transition triggers
# Key signals: body_entered, body_exited, area_entered, area_exited
# Web analogy: an invisible div with an IntersectionObserver
extends Area2D

func _ready() -> void:
    body_entered.connect(_on_body_entered)

func _on_body_entered(body: Node2D) -> void:
    if body.is_in_group("enemies"):
        print("Enemy entered attack range!")


# TileMapLayer — renders a grid of tiles efficiently
# Used for: dungeon floor and walls
# Key methods: set_cell(), get_cell_atlas_coords(), map_to_local(), local_to_map()
# Web analogy: a CSS Grid where each cell is a tile image
# Configuration: tile_set property links to a TileSet resource (.tres)


# Control — base class for all UI nodes
# Used for: HUD, menus, death screen, any on-screen interface
# Key properties: anchor, margin, size, theme
# Web analogy: a <div> that participates in CSS layout
# Important: NOT affected by Camera2D — stays fixed on screen


# Camera2D — controls what part of the world is visible
# Used for: following the player through the dungeon
# Key properties: zoom, position_smoothing_speed, limit_*
# Web analogy: CSS scroll position + smooth scroll behavior
# Make it a child of the player node to auto-follow


# Timer — fires a signal after a delay
# Used for: attack cooldowns, enemy spawn intervals, hit invincibility
# Key properties: wait_time, one_shot, autostart
# Key signal: timeout
# Web analogy: setTimeout (one_shot=true) or setInterval (one_shot=false)
extends Node  # Timer is a child node

@onready var attack_timer: Timer = $AttackCooldownTimer

func _ready() -> void:
    attack_timer.wait_time = 0.42  # 420ms, like ATTACK_COOLDOWN in Phaser prototype
    attack_timer.one_shot = true
    attack_timer.timeout.connect(_on_attack_timer_timeout)

func try_attack() -> void:
    if attack_timer.is_stopped():
        perform_attack()
        attack_timer.start()

func _on_attack_timer_timeout() -> void:
    pass  # timer stopped, attack is available again


# Polygon2D — draws a colored polygon (temporary visuals)
# Used for: player shape, enemy shapes (until sprite art exists)
# Key properties: polygon (PackedVector2Array of vertices), color
# Web analogy: an SVG polygon element
# This is our stand-in for Phaser's add.circle() and add.rectangle()


# CanvasLayer — puts all children on a separate rendering layer
# Used for: UI that should not be affected by Camera2D zoom/movement
# Key property: layer (higher = drawn on top)
# Web analogy: position: fixed (stays in place while content scrolls)
```

---

### Exports and Onready

**Web analogy:** `@export` is like React props or HTML attributes — configurable properties set from outside the component. `@onready` is like getting a DOM reference inside `DOMContentLoaded`.

```gdscript
# --- @export: properties editable in the Godot Inspector ---
# When you select a node in the editor, @export variables appear
# in the Inspector panel on the right. You can change them per-instance.

# Basic exports:
@export var move_speed: float = 200.0       # number field
@export var max_hp: int = 100               # integer field
@export var enemy_name: String = "Goblin"   # text field
@export var enemy_color: Color = Color.RED  # color picker
@export var is_boss: bool = false           # checkbox

# Constrained exports:
@export_range(1, 3) var danger_tier: int = 1         # slider, 1-3
@export_range(0.0, 1.0, 0.05) var armor: float = 0.0 # slider with step
@export_enum("Melee", "Ranged", "Magic") var attack_type: int = 0  # dropdown
@export_file("*.tscn") var loot_scene: String         # file picker, .tscn only
@export_multiline var description: String = ""         # multi-line text box

# Export categories and groups (organize the Inspector):
@export_category("Combat Stats")
@export var attack_damage: int = 10
@export var attack_cooldown: float = 0.42

@export_category("Movement")
@export var move_speed: float = 200.0
@export var chase_range: float = 300.0

# Using exports for scene composition:
# In dungeon.gd, you can expose which enemy scene to spawn:
@export var enemy_scene: PackedScene  # drag-and-drop a .tscn file in the Inspector

func spawn_enemy() -> void:
    var enemy = enemy_scene.instantiate()
    add_child(enemy)
```

```gdscript
# --- @onready: deferred initialization of node references ---
# Child nodes don't exist until _ready() runs. @onready defers assignment.

# Without @onready (verbose, but clear):
var hp_label: Label

func _ready() -> void:
    hp_label = $UI/HUD/HPLabel  # safe here — children exist

# With @onready (concise, same behavior):
@onready var hp_label: Label = $UI/HUD/HPLabel  # assigned when _ready() runs

# Common @onready patterns in our game:
@onready var polygon: Polygon2D = $Polygon2D
@onready var collision_shape: CollisionShape2D = $CollisionShape2D
@onready var attack_range: Area2D = $AttackRange
@onready var attack_timer: Timer = $AttackCooldownTimer
@onready var camera: Camera2D = $Camera2D

# GOTCHA: Do not access @onready variables in _init()
# _init() runs before the node is in the tree — @onready hasn't triggered yet
func _init() -> void:
    # hp_label is null here! Will crash if accessed.
    pass

func _ready() -> void:
    # hp_label is now assigned. Safe to use.
    hp_label.text = "HP: 100"
```

---

### Scene Instancing

**Web analogy:** Like importing and rendering React components, or `document.createElement()` + `appendChild()`. A PackedScene is the class/template; `instantiate()` creates a live instance.

```gdscript
# --- preload() vs load() ---

# preload() — loads at PARSE TIME (when the script is first loaded)
# Like: import Enemy from "./Enemy.jsx" (static ES6 import)
# Use when: you know the path at write time and will definitely need it
const EnemyScene: PackedScene = preload("res://scenes/enemies/enemy.tscn")
const HudScene: PackedScene = preload("res://scenes/ui/hud.tscn")

# load() — loads at CALL TIME (when the line executes)
# Like: const Enemy = await import("./Enemy.jsx") (dynamic import)
# Use when: the path is determined at runtime, or you want lazy loading
func load_level(level_name: String) -> void:
    var scene: PackedScene = load("res://scenes/levels/" + level_name + ".tscn")
    var instance: Node = scene.instantiate()
    add_child(instance)


# --- Full instantiation lifecycle ---

# 1. Load the scene resource (preload or load)
const EnemyScene: PackedScene = preload("res://scenes/enemies/enemy.tscn")

# 2. Create an instance (like new Component() or React.createElement)
var enemy: CharacterBody2D = EnemyScene.instantiate()
# At this point, the node exists in memory but is NOT in the scene tree.
# _ready() has NOT been called. _process() is NOT running.
# You CAN set properties (position, @export vars, etc.)

# 3. Configure the instance (like setting props before rendering)
enemy.position = Vector2(500, 300)
enemy.danger_tier = 2
enemy.enemy_color = Color.YELLOW

# 4. Add to the scene tree (like appendChild or ReactDOM.render)
$EntityContainer.add_child(enemy)
# NOW _enter_tree() fires, then _ready() fires, then _process() starts.
# The enemy is alive and part of the game.

# 5. Later: remove from the tree (like element.remove() or unmounting)
enemy.queue_free()
# Schedules removal at the end of the current frame.
# _exit_tree() fires. The node is freed from memory.
# Any references to it become invalid (will crash if used).


# --- Practical example: enemy spawning system ---
# (Replaces the Phaser prototype's spawnEnemy() + time.addEvent loop)

extends Node2D  # dungeon.gd

const EnemyScene: PackedScene = preload("res://scenes/enemies/enemy.tscn")
const MAX_ENEMIES: int = 14

@onready var entity_container: Node2D = $EntityContainer
@onready var spawn_timer: Timer = $SpawnTimer

func _ready() -> void:
    # Spawn initial enemies (like the Phaser for-loop in create())
    for i in 10:
        spawn_enemy()

    # Set up recurring spawn (like Phaser's time.addEvent with loop: true)
    spawn_timer.wait_time = 2.8  # 2800ms, same as Phaser prototype
    spawn_timer.timeout.connect(_on_spawn_timer_timeout)
    spawn_timer.start()

func _on_spawn_timer_timeout() -> void:
    var enemy_count: int = get_tree().get_nodes_in_group("enemies").size()
    if enemy_count < MAX_ENEMIES:
        spawn_enemy()

func spawn_enemy() -> void:
    var enemy: CharacterBody2D = EnemyScene.instantiate()
    enemy.position = _get_random_edge_position()
    enemy.danger_tier = randi_range(1, 3)
    entity_container.add_child(enemy)

func _get_random_edge_position() -> Vector2:
    # Replaces the Phaser spawnEnemy() edge calculation
    var tilemap: TileMapLayer = $TileMapLayer
    var edge: int = randi_range(0, 3)
    var map_width: int = 20   # tiles
    var map_height: int = 20  # tiles
    var grid_pos: Vector2i

    match edge:
        0: grid_pos = Vector2i(randi_range(0, map_width), 0)
        1: grid_pos = Vector2i(map_width - 1, randi_range(0, map_height))
        2: grid_pos = Vector2i(randi_range(0, map_width), map_height - 1)
        3: grid_pos = Vector2i(0, randi_range(0, map_height))

    return tilemap.map_to_local(grid_pos)
```

## Open Questions

- Should we document the entity-component pattern if the game grows beyond simple scene instancing?
- When does it make sense to introduce a proper state machine (for game flow, enemy behavior)?
- Should we add a section on Godot's tween system (replacement for Phaser tweens)?
- Would a section on Godot's built-in animation system (AnimationPlayer) be useful for this doc or belong in a separate doc?
- Should we document Godot's shader language basics for visual effects (slash effect, damage flash)?
