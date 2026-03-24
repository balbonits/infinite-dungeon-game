# Test Strategy

## Summary

Testing approach for "A Dungeon in the Middle of Nowhere" during the Godot 4 migration. Combines manual playtesting, unit tests, and integration tests to catch regressions and verify that game feel matches the Phaser 3 prototype.

## Current State

- No automated tests exist yet (project is migrating to Godot 4)
- Manual playtesting has been the primary QA method during Phaser prototype development
- GUT (Godot Unit Test) framework selected for automated testing
- Test cases documented in `docs/testing/manual-tests.md` and `docs/testing/automated-tests.md`

## Design

### Testing Philosophy

This is a learning project. Testing should:
- **Catch regressions early** -- when adding new systems, ensure existing systems still work
- **Verify game feel matches the Phaser prototype** -- movement speed, attack timing, enemy behavior should feel the same
- **Be quick to run** -- tests should not be a barrier to rapid iteration
- **Teach game testing patterns** -- learn how to test game logic vs. visual output vs. game feel
- **Focus on logic over visuals** -- automated tests verify math and state; manual tests verify visuals and feel

### Testing Layers

| Layer | Tool | Purpose | When to Run | Duration Target |
|-------|------|---------|-------------|-----------------|
| Manual playtest | Godot editor (F5) | Verify game feel, visual correctness, fun factor | Every change | 2-5 minutes |
| Unit tests | GUT framework | Verify formulas, state logic, isolated behavior | After each system is implemented | < 5 seconds |
| Integration tests | GUT framework | Verify systems work together (e.g., attack -> XP -> level up) | After milestones or cross-system changes | < 10 seconds |
| Performance test | Godot profiler | Verify frame rate with max enemies on screen | Periodically, and before any release | Manual (5 minutes) |

### What to Test Per System

| System | Unit Tests | Integration Tests | Manual Tests |
|--------|-----------|-------------------|--------------|
| GameState | Property defaults, damage clamping, XP/level math, signal emission | N/A (pure logic autoload) | Debugger inspection of values |
| Movement | Isometric transform math, normalized vector length | Move + wall collision response | WASD feel, diagonal speed consistency, camera follow |
| Combat | Damage formulas, cooldown timing, range detection | Player attacks enemy -> XP awarded -> level up triggers | Attack feel, slash visual effect, target selection |
| Enemies | Tier stat calculation (HP/speed/damage/XP), color assignment | Chase + contact damage + death flow | Enemy colors match tiers, chase feels right, no stuck enemies |
| Spawning | N/A (timer-based, hard to unit test) | Initial count = 10, cap = 14 enforced, respawn after death | Visual count, spawn locations at edges, no clustering |
| HUD | N/A (display only, no logic) | `GameState.stats_changed` signal -> HUD label updates correctly | Readability, positioning, colors match spec |
| Death/Restart | N/A (UI flow) | HP reaches 0 -> death screen appears -> R key restarts -> clean state | Screen appearance, overlay visibility, R key responsiveness |

### GUT Framework

**What is GUT?**
- Godot Unit Test: a testing framework for Godot 4
- GitHub: https://github.com/bitwes/Gut
- Provides `assert_eq`, `assert_true`, `assert_signal_emitted`, and many other assertions
- Tests run inside the Godot editor or headless from command line

**Installation:**
1. **Via AssetLib:** Godot editor -> AssetLib tab -> search "GUT" -> install -> enable plugin
2. **Via Git:** `git clone https://github.com/bitwes/Gut.git` into `addons/gut/`
3. Enable in Project Settings -> Plugins -> GUT -> Active

**Running Tests:**
- **Editor:** GUT panel (bottom dock) -> "Run All" button
- **Command line:** `godot --headless -s addons/gut/gut_cmdln.gd -gdir=res://tests/ -gexit`
- **Specific file:** `godot --headless -s addons/gut/gut_cmdln.gd -gtest=res://tests/test_game_state.gd -gexit`

**Writing Tests:**
```gdscript
extends GutTest

func test_example():
    assert_eq(1 + 1, 2, "Basic math should work")
```

- Test files must start with `test_` prefix
- Test functions must start with `test_` prefix
- Tests extend `GutTest` (not `Node` or `SceneTree`)
- Use `before_each()` and `after_each()` for setup/teardown

### Test File Organization

```
tests/
├── test_game_state.gd      -- Unit tests for GameState autoload
│                               (HP, XP, leveling, damage, reset, signals)
├── test_enemy.gd            -- Unit tests for enemy stat formulas
│                               (tier HP, speed, damage, XP values)
├── test_player.gd           -- Unit tests for player stats and movement math
│                               (damage formula, iso transform, speed normalization)
├── test_spawning.gd         -- Integration tests for spawn system
│                               (initial count, cap enforcement, respawn timing)
├── test_combat.gd           -- Integration tests for combat flow
│                               (attack -> damage -> death -> XP -> level up)
└── test_death_restart.gd    -- Integration tests for death/restart cycle
                                (HP=0 -> death state -> reset -> clean state)
```

### Testing Workflow

For each system implemented during the Godot migration:

1. **Implement the system** -- write the GDScript code, create scenes
2. **Playtest manually (F5)** -- run the game, verify it looks and feels right
3. **Write unit tests** for the system's logic (formulas, state changes, signal emissions)
4. **Write integration tests** for cross-system behavior (e.g., killing an enemy awards XP and triggers level-up)
5. **Run all tests** -- verify no regressions from the new system
6. **Document behavior differences** from the Phaser prototype (if any)

### Performance Testing

Performance testing is manual, using Godot's built-in profiler:

1. Open Godot editor -> Debugger -> Profiler tab
2. Run the game (F5)
3. Spawn maximum enemies (14 at soft cap + any respawning)
4. Verify:
   - Frame rate stays at 60 FPS (or target refresh rate)
   - Physics process time stays under 16ms
   - No memory leaks (monitor RSS over time)
   - No GC stalls visible in frame time graph

**Performance targets:**

| Metric | Target | Acceptable | Investigate |
|--------|--------|------------|-------------|
| FPS | 60 | 55+ | < 50 |
| Physics process | < 2ms | < 5ms | > 8ms |
| Enemy count at 60 FPS | 14 (soft cap) | 20+ | N/A |
| Memory growth per minute | < 1 MB | < 5 MB | > 10 MB |

### Regression Testing

When a bug is found:
1. Write a failing test that reproduces the bug (if possible)
2. Fix the bug
3. Verify the test passes
4. The test now prevents the bug from returning

When behavior changes are intentional:
1. Update the relevant test to expect the new behavior
2. Document why the change was made in a comment

## Implementation Notes

- GUT tests run in an isolated scene tree, which means `GameState` autoload must be accessible
- For integration tests that need scene instances (e.g., enemy scene), use `add_child_autofree()` to automatically clean up after each test
- Timer-based tests may need `await get_tree().create_timer(0.1).timeout` to advance time; use sparingly as they slow down test execution
- Camera shake and visual effects are not tested automatically -- rely on manual testing for these

## Open Questions

- Should CI/CD run tests automatically on push? (GitHub Actions with headless Godot)
- Should visual regression testing be added (screenshot comparison)?
- At what point should performance benchmarks be formalized (beyond manual profiler checks)?
- Should test coverage targets be set, or is that overkill for a learning project?
- Should playtesting sessions be logged/documented for tracking game feel evolution?
