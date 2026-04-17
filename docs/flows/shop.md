# Flow: Guild Store

**Script:** `scripts/ui/GuildWindow.cs` (new — replaces the old `ShopWindow.cs`)
**Opened by:** NPC Panel → Guild Maid → "Open Guild"
**UI spec:** [../ui/guild-window.md](../ui/guild-window.md)

The Guild window has three tabs: **Store**, **Bank**, **Transfer**. This flow doc describes the Store tab specifically. For Bank + Transfer flows see [bank.md](bank.md).

## Opening

The Store tab is **not** the default tab — the Guild window opens on the **Bank** tab. The user presses **E** (or clicks) to switch to the Store tab.

## Store Tab Operations

### Store inventory

Fixed catalog (always in stock, fixed prices). See [town.md](../world/town.md#guild-maid) for the full item list. Store only sells **basic consumables**, **basic crafting materials**, and **basic ammo**. No equipment, no scrolls, no mid/high-tier materials.

### Click item row → open Buy dialog

```
OnItemClicked(itemId):
1. Open BuyDialog with item name + unit price
2. Default amount: 1
3. Default "Send to" target: last-used value (sticky); first-ever = Bank
```

BuyDialog UI:
- Amount input field + slider (0 → reasonable max, e.g., 999)
- Live total: `amount × unit_price` gold
- Radio group: "Send to Bank" / "Send to Backpack"
- Confirm / Cancel buttons

### Confirm purchase

```
OnBuyConfirmed(itemId, amount, targetStorage):
1. totalCost = amount × unit_price
2. If Backpack.gold + Bank.gold < totalCost → disabled, Toast "Cannot afford"
3. Draw from Backpack.gold first, then Bank.gold (auto — no sub-dialog for store purchases)
4. AddItemToStorage(targetStorage, itemId, amount):
   a. If slot exists for itemId → merge (stack += amount)
   b. Else if free slot available → create new slot
   c. Else → Toast "<targetStorage> is full. Send elsewhere." (allow changing target in dialog)
5. Toast "Bought Nx <item> → <targetStorage>"
6. Remember target for next buy this session
```

**Note:** if the target storage is full for this item, the dialog prompts the user to change the target rather than failing the transaction.

### Sell? No.

The Store tab does **not** sell. Selling happens in the **Bank tab** via the item-actions dropdown. Players must transfer items from backpack to bank first, then sell. This is an intentional friction point — selling is not a fast-lane action.

## Input

| Input | Action |
|-------|--------|
| Q / E | Switch tab (Store ↔ Bank ↔ Transfer) |
| Arrow keys | Navigate item list and dialog controls |
| S / action_cross | Click the focused row (opens Buy dialog) or confirm dialog button |
| D / Escape | Close Buy dialog / Guild window |

## Closing

Same as Bank flow — Esc / D / X closes the Guild window, pops WindowStack, unpauses tree.

## Superseded

- Old `ShopWindow.cs` retained until `GuildWindow.cs` lands; then deleted.
- Sell mode removed from Store tab (moved to Bank tab's item-actions dropdown).
- Item Shop NPC retired.
