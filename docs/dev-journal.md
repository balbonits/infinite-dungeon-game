# Development Journal

A running log of everything we build, test, learn, and decide — from zero to game. This project is built entirely by AI, directed by a product owner who is learning game development for the first time.

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

*This journal is append-only. Each session adds a new section. Never edit previous sessions — they're a historical record.*
