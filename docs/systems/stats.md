# Stats System

## Summary

Four core stats define a character's strengths: STR, DEX, STA, INT. Stats influence combat, survivability, and abilities.

## Current State

Stats exist conceptually and are referenced in class selection, but stat-based formulas are not yet wired into gameplay mechanics.

## Design

### The Four Stats

| Stat | Name | General Purpose |
|------|------|----------------|
| STR | Strength | Physical power — melee damage |
| DEX | Dexterity | Agility and speed — attack speed, evasion, ranged accuracy |
| STA | Stamina | Health and endurance — max HP, damage resistance, recovery |
| INT | Intelligence | Magic power — spell damage, mana pool, special ability potency |

### Design Intent

Stats are kept intentionally loose at this stage. The purpose of each stat is defined, but exact formulas (e.g., "1 STR = +2 melee damage") are **not locked in**. This allows flexibility to tune balance as more systems come online.

### How Stats Will Be Used

- **Combat:** STR/INT affect damage output, DEX affects attack speed and evasion
- **Survivability:** STA affects max HP and damage resistance
- **Progression:** stats increase on level-up (base amount + class bonus)
- **Equipment:** items may modify stats (deferred to item system)

### Stat Growth

Stat growth uses a **hybrid allocation** system. On each level-up, the player receives both:

1. **Automatic class bonuses** — fixed stat increases determined by the character's class (see [classes.md](classes.md))
2. **Free stat points** — points the player allocates manually to any stat

The number of free points scales with level progression, giving players increasing agency over their build as they advance.

### Diminishing Returns

Stats use **soft diminishing returns** — each additional point in a stat provides slightly less benefit than the last. This is designed to promote experimentation: players are encouraged to try new characters, classes, and builds rather than min-maxing a single stat on one character.

### Stat Caps

**No caps.** Stats grow indefinitely, fitting the infinite dungeon theme. Combined with diminishing returns, this means investment in any stat is always rewarded, just with decreasing marginal gains.

### Backpack Size

Backpack size is **fixed** and not influenced by any stat. STR is combat-only — it has no relationship to carrying capacity.
