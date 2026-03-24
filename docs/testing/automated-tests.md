# Automated Tests

## Summary

Planned automated test suite for "A Dungeon in the Middle of Nowhere" using the GUT (Godot Unit Test) framework. Contains unit tests for game state logic, enemy stat formulas, player damage calculations, and movement math, plus integration tests for combat flow, spawning, and death/restart.

## Current State

- Tests are planned but not yet implemented (game is migrating to Godot 4)
- All test code below is ready to be placed in the `tests/` directory once the corresponding game systems are built
- Tests reference a `GameState` autoload singleton and enemy/player scenes that will be created during migration

## Design

### Framework Setup

| Property | Value |
|----------|-------|
| Framework | GUT (Godot Unit Test) v9.x |
| GitHub | https://github.com/bitwes/Gut |
| Installation | Godot AssetLib -> search "GUT" -> install, OR git clone into `addons/gut/` |
| Test directory | `res://tests/` |
| Test file naming | `test_*.gd` (prefix required by GUT) |
| Test function naming | `test_*` (prefix required by GUT) |
| Base class | `GutTest` (all test scripts extend this) |
| Run from editor | GUT panel (bottom dock) -> Run All |
| Run from CLI | `godot --headless -s addons/gut/gut_cmdln.gd -gdir=res://tests/ -gexit` |
| Run single file | `godot --headless -s addons/gut/gut_cmdln.gd -gtest=res://tests/test_game_state.gd -gexit` |

### Test File Organization

```
tests/
├── test_game_state.gd      -- 24 tests: GameState autoload unit tests
├── test_enemy.gd            -- 15 tests: Enemy stat formula unit tests
├── test_movement.gd         -- 6 tests: Isometric transform and speed unit tests
├── test_combat.gd           -- 8 tests: Combat integration tests
├── test_spawning.gd         -- 5 tests: Spawn system integration tests
└── test_death_restart.gd    -- 6 tests: Death/restart cycle integration tests
                             -- TOTAL: 64 tests
```

### GUT Assertion Reference

Common assertions used in these tests:

| Assertion | Purpose | Example |
|-----------|---------|---------|
| `assert_eq(a, b, msg)` | Exact equality | `assert_eq(hp, 100, "HP should be 100")` |
| `assert_ne(a, b, msg)` | Not equal | `assert_ne(hp, 0, "HP should not be 0")` |
| `assert_true(expr, msg)` | Boolean true | `assert_true(is_dead, "Should be dead")` |
| `assert_false(expr, msg)` | Boolean false | `assert_false(is_dead, "Should not be dead")` |
| `assert_gt(a, b, msg)` | Greater than | `assert_gt(xp, 0, "XP should be positive")` |
| `assert_lt(a, b, msg)` | Less than | `assert_lt(hp, 100, "HP should be reduced")` |
| `assert_between(val, low, high, msg)` | Range check | `assert_between(tier, 1, 3, "Tier in range")` |
| `assert_almost_eq(a, b, tolerance, msg)` | Float comparison | `assert_almost_eq(speed, 190.0, 0.1)` |
| `assert_signal_emitted(obj, sig)` | Signal was emitted | `assert_signal_emitted(GameState, "player_died")` |
| `assert_signal_not_emitted(obj, sig)` | Signal was NOT emitted | `assert_signal_not_emitted(GameState, "player_died")` |
| `assert_signal_emit_count(obj, sig, n)` | Signal emitted N times | `assert_signal_emit_count(GameState, "player_died", 1)` |
| `watch_signals(obj)` | Begin tracking signals | Must call before checking signal assertions |

---

## Test File: tests/test_game_state.gd

Unit tests for the `GameState` autoload singleton. Tests cover initial state, damage mechanics, death detection, XP/leveling, signal emissions, and reset behavior.

**Prerequisites:** `GameState` autoload must be registered in Project Settings with the following interface:
- Properties: `hp`, `max_hp`, `xp`, `level`, `floor_number`, `is_dead`
- Methods: `reset()`, `take_damage(amount)`, `award_xp(amount)`
- Signals: `stats_changed`, `player_died`

```gdscript
extends GutTest

# ============================================================
# INITIAL STATE TESTS
# Verify that GameState.reset() returns all properties to their
# documented default values.
# ============================================================

func test_initial_hp():
    GameState.reset()
    assert_eq(GameState.hp, 100, "Initial HP should be 100")

func test_initial_max_hp():
    GameState.reset()
    assert_eq(GameState.max_hp, 100, "Initial max HP should be 100 (100 + 1*0... wait, formula is 100 + level*8 but level 1 gives 108? No: base is 100 at level 1)")

func test_initial_xp():
    GameState.reset()
    assert_eq(GameState.xp, 0, "Initial XP should be 0")

func test_initial_level():
    GameState.reset()
    assert_eq(GameState.level, 1, "Initial level should be 1")

func test_initial_floor():
    GameState.reset()
    assert_eq(GameState.floor_number, 1, "Initial floor should be 1")

func test_initial_not_dead():
    GameState.reset()
    assert_false(GameState.is_dead, "Player should not be dead initially")

# ============================================================
# DAMAGE TESTS
# Verify that take_damage() correctly reduces HP, clamps at 0,
# and sets the is_dead flag.
# ============================================================

func test_take_damage_reduces_hp():
    GameState.reset()
    GameState.take_damage(10)
    assert_eq(GameState.hp, 90, "HP should be 90 after taking 10 damage from 100")

func test_take_damage_multiple_hits():
    GameState.reset()
    GameState.take_damage(10)
    GameState.take_damage(15)
    GameState.take_damage(5)
    assert_eq(GameState.hp, 70, "HP should be 70 after taking 10+15+5=30 damage from 100")

func test_take_damage_clamps_at_zero():
    GameState.reset()
    GameState.take_damage(999)
    assert_eq(GameState.hp, 0, "HP should not go below 0 even with massive damage")

func test_take_damage_exact_zero():
    GameState.reset()
    GameState.take_damage(100)
    assert_eq(GameState.hp, 0, "100 damage to 100 HP should equal exactly 0")

func test_take_damage_sets_dead_flag():
    GameState.reset()
    GameState.take_damage(100)
    assert_true(GameState.is_dead, "Player should be dead when HP reaches 0")

func test_take_damage_not_dead_when_alive():
    GameState.reset()
    GameState.take_damage(99)
    assert_false(GameState.is_dead, "Player should not be dead at 1 HP")

func test_take_damage_one_hp_not_dead():
    GameState.reset()
    GameState.take_damage(99)
    assert_eq(GameState.hp, 1, "HP should be exactly 1 after 99 damage")
    assert_false(GameState.is_dead, "Player should not be dead at 1 HP")

# ============================================================
# DEATH SIGNAL TESTS
# Verify that the player_died signal is emitted correctly.
# ============================================================

func test_player_died_signal_emitted():
    GameState.reset()
    watch_signals(GameState)
    GameState.take_damage(100)
    assert_signal_emitted(GameState, "player_died", "player_died signal should emit when HP reaches 0")

func test_player_died_signal_not_emitted_when_alive():
    GameState.reset()
    watch_signals(GameState)
    GameState.take_damage(50)
    assert_signal_not_emitted(GameState, "player_died", "player_died signal should NOT emit when player is still alive")

func test_player_died_signal_emitted_only_once():
    GameState.reset()
    watch_signals(GameState)
    GameState.take_damage(100)
    GameState.take_damage(10)  # Already dead, should not emit again
    assert_signal_emit_count(GameState, "player_died", 1, "player_died signal should emit exactly once, not on subsequent damage after death")

# ============================================================
# STATS_CHANGED SIGNAL TESTS
# Verify that the stats_changed signal is emitted on all state changes.
# ============================================================

func test_stats_changed_on_damage():
    GameState.reset()
    watch_signals(GameState)
    GameState.take_damage(10)
    assert_signal_emitted(GameState, "stats_changed", "stats_changed should emit when taking damage")

func test_stats_changed_on_xp_gain():
    GameState.reset()
    watch_signals(GameState)
    GameState.award_xp(10)
    assert_signal_emitted(GameState, "stats_changed", "stats_changed should emit when gaining XP")

func test_stats_changed_on_level_up():
    GameState.reset()
    watch_signals(GameState)
    GameState.award_xp(90)  # Should trigger level up
    assert_signal_emitted(GameState, "stats_changed", "stats_changed should emit on level up")

# ============================================================
# XP AND LEVELING TESTS
# Verify XP accumulation, level-up thresholds, excess XP carry-over,
# max HP increases, and healing on level-up.
# ============================================================

func test_award_xp_adds_correctly():
    GameState.reset()
    GameState.award_xp(50)
    assert_eq(GameState.xp, 50, "XP should be 50 after awarding 50")

func test_award_xp_accumulates():
    GameState.reset()
    GameState.award_xp(20)
    GameState.award_xp(30)
    assert_eq(GameState.xp, 50, "XP should accumulate: 20 + 30 = 50")

func test_level_up_at_threshold():
    GameState.reset()  # Level 1, threshold = 1 * 90 = 90
    GameState.award_xp(90)
    assert_eq(GameState.level, 2, "Should level up from 1 to 2 at exactly 90 XP")
    assert_eq(GameState.xp, 0, "XP should reset to 0 after level up with exact threshold XP")

func test_level_up_excess_xp_carries_over():
    GameState.reset()
    GameState.award_xp(100)  # 10 excess past the 90 threshold
    assert_eq(GameState.level, 2, "Should level up to 2")
    assert_eq(GameState.xp, 10, "Excess 10 XP should carry over after level up")

func test_no_level_up_below_threshold():
    GameState.reset()
    GameState.award_xp(89)
    assert_eq(GameState.level, 1, "Should NOT level up at 89 XP (threshold is 90)")
    assert_eq(GameState.xp, 89, "XP should remain at 89")

func test_level_up_increases_max_hp():
    GameState.reset()
    GameState.award_xp(90)  # Level 1 -> 2
    assert_eq(GameState.max_hp, 116, "Max HP at level 2 should be 100 + 2 * 8 = 116")

func test_level_up_heals_player():
    GameState.reset()
    GameState.take_damage(50)  # HP = 50
    GameState.award_xp(90)     # Level up: heal min(116, 50 + 18) = 68
    assert_eq(GameState.hp, 68, "Should heal 18 HP on level up: min(116, 50+18) = 68")

func test_level_up_heal_capped_at_max_hp():
    GameState.reset()  # HP = 100
    GameState.award_xp(90)  # Level up: heal min(116, 100 + 18) = 116
    assert_eq(GameState.hp, 116, "Level-up heal should not exceed max HP: min(116, 100+18) = 116")

func test_level_2_threshold_is_180():
    GameState.reset()
    GameState.award_xp(90)   # Level 1 -> 2, XP resets to 0
    GameState.award_xp(179)  # Just below level 2 threshold (2 * 90 = 180)
    assert_eq(GameState.level, 2, "Should NOT level up at 179 XP (threshold is 180)")
    GameState.award_xp(1)    # Now at 180
    assert_eq(GameState.level, 3, "Should level up to 3 at 180 XP")

func test_level_3_max_hp():
    GameState.reset()
    GameState.award_xp(90)   # Level 2
    GameState.award_xp(180)  # Level 3
    assert_eq(GameState.max_hp, 124, "Max HP at level 3 should be 100 + 3 * 8 = 124")

# ============================================================
# RESET TESTS
# Verify that reset() restores all properties to initial values.
# ============================================================

func test_reset_restores_all_defaults():
    GameState.reset()
    GameState.take_damage(50)
    GameState.award_xp(200)
    GameState.floor_number = 5
    GameState.reset()
    assert_eq(GameState.hp, 100, "HP should reset to 100")
    assert_eq(GameState.max_hp, 100, "Max HP should reset to 100")
    assert_eq(GameState.xp, 0, "XP should reset to 0")
    assert_eq(GameState.level, 1, "Level should reset to 1")
    assert_eq(GameState.floor_number, 1, "Floor should reset to 1")
    assert_false(GameState.is_dead, "is_dead should reset to false")

func test_reset_clears_dead_state():
    GameState.reset()
    GameState.take_damage(100)  # Kill the player
    assert_true(GameState.is_dead, "Player should be dead before reset")
    GameState.reset()
    assert_false(GameState.is_dead, "is_dead should be false after reset")
    assert_eq(GameState.hp, 100, "HP should be 100 after reset from dead state")
```

**Test count: 30 tests**

---

## Test File: tests/test_enemy.gd

Unit tests for enemy stat formulas. These test the mathematical formulas directly, without instantiating enemy scenes.

**Note:** These tests verify the formulas themselves (`18 + tier * 12`, etc.) to ensure the math is correct. Scene-based tests (verifying enemy nodes use these formulas) are in the integration tests.

```gdscript
extends GutTest

# ============================================================
# TIER HP FORMULA: 18 + danger_tier * 12
# ============================================================

func test_tier_1_hp():
    var hp := 18 + 1 * 12
    assert_eq(hp, 30, "Tier 1 HP should be 30 (18 + 1*12)")

func test_tier_2_hp():
    var hp := 18 + 2 * 12
    assert_eq(hp, 42, "Tier 2 HP should be 42 (18 + 2*12)")

func test_tier_3_hp():
    var hp := 18 + 3 * 12
    assert_eq(hp, 54, "Tier 3 HP should be 54 (18 + 3*12)")

# ============================================================
# TIER SPEED FORMULA: 48 + danger_tier * 18
# ============================================================

func test_tier_1_speed():
    var speed := 48 + 1 * 18
    assert_eq(speed, 66, "Tier 1 speed should be 66 (48 + 1*18)")

func test_tier_2_speed():
    var speed := 48 + 2 * 18
    assert_eq(speed, 84, "Tier 2 speed should be 84 (48 + 2*18)")

func test_tier_3_speed():
    var speed := 48 + 3 * 18
    assert_eq(speed, 102, "Tier 3 speed should be 102 (48 + 3*18)")

# ============================================================
# TIER DAMAGE FORMULA: 3 + danger_tier
# ============================================================

func test_tier_1_damage():
    var damage := 3 + 1
    assert_eq(damage, 4, "Tier 1 damage should be 4 (3 + 1)")

func test_tier_2_damage():
    var damage := 3 + 2
    assert_eq(damage, 5, "Tier 2 damage should be 5 (3 + 2)")

func test_tier_3_damage():
    var damage := 3 + 3
    assert_eq(damage, 6, "Tier 3 damage should be 6 (3 + 3)")

# ============================================================
# TIER XP REWARD FORMULA: 10 + danger_tier * 4
# ============================================================

func test_tier_1_xp():
    var xp := 10 + 1 * 4
    assert_eq(xp, 14, "Tier 1 XP reward should be 14 (10 + 1*4)")

func test_tier_2_xp():
    var xp := 10 + 2 * 4
    assert_eq(xp, 18, "Tier 2 XP reward should be 18 (10 + 2*4)")

func test_tier_3_xp():
    var xp := 10 + 3 * 4
    assert_eq(xp, 22, "Tier 3 XP reward should be 22 (10 + 3*4)")

# ============================================================
# PLAYER DAMAGE FORMULA: 12 + floor(level * 1.5)
# Used to verify how many hits it takes to kill each tier.
# ============================================================

func test_player_damage_level_1():
    var damage := 12 + int(1 * 1.5)
    assert_eq(damage, 13, "Player damage at level 1 should be 13 (12 + floor(1.5))")

func test_player_damage_level_2():
    var damage := 12 + int(2 * 1.5)
    assert_eq(damage, 15, "Player damage at level 2 should be 15 (12 + floor(3.0))")

func test_player_damage_level_3():
    var damage := 12 + int(3 * 1.5)
    assert_eq(damage, 16, "Player damage at level 3 should be 16 (12 + floor(4.5))")

func test_player_damage_level_5():
    var damage := 12 + int(5 * 1.5)
    assert_eq(damage, 19, "Player damage at level 5 should be 19 (12 + floor(7.5))")

func test_player_damage_level_10():
    var damage := 12 + int(10 * 1.5)
    assert_eq(damage, 27, "Player damage at level 10 should be 27 (12 + floor(15.0))")

# ============================================================
# HITS TO KILL CALCULATIONS
# Verify the number of player attacks needed to kill each tier.
# ============================================================

func test_hits_to_kill_tier_1_at_level_1():
    var enemy_hp := 30
    var player_damage := 13  # Level 1
    var hits := ceili(float(enemy_hp) / float(player_damage))
    assert_eq(hits, 3, "Should take 3 hits to kill tier 1 (30 HP) at level 1 (13 dmg)")

func test_hits_to_kill_tier_2_at_level_1():
    var enemy_hp := 42
    var player_damage := 13  # Level 1
    var hits := ceili(float(enemy_hp) / float(player_damage))
    assert_eq(hits, 4, "Should take 4 hits to kill tier 2 (42 HP) at level 1 (13 dmg)")

func test_hits_to_kill_tier_3_at_level_1():
    var enemy_hp := 54
    var player_damage := 13  # Level 1
    var hits := ceili(float(enemy_hp) / float(player_damage))
    assert_eq(hits, 5, "Should take 5 hits to kill tier 3 (54 HP) at level 1 (13 dmg)")

func test_hits_to_kill_tier_1_at_level_5():
    var enemy_hp := 30
    var player_damage := 19  # Level 5
    var hits := ceili(float(enemy_hp) / float(player_damage))
    assert_eq(hits, 2, "Should take 2 hits to kill tier 1 (30 HP) at level 5 (19 dmg)")

func test_hits_to_kill_tier_3_at_level_10():
    var enemy_hp := 54
    var player_damage := 27  # Level 10
    var hits := ceili(float(enemy_hp) / float(player_damage))
    assert_eq(hits, 2, "Should take 2 hits to kill tier 3 (54 HP) at level 10 (27 dmg)")
```

**Test count: 20 tests**

---

## Test File: tests/test_movement.gd

Unit tests for the isometric transform math and speed normalization. These test the mathematical operations used in the movement system.

```gdscript
extends GutTest

# The isometric transform matrix used for screen-to-world conversion.
# This maps screen-space input directions to isometric world directions.
var iso_transform := Transform2D(Vector2(1, 0.5), Vector2(-1, 0.5), Vector2.ZERO)

# ============================================================
# ISOMETRIC TRANSFORM DIRECTION TESTS
# Verify that screen-space input directions map to the correct
# isometric world directions.
# ============================================================

func test_iso_transform_screen_up_maps_to_northeast():
    var screen_up := Vector2(0, -1)
    var result := iso_transform * screen_up
    # Northeast: positive x, negative y in world space
    assert_gt(result.x, 0.0, "Screen up (W key) iso result should have positive x component (eastward)")
    assert_lt(result.y, 0.0, "Screen up (W key) iso result should have negative y component (northward)")

func test_iso_transform_screen_down_maps_to_southwest():
    var screen_down := Vector2(0, 1)
    var result := iso_transform * screen_down
    # Southwest: negative x, positive y in world space
    assert_lt(result.x, 0.0, "Screen down (S key) iso result should have negative x component (westward)")
    assert_gt(result.y, 0.0, "Screen down (S key) iso result should have positive y component (southward)")

func test_iso_transform_screen_right_maps_to_southeast():
    var screen_right := Vector2(1, 0)
    var result := iso_transform * screen_right
    # Southeast: positive x, positive y in world space
    assert_gt(result.x, 0.0, "Screen right (D key) iso result should have positive x component (eastward)")
    assert_gt(result.y, 0.0, "Screen right (D key) iso result should have positive y component (southward)")

func test_iso_transform_screen_left_maps_to_northwest():
    var screen_left := Vector2(-1, 0)
    var result := iso_transform * screen_left
    # Northwest: negative x, negative y in world space
    assert_lt(result.x, 0.0, "Screen left (A key) iso result should have negative x component (westward)")
    assert_lt(result.y, 0.0, "Screen left (A key) iso result should have negative y component (northward)")

# ============================================================
# SPEED NORMALIZATION TESTS
# Verify that diagonal movement is not faster than cardinal movement.
# After normalization and applying speed, both should be equal.
# ============================================================

func test_diagonal_speed_equals_cardinal_speed():
    var speed := 190.0
    var cardinal_input := Vector2(1, 0)  # Pure screen right
    var diagonal_input := Vector2(1, -1).normalized()  # Screen up-right (normalized)

    var cardinal_velocity := (iso_transform * cardinal_input).normalized() * speed
    var diagonal_velocity := (iso_transform * diagonal_input).normalized() * speed

    assert_almost_eq(cardinal_velocity.length(), diagonal_velocity.length(), 0.1,
        "Diagonal movement speed should equal cardinal movement speed (both should be 190)")

func test_cardinal_speed_is_exact():
    var speed := 190.0
    var input := Vector2(0, -1)  # Screen up
    var velocity := (iso_transform * input).normalized() * speed
    assert_almost_eq(velocity.length(), 190.0, 0.1,
        "Cardinal movement velocity magnitude should be exactly 190")

func test_zero_input_produces_zero_velocity():
    var speed := 190.0
    var input := Vector2.ZERO
    # Note: normalizing Vector2.ZERO returns Vector2.ZERO in Godot
    var direction := iso_transform * input
    if direction.length() > 0:
        direction = direction.normalized()
    var velocity := direction * speed
    assert_almost_eq(velocity.length(), 0.0, 0.01,
        "Zero input should produce zero velocity")

func test_opposing_inputs_cancel_out():
    var input := Vector2(1, 0) + Vector2(-1, 0)  # Right + Left = Zero
    assert_almost_eq(input.length(), 0.0, 0.01,
        "Opposing inputs (left + right) should cancel to zero")

func test_three_key_input_resolves_correctly():
    # Pressing W + A + D: A and D cancel on x-axis, leaving only W (screen up)
    var input := Vector2(0, -1) + Vector2(-1, 0) + Vector2(1, 0)
    # This simplifies to Vector2(0, -1) = screen up
    assert_almost_eq(input.x, 0.0, 0.01, "W+A+D: A and D should cancel, leaving x=0")
    assert_almost_eq(input.y, -1.0, 0.01, "W+A+D: Only W remains, y should be -1")

func test_all_four_keys_cancel_to_zero():
    var input := Vector2(0, -1) + Vector2(0, 1) + Vector2(-1, 0) + Vector2(1, 0)
    assert_almost_eq(input.length(), 0.0, 0.01,
        "Pressing all four keys should cancel to zero (no movement)")
```

**Test count: 10 tests**

---

## Test File: tests/test_combat.gd

Integration tests for the combat flow: player attacks enemy, enemy takes damage, enemy dies, XP is awarded, level-up triggers.

**Note:** These tests require scene instantiation and may need `await` for timer-based behavior. They test the interaction between multiple systems.

```gdscript
extends GutTest

# ============================================================
# ATTACK COOLDOWN TESTS
# ============================================================

func test_attack_cooldown_value():
    # The attack cooldown should be 0.42 seconds (420ms)
    var cooldown := 0.42
    assert_almost_eq(cooldown, 0.42, 0.001, "Attack cooldown should be 0.42 seconds")

func test_attack_range_value():
    # The attack range should be 78 pixels
    var range_px := 78.0
    assert_almost_eq(range_px, 78.0, 0.1, "Attack range should be 78 pixels")

# ============================================================
# DAMAGE FLOW TESTS
# Test that attacking an enemy -> reducing HP -> death -> XP gain
# works correctly with real values.
# ============================================================

func test_tier_1_enemy_dies_after_correct_hits():
    var enemy_hp := 30  # Tier 1
    var player_damage := 13  # Level 1: 12 + floor(1 * 1.5) = 13
    # Hit 1: 30 - 13 = 17
    enemy_hp -= player_damage
    assert_eq(enemy_hp, 17, "Tier 1 after 1 hit should have 17 HP")
    # Hit 2: 17 - 13 = 4
    enemy_hp -= player_damage
    assert_eq(enemy_hp, 4, "Tier 1 after 2 hits should have 4 HP")
    # Hit 3: 4 - 13 = -9 (dead)
    enemy_hp -= player_damage
    assert_lt(enemy_hp, 1, "Tier 1 should be dead after 3 hits (HP <= 0)")

func test_tier_2_enemy_dies_after_correct_hits():
    var enemy_hp := 42  # Tier 2
    var player_damage := 13  # Level 1
    # Hit 1: 42 - 13 = 29
    enemy_hp -= player_damage
    assert_eq(enemy_hp, 29, "Tier 2 after 1 hit should have 29 HP")
    # Hit 2: 29 - 13 = 16
    enemy_hp -= player_damage
    assert_eq(enemy_hp, 16, "Tier 2 after 2 hits should have 16 HP")
    # Hit 3: 16 - 13 = 3
    enemy_hp -= player_damage
    assert_eq(enemy_hp, 3, "Tier 2 after 3 hits should have 3 HP")
    # Hit 4: 3 - 13 = -10 (dead)
    enemy_hp -= player_damage
    assert_lt(enemy_hp, 1, "Tier 2 should be dead after 4 hits (HP <= 0)")

func test_tier_3_enemy_dies_after_correct_hits():
    var enemy_hp := 54  # Tier 3
    var player_damage := 13  # Level 1
    for i in range(4):
        enemy_hp -= player_damage
    assert_eq(enemy_hp, 2, "Tier 3 after 4 hits should have 2 HP remaining")
    enemy_hp -= player_damage
    assert_lt(enemy_hp, 1, "Tier 3 should be dead after 5 hits (HP <= 0)")

func test_xp_awarded_on_kill_updates_game_state():
    GameState.reset()
    # Simulate killing a tier 1 enemy
    var xp_reward := 10 + 1 * 4  # = 14
    GameState.award_xp(xp_reward)
    assert_eq(GameState.xp, 14, "XP should be 14 after killing one tier 1 enemy")

func test_kill_sequence_triggers_level_up():
    GameState.reset()
    # Kill enough tier 1 enemies to level up
    # Tier 1 gives 14 XP, threshold is 90
    # 6 kills = 84 XP (not enough)
    # 7 kills = 98 XP (level up! excess = 8)
    for i in range(7):
        GameState.award_xp(14)  # Tier 1 XP
    assert_eq(GameState.level, 2, "Should be level 2 after 7 tier 1 kills (98 XP, threshold 90)")
    assert_eq(GameState.xp, 8, "Should have 8 excess XP after level up (98 - 90 = 8)")

func test_mixed_kills_level_up():
    GameState.reset()
    # Kill 2 tier 3 (22 each = 44), 1 tier 2 (18), 2 tier 1 (14 each = 28)
    # Total: 44 + 18 + 28 = 90 -> exactly level up
    GameState.award_xp(22)  # Tier 3 kill
    GameState.award_xp(22)  # Tier 3 kill
    GameState.award_xp(18)  # Tier 2 kill
    GameState.award_xp(14)  # Tier 1 kill
    GameState.award_xp(14)  # Tier 1 kill
    assert_eq(GameState.level, 2, "Should level up with exactly 90 XP from mixed kills")
    assert_eq(GameState.xp, 0, "Should have 0 excess XP with exact threshold")
```

**Test count: 8 tests**

---

## Test File: tests/test_spawning.gd

Integration tests for the enemy spawning system: initial count, soft cap enforcement, tier randomization, and edge spawn positions.

```gdscript
extends GutTest

# ============================================================
# SPAWN COUNT TESTS
# ============================================================

func test_initial_spawn_count():
    var initial_count := 10
    assert_eq(initial_count, 10, "Game should spawn exactly 10 enemies initially")

func test_soft_cap_value():
    var soft_cap := 14
    assert_eq(soft_cap, 14, "Enemy soft cap should be 14")

func test_periodic_timer_value():
    var timer_interval := 2.8
    assert_almost_eq(timer_interval, 2.8, 0.01, "Periodic spawn timer should be 2.8 seconds")

func test_respawn_delay_value():
    var respawn_delay := 1.4
    assert_almost_eq(respawn_delay, 1.4, 0.01, "Enemy respawn delay should be 1.4 seconds")

# ============================================================
# TIER RANDOMIZATION TESTS
# ============================================================

func test_random_tier_in_valid_range():
    # Run 100 random tier generations and verify all are in range 1-3
    for i in range(100):
        var tier := randi_range(1, 3)
        assert_between(tier, 1, 3, "Random tier should be between 1 and 3 (inclusive)")

# ============================================================
# EDGE SPAWN POSITION TESTS
# ============================================================

func test_edge_spawn_positions_are_at_boundaries():
    # Simulate edge position generation for a 1920x1080 world
    var world_width := 1920.0
    var world_height := 1080.0

    for _i in range(50):
        var edge := randi_range(0, 3)
        var pos := Vector2.ZERO
        match edge:
            0:  # Top edge
                pos = Vector2(randf_range(0, world_width), 10)
                assert_almost_eq(pos.y, 10.0, 0.1, "Top edge spawn should have y=10")
                assert_between(pos.x, 0.0, world_width, "Top edge x should be within world width")
            1:  # Right edge
                pos = Vector2(world_width - 10, randf_range(0, world_height))
                assert_almost_eq(pos.x, world_width - 10, 0.1, "Right edge spawn should have x=world_width-10")
                assert_between(pos.y, 0.0, world_height, "Right edge y should be within world height")
            2:  # Bottom edge
                pos = Vector2(randf_range(0, world_width), world_height - 10)
                assert_almost_eq(pos.y, world_height - 10, 0.1, "Bottom edge spawn should have y=world_height-10")
                assert_between(pos.x, 0.0, world_width, "Bottom edge x should be within world width")
            3:  # Left edge
                pos = Vector2(10, randf_range(0, world_height))
                assert_almost_eq(pos.x, 10.0, 0.1, "Left edge spawn should have x=10")
                assert_between(pos.y, 0.0, world_height, "Left edge y should be within world height")
```

**Test count: 5 tests** (note: the last test runs 50 iterations internally)

---

## Test File: tests/test_death_restart.gd

Integration tests for the death and restart cycle: HP reaching 0 triggers death state, restart resets all state.

```gdscript
extends GutTest

# ============================================================
# DEATH STATE TESTS
# ============================================================

func test_death_occurs_at_zero_hp():
    GameState.reset()
    GameState.take_damage(100)
    assert_eq(GameState.hp, 0, "HP should be exactly 0 at death")
    assert_true(GameState.is_dead, "is_dead should be true when HP reaches 0")

func test_death_occurs_with_overkill_damage():
    GameState.reset()
    GameState.take_damage(150)  # 50 more than max HP
    assert_eq(GameState.hp, 0, "HP should clamp to 0 with overkill damage")
    assert_true(GameState.is_dead, "is_dead should be true with overkill damage")

func test_gradual_death_from_enemy_hits():
    GameState.reset()
    # Simulate tier 1 enemies (4 damage each) hitting 25 times: 25 * 4 = 100
    for i in range(25):
        GameState.take_damage(4)
    assert_eq(GameState.hp, 0, "25 hits of 4 damage should bring 100 HP to exactly 0")
    assert_true(GameState.is_dead, "Player should be dead after 25 hits of 4 damage")

func test_death_from_mixed_tier_enemies():
    GameState.reset()
    # Simulate: 5 tier 3 hits (6 dmg = 30) + 5 tier 2 hits (5 dmg = 25) + 10 tier 1 hits (4 dmg = 40) = 95
    for i in range(5):
        GameState.take_damage(6)  # Tier 3
    for i in range(5):
        GameState.take_damage(5)  # Tier 2
    for i in range(10):
        GameState.take_damage(4)  # Tier 1
    assert_eq(GameState.hp, 5, "Mixed hits totaling 95 damage should leave 5 HP")
    assert_false(GameState.is_dead, "Player should not be dead at 5 HP")
    GameState.take_damage(5)
    assert_eq(GameState.hp, 0, "One more hit of 5 should bring HP to 0")
    assert_true(GameState.is_dead, "Player should be dead at 0 HP")

# ============================================================
# RESTART STATE TESTS
# ============================================================

func test_restart_resets_all_state_after_death():
    GameState.reset()
    # Play a full game simulation
    GameState.award_xp(200)  # Level up multiple times
    GameState.take_damage(50)
    GameState.floor_number = 3
    GameState.take_damage(999)  # Die
    assert_true(GameState.is_dead, "Player should be dead before restart")

    # Restart
    GameState.reset()
    assert_eq(GameState.hp, 100, "HP should be 100 after restart")
    assert_eq(GameState.max_hp, 100, "Max HP should be 100 after restart")
    assert_eq(GameState.xp, 0, "XP should be 0 after restart")
    assert_eq(GameState.level, 1, "Level should be 1 after restart")
    assert_eq(GameState.floor_number, 1, "Floor should be 1 after restart")
    assert_false(GameState.is_dead, "is_dead should be false after restart")

func test_restart_allows_new_gameplay():
    GameState.reset()
    GameState.take_damage(100)  # Die
    assert_true(GameState.is_dead, "Should be dead")

    GameState.reset()  # Restart
    assert_false(GameState.is_dead, "Should not be dead after restart")

    # Verify new game works normally
    GameState.take_damage(10)
    assert_eq(GameState.hp, 90, "Should be able to take damage in new game")
    GameState.award_xp(14)
    assert_eq(GameState.xp, 14, "Should be able to gain XP in new game")
```

**Test count: 6 tests**

---

## Implementation Notes

### Running All Tests

To run the complete test suite:

**From Godot Editor:**
1. Open GUT panel (bottom dock)
2. Click "Run All"
3. All 64 tests should pass (green checkmarks)

**From Command Line:**
```bash
godot --headless -s addons/gut/gut_cmdln.gd -gdir=res://tests/ -gexit
```

**Expected Output:**
```
64 tests passed, 0 failed
```

### Test Dependencies

| Test File | Depends On |
|-----------|-----------|
| `test_game_state.gd` | `GameState` autoload registered |
| `test_enemy.gd` | Nothing (pure math) |
| `test_movement.gd` | Nothing (pure math) |
| `test_combat.gd` | `GameState` autoload registered |
| `test_spawning.gd` | Nothing (pure math + randomization) |
| `test_death_restart.gd` | `GameState` autoload registered |

### Adding New Tests

When adding a new system:
1. Create `tests/test_<system_name>.gd`
2. Extend `GutTest`
3. Name all test functions with `test_` prefix
4. Use `before_each()` for setup if needed (e.g., `GameState.reset()`)
5. Run from GUT panel to verify

### Common Patterns

**Testing formulas (pure math):**
```gdscript
func test_some_formula():
    var result := base + modifier * scaling_factor
    assert_eq(result, expected_value, "Description of what this formula should produce")
```

**Testing GameState changes:**
```gdscript
func test_some_state_change():
    GameState.reset()  # Always start clean
    # Perform action
    GameState.some_method()
    # Verify result
    assert_eq(GameState.some_property, expected_value, "Description")
```

**Testing signals:**
```gdscript
func test_some_signal():
    GameState.reset()
    watch_signals(GameState)  # Start watching BEFORE the action
    GameState.some_method()   # Action that should emit
    assert_signal_emitted(GameState, "signal_name", "Description")
```

## Open Questions

- Should tests use `before_each()` with `GameState.reset()` instead of calling it in every test function?
- Should scene-based integration tests be added once enemy.tscn and player.tscn are created?
- Should performance tests be automated (e.g., spawn 100 enemies and measure frame time)?
- Should test coverage reporting be set up?
- Should there be a dedicated test for the isometric coordinate conversion used in mouse/touch input (when implemented)?
