# A Dungeon in the Middle of Nowhere

**Engine:** Godot 4.x (.NET edition) with C#
**Perspective:** Isometric 2D (Diablo 1 style, 2:1 diamond tiles)
**Platform:** Desktop native (macOS primary, Windows/Linux supported)
**Status:** Fresh start — all specs locked, all code deleted, rebuilding with visual-first development

## What is this?

**A Dungeon in the Middle of Nowhere** is a persistent, never-ending real-time action dungeon crawler inspired by Diablo 1's atmosphere, loot chase, and town hub feel.

You control a **single permanent character** (Warrior, Ranger, or Mage) that grows stronger across all sessions — there are no rerolls. The dungeon descends infinitely with escalating difficulty, endless monster respawns, and a living dungeon entity that feeds on adventurers who die within it.

Death hurts (the dungeon eats your experience), but it's not permadeath. Gold buyout mitigates penalties, Sacrificial Idols protect your inventory, and revival is always an option — because the dungeon *wants* you to come back and grow stronger before it harvests you again.

### Core Features

- **3 classes** — Warrior (melee bruiser), Ranger (agile marksman), Mage (thought-based spellcaster)
- **Unified magic system** — all skills run on magicules (natural magic particles). Warriors enhance muscles, Rangers imbue weapons, Mages manifest elements from thought
- **Infinite progression** — no level cap, no skill cap, diminishing returns but never zero growth
- **Living dungeon** — the dungeon is an intelligent entity that attracts, fattens, and harvests adventurers
- **Meaningful death** — XP loss scales with depth, gold buyout, inventory risk, revival negotiation
- **Town hub** — safe zone with NPCs (Shop, Blacksmith, Adventure Guild, Teleporter, Banker)
- **Procedural floors** — seeded generation, background threaded, 10-floor cache
- **Spell scroll osmosis** — Mages learn spells through repeated scroll use (knowledge retention)

## Documentation

All game design lives in [`docs/`](docs/). Architecture and AI context in [AGENTS.md](AGENTS.md).

| Folder | Contents |
|--------|----------|
| [architecture/](docs/architecture/) | Tech stack, project structure, autoloads, signals, setup guide |
| [systems/](docs/systems/) | Stats, classes, skills, magic, combat, leveling, death, save, color system |
| [objects/](docs/objects/) | Player, enemies, tilemap, effects |
| [world/](docs/world/) | Dungeon (living entity lore), town, monsters |
| [inventory/](docs/inventory/) | Backpack, bank, items |
| [ui/](docs/ui/) | Controls, HUD, death screen |
| [testing/](docs/testing/) | Test strategy, 33 manual tests, automated test plans (GdUnit4 + xUnit) |
| [conventions/](docs/conventions/) | Team structure, ticketing |
| [teams/](docs/teams/) | Per-team ticket boards (Design, QA, DevOps, Engine, Systems, UI, World) |
| [reference/](docs/reference/) | Subagent research, technical references |

Development progress: [**dev-tracker.md**](docs/dev-tracker.md) (41 tickets across 6 phases)

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Engine | Godot 4.x (.NET edition) |
| Language | C# / .NET 8+ |
| Testing | GdUnit4 (scene tests) + xUnit (logic tests) |
| Serialization | System.Text.Json (saves) + MessagePack (floor cache) |
| Object pooling | Microsoft.Extensions.ObjectPool |
| Async generation | System.Threading.Channels |

## How to Run

> The game is in a fresh-start rebuild phase. All code was deleted (see dev-journal Session 8). Visual-first reimplementation begins from the locked specs.

When implementation begins:

1. Install [.NET 9+ SDK](https://dotnet.microsoft.com/download)
2. Install [Godot 4.x .NET edition](https://godotengine.org/download) (the .NET build, not standard)
3. Clone the repo
4. `dotnet restore` to install NuGet packages
5. Open the project in Godot editor → Press F5 to run

See [setup guide](docs/architecture/setup-guide.md) for detailed environment setup.

## Development

All development is AI-assisted. The product owner directs game vision; AI teams handle implementation.

```bash
make build      # dotnet build
make test       # dotnet test (GdUnit4 + xUnit)
make lint       # dotnet format --verify-no-changes
make check      # build + lint + test
make run        # Launch in Godot
```

7 AI team leads are defined in `.claude/agents/` for specialized work (Design, QA, DevOps, Engine, Systems, UI, World). See [team conventions](docs/conventions/teams.md).

## Contributing

Solo learning project — no formal contributions, but feel free to fork, play, and open issues with feedback.

Made with curiosity in Los Angeles, 2026.
