# A Dungeon in the Middle of Nowhere

**Repo name:** dungeon-web-game
**Current Status:** Documentation & planning phase (Godot 4 migration)
**Engine:** Godot 4.x with GDScript
**Perspective:** Isometric 2D (Diablo 1 style, 2:1 diamond tiles)
**Platform:** Desktop native (macOS primary)

## What is this?

**A Dungeon in the Middle of Nowhere** is a persistent, never-ending real-time action dungeon crawler inspired by Diablo 1's atmosphere, loot chase, and town hub feel.

You control a **single permanent character** (Warrior, Ranger, or Mage) that grows stronger across all sessions — there are no rerolls. The dungeon descends infinitely with endless monster respawns on each floor (soft cap + timers), allowing safe farming on any level or risky deep pushes for better rewards.

Death hurts (gold buyout to mitigate EXP & loot penalties, scaling with deepest floor achieved), but it's not full permadeath. On death you choose: return to town (reset progress) or respawn at the last safe spot (keep current floor layout). Safe spots exist at every floor entrance/exit.

### Prototype Features (from Phaser version)

These mechanics are documented and will be reimplemented in Godot:

- Isometric movement (WASD / arrow keys)
- Auto-targeting combat — attacks nearest enemy within range
- Infinite respawning enemies with 3 danger tiers (green/yellow/red)
- HUD overlay (HP, XP, level, floor)
- Basic death/restart screen
- XP and leveling system
- Dark fantasy UI theme

### Planned Features

- Death screen with penalty choices, gold buyout, and confirmation dialog
- Town hub with NPC interaction (Item Shop, Blacksmith, Adventure Guild, Level Teleporter)
- Depth-scaled death penalties with Sacrificial Idol mitigation
- Backpack (risky carry, 25 slots) vs Bank (safe storage, 15 slots)
- Procedural dungeon floors with seed-based generation
- Blacksmith crafting with risky upgrades
- Gamepad support
- Limitless skill leveling

## Documentation

Detailed game design and architecture documentation lives in the [`docs/`](docs/) folder:

- **[Overview](docs/overview.md)** — project vision and design philosophy
- **[Best Practices](docs/best-practices.md)** — development guidelines
- **[Architecture](docs/architecture/)** — tech stack, Godot basics, project structure, scene tree
- **[Objects](docs/objects/)** — player, enemies, tilemap, effects specifications
- **[Assets](docs/assets/)** — tile, sprite, and UI theme specifications
- **[Systems](docs/systems/)** — stats, classes, skills, combat, leveling, death, saves, movement, spawning, camera
- **[World](docs/world/)** — dungeon, town, monsters
- **[Inventory](docs/inventory/)** — backpack, bank, items
- **[UI](docs/ui/)** — controls, HUD, death screen
- **[Testing](docs/testing/)** — test strategy, manual tests, automated tests

For AI coding assistant guidelines, see [AGENTS.md](AGENTS.md).

## How to run

> **Note:** The game is currently in the documentation phase. Code implementation has not started yet.

When implementation begins:

1. Install [Godot 4.x](https://godotengine.org/download) (standard version, not .NET)
2. Clone the repo
3. Open the project folder in Godot editor (Project → Import → select the `project.godot` file)
4. Press F5 to run

### Archived Phaser prototype

The original browser prototype is preserved in `archive/phaser-prototype/`. To run it:
1. Open `archive/phaser-prototype/index.html` in a browser
2. Or serve with any static server

## Why this repo?

I'm a front-end developer (@balbonits) building my first real game. This is a personal learning project: Godot 4, GDScript, isometric 2D, procedural generation, state management — all while trying to make something addictive and fun.

The approach is docs-first: every system is designed and documented in exhaustive detail before a single line of code is written.

No fixed release date or polish promises — it's evolving slowly and thoughtfully.

## Roadmap (loose & flexible)

1. Complete all design documentation (architecture, objects, assets, systems, tests)
2. Godot project scaffold (project.godot, scenes, autoloads)
3. Isometric tile floor + player movement
4. Game state autoloads + HUD
5. Enemy spawning + chase AI
6. Combat system (auto-attack, damage, slash effects)
7. Death & restart flow
8. Polish + parity check against Phaser prototype
9. Death screen UI + penalties
10. Town hub + NPC interaction
11. Inventory systems (backpack + bank)
12. Procedural dungeon generation

## Contributing

Solo learning project for now — no formal contributions, but feel free to fork, play, and open issues with feedback or questions.

Made with curiosity in Los Angeles, 2026.
