# Flow: NPC Interaction

**Script:** `scripts/Npc.cs` (proximity), `scripts/ui/NpcPanel.cs` (panel)

## Proximity Detection

- Each NPC has an Area2D with radius 40 units
- When player enters: `_playerNearby = true`, prompt "Press S" shown
- When player exits: `_playerNearby = false`, prompt hidden, panel auto-dismissed

## Opening NPC Panel

```
1. Player within 40 units of NPC
2. Player presses S (action_cross)
3. Npc._UnhandledInput checks _playerNearby
4. NpcPanel.Instance.Show(npcName, greeting) called
5. Panel creates service button based on NPC name
6. WindowStack.Push(this) — becomes topmost modal
7. Fade in tween: _center modulate:a 0 → 1.0 over 0.15s
8. Focus set to first button
```

## Service Buttons Per NPC

| NPC Name | Button Text | Opens |
|----------|------------|-------|
| Shopkeeper | "Open Shop" | `ShopWindow.Instance.Open(itemList)` |
| Blacksmith | "Open Forge" | `BlacksmithWindow.Instance.Open()` |
| Guild Master | "View Quests" | `QuestPanel.Instance.Open()` |
| Teleporter | "Teleport" | `TeleportDialog.Instance.Show()` |
| Banker | "Open Bank" | `BankWindow.Instance.Open()` |

A "Cancel" button is always added below the service button.

## Panel Input

| Input | Action |
|-------|--------|
| Up/Down | Navigate buttons (KeyboardNav) |
| S / action_cross | Press focused button |
| D / Escape | Cancel → `Hide()` |

`KeyboardNav.BlockIfNotTopmost()` ensures only this modal receives input.

## Service Button Press

```
1. OnServicePressed(npcName) called
2. NpcPanel.Hide() — fade out 0.1s, WindowStack.Pop
3. Service window opened (ShopWindow, BankWindow, etc.)
```

## Closing

```
Hide():
1. Fade tween: _center modulate:a → 0 over 0.1s
2. Callback: _overlay.Visible = false, _center.Visible = false
3. WindowStack.Pop(this)
```

Also auto-closes when player walks away from NPC (OnPlayerExited).

## AutoPilot Sequence (visit Shopkeeper)

```csharp
// Move to shopkeeper area (tile 5,7 converted to world coords)
await act.MoveToward(shopkeeperWorldPos, 5f);
await act.WaitSeconds(0.3); // let proximity trigger

// Open NPC panel
act.Press("action_cross");
await act.WaitSeconds(0.5); // panel fade in

// Press service button (first button = service, second = cancel)
act.Press("action_cross"); // press focused service button
await act.WaitSeconds(0.5); // service window opens

// ... interact with service window ...

// Close (D or Escape)
act.Press("action_circle");
await act.WaitSeconds(0.3);
```
