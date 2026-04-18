# Isometric Rendering

## Summary

**DRAFT.** The engine layer is already isometric: `TileMapLayer.TileShape = Isometric`, `TileSize = (64, 32)`, `TextureRegionSize = (64, 64)` for both floors and walls (the bottom 32px of the texture sits in the cell, the top 32px rises above it). What is **not** yet iso-correct is the gameplay layer — entity sprite anchors, the cross-container Y-sort wiring, wall collision shape, the wall-occlusion fade, and the per-biome content expansion. This spec covers completing those gaps and the content-expansion contract for [ART-12](../dev-tracker.md) / [ART-13](../dev-tracker.md). Visual reference: Diablo 1 / Hellfire — inspiration only, no licensed assets.

## Current State

**Spec status: DRAFT.** Awaiting product-owner review before [ISO-01](../dev-tracker.md) implementation begins.

Today's renderer is a **hybrid**: tile placement, the TileSet config, and player movement velocity are all already iso-correct, but a few gameplay-layer pieces still treat the world as orthographic — wall collision is a screen-aligned rectangle, sprite anchors rely on an empirical pixel offset rather than a documented bottom-center convention, the scene root is not Y-sort-enabled (so the multi-row sort case is brittle), and there is no wall occlusion when the player walks behind a tall wall. ISO-01 closes those gaps; movement and the engine-level tilemap config are explicitly preserved as-is. The audit table below shows each subsystem's current status.

### Audit — what is already iso vs what is not

| Subsystem | Status | Where |
|---|---|---|
| TileSet shape & size | **Iso (correct)** | `Constants.cs` `Tiles.TileSize = (64, 32)`; `Dungeon.cs:150` and `Town.cs:44` set `TileShape = Isometric`. |
| Floor/wall texture region | **Iso (correct)** | `Constants.cs` `Tiles.TextureRegionSize = (64, 64)` is used for both floors and walls. The 64×64 floor texture has its diamond rendered in the bottom 32px (the upper 32px is transparent / decorative shading); the 64×64 wall texture's bottom 32px overlaps the cell rect and the top 32px rises above. One uniform region size for all atlas sources keeps the TileSet build simple. |
| Tile placement (cell → world) | **Iso (correct)** | `Dungeon.cs` and `Town.cs` use `TileMapLayer.SetCell` and `MapToLocal`; the engine performs the iso transform. |
| Per-cell wall rendering | **Iso (correct)** | Single shared TileMapLayer per scene holds both floor and wall sources (matches [docs/basics/tilemap-and-isometric.md](../basics/tilemap-and-isometric.md) Common Mistake #5). |
| Wall collision polygon | **Not iso** | `Constants.cs` `Tiles.WallCollisionPolygon = (-32,-16)→(32,16)` is a screen-aligned rectangle. For iso, this should be a diamond matching the cell footprint. |
| Per-TileMapLayer Y-sort | **Iso (correct, per layer)** | Both `dungeon.tscn` lines 9 + 12 and `town.tscn` lines 9 + 12 have `y_sort_enabled = true` on the `TileMapLayer` and the `Entities Node2D` individually. |
| Cross-container Y-sort interleave | **Not iso** | The scene roots (`Dungeon (Node2D)`, `Town (Node2D)`) are **not** `y_sort_enabled`. In Godot 4, two sibling Y-sorted containers cannot interleave with each other unless their common parent is also Y-sorted. Today, walls and entities each Y-sort internally but the two groups draw in scene-tree order, not by interleaved Y. This currently looks fine because `Entities` is listed after `TileMapLayer` in the scene file, but it will produce incorrect occlusion as soon as multi-tile objects, projectiles, or NPCs need to sort against walls on a per-row basis. |
| Sprite / entity anchor | **Inconsistent** | Player spawn offsets `+ new Vector2(0, 40)` after `MapToLocal` (`Dungeon.cs:39`, `:127`) — empirical centering, not a documented convention. There is no project-wide bottom-center anchor convention applied via `Sprite2D.offset` and `Node2D.y_sort_origin`. |
| Movement / input → velocity | **Iso-compatible (Diablo-style)** | `Player.cs:178` does `Velocity = inputDir.Normalized() * MoveSpeed; MoveAndSlide();`. Because the camera and world are both rendered in iso scene space, screen-space velocity produces visually correct movement. WASD = visual cardinal already works. No changes required. |
| Camera follow | **Iso-compatible** | `Camera2D` is a child of `Player.tscn` and follows `global_position` in iso scene space. Only camera **bounds** need iso-aware computation (see Camera Follow section). |
| Wall occlusion (player behind tall walls) | **Not implemented** | No fade today; tall walls fully occlude the player on the row above them. |
| Per-biome content (variants, objects) | **Not implemented** | Each scene currently has 1 wall + 4 floor variants ([docs/objects/tilemap.md](../objects/tilemap.md)). [ART-12](../dev-tracker.md) and [ART-13](../dev-tracker.md) expand this to ~30 tiles + ~8 environmental objects per biome × 8 biomes. |

### What this spec actually covers

ISO-01 is **not** "convert top-down → iso from scratch." It is the smaller, focused job of:

1. **Tighten the gaps that are not yet iso-aware** — wall collision polygon (rectangle → diamond), cross-container Y-sort wiring (root nodes get `y_sort_enabled = true`), explicit bottom-center sprite anchor convention, off-tilemap iso math helper.
2. **Add wall occlusion** — players behind tall walls fade those walls.
3. **Lock the content contract** — directory layout and per-biome counts so [ART-12](../dev-tracker.md) / [ART-13](../dev-tracker.md) can begin atlas generation in parallel.

Movement and the engine-level tilemap config are already correct; this spec confirms them and forbids "fixing" them back into something else.

Related code touched by [ISO-01](../dev-tracker.md):
- `scripts/Constants.cs` — replace `Tiles.WallCollisionPolygon` with diamond polygon; add iso-helper if added as a static.
- `scripts/Dungeon.cs` — verify `MapToLocal` usage; replace empirical `+ Vector2(0, 40)` spawn offset with documented anchor convention.
- `scripts/Town.cs` — same as Dungeon.
- `scripts/Player.cs` — confirm bottom-center sprite anchor; no movement code changes.
- `scripts/Enemy.cs` — confirm bottom-center sprite anchor.
- `scripts/Npc.cs` — same.
- `scripts/Projectile.cs` — verify Y-sort placement under `Entities`.
- `scenes/dungeon.tscn`, `scenes/town.tscn` — root node gets `y_sort_enabled = true` so the two child Y-sort containers interleave.
- `scenes/player.tscn`, `scenes/enemy.tscn` — confirm `Sprite2D.offset` + `Node2D.y_sort_origin` set bottom-center anchor.
- New (optional): `scripts/IsoTransform.cs` — pure-static helper for off-tilemap math (camera bounds, debug overlays). Most callers should keep using `TileMapLayer.MapToLocal` / `LocalToMap`.

Depends on / informs: [movement.md](movement.md), [camera.md](camera.md), [docs/architecture/scene-tree.md](../architecture/scene-tree.md), [docs/assets/sprite-specs.md](../assets/sprite-specs.md), [docs/world/dungeon.md](../world/dungeon.md), [docs/objects/tilemap.md](../objects/tilemap.md), [docs/basics/tilemap-and-isometric.md](../basics/tilemap-and-isometric.md). Unblocks [ART-12](../dev-tracker.md) and [ART-13](../dev-tracker.md).

## Design

### Goals

- **One model, end-to-end.** Input, movement, draw order, collision, and camera all use the same iso projection. Today's hybrid is mostly right; this spec finishes it.
- **Diablo-feel WASD (already shipping).** W moves visually up the screen. Lock this as canonical; do not allow regressions to a world-axis input model.
- **Cheap and correct.** Use Godot's built-in Y-sort and `TileMapLayer` math wherever possible; do not hand-roll per-frame z-index.
- **Content expansion can ship in parallel** — the tile/object directory shape is locked here so [ART-12](../dev-tracker.md) / [ART-13](../dev-tracker.md) can produce atlases without waiting on every code change.

### Tile Geometry

| Slot | Pixel Footprint | Texture Region | Anchor | Notes |
|---|---|---|---|---|
| Floor diamond (TileMapLayer) | 64w × 32h cell | 64×64 region (matches `Constants.Tiles.TextureRegionSize`) | Godot's default tile origin (the diamond reference point — see anchor note below) | Floor diamond rendered in bottom 32px of the 64×64 region; top 32px is transparent / decorative shadow. Placement uses `MapToLocal`. |
| Wall block (TileMapLayer, full height) | 64w × 32h cell, texture extends 32px above | 64×64 region | Same as floor | Bottom 32px overlaps the floor diamond; top 32px is the wall face that rises above. The taller texture is the entire mechanism by which iso wall blocks render. Do not give walls a different `TextureRegionSize` — the project's convention (see [docs/objects/tilemap.md](../objects/tilemap.md) and current `Constants.cs`) is **one uniform 64×64 region size for all sources**. |
| Sprite/entity (player, enemy, NPC, projectile, dropped item) | varies | n/a (not a tile) | **Bottom-center of the footprint** via `Sprite2D.offset` and `Node2D.y_sort_origin` | This is the Y-sort anchor — sets where the entity "stands" so depth ordering matches visual position. |
| Multi-tile object node (e.g., 2×2 altar as a `Node2D`) | 128w × 96h or larger | n/a | **Bottom-center of the southernmost floor cell the object covers** (set on `y_sort_origin`) | Y-sort uses this anchor so the object sorts as if it lives in its forward-most cell. |
| Object node (1×1, tall, e.g., free-standing pillar as a `Node2D`) | 64w × 96h or 64×128 | n/a | Bottom-center of the floor cell the object stands on | Same Y-sort rule as entities. |

**Anchor — single unambiguous convention for TileMapLayer cells:**

A `TileMapLayer` cell's local origin is the **diamond reference point** for that cell — the point that `MapToLocal(cell)` returns. For an iso TileSet with `TileSize = (64, 32)`, that point is at the **center of the diamond** (i.e., the cell's apex/center; Godot's iso reference, not the corner of a bounding rect). Worked examples below confirm this:

- `MapToLocal(Vector2I(0, 0))` → `Vector2(0, 0)`
- `MapToLocal(Vector2I(1, 0))` → `Vector2(32, 16)` (one cell east-in-world; visually down-right by half a diamond)
- `MapToLocal(Vector2I(0, 1))` → `Vector2(-32, 16)` (one cell south-in-world; visually down-left by half a diamond)

Every floor tile and wall tile placed via `SetCell` lives at this reference point. Sprite anchors (entities, multi-tile objects) are bottom-center of footprint and align to the same reference point of the cell they occupy. This is the **only** anchor convention in the project; do not introduce per-asset offset overrides.

**Sprite/entity anchor (a separate convention, do not conflate):** anything that is **not** a TileMapLayer cell uses **bottom-center of the footprint** as its anchor, configured via `Sprite2D.offset` and `Node2D.y_sort_origin`. This makes the entity's `position` correspond to the screen position of its forward floor cell, which makes Y-sort correct for free. Today's empirical `+ Vector2(0, 40)` offset on player spawn (`Dungeon.cs:39`, `:127`) is a hack that the bottom-center anchor convention replaces — once `Sprite2D.offset` and `y_sort_origin` are set on `player.tscn`, the spawn line becomes a clean `_player.GlobalPosition = _tileMap.MapToLocal(_stairsUpPosition);`.

**Why 64×32 (2:1):** Standard iso ratio. The existing ISS tileset and PixelLab pipeline both produce 64×32 floors (see [project memory: ISS adoption](../../.claude/agent-memory/design-lead/project_tile_standard.md), [docs/assets/sprite-specs.md](../assets/sprite-specs.md)). No reason to deviate.

### Coordinate Math

World coordinates are integer grid cells `(wx, wy)`. Screen coordinates are pixel `Vector2` values inside the dungeon scene's local space.

**World → Screen (returns the cell's diamond reference point — same as `MapToLocal`):**

```
TILE_W = 64
TILE_H = 32
HALF_W = 32
HALF_H = 16

screen.x = (wx - wy) * HALF_W
screen.y = (wx + wy) * HALF_H
```

This formula is the exact math Godot's `TileMapLayer.MapToLocal()` performs when `TileShape = Isometric` and `TileSize = (64, 32)`. **Use the built-in call inside any tilemap context; only fall back to the hand-rolled formula for off-tilemap callers** (camera bounds computed before the tilemap exists, debug overlays in screen space, future mouse picking outside a tilemap region).

**Screen → World — two APIs, do not conflate:**

```
# ScreenToWorld(screen): Vector2 — exact (continuous) inverse of WorldToScreen.
# Use for: sub-cell entity placement, world-coord debug overlay, anything
# that needs round-trip precision with WorldToScreen.
wx_float = (screen.x / HALF_W + screen.y / HALF_H) / 2
wy_float = (screen.y / HALF_H - screen.x / HALF_W) / 2
return Vector2(wx_float, wy_float)

# ScreenToCell(screen): Vector2I — the integer world cell containing the point.
# Use for: mouse-hover picking, cell-based gameplay queries (what tile is
# under the cursor, which cell did the projectile land in).
v = ScreenToWorld(screen)
return Vector2I(floor(v.x), floor(v.y))
```

`ScreenToWorld` and `WorldToScreen` are exact inverses to within float precision. `ScreenToCell` is **not** an inverse — it discards sub-cell information by flooring. Inside a tilemap region, `ScreenToCell` matches `TileMapLayer.LocalToMap`.

**Worked examples** (sanity-check during implementation):

| World (wx, wy) | Screen (x, y) | Visual position |
|---|---|---|
| (0, 0) | (0, 0) | Origin diamond |
| (1, 0) | (32, 16) | One cell east-in-world → visually down-right |
| (0, 1) | (-32, 16) | One cell south-in-world → visually down-left |
| (1, 1) | (0, 32) | Visually straight down |
| (2, 0) | (64, 32) | Visually down-right two diamonds |
| (-1, -1) | (0, -32) | Visually straight up |

**Sub-cell positions** (player standing between two cells) use the same formula on float `wx, wy` — entities are not snapped to integer cells.

### Z-Ordering (Draw Order)

**Read [docs/basics/tilemap-and-isometric.md](../basics/tilemap-and-isometric.md) first** — that doc is the canonical project guidance for the TileMapLayer pattern this section depends on. Key rules from there: floors and walls share **one** TileMapLayer (separate layers break Y-sort interleaving and is listed as Common Mistake #5); wall textures (64×64 region) extend above the cell rect because the texture is taller than the cell; entities must live in a Y-sorted parent in the same rendering context (not on a CanvasLayer).

**Godot 4 Y-sort model — the rule that matters:**

A `Y-sort-enabled` node sorts its **direct children** among each other by `global_position.y`. A `TileMapLayer` with `y_sort_enabled = true` sorts its **own cells** by their effective Y. **Two sibling Y-sorted containers do not interleave with each other** unless their **common parent is also Y-sorted** — without that, the engine just draws each child in scene-tree order, and the Y-sort inside each child only orders that container's contents.

This means the **scene root** (`Dungeon (Node2D)`, `Town (Node2D)`) must have `y_sort_enabled = true`. The current scene files set Y-sort on `TileMapLayer` and `Entities` but not on the root, which is a latent bug — it currently produces visually-correct output only because `Entities` is declared after `TileMapLayer` in the .tscn (so entities draw on top of all tiles unconditionally). The moment a multi-tile object, projectile, or NPC needs to sort **between** two walls on different rows, the scene-tree order is wrong.

**Required Y-sort wiring per scene:**

1. **Scene root `Node2D`** — `y_sort_enabled = true` (NEW; today this is missing).
2. **Shared `TileMapLayer`** — `y_sort_enabled = true` (already set). Holds **both** floor and wall tiles. One cell = one tile entry; floors and walls do not coexist in the same cell.
3. **`Entities` `Node2D`** — `y_sort_enabled = true` (already set). Holds player, enemies, NPCs, projectiles, dropped items, multi-tile object nodes.

With all three Y-sort-enabled, the scene root sorts its two children (TileMapLayer and Entities) by their effective Y, and inside each child the inner Y-sort orders that container's contents (TileMapLayer cells against each other; entities against each other). The required gameplay-facing result is that an entity at world (5, 4) draws **behind** a wall at (5, 5) and **in front of** a wall at (5, 3) — verified by the acceptance criteria below. If that result is not achieved with sibling y-sort under a y-sorted root, the fallback mechanism is to push entities into the same TileMapLayer's `Entities` child slot or to use a single y-sorted parent that holds both walls (as `Sprite2D` siblings of entities, not as TileMapLayer cells); the implementing PR (`ISO-01c`) picks whichever mechanism produces the correct visual result on the acceptance test scene and updates this spec with the chosen layout.

**Sort key is `global_position.y`** (Godot's default for Y-sort). Combined with the wall-tile placement and the entity bottom-center anchor convention, this means:
- A wall tile at world (5, 5) sits at screen y ≈ 160 (the cell's local origin maps there). The player at world (5, 4) sits at screen y ≈ 144. Player.y < Wall.y → player draws first → wall draws over player.
- Walk one cell south: player at world (5, 6) → screen y ≈ 176 > 160 → player draws over wall.

**Multi-tile object nodes** (e.g., the 2×2 altar from [ART-13](../dev-tracker.md)): these are **not** placed as TileMapLayer cells; they are `Node2D` objects under `Entities` so the bottom-center anchor / `y_sort_origin` rule applies. Anchor at the bottom-center of the **southernmost** floor cell the object covers. Result: a 2×2 altar occupying cells (5,5), (6,5), (5,6), (6,6) sorts as if it lives at (6,6) — anything at (5,5)/(6,5) draws behind it; anything at (7,7) draws in front. This is the standard "bottom-front-corner" rule.

**Do not compute per-frame `z_index` for normal entities or tiles.** Y-sort is sufficient. Reserve manual `z_index` for explicit overlays (HUD, floating text, screen-space FX) which live on a `CanvasLayer` and are outside the world Y-sort context entirely.

**Do not split floors and walls into separate TileMapLayers.** Doing so breaks Y-sort interleaving between the two layers (each TileMapLayer is its own sort context; they cannot interleave with each other). This was the original bug behind [docs/basics/tilemap-and-isometric.md](../basics/tilemap-and-isometric.md) — see Common Mistake #5 there.

### Input → Movement Mapping (already correct, locked here)

**Diablo-style "WASD = visual cardinal" is already shipping.** `Player.cs:178` does:

```csharp
Velocity = inputDir.Normalized() * Constants.PlayerStats.MoveSpeed;
DirectionalSprite.UpdateSprite(_sprite, Velocity, _rotations, ref _lastDirection);
// ...
MoveAndSlide();
```

`Input.GetVector` returns the raw screen-space input vector. Multiplying by `MoveSpeed` gives `Velocity` in **pixels per second** (no `delta` multiplier — `MoveAndSlide()` integrates velocity itself). The player and the world are both rendered in the same iso scene space, so screen-space velocity produces visually correct movement automatically.

**This is correct Godot 4 idiom and correct iso behavior.** Lock it. Do not let a future contributor "fix" it into a world-axis system or add a `* delta` to the velocity assignment.

| Key combo | Visual direction (screen) | Equivalent world cell delta (informational) |
|---|---|---|
| W | Up | (-1, -1) — NW in world coords |
| S | Down | (+1, +1) — SE in world coords |
| A | Left | (-1, +1) — SW in world coords |
| D | Right | (+1, -1) — NE in world coords |
| W+D | Up-right | (0, -1) — pure world-north |
| W+A | Up-left | (-1, 0) — pure world-west |
| S+D | Down-right | (+1, 0) — pure world-east |
| S+A | Down-left | (0, +1) — pure world-south |

The "world cell delta" column is informational; the implementation never computes it.

**Movement guidance rules (Godot 4 `CharacterBody2D`):**

- Set `Velocity` in **pixels per second**. No `* delta`.
- Call `MoveAndSlide()` once per `_PhysicsProcess`. It handles delta integration and collision response.
- Do **not** write to `global_position` in the movement path. `global_position` writes are reserved for **teleports** (initial spawn, floor descent reposition, debug warps) — see `Dungeon.cs:39` and `Dungeon.cs:127` for the legitimate teleport sites.
- Do not transform input into world space and back; the raw normalized input vector × `MoveSpeed` is correct because the entire scene is in iso scene space.

**Diagonal speed:** `Input.GetVector()` already normalizes the result, so W+D moves at the same speed as W alone.

**Why not WASD = world cardinal:** Tested mentally. W meaning "world north" in iso would visually move the player up-and-to-the-right at a 30° angle. Players who press W expect the character to go up. Forcing the brain to learn "the dungeon is rotated 45° so W is actually NE" is exactly the Ultima-Online-style friction we want to avoid for a Diablo-like feel.

### Camera Follow

**Camera tracks player's screen-space position directly.** No iso transform needed for the camera itself.

| Property | Value |
|---|---|
| Camera type | `Camera2D` |
| Parent | Player (camera is a child of `Player.tscn`) |
| Position offset | `(0, 0)` — camera sits on the player anchor |
| Zoom | 2.0 (current value, unchanged by ISO-01) |
| Smoothing | `position_smoothing_enabled = true`, `position_smoothing_speed = 5.0` (matches `scenes/player.tscn` and [camera.md](camera.md) — do not change as part of ISO-01) |
| Limits | Set per-scene to the iso-projected bounding box of the dungeon: transform the four world-grid corners via `WorldToScreen` (or `MapToLocal` after the tilemap exists), take min/max of x/y, add a small margin. Computed in `Dungeon.cs` after generation. |

The camera does **not** need to know about iso projection — it is following a `Node2D` whose `global_position` is already in iso screen space. The only iso-aware logic is camera-bounds calculation, which uses the world→screen formula on the four world-grid corners.

**Out of scope for ISO-01:** [camera.md](camera.md) Open Question #2 asks whether to raise `position_smoothing_speed` from 5.0 to 8–10 for snappier follow. That decision stays open and is **not** bundled into ISO-01 — re-tuning camera feel after the iso-tightening flip lets us evaluate smoothing speed against a clean baseline.

### Collision Shapes

**Keep `CircleShape2D` for entities.** Optimization opportunity, not a v1 requirement.

| Entity | Current shape | ISO-01 shape | Rationale |
|---|---|---|---|
| Player | `CircleShape2D` r=12 | `CircleShape2D` r=12 (unchanged) | Circle approximates a diamond well enough at this scale; collision behavior is identical in screen space. |
| Enemy | `CircleShape2D` r varies by species | `CircleShape2D` (unchanged) | Same reasoning. |
| NPC | `CircleShape2D` r=14 (`Constants.Npc.NpcCollisionRadius`) | unchanged | Same. |
| Wall (TileMap collision) | Rectangle polygon `WallCollisionPolygon` (-32,-16)→(32,16) | **Diamond polygon** `(0,-16), (32,0), (0,16), (-32,0)` | Walls must use a diamond polygon so circles slide correctly along the iso surfaces. The current rectangle is correct for top-down; in iso it protrudes 8px past the visible diamond at every corner and causes "phantom wall" collisions at diagonals. |

**Future optimization (post-ISO-01):** convert entity colliders to diamond `ConvexPolygonShape2D` for pixel-perfect iso feel. Logged in `Open Questions` of this spec → resolve to "future work" not blocking ISO-01.

### Wall Occlusion (Player Behind Tall Walls)

**v1 approach: shader-based per-cell alpha fade on the shared TileMapLayer.**

When the player walks behind a wall tile, the wall should fade so the player remains visible. Because floor and wall tiles share a single TileMapLayer (per the Z-Ordering section), per-cell alpha cannot be done via `modulate.a` on individual tiles — `modulate` is a per-CanvasItem property, not per-cell. v1 implementation uses a fragment shader on the TileMapLayer that takes the player's screen position as a uniform and reduces alpha for wall-textured fragments within a 3-cell window above the player:

1. Each frame, push the player's world cell `(pwx, pwy)` (or screen position) into the TileMapLayer's shader as a `vec2` uniform.
2. The shader fades any wall fragment whose source cell is one of `(pwx, pwy-1), (pwx-1, pwy), (pwx-1, pwy-1)` — the three cells **visually above** the player — to `alpha = 0.4`.
3. All other wall fragments stay at `alpha = 1.0`. Floor fragments are never faded.

The shader needs a way to identify wall fragments vs floor fragments — easiest is a custom `tile_data` flag set per atlas tile during `TileSet` build (`is_wall = true` on every wall tile). The shader reads that flag via the tile-data texture Godot exposes for shader access.

**Multi-tile object nodes** (e.g., 2×2 altar) are separate `Node2D` instances under `Entities`, so they fade via plain `modulate.a` set on the node — no shader needed. If the altar covers (5,5) (6,5) (5,6) (6,6) and the player is at (5,7), the whole object fades because it is one node.

**v2 (deferred):** circular-cutout shader instead of flat fade — the wall stays opaque except for a soft circle around the player's screen position. Looks better but requires more shader work; not v1 scope.

### Sprite Atlas Layout per Biome

The current in-repo layout is `assets/tiles/<biome>/` (loaded as `res://assets/tiles/<biome>/...`). ART-12 / ART-13 expand inside that existing structure — no directory rename. Per-biome subfolders for `floors/`, `walls/`, `objects/`, `stairs/` are added under each biome as the variant counts grow:

```
assets/tiles/
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
```

Cross-biome shared particles (smoke, dust, generic FX) live under `assets/effects/` (already in repo).

**Per biome: ~30 tiles** (6 floors + 16 walls + 8 objects approximately) **× 8 biomes = ~240 tiles** for [ART-12](../dev-tracker.md), plus **~64 environmental objects** for [ART-13](../dev-tracker.md). These numbers match the dev-tracker rows; this spec locks the directory shape so ART-12/13 can begin atlas generation in parallel with the engine-tightening work.

**Current art is each biome's single-floor + single-wall pair under `assets/tiles/<biome>/`** (see [docs/objects/tilemap.md](../objects/tilemap.md)). ISO-01 ships against this existing art; ART-12 expands biome-by-biome by adding the subfolders above and growing the variant count per slot.

### Migration Path

Most of the engine layer is already iso. ISO-01 is a tightening pass, not a rewrite, so the per-scene flag scaffolding from earlier drafts is dropped — the changes are small enough to ship in one PR per sub-phase and there is no top-down code path to A/B test against.

**Order of sub-phases (recommended):**

1. **ISO-01a** — Add `IsoTransform.cs` static helper (off-tilemap math); unit-test the math.
2. **ISO-01b** — Replace rectangle wall collision polygon with diamond polygon in `Constants.Tiles.WallCollisionPolygon`. One-line change; verify wall sliding still feels right.
3. **ISO-01c** — Set `y_sort_enabled = true` on the scene roots `Dungeon (Node2D)` and `Town (Node2D)` in `dungeon.tscn` / `town.tscn`. Two-line change; confirm walls and entities interleave correctly when an entity walks between two walls on different rows.
4. **ISO-01d** — Set bottom-center sprite anchor convention on `player.tscn`, `enemy.tscn`, NPC sprites via `Sprite2D.offset` and `Node2D.y_sort_origin`. Remove the empirical `+ Vector2(0, 40)` spawn offset in `Dungeon.cs:39` and `:127`.
5. **ISO-01e** — Camera-bounds: compute iso bounding box from world-grid corners after floor generation and apply to the player camera limits.
6. **ISO-01f** — Wall-occlusion shader: write the fragment shader on the shared TileMapLayer, push player cell as uniform, fade walls in the 3-cell window above the player.

Each sub-phase is a small PR. The whole sequence does not need to land in one milestone — the game continues to play correctly between sub-phases (today's hybrid renders fine; the gaps are correctness-on-edge-cases and v1-occlusion polish).

### Cross-References (Implementation Touch Surface)

ISO-01 must touch all of the following. This list is the spec's contract with the implementing team — if a file is missing here and turns out to be needed, update this spec first.

**Code:**
- `scripts/Constants.cs` — replace `Tiles.WallCollisionPolygon` with the diamond polygon.
- `scripts/Dungeon.cs` — verify `MapToLocal` usage; replace `+ Vector2(0, 40)` spawn offset (lines 39, 127) with documented anchor convention; add iso camera-bounds computation after `GenerateFloor()`.
- `scripts/Town.cs` — same anchor cleanup for NPC + player placement.
- `scripts/Player.cs` — **no movement code changes**. Confirm `Velocity = inputDir.Normalized() * MoveSpeed; MoveAndSlide();` stays as-is. Verify `Sprite2D.offset` set bottom-center on the scene side.
- `scripts/Enemy.cs` — verify pathing uses `(player.GlobalPosition - GlobalPosition).Normalized()` in screen space (already correct). Verify sprite anchor.
- `scripts/Npc.cs` — verify NPC sprite anchor.
- `scripts/Projectile.cs` — verify Y-sort placement under `Entities`.
- `scripts/DirectionalSprite.cs` — already iso-friendly; no change. The 8-way directions ("north", "north-east", etc.) refer to **visual** directions on screen, which is what the player perceives.
- New: `scripts/IsoTransform.cs` — pure-static helper exposing three APIs:
  - `WorldToScreen(Vector2 world) → Vector2` and overload `WorldToScreen(Vector2I worldCell) → Vector2`.
  - `ScreenToWorld(Vector2 screen) → Vector2` — exact (continuous) inverse of `WorldToScreen`.
  - `ScreenToCell(Vector2 screen) → Vector2I` — flooring picker; matches `TileMapLayer.LocalToMap` inside the tilemap region.

  Use only for off-tilemap math (camera bounds, debug overlays, future mouse picking outside a tilemap region). All in-tilemap callers should keep using `TileMapLayer.MapToLocal` / `LocalToMap`.

**Scenes:**
- `scenes/dungeon.tscn` — root `Dungeon (Node2D)` gets `y_sort_enabled = true`. Confirm `TileMapLayer` and `Entities` keep their existing `y_sort_enabled = true`.
- `scenes/town.tscn` — same: root `Town (Node2D)` gets `y_sort_enabled = true`.
- `scenes/player.tscn` — `Sprite2D.offset` + `Node2D.y_sort_origin` set bottom-center anchor; collision shape stays `CircleShape2D` r=12.
- `scenes/enemy.tscn` — same anchor convention; collision stays circle.
- `scenes/main.tscn` — top-level container; verify no transform overrides.
- `scenes/hud.tscn`, `scenes/death_screen.tscn`, `scenes/pause_menu.tscn` — no change (UI is screen-space, lives in CanvasLayer).

**Docs to update post-implementation:**
- [movement.md](movement.md) — confirm iso-aware screen-space velocity model is canonical; remove any "isometric transform removed" historical note that implies the current setup is broken.
- [camera.md](camera.md) — add iso-bounds computation rule.
- [docs/architecture/scene-tree.md](../architecture/scene-tree.md) — update Y-sort flag list to include the scene root; add IsoTransform helper.
- [docs/world/dungeon.md](../world/dungeon.md) — add wall-occlusion behavior.
- [docs/objects/tilemap.md](../objects/tilemap.md) — already documents the iso TileSet correctly; add cross-link to this spec for future per-biome variant counts.
- [docs/testing/manual-tests.md](../testing/manual-tests.md) — confirm MT-002 / movement tests reflect the existing W=visual-up behavior (no behavior change expected).

## Acceptance Criteria

- [ ] `IsoTransform.WorldToScreen(Vector2)` and `IsoTransform.ScreenToWorld(Vector2)` are exact inverses to within float precision (unit-testable: round-trip 1000 random screen points and assert per-axis delta < 1e-4).
- [ ] `IsoTransform.ScreenToCell(Vector2)` returns the integer cell containing the input point and matches `TileMapLayer.LocalToMap` for points inside the tilemap region.
- [ ] Pressing **W** moves the character visually up the screen (regression check — current behavior, must not break).
- [ ] Pressing **W+D** moves the character visually up-right at the same total speed as **W** alone (diagonal speed normalized — regression check).
- [ ] An entity at world cell (5, 4) draws **behind** a wall at (5, 5) and **in front of** a wall at (5, 3), without per-frame `z_index` manipulation. (This requires the scene-root Y-sort fix; verifies cross-container interleave works.)
- [ ] A 2×2 altar object placed at cells (5,5)–(6,6) sorts as if it occupies cell (6,6) — entities at (7,7) draw in front, entities at (5,4) draw behind.
- [ ] The camera follows the player smoothly with no visible lag or snap, and never reveals beyond the dungeon's iso bounding box.
- [ ] Wall tiles with their floor cell within the 3-cell occlusion window above the player render at alpha 0.4 (via the wall-fade shader); all others at alpha 1.0. Floor tiles are never faded.
- [ ] Diamond wall collision polygon prevents the player from clipping past the visible edge of any wall tile and removes the "phantom corner" collisions the rectangle polygon causes at diagonals.
- [ ] Player spawn after floor descent lands the sprite visually centered on the up-stairs cell with **no** empirical pixel offset in code (`Dungeon.cs:39` / `:127` becomes a clean `MapToLocal` assignment).
- [ ] Mouse hover (when added in a later spec) reports the correct world cell via `IsoTransform.ScreenToCell(GetGlobalMousePosition())`.

## Implementation Notes

- **Use `TileMapLayer.MapToLocal` / `LocalToMap` wherever a tile is involved.** Only fall back to `IsoTransform` for off-tilemap math (camera bounds, debug overlays, future mouse picking outside a tilemap).
- **Y-sort wiring is a three-level stack** — scene root, TileMapLayer, Entities all need `y_sort_enabled = true`. Today's scene files set the latter two; the root is missing and must be added.
- **Sprite anchor is bottom-center of footprint.** When importing PixelLab / ISS art, set the sprite's `offset` so the bottom-center of the diamond footprint sits at `(0, 0)` in sprite-local coordinates. ISS already follows this; PixelLab output may need a one-line offset shim per asset.
- **Diamond collision polygon vertex order:** clockwise from top → `(0, -16), (32, 0), (0, 16), (-32, 0)`. Godot expects clockwise winding for convex polygons.
- **Movement: `Velocity` is pixels per second; no `* delta`.** Set `Velocity`, call `MoveAndSlide()` once per `_PhysicsProcess`. Do not write to `global_position` in the movement path — only for teleports (initial spawn, floor descent).
- **Wall occlusion (shader-based):** the wall-fade lives in a fragment shader on the shared TileMapLayer (see Wall Occlusion section). Push the player's world cell as a `uniform vec2` each frame; tag wall atlas tiles with an `is_wall` custom data flag so the shader can distinguish wall vs floor fragments. Multi-tile object nodes fade via plain `modulate.a` on the node — no shader needed for those.
- **Z-index discipline:** **one** TileMapLayer holds both floors and walls (per [docs/basics/tilemap-and-isometric.md](../basics/tilemap-and-isometric.md) — separate layers break Y-sort interleaving). Entities container is a sibling of that TileMapLayer under the Y-sorted root. No manual `z_index` on tiles or entities; HUD `CanvasLayer` `layer = 100`. Reserve manual z-index only for explicit screen-space overlays.

## Open Questions

1. **Wall occlusion mechanism — shader vs separate wall-tile nodes?** Aligning with the single-TileMapLayer pattern from [docs/basics/tilemap-and-isometric.md](../basics/tilemap-and-isometric.md) means per-cell `modulate.a` is not available, so v1 occlusion needs a fragment shader on the TileMapLayer. The alternative is to make every visible-edge wall a sibling `Sprite2D` under `Entities` (fade via `modulate.a` directly, no shader) — simpler code, more nodes, and it diverges from the canonical tilemap pattern. Spec currently picks **shader**. PO should confirm shader complexity is acceptable for v1 vs accepting the node-per-wall divergence; if neither is acceptable, we defer occlusion entirely to a post-ISO-01 ticket and ship without it.

   *Recommendation:* shader-based fade. Keeps the canonical single-layer pattern intact and matches what the deferred v2 stencil-cut work would need anyway.

2. **Diamond entity colliders post-v1?** Circles at r=12 work fine today and will continue to work after the wall-collision diamond fix, but a diamond `ConvexPolygonShape2D` on the player would give pixel-perfect iso slide feel along walls. Defer to post-ISO-01; not blocking. Spec position: **future work, not v1**.
