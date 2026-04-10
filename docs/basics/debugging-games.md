# Debugging Games

## Why This Matters
Our bugs have been visual (sprites in wrong place, UI off-screen, floors not rendering) but we've been debugging by reading code instead of LOOKING at the game. Game debugging requires visual tools, not just print statements.

## Core Concepts

### The #1 Rule
**Play the game. Look at the screen. If something looks wrong, it IS wrong.** Don't argue with what you see. Don't say "the code is correct." If the player sees a bug, it's a bug.

### Visual Debugging in Godot

**Show collision shapes:**
In the editor: Debug → Visible Collision Shapes. Or at runtime:
```csharp
GetTree().DebugCollisionsHint = true;
```
This shows blue outlines around every CollisionShape2D. If the shape doesn't match the visual, that's your collision bug.

**Show navigation mesh:**
```csharp
GetTree().DebugNavigationHint = true;
```

**Remote scene tree:**
While the game runs, the editor shows the live scene tree. You can:
- Inspect any node's properties in real-time
- See which nodes exist and their hierarchy
- Check if nodes were added/removed correctly

### Console Logging Best Practices

```csharp
// Tag every log with the system name
GD.Print($"[TOWN] Floor texture: {tex.GetWidth()}x{tex.GetHeight()}");
GD.Print($"[COMBAT] Dealt {damage} to {enemy.Name} (HP: {enemy.HP})");
GD.PrintErr($"[SAVE] Failed to load slot {slot}");

// Log state transitions
GD.Print($"[AI] {Name}: {_oldState} → {_newState} (dist={dist:F0})");
```

**Don't log every frame** — it floods the console. Log on STATE CHANGES and EVENTS.

### Common Bug Categories

| Category | Symptom | Likely Cause |
|----------|---------|-------------|
| **Nothing renders** | Black screen | Scene not loaded, camera pointing wrong direction, CanvasLayer covering everything |
| **Sprites in wrong place** | Floating/offset | Wrong pivot point, wrong coordinate space (world vs screen) |
| **UI off-screen** | Can't see buttons | Anchors not set, parent Control has zero size, hardcoded position |
| **Collision not working** | Walk through walls | Wrong layer/mask, no CollisionShape2D, shape too small |
| **Z-order wrong** | Character behind floor | Y-sort not enabled, wrong Z-index, entity on wrong CanvasLayer |
| **Input not responding** | Button press ignored | Input action not defined in project.godot, wrong event method (_Input vs _UnhandledInput) |
| **Performance drop** | FPS < 60 | Too many nodes, pathfinding every frame, no object pooling |
| **Save corrupted** | Load fails | Saved references instead of values, version mismatch, invalid JSON |

### The "It Works Headless But Not Windowed" Problem
This specific bug pattern means the LOGIC is correct but the RENDERING is wrong. Common causes:
- Textures not loading (file path works in code but Godot import cache is stale → run `make import`)
- Camera not positioned correctly (works when no camera exists in headless)
- CanvasLayer not sized (works when viewport size is default)
- Font not imported (ResourceLoader.Load fails but the null check passes silently)

**Fix:** Always run `make import` after adding new assets. Check Godot's console for ERROR lines.

### Debugging Workflow

1. **Reproduce** — Find the exact steps to trigger the bug
2. **Observe** — Look at the screen, not the code. What do you SEE?
3. **Check console** — Any ERROR or WARNING lines? Read them.
4. **Enable visual debugging** — Collision shapes, navigation mesh
5. **Inspect remote tree** — Is the node hierarchy correct?
6. **Add targeted logs** — Log the specific values at the failure point
7. **Isolate** — Can you reproduce in a minimal scene?
8. **Fix** — Change the minimum amount of code
9. **Verify** — Does the fix work? Did it break anything else?

### Godot Profiler
Debugger → Profiler while game runs:
- Monitors tab: FPS, frame time, physics time, node count, memory
- Profiler tab: per-function timing (find the hot spot)
- Visual Profiler: draw call visualization

## Godot 4 + C# Implementation

```csharp
// Debug overlay: show collision shapes + performance stats
public override void _Ready()
{
    if (OS.IsDebugBuild())
    {
        GetTree().DebugCollisionsHint = true;
        
        // Performance monitor
        var label = new Label();
        label.Position = new Vector2(12, 12);
        label.AddThemeFontSizeOverride("font_size", 11);
        var ui = new CanvasLayer { Layer = 30 };
        ui.AddChild(label);
        AddChild(ui);
        
        // Update every frame
        SetProcess(true);
    }
}

public override void _Process(double delta)
{
    if (debugLabel != null)
    {
        float fps = Engine.GetFramesPerSecond();
        int nodes = GetTree().GetNodeCount();
        debugLabel.Text = $"FPS: {fps:F0}  Nodes: {nodes}";
    }
}
```

## Common Mistakes
1. **Reading code instead of looking at the screen** — the screen shows the truth
2. **Logging every frame** — floods console, hides real errors
3. **Not checking Godot's error output** — ERROR lines tell you exactly what's wrong
4. **Not using visual collision debugging** — can't see collision shapes = can't debug collision
5. **Fixing symptoms instead of causes** — "move the sprite 5px right" instead of "fix the pivot point"
6. **Not running `make import`** — asset cache is stale, textures don't load
7. **Assuming headless = windowed** — rendering bugs only appear in windowed mode

## Checklist
- [ ] Run the game and LOOK at the screen before debugging code
- [ ] Check console for ERROR and WARNING lines
- [ ] Enable collision shape visibility for collision bugs
- [ ] Use Remote scene tree to inspect live node hierarchy
- [ ] Log state changes, not every-frame values
- [ ] Run `make import` after adding/changing assets
- [ ] Test in windowed mode, not just headless

## Sources
- [Godot Debugging](https://docs.godotengine.org/en/stable/tutorials/scripting/debug/overview_of_debugging_tools.html)
- [Godot Profiler](https://docs.godotengine.org/en/stable/tutorials/scripting/debug/the_profiler.html)
- [Godot Remote Scene Tree](https://docs.godotengine.org/en/stable/tutorials/scripting/debug/debugger_panel.html)
