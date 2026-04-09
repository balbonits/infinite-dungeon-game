#!/usr/bin/env bash
# E2E test: Run the automated game demo in headless mode and verify all phases complete.
# Exit 0 = all checks pass, Exit 1 = failure.

set -euo pipefail

GODOT="/Applications/Godot_mono.app/Contents/MacOS/Godot"
PROJECT="/Users/johndilig/Projects/dungeon-web-game"
TIMEOUT=30

echo "=== E2E Demo Test ==="
echo "Running game demo in headless mode..."

# Run the demo and capture output
# In headless mode, demo steps run at 10ms delay (instant) and auto-quits on completion
OUTPUT=$( "$GODOT" --path "$PROJECT" --headless 2>&1 || true )

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

# Phase 1: Town
check "Demo starts"            "GAME SYSTEMS DEMO"
check "Player initialized"     "Player: Demo Hero Lv.1"
check "Movement works"         "Moving UP"
check "Stats panel opens"      "Opening Stats Panel"
check "Settings change"        "Target priority: Nearest -> Strongest"
check "NPC dialog"             "Old Sage:"
check "Shop buy sword"         "Bought 1x Iron Sword"
check "Shop buy potions"       "Bought 3x Health Potion"
check "Equip weapon"           "Equipped Iron Sword"
check "Equip armor"            "Equipped Leather Cap"

# Phase 2: Dungeon
check "Enter dungeon"          "Entered the dungeon! Floor 1"
check "Open chest"             "Opened chest"
check "Monster spawns"         "Giant Rat appears"
check "Auto-targeting"         "Auto-targeting nearest"
check "Basic attack"           "Basic attack"
check "Monster attacks"        "Giant Rat attacks"
check "Monster defeated"       "Giant Rat DEFEATED"
check "XP gained"              "+10 XP"
check "Loot drop"              "Loot: Rat Fang"
check "Skeleton spawns"        "Skeleton Warrior appears"
check "Skill used"             "Used Slash"
check "MP consumed"            "MP:"
check "Health potion used"     "Used Health Potion"
check "Skeleton defeated"      "Skeleton DEFEATED"

# Level up
check "Level up"               "LEVEL UP"
check "Stat allocation"        "Allocated stat point"

# Phase 3: Boss
check "Boss spawns"            "Orc Warlord appears"
check "Poison applied"         "Poisoned!"
check "Poison tick"            "POISON"
check "Boss defeated"          "Orc Warlord DEFEATED"
check "Rare loot"              "Orcish Blade"
check "Mana regen"             "Mana regeneration"

# Phase 4: Death
check "Death occurs"           "YOU DIED"
check "Respawn works"          "Returned to town"
check "Respawn location"       "Location: Town"

# Phase 5: Wrap up
check "Sell item"              "Sold Rat Fang"
check "Unequip item"           "Unequipped Iron Ring"
check "Save game"              "Game state saved"

# Phase 6: UI Showcase
check "UI showcase starts"     "PHASE 6: UI SHOWCASE"
check "XP bar"                 "XP Progress Bar"
check "Toast notifications"    "Toast Notifications"
check "Shortcut bar"           "Shortcut Bar"
check "Inventory grid"         "Inventory Grid"
check "Equipment panel"        "Equipment Panel"
check "Settings panel"         "Settings Panel"
check "Tooltip"                "Tooltip"
check "Death screen overlay"   "Death Screen"

# Phase 7: Performance Testing
check "Perf testing starts"    "PHASE 7: PERFORMANCE TESTING"
check "Live monitors"          "Live Monitors"
check "Bench combat"           "Combat calculations"
check "Bench stats"            "Stat recalculation"
check "Bench inventory"        "Inventory add/remove"
check "Bench XP"               "XP gain"
check "Bench sprites"          "Entity spawn"
check "Bench panels"           "panel creation"
check "Perf scorecard"         "PERFORMANCE SCORECARD"
check "Perf overall score"     "OVERALL:"

check "Demo completes"         "DEMO COMPLETE"
check "Final stats shown"      "Demo Hero Level"

echo ""
echo "=== Results: $PASS passed, $FAIL failed ==="

if [ "$FAIL" -gt 0 ]; then
    echo ""
    echo "--- Full output for debugging ---"
    echo "$OUTPUT"
    exit 1
fi

echo "All E2E checks passed!"
exit 0
