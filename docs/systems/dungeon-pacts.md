# Dungeon Pacts

## Summary

Voluntary difficulty modifiers the player toggles before entering the dungeon. Each pact makes the dungeon harder in a specific way, and active pacts increase rewards proportionally. Inspired by Hades' Pact of Punishment and Diablo 3's Torment system, Dungeon Pacts let players dial difficulty to match their power level, creating a personalized endgame challenge that scales infinitely.

## Current State

**Spec status: LOCKED.** Design complete. Not yet implemented. Depends on floor scaling (locked), monster modifiers (locked), item generation (locked), and leveling (locked).

## Design

### Overview

Dungeon Pacts are accessed at the **Pact Altar** in town before a dungeon run. Each pact has a name, a specific difficulty effect, and 3-5 ranks of increasing intensity. The player toggles individual pact ranks on or off freely. Active pacts increase a global **Pact Heat** score that drives reward scaling.

Pacts persist across deaths and sessions. Once toggled on, a pact stays active until the player returns to the altar and changes it. Pacts affect every floor of every dungeon run while active.

### Pact Heat

Each pact rank contributes a fixed heat value. Total heat is the sum of all active pact rank heat values. Heat drives all reward bonuses.

```
pact_heat = sum(heat_value of each active pact rank)
```

Heat has no cap. A player running all 10 pacts at maximum rank has 150 total heat. Reward formulas scale with heat, so there is always an incentive to push higher.

### Reward Scaling

All reward bonuses are computed from total pact heat:

```
xp_bonus = pact_heat * 1.5%
gold_bonus = pact_heat * 1.0%
material_drop_bonus = pact_heat * 0.8%
equipment_drop_bonus = pact_heat * 0.5%
quality_shift = floor(pact_heat / 30)
```

| Total Heat | XP Bonus | Gold Bonus | Mat Drop Bonus | Equip Drop Bonus | Quality Shift |
|------------|----------|------------|----------------|-------------------|---------------|
| 10 | +15% | +10% | +8% | +5% | 0 |
| 30 | +45% | +30% | +24% | +15% | +1 tier |
| 60 | +90% | +60% | +48% | +30% | +2 tiers |
| 100 | +150% | +100% | +80% | +50% | +3 tiers |
| 150 | +225% | +150% | +120% | +75% | +5 tiers |

**Quality shift** means the floor range thresholds for Normal/Superior/Elite quality distribution shift down by 10 floors per tier. At +3 quality shift, floor 1 uses the quality table for floor 31+ (60% Normal, 35% Superior, 5% Elite). The shift cannot push below floor range 100+ (the best available distribution).

**Material drop bonus** is additive with the base 25% flat material drop chance: at +24% bonus, the effective material drop chance is `25% * 1.24 = 31%`.

**Equipment drop bonus** is additive with the base tier drop rate: a Tier 1 enemy at 8% base with +15% bonus has `8% * 1.15 = 9.2%` drop chance.

### The 10 Pacts

#### PACT-01: Swelling Horde

More enemies per room. Increases the room budget divisor.

| Rank | Heat | Effect |
|------|------|--------|
| 1 | 3 | Room budget +20% (divisor 10 instead of 12) |
| 2 | 5 | Room budget +40% (divisor ~8.6) |
| 3 | 7 | Room budget +60% (divisor 7.5) |
| 4 | 10 | Room budget +80% (divisor ~6.7) |
| 5 | 15 | Room budget +100% (divisor 6) |

```
modified_budget = max(1, floor(area / (12 / (1 + rank * 0.20))))
```

At rank 5, every room has double the monsters. Combined with other pacts, this is the raw volume dial.

#### PACT-02: Iron Will

Enemies have more HP. Multiplies monster base HP after all other modifiers.

| Rank | Heat | Effect |
|------|------|--------|
| 1 | 3 | Monster HP +25% |
| 2 | 5 | Monster HP +50% |
| 3 | 8 | Monster HP +80% |
| 4 | 12 | Monster HP +120% |

```
modified_hp = base_hp * rarity_multiplier * (1 + rank * [0.25, 0.50, 0.80, 1.20][rank-1] / base_hp... )
-- Simplified: hp_multiplier = [1.25, 1.50, 1.80, 2.20][rank-1]
```

Exact multipliers by rank: 1.25x, 1.50x, 1.80x, 2.20x. Applied after rarity HP multiplier.

#### PACT-03: Sharpened Claws

Enemies deal more damage. Multiplies monster base damage after all other modifiers.

| Rank | Heat | Effect |
|------|------|--------|
| 1 | 4 | Monster damage +20% |
| 2 | 7 | Monster damage +40% |
| 3 | 10 | Monster damage +65% |
| 4 | 14 | Monster damage +100% |

Exact multipliers by rank: 1.20x, 1.40x, 1.65x, 2.00x. Applied after rarity and modifier damage multipliers.

#### PACT-04: Quicksilver Blood

Enemies move and attack faster. Multiplies both movement speed and reduces attack cooldown.

| Rank | Heat | Effect |
|------|------|--------|
| 1 | 3 | Enemy speed +15%, cooldown -10% |
| 2 | 5 | Enemy speed +30%, cooldown -20% |
| 3 | 8 | Enemy speed +50%, cooldown -30% |

```
modified_speed = base_speed * archetype_multiplier * (1 + rank * [0.15, 0.30, 0.50][rank-1])
modified_cooldown = base_cooldown * (1 - [0.10, 0.20, 0.30][rank-1])
```

At rank 3, melee enemies close distance 50% faster and attack 30% more often. Swarmers become genuinely dangerous.

#### PACT-05: Empowered Masses

Increases the spawn rate of Empowered and Named rarity monsters.

| Rank | Heat | Effect (Normal / Empowered / Named) |
|------|------|--------------------------------------|
| 0 | 0 | 78% / 20% / 2% (base) |
| 1 | 4 | 65% / 30% / 5% |
| 2 | 7 | 50% / 40% / 10% |
| 3 | 10 | 35% / 45% / 20% |
| 4 | 14 | 20% / 50% / 30% |

At rank 4, nearly a third of all enemies are Named elites with 2-3 modifiers. Combined with Swelling Horde, this creates density of modded enemies that demands optimized builds.

#### PACT-06: Waning Light

Reduces the player's effective resistances. Simulates deeper magicule pressure.

| Rank | Heat | Effect |
|------|------|--------|
| 1 | 3 | All resistances -10 |
| 2 | 5 | All resistances -20 |
| 3 | 8 | All resistances -35 |

```
effective_resistance = clamp(base_resistance - floor_penalty - pact_penalty, -100, 75)
pact_penalty = [10, 20, 35][rank-1]
```

At rank 3, the player takes elemental damage as if they were 70 floors deeper. This interacts with the resistance erosion curve from elemental-damage.md, making deep pushes significantly harder.

#### PACT-07: Fading Vitality

Reduces the player's passive HP regeneration from STA. Does not affect potion healing.

| Rank | Heat | Effect |
|------|------|--------|
| 1 | 3 | HP regen -30% |
| 2 | 6 | HP regen -60% |
| 3 | 9 | HP regen -100% (no passive regen) |

```
modified_regen = base_regen * max(0, 1 - [0.30, 0.60, 1.00][rank-1])
```

At rank 3, the player has zero passive HP regeneration. Sustain comes entirely from potions, skills, and Vampiric lifesteal builds. Forces active resource management.

#### PACT-08: Relentless Pursuit

Increases monster aggro range and leash range, and reduces alert duration. Enemies spot you sooner and chase you further.

| Rank | Heat | Effect |
|------|------|--------|
| 1 | 2 | Aggro range +25%, alert duration -50% |
| 2 | 4 | Aggro range +50%, alert duration -100% (instant), leash +25% |
| 3 | 6 | Aggro range +75%, leash +50%, all monsters skip Alert state |

```
modified_aggro = base_aggro * (1 + [0.25, 0.50, 0.75][rank-1])
modified_leash = base_leash * (1 + [0.00, 0.25, 0.50][rank-1])
modified_alert = base_alert * max(0, 1 - [0.50, 1.00, 1.00][rank-1])
```

At rank 3, every monster behaves like a Swarmer in terms of detection: instant aggro, 75% longer range, and they chase 50% further before giving up.

#### PACT-09: Hollow Ground

Safe spots provide reduced benefits. Shrinks the safe zone radius and reduces the regen bonus in safe areas.

| Rank | Heat | Effect |
|------|------|--------|
| 1 | 3 | Safe zone radius -40%, safe zone regen bonus -50% |
| 2 | 5 | Safe zone radius -70%, safe zone regen bonus -100% |
| 3 | 8 | Safe zones disabled entirely (entrance/exit are normal combat areas) |

At rank 3, there is no safe ground on any floor. The player must fight from entrance to exit with no breathing room. The auto-checkpoint still triggers at entrance/exit, but enemies can be present.

#### PACT-10: Dungeon's Favor

Boss enemies gain additional modifiers and increased stats. Affects bosses on every 10th floor and challenge room creatures.

| Rank | Heat | Effect |
|------|------|--------|
| 1 | 5 | Boss HP +50%, +1 modifier |
| 2 | 8 | Boss HP +100%, +2 modifiers, boss damage +25% |
| 3 | 12 | Boss HP +150%, +3 modifiers, boss damage +50%, boss speed +20% |

At rank 3, bosses have 2.5x normal HP, deal 1.5x damage, move 20% faster, and carry 3 additional modifiers on top of their base kit. This is the "prove you're ready for the next zone" pact.

### Heat Summary Table

| Pact | Max Ranks | Heat per Rank | Max Heat |
|------|-----------|---------------|----------|
| Swelling Horde | 5 | 3/5/7/10/15 | 15 |
| Iron Will | 4 | 3/5/8/12 | 12 |
| Sharpened Claws | 4 | 4/7/10/14 | 14 |
| Quicksilver Blood | 3 | 3/5/8 | 8 |
| Empowered Masses | 4 | 4/7/10/14 | 14 |
| Waning Light | 3 | 3/5/8 | 8 |
| Fading Vitality | 3 | 3/6/9 | 9 |
| Relentless Pursuit | 3 | 2/4/6 | 6 |
| Hollow Ground | 3 | 3/5/8 | 8 |
| Dungeon's Favor | 3 | 5/8/12 | 12 |
| **Total** | **35** | | **106** |

Note: Total max heat is 106 with all pacts at maximum. At 106 heat, reward bonuses are: +159% XP, +106% gold, +85% material drop rate, +53% equipment drop rate, +3 quality shift tiers.

### UI Design

The Pact Altar presents a vertical list of all 10 pacts. Each pact shows:
- Name and one-line description
- Current rank (clickable pips, 0 to max)
- Heat value contributed (changes as rank changes)
- Total heat displayed prominently at the top with reward preview

The total heat display shows a breakdown of all active reward bonuses: XP%, gold%, drop rates. The player can see exactly what they gain before entering the dungeon.

### Lore Integration

The Pact Altar is a crystalline structure in the town square, pulsing with magicule energy. The dungeon entity offers these "pacts" as challenges, daring adventurers to make the dungeon harder. In lore terms, the dungeon is raising the stakes because stronger, more challenged adventurers accumulate richer processed mana. The dungeon sweetens the deal with better loot because it knows the adventurer will eventually die deeper, yielding a bigger harvest.

The pact names reflect the dungeon's nature: Swelling Horde (the dungeon breeds more), Iron Will (the dungeon's creatures resist harder), Sharpened Claws (the dungeon's teeth grow sharper).

## Acceptance Criteria

- [ ] All 10 pacts are accessible from the Pact Altar in town
- [ ] Each pact can be toggled to any rank (0 through max) independently
- [ ] Pact settings persist across deaths and sessions (saved to character data)
- [ ] Total pact heat is calculated correctly as sum of active rank heat values
- [ ] XP bonus applies: `pact_heat * 1.5%` additive to kill XP
- [ ] Gold bonus applies: `pact_heat * 1.0%` additive to gold drops
- [ ] Material drop bonus applies as a multiplier on the base 25% material drop chance
- [ ] Equipment drop bonus applies as a multiplier on per-tier base drop rates
- [ ] Quality shift moves the floor range thresholds down by `floor(heat / 30) * 10` floors
- [ ] Swelling Horde modifies room budget formula at the correct multipliers per rank
- [ ] Iron Will multiplies monster HP after rarity multiplier at rank-specific values
- [ ] Sharpened Claws multiplies monster damage after all other multipliers
- [ ] Quicksilver Blood increases speed and reduces cooldown per rank
- [ ] Empowered Masses adjusts the rarity roll thresholds to match the table
- [ ] Waning Light reduces all elemental resistances by the flat pact penalty
- [ ] Fading Vitality reduces passive HP regen by the percentage per rank
- [ ] Relentless Pursuit increases aggro/leash range and reduces alert duration per rank
- [ ] Hollow Ground shrinks safe zone radius and disables safe zones at rank 3
- [ ] Dungeon's Favor adds HP, damage, speed, and modifiers to boss encounters
- [ ] Pact Altar UI shows all pacts, current ranks, heat total, and reward preview

## Implementation Notes

- Pact data should be stored on the character save (10 rank values, one per pact)
- Pact effects are applied as multipliers in the existing systems: MonsterSpawner (budget, rarity), MonsterModifier (stat multipliers), Resistances (penalty), StatSystem (regen), MonsterBehavior (aggro/leash/alert)
- The quality shift modifies `RollQuality()` in ItemGenerator by offsetting the floor range lookup
- Heat calculation is a pure function of the 10 rank values with no external dependencies
- The Pact Altar is a town interactable, not a menu -- it exists in the world as a physical object

## Open Questions

None.
