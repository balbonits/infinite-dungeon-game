# Synergy Bonuses

## Summary

When a Skill (passive mastery) reaches milestone levels, ALL its child Abilities receive a synergy bonus. These are automatic rewards for deep investment in a mastery -- no player choice required.

Innate Skill synergies are special: they affect ALL Abilities, not just children.

See [skills.md](skills.md) for the full Skills & Abilities system design.

## Current State

**Spec status: LOCKED.** Threshold levels, universal bonuses, and per-mastery bonuses are defined. Exact values are balance targets -- subject to tuning during playtesting.

## Design Research

System informed by threshold/mastery bonus designs in:
- [Path of Exile Masteries](https://www.poewiki.net/wiki/Mastery) -- thematic, identity-reinforcing bonuses per cluster
- [Last Epoch Skill Specialization](https://maxroll.gg/last-epoch/resources/passives-and-skills) -- per-skill trees that can fundamentally change abilities
- [Grim Dawn Devotion Constellations](https://grimdawn.fandom.com/wiki/Constellation) -- completion rewards that unlock further progression + proc abilities
- [WoW Talent Trees](https://www.wowhead.com/guide/classes/dragonflight-talent-trees) -- milestone Apex Talents at high levels

Key takeaways applied:
- Every bonus should feel **thematic** to its mastery (PoE approach)
- Major milestones should include **mechanical modifiers**, not just stat bumps (Grim Dawn procs)
- The highest milestone should feel **unique and powerful**, not just "more of the same" (WoW Apex)
- Bonuses should be noticeable, not invisible math (Last Epoch's skill-changing nodes)

---

## Threshold Structure

| Skill Level | Type | Bonus |
|-------------|------|-------|
| Lv. 5 | Universal | -15% mana cost on all child Abilities |
| Lv. 10 | Per-mastery | Unique stat bonus (identity-reinforcing) |
| Lv. 25 | Per-mastery | Unique stat bonus (stronger, more specialized) |
| Lv. 50 | Per-mastery | Visual overhaul + unique proc/modifier (Ability interaction) |
| Lv. 100 | Per-mastery | "Master" title + unique powerful bonus |

**Lv. 5 is universal** -- every mastery gives the same bonus. This is the "welcome to investing deeply" reward.

**Lv. 10 and 25 are per-mastery** -- each mastery gets bonuses that reinforce its combat identity. No two masteries share the same bonus.

**Lv. 50 is a game-changer** -- visual overhaul on all child Abilities PLUS a proc or modifier that creates Ability interactions (e.g., "Slash has a chance to trigger a free Thrust"). Inspired by Grim Dawn's celestial procs.

**Lv. 100 is the pinnacle** -- "Master of [Mastery]" title + one powerful, unique bonus that no other source provides.

---

## Warrior

### Body

#### Unarmed

| Level | Bonus |
|-------|-------|
| 5 | -15% mana cost |
| 10 | +15% unarmed attack speed |
| 25 | +10% stun chance on unarmed hits |
| 50 | Visual + **Flurry** -- Punch has a chance to trigger a free Elbow Strike |
| 100 | **Master of Unarmed** -- unarmed attacks ignore 25% of enemy armor |

#### Bladed

| Level | Bonus |
|-------|-------|
| 5 | -15% mana cost |
| 10 | +10% bleed chance on bladed hits |
| 25 | +20% critical damage |
| 50 | Visual + **Razor's Edge** -- Slash has a chance to trigger a free Thrust |
| 100 | **Master of Bladed** -- bladed attacks have +15% chance to cause deep wounds (stacking bleed) |

#### Blunt

| Level | Bonus |
|-------|-------|
| 5 | -15% mana cost |
| 10 | +0.5s stun duration |
| 25 | +15% armor penetration |
| 50 | Visual + **Shockwave** -- Smash has a chance to trigger a mini tremor in a small AoE |
| 100 | **Master of Blunt** -- stunned enemies take +25% damage from all sources |

#### Polearms

| Level | Bonus |
|-------|-------|
| 5 | -15% mana cost |
| 10 | +1 tile attack range |
| 25 | +20% knockback distance |
| 50 | Visual + **Impale** -- Pierce has a chance to pin enemy in place for 1s |
| 100 | **Master of Polearms** -- polearm attacks deal +15% damage to enemies beyond melee range |

#### Shields

| Level | Bonus |
|-------|-------|
| 5 | -15% mana cost |
| 10 | +10% block damage reduction |
| 25 | +20% counter damage after successful block |
| 50 | Visual + **Fortress** -- Block has a chance to reflect damage back to attacker |
| 100 | **Master of Shields** -- blocking restores 2% max HP |

#### Dual Wield

| Level | Bonus |
|-------|-------|
| 5 | -15% mana cost |
| 10 | +10% off-hand weapon damage |
| 25 | +10% critical hit chance |
| 50 | Visual + **Twin Fury** -- Dual Stab has a chance to trigger a free Dual Slash |
| 100 | **Master of Dual Wield** -- every 5th hit is a guaranteed critical |

### Mind

#### Discipline

| Level | Bonus |
|-------|-------|
| 5 | -15% mana cost |
| 10 | +20% buff duration |
| 25 | +2 HP/s regen while any Discipline ability is active |
| 50 | Visual + **Unbreakable** -- Endure has a chance to fully negate one incoming attack |
| 100 | **Master of Discipline** -- all Discipline abilities cost 0 mana when below 25% HP |

#### Intimidation

| Level | Bonus |
|-------|-------|
| 5 | -15% mana cost |
| 10 | +25% debuff duration |
| 25 | +15% debuff potency |
| 50 | Visual + **Dread Presence** -- Ugly Mug has a chance to cause enemies to flee |
| 100 | **Master of Intimidation** -- intimidated enemies deal 20% less damage (permanent until death) |

---

## Ranger

### Weaponry

#### Bowmanship

| Level | Bonus |
|-------|-------|
| 5 | -15% mana cost |
| 10 | +15% projectile range |
| 25 | +10% projectile speed |
| 50 | Visual + **Hunter's Mark** -- Dead Eye has a chance to mark target (+10% damage from all sources) |
| 100 | **Master of Bowmanship** -- critical hits deal triple damage instead of double |

#### Throwing

| Level | Bonus |
|-------|-------|
| 5 | -15% mana cost |
| 10 | +15% throw speed |
| 25 | +1 ricochet bounce (Ricochet ability) |
| 50 | Visual + **Trick Shot** -- Ricochet has a chance to return to the caster (free ammo recovery) |
| 100 | **Master of Throwing** -- thrown weapons have 20% chance to not consume ammo |

#### Firearms

| Level | Bonus |
|-------|-------|
| 5 | -15% mana cost |
| 10 | +15% accuracy |
| 25 | +20% reload speed |
| 50 | Visual + **Deadeye** -- Snipe resets its own cooldown on kill |
| 100 | **Master of Firearms** -- first shot from stealth (Tip Toes) deals +50% damage |

#### CQC

| Level | Bonus |
|-------|-------|
| 5 | -15% mana cost |
| 10 | +25% counter damage on Riposte |
| 25 | +0.5s stagger duration on Shiv |
| 50 | Visual + **Desperation** -- Riposte has a chance to trigger Disengage (free escape) |
| 100 | **Master of CQC** -- successful Parry restores 5% max MP |

### Survival

#### Awareness

| Level | Bonus |
|-------|-------|
| 5 | -15% mana cost |
| 10 | +20% detection range |
| 25 | +10% evasion chance |
| 50 | Visual + **Sixth Sense** -- Tip Toes auto-dodges the first attack received |
| 100 | **Master of Awareness** -- enemies within detection range have attacks slowed by 10% |

#### Trapping

| Level | Bonus |
|-------|-------|
| 5 | -15% mana cost |
| 10 | +25% trap duration |
| 25 | +20% trap damage |
| 50 | Visual + **Dead Zone** -- Snare has a chance to also apply Tripwire's knockdown |
| 100 | **Master of Trapping** -- traps are invisible to enemies (guaranteed trigger) |

#### Sapping

| Level | Bonus |
|-------|-------|
| 5 | -15% mana cost |
| 10 | +15% explosive radius |
| 25 | +20% explosive damage |
| 50 | Visual + **Chain Reaction** -- Frag has a chance to trigger a secondary smaller explosion |
| 100 | **Master of Sapping** -- enemies killed by explosives drop bonus loot |

---

## Mage

### Elemental

#### Fire

| Level | Bonus |
|-------|-------|
| 5 | -15% mana cost |
| 10 | +1s burn duration |
| 25 | +25% burn tick damage |
| 50 | Visual + **Eruption** -- Ignite has a chance to spread to adjacent enemies |
| 100 | **Master of Fire** -- fire spells deal +20% damage to already-burning enemies |

#### Water

| Level | Bonus |
|-------|-------|
| 5 | -15% mana cost |
| 10 | +20% slow potency |
| 25 | +1s freeze duration |
| 50 | Visual + **Shatter** -- killing a frozen enemy deals AoE ice damage |
| 100 | **Master of Water** -- frozen enemies take double damage from the first hit after freeze |

#### Air

| Level | Bonus |
|-------|-------|
| 5 | -15% mana cost |
| 10 | +1 chain target (Chain Shock) |
| 25 | +20% knockback distance |
| 50 | Visual + **Surge** -- Lightning has a chance to strike twice |
| 100 | **Master of Air** -- air spells have +15% crit chance against knocked-back enemies |

#### Earth

| Level | Bonus |
|-------|-------|
| 5 | -15% mana cost |
| 10 | +15% stagger chance |
| 25 | +25% Earthen Armor absorption |
| 50 | Visual + **Aftershock** -- Quake has a chance to trigger a delayed second tremor |
| 100 | **Master of Earth** -- earth spells reduce enemy movement speed by 20% for 3s |

### Aether

#### Aether

| Level | Bonus |
|-------|-------|
| 5 | -15% mana cost |
| 10 | +20% heal amount (Weld and Drain) |
| 25 | +15% gravity pull range (Singularity) |
| 50 | Visual + **Cosmic Echo** -- Nova has a chance to trigger a delayed second burst |
| 100 | **Master of Aether** -- Aether spells cost 25% less mana (stacks with Lv.5 bonus) |

### Attunement

#### Restoration

| Level | Bonus |
|-------|-------|
| 5 | -15% mana cost |
| 10 | +20% heal amount (Mend and Regeneration) |
| 25 | +25% Barrier strength |
| 50 | Visual + **Second Skin** -- Barrier has a chance to refresh itself when broken |
| 100 | **Master of Restoration** -- healing spells also restore 5% max MP |

#### Amplification

| Level | Bonus |
|-------|-------|
| 5 | -15% mana cost |
| 10 | +15% mana regeneration rate |
| 25 | +10% cast speed |
| 50 | Visual + **Overflow** -- Mana Surge has a chance to grant +20% spell damage for 5s |
| 100 | **Master of Amplification** -- mana regen continues at 50% rate during casting |

#### Overcharge

| Level | Bonus |
|-------|-------|
| 5 | -15% mana cost |
| 10 | -15% self-damage from Overcharge abilities |
| 25 | +1s overcharge ability duration |
| 50 | Visual + **Feedback Loop** -- Pain Gate has a chance to convert 2x incoming damage to mana |
| 100 | **Master of Overcharge** -- Last Resort triggers at 25% HP instead of near-death |

---

## Innate Synergies (Affect ALL Abilities)

Innate synergies are unique: they boost ALL Abilities across all categories, not just their own children. These are species-level improvements that enhance everything.

#### Haste

| Level | Bonus |
|-------|-------|
| 5 | -15% mana drain rate |
| 10 | +5% dodge chance while Haste is active |
| 25 | +10% movement speed bonus |
| 50 | Visual + **Blur** -- while Haste is active, 5% chance to phase through projectiles |
| 100 | **Master of Haste** -- Haste mana drain reduced by 50% |

#### Sense

| Level | Bonus |
|-------|-------|
| 5 | -15% mana drain rate |
| 10 | Detection shows enemy type and threat level |
| 25 | +25% detection range |
| 50 | Visual + **Prescience** -- Sense reveals trap locations and hidden paths |
| 100 | **Master of Sense** -- detected enemies have -10% evasion against you |

#### Fortify

| Level | Bonus |
|-------|-------|
| 5 | -15% mana drain rate |
| 10 | +10% damage resistance while active |
| 25 | -20% Fortify mana drain rate |
| 50 | Visual + **Stone Skin** -- Fortify has a chance to fully negate one hit per activation |
| 100 | **Master of Fortify** -- Fortify also reduces status effect duration by 30% |

#### Armor

| Level | Bonus |
|-------|-------|
| 5 | +10% armor effectiveness |
| 10 | +15% armor effectiveness |
| 25 | Class-specific: Warrior +max HP, Ranger +evasion, Mage +mana shield |
| 50 | Visual + class-specific proc: Warrior absorbs one killing blow per floor, Ranger dodge bonus on low HP, Mage mana shield absorbs overflow damage |
| 100 | **Master of Armor** -- equipped armor provides 25% more stats |

*Note: Armor Lv.5 grants armor effectiveness instead of mana cost reduction because Armor is always-on passive (no mana cost to reduce).*

---

## Acceptance Criteria

- [ ] Synergy bonuses automatically apply when parent Skill reaches threshold level
- [ ] Universal Lv.5 bonus (-15% mana cost) works for all masteries
- [ ] Per-mastery Lv.10 and Lv.25 bonuses are unique and identity-reinforcing
- [ ] Lv.50 procs trigger at a noticeable but not overwhelming rate (~10-15% chance)
- [ ] Lv.100 "Master" title displays on the Skill in the UI
- [ ] Lv.100 bonuses are mechanically significant and unique
- [ ] Innate synergies affect ALL Abilities, not just children
- [ ] Armor synergies use armor effectiveness instead of mana cost
- [ ] Synergy bonuses visible in Skill tooltip/detail view
- [ ] Visual overhaul at Lv.50 applies to all child Ability particle effects
