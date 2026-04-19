# PixelLab Isometric Sprites (Archived)

**Archived:** 2026-04-18 (per [ADR-007](../../../docs/decisions/007-top-down-oga-pivot.md))
**Reason:** project pivoted from isometric + PixelLab-generated art to top-down + OpenGameArt (LPC) sourced art. These sprites are kept for historical reference — the paradigm repo's audience cares about how the decision evolved, not just the final state.

## What's in here

- `enemies/` — 7 monster species (bat, dark_mage, goblin, orc, skeleton, spider, wolf) with 8-direction rotations + some animation frames. Total: ~215 files.
- `player/{warrior,ranger,mage}/` — isometric player class sprites. Each has `rotations/` (8 directions), usually `animations/` (walk / attack / death where generated), `metadata.json`, and `_theme-review/` PixelLab iteration outputs from the warrior-v2 theme-lock process.
- `npcs/` — mix of stale + rewired:
  - `banker`, `shopkeeper`, `teleporter`, `guild_master` — fully stale from the pre-NPC-roster-rewire 6-NPC design (see [NPC-ROSTER-REWIRE-01](../../../docs/dev-tracker.md)).
  - `blacksmith`, `guild_maid` — only the iso `rotations/` + `metadata.json` were archived; the LPC-generated sheets for these roles live at `assets/characters/npcs/{blacksmith,guild_maid}/`. Village Chief had no PixelLab art generated.

## Referenced by (may still be, until pivot rewrite completes)

- `scripts/Constants.cs` — sprite-path constants.
- `scenes/player.tscn`, `scenes/enemy.tscn` — pre-existing iso scenes.
- `tests/e2e/assets/AssetSandboxTests.cs` — E2E asset-existence tests.

The top-down rewrite (follow-up to ADR-007) will replace these references with paths to the LPC sheets in `assets/characters/`. Until then, iso-era scenes and tests will fail — expected during the transition.

## Restoring if the pivot is reversed

Everything here is still in git history under its original tree location before the move. To resurrect:

```bash
# For the whole archive:
git mv assets/archive/pixellab-iso-sprites/enemies assets/characters/enemies
git mv assets/archive/pixellab-iso-sprites/player/warrior/rotations assets/characters/player/warrior/rotations
# ...etc. for each subtree.
```

Or simply `git log --diff-filter=R --follow` any file to find its prior path.
