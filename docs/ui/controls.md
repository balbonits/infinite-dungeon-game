# Controls

## Summary

The game supports keyboard and gamepad input for movement. Combat is fully automatic (auto-targets nearest enemy in range). Mouse/touch movement is deferred for the Godot version. All input is mapped through Godot's Input Map system for unified handling.

## Current State

Migrating from Phaser 3 prototype to Godot 4:
- **Keyboard:** WASD and arrow keys for movement (both mapped to the same Input Map actions)
- **Combat:** fully automatic (no input required -- auto-targets nearest enemy)
- **Death restart:** R key via `_UnhandledInput(InputEvent)`, or click Restart button
- **Mouse/touch movement:** deferred (was click/tap-to-move in Phaser prototype)
- **Gamepad:** deferred but trivial to add via Input Map

## Design

### Keyboard Input

All keyboard input is handled through Godot's **Input Map** system (Project > Project Settings > Input Map). Named actions are defined once; any number of physical keys can be bound to each action.

#### Input Map Actions

| Action Name | Primary Key | Secondary Key | Purpose |
|-------------|-------------|---------------|---------|
| `move_up` | W | Up Arrow | Move player upward (screen space) |
| `move_down` | S | Down Arrow | Move player downward (screen space) |
| `move_left` | A | Left Arrow | Move player leftward (screen space) |
| `move_right` | D | Right Arrow | Move player rightward (screen space) |

No separate handling for WASD vs. arrow keys is needed. The Input Map unifies both key sets into the same actions.

#### Reading Movement Input

Movement direction is read as a single normalized vector each physics frame:

```csharp
Vector2 rawInput = Input.GetVector("move_left", "move_right", "move_up", "move_down");
```

This returns a `Vector2` where:
- `x` ranges from -1 (left) to +1 (right)
- `y` ranges from -1 (up) to +1 (down)
- Diagonals are automatically normalized (magnitude <= 1.0), so diagonal movement is never faster than cardinal movement
- Returns `Vector2.ZERO` when no keys are pressed

#### Death Screen Input

The death/restart screen uses `_UnhandledInput(InputEvent)` rather than polling in `_PhysicsProcess`:

```csharp
public override void _UnhandledInput(InputEvent @event)
{
    if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.R)
    {
        RestartGame();
    }
}
```

This ensures:
- R key only triggers restart when no other UI element has consumed the input
- Works regardless of focus state
- Does not conflict with any other input handling

### Isometric Input Mapping

Raw keyboard input is in **screen space** (up/down/left/right as seen on the monitor). The game world uses an isometric projection, so screen-space input must be transformed to isometric world space before being applied to the player's velocity.

#### Screen-to-Isometric Transform

A `Transform2D` matrix converts screen-space directions to isometric world directions:

```csharp
var isoTransform = new Transform2D(new Vector2(1, 0.5f), new Vector2(-1, 0.5f), Vector2.Zero);
Vector2 worldDirection = (isoTransform * rawInput).Normalized();
Velocity = worldDirection * MoveSpeed;
```

#### Directional Mapping (what the player sees)

| Key Pressed | Screen Direction | Isometric World Direction | Visual Result |
|-------------|------------------|---------------------------|---------------|
| W (or Up Arrow) | Screen up | Northeast | Player moves up-right |
| S (or Down Arrow) | Screen down | Southwest | Player moves down-left |
| A (or Left Arrow) | Screen left | Northwest | Player moves up-left |
| D (or Right Arrow) | Screen right | Southeast | Player moves down-right |
| W + D | Screen up-right | East | Player moves pure right |
| W + A | Screen up-left | North | Player moves pure up |
| S + D | Screen down-right | South | Player moves pure down |
| S + A | Screen down-left | West | Player moves pure left |

See `docs/systems/movement.md` for the full transform explanation and math derivation.

### Mouse / Touch (Deferred)

Mouse/touch pointer-to-move is **deferred** for the Godot version.

In the Phaser prototype, click/tap-and-hold moved the player toward the pointer position with a 12px dead zone. Implementing this in Godot requires:
- Screen-to-isometric coordinate conversion (pointer position is in screen space)
- `GetGlobalMousePosition()` returns world-space coordinates, but the isometric transform must be accounted for
- Dead zone logic to prevent jitter when the pointer is near the player

This will be added as a separate feature after core keyboard movement is solid.

### Gamepad (Deferred)

Godot has native gamepad support through the same Input Map system used for keyboard.

#### Adding Gamepad Support

1. Open Project > Project Settings > Input Map
2. For each `move_*` action, click "+" and add a Joypad Axis event:
   - `move_left`: Left stick left (axis 0, negative)
   - `move_right`: Left stick right (axis 0, positive)
   - `move_up`: Left stick up (axis 1, negative)
   - `move_down`: Left stick down (axis 1, positive)
3. No code changes needed -- `Input.GetVector()` automatically reads joypad axes

#### Planned Gamepad Mapping

| Input | Action |
|-------|--------|
| Left stick | Movement (via Input Map actions) |
| A button (Xbox) / Cross (PlayStation) | Future: manual attack |
| Start / Options | Future: pause menu |

Godot auto-detects gamepad connection and disconnection. No setup code is needed. The `Input.GetVector()` call already used for keyboard will seamlessly read gamepad axes once the Input Map entries are added.

### Virtual Joystick (Deferred)

For mobile/touch devices, a virtual joystick overlay is planned:
- Thumb-controlled movement joystick (left side of screen)
- Large attack button (right side of screen)
- Auto-detect mobile via touch capability
- Implementation: custom `TouchScreenButton` nodes or a lightweight joystick addon

### Input Priority (Godot)

In the current implementation, there are no priority conflicts because only keyboard input is active:

1. **Keyboard / Gamepad:** Both handled simultaneously via Input Map. If both are providing input, their values combine (this is Godot's default behavior with `Input.GetVector()`). Since the result is normalized, combined input never exceeds speed 1.0.
2. **Mouse / Touch:** Deferred. When implemented, pointer movement will likely override keyboard (matching the Phaser prototype behavior where pointer-down overrode WASD).
3. **UI Input:** Death screen restart (R key) is handled via `_UnhandledInput()`, which only fires if no UI Control node consumed the event first. This prevents accidental restarts while interacting with future UI elements.

### Comparison to Phaser Prototype

| Aspect | Phaser 3 | Godot 4 |
|--------|----------|---------|
| Key binding | `createCursorKeys()` + `addKeys("W,A,S,D")` | Input Map (project settings) |
| Reading input | Poll `isDown` each frame | `Input.GetVector()` returns normalized direction |
| Normalization | Manual `Vector2.normalize().scale(190)` | `GetVector()` auto-normalizes; multiply by speed |
| Pointer move | `pointer.worldX/Y` relative to player | Deferred |
| Gamepad | Not implemented | Native via Input Map (deferred) |
| Death restart | `keyboard.once("keydown-R")` | `_UnhandledInput()` checking `Key.R` |
| Isometric transform | Not needed (top-down) | `Transform2D` matrix conversion required |

## Implementation Notes

- Input Map actions should be set up in `project.godot` or via Project Settings before any movement code runs
- The `Input.GetVector()` call handles dead zones for analog stick input automatically (default dead zone: 0.5)
- Adjust dead zone per-action in Input Map settings if gamepad feels unresponsive
- Movement speed (190 px/s) is applied after normalization: `Velocity = worldDirection * 190.0f`
- Player movement code lives in `_PhysicsProcess(double delta)` for deterministic physics behavior

## Open Questions

- Should the virtual joystick be always visible or appear on touch?
- How should inventory and menus be navigated on mobile?
- Should there be customizable key bindings (rebinding UI)?
- How does gamepad input map to menu navigation?
- Should pointer-to-move use the isometric grid or raw world position?
- What dead zone value feels best for gamepad analog sticks?
