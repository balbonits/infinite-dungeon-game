---
name: ISS Tile Grid Standard
description: Isometric Stone Soup tileset is the game's environment art standard — 64x32 floors, 64x64 walls, CC0 license
type: project
---

Isometric Stone Soup (ISS) by Screaming Brain Studios (CC0) is the primary environment tileset. Adopted 2026-04-09.

- Floor tiles: 64x32 (isometric diamond, 2:1 ratio)
- Wall blocks: 64x64 (isometric cube, full/half variants + top-face overlays)
- TileMap tile size: 64x32
- Magenta (#FF00FF) = transparency key
- 43 wall theme sheets, 49 floor theme sheets, 3 torch sprites
- Located at: assets/isometric/tiles/stone-soup/
- Supersedes cave_atlas.png (1024x1024 irregular atlas)
- Zone visual theme assignments (which ISS sheet per zone) are TBD

**Why:** The game needed a standardized isometric tile grid. ISS provides a massive CC0 library that fits the dungeon crawler aesthetic and establishes concrete dimensions for all future environment art.

**How to apply:** Any spec referencing tile sizes, dungeon visuals, or environment art should use 64x32 / 64x64 as the canonical dimensions. The authoritative reference is docs/assets/sprite-specs.md "Tile Grid Standard" section.
