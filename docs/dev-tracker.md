# Development Tracker

Single source of truth for what's done, what's next, and what's blocked. Specs are completed before implementation begins — no code until the relevant doc is locked.

## How to Use

- `[x]` = done, `[ ]` = not started, `[~]` = in progress
- Each checkbox is roughly one task/commit
- Spec items gate their corresponding implementation items
- Links point to the relevant doc for full details

---

## Spec Gaps

These docs need writing or completion before their systems can be implemented. **This section gates all implementation work below.**

### Needs New Doc

- [ ] `docs/world/town.md` — town hub layout, NPC interactions, shop mechanics, blacksmith recycling ([overview](overview.md), [town ref](world/town.md))
- [ ] `docs/systems/crafting.md` — blacksmith recycling system, material extraction ([project ref](../AGENTS.md))
- [ ] `docs/systems/spell-acquisition.md` — Mage scroll osmosis learning system ([skills ref](systems/skills.md))

### Needs Completion (has open questions or deferred sections)

- [ ] `docs/systems/stats.md` — lock stat-to-formula bindings (STR->damage, DEX->speed, STA->HP, INT->magic) ([stats](systems/stats.md))
- [ ] `docs/systems/classes.md` — concrete per-level scaling bonuses for each class ([classes](systems/classes.md))
- [ ] `docs/systems/leveling.md` — finalize XP curve constant (45?), floor multiplier shape, milestone rewards ([leveling](systems/leveling.md))
- [ ] `docs/systems/skills.md` — exact passive bonuses per level, diminishing returns formula, unlock progression ([skills](systems/skills.md))
- [ ] `docs/inventory/items.md` — loot tables, item stats/formulas, drop rates, item balancing ([items](inventory/items.md))
- [ ] `docs/inventory/backpack.md` — UI layout, slot interaction, death-loss selection logic ([backpack](inventory/backpack.md))
- [ ] `docs/inventory/bank.md` — UI layout, deposit/withdraw flow, slot expansion ([bank](inventory/bank.md))
- [ ] `docs/world/dungeon.md` — generation algorithm, floor difficulty curve, special room types, boss floors ([dungeon](world/dungeon.md))
- [ ] `docs/systems/save.md` — resolve multiple save slots, backup rotation, file size with cached floors ([save](systems/save.md))
- [~] `docs/architecture/autoloads.md` — open questions resolved, C# pseudocode update in progress ([autoloads](architecture/autoloads.md))

### Nice-to-Have (post-MVP docs)

- [ ] `docs/architecture/game-dev-concepts.md` — entity-component patterns, state machines, shader basics
- [x] `docs/architecture/tech-stack.md` — updated for C# stack, platform matrix, NuGet deps
- [ ] `docs/systems/player-engagement.md` — finalize hitstop duration, screen flash specs, daily challenge variant

---

## Phase 0: Project Scaffold

### 0.1 Original Scaffold (GDScript era — done)

- [x] `project.godot` — Godot 4.6 config (1920x1080, GL Compatibility)
- [x] `Makefile` — automation targets ([ai-workflow](architecture/ai-workflow.md))
- [x] `.github/workflows/ci.yml` — lint + test on push/PR
- [x] `.githooks/pre-commit` — pre-commit hook
- [x] `scripts/generate_tiles.py` + tile assets
- [x] `.gitignore`, `.editorconfig`

### 0.2 C# Migration (in progress)

- [~] Update AGENTS.md for C# conventions, tech stack, tooling
- [~] Update CLAUDE.md for C# mode
- [x] Create `docs/architecture/setup-guide.md` — .NET SDK, Godot .NET, VS Code setup
- [~] Update all docs: GDScript pseudocode → C# pseudocode
- [ ] Remove `addons/gut/` — replaced by GdUnit4 (NuGet)
- [ ] Remove `tests/test_project_setup.gd` — rewrite in C#
- [ ] Add `DungeonGame.csproj` + `DungeonGame.sln`
- [ ] Update `project.godot` for .NET edition
- [ ] Update `Makefile` — `dotnet build/test/format` targets
- [ ] Update `.github/workflows/ci.yml` — .NET SDK + dotnet build/test/format
- [ ] Update `.githooks/pre-commit` — `dotnet format` on staged .cs files
- [ ] Update `.editorconfig` — add C# section
- [ ] Update `.gitignore` — add bin/, obj/, *.user, *.DotSettings
- [ ] NuGet restore: GdUnit4, xUnit, MessagePack, ObjectPool
- [ ] Verify `dotnet build` succeeds
- [ ] Verify `dotnet test` runs (sanity tests in C#)

---

## Phase 1: Prototype Parity

Reimplement the Phaser prototype's core loop in Godot C#. Specs for this phase are complete.

### 1.1 Autoloads

Spec: [autoloads.md](architecture/autoloads.md), [signals.md](architecture/signals.md)

- [ ] `scripts/autoloads/GameState.cs` — Hp, MaxHp, Xp, Level, FloorNumber, IsDead, Reset(), TakeDamage(), AwardXp()
- [ ] `scripts/autoloads/EventBus.cs` — decoupled gameplay signals
- [ ] Register both in project.godot
- [ ] Tests: `tests/TestGameState.cs` (24 unit tests) ([automated-tests](testing/automated-tests.md))

### 1.2 Tilemap & Dungeon Room

Spec: [tilemap.md](objects/tilemap.md), [tile-specs.md](assets/tile-specs.md)

- [ ] `scenes/dungeon/Dungeon.tscn` + `scripts/dungeon/Dungeon.cs` — 10x10 bordered room, programmatic TileSet
- [ ] Floor tiles (dark blue `#1a2438`) + wall tiles (outlined, with collision)
- [ ] Diamond collision polygon for wall sliding
- [ ] Tests: MT-002 (isometric floor)

### 1.3 Player

Spec: [player.md](objects/player.md), [movement.md](systems/movement.md), [camera.md](systems/camera.md)

- [ ] `scenes/player/Player.tscn` + `scripts/player/Player.cs` — CharacterBody2D, Polygon2D diamond (`#8ed6ff`)
- [ ] Isometric 8-direction movement (190 px/s), input map (WASD + arrows)
- [ ] Camera2D child (2x zoom, position smoothing 5.0)
- [ ] CollisionShape2D, attack range Area2D (78px)
- [ ] Tests: MT-003 to MT-012 (player visible, movement, walls, camera), `tests/TestMovement.cs` (6 tests)

### 1.4 Enemies

Spec: [enemies.md](objects/enemies.md), [spawning.md](systems/spawning.md)

- [ ] `scenes/enemies/Enemy.tscn` + `scripts/enemies/Enemy.cs` — CharacterBody2D, Polygon2D diamond, 3 tiers
- [ ] Tier stats: HP (30/42/54), speed (66/84/102), damage (4/5/6), XP (14/18/22)
- [ ] Tier colors: green `#6bff89`, yellow `#ffde66`, red `#ff6f6f`
- [ ] Chase AI (straight-line toward player)
- [ ] Contact damage with 0.7s hit cooldown per enemy
- [ ] Initial spawn (10 enemies from edges), periodic spawn (2.8s timer, soft cap 14)
- [ ] Respawn on kill (1.4s delay)
- [ ] Tests: MT-013 to MT-017 (spawn, colors, chase, periodic, cap), `tests/TestEnemy.cs` (15 tests), `tests/TestSpawning.cs` (5 tests)

### 1.5 Combat

Spec: [combat.md](systems/combat.md), [effects.md](objects/effects.md)

- [ ] Auto-target nearest enemy within 78px range
- [ ] Attack cooldown 0.42s, damage = 12 + (int)(level * 1.5)
- [ ] Slash effect: gold `#f5c86b` rectangle (26x4), random rotation, 120ms fade+rise
- [ ] Camera shake on player damage (+-3px, 90ms)
- [ ] Enemy death -> QueueFree(), trigger respawn timer
- [ ] XP award on kill (tier-based)
- [ ] Level-up: XP threshold = level * 90, excess carries over, MaxHp += 8, heal 18
- [ ] Tests: MT-018 to MT-027 (auto-attack, range, HP, slash, XP, level-up, damage, shake, cooldown, respawn), `tests/TestCombat.cs` (8 tests)

### 1.6 HUD

Spec: [hud.md](ui/hud.md), [ui-theme.md](assets/ui-theme.md)

- [ ] `scenes/ui/Hud.tscn` + `scripts/ui/Hud.cs` — CanvasLayer 10, top-left panel
- [ ] Panel style: dark `rgba(22,27,40,0.75)`, gold border `rgba(245,200,107,0.3)`, 10px radius
- [ ] Stats label: "HP: X | XP: Y | LVL: Z | Floor: W" — reactive via StatsChanged signal
- [ ] Tests: MT-028, MT-029 (HUD visible, stats update)

### 1.7 Death & Restart

Spec: [death.md](systems/death.md), [death-screen.md](ui/death-screen.md)

- [ ] `scenes/ui/DeathScreen.tscn` + `scripts/ui/DeathScreen.cs` — ProcessMode = Always
- [ ] Black overlay 75%, "You Died" text, restart instructions
- [ ] Restart via R key + button click
- [ ] Pause tree on death, unpause + reset on restart
- [ ] Tests: MT-030 to MT-033 (death screen, R restart, button restart, clean restart), `tests/TestDeathRestart.cs` (6 tests)

### 1.8 Main Scene

Spec: [scene-tree.md](architecture/scene-tree.md)

- [ ] `scenes/Main.tscn` + `scripts/Main.cs` — wire Dungeon, Player, HUD, DeathScreen
- [ ] Signal connections: GameState -> HUD, Player -> GameState, Enemy -> GameState
- [ ] Input map entries in project.godot (move_up/down/left/right, restart)
- [ ] Debug overlay (F3 toggle): FPS, entity count, floor gen time, memory
- [ ] Tests: MT-001 (game launch), full manual playthrough of all 33 test cases

---

## Phase 2: Core Systems

Extends prototype with designed systems. **Blocked on spec gaps** (stats formulas, class scaling, leveling finalization).

### 2.1 Leveling Redesign

Spec: [leveling.md](systems/leveling.md) (needs completion)

- [ ] Quadratic XP curve: (int)(level * level * 45)
- [ ] Floor-scaling enemy XP: baseXp * (1 + (floor - 1) * 0.5f)
- [ ] Rested XP system (5% per 8h offline, caps at 1.5 levels)
- [ ] Multi-reward level-ups: max HP, HP restore, stat points, skill points

### 2.2 Stats System

Spec: [stats.md](systems/stats.md) (needs completion)

- [ ] STR/DEX/STA/INT properties in GameState
- [ ] Stat-to-formula bindings (damage, speed, HP, magic power)
- [ ] Hybrid allocation: class bonuses + free points on level-up
- [ ] Soft diminishing returns

### 2.3 Class System

Spec: [classes.md](systems/classes.md) (needs completion)

- [ ] Class selection UI (one-time, permanent choice)
- [ ] Warrior/Ranger/Mage stat bonuses
- [ ] Per-level scaling bonuses

### 2.4 Save/Load

Spec: [save.md](systems/save.md) (needs completion)

- [ ] SaveManager autoload
- [ ] System.Text.Json (player saves) + MessagePack (floor cache)
- [ ] Auto-save triggers (level-up, floor change, death, town)
- [ ] Base64 export/import
- [ ] Save data versioning + migration

### 2.5 Full Death Flow

Spec: [death.md](systems/death.md), [death-screen.md](ui/death-screen.md)

- [ ] Multi-step UI: choose destination -> toggle mitigations -> review -> confirm
- [ ] EXP loss: floor * 0.4% (capped 50%)
- [ ] Backpack loss: floor/10 + 1 items
- [ ] Gold buyout: floor * 15 (EXP), floor * 25 (backpack)
- [ ] Sacrificial Idol consumable

---

## Phase 3: Skills & Equipment

**Blocked on spec gaps** (skill formulas, spell acquisition, item system).

### 3.1 Skill Trees

Spec: [skills.md](systems/skills.md) (needs completion)

- [ ] Skill tree UI per class
- [ ] Category -> Base Skill -> Specific Skill hierarchy
- [ ] Hybrid leveling: use-based XP + point allocation
- [ ] Infinite scaling with diminishing returns
- [ ] Passive bonus calculations

### 3.2 Equipment

Spec: [items.md](inventory/items.md) (needs new doc)

- [ ] Equipment slots (head, body, neck, rings, arms, legs, feet, hands, ammo)
- [ ] Class-restricted gear (heavy/light/robes)
- [ ] Equipment stat effects

---

## Phase 4: World

**Blocked on spec gaps** (dungeon generation, town design).

### 4.1 Procedural Dungeon

Spec: [dungeon.md](world/dungeon.md) (needs completion)

- [ ] Generation algorithm (BSP or random walk) — background threaded via Channels
- [ ] Seeded generation (System.Random with stored seeds)
- [ ] Multiple room types, corridors, doors
- [ ] Safe spots at entrances/exits
- [ ] Floor difficulty scaling

### 4.2 Floor System

Spec: [dungeon.md](world/dungeon.md) (needs completion)

- [ ] Floor caching (max 10) — MessagePack binary serialization
- [ ] Staircase descent/ascent
- [ ] Floor-specific enemy tier weighting
- [ ] Pre-generate adjacent floors on background thread

### 4.3 Town Hub

Spec: [town.md](world/town.md) (needs new doc)

- [ ] Town scene (safe zone, no enemies)
- [ ] NPCs: Item Shop, Blacksmith, Adventure Guild, Level Teleporter, Banker
- [ ] Scene transition (dungeon <-> town)

---

## Phase 5: Inventory & Items

**Blocked on spec gaps** (item system, loot tables, crafting).

### 5.1 Backpack

Spec: [backpack.md](inventory/backpack.md) (needs completion)

- [ ] 25-slot grid UI
- [ ] At risk on death (floor/10 + 1 items lost)
- [ ] Item pickup from ground

### 5.2 Bank

Spec: [bank.md](inventory/bank.md) (needs completion)

- [ ] 15-slot safe storage, town-only access
- [ ] Deposit/withdraw UI

### 5.3 Loot & Items

Spec: [items.md](inventory/items.md) (needs new doc)

- [ ] Item data model (type, tier, stats, class restrictions)
- [ ] Loot drop system
- [ ] Color-coded by level-relative gradient ([color-system](systems/color-system.md))

### 5.4 Blacksmith

Spec: needs new doc

- [ ] Recycling system (break items into materials)
- [ ] No magical drops — crafting only

---

## Phase 6: Polish & Juice

Spec: [player-engagement.md](systems/player-engagement.md), [effects.md](objects/effects.md)

- [ ] Hit flash on enemy damage
- [ ] Death particles
- [ ] Level-up glow effect
- [ ] Damage numbers
- [ ] Hitstop on impact
- [ ] Knockback on hit
- [ ] Object pooling for enemies/effects (Microsoft.Extensions.ObjectPool)
- [ ] Replace Polygon2D placeholders with pixel art sprites
- [ ] Sound effects and music
- [ ] Debug overlay (FPS, entity count, gen time, memory) — F3 toggle
- [ ] Test coverage reports (coverlet + ReportGenerator)
