# Tiles

Isometric tiles for dungeon and town rendering.

## Structure

```
tiles/
├── dungeon/             — Dungeon environment tiles
│   ├── floor.png        — Base dark cobblestone floor
│   ├── floor_cracked.png — Cracked cobblestone variation
│   ├── floor_flagstone.png — Irregular flagstone variation
│   ├── floor_worn.png   — Smooth worn stone variation
│   ├── wall.png         — Blue-gray stone brick wall block
│   ├── stairs_down.png  — Staircase descending to next floor
│   └── stairs_up.png    — Staircase ascending to previous floor
└── town/                — Town environment tiles
    ├── town_floor.png   — Warm tan cobblestone
    └── town_wall.png    — Timber-frame wood over stone
```

## Specs

- All tiles: 64x64px canvas
- Floor tiles: "thin tile" shape (isometric diamond)
- Wall/stairs tiles: "block" shape (isometric cube)
- TileSet grid: 64x32 footprint (2:1 isometric ratio)
- Floor tiles randomly mixed for visual variation
