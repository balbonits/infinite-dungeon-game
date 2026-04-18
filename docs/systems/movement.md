# Movement System

## Summary

The player moves in 8 directions using keyboard input. Movement is screen-space (up = up on screen, no isometric transform). Input uses `Input.GetVector()` with `move_left/right/up/down` actions. Movement uses Godot's CharacterBody2D with `MoveAndSlide()` for built-in wall collision and sliding. Directional sprites update based on velocity to show the player facing the direction of travel.

## Current State

Keyboard-only screen-space movement is implemented in `scripts/Player.cs`. The isometric transform matrix that was present in earlier versions has been removed -- movement is direct screen-space. The `DirectionalSprite` utility (`scripts/DirectionalSprite.cs`) handles 8-way sprite rotation based on velocity. Speed is `MoveSpeed = 190.0f` from `Constants.PlayerStats`. Dead/game-over state stops all movement.

## Design

### Input Reading

| Property | Value |
|----------|-------|
| Method | `Input.GetVector(Constants.InputActions.MoveLeft, Constants.InputActions.MoveRight, Constants.InputActions.MoveUp, Constants.InputActions.MoveDown)` |
| Return type | `Vector2` with x in [-1, 1] and y in [-1, 1] |
| Diagonal normalization | Automatic (GetVector normalizes the result when both axes are active) |
| Dead zone | 0.0 (keyboard is binary; dead zone applies to joystick input if added later) |

**Input Map Configuration (project.godot):**

| Action Name | Primary Key | Description |
|-------------|-------------|-------------|
| `move_up` | Up Arrow / W | Move toward screen top |
| `move_down` | Down Arrow / S | Move toward screen bottom |
| `move_left` | Left Arrow / A | Move toward screen left |
| `move_right` | Right Arrow / D | Move toward screen right |

### Movement Algorithm

Step-by-step, executed every physics frame via `HandleMovement()` (called from `_PhysicsProcess`):

```csharp
private void HandleMovement()
{
    // Step 1: Read raw input as a screen-space direction vector
    Vector2 inputDir = Input.GetVector(
        Constants.InputActions.MoveLeft,
        Constants.InputActions.MoveRight,
        Constants.InputActions.MoveUp,
        Constants.InputActions.MoveDown
    );

    // Step 2: If moving, normalize and scale to movement speed + update sprite
    if (inputDir.Length() > 0)
    {
        Velocity = inputDir.Normalized() * Constants.PlayerStats.MoveSpeed;
        DirectionalSprite.UpdateSprite(_sprite, Velocity, _rotations, ref _lastDirection);
    }
    else
    {
        Velocity = Vector2.Zero;
    }

    // Step 3: Apply movement with collision resolution
    MoveAndSlide();
}
```

**Key details:**
- No isometric transform. Input direction maps directly to screen movement.
- `MoveAndSlide()` uses the `Velocity` property of CharacterBody2D directly (no velocity parameter in Godot 4).
- `MoveAndSlide()` handles delta time internally -- do not multiply velocity by delta.
- When no keys are pressed, `Velocity` is `Vector2.Zero` and the player stands still.
- If `GameState.Instance.IsDead` is true, `_PhysicsProcess` sets `Velocity = Vector2.Zero` and returns early before `HandleMovement()` is called.

### Directional Sprites

The `DirectionalSprite` utility provides 8-way sprite rotation:

| Component | Details |
|-----------|---------|
| Utility class | `DirectionalSprite` (static, `scripts/DirectionalSprite.cs`) |
| Direction count | 8: south, south-west, west, north-west, north, north-east, east, south-east |
| Direction snapping | Velocity angle divided into 45-degree sectors via `Atan2` |
| Idle behavior | Keeps last facing direction when velocity is near zero |
| Texture loading | `LoadRotations(basePath)` loads `{basePath}/{direction}.png` for all 8 directions |

Sprites are loaded per-class on `_Ready()`:
```csharp
int classIndex = (int)GameState.Instance.SelectedClass;
string rotationsPath = Constants.Assets.PlayerClassRotations[classIndex];
_rotations = DirectionalSprite.LoadRotations(rotationsPath);
```

### Movement Speed

| Property | Value | Source |
|----------|-------|--------|
| MoveSpeed | 190.0f | `Constants.PlayerStats.MoveSpeed` |

The velocity is set explicitly each frame and naturally clamped to `MoveSpeed` after normalization. No additional max velocity capping is needed.

### Acceleration (SPEC-MOVEMENT-ACCEL-01)

**Movement is instant — no acceleration curve, no momentum, no ramp on start or stop.** Press direction → full speed on the next physics frame; release direction → stop on the next physics frame.

**Why instant:**
- **Diablo 1 reference:** the art pivot is cartoonish true-iso Diablo 1 style. Diablo uses instant movement; easing would fight the reference.
- **Precision dodging:** boss telegraphs (e.g., the 900 ms Bone Overlord ground-slam per [SPEC-BOSS-BONE-OVERLORD-01](../world/bosses/bone-overlord.md)) depend on the player being able to step out on the exact frame. Easing would introduce a reaction-time penalty.
- **Keyboard-first expectation:** the game is keyboard-first; keyboard movement feels best with 100% responsive state mapping.

**Interaction with other movement modifiers:**
- **Haste Innate** (hold to sprint per [magic.md §Innate Skills](magic.md)): speed multiplier applies instantly on press, removes instantly on release. No sprint-charge-up.
- **Slow zones** (e.g., Chitin Matriarch's ground-web per [SPEC-BOSS-CHITIN-MATRIARCH-01](../world/bosses/chitin-matriarch.md)): multiplier applies on the first frame inside the zone; removes on the first frame outside. No gradual transition.

**Rejected alternatives:** light ease (~80 ms) and full ease (~200 ms) were both considered and rejected. Even 80 ms breaks precision-dodge feel; 200 ms is incompatible with the genre reference.

**Guardrail:** if a future PR adds `Lerp` or `MoveToward` to the `Velocity` assignment path in `Player.HandleMovement()`, block it in review citing this spec as the reason.

### Wall Collision

| Property | Detail |
|----------|--------|
| Player body type | CharacterBody2D |
| Collision method | `MoveAndSlide()` |
| Wall physics | TileMapLayer collision polygons (rectangle: `WallCollisionPolygon` in Constants) |
| Player collision_layer | 2 (`Constants.Layers.Player`) |
| Player collision_mask | 1 (`Constants.Layers.Walls`) |
| Slide behavior | Player slides along walls at reduced speed rather than stopping completely |

**How MoveAndSlide() resolves collisions:**
1. Applies velocity as a motion vector for the frame.
2. If the motion would cause overlap with a physics body, computes the collision normal.
3. Subtracts the component of velocity along the collision normal (the "into the wall" component).
4. Applies the remaining velocity (the "along the wall" component) as a slide.
5. Repeats for up to `MaxSlides` iterations (default 6) to handle corners.
6. Updates the `Velocity` property with the post-slide velocity.

### Grace Period

On spawn and floor descent, the player gets a brief invincibility window:

| Property | Value | Source |
|----------|-------|--------|
| Duration | 1.5s | `Constants.PlayerStats.GracePeriod` |
| Visual | Sprite flickers between full alpha and 0.4 alpha at 10 Hz | `Constants.PlayerStats.GraceFlickerAlpha` |
| Trigger | `StartGracePeriod()` called on `_Ready()` and after `PerformFloorDescent()` |

## Implementation Notes

- Movement logic lives in `Player.HandleMovement()`, called from `_PhysicsProcess()`.
- `_PhysicsProcess()` is used (not `_Process()`) because movement involves physics collision via `MoveAndSlide()`.
- The default physics tick rate is 60 Hz. Movement at 190 px/s means ~3.17 pixels per physics frame.
- Enemy movement uses direct `MoveAndSlide()` toward the player position, not screen-space input.

## Open Questions

- ~~Should there be acceleration/deceleration (easing into movement) or is instant full-speed acceptable?~~ **Resolved 2026-04-18 by SPEC-MOVEMENT-ACCEL-01 — instant, no easing.** See §Acceleration above.
- ~~Should controller (gamepad) input be supported?~~ **Resolved 2026-04-18 by [SPEC-GAMEPAD-INPUT-01](gamepad-input.md) — yes, twin-stick convention.**
- Should pointer/click-to-move be added for touch/mobile support? (No current ticket; P3 future.)
