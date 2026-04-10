# Pathfinding

## Why This Matters
Our enemies use straight-line chase (move directly toward player). In a dungeon with walls and corridors, they get stuck on corners, walk into walls, and stack on each other. Pathfinding solves this — enemies navigate around obstacles intelligently.

## Core Concepts

### A* Algorithm
The gold standard for grid-based pathfinding. A* finds the shortest path between two points by exploring cells in order of `cost so far + estimated remaining distance`:

1. Start at enemy position
2. Explore neighbors, prioritizing cells closer to the target
3. Skip cells marked as walls/obstacles
4. Return the path as a list of cells

Godot provides `AStarGrid2D` which implements A* for 2D grids — no need to code it yourself.

### AStarGrid2D (Our Choice)
Built for tile grids, native isometric support:

```csharp
var astar = new AStarGrid2D();
astar.Region = new Rect2I(0, 0, floorWidth, floorHeight);
astar.CellSize = new Vector2(64, 32);
astar.CellShape = AStarGrid2D.CellShapeEnum.IsometricDown;  // ← key for isometric
astar.DiagonalMode = AStarGrid2D.DiagonalModeEnum.OnlyIfNoObstacles;
astar.JumpingEnabled = true;  // Jump Point Search optimization
astar.Update();

// Mark walls as impassable
for (int x = 0; x < floorWidth; x++)
    for (int y = 0; y < floorHeight; y++)
        if (floorData.IsWall(x, y))
            astar.SetPointSolid(new Vector2I(x, y), true);

// Get path
Vector2[] path = astar.GetPointPath(
    new Vector2I(enemyTileX, enemyTileY),
    new Vector2I(playerTileX, playerTileY)
);
```

### NavigationServer2D (Alternative)
Godot's built-in navigation uses polygon meshes instead of grids. Better for smooth paths (any-angle movement), but harder to set up for procedural dungeons:

- Requires a `NavigationRegion2D` with a baked `NavigationPolygon`
- Can bake from TileMap data at runtime
- `NavigationAgent2D` provides path following + obstacle avoidance (RVO)
- Overkill for our tile-based game

### When to Use Each

| Feature | AStarGrid2D | NavigationServer2D |
|---------|-------------|-------------------|
| Setup complexity | Low (feed tile grid) | Medium (bake nav mesh) |
| Path quality | Grid-locked (8 directions) | Smooth (any angle) |
| Isometric support | Native (`IsometricDown`) | Works in world coords |
| Obstacle avoidance | None (add manually) | Built-in (RVO) |
| Performance (150x300 grid) | ~0.5ms per path | ~1ms per path |
| Best for | Tile-based games | Open-world, smooth movement |

**Our choice: AStarGrid2D** — directly maps to our `FloorData.Tiles[,]` array.

### Staggered Path Updates
Don't recalculate every enemy's path every frame:

```csharp
// Each enemy recalculates every 0.3-0.5 seconds, staggered
private float _pathTimer;
private Vector2[] _currentPath;
private int _pathIndex;

public override void _Process(double delta)
{
    _pathTimer -= (float)delta;
    if (_pathTimer <= 0)
    {
        _pathTimer = 0.3f + (float)GD.RandRange(0, 0.2);  // Stagger
        _currentPath = astar.GetPointPath(myTile, playerTile);
        _pathIndex = 0;
    }
    
    // Follow current path
    if (_currentPath != null && _pathIndex < _currentPath.Length)
    {
        var target = _currentPath[_pathIndex];
        var dir = (target - Position).Normalized();
        Position += dir * speed * (float)delta;
        if (Position.DistanceTo(target) < 4)
            _pathIndex++;
    }
}
```

### Separation (Anti-Stacking)
A* doesn't prevent enemies from overlapping. Add a simple separation force:

```csharp
// Push away from nearby enemies
Vector2 separation = Vector2.Zero;
foreach (var other in nearbyEnemies)
{
    float dist = Position.DistanceTo(other.Position);
    if (dist < 20 && dist > 0)
    {
        Vector2 away = (Position - other.Position).Normalized();
        separation += away * (20 - dist) / 20;
    }
}
Position += separation * 60 * (float)delta;
```

## Godot 4 + C# Implementation

```csharp
// Full setup: create AStarGrid2D from FloorData
public static AStarGrid2D CreateFromFloor(FloorData floor)
{
    var astar = new AStarGrid2D();
    astar.Region = new Rect2I(0, 0, floor.Width, floor.Height);
    astar.CellSize = new Vector2(64, 32);
    astar.CellShape = AStarGrid2D.CellShapeEnum.IsometricDown;
    astar.DiagonalMode = AStarGrid2D.DiagonalModeEnum.OnlyIfNoObstacles;
    astar.JumpingEnabled = true;
    astar.Update();
    
    for (int x = 0; x < floor.Width; x++)
        for (int y = 0; y < floor.Height; y++)
            if (floor.IsWall(x, y))
                astar.SetPointSolid(new Vector2I(x, y), true);
    
    return astar;
}
```

## Common Mistakes
1. **Recalculating paths every frame** — expensive; use 0.3-0.5s intervals with staggering
2. **Forgetting CellShape.IsometricDown** — paths are calculated for square grid, not diamond
3. **No separation force** — all enemies path to exact same point and stack
4. **Path to player's exact position** — path to the tile NEAR the player, not ON the player
5. **Not calling astar.Update()** — grid changes aren't applied until Update() is called
6. **Huge grids without JumpingEnabled** — Jump Point Search is 10-50x faster on open grids

## Checklist
- [ ] AStarGrid2D created from FloorData on floor load
- [ ] CellShape = IsometricDown
- [ ] JumpingEnabled = true
- [ ] Wall cells marked solid
- [ ] Path recalculation staggered (0.3-0.5s per enemy, random offset)
- [ ] Separation force prevents stacking
- [ ] Path follows to near-player, not exact-player position

## Sources
- [Godot AStarGrid2D](https://docs.godotengine.org/en/stable/classes/class_astargrid2d.html)
- [Godot Navigation](https://docs.godotengine.org/en/stable/tutorials/navigation/navigation_introduction_2d.html)
- [Red Blob Games: A* Introduction](https://www.redblobgames.com/pathfinding/a-star/introduction.html)
- [AStarGrid2D Isometric PR #81267](https://github.com/godotengine/godot/pull/81267)
