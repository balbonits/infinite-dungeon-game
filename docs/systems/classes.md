# Classes

## Summary

Three classes define a character's playstyle: Warrior, Ranger, Mage. Each has distinct stat bonuses. Class is chosen once and cannot be changed.

## Current State

**Spec status: LOCKED.** All class stat bonuses (creation, per-level, milestone, free allocation) are defined and locked. Class selection exists in the prototype's design but the stat bonuses are not yet applied to gameplay mechanics.

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

*These are starting bonuses applied at character creation. Per-level-up bonuses are defined below.*

### Per-Level Auto Stat Bonuses

Every level-up grants automatic stat bonuses based on class. These are fixed (not player-chosen) and stack on top of free stat point allocation.

**Base auto-bonuses (levels 1–24):**

| Stat | Warrior | Ranger | Mage |
|------|---------|--------|------|
| STR | +2 | +1 | +0 |
| DEX | +1 | +2 | +1 |
| STA | +1 | +0 | +0 |
| INT | +0 | +0 | +2 |

**Total auto-bonus per level:** Warrior: +4, Ranger: +3, Mage: +3.

### Milestone Scaling (Every 25 Levels)

At every 25-level threshold, all non-zero auto-bonuses permanently increase by +1. This creates "class evolution" moments where growth accelerates.

| Level Range | Warrior STR/DEX/STA/INT | Ranger STR/DEX/STA/INT | Mage STR/DEX/STA/INT |
|-------------|------------------------|------------------------|----------------------|
| 1–24 | +2/+1/+1/+0 | +1/+2/+0/+0 | +0/+1/+0/+2 |
| 25–49 | +3/+2/+2/+0 | +2/+3/+0/+0 | +0/+2/+0/+3 |
| 50–74 | +4/+3/+3/+0 | +3/+4/+0/+0 | +0/+3/+0/+4 |
| 75–99 | +5/+4/+4/+0 | +4/+5/+0/+0 | +0/+4/+0/+5 |
| 100–124 | +6/+5/+5/+0 | +5/+6/+0/+0 | +0/+5/+0/+6 |

**Rule:** Only stats that have a non-zero base bonus receive the +1 milestone increase. Warrior never gains auto INT; Mage never gains auto STR or STA; Ranger never gains auto STA or INT.

**Why this works for metagaming:** Every stat at every level is calculable. Build guides can compute exact stat totals for any level+class combination. The milestone jumps create clear breakpoints that theorycrafters will optimize around (e.g., "hit level 25 before pushing Zone 3").

### Free Stat Point Allocation

On every level-up, the player receives **free stat points** to allocate manually to any of the four stats (STR, DEX, STA, INT). No restrictions — dump all into one stat or spread evenly.

| Level-Up Type | Free Stat Points | Free SP (Skill Points) |
|---------------|-----------------|-------------------|
| Normal level | 3 | 2 |
| Milestone (every 10th level) | 5 (+2 bonus) | 3 (+1 bonus) |

*SP are spent on passive masteries. AP (Ability Points) come from separate sources — see [Skills & Abilities](skills.md) for details.*

**No respec.** Stat allocation is permanent. This makes each point a meaningful decision and encourages rerolling new characters to try different builds.

### Stat Totals at Key Levels (Warrior Example)

Assumes all free stat points dumped into STR (worst-case max for one stat):

| Level | Auto STR | Free STR (all-in) | Total STR | Effective STR (after DR) |
|-------|---------|-------------------|-----------|-------------------------|
| 1 | 8 (base) | 3 | 11 | 10.0 |
| 10 | 28 | 33 | 61 | 37.9 |
| 25 | 58 | 81 | 139 | 58.2 |
| 50 | 133 | 159 | 292 | 74.5 |
| 100 | 358 | 315 | 673 | 87.1 |

*Effective STR uses the locked diminishing returns formula from [stats.md](stats.md): `raw * (100 / (raw + 100))`.*

**Design intent:** All numbers are player-facing and published. The community should be able to create build calculators, class comparison charts, and optimization guides. Transparency encourages metagaming.

### Unique Abilities

Each class has its own unique skill tree. Skills are **designed now, built post-MVP** — the core skill tree structure is established in documentation, with implementation deferred.

Key design decisions:
- **Infinite skill leveling** — every Skill (passive mastery) and Ability (active combat action) has no level cap
- **Strictly class-exclusive** — skills are unique to each class. No skill sharing between classes.
- **Hierarchical** — Skills (passive masteries) gate Abilities (active combat actions) (inspired by Project Zomboid's skill system)
- Full skill tree design: **[Skills & Abilities](skills.md)**

Skill tree status:
- **Warrior:** Designed — 8 masteries (Body: Unarmed, Bladed, Blunt, Polearms, Shields, Dual Wield / Mind: Discipline, Intimidation)
- **Ranger:** Designed — 7 masteries (Weaponry: Bowmanship, Throwing, Firearms, CQC / Survival: Awareness, Trapping, Sapping)
- **Mage:** Designed — 8 masteries (Elemental: Fire, Water, Air, Earth / Aether / Attunement: Restoration, Amplification, Overcharge)
- **Innate (all classes):** 4 standalone skills (Haste, Sense, Fortify, Armor) — see [magic.md](magic.md)

### Equipment Affinity

**No class-locked gear.** Any class can equip any item. Items have a `ClassAffinity` field — when the player's class matches, all stat bonuses on the item get **+25%**. Mismatched gear still works, just without the bonus. Unwanted gear can be recycled at the Blacksmith for materials.

**No imbued/magical equipment drops from monsters or bosses.** All equipment drops are base items. Magical/enchanted gear comes exclusively from player-driven crafting and enchanting systems.

See `docs/systems/equipment.md` for full slot layout, equip flow, and starting gear.

#### Ranger: Bow & Quiver System

The Ranger's primary attack is ranged. Equipment is split into two slots:

- **Bow** -- stat stick only. Determines base damage and attack speed. No effects or imbues.
- **Quiver** -- modifier slot. Effects and imbues (fire arrows, explosive bullets, poison tips, etc.) go on the quiver, not the bow.
- **Infinite ammo** -- arrows are unlimited. The player buys quivers to enable a weapon; ammo itself is never consumed.

### Character Appearance

Each class has a distinct default character design:

| Class | Appearance |
|-------|-----------|
| **Warrior** | Brown-haired Caucasian man. Balanced muscular build like a Roman centurion — strong but not bulky. |
| **Ranger** | Black woman. Tall, slightly less muscular than Warrior (farmer build). Wears a hood and light, fully-covering clothes (ninja-like) to blend into shadows. |
| **Mage** | White teenage girl. Always wearing long robes and carrying a staff taller than herself. |

*These are the default character designs. See [sprite-specs.md](../assets/sprite-specs.md) for sprite implementation details.*

### No Rerolls

The character is permanent. Once a class is chosen, it cannot be changed. This makes the choice meaningful and encourages mastering one playstyle.

### Three Classes, Emergent Builds

There are exactly 3 classes: Warrior, Ranger, and Mage. There are no hybrid classes or shared skill trees. Instead, "sub-classes" are **emergent** — players discover unique builds through their equipment choices and skill progression. The game is a platform for player creativity. Having no shared skills encourages rerolling new characters to try different classes.
