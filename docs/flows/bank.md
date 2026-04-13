# Flow: Bank

**Script:** `scripts/ui/BankWindow.cs`
**Opened by:** NPC Panel → Banker → "Open Bank"

## Operations

### Deposit

```
1. Select item in backpack (left panel)
2. Press deposit button or S
3. Bank.Deposit(inventory, slotIndex)
4. If success: item moves from backpack to bank storage
5. If fail: bank full → Toast feedback
```

### Withdraw

```
1. Select item in bank storage (right panel)
2. Press withdraw button or S
3. Bank.Withdraw(inventory, bankSlotIndex)
4. If success: item moves from bank to backpack
5. If fail: backpack full → Toast feedback
```

### Expand

```
1. Press "Expand" button
2. Cost: 500 * (N+1)^2 where N = current expansion count
3. Bank.PurchaseExpansion(inventory)
4. If success: gold deducted, slots increase by Bank.SlotsPerExpansion
5. If fail: not enough gold → Toast feedback
```

## Input

| Input | Action |
|-------|--------|
| Up/Down | Navigate items |
| Left/Right | Switch between backpack and bank panels |
| S / action_cross | Deposit or withdraw focused item |
| D / Escape | Close bank |

## Closing

Same pattern as ShopWindow: `WindowStack.Pop`, unpause.
