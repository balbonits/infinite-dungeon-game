# Scene Tree — Complete Node Hierarchy

## Summary

Every `.tscn` scene in the game, with every node listed, every property value documented, every collision layer/mask specified, and every group assignment noted. This is the canonical reference for the Godot scene tree structure.

## Current State

Six scenes form the complete game: `main.tscn` (root), `dungeon.tscn` (gameplay), `player.tscn` (player character), `enemy.tscn` (enemy template), `hud.tscn` (stats overlay), and `death_screen.tscn` (game over UI). All scenes use placeholder Polygon2D diamonds for sprites -- no pixel art assets yet.

## Design

### Scene Inventory

| Scene File | Root Type | Script | Instanced By |
|------------|-----------|--------|--------------|
| `scenes/main.tscn` | Node | `scripts/Main.cs` | Project entry point (run scene) |
| `scenes/dungeon.tscn` | Node2D | `scripts/Dungeon.cs` | `main.tscn` (child instance) |
| `scenes/player.tscn` | CharacterBody2D | `scripts/Player.cs` | `Dungeon.cs` at runtime via `Instantiate()` |
| `scenes/enemy.tscn` | CharacterBody2D | `scripts/Enemy.cs` | `Dungeon.cs` at runtime via `Instantiate()` |
| `scenes/hud.tscn` | Control | `scripts/ui/Hud.cs` | `main.tscn` (child instance under UILayer) |
| `scenes/death_screen.tscn` | Control | `scripts/ui/DeathScreen.cs` | `main.tscn` (child instance under UILayer) |

---

### main.tscn

```
Main (Node) [Main.cs]
├── Dungeon (instance of dungeon.tscn)
└── UILayer (CanvasLayer, layer=10)
    ├── HUD (instance of hud.tscn)
    └── DeathScreen (instance of death_screen.tscn, visible=false)
```

#### Node Details

**Main (Node)**
- **Type:** Node (the simplest possible root -- no transform, no rendering)
- **Script:** `res://scripts/Main.cs`
- **Why Node:** The root doesn't need spatial positioning or rendering. It exists purely to own the Dungeon and UI subtrees. Using Node instead of Node2D avoids an unnecessary transform and signals that this is a logical container, not a spatial entity.
- **Responsibilities:** Connects to `GameState.PlayerDied` signal in `_Ready()`. When the signal fires, it tells `DeathScreen` to show and pauses the scene tree (`GetTree().Paused = true`).

**Dungeon (Node2D)**
- **Type:** Instance of `scenes/dungeon.tscn`
- **Why instanced as child:** Dungeon is always present in the main scene. If the game later needs to swap dungeon layouts (e.g., town vs. dungeon), this child instance can be replaced.
- See **dungeon.tscn** section below for full subtree.

**UILayer (CanvasLayer)**
- **Type:** CanvasLayer
- **Property:** `layer = 10`
- **Why CanvasLayer:** CanvasLayer renders its children on a separate canvas that is unaffected by Camera2D zoom and position. The HUD and death screen must remain fixed on screen regardless of where the camera is in the dungeon.
- **Why layer 10:** High layer number ensures UI draws on top of everything. Layers are ordered numerically -- the default world layer is 0, so 10 guarantees the UI is above any future CanvasLayer additions (e.g., a minimap at layer 5).
- **No script:** UILayer is a pure organizational container.

**HUD (Control)**
- **Type:** Instance of `scenes/hud.tscn`
- See **hud.tscn** section below for full subtree.

**DeathScreen (Control)**
- **Type:** Instance of `scenes/death_screen.tscn`
- **Property:** `visible = false` (hidden until player dies)
- See **death_screen.tscn** section below for full subtree.

---

### dungeon.tscn

```
Dungeon (Node2D) [Dungeon.cs]
├── TileMapLayer (isometric tileset, y_sort_enabled=true)
├── Entities (Node2D, y_sort_enabled=true)
│   └── (Player instanced here at runtime)
│   └── (Enemies instanced here at runtime)
└── SpawnTimer (Timer, wait_time=2.8, autostart=true)
```

#### Node Details

**Dungeon (Node2D)**
- **Type:** Node2D
- **Script:** `res://scripts/Dungeon.cs`
- **Why Node2D:** Dungeon is a spatial container for all gameplay elements. Node2D provides a transform so the entire dungeon can be repositioned if needed (e.g., screen transitions).
- **Responsibilities:**
  - Creates the TileSet and paints tiles programmatically in `_Ready()`
  - Instances the player scene and adds it to the Entities container
  - Spawns initial enemies (10 on game start)
  - Connects to `SpawnTimer.Timeout` for periodic enemy spawning
  - Connects to `EventBus.EnemyDefeated` to schedule respawn after 1.4s delay
  - Manages the enemy soft cap (max 14 active enemies)

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
│   collision_mask = 1 (bit 0, binary: 0b01)
│   groups: ["player"]
├── CollisionShape2D
│   └── shape: CircleShape2D(radius=12.0)
├── Sprite (Polygon2D)
│   └── color: Color(0.557, 0.839, 1.0, 1.0) = #8ed6ff
│   └── polygon: PackedVector2Array[(0,-16), (12,0), (0,16), (-12,0)]
├── Camera2D
│   └── position_smoothing_enabled = true
│   └── position_smoothing_speed = 5.0
│   └── zoom = Vector2(2, 2)
└── AttackRange (Area2D)
    │   collision_layer = 0 (detects, does not broadcast)
    │   collision_mask = 4 (bit 2, detects enemy layer)
    │   monitoring = true
    │   monitorable = false
    └── AttackShape (CollisionShape2D)
        └── shape: CircleShape2D(radius=78.0)
```

#### Node Details

**Player (CharacterBody2D)**
- **Type:** CharacterBody2D
- **Script:** `res://scripts/Player.cs`
- **Properties:**
  - `collision_layer = 2` -- bit 1, identifies this body as "player" for other nodes querying layer 2
  - `collision_mask = 1` -- bit 0, collides with wall/tile physics bodies on layer 1
  - `motion_mode = MOTION_MODE_FLOATING` -- no gravity, free 2D movement in all directions
- **Groups:** `["player"]` -- added in `_Ready()` or set in the scene editor. Used by enemies to find the player via `GetTree().GetFirstNodeInGroup("player")`.
- **Why CharacterBody2D:** Provides `MoveAndSlide()` which handles wall collision response automatically. Unlike RigidBody2D, it doesn't simulate physics forces -- movement is entirely code-driven, matching the Phaser prototype's `setVelocity()` approach. Unlike StaticBody2D, it can move.
- **Collision behavior:** The player collides with walls (mask bit 0) but does NOT collide with enemies (no mask bit 2). Enemies overlap the player and detect contact via their own Area2D. This is intentional: enemies should be able to crowd around the player without pushing them.
- See `docs/objects/player.md` for full method pseudocode and movement system.

**CollisionShape2D**
- **Type:** CollisionShape2D
- **Shape:** `CircleShape2D` with `radius = 12.0`
- **Why circle:** A circle approximates the diamond sprite shape for physics. It's simpler than a polygon collider and prevents snagging on tile corners. The 12px radius is slightly smaller than the sprite's 16px half-height to give a forgiving feel -- the player appears to barely squeeze past walls.
- **Position:** `Vector2(0, 0)` (centered on the CharacterBody2D origin)

**Sprite (Polygon2D)**
- **Type:** Polygon2D
- **Properties:**
  - `color = Color(0.557, 0.839, 1.0, 1.0)` -- equivalent to hex `#8ed6ff`, a light blue
  - `polygon = PackedVector2Array[(0, -16), (12, 0), (0, 16), (-12, 0)]` -- diamond shape, 24px wide by 32px tall
- **Why Polygon2D:** Placeholder for a future sprite. Polygon2D draws a filled polygon with a solid color, matching the Phaser prototype's `this.add.circle()` approach. When pixel art is ready, this node will be replaced with a Sprite2D or AnimatedSprite2D.
- **Diamond dimensions:** 24px wide (±12 from center), 32px tall (±16 from center). This is slightly larger than the collision circle (radius 12) to make the sprite visually extend beyond the collision bounds, which feels natural in isometric view.

**Camera2D**
- **Type:** Camera2D
- **Properties:**
  - `position_smoothing_enabled = true` -- camera lerps toward the player instead of snapping, creating smooth follow
  - `position_smoothing_speed = 5.0` -- interpolation speed; lower values = more lag behind player, higher values = more responsive. 5.0 provides a gentle follow that reduces jarring movement.
  - `zoom = Vector2(2, 2)` -- 2x zoom on both axes. The viewport is 1920x1080 but the game world uses small pixel values (tiles are 64x32), so 2x zoom makes the world fill the screen appropriately.
  - `enabled = true` (default) -- this is the active camera
- **Why child of Player:** As a child of the Player CharacterBody2D, Camera2D automatically follows the player's position. No code is needed to update the camera position -- Godot's scene tree hierarchy handles it.
- **Shake:** The camera shake effect tweens the `offset` property (not `position`), which temporarily displaces the view without affecting the camera's actual follow position. See `docs/objects/effects.md` for shake parameters.

**AttackRange (Area2D)**
- **Type:** Area2D
- **Properties:**
  - `collision_layer = 0` -- the attack range does not broadcast its own presence; nothing detects it
  - `collision_mask = 4` -- bit 2, detects bodies on the enemy collision layer
  - `monitoring = true` -- actively checks for overlapping bodies each physics frame
  - `monitorable = false` -- other Area2D nodes cannot detect this area (performance optimization)
- **Why Area2D:** Area2D is Godot's detection zone. Unlike CharacterBody2D, it doesn't participate in physics collision response -- bodies pass through it. It simply reports what bodies are overlapping. This is exactly what auto-attack needs: "which enemies are within 78px of the player?"
- **Why not a signal-based approach:** The player script polls `GetOverlappingBodies()` each frame during `HandleAttack()` to find the nearest enemy. This is simpler and more predictable than reacting to `body_entered`/`body_exited` signals and maintaining a list, especially since the player needs the nearest enemy each frame, not just any enemy.

**AttackShape (CollisionShape2D)**
- **Type:** CollisionShape2D
- **Shape:** `CircleShape2D` with `radius = 78.0`
- **Why 78px:** This matches the Phaser prototype's `ATTACK_RANGE = 78` exactly. It defines the auto-attack detection radius. Any enemy whose collision shape overlaps this circle is a candidate for auto-attack.
- **Position:** `Vector2(0, 0)` (centered on the AttackRange Area2D, which is centered on the Player)

---

### enemy.tscn

```
Enemy (CharacterBody2D) [Enemy.cs]
│   collision_layer = 4 (bit 2, binary: 0b100)
│   collision_mask = 1 (bit 0, binary: 0b001)
│   groups: ["enemies"]
│   [Export] DangerTier: int = 1
├── CollisionShape2D
│   └── shape: CircleShape2D(radius=10.0)
├── Sprite (Polygon2D)
│   └── color: set in _ready() based on danger_tier
│   └── polygon: PackedVector2Array[(0,-14), (10,0), (0,14), (-10,0)]
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
  - `collision_layer = 4` -- bit 2, identifies this body as "enemy" for other nodes querying layer 4
  - `collision_mask = 1` -- bit 0, collides with wall/tile physics bodies on layer 1
  - `motion_mode = MOTION_MODE_FLOATING` -- no gravity, free 2D movement
- **Exported Properties:**
  - `[Export] public int DangerTier { get; set; } = 1` -- exported so it can be set per-instance from `Dungeon.cs` before adding to the scene tree. Values 1-3 determine all enemy stats.
- **Groups:** `["enemies"]` -- added in `_Ready()`. Used by the player's AttackRange to filter overlapping bodies, and by `GetTree().GetNodesInGroup("enemies")` for enemy counting.
- **Why CharacterBody2D:** Same reasoning as the player -- `MoveAndSlide()` handles wall collision so enemies don't walk through walls. Enemies slide along walls when chasing the player at an angle.
- **Collision behavior:** Enemies collide with walls (mask bit 0) but do NOT collide with the player (no mask bit 1) or other enemies (no mask bit 2). Enemies overlap freely with each other and the player. Contact with the player is detected by the HitArea Area2D, not physics collision.
- See `docs/objects/enemies.md` for full method pseudocode and tier stats.

**CollisionShape2D**
- **Type:** CollisionShape2D
- **Shape:** `CircleShape2D` with `radius = 10.0`
- **Why radius 10:** Slightly smaller than the player's 12px radius. Enemies are visually smaller (diamond is 20x28 vs player's 24x32), and the smaller collision radius means they can navigate slightly tighter spaces, which feels natural for "swarming" enemies.
- **Position:** `Vector2(0, 0)` (centered on the Enemy CharacterBody2D origin)

**Sprite (Polygon2D)**
- **Type:** Polygon2D
- **Properties:**
  - `color` -- set dynamically in `_Ready()` based on `DangerTier`:
    - Tier 1: `Color(0.420, 1.0, 0.537, 1.0)` = `#6bff89` (green)
    - Tier 2: `Color(1.0, 0.871, 0.400, 1.0)` = `#ffde66` (yellow)
    - Tier 3: `Color(1.0, 0.435, 0.435, 1.0)` = `#ff6f6f` (red)
  - `polygon = PackedVector2Array[(0, -14), (10, 0), (0, 14), (-10, 0)]` -- diamond shape, 20px wide by 28px tall (smaller than player)
- **Why dynamic color:** The same `enemy.tscn` scene is used for all three tiers. The `DangerTier` export property determines color at runtime, avoiding three nearly identical scene files.

**HitArea (Area2D)**
- **Type:** Area2D
- **Properties:**
  - `collision_layer = 0` -- does not broadcast; nothing detects the HitArea itself
  - `collision_mask = 2` -- bit 1, detects the player on layer 2
  - `monitoring = true` -- actively checks for overlapping bodies
  - `monitorable = false` -- other Area2D nodes cannot detect this area
- **Why Area2D:** Detects when the player overlaps with the enemy's "hit zone." This replaces Phaser's `physics.add.overlap(player, enemies, callback)`. The Area2D emits `BodyEntered` when the player first enters and provides `GetOverlappingBodies()` for the cooldown re-check.
- **Signal connections:**
  - `BodyEntered += OnHitAreaBodyEntered;` -- triggers initial damage when player first enters the hit zone
- **Why radius 15 (larger than collision shape):** The HitArea extends slightly beyond the enemy's physics body (radius 10). This creates a small "danger zone" around the enemy -- the player takes damage slightly before visually touching the enemy's body, which feels more threatening and reduces the frustration of pixel-perfect avoidance.

**HitShape (CollisionShape2D)**
- **Type:** CollisionShape2D
- **Shape:** `CircleShape2D` with `radius = 15.0`
- **Position:** `Vector2(0, 0)` (centered on the HitArea Area2D)

**HitCooldownTimer (Timer)**
- **Type:** Timer
- **Properties:**
  - `wait_time = 0.7` (seconds) -- matches Phaser's 700ms hit cooldown per enemy
  - `one_shot = true` -- fires once and stops; must be manually restarted
- **Why Timer node:** Enforces the 0.7-second cooldown between consecutive damage ticks from the same enemy. When the timer is running (not stopped), the enemy cannot deal damage. When it expires, the `Timeout` signal fires and the enemy re-checks if the player is still overlapping.
- **Signal connection:** `Timeout += OnHitCooldownTimerTimeout;` -- on timeout, checks `_hitArea.GetOverlappingBodies()` for the player and deals damage again if still overlapping, then restarts the timer.
- **Why one_shot:** The enemy should only deal damage when the player is actively overlapping. If the player leaves and re-enters, `body_entered` handles the new contact. The one_shot timer ensures the re-check loop stops when the timer isn't explicitly restarted.

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
│   anchor_right = 1.0, anchor_bottom = 1.0 (full rect)
│   visible = false
│   process_mode = PROCESS_MODE_ALWAYS
├── Overlay (ColorRect)
│   anchor_right = 1.0, anchor_bottom = 1.0
│   color = Color(0, 0, 0, 0.75)
└── CenterContainer
    │   anchor_right = 1.0, anchor_bottom = 1.0
    └── VBoxContainer (theme_override_constants/separation = 16)
        ├── TitleLabel (Label)
        │   text = "You Died"
        │   theme_override_colors/font_color = Color("#ffe1b0")
        │   theme_override_font_sizes/font_size = 48
        │   horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
        ├── InstructionLabel (Label)
        │   text = "Press R to restart"
        │   theme_override_colors/font_color = Color("#b6bfdb")
        │   theme_override_font_sizes/font_size = 20
        │   horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
        └── RestartButton (Button)
            text = "Restart"
            theme_override_colors/font_color = Color("#ecf0ff")
            theme_override_styles/normal: StyleBoxFlat with accent bg
            theme_override_styles/hover: StyleBoxFlat with lighter accent bg
            custom_minimum_size = Vector2(120, 40)
```

#### Node Details

**DeathScreen (Control)**
- **Type:** Control
- **Script:** `res://scripts/ui/DeathScreen.cs`
- **Properties:**
  - `anchor_right = 1.0`, `anchor_bottom = 1.0` -- fills the entire viewport
  - `visible = false` -- hidden by default. Set to `true` by `Main.cs` when the player dies.
  - `process_mode = PROCESS_MODE_ALWAYS` -- continues processing even when the scene tree is paused. This is essential: when the player dies, `Main.cs` pauses the tree (`GetTree().Paused = true`) to freeze all gameplay. The death screen must still respond to input (R key, button click) despite the pause.
- **Why process_mode ALWAYS:** Without this, the death screen would be paused along with everything else, and the player could never restart. PROCESS_MODE_ALWAYS is the standard Godot pattern for pause menus and death screens.
- **Responsibilities:**
  - Listens for R key press in `_UnhandledInput()` or `_Input()`
  - Connects `RestartButton.Pressed += OnRestartButtonPressed;`
  - On restart: calls `GameState.Reset()`, sets `GetTree().Paused = false`, calls `GetTree().ReloadCurrentScene()`

**Overlay (ColorRect)**
- **Type:** ColorRect
- **Properties:**
  - `anchor_right = 1.0`, `anchor_bottom = 1.0` -- fills entire parent (full viewport)
  - `color = Color(0, 0, 0, 0.75)` -- 75% opaque black overlay
- **Why ColorRect:** Provides the dark semi-transparent background behind the death UI. Matches Phaser's `this.add.rectangle(..., 0x000000, 0.75)`. ColorRect is the simplest way to fill a rectangular area with a solid color.

**CenterContainer**
- **Type:** CenterContainer
- **Properties:**
  - `anchor_right = 1.0`, `anchor_bottom = 1.0` -- fills entire parent
- **Why CenterContainer:** Automatically centers its single child (VBoxContainer) both horizontally and vertically. This replaces the CSS centering that Phaser's text had via `setOrigin(0.5)`.

**VBoxContainer**
- **Type:** VBoxContainer
- **Properties:**
  - `theme_override_constants/separation = 16` -- 16px vertical gap between elements. More spacing than the HUD (4px) because the death screen is a modal dialog, not a compact info panel.

**TitleLabel (Label)**
- **Type:** Label
- **Properties:**
  - `text = "You Died"` -- matches Phaser's death screen text
  - `theme_override_colors/font_color = Color("#ffe1b0")` -- warm gold/cream, matches Phaser's `#ffe1b0`
  - `theme_override_font_sizes/font_size = 48` -- large, prominent text. Phaser used `28px` but at 2x camera zoom the effective size doubles.
  - `horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER` -- text centered within the label

**InstructionLabel (Label)**
- **Type:** Label
- **Properties:**
  - `text = "Press R to restart"` -- simplified from Phaser's "Tap / Press R to restart" since mobile touch is deferred
  - `theme_override_colors/font_color = Color("#b6bfdb")` -- muted color, same as HUD controls text
  - `theme_override_font_sizes/font_size = 20`
  - `horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER`

**RestartButton (Button)**
- **Type:** Button
- **Properties:**
  - `text = "Restart"`
  - `theme_override_colors/font_color = Color("#ecf0ff")` -- bright ink color for readability
  - `theme_override_styles/normal` -- `StyleBoxFlat` with accent background color (gold/warm tone)
  - `theme_override_styles/hover` -- `StyleBoxFlat` with slightly lighter accent on hover
  - `custom_minimum_size = Vector2(120, 40)` -- minimum button size for comfortable clicking
- **Why Button in addition to R key:** Provides a visible, clickable restart option. The R key is a keyboard shortcut; the button is for discoverability and future mouse/touch users.
- **Signal connection:** `Pressed += OnRestartButtonPressed;`

---

### Collision Layer Reference

| Bit | Layer Value | Name | Used By |
|-----|-------------|------|---------|
| 0 | 1 | Walls/Tiles | TileMapLayer wall tile physics |
| 1 | 2 | Player | Player CharacterBody2D |
| 2 | 4 | Enemies | Enemy CharacterBody2D |

**Collision Matrix:**

| Node | Layer (what I am) | Mask (what I collide with) | Effect |
|------|-------------------|---------------------------|--------|
| Player body | 2 | 1 | Player slides along walls |
| Enemy body | 4 | 1 | Enemies slide along walls |
| Player AttackRange | 0 | 4 | Detects enemies for auto-attack |
| Enemy HitArea | 0 | 2 | Detects player for contact damage |

**Why enemies don't collide with each other:** Setting enemy mask to only bit 0 (walls) means enemies freely overlap each other. This is intentional -- in the Phaser prototype, enemies stack on top of each other when chasing the player. Adding enemy-enemy collision would require pathfinding to prevent enemies from getting stuck, which is deferred to a future iteration.

**Why player doesn't collide with enemies:** If the player collided with enemies (mask bit 2), enemy bodies would push the player around. This would make combat feel frustrating -- getting surrounded would shove the player uncontrollably. Instead, enemies deal damage via Area2D overlap, and the player can always move freely.

---

### Group Reference

| Group Name | Members | Queried By | Purpose |
|------------|---------|------------|---------|
| `"player"` | Player CharacterBody2D | `Enemy.cs` via `GetTree().GetFirstNodeInGroup("player")` | Enemies find the player to chase and damage check |
| `"enemies"` | All Enemy CharacterBody2D instances | `Player.cs` via `GetOverlappingBodies()` filtered by group | Player finds enemies in attack range |

## Implementation Notes

- All scenes are saved as `.tscn` files in `res://scenes/`. Scripts are in `res://scripts/` with subdirectories matching the scene organization.
- The player and enemy scenes are PackedScene resources loaded by `Dungeon.cs` using `GD.Load<PackedScene>()` for access:
  ```
  private static readonly PackedScene PlayerScene = GD.Load<PackedScene>("res://scenes/player.tscn");
  private static readonly PackedScene EnemyScene = GD.Load<PackedScene>("res://scenes/enemy.tscn");
  ```
- Y-sorting is enabled on both TileMapLayer and Entities to ensure correct isometric draw order. Without y-sort, entities behind walls could render in front of them.
- The UILayer CanvasLayer at layer 10 ensures UI is always on top regardless of Camera2D zoom or position.

## Open Questions

- Should the player scene include an AnimationPlayer node for future sprite animations, or add it when pixel art is introduced?
- Should the death screen scene include an AnimationPlayer for a fade-in transition?
- Should enemies have a separate HealthBar node (ProgressBar or custom drawing) for visual HP feedback?
- Should the Entities node use a YSort node (deprecated in Godot 4) or is `y_sort_enabled` on Node2D sufficient for all cases?
- Should the HUD panel use a proper Theme resource (`.tres`) instead of per-node theme overrides, for easier global style changes?
