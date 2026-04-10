# Development Tracker

Current state of the game. Updated as work completes. Run `make test` for test count, `make test-game` to play.

## How to Read This

- **Done** = implemented and tested (or tested via game loop)
- **Partial** = code exists but has known bugs or isn't integrated
- **To Do** = not started
- **Priority**: P0 = blocking, P1 = next up, P2 = planned, P3 = future

---

## What's Built (Done)

### Core Game Loop ✓
The full loop works: MainMenu → CharacterCreate → Town → Shop → Dungeon → Combat → Floor Transitions → Save/Load → Death/Respawn → Back to Town.

| System | Files | Tests |
|--------|-------|-------|
| GameState + GameSystems | `GameCore.cs` | CombatTests, LevelingTests, InventoryTests, ShopTests |
| Entity framework (9 systems) | `systems/*.cs`, `entities/*.cs`, `effects/*.cs` | StatSystem, VitalSystem, CombatSystem, EffectSystem, ProgressionSystem, SkillSystem tests |
| Dungeon generation (BSP + corridors + CA) | `dungeon/*.cs` | DungeonGeneratorTests (connectivity, sizing, room types) |
| Progressive floor sizing (50x100 → 150x300) | `DungeonGenerator.cs` | 7 sizing tests |
| IKEA guided layout + challenge rooms | `DungeonGenerator.cs` | Chain, challenge, boss-exit tests |
| Floor cache (10-floor LRU) | `FloorCache.cs` | Cache tests |
| Exploration / fog of war | `DungeonData.cs` | 9 exploration tests |
| Save/Load (JSON, slot-based) | `SaveSystem.cs`, `SaveFileIO.cs` | 29 save round-trip tests |
| Bank storage (50 slots, expandable) | `BankSystem.cs`, `BankData.cs` | 15 bank tests |
| Backpack expansion (25 base, +5 per) | `BackpackSystem.cs` | 9 backpack tests |
| Item generation (quality, loot drops) | `ItemGenerator.cs`, `AffixData.cs` | 51 item gen tests |
| Elemental damage (7 types, resistances) | `ElementalCombat.cs`, `Resistances.cs`, `DamageType.cs` | 20 elemental tests |
| Crit system (16 weapon types, variable crit) | `CritSystem.cs` | 18 crit tests |
| Monster AI (5 archetypes, state machine) | `MonsterBehavior.cs`, `MonsterArchetype.cs` | 62 monster tests |
| Monster modifiers (10 types, rarity tiers) | `MonsterModifier.cs`, `MonsterSpawner.cs` | Modifier + spawner tests |
| Town layout (30x30, 5 NPCs) | `TownLayout.cs`, `NpcData.cs` | — |
| Input Map (12 actions) | `project.godot` | — |
| SceneManager autoload | `SceneManager.cs` | — |

### Scenes ✓
| Scene | Script | What it does |
|-------|--------|-------------|
| MainMenu | `MainMenu.cs` | New Game, Load, Settings, Exit |
| CharacterCreate | `CharacterCreate.cs` | Name entry, stat preview |
| Town | `TownScene.cs` | Walk, NPC interact (S), shop, dungeon entrance |
| Dungeon | `DungeonScene.cs` | Combat, floor transitions, death, automap |

### UI ✓
| Component | Script | What it does |
|-----------|--------|-------------|
| GameplayHud | `GameplayHud.cs` | HP/MP orbs, floor, gold, level/XP |
| PauseMenu (game window) | `PauseMenu.cs` | 6 tabs: Inventory, Equipment, Stats, Skills, Settings, Game |
| SettingsPanel | `SettingsPanel.cs` | Volume, target priority, window mode, controls |
| NpcPanel | `NpcPanel.cs` | NPC greeting, shop buttons, interact prompt |
| Automap | `Automap.cs` | D1-style wireframe overlay, 3 modes |
| HP/MP Orbs | `HpMpOrbs.cs` | Diablo-style globes |

### Tools ✓
| Command | What |
|---------|------|
| `make test` | 480 unit tests |
| `make test-game` | Launch real game |
| `make test-game-headless` | 60-assertion E2E (16 phases) |
| `make tool-sprite-align` | Sprite alignment debugger |
| `make tool-sprite-frames` | Animation frame inspector |

### Documentation ✓
- 22 game dev learning docs in `docs/basics/`
- 5 system specs in `docs/systems/` (elemental, crit, monster behavior, modifiers, item gen)
- 5 ADRs in `docs/decisions/`
- Design pillars, glossary
- Dev journal (10+ sessions)

---

## What's Partial (Has Bugs)

| ID | Issue | Priority | What's Wrong |
|----|-------|----------|-------------|
| BUG-01 | Floor tiles render but look wrong | P0 | ISS tiles load via magenta-strip but may not display properly in all scene transitions |
| BUG-02 | Wall collision glitchy | P1 | Manual tile-check pushback misses corners; should use TileMap physics layers |
| BUG-03 | All enemies behave identically | P1 | MonsterArchetype system built but not wired into EnemyEntity/DungeonScene |
| BUG-04 | Elemental damage not integrated | P1 | ElementalCombat.cs exists, GameSystems.AttackMonster ignores it |
| BUG-05 | Crit system not integrated | P1 | CritSystem.cs exists, GameSystems uses hardcoded 15% |
| BUG-06 | NPC panel clips on small screens | P2 | Right-side anchor offsets, needs responsive sizing |
| BUG-07 | Font loading errors on first run | P2 | Need `make import` to build Godot asset cache |

---

## What's Not Built (To Do)

### P0 — Fix the Game (make it playable and feel right)

| ID | Title | Depends On | Notes |
|----|-------|-----------|-------|
| FIX-01 | Replace placeholder diamonds with real FLARE sprites | — | PlayerController, EnemyEntity need AnimatedSprite2D with creature sheets |
| FIX-02 | Add TileMap physics layers for wall collision | BUG-02 | Replace manual tile-check with proper TileSet collision polygons |
| FIX-03 | Integrate elemental damage into combat | BUG-04 | Wire ElementalCombat into GameSystems.AttackMonster |
| FIX-04 | Integrate crit system into combat | BUG-05 | Wire CritSystem into GameSystems.AttackMonster, use weapon type |
| FIX-05 | Integrate monster archetypes into spawning | BUG-03 | Add archetype to MonsterData, wire MonsterBehavior into EnemyEntity |
| FIX-06 | Add AStarGrid2D pathfinding for enemies | — | Replace straight-line chase with AStarGrid2D (see docs/basics/pathfinding.md) |
| FIX-07 | Add game feel (screen shake, hit flash, damage numbers) | — | See docs/basics/game-feel.md and docs/basics/visual-feedback.md |
| FIX-08 | Fix floor rendering | BUG-01 | Verify tile textures load correctly through all scene transitions |

### P1 — Complete Systems

| ID | Title | Depends On | Notes |
|----|-------|-----------|-------|
| SYS-01 | Character class selection (Warrior/Rogue/Mage) | — | CharacterCreate needs class picker, per-class bonuses |
| SYS-02 | Skill definitions + use-based leveling | SYS-01 | Define actual skills per class, track usage XP |
| SYS-03 | Death penalty flow (XP loss, item loss, buyout) | — | Multi-step death screen per docs/systems/death.md |
| SYS-04 | Quest system (Adventure Guild radiant quests) | — | Kill X, clear floor, push depth |
| SYS-05 | Level Teleporter NPC | — | UI to select previously visited floors |
| SYS-06 | Blacksmith crafting UI | — | Add affix selection, material cost, recycling |
| SYS-07 | Bank UI | — | Deposit/withdraw grid, expansion purchase |
| SYS-08 | Unique items (70-100 fixed-effect items) | — | Per research: mix of stat packages + build-altering |
| SYS-09 | Achievement system (Fated Ledger) | — | Per research: 4 tiers, Insight Points, account/per-char |
| SYS-10 | Monster families (zone-exclusive creature sets) | FIX-05 | Per research: each zone has 3-5 unique families |

### P2 — Endgame & Polish

| ID | Title | Notes |
|----|-------|-------|
| END-01 | Dungeon Pacts (voluntary difficulty modifiers) | Per research: Hades Heat model |
| END-02 | Magicule Attunement (post-cap passive tree) | Per research: Grim Dawn Devotion model |
| END-03 | Dungeon Intelligence (adaptive AI Director) | Per research: L4D Director + living dungeon |
| END-04 | Zone Saturation (per-zone difficulty dial) | Per research: LE Corruption model |
| END-05 | Depth gear tiers (new rarity at floor 50/100/150) | Per research: Nioh NG+ model |
| POL-01 | Audio system (SFX + music + ambient) | See docs/basics/audio-fundamentals.md |
| POL-02 | Real sprite animations (AnimatedSprite2D) | See docs/basics/godot-animation.md |
| POL-03 | Zone visual themes (per-zone floor/wall textures) | ISS has 49 floor + 43 wall themes |
| POL-04 | Shader effects (hit flash, outline, glow) | See docs/basics/godot-shaders.md |

### P3 — Future

| ID | Title | Notes |
|----|-------|-------|
| FUT-01 | Gamepad support | Add joypad events to existing Input Map actions |
| FUT-02 | Key rebinding UI | Runtime InputMap modification |
| FUT-03 | Desktop export (macOS/Windows/Linux) | See Godot export templates |
| FUT-04 | Monster trophy crafting | Per research: craft from monster parts |
| FUT-05 | Evolving items (grow through use) | Per research: items absorb magicules |
| FUT-06 | Synergy ring system (10 ring slots) | Per research: resonance combos |
| FUT-07 | Dungeon Weather (daily magicule fluctuation) | Per research: system-clock-based variety |

### Tech Debt

| ID | Title | Notes |
|----|-------|-------|
| DEBT-01 | Retire GameCore.cs, migrate to entity framework | QA audit #1 priority — dual system causes formula divergence |
| DEBT-02 | Fix stat formula divergence (code vs spec) | QA audit: STR 0.5 vs 1.5, VIT ×3 vs ×5 |
| DEBT-03 | Add namespaces to 31 files | All game code in global namespace |
| DEBT-04 | Fix EffectSystem single-tick-per-frame bug | Use while instead of if for accumulated ticks |
| DEBT-05 | Fix EntityFactory HP mismatch | Factory hardcodes 108, StatSystem returns 123 |
| DEBT-06 | Standardize data class fields vs properties | Mixed public fields and properties |
| DEBT-07 | Auto-doc maintenance (Claude Code hooks) | Per research: stop hook + session hook in .claude/settings.json |

---

## Test Summary

| Suite | Count | Command |
|-------|-------|---------|
| Unit tests (xUnit) | 480 | `make test` |
| Game loop E2E | 60 assertions | `make test-game-headless` |
| Visual test scenes | 24 scenes | `make test-visual` |
| Sprite tools | 2 tools | `make tool-sprite-align`, `make tool-sprite-frames` |

---

## Milestone Targets

| Milestone | What | Tickets |
|-----------|------|---------|
| **Playable Alpha** | Real sprites, working collision, game feel, integrated combat | FIX-01 through FIX-08 |
| **Feature Complete** | All NPCs functional, classes, skills, death flow, quests | SYS-01 through SYS-10 |
| **Endgame** | Pacts, attunement, adaptive dungeon, gear tiers | END-01 through END-05 |
| **Polish** | Audio, animations, shaders, zone themes | POL-01 through POL-04 |
| **Ship** | Export, gamepad, key rebinding | FUT-01 through FUT-03 |
