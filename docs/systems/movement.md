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

- Should there be acceleration/deceleration (easing into movement) or is instant full-speed acceptable?
- Should controller (gamepad) input be supported? Godot's Input Map can map joystick axes to the same actions.
- Should pointer/click-to-move be added for touch/mobile support?
