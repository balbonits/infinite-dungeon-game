# Project Structure

## Summary

Complete file organization for the Godot 4 migration of "A Dungeon in the Middle of Nowhere." Every file, its purpose, and the reasoning behind its placement.

## Current State

The project is transitioning from a single-file Phaser 3 prototype (preserved in `archive/`) to a multi-file Godot 4 project. The directory structure below represents the target layout — files will be created as features are implemented, following a "docs first, code later" approach.

## Design

### Full Directory Tree

```
dungeon-web-game/
├── project.godot                          — Godot 4 project config
├── .gitignore                             — Git ignore rules
├── .gdignore                              — Godot editor ignore rules
├── AGENTS.md                              — AI coding assistant guidelines
├── CLAUDE.md                              — Points to AGENTS.md
├── README.md                              — Project overview, how to run
├── CHANGELOG.md                           — Version history
├── archive/                               — Preserved Phaser 3 prototype
│   └── phaser-prototype/
│       ├── index.html                     — Original Phaser 3 prototype
│       └── docs/
│           ├── phaser-basics.md           — Phaser 3 reference (archived)
│           ├── single-file.md             — Single-file rationale (archived)
│           └── code-map.md                — Phaser code walkthrough (archived)
├── docs/                                  — Game design documentation
│   ├── overview.md                        — Project vision and design philosophy
│   ├── best-practices.md                  — Development guidelines and workflow
│   ├── architecture/                      — Technical architecture
│   │   ├── tech-stack.md                  — Godot 4 stack, window config, migration table
│   │   ├── godot-basics.md                — Godot 4 concepts for web developers
│   │   ├── project-structure.md           — This file (directory layout, naming conventions)
│   │   └── game-dev-concepts.md           — Game dev fundamentals mapped to web dev
│   ├── objects/                           — Game entity specifications
│   ├── assets/                            — Asset specifications
│   ├── systems/                           — Game systems design
│   │   ├── stats.md                       — STR/DEX/STA/INT stat system
│   │   ├── classes.md                     — Warrior/Marksman/Mage classes
│   │   ├── skills.md                      — Skill trees per class (hierarchical, infinite leveling)
│   │   ├── combat.md                      — Auto-targeting, cooldowns, damage formulas
│   │   ├── leveling.md                    — XP curve, level-up effects
│   │   ├── death.md                       — Death penalties, gold buyout, Sacrificial Idol
│   │   └── save.md                        — Save system (FileAccess + JSON)
│   ├── world/                             — World design
│   │   ├── dungeon.md                     — Infinite descent, floor generation, caching
│   │   ├── town.md                        — Town hub, NPC list, interaction
│   │   └── monsters.md                    — Enemy types, danger tiers, spawning
│   ├── inventory/                         — Inventory systems
│   │   ├── backpack.md                    — Risky carry storage (25 slots)
│   │   ├── bank.md                        — Safe town storage (15 slots)
│   │   └── items.md                       — Item system (deferred)
│   ├── ui/                                — UI design
│   │   ├── controls.md                    — Input methods (keyboard, mouse, gamepad)
│   │   ├── hud.md                         — HUD overlay display
│   │   └── death-screen.md                — Death UI flow
│   └── testing/                           — Test plans
├── scenes/                                — Godot scene files (.tscn) + scripts (.gd)
│   ├── main.tscn                          — Entry point scene (root of the game)
│   ├── main.gd                            — Entry point script (scene transitions, UI layer)
│   ├── dungeon/
│   │   ├── dungeon.tscn                   — Dungeon floor scene
│   │   └── dungeon.gd                     — Dungeon floor logic (tilemap, spawning, entity container)
│   ├── player/
│   │   ├── player.tscn                    — Player character scene
│   │   └── player.gd                      — Player logic (movement, auto-attack, camera)
│   ├── enemies/
│   │   ├── enemy.tscn                     — Base enemy scene
│   │   └── enemy.gd                       — Base enemy logic (chase AI, damage, tier system)
│   └── ui/
│       ├── hud.tscn                       — HUD overlay scene
│       ├── hud.gd                         — HUD logic (stats display, signal listeners)
│       ├── death_screen.tscn              — Death/restart screen scene
│       └── death_screen.gd                — Death screen logic (restart, penalties)
├── scripts/                               — Standalone scripts (not attached to scenes)
│   └── autoloads/
│       ├── game_state.gd                  — Singleton: HP, XP, level, floor, signals
│       └── event_bus.gd                   — Singleton: decoupled signal hub
├── assets/                                — Binary resources (images, audio)
│   ├── tiles/                             — Isometric tile PNG images
│   │   ├── floor.png                      — Floor tile (filled 64x32 diamond)
│   │   └── wall.png                       — Wall tile (outlined diamond, used with physics)
│   ├── sprites/                           — Character and enemy sprite images (future)
│   └── ui/                                — UI-specific images and icons (future)
└── resources/                             — Godot resource files (.tres)
    ├── tile_sets/
    │   └── dungeon_tileset.tres           — Isometric TileSet (tile size, shapes, physics, atlas)
    └── themes/
        └── game_theme.tres                — Dark fantasy UI Theme (colors, fonts, StyleBoxes)
```

### File-by-File Explanations

#### Root Files

| File | Purpose | Details |
|------|---------|---------|
| `project.godot` | Godot project configuration | INI-like text file. Contains: project name, window size (1920x1080), stretch settings (canvas_items / expand), input map (WASD + arrows), autoload registrations (GameState, EventBus), renderer setting (gl_compatibility), default texture filter (nearest). This is the equivalent of `package.json` + `webpack.config.js` + `tsconfig.json` all in one. Edited via Project Settings UI or directly as text. |
| `.gitignore` | Git ignore rules | Must ignore: `.godot/` (editor cache, regenerated on open), `export/` (build output), `.DS_Store` (macOS), `*.import` is debatable (auto-generated but tracked by convention in Godot projects). |
| `.gdignore` | Godot editor ignore rules | Placed in the repo root, tells Godot to skip importing files in `archive/`. Without this, Godot would try to import the Phaser HTML file and any images in the archive as game assets, wasting time and polluting the FileSystem dock. Content: just the file's existence in a directory causes Godot to ignore that directory's children. In our case, place it inside `archive/` or configure it to exclude `archive/`. |
| `AGENTS.md` | AI coding assistant guidelines | Rules for any AI tool (Claude, Copilot, etc.) helping with this project. Contains: naming conventions, tech stack summary, game design quick reference, priorities. Will be updated from Phaser conventions to Godot conventions. |
| `CLAUDE.md` | Claude Code pointer | Single line pointing to `AGENTS.md`. Exists because Claude Code looks for `CLAUDE.md` specifically. Keeps the actual content in one place (`AGENTS.md`) to avoid duplication. |
| `README.md` | Project overview | How to clone, how to open in Godot, how to run (F5), what the game is. Not detailed design — that's in `docs/`. |
| `CHANGELOG.md` | Version history | Tracks what changed in each version. Dated entries with bullet points. Follows Keep a Changelog format. |

#### archive/ Directory

| Path | Purpose | Details |
|------|---------|---------|
| `archive/phaser-prototype/index.html` | Original Phaser 3 prototype | The complete single-file game that started the project. Preserved as reference for the migration. Contains: HTML structure, CSS theme, Phaser game loop, player movement, enemy AI, auto-attack combat, HUD overlay, death screen. 451 lines total. |
| `archive/phaser-prototype/docs/` | Archived Phaser-specific docs | Documentation that was specific to the Phaser 3 / single-file approach. Moved here because it's no longer applicable to the Godot version, but valuable for understanding original design decisions. |
| `archive/phaser-prototype/docs/phaser-basics.md` | Phaser 3 concepts reference | Archived. Explained Phaser-specific APIs (Scene lifecycle, physics groups, tweens). Replaced by `docs/architecture/godot-basics.md`. |
| `archive/phaser-prototype/docs/single-file.md` | Single-file rationale | Archived. Explained why everything was in one HTML file. No longer applicable — Godot uses multiple files by design. |
| `archive/phaser-prototype/docs/code-map.md` | Phaser code walkthrough | Archived. Line-by-line annotation of `index.html`. Useful for understanding what needs to be migrated but not for ongoing development. |

**Why preserve the archive:** The original prototype is the ground truth for "what the game should do." During migration, we can reference it to ensure feature parity. It also documents the journey from web prototype to native game engine.

#### docs/ Directory

| Path | Purpose | Details |
|------|---------|---------|
| `docs/overview.md` | Project vision | Design philosophy, inspiration (Diablo 1), core principles (persistent character, meaningful death, infinite depth). Engine-agnostic — focuses on what the game IS, not how it's built. |
| `docs/best-practices.md` | Development workflow | How to propose changes, testing checklist, code review process. Updated for Godot workflow (scene-based, not single-file). |
| `docs/architecture/tech-stack.md` | Technology decisions | Godot 4 stack table, window configuration, what we avoid, migration comparison from Phaser. The "why" behind every technology choice. |
| `docs/architecture/godot-basics.md` | Godot for web devs | Bridge document mapping web concepts (DOM, CSS, events, components) to Godot equivalents (scene tree, Inspector, signals, scenes). Essential reading for a front-end developer learning Godot. |
| `docs/architecture/project-structure.md` | This file | Directory layout, naming conventions, file purposes. The "map" of the codebase. |
| `docs/architecture/game-dev-concepts.md` | Game dev fundamentals | Game loop, delta time, collision, state management — explained through web dev analogies with GDScript examples. |
| `docs/systems/*.md` | Game system specs | Detailed specifications for stats, classes, combat, leveling, death penalties, save system. Engine-agnostic where possible — describe what the system does, not how it's implemented. |
| `docs/world/*.md` | World design specs | Dungeon structure, town hub, monster types. Describes the game world and its rules. |
| `docs/inventory/*.md` | Inventory system specs | Backpack, bank, item definitions. Describes storage and loot mechanics. |
| `docs/ui/*.md` | UI design specs | Controls, HUD layout, death screen flow. Describes what the player sees and interacts with. |
| `docs/testing/` | Test plans | Manual testing checklists, edge cases, regression tests. Empty initially — populated as features are implemented. |

**Why docs/ stays engine-agnostic where possible:** System design docs (stats, combat, death penalties) describe game rules that don't change if we switched engines again. Only `docs/architecture/` contains Godot-specific documentation. This separation means game designers can read system docs without knowing Godot.

#### scenes/ Directory

| Path | Purpose | Node tree | Details |
|------|---------|-----------|---------|
| `scenes/main.tscn` + `main.gd` | Entry point scene | `Main (Node2D)` → `CurrentScene (Node2D)` + `UILayer (CanvasLayer)` | The root scene loaded when the game starts. Manages scene transitions (dungeon ↔ town) by swapping children of `CurrentScene`. `UILayer` holds persistent UI (HUD) that survives scene changes. The `main.gd` script handles `change_scene()` logic and connects to GameState signals. |
| `scenes/dungeon/dungeon.tscn` + `dungeon.gd` | Dungeon floor | `Dungeon (Node2D)` → `TileMapLayer` + `EntityContainer (Node2D, y_sort_enabled)` + `Player` (instanced) + `SpawnTimer (Timer)` | One floor of the dungeon. The TileMapLayer renders isometric tiles. EntityContainer holds enemies with Y-sorting for correct draw order. Player is an instanced scene. SpawnTimer triggers periodic enemy spawning. `dungeon.gd` handles floor generation, enemy spawning, and entity management. |
| `scenes/player/player.tscn` + `player.gd` | Player character | `Player (CharacterBody2D)` → `Polygon2D` + `CollisionShape2D` + `AttackRange (Area2D)` → `CollisionShape2D` + `AttackCooldownTimer (Timer)` + `Camera2D` | The player entity. CharacterBody2D for physics movement. Polygon2D is the temporary colored shape (replaced with Sprite2D when art exists). AttackRange detects nearby enemies for auto-attack. Camera2D follows the player with smoothing. `player.gd` handles input, movement via `move_and_slide()`, auto-attack logic, and damage. |
| `scenes/enemies/enemy.tscn` + `enemy.gd` | Base enemy | `Enemy (CharacterBody2D)` → `Polygon2D` + `CollisionShape2D` + `HitCooldownTimer (Timer)` | A single enemy instance. CharacterBody2D for movement. Polygon2D colored by danger tier (green/yellow/red). CollisionShape2D for physics. HitCooldownTimer prevents damage every frame. `enemy.gd` has `@export` properties (max_hp, move_speed, danger_tier, enemy_color) and handles chase AI, taking damage, and death (queue_free + signal). |
| `scenes/ui/hud.tscn` + `hud.gd` | HUD overlay | `HUD (Control)` → `PanelContainer` → `VBoxContainer` → `TitleLabel` + `HPLabel` + `XPLabel` + `LevelLabel` + `FloorLabel` | The persistent heads-up display. Shows HP, XP, level, floor number. Lives in the UILayer (CanvasLayer) so it stays on screen regardless of camera movement. `hud.gd` connects to `GameState.stats_changed` signal and updates labels reactively. |
| `scenes/ui/death_screen.tscn` + `death_screen.gd` | Death/restart screen | `DeathScreen (Control)` → `PanelContainer` → `VBoxContainer` → `TitleLabel` + `MessageLabel` + `RestartButton` | Shown when HP reaches 0. Displays "You Died" message and a restart option. `death_screen.gd` connects RestartButton.pressed to restart logic (`get_tree().reload_current_scene()` or scene transition back to dungeon). Fades in with a tween for visual polish. |

**Why scenes/ is organized by entity:** Each game entity (player, enemy, dungeon, UI element) is a self-contained unit — its scene file defines the visual/physics structure, its script file defines the behavior. Grouping `.tscn` + `.gd` together makes it obvious what files belong to what entity. This mirrors the "component" pattern from web development where a component's template and logic live together.

**Why .tscn and .gd have the same name:** Godot convention. When you attach a script to a scene's root node, it naturally gets the same name. `player.tscn` is the scene, `player.gd` is the script attached to the root `Player` node. This makes it unambiguous which script belongs to which scene.

#### scripts/ Directory

| Path | Purpose | Details |
|------|---------|---------|
| `scripts/autoloads/game_state.gd` | Global game state singleton | Extends Node. Holds: `hp`, `max_hp`, `xp`, `level`, `floor_number`. Emits signals: `stats_changed`, `player_died`, `floor_changed`. Uses property setters to auto-emit signals on change. Provides `reset()` method for new game. Registered as Autoload named `GameState` in project.godot. Accessible from any script as `GameState.hp`, `GameState.stats_changed.connect(...)`, etc. Replaces the `const state = { hp, xp, level }` object from the Phaser prototype. |
| `scripts/autoloads/event_bus.gd` | Decoupled signal hub | Extends Node. Declares signals that multiple unrelated systems need: `enemy_defeated(enemy)`, `floor_completed(floor_number)`, `item_dropped(item_data)`. Systems emit to EventBus; other systems listen. Prevents direct coupling between, say, the combat system and the UI. Registered as Autoload named `EventBus` in project.godot. |

**Why autoloads are separate from scenes:** Autoloads are scripts that Godot loads at startup and keeps alive forever. They are NOT instanced scenes — they don't have `.tscn` files (though they could). Keeping them in `scripts/autoloads/` makes it clear they serve a different purpose than scene scripts. They are global singletons, not entities in the game world.

**Why two autoloads (GameState vs EventBus):** `GameState` owns data (hp, xp, level) and emits signals when that data changes. `EventBus` is a pure signal relay — it owns no data, just provides a place for decoupled communication. Keeping them separate follows the single-responsibility principle. `GameState` answers "what is the current state?" `EventBus` answers "what just happened?"

#### assets/ Directory

| Path | Purpose | Details |
|------|---------|---------|
| `assets/tiles/floor.png` | Floor tile image | 64x32 pixel PNG. Filled diamond shape. Dark stone color. Used in the TileSet as the walkable floor tile. No collision polygon. |
| `assets/tiles/wall.png` | Wall tile image | 64x32 pixel PNG (may be taller for the wall "face"). Outlined diamond with depth shading. Used in the TileSet as the impassable wall tile. Has a collision polygon matching its shape. |
| `assets/sprites/` | Character and enemy sprites | Future — currently using Polygon2D nodes (colored shapes). When pixel art is created, sprite sheets go here. Will be referenced by Sprite2D nodes in scenes. |
| `assets/ui/` | UI images and icons | Future — currently using default Godot Control styling. When custom UI art is created, icons and panel backgrounds go here. Will be referenced by Theme resources. |

**Why assets/ is separate from resources/:** `assets/` contains **binary source files** — PNGs, WAVs, OGGs that are created in external tools (image editors, audio editors). `resources/` contains **Godot-specific configuration files** — `.tres` files created and edited within the Godot editor. The distinction is: assets come from outside Godot, resources are created inside Godot.

#### resources/ Directory

| Path | Purpose | Details |
|------|---------|---------|
| `resources/tile_sets/dungeon_tileset.tres` | Isometric TileSet | Defines: tile size (64x32), tile shape (diamond), tile layout (diamond down), tile offset axis (horizontal). Contains tile atlas referencing `assets/tiles/*.png`. Each tile has: texture region, collision polygon (walls only), custom data (tile type enum). Used by TileMapLayer nodes in dungeon scenes. |
| `resources/themes/game_theme.tres` | Dark fantasy UI Theme | Defines: default font, font sizes, colors for all Control node types. Panel StyleBox: dark semi-transparent background (like the Phaser `--panel` color), gold border (like `--panel-border`), rounded corners. Label colors: `--ink` for default text, `--accent` for titles, `--danger` for HP warnings. Button styles: normal, hover, pressed, disabled states. Applied to the root Control node of the UI — all children inherit it. |

**Why .tres instead of configuring in scenes:** Resources are **shared**. If two scenes need the same TileSet, they reference the same `.tres` file. Changing the `.tres` updates both scenes. Without shared resources, you'd duplicate configuration in every scene that needs it — like duplicating CSS inline on every element.

### Naming Conventions

#### File Names

All files use `snake_case` with their appropriate extension:

| Type | Convention | Example |
|------|-----------|---------|
| GDScript files | `snake_case.gd` | `game_state.gd`, `player.gd`, `death_screen.gd` |
| Scene files | `snake_case.tscn` | `player.tscn`, `dungeon.tscn`, `hud.tscn` |
| Resource files | `snake_case.tres` | `dungeon_tileset.tres`, `game_theme.tres` |
| Image files | `snake_case.png` | `floor.png`, `wall.png`, `player_idle.png` |
| Documentation | `kebab-case.md` | `tech-stack.md`, `game-dev-concepts.md`, `death-screen.md` |

**Why `snake_case` for Godot files:** This is the Godot community convention. GDScript itself uses `snake_case` for variables and functions, so file names match. The Godot editor generates `snake_case` file names by default when creating new scripts and scenes.

**Why `kebab-case` for docs:** Markdown documentation follows web conventions where `kebab-case` is standard for URLs and file names. Docs are not loaded by Godot — they're read by humans and rendered by GitHub/markdown viewers.

#### GDScript Identifiers

| Identifier type | Convention | Example |
|----------------|-----------|---------|
| Variables | `snake_case` | `move_speed`, `max_hp`, `danger_tier`, `floor_number` |
| Functions | `snake_case` | `handle_movement()`, `spawn_enemy()`, `take_damage()` |
| Constants | `UPPER_SNAKE_CASE` | `MOVE_SPEED`, `ATTACK_COOLDOWN`, `MAX_ENEMIES` |
| Signals | `snake_case`, past tense for events | `enemy_defeated`, `stats_changed`, `player_died`, `floor_completed` |
| Enums | `PascalCase` name, `UPPER_SNAKE_CASE` values | `enum DangerTier { LOW, MEDIUM, HIGH }` |
| Class names | `PascalCase` (rare in GDScript) | `class_name EnemyData` (only when needed for type hints or editor visibility) |

**Why past tense for signal names:** Signals represent events that have already happened. `enemy_defeated` means "an enemy was just defeated" — listeners react to something that already occurred. This prevents ambiguity: `enemy_defeat` could be interpreted as "request to defeat an enemy" or "an enemy was defeated."

#### Node Names in Scenes

| Convention | Example | Details |
|-----------|---------|---------|
| `PascalCase` for all nodes | `Player`, `CollisionShape2D`, `AttackRange`, `HPLabel` | Matches Godot editor default behavior. When you add a node, Godot names it in PascalCase. |
| Type suffix when ambiguous | `HitCooldownTimer`, `AttackCooldownTimer` | If a scene has multiple Timer nodes, the name clarifies which timer it is. |
| No type suffix when obvious | `Camera2D`, `CollisionShape2D` | When there's only one of that type, the default Godot name is fine. |

**Why PascalCase for nodes:** This is Godot's built-in convention. When you add a `Timer` node, it's named `Timer`. When you add a `CharacterBody2D`, it's named `CharacterBody2D`. Following the default means less manual renaming and instant recognition.

#### Groups

| Convention | Example | Used for |
|-----------|---------|----------|
| `snake_case`, singular noun | `"player"` | Identifying the player node from other scripts |
| `snake_case`, plural noun | `"enemies"` | Batch operations on all enemies |
| `snake_case`, adjective | `"damageable"` | Capability-based grouping (can take damage) |

#### No Abbreviations

| Do not use | Use instead | Why |
|-----------|------------|-----|
| `plr` | `player` | Clarity over brevity |
| `lvl` | `level` | Readable without context |
| `pos` | `position` | Matches Godot's own API (`node.position`) |
| `vel` | `velocity` | Matches Godot's own API (`body.velocity`) |
| `hp` | `hp` | Exception — HP is a universally understood game abbreviation |
| `xp` | `xp` | Exception — XP is a universally understood game abbreviation |
| `ai` | `ai` | Exception — AI is a universally understood abbreviation |
| `ui` | `ui` | Exception — UI is a universally understood abbreviation |
| `hud` | `hud` | Exception — HUD is a universally understood abbreviation |

**Rule of thumb:** If the abbreviation is used in everyday conversation by non-programmers (HP, XP, AI, UI, HUD), it's fine. If it's a programmer shorthand (plr, lvl, pos, vel, btn, cb, fn), spell it out.

### Why This Structure

**`scenes/` organized by game entity:**
Each entity (player, enemies, dungeon, UI) is a self-contained folder. The `.tscn` file defines what the entity looks like (node tree, physics shapes, visual elements). The `.gd` file defines what the entity does (movement, AI, damage). They live together because they change together — you never modify `player.tscn` without also touching `player.gd`.

**`scripts/autoloads/` separate from `scenes/`:**
Autoloads are NOT instanced into scenes. They don't have `.tscn` files. They're standalone scripts registered in `project.godot`. Putting them in `scenes/` would be misleading — they're not scenes. Putting them in `scripts/` makes it clear they're a different category of code.

**`assets/` for binary resources, `resources/` for Godot resource files:**
This prevents confusion about what's created where. If you need to edit a `.png`, open an image editor and look in `assets/`. If you need to edit a TileSet, open the Godot editor and look in `resources/`. Two different tools, two different directories.

**`docs/` stays engine-agnostic where possible:**
Game design docs (combat formulas, death penalties, stat scaling) describe game rules, not engine implementation. If we ever migrate again (unlikely, but possible), the design docs survive unchanged. Only `docs/architecture/` contains Godot-specific documentation.

**`archive/` preserves the original prototype:**
The Phaser prototype is the reference implementation. During migration, we compare Godot behavior against the prototype to verify feature parity. After migration is complete, the archive serves as historical documentation of the project's origins.

**Why no `tests/` directory (yet):**
Godot has a built-in testing framework (GUT — Godot Unit Test) but we haven't set it up yet. When we do, tests will go in a `tests/` directory at the project root, with a structure mirroring `scenes/`. The `docs/testing/` directory is for test PLANS (manual checklists), not automated test scripts.

**Why no `addons/` directory:**
We intentionally avoid Godot plugins/addons. Vanilla Godot 4 only. If we ever add an addon (like GUT for testing), it would go in the standard `addons/` directory that Godot expects.

## Open Questions

- Should `.import` files be gitignored or committed? Godot regenerates them, but committing them prevents the "first open" import delay for new contributors.
- Should enemy variants (different tiers, different behaviors) be separate scenes inheriting from `enemy.tscn`, or a single configurable scene with `@export` properties?
- Should we add a `data/` directory for JSON configuration files (enemy stats, loot tables) that are loaded at runtime, or keep everything in `@export` properties and resources?
- When automated tests are added, should they live in `tests/` at the root or inside each entity's folder (e.g., `scenes/player/test_player.gd`)?
