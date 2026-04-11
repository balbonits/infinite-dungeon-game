# Development Tracker

Current state of the game. Updated as work completes. Run `make build` to verify the project compiles.

## How to Read This

- **Done** = implemented and tested (or verified visually)
- **Partial** = exists but has known issues
- **To Do** = not started
- **Priority**: P0 = blocking, P1 = next up, P2 = planned, P3 = future

---

## What's Built (Done)

### Documentation & Assets ✓

| What | Details |
|------|---------|
| Game design specs | 80+ docs across 11 directories, all locked |
| Asset library | 819+ sprites, tiles, fonts, icons in `assets/` |
| Learning docs | 22 game dev fundamentals in `docs/basics/` |
| Architecture decisions | 5 ADRs in `docs/decisions/` |
| Dev journal | 8 sessions of development history |
| Project config | project.godot, DungeonGame.csproj, Makefile, input map (12 actions) |

---

## What's Partial (Has Issues)

| ID | Issue | Priority | What's Wrong |
|----|-------|----------|-------------|
| CFG-01 | project.godot has no main scene | P0 | Main scene will be set when first scene is created |
| CFG-02 | .csproj has no NuGet packages | P1 | Add packages as features are built |
| CFG-03 | CI disabled | P2 | Re-enable when code exists |

---

## What's Not Built (To Do)

### Phase 0 — Visual Foundation (one step at a time, verify each visually)

| ID | Title | Depends On | Verify |
|----|-------|-----------|--------|
| VIS-01 | Render one floor tile correctly | — | See a single 64x32 ISS floor tile in the Godot viewport |
| VIS-02 | Render a tile room (10x10) | VIS-01 | See a complete room with floor and wall tiles, correct isometric layout |
| VIS-03 | Place a character sprite on the tiles | VIS-02 | See a character sprite standing on the tiles, correctly z-ordered |
| VIS-04 | Move the character with arrow keys | VIS-03 | WASD/arrows move the character smoothly with isometric transform |
| VIS-05 | Camera follows the character | VIS-04 | Camera tracks the character with smoothing and zoom |
| VIS-06 | Place one enemy sprite that chases | VIS-05 | An enemy sprite moves toward the character |

### Phase 0.5 — Playable Prototype

| ID | Title | Depends On | Verify |
|----|-------|-----------|--------|
| PROTO-01 | Auto-attack system | VIS-06 | Character attacks nearest enemy in range on button press |
| PROTO-02 | Enemy damage and death | PROTO-01 | Enemy takes damage, plays death, disappears |
| PROTO-03 | Enemy spawning and respawning | PROTO-02 | Enemies spawn at edges, respawn after defeat |
| PROTO-04 | HUD overlay (HP, level, floor) | PROTO-01 | Stats visible on screen, update reactively |
| PROTO-05 | Player death and restart | PROTO-02 | Death screen shows, game restarts on input |
| PROTO-06 | GameState autoload and signals | PROTO-04 | Stats persist across frames, signals fire correctly |

### P1 — Complete Systems

| ID | Title | Depends On | Notes |
|----|-------|-----------|-------|
| SYS-01 | Character class selection (Warrior/Ranger/Mage) | PROTO-06 | CharacterCreate needs class picker, per-class bonuses |
| SYS-02 | Skill definitions + use-based leveling | SYS-01 | Define actual skills per class, track usage XP |
| SYS-03 | Death penalty flow (XP loss, item loss, buyout) | PROTO-05 | Multi-step death screen per docs/systems/death.md |
| SYS-04 | Quest system (Adventure Guild radiant quests) | PROTO-06 | Kill X, clear floor, push depth |
| SYS-05 | Level Teleporter NPC | PROTO-06 | UI to select previously visited floors |
| SYS-06 | Blacksmith crafting UI | PROTO-06 | Add affix selection, material cost, recycling |
| SYS-07 | Bank UI | PROTO-06 | Deposit/withdraw grid, expansion purchase |
| SYS-08 | Unique items (70-100 fixed-effect items) | SYS-06 | Per research: mix of stat packages + build-altering |
| SYS-09 | Achievement system (Fated Ledger) | PROTO-06 | Per research: 4 tiers, Insight Points, account/per-char |
| SYS-10 | Monster families (zone-exclusive creature sets) | VIS-06 | Per research: each zone has 3-5 unique families |

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

---

## Test Summary

| Suite | Count | Command |
|-------|-------|---------|
| Unit tests (xUnit) | 0 | `make test` (not yet configured) |
| Visual test scenes | 0 | — |

---

## Milestone Targets

| Milestone | What | Tickets |
|-----------|------|---------|
| **Visual Foundation** | One tile, one sprite, one room, movement, camera | VIS-01 through VIS-06 |
| **Playable Prototype** | Combat, HUD, death, game loop, state management | PROTO-01 through PROTO-06 |
| **Feature Complete** | All NPCs, classes, skills, death flow, quests | SYS-01 through SYS-10 |
| **Endgame** | Pacts, attunement, adaptive dungeon, gear tiers | END-01 through END-05 |
| **Polish** | Audio, animations, shaders, zone themes | POL-01 through POL-04 |
| **Ship** | Export, gamepad, key rebinding | FUT-01 through FUT-03 |
