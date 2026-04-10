# Monster Modifiers System

## Summary

Monsters spawn with a rarity tier that determines their HP multiplier, reward multiplier, and how many modifiers they carry. Modifiers are special properties that alter a monster's combat stats or grant unique abilities. Pack composition is determined by a room budget system based on room area, with a fixed archetype distribution.

## Current State

**Spec status: LOCKED.** Implemented in `MonsterModifier.cs` and `MonsterSpawner.cs`.

## Design

### Rarity Tiers

| Rarity | Spawn Chance | HP Multiplier | Reward Multiplier | Description |
|--------|-------------|---------------|-------------------|-------------|
| Normal | 78% | 1.0x | 1.0x | Base stats, spawns in packs |
| Empowered | 20% | 2.0x | 1.5x | Stronger, always has 1 modifier |
| Named | 2% | 3.0x | 3.0x | Rare elite, 2-3 modifiers |

Rarity is rolled per monster via `MonsterSpawner.RollRarity()`. The roll thresholds are: Named < 0.02, Empowered < 0.22, everything else is Normal.

### Modifier Types

There are 10 modifier types. Each grants a distinct combat effect:

| Modifier | Effect |
|----------|--------|
| ExtraFast | 1.33x speed multiplier |
| ExtraStrong | 1.5x damage multiplier |
| StoneSkin | +80 defense bonus |
| FireEnchanted | Fire-based attacks or aura |
| ColdEnchanted | Cold-based attacks or aura |
| LightningAura | Lightning damage aura |
| Teleporting | Can teleport to reposition |
| ManaBurn | Drains player mana on hit |
| Summoner | Spawns additional minions |
| Vampiric | Heals on dealing damage |

Of the 10 modifiers, 3 have directly quantified stat effects in the current implementation:

| Stat Modifier | Multiplier/Bonus |
|---------------|-----------------|
| ExtraFast | 1.33x speed |
| ExtraStrong | 1.5x damage |
| StoneSkin | +80 defense |

All other modifiers return neutral values for speed (1.0x), damage (1.0x), and defense (+0) -- their effects are handled by separate gameplay systems (auras, teleport logic, mana drain, summoning, lifesteal).

### Modifier Stacking

When a monster has multiple modifiers, their effects combine:
- **Speed multipliers** are multiplicative (ExtraFast + another speed effect = compounding)
- **Damage multipliers** are multiplicative
- **Defense bonuses** are additive

This is computed by `MonsterModifiers.GetCombinedEffects()`.

### Modifier Count by Rarity and Zone

| Rarity | Modifier Count | Zone Scaling |
|--------|---------------|-------------|
| Normal | 0 | None |
| Empowered | 1 | Fixed at 1 regardless of zone |
| Named | 1 + zone/2, capped at 3 | Zone 1: 1 mod, Zone 2-3: 2 mods, Zone 4+: 3 mods |

Named monsters become progressively more dangerous in later zones, gaining additional modifiers as zone number increases. The formula is `min(1 + zone/2, 3)` (integer division).

Modifiers are rolled without replacement -- a monster cannot have the same modifier twice.

### Pack Composition

#### Room Budget

The number of monsters in a room is determined by room area:

```
budget = max(1, area / 12)
```

Where `area = roomWidth * roomHeight` (in tiles). Every room spawns at least 1 monster.

| Room Size (tiles) | Area | Budget |
|-------------------|------|--------|
| 4 x 4 | 16 | 1 |
| 6 x 6 | 36 | 3 |
| 8 x 8 | 64 | 5 |
| 10 x 10 | 100 | 8 |
| 12 x 12 | 144 | 12 |
| 15 x 15 | 225 | 18 |

#### Archetype Distribution

The budget is split across archetypes in fixed proportions:

| Archetype | Target % | Calculation |
|-----------|----------|-------------|
| Melee | 30% | `max(1, budget * 0.30)` |
| Swarmer | 35% | `budget * 0.35` |
| Ranged | 20% | `budget * 0.20` |
| Bruiser | 15% | Remainder after other allocations |

Melee always gets at least 1 monster. Bruiser receives whatever budget remains after the other three archetypes are allocated, which means the actual bruiser percentage may vary slightly due to integer truncation. Support archetype is not included in the standard pack mix.

**Example:** Budget of 10:
- Melee: max(1, 3) = 3
- Swarmer: 3
- Ranged: 2
- Bruiser: 10 - 3 - 3 - 2 = 2

## Implementation Notes

- **`scripts/game/monsters/MonsterModifier.cs`** -- Defines the `MonsterModifierType` enum (10 types) and the `MonsterModifiers` static class with `GetSpeedMultiplier()`, `GetDamageMultiplier()`, `GetDefenseBonus()`, `RollModifiers()` (random selection without replacement), and `GetCombinedEffects()` (aggregates all modifier stat effects).
- **`scripts/game/monsters/MonsterSpawner.cs`** -- Static class with `GetRoomBudget()`, `RollRarity()`, `GetHPMultiplier()`, `GetRewardMultiplier()`, `GetModifierCount()`, and `GetArchetypeMix()`.
- **`scripts/game/monsters/MonsterArchetype.cs`** -- Defines `MonsterRarity` enum (Normal, Empowered, Named) with inline comments documenting HP/reward multipliers and modifier counts.
