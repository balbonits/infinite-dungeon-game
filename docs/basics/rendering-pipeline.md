# 2D Rendering Pipeline

## Why This Matters
Our UI rendered behind the game world, walls appeared in front of characters that should be in front of them, and overlays moved with the camera. Every Z-order and layering bug comes from not understanding how Godot decides what draws on top of what.

## Core Concepts

### Draw Order Rules (in priority)
Godot 2D uses these rules, checked in order:

1. **CanvasLayer** — highest layer number draws on top. UI on layer 10 always covers game world on layer 0.
2. **Z-Index** — within the same CanvasLayer, higher Z-Index draws on top. Range: -4096 to 4096.
3. **Y-Sort** — when `YSortEnabled = true` on a parent, children sort by Y position (higher Y = draws later = in front).
4. **Tree Order** — if all else is equal, later siblings in the scene tree draw on top.

### CanvasLayer: Separating UI from World
A `CanvasLayer` creates an independent rendering context. It doesn't move when Camera2D moves. This is why **all UI must be on a CanvasLayer**.

```
Node2D (game world — moves with camera)
├── TileMapLayer
├── Player
├── Enemies
├── CanvasLayer (layer=0, same as world — NOT useful for UI)

CanvasLayer (layer=10 — UI, fixed on screen)
├── HUD
├── PauseMenu
├── NpcPanel
```

| Layer | Use |
|-------|-----|
| -1 | Background (behind game world) |
| 0 | Default (game world) |
| 10 | HUD / overlays |
| 20 | Popup menus / modals |
| 30 | Debug overlays |

### Z-Index vs Y-Sort: When to Use Each

**Z-Index**: Manual control. Set `ZIndex = 5` on the player so they always draw above floor tiles. Good for layers that never change (player always above floor).

**Y-Sort**: Automatic depth based on Y position. Characters behind walls draw behind them; characters in front draw in front. Good for isometric depth ordering.

**Both together**: Y-Sort determines order among siblings. Z-Index overrides Y-Sort between different "groups." Use Y-Sort within the entity layer, Z-Index to separate entities from tiles.

### Isometric Depth Sorting
In isometric, "deeper" objects (further from camera) have lower Y values. With Y-Sort:
- A character at Y=100 draws before a character at Y=200
- This gives correct front-to-back ordering

**But walls are taller than floors.** A 64x64 wall extends 32px above its cell. If a character walks behind the wall, the wall should cover them. Y-Sort handles this automatically as long as both the wall tile and the character are in the same Y-sorted context.

**The trap:** If entities are on a separate CanvasLayer from the TileMap, they can't Y-sort against tiles. Entities must be siblings or children of the TileMap's parent for depth interleaving to work.

### Transparency and Blending
Godot uses the painter's algorithm: draw back-to-front, later draws cover earlier ones. Transparent pixels let earlier draws show through. This means draw order matters even for transparent objects.

**Overdraw**: Every transparent sprite forces the GPU to blend pixels. Too many overlapping transparent sprites = performance drop. Minimize unnecessary transparent layers.

## Godot 4 + C# Implementation

```csharp
// Game world setup
var gameWorld = new Node2D();  // default CanvasLayer (0)
var tileMap = new TileMapLayer { YSortEnabled = true };
gameWorld.AddChild(tileMap);

var entityContainer = new Node2D { YSortEnabled = true };
gameWorld.AddChild(entityContainer);
// Player and enemies go in entityContainer — they Y-sort with each other

// UI setup — separate CanvasLayer, fixed on screen
var uiLayer = new CanvasLayer { Layer = 10 };
gameWorld.GetParent().AddChild(uiLayer);

var hud = new GameplayHud();
hud.MouseFilter = Control.MouseFilterEnum.Ignore;  // clicks pass through
uiLayer.AddChild(hud);

// Popup — even higher layer
var popupLayer = new CanvasLayer { Layer = 20 };
gameWorld.GetParent().AddChild(popupLayer);
```

## Common Mistakes
1. **UI on default layer** — HUD moves with camera instead of staying fixed on screen
2. **Entities on separate CanvasLayer from tiles** — can't Y-sort against the tilemap (depth is wrong)
3. **Y-Sort not enabled on parent** — children render in tree order instead of depth order
4. **Z-Index too high on entities** — entities always draw above walls regardless of position
5. **Forgetting MouseFilter.Ignore** — HUD blocks mouse clicks to game world
6. **Multiple CanvasLayers at same layer number** — undefined draw order between them
7. **Debug overlays on game layer** — collision shapes render behind UI instead of on top

## Checklist
- [ ] All UI (HUD, menus, panels) is on a CanvasLayer (layer 10+)
- [ ] Game entities (player, enemies) are in a Y-sorted Node2D, NOT on a CanvasLayer
- [ ] TileMapLayer has YSortEnabled = true
- [ ] Entity container has YSortEnabled = true
- [ ] HUD root has MouseFilter = Ignore
- [ ] Popup menus on higher CanvasLayer than HUD (layer 20+)
- [ ] Background on lower CanvasLayer (layer -1)

## Sources
- [Godot 2D Rendering](https://docs.godotengine.org/en/stable/tutorials/2d/2d_rendering.html)
- [Godot CanvasLayer](https://docs.godotengine.org/en/stable/classes/class_canvaslayer.html)
- [Godot Canvas Item sorting](https://docs.godotengine.org/en/stable/tutorials/2d/canvas_layers.html)
- [Y-Sort explanation (Godot Forum)](https://forum.godotengine.org/t/understanding-y-sort/31444)
