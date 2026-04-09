# Development Backlog

Single source of truth for all work. Organized as tickets within epics. Specs are completed before implementation — no code until the relevant spec is locked.

## How to Use

- **Status:** `To Do` | `In Progress` | `Done` | `Blocked`
- **Ticket ID:** `EPIC-NUMBER` (e.g., `SPEC-01`, `P1-03`)
- **Rules:** Spec tickets (`SPEC-*`) gate implementation. `Blocked` = dependency not `Done`. One ticket = one branch.

### Priority Tiers

Every ticket has a priority tier. Tiers determine work order within and across teams.

| Tier | Name | Meaning | Timeline |
|------|------|---------|----------|
| **P0** | Critical | Urgent and immediate. Blocks all other work. Drop everything. | Now |
| **P1** | Urgent | Urgent with a defined timeline. Must ship for MVP. | This sprint |
| **P2** | Important | Important with a defined timeline. Needed for a specific milestone. | This phase |
| **P3** | Upcoming | Planned feature with a timeline. Scheduled but not urgent. | Next phase |
| **P4** | Someday | Known work without a timeline. Will get done eventually. | Unscheduled |
| **Backlog** | Unprioritized | Not yet evaluated. Needs triage before work begins. | — |
| **Tech Debt** | System maintenance | Refactors, cleanup, infra — no user-facing change. No priority. | As capacity allows |

**Rules:**
- P0 tickets are rare — only for build-breaking, data-loss, or blocker bugs
- P1 = MVP scope (SETUP + P1 + P2). Every MVP ticket is P1 until done.
- P2 = post-MVP milestones (P3, P4, P5)
- P3 = planned features with known scope (P6 polish, future systems)
- P4 = nice-to-haves without deadlines (gamepad, mobile, cloud save)
- Backlog = ideas captured but not triaged
- Tech Debt = accumulated when shipping fast, tracked but never urgent
- Research tickets (RES-*) inherit the priority of the ticket they feed into

---

## Ticket Index

Quick reference for all tickets. Search by ID, title, or status.

### SPEC — Spec Completion (26 sub-tickets)

All handled by `@design-lead`, reviewed by `@qa-lead`.

| ID | Title | Status | Deps | Doc |
|----|-------|--------|------|-----|
| SPEC-01a | Town layout and scene flow | Done | — | [town.md](world/town.md) |
| SPEC-01b | Item Shop NPC spec | Done | SPEC-01a | [town.md](world/town.md) |
| SPEC-01c | Blacksmith NPC spec | Done | SPEC-01a, SPEC-06a | [town.md](world/town.md) |
| SPEC-01d | Adventure Guild NPC spec | Done | SPEC-01a | [town.md](world/town.md) |
| SPEC-01e | Level Teleporter NPC spec | Done | SPEC-01a | [town.md](world/town.md) |
| SPEC-01f | Banker NPC spec | Done | SPEC-01a | [town.md](world/town.md) |
| SPEC-02 | Crafting/blacksmith system | Done | SPEC-06a | [items.md](inventory/items.md), [town.md](world/town.md) |
| SPEC-03 | Mage spell acquisition | Done | SPEC-05 | [magic.md](systems/magic.md) |
| SPEC-04a | STR → melee damage formula | Done | — | [stats.md](systems/stats.md) |
| SPEC-04b | DEX → attack speed / evasion formula | Done | — | [stats.md](systems/stats.md) |
| SPEC-04c | STA → HP / defense formula | Done | — | [stats.md](systems/stats.md) |
| SPEC-04d | INT → magic power formula | Done | — | [stats.md](systems/stats.md) |
| SPEC-04e | Stat diminishing returns curve | Done | SPEC-04a–d | [stats.md](systems/stats.md) |
| SPEC-05a | Warrior per-level bonuses | Done | SPEC-04a–d | [classes.md](systems/classes.md) |
| SPEC-05b | Ranger per-level bonuses | Done | SPEC-04a–d | [classes.md](systems/classes.md) |
| SPEC-05c | Mage per-level bonuses | Done | SPEC-04a–d | [classes.md](systems/classes.md) |
| SPEC-05d | Free stat point allocation rules | Done | SPEC-05a–c | [classes.md](systems/classes.md) |
| SPEC-06a | Item data model (types, tiers, fields) | Done | SPEC-04e | [items.md](inventory/items.md) |
| SPEC-06b | Loot drop rates and tables | Done | SPEC-06a | [items.md](inventory/items.md) |
| SPEC-06c | Equipment slot stat effects | Done | SPEC-06a | [items.md](inventory/items.md) |
| SPEC-07a | XP curve formula (lock constant) | Done | — | [leveling.md](systems/leveling.md) |
| SPEC-07b | Floor-scaling enemy XP multiplier | Done | SPEC-07a | [leveling.md](systems/leveling.md) |
| SPEC-07c | Rested XP rules and display | Done | — | [leveling.md](systems/leveling.md) |
| SPEC-07d | Level-up milestone rewards | Done | SPEC-07a | [leveling.md](systems/leveling.md) |
| SPEC-08 | Lock skill system spec | Done | SPEC-04e, SPEC-05d | [skills.md](systems/skills.md) |
| SPEC-09a | Dungeon generation algorithm | Done | — | [dungeon.md](world/dungeon.md) |
| SPEC-09b | Floor difficulty scaling | Done | SPEC-09a | [dungeon.md](world/dungeon.md) |
| SPEC-09c | Special room types (boss, treasure, safe) | Done | SPEC-09a | [dungeon.md](world/dungeon.md) |
| SPEC-10 | Lock save system spec | Done | SPEC-09a | [save.md](systems/save.md) |
| SPEC-11a | Backpack UI and death-loss logic | Done | SPEC-06a | [backpack.md](inventory/backpack.md) |
| SPEC-11b | Bank UI and deposit/withdraw flow | Done | SPEC-06a | [bank.md](inventory/bank.md) |
| SPEC-12 | Finalize autoloads spec | Done | — | [autoloads.md](architecture/autoloads.md) |

### SETUP — C# Project Setup (10 sub-tickets)

All handled by `@devops-lead`.

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| SETUP-01 | Install dev environment | P1 | Done | — |
| SETUP-02a | Generate .csproj via Godot editor | P0 | To Do | SETUP-01 |
| SETUP-02b | Create .sln and verify dotnet build | P0 | To Do | SETUP-02a |
| SETUP-02c | Add NuGet packages (gdUnit4, xunit, MessagePack) | P1 | To Do | SETUP-02b |
| SETUP-03 | Remove GUT addon and GDScript test files | P1 | To Do | SETUP-02a |
| SETUP-04a | Update project.godot for .NET runtime | P0 | To Do | SETUP-02a |
| SETUP-04b | Define Input Map actions in project.godot | P1 | To Do | SETUP-04a |
| SETUP-05 | Update Makefile for C# (dotnet build/test/run) | P1 | To Do | SETUP-02b |
| SETUP-06 | Update CI workflow and pre-commit hook for C# | P2 | To Do | SETUP-05 |
| SETUP-07 | Write C# sanity tests (build + run + xUnit hello world) | P1 | To Do | SETUP-02c, SETUP-04a |

### P1 — Prototype Parity (28 sub-tickets)

`@systems-lead` (logic), `@engine-lead` (scenes), `@ui-lead` (HUD/death), `@qa-lead` (tests).

**P1-01 — GameState Autoload**

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| P1-01a | Create GameState singleton class with base properties (HP, MaxHP, Level, XP, Floor) | P1 | To Do | SETUP-07 |
| P1-01b | Implement TakeDamage, Heal, AwardXp methods | P1 | To Do | P1-01a |
| P1-01c | Implement level-up logic (XP threshold, stat increase, reset) | P1 | To Do | P1-01b |
| P1-01d | Add StatsChanged signal and emit on every state mutation | P1 | To Do | P1-01b |
| P1-01e | Register as autoload in project.godot | P1 | To Do | P1-01a |
| P1-01f | Write GameState unit tests (24 tests) | P1 | To Do | P1-01d |

**P1-02 — EventBus Autoload**

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| P1-02a | Create EventBus singleton with all signal definitions | P1 | To Do | SETUP-07 |
| P1-02b | Register as autoload in project.godot | P1 | To Do | P1-02a |

**P1-03 — Tilemap and Dungeon Room**

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| P1-03a | Create TileMapLayer with floor and wall tile sources | P1 | To Do | P1-01e |
| P1-03b | Build single static dungeon room (walls + floor + collision) | P1 | To Do | P1-03a |
| P1-03c | Set up collision layer 1 (walls) with physics polygons | P1 | To Do | P1-03b |

**P1-04 — Player Scene and Movement**

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| P1-04a | Create player.tscn (CharacterBody2D + CollisionShape + Polygon2D sprite) | P1 | To Do | P1-03b |
| P1-04b | Add player to "player" group and set collision layer 2 / mask 1 | P1 | To Do | P1-04a |
| P1-04c | Implement arrow key input reading via Input.GetVector | P1 | To Do | P1-04a |
| P1-04d | Implement isometric Transform2D and MoveAndSlide | P1 | To Do | P1-04c |
| P1-04e | Add Camera2D child with 2x zoom and position smoothing | P1 | To Do | P1-04a |
| P1-04f | Write movement unit tests (iso transform math, normalized speed) | P1 | To Do | P1-04d |

**P1-05 — Enemy Scene and Tiers**

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| P1-05a | Create enemy.tscn (CharacterBody2D + CollisionShape + Polygon2D) | P1 | To Do | P1-04a |
| P1-05b | Implement DangerTier property and stat formulas (HP/speed/damage/XP) | P1 | To Do | P1-05a |
| P1-05c | Implement tier-based color assignment | P1 | To Do | P1-05b |
| P1-05d | Implement straight-line chase AI (move toward player each frame) | P1 | To Do | P1-05a |
| P1-05e | Add HitArea (Area2D) with contact damage and 700ms cooldown timer | P1 | To Do | P1-05a |
| P1-05f | Implement TakeDamage and Die methods | P1 | To Do | P1-05b |
| P1-05g | Write enemy stat formula unit tests (15 tests) | P1 | To Do | P1-05b |

**P1-06 — Enemy Spawning System**

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| P1-06a | Create SpawnManager node with initial 10-enemy spawn | P1 | To Do | P1-05f |
| P1-06b | Implement periodic spawn timer (2.8s) with soft cap (14) | P1 | To Do | P1-06a |
| P1-06c | Implement respawn-on-death timer (1.4s delay) | P1 | To Do | P1-06a |
| P1-06d | Implement random edge spawn position calculation | P1 | To Do | P1-06a |
| P1-06e | Write spawning integration tests (5 tests) | P1 | To Do | P1-06b |

**P1-07 — Combat System**

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| P1-07a | Add AttackRange Area2D to player (CircleShape2D, 78px radius) | P1 | To Do | P1-06a |
| P1-07b | Implement face button attack input (action_cross default) | P1 | To Do | P1-07a |
| P1-07c | Implement nearest-enemy targeting within AttackRange | P1 | To Do | P1-07a |
| P1-07d | Implement attack cooldown (420ms) and damage dealing | P1 | To Do | P1-07c |
| P1-07e | Implement slash visual effect (Polygon2D + tween fade/rise) | P1 | To Do | P1-07d |
| P1-07f | Implement camera shake on player hit (90ms, ±3px) | P1 | To Do | P1-05e |
| P1-07g | Implement XP award on enemy kill (via GameState.AwardXp) | P1 | To Do | P1-07d, P1-01c |
| P1-07h | Write combat integration tests (8 tests) | P1 | To Do | P1-07g |

**P1-08 — HUD Overlay**

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| P1-08a | Create CanvasLayer (layer 10) with HUD Control node | P1 | To Do | P1-01d |
| P1-08b | Build PanelContainer with StyleBoxFlat (dark panel, gold border) | P1 | To Do | P1-08a |
| P1-08c | Add title, controls hint, and stats labels | P1 | To Do | P1-08b |
| P1-08d | Connect StatsChanged signal to update stats label | P1 | To Do | P1-08c |
| P1-08e | Set MouseFilter to Ignore (click-through) | P1 | To Do | P1-08a |

**P1-09 — Death Screen and Restart**

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| P1-09a | Create death screen CanvasLayer (hidden by default) | P1 | To Do | P1-01d |
| P1-09b | Show death screen when GameState.HP reaches 0 | P1 | To Do | P1-09a |
| P1-09c | Implement restart via Esc key (_UnhandledInput) | P1 | To Do | P1-09b |
| P1-09d | Reset GameState and reload scene on restart | P1 | To Do | P1-09c |
| P1-09e | Write death/restart integration tests (6 tests) | P1 | To Do | P1-09d |

**P1-10 — Main Scene Wiring**

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| P1-10a | Create main.tscn with Dungeon, Entities, UILayer containers | P1 | To Do | P1-07h, P1-08d, P1-09d |
| P1-10b | Wire player spawn into Entities container at room center | P1 | To Do | P1-10a |
| P1-10c | Wire SpawnManager into Entities container | P1 | To Do | P1-10a |
| P1-10d | Wire HUD and DeathScreen into UILayer | P1 | To Do | P1-10a |
| P1-10e | Run full 33-case manual test pass (MT-001 through MT-033) | P1 | To Do | P1-10d |

**P1-11 — Debug Tools**

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| P1-11a | Create debug overlay (FPS, entity count, floor, player pos) | P1 | To Do | P1-10e |
| P1-11b | Add input visualizer overlay (shows active keys/buttons, D-pad state, bumper state) | P1 | To Do | P1-11a |
| P1-11c | Add collision shape visualizer (toggle collision shape rendering) | P1 | To Do | P1-11a |
| P1-11d | Add game state inspector (live stat values, XP, cooldown timers) | P1 | To Do | P1-11a |
| P1-11e | Add entity inspector (tap enemy to see HP, tier, speed, target status) | P1 | To Do | P1-11a |
| P1-11f | Toggle all debug tools with a master hotkey (F3) | P1 | To Do | P1-11a |
| P1-11g | Implement debug tool visibility flag (disable from view and screen recordings) | P1 | To Do | P1-11f |

### P2 — Core Systems (18 sub-tickets)

**P2-01 — Leveling Redesign**

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| P2-01a | Replace linear XP formula with quadratic (`L^2 * 45`) | P1 | To Do | P1-10e |
| P2-01b | Implement floor-scaling enemy XP (`base * floor_multiplier`) | P1 | To Do | P2-01a |
| P2-01c | Implement multi-reward level-up (HP, stat points, skill points) | P1 | To Do | P2-01a |
| P2-01d | Implement milestone bonuses (every 10th level: +2 stat, +1 skill) | P1 | To Do | P2-01c |
| P2-01e | Implement rested XP (accumulate offline, double kill XP) | P1 | To Do | P2-01a |
| P2-01f | Write leveling unit tests | P1 | To Do | P2-01d |

**P2-02 — Stats System**

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| P2-02a | Implement 4-stat model (STR/DEX/STA/INT) with diminishing returns | P1 | To Do | P1-10e |
| P2-02b | Implement STR → melee damage (flat + % boost) | P1 | To Do | P2-02a |
| P2-02c | Implement DEX → attack speed + dodge chance | P1 | To Do | P2-02a |
| P2-02d | Implement STA → max HP + HP regen | P1 | To Do | P2-02a |
| P2-02e | Implement INT → mana pool + mana regen + processing efficiency | P1 | To Do | P2-02a |
| P2-02f | Replace placeholder combat formula with STR-based formula | P1 | To Do | P2-02b |
| P2-02g | Write stat formula unit tests | P1 | To Do | P2-02e |

**P2-03 — Class System**

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| P2-03a | Implement class enum (Warrior/Ranger/Mage) and creation bonuses | P1 | To Do | P2-02g |
| P2-03b | Implement per-level auto stat bonuses per class | P1 | To Do | P2-03a |
| P2-03c | Implement milestone scaling (every 25 levels, +1 to non-zero bonuses) | P1 | To Do | P2-03b |
| P2-03d | Implement free stat point allocation (3/level, +2 at milestones) | P1 | To Do | P2-03b |
| P2-03e | Create character creation screen (pick class + name) | P1 | To Do | P2-03a |
| P2-03f | Write class system unit tests | P1 | To Do | P2-03d |

**P2-04 — Save/Load System**

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| P2-04a | Create SaveManager autoload with Save/Load methods | P1 | To Do | P1-10e |
| P2-04b | Implement 10-slot save file structure (`user://saves/slot_N.json`) | P1 | To Do | P2-04a |
| P2-04c | Implement auto-save triggers (level-up, floor transition, town, death) | P1 | To Do | P2-04b |
| P2-04d | Implement save data validation and version migration | P1 | To Do | P2-04b |
| P2-04e | Implement Base64 export/import with clipboard | P1 | To Do | P2-04b |
| P2-04f | Implement backup-before-import | P1 | To Do | P2-04b |
| P2-04g | Create save slot selection UI (10 slots) | P1 | To Do | P2-04b, P2-03e |
| P2-04h | Write save/load integration tests | P1 | To Do | P2-04d |

**P2-05 — Full Death Flow**

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| P2-05a | Implement multi-step death screen (destination → mitigations → summary → confirm) | P1 | To Do | P2-04c |
| P2-05b | Implement EXP loss formula (`min(deepestFloor * 0.4, 50)%`) | P1 | To Do | P2-05a |
| P2-05c | Implement backpack item loss formula (`floor(deepestFloor / 10) + 1`) | P1 | To Do | P2-05a |
| P2-05d | Implement gold buyout options (EXP protection + backpack protection) | P1 | To Do | P2-05b |
| P2-05e | Implement Sacrificial Idol auto-consume on death | P1 | To Do | P2-05c |
| P2-05f | Implement respawn destinations (town vs last safe spot) | P1 | To Do | P2-05a |
| P2-05g | Write death flow integration tests | P1 | To Do | P2-05f |

### P3 — Skills & Equipment (14 sub-tickets)

**P3-01 — Skill Tree System**

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| P3-01a | Implement skill data model (base skills, specific skills, innate skills) | P2 | To Do | P2-03f |
| P3-01b | Implement use-based skill XP system (XP per use, floor multiplier) | P2 | To Do | P3-01a |
| P3-01c | Implement skill point allocation (XP boost per point) | P2 | To Do | P3-01a |
| P3-01d | Implement passive bonus formula (`skill_level * multiplier * DR`) | P2 | To Do | P3-01a |
| P3-01e | Implement weapon requirement checks for specific skills | P2 | To Do | P3-01a, P3-02a |
| P3-01f | Create skill tree UI (view categories, base skills, specific skills) | P2 | To Do | P3-01d |
| P3-01g | Write skill system unit tests | P2 | To Do | P3-01d |

**P3-02 — Equipment System**

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| P3-02a | Implement item data model (Item struct with base stats, quality, affixes) | P2 | To Do | P2-02g |
| P3-02b | Implement equipment slot system (11 slots + 10 rings) | P2 | To Do | P3-02a |
| P3-02c | Implement equip/unequip with stat recalculation | P2 | To Do | P3-02b |
| P3-02d | Implement affix data model (prefix/suffix, tier gating by item level) | P2 | To Do | P3-02a |
| P3-02e | Implement defense formula (diminishing returns, K=100) | P2 | To Do | P3-02c |
| P3-02f | Create equipment UI (slots, drag-to-equip, stat comparison) | P2 | To Do | P3-02c |
| P3-02g | Write equipment unit tests | P2 | To Do | P3-02e |

### P4 — World (12 sub-tickets)

**P4-01 — Procedural Dungeon Generation**

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| P4-01a | Implement BSP room placement algorithm | P2 | To Do | P1-03c |
| P4-01b | Implement Drunkard's Walk corridor generation | P2 | To Do | P4-01a |
| P4-01c | Implement Cellular Automata smoothing pass | P2 | To Do | P4-01b |
| P4-01d | Place entrance room, exit room, and staircase tiles | P2 | To Do | P4-01c |
| P4-01e | Implement seeded generation (floor seed → deterministic layout) | P2 | To Do | P4-01d |
| P4-01f | Write dungeon generation tests (connectivity, room count, entrance/exit) | P2 | To Do | P4-01e |

**P4-02 — Floor Caching and Descent**

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| P4-02a | Implement 10-floor cache (store/retrieve floor layouts) | P2 | To Do | P4-01e |
| P4-02b | Implement floor transition (walk to stairs → load next floor) | P2 | To Do | P4-02a |
| P4-02c | Implement cache eviction (oldest floor purged when cache full) | P2 | To Do | P4-02a |
| P4-02d | Implement ascend/descend with cached floor restoration | P2 | To Do | P4-02b |

**P4-03 — Town Hub**

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| P4-03a | Create town scene with compact walkable layout and 5 NPC positions | P2 | To Do | P1-10e |
| P4-03b | Implement walk-up NPC interaction (proximity trigger → panel open) | P2 | To Do | P4-03a |

### P5 — Inventory & Items (16 sub-tickets)

**P5-01 — Backpack System**

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| P5-01a | Implement backpack data structure (25 slots, stackable items) | P2 | To Do | P2-05g |
| P5-01b | Implement add/remove/move item operations | P2 | To Do | P5-01a |
| P5-01c | Create backpack UI (grid of slots, item icons, stack counts) | P2 | To Do | P5-01b |
| P5-01d | Write backpack unit tests | P2 | To Do | P5-01b |

**P5-02 — Bank System**

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| P5-02a | Implement bank data structure (50 slots, expandable) | P2 | To Do | P4-03b |
| P5-02b | Implement deposit/withdraw between backpack and bank | P2 | To Do | P5-02a, P5-01b |
| P5-02c | Implement bank expansion purchase (10 slots, `500 * N^2` gold) | P2 | To Do | P5-02a |
| P5-02d | Create bank UI (Banker NPC panel with deposit/withdraw) | P2 | To Do | P5-02b |

**P5-03 — Loot and Item Drops**

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| P5-03a | Implement drop chance formula (tier-based + floor bonus) | P2 | To Do | P3-02g, P5-01d |
| P5-03b | Implement base quality distribution (Normal/Superior/Elite by floor) | P2 | To Do | P5-03a |
| P5-03c | Implement material drop system (25% flat chance, floor-appropriate tier) | P2 | To Do | P5-03a |
| P5-03d | Implement boss first-kill rare material drops | P2 | To Do | P5-03c |
| P5-03e | Create loot pickup interaction (walk over → add to backpack) | P2 | To Do | P5-03b |

**P5-04 — Blacksmith Crafting**

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| P5-04a | Implement deterministic affix application (pick affix, pay materials) | P2 | To Do | P5-01d, P3-02d |
| P5-04b | Implement gear recycling (break down → materials) | P2 | To Do | P5-04a |
| P5-04c | Create Blacksmith NPC panel UI (add affix, recycle, material costs) | P2 | To Do | P5-04b |

### P6 — Polish & Juice (14 sub-tickets)

**P6-01 — Visual Effects**

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| P6-01a | Implement level-up flash and particle burst | P3 | To Do | P1-07h |
| P6-01b | Implement enemy death fade-out effect | P3 | To Do | P1-07h |
| P6-01c | Implement damage number popups (floating text) | P3 | To Do | P1-07h |

**P6-02 — Game Feel**

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| P6-02a | Implement hitstop (2-3 frame pause on hit) | P3 | To Do | P1-07h |
| P6-02b | Implement knockback on enemy hit | P3 | To Do | P1-07h |
| P6-02c | Implement screen flash on player damage | P3 | To Do | P1-07h |

**P6-03 — Object Pooling**

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| P6-03a | Implement enemy object pool (pre-allocate, reuse on spawn/death) | P3 | To Do | P1-10e |
| P6-03b | Implement effect object pool (slash effects, damage numbers) | P3 | To Do | P6-01c |

**P6-04 — Art Assets**

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| P6-04a | Replace player Polygon2D with DCSS character sprite | P3 | To Do | P1-10e |
| P6-04b | Replace enemy Polygon2D with DCSS monster sprites (per tier) | P3 | To Do | P1-10e |
| P6-04c | Replace tilemap placeholder tiles with DCSS dungeon tiles | P3 | To Do | P1-10e |

**P6-05 — Audio**

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| P6-05a | Add attack/hit sound effects | P3 | To Do | P1-10e |
| P6-05b | Add dungeon ambient music (loopable) | P3 | To Do | P1-10e |

**P6-06 — Testing**

| ID | Title | Pri | Status | Deps |
|----|-------|-----|--------|------|
| P6-06a | Set up test coverage reporting | P3 | To Do | SETUP-07 |
| P6-06b | Set up CI test runner (dotnet test in GitHub Actions) | P3 | To Do | SETUP-06 |

### RES — Research (parallel, enhances specs & implementation)

Research tickets run in parallel via researcher agent. Findings feed back into specs, tickets, and code patterns. Each research ticket produces a brief in `docs/reference/`.

**RES-SETUP — Pre-Implementation Research**

| ID | Title | Pri | Status | Feeds Into |
|----|-------|-----|--------|-----------|
| RES-01 | Godot 4 C# project structure best practices | P1 | To Do | SETUP-02, project-structure.md |
| RES-02 | Godot 4 autoload patterns and singleton pitfalls in C# | P1 | To Do | P1-01, P1-02 |
| RES-03 | CharacterBody2D movement patterns (MoveAndSlide edge cases) | P1 | To Do | P1-04, movement.md |
| RES-04 | Godot 4 TileMap/TileMapLayer best practices for dungeon crawlers | P1 | To Do | P1-03, P4-01 |
| RES-05 | Input Map + rebinding at runtime in Godot 4 C# | P1 | To Do | SETUP-04b, controls.md |

**RES-COMBAT — Combat & Game Feel Research**

| ID | Title | Pri | Status | Feeds Into |
|----|-------|-----|--------|-----------|
| RES-06 | How Diablo 2 / PoE handle target cycling on controller | P1 | To Do | P1-07, combat.md |
| RES-07 | Hitstop / hit-freeze implementation patterns in 2D games | P3 | To Do | P6-02, player-engagement.md |
| RES-08 | Auto-attack + button-press hybrid combat in ARPGs (edge cases, feel) | P1 | To Do | P1-07, combat.md |
| RES-09 | Object pooling patterns for enemies and effects in Godot 4 | P3 | To Do | P6-03, spawning.md |
| RES-10 | Damage number popup patterns (font, animation, stacking) | P3 | To Do | P6-01c, effects.md |

**RES-SYSTEMS — Core Systems Research**

| ID | Title | Pri | Status | Feeds Into |
|----|-------|-----|--------|-----------|
| RES-11 | XP curve balancing: how games tune leveling speed post-launch | P1 | To Do | P2-01, leveling.md |
| RES-12 | Stat diminishing returns: how PoE / D2 / Last Epoch handle soft caps | P1 | To Do | P2-02, stats.md |
| RES-13 | Skill tree UI patterns for controller-first games | P2 | To Do | P3-01f, skills.md |
| RES-14 | Equipment stat stacking edge cases (10 rings, overflow, cap behavior) | P2 | To Do | P3-02, items.md |
| RES-15 | Save system corruption prevention and recovery patterns | P1 | To Do | P2-04, save.md |

**RES-WORLD — Dungeon & World Research**

| ID | Title | Pri | Status | Feeds Into |
|----|-------|-----|--------|-----------|
| RES-16 | BSP + Drunkard's Walk implementation reference (code examples, tuning params) | P2 | To Do | P4-01, dungeon.md |
| RES-17 | Floor caching strategies in roguelikes (memory, serialization) | P2 | To Do | P4-02, dungeon.md |
| RES-18 | Boss encounter design patterns in hack-n-slash games | P2 | To Do | P4-01, dungeon.md |
| RES-19 | NPC interaction UI patterns on controller (proximity, panels) | P2 | To Do | P4-03, town.md |

**RES-ITEMS — Inventory & Loot Research**

| ID | Title | Pri | Status | Feeds Into |
|----|-------|-----|--------|-----------|
| RES-20 | Diablo 2 affix generation deep dive (ilvl, alvl, qlvl formulas) | P2 | To Do | P5-03, items.md |
| RES-21 | Deterministic crafting UX patterns (how to make no-RNG feel rewarding) | P2 | To Do | P5-04, items.md |
| RES-22 | Inventory UI patterns for controller-first games (grid, cursor, quick-move) | P2 | To Do | P5-01c, backpack.md |
| RES-23 | Drop rate tuning: how games avoid loot drought and loot flood | P2 | To Do | P5-03, items.md |

**RES-QA — Testing & Edge Cases**

| ID | Title | Pri | Status | Feeds Into |
|----|-------|-----|--------|-----------|
| RES-24 | Common game-breaking bugs in ARPGs (duplication, overflow, desync) | P1 | To Do | All P-tickets |
| RES-25 | Roguelike edge cases: seed reproducibility, RNG fairness testing | P2 | To Do | P4-01, dungeon.md |
| RES-26 | Performance profiling patterns for Godot 4 C# (GC pressure, allocations) | P3 | To Do | P6-03, all code |
| RES-27 | Accessibility in ARPGs: colorblind modes, input assist, UI scaling | P4 | To Do | P6, hud.md, color-system.md |
| RES-28 | Godot 4 debug overlay packages/addons (input visualizer, perf monitor, collision viewer) | P1 | To Do | P1-11 |

---

## Dependency Graph

```
ALL SPECS DONE ✓

SETUP-01 ✓ → SETUP-02 → SETUP-03
                  │
                  ├→ SETUP-04 → SETUP-07
                  └→ SETUP-05 → SETUP-06

SETUP-07 → P1-01 → P1-03 → P1-04 → P1-05 → P1-06 → P1-07 ─┐
              │                                                │
              ├→ P1-08 ──────────────────────────────────────→ P1-10 → P1-11
              └→ P1-09 ──────────────────────────────────────┘

P1-10 → P2-01 (leveling)
P1-10 → P2-02 (stats) → P2-03 (classes) → P3-01 (skills)
                              │
                              └→ P3-02 (equipment)
P1-10 → P2-04 (save) → P2-05 (death flow) → P5-01 (backpack)
P1-03 → P4-01 (dungeon gen) → P4-02 (floor caching)
P1-10 → P4-03 (town) → P5-02 (bank)
P5-01 + P3-02 → P5-03 (loot drops)
P5-01 → P5-04 (blacksmith crafting)
```

### Critical Path (longest chain)

```
SETUP-02 → SETUP-04 → SETUP-07 → P1-01 → P1-03 → P1-04 → P1-05 → P1-06 → P1-07 → P1-10
```

**SETUP-02 is the single blocker.** Everything starts there.

### Implementation Priority (Basic Systems First)

**Phase 0 — Project Setup (SETUP):**
Must complete before any code. SETUP-02 unblocks three parallel tracks.

**Phase 1 — Core Loop (P1, priority order):**
1. P1-01 GameState autoload (state management foundation)
2. P1-02 EventBus autoload (signal bus — can parallel with P1-01)
3. P1-03 Tilemap and dungeon room (the world to move in)
4. P1-04 **Player scene and movement** (controls, input, isometric movement — the most basic system)
5. P1-05 Enemy scene and tiers (something to fight)
6. P1-06 Enemy spawning system (populate the dungeon)
7. P1-07 Combat system (hack and slash core)
8. P1-08 HUD overlay (can parallel after P1-01)
9. P1-09 Death screen and restart (can parallel after P1-01)
10. P1-10 Main scene wiring (connect everything)
11. P1-11 Debug overlay

**Phase 2 — Core Systems (P2):**
After P1-10 is done, these can run in parallel:
- **Track A:** P2-01 Leveling → P2-02 Stats → P2-03 Classes
- **Track B:** P2-04 Save/Load → P2-05 Death flow
- **Track C (parallel with P1):** P4-01 Dungeon generation (only needs P1-03)

**Phase 3 — Depth (P3-P5):**
- P3-01 Skill trees, P3-02 Equipment
- P4-02 Floor caching, P4-03 Town hub
- P5-01-04 Inventory, loot, crafting

**Phase 4 — Polish (P6):**
- Visual effects, game feel, audio, art assets, object pooling

### Parallelizable Work

After SETUP-07, three tracks can run simultaneously:
- **Track A:** P1-01 → P1-03 → P1-04 → P1-05 → P1-06 → P1-07 (core gameplay)
- **Track B:** P1-01 → P1-08 (HUD)
- **Track C:** P1-01 → P1-09 (death screen)

After P1-03, dungeon gen can start early:
- **Track D:** P1-03 → P4-01 → P4-02 (procedural floors)

---

## Ticket Details

Full acceptance criteria for each ticket. Organized by epic.

---

### Epic: SPEC — Spec Completion (26/26 DONE)

All 26 spec tickets are complete. Every system has a locked spec document. See the individual doc files for full details. The SPEC epic is closed.

---

### Epic: SETUP — C# Project Setup

> Scaffold the Godot C# project. Requires local environment setup first.

#### SETUP-01: Install development environment

- **Description:** Install .NET 10 SDK, Godot .NET 4.6.2, VS Code C# extensions per [setup-guide.md](architecture/setup-guide.md).
- **Acceptance Criteria:**
  - [x] `dotnet --version` returns 10.0.x
  - [x] `godot --version` returns 4.6.x with .NET suffix
  - [x] VS Code C# Dev Kit extension active
  - [x] Godot shows C# as script language option
- **Status:** Done
- **Deps:** None

#### SETUP-02: Create .csproj and .sln

- **Description:** Let Godot generate the C# project files, then add NuGet references.
- **Acceptance Criteria:**
  - [ ] `DungeonGame.csproj` exists with `Godot.NET.Sdk`, targeting net8.0+
  - [ ] `DungeonGame.sln` exists
  - [ ] NuGet packages added: gdUnit4.api, gdUnit4.test.adapter, xunit, MessagePack, ObjectPool
  - [ ] `dotnet build` succeeds
- **Status:** To Do
- **Deps:** SETUP-01

#### SETUP-03: Remove GUT and GDScript test files

- **Description:** Remove the vendored GUT addon and old GDScript sanity tests.
- **Acceptance Criteria:**
  - [ ] `addons/gut/` directory deleted
  - [ ] `tests/test_project_setup.gd` deleted
  - [ ] `project.godot` no longer references GUT plugin
- **Status:** To Do
- **Deps:** SETUP-02

#### SETUP-04: Update project.godot for .NET

- **Description:** Update project config for C# support and new input map.
- **Acceptance Criteria:**
  - [ ] `config/features` includes `"C#"`
  - [ ] Autoload entries point to `.cs` files
  - [ ] Input map entries present (move_up/down/left/right, restart)
- **Status:** To Do
- **Deps:** SETUP-02

#### SETUP-05: Update Makefile for C#

- **Description:** Replace all gdlint/gdformat/GUT targets with dotnet equivalents.
- **Acceptance Criteria:**
  - [ ] `make build` → `dotnet build`
  - [ ] `make test` → `dotnet test`
  - [ ] `make lint` → `dotnet format --verify-no-changes`
  - [ ] `make format` → `dotnet format`
  - [ ] `make check` → build + lint + test
  - [ ] `make watch` → `dotnet watch test`
  - [ ] `make coverage` → coverage report
  - [ ] `make clean` → wipe bin/, obj/, .godot/
  - [ ] `make setup` → verify tools + configure hooks
- **Status:** To Do
- **Deps:** SETUP-02

#### SETUP-06: Update CI and pre-commit hook

- **Description:** Update GitHub Actions and git hooks for C# tooling.
- **Acceptance Criteria:**
  - [ ] `.github/workflows/ci.yml` uses `setup-dotnet@v4`, runs `dotnet build/format/test`
  - [ ] `.githooks/pre-commit` runs `dotnet format --verify-no-changes` on staged `.cs` files
  - [ ] `.editorconfig` has C# section (4-space indent)
  - [ ] `.gitignore` includes bin/, obj/, *.user, *.DotSettings
- **Status:** To Do
- **Deps:** SETUP-05

#### SETUP-07: Write C# sanity tests

- **Description:** Rewrite the 4 project sanity tests in C# using GdUnit4.
- **Acceptance Criteria:**
  - [ ] `tests/DungeonGame.Tests.csproj` exists
  - [ ] Sanity tests pass: project loads, autoloads registered, input map configured, window settings correct
  - [ ] `dotnet test` passes
- **Status:** To Do
- **Deps:** SETUP-04

---

### Epic: P1 — Prototype Parity

> Reimplement the Phaser prototype's core loop in Godot C#.

#### P1-01: GameState autoload

- **Description:** Implement the GameState singleton — HP, XP, level, floor, death state, signals.
- **Create:** `scripts/autoloads/GameState.cs`
- **Register:** Add to `project.godot` autoloads as `GameState="*res://scripts/autoloads/GameState.cs"`
- **Class:** `public partial class GameState : Node`
- **Signals:**
  - `[Signal] public delegate void StatsChangedEventHandler();`
  - `[Signal] public delegate void PlayerDiedEventHandler();`
- **Properties** (all emit `StatsChanged` on set):
  - `Hp` (int, default 100, clamped to [0, MaxHp], emits `PlayerDied` when <=0 and `!IsDead`)
  - `MaxHp` (int, default 100, formula: `100 + Level * 8`)
  - `Xp` (int, default 0)
  - `Level` (int, default 1)
  - `FloorNumber` (int, default 1)
  - `IsDead` (bool, default false, no signal — internal guard)
- **Methods:**
  - `Reset()` → set `IsDead=false`, then `MaxHp=100, Hp=100, Xp=0, Level=1, FloorNumber=1` (order matters: MaxHp before Hp)
  - `TakeDamage(int amount)` → if `IsDead` return; else `Hp -= amount` (setter handles clamp + death)
  - `AwardXp(int amount)` → `Xp += amount;` then `while (Xp >= Level * 90) { Xp -= Level * 90; Level++; MaxHp = 100 + Level * 8; Hp = Math.Min(MaxHp, Hp + 18); }`
- **Acceptance Criteria:**
  - [ ] All properties emit `StatsChanged` on set
  - [ ] `Hp` clamped to [0, MaxHp], `PlayerDied` fires exactly once per death
  - [ ] `AwardXp` handles multi-level-up via while loop
  - [ ] `Reset()` produces clean initial state (no signals leak from prior run)
  - [ ] 24 unit tests pass (`tests/Unit/GameStateTests.cs`)
- **Spec:** [autoloads.md](architecture/autoloads.md)
- **Status:** To Do
- **Deps:** SETUP-07

#### P1-02: EventBus autoload

- **Description:** Implement the EventBus singleton — pure signal declarations, no logic.
- **Create:** `scripts/autoloads/EventBus.cs`
- **Register:** Add to `project.godot` autoloads as `EventBus="*res://scripts/autoloads/EventBus.cs"` (after GameState)
- **Class:** `public partial class EventBus : Node` (no methods, no state, signals only)
- **Signals:**
  - `[Signal] public delegate void EnemyDefeatedEventHandler(Vector2 position, int tier);` — emitted by Enemy on death, consumed by Dungeon (respawn) + future sound/particles
  - `[Signal] public delegate void EnemySpawnedEventHandler(Node enemy);` — emitted by Dungeon after AddChild, consumed by future minimap
  - `[Signal] public delegate void PlayerAttackedEventHandler(Node target);` — emitted by Player on hit, consumed by future sound/combo
  - `[Signal] public delegate void PlayerDamagedEventHandler(int amount, Node source);` — emitted by Enemy on contact damage, consumed by future damage numbers/sound
- **Acceptance Criteria:**
  - [ ] Script contains only signal declarations — no `_Ready()`, no `_Process()`, no state
  - [ ] Registered as autoload, loads after GameState
  - [ ] All signal delegates have typed parameters
- **Spec:** [autoloads.md](architecture/autoloads.md), [signals.md](architecture/signals.md)
- **Status:** To Do
- **Deps:** SETUP-07

#### P1-03: Tilemap and dungeon room

- **Description:** Static 10x10 isometric room with floor and wall tiles, programmatic TileSet.
- **Create:** `scenes/dungeon/Dungeon.tscn`, `scripts/dungeon/Dungeon.cs`
- **Class:** `public partial class Dungeon : Node2D`
- **Scene tree:** `Dungeon (Node2D)` → `TileMapLayer` + `EntityContainer (Node2D, YSortEnabled=true)` + `SpawnTimer (Timer, WaitTime=2.8, Autostart=true)`
- **TileSet** (created programmatically in `_Ready()`):
  - Tile size: 64×32, shape: diamond, layout: diamond down, offset axis: horizontal
  - Tile 0 (floor): filled diamond, color `#1a2438`, no collision
  - Tile 1 (wall): outlined diamond, color `#2a3a5c`, diamond collision polygon for wall sliding
- **Room layout:** 10×10 grid. Walls on all edges (row 0, row 9, col 0, col 9). Floor interior.
- **Painting:** Loop `for x in 0..9, y in 0..9`: `SetCell(new Vector2I(x, y), 0, isEdge ? wallAtlas : floorAtlas)`
- **Acceptance Criteria:**
  - [ ] Room renders as isometric diamond grid (64×32 tiles)
  - [ ] Walls have collision polygons — player slides along them, doesn't pass through
  - [ ] Floor tiles are walkable, dark blue
  - [ ] EntityContainer has `YSortEnabled = true` for correct draw order
  - [ ] MT-002 passes (isometric floor visible, no gaps, grid aligned)
- **Spec:** [tilemap.md](objects/tilemap.md), [tile-specs.md](assets/tile-specs.md)
- **Status:** To Do
- **Deps:** P1-01

#### P1-04: Player scene and movement

- **Description:** Player character with isometric 8-direction movement and camera follow.
- **Create:** `scenes/player/Player.tscn`, `scripts/player/Player.cs`
- **Class:** `public partial class Player : CharacterBody2D`
- **Scene tree:** `Player (CharacterBody2D)` → `Polygon2D` + `CollisionShape2D (CircleShape2D r=12)` + `AttackRange (Area2D)` → `CollisionShape2D (CircleShape2D r=78)` + `AttackCooldownTimer (Timer, WaitTime=0.42, OneShot=true)` + `Camera2D (Zoom=2x, PositionSmoothingSpeed=5.0, PositionSmoothingEnabled=true)`
- **Polygon2D:** diamond vertices `[(0,-16), (12,0), (0,16), (-12,0)]`, color `#8ed6ff`
- **Collision layers:** Body on layer 2 (player), mask 1 (walls). AttackRange on layer 4 (sensors), mask 3 (enemies).
- **Groups:** Add to `"player"` in `_Ready()`
- **Movement** (`_PhysicsProcess`):
  - `Vector2 input = Input.GetVector("move_left", "move_right", "move_up", "move_down");`
  - Isometric transform: `var isoTransform = new Transform2D(new Vector2(1, 0.5f), new Vector2(-1, 0.5f), Vector2.Zero);`
  - `Velocity = isoTransform * input.Normalized() * 190.0f;`
  - `MoveAndSlide();`
- **Input map entries** (in project.godot): `move_up` (W + Up), `move_down` (S + Down), `move_left` (A + Left), `move_right` (D + Right)
- **Acceptance Criteria:**
  - [ ] Player renders as light blue diamond at room center
  - [ ] WASD/arrows move in isometric directions at 190 px/s
  - [ ] Diagonal speed = cardinal speed (normalized input)
  - [ ] Zero momentum — stops instantly on key release
  - [ ] Camera follows smoothly, 2x zoom
  - [ ] Player slides along walls (no stuck state)
  - [ ] MT-003 to MT-012 pass, 6 movement unit tests pass
- **Spec:** [player.md](objects/player.md), [movement.md](systems/movement.md), [camera.md](systems/camera.md)
- **Status:** To Do
- **Deps:** P1-03

#### P1-05: Enemy scene and tiers

- **Description:** Enemy with 3 danger tiers, chase AI, and contact damage.
- **Create:** `scenes/enemies/Enemy.tscn`, `scripts/enemies/Enemy.cs`
- **Class:** `public partial class Enemy : CharacterBody2D`
- **Scene tree:** `Enemy (CharacterBody2D)` → `Polygon2D` + `CollisionShape2D (CircleShape2D r=10)` + `HitArea (Area2D)` → `CollisionShape2D (CircleShape2D r=15)` + `HitCooldownTimer (Timer, WaitTime=0.7, OneShot=true)`
- **Polygon2D:** diamond vertices `[(0,-14), (10,0), (0,14), (-10,0)]`
- **Collision layers:** Body on layer 3 (enemies), mask 1 (walls). HitArea on layer 5 (damage), mask 2 (player).
- **Groups:** Add to `"enemies"` in `_Ready()`
- **Exports:**
  - `[Export] public int DangerTier { get; set; } = 1;`
- **Tier stats** (computed from `DangerTier` in `_Ready()`):
  - HP: `18 + DangerTier * 12` → tier 1: 30, tier 2: 42, tier 3: 54
  - Speed: `48 + DangerTier * 18` → tier 1: 66, tier 2: 84, tier 3: 102
  - Damage: `3 + DangerTier` → tier 1: 4, tier 2: 5, tier 3: 6
  - XP: `10 + DangerTier * 4` → tier 1: 14, tier 2: 18, tier 3: 22
  - Color: tier 1 `#6bff89` (green), tier 2 `#ffde66` (yellow), tier 3 `#ff6f6f` (red)
- **Chase AI** (`_PhysicsProcess`): straight-line toward `GetTree().GetFirstNodeInGroup("player")`, `Velocity = direction.Normalized() * speed; MoveAndSlide();`
- **Contact damage:** `HitArea.BodyEntered` → deal damage to player via `GameState.TakeDamage(damage)`, start cooldown. `HitCooldownTimer.Timeout` → recheck if player still overlapping, deal again.
- **Death:** when `_hp <= 0` → `EventBus.EmitSignal(SignalName.EnemyDefeated, GlobalPosition, DangerTier); QueueFree();`
- **Acceptance Criteria:**
  - [ ] 3 visually distinct tier colors
  - [ ] Stats match formulas exactly
  - [ ] Chase AI tracks player position every physics frame
  - [ ] Contact damage fires once immediately, then every 0.7s while overlapping
  - [ ] Death emits signal with position+tier before QueueFree
  - [ ] MT-013 to MT-015 pass, 15 enemy unit tests pass
- **Spec:** [enemies.md](objects/enemies.md)
- **Status:** To Do
- **Deps:** P1-04

#### P1-06: Enemy spawning system

- **Description:** Initial spawn, periodic spawn, respawn on kill, soft cap.
- **Modify:** `scripts/dungeon/Dungeon.cs`
- **Enemy scene:** `var enemyScene = GD.Load<PackedScene>("res://scenes/enemies/Enemy.tscn");`
- **Initial spawn** (`_Ready()`): spawn 10 enemies at random edge positions with random tiers (1-3).
- **Periodic spawn** (`SpawnTimer.Timeout` signal, 2.8s loop): if `GetTree().GetNodesInGroup("enemies").Count < 14`, spawn 1 enemy at random edge.
- **Respawn on kill** (`EventBus.EnemyDefeated` signal): start a `SceneTreeTimer` for 1.4s, then spawn 1 enemy at random edge.
- **Edge position calculation:** pick random edge (top/bottom/left/right of tilemap bounds), random position along that edge.
- **Spawn enemy method:** `var enemy = enemyScene.Instantiate<Enemy>(); enemy.DangerTier = GD.RandRange(1, 3); enemy.GlobalPosition = edgePos; EntityContainer.AddChild(enemy);`
- **Soft cap:** Only the periodic timer checks the cap (14). Respawn-on-kill timers always fire (count may briefly exceed 14).
- **Acceptance Criteria:**
  - [ ] Exactly 10 enemies at game start
  - [ ] Periodic timer adds enemies up to soft cap 14
  - [ ] Killed enemies respawn after 1.4s at random edge
  - [ ] New enemies have random tier and immediately chase player
  - [ ] MT-016, MT-017 pass, 5 spawning integration tests pass
- **Spec:** [spawning.md](systems/spawning.md)
- **Status:** To Do
- **Deps:** P1-05

#### P1-07: Combat system

- **Description:** Auto-targeting, attack cooldown, damage, slash effect, XP, level-up.
- **Modify:** `scripts/player/Player.cs`
- **Auto-attack** (`_PhysicsProcess`, after movement):
  - If `AttackCooldownTimer.IsStopped()`: find nearest enemy in `AttackRange.GetOverlappingBodies()` filtered by `"enemies"` group
  - If found: deal damage, spawn slash, emit `EventBus.PlayerAttacked`, restart cooldown timer
- **Damage formula:** `int damage = 12 + (int)(GameState.Level * 1.5f);`
- **Enemy takes damage:** `enemy.TakeDamage(damage)` method on Enemy → decrements `_hp`, if `<=0`: `GameState.AwardXp(xpValue); EventBus.EmitSignal(...EnemyDefeated...); QueueFree();`
- **Slash effect** (spawned dynamically):
  - `var slash = new Polygon2D(); slash.Polygon = new Vector2[] { new(-13,-2), new(13,-2), new(13,2), new(-13,2) };`
  - Color: `new Color(0.961f, 0.784f, 0.420f, 0.95f)` (`#f5c86b` at 95%)
  - Position: enemy's `GlobalPosition`, rotation: `(float)GD.RandRange(-1.2, 1.2)`
  - Tween: fade alpha to 0 + move Y up 8px over 0.12s, then `QueueFree()`
- **Camera shake** (on player damaged):
  - Connect to `GameState.StatsChanged` or use `EventBus.PlayerDamaged`
  - Tween camera offset to random `(+-3, +-3)` then back to `Vector2.Zero` over 0.09s
- **Acceptance Criteria:**
  - [ ] Auto-targets nearest enemy only (not random)
  - [ ] Attack range exactly 78px radius
  - [ ] Cooldown exactly 0.42s
  - [ ] Damage formula matches: 12 + floor(level * 1.5)
  - [ ] Slash appears at enemy position with random rotation, fades in 120ms
  - [ ] Camera shakes on every hit taken (+-3px, 90ms)
  - [ ] XP awarded immediately, level-up triggers mid-combat if threshold reached
  - [ ] MT-018 to MT-027 pass, 8 combat integration tests pass
- **Spec:** [combat.md](systems/combat.md), [effects.md](objects/effects.md)
- **Status:** To Do
- **Deps:** P1-06

#### P1-08: HUD overlay

- **Description:** Stats display panel with reactive signal-based updates.
- **Create:** `scenes/ui/Hud.tscn`, `scripts/ui/Hud.cs`
- **Class:** `public partial class Hud : Control`
- **Scene tree:** `Hud (Control, FullRect)` on CanvasLayer (layer 10) → `PanelContainer` → `VBoxContainer` → `TitleLabel` + `ControlsLabel` + `StatsLabel`
- **Panel style:** `StyleBoxFlat` — bg `new Color(0.086f, 0.106f, 0.157f, 0.75f)`, border `new Color(0.961f, 0.784f, 0.420f, 0.3f)` 1px, corner radius 10px
- **Labels:**
  - Title: "A DUNGEON IN THE MIDDLE OF NOWHERE", color `#f5c86b`, size 13
  - Controls: "Move: WASD / Arrow keys\nAuto-attack: nearest enemy in range", color `#b6bfdb`, size 12
  - Stats: `$"HP: {GameState.Hp} | XP: {GameState.Xp} | LVL: {GameState.Level} | Floor: {GameState.FloorNumber}"`, color `#ecf0ff`, size 12
- **Signal connection** in `_Ready()`: `GameState.StatsChanged += OnStatsChanged;`
- **`OnStatsChanged()`:** update `StatsLabel.Text` with current values
- **MouseFilter:** `MouseFilterEnum.Ignore` on all controls (clicks pass through)
- **Acceptance Criteria:**
  - [ ] Panel visible top-left with correct colors and border
  - [ ] Stats update on same frame as value change (signal-driven)
  - [ ] Mouse clicks pass through HUD to game world
  - [ ] MT-028, MT-029 pass
- **Spec:** [hud.md](ui/hud.md), [ui-theme.md](assets/ui-theme.md)
- **Status:** To Do
- **Deps:** P1-01

#### P1-09: Death screen and restart

- **Description:** Death overlay with restart via R key and button click.
- **Create:** `scenes/ui/DeathScreen.tscn`, `scripts/ui/DeathScreen.cs`
- **Class:** `public partial class DeathScreen : Control`
- **Scene tree:** `DeathScreen (Control, FullRect, ProcessMode=Always, Visible=false)` → `ColorRect (black, 75% alpha, FullRect)` + `VBoxContainer (centered)` → `YouDiedLabel` + `InstructionsLabel` + `RestartButton`
- **ProcessMode = Always:** Critical — this node keeps processing even when tree is paused.
- **Show on death:** connect `GameState.PlayerDied` in `_Ready()` → `Show(); GetTree().Paused = true;`
- **Restart flow:**
  - `RestartButton.Pressed` → `Restart()`
  - `_UnhandledInput`: if `Input.IsActionJustPressed("restart")` and `Visible` → `Restart()`
  - `Restart()`: `Hide(); GetTree().Paused = false; GameState.Reset(); GetTree().ReloadCurrentScene();`
- **Input map:** `restart` action mapped to `Key.R` in project.godot
- **Colors:** "You Died" in `#ffe1b0` (warm gold), instructions in `#b6bfdb` (muted)
- **Acceptance Criteria:**
  - [ ] Death screen appears immediately when HP reaches 0
  - [ ] Game tree paused (enemies stop, no further damage)
  - [ ] R key and button both restart
  - [ ] After restart: HP=100, XP=0, LVL=1, Floor=1, 10 enemies, no lingering effects
  - [ ] MT-030 to MT-033 pass, 6 death/restart integration tests pass
- **Spec:** [death.md](systems/death.md), [death-screen.md](ui/death-screen.md)
- **Status:** To Do
- **Deps:** P1-01

#### P1-10: Main scene wiring

- **Description:** Wire all scenes together, connect signals, configure input map.
- **Create:** `scenes/Main.tscn`, `scripts/Main.cs`
- **Class:** `public partial class Main : Node2D`
- **Scene tree:** `Main (Node2D)` → `Dungeon (instanced)` → `Player (instanced as child of EntityContainer)` + `UILayer (CanvasLayer, layer=10)` → `Hud (instanced)` + `DeathScreen (instanced)`
- **Signal wiring** (in `_Ready()` or via editor connections):
  - `GameState.StatsChanged` → `Hud.OnStatsChanged` (HUD updates)
  - `GameState.PlayerDied` → `DeathScreen.OnPlayerDied` (show death screen)
  - `EventBus.EnemyDefeated` → `Dungeon.OnEnemyDefeated` (schedule respawn)
- **project.godot input map:**
  - `move_up`: W, Up Arrow
  - `move_down`: S, Down Arrow
  - `move_left`: A, Left Arrow
  - `move_right`: D, Right Arrow
  - `restart`: R
- **Main scene setting:** Set `Main.tscn` as the run/main scene in project.godot
- **Acceptance Criteria:**
  - [ ] Game launches with F5 — no errors in output
  - [ ] All systems work together: move, attack, kill, XP, level-up, take damage, die, restart
  - [ ] Signal flow verified: damage → HUD update → death screen → restart → clean state
  - [ ] MT-001 passes (game launch)
  - [ ] Full 33-test manual playthrough passes
- **Spec:** [scene-tree.md](architecture/scene-tree.md)
- **Status:** To Do
- **Deps:** P1-07, P1-08, P1-09

#### P1-11: Debug overlay

- **Description:** Toggleable in-game debug panel showing runtime stats.
- **Acceptance Criteria:**
  - [ ] F3 toggles overlay visibility
  - [ ] Shows: FPS, active entity count, memory usage
  - [ ] CanvasLayer above HUD, disabled in release builds (`#if DEBUG`)
- **Spec:** [test-strategy.md](testing/test-strategy.md)
- **Status:** To Do
- **Deps:** P1-10

---

### Epic: P2 — Core Systems

> Extends prototype with designed progression systems. Blocked on spec completion.

#### P2-01: Leveling redesign

- **Description:** Replace linear XP curve with quadratic, add rested XP, floor-scaling enemy XP.
- **Acceptance Criteria:**
  - [ ] XP threshold = (int)(level^2 * 45)
  - [ ] Enemy XP scales with floor: baseXp * (1 + (floor-1) * 0.5f)
  - [ ] Rested XP: 5% per 8h offline, caps at 1.5 levels
  - [ ] Level-up awards: max HP, HP restore, stat points, skill points
- **Spec:** [leveling.md](systems/leveling.md)
- **Status:** Blocked
- **Deps:** P1-10, SPEC-07

#### P2-02: Stats system

- **Description:** Implement STR/DEX/STA/INT with formula bindings and allocation.
- **Acceptance Criteria:**
  - [ ] 4 stats on GameState with formula-derived combat values
  - [ ] Hybrid allocation: class auto-bonuses + free points on level-up
  - [ ] Soft diminishing returns on stat scaling
- **Spec:** [stats.md](systems/stats.md)
- **Status:** Blocked
- **Deps:** P1-10, SPEC-04

#### P2-03: Class system

- **Description:** One-time class selection with per-class stat bonuses.
- **Acceptance Criteria:**
  - [ ] Class selection UI (Warrior/Ranger/Mage), permanent choice
  - [ ] Each class has distinct stat bonuses per level
  - [ ] Class stored in save data
- **Spec:** [classes.md](systems/classes.md)
- **Status:** Blocked
- **Deps:** P2-02, SPEC-05

#### P2-04: Save/load system

- **Description:** Persistent save with auto-save triggers, export/import, versioning.
- **Acceptance Criteria:**
  - [ ] SaveManager autoload with Save/Load/Export/Import methods
  - [ ] System.Text.Json for player data, MessagePack for floor cache
  - [ ] Auto-save on: level-up, floor change, death, town entry/exit
  - [ ] Base64 export/import for save sharing
  - [ ] Version field with migration support
- **Spec:** [save.md](systems/save.md)
- **Status:** Blocked
- **Deps:** P1-10, SPEC-10

#### P2-05: Full death flow

- **Description:** Multi-step death UI with penalties, mitigations, and gold buyout.
- **Acceptance Criteria:**
  - [ ] UI flow: choose destination -> toggle mitigations -> review -> confirm
  - [ ] EXP loss: floor * 0.4% (capped 50%)
  - [ ] Backpack loss: floor/10 + 1 items
  - [ ] Gold buyout: floor*15 (EXP), floor*25 (backpack)
  - [ ] Sacrificial Idol consumable negates backpack loss
- **Spec:** [death.md](systems/death.md), [death-screen.md](ui/death-screen.md)
- **Status:** Blocked
- **Deps:** P2-04, SPEC-06

---

### Epic: P3 — Skills & Equipment

> Blocked on spec completion.

#### P3-01: Skill tree system

- **Description:** Per-class hierarchical skill trees with hybrid leveling.
- **Acceptance Criteria:**
  - [ ] Skill tree UI shows Category -> Base Skill -> Specific Skill
  - [ ] Hybrid leveling: use-based XP + point allocation
  - [ ] Infinite scaling with diminishing returns
  - [ ] Passive bonus calculations affect combat stats
- **Spec:** [skills.md](systems/skills.md)
- **Status:** Blocked
- **Deps:** P2-03, SPEC-08

#### P3-02: Equipment system

- **Description:** Equipment slots with class restrictions and stat effects.
- **Acceptance Criteria:**
  - [ ] 9 equipment slots (head, body, neck, rings, arms, legs, feet, hands, ammo)
  - [ ] Class-restricted gear (Warrior: heavy, Ranger: light, Mage: robes)
  - [ ] Equipped items modify stats
- **Spec:** [items.md](inventory/items.md)
- **Status:** Blocked
- **Deps:** P2-02, SPEC-06

---

### Epic: P4 — World

> Blocked on spec completion.

#### P4-01: Procedural dungeon generation

- **Description:** Generate dungeon floors procedurally on a background thread.
- **Acceptance Criteria:**
  - [ ] Generation algorithm produces varied room layouts
  - [ ] Seeded generation (System.Random) — same seed = same floor
  - [ ] Runs on background thread via System.Threading.Channels
  - [ ] Generation time < 500ms for largest floors
- **Spec:** [dungeon.md](world/dungeon.md)
- **Status:** Blocked
- **Deps:** P1-03, SPEC-09

#### P4-02: Floor caching and descent

- **Description:** Cache up to 10 floors, staircase traversal, pre-generation.
- **Acceptance Criteria:**
  - [ ] 10-floor LRU cache using MessagePack binary serialization
  - [ ] Staircases connect floors (descent/ascent)
  - [ ] Adjacent floors pre-generated on background thread
  - [ ] Floor-specific enemy tier weighting
- **Spec:** [dungeon.md](world/dungeon.md)
- **Status:** Blocked
- **Deps:** P4-01

#### P4-03: Town hub

- **Description:** Safe scene with NPCs and scene transitions.
- **Acceptance Criteria:**
  - [ ] Town scene: safe zone, no enemies, no combat
  - [ ] NPCs: Item Shop, Blacksmith, Adventure Guild, Level Teleporter, Banker
  - [ ] Scene transition: dungeon <-> town (seamless)
- **Spec:** [town.md](world/town.md)
- **Status:** Blocked
- **Deps:** P1-10, SPEC-01

---

### Epic: P5 — Inventory & Items

> Blocked on spec completion.

#### P5-01: Backpack system

- **Description:** 25-slot risky carry storage with death-loss mechanic.
- **Acceptance Criteria:**
  - [ ] 25-slot grid UI with drag/drop or click interaction
  - [ ] Items at risk on death (floor/10 + 1 items lost)
  - [ ] Item pickup from ground drops
- **Spec:** [backpack.md](inventory/backpack.md)
- **Status:** Blocked
- **Deps:** P2-05, SPEC-11

#### P5-02: Bank system

- **Description:** 15-slot safe town storage.
- **Acceptance Criteria:**
  - [ ] 15-slot grid UI, town-only access
  - [ ] Deposit/withdraw interaction
- **Spec:** [bank.md](inventory/bank.md)
- **Status:** Blocked
- **Deps:** P4-03, SPEC-11

#### P5-03: Loot and item drops

- **Description:** Item data model, loot drops, color-coded by level-relative gradient.
- **Acceptance Criteria:**
  - [ ] Item data: type, tier, stats, class restrictions
  - [ ] Loot drops from enemies based on drop tables
  - [ ] Items color-coded by level-relative gradient
- **Spec:** [items.md](inventory/items.md)
- **Status:** Blocked
- **Deps:** P3-02, SPEC-06

#### P5-04: Blacksmith crafting

- **Description:** Recycling system — break items into materials, no magical drops.
- **Acceptance Criteria:**
  - [ ] Break items into materials at Blacksmith NPC
  - [ ] Crafting recipes produce equipment
  - [ ] No magical item drops — all special gear is crafted
- **Spec:** `docs/systems/crafting.md`
- **Status:** Blocked
- **Deps:** P5-01, SPEC-02

---

### Epic: P6 — Polish & Juice

#### P6-01: Visual effects

- **Description:** Hit flash, death particles, level-up glow, damage numbers.
- **Acceptance Criteria:**
  - [ ] Enemy flashes white on hit
  - [ ] Death particles spawn on enemy kill
  - [ ] Level-up glow effect on player
  - [ ] Floating damage numbers on hit
- **Spec:** [effects.md](objects/effects.md), [player-engagement.md](systems/player-engagement.md)
- **Status:** To Do
- **Deps:** P1-07

#### P6-02: Game feel (hitstop, knockback)

- **Description:** Hitstop, knockback on impact.
- **Acceptance Criteria:**
  - [ ] Brief hitstop (frame pause) on attack landing
  - [ ] Enemies knocked back slightly on hit
- **Spec:** [player-engagement.md](systems/player-engagement.md)
- **Status:** To Do
- **Deps:** P1-07

#### P6-03: Object pooling

- **Description:** Pool enemies and effects to avoid GC pressure.
- **Acceptance Criteria:**
  - [ ] Enemy instances rented from pool, returned on "death" instead of QueueFree
  - [ ] Slash effects pooled
  - [ ] No GC spikes during gameplay (verified via profiler)
- **Status:** To Do
- **Deps:** P1-10

#### P6-04: Art assets

- **Description:** Replace Polygon2D placeholders with pixel art sprites.
- **Acceptance Criteria:**
  - [ ] Player, enemy (3 tiers), and slash have sprite animations
  - [ ] All assets are CC0 or CC-BY, tracked in ATTRIBUTION.md
  - [ ] AnimatedSprite2D replaces Polygon2D nodes
- **Status:** To Do
- **Deps:** P1-10

#### P6-05: Audio

- **Description:** Sound effects and background music.
- **Acceptance Criteria:**
  - [ ] Attack, hit, death, level-up, UI click sound effects
  - [ ] Dungeon ambient music loop
  - [ ] Town ambient music loop
  - [ ] All audio CC0 or CC-BY, tracked in ATTRIBUTION.md
- **Status:** To Do
- **Deps:** P1-10

#### P6-06: Test coverage tooling

- **Description:** Coverage reports via coverlet + ReportGenerator.
- **Acceptance Criteria:**
  - [ ] `make coverage` generates HTML coverage report in `coverage/`
  - [ ] CI uploads coverage artifact
- **Status:** To Do
- **Deps:** SETUP-07
