# Input Rebinding UI

## Summary

In-game UI for players to rebind keyboard keys and gamepad buttons to in-game actions. SPEC-INPUT-REBINDING-UI-01 (Phase I / FUT-02). Depends on [SPEC-GAMEPAD-INPUT-01](../systems/gamepad-input.md) — rebinding surface covers both input sources.

## Current State

**Spec status: LOCKED** via SPEC-INPUT-REBINDING-UI-01 (Phase I). The project currently has no rebinding UI — all bindings are baked into `project.godot`'s `InputMap`. This spec adds a player-facing rebinding panel and the persistence layer.

## Design

### Panel entry point

**Pause Menu → Settings tab → "Controls" sub-panel.** Appears alongside audio, display, gameplay settings. Players don't typically rebind mid-dungeon, so the pause-menu nesting is fine.

### Panel layout (at 1280×720 design resolution)

```
┌─ Controls Settings ─────────────────────────────────────────┐
│                                                              │
│ Movement                                                     │
│   Move Up        [W] [↑]         [Add binding]  [Reset]      │
│   Move Down      [S] [↓]         [Add binding]  [Reset]      │
│   Move Left      [A] [←]         [Add binding]  [Reset]      │
│   Move Right     [D] [→]         [Add binding]  [Reset]      │
│                                                              │
│ Actions                                                      │
│   Confirm        [Enter] [S] [A·button]   [Add] [Reset]      │
│   Cancel         [Escape] [D] [B·button]  [Add] [Reset]      │
│   Pause          [Escape] [P] [Start]     [Add] [Reset]      │
│                                                              │
│ Skills                                                       │
│   Skill 1        [1] [D-pad Up]    [Add binding]  [Reset]    │
│   ... (skill 2-4)                                            │
│                                                              │
│ Innates                                                      │
│   Haste          [Shift] [RT]      [Add binding]  [Reset]    │
│   Sense          [C] [LT]          [Add binding]  [Reset]    │
│   Fortify        [V] [RB]          [Add binding]  [Reset]    │
│                                                              │
│ [ Reset all to defaults ]   [ Done ]                         │
└──────────────────────────────────────────────────────────────┘
```

**Rows are grouped into sections** (Movement, Actions, Skills, Innates) with headings. Each row shows:

- **Action name** (left column).
- **Current bindings list** — every currently-mapped key/button/axis, comma-separated. Keyboard keys show as `[W]`, gamepad buttons as `[A·button]`, axes as `[LStick↑]` etc.
- **"Add binding" button** — starts a capture flow (described below).
- **"Reset" button** — restores this action's default bindings.

At the bottom: **"Reset all to defaults"** and **"Done"** (saves + closes).

### Add-binding flow

1. Player clicks "Add binding" on a row.
2. UI enters **capture mode**: a modal overlay shows "Press any key or button for <Action Name>. Press [Escape] to cancel."
3. Player presses any key or gamepad button or stick-axis-direction.
4. System validates the input (see below), then either:
   - **Adds it to the action's binding list** and returns to the settings panel.
   - **Rejects with a reason** (e.g., conflict) and stays in capture mode.
5. Player can press Escape at any time to cancel without adding.

### Remove-binding flow

- Each binding chip is clickable. Clicking surfaces a small context menu: "Remove this binding / Cancel."
- Removing a binding decrements the row's list. If the row has only one binding remaining, removing it prompts: "This will unbind <Action Name>. Continue?" — to prevent the player from accidentally making an action unreachable.

### Conflict handling

If a new binding would collide with an existing mapping on a DIFFERENT action:

> *"[E] is currently bound to Menu Tab Right. Reassign it to <New Action>?"*

Options: Reassign (removes it from the old action and assigns to new) / Cancel.

If a new binding is already on the SAME action (duplicate):
- Silently ignore — no need to add the same binding twice.

**Reserved binding** (cannot be bound to any action):
- `Escape` for keyboard — always cancel. Users attempting to bind Escape see a notice: "Escape is reserved for cancel and cannot be rebound."
- Similarly, gamepad Start on some platforms is OS-reserved (Steam overlay). Warn but allow — player may know what they're doing.

### Reset

- **Row Reset:** restores that action's defaults per `project.godot` InputMap originals.
- **Reset all to defaults:** restores every action. Confirmation dialog: "Reset all bindings to defaults?" with Yes/Cancel.

### Save / persistence

- Rebinds save to `user://input_bindings.cfg` (a Godot `ConfigFile`).
- Loaded at game start; overrides the `project.godot` defaults on load.
- If the config file is missing or corrupt, fallback to `project.godot` defaults and save a fresh config.
- Save triggers on "Done" button; player can preview-rebind and close with Escape to discard (so half-made changes don't overwrite the config).

### Persistence format

```ini
[actions]

move_up = ["W", "Up", "Joy0/DPadUp"]
move_down = ["S", "Down", "Joy0/DPadDown"]
action_cross = ["Enter", "S", "Joy0/Button0"]
...
```

Each action is an array of input-source strings. Gamepad bindings use `Joy{deviceIndex}/{buttonOrAxis}` format for clarity in the save file.

### Accessibility — button-label rendering

The UI renders gamepad button labels using the current controller's convention (A/B/X/Y on Xbox, ×/○/□/△ on PS, etc.). Godot's `InputEvent.AsText()` handles the substitution automatically when given the current connected gamepad type. If the player's current controller type doesn't match the label (e.g., connected Xbox controller but binding was saved on PS controller), render the stored-format label with a neutral fallback symbol.

### UI navigation (keyboard-only path)

The rebinding panel must be operable entirely from keyboard (no mouse required):
- Tab or Arrow keys move between rows.
- Enter / `action_cross` activates a row's "Add binding" button.
- The capture-mode overlay gets keyboard focus; arrows and Escape work as expected.
- Focus ring follows the [UiTheme.PanelBorderBright](../ui/font.md) convention for visibility.

---

## Acceptance Criteria

- [ ] Controls sub-panel accessible from Pause → Settings → Controls.
- [ ] Every action listed in [movement.md](../systems/movement.md), [gamepad-input.md](../systems/gamepad-input.md), and [hud-layout.md](hud-layout.md) hotkey table appears in the rebinding UI.
- [ ] Add-binding capture flow works for keyboard keys, gamepad buttons, and stick-axis directions.
- [ ] Conflict detection prompts reassign-or-cancel for cross-action collisions.
- [ ] Escape key cannot be bound to any action (reserved for cancel).
- [ ] Row Reset + Reset-all-to-defaults both restore project.godot defaults.
- [ ] Save persists to `user://input_bindings.cfg`; load on game start overrides project defaults.
- [ ] Escape during preview-rebind discards changes (only "Done" saves).
- [ ] Controller-type-appropriate button labels render in the UI.
- [ ] Panel is fully keyboard-navigable (no mouse required).

## Implementation Notes

- **New UI scene:** `scenes/ui/controls_settings.tscn`, hosted inside the existing Settings panel via a tab or sub-section.
- **Binding-state manager:** new autoload `InputBindings` — loads the `user://input_bindings.cfg` at game start, replaces `InputMap` entries with the saved bindings, exposes `SetBinding(actionName, inputEvent)` / `RemoveBinding` / `ResetAction` / `ResetAll` / `Save`.
- **Capture-mode modal:** new `BindingCaptureDialog` — temporary modal that listens to the next `_UnhandledInput` event and reports back. Must be a WindowStack-compatible modal so it can be Escaped.
- **Save file format:** Godot `ConfigFile` with actions as keys. Survives serialization round-trip.
- **Default restore:** cache the project.godot defaults at `InputBindings` init time so Reset can replay them without re-reading the file.
- **Event equality:** comparing `InputEventKey` or `InputEventJoypadButton` instances requires checking `keycode`, `device`, `button_index`, etc. — use a canonical string representation for hashing in the config file.

## Open Questions

None — spec is locked.
