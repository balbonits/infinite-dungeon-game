# Sprite Alignment Reference

Alignment values for each sprite on the 64x32 isometric tile grid.
Updated using `make test-sprite-align` (F12 saves screenshots with metadata).

## How to Use

1. Run `make test-sprite-align`
2. Use WASD to nudge, +/- to scale until sprite sits correctly on the diamond
3. Press F12 — screenshot filename encodes the values
4. Record the final values in the table below
5. These values get baked into game code (PlayerController, EnemyEntity, etc.)

## Creature Sprites (128x128 frames from 1024x1024 sheets, 8x8 grid)

| Sprite | Scale | Offset X | Offset Y | Notes |
|--------|-------|----------|----------|-------|
| Slime | 0.3125 | 0 | 0 | Default — needs tuning |
| Goblin | 0.3125 | 0 | 0 | Default — needs tuning |
| Skeleton | 0.3125 | 0 | 0 | Default — needs tuning |
| Zombie | 0.3125 | 0 | 0 | Default — needs tuning |
| Ogre | 0.3125 | 0 | 0 | Default — needs tuning |
| Werewolf | 0.3125 | 0 | 0 | Default — needs tuning |
| Elemental | 0.3125 | 0 | 0 | Default — needs tuning |
| Magician | 0.3125 | 0 | 0 | Default — needs tuning |

## Hero Sprites (64x128 frames from 2048x1024 sheets, 32x8 grid)

| Sprite | Scale | Offset X | Offset Y | Notes |
|--------|-------|----------|----------|-------|
| Hero | 0.625 | 0 | 0 | Default — needs tuning |
| Heroine | 0.625 | 0 | 0 | Default — needs tuning |

## Screenshot Evidence

Screenshots saved to `docs/evidence/sprite-align/` with filenames like:
```
Slime_dir0_f0_s0.313_x0y-5_20260409_153022.png
```
Format: `{Name}_dir{row}_f{frame}_s{scale}_x{offsetX}y{offsetY}_{timestamp}.png`

Animation strips saved to `docs/evidence/sprite-frames/` with filenames like:
```
Slime_dir0_Walk_3f_20260409_153045.png
```
