# Elemental Damage System

## Summary

The elemental damage system defines 7 damage types, each with distinct mitigation mechanics. Physical damage uses a defense-based damage reduction formula with diminishing returns, while the 6 elemental types (Fire, Water, Air, Earth, Light, Dark) use percentage-based resistances that erode as the player descends deeper into the dungeon. Past floor 75, ambient dark damage creates a survival gradient that acts as the dungeon's natural hard ceiling.

## Current State

**Spec status: LOCKED.** Implemented across `DamageType.cs`, `Resistances.cs`, and `ElementalCombat.cs`.

## Design

### Damage Types

| Damage Type | Description | Mitigation |
|-------------|-------------|------------|
| Physical | Standard weapon/melee damage | Defense DR (diminishing returns) |
| Fire | Heat, flame, combustion | Percentage resistance |
| Water | Cold, frost, ice effects | Percentage resistance |
| Air | Lightning, shock, wind effects | Percentage resistance |
| Earth | Physical-manifested force (rocks, tremors) | Percentage resistance |
| Light | Energy, radiance | Percentage resistance |
| Dark | Raw magicule corruption | Percentage resistance |

### Physical Damage: Defense DR Formula

Physical damage bypasses the resistance system entirely and uses the defense reduction formula from `StatSystem`:

```
effectiveStat = rawStat * (100 / (rawStat + 100))
defenseReduction = effectiveStat / 100
finalDamage = max(1, rawDamage * (1 - defenseReduction))
```

This creates diminishing returns: doubling defense does not double mitigation. At 100 defense, reduction is 50%. At 200 defense, reduction is ~66.7%. Physical damage always deals at least 1.

### Elemental Damage: Percentage Resistance

All non-Physical damage types use flat percentage resistance:

```
finalDamage = max(1, rawDamage * (1 - effectiveResistance / 100))
```

**Resistance caps:**

| Cap | Value |
|-----|-------|
| Maximum resistance | 75% |
| Minimum resistance (vulnerability) | -100% |

At 75% resistance, the target takes 25% of incoming elemental damage. At -100% resistance, the target takes 200% of incoming elemental damage.

### Floor-Based Resistance Penalty

Effective resistance degrades as the player descends. Each element's resistance is reduced by a penalty tied to floor depth:

```
penalty = max(0, floorNumber) / 2    (integer division)
effectiveResistance = max(baseResistance - penalty, -100)
```

After the penalty is applied, the result is clamped to the [-100, 75] range.

### Resistance Erosion by Floor Depth

The following table shows how a character with 50 base resistance in an element sees that resistance erode:

| Floor | Penalty (floor/2) | Effective Resistance (base 50) | Damage Taken (% of raw) |
|-------|-------------------|-------------------------------|------------------------|
| 1 | 0 | 50% | 50% |
| 10 | 5 | 45% | 55% |
| 25 | 12 | 38% | 62% |
| 50 | 25 | 25% | 75% |
| 75 | 37 | 13% | 87% |
| 100 | 50 | 0% | 100% |
| 150 | 75 | -25% | 125% |
| 200 | 100 | -50% (clamped) | 150% |
| 250 | 125 | -75% (clamped) | 175% |
| 300 | 150 | -100% (clamped) | 200% |

Even characters who invest heavily in resistance will eventually be overwhelmed by the floor penalty. At floor 100, a character with 50 base resistance has zero effective resistance. Past that point, elemental damage begins amplifying.

### Ambient Dark DPS

Starting at floor 76, the dungeon itself deals continuous Dark damage to all entities. This is the primary hard ceiling mechanic, representing lethal magicule density at extreme depth.

**Raw DPS formula:**

```
rawDarkDPS = max(0, (floorNumber - 75) * 2)
```

The raw DPS is then modified by the target's effective Dark resistance (including floor penalty):

```
effectiveDarkRes = clamp(target.Dark - floor/2, -100, 75)
actualDPS = max(0, rawDarkDPS * (1 - effectiveDarkRes / 100))
```

| Floor | Raw Dark DPS | Effective Dark Res (base 50) | Actual DPS |
|-------|-------------|------------------------------|------------|
| 75 | 0 | 12% | 0 |
| 76 | 2 | 12% | 1 |
| 100 | 50 | 0% | 50 |
| 125 | 100 | -12% | 112 |
| 150 | 150 | -25% | 187 |
| 175 | 200 | -37% | 275 |
| 200 | 250 | -50% | 375 |

The combination of increasing raw DPS and eroding Dark resistance creates an exponential survival difficulty curve. No realistic build can sustain indefinitely past floor ~200.

### Mage School to Damage Type Mapping

Each Mage elemental school maps to a `DamageType` for combat resolution. The mapping reflects the lore described in the magic and skills documentation:

| Mage School | Damage Type | Notes |
|-------------|-------------|-------|
| Fire | Fire | Direct mapping -- heat and flame |
| Water | Water | Cold/frost effects |
| Air | Air | Lightning/shock effects |
| Earth | Earth | Physical-manifested force |
| Light | Light | Energy/radiance |
| Dark | Dark | Raw magicule corruption |

### Crit Interaction

Critical hits are applied **after** resistance mitigation. The `ElementalCombat.CalculateDamage` method first calculates the mitigated damage (via defense DR or resistance percentage), then applies a 1.5x multiplier if the hit is a crit. The final result is always at least 1.

```
if (isCrit)
    mitigated = (int)(mitigated * 1.5f)
finalDamage = max(1, mitigated)
```

## Implementation Notes

- **`scripts/game/systems/DamageType.cs`** -- Enum defining all 7 damage types with inline comments describing each.
- **`scripts/game/systems/Resistances.cs`** -- `Resistances` class with per-element properties, `GetResistance(DamageType)` lookup, and `GetEffective(DamageType, int floorNumber)` which applies the floor penalty and clamps to -100.
- **`scripts/game/systems/ElementalCombat.cs`** -- Static class containing `CalculateDamage()` (branches on Physical vs elemental), `GetAmbientDarkDPS()` (raw formula), `GetAmbientDarkDamagePerSecond()` (applies target's Dark resistance), and the `ElementalDamageResult` struct.
- **`scripts/game/systems/StatSystem.cs`** -- `GetDefenseReduction(EntityData)` provides the diminishing-returns defense formula used for Physical damage.
