# Isometric Rendering

## Summary

Convert the dungeon and town from screen-aligned 32×32 top-down tiles to a Diablo-style 2:1 isometric projection. Floor tiles are 64×32 diamonds; wall and object tiles slot into a 64×64 footprint whose bottom 32px overlap the floor diamond. World coordinates remain integer grid `(x, y)`; screen coordinates are derived via a fixed iso transform. Visual reference is Diablo 1 / Hellfire Catacombs — inspiration only, no licensed assets.

## Current State

**Spec status: DRAFT.** Awaiting product-owner review before [ISO-01](../dev-tracker.md) implementation begins.

Today's renderer is a hybrid: the `TileMapLayer` is already configured for `TileShapeIsometric` with `tile_size = (64, 32)` (see [docs/architecture/scene-tree.md](../architecture/scene-tree.md), [docs/world/dungeon.md](../world/dungeon.md)) and ISS art assets are 64×32 floors + 64×64 walls (see [docs/assets/sprite-specs.md](../assets/sprite-specs.md)). However, **player and enemy movement remain screen-aligned** — `Input.GetVector` maps WASD directly to screen-space velocity with no iso transform (see [movement.md](movement.md) § Current State, which explicitly notes the "isometric transform matrix that was present in earlier versions has been removed"). The result: tiles look isometric, but movement, camera follow, and collision shapes still treat the world as orthographic.

This spec re-establishes a single, end-to-end isometric model so that input, movement, draw order, collision, and camera all agree on the same projection.

Related code touched by [ISO-01](../dev-tracker.md):
- `scripts/Constants.cs` — `Tiles.TileSize`, `Tiles.WallCollisionPolygon`, `PlayerStats.MoveSpeed`
- `scripts/Dungeon.cs` — tile placement, `MapToLocal`, player/enemy/stairs spawning
- `scripts/Player.cs` — input → velocity, camera ownership
- `scripts/Enemy.cs` — pathing toward player, sprite orientation
- `scripts/Main.cs` — scene loading, transitions
- `scripts/DirectionalSprite.cs` — 8-way sprite picker (already iso-friendly; sprite "north" already means visual up)
- `scripts/Projectile.cs` — projectile travel + Y-sort placement
- `scripts/Npc.cs` — town NPC placement on iso grid
- `scenes/dungeon.tscn`, `scenes/town.tscn`, `scenes/player.tscn`, `scenes/enemy.tscn`, `scenes/main.tscn` — scene-tree Y-sort flags + camera setup

Depends on / informs: [movement.md](movement.md) (will be rewritten by ISO-01f), [camera.md](camera.md), [docs/architecture/scene-tree.md](../architecture/scene-tree.md), [docs/assets/sprite-specs.md](../assets/sprite-specs.md), [docs/world/dungeon.md](../world/dungeon.md). Unblocks [ART-12](../dev-tracker.md) and [ART-13](../dev-tracker.md).

## Design

### Goals

- **One model, end-to-end.** Input, movement, draw order, collision, and camera all use the same iso projection. No more half-iso/half-top-down hybrid.
- **Diablo-feel WASD.** W moves "up the screen." Players never have to think about world coordinates.
- **Cheap and correct.** Use Godot's built-in Y-sort and `TileMapLayer` iso math wherever possible; do not hand-roll per-frame z-index unless a specific case requires it.
- **Reversible per-scene.** During the transition window, individual scenes can opt into iso via a flag so we can A/B test feel before the all-scenes flip.

### Tile Geometry

| Slot | Pixel Footprint | Anchor | Notes |
|---|---|---|---|
| Floor diamond | 64w × 32h | Center of the diamond (sprite center) | The base grid unit. One floor tile = one world cell. |
| Wall block (full height) | 64w × 64h | Bottom-center of the sprite (= center of the floor diamond it occupies) | Bottom 32px overlaps the floor diamond; top 32px is the wall face that rises above it. |
| Wall block (half height) | 64w × 48h | Bottom-center | Same anchor; shorter rise. Used for waist-high railings, low cathedral walls. |
| Multi-tile object (e.g., 2×2 altar) | 128w × 96h or larger | Bottom-center of the **southernmost** floor cell the object covers | Y-sort uses this anchor so the object sorts as if it lives in its forward-most cell. |
| Object (1×1, tall, e.g., pillar) | 64w × 96h or 64×128 | Bottom-center | Same as wall: bottom 32px = floor footprint. |

**Anchor convention is "bottom-center of the floor footprint" for everything.** This is the industry-standard iso anchor: it makes the sprite's `position` correspond exactly to the screen position of its forward floor cell, which makes Y-sort correct for free and makes hand-placement intuitive.

**Why 64×32 (2:1):** Standard iso ratio. The existing ISS tileset and PixelLab pipeline both produce 64×32 floors (see [project memory: ISS adoption](../../.claude/agent-memory/design-lead/project_tile_standard.md), [docs/assets/sprite-specs.md](../assets/sprite-specs.md)). No reason to deviate.

### Coordinate Math

World coordinates are integer grid cells `(wx, wy)`. Screen coordinates are pixel `Vector2` values inside the dungeon scene's local space.

**World → Screen (cell center):**

```
TILE_W = 64
TILE_H = 32
HALF_W = 32
HALF_H = 16

screen.x = (wx - wy) * HALF_W
screen.y = (wx + wy) * HALF_H
```

**Screen → World (inverse, for input picking / mouse hover):**

```
wx_float = (screen.x / HALF_W + screen.y / HALF_H) / 2
wy_float = (screen.y / HALF_H - screen.x / HALF_W) / 2

wx = floor(wx_float)
wy = floor(wy_float)
```

**Worked examples** (sanity-check during implementation):

| World (wx, wy) | Screen (x, y) | Visual position |
|---|---|---|
| (0, 0) | (0, 0) | Origin diamond |
| (1, 0) | (32, 16) | One cell east-in-world → visually down-right |
| (0, 1) | (-32, 16) | One cell south-in-world → visually down-left |
| (1, 1) | (0, 32) | Visually straight down |
| (2, 0) | (64, 32) | Visually down-right two diamonds |
| (-1, -1) | (0, -32) | Visually straight up |

Godot's `TileMapLayer.MapToLocal()` and `LocalToMap()` already perform this math when the tileset is `TileShapeIsometric` with `tile_size = (64, 32)`. **Use the built-in calls; do not hand-roll** unless converting outside a `TileMapLayer` context (e.g., placing an entity that is not a child of the tilemap).

**Sub-cell positions** (player standing between two cells) use the same formula on float `wx, wy` — entities are not snapped to integer cells.

### Z-Ordering (Draw Order)

**Use Godot's built-in Y-sort.** The `TileMapLayer` and the `Entities` `Node2D` parent both have `y_sort_enabled = true` (already true today per [scene-tree.md](../architecture/scene-tree.md)). Every visible thing — floor tiles, wall tiles, multi-tile objects, the player, enemies, NPCs, projectiles, dropped items, FX — lives under one Y-sort container so they all sort against each other.

**Sort key is `global_position.y`** (Godot's default for Y-sort). Combined with the bottom-center anchor convention, this means:
- A wall tile at world (5, 5) sits at screen y ≈ 160. The player at world (5, 4) sits at screen y ≈ 144. Player.y < Wall.y → player draws first → wall draws over player.
- Walk one cell south: player at world (5, 6) → screen y ≈ 176 > 160 → player draws over wall.

This is exactly the behavior we want — entities in front of walls occlude them, entities behind walls are occluded by them.

**Multi-tile objects** (e.g., the 2×2 altar from [ART-13](../dev-tracker.md)): anchor at the bottom-center of the **southernmost** floor cell the object covers. The object's `y_sort_origin` points there. Result: a 2×2 altar occupying cells (5,5), (6,5), (5,6), (6,6) sorts as if it lives at (6,6) — anything at (5,5)/(6,5) draws behind it; anything at (7,7) draws in front. This is the standard "bottom-front-corner" rule.

**Do not compute per-frame `z_index` for normal entities.** Y-sort is sufficient. Reserve manual `z_index` for explicit overlays (HUD, floating text, screen-space FX).

**Floor tiles all sort below entities** by giving the floor tilemap a separate `z_index = 0` and the entities container `z_index = 1`. Wall tiles stay on the same sort layer as entities so they interleave correctly.

### Input → Movement Mapping

**Adopt Diablo-style "WASD = visual cardinal."** The player presses W and the character moves up the screen, regardless of what world axis that corresponds to.

| Key combo | Visual direction (screen) | Equivalent world cell delta |
|---|---|---|
| W | Up | (-1, -1) — NW in world coords |
| S | Down | (+1, +1) — SE in world coords |
| A | Left | (-1, +1) — SW in world coords |
| D | Right | (+1, -1) — NE in world coords |
| W+D | Up-right | (0, -1) — pure world-north |
| W+A | Up-left | (-1, 0) — pure world-west |
| S+D | Down-right | (+1, 0) — pure world-east |
| S+A | Down-left | (0, +1) — pure world-south |

The "world cell delta" column is informational — it shows where the player ends up in world coords after one step. The implementation does **not** need to compute these; it just feeds the screen-space input vector into `Velocity` (see resolution below).

**Resolution of the "which world direction does W map to" question:** The screen-space input vector `inputScreen = (a-d, w-s)` (pre-normalization) is rotated/scaled into world space by the inverse iso transform:

```
worldDir.x = inputScreen.x / HALF_W + inputScreen.y / HALF_H   (then normalize)
worldDir.y = inputScreen.y / HALF_H - inputScreen.x / HALF_W   (then normalize)
```

In practice: just take the player's current `global_position`, add `inputScreen * speed * delta`, and call `MoveAndSlide()`. Because the player and the world are both rendered in the same iso transform, moving the player's screen-space position by the input vector produces visually correct movement automatically — there is no need to round-trip through world coordinates. **The collision shape and the wall tiles are both in the same screen-space, so screen-space velocity Just Works.**

This is the simplest implementation and matches Diablo 1 input behavior. Document this clearly in [movement.md](movement.md) when ISO-01 lands so future contributors do not "fix" it back into a world-axis system.

**Why not WASD = world cardinal:** Tested mentally. W meaning "world north" in iso would visually move the player up-and-to-the-right at a 30° angle. Players who press W expect the character to go up. Forcing the brain to learn "the dungeon is rotated 45° so W is actually NE" is exactly the Ultima-Online-style friction we want to avoid for a Diablo-like feel.

**Diagonal speed:** `Input.GetVector()` already normalizes the result, so W+D moves at the same speed as W alone. This holds in iso just as it did in top-down — no fix needed.

### Camera Follow

**Camera tracks player's screen-space position directly.** No iso transform needed for the camera itself.

| Property | Value |
|---|---|
| Camera type | `Camera2D` |
| Parent | Player (camera is a child of `Player.tscn`) |
| Position offset | `(0, 0)` — camera sits on the player anchor |
| Zoom | 2.0 (current value, unchanged by iso pivot) |
| Smoothing | `position_smoothing_enabled = true`, speed = 8.0 (Godot default acceptable) |
| Limits | Set per-scene to the iso-projected bounding box of the dungeon: `(min_screen.x - margin, min_screen.y - margin, max_screen.x + margin, max_screen.y + margin)`. Computed in `Dungeon.cs` after generation by transforming the four corners of the world grid. |

The camera does **not** need to know about iso projection — it is following a `Node2D` whose `global_position` is already in iso screen space. The only iso-aware logic is camera-bounds calculation, which uses the world→screen formula above on the four world-grid corners.

### Collision Shapes

**Keep `CircleShape2D` for entities.** Optimization opportunity, not a v1 requirement.

| Entity | Current shape | ISO-01 shape | Rationale |
|---|---|---|---|
| Player | `CircleShape2D` r=12 | `CircleShape2D` r=12 (unchanged) | Circle approximates a diamond well enough at this scale; collision behavior is identical in screen space. |
| Enemy | `CircleShape2D` r varies by species | `CircleShape2D` (unchanged) | Same reasoning. |
| NPC | `CircleShape2D` r=14 (`Constants.Npc.NpcCollisionRadius`) | unchanged | Same. |
| Wall (TileMap collision) | Rectangle polygon `WallCollisionPolygon` (-32,-16)→(32,16) | **Diamond polygon** `(0,-16), (32,0), (0,16), (-32,0)` | Walls must use a diamond polygon so circles slide correctly along the iso surfaces. The current rectangle is correct for top-down; for iso it would protrude 8px past the visible diamond at every corner and cause "phantom wall" collisions. |

**Future optimization (post-ISO-01):** convert entity colliders to diamond `ConvexPolygonShape2D` for pixel-perfect iso feel. Logged in `Open Questions` of this spec → resolve to "future work" not blocking ISO-01.

### Wall Occlusion (Player Behind Tall Walls)

**v1 approach: alpha-modulate walls between camera and player.**

When the player walks behind a wall tile, the wall should fade so the player remains visible. v1 implementation:

1. Each frame, compute the player's world cell `(pwx, pwy)`.
2. For every wall tile in cells `(pwx, pwy-1), (pwx-1, pwy), (pwx-1, pwy-1)` — i.e., the three cells **visually above** the player on screen — set `modulate.a = 0.4` if a wall exists there.
3. Walls not in that set return to `modulate.a = 1.0`.

This is a 3-cell lookup per frame. Cheap. Works without shaders. Visually reads as "the wall fades to show me where I am."

**v2 (deferred):** shader-based stencil cut — replace the alpha fade with a circular cutout in the wall sprite centered on the player. Looks better, requires a shader. Not v1 scope.

**Multi-tile objects** (e.g., 2×2 altar): treat each occupied cell as a fade target. If the altar covers (5,5) (6,5) (5,6) (6,6) and the player is at (5,7), all four cells fade together because the object is one node — alpha-modulate the whole object.

### Sprite Atlas Layout per Biome

```
assets/iso/
  dungeon/
    floors/      # 4-6 diamond variants (cobble, cracked, mossy, dark, drain)
    walls/       # NW face / NE face / SW face / SE face / corners / T-junctions / cross
    objects/     # pillar, rubble, statue, altar, chains, bones, hanging-cage
    stairs/      # up + down
  dungeon_dark/   # same structure
  cathedral/      # same
  nether/         # same
  sky_temple/     # same
  volcano/        # same
  water/          # same
  town/           # same — buildings instead of walls
  shared/
    fx/           # smoke, dust, particle sprites that look the same per biome
```

**Per biome: ~30 tiles** (6 floors + 16 walls + 8 objects approximately) **× 8 biomes = ~240 tiles** for [ART-12](../dev-tracker.md), plus **~64 environmental objects** for [ART-13](../dev-tracker.md). These numbers match the dev-tracker rows; this spec locks the directory shape so ART-12/13 can begin atlas generation as soon as ISO-01 lands.

**The current `assets/isometric/tiles/stone-soup/` ISS pack remains as the bootstrap tileset** while the per-biome PixelLab atlases fill in. ISO-01 ships using ISS art; ART-12 swaps it biome-by-biome later.

### Migration Path: Per-Scene Iso Flag

**Ship iso behind a scene-level flag, not all-or-nothing.** This lets us A/B test feel on the dungeon scene before flipping town.

```csharp
// In Constants.cs
public static class Rendering
{
    public const bool DungeonUsesIso = true;     // Flip true scene-by-scene
    public const bool TownUsesIso = false;       // Stays top-down until validated
}
```

Each scene reads its flag in `_Ready()` and chooses tile placement / collision / movement accordingly. Once **all scenes** have flipped to iso and stayed iso for one full milestone (no rollbacks), the flag is removed and the top-down code paths are deleted.

**Order of migration (recommended):**

1. **ISO-01a** — Add coord helpers (`IsoTransform.cs`), unit-test the math. Flag remains off.
2. **ISO-01b** — Flip `Dungeon.cs` to iso (already partially iso via ISS tilemap; this completes the conversion).
3. **ISO-01c** — Update `Player.cs` + `Enemy.cs` movement. The screen-space input transform (described above) is the only behavioral change.
4. **ISO-01d** — Camera bounds + Y-sort verification.
5. **ISO-01e** — Migrate `town.tscn` last (lowest risk; mostly NPC placement).
6. **ISO-01f** — Remove the flag and the legacy code paths.

The whole sequence is multi-PR. SPEC-ISO-01 just needs to land first; ISO-01a–f branch off it.

### Cross-References (Implementation Touch Surface)

ISO-01 must touch all of the following. This list is the spec's contract with the implementing team — if a file is missing here and turns out to be needed, update this spec first.

**Code:**
- `scripts/Constants.cs` — replace `Tiles.WallCollisionPolygon` with the diamond polygon; add `Rendering` static class with per-scene flags.
- `scripts/Dungeon.cs` — tile placement uses `MapToLocal`; verify all manual position math in `PlaceStairs()`, `SpawnInitialEnemies()`, and player spawn logic uses iso transform.
- `scripts/Player.cs` — confirm `Input.GetVector()` flows through `MoveAndSlide()` with no implicit world-axis assumption; remove any leftover code that treats input as world-axis.
- `scripts/Enemy.cs` — pathing toward player uses `(player.GlobalPosition - GlobalPosition).Normalized()` in screen space (already correct if Player is in iso scene space).
- `scripts/Main.cs` — scene transitions; no logic change expected, but verify no hardcoded world coordinates.
- `scripts/DirectionalSprite.cs` — already iso-friendly; no change. The 8-way directions ("north", "north-east", etc.) refer to **visual** directions on screen, which is what the player perceives. Confirm in code comments.
- `scripts/Projectile.cs` — projectile travel along screen-space velocity; verify Y-sort placement under `Entities` so it sorts against walls.
- `scripts/Npc.cs` — NPC placement in `town.tscn` uses iso tilemap `MapToLocal`.
- New: `scripts/IsoTransform.cs` — pure-static helper with `WorldToScreen(Vector2 world)`, `ScreenToWorld(Vector2 screen)`, `WorldToScreen(Vector2I worldCell)`. Wraps the formulas above; redundant with `TileMapLayer.MapToLocal` but useful for non-tilemap callers (e.g., camera bounds, world coord debug overlay).

**Scenes:**
- `scenes/dungeon.tscn` — TileMapLayer already iso; verify `Entities` `y_sort_enabled = true`; add wall-occlusion node hook if implemented as a script.
- `scenes/town.tscn` — same.
- `scenes/player.tscn` — Camera2D smoothing config, collision shape (keep circle).
- `scenes/enemy.tscn` — collision shape (keep circle), sprite Y-sort origin.
- `scenes/main.tscn` — top-level container; verify no transform overrides.
- `scenes/hud.tscn`, `scenes/death_screen.tscn`, `scenes/pause_menu.tscn` — no change (UI is screen-space, lives in CanvasLayer).

**Docs to update post-implementation:**
- [movement.md](movement.md) — rewrite to document iso input transform; remove "no isometric transform" claim.
- [camera.md](camera.md) — add iso-bounds computation rule.
- [docs/architecture/scene-tree.md](../architecture/scene-tree.md) — confirm Y-sort flags; add IsoTransform helper.
- [docs/world/dungeon.md](../world/dungeon.md) — already mentions 64×32; add wall-occlusion behavior.
- [docs/testing/manual-tests.md](../testing/manual-tests.md) — update MT-002 / movement tests to reflect new W=visual-up behavior.

## Acceptance Criteria

- [ ] `IsoTransform.WorldToScreen` and `ScreenToWorld` are inverses to within sub-pixel rounding (unit-testable).
- [ ] Pressing **W** moves the character visually up the screen (not up-and-to-the-right).
- [ ] Pressing **W+D** moves the character visually up-right at the same total speed as **W** alone (diagonal speed normalized).
- [ ] An entity at world cell (5, 4) draws **behind** a wall at (5, 5) and **in front of** a wall at (5, 3), without per-frame `z_index` manipulation.
- [ ] A 2×2 altar object placed at cells (5,5)–(6,6) sorts as if it occupies cell (6,6) — entities at (7,7) draw in front, entities at (5,4) draw behind.
- [ ] The camera follows the player smoothly with no visible lag or snap, and never reveals beyond the dungeon's iso bounding box.
- [ ] Wall tiles with their floor cell within the 3-cell occlusion window above the player render at `modulate.a = 0.4`; all others at `1.0`.
- [ ] Diamond wall collision polygon prevents the player from clipping past the visible edge of any wall tile.
- [ ] `Constants.Rendering.DungeonUsesIso` toggles iso behavior on `dungeon.tscn` without breaking the other scenes.
- [ ] Mouse hover (when added in a later spec) reports the correct world cell via `ScreenToWorld(GetGlobalMousePosition())`.

## Implementation Notes

- **Use `TileMapLayer.MapToLocal` / `LocalToMap` wherever a tile is involved.** Only fall back to `IsoTransform` for off-tilemap math (camera bounds, debug overlays, future mouse picking outside a tilemap).
- **Y-sort is global per Node2D container.** All entities (player, enemies, NPCs, projectiles, dropped items, multi-tile objects) must live under the same `Entities` node with `y_sort_enabled = true`. Scattered Y-sort containers will sort independently and produce visual bugs.
- **Sprite anchor is bottom-center of floor footprint.** When importing PixelLab / ISS art, set the sprite's `offset` so the bottom-center of the diamond footprint sits at `(0, 0)` in sprite-local coordinates. ISS already follows this; PixelLab output may need a one-line offset shim per asset.
- **Diamond collision polygon vertex order:** clockwise from top → `(0, -16), (32, 0), (0, 16), (-32, 0)`. Godot expects clockwise for convex polygons.
- **`Velocity` stays in screen-space** — do not transform input into world space and back; just feed the raw normalized input vector × `MoveSpeed` into `Velocity` and call `MoveAndSlide()`. The math works because the player's `position` is in iso scene space.
- **Wall occlusion lookup:** keep a `Dictionary<Vector2I, Sprite2D>` of wall tiles indexed by world cell during dungeon generation. Per-frame lookup is then O(1) for the 3-cell occlusion window.
- **Z-index discipline:** floor tilemap `z_index = 0`, entities container `z_index = 1`, wall tilemap `z_index = 1` (same as entities so Y-sort interleaves), HUD `CanvasLayer` `layer = 100`. No other manual z-index unless an explicit screen-space overlay is needed.
- **Migration flag is a temporary scaffold.** Do not let `Constants.Rendering` calcify into a permanent feature flag. After ISO-01f deletes it, the codebase has one rendering model.

## Open Questions

None. Awaiting product-owner sign-off to remove DRAFT status and unblock ISO-01.
