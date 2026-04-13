# Sandbox Guide

Isolated testing scenes for assets, systems, and mechanics. Each sandbox runs independently with no game state interference.

## Running Sandboxes

```bash
# Visual (opens Godot window)
make sandbox SCENE=floor-gen

# Headless (console output, pass/fail exit code)
make sandbox-headless SCENE=floor-gen

# Run all headless checks
make sandbox-headless-all

# Launch the sandbox picker GUI
make sandbox SCENE=launcher

# GdUnit4 scene-runner tests
make test-gdunit
```

## Available Scenes

| SCENE name | Category | What it tests |
|---|---|---|
| `sprite-viewer` | Assets | 8-direction textures for all player/enemy characters |
| `tile-viewer` | Assets | All dungeon tile variants + 4×4 tiling seam check |
| `projectile-viewer` | Assets | All projectile sprites in motion |
| `floor-gen` | Systems | Procedural floor generation — rooms, corridors, entrance/exit |
| `inventory` | Systems | Item stacking, overflow, buy/sell, gold tracking |
| `loot-table` | Systems | Drop rate distribution over N kills |
| `bank` | Systems | Deposit/withdraw/expand — item survival |
| `death-penalty` | Systems | XP/item loss calculator, idol logic |
| `skill-tree` | Systems | Skill graph per class, passive bonuses |
| `combat` | Mechanics | Attack configs, damage formula, DPS counter |
| `movement` | Mechanics | 8-way directional snapping, sprite switching |
| `enemy` | Mechanics | Species configs — stats, AI params |
| `stats` | Mechanics | StatBlock sliders with live derived stat view |
| `full-run` | Integration | AutoPilot plays the game: splash → class select → town → dungeon → combat |

## How Headless Mode Works

Every sandbox extends `SandboxBase`. In headless mode (`--headless` flag):
1. `_SandboxReady()` runs as normal
2. `RunHeadlessChecks()` is called automatically
3. Each `Assert(condition, description)` logs ✅ or ❌
4. `FinishHeadless()` prints the summary and exits with code `0` (pass) or `1` (fail)

The `make sandbox-headless` target captures the exit code and prints `✅ Sandbox passed` or `❌ Sandbox failed`.

## Adding a New Sandbox

1. Create `scripts/sandbox/<category>/<Name>Sandbox.cs` extending `SandboxBase`
2. Implement `SandboxTitle`, `_SandboxReady()`, `_Reset()`, `RunHeadlessChecks()`
3. Create `scenes/sandbox/<category>/<Name>Sandbox.tscn` pointing to the script
4. Add to `_sandbox-path` resolver in `Makefile`
5. Add an entry to `SandboxLauncher.cs` Groups array
6. Add GdUnit4 tests in `tests/e2e/<category>/`
