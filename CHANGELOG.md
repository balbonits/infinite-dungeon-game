# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Session 21 — Bank & Backpack Redesign: Spec + Implementation (2026-04-17)

Branch: `feat/bank-backpack-redesign`. Milestones 1 (spec lock) + 2a–2g (implementation) landed in the same session. All 402 unit + 11 integration tests green. See the dev-journal Session 21 entry for the narrative; this changelog lists the concrete additions/changes by category.

#### Implementation (milestones 2a–2g)

#### Added
- **NEW: `scripts/logic/NumberFormat.cs`** — abbreviated K/M/B/T/Qa/Qi formatter for stacks and gold, plus `Full(value)` for comma-separated tooltips. Covered by 28 unit tests in `NumberFormatTests`.
- **NEW: `scripts/ui/SlotGrid.cs`** — reusable Godot Control that binds to an `Inventory` and renders a keyboard-navigable slot grid. Category-colored slots, lock icon overlay, abbreviated count, `SlotActivated` + `SlotFocused` events, configurable columns + slot size. Shared by `BackpackWindow`, `GuildWindow` (Bank tab + Transfer tab).
- **NEW: `scripts/ui/GuildWindow.cs`** — merged Store + Bank + Transfer window. Three tabs (Store / Bank / Transfer), opens on Bank (Y2: c), Q/E switches tabs. Store buy flow with "Send to Bank/Backpack" toggle (sticky across session). Bank tab: SlotGrid + gold row (Withdraw All / Deposit All) + inline Upgrade button. Transfer tab: side-by-side SlotGrids, click-to-move, gold transfer buttons. Replaces the old ShopWindow + BankWindow in the post-redesign town.
- **NEW: `scripts/testing/tests/GuildWindowTests.cs`** — GoDotTest suite covering: opens on Bank tab, Upgrade button increases slots, Deposit All moves gold backpack→bank.
- Added `UiHelper.FindButton(predicate)` overload for substring/pattern button lookups.

#### Changed
- **`scripts/logic/Item.cs`**: `ItemStack.Count: long`, new `Locked: bool` flag. Removed `MaxStack` / `IsStackable` (all items stack unlimited).
- **`scripts/logic/Inventory.cs`**: `Gold: long`. Enforces one-type-per-slot invariant in `TryAdd` (merges by Id). New methods: `FindSlot`, `FindEmptySlot`, `ToggleLock`, `Transfer(from, dest, count)`, `Drop(i, count)`. `Drop` refuses Locked items; `Transfer` ignores Lock (Lock only gates Sell/Drop).
- **`scripts/logic/Bank.cs`**: starting slots 50 → 25; +10 → +1; expansion cost `500×N²` → `50×N`. Own `Gold` pocket (safe on death). New `DepositGold`/`WithdrawGold`. `PurchaseExpansion` draws from bank-first then backpack. `Deposit`/`Withdraw` legacy helpers wrap `Inventory.Transfer` and preserve `Locked` flags.
- **`scripts/logic/SaveData.cs`** + **`SaveSystem.cs`**: `Gold: long`, `Count: long`, `Locked` flag round-trip, `SavedBankData.Gold` new field.
- **`scripts/logic/DeathPenalty.cs`**: new `GetEquipmentBuyoutCost` (`floor×25`), `GetBackpackBuyoutCost` (`floor×60`), `GetBothBuyoutCost`, `WipeBackpack`, `PayBuyout(backpack, bank, cost)` (backpack-first). Legacy `GetExpProtectionCost` / `GetBackpackProtectionCost` / `ApplyItemLoss` retained for compat.
- **`scripts/ui/DeathScreen.cs`**: full rewrite. Replaces two-step flow with the 5-option sacrifice dialog (Save Both / Save Equipment / Save Backpack / Accept Fate / Quit Game). Accept Fate + Quit Game have second-confirmation dialogs listing exact losses. Sacrificial Idol still auto-detected (offers free "Save Both"). EXP loss applies in all paths. Equipment loss is stubbed (`TODO SYS-11`) since the equipment system isn't implemented yet.
- **`scripts/ui/BackpackWindow.cs`**: refactored to use `SlotGrid`. Item-actions dropdown gains Lock/Unlock + Drop. Gold displayed via `NumberFormat.Abbrev`. Detail panel shows full count + Locked status.
- **`scripts/Town.cs`**: NPC roster updated per docs/world/town.md. Guild Maid (new — uses Banker sprite placeholder), Village Chief (renamed from Guild Master — reuses old sprite), Blacksmith, Teleporter. Shopkeeper and Banker retired from the scene.
- **`scripts/ui/NpcPanel.cs`**: dispatch updated — Guild Maid → GuildWindow, Village Chief → QuestPanel. Legacy Shopkeeper/Banker branches retained for test compat.
- **`scripts/Strings.cs`**: new NPC names (GuildMaid, VillageChief), new greetings, new NpcServices.OpenGuild. New Death labels (SaveBoth, SaveEquipment, SaveBackpack, AcceptFate, QuitGame). GoldDisplay/BonusGold/Stats accept `long`. Legacy strings retained.
- **`scripts/Constants.cs`**: new `PlayerStats.BackpackStartingSlots = 15`, `BackpackSlotsPerExpansion = 5`, `StartingGold = 0`.
- **`scenes/main.tscn`**: registered `GuildWindow` node on UILayer.
- **`scripts/testing/tests/NpcTests.cs`**: updated to use GuildMaid + GuildWindow + OpenGuild label.
- **`scripts/testing/tests/TownTests.cs`**: expected NPC roster updated (Guild Maid, Blacksmith, Village Chief, Teleporter).
- **`scripts/testing/tests/DeathTests.cs`**: full rewrite for the 5-option dialog. Covers: YOU DIED cinematic, all five sacrifice buttons present, Accept Fate → confirmation → wipes backpack items + gold and returns to town.
- **`scripts/testing/tests/DeathCinematicTests.cs`**: updated to check `Death.SaveBoth` visibility (was `ReturnToTown`).

#### Spec (milestone 1) — part of the same Session 21
- **REWRITTEN: `docs/inventory/bank.md`** — Bank merged with Store into a new Guild window (3 tabs: Store / Bank / Transfer). Starting 25 slots (was 50), +1 per upgrade (was +10), cost `50 × N gold` (was `500 × N²`). Added a gold pocket (bank gold is safe on death — reverses previous prohibition). One-item-type-per-slot, unlimited stacking, no stack splitting within the same storage.
- **REWRITTEN: `docs/inventory/backpack.md`** — Starting 15 slots (was 25), still +5 per upgrade, cost `200 × N² gold + crafting materials` (was `300 × N²` pure gold). Unlimited stacking per slot (was capped at 99). Gold displayed as a label (no Deposit/Withdraw controls on backpack). Expansion moved from Item Shop to Blacksmith. Material tiers: basic (Store) / mid (drops) / high (manufactured at new Blacksmith Workshop tab). Drop = destroy (single confirmation), no ground-drop retention.
- **REWRITTEN: `docs/systems/death.md`** — New 5-option sacrifice dialog: **Save Both** / **Save Equipment** / **Save Backpack** / **Accept Fate** / **Quit Game**. Replaces old multi-step gold-buyout flow. Equipment loss = 1 random of 19 equipped slots. Backpack loss = 100% of items + gold. EXP loss still applies in all paths (no EXP buyout — removed). Sacrificial Idol now acts as a free "Save Both". Gold buyout sourced from player-chosen pocket (backpack first default, override allowed).
- **UPDATED: `docs/inventory/items.md`** — Stacking now unlimited across all categories (stored as `long`, max ~9.2 quintillion). Number display uses abbreviated K/M/B/T format with exact values in tooltips. New Sell Pricing section: consumables/materials 100% of buy price, equipment scales with affix count (10% × (1 + affix_count) of `base_value`). New Item Actions reference: Inspect, Use, Equip/Unequip, Sell, Lock/Unlock, Transfer, Drop. Sell dialog spec with "Sell All but 1" + "Don't ask me again" mute.
- **UPDATED: `docs/systems/equipment.md`** — New "Equipment on Death" section: 1 random of 19 equipped slots lost if equipment not saved, cost `deepestFloor × 25` (~40% of backpack buyout). Locked equipped items prevent accidental unequip but do NOT protect from death.
- **NEW: `docs/ui/guild-window.md`** — Full UI spec for the merged Guild window. Title "Guild", default tab Bank, Q/E tab switching, item-actions dropdown reference, Sell/Drop/Lock dialog specs.
- **UPDATED: `docs/world/town.md`** — NPC roster restructured for the Guild branch lore. Banker and Item Shop retired. New roster (4 NPCs, was 5):
  - **Guild Maid** (Guild Maid Assistant) — operates Store + Bank + Transfer. Art placeholder: current Banker sprite. Future sprite: female maid with glasses, long skirt, logbook (tracked as ART ticket).
  - **Blacksmith** (Old Master Blacksmith) — Forge + new Workshop tab for high-tier material manufacturing. Backpack expansion moved here from the old Item Shop.
  - **Village Chief** (Old Village Chief) — renamed from the previous Guild Master NPC; quest-giver role unchanged.
  - **Teleporter** (Old Master Wizard) — unchanged.
  - Lore: PC is the `{Class} Guildmaster` of a new branch; other NPCs are senior guild personnel supporting the expedition. Maoyuu-style: all NPCs title-only, no personal names.
- **UPDATED: `docs/world/dungeon.md`** — New "Regurgitation: Where Loot Comes From" section explaining the dungeon's absorb-and-re-emit cycle. Monster parts at shallow floors, crates/jars/chests at deeper floors. Also explains in-world how the dungeon takes gold from corpses (answer: don't ask, it's a mystery).
- **UPDATED: `docs/flows/bank.md`** — Rewritten for the Guild window flow. Old `BankWindow.cs` replaced by new `GuildWindow.cs` (to be implemented). Bank tab operations: item-actions dropdown, Sell, Lock, Transfer shortcut, Upgrade sub-dialog, Withdraw/Deposit gold.
- **UPDATED: `docs/flows/shop.md`** — Rewritten for the Guild Store tab. Buy flow: pick item → input amount → choose "Send to Bank/Backpack" → Confirm. Store no longer sells (moved to Bank tab item-actions). Item Shop NPC retired.

(Implementation deferred to milestone 2+ on this branch.)

### Session 19 — Load Game Spec (2026-04-17)

#### Documentation
- NEW: `docs/flows/load-game.md` — full spec for the Load Game screen (3 slots, Class-Select-style layout, delete-with-confirmation dialog, SaveManager multi-slot API, splash-to-LoadGame interaction).
- UPDATED: `docs/flows/splash-screen.md` — button order now Continue (above New Game) / Tutorial / Settings / Exit. Inline CharacterCard removed.
- UPDATED: `docs/flows/save-load.md` — documents the 3-slot save file layout (`user://save_0.json` through `save_2.json`) and `SaveManager` multi-slot methods.
- NEW tickets in `docs/dev-tracker.md`: UI-02 (Load Game implementation, new branch) and ART-01 (splash background image, blocked on PixelLab MCP reconnection).

(Implementation deferred to new branches per the current branch's wind-down directive.)

### Session 18 — Shop/Forge UX Polish (2026-04-17)

#### Fixed
- **Focus highlight made item text unreadable.** Shop/Blacksmith list item buttons only overrode `font_color` (normal state). The GameWindow theme's `font_focus_color`/`font_hover_color` (both `BgDark`) bleed through when a row was focused, rendering black text on a dark-accent highlight. Fix: new `UiTheme.StyleListItemButton()` helper overrides all four font color states (normal/hover/focus/pressed) to `Colors.Ink` (white) so list rows stay legible under any state.
- **Buy button stayed enabled when player couldn't afford.** `ShopWindow.UpdateDescription()` now checks `gold >= item.BuyPrice` and sets `_buyButton.Disabled` accordingly. After a purchase, affordability is re-checked (gold dropped). Applies in both Buy and Sell modes.
- **Keyboard nav dead-end in Forge/Quests/Bank/Teleport when list is empty.** Each window called `FocusFirstButton(ScrollContent)` which found nothing when the list was empty (no craftable items, no quests, etc.), leaving no initial focus — keyboard users couldn't even reach the Cancel/Close button. Fix: new `UiTheme.FocusFirstButtonOrFallback(primary, fallback)` helper tries the primary list first, falls back to `ContentBox` (whole window) if empty. `FocusFirstButton` now returns `bool` so callers can compose fallbacks.

#### Added
- `UiTheme.StyleListItemButton(Button)` — consistent styling for list rows across Shop, Blacksmith, and future windows. Accent-tinted highlight + white text in all states.
- `UiTheme.FocusFirstButtonOrFallback(Control primary, Control fallback)` — guarantees keyboard users can always reach SOME button on window open, even when the primary list is empty.

### Session 17 — Work Discipline Codified (2026-04-17)

#### Added
- `docs/conventions/work-discipline.md` — canonical convention for AI work discipline ("slow is smooth, smooth is fast"). 7 rules, rework-throughput rationale, warning signs, examples.
- "Work Discipline" section in `AGENTS.md` as a top-level principle (second only to Paradigm).
- Jump-to row in `CLAUDE.md` for Work Discipline; promoted "slow is smooth" to Hard Rule #1.

#### Fixed
- Removed invented AI tool filenames (`CURSOR.md`, `COPILOT.md`) from `docs/conventions/ai-context-files.md` and the `AGENTS.md`/`CLAUDE.md` pointers. Replaced with verified filenames from official docs.

### Session 16 — Tabbed PauseMenu, GoDotTest Rewrite, Transition Fixes, SoulsBorne Death (2026-04-17)

#### Added
- **Tabbed PauseMenu** (Diablo 2-style, 8 tabs: Inventory, Equip, Skills, Abilities*, Quests, Ledger, Stats, System) — replaces the old flat button list. Q/E cycles tabs, Esc toggles open/close, D closes.
- **GoDotTest** (`Chickensoft.GoDotTest` v2.0.28) as the in-game UI test framework. Replaces the old AutoPilot/FullRunSandbox system entirely.
- **Test infrastructure**: `GameTestBase`, `InputHelper` (keyboard/action sim via GodotTestDriver), `UiHelper` (focus/window/pause queries)
- **Test suites** (all keyboard-only, all tied to `docs/flows/*.md` specs):
  - `SplashTests`, `ClassSelectTests`, `TownTests`, `PauseMenuTests`, `NpcTests`, `DeathTests`, `TransitionTests`, `DeathCinematicTests`
- **SoulsBorne "YOU DIED" cinematic** on player death — 6.3s dramatic sequence (overlay fade, red text appears, holds, fades, menu reveals). See `docs/flows/death.md`.
- **`docs/development-paradigm.md`** — public-facing doc on the AI+Human natural-language programming paradigm
- **Prominent README section** describing the paradigm on the GitHub front page
- **Resource leak monitoring** in DebugPanel: orphan node count, node count, modal count (via Godot's `Performance.Monitor`)
- **Auto mode** configured as default via `.claude/settings.local.json`
- Makefile targets: `test-ui`, `test-ui-suite`

#### Fixed
- **Screen transition flash bug** — Every caller using `ScreenTransition.Play()` to swap worlds now hides source content INSIDE the midpoint callback (when overlay is opaque), never before `Play()` starts. Prevents the new scene from briefly rendering under a translucent overlay. Applied to: ClassSelect, splash Continue, TeleportDialog, AscendDialog (3 buttons), FloorWipeDialog (2 buttons), DeathScreen respawn. Verified by `TransitionTests` asserting `OverlayAlpha ≥ 0.99` when new world enters tree.
- **DeathScreen respawn had no transition** — Now uses ScreenTransition so the dungeon→town swap is hidden behind fade-to-black.
- **Resource leaks at exit** — Orphaned Scroll nodes freed in GameWindow._ExitTree(), Theme and tab styles cached as singletons. RID leaks reduced dramatically at shutdown.

#### Changed
- `ScreenTransition.OverlayAlpha` public property added for test inspection
- `WindowStack.Count` and `TopTypeName` added for test assertions
- `UiTheme.CreateGameTheme()` + `CreateTabStyle()` cached as static singletons
- `GameWindow.Close()` only unpauses if `!WindowStack.HasModal` (fixes NPC→Shop race)
- `FocusFirstButton` skips buttons with `FocusMode.None`
- PauseMenu is now a programmatic GameWindow subclass (old `pause_menu.tscn` removed)

#### Removed
- `scripts/testing/AutoPilot*.cs` — replaced by GoDotTest infrastructure
- `scripts/sandbox/FullRunSandbox.cs` + `scenes/sandbox/FullRunSandbox.tscn`
- `scenes/pause_menu.tscn`
- Custom `KeyboardNav.HandleInput` method — replaced by Godot's built-in focus system

### Session 15 — Skills & Abilities Code Implementation (2026-04-16)

#### Added
- New data layer: `MasteryDef`, `AbilityDef`, `MasteryState`, `AbilityState`, `SkillAbilityDatabase` (130 registrations), `ProgressionTracker`
- `GameWindow.cs` — unified base class for all game windows
- `GameTabPanel.cs` — reusable Q/E tab switching system
- `AbilitiesDialog.cs` — class-specific abilities tab (Warrior Arts / Ranger Crafts / Arcane Spells)
- AbilitiesDialog node in `main.tscn`, Abilities button in `pause_menu.tscn`
- 72 new unit tests for data layer

#### Fixed
- NPC crash: `Npc.cs` called `Hide()` instead of `Close()`, corrupting WindowStack and freezing game
- Resource leaks at exit: orphaned Scroll nodes, unreleased GameTabPanel, uncached Theme/StyleBox resources
- Keyboard nav: replaced custom `KeyboardNav.HandleInput` with Godot's built-in focus system (`ui_up`/`ui_down` + `FollowFocus`)
- Focus warnings: `FocusFirstButton` now skips buttons with `FocusMode.None`

#### Changed
- `SkillTreeDialog.cs` — rewritten on GameWindow + GameTabPanel, shows masteries only
- `GameState.cs` — awards SP (2/lvl) + AP (3/lvl) on level-up
- `SaveData.cs` + `SaveSystem.cs` — new fields for masteries, abilities, use counts, category AP
- `Player.cs` — movement blocked when any modal window is open
- `ShopWindow.cs` — Buy/Sell merged into colored buttons, fixed panel resizing
- `UiTheme.cs` — added `CreateTabStyle`, made `CreateColoredButtonStyle` public
- All 13 ScrollContainers — `FollowFocus = true` for keyboard navigation

#### Removed
- Old skill system: `SkillDef.cs`, `SkillDatabase.cs`, `SkillState.cs`, `SkillTracker.cs`
- 671 lines of duplicate window boilerplate (all 14 windows migrated to GameWindow base)

#### Documentation
- `docs/reference/godot4-engine-reference.md` — comprehensive catalog of built-in engine systems and usage status

### Session 14 — Skills & Abilities System Complete (2026-04-15)

#### Added
- `docs/world/class-lore.md` — Class backstories and magic philosophy for Warrior, Ranger, Mage
- `docs/systems/point-economy.md` — SP/AP rates, sources, and total budget at key levels
- `docs/systems/synergy-bonuses.md` — Mastery threshold bonuses (Lv.5/10/25/50/100) with per-mastery procs
- `docs/systems/ability-affinity.md` — Cosmetic use-based milestones (100/500/1,000/5,000 uses)
- `assets/icons/abilities_icons.png` — Combined 512x1024 sprite sheet with 131 icons (all 3 classes + innate)
- `assets/icons/abilities_icons.json` — Icon atlas index for combined sheet

#### Changed
- `docs/systems/skills.md` — Complete rewrite: dual system (Skills + Abilities), all 3 class trees, SP/AP split
- `docs/systems/magic.md` — Mage: Arcane→Elemental, added Aether, Conduit→Attunement; Armor innate added
- `docs/systems/classes.md` — Updated mastery structure, SP/AP terminology
- `docs/ui/pause-menu-tabs.md` — 7→8 tabs, new Abilities tab (class-specific)
- `docs/systems/combat.md` — Skill Hotbar→Ability Hotbar, dual XP tracking
- `docs/systems/leveling.md` — SP/AP references replace old "skill points"
- `docs/ui/hud.md` — Ability cooldown overlays, status effect icons
- `docs/ui/controls.md` — Abilities terminology for hotbar
- `docs/flows/combat.md` — Ability activation flow
- `docs/flows/progression.md` — SP/AP allocation flows
- `docs/dev-tracker.md` — SPEC-13 tickets added
- `AGENTS.md` — New doc references
- `scripts/generate_icons.py` — Rewritten for combined abilities sheet
- Ranger ability renames: Steady Shot→Bead, Burst Fire→Spray, Guard→Hunker

### Session 13 — Skill & Spell Icon Sprite Sheets (2026-04-14)

#### Added
- `assets/icons/skills_icons.png` — 512x512 sprite sheet with 73 skill icons (Warrior + Ranger + Innate, 32x32 each)
- `assets/icons/spells_icons.png` — 512x512 sprite sheet with 45 spell icons (Mage Arcane + Conduit, 32x32 each)
- `assets/icons/skills_icons.json` — JSON atlas index for skill icons (name → x, y, w, h)
- `assets/icons/spells_icons.json` — JSON atlas index for spell icons (name → x, y, w, h)
- `scripts/generate_icons.py` — Pillow-based icon sprite sheet generator (re-runnable)
### Session 14 — Various Fixes & Visual Polish (2026-04-14)

#### Fixed
- Floor tile chaos: weighted variant selection (50/25/25%) instead of equal random
- SettingsPanel Godot warning: removed redundant `Size` set on FullRect-anchored controls (3 files)
- Enter key now works as confirm on saved character card and all buttons
- Pre-existing SmokeTests.cs missing `using System.Threading.Tasks`

#### Added (Projectile Sprites)
- 8-direction sprite sheets for all 9 projectile types (arrow, magic arrow, magic bolt, fireball, frost bolt, lightning, stone spike, energy blast, shadow bolt)
- `Projectile.cs` auto-detects sprite sheets and uses frame selection instead of rotation
- `MagicArrowProjectile` constant for Ranger elemental skills

#### Added (Effect Sprites)
- 18 animated effect sprite sheets (6 frames each, 64x64 per frame): fire, ice, poison pool, lava, shadow void, magic circle, heal aura, shield bubble, explosion, water puddle, torch, lightning strike, dust/debris, nether wisps, sparkle, poison cloud, cathedral light, volcanic ash
- Asset existence and dimension tests for effects and projectiles

#### Added (Settings & Controls)
- Rebindable keybindings in Settings > Controls tab with click-to-rebind UI
- Keybinding persistence in settings.json
- Derived skill chord display (auto-updates when base keys change)
- Reset to Defaults button
- `UiTheme.CreateHintBar()` reusable control hint component
- Control hints on Splash Screen and Pause Menu

#### Changed
- Deleted old single-frame projectile sprites (replaced by 8-direction sheets)
- Added `*.slnx` to `.gitignore`, deleted empty `DungeonGame.slnx`

### Session 12 — Fix & Expand Test Suite + Full-Run Integration Test (2026-04-12)

#### Added (Flow Docs + Testing Infrastructure)
- `docs/flows/` — 14 step-by-step flow docs covering every player interaction
- GodotTestDriver v3.1.66 integrated (Chickensoft allowed for testing only)
- `scripts/testing/DebugTelemetry.cs` — full audit trail (#if DEBUG): input, signals, state, scenes
- FullRunSandbox rewritten: 3-class walkthrough (Warrior, Ranger, Mage), death flow, achievement checks
- `docs/conventions/versioning.md` — SemVer + git tags strategy

#### Added (AutoPilot Player Emulation Library)
- `scripts/testing/AutoPilot.cs` — core player emulation node (step runner, logging, assertions)
- `scripts/testing/AutoPilotActions.cs` — input simulation: press/hold/release, movement, UI clicks, waiting
- `scripts/testing/AutoPilotAssertions.cs` — game state verification: HP, level, floor, inventory, enemies
- `docs/testing/autopilot.md` — spec for the AutoPilot library
- FullRunSandbox rewritten: now launches the real game and plays through it via AutoPilot
- Reusable for any sandbox or debug scenario — not tied to testing infrastructure

#### Added (Full-Run Integration Test)
- `docs/testing/full-run-test.md` — spec for the full-run integration test
- `tests/unit/FullRunTests.cs` — 13 tests: 10 phase tests + 3 class-parameterized init tests + 1 full session chain
- `scripts/sandbox/FullRunSandbox.cs` + `scenes/sandbox/FullRunSandbox.tscn` — Godot sandbox layer with all 10 phases
- `make sandbox-headless SCENE=full-run` — one-command full game smoke test
- Registered in SandboxLauncher under new "Integration" category

#### Fixed
- Unit and integration tests now compile and pass (added missing `using Xunit;` to all 5 test files)
- Excluded `SaveSystem.cs` from test project compilation (references Godot-specific `Autoloads` class)
- Added `RollForward` to test .csproj files for .NET 10 runtime compatibility

#### Added
- 199 new unit tests across 10 test files covering all untested pure-C# logic systems:
  - DungeonPacts (18 tests): rank management, heat calculation, reward scaling, pact effects, serialization
  - ZoneSaturation (19 tests): kill tracking, time decay, stat multipliers, reward bonuses, serialization
  - SkillBar (19 tests): slot assignment, cooldowns, activation, serialization
  - SkillState (14 tests): XP curves, level-up, skill points, passive bonuses, diminishing returns
  - AchievementSystem (15 tests): counters, evaluation, progress tracking, save/load
  - Crafting + AffixDatabase (16 tests): affix eligibility, application, recycling, display names
  - QuestSystem (10 tests): quest generation, kill/clear/depth progress, completion, save/load
  - DepthGearTiers (15 tests): floor gates, stat ranges, affix slots, quality rolls, tier lookup
  - LootTable (4 tests): gold drop formulas, level scaling
  - MagiculeAttunement (21 tests): floor clearing, node pathing, keystones, stat bonuses, serialization

### Session 11 — Endgame + Mana + Skills + UI Overhaul (2026-04-11)

#### Added
- Endgame systems: Zone Saturation, Dungeon Pacts (10 pacts), Dungeon Intelligence (adaptive AI), Magicule Attunement (40-node tree), Depth Gear Tiers (6 quality tiers)
- Mana system: Mana/MaxMana on GameState, class pools (M:200/R:100/W:60), INT regen, save/load
- Skill execution: all 80+ skills castable from hotbar with ManaCost/Cooldown/AttackConfig
- Skill bar HUD: 4 slots with shoulder+face combos (Q+W, Q+S, E+W, E+S), dynamic key labels
- HP/MP orbs: Diablo-style glass sphere sprites (PixelLab), fill/drain with ratio
- XP progress bar below skill bar with level-up gold glow and XP loss red flash
- Backpack window: 5-column slot grid (64x64), action menu (Use/Drop), from pause menu
- Settings panel: 4 tabbed categories (Gameplay/Display/Audio/Controls), persisted to JSON
- Tutorial: 4 tabbed sections (Movement/Combat/Menus/Town), static reference
- Debug console (F4): god mode, XP/gold/level cheats, teleport, kill all, perf metrics
- Character card: reusable component on title screen showing saved game sprite/stats
- Action menu: FF-style popup for context actions on items and skills
- 8 projectile sprites: arrow, magic bolt, fireball, frost bolt, lightning, stone spike, energy blast, shadow bolt
- Reusable UI components: GameWindow, TabBar, ScrollList, ContentSection
- WindowStack: central input routing, eliminates bleed-through permanently
- Back to Main Menu button on class select screen
- Camera shake on damage setting (off by default)

#### Fixed
- All UI panels block ALL input when open (not just movement)
- Sub-dialogs restore PauseMenu visibility and focus on close
- NpcPanel closeable with D/Escape (was missing)
- Mage auto-attack: staff melee is free, magic bolt requires mana via hotbar
- Monster spawn: guaranteed 10 per floor, floor wipe requires 10+ kills
- All scene loads use transition screen (town, dungeon, floor descent)
- Keyboard nav auto-scrolls ScrollContainer to focused button
- Skill targeting uses skill's range, not auto-attack range
- ClassSelect calls Reset() to initialize mana on new game
- GlobalTheme buttons use blue Action color (not gold Accent)
- Confirm button uses proper disabled style (not transparent modulate)
- Tutorial text autowraps within panel (no horizontal clipping)
- Section dividers at bottom (no double dividers)
- Removed fabricated difficulty setting (not in specs)

### Phase 1 Complete — All Systems Built (2026-04-11)

#### Added
- Skill system: 80+ skills across 3 classes, use-based XP + skill point allocation, SkillTreeDialog UI
- Bank system: 50 start slots, deposit/withdraw UI, expansion purchasing at 500*N^2
- Blacksmith crafting: deterministic affix system (28 affixes, tiers 1-4), equipment recycling
- Quest system: radiant quests (Kill/ClearFloor/DepthPush), 3 active quests, QuestPanel UI
- Achievement system (Dungeon Ledger): 30 achievements, counter-based tracking, DungeonLedger UI
- Teleporter NPC floor-select UI with zone labels
- Procedural floor generation: BSP rooms + Drunkard's Walk corridors + Cellular Automata smoothing
- Zone-based monster families: zone-exclusive species spawning per 10-floor block
- IDamageable interface for type-safe combat dispatch

#### Fixed
- Unsafe Random instance in DeathPenalty.cs → Random.Shared
- Silent save deserialization failures now log errors
- Save data validation prevents corrupted saves from creating impossible game states
- Duplicated stairs creation logic extracted into shared method

### Session 10 — Full Prototype Build (2026-04-11)

#### Added
- Playable dungeon crawler with full game loop (move, fight, level, die, restart)
- PixelLab-generated art: warrior, skeleton, goblin, 5 town NPCs, dungeon tiles, town tiles, stairs
- GameState + EventBus autoloads with reactive signals
- Level-based enemy system with full color gradient (grey→blue→cyan→green→yellow→gold→orange→red)
- Floor scaling formula: floor N = level N monsters, spawn range [N-1, N+2]
- Proc-gen floors with random room sizes and 4 floor tile variations
- Stairs up/down with physical collision + area trigger + sprite art
- Screen transition system (fade to black, message, fade in)
- Floating combat text (damage numbers, XP gains, level up)
- Flash effect system (damage, poison, curse, boost, shield, freeze, heal, crazed)
- Pause menu (Esc toggle, Resume/Quit)
- Death screen with Restart (R) and Quit (Esc) options
- HUD overlay (HP, XP, LVL, Floor) with reactive updates
- Spawn safety: 150px safe radius + 1.5s invincibility grace period
- Entity-to-entity collision (player↔enemies, enemy↔enemy)
- Directional sprites (8-way rotation for player and enemies)
- UiTheme shared palette + factory methods (DRY)
- GameSettings with ShowCombatNumbers toggle
- @art-lead agent for PixelLab art generation
- Asset READMEs for navigability

#### Fixed
- Signal leak on scene reload (autoload += without -= in _ExitTree)
- Diamond wall collision causing wiggle (switched to rectangular)
- Isometric movement confusion (switched to screen-space: up=up)
- Camera shake causing motion sickness (switched to red sprite flash)
- Esc not working on death screen (process_mode=ALWAYS)
- Enemies spawning outside room bounds (margin + despawn safeguard)
- "LEVEL UP" showing on floor descent instead of actual level ups

#### Docs
- docs/systems/floor-scaling.md — monster level formula
- docs/systems/spawn-safety.md — spawn safety rules
- docs/systems/accessibility.md — font, color, readability settings spec
- docs/dev-journal.md Session 10 entry

### Reset — Fresh Start (2026-04-10)

**All code, scenes, and tests deleted.** The pure C# game logic (480 tests passing) was correct, but the Godot rendering layer never worked visually. Starting over with visual-first development.

#### Deleted
- All Godot scene files (6 game scenes, 24+ test scenes)
- All C# scripts (autoloads, dungeon, player, ui, game systems, entity framework)
- All tests (480 unit tests, E2E test infrastructure, shell scripts)
- `archive/phaser-prototype/` (original Phaser 3 prototype)
- `addons/gut/` (GDScript testing addon)

#### Retained
- All documentation (80+ files across 11 directories)
- All assets (819+ sprites, tiles, fonts, icons)
- Config files (project.godot, DungeonGame.csproj, Makefile, AGENTS.md, CLAUDE.md)
- Input map (12 actions in project.godot)

#### Why
See `docs/dev-journal.md` Session 8 for the full post-mortem.

#### Docs Cleanup (2026-04-10)
- Updated all architecture docs to reframe as design blueprints (no code exists to describe)
- Rewrote `docs/dev-tracker.md` with new ticket structure (VIS-*, PROTO-*, CFG-*)
- Stripped Makefile of 50+ stale targets referencing deleted scenes/scripts
- Fixed `project.godot` (removed stale main scene and autoload references)
- Updated `AGENTS.md` current state, project structure, priorities
- Updated pre-commit hook from GDScript to C# formatting

### Previous [Unreleased]

### Added

- `docs/dev-tracker.md` — single-file development progress tracker with phased checklist
- `docs/architecture/setup-guide.md` — .NET SDK, Godot .NET edition, VS Code extension setup guide

### Changed

- **Language migration: GDScript → C# (.NET 8+)** — all docs, conventions, and tooling updated for C#
- `AGENTS.md` — overhauled: C# conventions (§4), tech stack with NuGet deps (§5), PascalCase naming (§6), new project structure (§7), dotnet tooling (§10)
- `CLAUDE.md` — added C# migration mode, setup guide reference
- `docs/architecture/tech-stack.md` — rewritten for C#/.NET 8+, added NuGet deps, platform support matrix, perf comparison
- `docs/architecture/project-structure.md` — updated directory tree for .csproj/.sln, PascalCase files, C# naming conventions
- `docs/dev-tracker.md` — updated all file extensions (.gd → .cs), test refs (GUT → GdUnit4), added Phase 0.2 C# migration checklist
- Stack additions: MessagePack-CSharp (binary floor cache), Microsoft.Extensions.ObjectPool (entity pooling), System.Threading.Channels (async generation), GdUnit4 + xUnit (testing)
- Autoloads open questions resolved: while-loop multi-level-up, player_damaged signal added, scripts over scenes, floor_number stays on GameState, save slot-aware interface

### Removed

- GDScript conventions, gdlint/gdformat references, GUT test framework references (replaced by C# equivalents)

### Previous Unreleased

- `docs/systems/skills.md` — full skill trees for all 3 classes (Warrior, Ranger, Mage) with hierarchical categories, hybrid leveling (use-based + point-based), infinite scaling
- `docs/systems/color-system.md` — unified cool→warm color gradient system for enemies, items, and zones (level-relative coloring)
- `docs/systems/player-engagement.md` — feedback loops, session pacing, juice/feel, retention hooks
- `docs/architecture/analytics.md` — opt-in telemetry, bug reporting, feedback system (offline-first)

### Changed

- `docs/systems/classes.md` — renamed Archer → Marksman → Ranger; added class-specific skill category names
- `docs/systems/skills.md` — added equipment slots, refined hybrid skill leveling (use-based + point-based)
- `docs/systems/leveling.md` — redesigned XP curve (linear-polynomial hybrid), added rested XP bonus, floor-scaling enemy XP, no level cap
- `docs/systems/combat.md` — updated class references (Archer → Ranger)
- `docs/inventory/items.md` — added equipment slot definitions and item tier coloring
- `docs/assets/sprite-specs.md` — added asset generation strategy and color gradient integration
- `docs/assets/ui-theme.md` — added color gradient palette entries
- `docs/world/monsters.md` — updated enemy color references to use unified color system
- `AGENTS.md` — added Skills, Color System, Player Engagement, Leveling to Game Design Quick Reference; updated Documentation Map with new docs

## [0.3.0] - 2026-03-23

### Added

- `project.godot` — Godot 4.6 project config (1920x1080, GL Compatibility, GUT plugin)
- `Makefile` — 11 automation targets for AI-driven terminal development (`make help`)
- `.github/workflows/ci.yml` — GitHub Actions CI: lint + test on push/PR to `main`
- `.githooks/pre-commit` — GDScript linting and formatting check on commit
- `.gitignore` — Godot, macOS, Python, IDE ignores
- `.editorconfig` — tabs for GDScript, spaces for YAML/Python
- `archive/phaser-prototype/.gdignore` — prevents Godot from importing archived Phaser files
- `addons/gut/` — GUT v9.6.0 test framework (vendored)
- `tests/test_project_setup.gd` — 4 sanity tests verifying project config
- `scripts/generate_tiles.py` — tile asset generator (from tile-specs.md)
- `assets/tiles/floor.png` and `wall.png` — generated isometric tile assets

### Changed

- `AGENTS.md` — added section 10 (Development Automation), updated project structure tree
- `docs/conventions/ai-workflow.md` — replaced Open Questions with Automation section (moved from architecture/)
- `docs/systems/stats.md` — closed 4 open questions (hybrid allocation, soft diminishing returns, no caps, fixed backpack)
- `docs/systems/classes.md` — closed 4 open questions (scaling bonuses, design-now-build-later skills, class-locked skills, class-locked gear)
- `docs/systems/leveling.md` — closed 2 open questions (linear-polynomial hybrid XP, no level cap)
- `docs/systems/death.md` — closed 2 open questions (locked-in formulas, MVP scope)
- `docs/overview.md` — closed 2 open questions (all procedural floors, multiple save slots)
- `docs/ui/death-screen.md` — closed 2 open questions (no inventory shown, instant death screen)
- `docs/assets/tile-specs.md` — added Licensing section (CC0/CC-BY 3.0/CC-BY 4.0)
- `docs/assets/sprite-specs.md` — added Licensing section
- `assets/ATTRIBUTION.md` — created attribution tracking template
- `README.md` — updated title formatting

## [0.2.0] - 2026-03-23

### Changed

- **Engine migration:** Phaser 3 (browser) → Godot 4 (desktop native)
- **Perspective:** Top-down 2D → Isometric 2D (2:1 diamond tiles, 64×32)
- **Language:** Vanilla JavaScript → GDScript
- **Architecture:** Single-file monolith → Scene/node project structure
- **Platform:** Browser → Desktop native (macOS primary)
- **Persistence:** localStorage → FileAccess + user:// + JSON
- **State management:** Global JS object → Autoload singletons (GameState, EventBus)
- **UI:** HTML/CSS overlay → Godot Control nodes in CanvasLayer

### Added

- Comprehensive game design documentation in `docs/` (25+ design documents)
- Architecture docs: tech stack, Godot basics, project structure, scene tree, autoloads, signals
- Object specs: player, enemies, tilemap, effects — every property and method documented
- Asset specs: tile dimensions, sprite shapes, UI color palette
- System docs: isometric movement, enemy spawning, camera behavior
- Testing docs: test strategy, 33 manual test cases, automated test plan (GUT framework)
- `archive/phaser-prototype/` — preserved original Phaser code for reference

### Removed

- `package.json`, `bunfig.toml`, `node_modules/` — Bun/Node dependencies no longer needed
- Phaser-specific architecture docs moved to archive (phaser-basics, single-file, code-map)

## [0.1.0] - 2026-03-19

### Added

- Phaser 3 game engine setup via CDN (v3.90.0) with arcade physics
- Responsive canvas layout using Phaser.Scale.FIT + CENTER_BOTH
- Player character rendered as a colored circle with physics body
- Movement via WASD, arrow keys, and pointer/touch drag
- Auto-targeting combat: attacks nearest enemy within range on cooldown
- Slash visual effect using tweened rectangles
- Enemy spawning from screen edges with three danger tiers (green/yellow/red)
- Enemy AI: chase player using physics.moveToObject
- Enemy respawn timer (2.8s loop, soft cap at 14 active enemies)
- Player damage on enemy overlap with hit cooldown and camera shake
- XP gain on enemy defeat, scaling by danger tier
- Level-up system: XP threshold increases per level, HP boost on level-up
- HUD overlay displaying HP, XP, level, and floor number
- Death screen with restart via R key or tap
- Mobile-responsive layout with safe-area inset support
- Mobile touch note displayed on small screens
- Dark fantasy UI theme with CSS custom properties
- Bun dev server configuration (port 8000)
- Project documentation: README.md, AGENTS.md
