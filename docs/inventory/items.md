# Item System

## Summary

Items include equipment, consumables, and special objects. The full item system is deferred — this file serves as a placeholder and tracks what's known.

## Current State

No items are implemented. The only item referenced in the design is the Sacrificial Idol (a consumable that negates backpack loss on death).

## Design

### Known Items

| Item | Type | Source | Effect |
|------|------|--------|--------|
| Sacrificial Idol | Consumable | Item Shop (town) | Negates backpack item loss on death. Single use. |

### Planned Categories

- **Equipment:** weapons, armor — affect stats and combat (see Equipment Slots below)
- **Consumables:** potions, idols, scrolls — single-use effects
- **Materials:** crafting ingredients for the Blacksmith
- **Special:** quest items, keys, unique rewards

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

### Loot System

How items are obtained is not yet designed:
- Enemy drops?
- Floor completion rewards?
- Chest/container spawns?
- Shop-only for some items?

All loot details are **explicitly deferred** until the core systems (combat, death, inventory UI) are functional.

### Item Color

Item color uses the **unified color gradient** (see [color-system.md](../systems/color-system.md)). There are no discrete rarity tiers (no "Rare" or "Epic" labels). Instead, an item's color is computed from the level gap between the player and the item's effective level:

- Warm colors (orange/red) = item is above your level, powerful but bonuses may be gated
- Green/yellow = item is appropriate for your level
- Cool colors (blue/cyan) = item is below your level, losing relevance
- Grey = item is far below your level, candidate for recycling at the Blacksmith

The same item shifts color as the player levels. No equipment restrictions on wearing items — any item can be equipped, but abilities/bonuses may be locked if the player is underleveled.

## Open Questions

- How does equipment affect stats?
- Should items have durability?
- How does the Blacksmith crafting system work?
- What's the loot table structure for enemies/floors?
- Should items be tradeable (if multiplayer is ever considered)?
