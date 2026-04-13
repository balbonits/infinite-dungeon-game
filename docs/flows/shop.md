# Flow: Shop

**Script:** `scripts/ui/ShopWindow.cs`
**Opened by:** NPC Panel → Shopkeeper → "Open Shop"

## Opening

```
ShopWindow.Open(shopInventory):
1. Store items list (all ItemDatabase items)
2. _isOpen = true
3. WindowStack.Push(this)
4. GetTree().Paused = true
5. Show overlay + center panel
6. Default to Buy mode
7. RefreshList() — populate item buttons
8. Focus first item button
```

## Layout

- **Left panel:** Scrollable item list (buttons)
- **Right panel:** Item description (name, stats, action button)

## Tabs

| Tab | Key | Shows |
|-----|-----|-------|
| Buy | Q (shoulder_left) | All shop items from ItemDatabase |
| Sell | E (shoulder_right) | Player's backpack items (non-empty slots) |

## Input

| Input | Action |
|-------|--------|
| Up/Down | Navigate item list |
| Q | Switch to Buy tab |
| E | Switch to Sell tab |
| S / action_cross | Buy/sell focused item |
| D / Escape | Close shop |

## Buy Flow

```
1. Navigate to item (focus updates description panel)
2. Press S or click "Buy" action button
3. OnActionPressed():
   a. inventory.TryBuy(_selectedItem)
   b. If success: Toast "Bought X", update gold label, refresh list
   c. If fail: Toast "Cannot afford"
```

## Sell Flow

```
1. Switch to Sell tab (Q key)
2. List refreshes with backpack items
3. Navigate to item
4. Press S or click "Sell" action button
5. OnActionPressed():
   a. inventory.TrySell(_selectedIndex)
   b. If success: Toast "Sold X", refresh list
   c. Item removed from backpack
```

## Closing

```
Close():
1. _isOpen = false
2. WindowStack.Pop(this)
3. GetTree().Paused = false
4. Hide overlay + center
```
