# Backpack

## Summary

The backpack is the player's carried inventory. Items in the backpack are at risk on death. Starts with 25 slots and can be expanded.

## Current State

Not yet implemented. The prototype has no inventory system.

## Design

### Core Rules

- **Starting slots:** 25
- **At risk on death:** items in the backpack can be lost as a death penalty (see [death.md](../systems/death.md))
- **Always accessible:** the backpack can be opened anywhere (dungeon or town)
- **Expansion:** additional slots can be purchased or earned (mechanism TBD)

### Backpack vs. Bank

| Feature | Backpack | Bank |
|---------|----------|------|
| Starting slots | 25 | 15 |
| Accessible from | Anywhere | Town only |
| At risk on death | Yes | No |
| Expansion | Yes | Yes |

The tension between backpack and bank is a core risk/reward mechanic. Players must decide what to carry (convenient but risky) versus what to store (safe but inconvenient).

### Death Penalty Interaction

On death, a random number of backpack items are lost:
```
itemsLost = floor(deepestFloor / 10) + 1
```

Items are selected randomly. The Sacrificial Idol (if present in the backpack) negates this loss entirely. See [death.md](../systems/death.md) for full details.

### Slot Expansion

Details TBD. Possible approaches:
- Purchase additional slots from a town NPC
- Earn slots through achievements or milestones
- Find slot-expanding items in the dungeon

## Open Questions

- Should the backpack have a weight system in addition to slot limits?
- Can items be stacked (e.g., 10 potions in one slot)?
- Should there be a "protected" slot that's immune to death penalties?
- What's the maximum number of slots the backpack can expand to?
