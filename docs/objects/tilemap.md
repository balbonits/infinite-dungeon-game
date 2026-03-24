# TileMap

## Summary

The dungeon floor is rendered using a TileMapLayer node with an isometric TileSet. The TileSet is created programmatically in `dungeon.gd` (not loaded from a `.tres` file) and contains two tile types: floor (walkable) and wall (physics collision). The room is a simple 10x10 grid with walls on the border and floors in the interior.

## Current State

A single 10x10 room with programmatic tile painting:
- Floor tiles: filled diamonds, dark blue-gray (`rgb(36, 49, 74)`), no physics
- Wall tiles: outlined diamonds, lighter blue-gray (`rgb(60, 70, 100)`), full diamond collision polygon
- TileSet configured for isometric shape (64x32 tiles)
- One physics layer for wall collision
- All tiles painted via `set_cell()` in `dungeon.gd._ready()`

## Design

### TileSet Configuration

The TileSet is created programmatically, not loaded from a `.tres` resource file. This is done in `dungeon.gd._ready()` because:
1. The TileSet only needs two simple tile types (no complex authoring needed)
2. Programmatic creation avoids `.tres` file management and import issues
3. Future procedural generation will likely modify the TileSet at runtime anyway

**Core TileSet properties:**

| Property | Value | Description |
|----------|-------|-------------|
| `tile_shape` | `TileSet.TILE_SHAPE_ISOMETRIC` | Isometric diamond rendering mode. Tiles are drawn as diamonds, not rectangles. |
| `tile_size` | `Vector2i(64, 32)` | Each tile occupies a 64-pixel-wide by 32-pixel-tall bounding box. The 2:1 ratio is the standard isometric proportion (width = 2 * height). |
| `tile_layout` | `TileSet.TILE_LAYOUT_STACKED` (default) | Standard stacked isometric layout where odd rows are offset. |
| Physics layers | 1 layer (index 0) | A single physics layer for wall collision. Floor tiles have no physics data on this layer. |

**Why 64x32:** This is the most common isometric tile size, giving a clean 2:1 diamond ratio. It's small enough that a 10x10 room fits comfortably in the viewport at 2x camera zoom, and large enough that individual tiles are visually distinct.

---

### Tile Types

| Source ID | Name | Texture | Visual Description | Physics |
|-----------|------|---------|-------------------|---------|
| 0 (first atlas source) | Floor | `res://assets/tiles/floor.png` (64x32) | Filled diamond shape, solid color `rgb(36, 49, 74)` -- dark blue-gray matching CSS `var(--bg-1)` | None (walkable) |
| 1 (second atlas source) | Wall | `res://assets/tiles/wall.png` (64x32) | Outlined diamond shape, border color `rgb(60, 70, 100)` -- lighter blue-gray, visually distinct from floor | Full diamond collision polygon on physics layer 0 |

**Texture format:** Both textures are 64x32 PNG images. They contain a single tile each (atlas coordinates `(0, 0)`).

**Floor tile visual:** A filled diamond with a solid dark color. It should be subtle -- the floor is background, not a focal point. The Phaser prototype used `fillStyle(0x131927)` for the background and drew a grid; the Godot version replaces this with isometric floor tiles.

**Wall tile visual:** A diamond with a visible border/outline and slightly lighter fill. The border makes walls recognizable. The fill is lighter than the floor to create contrast. The Phaser prototype had no walls (the game was bounded by the viewport edge); walls are a new addition for the Godot version.

**Alternative (placeholder without textures):** If tile textures don't exist yet, the TileSet can use a programmatically generated `ImageTexture`:
```gdscript
# Create a 64x32 image, fill with color, convert to texture
var img = Image.create(64, 32, false, Image.FORMAT_RGBA8)
img.fill(Color(0.141, 0.192, 0.290))  # rgb(36, 49, 74)
var texture = ImageTexture.create_from_image(img)
```

---

### Wall Collision Polygon

Wall tiles have a physics collision polygon that matches the diamond shape:

```
Points: [Vector2(-32, 0), Vector2(0, -16), Vector2(32, 0), Vector2(0, 16)]
```

**Visual representation:**
```
        (0, -16)
         /    \
        /      \
(-32, 0)        (32, 0)
        \      /
         \    /
        (0, 16)
```

**Why these coordinates:**
- The polygon is defined relative to the tile's local origin (center)
- A 64x32 tile has half-width 32 and half-height 16
- The four points are the midpoints of the tile's bounding rectangle edges
- This creates a diamond shape that exactly fills the isometric tile

**Physics layer assignment:**
```gdscript
# On the wall tile's TileData, physics layer 0:
var polygon = PackedVector2Array([
    Vector2(-32, 0),
    Vector2(0, -16),
    Vector2(32, 0),
    Vector2(0, 16)
])
wall_tile_data.add_collision_polygon(0)  # Physics layer 0
wall_tile_data.set_collision_polygon_points(0, 0, polygon)  # Layer 0, polygon 0
```

**Collision interaction:**
- Player (collision_mask bit 0): collides with wall physics, slides along diamond edges via `move_and_slide()`
- Enemies (collision_mask bit 0): collides with wall physics, slides along diamond edges
- The diamond collision shape means characters slide smoothly along diagonal wall surfaces rather than getting stuck on rectangular corners

---

### Room Layout

**Dimensions:**
- Room size: `ROOM_SIZE = 10` tiles (configurable constant in `dungeon.gd`)
- Total tiles: 10 * 10 = 100
- Border tiles (walls): 36 (all tiles where row == 0, row == 9, col == 0, or col == 9)
- Interior tiles (floor): 64 (all tiles where 1 <= row <= 8 AND 1 <= col <= 8)

**Layout diagram (10x10, W = wall, F = floor):**
```
W W W W W W W W W W
W F F F F F F F F W
W F F F F F F F F W
W F F F F F F F F W
W F F F F F F F F W
W F F F F F F F F W
W F F F F F F F F W
W F F F F F F F F W
W F F F F F F F F W
W W W W W W W W W W
```

**Isometric rendering:** Even though the layout is a simple grid, isometric rendering rotates the visual representation 45 degrees. The top-left corner of the grid appears at the top of the screen, and the grid extends down-left and down-right.

**Player spawn position:** Center of the room at tile coordinates `(5, 5)`. Converted to world position via `tile_map.map_to_local(Vector2i(5, 5))`.

**Enemy spawn positions:** Near the room edges, on floor tiles adjacent to walls (e.g., tiles at row 1, row 8, col 1, col 8). Enemies should not spawn ON wall tiles (they'd be inside the collision polygon).

---

### TileSet Setup Algorithm (dungeon.gd)

Complete pseudocode for the programmatic TileSet creation:

```gdscript
func _setup_tileset() -> void:
    # Step 1: Create TileSet with isometric configuration
    var tile_set = TileSet.new()
    tile_set.tile_shape = TileSet.TILE_SHAPE_ISOMETRIC
    tile_set.tile_size = Vector2i(64, 32)

    # Step 2: Add physics layer for wall collision
    tile_set.add_physics_layer()
    # Physics layer 0 now exists; collision_layer and collision_mask
    # default to layer 1 (bit 0), which is what player/enemy masks expect

    # Step 3: Create floor tile atlas source
    var floor_texture: Texture2D = preload("res://assets/tiles/floor.png")
    var floor_source: TileSetAtlasSource = TileSetAtlasSource.new()
    floor_source.texture = floor_texture
    floor_source.create_tile(Vector2i(0, 0))  # Single tile at atlas coords (0,0)
    var floor_source_id: int = tile_set.add_source(floor_source)

    # Step 4: Create wall tile atlas source
    var wall_texture: Texture2D = preload("res://assets/tiles/wall.png")
    var wall_source: TileSetAtlasSource = TileSetAtlasSource.new()
    wall_source.texture = wall_texture
    wall_source.create_tile(Vector2i(0, 0))  # Single tile at atlas coords (0,0)
    var wall_source_id: int = tile_set.add_source(wall_source)

    # Step 5: Add collision polygon to wall tile
    var wall_tile_data: TileData = wall_source.get_tile_data(Vector2i(0, 0), 0)
    wall_tile_data.add_collision_polygon(0)  # Physics layer index 0
    wall_tile_data.set_collision_polygon_points(0, 0, PackedVector2Array([
        Vector2(-32, 0),
        Vector2(0, -16),
        Vector2(32, 0),
        Vector2(0, 16)
    ]))

    # Step 6: Assign TileSet to TileMapLayer node
    tile_map.tile_set = tile_set

    # Return source IDs for use in floor painting
    return [floor_source_id, wall_source_id]
```

**Why separate TileSetAtlasSource per tile type:** Each atlas source references one texture. With two tile types, there are two textures and two atlas sources. The alternative -- a single texture atlas with multiple tiles -- is the standard approach for production art, but for the current placeholder textures, separate sources are simpler.

**Why TileSetAtlasSource (not TileSetScenesCollectionSource):** TileSetAtlasSource is for tiles from image textures. TileSetScenesCollectionSource is for tiles that are full scenes (e.g., animated tiles, tiles with logic). Our tiles are simple static images, so atlas source is appropriate.

---

### Floor Painting Algorithm (dungeon.gd)

Complete pseudocode for painting the tile grid:

```gdscript
const ROOM_SIZE: int = 10

func _paint_room(floor_source_id: int, wall_source_id: int) -> void:
    for col in range(ROOM_SIZE):
        for row in range(ROOM_SIZE):
            var is_border: bool = (
                col == 0 or col == ROOM_SIZE - 1 or
                row == 0 or row == ROOM_SIZE - 1
            )

            if is_border:
                tile_map.set_cell(
                    Vector2i(col, row),     # Tile coordinates
                    wall_source_id,         # Source ID (wall atlas)
                    Vector2i(0, 0)          # Atlas coordinates (only tile)
                )
            else:
                tile_map.set_cell(
                    Vector2i(col, row),     # Tile coordinates
                    floor_source_id,        # Source ID (floor atlas)
                    Vector2i(0, 0)          # Atlas coordinates (only tile)
                )
```

**`set_cell()` parameters:**
1. `Vector2i(col, row)` -- tile map coordinates. (0, 0) is the top-left tile. In isometric mode, tile (0, 0) renders at the top of the diamond grid.
2. Source ID -- which TileSetAtlasSource to use (floor or wall)
3. `Vector2i(0, 0)` -- atlas coordinates within the source. Both sources have only one tile at (0, 0).

**Iteration order:** Column-major (`col` outer, `row` inner). The order doesn't matter for correctness -- all tiles are painted regardless. The TileMapLayer handles render ordering based on its isometric configuration.

---

### Coordinate Conversion

TileMapLayer provides methods to convert between tile coordinates and world coordinates:

**`map_to_local(tile_coords: Vector2i) -> Vector2`**

Converts tile grid coordinates to the tile's center position in the TileMapLayer's local coordinate space.

```gdscript
# Get the world position of tile (5, 5) -- center of the room
var center_pos: Vector2 = tile_map.map_to_local(Vector2i(5, 5))
player.global_position = center_pos
```

**Usage in the game:**
- Placing the player at room center: `tile_map.map_to_local(Vector2i(ROOM_SIZE / 2, ROOM_SIZE / 2))`
- Spawning enemies at edge tiles: `tile_map.map_to_local(Vector2i(1, row))` for left edge
- Future: placing items, NPCs, staircase at specific tile positions

**`local_to_map(local_pos: Vector2) -> Vector2i`**

Converts a local position to the nearest tile coordinates. Useful for determining which tile a character is standing on.

```gdscript
# What tile is the player standing on?
var player_tile: Vector2i = tile_map.local_to_map(player.position)
```

**Usage in the game:**
- Future: checking if the player is on a special tile (exit, safe spot)
- Future: fog of war based on explored tiles

---

### Isometric Coordinate Math

**Tile (0, 0) position:** In isometric mode, tile (0, 0) is at the top of the diamond. Each subsequent column shifts right and down; each subsequent row shifts left and down.

**Tile spacing in world coordinates:**
- Moving +1 column: `+32` X, `+16` Y (half tile width right, quarter tile height down)
- Moving +1 row: `-32` X, `+16` Y (half tile width left, quarter tile height down)

**Room dimensions in world pixels:**
- A 10x10 isometric room spans approximately:
  - Width: `(ROOM_SIZE * 2) * (tile_width / 2)` = `20 * 32` = 640 pixels
  - Height: `(ROOM_SIZE * 2) * (tile_height / 2)` = `20 * 16` = 320 pixels
  - (These are approximate; actual bounds depend on tile overlap and the diamond orientation)

---

### TileMapLayer Node Properties

| Property | Value | Description |
|----------|-------|-------------|
| `y_sort_enabled` | `true` | Tiles at lower Y positions (further "back" in isometric space) render behind tiles at higher Y positions. Critical for correct isometric depth. |
| `tile_set` | Set programmatically | Assigned in `_setup_tileset()` during `dungeon.gd._ready()`. |
| `collision_visibility_mode` | `0` (default) | Collision shapes are visible in the editor but hidden at runtime. Useful for debugging. |

---

### Future: Procedural Generation

The current 10x10 bordered room is a placeholder. Future dungeon generation will:

**Replace the fixed room with generated layouts:**
- Multiple rooms connected by corridors
- Rooms of varying sizes (5x5 to 20x20)
- L-shaped rooms, T-intersections, dead ends
- Use the same `set_cell()` API to paint generated layouts

**Generation algorithm (planned, not implemented):**
- Binary Space Partition (BSP) or random walk to create room layouts
- Corridor carving between rooms
- Entrance (staircase up) and exit (staircase down) placement
- Safe spot placement at entrance/exit tiles

**Floor seeds:**
- Each floor will have a seed that determines its layout
- `seed(floor_seed)` before generation for reproducibility
- Seeds allow sharing: "try floor 47, seed ABC123"
- See `docs/world/dungeon.md` for generation rules

**New tile types (future):**
| ID | Name | Visual | Physics | Purpose |
|----|------|--------|---------|---------|
| 2 | Door | Doorway shape | None (open) or full (closed) | Room transitions |
| 3 | Stairs Down | Arrow/spiral indicator | None | Descend to next floor |
| 4 | Stairs Up | Arrow/spiral indicator | None | Ascend to previous floor |
| 5 | Safe Spot | Glowing crystal | None | Checkpoint / respawn point |
| 6 | Corridor | Narrow floor variant | None | Connects rooms |

**Tileset evolution:** When procedural generation is added, the TileSet will likely move from programmatic creation to a `.tres` resource file authored in the Godot TileSet editor. This allows:
- Visual tile editing with terrain rules (auto-tiling)
- Multiple tiles per terrain type (floor variation)
- Animated tiles (water, lava)
- Alternative tiles for visual variety

---

### Phaser-to-Godot Background Comparison

| Aspect | Phaser Prototype | Godot Implementation |
|--------|-----------------|---------------------|
| Background rendering | `this.add.graphics()` with filled rect + grid lines | TileMapLayer with isometric tiles |
| World bounds | `this.physics.world.setBounds(0, 0, 1100, 700)` | Wall tiles with physics collision |
| Coordinate system | Screen space (0,0 at top-left, pixel positions) | Isometric tile coordinates + map_to_local conversion |
| Tile shape | No tiles (flat rectangle world) | 64x32 isometric diamonds |
| Wall collision | `setCollideWorldBounds(true)` (screen edges) | CharacterBody2D + wall tile collision polygons |
| Scrolling | None (fixed camera) | Camera2D follows player with 2x zoom |

## Implementation Notes

- The TileMapLayer must be a direct child of the Dungeon node (not nested inside other containers) so that `map_to_local()` returns positions in the Dungeon's coordinate space.
- `y_sort_enabled` on the TileMapLayer sorts tiles by their Y position. This is separate from the Entities node's y-sorting -- tiles and entities are sorted within their own containers, and the container draw order is determined by their position in the scene tree.
- The physics layer added to the TileSet automatically uses collision layer 1 (bit 0) by default, which matches the player's and enemy's collision masks. No explicit layer/mask configuration is needed on the TileSet's physics layer.
- `set_cell()` can be called multiple times on the same coordinates -- the latest call overwrites the previous tile. This is useful for future door/corridor carving that might need to replace walls with floors.

## Open Questions

- Should floor tiles have subtle visual variation (multiple floor tile alternatives) to break up the uniform look?
- Should wall tiles be taller than floor tiles (rendered as 64x48 or 64x64 "wall blocks") to create a 2.5D effect?
- How should the TileSet evolve when procedural generation is added -- stay programmatic or move to a `.tres` file?
- Should there be a fog-of-war system that hides unexplored tiles?
- What procedural generation algorithm is best for the dungeon layout (BSP, random walk, cellular automata)?
- Should tile textures be hand-drawn pixel art or generated from code (shader-based rendering)?
