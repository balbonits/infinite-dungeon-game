# Zone Saturation

## Summary

A per-zone difficulty dial that increases as the player farms a zone and decays when they move on. Higher saturation means harder enemies but better loot. The system encourages zone rotation instead of grinding one floor range forever, creating a natural play pattern where the player pushes deep, farms a zone until it saturates, moves to a different zone, and returns later when saturation has decayed.

## Current State

**Spec status: LOCKED.** Design complete. Not yet implemented. Depends on floor scaling (locked), dungeon zones (locked), item generation (locked), and monster modifiers (locked).

## Design

### Overview

Each 10-floor dungeon zone (floors 1-10, 11-20, 21-30, etc.) maintains an independent **saturation value** ranging from 0 to 100. Saturation increases with every enemy killed in that zone and decays passively while the player is not in that zone. Higher saturation increases enemy stats (HP, damage, speed) but also increases loot quantity and quality.

Saturation is per-character and persists across sessions. It is stored in the save data as a floating-point value per zone.

### Saturation Accumulation

Every enemy killed in a zone increases that zone's saturation:

```
saturation_gain_per_kill = base_gain * zone_multiplier

base_gain = 0.15
zone_multiplier = 1.0 + (zone_number - 1) * 0.05
```

| Zone | Zone Mult | Gain per Kill | Kills to 50% | Kills to 100% |
|------|-----------|---------------|---------------|----------------|
| 1 (floors 1-10) | 1.00x | 0.15 | 333 | 667 |
| 2 (floors 11-20) | 1.05x | 0.158 | 317 | 634 |
| 5 (floors 41-50) | 1.20x | 0.18 | 278 | 556 |
| 10 (floors 91-100) | 1.45x | 0.218 | 230 | 459 |

Deeper zones saturate faster because the player is expected to be more powerful and kill faster, so the system needs to keep up.

Saturation is capped at 100. It cannot exceed this value.

### Saturation Decay

Saturation decays passively while the player is NOT on a floor within that zone. Decay ticks once per real-time minute (including while in town, on other floors, or offline).

```
decay_per_minute = 0.25
```

| Starting Saturation | Time to Reach 50% | Time to Reach 25% | Time to Reach 0 |
|--------------------|--------------------|--------------------|--------------------|
| 100 | 200 min (~3.3 hrs) | 300 min (5 hrs) | 400 min (~6.7 hrs) |
| 75 | 100 min (~1.7 hrs) | 200 min (~3.3 hrs) | 300 min (5 hrs) |
| 50 | 0 min | 100 min (~1.7 hrs) | 200 min (~3.3 hrs) |

**Design intent:** A player who farms a zone for a full session (30-60 minutes, reaching ~50-70% saturation) will find that zone mostly decayed by the next day. A player who maxes out saturation at 100% will need to leave that zone for about 7 hours before it fully resets. This encourages natural session breaks and zone rotation without punishing the player for playing a lot.

Decay is linear (not exponential) to keep the math simple and predictable. The player can learn the decay rate and plan around it.

### Difficulty Scaling from Saturation

Saturation increases enemy stats within the saturated zone:

```
saturation_ratio = current_saturation / 100

hp_multiplier = 1.0 + saturation_ratio * 0.50
damage_multiplier = 1.0 + saturation_ratio * 0.35
speed_multiplier = 1.0 + saturation_ratio * 0.15
```

| Saturation | HP Mult | Damage Mult | Speed Mult | Feel |
|------------|---------|-------------|------------|------|
| 0% | 1.00x | 1.00x | 1.00x | Baseline |
| 25% | 1.125x | 1.088x | 1.038x | Barely noticeable |
| 50% | 1.25x | 1.175x | 1.075x | Enemies feel tougher |
| 75% | 1.375x | 1.263x | 1.113x | Noticeably harder |
| 100% | 1.50x | 1.35x | 1.15x | Significant challenge increase |

These multipliers are applied after floor scaling and pact multipliers but before Dungeon Intelligence adjustments:

```
final_stat = base * floor_mult * pact_mult * saturation_mult * intelligence_mult
```

At 100% saturation, enemies have 50% more HP, deal 35% more damage, and move 15% faster. This is comparable to pushing 1-2 zones deeper than the player is currently in.

### Reward Scaling from Saturation

Higher saturation improves rewards to compensate for the increased difficulty:

```
saturation_ratio = current_saturation / 100

xp_bonus = saturation_ratio * 30%
material_drop_bonus = saturation_ratio * 40%
quality_shift_floors = floor(saturation_ratio * 20)
equipment_drop_bonus = saturation_ratio * 20%
```

| Saturation | XP Bonus | Material Drop Bonus | Quality Shift | Equip Drop Bonus |
|------------|----------|---------------------|---------------|------------------|
| 0% | +0% | +0% | 0 floors | +0% |
| 25% | +7.5% | +10% | 5 floors | +5% |
| 50% | +15% | +20% | 10 floors | +10% |
| 75% | +22.5% | +30% | 15 floors | +15% |
| 100% | +30% | +40% | 20 floors | +20% |

**XP bonus** is additive to kill XP (stacks with pact XP bonus and rested XP).

**Material drop bonus** multiplies the base 25% material drop chance: at +40%, effective chance is `25% * 1.40 = 35%`.

**Quality shift** offsets the floor range for quality distribution lookup. At 100% saturation, the quality table behaves as if the player is 20 floors deeper. On floor 5 with 100% saturation, quality rolls use the floor 25 table (60% Normal, 35% Superior, 5% Elite).

**Equipment drop bonus** multiplies the per-tier base drop rate. A Tier 1 enemy at 8% base with +20% bonus has `8% * 1.20 = 9.6%` chance.

### Zone Identification

The zone for any floor is:

```
zone_number = ceil(floor_number / 10)
```

When the player enters a floor, the game identifies which zone it belongs to and uses that zone's current saturation value for all difficulty and reward calculations on that floor.

### UI Communication

Saturation is communicated through subtle environmental cues, not explicit numbers:

- **0-25% saturation:** No visual change
- **25-50% saturation:** Faint reddish tint to the zone's ambient lighting
- **50-75% saturation:** Noticeable red ambient tint, enemy spawn points pulse faintly
- **75-100% saturation:** Strong red ambient tint, visible magicule particles in the air, spawn points glow

The exact saturation percentage is visible in the character stats panel (accessible from the pause menu), but not on the HUD during gameplay. The environmental cues let the player sense the zone "heating up" without checking numbers.

### Interaction with Other Systems

- **Floor Scaling (floor-scaling.md):** Saturation multipliers are separate from zone multipliers. A floor 15 enemy (zone 2, base 1.5x multiplier) in a 50% saturated zone has stats multiplied by 1.5 * 1.25 = 1.875x for HP.
- **Dungeon Pacts (dungeon-pacts.md):** Pact effects and saturation effects stack multiplicatively. Running Iron Will rank 2 (1.50x HP) in a 100% saturated zone (1.50x HP) gives 2.25x HP total.
- **Dungeon Intelligence (dungeon-intelligence.md):** Intelligence modifiers are the outermost layer, applied after saturation. If the player is dominating even in a saturated zone, Intelligence pushes further.
- **Item Generation (item-generation.md):** Quality shift from saturation stacks with quality shift from pacts. Both shift the floor range lookup for RollQuality.
- **Rested XP (leveling.md):** Saturation XP bonus stacks additively with rested XP's doubling. A rested player in a 50% saturated zone gets `(base_xp * 2.0) + (base_xp * 0.15)` = 2.15x effective XP per kill.

### Design Rationale

**Why zone rotation?** Without saturation, the optimal strategy is to find the deepest floor where you can efficiently one-shot enemies and grind it forever. This is boring. Saturation makes that strategy self-limiting: the longer you farm one zone, the harder it gets, until you are better off moving to a different zone. This creates a natural rotation pattern:

1. Push to a new zone (fresh, 0% saturation)
2. Farm the zone until saturation makes it harder than the next zone would be
3. Move to a different zone (or push deeper)
4. Return later when saturation has decayed

This pattern mirrors the "wave" shape of difficulty described in difficulty-design.md: ramp up (saturation builds), peak (saturation makes the zone too hard), relief (switch zones), ramp up again (new zone or decayed zone).

**Why not per-floor instead of per-zone?** Zones are 10 floors wide. Per-floor saturation would be too granular -- the player could just hop between floors 1 and 2 forever. Per-zone forces meaningful travel and makes the decision to leave a zone feel like a real commitment.

## Acceptance Criteria

- [ ] Each zone (10-floor range) maintains an independent saturation value, 0 to 100
- [ ] Saturation increases by `0.15 * zone_multiplier` per enemy killed in that zone
- [ ] Zone multiplier is `1.0 + (zone_number - 1) * 0.05`
- [ ] Saturation decays at 0.25 per real-time minute when the player is not in that zone
- [ ] Decay applies while in town, on other floors, and while offline
- [ ] Saturation is capped at 100 and floored at 0
- [ ] Enemy HP multiplier is `1.0 + (saturation / 100) * 0.50`
- [ ] Enemy damage multiplier is `1.0 + (saturation / 100) * 0.35`
- [ ] Enemy speed multiplier is `1.0 + (saturation / 100) * 0.15`
- [ ] XP bonus is `(saturation / 100) * 30%`, additive to kill XP
- [ ] Material drop bonus multiplies base 25% chance by `1 + (saturation / 100) * 0.40`
- [ ] Quality shift offsets floor range by `floor((saturation / 100) * 20)` floors
- [ ] Equipment drop bonus multiplies base rate by `1 + (saturation / 100) * 0.20`
- [ ] Saturation multipliers apply after floor scaling and pact effects
- [ ] Saturation persists across sessions in character save data
- [ ] Environmental visual cues change at 25%, 50%, 75% thresholds
- [ ] Exact saturation percentage is visible in character stats panel

## Implementation Notes

- Store saturation as a `Dictionary<int, float>` mapping zone number to saturation value in the save data
- Decay calculation on save load: compute elapsed real-time minutes since last save, subtract `0.25 * elapsed_minutes` from each zone's saturation (clamped to 0)
- Saturation gain is applied in the kill handler after XP reward
- Saturation stat multipliers feed into the same multiplier chain as floor scaling: `base * floor * pact * saturation * intelligence`
- Visual tint can be implemented as a CanvasModulate color that shifts toward red as saturation increases
- Zone identification is a simple `ceil(floor / 10)` lookup, reused from the existing zone system in dungeon.md

## Open Questions

None.
