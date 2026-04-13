# Item System

## Summary

Items include equipment, consumables, materials, and special objects. Equipment uses a Diablo 2-inspired prefix/suffix affix system. Monsters drop base items only — all magical properties come from deterministic crafting at the Blacksmith.

## Current State

**Spec status: LOCKED (data model).** Item data model, affix structure, and crafting rules are defined. Loot tables (SPEC-06b) and equipment stat effects (SPEC-06c) are separate tickets.

No items are implemented. The only item referenced in the design is the Sacrificial Idol (a consumable that negates backpack loss on death).

## Design

### Item Categories

| Category | Description | Examples |
|----------|-------------|---------|
| **Equipment** | Weapons, armor, accessories. Affect stats and combat. | Iron Sword, Leather Helm, Gold Ring |
| **Consumables** | Single-use effects. Destroyed on use. | Sacrificial Idol, Health Potion, Spell Scroll |
| **Materials** | Crafting ingredients for the Blacksmith. | Iron Ore, Monster Bone, Fire Crystal |
| **Special** | Quest items, keys, unique rewards. | Boss Key, Dungeon Map Fragment |

### Known Items

| Item | Type | Source | Effect |
|------|------|--------|--------|
| Sacrificial Idol | Consumable | Item Shop (town) | Negates backpack item loss on death. Single use. |

### Equipment Slots

Characters have the following equipment slots. Some item types are class-specific.

| Slot | Warrior | Ranger | Mage |
|------|---------|--------|------|
| Head | Helmets | Bands | Crowns |
| Body/Torso | Heavy armor | Light armor | Magic robes |
| Neck | Necklaces & chokers | Necklaces & chokers | Necklaces & chokers |
| Rings | 10 ring slots | 10 ring slots | 10 ring slots |
| Arms (shoulders to forearms) | Gauntlets | Braces | Bangles |
| Legs (hips to thighs) | Greaves | Chausses/Breeches | Leggings |
| Feet | Boots | Shoes | Sandals |
| Main hand | Melee weapons (bladed, blunt, polearms) | Ranged weapons (bows, crossbows, thrown, firearms) | Staves, wands |
| Off hand | Shields | Defensive melee (knives, small bucklers, claw gauntlets) | Grimoires, magic-enabled defensive items |
| Ammo slot | — | Magazine items (quivers, mags, projectile bags, bandoliers) | — |

**Notes:**
- Neck and rings are shared across all classes
- 10 ring slots is notably generous — encourages build diversity through ring stacking
- Ranger ammo is unlimited but requires a magazine item equipped in the ammo slot
- Mage offhand items (grimoires, ward orbs) passively enhance existing skills — no dedicated base skill

### Item Data Model

Every item in the game follows this structure:

```
Item {
  id:               string    // Unique identifier (UUID)
  name:             string    // Display name (e.g., "Iron Sword")
  type:             enum      // Equipment | Consumable | Material | Special
  slot:             enum      // Head | Body | Neck | Ring | Arms | Legs | Feet |
                              // MainHand | OffHand | Ammo | None
  class_affinity:    enum     // Warrior | Ranger | Mage | null (universal)
                              // No restriction — any class can equip. +25% stat bonus when matched.
  item_level:       int       // Floor it dropped on — gates affix availability
  base_quality:     enum      // Normal | Superior | Elite
  base_stats:       dict      // { damage?, defense?, stat_bonus? }
  affixes:          Affix[]   // Max 3 prefix + 3 suffix = 6 total
  stack_count:      int       // For stackable items (consumables, materials). 1 for equipment.
}

Affix {
  name:   string    // Display name (e.g., "of Strength", "Fiery")
  type:   enum      // Prefix | Suffix
  stat:   string    // Which stat/effect it modifies
  value:  int/float // Deterministic value based on affix tier
  tier:   int       // Affix power tier (gated by item_level)
}
```

### Base Quality

Monster drops come in three base quality tiers, determined by floor depth:

| Quality | Floor Range | Stat Range | Description |
|---------|-------------|------------|-------------|
| Normal | All floors | Base values | Standard item. Most common drop. |
| Superior | Floor 10+ | +10-20% base stats | Better-crafted version. Less common. |
| Elite | Floor 25+ | +25-40% base stats | Exceptional craftsmanship. Rare drop. |

Quality affects only the item's **base stats** (damage, defense). It does not affect affix slots or affix power.

### Affix System

Affixes are the core of item depth. They are **never** found on drops — all affixes are added by the Blacksmith through deterministic crafting.

**Rules:**
- Maximum **3 prefixes + 3 suffixes** per item (6 total affixes)
- Player picks the **exact affix** they want and pays materials. No RNG.
- **Item level gates affix tiers** — a floor 5 item can only receive low-tier affixes. A floor 50 item can receive much stronger ones.
- Affixes cannot be removed once applied (choose carefully)
- Adding an affix costs materials + gold, scaling with affix tier

**Affix tier gating:**

| Affix Tier | Min Item Level | Power Level |
|------------|---------------|-------------|
| Tier 1 | 1 | Weak (+1-3 flat, +1-3%) |
| Tier 2 | 10 | Moderate (+4-8 flat, +4-8%) |
| Tier 3 | 25 | Strong (+9-15 flat, +9-15%) |
| Tier 4 | 50 | Powerful (+16-25 flat, +16-25%) |
| Tier 5 | 75 | Elite (+26-40 flat, +26-40%) |
| Tier 6 | 100+ | Legendary (+41+ flat, +41+%) |

**Affix categories:**

| Category | Prefix Examples | Suffix Examples |
|----------|----------------|-----------------|
| Offensive | Keen (+flat damage), Vicious (+% damage), Swift (+attack speed) | of Striking (+crit chance), of Ruin (+crit damage) |
| Defensive | Sturdy (+defense), Fortified (+HP), Warding (+damage resist) | of the Bear (+max HP), of Evasion (+dodge) |
| Utility | Energizing (+mana), Flowing (+mana regen) | of Swiftness (+move speed), of Learning (+XP bonus) |
| Elemental | Fiery (+fire damage), Frozen (+frost damage), Shocking (+lightning) | of Flame Resist, of Frost Resist, of Storm Resist |

*Affix definitions are implemented in `AffixDatabase.cs` (28 affixes across tiers 1-4). The categories above are representative — see code for the complete list.*

### Crafting at the Blacksmith

The Blacksmith is the only source of magical (affixed) equipment. Crafting is **fully deterministic**.

**Crafting flow:**
1. Bring a base item (dropped from monsters)
2. Choose an affix from the available list (filtered by item level and slot)
3. Pay the material + gold cost
4. Affix is permanently applied to the item
5. Repeat up to the 3 prefix + 3 suffix maximum

**Material sources:**
- Monster drops (common materials)
- Boss kills (rare materials, first-kill only)
- Treasure room chests (floor-appropriate materials)
- Recycling off-affinity gear at the Blacksmith (see below)

**Materials are generic.** Crafting materials are not species-specific — there is no "Goblin Bone" vs. "Skeleton Bone." Materials are tiered by floor depth (e.g., "Iron Ore" from floors 1-10, "Mithril Ore" from floors 25+). Any enemy on a given floor range drops from the same material pool.

**Recycling:** Bring off-affinity gear you don't want → Blacksmith breaks it down into materials. Material yield scales with item level and quality. This ensures no drop feels wasted.

### Loot System

Loot tables and drop rates are defined in SPEC-06b (separate ticket). High-level rules:

- **Monsters drop base items only** (no affixes, no magic)
- **Drop quality scales with floor depth** (deeper = more Superior/Elite drops)
- **Class-affinity items drop for any class** (+25% bonus if matched, recyclable if not)
- **Materials drop alongside equipment** (monsters also drop crafting materials directly)
- **Boss first-kills drop guaranteed rare materials**
- **Treasure rooms contain material chests** (no equipment, just crafting ingredients)

### Item Color

Item color uses the **unified color gradient** (see [color-system.md](../systems/color-system.md)). There are no discrete rarity tiers (no "Rare" or "Epic" labels). Instead, an item's color is computed from the level gap between the player and the item's effective level:

- Warm colors (orange/red) = item is above your level, powerful but bonuses may be gated
- Green/yellow = item is appropriate for your level
- Cool colors (blue/cyan) = item is below your level, losing relevance
- Grey = item is far below your level, candidate for recycling at the Blacksmith

The same item shifts color as the player levels. No equipment restrictions on wearing items — any item can be equipped, but abilities/bonuses may be locked if the player is underleveled.

## Resolved Questions

| Question | Decision |
|----------|----------|
| How does equipment affect stats? | Base stats (damage/defense) + up to 6 affixes (3 prefix + 3 suffix). See affix system above. |
| Should items have durability? | No. Items are permanent once crafted. Simplicity over maintenance mechanics. |
| How does the Blacksmith crafting system work? | Deterministic: pick exact affix, pay materials + gold. No RNG. |
| What's the loot table structure? | Deferred to SPEC-06b. Base items + materials from monsters, rare mats from bosses/treasure rooms. |
| Should items be tradeable? | No. Single-player game. No trading. |

### Loot Drop Tables (SPEC-06b)

#### Equipment Drop Rates

Every enemy kill has a chance to drop a base item. Drop chance scales with enemy tier and floor depth.

```
drop_chance(tier) = base_rate(tier) + floor_bonus
base_rate: Tier 1 = 8%, Tier 2 = 12%, Tier 3 = 18%
floor_bonus = floor_number * 0.1% (caps at +5%)
```

| Floor | Tier 1 Drop % | Tier 2 Drop % | Tier 3 Drop % |
|-------|--------------|--------------|--------------|
| 1 | 8.1% | 12.1% | 18.1% |
| 10 | 9.0% | 13.0% | 19.0% |
| 25 | 10.5% | 14.5% | 20.5% |
| 50+ | 13.0% (capped) | 17.0% | 23.0% |

#### Base Quality Distribution

When an item drops, its quality tier is rolled:

| Floor Range | Normal | Superior | Elite |
|-------------|--------|----------|-------|
| 1–9 | 100% | 0% | 0% |
| 10–24 | 80% | 20% | 0% |
| 25–49 | 60% | 35% | 5% |
| 50–74 | 40% | 45% | 15% |
| 75–99 | 25% | 50% | 25% |
| 100+ | 15% | 50% | 35% |

#### Item Type Distribution

When an equipment item drops, the slot is rolled with equal weight across all valid slots for the enemy's floor. Class-affinity items drop for any class — +25% bonus if matched, recyclable at Blacksmith if not.

#### Material Drops

Every enemy kill has a separate material drop chance (independent of equipment drops):

```
material_drop_chance = 25% (flat, all enemies)
material_tier = floor-appropriate (higher floors drop better materials)
```

Boss first-kills: guaranteed 3-5 rare materials (one-time only).
Treasure room chests: 5-8 materials of floor-appropriate tier.

### Equipment Stat Effects (SPEC-06c)

#### How Defense Works

Equipment defense reduces incoming damage via a diminishing returns formula (same curve as stats):

```
effective_defense = total_defense * (100 / (total_defense + 100))
damage_reduction_percent = effective_defense
final_damage = incoming_damage * (1 - damage_reduction_percent / 100)
```

| Total Defense | Damage Reduction | 100 Incoming → Takes |
|--------------|-----------------|---------------------|
| 10 | 9.1% | 91 |
| 25 | 20.0% | 80 |
| 50 | 33.3% | 67 |
| 100 | 50.0% | 50 |
| 200 | 66.7% | 33 |

Defense never reaches 100% reduction — enemies always deal some damage.

#### How Equipment Stats Stack

All equipment stats are additive within their category:

- **Flat bonuses** from all equipped items are summed, then added to base stats
- **Percentage bonuses** from all equipped items are summed into one total %, then applied multiplicatively
- **Affix bonuses** follow the same rules — they're just stats on items

```
total_flat_bonus = sum(all equipped item flat bonuses)
total_percent_bonus = sum(all equipped item percent bonuses)
final_stat = (base_stat + total_flat_bonus) * (1 + total_percent_bonus / 100)
```

**10 ring slots** can stack the same affix type across all rings. This is intentional — ring stacking is a major build customization vector.

#### Weapon Damage

Main hand weapon provides base damage used in the STR melee damage formula (see [stats.md](stats.md)):

```
total_melee_damage = (base_weapon_damage + flat_melee_bonus_from_STR) * (1 + percent_melee_boost_from_STR / 100)
```

Weapon base damage scales with item level and quality:

| Item Level | Normal Base Damage | Superior | Elite |
|-----------|-------------------|----------|-------|
| 1 | 5–8 | 6–10 | 7–12 |
| 10 | 15–22 | 18–26 | 20–30 |
| 25 | 35–50 | 42–60 | 47–68 |
| 50 | 70–100 | 84–120 | 94–135 |
| 100 | 140–200 | 168–240 | 189–270 |

*Values are locked. Tunable during playtesting only — the formulas and scaling curves are final.*

## Resolved Questions

| Question | Decision |
|----------|----------|
| Equipment stats | Flat + % bonuses, additive within category, multiplicative across categories |
| Durability | No. Items are permanent. |
| Crafting system | Deterministic affix application at Blacksmith. See above. |
| Loot tables | Floor-scaled drop rates, quality distribution, separate material drops |
| Trading | No. Single-player. |
| Drop rate tables | Defined above. Tier-based + floor bonus. |
| Material costs per affix | Scales with affix tier. Exact costs are implementation-phase balancing. |
| Full affix list | Representative categories defined. Full list is implementation-phase content. |
| Defense formula | Diminishing returns, same K=100 curve as stats. |
