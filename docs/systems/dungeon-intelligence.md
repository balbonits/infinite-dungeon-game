# Dungeon Intelligence

## Summary

An adaptive AI Director that monitors player performance and adjusts dungeon parameters in real time to maintain tension. The dungeon "learns" the player, increasing pressure when they dominate and easing when they struggle. The system operates within strict bounds so the game never becomes trivial or impossible. Fits the lore: the dungeon is a living entity that adapts its immune response to the invader.

## Current State

**Spec status: LOCKED.** Design complete. Not yet implemented. Depends on floor scaling (locked), monster modifiers (locked), monster behavior (locked), item generation (locked), and spawning (locked).

## Design

### Overview

The Dungeon Intelligence system tracks four performance metrics across a rolling window and computes a **Pressure Score** that drives adjustments to spawn rate, enemy aggression, elite frequency, and loot quality. The system is invisible to the player -- no UI element reveals the pressure score or the adjustments. The player should feel the dungeon responding without understanding the exact mechanics.

The system does NOT replace floor scaling. Floor-based difficulty (zone multipliers, monster levels) is the baseline. Dungeon Intelligence applies a secondary modifier layer on top of that baseline, limited to a narrow adjustment range.

### Performance Metrics

Four metrics are tracked per-session (reset when the player returns to town or loads a save):

#### 1. Kill Speed (KS)

How fast the player kills enemies relative to the floor's expected clear time.

```
kill_speed_ratio = actual_kills_per_minute / expected_kills_per_minute
expected_kills_per_minute = 6 + floor_number * 0.05
```

| Kill Speed Ratio | Meaning |
|-----------------|---------|
| < 0.5 | Struggling -- killing much slower than expected |
| 0.5 - 0.8 | Below average |
| 0.8 - 1.2 | On pace |
| 1.2 - 1.5 | Above average |
| > 1.5 | Dominating -- clearing far faster than expected |

The expected kills/minute increases slightly with floor depth because deeper enemies give more XP and the player is expected to be more powerful, not because the floor should clear faster.

#### 2. Damage Efficiency (DE)

Ratio of damage dealt to damage taken over the rolling window.

```
damage_efficiency = damage_dealt_last_60s / max(1, damage_taken_last_60s)
```

| Damage Efficiency | Meaning |
|-------------------|---------|
| < 2.0 | Taking heavy damage relative to output |
| 2.0 - 5.0 | Healthy trade ratio |
| 5.0 - 10.0 | Very efficient -- barely getting hit |
| > 10.0 | Untouchable -- trivializing the content |

A 60-second rolling window prevents single large hits or burst kills from skewing the metric.

#### 3. Floor Pace (FP)

How quickly the player is clearing full floors.

```
floor_pace = floors_cleared_this_session / session_duration_minutes
expected_pace = 1 / max(5, 10 - floor_number * 0.02)
floor_pace_ratio = floor_pace / expected_pace
```

Expected pace is roughly 1 floor per 5-10 minutes, scaling slightly faster at deeper floors (the player is more powerful and efficient).

| Floor Pace Ratio | Meaning |
|-----------------|---------|
| < 0.5 | Very slow -- farming or struggling |
| 0.5 - 0.8 | Below expected pace |
| 0.8 - 1.2 | On pace |
| 1.2 - 1.5 | Fast -- efficient clearing |
| > 1.5 | Speed-running through content |

#### 4. Death Frequency (DF)

How often the player dies.

```
deaths_this_session = count of deaths since session start
death_weight = deaths_this_session * 2.0
```

Each death adds 2.0 to the death weight. This is a cumulative counter, not a ratio. More deaths = higher weight = more the system eases up. The weight is halved every 10 minutes of survival to prevent permanent easement after early deaths.

```
death_decay: every 10 minutes without dying, death_weight *= 0.5
```

### Pressure Score Calculation

The four metrics combine into a single pressure score:

```
raw_pressure = (KS_ratio * 0.35) + (DE_ratio * 0.25) + (FP_ratio * 0.25) - (death_weight * 0.15)

pressure_score = clamp(raw_pressure, 0.5, 1.8)
```

| Weight | Metric | Why |
|--------|--------|-----|
| 0.35 | Kill Speed | Most direct measure of player power vs floor difficulty |
| 0.25 | Damage Efficiency | Measures survivability and build quality |
| 0.25 | Floor Pace | Catches players who are efficient overall even if individual fights vary |
| -0.15 | Death Weight | Deaths directly reduce pressure -- the dungeon backs off |

**Score interpretation:**

| Pressure Score | State | Dungeon Response |
|---------------|-------|------------------|
| 0.50 - 0.70 | Struggling | Ease up -- fewer spawns, weaker enemies, better drops |
| 0.70 - 0.90 | Challenged | Slight ease -- minor adjustments |
| 0.90 - 1.10 | Balanced | No adjustment -- baseline difficulty |
| 1.10 - 1.30 | Comfortable | Slight push -- more enemies, tougher modifiers |
| 1.30 - 1.80 | Dominating | Push hard -- max spawns, aggressive enemies, elite density up |

### Adjustment Formulas

All adjustments are multiplicative modifiers applied on top of existing floor scaling and pact effects. The adjustment range is narrow to prevent the system from overwhelming other difficulty systems.

#### Spawn Rate Modifier

```
spawn_rate_modifier = 0.8 + (pressure_score - 0.5) * 0.31
```

| Pressure | Modifier | Effect |
|----------|----------|--------|
| 0.50 | 0.80x | 20% fewer spawns |
| 0.90 | 0.92x | 8% fewer spawns |
| 1.00 | 0.96x | ~baseline |
| 1.10 | 0.99x | ~baseline |
| 1.50 | 1.11x | 11% more spawns |
| 1.80 | 1.20x | 20% more spawns |

Applied to the room budget calculation and respawn timer. Range: 0.80x to 1.20x.

#### Enemy Aggression Modifier

Adjusts aggro range, alert duration, and attack cooldown.

```
aggression_modifier = 0.85 + (pressure_score - 0.5) * 0.23
```

| Pressure | Modifier | Effect |
|----------|----------|--------|
| 0.50 | 0.85x | 15% less aggressive (shorter aggro, longer alert) |
| 1.00 | 0.97x | ~baseline |
| 1.50 | 1.08x | 8% more aggressive |
| 1.80 | 1.15x | 15% more aggressive (longer aggro, shorter alert, faster attacks) |

Applied to aggro range (direct multiply), alert duration (inverse multiply), and attack cooldown (inverse multiply). Range: 0.85x to 1.15x.

#### Elite Frequency Modifier

Shifts the rarity roll thresholds for Empowered and Named monsters.

```
elite_shift = (pressure_score - 1.0) * 0.05
```

| Pressure | Elite Shift | Effective Named % (base 2%) |
|----------|------------|----------------------------|
| 0.50 | -2.5% | 0% (floor at 0) |
| 1.00 | 0% | 2.0% |
| 1.50 | +2.5% | 4.5% |
| 1.80 | +4.0% | 6.0% |

The shift is additive to the Named threshold and double that to the Empowered threshold. Minimum Named chance is 0%. Maximum Named chance from this system alone is 6%.

#### Loot Quality Modifier

Adjusts the quality roll distribution when the player is struggling.

```
if pressure_score < 0.85:
    loot_quality_bonus = (0.85 - pressure_score) * 20
    // Adds up to +7% chance of Superior, +3% chance of Elite
else:
    loot_quality_bonus = 0
```

Loot quality only improves when the player is struggling. The dungeon never reduces loot quality below baseline when the player is dominating. This is a "rubber band" that helps struggling players catch up through gear, not a punishment for skilled players.

| Pressure | Superior Bonus | Elite Bonus |
|----------|---------------|-------------|
| 0.50 | +7% | +3% |
| 0.70 | +3% | +1.3% |
| 0.85+ | +0% | +0% |

### Adjustment Bounds

Hard limits prevent the system from making the game trivial or impossible:

| Parameter | Minimum Modifier | Maximum Modifier |
|-----------|-----------------|------------------|
| Spawn rate | 0.80x | 1.20x |
| Aggression | 0.85x | 1.15x |
| Named rarity bonus | +0% | +4% |
| Loot quality bonus | +0% Superior, +0% Elite | +7% Superior, +3% Elite |

These bounds are intentionally narrow. The Dungeon Intelligence system is a subtle tuning layer, not a difficulty selector. Dungeon Pacts (dungeon-pacts.md) are the player's tool for large difficulty adjustments. Intelligence handles the fine grain.

### Interaction with Other Systems

- **Floor Scaling (floor-scaling.md):** Intelligence modifiers multiply on top of floor multipliers. A floor with 1.5x zone multiplier and 1.15x intelligence aggression modifier results in 1.725x effective aggression.
- **Dungeon Pacts (dungeon-pacts.md):** Pact effects are applied first, then intelligence modifiers. A player running high heat pacts who is still dominating will face the combined pressure of both systems.
- **Monster Modifiers (monster-modifiers.md):** Elite frequency shift adjusts the rarity roll thresholds, not the modifier effects themselves. More Named enemies means more modifier stacking, which compounds with pact rank of Empowered Masses.
- **Death (death.md):** Each death increases death_weight, causing the system to ease up. The system naturally softens after repeated deaths without any explicit "easy mode" toggle.

### Lore Integration

The dungeon is an intelligent, patient predator. It observes its prey through its monsters -- every fight is data. When an adventurer breezes through a floor, the dungeon recognizes they are growing strong and redirects more of its immune response to that area. When an adventurer struggles, the dungeon eases back: a weakened adventurer who quits is worthless, but one who survives and pushes deeper is a bigger meal later.

The system is the mechanical expression of Design Pillar #3: "The Dungeon Is Alive." The living dungeon watches, learns, and responds. Not with malice, but with the cold calculation of a predator optimizing its food chain.

### Session Reset

All performance metrics reset when:
- The player returns to town
- The player loads a save
- A new game session starts

The pressure score begins at 1.0 (balanced) at the start of each session and adjusts within the first 2-3 minutes of combat based on incoming metrics. There is a 60-second "grace period" at session start during which no adjustments are applied, giving the player time to warm up.

## Acceptance Criteria

- [ ] Kill speed ratio tracks actual kills/minute vs expected kills/minute per floor
- [ ] Damage efficiency tracks a 60-second rolling window of dealt vs taken
- [ ] Floor pace tracks floors cleared per session minute vs expected pace
- [ ] Death weight accumulates on death (+2.0) and decays every 10 minutes (*0.5)
- [ ] Pressure score is computed from all four metrics with weights 0.35/0.25/0.25/-0.15
- [ ] Pressure score is clamped to [0.5, 1.8]
- [ ] Spawn rate modifier stays within [0.80x, 1.20x]
- [ ] Enemy aggression modifier stays within [0.85x, 1.15x]
- [ ] Elite frequency shift stays within [+0%, +4%] for Named chance
- [ ] Loot quality bonus only applies when pressure < 0.85 (struggling players only)
- [ ] Loot quality is never reduced below baseline (dominating players get normal loot, not worse)
- [ ] All metrics reset when player returns to town, loads a save, or starts a new session
- [ ] 60-second grace period at session start applies no adjustments
- [ ] No UI element reveals the pressure score or adjustment values to the player
- [ ] Adjustments multiply on top of floor scaling and pact effects (not replace them)

## Implementation Notes

- Track metrics in a session-scoped data object (not saved to disk -- resets on session boundaries)
- Kill speed and damage efficiency use circular buffers for rolling windows (60 seconds for DE, full session for KS)
- Pressure score should be recalculated every 5 seconds, not every frame (performance)
- Apply modifiers through the same multiplier chain as floor scaling: `final_value = base * floor_mult * pact_mult * intelligence_mult`
- The grace period can be implemented as a simple timer that zeroes all modifier outputs until it expires
- Loot quality bonus modifies the quality roll in ItemGenerator.RollQuality by adding to the Superior and Elite probabilities before normalization

## Open Questions

None.
