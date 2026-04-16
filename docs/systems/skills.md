# Skills & Abilities

## Summary

Each class has two interconnected progression systems: **Skills** (passive masteries) and **Abilities** (active combat actions). Skills provide passive bonuses and gate access to their child Abilities. Both use infinite leveling with separate point pools.

Inspired by Project Zomboid's hierarchical skill system (passive proficiency through use), combined with ARPG-style active abilities (Diablo, Path of Exile).

For class lore and magic philosophy, see [class-lore.md](../world/class-lore.md).

## Current State

**Spec status: LOCKED.** Skill/Ability hierarchy, leveling model, passive bonus formulas, XP system, and all three class trees are defined. Individual tuning numbers (exact cooldowns, damage values, ranges) are implementation-phase balancing -- the formulas and structure are locked.

---

## System Design

### Hierarchy

```
Category (organizational label -- has no level)
  +-- Skill / Mastery (leveled infinitely, provides passive bonuses, gates Abilities)
        +-- Ability (unlocked at parent Skill level 1, leveled infinitely)
```

Two layers of actual progression. Categories are purely organizational -- they group Skills visually but have no levels or stats of their own.

### Taxonomy

**Skills** are passive masteries you improve over time. They provide passive bonuses (damage %, cast speed %, block chance, etc.) and gate access to their child Abilities. Examples: Bladed, Bowmanship, Fire, Awareness.

**Abilities** are active combat actions you use in fights. They have mana costs, cooldowns, ranges, and damage values. They are assigned to the 4-slot hotbar. Examples: Slash, Dead Eye, Fireball, Snare.

Each Ability belongs to exactly one parent Skill. Using an Ability in combat grants XP to both the Ability and its parent Skill.

### Architecture

**Reactive/pull pattern.** The Ability looks UP to its parent Skill to read data (level, passive bonus values). The Ability then adjusts its own values based on what it reads. All logic lives in the Ability -- the Skill is just a data source.

**No shared type framework.** Each Ability is individually coded with its own specific behavior. No Passive/Toggle/Active base classes or type enums. This avoids coupling where modifying one Ability's framework breaks others.

### Progression Model

1. **Skills** are the entry point. The player levels a Skill (e.g., "Bladed") through use and SP investment.
2. When a Skill reaches **level 1**, all Abilities underneath it **unlock immediately**.
3. Each Ability is then leveled independently and infinitely.
4. The Skill continues to be leveled infinitely alongside its Abilities.

**Hybrid leveling:** Both Skills and Abilities gain XP through two paths:
- **Use-based XP** -- performing actions earns XP automatically (swinging a sword levels Bladed and Slash)
- **Point allocation** -- on level-up, the player receives SP (for Skills) and AP (for Abilities) to allocate manually

Both paths feed progression simultaneously.

**Skills provide two things:**
- **Passive bonuses** that scale with level (e.g., Bladed level 10 = passive bladed damage bonus)
- **Gating** -- Abilities require at least level 1 in the parent Skill

**Abilities** are the actionable powers used in combat. Each level improves the Ability's stats (damage, cooldown, range, duration, etc.). Exact per-level scaling is implementation-phase balancing.

### Skill Passive Bonus Formula

Every Skill provides a passive bonus that scales with its level. All Skills use the same formula structure, with a class-appropriate multiplier:

```
passive_bonus(skill_level) = skill_level * base_multiplier * (100 / (skill_level + 100))
```

This reuses the same diminishing returns curve as stats (see [stats.md](stats.md), K = 100). Early levels give strong returns; deep investment still helps but with decreasing gains.

**Base multiplier by bonus type:**

| Bonus Type | Multiplier | Example at Skill Level 10 | Example at Skill Level 50 |
|-----------|-----------|--------------------------|--------------------------|
| Damage % | 1.5 | +13.6% | +50.0% |
| Attack speed % | 0.8 | +7.3% | +26.7% |
| Defense % | 1.2 | +10.9% | +40.0% |
| Chance % (crit, dodge, block) | 0.5 | +4.5% | +16.7% |
| Regen / utility | 0.3 | +2.7% | +10.0% |

**Example -- Warrior Bladed Skill at level 25:**
- Passive damage bonus: `25 * 1.5 * (100 / 125)` = **30.0%** bonus bladed weapon damage
- Passive crit chance: `25 * 0.5 * (100 / 125)` = **10.0%** bladed crit chance

These bonuses are always active while the Skill is at that level -- no activation needed.

### XP System

Skills and Abilities gain XP through two simultaneous paths:

#### Use-Based XP

Every time an Ability is used in combat, it earns XP for both itself AND its parent Skill.

```
xp_per_use(type) = base_xp_per_use * floor_multiplier
```

| Type | Base XP Per Use |
|------|----------------|
| Skill (passive -- levels when any child Ability is used) | 5 |
| Ability (active -- levels on direct use) | 10 |
| Innate Skill (per second while active) | 2 |

`floor_multiplier` = same as enemy XP floor multiplier from [leveling.md](leveling.md): `1 + (floor - 1) * 0.5`

Using deeper-floor Abilities earns more XP, keeping progression relevant at all stages.

#### Point Allocation

**Skill Points (SP)** are allocated to Skills (passive masteries). **Ability Points (AP)** are allocated to Abilities (active combat actions). Each point grants a flat XP boost:

```
xp_from_point = 50 * (1 + target_level * 0.1)
```

This scales with the target's current level -- points invested in higher-level Skills/Abilities give proportionally more XP.

**SP sources:** Character level-up.

**AP sources (3):**
1. **Leveling** (primary, ~60% of total AP) -- AP per character level-up
2. **Combat milestones** (bonus, ~25%) -- AP from boss kills, floor clears
3. **Use-based per-category** (trickle, ~15%) -- earn AP by using Abilities in combat, tracked per category (Body AP, Survival AP, Elemental AP, etc.)

*Exact rates defined in [point-economy.md](point-economy.md).*

#### Level-Up Formula

```
xp_to_next_level(current_level) = floor(current_level^2 * 20)
```

Same quadratic shape as character XP but with a smaller constant (20 vs 45), so Skill/Ability levels come faster than character levels. Same formula for both Skills and Abilities.

| Level | XP Required |
|-------|------------|
| 0 -> 1 | 0 (instant -- all Skills start available) |
| 1 -> 2 | 20 |
| 5 -> 6 | 500 |
| 10 -> 11 | 2,000 |
| 25 -> 26 | 12,500 |
| 50 -> 51 | 50,000 |

### Weapon Equipment Requirements

**Weapon-type Skills require a matching weapon equipped to use their child Abilities.**

| Skill (Mastery) | Required Equipment | Class |
|-----------------|-------------------|-------|
| Unarmed | No weapon equipped (or fists) | Warrior |
| Bladed | Sword, axe, or dagger in main hand | Warrior |
| Blunt | Club, hammer, or mace in main hand | Warrior |
| Polearms | Spear, halberd, or staff in main hand | Warrior |
| Shields | Shield in off hand | Warrior |
| Dual Wield | Two weapons equipped | Warrior |
| Bowmanship | Bow or crossbow in main hand | Ranger |
| Throwing | Throwing weapon in main hand | Ranger |
| Firearms | Firearm in main hand | Ranger |
| CQC | Defensive offhand weapon equipped | Ranger |

**Mind/Survival/Elemental/Aether/Attunement Skills have no weapon requirement** -- they are mental/magical and work regardless of equipment.

**Skill passive bonuses are always active** even without the matching weapon -- they represent general proficiency. Only the Abilities (active combat actions) require the weapon.

### Ability Unlock Rules

**All Abilities under a Skill unlock at Skill level 1.** Higher Skill levels do NOT unlock additional Abilities. The full roster is available immediately once the player puts their first point into the parent Skill.

**Design rationale:** This encourages breadth of experimentation. Players can try all Abilities early and discover which ones fit their build. Depth comes from leveling individual Abilities higher, not from gating access.

### Infinite Leveling

Every Skill and Ability has **no level cap**. Consistent with the game's infinite dungeon theme, players can always grind deeper. Combined with diminishing returns on stats (see [stats.md](stats.md)), each level is always rewarding but with decreasing marginal gains.

### Class-Locked

Class Skills and Abilities are strictly locked to their class. No sharing between classes. See [classes.md](classes.md) for class design.

**Exception: Innate Skills.** There is a universal Innate category (Haste, Sense, Fortify, Armor) available to all classes. These are species-level magicule abilities, not class skills. See [magic.md](magic.md) for full Innate design.

---

## Innate Skills (4, All Classes)

Innate Skills are species-level magicule abilities every human possesses. They sit outside the class skill trees and are available to all classes. See [magic.md](magic.md) for full lore and mechanics.

| Skill | Type | Warrior Name | Ranger Name | Mage Name | Description |
|-------|------|-------------|-------------|-----------|-------------|
| Haste | Toggle (mana drain) | Haste | Haste | Haste | Magicule-enhanced burst of speed + dodge chance |
| Sense | Toggle (mana drain) | Sense | Sense | Sense | Magicule-enhanced perception, detect through walls |
| Fortify | Toggle (mana drain) | Fortify | Fortify | Fortify | Magicule-reinforced body, damage resistance |
| Armor | Always-on passive | Ironhide | Nimbleguard | Spellweave | Armor proficiency, class-specific equipment mastery |

**Innate UI:** Innate Skills display in a separate sub-section of the Skills tab, distinct from class masteries.

---

## Warrior

**Categories:** Body (6 masteries) + Mind (2 masteries)
**Tab name:** Warrior Arts
**Total: 8 masteries, 33 abilities**

*Class lore: see [class-lore.md](../world/class-lore.md#warrior)*

### Body

Physical combat techniques. Six masteries reflecting mercenary pragmatism -- trained in all weapon forms.

#### Unarmed

Hand-to-hand combat. Passive bonuses: unarmed damage, unarmed attack speed.

| Ability | Description |
|---------|-------------|
| Punch | Fast straight strikes, high attack speed |
| Kick | Leg strike with knockback |
| Grappling | Holds, throws, pins |
| Elbow Strike | Short-range burst damage |

#### Bladed

Swords, axes, and daggers. Passive bonuses: bladed weapon damage, bladed critical chance.

| Ability | Description |
|---------|-------------|
| Slash | Wide arc swing, multi-target |
| Thrust | Precision stab, high single-target |
| Cleave | Heavy overhead, multi-target |
| Parry | Deflect attack, counter window |

#### Blunt

Clubs, hammers, and maces. Passive bonuses: blunt weapon damage, stun chance.

| Ability | Description |
|---------|-------------|
| Smash | Heavy overhead, bonus vs armored |
| Bump | Blunt thrust, knocks enemy back ~1 tile |
| Crush | Charged hit, chance to stun |
| Shatter | Break enemy guard/shields |

#### Polearms

Spears, halberds, and quarterstaffs. Passive bonuses: polearm damage, attack range.

| Ability | Description |
|---------|-------------|
| Pierce | Long-range stab, keeps distance |
| Sweep | Horizontal AoE swing |
| Brace | Set against charge, counter bonus |
| Vault | Polearm reposition/dodge |
| Haft Blow | Thrust blunt end for knockback |

#### Shields

Shield techniques. Passive bonuses: block chance, block damage reduction.

| Ability | Description |
|---------|-------------|
| Block | Active damage reduction stance |
| Shield Bash | Offensive strike, staggers |
| Deflect | Reflect projectiles |
| Bulwark | Sustained defense, reduced movement |

#### Dual Wield

Two-weapon fighting. Passive bonuses: dual wield attack speed, dual wield damage.

| Ability | Description |
|---------|-------------|
| Dual Stab | Simultaneous stab, upped crit chance |
| Dual Slash | X-shaped slash, upped bleed chance |
| Spin Attack | Single spin AoE |
| Rapid Combo | 3-strike combo; +1 strike every 5 ability levels, caps at 15 -> becomes **Omnislash** |

### Mind

Mental discipline for a solo warrior. Two masteries covering internal resilience and external presence.

#### Discipline

Self-focused mental toughness. Passive bonuses: damage resistance, HP regeneration.

| Ability | Description |
|---------|-------------|
| Focus | Heightened awareness, +accuracy +crit |
| Endure | Damage reduction + debuff resistance |
| Deep Breaths | Self-heal over time, cooldown-based |
| Blood Lust | Kills extend effect. +ATK, +DMG, +SPD, +HP/MP regen, +status resist. BUT -DEF, -MP capacity |

#### Intimidation

Enemy-focused magicule projection. Passive bonuses: debuff aura range, debuff potency.

| Ability | Description |
|---------|-------------|
| Shout | AoE weakens nearby enemies |
| Intimidate | Single-target fear/stagger |
| Ugly Mug | Debuff aura on nearby enemies |
| Battle Roar | AoE slows enemy attack speed + chance to stun |

---

## Ranger

**Categories:** Weaponry (4 masteries) + Survival (3 masteries)
**Tab name:** Ranger Crafts
**Total: 7 masteries, 37 abilities**

*Class lore: see [class-lore.md](../world/class-lore.md#ranger)*

**Ammo system:** Ammo is unlimited, but requires a "magazine" item equipped (quivers, mags, projectile bags, bandoliers). Purchased, not crafted.

### Weaponry

The tools of the hunt. Each weapon solves a specific engagement problem.

#### Bowmanship

Bows and crossbows. The identity weapon -- silent, precise. Passive bonuses: bow/crossbow damage, draw speed. No auto-crossbows in the game for now.

| Ability | Description |
|---------|-------------|
| Dead Eye | Aimed shot, high damage, slow draw. The kill shot. |
| Pepper | Fast consecutive shots, reduced per-hit damage. When stealth breaks. |
| Lob | Arcing trajectory, hits behind cover or obstacles |
| Pin | Pins enemy in place, movement denial. The prey doesn't run. |
| Flame Arrow | Fire-imbued shot, DoT on impact. Tinkered. |

#### Throwing

Throwing knives, axes, and other projectiles. Quick deployment tools from the belt. Passive bonuses: thrown weapon damage, throw speed.

| Ability | Description |
|---------|-------------|
| Flick | Fast knife throw, low damage, quick cooldown. The quick option. |
| Chuck | Heavy throw (axe), higher damage, slower. For bigger targets. |
| Fan | Multiple projectiles in spread arc. Group coverage. |
| Ricochet | Bounces between enemies. The trick shot. |
| Frost Blade | Cold-imbued throw, slows target. Tinkered. |

#### Firearms

Pistols, rifles, and other guns. The loud option. Passive bonuses: firearm damage, reload speed.

| Ability | Description |
|---------|-------------|
| Quick Draw | Fast shot from holster, short range. Surprise encounters. |
| Bead | Aimed shot, high accuracy. Draw a bead on 'em. |
| Spray | Multiple rapid shots, spread increases. Suppressive. |
| Snipe | Long-range precision, high damage, long cooldown. THE shot. |
| Shock Round | Lightning-imbued bullet, chains to nearby enemy. Tinkered. |

#### CQC

Close Quarters Combat. The backup plan -- when prey gets too close, survive and get back to range. Passive bonuses: melee block chance, melee counter damage.

| Ability | Description |
|---------|-------------|
| Parry | Deflect incoming melee attack. Buy time. |
| Hunker | Reduce damage with offhand buckler. Absorb what you can't dodge. |
| Riposte | Counter-strike after parry/guard. Punish their approach. |
| Shiv | Quick dirty stab, chance to stagger. Create an opening to escape. |

### Survival

What the wilderness teaches you. Preparation, observation, and engineering solutions.

#### Awareness

The ghillie suit. See without being seen. Passive bonuses: evasion chance, detection range. 4 passive + 4 active.

| Ability | Behavior | Description |
|---------|----------|-------------|
| Keen Senses | Passive | Increased detection range |
| Tip Toes | Toggle | Active evasion/concealment effect (turn on/off) |
| Disengage | Active | Step back 1 tile + i-frames. Level-up adds duration, max 1.5s |
| Steady Breathing | Passive | Slight HP recovery, better MP recovery. Sniper calming breath |
| Rangefinding | Passive | Better hit & crit chance while standing still |
| Tracking | Passive | Better movement speed, but decreased range & accuracy while moving/firing |
| Steady Aim | Active | Charge 1-shot. Guaranteed crit & hit, locks into stance. Up to 1.5s charge |
| Weak Spot | Active | Single target. +attack speed +hit chance for 5-10s. Duration from repeated use on same target (10 uses to max) |

Design tensions:
- Rangefinding (better standing) vs Tracking (better moving) -- playstyle choice
- Steady Aim (burst precision) vs Weak Spot (sustained bonus) -- two precision modes
- Tip Toes (sustained evasion) vs Disengage (burst escape) -- two defensive modes

#### Trapping

The patient hunter's toolkit. Area denial, enemy manipulation, ambush setup. Passive bonuses: trap damage, trap duration.

| Ability | Description |
|---------|-------------|
| Snare | Place trap that roots enemies. Hold the prey. |
| Tripwire | Line trap triggers knockdown on contact. Area denial. |
| Decoy | Dummy that draws enemy attention. Misdirection. |
| Bait | Lure that attracts enemies to a specific spot. Sets up kill zones. |
| Ambush | Bonus damage on first strike against unaware enemies. The hunter's advantage. |

#### Sapping

The tinkerer's workshop. Homemade explosives and area denial devices. Passive bonuses: explosive damage, explosive radius.

| Ability | Description |
|---------|-------------|
| Frag | AoE explosion damage. Simple, effective boom. |
| Smoke Bomb | Obscure area, reduces enemy accuracy. Cover for escape or repositioning. |
| Flashbang | AoE blind/stun, brief. Disorient and reposition. |
| Caltrops | Scatter on ground, slows enemies in area. Passive area denial. |
| Sticky Bomb | Attach to enemy, delayed explosion. Targeted demolition. |

---

## Mage

**Categories:** Elemental (4 masteries) + Aether (1 mastery) + Attunement (3 masteries)
**Tab name:** Arcane Spells
**Total: 8 masteries, 33 abilities**

*Class lore and magic philosophy: see [class-lore.md](../world/class-lore.md#mage)*

**Spell acquisition:** Mages learn spells through two methods:
- **Spell books:** Buy or find a spell book -> learn the spell directly (Diablo 1 style)
- **Spell scrolls (osmosis learning):** Use scrolls to gradually internalize a spell through repeated casting

Three states: Unknown -> Learning (scroll progress bar) -> Learned. Unlearned spells show as grayed out "Unknown Spell" in the Abilities tab.

Full spell acquisition design in [magic.md](magic.md).

**Offhand slot:** Grimoires, ward orbs, and magic-enabled defensive items. Passive equipment that enhances existing Skills (e.g., a fire grimoire buffs Fire spells).

**Skill vs Ability roles:**
- **Skill level (e.g., Fire):** Improves *casting* -- cast speed, spell range, mana efficiency
- **Ability level (e.g., Fireball):** Improves the *spell itself* -- damage, AoE, duration, healing amount

### Elemental

Nature manipulation. Mental models built on everyday sensory experience. Low-moderate mana cost.

#### Fire

Heat, flame, combustion. Passive bonuses: fire cast speed, fire spell range, fire mana efficiency.

| Ability | Description |
|---------|-------------|
| Fireball | Projectile explosion, area damage on impact |
| Flame Wall | Line of fire, damages enemies passing through |
| Ignite | Set target ablaze, damage over time |
| Inferno | Large area sustained fire, high mana cost |

#### Water

Cold, frost, tides. Passive bonuses: water cast speed, water spell range, water mana efficiency.

| Ability | Description |
|---------|-------------|
| Frost Bolt | Ice projectile, slows target on hit |
| Freeze | Immobilize target in ice, duration scales with level |
| Tidal Wave | Wide frontal wave, pushes and damages |
| Mist Veil | Obscuring mist, reduces enemy accuracy in area |

#### Air

Wind, lightning, storms. Passive bonuses: air cast speed, air spell range, air mana efficiency.

| Ability | Description |
|---------|-------------|
| Lightning | Fast bolt, high single-target damage |
| Gust | Knockback wind blast, repositions enemies |
| Chain Shock | Lightning jumps between nearby enemies |
| Tempest | Area storm, sustained damage and disruption |

#### Earth

Stone, tremors, petrification. Passive bonuses: earth cast speed, earth spell range, earth mana efficiency.

| Ability | Description |
|---------|-------------|
| Stone Spike | Rock eruption from ground, single-target |
| Quake | Area tremor, damages and staggers nearby |
| Petrify | Turn target to stone temporarily, hard CC |
| Earthen Armor | Coat self in stone, temporary damage absorption |

### Aether

Cosmic force -- light and dark as two expressions of one phenomenon. Star and black hole. Push and pull. High mana cost, limited but powerful.

#### Aether

Light (outward: energy, creation, welding-style restoration) and Dark (inward: gravity, consumption, compression) under one mastery. Passive bonuses: aether cast speed, aether spell range, aether mana efficiency.

| Ability | Direction | Description |
|---------|-----------|-------------|
| Nova | Light | Radiant energy burst around caster, AoE damage |
| Weld | Light | Burst heal -- fuses wounds shut with raw energy. Expensive, powerful |
| Purify | Light | Cleanse all debuffs and status effects with purifying energy |
| Drain | Dark | Gravitational pull on target's life force, heals caster |
| Singularity | Dark | Gravity well at target location, pulls enemies in and damages over time |

**Purify vs Cleanse:** Purify (Aether) removes ALL debuffs including magical curses, high cost. Cleanse (Attunement: Restoration) removes physical ailments only (poison, bleed), low cost.

**Earthen Armor vs Barrier:** Earthen Armor is literal stone coating (nature manipulation). Barrier is a pure mana shield (internal magic). Different sources, can stack.

### Attunement

The science of internal mana. Training the brain and body to process magic better. Low-moderate mana cost.

#### Restoration

Body defense and self-repair. Passive bonuses: damage resistance, HP regeneration.

| Ability | Description |
|---------|-------------|
| Mend | Quick self-heal, low mana cost, short cooldown |
| Barrier | Magical shield that absorbs incoming damage |
| Cleanse | Remove physical ailments (poison, bleed, burn) |
| Regeneration | Sustained HP recovery over time, longer duration |

#### Amplification

Neural enhancement -- expanding the brain's capacity to channel magic. Passive bonuses: max mana, mana regeneration rate.

| Ability | Description |
|---------|-------------|
| Mana Surge | Burst mana recovery, cooldown-based |
| Quick Cast | Temporarily reduce cast time of all spells |
| Resonance | Boost damage of an attuned element |
| Focus Channel | Reduce mana cost of all spells while stationary |

#### Overcharge

Push the nervous system beyond safe limits -- massive power at bodily cost. The Mage's "berserk" equivalent. Passive bonuses: overcharge duration, reduced overcharge self-damage.

| Ability | Description |
|---------|-------------|
| Neural Burn | Greatly boost spell damage, drains HP over time |
| Mana Frenzy | Eliminate mana costs temporarily, HP damage per cast instead |
| Pain Gate | Convert incoming damage into mana, risk/reward tradeoff |
| Last Resort | Near death -> massively amplify all abilities for a short burst |

---

## UI Tabs

### Skills Tab (Tab 3)

- Header: "SKILLS" + "SP: N available"
- Class masteries grouped by category
- Each mastery row: name, level, XP bar, passive bonus value, [+] allocate SP button
- Cross-tab link: each mastery shows which Abilities it unlocks
- Separator + "INNATE" sub-section: Haste, Sense, Fortify, Armor

### Abilities Tab (Tab 4, class-specific)

Tab label changes per class:
- Warrior: **Warrior Arts**
- Ranger: **Ranger Crafts**
- Mage: **Arcane Spells**

Content:
- Header: class-specific name + "AP: N available"
- Abilities grouped by parent Skill mastery
- Each ability row: name, level, XP bar, mana cost, cooldown, [+] allocate AP, [hotbar] assign
- Locked abilities: grayed out, "Requires [Skill Name] Lv.1"
- Mage-specific: unlearned spells show name + "Unknown Spell"
- Cross-tab link: each ability shows parent Skill mastery level + bonus

See [pause-menu-tabs.md](../ui/pause-menu-tabs.md) for full tab layout.

---

## Resolved Questions

| Question | Decision |
|----------|----------|
| Passive bonuses per level? | Universal formula: `skill_level * multiplier * (100 / (skill_level + 100))`. Same DR curve as stats. |
| Diminishing returns? | Same K=100 curve as character stats. Consistent across all systems. |
| How are Skills leveled? | Both: use-based XP (automatic) + SP allocation (manual). Simultaneous paths. |
| How are Abilities leveled? | Both: use-based XP (automatic) + AP allocation (manual). Simultaneous paths. |
| Ability tuning numbers? | Defined during implementation phase. Structure and formulas are locked; tuning numbers are not. |
| Unlock more Abilities at higher Skill? | No. All Abilities unlock at parent Skill level 1. Depth from leveling, not gating. |
| Weapon requirements? | Abilities require matching weapon. Skill passive bonuses are always active. |
| Mage spell acquisition? | Defined in [magic.md](magic.md) -- spell books (direct) and scroll osmosis (learn by repeated use). |
| Shared type framework? | No. Each Ability is individually coded. No Passive/Toggle/Active base classes. |
| Skill-Ability relationship? | Reactive/pull. Ability reads from parent Skill. All logic lives in the Ability. |
| Point pools? | Separate: SP (Skill Points) for Skills, AP (Ability Points) for Abilities. |
| AP sources? | 3: leveling (~60%), combat milestones (~25%), use-based per-category (~15%). |
| Tab layout? | 8 tabs. Tab 3 = Skills, Tab 4 = class-specific Abilities (Warrior Arts / Ranger Crafts / Arcane Spells). |
| Innate skills? | 4: Haste, Sense, Fortify, Armor. Armor is always-on passive with class-specific names. |
| Synergy bonuses? | Yes -- Skill mastery thresholds unlock bonuses for child Abilities. Details in synergy-bonuses.md (TBD). |
| Ability affinity? | Yes -- cosmetic visual flair at use-based milestones. Details in ability-affinity.md (TBD). |

## Acceptance Criteria

- [ ] All three classes have full Skill + Ability trees implemented with correct hierarchy
- [ ] Skills provide passive bonuses using the diminishing returns formula
- [ ] Abilities unlock at parent Skill level 1
- [ ] Use-based XP tracks for both Abilities (direct) and parent Skills (indirect)
- [ ] SP and AP are separate pools with separate allocation UI
- [ ] AP comes from 3 sources: leveling, combat milestones, use-based per-category
- [ ] Weapon equipment requirements enforced for Abilities, not for Skill passive bonuses
- [ ] Innate Skills (Haste, Sense, Fortify, Armor) available to all classes
- [ ] Armor Innate has class-specific names (Ironhide/Nimbleguard/Spellweave)
- [ ] Skills tab shows SP counter, masteries by category, cross-tab links to Abilities
- [ ] Abilities tab shows AP counter, abilities by parent mastery, hotbar assignment
- [ ] Abilities tab label changes per class (Warrior Arts / Ranger Crafts / Arcane Spells)
- [ ] Mage unlearned spells show as grayed "Unknown Spell"
- [ ] Infinite leveling with no caps on Skills or Abilities
- [ ] Each Ability is individually coded with no shared type framework
