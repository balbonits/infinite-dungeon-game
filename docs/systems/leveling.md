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

The current formula is linear: each level requires `level * 90` XP. This means:

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

- **Skill leveling:** separate from character level, skills improve through use
- **Stat allocation:** players may get points to distribute on level-up
- **Floor-scaling XP:** enemies on deeper floors should grant more XP
- **XP curve tuning:** the linear curve may need to become exponential at higher levels

## Open Questions

- Should the XP curve be linear, polynomial, or exponential?
- At what level does the current formula start to feel grindy?
- Should there be a level cap, or is infinite leveling part of the design?
- How should "limitless skill leveling" (mentioned in the README) interact with character level?
