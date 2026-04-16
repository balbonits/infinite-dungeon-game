# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Session 15 — Skills & Abilities Code Implementation (2026-04-16)

#### Added
- New data layer: `MasteryDef`, `AbilityDef`, `MasteryState`, `AbilityState`, `SkillAbilityDatabase` (130 registrations), `ProgressionTracker`
- `GameWindow.cs` — unified base class for all game windows
- `GameTabPanel.cs` — reusable Q/E tab switching system
- `AbilitiesDialog.cs` — class-specific abilities tab (Warrior Arts / Ranger Crafts / Arcane Spells)
- AbilitiesDialog node in `main.tscn`, Abilities button in `pause_menu.tscn`
- 72 new unit tests for data layer

#### Changed
- `SkillTreeDialog.cs` — rewritten on GameWindow + GameTabPanel, shows masteries only
- `GameState.cs` — awards SP (2/lvl) + AP (3/lvl) on level-up
- `SaveData.cs` + `SaveSystem.cs` — new fields for masteries, abilities, use counts, category AP
- `Player.cs` — movement blocked when any modal window is open
- `ShopWindow.cs` — Buy/Sell merged into colored buttons, fixed panel resizing
- `UiTheme.cs` — added `CreateTabStyle`, made `CreateColoredButtonStyle` public
- All 13 ScrollContainers — `FollowFocus = true` for keyboard navigation

#### Removed
- Old skill system: `SkillDef.cs`, `SkillDatabase.cs`, `SkillState.cs`, `SkillTracker.cs`
- 671 lines of duplicate window boilerplate (all 14 windows migrated to GameWindow base)

#### Documentation
- `docs/reference/godot4-engine-reference.md` — comprehensive catalog of built-in engine systems and usage status

### Session 14 — Skills & Abilities System Complete (2026-04-15)

#### Added
- `docs/world/class-lore.md` — Class backstories and magic philosophy for Warrior, Ranger, Mage
- `docs/systems/point-economy.md` — SP/AP rates, sources, and total budget at key levels
- `docs/systems/synergy-bonuses.md` — Mastery threshold bonuses (Lv.5/10/25/50/100) with per-mastery procs
- `docs/systems/ability-affinity.md` — Cosmetic use-based milestones (100/500/1,000/5,000 uses)
- `assets/icons/abilities_icons.png` — Combined 512x1024 sprite sheet with 131 icons (all 3 classes + innate)
- `assets/icons/abilities_icons.json` — Icon atlas index for combined sheet

#### Changed
- `docs/systems/skills.md` — Complete rewrite: dual system (Skills + Abilities), all 3 class trees, SP/AP split
- `docs/systems/magic.md` — Mage: Arcane→Elemental, added Aether, Conduit→Attunement; Armor innate added
- `docs/systems/classes.md` — Updated mastery structure, SP/AP terminology
- `docs/ui/pause-menu-tabs.md` — 7→8 tabs, new Abilities tab (class-specific)
- `docs/systems/combat.md` — Skill Hotbar→Ability Hotbar, dual XP tracking
- `docs/systems/leveling.md` — SP/AP references replace old "skill points"
- `docs/ui/hud.md` — Ability cooldown overlays, status effect icons
- `docs/ui/controls.md` — Abilities terminology for hotbar
- `docs/flows/combat.md` — Ability activation flow
- `docs/flows/progression.md` — SP/AP allocation flows
- `docs/dev-tracker.md` — SPEC-13 tickets added
- `AGENTS.md` — New doc references
- `scripts/generate_icons.py` — Rewritten for combined abilities sheet
- Ranger ability renames: Steady Shot→Bead, Burst Fire→Spray, Guard→Hunker

### Session 13 — Skill & Spell Icon Sprite Sheets (2026-04-14)

#### Added
- `assets/icons/skills_icons.png` — 512x512 sprite sheet with 73 skill icons (Warrior + Ranger + Innate, 32x32 each)
- `assets/icons/spells_icons.png` — 512x512 sprite sheet with 45 spell icons (Mage Arcane + Conduit, 32x32 each)
- `assets/icons/skills_icons.json` — JSON atlas index for skill icons (name → x, y, w, h)
- `assets/icons/spells_icons.json` — JSON atlas index for spell icons (name → x, y, w, h)
- `scripts/generate_icons.py` — Pillow-based icon sprite sheet generator (re-runnable)

### Session 12 — Fix & Expand Test Suite + Full-Run Integration Test (2026-04-12)

#### Added (Flow Docs + Testing Infrastructure)
- `docs/flows/` — 14 step-by-step flow docs covering every player interaction
- GodotTestDriver v3.1.66 integrated (Chickensoft allowed for testing only)
- `scripts/testing/DebugTelemetry.cs` — full audit trail (#if DEBUG): input, signals, state, scenes
- FullRunSandbox rewritten: 3-class walkthrough (Warrior, Ranger, Mage), death flow, achievement checks
- `docs/conventions/versioning.md` — SemVer + git tags strategy

#### Added (AutoPilot Player Emulation Library)
- `scripts/testing/AutoPilot.cs` — core player emulation node (step runner, logging, assertions)
- `scripts/testing/AutoPilotActions.cs` — input simulation: press/hold/release, movement, UI clicks, waiting
- `scripts/testing/AutoPilotAssertions.cs` — game state verification: HP, level, floor, inventory, enemies
- `docs/testing/autopilot.md` — spec for the AutoPilot library
- FullRunSandbox rewritten: now launches the real game and plays through it via AutoPilot
- Reusable for any sandbox or debug scenario — not tied to testing infrastructure

#### Added (Full-Run Integration Test)
- `docs/testing/full-run-test.md` — spec for the full-run integration test
- `tests/unit/FullRunTests.cs` — 13 tests: 10 phase tests + 3 class-parameterized init tests + 1 full session chain
- `scripts/sandbox/FullRunSandbox.cs` + `scenes/sandbox/FullRunSandbox.tscn` — Godot sandbox layer with all 10 phases
- `make sandbox-headless SCENE=full-run` — one-command full game smoke test
- Registered in SandboxLauncher under new "Integration" category

#### Fixed
- Unit and integration tests now compile and pass (added missing `using Xunit;` to all 5 test files)
- Excluded `SaveSystem.cs` from test project compilation (references Godot-specific `Autoloads` class)
- Added `RollForward` to test .csproj files for .NET 10 runtime compatibility

#### Added
- 199 new unit tests across 10 test files covering all untested pure-C# logic systems:
  - DungeonPacts (18 tests): rank management, heat calculation, reward scaling, pact effects, serialization
  - ZoneSaturation (19 tests): kill tracking, time decay, stat multipliers, reward bonuses, serialization
  - SkillBar (19 tests): slot assignment, cooldowns, activation, serialization
  - SkillState (14 tests): XP curves, level-up, skill points, passive bonuses, diminishing returns
  - AchievementSystem (15 tests): counters, evaluation, progress tracking, save/load
  - Crafting + AffixDatabase (16 tests): affix eligibility, application, recycling, display names
  - QuestSystem (10 tests): quest generation, kill/clear/depth progress, completion, save/load
  - DepthGearTiers (15 tests): floor gates, stat ranges, affix slots, quality rolls, tier lookup
  - LootTable (4 tests): gold drop formulas, level scaling
  - MagiculeAttunement (21 tests): floor clearing, node pathing, keystones, stat bonuses, serialization

### Session 11 — Endgame + Mana + Skills + UI Overhaul (2026-04-11)

#### Added
- Endgame systems: Zone Saturation, Dungeon Pacts (10 pacts), Dungeon Intelligence (adaptive AI), Magicule Attunement (40-node tree), Depth Gear Tiers (6 quality tiers)
- Mana system: Mana/MaxMana on GameState, class pools (M:200/R:100/W:60), INT regen, save/load
- Skill execution: all 80+ skills castable from hotbar with ManaCost/Cooldown/AttackConfig
- Skill bar HUD: 4 slots with shoulder+face combos (Q+W, Q+S, E+W, E+S), dynamic key labels
- HP/MP orbs: Diablo-style glass sphere sprites (PixelLab), fill/drain with ratio
- XP progress bar below skill bar with level-up gold glow and XP loss red flash
- Backpack window: 5-column slot grid (64x64), action menu (Use/Drop), from pause menu
- Settings panel: 4 tabbed categories (Gameplay/Display/Audio/Controls), persisted to JSON
- Tutorial: 4 tabbed sections (Movement/Combat/Menus/Town), static reference
- Debug console (F4): god mode, XP/gold/level cheats, teleport, kill all, perf metrics
- Character card: reusable component on title screen showing saved game sprite/stats
- Action menu: FF-style popup for context actions on items and skills
- 8 projectile sprites: arrow, magic bolt, fireball, frost bolt, lightning, stone spike, energy blast, shadow bolt
- Reusable UI components: GameWindow, TabBar, ScrollList, ContentSection
- WindowStack: central input routing, eliminates bleed-through permanently
- Back to Main Menu button on class select screen
- Camera shake on damage setting (off by default)

#### Fixed
- All UI panels block ALL input when open (not just movement)
- Sub-dialogs restore PauseMenu visibility and focus on close
- NpcPanel closeable with D/Escape (was missing)
- Mage auto-attack: staff melee is free, magic bolt requires mana via hotbar
- Monster spawn: guaranteed 10 per floor, floor wipe requires 10+ kills
- All scene loads use transition screen (town, dungeon, floor descent)
- Keyboard nav auto-scrolls ScrollContainer to focused button
- Skill targeting uses skill's range, not auto-attack range
- ClassSelect calls Reset() to initialize mana on new game
- GlobalTheme buttons use blue Action color (not gold Accent)
- Confirm button uses proper disabled style (not transparent modulate)
- Tutorial text autowraps within panel (no horizontal clipping)
- Section dividers at bottom (no double dividers)
- Removed fabricated difficulty setting (not in specs)

### Phase 1 Complete — All Systems Built (2026-04-11)

#### Added
- Skill system: 80+ skills across 3 classes, use-based XP + skill point allocation, SkillTreeDialog UI
- Bank system: 50 start slots, deposit/withdraw UI, expansion purchasing at 500*N^2
- Blacksmith crafting: deterministic affix system (28 affixes, tiers 1-4), equipment recycling
- Quest system: radiant quests (Kill/ClearFloor/DepthPush), 3 active quests, QuestPanel UI
- Achievement system (Dungeon Ledger): 30 achievements, counter-based tracking, DungeonLedger UI
- Teleporter NPC floor-select UI with zone labels
- Procedural floor generation: BSP rooms + Drunkard's Walk corridors + Cellular Automata smoothing
- Zone-based monster families: zone-exclusive species spawning per 10-floor block
- IDamageable interface for type-safe combat dispatch

#### Fixed
- Unsafe Random instance in DeathPenalty.cs → Random.Shared
- Silent save deserialization failures now log errors
- Save data validation prevents corrupted saves from creating impossible game states
- Duplicated stairs creation logic extracted into shared method

### Session 10 — Full Prototype Build (2026-04-11)

#### Added
- Playable dungeon crawler with full game loop (move, fight, level, die, restart)
- PixelLab-generated art: warrior, skeleton, goblin, 5 town NPCs, dungeon tiles, town tiles, stairs
- GameState + EventBus autoloads with reactive signals
- Level-based enemy system with full color gradient (grey→blue→cyan→green→yellow→gold→orange→red)
- Floor scaling formula: floor N = level N monsters, spawn range [N-1, N+2]
- Proc-gen floors with random room sizes and 4 floor tile variations
- Stairs up/down with physical collision + area trigger + sprite art
- Screen transition system (fade to black, message, fade in)
- Floating combat text (damage numbers, XP gains, level up)
- Flash effect system (damage, poison, curse, boost, shield, freeze, heal, crazed)
- Pause menu (Esc toggle, Resume/Quit)
- Death screen with Restart (R) and Quit (Esc) options
- HUD overlay (HP, XP, LVL, Floor) with reactive updates
- Spawn safety: 150px safe radius + 1.5s invincibility grace period
- Entity-to-entity collision (player↔enemies, enemy↔enemy)
- Directional sprites (8-way rotation for player and enemies)
- UiTheme shared palette + factory methods (DRY)
- GameSettings with ShowCombatNumbers toggle
- @art-lead agent for PixelLab art generation
- Asset READMEs for navigability

#### Fixed
- Signal leak on scene reload (autoload += without -= in _ExitTree)
- Diamond wall collision causing wiggle (switched to rectangular)
- Isometric movement confusion (switched to screen-space: up=up)
- Camera shake causing motion sickness (switched to red sprite flash)
- Esc not working on death screen (process_mode=ALWAYS)
- Enemies spawning outside room bounds (margin + despawn safeguard)
- "LEVEL UP" showing on floor descent instead of actual level ups

#### Docs
- docs/systems/floor-scaling.md — monster level formula
- docs/systems/spawn-safety.md — spawn safety rules
- docs/systems/accessibility.md — font, color, readability settings spec
- docs/dev-journal.md Session 10 entry

### Reset — Fresh Start (2026-04-10)

**All code, scenes, and tests deleted.** The pure C# game logic (480 tests passing) was correct, but the Godot rendering layer never worked visually. Starting over with visual-first development.

#### Deleted
- All Godot scene files (6 game scenes, 24+ test scenes)
- All C# scripts (autoloads, dungeon, player, ui, game systems, entity framework)
- All tests (480 unit tests, E2E test infrastructure, shell scripts)
- `archive/phaser-prototype/` (original Phaser 3 prototype)
- `addons/gut/` (GDScript testing addon)

#### Retained
- All documentation (80+ files across 11 directories)
- All assets (819+ sprites, tiles, fonts, icons)
- Config files (project.godot, DungeonGame.csproj, Makefile, AGENTS.md, CLAUDE.md)
- Input map (12 actions in project.godot)

#### Why
See `docs/dev-journal.md` Session 8 for the full post-mortem.

#### Docs Cleanup (2026-04-10)
- Updated all architecture docs to reframe as design blueprints (no code exists to describe)
- Rewrote `docs/dev-tracker.md` with new ticket structure (VIS-*, PROTO-*, CFG-*)
- Stripped Makefile of 50+ stale targets referencing deleted scenes/scripts
- Fixed `project.godot` (removed stale main scene and autoload references)
- Updated `AGENTS.md` current state, project structure, priorities
- Updated pre-commit hook from GDScript to C# formatting

### Previous [Unreleased]

### Added

- `docs/dev-tracker.md` — single-file development progress tracker with phased checklist
- `docs/architecture/setup-guide.md` — .NET SDK, Godot .NET edition, VS Code extension setup guide

### Changed

- **Language migration: GDScript → C# (.NET 8+)** — all docs, conventions, and tooling updated for C#
- `AGENTS.md` — overhauled: C# conventions (§4), tech stack with NuGet deps (§5), PascalCase naming (§6), new project structure (§7), dotnet tooling (§10)
- `CLAUDE.md` — added C# migration mode, setup guide reference
- `docs/architecture/tech-stack.md` — rewritten for C#/.NET 8+, added NuGet deps, platform support matrix, perf comparison
- `docs/architecture/project-structure.md` — updated directory tree for .csproj/.sln, PascalCase files, C# naming conventions
- `docs/dev-tracker.md` — updated all file extensions (.gd → .cs), test refs (GUT → GdUnit4), added Phase 0.2 C# migration checklist
- Stack additions: MessagePack-CSharp (binary floor cache), Microsoft.Extensions.ObjectPool (entity pooling), System.Threading.Channels (async generation), GdUnit4 + xUnit (testing)
- Autoloads open questions resolved: while-loop multi-level-up, player_damaged signal added, scripts over scenes, floor_number stays on GameState, save slot-aware interface

### Removed

- GDScript conventions, gdlint/gdformat references, GUT test framework references (replaced by C# equivalents)

### Previous Unreleased

- `docs/systems/skills.md` — full skill trees for all 3 classes (Warrior, Ranger, Mage) with hierarchical categories, hybrid leveling (use-based + point-based), infinite scaling
- `docs/systems/color-system.md` — unified cool→warm color gradient system for enemies, items, and zones (level-relative coloring)
- `docs/systems/player-engagement.md` — feedback loops, session pacing, juice/feel, retention hooks
- `docs/architecture/analytics.md` — opt-in telemetry, bug reporting, feedback system (offline-first)

### Changed

- `docs/systems/classes.md` — renamed Archer → Marksman → Ranger; added class-specific skill category names
- `docs/systems/skills.md` — added equipment slots, refined hybrid skill leveling (use-based + point-based)
- `docs/systems/leveling.md` — redesigned XP curve (linear-polynomial hybrid), added rested XP bonus, floor-scaling enemy XP, no level cap
- `docs/systems/combat.md` — updated class references (Archer → Ranger)
- `docs/inventory/items.md` — added equipment slot definitions and item tier coloring
- `docs/assets/sprite-specs.md` — added asset generation strategy and color gradient integration
- `docs/assets/ui-theme.md` — added color gradient palette entries
- `docs/world/monsters.md` — updated enemy color references to use unified color system
- `AGENTS.md` — added Skills, Color System, Player Engagement, Leveling to Game Design Quick Reference; updated Documentation Map with new docs

## [0.3.0] - 2026-03-23

### Added

- `project.godot` — Godot 4.6 project config (1920x1080, GL Compatibility, GUT plugin)
- `Makefile` — 11 automation targets for AI-driven terminal development (`make help`)
- `.github/workflows/ci.yml` — GitHub Actions CI: lint + test on push/PR to `main`
- `.githooks/pre-commit` — GDScript linting and formatting check on commit
- `.gitignore` — Godot, macOS, Python, IDE ignores
- `.editorconfig` — tabs for GDScript, spaces for YAML/Python
- `archive/phaser-prototype/.gdignore` — prevents Godot from importing archived Phaser files
- `addons/gut/` — GUT v9.6.0 test framework (vendored)
- `tests/test_project_setup.gd` — 4 sanity tests verifying project config
- `scripts/generate_tiles.py` — tile asset generator (from tile-specs.md)
- `assets/tiles/floor.png` and `wall.png` — generated isometric tile assets

### Changed

- `AGENTS.md` — added section 10 (Development Automation), updated project structure tree
- `docs/conventions/ai-workflow.md` — replaced Open Questions with Automation section (moved from architecture/)
- `docs/systems/stats.md` — closed 4 open questions (hybrid allocation, soft diminishing returns, no caps, fixed backpack)
- `docs/systems/classes.md` — closed 4 open questions (scaling bonuses, design-now-build-later skills, class-locked skills, class-locked gear)
- `docs/systems/leveling.md` — closed 2 open questions (linear-polynomial hybrid XP, no level cap)
- `docs/systems/death.md` — closed 2 open questions (locked-in formulas, MVP scope)
- `docs/overview.md` — closed 2 open questions (all procedural floors, multiple save slots)
- `docs/ui/death-screen.md` — closed 2 open questions (no inventory shown, instant death screen)
- `docs/assets/tile-specs.md` — added Licensing section (CC0/CC-BY 3.0/CC-BY 4.0)
- `docs/assets/sprite-specs.md` — added Licensing section
- `assets/ATTRIBUTION.md` — created attribution tracking template
- `README.md` — updated title formatting

## [0.2.0] - 2026-03-23

### Changed

- **Engine migration:** Phaser 3 (browser) → Godot 4 (desktop native)
- **Perspective:** Top-down 2D → Isometric 2D (2:1 diamond tiles, 64×32)
- **Language:** Vanilla JavaScript → GDScript
- **Architecture:** Single-file monolith → Scene/node project structure
- **Platform:** Browser → Desktop native (macOS primary)
- **Persistence:** localStorage → FileAccess + user:// + JSON
- **State management:** Global JS object → Autoload singletons (GameState, EventBus)
- **UI:** HTML/CSS overlay → Godot Control nodes in CanvasLayer

### Added

- Comprehensive game design documentation in `docs/` (25+ design documents)
- Architecture docs: tech stack, Godot basics, project structure, scene tree, autoloads, signals
- Object specs: player, enemies, tilemap, effects — every property and method documented
- Asset specs: tile dimensions, sprite shapes, UI color palette
- System docs: isometric movement, enemy spawning, camera behavior
- Testing docs: test strategy, 33 manual test cases, automated test plan (GUT framework)
- `archive/phaser-prototype/` — preserved original Phaser code for reference

### Removed

- `package.json`, `bunfig.toml`, `node_modules/` — Bun/Node dependencies no longer needed
- Phaser-specific architecture docs moved to archive (phaser-basics, single-file, code-map)

## [0.1.0] - 2026-03-19

### Added

- Phaser 3 game engine setup via CDN (v3.90.0) with arcade physics
- Responsive canvas layout using Phaser.Scale.FIT + CENTER_BOTH
- Player character rendered as a colored circle with physics body
- Movement via WASD, arrow keys, and pointer/touch drag
- Auto-targeting combat: attacks nearest enemy within range on cooldown
- Slash visual effect using tweened rectangles
- Enemy spawning from screen edges with three danger tiers (green/yellow/red)
- Enemy AI: chase player using physics.moveToObject
- Enemy respawn timer (2.8s loop, soft cap at 14 active enemies)
- Player damage on enemy overlap with hit cooldown and camera shake
- XP gain on enemy defeat, scaling by danger tier
- Level-up system: XP threshold increases per level, HP boost on level-up
- HUD overlay displaying HP, XP, level, and floor number
- Death screen with restart via R key or tap
- Mobile-responsive layout with safe-area inset support
- Mobile touch note displayed on small screens
- Dark fantasy UI theme with CSS custom properties
- Bun dev server configuration (port 8000)
- Project documentation: README.md, AGENTS.md
