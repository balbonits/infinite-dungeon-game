# Tech Stack

## Summary

The game runs on Godot 4 (.NET edition) as a native desktop application using C# and .NET 8+. Development uses VS Code + Godot editor, with `dotnet build` and `make` for automation.

## Current State

- **Engine:** Godot 4.x .NET edition (open-source, MIT license)
- **Language:** C# / .NET 8+ (strong typing, PascalCase, partial classes)
- **Renderer:** GL Compatibility (broadest hardware support, forward+ available later)
- **Testing:** GdUnit4 (Godot scene tests) + xUnit (pure logic tests)
- **Serialization:** System.Text.Json (player saves) + MessagePack-CSharp v3 (floor cache, binary)
- **Object pooling:** Microsoft.Extensions.ObjectPool (enemies, effects, projectiles)
- **Async generation:** System.Threading.Channels (background floor generation)
- **Physics:** Built-in Godot 2D physics (CharacterBody2D + Area2D, no external library)
- **Perspective:** Isometric 2D — 2:1 diamond tiles (64x32 pixels), rendered via TileMapLayer
- **UI:** Built-in Control node system with Theme resources
- **Persistence:** FileAccess + JSON/MessagePack, saved to `user://` directory
- **Platform:** Desktop native — macOS primary development, Windows/Linux exportable
- **Dev workflow:** VS Code + Godot editor + terminal (`dotnet build`, `make check`)
- **Known limitation:** C# web export is not supported as of Godot 4.6. Desktop only.

## Design

### Core Stack

| Layer | Technology | Notes |
|-------|-----------|-------|
| Engine | Godot 4.x (.NET edition) | Open-source (MIT), scene/node architecture, separate download from standard Godot |
| Language | C# / .NET 8+ | Strong typing, PascalCase, partial classes, nullable enabled |
| Renderer | GL Compatibility | OpenGL-based, broadest hardware support; Forward+ available for upgrade later |
| Testing | GdUnit4 + xUnit | GdUnit4 for Godot scene/node tests, xUnit for pure C# logic tests |
| Serialization (saves) | System.Text.Json | Source-generated, human-readable JSON, AOT-friendly |
| Serialization (cache) | MessagePack-CSharp v3 | Binary format, ~10x faster than JSON, source generator support |
| Object pooling | Microsoft.Extensions.ObjectPool | Pool enemies, effects, projectiles — avoids GC pressure |
| Async generation | System.Threading.Channels | Producer/consumer pipeline for background floor generation |
| Physics | Built-in 2D | CharacterBody2D for player/enemies, Area2D for detection zones |
| Perspective | Isometric 2D | 2:1 diamond tiles (64x32 px), TileMapLayer node, diamond tile shape mode |
| UI | Control nodes | Built-in UI system — Label, Button, Panel, VBoxContainer; Theme resources |
| Persistence | FileAccess + JSON/MessagePack | `user://` directory; JSON for player saves, MessagePack for floor cache |
| Platform | Desktop native | macOS primary; Windows and Linux via Godot export templates |
| Dev workflow | VS Code + Godot + terminal | `dotnet build`, `dotnet test`, `dotnet format`, `make check` |

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
| Web/browser deployment | C# web export is not supported in Godot 4.6. Desktop native only. |
| GDScript | Switched to C# for type safety, better IDE tooling, richer ecosystem, and superior AI code generation quality. |
| External asset pipelines | No Aseprite CLI, no TexturePacker. Assets are imported directly into the Godot editor. |
| C++ (GDExtension) | For performance-critical code we don't need. C# with object pooling and async generation is sufficient. |
| 3D rendering | The game is 2D isometric. No 3D meshes, no 3D physics, no 3D camera. All depth is faked via tile stacking and draw order. |
| External physics engines | Godot's built-in 2D physics handles everything. No Box2D, no Rapier, no custom physics. |
| Heavy frameworks | No Chickensoft in game code, no ECS. The game's scope doesn't justify framework overhead. Chickensoft packages (GodotTestDriver) are allowed for testing tools only (`scripts/testing/`). |
| Multiple scripting languages | C# everywhere. No mixing C# with GDScript. One language, one way to do things. |

### Migration Comparison: Phaser 3 vs Godot 4

This table maps every layer of the original Phaser prototype to its Godot 4 equivalent.

| Concern | Phaser 3 (old) | Godot 4 (new) | Migration notes |
|---------|----------------|---------------|-----------------|
| **Runtime** | Browser (Chrome, Firefox, Safari) | Native desktop (macOS, Windows, Linux) | No browser needed; Godot compiles to native binary |
| **Engine loading** | `<script>` tag loading Phaser CDN | Godot editor opens `project.godot` | No CDN, no network dependency; engine is installed locally |
| **Language** | Vanilla JavaScript (ES6+) | C# (.NET 8+) | Strongly-typed; `public partial class` for nodes, PascalCase methods, `using` instead of `import` |
| **Rendering** | Phaser.AUTO (WebGL + Canvas fallback) | GL Compatibility renderer | GPU-accelerated; no Canvas 2D fallback needed |
| **Physics** | Phaser Arcade Physics | Godot built-in 2D physics | CharacterBody2D replaces `physics.add.existing()`; `move_and_slide()` replaces `setVelocity()` |
| **Collision** | `physics.add.overlap()` callback | Area2D `body_entered` signal | Signal-based instead of callback registration; collision layers/masks for filtering |
| **Input** | `input.keyboard.createCursorKeys()` | Input Map + `Input.get_vector()` | Named actions ("move_left", "move_right") instead of raw key checks; supports rebinding |
| **Game objects** | `this.add.circle()`, `this.add.rectangle()` | Polygon2D, Sprite2D, ColorRect | Nodes in a scene tree instead of factory methods; each has its own properties in the Inspector |
| **Scene management** | `Phaser.Scene` class, `scene.restart()` | PackedScene + `get_tree().change_scene_to_file()` | Scenes are `.tscn` files edited visually; scene tree replaces scene manager |
| **Game loop** | `update(time, delta)` method | `_physics_process(delta)` / `_process(delta)` | `_physics_process` runs at fixed 60fps; `_process` runs at display refresh rate |
| **State** | Plain JS object (`const state = {...}`) | Autoload singleton (`GameState.cs`) | Globally accessible via static Instance; uses [Signal] delegates for reactivity |
| **HUD** | HTML `<p>` element above canvas | Control nodes (Label, Panel) in CanvasLayer | UI is part of the scene tree, not separate HTML; styled via Theme resources |
| **Persistence** | `localStorage.setItem()` | `FileAccess.Open("user://save.json")` | File-based instead of browser storage; `user://` maps to OS-appropriate directory |
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
| `.cs` | C# source file | Yes — strongly-typed, compiled |
| `.csproj` | .NET project file (NuGet refs, SDK) | Yes — XML, defines build configuration |
| `.sln` | .NET solution file | Yes — references .csproj files |
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

### Performance: C# vs GDScript

C# maintains a **2-3x advantage** for compute-heavy tasks (array sorting, physics queries, AI state machines). For typical game logic (input, state management), the difference is negligible. The primary reasons for choosing C# are type safety, IDE tooling, and AI code generation quality — not raw performance.

### NuGet Dependencies

> **Note:** These packages are the target dependencies. The current DungeonGame.csproj is minimal (Godot.NET.Sdk only, no NuGet references). Packages will be added as features are implemented.

```xml
<ItemGroup>
  <!-- Testing -->
  <PackageReference Include="gdUnit4.api" Version="5.1.0" />
  <PackageReference Include="gdUnit4.test.adapter" Version="3.0.0" />
  <PackageReference Include="gdUnit4.analyzers" Version="1.0.0" />
  <PackageReference Include="xunit" Version="2.9.3" />
  <PackageReference Include="xunit.runner.visualstudio" Version="3.0.2" />
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.0.0" />
  <!-- Serialization (binary floor cache) -->
  <PackageReference Include="MessagePack" Version="3.1.4" />
  <!-- Object pooling -->
  <PackageReference Include="Microsoft.Extensions.ObjectPool" Version="9.0.0" />
</ItemGroup>
```

### Platform Support

| Platform | Status | Notes |
|----------|--------|-------|
| macOS | Stable | Primary dev platform (Apple Silicon + Intel) |
| Windows | Stable | CoreCLR runtime |
| Linux | Stable | CoreCLR runtime |
| Web | Not supported | C# web export blocked in Godot 4.6 |
| Mobile | Experimental | Android (Mono), iOS (NativeAOT) — not targeted |

## Open Questions

- When should we upgrade from GL Compatibility to Forward+ renderer? (Answer: when we need advanced 2D lighting effects like normal maps or 2D shadows)
- Should we add Godot export templates to the repo for reproducible builds, or download them on-demand?
- Should we pin a specific Godot version in documentation, or track "latest stable 4.x"?
