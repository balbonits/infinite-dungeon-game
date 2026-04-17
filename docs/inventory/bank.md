# Bank

## Summary

The bank is safe, permanent storage merged with the Store under the **Guild Maid** NPC. Accessed via the Guild window in town only. Items and gold held in the bank are **never lost on death**. Starts with 25 slots, expandable one slot at a time.

## Current State

**Spec status: LOCKED.** Implemented in `scripts/logic/Bank.cs` (25-slot start, `50×N` expansion cost, separate Gold pocket, `DepositGold`/`WithdrawGold`/`PurchaseExpansion`) and exposed via `GuildWindow.cs` (Bank tab + Transfer tab). Deposit/Withdraw/Sort/Filter/Search UI follow the GuildWindow MVP deviations tracked in `docs/ui/guild-window.md#current-state`. This spec supersedes the previous bank design (50 starting slots, +10 per upgrade, Banker NPC).

## Design

### Core Rules

- **Starting slots:** 25
- **Safe on death:** bank items and bank gold are never at risk
- **Town-only access:** the Guild window is only reachable in town. No dungeon access (see [death.md](../systems/death.md) for the one exception: gold is accessible to pay death buyouts)
- **Expansion:** +1 slot per purchase from the Guild Maid. Cost: `50 × N gold` (N = expansion number)
- **One item type per slot.** Unlimited quantity per slot. See [items.md](items.md) for stacking rules.
- **No stack splitting within the same storage.** A single item type occupies exactly one slot in the bank. The only way to "split" a stack is to transfer some to the backpack (Transfer tab).
- **Gold pocket:** the bank has its own gold balance (separate from backpack gold)

### NPC Access

- Accessed via the **Guild Maid** in town (the merged Shopkeeper + Banker role — see [town.md](../world/town.md))
- The Guild window has three tabs: **Store**, **Bank**, **Transfer**
- Opens on the **Bank** tab by default

### Slot Expansion

Purchased from the Guild Maid (Bank tab → Upgrade sub-dialog).

| Expansion # | New Total | Gold Cost |
|-------------|-----------|-----------|
| 1 | 26 | 50 |
| 2 | 27 | 100 |
| 5 | 30 | 250 |
| 10 | 35 | 500 |
| 25 | 50 | 1,250 |
| 50 | 75 | 2,500 |
| N | 25 + N | `50 × N` |

- No hard cap — infinite expansion fits the infinite dungeon theme.
- Cost is pure gold (no materials required). Simple, always-affordable early; scales to meaningful but never gated.
- Gold is paid from **either** the bank gold pocket or the backpack gold pocket (player's choice at the upgrade dialog).

### Gold Pocket

- Bank stores gold separately from the backpack (two pockets — one in bank, one in backpack).
- Bank gold is **safe on death**.
- Withdraw/Deposit controls live on the Bank tab's gold row (not in the backpack UI — the backpack only displays its own gold, no controls).
- Gold transfer between pockets also available in the Transfer tab.

### Sort / Filter / Search

The Bank tab supports all of the following (see [guild-window.md](../ui/guild-window.md) for UI details):

- **Sort:** Name, Value, Quantity, Category, Recently Added
- **Filter:** Category toggles (Equipment / Consumables / Materials / Special)
- **Search:** free-text filter on item name

### Item Actions (dropdown on Bank slots)

Clicking a slot opens an item-actions dropdown. Actions available from the Bank tab:

| Action | Effect |
|--------|--------|
| Inspect | Show full tooltip (affixes, item level, value) |
| Use | Only if consumable |
| Equip | Only if equippable (item goes to an equipment slot; previously equipped item returns to bank) |
| Sell | Opens Sell dialog. Greyed out if item is **Locked**. |
| Lock / Unlock | Toggles lock state. Locked items cannot be sold or dropped (bank has no Drop anyway). Lock does **not** protect from death — it only prevents accidental sale. |
| Transfer | Navigation shortcut — opens the Transfer tab with the item pre-selected |

No Drop/Destroy action in the bank. Items in the bank cannot be destroyed (they're in safe storage). Use Sell to remove.

### Sell Pricing (from bank)

- **Consumables / Materials:** 100% of buy price. The Store acts as free storage for basics.
- **Equipment:** `base_value × 0.10 × (1 + affix_count)` — 10% of base value at 0 affixes, up to 70% at 6 affixes (the affix system max).
- `base_value = item_level × rarity_multiplier × (1 + 0.5 × affix_count)`. See [items.md](items.md) for the full formula.

Sell dialog presents: amount field, slider, "Sell Amount" / "Sell All" / "Sell All but 1" buttons, and a "Don't ask me again for this item" checkbox. The checkbox also appears on the main Sell dialog for per-item mute control.

### Starting State

- **25 bank slots** on new game
- **0 bank gold** on new game (starting gold rule G1: player starts with zero)

## Resolved Questions

| Question | Decision |
|----------|----------|
| Bank from dungeon? | No — town-only. Only exception: bank gold is accessible at the death dialog for buyouts. |
| Max bank slots? | No hard cap. Cost is pure gold, `50 × N`. |
| Bank tabs/categories? | Single flat list with sort/filter/search. Simple, scales with quantity. |
| Gold in bank? | **Yes.** Two pockets (bank + backpack). Bank gold safe on death; backpack gold at risk. Reversed from original spec — design evolved when merging Store + Bank under a single NPC. |
| Stack splitting within the bank? | No. One item type per slot, one slot per item type. Transfer tab is the only way to "split" between bank and backpack. |
| Merge with Store? | Yes. Same NPC (Guild Maid) owns Store + Bank + Transfer as three tabs in one window. |

## Backpack vs. Bank (quick reference)

| Feature | Backpack | Bank |
|---------|----------|------|
| Starting slots | 15 | 25 |
| Accessible from | Anywhere | Town only (Guild window) |
| At risk on death | Yes (all items + gold) | No (items + gold safe) |
| Stack size per slot | Unlimited | Unlimited |
| Expansion NPC | Blacksmith | Guild Maid |
| Expansion size | +5 per upgrade | +1 per upgrade |
| Expansion cost | `200 × N² gold + materials` | `50 × N gold` (no materials) |
| Has Drop action? | Yes (destroys item) | No |
| Has Sell action? | No (must transfer to bank first) | Yes |
| Gold controls | Display-only label | Withdraw / Deposit buttons |
