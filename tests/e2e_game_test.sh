#!/usr/bin/env bash
# E2E test: Run the full game loop test in headless mode and verify all phases complete.
# Exit 0 = all checks pass, Exit 1 = failure.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
cd "$SCRIPT_DIR"

echo "=== E2E Full Game Loop Test ==="
echo "Running test-game in headless mode..."

# Run the test and capture output
OUTPUT=$( make test-game 2>&1 || true )

echo ""
echo "--- Checking assertions ---"

PASS=0
FAIL=0

check() {
    local label="$1"
    local pattern="$2"
    if echo "$OUTPUT" | grep -q "$pattern"; then
        echo "  PASS: $label"
        PASS=$((PASS + 1))
    else
        echo "  FAIL: $label (expected: $pattern)"
        FAIL=$((FAIL + 1))
    fi
}

# Phase 1: Init
check "Test starts"               "\[TEST-GAME\] === FULL GAME LOOP TEST ==="

# Phase 2: Town
check "Entered Town"              "Entered Town"
check "Bought potions"            "Bought potions"

# Phase 3: Dungeon
check "Entered Dungeon Floor 1"   "Entered Dungeon Floor 1"

# Phase 4: Spawn
check "Spawned monsters"          "Spawned.*monsters"

# Phase 5: Combat
check "Killed enemy"              "Killed"

# Phase 6: Level check
check "Floor 1 cleared"           "Floor 1 cleared"

# Phase 7: Floor transition
check "Advanced to Floor 2"       "Advanced to Floor 2"
check "Floor 2 combat"            "Floor 2 combat verified"

# Phase 8: Save
check "Game saved on Floor 2"     "Game saved on Floor 2"

# Phase 9: Load
check "Game loaded"               "Game loaded"

# Phase 10: Return to town
check "Returned to Town"          "Returned to Town"

# Phase 11: Bank
check "Bank verified"             "Bank deposit/withdraw verified"

# Phase 12: Backpack
check "Backpack expanded"         "Backpack expanded"

# Phase 13: Town save
check "Saved in Town"             "Saved in Town"

# Phase 14: Reload
check "Reloaded in Town"          "Reloaded in Town"

# Phase 15: New systems
check "ElementalCombat"           "ElementalCombat"
check "CritSystem"                "CritSystem"
check "MonsterBehavior"           "MonsterBehavior"
check "MonsterSpawner"            "MonsterSpawner"

# Phase 16: Summary
check "Test complete"             "=== TEST COMPLETE ==="
check "All systems"               "ALL SYSTEMS OPERATIONAL"

echo ""
echo "=== Results: $PASS passed, $FAIL failed ==="

if [ "$FAIL" -gt 0 ]; then
    echo ""
    echo "--- Full output for debugging ---"
    echo "$OUTPUT"
    exit 1
fi

echo "=== ALL E2E ASSERTIONS PASSED ==="
exit 0
