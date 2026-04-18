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

Post-BLACKSMITH-MENU-IMPL-01 + GUILD-MAID-MENU-IMPL-01: **3 NPCs** in the town, each with exactly **one** service button — the service windows themselves are tabbed and handle multi-service routing internally.

| NPC Name | Button Text | Opens | Window structure |
|----------|------------|-------|------------------|
| Guild Maid | "Open Guild" | `GuildWindow.Instance.Open()` | 2 tabs: **Bank** (storage + Bank↔Backpack transfer + gold controls) / **Teleport** (floor fast-travel) |
| Blacksmith | "Open Forge" | `BlacksmithWindow.Instance.Open()` | 4 tabs: **Forge** (affix application) / **Craft** (recipes — placeholder) / **Recycle** (break down gear) / **Shop** (caravan-stocked consumables) |
| Village Chief | "View Quests" | `QuestPanel.Instance.Open()` | Single-service panel (quest queue) |

A "Cancel" button is always added after the service button. The service button is default-focused (keyboard nav). Tab-cycling within windows uses Q/E — see [hud-layout.md §Hotkeys](../ui/hud-layout.md).

**Retired NPCs** (not in the town scene; still dispatchable via direct code for test compat only):

| NPC Name | Button Text | Opens |
|----------|------------|-------|
| Shopkeeper | "Browse Wares" | `ShopWindow.Instance.Open(itemList)` |
| Guild Master | "View Quests" | `QuestPanel.Instance.Open()` |
| Teleporter | "Teleport" | `TeleportDialog.Instance.Show()` |
| Banker | "Open Vault" | `BankWindow.Instance.Open()` |

These legacy paths keep existing tests passing and protect against direct-code invocation, but the town scene never spawns these NPCs. `TeleportDialog` is kept for the Teleporter-NPC legacy path even though the Guild Maid's Teleport tab is now the canonical entry point.

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
