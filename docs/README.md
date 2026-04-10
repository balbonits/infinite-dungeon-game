# Documentation Index

Master navigation for all project documentation. 60+ files across 11 directories.

---

## For AI Sessions (read first)

| Doc | Purpose |
| --- | --- |
| [conventions/code.md](conventions/code.md) | Code patterns, naming, quality standards, performance |
| [conventions/agile.md](conventions/agile.md) | Dev process, tickets, scope discipline, docs-first |
| [conventions/teams.md](conventions/teams.md) | AI team structure (7 leads) and domain ownership |
| [conventions/ai-workflow.md](conventions/ai-workflow.md) | Dev ticket cycle: branch, research, plan, test, code, verify |

## Game Design Specs (all locked)

| Directory | Contents |
| --- | --- |
| [systems/](systems/) | Combat, leveling, stats, classes, skills, magic, death, save, movement, spawning, camera, color system, player engagement |
| [objects/](objects/) | Player, enemies, tilemap, visual effects |
| [world/](world/) | Dungeon generation, town hub, monster types |
| [inventory/](inventory/) | Backpack, bank, item system |
| [ui/](ui/) | Controls, HUD, death screen |
| [assets/](assets/) | Tile specs, sprite specs, UI theme |

## Technical Architecture

| Doc | Purpose |
| --- | --- |
| [architecture/tech-stack.md](architecture/tech-stack.md) | Godot 4 + C# stack, window config, what we avoid |
| [architecture/project-structure.md](architecture/project-structure.md) | Directory layout, file naming, organization |
| [architecture/scene-tree.md](architecture/scene-tree.md) | Complete node hierarchy for all 6 scenes |
| [architecture/autoloads.md](architecture/autoloads.md) | GameState + EventBus singleton design |
| [architecture/signals.md](architecture/signals.md) | Signal registry (9 signals), flow diagrams |
| [architecture/setup-guide.md](architecture/setup-guide.md) | .NET SDK, Godot .NET, VS Code setup |
| [architecture/entity-framework.md](architecture/entity-framework.md) | Unified entity data model + 6 gameplay systems |
| [architecture/analytics.md](architecture/analytics.md) | Opt-in telemetry, bug reporting, feedback |

## Reference and Learning

| Doc | Purpose |
| --- | --- |
| [reference/godot-basics.md](reference/godot-basics.md) | Godot 4 concepts for web developers |
| [reference/game-dev-concepts.md](reference/game-dev-concepts.md) | Game dev fundamentals mapped to web dev |
| [reference/game-development.md](reference/game-development.md) | Research journal: accumulated learnings |
| [reference/subagent-research.md](reference/subagent-research.md) | AI agent design and coordination research |

## Testing

| Doc | Purpose |
| --- | --- |
| [testing/test-strategy.md](testing/test-strategy.md) | Testing approach (manual + GdUnit4 + xUnit) |
| [testing/manual-tests.md](testing/manual-tests.md) | 33 manual test cases |
| [testing/automated-tests.md](testing/automated-tests.md) | GdUnit4 + xUnit automated test specs |

## Tracking

| Doc | Purpose |
| --- | --- |
| [dev-tracker.md](dev-tracker.md) | Master ticket list, dependency graph, priority tiers |
| [dev-journal.md](dev-journal.md) | Running session log (append-only) |
| [overview.md](overview.md) | Project vision and design philosophy |

## Testing Pipeline

### Universal Test Runner

Any test scene can be run in 4 modes via the universal runner:

```
make t S=<scene> [F=--headless|--capture|--check]
```

| Flag | Mode | What It Does |
| --- | --- | --- |
| *(none)* | Windowed | Launch scene, watch it run |
| `--headless` | Headless | Console output, auto-quits, CI-ready |
| `--capture` | Capture | Screenshots at timed intervals + video recording |
| `--check` | Regression | Headless + crash detection + evidence check |

**Examples:**
```
make t S=test-game                    # watch game loop
make t S=test-game F=--headless       # CI: 60 assertions
make t S=test-hero F=--capture        # capture hero screenshots
make t S=test-dungeon F=--check       # regression check
```

### Test Commands

| Command | Type | What It Does |
| --- | --- | --- |
| `make test` | Unit | 480 xUnit tests (<1s, no Godot needed) |
| `make test-game` | Integration | Windowed full game loop (16 phases, watchable) |
| `make test-game-headless` | Integration (CI) | Headless game loop (60 assertions, auto-quits) |
| `make test-game-capture` | Evidence | Screenshots + video of game loop |
| `make test-game-check` | Regression | Headless + unit + evidence verification |
| `make test-all` | Full Suite | Unit tests + headless game loop |

### Game Loop Test (`test-game`)

The full game loop E2E test exercises 16 phases:

1. Init (reset, set player) → 2. Town shopping (buy potions) → 3. Enter dungeon (generate floor 1) → 4. Spawn enemies (rarity, modifiers) → 5. Combat (crit, elemental) → 6. Level check → 7. Floor transition (floor 2) → 8. Save → 9. Load → 10. Return to town → 11. Bank ops → 12. Backpack expand → 13. Town save → 14. Reload → 15. Systems validation (elemental, crit, monster AI, spawner) → 16. Summary

Systems tested: GameState, GameSystems, DungeonGenerator, SaveSerializer, BankSystem, BackpackSystem, ElementalCombat, CritSystem, MonsterBehavior, MonsterSpawner, MonsterModifiers, ItemGenerator.

## Visual Test Scenes

Run `make help` to see all targets, or use the category runners below.

**Category Runners:**

| Command | What it launches |
| --- | --- |
| `make test-visual` | Everything below (all visual tests) |
| `make test-creatures` | Creature browser (Up/Down to switch between all 8) |
| `make test-characters` | Hero equipment viewer |
| `make test-env` | All 10 environment viewers |
| `make test-ui-all` | All 2 UI viewers |

**Creatures:**

| Command | Asset |
| --- | --- |
| `make test-slime` | Slime sprite sheet |
| `make test-skeleton` | Skeleton sprite sheet |
| `make test-goblin` | Goblin sprite sheet |
| `make test-zombie` | Zombie sprite sheet |
| `make test-ogre` | Ogre sprite sheet |
| `make test-werewolf` | Werewolf sprite sheet |
| `make test-elemental` | Elemental sprite sheet |
| `make test-magician` | Magician sprite sheet |

**Characters:**

| Command | Asset |
| --- | --- |
| `make test-hero` | Hero/heroine with toggleable equipment layers |

**Environment:**

| Command | Asset |
| --- | --- |
| `make test-floors` | ISS floor themes (49 themes, Left/Right to cycle) |
| `make test-walls` | ISS wall blocks + animated torch (22 themes) |
| `make test-doors` | Doorways & passages (closed, open, archway) |
| `make test-tilemap` | Isometric tilemap rendering |
| `make test-crates` | SBS crate sprite sheets (64x64) |
| `make test-roads` | SBS road + pathway tiles (128x64) |
| `make test-water` | SBS water + autotile transitions (128x64) |
| `make test-objects` | SBS objects (stairs, copings, temple kit) |
| `make test-town` | SBS town building + roof tiles |
| `make test-items` | Dungeon items and objects (crates, stairs, copings, temple) |

**UI:**

| Command | Asset |
| --- | --- |
| `make test-ui` | HUD elements, HP/MP orbs |
| `make test-buttons` | Button sprites (round, square, states, arrows) |

**Effects:**

| Command | Asset |
| --- | --- |
| `make test-combat` | Combat effects (slash, damage, hit/die) |

**Entity Framework:**

| Scene | Purpose |
| --- | --- |
| `scenes/tests/test_entity.tscn` | Entity framework visual test (EntityData + systems) |

**Dungeon Generation:**

| Command | What it shows |
| --- | --- |
| `make test-dungeon` | Full proc gen pipeline (BSP + corridors + smoothing + automap) |
| `make test-bsp` | BSP room partitioning |
| `make test-drunkard` | Drunkard's walk corridor generation |
| `make test-cellular` | Cellular automata smoothing |
| `make test-procgen` | All proc gen scenes at once |

**Game Scenes:**

| Scene | Purpose |
| --- | --- |
| `scenes/ui/MainMenu.tscn` | Main menu (New Game / Load / Exit) |
| `scenes/ui/CharacterCreate.tscn` | Character creation (name entry) |
| `scenes/Town.tscn` | Town hub (5 NPCs, shop, dungeon entrance) |
| `scenes/Dungeon.tscn` | Dungeon gameplay (combat, floor transitions) |
| `scenes/tests/test_town2.tscn` | Town test scene (standalone, raw keys) |
| `scenes/tests/test_game.tscn` | Full game loop E2E test (16 phases, 60 assertions) |

## Supporting

| Directory | Contents |
| --- | --- |
| [evidence/](evidence/) | Proof-of-concept screenshots and notes |
| [teams/](teams/) | Per-team ticket tracking files |
