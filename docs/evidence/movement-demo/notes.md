# Scripted Movement Demo — 4-Direction Test

**Date:** 2026-04-08
**Ticket:** Pre-P1 validation (movement pipeline test)

## What Was Tested
- Programmatic character movement (not player input — scripted demo)
- Character moves UP, DOWN, LEFT, RIGHT with 1-second pauses between moves
- Movement is smooth (96 px/s, 2 tiles per move)
- All 3 paper doll layers (body + armor + weapon) move together as a single unit
- Character renders above floor/wall tiles (ZIndex = 10)
- Auto-quit after demo completes (2s delay)

## Demo Sequence
1. 0.5s initial pause
2. Move UP 2 tiles → 1s pause
3. Move DOWN 2 tiles → 1s pause
4. Move LEFT 2 tiles → 1s pause
5. Move RIGHT 2 tiles
6. "Demo complete" → auto-quit after 2s

## Console Output
```
Input demo — scripted movement test
[Step 1/5] Moving UP
[Step 2/5] Moving DOWN
[Step 3/5] Moving LEFT
[Step 4/5] Moving RIGHT
[Step 5/5] Demo complete — auto-quit in 2s
```

## Implementation Details
- Character is a Node2D container with 3 Sprite2D children (body, armor, weapon)
- Movement uses `_Process(delta)` with linear interpolation toward target position
- State machine: waiting → moving → waiting → moving → ...
- MoveSpeed: 96 px/s, StepDistance: 64 px (2 tiles)

## Result
PASS — Smooth movement in all 4 cardinal directions. Paper doll layers stay composited during movement. Confirms the character rendering + movement pipeline works before implementing player input.

## Screenshots/Recordings
- TODO: capture on re-run

## Next Steps
- Replace scripted movement with arrow key player input
- Add isometric transform
- Add wall collision (CharacterBody2D + MoveAndSlide)
