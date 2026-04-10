#!/usr/bin/env bash
# Automated visual regression test for the full game loop.
# Runs test-game headless, captures console output, then compares
# against expected phase markers AND validates screenshot evidence exists.
# Usage: make test-game-visual-check

set -euo pipefail

PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
EVIDENCE_DIR="$PROJECT_DIR/docs/evidence/test-game"
cd "$PROJECT_DIR"

echo "=== Automated Visual Test ==="
echo ""

PASS=0
FAIL=0

check() {
    local label="$1"
    local result="$2"
    if [ "$result" = "1" ]; then
        echo "  PASS: $label"
        PASS=$((PASS + 1))
    else
        echo "  FAIL: $label"
        FAIL=$((FAIL + 1))
    fi
}

# ─── Step 1: Run headless test and verify all phases ───
echo "--- Step 1: Headless Logic Test ---"

OUTPUT=$( make test-game-headless 2>&1 || true )

# Check all 16 phases completed
check "Init"            "$( echo "$OUTPUT" | grep -q "FULL GAME LOOP TEST" && echo 1 || echo 0 )"
check "Town entry"      "$( echo "$OUTPUT" | grep -q "Entered Town" && echo 1 || echo 0 )"
check "Shopping"        "$( echo "$OUTPUT" | grep -q "Bought potions" && echo 1 || echo 0 )"
check "Dungeon entry"   "$( echo "$OUTPUT" | grep -q "Entered Dungeon Floor 1" && echo 1 || echo 0 )"
check "Enemy spawn"     "$( echo "$OUTPUT" | grep -q "Spawned.*monsters" && echo 1 || echo 0 )"
check "Combat"          "$( echo "$OUTPUT" | grep -q "Killed" && echo 1 || echo 0 )"
check "Floor cleared"   "$( echo "$OUTPUT" | grep -q "Floor 1 cleared" && echo 1 || echo 0 )"
check "Floor transition" "$( echo "$OUTPUT" | grep -q "Advanced to Floor 2" && echo 1 || echo 0 )"
check "Save"            "$( echo "$OUTPUT" | grep -q "Game saved on Floor 2" && echo 1 || echo 0 )"
check "Load"            "$( echo "$OUTPUT" | grep -q "Game loaded" && echo 1 || echo 0 )"
check "Town return"     "$( echo "$OUTPUT" | grep -q "Returned to Town" && echo 1 || echo 0 )"
check "Bank ops"        "$( echo "$OUTPUT" | grep -q "Bank deposit/withdraw verified" && echo 1 || echo 0 )"
check "Backpack expand" "$( echo "$OUTPUT" | grep -q "Backpack expanded" && echo 1 || echo 0 )"
check "Town save"       "$( echo "$OUTPUT" | grep -q "Saved in Town" && echo 1 || echo 0 )"
check "Town reload"     "$( echo "$OUTPUT" | grep -q "Reloaded in Town" && echo 1 || echo 0 )"
check "Elemental test"  "$( echo "$OUTPUT" | grep -q "ElementalCombat" && echo 1 || echo 0 )"
check "Crit test"       "$( echo "$OUTPUT" | grep -q "CritSystem" && echo 1 || echo 0 )"
check "Monster AI test" "$( echo "$OUTPUT" | grep -q "MonsterBehavior" && echo 1 || echo 0 )"
check "Spawner test"    "$( echo "$OUTPUT" | grep -q "MonsterSpawner" && echo 1 || echo 0 )"
check "All assertions"  "$( echo "$OUTPUT" | grep -q "0 failed" && echo 1 || echo 0 )"
check "All systems OK"  "$( echo "$OUTPUT" | grep -q "ALL SYSTEMS OPERATIONAL" && echo 1 || echo 0 )"

echo ""

# ─── Step 2: Check unit tests pass ───
echo "--- Step 2: Unit Tests ---"

TEST_OUTPUT=$( dotnet test tests/DungeonGame.Tests.csproj --verbosity minimal 2>&1 || true )
UNIT_PASSED=$( echo "$TEST_OUTPUT" | grep -oP 'Passed:\s+\K\d+' || echo "0" )
UNIT_FAILED=$( echo "$TEST_OUTPUT" | grep -oP 'Failed:\s+\K\d+' || echo "0" )

check "Unit tests pass" "$( echo "$TEST_OUTPUT" | grep -q "Passed!" && echo 1 || echo 0 )"
echo "    ($UNIT_PASSED passed, $UNIT_FAILED failed)"
echo ""

# ─── Step 3: Check evidence directory has screenshots ───
echo "--- Step 3: Evidence Artifacts ---"

if [ -d "$EVIDENCE_DIR" ]; then
    SCREENSHOT_COUNT=$( find "$EVIDENCE_DIR" -name "frame_*.png" -newer "$EVIDENCE_DIR" -o -name "frame_*.png" | wc -l | tr -d ' ' )
    VIDEO_COUNT=$( find "$EVIDENCE_DIR" -name "*.mov" | wc -l | tr -d ' ' )

    check "Evidence dir exists"  "1"
    check "Screenshots exist ($SCREENSHOT_COUNT)" "$( [ "$SCREENSHOT_COUNT" -gt 0 ] && echo 1 || echo 0 )"
    check "Video exists ($VIDEO_COUNT)"            "$( [ "$VIDEO_COUNT" -gt 0 ] && echo 1 || echo 0 )"
else
    check "Evidence dir exists" "0"
    check "Screenshots exist" "0"
    check "Video exists" "0"
    echo "    Run 'make test-game-capture' first to generate evidence"
fi

echo ""

# ─── Results ───
TOTAL=$((PASS + FAIL))
echo "=== Results: $PASS/$TOTAL passed, $FAIL failed ==="

if [ "$FAIL" -gt 0 ]; then
    echo ""
    echo "--- Headless output (for debugging) ---"
    echo "$OUTPUT" | grep "\[TEST-GAME\]" | tail -20
    exit 1
fi

echo "=== ALL VISUAL TESTS PASSED ==="
exit 0
