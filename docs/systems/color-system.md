# Color System

## Summary

A unified, continuous color gradient used across the entire game to communicate relevance and threat — always relative to the player's current level. Instead of discrete rarity tiers (white/green/blue/purple/orange), this game uses a **cool-to-warm rainbow spectrum** where an entity's color shifts dynamically as the player levels.

This is a novel system. Research across 50+ games (Diablo, WoW, PoE, Dead Cells, Terraria, Lost Ark, roguelikes, gacha, tabletop RPGs) found no shipped game using a true continuous color gradient for item/monster identity. The closest precedents are 7 Days to Die's 0-600 quality bands and ARK's continuous quality values mapped to named tiers.

## Current State

Design phase. Replaces the fixed 3-tier enemy danger color system (green/yellow/red) with a unified gradient. The existing UI palette (see [ui-theme.md](../assets/ui-theme.md)) remains unchanged — the gradient system builds on top of it.

## Design

### The Gradient (Cool → Warm)

One spectrum, used everywhere. Color is always computed from the **level gap** between the player and the entity or item.

```
Grey → Blue → Cyan → Green → Yellow → Gold → Orange → Red
trash   low                    even                  deadly/powerful
```

- Standard 6-digit HEX (RRGGBB)
- The same item or monster changes color as the player levels
- No discrete "Rare" or "Epic" labels — just a position on the spectrum
- Warmer = more powerful/dangerous relative to you
- Cooler = less relevant/threatening relative to you
- Grey = so far below you it's not worth engaging

### Gradient Anchor Points

Eight reference points on the spectrum. Color is interpolated between these anchors based on level difference.

| Position | Color | Hex | Level Gap (approx) | Meaning |
|----------|-------|-----|---------------------|---------|
| Trivial | Grey | `#9D9D9D` | -10+ below | Far below player, not worth engaging |
| Low | Blue | `#4A7DFF` | -7 to -5 below | Below player, minimal challenge/value |
| Low-Mid | Cyan | `#4AE8E8` | -4 to -3 below | Slightly below, still some value |
| Even | Green | `#6BFF89` | -2 to +2 | Around player level, appropriate |
| Mid-High | Yellow | `#FFDE66` | +3 to +4 above | Slightly above, rewarding |
| High | Gold | `#F5C86B` | +5 to +7 above | Above player, valuable/dangerous |
| Very High | Orange | `#FF9340` | +8 to +9 above | Significantly above, risky |
| Extreme | Red | `#FF6F6F` | +10+ above | Way above player, deadly/powerful |

**Notes:**
- Green (`#6BFF89`) and gold (`#F5C86B`) reuse the existing game palette (`safe` and `accent`)
- Red (`#FF6F6F`) reuses the existing `danger` color
- Blue, cyan, and orange are new additions tuned to complement the dark fantasy palette
- Exact level gap thresholds are approximate — final tuning deferred to balance pass

### Purple — Reserved / Out-of-System

Purple is **not part of the gradient**. It is reserved for entities and objects that exist outside the normal level/rarity system:

- Unkillable or invincible entities
- Story items or narrative objects
- NPCs or objects outside normal gameplay interaction
- Future: quest-related or lore-significant items

Purple signals: "this does not follow the rules."

### Application: Items

Item color represents the **level gap between the player and the item's effective level**.

- A freshly dropped item on a hard floor glows warm (orange/red) — powerful, but ability bonuses may be locked if you're underleveled
- As the player levels up, the same item shifts through the spectrum: Red → Orange → Gold → Yellow → Green → Blue → Grey
- **No equipment restrictions on wearing items** — any class can equip anything, but abilities/bonuses tied to the item may be gated by level as a sign of being underleveled
- At a glance, inventory readability: warm items = still relevant, cool/grey items = time to replace or recycle at the Blacksmith

### Application: Monsters

Monster color represents the **level gap between the player and the monster's effective level** (determined by floor depth).

- Blue/cyan monster = easy, below your level
- Green/yellow monster = appropriate challenge
- Orange/red monster = dangerous, above your level
- Grey monster = trivial, not worth fighting
- **Replaces the fixed 3-tier danger system** (green/yellow/red) from the Phaser prototype
- A player descending to a new deep floor sees mostly yellow/orange/red monsters
- Returning to earlier floors, the same monsters appear blue/grey

### Application: Floors

Floor depth can be communicated through color in the UI:

- Floor number color in the HUD follows the gradient (a floor appropriate for your level shows green/yellow; a deep push floor shows orange/red)
- Potential future: ambient lighting or tilemap tinting shifts with floor depth
- Exact floor color integration is deferred — the gradient system defines the palette, specific floor visuals are a future design task

### XP Bonus Scaling

Monster threat color maps to an XP multiplier, incentivizing risky deep pushes:

| Color Range | Threat Level | XP Effect |
|-------------|-------------|-----------|
| Grey | Trivial | Heavily reduced XP |
| Blue / Cyan | Low | Reduced XP |
| Green / Yellow | Even | Base XP |
| Gold / Orange | High | Bonus XP (scaling with level gap) |
| Red | Extreme | Large bonus XP |

The harder (warmer) the enemy, the more XP it rewards. Exact multiplier formula deferred to balance tuning.

### Relationship to Existing Palette

The gradient system extends the existing UI theme palette, not replacing it:

| Existing Color | Hex | Gradient Role |
|---------------|-----|---------------|
| `safe` | `#76F79F` / `#6BFF89` | Maps to "Even" (green) anchor |
| `accent` | `#F5C86B` | Maps to "High" (gold) anchor |
| `danger` | `#FF6F6F` | Maps to "Extreme" (red) anchor |
| `enemy-low` | `#6BFF89` | Replaced by gradient position |
| `enemy-mid` | `#FFDE66` | Replaced by gradient position |
| `enemy-high` | `#FF6F6F` | Replaced by gradient position |

The fixed `enemy-low`, `enemy-mid`, `enemy-high` colors become legacy once the gradient system is implemented. The core UI colors (`bg-0`, `bg-1`, `ink`, `muted`, `accent`, `panel-bg`, `panel-border`) remain unchanged.

## Open Questions

- Exact mathematical formula: how does `(entity_level - player_level)` map to a position on the gradient? Linear interpolation between anchors, or a curve?
- How many discrete interpolation steps vs truly continuous color blending?
- Should items display their absolute level in a tooltip alongside the relative color?
- How does the gradient interact with the Mage's elemental system (fire-element items are visually "red" but may be low-level)?
- Should the gradient be purely on the entity sprite, or also applied to name text, item borders, loot beams, etc.?
- Floor ambient color integration: tilemap tinting, lighting, or UI-only?
- How does the color shift animate when the player levels up? Instant snap or smooth transition?
