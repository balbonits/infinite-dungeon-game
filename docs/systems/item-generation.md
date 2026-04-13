# Item Generation System

## Summary

The item generation system produces equipment, materials, and consumables scaled to dungeon floor depth. Equipment drops as blank bases without affixes (the Blacksmith adds those). Item quality improves with floor depth, weapon/armor stats scale linearly with item level, and materials are tiered into 5 depth brackets. Loot drops use independent roll checks for equipment and materials, with rates that increase slightly by floor.

## Current State

**Spec status: LOCKED.** Implemented in `ItemGenerator.cs`.

## Design

### Equipment Generation

When equipment is generated, the floor number determines both the **item level** and the **quality distribution**.

```
itemLevel = max(1, floorNumber)
```

Equipment is split 50/50 between weapons and armor. All equipment drops without affixes.

#### Quality Distribution by Floor Range

| Floor Range | Normal | Superior | Elite |
|-------------|--------|----------|-------|
| 1-9 | 100% | 0% | 0% |
| 10-24 | 80% | 20% | 0% |
| 25-49 | 60% | 35% | 5% |
| 50-74 | 40% | 45% | 15% |
| 75-99 | 25% | 50% | 25% |
| 100+ | 15% | 50% | 35% |

#### Quality Stat Bonuses

| Quality | Stat Bonus Range |
|---------|-----------------|
| Normal | No bonus |
| Superior | +10-20% |
| Elite | +25-40% |

The bonus is applied to the base damage (weapons) or base defense (armor) as a random multiplier within the range. The result is always at least `baseStat + 1` for non-Normal quality.

### Weapon Damage Scaling

Weapon base damage scales linearly with item level:

```
minDmg = max(5, (int)(1.4 * itemLevel + 3.6))
maxDmg = max(8, (int)(2.0 * itemLevel + 6.0))
baseDamage = random(minDmg, maxDmg)   // inclusive
```

| Item Level | Min Damage | Max Damage | Range |
|------------|-----------|-----------|-------|
| 1 | 5 | 8 | 5-8 |
| 10 | 17 | 26 | 17-26 |
| 25 | 38 | 56 | 38-56 |
| 50 | 73 | 106 | 73-106 |
| 75 | 108 | 156 | 108-156 |
| 100 | 143 | 206 | 143-206 |

Quality bonuses are applied on top of the rolled base damage.

### Armor Defense Scaling

Armor base defense scales linearly with item level:

```
minDef = max(2, (int)(0.8 * itemLevel + 2))
maxDef = max(4, (int)(1.2 * itemLevel + 4))
baseDefense = random(minDef, maxDef)   // inclusive
```

Body armor receives a 1.3x multiplier to base defense (primary slot bonus).

| Item Level | Min Defense | Max Defense | Body Slot (1.3x) |
|------------|-----------|-----------|------------------|
| 1 | 2 | 5 | 2-6 |
| 10 | 10 | 16 | 13-20 |
| 25 | 22 | 34 | 28-44 |
| 50 | 42 | 64 | 54-83 |
| 100 | 82 | 124 | 106-161 |

Armor generation covers 5 body slots: Head, Body, Arms, Legs, Feet (selected randomly). Accessories (Neck, Rings) and weapon slots (Main Hand, Off Hand, Ammo) are generated separately. See `docs/systems/equipment.md` for the full 10-slot / 19-position layout.

### Item Gold Value

```
baseValue = 10 + itemLevel * 3
qualityMultiplier = Normal: 1x, Superior: 2x, Elite: 4x
value = baseValue * qualityMultiplier
```

| Item Level | Normal | Superior | Elite |
|------------|--------|----------|-------|
| 1 | 13 | 26 | 52 |
| 25 | 85 | 170 | 340 |
| 50 | 160 | 320 | 640 |
| 100 | 310 | 620 | 1240 |

### Material Generation

Crafting materials are organized into 5 tiers based on floor depth:

| Floor Range | Tier | Example Materials | Base Value |
|-------------|------|-------------------|------------|
| 1-9 | Basic | Scrap Metal, Tattered Hide, Bone Fragment | 5 + floor |
| 10-24 | Low | Iron Ore, Monster Bone, Rough Gem | 10 + floor |
| 25-49 | Mid | Steel Ingot, Monster Hide, Fire Crystal | 25 + floor |
| 50-74 | High | Dark Iron Ore, Wyvern Bone, Arcane Dust | 50 + floor |
| 75+ | Rare | Enchanted Crystal, Dragon Scale, Mythril Shard | 80 + floor |

Each tier has 3 possible material names, selected randomly. Materials are stackable.

### Consumable Generation

Consumables are generated with fixed probabilities:

| Consumable | Drop Chance | Effect | Value |
|------------|------------|--------|-------|
| Health Potion | 45% | Restores 50 HP | 25 |
| Mana Potion | 40% | Restores 30 MP | 25 |
| Sacrificial Idol | 15% | Negates backpack item loss on death | 200 |

Consumables are not floor-scaled -- they have fixed stats regardless of depth. All consumables are stackable.

### Loot Drop Rates

When a monster is killed, equipment and material drops are rolled independently.

#### Equipment Drop

```
baseRate = Tier1: 8%, Tier2: 12%, Tier3: 18%
floorBonus = min(floorNumber * 0.1%, 5%)
dropChance = baseRate + floorBonus
```

| Monster Tier | Base Rate | At Floor 1 | At Floor 25 | At Floor 50+ (capped) |
|-------------|-----------|-----------|-------------|----------------------|
| Tier 1 | 8% | 8.1% | 10.5% | 13% |
| Tier 2 | 12% | 12.1% | 14.5% | 17% |
| Tier 3 | 18% | 18.1% | 20.5% | 23% |

The floor bonus caps at +5% (reached at floor 50).

#### Material Drop

If the equipment roll fails, materials are rolled at a **flat 25%** chance, independent of floor or monster tier. The material tier is determined by the current floor depth (see Material Generation table above).

#### Drop Priority

Equipment and material checks are sequential. If the equipment drop succeeds, it is returned and the material roll is skipped. If equipment fails, the material roll proceeds. This means a single kill can produce at most one item from `RollLootDrop()`.

### Crate / Container Loot

Crates and containers generate 1-3 items with the following distribution per item:

| Item Type | Chance |
|-----------|--------|
| Material | 85% |
| Equipment | 15% |

All items are generated at the current floor's item level and quality distribution.

## Implementation Notes

- **`scripts/game/inventory/ItemGenerator.cs`** -- Static class containing all generation logic. Key methods:
  - `GenerateEquipment(int floorNumber, Random rng)` -- Rolls quality, picks weapon or armor, applies scaling.
  - `GenerateWeapon(int itemLevel, ItemQuality quality, Random rng)` -- Linear damage scaling with quality bonus.
  - `GenerateArmor(int itemLevel, ItemQuality quality, Random rng)` -- Linear defense scaling, body armor 1.3x bonus.
  - `GenerateMaterial(int floorNumber, Random rng)` -- 5-tier floor-based material generation.
  - `GenerateConsumable(Random rng)` -- Fixed probability consumable roll.
  - `RollLootDrop(int monsterTier, int floorNumber, Random rng)` -- Equipment then material drop check. Returns null on no drop.
  - `GenerateCrateLoot(int floorNumber, Random rng)` -- 1-3 items, 85% material / 15% equipment.
  - `RollQuality(int floorNumber, Random rng)` -- Quality distribution lookup by floor range.
  - `ApplyQualityBonus(int baseStat, ItemQuality quality, Random rng)` -- Superior +10-20%, Elite +25-40%.
  - `CalculateValue(int itemLevel, ItemQuality quality)` -- Gold value formula.
- Items drop without affixes by design. The Blacksmith system handles affix application separately.
