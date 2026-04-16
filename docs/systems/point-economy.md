# Point Economy — SP & AP

## Summary

Two separate point pools fund the Skills & Abilities system. **Skill Points (SP)** invest in passive masteries. **Ability Points (AP)** invest in active combat actions. Separate pools prevent a single currency from forcing players to choose between passive growth and active power.

See [skills.md](skills.md) for the full Skills & Abilities system design.

## Current State

**Spec status: LOCKED.** Pool structure, sources, and rates are defined. Exact numbers are balance targets -- subject to tuning during playtesting.

---

## Skill Points (SP)

SP invest in Skills (passive masteries). Each SP allocated to a Skill grants XP toward that Skill's next level.

### SP Sources

| Source | Amount | When |
|--------|--------|------|
| Character level-up | 2 SP | Every level |
| Milestone level-up (every 10th) | 3 SP (1 bonus) | Levels 10, 20, 30... |

**Total SP by level:**

| Level | Total SP Earned |
|-------|----------------|
| 10 | 21 (18 base + 3 milestone) |
| 25 | 52 (48 base + 4 milestone) |
| 50 | 105 (96 base + 9 milestone) |
| 100 | 210 (192 base + 18 milestone) |

### SP Allocation

SP can be allocated to any Skill (passive mastery) the player has access to, including Innate Skills. Each SP grants XP:

```
xp_from_sp = 50 * (1 + target_skill_level * 0.1)
```

This scales with the target Skill's current level -- SP invested in higher-level Skills give proportionally more XP.

---

## Ability Points (AP)

AP invest in Abilities (active combat actions). Each AP allocated to an Ability grants XP toward that Ability's next level. AP come from three sources, creating a richer progression feel than SP alone.

### AP Sources

#### 1. Leveling (~60% of total AP income)

| Source | Amount | When |
|--------|--------|------|
| Character level-up | 3 AP | Every level |
| Milestone level-up (every 10th) | 5 AP (2 bonus) | Levels 10, 20, 30... |

#### 2. Combat Milestones (~25% of total AP income)

Bonus AP from significant combat achievements:

| Milestone | AP Reward |
|-----------|-----------|
| First boss kill per floor | 2 AP |
| Floor clear (all enemies on a floor) | 1 AP |
| Depth milestone (every 10th floor first reached) | 3 AP |

#### 3. Use-Based Per-Category (~15% of total AP income)

Earn AP by using Abilities in combat. Tracked per category -- using Body Abilities earns Body AP, using Survival Abilities earns Survival AP, etc.

| Threshold | Reward |
|-----------|--------|
| Every 100 Ability uses in a category | 1 AP (usable on any Ability in that category) |

**Category tracking:**

| Class | Categories Tracked |
|-------|-------------------|
| Warrior | Body AP, Mind AP |
| Ranger | Weaponry AP, Survival AP |
| Mage | Elemental AP, Aether AP, Attunement AP |

**Total AP by level** (leveling source only, excluding milestones and use-based):

| Level | AP from Leveling | Estimated Total AP (all sources) |
|-------|-----------------|----------------------------------|
| 10 | 32 | ~45 |
| 25 | 79 | ~115 |
| 50 | 159 | ~240 |
| 100 | 318 | ~500 |

*Milestone and use-based AP vary by playstyle. Estimates assume moderate combat engagement.*

### AP Allocation

AP can be allocated to any unlocked Ability (requires parent Skill at level 1+). Each AP grants XP:

```
xp_from_ap = 50 * (1 + target_ability_level * 0.1)
```

Same formula as SP -- scales with the target Ability's current level.

**Category-earned AP restriction:** AP earned from use-based per-category tracking can only be spent on Abilities within that category. AP from leveling and combat milestones can be spent on any Ability.

---

## Budget Philosophy

**SP is simple and predictable.** 2 per level, 1 bonus at milestones. Players always know how much SP they'll have. This suits Skills (passive masteries) because passive progression should feel steady and reliable.

**AP is richer and more dynamic.** Three sources mean AP income varies based on how you play. Aggressive dungeon pushers earn more milestone AP. Players who favor specific categories earn bonus AP in those categories. This suits Abilities (active combat actions) because active gameplay should reward active engagement.

**Neither pool is scarce enough to feel punishing.** At level 50, a player has ~105 SP and ~240 AP. With 23 masteries and 103 abilities across a class, no player invests in everything -- but they have enough to meaningfully specialize in several areas without feeling starved.

---

## Resolved Questions

| Question | Decision |
|----------|----------|
| Single pool or separate? | Separate. SP for Skills, AP for Abilities. Prevents passive/active competition. |
| AP sources? | 3: leveling (primary), combat milestones (bonus), use-based per-category (trickle). |
| Category AP restriction? | Use-based AP is category-locked. Leveling and milestone AP is unrestricted. |
| SP rate? | 2/level, 3 at milestones. Same as the old "skill points" rate. |
| AP rate? | 3/level, 5 at milestones. Higher than SP because there are more Abilities than Skills. |

## Acceptance Criteria

- [ ] SP and AP are separate pools with separate counters in the UI
- [ ] SP awarded on level-up (2 base, +1 bonus at milestones)
- [ ] AP awarded on level-up (3 base, +2 bonus at milestones)
- [ ] AP awarded from combat milestones (boss kills, floor clears, depth milestones)
- [ ] AP use-tracking per category (Body, Mind, Weaponry, Survival, Elemental, Aether, Attunement)
- [ ] Category-earned AP restricted to that category's Abilities
- [ ] Leveling and milestone AP unrestricted (allocatable to any Ability)
- [ ] Point allocation XP formula scales with target level
- [ ] Skills tab shows "SP: N available"
- [ ] Abilities tab shows "AP: N available"
