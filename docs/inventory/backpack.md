# Backpack

## Summary

The backpack is the player's carried inventory. Items in the backpack are at risk on death. Starts with 25 slots and can be expanded.

## Current State

**Spec status: LOCKED.**

Not yet implemented. The prototype has no inventory system.

## Design

### Core Rules

- **Starting slots:** 25
- **At risk on death:** items in the backpack can be lost as a death penalty (see [death.md](../systems/death.md))
- **Always accessible:** the backpack can be opened anywhere (dungeon or town)
- **Expansion:** additional slots can be purchased or earned (mechanism TBD)

### Inventory Lore

The backpack uses a "magical pocket dimension" mechanic common to fantasy settings. It simply exists — there is no in-world explanation for how it works. It just is.

### Backpack vs. Bank

| Feature | Backpack | Bank |
|---------|----------|------|
| Starting slots | 25 | 50 |
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

- Purchased from the Item Shop NPC in town with gold
- Cost scales with current backpack size (each expansion costs more)
- Expansion adds 5 slots per purchase
- No hard cap

| Expansion # | New Total | Gold Cost |
|------------|-----------|-----------|
| 1 | 30 | 300 |
| 2 | 35 | 900 |
| 3 | 40 | 1,800 |
| N | 25 + N*5 | `300 * N^2` |

*Costs are starting values — subject to balancing.*

### Item Stacking

Consumables and materials stack within a single slot. Equipment does not stack (1 per slot).

| Item Type | Stackable | Max Stack |
|-----------|-----------|-----------|
| Equipment | No | 1 |
| Consumables | Yes | 99 |
| Materials | Yes | 99 |
| Special | No | 1 |

## Resolved Questions

| Question | Decision |
|----------|----------|
| Weight system? | No. Slot-based only. Keep it simple. |
| Item stacking? | Yes — consumables and materials stack to 99. Equipment does not stack. |
| Protected slot? | No. All backpack slots are at risk. Use the bank or Sacrificial Idol for protection. |
| Max slots? | No hard cap. Expansion cost escalates. |
