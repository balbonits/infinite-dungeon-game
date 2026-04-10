#!/usr/bin/env bash
# Universal test runner with flags.
# Usage:
#   ./tests/run-test.sh <scene-name> [--headless] [--capture] [--check]
#
# Examples:
#   ./tests/run-test.sh test-game              # windowed (default)
#   ./tests/run-test.sh test-game --headless   # headless, console output
#   ./tests/run-test.sh test-game --capture    # screenshots + video
#   ./tests/run-test.sh test-game --check      # regression (headless + evidence)
#   ./tests/run-test.sh test-hero              # windowed hero viewer
#   ./tests/run-test.sh test-hero --capture    # capture hero screenshots
#   ./tests/run-test.sh test-dungeon --capture # capture dungeon gen screenshots

set -euo pipefail

GODOT="/Applications/Godot_mono.app/Contents/MacOS/Godot"
PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
EVIDENCE_BASE="$PROJECT_DIR/docs/evidence"

# ─── Parse Args ──────────────────────────────────────────────────────────────

SCENE_NAME=""
MODE="windowed"  # default

for arg in "$@"; do
    case "$arg" in
        --headless)  MODE="headless" ;;
        --capture)   MODE="capture" ;;
        --check)     MODE="check" ;;
        --help|-h)
            echo "Usage: $0 <scene-name> [--headless] [--capture] [--check]"
            echo ""
            echo "Modes:"
            echo "  (default)    Windowed — launch and watch"
            echo "  --headless   Headless — console output, auto-quits"
            echo "  --capture    Screenshots at timed intervals + video"
            echo "  --check      Headless run + verify output markers"
            echo ""
            echo "Examples:"
            echo "  $0 test-game"
            echo "  $0 test-game --headless"
            echo "  $0 test-hero --capture"
            echo "  $0 test-dungeon --check"
            exit 0
            ;;
        -*)
            echo "Unknown flag: $arg (use --help)" >&2
            exit 1
            ;;
        *)
            SCENE_NAME="$arg"
            ;;
    esac
done

if [ -z "$SCENE_NAME" ]; then
    echo "Error: scene name required. Usage: $0 <scene-name> [--headless|--capture|--check]" >&2
    echo "Run '$0 --help' for examples." >&2
    exit 1
fi

# ─── Resolve Scene Path ─────────────────────────────────────────────────────

# Try multiple locations for the scene file
SCENE_PATH=""
for candidate in \
    "scenes/tests/${SCENE_NAME}.tscn" \
    "scenes/tests/${SCENE_NAME#test-}.tscn" \
    "scenes/tests/test_${SCENE_NAME#test-}.tscn" \
    "scenes/${SCENE_NAME}.tscn" \
    "scenes/${SCENE_NAME#test-}.tscn"; do
    if [ -f "$PROJECT_DIR/$candidate" ]; then
        SCENE_PATH="$candidate"
        break
    fi
done

# Also check with underscores instead of dashes
if [ -z "$SCENE_PATH" ]; then
    UNDERSCORE_NAME=$(echo "$SCENE_NAME" | tr '-' '_')
    for candidate in \
        "scenes/tests/${UNDERSCORE_NAME}.tscn" \
        "scenes/tests/test_${UNDERSCORE_NAME#test_}.tscn"; do
        if [ -f "$PROJECT_DIR/$candidate" ]; then
            SCENE_PATH="$candidate"
            break
        fi
    done
fi

if [ -z "$SCENE_PATH" ]; then
    echo "Error: could not find scene for '$SCENE_NAME'" >&2
    echo "Searched: scenes/tests/${SCENE_NAME}.tscn and variants" >&2
    echo ""
    echo "Available test scenes:"
    ls "$PROJECT_DIR/scenes/tests/"*.tscn 2>/dev/null | xargs -I{} basename {} .tscn | sed 's/^/  /'
    exit 1
fi

echo "Scene: $SCENE_PATH"
echo "Mode:  $MODE"
echo ""

cd "$PROJECT_DIR"

# Build first
dotnet build --nologo -v q 2>&1 | tail -1

# ─── Windowed Mode ───────────────────────────────────────────────────────────

if [ "$MODE" = "windowed" ]; then
    "$GODOT" --path . "$SCENE_PATH" &
    echo "Launched windowed. PID: $!"
    exit 0
fi

# ─── Headless Mode ───────────────────────────────────────────────────────────

if [ "$MODE" = "headless" ]; then
    "$GODOT" --path . --headless "$SCENE_PATH" 2>&1
    exit $?
fi

# ─── Capture Mode ────────────────────────────────────────────────────────────

if [ "$MODE" = "capture" ]; then
    EVIDENCE_DIR="$EVIDENCE_BASE/$SCENE_NAME"
    TIMESTAMP=$(date +%Y%m%d_%H%M%S)
    mkdir -p "$EVIDENCE_DIR"

    echo "--- Capturing screenshots ---"

    "$GODOT" --path . "$SCENE_PATH" 2>/dev/null &
    GODOT_PID=$!
    FRAME_COUNT=0

    # Capture at 2, 5, 10, 15, 20 seconds
    PREV=0
    for SEC in 2 5 10 15 20; do
        sleep $(( SEC - PREV ))
        PREV=$SEC
        FRAME="$EVIDENCE_DIR/frame_${SEC}s_$TIMESTAMP.png"
        screencapture -x "$FRAME" 2>/dev/null || true
        if [ -f "$FRAME" ] && [ -s "$FRAME" ]; then
            echo "  [${SEC}s] Captured ($(du -h "$FRAME" | cut -f1))"
            FRAME_COUNT=$((FRAME_COUNT + 1))
        else
            echo "  [${SEC}s] Failed"
        fi
    done

    sleep 3
    kill "$GODOT_PID" 2>/dev/null || true
    wait "$GODOT_PID" 2>/dev/null || true

    echo "  $FRAME_COUNT/5 screenshots captured"
    echo ""

    echo "--- Capturing video (20s) ---"
    RECORDING="$EVIDENCE_DIR/${SCENE_NAME}_$TIMESTAMP.mov"
    screencapture -V 20 "$RECORDING" &
    RECORD_PID=$!
    sleep 1

    "$GODOT" --path . "$SCENE_PATH" 2>/dev/null &
    GODOT_PID=$!

    sleep 22
    kill "$GODOT_PID" 2>/dev/null || true
    kill "$RECORD_PID" 2>/dev/null || true
    wait "$RECORD_PID" 2>/dev/null || true
    wait "$GODOT_PID" 2>/dev/null || true

    if [ -f "$RECORDING" ] && [ -s "$RECORDING" ]; then
        echo "  Video: $(du -h "$RECORDING" | cut -f1)"
    else
        echo "  Video failed (needs Screen Recording permission)"
    fi

    echo ""
    echo "Evidence: $EVIDENCE_DIR/"
    ls -lh "$EVIDENCE_DIR/"*"$TIMESTAMP"* 2>/dev/null || true
    exit 0
fi

# ─── Check Mode ──────────────────────────────────────────────────────────────

if [ "$MODE" = "check" ]; then
    EVIDENCE_DIR="$EVIDENCE_BASE/$SCENE_NAME"
    PASS=0
    FAIL=0

    check() {
        if [ "$2" = "1" ]; then
            echo "  PASS: $1"
            PASS=$((PASS + 1))
        else
            echo "  FAIL: $1"
            FAIL=$((FAIL + 1))
        fi
    }

    echo "--- Headless run ---"
    OUTPUT=$( "$GODOT" --path . --headless "$SCENE_PATH" 2>&1 || true )

    # Check for common success/failure markers
    check "Scene runs without crash" "$( echo "$OUTPUT" | grep -qv "SCRIPT ERROR" && echo 1 || echo 0 )"
    check "No unhandled exceptions"  "$( echo "$OUTPUT" | grep -qv "Unhandled exception" && echo 1 || echo 0 )"

    # If this is test-game, check specific markers
    if [ "$SCENE_NAME" = "test-game" ]; then
        check "Init"            "$( echo "$OUTPUT" | grep -q "FULL GAME LOOP TEST" && echo 1 || echo 0 )"
        check "Town"            "$( echo "$OUTPUT" | grep -q "Entered Town" && echo 1 || echo 0 )"
        check "Dungeon"         "$( echo "$OUTPUT" | grep -q "Entered Dungeon Floor 1" && echo 1 || echo 0 )"
        check "Combat"          "$( echo "$OUTPUT" | grep -q "Killed" && echo 1 || echo 0 )"
        check "Save/Load"       "$( echo "$OUTPUT" | grep -q "Game loaded" && echo 1 || echo 0 )"
        check "All assertions"  "$( echo "$OUTPUT" | grep -q "ALL SYSTEMS OPERATIONAL" && echo 1 || echo 0 )"
    fi

    echo ""

    # Check evidence exists
    echo "--- Evidence check ---"
    if [ -d "$EVIDENCE_DIR" ]; then
        SC=$( find "$EVIDENCE_DIR" -name "frame_*.png" | wc -l | tr -d ' ' )
        VD=$( find "$EVIDENCE_DIR" -name "*.mov" | wc -l | tr -d ' ' )
        check "Evidence dir exists" "1"
        check "Screenshots ($SC)"   "$( [ "$SC" -gt 0 ] && echo 1 || echo 0 )"
        check "Video ($VD)"         "$( [ "$VD" -gt 0 ] && echo 1 || echo 0 )"
    else
        check "Evidence dir exists" "0"
        echo "    Run '$0 $SCENE_NAME --capture' first"
    fi

    echo ""
    TOTAL=$((PASS + FAIL))
    echo "=== $PASS/$TOTAL passed, $FAIL failed ==="
    [ "$FAIL" -gt 0 ] && exit 1
    exit 0
fi
