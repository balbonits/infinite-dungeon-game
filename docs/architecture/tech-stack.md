# Tech Stack

## Summary

The game runs on Godot 4 as a native desktop application. No web browser, no build tools, no package managers. The Godot editor is the entire development environment — press F5 to play.

## Current State

- **Engine:** Godot 4.x (open-source, MIT license)
- **Language:** GDScript (Python-like, tightly integrated with the Godot editor)
- **Renderer:** GL Compatibility (broadest hardware support, forward+ available later)
- **Physics:** Built-in Godot 2D physics (CharacterBody2D + Area2D, no external library)
- **Perspective:** Isometric 2D — 2:1 diamond tiles (64x32 pixels), rendered via TileMapLayer
- **UI:** Built-in Control node system with Theme resources
- **Persistence:** FileAccess + JSON, saved to `user://` directory (no localStorage)
- **Platform:** Desktop native — macOS primary development, Windows/Linux exportable
- **Dev workflow:** Godot editor + F5 — no build tools, no package managers, no terminal commands

## Design

### Core Stack

| Layer | Technology | Notes |
|-------|-----------|-------|
| Engine | Godot 4.x | Open-source (MIT), scene/node architecture, built-in editor |
| Language | GDScript | Python-like syntax, tightly integrated with editor, auto-completion, debugger |
| Renderer | GL Compatibility | OpenGL-based, broadest hardware support; Forward+ available for upgrade later |
| Physics | Built-in 2D | CharacterBody2D for player/enemies, Area2D for detection zones, no external physics lib |
| Perspective | Isometric 2D | 2:1 diamond tiles (64x32 px), TileMapLayer node, diamond tile shape mode |
| UI | Control nodes | Built-in UI system — Label, Button, Panel, VBoxContainer; Theme resources for styling |
| Persistence | FileAccess + JSON | Reads/writes to `user://` directory; platform-appropriate save location |
| Platform | Desktop native | macOS primary dev environment; Windows and Linux via Godot export templates |
| Dev workflow | Godot editor + F5 | No build tools, no package managers, no CLI required; editor handles everything |

### Window Configuration

These values live in `project.godot` under `[display]`:

| Setting | Value | Why |
|---------|-------|-----|
| `window/size/viewport_width` | `1920` | Full HD base resolution — content designed for this width |
| `window/size/viewport_height` | `1080` | Full HD base resolution — content designed for this height |
| `window/size/mode` | `2` (Maximized) | Game starts maximized, filling the screen without going fullscreen |
| `display/window/stretch/mode` | `canvas_items` | UI and game content scale together, sharp at any resolution |
| `display/window/stretch/aspect` | `expand` | Extra screen space shows more of the world, no black bars |
| `rendering/textures/canvas_textures/default_texture_filter` | `0` (Nearest) | Pixel-art-ready — no blurry interpolation on scaled sprites/tiles |
| `rendering/renderer/rendering_method` | `gl_compatibility` | Broadest GPU support — works on integrated graphics, old hardware |

**Why maximized instead of fullscreen:** Maximized (mode=2) keeps the OS title bar and taskbar visible, making alt-tabbing and window management natural. Players can press F11 or use a menu option for true fullscreen later.

**Why canvas_items stretch mode:** This mode scales all 2D content (sprites, UI, tiles) uniformly. Combined with `expand` aspect, it means larger monitors see more of the dungeon rather than getting stretched or letterboxed content.

**Why nearest-neighbor filtering:** Even though the game currently uses Polygon2D shapes (not pixel art sprites), nearest-neighbor is set as the default so that when pixel art tiles and sprites are introduced, they render crisply without configuration changes.

### What We Intentionally Avoid

| Avoided | Why |
|---------|-----|
| Web/browser deployment | Godot can export to HTML5, but native desktop gives better performance, simpler input handling, and no browser security sandbox limitations. Web export is a future possibility, not a priority. |
| External asset pipelines | No Aseprite CLI, no TexturePacker, no sprite sheet automation. Assets are imported directly into the Godot editor. |
| C# or C++ | GDScript only. C# requires .NET runtime and adds deployment complexity. C++ (GDExtension) is for performance-critical code we don't need. GDScript is sufficient for a 2D dungeon crawler. |
| 3D rendering | The game is 2D isometric. No 3D meshes, no 3D physics, no 3D camera. All depth is faked via tile stacking and draw order. |
| External physics engines | Godot's built-in 2D physics (CharacterBody2D, Area2D, CollisionShape2D) handles everything. No Box2D, no Rapier, no custom physics. |
| Build tools or package managers | No npm, no pip, no cargo, no Makefile. The Godot editor is the entire toolchain. Open the project, press F5, it runs. |
| Plugins or addons | No GDScript linter plugins, no visual scripting addons, no third-party node types. Vanilla Godot 4 only, to reduce maintenance burden and version conflicts. |
| Multiple scripting languages | GDScript everywhere. No mixing GDScript with C# or VisualScript. One language, one way to do things. |

### Migration Comparison: Phaser 3 vs Godot 4

This table maps every layer of the original Phaser prototype to its Godot 4 equivalent.

| Concern | Phaser 3 (old) | Godot 4 (new) | Migration notes |
|---------|----------------|---------------|-----------------|
| **Runtime** | Browser (Chrome, Firefox, Safari) | Native desktop (macOS, Windows, Linux) | No browser needed; Godot compiles to native binary |
| **Engine loading** | `<script>` tag loading Phaser CDN | Godot editor opens `project.godot` | No CDN, no network dependency; engine is installed locally |
| **Language** | Vanilla JavaScript (ES6+) | GDScript | Python-like syntax; `var` instead of `let/const`, `func` instead of `function`, indentation-based blocks |
| **Rendering** | Phaser.AUTO (WebGL + Canvas fallback) | GL Compatibility renderer | GPU-accelerated; no Canvas 2D fallback needed |
| **Physics** | Phaser Arcade Physics | Godot built-in 2D physics | CharacterBody2D replaces `physics.add.existing()`; `move_and_slide()` replaces `setVelocity()` |
| **Collision** | `physics.add.overlap()` callback | Area2D `body_entered` signal | Signal-based instead of callback registration; collision layers/masks for filtering |
| **Input** | `input.keyboard.createCursorKeys()` | Input Map + `Input.get_vector()` | Named actions ("move_left", "move_right") instead of raw key checks; supports rebinding |
| **Game objects** | `this.add.circle()`, `this.add.rectangle()` | Polygon2D, Sprite2D, ColorRect | Nodes in a scene tree instead of factory methods; each has its own properties in the Inspector |
| **Scene management** | `Phaser.Scene` class, `scene.restart()` | PackedScene + `get_tree().change_scene_to_file()` | Scenes are `.tscn` files edited visually; scene tree replaces scene manager |
| **Game loop** | `update(time, delta)` method | `_physics_process(delta)` / `_process(delta)` | `_physics_process` runs at fixed 60fps; `_process` runs at display refresh rate |
| **State** | Plain JS object (`const state = {...}`) | Autoload singleton (`GameState.gd`) | Globally accessible via autoload name; uses signals for reactivity |
| **HUD** | HTML `<p>` element above canvas | Control nodes (Label, Panel) in CanvasLayer | UI is part of the scene tree, not separate HTML; styled via Theme resources |
| **Persistence** | `localStorage.setItem()` | `FileAccess.open("user://save.json")` | File-based instead of browser storage; `user://` maps to OS-appropriate directory |
| **Responsive layout** | CSS Grid + Phaser Scale FIT/CENTER_BOTH | Stretch mode `canvas_items` + aspect `expand` | Godot handles scaling natively; no CSS needed |
| **Mobile input** | Pointer events + `movePointer` tracking | Not applicable (desktop native) | Touch support deferred; would use TouchScreenButton or InputEvent system |
| **Styling** | CSS custom properties (`:root` vars) | Theme resources (`.tres` files) | Colors, fonts, margins defined in Theme; applied to Control nodes |
| **Code organization** | Single `index.html` file | Scene files (`.tscn`) + script files (`.gd`) | Each entity gets its own scene + script pair; autoloads for global logic |
| **Background** | `this.add.graphics()` grid lines | TileMapLayer with isometric tiles | Tile-based rendering replaces procedural grid drawing |
| **Enemy group** | `this.physics.add.group()` | Node2D container + `get_tree().get_nodes_in_group("enemies")` | Groups are a tagging system; enemies are individual scene instances |
| **Tweens** | `this.tweens.add({targets, alpha, ...})` | `create_tween().tween_property(node, "modulate:a", ...)` | Godot Tween API is method-chained; properties are NodePaths |
| **Timers** | `this.time.addEvent({delay, loop, ...})` | Timer node or `get_tree().create_timer(delay)` | Timer is a scene node with `timeout` signal; one-shot uses `await` |
| **Camera** | `this.cameras.main.shake()` | Camera2D node with `position_smoothing`, offset, shake via tween | Camera is a node in the scene tree, follows player via `position` tracking |
| **Debug** | `window.__dungeonGame` in browser console | Godot debugger (F5), remote scene tree, print() to Output | Built-in debugger with breakpoints, variable inspection, scene tree viewer |

### Godot Version Policy

- Target: Godot 4.x stable (latest point release)
- No alpha/beta/RC builds in the main branch
- Update only when a new stable release offers features we need or fixes bugs we've hit
- The specific version is tracked in the `project.godot` file under `config/features`

### File Formats

| Extension | What it is | Human-readable? |
|-----------|-----------|-----------------|
| `.godot` | Project configuration | Yes — INI-like format, text-based |
| `.tscn` | Scene file (nodes + properties) | Yes — text-based, stores node tree |
| `.gd` | GDScript source file | Yes — Python-like script |
| `.tres` | Resource file (TileSet, Theme, etc.) | Yes — text-based, stores resource properties |
| `.import` | Auto-generated import settings | Yes — but managed by Godot, do not edit manually |
| `.png` | Image assets | Binary — tile sprites, character sprites |
| `.wav`/`.ogg` | Audio assets (future) | Binary — sound effects, music |

### The `.godot/` Directory

The `.godot/` directory is Godot's internal cache. It contains:
- `imported/` — converted versions of assets (e.g., `.png` reimported as `.ctex`)
- `editor/` — editor layout, recent files, breakpoints
- `uid_cache.bin` — unique ID cache for resources

This directory is **always gitignored**. It is regenerated automatically when the project is opened. Never commit it, never manually edit it.

## Open Questions

- When should we upgrade from GL Compatibility to Forward+ renderer? (Answer: when we need advanced 2D lighting effects like normal maps or 2D shadows)
- Should we add Godot export templates to the repo for reproducible builds, or download them on-demand?
- Will HTML5 export be pursued for a web demo, or is desktop-only sufficient?
- Should we pin a specific Godot version in documentation, or track "latest stable 4.x"?
