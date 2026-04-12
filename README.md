<p align="center">
  <img src="icon.png" alt="A Dungeon in the Middle of Nowhere" width="128" />
</p>

<h1 align="center">A Dungeon in the Middle of Nowhere</h1>

<p align="center">
  A persistent, never-ending real-time action dungeon crawler built with <strong>Godot 4 + C#</strong>.<br/>
  Inspired by Diablo 1's atmosphere, loot chase, and town hub — reimagined as an infinite descent into a living dungeon that feeds on adventurers.
</p>

<p align="center">
  <strong>Status:</strong> In active development. Playable prototype with full game loop.
</p>

## The Game

You control a single permanent character — **Warrior**, **Ranger**, or **Mage** — that grows stronger across all sessions. There are no rerolls. The dungeon descends infinitely with escalating difficulty, and the dungeon itself is a living entity that wants you to grow strong before it harvests you.

**Core features:**

- 3 classes with 80+ skills across melee, ranged, and magic
- Infinite progression — no level cap, diminishing returns but never zero growth
- Living dungeon — an intelligent entity that adapts to your play style
- Meaningful death — XP loss, inventory risk, gold buyout, revival negotiation
- Town hub with 5 NPCs — Shop, Blacksmith, Guild, Teleporter, Banker
- Procedural floors with zone-based themes and difficulty scaling
- Diablo-style HUD — HP/MP orbs, skill hotbar, XP progress bar
- Endgame systems — Dungeon Pacts, Zone Saturation, Magicule Attunement

## Screenshots

*Coming soon*

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Engine | Godot 4.6 (.NET edition) |
| Language | C# / .NET 8+ |
| Renderer | GL Compatibility |
| Art | PixelLab AI + Screaming Brain Studios (CC0) |
| Platform | macOS (Apple Silicon + Intel), Windows, Linux |

## Building from Source

### Requirements

- [.NET 8+ SDK](https://dotnet.microsoft.com/download)
- [Godot 4.6 .NET edition](https://godotengine.org/download)

### Run

```bash
# Build
dotnet build

# Run in Godot
/path/to/Godot --path .

# Export (macOS)
# Open Godot editor → Project → Export → macOS → Export Project
```

### Make targets

```bash
make build      # dotnet build
make run        # Build and launch
make test       # Run tests
make lint       # Format check
make clean      # Remove build artifacts
```

## Project Structure

```
scripts/
├── autoloads/          # GameState, EventBus, SaveManager
├── logic/              # Pure C# game logic (no Godot dependency)
│   ├── SkillDatabase.cs, SkillDef.cs, SkillBar.cs
│   ├── DungeonPacts.cs, ZoneSaturation.cs, DungeonIntelligence.cs
│   ├── MagiculeAttunement.cs, DepthGearTier.cs
│   ├── Inventory.cs, Item.cs, Bank.cs, Crafting.cs
│   └── SaveData.cs, SaveSystem.cs
├── ui/                 # All UI (GameWindow, TabBar, ScrollList, etc.)
└── *.cs                # Scene scripts (Player, Enemy, Dungeon, Town)

docs/                   # 80+ design docs, specs, and architecture
assets/                 # Sprites, tiles, projectiles, UI art
scenes/                 # Godot .tscn scene files
```

## Documentation

Extensive game design documentation lives in [`docs/`](docs/):

- **Game systems** — combat, leveling, stats, skills, magic, death, save
- **World design** — dungeon lore, town, monsters, zone themes
- **Architecture** — autoloads, signals, entity framework, tech stack
- **Endgame** — pacts, attunement, zone saturation, dungeon intelligence

## License

**Proprietary.** See [LICENSE](LICENSE) for terms.

This repository is public for viewing and educational purposes. The source code and original game assets may not be used in commercial products or redistributed. Third-party assets retain their original licenses — see [CREDITS.md](CREDITS.md).

## Credits

See [CREDITS.md](CREDITS.md) for full attribution of third-party assets and tools.

---

Made in Los Angeles, 2026.
