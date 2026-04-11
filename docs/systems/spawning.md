# Enemy Spawn System

## Summary

Enemies spawn inside the dungeon room away from walls. An initial batch spawns when the room loads, then a periodic timer adds more up to a soft cap. Defeated enemies are replaced after a short delay. Enemy levels are derived from the current floor number. Species (skeleton or goblin) is assigned randomly. A safety-net despawn distance prevents enemies that escape room bounds.

## Current State

Fully implemented in `scripts/Dungeon.cs`. Spawning uses `Constants.Spawning` and `Constants.FloorScaling` for all values. Room sizes are 18-30 tiles (with floor-based growth). Spawn positions are validated against `IsFloorTile()` and a minimum distance from the player. Species-specific directional sprites load from `Constants.Assets.EnemySpeciesRotations`.

## Design

### Spawn Parameters

| Parameter | Value | Source | Purpose |
|-----------|-------|--------|---------|
| `InitialEnemies` | 8 | `Constants.Spawning.InitialEnemies` | Enemies spawned on room load and each floor descent |
| `EnemySoftCap` | 14 | `Constants.Spawning.EnemySoftCap` | Max active enemies before periodic spawning pauses |
| `SpawnInterval` | 2.8s | `Constants.Spawning.SpawnInterval` | Timer period for periodic spawn checks |
| `RespawnDelay` | 1.4s | `Constants.Spawning.RespawnDelay` | Delay before replacement spawn after defeat |
| `SafeSpawnRadius` | 150.0 px | `Constants.Spawning.SafeSpawnRadius` | Minimum distance from player for spawn position |
| `MaxSpawnRetries` | 10 | `Constants.Spawning.MaxSpawnRetries` | Attempts to find a valid spawn position |
| `SpawnWallMargin` | 5 tiles | `Constants.Spawning.SpawnWallMargin` | Minimum distance from wall edges for spawn positions |
| `DespawnDistance` | 800.0 px | `Constants.Spawning.DespawnDistance` | Safety net: enemy is freed if this far from player |

### Room Size

Rooms are dynamically sized per floor:

| Parameter | Value | Source |
|-----------|-------|--------|
| MinRoomSize | 18 tiles | `Constants.FloorScaling.MinRoomSize` |
| MaxRoomSize | 30 tiles | `Constants.FloorScaling.MaxRoomSize` |
| RoomGrowthPerFloors | 5 | Every 5 floors, room size range increases by 1 |
| MaxRoomGrowth | 6 | Maximum additional tiles from floor scaling |

Room width and height are independently randomized each floor:
```csharp
int floorBonus = Min(FloorNumber / RoomGrowthPerFloors, MaxRoomGrowth);
_roomWidth = RandRange(MinRoomSize + floorBonus, MaxRoomSize + floorBonus + 1);
_roomHeight = RandRange(MinRoomSize + floorBonus, MaxRoomSize + floorBonus + 1);
```

### Spawn Flow

Three spawn triggers operate independently.

#### 1. Initial Spawn (on room load and floor descent)

```csharp
for (int i = 0; i < Constants.Spawning.InitialEnemies; i++)
    SpawnEnemy();
```

- Spawns exactly 8 enemies synchronously on the first frame.
- Runs in `_Ready()` and again in `PerformFloorDescent()` after clearing old enemies.

#### 2. Periodic Spawn (timer-driven)

A repeating `SpawnTimer` node fires every 2.8 seconds:

```csharp
private void OnSpawnTimerTimeout()
{
    int enemyCount = GetTree().GetNodesInGroup(Constants.Groups.Enemies).Count;
    if (enemyCount < Constants.Spawning.EnemySoftCap)
        SpawnEnemy();
}
```

- Only spawns if count is strictly less than `EnemySoftCap` (14).
- At most one enemy per timer tick.

#### 3. Replacement Spawn (on enemy defeat)

Listens for `EventBus.EnemyDefeated` signal:

```csharp
private async void OnEnemyDefeated(Vector2 position, int tier)
{
    await ToSignal(GetTree().CreateTimer(Constants.Spawning.RespawnDelay), "timeout");
    if (IsInsideTree())
        SpawnEnemy();
}
```

- 1.4 second delay from defeat.
- Does NOT check the soft cap (can push count above cap temporarily).
- Guards against scene-change race with `IsInsideTree()` check.

### SpawnEnemy() Algorithm

```csharp
private void SpawnEnemy()
{
    // 1. Find a valid spawn position (up to MaxSpawnRetries attempts)
    //    - Must be a floor tile (not wall border)
    //    - Must be >= SafeSpawnRadius (150px) from player
    // 2. If no valid position found after retries, skip this spawn
    // 3. Instantiate enemy scene
    // 4. Set level: random in [GetMinEnemyLevel(floor), GetMaxEnemyLevel(floor)]
    // 5. Set species: random index into EnemySpeciesRotations
    // 6. Place at spawn position, add to Entities node
    // 7. Emit EventBus.EnemySpawned signal
}
```

### Spawn Position Algorithm

Enemies spawn at the inner edges of the room (not on walls), with a configurable margin:

```csharp
private Vector2 GetRandomEdgePosition()
{
    int margin = Constants.Spawning.SpawnWallMargin; // 5 tiles
    int edge = (int)(GD.Randi() % 4);
    // Compute tile coordinate on chosen edge, offset by margin from walls
    // ...
    return _tileMap.MapToLocal(coords);
}
```

For each edge, coordinates are computed within `[margin, roomSize - 1 - margin]` range, ensuring spawn positions are well inside the room.

**Spawn position validation:**
```csharp
for (int attempt = 0; attempt < Constants.Spawning.MaxSpawnRetries; attempt++)
{
    spawnPos = GetRandomEdgePosition();
    Vector2I tileCoord = _tileMap.LocalToMap(spawnPos);

    if (!IsFloorTile(tileCoord))        // Must not be a wall tile
        continue;
    if (spawnPos.DistanceTo(playerPos) < Constants.Spawning.SafeSpawnRadius)  // 150px from player
        continue;

    foundSafe = true;
    break;
}
```

`IsFloorTile()` returns true if the tile is inside the wall border (not row/col 0 or roomSize-1).

### Enemy Level Assignment

Levels are derived from the current floor number:

| Formula | Value | Source |
|---------|-------|--------|
| Min enemy level | `Max(1, floor - 1)` | `Constants.FloorScaling.GetMinEnemyLevel()` |
| Max enemy level | `floor + 2` | `Constants.FloorScaling.GetMaxEnemyLevel()` |

| Floor | Min Level | Max Level | Range |
|-------|-----------|-----------|-------|
| 1 | 1 | 3 | 1-3 |
| 3 | 2 | 5 | 2-5 |
| 5 | 4 | 7 | 4-7 |
| 10 | 9 | 12 | 9-12 |

### Enemy Species

Species is assigned randomly from the available species rotations:

```csharp
enemy.SpeciesIndex = (int)(GD.Randi() % Constants.Assets.EnemySpeciesRotations.Length);
```

Currently two species: skeleton (index 0) and goblin (index 1), each with their own directional sprite set.

### Enemy Stats by Level

Stats are level-based (not tier-based), computed from `Constants.EnemyStats`:

| Stat | Formula | Level 1 | Level 3 | Level 5 | Level 10 |
|------|---------|---------|---------|---------|----------|
| HP | `20 + level * 10` | 30 | 50 | 70 | 120 |
| Speed | `50 + level * 5` px/s | 55 | 65 | 75 | 100 |
| Damage | `2 + level * 1` | 3 | 5 | 7 | 12 |
| XP | `8 + level * 4` | 12 | 20 | 28 | 48 |

### Enemy Color (Level Gap Gradient)

Enemy sprite color is based on the gap between enemy level and player level, using a gradient with 8 anchor points:

| Gap | Color | Meaning |
|-----|-------|---------|
| -10 | #9D9D9D (grey) | Trivial |
| -6 | #4A7DFF (blue) | Low |
| -3 | #4AE8E8 (cyan) | Low-mid |
| 0 | #6BFF89 (green) | Even |
| +3 | #FFDE66 (yellow) | Mid-high |
| +6 | #F5C86B (gold) | High |
| +8 | #FF9340 (orange) | Very high |
| +10 | #FF6F6F (red) | Extreme |

Colors lerp between anchors for smooth transitions. Updates dynamically when the player levels up.

### Despawn Safety Net

In `Enemy._PhysicsProcess()`:
```csharp
if (GlobalPosition.DistanceTo(player.GlobalPosition) > Constants.Spawning.DespawnDistance)
    QueueFree();
```

If an enemy somehow escapes 800px from the player, it is freed. This prevents runaway entities.

### Floor Descent

When the player descends (`PerformFloorDescent()`):
1. All enemies in the `"enemies"` group are `QueueFree()`'d.
2. Tilemap is cleared and a new floor is generated.
3. Player is moved to the new up-stairs position.
4. Grace period is reset.
5. 8 new enemies are spawned.

### Soft Cap Behavior

The soft cap (14) limits periodic spawning but NOT replacement spawning. The count can temporarily exceed 14 when replacement spawns coincide with periodic spawns. The system self-corrects as the periodic timer stops adding until count drops below 14.

### Enemy Group Management

All enemies are added to the `Constants.Groups.Enemies` ("enemies") group in `Enemy._Ready()`. Counting uses `GetTree().GetNodesInGroup()`. Player attack targeting uses `Area2D.GetOverlappingBodies()` filtered by group.

### Edge Cases

| Scenario | Behavior |
|----------|----------|
| No valid spawn position found | `SpawnEnemy()` returns without spawning (after `MaxSpawnRetries` attempts) |
| Player is dead | Spawning continues. Timer keeps running. |
| Floor descent | All enemies freed, new batch of 8 spawned |
| `await` interrupted by scene change | `IsInsideTree()` guard prevents spawning into freed scene |
| Timer paused (tree paused) | Timer nodes pause with the tree during pause menu |

## Implementation Notes

- Spawn logic is in `Dungeon.cs` (not a separate manager). Each dungeon instance owns its spawn state.
- `EnemyScene` is loaded once as a static readonly `PackedScene` at class level.
- All enemies are children of the `Entities` Node2D for organized scene tree.
- The `_tileMap` and `_entities` references are obtained in `_Ready()`.

## Open Questions

- Should the soft cap scale with floor depth (more enemies on deeper floors)?
- Should spawn intervals decrease on deeper floors (faster spawning = harder)?
- Should there be a "spawn wave" mechanic (burst of enemies at intervals) instead of steady trickle?
- Should enemies spawn with a visual effect (fade in, portal animation) instead of appearing instantly?
