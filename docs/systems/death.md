# Death System

## Summary

Death triggers a **5-option sacrifice dialog**: the player pays gold to save their equipment and/or backpack, or chooses to accept the loss. The gold paid IS the sacrifice — there is no additional penalty beyond items-and-gold-or-equipment-or-both. A Sacrificial Idol in the backpack acts as a free "Save Both." XP loss still applies on top of the sacrifice choice and is **unavoidable** — there is no gold buyout for XP (the old two-step mitigation model is retired).

## Current State

**Spec status: LOCKED.** This supersedes the previous death design (two-step flow with separate EXP/Backpack mitigation toggles).

Implemented in `scripts/ui/DeathScreen.cs` (5-option dialog, cinematic, second-confirmation on Accept Fate / Quit Game) and `scripts/logic/DeathPenalty.cs` (cost formulas, PayBuyout, WipeBackpack). Equipment loss is currently stubbed as a TODO pending the equipment system (SYS-11). The companion specs in `docs/flows/death.md` and `docs/ui/death-screen.md` describe the older flow and need a follow-up refresh; this spec is authoritative in the meantime.

## Death Lore

Death in the dungeon is a **transaction** between the adventurer and the living dungeon entity.

### The Dungeon's Appetite

The dungeon absorbs everything that dies or breaks inside it — bodies, broken weapons, lost gold, spilled magic. When the dungeon regenerates itself between visits, it regurgitates what it consumed back into its halls, rearranged: first as monster parts (the obvious "loot drops"), then at deeper floors as crates, jars, and chests. This is why containers appear on floors where no one placed them. The dungeon is showing the player their own consumed history, re-sorted for digestion.

### XP Loss = The Dungeon Eating Your Memories

When an adventurer dies, the dungeon consumes a portion of their processed mana — the imprinted action memories that make up EXP. These memories are the record of every fight, every skill used, every lesson learned. Losing EXP means the adventurer literally forgets some of what their body and mind learned.

### Revival = The Dungeon Rebuilding Its Prey

The dungeon does not let adventurers die permanently. At the moment of death, the dungeon captures the adventurer's consciousness and holds it. While held, the consciousness experiences the death screen — bargaining for the terms of revival. The dungeon then constructs a new body and imbues the consciousness into it. Revival is not mercy; it is the dungeon investing in future harvests.

### The Sacrifice = Paying the Tab

The new death dialog offers the player explicit bargains. Pay gold to save the equipment, pay gold to save the backpack. If you choose to save something, the gold paid IS the replacement offering — the dungeon takes coin instead of gear. Pay for both and the dungeon takes nothing but coin. Pay for nothing and the dungeon takes everything.

**How does the dungeon take gold from a corpse?** The lore answer is: **don't ask.** This is part of the dungeon's mystery. If it can capture consciousness and build new bodies, it can reach into pockets. The gold just disappears. Flavor-text NPCs treat this as unknowable dungeon-magic — "the gold was there, and then it wasn't."

### Sacrificial Idol = A Tastier Offering

The Sacrificial Idol is crafted specifically to appeal to the dungeon's appetite. It is "tastier" than the adventurer's belongings — magically rich enough that the dungeon accepts it in place of everything else. When an idol is in the backpack at death, the dungeon takes the idol and leaves both equipment and backpack alone. One idol, one death, consumed on use.

### The Death Screen = Consciousness Negotiating

The death screen UI represents the adventurer's consciousness suspended inside the dungeon, bargaining for revival. The dungeon is in no hurry — the screen is untimed.

---

## Design

### Death Flow

```
Player HP reaches 0
  → Game pauses (enemies stop, player disabled)
  → Death cinematic plays ("YOU DIED" fade-in/out)
  → Death dialog appears with 5 options:

    ┌─────────────────────────────────────────────────┐
    │           YOU DIED                              │
    │                                                 │
    │  Deepest Floor: 42                              │
    │  EXP Loss: 16.8% of current level               │
    │                                                 │
    │  Choose your bargain:                           │
    │                                                 │
    │  [Save Both (2,500g)]                           │
    │     Keep all equipment + all backpack contents  │
    │                                                 │
    │  [Save Equipment (1,000g)]                      │
    │     Keep gear. Lose all backpack items + gold.  │
    │                                                 │
    │  [Save Backpack (1,500g)]                       │
    │     Keep pack. Lose 1 random equipped piece.    │
    │                                                 │
    │  [Accept Fate]                                  │
    │     Lose 1 equipment + all backpack + backpack  │
    │     gold. Respawn in town.                      │
    │                                                 │
    │  [Quit Game]                                    │
    │     Same penalty as Accept Fate, then quit.     │
    └─────────────────────────────────────────────────┘
```

### The Five Options

| Option | Gold Cost | Equipment | Backpack Items | Backpack Gold | Next |
|--------|-----------|-----------|----------------|---------------|------|
| Save Both | `equipCost + backpackCost` | Kept | Kept | Kept | Respawn in town |
| Save Equipment | `equipCost` | Kept | Lost (all) | Lost (all) | Respawn in town |
| Save Backpack | `backpackCost` | 1 random piece lost | Kept | Kept | Respawn in town |
| Accept Fate | 0 | 1 random piece lost | Lost (all) | Lost (all) | Respawn in town |
| Quit Game | 0 | 1 random piece lost | Lost (all) | Lost (all) | Quit to main menu |

**Key rules:**
- No auto-select. The player must actively click one button.
- Buyouts are **all-or-nothing per target.** You cannot partially save the backpack (can't pay half to keep half). Either save it or lose it.
- Player may choose which pocket pays: backpack-gold first, then bank-gold, or any combination. A sub-dialog presents the payment split before confirming.
- Both **Accept Fate** and **Quit Game** show a **second confirmation dialog** listing exact losses before proceeding. This is a final decision — no undo.
- Grayed-out options: if the player can't afford a buyout, that button is disabled with the cost shown.
- **EXP loss applies regardless of choice** (it's the dungeon's "consciousness tax"). See EXP Loss below.

### Equipment Loss Detail (1 random piece)

When equipment is not saved, the dungeon takes **exactly 1** random equipped item, uniformly distributed across all **19 equipped slots** (Head, Body, Arms, Legs, Feet, Neck, Main Hand, Off Hand, Ammo, + 10 Ring slots). A slot with no item equipped is skipped in the random roll (only currently-occupied slots are eligible).

- If the player has nothing equipped (fresh char, before starting gear), no equipment is lost.
- **Locked equipped items** are NOT protected from this loss. Lock only prevents accidental unequip.
- The lost item is destroyed — does not go to the bank, does not reappear in the dungeon.

### Buyout Cost Formulas

Equipment buyout is cheaper than backpack buyout, encouraging the player to save gear over bulk inventory:

```
equipBuyoutCost = deepestFloor × 25
backpackBuyoutCost = deepestFloor × 60
bothBuyoutCost = equipBuyoutCost + backpackBuyoutCost
```

| Floor | Save Equip | Save Pack | Save Both |
|-------|-----------|-----------|-----------|
| 1 | 25g | 60g | 85g |
| 10 | 250g | 600g | 850g |
| 25 | 625g | 1,500g | 2,125g |
| 50 | 1,250g | 3,000g | 4,250g |
| 100 | 2,500g | 6,000g | 8,500g |

*Formulas are starting values — tunable during playtesting. The relative ratio (equipment ≈ 40% of backpack cost) is locked.*

### Payment Sourcing

The player chooses which pocket pays for the buyout. When the player clicks a Save button, a sub-dialog presents:

```
┌──────────────────────────────┐
│  Pay 2,500g for: Save Both   │
│                              │
│  Backpack gold:   1,800g     │
│  Bank gold:       5,000g     │
│                              │
│  From backpack:  [1,800]  g  │
│  From bank:      [  700]  g  │
│  ──────────────────────────  │
│  Total:           2,500g     │
│                              │
│  [Confirm]  [Cancel]         │
└──────────────────────────────┘
```

- Default split: drain backpack gold first, then bank.
- Player can override the split freely.
- If the combined total is less than the buyout cost, Confirm is disabled.

### EXP Loss (independent of sacrifice choice)

EXP loss still applies on top of the sacrifice dialog. This is the dungeon's "memory tax" — it happens regardless of what items are saved.

```
expLossPercent = min(deepestFloor × 0.4, 50)
```

- Loses a percentage of XP progress **within the current level** (not total XP).
- Floor 1: 0.4% loss (negligible).
- Floor 50: 20% loss (noticeable).
- Floor 125+: 50% loss (cap — never lose more than half a level's progress in one death).

**No gold buyout for EXP loss** under the new design. EXP is the unavoidable cost of death; the sacrifice dialog handles items and gold. (This is a simplification from the old three-protection model.)

### Sacrificial Idol

- Purchased from the Guild Maid (Store tab) or dropped from late-floor chests.
- **Effect:** If an idol is in the **backpack** at time of death, it auto-consumes and acts as a free "Save Both":
  - Equipment kept.
  - Backpack items kept.
  - Backpack gold kept.
  - Gold cost: 0.
  - EXP loss still applies.
- Consumed on use (one-shot item, destroyed).
- The death dialog shows "Sacrificial Idol will be consumed" at the top and greys out the Save Both / Save Equipment / Save Backpack buttons (no reason to pay).
- Accept Fate and Quit Game are still available — the player can choose to skip the idol if they want to (idol is NOT destroyed if those are picked; it stays in the backpack and the backpack is lost anyway).
- Multiple idols stack in one slot (unlimited stacking). Only one is consumed per death.

### Respawn

After any Save / Accept Fate choice, the player respawns at the **town spawn point**. Dungeon state (floor layouts, enemy states) is reset for the next dungeon entry. The "Respawn at Last Safe Spot" option from the old design is **removed** — death always returns to town. This keeps the stakes honest: a run can be ended, but town remains the hub.

Quit Game applies the same loss (as Accept Fate) before exiting to main menu. On next game load, the player is in town with the penalty already applied.

### Order of Operations

1. HP reaches 0 → game pause + death cinematic
2. Dialog appears with 5 options + EXP loss preview
3. Player clicks a Save option → payment sub-dialog if cost > 0
4. Player confirms payment → items/equipment/gold adjusted
5. **OR** player clicks Accept Fate / Quit Game → confirmation dialog
6. Player confirms → items/equipment/gold adjusted
7. EXP loss applied (in all paths)
8. Respawn in town (Save/Accept) or quit to menu (Quit Game)

## Design Principles

- **Sacrifice is the currency of death.** Gold paid replaces items lost. The player is always trading one form of wealth for another.
- **Equipment is cheaper to save than backpack** — gear has long-term value (affixed builds), backpack is more replaceable (stackable goods + fresh drops).
- **Player agency preserved.** No auto-apply beyond the Sacrificial Idol. Every option is explicit.
- **Accept Fate is a valid strategic choice** — sometimes taking the hit is cheaper than paying gold, especially early game.
- **Locked items don't protect from death** — Lock is anti-accidental-sell/drop, not anti-dungeon.
- **EXP loss is unavoidable** — keeps death meaningful even for a rich player.

## Resolved Questions

| Question | Decision |
|----------|----------|
| Dialog format | 5 buttons on a single screen, no multi-step flow. |
| Equipment loss magnitude | Exactly 1 random equipped piece (of 19 possible). |
| Backpack loss magnitude | 100% of items + gold if not saved. |
| EXP buyout | Removed. EXP loss is unavoidable. |
| Gold pocket used for buyout | Player choice (sub-dialog shows split: backpack first, then bank, freely adjustable). |
| Can Lock protect from death? | No. Lock prevents Sell/Drop only. |
| Respawn location | Always town. "Respawn at Last Safe Spot" removed. |
| Quit Game button | Applies same penalty as Accept Fate, quits to main menu. Confirmation required. |
| Sacrificial Idol effect | Free "Save Both." Consumed on use. EXP loss still applies. |
| Why can the dungeon take gold from a corpse? | In-world answer: mystery magic. Part of the dungeon's lore — don't ask. |
