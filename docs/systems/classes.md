# Classes

## Summary

Three classes define a character's playstyle: Warrior, Archer, Mage. Each has distinct stat bonuses. Class is chosen once and cannot be changed.

## Current State

Class selection exists in the prototype's design but the stat bonuses are not yet applied to gameplay mechanics.

## Design

### Available Classes

| Class | Primary Stats | Playstyle |
|-------|--------------|-----------|
| Warrior | STR, STA | Melee-focused, tanky, high physical damage |
| Archer | DEX | Ranged attacks, fast attack speed, evasive |
| Mage | INT | Spell-based damage, area effects, fragile |

### Base Stats + Class Bonuses

All characters start with the same base stats. Class determines which stats get bonus points at creation and on each level-up.

| Stat | Base | Warrior Bonus | Archer Bonus | Mage Bonus |
|------|------|--------------|-------------|------------|
| STR | 5 | +3 | +1 | +0 |
| DEX | 5 | +1 | +3 | +1 |
| STA | 5 | +2 | +1 | +0 |
| INT | 5 | +0 | +0 | +3 |

*These are starting bonuses. Per-level-up bonuses follow a similar but smaller pattern.*

### Unique Abilities

Each class will eventually have unique abilities. These are **explicitly deferred** — no ability design is locked in yet.

Potential directions (brainstorming only):
- **Warrior:** shield block, ground slam, charge
- **Archer:** multi-shot, dodge roll, trap
- **Mage:** fireball, teleport, area freeze

### No Rerolls

The character is permanent. Once a class is chosen, it cannot be changed. This makes the choice meaningful and encourages mastering one playstyle.

## Open Questions

- Should per-level stat bonuses be flat (same every level) or scaling?
- When should unique abilities be designed and implemented?
- Should there be a hybrid build path, or strictly class-locked abilities?
- How does class affect equipment restrictions (if at all)?
