# PixelLab MCP — AI Pixel Art Generation

## Summary

PixelLab is an AI-powered pixel art generation service available as an MCP server in Claude Code. It generates characters (with directional views and animations), isometric tiles, top-down tilesets, sidescroller tilesets, and map objects — all as transparent-background PNGs ready for game use.

**API Docs:** https://api.pixellab.ai/mcp/docs
**Setup Guide:** https://pixellab.ai/vibe-coding

## How It Works

All creation tools are **non-blocking**. They return a job ID immediately; processing happens in the background (typically 2–5 minutes). Check status later with the corresponding `get_*` tool. Downloads require no authentication — the UUID acts as the access key.

**Status indicators:** ✅ completed, ⏳ processing, ❌ error

**Workflow:** Create → get job ID → poll with `get_*` → download when ready

## Available Tools

### Characters

| Tool | Purpose |
|------|---------|
| `create_character` | Generate a character with directional sprite views |
| `animate_character` | Queue animations for an existing character |
| `get_character` | Get character data, status, preview, download URL |
| `list_characters` | List all characters (paginated, filterable by tags) |
| `delete_character` | Permanently delete a character |

### Isometric Tiles

| Tool | Purpose |
|------|---------|
| `create_isometric_tile` | Create a single 3D-looking isometric tile |
| `get_isometric_tile` | Get tile status and base64 PNG data |
| `list_isometric_tiles` | List all isometric tiles (newest first) |
| `delete_isometric_tile` | Permanently delete a tile |

### Top-Down Tilesets (Wang System)

| Tool | Purpose |
|------|---------|
| `create_topdown_tileset` | Generate a 16-tile Wang tileset for seamless terrain transitions |
| `get_topdown_tileset` | Get tileset data and status |
| `list_topdown_tilesets` | List all top-down tilesets |
| `delete_topdown_tileset` | Permanently delete a tileset |

### Sidescroller Tilesets

| Tool | Purpose |
|------|---------|
| `create_sidescroller_tileset` | Generate a 16-tile platformer tileset |
| `get_sidescroller_tileset` | Get tileset data, status, and example map |
| `list_sidescroller_tilesets` | List all sidescroller tilesets |
| `delete_sidescroller_tileset` | Permanently delete a tileset |

### Map Objects

| Tool | Purpose |
|------|---------|
| `create_map_object` | Create a transparent-background pixel art object |
| `get_map_object` | Get object status and data |

### Tiles Pro (Advanced)

| Tool | Purpose |
|------|---------|
| `create_tiles_pro` | Advanced tile creation with extended style options |
| `get_tiles_pro` | Get tile status and data |
| `list_tiles_pro` | List all tiles pro |
| `delete_tiles_pro` | Permanently delete a tile |

---

## Parameter Reference

### create_character

| Parameter | Type | Default | Notes |
|-----------|------|---------|-------|
| `description` | string | — | Character concept / appearance prompt |
| `name` | string | None | Display name |
| `body_type` | enum | `humanoid` | `humanoid` or `quadruped` |
| `template` | string | None | Quadruped only: `bear`, `cat`, `dog`, `horse`, `lion` |
| `mode` | enum | `standard` | Generation mode |
| `n_directions` | enum | `8` | `4` (cardinal) or `8` (full rotation) |
| `proportions` | object | `{"type": "preset", "name": "default"}` | Presets: `default`, `chibi`, `cartoon`, `stylized`, `realistic_male`, `realistic_female`, `heroic` |
| `size` | integer | `48` | Canvas size in pixels (~60% is character height) |
| `outline` | string | `single color black outline` | Outline style description |
| `shading` | string | `basic shading` | Shading depth description |
| `detail` | string | `medium detail` | Detail level description |
| `ai_freedom` | float | `750` | Creative variance |
| `view` | enum | `low top-down` | Camera perspective |

### animate_character

| Parameter | Type | Default | Notes |
|-----------|------|---------|-------|
| `character_id` | string | — | UUID from `create_character` |
| `template_animation_id` | string | None | Predefined animation type |
| `action_description` | string | None | Custom animation modifier |
| `animation_name` | string | None | Named animation variant |
| `directions` | array | None | Specific directional views to animate |
| `confirm_cost` | boolean | `false` | Cost confirmation flag |

### create_isometric_tile

| Parameter | Type | Default | Notes |
|-----------|------|---------|-------|
| `description` | string | — | Tile concept prompt |
| `size` | integer | `32` | Pixel dimension (32 recommended) |
| `tile_shape` | enum | `block` | `thin` (~10%), `thick` (~25%), `block` (~50%) |
| `outline` | enum | `lineless` | `lineless` (modern) or `single color` (retro) |
| `shading` | string | `basic shading` | Depth effect description |
| `detail` | string | `medium detail` | Detail level description |
| `text_guidance_scale` | float | `8.0` | How closely to follow the description |
| `seed` | string | None | For consistency across tiles |

### create_topdown_tileset

| Parameter | Type | Default | Notes |
|-----------|------|---------|-------|
| `lower_description` | string | — | Primary terrain (e.g., "dirt path") |
| `upper_description` | string | — | Secondary terrain (e.g., "grass") |
| `transition_size` | float | `0.0` | `0` = sharp edge, `0.25` = medium blend, `0.5` = wide blend |
| `transition_description` | string | None | Edge decoration details |
| `tile_size` | object | `{width: 16, height: 16}` | Pixel dimensions per tile |
| `outline` | string | None | Line style |
| `shading` | string | None | Depth option |
| `detail` | string | None | Detail level |
| `view` | enum | `high top-down` | `high top-down` (RTS) or `low top-down` (RPG) |
| `tile_strength` | float | `1.0` | Intensity multiplier |
| `lower_base_tile_id` | string | None | Previous tileset base ID for chaining |
| `upper_base_tile_id` | string | None | Alternative base tile reference |
| `tileset_adherence` | float | `100.0` | Constraint strength |
| `tileset_adherence_freedom` | float | `500.0` | Creative variance |
| `text_guidance_scale` | float | `8.0` | Description adherence strength |

### create_sidescroller_tileset

| Parameter | Type | Default | Notes |
|-----------|------|---------|-------|
| `lower_description` | string | — | Platform material (e.g., "stone", "wood", "ice") |
| `transition_description` | string | — | Top decoration (e.g., "grass", "snow", "moss") |
| `transition_size` | float | `0.0` | `0` = no layer, `0.25` = light, `0.5` = heavy coverage |
| `tile_size` | object | `{width: 16, height: 16}` | Pixel dimensions |
| `outline` | string | None | Line style |
| `shading` | string | None | Depth option |
| `detail` | string | None | Detail level |
| `tile_strength` | float | `1.0` | Intensity multiplier |
| `base_tile_id` | string | None | Previous tileset ID for visual consistency |
| `tileset_adherence` | float | `100.0` | Constraint strength |
| `tileset_adherence_freedom` | float | `500.0` | Creative variance |
| `text_guidance_scale` | float | `8.0` | Description adherence strength |
| `seed` | string | None | Reproducibility control |

### create_map_object

| Parameter | Type | Default | Notes |
|-----------|------|---------|-------|
| `description` | string | — | Object concept prompt |
| `width` | integer | None | Pixel width |
| `height` | integer | None | Pixel height |
| `view` | enum | `high top-down` | Camera perspective |
| `outline` | string | `single color outline` | Line style |
| `shading` | string | `medium shading` | Depth effect |
| `detail` | string | `medium detail` | Detail level |
| `background_image` | string | None | Reference background |
| `inpainting` | string | None | Inpainting mode |

### create_tiles_pro

| Parameter | Type | Default | Notes |
|-----------|------|---------|-------|
| `description` | string | — | Tile concept prompt |
| `tile_type` | enum | `isometric` | Tile classification |
| `tile_size` | integer | `32` | Pixel dimension |
| `tile_height` | integer | None | Height override |
| `tile_view` | enum | `low top-down` | Camera perspective |
| `tile_view_angle` | float | None | Angle adjustment |
| `tile_depth_ratio` | float | None | Depth proportion |
| `seed` | string | None | Reproducibility control |
| `style_images` | array | None | Reference image URLs |
| `style_options` | object | None | Custom style parameters |
| `outline_mode` | enum | `outline` | Outline style |

---

## Existing Account Assets

Characters created so far (as of 2026-04-10):

| Name / Description | ID | Directions | Size | Animations |
|----|------|------------|------|------------|
| Full-body male adventurer in leather armor | `194f79ad-cfbb-44fa-bd09-0b45f7feb247` | 8 | 196×196 | — |
| Warrior | `6651c46a-5071-4883-8044-ef70676e84bc` | 8 | 68×68 | 10 |
| Light armored warrior with sword & shield | `1e331004-c023-422d-a4f7-eccb75da2374` | 8 | 92×92 | — |

No isometric tiles, tilesets, or map objects have been created yet.

## Integration with Godot

PixelLab outputs transparent-background PNGs. To use them in Godot:

1. Download the asset ZIP (via the URL returned by `get_*` tools)
2. Extract PNGs into the appropriate `assets/` subdirectory
3. Godot auto-imports PNGs as `CompressedTexture2D`
4. Use with `Sprite2D`, `AnimatedSprite2D`, or `TileSet` as needed

For character sprites, PixelLab generates directional views as separate frames — these map directly to `SpriteFrames` resources for `AnimatedSprite2D`.

PixelLab also provides Godot-specific integration docs:
- `pixellab://docs/godot/isometric-tiles` — Godot isometric tile configuration
- `pixellab://docs/godot/wang-tilesets` — Godot Wang tileset integration
- `pixellab://docs/godot/sidescroller-tilesets` — Godot sidescroller setup

## Tips

- **Seed parameter**: Use the same seed across related tiles for visual consistency
- **Tileset chaining**: Pass `base_tile_id` from one tileset to the next for seamless transitions between biomes
- **text_guidance_scale**: Higher values (10+) follow the prompt more literally; lower values (5–6) give the AI more creative freedom
- **Size matters**: For isometric tiles, 32px is the sweet spot for quality. For characters, larger sizes (64–128+) give more detail
- **View setting**: Use `low top-down` for our isometric ARPG perspective

## Licensing

PixelLab is a paid subscription service. See https://pixellab.ai/termsofservice for terms of use. Generated assets are owned by the user per their subscription terms.
