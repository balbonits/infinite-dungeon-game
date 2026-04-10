# Collision and Physics

## Why This Matters
Our player clips through walls because the collision shape (10px radius) was far too small for the tile grid (64px tiles). Collision bugs are the most common and most visible issue in 2D games — understanding layers, masks, and shape sizing prevents them.

## Core Concepts

### Collision Bodies — When to Use Each

| Body Type | Use For | Moves? | Physics? |
|-----------|---------|--------|----------|
| `CharacterBody2D` | Player, enemies, NPCs | Yes (code-driven) | MoveAndSlide |
| `StaticBody2D` | Walls, obstacles, floors | No | Blocks movement |
| `RigidBody2D` | Crates, projectiles, debris | Yes (physics-driven) | Gravity, forces |
| `Area2D` | Triggers, pickups, attack range | Detects overlap | No blocking |

**Rule of thumb:** If it moves by code, use CharacterBody2D. If it moves by physics, use RigidBody2D. If it doesn't move, use StaticBody2D. If it just detects, use Area2D.

### Collision Layers vs Masks
This is the #1 source of "why won't things collide?"

- **Layer** = "I AM on this layer" (what am I?)
- **Mask** = "I SCAN these layers" (what do I care about?)

For collision to happen: Object A's **mask** must include Object B's **layer**, OR vice versa.

| Entity | Layer | Mask | Why |
|--------|-------|------|-----|
| Player | 2 | 1 (walls) | Player is on layer 2, detects walls on layer 1 |
| Enemy | 4 | 1 (walls) | Enemy is on layer 4, detects walls |
| Walls/Tiles | 1 | none | Walls don't need to detect anything |
| Player Attack | — | 4 (enemies) | Area2D that detects enemies |
| Enemy Hit | — | 2 (player) | Area2D that detects player |

### Collision Shape Sizing
**The shape must match the visual.** A 10px circle on a 40px-wide character means the character visually overlaps walls by 30px before the collision system notices.

For isometric characters on a 64x32 tile grid:
- Character visual width: ~40px → collision circle radius: **~16-20px**
- This prevents visual overlap while allowing smooth wall sliding

### MoveAndSlide
`CharacterBody2D.MoveAndSlide()` moves the body by its `Velocity` and automatically slides along surfaces it can't pass through. This is why CharacterBody2D is the right choice for player/enemy movement.

```csharp
Velocity = direction * speed;
MoveAndSlide();  // Handles collision response automatically
```

### Tile-Based Collision: Two Approaches

**Approach 1: TileMap Physics Layers (Recommended)**
Add a physics layer to the TileSet. Mark wall tiles as solid. The TileMapLayer automatically generates StaticBody2D collision shapes for those tiles. MoveAndSlide handles the rest.

**Approach 2: Manual Tile Checking (Our Current Approach)**
After MoveAndSlide, convert the player's position to tile coordinates and check if it's a wall:
```csharp
var tileCoords = tileMap.LocalToMap(tileMap.ToLocal(player.GlobalPosition));
if (floorData.IsWall(tileCoords.X, tileCoords.Y))
    player.Position = previousPosition;  // Push back
```
This works but is fragile — the pushback can feel jerky, and corner cases (diagonal movement into corners) cause glitching.

### Isometric Collision Quirks
Isometric diamonds create diagonal edges. A rectangular collision shape aligned to screen axes doesn't match the tile diamond. Solutions:
- Use a circle shape (works for all angles)
- Use a small diamond-shaped polygon (matches the tile shape)
- Accept slight visual mismatch with a circle (most games do this)

## Godot 4 + C# Implementation

```csharp
// Player setup
public override void _Ready()
{
    var collision = new CollisionShape2D();
    var shape = new CircleShape2D();
    shape.Radius = 18f;  // ~60% of character visual half-width
    collision.Shape = shape;
    AddChild(collision);
}

// Movement with MoveAndSlide
public override void _PhysicsProcess(double delta)
{
    var input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
    Velocity = (IsoTransform * input).Normalized() * MoveSpeed;
    MoveAndSlide();
}
```

## Common Mistakes
1. **Collision shape too small** — character visually overlaps walls before collision fires
2. **Wrong layer/mask** — objects pass through each other (check: does A's mask include B's layer?)
3. **No CollisionShape2D child** — body exists but has no shape, so nothing collides
4. **Using RigidBody2D for player** — player movement should be code-driven (CharacterBody2D), not physics-driven
5. **Manual tile checking instead of TileMap physics** — creates pushback glitches and corner-case bugs
6. **Forgetting Area2D signals** — BodyEntered/BodyExited must be connected for detection to work
7. **Collision shape not matching visual** — shape is a rectangle but character is drawn as a diamond

## Checklist
- [ ] Every CharacterBody2D has a CollisionShape2D child
- [ ] Collision shape radius is ~50-60% of character visual width
- [ ] Layers and masks are set correctly (layer = "I am", mask = "I detect")
- [ ] Walls are on layer 1, player on layer 2, enemies on layer 4
- [ ] MoveAndSlide() is called in _PhysicsProcess, not _Process
- [ ] TileMap has physics layers on wall tiles (preferred over manual checking)

## Sources
- [Godot Physics Introduction](https://docs.godotengine.org/en/stable/tutorials/physics/physics_introduction.html)
- [Godot CharacterBody2D](https://docs.godotengine.org/en/stable/classes/class_characterbody2d.html)
- [Godot Collision Layers/Masks](https://docs.godotengine.org/en/stable/tutorials/physics/physics_introduction.html#collision-layers-and-masks)
- [Stack Overflow: Godot collision not working](https://stackoverflow.com/questions/tagged/godot+collision)
