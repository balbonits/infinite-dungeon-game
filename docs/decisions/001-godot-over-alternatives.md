# ADR-001: Godot 4 + C# Over Alternatives

**Status:** Accepted
**Date:** 2026-04-08

**Context:** The project needed a game engine for a 2D isometric dungeon crawler. The game began as a single-file Phaser 3 browser prototype (~450 lines of vanilla JavaScript). The rebuild required a real engine capable of isometric rendering, physics, scene management, and native desktop performance. Candidates considered: Unity, Unreal, Godot 4, and staying with web frameworks (Phaser 3, PixiJS).

**Decision:** Godot 4 with C# (.NET 8+) as the engine and language. Specifically the .NET edition of Godot (not the standard GDScript-only build).

Key factors:
- **Free and open source** -- no licensing fees, no revenue share, no vendor lock-in
- **C# over GDScript** -- strong typing, mature ecosystem, NuGet packages, familiar to the AI dev team, and enables xUnit testing outside the engine
- **Desktop native** -- the game targets macOS primarily with Windows/Linux support; C# web export is not supported in Godot 4.x, which is acceptable since the project moved away from browser deployment
- **Lightweight** -- Godot's binary is small, startup is fast, and 2D performance is excellent for an isometric tile-based game
- **Scene/node architecture** -- composable scenes replace the single-file monolith of the Phaser prototype

**Consequences:**
- The project requires the Godot .NET edition specifically (the standard Godot binary does not support C#)
- All game logic can be written as pure C# and tested with xUnit without launching the engine, enabling fast test-driven development
- Web export is not possible -- the game is desktop-only
- The AI dev team can use standard C# tooling (dotnet CLI, NuGet, VS Code + C# Dev Kit) for building, testing, and formatting
- GdUnit4 is available for Godot scene-level integration tests alongside xUnit for pure logic tests
