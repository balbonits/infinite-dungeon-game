# Combat System

## Summary

Real-time button-press combat with target cycling. The player presses face buttons to attack the current target. L1/R1 cycles through nearby enemies based on a configurable priority mode (nearest, strongest, tankiest, bosses, weakest). Default target is nearest enemy within range.

## Current State

Implemented in the prototype:
- Auto-targeting: finds nearest enemy within `ATTACK_RANGE` (78px)
- Attack cooldown: `ATTACK_COOLDOWN` (420ms)
- Damage: `12 + floor(level * 1.5)` per hit
- Slash visual effect (tweened rectangle)
- Enemy damage to player: `3 + dangerTier` on overlap, with 700ms hit cooldown per enemy
- Camera shake on hit

## Design

### Attack Input

Combat is triggered by **face button presses** (✕/○/□/△ on PS1, Z/X/A/S on keyboard). All face buttons default to basic attack. The player mashes a button to attack — no aiming required.

See [controls.md](../ui/controls.md) for the full control scheme and shortcut system.

### Targeting

Target selection uses a **priority-based system** with optional manual cycling:

**Default (no cycling):** The target is selected automatically based on the active priority mode.
**After L1/R1 tap:** A visual indicator locks onto the selected enemy. Attacks focus on that target.
**Target dies:** Lock clears, reverts to priority-based auto-selection.

**Target priority modes (configurable in Settings):**

| Mode | Behavior | Default? |
|------|----------|----------|
| Nearest | Closest enemy within range | Yes |
| Strongest | Highest damage enemy | |
| Tankiest | Highest HP enemy | |
| Bosses | Boss enemies first, then nearest | |
| Weakest | Lowest HP enemy (finish off wounded) | |

### Attack Flow

Each physics frame when a face button is held/pressed:
1. Check if the attack cooldown has elapsed
2. Find the current target (locked target if cycling, else priority-based)
3. If target is in range, deal damage and show slash effect
4. If the enemy's HP drops to 0, defeat it and clear target lock
5. Reset attack cooldown

### Damage Formula

**P1 (prototype parity):** Placeholder formula — no stats system yet:
```
playerDamage = 12 + floor(level * 1.5)
enemyDamage = 3 + dangerTier  (tier 1–3)
```

**P2+ (after stats system is implemented):** Replaced by the STR-based formula from [stats.md](stats.md):
```
total_melee_damage = (base_weapon_damage + flat_melee_bonus) * (1 + percent_melee_boost / 100)
```
Where `flat_melee_bonus = effective_str * 1.5` and `percent_melee_boost = effective_str * 0.8%`. See stats.md for full formula and value tables. The transition happens when P2-02 (stats system) is implemented.

### Attack Cooldown

Fixed at 420ms. Future plans:
- DEX could reduce cooldown
- Different weapon types could have different base cooldowns
- Abilities may have their own independent cooldowns

### Range

Fixed at 78px for melee. Future plans:
- Ranger class could have longer range
- Mage spells could have variable range
- Melee range might scale slightly with weapon type

### Visual Feedback

- **Slash effect:** a small rotated rectangle that fades upward (120ms tween)
- **Camera shake:** 90ms, low intensity (0.0035) on player hit
- **Enemy defeat:** enemy is disabled and fades out (currently instant disable)

## Godot Implementation

### Player Auto-Attack (Godot)

The auto-attack system is implemented using an Area2D node for range detection and a float-based cooldown timer.

**Scene tree structure:**
```
Player (CharacterBody2D)
  +-- Polygon2D (player sprite)
  +-- CollisionShape2D (body, radius 12px)
  +-- AttackRange (Area2D)
  |     +-- CollisionShape2D (CircleShape2D, radius 78px)
  +-- Camera2D
```

**AttackRange Area2D configuration:**

| Property | Value |
|----------|-------|
| Node type | Area2D |
| CollisionShape2D shape | CircleShape2D |
| Shape radius | 78.0 px (matches Phaser `ATTACK_RANGE = 78`) |
| collision_layer | 4 (sensors) |
| collision_mask | 3 (enemies) |
| monitoring | true (detects bodies entering) |
| monitorable | false (other areas do not need to detect this) |

**Target finding algorithm (every physics frame):**
```csharp
private const float AttackCooldown = 0.42f; // seconds (420ms)
private float _attackCooldownRemaining = 0.0f;

public override void _PhysicsProcess(double delta)
{
    // Decrement cooldown
    _attackCooldownRemaining -= (float)delta;
    if (_attackCooldownRemaining > 0.0f)
        return;

    // Get all enemy bodies overlapping the AttackRange Area2D
    var bodies = _attackRange.GetOverlappingBodies();

    // Find the nearest enemy
    CharacterBody2D bestEnemy = null;
    float bestDistance = float.PositiveInfinity;

    foreach (var body in bodies)
    {
        if (!body.IsInGroup("enemies"))
            continue;
        if (!body.IsInsideTree())
            continue;
        float dist = GlobalPosition.DistanceTo(body.GlobalPosition);
        if (dist < bestDistance)
        {
            bestDistance = dist;
            bestEnemy = (CharacterBody2D)body;
        }
    }

    if (bestEnemy == null)
        return;

    // Attack the nearest enemy
    _attackCooldownRemaining = AttackCooldown;
    int damage = 12 + (int)(GameState.Level * 1.5f);
    bestEnemy.TakeDamage(damage);
    DrawSlash(bestEnemy.GlobalPosition);
}
```

**Damage calculation:**
```
damage = 12 + int(GameState.level * 1.5)
```

| Player Level | Damage | Calculation |
|-------------|--------|-------------|
| 1 | 13 | 12 + int(1 * 1.5) = 12 + 1 |
| 2 | 15 | 12 + int(2 * 1.5) = 12 + 3 |
| 3 | 16 | 12 + int(3 * 1.5) = 12 + 4 |
| 5 | 19 | 12 + int(5 * 1.5) = 12 + 7 |
| 10 | 27 | 12 + int(10 * 1.5) = 12 + 15 |

**Slash visual effect (Godot):**
```csharp
private void DrawSlash(Vector2 targetPos)
{
    var slash = new Polygon2D();
    slash.Polygon = new Vector2[]
    {
        new Vector2(-13, -2), new Vector2(13, -2),
        new Vector2(13, 2), new Vector2(-13, 2)
    };
    slash.Color = new Color(0.961f, 0.784f, 0.420f, 0.95f); // #f5c86b @ 95%
    slash.Position = targetPos;
    slash.Rotation = (float)GD.RandRange(-1.2, 1.2);
    GetParent().AddChild(slash);

    var tween = CreateTween();
    tween.TweenProperty(slash, "modulate:a", 0.0f, 0.12); // Fade out over 120ms
    tween.Parallel().TweenProperty(slash, "position:y", targetPos.Y - 8.0f, 0.12); // Drift up 8px
    tween.TweenCallback(Callable.From(slash.QueueFree)); // Clean up
}
```

### Enemy Damage to Player (Godot)

Enemies deal contact damage through an Area2D "hit area" that detects the player. A cooldown timer prevents rapid-fire damage.

**Enemy scene tree structure:**
```
Enemy (CharacterBody2D)
  +-- Polygon2D (enemy sprite, color set by tier)
  +-- CollisionShape2D (body, radius 10px)
  +-- HitArea (Area2D)
  |     +-- CollisionShape2D (CircleShape2D, radius 15px)
  +-- HitCooldownTimer (Timer)
```

**HitArea Area2D configuration:**

| Property | Value |
|----------|-------|
| Node type | Area2D |
| CollisionShape2D shape | CircleShape2D |
| Shape radius | 15.0 px |
| collision_layer | 5 (damage) |
| collision_mask | 2 (player) |
| monitoring | true |

**HitCooldownTimer configuration:**

| Property | Value |
|----------|-------|
| Node type | Timer |
| wait_time | 0.7 seconds (700ms, matches Phaser `enemy.lastHitAt` check) |
| one_shot | true |
| autostart | false |

**Damage flow:**

1. **Initial hit:** When the player enters the HitArea, `body_entered` signal fires.
   ```csharp
   private void OnHitAreaBodyEntered(Node2D body)
   {
       if (body.IsInGroup("player") && _hitCooldownTimer.IsStopped())
       {
           DealDamageToPlayer((CharacterBody2D)body);
           _hitCooldownTimer.Start();
       }
   }
   ```

2. **Continuous damage:** While the player remains in the HitArea, the cooldown timer repeats.
   ```csharp
   private void OnHitCooldownTimerTimeout()
   {
       var overlapping = _hitArea.GetOverlappingBodies();
       foreach (var body in overlapping)
       {
           if (body.IsInGroup("player"))
           {
               DealDamageToPlayer((CharacterBody2D)body);
               _hitCooldownTimer.Start(); // Restart for next tick
               return;
           }
       }
       // Player left the area -- do not restart timer
   }
   ```

3. **Damage calculation:**
   ```csharp
   private void DealDamageToPlayer(CharacterBody2D player)
   {
       int damage = 3 + _dangerTier;
       player.TakeDamage(damage);
   }
   ```

**Damage values by tier:**

| Tier | Damage | Phaser Source |
|------|--------|---------------|
| 1 | 4 | `3 + 1` |
| 2 | 5 | `3 + 2` |
| 3 | 6 | `3 + 3` |

**Player take_damage() handler:**
```csharp
public void TakeDamage(int amount)
{
    GameState.Hp -= amount;
    ShakeCamera();
    EventBus.PlayerDamaged.Emit(amount);

    if (GameState.Hp <= 0)
    {
        GameState.Hp = 0;
        Die();
    }
}
```

### Key Differences from Phaser

| Aspect | Phaser Prototype | Godot Implementation |
|--------|-----------------|---------------------|
| Attack range check | Manual `Phaser.Math.Distance.Between()` every frame for every enemy | Area2D `GetOverlappingBodies()` -- physics engine maintains the overlap list; script just reads it |
| Nearest enemy search | `this.enemies.children.iterate()` with distance comparison | Same logic, but over `_attackRange.GetOverlappingBodies()` (pre-filtered to in-range enemies only) |
| Enemy-player overlap | `this.physics.add.overlap(player, enemies, callback)` -- fires every frame while overlapping | Area2D `BodyEntered` signal (event-based, fires once on enter) + Timer for sustained damage |
| Cooldown tracking | `time - this.lastAttackAt < ATTACK_COOLDOWN` using scene time in milliseconds | Float variable decremented by delta each frame; attack fires when <= 0 |
| Enemy hit cooldown | `now - enemy.lastHitAt < 700` per-enemy timestamp | Timer node per enemy, 0.7s one_shot |
| Slash effect | `this.add.rectangle(x, y, 26, 4, color, alpha)` + `this.tweens.add(...)` | `new Polygon2D()` with vertices + `CreateTween()` property animations |
| Camera shake | `this.cameras.main.shake(90, 0.0035)` -- built-in method, oscillating | Tween `Camera2D.Offset` -- single offset + return, 2 phases at 45ms each |
| Damage application | Direct property mutation: `bestEnemy.hp -= damage` | Method call: `enemy.TakeDamage(damage)` -- allows encapsulation, signals, death checks |
| Death check | Inline: `if (bestEnemy.hp <= 0) this.defeatEnemy(bestEnemy)` | Inside `TakeDamage()`: enemy checks its own HP and handles its own death |

### Attack Timing Analysis

At `ATTACK_COOLDOWN = 0.42s`, the player attacks ~2.38 times per second.

**DPS (damage per second) by level:**

| Level | Damage/Hit | Attacks/Sec | DPS | Tier 1 Kill Time (30 HP) | Tier 2 Kill Time (42 HP) | Tier 3 Kill Time (54 HP) |
|-------|-----------|-------------|-----|-------------------------|-------------------------|-------------------------|
| 1 | 13 | 2.38 | 30.9 | 0.97s (3 hits) | 1.39s (4 hits) | 1.81s (5 hits) |
| 2 | 15 | 2.38 | 35.7 | 0.84s (2 hits) | 1.26s (3 hits) | 1.68s (4 hits) |
| 3 | 16 | 2.38 | 38.1 | 0.84s (2 hits) | 1.26s (3 hits) | 1.68s (4 hits) |
| 5 | 19 | 2.38 | 45.2 | 0.84s (2 hits) | 1.26s (3 hits) | 1.26s (3 hits) |
| 10 | 27 | 2.38 | 64.3 | 0.42s (2 hits) | 0.84s (2 hits) | 0.84s (2 hits) |

**Hits to kill formula:** `ceil(enemy_hp / damage)`

## Open Questions

- Should different classes have fundamentally different attack patterns (not just stat scaling)?
- How should area-of-effect attacks work?
- Should there be a manual attack option alongside auto-attack?
- How does weapon type affect combat (speed, range, damage)?
- Should the Area2D approach be replaced with a manual distance check for more control over targeting priority?
- Should there be a visual indicator showing which enemy is currently targeted (outline, highlight)?
- Should the attack cooldown be displayed as a small UI element (cooldown arc) near the player?
