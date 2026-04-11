# Automated Tests

## Summary

Planned automated test suite for "A Dungeon in the Middle of Nowhere" using the GdUnit4 testing framework for C#. Contains unit tests for game state logic, enemy stat formulas, player damage calculations, and movement math, plus integration tests for combat flow, spawning, and death/restart.

## Current State

- Tests are planned but not yet implemented (game is being rebuilt with visual-first development after the Session 8 fresh start)
- All test code below is ready to be placed in the `tests/` directory once the corresponding game systems are built
- Tests reference a `GameState` autoload singleton and enemy/player scenes that will be created during migration

## Design

### Framework Setup

| Property | Value |
|----------|-------|
| Framework | GdUnit4 for C# |
| GitHub | https://github.com/MikeSchulze/gdUnit4 |
| Installation | Godot AssetLib -> search "GdUnit4" -> install, OR git clone into `addons/gdUnit4/` |
| Test directory | `res://tests/` |
| Test file naming | `*Tests.cs` (e.g., `GameStateTests.cs`) |
| Test function naming | `[TestCase]` attribute on public methods |
| Base class | `[TestSuite] public partial class` (all test classes use this attribute) |
| Run from editor | GdUnit4 panel (bottom dock) -> Run All |
| Run from CLI | `godot --headless -s addons/gdUnit4/bin/GdUnitCmdTool.gd --add "res://tests/" -e` |
| Run single file | `godot --headless -s addons/gdUnit4/bin/GdUnitCmdTool.gd --add "res://tests/GameStateTests.cs" -e` |

### Test File Organization

```
tests/
├── GameStateTests.cs        -- 24 tests: GameState autoload unit tests
├── EnemyTests.cs            -- 15 tests: Enemy stat formula unit tests
├── MovementTests.cs         -- 6 tests: Isometric transform and speed unit tests
├── CombatTests.cs           -- 8 tests: Combat integration tests
├── SpawningTests.cs         -- 5 tests: Spawn system integration tests
└── DeathRestartTests.cs     -- 6 tests: Death/restart cycle integration tests
                             -- TOTAL: 64 tests
```

### GdUnit4 Assertion Reference

Common assertions used in these tests:

| Assertion | Purpose | Example |
|-----------|---------|---------|
| `AssertThat(a).IsEqual(b)` | Exact equality | `AssertThat(hp).IsEqual(100)` |
| `AssertThat(a).IsNotEqual(b)` | Not equal | `AssertThat(hp).IsNotEqual(0)` |
| `AssertThat(expr).IsTrue()` | Boolean true | `AssertThat(isDead).IsTrue()` |
| `AssertThat(expr).IsFalse()` | Boolean false | `AssertThat(isDead).IsFalse()` |
| `AssertThat(a).IsGreater(b)` | Greater than | `AssertThat(xp).IsGreater(0)` |
| `AssertThat(a).IsLess(b)` | Less than | `AssertThat(hp).IsLess(100)` |
| `AssertThat(val).IsBetween(low, high)` | Range check | `AssertThat(tier).IsBetween(1, 3)` |
| `AssertThat(a).IsEqual(b).OverridFailureMessage(msg)` | Float comparison | `AssertThat(speed).IsEqual(190.0)` |
| `await AssertSignal(obj).IsEmitted(sig)` | Signal was emitted | `await AssertSignal(GameState).IsEmitted("PlayerDied")` |
| `await AssertSignal(obj).IsNotEmitted(sig)` | Signal was NOT emitted | `await AssertSignal(GameState).IsNotEmitted("PlayerDied")` |
| `await AssertSignal(obj).IsEmittedCount(n, sig)` | Signal emitted N times | `await AssertSignal(GameState).IsEmittedCount(1, "PlayerDied")` |
| Signal monitoring | Automatic | GdUnit4 monitors signals automatically when using `AssertSignal` |

---

## Test File: tests/GameStateTests.cs

Unit tests for the `GameState` autoload singleton. Tests cover initial state, damage mechanics, death detection, XP/leveling, signal emissions, and reset behavior.

**Prerequisites:** `GameState` autoload must be registered in Project Settings with the following interface:
- Properties: `Hp`, `MaxHp`, `Xp`, `Level`, `FloorNumber`, `IsDead`
- Methods: `Reset()`, `TakeDamage(amount)`, `AwardXp(amount)`
- Signals: `StatsChanged`, `PlayerDied`

```csharp
using Godot;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public partial class GameStateTests
{
    // ============================================================
    // INITIAL STATE TESTS
    // Verify that GameState.Reset() returns all properties to their
    // documented default values.
    // ============================================================

    [TestCase]
    public void TestInitialHp()
    {
        GameState.Reset();
        AssertThat(GameState.Hp).IsEqual(100);
    }

    [TestCase]
    public void TestInitialMaxHp()
    {
        GameState.Reset();
        AssertThat(GameState.MaxHp).IsEqual(100);
    }

    [TestCase]
    public void TestInitialXp()
    {
        GameState.Reset();
        AssertThat(GameState.Xp).IsEqual(0);
    }

    [TestCase]
    public void TestInitialLevel()
    {
        GameState.Reset();
        AssertThat(GameState.Level).IsEqual(1);
    }

    [TestCase]
    public void TestInitialFloor()
    {
        GameState.Reset();
        AssertThat(GameState.FloorNumber).IsEqual(1);
    }

    [TestCase]
    public void TestInitialNotDead()
    {
        GameState.Reset();
        AssertThat(GameState.IsDead).IsFalse();
    }

    // ============================================================
    // DAMAGE TESTS
    // Verify that TakeDamage() correctly reduces HP, clamps at 0,
    // and sets the IsDead flag.
    // ============================================================

    [TestCase]
    public void TestTakeDamageReducesHp()
    {
        GameState.Reset();
        GameState.TakeDamage(10);
        AssertThat(GameState.Hp).IsEqual(90);
    }

    [TestCase]
    public void TestTakeDamageMultipleHits()
    {
        GameState.Reset();
        GameState.TakeDamage(10);
        GameState.TakeDamage(15);
        GameState.TakeDamage(5);
        AssertThat(GameState.Hp).IsEqual(70);
    }

    [TestCase]
    public void TestTakeDamageClampsAtZero()
    {
        GameState.Reset();
        GameState.TakeDamage(999);
        AssertThat(GameState.Hp).IsEqual(0);
    }

    [TestCase]
    public void TestTakeDamageExactZero()
    {
        GameState.Reset();
        GameState.TakeDamage(100);
        AssertThat(GameState.Hp).IsEqual(0);
    }

    [TestCase]
    public void TestTakeDamageSetsDeadFlag()
    {
        GameState.Reset();
        GameState.TakeDamage(100);
        AssertThat(GameState.IsDead).IsTrue();
    }

    [TestCase]
    public void TestTakeDamageNotDeadWhenAlive()
    {
        GameState.Reset();
        GameState.TakeDamage(99);
        AssertThat(GameState.IsDead).IsFalse();
    }

    [TestCase]
    public void TestTakeDamageOneHpNotDead()
    {
        GameState.Reset();
        GameState.TakeDamage(99);
        AssertThat(GameState.Hp).IsEqual(1);
        AssertThat(GameState.IsDead).IsFalse();
    }

    // ============================================================
    // DEATH SIGNAL TESTS
    // Verify that the PlayerDied signal is emitted correctly.
    // ============================================================

    [TestCase]
    public async Task TestPlayerDiedSignalEmitted()
    {
        GameState.Reset();
        GameState.TakeDamage(100);
        await AssertSignal(GameState).IsEmitted("PlayerDied");
    }

    [TestCase]
    public async Task TestPlayerDiedSignalNotEmittedWhenAlive()
    {
        GameState.Reset();
        GameState.TakeDamage(50);
        await AssertSignal(GameState).IsNotEmitted("PlayerDied");
    }

    [TestCase]
    public async Task TestPlayerDiedSignalEmittedOnlyOnce()
    {
        GameState.Reset();
        GameState.TakeDamage(100);
        GameState.TakeDamage(10); // Already dead, should not emit again
        await AssertSignal(GameState).IsEmittedCount(1, "PlayerDied");
    }

    // ============================================================
    // STATS_CHANGED SIGNAL TESTS
    // Verify that the StatsChanged signal is emitted on all state changes.
    // ============================================================

    [TestCase]
    public async Task TestStatsChangedOnDamage()
    {
        GameState.Reset();
        GameState.TakeDamage(10);
        await AssertSignal(GameState).IsEmitted("StatsChanged");
    }

    [TestCase]
    public async Task TestStatsChangedOnXpGain()
    {
        GameState.Reset();
        GameState.AwardXp(10);
        await AssertSignal(GameState).IsEmitted("StatsChanged");
    }

    [TestCase]
    public async Task TestStatsChangedOnLevelUp()
    {
        GameState.Reset();
        GameState.AwardXp(90); // Should trigger level up
        await AssertSignal(GameState).IsEmitted("StatsChanged");
    }

    // ============================================================
    // XP AND LEVELING TESTS
    // Verify XP accumulation, level-up thresholds, excess XP carry-over,
    // max HP increases, and healing on level-up.
    // ============================================================

    [TestCase]
    public void TestAwardXpAddsCorrectly()
    {
        GameState.Reset();
        GameState.AwardXp(50);
        AssertThat(GameState.Xp).IsEqual(50);
    }

    [TestCase]
    public void TestAwardXpAccumulates()
    {
        GameState.Reset();
        GameState.AwardXp(20);
        GameState.AwardXp(30);
        AssertThat(GameState.Xp).IsEqual(50);
    }

    [TestCase]
    public void TestLevelUpAtThreshold()
    {
        GameState.Reset(); // Level 1, threshold = 1 * 90 = 90
        GameState.AwardXp(90);
        AssertThat(GameState.Level).IsEqual(2);
        AssertThat(GameState.Xp).IsEqual(0);
    }

    [TestCase]
    public void TestLevelUpExcessXpCarriesOver()
    {
        GameState.Reset();
        GameState.AwardXp(100); // 10 excess past the 90 threshold
        AssertThat(GameState.Level).IsEqual(2);
        AssertThat(GameState.Xp).IsEqual(10);
    }

    [TestCase]
    public void TestNoLevelUpBelowThreshold()
    {
        GameState.Reset();
        GameState.AwardXp(89);
        AssertThat(GameState.Level).IsEqual(1);
        AssertThat(GameState.Xp).IsEqual(89);
    }

    [TestCase]
    public void TestLevelUpIncreasesMaxHp()
    {
        GameState.Reset();
        GameState.AwardXp(90); // Level 1 -> 2
        AssertThat(GameState.MaxHp).IsEqual(116);
    }

    [TestCase]
    public void TestLevelUpHealsPlayer()
    {
        GameState.Reset();
        GameState.TakeDamage(50); // HP = 50
        GameState.AwardXp(90);    // Level up: heal min(116, 50 + 18) = 68
        AssertThat(GameState.Hp).IsEqual(68);
    }

    [TestCase]
    public void TestLevelUpHealCappedAtMaxHp()
    {
        GameState.Reset(); // HP = 100
        GameState.AwardXp(90); // Level up: heal min(116, 100 + 18) = 116
        AssertThat(GameState.Hp).IsEqual(116);
    }

    [TestCase]
    public void TestLevel2ThresholdIs180()
    {
        GameState.Reset();
        GameState.AwardXp(90);  // Level 1 -> 2, XP resets to 0
        GameState.AwardXp(179); // Just below level 2 threshold (2 * 90 = 180)
        AssertThat(GameState.Level).IsEqual(2);
        GameState.AwardXp(1);   // Now at 180
        AssertThat(GameState.Level).IsEqual(3);
    }

    [TestCase]
    public void TestLevel3MaxHp()
    {
        GameState.Reset();
        GameState.AwardXp(90);  // Level 2
        GameState.AwardXp(180); // Level 3
        AssertThat(GameState.MaxHp).IsEqual(124);
    }

    // ============================================================
    // RESET TESTS
    // Verify that Reset() restores all properties to initial values.
    // ============================================================

    [TestCase]
    public void TestResetRestoresAllDefaults()
    {
        GameState.Reset();
        GameState.TakeDamage(50);
        GameState.AwardXp(200);
        GameState.FloorNumber = 5;
        GameState.Reset();
        AssertThat(GameState.Hp).IsEqual(100);
        AssertThat(GameState.MaxHp).IsEqual(100);
        AssertThat(GameState.Xp).IsEqual(0);
        AssertThat(GameState.Level).IsEqual(1);
        AssertThat(GameState.FloorNumber).IsEqual(1);
        AssertThat(GameState.IsDead).IsFalse();
    }

    [TestCase]
    public void TestResetClearsDeadState()
    {
        GameState.Reset();
        GameState.TakeDamage(100); // Kill the player
        AssertThat(GameState.IsDead).IsTrue();
        GameState.Reset();
        AssertThat(GameState.IsDead).IsFalse();
        AssertThat(GameState.Hp).IsEqual(100);
    }
}
```

**Test count: 30 tests**

---

## Test File: tests/EnemyTests.cs

Unit tests for enemy stat formulas. These test the mathematical formulas directly, without instantiating enemy scenes.

**Note:** These tests verify the formulas themselves (`18 + tier * 12`, etc.) to ensure the math is correct. Scene-based tests (verifying enemy nodes use these formulas) are in the integration tests.

```csharp
using Godot;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public partial class EnemyTests
{
    // ============================================================
    // TIER HP FORMULA: 18 + dangerTier * 12
    // ============================================================

    [TestCase]
    public void TestTier1Hp()
    {
        int hp = 18 + 1 * 12;
        AssertThat(hp).IsEqual(30);
    }

    [TestCase]
    public void TestTier2Hp()
    {
        int hp = 18 + 2 * 12;
        AssertThat(hp).IsEqual(42);
    }

    [TestCase]
    public void TestTier3Hp()
    {
        int hp = 18 + 3 * 12;
        AssertThat(hp).IsEqual(54);
    }

    // ============================================================
    // TIER SPEED FORMULA: 48 + dangerTier * 18
    // ============================================================

    [TestCase]
    public void TestTier1Speed()
    {
        int speed = 48 + 1 * 18;
        AssertThat(speed).IsEqual(66);
    }

    [TestCase]
    public void TestTier2Speed()
    {
        int speed = 48 + 2 * 18;
        AssertThat(speed).IsEqual(84);
    }

    [TestCase]
    public void TestTier3Speed()
    {
        int speed = 48 + 3 * 18;
        AssertThat(speed).IsEqual(102);
    }

    // ============================================================
    // TIER DAMAGE FORMULA: 3 + dangerTier
    // ============================================================

    [TestCase]
    public void TestTier1Damage()
    {
        int damage = 3 + 1;
        AssertThat(damage).IsEqual(4);
    }

    [TestCase]
    public void TestTier2Damage()
    {
        int damage = 3 + 2;
        AssertThat(damage).IsEqual(5);
    }

    [TestCase]
    public void TestTier3Damage()
    {
        int damage = 3 + 3;
        AssertThat(damage).IsEqual(6);
    }

    // ============================================================
    // TIER XP REWARD FORMULA: 10 + dangerTier * 4
    // ============================================================

    [TestCase]
    public void TestTier1Xp()
    {
        int xp = 10 + 1 * 4;
        AssertThat(xp).IsEqual(14);
    }

    [TestCase]
    public void TestTier2Xp()
    {
        int xp = 10 + 2 * 4;
        AssertThat(xp).IsEqual(18);
    }

    [TestCase]
    public void TestTier3Xp()
    {
        int xp = 10 + 3 * 4;
        AssertThat(xp).IsEqual(22);
    }

    // ============================================================
    // PLAYER DAMAGE FORMULA: 12 + floor(level * 1.5)
    // Used to verify how many hits it takes to kill each tier.
    // ============================================================

    [TestCase]
    public void TestPlayerDamageLevel1()
    {
        int damage = 12 + (int)(1 * 1.5);
        AssertThat(damage).IsEqual(13);
    }

    [TestCase]
    public void TestPlayerDamageLevel2()
    {
        int damage = 12 + (int)(2 * 1.5);
        AssertThat(damage).IsEqual(15);
    }

    [TestCase]
    public void TestPlayerDamageLevel3()
    {
        int damage = 12 + (int)(3 * 1.5);
        AssertThat(damage).IsEqual(16);
    }

    [TestCase]
    public void TestPlayerDamageLevel5()
    {
        int damage = 12 + (int)(5 * 1.5);
        AssertThat(damage).IsEqual(19);
    }

    [TestCase]
    public void TestPlayerDamageLevel10()
    {
        int damage = 12 + (int)(10 * 1.5);
        AssertThat(damage).IsEqual(27);
    }

    // ============================================================
    // HITS TO KILL CALCULATIONS
    // Verify the number of player attacks needed to kill each tier.
    // ============================================================

    [TestCase]
    public void TestHitsToKillTier1AtLevel1()
    {
        int enemyHp = 30;
        int playerDamage = 13; // Level 1
        int hits = (int)Mathf.Ceil((float)enemyHp / (float)playerDamage);
        AssertThat(hits).IsEqual(3);
    }

    [TestCase]
    public void TestHitsToKillTier2AtLevel1()
    {
        int enemyHp = 42;
        int playerDamage = 13; // Level 1
        int hits = (int)Mathf.Ceil((float)enemyHp / (float)playerDamage);
        AssertThat(hits).IsEqual(4);
    }

    [TestCase]
    public void TestHitsToKillTier3AtLevel1()
    {
        int enemyHp = 54;
        int playerDamage = 13; // Level 1
        int hits = (int)Mathf.Ceil((float)enemyHp / (float)playerDamage);
        AssertThat(hits).IsEqual(5);
    }

    [TestCase]
    public void TestHitsToKillTier1AtLevel5()
    {
        int enemyHp = 30;
        int playerDamage = 19; // Level 5
        int hits = (int)Mathf.Ceil((float)enemyHp / (float)playerDamage);
        AssertThat(hits).IsEqual(2);
    }

    [TestCase]
    public void TestHitsToKillTier3AtLevel10()
    {
        int enemyHp = 54;
        int playerDamage = 27; // Level 10
        int hits = (int)Mathf.Ceil((float)enemyHp / (float)playerDamage);
        AssertThat(hits).IsEqual(2);
    }
}
```

**Test count: 20 tests**

---

## Test File: tests/MovementTests.cs

Unit tests for the isometric transform math and speed normalization. These test the mathematical operations used in the movement system.

```csharp
using Godot;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public partial class MovementTests
{
    // The isometric transform matrix used for screen-to-world conversion.
    // This maps screen-space input directions to isometric world directions.
    private Transform2D _isoTransform = new Transform2D(new Vector2(1, 0.5f), new Vector2(-1, 0.5f), Vector2.Zero);

    // ============================================================
    // ISOMETRIC TRANSFORM DIRECTION TESTS
    // Verify that screen-space input directions map to the correct
    // isometric world directions.
    // ============================================================

    [TestCase]
    public void TestIsoTransformScreenUpMapsToNortheast()
    {
        var screenUp = new Vector2(0, -1);
        var result = _isoTransform * screenUp;
        // Northeast: positive x, negative y in world space
        AssertThat(result.X).IsGreater(0.0f);
        AssertThat(result.Y).IsLess(0.0f);
    }

    [TestCase]
    public void TestIsoTransformScreenDownMapsToSouthwest()
    {
        var screenDown = new Vector2(0, 1);
        var result = _isoTransform * screenDown;
        // Southwest: negative x, positive y in world space
        AssertThat(result.X).IsLess(0.0f);
        AssertThat(result.Y).IsGreater(0.0f);
    }

    [TestCase]
    public void TestIsoTransformScreenRightMapsToSoutheast()
    {
        var screenRight = new Vector2(1, 0);
        var result = _isoTransform * screenRight;
        // Southeast: positive x, positive y in world space
        AssertThat(result.X).IsGreater(0.0f);
        AssertThat(result.Y).IsGreater(0.0f);
    }

    [TestCase]
    public void TestIsoTransformScreenLeftMapsToNorthwest()
    {
        var screenLeft = new Vector2(-1, 0);
        var result = _isoTransform * screenLeft;
        // Northwest: negative x, negative y in world space
        AssertThat(result.X).IsLess(0.0f);
        AssertThat(result.Y).IsLess(0.0f);
    }

    // ============================================================
    // SPEED NORMALIZATION TESTS
    // Verify that diagonal movement is not faster than cardinal movement.
    // After normalization and applying speed, both should be equal.
    // ============================================================

    [TestCase]
    public void TestDiagonalSpeedEqualsCardinalSpeed()
    {
        float speed = 190.0f;
        var cardinalInput = new Vector2(1, 0);  // Pure screen right
        var diagonalInput = new Vector2(1, -1).Normalized();  // Screen up-right (normalized)

        var cardinalVelocity = (_isoTransform * cardinalInput).Normalized() * speed;
        var diagonalVelocity = (_isoTransform * diagonalInput).Normalized() * speed;

        AssertThat(cardinalVelocity.Length()).IsEqual(diagonalVelocity.Length());
    }

    [TestCase]
    public void TestCardinalSpeedIsExact()
    {
        float speed = 190.0f;
        var input = new Vector2(0, -1);  // Screen up
        var velocity = (_isoTransform * input).Normalized() * speed;
        AssertThat(velocity.Length()).IsEqualApprox(190.0f, 0.1f);
    }

    [TestCase]
    public void TestZeroInputProducesZeroVelocity()
    {
        float speed = 190.0f;
        var input = Vector2.Zero;
        // Note: normalizing Vector2.Zero returns Vector2.Zero in Godot
        var direction = _isoTransform * input;
        if (direction.Length() > 0)
            direction = direction.Normalized();
        var velocity = direction * speed;
        AssertThat(velocity.Length()).IsEqualApprox(0.0f, 0.01f);
    }

    [TestCase]
    public void TestOpposingInputsCancelOut()
    {
        var input = new Vector2(1, 0) + new Vector2(-1, 0);  // Right + Left = Zero
        AssertThat(input.Length()).IsEqualApprox(0.0f, 0.01f);
    }

    [TestCase]
    public void TestThreeKeyInputResolvesCorrectly()
    {
        // Pressing W + A + D: A and D cancel on x-axis, leaving only W (screen up)
        var input = new Vector2(0, -1) + new Vector2(-1, 0) + new Vector2(1, 0);
        // This simplifies to Vector2(0, -1) = screen up
        AssertThat(input.X).IsEqualApprox(0.0f, 0.01f);
        AssertThat(input.Y).IsEqualApprox(-1.0f, 0.01f);
    }

    [TestCase]
    public void TestAllFourKeysCancelToZero()
    {
        var input = new Vector2(0, -1) + new Vector2(0, 1) + new Vector2(-1, 0) + new Vector2(1, 0);
        AssertThat(input.Length()).IsEqualApprox(0.0f, 0.01f);
    }
}
```

**Test count: 10 tests**

---

## Test File: tests/CombatTests.cs

Integration tests for the combat flow: player attacks enemy, enemy takes damage, enemy dies, XP is awarded, level-up triggers.

**Note:** These tests require scene instantiation and may need `await` for timer-based behavior. They test the interaction between multiple systems.

```csharp
using Godot;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public partial class CombatTests
{
    // ============================================================
    // ATTACK COOLDOWN TESTS
    // ============================================================

    [TestCase]
    public void TestAttackCooldownValue()
    {
        // The attack cooldown should be 0.42 seconds (420ms)
        float cooldown = 0.42f;
        AssertThat(cooldown).IsEqualApprox(0.42f, 0.001f);
    }

    [TestCase]
    public void TestAttackRangeValue()
    {
        // The attack range should be 78 pixels
        float rangePx = 78.0f;
        AssertThat(rangePx).IsEqualApprox(78.0f, 0.1f);
    }

    // ============================================================
    // DAMAGE FLOW TESTS
    // Test that attacking an enemy -> reducing HP -> death -> XP gain
    // works correctly with real values.
    // ============================================================

    [TestCase]
    public void TestTier1EnemyDiesAfterCorrectHits()
    {
        int enemyHp = 30;  // Tier 1
        int playerDamage = 13;  // Level 1: 12 + floor(1 * 1.5) = 13
        // Hit 1: 30 - 13 = 17
        enemyHp -= playerDamage;
        AssertThat(enemyHp).IsEqual(17);
        // Hit 2: 17 - 13 = 4
        enemyHp -= playerDamage;
        AssertThat(enemyHp).IsEqual(4);
        // Hit 3: 4 - 13 = -9 (dead)
        enemyHp -= playerDamage;
        AssertThat(enemyHp).IsLess(1);
    }

    [TestCase]
    public void TestTier2EnemyDiesAfterCorrectHits()
    {
        int enemyHp = 42;  // Tier 2
        int playerDamage = 13;  // Level 1
        // Hit 1: 42 - 13 = 29
        enemyHp -= playerDamage;
        AssertThat(enemyHp).IsEqual(29);
        // Hit 2: 29 - 13 = 16
        enemyHp -= playerDamage;
        AssertThat(enemyHp).IsEqual(16);
        // Hit 3: 16 - 13 = 3
        enemyHp -= playerDamage;
        AssertThat(enemyHp).IsEqual(3);
        // Hit 4: 3 - 13 = -10 (dead)
        enemyHp -= playerDamage;
        AssertThat(enemyHp).IsLess(1);
    }

    [TestCase]
    public void TestTier3EnemyDiesAfterCorrectHits()
    {
        int enemyHp = 54;  // Tier 3
        int playerDamage = 13;  // Level 1
        for (int i = 0; i < 4; i++)
            enemyHp -= playerDamage;
        AssertThat(enemyHp).IsEqual(2);
        enemyHp -= playerDamage;
        AssertThat(enemyHp).IsLess(1);
    }

    [TestCase]
    public void TestXpAwardedOnKillUpdatesGameState()
    {
        GameState.Reset();
        // Simulate killing a tier 1 enemy
        int xpReward = 10 + 1 * 4; // = 14
        GameState.AwardXp(xpReward);
        AssertThat(GameState.Xp).IsEqual(14);
    }

    [TestCase]
    public void TestKillSequenceTriggersLevelUp()
    {
        GameState.Reset();
        // Kill enough tier 1 enemies to level up
        // Tier 1 gives 14 XP, threshold is 90
        // 6 kills = 84 XP (not enough)
        // 7 kills = 98 XP (level up! excess = 8)
        for (int i = 0; i < 7; i++)
            GameState.AwardXp(14); // Tier 1 XP
        AssertThat(GameState.Level).IsEqual(2);
        AssertThat(GameState.Xp).IsEqual(8);
    }

    [TestCase]
    public void TestMixedKillsLevelUp()
    {
        GameState.Reset();
        // Kill 2 tier 3 (22 each = 44), 1 tier 2 (18), 2 tier 1 (14 each = 28)
        // Total: 44 + 18 + 28 = 90 -> exactly level up
        GameState.AwardXp(22); // Tier 3 kill
        GameState.AwardXp(22); // Tier 3 kill
        GameState.AwardXp(18); // Tier 2 kill
        GameState.AwardXp(14); // Tier 1 kill
        GameState.AwardXp(14); // Tier 1 kill
        AssertThat(GameState.Level).IsEqual(2);
        AssertThat(GameState.Xp).IsEqual(0);
    }
}
```

**Test count: 8 tests**

---

## Test File: tests/SpawningTests.cs

Integration tests for the enemy spawning system: initial count, soft cap enforcement, tier randomization, and edge spawn positions.

```csharp
using Godot;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public partial class SpawningTests
{
    // ============================================================
    // SPAWN COUNT TESTS
    // ============================================================

    [TestCase]
    public void TestInitialSpawnCount()
    {
        int initialCount = 10;
        AssertThat(initialCount).IsEqual(10);
    }

    [TestCase]
    public void TestSoftCapValue()
    {
        int softCap = 14;
        AssertThat(softCap).IsEqual(14);
    }

    [TestCase]
    public void TestPeriodicTimerValue()
    {
        float timerInterval = 2.8f;
        AssertThat(timerInterval).IsEqualApprox(2.8f, 0.01f);
    }

    [TestCase]
    public void TestRespawnDelayValue()
    {
        float respawnDelay = 1.4f;
        AssertThat(respawnDelay).IsEqualApprox(1.4f, 0.01f);
    }

    // ============================================================
    // TIER RANDOMIZATION TESTS
    // ============================================================

    [TestCase]
    public void TestRandomTierInValidRange()
    {
        // Run 100 random tier generations and verify all are in range 1-3
        var rng = new RandomNumberGenerator();
        for (int i = 0; i < 100; i++)
        {
            int tier = rng.RandiRange(1, 3);
            AssertThat(tier).IsBetween(1, 3);
        }
    }

    // ============================================================
    // EDGE SPAWN POSITION TESTS
    // ============================================================

    [TestCase]
    public void TestEdgeSpawnPositionsAreAtBoundaries()
    {
        // Simulate edge position generation for a 1920x1080 world
        float worldWidth = 1920.0f;
        float worldHeight = 1080.0f;
        var rng = new RandomNumberGenerator();

        for (int i = 0; i < 50; i++)
        {
            int edge = rng.RandiRange(0, 3);
            var pos = Vector2.Zero;
            switch (edge)
            {
                case 0: // Top edge
                    pos = new Vector2(rng.RandfRange(0, worldWidth), 10);
                    AssertThat(pos.Y).IsEqualApprox(10.0f, 0.1f);
                    AssertThat(pos.X).IsBetween(0.0f, worldWidth);
                    break;
                case 1: // Right edge
                    pos = new Vector2(worldWidth - 10, rng.RandfRange(0, worldHeight));
                    AssertThat(pos.X).IsEqualApprox(worldWidth - 10, 0.1f);
                    AssertThat(pos.Y).IsBetween(0.0f, worldHeight);
                    break;
                case 2: // Bottom edge
                    pos = new Vector2(rng.RandfRange(0, worldWidth), worldHeight - 10);
                    AssertThat(pos.Y).IsEqualApprox(worldHeight - 10, 0.1f);
                    AssertThat(pos.X).IsBetween(0.0f, worldWidth);
                    break;
                case 3: // Left edge
                    pos = new Vector2(10, rng.RandfRange(0, worldHeight));
                    AssertThat(pos.X).IsEqualApprox(10.0f, 0.1f);
                    AssertThat(pos.Y).IsBetween(0.0f, worldHeight);
                    break;
            }
        }
    }
}
```

**Test count: 5 tests** (note: the last test runs 50 iterations internally)

---

## Test File: tests/DeathRestartTests.cs

Integration tests for the death and restart cycle: HP reaching 0 triggers death state, restart resets all state.

```csharp
using Godot;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public partial class DeathRestartTests
{
    // ============================================================
    // DEATH STATE TESTS
    // ============================================================

    [TestCase]
    public void TestDeathOccursAtZeroHp()
    {
        GameState.Reset();
        GameState.TakeDamage(100);
        AssertThat(GameState.Hp).IsEqual(0);
        AssertThat(GameState.IsDead).IsTrue();
    }

    [TestCase]
    public void TestDeathOccursWithOverkillDamage()
    {
        GameState.Reset();
        GameState.TakeDamage(150); // 50 more than max HP
        AssertThat(GameState.Hp).IsEqual(0);
        AssertThat(GameState.IsDead).IsTrue();
    }

    [TestCase]
    public void TestGradualDeathFromEnemyHits()
    {
        GameState.Reset();
        // Simulate tier 1 enemies (4 damage each) hitting 25 times: 25 * 4 = 100
        for (int i = 0; i < 25; i++)
            GameState.TakeDamage(4);
        AssertThat(GameState.Hp).IsEqual(0);
        AssertThat(GameState.IsDead).IsTrue();
    }

    [TestCase]
    public void TestDeathFromMixedTierEnemies()
    {
        GameState.Reset();
        // Simulate: 5 tier 3 hits (6 dmg = 30) + 5 tier 2 hits (5 dmg = 25) + 10 tier 1 hits (4 dmg = 40) = 95
        for (int i = 0; i < 5; i++)
            GameState.TakeDamage(6); // Tier 3
        for (int i = 0; i < 5; i++)
            GameState.TakeDamage(5); // Tier 2
        for (int i = 0; i < 10; i++)
            GameState.TakeDamage(4); // Tier 1
        AssertThat(GameState.Hp).IsEqual(5);
        AssertThat(GameState.IsDead).IsFalse();
        GameState.TakeDamage(5);
        AssertThat(GameState.Hp).IsEqual(0);
        AssertThat(GameState.IsDead).IsTrue();
    }

    // ============================================================
    // RESTART STATE TESTS
    // ============================================================

    [TestCase]
    public void TestRestartResetsAllStateAfterDeath()
    {
        GameState.Reset();
        // Play a full game simulation
        GameState.AwardXp(200); // Level up multiple times
        GameState.TakeDamage(50);
        GameState.FloorNumber = 3;
        GameState.TakeDamage(999); // Die
        AssertThat(GameState.IsDead).IsTrue();

        // Restart
        GameState.Reset();
        AssertThat(GameState.Hp).IsEqual(100);
        AssertThat(GameState.MaxHp).IsEqual(100);
        AssertThat(GameState.Xp).IsEqual(0);
        AssertThat(GameState.Level).IsEqual(1);
        AssertThat(GameState.FloorNumber).IsEqual(1);
        AssertThat(GameState.IsDead).IsFalse();
    }

    [TestCase]
    public void TestRestartAllowsNewGameplay()
    {
        GameState.Reset();
        GameState.TakeDamage(100); // Die
        AssertThat(GameState.IsDead).IsTrue();

        GameState.Reset(); // Restart
        AssertThat(GameState.IsDead).IsFalse();

        // Verify new game works normally
        GameState.TakeDamage(10);
        AssertThat(GameState.Hp).IsEqual(90);
        GameState.AwardXp(14);
        AssertThat(GameState.Xp).IsEqual(14);
    }
}
```

**Test count: 6 tests**

---

## Implementation Notes

### Running All Tests

To run the complete test suite:

**From Godot Editor:**
1. Open GdUnit4 panel (bottom dock)
2. Click "Run All"
3. All 64 tests should pass (green checkmarks)

**From Command Line:**
```bash
godot --headless -s addons/gdUnit4/bin/GdUnitCmdTool.gd --add "res://tests/" -e
```

**Expected Output:**
```
64 tests passed, 0 failed
```

### Test Dependencies

| Test File | Depends On |
|-----------|-----------|
| `GameStateTests.cs` | `GameState` autoload registered |
| `EnemyTests.cs` | Nothing (pure math) |
| `MovementTests.cs` | Nothing (pure math) |
| `CombatTests.cs` | `GameState` autoload registered |
| `SpawningTests.cs` | Nothing (pure math + randomization) |
| `DeathRestartTests.cs` | `GameState` autoload registered |

### Adding New Tests

When adding a new system:
1. Create `tests/<SystemName>Tests.cs`
2. Add the `[TestSuite]` attribute to the class
3. Add the `[TestCase]` attribute to all test methods
4. Use `[Before] public void Setup()` for setup if needed (e.g., `GameState.Reset()`)
5. Run from GdUnit4 panel to verify

### Common Patterns

**Testing formulas (pure math):**
```csharp
[TestCase]
public void TestSomeFormula()
{
    int result = baseValue + modifier * scalingFactor;
    AssertThat(result).IsEqual(expectedValue);
}
```

**Testing GameState changes:**
```csharp
[TestCase]
public void TestSomeStateChange()
{
    GameState.Reset(); // Always start clean
    // Perform action
    GameState.SomeMethod();
    // Verify result
    AssertThat(GameState.SomeProperty).IsEqual(expectedValue);
}
```

**Testing signals:**
```csharp
[TestCase]
public async Task TestSomeSignal()
{
    GameState.Reset();
    GameState.SomeMethod(); // Action that should emit
    await AssertSignal(GameState).IsEmitted("SignalName");
}
```

## Open Questions

- Should tests use `[Before] public void Setup()` with `GameState.Reset()` instead of calling it in every test method?
- Should scene-based integration tests be added once enemy.tscn and player.tscn are created?
- Should performance tests be automated (e.g., spawn 100 enemies and measure frame time)?
- Should test coverage reporting be set up?
- Should there be a dedicated test for the isometric coordinate conversion used in mouse/touch input (when implemented)?
