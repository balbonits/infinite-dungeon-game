# Death System

## Summary

Death imposes penalties scaled by the deepest floor achieved. Players can spend gold to mitigate penalties. A Sacrificial Idol consumable can negate backpack loss entirely.

## Current State

The prototype has a basic death screen ("You Died — Tap / Press R to restart") that restarts the scene. No penalties, gold buyout, or death options are implemented yet.

## Design

### Death Flow

```
Player HP reaches 0
  → Game pauses (enemies stop, player disabled)
  → Death screen appears
  → Step 1: Choose destination
    → "Return to Town" (lose floor progress, safe restart)
    → "Respawn at Last Safe Spot" (stay on current floor)
  → Step 2: Toggle mitigation options
    → [ ] Buy EXP protection (costs gold)
    → [ ] Buy Backpack protection (costs gold)
    → Sacrificial Idol auto-applies if in inventory (negates backpack loss, consumed)
  → Step 3: Review summary
    → Shows exact penalties with and without mitigations
    → Shows gold cost
  → Step 4: Confirm
    → Confirmation dialog: "Are you sure? You will lose X, Y, Z"
    → Player confirms → penalties applied → respawn
```

**Key rule:** No auto-select. The player must actively choose every option. Nothing is pre-checked.

### Penalty Formulas (Proposed)

These are proposed starting values, intended to be tuned through playtesting.

#### EXP Loss

```
expLossPercent = min(deepestFloor * 0.4, 50)
```

- Loses a percentage of XP progress within the current level (not total XP)
- Floor 1: 0.4% loss (negligible)
- Floor 50: 20% loss (noticeable)
- Floor 125+: 50% loss (cap — never lose more than half a level's progress)

#### Backpack Item Loss

```
itemsLost = floor(deepestFloor / 10) + 1
```

- Randomly selected items from the backpack
- Floor 1–9: lose 1 item
- Floor 10–19: lose 2 items
- Floor 50–59: lose 6 items
- Capped by actual backpack contents (can't lose more items than you have)
- Bank items are **never** at risk

#### Gold Buyout Costs

Separate costs to mitigate each penalty:

```
expProtectionCost = deepestFloor * 15
backpackProtectionCost = deepestFloor * 25
```

- These are not automatic — the player must opt in on the death screen
- If the player can't afford it, the option is grayed out with the cost shown

### Sacrificial Idol

- Purchased from the Item Shop in town
- Stored in inventory (backpack or bank)
- On death: if one is in the **backpack**, it auto-applies and fully negates backpack item loss
- Consumed on use (single-use item)
- Does **not** protect against EXP loss — that still requires gold buyout
- The death screen shows when an idol is being consumed

### Respawn Destinations

| Option | Effect |
|--------|--------|
| Return to Town | Player spawns in town hub. Current floor layout is preserved in cache (if within the 10-floor cache limit). |
| Respawn at Last Safe Spot | Player spawns at the last floor entrance/exit they passed. Current floor layout is preserved. |

### Design Principles

- **Death should hurt, but not devastate** — penalties scale with depth but cap out
- **Player agency** — every mitigation is a conscious choice, not automatic
- **Gold as insurance** — gold's primary purpose is death mitigation, not shopping
- **No auto-select** — forcing the player to read and choose prevents accidental confirmations

## Open Questions

- Should the gold buyout costs scale linearly or with a curve?
- Is losing 50% of a level's XP progress too harsh or too lenient at the cap?
- Should there be a "grace period" (e.g., no penalties on floors 1–5)?
- Can multiple Sacrificial Idols stack, or is one always enough?
- Should the death screen show a timer or be untimed?
