# Flow: Class Selection

**Script:** `scripts/ui/ClassSelect.cs`
**Scene:** Created dynamically by `Main.ShowClassSelection()`
**Process Mode:** Always (runs during pause)

## Initial State

- Game is **paused** (`GetTree().Paused = true`)
- 3 class cards displayed horizontally (Warrior, Ranger, Mage)
- Confirm button below cards (starts **disabled**)
- Back button below confirm
- Focus zone 0 (cards), focus index -1 (nothing selected)

## Focus Zone System

| Zone | Value | Contains | GrabFocus target |
|------|-------|----------|-----------------|
| Cards | 0 | 3 class cards | Card panel highlight |
| Confirm | 1 | Confirm button | `_confirmButton.GrabFocus()` |
| Back | 2 | Back button | `_backButton.GrabFocus()` |

## Input

| Input | Zone 0 (Cards) | Zone 1 (Confirm) | Zone 2 (Back) |
|-------|----------------|-------------------|---------------|
| Left/Right | Navigate cards + auto-select | — | — |
| Down | Move to zone 1 | Move to zone 2 | — |
| Up | — | Move to zone 0 | Move to zone 1 |
| S / action_cross | Select focused card | Call `OnConfirmPressed()` | Go back |
| D / Escape | — | — | Go back (reload scene) |

**Key behavior:** Left/Right navigation **auto-selects** the card (calls `OnCardClicked()`), which enables the Confirm button.

## Card Styles

| State | Style |
|-------|-------|
| Default | BgColor 0.85 alpha, standard border |
| Hover (focused, not selected) | BgColor 0.92 alpha, bright border |
| Selected | BgColor 0.95 alpha, accent border |

## Flow: Select and Confirm Warrior

```
1. Screen opens at zone 0, no card selected, Confirm disabled
2. Press S (action_cross) — selects card at current focus index
   → OnCardClicked() called
   → Card gets SelectedCardStyle
   → _confirmButton.Disabled = false
   → _selectedClass = PlayerClass.Warrior
3. Press Down — moves to zone 1 (Confirm button)
   → _focusZone = 1
   → _confirmButton.GrabFocus()
4. Press S (action_cross) — triggers OnConfirmPressed()
```

## OnConfirmPressed() — Transition

```
1. Guard: _selectedCard must be non-null
2. GameState.Instance.SelectedClass = _selectedClass
3. GameState.Instance.Reset() — initializes HP, Mana, Stats, Skills, etc.
4. Tween: modulate:a → 0.0 over 0.4 seconds (fade out)
5. Tween callback (fires after 0.4s):
   a. Visible = false
   b. Modulate = Colors.White (reset)
   c. GetTree().Paused = false
   d. Scenes.Main.Instance.LoadTown()
6. LoadTown() triggers ScreenTransition (~2.1s total)
7. Town scene loads during transition hold phase
```

**Total time from confirm press to town loaded:** ~2.5 seconds (0.4s tween + 2.1s screen transition)

## AutoPilot Input Sequence

```csharp
// 1. Select first card (Warrior)
act.Press("action_cross");
await act.WaitSeconds(0.3);

// 2. Navigate to Confirm zone
act.Press("move_down");
await act.WaitSeconds(0.2);

// 3. Press Confirm
act.Press("action_cross");
await act.WaitSeconds(0.5);

// 4. Wait for tween + transition
await act.WaitForTransition();
await act.WaitSeconds(1.0);
```
