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

The MVP is a playable dungeon crawler with the core loop: **move → fight → level → die → restart.** Built across SETUP + P1 + P2. Uses placeholder graphics (colored shapes). One static room. No items, no town, no procedural generation — those come later.

### MVP Scope: SETUP + P1 + P2 (56 sub-tickets)

**SETUP (10 tickets) — Project scaffold:**

| What | Details |
|------|---------|
| C# project | .csproj, .sln, NuGet packages, dotnet build works |
| project.godot | .NET runtime, Input Map with all actions defined |
| Makefile | C# targets (build, test, run) |
| CI | Disabled until P1 complete, then re-enabled |
| Sanity tests | xUnit hello world, GdUnit4 hello world |

**P1 (28 tickets) — Prototype parity (single static room):**

| System | What You Get |
|--------|-------------|
| **GameState** | Singleton with HP, XP, Level, Floor. TakeDamage, AwardXp, level-up logic. StatsChanged signal. |
| **EventBus** | Signal bus for cross-system communication. |
| **Tilemap** | One static dungeon room with floor tiles, walls, collision. |
| **Movement** | Arrow key input → isometric transform → MoveAndSlide. 8-direction, 190 px/s. Camera follow with 2x zoom. |
| **Enemies** | 3 danger tiers (green/yellow/red). Chase AI. Contact damage (700ms cooldown). TakeDamage/Die. |
| **Spawning** | 10 initial enemies. Periodic spawn (2.8s, soft cap 14). Respawn on death (1.4s delay). Edge positions. |
| **Combat** | Face button (WASD) attack. Nearest-enemy targeting. 420ms cooldown. Placeholder damage formula. Slash effect. Camera shake on hit. XP on kill. |
| **HUD** | CanvasLayer overlay. Title, controls hint, stats line (HP/XP/LVL/Floor). Updates via signal. |
| **Death screen** | Shows on HP=0. Restart via Esc. Resets GameState and reloads scene. |
| **Main scene** | Wires everything: dungeon + player + enemies + HUD + death screen. Full 33-case manual test. |
| **Debug tools** | FPS, entity count, input visualizer, collision viewer, state inspector. F3 toggle. Hideable for captures. |

**P2 (18 tickets) — Core systems (replaces placeholders with real math):**

| System | What You Get |
|--------|-------------|
| **Leveling** | Quadratic XP curve (`L^2 * 45`). Floor-scaling enemy XP. Multi-reward level-up (HP + stat points + skill points). Milestone bonuses every 10th level. Rested XP (offline accumulation, doubles kill XP). |
| **Stats** | STR/DEX/STA/INT with diminishing returns (K=100). STR → melee damage. DEX → attack speed + dodge. STA → HP + regen. INT → mana + regen + efficiency. Replaces placeholder combat formula. |
| **Classes** | Warrior/Ranger/Mage. Creation bonuses. Per-level auto stat bonuses. Milestone scaling every 25 levels. Free stat point allocation. Character creation screen. |
| **Save/Load** | SaveManager autoload. 10-slot file structure. Auto-save triggers (level-up, floor, town, death). Validation + version migration. Base64 export/import. Save slot selection UI. |
| **Death (full)** | Multi-step flow (destination → mitigations → summary → confirm). EXP loss formula. Backpack item loss. Gold buyout. Sacrificial Idol. Respawn destinations (town vs safe spot). |

### MVP Does NOT Include

| System | Deferred To | Why |
|--------|-------------|-----|
| Skill trees (active abilities) | P3 | Needs class system + UI |
| Equipment / affix system | P3 | Needs stat system + item model |
| Procedural dungeon generation | P4 | MVP uses single static room |
| Floor caching / descent | P4 | Needs proc gen |
| Town hub + NPCs | P4 | Needs main scene wiring + save |
| Backpack / Bank UI | P5 | Needs death flow + town |
| Loot drops / Blacksmith | P5 | Needs equipment system |
| Target cycling (L1/R1) | P2+ | MVP uses nearest-enemy auto-target |
| Shortcut system (L1/R1 hold) | P3+ | Needs skills/items to assign |
| Map overlay | P4+ | Needs procedural floors |
| Visual effects (hitstop, knockback) | P6 | Polish, not core |
| Audio / Music | P6 | Polish, not core |
| Sprite art | P6 | Placeholder shapes are functional |
| Gamepad / Mouse / Touch | Deferred | Keyboard-first, add later via Input Map |

### MVP Success Criteria

- [ ] Player can move in 8 isometric directions with arrow keys
- [ ] Player can attack enemies by pressing face buttons (WASD)
- [ ] Enemies spawn from edges, chase player, deal contact damage, respawn after death
- [ ] Killing enemies awards XP; leveling up grants HP + stat/skill points
- [ ] 4-stat system (STR/DEX/STA/INT) affects combat with diminishing returns
- [ ] 3 classes feel mechanically distinct (different auto-stat growth per level)
- [ ] Death screen shows with penalty choices (EXP loss, gold buyout)
- [ ] Game auto-saves at level-up, floor transition, and death — no save on quit
- [ ] 10 save slots with character creation (pick name + class)
- [ ] Debug tools toggle with F3 (FPS, input visualizer, collision shapes, state inspector)
- [ ] All debug tools hideable for clean screenshot/recording captures
- [ ] A 15-minute play session feels satisfying and complete
- [ ] A new player understands the controls without explanation

## Migration Context

The game began as a single-file Phaser 3 browser prototype (~450 lines), then went through a C# implementation (480 unit tests passing), and was reset in Session 8 when the rendering layer never worked visually. The Godot 4 rebuild keeps all game design intact while adopting:

- **Godot 4.x** as the engine (replacing Phaser 3)
- **C#** as the language (replacing vanilla JavaScript)
- **Isometric 2D** perspective (replacing top-down 2D)
- **Desktop native** platform (replacing browser)
- **Scene/node architecture** (replacing single-file monolith)

## Current State

All 26 spec tickets are complete. All game systems are fully designed and documented. The project is in a fresh-start rebuild phase — all code was deleted in Session 8. Visual-first reimplementation begins from VIS-01 (render one floor tile). See `docs/dev-tracker.md` for the full ticket list.
