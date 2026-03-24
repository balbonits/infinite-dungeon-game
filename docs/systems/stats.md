# Stats System

## Summary

Four core stats define a character's strengths: STR, DEX, STA, INT. Stats influence combat, survivability, and abilities.

## Current State

Stats exist conceptually and are referenced in class selection, but stat-based formulas are not yet wired into gameplay mechanics.

## Design

### The Four Stats

| Stat | Name | General Purpose |
|------|------|----------------|
| STR | Strength | Physical power — melee damage, carrying capacity |
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

On each level-up, the player receives stat points. How they're distributed depends on the class — see [classes.md](classes.md).

## Open Questions

- Should players get free stat points to allocate manually, or is it all automatic per class?
- How should diminishing returns work (if at all) to prevent one stat from dominating?
- Should stats have soft caps or hard caps?
- What's the relationship between STR and carrying capacity (backpack slots)?
