# Development Backlog

Single source of truth for all work. Organized as tickets within epics. Specs are completed before implementation — no code until the relevant spec is locked.

## How to Use

- **Status:** `To Do` | `In Progress` | `Done` | `Blocked`
- **Ticket ID:** `EPIC-NUMBER` (e.g., `SPEC-01`, `P1-03`)
- **Rules:** Spec tickets (`SPEC-*`) gate implementation. `Blocked` = dependency not `Done`. One ticket = one commit.

---

## Ticket Index

Quick reference for all 44 tickets. Search by ID, title, or status.

### SPEC — Spec Completion (26 sub-tickets)

All handled by `@design-lead`, reviewed by `@qa-lead`.

| ID | Title | Status | Deps | Doc |
|----|-------|--------|------|-----|
| SPEC-01a | Town layout and scene flow | To Do | — | [town.md](world/town.md) |
| SPEC-01b | Item Shop NPC spec | To Do | SPEC-01a | [town.md](world/town.md) |
| SPEC-01c | Blacksmith NPC spec | To Do | SPEC-01a, SPEC-02 | [town.md](world/town.md) |
| SPEC-01d | Adventure Guild NPC spec | To Do | SPEC-01a | [town.md](world/town.md) |
| SPEC-01e | Level Teleporter NPC spec | To Do | SPEC-01a | [town.md](world/town.md) |
| SPEC-01f | Banker NPC spec | To Do | SPEC-01a | [town.md](world/town.md) |
| SPEC-02 | Crafting/blacksmith system | To Do | SPEC-06 | *new: crafting.md* |
| SPEC-03 | Mage spell acquisition | To Do | SPEC-05 | *new: spell-acquisition.md* |
| SPEC-04a | STR → melee damage formula | To Do | — | [stats.md](systems/stats.md) |
| SPEC-04b | DEX → attack speed / evasion formula | To Do | — | [stats.md](systems/stats.md) |
| SPEC-04c | STA → HP / defense formula | To Do | — | [stats.md](systems/stats.md) |
| SPEC-04d | INT → magic power formula | To Do | — | [stats.md](systems/stats.md) |
| SPEC-04e | Stat diminishing returns curve | To Do | SPEC-04a–d | [stats.md](systems/stats.md) |
| SPEC-05a | Warrior per-level bonuses | To Do | SPEC-04a–d | [classes.md](systems/classes.md) |
| SPEC-05b | Ranger per-level bonuses | To Do | SPEC-04a–d | [classes.md](systems/classes.md) |
| SPEC-05c | Mage per-level bonuses | To Do | SPEC-04a–d | [classes.md](systems/classes.md) |
| SPEC-05d | Free stat point allocation rules | To Do | SPEC-05a–c | [classes.md](systems/classes.md) |
| SPEC-06a | Item data model (types, tiers, fields) | To Do | SPEC-04e | [items.md](inventory/items.md) |
| SPEC-06b | Loot drop rates and tables | To Do | SPEC-06a | [items.md](inventory/items.md) |
| SPEC-06c | Equipment slot stat effects | To Do | SPEC-06a | [items.md](inventory/items.md) |
| SPEC-07a | XP curve formula (lock constant) | To Do | — | [leveling.md](systems/leveling.md) |
| SPEC-07b | Floor-scaling enemy XP multiplier | To Do | SPEC-07a | [leveling.md](systems/leveling.md) |
| SPEC-07c | Rested XP rules and display | To Do | — | [leveling.md](systems/leveling.md) |
| SPEC-07d | Level-up milestone rewards | To Do | SPEC-07a | [leveling.md](systems/leveling.md) |
| SPEC-08 | Lock skill system spec | To Do | SPEC-04e, SPEC-05d | [skills.md](systems/skills.md) |
| SPEC-09a | Dungeon generation algorithm | To Do | — | [dungeon.md](world/dungeon.md) |
| SPEC-09b | Floor difficulty scaling | To Do | SPEC-09a | [dungeon.md](world/dungeon.md) |
| SPEC-09c | Special room types (boss, treasure, safe) | To Do | SPEC-09a | [dungeon.md](world/dungeon.md) |
| SPEC-10 | Lock save system spec | To Do | SPEC-09a | [save.md](systems/save.md) |
| SPEC-11a | Backpack UI and death-loss logic | To Do | SPEC-06a | [backpack.md](inventory/backpack.md) |
| SPEC-11b | Bank UI and deposit/withdraw flow | To Do | SPEC-06a | [bank.md](inventory/bank.md) |
| SPEC-12 | Finalize autoloads spec | Done | — | [autoloads.md](architecture/autoloads.md) |

### SETUP — C# Project Setup (7 tickets)

All handled by `@devops-lead`.

| ID | Title | Status | Deps |
|----|-------|--------|------|
| SETUP-01 | Install dev environment | To Do | — |
| SETUP-02 | Create .csproj and .sln | To Do | SETUP-01 |
| SETUP-03 | Remove GUT and GDScript tests | To Do | SETUP-02 |
| SETUP-04 | Update project.godot for .NET | To Do | SETUP-02 |
| SETUP-05 | Update Makefile for C# | To Do | SETUP-02 |
| SETUP-06 | Update CI and pre-commit hook | To Do | SETUP-05 |
| SETUP-07 | Write C# sanity tests | To Do | SETUP-04 |

### P1 — Prototype Parity (11 tickets)

`@systems-lead` (logic), `@engine-lead` (scenes), `@ui-lead` (HUD/death), `@qa-lead` (tests).

| ID | Title | Status | Deps | Spec | Tests |
|----|-------|--------|------|------|-------|
| P1-01 | GameState autoload | To Do | SETUP-07 | [autoloads](architecture/autoloads.md) | 24 unit |
| P1-02 | EventBus autoload | To Do | SETUP-07 | [autoloads](architecture/autoloads.md), [signals](architecture/signals.md) | — |
| P1-03 | Tilemap and dungeon room | To Do | P1-01 | [tilemap](objects/tilemap.md) | MT-002 |
| P1-04 | Player scene and movement | To Do | P1-03 | [player](objects/player.md), [movement](systems/movement.md) | MT-003–012, 6 unit |
| P1-05 | Enemy scene and tiers | To Do | P1-04 | [enemies](objects/enemies.md) | MT-013–015, 15 unit |
| P1-06 | Enemy spawning system | To Do | P1-05 | [spawning](systems/spawning.md) | MT-016–017, 5 integ |
| P1-07 | Combat system | To Do | P1-06 | [combat](systems/combat.md), [effects](objects/effects.md) | MT-018–027, 8 integ |
| P1-08 | HUD overlay | To Do | P1-01 | [hud](ui/hud.md), [ui-theme](assets/ui-theme.md) | MT-028–029 |
| P1-09 | Death screen and restart | To Do | P1-01 | [death](systems/death.md), [death-screen](ui/death-screen.md) | MT-030–033, 6 integ |
| P1-10 | Main scene wiring | To Do | P1-07, 08, 09 | [scene-tree](architecture/scene-tree.md) | MT-001, full 33 manual |
| P1-11 | Debug overlay | To Do | P1-10 | [test-strategy](testing/test-strategy.md) | — |

### P2 — Core Systems (5 tickets)

| ID | Title | Status | Deps | Spec |
|----|-------|--------|------|------|
| P2-01 | Leveling redesign | Blocked | P1-10, SPEC-07 | [leveling](systems/leveling.md) |
| P2-02 | Stats system | Blocked | P1-10, SPEC-04 | [stats](systems/stats.md) |
| P2-03 | Class system | Blocked | P2-02, SPEC-05 | [classes](systems/classes.md) |
| P2-04 | Save/load system | Blocked | P1-10, SPEC-10 | [save](systems/save.md) |
| P2-05 | Full death flow | Blocked | P2-04, SPEC-06 | [death](systems/death.md) |

### P3 — Skills & Equipment (2 tickets)

| ID | Title | Status | Deps | Spec |
|----|-------|--------|------|------|
| P3-01 | Skill tree system | Blocked | P2-03, SPEC-08 | [skills](systems/skills.md) |
| P3-02 | Equipment system | Blocked | P2-02, SPEC-06 | [items](inventory/items.md) |

### P4 — World (3 tickets)

| ID | Title | Status | Deps | Spec |
|----|-------|--------|------|------|
| P4-01 | Procedural dungeon generation | Blocked | P1-03, SPEC-09 | [dungeon](world/dungeon.md) |
| P4-02 | Floor caching and descent | Blocked | P4-01 | [dungeon](world/dungeon.md) |
| P4-03 | Town hub | Blocked | P1-10, SPEC-01 | [town](world/town.md) |

### P5 — Inventory & Items (4 tickets)

| ID | Title | Status | Deps | Spec |
|----|-------|--------|------|------|
| P5-01 | Backpack system | Blocked | P2-05, SPEC-11 | [backpack](inventory/backpack.md) |
| P5-02 | Bank system | Blocked | P4-03, SPEC-11 | [bank](inventory/bank.md) |
| P5-03 | Loot and item drops | Blocked | P3-02, SPEC-06 | [items](inventory/items.md) |
| P5-04 | Blacksmith crafting | Blocked | P5-01, SPEC-02 | *new doc* |

### P6 — Polish & Juice (6 tickets)

| ID | Title | Status | Deps | Spec |
|----|-------|--------|------|------|
| P6-01 | Visual effects | To Do | P1-07 | [effects](objects/effects.md) |
| P6-02 | Game feel (hitstop, knockback) | To Do | P1-07 | [player-engagement](systems/player-engagement.md) |
| P6-03 | Object pooling | To Do | P1-10 | — |
| P6-04 | Art assets | To Do | P1-10 | [sprite-specs](assets/sprite-specs.md) |
| P6-05 | Audio | To Do | P1-10 | — |
| P6-06 | Test coverage tooling | To Do | SETUP-07 | — |

---

## Dependency Graph

```
SPEC-04 ──→ SPEC-05 ──→ SPEC-08
  │            │
  ├──→ SPEC-06 ──→ SPEC-02
  │      │         SPEC-11
  │      │
  v      v
SPEC-07  SPEC-03
SPEC-09 ──→ SPEC-10

SETUP-01 → SETUP-02 → SETUP-03
                │
                ├→ SETUP-04 → SETUP-07
                └→ SETUP-05 → SETUP-06

SETUP-07 → P1-01 → P1-03 → P1-04 → P1-05 → P1-06 → P1-07 ─┐
              │                                                │
              ├→ P1-08 ──────────────────────────────────────→ P1-10 → P1-11
              └→ P1-09 ──────────────────────────────────────┘

P1-10 → P2-01 → (needs SPEC-07)
P1-10 → P2-02 → P2-03 → P3-01 (needs SPEC-08)
                    │
                    └→ P3-02 (needs SPEC-06)
P1-10 → P2-04 → P2-05 → P5-01
P1-03 → P4-01 → P4-02
P1-10 → P4-03 → P5-02
```

### Critical Path (longest chain)

```
SETUP-01 → SETUP-02 → SETUP-04 → SETUP-07 → P1-01 → P1-03 → P1-04 → P1-05 → P1-06 → P1-07 → P1-10 → P2-02 → P2-03 → P3-01
```

### Parallelizable Work

After SETUP-07 is done, these can run in parallel:

- **Track A:** P1-01 → P1-03 → P1-04 → P1-05 → P1-06 → P1-07 (core gameplay chain)
- **Track B:** P1-01 → P1-08 (HUD — independent after autoloads)
- **Track C:** P1-01 → P1-09 (death screen — independent after autoloads)
- **Track D:** All SPEC tickets (can be worked on anytime, in parallel with setup/P1)

---

## Ticket Details

Full acceptance criteria for each ticket. Organized by epic.

---

### Epic: SPEC — Spec Completion

> Complete all design docs before any implementation begins. This epic gates everything else.

#### SPEC-01a: Town layout and scene flow

- **Type:** spec | **Team:** Design
- **Description:** Define the town as a safe zone — physical layout, how the player enters/exits, what the player sees when they arrive.
- **Doc:** `docs/world/town.md`
- **Decisions Needed:**
  - [ ] How big is the town? (single screen, scrollable, multiple areas?)
  - [ ] How does the player enter from the dungeon? (portal, stairs, fade transition?)
  - [ ] What's the visual feel? (cozy refuge vs bustling hub?)
- **Acceptance Criteria:**
  - [ ] Town layout described (where NPCs stand, where the entrance/exit is)
  - [ ] Scene transition design (dungeon ↔ town) documented
  - [ ] No open questions remain
- **Status:** To Do
- **Deps:** None

#### SPEC-01b: Item Shop NPC spec

- **Type:** spec | **Team:** Design
- **Description:** What does the Item Shop sell? How does buying/selling work?
- **Doc:** `docs/world/town.md` (NPC section)
- **Decisions Needed:**
  - [ ] What items are for sale? (consumables, basic gear, scrolls?)
  - [ ] How are prices determined? (fixed, scaled by level, dynamic?)
  - [ ] Can the player sell items back? At what rate?
- **Acceptance Criteria:**
  - [ ] Shop inventory rules defined
  - [ ] Pricing formula documented
  - [ ] Buy/sell UI flow described
- **Status:** To Do
- **Deps:** SPEC-01a

#### SPEC-01c: Blacksmith NPC spec

- **Type:** spec | **Team:** Design
- **Description:** What does the Blacksmith do? Crafting, recycling, upgrades?
- **Doc:** `docs/world/town.md` (NPC section)
- **Decisions Needed:**
  - [ ] What services does the Blacksmith offer? (recycle items, craft gear, upgrade?)
  - [ ] How does the player interact? (menu, dialogue, drag-drop?)
- **Acceptance Criteria:**
  - [ ] Blacksmith services listed with interaction flow
  - [ ] Links to SPEC-02 (crafting system) for detail
- **Status:** To Do
- **Deps:** SPEC-01a, SPEC-02

#### SPEC-01d: Adventure Guild NPC spec

- **Type:** spec | **Team:** Design
- **Description:** What does the Adventure Guild offer? Quests, bounties, leaderboards?
- **Doc:** `docs/world/town.md` (NPC section)
- **Decisions Needed:**
  - [ ] Does the guild give quests/bounties? Or just provide info?
  - [ ] Is there a leaderboard or achievement display?
- **Acceptance Criteria:**
  - [ ] Guild purpose and services documented
  - [ ] Interaction flow described
- **Status:** To Do
- **Deps:** SPEC-01a

#### SPEC-01e: Level Teleporter NPC spec

- **Type:** spec | **Team:** Design
- **Description:** How does the teleporter work? Can you skip to previously reached floors?
- **Doc:** `docs/world/town.md` (NPC section)
- **Decisions Needed:**
  - [ ] Can you teleport to any previously visited floor, or only milestones (every 5/10)?
  - [ ] Does teleporting cost gold?
  - [ ] Is there a cooldown?
- **Acceptance Criteria:**
  - [ ] Teleport rules documented (which floors, cost, cooldown)
  - [ ] Interaction flow described
- **Status:** To Do
- **Deps:** SPEC-01a

#### SPEC-01f: Banker NPC spec

- **Type:** spec | **Team:** Design
- **Description:** How does banking work beyond the basic bank spec?
- **Doc:** `docs/world/town.md` (NPC section)
- **Decisions Needed:**
  - [ ] Does the banker offer slot expansion? At what cost?
  - [ ] Any gold storage? Interest?
- **Acceptance Criteria:**
  - [ ] Banker services documented
  - [ ] Links to SPEC-11b (bank UI) for detail
- **Status:** To Do
- **Deps:** SPEC-01a

#### SPEC-02: Crafting/blacksmith system

- **Type:** spec | **Team:** Design
- **Description:** How does the recycling/crafting system work? Break items into materials, craft new gear. No magical drops — all special gear is crafted.
- **Doc:** *new:* `docs/systems/crafting.md`
- **Decisions Needed:**
  - [ ] What materials exist? (ore, cloth, leather, gems?)
  - [ ] How many materials does recycling yield? (based on item tier?)
  - [ ] What recipes exist? (one material type per gear slot? Complex recipes?)
  - [ ] Is crafting instant or does it take time?
- **Acceptance Criteria:**
  - [ ] Material types defined
  - [ ] Recycling formula (item tier → material yield)
  - [ ] Recipe system rules
  - [ ] Crafting UI flow
- **Status:** To Do
- **Deps:** SPEC-06a

#### SPEC-03: Mage spell acquisition

- **Type:** spec | **Team:** Design
- **Description:** How do Mages learn spells? Scroll osmosis system — find scrolls, study them, unlock spells.
- **Doc:** *new:* `docs/systems/spell-acquisition.md`
- **Decisions Needed:**
  - [ ] How does a Mage "learn" from a scroll? (use it X times? Study for duration? Instant?)
  - [ ] Are scrolls consumed or reusable?
  - [ ] How many spells can a Mage know at once?
  - [ ] How does this interact with the skill tree?
- **Acceptance Criteria:**
  - [ ] Scroll types and rarity defined
  - [ ] Learning process documented
  - [ ] Spell slot/limit rules
  - [ ] Skill tree interaction documented
- **Status:** To Do
- **Deps:** SPEC-05d

#### SPEC-04a: STR → melee damage formula

- **Type:** spec | **Team:** Design
- **Description:** How much extra melee damage does each point of STR give? This is the Warrior's primary stat.
- **Doc:** `docs/systems/stats.md`
- **Context:** STR should feel impactful for Warriors but still useful for other classes. Each point should make attacks hit noticeably harder, especially early on.
- **Decisions Needed:**
  - [ ] How much base damage does 1 STR add? (flat bonus? percentage?)
  - [ ] Does STR also affect anything else? (carry weight, knockback, shield block?)
- **Acceptance Criteria:**
  - [ ] Formula written: `melee_damage = base + STR * X` with exact X value
  - [ ] Example values at STR 10, 50, 100
- **Status:** To Do
- **Deps:** None

#### SPEC-04b: DEX → attack speed / evasion formula

- **Type:** spec | **Team:** Design
- **Description:** What does DEX do for the player? This is the Ranger's primary stat.
- **Doc:** `docs/systems/stats.md`
- **Context:** DEX should make the Ranger feel fast and agile. Could affect attack speed, evasion chance, or both.
- **Decisions Needed:**
  - [ ] Does DEX increase attack speed? (reduce cooldown? increase attack rate?)
  - [ ] Does DEX add evasion/dodge chance?
  - [ ] Does DEX affect ranged damage?
- **Acceptance Criteria:**
  - [ ] Formula(s) written with exact values
  - [ ] Example values at DEX 10, 50, 100
- **Status:** To Do
- **Deps:** None

#### SPEC-04c: STA → HP / defense formula

- **Type:** spec | **Team:** Design
- **Description:** How does STA affect survivability? Applies to all classes but especially Warrior.
- **Doc:** `docs/systems/stats.md`
- **Context:** Currently max_hp = 100 + level * 8. STA should add on top of that. The player should feel tankier with more STA.
- **Decisions Needed:**
  - [ ] How much HP does 1 STA add?
  - [ ] Does STA also reduce incoming damage? (flat reduction? percentage?)
  - [ ] Does STA affect HP regeneration?
- **Acceptance Criteria:**
  - [ ] HP formula updated: `max_hp = base + level * X + STA * Y`
  - [ ] Any damage reduction formula
  - [ ] Example values at STA 10, 50, 100
- **Status:** To Do
- **Deps:** None

#### SPEC-04d: INT → magic power formula

- **Type:** spec | **Team:** Design
- **Description:** What does INT do? This is the Mage's primary stat.
- **Doc:** `docs/systems/stats.md`
- **Context:** INT should make spells hit harder and feel more powerful. Could also affect mana/cooldowns if those exist.
- **Decisions Needed:**
  - [ ] Does INT increase spell damage? By how much?
  - [ ] Does INT affect spell cooldowns?
  - [ ] Is there a mana system, and does INT affect max mana?
- **Acceptance Criteria:**
  - [ ] Formula(s) written with exact values
  - [ ] Example values at INT 10, 50, 100
- **Status:** To Do
- **Deps:** None

#### SPEC-04e: Stat diminishing returns curve

- **Type:** spec | **Team:** Design
- **Description:** How do stats scale at high levels? Should 100 STR be twice as good as 50 STR, or less?
- **Doc:** `docs/systems/stats.md`
- **Context:** Without diminishing returns, stats scale infinitely and balance breaks. With too much diminishing, high-level investment feels bad. Need a curve that rewards investment but doesn't explode.
- **Decisions Needed:**
  - [ ] Soft cap or logarithmic curve? (e.g., `effective = stat * (1 - stat/(stat + K))`)
  - [ ] At what point do returns start diminishing noticeably?
  - [ ] Should the curve be the same for all stats?
- **Acceptance Criteria:**
  - [ ] Diminishing returns formula with exact constants
  - [ ] Graph-friendly example: effective value at 10, 25, 50, 100, 200, 500 raw stat
  - [ ] Applied consistently to SPEC-04a through 04d formulas
- **Status:** To Do
- **Deps:** SPEC-04a, SPEC-04b, SPEC-04c, SPEC-04d

#### SPEC-05a: Warrior per-level bonuses

- **Type:** spec | **Team:** Design
- **Description:** How many bonus stat points does a Warrior get per level, and in which stats?
- **Doc:** `docs/systems/classes.md`
- **Context:** Warriors favor STR and STA. They should feel tough and hit hard compared to other classes.
- **Acceptance Criteria:**
  - [ ] Bonus points per level defined (e.g., +2 STR, +1 STA per level)
  - [ ] Values feel distinct from Ranger and Mage
- **Status:** To Do
- **Deps:** SPEC-04a, SPEC-04c

#### SPEC-05b: Ranger per-level bonuses

- **Type:** spec | **Team:** Design
- **Description:** How many bonus stat points does a Ranger get per level?
- **Doc:** `docs/systems/classes.md`
- **Context:** Rangers favor DEX. They should feel fast and evasive.
- **Acceptance Criteria:**
  - [ ] Bonus points per level defined
  - [ ] Values feel distinct from Warrior and Mage
- **Status:** To Do
- **Deps:** SPEC-04b

#### SPEC-05c: Mage per-level bonuses

- **Type:** spec | **Team:** Design
- **Description:** How many bonus stat points does a Mage get per level?
- **Doc:** `docs/systems/classes.md`
- **Context:** Mages favor INT. They should feel powerful but fragile.
- **Acceptance Criteria:**
  - [ ] Bonus points per level defined
  - [ ] Values feel distinct from Warrior and Ranger
- **Status:** To Do
- **Deps:** SPEC-04d

#### SPEC-05d: Free stat point allocation rules

- **Type:** spec | **Team:** Design
- **Description:** How many free points does the player get per level to spend however they want?
- **Doc:** `docs/systems/classes.md`
- **Context:** Free points let players customize their build beyond the class default. Too many = classes feel the same. Too few = no player choice.
- **Decisions Needed:**
  - [ ] How many free points per level? (1? 2? 3?)
  - [ ] Can free points go into any stat, or only non-primary?
  - [ ] Can the player respec? (cost? limit?)
- **Acceptance Criteria:**
  - [ ] Free points per level defined
  - [ ] Allocation rules documented
  - [ ] Respec rules (if any) documented
- **Status:** To Do
- **Deps:** SPEC-05a, SPEC-05b, SPEC-05c

#### SPEC-06a: Item data model

- **Type:** spec | **Team:** Design
- **Description:** What fields does an item have? Types, tiers, rarity, stats, restrictions.
- **Doc:** `docs/inventory/items.md`
- **Decisions Needed:**
  - [ ] What item types exist? (weapon, armor, consumable, scroll, material, quest?)
  - [ ] How many tiers/rarity levels? (common, uncommon, rare, epic, legendary?)
  - [ ] What stats can an item have? (STR bonus, damage, defense, speed?)
  - [ ] Class restrictions per item type?
- **Acceptance Criteria:**
  - [ ] Item data structure defined (all fields with types)
  - [ ] Tier/rarity list with color mapping (using color-system.md gradient)
  - [ ] Class restriction rules
- **Status:** To Do
- **Deps:** SPEC-04e

#### SPEC-06b: Loot drop rates and tables

- **Type:** spec | **Team:** Design
- **Description:** How often do enemies drop items? What determines what drops?
- **Doc:** `docs/inventory/items.md`
- **Decisions Needed:**
  - [ ] Base drop chance per enemy kill? (10%? 25%? Always?)
  - [ ] Does drop chance scale with enemy tier or floor depth?
  - [ ] How is the dropped item's tier determined?
- **Acceptance Criteria:**
  - [ ] Drop chance formula
  - [ ] Loot table structure (how tier/type is selected)
  - [ ] Example drop rates at floor 1, 10, 50
- **Status:** To Do
- **Deps:** SPEC-06a

#### SPEC-06c: Equipment slot stat effects

- **Type:** spec | **Team:** Design
- **Description:** How do equipped items modify the player's stats?
- **Doc:** `docs/inventory/items.md`
- **Decisions Needed:**
  - [ ] Do items give flat stat bonuses? Percentage? Both?
  - [ ] Can items have multiple stat bonuses?
  - [ ] Are there set bonuses (wearing matching items)?
- **Acceptance Criteria:**
  - [ ] Stat modification rules documented
  - [ ] Example items at different tiers with their stat effects
- **Status:** To Do
- **Deps:** SPEC-06a

#### SPEC-07a: XP curve formula

- **Type:** spec | **Team:** Design
- **Description:** How much XP does each level require? Lock the exact formula constant.
- **Doc:** `docs/systems/leveling.md`
- **Context:** Current proposal is `floor(level^2 * 45)`. Need to verify this feels right at all levels.
- **Acceptance Criteria:**
  - [ ] XP formula locked with exact constant
  - [ ] Table showing XP required at levels 1, 5, 10, 25, 50, 100
  - [ ] Feels right: early levels are quick, later levels are a satisfying grind (not punishing)
- **Status:** To Do
- **Deps:** None

#### SPEC-07b: Floor-scaling enemy XP multiplier

- **Type:** spec | **Team:** Design
- **Description:** How much more XP do deeper-floor enemies give?
- **Doc:** `docs/systems/leveling.md`
- **Context:** Current proposal: `baseXp * (1 + (floor-1) * 0.5)`. Deeper floors should reward more XP so pushing deeper always feels worthwhile.
- **Acceptance Criteria:**
  - [ ] Multiplier formula locked
  - [ ] Table showing XP per tier at floors 1, 5, 10, 25, 50
  - [ ] Deeper floors always give meaningfully more XP
- **Status:** To Do
- **Deps:** SPEC-07a

#### SPEC-07c: Rested XP rules and display

- **Type:** spec | **Team:** Design
- **Description:** How does the rested XP bonus work for players who take breaks?
- **Doc:** `docs/systems/leveling.md`
- **Context:** Proposal: 5% bonus per 8h offline, caps at 1.5 levels worth. Rewards returning players without punishing daily players.
- **Decisions Needed:**
  - [ ] How is rested XP shown to the player? (bar color change? icon? tooltip?)
  - [ ] Does rested XP double gains or add a flat bonus?
- **Acceptance Criteria:**
  - [ ] Rested XP accumulation formula locked
  - [ ] Cap defined
  - [ ] Display/UI indicator described
- **Status:** To Do
- **Deps:** None

#### SPEC-07d: Level-up milestone rewards

- **Type:** spec | **Team:** Design
- **Description:** What special rewards happen at milestone levels (10, 25, 50, etc.)?
- **Doc:** `docs/systems/leveling.md`
- **Decisions Needed:**
  - [ ] Which levels are milestones?
  - [ ] What do milestones give? (extra skill points? cosmetic unlock? title?)
- **Acceptance Criteria:**
  - [ ] Milestone levels listed
  - [ ] Reward per milestone defined
- **Status:** To Do
- **Deps:** SPEC-07a

#### SPEC-08: Lock skill system spec

- **Type:** spec | **Team:** Design
- **Description:** Finalize passive bonus values per level, diminishing returns, unlock progression for all class skill trees.
- **Doc:** `docs/systems/skills.md`
- **Decisions Needed:**
  - [ ] Exact bonus per skill level (e.g., "Sword Mastery level 5 = +X% melee damage")
  - [ ] Diminishing returns on skill levels (same curve as stats? different?)
  - [ ] Unlock all skills at base level 1, or unlock progressively?
- **Acceptance Criteria:**
  - [ ] Every base and specific skill has concrete per-level values
  - [ ] Skill diminishing returns formula defined
  - [ ] Unlock progression rules defined
- **Status:** To Do
- **Deps:** SPEC-04e, SPEC-05d

#### SPEC-09a: Dungeon generation algorithm

- **Type:** spec | **Team:** Design
- **Description:** How are dungeon floors built? What algorithm generates the rooms and corridors?
- **Doc:** `docs/world/dungeon.md`
- **Decisions Needed:**
  - [ ] BSP tree, random walk, cellular automata, or room placement?
  - [ ] How big are floors? (grid size, room count)
  - [ ] How are rooms connected? (corridors, doors, open passages?)
- **Acceptance Criteria:**
  - [ ] Algorithm chosen with reasoning
  - [ ] Floor size parameters defined
  - [ ] Room/corridor generation rules documented
- **Status:** To Do
- **Deps:** None

#### SPEC-09b: Floor difficulty scaling

- **Type:** spec | **Team:** Design
- **Description:** How does difficulty increase as the player goes deeper?
- **Doc:** `docs/world/dungeon.md`
- **Decisions Needed:**
  - [ ] How does enemy tier distribution change per floor?
  - [ ] Do floors get physically larger or more complex?
  - [ ] Are there difficulty "plateaus" (e.g., floors 1-5 feel similar, then 6-10 step up)?
- **Acceptance Criteria:**
  - [ ] Tier distribution formula per floor depth
  - [ ] Floor complexity scaling rules
  - [ ] Difficulty curve feels like steady escalation (not sudden walls)
- **Status:** To Do
- **Deps:** SPEC-09a

#### SPEC-09c: Special room types

- **Type:** spec | **Team:** Design
- **Description:** What special rooms exist? Boss rooms, treasure rooms, safe spots?
- **Doc:** `docs/world/dungeon.md`
- **Decisions Needed:**
  - [ ] How often do boss rooms appear? (every 5 floors? 10?)
  - [ ] What makes a boss room different? (bigger, tougher enemy, unique loot?)
  - [ ] Are there treasure rooms? Rest areas? Hidden rooms?
- **Acceptance Criteria:**
  - [ ] Each special room type defined with frequency and rules
  - [ ] Placement constraints documented (boss rooms far from stairs, etc.)
- **Status:** To Do
- **Deps:** SPEC-09a

#### SPEC-10: Lock save system spec

- **Type:** spec | **Team:** Design
- **Description:** Resolve remaining save questions — slots, backup rotation, floor cache size.
- **Doc:** `docs/systems/save.md`
- **Decisions Needed:**
  - [ ] How many save slots? (3? 5? unlimited?)
  - [ ] Backup rotation? (keep last N saves? one backup?)
  - [ ] How big is a saved floor in MB? (estimate based on tile data + enemy state)
- **Acceptance Criteria:**
  - [ ] Slot count locked
  - [ ] Backup strategy defined
  - [ ] Floor cache size estimated (10 floors × X MB)
  - [ ] All open questions resolved
- **Status:** To Do
- **Deps:** SPEC-09a

#### SPEC-11a: Backpack UI and death-loss logic

- **Type:** spec | **Team:** Design
- **Description:** How does the backpack look and how does death-item-loss selection work?
- **Doc:** `docs/inventory/backpack.md`
- **Decisions Needed:**
  - [ ] Grid layout? (5×5? scrollable list?)
  - [ ] When items are lost on death, which items? (random? cheapest? player chooses?)
  - [ ] How does the player interact? (click to use? drag to equip? right-click menu?)
- **Acceptance Criteria:**
  - [ ] UI layout described (wireframe-level)
  - [ ] Death-loss selection logic defined
  - [ ] Interaction flow documented
- **Status:** To Do
- **Deps:** SPEC-06a

#### SPEC-11b: Bank UI and deposit/withdraw flow

- **Type:** spec | **Team:** Design
- **Description:** How does the bank storage UI work?
- **Doc:** `docs/inventory/bank.md`
- **Decisions Needed:**
  - [ ] Same grid layout as backpack?
  - [ ] How does deposit/withdraw work? (drag between backpack and bank? click to transfer?)
  - [ ] Can bank slots be expanded? How? (gold? gameplay milestone?)
- **Acceptance Criteria:**
  - [ ] UI layout described
  - [ ] Deposit/withdraw interaction flow
  - [ ] Slot expansion rules (if any)
- **Status:** To Do
- **Deps:** SPEC-06a

#### SPEC-12: Finalize autoloads spec

- **Description:** Complete C# pseudocode conversion, all open questions already resolved.
- **Acceptance Criteria:**
  - [x] `docs/architecture/autoloads.md` has complete C# pseudocode
  - [x] Resolved questions section documents all 5 decisions
- **Status:** Done
- **Deps:** None

---

### Epic: SETUP — C# Project Setup

> Scaffold the Godot C# project. Requires local environment setup first.

#### SETUP-01: Install development environment

- **Description:** Install .NET 9 SDK, Godot .NET edition, VS Code C# extensions per [setup-guide.md](architecture/setup-guide.md).
- **Acceptance Criteria:**
  - [ ] `dotnet --version` returns 9.0.x
  - [ ] `godot --version` returns 4.6.x with .mono suffix
  - [ ] VS Code C# Dev Kit extension active
  - [ ] Godot shows C# as script language option
- **Status:** To Do
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
