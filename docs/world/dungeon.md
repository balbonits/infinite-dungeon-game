# Dungeon

## Summary

An infinitely descending dungeon with procedurally generated floors. Each floor has monsters that respawn forever. The dungeon caches up to 10 floors.

## Current State

**Spec status: LOCKED.** Generation algorithm, difficulty scaling, and special room types are defined and locked.

The prototype has a single floor with spawning enemies. There is no floor generation, descent, or caching system yet.

## Design

### Infinite Descent

- The dungeon has no bottom — floors increase indefinitely
- Difficulty scales with depth (stronger enemies, more dangerous tiers)
- Players descend via a staircase/exit point on each floor
- Players can ascend back to previously visited floors (if cached)

### Floor Generation

Each floor is procedurally generated using a **hybrid algorithm**:

#### Generation Algorithm

Three techniques combined for structured-but-organic layouts:

1. **Binary Space Partitioning (BSP)** — Macro structure
   - Recursively subdivide the floor into rectangular regions
   - Place rooms within each leaf partition
   - Guarantees predictable room count and connectivity
   - Produces clear room-corridor structure

2. **Drunkard's Walk** — Corridor generation
   - Random walk agents carve corridors between BSP rooms
   - Creates winding, organic connectors (not straight lines)
   - Multiple agents for natural branching paths
   - Produces occasional loops (alternative routes through the floor)

3. **Cellular Automata** — Smoothing pass
   - Apply 2-3 iterations of smoothing to polish edges
   - Rule: tile becomes wall if 5+ of 8 neighbors are walls (Moore neighborhood)
   - Removes jagged artifacts, creates natural cave-like edges
   - Runs after BSP + drunkard's walk are complete

**Floor size** scales with depth using a zone-stepped formula with intra-zone ramp (mirroring the difficulty scaling):

```
zone = ceil(floor_number / 10)
intra_step = (floor_number - 1) % 10
zone_scale = 1.0 + (zone - 1) * 0.25
intra_scale = 1.0 + intra_step * 0.02
size_scale = zone_scale * intra_scale
width = clamp(round(50 * size_scale), 50, 150)
height = clamp(round(100 * size_scale), 100, 300)
```

Constants: BASE_WIDTH=50, BASE_HEIGHT=100, MAX_WIDTH=150, MAX_HEIGHT=300

| Floor | Zone | Scale | Size (WxH) | Feel |
|-------|------|-------|------------|------|
| 1 | 1 | 1.00x | 50x100 | Compact tutorial |
| 10 | 1 | 1.18x | 59x118 | Moderate |
| 11 | 2 | 1.25x | 63x125 | Zone jump |
| 20 | 2 | 1.48x | 74x148 | Getting big |
| 50 | 5 | 2.18x | 109x218 | Large |
| 100 | 10 | 3.23x | 150x300 | Max (capped) |

Floors are larger than one screen — the camera follows the player and scrolls.

### Floor Pathing (IKEA Layout)

Rooms are connected as a semi-linear chain from entrance to exit using nearest-neighbor traversal. BSP generates room positions, then rooms are ordered into a guided path:

Entrance → Room A → Room B → Room C → ... → Exit

- Corridors connect rooms in chain order (not BSP sibling pairs)
- The exit is always the last room in the chain
- Optional loop corridors (15% chance) still apply for minor alternative routes
- Player CAN backtrack, but the natural corridor flow guides them forward

**Room count per floor:** 5-8 rooms (BSP generates this range naturally). Includes entrance room, exit room, and 3-6 combat/exploration rooms.

Each floor contains:
- Floor layout (walls, obstacles, open areas)
- Enemy spawn points and types
- Entrance point (from above) and exit point (to below)
- Safe spots at entrance and exit (see below)
- Possible special rooms (see Special Room Types below)

### Tile System

All dungeon environment art uses the **Isometric Stone Soup (ISS)** tileset by Screaming Brain Studios (CC0 license), located at `assets/isometric/tiles/stone-soup/`.

#### Tile Dimensions

| Tile Type | Size (px) | Shape | Notes |
|-----------|-----------|-------|-------|
| Floor tile | 64x32 | Isometric diamond | Standard 2:1 isometric ratio |
| Wall block | 64x64 | Isometric cube | Full block and half block variants available, plus top-face overlays |

The Godot TileMap tile size is **64x32** (isometric).

#### Floor Size in Tiles

Floor size in tiles matches the progressive formula above — width and height values are tile counts. At floor 1 the dungeon is 50x100 tiles; at floor 100 it caps at 150x300 tiles. The floor should feel large enough for meaningful exploration across 5-8 rooms, with deeper floors offering significantly more space to fill with enemies and rooms.

#### Zone Visual Themes

Each dungeon zone maps to an ISS wall sheet + floor sheet pair, giving each 10-floor zone a distinct visual identity. ISS provides 43 wall theme sheets and 49 floor theme sheets.

| Zone | Floors | Wall Theme | Floor Theme |
|------|--------|------------|-------------|
| Zone 1 | 1-10 | TBD | TBD |
| Zone 2 | 11-20 | TBD | TBD |
| Zone 3 | 21-30 | TBD | TBD |
| Zone N | (N-1)*10+1 - N*10 | TBD | TBD |

Theme assignments will be finalized after reviewing the full ISS sheet catalog. The goal: each zone should feel visually distinct and progressively darker/more hostile to reinforce the magicule density gradient described in The Living Dungeon section.

#### Transparency

ISS sprites use magenta (`#FF00FF`) backgrounds as a transparency key. The import pipeline must strip this color to transparent.

#### Lighting

ISS includes 3 torch sprites (including an animated variant). Torches provide ambient environmental detail and can reinforce the dungeon's atmosphere. Torch placement rules are TBD.

#### Map Prototyping

ISS ships with Tiled `.tsx` files that can be used for rapid level prototyping before the procedural generation pipeline is built.

#### Superseded Assets

The previous `cave_atlas.png` (1024x1024 irregular atlas) is superseded by ISS. All new environment work must use ISS tiles at the dimensions specified above.

### Seeded Generation

- Each floor has a seed that determines its layout
- Seeds allow sharing specific floors: "try floor 47, seed ABC123"
- **Seeded mode restrictions:** when replaying a seeded floor, certain benefits may be limited to prevent exploits (details TBD)

### Floor Caching

- Maximum 10 floors are cached in memory/save data at a time
- When the cache is full, the oldest floor is purged
- Purged floors regenerate with a new layout when revisited (new seed)
- Cache priority: keep the floors nearest to the player's current position

### Safe Spots

- Located at every floor entrance and exit
- Visual indicator: glowing crystal or similar landmark
- Function as auto-checkpoints — the last safe spot touched is the respawn point on death
- No enemies spawn within a radius of safe spots

### Floor Difficulty Scaling

Difficulty uses a **zone-based system** with steep inter-zone jumps and gentle intra-zone ramps. Floors are grouped into zones of 10.

#### Zone Structure

| Zone | Floors | Feel |
|------|--------|------|
| Zone 1 | 1–10 | Tutorial. Learn the basics. |
| Zone 2 | 11–20 | Real challenge begins. |
| Zone 3 | 21–30 | Gear and build start mattering. |
| Zone N | (N-1)*10+1 – N*10 | Increasingly demanding. |

#### Scaling Formula

```
zone = ceil(floor_number / 10)
zone_multiplier = 1.0 + (zone - 1) * 0.5
intra_zone_step = (floor_number - 1) % 10
intra_zone_multiplier = 1.0 + (intra_zone_step * 0.05)
total_multiplier = zone_multiplier * intra_zone_multiplier
```

All monster stats (HP, damage, speed) are multiplied by `total_multiplier`.

**Example progression:**

| Floor | Zone | Zone Mult | Intra Mult | Total Mult |
|-------|------|-----------|------------|------------|
| 1 | 1 | 1.0x | 1.00x | 1.00x |
| 5 | 1 | 1.0x | 1.20x | 1.20x |
| 10 | 1 | 1.0x | 1.45x | 1.45x |
| 11 | 2 | 1.5x | 1.00x | 1.50x (steep jump!) |
| 15 | 2 | 1.5x | 1.20x | 1.80x |
| 20 | 2 | 1.5x | 1.45x | 2.18x |
| 21 | 3 | 2.0x | 1.00x | 2.00x (steep jump!) |
| 50 | 5 | 3.0x | 1.45x | 4.35x |
| 100 | 10 | 5.5x | 1.45x | 7.98x |

**Design intent:** Within a zone, difficulty ramps gently (~5% per floor). Between zones, a steep ~40-50% stat jump creates clear walls. Players must farm, gear up, and optimize before pushing into the next zone. Bosses on every 10th floor gate the zone transition.

### Special Room Types

#### Boss Rooms (Every 10th Floor)

- Floor 10, 20, 30, etc. are **boss floors**
- Single boss enemy with significantly higher stats (3x the floor's normal monster stats)
- The boss spawns in the exit room, blocking the staircase down. The boss must be defeated to descend.
- First kill drops **rare crafting materials** (one-time reward per boss per character)
- First kill grants **5x XP bonus** (see [leveling.md](../systems/leveling.md))
- Revisiting a cleared boss floor spawns normal enemies, no boss

#### Challenge Rooms (Shortcut)

- One challenge room spawns on every non-boss floor (always present, not RNG)
- Positioned off the main path, connected to an early room in the chain (room 2 or 3)
- Contains a second corridor shortcut that connects to the second-to-last room in the chain
- Inside: a single boss-level creature with stats scaled to **player level** (not floor level)
- Defeating the creature grants bonus rewards proportional to creature difficulty
- The shortcut corridor is blocked until the creature is defeated
- The player choice is fight-or-skip, not find-or-miss

#### Treasure Rooms

- **~5% spawn chance** per non-boss floor (roughly 1 every 20 floors)
- Contains a material chest with crafting materials — no enemies
- Treasure room materials are floor-appropriate (higher floors = better materials)
- Indicated by a distinct visual cue at the room entrance (e.g., glowing doorway)
- Single chest, single pickup — no repeated farming of the same treasure room

#### Safe Rooms

- Located at floor **entrance and exit only** (no mid-floor safe rooms)
- Shallow breathing room: HP regeneration ticks slightly faster, no full heal
- No enemies spawn within the safe room radius
- Visual indicator: glowing crystal or landmark
- Function as auto-checkpoints — last safe spot touched = death respawn point
- **Not generous.** Players should struggle through floors. The only way to "dominate" is being overleveled and overgeared.

**No puzzle rooms.** The dungeon is a hack-and-slash zone — complexity lives in character building, not mid-dungeon mechanics.

### Monster Respawning

- Enemies on each floor respawn infinitely on timers
- This allows safe farming on any floor
- Respawn rate and enemy composition scale with floor depth
- Soft cap on active enemies per floor to prevent performance issues

---

## The Living Dungeon

The dungeon is not just a place. It is a **living entity** — a massive, intelligent concentration of magicules that has developed some form of awareness over an incomprehensible span of time. In lore terms, the dungeon IS a monster. The largest, oldest, most patient monster in the world.

### The Dungeon Feeds

The dungeon attracts outsiders — adventurers, creatures, anything alive — to enter its body. It lures them with treasure, rare materials, and the promise of growth. Adventurers enter weak, fight monsters, grow stronger (accumulating processed mana as EXP), push deeper into more dangerous territory, and eventually die. When they die, the dungeon eats their accumulated EXP — the processed mana memories stored in their bodies and minds.

This is the dungeon's food cycle. It is a predator, and adventurers are its prey.

### Monsters Are the Dungeon's Biology

The monsters that fill the dungeon are not random wildlife. They are part of the dungeon's body, serving two biological functions:

- **White blood cells** — They fight invaders to protect the dungeon's body. Adventurers are foreign organisms entering a living system, and monsters are the immune response.
- **Digestive enzymes** — They kill adventurers so the dungeon can absorb their processed mana and EXP. Every adventurer death is a meal.

Monsters are born from heavy magicule exposure within the dungeon. Their brains cannot properly process magicules, so exposure causes mutations instead of controlled magic. Deeper floors have denser magicules, which produces more twisted, more powerful creatures. The dungeon's "biology" gets more aggressive the deeper you go — like moving from skin into organs, then into the gut.

### Safe Spots Are Deliberate

The dungeon deliberately provides rest areas. Safe spots at floor entrances and exits are not accidents or weaknesses in the dungeon's defenses. They are **bait**.

The dungeon is fattening its prey. It lets adventurers rest, recover HP and mana, level up, and grow stronger — because a stronger adventurer carries more processed mana. A level 5 adventurer who dies is a snack. A level 50 adventurer who dies is a feast. The dungeon invests in its prey's growth so that the eventual harvest is richer.

### Revival Is a Dungeon Mechanic

When an adventurer dies in the dungeon, they don't simply respawn. Here is what happens in lore terms:

1. **The dungeon captures their consciousness.** At the moment of death, the adventurer's consciousness — which is itself a form of magic — is caught by the dungeon rather than dissipating.
2. **The dungeon holds the consciousness.** This is the death screen. The adventurer's awareness is suspended while the dungeon decides what to do with them. From the adventurer's perspective, they are negotiating with the dungeon for their life.
3. **The adventurer makes revival decisions.** The death screen UI — choosing a respawn destination, deciding whether to pay gold for protections — represents the consciousness bargaining with the dungeon entity.
4. **The dungeon constructs a new body.** Using magic, the dungeon builds a fresh physical form for the adventurer.
5. **The dungeon imbues the consciousness into the new body.** The adventurer is reborn inside the dungeon.
6. **But first, the dungeon eats.** Before releasing the consciousness, the dungeon skims some of their memories — their processed mana, their EXP. This is the death penalty. The dungeon takes its cut.

Revival is not mercy. It is an **investment**. The dungeon lets prey rebuild so they can die again later, deeper, carrying even more processed mana. Kill them once for a small meal, or let them grow and kill them ten more times for ten bigger meals.

### The Dungeon Is an Investor

The dungeon plays "penny stocks by the millions." It doesn't need any single adventurer to make it rich. It processes thousands of adventurers over centuries, taking small harvests from each death. Weak adventurers who die on floor 3 are worth almost nothing — but the dungeon barely spent anything reviving them either. Strong adventurers who push to floor 80 and die are worth enormously more. The dungeon is patient, strategic, and has been doing this forever.

Why doesn't the dungeon just kill everyone immediately? Because small harvests from weak adventurers aren't worth the effort. The return on investment is better when adventurers grow strong and die deep. A level 1 adventurer's processed mana is pocket change. A level 50 adventurer's processed mana is a windfall. The dungeon maximizes its total intake by letting prey accumulate value.

### Rested XP Is a Lure

When an adventurer leaves the dungeon and returns later, they come back with a rested XP bonus. In lore terms, this is the dungeon **incentivizing adventurers to return**. The dungeon wants them to come back. It sweetens the deal — "come back, you'll grow faster" — because every returning adventurer is a potential future meal.

The rested XP mechanic (see [leveling.md](../systems/leveling.md)) is not a passive reward. It is the dungeon actively luring prey back into its body.

### Magicule Density Gradient

The dungeon's magicule density increases with depth, creating a gradient from mild to lethal:

- **Shallow floors** — Low density. Skills work normally. The environment is relatively safe. This is the dungeon's "skin" — it lets prey enter easily.
- **Mid floors** — Moderate density. Monsters are stronger (they absorb more magicules from the environment). Faint environmental effects become visible — shimmering air, unusual warmth.
- **Deep floors** — High density. The environment itself pressures the adventurer. Monsters are significantly more powerful and mutated. The dungeon's "muscles and organs."
- **Extreme depth (past crust/mantle equivalent)** — Lethal density. The flood of magicules overwhelms living cells and overloads the nervous system. It's not just heat and pressure — the magicules themselves tear through biology. This is the dungeon's "core," the densest concentration of its being.

This gradient creates a natural hard ceiling for the infinite dungeon. It's not a wall that says STOP — it's the dungeon's body becoming increasingly hostile. The most powerful adventurers can push a few floors deeper than average, but nobody reaches the core. The endpoint emerges from the world's rules, not an arbitrary level cap.

---

## Resolved Questions

| Question | Decision |
|----------|----------|
| Generation algorithm | Hybrid: BSP (macro) + Drunkard's Walk (corridors) + Cellular Automata (smoothing) |
| Difficulty scaling | Zone-based: 10-floor zones, gentle intra-zone ramp, steep inter-zone jumps |
| Special floor types | Boss (every 10th), treasure (~5% chance), safe (entrance/exit only). No puzzles. |
| Floor size | Progressive: 50x100 at floor 1, scaling to 150x300 cap via zone-stepped formula |
| Teleport from town | Yes — Level Teleporter NPC in town accesses previously visited floors |
