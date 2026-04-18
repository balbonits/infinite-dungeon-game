# Gamepad Input

## Summary

Gamepad (controller) support for all gameplay actions, mapped alongside the existing keyboard bindings via Godot's `InputMap`. SPEC-GAMEPAD-INPUT-01 (Phase I / FUT-01). Depends on [SPEC-MOVEMENT-ACCEL-01](movement.md) for the instant-movement contract — the gamepad spec inherits that, so left-stick movement has no separate easing.

## Current State

**Spec status: LOCKED** via SPEC-GAMEPAD-INPUT-01 (Phase I). The game's current input handling is keyboard-only; `Input.GetVector` and `Input.IsActionPressed` are already the abstractions used, so adding gamepad is a `project.godot` InputMap edit plus a deadzone-setting edit — no gameplay code changes.

## Design

### Target controller layout

**Primary target:** Xbox-style and PlayStation-style gamepads (Xbox One / PS4 / PS5 / Series X|S / Nintendo Pro Controller). Godot auto-detects controller type via SDL GameControllerDB. Button labels adjust automatically in-engine (A/X swap between Xbox/PS).

**Bindings** — each in-game action gets a gamepad binding alongside its keyboard binding:

| Action | Keyboard | Gamepad |
|--------|----------|---------|
| `move_left` / `move_right` / `move_up` / `move_down` | Arrows / WASD | Left stick (x/y axes with deadzone) |
| `action_cross` (confirm, press service button, attack) | S / Enter / Space | South button (A on Xbox, X on PS) |
| `action_circle` (cancel, close modal) | D / Escape | East button (B on Xbox, O on PS) |
| `action_square` (context action) | Q | West button (X on Xbox, □ on PS) |
| `action_triangle` (secondary context) | E | North button (Y on Xbox, △ on PS) |
| `skill_1` / `2` / `3` / `4` | 1 / 2 / 3 / 4 | D-pad up / left / right / down |
| `open_pause` | Esc / P | Start / Options |
| `open_inventory` | I | Select / Share |
| `stats_peek` (hold-to-peek per [hud-layout.md](../ui/hud-layout.md)) | Tab (hold) | Left bumper (hold) |
| `haste_toggle` | Shift (hold) | Right trigger (hold) |
| `sense_toggle` | C | Left trigger (tap) |
| `fortify_toggle` | V | Right bumper (tap) |
| `tab_cycle_left` / `_right` (service-menu tab Q/E) | Q / E | Left bumper / right bumper (in menu context) |

**Left-bumper conflict note:** Left bumper is `stats_peek` in gameplay context and `tab_cycle_left` in menu context. These contexts don't overlap (Stats-peek only fires when Hud is active; tab-cycle only fires when a service menu is open), so both bindings coexist without collision. Dispatch happens via the existing `WindowStack.BlockIfNotTopmost` gate.

**Thumb-stick dual binding:** both left-stick and d-pad bind to movement. Left-stick is the natural choice; d-pad is a precision fallback and also the natural choice on D-pad-style controllers.

### Deadzone

Left-stick deadzone: **0.25** (Godot default is 0.5; 0.25 is more responsive without drifting). Applies to both movement axes.

Right-stick deadzone: N/A — no right-stick binding in this spec. The game is keyboard-first; right-stick free to remain unassigned.

Right-stick reserved for future: camera pan / cursor-aim if a future spec adds mouse-cursor-style aiming. Out of scope for SPEC-GAMEPAD-INPUT-01.

### UI navigation

Godot's focus-navigation system (already used by `KeyboardNav`) works with gamepad out-of-the-box once D-pad + left-stick are bound to movement and `action_cross` is bound to the south button. No special UI-gamepad code needed — the existing Focus path handles it.

### Disconnection / reconnection

- **Disconnect while playing:** game pauses automatically (existing Pause Menu logic can extend to detect `Input.JoyConnectionChanged`). Display a message: "Controller disconnected. Reconnect to continue." Resume on reconnection.
- **Multi-controller:** single-player only. First detected controller is "the controller." If a second controller connects, its input is ignored (no couch-coop in scope).

### Rumble

- **No rumble in this spec.** Godot supports `Input.StartJoyVibration` but rumble is a feel mechanic like camera-shake and hitstop — deserves its own spec if introduced. Keep the controller-input spec focused on mapping + deadzone.

### Vibrating haptics on PS5 DualSense

- **Out of scope.** Adaptive triggers and advanced haptics are PS5-specific and require Godot platform work beyond InputMap. Defer until there's a specific need.

### Accessibility — "swap confirm and cancel"

Some players (especially Japan-locale players) expect east-button as confirm and south-button as cancel (opposite of the US/Europe convention). An Options-menu toggle "Swap confirm / cancel buttons" remaps `action_cross` and `action_circle` to their opposite buttons. Default off.

---

## Acceptance Criteria

- [ ] All bindings in the table above work via `InputMap` — `Input.IsActionPressed("action_cross")` returns true on either keyboard or gamepad press.
- [ ] Left-stick movement respects instant-movement contract (no stick-drift easing).
- [ ] Deadzone 0.25 applies to left-stick axes.
- [ ] Stats-peek hold works on left-bumper during gameplay.
- [ ] Service-menu tab-cycling works on left/right bumper during menu interactions.
- [ ] Skill-bar slots 1-4 bind to D-pad up/left/right/down.
- [ ] Start/Options button opens Pause Menu; Select/Share button opens Inventory.
- [ ] Controller disconnect pauses the game; reconnection resumes.
- [ ] Options-menu toggle for confirm/cancel swap works.
- [ ] Multi-controller setup ignores the second controller's input.

## Implementation Notes

- **`project.godot` InputMap additions:** add a gamepad binding to each existing action. Godot handles OS-level button remapping for PS vs Xbox labels automatically via `InputEvent.AsText()` when rendering tooltips/rebind UI. See [SPEC-INPUT-REBINDING-UI-01](../ui/input-rebinding.md) for how labels surface.
- **Deadzone:** `project.godot` action-level deadzone attribute; set to 0.25 for the four `move_*` actions.
- **JoyConnectionChanged signal:** subscribe in an autoload (`InputState` or similar). Triggers pause on disconnect.
- **No gameplay code changes** — the `Input.GetVector` / `Input.IsActionPressed` calls already in `Player.cs`, `NpcPanel.cs`, etc. pick up the gamepad bindings automatically once `project.godot` adds them.
- **Testing:** Godot's input simulation in tests is keyboard-only by default. Gamepad integration tests should be manual until Godot adds scripted `Input.JoyButton` events or we add a test harness for them.

## Open Questions

None — spec is locked.
