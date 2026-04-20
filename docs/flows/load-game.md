# Flow: Load Game Screen

**Status:** Spec. Implementation pending on a separate branch.
**Script (planned):** `scripts/ui/LoadGameScreen.cs`
**Entry point:** Splash screen → "Continue" button (above "New Game")
**Parallel to:** `docs/flows/class-select.md` — same layout, same navigation model

## Summary

The Load Game screen replaces the old single-Character-Card approach on the splash screen. It presents three save slots side-by-side — each a character summary card or an empty placeholder — letting the player pick which save to load or delete. Visually and interactively modeled on the Class Select screen, so the navigation feel is consistent.

## Splash Screen Entry

The splash screen's button order is updated:

```
┌─────────────────────┐
│     Continue        │   ← new; disabled + greyed if no saves exist
│                     │
│     New Game        │
│     Tutorial        │
│     Settings        │
│     Exit Game       │
└─────────────────────┘
```

- **Continue** — always rendered, but greyed out / disabled when no save slots are populated. Opens the Load Game screen when pressed. Positioned **above** New Game so returning players don't have to scroll past "New Game" to resume.
- No more inline Character Card on the splash screen — that role is now fully owned by the Load Game screen.

## Constraints

- **Maximum 3 save slots**, fixed for now.
- Save files live at `user://save_0.json`, `user://save_1.json`, `user://save_2.json`.
- An empty slot = no file present on disk for that index.
- A populated slot = valid save file deserializable into a `CharacterCard`.

## Layout

```
┌─────────────────────────────────────────────────────────────┐
│                         LOAD GAME                           │
│          Choose a character to continue your descent.       │
│                                                             │
│   ┌───────────┐      ┌───────────┐      ┌───────────┐       │
│   │ [X] (red) │      │ [X] (red) │      │           │       │
│   │           │      │           │      │           │       │
│   │  Warrior  │      │  Ranger   │      │   Empty   │       │
│   │  [sprite] │      │  [sprite] │      │    Slot   │       │
│   │  Level 12 │      │  Level 3  │      │           │       │
│   │  Floor 24 │      │  Floor 5  │      │           │       │
│   └───────────┘      └───────────┘      └───────────┘       │
│                                                             │
│              ┌─────────┐      ┌──────────┐                  │
│              │  Back   │      │   Load   │                  │
│              └─────────┘      └──────────┘                  │
└─────────────────────────────────────────────────────────────┘
```

- Three card slots in a row, centered horizontally.
- Each populated card has a red **`[X]`** delete button anchored to its top-right corner.
- An empty slot shows a muted "Empty Slot" placeholder card with no delete button (nothing to delete).
- Bottom row: `Back` (returns to splash) and `Load` (loads the selected slot).
- `Load` is disabled until a populated slot is focused/selected.

## Navigation (keyboard-first, same model as Class Select)

| Input | Action |
|-------|--------|
| Left / Right | Cycle focus between the 3 cards |
| Down | Move focus from cards zone → Load button zone |
| Down (again) | Move focus from Load zone → Back button zone |
| Up | Move focus up one zone |
| S / Enter | Activate focused control (select card, press Load, press Back, press delete X) |
| D / Esc | Back to splash screen |

Focus zones (spatial, same mental model as Class Select):

- **Zone 0**: Card row (Left/Right to cycle, auto-selects)
- **Zone 1**: Load button
- **Zone 2**: Back button

## Populated Slot: The Delete Button

Each populated card has a red square **`[X]`** button:

- Positioned in the card's top-right corner (inside the card's border).
- Focusable with its own tab stop. In keyboard-nav terms, while a card is focused, pressing a dedicated key (TBD — likely **Delete** or **Shift+S**) opens the delete confirmation. Mouse users click the X directly.
- **Never triggers deletion directly.** Always opens a confirm dialog first.
- Styled with `UiTheme.Colors.Danger` background and a white X glyph.

### Delete confirmation dialog

```
┌───────────────────────────────────┐
│         DELETE CHARACTER?         │
│                                   │
│  Warrior — Level 12, Floor 24     │
│                                   │
│  This cannot be undone.           │
│                                   │
│    ┌────────┐    ┌─────────┐      │
│    │ Cancel │    │ Delete  │      │
│    └────────┘    └─────────┘      │
└───────────────────────────────────┘
```

- Modal `GameWindow` subclass.
- Title: "DELETE CHARACTER?" (gold/accent).
- Body: Short summary of the save being deleted (class, level, floor).
- Warning line: "This cannot be undone."
- Buttons: `Cancel` (left, default-focused secondary) + `Delete` (right, danger-styled).
- Pressing **Esc/D/Cancel** → closes dialog, no action.
- Pressing **Delete** → removes save file, closes dialog, refreshes Load Game screen (the slot becomes Empty).

## Flows

### Flow: Splash → Load Game

```
1. User presses "Continue" on splash (only enabled if ≥1 save exists)
2. Splash fades under the ScreenTransition overlay (not required — but consistent
   with other transitions). Optionally, Load Game screen is a sibling overlay.
3. LoadGameScreen instantiated, added to UILayer.
4. Screen scans user:// for save_0.json, save_1.json, save_2.json.
5. For each found save, deserialize and render a CharacterCard.
6. For missing slots, render an "Empty Slot" placeholder card.
7. First populated card auto-focused. If all empty, Back button focused.
```

### Flow: Load

```
1. User selects a populated card → presses Load
2. ScreenTransition.Play("Town", midpoint_callback, "Loading…")
3. Midpoint callback:
   a. SaveManager.Load(slotIndex) — reads save_{slotIndex}.json
   b. SaveSystem.RestoreState(GameState, data)
   c. LoadGameScreen.QueueFree()
   d. Splash.QueueFree() (if still present in tree)
   e. GetTree().Paused = false
   f. Main.Instance.LoadTown()
```

### Flow: Delete

```
1. User presses X on a populated card (mouse click or keyboard shortcut).
2. DeleteConfirmDialog opens as a modal (pushes to WindowStack).
3. Dialog shows summary of the save to be deleted.
4. User presses Cancel → dialog closes, no change.
5. User presses Delete →
   a. SaveManager.DeleteSlot(slotIndex) — removes user://save_{slotIndex}.json
   b. Dialog closes.
   c. LoadGameScreen.Refresh() — the card for that slot becomes Empty Slot.
   d. Focus snaps to the next populated card, or to Back if none remain.
```

### Flow: Back / Cancel

```
1. User presses Back button OR Esc/D
2. LoadGameScreen.QueueFree()
3. Splash screen is re-shown (was behind; just make visible or re-instantiate).
```

## Interaction with New Game

When the player presses **New Game** on the splash screen:

- If at least one slot is empty → proceed to Class Select normally. On confirm, the new character is saved to the first empty slot.
- If all three slots are full → show a **SlotsFullDialog** modal with headline "ALL SAVE SLOTS ARE FULL", a short explanation, and two buttons:
  - **Open Load Game** (default focus) → closes the dialog and navigates to the Load Game screen, where the player can delete a slot.
  - **Cancel** → closes the dialog and returns the player to splash.

The dialog is parented under the splash screen and queue-frees itself in both button handlers and on keyboard Cancel, so repeated blocked-New-Game clicks don't accumulate hidden dialog instances.

(Historical: an earlier pass used a transient Toast for the "slots full" case. It was replaced with the modal because a) the toast was easy to miss and made New Game look silently broken, and b) the modal lets us route the player directly to the only screen that can resolve the block.)

## Save File Layout

`user://save_0.json`, `user://save_1.json`, `user://save_2.json`.

Each file is the same format as the current single-slot save (see [save-load.md](save-load.md)). The only change is the filename includes a slot index.

`SaveManager` gains:

```csharp
public bool HasSave(int slotIndex)
public SaveData? LoadSlot(int slotIndex)
public void SaveToSlot(int slotIndex, SaveData data)
public void DeleteSlot(int slotIndex)
public int? FindFirstEmptySlot()  // or null if all full
```

`GameState` gains a `CurrentSaveSlot` property (0/1/2) so auto-saves during gameplay target the right file.

## Open Design Questions

- **Keyboard shortcut for delete** while a card is focused — Delete key? Shift+S? TBD.
- **Splash screen visibility** during Load Game — keep splash behind as a backdrop, or fade it fully out? Leaning: keep behind (smoother back-navigation).
- **Save slot ordering** on the screen — always in slot index order (0,1,2), or sort by most-recent-play? Leaning: always slot index order (stable positioning).

## Critical Files (implementation, future branch)

- `scripts/ui/LoadGameScreen.cs` — NEW
- `scripts/ui/DeleteConfirmDialog.cs` — NEW (or reuse a generic confirm dialog helper)
- `scripts/ui/CharacterCard.cs` — UPDATE to render the delete X button + focus zone
- `scripts/ui/SplashScreen.cs` — remove inline Character Card; add "Continue" button above "New Game"; disable Continue when no saves exist
- `scripts/autoloads/SaveManager.cs` — UPDATE for multi-slot methods
- `scripts/logic/SaveSystem.cs` — slot-aware file paths
- `scripts/autoloads/GameState.cs` — `CurrentSaveSlot` property
- `scripts/Main.cs` — Load Game button handler, slot-aware transitions

## Verification (for the implementation branch)

1. `make build` — compiles
2. `make test` — xUnit tests still pass
3. `make test-ui-suite SUITE=LoadGameTests` — new GoDotTest suite:
   - Load Game screen appears on button press
   - Up to 3 slots render correctly (mix of populated + empty)
   - Arrow keys cycle cards, Down moves to Load, Up returns to cards
   - Pressing Enter/S on Load loads the correct slot
   - Pressing X on a populated card opens confirm dialog
   - Confirm→Delete removes the save file and refreshes the screen
   - Confirm→Cancel does nothing
   - Back/Esc returns to splash
4. Manual: all 3 save slots independently usable; deletion is confirmed; no data crosses slots.
