# AutoPilot — Player Emulation Testing Library

## Summary

AutoPilot is a testing tool library that emulates a human player. It injects input, navigates menus, moves the character, and asserts the game responds correctly. The game runs exactly as it would for a real player — AutoPilot just provides the input.

Lives in `scripts/testing/` — separate from game code. Game code never references it.

## Architecture

```
scripts/testing/
├── AutoPilot.cs              ← Core: step runner, logging, assertions, lifecycle
├── AutoPilotActions.cs       ← Input simulation (wraps GodotTestDriver)
├── AutoPilotAssertions.cs    ← State verification: game state, inventory, UI
└── DebugTelemetry.cs         ← Full audit trail: input, signals, state (#if DEBUG)
```

### Dependencies

- **GodotTestDriver** (Chickensoft) — used for input simulation primitives only. Chickensoft is banned for game code but allowed for testing tools per convention update. AutoPilot wraps GodotTestDriver; walkthrough code never references it directly.

### How It Works

1. `FullRunSandbox.tscn` loads as the entry scene
2. Creates `AutoPilot` node attached to `GetTree().Root` (survives scene changes)
3. Scene changes to `main.tscn` (the real game starts normally)
4. AutoPilot runs an async scripted walkthrough over many frames
5. Each step: inject input → wait for response → check telemetry → assert state
6. Exits with code 0 (pass) or 1 (fail)

## Debug Telemetry

Full audit trail for debugging. Compiled out for release via `#if DEBUG`.

**Tracks:**
- Input consumption — which node handled each input event
- Signal emissions — timestamped (StatsChanged, PlayerDied, EnemyDefeated, etc.)
- State snapshots — periodic GameState dumps (HP, Mana, XP, Level, Floor, Gold)
- Scene changes — every ChangeSceneToFile and ScreenTransition

**Output:** `user://debug_telemetry_{timestamp}.jsonl` — per-session file, one JSON object per event per line.

**Debug console commands:** `telemetry start`, `telemetry stop`, `telemetry dump`

## Walkthrough Scope

Full walkthrough x3 — all 3 classes, each doing:

1. Splash screen → New Game
2. Class selection → confirm
3. Town → walk, visit all 5 NPCs (test service buttons)
4. Pause menu → open each sub-dialog
5. Enter dungeon → fight enemies → verify XP/gold gain
6. Verify achievement unlocks (First Blood, First Steps)
7. Force death (debug 'kill' command) → death flow → respawn
8. Save game → scene reload → load → verify state matches
9. Stairs → floor transition

### Two-Pass Save/Load Test

- **Pass 1 (make target):** Play and save mid-session
- **Pass 2 (separate make target):** Load save, verify all state matches

## Test Layers (Both Kept)

| Layer | File | What it tests | Godot needed? |
|-------|------|--------------|---------------|
| C# logic | `tests/unit/FullRunTests.cs` | Pure logic walkthrough — 10 phases, all systems | No |
| Godot walkthrough | `scripts/sandbox/FullRunSandbox.cs` | Real game with input simulation, 3 classes | Yes |

## Usage Contexts

AutoPilot is scene-agnostic:

| Context | How it's used |
|---------|--------------|
| **Full-run walkthrough** | Plays entire game x3 classes |
| **Combat sandbox** | Cast every skill, all 8 directions, verify damage |
| **Any sandbox** | Automated script alongside manual controls |
| **Debug console** | `telemetry start/stop/dump` during live play |

## API Reference

### AutoPilot (Core)

| Method | Purpose |
|--------|---------|
| `Run(label, step)` | Execute named async step with logging |
| `Log(message)` | Print `[AUTOPILOT] message` to stdout |
| `Assert(condition, desc)` | Log pass/fail, track counts |
| `Finish()` | Print summary, `GetTree().Quit(0 or 1)` |

### AutoPilotActions (Input — wraps GodotTestDriver)

| Method | Purpose |
|--------|---------|
| `Press(action)` | Single tap (press + auto-release) |
| `Hold(action)` | Start holding |
| `Release(action)` | Stop holding |
| `MoveDirection(dir, seconds)` | Walk in direction |
| `MoveToward(worldPos, timeout)` | Walk toward point |
| `ClickButton(button)` | Emit pressed signal |
| `WaitFrames(count)` | Wait N physics frames |
| `WaitSeconds(seconds)` | Wait real time |
| `WaitUntil(condition, timeout)` | Poll until true |
| `WaitForTransition()` | Wait for ScreenTransition |

### AutoPilotAssertions (State)

| Method | What it checks |
|--------|---------------|
| `Alive()` | HP > 0, not dead |
| `Dead()` | IsDead == true |
| `OnFloor(floor)` | FloorNumber matches |
| `HasGoldAtLeast(min)` | Gold >= min |
| `AchievementUnlocked(id)` | Specific achievement unlocked |
| `EnemiesExist()` | Enemies in scene tree |

## Flow Docs

Every walkthrough step references a flow doc in `docs/flows/`. Flow docs are the source of truth for input sequences, timing, and expected behavior. See `docs/flows/` for the complete list.

## Commands

```bash
make sandbox-headless SCENE=full-run   # Headless: 3 classes, exits 0/1
make sandbox SCENE=full-run            # Visual: watch it play
make test-unit                         # C# logic layer (fast, no Godot)
```
