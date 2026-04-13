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

## AutoPilot Wait Pattern

```csharp
await act.WaitForTransition(); // polls IsTransitioning until false
await act.WaitSeconds(0.5);    // settle time
```
