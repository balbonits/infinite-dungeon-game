# Skills & Abilities Systems

**ARCHIVED** — Working document for the Skills/Abilities redesign. Research, decisions, and class breakdowns.

This document served as the design workspace. The locked specs are now the source of truth:
- [skills.md](skills.md) — full Skills & Abilities spec (all class trees, formulas, architecture)
- [point-economy.md](point-economy.md) — SP/AP rates and sources
- [class-lore.md](../world/class-lore.md) — class backstories and magic philosophy

**Status:** ARCHIVED. All three classes locked. All specs updated.

---

## Cross-Game Research

Analysis of 15+ games on how they organize skills, abilities, and spells.

### Game-by-Game Findings

| Game | "Skills" means | "Abilities" means | Passive/Active Split |
|------|---------------|-------------------|---------------------|
| **Project Zomboid** (our inspiration) | Passive proficiency only (no active skills exist) | N/A | All passive |
| **Diablo 2** | Everything (mixed passive + active in themed trees) | N/A | No separation |
| **Diablo 3** | Umbrella, explicitly splits "Active Skills" vs "Passive Skills" | N/A | Hard separation |
| **Diablo 4** | Both in one tree, distinguished by node shape (square=active, circle=passive) | N/A | Soft separation (D4 later removed passives entirely) |
| **Path of Exile** | Passive tree (1,325+ nodes) | Skill Gems (socketed actives, separate system) | Completely separate systems |
| **Last Epoch** | Active combat abilities (S key) | N/A | Hard split: Skills (S) vs Passives (P), different keybinds |
| **Grim Dawn** | Mixed active+passive in class Masteries | N/A | Masteries (class) + Devotion (universal passives) = two layers |
| **WoW** | N/A | Class/spec combat powers ("Talents") | Class tree (utility) + Spec tree (combat identity) |
| **FFXIV** | N/A | "Actions" (active), "Traits" (passive), "Role Actions" (shared) | 3-tier split, crystal clear |
| **Guild Wars 2** | Weapon/utility actives | N/A | Skills (active, on skill bar) vs Traits (passive, in Build screen) |
| **Skyrim** | Passive proficiency (level through use) | N/A | Skills/Perks (passive) vs Spells (active, separate Magic menu) |
| **Elden Ring** | Weapon Arts (active, bound to weapons) | N/A | Stats vs Skills vs Sorceries (3 separate systems) |
| **BG3/D&D** | N/A | Class Features | Features vs Spells vs Feats (organized by source) |
| **DOS2** | Active spells/attacks | Passive proficiencies | INVERTED naming — "Abilities" = passive, "Skills" = active |
| **Monster Hunter** | Passive gear bonuses only | N/A | Skills purely passive; combat = fixed weapon moveset |
| **RuneScape** | Passive proficiency (level 1-99 through use) | N/A | All passive |
| **ESO** | Mixed in "Skill Lines" (active + passive per line) | N/A | Active/passive mixed but clearly labeled within each line |
| **Torchlight** | Mixed active+passive in D2-style trees | N/A | No separation, follows D2 model |

### Key Findings

1. **"Skill" = something you get better at.** When players hear "skill," they intuitively think proficiency. This aligns with PZ, Skyrim, RuneScape. Our passive masteries fit this perfectly.

2. **"Ability" = something you can do.** When players hear "ability," they think action. This aligns with WoW, FFXIV. Our active combat actions fit this perfectly.

3. **PZ has NO active skills.** Our stated inspiration has entirely passive proficiency skills. Our "base skills" are the PZ part. Our "specific skills" (active combat) come from the ARPG side (Diablo, PoE). These are two different design philosophies that were jammed into one tab.

4. **Deeper games separate passives from actives.** PoE, Last Epoch, FFXIV, Skyrim — all hard-separate. Games that mix them (D2, Grim Dawn) work but create confusion about where to invest.

5. **No game uses "Skills" + "Abilities" as exact tab labels.** But the concepts are universal. Most common splits: "Skills vs Passives," "Actions vs Traits," "Active Skills vs Passive Skills."

6. **DOS2 is the outlier.** It uses "Abilities" for passives and "Skills" for actives — the opposite of everyone else. Cautionary tale, but we follow the majority convention.

### Naming Conventions Across Games

| Term | Most Common Meaning | Used By |
|------|-------------------|---------|
| Skills | Umbrella for everything OR passive proficiency levels | D2, PZ, Skyrim, RuneScape, GW2, ESO |
| Abilities | Active things you press in combat | WoW, FFXIV, D3 |
| Spells | Magic-specific active abilities | D&D, Skyrim, Elden Ring |
| Talents | Player-chosen customization nodes | WoW, D4 |
| Perks | Permanent passive bonuses from a tree | Skyrim |
| Traits | Always-on passive bonuses | FFXIV, GW2 |
| Feats | Optional permanent upgrades at key levels | D&D/BG3 |

---

## Taxonomy

### Skills (Passive Masteries)

- **What they are:** Passive masteries you improve over time
- **What they were:** "Base skills" in the old spec
- **Examples:** Bladed, Bowmanship, Fire, Shields, Awareness
- **How they level:** Use-based XP + Skill Point (SP) investment
- **What they provide:** Passive bonuses (damage %, cast speed %, block chance, etc.)
- **What they gate:** Each Skill unlocks its child Abilities when it reaches level 1
- **Universal:** Innate skills (Haste, Sense, Fortify, Armor) live here too

### Abilities (Active Combat Actions)

- **What they are:** Active combat actions you use in fights
- **What they were:** "Specific skills" in the old spec
- **Examples:** Slash, Fireball, Dodge Roll, Shield Bash, Keen Senses
- **How they level:** Use-based XP + Ability Point (AP) investment
- **What they have:** Mana cost, cooldown, range, damage — each varies per ability
- **How they're used:** Assigned to the 4-slot hotbar (active abilities), or always-on (passive abilities)
- **Class-locked:** Each class has its own set

### The Relationship

Each Ability belongs to exactly one parent Skill. The Skill provides passive mastery; the Ability is the active expression of that mastery. Using an Ability in combat grants XP to both the Ability and its parent Skill.

**Architecture:** Reactive/pull pattern. The Ability looks UP to its parent Skill to read data (level, passive bonus values). The Ability then adjusts its own values based on what it reads. All logic lives in the Ability — the Skill is just a data source, it doesn't know or care what its children do with its data.

**No shared type framework.** Each ability is individually coded with its own specific behavior. No Passive/Toggle/Active base classes or type enums. This avoids coupling where modifying one ability's framework breaks others. Front-load individual work now so the system stays maintainable as abilities grow and change independently.

---

## Architecture Decisions

| Decision | Choice |
|----------|--------|
| Tab names | **Warrior Arts** / **Ranger Crafts** / **Arcane Spells** |
| Mage categories | **Elemental** (nature) / **Aether** (cosmic) / **Attunement** (internal mana) |
| Innate skills location | **Skills tab** as universal masteries |
| Point pools | **Separate**: SP (Skill Points) & AP (Ability Points) |
| AP sources | Leveling (primary ~60%) + combat milestones (~25%) + use-based per-category (~15%) |
| AP use-tracking | **Per-category** (Body AP, Survival AP, etc. — earned by using abilities in that category) |
| Ability count per mastery | **3-8** (variable) |
| Unlearned Mage spells | Show name, grayed out, "Unknown Spell" |
| Tab theming | **Yes** — each class gets distinct visual styling on Abilities tab |
| Cross-tab linking | **Yes** — Skills tab shows gated Abilities, Abilities tab shows parent Skill |
| Synergy bonuses | **Yes** — Skill mastery thresholds unlock bonuses for child Abilities |
| Innate synergy | **Yes** — Innate synergies affect ALL abilities |
| Ability affinity | **Yes** — cosmetic visual flair at use-based milestones |
| Build presets | **Yes** — deferred (later feature) |
| Ability preview | **Yes** — stat comparison before spending AP |

### Pause Menu Tabs (8)

`[Inventory] [Equipment] [Skills] [Warrior Arts*] [Quests] [Ledger] [Stats] [System]`

Tab 4 label changes per class:
- Warrior: **Warrior Arts**
- Ranger: **Ranger Crafts**
- Mage: **Arcane Spells**

### Skills Tab Content

- Header: "SKILLS" + "SP: N available"
- Class masteries grouped by category
- Each mastery row: name, level, XP bar, passive bonus value, [+] allocate SP button
- Cross-tab link: each mastery shows which Abilities it unlocks
- Separator + "INNATE" sub-section: Haste, Sense, Fortify, Armor

### Abilities Tab Content

- Header: class-specific name + "AP: N available"
- Abilities grouped by parent Skill mastery
- Each ability row: name, level, XP bar, mana cost, cooldown, [+] allocate AP, [hotbar] assign
- Locked abilities: grayed out, "Requires [Skill Name] Lv.1"
- Mage-specific: unlearned spells show name + "Unknown Spell"
- Cross-tab link: each ability shows parent Skill mastery level + bonus

---

## Innate Skills (4, All Classes)

| Skill | Type | Warrior | Ranger | Mage | Description |
|-------|------|---------|--------|------|-------------|
| Haste | Toggle (mana drain) | Haste | Haste | Haste | Magicule-enhanced burst of speed + dodge chance |
| Sense | Toggle (mana drain) | Sense | Sense | Sense | Magicule-enhanced perception, detect through walls |
| Fortify | Toggle (mana drain) | Fortify | Fortify | Fortify | Magicule-reinforced body, damage resistance |
| Armor | Always-on passive | **Ironhide** | **Nimbleguard** | **Spellweave** | Armor proficiency, class-specific equipment mastery |

Innate synergies affect ALL abilities at threshold levels (details TBD).

---

## Warrior — LOCKED

**Categories:** Body (6 masteries) + Mind (2 masteries)
**Tab name:** Warrior Arts
**Total: 8 masteries, 33 abilities**

*Class lore and magic philosophy: see [class-lore.md](../world/class-lore.md#warrior)*

### Body

#### Unarmed (4 abilities)

| Ability | Description |
|---------|-------------|
| Punch | Fast straight strikes, high attack speed |
| Kick | Leg strike with knockback |
| Grappling | Holds, throws, pins |
| Elbow Strike | Short-range burst damage |

#### Bladed (4 abilities)

| Ability | Description |
|---------|-------------|
| Slash | Wide arc swing, multi-target |
| Thrust | Precision stab, high single-target |
| Cleave | Heavy overhead, multi-target |
| Parry | Deflect attack, counter window |

#### Blunt (4 abilities)

| Ability | Description |
|---------|-------------|
| Smash | Heavy overhead, bonus vs armored |
| Bump | Blunt thrust, knocks enemy back ~1 tile |
| Crush | Charged hit, chance to stun |
| Shatter | Break enemy guard/shields |

#### Polearms (5 abilities)

| Ability | Description |
|---------|-------------|
| Pierce | Long-range stab, keeps distance |
| Sweep | Horizontal AoE swing |
| Brace | Set against charge, counter bonus |
| Vault | Polearm reposition/dodge |
| Haft Blow | Thrust blunt end for knockback |

#### Shields (4 abilities)

| Ability | Description |
|---------|-------------|
| Block | Active damage reduction stance |
| Shield Bash | Offensive strike, staggers |
| Deflect | Reflect projectiles |
| Bulwark | Sustained defense, reduced movement |

#### Dual Wield (4 abilities)

| Ability | Description |
|---------|-------------|
| Dual Stab | Simultaneous stab, upped crit chance |
| Dual Slash | X-shaped slash, upped bleed chance |
| Spin Attack | Single spin AoE |
| Rapid Combo | 3-strike combo; +1 strike every 5 ability levels, caps at 15 → becomes **Omnislash** (FF7 easter egg) |

### Mind

#### Discipline (4 abilities)

| Ability | Description |
|---------|-------------|
| Focus | Heightened awareness, +accuracy +crit |
| Endure | Damage reduction + debuff resistance (merged Iron Will) |
| Deep Breaths | Self-heal over time, cooldown-based |
| Blood Lust | Kills extend effect. +ATK, +DMG, +SPD, +HP/MP regen, +status resist. BUT -DEF, -MP capacity |

#### Intimidation (4 abilities)

| Ability | Description |
|---------|-------------|
| Shout | AoE weakens nearby enemies |
| Intimidate | Single-target fear/stagger |
| Ugly Mug | Debuff aura on nearby enemies |
| Battle Roar | AoE slows enemy attack speed + chance to stun |

---

## Ranger — LOCKED (pending gameplay refinement)

**Categories:** Weaponry (4 masteries) + Survival (3 masteries)
**Tab name:** Ranger Crafts
**Precision removed** — aim/crit handled by Weaponry mastery passives
**Total: 7 masteries, 37 abilities**

*Class lore and magic philosophy: see [class-lore.md](../world/class-lore.md#ranger)*

### Weaponry

#### Bowmanship (5 abilities)

The identity weapon. Silent, precise. Covers bows + crossbows. No auto-crossbows in the game for now.

| Ability | Description |
|---------|-------------|
| Dead Eye | Aimed shot, high damage, slow draw. The kill shot. |
| Pepper | Fast consecutive shots, reduced per-hit damage. When stealth breaks. |
| Lob | Arcing trajectory, hits behind cover or obstacles |
| Pin | Pins enemy in place, movement denial. The prey doesn't run. |
| Flame Arrow | Fire-imbued shot, DoT on impact. Tinkered. |

#### Throwing (5 abilities)

Quick deployment tools from the belt. Close-mid range solutions.

| Ability | Description |
|---------|-------------|
| Flick | Fast knife throw, low damage, quick cooldown. The quick option. |
| Chuck | Heavy throw (axe), higher damage, slower. For bigger targets. |
| Fan | Multiple projectiles in spread arc. Group coverage. |
| Ricochet | Bounces between enemies. The trick shot. |
| Frost Blade | Cold-imbued throw, slows target. Tinkered. |

#### Firearms (5 abilities)

The loud option. When stealth doesn't matter and you need stopping power.

| Ability | Description |
|---------|-------------|
| Quick Draw | Fast shot from holster, short range. Surprise encounters. |
| Bead | Aimed shot, high accuracy. Draw a bead on 'em. |
| Spray | Multiple rapid shots, spread increases. Suppressive. |
| Snipe | Long-range precision, high damage, long cooldown. THE shot. |
| Shock Round | Lightning-imbued bullet, chains to nearby enemy. Tinkered. |

#### CQC (4 abilities)

Close Quarters Combat. The backup plan — when prey gets too close, survive and get back to range.

| Ability | Description |
|---------|-------------|
| Parry | Deflect incoming melee attack. Buy time. |
| Hunker | Reduce damage with offhand buckler. Absorb what you can't dodge. |
| Riposte | Counter-strike after parry/guard. Punish their approach. |
| Shiv | Quick dirty stab, chance to stagger. Create an opening to escape. |

### Survival

#### Awareness (8 abilities)

The ghillie suit. See without being seen. 4 passive + 4 active.

| Ability | Behavior | Description |
|---------|----------|-------------|
| Keen Senses | Passive | Increased detection range |
| Tip Toes | Toggle | Active evasion/concealment effect (turn on/off) |
| Disengage | Active | Step back 1 tile + i-frames. Level-up adds duration, max 1.5s |
| Steady Breathing | Passive | Slight HP recovery, better MP recovery. Sniper calming breath |
| Rangefinding | Passive | Better hit & crit chance while standing still |
| Tracking | Passive | Better movement speed, but decreased range & accuracy while moving/firing |
| Steady Aim | Active | Charge 1-shot. Guaranteed crit & hit, locks into stance. Stronger = longer charge, up to 1.5s |
| Weak Spot | Active | Single target. +attack speed +hit chance for 5-10s. Level-up increases effect. Duration based on repeated use on same target (10 uses to max) |

Design tensions:
- Rangefinding (better standing) vs Tracking (better moving) — playstyle choice
- Steady Aim (burst precision) vs Weak Spot (sustained bonus) — two precision modes
- Tip Toes (sustained evasion) vs Disengage (burst escape) — two defensive modes

#### Trapping (5 abilities)

The patient hunter's toolkit. Why fight directly when the terrain can do it for you?

| Ability | Description |
|---------|-------------|
| Snare | Place trap that roots enemies. Hold the prey. |
| Tripwire | Line trap triggers knockdown on contact. Area denial. |
| Decoy | Dummy that draws enemy attention. Misdirection. |
| Bait | Lure that attracts enemies to a specific spot. Sets up kill zones. |
| Ambush | Bonus damage on first strike against unaware enemies. The hunter's advantage. |

#### Sapping (5 abilities)

The tinkerer's workshop. Homemade explosives and area denial devices.

| Ability | Description |
|---------|-------------|
| Frag | AoE explosion damage. Simple, effective boom. |
| Smoke Bomb | Obscure area, reduces enemy accuracy. Cover for escape or repositioning. |
| Flashbang | AoE blind/stun, brief. Disorient and reposition. |
| Caltrops | Scatter on ground, slows enemies in area. Passive area denial. |
| Sticky Bomb | Attach to enemy, delayed explosion. Targeted demolition. |

---

## Mage — LOCKED (pending gameplay refinement)

**Categories:** Elemental (4 masteries) + Aether (1 mastery) + Attunement (3 masteries)
**Tab name:** Arcane Spells
**Total: 8 masteries, 33 abilities**

*Class lore and magic philosophy: see [class-lore.md](../world/class-lore.md#mage)*

**Mage-specific mechanics:**
- Spell acquisition via spell books (direct) and scroll osmosis (learning by repeated use)
- Unlearned spells show as grayed out "Unknown Spell" in Abilities tab
- Three states: Unknown → Learning (scroll progress bar) → Learned

### Elemental

Nature manipulation. Mental models built on everyday sensory experience.

#### Fire (4 abilities)

| Ability | Description |
|---------|-------------|
| Fireball | Projectile explosion, area damage on impact |
| Flame Wall | Line of fire, damages enemies passing through |
| Ignite | Set target ablaze, damage over time |
| Inferno | Large area sustained fire, high mana cost |

#### Water (4 abilities)

| Ability | Description |
|---------|-------------|
| Frost Bolt | Ice projectile, slows target on hit |
| Freeze | Immobilize target in ice, duration scales with level |
| Tidal Wave | Wide frontal wave, pushes and damages |
| Mist Veil | Obscuring mist, reduces enemy accuracy in area |

#### Air (4 abilities)

| Ability | Description |
|---------|-------------|
| Lightning | Fast bolt, high single-target damage |
| Gust | Knockback wind blast, repositions enemies |
| Chain Shock | Lightning jumps between nearby enemies |
| Tempest | Area storm, sustained damage and disruption |

#### Earth (4 abilities)

| Ability | Description |
|---------|-------------|
| Stone Spike | Rock eruption from ground, single-target |
| Quake | Area tremor, damages and staggers nearby |
| Petrify | Turn target to stone temporarily, hard CC |
| Earthen Armor | Coat self in stone, temporary damage absorption |

### Aether

Cosmic force — light and dark as two expressions of one phenomenon. Star and black hole. Push and pull. High mana cost, limited but powerful.

#### Aether (5 abilities)

| Ability | Direction | Description |
|---------|-----------|-------------|
| Nova | Light | Radiant energy burst around caster, AoE damage |
| Weld | Light | Burst heal — fuses wounds shut with raw energy. Expensive, powerful |
| Purify | Light | Cleanse all debuffs and status effects with purifying energy |
| Drain | Dark | Gravitational pull on target's life force, heals caster |
| Singularity | Dark | Gravity well at target location, pulls enemies in and damages over time |

**Purify vs Cleanse:** Purify (Aether) removes ALL debuffs including magical curses, high cost. Cleanse (Attunement: Restoration) removes physical ailments only (poison, bleed), low cost. Emergency miracle vs routine maintenance.

**Earthen Armor vs Barrier:** Earthen Armor is literal stone coating (nature manipulation). Barrier is a pure mana shield (internal magic). Different sources, can stack, different visuals.

### Attunement

The science of internal mana. Training the brain and body to process magic better. The Mage's scholarly advantage — understanding what Warriors and Rangers do instinctively.

#### Restoration (4 abilities)

| Ability | Description |
|---------|-------------|
| Mend | Quick self-heal, low mana cost, short cooldown |
| Barrier | Magical shield that absorbs incoming damage |
| Cleanse | Remove physical ailments (poison, bleed, burn) |
| Regeneration | Sustained HP recovery over time, longer duration |

#### Amplification (4 abilities)

| Ability | Description |
|---------|-------------|
| Mana Surge | Burst mana recovery, cooldown-based |
| Quick Cast | Temporarily reduce cast time of all spells |
| Resonance | Boost damage of an attuned element |
| Focus Channel | Reduce mana cost of all spells while stationary |

#### Overcharge (4 abilities)

| Ability | Description |
|---------|-------------|
| Neural Burn | Greatly boost spell damage, drains HP over time |
| Mana Frenzy | Eliminate mana costs temporarily, HP damage per cast instead |
| Pain Gate | Convert incoming damage into mana, risk/reward tradeoff |
| Last Resort | Near death → massively amplify all abilities for a short burst |

---

## Synergy Bonuses — TBD

When a Skill mastery reaches threshold levels, ALL its child Abilities receive a bonus.

Proposed thresholds (to be refined per mastery):

| Skill Level | Synergy Bonus (template) |
|-------------|------------------------|
| Lv. 5 | -5% mana cost on all child Abilities |
| Lv. 10 | +10% damage / healing on all child Abilities |
| Lv. 25 | -0.5s cooldown on all child Abilities |
| Lv. 50 | Unlock a unique visual effect on all child Abilities |
| Lv. 100 | "Master" title + all bonuses doubled |

Innate synergies affect ALL abilities (not just children). Details TBD.

---

## Ability Affinity — TBD

Cosmetic-only milestones from repeated use of a specific ability.

| Uses | Affinity Tier | Visual Effect |
|------|--------------|---------------|
| 100 | Familiar | Slightly brighter particles |
| 500 | Practiced | Unique particle color tint |
| 1,000 | Expert | Trail effect added |
| 5,000 | Mastered | Full visual overhaul (premium particles, sound) |

No stat bonuses. Pure cosmetic reward for dedication.

---

## Point Systems — TBD

### Skill Points (SP)

- Source: Character level-up
- Rate: TBD (was 2/level, 3 at milestones — needs adjustment for separate pools)

### Ability Points (AP)

Three sources:
1. **Leveling** (primary, ~60% of total AP income): AP per level-up
2. **Combat milestones** (bonus, ~25%): AP from boss kills, floor clears
3. **Use-based per-category** (trickle, ~15%): Earn AP by using abilities in combat, tracked per category (Body AP, Survival AP, etc.)

Exact rates TBD.

---

## TODO

- [x] Review remaining Ranger abilities (Bowmanship, Throwing, Firearms, CQC, Trapping, Sapping)
- [x] Full Mage class redesign
- [ ] Define synergy bonus specifics per mastery
- [ ] Define ability affinity cosmetic tiers
- [ ] Define SP/AP exact rates and milestone schedule
- [ ] Refine pass on all three classes ("finer toothcomb")
- [ ] Update locked specs (skills.md, pause-menu-tabs.md, controls.md, magic.md, combat.md)
- [ ] Update icon sprite sheets to match new taxonomy
