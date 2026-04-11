# Scene Tree — Complete Node Hierarchy

## Summary

Every `.tscn` scene in the game, with every node listed, every property value documented, every collision layer/mask specified, and every group assignment noted. This is the canonical reference for the Godot scene tree structure.

## Current State

> **Implemented.** All scenes below exist and are functional. Last verified against code as of Session 10+.

## Design

### Scene Inventory

| Scene File | Root Type | Script | Instanced By |
|------------|-----------|--------|--------------|
| `scenes/main.tscn` | Node | `scripts/Main.cs` | Project entry point (run scene) |
| `scenes/town.tscn` | Node2D | `scripts/Town.cs` | `Main.cs` at runtime via `SwapWorld()` |
| `scenes/dungeon.tscn` | Node2D | `scripts/Dungeon.cs` | `Main.cs` at runtime via `SwapWorld()` |
| `scenes/player.tscn` | CharacterBody2D | `scripts/Player.cs` | `Town.cs` / `Dungeon.cs` at runtime via `Instantiate()` |
| `scenes/enemy.tscn` | CharacterBody2D | `scripts/Enemy.cs` | `Dungeon.cs` at runtime via `Instantiate()` |
| `scenes/hud.tscn` | Control | `scripts/ui/Hud.cs` | `main.tscn` (child instance under UILayer) |
| `scenes/death_screen.tscn` | Control | `scripts/ui/DeathScreen.cs` | `main.tscn` (child instance under UILayer) |
| `scenes/pause_menu.tscn` | Control | `scripts/ui/PauseMenu.cs` | `main.tscn` (child instance under UILayer) |

---

### main.tscn

```
Main (Node) [Main.cs]
└── UILayer (CanvasLayer, layer=100)
    ├── HUD (instance of hud.tscn)
    ├── DeathScreen (instance of death_screen.tscn, visible=false)
    ├── PauseMenu (instance of pause_menu.tscn, visible=false)
    ├── ScreenTransition (Control) [ScreenTransition.cs]
    ├── DebugPanel (Control) [DebugPanel.cs, process_mode=ALWAYS]
    ├── NpcPanel (Control) [NpcPanel.cs]
    ├── Toast (Control) [Toast.cs]
    ├── AscendDialog (Control) [AscendDialog.cs, process_mode=ALWAYS]
    ├── DialogueBox (Control) [DialogueBox.cs, process_mode=ALWAYS]
    └── ShopWindow (Control) [ShopWindow.cs, process_mode=ALWAYS]
```

World scenes (Town or Dungeon) are **not** hardcoded children of Main. They are loaded dynamically by `Main.cs.SwapWorld()` and inserted as the first child (index 0) at runtime. At any given time, `_currentWorld` is either a Town or Dungeon instance.

At startup, `Main.cs._Ready()` creates a `SplashScreen` (programmatic, added to UILayer), which transitions to `ClassSelect` (also programmatic), which then calls `Main.Instance.LoadTown()`.

#### Node Details

**Main (Node)**
- **Type:** Node (the simplest possible root -- no transform, no rendering)
- **Script:** `res://scripts/Main.cs`
- **Static instance:** `Main.Instance` singleton pattern, set in `_Ready()`
- **Responsibilities:**
  - Connects to `GameState.PlayerDied` signal in `_Ready()`. When fired, shows DeathScreen and pauses the tree.
  - Applies `GlobalTheme` to all Control children of UILayer.
  - `LoadTown()` / `LoadDungeon()` swap the world child via `SwapWorld()`.
  - Shows splash screen and class selection on first launch.
- **No hardcoded Dungeon child.** Any existing "Dungeon" or "Town" child found in `_Ready()` is freed.

**UILayer (CanvasLayer)**
- **Type:** CanvasLayer
- **Property:** `layer = 100`
- **Why CanvasLayer:** CanvasLayer renders its children on a separate canvas that is unaffected by Camera2D zoom and position. The HUD and all overlays must remain fixed on screen regardless of where the camera is in the world.
- **Why layer 100:** High layer number ensures UI draws on top of everything. Layers are ordered numerically -- the default world layer is 0, so 100 guarantees the UI is above any future CanvasLayer additions.
- **No script:** UILayer is a pure organizational container.

**HUD (Control)**
- **Type:** Instance of `scenes/hud.tscn`
- See **hud.tscn** section below for full subtree.

**DeathScreen (Control)**
- **Type:** Instance of `scenes/death_screen.tscn`
- **Property:** `visible = false` (hidden until player dies)
- See **death_screen.tscn** section below for full subtree.

**PauseMenu (Control)**
- **Type:** Instance of `scenes/pause_menu.tscn`
- **Property:** `visible = false`, `process_mode = PROCESS_MODE_ALWAYS`
- See **pause_menu.tscn** section below for full subtree.

**ScreenTransition (Control)**
- **Script:** `res://scripts/ui/ScreenTransition.cs`
- Full-rect Control with `mouse_filter = IGNORE`. Builds its UI (overlay + labels) programmatically in `_Ready()`.
- Singleton: `ScreenTransition.Instance`. Used for fade-to-black transitions between floors and town/dungeon swaps.

**DebugPanel (Control)**
- **Script:** `res://scripts/ui/DebugPanel.cs`
- Full-rect Control, `process_mode = PROCESS_MODE_ALWAYS`. Toggled with F3. Shows HP, level, XP, floor, damage, enemy count, kill stats, session time.

**NpcPanel (Control)**
- **Script:** `res://scripts/ui/NpcPanel.cs`
- Full-rect Control. Singleton: `NpcPanel.Instance`. Shows NPC name, greeting, and service button when player interacts with an NPC. Builds UI programmatically.

**Toast (Control)**
- **Script:** `res://scripts/ui/Toast.cs`
- Full-rect Control. Singleton: `Toast.Instance`. Shows stacking toast notifications at bottom-center. Max 5 visible.

**AscendDialog (Control)**
- **Script:** `res://scripts/ui/AscendDialog.cs`
- Full-rect Control, `process_mode = PROCESS_MODE_ALWAYS`. Singleton: `AscendDialog.Instance`. Modal dialog for stairs-up: return to town, go up one floor, or select a specific floor.

**DialogueBox (Control)**
- **Script:** `res://scripts/ui/DialogueBox.cs`
- Full-rect Control, `process_mode = PROCESS_MODE_ALWAYS`. Singleton: `DialogueBox.Instance`. Visual novel-style typewriter dialogue with portraits. Advanced with S / Space / Enter.

**ShopWindow (Control)**
- **Script:** `res://scripts/ui/ShopWindow.cs`
- Full-rect Control, `process_mode = PROCESS_MODE_ALWAYS`. Singleton: `ShopWindow.Instance`. JRPG-style buy/sell shop with two-panel layout (item list + description). Currently only the Shopkeeper NPC opens it.

---

### town.tscn

```
Town (Node2D) [Town.cs]
├── TileMapLayer (isometric tileset, y_sort_enabled=true)
└── Entities (Node2D, y_sort_enabled=true)
    └── (Player instanced at runtime)
    └── (NPCs instanced at runtime: Shopkeeper, Blacksmith, Guild Master, Teleporter, Banker)
    └── (Dungeon entrance object created at runtime)
```

#### Node Details

**Town (Node2D)**
- **Type:** Node2D
- **Script:** `res://scripts/Town.cs`
- **Fixed size:** 16x12 tiles (defined in `Constants.Town.Width/Height`)
- **Responsibilities:**
  - Creates TileSet and paints the town grid programmatically (floor source 0, wall source 1)
  - Spawns player at town center
  - Spawns 5 NPCs at fixed tile positions (each is a `Npc` StaticBody2D created programmatically)
  - Creates dungeon entrance at bottom of town (stairs sprite + label + trigger area)
  - When player enters dungeon entrance trigger, plays ScreenTransition and calls `Main.Instance.LoadDungeon()`
- **NPC layout:** Shopkeeper (4,3), Blacksmith (12,3), Guild Master (4,7), Teleporter (12,7), Banker (8,3)

**TileMapLayer / Entities** -- same structure as dungeon.tscn (see below), but no SpawnTimer.

---

### dungeon.tscn

```
Dungeon (Node2D) [Dungeon.cs]
├── TileMapLayer (isometric tileset, y_sort_enabled=true)
├── Entities (Node2D, y_sort_enabled=true)
│   └── (Player instanced here at runtime)
│   └── (Enemies instanced here at runtime)
│   └── (StairsDown + StairsUp objects created at runtime)
└── SpawnTimer (Timer, wait_time=2.8, autostart=true)
```

#### Node Details

**Dungeon (Node2D)**
- **Type:** Node2D
- **Script:** `res://scripts/Dungeon.cs`
- **Why Node2D:** Dungeon is a spatial container for all gameplay elements. Node2D provides a transform so the entire dungeon can be repositioned if needed (e.g., screen transitions).
- **Responsibilities:**
  - Creates the TileSet and paints tiles programmatically in `_Ready()` via `SetupTileset()` + `GenerateFloor()`
  - Proc-gen room: random size (18-30 base, grows with floor depth up to +6), random floor tile variations (4 textures), wall border
  - Spawns player at the up-stairs position
  - Spawns initial enemies (8 on game start, per `Constants.Spawning.InitialEnemies`)
  - Creates stairs-down and stairs-up objects with collision bodies and trigger areas
  - Connects to `SpawnTimer.Timeout` for periodic enemy spawning
  - Connects to `EventBus.EnemyDefeated` to schedule respawn after 1.4s delay
  - Manages the enemy soft cap (max 14 active enemies)
  - `PerformFloorDescent()` clears enemies and tiles, regenerates floor, repositions player and stairs
  - Floor 1 stairs-up shows "Return to Town"; deeper floors show "STAIRS UP"

**TileMapLayer**
- **Type:** TileMapLayer
- **Property:** `y_sort_enabled = true`
- **Why TileMapLayer:** Renders the isometric floor and wall tiles. TileMapLayer is Godot 4's replacement for the deprecated TileMap node. It uses a TileSet resource configured for isometric tile shape.
- **Why y_sort_enabled:** Enables depth sorting so entities that are lower on screen (higher Y in isometric space) render in front. This is critical for correct isometric visual ordering.
- **TileSet:** Created programmatically in `Dungeon.cs._Ready()` -- see `docs/objects/tilemap.md` for full TileSet specification.
- **Key TileSet properties:**
  - `tile_shape = TileSet.TILE_SHAPE_ISOMETRIC`
  - `tile_size = Vector2i(64, 32)` -- matches ISS floor tile dimensions (64x32 isometric diamond)
  - Wall blocks are 64x64 (isometric cube); floor tiles are 64x32
  - Physics layer 0 added for wall collision
- **Tileset source:** Isometric Stone Soup (ISS) environment tiles in `assets/isometric/tiles/stone-soup/`. ISS defines the project-wide tile grid standard. See `docs/reference/game-development.md` Section 3 for full details.
- See `docs/objects/tilemap.md` for tile types, collision polygons, and painting algorithm.

**Entities (Node2D)**
- **Type:** Node2D
- **Property:** `y_sort_enabled = true`
- **Why separate from TileMapLayer:** Entities (player, enemies, effects) need to be y-sorted independently of tile rendering. Keeping them in their own y-sorted container ensures correct draw order among moving objects. Slash effects are also added here so they y-sort with entities.
- **Children at runtime:**
  - Player instance (added first in `_Ready()`)
  - Enemy instances (added by `SpawnEnemy()`)
  - Slash effect Polygon2D nodes (added by `Player.DrawSlash()`, short-lived)

**SpawnTimer (Timer)**
- **Type:** Timer
- **Properties:**
  - `wait_time = 2.8` (seconds between spawn attempts)
  - `autostart = true` (starts ticking immediately when scene enters tree)
  - `one_shot = false` (repeats indefinitely)
- **Why Timer node:** Timer nodes emit a `Timeout` signal that `Dungeon.cs` connects to in `_Ready()`. This replaces Phaser's `this.time.addEvent({delay: 2800, loop: true, ...})`.
- **Signal connection:** `SpawnTimer.Timeout += OnSpawnTimerTimeout;`
- **Behavior:** On each timeout, checks if the active enemy count is below the soft cap (14). If so, calls `SpawnEnemy()`. If at or above cap, does nothing (the timer still repeats -- it checks again next tick).

---

### player.tscn

```
Player (CharacterBody2D) [Player.cs]
│   collision_layer = 2 (bit 1, binary: 0b10)
│   collision_mask = 5 (bits 0+2, binary: 0b101)
│   motion_mode = FLOATING
│   groups: ["player"] (added in _Ready)
├── CollisionShape2D
│   └── shape: CircleShape2D(radius=12.0)
├── Sprite (Sprite2D)
│   └── texture: res://assets/characters/player/warrior/rotations/south.png
│   └── texture_filter = NEAREST
│   └── offset = Vector2(0, -30)
├── Camera2D
│   └── zoom = Vector2(2, 2)
│   └── position_smoothing_enabled = true
│   └── position_smoothing_speed = 5.0
│   └── process_callback = PHYSICS
└── AttackRange (Area2D)
    │   collision_layer = 0 (detects, does not broadcast)
    │   collision_mask = 4 (bit 2, detects enemy layer)
    │   monitoring = true
    │   monitorable = false
    └── AttackShape (CollisionShape2D)
        └── shape: CircleShape2D(radius=78.0) — resized at runtime per class
```

#### Node Details

**Player (CharacterBody2D)**
- **Type:** CharacterBody2D
- **Script:** `res://scripts/Player.cs`
- **Properties:**
  - `collision_layer = 2` -- bit 1, identifies this body as "player"
  - `collision_mask = 5` -- bits 0+2 (binary 0b101), collides with walls (bit 0) and enemies (bit 2)
  - `motion_mode = MOTION_MODE_FLOATING` (value 1) -- no gravity, free 2D movement
- **Groups:** `["player"]` -- added in `_Ready()` via `AddToGroup(Constants.Groups.Player)`.
- **Runtime init in `_Ready()`:** loads class attack config, resizes AttackShape to primary range, loads 8-directional sprites for selected class, connects to `GameState.StatsChanged`, starts 1.5s grace period.
- **Movement:** Screen-space arrow keys (NOT isometric). Speed: 190 px/s. `DirectionalSprite.UpdateSprite()` swaps texture per frame.
- **Combat:** Auto-attack polls `AttackRange.GetOverlappingBodies()` each physics frame. Uses `AttackConfig` for melee (instant + slash) or projectile (spawns `Projectile`). Mage has staff melee fallback.

**CollisionShape2D**
- **Shape:** `CircleShape2D` with `radius = 12.0`
- **Position:** `Vector2(0, 0)` (centered on the CharacterBody2D origin)

**Sprite (Sprite2D)**
- **Type:** Sprite2D (pixel art sprites, NOT Polygon2D)
- **Properties:**
  - `texture` -- default: warrior south-facing. Replaced at runtime with class-specific 8-directional rotations.
  - `texture_filter = NEAREST` (value 0) -- pixel-perfect rendering
  - `offset = Vector2(0, -30)` -- raises sprite above collision circle to align feet with tile surface

**Camera2D**
- **Properties:**
  - `zoom = Vector2(2, 2)`, `position_smoothing_enabled = true`, `position_smoothing_speed = 5.0`
  - `process_callback = PHYSICS` (value 0) -- updates during physics step

**AttackRange (Area2D)**
- **Properties:**
  - `collision_layer = 0`, `collision_mask = 4` (detects enemies), `monitoring = true`, `monitorable = false`
- **Runtime resize:** AttackShape radius set to class primary attack range (Warrior: 78, Ranger: 250, Mage: 200).

**AttackShape (CollisionShape2D)**
- **Shape:** `CircleShape2D` with `radius = 78.0` (scene default, resized at runtime per class)
- **Position:** `Vector2(0, 0)`

---

### enemy.tscn

```
Enemy (CharacterBody2D) [Enemy.cs]
│   collision_layer = 4 (bit 2, binary: 0b100)
│   collision_mask = 3 (bits 0+1, binary: 0b011)
│   motion_mode = FLOATING
│   groups: ["enemies"] (added in _Ready)
│   [Export] Level: int = 1
│   [Export] SpeciesIndex: int = 0 (Skeleton)
├── CollisionShape2D
│   └── shape: CircleShape2D(radius=10.0)
├── Sprite (Sprite2D)
│   └── texture: res://assets/characters/enemies/skeleton/rotations/south.png
│   └── texture_filter = NEAREST
│   └── scale = Vector2(0.7, 0.7)
│   └── offset = Vector2(0, -26)
├── LevelLabel (Label)
│   └── text: "Lv.1" (set at runtime)
│   └── position = Vector2(0, -56)
│   └── horizontal_alignment = CENTER
│   └── font_color = Color(1, 1, 1, 0.9), outline_size = 3, font_size = 11
├── HitArea (Area2D)
│   │   collision_layer = 0 (does not broadcast)
│   │   collision_mask = 2 (bit 1, detects player layer)
│   │   monitoring = true
│   │   monitorable = false
│   └── HitShape (CollisionShape2D)
│       └── shape: CircleShape2D(radius=15.0)
└── HitCooldownTimer (Timer)
    └── wait_time = 0.7, one_shot = true
```

#### Node Details

**Enemy (CharacterBody2D)**
- **Type:** CharacterBody2D
- **Script:** `res://scripts/Enemy.cs`
- **Properties:**
  - `collision_layer = 4` -- bit 2, identifies this body as "enemy"
  - `collision_mask = 3` -- bits 0+1 (binary 0b011), collides with walls (bit 0) AND player (bit 1)
  - `motion_mode = MOTION_MODE_FLOATING` (value 1) -- no gravity, free 2D movement
- **Exported Properties:**
  - `[Export] public int Level { get; set; } = 1` -- set by `Dungeon.SpawnEnemy()` based on floor scaling formula
  - `[Export] public int SpeciesIndex { get; set; } = 0` -- indexes into `Constants.Assets.EnemySpeciesRotations` (0=Skeleton, 1=Goblin)
- **Groups:** `["enemies"]` -- added in `_Ready()` via `AddToGroup()`.
- **Runtime init in `_Ready()`:** computes HP/speed/damage/XP from level via `Constants.EnemyStats` formulas, loads species-specific 8-directional sprites, sets color based on level gap to player (gradient from grey through green/yellow/red), connects to `GameState.StatsChanged` to update color as player levels.
- **Color system:** NOT tier-based. Uses a continuous 8-anchor gradient based on `enemy.Level - player.Level` gap: grey (-10), blue (-6), cyan (-3), green (0), yellow (+3), gold (+6), orange (+8), red (+10). Colors lerp between anchors.
- **Despawn safety:** If enemy is >800px from player, it `QueueFree()`s.
- **Signal connections use `Connect()`:** `_hitArea.Connect(Area2D.SignalName.BodyEntered, new Callable(this, MethodName.OnHitAreaBodyEntered))`. This is the project convention (not C# `+=` event syntax).

**CollisionShape2D**
- **Shape:** `CircleShape2D` with `radius = 10.0`
- **Position:** `Vector2(0, 0)`

**Sprite (Sprite2D)**
- **Type:** Sprite2D (NOT Polygon2D -- pixel art sprites are in use)
- **Properties:**
  - `texture_filter = NEAREST` (value 0) -- pixel-perfect rendering
  - `scale = Vector2(0.7, 0.7)` -- enemies are 70% size of player sprites
  - `offset = Vector2(0, -26)` -- raises sprite above collision circle
- **Runtime behavior:** `DirectionalSprite.UpdateSprite()` swaps texture based on movement velocity. Modulate color set by `UpdateColor()` (level-gap gradient).

**LevelLabel (Label)**
- **Type:** Label -- shows "Lv.N" above the enemy
- **Properties:**
  - `position = Vector2(0, -56)`, centered horizontally (60px wide)
  - `font_color = Color(1, 1, 1, 0.9)`, `outline_size = 3`, `font_size = 11`
  - Modulate color matches the sprite color (updates with level gap gradient)

**HitArea (Area2D)**
- **Properties:** `collision_layer = 0`, `collision_mask = 2` (detects player), `monitoring = true`, `monitorable = false`
- **Signal:** `BodyEntered` connected to `OnHitAreaBodyEntered` via `Connect()`. Respects player invincibility (`IsInvincible`).
- **Damage:** Uses `DealDamageTo()` which checks player invincibility, calls `GameState.Instance.TakeDamage()`, triggers damage flash, spawns floating damage number.

**HitShape (CollisionShape2D)**
- **Shape:** `CircleShape2D` with `radius = 15.0` (slightly larger than body collision for a danger zone)

**HitCooldownTimer (Timer)**
- **Properties:** `wait_time = 0.7`, `one_shot = true`
- **Signal:** `Timeout` connected to `OnHitCooldownTimerTimeout` via `Connect()`. Re-checks overlap and deals damage again if player is still in range.

---

### hud.tscn

```
HUD (Control) [Hud.cs]
│   anchor_right = 1.0, anchor_bottom = 1.0 (full rect)
│   mouse_filter = MOUSE_FILTER_IGNORE
└── PanelContainer
    │   anchor_left = 0, anchor_top = 0
    │   offset_left = 12, offset_top = 12
    │   custom_minimum_size = Vector2(220, 0)
    │   StyleBoxFlat:
    │       bg_color = Color(0.086, 0.106, 0.157, 0.75) = rgba(22,27,40,0.75)
    │       border_color = Color(0.961, 0.784, 0.420, 0.3) = rgba(245,200,107,0.3)
    │       border_width_left = 1, border_width_top = 1
    │       border_width_right = 1, border_width_bottom = 1
    │       corner_radius_top_left = 10, corner_radius_top_right = 10
    │       corner_radius_bottom_left = 10, corner_radius_bottom_right = 10
    └── MarginContainer (margin_left=10, margin_top=10, margin_right=10, margin_bottom=10)
        └── VBoxContainer (theme_override_constants/separation = 4)
            ├── TitleLabel (Label)
            │   text = "A DUNGEON IN THE MIDDLE OF NOWHERE"
            │   theme_override_colors/font_color = Color("#f5c86b")
            │   theme_override_font_sizes/font_size = 13
            │   uppercase = true
            ├── ControlsLabel (Label)
            │   text = "Move: WASD / Arrow keys\nAuto-attack: nearest enemy in range"
            │   theme_override_colors/font_color = Color("#b6bfdb")
            │   theme_override_font_sizes/font_size = 12
            └── StatsLabel (Label)
                text = "HP: 100 | XP: 0 | LVL: 1 | Floor: 1"
                theme_override_colors/font_color = Color("#ecf0ff")
                theme_override_font_sizes/font_size = 12
```

#### Node Details

**HUD (Control)**
- **Type:** Control
- **Script:** `res://scripts/ui/Hud.cs`
- **Properties:**
  - `anchor_right = 1.0`, `anchor_bottom = 1.0` -- anchors stretch the Control to fill the entire parent (UILayer's viewport). This makes the HUD a full-screen invisible container.
  - `mouse_filter = MOUSE_FILTER_IGNORE` -- the HUD root does not consume mouse events. Clicks pass through to the game world. This is critical: without it, the full-rect Control would eat all mouse input.
- **Why Control:** Control is the base type for all Godot UI nodes. It provides the anchor/margin layout system used for positioning the panel in the top-left corner.
- **Responsibilities:** Connects to `GameState.StatsChanged` in `_Ready()`. When the signal fires, updates `StatsLabel.Text` with current `GameState.Hp`, `GameState.Xp`, `GameState.Level`, and `GameState.FloorNumber`.

**PanelContainer**
- **Type:** PanelContainer
- **Properties:**
  - `anchor_left = 0`, `anchor_top = 0` -- positioned relative to top-left corner
  - `offset_left = 12`, `offset_top = 12` -- 12px inset from the top-left, matching Phaser's `left: 12px; top: 12px`
  - `custom_minimum_size = Vector2(220, 0)` -- minimum width to prevent the panel from collapsing when text is short
  - Theme override: `StyleBoxFlat` for the `panel` style
- **StyleBoxFlat properties:**
  - `bg_color = Color(0.086, 0.106, 0.157, 0.75)` -- `rgba(22, 27, 40, 0.75)`, a dark blue-gray at 75% opacity. Matches CSS `var(--panel)`.
  - `border_color = Color(0.961, 0.784, 0.420, 0.3)` -- `rgba(245, 200, 107, 0.3)`, a gold accent at 30% opacity. Matches CSS `var(--panel-border)`.
  - `border_width_*: 1` -- 1px border on all four sides
  - `corner_radius_*: 10` -- 10px rounded corners on all four corners, matching CSS `border-radius: 10px`
- **Why PanelContainer:** PanelContainer renders a background panel (via StyleBox) and automatically sizes to fit its single child. This replaces the HTML `#overlay` div with its CSS background and border.

**MarginContainer**
- **Type:** MarginContainer
- **Properties:**
  - `margin_left = 10`, `margin_top = 10`, `margin_right = 10`, `margin_bottom = 10`
- **Why MarginContainer:** Adds 10px padding inside the PanelContainer, matching the CSS `padding: 10px 12px`. This separates content from the panel border.

**VBoxContainer**
- **Type:** VBoxContainer
- **Properties:**
  - `theme_override_constants/separation = 4` -- 4px vertical gap between child labels
- **Why VBoxContainer:** Stacks the title, controls, and stats labels vertically with consistent spacing. Matches the HTML structure of `<h1>` + `<p>` + `<p id="hud">`.

**TitleLabel (Label)**
- **Type:** Label
- **Properties:**
  - `text = "A DUNGEON IN THE MIDDLE OF NOWHERE"`
  - `theme_override_colors/font_color = Color("#f5c86b")` -- gold accent color, matches CSS `var(--accent)` = `#f5c86b`
  - `theme_override_font_sizes/font_size = 13` -- matches CSS `font-size: 13px`
  - `uppercase = true` -- matches CSS `text-transform: uppercase`
- **Why Label:** Simple text display. The title never changes at runtime.

**ControlsLabel (Label)**
- **Type:** Label
- **Properties:**
  - `text = "Move: WASD / Arrow keys\nAuto-attack: nearest enemy in range"` -- two lines of control instructions
  - `theme_override_colors/font_color = Color("#b6bfdb")` -- muted text color, matches CSS `var(--muted)` = `#b6bfdb`
  - `theme_override_font_sizes/font_size = 12`
- **Why separate from title:** Different font color (muted vs. accent) requires a separate Label node.

**StatsLabel (Label)**
- **Type:** Label
- **Properties:**
  - `text = "HP: 100 | XP: 0 | LVL: 1 | Floor: 1"` -- default text, updated at runtime
  - `theme_override_colors/font_color = Color("#ecf0ff")` -- bright ink color, matches CSS `var(--ink)` = `#ecf0ff`
  - `theme_override_font_sizes/font_size = 12`
- **Why separate label:** This is the only label that updates at runtime. Keeping it as a dedicated node means `Hud.cs` only needs to update one label's text when stats change:
  ```
  _statsLabel.Text = $"HP: {GameState.Hp} | XP: {GameState.Xp} | LVL: {GameState.Level} | Floor: {GameState.FloorNumber}";
  ```

---

### death_screen.tscn

```
DeathScreen (Control) [DeathScreen.cs]
│   visible = false
│   full rect (anchors_preset=15)
│   process_mode = PROCESS_MODE_ALWAYS
├── Overlay (ColorRect)
│   color = Color(0, 0, 0, 0.75)
└── CenterContainer
    └── VBoxContainer (separation = 16)
        ├── TitleLabel (Label) — "You Died", font_size=48, color=Color(1, 0.882, 0.69)
        ├── InstructionLabel (Label) — "Press R to restart", font_size=20
        ├── RestartButton (Button) — "Restart (R)", min_size=180x40
        └── QuitButton (Button) — "Quit Game (Esc)", min_size=180x40
```

#### Node Details

**DeathScreen (Control)**
- **Script:** `res://scripts/ui/DeathScreen.cs`
- `visible = false`, `process_mode = PROCESS_MODE_ALWAYS`
- **Input:** R key restarts (calls `GameState.Reset()`, hides self, unpauses, loads town via `Main.Instance.LoadTown()`). Esc quits game.
- **Signal connections:** RestartButton and QuitButton both use `Connect(BaseButton.SignalName.Pressed, new Callable(...))`.
- **Restart does NOT reload scene.** Instead it resets state and swaps world to town.

**Buttons:** Both RestartButton and QuitButton use `StyleBoxFlat` with accent gold bg (`0.961, 0.784, 0.42`), dark text color (`0.086, 0.106, 0.157`), 6px corner radius. Hover state is a lighter gold (`0.98, 0.85, 0.55`).

---

### pause_menu.tscn

```
PauseMenu (Control) [PauseMenu.cs]
│   visible = false
│   full rect (anchors_preset=15)
│   process_mode = PROCESS_MODE_ALWAYS
├── Overlay (ColorRect)
│   color = Color(0, 0, 0, 0.5)
└── CenterContainer
    └── PanelContainer (min_size=280, StyleBoxFlat bg=0.92 opacity)
        └── MarginContainer
            └── VBoxContainer (separation = 12)
                ├── TitleLabel (Label) — "PAUSED", font_size=24, accent color
                ├── Separator (HSeparator)
                ├── ResumeButton (Button) — "Resume", min_height=40
                └── QuitButton (Button) — "Quit Game", min_height=40
```

#### Node Details

**PauseMenu (Control)**
- **Script:** `res://scripts/ui/PauseMenu.cs`
- `visible = false`, `process_mode = PROCESS_MODE_ALWAYS`
- **Input:** Esc toggles the pause menu. Blocked when DeathScreen is visible (checks sibling node).
- **Resume:** Hides menu, unpauses tree. **Quit:** Calls `GetTree().Quit()`.
- **Panel style:** Dark panel (bg 0.92 opacity), gold border (0.5 opacity), 2px border, 8px corners.

---

### Collision Layer Reference

| Bit | Layer Value | Name | Used By |
|-----|-------------|------|---------|
| 0 | 1 | Walls/Tiles | TileMapLayer wall tile physics, NPC StaticBody2D, Stairs StaticBody2D |
| 1 | 2 | Player | Player CharacterBody2D |
| 2 | 4 | Enemies | Enemy CharacterBody2D |

**Collision Matrix:**

| Node | Layer (what I am) | Mask (what I collide with) | Effect |
|------|-------------------|---------------------------|--------|
| Player body | 2 | 5 (walls + enemies) | Player slides along walls and bumps into enemies |
| Enemy body | 4 | 3 (walls + player) | Enemies slide along walls and bump into player |
| Player AttackRange | 0 | 4 | Detects enemies for auto-attack |
| Enemy HitArea | 0 | 2 | Detects player for contact damage |
| NPC body | 1 (walls) | 0 | NPCs are solid obstacles (StaticBody2D) |
| Stairs body | 1 (walls) | 0 | Stairs are solid obstacles (StaticBody2D) |
| Stairs trigger | 0 | 2 (player) | Detects player approach for floor transition |
| Projectile | 0 | 4 (enemies) | Arrow/bolt detects enemy hit |

**Note on player-enemy collision:** Both player (`mask=5`) and enemy (`mask=3`) include each other in their masks. This means they physically collide and push each other -- enemies cannot walk through the player, and the player bumps into enemies.

---

### Group Reference

| Group Name | Members | Queried By | Purpose |
|------------|---------|------------|---------|
| `"player"` | Player CharacterBody2D | `Enemy.cs` via `GetTree().GetFirstNodeInGroup("player")` | Enemies find the player to chase and damage check |
| `"enemies"` | All Enemy CharacterBody2D instances | `Player.cs` via `GetOverlappingBodies()` filtered by group | Player finds enemies in attack range |

## Implementation Notes

- All scenes are saved as `.tscn` files in `res://scenes/`. Scripts are in `res://scripts/` with subdirectories (`ui/`, `logic/`, `autoloads/`).
- Scene paths are centralized in `Constants.Assets` (e.g., `Constants.Assets.PlayerScene`, `Constants.Assets.EnemyScene`).
- PackedScenes are loaded via `GD.Load<PackedScene>()` as static readonly fields in `Main.cs`, `Dungeon.cs`, and `Town.cs`.
- Y-sorting is enabled on both TileMapLayer and Entities in both town and dungeon scenes.
- The UILayer CanvasLayer at **layer 100** ensures UI is always on top regardless of Camera2D zoom or position.
- Signal connections throughout the project use `Connect()` with `Callable`, NOT the C# `+=` event syntax. This is the project convention.
- Many UI nodes are built programmatically in `_Ready()` rather than defined in `.tscn` files (SplashScreen, ClassSelect, DebugPanel, NpcPanel, Toast, AscendDialog, DialogueBox, ShopWindow, ScreenTransition). Only the node itself is declared in `main.tscn`; its children are created in code.
- `GlobalTheme.Create()` is applied to all UILayer Control children in `Main._Ready()`.
- Multiple UI singletons use the `Instance` pattern: `ScreenTransition.Instance`, `Toast.Instance`, `NpcPanel.Instance`, `AscendDialog.Instance`, `DialogueBox.Instance`, `ShopWindow.Instance`.

## Open Questions

- Should enemies have a separate HealthBar node (ProgressBar or custom drawing) for visual HP feedback?
- Should the HUD panel use a proper Theme resource (`.tres`) instead of per-node theme overrides, for easier global style changes?
