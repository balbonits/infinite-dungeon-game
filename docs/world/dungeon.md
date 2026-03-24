# Dungeon

## Summary

An infinitely descending dungeon with procedurally generated floors. Each floor has monsters that respawn forever. The dungeon caches up to 10 floors.

## Current State

The prototype has a single floor with spawning enemies. There is no floor generation, descent, or caching system yet.

## Design

### Infinite Descent

- The dungeon has no bottom — floors increase indefinitely
- Difficulty scales with depth (stronger enemies, more dangerous tiers)
- Players descend via a staircase/exit point on each floor
- Players can ascend back to previously visited floors (if cached)

### Floor Generation

Each floor is procedurally generated:
- Floor layout (walls, obstacles, open areas)
- Enemy spawn points and types
- Entrance point (from above) and exit point (to below)
- Safe spots at entrance and exit (see below)

### Seeded Generation

- Each floor has a seed that determines its layout
- Seeds allow sharing specific floors: "try floor 47, seed ABC123"
- **Seeded mode restrictions:** when replaying a seeded floor, certain benefits may be limited to prevent exploits (details TBD)

### Floor Caching

- Maximum 10 floors are cached in memory/save data at a time
- When the cache is full, the oldest floor is purged
- Purged floors regenerate with a new layout when revisited (new seed)
- Cache priority: keep the floors nearest to the player's current position

### Safe Spots

- Located at every floor entrance and exit
- Visual indicator: glowing crystal or similar landmark
- Function as auto-checkpoints — the last safe spot touched is the respawn point on death
- No enemies spawn within a radius of safe spots

### Monster Respawning

- Enemies on each floor respawn infinitely on timers
- This allows safe farming on any floor
- Respawn rate and enemy composition scale with floor depth
- Soft cap on active enemies per floor to prevent performance issues

## Open Questions

- What procedural generation algorithm should be used for floor layouts?
- How should floor difficulty scaling work (linear, exponential, stepped)?
- Should some floors have special properties (boss floors, treasure rooms)?
- How large should each floor be relative to the current game viewport?
- Should the player be able to teleport to previously visited floors from town?
