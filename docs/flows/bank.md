# Flow: Guild Bank

**Script:** `scripts/ui/GuildWindow.cs` (new — replaces the old `BankWindow.cs` and `ShopWindow.cs`)
**Opened by:** NPC Panel → Guild Maid → "Open Guild"
**UI spec:** [../ui/guild-window.md](../ui/guild-window.md)

The Guild window has three tabs: **Store**, **Bank**, **Transfer**. This flow doc describes the Bank tab specifically. For Store flows see [shop.md](shop.md). For full UI reference see the UI spec.

## Opening

```
GuildWindow.Open():
1. WindowStack.Push(this), tree paused
2. Default tab: Bank (Y2: c)
3. RefreshBankGrid() — populate slots from GameState.Bank
4. Focus first slot
```

## Bank Tab Operations

### Click an item slot → open item-actions dropdown

```
OnSlotActivated(slotIndex):
1. Open ItemActionDropdown positioned near the slot
2. Context: "Bank" → available actions = Inspect, Use, Equip, Sell, Lock/Unlock, Transfer
3. User picks action → corresponding handler runs
```

### Sell

```
OnSellPressed(slotIndex):
1. If item.Locked → disabled, Toast "Item is locked"
2. Open SellDialog with item + stack count
3. User enters amount + confirms
4. Bank.gold += sell_price × amount
5. Update stack (remove slot if quantity → 0)
6. Toast "Sold Nx <item>"
```

Sell price formula: see [items.md](../inventory/items.md#sell-pricing).

### Lock / Unlock

```
OnLockPressed(slotIndex):
1. Toggle item.Locked flag
2. Persist to save data
3. UI refreshes: item shows a lock icon overlay when Locked
```

### Transfer (navigation shortcut)

```
OnTransferPressed(slotIndex):
1. Switch to Transfer tab
2. Pre-select the item on the Bank side
3. Amount-input dialog opens (per B1: a) — user picks quantity + Confirm
4. Specified quantity moves to backpack (merges with existing backpack slot, or creates a new one)
```

### Upgrade (+1 slot)

```
OnUpgradePressed():
1. Calculate cost = 50 × (expansionCount + 1)
2. Open UpgradeDialog showing cost + a payment-split input
   (Bank: X | Backpack: Y, with sliders/fields for how much comes from each pocket)
3. User confirms the split (total must equal cost)
4. Deduct gold from each pocket per the split
5. Bank.ExpansionCount += 1
6. Bank.SlotCount += 1
7. Refresh grid (one empty slot appears at the end)

MVP implementation (GuildWindow.cs): the dialog is deferred — the in-game button
currently auto-splits (backpack first, then bank) with no per-pocket input. Full
split-dialog is a polish ticket.
```

### Gold Withdraw / Deposit

```
OnWithdrawPressed():
1. Open amount-input dialog (quick buttons: 1, 10, 100, All)
2. User enters amount
3. If Bank.gold >= amount:
     Bank.gold -= amount
     Backpack.gold += amount
4. Else: disabled, Toast "Insufficient bank gold"

OnDepositPressed():
1. Open amount-input dialog (same UI)
2. User enters amount
3. If Backpack.gold >= amount:
     Backpack.gold -= amount
     Bank.gold += amount
4. Else: disabled, Toast "Insufficient backpack gold"
```

## Transfer Tab (separate flow)

Covered under the same `GuildWindow.cs` script. See [../ui/guild-window.md](../ui/guild-window.md#transfer-tab) for UI. Key operations:

- Click bank slot → amount-input dialog (per B1: a) → move specified quantity to backpack (merges or creates slot)
- Click backpack slot → same flow in reverse
- "Transfer All" button shortcut moves the full stack in one click
- Gold transfer controls in bottom row: amount input + slider + left/right transfer buttons

## Input

| Input | Action |
|-------|--------|
| Q / E | Switch tab (Store ↔ Bank ↔ Transfer) |
| Arrow keys | Navigate grid slots and controls |
| S / action_cross | Activate focused slot (opens item-actions dropdown) or confirm focused button |
| D / Escape | Close Guild window |

## Closing

```
Close():
1. WindowStack.Pop(this)
2. Tree unpaused
3. Return control to overworld (player can move again)
```

## Superseded

- Old `BankWindow.cs` script path retained until the new `GuildWindow.cs` lands; then deleted.
- Old `ShopWindow.cs` likewise.
- Banker NPC retired (merged into Guild Maid).
- Item Shop NPC retired (merged into Guild Maid → Store tab).
