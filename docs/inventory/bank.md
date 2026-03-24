# Bank

## Summary

The bank is safe, permanent storage accessible only from town. Items in the bank are never lost on death. Starts with 15 slots.

## Current State

Not yet implemented. The prototype has no inventory system.

## Design

### Core Rules

- **Starting slots:** 15
- **Safe on death:** bank items are never at risk
- **Town-only access:** players must be in town to deposit or withdraw
- **Expansion:** additional slots can be purchased (mechanism TBD)

### Purpose

The bank provides a counterbalance to the backpack's risk:
- Store valuable items safely before dangerous dungeon runs
- Strategic choice: carry items for immediate use vs. bank them for safety
- Encourages regular town visits to deposit loot

### Interaction

- Accessed via the Banker NPC in town (see [town.md](../world/town.md))
- Simple deposit/withdraw UI
- Drag-and-drop or click-to-transfer between backpack and bank

### Slot Expansion

Details TBD. Possible approaches:
- Purchase from the Banker NPC with gold
- Unlock through progression milestones

## Open Questions

- Should the bank be accessible from the dungeon via a consumable item?
- What's the maximum number of bank slots?
- Should bank tabs or categories exist for organization?
- Can the bank hold gold, or is gold always "on person"?
