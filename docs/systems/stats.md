# Stats System

## Summary

Four core stats define a character's strengths: STR, DEX, STA, INT. Stats influence combat, survivability, and abilities. All stat formulas use the same diminishing returns curve so investment always feels consistent.

## Current State

> **Entity Framework:** The diminishing returns formula `raw * (100 / (raw + 100))` is implemented in `StatSystem.GetEffective()`. Derived stats MaxHP and MaxMP are calculated by `StatSystem.GetMaxHP()` and `StatSystem.GetMaxMP()` respectively. The entity framework uses the stat name **VIT** (Vitality) for what this spec calls **STA** (Stamina) -- they are the same stat. See [entity-framework.md](../architecture/entity-framework.md) for the full system architecture.

Stat formulas are **locked**. This doc defines the exact math for every stat-derived value. All formulas are intended to be player-facing — published so the community can create build guides.

## Design

### The Four Stats

| Stat | Name | Primary Class | What It Does |
|------|------|--------------|-------------|
| STR | Strength | Warrior | Melee damage (flat bonus + percentage boost) |
| DEX | Dexterity | Ranger | Attack speed + dodge/evasion chance |
| STA | Stamina | All (Warrior most) | Max HP + passive HP regeneration |
| INT | Intelligence | Mage | Mana pool + mana regen + magicule processing efficiency |

### Diminishing Returns (Universal Curve)

Every stat uses the same soft diminishing returns formula. Early points are very impactful. Later points still help, but each one matters a little less. There is no hard cap — you can always invest more, and it always does something.

**Formula:**

```
effective_value(raw_stat) = raw_stat * (K / (raw_stat + K))
```

Where **K = 100** for all stats.

This means:
- At 10 raw stat: effective = 10 * (100 / 110) = **9.09** (91% efficiency — almost full value)
- At 100 raw stat: effective = 100 * (100 / 200) = **50.00** (50% efficiency — half value, still meaningful)
- At 500 raw stat: effective = 500 * (100 / 600) = **83.33** (17% efficiency — deep investment, small gains)

The curve is gentle early (you don't feel punished at normal stat ranges) and firm late (prevents one stat from dominating everything at extreme values).

**Reference table (universal curve, K = 100):**

| Raw Stat | Effective Value | Efficiency |
|----------|----------------|------------|
| 10 | 9.09 | 91% |
| 25 | 20.00 | 80% |
| 50 | 33.33 | 67% |
| 100 | 50.00 | 50% |
| 200 | 66.67 | 33% |
| 500 | 83.33 | 17% |

**Why K = 100:** At the stat ranges players will see during normal progression (10–100), the curve is forgiving — you get most of your investment back. It only bites hard past 100, which is deep into specialization territory. This matches the power fantasy: you SHOULD feel strong during the main game. Diminishing returns exist to keep extreme endgame builds from breaking the math.

---

### STR — Strength (Warrior Primary)

STR increases melee damage through a **flat bonus plus a percentage boost**. This means STR always adds raw damage AND scales with your weapon — both matter.

**Formulas:**

```
effective_str = raw_str * (100 / (raw_str + 100))

flat_melee_bonus = effective_str * 1.5
percent_melee_boost = effective_str * 0.8%

total_melee_damage = (base_weapon_damage + flat_melee_bonus) * (1 + percent_melee_boost / 100)
```

**Value table:**

| Raw STR | Effective STR | Flat Damage Bonus | % Damage Boost | Example: 50 Base Weapon → Total |
|---------|--------------|-------------------|----------------|-------------------------------|
| 10 | 9.09 | +13.6 | +7.3% | 68.2 |
| 25 | 20.00 | +30.0 | +16.0% | 92.8 |
| 50 | 33.33 | +50.0 | +26.7% | 126.7 |
| 100 | 50.00 | +75.0 | +40.0% | 175.0 |
| 200 | 66.67 | +100.0 | +53.3% | 230.0 |
| 500 | 83.33 | +125.0 | +66.7% | 291.7 |

**Why this feels right:** At 50 STR (a mid-game Warrior), you're hitting for 2.5x your base weapon damage. At 100 STR (a late-game specialist), you're at 3.5x. The flat bonus means even a weak weapon benefits from STR, while the percentage boost rewards finding better weapons. Doubling your STR from 100 to 200 only adds about 30% more total damage — meaningful but not gamebreaking. STR doesn't directly give HP or defense, but Warriors naturally invest in STA too (their secondary class stat), which covers survivability.

---

### DEX — Dexterity (Ranger Primary)

DEX represents reaction time, coordination, and body control. It boosts **attack speed** (how fast you swing or shoot) AND **dodge/evasion chance** (how often you avoid hits entirely). Rangers should feel quick in every way.

**Formulas:**

```
effective_dex = raw_dex * (100 / (raw_dex + 100))

attack_speed_bonus = effective_dex * 1.0%
dodge_chance = effective_dex * 0.5%
```

Attack speed bonus is a percentage increase to attack rate (not a flat frames-faster value). Dodge chance is capped at a functional maximum by the diminishing returns curve — even at 500 DEX, dodge chance is ~42%, never reaching 100%.

**Value table:**

| Raw DEX | Effective DEX | Attack Speed Bonus | Dodge Chance |
|---------|--------------|-------------------|-------------|
| 10 | 9.09 | +9.1% | 4.5% |
| 25 | 20.00 | +20.0% | 10.0% |
| 50 | 33.33 | +33.3% | 16.7% |
| 100 | 50.00 | +50.0% | 25.0% |
| 200 | 66.67 | +66.7% | 33.3% |
| 500 | 83.33 | +83.3% | 41.7% |

**Why this feels right:** At 50 DEX (mid-game Ranger), you attack 33% faster and dodge 1 in 6 hits. That already feels snappy and evasive. At 100 DEX (late-game specialist), you attack 50% faster and dodge 1 in 4 hits — a genuine glass cannon who's hard to pin down. The dodge chance never gets absurd (even at 500 DEX, you still get hit more than half the time), so enemies always feel threatening. DEX is intentionally a "feel" stat — the Ranger should feel different from a Warrior in moment-to-moment gameplay, not just on the stat screen.

---

### STA — Stamina (All Classes, Warrior Favored)

STA gives **max HP** and **passive HP regeneration**. Every class wants some STA, but Warriors get the most benefit because they're in melee range taking constant hits.

**Formulas:**

```
effective_sta = raw_sta * (100 / (raw_sta + 100))

bonus_max_hp = effective_sta * 5.0
hp_regen_per_sec = effective_sta * 0.15

total_max_hp = class_base_hp + level_hp + bonus_max_hp
```

Where `class_base_hp` and `level_hp` come from [leveling.md](leveling.md) (`level_hp = floor(8 + level * 0.5)` per level).

**Value table:**

| Raw STA | Effective STA | Bonus Max HP | HP Regen/sec |
|---------|--------------|-------------|-------------|
| 10 | 9.09 | +45.5 | 1.4/sec |
| 25 | 20.00 | +100.0 | 3.0/sec |
| 50 | 33.33 | +166.7 | 5.0/sec |
| 100 | 50.00 | +250.0 | 7.5/sec |
| 200 | 66.67 | +333.3 | 10.0/sec |
| 500 | 83.33 | +416.7 | 12.5/sec |

**Why this feels right:** At 50 STA (mid-game), you gain ~167 bonus HP — a substantial buffer on top of your base HP from leveling. The HP regen at 5.0/sec means you noticeably recover between fights without making potions useless. At 100 STA (late-game Warrior), +250 HP and 7.5/sec regen makes you a genuine tank. The regen is passive (always on, no activation needed) but not fast enough to out-heal sustained combat damage — it's for recovery between encounters and topping off during lulls. Every class benefits: even a Mage with 25 STA gets +100 HP and 3.0/sec regen, which is the difference between surviving a hit and dying.

---

### INT — Intelligence (Mage Primary)

INT is the most multifaceted stat. It affects **mana pool size**, **mana regeneration rate**, and **magicule processing efficiency** (spells cost less mana AND scroll osmosis requires fewer scrolls). For Mages, it also boosts **spell/skill damage**.

**Formulas:**

```
effective_int = raw_int * (100 / (raw_int + 100))

bonus_max_mana = effective_int * 4.0
mana_regen_per_sec = effective_int * 0.2
processing_efficiency = effective_int * 0.6%
spell_damage_bonus = effective_int * 1.2%

total_max_mana = class_base_mana + bonus_max_mana
mana_cost_multiplier = 1.0 - (processing_efficiency / 100)
scroll_requirement_multiplier = 1.0 - (processing_efficiency / 100)
```

Where `class_base_mana` is Mage: 200, Ranger: 100, Warrior: 60 (from [magic.md](magic.md)).

**Processing efficiency** does double duty:
- **Spell costs reduced** — a spell that normally costs 50 mana costs `50 * mana_cost_multiplier` instead
- **Scroll osmosis accelerated** — a spell that requires 10 scrolls to learn requires `ceil(10 * scroll_requirement_multiplier)` instead

**Spell damage bonus** applies to all spell and skill damage output (Mage spells benefit most because they deal the most base damage).

**Value table:**

| Raw INT | Effective INT | Bonus Mana | Mana Regen/sec | Processing Efficiency | Spell Damage Bonus |
|---------|--------------|-----------|----------------|----------------------|-------------------|
| 10 | 9.09 | +36.4 | 1.8/sec | 5.5% cheaper | +10.9% |
| 25 | 20.00 | +80.0 | 4.0/sec | 12.0% cheaper | +24.0% |
| 50 | 33.33 | +133.3 | 6.7/sec | 20.0% cheaper | +40.0% |
| 100 | 50.00 | +200.0 | 10.0/sec | 30.0% cheaper | +60.0% |
| 200 | 66.67 | +266.7 | 13.3/sec | 40.0% cheaper | +80.0% |
| 500 | 83.33 | +333.3 | 16.7/sec | 50.0% cheaper | +100.0% |

**Why this feels right:** At 50 INT (mid-game Mage), you have +133 bonus mana (bringing the Mage's total pool to 333+), regen at 6.7/sec, spells cost 20% less, and deal 40% more damage. That's a Mage who feels powerful and sustainable. At 100 INT (late-game specialist), spells cost 30% less and deal 60% more — a massive power spike that rewards dedication.

For Warriors and Rangers, INT is still useful but less dramatic. A Warrior with 10 INT gets +36 bonus mana (total pool ~96) and 1.8/sec regen — enough to use a few more skills before tiring out. The processing efficiency helps them too (skills cost a bit less), but with cheaper skills to begin with, the percentage savings are smaller in absolute terms.

**Scroll osmosis example:** An intermediate spell that normally takes 10 scrolls to learn. At 50 INT (20% efficiency), you need `ceil(10 * 0.80) = 8 scrolls`. At 100 INT (30% efficiency), you need `ceil(10 * 0.70) = 7 scrolls`. High INT Mages learn spells faster — their brains retain more from each scroll use.

---

### Stat Growth

Stat growth uses a **hybrid allocation** system. On each level-up, the player receives both:

1. **Automatic class bonuses** — fixed stat increases determined by the character's class (see [classes.md](classes.md))
2. **Free stat points** — points the player allocates manually to any stat

The number of free points scales with level progression, giving players increasing agency over their build as they advance.

### Stat Caps

**No caps.** Stats grow indefinitely, fitting the infinite dungeon theme. Combined with diminishing returns, this means investment in any stat is always rewarded, just with decreasing marginal gains.

### Backpack Size

Backpack size is **fixed** and not influenced by any stat. STR is combat-only — it has no relationship to carrying capacity.

---

### Formula Summary (Quick Reference)

All stats use the same diminishing returns curve: `effective = raw * (100 / (raw + 100))`

| Stat | Derived Values | Multipliers |
|------|---------------|-------------|
| STR | Flat melee bonus: `eff * 1.5` | % melee boost: `eff * 0.8%` |
| DEX | Attack speed bonus: `eff * 1.0%` | Dodge chance: `eff * 0.5%` |
| STA | Bonus max HP: `eff * 5.0` | HP regen/sec: `eff * 0.15` |
| INT | Bonus mana: `eff * 4.0`, Mana regen/sec: `eff * 0.2` | Processing efficiency: `eff * 0.6%`, Spell damage: `eff * 1.2%` |

### Equipment Overlay

Equipment bonuses (STR/DEX/STA/INT from gear) stack onto the allocated raw stat **before** the DR curve is applied — one raw sum, one DR pass. Ring combat focuses (Crit / Haste / Dodge / Block) use the **same soft-cap curve shape** with K = 60 instead of K = 100, and the portion above the soft cap converts into a paired power stat rather than being clamped away. See [combat-equipment-integration.md](combat-equipment-integration.md) for the full overlay model, per-focus formulas, and worked examples.
