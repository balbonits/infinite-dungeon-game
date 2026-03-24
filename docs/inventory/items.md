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

- **Equipment:** weapons, armor — affect stats and combat (details deferred)
- **Consumables:** potions, idols, scrolls — single-use effects
- **Materials:** crafting ingredients for the Blacksmith
- **Special:** quest items, keys, unique rewards

### Loot System

How items are obtained is not yet designed:
- Enemy drops?
- Floor completion rewards?
- Chest/container spawns?
- Shop-only for some items?

All loot details are **explicitly deferred** until the core systems (combat, death, inventory UI) are functional.

## Open Questions

- What item rarity tiers should exist?
- How does equipment affect stats?
- Should items have durability?
- How does the Blacksmith crafting system work?
- What's the loot table structure for enemies/floors?
- Should items be tradeable (if multiplayer is ever considered)?
