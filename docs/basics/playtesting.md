# Playtesting

## Why This Matters

We've been building systems that pass unit tests but don't feel right when played. Specs describe WHAT should happen. Playtesting reveals HOW IT FEELS. A game that matches the spec perfectly can still feel terrible. Playtesting is how you find out.

## Core Concepts

### Why Playtesting > Specs

Specs can tell you:
- "Attack deals 13 damage" ✓
- "Enemies have 30 HP" ✓
- "Attack cooldown is 420ms" ✓

Specs CANNOT tell you:
- "Does combat feel satisfying?" ✗
- "Is the player bored by floor 3?" ✗
- "Do enemies feel threatening or annoying?" ✗
- "Is the UI intuitive without explanation?" ✗

These questions can ONLY be answered by playing the game.

### Developer Blindness

You (or the AI that wrote the code) know exactly how everything works. You know where the NPCs are, what buttons to press, how the combat system calculates damage. A real player knows NONE of this.

**Fix:** Play the game as if you've never seen it. Don't use knowledge of the code. If something isn't obvious from the screen, it needs to be communicated better.

### The First 5 Minutes Test

New players decide within 5 minutes whether a game is worth playing. In those 5 minutes, they need to:

1. **Understand what they control** (character, movement)
2. **Understand the goal** (go deeper, get stronger)
3. **Successfully do SOMETHING** (kill an enemy, pick up an item)
4. **Feel a reward** (XP, loot, level up)
5. **Want to do it again** (next floor, next fight)

If ANY of these fail in the first 5 minutes, the player quits.

### What to Observe During Playtesting

| Observation | What It Means | Fix |
|-------------|--------------|-----|
| Player pauses frequently | Confused about controls or objectives | Better onboarding, clearer UI |
| Player ignores an NPC/feature | Didn't notice it or doesn't understand it | Make it more visible, add prompt |
| Player dies and looks frustrated | Difficulty spike or unfair damage | Reduce enemy damage, add telegraph |
| Player dies and immediately retries | Good difficulty — challenging but fair | No fix needed |
| Player stops engaging with combat | Combat is boring or too easy | Add variety, increase difficulty |
| Player doesn't use the Blacksmith | Doesn't understand crafting or doesn't need it | Better tutorial, make crafting more rewarding |
| Player walks in circles | Lost, no clear direction | Add visual guides, minimap markers |

### How to Playtest Your Own Game

1. **Play for 15 minutes without debugging tools** — no collision shapes, no console
2. **Note every moment of confusion** — "I didn't know I could do that"
3. **Note every moment of frustration** — "that felt unfair"
4. **Note every moment of satisfaction** — "that felt great"
5. **Time each session** — does a floor take 3 minutes or 30?
6. **Count deaths** — are you dying 0 times (too easy) or 10 times (too hard)?

### Tuning Through Play

Constants like damage, HP, speed, and XP curve are NEVER right on the first try. The spec gives a starting point. Playtesting reveals reality.

**Tuning process:**

1. Play with default values
2. Write down what feels wrong: "enemies die too fast," "not enough gold"
3. Change ONE constant at a time (e.g., enemy HP × 1.5)
4. Play again
5. Repeat until it feels right
6. Update the spec to match the tuned values

**What to tune:**
- Enemy HP and damage (combat pacing)
- Drop rates (reward frequency)
- XP curve (leveling speed)
- Potion healing amount (resource management)
- Movement speed (world traversal feel)
- Attack cooldown (combat rhythm)

### When to Trust Spec vs Feel

| Trust the Spec | Trust the Feel |
|---------------|---------------|
| Math relationships (XP = L² × 45) | Absolute values (should 45 be 50?) |
| System architecture (how damage is calculated) | Numbers plugged in (is 15% crit too low?) |
| Feature design (what the Blacksmith does) | Tuning (is 50g too expensive for a potion?) |

The STRUCTURE of systems comes from specs. The NUMBERS come from playtesting.

## Common Mistakes

1. **Never playing the game** — testing only through unit tests and headless runs
2. **Playing with debug tools on** — collision shapes and console distract from the player experience
3. **Changing multiple things at once** — can't tell which change made the difference
4. **Ignoring frustration** — "the spec says it should work" doesn't matter if it feels bad
5. **Not timing sessions** — you think it takes 15 minutes but it actually takes 45
6. **Only testing happy path** — what happens when the player does something unexpected?

## Checklist

- [ ] Play the full loop (menu → town → dungeon → boss → town) at least once before committing
- [ ] Note confusion, frustration, and satisfaction moments
- [ ] Time a full session (should be 15-30 minutes)
- [ ] Test death and respawn flow
- [ ] Test with NO prior knowledge of the code
- [ ] Tune one constant at a time based on feel

## Sources

- [GDC: Playtesting Your Game (Josh Sawyer)](https://www.youtube.com/watch?v=dHMNeNapL1E)
- [Gamasutra: The Art of Playtesting](https://www.gamedeveloper.com/design/the-art-of-playtesting)
- [Extra Credits: Playtesting 101](https://www.youtube.com/watch?v=on7endO4lPY)
- [Valve's Playtesting Methodology (Portal/L4D)](https://www.valvesoftware.com/en/publications)
