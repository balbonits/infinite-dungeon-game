# Development Tracker

Current state of the game. Updated as work completes. Run `make build` to verify the project compiles.

## How to Read This

- **Done** = implemented and tested (or verified visually)
- **Partial** = exists but incomplete or has known gaps
- **To Do** = not started
- **Priority**: P0 = blocking, P1 = next up, P2 = planned, P3 = future

---

## Phase 0 -- Visual Foundation (ALL DONE)

| ID | Title | Status |
|----|-------|--------|
| VIS-01 | Render one floor tile correctly | Done |
| VIS-02 | Render a tile room (10x10) | Done |
| VIS-03 | Place a character sprite on the tiles | Done |
| VIS-04 | Move the character with arrow keys | Done |
| VIS-05 | Camera follows the character | Done |
| VIS-06 | Place one enemy sprite that chases | Done |

---

## Phase 0.5 -- Playable Prototype (ALL DONE)

| ID | Title | Status |
|----|-------|--------|
| PROTO-01 | Auto-attack system | Done |
| PROTO-02 | Enemy damage and death | Done |
| PROTO-03 | Enemy spawning and respawning | Done |
| PROTO-04 | HUD overlay (HP, level, floor) | Done |
| PROTO-05 | Player death and restart | Done |
| PROTO-06 | GameState autoload and signals | Done |

---

## Phase 1 -- Complete Systems

| ID | Title | Status | Notes |
|----|-------|--------|-------|
| SYS-01 | Character class selection (Warrior/Ranger/Mage) | Done | ClassSelect screen with stat cards, skill previews, keyboard/mouse nav. Warrior=melee slash, Ranger=arrow projectile, Mage=magic bolt+staff fallback. |
| SYS-02 | Skill definitions + use-based leveling | Done | Full skill tree: 80+ skills across 3 classes, SkillDatabase, SkillTracker, use-based XP + skill points, passive bonuses, SkillTreeDialog UI in pause menu. |
| SYS-03 | Death penalty flow (XP loss, item loss, buyout) | Done | Multi-step flow: XP loss, item loss chance, gold buyout option, Sacrificial Idol prevention. |
| SYS-04 | Quest system (Adventure Guild radiant quests) | Done | QuestTracker with Kill/ClearFloor/DepthPush types, 3 active quests, scaling rewards, QuestPanel UI at Guild Master NPC, save/load. |
| SYS-05 | Level Teleporter NPC | Done | TeleportDialog with floor selection, zone labels, wired to Teleporter NPC service button. |
| SYS-06 | Blacksmith crafting UI | Done | BlacksmithWindow with Craft/Recycle tabs, AffixDatabase (28 affixes, tiers 1-4), deterministic affix application, gear recycling for gold. |
| SYS-07 | Bank UI | Done | BankWindow with deposit/withdraw, 50 start slots, expansion purchasing (500*N^2), persistent save/load. |
| SYS-08 | Items & affix system | Done | AffixDef/AffixDatabase, CraftableItem model, Crafting logic (max 3 prefix + 3 suffix), BaseQuality enum. Unique items catalog pending. |
| SYS-09 | Achievement system (Dungeon Ledger) | Done | AchievementTracker with 30 achievements across 5 categories, counter-based tracking, gold/title rewards, DungeonLedger UI in pause menu. |
| SYS-10 | Monster families (zone-exclusive creature sets) | Done | Constants.Zones with 10-floor zone system, zone-gated species spawning. Zone 1: Skeleton+Bat, Zone 2: Goblin+Wolf, Zone 3: Orc+Spider, Zone 4: DarkMage mix, Zone 5+: all. |

---

## Systems Built Beyond Original Tracker

These systems were implemented during visual-first development but were not part of the original ticket plan.

### Game Flow
| System | Scripts | Status |
|--------|---------|--------|
| Splash screen | `SplashScreen.cs` | Done -- title, character card for saved game, New Game/Tutorial/Settings/Exit |
| Class selection | `ClassSelect.cs` | Done -- 3 class cards, Up/Down zone nav (cards→confirm→back), keyboard+mouse |
| Town hub | `Town.cs` | Done -- 16x12 tile room, 5 NPCs, dungeon entrance |
| World swapping | `Main.cs` | Done -- `LoadTown()`/`LoadDungeon()` swap world scenes dynamically |
| Screen transitions | `ScreenTransition.cs` | Done -- fade-to-black with message, used for floor descent and town/dungeon travel |

### NPC & Shop
| System | Scripts | Status |
|--------|---------|--------|
| NPC interaction | `Npc.cs`, `NpcPanel.cs` | Done -- proximity detect, "[S] Interact" prompt, service menu with greeting |
| Shop system | `ShopWindow.cs`, `ItemDatabase.cs`, `Inventory.cs`, `Item.cs` | Done -- buy/sell tabs, gold tracking, item descriptions, 8 starter items |
| Dialogue system | `DialogueBox.cs` | Done -- typewriter effect, portraits, advance with S/Space/Enter |
| Toast notifications | `Toast.cs` | Done -- stacking bottom-center toasts, auto-dismiss, info/success/warning/error |

### Dungeon & Combat
| System | Scripts | Status |
|--------|---------|--------|
| Proc-gen floors | `Dungeon.cs` | Done -- random room size (18-30), 4 floor tile variations, wall border |
| Floor scaling | `Constants.FloorScaling` | Done -- room growth per 5 floors, enemy level range per floor |
| Spawn safety | `Dungeon.SpawnInitialEnemies()` | Done -- guaranteed 10 enemies per floor, fallback to any floor tile, floor wipe requires 10+ kills |
| Ascend dialog | `AscendDialog.cs` | Done -- stairs-up triggers: return to town, go up 1 floor, select specific floor |
| Unified combat | `AttackConfig.cs`, `ClassAttacks.cs` | Done -- data-driven melee/projectile, Mage auto=staff melee (magic bolt via hotbar) |
| Projectile system | `Projectile.cs` | Done -- 8 projectile sprites (arrow, magic bolt, fireball, frost bolt, lightning, stone spike, energy blast, shadow bolt) |
| Directional sprites | `DirectionalSprite.cs` | Done -- 8-direction rotation loading and velocity-based switching |
| Flash effects | `FlashFx.cs` | Done -- damage, poison, curse, boost, shield, freeze, heal, crazed flash types |
| Floating text | `FloatingText.cs` | Done -- combat damage, heals, XP, level-up numbers that rise and fade |
| Level-gap enemy colors | `Enemy.cs` | Done -- 8-anchor gradient (grey to red) based on enemy-vs-player level gap |

### UI & Debug
| System | Scripts | Status |
|--------|---------|--------|
| Debug panel | `DebugPanel.cs` | Done -- F3 toggle, shows HP/level/XP/floor/damage/enemies/kills/session time |
| Debug console | `DebugConsole.cs` | Done -- F4 toggle, god mode, XP/gold/level cheats, teleport, kill all, give items, perf metrics |
| Pause menu | `PauseMenu.cs` | Done -- Esc toggle, Resume/Backpack/Stats/Skills/Ledger/Tutorial/Settings/Main Menu/Quit |
| HUD | `Hud.cs` | Done -- Diablo-style HP/MP orbs (bottom-left/right), XP bar below skill bar, stats panel top-left |
| Skill bar HUD | `SkillBarHud.cs` | Done -- 4 skill slots (Q+W/Q+S/E+W/E+S), controller-aware labels, cooldown overlays |
| XP bar | `XpBar.cs` | Done -- progress bar below skill bar, level-up gold glow, XP loss red flash |
| HP/MP orbs | `OrbDisplay.cs` | Done -- PixelLab glass sphere sprites, colored fill rises/falls with ratio |
| Backpack window | `BackpackWindow.cs` | Done -- 5-column slot grid (64x64), action menu (Use/Drop), item detail on focus |
| Action menu | `ActionMenu.cs` | Done -- FF-style popup for item/skill context actions, keyboard nav |
| Tutorial | `TutorialPanel.cs` | Done -- 4 tabbed sections (Movement/Combat/Menus/Town), static reference |
| Settings panel | `SettingsPanel.cs` | Done -- 4 tabbed categories (Gameplay/Display/Audio/Controls), saved to disk |
| Character card | `CharacterCard.cs` | Done -- reusable PanelContainer with sprite/stats/level, used on title screen |
| Death screen | `DeathScreen.cs` | Done -- R to restart (loads town), Esc to quit, button variants |

### Save/Load & Persistence
| System | Scripts | Status |
|--------|---------|--------|
| Save/load system | `SaveData`, `SaveSystem`, `SaveManager` | Done -- auto-save on transitions, Continue from title screen |

### Stats & Progression
| System | Scripts | Status |
|--------|---------|--------|
| Stat system | `Constants.cs` (stat formulas) | Done -- STR/DEX/STA/INT with diminishing returns, class bonuses per level, free points |
| Stat allocation UI | Pause menu dialog | Done -- allocate free stat points from pause menu |
| HP regen | STA stat | Done -- passive HP regen derived from STA |
| Death penalty flow | Multi-step in `Player.cs` | Done -- XP loss, item loss, gold buyout, Sacrificial Idol |

### Loot & Economy
| System | Scripts | Status |
|--------|---------|--------|
| Loot drops | Enemy death drops | Done -- gold + item chance per enemy kill |
| Floor wipe mechanic | `Dungeon.cs` | Done -- bonus rewards when all enemies killed on a floor |
| Item system | `ItemDef`, `ItemDatabase`, `Inventory` | Done -- full item definitions with stats, descriptions, rarity |

### Navigation & Combat
| System | Scripts | Status |
|--------|---------|--------|
| Stairs compass | `StairsCompass.cs` | Done -- gold/green arrows on screen edge, auto-hide when visible |
| Hitscan projectile system | `Projectile.cs` | Done -- instant damage + cosmetic tracer, replaced physics-based collision |
| Targeting system | `AttackConfig.cs`, `ClassAttacks.cs` | Done -- 8 targeting modes + 3 projectile behaviors |
| Per-species collision | `SpeciesConfig`, `SpeciesDatabase` | Done -- unique hitbox per enemy species |
| 7 enemy species | `Enemy.cs` | Done -- Skeleton, Goblin, Bat, Wolf, Orc, Dark Mage, Spider |
| Stairs exclusion zone | `Dungeon.cs` | Done -- enemies can't spawn within 150px of staircases |

### Architecture
| System | Scripts | Status |
|--------|---------|--------|
| Constants architecture | `Constants.cs` | Done -- all magic numbers centralized: PlayerStats, EnemyStats, Spawning, FloorScaling, Leveling, Tiles, Effects, Assets, Layers, Groups, InputActions, ClassCombat, Town, Sprite |
| Strings architecture | `Strings.cs` | Done -- all player-facing strings centralized for future i18n |
| Global theme | `GlobalTheme.cs`, `UiTheme.cs` | Done -- blue buttons, gold headings, semantic color system, slot styles |
| Game settings | `GameSettings.cs` | Done -- 20+ settings across Gameplay/Display/Audio/Controls, persisted to JSON |
| GC during loading | `ScreenTransition.cs` | Done -- forced GC during loading screen transitions |
| Mana system | `GameState.cs` | Done -- Mana/MaxMana with signals, class base pools, INT regen, save/load |
| Skill execution | `SkillDef.cs`, `SkillBar.cs`, `Player.ExecuteSkill()` | Done -- 80+ skills with ManaCost/Cooldown/AttackConfig, hotbar casting |
| Reusable UI components | `GameWindow.cs`, `TabBar.cs`, `ScrollList.cs`, `ContentSection.cs` | Done -- base window, tab system, scroll nav, content helpers |
| Window stack | `WindowStack.cs`, `KeyboardNav.BlockIfNotTopmost()` | Done -- central input routing, eliminates bleed-through |

---

## P2 -- Endgame & Polish

| ID | Title | Status | Notes |
|----|-------|--------|-------|
| END-01 | Dungeon Pacts (voluntary difficulty modifiers) | Done | `DungeonPacts.cs` — 10 pacts, heat-based rewards, enemy stat multipliers, save/load |
| END-02 | Magicule Attunement (post-cap passive tree) | Done | `MagiculeAttunement.cs` — 40-node tree, 3 rings, 4 branches, keystones, floor tracking |
| END-03 | Dungeon Intelligence (adaptive AI Director) | Done | `DungeonIntelligence.cs` — 4 metrics, pressure score, spawn/aggro/elite modifiers |
| END-04 | Zone Saturation (per-zone difficulty dial) | Done | `ZoneSaturation.cs` — per-zone saturation, decay, stat/reward multipliers |
| END-05 | Depth gear tiers (new rarity at floor 50/100/150) | Done | `DepthGearTier.cs` — BaseQuality extended to 6 tiers, quality roll with floor shift |
| UI-01 | Pause menu tabbed redesign | Done | 8 tabs: Inventory, Equip, Skills, Abilities*, Quests, Ledger, Stats, System. Q/E cycles tabs. PauseMenu rewritten as programmatic `GameWindow` + `GameTabPanel`. Equipment tab wired to real EquipmentSet in SYS-11. |
| UI-02 | Load Game screen + 3-slot save system | Done | Multi-slot `SaveManager` (3 slots at `user://saves/save_{0..2}.json`), injectable `ISaveStorage`, `GameState.CurrentSaveSlot`. Splash screen has a "Continue" button (above "New Game", disabled when no saves exist) that opens `LoadGameScreen`. Three cards (populated `CharacterCard` or "Empty Slot" panel); populated cards overlay a red X button wired to `DeleteConfirmDialog`. New Game with all 3 slots full → Toast error per spec. Keyboard nav: Left/Right cycle cards, Down → Load button, further Down → Back button, Delete key triggers slot-delete on focused card. 446 unit tests green. Follow-up: add `LoadGameTests` GoDotTest suite once TEST-09 cross-suite isolation lands. |
| ART-01 | Splash screen background image | Done | `assets/ui/splash_background.png` (1920x1080, ~456KB). PixelLab generated 4 hero sprites (archway, rubble, pillar, rune-glyph — rune unused as literal 'T' letter was off-brief; replaced with procedural radial glow) composited via Pillow with vertical gradient, tiled dungeon floor texture, interior-masked warm glow through arch, rim light, bloom spill, atmospheric fog, particles, and vignette. Style matches dungeon-tileset palette. Not yet wired into `SplashScreen.cs` — awaiting visual review. |
| SYS-11 | Equipment system (10-slot, 19 equippable) | Done | `EquipmentSet.cs` (pure logic, 10 slots + 10 ring sub-slots, HasQuiver check, class-affinity 1.25× bonus, capture/restore). `EquipSlot` enum + `Slot`/`ClassAffinity` fields on `ItemDef`. `GameState.Equipment` with class-based starting gear in `Reset()`. `SavedEquipment` + full save/load round-trip. Equipment tab UI in `PauseMenu` with slot rows, equip-picker action menu, and live bonus summary. `Player.GetEffectivePrimary()` swaps Ranger bow→melee bash when no quiver equipped (`ClassAttacks.RangerBowBash`). 433 unit + 11 integration tests green, 24 new `EquipmentSetTests`. Placeholder items in `ItemDatabase.cs` will be replaced by ITEM-01 catalog wiring. |
| SYS-12 | Bank & Backpack Redesign (Guild window) | Done | Milestones 1 (spec lock) + 2a–2g + equipment-on-death closure: model rewrite (Inventory/Bank `long` gold, one-type-per-slot, Lock flag, Transfer/Drop), reusable `SlotGrid` + `NumberFormat`, new `GuildWindow` (3 tabs), refreshed `BackpackWindow` with Drop/Lock, 5-option sacrifice death dialog, Guild Maid with dedicated ART-02 sprite. Equipment-on-death wired: `DeathScreen` calls `EquipmentSet.DestroyRandomEquipped()` on Save Backpack / Accept Fate / Quit paths; loss surfaced via toast. Buyout costs (from `DeathPenalty.cs`): equipment `deepestFloor × 25`, backpack `deepestFloor × 60`, both = sum. Sacrificial Idol = free Save Both. 433 unit + 11 integration tests green. |
| ART-02 | Guild Maid NPC sprite | Done | `assets/characters/npcs/guild_maid/` — 8-directional, 92×92, female maid with glasses, dark blue dress, white apron, ledger. Wired into `Town.cs` NPC roster. |
| POL-01 | Audio system (SFX + music + ambient) | To Do | Skipped per user — no audio/sound tasks |
| POL-02 | Real sprite animations (AnimatedSprite2D) | To Do | 8-directional static rotations done; animation frames not yet |
| POL-03 | Zone visual themes (per-zone floor/wall textures) | Done | `Constants.Assets.GetZoneTheme()` — 5 themes cycling for zone 6+ |
| POL-04 | Shader effects (hit flash, outline, glow) | Partial | FlashFx uses Modulate tinting; no custom shaders yet |
| SYS-13 | Dungeon Regurgitation | Draft-spec | P2 | Per-save-slot graveyard pool: items destroyed at death can re-surface as monster drops or chest contents at or below the floor they were lost on. Brutal-low rate (0.5% mob / 3% chest), celebrated "overleveled trash drop" comedy. Spec: `docs/systems/dungeon-regurgitation.md`. |

---

## Skills & Abilities Spec Updates

| ID | Title | Status | Notes |
|----|-------|--------|-------|
| SPEC-13a | Rewrite skills.md → Skills & Abilities spec | Done | Full rewrite: dual-system (Skills + Abilities), 3 class trees, SP/AP split, reactive/pull architecture |
| SPEC-13b | Update magic.md (Elemental/Aether/Attunement) | Done | Mage: Arcane→Elemental, added Aether, Conduit→Attunement. Ranger: Arms→Weaponry, Instinct→Survival. Warrior: Inner→Discipline, Outer→Intimidation. Added Armor innate. |
| SPEC-13c | Update classes.md (new mastery structure) | Done | Updated all 3 class summaries, Innate 3→4, SP/AP terminology |
| SPEC-13d | Update pause-menu-tabs.md (7→8 tabs) | Done | Added class-specific Abilities tab (Warrior Arts / Ranger Crafts / Arcane Spells) |
| SPEC-13e | Update combat.md (ability activation flow) | Done | Skill Hotbar→Ability Hotbar, dual XP tracking, updated flows/combat.md too |
| SPEC-13f | Update hud.md (cooldowns, status effects) | Done | Ability cooldown overlay, Armor status effect, skill bar→ability bar |
| SPEC-13g | Review controls.md (terminology pass) | Done | Skills→abilities for hotbar refs, updated tab list |
| SPEC-13i | Create point-economy.md (SP/AP rates) | Done | New doc: SP 2/level, AP 3/level + milestones + combat + use-based. Updated leveling.md |
| SPEC-13k | Archive working doc + cross-reference sweep | Done | SKILLS_AND_ABILITIES_SYSTEMS.md archived, stale terminology fixed in progression.md, items.md |

---

## Testing Infrastructure

| ID | Title | Status | Priority | Notes |
|----|-------|--------|----------|-------|
| TEST-01 | ISaveStorage interface + file I/O abstraction | Done | `ISaveStorage` interface with `GodotFileSaveStorage` (prod) and `FakeSaveStorage` (tests) implementations. `SaveManager.Storage` is injectable for testing. `SaveDataJson` helper extracted from `SaveSystem` so the pure JSON codec is unit-testable without pulling the Godot-coupled `GameState` autoload into the test project. |
| TEST-02 | Save/load round-trip unit tests | Done | 13 `SaveSystemTests` covering `FakeSaveStorage` semantics, `SaveData` JSON round-trip for all populated fields including new `SavedEquipment`, item stacks with `long` counts, corrupt-JSON handling, and multi-slot independence. 446 unit tests green. |
| TEST-03 | Seed file persistence tests | To Do | P3 | When game seeds are persisted to disk for replay/sharing, add tests that write seed → read seed → generate same floor. Uses same `ISaveStorage` abstraction from TEST-01. |
| TEST-04 | E2E scene-runner tests (GdUnit4) | To Do | P2 | Wire up scaffolded `tests/e2e/` tests to actually run scene transitions via GdUnit4 `ISceneRunner`. Requires Godot binary in CI. |
| TEST-05 | CI screenshot capture on E2E failure | To Do | P3 | `GetViewport().GetTexture().GetImage().SavePng()` in GdUnit4 tests, uploaded as CI artifacts on failure. |
| TEST-06 | Integrate GodotTestDriver (Chickensoft) | Done | P1 | `Chickensoft.GodotTestDriver` v3.1.66 wraps keyboard/action input simulation. Used by `InputHelper`. |
| TEST-07 | Create game-specific test drivers | To Do | P2 | Optional convenience — write typed drivers (PauseMenuDriver, ShopDriver, NpcDriver) that wrap keyboard flows. Current `InputHelper` + `UiHelper` already cover the bases. |
| TEST-08 | GoDotTest + keyboard-nav test suite | Done | P1 | Added `Chickensoft.GoDotTest` v2.0.28. `Main.cs` boots tests via `--run-tests`. Test suites in `scripts/testing/tests/`: Splash, ClassSelect, Town, PauseMenu, Npc, Death, Transition, DeathCinematic — all use keyboard-only input, all verify specs in `docs/flows/*.md`. Run via `make test-ui`. |
| TEST-09 | Cross-suite state isolation | Done (helper shipped; per-suite adoption gradual) | P1 | `GameTestBase.ResetToFreshSplash()` helper added — each suite can call it in `[Setup]` to guarantee a fresh GameState + splash screen. Resets `GameState.Instance`, reloads the current scene (re-runs Main._Ready), awaits splash re-appearance. Existing suites will adopt gradually as they start failing; new suites should call it up front. |
| TEST-10 | CI integration for make test-ui | Done | P2 | New `ui-tests` job in `.github/workflows/ci.yml`. Runs on every PR and push (not main-push-only, unlike the existing e2e-tests job) so keyboard-nav regressions fail CI at PR time. Uses `chickensoft-games/setup-godot@v2` for Godot 4.3. Greps the output for `✗` failure marker; uploads log as artifact on every run. |

---

## Content Catalogs

| ID | Title | Status | Priority | Notes |
|----|-------|--------|----------|-------|
| ITEM-01 | Base item catalog spec + ItemDatabase expansion | Done | Full 259-item base catalog implemented in `ItemDatabase.cs` per `docs/inventory/item-catalog.md`: 75 armor + 40 main-hand + 30 off-hand + 9 quivers + 15 neck + 40 ring + 28 consumables + 22 materials. Class-voice naming (Warrior boastful, Ranger dry, Mage verbose). Unified metal ladder (Iron/Steel/Mithril/Orichalcum/Dragonite). `ItemDef` gained `Tier` field (1..5 for gear, 0 for untiered). Starting-gear `GameState.EquipStartingGear` now pulls tier-1 catalog items. Legacy IDs (`sword_iron`, `potion_hp_small`, `idol_sacrificial`, etc.) fully migrated across tests + sandboxes. 446 unit + 11 integration tests green. Combat-relevant ring focuses (crit/haste/dodge/block) ship as stat-zero — will be activated by COMBAT-01. |
| ITEM-02 | Monster drop tables spec + wiring | Draft-spec | P1 | Per-species drop tables with signature material and thematic "Preferred Slots" identity (currently uniform weighting; 2× weighting reserved for when species variety per zone expands). Spec: `docs/systems/monster-drops.md`. Implementation replaces `LootTable.cs` with `MonsterDropTables`. Depends on ITEM-01 for the base-item IDs. |
| FORGE-01 | Blacksmith Forge RNG system (spec + impl) | Needs-spec | P2 | Player-owned base item + large material investment rolls against a curated unique-item table gated by base tier and ilvl. Produces a named item with a hand-designed affix package. Replaces the traditional "unique drops from bosses" model. **Spec authoring pending** — open questions: material cost curve, one-shot vs re-rollable, stored as fixed seed / named template / procedural, stat override vs overlay, floor-depth / gold / quest gating. |
| COMBAT-01 | Equipment stats into combat formulas | Needs-spec | P1 | `EquipmentSet.GetTotalBonuses()` is computed and displayed (SYS-11) but never fed into `Player.ExecuteAttack`, `GameState.MaxHp` recalculation, or any other formula. Players can equip Mega Armor and feel zero effect. Spec must cover: which formulas merge bonuses (damage, MaxHp via STA, MaxMana via INT, attack speed, HP regen, mana regen), cache vs per-attack recompute, and integration for new ring focuses (Crit/Haste/Evasion/Bulwark from catalog Q6). **Blocks SYS-11 feeling real in-game.** |
| COMBAT-02 | Weapon archetype cross-class attack dispatch | Needs-spec | P2 | Catalog Q3:A says "weapon archetype dictates attack mechanic, not wielder class." Currently only `Ranger + no quiver → bow-bash` is wired (SYS-11). Full rule: Mage with crossbow fires bolts, Warrior with staff fires spells, etc. Needs spec covering: weapon→attack dispatch resolution point (dynamic per attack vs cached on equip change), fallback when no weapon equipped, archetype→AttackConfig mapping table. |
| ART-03 | Catalog armor sprites (Head/Body/Arms/Legs/Feet × 5 tiers × 3 classes) | To Do | P2 | 75 armor sprites total. Class-voice visual differentiation (Warrior plate vs Ranger leather vs Mage robes). PixelLab batch generation per art-lead. Blocks ITEM-01 visual polish but not core ITEM-01 data wiring. |
| ART-04 | Catalog weapon sprites (Main Hand × 40 + Off Hand × 30) | To Do | P2 | 70 weapon/offhand sprites. Swords/axes/hammers (Warrior), shortbow/longbow/crossbow (Ranger), staves/wands (Mage), shields/defensive-melee/spellbooks. |
| ART-05 | Catalog quiver sprites (9 imbue types) | To Do | P2 | 9 quiver sprites (Basic/Hot/Cold/Heavy/Nasty/Zap/Quiet/Sharp/Bright). Element-coded visuals. |
| ART-06 | Catalog accessory sprites (Neck × 15 + Ring × 40) | To Do | P3 | 55 small-icon sprites. Metal ladder visual cues (Iron/Steel/Mithril/Orichalcum/Dragonite). Likely smallest art lift — icon-only, no directional rotations. |

---

## P3 -- Future

| ID | Title | Status | Notes |
|----|-------|--------|-------|
| FUT-01 | Gamepad support | To Do | Input Map actions defined; no joypad events added |
| FUT-02 | Key rebinding UI | To Do | |
| FUT-03 | Desktop export (macOS/Windows/Linux) | To Do | |
| FUT-04 | Monster trophy crafting | To Do | |
| FUT-05 | Evolving items (grow through use) | To Do | |
| FUT-06 | Synergy ring system (10 ring slots) | To Do | |
| FUT-07 | Dungeon Weather (daily magicule fluctuation) | To Do | |

---

## Test Summary

| Suite | Command | Notes |
|-------|---------|-------|
| Unit tests (xUnit) | `make test-unit` | Run `make test-unit` for current count |
| Integration tests (xUnit) | `make test-integration` | Cross-system flow tests |
| Full-run integration | `make sandbox-headless SCENE=full-run` | End-to-end play session |
| Sandbox scenes (14) | `make sandbox-headless-all` | Headless checks per system |
| E2E (GdUnit4) | `make test-gdunit` | Scaffolded, pending wiring (TEST-04) |

---

## File Counts

| Category | Count | Location |
|----------|-------|----------|
| Scene files (.tscn) | 8 | `scenes/` |
| C# scripts | 87 | `scripts/` (includes autoloads/, ui/, logic/) |
| Input actions | 12 | `project.godot` [input] section |
| Autoloads | 3 | GameState, EventBus, SaveManager |
| Item definitions | 8 | `ItemDatabase.cs` |
| Enemy species | 7 | Skeleton, Goblin, Bat, Wolf, Orc, Dark Mage, Spider |
| Player classes | 3 | Warrior, Ranger, Mage |
| Projectile sprites | 8 | `assets/projectiles/` |
| UI orb assets | 2 | `assets/ui/` (HP red, MP blue) |
| Zone tilesets | 7 | `assets/tiles/` |

---

## Milestone Targets

| Milestone | What | Tickets |
|-----------|------|---------|
| **Visual Foundation** | One tile, one sprite, one room, movement, camera | VIS-01 through VIS-06 -- ALL DONE |
| **Playable Prototype** | Combat, HUD, death, game loop, state management | PROTO-01 through PROTO-06 -- ALL DONE |
| **Feature Complete** | All NPCs, classes, skills, death flow, quests | SYS-01 through SYS-10 |
| **Endgame** | Pacts, attunement, adaptive dungeon, gear tiers | END-01 through END-05 |
| **Polish** | Audio, animations, shaders, zone themes | POL-01 through POL-04 |
| **Ship** | Export, gamepad, key rebinding | FUT-01 through FUT-03 |
