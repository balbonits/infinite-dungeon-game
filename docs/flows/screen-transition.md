# Flow: Screen Transition

**Script:** `scripts/ui/ScreenTransition.cs`
**Access:** `ScreenTransition.Instance` (singleton in UILayer)
**Check state:** `ScreenTransition.Instance.IsTransitioning`

## Timing

All transitions use the same animation sequence:

| Phase | Duration | What happens |
|-------|----------|-------------|
| 1. Fade out | 0.3s | Overlay alpha 0 → 1.0 (black) |
| 2. Show text | 0.15s | Message label fades in |
| 3. Show subtext | 0.1s | Submessage label fades in (overlapped with text) |
| 4. Execute callback | instant | Scene swap, floor generation, etc. + forced GC |
| 5. Hold | 0.6s | Screen stays black with text visible |
| 6. Fade text | 0.25s | Both labels alpha → 0 |
| 7. Fade in | 0.4s | Overlay alpha → 0 (reveal scene) |
| 8. Cleanup | instant | `_isTransitioning = false` |

**Total duration:** ~2.1 seconds

## API

```csharp
ScreenTransition.Instance.Play(
    string message,       // e.g., "Floor 1"
    Action onMidpoint,    // callback during black screen (scene swap)
    string submessage     // e.g., "Entering Dungeon"
);
```

## Usage in Game

| Trigger | Message | Callback |
|---------|---------|----------|
| Town → Dungeon | `Strings.Floor.FloorNumber(n)` | `Main.DoLoadDungeon()` |
| Dungeon → Town | `Strings.Town.Title` | `Main.DoLoadTown()` |
| Floor descent | `Strings.Floor.FloorNumber(n+1)` | `Dungeon.PerformFloorDescent()` |
| Floor 1 stairs up | `"Dungeon Entrance"` | `Main.LoadTown()` |
| ClassSelect → Town | `Strings.Town.Title` | hide ClassSelect + `LoadTown()` |
| Continue (from splash) → Town | `Strings.Town.Title` | hide splash + load save + `LoadTown()` |
| DeathScreen respawn | `Strings.Town.Title` | hide DeathScreen + `LoadTown()` |
| NPC dialog → Shop/etc. | n/a — direct modal, no scene swap | n/a |

## Critical Invariant: No Flash of New Content

**When a caller uses `Play()` to swap worlds, the overlay MUST be fully opaque before the new world is instantiated.**

The current content (whatever screen the player is on) must remain visible during the fade-to-black phase so the overlay has something to fade *over*. Any `Close()`, `Visible = false`, or `QueueFree()` on the source screen must happen **inside the midpoint callback**, never before `Play()` starts.

✅ Correct pattern (matches `Dungeon.OnStairsUpEntered`, all fixed callers):
```csharp
// Dungeon is still visible on-screen; the overlay fades over it.
ScreenTransition.Instance.Play(
    "Town",
    () =>
    {
        // Midpoint — overlay is at alpha = 1.0 now, safe to swap.
        Close();
        Scenes.Main.Instance.LoadTown();
    },
    "Returning to town");
```

❌ Wrong pattern (causes a flash of the new world):
```csharp
// Dialog closes → viewport is empty → new world renders during fade-to-black.
Close();
ScreenTransition.Instance.Play(
    "Town",
    () => Scenes.Main.Instance.LoadTown(),
    "Returning to town");
```

This invariant is verified by `TransitionTests.ClassSelect_ConfirmLoadsTownWithOpaqueOverlay` which asserts `OverlayAlpha >= 0.99` at the moment `Town` is added to the scene tree.

## Test API

- `ScreenTransition.Instance.OverlayAlpha` — current alpha of the black overlay (0 transparent, 1 opaque). Used by `TransitionTests` to sample mid-transition state.
- `ScreenTransition.Instance.IsTransitioning` — true while a transition is in progress.
