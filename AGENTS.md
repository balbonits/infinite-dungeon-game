# AGENTS.md — AI Coding Assistant Reference

This is the single reference file for any AI coding tool helping with **A Dungeon in the Middle of Nowhere** (repo: `dungeon-web-game`).

For detailed game design, see the [`docs/`](docs/) folder.

---

## Core Rules

### 1. Scope Discipline (READ THIS FIRST)

**This project is micromanaged. AI must stay strictly within the scope of the current task.**

- Do EXACTLY what is asked. Nothing more, nothing less.
- Do NOT add features, refactors, or "improvements" beyond what was requested.
- Do NOT invent requirements that aren't in the spec docs.
- Do NOT add code, patterns, or placeholders "just in case" or "for future use."
- Do NOT make assumptions about what the user might want — ASK instead.
- If the task is ambiguous or seems incomplete, STOP and ask for clarification.
- If you notice something that seems like it should also be done, mention it — don't do it.

Violations of scope discipline are the #1 failure mode for AI-assisted development. Hallucinations from assumptions and scope expansion cause bugs, wrong patterns, and wasted effort. When in doubt, do less.

**Three-tier boundary system:**

| Tier | Action | Examples |
|------|--------|----------|
| Always do | Safe actions, no approval needed | Read spec docs before code, run tests after changes, use static typing, follow naming conventions |
| Ask first | High-impact decisions need user approval | Adding new files/scenes not in spec, changing autoloads, modifying project.godot, adding dependencies, any refactoring beyond current task |
| Never do | Hard stops, no exceptions | Add unspecified features, make assumptions about intent, skip tests, change code outside task scope, add TODO/future-proofing comments, suppress errors, modify archived files |

### 2. AI Workflow Protocol

Follow this cycle for every task. Do not skip steps.

1. **Read** — Read the relevant spec doc in `docs/` before touching anything.
2. **Plan** — State what you will do and what files you will touch. Keep it minimal.
3. **Test first** — Write or reference the test cases that define "done" (from `docs/testing/manual-tests.md` or `docs/testing/automated-tests.md`).
4. **Implement** — Write the minimum code that passes the tests and satisfies the spec.
5. **Verify** — Run the tests. If you can't run them, describe exact manual verification steps.
6. **Stop** — Do not continue to the next task. Wait for user direction.

One task = one focused change = one commit. Prefer small, reviewable diffs over large batches.

See [docs/architecture/ai-workflow.md](docs/architecture/ai-workflow.md) for the full workflow reference.

### 3. Development Principles

- **KISS** — Keep It Simple. Use the simplest approach that satisfies the spec.
- **DRY** — Don't Repeat Yourself. Extract shared logic when repetition appears; don't prematurely abstract.
- **No scope creep** — Only implement what the current spec describes. Nothing extra.
- **Spec-driven** — Every system is fully documented in `docs/` before code is written. Read the relevant doc before modifying any code.
- **Test-driven** — Tests are written before implementation. Manual test cases first, then GUT automated tests, then code.
- **AI-coded** — All code is written by AI assistants. The user directs and reviews.
- **Free assets only** — No paid assets. Polygon2D placeholders, then free/open-source packs.

### 4. GDScript Conventions

Follow the [official GDScript style guide](https://docs.godotengine.org/en/stable/tutorials/scripting/gdscript/gdscript_styleguide.html). Key rules:

**Script ordering** (top to bottom):
1. `class_name` / `extends`
2. `## Docstring`
3. Signals
4. Enums
5. Constants
6. `@export` variables
7. Public variables
8. Private variables (`_` prefix)
9. `@onready` variables
10. Built-in methods (`_ready`, `_process`, `_physics_process`)
11. Public methods
12. Private methods (`_` prefix)

**Scene architecture:**
- **"Call down, signal up"** — parents call methods on children; children emit signals to parents. Never reach up the tree.
- **One responsibility per node** — if a script exceeds ~300 lines, split behavior into child nodes.
- **Composition over inheritance** — build entities by combining specialized child scenes, not deep inheritance chains.
- **Use static typing** — `var speed: float = 190.0` not `var speed = 190`. Include return types on functions.
- **Prefer `@onready`** — `@onready var sprite := $Sprite` over `get_node()` in methods.
- **Autoloads sparingly** — only for truly global state (GameState) and cross-system signals (EventBus).

### 5. Tech Stack

| Layer | Technology | Notes |
|-------|-----------|-------|
| Engine | Godot 4.x | Open-source, scene/node architecture |
| Language | GDScript | Python-like, tightly integrated with editor |
| Renderer | GL Compatibility | Broadest hardware support |
| Physics | Built-in 2D | CharacterBody2D + Area2D |
| Perspective | Isometric 2D | 2:1 diamond tiles (64×32), TileMapLayer |
| UI | Control nodes | Built-in UI, Theme resources |
| Persistence | FileAccess + JSON | user:// directory |
| Platform | Desktop native | macOS primary |

See [docs/architecture/tech-stack.md](docs/architecture/tech-stack.md) for details.

### 6. Naming Conventions

- **Variables/functions:** `snake_case` — `move_speed`, `handle_movement`, `attack_timer`
- **Constants:** `UPPER_SNAKE_CASE` — `MOVE_SPEED`, `ATTACK_COOLDOWN`, `ENEMY_SOFT_CAP`
- **Node names:** `PascalCase` — `CollisionShape2D`, `AttackRange`, `HitCooldownTimer`
- **Signals:** `snake_case`, past tense — `enemy_defeated`, `stats_changed`, `player_died`
- **Groups:** `snake_case` — `"player"`, `"enemies"`
- **Files:** `snake_case` — `game_state.gd`, `death_screen.tscn`, `dungeon_tileset.tres`
- **No abbreviations** — `player` not `plr`, `level` not `lvl`

### 7. Project Structure

```text
dungeon-web-game/
├── project.godot                  — Godot 4 project config
├── Makefile                       — AI-drivable automation (make help)
├── .gitignore / .gdignore         — Git + editor ignores
├── .editorconfig                  — Editor formatting rules
├── .githooks/pre-commit           — GDScript lint on commit
├── .github/workflows/ci.yml      — GitHub Actions CI (lint + test)
├── AGENTS.md / CLAUDE.md          — AI assistant guidelines
├── README.md / CHANGELOG.md       — Project docs
├── archive/phaser-prototype/      — Original Phaser 3 code (preserved)
├── docs/                          — Game design documentation
│   ├── overview.md / best-practices.md
│   ├── architecture/              — Tech stack, Godot basics, project structure, scene tree, autoloads, signals
│   ├── objects/                   — Player, enemies, tilemap, effects specs
│   ├── assets/                    — Tile, sprite, UI theme specs
│   ├── systems/                   — Stats, classes, combat, leveling, death, save, movement, spawning, camera
│   ├── world/                     — Dungeon, town, monsters
│   ├── inventory/                 — Backpack, bank, items
│   ├── ui/                        — Controls, HUD, death screen
│   └── testing/                   — Test strategy, manual tests, automated tests
├── scenes/                        — Godot scenes + scripts
│   ├── main.tscn + main.gd
│   ├── dungeon/ player/ enemies/ ui/
├── scripts/
│   ├── autoloads/                 — GameState, EventBus singletons
│   └── generate_tiles.py          — Tile asset generator
├── tests/                         — GUT automated tests
├── addons/gut/                    — GUT test framework (v9.x)
├── assets/                        — Tiles, sprites, UI (binary assets)
└── resources/                     — TileSet, Theme (.tres resources)
```

See [docs/architecture/project-structure.md](docs/architecture/project-structure.md) for the full breakdown.

### 8. How to Propose Changes

- Read the relevant design doc in `docs/` before touching code
- Show the modified function or section with clear context
- Explain **why** each change was made
- Include manual test steps (from `docs/testing/manual-tests.md` or new ones)
- One feature or system per response — keep scope small
- If the change affects game mechanics, update the design doc too

### 9. Visual Style

- **Placeholder shapes:** Polygon2D diamonds for characters/enemies (no sprites yet)
- **Colors:**
  - Player: `#8ed6ff` (light blue)
  - Enemy tiers: `#6bff89` (green), `#ffde66` (yellow), `#ff6f6f` (red)
  - Accent/sword: `#f5c86b` (gold)
  - UI panel: `rgba(22, 27, 40, 0.75)` with `rgba(245, 200, 107, 0.3)` border
- **Tiles:** 64×32 isometric diamonds — floor dark blue, wall outlined
- See [docs/assets/ui-theme.md](docs/assets/ui-theme.md) for the full color palette

### 10. Development Automation

All development can be driven from the terminal. Run `make help` for available targets.

**Setup (first time):**
```bash
make setup          # Configure git hooks + verify tools
```

**Daily workflow:**
```bash
make lint           # Lint GDScript (gdlint)
make format         # Check formatting (gdformat --check)
make format-fix     # Auto-format GDScript
make test           # Run GUT tests headlessly
make check          # lint + format + test (all three)
make run            # Launch the game
make tiles          # Generate tile assets
```

**Tools required:**
| Tool | Install | Purpose |
|------|---------|---------|
| Godot 4.x | `brew install --cask godot` + symlink to PATH | Engine, headless test runner |
| gdtoolkit | `pipx install gdtoolkit` | GDScript linting + formatting |
| GUT | Bundled in `addons/gut/` | Godot unit test framework |

**CI:** GitHub Actions (`.github/workflows/ci.yml`) runs lint + test on every push/PR to `main`.

**Pre-commit hook:** `.githooks/pre-commit` runs gdlint + gdformat on staged `.gd` files. Activated by `make setup`.

See [docs/architecture/ai-workflow.md](docs/architecture/ai-workflow.md) for the full automation reference.

---

## Game Design Quick Reference

Detailed design docs live in `docs/`. Here's a summary with links.

### Character & Stats

- **4 stats:** STR (physical power), DEX (agility), STA (health), INT (magic) — [docs/systems/stats.md](docs/systems/stats.md)
- **3 classes:** Warrior (STR/STA), Marksman (DEX), Mage (INT) — [docs/systems/classes.md](docs/systems/classes.md)
- **Persistent character** — no rerolls, one character forever

### Combat

Auto-targeting, cooldown-based, scales with level and stats — [docs/systems/combat.md](docs/systems/combat.md)

### Death Penalties

Scale by deepest floor achieved. Gold buyout mitigates EXP and backpack loss. Sacrificial Idol negates backpack loss — [docs/systems/death.md](docs/systems/death.md)

### Dungeon

Infinite descent, procedural floors, 10-floor cache, safe spots at entrances/exits — [docs/world/dungeon.md](docs/world/dungeon.md)

### Town Hub

Safe scene with NPCs: Item Shop, Blacksmith, Adventure Guild, Level Teleporter, Banker — [docs/world/town.md](docs/world/town.md)

### Inventory

- **Backpack:** 25 start slots, at risk on death — [docs/inventory/backpack.md](docs/inventory/backpack.md)
- **Bank:** 15 start slots, safe storage, town-only — [docs/inventory/bank.md](docs/inventory/bank.md)
- **Items/Loot:** deferred — [docs/inventory/items.md](docs/inventory/items.md)

### UI

- **Controls:** keyboard + future gamepad — [docs/ui/controls.md](docs/ui/controls.md)
- **HUD:** HP, XP, level, floor overlay — [docs/ui/hud.md](docs/ui/hud.md)
- **Death screen:** multi-step flow with destination choice, mitigations, confirmation — [docs/ui/death-screen.md](docs/ui/death-screen.md)

---

## Documentation Map

```text
docs/
├── overview.md                — Project vision and design philosophy
├── best-practices.md          — Development guidelines and workflow
├── architecture/
│   ├── tech-stack.md          — Godot 4 stack details
│   ├── godot-basics.md        — Godot concepts for web devs
│   ├── project-structure.md   — File organization and naming
│   ├── scene-tree.md          — Complete node hierarchy for every scene
│   ├── autoloads.md           — GameState + EventBus singleton design
│   ├── signals.md             — Signal flow between all systems
│   └── game-dev-concepts.md   — Game dev fundamentals (GDScript)
├── objects/
│   ├── player.md              — Player node, script, movement, attack
│   ├── enemies.md             — Enemy tiers, AI, damage
│   ├── tilemap.md             — TileSet, floor generation
│   └── effects.md             — Slash, camera shake, visual fx
├── assets/
│   ├── tile-specs.md          — Tile dimensions, colors, formats
│   ├── sprite-specs.md        — Character/enemy shape specs
│   └── ui-theme.md            — Color palette, fonts, panel styles
├── systems/
│   ├── stats.md               — STR/DEX/STA/INT
│   ├── classes.md             — Warrior/Marksman/Mage
│   ├── skills.md              — Skill trees per class (hierarchical, infinite leveling)
│   ├── combat.md              — Auto-targeting, cooldowns, damage
│   ├── leveling.md            — XP curve, level-up effects
│   ├── death.md               — Penalties, gold buyout, Sacrificial Idol
│   ├── save.md                — FileAccess, JSON, user://, Base64 export
│   ├── movement.md            — Isometric movement + transform matrix
│   ├── spawning.md            — Enemy spawn system + timers
│   └── camera.md              — Camera follow, zoom, shake
├── world/
│   ├── dungeon.md             — Infinite descent, floor generation, caching
│   ├── town.md                — Town hub, NPC list, interaction
│   └── monsters.md            — Enemy types, danger tiers, spawning
├── inventory/
│   ├── backpack.md            — Risky carry storage (25 slots)
│   ├── bank.md                — Safe town storage (15 slots)
│   └── items.md               — Item system (deferred)
├── ui/
│   ├── controls.md            — Input methods (keyboard, gamepad)
│   ├── hud.md                 — HUD overlay with Control nodes
│   └── death-screen.md        — Death UI flow
└── testing/
    ├── test-strategy.md       — Testing approach (manual + GUT)
    ├── manual-tests.md        — 33 manual test cases
    └── automated-tests.md     — GUT unit + integration tests
```

---

## Current State

**Phase: Documentation & Planning**

All game systems are being specified in exhaustive detail before code is written. The original Phaser prototype is archived in `archive/phaser-prototype/` for reference.

## Priorities

1. Complete all documentation (architecture, objects, assets, systems, tests)
2. Implement Phase 0: Godot project scaffold
3. Implement Phase 1–7: Core gameplay systems
4. Polish and parity check against Phaser prototype

---

When helping:

- **Stay in scope.** Do exactly what was asked. Nothing more.
- **Do not assume.** If something is unclear, ask. Don't guess and run with it.
- **Read the spec first.** Check the relevant doc in `docs/` before proposing or writing anything.
- **One task at a time.** Keep responses focused on the single thing that was requested.
- **Flag, don't fix.** If you notice something out of scope that seems wrong, mention it — don't silently fix it.
- **Tests before code.** Write or reference the test cases before writing the implementation.
- **Verify your work.** Run tests or describe exact manual verification steps. Passing tests are the definition of "done."
- **Prefer "X over Y"** framing over "don't do X" when explaining tradeoffs — positive guidance is clearer than prohibitions.
- See [docs/best-practices.md](docs/best-practices.md) for full development guidelines
- See [docs/architecture/ai-workflow.md](docs/architecture/ai-workflow.md) for the detailed AI workflow protocol
