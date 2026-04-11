# Assets

All visual game assets. Generated via PixelLab AI — no external art, no placeholders.

## Structure

```
assets/
├── characters/          — All entity sprites (player, enemies, NPCs)
│   ├── player/          — Playable character classes
│   ├── enemies/         — Monster species
│   └── npcs/            — Town NPC characters
├── tiles/               — Isometric tiles for floor/wall rendering
│   ├── dungeon/         — Dungeon floor, wall, stairs tiles
│   └── town/            — Town floor and wall tiles
└── objects/             — Map objects (future: chests, decorations)
```

## Art Style

- **Perspective:** Low top-down isometric
- **Character canvas:** 92x92px (all characters, 8 directional rotations)
- **Tile canvas:** 64x64px (isometric diamond or block)
- **Outline:** Single color black outline (characters), selective outline (tiles)
- **Texture filter:** Nearest-neighbor (pixel art, no smoothing)
- **Tool:** PixelLab MCP (all assets AI-generated)
