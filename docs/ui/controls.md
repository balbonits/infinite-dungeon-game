# Controls

## Summary

PS1 DualShock controller as the MVP baseline. Arrow keys for movement, face buttons for combat actions, L1/R1 for target cycling and shortcut modifiers. Designed so the game works with ~12 inputs, then scales cleanly to modern controllers and full keyboards.

**Core philosophy:** "Dumb hack n slash" -- mash buttons to attack, bump shoulders to switch targets. Zero learning curve for new players.

## Current State

**Spec status: LOCKED.** Implementation status: partially implemented (see "What's Implemented Now" below).

---

## What's Implemented Now

These controls are functional in the current build. Everything else in this doc is design spec for future implementation.

### Movement (Implemented)

| Input | Keyboard | Action |
|-------|----------|--------|
| D-pad / Arrow keys | Up/Down/Left/Right Arrow | 8-directional **screen-space** movement (190 px/s) |

Movement uses Godot's Input Map system. Arrow keys map to `move_up`, `move_down`, `move_left`, `move_right` actions.

**Screen-space movement (NOT isometric transform).** Arrow keys move the player directly in screen directions. The isometric transform matrix from the original spec is NOT used in the current implementation. `Player.cs` uses `Input.GetVector()` directly:

```csharp
Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_up", "move_down");
Velocity = inputDir.Normalized() * Constants.PlayerStats.MoveSpeed;
MoveAndSlide();
```

| Key(s) | Visual Result |
|--------|---------------|
| Up Arrow | Player moves up on screen |
| Down Arrow | Player moves down on screen |
| Left Arrow | Player moves left on screen |
| Right Arrow | Player moves right on screen |
| Up + Right | Player moves diagonally up-right |

### Combat (Implemented)

Auto-attack is fully automatic -- no button press needed. The player attacks the nearest enemy within range every cooldown tick.

| Class | Primary Attack | Range | Cooldown | Fallback |
|-------|---------------|-------|----------|----------|
| Warrior | Melee slash (instant) | 78 px | 0.42s | -- |
| Ranger | Arrow projectile | 250 px | 0.55s | -- |
| Mage | Magic bolt projectile | 200 px | 0.80s | Staff melee (78 px, 0.50s) when enemy is close |

### NPC Interaction (Implemented)

| Input | Keyboard | Context | Action |
|-------|----------|---------|--------|
| action_cross | S | Near NPC in town | Open NPC panel (name, greeting, service button) |
| -- | -- | Walk away from NPC | Auto-dismiss NPC panel |

### Dialogue (Implemented)

| Input | Keyboard | Context | Action |
|-------|----------|---------|--------|
| action_cross / Space / Enter | S / Space / Enter | Dialogue box open | Advance dialogue (or skip typewriter to show full text) |

### Class Selection (Implemented)

| Input | Keyboard | Action |
|-------|----------|--------|
| move_left / move_right | Left Arrow / Right Arrow | Navigate between class cards |
| action_cross / Space / Enter | S / Space / Enter | Select highlighted card, or confirm selection |
| Mouse click | -- | Click a card to select, click Confirm to proceed |

### System (Implemented)

| Input | Keyboard | Context | Action |
|-------|----------|---------|--------|
| Escape | Esc | Gameplay | Toggle pause menu (blocked when death screen is visible) |
| Escape | Esc | Pause menu open | Resume (close pause menu) |
| Escape | Esc | Death screen | Quit game |
| Escape | Esc | Shop window | Close shop |
| Escape | Esc | Ascend dialog | Cancel and close |
| R | R | Death screen only | Restart (reset state, load town) |
| F3 | F3 | Any time | Toggle debug panel |

### Debug (Implemented)

| Input | Keyboard | Context | Action |
|-------|----------|---------|--------|
| F3 | F3 | Any time | Toggle debug stats overlay (HP, level, XP, floor, enemies, kills, session time) |

Note: The `F` key for debug floor descent mentioned in some contexts is NOT currently implemented in the codebase. Floor descent is triggered by walking into the stairs-down trigger area.

---

## Design Spec (Not Yet Implemented)

Everything below is the locked design spec. These features will be built as their dependent systems come online.

### PS1 Controller Baseline

The game must be fully playable with a PS1 DualShock:

```
                L1          R1
    
         D-pad       triangle
                   square   circle
                     cross
    
                  Start
```

If it works with these inputs, it works on any controller and any keyboard.

### Face Buttons -- Combat Actions (Spec)

| PS1 | Keyboard | Default Action | Context: Menus | Assignable? |
|-----|----------|---------------|---------------|-------------|
| cross | S | Basic attack on current target | Confirm / Select | Yes |
| circle | D | Basic attack (alt) | Cancel / Back / Close | Yes |
| square | A | Basic attack (alt) | -- | Yes |
| triangle | W | Basic attack (alt) | -- | Yes |

Currently only `action_cross` (S) is used for NPC interaction and dialogue. Auto-attack does not require button press.

### Target Cycling -- L1/R1 Tap (Spec)

| PS1 | Keyboard | Action |
|-----|----------|--------|
| L1 tap | Q | Cycle target to previous enemy |
| R1 tap | E | Cycle target to next enemy |

Not yet implemented. Auto-attack currently always targets the nearest enemy.

### Shortcuts -- L1/R1 Hold + Face Button (Spec)

8 shortcut slots via hold-modifier. Not yet implemented (needs skills/items to assign).

### Game Window (Spec)

Start (Esc) should open a tabbed game window (Inventory/Skills/Stats/Pause/Settings). Currently Esc only opens the simple pause menu with Resume/Quit.

### Map Overlay (Spec)

| Input | Keyboard | Action |
|-------|----------|--------|
| map_toggle | M | Cycle map modes: Overlay -> Full Map -> Off |

Not yet implemented. Input action is defined in project.godot.

---

### Input Map (project.godot)

All 12 input actions are defined in `project.godot`. These match `Constants.InputActions`:

| Action Name | Key | Keycode | Currently Used By |
|-------------|-----|---------|-------------------|
| `move_up` | Up Arrow | 4194320 | Player movement |
| `move_down` | Down Arrow | 4194322 | Player movement |
| `move_left` | Left Arrow | 4194319 | Player movement, class selection nav |
| `move_right` | Right Arrow | 4194321 | Player movement, class selection nav |
| `action_cross` | S | 83 | NPC interaction, dialogue advance, class selection confirm |
| `action_circle` | D | 68 | (defined, not yet used in code) |
| `action_square` | A | 65 | (defined, not yet used in code) |
| `action_triangle` | W | 87 | (defined, not yet used in code) |
| `shoulder_left` | Q | 81 | (defined, not yet used in code) |
| `shoulder_right` | E | 69 | (defined, not yet used in code) |
| `map_toggle` | M | 77 | (defined, not yet used in code) |
| `start` | Esc | 4194305 | (defined, but pause uses raw Key.Escape check) |

**Note on Esc handling:** The pause menu, death screen, shop window, and ascend dialog all check for `Key.Escape` directly in `_UnhandledInput()` rather than using the `start` input action. This is a minor inconsistency -- the `start` action is defined but not used.

---

### Keyboard Layout (TKL)

```
Left hand (actions):              Right hand (movement):

[Q]  [W]  [E]                          [Up]
 L1   tri   R1                        [Left][Down][Right]
                                    
[A]  [S]  [D]
 sq   X    O

[M]        = Map overlay (cycle)
[Esc]      = Pause menu
[R]        = Restart (death screen only)
[F3]       = Debug panel toggle
```

---

### Gamepad Support (Deferred)

Adding gamepad is trivial -- add joypad events to the same Input Map actions. No code changes needed. `Input.GetVector()` and `Input.IsActionJustPressed()` read both keyboard and gamepad inputs automatically.

### Mouse / Touch (Deferred)

Pointer-to-move and virtual joystick are deferred.

## Resolved Questions

| Question | Decision |
|----------|----------|
| Primary movement input | Arrow keys (not WASD) |
| WASD purpose | Reserved for action buttons (not movement) |
| Attack input | Auto-attack (proximity-based, no button needed currently) |
| Target selection | Nearest enemy (L1/R1 cycling deferred) |
| Controller baseline | PS1 DualShock (~12 buttons) |
| Key rebinding | Deferred. All bindings are defaults. |
| Map toggle key | M (defined in input map, not yet implemented) |
| Isometric movement | NOT implemented. Screen-space movement is used. |
