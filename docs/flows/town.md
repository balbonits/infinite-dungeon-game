# Flow: Town

**Script:** `scripts/Town.cs`
**Scene:** `scenes/town.tscn`
**Size:** 24x20 tiles (`Constants.Town.Width` x `Constants.Town.Height`)

## Player Spawn

- **Position:** Center-bottom of town — tile `(Width/2, Height-5)` = `(12, 15)` converted to local coords via `MapToLocal()`
- **Collision layer:** `Constants.Layers.Player` (bit 1)
- **Grace period:** Started on spawn

## NPC Positions

| NPC | Tile Position | Constants ref | Services |
|-----|--------------|---------------|----------|
| Shopkeeper | (5, 7) | — | ShopWindow.Open() |
| Blacksmith | (18, 7) | — | BlacksmithWindow.Open() |
| Guild Master | (5, 14) | — | QuestPanel.Open() |
| Teleporter | (18, 14) | — | TeleportDialog.Show() |
| Banker | (12, 14) | — | BankWindow.Open() |

Each NPC has:
- Sprite at position with Y offset
- Name label (always visible above)
- Collision body (StaticBody2D + CircleShape, radius = `Constants.Town.NpcCollisionRadius` = 14.0)
- Interaction area (Area2D, radius = 40 units)
- Interact prompt label ("Press S") — initially hidden

## NPC Interaction Flow

See `docs/flows/npc-interaction.md` for details.

```
1. Player enters NPC Area2D (40 unit radius)
   → OnPlayerEntered(): _playerNearby = true, show "Press S" prompt
2. Player presses S (action_cross)
   → NpcPanel.Instance.Show(npcName, greeting)
3. Player walks away from NPC
   → OnPlayerExited(): hide prompt, NpcPanel.Instance.Hide()
```

## Dungeon Entrance

- **Position:** Top center of town — tile `(Width/2, 2)` = `(12, 2)`
- **Visual:** Cave entrance sprite + "Dungeon Entrance" red label
- **Trigger:** Area2D with radius = `Constants.Effects.StairsTriggerRadius`

### Entry Flow

```
1. Player body enters dungeon entrance Area2D
2. Guard: not already transitioning
3. ScreenTransition.Instance.Play():
   message = floor label, callback = Main.Instance.LoadDungeon()
4. During transition hold phase:
   a. Main.DoLoadDungeon() swaps Town for Dungeon scene
   b. SaveManager auto-saves
5. Transition fades in, player is in dungeon
```
