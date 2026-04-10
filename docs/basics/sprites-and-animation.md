# Sprites and Animation

## Why This Matters
Our sprites render at wrong sizes, float above tiles, and look garbled when frame indexing is wrong. Every visual bug with characters and creatures traces back to not understanding sprite sheet conventions, pivot points, and isometric alignment.

## Core Concepts

### Sprite Sheets
A sprite sheet is a single image containing multiple frames arranged in a grid. Godot's `Sprite2D` splits it using `Hframes` (columns) and `Vframes` (rows). The `Frame` property selects which cell to display.

Frame index formula: `frame = row * Hframes + column`

**Our sprite specs:**
| Type | Sheet Size | Grid | Frame Size | Scale to Tile |
|------|-----------|------|-----------|---------------|
| Creature | 1024x1024 | 8x8 | 128x128 | 0.3125 (40/128) |
| Hero | 2048x1024 | 32x8 | 64x128 | 0.625 (40/64) |

Creature animations: Stance(0), Walk(1-3), Attack(4-5), Hit(6), Dead(7) per row.
Hero animations: Stance(0-3), Run(4-11), Melee(12-15), Block(16-17), Hit(18-20), Die(21-23), Cast(24-27), Shoot(28-31).

Each row = one direction (0=S, 1=SW, 2=W, 3=NW, 4=N, 5=NE, 6=E, 7=SE).

### Pivot Points and Offsets
By default, `Sprite2D` renders centered on the node position. For isometric games, a character's **feet** must sit on the tile center, not the sprite center. The fix is `Sprite2D.Offset`:

```csharp
// Shift sprite up so feet land on node position (tile center)
float frameHeight = texture.GetHeight() / vframes;
sprite.Offset = new Vector2(0, -frameHeight * scale * 0.4f);
```

The `0.4f` factor means "40% of the rendered height above center" — this places feet roughly at the bottom of the sprite.

### Scaling for Isometric Tiles
ISS tiles are 64x32. A character should occupy ~60-80% of tile width (38-48px). Our creatures render 128px frames, so: `40 / 128 = 0.3125` scale. Heroes render 64px-wide frames: `40 / 64 = 0.625`.

### Rendering Order
Isometric depth uses Y-sort: objects at higher Y (closer to camera) render in front. Enable `YSortEnabled` on the parent node. For same-Y objects, lower Z-index renders first.

### TextureFilter
Always use `TextureFilterEnum.Nearest` for pixel art. Without it, Godot applies bilinear filtering which blurs pixel edges.

## Godot 4 + C# Implementation

```csharp
// Load and configure a creature sprite
var sprite = new Sprite2D();
sprite.Texture = ResourceLoader.Load<Texture2D>("res://assets/isometric/enemies/creatures/slime.png");
sprite.Hframes = 8;
sprite.Vframes = 8;
sprite.Frame = 0;  // row 0 (south), column 0 (stance)
sprite.Scale = new Vector2(0.3125f, 0.3125f);
sprite.TextureFilter = TextureFilterEnum.Nearest;

// Offset so feet land on tile center
float frameH = sprite.Texture.GetHeight() / sprite.Vframes;
sprite.Offset = new Vector2(0, -frameH * 0.3125f * 0.4f);

// Set direction and animation frame
int direction = 2;  // west
int animFrame = 1;  // first walk frame
sprite.Frame = direction * sprite.Hframes + animFrame;
```

## Common Mistakes
1. **Wrong Hframes/Vframes** — sprite displays garbled mosaic of the entire sheet
2. **No pivot offset** — sprite center sits on tile center, making character float above ground
3. **Missing TextureFilter.Nearest** — pixel art becomes blurry at any zoom level
4. **Scale too small** — character is a dot on the tile; scale should make sprite fill ~60% of tile width
5. **Frame index out of range** — `Frame = row * Hframes + col` must be < `Hframes * Vframes`
6. **Y-sort not enabled** — characters render in tree order instead of depth order
7. **Mixing sprite sizes** — hero frames (64px wide) need different scale than creature frames (128px wide)

## Checklist
- [ ] Hframes and Vframes match the sprite sheet grid
- [ ] Scale makes the character fill ~60-80% of a 64x32 tile
- [ ] Offset.Y shifts sprite so feet land on node position
- [ ] TextureFilter = Nearest
- [ ] Parent node has YSortEnabled = true
- [ ] Frame formula: `direction * Hframes + animColumn`

## Sources
- [Godot Sprite2D docs](https://docs.godotengine.org/en/stable/classes/class_sprite2d.html)
- [Red Blob Games: Isometric Rendering](https://www.redblobgames.com/grids/hexagons/)
- [Kenney: Rendering Isometric Sprites in Godot](https://kenney.nl/knowledge-base/learning/rendering-isometric-sprites-using-godot)
- [FLARE sprite sheet conventions](https://flarerpg.org/wiki/index.php?title=Sprites)
