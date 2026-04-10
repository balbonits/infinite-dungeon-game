# TileMap and Isometric Rendering

## Why This Matters
Our floors weren't rendering because the TileSet was misconfigured — wall tiles (64x64) were being rendered in 64x32 cells, causing them to be clipped. Understanding how Godot's TileMap system handles isometric tiles, especially mixed-size tiles, prevents this entire class of bugs.

## Core Concepts

### TileSet Configuration
A TileSet defines what tiles look like and how they behave. Key properties for isometric:

| Property | Value | Why |
|----------|-------|-----|
| `TileShape` | `Isometric` | Diamond-shaped cells instead of rectangular |
| `TileSize` | `64x32` | The **cell size** — the diamond footprint on screen |
| `TileLayout` | `Stacked` | How cells are arranged (stacked = standard isometric) |

**Critical distinction:** `TileSize` is the **cell footprint**, not the texture size. A 64x64 wall block sits in a 64x32 cell — the extra height extends upward.

### Atlas Sources and TextureRegionSize
Each tileset has one or more `TileSetAtlasSource` entries. Each source points to a sprite sheet and defines how to slice it:

```csharp
var source = new TileSetAtlasSource();
source.Texture = floorTexture;
source.TextureRegionSize = new Vector2I(64, 32);  // Each tile is 64x32 from the sheet
```

**For walls (64x64 textures in a 64x32 cell):**
```csharp
var wallSource = new TileSetAtlasSource();
wallSource.Texture = wallTexture;
wallSource.TextureRegionSize = new Vector2I(64, 64);  // Larger than cell — top extends up
```

When `TextureRegionSize` is taller than `TileSize`, the extra height renders **above** the cell. This is how isometric walls work — the cube's top face sits at the cell position, and the vertical face extends upward.

### Coordinate Systems

**Cell coordinates** = integer grid position (x, y) in the tile array
**World coordinates** = pixel position in the game world

Convert between them:
```csharp
// Cell → World
Vector2 worldPos = tileMap.MapToLocal(new Vector2I(cellX, cellY));

// World → Cell
Vector2I cellPos = tileMap.LocalToMap(tileMap.ToLocal(worldPosition));
```

In isometric, `MapToLocal` applies the diamond transform. Cell (0,0) maps to the world origin. Cell (1,0) is 32px right and 16px down. Cell (0,1) is 32px left and 16px down.

### Isometric Math
The isometric transform converts grid coordinates to screen coordinates:

```
screenX = (gridX - gridY) * tileWidth / 2
screenY = (gridX + gridY) * tileHeight / 2
```

This is the same `Transform2D` used for player movement:
```csharp
private static readonly Transform2D IsoTransform = new(
    new Vector2(1, 0.5f),    // grid-right → screen southeast
    new Vector2(-1, 0.5f),   // grid-down → screen southwest
    Vector2.Zero
);
```

### Wall Rendering
Walls in our game are 64x64 isometric cubes. They sit in the same 64x32 grid as floors but extend 32px upward. Key rules:

1. **Same TileMapLayer**: Floors and walls can share one layer (one cell = one tile)
2. **Different atlas sources**: Floor source has `TextureRegionSize = 64x32`, wall source has `TextureRegionSize = 64x64`
3. **Edge walls only**: Only render wall blocks adjacent to floor tiles (buried walls waste draw calls)
4. **Y-Sort**: Walls at lower Y render behind walls at higher Y (isometric depth)

### Y-Sort Depth Ordering
Enable `YSortEnabled` on the TileMapLayer. Godot sorts cells by Y coordinate — cells at higher Y (closer to camera in isometric) render on top. This gives correct depth for both tiles and entities.

For entities on the tilemap (player, enemies), they need to be in a Y-sorted parent that's a sibling or child of the TileMapLayer, NOT on a separate CanvasLayer (which would break depth interleaving).

### Navigation from TileMap
For pathfinding, Godot can bake a `NavigationPolygon` from TileMap data. Or use `AStarGrid2D` and mark wall cells as solid:

```csharp
var astar = new AStarGrid2D();
astar.Region = new Rect2I(0, 0, width, height);
astar.CellSize = new Vector2(64, 32);
astar.CellShape = AStarGrid2D.CellShapeEnum.IsometricDown;
astar.Update();

for (int x = 0; x < width; x++)
    for (int y = 0; y < height; y++)
        if (IsWall(x, y))
            astar.SetPointSolid(new Vector2I(x, y), true);
```

## Godot 4 + C# Implementation

```csharp
// Build an isometric TileMap with floors and walls
var tileSet = new TileSet();
tileSet.TileShape = TileSet.TileShapeEnum.Isometric;
tileSet.TileSize = new Vector2I(64, 32);

// Floor tiles: 64x32 each
var floorSource = new TileSetAtlasSource();
floorSource.Texture = LoadTexture("floors/floor_rect_gray.png");
floorSource.TextureRegionSize = new Vector2I(64, 32);
int floorSrcId = tileSet.AddSource(floorSource);
// Create tile entries for each cell in the sheet
int cols = floorSource.Texture.GetWidth() / 64;
int rows = floorSource.Texture.GetHeight() / 32;
for (int x = 0; x < cols; x++)
    for (int y = 0; y < rows; y++)
        floorSource.CreateTile(new Vector2I(x, y));

// Wall blocks: 64x64 each (taller than cell)
var wallSource = new TileSetAtlasSource();
wallSource.Texture = LoadTexture("walls/brick_gray.png");
wallSource.TextureRegionSize = new Vector2I(64, 64);
int wallSrcId = tileSet.AddSource(wallSource);
// Create wall tile entries...

var tileMap = new TileMapLayer();
tileMap.TileSet = tileSet;
tileMap.YSortEnabled = true;

// Paint cells
tileMap.SetCell(new Vector2I(x, y), floorSrcId, new Vector2I(atlasX, atlasY));
```

## Common Mistakes
1. **TileSize doesn't match TextureRegionSize** — tiles render clipped or stretched (OUR BUG: 64x64 walls in 64x32 cells)
2. **Y-Sort not enabled** — tiles and entities render in wrong depth order
3. **Using world coordinates where cell coordinates are expected** — position 320,160 is NOT cell 320,160
4. **Forgetting MapToLocal** — placing entities at cell coordinates instead of converting to world position
5. **Separate TileMapLayers for floors and walls** — breaks Y-sort depth interleaving between floors and walls
6. **Rendering ALL wall cells** — interior walls (surrounded by other walls) are invisible and waste draw calls
7. **Entities on CanvasLayer** — they won't depth-sort with tiles; entities must be in the same rendering context
8. **Wrong CellShape for AStarGrid2D** — must use `IsometricDown` for isometric maps

## Checklist
- [ ] TileSet.TileShape = Isometric, TileSize = 64x32
- [ ] Floor source: TextureRegionSize = 64x32
- [ ] Wall source: TextureRegionSize = 64x64 (taller tiles extend upward)
- [ ] Single TileMapLayer with YSortEnabled for both floors and walls
- [ ] Only render edge walls (adjacent to floor tiles)
- [ ] Entities placed using MapToLocal(cellCoords), not raw pixel positions
- [ ] AStarGrid2D.CellShape = IsometricDown if using pathfinding

## Sources
- [Godot TileMap docs](https://docs.godotengine.org/en/stable/classes/class_tilemaplayer.html)
- [Godot TileSet docs](https://docs.godotengine.org/en/stable/classes/class_tileset.html)
- [Godot Isometric Tutorial](https://docs.godotengine.org/en/stable/tutorials/2d/using_tilemaps.html)
- [Red Blob Games: Isometric Grids](https://www.redblobgames.com/grids/hexagons/)
- [AStarGrid2D IsometricDown PR #81267](https://github.com/godotengine/godot/pull/81267)
