# ADR-004: ISS Tileset as Grid Standard

**Status:** Accepted
**Date:** 2026-04-09

**Context:** The game uses an isometric 2D perspective and needed a consistent visual language for the dungeon world. Multiple free tile sources existed (DCSS, rubberduck, Dragosha, various OpenGameArt packs), but mixing art styles created visual inconsistency. The project also needed a locked grid specification that all systems -- rendering, collision, pathfinding, proc gen -- could rely on.

**Decision:** Isometric Stone Soup (ISS) by Screaming Brain Studios is the sole tile source and grid standard. All isometric environment art comes from SBS asset packs (CC0 license). The ISS grid dimensions define the game's spatial unit:

- Floor tiles: 64x32 pixels (isometric diamond)
- Wall blocks: 64x64 pixels (isometric cube with vertical face)
- Magenta (#FF00FF) is the transparency key used by all ISS sprites

14 SBS packs were integrated (819+ game assets): floors, walls, roads, water, doorways, crates, town buildings, objects, buttons, and the ISS tile toolkit.

**Consequences:**
- One visual language across the entire game -- style consistency over asset variety
- All systems can hardcode the 64x32 / 64x64 grid dimensions, simplifying rendering, collision, and generation code
- TileMap requires two layers: FloorLayer (64x32 tiles) and WallLayer (64x64 blocks) for correct isometric depth sorting
- ISS magenta backgrounds must be stripped to alpha at load time (test scenes use pixel scanning; production will pre-process at build time)
- Non-SBS character art (Clint Bellanger creatures, Dragosha NPCs) is kept because SBS does not provide character sprites, but all environment and UI art is SBS-only
- Adding new environment art means finding or requesting it from SBS, not mixing in other sources
