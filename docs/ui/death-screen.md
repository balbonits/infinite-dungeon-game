# Death Screen

## Summary

A multi-step UI flow that appears when the player dies. The player chooses a respawn destination, toggles mitigation options, reviews penalties, and confirms.

## Current State

The prototype has a basic death screen: a centered panel showing "You Died" with instructions to press R or tap to restart. No penalty options, no choices.

## Design

### Flow Diagram

```
┌─────────────────────────────┐
│         YOU DIED             │
│                              │
│  Step 1: Choose destination  │
│  ○ Return to Town            │
│  ○ Respawn at Last Safe Spot │
│                              │
│  Step 2: Mitigations         │
│  □ Protect EXP (cost: X g)  │
│  □ Protect Backpack (Y g)   │
│  🗿 Sacrificial Idol: [Yes]  │
│                              │
│  Step 3: Penalty Summary     │
│  EXP loss: Z%                │
│  Items lost: N items         │
│  Total gold cost: X + Y      │
│                              │
│  [Confirm Respawn]           │
└─────────────────────────────┘
```

### Step-by-Step

#### Step 1: Choose Destination
- Two radio-button options (neither pre-selected)
- **Return to Town:** safe restart, lose floor position
- **Respawn at Last Safe Spot:** stay near where you died
- Must select one before proceeding

#### Step 2: Toggle Mitigations
- **Protect EXP:** checkbox, shows gold cost. Grayed out if insufficient gold.
- **Protect Backpack:** checkbox, shows gold cost. Grayed out if insufficient gold.
- **Sacrificial Idol:** auto-detected. If one is in the backpack, it displays as active. Shows that it will be consumed.
- None are pre-checked — player must opt in.

#### Step 3: Review Summary
- Shows the exact penalties that will apply based on current selections:
  - EXP loss percentage and absolute XP amount
  - Number of items that will be lost (and which ones, if known)
  - Total gold cost of selected mitigations
  - Current gold balance and remaining gold after costs
- Updates dynamically as the player toggles mitigations

#### Step 4: Confirm
- Single "Confirm Respawn" button
- Triggers a confirmation dialog: "Are you sure? You will lose [specific penalties]."
- On confirm: penalties applied, player respawns at chosen destination
- On cancel: return to the death screen (no penalties applied yet)

### Penalty Formulas

See [death.md](../systems/death.md) for the full penalty formulas.

### UI Guidelines

- The death screen should feel weighty — dark overlay, deliberate pacing
- No timers or pressure — the player can take as long as they need
- Clear visual distinction between "protected" and "at risk" items/XP
- Gold costs should be prominent so the player understands the trade-off
- The confirm button should look different from other buttons (e.g., red/warning color)

## Open Questions

- Should the death screen show the player's inventory so they can see what's at risk?
- Should there be an animation or transition when the death screen appears?
- How should the UI work on mobile (scrollable panel? multi-page?)?
- Should the death screen track death statistics (deaths per floor, total deaths)?
