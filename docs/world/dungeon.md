# Dungeon

## Summary

An infinitely descending dungeon with procedurally generated floors. Each floor has monsters that respawn forever. The dungeon caches up to 10 floors.

## Current State

The prototype has a single floor with spawning enemies. There is no floor generation, descent, or caching system yet.

## Design

### Infinite Descent

- The dungeon has no bottom — floors increase indefinitely
- Difficulty scales with depth (stronger enemies, more dangerous tiers)
- Players descend via a staircase/exit point on each floor
- Players can ascend back to previously visited floors (if cached)

### Floor Generation

Each floor is procedurally generated:
- Floor layout (walls, obstacles, open areas)
- Enemy spawn points and types
- Entrance point (from above) and exit point (to below)
- Safe spots at entrance and exit (see below)

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

## Open Questions

- What procedural generation algorithm should be used for floor layouts?
- How should floor difficulty scaling work (linear, exponential, stepped)?
- Should some floors have special properties (boss floors, treasure rooms)?
- How large should each floor be relative to the current game viewport?
- Should the player be able to teleport to previously visited floors from town?
