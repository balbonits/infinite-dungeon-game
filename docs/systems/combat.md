# Combat System

## Summary

Data-driven auto-attack combat. Each class has an `AttackConfig` record defining range, cooldown, damage multiplier, and whether the attack is melee (instant hit) or ranged (projectile). The player automatically attacks the nearest enemy in range every physics frame. No class-specific branching exists in the attack execution path -- `Player.ExecuteAttack()` reads one `AttackConfig` and either applies instant damage (melee slash) or spawns a `Projectile` (ranged).

## Current State

Fully implemented across four files:
- `scripts/Player.cs` -- auto-targeting, attack selection (primary vs melee fallback), unified `ExecuteAttack()`
- `scripts/logic/AttackConfig.cs` -- data record defining any attack's properties
- `scripts/logic/ClassAttacks.cs` -- three class configs: `WarriorSlash`, `RangerArrowShot`, `MageMagicBolt`, plus `MageStaffMelee` fallback
- `scripts/Projectile.cs` -- projectile travel, collision, and damage application

Damage formula: `Constants.PlayerStats.GetDamage(level) * AttackConfig.DamageMultiplier`, where base damage is `12 + floor(level * 1.5)`. Enemy damage to player: `Constants.EnemyStats.GetDamage(level)` = `2 + level * 1`. Damage feedback uses `FlashFx.Flash()` (red tint) instead of camera shake. Floating damage numbers via `FloatingText`.

## Design

### AttackConfig Record

All attacks (melee and ranged) are defined as `AttackConfig` records (`scripts/logic/AttackConfig.cs`):

| Property | Type | Default | Purpose |
|----------|------|---------|---------|
| `Range` | float | -- | Detection radius (px) for Area2D and projectile max travel |
| `Cooldown` | float | -- | Seconds between attacks |
| `DamageMultiplier` | float | 1.0 | Multiplied against base damage |
| `IsProjectile` | bool | false | `false` = instant melee hit, `true` = spawn projectile |
| `ProjectileSpeed` | float | 0 | Pixels/second travel speed (ranged only) |
| `ProjectileTexture` | string | "" | Resource path to projectile sprite |
| `ProjectileScale` | float | 1.0 | Scale of projectile sprite |
| `ProjectileTint` | Color? | null | Optional color tint for projectile sprite |
| `Effect` | VisualEffect | Slash | Visual effect type (Slash, Projectile, None) |
| `EffectColor` | Color | #f5c86b | Color of the slash rectangle or effect |

### Class Attack Configurations

Defined as static readonly fields in `ClassAttacks` (`scripts/logic/ClassAttacks.cs`):

**WarriorSlash (melee):**

| Property | Value | Source |
|----------|-------|--------|
| Range | 78.0 px | `Constants.ClassCombat.WarriorMeleeRange` |
| Cooldown | 0.42s | `Constants.ClassCombat.WarriorCooldown` |
| DamageMultiplier | 1.0 | -- |
| IsProjectile | false | -- |
| Effect | Slash | Gold slash rectangle |
| EffectColor | #f5c86b | -- |

**RangerArrowShot (ranged projectile):**

| Property | Value | Source |
|----------|-------|--------|
| Range | 250.0 px | `Constants.ClassCombat.RangerProjectileRange` |
| Cooldown | 0.55s | `Constants.ClassCombat.RangerCooldown` |
| DamageMultiplier | 1.0 | -- |
| IsProjectile | true | -- |
| ProjectileSpeed | 400.0 px/s | `Constants.ClassCombat.ArrowSpeed` |
| ProjectileTexture | `res://assets/projectiles/arrow.png` | `Constants.Assets.ArrowProjectile` |
| ProjectileScale | 0.6 | `Constants.ClassCombat.ArrowScale` |

**MageMagicBolt (ranged projectile):**

| Property | Value | Source |
|----------|-------|--------|
| Range | 200.0 px | `Constants.ClassCombat.MageSpellRange` |
| Cooldown | 0.80s | `Constants.ClassCombat.MageSpellCooldown` |
| DamageMultiplier | 1.3 | Higher damage, slower fire rate |
| IsProjectile | true | -- |
| ProjectileSpeed | 300.0 px/s | `Constants.ClassCombat.MagicBoltSpeed` |
| ProjectileTexture | `res://assets/projectiles/magic_bolt.png` | `Constants.Assets.MagicBoltProjectile` |
| ProjectileScale | 0.8 | `Constants.ClassCombat.MagicBoltScale` |
| ProjectileTint | #4AE8E8 | Cyan tint |
| EffectColor | #4AE8E8 | Cyan |

**MageStaffMelee (melee fallback):**

| Property | Value | Source |
|----------|-------|--------|
| Range | 78.0 px | `Constants.ClassCombat.MageMeleeRange` |
| Cooldown | 0.50s | `Constants.ClassCombat.MageMeleeCooldown` |
| DamageMultiplier | 0.8 | Weaker than spell |
| IsProjectile | false | -- |
| Effect | Slash | Purple slash rectangle |
| EffectColor | #9B6BFF | -- |

### Attack Selection

On `_Ready()`, the player loads their class configs:
```csharp
_primaryAttack = ClassAttacks.GetPrimary(selectedClass);
_meleeFallback = ClassAttacks.GetMeleeFallback(selectedClass);
```

`GetMeleeFallback()` returns `MageStaffMelee` for Mage, `null` for all other classes.

The AttackRange Area2D collision shape radius is resized to match the primary attack's range.

### Attack Flow

Each physics frame in `HandleAttack(delta)`:

1. Decrement `_attackTimer` by delta. If still positive, skip.
2. Call `FindNearestEnemy()` -- iterates `_attackArea.GetOverlappingBodies()`, returns closest body in the `"enemies"` group.
3. If no target found, skip.
4. **Attack selection**: if a melee fallback exists AND the target is within fallback range, use the fallback. Otherwise use the primary attack.
5. Call `ExecuteAttack(attack, target)`.
6. Emit `EventBus.PlayerAttacked` signal.

### ExecuteAttack (Unified)

```csharp
private void ExecuteAttack(AttackConfig attack, Node2D target)
{
    int baseDamage = Constants.PlayerStats.GetDamage(GameState.Instance.Level);
    int finalDamage = (int)(baseDamage * attack.DamageMultiplier);
    _attackTimer = attack.Cooldown;

    if (attack.IsProjectile)
    {
        Projectile.Spawn(GetParent(), GlobalPosition, target.GlobalPosition,
            finalDamage, attack.ProjectileSpeed, attack.Range,
            attack.ProjectileTexture, attack.ProjectileScale, attack.ProjectileTint);
    }
    else
    {
        target.Call("TakeDamage", finalDamage);
        DrawSlash(target.GlobalPosition, attack.EffectColor);
    }
}
```

No class-specific branching. The `AttackConfig` determines all behavior.

### Damage Formula

**Player damage:**
```
baseDamage = 12 + floor(level * 1.5)     // Constants.PlayerStats.GetDamage()
finalDamage = floor(baseDamage * attackConfig.DamageMultiplier)
```

| Player Level | Base Damage | Warrior (x1.0) | Ranger (x1.0) | Mage Bolt (x1.3) | Mage Staff (x0.8) |
|-------------|-------------|-----------------|----------------|-------------------|---------------------|
| 1 | 13 | 13 | 13 | 16 | 10 |
| 3 | 16 | 16 | 16 | 20 | 12 |
| 5 | 19 | 19 | 19 | 24 | 15 |
| 10 | 27 | 27 | 27 | 35 | 21 |

**Enemy damage to player:**
```
damage = 2 + level * 1     // Constants.EnemyStats.GetDamage()
```

### Projectile System

`Projectile` (`scripts/Projectile.cs`) is an Area2D spawned via `Projectile.Spawn()`:

| Property | Value |
|----------|-------|
| Collision layer | 0 (does not block anything) |
| Collision mask | `Constants.Layers.Enemies` (4) |
| Collision shape | CircleShape2D, radius 6.0 px |
| Monitoring | true |
| Rotation | Faces travel direction (`_direction.Angle()`) |
| Despawn | After traveling `_maxDistance` pixels |

**Behavior:**
- Flies in a straight line at `_speed` px/s toward initial target position.
- On `BodyEntered`: if body is in `"enemies"` group, calls `TakeDamage(_damage)`, spawns `FloatingText.Damage`, then `QueueFree()`.
- If max distance reached without hitting, `QueueFree()`.

### Visual Feedback

**Melee slash effect:**
- Polygon2D rectangle (configurable via `Constants.Effects`): width 13px, height 2px
- Color from `AttackConfig.EffectColor` at 0.95 alpha (`Constants.Effects.SlashAlpha`)
- Random rotation in [-1.2, 1.2] radians (`Constants.Effects.SlashMaxRotation`)
- Tween: fade alpha to 0 and rise 8px over 0.12s (`Constants.Effects.SlashFadeDuration`, `SlashRiseAmount`)
- Auto-freed on tween completion

**Damage feedback on player hit:**
- `FlashFx.Flash()` -- red sprite tint, 0.15s fade back to white (no camera shake)
- `FloatingText.Damage` -- floating damage number

**Damage feedback on enemy hit:**
- `FlashFx.Flash()` -- white flash, 0.1s
- `FloatingText.Damage` -- floating damage number
- On death: `FloatingText.Xp` shows XP reward

### Enemy Damage to Player

Enemies deal contact damage through an Area2D hit area with cooldown timer:

| Property | Value | Source |
|----------|-------|--------|
| Hit area radius | 15.0 px | `Constants.EnemyStats.HitAreaRadius` |
| Hit cooldown | 0.7s | `Constants.EnemyStats.HitCooldown` |
| Invincibility check | Skips if `player.IsInvincible` (grace period) | -- |

Flow: `BodyEntered` signal fires -> checks player group + cooldown -> `GameState.Instance.TakeDamage()` -> `player.DamageFlash()` -> `FloatingText.Damage` -> starts cooldown timer.

## Open Questions

- How should area-of-effect attacks work?
- How does weapon type affect combat (speed, range, damage)?
- Should there be a visual indicator showing which enemy is currently targeted (outline, highlight)?
- Should the attack cooldown be displayed as a small UI element (cooldown arc) near the player?
