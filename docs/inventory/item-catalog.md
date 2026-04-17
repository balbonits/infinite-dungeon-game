# Item Catalog

## Summary

The concrete base-item catalog for the game. Every base item a player can drop, buy, craft from, or find in a chest is enumerated here by ID. This is the content-authoring source of truth; `ItemDatabase.cs` is its mechanical mirror. No uniques are defined here — they are the domain of a planned Blacksmith Forge RNG system (TBD — Forge RNG system) and will get their own spec.

## Current State

**Spec status: DRAFT.** Content is locked at structure and voice; exact stat numbers are implementation-phase balancing (formulas live in [item-generation.md](../systems/item-generation.md)). `ItemDatabase.cs` currently contains placeholder items (ITEM-01 replaces them with this catalog).

Depends on:
- [items.md](items.md) — data model, affix rules, sell pricing, stacking (LOCKED).
- [equipment.md](../systems/equipment.md) — slot layout, class affinity, ammo rules.
- [item-generation.md](../systems/item-generation.md) — floor → item-level, stat scaling.
- [depth-gear-tiers.md](../systems/depth-gear-tiers.md) — BaseQuality ladder and affix-slot expansion at floors 50/100/150.
- [monster-drops.md](../systems/monster-drops.md) — which species drop which slots.

## Design

### Table of Contents

1. [Design philosophy: class voice](#design-philosophy-class-voice)
2. [Tier ladder](#tier-ladder)
3. [Class Affinity rule](#class-affinity-rule)
4. [Item ID convention](#item-id-convention)
5. [Catalog density](#catalog-density)
6. [Armor](#armor)
   - [Head](#armor--head-15-items)
   - [Body](#armor--body-15-items)
   - [Arms](#armor--arms-15-items)
   - [Legs](#armor--legs-15-items)
   - [Feet](#armor--feet-15-items)
7. [Main Hand weapons](#main-hand-weapons-40-items)
   - [Warrior: sword / axe / hammer](#warrior-main-hand)
   - [Ranger: short bow / long bow / crossbow](#ranger-main-hand)
   - [Mage: staff / wand](#mage-main-hand)
8. [Off Hand](#off-hand-30-items)
   - [Warrior: shields](#warrior-off-hand-shields)
   - [Ranger: defensive melee](#ranger-off-hand-defensive-melee)
   - [Mage: spellbooks](#mage-off-hand-spellbooks)
9. [Ammo / Quivers](#ammo--quivers-9-items)
10. [Neck](#neck-15-items)
11. [Ring](#ring-40-items)
12. [Consumables](#consumables-28-items)
13. [Materials](#materials-22-items)
14. [Uniques — deferred](#uniques--deferred)

---

### Design philosophy: class voice

Every class-flavored item is named in that class's voice. The voice is the character-flavor hook; it turns a flat stat stick into a reminder of who the item belongs to. Neutral items (Neck, Ring, Consumables, Materials) use a clean-fantasy voice because they're sold and handled by the Guild Maid, not the player's class.

| Voice | Personality | Naming style | Example |
|---|---|---|---|
| **Warrior** | Brash, loud, simple-minded, charismatic. Names gear like a kid describing his toys — loud, short, boastful. | Short. Superlatives. Simple nouns (Sword, Shield, Helmet). Adjective-heavy. | Mega Sword, Super Helmet, Bash Shield |
| **Ranger** | Dark, brooding tinkerer with dry/whimsical humor. Names gear like a mechanic names tools. | Short, functional, slightly dry. Mild self-deprecation. Often adjective + object. | Cold Quiver, Shortie, Stabby, Fancy Vest |
| **Mage** | Young, prideful, scholarly, verbose. Tries hard with thesaurus words and occasionally flubs them. | Long, latinate, pretentious. Occasional redundancy/malaprop. | The Novice's Coronet of Preliminary Warding |
| **Neutral** | Clean-fantasy shopkeeper voice. No personality. | Functional. "Ring of Strength", "Iron Chain", "Mithril Ore". | Ring of Strength, Health Potion |

Every non-neutral item **must** read in its class voice. If a name reads bland, it's wrong — the whole point is that the Warrior's "Mega Armor" feels like it's worn by an excitable meathead.

### Tier ladder

Every equipment slot has exactly **5 tiers**. Tier maps to floor bracket and to a voice-specific adjective ladder. Floor brackets are the **single shared bracketing** used across the project — see [item-generation.md](../systems/item-generation.md) § Floor Brackets = Tiers for the source-of-truth table.

| Tier | Floor bracket | Warrior adj | Ranger adj | Mage descriptor |
|---|---|---|---|---|
| 1 | 1–10 | Iron | Rough | Novice |
| 2 | 11–25 | Steel | Standard | Apprentice |
| 3 | 26–50 | Hard | Fine | Adept |
| 4 | 51–100 | Super | Fancy | Master |
| 5 | 100+ | Mega | Top-Shelf | Archmage |

Tier gates **which item ID drops at which floor** (see [monster-drops.md](../systems/monster-drops.md) slot-roll step 4). Tier does **not** gate whether a player can equip the item — any item can be equipped at any level; the color-gradient system makes appropriateness obvious (see [items.md](items.md#item-color)).

Tier is separate from **BaseQuality** (Normal / Superior / Elite / Masterwork / Mythic / Transcendent) — quality is rolled at drop per [depth-gear-tiers.md](../systems/depth-gear-tiers.md) and applies multiplicatively on the tier's base stats. A Tier 2 Steel Helmet rolled Elite is stronger than a Tier 2 Steel Helmet rolled Normal; a Tier 3 Hard Helmet rolled Normal is still stronger again than the Elite T2 (usually).

### Class Affinity rule

Every class-voiced item has `ClassAffinity` set to its class. Every neutral item has `ClassAffinity = null`. **No item is class-locked.** The Warrior can wear a Mage's robes; they just do not get the +25% stat bonus. Per [equipment.md](../systems/equipment.md) §"Class Affinity (Replaces Class-Lock)."

| Item category | ClassAffinity |
|---|---|
| Warrior-voice equipment | `Warrior` |
| Ranger-voice equipment | `Ranger` |
| Mage-voice equipment | `Mage` |
| Neck, Ring | `null` |
| Consumables | `null` |
| Materials | `null` |

This is re-stated from [equipment.md](../systems/equipment.md). Re-emphasize: a Mage equipping "Mega Armor" still gets the base defense, just without the +25% affinity multiplier. Nothing is gated.

### Item ID convention

Item IDs are stable, kebab-case, class-voice-agnostic, and derived from the catalog below. Format:

```
{slot_key}_{class_key}_{archetype_key}_t{tier}
```

Examples:
- `head_warrior_helmet_t1` → "Iron Helmet"
- `mainhand_ranger_shortbow_t3` → "Quality Shortie"
- `offhand_mage_spellbook_t5` → "The Archmage's Supreme Compendium of Transcendent Theurgy"
- `neck_t2_str` → "Steel Chain of Strength" (neutral — class_key is omitted for neutral items and tier is explicit)
- `ring_t3_movespeed` → "Mithril Band of Swiftness"
- `consumable_hp_large` → "Large Health Potion"
- `material_ore_t1` → "Iron Ore"
- `material_sig_skeleton` → "Bone Dust"

IDs are the wire format in save files and in `MonsterDropTables`. **Display names** are locked in the tables below.

### Catalog density

Target: 200–280 items. Final count from the tables below:

| Category | Count |
|---|---|
| Armor (5 slots × 5 tiers × 3 classes) | 75 |
| Main Hand (Warrior 3 + Ranger 3 + Mage 2 archetypes × 5 tiers) | 40 |
| Off Hand (3 classes × 5 tiers × 2 variants) | 30 |
| Ammo / Quivers (9 imbue types, no tiers) | 9 |
| Neck (5 tiers × 3 variants) | 15 |
| Ring (5 tiers × 8 variants: 4 stats + 4 combat) | 40 |
| Consumables | 28 |
| Materials (15 generic + 7 signature) | 22 |
| **Total** | **259** |

---

## Armor

All five armor slots use the same pattern: 5 tiers × 3 class voices = 15 items per slot. Base defense scales with item level per [item-generation.md](../systems/item-generation.md). The Body slot gets the 1.3x primary-slot defense multiplier per `item-generation.md`. No affixes on drop.

### Armor — Head (15 items)

| ID | Display Name | Tier | ClassAffinity |
|---|---|---|---|
| `head_warrior_helmet_t1` | Iron Helmet | 1 | Warrior |
| `head_warrior_helmet_t2` | Steel Helmet | 2 | Warrior |
| `head_warrior_helmet_t3` | Hard Helmet | 3 | Warrior |
| `head_warrior_helmet_t4` | Super Helmet | 4 | Warrior |
| `head_warrior_helmet_t5` | Mega Helmet | 5 | Warrior |
| `head_ranger_hood_t1` | Rough Hood | 1 | Ranger |
| `head_ranger_hood_t2` | Standard Hood | 2 | Ranger |
| `head_ranger_hood_t3` | Fine Hood | 3 | Ranger |
| `head_ranger_hood_t4` | Fancy Hood | 4 | Ranger |
| `head_ranger_hood_t5` | Top-Shelf Hood | 5 | Ranger |
| `head_mage_crown_t1` | The Novice's Coronet of Preliminary Warding | 1 | Mage |
| `head_mage_crown_t2` | The Apprentice's Diadem of Moderate Arcane Protection | 2 | Mage |
| `head_mage_crown_t3` | The Adept's Circlet of Greater Warding | 3 | Mage |
| `head_mage_crown_t4` | The Master's Crown of Supreme Ward | 4 | Mage |
| `head_mage_crown_t5` | The Archmage's Diadem of Absolute Theurgic Supremacy | 5 | Mage |

### Armor — Body (15 items)

| ID | Display Name | Tier | ClassAffinity |
|---|---|---|---|
| `body_warrior_armor_t1` | Iron Armor | 1 | Warrior |
| `body_warrior_armor_t2` | Steel Armor | 2 | Warrior |
| `body_warrior_armor_t3` | Hard Armor | 3 | Warrior |
| `body_warrior_armor_t4` | Super Armor | 4 | Warrior |
| `body_warrior_armor_t5` | Mega Armor | 5 | Warrior |
| `body_ranger_vest_t1` | Rough Vest | 1 | Ranger |
| `body_ranger_vest_t2` | Standard Vest | 2 | Ranger |
| `body_ranger_vest_t3` | Fine Vest | 3 | Ranger |
| `body_ranger_vest_t4` | Fancy Vest | 4 | Ranger |
| `body_ranger_vest_t5` | Top-Shelf Vest | 5 | Ranger |
| `body_mage_robe_t1` | Novice Robes of Basic Magery | 1 | Mage |
| `body_mage_robe_t2` | Apprentice Vestments of Intermediate Study | 2 | Mage |
| `body_mage_robe_t3` | Adept's Mantle of Advanced Theurgy | 3 | Mage |
| `body_mage_robe_t4` | Master's Garb of Superior Magical Conduction | 4 | Mage |
| `body_mage_robe_t5` | Archmage's Robes of Transcendent Theurgism | 5 | Mage |

### Armor — Arms (15 items)

| ID | Display Name | Tier | ClassAffinity |
|---|---|---|---|
| `arms_warrior_gauntlets_t1` | Iron Gauntlets | 1 | Warrior |
| `arms_warrior_gauntlets_t2` | Steel Gauntlets | 2 | Warrior |
| `arms_warrior_gauntlets_t3` | Hard Gauntlets | 3 | Warrior |
| `arms_warrior_gauntlets_t4` | Super Gauntlets | 4 | Warrior |
| `arms_warrior_gauntlets_t5` | Mega Gauntlets | 5 | Warrior |
| `arms_ranger_braces_t1` | Rough Braces | 1 | Ranger |
| `arms_ranger_braces_t2` | Standard Braces | 2 | Ranger |
| `arms_ranger_braces_t3` | Fine Braces | 3 | Ranger |
| `arms_ranger_braces_t4` | Fancy Braces | 4 | Ranger |
| `arms_ranger_braces_t5` | Top-Shelf Braces | 5 | Ranger |
| `arms_mage_bangles_t1` | Novice Bangles of Minor Focus | 1 | Mage |
| `arms_mage_bangles_t2` | Apprentice Bangles of Steady Incantation | 2 | Mage |
| `arms_mage_bangles_t3` | Adept's Bangles of Disciplined Channeling | 3 | Mage |
| `arms_mage_bangles_t4` | Master's Bangles of Superior Gestural Arcana | 4 | Mage |
| `arms_mage_bangles_t5` | Archmage's Bangles of Transcendent Somatic Command | 5 | Mage |

### Armor — Legs (15 items)

| ID | Display Name | Tier | ClassAffinity |
|---|---|---|---|
| `legs_warrior_greaves_t1` | Iron Greaves | 1 | Warrior |
| `legs_warrior_greaves_t2` | Steel Greaves | 2 | Warrior |
| `legs_warrior_greaves_t3` | Hard Greaves | 3 | Warrior |
| `legs_warrior_greaves_t4` | Super Greaves | 4 | Warrior |
| `legs_warrior_greaves_t5` | Mega Greaves | 5 | Warrior |
| `legs_ranger_breeches_t1` | Rough Breeches | 1 | Ranger |
| `legs_ranger_breeches_t2` | Standard Breeches | 2 | Ranger |
| `legs_ranger_breeches_t3` | Fine Breeches | 3 | Ranger |
| `legs_ranger_breeches_t4` | Fancy Breeches | 4 | Ranger |
| `legs_ranger_breeches_t5` | Top-Shelf Breeches | 5 | Ranger |
| `legs_mage_leggings_t1` | Novice Leggings of Basic Ambulatory Warding | 1 | Mage |
| `legs_mage_leggings_t2` | Apprentice Leggings of Moderate Lower Theurgy | 2 | Mage |
| `legs_mage_leggings_t3` | Adept's Leggings of Greater Peripatetic Ward | 3 | Mage |
| `legs_mage_leggings_t4` | Master's Leggings of Superior Locomotive Arcana | 4 | Mage |
| `legs_mage_leggings_t5` | Archmage's Leggings of Transcendent Ambulatory Supremacy | 5 | Mage |

### Armor — Feet (15 items)

| ID | Display Name | Tier | ClassAffinity |
|---|---|---|---|
| `feet_warrior_boots_t1` | Iron Boots | 1 | Warrior |
| `feet_warrior_boots_t2` | Steel Boots | 2 | Warrior |
| `feet_warrior_boots_t3` | Hard Boots | 3 | Warrior |
| `feet_warrior_boots_t4` | Super Boots | 4 | Warrior |
| `feet_warrior_boots_t5` | Mega Boots | 5 | Warrior |
| `feet_ranger_shoes_t1` | Rough Shoes | 1 | Ranger |
| `feet_ranger_shoes_t2` | Standard Shoes | 2 | Ranger |
| `feet_ranger_shoes_t3` | Fine Shoes | 3 | Ranger |
| `feet_ranger_shoes_t4` | Fancy Shoes | 4 | Ranger |
| `feet_ranger_shoes_t5` | Top-Shelf Shoes | 5 | Ranger |
| `feet_mage_sandals_t1` | Novice Sandals of Minor Foot-Warding | 1 | Mage |
| `feet_mage_sandals_t2` | Apprentice Sandals of Moderate Pedal Arcana | 2 | Mage |
| `feet_mage_sandals_t3` | Adept's Sandals of Greater Plantar Theurgy | 3 | Mage |
| `feet_mage_sandals_t4` | Master's Sandals of Superior Pedestrian Conduction | 4 | Mage |
| `feet_mage_sandals_t5` | Archmage's Sandals of Transcendent Perambulatory Command | 5 | Mage |

---

## Main Hand weapons (40 items)

Weapons are class-archetyped. Warrior has 3 archetypes (sword / axe / hammer), Ranger has 3 archetypes (short bow / long bow / crossbow), Mage has 2 archetypes (staff / wand). Base damage scales with item level per [item-generation.md](../systems/item-generation.md).

**Cross-class weapon rule:** any class can equip any weapon (per [equipment.md](../systems/equipment.md) § "Class Affinity"). The weapon's **archetype** dictates the attack mechanic, not the player's class:
- Bow or crossbow equipped → fires the projectile attack **if the player has a quiver in the Ammo slot**. No quiver → bow becomes a melee bash (per [equipment.md](../systems/equipment.md) § "Ammo System"). This applies to every class — a Mage with a crossbow and a quiver shoots bolts; a Warrior with a bow and no quiver bashes.
- Staff or wand equipped → Mage-style spell projectile attack. A Warrior holding a staff fires magic bolts (at their STR-scaled damage, which is probably sub-optimal — that's the point of class-affinity).
- Sword / axe / hammer equipped → melee slash.

Class Affinity just grants the +25% stat bonus on items that match your class. The attack mechanic is always determined by the weapon itself.

### Warrior Main Hand

3 archetypes × 5 tiers = 15 items. The Warrior calls his weapons by what they do, not what they are.

#### Swords (5 items)

| ID | Display Name | Tier | ClassAffinity |
|---|---|---|---|
| `mainhand_warrior_sword_t1` | Sharp Sword | 1 | Warrior |
| `mainhand_warrior_sword_t2` | Sharper Sword | 2 | Warrior |
| `mainhand_warrior_sword_t3` | Big Sword | 3 | Warrior |
| `mainhand_warrior_sword_t4` | Super Sword | 4 | Warrior |
| `mainhand_warrior_sword_t5` | Mega Sword | 5 | Warrior |

#### Axes (5 items)

| ID | Display Name | Tier | ClassAffinity |
|---|---|---|---|
| `mainhand_warrior_axe_t1` | Chopper | 1 | Warrior |
| `mainhand_warrior_axe_t2` | Big Chopper | 2 | Warrior |
| `mainhand_warrior_axe_t3` | Hard Chopper | 3 | Warrior |
| `mainhand_warrior_axe_t4` | Super Chopper | 4 | Warrior |
| `mainhand_warrior_axe_t5` | Mega Chopper | 5 | Warrior |

#### Hammers (5 items)

| ID | Display Name | Tier | ClassAffinity |
|---|---|---|---|
| `mainhand_warrior_hammer_t1` | Smasher | 1 | Warrior |
| `mainhand_warrior_hammer_t2` | Big Smasher | 2 | Warrior |
| `mainhand_warrior_hammer_t3` | Hard Smasher | 3 | Warrior |
| `mainhand_warrior_hammer_t4` | Super Smasher | 4 | Warrior |
| `mainhand_warrior_hammer_t5` | Mega Smasher | 5 | Warrior |

### Ranger Main Hand

3 archetypes × 5 tiers = 15 items. The Ranger names bows the way a mechanic names wrenches. "Shortie" is hers — the self-deprecating short bow. "Longer" is the long bow. "Crank" is the crossbow (because you crank it).

#### Short bows (5 items)

| ID | Display Name | Tier | ClassAffinity |
|---|---|---|---|
| `mainhand_ranger_shortbow_t1` | Shortie | 1 | Ranger |
| `mainhand_ranger_shortbow_t2` | Solid Shortie | 2 | Ranger |
| `mainhand_ranger_shortbow_t3` | Quality Shortie | 3 | Ranger |
| `mainhand_ranger_shortbow_t4` | Mean Shortie | 4 | Ranger |
| `mainhand_ranger_shortbow_t5` | Top-Shelf Shortie | 5 | Ranger |

#### Long bows (5 items)

| ID | Display Name | Tier | ClassAffinity |
|---|---|---|---|
| `mainhand_ranger_longbow_t1` | Longer | 1 | Ranger |
| `mainhand_ranger_longbow_t2` | Solid Longer | 2 | Ranger |
| `mainhand_ranger_longbow_t3` | Quality Longer | 3 | Ranger |
| `mainhand_ranger_longbow_t4` | Mean Longer | 4 | Ranger |
| `mainhand_ranger_longbow_t5` | Top-Shelf Longer | 5 | Ranger |

#### Crossbows (5 items)

| ID | Display Name | Tier | ClassAffinity |
|---|---|---|---|
| `mainhand_ranger_crossbow_t1` | Crank | 1 | Ranger |
| `mainhand_ranger_crossbow_t2` | Solid Crank | 2 | Ranger |
| `mainhand_ranger_crossbow_t3` | Quality Crank | 3 | Ranger |
| `mainhand_ranger_crossbow_t4` | Mean Crank | 4 | Ranger |
| `mainhand_ranger_crossbow_t5` | Top-Shelf Crank | 5 | Ranger |

### Mage Main Hand

2 archetypes × 5 tiers = 10 items. The Mage names staves "Implement of Channeling" and wands "Baton of Focused Incantation" because calling them "sticks" would be beneath him.

#### Staves (5 items)

| ID | Display Name | Tier | ClassAffinity |
|---|---|---|---|
| `mainhand_mage_staff_t1` | Novice's Staff of Beginner Channeling | 1 | Mage |
| `mainhand_mage_staff_t2` | Apprentice's Implement of Elementary Incantation | 2 | Mage |
| `mainhand_mage_staff_t3` | Adept's Staff of Intermediate Thaumaturgic Conduction | 3 | Mage |
| `mainhand_mage_staff_t4` | Master's Implement of Superior Arcane Transmission | 4 | Mage |
| `mainhand_mage_staff_t5` | The Archmage's Transcendent Staff of Supreme Theurgic Conduction | 5 | Mage |

#### Wands (5 items)

| ID | Display Name | Tier | ClassAffinity |
|---|---|---|---|
| `mainhand_mage_wand_t1` | Novice's Baton of Preliminary Focus | 1 | Mage |
| `mainhand_mage_wand_t2` | Apprentice's Rod of Moderate Incantation | 2 | Mage |
| `mainhand_mage_wand_t3` | Adept's Wand of Advanced Spell-Precision | 3 | Mage |
| `mainhand_mage_wand_t4` | Master's Rod of Superior Arcane Targeting | 4 | Mage |
| `mainhand_mage_wand_t5` | The Archmage's Ultimate Baton of Supreme Thaumaturgic Precision | 5 | Mage |

---

## Off Hand (30 items)

Three off-hand categories, one per class. Each class gets 5 tiers × 2 variants = 10 items.

### Warrior Off Hand: Shields

10 items. The Warrior names shields by what they do to the thing hitting them.

#### Small shields (5 items)

| ID | Display Name | Tier | ClassAffinity |
|---|---|---|---|
| `offhand_warrior_smallshield_t1` | Bash Shield | 1 | Warrior |
| `offhand_warrior_smallshield_t2` | Sturdy Bash Shield | 2 | Warrior |
| `offhand_warrior_smallshield_t3` | Hard Bash Shield | 3 | Warrior |
| `offhand_warrior_smallshield_t4` | Super Bash Shield | 4 | Warrior |
| `offhand_warrior_smallshield_t5` | Mega Bash Shield | 5 | Warrior |

#### Tower shields (5 items)

| ID | Display Name | Tier | ClassAffinity |
|---|---|---|---|
| `offhand_warrior_towershield_t1` | Big Shield | 1 | Warrior |
| `offhand_warrior_towershield_t2` | Bigger Shield | 2 | Warrior |
| `offhand_warrior_towershield_t3` | Strong Shield | 3 | Warrior |
| `offhand_warrior_towershield_t4` | Super Shield | 4 | Warrior |
| `offhand_warrior_towershield_t5` | Mega Shield | 5 | Warrior |

### Ranger Off Hand: Defensive Melee

10 items. Ranger off-hand is a close-range backup — knives, claws. Short names, functional.

#### Knives (5 items)

| ID | Display Name | Tier | ClassAffinity |
|---|---|---|---|
| `offhand_ranger_knife_t1` | Stabby | 1 | Ranger |
| `offhand_ranger_knife_t2` | Solid Stabby | 2 | Ranger |
| `offhand_ranger_knife_t3` | Quality Stabby | 3 | Ranger |
| `offhand_ranger_knife_t4` | Mean Stabby | 4 | Ranger |
| `offhand_ranger_knife_t5` | Top-Shelf Stabby | 5 | Ranger |

#### Claws / punch-weapons (5 items)

| ID | Display Name | Tier | ClassAffinity |
|---|---|---|---|
| `offhand_ranger_claw_t1` | Puncher | 1 | Ranger |
| `offhand_ranger_claw_t2` | Solid Puncher | 2 | Ranger |
| `offhand_ranger_claw_t3` | Quality Puncher | 3 | Ranger |
| `offhand_ranger_claw_t4` | Mean Puncher | 4 | Ranger |
| `offhand_ranger_claw_t5` | Top-Shelf Puncher | 5 | Ranger |

### Mage Off Hand: Spellbooks

10 items. The Mage's off-hand fantasy is a leather tome with his own annotations. The names are unbearable.

#### Grimoires (5 items)

| ID | Display Name | Tier | ClassAffinity |
|---|---|---|---|
| `offhand_mage_grimoire_t1` | Lesser Grimoire of Foundational Theurgies | 1 | Mage |
| `offhand_mage_grimoire_t2` | Moderate Grimoire of Intermediate Arcana | 2 | Mage |
| `offhand_mage_grimoire_t3` | Greater Grimoire of Advanced Thaumaturgy | 3 | Mage |
| `offhand_mage_grimoire_t4` | Master's Grimoire of Superior Magical Lore | 4 | Mage |
| `offhand_mage_grimoire_t5` | The Archmage's Supreme Grimoire of Transcendent Theurgy | 5 | Mage |

#### Codices (5 items)

| ID | Display Name | Tier | ClassAffinity |
|---|---|---|---|
| `offhand_mage_codex_t1` | Tome of Lesser Illumination of Great Knowledge | 1 | Mage |
| `offhand_mage_codex_t2` | Tome of Moderate Illumination | 2 | Mage |
| `offhand_mage_codex_t3` | Codex of Advanced Arcana | 3 | Mage |
| `offhand_mage_codex_t4` | Master's Treatise on Supreme Thaumaturgy | 4 | Mage |
| `offhand_mage_codex_t5` | The Archmage's Supreme Compendium of Transcendent Theurgy | 5 | Mage |

(The Tier 1 codex is a deliberate malaprop — "Lesser Illumination of Great Knowledge" is redundant. The Mage does not know this.)

---

## Ammo / Quivers (9 items)

Quivers carry **imbues** — the effect is baked into the quiver, not the bow (per [equipment.md](../systems/equipment.md) § "Ammo System"). Ammo is infinite; the quiver is the permanent equipped item.

**One quiver per imbue type — no tiers, no sizes.** Ammo is infinite, so "big quiver vs small quiver" has no mechanical meaning — size would be cosmetic-only. Stats scale via item-level rolled at drop time per [item-generation.md](../systems/item-generation.md), so a Basic Quiver dropped on floor 50 is naturally stronger than one dropped on floor 1 without needing separate IDs. All Ranger-voiced.

| ID | Display Name | Imbue | Effect | ClassAffinity |
|---|---|---|---|---|
| `ammo_quiver_basic` | Basic Quiver | Basic | No element. Plain arrows. | Ranger |
| `ammo_quiver_hot` | Hot Quiver | Fire | Burn damage over time. | Ranger |
| `ammo_quiver_cold` | Cold Quiver | Frost | Slow on hit. | Ranger |
| `ammo_quiver_heavy` | Heavy Quiver | Stun/Knockback | Heavy impact staggers enemies. | Ranger |
| `ammo_quiver_nasty` | Nasty Quiver | Poison | Poison DoT. | Ranger |
| `ammo_quiver_zap` | Zap Quiver | Lightning | Chain to nearby enemies. | Ranger |
| `ammo_quiver_quiet` | Quiet Quiver | Shadow | Stealth bonus + shadow damage. | Ranger |
| `ammo_quiver_sharp` | Sharp Quiver | Bleed | Physical DoT on crit. | Ranger |
| `ammo_quiver_bright` | Bright Quiver | Holy | Extra damage to undead (Skeleton species). Heals on hit vs undead. | Ranger |

The Ranger named these herself over three drunk evenings in the Adventurer's Guild. Nobody corrects her.

---

## Neck (15 items)

5 tiers × 3 variants. Neutral voice. Each tier rotates through three stat-focus flavors: one offensive, one defensive, one utility. Metal ladder maps to tier.

| ID | Display Name | Tier | Focus | ClassAffinity |
|---|---|---|---|---|
| `neck_t1_offense` | Iron Chain of Might | 1 | STR / damage | null |
| `neck_t1_defense` | Iron Chain of Warding | 1 | Defense | null |
| `neck_t1_utility` | Iron Chain of Fortune | 1 | Gold find | null |
| `neck_t2_offense` | Steel Chain of Might | 2 | STR / damage | null |
| `neck_t2_defense` | Steel Chain of Warding | 2 | Defense | null |
| `neck_t2_utility` | Steel Chain of Fortune | 2 | Gold find | null |
| `neck_t3_offense` | Mithril Chain of Might | 3 | STR / damage | null |
| `neck_t3_defense` | Mithril Chain of Warding | 3 | Defense | null |
| `neck_t3_utility` | Mithril Chain of Fortune | 3 | Gold find | null |
| `neck_t4_offense` | Orichalcum Chain of Might | 4 | STR / damage | null |
| `neck_t4_defense` | Orichalcum Chain of Warding | 4 | Defense | null |
| `neck_t4_utility` | Orichalcum Chain of Fortune | 4 | Gold find | null |
| `neck_t5_offense` | Dragonite Chain of Might | 5 | STR / damage | null |
| `neck_t5_defense` | Dragonite Chain of Warding | 5 | Defense | null |
| `neck_t5_utility` | Dragonite Chain of Fortune | 5 | Gold find | null |

---

## Ring (40 items)

5 tiers × 8 focuses per tier. Every focus ships at every tier — ring stacking is the primary build-diversity vector (10 ring slots), so the catalog gives every identity complete coverage.

The 8 focuses split into two clean families:

- **4 core stats**: STR, DEX, STA, INT
- **4 combat stats**: Crit (Precision), Haste, Dodge (Evasion), Block (Bulwark)

Utility effects (move speed, XP bonus, gold find) moved to Neck accessories (see [Neck](#neck-15-items) §utility variants) to keep Ring identity focused on combat contribution. Rings are your fight-modifiers; necks are your life-modifiers.

Metals follow the same ladder as Neck and Ore materials (Iron / Steel / Mithril / Orichalcum / Dragonite).

### Ring table (40 items)

| ID | Display Name | Tier | Focus | ClassAffinity |
|---|---|---|---|---|
| `ring_t1_str` | Iron Ring of Strength | 1 | STR | null |
| `ring_t1_dex` | Iron Ring of Dexterity | 1 | DEX | null |
| `ring_t1_sta` | Iron Ring of Vigor | 1 | STA | null |
| `ring_t1_int` | Iron Ring of Intellect | 1 | INT | null |
| `ring_t1_crit` | Iron Ring of Precision | 1 | Crit chance | null |
| `ring_t1_haste` | Iron Ring of Haste | 1 | Attack speed | null |
| `ring_t1_dodge` | Iron Ring of Evasion | 1 | Dodge chance | null |
| `ring_t1_block` | Iron Ring of Bulwark | 1 | Block chance | null |
| `ring_t2_str` | Steel Ring of Strength | 2 | STR | null |
| `ring_t2_dex` | Steel Ring of Dexterity | 2 | DEX | null |
| `ring_t2_sta` | Steel Ring of Vigor | 2 | STA | null |
| `ring_t2_int` | Steel Ring of Intellect | 2 | INT | null |
| `ring_t2_crit` | Steel Ring of Precision | 2 | Crit chance | null |
| `ring_t2_haste` | Steel Ring of Haste | 2 | Attack speed | null |
| `ring_t2_dodge` | Steel Ring of Evasion | 2 | Dodge chance | null |
| `ring_t2_block` | Steel Ring of Bulwark | 2 | Block chance | null |
| `ring_t3_str` | Mithril Ring of Strength | 3 | STR | null |
| `ring_t3_dex` | Mithril Ring of Dexterity | 3 | DEX | null |
| `ring_t3_sta` | Mithril Ring of Vigor | 3 | STA | null |
| `ring_t3_int` | Mithril Ring of Intellect | 3 | INT | null |
| `ring_t3_crit` | Mithril Ring of Precision | 3 | Crit chance | null |
| `ring_t3_haste` | Mithril Ring of Haste | 3 | Attack speed | null |
| `ring_t3_dodge` | Mithril Ring of Evasion | 3 | Dodge chance | null |
| `ring_t3_block` | Mithril Ring of Bulwark | 3 | Block chance | null |
| `ring_t4_str` | Orichalcum Ring of Strength | 4 | STR | null |
| `ring_t4_dex` | Orichalcum Ring of Dexterity | 4 | DEX | null |
| `ring_t4_sta` | Orichalcum Ring of Vigor | 4 | STA | null |
| `ring_t4_int` | Orichalcum Ring of Intellect | 4 | INT | null |
| `ring_t4_crit` | Orichalcum Ring of Precision | 4 | Crit chance | null |
| `ring_t4_haste` | Orichalcum Ring of Haste | 4 | Attack speed | null |
| `ring_t4_dodge` | Orichalcum Ring of Evasion | 4 | Dodge chance | null |
| `ring_t4_block` | Orichalcum Ring of Bulwark | 4 | Block chance | null |
| `ring_t5_str` | Dragonite Ring of Strength | 5 | STR | null |
| `ring_t5_dex` | Dragonite Ring of Dexterity | 5 | DEX | null |
| `ring_t5_sta` | Dragonite Ring of Vigor | 5 | STA | null |
| `ring_t5_int` | Dragonite Ring of Intellect | 5 | INT | null |
| `ring_t5_crit` | Dragonite Ring of Precision | 5 | Crit chance | null |
| `ring_t5_haste` | Dragonite Ring of Haste | 5 | Attack speed | null |
| `ring_t5_dodge` | Dragonite Ring of Evasion | 5 | Dodge chance | null |
| `ring_t5_block` | Dragonite Ring of Bulwark | 5 | Block chance | null |

Ring stacking (all 10 ring slots with the same focus) is intended per [equipment.md](../systems/equipment.md) § "Ring Slots" — a "10× Dragonite Ring of Precision" crit-build is a legitimate goal.

---

## Consumables (28 items)

Neutral voice. Guild Maid store inventory + drops. All stack unlimited per slot per [items.md](items.md).

### HP Potions (4 items)

| ID | Display Name | Effect | Sold by | Stack |
|---|---|---|---|---|
| `consumable_hp_small` | Small Health Potion | Restore 30 HP | Guild Maid | unlimited |
| `consumable_hp_medium` | Medium Health Potion | Restore 80 HP | Guild Maid | unlimited |
| `consumable_hp_large` | Large Health Potion | Restore 180 HP | Guild Maid | unlimited |
| `consumable_hp_greater` | Greater Health Potion | Restore 400 HP | Guild Maid (unlocks deeper) | unlimited |

### MP Potions (4 items)

| ID | Display Name | Effect | Sold by | Stack |
|---|---|---|---|---|
| `consumable_mp_small` | Small Mana Potion | Restore 20 MP | Guild Maid | unlimited |
| `consumable_mp_medium` | Medium Mana Potion | Restore 60 MP | Guild Maid | unlimited |
| `consumable_mp_large` | Large Mana Potion | Restore 140 MP | Guild Maid | unlimited |
| `consumable_mp_greater` | Greater Mana Potion | Restore 320 MP | Guild Maid (unlocks deeper) | unlimited |

### Buff Scrolls (5 items)

Short-duration (120 s) buff scrolls. Stack with each other.

| ID | Display Name | Effect |
|---|---|---|
| `consumable_scroll_might` | Scroll of Might | +20% physical damage for 120 s |
| `consumable_scroll_focus` | Scroll of Focus | +20% spell damage for 120 s |
| `consumable_scroll_warding` | Scroll of Warding | +25% defense for 120 s |
| `consumable_scroll_haste` | Scroll of Haste | +20% move + attack speed for 120 s |
| `consumable_scroll_sight` | Scroll of Sight | Reveals stairs on map for 120 s |

### Elemental Bombs (3 items)

Thrown AoE consumables. Short arc, 64 px radius. Damage scales with **item level at drop** (the floor it was found on), independent of the player's weapon — so a Mage with a low-damage staff gets the same bomb payoff as a Warrior with a big sword. This keeps bombs class-egalitarian.

| ID | Display Name | Damage formula | Secondary effect |
|---|---|---|---|
| `consumable_bomb_fire` | Fire Bomb | `itemLevel × 8` | Burn DoT (`itemLevel × 2` over 4 s) |
| `consumable_bomb_shock` | Shock Bomb | `itemLevel × 6` | Stun 1.5 s |
| `consumable_bomb_frost` | Frost Bomb | `itemLevel × 4` | Freeze 2 s (movement halted, takes +50% damage) |

At floor 25 (itemLevel 25), a Fire Bomb deals 200 raw + 200 burn = 400 over 4 s. At floor 100, that's 800 + 800 = 1600. Scales linearly; no diminishing returns.

### Food (3 items)

Passive out-of-combat regeneration. Consumed instantly; grants a 30-second "Fed" buff.

| ID | Display Name | Effect |
|---|---|---|
| `consumable_food_bread` | Traveler's Bread | +5 HP/s for 30 s |
| `consumable_food_stew` | Hearty Stew | +12 HP/s for 30 s |
| `consumable_food_feast` | Guild Feast | +25 HP/s and +5 MP/s for 30 s |

### Bandages (2 items)

Instant out-of-combat heal with a short channel.

| ID | Display Name | Effect |
|---|---|---|
| `consumable_bandage_rough` | Rough Bandage | Restore 50 HP over 3-second channel |
| `consumable_bandage_fine` | Fine Bandage | Restore 150 HP over 3-second channel |

### Antidotes (2 items)

| ID | Display Name | Effect |
|---|---|---|
| `consumable_antidote_small` | Small Antidote | Cleanse poison DoT |
| `consumable_antidote_strong` | Strong Antidote | Cleanse poison + immunity for 60 s |

### Teleport Stones (2 items)

| ID | Display Name | Effect |
|---|---|---|
| `consumable_teleport_town` | Town Teleport Stone | Return to town instantly |
| `consumable_teleport_dungeon` | Dungeon Teleport Stone | Return to the deepest floor reached (town → dungeon) |

### Sacrificial Idol (1 item, existing)

| ID | Display Name | Effect |
|---|---|---|
| `consumable_sacrificial_idol` | Sacrificial Idol | Acts as free "Save Both" on death. See [death.md](../systems/death.md). |

**Unchanged from existing spec.** The idol is the locked one-of-a-kind consumable that breaks the death-penalty balance sheet in the player's favor.

### Elixirs (2 items)

| ID | Display Name | Effect |
|---|---|---|
| `consumable_elixir_xp` | Elixir of Insight | +25% XP gain for 300 s |
| `consumable_elixir_luck` | Elixir of Fortune | +25% drop rate for 300 s |

**Consumables total: 4 + 4 + 5 + 3 + 3 + 2 + 2 + 2 + 1 + 2 = 28.**

---

## Materials (22 items)

Two sub-categories: **tiered generic** (5 tiers × 3 types = 15) and **species-signature** (7). Neutral voice throughout. Materials drop from monsters per [monster-drops.md](../systems/monster-drops.md).

### Tiered generic materials (15 items)

Three types (Ore, Bone, Hide) × five tiers. Tier maps to floor bracket. See [item-generation.md](../systems/item-generation.md) for the existing material-tier floor brackets.

| ID | Display Name | Tier | Type |
|---|---|---|---|
| `material_ore_t1` | Iron Ore | 1 | Ore |
| `material_ore_t2` | Steel Ingot | 2 | Ore |
| `material_ore_t3` | Mithril Ore | 3 | Ore |
| `material_ore_t4` | Orichalcum Ore | 4 | Ore |
| `material_ore_t5` | Dragonite Ore | 5 | Ore |
| `material_bone_t1` | Rough Bone | 1 | Bone |
| `material_bone_t2` | Standard Bone | 2 | Bone |
| `material_bone_t3` | Fine Bone | 3 | Bone |
| `material_bone_t4` | Masterwork Bone | 4 | Bone |
| `material_bone_t5` | Top-Shelf Bone | 5 | Bone |
| `material_hide_t1` | Rough Hide | 1 | Hide |
| `material_hide_t2` | Standard Hide | 2 | Hide |
| `material_hide_t3` | Fine Hide | 3 | Hide |
| `material_hide_t4` | Masterwork Hide | 4 | Hide |
| `material_hide_t5` | Top-Shelf Hide | 5 | Hide |

(The Bone and Hide ladders use the `Rough / Standard / Fine / Masterwork / Top-Shelf` pattern. The Ore ladder — **Iron → Steel → Mithril → Orichalcum → Dragonite** — is the unified metal ladder for Neck and Ring accessories as well. This single shared ladder is the canonical fantasy progression for the game; the player's accessory wealth and material wealth travel together visually.)

### Species-signature materials (7 items)

One per enemy species per [SYS-10](../dev-tracker.md). Signature materials drop at 7–10% per kill on top of the generic material roll — see [monster-drops.md](../systems/monster-drops.md).

| ID | Display Name | Species | Notes |
|---|---|---|---|
| `material_sig_skeleton` | Bone Dust | Skeleton | Finely-ground bone; primary reagent for necro-adjacent Mage affixes. |
| `material_sig_goblin` | Goblin Tooth | Goblin | Crooked, tough; used for piercing/crit affixes. |
| `material_sig_bat` | Echo Shard | Bat | Crystalline resonance; used for sound / stealth quiver imbues. |
| `material_sig_wolf` | Wolf Pelt | Wolf | Pristine hide; used for movement-speed armor affixes. |
| `material_sig_orc` | Orc Tusk | Orc | Dense ivory; used for heavy-damage / bash affixes. |
| `material_sig_darkmage` | Arcane Residue | Dark Mage | Raw magical sludge; used for elemental-damage affixes. |
| `material_sig_spider` | Chitin Fragment | Spider | Brittle-but-sharp shell; used for poison / bleed affixes. |

Signature-material affix bindings are **suggestions**, not rules — the Blacksmith's crafting UI exposes them as categories, but any affix can use any material as long as the material-tier matches the affix-tier. This is the existing rule from [items.md](items.md) § "Materials are generic" (generic is the baseline; signature is the thematic tilt).

---

## Uniques — deferred

Not in this spec. Powerful named items (the equivalent of Diablo 1 uniques — items with hand-tuned affix sets, fixed names, fixed ilvls) are produced by a planned **Blacksmith Forge RNG system** (TBD — Forge RNG system). That system will:

- Take a player-owned base item + a large material investment.
- Roll against a curated unique-item table gated by the base's tier and ilvl.
- Produce a named item with a hand-designed affix package.

Uniques get their own spec when that system is ready. This catalog defines only bases. Do **not** add uniques here; they would bloat the monolith and conflict with the Forge system's authorship.

---

## Acceptance Criteria

- [ ] All 259 items enumerated above exist in `ItemDatabase.cs` with matching IDs, display names, slots, and `ClassAffinity` values.
- [ ] Armor items implement base defense via [item-generation.md](../systems/item-generation.md) formulas; weapon items implement base damage via the same.
- [ ] Quivers carry their imbue effect as a property on the item; bow weapons do not carry imbues (per [equipment.md](../systems/equipment.md)).
- [ ] Consumables match the effects listed above. Sacrificial Idol is unchanged.
- [ ] Materials appear in the Blacksmith's crafting UI with tier and type metadata.
- [ ] Species-signature materials drop only from the correct species (handled by [monster-drops.md](../systems/monster-drops.md) tables).
- [ ] `ItemDatabase.cs` has no dangling IDs referenced by `MonsterDropTables` or save-file graveyard entries (see [dungeon-regurgitation.md](../systems/dungeon-regurgitation.md)).
- [ ] Class-voice names are copied **exactly** as written here — voice is content-authoring work, not implementation paraphrase. If a name reads flat, flag it on the design-lead doc, do not silently rewrite.

## Implementation Notes

- `ItemDatabase.cs` should be regenerated from this doc rather than hand-edited. Consider a build-step that parses these tables into a C# source file (low-priority; for now, implementer types them).
- Base stat values are **not** in this catalog — they come from [item-generation.md](../systems/item-generation.md) formulas at drop time based on `ItemLevel` and `BaseQuality`. The catalog only defines identity (ID, name, slot, class affinity, tier).
- Tier → item-level bracket is advisory for drop selection (see [monster-drops.md](../systems/monster-drops.md) slot-roll step 4). A Tier 1 item dropped on floor 1 has `ItemLevel = 1`; a Tier 1 item accidentally dropped on floor 50 (via regurgitation or floor-sparse drop tables) still has its original `ItemLevel`.
- Voice-specific sprites: art will need multi-sprite coverage for at least the three class voices per slot. That is an ART ticket, not an ITEM-01 ticket. The catalog is name-and-identity only.
- `ItemDef.Tier` does not currently exist — add it as an `int` field (1..5). It's orthogonal to `BaseQuality` and used only by `MonsterDropTables` for floor-bracket selection.
- IDs are **stable forever.** If an item is renamed, the ID stays the same. Save files and graveyard entries reference IDs, not display names.

## Open Questions

None.
