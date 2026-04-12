<p align="center">
  <img src="../icon.png" alt="A Dungeon in the Middle of Nowhere" width="96" />
</p>

# Documentation Index

Master navigation for all project documentation. 80+ files across 11 directories.

---

## Game Dev Fundamentals (read before coding)

| Doc | Purpose |
| --- | --- |
| [basics/README.md](basics/README.md) | 22 learning docs: sprites, collision, tilemap, UI, camera, game feel, state machines, ARPG design, audio, save, pathfinding, shaders, Godot patterns, and more |

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
| [architecture/project-structure.md](architecture/project-structure.md) | Target directory layout, file naming, organization |
| [architecture/scene-tree.md](architecture/scene-tree.md) | Target node hierarchy for all scenes |
| [architecture/autoloads.md](architecture/autoloads.md) | GameState + EventBus singleton design |
| [architecture/signals.md](architecture/signals.md) | Signal registry, flow diagrams |
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

> No test infrastructure currently exists. The docs below describe the target testing strategy and serve as acceptance criteria for reimplementation.

| Doc | Purpose |
| --- | --- |
| [testing/test-strategy.md](testing/test-strategy.md) | Testing approach (manual + GdUnit4 + xUnit) |
| [testing/manual-tests.md](testing/manual-tests.md) | 33 manual test cases (acceptance criteria) |
| [testing/automated-tests.md](testing/automated-tests.md) | GdUnit4 + xUnit automated test specs |

## Tracking

| Doc | Purpose |
| --- | --- |
| [dev-tracker.md](dev-tracker.md) | Master ticket list, dependency graph, priority tiers |
| [dev-journal.md](dev-journal.md) | Running session log (append-only) |
| [overview.md](overview.md) | Project vision and design philosophy |

## Make Targets

Run `make help` to see all available targets. Core targets:

| Command | What It Does |
| --- | --- |
| `make build` | Build C# project (dotnet build) |
| `make run` | Build and launch the game |
| `make import` | Run Godot import (after adding new assets/scenes) |
| `make test` | Run unit tests (when tests exist) |
| `make doctor` | Check dev environment health |
| `make clean` | Remove build artifacts |

Visual test targets will be added as scenes are created during visual-first development.

## Supporting

| Directory | Contents |
| --- | --- |
| [evidence/](evidence/) | Proof-of-concept screenshots and notes |
| [teams/](teams/) | Per-team ticket tracking files |
