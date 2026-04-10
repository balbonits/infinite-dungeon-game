# 2D Performance

## Why This Matters
Our game runs fine with 5-8 rooms, but at max floor size (150x300 = 45,000 tiles) with 14+ enemies, performance could degrade. Understanding draw calls, batching, and pooling prevents frame drops before they happen.

## Core Concepts

### Draw Calls
Every visible sprite/tile is a "draw call" to the GPU. Godot batches adjacent same-texture draws into one call. Breaking batches (different textures, different Z-index) creates more calls.

**Budget:** 2D games should stay under ~2,000 draw calls for 60fps. Our tile floors alone could be 45,000 draws without batching.

**Why TileMapLayer is fast:** It batches all tiles in a quadrant (16x16 cells) into one draw call. A 150x300 grid = ~176 quadrants = ~176 draw calls for the entire floor. This is why we use TileMapLayer instead of individual Sprite2D per tile.

### Object Pooling
Creating and destroying nodes is expensive. Instead of `new Node()` + `QueueFree()` for every damage number, effect, and projectile, pre-create a pool:

```csharp
// Pool of 20 damage labels, reused instead of created/destroyed
private readonly Queue<Label> _damagePool = new();

public Label GetDamageLabel()
{
    if (_damagePool.Count > 0)
    {
        var label = _damagePool.Dequeue();
        label.Visible = true;
        return label;
    }
    return new Label();  // Create new only if pool is empty
}

public void ReturnDamageLabel(Label label)
{
    label.Visible = false;
    _damagePool.Enqueue(label);
}
```

### Culling
Don't process/render what the camera can't see. Godot handles visual culling automatically (off-screen nodes don't render), but logic still runs. For expensive logic:

```csharp
// Only update enemies near the player
if (Position.DistanceTo(player.Position) > 800)
    return;  // Skip AI update for distant enemies
```

### _Process vs _PhysicsProcess Performance
- `_Process` runs every render frame (60fps = 60 calls/sec)
- `_PhysicsProcess` runs at fixed rate (default 60/sec)
- Put expensive logic in `_PhysicsProcess` with staggered timers, not every-frame `_Process`

### Godot Profiler
Press F5 in the editor to start the game, then Debugger → Profiler:
- **FPS**: target 60
- **Frame time**: target < 16.6ms
- **Physics time**: how long _PhysicsProcess takes
- **Node count**: how many nodes in the tree (lower = better)
- **Draw calls**: how many batched draw commands

### Specific to Our Game

| System | Risk | Mitigation |
|--------|------|------------|
| TileMap (45K tiles) | Moderate | TileMapLayer handles batching; only edge walls rendered |
| Enemies (14 max) | Low | Soft cap prevents runaway spawns |
| Damage numbers | Medium | Pool labels instead of new/QueueFree each hit |
| Pathfinding (14 A*) | Medium | Stagger updates (0.3-0.5s per enemy) |
| Automap (_Draw) | Low | Only redraws on QueueRedraw(), not every frame |
| Exploration marking | Low | Circle check is O(radius²), runs once per movement |

## Godot 4 + C# Implementation

```csharp
// Performance-safe effect spawning with pooling
public class EffectPool
{
    private readonly PackedScene _template;
    private readonly Queue<Node2D> _pool = new();
    
    public EffectPool(PackedScene template, int warmup = 10)
    {
        _template = template;
        for (int i = 0; i < warmup; i++)
        {
            var instance = _template.Instantiate<Node2D>();
            instance.Visible = false;
            _pool.Enqueue(instance);
        }
    }
    
    public Node2D Get(Node parent)
    {
        var instance = _pool.Count > 0 ? _pool.Dequeue() : _template.Instantiate<Node2D>();
        instance.Visible = true;
        if (instance.GetParent() == null)
            parent.AddChild(instance);
        return instance;
    }
    
    public void Return(Node2D instance)
    {
        instance.Visible = false;
        _pool.Enqueue(instance);
    }
}
```

## Common Mistakes
1. **Individual Sprite2D per tile** — kills performance; use TileMapLayer
2. **New + QueueFree in hot loops** — creates GC pressure; pool instead
3. **Pathfinding every frame** — stagger enemy path updates
4. **Processing off-screen enemies** — skip AI for distant entities
5. **_Draw without QueueRedraw guard** — _Draw can fire multiple times per frame
6. **No profiling** — guessing at performance instead of measuring

## Checklist
- [ ] Tiles rendered via TileMapLayer (not individual sprites)
- [ ] Damage numbers and effects use object pooling
- [ ] Enemy pathfinding is staggered (not every frame)
- [ ] Enemy soft cap prevents > 14 active enemies
- [ ] Godot profiler checked: FPS stable at 60, draw calls < 2000

## Sources
- [Godot Performance Best Practices](https://docs.godotengine.org/en/stable/tutorials/performance/index.html)
- [Godot Optimization Using Servers](https://docs.godotengine.org/en/stable/tutorials/performance/using_servers.html)
- [Game Programming Patterns: Object Pool](https://gameprogrammingpatterns.com/object-pool.html)
