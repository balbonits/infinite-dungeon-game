# Test Strategy

## Summary

Testing approach for "A Dungeon in the Middle of Nowhere" using Godot 4 + C#. Combines manual playtesting, unit tests (xUnit), and integration/scene tests (GdUnit4) to catch regressions and verify that game feel matches the Phaser 3 prototype.

## Current State

- **219 unit tests passing** (51 legacy + 168 entity framework)
- **GdUnit4** selected for Godot scene/node testing (NuGet: `gdUnit4.api`)
- **xUnit** selected for pure C# logic testing (stat formulas, XP curves, data structures)
- Test cases documented in `docs/testing/manual-tests.md` and `docs/testing/automated-tests.md`

## Design

### Testing Philosophy

This is a learning project. Testing should:
- **Catch regressions early** -- when adding new systems, ensure existing systems still work
- **Verify game feel matches the Phaser prototype** -- movement speed, attack timing, enemy behavior should feel the same
- **Be quick to run** -- tests should not be a barrier to rapid iteration
- **Teach game testing patterns** -- learn how to test game logic vs. visual output vs. game feel
- **Focus on logic over visuals** -- automated tests verify math and state; manual tests verify visuals and feel

### Dual Framework Approach

**Why two frameworks?**

| Framework | Use Case | Godot Runtime? | Speed |
|-----------|----------|----------------|-------|
| xUnit | Pure C# logic (formulas, state, data) | No — runs without Godot | Very fast (~ms per test) |
| GdUnit4 | Scene tests, node interaction, signals | Yes — needs Godot runtime | Slower (~100ms per test) |

Most game logic (stat formulas, XP curves, damage calculations, level-up math) is pure C# — no Godot nodes needed. Test these with xUnit for maximum speed. Scene-dependent behavior (signal flow, node interactions, timer-based spawning) needs GdUnit4's `[RequireGodotRuntime]`.

### Testing Layers

| Layer | Tool | Purpose | When to Run | Duration Target |
|-------|------|---------|-------------|-----------------|
| Manual playtest | Godot editor (F5) | Verify game feel, visual correctness, fun factor | Every change | 2-5 minutes |
| Unit tests | xUnit | Verify formulas, state logic, isolated behavior | After each system is implemented | < 2 seconds |
| Integration tests | GdUnit4 | Verify systems work together (e.g., attack -> XP -> level up) | After milestones or cross-system changes | < 10 seconds |
| Performance test | Godot profiler | Verify frame rate with max enemies on screen | Periodically, and before any release | Manual (5 minutes) |

### What to Test Per System

| System | Unit Tests (xUnit) | Integration Tests (GdUnit4) | Manual Tests |
|--------|-----------|-------------------|--------------|
| GameState | Property defaults, damage clamping, XP/level math | Signal emission verification | Debugger inspection of values |
| Movement | Isometric transform math, normalized vector length | Move + wall collision response | WASD feel, diagonal speed consistency, camera follow |
| Combat | Damage formulas, cooldown timing, range detection | Player attacks enemy -> XP awarded -> level up triggers | Attack feel, slash visual effect, target selection |
| Enemies | Tier stat calculation (HP/speed/damage/XP), color assignment | Chase + contact damage + death flow | Enemy colors match tiers, chase feels right, no stuck enemies |
| Spawning | N/A (timer-based) | Initial count = 10, cap = 14 enforced, respawn after death | Visual count, spawn locations at edges, no clustering |
| HUD | N/A (display only) | `StatsChanged` signal -> HUD label updates correctly | Readability, positioning, colors match spec |
| Death/Restart | N/A (UI flow) | HP reaches 0 -> death screen appears -> R key restarts -> clean state | Screen appearance, overlay visibility, R key responsiveness |

### GdUnit4 Framework

**What is GdUnit4?**
- Testing framework for Godot 4 with first-class C# support
- GitHub: https://github.com/godot-gdunit-labs/gdUnit4Net
- Provides `AssertThat()`, `AssertSignal()`, scene runner, and many assertions
- Tests run via `dotnet test` (headless) or in VS Code Test Explorer
- v5.0+ runs tests **without Godot runtime by default** — add `[RequireGodotRuntime]` only when needed

**NuGet packages:**
```xml
<PackageReference Include="gdUnit4.api" Version="5.1.0" />
<PackageReference Include="gdUnit4.test.adapter" Version="3.0.0" />
<PackageReference Include="gdUnit4.analyzers" Version="1.0.0" />
```

**Writing GdUnit4 tests:**
```csharp
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public partial class GameStateTests
{
    [TestCase]
    public void TestInitialHp()
    {
        var state = new GameState();
        state.Reset();
        AssertThat(state.Hp).IsEqual(100);
    }

    [TestCase]
    [RequireGodotRuntime] // Only when test needs Godot nodes/scenes
    public void TestSignalEmission()
    {
        // Scene runner tests that need the Godot engine
    }
}
```

### xUnit Framework

**For pure logic tests** that don't touch Godot nodes:

```csharp
using Xunit;

public class DamageFormulaTests
{
    [Fact]
    public void TestBaseDamageAtLevel1()
    {
        int damage = 12 + (int)(1 * 1.5);
        Assert.Equal(13, damage);
    }

    [Theory]
    [InlineData(1, 90)]
    [InlineData(2, 180)]
    [InlineData(10, 900)]
    public void TestXpThreshold(int level, int expectedXp)
    {
        int threshold = level * 90;
        Assert.Equal(expectedXp, threshold);
    }
}
```

### Test File Organization

```
tests/
├── DungeonGame.Tests.csproj     -- Test project (references main .csproj)
├── CombatTests.cs               -- Combat formula tests (legacy, 51 total legacy tests)
├── DungeonTests.cs              -- Dungeon generation tests (legacy)
├── InventoryTests.cs            -- Inventory system tests (legacy)
├── LevelingTests.cs             -- XP/leveling tests (legacy)
├── ShopTests.cs                 -- Shop system tests (legacy)
├── EntityFactoryTests.cs        -- Entity creation and type validation
├── VitalSystemTests.cs          -- HP/MP, damage, healing, death edge cases
├── StatSystemTests.cs           -- Stat calculation, modifiers, scaling
├── CombatSystemTests.cs         -- Attack resolution, defense, damage formulas
└── EffectSystemTests.cs         -- Buff/debuff application, ticking, expiry
```

### Testing Workflow

For each system implemented:

1. **Write unit tests first** (xUnit) for pure logic (formulas, state changes)
2. **Write integration tests** (GdUnit4) for cross-system behavior
3. **Run `dotnet test`** to verify tests pass
4. **Implement the system** -- write the C# code, create scenes
5. **Playtest manually (F5)** -- run the game, verify it looks and feels right
6. **Run all tests again** -- verify no regressions
7. **Document behavior differences** from the Phaser prototype (if any)

### Running Tests

```bash
# All tests (xUnit + GdUnit4)
make test
# or: dotnet test

# With coverage
make coverage
# or: dotnet test --collect:"XPlat Code Coverage"

# Specific test class
dotnet test --filter "FullyQualifiedName~GameStateTests"

# Watch mode (auto-run on file change)
make watch
# or: dotnet watch test
```

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
| Floor generation time | < 100ms | < 500ms | > 1s |

### Debug Overlay

A toggleable in-game debug panel (F3 key) showing:
- FPS counter
- Active entity count (enemies, effects)
- Floor generation time (ms)
- Memory usage (MB)
- Object pool stats (rented/returned)

Built with Godot Control nodes in a CanvasLayer. No external dependency. Disabled in release builds via `#if DEBUG`.

### Regression Testing

When a bug is found:
1. Write a failing test that reproduces the bug (if possible)
2. Fix the bug
3. Verify the test passes
4. The test now prevents the bug from returning

When behavior changes are intentional:
1. Update the relevant test to expect the new behavior
2. Document why the change was made in the commit message

### Test Coverage

Coverage reports generated via `coverlet` + `ReportGenerator`:

```bash
make coverage    # Generate HTML coverage report in coverage/
```

No hard coverage targets — this is a learning project. Coverage is a tool for finding untested paths, not a metric to optimize for.

### Entity Framework Tests

168 new xUnit tests across 5 test files covering the unified entity mechanics framework. These tests validate the core gameplay logic that all entities (players, monsters, NPCs) share.

| Test File | Count | Covers |
|-----------|-------|--------|
| `EntityFactoryTests.cs` | Entity creation | Factory methods, type assignment, default values, entity type validation |
| `VitalSystemTests.cs` | HP/MP operations | Damage clamping, healing caps, death triggers, overkill, zero-damage edge cases |
| `StatSystemTests.cs` | Stat calculations | Base stats, modifier stacking, level scaling, stat floor/ceiling bounds |
| `CombatSystemTests.cs` | Attack resolution | Damage formulas, defense reduction, critical hits, attack-against-dead targets |
| `EffectSystemTests.cs` | Buff/debuff logic | Effect application, tick processing, expiry, stacking rules, removal |

**Test categories covered:**
- **Boundary conditions** -- min/max values, zero inputs, negative inputs, overflow scenarios
- **Edge cases** -- dead entity interactions, empty effect lists, duplicate effects
- **Stress tests** -- rapid sequential operations, many simultaneous effects, multi-level-up chains
- **Symmetry proofs** -- damage then heal returns to original, apply then remove effect is clean
- **Regression guards** -- specific bug scenarios that were caught and prevented from recurring

**Total test count:** 219 (51 legacy game logic tests + 168 entity framework tests).

## Implementation Notes

- GdUnit4 tests with `[RequireGodotRuntime]` run inside a Godot scene tree, so autoloads are accessible
- For integration tests that need scene instances, use GdUnit4's scene runner API
- Timer-based tests may need `await` with `ToSignal()` to advance time; use sparingly
- Camera shake and visual effects are not tested automatically -- rely on manual testing
- xUnit tests run without Godot entirely — fast, parallelizable, CI-friendly

## Resolved Questions

- **CI/CD:** Yes — GitHub Actions runs `dotnet build` + `dotnet format --verify-no-changes` + `dotnet test` on every push/PR to main
- **Coverage:** Added via coverlet. No hard targets, used as a diagnostic tool.
- **Debug overlay:** Specced as F3-toggleable panel with FPS, entity count, gen time, memory.
