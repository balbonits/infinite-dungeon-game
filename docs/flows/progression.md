# Flow: Progression

## XP Gain

Per enemy kill: `Constants.EnemyStats.GetXpReward(enemyLevel)`

Formula: `8 + 4 * level`

XP added to `GameState.Xp`, triggers `StatsChanged` signal.

## Level Up

Threshold: `level^2 * 45`

| Level | XP Required |
|-------|-------------|
| 1 → 2 | 45 |
| 2 → 3 | 180 |
| 5 → 6 | 1,125 |
| 10 → 11 | 4,500 |

```
When Xp >= threshold:
1. Xp -= threshold
2. Level += 1
3. Apply class level bonus (StatBlock.ApplyClassLevelBonus)
4. Award free stat points (+3, or +5 at milestone levels)
5. Award skill points (+2, or +3 at milestone levels)
6. HP restore: 15% of new MaxHp
7. Mana restore: 100% (full)
8. Start grace period (invincibility)
9. Emit StatsChanged
10. Check for level-based achievements
```

## Stat Allocation

- Free points from leveling: `StatBlock.FreePoints`
- Allocate via StatAllocDialog (pause menu → Stats)
- Each point: +1 to STR, DEX, STA, or INT
- Points are permanent (no respec)

## Skill XP (Use-Based)

When a skill is used in combat:
```
1. SkillTracker.RecordUse(skillId, floorNumber)
2. Floor multiplier: 1 + (floor - 1) * 0.5
3. XP to used skill: def.BaseXpPerUse * floorMultiplier
4. XP to parent base skill (if specific skill used)
5. Skill level formula: level^2 * 20 XP per level
6. Level 0 → 1 is instant (0 XP required)
```

## Skill Point Allocation

```
SkillTracker.AllocatePoint(skillId):
1. Check SkillPoints > 0
2. Check skill exists and matches player class
3. If specific skill: check parent base skill at level 1+
4. Deduct 1 skill point
5. Add XP: 50 * (1 + targetSkillLevel * 0.1)
```
