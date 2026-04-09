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

## Visual Test Scenes

Run `make help` to see all targets, or use the category runners below.

**Category Runners:**

| Command | What it launches |
| --- | --- |
| `make test-visual` | Everything below (all visual tests) |
| `make test-creatures` | All 8 creature viewers |
| `make test-characters` | Hero equipment viewer |
| `make test-env` | All 9 environment viewers |
| `make test-ui-all` | All 3 UI viewers |

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

**UI:**

| Command | Asset |
| --- | --- |
| `make test-ui` | HUD elements, HP/MP orbs |
| `make test-buttons` | Button sprites (round, square, states, arrows) |
| `make test-icons` | UI icons (weapons, potions, coins, gear) |

**Effects:**

| Command | Asset |
| --- | --- |
| `make test-combat` | Combat effects (slash, damage, hit/die) |

## Supporting

| Directory | Contents |
| --- | --- |
| [evidence/](evidence/) | Proof-of-concept screenshots and notes |
| [teams/](teams/) | Per-team ticket tracking files |
