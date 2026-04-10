# Camera and Viewport

## Why This Matters
Our camera zoom made things too small or too large, UI elements didn't scale correctly with the window, and world-to-screen coordinate confusion caused entities to spawn at wrong positions. Understanding how Godot's viewport and camera system works prevents all these issues.

## Core Concepts

### Viewport = The Rendering Target
Everything in Godot renders into a `Viewport`. The main viewport size is set in `project.godot`:

```
window/size/viewport_width=1920
window/size/viewport_height=1080
```

This is the **logical** resolution — the coordinate system your code works in. The actual window can be any size.

### Stretch Modes
The stretch mode determines how the logical viewport maps to the window:

| Mode | Behavior | Use For |
|------|----------|---------|
| `disabled` | No stretching. 1:1 pixel mapping. | Pixel-perfect retro games |
| `canvas_items` | UI and 2D scale to fit window | **Our setting** — responsive UI |
| `viewport` | Renders at viewport size, then scales the image | Pixel art with crisp edges |

**Our setup: `canvas_items` + 1920x1080** means:
- Logical coordinates are always 1920x1080
- UI scales smoothly to any window size
- 2D game world scales with the window
- A Control at position (960, 540) is always at screen center

### Stretch Aspect
Controls how aspect ratio mismatches are handled:

| Aspect | Behavior |
|--------|----------|
| `ignore` | Stretches to fill (distorts) |
| `keep` | Letterbox/pillarbox to preserve ratio |
| `keep_width` | Width is preserved, height may crop |
| `keep_height` | Height is preserved, width may crop |
| `expand` | Viewport expands to fill (no letterbox, more visible area) |

### Camera2D
A Camera2D defines which part of the world is visible. Key properties:

| Property | What it does |
|----------|-------------|
| `Position` | World position the camera looks at |
| `Zoom` | Scale factor. (2,2) = 2x zoom in, (0.5,0.5) = zoomed out |
| `Offset` | Pixel offset from Position (for screen shake, UI sidebar offset) |
| `PositionSmoothingEnabled` | Smooth follow instead of snap |
| `PositionSmoothingSpeed` | How fast the camera catches up (higher = faster) |
| `LimitLeft/Right/Top/Bottom` | Camera doesn't scroll past these bounds |

### Following the Player
Two approaches:

**Reparent camera as child of player (simplest):**
```csharp
camera.GetParent().RemoveChild(camera);
player.AddChild(camera);
camera.Position = Vector2.Zero;
```

**Set position in _Process (more control):**
```csharp
public override void _Process(double delta)
{
    camera.Position = camera.Position.Lerp(player.Position, 5f * (float)delta);
}
```

The reparent approach is simpler and what we use. The Lerp approach allows camera lag, leading/trailing, and deadzone behavior.

### Camera Shake
Screen shake is one of the most impactful juice techniques. Implement via `Offset`, NOT `Position`:

```csharp
// Start shake
private float _shakeIntensity;
private float _shakeDuration;

public void Shake(float intensity, float duration)
{
    _shakeIntensity = intensity;
    _shakeDuration = duration;
}

public override void _Process(double delta)
{
    if (_shakeDuration > 0)
    {
        _shakeDuration -= (float)delta;
        float strength = _shakeIntensity * (_shakeDuration / 0.3f);  // decay
        Offset = new Vector2(
            (float)GD.RandRange(-strength, strength),
            (float)GD.RandRange(-strength, strength)
        );
    }
    else
    {
        Offset = Vector2.Zero;
    }
}
```

Using `Offset` means the camera still tracks the player correctly — the shake is visual-only.

### World vs Screen Coordinates
Two coordinate spaces that are easy to confuse:

| Space | Description | Example |
|-------|-------------|---------|
| **World** | Position in the game world | Player at (500, 300) |
| **Screen** | Pixel position on the viewport | Mouse at (960, 540) |

Converting:
```csharp
// Screen → World (for mouse clicks)
Vector2 worldPos = GetGlobalMousePosition();  // Already world space in 2D

// World → Screen (for UI positioning)
Vector2 screenPos = GetViewport().GetScreenTransform() * worldPos;

// Viewport size (logical, not window)
Vector2 viewportSize = GetViewportRect().Size;  // Always 1920x1080 with our setup
```

### Zoom Levels
Our camera zooms:
| Scene | Zoom | Visible Area |
|-------|------|-------------|
| Town | (2, 2) | ~960x540 world pixels |
| Dungeon | (2, 2) | ~960x540 world pixels |
| Sprite Align Tool | (3.5, 3.5) | ~549x309 world pixels |
| TestDungeonGen | (0.5, 0.5) | ~3840x2160 world pixels |

Higher zoom = see less, bigger sprites. Lower zoom = see more, smaller sprites.

## Godot 4 + C# Implementation

```csharp
// Camera setup for isometric game
var camera = new Camera2D();
camera.Zoom = new Vector2(2, 2);
camera.PositionSmoothingEnabled = true;
camera.PositionSmoothingSpeed = 8.0f;

// Attach to player for auto-follow
player.AddChild(camera);

// Camera limits (prevent seeing outside the map)
camera.LimitLeft = 0;
camera.LimitTop = -200;  // Allow some sky
camera.LimitRight = mapWidth;
camera.LimitBottom = mapHeight;
```

## Common Mistakes
1. **Camera zoom too high** — can't see enough of the map; player feels claustrophobic
2. **Camera zoom too low** — everything is tiny; hard to see characters
3. **Confusing world and screen coordinates** — placing UI at world position (moves with camera) instead of screen position
4. **Shaking Position instead of Offset** — camera loses track of the player during shake
5. **Not enabling position smoothing** — camera snaps to player each frame, feels jerky
6. **Forgetting camera limits** — camera scrolls past the edge of the map into void
7. **SubViewport not sized** — renders at 0x0 pixels (invisible)

## Checklist
- [ ] Camera2D is a child of the player (or Position set in _Process)
- [ ] Zoom is appropriate: (2,2) for gameplay, (0.5,0.5) for overview
- [ ] PositionSmoothingEnabled = true, speed 5-10
- [ ] Screen shake uses Offset, not Position
- [ ] Camera limits set to map bounds
- [ ] UI on CanvasLayer (not affected by camera)

## Sources
- [Godot Camera2D docs](https://docs.godotengine.org/en/stable/classes/class_camera2d.html)
- [Godot Viewports](https://docs.godotengine.org/en/stable/tutorials/rendering/viewports.html)
- [Godot Multiple Resolutions](https://docs.godotengine.org/en/stable/tutorials/rendering/multiple_resolutions.html)
- [GDC: Scroll Back — The Theory and Practice of Cameras in Side-Scrollers](https://www.youtube.com/watch?v=pdvCO97jOQk)
