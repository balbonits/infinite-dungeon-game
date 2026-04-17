# Flow: Splash Screen

**Script:** `scripts/ui/SplashScreen.cs`
**Scene:** Created dynamically by `Main.ShowSplashScreen()` in `Main._Ready()`
**Process Mode:** Always (runs during pause)

## Initial State

- Game is **paused** (`GetTree().Paused = true`)
- SplashScreen added to `UILayer` CanvasLayer
- Initial focus set to first button after 0.3s delay via `UiTheme.FocusFirstButton()`

## Buttons (top to bottom)

1. **Continue** — opens the [Load Game screen](load-game.md). Greyed out / disabled when no save files exist. Replaces the old inline Character Card UI.
2. **New Game** — starts fresh game (opens Class Select). If all 3 save slots are full, shows a Toast error: "All save slots are full. Delete a character from Continue first."
3. **Tutorial** — opens TutorialPanel overlay
4. **Settings** — opens SettingsPanel overlay
5. **Exit Game** — quits application

**Background:** Atmospheric dungeon-themed image at `assets/ui/splash_background.png` (see [load-game.md](load-game.md) — generated via PixelLab). The title text and buttons overlay on top with enough contrast via a subtle dark tint layer.

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

## Flow: Continue (opens Load Game screen)

```
1. User presses "Continue" button (only enabled when ≥1 save exists)
2. Signal ContinuePressed emitted
3. Main.cs handler:
   a. splash remains visible/present in tree (serves as backdrop)
   b. LoadGameScreen instantiated, added to UILayer as an overlay
4. Player picks a slot on the Load Game screen and presses Load.
5. See docs/flows/load-game.md for the full load flow.
```

## Flow: Exit

```
1. User presses "Exit Game"
2. GetTree().Quit() called immediately
```
