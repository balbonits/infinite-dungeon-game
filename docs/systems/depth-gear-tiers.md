# Depth Gear Tiers

## Summary

Equipment base quality expands beyond Normal/Superior/Elite with additional rarity tiers unlocked at deep floor milestones (50, 100, 150). Higher-tier equipment has stronger base stats, more affix slots, and distinct visual indicators. This system rewards deep dungeon pushes with meaningfully better crafting bases.

## Current State

**Spec status: LOCKED (structure).** Tier unlock floors and slot counts are final. Exact stat ranges are implementation-phase balancing.

Depends on: item system (locked), affix system (locked), floor scaling (locked), loot tables (locked).

## Design

### Tier Progression

The base quality system from [items.md](../inventory/items.md) defines three tiers (Normal, Superior, Elite). Depth gear tiers extend this with three additional tiers that only drop on deep floors:

| Tier | Min Floor | Base Stat Bonus | Max Affixes | Visual Indicator |
|------|-----------|-----------------|-------------|------------------|
| Normal | 1 | Baseline | 6 (3+3) | No border |
| Superior | 10 | +10-20% | 6 (3+3) | Silver border |
| Elite | 25 | +25-40% | 6 (3+3) | Gold border |
| **Masterwork** | **50** | **+45-60%** | **8 (4+4)** | **Teal border + shimmer** |
| **Mythic** | **100** | **+65-85%** | **10 (5+5)** | **Purple border + particle trail** |
| **Transcendent** | **150** | **+90-120%** | **12 (6+6)** | **Red border + ambient glow** |

### Why Three New Tiers

The infinite dungeon needs infinite incentives. After floor 25 (where Elite unlocks), the player has no equipment quality milestones for 75+ floors. Depth gear tiers create three aspirational goals at floors 50, 100, and 150 — each representing a significant power leap worth grinding toward.

### Drop Rates

Higher-tier items are progressively rarer. Drop rates follow the same floor-based distribution table from items.md, extended:

| Floor Range | Normal | Superior | Elite | Masterwork | Mythic | Transcendent |
|-------------|--------|----------|-------|------------|--------|--------------|
| 1-9 | 100% | 0% | 0% | 0% | 0% | 0% |
| 10-24 | 80% | 20% | 0% | 0% | 0% | 0% |
| 25-49 | 60% | 35% | 5% | 0% | 0% | 0% |
| 50-74 | 35% | 40% | 20% | 5% | 0% | 0% |
| 75-99 | 20% | 35% | 30% | 15% | 0% | 0% |
| 100-149 | 10% | 25% | 35% | 25% | 5% | 0% |
| 150+ | 5% | 15% | 30% | 30% | 15% | 5% |

### Affix Slot Expansion

The core affix system (items.md) caps at 3 prefix + 3 suffix = 6 affixes. Higher-tier bases unlock additional affix slots:

| Tier | Prefix Slots | Suffix Slots | Total |
|------|-------------|-------------|-------|
| Normal / Superior / Elite | 3 | 3 | 6 |
| Masterwork | 4 | 4 | 8 |
| Mythic | 5 | 5 | 10 |
| Transcendent | 6 | 6 | 12 |

Additional affix slots are the primary power differentiator. A Transcendent item fully affixed has double the affixes of a Normal item — this is the reward for pushing to floor 150+.

### Base Stat Scaling

Base stat bonuses apply to the item's base damage/defense before affixes:

```
final_base_stat = base_stat * (1 + tier_bonus / 100)
```

Where `tier_bonus` is rolled within the tier's range (e.g., Masterwork rolls between +45% and +60%).

### Crafting at the Blacksmith

Higher-tier items cost proportionally more to craft:

```
affix_cost_multiplier(tier):
  Normal:       1.0x
  Superior:     1.2x
  Elite:        1.5x
  Masterwork:   2.0x
  Mythic:       3.0x
  Transcendent: 5.0x
```

This multiplier applies to both material and gold costs for adding affixes. Crafting a fully-affixed Transcendent item is a massive material investment — the ultimate endgame goal.

### Interaction with Other Systems

- **Zone Saturation (zone-saturation.md):** Quality shift from saturation can push effective floor for quality rolls, making Masterwork drops possible slightly earlier than floor 50 at high saturation.
- **Item Color (color-system.md):** The color gradient still applies independently of tier. A Transcendent item 20 levels below you still shows as blue/grey.
- **Recycling:** Higher-tier items yield proportionally more materials when recycled at the Blacksmith (`tier_multiplier * base_yield`).
- **Dungeon Pacts (dungeon-pacts.md):** The "Fortune" pact line could shift quality rolls upward, stacking with floor depth.

### Design Rationale

**Why floor milestones, not level milestones?** Floor depth is the player's commitment metric. Reaching floor 150 means the player has pushed deep and can survive there. Level alone doesn't prove this (a player could grind shallow floors to high level without pushing). Floor-gated drops reward exploration and risk-taking.

**Why not infinite tiers?** Three additional tiers (50/100/150) provide clear goals without overwhelming the player with too many quality levels to track. The jumps are meaningful: floor 50 is mid-game, floor 100 is late-game, floor 150 is endgame.

## Acceptance Criteria

- [ ] Three new base quality tiers exist: Masterwork (floor 50+), Mythic (floor 100+), Transcendent (floor 150+)
- [ ] Higher tiers have proportionally stronger base stats (+45-60%, +65-85%, +90-120%)
- [ ] Masterwork allows 4+4 affixes, Mythic 5+5, Transcendent 6+6
- [ ] Drop distribution tables are extended to include new tiers at appropriate floors
- [ ] Crafting costs scale with tier multiplier (2x, 3x, 5x)
- [ ] Recycling yields scale with tier
- [ ] Visual indicators distinguish each tier in inventory UI
- [ ] Quality shift from saturation can influence tier roll outcomes

## Open Questions

None.
