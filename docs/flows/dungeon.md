# Flow: Dungeon

**Script:** `scripts/Dungeon.cs`
**Scene:** `scenes/dungeon.tscn`

## Scene Initialization

```
Dungeon._Ready():
1. Load zone theme tileset (Constants.Zones.GetZone(floor))
2. Generate floor: FloorGenerator(seed).Generate(floorNumber)
3. Paint tiles onto TileMapLayer
4. Spawn player at up-stairs position + (0, 40) Y offset
5. Start player grace period
6. Spawn initial enemies (guaranteed minimum)
7. Place stairs (up + down)
8. Set compass targets
```

## Enemy Spawning

### Initial Spawn

`SpawnInitialEnemies()`:
- Target: `Constants.Spawning.InitialEnemies` (10)
- Loop until target count or max attempts
- Each call to `SpawnEnemy()`:
  1. Get random floor position
  2. Validate: must be floor tile, > SafeSpawnRadius from player, > SafeSpawnRadius from both stairs
  3. If no safe position after retries, use any floor tile
  4. Set level (random in range per floor via FloorScaling)
  5. Set species (random from zone-specific list)
  6. Add to entities, emit `EnemySpawned`

### Ongoing Spawning

- SpawnTimer fires periodically (`Constants.Spawning.RespawnDelay`)
- If not floor-wiped AND enemy count < `Constants.Spawning.EnemySoftCap` (14):
  - Spawn one enemy

## Stairs

### Down-Stairs (Next Floor)

```
1. Player touches down-stairs Area2D
2. Guard: not transitioning
3. ScreenTransition.Play():
   message = "Floor X+1", callback = PerformFloorDescent()
4. PerformFloorDescent():
   a. GameState.FloorNumber += 1
   b. Track quest progress (depth push)
   c. Track deepest floor achievement
   d. Reset _floorWiped, _killCount
   e. Clear all enemies (QueueFree)
   f. Clear tilemap
   g. Generate new floor
   h. Respawn player at new up-stairs
   i. Restart grace period
   j. Spawn initial enemies
```

### Up-Stairs (Return)

```
1. Player touches up-stairs Area2D
2. If floor == 1:
   → Direct transition to town (ScreenTransition → Main.LoadTown)
3. If floor > 1:
   → Show AscendDialog (choose: return to town or continue)
```

## Floor Wipe

Triggers when all enemies defeated:

```
1. OnEnemyDefeated() called on each kill
2. After 0.1s delay (let QueueFree process):
   - Check: enemy count == 0 AND !_floorWiped AND _killCount >= InitialEnemies
   - If true:
     a. _floorWiped = true
     b. Stop spawn timer
     c. Record floor clear quest
     d. Increment floor_wipes achievement counter
     e. Wait 3 seconds
     f. FloorWipeDialog.Instance.ShowWipe()
3. FloorWipeDialog shows reward options:
   - Bonus gold: 20 + 10 * floor
   - Bonus XP: 30 + 15 * floor
   - Options: Descend / Stay & Farm / Return to Town
```

## Zone Theming

| Zone | Floors | Theme | Species |
|------|--------|-------|---------|
| 1 | 1-10 | Dark Dungeon | Skeleton, Bat |
| 2 | 11-20 | Cathedral | Goblin, Wolf |
| 3 | 21-30 | Volcano | Orc, Spider |
| 4 | 31-40 | Sky Temple | DarkMage, Skeleton, Orc |
| 5+ | 41+ | Nether | All species |
