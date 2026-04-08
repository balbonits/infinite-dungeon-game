# Death System

## Summary

Death imposes penalties scaled by the deepest floor achieved. Players can spend gold to mitigate penalties. A Sacrificial Idol consumable can negate backpack loss entirely.

## Current State

The prototype has a basic death screen ("You Died — Tap / Press R to restart") that restarts the scene. No penalties, gold buyout, or death options are implemented yet.

## Death Lore

Death in the dungeon is not a simple mechanical reset. It is a transaction between the adventurer and the living dungeon entity.

### XP Loss = The Dungeon Eating Your Memories

When an adventurer dies, the dungeon consumes a portion of their processed mana — the imprinted action memories that make up their EXP. These memories are the record of every fight, every skill used, every lesson learned. The dungeon strips them away and absorbs them as nutrition. Losing EXP on death means the adventurer literally forgets some of what their body and mind learned. Lose enough, and they lose levels — their growth partially undone.

### Revival = The Dungeon Rebuilding Its Prey

The dungeon does not let adventurers die permanently. At the moment of death, the dungeon captures the adventurer's consciousness — which is itself a form of magic — and holds it. While held, the consciousness experiences the death screen: choosing where to respawn, deciding what protections to buy. From a lore perspective, the adventurer is negotiating with the dungeon entity for the terms of their return.

The dungeon then constructs a new physical body using magic and imbues the consciousness into it. But before releasing the adventurer, it takes its cut — skimming memories (EXP) as payment. Revival is not mercy. It is the dungeon investing in future harvests. A dead adventurer earns the dungeon nothing more. A revived adventurer can grow, push deeper, and die again — yielding an even bigger meal next time.

### Gold Buyout = Bribing the Dungeon

Spending gold to protect EXP or backpack items on the death screen is the adventurer offering the dungeon an alternative payment. Gold carries its own magical value — it's a concentrated, tradeable form of processed resources. By offering enough gold, the adventurer convinces the dungeon to eat fewer of their memories or leave their belongings intact. The dungeon accepts because gold is still nutrition, just a different kind.

### Sacrificial Idol = A Tastier Offering

The Sacrificial Idol is an item crafted or purchased specifically to appeal to the dungeon's appetite. It is "tastier" than the adventurer's backpack contents — magically rich enough that the dungeon accepts it as a substitute for the items it would otherwise consume. When an idol is in the backpack at death, the dungeon takes the idol and leaves the rest of the backpack alone. One idol, one death, consumed on use.

### The Death Screen = Consciousness Negotiating

The death screen UI represents the adventurer's consciousness suspended inside the dungeon, bargaining for revival. The dungeon is in no hurry — it holds the consciousness for as long as needed (the death screen is untimed). The choices the player makes on the death screen are the terms of the deal: where to be reborn, what to sacrifice, what to protect. Once the player confirms, the deal is struck and the dungeon releases them.

---

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

### Penalty Formulas

These formulas are the starting values. All numbers are subject to change based on playtesting — fun is the top priority. The full death system (gold buyout, Sacrificial Idol, multi-step flow) is **MVP scope** — not deferred.

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

### Tuning Notes

The following parameters are starting values — all subject to change based on gameplay feedback:
- Gold buyout cost scaling (currently linear)
- EXP loss cap percentage (currently 50%)
- Whether a grace period is needed for early floors
- Sacrificial Idol stacking behavior
- The death screen is **untimed** — players take as long as they need (see [death-screen.md](../ui/death-screen.md))
