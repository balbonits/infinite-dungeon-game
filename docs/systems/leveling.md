# Leveling System

## Summary

XP-based leveling with a quadratic curve, floor-scaling enemy XP, rested XP for returning players, and multi-reward level-ups. No level cap — infinite leveling fits the infinite dungeon theme.

All formulas in this doc are intended to be **player-facing** — published so the community can create build guides and calculate progression.

## Current State

Prototype uses placeholder linear formula (`level * 90`). This doc specifies the redesigned system based on research across 50+ games (Diablo 1-4, PoE, WoW, RuneScape, Hades, Dead Cells, D&D 5e, etc.).

## Design

### XP Curve

**Shape:** Quadratic (exponent ~2.0) — moderate pacing inspired by Diablo 3's 1-70 curve and Last Epoch's accessible approach.

**Formula:**
```
xp_to_next_level(L) = floor(L^2 * 45)
```

| Level | XP Required | Cumulative XP | Approx Time (at level-appropriate floor) |
|-------|------------|---------------|------------------------------------------|
| 1 → 2 | 45 | 45 | ~2 minutes |
| 2 → 3 | 180 | 225 | ~3 minutes |
| 3 → 4 | 405 | 630 | ~4 minutes |
| 5 → 6 | 1,125 | 2,475 | ~7 minutes |
| 10 → 11 | 4,500 | 16,650 | ~15 minutes |
| 20 → 21 | 18,000 | 117,600 | ~20 minutes |
| 50 → 51 | 112,500 | 1,911,375 | ~25 minutes |
| 100 → 101 | 450,000 | 15,150,000 | ~30 minutes |

*Time estimates assume the player is fighting level-appropriate enemies on a matching floor. Actual time varies with build efficiency, floor difficulty choice, and rested XP status.*

**Why quadratic (not linear or exponential):**
- **Linear** (prototype's `level * 90`): time-per-level stays constant — no milestone differentiation, feels flat
- **Quadratic** (`level^2 * 45`): early levels come fast (dopamine), later levels take longer (aspirational) — sweet spot for power fantasy
- **Cubic or higher**: creates prestige walls — good for competitive games (PoE, D2), bad for single-player power fantasy
- **Exponential** (RuneScape): "92 is half of 99" — too punishing for infinite progression

**No level cap.** Consistent with the infinite dungeon theme. Power keeps growing forever, tempered by diminishing returns on stats (see [stats.md](stats.md)).

### Enemy XP (Floor-Scaling)

Enemy XP scales with floor depth so that time-per-level stays roughly consistent when fighting level-appropriate enemies.

**Formula:**
```
enemy_xp(floor, tier) = base_xp(tier) * floor_multiplier(floor)

base_xp(tier):
  Tier 1: 10
  Tier 2: 15
  Tier 3: 20

floor_multiplier(floor) = 1 + (floor - 1) * 0.5
```

| Floor | Multiplier | Tier 1 XP | Tier 2 XP | Tier 3 XP |
|-------|-----------|-----------|-----------|-----------|
| 1 | 1.0x | 10 | 15 | 20 |
| 5 | 3.0x | 30 | 45 | 60 |
| 10 | 5.5x | 55 | 82 | 110 |
| 20 | 10.5x | 105 | 157 | 210 |
| 50 | 25.5x | 255 | 382 | 510 |
| 100 | 50.5x | 505 | 757 | 1,010 |

Combined with the color gradient system (see [color-system.md](color-system.md)), warmer (harder) enemies on deeper floors give bonus XP, incentivizing risk.

**XP bonus from threat level:** Enemies above your level give bonus XP proportional to the level gap. Enemies below your level give standard XP (no penalty — see XP Penalty section).

### Level-Up Rewards

Each level-up should feel meaningful. Multiple rewards per level:

| Reward | Scaling | Notes |
|--------|---------|-------|
| Max HP increase | `floor(8 + level * 0.5)` | Scales with level (replaces flat +8) |
| HP restore | `floor(max_hp * 0.15)` | 15% of new max HP (replaces flat +18) |
| Free stat points | 3 per level (base) | Player allocates to STR/DEX/STA/INT |
| Skill points | 2 per level (base) | Player allocates to any base or specific skill |
| Class stat bonuses | Per class table | Automatic, see [classes.md](classes.md) |

**Milestone levels (every 10th):**
- Bonus stat points: +2 extra (5 total that level)
- Bonus skill points: +1 extra (3 total that level)
- Future: cosmetic rewards, titles, achievements

**Level-up feedback:** Level-ups should have strong visual/audio feedback — full-screen flash, particle burst, fanfare sound, brief pause. See [player-engagement.md](player-engagement.md) for the full juice/feel spec.

### Rested XP

A return-to-game bonus inspired by WoW's rested XP system. Rewards players for coming back without punishing absence.

**How it works:**
- Accumulates while the game is not running: **5% of the current level's XP requirement per 8 hours offline**
- Caps at **1.5 levels** worth of bonus XP
- While active: **doubles XP from enemy kills**
- Consumed as you earn doubled XP (like spending a bonus pool)
- Does NOT double quest/milestone XP (if added later), only kill XP

**Example:** At level 20, XP to next level = 18,000. After 24 hours offline:
- Rested XP accumulated: 3 × 5% × 18,000 = 2,700 bonus XP pool
- Next session: every 100 XP kill gives 200 XP until the 2,700 pool is consumed

**UI indicator:** A subtle visual cue (e.g., XP bar has a different color or shimmer) when rested bonus is active. The rested pool remaining is shown in a tooltip or stats panel.

**Design intent:** This is purely a "welcome back" bonus. It does not punish playing multiple sessions back-to-back (no "rested drought"), and it never removes earned XP.

### XP Penalty for Low-Level Enemies (Deferred)

**Current design: no penalty.** The color gradient system already communicates when enemies are trivial (grey/blue), and players are trusted to push deeper for better rewards.

**Future consideration (PoE-style smooth curve):** If playtesting reveals that players camp easy floors excessively, a smooth penalty curve can be added:
```
safe_zone = 3 levels
if abs(player_level - enemy_level) <= safe_zone:
    xp_multiplier = 1.0
else:
    diff = abs(player_level - enemy_level) - safe_zone
    xp_multiplier = (player_level + 5)^1.5 / ((player_level + 5)^1.5 + diff^2.5)
```
This would give ~80% XP at 5 levels below, ~35% at 10, ~3% at 20+. No hard cutoff.

This is documented as an option, not a commitment. Revisit during balance tuning.

### Prototype Formulas (Legacy Reference)

The original Phaser prototype used these values, preserved here for comparison:

```
xp_to_next_level = level * 90            (linear)
enemy_xp = 10 + danger_tier * 4          (flat, no floor scaling)
max_hp = 100 + level * 8                 (linear)
hp_restore_on_levelup = 18               (flat)
```

These are replaced by the formulas above.

## Open Questions

- Exact constant `C` in the XP curve (45) needs playtesting to feel right — may need tuning
- Should the floor multiplier be linear (current) or polynomial?
- How do milestone rewards interact with the skill system? (Extra skill points from milestone levels)
- Should the XP bar show "rested bonus remaining" numerically or just visually?
- How does XP interact with the Mage's spell scroll learning system? (Does casting scrolls give XP?)
- Should boss kills give a large one-time XP bonus separate from the per-kill formula?
