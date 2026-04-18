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

**P2 implementation order:** Mana is implemented for the Mage class first. Warrior and Ranger mana pools are added in a subsequent phase once the base mana system is stable and tested.

*Exact formulas defined in [stats.md](stats.md).*

#### Mana Regeneration

Mana recovers passively over time. Rate depends on class and INT.

| Factor | Effect |
|--------|--------|
| Base regen | Mage: fastest, Ranger: moderate, Warrior: slowest |
| INT stat | Increases regen rate (magicule processing efficiency) |
| Combat state | Regen may be slower during active combat (optional — balance decision) |
| Amplification skills | Mage's Attunement tree has skills that boost regen (Mana Surge, Focus Channel) |

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
- **Project presence** — the "Mind: Intimidation" skills (Shout, Intimidate, Ugly Mug, Battle Roar) are magicule projections that affect enemies' nervous systems

A Warrior doesn't think "I'm casting a spell." They think "I'm pushing harder" and magicules respond. Their Body category skills are enhanced physical techniques. Their Mind category skills are the closest Warriors get to conscious magic use — projecting magicules outward to affect others.

**Connection to skill tree:**
- Body skills = magicule-enhanced physical combat (subconscious processing)
- Mind: Discipline = self-directed magicule enhancement (damage resistance, regen, willpower)
- Mind: Intimidation = projected magicule effects (debuffs, fear, aura)

#### Rangers — Enhancement + Matter Manipulation

Rangers have the same biological enhancement as Warriors PLUS conscious **matter manipulation**:

- **Enhanced nerves and brain signals** — faster reaction time, better coordination, sharper senses
- **Imbue projectiles** — channel magicules into arrows, bolts, thrown weapons for elemental effects
- **Enhance equipment** — temporarily boost weapon properties through magicule infusion
- **Create magic projectiles** — minor manifestation (not as complex as Mage spells, but more than pure physical)

The Ranger sits between Warrior and Mage on the magic spectrum. Their Weaponry skills use magicules for weapon enhancement. Their Survival skills use magicules for cognitive enhancement and environmental manipulation (Awareness, Trapping, Sapping).

**Imbuing and equipment materials:** Ranger imbuing is **equipment-enhanced, not equipment-required**. A Ranger can use elemental skills (fire arrows, frost bolts, etc.) with any weapon — even basic iron. However, materials have natural magical affinities: a bow strung with fire-aligned sinew, or arrows tipped with flame-conductive metal, will produce stronger fire imbuing than basic materials. Equipment enhances elemental output but is never a gate. This gives Rangers a reason to seek specific materials without locking elemental gameplay behind gear checks.

**Connection to skill tree:**
- Weaponry skills = magicule-enhanced ranged combat + matter manipulation (imbuing, magic projectiles)
- Survival: Awareness = magicule-enhanced perception (threat detection, evasion reflexes — also absorbs former Precision targeting abilities)
- Survival: Trapping = magicule-infused objects (traps are items charged with magicule effects)
- Survival: Sapping = magicule-charged explosives and area denial (demolitions, resource disruption)

#### Mages — Full Thought-Based Manifestation

Mages create phenomena **purely from mental models**. This is the most demanding use of magicules:

- **Visualize the effect** — its properties, behavior, shape, intensity
- **Direct magicules to manifest** the mental model into physical reality
- **Sustain concentration** to maintain ongoing effects
- **Process feedback** — the manifestation pushes back on the brain, which is why overcasting causes cognitive impairment

The Mage's Elemental category contains their elemental manifestation skills (Fire, Water, Air, Earth, Light, Dark). Their Attunement category is about training the body to handle more magical throughput.

**Connection to skill tree:**
- Elemental: [Element] mastery = magicule processing efficiency for that element (cast speed, range, mana efficiency)
- Elemental: [Element] abilities = refined mental models for specific manifestations (Fireball, Lightning, etc.)
- Aether: Light+Dark as one mastery — cosmic force manipulation (raw energy outward, gravity inward)
- Attunement: Restoration = using magicules to repair the body (self-healing is magicule-directed cellular repair)
- Attunement: Amplification = expanding the brain's magicule processing capacity (more mana, faster regen)
- Attunement: Overcharge = pushing the nervous system past safe limits (power at bodily cost)

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
| Cognitive enhancement | Warrior Mind: Discipline, Ranger Survival | Semi-conscious focus | Low–Medium (10–30) | Background |
| Matter manipulation | Ranger imbuing, trapping | Conscious object direction | Medium (15–35) | Active |
| Projected presence | Warrior Mind: Intimidation (Shout, Intimidate) | Conscious outward projection | Medium (15–30) | Active |
| Elemental manifestation | Mage Elemental spells | Fully conscious visualization | High (25–120+) | Full concentration |
| Body channeling | Mage Attunement skills | Conscious internal direction | Medium–High (20–60) | Active |
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

#### Density Formula (SPEC-MAGICULE-DENSITY-01)

Magicule density at floor `F` is defined piecewise:

```
if F <= 100:
    density(F) = F / 100                    # linear ramp, danger onset at F = 100
else:
    density(F) = 1.0 * k ^ (F - 100)        # exponential above threshold
    where k = 1.032
```

- **Linear branch (floors 1–100):** density scales gently from 0.01 at floor 1 to 1.00 at floor 100. Players feel an ambient increase but skills behave normally and the environment does not actively harm them.
- **Exponential branch (floors 101+):** each extra floor multiplies density by `k = 1.032` (compounded). That's roughly a **doubling every ~22 floors** past the threshold — a gentle slope at first, then steepening fast once the floors pile up.

**Anchor table (k = 1.032):**

| Floor | Density | Interpretation |
|-------|---------|----------------|
| 1 | 0.01 | Baseline surface ambient — near zero pressure |
| 25 | 0.25 | Shallow — no ambient effects |
| 50 | 0.50 | Mid-dungeon — faint shimmer visible |
| 75 | 0.75 | Approaching threshold — noticeable warmth |
| 100 | 1.00 | **Danger onset** — player starts to feel it |
| 125 | 2.19 | Dangerous — environment begins to bite |
| 150 | 4.77 | Highly dangerous — needs skill and gear |
| 180 | 12.40 | Elite territory — only optimized builds survive here long |
| 200 | 23.30 | **Effective ceiling for elite builds** — each push is expensive |
| 250 | 109.63 | Beyond stable reach — nobody farms here |

**Danger threshold (floor 100):** density = 1.00. Below this, the environment is mechanically quiet. At and above this, every additional floor compounds — the air itself is hostile.

**Effective ceiling (floors 180–200):** density ≈ 12–23. Only elite, heavily-optimized characters can stably operate in this band. This is the "endgame push" zone where bragging rights live.

**Why this curve:** density grows slowly while floor ≤ 100, so the first hundred floors are a welcoming slope for most players. Past 100, each extra floor multiplies density by `k` — difficulty ramps exponentially as players push past the threshold. The curve has no hard wall; it becomes a gradient of futility that asymptotically prices out every build, no matter how optimized. This is the load-bearing magic number every Phase B/C/D magic spec inherits.

**What density does and does not modify:**

| Affected by density | Not affected by density |
|---------------------|-------------------------|
| Enemy stat scaling (HP, damage — per `monster-drops.md` + enemy baselines) | **Innate toggle drain** (SPEC-INNATE-MANA-COST-01 — fixed per-level) |
| Lethal-gradient environmental pressure at deep floors (narrative) | **Mage spell damage** (SPEC-MAGIC-COMBAT-FORMULA-01 — INT-only per [stats.md §INT](stats.md#int--intelligence-mage-primary)) |
| Regurgitation / mutation rates (per `dungeon-intelligence.md`) | Spell mana cost (INT-only per stats.md) |
| Quality shift ceiling on rolls at extreme saturation (per `zone-saturation.md`) | Ability cooldowns (fixed per ability) |

The guiding principle: **density manifests through the world (enemies, environment, dungeon AI), not through the player's inherent combat math.** The player's damage and costs stay legible at any depth; the world becomes hostile instead. This keeps build identity stable and prevents runaway class imbalance at depth (a density-multiplier on Mage damage would scale mages past Warriors/Rangers since only Mages consciously channel environmental magicules).

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
- **Mana drain (level 1):** 13.33 mana/sec — a brand-new Mage with a 200-mana pool and no regen can sprint for about 15 seconds before running out. See §Drain Scaling Per Level (SPEC-INNATE-MANA-COST-01) for the per-level curve.
- **Why short uptime:** Haste is the burst tool of the Innate kit. It's meant to be flicked on for a chase, a reposition, or a dodge — not held as a default state. The 15-second level-1 budget trains the player to spend it decisively and turn it off.
- **Leveling benefits:** Each level reduces mana drain rate and increases both the speed bonus and dodge chance bonus.
- **Why it's Innate:** Every creature with legs can push magicules into them to move faster. It's the most basic physical magicule application — even animals do it instinctively (a fleeing deer, a pouncing predator).

---

**Sense** — Magicule-enhanced perception.

- **Activation:** Toggle on/off. Drains mana per second while active.
- **Effect:** Detect nearby enemies and items through walls and obstacles. Displays as a subtle pulse or highlight effect on the minimap and in the game world. Range increases with level.
- **Mana drain (level 1):** 6.67 mana/sec — a brand-new Mage with a 200-mana pool and no regen can keep Sense on for about 30 seconds before running out. See §Drain Scaling Per Level (SPEC-INNATE-MANA-COST-01) for the per-level curve.
- **Why medium uptime:** Sense is the exploration tool. The player flicks it on when entering a new room or approaching a corner, not while standing in a safe hallway. 30 seconds is long enough to sweep a room or two, short enough that wasteful always-on use will drain the pool.
- **Leveling benefits:** Each level reduces mana drain rate, increases detection range, and reveals more detail (higher levels show enemy type, threat level, and item rarity).
- **Why it's Innate:** Every living being has an instinctive awareness of nearby magicule signatures. Prey animals sense predators. Predators sense prey. Humans refine this into conscious perception — "I feel something watching me" is a low-level Sense activation.

---

**Fortify** — Magicule-reinforced body.

- **Activation:** Toggle on/off. Drains mana per second while active.
- **Effect:** Temporary damage resistance. The character's body becomes tougher — magicules reinforce skin, bone, and muscle against incoming damage. A faint visual shimmer indicates the effect is active.
- **Mana drain (level 1):** 3.33 mana/sec — a brand-new Mage with a 200-mana pool and no regen can hold Fortify for about 60 seconds before running out. See §Drain Scaling Per Level (SPEC-INNATE-MANA-COST-01) for the per-level curve.
- **Why long uptime:** Fortify is the held stance. A player might click it on before a room fight and keep it up through the whole engagement — it's the Innate that most resembles a defensive "mode." A full minute at level 1 is long enough to cover a typical room-clear without feeling like a cooldown.
- **Leveling benefits:** Each level reduces mana drain rate and increases the damage resistance percentage.
- **Why it's Innate:** The flinch response. When a creature braces for impact, magicules flood to the point of contact. Every living thing does this — it's why a startled person can take a hit that would otherwise break bone. Leveling Fortify turns an unconscious reflex into a deliberate defensive tool.

---

**Armor** — Always-on passive armor proficiency.

- **Class-specific names:** Ironhide (Warrior), Nimbleguard (Ranger), Spellweave (Mage)
- **Activation:** Always active. Unlike the other Innate skills, Armor does NOT toggle and does NOT drain mana — it is permanently on.
- **Effect:** Class-specific equipment mastery. Provides passive bonuses to armor effectiveness, equipment synergy, and class-appropriate defensive scaling.
- **Leveling benefits:** Each level increases armor effectiveness and deepens class-specific equipment mastery.
- **Why it's Innate:** Every living being develops a natural affinity for protecting itself. Humans extend this instinct through equipment — Warriors through heavy plate, Rangers through agile leathers, Mages through enchanted cloth. The body learns to work with its armor, not just wear it.

---

#### Drain Scaling Per Level (SPEC-INNATE-MANA-COST-01)

Haste, Sense, and Fortify all share the same per-level drain-reduction curve. Armor is excluded — it has no drain to reduce.

**Formula:**

```
drain(L) = max(base_drain * 0.96^L, base_drain * 0.25)
```

- `L` is the Innate's skill level (starts at 1; leveling is infinite per §Innate Skills).
- `base_drain` is the level-1 drain rate for that Innate: Haste 13.33, Sense 6.67, Fortify 3.33 mana/sec.
- Each level multiplies drain by 0.96 (a 4% reduction per level), compounded.
- Drain floors at 25% of `base_drain` — it gets very cheap at high levels but never zero. The floor is reached at level 35 (0.96^35 ≈ 0.243, so the `max` clamp kicks in).
- **Drain is NOT modified by floor density.** Innate cost is the brain's processing cost, not an environmental cost. Deep floors make monsters stronger and the air hostile; they do not make your Innates more expensive.

**Plain-language:** Every level you invest makes your Innate a little cheaper. A level-10 Mage's Haste lasts about 50% longer than a level-1 Mage's Haste on the same mana pool. A level-20 Mage's Haste lasts about 2.3× as long. By level 35, drain has bottomed out at 25% of the starting cost — Innates are still metered resources at that point, but they feel almost free. This gives the player a long, legible improvement curve: the Innate you used for 15 seconds on day one eventually gives you nearly a full minute, without ever becoming a spammable toggle that trivializes resource management.

**Drain table (mana/sec at select levels, floored at 25% of base):**

| Level | Multiplier | Haste (base 13.33) | Sense (base 6.67) | Fortify (base 3.33) |
|-------|------------|--------------------|-------------------|---------------------|
| 1 | 0.960 | 12.80 | 6.40 | 3.20 |
| 5 | 0.815 | 10.87 | 5.44 | 2.71 |
| 10 | 0.664 | 8.85 | 4.43 | 2.21 |
| 20 | 0.442 | 5.89 | 2.95 | 1.47 |
| 30 | 0.294 | 3.92 | 1.96 | 0.98 |
| 35+ | 0.250 (floor) | 3.33 | 1.67 | 0.83 |
| 50 | 0.250 (floor) | 3.33 | 1.67 | 0.83 |

*Note on level 1: the formula applies `0.96^1` at level 1, so even the "base rates" quoted earlier (13.33, 6.67, 3.33) are the pre-level-1 anchor — a freshly-unlocked Innate at level 1 drains slightly less than that anchor (12.80 / 6.40 / 3.20). The round numbers 15s / 30s / 60s uptime at a 200-mana pool are the **design targets** at unlock; actual in-play uptime at level 1 will be ~4% longer than the round number. This asymmetry is intentional — the player's first second with any Innate feels slightly better than the spec promised.*

**Uptime at key levels (Mage, 200 mana pool, no regen, no other active Innates):**

| Innate | Level 1 uptime | Level 10 uptime | Level 20 uptime | Level 35+ uptime |
|--------|----------------|------------------|-----------------|-------------------|
| Haste | ~15.6 s | ~22.6 s | ~34.0 s | ~60.0 s |
| Sense | ~31.3 s | ~45.2 s | ~67.8 s | ~120.0 s |
| Fortify | ~62.5 s | ~90.5 s | ~135.9 s | ~240.0 s |

**Innate skill design principles:**
- There are 4 Innate skills total: Haste, Sense, Fortify, and Armor
- Haste, Sense, and Fortify drain mana per second (consistent cost model — encourages moment-to-moment resource decisions)
- Armor is the exception: it is always active with no toggle and no mana drain
- All four are useful for every class (Warriors benefit from Sense just as much as Mages benefit from Fortify)
- None overlap with class skills (Haste is movement, not combat; Sense is detection, not combat awareness like the Ranger's Threat Sense; Fortify is flat resistance, not the Warrior's specialized Endure)
- Leveling always reduces mana cost AND increases effect for toggle skills (double incentive to invest); for Armor, leveling purely increases effectiveness
- The three toggle Innates share a single drain-scaling curve (see §Drain Scaling Per Level) with per-Innate `base_drain` values that set asymmetric level-1 uptime: Haste 15s (burst), Sense 30s (exploration), Fortify 60s (held stance)

#### Stacking Rule (SPEC-INNATE-STACKING-01)

**The rule.** Haste, Sense, and Fortify may be activated together in any combination. There is no hard limit on how many toggle-Innates can run simultaneously. Armor is always-on and does not participate in stacking (no toggle, no drain).

**In play terms:** a player can sprint while Sense-scanning for threats while Fortify-braced for damage — all three at once. Nothing stops them. The game doesn't say "pick one." It says "pay for everything you keep running."

**The governor — mana drain sums.** When multiple toggle-Innates are active, their per-second drains add together. At level 1, the combined drain is:

| Combination | Drain (mana/sec) | Pool burn time (200-mana Mage, no regen) |
|-------------|------------------|------------------------------------------|
| Haste only | 13.33 | ~15.0 sec |
| Sense only | 6.67 | ~30.0 sec |
| Fortify only | 3.33 | ~60.0 sec |
| Haste + Sense | 20.00 | ~10.0 sec |
| Haste + Fortify | 16.67 | ~12.0 sec |
| Sense + Fortify | 10.00 | ~20.0 sec |
| **All three** | **23.33** | **~8.6 sec** |

**In play terms:** running all three at once burns a fresh Mage's mana pool in under nine seconds. A Warrior (60 pool) or Ranger (100 pool) would drain even faster. That uptime is the entire balance lever — the game lets you stack freely because the mana economy makes blanket-stacking self-punishing. You *can* run everything; you won't get to keep running it for long.

**Why no hard cap.** A one-active-at-a-time limit would be a UI constraint, not a design one. It would tell the player "the game has decided what you can do." Free stacking instead says "the game has decided what you can *afford*." That's the more interesting question, and it keeps combo plays alive:

- **Fortify + Haste** = the durable sprinter. Charge into a pack, soak a hit, sprint out.
- **Sense + Haste** = the scout. See the threats, outrun them.
- **Sense + Fortify** = the cautious explorer. Spot trouble early, brace for it if it lands.

None of these combos would exist with a hard cap. All of them are self-limiting because they cost real mana.

**Worked example — mid-game Mage (level-15 Innate investment, ~400-mana pool, ~12 mana/sec regen from modest Attunement investment):**

Assume each Innate's drain at level 15 has dropped to roughly 60% of its level-1 value (per the SPEC-INNATE-MANA-COST-01 per-level curve — exact numbers owned by that spec). Approximate drains at level 15: Haste ~8.0, Sense ~4.0, Fortify ~2.0 = **~14.0 mana/sec combined**.

With ~12 mana/sec regen, running all three has a **net drain of only ~2.0 mana/sec**. A 400-mana pool lasts ~200 seconds of all-three uptime before exhaustion — effectively sustainable for most encounters. Dropping Haste (the biggest drain) puts net regen in the black, meaning Sense + Fortify can be held indefinitely while Haste is flicked on for bursts.

**In play terms:** this is the payoff for Mages who invest in mana regen. At level 1 they can barely keep all three up for nine seconds. At level 15 with Attunement investment, they can hold the "scout-and-brace" combo forever and only spend mana when they actually sprint. That feels like mastery — not "the game lifted the cap," but "I earned the ability to pay the cost."

**Build identity:**

- **Mage** who wants to stack all three permanently: invest in Attunement (regen) and INT (pool size). This is the "Innate specialist" Mage build — trades some elemental damage throughput for permanent utility uptime.
- **Warrior** (60-mana pool): will typically pick one Innate per situation — Fortify in melee, Haste for repositioning. Stacking two is a short-term burst, not a sustained state.
- **Ranger** (100-mana pool): lives in the middle — can comfortably run one Innate passively, stack two briefly, and stack three only in emergencies.

**In play terms:** every class can use every Innate, but the *way* they use them differs. Mages sustain. Warriors burst. Rangers manage. The stacking rule is the same for everyone; the class resources make the feel different.

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
- [ ] Magicule density follows the piecewise formula in §Density Formula (SPEC-MAGICULE-DENSITY-01); danger onset at floor 100, effective ceiling ~floors 180–200
- [ ] Movement is always a brisk jog — no walk/run toggle
- [ ] Haste skill: hold to sprint, drains 13.33 mana/sec at level 1 (~15s uptime on a 200-mana Mage pool), boosts movement speed and dodge chance; drain scales per §Drain Scaling Per Level (SPEC-INNATE-MANA-COST-01)
- [ ] Sense skill: toggle, drains 6.67 mana/sec at level 1 (~30s uptime), detects nearby enemies and items through obstacles; drain scales per §Drain Scaling Per Level
- [ ] Fortify skill: toggle, drains 3.33 mana/sec at level 1 (~60s uptime), provides damage resistance; drain scales per §Drain Scaling Per Level
- [ ] Innate drain is not modified by floor density (magicule density affects enemy strength and lethal-floor gradient, not Innate mana cost)
- [ ] Haste/Sense/Fortify can be active simultaneously with no hard concurrency cap; combined mana drain is the only limit (per §Stacking Rule, SPEC-INNATE-STACKING-01)
- [ ] Armor skill: always active (no toggle, no mana drain), class-specific passive armor proficiency (Ironhide/Nimbleguard/Spellweave)
- [ ] All four Innate skills are available to every class and level infinitely

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

- Should Sense reveal trap locations in addition to enemies and items?
- How do material affinities for Ranger imbuing interact with the blacksmith/crafting system? (Needs crafting doc)
- Should Innate skills have visual upgrades at milestone levels (e.g., Fortify shimmer changes color at level 25)?
