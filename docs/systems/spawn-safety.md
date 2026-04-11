# Spawn Safety Rules

## Summary

Monsters must never spawn on top of or immediately adjacent to the player, safe spots, or floor entrances/exits. These rules prevent cheap deaths and ensure the player always has a moment to orient when entering a floor or respawning.

## Rules

### Rule 1: Safe Spawn Radius

```
SafeSpawnRadius = 150 pixels
```

No monster may spawn within 150px of the player's current position. If a randomly chosen spawn position falls within this radius, a new position is chosen (up to 10 retries, then the spawn is deferred to the next timer tick).

**Why 150px:** The player's attack range is 78px. A 150px safe radius means the player has ~72px of buffer before enemies enter attack range — roughly 1 second of reaction time at enemy speed 75px/s (level 5).

### Rule 2: Floor Entry Invincibility

```
FloorEntryGracePeriod = 1.5 seconds
```

When the player enters a new floor (descend, ascend, or respawn), they receive 1.5 seconds of invincibility. During this window:
- The player cannot take damage
- The player CAN deal damage (allows preemptive attacks)
- A subtle visual indicator shows the grace period (sprite flickers)

**Why 1.5s:** Long enough to assess surroundings and start moving; short enough that it doesn't feel exploitable.

### Rule 3: Safe Spots are Monster-Free Zones

```
SafeSpotRadius = 200 pixels
```

Designated safe spots (floor entrance, floor exit, future checkpoints) maintain a permanent monster-free zone. Monsters that wander into this radius are pushed back to the edge. This applies to:
- The player spawn tile (center of room for MVP)
- Future: staircase up/down tiles
- Future: checkpoint crystals

### Rule 4: Boss Spawn Separation

```
BossMinDistance = 300 pixels from player spawn
```

Boss enemies (future) must spawn at least 300px from the player's spawn position. This ensures the player sees the boss before engaging and can prepare.

## Implementation Priority

For MVP:
- **Rule 1 (safe spawn radius):** Implement now
- **Rule 2 (floor entry grace):** Implement now
- **Rule 3 (safe spot zones):** Implement when proc-gen floors exist
- **Rule 4 (boss separation):** Implement when bosses exist
