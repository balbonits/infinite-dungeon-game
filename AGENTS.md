# AGENTS.md — AI Coding Assistant Reference

This is the single reference file for any AI coding tool helping with **A Dungeon in the Middle of Nowhere** (repo: `dungeon-web-game`).

For detailed game design, see the [`docs/`](docs/) folder.

---

## Who You're Working For

**The user is the product owner — not a developer.** They own the game vision, make design decisions, and approve outcomes. They do not write, review, or debug code. All technical work is handled by AI.

- Frame questions in **game/player terms** — "should enemies feel threatening or swarmlike?" not "Dictionary vs Array?"
- Make technical decisions **autonomously** — the user approves what the game does, not how it's built
- When the user says "do X", **do it** — don't over-explain the approach unless asked
- Present options as **player experience tradeoffs**, not architecture tradeoffs
- The AI is the **entire dev team**. The user is the client.

---

## AI Team Structure

This project uses specialized AI team leads defined in `.claude/agents/`. Each team owns a specific domain. See [docs/conventions/teams.md](docs/conventions/teams.md) for full details.

| Team | Agent | Handles | Tickets |
|------|-------|---------|---------|
| Design | `@design-lead` | Game specs, formulas, balance | SPEC-* |
| QA | `@qa-lead` | Spec review, test planning | TEST-*, reviews |
| DevOps | `@devops-lead` | CI, Makefile, project config | SETUP-*, INFRA-* |
| Engine | `@engine-lead` | Godot scenes, physics, tiles | P1 scenes (Phase 2) |
| Systems | `@systems-lead` | C# logic, autoloads, signals | P1 logic (Phase 2) |
| UI | `@ui-lead` | HUD, menus, input | P1 UI (Phase 2) |
| World | `@world-lead` | Dungeon gen, floors, town | P4-* (Phase 2) |

**Routing:** Work is automatically routed to the right team. The user can also @mention a specific lead.

**Ownership:** Each team only modifies files in its domain. Cross-domain needs create dependency tickets.

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

See [docs/conventions/ai-workflow.md](docs/conventions/ai-workflow.md) for the full workflow reference.

### 3. Development Principles

- **KISS** — Keep It Simple. Use the simplest approach that satisfies the spec.
- **DRY** — Don't Repeat Yourself. Extract shared logic when repetition appears; don't prematurely abstract.
- **No scope creep** — Only implement what the current spec describes. Nothing extra.
- **Spec-driven** — Every system is fully documented in `docs/` before code is written. Read the relevant doc before modifying any code.
- **Test-driven** — Tests are written before implementation. Manual test cases first, then automated tests (GdUnit4 + xUnit), then code.
- **AI-coded** — All code is written by AI assistants. The user directs and reviews.
- **Free assets only** — No paid assets. Polygon2D placeholders, then free/open-source packs.

### 4. C# Conventions

Follow the [official Godot C# style guide](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_style_guide.html) and standard [C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).

**Script ordering** (top to bottom):
1. `using` directives
2. `namespace` declaration
3. `[Signal]` delegates
4. Enums
5. Constants
6. `[Export]` properties
7. Public properties
8. Private fields (`_camelCase`)
9. Lifecycle overrides (`_Ready`, `_Process`, `_PhysicsProcess`)
10. Public methods
11. Private methods

**All node scripts must use `partial`:**
```csharp
public partial class Player : CharacterBody2D { }
```

**Godot lifecycle methods** (override with `public override void`):
```csharp
public override void _Ready() { }
public override void _Process(double delta) { }
public override void _PhysicsProcess(double delta) { }
public override void _Input(InputEvent @event) { }
```

**Signals** use `[Signal]` attribute with `EventHandler` suffix:
```csharp
[Signal] public delegate void StatsChangedEventHandler();
[Signal] public delegate void EnemyDefeatedEventHandler(Vector2 position, int tier);

// Emit:
EmitSignal(SignalName.StatsChanged);
// Connect (preferred — auto-disconnects on node free):
source.Connect(SignalName.StatsChanged, new Callable(this, MethodName.OnStatsChanged));
```
Use `Connect()` over `+=` for signals — `Connect()` auto-disconnects when nodes are freed.

**Exports:**
```csharp
[Export] public float Speed { get; set; } = 190.0f;
[Export(PropertyHint.Range, "0,100,1")] public int Armor { get; set; }
[ExportGroup("Movement")]
[Export] public float JumpVelocity { get; set; } = 4.5f;
```

**Node references:**
```csharp
// Preferred — type-safe:
private Sprite2D _sprite = null!;
public override void _Ready() { _sprite = GetNode<Sprite2D>("Sprite"); }

// Or with unique names (% in scene tree):
[Export] public Sprite2D Sprite { get; set; } = null!;
```

**Scene architecture:**
- **"Call down, signal up"** — parents call methods on children; children emit signals to parents. Never reach up the tree.
- **One responsibility per node** — if a script exceeds ~300 lines, split behavior into child nodes.
- **Composition over inheritance** — build entities by combining specialized child scenes, not deep inheritance chains.
- **Static typing is enforced** — C# provides compile-time type checking. Use explicit types everywhere.
- **Nullable enabled** — use `null!` for fields initialized in `_Ready()`. Enable `<Nullable>enable</Nullable>` in `.csproj`.
- **Autoloads sparingly** — only for truly global state (GameState) and cross-system signals (EventBus). Access via static `Instance` property.

### 5. Tech Stack

| Layer | Technology | Notes |
|-------|-----------|-------|
| Engine | Godot 4.x (.NET edition) | Separate download from standard Godot |
| Language | C# / .NET 8+ | Strong typing, PascalCase, partial classes |
| Renderer | GL Compatibility | Broadest hardware support |
| Testing | GdUnit4 + xUnit | GdUnit4 for Godot scene tests, xUnit for pure logic |
| Serialization (saves) | System.Text.Json | Source-generated, human-readable, AOT-friendly |
| Serialization (cache) | MessagePack-CSharp v3 | Binary, ~10x faster, source generator support |
| Object pooling | Microsoft.Extensions.ObjectPool | Pool enemies, effects, projectiles — avoid GC |
| Async generation | System.Threading.Channels | Background floor generation pipeline |
| Physics | Built-in 2D | CharacterBody2D + Area2D |
| Perspective | Isometric 2D | 2:1 diamond tiles — floors 64×32, wall blocks 64×64 (ISS standard) |
| UI | Control nodes | Built-in UI, Theme resources |
| Persistence | FileAccess + JSON/MessagePack | user:// directory |
| Platform | Desktop native | macOS primary, Windows/Linux supported |

**NuGet dependencies:**
```xml
<ItemGroup>
  <PackageReference Include="gdUnit4.api" Version="5.1.0" />
  <PackageReference Include="gdUnit4.test.adapter" Version="3.0.0" />
  <PackageReference Include="gdUnit4.analyzers" Version="1.0.0" />
  <PackageReference Include="xunit" Version="2.9.3" />
  <PackageReference Include="xunit.runner.visualstudio" Version="3.0.2" />
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.0.0" />
  <PackageReference Include="MessagePack" Version="3.1.4" />
  <PackageReference Include="Microsoft.Extensions.ObjectPool" Version="9.0.0" />
</ItemGroup>
```

**Known limitation:** C# web export is not supported as of Godot 4.6. Desktop-only.

See [docs/architecture/tech-stack.md](docs/architecture/tech-stack.md) for details.

### 6. Naming Conventions

- **Public properties/methods:** `PascalCase` — `MoveSpeed`, `HandleMovement()`, `MaxHp`
- **Private fields:** `_camelCase` — `_attackTimer`, `_isDead`, `_moveSpeed`
- **Constants:** `PascalCase` — `MoveSpeed`, `AttackCooldown`, `EnemySoftCap`
- **Node names:** `PascalCase` — `CollisionShape2D`, `AttackRange`, `HitCooldownTimer`
- **Signals:** `PascalCase` + `EventHandler` — `EnemyDefeatedEventHandler`, `StatsChangedEventHandler`
- **Namespaces:** `PascalCase` by directory — `DungeonGame.Autoloads`, `DungeonGame.Scenes.Player`
- **Groups:** `snake_case` — `"player"`, `"enemies"`
- **C# files:** `PascalCase.cs` — `GameState.cs`, `DeathScreen.cs`, `EventBus.cs`
- **Scene/resource files:** `PascalCase` — `Player.tscn`, `DungeonTileset.tres`
- **No abbreviations** — `Player` not `Plr`, `Level` not `Lvl`

### 7. Project Structure

```text
dungeon-web-game/
├── DungeonGame.sln                — .NET solution file
├── DungeonGame.csproj             — Main project (Godot.NET.Sdk, NuGet refs)
├── project.godot                  — Godot 4 project config (.NET edition)
├── Makefile                       — AI-drivable automation (make help)
├── .editorconfig                  — Editor formatting rules (C# + Godot)
├── .gitignore / .gdignore         — Git + editor ignores (includes bin/, obj/)
├── .githooks/pre-commit           — C# formatting check on commit
├── .github/workflows/ci.yml      — GitHub Actions CI (build + lint + test)
├── AGENTS.md / CLAUDE.md          — AI assistant guidelines
├── README.md / CHANGELOG.md       — Project docs
├── archive/phaser-prototype/      — Original Phaser 3 code (preserved)
├── docs/                          — Game design documentation
│   ├── overview.md / dev-tracker.md / dev-journal.md
│   ├── architecture/              — Tech stack, setup guide, project structure, scene tree, autoloads, signals
│   ├── conventions/               — Code patterns, agile process, AI workflow, teams
│   ├── objects/                   — Player, enemies, tilemap, effects specs
│   ├── assets/                    — Tile, sprite, UI theme specs
│   ├── systems/                   — Stats, classes, combat, leveling, death, save, movement, spawning, camera
│   ├── world/                     — Dungeon, town, monsters
│   ├── inventory/                 — Backpack, bank, items
│   ├── ui/                        — Controls, HUD, death screen
│   └── testing/                   — Test strategy, manual tests, automated tests
├── scenes/                        — Godot scenes (.tscn)
│   ├── Main.tscn
│   ├── dungeon/ player/ enemies/ ui/
├── scripts/                       — C# scripts (.cs)
│   ├── autoloads/                 — GameState.cs, EventBus.cs
│   └── GenerateTiles.py           — Tile asset generator (Python)
├── tests/                         — GdUnit4 + xUnit automated tests
├── addons/                        — GdUnit4 addon (if required)
├── assets/                        — Tiles, sprites, UI, audio (binary assets)
│   └── ATTRIBUTION.md             — Asset license tracking
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
- **Tiles:** [Screaming Brain Studios](https://opengameart.org/users/screaming-brain-studios) (CC0) — sole source for all isometric textures/tiles. ISS pack: floors 64×32, wall blocks 64×64, magenta key
- See [docs/assets/ui-theme.md](docs/assets/ui-theme.md) for the full color palette

### 10. Development Automation

All development can be driven from the terminal. Run `make help` for available targets.

**Setup (first time):**
```bash
make setup          # Verify .NET SDK + Godot .NET, configure git hooks, NuGet restore
```

**Daily workflow:**
```bash
make build          # dotnet build
make test           # dotnet test (xUnit + GdUnit4)
make lint           # dotnet format --verify-no-changes
make format         # dotnet format (auto-fix)
make check          # build + lint + test (all three)
make run            # Launch the game (godot --path .)
make tiles          # Generate tile assets (Python)
```

**Tools required:**
| Tool | Install | Purpose |
|------|---------|---------|
| .NET 9 SDK | `brew install dotnet` | Build, test, format C# |
| Godot 4.x (.NET) | Download from godotengine.org (.NET build) | Engine, scene test runner |
| VS Code + C# Dev Kit | `ms-dotnettools.csharp` extension | IDE, IntelliSense, debugging |

See [docs/architecture/setup-guide.md](docs/architecture/setup-guide.md) for full install instructions.

**CI:** GitHub Actions (`.github/workflows/ci.yml`) runs `dotnet build` + `dotnet format --verify-no-changes` + `dotnet test` on every push/PR to `main`.

**Pre-commit hook:** `.githooks/pre-commit` runs `dotnet format --verify-no-changes` on staged `.cs` files. Activated by `make setup`.

See [docs/conventions/ai-workflow.md](docs/conventions/ai-workflow.md) for the full automation reference.

---

## Game Design Quick Reference

Detailed design docs live in `docs/`. Here's a summary with links.

### Character & Stats

- **4 stats:** STR (physical power), DEX (agility), STA (health), INT (magic) — [docs/systems/stats.md](docs/systems/stats.md)
- **3 classes:** Warrior (STR/STA), Ranger (DEX), Mage (INT) — [docs/systems/classes.md](docs/systems/classes.md)
- **Persistent character** — no rerolls, one character forever

### Skills

Hierarchical skill trees per class with unique category names, hybrid leveling (use-based + point-based), infinite scaling — [docs/systems/skills.md](docs/systems/skills.md)

### Color System

Unified cool→warm gradient for all game elements (enemies, items, zones), level-relative coloring — [docs/systems/color-system.md](docs/systems/color-system.md)

### Combat

Auto-targeting, cooldown-based, scales with level and stats — [docs/systems/combat.md](docs/systems/combat.md)

### Leveling

Redesigned XP curve (linear-polynomial hybrid), rested XP bonus, floor-scaling enemy XP, no level cap — [docs/systems/leveling.md](docs/systems/leveling.md)

### Player Engagement

Feedback loops, session pacing, juice/feel, retention hooks — [docs/systems/player-engagement.md](docs/systems/player-engagement.md)

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
├── dev-tracker.md             — Master ticket list and dependency graph
├── dev-journal.md             — Running session log
├── architecture/
│   ├── tech-stack.md          — Godot 4 stack details
│   ├── project-structure.md   — File organization and naming
│   ├── scene-tree.md          — Complete node hierarchy for every scene
│   ├── autoloads.md           — GameState + EventBus singleton design
│   ├── signals.md             — Signal flow between all systems
│   ├── setup-guide.md         — .NET SDK, Godot .NET, VS Code setup
│   └── analytics.md           — Opt-in telemetry, bug reporting, feedback (offline-first)
├── conventions/
│   ├── code.md                — Code patterns, naming, quality standards
│   ├── agile.md               — Dev process, tickets, scope discipline
│   ├── ai-workflow.md         — Dev ticket cycle protocol
│   └── teams.md               — AI team structure and ownership
├── reference/
│   ├── godot-basics.md        — Godot concepts for web devs
│   ├── game-dev-concepts.md   — Game dev fundamentals (C#)
│   ├── game-development.md    — Research journal (accumulated learnings)
│   └── subagent-research.md   — AI agent design research
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
│   ├── classes.md             — Warrior/Ranger/Mage
│   ├── skills.md              — Skill trees per class (hierarchical, infinite leveling)
│   ├── color-system.md        — Unified color gradient (cool→warm, level-relative)
│   ├── combat.md              — Auto-targeting, cooldowns, damage
│   ├── leveling.md            — XP curve, rested XP, floor-scaling enemy XP
│   ├── player-engagement.md   — Feedback loops, session pacing, juice/feel, retention
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
    ├── test-strategy.md       — Testing approach (manual + GdUnit4 + xUnit)
    ├── manual-tests.md        — 33 manual test cases
    └── automated-tests.md     — GdUnit4 + xUnit automated tests
```

---

## Current State

**Phase: Implementation active.** All 26 specs are locked. Code is being written.

Stack: Godot 4.6 + C# (.NET 8+). Learning demo with 46-step automated showcase covering all core mechanics and UI patterns. 51 unit tests + 40 E2E assertions passing.

**Current mode:** Implementation. Follow the dev ticket cycle in [docs/conventions/ai-workflow.md](docs/conventions/ai-workflow.md). SETUP tickets partially complete (02a, 02b, 04a done). P1 tickets next.

## Priorities

1. **Complete SETUP tickets** — see [docs/dev-tracker.md](docs/dev-tracker.md) for remaining SETUP items
2. **P1 tickets** — Core gameplay loop (GameState autoload, tilemap, player, enemies, combat, HUD, death screen)
3. **P2+ tickets** — Systems depth (leveling, stats, classes, save/load, death flow)

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
- See [docs/conventions/code.md](docs/conventions/code.md) for code patterns, naming, and quality standards
- See [docs/conventions/agile.md](docs/conventions/agile.md) for dev process, scope discipline, and ticket workflow
- See [docs/conventions/ai-workflow.md](docs/conventions/ai-workflow.md) for the detailed AI workflow protocol
