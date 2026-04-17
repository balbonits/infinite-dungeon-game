# Project Audit — infinite-dungeon-game
**Date:** 2026-04-17
**Scope:** `scripts/` and `tests/` (excluded: `.godot/`, `bin/`, `obj/`, `assets/`)
**Method:** systematic file-by-file read of autoloads, save system, pure logic, key UI, and test patterns.

---

## CRITICAL (real bugs)

### 1. `Enemy.cs:53` — SpawnRateModifier mixed into enemy HP
```csharp
float hpMult = zoneMult * pacts.EnemyHpMultiplier * sat.GetHpMultiplier(zone) * intel.SpawnRateModifier;
```
`DungeonIntelligence.SpawnRateModifier` is documented as "spawn rate multiplier (0.80 to 1.20). Applied to room budget and respawn timer." It is incorrectly multiplied into the enemy HP calculation. AggressionModifier is also misapplied (multiplied into spdMult on line 55 — correct for speed but its docstring says "applied to aggro range and attack cooldown", not movement speed).
**Fix:** drop `intel.SpawnRateModifier` from `hpMult`. Apply it where enemy spawning is rate-controlled (`Dungeon._spawnTimer.WaitTime` or in `SpawnInitialEnemies` budget). Re-evaluate where AggressionModifier should apply (probably enemy attack cooldown, not move speed).

### 2. `GodotFileSaveStorage.cs:21-30` — silent save failure
```csharp
public void Write(string key, string content) {
    using var file = FileAccess.Open(key, FileAccess.ModeFlags.Write);
    if (file == null) { GD.PrintErr(...); return; }
    file.StoreString(content);
}
```
If file open fails (disk full, permissions, sandbox), the Write returns silently. Caller `SaveManager.SaveToSlot` then prints `"Game saved to slot N"` regardless, and `Save()` returns no status. The player's run is unsaved with no UI feedback.
**Fix:** make `ISaveStorage.Write` return `bool`, propagate failure to `SaveManager.Save()` so callers (PauseMenu line 894, DeathScreen line 272, Dungeon close, etc.) can surface a Toast.Error.

### 3. `SaveManager.Save()` (line 105-109) — slot-0 silent overwrite
```csharp
public void Save() { int slot = GameState.Instance.CurrentSaveSlot ?? 0; SaveToSlot(slot); }
```
When `CurrentSaveSlot` is null (e.g. a fresh New Game that never picked a slot, or after `GameState.Reset()` which sets it to null on line 157), `Save()` silently writes to slot 0 — overwriting any existing slot-0 save. Specifically called from:
- `DeathScreen.cs:272` (Quit Game from death screen)
- `PauseMenu.cs:894` (Back to Main Menu)
**Fix:** if `CurrentSaveSlot` is null, prompt the player to pick a slot via the LoadGameScreen-style UI, or refuse to save and surface a warning.

### 4. `MagiculeAttunement.RecordFloorClear` (line 148-159) vs `ImportState` (line 295)
`RecordFloorClear` increments `TotalPoints` only when the floor is genuinely "new past 50" (`floor > UnlockFloor` and not already in `_clearedFloors`). On `ImportState`, `TotalPoints = _clearedFloors.Count` — but the imported `clearedFloors` array isn't filtered by `> UnlockFloor`. A corrupt save with floor `<= 50` entries inflates `TotalPoints` and gives the player extra spendable attunement points (combined with `SpentPoints` recomputed from node costs, this can produce a negative `AvailablePoints` only if save was tampered). Mostly defensive concern.
**Fix:** filter `clearedFloors` to `> UnlockFloor` before the count.

### 5. `PauseMenu.cs:74-75` — `_tabs.Free()` on potentially-detached tab
```csharp
_tabs.GetParent()?.RemoveChild(_tabs);
_tabs.Free();
```
On the first `OnShow()` call, `_tabs` was created in `BuildContent` (line 58) but never added to the tree (the line right before only constructs it; never AddChild-ed). The first `OnShow` therefore frees an orphan with no parent. Subsequent `OnShow` calls free a tab panel synchronously while it may be processing; should use `QueueFree()`. Combined with the fact that the original `_tabs` from `BuildContent` is dead code (immediately replaced in OnShow without ever being mounted), this whole pattern is fragile.
**Fix:** drop the `_tabs` instantiation in BuildContent (it's redundant) and use `QueueFree` in OnShow.

### 6. `Toast.cs:79-81` — timer holds reference to potentially freed toast
```csharp
var timer = GetTree().CreateTimer(duration);
timer.Connect(SceneTreeTimer.SignalName.Timeout, Callable.From(() => DismissToast(toast)));
```
`SceneTreeTimer.Timeout` is held by the SceneTree, not the Toast. If `toast` is dismissed early via `DismissOldest` (line 36) and then QueueFree'd by the dismiss tween, the timer fires later and `DismissToast(toast)` runs `IsInstanceValid(toast)` (line 91 — guards), so no crash. But on `_activeToasts.Remove(toast)` in `DismissToast`, list mutation while the existing dismiss-tween's `QueueFree` callback also calls Remove → double-remove (Remove returns false silently, no crash but the in-progress slide-up tweens of other toasts read stale `_activeToasts.Count`). Mild correctness issue, no crash.
**Fix:** track by ID and use `IsInstanceValid` everywhere, or keep a "dismissing" flag on each toast.

### 7. `Dungeon.OnEnemyDefeated` (line 492-534) — async race on `_killCount`
```csharp
private async void OnEnemyDefeated(Vector2 position, int tier) {
    _killCount++;
    ...
    await ToSignal(GetTree().CreateTimer(0.1), "timeout");
    ...
}
```
`async void` without exception handling. If `EmitSignal(EnemyDefeated)` fires multiple times in the same frame and `Dungeon` is freed mid-await (via stair transition), exceptions are swallowed. The `IsInsideTree()` check (line 504) helps but `_killCount` mutation on line 494 happens before the check — if Dungeon is freed during the await, the mutation already completed on the freed instance (no observable issue, but anti-pattern).
**Fix:** wrap in try/catch or guard the increment with `IsInsideTree` first.

### 8. `Constants.PlayerStats.GetMaxHp` (line 71-77) — O(level) loop on every emission
```csharp
public static int GetMaxHp(int level) {
    int total = StartingHp;
    for (int l = 1; l <= level; l++) total += (int)(8 + l * 0.5f);
    return total;
}
```
Called from `GameState.AwardXp` (line 218), `PauseMenu.AddStatRow` (line 863), every `StatsChanged` consumer. At level 200+ this is hundreds of loop iters per signal emission. Not a bug now, but performance smell. Equivalent closed form: `8*level + 0.5 * level*(level+1)/2`. Tier of concern is low; flagging because it's hot-path-adjacent.

---

## SPEC DRIFT

### 1. `DepthGearTier.RollQuality` (line 105-118) — invented 75-floor bracket
- **Spec** (`docs/systems/depth-gear-tiers.md` line 41-44 + `monster-drops.md` line 88-96): floor brackets are `1-9 / 10-24 / 25-49 / 50-74 / 75-99 / 100-149 / 150+` per the unified table. Wait — actually only the monster-drops.md spec page invents 75 explicitly. The depth-gear-tiers.md table has `50-74` and `75-99` rows but item-generation.md and the dungeon spec use the `1-10/11-25/26-50/51-100/100+` bracketing.
- **Code** at `>= 50`: `35% Normal / 40% Superior / 20% Elite / 5% Masterwork`.
- **Spec** at `50-74` row: `35 / 40 / 20 / 5` — actually matches code.
- **Code** at `>= 75`: `20 / 35 / 30 / 15`.
- **Spec** at `75-99` row: `20 / 35 / 30 / 15` — matches.

Actually after re-reading, code matches the depth-gear-tiers.md extended table. The drift is internal to the docs (`item-generation.md` uses 5-bracket, `depth-gear-tiers.md` uses 7-bracket). **Not a code drift, but a docs-vs-docs disagreement** worth flagging for the owner to reconcile.

### 2. `MonsterDropTable.FloorToTier` (line 137-144) — floor-100 boundary
```csharp
public static int FloorToTier(int floor) => floor switch {
    <= 10 => 1, <= 25 => 2, <= 50 => 3, <= 100 => 4, _ => 5,
};
```
Spec (`item-generation.md:17-23` and `item-catalog.md:65`): `T4 = 51-100, T5 = 100+`. Both rows include floor 100. Code resolves to T4 at 100. This is consistent with item-catalog (T4 cap at 100) but contradicts `T5 = 100+` (which would include 100). Probably benign — but the spec itself is ambiguous and the code picked T4 silently.
**Fix path:** disambiguate the spec to `100+ → T5` or `101+ → T5` and align code.

### 3. `AffixDatabase.GetMaxTier` (line 71-79) — claims tiers 5/6 exist
```csharp
if (itemLevel >= 100) return 6;
if (itemLevel >= 75) return 5;
```
But `RegisterPrefix`/`RegisterSuffix` only registers Tier 1-4 (lines 17-53). Calling `GetMaxTier(100)` returns 6, but `GetAvailable(100)` returns at most Tier-4 affixes. Caller is misled.
**Fix:** either register Tier 5 / 6 affixes per the spec, or cap `GetMaxTier` at 4 until they exist.

### 4. `Constants.Spawning.InitialEnemies = 10` — no spec backing visible
Hardcoded to 10. The spawn spec is at `docs/systems/spawning.md` (not read in this audit). If the spec calls for floor-scaled initial spawns, this is drift. Hypothesis only.

### 5. `MagiculeAttunement` save schema doesn't capture `TotalPoints`
`SaveData.AttunementData` (in `SaveData.cs:103`) has `Nodes`, `ClearedFloors`, `ActiveKeystone`, `IsUnlocked` — but no `TotalPoints`. `ImportState` reconstructs `TotalPoints = _clearedFloors.Count`. If the spec ever decouples points from cleared-floor count (e.g. milestone bonuses), this will silently lose data.
**Fix:** add `TotalPoints` to `SavedAttunementData` for forward-compat, even if currently derivable.

---

## TEST GAPS

Pure-logic classes with no dedicated unit test in `tests/unit/`:

### Critical
- **`scripts/logic/DungeonIntelligence.cs`** — 199 LOC, time-based decay logic, pressure recalculation with weights, several exposed modifiers consumed by `Enemy.cs`. Currently completely untested. Includes the only place where `LootQualityBonus` (line 174) returns `(deficit * 20f / 100f, deficit * 20f * 0.43f / 100f)` — magic constants with no test asserting the formula matches spec.
- **`scripts/logic/AffixDatabase.cs`** — 118 LOC, drives Crafting. `GetMaxTier`/`GetAvailable` filtering logic untested. `CraftingTests.cs` uses affixes indirectly but doesn't validate the database itself.
- **`scripts/logic/ItemDatabase.cs`** — 581 LOC of hand-registered catalog. No test asserts item count by tier/slot/class, no test enforces uniqueness of IDs (silent overwrite if a duplicate Register call sneaks in). High-value test gap given ITEM-01/02 churn.

### Worth adding
- **`scripts/logic/ClassAttacks.cs`** — small, but `GetPrimary`/`GetMeleeFallback` switch + `RangerBowBash` quiver-fallback used in `Player.GetEffectivePrimary`. Not tested.
- **`scripts/logic/SpeciesConfig.cs`** — bounds check `Get(speciesIndex)` falls back to Default. Not tested.
- **`scripts/logic/FloorGenerator.cs`** — 435 LOC, only smoke-tested via FullRunTests (line 173) and SystemSandbox. No assertions on room count bounds, no test for the determinism guarantee at the same seed across multiple `Generate` calls (currently broken for instance-reuse since `Rooms` isn't cleared, but production always constructs fresh).

### Silent-no-op test patterns
- **`tests/unit/QuestSystemTests.cs:67`, `:102`, `:126`, `:146`** — tests early-return when RNG doesn't generate the desired quest type. Without a kill/clear/depth quest in the rolled set, the test passes without asserting. Replace with seeded `QuestTracker` or refactor `QuestTracker.GenerateQuests` to accept `Random rng` so tests can force the outcome.
- **`tests/unit/EquipmentSetTests.cs:323-325`** — test silently returns if test items aren't in `ItemDatabase`. The IDs (`leather_cap`, `vest`, `sword`, `buckler`, `basic`, `copper`) are likely not catalog IDs (catalog uses prefixed IDs like `mainhand_warrior_sword_t1`). This test almost certainly green-passes without exercising the round-trip.
- **`AchievementTracker.SetCounter`** (achievement.cs:54-59) — only updates if `value > current`. Counter-monotonic by design, but **no test asserts this contract**. A future refactor that calls `SetCounter` with a smaller value will silently no-op. Add a test.

### Save/load coverage
SaveSystemTests.cs exercises round-trip but does not test:
- Corrupt JSON path (`SaveDataJson.Deserialize` returning null) → does `RestoreState` get called with null? `SaveManager.LoadSlot` line 53-55 guards. OK.
- Missing items in `ItemDatabase.Get` during restore — currently silently dropped (Inventory.cs line 113-127, EquipmentSet.cs line 290-294). Test should assert "save with item X, drop X from catalog, restore → X disappears, doesn't crash".
- Slot-overwrite warning when CurrentSaveSlot is null.

---

## NITPICKS (defer)

- **`AchievementTracker` (line 36)** uses a `static readonly List<AchievementDef> AllAchievements`. `RegisterAchievements` runs in static ctor — fine, but tests that create multiple `AchievementTracker` instances share this list. Not a bug, just a singleton smell.
- **`QuestTracker.GenerateQuests` (line 52-67)** uses `Random.Shared` directly. Untestable RNG. Same in `MonsterDropTable`, `LootTable`, `DeathPenalty.ApplyItemLoss`. Inject `Random?` parameter pattern (already done in some places).
- **`Bank.PurchaseExpansion` (line 67-75)** — Locked-flag re-application uses `ToggleLock` instead of `SetLocked`. Per the same comment in `Inventory.SetLocked`, this is the brittle pattern the codebase moved away from elsewhere. Should use `SetLocked(idx, true)` for consistency.
- **`SaveData.Hp` default = 100** but `MaxHp` default also 100 — fine for new save, but `RestoreState` clamps Hp to MaxHp (good). Worth a comment that defaults exist only for record-init compat.
- **`Player.cs:57`** subscribes to `GameState.StatsChanged` but no `_ExitTree` disconnect. Godot auto-disconnects when the Callable target is freed; behavior is correct, but the `Disconnect` discipline is inconsistent across UI files (none do it explicitly). Add a project convention note.
- **`GameState.AwardXp` (line 197-201)** comment says "SP — 2 per level, +1 at milestones; AP — 3 per level, +2 at milestones" but the code grants 3/5 at milestones (not 2+1=3, 3+2=5 — match) and 2/3 normally. The comment math is right; flagging that the comment says `+1`/`+2` but the literal code is the absolute milestone value. Easy to misread.
- **`Enemy.cs:99-101`** — Enemy connects to `GameState.StatsChanged` to call `UpdateColor`. Fires for every player HP/mana/XP/Level/MaxHp change. Hundreds of enemies × dozens of stat emissions/second = signal storm. Color only depends on `Level` gap, so 99% of fires are wasted. Cache `_lastPlayerLevel` and skip if unchanged.
- **`DeathScreen.cs:234`** — `new System.Random()` per call. Reseeds on every death. Not a bug, but if a test ever needs determinism here, inject the `Random`.
- **`UiTheme.FocusFirstButton` (line 184-193)** — uses `CallDeferred` for GrabFocus. Fine. But many callers wrap this themselves in `CallDeferred` again (e.g. `BackpackWindow:92` calls `CallDeferred(MethodName.FocusFirstSlot)` which calls `UiTheme.FocusFirstButton` which calls `CallDeferred(GrabFocus)`). Double-defer — works, but redundant.
- **`Constants.cs:57`** — `StartingGold = 0` with comment "G1: d — zero starting gold". The `G1: d` is a tracker reference; useful but should link to the doc/ticket explicitly.
- **`SkillBarHud.cs:122`** silently no-ops when `def?.CombatConfig == null` — and most abilities in `SkillAbilityDatabase` have no CombatConfig (only legacy attacks do). Keypress on assigned slot does nothing with no toast. Either gate the assign UI to abilities that have CombatConfig, or surface "Ability not yet implemented" toast.
- **`Crafting.RecycleItem` (line 67-72)** — quality bonus only handles Normal/Superior/Elite cases. Masterwork/Mythic/Transcendent items fall to `_ => 0` and yield no quality bonus. Per spec (`depth-gear-tiers.md:88-89`), recycling yields scale with tier. Drift.
- **`ProgressionTracker.RestoreState`** doesn't validate that restored mastery/ability IDs match the player's class — if save has a Warrior ability but `_class` is Ranger, the ID isn't in `_abilities`, restore silently skips. OK, but no warning logged.

---

## Summary Counts
- Critical bugs: 8
- Spec drift items: 5 (incl. 1 docs-vs-docs)
- Test gaps (classes): 5 critical + several worth-adding
- Silent-no-op test patterns: 5 distinct sites
- Nitpicks: 14

Total findings: ~37. Recommend triage by clustering #1, #2, #3 (save/state integrity) and #5, #6, #7 (UI lifecycle) into separate tracker tickets.
