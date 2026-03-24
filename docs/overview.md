# Project Overview

## Summary

**A Dungeon in the Middle of Nowhere** is a persistent, never-ending real-time action dungeon crawler built with Godot 4 and GDScript. Isometric 2D perspective (Diablo 1 style), desktop native (macOS primary). Inspired by Diablo 1's atmosphere, loot chase, and town hub feel.

## Design Philosophy

- **Persistent character** — no rerolls, one character that grows across all sessions
- **Death has consequences** — penalties scale with depth but are never full permadeath
- **Infinite depth** — the dungeon descends forever with escalating difficulty
- **Isometric 2D** — classic 2:1 diamond-tile perspective, dark fantasy atmosphere
- **Desktop native** — runs as a native Godot application, no browser dependency
- **Docs first, code later** — every system is designed and documented before implementation

## Inspiration

- **Diablo 1** — dark dungeon atmosphere, town hub, real-time combat, loot-driven progression
- **Roguelike elements** — procedural generation, meaningful death penalties, risk/reward balance
- **Learning project** — a front-end web developer's first game, built with Godot 4

## Migration Context

The game began as a single-file Phaser 3 browser prototype (`index.html`, ~450 lines). The original code is preserved in `archive/phaser-prototype/` for reference. The Godot 4 rebuild keeps all game design intact while adopting:

- **Godot 4.x** as the engine (replacing Phaser 3)
- **GDScript** as the language (replacing vanilla JavaScript)
- **Isometric 2D** perspective (replacing top-down 2D)
- **Desktop native** platform (replacing browser)
- **Scene/node architecture** (replacing single-file monolith)

All game design documentation in `docs/` is engine-agnostic and carries over. Architecture docs have been updated for Godot.

## Current State

Documentation and planning phase. All game systems are being specified in detail before any code is written. The Phaser prototype established the core gameplay loop:

- Movement (WASD / arrow keys)
- Auto-targeting combat with slash effects
- Enemy spawning with 3 danger tiers + chase AI
- HUD overlay (HP, XP, level, floor)
- Basic death/restart screen
- XP and leveling system

These systems will be reimplemented in Godot with identical mechanics and values.

## Open Questions

- How far should procedural floor generation go before introducing hand-crafted content?
- Should the game support multiple save slots or remain strictly single-character?
- When should placeholder Polygon2D shapes be replaced with pixel art sprites?
- What's the right camera zoom level for the isometric perspective? (Starting at 2x)
