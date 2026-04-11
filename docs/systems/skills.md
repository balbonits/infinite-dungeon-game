# Skills

## Summary

Each class has a unique skill tree organized into **categories**, **base skills**, and **specific skills**. All skills use infinite leveling. Base skills provide passive bonuses and gate access to their specific skills.

Inspired by Project Zomboid's hierarchical skill system, adapted for infinite progression.

## Current State

**Spec status: LOCKED.** Skill hierarchy, leveling model, passive bonus formulas, and skill XP system are defined. All three class skill trees are designed with full skill lists. Individual skill tuning numbers (exact cooldowns, damage values, ranges) are implementation-phase balancing — the formulas and structure are locked.



## Skill System Design

### Hierarchy

```
Category (organizational label, not a skill — has no level)
  └── Base Skill (leveled infinitely, provides passive bonuses, gates specific skills)
        └── Specific Skill (unlocked at base skill level 1, leveled infinitely)
```

Two layers of actual skills. Categories are purely organizational — they group base skills visually but have no levels or stats of their own.

### Progression Model

1. **Base skills** are the entry point. The player levels a base skill (e.g., "Unarmed") through use and XP investment.
2. When a base skill reaches **level 1**, all specific skills underneath it **unlock immediately**.
3. Each specific skill is then leveled independently and infinitely.
4. The base skill continues to be leveled infinitely alongside its specific skills.

**Hybrid leveling:** Skills gain XP through two paths:
- **Use-based XP** — performing actions with a skill earns XP toward that skill (e.g., swinging a sword levels Bladed, casting fire spells levels Fire)
- **Skill points on level-up** — on each character level-up, the player receives skill points to allocate freely to any base or specific skill

Both paths feed progression simultaneously.

**Base skills provide two things:**
- **Passive bonuses** that scale with level (e.g., Unarmed level 10 gives a passive unarmed damage bonus)
- **Gating** — specific skills require at least level 1 in the parent base skill

**Specific skills** are the actionable abilities used in combat. Each level improves the skill's stats (damage, cooldown, range, duration, etc.). Exact per-level scaling is implementation-phase balancing — the formula structure is locked.

### Base Skill Passive Bonus Formula

Every base skill provides a passive bonus that scales with its level. All base skills use the same formula structure, with a class-appropriate multiplier:

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

**Example — Warrior Bladed base skill at level 25:**
- Passive damage bonus: `25 * 1.5 * (100 / 125)` = **30.0%** bonus bladed weapon damage
- Passive crit chance: `25 * 0.5 * (100 / 125)` = **10.0%** bladed crit chance

These bonuses are always active while the skill is at that level — no activation needed.

### Skill XP System

Skills gain XP through two simultaneous paths:

#### Use-Based XP

Every time a skill is used in combat, it earns XP toward that skill's next level.

```
skill_xp_per_use(skill) = base_xp_per_use * floor_multiplier
```

| Skill Type | Base XP Per Use |
|-----------|----------------|
| Base skill (passive — levels when any child specific skill is used) | 5 |
| Specific skill (active ability) | 10 |
| Innate skill (per second while active) | 2 |

`floor_multiplier` = same as enemy XP floor multiplier from [leveling.md](leveling.md): `1 + (floor - 1) * 0.5`

Using deeper-floor skills earns more skill XP, keeping skill leveling relevant at all stages.

#### Skill Point Allocation

On level-up, the player receives skill points (2 per level, 3 at milestones). Each point can be allocated to any base or specific skill to grant a flat XP boost:

```
xp_from_skill_point = 50 * (1 + target_skill_level * 0.1)
```

This scales with the target skill's current level — points invested in higher-level skills give proportionally more XP, preventing "one point = one level" at any stage.

#### Skill Level-Up Formula

```
skill_xp_to_next_level(skill_level) = floor(skill_level^2 * 20)
```

Same quadratic shape as character XP but with a smaller constant (20 vs 45), so skill levels come faster than character levels.

| Skill Level | XP Required |
|-------------|------------|
| 0 → 1 | 0 (instant — all skills start available) |
| 1 → 2 | 20 |
| 5 → 6 | 500 |
| 10 → 11 | 2,000 |
| 25 → 26 | 12,500 |
| 50 → 51 | 50,000 |

### Weapon Equipment Requirements

**Weapon-type base skills require a matching weapon equipped to use their specific skills.**

| Base Skill | Required Equipment |
|-----------|-------------------|
| Unarmed | No weapon equipped (or fists) |
| Bladed | Sword, axe, or dagger in main hand |
| Blunt | Club, hammer, or mace in main hand |
| Polearms | Spear, halberd, or staff in main hand |
| Shields | Shield in off hand |
| Drawn | Bow or crossbow in main hand |
| Thrown | Throwing weapon in main hand |
| Firearms | Firearm in main hand |
| Melee (Ranger) | Defensive offhand weapon equipped |

**Mind/Instinct/Arcane/Conduit skills have no weapon requirement** — they are mental/magical and work regardless of equipment.

**Base skill passive bonuses are always active** even without the matching weapon — they represent general proficiency. Only the specific skills (active abilities) require the weapon.

### Specific Skill Unlock Rules

**All specific skills under a base skill unlock at base level 1.** Higher base levels do NOT unlock additional specific skills. The full roster is available immediately once the player puts their first point into the base skill.

**Design rationale:** This encourages breadth of experimentation. Players can try all abilities early and discover which ones fit their build. Depth comes from leveling individual specific skills higher, not from gating access.

### Infinite Leveling

Every base skill and specific skill has **no level cap**. Consistent with the game's infinite dungeon theme, players can always grind deeper into any skill. Combined with diminishing returns on stats (see [stats.md](stats.md)), each level is always rewarding but with decreasing marginal gains.

### Class-Locked

Class skills are strictly locked to their class. No skill sharing between classes. See [classes.md](classes.md) for class design.

**Exception: Innate skills.** There is a universal Innate skill category (Haste, Sense, Fortify) available to all classes. These are species-level magicule abilities, not class skills. See [magic.md](magic.md) for full Innate skill design.

**Innate skill UI:** Innate skills display in separate sub-panels per category (one sub-panel for each innate skill), distinct from the class skill tree panel. This prevents visual clutter and reinforces that innate skills are a separate system from class combat skills.

---

## Warrior Skills

The Warrior is the melee-focused class. Power comes from close combat mastery.

**Categories:** Body, Mind

### Body

Physical combat techniques. Five base skills covering all melee fighting styles.

#### Unarmed

Hand-to-hand combat. Passive bonuses: unarmed damage, unarmed attack speed.

| Specific Skill | Description |
|----------------|-------------|
| Punch | Fast straight strikes, high attack speed |
| Kick | Leg strikes, knockback effect |
| Grapple | Holds and throws, close-range crowd control |
| Elbow/Knee | Short-range burst damage |

#### Bladed

Swords, axes, and daggers. Passive bonuses: bladed weapon damage, bladed critical chance.

| Specific Skill | Description |
|----------------|-------------|
| Slash | Wide arc swing, can hit multiple enemies |
| Thrust | Precision stab, high single-target damage |
| Cleave | Heavy overhead strike, hits multiple enemies |
| Parry | Deflect incoming melee attack, opens a counter window |

#### Blunt

Clubs, hammers, and maces. Passive bonuses: blunt weapon damage, stun chance.

| Specific Skill | Description |
|----------------|-------------|
| Smash | Heavy overhead hit, bonus damage vs armored enemies |
| Sweep | Low arc swing, knockback effect |
| Crush | Charged hit, chance to stun |
| Shatter | Break enemy guard or shields |

#### Polearms

Spears, halberds, and quarterstaffs. Passive bonuses: polearm damage, attack range.

| Specific Skill | Description |
|----------------|-------------|
| Thrust | Long-range poke, keeps distance from enemies |
| Sweep | Wide arc, crowd control |
| Brace | Set weapon against charging enemies, counter-charge bonus |
| Vault | Use polearm to reposition or dodge |

#### Shields

Shield techniques. Passive bonuses: block chance, block damage reduction.

| Specific Skill | Description |
|----------------|-------------|
| Block | Active damage reduction stance |
| Shield Bash | Offensive shield strike, staggers enemy |
| Deflect | Reflect incoming projectiles |
| Bulwark | Sustained defensive stance, increased defense at the cost of movement speed |

### Mind

Mental discipline for a solo warrior. Two base skills covering internal resilience and external presence.

#### Inner

Self-focused mental discipline. Passive bonuses: damage resistance, HP regeneration.

| Specific Skill | Description |
|----------------|-------------|
| Battle Focus | Heightened awareness, increases accuracy and critical hit chance |
| Pain Tolerance | Passive damage reduction |
| Second Wind | Self-heal over time, cooldown-based |
| Iron Will | Resist debuffs and status effects |

#### Outer

Enemy-focused mental presence. Passive bonuses: enemy stat debuff aura range, debuff potency.

| Specific Skill | Description |
|----------------|-------------|
| War Cry | AoE shout that weakens nearby enemies |
| Intimidate | Single-target fear or stagger |
| Menacing Presence | Passive aura that debuffs nearby enemy stats |
| Battle Roar | AoE shout that slows enemy attack speed |

---

## Ranger Skills

The Ranger is the ranged combat class (formerly "Archer"). Power comes from distance, precision, and preparation. Not as physically dominant as the Warrior, but compensates with mental calculation — range, trajectory, and situational awareness.

**Categories:** Arms, Instinct

**Ammo system:** Ammo is unlimited, but requires a "magazine" item equipped (quivers, mags, projectile bags, bandoliers). These are purchased, not crafted from drops.

### Arms

Ranged weapon mastery and close-range defense. Four base skills covering all Ranger fighting styles.

#### Drawn

Bows and crossbows. Passive bonuses: drawn weapon damage, draw speed.

| Specific Skill | Description |
|----------------|-------------|
| Power Shot | High-damage single shot, slow draw |
| Rapid Fire | Fast consecutive shots, reduced damage per hit |
| Arc Shot | Arcing trajectory, hits behind cover or over obstacles |
| Pin Shot | Pins enemy in place briefly, movement denial |

#### Thrown

Throwing knives, axes, and other projectiles. Passive bonuses: thrown weapon damage, throw speed.

| Specific Skill | Description |
|----------------|-------------|
| Knife Throw | Fast, low-damage throw, quick cooldown |
| Axe Throw | Slower, heavier throw, higher damage |
| Fan Throw | Multiple projectiles in a spread arc |
| Bounce Shot | Projectile ricochets between nearby enemies |

#### Firearms

Pistols, rifles, and other guns. Passive bonuses: firearm damage, reload speed.

| Specific Skill | Description |
|----------------|-------------|
| Quick Draw | Fast shot from holster, short-range |
| Steady Shot | Aimed shot, high accuracy, moderate damage |
| Burst Fire | Multiple rapid shots, spread increases |
| Snipe | Long-range precision shot, high damage, long cooldown |

#### Melee

Defensive offhand weapons — knives, small bucklers, claw gauntlets. Passive bonuses: melee block chance, melee counter damage. These are offhand-only weapons that don't interfere with the Ranger's primary ranged weapon.

| Specific Skill | Description |
|----------------|-------------|
| Parry | Deflect incoming melee attack |
| Block | Reduce damage with offhand buckler or gauntlet |
| Riposte | Counter-attack after a successful parry or block |
| Disarm | Knock weapon from enemy's grip, reducing their damage temporarily |

### Instinct

Gut-level reactions and tactical sense. Three base skills covering offensive calculation, defensive awareness, and tactical preparation.

#### Precision

Offensive mental calculation — accuracy, targeting, ballistics. Passive bonuses: accuracy, critical hit chance.

| Specific Skill | Description |
|----------------|-------------|
| Steady Aim | Increases accuracy while stationary, stacking buff |
| Weak Spot | Identify and target enemy vulnerabilities, bonus damage |
| Range Calc | Improved damage at long range, reduced falloff |
| Lead Shot | Predict enemy movement, hit moving targets more reliably |

#### Awareness

Defensive situational reading — threat detection, evasion, positioning. Passive bonuses: evasion chance, threat detection range. Evasion skills are equipment-independent — pure skill, not gear. Melee offhand equipment can bonus these skills but is not required.

| Specific Skill | Description |
|----------------|-------------|
| Threat Sense | Detect enemies before they enter visual range |
| Dodge Roll | Quick roll to evade attacks, brief invulnerability |
| Disengage | Create distance from nearby enemies, movement burst |
| Tumble | Recover from knockback or stagger, reduced stun duration |

#### Trapping

Tactical preparation — area denial, enemy manipulation, ambush setup. Passive bonuses: trap damage, trap duration.

| Specific Skill | Description |
|----------------|-------------|
| Snare | Place a trap that roots enemies in place |
| Tripwire | Line trap that triggers knockdown on contact |
| Decoy | Place a dummy that draws enemy attention |
| Ambush | Bonus damage on first attack against unaware enemies |

## Mage Skills

The Mage is the spell-based class. Power comes entirely from magic and mental mastery. The Mage's hypothesis: mana is rooted in the brain and nervous system — enhancing these bodily systems directly improves magical ability.

**Categories:** Arcane, Conduit

**Spell acquisition:** Mages learn spells through two methods:
- **Spell books:** Buy or find a spell book → learn the spell directly (Diablo 1 style)
- **Spell scrolls (osmosis learning):** Consume one scroll to establish a base understanding of the spell, then consume additional copies of the same scroll to fully learn it. Learning by repeated casting and experience.

Full spell acquisition system design is deferred to a dedicated doc (crosses into items/inventory territory).

**Offhand slot:** Grimoires, ward orbs, and magic-enabled defensive items. These are passive equipment that enhance existing skills (e.g., a fire grimoire buffs Fire spells). No dedicated base skill — offhand mastery comes from the skills it enhances.

**Base skill vs specific skill roles:**
- **Base skill level (e.g., Fire):** Improves *casting* — cast speed, spell range, mana efficiency
- **Specific skill level (e.g., Fireball):** Improves the *spell itself* — damage, area of effect, duration, healing amount, etc.

This mirrors Warrior/Ranger where base skills improve general weapon handling and specific skills improve individual techniques.

### Arcane

Spell mastery across six elemental schools. Six base skills — the most of any class, reflecting the breadth of magical knowledge.

Spells are divided into two groups:
- **Elemental** (physical forces): Fire, Water, Air, Earth
- **Null** (non-physical forces): Light, Dark

#### Fire

Fire magic — heat, flame, combustion. Passive bonuses: fire cast speed, fire spell range, fire mana efficiency.

| Specific Skill | Description |
|----------------|-------------|
| Fireball | Projectile explosion, area damage on impact |
| Flame Wall | Line of fire that damages enemies passing through |
| Ignite | Set target ablaze, damage over time |
| Inferno | Large area sustained fire, high mana cost |

#### Water

Water and ice magic — cold, frost, tides. Passive bonuses: water cast speed, water spell range, water mana efficiency.

| Specific Skill | Description |
|----------------|-------------|
| Frost Bolt | Ice projectile, slows target on hit |
| Freeze | Immobilize target in ice, duration scales with level |
| Tidal Wave | Wide frontal wave, pushes and damages enemies |
| Mist Veil | Create obscuring mist, reduces enemy accuracy in area |

#### Air

Air and electricity magic — wind, lightning, storms. Passive bonuses: air cast speed, air spell range, air mana efficiency.

| Specific Skill | Description |
|----------------|-------------|
| Lightning | Fast bolt, high single-target damage |
| Gust | Knockback wind blast, repositions enemies |
| Chain Shock | Lightning jumps between nearby enemies |
| Tempest | Area storm, sustained damage and disruption |

#### Earth

Earth and stone magic — rock, tremors, petrification. Passive bonuses: earth cast speed, earth spell range, earth mana efficiency.

| Specific Skill | Description |
|----------------|-------------|
| Stone Spike | Sharp rock eruption from ground, single-target |
| Quake | Area tremor, damages and staggers nearby enemies |
| Petrify | Turn target to stone temporarily, hard crowd control |
| Earthen Armor | Coat self in stone, temporary damage absorption |

#### Light

Light and energy magic — radiance, energy blasts, purification. Offensive light spells are energy-based (concentrated energy blasts, not "holy" themed). Passive bonuses: light cast speed, light spell range, light mana efficiency.

| Specific Skill | Description |
|----------------|-------------|
| Energy Blast | Concentrated energy projectile, high direct damage |
| Radiance | Burst of light energy around caster, damages nearby enemies |
| Heal | Restore HP to self, amount scales with level |
| Purify | Remove debuffs and negative status effects from self |

#### Dark

Shadow and void magic — draining, cursing, darkness. Passive bonuses: dark cast speed, dark spell range, dark mana efficiency.

| Specific Skill | Description |
|----------------|-------------|
| Drain Life | Steal HP from target, heals self for a portion |
| Curse | Debuff target, reduces their damage and defense |
| Shadow Bolt | Dark projectile, high damage, slow cast |
| Void Zone | Area of darkness that damages enemies standing in it |

### Conduit

The body as a vessel for magic. A Mage's physical training isn't about combat strength — it's about defense, healing, and enhancing the nervous system to channel more magic. Think Monk or Priest physicality, not Warrior.

Three base skills covering bodily restoration, neural enhancement, and dangerous overcharge.

#### Restoration

Body defense and self-repair. Passive bonuses: damage resistance, HP regeneration.

| Specific Skill | Description |
|----------------|-------------|
| Mend | Quick self-heal, low mana cost, short cooldown |
| Barrier | Magical shield that absorbs incoming damage |
| Cleanse | Remove physical ailments (poison, bleed, etc.) |
| Regeneration | Sustained HP recovery over time, longer duration than Mend |

#### Amplification

Neural enhancement — expanding the brain's capacity to channel magic. Passive bonuses: max mana, mana regeneration rate.

| Specific Skill | Description |
|----------------|-------------|
| Mana Surge | Burst of mana recovery, cooldown-based |
| Quick Cast | Temporarily reduce cast time of all spells |
| Attunement | Increase elemental affinity, boosting damage of attuned element |
| Focus Channel | Reduce mana cost of all spells while stationary |

#### Overcharge

Push the nervous system beyond safe limits — massive power at bodily cost. The Mage's "berserk" equivalent. Passive bonuses: overcharge duration, reduced overcharge self-damage.

| Specific Skill | Description |
|----------------|-------------|
| Neural Burn | Greatly boost spell damage, drains HP over time |
| Mana Frenzy | Eliminate mana costs temporarily, take HP damage per cast instead |
| Pain Conduit | Convert incoming damage into mana, risk/reward tradeoff |
| Last Resort | When near death, massively amplify all abilities for a short burst |

---

## Resolved Questions

| Question | Decision |
|----------|----------|
| Passive bonuses per level? | Universal formula: `skill_level * multiplier * (100 / (skill_level + 100))`. Same DR curve as stats. |
| Diminishing returns? | Same K=100 curve as character stats. Consistent across all systems. |
| How are skills leveled? | Both: use-based XP (automatic) + skill point allocation (manual). Simultaneous paths. |
| Specific skill cooldowns/ranges? | Defined during implementation phase. Structure and formulas are locked; tuning numbers are not. |
| Unlock more skills at higher base? | No. All specific skills unlock at base level 1. Depth from leveling, not gating. |
| Weapon requirements? | Specific skills require matching weapon. Base passive bonuses are always active. |
| Mage spell acquisition? | Defined in [magic.md](magic.md) — spell books (direct) and scroll osmosis (learn by repeated use). |
