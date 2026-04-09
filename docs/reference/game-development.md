# Game Development Research Reference

Accumulated knowledge from building a 2D isometric dungeon crawler in Godot 4 + C#. This is a lookup reference for future sessions so previously researched patterns are not re-investigated. All entries reflect patterns that have been implemented and tested in this project.

Last updated: 2026-04-09 (added ISS environment tileset section).

---

## 1. Godot 4 + C# Patterns

Core engine patterns validated through 3 pipeline tests and an automated 36-step demo.

### Scene/Node Model

- Everything is a tree of Nodes. The root `Node` contains children, which contain their own children. Godot renders the current tree state each frame.
- `GetNode<T>("Path/To/Child")` traverses the tree (equivalent to DOM's `querySelector`).
- `QueueFree()` removes a node at end of frame (safe). `Free()` removes immediately (dangerous -- can crash if referenced mid-frame). Always prefer `QueueFree()`.
- Scenes (`.tscn`) are reusable sub-trees. `PackedScene.Instantiate<T>()` creates a copy. Think of scenes as web components -- self-contained, composable units.
- **Node2D** is for game world objects (position/rotation/scale, affected by Camera2D). **Control** is for UI elements (anchors/margins/themes, not affected by Camera2D). Never mix them.
- Groups are a tagging system: `GetTree().GetNodesInGroup("enemies")` returns all nodes in that group, like `querySelectorAll(".enemies")`.

### Lifecycle Methods

| Method | When It Runs | Use For |
|--------|-------------|---------|
| Constructor | Object creation (before tree) | Do not use for Godot API calls -- node is not in the tree yet |
| `_Ready()` | Once, when node enters the tree | One-time setup: load resources, connect signals, configure children |
| `_Process(delta)` | Every render frame (varies: 60/144fps) | Visual effects, animations, UI updates, camera smoothing |
| `_PhysicsProcess(delta)` | Fixed 60fps, always | Movement, collision, AI, combat -- anything that affects gameplay |

- `_Ready()` replaces Phaser's `create()`. `_PhysicsProcess()` replaces Phaser's `update()` for gameplay logic.
- Delta in `_PhysicsProcess()` is ~0.01667s (1/60th). Delta in `_Process()` varies per frame.

### MoveAndSlide Delta Gotcha

`MoveAndSlide()` applies delta internally. Do NOT multiply velocity by delta before calling it.

```csharp
// WRONG -- double-applies delta, character moves in slow motion
Velocity = direction * MoveSpeed * (float)delta;
MoveAndSlide();

// RIGHT -- MoveAndSlide handles delta for you
Velocity = direction * MoveSpeed;
MoveAndSlide();
```

For manual position updates (non-physics objects like visual effects), multiply by delta yourself: `Position += direction * speed * (float)delta`.

### Signals

- Godot's observer pattern. Nodes emit signals, other nodes connect and react. "Call down, signal up" is the design principle.
- C# syntax: `signalSource.SignalName += OnSignalHandler;`
- Custom signals via `[Signal] public delegate void StatsChangedEventHandler();`
- Planned but not yet wired in production: 9 signals defined in `signals.md` covering stats changes, combat events, timers, physics detection, and UI input.

### Autoloads

- Singleton nodes that persist across scene changes. Registered in `project.godot`.
- Accessed via `GetNode<GameState>("/root/GameState")`.
- Planned: `GameState` (reactive player state with signal emission) and `EventBus` (decoupled gameplay events like `EnemyDefeated`, `PlayerAttacked`).
- Current demo uses static C# classes instead -- same persistence, but no signals or scene tree integration.

### Headless Mode Detection

```csharp
if (DisplayServer.GetName() == "headless")
{
    GetTree().Quit();
}
```

Used for CI/testing. Godot does not auto-exit after `_Ready()` in headless mode -- you must call `GetTree().Quit()` explicitly.

### CanvasLayer for UI

- UI elements placed on a `CanvasLayer` node use viewport pixel coordinates, unaffected by Camera2D zoom/position.
- Game entities use world coordinates that move with the camera.
- This separation is essential: the HUD stays fixed while the dungeon scrolls.

---

## 2. DCSS Tile Assets

12,700+ CC0 sprites from Dungeon Crawl Stone Soup. The primary art asset library for this project.

### Source Packs

| Pack | Count | Location |
|------|-------|----------|
| DCSS Full | 6,029 PNGs | `assets/tilesets/dungeon-crawl/dcss-full/` |
| DCSS Supplemental | 3,016 PNGs | `assets/tilesets/dungeon-crawl-supplemental/dcss-supplemental/` |
| Crawl Tiles 2010 | 3,039 PNGs | `assets/tilesets/dungeon-crawl/crawl-tiles-2010/` |

### Directory Structure (inside each pack)

```
monster/          -- enemies (animals/, undead/, orc_*, dragon_*, etc.)
player/           -- paper doll layers (base/, body/, hand_right/, etc.)
item/             -- weapons, armor, potions, scrolls, misc
dungeon/          -- floor tiles, wall tiles, doors, traps, furniture
effect/           -- spell effects, explosions, auras
gui/              -- UI elements (health bars, icons)
misc/             -- miscellaneous (emissaries, etc.)
```

### Tile Format

- 32x32 pixels, PNG with alpha channel.
- CC0 license (public domain) -- no attribution required, but credited in `assets/ATTRIBUTION.md`.
- Spritesheets also available (`ProjectUtumno_full.png`, `ProjectUtumno_supplemental.png`) but individual PNGs are used for flexibility.

### Paper Doll Compositing

Layer sprites on a `Node2D` container. Each layer is a `Sprite2D` child, composited via transparency.

```
Node2D (container -- all layers move together)
  +-- Sprite2D (body: player/base/human_male.png)
  +-- Sprite2D (armor: player/body/chainmail.png)
  +-- Sprite2D (weapon: player/hand_right/long_sword.png)
```

- Layers stack in child order (later children draw on top).
- The container's position moves all layers together.
- Validated in Session 1 asset render test.

### Pixel-Perfect Rendering

Set `TextureFilter = CanvasItem.TextureFilterEnum.Nearest` on each sprite, or set the project default in `project.godot`:

```
rendering/textures/canvas_textures/default_texture_filter = 0  (Nearest)
```

Nearest-neighbor filtering preserves pixel art crispness at any zoom level. Without it, 32x32 sprites blur when scaled.

### Loading with Fallback

```csharp
if (ResourceLoader.Exists(path))
{
    sprite.Texture = GD.Load<Texture2D>(path);
}
else
{
    // Fallback to programmatic colored texture
    var img = Image.CreateEmpty(32, 32, false, Image.Format.Rgba8);
    img.Fill(fallbackColor);
    sprite.Texture = ImageTexture.CreateFromImage(img);
}
```

Always guard `GD.Load<Texture2D>()` with `ResourceLoader.Exists()` to avoid crashes on missing assets.

---

## 3. Isometric Stone Soup (ISS) Environment Tiles

The primary environment tileset. ISS defines the game's tile grid standard -- all tilemaps, floor tiles, and wall blocks conform to its dimensions.

### Source

- **Author:** [Screaming Brain Studios](https://opengameart.org/users/screaming-brain-studios) — sole source for all isometric textures and tiles
- **Pack:** Isometric Stone Soup
- **License:** CC0 (public domain) -- no attribution required
- **Origin:** Converted from Dungeon Crawl Stone Soup CC0 tiles into isometric format
- **Location:** `assets/isometric/tiles/stone-soup/`

### Tile Dimensions

| Tile Type | Size (px) | Notes |
|-----------|-----------|-------|
| Floor tiles | 64x32 | Isometric diamond -- defines the TileMapLayer cell size |
| Wall blocks (full) | 64x64 | Isometric cube, full-height block |
| Wall blocks (half) | 64x64 | Half-height variant |
| Wall top-face overlays | 64x64 | Overlay sprites for wall tops |

These dimensions set the project-wide tile grid standard:
- `TileMapLayer.TileSet.TileSize = Vector2I(64, 32)`
- `TileMapLayer.TileSet.TileShape = TileSet.TileShapeIsometric`

### Sheet Inventory

| Category | Count | Description |
|----------|-------|-------------|
| Wall block sheets | 43 | Full blocks, half blocks, top-face overlays |
| Floor sheets | 49 | Stone, dirt, grass, water, and other floor variants |
| Torch sprites | 3 | Wall-mounted torches for dungeon lighting |
| **Total** | **95** | Sprite sheets |

Comes with 86 Tiled `.tsx` files (tileset definitions for the Tiled map editor).

### Transparency

Sprite sheets use magenta (`#FF00FF`) backgrounds as a transparency key. When importing into Godot, either:
- Strip magenta and export with alpha in an image editor, or
- Use a shader/import setting to treat `#FF00FF` as transparent

### Relationship to DCSS Tiles (Section 2)

ISS covers **environment** art: walls, floors, decorations. The DCSS tile packs (Section 2) cover **creatures, items, and effects**. ISS replaces the old `cave_atlas.png` placeholder, which does not conform to the 64x32 grid standard.

---

## 4. Combat and Formulas

All formulas from the specs, implemented in `GameCore.cs` and validated by 51 unit tests.

### Damage

| Formula | Expression | Notes |
|---------|-----------|-------|
| Player damage (P1) | `12 + floor(level * 1.5) + weapon_damage` | Placeholder until stats system |
| Player damage (P2+) | `(base_weapon + flat_melee) * (1 + pct_melee / 100)` | STR-based, from stats.md |
| Monster damage | `3 + tier` | Tier 1-3 |

### Defense Diminishing Returns

```
damage_reduction = defense * (100 / (defense + K))
```

Where `K = 100`. At 100 defense, DR = 50%. At 200 defense, DR = 66.7%. Never reaches 100% -- always takes some damage. This is the standard `x / (x + K)` hyperbolic curve used by WoW, LoL, Diablo, and most modern ARPGs.

### Crit System

- Base crit chance: 15%
- Crit multiplier: 1.5x damage
- Displayed as yellow floating text in the demo

### XP Curve

```
xp_to_next_level(L) = L^2 * 45
```

Quadratic curve. Pure integer math (no float promotion needed since L is always integer).

| Level | XP Required | Approx Time |
|-------|------------|-------------|
| 1 to 2 | 45 | ~2 min |
| 10 to 11 | 4,500 | ~15 min |
| 50 to 51 | 112,500 | ~25 min |
| 100 to 101 | 450,000 | ~30 min |

No level cap. Infinite progression matches the infinite dungeon theme.

### Level-Up Rewards

- HP increase: `floor(8 + level * 0.5)` per level, plus 15% current HP heal
- Stat points: 3 per level, bonus at milestones
- Skill points: 2 per level, bonus at milestones

### Floor Difficulty Scaling

```
effective_stat = base_stat * (1 + (floor - 1) * 0.5)
```

Applies to monster HP and XP. Floor 1 = 1.0x, Floor 2 = 1.5x, Floor 3 = 2.0x. Linear scaling keeps deeper floors challenging without exponential blowout.

### Monster Stats by Tier

| Stat | Tier 1 | Tier 2 | Tier 3 |
|------|--------|--------|--------|
| Base HP | 30 | 42 | 54 |
| Damage | 4 | 5 | 6 |
| Base XP | 10 | 15 | 20 |

All values scaled by floor difficulty multiplier at runtime.

---

## 5. UI Patterns Learned

Patterns validated in the 36-step demo with styled dark-fantasy windows, HP/MP orbs, and visual effects.

### Control._Draw() for Custom Rendering

Used for the Diablo-style HP/MP orbs. Override `_Draw()` on a `Control` node and call drawing primitives:

```csharp
DrawCircle(center, radius, color);       // filled circle
DrawArc(center, radius, startAngle, endAngle, pointCount, color, width);  // arc
DrawLine(from, to, color, width);        // line
DrawString(font, position, text, alignment, width, fontSize, color);      // text
```

Call `QueueRedraw()` to schedule a repaint. `_Draw()` does not run every frame -- only when queued.

### HP/MP Orb Fill Technique

Fill-from-bottom using horizontal line sweep: iterate Y values from bottom to top, calculate the circle's half-width at each row, draw a horizontal line of the fill color. Glass highlight is a semi-transparent white arc at the top. Double metallic border (outer dark, inner lighter).

~100 `DrawLine()` calls per orb per draw. Acceptable because `_Draw()` only fires when values change. For animated orbs (sloshing, glow pulse), switch to a fragment shader.

### StyleBoxFlat for Dark Fantasy Panels

Godot's equivalent of inline CSS for panel styling.

```csharp
var style = new StyleBoxFlat();
style.BgColor = new Color(0.086f, 0.106f, 0.157f, 0.9f);   // dark blue-grey
style.BorderColor = new Color(0.961f, 0.784f, 0.42f, 0.4f); // gold
style.SetBorderWidthAll(2);
style.SetCornerRadiusAll(8);
style.SetContentMarginAll(12);
panel.AddThemeStyleboxOverride("panel", style);
```

Property mapping: `BgColor` = `background-color`, `BorderColor` = `border-color`, `SetBorderWidthAll()` = `border-width`, `SetCornerRadiusAll()` = `border-radius`, `SetContentMarginAll()` = `padding`.

### Tween-Based Animations

`CreateTween()` is the animation swiss army knife. Every visual effect in the demo uses it.

| Effect | Tween Call |
|--------|-----------|
| Fade out | `TweenProperty(node, "modulate:a", 0.0, 0.3)` |
| Drift up | `TweenProperty(node, "position:y", target, 0.8)` |
| Scale expand | `TweenProperty(node, "scale", Vector2.One * 3, 0.25)` |
| Flash color | `TweenProperty(node, "modulate", Colors.White, 0.1)` then back |
| Cleanup after | `TweenCallback(Callable.From(node.QueueFree))` |

- `Parallel()` chains run simultaneously; sequential chains run in order.
- Slash effect: `Polygon2D` bar (26x4px, gold) with random rotation, fades + drifts up in 150ms.
- Floating damage text: Label tweens upward and fades. Color by type: red (player damage), white (monster), yellow (crit), green (heal), purple-green (poison).

### UI Node Types for Common Patterns

| Pattern | Node | Notes |
|---------|------|-------|
| Inventory grid | `GridContainer` | Set `Columns` property; children auto-flow into rows |
| Stat list | `VBoxContainer` | Vertical stack of Label nodes |
| Item row | `HBoxContainer` | Icon (TextureRect) + name (Label) + value (Label) |
| Dark panel | `PanelContainer` | Apply `StyleBoxFlat` override; set content margins |
| Settings slider | `HSlider` | Float range, connect `ValueChanged` signal |
| Settings toggle | `CheckButton` | Boolean, connect `Toggled` signal |
| Settings dropdown | `OptionButton` | Enum values, connect `ItemSelected` signal |
| Toast notification | Queue of Labels | Timer-based removal, stack from bottom |
| Sprite in UI | `TextureRect` | Use instead of Sprite2D for Control-space rendering |

### TextureRect vs Sprite2D

- `Sprite2D` is for game world objects (Node2D space, affected by Camera2D).
- `TextureRect` is for UI (Control space, works inside Panel/VBoxContainer layouts).
- Both load textures the same way, but TextureRect respects UI layout containers.

---

## 6. Testing Approaches

Dual framework strategy: xUnit for pure logic, GdUnit4 for Godot scene tests.

### Pure C# Logic Testing with xUnit

The key architectural win: `GameCore.cs` has zero Godot imports. It uses only `System` and `System.Collections.Generic`. This means all game logic (damage formulas, inventory, leveling, combat, shop, death/respawn) is testable with plain xUnit -- no game engine runtime needed.

- 51 unit tests across 10 test classes
- Runs in milliseconds via `dotnet test`
- Covers: combat, inventory, shop, leveling, dungeon, death/respawn, status effects, skills, settings, save

### Source Linking via Compile Include

The test project includes the game logic source file directly rather than referencing the Godot project:

```xml
<!-- In DungeonGame.Tests.csproj -->
<Compile Include="../scripts/game/GameCore.cs" Link="GameCore.cs" />
```

This compiles `GameCore.cs` fresh against the test project's target framework (net10.0). No project reference to the Godot SDK needed. Avoids the "can't reference a Godot project from a plain .NET test project" problem entirely.

### Disable Parallel Test Execution

Static singletons (`GameState`) and parallel test execution cause race conditions. Fix:

```csharp
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

Applied at assembly level in the test project. Future improvement: make `GameState` an instance that each test creates fresh.

### Headless E2E with Console Grep

The automated demo runs in headless mode at 10ms step delay (~2 seconds for all 36 steps). Console output is captured and asserted with grep patterns:

```bash
# e2e_demo_test.sh
godot --headless --path . | grep -q "[INIT]" && echo "PASS: init"
godot --headless --path . | grep -q "LEVEL UP" && echo "PASS: leveling"
```

40 E2E assertions covering every phase, mechanic, and state transition. Fast CI without visual rendering.

### Screenshot/Video Capture

macOS `screencapture` for visual test evidence:

```bash
screencapture -x screenshot.png     # -x flag for silent capture
```

Requires Screen Recording permission. Used for evidence documentation, not automated assertions.

---

## 7. Performance Patterns

Patterns applied during the Session 2c performance audit. All validated with tests passing after changes.

### Dirty-Flag Stat Caching

The number one performance pattern in game dev for derived values.

```csharp
private int _cachedDamage;
private bool _statsDirty = true;

public int TotalDamage
{
    get
    {
        if (_statsDirty) RecalculateStats();
        return _cachedDamage;
    }
}

public void EquipItem(ItemData item)
{
    // ... equip logic ...
    _statsDirty = true;  // invalidate
}
```

Compute once on mutation, read many times per frame. Applies to: effective stats, damage ranges, movement speed, spell costs, attack speed -- anything derived from equipment or level.

Any code that directly mutates stats must call `InvalidateStats()`. If it does not, cached values go stale. In production, make stat fields private-set with methods that auto-invalidate.

### Integer-Only Formulas

Avoid unnecessary float promotion when the result is always integer:

```csharp
// Bad: promotes to double, floors back to int (3 type conversions)
public int XPToNextLevel => (int)Math.Floor(Level * Level * 45.0);

// Good: pure integer math, same result
public int XPToNextLevel => Level * Level * 45;
```

Applies to: XP curves, tier calculations, floor multipliers, any formula where all inputs and outputs are integers.

### Skip-Redraw on Unchanged Values

Gate `QueueRedraw()` behind a value-change check:

```csharp
public void UpdateValues(int hp, int maxHp, int mp, int maxMp)
{
    if (hp == _hp && maxHp == _maxHp && mp == _mp && maxMp == _maxMp)
        return;  // nothing changed, skip redraw

    _hp = hp; _maxHp = maxHp; _mp = mp; _maxMp = maxMp;
    _hpText = $"{_hp}/{_maxHp}";  // pre-format strings
    _mpText = $"{_mp}/{_maxMp}";
    QueueRedraw();
}
```

The biggest performance win is not doing work at all. Before optimizing HOW something runs, ask IF it needs to run.

### String Pre-Caching in Draw Loops

`_Draw()` should be allocation-free. Pre-compute formatted strings in the update method and reuse them in the draw method. String interpolation like `$"{_hp}/{_maxHp}"` allocates a new string every call.

### RemoveRange vs Repeated RemoveAt

`List.RemoveAt(0)` is O(n) -- shifts all remaining elements. For queue-like trimming:

```csharp
// Bad: 3 separate array shifts
while (list.Count > 14) list.RemoveAt(0);

// Good: single shift operation
if (list.Count > 14) list.RemoveRange(0, list.Count - 14);
```

For true FIFO behavior, use `Queue<T>` (O(1) dequeue) or a circular buffer.

### Backward Iteration for Collection Removal

Forward iteration skips nodes when indices shift after removal. Backward iteration is safe:

```csharp
// Safe: removing index 5 doesn't affect indices 0-4
for (int i = parent.GetChildCount() - 1; i >= 0; i--)
{
    var child = parent.GetChild(i);
    if (child is TextureRect) child.QueueFree();
}
```

One pass, zero allocations. Applies everywhere you clean up child nodes: clearing enemy groups, resetting UI, despawning effects.

### Production-Scale Patterns (Documented, Not Yet Needed)

| Current Pattern | Production Replacement | When |
|----------------|----------------------|------|
| 165 individual Sprite2D tiles | `TileMapLayer` with `TileSet` (single batched draw call) | Real dungeon generation (P1-05) |
| Create/QueueFree per effect | Object pool (toggle `Visible` + `ProcessMode`) | Real-time multi-enemy combat |
| `CreateColorTexture()` per call | `Dictionary<(Color, int), ImageTexture>` cache | Dynamic colored textures at runtime |
| CPU line-sweep orb fill (~100 DrawLine calls) | Fragment shader with fill_percent uniform | Production HUD build |

---

## 8. Architecture Lessons

Insights from the Session 2d architecture audit comparing demo code against 6 spec docs.

### Static Game Logic Layer Enables Unit Testing

The most important architecture decision so far: `GameCore.cs` imports only `System` and `System.Collections.Generic`. Zero Godot dependency. This means 51 unit tests run with plain xUnit in milliseconds, no game engine needed.

When migrating to autoloads, keep a pure-logic layer underneath. The autoload Node wraps the logic and adds signal emission -- it does not contain the logic itself.

### Demo Code Does Not Equal Production Architecture

The demo validates mechanics in isolation. Production code requires:

| Concern | Demo | Production |
|---------|------|-----------|
| State management | Static C# class | Godot autoload Node with signal-emitting property setters |
| Communication | Direct method calls | EventBus signals ("call down, signal up") |
| Scene structure | 1 monolith scene, everything built in `_Ready()` | 6+ scenes, each entity in its own `.tscn` + `.cs` pair |
| File layout | Flat (`scripts/`, `scenes/`) | Categorized (`scripts/autoloads/`, `scripts/ui/`, `scenes/dungeon/`) |
| Physics | `Position +=` in scripted steps | `CharacterBody2D` + `MoveAndSlide()` + collision layers |
| Script size | 1,022 lines (GameDemo.cs) | 300-line limit per file |

Demo code is a test harness. It proves "does the math work?" not "is the node tree correct?"

### Signals Enable Reactive State

The planned architecture uses signals for decoupled reactivity:

- `GameState.StatsChanged` -- HUD auto-updates when HP/MP/XP change
- `EventBus.EnemyDefeated` -- dungeon schedules respawn AND HUD shows XP (neither system knows about the other)
- `Area2D.BodyEntered` -- physics-based hit detection replaces proximity checks

Current demo uses zero signals. All 9 planned signals are defined in `signals.md` and will be implemented when their owning systems are built.

### Each Gap Maps to a Spec Doc Section

Every architecture gap identified in the audit maps directly to a locked spec:

| Gap | Spec |
|-----|------|
| Static state to reactive autoloads | `autoloads.md` |
| Missing EventBus | `autoloads.md` |
| Monolith scene to entity scenes | `scene-tree.md` |
| Flat files to categorized structure | `project-structure.md` |
| Zero signals to 9 connections | `signals.md` |
| No physics to collision layers | `scene-tree.md` + entity specs |

The specs are the migration checklist. No guesswork needed.

### Naming Convention Enforcement

Demo files deviate from spec conventions (e.g., `game_demo.tscn` snake_case instead of PascalCase). Production tickets must enforce naming in PR checklists from day one. Conventions: C# files PascalCase, private fields `_camelCase`, public methods PascalCase, constants PascalCase.

---

*This is a living reference. Append new sections as research accumulates. Do not edit existing entries -- add corrections as new entries with dates.*
