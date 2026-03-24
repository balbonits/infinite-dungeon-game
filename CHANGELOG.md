# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

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
