# ADR-007: Pivot to Top-Down Rendering + OpenGameArt as Primary Asset Source

**Status:** Accepted
**Date:** 2026-04-18
**Supersedes:** [ADR-004](004-iss-tileset-standard.md) (ISS isometric tileset standard)

## Context

Through sessions up to 2026-04-18, the project committed to two load-bearing art decisions:

1. **Isometric 2D rendering** with Screaming Brain Studios' ISS tileset as the sole environment art source ([ADR-004](004-iss-tileset-standard.md)). Grid locked at 64×32 floors / 64×64 walls, magenta transparency, ~819 assets integrated.
2. **PixelLab-generated character art** for all players, NPCs, enemies, and bosses. Multi-week pipeline authoring: prompt templates, canvas specs, batch dispatches, theme reviews, animation research.

Both commitments were showing strain by session 20:

**On isometric.** Every gameplay system that touches visuals inherits isometric overhead:
- `docs/systems/iso-rendering.md` specifies Y-sort wiring, wall-occlusion shader, dual-layer TileMap (floor + wall), bottom-center sprite anchor convention.
- Collision, pathfinding, camera, and FX are all reasoned about in diamond-space.
- The engineering "tax" to ship an MVP scene is large.

**On PixelLab-generated characters.** The char-art pipeline produced ~296 PNGs (players, NPCs, enemies) but required per-class theme reviews, pro/standard-mode tuning, and style-consistency passes that consumed days per MVP-blocker. Warrior v2 was locked as canonical only after multiple retries. Species and boss art were still upstream of first playable content.

**The shift.** The product owner reframed the priority on 2026-04-18: *"we need graphics & UI first, or else we're just building an ASCII game."* Specs are not the bottleneck — Phases A–J are locked. Implementation is the bottleneck, and implementation is gated on art the team can point at today.

OpenGameArt.org's Liberated Pixel Cup (LPC) ecosystem provides a free, consistent, top-down 2D art library covering characters, tiles, monsters, and (via Kenney's CC0 UI Pack) menus — all usable immediately with no generation delay.

## Decision

**Pivot to top-down 2D rendering. OpenGameArt.org free assets become the primary art source for MVP.**

### Rendering

- Top-down 2D (axis-aligned grid). Retire the isometric Y-sort + wall-occlusion stack.
- Tile grid: **32×32 pixels** (LPC standard).
- Character frame: **64×64 pixels** (LPC standard). Sprites anchored top-center on a 32×32 footprint cell.
- Single TileMap layer for floors; optional overlay layer for decorations. No wall occlusion shader.

### Asset stack (MVP)

| Role | Source | License |
|------|--------|---------|
| Tiles (all biomes) | **LPC Base Assets** `tiles/` (37 sheets: dungeon, castle, grass, lava, water, etc.) | mixed (CC0 / CC-BY / CC-BY-SA 4.0 / OGA-BY / GPL) — per-layer |
| Monsters (10 creatures, MVP-capped) | **LPC Monsters** pack (bat, bee, worm×2, eyeball, ghost, man-eater-flower, pumpking, slime, snake) | CC-BY-SA 3.0 (bat = OGA-BY 3.0) |
| Player + NPC characters (6 total) | **LPC Universal Spritesheet Character Generator** (web tool, layered composition) — output PNGs saved into repo | mixed (CC0 / CC-BY / CC-BY-SA 4.0 / OGA-BY / GPL) — per-layer |
| UI (menus, buttons, panels, icons) | **Kenney UI Pack** (5 color themes × ~170 widgets + SFX) | CC0 |
| Font | Press Start 2P (existing lock — [SPEC-UI-FONT-01](../ui/font.md)) | OFL |

Total MVP download size: ~3 MB. All assets already in `assets/downloaded/` as of 2026-04-18.

### Character generation workflow (replaces PixelLab pipeline)

1. Open the hosted LPC Universal Spritesheet Character Generator.
2. For each of the 6 MVP characters (Warrior / Ranger / Mage / Blacksmith / Guild Maid / Village Chief), follow the recipe in [`assets/md/lpc-sprite-recipes.md`](../../assets/md/lpc-sprite-recipes.md) (authored alongside this ADR).
3. Export full spritesheet PNG + credits TXT.
4. Save to `assets/characters/player/{class}/{class}_full_sheet.png` or `assets/characters/npcs/{role}/{role}_full_sheet.png`.
5. Aggregate credits into `assets/characters/CREDITS.md`.

No PixelLab dispatch, no prompt authoring, no theme-review ticket.

## Consequences

### Retired

- [ADR-004](004-iss-tileset-standard.md) is **superseded**. ISS / SBS packs are no longer the environment art source. Existing downloaded ISS assets may remain in `assets/downloaded/` for historical reference.
- `docs/systems/iso-rendering.md` — to be **archived** (moved to `docs/archive/` with a pointer header). Superseded by a new `docs/systems/topdown-rendering.md` (follow-up ticket).
- `docs/basics/tilemap-and-isometric.md` — to be **archived**.
- `docs/assets/sprite-specs.md` — to be **rewritten** for top-down (64×64 frames, top-center anchor, 32×32 footprint).
- `docs/assets/player-class-pipeline.md`, `npc-pipeline.md`, `species-pipeline.md`, `boss-pipeline.md`, `pixellab.md`, `prompt-templates.md` — PixelLab references **retired**. Replaced by [`assets/md/lpc-sprite-recipes.md`](../../assets/md/lpc-sprite-recipes.md) + `docs/assets/asset-sources.md` (follow-up).
- 296 existing PixelLab-generated character PNGs in `assets/characters/{enemies,player,npcs}/` — **archived** to `assets/archive/pixellab-iso-sprites/`, not deleted (historical reference for the paradigm repo).
- Warrior v2 "canonical human reference" memory — **stale**. New canon is whichever LPC-generated Warrior sheet ships. Feedback memory updated post-ADR.

### Open follow-ups (not blocking this ADR)

- **Species roster rewrite.** The current 7-species roster (Bat, Skeleton, Wolf, Spider, DarkMage, Orc, Goblin) has only 1 overlap with the LPC Monsters pack (bat). The species spec and boss-art spec must be rewritten to draw from the 10-creature LPC Monsters gallery. Tracked as a separate **SPEC-SPECIES-LPC-REWRITE-01** ticket; does not block characters/UI/tiles implementation.
- **Top-down rendering spec** — new `docs/systems/topdown-rendering.md` (replaces `iso-rendering.md`): Y-sort still used for depth, but simpler (single layer, top-center anchor, no wall shader). Follow-up ticket.
- **Asset-sources map** — new `docs/assets/asset-sources.md`: table of every in-game entity → exact PNG file path. Authored once the 6 character sheets land.
- **Decision audit** — confirm no other ADR implicitly depends on ISO. ADRs 001/002/003/005/006 checked: none do.

### Kept (no rework)

- All gameplay specs (Phases A–J). Magic, skills, combat, loot, crafting, NPC dialogue, menus, HUD layout, input bindings, movement — none bake in view angle.
- Press Start 2P font ([SPEC-UI-FONT-01](../ui/font.md)).
- NPC roster (3: Blacksmith, Guild Maid, Village Chief — from [NPC-ROSTER-REWIRE-01](../dev-tracker.md)).
- PC class roster (3: Warrior, Ranger, Mage).
- Boss roster structure (8 zone bosses as species-scale-ups) — but the species list feeding it will be rewritten.

## Rationale

**Why this is the right call, in one line each:**

- Art-first unblocks implementation. Specs are not the limiter.
- OpenGameArt is free, consistent, immediate. PixelLab generation was bespoke and slow.
- Top-down is dramatically simpler to ship than isometric. Every system benefits.
- LPC's ecosystem is big enough to outlast MVP. The MVP-cap-at-lpc-monsters rule prevents scope creep while leaving a clear on-ramp for v1 expansion.
- The paradigm repo's value is in process documentation, not art originality. Using battle-tested free assets reinforces the "shipping over artisanal" ethos.

## Licensing posture

Accepted: the most-restrictive license applied to our chosen layers governs the combined derivative work. In practice, using the LPC generator means **CC-BY-SA 4.0** as the binding floor (any layer under CC-BY-SA forces the whole composite under CC-BY-SA). Game code remains unaffected (separate license). A top-level `assets/characters/CREDITS.md` (and eventually `assets/CREDITS.md` covering all OGA sources) lists contributors and links per the CC-BY-SA 4.0 attribution requirement; we ship the generator-exported credits CSV alongside the PNGs. Kenney's CC0 UI assets require no attribution but will be credited anyway out of courtesy.

**LPC Monsters pack** retains its original CC-BY-SA 3.0 (bat = OGA-BY 3.0) license since it's shipped as pre-baked sheets, not composed from the generator.

## Reversibility

Low-risk pivot. Everything rolls back by restoring ADR-004 as active, un-archiving the iso spec docs, and resuming the PixelLab pipeline. The 296 PixelLab sprites are archived at `assets/archive/pixellab-iso-sprites/`, not deleted. Cost of reversal: ~1 session of doc restoration.

This pivot should be treated as load-bearing through MVP ship; revisit only if the LPC aesthetic turns out to actively undermine the game's identity in playtesting.

## Future consideration — "top-down now, iso maybe later"

Observation from 2026-04-18: LPC character sheets are simple top-down spritesheets with well-defined 8-directional frames. If a future session chooses to re-pivot to isometric (or adds an iso mode alongside top-down), **the LPC character sprites can be reused** — an iso renderer can consume them with scaling + anchor adjustments; no new character art generation required. The expensive part of an iso re-pivot would be the environment/tile art and the rendering stack (Y-sort + wall occlusion + diamond grid math), not the characters.

Do **not** act on this now. MVP and a playable game ship first. Recorded here so a future session doesn't re-discover it from scratch.
