# Leveling System

## Summary

XP-based leveling with an increasing threshold per level. Defeating enemies grants XP, and leveling up boosts HP.

## Current State

Implemented in the prototype:
- XP awarded on enemy defeat: `10 + dangerTier * 4` (14/18/22 for tiers 1/2/3)
- XP threshold per level: `level * 90`
- On level-up: excess XP carries over, HP boosted by 18 (capped at `100 + level * 8`)
- HUD displays current XP and level

## Design

### XP Curve

The XP curve uses a **linear-polynomial hybrid** — linear enough that players can create predictable build guides, with a polynomial curve element that gives experienced players aspirational "radiant" goals. This is a **single-player power fantasy** game, not an MMO — leveling should feel rewarding, not grindy.

There is **no level cap** — infinite leveling fits the infinite dungeon theme. Power keeps growing forever.

The current prototype formula is linear: each level requires `level * 90` XP. This means:

| Level | XP Required | Cumulative XP |
|-------|------------|---------------|
| 1 → 2 | 90 | 90 |
| 2 → 3 | 180 | 270 |
| 3 → 4 | 270 | 540 |
| 5 → 6 | 450 | 1,350 |
| 10 → 11 | 900 | 4,950 |

As the dungeon deepens, stronger enemies should provide more XP to keep progression feeling rewarding.

### Level-Up Effects

On leveling up:
- HP is restored by a fixed amount (currently 18)
- HP maximum increases (`100 + level * 8`)
- Excess XP rolls over to the next level
- Stats will increase per class bonuses (see [classes.md](classes.md)) — not yet implemented

### Future Considerations

- **Skill leveling:** separate from character level, each skill has its own infinite progression (see [classes.md](classes.md))
- **Stat allocation:** players get free stat points to distribute on level-up (see [stats.md](stats.md))
- **Floor-scaling XP:** enemies on deeper floors should grant more XP
- **XP curve tuning:** the prototype's linear formula will be updated to the linear-polynomial hybrid curve. The polynomial element should kick in at higher levels to create aspirational goals without feeling grindy.
