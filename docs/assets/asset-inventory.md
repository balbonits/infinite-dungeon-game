# Asset Inventory — Pre-Redraw Snapshot (2026-04-17)

## Summary

Complete inventory of every image asset currently in `assets/` at the point of the iso-pivot redraw decision. Each row is tagged **KEEP** (stays through the redraw) or **REDRAW** (burned down by a named ART-SPEC-* ticket before regeneration).

The snapshot is **frozen at commit 375f42e** — the starting line for the redraw waves. If new assets are added before a redraw ticket closes, append them to the matching redraw bucket.

## Current State

The engine is true-isometric (per SPEC-ISO-01), but every existing character/tile/object was generated for "low top-down" perspective and is mis-anchored for the true-iso render pipeline. The product-owner decision: redraw everything non-exempt in coordinated ART-SPEC-* waves, one family per ticket, deleting the old batch before the new one lands.

## Kept assets (exempt from redraw)

| Path | Why kept |
|---|---|
| `icon.png` (project root) | Godot project icon — engine reference, not in-game art |
| `assets/ui/orb_hp.png` + `.import` | HP orb sphere art — renders via `OrbDisplay.cs` Modulate fill, iso-agnostic (UI) |
| `assets/ui/orb_mp.png` + `.import` | MP orb — same |

Everything below is **REDRAW**. No other exemptions.

## Redraw buckets (grouped by target ticket)

Each bucket lists the exact paths to remove before the regeneration ticket begins. Ticket IDs reference `dev-tracker.md`.

### Bucket A — Player classes (`ART-SPEC-PC-01` / `SPEC-PC-ART-01`)
```
assets/characters/player/warrior/          # all rotations + animations
assets/characters/player/ranger/           # all rotations
assets/characters/player/mage/             # all rotations
```
Delete-before-regen: yes. Old sprites mis-anchored for iso.

### Bucket B — Monster species (`ART-SPEC-02` / `SPEC-SPECIES-01` pairing)
```
assets/characters/enemies/bat/
assets/characters/enemies/skeleton/
assets/characters/enemies/goblin/
assets/characters/enemies/wolf/
assets/characters/enemies/orc/
assets/characters/enemies/spider/
assets/characters/enemies/dark_mage/
```
Delete-before-regen: yes. Bat/Spider/Wolf also need body-plan fix (ART-14) — subsumed by this bucket.

### Bucket C — Town NPCs (`ART-SPEC-NPC-01` / `SPEC-NPC-ART-01`)

**Roster locked to 3 NPCs** (PO confirmation 2026-04-17). Service consolidation:

| NPC | Services owned |
|---|---|
| **Blacksmith** | Forge (FORGE-01), crafting (SYS-06), shop (merged from shopkeeper) |
| **Guild Maid** | Bank (merged from banker), Teleport / floor-select (merged from teleporter, per SYS-05) |
| **Village Chief** | Quest giver (SYS-04 — was mis-wired to Guild Master, see rewire ticket) |

"Guild Master" is the PC's title ("{Class} Guildmaster") per `project_npc_naming.md`, not an NPC. No sprite needed.

**Redraw-in-place (delete + regen with new iso style):**
```
assets/characters/npcs/blacksmith/
assets/characters/npcs/guild_maid/
```

**Remove (no regen — service consolidated elsewhere, dir deleted):**
```
assets/characters/npcs/banker/              → services moved to Guild Maid
assets/characters/npcs/shopkeeper/          → services moved to Blacksmith
assets/characters/npcs/guild_master/        → PC role, not an NPC
assets/characters/npcs/teleporter/          → services moved to Guild Maid
```

**Create net-new (no existing dir):**
```
assets/characters/npcs/village_chief/       → fresh sprite, quest-giver NPC
```

Delete-before-regen: yes for the two redraws. Deletions for the four removed dirs happen in the same redraw PR (single sweep). Code rewire is a separate ticket (`NPC-ROSTER-REWIRE-01`, see dev-tracker).

### Bucket D — Iso tiles (`ART-SPEC-03` / ART-12)
```
assets/tiles/dungeon/              # floor*.png, wall.png, stairs_*.png (7 files)
assets/tiles/dungeon_dark/         # floor_0-5, wall_0-5 (12 files)
assets/tiles/cathedral/            # floor_0-5 (6 files, walls missing)
assets/tiles/nether/               # floor_0-5, wall_0-5 (12 files)
assets/tiles/sky_temple/           # floor_0-5, wall_0-5 (12 files)
assets/tiles/volcano/              # floor_0-5, wall_0-5 (12 files)
assets/tiles/water/                # water_0-5 (6 files, no walls)
assets/tiles/town/                 # town_floor, town_wall, cave_entrance, well, building_* (7 files)
```
Delete-before-regen: yes. New atlas = full Catacombs-style wall/corner/junction/stair coverage per biome, not just floor+wall pairs.

### Bucket E — Environmental objects (`ART-SPEC-04` / ART-13)
Currently empty — no object sprites have been authored yet. Bucket exists as a placeholder for delete-before-regen hygiene (nothing to delete on first run; applies to future iterations). When ART-SPEC-04 ships assets under `assets/objects/` (path to be created), subsequent regenerations follow the same delete-first protocol.

### Bucket F — Containers (`ART-SPEC-05` / ART-15)
Currently empty. Same as Bucket E — placeholder for future iterations. Per SPEC-LOOT-01 will produce `jar_closed.png`, `jar_open.png`, `crate_closed.png`, `crate_open.png`, `chest_closed.png`, `chest_open.png` under `assets/objects/containers/` (path TBD).

### Bucket G — Equipment catalog sprites (`ART-SPEC-06` / ART-03/04/05/06)
Currently empty — no equipment sprites authored yet. 259 slots incoming: 75 armor + 70 weapon/offhand + 9 quiver + 55 neck/ring + 50 consumables/material icons. Paths TBD.

### Bucket H — Projectiles (`ART-SPEC-07`)
```
assets/projectiles/arrow_8dir.png
assets/projectiles/magic_arrow_8dir.png
assets/projectiles/magic_bolt_8dir.png
assets/projectiles/fireball_8dir.png
assets/projectiles/frost_bolt_8dir.png
assets/projectiles/lightning_8dir.png
assets/projectiles/stone_spike_8dir.png
assets/projectiles/energy_blast_8dir.png
assets/projectiles/shadow_bolt_8dir.png
```
Delete-before-regen: yes. New style = D1 Arrow sheet reference (tight, single-frame or 2-frame, 8-dir).

### Bucket I — Ability/skill icons (`ART-SPEC-08` / ART-07a/b)
```
assets/icons/abilities_icons.png + .json
assets/icons/skills_icons.png + .json
assets/icons/spells_icons.png + .json
assets/icons/skill_arrow.png + .import
assets/icons/skill_magic_bolt.png + .import
assets/icons/skill_slash.png + .import
assets/ui/skill-icons/ability_*.png          # 12 ability icons
assets/ui/skill-icons/mastery_*.png          # 27 mastery icons
assets/ui/skill-icons/atlas_*.png + .json    # per-class atlases
```
Delete-before-regen: yes. New style = D1 Spell Icons reference (stone-tile background, white/grey pictograph, red accent for dark spells).

### Bucket J — Portraits (`ART-SPEC-09` / ART-08/09)
Currently empty — no portrait art authored yet. Splash screen currently uses upscaled rotation sprites via `CharacterCard.cs`. New portrait pipeline = 2D UI bust art, not iso.

Note on `assets/ui/splash_background.png` + `.import`: currently used as the title screen backdrop. Whether this is KEEP or REDRAW depends on whether the new class-portrait spec reworks splash composition. Flagged for decision at ART-SPEC-09 time; tentatively **REDRAW** to match the new art direction.

### Bucket K — Effects / FX (`ART-SPEC-FX-01`, not yet ticketed)
```
assets/effects/cathedral_light.png
assets/effects/dust_debris.png
assets/effects/explosion.png
assets/effects/fire_tile.png
assets/effects/heal_aura.png
assets/effects/ice_ground.png
assets/effects/lava_ground.png
assets/effects/lightning_strike.png
assets/effects/magic_circle.png
assets/effects/nether_wisp.png
assets/effects/poison_cloud.png
assets/effects/poison_pool.png
assets/effects/shadow_void.png
assets/effects/shield_bubble.png
assets/effects/sparkle.png
assets/effects/torch.png
assets/effects/volcanic_ash.png
assets/effects/water_puddle.png
```
Delete-before-regen: yes. Deferred — no ART-SPEC-FX-01 ticket open yet. Many of these are ground-effect overlays that may need iso-diamond alignment rules similar to tiles; scope when art-lead finishes the foundational waves.

## Delete-before-regen protocol

Each ART-SPEC-* ticket's implementation block (the ART-* impl ticket it blocks) MUST:

1. Open a dedicated branch for the redraw batch.
2. **Delete the entire bucket's paths as the first commit** (git preserves the v1 assets in history).
3. Regenerate via PixelLab per the spec.
4. Commit the new assets + updated `metadata.json` / `manifest.json` as the second commit.
5. Verify the game builds + renders in-engine before merging.

This order (delete → regen) keeps PR diffs reviewable and prevents accidental "old + new coexisting" states.

## Acceptance Criteria

- [ ] Every path under `assets/` at commit 375f42e appears in one bucket above (KEEP or REDRAW).
- [ ] Every REDRAW bucket names its target ART-SPEC-* ticket.
- [ ] `icon.png`, `orb_hp.png`, `orb_mp.png` are the only KEEP entries.
- [ ] Each redraw ticket cites this inventory and lists its Bucket ID in the Notes field.

## Implementation Notes

- **No compatibility layer.** We do not keep old assets alongside new — the render pipeline will break if both exist, and parallel metadata would confuse the loader.
- **Manifests are authored per ticket**, not globally pre-populated. Each spec authors its own (e.g. ART-SPEC-03 writes the per-biome tile manifest).
- **Scene-file breakage is expected.** Redrawing a character family invalidates `.tscn` references to removed `.png` paths. Each implementation ticket owns fixing the scene-file side alongside the asset swap.
- **Dev-tracker rows** for ART-SPEC-02 through ART-SPEC-09 each link back to this inventory and name the specific bucket(s) they consume.
- **NPC roster is LOCKED at 3** (Blacksmith, Guild Maid, Village Chief). See Bucket C for the service-consolidation table. Code rewire (move QuestPanel → Village Chief, Teleport → Guild Maid, Shop → Blacksmith, Bank → Guild Maid) tracked as `NPC-ROSTER-REWIRE-01`. Must land before Bucket C redraw so the new sprites wire to the right service handlers.

## Open Questions

None.
