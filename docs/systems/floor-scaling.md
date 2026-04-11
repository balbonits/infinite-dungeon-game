# Floor Scaling — Monster Level & Difficulty Formula

## Summary

Every dungeon floor has a mathematically defined difficulty. Monster levels are derived from the floor number using a transparent formula that players can learn and metagame. Deeper floors = stronger monsters = warmer colors = more XP.

## The Formula

### Monster Level Range per Floor

```
baseLevel = floor
minLevel  = max(1, floor - 1)
maxLevel  = floor + 2
```

A monster spawned on floor N has a level uniformly chosen from `[minLevel, maxLevel]`.

| Floor | Base Level | Min Level | Max Level | Spread |
|-------|-----------|-----------|-----------|--------|
| 1     | 1         | 1         | 3         | 1-3    |
| 2     | 2         | 1         | 4         | 1-4    |
| 5     | 5         | 4         | 7         | 4-7    |
| 10    | 10        | 9         | 12        | 9-12   |
| 25    | 25        | 24        | 27        | 24-27  |
| 50    | 50        | 49        | 52        | 49-52  |
| 100   | 100       | 99        | 102       | 99-102 |

**Why this formula:**
- `baseLevel = floor` is dead simple and immediately learnable: "floor 10 = level 10 monsters"
- The -1/+2 asymmetric spread means most monsters are at or above the floor's base level — floors feel slightly challenging, not trivially easy
- The spread creates visual variety on each floor (mix of colors)

### Monster Stat Scaling

All stats are linear functions of monster level:

```
hp       = 20 + level * 10
speed    = 50 + level * 5     (pixels/second)
damage   = 2 + level
xpReward = 8 + level * 4
```

| Level | HP  | Speed | Damage | XP  |
|-------|-----|-------|--------|-----|
| 1     | 30  | 55    | 3      | 12  |
| 3     | 50  | 65    | 5      | 20  |
| 5     | 70  | 75    | 7      | 28  |
| 10    | 120 | 100   | 12     | 48  |
| 20    | 220 | 150   | 22     | 88  |
| 50    | 520 | 300   | 52     | 208 |

### Color Mapping (Level Gap)

Monster color is determined by `gap = monsterLevel - playerLevel`:

| Gap      | Color  | Hex       | Meaning          |
|----------|--------|-----------|------------------|
| -10+     | Grey   | `#9D9D9D` | Trivial          |
| -6       | Blue   | `#4A7DFF` | Low              |
| -3       | Cyan   | `#4AE8E8` | Low-mid          |
| 0        | Green  | `#6BFF89` | Even             |
| +3       | Yellow | `#FFDE66` | Mid-high         |
| +6       | Gold   | `#F5C86B` | High             |
| +8       | Orange | `#FF9340` | Very high        |
| +10+     | Red    | `#FF6F6F` | Extreme          |

Colors interpolate smoothly between anchors. See [color-system.md](color-system.md).

### Player Power Curve

Player damage: `12 + floor(level * 1.5)`

| Player Lvl | Damage | Hits to Kill Lvl-Even (green) | Hits to Kill Lvl+2 (yellow) |
|-----------|--------|-------------------------------|------------------------------|
| 1         | 13     | 3 (30 HP)                     | 4 (40 HP)                    |
| 3         | 16     | 4 (50 HP)                     | 5 (70 HP)                    |
| 5         | 19     | 4 (70 HP)                     | 5 (90 HP)                    |
| 10        | 27     | 5 (120 HP)                    | 6 (140 HP)                   |
| 20        | 42     | 6 (220 HP)                    | 6 (240 HP)                   |

At all levels, even-level enemies die in 3-6 hits. The ratio stays consistent — power creep is controlled.

### XP Curve & Floor Pacing

XP to next level: `floor(level^2 * 45)` (see [leveling.md](leveling.md) for canonical formula)

Expected kills to level up (killing even-level monsters):

| Player Lvl | XP Needed | Monster XP (even) | Kills to Level |
|-----------|-----------|-------------------|----------------|
| 1         | 45        | 12                | 4              |
| 3         | 405       | 20                | 21             |
| 5         | 1,125     | 28                | 41             |
| 10        | 4,500     | 48                | 94             |
| 20        | 18,000    | 88                | 205            |

The quadratic XP curve means early levels come fast (dopamine) while later levels require deeper floor pushes for sufficient XP from the floor multiplier. See [leveling.md](leveling.md) for enemy XP scaling with floor depth.

### Metagaming

Players can derive these rules:
1. "Floor N has level N monsters" — the base rule
2. "I should be level N before floor N" — to see green/yellow enemies
3. "Floor N+5 has gold/orange enemies" — for XP farming at higher risk
4. "Floor N-5 has grey enemies" — not worth farming (low XP)
5. "I need ~15-20 kills per level" — predictable session length

### Future Scaling Hooks (not implemented)

- **Floor modifiers**: Certain floors (multiples of 10?) could have +level bonus
- **Dungeon Pacts**: Voluntary difficulty that shifts the level formula upward
- **Zone Saturation**: Per-zone difficulty dial that modifies the spread
- **Boss floors**: Every Nth floor spawns a boss at `floor + 5` level
