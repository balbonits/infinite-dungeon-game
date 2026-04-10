# Critical Hit System

## Summary

The critical hit system assigns every weapon type a base crit chance, then scales it through affix modifiers. When a crit occurs, the hit deals bonus damage based on a crit multiplier that starts at 150% and can be increased without cap. Crits are resolved independently from elemental resistance -- resistance reduces damage first, then the crit multiplier amplifies the mitigated result.

## Current State

**Spec status: LOCKED.** Implemented in `CritSystem.cs`. Crit-elemental interaction handled in `ElementalCombat.cs`.

## Design

### Weapon Types and Base Crit Chances

There are 16 weapon types organized by class affinity. Each has a distinct base crit chance:

| Weapon Type | Class | Base Crit % |
|-------------|-------|-------------|
| Dagger | Warrior | 8% |
| Sword | Warrior | 6% |
| Axe | Warrior | 4% |
| Club | Warrior | 3% |
| Mace | Warrior | 4% |
| Spear | Warrior | 5% |
| Halberd | Warrior | 3% |
| Bow | Rogue | 6% |
| Crossbow | Rogue | 8% |
| ThrowingKnife | Rogue | 7% |
| ThrowingAxe | Rogue | 4% |
| Pistol | Rogue | 5% |
| Rifle | Rogue | 9% |
| Staff | Mage | 4% |
| Wand | Mage | 7% |
| Unarmed | Default | 3% |

Fast, precise weapons (Dagger, Crossbow, Rifle, Wand) have higher base crit. Heavy, slow weapons (Club, Halberd) have lower base crit.

### Crit Chance Formula

```
finalCritChance = baseCrit * (1 + increasedCritPercent / 100) + flatCritBonus
```

- `baseCrit` -- from weapon type table above
- `increasedCritPercent` -- percentage-based scaling (multiplicative with base)
- `flatCritBonus` -- added after scaling (additive)
- Result is clamped to **[0%, 75%]**

**Example:** A Dagger (8% base) with +50% Increased Crit Chance and +3% Flat Crit:
```
8 * (1 + 50/100) + 3 = 8 * 1.5 + 3 = 12 + 3 = 15% crit chance
```

### Crit Multiplier

```
critMultiplier = 150 + bonusCritMulti
```

- Base multiplier is **150%** (1.5x damage)
- `bonusCritMulti` is added directly (e.g., +50 bonus = 200% = 2.0x)
- **No cap** on crit multiplier

When a crit occurs, the base damage is multiplied:
```
critDamage = max(1, baseDamage * critMultiplier / 100)
```

### Crit + Elemental Interaction

Critical hits and elemental resistance are resolved in a specific order within `ElementalCombat.CalculateDamage`:

1. **Calculate raw damage** (from weapon, stats, etc.)
2. **Apply mitigation** (Physical: defense DR; Elemental: resistance percentage)
3. **Apply crit multiplier** (if the hit is a crit, multiply the mitigated result by 1.5x)
4. **Floor at 1** (minimum damage is always 1)

This means resistance reduces damage before the crit amplifies it. A crit against a highly resistant target still benefits from the multiplier, but the base it multiplies is already reduced.

**Example:** 100 raw Fire damage vs 50% Fire resistance, crit hit:
```
mitigated = 100 * (1 - 50/100) = 50
crit = 50 * 1.5 = 75 final damage
```

Note: The 1.5x multiplier applied in `ElementalCombat.CalculateDamage` when `isCrit` is true uses the default 150% base. For custom crit multipliers (via affixes), the `CritSystem.RollCrit` method calculates the full multiplied damage before passing it to the elemental system.

### Affix Types

Equipment can roll the following crit-related affixes:

| Affix | Effect | Scaling |
|-------|--------|---------|
| Increased Crit Chance | Multiplies base crit by a percentage | `baseCrit * (1 + value/100)` |
| Flat Crit | Adds flat crit chance after percentage scaling | `+ value%` to final chance |
| Crit Multiplier | Adds bonus to the 150% base multiplier | `150 + value` total multiplier |

Increased Crit Chance is most effective on weapons with high base crit (Rifle, Dagger, Crossbow). Flat Crit is equally valuable regardless of weapon type. Crit Multiplier has no cap, making it the primary scaling stat for crit-focused builds in late game.

### Crit Chance Cap Reasoning

The 75% cap ensures that crit builds remain probabilistic rather than deterministic. Even a fully optimized crit build will have 1 in 4 attacks be non-crits, maintaining gameplay variance.

## Implementation Notes

- **`scripts/game/systems/CritSystem.cs`** -- Contains the `WeaponType` enum (16 types), `CritSystem` static class with `GetBaseCritChance()`, `CalculateCritChance()`, `CalculateCritMultiplier()`, and `RollCrit()`. Also defines the `CritResult` struct.
- **`scripts/game/systems/ElementalCombat.cs`** -- `CalculateDamage()` accepts an `isCrit` parameter and applies a 1.5x multiplier to the post-mitigation damage when true.
- `RollCrit()` uses `Random.NextDouble() * 100.0 < critChance` for the crit roll, ensuring uniform distribution.
- Constants: `DefaultCritMultiplier = 150f`, `MaxCritChance = 75f`.
