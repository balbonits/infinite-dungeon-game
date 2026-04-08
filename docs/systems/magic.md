# Magic & Mana System

## Summary

Magic is a natural phenomenon powered by **magicules** — fundamental particles that permeate the world. Every living being absorbs and processes magicules. Every skill in the game — from a Warrior's sword strike to a Mage's fireball — is a form of magicule manipulation. The skill system and magic system are two sides of the same coin.

## Current State

Design phase. This doc defines the mana resource, magicule processing mechanics, class-specific magic usage, and spell acquisition. Works in conjunction with [skills.md](skills.md), [stats.md](stats.md), and [classes.md](classes.md).

## Design

### The Nature of Magic

Magic extends the laws of physics, biology, and chemistry — pushing them to and sometimes past their breaking point. The results are called **"miracles"**: effects that appear supernatural but follow consistent, learnable rules.

**Magicules** are the medium. They exist everywhere — from the sky to the planet's core — like how air is a mixture of elements. Every cell in every living being absorbs and processes magicules from the moment of creation, inheriting exposure from their parents.

The source of magicules is irrelevant to gameplay. What matters is the rules they follow.

### The Brain as Magicule Processor

The brain is the central organ for all magic. Every magical ability — from a Warrior's reinforced muscles to a Mage's fireball — is routed through the brain. The brain absorbs, processes, and directs magicules.

**INT as a stat represents brain processing capacity.** A higher INT means the brain can handle more magicules at once, form more detailed mental models, and sustain more complex magical effects. Skill improvement is literally the brain getting better at forming and executing mental models.

**Why humans are special:** The human brain is uniquely suited to process magicules safely. It can absorb and direct them without adverse effects, which is why humans can wield magic as a tool rather than being consumed by it. This is a biological trait of the species — not learned, not gifted. Every human brain does this from birth.

**Rare exceptions:** Humans with brain damage, genetic defects, or neurological conditions can be adversely affected by magicules. A compromised brain can't properly process magicules, which can lead to mutations or magical illness. This is rare but exists in the world.

**Monsters exist because their brains can't do this.** Creatures with brains that fail to process magicules properly are warped by exposure instead of empowered. Heavy magicule environments — especially the dungeon — breed increasingly mutated, dangerous beings. The deeper the dungeon, the denser the magicules, the more twisted the creatures. The surface is safe because magicule density is low above ground, so there are few high-level monster threats outside the dungeon.

---

### EXP as Processed Mana

Experience points are not raw mana. You cannot level up by sitting in a mana-dense area and passively absorbing magicules. EXP is mana that has been **processed by the brain through action** — fighting, casting spells, using skills.

**Each action is "chewing."** When the brain uses mana as fuel for a skill or spell, that use leaves an imprint — a memory of the action. Those imprinted memories ARE experience. The brain literally remembers what it did, how it directed the magicules, and what happened as a result. That's what EXP represents.

**Leveling up** means the brain has accumulated enough processed mana memories to fundamentally grow. The body and mind have been reshaped by the sum of everything the character has done. It's not arbitrary — it's earned through action.

**Processed mana is "tastier" than raw mana.** Monsters, dungeons, and other magical entities can survive on raw mana absorption (like eating raw ingredients), but processed mana — the kind that comes from a living being who has fought, learned, and grown — is premium nutrition. It's like the difference between flour and fresh bread. This is why the dungeon wants adventurers to grow strong before they die: stronger adventurers carry richer, more processed mana.

**Death means memory loss.** When a character dies, the dungeon eats a portion of their imprinted action memories (EXP). Lose enough memories and the character loses levels — they literally forget what their body and mind learned. This is the lore behind the XP penalty on death (see [death.md](death.md)).

---

### How Magic Works: Mental Models

All magic — whether a Warrior's enhanced strength or a Mage's fireball — operates through the same mechanism:

1. **The caster forms a mental model** of the desired effect
2. **Magicules respond to the mental model** and manifest the effect in reality
3. **The quality of the mental model determines the quality of the result**

A Warrior thinking "hit harder" unconsciously directs magicules to reinforce their muscles. A Mage thinking "ball of fire — its heat, light, shape, color, behavior" consciously directs magicules to manifest flame.

**Better skill = better mental model = more efficient magicule processing = stronger effect for less mana.**

This is why use-based leveling works: practice literally improves the brain's ability to form and execute mental models.

### Chanting & Incantations

Chanting is a tool, not a requirement. It provides the caster with a **memorized mental framework** — a structured pattern the brain can follow to reliably manifest a specific effect.

- **Novice casters** need full incantations (detailed mental scaffolding)
- **Experienced casters** can abbreviate or skip chanting (the mental model is internalized)
- **Masters** can manifest with a passing thought (the mental model is automatic)

Skill level directly correlates with how much scaffolding the brain still needs.

---

### Mana (Unified Resource)

Mana is the measurable resource representing a being's available magicule processing capacity. **Mana is the only resource bar besides HP.** There is no separate stamina system.

- **Mages** tire out **mentally** when mana is exhausted — the brain can't form mental models.
- **Warriors** tire out **physically** when mana is exhausted — their mana IS their stamina. Muscles can't be magicule-enhanced.
- **Rangers** tire out **physically** when mana is exhausted — same as Warriors. Enhanced reflexes and imbuing shut down.

Same resource, same severity, different flavor text. Every class experiences exhaustion as "tiring out."

#### Mana Pool

Every class has a mana bar. Pool size depends on class and stats.

| Factor | Effect |
|--------|--------|
| Class base | Mage: 200, Ranger: 100, Warrior: 60 |
| INT stat | Increases max mana (see [stats.md](stats.md) for formula) |
| Level | Base pool grows slightly per level |

For Warriors and Rangers, INT still matters: a bigger mana pool means more skills before tiring out. It's less impactful than for Mages, but never useless.

*Exact formulas defined in [stats.md](stats.md).*

#### Mana Regeneration

Mana recovers passively over time. Rate depends on class and INT.

| Factor | Effect |
|--------|--------|
| Base regen | Mage: fastest, Ranger: moderate, Warrior: slowest |
| INT stat | Increases regen rate (magicule processing efficiency) |
| Combat state | Regen may be slower during active combat (optional — balance decision) |
| Amplification skills | Mage's Conduit tree has skills that boost regen (Mana Surge, Focus Channel) |

#### Mana Costs By Class

The same underlying system, but vastly different resource pressure:

| Class | Skill Cost Range | Why |
|-------|-----------------|-----|
| Warrior | 5–20 mana | Biological enhancement is subconscious — the body processes magicules automatically with minimal mental effort |
| Ranger | 10–35 mana | Enhancement + minor matter manipulation — more conscious direction of magicules, especially for imbuing and projectiles |
| Mage | 25–120+ mana | Full thought-based manifestation — creating phenomena from pure mental models requires heavy magicule processing |

Warriors rarely worry about mana. Rangers manage it loosely. Mages must manage it carefully. But all three classes feel it when they hit zero.

#### Mana Exhaustion

When mana reaches 0, the character is **tired out**. The severity is the same across all classes — exhaustion is recoverable, not a death sentence. The difference is flavor, not penalty.

| Class | At 0 Mana | Flavor |
|-------|-----------|--------|
| **Warrior** | All skills disabled. Basic attacks still work (no enhancement). Minor debuff to attack speed. Recovers as mana regenerates. | Physical exhaustion — muscles are spent, breathing hard, can't push any harder. |
| **Ranger** | All skills disabled. Basic attacks work (no imbuing, no magic projectiles). Minor debuff to attack speed. Recovers as mana regenerates. | Physical exhaustion — hands shaking, reflexes dulled, can't keep up the pace. |
| **Mage** | All spells disabled. Basic attacks work (weakened). Minor debuff to cast speed. Recovers as mana regenerates. | Mental exhaustion — brain is foggy, can't focus, thoughts won't form. |

**Same mechanical penalty, different narrative.** No class gets punished harder than any other for running out of mana. The minor debuff encourages avoiding exhaustion without making it a game-over state.

---

### Class-Specific Magic Usage

#### Warriors — Performance Enhancement

Warriors use magicules **subconsciously to enhance their body**:

- **Strengthen muscles** — hit harder, lift heavier, resist knockback
- **Reinforce bones and joints** — take hits that would break a normal person
- **Enhance reflexes** — react faster, parry instinctively
- **Project presence** — the "Mind: Outer" skills (War Cry, Intimidate, Menacing Presence) are magicule projections that affect enemies' nervous systems

A Warrior doesn't think "I'm casting a spell." They think "I'm pushing harder" and magicules respond. Their Body category skills are enhanced physical techniques. Their Mind category skills are the closest Warriors get to conscious magic use — projecting magicules outward to affect others.

**Connection to skill tree:**
- Body skills = magicule-enhanced physical combat (subconscious processing)
- Mind: Inner = self-directed magicule enhancement (damage resistance, regen, willpower)
- Mind: Outer = projected magicule effects (debuffs, fear, aura)

#### Rangers — Enhancement + Matter Manipulation

Rangers have the same biological enhancement as Warriors PLUS conscious **matter manipulation**:

- **Enhanced nerves and brain signals** — faster reaction time, better coordination, sharper senses
- **Imbue projectiles** — channel magicules into arrows, bolts, thrown weapons for elemental effects
- **Enhance equipment** — temporarily boost weapon properties through magicule infusion
- **Create magic projectiles** — minor manifestation (not as complex as Mage spells, but more than pure physical)

The Ranger sits between Warrior and Mage on the magic spectrum. Their Arms skills use magicules for weapon enhancement. Their Instinct skills use magicules for cognitive enhancement (Precision, Awareness, Trapping).

**Imbuing and equipment materials:** Ranger imbuing is **equipment-enhanced, not equipment-required**. A Ranger can use elemental skills (fire arrows, frost bolts, etc.) with any weapon — even basic iron. However, materials have natural magical affinities: a bow strung with fire-aligned sinew, or arrows tipped with flame-conductive metal, will produce stronger fire imbuing than basic materials. Equipment enhances elemental output but is never a gate. This gives Rangers a reason to seek specific materials without locking elemental gameplay behind gear checks.

**Connection to skill tree:**
- Arms skills = magicule-enhanced ranged combat + matter manipulation (imbuing, magic projectiles)
- Instinct: Precision = magicule-enhanced targeting (cognitive processing, trajectory calculation)
- Instinct: Awareness = magicule-enhanced perception (threat detection, evasion reflexes)
- Instinct: Trapping = magicule-infused objects (traps are items charged with magicule effects)

#### Mages — Full Thought-Based Manifestation

Mages create phenomena **purely from mental models**. This is the most demanding use of magicules:

- **Visualize the effect** — its properties, behavior, shape, intensity
- **Direct magicules to manifest** the mental model into physical reality
- **Sustain concentration** to maintain ongoing effects
- **Process feedback** — the manifestation pushes back on the brain, which is why overcasting causes cognitive impairment

The Mage's Arcane category contains their elemental manifestation skills (Fire, Water, Air, Earth, Light, Dark). Their Conduit category is about training the body to handle more magical throughput.

**Connection to skill tree:**
- Arcane: [Element] base skill = magicule processing efficiency for that element (cast speed, range, mana efficiency)
- Arcane: [Element] specific skills = refined mental models for specific manifestations (Fireball, Lightning, etc.)
- Conduit: Restoration = using magicules to repair the body (self-healing is magicule-directed cellular repair)
- Conduit: Amplification = expanding the brain's magicule processing capacity (more mana, faster regen)
- Conduit: Overcharge = pushing the nervous system past safe limits (power at bodily cost)

---

### Spell Acquisition (Mage-Specific)

Mages learn spells through mental model acquisition. Two methods:

#### Spell Books (Direct Learning)

- Buy or find a spell book
- Reading it **permanently teaches** the spell (the book provides a complete mental model)
- The book is consumed on use
- Expensive / rare — this is the "premium" learning path

#### Scroll Osmosis (Learning By Application)

- Find or buy a spell scroll (e.g., "Fireball Scroll")
- **Use it:** the scroll temporarily injects enough knowledge into the brain to manifest the spell once. The scroll is consumed.
- **The knowledge fades** almost immediately after casting — but the brain retains a fragment
- **Each use = progress toward permanent learning.** After enough uses, the mental model is fully internalized.
- **Once learned:** the spell is permanently available without scrolls

**Osmosis progress:**

| Scroll Tier | Uses To Learn | Why |
|-------------|--------------|-----|
| Basic spells | 3–5 scrolls | Simple mental models, easy to internalize |
| Intermediate | 8–12 scrolls | More complex phenomena, harder to retain |
| Advanced | 15–25 scrolls | Extremely complex mental models, requires many repetitions |
| Master | 30–50 scrolls | Near-impossible phenomena, only the most dedicated Mages master these |

*Exact numbers are balance targets — adjust during testing.*

**INT accelerates learning:** Higher INT = better magicule processing = the brain retains more from each scroll use. An INT-focused Mage might need fewer scrolls to learn the same spell.

**Why this mechanic works lorewise:** The scroll doesn't teach you. It gives your brain one brief experience of manifesting the effect. Like learning to ride a bike — each attempt builds muscle memory (or in this case, "mental model memory"). Eventually, the brain can do it on its own.

---

### Unified Framework: Skills = Magic

Every skill in the game is magicule manipulation. The categories describe different applications:

| Application | Example Skills | Mental Model Type | Mana Cost | Consciousness Level |
|-------------|---------------|-------------------|-----------|-------------------|
| Biological enhancement | Warrior Body skills, Ranger reflexes | Subconscious body direction | Low (5–20) | Automatic |
| Cognitive enhancement | Warrior Mind: Inner, Ranger Instinct | Semi-conscious focus | Low–Medium (10–30) | Background |
| Matter manipulation | Ranger imbuing, trapping | Conscious object direction | Medium (15–35) | Active |
| Projected presence | Warrior Mind: Outer (War Cry, Intimidate) | Conscious outward projection | Medium (15–30) | Active |
| Elemental manifestation | Mage Arcane spells | Fully conscious visualization | High (25–120+) | Full concentration |
| Body channeling | Mage Conduit skills | Conscious internal direction | Medium–High (20–60) | Active |
| Overcharge | Mage Overcharge skills | Dangerous conscious override | HP cost, not mana | Desperate |

**Use-based leveling** = the brain gets better at forming the mental model through repetition.
**Skill points** = deliberate study and understanding that improves the mental model's efficiency.
**Infinite scaling** = there is always a more refined mental model to achieve. Diminishing returns reflect that early improvements are easy (rough → decent model) while later improvements are subtle (great → slightly better model).

---

### Magicule Density and Dungeon Depth

Magicule concentration increases with depth. The deeper you go, the denser the ambient magicules become.

**Effects of increasing density:**
- **Shallow floors:** Low magicule density. Skills work normally. No environmental pressure.
- **Mid floors:** Moderate density. Stronger monsters (they absorb more magicules from the environment). Players may notice ambient effects — faint shimmer in the air, unusual warmth.
- **Deep floors:** High density. The environment itself starts to affect the player. Monsters are significantly stronger. Visual and audio cues communicate the oppressive density.
- **Extreme depth (past crust/mantle equivalent):** **Lethal magicule density.** The concentration is so overwhelming that living beings cannot process the flood of magicules. It's not just heat and pressure — the magicules themselves tear through cells and overload the nervous system. This creates a **natural hard ceiling** for the infinite dungeon.

The hard ceiling is not a wall you hit — it's a gradient. Each floor past the danger threshold is exponentially harder to survive. The most powerful characters in the game can push a few floors deeper than average, but nobody goes infinitely deep. This gives the infinite dungeon a meaningful endpoint that emerges from the world's rules rather than an arbitrary level cap.

**Why this matters for gameplay:** The magicule density curve is the answer to "what stops you?" It's not a door that says STOP. It's the dungeon itself becoming hostile. Players who push deeper earn bragging rights and better loot, but they're fighting the environment as much as the monsters.

---

### Movement

**Base movement is always a brisk jog.** There is no walk/run speed toggle. No slow walking mode, no Diablo 1-style movement speed tiers. The character always moves at a comfortable, responsive jog pace.

This keeps the game feeling snappy and avoids the "why am I walking so slowly" frustration. Movement speed is a fixed constant (outside of Haste and debuffs).

---

### Innate Skills (Universal)

Innate skills are a **species-level category** — magicule abilities that every living being possesses regardless of class. These are not class skills. They sit outside the Warrior/Ranger/Mage skill trees.

Every character starts with all Innate skills at level 0 and can level them infinitely through use and skill point investment, just like class skills. Innate skills follow the same hybrid leveling model (use-based XP + skill point allocation).

**Why Innate skills exist:** In a world where every cell absorbs magicules from birth, certain basic applications of magicule processing are universal. These are abilities that any living creature develops naturally — the magical equivalent of running, seeing, and flinching.

#### Innate Skill List

**Structure:** Innate skills are standalone — no base/specific hierarchy. Each is a single skill leveled infinitely.

---

**Haste** — Magicule-enhanced burst of speed.

- **Activation:** Hold to sprint. Drains mana per second while active.
- **Effect:** Increased movement speed AND enhanced dodge chance (magicule-enhanced legs react faster to threats).
- **Leveling benefits:** Each level reduces mana drain rate and increases both the speed bonus and dodge chance bonus.
- **Why it's Innate:** Every creature with legs can push magicules into them to move faster. It's the most basic physical magicule application — even animals do it instinctively (a fleeing deer, a pouncing predator).

---

**Sense** — Magicule-enhanced perception.

- **Activation:** Toggle on/off. Drains mana per second while active.
- **Effect:** Detect nearby enemies and items through walls and obstacles. Displays as a subtle pulse or highlight effect on the minimap and in the game world. Range increases with level.
- **Leveling benefits:** Each level reduces mana drain rate, increases detection range, and reveals more detail (higher levels show enemy type, threat level, and item rarity).
- **Why it's Innate:** Every living being has an instinctive awareness of nearby magicule signatures. Prey animals sense predators. Predators sense prey. Humans refine this into conscious perception — "I feel something watching me" is a low-level Sense activation.

---

**Fortify** — Magicule-reinforced body.

- **Activation:** Toggle on/off. Drains mana per second while active.
- **Effect:** Temporary damage resistance. The character's body becomes tougher — magicules reinforce skin, bone, and muscle against incoming damage. A faint visual shimmer indicates the effect is active.
- **Leveling benefits:** Each level reduces mana drain rate and increases the damage resistance percentage.
- **Why it's Innate:** The flinch response. When a creature braces for impact, magicules flood to the point of contact. Every living thing does this — it's why a startled person can take a hit that would otherwise break bone. Leveling Fortify turns an unconscious reflex into a deliberate defensive tool.

---

**Innate skill design principles:**
- All three drain mana per second (consistent cost model — encourages moment-to-moment resource decisions)
- All three are useful for every class (Warriors benefit from Sense just as much as Mages benefit from Fortify)
- None overlap with class skills (Haste is movement, not combat; Sense is detection, not combat awareness like the Ranger's Threat Sense; Fortify is flat resistance, not the Warrior's specialized Pain Tolerance)
- Leveling always reduces mana cost AND increases effect (double incentive to invest)

## Acceptance Criteria

- [ ] All classes have a visible mana bar (the only resource bar besides HP — no separate stamina)
- [ ] Warrior/Ranger skills cost mana (cheap for Warrior, moderate for Ranger)
- [ ] Mage spells cost significantly more mana than physical skills
- [ ] Mana regenerates passively, faster for Mages
- [ ] Mana exhaustion (0 mana) applies the same severity across all classes (skills disabled, minor debuff, recoverable)
- [ ] Exhaustion flavor text differs per class (physical tiredness for Warrior/Ranger, mental tiredness for Mage)
- [ ] Ranger elemental imbuing works with any equipment but is enhanced by material affinities
- [ ] Spell scrolls are consumable — one cast per scroll
- [ ] Repeated scroll use progresses toward permanent spell learning
- [ ] INT stat affects mana pool, regen, processing efficiency, AND scroll learning speed
- [ ] Skill leveling (use-based) is explained by magicule processing improvement
- [ ] Magicule density increases with dungeon depth, creating a natural hard ceiling at extreme depth
- [ ] Movement is always a brisk jog — no walk/run toggle
- [ ] Haste skill: hold to sprint, drains mana/sec, boosts movement speed and dodge chance
- [ ] Sense skill: toggle, drains mana/sec, detects nearby enemies and items through obstacles
- [ ] Fortify skill: toggle, drains mana/sec, provides damage resistance
- [ ] All three Innate skills are available to every class and level infinitely

## Implementation Notes

- Mana bar is the only resource bar besides HP on the HUD — no separate stamina bar
- Mana exhaustion effects should be visually distinct per class (Warrior: red sweat/panting, Ranger: shaking hands, Mage: head-holding/daze) but mechanically identical
- Scroll osmosis progress should be visible in a spell learning UI (progress bar per spell)
- Innate skills need their own UI section, separate from class skill trees (universal category)
- Haste should have a clear "sprinting" animation with a visual trail or blur effect
- Sense detection overlay should be subtle (pulse rings, highlighted silhouettes) — not cluttering the screen
- Fortify should have a faint body shimmer, not a full barrier bubble (it's internal reinforcement)
- Magicule density should be communicated through environmental art: faint particle effects that get denser with depth, color shifts, ambient sound changes
- The lore explanation (magicules, mental models) should be conveyed through in-game flavor text, NPC dialogue, and item descriptions — not a tutorial dump

## Open Questions

- What is the exact mana drain rate per second for each Innate skill at level 1? (Needs balance testing — should feel costly early, manageable late)
- How does magicule density scale with floor number? (Linear? Exponential? Need a formula for the hard ceiling gradient)
- At what floor depth does magicule density start becoming dangerous? (Affects dungeon length and endgame pacing)
- Should Sense reveal trap locations in addition to enemies and items?
- Can Fortify and Haste be active simultaneously, or is there a one-active-Innate-at-a-time limit? (Stacking all three would drain mana very fast, which might be self-balancing)
- How do material affinities for Ranger imbuing interact with the blacksmith/crafting system? (Needs crafting doc)
- Should Innate skills have visual upgrades at milestone levels (e.g., Fortify shimmer changes color at level 25)?
