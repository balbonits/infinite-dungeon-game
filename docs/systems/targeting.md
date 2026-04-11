# Attack Targeting System

## Summary

Every attack in the game has a `TargetMode` that determines how it selects and hits targets. The combat system reads the mode from `AttackConfig` and resolves targets accordingly -- no special-case branching per skill. This spec defines the eight target modes, their parameters, three projectile behavior modifiers, and how they combine to create the full range of skill archetypes.

## Current State

**Spec status: LOCKED.** The `TargetMode` enum (8 values) and all targeting-related fields are defined in `AttackConfig`. The combat system currently implements `SingleTarget` for all three class auto-attacks (WarriorSlash, RangerArrowShot, MageMagicBolt). The remaining modes (Self, AreaOfEffect, MultiTarget, PlayerCentricAoe, Line, Cone, Homing) are defined in the enum but not yet wired into the attack execution path. Projectile behavior modifiers (Pierce, Fork, Split) are defined as fields on `AttackConfig` but not yet implemented.

## Design

### Target Modes

There are exactly eight target modes. Every `AttackConfig` sets one of these. The combat system uses the mode to determine how targets are resolved after the attack triggers.

#### 1. Self

Affects the caster only. No target selection occurs -- the effect is applied directly to the entity that initiated the attack.

**Use cases:** Buffs (Battle Focus, Quick Cast), self-heals (Mend, Second Wind, Heal), self-shields (Barrier, Earthen Armor), toggle auras that apply to self (Menacing Presence, Bulwark).

**Behavior:**
- Ignores `Range` for target selection (no target needed).
- `Range` may still be used for aura radius in cases where a Self-targeted buff creates an aura (e.g., Menacing Presence debuffs enemies around the caster -- the debuff application is a separate effect, but the buff itself targets Self).
- `MaxTargets` is always 1 (implicit).
- Never spawns a projectile (`IsProjectile` is always false).
- Visual effect plays on the caster.

**Example AttackConfig:**
```
Second Wind (Warrior self-heal):
  TargetMode    = Self
  Cooldown      = 8.0s
  IsProjectile  = false
  Effect        = None
```

---

#### 2. SingleTarget

Hits exactly one target. The target is selected by the existing auto-targeting system: nearest enemy within `Range`, or the locked target if target cycling is active (deferred to P2+).

**Use cases:** Basic auto-attacks (WarriorSlash, RangerArrowShot, MageMagicBolt), single-target spells (Lightning, Frost Bolt, Energy Blast, Drain Life), precision skills (Thrust, Power Shot, Snipe).

**Behavior:**
- Selects the nearest enemy within `Range` from `AttackRange.GetOverlappingBodies()`.
- If `IsProjectile = true`, spawns a projectile that travels toward the target position. The projectile stops and deals damage on first enemy hit (unless `PiercesTargets` is true -- see below).
- If `IsProjectile = false`, applies damage instantly to the selected target and plays the melee visual effect.
- `MaxTargets` is always 1 (implicit, ignored even if set higher).
- This is the default `TargetMode` in `AttackConfig`.

**Example AttackConfig (current):**
```
RangerArrowShot:
  TargetMode       = SingleTarget
  Range            = 250.0 px
  Cooldown         = 0.55s
  DamageMultiplier = 1.0
  IsProjectile     = true
  ProjectileSpeed  = 400.0 px/s
```

---

#### 3. AreaOfEffect

Hits all enemies within a radius around a **point on the ground**. The point is determined by the attack's delivery method: projectile-based AoE detonates at the projectile's impact location; instant AoE uses the targeted enemy's position.

**Use cases:** Fireball (projectile detonation), Quake (instant around target area), Inferno (sustained area), Void Zone (placed area).

**Behavior:**
- Target selection: the initial aim point is the nearest enemy (or locked target). This determines where the projectile flies or where the instant effect centers.
- On detonation/activation, a circle query with radius `AoeRadius` is performed at the impact point.
- All enemies within the radius take damage.
- `MaxTargets` caps how many enemies can be hit. If more enemies are in the radius than `MaxTargets`, the closest to the center are hit first.
- Projectile-based AoE: the projectile flies to the aim point and detonates on arrival (or on first enemy hit, whichever comes first). The detonation applies AoE damage at that position. The projectile is consumed on detonation regardless of `PiercesTargets`.
- Instant AoE: damage is applied at the target's position immediately, no projectile spawned.

**Damage falloff:** Damage is uniform within the AoE radius. No distance-based falloff. This keeps the system simple and predictable for the player -- if you see the blast, you got the full hit. If a future design needs falloff, it would be a new `AoeFalloff` flag, not a change to the base behavior.

**Example AttackConfig:**
```
Fireball (Mage Fire spell):
  TargetMode       = AreaOfEffect
  Range            = 200.0 px       (projectile travel range)
  AoeRadius        = 64.0 px        (explosion radius)
  MaxTargets       = 8
  Cooldown         = 1.2s
  DamageMultiplier = 1.1
  IsProjectile     = true
  ProjectileSpeed  = 280.0 px/s

Quake (Mage Earth spell, instant):
  TargetMode       = AreaOfEffect
  Range            = 150.0 px       (max cast range to target point)
  AoeRadius        = 80.0 px        (tremor radius)
  MaxTargets       = 12
  Cooldown         = 2.0s
  DamageMultiplier = 0.9
  IsProjectile     = false
```

---

#### 4. MultiTarget (Chain)

Automatically chains between multiple targets. The first target is selected normally (nearest enemy or locked target). After hitting the first target, the attack jumps to the nearest additional target within `ChainRange` of the previous target. Each chain applies the full damage (no per-bounce reduction in the base system -- individual skills can set a lower `DamageMultiplier` on chain configs if they want decay).

**Use cases:** Chain Shock (lightning jumps between enemies), Bounce Shot (Ranger ricochet), smart missiles, any "bouncing" attack.

**Behavior:**
- First target: nearest enemy within `Range`.
- After hitting the first target, find the nearest enemy within `ChainRange` of that target that has not already been hit by this attack instance.
- Repeat until `ChainCount` total targets have been hit, or no valid chain target exists.
- `ChainCount` defines the total number of targets hit (including the first). A `ChainCount` of 1 is functionally identical to `SingleTarget`.
- `ChainRange` is the maximum distance between consecutive chain targets (not from the caster).
- Each chain hit can play a visual arc/bolt effect between the previous target and the current target.
- If `IsProjectile = true`, the initial projectile flies to the first target. Subsequent chains are instant visual arcs (not additional projectiles).
- `MaxTargets` is ignored for MultiTarget -- `ChainCount` is the controlling cap.
- A target can only be hit once per chain (no looping back).

**Example AttackConfig:**
```
Chain Shock (Mage Air spell):
  TargetMode       = MultiTarget
  Range            = 200.0 px       (cast range to first target)
  ChainCount       = 5              (hits up to 5 enemies)
  ChainRange       = 96.0 px        (max jump distance between targets)
  Cooldown         = 1.5s
  DamageMultiplier = 1.0
  IsProjectile     = true
  ProjectileSpeed  = 350.0 px/s

Bounce Shot (Ranger Thrown skill):
  TargetMode       = MultiTarget
  Range            = 220.0 px
  ChainCount       = 3
  ChainRange       = 80.0 px
  Cooldown         = 0.8s
  DamageMultiplier = 0.85
  IsProjectile     = true
  ProjectileSpeed  = 380.0 px/s
```

---

#### 5. PlayerCentricAoe

Hits all enemies within a radius centered on the **caster**, not on a target point. The caster does not need to select or face a target -- the effect radiates outward from the player's position.

**Use cases:** Wide sword swings (Slash, Cleave, Sweep), whirlwind attacks, shockwaves (Quake when centered on caster), shouts (War Cry, Battle Roar), aura pulses (Radiance), ground slams.

**Behavior:**
- No target selection occurs. The caster's current position is the center.
- A circle query with radius `AoeRadius` is performed at the caster's position.
- All enemies within the radius take damage.
- `MaxTargets` caps how many enemies can be hit. If more enemies are in the radius than `MaxTargets`, the closest to the caster are hit first.
- Never spawns a projectile -- this is always an instant effect.
- `Range` on the `AttackConfig` is used only for the AttackRange Area2D detection (visual feedback showing "enemies are nearby"), not for damage calculation. `AoeRadius` determines the actual hit area.
- Can be used for non-damaging effects (shouts, auras) where `DamageMultiplier = 0` and a status effect is applied instead.

**Example AttackConfig:**
```
Cleave (Warrior Bladed skill):
  TargetMode       = PlayerCentricAoe
  AoeRadius        = 72.0 px
  MaxTargets       = 4
  Cooldown         = 0.6s
  DamageMultiplier = 0.8
  IsProjectile     = false
  Effect           = Slash
  EffectColor      = #f5c86b

War Cry (Warrior Outer skill):
  TargetMode       = PlayerCentricAoe
  AoeRadius        = 128.0 px
  MaxTargets       = 20
  Cooldown         = 10.0s
  DamageMultiplier = 0.0           (no damage, applies debuff)
  IsProjectile     = false
  Effect           = None
```

---

#### 6. Line

Hits all enemies in a straight line from the caster to max range. The line extends in the direction of the nearest enemy (or locked target). Everything in that corridor takes damage.

**Use cases:** Laser beams, charge attacks, dragon breath in a line, rail guns, penetrating energy blasts. Think Diablo 3 Disintegrate.

**Behavior:**
- Direction is determined by the nearest enemy within `Range` (or locked target). If no target is found, the attack does not fire and cooldown does not start.
- A rectangle query is performed from the caster's position along the direction vector, extending `Range` pixels long and `LineWidth` pixels wide (centered on the line).
- All enemies within the rectangle take damage.
- `MaxTargets` caps how many enemies can be hit. If more enemies are in the line than `MaxTargets`, those closest to the caster are hit first (rewarding aggressive positioning).
- Can be `IsProjectile = true` (a beam/bolt that visually travels down the line, dealing damage on contact) or `IsProjectile = false` (instant damage across the entire line, like a shockwave or ground crack).
- When `IsProjectile = true`, the projectile travels the full length of the line and damages each enemy it passes through (inherently piercing -- `PiercesTargets` is implicit for Line mode projectiles).
- `AoeRadius` is ignored. `LineWidth` is the controlling parameter.

**Parameters:**
| Field | Type | Default | Purpose |
|-------|------|---------|---------|
| `LineWidth` | `float` | `20.0` px | Width of the line corridor |

**Example AttackConfig:**
```
Flame Wall (Mage Fire spell):
  TargetMode       = Line
  Range            = 180.0 px        (line length)
  LineWidth        = 24.0 px         (corridor width)
  MaxTargets       = 10
  Cooldown         = 1.8s
  DamageMultiplier = 0.9
  IsProjectile     = false           (instant across line)
  Effect           = None

Disintegrate-style beam (Mage Light spell):
  TargetMode       = Line
  Range            = 250.0 px
  LineWidth        = 16.0 px
  MaxTargets       = 0               (unlimited)
  Cooldown         = 0.1s            (channeled, fires rapidly)
  DamageMultiplier = 0.3
  IsProjectile     = true            (beam travels visually)
  ProjectileSpeed  = 600.0 px/s
```

---

#### 7. Cone

Hits all enemies in a cone-shaped area in front of the caster. The cone points toward the nearest enemy (or locked target). The cone originates at the caster and fans outward.

**Use cases:** Breath attacks, shotgun spreads, cleave, fan of knives, frontal wave attacks. Think Diablo 3 Cone of Cold.

**Behavior:**
- Direction is determined by the nearest enemy within `Range` (or locked target). If no target is found, the attack does not fire and cooldown does not start.
- A cone query is performed from the caster's position: the cone extends `Range` pixels in the aim direction with an angular spread of `ConeAngle` degrees (total angle, not half-angle). For example, 60 degrees means 30 degrees to each side of the aim direction.
- All enemies within the cone take damage.
- `MaxTargets` caps how many enemies can be hit. If more enemies are in the cone than `MaxTargets`, those closest to the caster are hit first.
- Always instant (`IsProjectile` is always false). The visual effect is a fan/wave/burst that fills the cone shape.
- `AoeRadius` and `LineWidth` are ignored. `Range` determines the cone's reach; `ConeAngle` determines its spread.

**Parameters:**
| Field | Type | Default | Purpose |
|-------|------|---------|---------|
| `ConeAngle` | `float` | `60.0` degrees | Total angular width of the cone |

**Example AttackConfig:**
```
Tidal Wave (Mage Water spell):
  TargetMode       = Cone
  Range            = 120.0 px        (cone reach)
  ConeAngle        = 90.0            (wide frontal wave)
  MaxTargets       = 8
  Cooldown         = 2.0s
  DamageMultiplier = 0.85
  IsProjectile     = false
  Effect           = None

Fan Throw (Ranger Thrown skill):
  TargetMode       = Cone
  Range            = 150.0 px
  ConeAngle        = 60.0            (narrower spread)
  MaxTargets       = 5
  Cooldown         = 0.9s
  DamageMultiplier = 0.6
  IsProjectile     = false
  Effect           = Slash
  EffectColor      = #c0c0c0
```

**Design note:** Fan Throw was previously listed as PlayerCentricAoe in the skill mapping table. Cone is a better fit because the knives are thrown in a forward arc, not radially around the player. PlayerCentricAoe remains correct for 360-degree effects like Cleave and War Cry.

---

#### 8. Homing

A projectile that tracks and follows a single target. Unlike standard projectiles that fly in a straight line toward the target's initial position, a Homing projectile continuously adjusts its heading toward the target's current position. Slower than straight projectiles but nearly guaranteed to hit a moving target.

**Use cases:** Seeking missiles, tracking bolts, guided arrows, heat-seeking fireballs. Think Diablo 2 Guided Arrow.

**Behavior:**
- Target selection: nearest enemy within `Range` (or locked target). If no target is found, the attack does not fire and cooldown does not start.
- Spawns a projectile that travels at `ProjectileSpeed` toward the target. Each physics frame, the projectile rotates toward the target by up to `HomingTurnSpeed` radians per second.
- A higher `HomingTurnSpeed` means sharper turns (more aggressive tracking). A lower value means wider arcs (the projectile can be outmaneuvered by fast enemies).
- On contact with the target (or any enemy in its path), the projectile applies damage and is consumed.
- If the target dies before the projectile arrives, the projectile continues toward the target's last known position and despawns if it reaches max `Range` distance traveled without hitting anything.
- `IsProjectile` is always `true` (implicit -- homing has no meaning without a projectile).
- `MaxTargets` is always 1 (implicit). To combine homing with multi-hit, use Homing + Pierce or Homing + Fork (see Projectile Behaviors and Combination Examples below).

**Parameters:**
| Field | Type | Default | Purpose |
|-------|------|---------|---------|
| `HomingTurnSpeed` | `float` | `5.0` rad/s | Maximum angular rotation per second toward target |

**Homing turn speed reference values:**
| Value | Behavior | Feel |
|-------|----------|------|
| 2.0 | Gentle curves, can miss nimble targets | Slow missile, dodgeable |
| 5.0 | Moderate tracking, hits most targets | Default, reliable |
| 10.0 | Aggressive tracking, very hard to dodge | Lock-on missile |
| 15.0+ | Near-instant correction, practically a straight line | Guaranteed hit |

**Example AttackConfig:**
```
Guided Arrow (Ranger Drawn skill):
  TargetMode       = Homing
  Range            = 300.0 px        (max travel distance)
  HomingTurnSpeed  = 6.0 rad/s       (moderate-aggressive tracking)
  Cooldown         = 0.8s
  DamageMultiplier = 0.9             (slight penalty for guaranteed hit)
  IsProjectile     = true
  ProjectileSpeed  = 350.0 px/s      (slower than normal arrow)

Seeking Fireball (Mage Fire spell):
  TargetMode       = Homing
  Range            = 220.0 px
  HomingTurnSpeed  = 4.0 rad/s       (wider arcs, more dramatic)
  AoeRadius        = 48.0 px         (explodes on impact)
  MaxTargets       = 6               (AoE on detonation)
  Cooldown         = 1.5s
  DamageMultiplier = 1.0
  IsProjectile     = true
  ProjectileSpeed  = 250.0 px/s
```

**Homing + AoE interaction:** When a Homing projectile has `AoeRadius > 0`, it tracks the target and detonates on impact just like an AreaOfEffect projectile -- but it follows the target instead of flying to the target's initial position. The AoE detonation uses the standard AoE damage resolution (circle query at impact, MaxTargets cap, closest-to-center priority).

---

### Projectile Behaviors

These are modifiers that can apply to **any projectile-based attack** regardless of target mode. They modify what happens when a projectile hits a target or travels through the world. They are fields on `AttackConfig`, not separate target modes.

A single projectile can have multiple behaviors active simultaneously (e.g., Pierce + Fork), though some combinations are redundant or nonsensical -- see the interaction table below.

#### PiercesTargets (Pierce)

**Type:** `bool` (default: `false`)

When `true`, a projectile passes through enemies it hits instead of being consumed on the first hit. Each enemy in the projectile's path takes damage independently. The projectile continues until it reaches max `Range` or hits `MaxTargets` enemies.

Inspired by Path of Exile's Pierce Support gem.

**Interactions by target mode:**
- **SingleTarget + Pierce:** The projectile flies its full `Range` and damages every enemy it passes through, up to `MaxTargets`. Effectively turns a single-target shot into a penetrating line. Used for piercing arrows, penetrating bolts.
- **Homing + Pierce:** The projectile tracks and hits the primary target, then continues in its current direction of travel (no longer homing), piercing through additional enemies in that line.
- **Line + Pierce:** Redundant. Line mode projectiles inherently pierce all enemies in the corridor. `PiercesTargets` is ignored.
- **AreaOfEffect + Pierce:** Ignored. AoE projectiles always detonate at the aim point.
- **MultiTarget + Pierce:** The chain projectile pierces through each chain target instead of stopping, damaging anything between chain hops as well.
- **Self / PlayerCentricAoe / Cone:** N/A (no projectile).

**Example AttackConfig:**
```
Piercing Arrow (Ranger Drawn skill):
  TargetMode       = SingleTarget
  Range            = 280.0 px
  MaxTargets       = 5              (hits up to 5 enemies in a line)
  Cooldown         = 0.7s
  DamageMultiplier = 0.7
  IsProjectile     = true
  PiercesTargets   = true
  ProjectileSpeed  = 420.0 px/s
```

---

#### ForkCount (Fork)

**Type:** `int` (default: `0`)

On the first enemy hit, the projectile splits into 2 new projectiles at diverging angles (one clockwise, one counter-clockwise from the original travel direction). The fork angle is fixed at 30 degrees per side (60 degrees total spread). Each forked projectile can hit a new target but cannot hit the original target again.

`ForkCount` defines how many times forking can occur. A value of 1 means the projectile forks once (on first hit, creating 2 new projectiles). A value of 2 means each forked projectile can fork again on its next hit (creating up to 4 projectiles total from the second fork). Values above 2 are not recommended -- exponential projectile growth becomes visually chaotic and computationally expensive.

Inspired by Path of Exile's Fork Support gem.

**Behavior:**
- Fork only triggers on the **first hit** of each projectile instance. After forking, the original projectile is consumed.
- Each forked projectile inherits the parent's remaining `Range`, `DamageMultiplier`, `ProjectileSpeed`, and any other behaviors (Pierce, etc.).
- Forked projectiles can hit enemies that the parent already hit (the "no repeat" rule applies only within a single projectile's path, not across forks).
- If `PiercesTargets` is also true, the original projectile pierces through the first target AND forks, creating a pierce-through-and-split effect.
- `ForkCount = 0` means no forking (default).
- Does not apply to non-projectile attacks.

**Interactions by target mode:**
- **SingleTarget + Fork:** Projectile hits first enemy, forks into 2 that seek new targets in a V-pattern.
- **Homing + Fork:** Homing projectile hits tracked target, forks into 2 non-homing projectiles at angles.
- **AreaOfEffect + Fork:** Ignored. AoE projectiles detonate, they do not fork.
- **MultiTarget + Fork:** The initial chain projectile forks on first hit. Forked projectiles do not continue the chain -- they fly independently.
- **Line + Fork:** Ignored. Line projectiles pierce inherently and do not fork.
- **Self / PlayerCentricAoe / Cone:** N/A.

**Example AttackConfig:**
```
Splitting Bolt (Mage Light spell):
  TargetMode       = SingleTarget
  Range            = 200.0 px
  ForkCount        = 1               (forks once into 2)
  Cooldown         = 1.0s
  DamageMultiplier = 0.9
  IsProjectile     = true
  ProjectileSpeed  = 320.0 px/s
```

---

#### SplitCount (Split)

**Type:** `int` (default: `0`)

On the first enemy hit, the projectile creates `SplitCount` new projectiles, each aimed at the nearest distinct enemy that has not already been targeted by this attack instance. Unlike Fork (which fires at fixed angles), Split intelligently targets nearby enemies.

Inspired by Path of Exile's Split Arrow.

**Behavior:**
- Split triggers on the **first hit**. The original projectile is consumed after splitting.
- The system finds the `SplitCount` nearest enemies within `ChainRange` of the impact point (reusing the `ChainRange` field) that have not been hit by this attack.
- One new projectile is spawned aimed at each valid target. If fewer valid targets exist than `SplitCount`, only that many projectiles are spawned.
- Each split projectile inherits the parent's `Range` (remaining), `DamageMultiplier`, `ProjectileSpeed`, and other behaviors.
- Split projectiles can themselves pierce (if `PiercesTargets` is true) but cannot split again (split is a one-time event per attack instance, preventing infinite recursion).
- `SplitCount = 0` means no splitting (default).
- Does not apply to non-projectile attacks.

**Interactions by target mode:**
- **SingleTarget + Split:** Projectile hits first enemy, splits to N new projectiles toward nearby enemies. A smart-targeting multi-hit from a single shot.
- **Homing + Split:** Homing projectile hits tracked target, splits into non-homing projectiles toward nearby enemies.
- **AreaOfEffect + Split:** Ignored. AoE projectiles detonate.
- **MultiTarget + Split:** Ignored. Chain logic handles multi-targeting already. Split would be redundant.
- **Line + Split:** Ignored. Line projectiles do not split.
- **Self / PlayerCentricAoe / Cone:** N/A.

**Example AttackConfig:**
```
Split Arrow (Ranger Drawn skill):
  TargetMode       = SingleTarget
  Range            = 260.0 px
  SplitCount       = 3               (splits into 3 targeting arrows)
  ChainRange       = 100.0 px        (search radius for split targets)
  Cooldown         = 0.9s
  DamageMultiplier = 0.75
  IsProjectile     = true
  ProjectileSpeed  = 400.0 px/s
```

---

#### Projectile Behavior Interaction Matrix

This table shows which projectile behaviors are meaningful for each target mode.

| Behavior | SingleTarget | AreaOfEffect | MultiTarget | PlayerCentricAoe | Self | Line | Cone | Homing |
|----------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| **Pierce** | Yes | Ignored | Yes | N/A | N/A | Implicit | N/A | Yes |
| **Fork** | Yes | Ignored | Partial | N/A | N/A | Ignored | N/A | Yes |
| **Split** | Yes | Ignored | Ignored | N/A | N/A | Ignored | N/A | Yes |

"Partial" means the behavior applies to the initial projectile but does not extend to the chain mechanic.

---

### MaxTargets

**Type:** `int` (default: `1`)

Hard cap on how many entities a single attack instance can damage.

**Interactions by target mode:**
- **Self:** Always 1 (implicit).
- **SingleTarget:** Always 1 (implicit), unless `PiercesTargets` is true -- then `MaxTargets` limits pierce count.
- **AreaOfEffect:** Limits how many enemies in the blast radius take damage. Closest to center are prioritized.
- **MultiTarget:** Ignored. `ChainCount` is the cap for chain attacks.
- **PlayerCentricAoe:** Limits how many enemies around the caster take damage. Closest to caster are prioritized.
- **Line:** Limits how many enemies in the corridor take damage. Closest to caster are prioritized.
- **Cone:** Limits how many enemies in the cone take damage. Closest to caster are prioritized.
- **Homing:** Always 1 for the projectile itself (implicit). If `AoeRadius > 0`, MaxTargets limits the AoE detonation.

---

### Combination Examples

Target modes and projectile behaviors combine to create the full range of skill archetypes. The following examples show how a small number of building blocks produce diverse combat feel without special-case code.

#### SingleTarget + Pierce = Piercing Arrow
A standard arrow that passes through every enemy in its path. Fires at one target, damages everything between the caster and max range. Great for tight corridors where enemies line up.
```
  TargetMode     = SingleTarget
  PiercesTargets = true
  MaxTargets     = 5
```

#### SingleTarget + Fork = Splitting Bolt
A magic bolt that hits one enemy, then forks into two bolts at angles. The forked bolts seek new targets in a V-pattern. Effective for dealing with small groups from range.
```
  TargetMode = SingleTarget
  ForkCount  = 1
```

#### SingleTarget + Split = Smart Scatter Shot
An arrow that hits one enemy, then splits into 3 seeking arrows toward nearby enemies. Like Fork but the split projectiles are aimed intelligently rather than at fixed angles. The Ranger's crowd-control answer.
```
  TargetMode = SingleTarget
  SplitCount = 3
  ChainRange = 100.0 px
```

#### AreaOfEffect + Homing = Seeking Fireball
A fireball that tracks a moving target, then explodes on impact. Combines the guaranteed-hit nature of Homing with the area damage of AoE. The Mage's "you cannot run from this" spell.
```
  TargetMode      = Homing
  AoeRadius       = 48.0 px
  MaxTargets      = 6
  HomingTurnSpeed = 4.0 rad/s
```

#### MultiTarget + Pierce = Chain Lightning That Also Pierces
Chain lightning that jumps between targets AND passes through enemies between chain hops. In a dense pack, this hits nearly everything -- the chain selects targets, and the pierce fills in the gaps.
```
  TargetMode     = MultiTarget
  ChainCount     = 5
  ChainRange     = 96.0 px
  PiercesTargets = true
```

#### Homing + Fork = Guided Splitting Missile
A homing missile tracks its target, hits it, then forks into two non-homing projectiles at angles. Guarantees the first hit, then adds bonus splash for nearby enemies.
```
  TargetMode      = Homing
  HomingTurnSpeed = 6.0 rad/s
  ForkCount       = 1
```

#### Line + AoeRadius = Flame Wall With Lingering Explosion
A beam attack where each point along the line also has a small AoE splash radius, catching enemies slightly outside the narrow corridor. (Future consideration -- requires per-hit AoE logic on Line mode.)

#### Cone + High ConeAngle = Near-360 Blast
A cone with ConeAngle = 300 degrees covers nearly all directions, creating an almost-circular blast centered on the caster (similar to PlayerCentricAoe but with a blind spot behind the player). Useful for a "desperate sweep" ability.
```
  TargetMode = Cone
  ConeAngle  = 300.0
  Range      = 80.0 px
  MaxTargets = 10
```

---

### AttackConfig Targeting Fields Summary

All targeting parameters live on the `AttackConfig` record. No targeting data exists outside of it. The combat system reads these fields to resolve every attack.

| Field | Type | Default | Used By |
|-------|------|---------|---------|
| `TargetMode` | `TargetMode` enum | `SingleTarget` | All attacks |
| `Range` | `float` | -- | All modes (detection range / cast range / projectile max travel / line length / cone reach) |
| `AoeRadius` | `float` | `0` | AreaOfEffect, PlayerCentricAoe, Homing (detonation) |
| `MaxTargets` | `int` | `1` | AreaOfEffect, PlayerCentricAoe, Line, Cone, SingleTarget+Pierce, Homing+AoE |
| `ChainCount` | `int` | `1` | MultiTarget |
| `ChainRange` | `float` | `0` | MultiTarget, Split (search radius for split targets) |
| `LineWidth` | `float` | `20.0` px | Line |
| `ConeAngle` | `float` | `60.0` degrees | Cone |
| `HomingTurnSpeed` | `float` | `5.0` rad/s | Homing |
| `PiercesTargets` | `bool` | `false` | Any projectile-based mode (see interaction matrix) |
| `ForkCount` | `int` | `0` | Any projectile-based mode (see interaction matrix) |
| `SplitCount` | `int` | `0` | Any projectile-based mode (see interaction matrix) |
| `IsProjectile` | `bool` | `false` | SingleTarget, AreaOfEffect, MultiTarget, Line; always true for Homing |

### Target Resolution Flow

The combat system resolves targets in a single unified path:

```
1. Read AttackConfig.TargetMode
2. Branch on mode:

   SELF:
     -> Apply effect to caster
     -> Done

   SINGLE_TARGET:
     -> Find nearest enemy in Range (or locked target)
     -> If no target, abort
     -> If IsProjectile: spawn projectile toward target
        -> On hit: apply damage
        -> If PiercesTargets: continue through, damage each enemy (up to MaxTargets)
        -> If ForkCount > 0: consume projectile, spawn 2 forked projectiles at +/-30 deg
        -> If SplitCount > 0: consume projectile, spawn N projectiles toward nearest enemies
        -> Else: consume projectile
     -> If !IsProjectile: apply damage instantly to target
     -> Done

   AREA_OF_EFFECT:
     -> Find nearest enemy in Range (aim point)
     -> If no target, abort
     -> If IsProjectile: spawn projectile toward aim point
        -> On arrival/impact: circle query at impact position, radius = AoeRadius
     -> If !IsProjectile: circle query at target position, radius = AoeRadius
     -> Sort results by distance from center
     -> Apply damage to first MaxTargets results
     -> Done

   MULTI_TARGET:
     -> Find nearest enemy in Range (first target)
     -> If no target, abort
     -> If IsProjectile: spawn projectile toward first target
        -> On hit: apply damage, begin chain
     -> If !IsProjectile: apply damage instantly to first target, begin chain
     -> Chain loop:
        -> From last-hit target, find nearest unhit enemy within ChainRange
        -> If PiercesTargets: projectile pierces through to chain target (damaging in-between enemies)
        -> Apply damage, play chain visual
        -> Repeat until ChainCount reached or no valid target
     -> Done

   PLAYER_CENTRIC_AOE:
     -> Circle query at caster position, radius = AoeRadius
     -> If no results, abort (attack still goes on cooldown)
     -> Sort results by distance from caster
     -> Apply damage to first MaxTargets results
     -> Done

   LINE:
     -> Find nearest enemy in Range (aim direction)
     -> If no target, abort
     -> Compute direction vector from caster to target
     -> Rectangle query: origin = caster, length = Range, width = LineWidth, along direction
     -> Sort results by distance from caster
     -> Apply damage to first MaxTargets results
     -> If IsProjectile: spawn projectile that travels the line visually (inherent pierce)
     -> If !IsProjectile: instant damage across entire line
     -> Done

   CONE:
     -> Find nearest enemy in Range (aim direction)
     -> If no target, abort
     -> Compute direction vector from caster to target
     -> Cone query: origin = caster, reach = Range, spread = ConeAngle degrees
     -> Filter to enemies within the cone angle of the aim direction
     -> Sort results by distance from caster
     -> Apply damage to first MaxTargets results
     -> Play cone visual effect
     -> Done

   HOMING:
     -> Find nearest enemy in Range (or locked target)
     -> If no target, abort
     -> Spawn homing projectile toward target
     -> Each physics frame: rotate heading toward target by up to HomingTurnSpeed rad/s
     -> On hit:
        -> If AoeRadius > 0: circle query at impact, apply AoE damage (MaxTargets cap)
        -> Else: apply damage to hit target
        -> If ForkCount > 0: spawn 2 forked non-homing projectiles at +/-30 deg
        -> If SplitCount > 0: spawn N non-homing projectiles toward nearest enemies
        -> If PiercesTargets: continue in current direction (no longer homing), pierce remaining
        -> Else: consume projectile
     -> If target dies mid-flight: continue to last known position, then fly straight until Range
     -> Done
```

### How Skills Map to Target Modes

The following table shows representative skills from each class mapped to their target mode. This is not exhaustive -- every skill's `AttackConfig` will define its mode. This demonstrates coverage across classes.

| Skill | Class | Target Mode | Proj. Behavior | Notes |
|-------|-------|-------------|----------------|-------|
| Slash (auto-attack) | Warrior | SingleTarget | -- | Basic melee, instant |
| Arrow Shot (auto-attack) | Ranger | SingleTarget | -- | Basic ranged, projectile |
| Magic Bolt (auto-attack) | Mage | SingleTarget | -- | Basic ranged, projectile |
| Second Wind | Warrior | Self | -- | Self-heal over time |
| Mend | Mage | Self | -- | Self-heal, low cooldown |
| Barrier | Mage | Self | -- | Self-shield |
| Battle Focus | Warrior | Self | -- | Self-buff |
| Fireball | Mage | AreaOfEffect | -- | Projectile + explosion |
| Quake | Mage | AreaOfEffect | -- | Instant, centered on target area |
| Inferno | Mage | AreaOfEffect | -- | Sustained area, placed on ground |
| Void Zone | Mage | AreaOfEffect | -- | Damage zone, placed on ground |
| Chain Shock | Mage | MultiTarget | -- | Lightning chains between enemies |
| Bounce Shot | Ranger | MultiTarget | -- | Ricochet between targets |
| Cleave | Warrior | PlayerCentricAoe | -- | Wide swing around player |
| Sweep (Blunt) | Warrior | PlayerCentricAoe | -- | Low arc around player |
| War Cry | Warrior | PlayerCentricAoe | -- | AoE debuff shout |
| Battle Roar | Warrior | PlayerCentricAoe | -- | AoE slow shout |
| Radiance | Mage | PlayerCentricAoe | -- | Light burst around caster |
| Flame Wall | Mage | Line | -- | Fire corridor, instant damage |
| Energy Blast | Mage | Line | -- | Concentrated beam, channel |
| Tidal Wave | Mage | Cone | -- | Wide frontal wave |
| Fan Throw | Ranger | Cone | -- | Thrown knives in forward arc |
| Gust | Mage | Cone | -- | Knockback wind blast |
| Guided Arrow | Ranger | Homing | -- | Tracking arrow, guaranteed hit |
| Seeking Fireball | Mage | Homing | -- | Tracks target, AoE on impact |
| Piercing Arrow | Ranger | SingleTarget | Pierce | Passes through enemies in a line |
| Split Arrow | Ranger | SingleTarget | Split | Hits one, splits to nearby targets |
| Splitting Bolt | Mage | SingleTarget | Fork | Hits one, forks into two bolts |

### Edge Cases

**No valid target:** If the required target (SingleTarget, AreaOfEffect, MultiTarget, Line, Cone, Homing) cannot be found, the attack does not fire and the cooldown does not start. PlayerCentricAoe always fires (it does not require a target), but if no enemies are in range, no damage is applied and the cooldown still starts.

**Target dies mid-chain (MultiTarget):** If a chain target dies before the chain visual reaches it, the chain still counts that target as "hit" and continues seeking the next. The dead target takes no damage (already dead), but a chain slot is consumed.

**Projectile outlives target (SingleTarget, Homing):** If the target dies or moves before the projectile arrives, the projectile continues to its original aim position (SingleTarget) or last known position (Homing). For SingleTarget without PiercesTargets, the projectile despawns if it reaches max distance without hitting anything. For PiercesTargets, it still damages anything in its path. For Homing, the projectile stops tracking and flies straight from its current heading until max Range.

**AoE on single enemy:** If only one enemy is in the blast radius, only that enemy is hit. AoE attacks do not require multiple targets to be useful.

**MaxTargets = 0:** Treated as unlimited. All enemies in the AoE/pierce/line/cone path are hit.

**Self-targeting while no enemies present:** Self-targeted abilities always work regardless of nearby enemies. The caster is always a valid target for Self mode.

**Line with no enemies in corridor:** If the nearest enemy sets the aim direction but no enemies are actually inside the LineWidth corridor, no damage is applied. The attack still fires (visual plays, cooldown starts) because the direction was valid.

**Cone angle extremes:** `ConeAngle = 0` is invalid and treated as a minimum of 1 degree. `ConeAngle = 360` hits all enemies within Range regardless of direction (functionally identical to PlayerCentricAoe at the same radius). Values above 360 are clamped to 360.

**Homing projectile in tight spaces:** A homing projectile with low `HomingTurnSpeed` may orbit a target it cannot reach (e.g., behind a wall). The projectile despawns when it exceeds max `Range` distance traveled, preventing infinite loops.

**Fork/Split with no nearby enemies:** If a projectile with Fork or Split hits an enemy but there are no other valid targets nearby, the fork/split simply does not occur. The original hit still applies damage normally.

**Fork + Pierce simultaneously:** The original projectile pierces through the first target AND spawns 2 forked projectiles. The original continues on its line; the forks diverge at angles. All three projectiles can hit additional enemies.

**Split recursion prevention:** A split projectile cannot split again. This is a hard rule to prevent exponential projectile spawning. Fork projectiles CAN fork again if `ForkCount > 1`, but this is bounded by the ForkCount value.

---

### Future Considerations

These are planned extensions that do not affect the current spec. They are documented here so the targeting system is designed with room for them.

**Friendly targeting:** Support NPCs, familiars, and pets will eventually need to receive beneficial effects (heals, buffs). This requires a `TargetTeam` parameter on `AttackConfig`:
- `TargetTeam.Enemy` (default) -- current behavior, targets hostile entities
- `TargetTeam.Friendly` -- targets allies (self, NPCs, pets, familiars)
- `TargetTeam.All` -- targets both (rare, for effects that hit everything in radius)

The eight `TargetMode` values remain unchanged. `TargetTeam` controls *which* entities are valid targets; `TargetMode` controls *how* they are selected. This keeps the two concerns orthogonal.

**Target cycling (L1/R1):** Deferred to P2+. When implemented, the "nearest enemy" selection in SingleTarget, AreaOfEffect, MultiTarget, Line, Cone, and Homing will be overridden by the player's locked target. PlayerCentricAoe and Self are unaffected by target cycling.

**Cursor/ground targeting:** Some AreaOfEffect and Line skills may eventually target a point on the ground chosen by the player (e.g., placing a Flame Wall at a specific location) rather than auto-targeting the nearest enemy. This would add a `TargetPoint` variant to the aim-point resolution step. The damage resolution logic itself does not change.

**Status effect application:** Currently `AttackConfig` only defines damage. Skills that apply debuffs (War Cry, Curse, Freeze) or buffs (Barrier, Battle Focus) will need a status effect system. The targeting system resolves *which entities are affected* -- the status effect system defines *what happens to them*. These are separate concerns.

**Channeled attacks:** Line and Homing modes are natural candidates for channeled abilities (sustained beam, continuous tracking). A future `IsChanneled` flag and `ChannelDuration` field would allow the attack to fire continuously while the player holds the button, with per-tick damage instead of per-attack damage. The targeting resolution per tick is identical to the non-channeled version.

**Projectile behavior stacking from equipment:** Fork, Split, and Pierce could eventually come from equipment affixes (e.g., a quiver that adds Pierce to all arrow attacks). The system already supports this -- the equipment modifier would set the corresponding field on the computed `AttackConfig` before the combat system reads it.

## Acceptance Criteria

**Target Modes (core):**
- [ ] Every `AttackConfig` has exactly one `TargetMode` -- no attack has ambiguous target resolution
- [ ] Self-targeted skills apply effects to the caster without requiring a nearby enemy
- [ ] SingleTarget selects the nearest enemy and damages exactly one target
- [ ] AreaOfEffect damages all enemies within AoeRadius of the impact point, capped by MaxTargets
- [ ] AreaOfEffect with IsProjectile detonates at the projectile's impact position
- [ ] AreaOfEffect without IsProjectile detonates at the targeted enemy's position
- [ ] MultiTarget chains to ChainCount total targets, each within ChainRange of the previous
- [ ] MultiTarget does not hit the same target twice in one chain
- [ ] PlayerCentricAoe damages all enemies within AoeRadius of the caster, capped by MaxTargets
- [ ] PlayerCentricAoe fires even with no enemies present (goes on cooldown, no damage applied)

**Target Modes (new):**
- [ ] Line damages all enemies in a rectangle corridor (Range x LineWidth) in the aim direction, capped by MaxTargets
- [ ] Line with IsProjectile spawns a visual beam that travels the corridor length
- [ ] Line without IsProjectile applies instant damage across the entire corridor
- [ ] Cone damages all enemies within a cone (Range reach, ConeAngle spread) in the aim direction, capped by MaxTargets
- [ ] Cone is always instant (no projectile)
- [ ] ConeAngle is clamped to [1, 360] degrees
- [ ] Homing spawns a projectile that tracks the target each physics frame, rotating by up to HomingTurnSpeed rad/s
- [ ] Homing projectile stops tracking and flies straight when target dies
- [ ] Homing + AoeRadius > 0 detonates as AoE on impact

**Projectile Behaviors:**
- [ ] PiercesTargets causes the projectile to pass through enemies and continue, up to MaxTargets
- [ ] ForkCount > 0 causes the projectile to spawn 2 forked projectiles at +/-30 degrees on first hit
- [ ] Forked projectiles inherit remaining Range, DamageMultiplier, and ProjectileSpeed
- [ ] SplitCount > 0 causes the projectile to spawn N projectiles toward nearest distinct enemies on first hit
- [ ] Split projectiles cannot split again (recursion prevention)
- [ ] Fork/Split with no nearby valid targets still applies damage to the initial hit target
- [ ] Pierce + Fork on the same projectile: original pierces through AND forks spawn

**General:**
- [ ] MaxTargets = 0 means unlimited targets
- [ ] Attacks with no valid target (except PlayerCentricAoe and Self) do not fire and do not start cooldown
- [ ] No class-specific or skill-specific branching in the target resolution code path

## Implementation Notes

- The `TargetMode` enum (8 values) and all targeting fields already exist on `AttackConfig` (see `scripts/logic/AttackConfig.cs` and `scripts/logic/TargetMode.cs`). Line, Cone, and Homing are already in the enum. `PiercesTargets`, `ForkCount`, `SplitCount`, `LineWidth`, `ConeAngle`, and `HomingTurnSpeed` are already fields on `AttackConfig`. No schema changes needed.
- Target resolution should be a single method (e.g., `ResolveTargets(AttackConfig, Vector2 casterPosition, Node2D lockedTarget)`) that returns a list of targets. The combat system calls this once, then applies damage/effects to the returned list. For Homing, the method returns the single tracked target; the projectile handles the tracking behavior.
- **AoE circle queries:** Use Godot's `PhysicsDirectSpaceState2D.IntersectShape()` with a `CircleShape2D` at the desired position. Preferred for one-shot queries over `GetOverlappingBodies()`.
- **Line rectangle queries:** Use `PhysicsDirectSpaceState2D.IntersectShape()` with a `RectangleShape2D` (size = Range x LineWidth), positioned at the midpoint of the corridor, rotated to the aim direction.
- **Cone queries:** No built-in cone shape in Godot. Use a circle query at caster position with radius = Range, then filter results by checking if the angle from caster to each enemy is within ConeAngle/2 of the aim direction. Use `Vector2.AngleTo()` for the angle check.
- **Homing projectile:** Extend the existing `Projectile` class with a homing mode. Each `_PhysicsProcess` frame, compute the angle to the target and rotate the velocity vector toward it by `HomingTurnSpeed * delta` radians. Use `Mathf.MoveToward` on the angle for smooth tracking.
- **Fork/Split spawning:** When a projectile with ForkCount or SplitCount hits a target, it calls a factory method to spawn child projectiles. Child projectiles should carry a flag preventing further splits (for Split) or a decremented ForkCount (for Fork).
- Chain visuals (MultiTarget) should use a line or arc drawn between consecutive targets, tinted to the attack's `EffectColor`, with a short tween (0.08-0.12s travel time per hop).
- For PlayerCentricAoe, use a separate physics query at the caster's position with `AoeRadius`.

## Open Questions

None. Spec is locked.
