# Game Dev Concepts for Web Developers

## Summary

A bridge from web development to game development, mapping familiar concepts to their Godot 4 / C# equivalents. Every concept includes a web analogy, a C# code example, and key differences.

## Current State

Understanding these concepts will help make sense of the Godot 4 (C#) codebase. The game originated as a Phaser 3 browser prototype, went through a C# implementation phase, and is now being rebuilt with visual-first development after the Session 8 fresh start.

## Design

### The Game Loop

**Web analogy:** `requestAnimationFrame` — but managed by Godot, split into two separate loops.

In web development, the browser renders the DOM and you respond to events. In game development, you have a **game loop** that runs every frame. Each frame:

1. **Process input** — check what keys are pressed, read action states
2. **Update state** — move objects, run AI, check collisions, apply damage
3. **Render** — Godot draws everything automatically (you don't call a render function)

Godot provides TWO game loop callbacks. Understanding when to use each is critical:

```csharp
// _PhysicsProcess(delta) — runs at a FIXED rate (60 times per second, always)
// Use for: movement, collision, AI, combat — anything that affects gameplay
// This is where most game logic goes.
// Replaces: Phaser's update(time, delta) for gameplay logic
public override void _PhysicsProcess(double delta)
{
    HandleMovement();
    HandleEnemyAi();
    TryAutoAttack();
}

// _Process(delta) — runs every RENDER frame (varies: 60fps, 144fps, etc.)
// Use for: visual effects, animations, UI updates, camera smoothing
// Replaces: Phaser's update(time, delta) for visual-only concerns
public override void _Process(double delta)
{
    UpdateSlashEffect();
    SmoothCamera();
}
```

**Why two loops instead of one?** In the Phaser prototype, `update(time, delta)` handled everything — movement, AI, combat, and visuals — all in one callback that ran at the browser's frame rate. This means a player on a 144Hz monitor would have their game loop run 2.4x more often than a player on a 60Hz monitor. Even with delta time compensation, floating-point accumulation causes tiny gameplay differences.

Godot's `_PhysicsProcess()` runs at a **fixed** 60fps regardless of monitor refresh rate. This makes gameplay deterministic — the same inputs produce the same results on every machine. `_Process()` runs at the display's refresh rate for smooth visuals.

**Migration mapping from Phaser prototype:**

| Phaser (old) | Godot (new) | Why it moved |
|-------------|-------------|-------------|
| `update()` → `handleMovement()` | `_PhysicsProcess()` → `HandleMovement()` | Movement uses physics (MoveAndSlide) |
| `update()` → `handleEnemyAI()` | `_PhysicsProcess()` → `HandleEnemyAi()` | AI chase uses velocity/physics |
| `update()` → `tryAutoAttack(time)` | `_PhysicsProcess()` → `TryAutoAttack()` | Combat timing must be deterministic |
| `create()` → setup code | `_Ready()` → setup code | One-time initialization |

---

### Delta Time

**Web analogy:** The timestamp parameter in `requestAnimationFrame`, used to make animations frame-rate-independent.

Frames don't always take the same amount of time. If you move a character 5 pixels per frame, they'll move faster on a 120fps monitor than a 60fps one. **Delta time** tells you how many seconds elapsed since the last frame. Use it to make movement frame-independent:

```csharp
// Bad: speed depends on frame rate
Position = new Vector2(Position.X + 5, Position.Y);

// Good: consistent speed regardless of frame rate
Position = new Vector2(Position.X + 300.0f * (float)delta, Position.Y); // 300 pixels per second

// Best (for CharacterBody2D): use Velocity + MoveAndSlide()
// MoveAndSlide() handles delta internally — do NOT multiply by delta yourself
Velocity = direction * MoveSpeed; // MoveSpeed is in pixels per second
MoveAndSlide();                   // applies velocity, handles delta, resolves collisions
```

**The biggest gotcha for web developers:** When using `MoveAndSlide()` (which you almost always should for CharacterBody2D), do NOT multiply by delta. The function handles it internally. This is different from the Phaser prototype where `body.setVelocity()` also handled delta internally via the Arcade physics engine, but the principle was less visible.

```csharp
// WRONG — double-applies delta, character moves in slow motion
Velocity = direction * MoveSpeed * (float)delta;
MoveAndSlide();

// RIGHT — MoveAndSlide applies delta for you
Velocity = direction * MoveSpeed;
MoveAndSlide();

// ALSO RIGHT — manual position update when NOT using MoveAndSlide
// (rare, used for non-physics objects like visual effects)
Position += direction * MoveSpeed * (float)delta;
```

**Delta values:**
- In `_PhysicsProcess(delta)`: delta is ~0.01667 seconds (1/60th), nearly constant
- In `_Process(delta)`: delta varies per frame (0.0069 at 144fps, 0.0167 at 60fps, higher if frame drops)

---

### Sprites vs DOM Elements

**Web analogy:** There are no DOM elements in the game world. Everything is a **node in the scene tree**, rendered by the engine.

In web dev, everything is an HTML element positioned by CSS. In Godot, everything is a **Node** with x/y coordinates, rendered by the engine each frame. You can't inspect them in browser DevTools — but you CAN inspect them in the Godot editor's Remote scene tree (while the game is running).

```csharp
// Phaser prototype used:
// this.add.circle(x, y, radius, color)     → colored circle on canvas
// this.add.rectangle(x, y, w, h, color)    → colored rectangle on canvas

// Godot equivalents:

// Option 1: Polygon2D — draw a colored shape (our current approach)
// Like the Phaser circles, but defined as a polygon with vertices
var polygon = new Polygon2D();
polygon.Polygon = new Vector2[]
{
    new Vector2(0, -12), new Vector2(12, 0), new Vector2(0, 12), new Vector2(-12, 0)
}; // diamond shape
polygon.Color = new Color(0.56f, 0.84f, 1.0f); // light blue, like COLORS.player
AddChild(polygon);

// Option 2: Sprite2D — display a texture/image (future, when art exists)
var sprite = new Sprite2D();
sprite.Texture = GD.Load<Texture2D>("res://assets/sprites/player.png");
sprite.Position = new Vector2(100, 200);
AddChild(sprite);

// Option 3: ColorRect — a colored rectangle (for UI elements, not game world)
var rect = new ColorRect();
rect.Color = new Color(0.0f, 0.0f, 0.0f, 0.75f);
rect.Size = new Vector2(380, 160);
AddChild(rect);
```

**Critical distinction — Node2D vs Control:**

```csharp
// Node2D and its children (Sprite2D, Polygon2D, CharacterBody2D, Area2D):
// → Used for GAME WORLD objects (player, enemies, tiles, effects)
// → Positioned by x/y coordinates (Position = new Vector2(100, 200))
// → Affected by Camera2D (scroll with the world)
// → Not affected by UI layout containers
// → Like: absolutely positioned canvas drawings

// Control and its children (Label, Button, Panel, TextureRect):
// → Used for UI elements (HUD, menus, death screen)
// → Positioned by anchors, margins, and layout containers
// → NOT affected by Camera2D (stay fixed on screen)
// → Affected by Theme resources for styling
// → Like: HTML elements with CSS layout

// NEVER put a Control node as a child of a Node2D expecting it to move with the camera.
// NEVER put a Node2D inside a VBoxContainer expecting it to follow UI layout.
```

---

### Collision Detection

**Web analogy:** No real equivalent. Maybe `IntersectionObserver`, but much more precise and physics-based.

The Phaser prototype used `this.physics.add.overlap(player, enemies, onPlayerHit)` — a single callback for when player and enemy circles intersected. Godot uses a fundamentally different approach: **collision shapes** on **physics bodies** and **areas**, with **signals** for notification.

```csharp
// Godot collision system has three key pieces:

// 1. CollisionShape2D — defines the SHAPE of the collider
//    Attached as a child of a physics body or area.
//    Shape types: CircleShape2D, RectangleShape2D, CapsuleShape2D, ConvexPolygonShape2D

// 2. Physics bodies — objects that participate in collision RESOLUTION
//    CharacterBody2D: moves with code control, collides and slides (player, enemies)
//    StaticBody2D: doesn't move, blocks other bodies (walls)
//    RigidBody2D: moves with physics simulation (thrown objects, ragdolls)

// 3. Area2D — objects that DETECT overlaps but don't physically collide
//    Like: invisible trigger zones. "Something entered my space."
//    Used for: attack range detection, item pickups, room transitions

// --- Setting up collision (replaces Phaser's physics.add.overlap) ---

// In the player scene (player.tscn):
// Player (CharacterBody2D)          ← moves and collides with walls/enemies
//   ├── CollisionShape2D             ← player's physical body (circle, radius 12)
//   └── AttackRange (Area2D)         ← invisible detection zone for auto-attack
//       └── CollisionShape2D         ← attack range shape (circle, radius 78)

// In Player.cs:
private List<CharacterBody2D> _enemiesInRange = new();

public override void _Ready()
{
    // Connect the Area2D signal (like addEventListener)
    var attackRange = GetNode<Area2D>("AttackRange");
    attackRange.BodyEntered += OnAttackRangeBodyEntered;
    attackRange.BodyExited += OnAttackRangeBodyExited;
}

private void OnAttackRangeBodyEntered(Node2D body)
{
    if (body.IsInGroup("enemies"))
        _enemiesInRange.Add((CharacterBody2D)body);
}

private void OnAttackRangeBodyExited(Node2D body)
{
    _enemiesInRange.Remove((CharacterBody2D)body);
}
```

**Collision layers and masks** (no Phaser equivalent — this is new):

```csharp
// Godot has 32 collision layers. Each physics body/area exists on layers
// and scans other layers. This replaces Phaser's simple "group A overlaps group B."

// Layer setup (in Project Settings → 2D Physics → Layer Names):
// Layer 1: "player"       — the player's body
// Layer 2: "enemies"      — enemy bodies
// Layer 3: "walls"        — static wall collision
// Layer 4: "player_attack"— player's attack detection area
// Layer 5: "enemy_attack" — enemy damage detection area

// Player CharacterBody2D:
//   Collision Layer: 1 (player)        — "I exist on layer 1"
//   Collision Mask: 2, 3 (enemies, walls) — "I collide with layers 2 and 3"

// Enemy CharacterBody2D:
//   Collision Layer: 2 (enemies)       — "I exist on layer 2"
//   Collision Mask: 1, 3 (player, walls) — "I collide with layers 1 and 3"

// Player AttackRange Area2D:
//   Collision Layer: 4 (player_attack) — "I exist on layer 4"
//   Collision Mask: 2 (enemies)        — "I detect things on layer 2"

// This means:
// Player collides with enemies and walls
// Enemies collide with player and walls
// Player attack range detects enemies
// Enemies don't collide with each other (no friendly fire)
// Player attack range doesn't detect the player itself
```

**Migration mapping from Phaser prototype:**

| Phaser (old) | Godot (new) |
|-------------|-------------|
| `physics.add.overlap(player, enemies, onPlayerHit)` | Enemy Area2D `BodyEntered` event → `OnBodyEntered()` |
| `physics.moveToObject(enemy, player, speed)` | `Velocity = Position.DirectionTo(target) * speed` + `MoveAndSlide()` |
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

```csharp
// --- Scripts/Autoloads/GameState.cs ---
// Registered as Autoload "GameState" in Project Settings
using Godot;

public partial class GameState : Node
{
    // Signals — listeners subscribe to these for reactive updates
    [Signal] public delegate void StatsChangedEventHandler();
    [Signal] public delegate void PlayerDiedEventHandler();
    [Signal] public delegate void FloorChangedEventHandler(int newFloor);

    // State with reactive setters — changing a value auto-emits a signal
    private int _hp = 100;
    public int Hp
    {
        get => _hp;
        set
        {
            _hp = Mathf.Clamp(value, 0, MaxHp);
            EmitSignal(SignalName.StatsChanged);
            if (_hp <= 0)
                EmitSignal(SignalName.PlayerDied);
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
            CheckLevelUp();
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
            EmitSignal(SignalName.FloorChanged, _floorNumber);
            EmitSignal(SignalName.StatsChanged);
        }
    }

    private void CheckLevelUp()
    {
        // Canonical formula: floor(Level^2 * 45). See leveling.md.
        int xpToLevel = (int)Math.Floor(Level * Level * 45.0);
        if (Xp >= xpToLevel)
        {
            Xp -= xpToLevel;
            Level += 1;
            int hpGain = (int)Math.Floor(8 + Level * 0.5);
            MaxHp += hpGain;
            Hp = Math.Min(MaxHp, Hp + (int)Math.Floor(MaxHp * 0.15));
        }
    }

    public void Reset()
    {
        Hp = 100;
        MaxHp = 100;
        Xp = 0;
        Level = 1;
        FloorNumber = 1;
    }
}


// --- Scenes/Ui/Hud.cs ---
// The HUD listens to state changes — no manual UpdateHud() calls needed
using Godot;

public partial class Hud : Control
{
    private Label _hpLabel = null!;
    private Label _xpLabel = null!;
    private Label _levelLabel = null!;
    private Label _floorLabel = null!;

    public override void _Ready()
    {
        _hpLabel = GetNode<Label>("VBoxContainer/HPLabel");
        _xpLabel = GetNode<Label>("VBoxContainer/XPLabel");
        _levelLabel = GetNode<Label>("VBoxContainer/LevelLabel");
        _floorLabel = GetNode<Label>("VBoxContainer/FloorLabel");

        var gameState = GetNode<GameState>("/root/GameState");
        gameState.StatsChanged += UpdateDisplay;
        UpdateDisplay(); // initial render
    }

    private void UpdateDisplay()
    {
        var gameState = GetNode<GameState>("/root/GameState");
        _hpLabel.Text = $"HP: {gameState.Hp} / {gameState.MaxHp}";
        _xpLabel.Text = $"XP: {gameState.Xp}";
        _levelLabel.Text = $"LVL: {gameState.Level}";
        _floorLabel.Text = $"Floor: {gameState.FloorNumber}";
    }
}
```

**Key difference from the Phaser prototype:** In the prototype, `updateHud()` was called manually after every state change (in `defeatEnemy`, `onPlayerHit`, `gameOver`). If you forgot to call it, the HUD showed stale data. In Godot, the signal-based approach makes it **impossible** to forget — changing `GameState.Hp` automatically triggers `StatsChanged`, which automatically updates the HUD.

---

### Scenes

**Web analogy:** Routes in a single-page app (React Router, Vue Router) — each "page" has its own component tree, and you navigate between them.

The Phaser prototype had a single `DungeonScene` class with `create()` and `update()` methods, and `scene.restart()` for restarting. Godot scenes are fundamentally different — they are **files on disk** (`.tscn`) that you edit visually in the editor.

```csharp
// --- Key scene operations ---

// Loading a scene (like importing a component)
private static readonly PackedScene EnemyScene = GD.Load<PackedScene>("res://scenes/enemies/enemy.tscn");

// Instantiating a scene (like rendering a component)
var enemy = EnemyScene.Instantiate<CharacterBody2D>();
enemy.Position = new Vector2(500, 300);
AddChild(enemy); // adds to the scene tree, triggers _Ready()

// Removing a scene instance (like unmounting a component)
enemy.QueueFree(); // safe removal at end of frame

// Switching scenes entirely (like route navigation)
GetTree().ChangeSceneToFile("res://scenes/dungeon/dungeon.tscn");

// Restarting the current scene (replaces Phaser's scene.restart())
GetTree().ReloadCurrentScene();

// Pausing the scene tree (like disabling requestAnimationFrame)
GetTree().Paused = true;  // everything stops
GetTree().Paused = false; // everything resumes
// Individual nodes can opt out of pausing via ProcessMode property:
// ProcessModeEnum.Always — keeps running when paused (for pause menu UI)
```

**Scene transitions in our game:**

```csharp
// The main.tscn scene manages transitions:
// Main (Node2D)
//   ├── CurrentScene (Node2D)    ← dungeon or town scene loaded here
//   └── UILayer (CanvasLayer)    ← HUD stays across scene changes
//       └── HUD (instanced)

// --- Scenes/Main.cs ---
using Godot;

public partial class Main : Node2D
{
    public void ChangeToDungeon()
    {
        ChangeScene("res://scenes/dungeon/dungeon.tscn");
    }

    public void ChangeToTown()
    {
        ChangeScene("res://scenes/world/town.tscn");
    }

    private void ChangeScene(string scenePath)
    {
        // Remove current scene
        var currentScene = GetNode<Node2D>("CurrentScene");
        foreach (var child in currentScene.GetChildren())
            child.QueueFree();

        // Load and add new scene
        var newScene = GD.Load<PackedScene>(scenePath);
        var instance = newScene.Instantiate();
        currentScene.AddChild(instance);
    }
}
```

**Migration mapping from Phaser prototype:**

| Phaser (old) | Godot (new) |
|-------------|-------------|
| `class DungeonScene extends Phaser.Scene` | `dungeon.tscn` + `Dungeon.cs` (: Node2D) |
| `constructor() { super("dungeon") }` | Scene name is the file name |
| `create()` | `_Ready()` |
| `update(time, delta)` | `_PhysicsProcess(delta)` |
| `this.scene.restart()` | `GetTree().ReloadCurrentScene()` |
| `this.scene.start("town")` | `GetTree().ChangeSceneToFile("res://scenes/world/town.tscn")` |

---

### Coordinate System

**Web analogy:** Same as CSS `position: absolute` — (0,0) is top-left, Y increases downward.

The Phaser prototype used a fixed 1100x700 pixel game world with a grid background. Godot's coordinate system is identical in principle but adds **isometric transformation** for the tile-based dungeon.

```csharp
// Standard 2D coordinates (same as web):
// (0,0) is top-left
// X increases rightward →
// Y increases downward ↓
// This is identical to CSS absolute positioning and Canvas 2D.

// Our Phaser prototype:
// - Game world: 1100 x 700 pixels, fixed size
// - Player spawned at (550, 350) — center of the world
// - Enemy spawned at edges (x=10, y=10, etc.)
// - All coordinates were screen pixels

// Our Godot game:
// - Game world: much larger, scrolled by Camera2D
// - Viewport: 1920 x 1080 (what's visible on screen)
// - The TileMapLayer uses isometric coordinates

// Tile dimensions (defined by the ISS environment tileset):
// Floor tiles: 64x32 pixels (isometric diamond)
// Wall blocks: 64x64 pixels (isometric cube)
// TileMapLayer config: TileSize = Vector2I(64, 32), TileShape = Isometric

// Isometric coordinate conversion:
// In isometric view, the grid is rotated 45° and squashed vertically.
// Grid position (5, 3) does NOT correspond to pixel position (5, 3).
// The TileMapLayer handles conversion:

var tilemap = GetNode<TileMapLayer>("TileMapLayer");

// Grid → Screen: "Where does tile (5, 3) appear on screen?"
Vector2 screenPos = tilemap.MapToLocal(new Vector2I(5, 3));

// Screen → Grid: "Which tile did the player click on?"
Vector2I gridPos = tilemap.LocalToMap(GetGlobalMousePosition());

// Player spawn position (center of the map):
// DON'T: player.Position = new Vector2(550, 350);  ← pixel guess
// DO: player.Position = tilemap.MapToLocal(new Vector2I(mapWidth / 2, mapHeight / 2));

// Enemy spawn positions (edges of the map):
// DON'T: enemy.Position = new Vector2(10, GD.RandRange(0, 700));  ← pixel guess
// DO: enemy.Position = tilemap.MapToLocal(new Vector2I(0, (int)GD.RandRange(0, mapHeight)));
```

**Isometric draw order (Y-sorting):**

```csharp
// In a top-down game, draw order doesn't matter much.
// In isometric view, objects "closer to the camera" (lower on screen)
// must be drawn on TOP of objects further away.
// Without this, a character behind a wall appears in front of it.

// Godot handles this with Y-sorting:
// Enable YSortEnabled on the parent Node2D container.
// All children are automatically sorted by Y position each frame.

// In dungeon.tscn:
// Dungeon (Node2D)
//   ├── TileMapLayer                    ← tiles have their own Y-sort
//   └── EntityContainer (Node2D)        ← YSortEnabled = true
//       ├── Player (CharacterBody2D)    ← sorted by Position.Y
//       ├── Enemy1 (CharacterBody2D)    ← sorted by Position.Y
//       └── Enemy2 (CharacterBody2D)    ← sorted by Position.Y
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

Godot does not have a built-in object pool. The standard approach is `QueueFree()` + `Instantiate()`:

```csharp
// Simple approach: destroy and recreate (sufficient for our scale)
private async void DefeatEnemy(CharacterBody2D enemy)
{
    var eventBus = GetNode<EventBus>("/root/EventBus");
    eventBus.EmitSignal(EventBus.SignalName.EnemyDefeated, enemy);
    enemy.QueueFree(); // destroyed, memory freed

    // Spawn a replacement after a delay
    await ToSignal(GetTree().CreateTimer(1.4), SceneTreeTimer.SignalName.Timeout);
    SpawnEnemy(); // creates a new instance from PackedScene
}

private void SpawnEnemy()
{
    var enemy = EnemyScene.Instantiate<CharacterBody2D>();
    enemy.Position = GetRandomEdgePosition();
    enemy.Set("DangerTier", GD.RandRange(1, 3));
    GetNode<Node2D>("EntityContainer").AddChild(enemy);
}


// Advanced approach: object pool pattern (for when performance matters)
// Reuse nodes instead of destroying/creating them.
private List<CharacterBody2D> _enemyPool = new();

private CharacterBody2D GetEnemyFromPool()
{
    if (_enemyPool.Count > 0)
    {
        var enemy = _enemyPool[^1];
        _enemyPool.RemoveAt(_enemyPool.Count - 1);
        enemy.Visible = true;
        enemy.SetPhysicsProcess(true);
        enemy.SetProcess(true);
        return enemy;
    }
    else
    {
        return EnemyScene.Instantiate<CharacterBody2D>(); // pool empty, create new
    }
}

private void ReturnEnemyToPool(CharacterBody2D enemy)
{
    enemy.Visible = false;
    enemy.SetPhysicsProcess(false);
    enemy.SetProcess(false);
    enemy.Position = new Vector2(-9999, -9999); // move off-screen
    _enemyPool.Add(enemy);
}
```

**When to use which approach:**
- **Destroy + recreate** (`QueueFree()` + `Instantiate()`): Use for our dungeon crawler. We have ~10-14 enemies at a time. Instantiation cost is negligible. Simpler code, no pooling bugs.
- **Object pool** (hide + reuse): Use when spawning/destroying hundreds of objects per second (bullet hell games, particle systems). Not needed for our scale.

---

### Node Types

**Web analogy:** Like HTML element types (`<div>`, `<button>`, `<canvas>`) — each node type has specific capabilities and behaviors built in.

These are the node types used in our game, what they do, and why:

```csharp
// CharacterBody2D — physics-aware object that YOU control
// Used for: player, enemies (anything that moves and collides)
// Key method: MoveAndSlide() — applies velocity, slides along walls
// Web analogy: a draggable DOM element with collision detection
using Godot;

public partial class Player : CharacterBody2D
{
    [Export] public float MoveSpeed { get; set; } = 200.0f;

    public override void _PhysicsProcess(double delta)
    {
        Velocity = Input.GetVector("move_left", "move_right", "move_up", "move_down") * MoveSpeed;
        MoveAndSlide();
        // After MoveAndSlide(), the node has moved and resolved all collisions.
        // Velocity is updated to reflect the actual movement (after sliding).
    }
}


// Area2D — invisible detection zone (does NOT block movement)
// Used for: attack range, item pickup zone, room transition triggers
// Key signals: BodyEntered, BodyExited, AreaEntered, AreaExited
// Web analogy: an invisible div with an IntersectionObserver
public partial class AttackRange : Area2D
{
    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body.IsInGroup("enemies"))
            GD.Print("Enemy entered attack range!");
    }
}


// TileMapLayer — renders a grid of tiles efficiently
// Used for: dungeon floor and walls
// Key methods: SetCell(), GetCellAtlasCoords(), MapToLocal(), LocalToMap()
// Web analogy: a CSS Grid where each cell is a tile image
// Configuration: TileSet property links to a TileSet resource (.tres)


// Control — base class for all UI nodes
// Used for: HUD, menus, death screen, any on-screen interface
// Key properties: anchor, margin, size, theme
// Web analogy: a <div> that participates in CSS layout
// Important: NOT affected by Camera2D — stays fixed on screen


// Camera2D — controls what part of the world is visible
// Used for: following the player through the dungeon
// Key properties: Zoom, PositionSmoothingSpeed, Limit*
// Web analogy: CSS scroll position + smooth scroll behavior
// Make it a child of the player node to auto-follow


// Timer — fires a signal after a delay
// Used for: attack cooldowns, enemy spawn intervals, hit invincibility
// Key properties: WaitTime, OneShot, Autostart
// Key signal: Timeout
// Web analogy: setTimeout (OneShot=true) or setInterval (OneShot=false)
public partial class CombatNode : Node // Timer is a child node
{
    private Timer _attackTimer = null!;

    public override void _Ready()
    {
        _attackTimer = GetNode<Timer>("AttackCooldownTimer");
        _attackTimer.WaitTime = 0.42; // 420ms, like ATTACK_COOLDOWN in Phaser prototype
        _attackTimer.OneShot = true;
        _attackTimer.Timeout += OnAttackTimerTimeout;
    }

    public void TryAttack()
    {
        if (_attackTimer.IsStopped())
        {
            PerformAttack();
            _attackTimer.Start();
        }
    }

    private void OnAttackTimerTimeout()
    {
        // timer stopped, attack is available again
    }

    private void PerformAttack() { /* ... */ }
}


// Polygon2D — draws a colored polygon (temporary visuals)
// Used for: player shape, enemy shapes (until sprite art exists)
// Key properties: Polygon (Vector2[] of vertices), Color
// Web analogy: an SVG polygon element
// This is our stand-in for Phaser's add.circle() and add.rectangle()


// CanvasLayer — puts all children on a separate rendering layer
// Used for: UI that should not be affected by Camera2D zoom/movement
// Key property: Layer (higher = drawn on top)
// Web analogy: position: fixed (stays in place while content scrolls)
```

---

### Exports and Onready

**Web analogy:** `[Export]` is like React props or HTML attributes — configurable properties set from outside the component. Node references initialized in `_Ready()` are like getting a DOM reference inside `DOMContentLoaded`.

```csharp
// --- [Export]: properties editable in the Godot Inspector ---
// When you select a node in the editor, [Export] properties appear
// in the Inspector panel on the right. You can change them per-instance.

// Basic exports:
[Export] public float MoveSpeed { get; set; } = 200.0f;      // number field
[Export] public int MaxHp { get; set; } = 100;               // integer field
[Export] public string EnemyName { get; set; } = "Goblin";   // text field
[Export] public Color EnemyColor { get; set; } = Colors.Red; // color picker
[Export] public bool IsBoss { get; set; } = false;            // checkbox

// Constrained exports:
[Export(PropertyHint.Range, "1,3")] public int DangerTier { get; set; } = 1;          // slider, 1-3
[Export(PropertyHint.Range, "0.0,1.0,0.05")] public float Armor { get; set; } = 0.0f; // slider with step
[Export(PropertyHint.Enum, "Melee,Ranged,Magic")] public int AttackType { get; set; } = 0; // dropdown
[Export(PropertyHint.File, "*.tscn")] public string LootScene { get; set; } = "";     // file picker, .tscn only
[Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";     // multi-line text box

// Export categories and groups (organize the Inspector):
[ExportCategory("Combat Stats")]
[Export] public int AttackDamage { get; set; } = 10;
[Export] public float AttackCooldown { get; set; } = 0.42f;

[ExportCategory("Movement")]
[Export] public float MovementSpeed { get; set; } = 200.0f;
[Export] public float ChaseRange { get; set; } = 300.0f;

// Using exports for scene composition:
// In Dungeon.cs, you can expose which enemy scene to spawn:
[Export] public PackedScene EnemyScene { get; set; } = null!; // drag-and-drop a .tscn file in the Inspector

public void SpawnEnemy()
{
    var enemy = EnemyScene.Instantiate();
    AddChild(enemy);
}
```

```csharp
// --- Node references initialized in _Ready() ---
// Child nodes don't exist until _Ready() runs. Declare fields with null!
// and assign them in _Ready().

// C# does not have @onready — assign node references in _Ready():
private Label _hpLabel = null!;

public override void _Ready()
{
    _hpLabel = GetNode<Label>("UI/HUD/HPLabel"); // safe here — children exist
}

// Common node reference patterns in our game:
private Polygon2D _polygon = null!;
private CollisionShape2D _collisionShape = null!;
private Area2D _attackRange = null!;
private Timer _attackTimer = null!;
private Camera2D _camera = null!;

public override void _Ready()
{
    _polygon = GetNode<Polygon2D>("Polygon2D");
    _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
    _attackRange = GetNode<Area2D>("AttackRange");
    _attackTimer = GetNode<Timer>("AttackCooldownTimer");
    _camera = GetNode<Camera2D>("Camera2D");

    // _hpLabel is now assigned. Safe to use.
    _hpLabel.Text = "HP: 100";
}

// GOTCHA: Do not access node references in the constructor
// The constructor runs before the node is in the tree — fields are still null!
public MyNode()
{
    // _hpLabel is null here! Will crash if accessed.
}
```

---

### Scene Instancing

**Web analogy:** Like importing and rendering React components, or `document.createElement()` + `appendChild()`. A PackedScene is the class/template; `Instantiate()` creates a live instance.

```csharp
// --- GD.Load<>() vs static field ---

// Static field with GD.Load<>() — loads when the class is first accessed
// Like: import Enemy from "./Enemy.jsx" (static ES6 import)
// Use when: you know the path at write time and will definitely need it
private static readonly PackedScene EnemyScene = GD.Load<PackedScene>("res://scenes/enemies/enemy.tscn");
private static readonly PackedScene HudScene = GD.Load<PackedScene>("res://scenes/ui/hud.tscn");

// GD.Load<>() at call time — loads when the line executes
// Like: const Enemy = await import("./Enemy.jsx") (dynamic import)
// Use when: the path is determined at runtime, or you want lazy loading
public void LoadLevel(string levelName)
{
    var scene = GD.Load<PackedScene>($"res://scenes/levels/{levelName}.tscn");
    var instance = scene.Instantiate();
    AddChild(instance);
}


// --- Full instantiation lifecycle ---

// 1. Load the scene resource
private static readonly PackedScene EnemyScene = GD.Load<PackedScene>("res://scenes/enemies/enemy.tscn");

// 2. Create an instance (like new Component() or React.createElement)
var enemy = EnemyScene.Instantiate<CharacterBody2D>();
// At this point, the node exists in memory but is NOT in the scene tree.
// _Ready() has NOT been called. _Process() is NOT running.
// You CAN set properties (Position, [Export] properties, etc.)

// 3. Configure the instance (like setting props before rendering)
enemy.Position = new Vector2(500, 300);
enemy.Set("DangerTier", 2);
enemy.Set("EnemyColor", Colors.Yellow);

// 4. Add to the scene tree (like appendChild or ReactDOM.render)
GetNode<Node2D>("EntityContainer").AddChild(enemy);
// NOW _EnterTree() fires, then _Ready() fires, then _Process() starts.
// The enemy is alive and part of the game.

// 5. Later: remove from the tree (like element.remove() or unmounting)
enemy.QueueFree();
// Schedules removal at the end of the current frame.
// _ExitTree() fires. The node is freed from memory.
// Any references to it become invalid (will crash if used).


// --- Practical example: enemy spawning system ---
// (Replaces the Phaser prototype's spawnEnemy() + time.addEvent loop)

using Godot;

public partial class Dungeon : Node2D // Dungeon.cs
{
    private static readonly PackedScene EnemyScene = GD.Load<PackedScene>("res://scenes/enemies/enemy.tscn");
    private const int MaxEnemies = 14;

    private Node2D _entityContainer = null!;
    private Timer _spawnTimer = null!;

    public override void _Ready()
    {
        _entityContainer = GetNode<Node2D>("EntityContainer");
        _spawnTimer = GetNode<Timer>("SpawnTimer");

        // Spawn initial enemies (like the Phaser for-loop in create())
        for (int i = 0; i < 10; i++)
            SpawnEnemy();

        // Set up recurring spawn (like Phaser's time.addEvent with loop: true)
        _spawnTimer.WaitTime = 2.8; // 2800ms, same as Phaser prototype
        _spawnTimer.Timeout += OnSpawnTimerTimeout;
        _spawnTimer.Start();
    }

    private void OnSpawnTimerTimeout()
    {
        int enemyCount = GetTree().GetNodesInGroup("enemies").Count;
        if (enemyCount < MaxEnemies)
            SpawnEnemy();
    }

    private void SpawnEnemy()
    {
        var enemy = EnemyScene.Instantiate<CharacterBody2D>();
        enemy.Position = GetRandomEdgePosition();
        enemy.Set("DangerTier", (int)GD.RandRange(1, 3));
        _entityContainer.AddChild(enemy);
    }

    private Vector2 GetRandomEdgePosition()
    {
        // Replaces the Phaser spawnEnemy() edge calculation
        var tilemap = GetNode<TileMapLayer>("TileMapLayer");
        int edge = (int)GD.RandRange(0, 3);
        int mapWidth = 20;  // tiles
        int mapHeight = 20; // tiles
        Vector2I gridPos;

        switch (edge)
        {
            case 0: gridPos = new Vector2I((int)GD.RandRange(0, mapWidth), 0); break;
            case 1: gridPos = new Vector2I(mapWidth - 1, (int)GD.RandRange(0, mapHeight)); break;
            case 2: gridPos = new Vector2I((int)GD.RandRange(0, mapWidth), mapHeight - 1); break;
            default: gridPos = new Vector2I(0, (int)GD.RandRange(0, mapHeight)); break;
        }

        return tilemap.MapToLocal(gridPos);
    }
}
```

## Open Questions

- Should we document the entity-component pattern if the game grows beyond simple scene instancing?
- When does it make sense to introduce a proper state machine (for game flow, enemy behavior)?
- Should we add a section on Godot's tween system (replacement for Phaser tweens)?
- Would a section on Godot's built-in animation system (AnimationPlayer) be useful for this doc or belong in a separate doc?
- Should we document Godot's shader language basics for visual effects (slash effect, damage flash)?
