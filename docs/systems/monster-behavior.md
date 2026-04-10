# Monster Behavior System

## Summary

Monsters use a finite state machine driven by their archetype to determine movement and combat behavior. There are 5 archetypes (Melee, Ranged, Bruiser, Swarmer, Support), each with distinct aggro ranges, attack ranges, cooldowns, speed multipliers, and AI state transitions. The state machine has 9 states, with certain states (Reposition, Retreat) only available to archetypes that maintain preferred distance.

## Current State

**Spec status: LOCKED.** Implemented in `MonsterBehavior.cs` and `MonsterArchetype.cs`.

## Design

### Archetypes

| Archetype | Description | Aggro Range | Attack Range | Preferred Distance | Attack Cooldown | Speed Multiplier | Alert Duration |
|-----------|-------------|-------------|--------------|-------------------|-----------------|-----------------|----------------|
| Melee | Direct chase, close range attack | 384 | 30 | 0 (none) | 1.0s | 1.0x | 0.3s |
| Ranged | Maintains distance, projectile attacks | 512 | 320 | 256 | 1.5s | 0.9x | 0.3s |
| Bruiser | Slow, high HP, heavy hits, telegraphed | 320 | 40 | 0 (none) | 2.0s | 0.6x | 0.5s |
| Swarmer | Fast, low HP, spawns in groups | 448 | 25 | 0 (none) | 0.4s | 1.3x | 0.0s (skips) |
| Support | Buffs allies, debuffs player | 384 | 256 | 192 | 2.5s | 0.8x | 0.3s |

All range values are in pixels. Speed multipliers are applied to the monster's base movement speed.

### AI State Machine

The AI uses 9 discrete states:

| State | Description |
|-------|-------------|
| **Idle** | Default state. Monster is stationary, not aware of the player. |
| **Alert** | Player detected within aggro range. Brief pause before engaging. |
| **Chase** | Actively pursuing the player to reach attack range. |
| **Attack** | Executing an attack. Immediately transitions to Cooldown. |
| **Cooldown** | Post-attack recovery period. Duration set by archetype. |
| **Reposition** | Adjusting distance to preferred range. Ranged/Support only. |
| **Retreat** | Backing away when player is too close. Ranged/Support only. |
| **Flee** | Terminal fleeing state. Once entered, does not exit. |
| **Dead** | Monster is dead. Terminal state. |

### State Transitions

```
Idle ──(player in aggro range)──> Alert (or Chase if alert duration = 0)
Alert ──(alert timer expires)──> Chase
Chase ──(player leaves 1.5x aggro range)──> Idle
Chase ──(player within attack range)──> Attack
Attack ──(always)──> Cooldown
Cooldown ──(timer expires, has preferred distance, player too close)──> Retreat
Cooldown ──(timer expires, has preferred distance)──> Reposition
Cooldown ──(timer expires, no preferred distance)──> Chase
Reposition ──(player in attack range and not too close)──> Chase
Reposition ──(player very close, < 50% preferred distance)──> Retreat
Retreat ──(distance >= preferred distance)──> Chase
Any state ──(HP <= 0)──> Dead
```

### Archetype-Specific Behavior

#### Swarmer: Instant Aggro

Swarmers have an alert duration of 0 seconds. When a player enters their aggro range, they skip the Alert state entirely and transition directly from Idle to Chase. This makes swarmer packs feel immediately aggressive, swarming the player without hesitation.

#### Ranged and Support: Distance Management

Ranged and Support are the only archetypes with a non-zero preferred distance (256 and 192 respectively). This gives them access to two additional states:

- **Reposition** -- After an attack cooldown, if the monster has a preferred distance, it moves to maintain optimal range rather than blindly chasing.
- **Retreat** -- If the player closes to within 70% of preferred distance during Cooldown, or within 50% during Reposition, the monster actively backs away. Retreat ends when the monster reaches its preferred distance.

Melee, Bruiser, and Swarmer have preferred distance of 0, so they always return to Chase after Cooldown.

#### Bruiser: Telegraphed Attacks

Bruisers have the longest attack cooldown (2.0s) and the slowest speed (0.6x). Their alert duration is also the longest at 0.5s, giving players a window to react before the bruiser engages. Combined with their short attack range (40), bruisers are designed to be avoidable but punishing if they connect.

#### Leash Range

All archetypes disengage (return to Idle) if the player moves beyond 1.5x their aggro range while in Chase state. This prevents monsters from following indefinitely.

| Archetype | Leash Range (1.5x aggro) |
|-----------|--------------------------|
| Melee | 576 |
| Ranged | 768 |
| Bruiser | 480 |
| Swarmer | 672 |
| Support | 576 |

## Implementation Notes

- **`scripts/game/monsters/MonsterArchetype.cs`** -- Defines the `MonsterArchetype` enum (Melee, Ranged, Bruiser, Swarmer, Support), `MonsterAIState` enum (9 states), and `MonsterRarity` enum.
- **`scripts/game/monsters/MonsterBehavior.cs`** -- Static class with all behavior logic. `GetAggroRange()`, `GetAttackRange()`, `GetPreferredDistance()`, `GetAttackCooldown()`, `GetAlertDuration()`, and `GetSpeedMultiplier()` return per-archetype values. `GetNextState()` contains the full state machine transition logic.
- All methods are static and take archetype + current state parameters, keeping behavior logic stateless and testable.
- The `GetNextState()` method takes `distanceToPlayer`, `currentHP/maxHP`, `alertTimer`, and `cooldownTimer` as inputs, making the state machine fully deterministic for a given set of conditions.
