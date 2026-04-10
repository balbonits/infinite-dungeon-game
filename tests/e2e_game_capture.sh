#!/usr/bin/env bash
# Capture screenshots & video recording of the full game loop test.
# Uses macOS screencapture. Requires Screen Recording permission.
# Usage: make test-game-capture

set -euo pipefail

GODOT="/Applications/Godot_mono.app/Contents/MacOS/Godot"
PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
EVIDENCE_DIR="$PROJECT_DIR/docs/evidence/test-game"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
TEST_SCENE="scenes/tests/test_game.tscn"

mkdir -p "$EVIDENCE_DIR"

echo "=== Test-Game Visual Capture ==="
echo "Evidence dir: $EVIDENCE_DIR"
echo ""

# Build first
echo "Building..."
cd "$PROJECT_DIR"
dotnet build --nologo -v q 2>&1 | tail -1
echo ""

# ─── Phase 1: Timed Screenshots During Game Loop ───
echo "--- Phase 1: Timed Screenshots ---"

"$GODOT" --path "$PROJECT_DIR" "$TEST_SCENE" 2>/dev/null &
GODOT_PID=$!
FRAME_COUNT=0

# Capture at key moments (seconds after launch)
# 2s=init, 4s=town, 7s=dungeon, 10s=combat, 14s=floor2, 18s=save/load, 22s=summary
for SEC in 2 4 7 10 14 18 22; do
    if [ "$FRAME_COUNT" -eq 0 ]; then
        sleep "$SEC"
    else
        sleep $(( SEC - PREV_SEC ))
    fi
    PREV_SEC=$SEC
    FRAME="$EVIDENCE_DIR/frame_${SEC}s_$TIMESTAMP.png"
    screencapture -x "$FRAME" 2>/dev/null || true
    if [ -f "$FRAME" ] && [ -s "$FRAME" ]; then
        SIZE=$(du -h "$FRAME" | cut -f1)
        echo "  [${SEC}s] Captured ($SIZE)"
        FRAME_COUNT=$((FRAME_COUNT + 1))
    else
        echo "  [${SEC}s] Failed"
    fi
done

# Wait for test to finish (it auto-quits after ~25s)
sleep 5
kill "$GODOT_PID" 2>/dev/null || true
wait "$GODOT_PID" 2>/dev/null || true

echo "  Captured $FRAME_COUNT/7 screenshots"
echo ""

# ─── Phase 2: Video Recording ───
echo "--- Phase 2: Video Recording ---"

RECORDING="$EVIDENCE_DIR/test_game_$TIMESTAMP.mov"

# Start recording
screencapture -V 30 "$RECORDING" &
RECORD_PID=$!
sleep 1

# Launch game loop
"$GODOT" --path "$PROJECT_DIR" "$TEST_SCENE" 2>/dev/null &
GODOT_PID=$!

# Wait for test to complete (~25s) + buffer
sleep 28

# Stop everything
kill "$GODOT_PID" 2>/dev/null || true
kill "$RECORD_PID" 2>/dev/null || true
wait "$RECORD_PID" 2>/dev/null || true
wait "$GODOT_PID" 2>/dev/null || true

if [ -f "$RECORDING" ] && [ -s "$RECORDING" ]; then
    SIZE=$(du -h "$RECORDING" | cut -f1)
    echo "  Video captured ($SIZE)"
    RECORDING_OK=1
else
    echo "  Video failed (grant Screen Recording permission in System Settings)"
    RECORDING_OK=0
fi

echo ""

# ─── Summary ───
echo "=== Capture Results ==="
echo "  Screenshots: $FRAME_COUNT/7"
echo "  Video:       $([ "${RECORDING_OK:-0}" -eq 1 ] && echo "YES" || echo "NO")"
echo "  Evidence:    $EVIDENCE_DIR/"
echo ""
ls -lh "$EVIDENCE_DIR/"*"$TIMESTAMP"* 2>/dev/null || echo "  (no files)"
echo ""
echo "Done."
