# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added

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
- `docs/architecture/ai-workflow.md` — replaced Open Questions with Automation section
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
