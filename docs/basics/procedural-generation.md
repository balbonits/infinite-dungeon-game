# Procedural Generation

## Why This Matters
Our dungeon uses BSP + Drunkard's Walk + Cellular Automata — a proven hybrid. But understanding WHY each technique exists and WHEN to use alternatives helps us tune the generator for better level feel and avoid pathological layouts.

## Core Concepts

### The Pipeline
Our generation pipeline runs in order:

```
BSP (room placement) → Drunkard's Walk (corridors) → Cellular Automata (smoothing)
    → Chain Ordering (IKEA layout) → Challenge Room (shortcut) → Room Types (boss/treasure)
```

### Binary Space Partitioning (BSP)
Recursively divide space into rectangular regions, then place a room in each leaf:

1. Start with full floor rectangle
2. Split horizontally or vertically (random, prefer longer axis)
3. Recurse until regions are small enough (or max depth reached)
4. Place a room in each leaf (smaller than the partition, with padding)

**Produces:** Guaranteed non-overlapping rooms with clear spatial distribution.
**Doesn't produce:** Corridors, organic shapes, or variety.

Our params: `minRoomSize = 12`, `padding = 2`, `maxDepth = 3` → 5-8 rooms per floor.

### Drunkard's Walk (Random Walk Corridors)
A "drunk" agent walks from room A to room B, carving floor tiles as it goes:
- 70% chance to move toward target (biased walk)
- 30% chance to move in random direction
- Creates winding, organic corridors

**Produces:** Natural-looking connectors between rooms.
**Doesn't produce:** Straight, efficient hallways (which is the point).

### Cellular Automata (Smoothing)
Apply rules to each tile based on its neighbors:
- If a floor tile has 5+ wall neighbors (of 8), become wall
- If a wall tile has fewer than 5 wall neighbors, become floor
- Run 2-3 iterations

**Produces:** Smooth, cave-like edges. Removes jagged artifacts.
**Risk:** Can disconnect rooms. We run connectivity repair afterward.

### Other Techniques (Not Used, But Good to Know)

| Technique | Produces | Best For |
|-----------|---------|----------|
| **Perlin/Simplex Noise** | Organic terrain (hills, caves) | Open-world, terrain height |
| **Wave Function Collapse** | Pattern-matched tile layouts | Detailed, rule-based maps |
| **Prefab Assembly** | Hand-designed rooms connected procedurally | Hades, Enter the Gungeon |
| **Voronoi** | Organic region boundaries | Zone/biome borders |

### Seed Determinism
Same seed = same layout. Critical for:
- Save/load (store floor number, regenerate identical layout)
- Bug reproduction (share the seed)
- Seeded runs (player can share "try floor 47, seed ABC")

Our seeds: `floor * 31337 + 42` — deterministic per floor number.

### Level Feel
Procedural levels can feel sterile. Fixes:
- **Vary room sizes** — not all rooms should be the same
- **Vary room shapes** — L-shapes, irregular polygons (future)
- **Place hand-designed content** — treasure rooms, boss arenas, challenge rooms
- **Zone-specific decoration** — different wall/floor themes per zone
- **Density scaling** — more rooms on larger floors (progressive sizing)

## Godot 4 + C# Implementation

```csharp
// Our generation call
var gen = new DungeonGenerator();
var floor = gen.Generate(seed: floorNumber * 31337 + 42, floorNumber: floorNumber);
// floor.Tiles[x,y] = Wall or Floor
// floor.Rooms = list of RoomData (position, size, kind)
// floor.Width, floor.Height = progressive sizing
```

## Common Mistakes
1. **No connectivity check** — smoothing can disconnect rooms; always verify and repair
2. **Hardcoded seeds** — use floor-based deterministic seeds for reproducibility
3. **No variety in room size** — all rooms the same size feels artificial
4. **Over-smoothing** — too many CA iterations removes all interesting features
5. **No hand-placed content** — pure random feels generic; mix in designed elements
6. **Ignoring performance** — generating a 150x300 floor should take < 50ms

## Checklist
- [ ] BSP produces non-overlapping rooms
- [ ] Corridors connect all rooms (chain order or sibling pairs)
- [ ] Cellular automata runs 2-3 iterations, not more
- [ ] Connectivity check after smoothing (repair if broken)
- [ ] Seeds are deterministic per floor
- [ ] Challenge rooms and boss rooms placed after base generation

## Sources
- [Roguebasin: BSP Dungeon Generation](http://www.roguebasin.com/index.php/Basic_BSP_Dungeon_generation)
- [Red Blob Games: Map Generation](https://www.redblobgames.com/maps/terrain-from-noise/)
- [Dungeon Generation in Diablo 1 (BorisTheBrave)](https://www.boristhebrave.com/2019/07/14/dungeon-generation-in-diablo-1/)
- [Wave Function Collapse Explained](https://github.com/mxgmn/WaveFunctionCollapse)
