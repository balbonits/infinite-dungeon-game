# Controls

## Summary

PS1 DualShock controller as the MVP baseline. Arrow keys for movement, face buttons for combat actions, L1/R1 for target cycling and shortcut modifiers. Designed so the game works with ~12 inputs, then scales cleanly to modern controllers and full keyboards.

**Core philosophy:** "Dumb hack n slash" — mash buttons to attack, bump shoulders to switch targets. Zero learning curve for new players.

## Current State

**Spec status: LOCKED.**

Redesigned from the Phaser prototype's WASD + auto-attack scheme. New control system uses arrow keys for movement, face buttons for combat, and a PS1-baseline button count.

## Design

### PS1 Controller Baseline

The game must be fully playable with a PS1 DualShock:

```
                L1          R1
    
         D-pad       △
                   □   ○
                     ✕
    
                  Start
```

If it works with these inputs, it works on any controller and any keyboard.

---

### Movement

| Input | PS1 | Keyboard | Action |
|-------|-----|----------|--------|
| D-pad / Left stick | D-pad | Arrow keys | 8-directional isometric movement (190 px/s) |

Movement uses Godot's Input Map system. Arrow keys map to `move_up`, `move_down`, `move_left`, `move_right` actions. WASD is **not** mapped to movement — those keys are reserved for the action button area.

#### Isometric Transform

Raw keyboard input is screen-space. A `Transform2D` matrix converts to isometric world directions:

```csharp
private static readonly Transform2D IsoTransform = new Transform2D(
    new Vector2(1, 0.5f),    // screen-right → iso-southeast
    new Vector2(-1, 0.5f),   // screen-down → iso-southwest
    Vector2.Zero
);

Vector2 rawInput = Input.GetVector("move_left", "move_right", "move_up", "move_down");
Vector2 worldDir = (IsoTransform * rawInput).Normalized();
Velocity = worldDir * MoveSpeed;
MoveAndSlide();
```

| Key(s) | Screen Direction | Isometric Direction | Visual Result |
|--------|-----------------|-------------------|---------------|
| Up Arrow | Screen up | Northeast | Player moves up-right |
| Down Arrow | Screen down | Southwest | Player moves down-left |
| Left Arrow | Screen left | Northwest | Player moves up-left |
| Right Arrow | Screen right | Southeast | Player moves down-right |
| Up + Right | Screen up-right | East | Player moves pure right |
| Up + Left | Screen up-left | North | Player moves pure up |
| Down + Right | Screen down-right | South | Player moves pure down |
| Down + Left | Screen down-left | West | Player moves pure left |

---

### Face Buttons — Combat Actions

| PS1 | Keyboard | Default Action | Context: Menus | Assignable? |
|-----|----------|---------------|---------------|-------------|
| ✕ (Cross) | S | Basic attack on current target | Confirm / Select | Yes |
| ○ (Circle) | D | Basic attack (alt) | Cancel / Back / Close | Yes |
| □ (Square) | A | Basic attack (alt) | — | Yes |
| △ (Triangle) | W | Basic attack (alt) | — | Yes |

Keyboard layout mirrors the PS1 diamond: W on top (△), A left (□), D right (○), S bottom (✕).

**Default behavior:** All face buttons perform **basic attack** until the player assigns something else via the shortcut system. A new player can mash any face button to fight — zero learning curve. As they unlock skills and items, they assign them to replace the defaults.

**Context switching:** ✕ and ○ change behavior in menus (Confirm/Cancel). In the dungeon, they're combat buttons. NPC proximity + ✕ opens the NPC panel; ○ closes it.

---

### Target Cycling — L1/R1 Tap

| PS1 | Keyboard | Action |
|-----|----------|--------|
| L1 tap | Q | Cycle target to previous enemy |
| R1 tap | E | Cycle target to next enemy |

**Targeting behavior:**
- **Default (no cycling):** Attacks hit based on the active **target priority setting** (defaults to nearest enemy)
- **After L1/R1 tap:** A target indicator (highlight ring) appears on the selected enemy
- Attacks focus on the locked target until it dies, moves out of range, or player cycles again
- When the targeted enemy dies, targeting reverts to priority-based auto-selection
- Cycling order follows the current target priority mode

**Target priority setting (configurable in Settings):**

| Priority Mode | Description | Default? |
|--------------|-------------|----------|
| Nearest | Closest enemies first | Yes |
| Strongest | Highest damage enemies first | |
| Tankiest | Highest HP enemies first | |
| Bosses | Boss enemies first, then nearest | |
| Weakest | Lowest HP enemies first (finish off wounded) | |

Player selects a mode in Settings. L1/R1 cycling follows that priority order. Can be changed anytime.

---

### Shortcuts — L1/R1 Hold + Face Button

| Combo | Keyboard | Shortcut Slot |
|-------|----------|--------------|
| L1 hold + ✕ | Q hold + S | Slot 1 |
| L1 hold + ○ | Q hold + D | Slot 2 |
| L1 hold + □ | Q hold + A | Slot 3 |
| L1 hold + △ | Q hold + W | Slot 4 |
| R1 hold + ✕ | E hold + S | Slot 5 |
| R1 hold + ○ | E hold + D | Slot 6 |
| R1 hold + □ | E hold + A | Slot 7 |
| R1 hold + △ | E hold + W | Slot 8 |

**Tap vs Hold distinction:**
- L1/R1 **tapped** (pressed and released < 200ms) = cycle target
- L1/R1 **held** (pressed > 200ms) = modifier for shortcut slots
- Visual: when L1/R1 is held, a small shortcut bar appears on-screen showing the 4 assigned slots

**Shortcut rules:**
- 8 slots total (4 per bumper)
- One assignment per slot — consumable item, active skill, or innate skill
- Assigned via the game window (Inventory/Skills tabs)
- Empty slots do nothing when pressed
- Diablo 2 style — simple, one-to-one mapping. No Fallout-style multi-assignment.

---

### System Buttons

| PS1 | Keyboard | Action |
|-----|----------|--------|
| Start | Esc | Game window (Inventory / Skills / Stats / Pause / Settings). Pauses game. |
| — | M | Map overlay (cycles: Overlay → Full Map → Off). Does NOT pause game. |
| ~~Select~~ | ~~P~~ | ~~Removed. Merged into Start (Esc).~~ |

**Game window (Esc):** A tabbed window containing all panels: Inventory, Skills, Stats, Pause, and Settings. When open, L1/R1 (Q/E) cycle between tabs (repurposed from target cycling while panel is active). Inside panels the player manages inventory, views skills, checks stats, assigns shortcuts, and accesses pause/settings. Opening the game window pauses gameplay. Closing it unpauses.

**Map overlay (M key):** Cycles through three states: Overlay (translucent map on top of gameplay) → Full Map (opaque, centered) → Off. Does not pause the game in any mode. This is a dedicated key outside the PS1 baseline — not context-dependent.

**Direct panel shortcuts (unbound by default):**

| Action | Default Key | Description |
|--------|------------|-------------|
| `panel_inventory` | Unbound | Open Inventory tab directly |
| `panel_skills` | Unbound | Open Skills tab directly |
| `panel_stats` | Unbound | Open Stats tab directly |

Players can bind these in Settings for quick access without cycling through tabs. These are optional convenience bindings — the Esc game window with tab cycling is always available.

---

### Keyboard Layout (TKL)

```
Left hand (actions):              Right hand (movement):

[Q]  [W]  [E]                          [↑]
 L1   △    R1                        [←][↓][→]
                                    
[A]  [S]  [D]
 □    ✕    ○

[M]        = Map overlay (cycle)
[Esc]      = Start (game window / pause)
```

The WASD diamond mirrors the PS1 face button diamond exactly. Q and E sit on either side as the bumpers — mirrors the QWE row naturally. M for Map is a dedicated key outside the action area.

---

### Input Map (project.godot)

| Action Name | Key 1 | Key 2 (gamepad) | Purpose |
|-------------|-------|-----------------|---------|
| `move_up` | Up Arrow | D-pad Up / Left Stick Up | Movement |
| `move_down` | Down Arrow | D-pad Down / Left Stick Down | Movement |
| `move_left` | Left Arrow | D-pad Left / Left Stick Left | Movement |
| `move_right` | Right Arrow | D-pad Right / Left Stick Right | Movement |
| `action_cross` | S | Cross (✕) | Basic attack / Confirm |
| `action_circle` | D | Circle (○) | Basic attack / Cancel |
| `action_square` | A | Square (□) | Basic attack / Assignable |
| `action_triangle` | W | Triangle (△) | Assignable face button |
| `shoulder_left` | Q | L1 | Target cycle / Shortcut modifier |
| `shoulder_right` | E | R1 | Target cycle / Shortcut modifier |
| `start` | Esc | Start | Game window (Inventory/Skills/Stats/Pause/Settings) |
| `map_toggle` | M | — | Map overlay cycle |
| `panel_inventory` | Unbound | — | Direct open Inventory (bindable) |
| `panel_skills` | Unbound | — | Direct open Skills (bindable) |
| `panel_stats` | Unbound | — | Direct open Stats (bindable) |

---

### Context-Dependent Input

| Context | ✕ (S) | ○ (D) | □ (A) | △ (W) | L1 (Q) | R1 (E) |
|---------|-------|-------|-------|-------|--------|--------|
| **Dungeon** | Attack / Shortcut | Attack / Shortcut | Attack / Shortcut | Attack / Shortcut | Cycle target / Hold: shortcuts 1-4 | Cycle target / Hold: shortcuts 5-8 |
| **Menus/Panels** | Confirm | Cancel / Close | — | — | Previous tab | Next tab |
| **NPC proximity** | Open NPC panel | Close NPC panel | — | — | — | — |
| **Death screen** | Confirm choice | — | — | — | — | — |

---

### Key Rebinding

All key bindings listed above are **defaults** — the industry-standard starting layout. Players can rebind any action in the Settings menu. Godot's Input Map system supports runtime rebinding natively.

**Rebinding rules:**
- Any action can be rebound to any key
- Multiple keys can map to the same action (alternates)
- Conflicts are warned but allowed (player's choice)
- Reset to defaults option available
- Rebindings are saved per profile (persisted in settings file, not the save slot)

### P1 Implementation Scope

**P1 implements (basic systems):**
- Arrow key movement with isometric transform
- Face button (S) basic attack on current target
- Esc to pause / restart from death screen
- Input Map in project.godot with all actions defined

**P2+ implements (as systems come online):**
- L1/R1 target cycling with visual indicator + priority setting
- L1/R1 hold shortcut system (needs skills/items to assign)
- Esc → Game window with tabbed UI (Inventory/Skills/Stats/Pause/Settings)
- Shortcut assignment interface
- Gamepad support via Input Map (same actions, add joypad events)

---

### Gamepad Support (Deferred)

Adding gamepad is trivial — add joypad events to the same Input Map actions. No code changes needed. `Input.GetVector()` and `Input.IsActionJustPressed()` read both keyboard and gamepad inputs automatically.

### Mouse / Touch (Deferred)

Pointer-to-move and virtual joystick are deferred. When implemented, pointer position uses `GetGlobalMousePosition()` (world-space, no iso conversion needed).

## Resolved Questions

| Question | Decision |
|----------|----------|
| Primary movement input | Arrow keys (not WASD) |
| WASD purpose | Reserved for action buttons (not movement) |
| Attack input | Face button press (not automatic proximity) |
| Target selection | L1/R1 cycles, configurable priority mode (nearest/strongest/etc.) |
| Shortcut system | 8 slots via L1/R1 hold + face buttons, Diablo 2 style |
| Controller baseline | PS1 DualShock (~12 buttons) |
| Panel system | Start (Esc) opens game window with all tabs (Inventory/Skills/Stats/Pause/Settings) |
| Key rebinding | Supported. All bindings are defaults — player can rebind any action in Settings. |
| Map toggle key | M (dedicated key outside PS1 baseline). Triangle (W) freed for combat. |
| Select button | Removed. Start (Esc) handles all panels + pause. |
