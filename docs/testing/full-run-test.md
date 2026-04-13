# Full-Run Integration Test

## Summary

A railed integration test that simulates a complete play session from character creation through endgame, exercising every game system end-to-end. Two layers: pure C# logic (fast, no Godot) and Godot sandbox (scene loading + rendering verification).

## Why

Unit tests verify individual systems in isolation. This test verifies they **work together** in the order a player would encounter them. Catches cross-system wiring bugs, state corruption, and integration regressions that isolated tests miss.

## Coverage

### C# Logic Layer (`tests/unit/FullRunTests.cs`)

Runs via `make test-unit`. No Godot required. Simulates a complete session through 10 phases:

| Phase | Systems Exercised | What's Verified |
|-------|-------------------|-----------------|
| 1. Character Creation | StatBlock, Inventory, Bank, SkillTracker, SkillBar, QuestTracker, AchievementTracker | All classes initialize correctly, starting state is clean |
| 2. Town Shopping | Inventory, ItemDatabase | Buy/sell round-trip, gold math, slot management |
| 3. Bank & Storage | Bank, Inventory | Deposit/withdraw, expansion purchase |
| 4. Crafting | Crafting, AffixDatabase, CraftableItem | Affix application, limits, recycling, display names |
| 5. Dungeon & Combat | FloorGenerator, StatBlock, LootTable, ZoneSaturation, AchievementTracker | Floor structure (seeded), damage calc, XP/gold awards, saturation tracking |
| 6. Progression | StatBlock, SkillBar, SkillState, SkillTracker | Level-up, stat allocation, skill assignment, cooldowns, skill XP |
| 7. Quests | QuestTracker | Generation, kill/clear/depth progress, completion, AllComplete |
| 8. Death & Penalty | DeathPenalty, Inventory, Bank | XP/item loss, idol protection, protection costs |
| 9. Save/Load | Bank, QuestTracker, AchievementTracker, DungeonPacts, ZoneSaturation, MagiculeAttunement | Per-subsystem CaptureState/RestoreState round-trips |
| 10. Endgame | DungeonPacts, ZoneSaturation, MagiculeAttunement, DepthGearTiers | Pact heat + multipliers, saturation decay, passive tree pathing, quality rolls |

### Godot Sandbox Layer (`scripts/sandbox/FullRunSandbox.cs`)

Runs via `make sandbox-headless SCENE=full-run`. Requires Godot. Verifies:

- All core scenes load without error (main, town, dungeon, player, enemy, hud, pause_menu, death_screen)
- Static databases initialized (ItemDatabase, SkillDatabase, AchievementTracker, AffixDatabase)
- Same 10-phase logic walkthrough as assertions via SandboxBase.Assert()

## Pass/Fail Criteria

- **Pass**: All assertions green, exit code 0
- **Fail**: Any assertion red, exit code 1, failing assertion logged to stdout

## RNG Handling

Procedural systems use fixed seeds or avoid randomness:
- FloorGenerator: seeded constructor (`new FloorGenerator(42)`)
- DepthGearTiers: seeded `new Random(42)` passed to `RollQuality()`
- LootTable: only tests `GetGoldDrop()` formula (deterministic base + verified bounds)
- QuestTracker: structure verification (3 quests generated, rewards positive), not exact values

## Commands

```bash
make test-unit                         # C# layer (with all other unit tests)
make sandbox-headless SCENE=full-run   # Godot layer (headless, CI-friendly)
make sandbox SCENE=full-run            # Godot layer (GUI, interactive)
```
