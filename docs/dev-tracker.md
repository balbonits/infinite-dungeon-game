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
| SYS-09 | Achievement system (Fated Ledger) | Done | AchievementTracker with 30 achievements across 5 categories, counter-based tracking, gold/title rewards, FatedLedger UI in pause menu. |
| SYS-10 | Monster families (zone-exclusive creature sets) | Done | Constants.Zones with 10-floor zone system, zone-gated species spawning. Zone 1: Skeleton+Bat, Zone 2: Goblin+Wolf, Zone 3: Orc+Spider, Zone 4: DarkMage mix, Zone 5+: all. |

---

## Systems Built Beyond Original Tracker

These systems were implemented during visual-first development but were not part of the original ticket plan.

### Game Flow
| System | Scripts | Status |
|--------|---------|--------|
| Splash screen | `SplashScreen.cs` | Done -- title, subtitle, "press any key" with pulse animation |
| Class selection | `ClassSelect.cs` | Done -- 3 class cards with stats/skills, keyboard+mouse, two-step confirm |
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
| Spawn safety | `Dungeon.SpawnEnemy()` | Done -- safe spawn radius (150px from player), wall margin, max retries |
| Ascend dialog | `AscendDialog.cs` | Done -- stairs-up triggers: return to town, go up 1 floor, select specific floor |
| Unified combat | `AttackConfig.cs`, `ClassAttacks.cs` | Done -- data-driven melee/projectile, no class-specific branching in Player |
| Projectile system | `Projectile.cs` | Done -- spawned by Ranger (arrow) and Mage (magic bolt), despawns on hit or max range |
| Directional sprites | `DirectionalSprite.cs` | Done -- 8-direction rotation loading and velocity-based switching |
| Flash effects | `FlashFx.cs` | Done -- damage, poison, curse, boost, shield, freeze, heal, crazed flash types |
| Floating text | `FloatingText.cs` | Done -- combat damage, heals, XP, level-up numbers that rise and fade |
| Level-gap enemy colors | `Enemy.cs` | Done -- 8-anchor gradient (grey to red) based on enemy-vs-player level gap |

### UI & Debug
| System | Scripts | Status |
|--------|---------|--------|
| Debug panel | `DebugPanel.cs` | Done -- F3 toggle, shows HP/level/XP/floor/damage/enemies/kills/session time |
| Pause menu | `PauseMenu.cs` | Done -- Esc toggle, Resume/Quit buttons, blocked during death screen |
| HUD | `Hud.cs` | Done -- stats label updated reactively from GameState |
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
| Global theme | `GlobalTheme.cs`, `UiTheme.cs` | Done -- consistent UI colors, font sizes, panel styles, button styles |
| Game settings | `GameSettings.cs` | Done -- toggles like ShowCombatNumbers |
| GC during loading | `ScreenTransition.cs` | Done -- forced GC during loading screen transitions |

---

## P2 -- Endgame & Polish

| ID | Title | Status | Notes |
|----|-------|--------|-------|
| END-01 | Dungeon Pacts (voluntary difficulty modifiers) | To Do | |
| END-02 | Magicule Attunement (post-cap passive tree) | To Do | |
| END-03 | Dungeon Intelligence (adaptive AI Director) | To Do | |
| END-04 | Zone Saturation (per-zone difficulty dial) | To Do | |
| END-05 | Depth gear tiers (new rarity at floor 50/100/150) | To Do | |
| POL-01 | Audio system (SFX + music + ambient) | To Do | |
| POL-02 | Real sprite animations (AnimatedSprite2D) | To Do | 8-directional static rotations done; animation frames not yet |
| POL-03 | Zone visual themes (per-zone floor/wall textures) | To Do | Dungeon has 4 floor variations; no zone-specific theming |
| POL-04 | Shader effects (hit flash, outline, glow) | Partial | FlashFx uses Modulate tinting; no custom shaders yet |

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

| Suite | Count | Command |
|-------|-------|---------|
| Unit tests (xUnit) | 0 | `make test` |
| Visual test scenes | 0 | -- |

---

## File Counts

| Category | Count | Location |
|----------|-------|----------|
| Scene files (.tscn) | 8 | `scenes/` |
| C# scripts | 67 | `scripts/` (includes autoloads/, ui/, logic/) |
| Input actions | 12 | `project.godot` [input] section |
| Autoloads | 2 | GameState, EventBus |
| Item definitions | 8 | `ItemDatabase.cs` |
| Enemy species | 7 | Skeleton, Goblin, Bat, Wolf, Orc, Dark Mage, Spider |
| Player classes | 3 | Warrior, Ranger, Mage |

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
