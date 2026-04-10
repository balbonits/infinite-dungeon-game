# Godot TileSet Deep Dive

## Why This Matters
Our floors didn't render because of TileSet misconfiguration. The Godot TileSet editor is powerful but has non-obvious settings for isometric, physics, and mixed-size tiles. This doc covers what the Godot docs don't make clear.

## Core Concepts

### TileSet vs TileMapLayer
- **TileSet** = the DEFINITION (what tiles exist, their textures, physics, terrain)
- **TileMapLayer** = the INSTANCE (which cells have which tiles painted)

Think of TileSet as the palette, TileMapLayer as the canvas.

### TileSet Configuration for Isometric

```csharp
var tileSet = new TileSet();
tileSet.TileShape = TileSet.TileShapeEnum.Isometric;
tileSet.TileSize = new Vector2I(64, 32);  // Diamond footprint
tileSet.TileLayout = TileSet.TileLayoutEnum.Stacked;
tileSet.TileOffsetAxis = TileSet.TileOffsetAxisEnum.Horizontal;
```

`TileSize` is the CELL size (the diamond footprint), NOT the texture size. A 64x64 wall block fits in a 64x32 cell.

### Atlas Sources
Each texture sheet becomes a `TileSetAtlasSource`:

```csharp
var source = new TileSetAtlasSource();
source.Texture = texture;
source.TextureRegionSize = new Vector2I(64, 32);  // How to slice the sheet
int sourceId = tileSet.AddSource(source);

// Create tile entries
int cols = texture.GetWidth() / 64;
int rows = texture.GetHeight() / 32;
for (int x = 0; x < cols; x++)
    for (int y = 0; y < rows; y++)
        source.CreateTile(new Vector2I(x, y));
```

**Key:** `TextureRegionSize` can differ from `TileSize`. Walls use `TextureRegionSize = 64x64` with `TileSize = 64x32`. The extra 32px extends upward.

### Physics Layers on Tiles
Instead of manual collision checking, add physics directly to the TileSet:

```csharp
// Add a physics layer to the TileSet
tileSet.AddPhysicsLayer();

// For wall tiles: add a collision polygon
// In the TileSet editor, or programmatically:
var tileData = source.GetTileData(new Vector2I(0, 0), 0);
var polygon = new[] {
    new Vector2(0, -16), new Vector2(32, 0),
    new Vector2(0, 16), new Vector2(-32, 0)
};
tileData.AddCollisionPolygon(0);
tileData.SetCollisionPolygonPoints(0, 0, polygon);
```

With physics on tiles, `CharacterBody2D.MoveAndSlide()` handles wall collision automatically — no manual tile checking needed.

### Texture Origin (Tile Offset)
Each tile can have a texture origin offset, shifting where the texture renders relative to the cell:

```csharp
var tileData = source.GetTileData(new Vector2I(col, row), 0);
tileData.TextureOrigin = new Vector2I(0, -16);  // Shift up 16px
```

Use this when a tile's visual center doesn't match the cell center.

### Y-Sort Origin
For depth sorting, each tile can specify where its "feet" are:

```csharp
tileData.YSortOrigin = 16;  // Sort based on 16px below cell center
```

This matters for tall tiles (walls) — the sort point should be at the base, not the top.

### Terrain Sets (Auto-Tiling)
Godot 4's terrain system automatically picks the right tile variant based on neighbors:

1. Define a terrain set (e.g., "ground")
2. Mark each tile's edges as "ground" or "not ground"
3. Paint with the terrain brush — Godot picks the correct tile

This is how you get seamless floor-to-wall transitions without manually painting each corner piece.

### Multiple Sources in One TileSet
Our approach: one TileSet with two sources (floors + walls):

```csharp
// Source 0: floors (64x32 regions)
int floorSrcId = tileSet.AddSource(floorSource);

// Source 1: walls (64x64 regions)
int wallSrcId = tileSet.AddSource(wallSource);

// Paint using source ID
tileMap.SetCell(pos, floorSrcId, atlasCoords);  // Floor tile
tileMap.SetCell(pos, wallSrcId, atlasCoords);    // Wall tile
```

## Common Mistakes
1. **TileSize ≠ TextureRegionSize confusion** — TileSize is the cell, TextureRegionSize is the texture slice
2. **No physics layer** — relying on manual collision checking instead of TileMap physics
3. **Wrong TileShape** — must be Isometric for diamond grids
4. **Forgetting CreateTile()** — source has no tiles until you explicitly create them
5. **Y-Sort origin not set** — tall tiles sort from their center instead of their base
6. **Terrain not configured** — manually painting every edge tile instead of using auto-tiling
7. **Not calling tileMap.SetCell with correct sourceId** — wrong source = wrong tile set

## Checklist
- [ ] TileSet.TileShape = Isometric
- [ ] TileSet.TileSize = (64, 32) for our grid
- [ ] Floor source: TextureRegionSize = (64, 32)
- [ ] Wall source: TextureRegionSize = (64, 64)
- [ ] Physics layer added for wall tiles (if using TileMap collision)
- [ ] Y-Sort origin set for tall tiles
- [ ] CreateTile() called for every cell in each atlas source

## Sources
- [Godot TileSet](https://docs.godotengine.org/en/stable/classes/class_tileset.html)
- [Godot Using TileMaps](https://docs.godotengine.org/en/stable/tutorials/2d/using_tilemaps.html)
- [Godot TileSet Physics](https://docs.godotengine.org/en/stable/tutorials/2d/using_tilemaps.html#physics)
- [Godot Terrain Sets](https://docs.godotengine.org/en/stable/tutorials/2d/using_tilemaps.html#terrain-sets)
