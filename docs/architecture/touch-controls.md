# Touch Controls Research

## Key Finding

**Godot 4.7 will include a built-in `VirtualJoystick` Control node** (PR #110933, merged). Godot 4.7 is at dev4 (April 10, 2026), feature freeze imminent. Expected stable mid-2026.

The built-in VirtualJoystick:
- Integrates directly with **Godot's Input Map actions** — our existing `move_up/down/left/right` work with zero code changes
- Three modes: Fixed, Dynamic, Following
- Is a Control node (supports anchors/margins for responsive layout)
- Resolution-independent (procedural rendering)

**This eliminates the need for any joystick plugin.** Wait for 4.7, get it for free.

## Recommended Approach

### Phase 1 (Now, on 4.6) — Prepare

1. Fix raw `Key.Escape` checks to use `start` Input Map action (known inconsistency)
2. Add gamepad events to existing Input Map actions in project.godot (zero code changes)
3. Design touch control layout mockup
4. Consider `viewport` stretch mode for pixel-perfect mobile rendering

### Phase 2 (On 4.7+) — Implement

1. Upgrade to Godot 4.7 stable
2. Add `VirtualJoystick` to a CanvasLayer (maps to existing `move_*` actions)
3. Add 7 `TouchScreenButton` nodes for face buttons, shoulders, and pause
4. Set all to `VISIBILITY_TOUCHSCREEN_ONLY`
5. Add `Input.VibrateHandheld()` at gameplay events
6. Test Android first, then iOS

### If Needed Before 4.7

Use [MarcoFazioRandom/Virtual-Joystick-Godot](https://github.com/MarcoFazioRandom/Virtual-Joystick-Godot) (MIT, 952 stars) as a drop-in stopgap. Feeds Input Map actions directly — easy to rip out when upgrading.

## Touch Control Layout

```
┌─────────────────────────────────────────────┐
│  [L1]              [Pause]            [R1]  │
│                                             │
│                                             │
│                  (gameplay)                  │
│                                             │
│                                     [△]     │
│    ╭───╮                          [□] [○]   │
│    │ ◯ │  joystick               [✕]        │
│    ╰───╯                                    │
└─────────────────────────────────────────────┘
```

| Virtual Button | Input Map Action | Position |
|---------------|-----------------|----------|
| Joystick | move_up/down/left/right | Bottom-left |
| Cross (X) | action_cross | Bottom-right, south |
| Circle (O) | action_circle | Bottom-right, east |
| Square | action_square | Bottom-right, west |
| Triangle | action_triangle | Bottom-right, north |
| L1 | shoulder_left | Top-left |
| R1 | shoulder_right | Top-right |
| Pause | start | Top-center |

## Built-in TouchScreenButton (Available Now in 4.6)

Handles **action buttons** perfectly:
- `action` property → set to any Input Map action name (e.g., `"action_cross"`)
- Fires `InputEventAction` → existing `Input.IsActionJustPressed()` works automatically
- `visibility_mode = TOUCHSCREEN_ONLY` → auto-hides on desktop
- `passby_press` → fires if finger slides onto it (important for fast combat)
- Full multitouch support

**Limitation:** Node2D, not Control — no anchors/margins. Position manually or via script.

## GDScript Addons in C# Projects

**They work.** GDScript addons that emit Input Map actions require zero C# interop. The addon manipulates Godot's Input singleton; C# reads from the same singleton. Add the GDScript scene as a child node and configure in inspector.

## Plugin Survey

### Recommended (if needed before 4.7)

| Plugin | License | Godot | Stars | Key Feature |
|--------|---------|-------|-------|-------------|
| [Virtual-Joystick-Godot](https://github.com/MarcoFazioRandom/Virtual-Joystick-Godot) | MIT | 4.2+ | 952 | Feeds Input Map actions directly |

### Others Evaluated

| Plugin | License | Verdict |
|--------|---------|---------|
| Virtual Joystick Plus (Asset Library) | MIT | Custom signals, doesn't feed Input Map — worse fit |
| Versatile Mobile Joystick | MIT | Less popular, less documented |
| MuchLab/VirtualJoyStick | MIT | C# native but 1 star, no docs |
| G4 Ultimate Touch Joypad | — | Dead since 2022, uses Visual Script |
| Godot Touch Input Manager | MIT | Gesture recognition, not gamepad emulation |

## Responsive UI for Mobile

**Sizing:** Minimum 80-120px per button at 1080p. Joystick 200-300px diameter.

**Layout:** Anchors to pin to screen edges. All touch controls on a CanvasLayer (fixed regardless of camera). Keep center clear for gameplay.

**Phone vs tablet:** Phones 19:9-20:9; tablets 4:3. Use `expand` stretch aspect (already set).

**Stretch mode:** Consider `viewport` (pixel-perfect) over `canvas_items` for crisp pixel art on mobile.

## Conditional Visibility

```csharp
bool isTouchDevice = DisplayServer.IsTouchscreenAvailable();
bool isMobile = OS.GetName() == "Android" || OS.GetName() == "iOS";
```

Or just use `TouchScreenButton.visibility_mode = VISIBILITY_TOUCHSCREEN_ONLY` — Godot handles it.

For desktop testing: enable "Emulate Touch from Mouse" in Project Settings.

## Haptic Feedback

**Built-in:**
```csharp
Input.VibrateHandheld(200); // vibrate 200ms — no-op on desktop
```
Android requires VIBRATE permission in export preset.

**Suggested mapping:**

| Event | Duration |
|-------|----------|
| Attack hit | 50ms |
| Take damage | 100ms |
| Critical hit | 80-150ms |
| Death | 300ms |
| Level up | 50ms x2 |
| Button press | 20ms |

**For finer control:** [kyoz/godot-haptics](https://github.com/kyoz/godot-haptics) (MIT, 34 stars) — light/medium/heavy intensity via native Android/iOS APIs.

## Key Links

- [Godot VirtualJoystick PR #110933](https://github.com/godotengine/godot/pull/110933)
- [MarcoFazio Virtual Joystick](https://github.com/MarcoFazioRandom/Virtual-Joystick-Godot)
- [TouchScreenButton docs](https://docs.godotengine.org/en/stable/classes/class_touchscreenbutton.html)
- [godot-haptics](https://github.com/kyoz/godot-haptics)
