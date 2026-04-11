# Bank

## Summary

The bank is safe, permanent storage accessible only from town. Items in the bank are never lost on death. Starts with 50 slots, expandable.

## Current State

**Spec status: LOCKED.**

Not yet implemented. The prototype has no inventory system.

## Design

### Core Rules

- **Starting slots:** 50
- **Safe on death:** bank items are never at risk
- **Town-only access:** players must be in town to deposit or withdraw. No dungeon access.
- **Expansion:** additional slots can be purchased from the Banker NPC with gold

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

- Purchased from the Banker NPC with gold
- Cost scales with current bank size (each expansion costs more than the last)
- Expansion adds 10 slots per purchase
- No hard cap — infinite expansion fits the infinite dungeon theme, tempered by escalating cost

| Expansion # | New Total | Gold Cost |
|------------|-----------|-----------|
| 1 | 60 | 500 |
| 2 | 70 | 1,500 |
| 3 | 80 | 3,000 |
| 4 | 90 | 5,000 |
| N | 50 + N*10 | `500 * N^2` |

*Values are locked. Tunable during playtesting only — the formula `500 * N^2` and +10 slots per expansion are final.*

### Backpack vs. Bank

| Feature | Backpack | Bank |
|---------|----------|------|
| Starting slots | 25 | 50 |
| Accessible from | Anywhere | Town only |
| At risk on death | Yes | No |
| Expansion | Yes | Yes |

## Resolved Questions

| Question | Decision |
|----------|----------|
| Bank from dungeon? | No — town-only. Encourages return trips. |
| Max bank slots? | No hard cap. Expansion cost escalates. |
| Bank tabs/categories? | No — single flat list. Keep it simple. |
| Gold in bank? | No — gold is always on person. Gold is spent on death mitigation, so it must be accessible. |
