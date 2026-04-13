# Flow: Save & Load

**Scripts:** `scripts/autoloads/SaveManager.cs`, `scripts/logic/SaveSystem.cs`

## Auto-Save Triggers

Save happens automatically on:
1. `Main.DoLoadTown()` — after transitioning to town
2. `Main.DoLoadDungeon()` — after transitioning to dungeon
3. `PauseMenu.OnMainMenuPressed()` — before returning to main menu

## Save: What's Serialized

`SaveSystem.CaptureState(GameState gs)` captures:

| Field | Source |
|-------|--------|
| SelectedClass | `gs.SelectedClass` |
| Level, HP, MaxHP, Mana, MaxMana | `gs` properties |
| XP, FloorNumber, DeepestFloor | `gs` properties |
| Stats (STR/DEX/STA/INT/FreePoints) | `gs.Stats` |
| Gold | `gs.PlayerInventory.Gold` |
| Items[] | `gs.PlayerInventory` slots → SavedItemStack[] |
| SkillPoints, SkillStates[] | `gs.Skills.CaptureStates()` |
| BankData | `gs.PlayerBank.CaptureState()` |
| QuestData | `gs.Quests.CaptureState()` |
| AchievementData | `gs.Achievements.CaptureState()` |
| SkillBarSlots[] | `gs.SkillHotbar.ExportSlots()` |
| SaturationData | `gs.Saturation.ExportState()` |
| PactRanks[] | `gs.Pacts.ExportRanks()` |
| AttunementData | `gs.Attunement` nodes + floors + keystone |

Serialized to JSON via `System.Text.Json.JsonSerializer`.

## Load: Continue from Splash

```
1. Splash screen → user clicks Character Card
2. SaveManager.Instance.Load():
   a. Read JSON from disk
   b. SaveSystem.Deserialize(json) → SaveData
   c. SaveSystem.RestoreState(GameState, SaveData)
3. All subsystems restored from SaveData
4. Main.LoadTown() called
```

## Load: RestoreState Details

```
SaveSystem.RestoreState(gs, data):
1. Set class, stats, level, HP, mana, XP, floor (all clamped/validated)
2. Create new Inventory(25), set gold, add items via ItemDatabase.Get()
3. Create new SkillTracker, restore states
4. Create new Bank, restore state
5. Create new QuestTracker, restore state (or generate new if null)
6. Create new AchievementTracker, restore state
7. Create new SkillBar, import slots
8. Create new ZoneSaturation, import state + apply offline decay
9. Create new DungeonPacts, import ranks
10. Create new MagiculeAttunement, import state
11. Create new DungeonIntelligence (session-scoped, always fresh)
```

## Two-Pass Walkthrough Test

**Pass 1 (`make sandbox-headless SCENE=full-run`):**
Play game, reach dungeon, kill enemies, save (auto-save on town return).

**Pass 2 (`make sandbox-headless SCENE=full-run-verify`):**
Load save, verify: level, XP, gold, inventory, achievements, floor all match expectations.
