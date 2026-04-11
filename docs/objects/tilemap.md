# TileMap

## Summary

The dungeon floor is rendered using a TileMapLayer node with an isometric TileSet. The TileSet is created programmatically in `Dungeon.cs` (not loaded from a `.tres` file) and contains multiple tile types: 4 floor variations (randomly mixed), 1 wall tile with rectangular collision, and stairs objects (up/down) with collision and area triggers. Rooms are procedurally generated with dimensions scaling by floor depth.

## Current State

Implemented. `scripts/Dungeon.cs` handles tileset setup, procedural room generation, stairs creation, enemy spawning, and floor transitions. Tiles use 64x64 texture art with a 64x32 isometric footprint. Four floor tile variations are randomly mixed for visual variety. Stairs are physical Node2D objects with sprites, collision bodies, and trigger areas.

## Design

### TileSet Configuration

The TileSet is created programmatically in `Dungeon.cs.SetupTileset()`:

| Property | Value | Description |
|----------|-------|-------------|
| `tile_shape` | `TileSet.TILE_SHAPE_ISOMETRIC` | Isometric diamond rendering mode. |
| `tile_size` | `Vector2I(64, 32)` | 64px wide by 32px tall isometric footprint. |
| Physics layers | 1 layer (index 0) | Wall collision on layer 1 (bit 0). |

**Texture region size:** `Vector2I(64, 64)`. The source textures are 64x64 pixels, rendered into the 64x32 isometric footprint by the TileSet. This gives walls and floors visual height.

---

### Tile Types

**Floor Tiles (4 variations):**

Each floor texture is loaded as a separate `TileSetAtlasSource`. A random variation is selected per tile during room painting.

| Path | Description |
|------|-------------|
| `res://assets/tiles/dungeon/floor.png` | Base floor |
| `res://assets/tiles/dungeon/floor_cracked.png` | Cracked variant |
| `res://assets/tiles/dungeon/floor_flagstone.png` | Flagstone variant |
| `res://assets/tiles/dungeon/floor_worn.png` | Worn variant |

Floor tiles have no physics collision (walkable).

**Wall Tile:**

| Path | Description |
|------|-------------|
| `res://assets/tiles/dungeon/wall.png` | Single wall texture |

Wall tiles have a rectangular collision polygon for smooth wall sliding.

---

### Wall Collision Polygon

Wall tiles use a **rectangular** collision polygon (not diamond-shaped):

```
Points: [(-32, -16), (32, -16), (32, 16), (-32, 16)]
```

This is a full 64x32 rectangle matching the tile footprint. The rectangular shape allows characters to slide smoothly along walls without catching on diamond corners. Both the player and enemies collide with walls via `MoveAndSlide()`.

Defined in `Constants.Tiles.WallCollisionPolygon`.

---

### Procedural Room Generation

Rooms are single rectangular chambers with wall borders and floor interiors. Dimensions grow with floor depth.

**Room size formula:**

```
floorBonus = min(floorNumber / 5, 6)
width  = random(18 + floorBonus, 30 + floorBonus + 1)
height = random(18 + floorBonus, 30 + floorBonus + 1)
```

| Constant | Value | Purpose |
|----------|-------|---------|
| `MinRoomSize` | `18` | Minimum room dimension in tiles. |
| `MaxRoomSize` | `30` | Maximum room dimension in tiles. |
| `RoomGrowthPerFloors` | `5` | Every 5 floors, bonus increases by 1. |
| `MaxRoomGrowth` | `6` | Cap on floor bonus (reached at floor 30). |

| Floor | Bonus | Size Range |
|-------|-------|------------|
| 1 | 0 | 18-30 |
| 5 | 1 | 19-31 |
| 10 | 2 | 20-32 |
| 15 | 3 | 21-33 |
| 30+ | 6 | 24-36 |

Width and height are rolled independently, so rooms are not necessarily square.

**Floor painting:** Border tiles (col=0, col=width-1, row=0, row=height-1) are walls. Interior tiles use a random floor source: `_floorSourceBaseId + (GD.Randi() % _floorSourceCount)`.

---

### Stairs System

Stairs are physical Node2D objects (not special tiles) created by `CreateStairsObject()`. Each stairs object contains:

1. **Sprite2D** -- Stairs tile art from `res://assets/tiles/dungeon/stairs_down.png` or `stairs_up.png`, texture_filter=Nearest, offset=(0,-16)
2. **StaticBody2D** -- Collision body (CircleShape2D, radius=14px) so the player bumps into stairs
3. **Area2D** -- Trigger area (CircleShape2D, radius=24px) that detects player approach
4. **Label** -- Text above stairs (font_size=11, outline_size=3, centered)

**Stairs placement:**

Stairs are placed with a minimum wall margin of 4 tiles. The room is split by center; up-stairs and down-stairs are placed in opposite halves (randomly which half), ensuring they are separated.

```
wallMargin = 4
centerCol = roomWidth / 2
centerRow = roomHeight / 2
```

One staircase goes in the range `[wallMargin, center-2]`, the other in `[center+2, roomSize-1-wallMargin]`. A coin flip decides which half gets up vs. down.

**Stairs Down behavior:** When the player enters the trigger area, `ScreenTransition.Instance.Play()` is called, which shows a floor transition screen and calls `PerformFloorDescent()`.

**Stairs Up behavior:** When the player enters the trigger area, `AscendDialog.Instance.Show()` is called. On floor 1, the label reads "Back to Town"; on deeper floors, "Stairs Up".

**Floor descent (`PerformFloorDescent()`):**
1. Increment `GameState.FloorNumber`
2. Clear all enemies (queue free nodes in enemies group)
3. Clear tilemap
4. Generate new floor layout
5. Reposition player to new stairs-up position
6. Reset player grace period
7. Reposition stairs objects
8. Spawn fresh enemies

---

### Enemy Spawning Integration

Enemy spawning uses the room dimensions for placement:

| Constant | Value | Purpose |
|----------|-------|---------|
| `SpawnWallMargin` | `5 tiles` | Enemies spawn this far from walls. |
| `SafeSpawnRadius` | `150px` | Minimum distance from player. |
| `MaxSpawnRetries` | `10` | Attempts to find a valid spawn position. |

`GetRandomEdgePosition()` picks a random position along one of the 4 room edges (inset by `SpawnWallMargin`). `IsFloorTile()` validates the position is inside the wall border. The spawn is rejected if too close to the player.

Enemy levels scale with floor depth:
- Min level: `max(1, floor - 1)`
- Max level: `floor + 2`

---

### Coordinate Conversion

`TileMapLayer.MapToLocal(Vector2I)` converts tile coordinates to world position. Used for:
- Player spawn at stairs-up position
- Stairs object positioning
- Enemy spawn position calculation

`TileMapLayer.LocalToMap(Vector2)` converts world position to tile coordinates. Used for spawn validation (`IsFloorTile()`).

---

### Scene Structure (Dungeon.tscn)

```
Dungeon (Node2D) [Dungeon.cs]
├── TileMapLayer -> tileset assigned programmatically
├── Entities (Node2D) -> player, enemies, stairs, effects
└── SpawnTimer (Timer) -> 2.8s interval, enemy soft cap check
```

The Dungeon node also connects to:
- `SpawnTimer.Timeout` for periodic enemy spawning
- `EventBus.EnemyDefeated` for respawn-after-kill logic

## Implementation Notes

- The TileMapLayer must be a direct child of Dungeon so `MapToLocal()` returns positions in Dungeon's coordinate space.
- The physics layer added to the TileSet automatically uses collision layer 1 (bit 0), matching player/enemy collision masks.
- `SetCell()` can be called multiple times on the same coordinates; latest call overwrites.
- Stairs objects are persistent across floor transitions -- they are repositioned, not recreated.
- The `_tileMap.Clear()` call in `PerformFloorDescent()` removes all tiles before regenerating.
- Each floor texture is a separate TileSetAtlasSource with a unique source ID. Random selection uses modular arithmetic on the source ID range.

## Open Questions

- Should rooms evolve from single rectangles to multi-room layouts with corridors?
- Should there be a fog-of-war system that hides unexplored tiles?
- Should the TileSet move from programmatic creation to a `.tres` resource file when more tile types are needed?
