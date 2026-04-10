# Building an Isometric ARPG

## Why This Matters
We're building a Diablo-inspired dungeon crawler but haven't internalized what makes the ARPG loop work. Why does killing 1000 identical enemies feel good? Why does a 15-minute session feel complete? Why does loot feel exciting? These aren't accidents — they're engineered.

## Core Concepts

### The ARPG Core Loop
Every ARPG runs on the same loop:

```
FIGHT → LOOT → UPGRADE → PUSH DEEPER → (repeat)
```

Each step feeds the next:
- **Fight**: Enemies give XP, gold, and items. Combat must feel satisfying moment-to-moment.
- **Loot**: Items create power growth AND build decisions. The drop must feel exciting.
- **Upgrade**: Spending gold/materials at the Blacksmith, leveling up, allocating stats. The player feels smarter.
- **Push deeper**: New zone with harder enemies, better loot. The cycle resets at a higher baseline.

The loop is addictive because each step provides a different kind of reward: combat = mastery, loot = surprise, upgrade = control, deeper = achievement.

### Session Design (15-30 Minutes)
A single play session (one dungeon descent) should feel complete. The player should experience:

| Minute | What Happens | Feeling |
|--------|-------------|---------|
| 0-2 | Enter dungeon, first room, easy enemies | Warming up, getting oriented |
| 2-8 | Mid-floor, harder packs, first loot drops | Engaged, making decisions |
| 8-12 | Approaching exit, resource pressure | Tension, risk/reward ("do I push or bank?") |
| 12-15 | Floor boss or exit, major loot explosion | Climax, satisfaction |
| 15+ | Return to town, sell, craft, upgrade | Reflection, planning next run |

**The key:** Every session must have a CLIMAX (boss, loot explosion, floor transition) and a RESOLUTION (return to town, upgrade). Without climax = boring. Without resolution = incomplete.

### What Makes Combat Feel Good
Diablo's combat feels good because of DENSITY + FEEDBACK + POWER CONTRAST:

1. **Density**: Lots of enemies on screen. Killing one feels trivial; killing 20 in 5 seconds feels powerful.
2. **Feedback**: Every hit has sound, visual effect, knockback, and damage number. The player's brain processes this as "I did that."
3. **Power contrast**: Enemies on your level are dangerous. Enemies 10 levels below are trivial. The CONTRAST between "this was hard" and "now it's easy" IS the power fantasy.

### Enemy Design for ARPGs
Enemies serve specific roles in the ARPG loop:

| Role | HP | Damage | Speed | Purpose |
|------|----|----|-------|---------|
| **Fodder** | Very low | Low | Medium | Feel powerful mowing them down. Provide density. |
| **Standard** | Medium | Medium | Medium | The baseline challenge. Test build effectiveness. |
| **Elite/Champion** | High | High | Varies | Create tension. Force the player to pay attention. |
| **Boss** | Very high | High | Variable | Gate progression. Test mastery. Provide climax. |

The mix matters. A floor with ONLY fodder is boring. A floor with ONLY elites is exhausting. The ideal: 60% fodder, 30% standard, 8% elite, 2% boss.

### Loot Psychology
Why do players farm for hours?

- **Variable reward schedule**: Not every kill drops something. The UNCERTAINTY of "will this one drop?" creates anticipation. Fixed drop rates (100%) have no excitement.
- **Near-miss effect**: "That drop was almost perfect — one more affix and it would be godly" keeps players chasing.
- **Incremental upgrade**: Each item is 2-5% better than the last. Small enough that you notice, large enough that it matters.
- **Jackpot moments**: Occasionally (1-2% of drops), something genuinely amazing drops. This one moment fuels 100 hours of play.

### Difficulty Curves
Our zone-based system (10-floor zones, steep inter-zone jumps, gentle intra-zone ramps) mirrors Diablo's difficulty model:

```
Zone 1: Learn the game      → feel capable
Zone 2: Real challenge       → feel tested
Zone 3: Gear matters         → feel strategic
Zone 5+: Build optimization  → feel masterful
Zone 10+: Extreme efficiency → feel godlike
```

The key: **players should feel STUCK at zone boundaries, then POWERFUL after gearing up.** The frustration of "I can't beat Zone 3" followed by the triumph of "I crushed Zone 3" IS the game.

## Godot 4 + C# Implementation

```csharp
// Enemy density: spawn enough enemies to feel impactful
int roomBudget = roomArea / 12;  // 1 enemy per ~12 tiles
// A 12x12 room = 12 enemies. Player wades through them.

// Loot explosion on boss kill
for (int i = 0; i < 5 + floorNumber / 5; i++)
{
    var item = ItemGenerator.GenerateEquipment(floorNumber, rng);
    // Scatter items visually around the boss corpse
    SpawnLootDrop(item, bossPosition + RandomOffset(30));
}

// Power contrast: show enemy threat level via color
Color enemyColor = ColorSystem.GetRelativeColor(enemyLevel, playerLevel);
// Grey = trivial, Green = even, Yellow = hard, Red = deadly
```

## Common Mistakes
1. **Combat has no feedback** — no sound, no shake, no flash = feels like clicking a spreadsheet
2. **All enemies are the same** — no variety = no tactical decisions
3. **Loot always drops** — 100% drop rate = no anticipation, no excitement
4. **No power contrast** — player never feels stronger than enemies, or always feels stronger
5. **Sessions have no climax** — player just... walks through rooms. No boss, no loot explosion, no crescendo.
6. **Difficulty is flat** — no zone walls, no "I can't beat this yet" moments, no triumph when you do
7. **Upgrade path unclear** — player has gold but nothing worth buying

## Checklist
- [ ] Each floor has a climax (boss or loot explosion at exit)
- [ ] Enemy mix: 60% fodder, 30% standard, 8% elite, 2% boss
- [ ] Every hit has at least 2 forms of feedback (visual + audio OR visual + number)
- [ ] Loot drops are variable rate (not guaranteed)
- [ ] Player feels power contrast when returning to old zones
- [ ] Session completes in 15-30 minutes (town → dungeon → boss → town)

## Sources
- [GDC: Diablo Postmortem (David Brevik)](https://www.youtube.com/watch?v=VscdPA6sUkc)
- [GDC: Designing PoE to Be Played Forever (Chris Wilson)](https://www.youtube.com/watch?v=pM_5S55jUzk)
- [GDC: Loot Systems and Game Design (various)](https://www.gamedeveloper.com/design/the-psychology-of-loot-boxes)
- [Diablo Design Deep Dive (Maxroll)](https://maxroll.gg/d2/resources/game-mechanics)
