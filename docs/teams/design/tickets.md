# Design Team — Ticket Board

**Lead:** `@design-lead` | **Model:** Opus | **Domain:** Game specs, formulas, balance, player experience

All SPEC-* tickets. This team writes and locks spec docs in `docs/`.

## Active Queue

Tickets with no blockers — ready to work on now.

| ID | Title | Status | Doc |
|----|-------|--------|-----|
| SPEC-04a | STR → melee damage formula | To Do | [stats.md](../../systems/stats.md) |
| SPEC-04b | DEX → attack speed / evasion formula | To Do | [stats.md](../../systems/stats.md) |
| SPEC-04c | STA → HP / defense formula | To Do | [stats.md](../../systems/stats.md) |
| SPEC-04d | INT → magic power formula | To Do | [stats.md](../../systems/stats.md) |
| SPEC-07a | XP curve formula (lock constant) | To Do | [leveling.md](../../systems/leveling.md) |
| SPEC-07c | Rested XP rules and display | To Do | [leveling.md](../../systems/leveling.md) |
| SPEC-09a | Dungeon generation algorithm | To Do | [dungeon.md](../../world/dungeon.md) |
| SPEC-01a | Town layout and scene flow | To Do | [town.md](../../world/town.md) |

## Blocked Queue

Waiting on other tickets to finish first.

| ID | Title | Status | Blocked By |
|----|-------|--------|------------|
| SPEC-04e | Stat diminishing returns curve | Blocked | SPEC-04a–d |
| SPEC-05a | Warrior per-level bonuses | Blocked | SPEC-04a, 04c |
| SPEC-05b | Ranger per-level bonuses | Blocked | SPEC-04b |
| SPEC-05c | Mage per-level bonuses | Blocked | SPEC-04d |
| SPEC-05d | Free stat point allocation rules | Blocked | SPEC-05a–c |
| SPEC-06a | Item data model | Blocked | SPEC-04e |
| SPEC-06b | Loot drop rates and tables | Blocked | SPEC-06a |
| SPEC-06c | Equipment slot stat effects | Blocked | SPEC-06a |
| SPEC-07b | Floor-scaling enemy XP multiplier | Blocked | SPEC-07a |
| SPEC-07d | Level-up milestone rewards | Blocked | SPEC-07a |
| SPEC-08 | Lock skill system spec | Blocked | SPEC-04e, SPEC-05d |
| SPEC-09b | Floor difficulty scaling | Blocked | SPEC-09a |
| SPEC-09c | Special room types | Blocked | SPEC-09a |
| SPEC-10 | Lock save system spec | Blocked | SPEC-09a |
| SPEC-01b | Item Shop NPC spec | Blocked | SPEC-01a |
| SPEC-01c | Blacksmith NPC spec | Blocked | SPEC-01a, SPEC-02 |
| SPEC-01d | Adventure Guild NPC spec | Blocked | SPEC-01a |
| SPEC-01e | Level Teleporter NPC spec | Blocked | SPEC-01a |
| SPEC-01f | Banker NPC spec | Blocked | SPEC-01a |
| SPEC-02 | Crafting/blacksmith system | Blocked | SPEC-06a |
| SPEC-03 | Mage spell acquisition | Blocked | SPEC-05d |
| SPEC-11a | Backpack UI and death-loss logic | Blocked | SPEC-06a |
| SPEC-11b | Bank UI and deposit/withdraw flow | Blocked | SPEC-06a |

## Done

| ID | Title |
|----|-------|
| SPEC-12 | Finalize autoloads spec |
