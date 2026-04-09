# Project Overview

## Summary

**A Dungeon in the Middle of Nowhere** is a persistent, never-ending real-time action dungeon crawler built with Godot 4 and C# (.NET 8+). Isometric 2D perspective, desktop native (macOS primary). A "dumb hack n slash" with complex character building — simple combat, deep theorycrafting.

## Design Philosophy

- **"Dumb hack n slash, complex character building"** — simple moment-to-moment gameplay, deep metagaming
- **Session feel** — casual enough for 15-30 minute sessions, addicting enough to lose 4+ hours
- **Multiple save slots (10)** — try different classes and builds
- **Death has consequences** — penalties scale with depth, no save on quit, get to a safe spot or lose progress
- **Infinite depth** — the dungeon descends forever with zone-based difficulty scaling
- **No level cap** — infinite leveling with diminishing returns
- **PS1 controller baseline** — works with ~12 buttons, scales to modern controllers and keyboards
- **Docs first, code later** — every system is specified before implementation

## Inspiration

- **Azure Dreams** — dungeon loop, town progression feel
- **Diablo 1** — compact town hub (Tristram), atmosphere, simple satisfying loot
- **Diablo 2** — item affix system, build diversity, metagaming culture, map overlay, HP/MP orbs

## MVP Definition

The MVP is a playable single-session dungeon crawler with the core loop: **move → fight → loot → level → die → restart.**

### MVP Includes (P1 + P2)

| System | Scope |
|--------|-------|
| **Movement** | Arrow keys, isometric transform, wall collision |
| **Combat** | Face button attack, nearest-target auto-targeting, attack cooldown, slash effect |
| **Enemies** | 3 danger tiers, chase AI, contact damage, respawning |
| **Spawning** | Edge spawning, soft cap, respawn timers |
| **Leveling** | Quadratic XP curve, floor-scaling enemy XP, multi-reward level-up, milestones |
| **Stats** | 4-stat system (STR/DEX/STA/INT) with diminishing returns |
| **Classes** | 3 classes with per-level bonuses, milestone scaling, free stat allocation |
| **Death** | Multi-step death screen, EXP loss, backpack item loss, gold buyout, Sacrificial Idol |
| **Save/Load** | 10 slots, auto-save triggers, verbose JSON, export/import |
| **HUD** | HP/MP orbs (Diablo-style), XP bar, stats display, floor indicator |
| **Controls** | PS1 baseline: arrows move, WASD actions, Q/E bumpers, P select, Esc menu |

### MVP Does NOT Include

| System | Deferred To |
|--------|-------------|
| Skill trees (active abilities) | P3 |
| Equipment / affix system | P3 |
| Procedural dungeon generation | P4 |
| Floor caching / descent | P4 |
| Town hub + NPCs | P4 |
| Backpack / Bank UI | P5 |
| Loot drops / Blacksmith | P5 |
| Target cycling (L1/R1) | P2+ |
| Shortcut system (L1/R1 hold) | P3+ |
| Map overlay | P2+ |
| Visual effects (hitstop, knockback) | P6 |
| Audio / Music | P6 |
| Sprite art (uses placeholder shapes) | P6 |
| Gamepad / Mouse / Touch input | Deferred |
| Rested XP | P2 |

### MVP Success Criteria

- [ ] Player can move in 8 isometric directions with arrow keys
- [ ] Player can attack enemies by pressing face buttons (WASD)
- [ ] Enemies spawn, chase, deal contact damage, and respawn
- [ ] Killing enemies awards XP, leveling up increases stats
- [ ] Death triggers a multi-step death screen with penalty choices
- [ ] Game state saves automatically at safe moments
- [ ] 3 classes feel mechanically distinct (different stat growth)
- [ ] A 15-minute play session feels satisfying and complete
- [ ] A new player understands the controls without explanation

## Migration Context

The game began as a single-file Phaser 3 browser prototype (`index.html`, ~450 lines). The original code is preserved in `archive/phaser-prototype/` for reference. The Godot 4 rebuild keeps all game design intact while adopting:

- **Godot 4.x** as the engine (replacing Phaser 3)
- **C#** as the language (replacing vanilla JavaScript)
- **Isometric 2D** perspective (replacing top-down 2D)
- **Desktop native** platform (replacing browser)
- **Scene/node architecture** (replacing single-file monolith)

## Current State

All 26 spec tickets are complete. All game systems are fully designed and documented. The project is transitioning from docs-only to implementation phase. Next step: SETUP-02 (create C# project files).
