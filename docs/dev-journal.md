# Development Journal

A running log of everything we build, test, learn, and decide — from zero to game. This project is built entirely by AI, directed by a product owner who is learning game development for the first time.

---

## Session 13 — Various Fixes & Visual Polish (2026-04-14)

### What Happened

**Branch:** `fix/various-fixes`

**Fixes:**
- Weighted floor tile variant selection (50% base, 25% secondary, 25% accent) — eliminates chaotic patchwork
- SettingsPanel runtime warning from setting `Size` on FullRect-anchored controls
- Enter key now works as confirm everywhere (CharacterCard + KeyboardNav)

**8-Direction Projectile Sprites:**
- Generated 9 sprite sheets: arrow, magic arrow, magic bolt, fireball, frost bolt, lightning, stone spike, energy blast, shadow bolt
- `Projectile.cs` auto-detects sprite sheets (width > height) and uses frame selection instead of pixel-art-ruining rotation

**18 Animated Effect Sprites:**
- Tile effects: fire, ice, poison pool, lava, shadow void, water puddle, magic circle
- Combat effects: heal aura, shield bubble, explosion, lightning strike, poison cloud
- Environmental: torch, dust/debris, nether wisps, sparkle, cathedral light, volcanic ash

**Rebindable Keybindings:**
- Click-to-rebind UI in Settings > Controls tab
- Persists custom bindings to settings.json
- Skill chord display auto-derives from base action keys
- Reset to Defaults button

**Control Hints:**
- Reusable `UiTheme.CreateHintBar()` component
- Added to Splash Screen and Pause Menu
- Respects `ShowControlHints` setting

---

## Session 12 — Fix & Expand Test Suite (2026-04-12)

### What Happened

**Started from:** `feat/testing-setup` branch had 4 commits scaffolding test infrastructure, but tests didn't compile.

**Ended with:** 302 tests passing (291 unit + 11 integration). Full coverage of all pure-C# logic systems.

### Bugs Found & Fixed

1. **Missing `using Xunit;`** in all 5 test files — `[Fact]` attributes couldn't resolve. Added the import to each file.
2. **SaveSystem.cs references `Autoloads.GameState`** — pulled in by `scripts/logic/*.cs` wildcard in test .csproj files. `Autoloads` is a Godot-specific autoload class, not available in test projects. Fixed by excluding `SaveSystem.cs` from both test project compile lists.
3. **No .NET 8 runtime installed** — only .NET 10 available. Test projects targeted `net8.0` but couldn't run the test host. Fixed by adding `<RollForward>LatestMajor</RollForward>` to both test .csproj files.

### Flow Docs, GodotTestDriver, Debug Telemetry, Versioning

**Flow documentation:** Created `docs/flows/` with 14 step-by-step flow docs covering every player interaction: splash screen, class selection (3 focus zones, confirm tween timing), town (NPC positions, dungeon entrance trigger), NPC panel, shop, bank, blacksmith, dungeon (spawning, stairs, floor wipe), combat (auto-attack, skill hotbar), death (3-step UI), pause menu, progression, save/load, screen transition timing. Each doc traced from actual code, not guessed.

**GodotTestDriver:** Integrated v3.1.66 — AutoPilotActions now wraps GodotTestDriver's `StartAction`/`EndAction` instead of hand-rolled `Input.ParseInputEvent`. Chickensoft ban lifted for testing tools only (convention docs updated).

**Debug telemetry:** `DebugTelemetry.cs` (`#if DEBUG`) — tracks input consumption, signal emissions, state snapshots (every 2s), scene changes. Per-session JSONL output. AutoPilot auto-starts it during walkthroughs.

**Walkthrough rewrite:** FullRunSandbox now runs 3 complete sessions (Warrior, Ranger, Mage). Each does: splash → class select → town → NPC interaction → pause menu → dungeon → combat → achievement check → force death → respawn → verify. Flow doc references in every step.

**Versioning:** SemVer + git tags. Pre-1.0 development. Spec at `docs/conventions/versioning.md`.

### GodotTestDriver Integration Decision

Researched automated testing tools for Godot 4 + C#. Evaluated:
- **GodotTestDriver** (Chickensoft) — MIT license, NuGet package, C# native. Provides input simulation (`PressAction`, `HoldActionFor`, `ClickMouseAt`), drivers for all Godot node types, fixture management, waiting extensions. Same team as `setup-godot` (already in our CI).
- **godot-ui-automation** — Record/playback framework. Good for capturing human input, but less useful for scripted walkthroughs.
- **Hand-built AutoPilot** — Built 3 files (`AutoPilot.cs`, `AutoPilotActions.cs`, `AutoPilotAssertions.cs`) but hit async timing issues with scene changes and paused game state.

**Decision:** Adopt GodotTestDriver as the foundation. Our AutoPilot becomes a thin game-specific wrapper (GameState assertions, NPC interaction helpers) on top of GodotTestDriver's proven primitives. Replaces hand-rolled `Input.ParseInputEvent` with battle-tested `PressAction`/`HoldActionFor`.

Tickets: TEST-06 (integrate package), TEST-07 (game-specific drivers), TEST-08 (full-run walkthrough), TEST-09 (per-sandbox scripts).

### AutoPilot — Player Emulation Library

Built a standalone testing/debugging tool that emulates a human player. Lives in `scripts/testing/` — separate from game code.

**Three files:**
- `AutoPilot.cs` — core Node: step runner, logging, pass/fail assertions, lifecycle
- `AutoPilotActions.cs` — input simulation: `Press()`, `Hold()`, `Release()`, `MoveDirection()`, `MoveToward()`, `WaitFrames()`, `WaitUntil()`, `WaitForTransition()`, `ClickButton()`, `FindButton()`
- `AutoPilotAssertions.cs` — state checks: `Alive()`, `OnFloor()`, `HasGoldAtLeast()`, `EnemiesExist()`, `InventoryHas()`, `AchievementUnlocked()`

**How it works:** AutoPilot attaches to `GetTree().Root` (survives scene changes), injects input via `InputEventAction` + `Input.ParseInputEvent()`, clicks UI buttons via `EmitSignal(BaseButton.SignalName.Pressed)`, and uses Godot's `ToSignal()` async pattern for frame/time waits.

**FullRunSandbox rewritten:** Now launches the real game (`main.tscn`) and AutoPilot plays through: splash → class select → town walk → NPC interaction → pause menu → dungeon entry → combat.

Reusable for any sandbox (combat skill testing, inventory automation) or live game debugging.

### Full-Run Integration Test

Built a railed integration test that simulates a complete play session across 10 phases:

1. Character creation (all 3 classes)
2. Town shopping (buy/sell/stack)
3. Bank (deposit/withdraw/expand)
4. Crafting (affixes, limits, recycling, display names)
5. Dungeon & combat (seeded floor gen, damage calc, loot, saturation)
6. Progression (level-up, stat allocation, skill bar, cooldowns, skill XP)
7. Quest completion (generate, kill/clear/depth, AllComplete)
8. Death & penalty (XP/item loss, idol, bank survival)
9. Save/load (per-subsystem CaptureState/RestoreState round-trips)
10. Endgame (pacts, saturation decay, attunement tree pathing, gear tier rolls)

Two layers:
- **C# logic**: `tests/unit/FullRunTests.cs` — 13 tests, runs via `make test-unit`
- **Godot sandbox**: `scripts/sandbox/FullRunSandbox.cs` — runs via `make sandbox-headless SCENE=full-run`

Includes a `FullSession_WarriorPlaythrough_AllSystemsIntegrate` test that chains all phases into one continuous play session with shared state.

### Tests Added (199 new unit tests)

| File | System | Tests |
|------|--------|-------|
| `DungeonPactsTests.cs` | DungeonPacts | 18 |
| `ZoneSaturationTests.cs` | ZoneSaturation | 19 |
| `SkillBarTests.cs` | SkillBar | 19 |
| `SkillStateTests.cs` | SkillState | 14 |
| `AchievementSystemTests.cs` | AchievementTracker | 15 |
| `CraftingTests.cs` | Crafting + AffixDatabase | 16 |
| `QuestSystemTests.cs` | QuestTracker | 10 |
| `DepthGearTierTests.cs` | DepthGearTiers | 15 |
| `LootTableTests.cs` | LootTable | 4 |
| `MagiculeAttunementTests.cs` | MagiculeAttunement | 21 |

### What We Learned

1. **Test project wildcard includes need exclusion lists.** `scripts/logic/*.cs` is convenient but pulls in files with Godot-specific dependencies like `Autoloads`. Always check what the wildcard catches.
2. **Timestamp-based tests need care.** ZoneSaturation's decay logic guards against `LastDecayTimestamp <= 0`, so tests must use positive timestamps.
3. **`RollForward` is the clean fix for runtime version mismatches** when you can't install the exact target framework.

---

## Session 11 — Endgame Systems, Mana, Skill Execution, UI Overhaul (2026-04-11)

### What Happened

**Started from:** Specs reconciled, all formulas locked.

**Ended with:** All 5 END systems implemented, full mana system, skill execution engine, Diablo-style HUD, 8 projectile sprites, 4 reusable UI components, settings panel, tutorial, backpack window, debug console.

### Systems Built

| System | Scripts | Status |
|--------|---------|--------|
| Zone Saturation | `ZoneSaturation.cs` | Done — per-zone difficulty, builds on kills, decays, stat/reward multipliers |
| Dungeon Pacts | `DungeonPacts.cs` | Done — 10 pacts, heat scoring, enemy stat multipliers |
| Dungeon Intelligence | `DungeonIntelligence.cs` | Done — 4 performance metrics, adaptive pressure score |
| Magicule Attunement | `MagiculeAttunement.cs` | Done — 40-node passive tree, floor tracking, keystones |
| Depth Gear Tiers | `DepthGearTier.cs` | Done — 6 quality tiers (Normal→Transcendent), floor-gated |
| Mana system | `GameState.cs` | Done — Mana/MaxMana, class pools (M:200/R:100/W:60), INT regen |
| Skill execution | `SkillDef.cs`, `SkillBar.cs`, `Player.ExecuteSkill()` | Done — all 80+ skills castable with mana/cooldowns/targeting |
| Skill bar HUD | `SkillBarHud.cs` | Done — 4 slots, shoulder+face combos (Q+W/Q+S/E+W/E+S) |
| HP/MP orbs | `OrbDisplay.cs`, `Hud.cs` | Done — Diablo-style glass sphere sprites, fill/drain |
| XP bar | `XpBar.cs` | Done — below skill bar, level-up glow, XP loss flash |
| Backpack window | `BackpackWindow.cs` | Done — 5x5 slot grid, action menu, accessible from pause |
| Settings panel | `SettingsPanel.cs` | Done — 4 tabs, 20+ settings, persisted to JSON |
| Tutorial | `TutorialPanel.cs` | Done — 4 tabs, static reference |
| Debug console | `DebugConsole.cs` | Done — F4, god mode, cheats, teleport, perf metrics |
| Character card | `CharacterCard.cs` | Done — reusable, on title screen for saved games |
| Action menu | `ActionMenu.cs` | Done — FF-style popup for item/skill context actions |
| Reusable components | `GameWindow.cs`, `TabBar.cs`, `ScrollList.cs`, `ContentSection.cs` | Done |
| Window stack | `WindowStack.cs` | Done — central input routing, no bleed-through |

### Key Fixes

- **Mage auto-attack**: staff melee is free auto-attack; magic bolt is mana skill via hotbar
- **Monster spawn**: guaranteed 10 per floor via `SpawnInitialEnemies()` loop; floor wipe requires 10+ kills
- **Transition screens**: all scene loads (town/dungeon/floor) use `ScreenTransition.Play()`
- **Input isolation**: every dialog blocks ALL keyboard input when open; WindowStack prevents bleed-through
- **UI color system**: blue buttons (#518ef4), gold headings only, distinct semantic colors
- **Sub-dialog focus restoration**: Skills/Stats/Ledger return to PauseMenu with focus on close
- **Keyboard scroll**: `KeyboardNav.EnsureVisible()` auto-scrolls ScrollContainer to focused button
- **NpcPanel cancel**: D/Escape now closes NPC panel (was missing)

### Projectile Sprites (PixelLab)

arrow, magic_bolt, fireball, frost_bolt, lightning, stone_spike, energy_blast, shadow_bolt — all 32x32 pixel art.

### What We Learned

1. **Build complete systems, not skeletons.** Skills that show a toast but deal zero damage are not "done." Every system must work end-to-end: input → effect → visual → persistence.

2. **Never invent features.** The specs are the source of truth. If it's not documented, don't build it. A "difficulty setting" that doesn't exist in any spec is hallucination, no matter how "obvious" it seems.

3. **Trace all callers before modifying shared functions.** `LoadDungeon()` is called from 4 places. Adding a transition wrapper broke 3 of them because they already had their own transitions.

4. **WindowStack > per-window checks.** Checking "is SettingsPanel open? is ActionMenu open?" in every parent is fragile and grows linearly. A central stack tracks topmost and blocks everything below.

5. **Reusable components save hundreds of lines.** GameWindow, TabBar, ScrollList, ContentSection — 4 components eliminated 30+ lines of boilerplate per dialog.

6. **Dividers go at section bottom, not top.** Prevents double dividers when the parent already has a top separator.

7. **Buttons need consistent sizing.** If two buttons sit next to each other, they need the same height, font size, and padding. StyleButton vs StyleSecondaryButton can't have different sizes.

---

## Session — 2026-04-11

### What Happened

**Started from:** Phase 0+0.5 complete, Phase 1 partial (SYS-01 and SYS-03 done, rest pending/partial).

**Ended with:** All Phase 1 systems complete + procedural floor generation + codebase hardening.

### What We Built

#### Codebase Audit & Hardening
- Fixed unsafe `new Random()` → `Random.Shared` in DeathPenalty.cs
- Added JSON deserialization error logging in SaveSystem
- Added save data validation (bounds checking all fields on load)
- Created `IDamageable` interface to replace unsafe `.Call("TakeDamage")` string dispatch
- Replaced all `.Call()` in Player.cs with type-safe `is IDamageable` pattern matching
- Extracted duplicated stairs creation logic in Dungeon.cs into `PlaceStairs()` method

#### SYS-05: Level Teleporter NPC (completed partial)
- Built `TeleportDialog.cs` — floor selection UI with zone labels
- Wired Teleporter NPC service button to open the dialog
- Added to main scene

#### SYS-10: Monster Families (completed partial)
- Added `Constants.Zones` with 10-floor zone system and species-to-zone mapping
- Updated `Dungeon.GetRandomAvailableSpecies()` to use zone-gated species

#### SYS-02: Skill System + Use-Based Leveling
- `SkillDef.cs` — immutable skill definition record
- `SkillState.cs` — mutable skill XP/level tracking with diminishing returns passive bonuses
- `SkillDatabase.cs` — complete registry of 80+ skills for all 3 classes (Warrior: 7 base + 28 specific, Ranger: 7 base + 28 specific, Mage: 9 base + 36 specific)
- `SkillTracker.cs` — manages all skill states, use-based XP, skill point allocation, passive bonuses
- `SkillTreeDialog.cs` — hierarchical skill browser UI with allocation buttons
- Integrated into GameState, SaveData, SaveSystem, ClassSelect, PauseMenu
- Skill points awarded on level-up (2 per level, 3 at milestones)

#### SYS-07: Bank System
- `Bank.cs` — pure logic: 50 start slots, deposit/withdraw, expansion at 500*N^2
- `BankWindow.cs` — two-column UI (bank | backpack), item transfer, expansion purchasing
- Wired to Banker NPC service button
- Full save/load support

#### SYS-06 + SYS-08: Items & Crafting
- `Affix.cs` — AffixDef record, AppliedAffix, AffixType/AffixCategory enums
- `AffixDatabase.cs` — 28 affixes across 4 tiers (1/10/25/50+ item level gates)
- `Crafting.cs` — deterministic affix application (max 3 prefix + 3 suffix), CraftableItem model, BaseQuality enum, recycling
- `BlacksmithWindow.cs` — Craft/Recycle tab UI, wired to Blacksmith NPC

#### SYS-04: Quest System
- `QuestSystem.cs` — QuestDef/QuestState/QuestTracker, 3 quest types (Kill/ClearFloor/DepthPush), scaling rewards
- `QuestPanel.cs` — quest list UI with progress, claim, and refresh buttons
- Wired to Guild Master NPC and EventBus signals
- Quest tracking: enemy kills, floor clears, floor descent all update quest progress
- Full save/load support

#### SYS-09: Achievement System (Dungeon Ledger)
- `AchievementSystem.cs` — counter-based tracker, 30 achievements across 5 categories (Combat/Exploration/Progression/Economy/Mastery)
- `DungeonLedger.cs` — achievement browser UI with progress bars and unlock status
- Counter updates wired to enemy kills, level-ups, floor descent, floor wipes
- Gold rewards auto-applied on unlock with toast notifications
- Full save/load support

#### Procedural Floor Generation
- `FloorGenerator.cs` — BSP room placement → Drunkard's Walk corridors → Cellular Automata smoothing
- Floor size scales with depth (50x50 at floor 1, up to 150x150)
- 5-8 rooms per floor, ordered into IKEA-path chain (nearest-neighbor)
- Optional loop corridors (15% chance)
- Integrated into Dungeon.cs, replacing the old single-rectangle generation

### New Files Created (15 new scripts)
| File | Lines | Purpose |
|------|-------|---------|
| `scripts/logic/IDamageable.cs` | 10 | Type-safe damage interface |
| `scripts/logic/SkillDef.cs` | 36 | Skill definition record |
| `scripts/logic/SkillState.cs` | 80 | Skill XP/level tracking |
| `scripts/logic/SkillDatabase.cs` | 230 | 80+ skill registry |
| `scripts/logic/SkillTracker.cs` | 140 | Player skill manager |
| `scripts/logic/Bank.cs` | 105 | Bank storage logic |
| `scripts/logic/Affix.cs` | 40 | Affix data model |
| `scripts/logic/AffixDatabase.cs` | 125 | 28 affix definitions |
| `scripts/logic/Crafting.cs` | 105 | Blacksmith crafting logic |
| `scripts/logic/QuestSystem.cs` | 200 | Quest tracking system |
| `scripts/logic/AchievementSystem.cs` | 175 | Achievement tracking system |
| `scripts/logic/FloorGenerator.cs` | 220 | Procedural dungeon generation |
| `scripts/ui/TeleportDialog.cs` | 150 | Teleporter NPC UI |
| `scripts/ui/SkillTreeDialog.cs` | 220 | Skill tree browser UI |
| `scripts/ui/BankWindow.cs` | 230 | Bank deposit/withdraw UI |
| `scripts/ui/BlacksmithWindow.cs` | 200 | Blacksmith crafting UI |
| `scripts/ui/QuestPanel.cs` | 200 | Quest list UI |
| `scripts/ui/DungeonLedger.cs` | 200 | Achievement browser UI |

---

## Session 1 — 2026-04-08

### What Happened

**Started from:** 26 locked specs, 165 tickets, zero code. No C# project, no scenes, no scripts.

**Ended with:** A working Godot 4 + C# pipeline, rendered dungeon room with a character, and scripted movement demo.

### What We Built (in order)

#### Test 1: Hello World
- **What I was asked:** "Create a Hello World thing — see if we could even start a basic app from you coding everything."
- **What I coded:**
  - `DungeonGame.csproj` — C# project file (Godot.NET.Sdk 4.6.2, net8.0)
  - `scripts/HelloWorld.cs` — prints to console, auto-quits in headless mode
  - `scenes/hello_world.tscn` — scene with two labels
  - Updated `project.godot` for .NET runtime, set main scene, removed old GUT plugin
- **What the user saw:** Console output confirming C#, Godot, and .NET versions. Then launched windowed — saw text labels on screen.
- **Result:** PASS. Pipeline verified end to end.
- **Evidence:** `docs/evidence/hello-world/notes.md`

#### Test 2: Asset Render
- **What I was asked:** "Next is basic assets render. Have a character & a floor show on screen."
- **What I coded:**
  - `scripts/AssetTest.cs` — procedurally creates a 15x11 tile room with DCSS assets
  - `scenes/asset_test.tscn` — scene with 3x zoom camera
  - Used DCSS paper doll system: base body (`human_male.png`) + armor overlay (`chainmail.png`) + weapon overlay (`long_sword.png`)
  - Floor: `grey_dirt_0_new.png`, Walls: `brick_dark_0.png`
- **What the user saw:** A dungeon room with dark brick walls, grey dirt floor, and a warrior (human + chainmail + longsword) standing in the center. Top-down view at 3x zoom.
- **User feedback:** "yup! top-down, but it's there. good test on rendering."
- **Result:** PASS. DCSS tiles render correctly. Paper doll layering works.
- **Evidence:** `docs/evidence/asset-render/notes.md`

#### Test 3: Scripted Movement Demo
- **What I was asked:** "Next, input. Have a scripted demo of the character going up, down, left, and right. Have a 1 sec between commands."
- **What I coded:**
  - Updated `scripts/AssetTest.cs` with a state machine: waiting → moving → waiting
  - Scripted sequence: UP → DOWN → LEFT → RIGHT with 1s pauses
  - Movement: 96 px/s, 2 tiles per move, smooth linear interpolation
  - All 3 paper doll layers move together (Node2D container)
  - Auto-quits after demo completes
- **What the user saw:** The warrior moved smoothly in all 4 directions with pauses between each move, then the window closed automatically.
- **User feedback:** "i saw it"
- **Result:** PASS. Smooth movement, layers stay composited.
- **Evidence:** `docs/evidence/movement-demo/notes.md`

### Tooling Created

| Tool | Purpose |
|------|---------|
| `make build` | dotnet build |
| `make run` | Build + launch windowed |
| `make run-headless` | Build + run + auto-quit (CI/testing) |
| `make verify` | Full pipeline check (build + headless run + confirm output) |
| `make doctor` | Environment health check (Godot, .NET, .csproj, main scene) |
| `make kill` | Kill lingering Godot processes |
| `make branch T=X` | Create ticket branch |
| `make done` | Squash-merge branch to main |
| `make status` | Git + build + versions + ticket count |

### Problems Hit

| Problem | How We Fixed It |
|---------|----------------|
| `godot` in PATH was non-.NET version | Found `Godot_mono.app` in Applications, used full path in Makefile |
| No .csproj existed | Created manually, Godot updated SDK version on import |
| Headless Godot didn't auto-quit | Added `DisplayServer.GetName() == "headless"` check + `GetTree().Quit()` |
| Godot process lingered after headless run | Added `make kill` target |
| `.import` files flooded git status | Added `*.import` to .gitignore |

### What We Learned

1. **Godot .NET requires the Mono variant** — the standard `godot` binary from homebrew doesn't support C#. Need `/Applications/Godot_mono.app`.
2. **DCSS paper doll system works** — layer sprites on a Node2D container and they composite via transparency. Body + armor + weapon all stack correctly.
3. **32x32 DCSS tiles render clean** — `TextureFilter = Nearest` preserves pixel art at any zoom level.
4. **Headless mode needs explicit quit** — Godot won't exit on its own after `_Ready()` unless you call `GetTree().Quit()`.
5. **Godot generates `.import` files** for every asset it scans — these are binary cache files that shouldn't be committed.
6. **`_Process(delta)` works for frame-based movement** — multiply speed by delta for frame-rate independent movement.

### Decisions Made This Session

| Decision | Rationale |
|----------|-----------|
| Flipped CLAUDE.md from "docs only" to coding | All 26 specs locked, ready to implement |
| Makefile rewritten for C# (not GDScript) | Old targets were for GUT/gdlint, now uses dotnet build/test |
| Main scene set to asset_test.tscn | Current test scene, will change as we progress |
| Godot mono path hardcoded in Makefile | Reliable — won't accidentally use non-.NET version |

### What's Next

The three pipeline tests (hello world → asset render → movement) proved:
- C# compiles and runs ✓
- Assets load and display ✓
- Movement code works ✓

Next logical step: **player-controlled movement with arrow keys** — the first real gameplay input. This is P1-04c (arrow key input) and P1-04d (isometric transform).

---

## Session 2 — 2026-04-08

### What Happened

**Started from:** 3 pipeline tests passed (hello world, asset render, scripted movement). Zero game systems.

**Ended with:** A complete automated game systems demo — 36 scripted steps exercising 16+ game mechanics, all running on real game logic with spec-accurate formulas.

### What We Built (in order)

#### Game Core Systems (`scripts/game/GameCore.cs`)
- **What was coded:**
  - 6 enums: `ItemType`, `EquipSlot`, `MonsterTier`, `TargetPriority`, `GameLocation`, `StatusEffect`
  - 5 data classes: `PlayerState`, `ItemData`, `MonsterData`, `SkillData`, `GameSettings`
  - `GameState` static singleton — holds all game state (player, settings, location, monsters, skills)
  - `GameSystems` static class — 20+ methods covering combat, inventory, shop, leveling, dungeon, death/respawn, status effects, settings, mana regen, save, and item factory
- **Formulas implemented (from specs):**
  - XP curve: `floor(L^2 * 45)` (from leveling.md)
  - Player damage: `12 + floor(level * 1.5) + weapon_damage` (from combat.md P1 placeholder)
  - Monster damage: `3 + tier` (from combat.md)
  - Defense DR: `defense * (100 / (defense + 100))` (from items.md)
  - HP on level-up: `floor(8 + level * 0.5)` increase, 15% heal (from leveling.md)
  - Monster HP/XP: tier-based with floor multiplier `1 + (floor - 1) * 0.5` (from leveling.md)
  - Stat/skill points: 3 stat + 2 skill per level, bonus at milestones (from leveling.md)

#### Automated Demo Scene (`scripts/GameDemo.cs` + `scenes/game_demo.tscn`)
- **What was coded:**
  - 36-step automated demo running through all basic game mechanics
  - Visual: dungeon room with DCSS tiles, paper doll character, colored rectangle entities
  - UI overlay: stats bar (top), event log (bottom), pop-up panel
  - Each step calls real GameSystems methods and logs inputs + results
  - Entities (monsters, NPCs, chests) spawn as colored sprites, fade out on removal
  - Movement animation between steps
  - Auto-quit after 5 seconds post-completion

### Demo Sequence (36 steps across 5 phases)

| Phase | Steps | Mechanics Tested |
|-------|-------|-----------------|
| **Town** | 1-12 | Init, movement (4 dir), stats panel, settings change, NPC dialog, shop buy (3 items), equip weapon + armor |
| **Dungeon** | 13-22 | Enter dungeon, chest interaction, Tier 1 combat (attack, take damage, kill, XP+gold+loot), Tier 2 combat (skill, heal with potion, crit) |
| **Boss Fight** | 23-27 | Tier 3 boss, poison DOT, multiple attack rounds, skill usage, health potion + poison tick, boss kill + rare loot, mana regen |
| **Death & Respawn** | 28-30 | Two enemies spawn, fatal damage, death, respawn in town at half HP/MP |
| **Wrap Up** | 31-36 | Sell loot, unequip, inventory test, exit dungeon, save game state, final stats summary |

### Console Output (key moments)

```
[INIT] Player: Demo Hero Lv.1 — HP:108/108 MP:65/65 Gold:150
[SHOP] Bought 1x Iron Sword for 50g
[EQUIP] Equipped Iron Sword (+8 damage) — Total damage: 21
[DUNGEON] Entered the dungeon! Floor 1
[SPAWN] A Giant Rat appears! — HP:30/30 Tier:1 XP:10
[ATTACK] Basic attack -> 21 damage to Giant Rat
[SKILL] Used Slash! (-15 MP) -> 35 damage
>> LEVEL UP! Now Level 2 <<
[BOSS] Orc Warlord appears! — HP:54/54 Tier:3 XP:20
[STATUS] Poisoned! (3 dmg/tick, 3 ticks)
>> YOU DIED <<
[RESPAWN] Returned to town with half HP/MP
[SAVE] Game state saved: Demo Hero Level 2, Gold:82, 4 items
```

### Files Created

| File | Purpose | Lines |
|------|---------|-------|
| `scripts/game/GameCore.cs` | All game data models, state, and systems | ~310 |
| `scripts/GameDemo.cs` | Automated 36-step demo scene script | ~480 |
| `scenes/game_demo.tscn` | Demo scene (Node2D + Camera2D) | 8 |

### Files Modified

| File | Change |
|------|--------|
| `project.godot` | Main scene changed from `asset_test.tscn` to `game_demo.tscn` |

### Problems Hit

| Problem | How We Fixed It |
|---------|----------------|
| `icon.svg` missing (non-critical error on launch) | Ignored — cosmetic only, doesn't affect functionality |
| None critical | First-time clean build and run with zero code fixes needed |

### What We Learned

1. **Game systems can be pure C# with no Godot dependency.** GameCore.cs uses only `System` and `System.Collections.Generic` — no Godot imports. This means the logic is testable independently of the engine.
2. **Step-based demo pattern works well.** A list of `(delay, action)` tuples with a timer in `_Process()` is a clean way to script automated sequences. Simpler than coroutines or state machines for linear demos.
3. **Colored rectangles are good enough for entity placeholders.** `Image.CreateEmpty()` + `Fill()` + `ImageTexture.CreateFromImage()` creates instant colored sprite textures. No asset files needed for prototyping.
4. **CanvasLayer separates UI from game world.** UI elements on a CanvasLayer aren't affected by Camera2D zoom — exactly right for stats bars and event logs.
5. **All spec formulas translate directly to code.** XP curve, damage formula, defense DR, level-up rewards — everything in the specs maps 1:1 to simple arithmetic. The specs are doing their job as implementation blueprints.
6. **Tween-based fade-out for entity removal** — `CreateTween().TweenProperty(node, "modulate:a", 0.0, 0.3)` is clean and visual.

### Decisions Made This Session

| Decision | Rationale |
|----------|-----------|
| Pure C# game systems (no Godot in GameCore.cs) | Keeps logic testable, portable, and clean |
| Step-based demo pattern | Simple, readable, easy to extend |
| Colored rectangles for entities | Fast prototyping — real DCSS sprites can be swapped in later |
| 36 steps across 5 phases | Comprehensive coverage of all requested mechanics |
| Main scene switched to game_demo.tscn | Current working demo is the active scene |

### What's Next

The automated demo proves all basic systems work. Next logical steps:
- **Player-controlled movement** (arrow keys + isometric transform) — P1-04c/04d
- **Real DCSS sprites for monsters/items/NPCs** — replace colored rectangles
- **Collision detection** (CharacterBody2D + walls)
- **Real-time combat** (enemies move, attack, die with sprites)
- **HUD** (HP/MP orbs, minimap, shortcut bar)

---

## Session 2b — 2026-04-08 (continued)

### What Happened

**Started from:** Working automated demo with colored rectangle entities and basic UI.

**Ended with:** DCSS sprites for all entities, Diablo-style HP/MP orbs, styled dark-fantasy window UIs (game/shop/dialog), visual combat feedback (slash effects, floating damage numbers, hit flashes, skill bursts, poison tint, death fade), 51 unit tests, 40 E2E assertions, and a Makefile test pipeline.

### What We Built

#### DCSS Sprite Integration
- Replaced all 8 colored rectangle entities with real DCSS 32x32 sprites
- Sprite paths: `monster/animals/rat.png`, `monster/undead/skeletons/skeleton_humanoid_large_new.png`, `monster/orc_warrior_new.png`, `monster/death_knight.png`, `monster/undead/shadow_new.png`, `monster/wizard.png`, `monster/deep_dwarf.png`, `dungeon/chest.png`
- Fallback to colored rectangle if sprite not found (graceful degradation)

#### HP/MP Orbs (`scripts/game/HpMpOrbs.cs`)
- Diablo-style globe indicators using `Control._Draw()` override
- Fill-from-bottom using horizontal line sweep (iterates Y values, calculates circle half-width at each row)
- Glass highlight effect (semi-transparent white arc at top)
- Double metallic border (outer dark, inner lighter)
- Updates via `QueueRedraw()` on value change
- Positioned in CanvasLayer screen space using `GetViewportRect().Size`

#### Styled Window UIs
- `CreateStyledWindow()` helper builds dark-fantasy panels matching `scene-tree.md` HUD spec
- `StyleBoxFlat`: bg `rgba(22,27,40,0.9)`, border gold `rgba(245,200,107,0.4)`, 2px border, 8px corners
- Three windows: game stats (center), shop (center, with item icons), dialog (bottom, with NPC portrait)
- Item icons via `TextureRect` inside `Panel` — DCSS weapon/potion/armor sprites shown next to shop text
- NPC portrait via `TextureRect` — wizard sprite in dialog window

#### Visual Feedback Effects
- **Floating damage numbers** — Labels that tween upward and fade. Red for player damage, white for monster, yellow for crits, green for heals, purple-green for poison.
- **Slash effect** — `Polygon2D` bar (26x4px, gold) with random rotation, fades + drifts up in 150ms. Matches combat.md spec.
- **Skill burst** — Expanding colored `Sprite2D` circle (3.5x scale), fades in 300ms.
- **Hit flash** — Entity `Modulate` tweens to white/color then back in 200ms total.
- **Poison tint** — Character `Modulate` set to green-yellow during poison.
- **Death fade** — Character `Modulate` tweens to dark red at 40% alpha over 800ms.
- **Level up** — Big floating "LEVEL UP!" text (gold) + character flash (yellow).

#### Unit Testing (`tests/`)
- `DungeonGame.Tests.csproj` — xUnit on net10.0, includes GameCore.cs via `<Compile Include>` (source link, no Godot dependency)
- 51 tests across 8 test classes: CombatTests, InventoryTests, ShopTests, LevelingTests, DungeonTests, DeathRespawnTests, StatusEffectTests, SkillTests, SettingsTests, SaveTests
- Disabled parallel execution via `[assembly: CollectionBehavior(DisableTestParallelization = true)]` — required because GameState is static/shared

#### E2E Testing
- `tests/e2e_demo_test.sh` — Runs demo in headless mode, asserts 40 console output patterns (every phase, mechanic, and state transition)
- `tests/e2e_visual_test.sh` — Captures screenshots + video using macOS `screencapture` (needs Screen Recording permission)
- Headless demo runs at 10ms step delay (instant) for fast CI

#### Makefile Targets
- `make test` — unit tests (xUnit, no Godot)
- `make e2e` — headless E2E demo assertions
- `make e2e-visual` — screenshot + video capture
- `make test-all` — unit + E2E combined

### Problems Hit

| Problem | How We Fixed It |
|---------|----------------|
| Test project picked up by main build | Added `<Compile Remove="tests/**" />` to `DungeonGame.csproj` |
| .NET 8 runtime not installed (only 10) | Changed test project to `net10.0` |
| xUnit parallel execution corrupted static GameState | Added `[assembly: CollectionBehavior(DisableTestParallelization = true)]` |
| `timeout` command doesn't exist on macOS | Removed timeout, used Godot's built-in auto-quit instead |
| `screencapture` needs non-interactive flag | Needs `-x` flag for silent capture; Screen Recording permission required |
| `LayoutPreset` not found in Node2D context | Changed to `Control.LayoutPreset.FullRect` (fully qualified) |

### What We Learned

1. **GameCore.cs has zero Godot dependency — and that's powerful.** By keeping all game logic in pure C# (System.Random instead of GD.Randf, no Godot imports), the entire game engine is testable with plain xUnit. No Godot runtime needed for 51 unit tests. This is the #1 architecture win.

2. **Static singletons and test parallelism don't mix.** GameState is static, so xUnit's default parallel test execution causes race conditions. Fix: disable parallelism at assembly level. Future fix: make GameState an instance that tests can create fresh.

3. **`Control._Draw()` is Godot's canvas API.** The HP/MP orbs use `DrawCircle()`, `DrawLine()`, `DrawArc()`, `DrawString()` in `_Draw()`, with `QueueRedraw()` to trigger repaints. This is how you do custom rendering on UI elements — similar to HTML Canvas but integrated into Godot's Control tree.

4. **StyleBoxFlat is Godot's CSS equivalent.** `panel.AddThemeStyleboxOverride("panel", styleBox)` is like setting inline CSS. Properties map cleanly: `BgColor` = `background-color`, `BorderColor` = `border-color`, `SetBorderWidthAll()` = `border-width`, `SetCornerRadiusAll()` = `border-radius`.

5. **TextureRect puts sprites in UI space.** Game sprites are Sprite2D (world space), but for UI panels you use TextureRect (Control space). Both load textures the same way, but TextureRect works inside Panel/VBoxContainer layouts.

6. **Tween is the animation swiss army knife.** Every visual effect uses `CreateTween()`:
   - `TweenProperty(node, "modulate:a", 0.0, 0.3)` — fade out
   - `TweenProperty(node, "position:y", target, 0.8)` — drift up
   - `TweenProperty(node, "scale", Vector2.One * 3, 0.25)` — expand
   - `Parallel()` chains run simultaneously; sequential chains run in order
   - `TweenCallback(Callable.From(node.QueueFree))` — cleanup after animation

7. **CanvasLayer isolates UI from camera.** UI on CanvasLayer uses viewport pixel coordinates, unaffected by Camera2D zoom/position. Game entities use world coordinates. This separation is essential — the HUD stays fixed while the game world scrolls.

8. **Headless mode is a CI goldmine.** By making the demo run at 10ms delays in headless mode, the full 36-step demo completes in ~2 seconds. Console output + grep assertions = fast E2E testing without any visual rendering. This pattern scales to any automated game test.

9. **`<Compile Include>` source linking lets tests share code without project references.** The test project includes GameCore.cs as a source link, compiling it fresh against net10.0. No project reference to the Godot SDK needed. This avoids the "can't reference a Godot project from a plain .NET test project" problem entirely.

10. **DCSS sprites load with `ResourceLoader.Exists()` guard.** Always check before `GD.Load<Texture2D>()` to avoid crashes on missing assets. Fallback to programmatic `ImageTexture` keeps the demo running regardless of asset state.

### Decisions Made

| Decision | Rationale |
|----------|-----------|
| xUnit over GdUnit4 for unit tests | GameCore.cs has no Godot dependency — plain xUnit is simpler and faster |
| Source link `<Compile Include>` over project reference | Avoids Godot SDK dependency in test project |
| Disable test parallelism | Static GameState requires sequential execution |
| StyleBoxFlat matching HUD spec colors | Consistent dark fantasy theme across all UI |
| TextureRect for in-window sprites | Proper Control-space rendering for shop/dialog icons |
| Tween-based visual effects | Godot's built-in, no external animation library needed |

---

## Session 2c — 2026-04-08 (performance audit)

### What Happened

**Started from:** Fully working automated demo with DCSS sprites, HP/MP orbs, styled windows, visual effects, 51 unit tests, 40 E2E assertions.

**Ended with:** Performance audit completed — 6 code fixes applied, 4 production-scale patterns documented, all tests still passing, and a clear understanding of what matters for perf now vs. later.

### The Audit

Reviewed all 3 code files line-by-line (~1,500 lines total) looking for: per-frame allocations, unnecessary recomputation, draw call count, Godot-specific anti-patterns, and scalability bottlenecks.

### Fixes Applied

#### 1. Dirty-Flag Stat Cache (GameCore.cs)

**Before:** `TotalDamage` and `TotalDefense` were computed properties that iterated the equipment dictionary every time they were read. In a real-time combat loop checking damage every physics frame at 60fps, that's 60 dictionary iterations per second for a value that only changes when equipment changes.

**After:** Added `_cachedDamage`, `_cachedDefense`, and `_statsDirty` flag. `InvalidateStats()` is called from `EquipItem()`, `UnequipItem()`, and `LevelUp()`. The cached values are recalculated only when the dirty flag is set.

**Pattern learned:** Dirty-flag caching. Compute once, read many. Invalidate on mutation. This is the standard game dev pattern for derived stats — RPG games compute effective stats on equipment change, not every frame. Same pattern will apply to: effective attack speed, total magic resistance, movement speed modifiers, etc.

**Side effect caught:** The unit test `PlayerDamage_ScalesWithLevel` broke because it set `Level = 10` directly without calling `InvalidateStats()`. This proves the cache works — and teaches us that any code that directly mutates stats must invalidate. In production, this argues for making stats private-set with methods that auto-invalidate.

#### 2. Integer XP Formula (GameCore.cs)

**Before:** `XPToNextLevel => (int)Math.Floor(Level * Level * 45.0)` — multiplied integers, promoted to double, floored back to int. Three type conversions for a result that's always an integer.

**After:** `XPToNextLevel => Level * Level * 45` — pure integer math. Same result, zero floating-point overhead.

**Pattern learned:** Avoid unnecessary float/double promotion. If the formula is purely integer (L^2 * 45 is always a whole number when L is integer), keep it integer. `Math.Floor()` on an integer-derived double is a no-op that costs CPU time.

#### 3. Culture-Safe String Comparison (GameCore.cs)

**Before:** `stat.ToUpper()` in `AllocateStatPoint()` — culture-sensitive uppercase. In Turkish locale, `"i".ToUpper()` returns `"İ"` (dotted I), not `"I"`.

**After:** `stat.ToUpperInvariant()` — culture-invariant, predictable behavior regardless of system locale.

**Pattern learned:** Always use `ToUpperInvariant()` or `StringComparison.OrdinalIgnoreCase` for programmatic string comparison. `ToUpper()` is for display, not logic. This applies to: item name matching, command parsing, save file keys, config values.

#### 4. HpMpOrbs Skip-Redraw + String Cache (HpMpOrbs.cs)

**Before:** `UpdateValues()` always called `QueueRedraw()` even when HP/MP values hadn't changed. `_Draw()` formatted `$"{_hp}/{_maxHp}"` strings every call — two string allocations per draw. These strings were identical between frames when values didn't change.

**After:** Early return in `UpdateValues()` if all 4 values match previous. Pre-format `_hpText`/`_mpText` strings in `UpdateValues()` and reuse them in `_Draw()`. Zero allocations in `_Draw()`.

**Pattern learned:** `_Draw()` should be allocation-free. Pre-compute everything that doesn't change between draws. Godot's `QueueRedraw()` is cheap to call but `_Draw()` can be expensive — so gate the queue call, not just the draw.

**Draw call count context:** The orb fill loop does ~100 `DrawLine()` calls per orb (200 total) per `_Draw()`. This is acceptable because `_Draw()` now only fires when HP/MP actually changes (a few times per combat encounter, not 60fps). If we ever need animated orbs (sloshing liquid, glow pulse), we'd switch to a shader.

#### 5. Log List Shift (GameDemo.cs)

**Before:** `while (_logLines.Count > 14) _logLines.RemoveAt(0)` — each `RemoveAt(0)` shifts all remaining elements left. If the log overflows by 3, that's 3 separate array shifts.

**After:** `_logLines.RemoveRange(0, _logLines.Count - 14)` — single shift operation, removes all excess at once.

**Pattern learned:** `List.RemoveAt(0)` is O(n). For queue-like behavior (add to end, remove from front), either use `RemoveRange()` for batch removal, or use a `Queue<T>` / circular buffer. For our 14-line log this is trivial, but in production with a combat log receiving multiple events per frame, it matters.

#### 6. Zero-Allocation Icon Cleanup (GameDemo.cs)

**Before:** `ClearWindowIcons()` allocated a `new List<Node>()`, iterated children to collect TextureRects, then iterated the list to free them. Two passes, one allocation.

**After:** Single backwards loop with `GetChild(i)`. No collection allocated. One pass.

**Pattern learned:** When removing children during iteration, iterate backwards (high index to low). Each `QueueFree` doesn't shift indices of earlier children. This pattern applies everywhere you clean up child nodes: clearing enemy groups, resetting UI, despawning effects.

### Production-Scale Patterns (Not Fixed — Documented)

These are fine at demo scale but will need attention when the real game runs:

#### Individual Sprite2D Tiles → TileMapLayer

**Current:** `DrawFloor()` creates 117 Sprite2D nodes. `DrawWalls()` creates ~48 more. That's 165 individual draw calls for a 15x11 room.

**Production fix:** `TileMapLayer` with a `TileSet` resource. All tiles rendered in a single batched draw call. Godot's tile engine handles culling, batching, and Y-sorting natively. This is already specced in `scene-tree.md` — the demo just skipped it for simplicity.

**When to migrate:** When implementing real dungeon generation (P1-05). The BSP+Drunkard's Walk algorithm will paint tiles onto a TileMapLayer, not create Sprite2D nodes.

#### Node Creation for Effects → Object Pool

**Current:** Every `ShowFloatingText()` creates a `new Label()`, tweens it, then `QueueFree()`. Every `ShowSlashEffect()` creates a `new Polygon2D()` and frees it. In a real combat scenario with 14 enemies, attack rate 2.38/sec, that's ~33 node create/destroy cycles per second just for slash effects.

**Production fix:** Pre-create a pool of Label and Polygon2D nodes (e.g., 20 each). On use, activate from pool + reset properties. On tween complete, deactivate back to pool instead of freeing. Zero allocation during gameplay.

**When to implement:** When real-time combat is running with multiple enemies (P1 combat implementation).

#### CreateColorTexture → Texture Cache

**Current:** `CreateColorTexture()` creates a new `Image` + `ImageTexture` per call. Each is a GPU upload.

**Production fix:** `Dictionary<(Color, int), ImageTexture>` cache. Return cached texture if the same color/size was already created.

**When to implement:** When the game creates colored textures dynamically at runtime (status effect indicators, minimap dots, UI highlights).

#### HpMpOrbs Line-Sweep → Shader

**Current:** The orb fill effect draws ~100 horizontal lines per orb via CPU `DrawLine()` calls. Visually correct but CPU-heavy per draw.

**Production fix:** A fragment shader that takes fill percentage as a uniform. The GPU does the circle math per-pixel in parallel — one draw call per orb regardless of fill level. Shader code:
```glsl
// Pseudocode — circle fill shader
uniform float fill_percent : hint_range(0.0, 1.0);
uniform vec4 fill_color;
uniform vec4 empty_color;

void fragment() {
    vec2 uv = UV * 2.0 - 1.0; // -1 to 1
    float dist = length(uv);
    if (dist > 1.0) discard; // outside circle
    float y_normalized = (uv.y + 1.0) / 2.0; // 0 = top, 1 = bottom
    COLOR = y_normalized >= (1.0 - fill_percent) ? fill_color : empty_color;
}
```

**When to implement:** When the HUD is being built for real (P1 HUD implementation). The CPU line-sweep is fine for the demo since _Draw() only fires on value change.

### What We Learned (Perf Edition)

1. **Cache derived values, invalidate on mutation.** The dirty-flag pattern (`_statsDirty`) is the #1 perf pattern in game dev. RPGs compute effective stats once per equipment change, not once per frame. Every system with derived state should use this: effective stats, damage ranges, movement speed, spell costs.

2. **`_Draw()` should be allocation-free.** Pre-compute strings, cache viewport sizes, avoid `new` inside draw methods. `_Draw()` may be called multiple times per frame if parent nodes trigger redraws.

3. **Integer math over float math when the result is always integer.** `L * L * 45` > `Math.Floor(L * L * 45.0)`. No type promotion, no floor call, same result. Apply this to: XP formulas, damage formulas where inputs are all int, floor/tier calculations.

4. **`RemoveAt(0)` on List is O(n). Use RemoveRange or Queue.** Lists are backed by arrays. Removing from the front shifts everything. For FIFO behavior, Queue<T> is O(1) dequeue. For batch removal, RemoveRange is one shift.

5. **Iterate backwards when removing children.** Forward iteration skips nodes when indices shift after removal. Backward iteration is safe because removing index 5 doesn't affect indices 0-4.

6. **165 Sprite2D nodes for tiles is fine for a demo, not for production.** Godot's TileMapLayer exists specifically to batch tilemap rendering. When we build real dungeons, use TileMapLayer from the start.

7. **Object pools prevent GC pressure in hot loops.** Node creation involves: memory allocation, constructor, scene tree insertion, and potentially GPU resource creation. In a 60fps combat loop, pooling eliminates all of this. Godot nodes can be "pooled" by toggling `Visible` and `ProcessMode` rather than creating/freeing.

8. **The biggest perf win is not doing work at all.** The HpMpOrbs skip-redraw check (`if values unchanged, return`) is more impactful than optimizing the draw itself. Before optimizing HOW something runs, ask IF it needs to run.

### Test Results After All Changes

| Suite | Result |
|-------|--------|
| Unit tests (51) | All passing |
| E2E assertions (40) | All passing |

One test (`PlayerDamage_ScalesWithLevel`) broke during the audit because it mutated `Level` directly without calling `InvalidateStats()`. This was a **test bug, not a system bug** — the cache correctly prevented stale reads. Fixed by adding `InvalidateStats()` to the test. This validates that the dirty-flag pattern enforces correct usage.

---

## Session 2d — 2026-04-08

### Architecture Audit: Current Code vs Planned Specs

Compared the current demo codebase against the 6 architecture spec docs (`project-structure.md`, `scene-tree.md`, `autoloads.md`, `signals.md`, `ai-workflow.md`, `tech-stack.md`) and the conventions docs (`teams.md`). The goal: identify what the demo code does differently from the planned production architecture, so we know exactly what to change when real implementation begins.

### Gap Analysis

#### 1. GameState: Static Class vs Autoload Node

| Aspect | Planned (autoloads.md) | Current (GameCore.cs) |
|--------|----------------------|----------------------|
| Type | Godot Node, registered as autoload | Static C# class, no Godot dependency |
| Access | `GetNode<GameState>("/root/GameState")` | `GameState.Player.HP` (direct static) |
| Reactivity | Property setters emit signals (`StatsChanged`, `PlayerDied`) | No signals — callers must manually update UI |
| Persistence | Survives scene transitions (autoload) | Survives because it's static (same effect, different mechanism) |

**Why it matters:** The signal-based autoload pattern enables "change HP → HUD auto-updates" without any coupling. The static pattern requires every consumer to poll or be explicitly notified. For the demo this is fine — for production, the autoload pattern is required.

**Migration path:** Move `PlayerState` fields into a `GameState : Node` class with custom property setters that call `EmitSignal()`. Register in `project.godot` as autoload.

#### 2. EventBus: Missing Entirely

| Aspect | Planned (autoloads.md) | Current |
|--------|----------------------|---------|
| Exists? | Yes — `scripts/autoloads/EventBus.cs` | No |
| Signals | `EnemyDefeated`, `EnemySpawned`, `PlayerAttacked`, `PlayerDamaged` | N/A |
| Pattern | "Call down, signal up" — decoupled gameplay events | Direct method calls between static classes |

**Why it matters:** EventBus decouples systems. When an enemy dies, the dungeon schedules a respawn AND the HUD shows XP — neither system knows about the other. Without EventBus, every interaction is a hardcoded call chain.

**Migration path:** Create `EventBus : Node` with 4 signal declarations. Register as autoload. Replace direct calls with signal emissions.

#### 3. Scene Organization: Monolith vs 6 Scenes

| Aspect | Planned (scene-tree.md) | Current |
|--------|------------------------|---------|
| Scenes | 6: main, dungeon, player, enemy, hud, death_screen | 1: game_demo.tscn |
| Node types | CharacterBody2D (player/enemy), TileMapLayer, Area2D | Sprite2D only (no physics) |
| Instancing | Scenes loaded and instantiated at runtime | Everything built in code in `_Ready()` |

**Why it matters:** Scene separation enables reuse (enemy.tscn instantiated 14 times), editor configuration (tweak in Inspector, not code), and team ownership (UI lead owns `scenes/ui/`, engine lead owns `scenes/dungeon/`).

**Migration path:** Extract each entity into its own `.tscn` + `.cs` pair. GameDemo was never meant to be production architecture — it's a test harness.

#### 4. File Structure: Flat vs Categorized

| Aspect | Planned (project-structure.md) | Current |
|--------|-------------------------------|---------|
| Scripts | `scripts/autoloads/`, `scripts/ui/`, `scripts/{entity}/` | `scripts/game/`, `scripts/` (flat) |
| Scenes | `scenes/ui/`, `scenes/dungeon/`, `scenes/player/` | `scenes/` (flat, 3 files) |
| Autoloads | `scripts/autoloads/GameState.cs`, `scripts/autoloads/EventBus.cs` | Does not exist |

**Current files vs planned layout:**

```
Current:                          Planned:
scripts/                          scripts/
  GameDemo.cs (1022 lines)          autoloads/GameState.cs
  HelloWorld.cs                     autoloads/EventBus.cs
  AssetTest.cs                      Main.cs
  game/GameCore.cs (511 lines)      Player.cs
  game/HpMpOrbs.cs                  Enemy.cs
                                    Dungeon.cs
scenes/                             ui/Hud.cs
  game_demo.tscn                    ui/DeathScreen.cs
  hello_world.tscn                  ui/HpMpOrbs.cs
  asset_test.tscn
                                  scenes/
                                    main.tscn
                                    dungeon.tscn
                                    player.tscn
                                    enemy.tscn
                                    ui/hud.tscn
                                    ui/death_screen.tscn
```

**Migration path:** When real implementation starts, create the planned folder structure. Demo files stay as-is (they're test harnesses, not production code).

#### 5. Script Size: Over Limits

| File | Lines | Planned Limit | Status |
|------|-------|--------------|--------|
| GameDemo.cs | 1,022 | 300 | 3.4x over — acceptable for demo harness |
| GameCore.cs | 511 | 300 | 1.7x over — will split into separate autoload + systems |
| HpMpOrbs.cs | 122 | 300 | Under limit |

**Why it's OK now:** GameDemo is a test script, not production code. GameCore intentionally bundles everything for testability without Godot. When migrated to autoloads, the natural split (GameState node + GameSystems utility + data models) will bring each under 300.

#### 6. Signals: Zero of 9 Implemented

The spec defines 9 signal connections. Current code uses 0 signals — all communication is synchronous method calls.

| Signal | Planned Source | Current Equivalent |
|--------|---------------|-------------------|
| `StatsChanged` | GameState property setters | Manual `_hpMpOrbs.UpdateValues()` call |
| `PlayerDied` | GameState.Hp setter | `if (player.IsDead)` check after attack |
| `EnemyDefeated` | Enemy.TakeDamage | `if (monster.IsDead)` check after attack |
| `EnemySpawned` | Dungeon.SpawnEnemy | Direct `SpawnEntity()` call |
| `PlayerAttacked` | Player.HandleAttack | Direct `AttackMonster()` call |
| `Timeout` (spawn) | SpawnTimer | Demo uses scripted timing |
| `Timeout` (cooldown) | HitCooldownTimer | No cooldowns in demo |
| `BodyEntered` (hit) | HitArea Area2D | No physics collision |
| `Pressed` (restart) | RestartButton | Demo scripts respawn directly |

**Migration path:** Each signal gets implemented when its owning system is built. The signal registry in `signals.md` is the implementation checklist.

#### 7. Physics / Collision: Not Implemented

| Aspect | Planned | Current |
|--------|---------|---------|
| Player body | CharacterBody2D, layer=2, mask=1 | Sprite2D, no physics |
| Enemy body | CharacterBody2D, layer=4, mask=1 | Sprite2D, no physics |
| Attack range | Area2D, radius=78px, mask=4 | Proximity check via static method |
| Hit detection | Area2D.BodyEntered signal | Direct method call |
| Movement | `MoveAndSlide()` + `Input.GetVector()` | `Position +=` in scripted steps |

**Why it's OK now:** The demo validates game mechanics (damage formulas, inventory, leveling), not physics. Physics implementation is a separate ticket scope.

#### 8. Naming Conventions: Mostly Compliant

| Convention | Spec | Current | Status |
|-----------|------|---------|--------|
| C# files | PascalCase.cs | GameCore.cs, HpMpOrbs.cs | PASS |
| Private fields | _camelCase | `_hp`, `_cachedDamage`, `_stepTimer` | PASS |
| Public methods | PascalCase | `AttackMonster()`, `EnterDungeon()` | PASS |
| Constants | PascalCase | `OrbRadius`, `ArcSegments` | PASS |
| Subfolder | PascalCase | `scripts/game/` (lowercase) | MINOR — spec unclear on folder case |
| Scene files | PascalCase.tscn | `game_demo.tscn` (snake_case) | DEVIATE — demo convention, not production |

#### 9. What the Demo Got Right (Production-Ready Patterns)

These patterns from the demo are directly usable in production:

1. **Dirty-flag stat caching** — `InvalidateStats()` pattern matches production needs exactly
2. **Integer-only formulas** — XP curve `Level² × 45`, damage `12 + floor(Level * 1.5)` avoid float issues
3. **Defense diminishing returns** — `DR = def * (100 / (def + 100))` is the spec formula
4. **Equipment slot system** — Dictionary<EquipSlot, ItemData> with swap logic
5. **Stackable consumables** — Quantity tracking, depletion, capacity checks
6. **Floor difficulty scaling** — `baseHP * (1 + (floor-1) * 0.5)` matches spec
7. **HP/MP orb rendering** — Custom `_Draw()` with fill-from-bottom, will port to production HUD
8. **Tween-based effects** — Floating text, flash, slash — reusable visual feedback patterns
9. **CanvasLayer UI isolation** — Correct pattern for keeping UI fixed while camera moves
10. **Headless mode detection** — `DisplayServer.GetName() == "headless"` for CI/testing

### Summary: Architecture Readiness Score

| Category | Score | Notes |
|----------|-------|-------|
| Game mechanics | 10/10 | All formulas match specs, thoroughly tested |
| Data models | 9/10 | Complete; needs Godot signal integration |
| Visual patterns | 8/10 | Orbs, effects, styled windows all production-quality |
| File structure | 3/10 | Flat, needs full reorganization per spec |
| State management | 3/10 | Static instead of reactive autoloads |
| Signal architecture | 0/10 | Zero signals implemented |
| Physics/collision | 0/10 | No physics (expected — separate scope) |
| Scene organization | 2/10 | Single monolith scene |

**Overall:** The demo code is a successful **mechanics validation layer**. Every game system works correctly and is well-tested. The architecture gaps are all expected — the demo was designed to test "does the math work?" not "is the node tree correct?" When production implementation begins, the migration path from demo → production is clear for every gap.

### What This Teaches Us

1. **Demo code ≠ production architecture.** The demo validates mechanics in isolation. Production code needs reactive state, signal wiring, scene separation, and physics. These are different concerns, intentionally tested separately.

2. **Static C# is great for unit testing.** The zero-Godot-dependency GameCore.cs pattern lets us run 51 xUnit tests without a game engine. When we split into autoloads, we should keep a pure-logic layer underneath for testability.

3. **The specs are the migration checklist.** Every gap identified above maps to a specific spec doc section. `autoloads.md` = GameState + EventBus migration. `scene-tree.md` = scene extraction. `signals.md` = wiring checklist. No guesswork needed.

4. **Naming conventions need enforcement from day one.** The demo's `game_demo.tscn` and `scripts/game/` folder wouldn't pass spec review. Production tickets should enforce naming in the PR checklist.

5. **The 300-line limit will happen naturally.** GameCore.cs (511 lines) contains GameState + GameSystems + 5 data models in one file. When split into autoloads and separate model files, each will be well under 300.

---

## Session 2e — 2026-04-08

### What Happened

Extended the learning demo with UI systems, performance testing, and reorganized all project documentation.

### What We Built

#### 8 New UI Systems (Phase 6: UI Showcase)

| UI Element | Godot Pattern Learned | Control Nodes Used |
|------------|----------------------|-------------------|
| XP Progress Bar | ProgressBar theming with StyleBoxFlat | ProgressBar, Label |
| Toast Notifications | Animated slide-in queue, auto-dismiss | VBoxContainer, PanelContainer, Label |
| Shortcut Bar | Fixed-size slot grid with icons | HBoxContainer, Panel, TextureRect, Label |
| Inventory Grid | GridContainer with dynamic slot population | GridContainer, Panel, TextureRect, Label |
| Equipment Panel | Positioned slot layout with labels | Panel, Label, TextureRect |
| Settings Panel | Form controls (sliders, toggles, dropdowns) | HSlider, CheckButton, OptionButton, Label |
| Tooltip | Contextual popup with auto-wrap | Panel, Label |
| Death Screen Overlay | Full-screen modal with centered content | ColorRect, CenterContainer, VBoxContainer, Button |

#### Performance Testing (Phase 7)

Built a Lighthouse-equivalent for Godot:

| Component | Web Equivalent | Godot API |
|-----------|---------------|-----------|
| Live perf overlay | Chrome DevTools | `Performance.GetMonitor()` |
| Operation timing | `performance.now()` | `Time.GetTicksUsec()` |
| Scorecard | Lighthouse score | Custom 0-100 scoring per metric |

**Benchmarks added:** Combat calculations (1000x), stat recalculation (1000x), inventory operations (500x), XP/leveling (1000x), sprite spawn/remove (50x), UI panel creation (20x).

**Scorecard metrics:** FPS, frame time, memory, node count — each scored 0-100, averaged for overall.

#### Docs Reorganization

| Action | Details |
|--------|---------|
| Moved 3 files | `ai-workflow.md` → `conventions/`, `godot-basics.md` + `game-dev-concepts.md` → `reference/` |
| Deprecated | `best-practices.md` → redirect to `conventions/` |
| Created | `conventions/code.md` (252 lines), `conventions/agile.md` (216 lines), `reference/game-development.md`, `docs/README.md` (master index) |
| Updated refs | AGENTS.md, CLAUDE.md, CHANGELOG.md, project-structure.md |

**New docs structure:**
- `conventions/` — 4 files: code.md, agile.md, ai-workflow.md, teams.md
- `reference/` — 4 files: godot-basics.md, game-dev-concepts.md, game-development.md, subagent-research.md
- `architecture/` — 7 files (clean, architecture-only)

### Test Results

| Suite | Count | Result |
|-------|-------|--------|
| Unit tests (xUnit) | 51 | All passing |
| E2E assertions | 59 | All passing (was 40, added 19 for phases 6-7) |
| Perf scorecard | 95/100 | Headless mode |

### What We Learned

1. **`Performance.GetMonitor()` is the game dev Performance Observer.** Returns FPS, frame time, memory, node count, draw calls — all read-only, zero overhead. Use it like you'd use Chrome DevTools Performance tab.

2. **`Time.GetTicksUsec()` returns `ulong`, not `long`.** Arithmetic with signed types causes CS0034 ambiguity errors. Always use `ulong` for timing variables.

3. **ProgressBar theming uses `"background"` and `"fill"` style overrides.** Not obvious from docs — discovered by experimentation.

4. **HSlider, CheckButton, OptionButton are Godot's form controls.** Direct equivalents of HTML `<input type="range">`, `<input type="checkbox">`, and `<select>`. All work in code without scenes.

5. **CenterContainer needs explicit Size when inside CanvasLayer.** Anchors don't auto-expand in CanvasLayer children — set Size manually to viewport dimensions.

6. **PanelContainer with ContentMargin is cleaner than Panel + manual Label positioning.** The margin properties handle padding automatically.

7. **Toast notification pattern: VBoxContainer + tween + QueueFree callback.** Add child, animate in, TweenInterval for hold time, animate out, callback to free. Clean and reusable.

8. **Docs reorganization pays off immediately.** Moving bridge docs to `reference/` and process docs to `conventions/` makes the architecture/ folder purely technical. AI sessions can find things faster.

9. **3rd-party tool policy established.** User is open to free, industry-standard tools that improve development without affecting runtime performance. BenchmarkDotNet identified for future formal benchmarking.

10. **55 demo steps across 7 phases now.** The demo has grown from 36 → 46 → 55 steps. Each phase teaches a different category: mechanics, UI patterns, performance measurement.

---

## Session 5 — Asset Pipeline, Grid System, Entity Framework (2026-04-09)

### What We Built

**Isometric Stone Soup (ISS) adoption — grid standard locked:**
- Adopted ISS by Screaming Brain Studios as the game's tile grid standard: 64x32 floors, 64x64 wall blocks
- Sorted ISS into `assets/isometric/tiles/stone-soup/` (49 floor themes, 43 wall block themes, 3 torches, 86 Tiled .tsx files)
- Added `TestHelper.LoadIssPng()` — strips magenta (#FF00FF) transparency key from ISS sprites
- Added `TestHelper.CreateFloorGrid()` — reusable ISS floor backdrop for any test scene

**14 SBS asset packs sorted (819+ game assets):**
- Downloaded and integrated: crates, doorways, walls, roads, pathways, floor tiles, autotiles, water, wall textures, town buildings, buttons, objects, tile toolkit, grid pack
- All packs from Screaming Brain Studios (CC0) — sole source for all isometric environment/UI art
- Renamed files: lowercase, underscores, stripped prefixes/suffixes
- Large variants (2x) archived to `source/large-variants/` (gitignored)

**Old asset replacement:**
- Replaced all Dragosha objects (doors, crates, chests) + UI (buttons, arrows, icons) with SBS equivalents
- Replaced rubberduck ground tiles with ISS floors
- Kept: Clint Bellanger characters/creatures, Dragosha NPCs (characters — no SBS equivalent)
- SBS crates now serve as the game's loot/prize containers (style consistency over variety)
- Moved 32 old assets to `source/legacy/` for reference

**Unified TestEntity.cs:**
- Replaced separate TestCreature.cs + TestHero.cs with one unified viewer
- Same animation/movement/display code for ALL entities
- Creatures: 8x8 sheets, 5 animations (Stance, Walk[1-3], Attack[4-5], Hit, Dead)
- Hero: 32x8 sheets, 7 animations, equipment layers
- Fixed walk animation bug: was frames 1-4 (included attack), now frames 1-3

**Entity Mechanics Framework (scripts/game/):**
- Designed and built a unified entity system: 11 files, 6 systems
- `EntityData` — single data model for ALL entities (player, enemy, NPC)
- `EntityFactory` — creates pre-configured entities with correct defaults
- `VitalSystem` — HP/MP management, death, revive, regeneration
- `StatSystem` — STR/DEX/INT/VIT with diminishing returns, derived stats
- `CombatSystem` — unified damage calc (same function for player→enemy AND enemy→player)
- `EffectSystem` — status effects (poison, regen, buffs), tick-based processing
- `ProgressionSystem` — XP, leveling (L² × 45 curve), stat/skill points
- `SkillSystem` — skill execution, cooldowns, mana costs
- All systems are static classes, pure C# (no Godot deps), fully xUnit testable

**24 visual test commands + 5 category runners:**
- `make test-creatures` — unified browser (Up/Down to switch)
- `make test-floors/walls/doors/crates/roads/water/objects/town/items` — environment
- `make test-buttons/ui` — UI elements
- `make test-entity` — unified entity viewer with ISS grid scale reference
- `make test-visual` — launches everything
- Individual creature tests (`test-slime`, etc.) start browser on that creature

**Documentation:**
- `docs/architecture/entity-framework.md` — full framework spec
- Updated: CREDITS.md (15 new SBS entries), tile-specs.md, sprite-specs.md, project-structure.md, AGENTS.md, dev-tracker.md, README.md

### Key Decisions

| Decision | Rationale |
|----------|-----------|
| SBS = sole source for all isometric tiles/textures | Style consistency > variety. One visual language. |
| ISS defines the grid (64x32 floors, 64x64 walls) | Everything conforms to ISS dimensions. |
| SBS crates replace chests | No SBS chest equivalent; crates fill the container role. |
| No Blender pipeline | User wants ready-to-use PNGs only. No time for 3D workflows. |
| Unified entity system | All entities share same mechanics. Only assets + hitboxes differ. |
| CC-BY-SA is acceptable | Resizing sprites for grid fit is technical integration, not modification. |
| Entity framework alongside GameCore.cs | New system lives in parallel. Old code kept for existing test compat. |

### Test Counts

| Type | Count | Status |
|------|-------|--------|
| Visual test scenes | 24 | All build-verified |
| Category runners | 5 | Working |
| Entity framework systems | 6 | Built, tests in progress |

### What We Learned

1. **Magenta (#FF00FF) is the ISS transparency key.** Every ISS sprite uses magenta backgrounds instead of alpha. `LoadIssPng()` strips it by scanning pixels — slow but fine for test scenes. For production, pre-process to alpha at build time.

2. **ISS wall blocks are 64x64, not 64x32.** Walls are isometric cubes (base + vertical face). Floors are flat diamonds. Two separate TileMapLayers needed: FloorLayer (64x32) and WallLayer (64x64).

3. **FLARE creature sprites use 8x8 grids (8 directions × 8 frames).** Walk = frames 1-3, NOT 1-4. Frame 4 is attack start. This caused a visible bug where walk animation included a sword swing.

4. **Hero sprites use 32x8 grids (8 directions × 32 frames).** Much more animation variety: stance×4, run×8, melee×4, block×2, hit+die×6, cast×4, shoot×4.

5. **SBS "small" floor/road packs are 128x64 (2x our grid), not 64x32.** These are overlay/decoration assets, not base tiles. Only ISS Stone Soup floors match the 64x32 base grid exactly.

6. **SBS doorway sprites are sprite strips (384x96 = 6 frames at 64x96).** Each strip shows 6 arch shape variants, not animation frames. Materials: stone, brick, wood. Directions: SE, SW.

7. **Static systems with EntityData-first parameters = clean, testable architecture.** No Godot dependencies means xUnit tests run instantly. Same `DealDamage(attacker, target)` call works for any entity type — symmetry proven by tests.

8. **The diminishing returns formula `raw * (100 / (raw + 100))` is elegant.** At raw=100, effective=50. At raw=1000, effective=90.9. Asymptotically approaches 100 but never reaches it. Prevents stat inflation.

9. **Parallel agent workflows dramatically speed up large tasks.** 3 agents built the entire entity framework (11 files, 6 systems) simultaneously. File sorting + code updates + doc updates also parallelized effectively.

10. **Asset consistency matters more than asset variety.** The user's strong preference for one art source (SBS) over mixing styles from different artists is a core design principle. It extends to everything: tiles, objects, UI, future assets.

---

## Session 6 — Proc Gen Overhaul, Automap, Town Foundation (2026-04-09)

### What We Built

**Progressive floor sizing:**
- Replaced fixed 100x200 floor grid with zone-stepped formula mirroring difficulty scaling
- Zone 1 (floors 1-10): starts at 50x100, Zone 10 (floors 91-100): caps at 150x300
- Formula: `zone_scale = 1.0 + (zone-1) * 0.25`, `intra_scale = 1.0 + step * 0.02`, `size = base * zone_scale * intra_scale`
- BSP naturally produces fewer rooms on smaller grids — zone 1 gets 3-4 rooms, deep zones get 8+
- `DungeonGenerator.CalculateFloorSize(floorNumber)` is a public static method for any system to query

**IKEA guided layout:**
- Replaced random BSP sibling corridors with nearest-neighbor chain pathing
- Rooms are ordered: Entrance → Room A → Room B → ... → Exit
- Corridors carved along chain order, creating a guided flow through the floor
- Player CAN backtrack, but natural flow pushes forward (like IKEA showroom)
- 15% loop chance still applies for optional alternate routes

**Challenge room shortcut:**
- One challenge room per non-boss floor, placed off the main path
- Connected to an early room AND shortcuts to near-exit room
- Grid-scan placement algorithm finds valid non-overlapping positions
- `RoomKind.Challenge` added to enum
- Scales room size with floor size: `clamp(width/5, 8, 16)`

**Boss blocks exit:**
- On every 10th floor, the exit room becomes the boss room
- `RoomKind.Boss` replaces `RoomKind.Exit` on boss floors
- Must defeat boss to descend — no separate boss room needed

**Isometric wall rendering in dungeon test:**
- Added wall block rendering (64x64 ISS cubes) to TestDungeonGen
- Only renders "edge walls" (walls adjacent to at least one floor tile)
- Single TileMapLayer with two atlas sources (floors 64x32 + walls 64x64) for correct isometric depth sorting
- Uses brick_gray.png wall theme, row 0 (full blocks only, not overlays)

**Exploration tracking (fog of war foundation):**
- Added `bool[,] Explored` to FloorData, initialized to all false
- `MarkExplored(x, y, radius)` marks circular area using distance check
- `IsExplored(x, y)` queries explored state
- Persists while floor is cached (10-floor LRU); purged floors reset exploration

**Automap overlay system (in progress — parallel agent):**
- D1-style wireframe overlay using Control._Draw() pattern
- 3 modes via M key: Overlay → Full Map → Off
- Color-coded: dim gold walls, bright yellow stairs, orange player, red/gold/orange room outlines
- Per-tile fog of war (only explored tiles drawn on map)

**Town scene + NPC foundation (in progress — parallel agent):**
- Hand-designed ~30x30 isometric town layout with ISS tiles
- 5 NPCs (Item Shop, Blacksmith, Guild, Teleporter, Banker) at fixed positions
- Walk-up proximity detection (32px radius → panel appears, walk away → dismisses)
- Item Shop UI as first functional NPC (uses existing GameCore.BuyItem/SellItem)

**Input Map setup (in progress — parallel agent):**
- All actions from controls.md wired in project.godot
- New `map_toggle` action on M key
- Arrow keys, WASD face buttons, Q/E shoulders, Esc start

**Control scheme change:**
- M key = Map cycle (overlay → full → off) — dedicated key outside PS1 baseline
- Start (Esc) = Game window with all tabs/panels (absorbs old Select function)
- △ (W) reverts to fully assignable face button

### Key Decisions

| Decision | Rationale |
|----------|-----------|
| Progressive sizing over fixed grid | Early floors feel compact and tutorial-like; deep floors feel sprawling |
| Zone-stepped + intra ramp (not smooth) | Mirrors difficulty scaling exactly — same jumps, same feel |
| 1:2 width:height ratio (50x100 base) | Matches isometric projection; taller grids work better in diamond layout |
| IKEA chain pathing over random BSP | Guided flow ensures player sees all rooms; prevents confusing dead ends |
| Challenge room always present (not RNG) | Player choice is fight-or-skip, not find-or-miss |
| Boss = exit room on 10th floors | Simpler, more dramatic — boss literally blocks your path |
| D1 wireframe style over D2 sprite icons | Fits dark dungeon atmosphere; clean minimal look |
| M key for map (not W/P) | Dedicated key avoids conflicts with face buttons and panel system |
| Town is hand-designed (not proc gen) | Hub needs to feel like a real, consistent place |

### Test Counts

| Type | Count | Status |
|------|-------|--------|
| Unit tests | 267 | All passing |
| New sizing tests | 7 | Floor 1 base, zone growth, intra ramp, zone jump, cap, generated match |
| New challenge room tests | 3 | Appears on most floors, absent on boss, reachable from entrance |
| New boss-blocks-exit test | 1 | Boss room exists, no separate exit on 10th floors |
| Visual test floor cycle | 8 floors | 1/5/10/11/20/30/50/100 |

### Files Changed

| File | Change |
|------|--------|
| `scripts/game/dungeon/DungeonGenerator.cs` | Complete rewrite: progressive sizing, chain pathing, challenge room, boss=exit |
| `scripts/game/dungeon/DungeonData.cs` | Added `RoomKind.Challenge`, `Explored` array, `MarkExplored()`, `IsExplored()` |
| `scripts/game/dungeon/DrunkardWalkCarver.cs` | Added public `CarvePath()` for challenge room corridors |
| `scripts/tests/TestDungeonGen.cs` | Progressive sizing, wall rendering, challenge room color, expanded floor cycle |
| `tests/DungeonGeneratorTests.cs` | 11 new tests for sizing, chain pathing, challenge room, boss-blocks-exit |
| `docs/world/dungeon.md` | Updated spec: progressive sizing formula, IKEA layout, challenge rooms, boss-blocks-exit |

### What We Learned

1. **Grid-scan beats random placement for tight spaces.** Challenge room random offset placement failed 50-75% on small floors. Scanning all valid positions then picking randomly got it to 95%+.

2. **Nearest-neighbor chain produces intuitive room ordering.** The algorithm naturally visits nearby rooms first, creating a winding but logical path. No need for complex graph algorithms.

3. **Diablo 1 automap uses pure DrawLine, not sprites.** D2 switched to pre-rendered tile icons (dc6 files). D1's approach is simpler and fits our aesthetic better.

4. **Diablo 2 "transparency" was checkerboard dithering, not alpha.** Every other pixel deleted in a grid pattern. D2R added true alpha. We'll use true alpha since we have modern rendering.

5. **Diablo 1 floors are exactly 40x40 tiles.** Fixed size, no scaling. Our progressive system (50x100 → 150x300) is an original design choice, not industry standard.

6. **Wall sheets have alternating rows.** Even rows (0, 2, 4) = full 64x64 blocks. Odd rows (1, 3, 5) = top-face overlays. TestWalls.cs iterates `row += 2` to skip overlays.

7. **Single TileMapLayer with multiple atlas sources is better than separate layers for isometric.** Two layers break Y-sort depth ordering. One layer with mixed sources sorts correctly by cell position.

8. **Parallel agents can build independent features simultaneously.** Automap, town, and input map are independent tracks — no file conflicts, all buildable in parallel.

---

## Session 6b — Systems Build-out, Research, QA Audit (2026-04-09)

### What We Built

**Bank storage system:**
- BankData class (50 starting slots, expandable +10 per expansion at 500*N^2 gold)
- BankSystem: deposit/withdraw with stacking, expand, full checks
- 15 tests covering deposit, withdraw, expand, cost scaling, edge cases

**Backpack expansion system:**
- BackpackSystem: expand inventory (+5 slots at 300*N^2 gold)
- Added BackpackExpansions tracking to PlayerState
- 9 tests covering expand, cost scaling, edge cases

**Item generation and loot system:**
- Extended ItemData with ItemLevel, Quality (Normal/Superior/Elite), Prefixes, Suffixes
- AffixData class for prefix/suffix stat bonuses (tier 1-6)
- ItemGenerator: GenerateEquipment, GenerateMaterial, GenerateConsumable, RollLootDrop, GenerateCrateLoot
- Quality distribution scales with floor depth per items.md spec
- Loot drops: Tier1=8%, Tier2=12%, Tier3=18% base + floor*0.1% (cap +5%)
- 51 tests covering generation, distribution, scaling, edge cases

**Creature sprite scaling fix:**
- Calculated proper scale: creatures 0.3125x (128px frames → ~40px), heroes 0.625x (64px frames → ~40px)
- Updated TestEntity.cs and IsometricDemo.cs with documented scale constants
- Added vertical offset so feet sit on tile center, not sprite center

**Controls spec update:**
- M key for map cycle (overlay → full → off), dedicated key outside PS1 baseline
- Start (Esc) absorbs Select's panel function — unified game window
- △ (W) freed as assignable face button
- All stale references cleaned across controls.md

**Input Map wired:**
- 12 actions defined in project.godot: movement, face buttons, shoulders, map_toggle (M), start (Esc)

### Research Completed

**Monster technical data structures:**
- Diablo 1: ~15 fields per monster (HP range, AC, damage, resistances 0/75/immune, IntF for AI, XP)
- Diablo 2 MonStats.txt: 50+ fields per monster, difficulty-specific stats, treasure class system, monster modifiers (Extra Fast, Fire Enchanted, etc.), pack composition
- PoE: Normal/Magic/Rare/Unique hierarchy with stacking affixes, Bloodline/Nemesis mods
- Roguelikes (Angband/NetHack/DCSS): template inheritance, hit dice, behavioral flags
- Common universal fields: ID, HP, Damage, Defense, Speed, XP, Depth, Type

**Monster world-building philosophy:**
- Monster Hunter: biological taxonomy, behavioral tells, turf wars, ecological food chains
- FromSoft: enemy placement as environmental storytelling, faction variants, bosses as fundamentally different encounters (not stat-inflated normals)
- Hollow Knight: zone-exclusive creature families, Infection as state transformation, bosses as zone culminations
- Hades: three-tier system (Normal/Armored/Infernal), biome-exclusive rosters, modifier stacking
- Mutation systems: Pokemon regional forms, Hollow Knight infection, Diablo champion/unique modifiers

**Key synthesis for our game:**
- 5 classification axes: Species Family, Mutation Tier (0-3), Behavior Role, Element, Dungeon Role
- The dungeon as intelligent breeder — creatures manufactured for purpose, not naturally evolved
- Zone-exclusive families + mutation variants = exponential variety from small base roster
- D4-style monster families: packs mix archetypes (swarm + tank + caster) from same family

### QA Audit Results

**Comprehensive code audit across all 29 source files and 17 test files.**

| Severity | Count | Top Issues |
|----------|-------|------------|
| Critical | 5 | Dual system divergence, GetMeleeDamage double-counts BaseDamage, code ≠ spec formulas, effect tick bug, factory HP mismatch |
| Important | 9 | Namespace inconsistency (15 files no namespace), data class style mix (fields vs properties), IsInsideAnyRoom O(n) in hot loop, static Random not thread-safe |
| Minor | 6 | STA vs VIT naming, inconsistent return types, magic numbers, NpcPanel potion values ≠ ItemGenerator |
| Recommendations | 6 | Retire GameCore.cs, add InventorySystem, spec-validating tests, overflow guards, injectable Random |

**#1 priority: Retire GameCore.cs.** Nearly half of all findings trace to the dual-system architecture. Every new feature risks building on the wrong foundation.

**#2 priority: Reconcile code with specs.** STR multiplier (0.5 vs 1.5), VIT bonus (3 vs 5), MaxMP formula — code and locked specs disagree.

### Test Counts

| Metric | Value |
|--------|-------|
| Total tests | 351 |
| New this session | 75 (9 exploration + 15 bank + 9 backpack + 51 item gen + QA findings) |
| All passing | Yes |
| Build errors | 0 |

### Files Created This Session

| File | Purpose |
|------|---------|
| `scripts/game/ui/Automap.cs` | D1-style wireframe map overlay with 3 modes |
| `scripts/game/town/NpcData.cs` | NPC data model + NpcType enum |
| `scripts/game/town/TownLayout.cs` | 30x30 hand-designed town layout |
| `scripts/game/ui/NpcPanel.cs` | NPC interaction panel with shop UI |
| `scripts/tests/TestTown2.cs` | Interactive town test scene |
| `scenes/tests/test_town2.tscn` | Town scene file |
| `scripts/game/inventory/BankData.cs` | Bank storage data class |
| `scripts/game/inventory/BankSystem.cs` | Deposit/withdraw/expand logic |
| `scripts/game/inventory/BackpackSystem.cs` | Backpack expansion logic |
| `scripts/game/inventory/AffixData.cs` | Item affix data class |
| `scripts/game/inventory/ItemGenerator.cs` | Procedural item generation + loot tables |
| `tests/BankTests.cs` | 15 bank tests |
| `tests/BackpackTests.cs` | 9 backpack tests |
| `tests/ItemGeneratorTests.cs` | 51 item generation tests |

### What We Learned

1. **Two parallel systems = half the findings.** The #1 architectural debt is GameCore.cs coexisting with the entity framework. Divergent formulas, inconsistent data models, tests that validate the wrong values.

2. **GetMeleeDamage double-counts BaseDamage.** TotalDamage already includes BaseDamage, but GetMeleeDamage adds it again. Player does 12 extra damage at level 1. Easy to miss because tests validate the buggy code.

3. **Code formulas diverge from locked specs.** STR multiplier is 0.5 in StatSystem but 1.5 in stats.md. VIT bonus is ×3 in code but ×5 in spec. Either code or spec must be authoritative — not both.

4. **EffectSystem's single-tick-per-frame is a time-bomb.** A lag spike causes poison to underapply. Use `while` instead of `if` to drain accumulated ticks.

5. **Factory HP doesn't match StatSystem.** EntityFactory hardcodes MaxHP=108, but StatSystem.GetMaxHP returns 123 for the same entity. The factory should call RecalculateDerived after creation.

6. **Grid-scan placement is reliable for tight spaces.** Challenge room grid-scan hits 95%+ vs random's 25-55%.

7. **Monster taxonomy needs 5 axes.** Species Family, Mutation Tier, Behavior Role, Element, Dungeon Role. The dungeon-as-breeder framing makes all 5 narratively coherent.

8. **Zone-exclusive creature families are the key to replayability.** Every 10 floors should feel like a different game. Hollow Knight and Hades prove this works.

9. **Monster families > random encounters.** D4's pack composition (swarm + tank + caster from same family) creates tactical encounters. Random individual spawns feel generic.

10. **NpcPanel potion values don't match ItemGenerator.** Shop sells 30HP potions at 50g, generator creates 50HP potions at 25g. Need single source of truth for item definitions.

---

## Session 6c — Full Game Loop Build (2026-04-09)

### What We Built

The complete gameplay loop from app open to exit, built in 5 phases with parallel agents.

**Phase 0: Infrastructure**
- `SaveSystem.cs` (SaveSerializer) — pure C# serialization of full GameState to JSON (player stats, inventory, equipment, location, floor)
- `SaveFileIO.cs` — Godot FileAccess wrapper for save/load to `user://saves/slot_N.json`
- `SceneManager.cs` — autoload singleton with GoToMainMenu/Town/Dungeon/CharacterCreate
- Registered SceneManager as autoload in project.godot, set main scene to MainMenu
- 29 new tests for save round-trips, item serialization, equipment, affixes, error handling

**Phase 1: Menu Layer**
- `MainMenu.cs` + `MainMenu.tscn` — New Game / Load Game / Exit, styled with dark theme + gold accents
- `CharacterCreate.cs` + `CharacterCreate.tscn` — name entry + stat preview + Begin Adventure
- Load Game shows slot summary, routes to correct scene based on saved location

**Phase 2: Player & HUD**
- `PlayerController.cs` — CharacterBody2D with isometric Transform2D, Input Map actions, cyan diamond placeholder sprite
- `GameplayHud.cs` — HP/MP orbs + floor label + gold counter + level/XP, updated from GameState each frame
- `PauseMenu.cs` — ProcessMode.Always, Esc toggle, Resume/Save/Exit buttons

**Phase 3: Town Scene**
- `TownScene.cs` + `Town.tscn` — full gameplay town replacing test scene
- PlayerController with camera follow, wall collision via tile checking
- NPC proximity detection (48px) → NpcPanel with shop UI
- Dungeon entrance with "Press S to enter" prompt → SceneManager.GoToDungeon(1)
- HUD + PauseMenu integrated

**Phase 4: Dungeon Scene**
- `DungeonScene.cs` + `Dungeon.tscn` — full gameplay dungeon
- Floor loading via FloorCache + DungeonGenerator, deterministic seeds
- Tile rendering (ISS floor diamonds + edge wall blocks) copied from TestDungeonGen
- PlayerController with wall collision, camera follow, exploration tracking
- `EnemyEntity.cs` — tier-colored diamond enemies with HP bars, chase AI (6-tile aggro), melee attack (1.5s cooldown)
- Combat: S key attacks nearest enemy within 78px, 0.42s cooldown, slash effect, floating damage
- Enemy death: XP award, gold, loot roll via ItemGenerator, fade-out
- Floor transitions: reaching Exit room → "Floor Complete!" → next floor
- Boss floors: must kill boss to access exit
- Town return: floor 1 entrance → "Press S to return to town"
- Player death: "You Died" overlay → respawn in town
- Automap integration: M key cycles, exploration tracking, player position

### The Complete Game Loop

```
App Open → MainMenu (New Game / Load / Exit)
  → New Game → CharacterCreate (name entry)
    → Town (explore, talk to NPCs, buy from Item Shop)
      → Walk to dungeon entrance → Press S
        → Dungeon Floor 1 (fight enemies, gain XP/gold/loot)
          → Reach exit → Floor 2
            → Esc → Save → Exit to Menu
              → Load Game → Resume on Floor 2
                → Floor 1 entrance → Town
                  → Esc → Save → Exit to Menu
                    → Load → Resume in Town
                      → Exit Game
```

Every step of this loop is now implemented in code.

### Milestone Tracking

| Milestone | Phase | Status |
|-----------|-------|--------|
| SaveSystem (29 tests) | 0 | Done |
| SceneManager autoload | 0 | Done |
| project.godot updated | 0 | Done |
| MainMenu scene | 1 | Done |
| CharacterCreate scene | 1 | Done |
| PlayerController | 2 | Done |
| GameplayHud | 2 | Done |
| PauseMenu | 2 | Done |
| Town gameplay scene | 3 | Done |
| EnemyEntity | 4 | Done |
| Dungeon gameplay scene | 4 | Done |
| Full build verification | 5 | 380 tests, 0 errors |

### Files Created (14 new files)

| File | Purpose |
|------|---------|
| `scripts/game/SaveSystem.cs` | Pure C# save serialization |
| `scripts/game/SaveFileIO.cs` | Godot file I/O wrapper |
| `scripts/autoloads/SceneManager.cs` | Scene transition autoload |
| `scripts/ui/MainMenu.cs` | Main menu UI |
| `scenes/ui/MainMenu.tscn` | Main menu scene |
| `scripts/ui/CharacterCreate.cs` | Character creation UI |
| `scenes/ui/CharacterCreate.tscn` | Character creation scene |
| `scripts/player/PlayerController.cs` | Isometric player controller |
| `scripts/ui/GameplayHud.cs` | Gameplay HUD overlay |
| `scripts/ui/PauseMenu.cs` | Pause menu with save/exit |
| `scripts/town/TownScene.cs` | Town gameplay scene |
| `scenes/Town.tscn` | Town scene file |
| `scripts/dungeon/DungeonScene.cs` | Dungeon gameplay scene |
| `scripts/dungeon/EnemyEntity.cs` | Enemy entity with AI/combat |
| `scenes/Dungeon.tscn` | Dungeon scene file |
| `tests/SaveSystemTests.cs` | 29 save/load tests |

### Industry Comparison Results (from QA audit)

**Overall readiness: 5.5/10.** Strong foundation (8/10 architecture, 7/10 dungeon gen) but gaps in combat depth (4/10), monster variety (2/10), item excitement (5/10).

**Top 5 actions identified:**
1. Spec damage types + resistances (Physical + Fire/Ice/Lightning)
2. Spec unique/legendary items with build-altering effects
3. Design monster behavior archetypes + modifier system
4. Implement A* pathfinding (straight-line chase breaks in proc gen dungeons)
5. Expand crit system + add defense layers

### Test Counts

| Metric | Value |
|--------|-------|
| Total tests | 380 |
| New save tests | 29 |
| All passing | Yes |
| Build errors | 0 |
| Game loop steps covered | 12/12 |

### What We Learned

1. **Parallel agent phasing works for large features.** 5 phases with dependency tracking, parallel where possible. No file conflicts because each agent had a clear scope.

2. **GameCore.cs static GameState persists across scene changes.** Because it's a static class (not a Node), it survives Godot scene transitions naturally. Combined with SceneManager autoload, this gives us state persistence without complex serialization between scenes.

3. **Tile-based wall collision is simpler than physics bodies for proc gen.** Rather than generating StaticBody2D for every wall tile (expensive), check FloorData.IsWall() after MoveAndSlide() and push back. Works for both town and dungeon.

4. **Camera follow = reparent Camera2D to player.** Simplest approach: move the Camera2D node to be a child of PlayerController. Camera automatically follows without any code.

5. **PauseMenu needs ProcessMode.Always.** When GetTree().Paused = true, only nodes with ProcessMode.Always continue to receive input. Without this, the pause menu can't unpause itself.

6. **Deterministic dungeon seeds from floor number.** `floor * 31337 + 42` gives unique, reproducible layouts per floor. Save/load just stores the floor number; the layout regenerates identically.

7. **The full game loop requires 14 new files.** Menu (4), player/HUD (3), town (2), dungeon (3), save (2). Each is relatively small (50-400 lines) because they compose existing systems rather than building new logic.

---

## Session 6d — Gap Closure Research + System Prototyping (2026-04-09)

### Research Completed (6 Deep Dives)

**1. Elemental Damage Systems** — D2 (4 elements + physical, 0/75/immune resistance, difficulty penalties), PoE (5 types, armor formula vs flat resistance, penetration/exposure/conversion layers), LE (7 types, 75% cap, area-level penetration). Recommendation: 7 damage types (Physical + 6 elements) with floor-based resistance penalty (floor/2).

**2. Monster AI + Pathfinding** — Godot 4 AStarGrid2D with native IsometricDown cell shape is the clear winner over NavMesh for proc gen tile grids. D4's 5 archetype system (Melee/Ranged/Bruiser/Swarmer/Support) with finite state machines. D2 modifier system (ExtraFast/StoneSkin/FireEnchanted/etc). Pack composition by room budget with family mixing.

**3. Unique/Legendary Items** — D2 uniques (fixed stats, ~385 items), PoE uniques (build-enabling mechanics > stat sticks), LE Legendary Potential (merge unique + crafted). User chose: fixed effects, Blacksmith can't touch them. Target: 70-100 uniques. No set items.

**4. Alternative Item Approaches (10 explored)** — Monster trophies, evolving items, synergy sets, conditional effects, sacrifice/transmutation, curse/blessing duality, procedural uniques, lore-bound items, socket abilities, corruption/blessing. Top 5 fits: Monster Trophies (perfect lore), Evolving Items (matches skill philosophy), Conditional Effects (casual+theorycrafter), Synergy Sets (10 ring slots), Sacrifice/Transmutation (extends Blacksmith).

**5. Achievement Systems** — Hades prophecies (achievements = resource rewards), Isaac (achievements unlock content), WoW meta-achievements, Grim Dawn devotions (exploration = permanent power). Proposed: "The Dungeon Ledger" with 4 tiers (Chronicle/Trials/Whispers/Sagas), Insight Points for permanent bonuses, hybrid account/per-character scope.

**6. Endgame Progression (11 systems)** — D3 Paragon, PoE Atlas, Hades Heat, LE Corruption, prestige/ascension, NG+, mastery systems, infinite scaling (GR/Delve/VS), power fantasy engineering, single-player seasons, adaptive dungeon intelligence. Top recommendations: Dungeon Pacts (voluntary difficulty), Magicule Attunement (post-cap passive tree), Dungeon Intelligence (adaptive AI Director), Zone Saturation (per-zone infinite scaling).

### Key User Decisions Made

| Decision | Choice |
|----------|--------|
| Elemental damage types | All 6 elements (Physical + Fire/Water/Air/Earth/Light/Dark = 7 total) |
| Unique items | Fixed effects only — Blacksmith can't touch them |
| Unique design philosophy | Mix of both: common = stat packages, rare = build-altering mechanics |
| Skill system | Use-based leveling (like IRL), NOT PoE passive tree |

### Systems Prototyped and Tested

Built 3 new systems with 100 tests to validate before speccing:

**Elemental Damage (4 files, 20 tests):**
- `DamageType` enum: Physical, Fire, Water, Air, Earth, Light, Dark
- `Resistances` class: per-element resistance, floor penalty (floor/2), -100 floor, 75% cap
- `ElementalCombat`: calculates damage with type-specific mitigation, ambient dark DPS at depth 76+
- Physical uses existing defense DR formula; elemental uses percentage resistance
- Validated: floor scaling erosion, crit after resistance, min 1 damage, double damage at -100%

**Crit System (1 file, 18 tests):**
- `WeaponType` enum: 16 types (Dagger 8%, Rifle 9%, Club 3%, etc.)
- `CritSystem`: per-weapon base crit, buildable multiplier (150% base), 75% chance cap
- Formula: `baseCrit * (1 + increasedPercent/100) + flatBonus`, capped at 75%
- Validated: all weapon types, statistical distribution over 50K rolls, multiplier stacking

**Monster AI (4 files, 62 tests):**
- `MonsterArchetype` enum: Melee, Ranged, Bruiser, Swarmer, Support
- `MonsterBehavior`: finite state machine (Idle→Alert→Chase→Attack→Cooldown→Reposition/Retreat/Flee/Dead)
- `MonsterModifiers`: 10 types (ExtraFast 1.33x speed, ExtraStrong 1.5x damage, StoneSkin 80 defense, etc.)
- `MonsterSpawner`: room budget (area/12), rarity rolls (Normal 78%/Empowered 20%/Named 2%), archetype mix
- Validated: all state transitions, swarmer skips alert, ranged repositions, dead from any state, modifier stacking, rarity distribution

### Test Counts

| Metric | Value |
|--------|-------|
| Total tests | 480 |
| New this session | 100 (20 elemental + 18 crit + 62 monster) |
| All passing | Yes |
| Build errors | 0 |
| Run time | <1 second |

### Files Created

| File | Purpose |
|------|---------|
| `scripts/game/systems/DamageType.cs` | 7 damage type enum |
| `scripts/game/systems/Resistances.cs` | Per-entity elemental resistances |
| `scripts/game/systems/ElementalCombat.cs` | Elemental damage calculation |
| `scripts/game/systems/CritSystem.cs` | Variable crit per weapon type |
| `scripts/game/monsters/MonsterArchetype.cs` | 5 archetypes + AI states + rarity enums |
| `scripts/game/monsters/MonsterBehavior.cs` | AI state machine logic |
| `scripts/game/monsters/MonsterModifier.cs` | 10 modifier types + combined effects |
| `scripts/game/monsters/MonsterSpawner.cs` | Room budget, rarity, pack composition |
| `tests/ElementalCombatTests.cs` | 20 elemental tests |
| `tests/CritSystemTests.cs` | 18 crit tests |
| `tests/MonsterSystemTests.cs` | 62 monster tests |

### What We Learned

1. **Test before spec.** Building the system first and running 100 tests caught formula edge cases (negative resistance double-damage, crit after resistance ordering) that would have been wrong in a spec-only approach.

2. **The floor penalty formula (floor/2) is elegant.** At floor 150, 75% resistance becomes 0%. At floor 200, it's -25%. This naturally creates the D2 Hell difficulty feel without discrete difficulty tiers.

3. **Ambient dark DPS is the hard ceiling mechanic.** Starting at floor 76 with (floor-75)*2 DPS, floor 200 deals 250 raw dark DPS. Combined with floor-eroded dark resistance, this creates a survival gradient that no build can fully overcome — matching the magicule lore perfectly.

4. **Per-weapon crit creates meaningful weapon choice.** Daggers (8%) vs Clubs (3%) validated statistically over 50K rolls. This alone creates a "crit build vs raw damage build" decision that didn't exist before.

5. **Swarmer skipping alert state changes combat rhythm.** The state machine test proved that swarmers rush immediately while bruisers pause 0.5s. This single difference creates distinct encounter feelings from the same AI framework.

6. **Monster modifier stacking is multiplicative.** ExtraFast + ExtraStrong = 1.33x speed AND 1.5x damage. This means Named monsters with 3 modifiers can be terrifying combinations — validated by the combined effects test.

7. **Room budget of area/12 scales naturally with floor size.** A 12x12 room spawns 12 monsters. A 24x24 room spawns 48. Progressive floor sizing means deeper floors have bigger rooms with more monsters — no separate scaling needed.

8. **The rarity distribution (78/20/2) matches D2's feel.** Validated over 50K rolls: Normal packs are the majority, Empowered are uncommon but frequent enough to notice, Named are rare enough to be exciting.

---

## Session 6e — Universal Test Runner + Full E2E Pipeline (2026-04-09)

### What We Built

**Full game loop E2E test (`TestGameRun.cs`):**
- 16-phase automated test running the complete game loop: init → town → shop → dungeon → combat → floor transitions → save/load → bank → backpack → systems validation → summary
- 60 assertions validating every system: GameState, GameSystems, DungeonGenerator, SaveSerializer, BankSystem, BackpackSystem, ElementalCombat, CritSystem, MonsterBehavior, MonsterSpawner, MonsterModifiers, ItemGenerator
- Runs headless in ~3 seconds, auto-quits, logs everything with `[TEST-GAME]` prefix
- E2E shell script greps 22 phase markers for CI validation

**Universal test runner (`tests/run-test.sh`):**
- Single entry point for ALL test scenes with 4 modes via flags
- Auto-resolves scene paths (handles dashes, underscores, `test_` prefixes)
- Lists available scenes on error
- Works with ANY scene, not just test-game

**Screenshot + video capture pipeline:**
- `--capture` flag: timed screenshots at key moments (2s, 5s, 10s, 15s, 20s) + 20s video recording
- Evidence saved to `docs/evidence/<scene-name>/` with timestamps
- Uses macOS `screencapture` — no dependencies

**Regression testing:**
- `--check` flag: headless run + crash/exception detection + evidence artifact verification
- Scene-specific checks (test-game gets 6 extra assertions for game loop phases)

### The Universal Test Command

```
make t S=<scene> [F=--flag]
```

| Flag | Mode | What It Does |
|------|------|-------------|
| *(none)* | Windowed | Launch scene, watch it run |
| `--headless` | Headless | Console output, auto-quits, CI-ready |
| `--capture` | Capture | Screenshots at timed intervals + video |
| `--check` | Regression | Headless + crash detection + evidence check |

**Examples that now work on ANY test scene:**
```
make t S=test-game                    # watch game loop
make t S=test-game F=--headless       # CI: 60 assertions
make t S=test-game F=--capture        # screenshots + video
make t S=test-game F=--check          # full regression
make t S=test-hero F=--capture        # capture hero viewer
make t S=test-dungeon F=--headless    # headless dungeon gen
make t S=test-hero F=--check          # check hero doesn't crash
```

### Why This Matters

**This is a major architectural pattern.** Instead of writing separate capture/check/headless scripts for each test scene (which we were doing — `e2e_demo_test.sh`, `e2e_visual_test.sh`, `e2e_game_test.sh`, `e2e_game_capture.sh`, `e2e_game_visual_test.sh`), one universal runner handles everything. Adding a new test scene requires ZERO testing infrastructure — `run-test.sh` discovers it automatically.

The pattern is: **scene + mode = test**. Any scene can be run in any mode. The modes are orthogonal to the content. This scales to 100 test scenes without 100 shell scripts.

### Full Testing Suite

| Command | Type | Duration | Assertions |
|---------|------|----------|------------|
| `make test` | Unit (xUnit) | <1s | 480 tests |
| `make t S=test-game F=--headless` | Integration | ~3s | 60 assertions |
| `make t S=test-game F=--capture` | Evidence gen | ~50s | 5 screenshots + video |
| `make t S=test-game F=--check` | Regression | ~5s | Crash + phase + evidence |
| `make test-all` | Full CI | ~5s | 480 unit + 60 E2E |

### What We Learned

1. **Scene + mode = test is the right abstraction.** Separating "what to test" (scene) from "how to test" (mode) eliminates per-scene test boilerplate. One script, infinite scenes.

2. **Auto-resolving scene paths prevents typos.** `run-test.sh` tries `test-hero`, `test_hero`, `hero`, and lists available scenes on failure. No memorizing exact filenames.

3. **Evidence directories per scene keep artifacts organized.** `docs/evidence/test-game/`, `docs/evidence/test-hero/` — each scene's screenshots and videos are isolated.

4. **The `--check` mode is the real CI workhorse.** It catches: crashes (grep for SCRIPT ERROR), unhandled exceptions, missing evidence (run `--capture` first), AND scene-specific assertions. One command validates everything.

5. **Headless Godot is a legitimate CI tool.** The full game loop runs in 3 seconds headless with zero visual rendering. This is faster than most web app E2E suites.

---

## Session 7 — Knowledgebase, Tracker Rewrite, Bug Fixes (2026-04-10)

### What We Built

**Game dev knowledgebase (22 docs in docs/basics/):**
Built a permanent reference library covering sprites, collision, tilemaps, rendering, UI, camera, game feel, state machines, ARPG design, visual feedback, audio, save systems, pathfinding, procedural generation, performance, debugging, Godot TileSet, animation, shaders, patterns, difficulty, and playtesting. Each doc has: Why This Matters, Core Concepts, Godot 4 C# Implementation, Common Mistakes, Checklist, Sources.

**Complete dev tracker rewrite:**
Replaced the 1183-line original tracker (pre-implementation roadmap from before coding started) with a reality-based tracker. New structure: What's Built (done), What's Partial (bugs), What's Not Built (to do) with clear milestones: Playable Alpha → Feature Complete → Endgame → Polish → Ship.

**Bug fixes this session:**
- Pause menu centering (CenterContainer pattern)
- Font loading errors (ResourceLoader.Exists guard)
- NPC panel auto-open → requires button press
- Game menu tabs (Inventory, Equipment, Stats, Skills, Settings, Game)
- Settings panel (audio, gameplay, display, controls)
- Responsive UI (containers replace hardcoded positions)
- Test-game launches real game (not test harness)
- Sprite align tool (sidebar UI, save system, auto-scan)

**Sprite alignment tools:**
- `tool-sprite-align`: sidebar with category dropdown, sprite list, scale/offset spinboxes, save button, auto-scan filesystem
- `tool-sprite-frames`: sheet view, frame inspector, animation strip export

### Key Decisions

| Decision | Rationale |
|----------|-----------|
| 22 docs not 39 | Cut 17 that overlapped with existing docs/reference/ |
| CenterContainer for all centering | GrowDirection gotcha makes manual anchors unreliable |
| New tracker from scratch | Old tracker was pre-implementation; 90% of statuses were wrong |
| Milestone-based organization | Playable Alpha → Feature Complete → Endgame → Polish → Ship |

### What We Learned

1. **We were coding like software engineers, not game developers.** The knowledgebase exists because every visual bug traced to not knowing game dev fundamentals.
2. **Play the game, look at the screen.** The #1 debugging rule from debugging-games.md. We kept reading code instead of looking at what rendered.
3. **CenterContainer > manual anchors.** Godot's GrowDirection gotcha (GitHub #86004) means programmatic centering fails silently. CenterContainer just works.
4. **The old tracker was fiction.** 90% of tickets were "To Do" but the work was done — just not via the ticket system. Starting fresh with reality-based tracking.
5. **ResourceLoader.Exists before Load.** Font loading crashed because the import cache was stale. Always check existence first.

---

## Session 8 — Reset Decision (2026-04-10)

### What Happened

The user tested the game repeatedly and every time found fundamental visual issues that couldn't be fixed incrementally:
- Floor tiles never rendered correctly (TileLayout was wrong — Stacked instead of DiamondDown)
- Wall tiles clipped and overlapped wrong
- Character sprites didn't load (wrong file paths, wrong folder structure)
- UI windows positioned off-screen (GrowDirection gotcha, hardcoded pixels)
- Enemy sprites didn't animate correctly
- Collision was janky (manual tile-check pushback instead of TileMap physics)
- Font loading crashed on every launch
- The game menu opened broken every time

Each fix revealed another bug underneath. The codebase accumulated 7,000+ lines of game scene code across 6 sessions of rapid parallel-agent development, but none of it was ever validated by actually playing the game and looking at the screen.

### The Decision

**Delete all code. Keep all documentation and assets. Start from scratch.**

The user will learn game development themselves and rebuild, because:
1. The AI kept patching symptoms instead of understanding the game engine
2. Parallel agents built code that was never visually tested together
3. Every "fix" introduced new bugs because the foundations were wrong
4. The AI thought like a software engineer, not a game developer — building systems that pass unit tests but look broken on screen

### What We Keep
- `docs/` — all 80+ documentation files including the 22 learning docs in `docs/basics/`
- `docs/decisions/` — 5 architecture decision records
- `docs/design-pillars.md`, `docs/glossary.md`
- `assets/` — all sprite sheets, tile sets, fonts, icons (819+ game assets, properly sorted)
- `tests/` — the 480 unit tests and test infrastructure (pure C# logic is correct)
- `AGENTS.md`, `CLAUDE.md` — AI instructions with post-task protocol

### What We Delete
- All Godot scene scripts (`scripts/dungeon/`, `scripts/town/`, `scripts/player/`, `scripts/ui/`)
- All scene files (`scenes/Town.tscn`, `scenes/Dungeon.tscn`, `scenes/ui/`)
- The game loop code that was never visually correct

### What We Learned (The Hard Way)

1. **Unit tests passing ≠ game working.** 480 tests passed. The game was broken. Tests validate math, not rendering. You can only validate rendering by LOOKING AT THE SCREEN.

2. **Parallel agents are dangerous for visual code.** 3 agents building scenes simultaneously means nobody verifies that the pieces work together visually. Each agent's code compiles and passes tests, but the combined result is broken.

3. **Never build faster than you can playtest.** We built 14 scene files in one session without playing the game once. Every one of them had visual bugs that compounded.

4. **The AI doesn't understand game development yet.** Despite 22 learning docs, the AI still made fundamental mistakes: wrong TileLayout, wrong sprite paths, wrong collision approach, hardcoded UI positions. Reading about game dev is not the same as understanding it.

5. **The user was right every time.** Every time the user said "this is broken," it was broken. Every time the AI said "it should work," it didn't. Trust the screen, not the code.

6. **Start small, verify visually, then build.** The correct approach: render ONE tile correctly. Then ONE character on ONE tile. Then ONE room. Then movement. Then combat. Verify EACH step visually before adding the next. Not: build everything in parallel and hope it works.

7. **Documentation is the lasting value.** The 22 game dev docs, 5 ADRs, design pillars, glossary, 26 game specs, and dev journal — this knowledge persists. The broken code doesn't. The next attempt starts with better understanding.

### The Salvageable Work
- `scripts/game/` — pure C# game logic (GameCore, entity framework, combat, stats, effects, progression, skills, inventory, bank, items, monsters, dungeon generation). This is engine-independent and correct.
- `scripts/game/systems/` — elemental damage, crit system, resistances. Tested, works.
- `scripts/game/monsters/` — archetypes, behavior, modifiers, spawner. Tested, works.
- `scripts/game/dungeon/` — BSP, corridors, cellular automata, floor cache. Tested, works.
- `tests/` — all 480 tests validate the logic layer correctly.

The logic is sound. The rendering was not. The next build should use the tested logic layer but rebuild ALL Godot scene integration from scratch, one visual step at a time.

---

## Session 8 Addendum — 2026-04-10

Session 8 listed `tests/` and `scripts/game/` under "What We Keep," but the actual commit (`1f917e2`) deleted everything — tests, scripts, scenes, all of it. What actually survived the fresh start: docs, assets, and config files only. No C# source code, no test files, no scene files remain.

---

## Session 9 — Docs Cleanup (2026-04-10)

### What Happened

**Started from:** 80+ docs describing a game that no longer exists in code. Config files referencing deleted scenes/scripts.

**Ended with:** All docs cleaned up to reflect reality. New ticket structure for visual-first rebuild.

### What We Did

1. **Fixed config files:**
   - `project.godot` — removed stale main scene and autoload references
   - `Makefile` — stripped 50+ targets referencing deleted scenes/scripts, kept core targets
   - `.githooks/pre-commit` — replaced GDScript lint with C# format check
   - `CLAUDE.md` — updated to "Fresh start" mode
   - `AGENTS.md` — updated current state, project structure, priorities, NuGet notes

2. **Reframed architecture docs as design blueprints:**
   - `scene-tree.md`, `autoloads.md`, `signals.md`, `entity-framework.md`, `project-structure.md`, `tech-stack.md`
   - Changed "Current State: X exists" to "Design spec: X will be built"

3. **Reframed object docs as design specs:**
   - `player.md`, `enemies.md`, `tilemap.md`, `effects.md`

4. **Rewrote tracking docs:**
   - `dev-tracker.md` — complete rewrite with new VIS-*, PROTO-*, CFG-* tickets
   - `docs/README.md` — updated navigation, removed stale test references
   - `CHANGELOG.md` — added fresh-start entry documenting what was deleted/retained
   - `dev-journal.md` — added Session 8 addendum (factual correction)

5. **Updated testing docs:**
   - `test-strategy.md`, `automated-tests.md`, `manual-tests.md` — reframed as target strategy

6. **Updated root README.md:**
   - Status changed to "Fresh start"
   - Removed archived Phaser prototype section (deleted)

7. **Updated team ticket boards:**
   - Added fresh-start notes, updated ticket references to VIS-*/PROTO-*

8. **Created new ticket structure:**
   - Phase 0: VIS-01 through VIS-06 (visual foundation)
   - Phase 0.5: PROTO-01 through PROTO-06 (playable prototype)
   - Config: CFG-01 through CFG-05

### Key Decision

55+ docs (game design specs, learning material, ADRs, conventions) were **left untouched** — they are blueprints for what to build, not claims about what exists.

~30 docs were updated — all changes were reframing "what exists" to "design spec" or removing references to deleted code/scenes/tests.

---

## Session 10 — Full Prototype Build (2026-04-11)

### What Happened

**Started from:** Zero code, zero scenes. 80+ locked specs, character sprites (warrior/mage/ranger), and project config.

**Ended with:** A playable dungeon crawler with real PixelLab art, 8 C# scripts, 7 scenes, 2 autoloads, level-based enemies with a full color gradient, floating combat text, a pause menu, and a floor scaling system.

**Approach:** The user gave full autonomy — "go ham, no micromanaging." AI built everything from scratch in one session, generating art assets in parallel with code.

### What We Built (in order)

#### Phase 1: Asset Generation (PixelLab)
- Generated isometric floor tile (64px, thin tile, dark blue-gray cobblestone)
- Generated isometric wall tile (64px, block, lighter blue-gray brick)
- Generated Skeleton Enemy character (92x92, 8 directional rotations + walking animation)
- Generated 3 floor tile variations (cracked, flagstone, worn) via background agent
- Initially generated at 32px, upscaled with sips, then regenerated natively at 64px for crispness

**Learning:** PixelLab's `size` parameter is canvas size, not tile footprint. A 64px canvas produces a 64x64 PNG. For isometric tiles with a 64x32 footprint, the TileSet `TextureRegionSize` should be `Vector2I(64, 64)` with `TileSize` at `Vector2I(64, 32)`.

**Learning:** PixelLab rate limits at 8 concurrent jobs. Walking animations (8 directions) consume all 8 slots. Queue animations after rotations complete, not simultaneously.

#### Phase 2: Core Code (Autoloads + Scripts)
- `GameState.cs` — HP, MaxHp, Xp, Level, FloorNumber with reactive setters + signals
- `EventBus.cs` — EnemyDefeated, EnemySpawned, PlayerAttacked, PlayerDamaged signals
- `Player.cs` — movement, auto-attack, slash effects, damage flash
- `Enemy.cs` — level-based stats, chase AI, contact damage, color gradient
- `Dungeon.cs` — programmatic TileSet, room painting, enemy spawning, floor advancement
- `Main.cs` — death handling, scene management
- `Hud.cs` — reactive stats display
- `DeathScreen.cs` — restart/quit with keyboard shortcuts

#### Phase 3: Scenes (.tscn files)
All 7 scenes written by hand in Godot's text format (no editor):
- `main.tscn`, `dungeon.tscn`, `player.tscn`, `enemy.tscn`, `hud.tscn`, `death_screen.tscn`, `pause_menu.tscn`

**Learning:** Writing .tscn files by hand is viable for simple scenes. Key format details: `load_steps` count, `ExtResource` IDs, `SubResource` IDs, `layout_mode` for Control nodes, `process_mode = 3` for PROCESS_MODE_ALWAYS.

#### Phase 4: Test Suite (sequenced debugging)
- **Test 1:** Room + player only (no enemies) — verified tiles render, movement works
- **Test 2:** Single enemy — verified auto-attack, slash effect, XP gain, enemy death/respawn
- **Test 3:** Full game (10 enemies, spawn timer) — verified game loop end-to-end

**Learning:** Always test incrementally. Spawning 10 enemies immediately overwhelmed the player in a 10x10 room. Expanded to 24x24 room with 8 initial enemies for breathing room.

### Issues Found & Fixed

| Issue | Root Cause | Fix |
|-------|-----------|-----|
| Death/restart broke after first restart | C# `+=` signal subscriptions on autoloads don't auto-disconnect when scene nodes are freed | Added `_ExitTree()` to disconnect from autoload signals in Main, Dungeon, Hud |
| Esc didn't work on death screen | `GetTree().Paused = true` freezes Main's input handler | Added Esc handling to DeathScreen (has `process_mode = ALWAYS`) |
| Player wiggled along walls | Diamond-shaped collision polygons on wall tiles created zigzag edges | Changed wall collision to full rectangle — adjacent tiles merge into smooth straight edges |
| Isometric movement felt wrong | User expected up=up, not up=northeast | Removed IsoTransform matrix — screen-space movement (up=up, down=down) |
| Camera shake caused motion sickness | Camera offset tween on damage | Replaced with red sprite flash (0.15s tween on Modulate) |
| Enemies spawned on top of player | No minimum distance check | Added SafeSpawnRadius (150px) with 10 retry attempts |

### Design Decisions Made

1. **Screen-space movement over isometric transform.** The user explicitly rejected Diablo-style isometric input mapping. Up arrow = up on screen, period. This overrides the movement spec in `docs/systems/movement.md`.

2. **No camera shake, red flash instead.** Camera shake induces motion sickness. Damage feedback is a red sprite flash (0.15s). This overrides the camera shake spec in `docs/systems/camera.md`.

3. **Level-based enemies over tier-based.** Replaced the 3-tier danger system (green/yellow/red) with actual enemy levels. Color is now computed from `(enemyLevel - playerLevel)` using the full 8-anchor gradient from `docs/systems/color-system.md`.

4. **Floor = Level formula.** `baseLevel = floorNumber`, spawn range `[floor-1, floor+2]`. Documented in `docs/systems/floor-scaling.md`. Transparent and metagame-able.

5. **Rectangular wall collision.** Diamond-shaped collision on wall tiles causes jitter. Full-rectangle collision creates smooth sliding walls. This overrides the collision polygon in `docs/objects/tilemap.md`.

6. **Spawn safety rules.** 150px safe radius around player, 1.5s invincibility grace period on floor entry. Documented in `docs/systems/spawn-safety.md`.

### New Files Created

| File | Purpose |
|------|---------|
| `scripts/autoloads/GameState.cs` | Reactive game state singleton |
| `scripts/autoloads/EventBus.cs` | Decoupled signal hub |
| `scripts/Player.cs` | Player movement, combat, flash effects |
| `scripts/Enemy.cs` | Level-based enemy with color gradient |
| `scripts/Dungeon.cs` | Room generation, spawning, floor advancement |
| `scripts/Main.cs` | Scene management, death handling |
| `scripts/ui/Hud.cs` | Stats overlay |
| `scripts/ui/DeathScreen.cs` | Death screen with restart/quit |
| `scripts/ui/PauseMenu.cs` | Esc-toggle pause menu |
| `scripts/ui/UiTheme.cs` | Shared UI palette + factory methods |
| `scripts/ui/FlashFx.cs` | Reusable sprite flash effects |
| `scripts/ui/FloatingText.cs` | Floating combat text (damage, XP, heal) |
| `scripts/GameSettings.cs` | Toggleable settings (combat numbers) |
| `scenes/*.tscn` (7 files) | All game scenes |
| `assets/tiles/floor.png` | PixelLab floor tile (64x64) |
| `assets/tiles/wall.png` | PixelLab wall tile (64x64) |
| `assets/tiles/floor_*.png` (3 files) | Floor tile variations |
| `assets/characters/enemy/` | Skeleton enemy (8-dir + walking) |
| `docs/systems/floor-scaling.md` | Floor difficulty formula spec |
| `docs/systems/spawn-safety.md` | Spawn safety rules spec |
| `.claude/agents/art-lead.md` | PixelLab art generation agent |

### What We Learned

1. **C# signal subscriptions (`+=`) leak across scene reloads.** Autoload signals persist but subscribing scene nodes are freed. Always pair `+=` in `_Ready()` with `-=` in `_ExitTree()`. This is the #1 bug pattern in Godot C# with autoloads.

2. **Isometric movement is a game feel choice, not a technical requirement.** The isometric tile grid and the movement system are independent. You can have isometric tiles with screen-space movement and it feels natural. Don't force the Diablo control scheme — let the user decide.

3. **Camera shake is a motion sickness risk.** Sprite flashing achieves the same "you got hit" feedback without moving the viewport. Red flash (0.15s) is universally readable.

4. **Test incrementally with sequenced runs.** Don't launch with the full game and debug everything at once. Build up: empty room → movement → single enemy → full game. Each step catches different bugs.

5. **PixelLab art can run in parallel with coding.** Kick off asset generation with a background agent while writing code. By the time the code compiles, the art is ready to download.

6. **Write .tscn files by hand for simple scenes.** The Godot text scene format is learnable. For scenes with <15 nodes, hand-writing is faster than fighting the editor.

7. **Rectangular wall collision > diamond collision for smooth sliding.** Isometric diamond collisions create zigzag edges. Full-rectangle collisions on adjacent tiles merge into straight walls.

8. **DRY the UI early.** A shared `UiTheme.cs` with color constants and `StyleBoxFlat` factories prevents copy-paste drift across scenes. Same gold border, same panel bg, same button style everywhere.

---

## Session 10b — Hitscan Combat, Save/Load, Stats, Compass, 7 Species, Floor Wipe (2026-04-11)

### What Happened

**Started from:** Playable prototype from Session 10 — movement, combat, enemies, floors, HUD, death screen.

**Ended with:** A vastly deeper game with hitscan projectiles, save/load persistence, stat allocation, 7 enemy species, stairs compass navigation, death penalty flow, loot drops, floor wipe bonuses, full town with buildings and NPCs, and a complete item/inventory system.

### What We Built

#### Hitscan Projectile System
Replaced physics-based projectile collision with instant damage + cosmetic tracer. The arrow was passing through enemies due to Y-offset mismatch between projectile spawn and enemy hitbox planes. Instead of fighting the physics engine, switched to the industry-standard ARPG approach: hitscan determines the hit instantly, then a cosmetic tracer (arrow or bolt sprite) flies to the target for visual feedback.

**Learning:** Don't fight projectile physics — use hitscan + visual tracer. This is the industry standard for ARPGs. Instant damage calculation with a cosmetic-only projectile eliminates all collision plane issues.

#### Stairs Compass Navigation
Two arrows on screen edges pointing to stairs-down (gold) and stairs-up (green). Auto-hides when the respective staircase is visible on screen. Gives the player constant orientation in large procedurally generated maps.

#### Stairs & Floor Fixes
- Labels now recreated on floor descent (was showing "Return to Town" on floor 2)
- Floor 1 stairs-up goes directly to town (no dialog)
- Player spawns south of stairs-up (40px offset)
- Stairs exclusion zone: enemies can't spawn within 150px of either staircase

#### Map Size Increase
Minimum map size increased to 50-70 tiles. Larger maps make exploration meaningful and give the compass a reason to exist.

#### Save/Load System
- `SaveData`, `SaveSystem`, `SaveManager` autoload
- Auto-save on floor transitions and town entry
- Continue from title screen loads last save
- Persists floor number, player stats, inventory, gold

#### Stat System
- STR/DEX/STA/INT with diminishing returns
- Class-specific bonuses per level (Warrior gets more STR, etc.)
- Free stat points on level-up
- HP regen from STA stat
- Stat allocation dialog accessible from pause menu

#### Death Penalty Multi-Step Flow
- XP loss on death
- Item loss chance
- Gold buyout option to recover lost items
- Sacrificial Idol consumable prevents all death penalties

#### Loot Drops & Gold Economy
- Enemies drop gold on death
- Item drop chance per enemy kill
- Gold economy feeds into shop system and death penalty buyout

#### 7 Enemy Species
Skeleton, Goblin, Bat, Wolf, Orc, Dark Mage, Spider. Each with unique collision shapes via `SpeciesConfig` and `SpeciesDatabase`. Per-species configuration replaces the old one-size-fits-all enemy setup.

#### Floor Wipe Mechanic
Bonus rewards when all enemies on a floor are killed. Incentivizes full exploration over rushing to stairs.

#### Town Expansion
- Expanded to 24x20 tiles
- Buildings placed behind NPCs for visual context
- Cave entrance at top of town leading to dungeon
- NPC interaction with S key
- Frontier town lore

#### Targeting System
8 targeting modes + 3 projectile behaviors. Data-driven configuration per class and attack type.

#### Dialogue & Shop UI
- Visual novel dialogue system with typewriter effect and portraits
- JRPG-style shop window with buy/sell tabs
- Keyboard navigation across all dialogs (Q/E bumpers)

#### Item System
- `ItemDef`, `ItemDatabase`, `Inventory` classes
- Full item definitions with stats, descriptions, rarity
- Inventory management with equip/use/drop

#### Hand-Drawn Projectile Sprites
Arrow and magic bolt sprites drawn by hand for projectile visuals.

#### Performance
- GC forced during loading screen transitions to prevent hitches during gameplay

#### Documentation Audit
11 specs updated to match code — full reconciliation between docs and implementation.

### Issues Found & Fixed

| Issue | Root Cause | Fix |
|-------|-----------|-----|
| Arrow passing through enemies | Y-offset mismatch between projectile spawn plane and enemy hitbox plane | Replaced with hitscan + cosmetic tracer |
| "Return to Town" label on floor 2 | Stairs labels not recreated on floor descent | Recreate labels each floor transition |
| Enemies spawning on stairs | No exclusion zone around staircases | 150px exclusion radius around both stairs |
| GC hitches during gameplay | Object allocation during floor transitions | Force GC during loading screen |

### Design Decisions Made

1. **Hitscan over physics projectiles.** Physics-based collision is unreliable with isometric Y-offset mismatches. Hitscan + cosmetic tracer is simpler, more reliable, and industry standard.
2. **Compass over minimap for stairs.** Two simple arrows are less intrusive than a minimap and solve the "where are the stairs?" problem directly.
3. **Floor 1 stairs-up = town shortcut.** No dialog, no confirmation. Just go home. Reduces friction.
4. **Forced GC during loading screens.** Players expect loading screens to take a moment. Hide GC pauses there.
5. **Per-species collision.** Each enemy species has unique hitbox dimensions via SpeciesConfig. More realistic than uniform collision for all creatures.

### New Systems

| System | Key Files | Status |
|--------|-----------|--------|
| Hitscan projectiles | `Projectile.cs` | Done |
| Stairs compass | `StairsCompass.cs` | Done |
| Save/load | `SaveData`, `SaveSystem`, `SaveManager` | Done |
| Stat system | `Constants.cs` (stat formulas) | Done |
| Death penalty | Multi-step flow in `Player.cs` | Done |
| Loot drops | Enemy death → gold + items | Done |
| 7 enemy species | `SpeciesConfig`, `SpeciesDatabase` | Done |
| Floor wipe | Bonus on full clear | Done |
| Item system | `ItemDef`, `ItemDatabase`, `Inventory` | Done |
| Targeting | 8 modes + 3 projectile types | Done |
| Town (expanded) | 24x20, buildings, NPCs, cave entrance | Done |
| Dialogue system | Visual novel style | Done |
| Shop system | JRPG buy/sell window | Done |

### What We Learned

1. **Don't fight projectile physics — use hitscan + visual tracer.** This is the industry standard for ARPGs. Instant damage calculation eliminates all collision plane issues while the cosmetic tracer provides the visual feedback players expect.

2. **Compass arrows are better than minimaps for single objectives.** When the player only needs to find one or two things (stairs up/down), dedicated directional indicators are clearer and less screen-intrusive than a full minimap.

3. **Per-species collision makes enemies feel distinct.** A bat and an orc shouldn't have the same hitbox. SpeciesConfig/SpeciesDatabase makes this data-driven rather than hardcoded.

4. **Auto-save on transitions is invisible persistence.** Players never think about saving. Every floor change and town entry saves automatically. Combined with "Continue" on the title screen, this feels modern.

5. **Death penalty needs multiple outs.** XP loss alone feels punishing. Adding gold buyout and Sacrificial Idol gives players agency over the penalty — it's a cost, not a punishment.

6. **Floor wipe rewards exploration.** Without it, players rush to stairs. With it, clearing every enemy on a floor is a meaningful choice with tangible rewards.

7. **Force GC during loading screens.** Players expect loading to take a moment. Hiding garbage collection there prevents mid-gameplay hitches at zero perceived cost.

---

*This journal is append-only. Each session adds a new section. Never edit previous sessions — they're a historical record.*
