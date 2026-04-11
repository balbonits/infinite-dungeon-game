# Characters

All entity sprites organized by role.

## Structure

```
characters/
├── player/              — Playable character classes
│   ├── warrior/         — Armored knight with sword+shield (8-dir + idle + attack anims)
│   ├── mage/            — Robed spellcaster (8-dir rotations)
│   └── ranger/          — Agile ranged fighter (8-dir rotations)
├── enemies/             — Monster species
│   ├── skeleton/        — Undead skeleton warrior (8-dir + walking anim)
│   └── goblin/          — Hunched goblin brute with club (8-dir rotations)
└── npcs/                — Town NPC characters
    ├── shopkeeper/      — Portly merchant with coin pouch
    ├── blacksmith/      — Muscular smith with hammer
    ├── guild_master/    — Elder in green robes with scroll
    ├── teleporter/      — Hooded mage with glowing blue runes
    └── banker/          — Well-dressed with spectacles and ledger
```

## Conventions

- All characters: 92x92px canvas, 8 directional rotations
- Each character folder has `rotations/` with 8 PNGs (south, south-east, east, etc.)
- Some have `animations/` with frame sequences per direction
- `metadata.json` contains PixelLab generation parameters
- Scale in-game: Player 1.0, Enemies 0.7, NPCs 0.9, Bosses 1.0-1.2
