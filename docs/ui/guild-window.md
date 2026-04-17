# Guild Window

## Summary

The Guild window is a town-only UI panel owned by the **Guild Maid** NPC. It merges the Store, Bank, and Transfer functions into a three-tab interface. Title: "Guild". Always opens on the **Bank** tab.

## Current State

**Spec status: LOCKED.** Implemented in `scripts/ui/GuildWindow.cs` and registered in
`scenes/main.tscn`. Replaces the planned separate `ShopWindow` / `BankWindow` (those scripts
still exist for test compat but are not placed in the town scene).

**MVP deviations from the full spec** (tracked as polish tickets):

- **Store inventory**: currently surfaces `ItemDef`s with `Category == Consumable`
  (potions, idol). Basic materials + basic ammo per the full spec will land when
  `ItemDatabase` gains those entries (content ticket).
- **Store Buy dialog**: uses an ActionMenu with preset buttons ("Buy 1", "Buy 10",
  toggle Send-to target). The spec calls for an amount input + slider + explicit
  "Send to Bank/Backpack" radio — deferred.
- **Transfer tab**: item transfer is click-to-move-entire-stack. The amount-input
  dialog per B1: a is deferred; current UI is functional but coarser than spec.
- **Bank upgrade payment split**: the UI auto-draws backpack-gold-first rather than
  showing the payment-split dialog from the spec. Same auto-split applies to every
  caller of `DeathPenalty.PayBuyout`.
- **Bank gold Withdraw/Deposit**: the UI uses "Withdraw All" / "Deposit All" buttons.
  The spec calls for an amount-input dialog (quick buttons 1/10/100/All + text field)
  — deferred.
- **Bank sort/filter/search**: UI exposes only the slot grid. Sort dropdown, category
  toggles, and text search per spec are deferred.
- **Sell dialog**: Bank tab's item-actions provide "Sell 1" / "Sell All" action-menu
  entries. The spec's full Sell dialog (amount input + slider + "Sell All but 1" +
  per-item "Don't ask me again" mute) is deferred.
- **Drop dialog**: BackpackWindow's drop shows a yes/no confirmation but always
  destroys the entire stack. The partial-drop amount input per spec is deferred.

These deviations are deliberate MVP scope — the full dialogs will be added once the
rest of SYS-12 settles.

## Design

### Layout

```
┌─────────────────────────────────────────────────┐
│  Guild                                   [✕]    │
├─────────────────────────────────────────────────┤
│  [ Store ]  [ Bank ]  [ Transfer ]              │
├─────────────────────────────────────────────────┤
│                                                 │
│        (tab content — full grid area)           │
│                                                 │
│                                                 │
├─────────────────────────────────────────────────┤
│  Bank gold: 12.3K     Backpack gold: 487        │
└─────────────────────────────────────────────────┘
```

- Title bar: "Guild" + close button (X / Esc)
- Tab bar: Q/E switches tabs (keyboard), arrow keys or mouse click also work
- Gold status strip at the bottom shows both pockets at all times (read-only display across all three tabs)

### Opening Behavior

- Triggered by interacting with the **Guild Maid** NPC (walk up + S).
- Always opens on the **Bank** tab (Y2: c — predictable, most-used).
- Modal — player input is captured by the Guild window via `WindowStack`. See [GameWindow.cs](../../scripts/ui/GameWindow.cs) base class.
- Pauses the game tree (town NPCs/movement freeze while the window is open).
- Closing: Esc / D / click X. Returns focus to the overworld.

---

## Store Tab

The Store sells basic consumables, basic crafting materials, and basic ammo. Infinite stock. Fixed prices.

### Inventory

The Store's catalog is fixed and always in stock. It does NOT sell equipment (the Blacksmith crafts equipment from dropped base items) and does NOT sell scrolls (scope-out for this spec).

**Categories stocked:**

| Category | Examples |
|----------|----------|
| Basic Consumables | Small Health Potion, Small Mana Potion, Sacrificial Idol |
| Basic Crafting Materials | Glass Bottle, Arrow Shaft, Feather, Tanning Oil, Thread, String, Flux Powder |
| Basic Ammo | Iron Arrows, Iron Bolts |

**Note on crafting materials:** basic materials are inputs for ALL tiers of Blacksmith recipes. For example, arrow shafts are used in both iron arrows (tier 1) and mithril arrows (tier 5); flux powder is used in all metal tiers. Higher-tier recipes additionally require higher-tier mats that only drop from dungeon monsters or are manufactured at the Blacksmith's Workshop — the Store never stocks mid/high-tier materials.

### Prices

Fixed prices, always. The player does not have to guess or check daily rotation. See also [items.md](../inventory/items.md#sell-pricing) — basic consumables and materials sell back at 100% of buy price, so the Store effectively acts as free storage for basics.

### Buy Flow

```
Player clicks an item row in the Store grid
  → Buy dialog opens:
     ┌─────────────────────────────────┐
     │  Buy: Small Health Potion       │
     │  Unit price: 50g                │
     │                                 │
     │  Amount: [     10    ] (slider) │
     │  Total: 500g                    │
     │                                 │
     │  Send to: (●) Bank  (○) Backpack│
     │                                 │
     │  [Confirm]  [Cancel]            │
     └─────────────────────────────────┘
  → Player enters amount (default 1)
  → Player picks "Send to Bank" or "Send to Backpack"
  → Default is whichever was used last (sticky across buys this session).
    First-ever buy default: Bank.
  → Click Confirm → gold deducted, item added to chosen storage
  → Stack merges with existing slot if item type already present,
    else occupies a new slot (fails with "storage full" if no slot).
```

**Gold source:** buys draw from the **backpack pocket first**, then the bank pocket (no sub-dialog — automatic, since Store purchases are frequent and low-stakes). If the player wants to spend bank gold first, they can Withdraw to the backpack manually.

---

## Bank Tab

The Bank tab displays the player's bank storage — a grid of slots, each with item icon + stack count. Starts at 25 slots, expandable via Upgrade sub-dialog.

### Layout

```
┌────────────────────────────────────────────────────┐
│  Bank  (37/50 slots)                   [Upgrade]   │
│  Sort: [Name ▾]  Filter: [All ▾]  Search: [_____]  │
├────────────────────────────────────────────────────┤
│  ┌──┐ ┌──┐ ┌──┐ ┌──┐ ┌──┐ ┌──┐ ┌──┐ ┌──┐ ┌──┐ ┌──┐│
│  │🗡1│ │🛡1│ │💉K│ │⚙M│ │🏹K│ │...│ │...│ │...│ │...│ │...││
│  └──┘ └──┘ └──┘ └──┘ └──┘ └──┘ └──┘ └──┘ └──┘ └──┘│
│  (grid continues, scrollable)                      │
├────────────────────────────────────────────────────┤
│  Gold: 12.3K     [Withdraw]  [Deposit]             │
└────────────────────────────────────────────────────┘
```

### Sort Options (C1: all the above)

Dropdown with: **Name**, **Value**, **Quantity**, **Category**, **Recently Added**. All five always available. Last-used sort persists across sessions.

### Filter Options (C2: text + category)

- Text search: free-text on item name (substring, case-insensitive)
- Category toggles: **Equipment**, **Consumables**, **Materials**, **Special** — multiselect; if none selected, all categories shown.

Both filters apply together (AND).

### Upgrade Sub-Dialog (C3: b)

Clicking the **Upgrade** button in the Bank tab's header opens a dedicated sub-dialog:

```
┌──────────────────────────────────┐
│  Upgrade Bank Storage            │
│                                  │
│  Current: 37 slots               │
│  After:   38 slots (+1)          │
│  Cost:    1,300g                 │
│                                  │
│  Pay from:                       │
│  ( ) Backpack gold (487g)        │
│  (●) Bank gold (12,300g)         │
│                                  │
│  [Upgrade]  [Cancel]             │
└──────────────────────────────────┘
```

- Cost formula: `50 × N gold` (N = expansion number, starting at 1 for the first upgrade).
- Default payment source: whichever pocket has enough. If both have enough, default is bank.
- Greyed out Upgrade button if combined gold < cost.

### Gold Row (bottom of Bank tab)

Always visible at the bottom of the Bank tab. Shows the current bank gold balance (abbreviated) with **Withdraw** and **Deposit** buttons.

- **Withdraw:** opens a small amount-input dialog. Moves gold from bank → backpack.
- **Deposit:** opens a small amount-input dialog. Moves gold from backpack → bank.
- Both dialogs have quick buttons: `1`, `10`, `100`, `All`, and a text field for custom amounts.

### Item Actions Dropdown (Bank)

Click a slot → dropdown appears with actions (see [items.md](../inventory/items.md#item-actions) for the full reference). Bank-available actions:

- Inspect, Use, Equip, Sell, Lock/Unlock, Transfer.
- **No Drop** in the bank (items in safe storage cannot be destroyed).

### Sell Action

Opens the Sell dialog (see [items.md](../inventory/items.md#sell-dialog)). Sold gold goes to the **bank gold pocket** (since the transaction is happening in the bank).

---

## Transfer Tab

Move items and gold between the bank and the backpack. The two grids are displayed side-by-side.

### Layout

```
┌──────────────────────────────────────────────────────┐
│                    Transfer                          │
├──────────────────────────────────────────────────────┤
│  BANK (37/50)              BACKPACK (11/15)          │
│  ┌──┐ ┌──┐ ┌──┐ ...         ┌──┐ ┌──┐ ┌──┐ ...       │
│  │🗡1│ │🛡1│ │💉K│            │⚙5│ │🏹│ │🎯│           │
│  └──┘ └──┘ └──┘              └──┘ └──┘ └──┘          │
│                                                      │
│  (scroll)                   (scroll)                 │
├──────────────────────────────────────────────────────┤
│  Gold transfer:                                      │
│  Bank: 12.3K  [← 100 →]  Backpack: 487               │
│  Amount: [  100 ] (slider)  [Transfer →]  [← Transfer]│
└──────────────────────────────────────────────────────┘
```

### Item Transfer (B1: a)

Click an item slot on either side → an **amount-input dialog** appears asking how much to transfer. Confirm → the specified quantity moves to the opposite side.

```
┌─────────────────────────────────┐
│  Transfer: Small Health Potion  │
│  Bank has: 200   Backpack: 0    │
│                                 │
│  Amount: [     50    ] (slider) │
│  [Transfer All] [50] [Cancel]   │
└─────────────────────────────────┘
```

- **Within-storage invariant (B3):** each storage has at most one slot per item type. Transfers respect this — the destination merges with an existing slot if one exists, or creates a new slot if not.
- **Cross-storage splitting IS supported** via the amount input. Move 50 of a 200-stack to bank → backpack keeps 150, bank gets 50 (new slot or merged with existing). This is the primary way to maintain separate quantities on each side.
- **Transfer All** button shortcut moves the entire stack in one click.
- If the destination storage is full AND does not already have a slot for this item type, Confirm is disabled with a tooltip: "Destination storage full."

**Why the dialog (not one-click full-stack):** the user explicitly chose B1: a — amount input on transfer — to allow fine-grained control over how much of a stack sits in the at-risk backpack vs the safe bank. This is a deliberate risk-management tool, not friction.

### Gold Transfer (B2: b)

Bottom row of the Transfer tab. Controls:

- Current balance shown on each side.
- Amount input field + slider.
- Two directional buttons: `[Transfer →]` (backpack → bank) and `[← Transfer]` (bank → backpack).
- Quick buttons: `1`, `10`, `100`, `All`.

---

## Item Actions Dropdown (full reference)

Applies across Bank, Backpack, and Equipped views (K2: c — same dropdown everywhere applicable).

Clicking an item slot opens a contextual dropdown. Actions available depend on item type and location:

| Action | Bank | Backpack | Equipped |
|--------|:----:|:--------:|:--------:|
| Inspect | ✓ | ✓ | ✓ |
| Use | ✓* | ✓* | — |
| Equip | ✓† | ✓† | — |
| Unequip | — | — | ✓ |
| Sell | ✓‡ | — | — |
| Lock / Unlock | ✓ | ✓ | ✓ |
| Transfer | ✓§ | ✓§ | — |
| **Drop** | — | ✓ | — |

\* Consumables only.
† Equippable only. Auto-swaps if the slot is occupied.
‡ Greyed out if Locked.
§ Navigation shortcut — opens the Transfer tab with the item pre-selected.

### Drop (backpack only)

The Drop action destroys an item permanently. Single confirmation:

```
┌───────────────────────────────────┐
│  Destroy 50 "Arrow Shafts"?       │
│  This cannot be undone.           │
│                                   │
│  Amount to destroy: [ 50 ] (slider)│
│  [Drop All] [Drop]  [Cancel]      │
└───────────────────────────────────┘
```

For stacks:
- Amount field + slider for partial drops
- **Drop All** button shortcut
- No "Don't ask me again" — Drop is always confirmed (it's a destructive action).

Greyed out if the item is Locked.

### Sell Dialog

See [items.md](../inventory/items.md#sell-dialog) for full spec. Summary: amount field + slider + Sell Amount / Sell All / Sell All but 1 buttons + per-item "Don't ask me again" checkbox.

### Lock / Unlock

Toggles the `Locked` flag on the item instance. Effects:
- Greys out Sell action (bank) and Drop action (backpack)
- For equipped items: greys out Unequip (prevents accidental swap via dropdown)
- **Does NOT protect from death-loss** — locked items are still destroyed if the player doesn't save the backpack

Lock state persists across transfers, sessions, and equip/unequip cycles.

---

## Keyboard Navigation

The Guild window follows the standard game window patterns (see [GameWindow.cs](../../scripts/ui/GameWindow.cs)):

| Key | Action |
|-----|--------|
| Esc / D | Close the Guild window (returns to overworld) |
| Q / E | Switch tab (Store ↔ Bank ↔ Transfer) |
| Arrow keys | Navigate grid slots and menu buttons |
| S / Enter | Activate focused slot (opens item-actions dropdown) or confirm focused button |
| Tab | Cycle between grid / filters / gold row (within a tab) |

Item-actions dropdowns are their own modal (stacked on top of the Guild window via `WindowStack`). Same nav rules: arrow keys + S/Enter to pick, D/Esc to close the dropdown.

---

## Related Specs

- [bank.md](../inventory/bank.md) — bank storage rules, expansion costs, gold pocket
- [backpack.md](../inventory/backpack.md) — backpack rules, unlimited stacking, Drop mechanic
- [items.md](../inventory/items.md) — stacking, number display, sell pricing, item actions
- [death.md](../systems/death.md) — 5-option sacrifice dialog (separate window, NOT in Guild)
- [town.md](../world/town.md) — Guild Maid NPC placement and lore

---

## Resolved Questions

| Question | Decision |
|----------|----------|
| Window title | "Guild" (fixed, doesn't change per tab) |
| Default tab | Bank |
| Merge Shop + Bank? | Yes — merged under Guild Maid, with Transfer as the third tab |
| Window framework | `GameWindow` base class (standard modal, `WindowStack` integration) |
| Keyboard-first? | Yes — Q/E tab switching, arrow keys for grid, S/Enter to confirm. Mouse is supported but not required. |
| Drop in Bank? | No (bank is safe storage — Sell is the only removal path) |
| Sell in Backpack? | No (transfer to bank first, then Sell) |
| Stack splitting? | No (one type per slot per storage) |
| Item actions across views? | Yes — same dropdown in Bank, Backpack, and Equipped (action set filtered by context) |
