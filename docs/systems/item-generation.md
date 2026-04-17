# Item Generation System

## Summary

The item generation system produces equipment, materials, and consumables scaled to dungeon floor depth. Equipment drops as blank bases without affixes (the Blacksmith adds those). Item quality improves with floor depth, weapon/armor stats scale linearly with item level, and materials are tiered into 5 depth brackets. Loot drops use independent roll checks for equipment and materials, with rates that increase slightly by floor.

## Current State

**Spec status: LOCKED.** Implemented in `ItemGenerator.cs`.

## Design

### Floor Brackets = Tiers (single source of truth)

The game uses **five floor brackets**, which also serve as the **five item tiers** in the catalog. One number, one concept — no separate "floor range" and "tier" systems.

| Tier | Floor Range | Role |
|------|-------------|------|
| 1 | 1–10 | Entry / tutorial depths |
| 2 | 11–25 | Mid-early |
| 3 | 26–50 | Mid-late |
| 4 | 51–100 | Deep |
| 5 | 101+ | Endgame / infinite descent |

**These brackets apply to everything floor-scaled**: quality distribution, material tier, catalog item tier, monster drop tables. Any spec that references "floor range" or "tier" means this table. See [item-catalog.md](../inventory/item-catalog.md) for the catalog-tier binding and [monster-drops.md](monster-drops.md) for the drop-tier binding.

### Equipment Generation

When equipment is generated, the floor number determines both the **item level** and the **quality distribution**.

```
itemLevel = max(1, floorNumber)
```

Equipment is split 50/50 between weapons and armor. All equipment drops without affixes.

#### Quality Distribution by Floor Range

Floor brackets are unified across the project: **1–10 / 11–25 / 26–50 / 51–100 / 101+**. These are the same brackets used by [item-catalog.md](../inventory/item-catalog.md) (tier assignment) and [monster-drops.md](monster-drops.md) (material tier). A single shared bracketing so all three specs agree — floor 100 resolves to Tier 4, floor 101 is the first Tier 5 floor.

| Floor Range | Tier | Normal | Superior | Elite |
|-------------|------|--------|----------|-------|
| 1–10 | 1 | 100% | 0% | 0% |
| 11–25 | 2 | 80% | 20% | 0% |
| 26–50 | 3 | 60% | 35% | 5% |
| 51–100 | 4 | 30% | 50% | 20% |
| 101+ | 5 | 15% | 50% | 35% |

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

Crafting materials are organized into 5 tiers aligned to the shared floor brackets. Concrete material names are locked in [item-catalog.md](../inventory/item-catalog.md) § Materials; this table is the mechanical summary.

| Floor Range | Tier | Ore | Bone | Hide | Base Value |
|-------------|------|-----|------|------|------------|
| 1–10 | 1 | Iron Ore | Rough Bone | Rough Hide | 5 + floor |
| 11–25 | 2 | Steel Ingot | Standard Bone | Standard Hide | 10 + floor |
| 26–50 | 3 | Mithril Ore | Fine Bone | Fine Hide | 25 + floor |
| 51–100 | 4 | Orichalcum Ore | Masterwork Bone | Masterwork Hide | 50 + floor |
| 101+ | 5 | Dragonite Ore | Top-Shelf Bone | Top-Shelf Hide | 80 + floor |

Materials are stackable. The three types (Ore, Bone, Hide) are all available per tier; which type drops is biased by the enemy species's thematic generic per [monster-drops.md](monster-drops.md).

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
