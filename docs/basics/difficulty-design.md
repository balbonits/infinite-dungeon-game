# Difficulty Design

## Why This Matters

Our zone-based difficulty system (10-floor zones, steep inter-zone jumps) needs to feel fair. If difficulty spikes too hard, players quit. If it's too easy, they're bored. Understanding how successful games design difficulty curves helps us tune ours.

## Core Concepts

### The Difficulty Curve

Good games follow an escalating wave pattern, not a straight line:

```
Difficulty
  ▲
  │    ╱╲     ╱╲╲     ╱╲╲╲
  │   ╱  ╲   ╱   ╲╲  ╱    ╲╲╲
  │  ╱    ╲ ╱     ╲╲╱      ╲╲╲
  │ ╱      V       V
  │╱
  └──────────────────────────────→ Time
     Zone 1    Zone 2    Zone 3
```

Each zone has: **ramp up → peak (boss) → brief relief → ramp up again**. The player is never comfortable for long, but they get breathing room between challenges.

### Zone Walls vs Smooth Scaling

**Zone walls** (our approach): Difficulty jumps sharply at zone boundaries (floors 11, 21, 31...). Within a zone, it ramps gently.

**Why walls work:** They create clear "I need to get stronger" moments. The player farms Zone 1 until they're overpowered, then pushes into Zone 2 and feels challenged again. The CONTRAST between "I was a god" and "I'm struggling" is motivating.

**Our formula:**

```
zone_multiplier = 1.0 + (zone - 1) * 0.5
intra_zone_multiplier = 1.0 + (intra_step * 0.05)
total = zone_multiplier * intra_zone_multiplier
```

Floor 10 → 11 jump: 1.45x → 1.50x (+3.4%). This is a STEEP jump in practice because the player's gear was optimized for Zone 1.

### "Hard But Fair"

The FromSoftware principle. Every death should feel like the player's fault, not the game's:

- **Telegraphed attacks**: Enemies always telegraph before hitting. No instant damage.
- **Consistent rules**: Same enemy, same behavior, every time. No random instant kills.
- **Learnable patterns**: Each enemy has 2-4 attacks the player can memorize.
- **Escalating punishment**: Early zones are forgiving. Deep zones punish mistakes harder.
- **Escape routes**: The player can always retreat to a safer floor.

### Tuning Methodology

1. **Set initial values from formulas** (our spec-driven approach)
2. **Playtest the first 30 minutes** — is the player dying too much? Too little?
3. **Adjust constants, not formulas** — change the 0.5 zone multiplier to 0.4, don't rewrite the formula
4. **Test extremes** — play floor 1 (should be trivial), floor 50 (should be hard), floor 100 (should be very hard)
5. **Watch for "feels unfair" moments** — if the player says "that was BS," the difficulty curve has a spike

### Dynamic Difficulty (Advanced)

Some games adjust difficulty based on player performance. Options:

- **Rubber banding**: If player dies 3 times on a floor, reduce enemy HP by 10%
- **Dungeon mood**: Our living dungeon could become "generous" after repeated deaths (more drops, weaker enemies)
- **Player choice**: Dungeon Pacts let the player increase difficulty for better rewards

We currently don't have dynamic difficulty, but the Dungeon Pact system (from endgame research) would add voluntary difficulty modifiers.

## Common Mistakes

1. **Flat difficulty** — no peaks or valleys, just a straight line upward (boring)
2. **Spikes without warning** — sudden jump from easy to impossible (feels unfair)
3. **No breathing room** — constant high difficulty causes burnout
4. **Testing at developer skill level** — devs are better than new players; playtest with fresh eyes
5. **Tuning formulas instead of constants** — rewriting the entire scaling system when you just need to change one number

## Checklist

- [ ] Each zone has a ramp → peak → relief pattern
- [ ] Zone boundary jump is noticeable but not impossible
- [ ] Floor 1 is trivially easy (onboarding)
- [ ] Boss floors are clearly harder than normal floors
- [ ] Player always has a way to retreat and farm

## Sources

- [GDC: Difficulty in Games (Mark Brown)](https://www.youtube.com/watch?v=kJf_3mJLHco)
- [Game Maker's Toolkit: What Makes a Good Difficulty Curve](https://www.youtube.com/watch?v=VL3Kmj8W5W0)
- [Designing Difficulty in Dark Souls](https://www.gamedeveloper.com/design/dark-souls-difficulty-design)
