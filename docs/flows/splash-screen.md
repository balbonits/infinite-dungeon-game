# Flow: Splash Screen

**Script:** `scripts/ui/SplashScreen.cs`
**Scene:** Created dynamically by `Main.ShowSplashScreen()` in `Main._Ready()`
**Process Mode:** Always (runs during pause)

## Initial State

- Game is **paused** (`GetTree().Paused = true`)
- SplashScreen added to `UILayer` CanvasLayer
- Initial focus set to first button after 0.3s delay via `UiTheme.FocusFirstButton()`

## Buttons (top to bottom)

1. **Character Card** (only if save exists) — shows saved character sprite/stats
2. **New Game** — starts fresh game
3. **Tutorial** — opens TutorialPanel overlay
4. **Settings** — opens SettingsPanel overlay
5. **Exit Game** — quits application

## Input

| Input | Action |
|-------|--------|
| Up/Down arrows | Navigate buttons (KeyboardNav) |
| S / action_cross | Press focused button |
| Mouse click | Press clicked button |

WindowStack modal check: if Settings or Tutorial is open on top, splash buttons are blocked.

## Flow: New Game

```
1. User presses "New Game" button
2. Signal NewGamePressed emitted
3. Main.cs handler:
   a. splash.Visible = false
   b. splash.QueueFree()
   c. ShowClassSelection() called
4. ClassSelect instantiated, added to UILayer
5. Game remains paused
```

## Flow: Continue (Load Save)

```
1. User clicks Character Card button
2. Signal ContinuePressed emitted
3. Main.cs handler:
   a. splash.Visible = false
   b. splash.QueueFree()
   c. SaveManager.Instance.Load() — restores all GameState from disk
   d. GetTree().Paused = false
   e. Main.LoadTown() called
```

## Flow: Exit

```
1. User presses "Exit Game"
2. GetTree().Quit() called immediately
```
