# Classes

## Summary

Three classes define a character's playstyle: Warrior, Ranger, Mage. Each has distinct stat bonuses. Class is chosen once and cannot be changed.

## Current State

Class selection exists in the prototype's design but the stat bonuses are not yet applied to gameplay mechanics.

## Design

### Available Classes

| Class | Primary Stats | Playstyle |
|-------|--------------|-----------|
| Warrior | STR, STA | Melee-focused, tanky, high physical damage |
| Ranger | DEX | Ranged attacks, fast attack speed, evasive |
| Mage | INT | Spell-based damage, area effects, fragile |

### Base Stats + Class Bonuses

All characters start with the same base stats. Class determines which stats get bonus points at creation and on each level-up.

| Stat | Base | Warrior Bonus | Ranger Bonus | Mage Bonus |
|------|------|--------------|---------------|------------|
| STR | 5 | +3 | +1 | +0 |
| DEX | 5 | +1 | +3 | +1 |
| STA | 5 | +2 | +1 | +0 |
| INT | 5 | +0 | +0 | +3 |

*These are starting bonuses. Per-level-up bonuses are **scaling** — they grow at level thresholds, rewarding sustained investment in a class. The scaling roadmap must be concrete and documentable so that the community can create build guides, encouraging metagame exploration.*

### Unique Abilities

Each class has its own unique skill tree. Skills are **designed now, built post-MVP** — the core skill tree structure is established in documentation, with implementation deferred.

Key design decisions:
- **Infinite skill leveling** — every base skill and specific skill has no level cap
- **Strictly class-locked** — skills are unique to each class. No skill sharing between classes.
- **Hierarchical** — base skills gate specific skills (inspired by Project Zomboid's skill system)
- Full skill tree design: **[skills.md](skills.md)**

Skill tree status:
- **Warrior:** Designed — 7 base skills (Body: Unarmed, Bladed, Blunt, Polearms, Shields / Mind: Inner, Outer)
- **Ranger:** Designed — 7 base skills (Arms: Drawn, Thrown, Firearms, Melee / Instinct: Precision, Awareness, Trapping)
- **Mage:** Designed — 9 base skills (Arcane: Fire, Water, Air, Earth, Light, Dark / Conduit: Restoration, Amplification, Overcharge)
- **Innate (all classes):** 3 standalone skills (Haste, Sense, Fortify) — see [magic.md](magic.md)

### Equipment Restrictions

Equipment has **class-locked gear** — some items can only be equipped by specific classes. When a class-restricted item drops that the player can't use, it can be taken to the **blacksmith for recycling** (harvest materials from unwanted gear).

**No imbued/magical equipment drops from monsters or bosses.** All equipment drops are base items. Magical/enchanted gear comes exclusively from player-driven crafting and enchanting systems.

### No Rerolls

The character is permanent. Once a class is chosen, it cannot be changed. This makes the choice meaningful and encourages mastering one playstyle.

### Three Classes, Emergent Builds

There are exactly 3 classes: Warrior, Ranger, and Mage. There are no hybrid classes or shared skill trees. Instead, "sub-classes" are **emergent** — players discover unique builds through their equipment choices and skill progression. The game is a platform for player creativity. Having no shared skills encourages rerolling new characters to try different classes.
