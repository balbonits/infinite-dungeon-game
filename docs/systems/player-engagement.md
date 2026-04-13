# Player Engagement

## Summary

Design principles for keeping players engaged, session pacing, feedback loops, game feel ("juice"), and retention. This doc covers the *game design* side of engagement — for the technical analytics and feedback collection architecture, see [analytics.md](../architecture/analytics.md).

Informed by research across GDC talks (Vlambeer's "Art of Screenshake", Supergiant's Hades post-mortem, David Brevik's Diablo retrospective), game design literature (Raph Koster's "A Theory of Fun", Steve Swink's "Game Feel", Celia Hodent's "The Gamer's Brain"), and player psychology research (flow theory, goal-gradient hypothesis, Zeigarnik effect).

## Current State

Design phase. These principles guide future implementation decisions for combat feel, UI feedback, session flow, and reward systems.

## Design

### Core Principle: Engagement Loops, Not Compulsion Loops

| Engagement (use these) | Compulsion (avoid these) |
|------------------------|-------------------------|
| "I wonder what's on the next floor" | "Log in daily or lose your streak" |
| Rested XP that accumulates passively | Timed rewards that expire |
| Player-driven pacing | Artificial lockouts |
| Optional challenge modifiers | Mandatory dailies |
| Welcoming return with bonuses | Punishing missed sessions |

This is a single-player game with no monetization motive for compulsion. Every engagement mechanic must create intrinsic motivation (curiosity, empowerment) not extrinsic pressure (obligation, FOMO).

### Three Nested Gameplay Loops

| Loop | Timeframe | What Happens | Engagement Source |
|------|-----------|--------------|-------------------|
| **Micro** | 3–10 seconds | Attack → damage feedback → kill → drop | Immediate visceral satisfaction |
| **Mid** | 5–10 minutes | Clear floor → loot → level up → next floor decision | Goal completion + anticipation |
| **Macro** | Hours / sessions | Unlock skills → craft gear → push deeper floors → new content | Long-term purpose + curiosity |

The micro loop must feel good *immediately*. The mid loop must have a clear goal and payoff. The macro loop must give long-term purpose.

### Session Design

**Target session length:** 20–60 minutes (3–6 floors)

**Pacing principles:**
- Each dungeon floor should take **5–10 minutes** to clear
- Floor transitions should take **under 5 seconds** with no mandatory menus
- Save points are **between floors** (moments of anticipation, not completion)
- The XP bar should be **always visible** and players should frequently end floors close to the next level (goal-gradient effect — people accelerate effort as they approach a goal)

**"Just one more floor" triggers:**
- Low friction: one button press to enter next floor
- Show preview of next floor (enemy types, potential rewards) before entering
- XP bar near completion: "90% to next level — one more floor"
- New content tease: "Floor 15 unlocks Boss encounters"
- Residual momentum: player is still in flow state from last floor

**Natural stopping points that bring players BACK:**
- Save between floors with a tease of the next floor's theme/rewards
- Show upcoming milestones on the pause/save screen: "Next level in 340 XP. Next skill unlock at Level 15."
- Unlock something but don't let them use it yet: "Blacksmith's new tier unlocked — visit town to try it"
- Rested XP accumulating while away (see [leveling.md](leveling.md))

### Game Feel / "Juice" Checklist

Every interaction needs feedback. A game that feels good to play retains players through feel alone.

| Event | Visual Feedback | Audio Feedback | Feel Feedback |
|-------|----------------|----------------|---------------|
| Hit enemy | Damage number popup (color-coded), enemy flash white, particles | Impact sound (pitch-shifted per hit) | Hitstop (2–3 frames), knockback |
| Kill enemy | Death animation, larger particles, loot scatter | Satisfying crunch/shatter | Brief screen shake |
| Take damage | Screen edge flash red, HP bar shake | Pain sound, heartbeat if low HP | Screen shake (already designed: ±3px, 90ms) |
| Level up | Full-screen flash, particle burst, XP bar fill animation | Ascending fanfare chime | Brief pause (200ms), subtle zoom on character |
| Loot drop | Item bounces on ground, glow effect (color from gradient system) | "Ding" sound, pitch/volume scaled by rarity | Slight time slow on rare drops (100ms) |
| XP gain | Numbers float toward XP bar, bar fills with smooth animation | Subtle tick sound | — |
| Floor cleared | Doorway/portal glow, completion banner | Achievement-style chime | — |
| Near level-up | XP bar pulses/glows when >80% full | — | — |

**Key references:**
- Hitstop: pause for 2–3 frames on hit to emphasize impact (used in Hades, Hollow Knight, Dead Cells)
- Knockback: enemies physically react to hits, confirming the action mattered
- Damage numbers: visible, color-coded by type (physical/elemental), scaled by damage amount
- Screen shake: proportional to event severity (light on hit, heavier on big crits/boss attacks)

### Retention Design

**Death as progress, not punishment:**
- Death screen emphasizes *gains* (XP earned, items found, floors cleared)
- Suggest what to try next: "You almost cleared Floor 12. Try upgrading your Bladed skill."
- Procedural generation means the next attempt is different — not replaying the same content
- Death penalties exist but are mitigatable (gold buyout, Sacrificial Idol) — see [death.md](death.md)
- One button from death screen to respawn — minimize friction

**The Zeigarnik Effect (uncompleted tasks create mental tension):**
- Save between floors, not after boss kills — the player quits while looking *forward*, not while feeling *done*
- Tease future content in the save screen: locked skill branches, upcoming boss floors, crafting recipes

**Rested XP** rewards returning after a break without punishing absence. See [leveling.md](leveling.md).

**No time-limited content.** No dailies, no expiring rewards, no seasonal FOMO. The dungeon is always there.

### Loot Engagement

**Layered reinforcement** — three reward types running simultaneously:

| Layer | Type | Example | Psychology |
|-------|------|---------|-----------|
| Predictable | XP, materials, gold | Every kill rewards XP | "Salary" — reliable, prevents frustration |
| Unpredictable | Rare drops, unique items | Variable-ratio loot drops | "Lottery" — exciting when it hits |
| Milestone | Boss kills, floor clears, achievements | Complete Floor 10 → reward | "Promotion" — earned through sustained effort |

A player who gets no rare drops should still feel they progressed (XP, materials). A player who gets a rare drop should feel thrilled. Both should want to play again.

**Smart loot:** Weight drops toward the player's class and active skill build. Nothing kills engagement faster than 10 rare drops for a class you're not playing.

**Bad luck protection / pity timer:** Guarantee a notable drop after N kills without one. Hidden from the player, but prevents excessively dry streaks. The player should never go more than ~15 minutes without *something* worth picking up.

**Blacksmith recycling** (see [classes.md](classes.md)): Off-affinity gear the player doesn't want is recycled into crafting materials. No wasted loot — every drop has value.

### Moment-to-Moment Engagement

**The XP bar as an engagement tool:**
- Always visible — the player always knows how close the next level is
- Smooth fill animation (not instant jumps) — the *animation of progress* is itself rewarding
- The last 20% should feel faster (encounter density naturally increases on deeper floors)
- "Near miss" on level-up is a powerful retention hook — ending a floor at 90% to next level almost guarantees "one more"

**Enemy variety prevents pattern stagnation:**
- New enemy types at floor thresholds (Raph Koster: fun = brain's reward for pattern recognition; fun stops when patterns are fully internalized)
- Visual variety, behavioral variety, not just stat scaling
- Floor modifiers that change combat dynamics (darkness, poison floors, time pressure)

**Player agency in difficulty:**
- Players choose which floor to enter — deeper = harder + better rewards
- No artificial gates — if you want to push floor 50 at level 30, you can try (the color system warns you)
- The color gradient system makes risk/reward visible at a glance

## Open Questions

- Exact hitstop duration (2 vs 3 frames) — needs playtesting
- Should rare loot drops have a brief time-slow effect or is that too disruptive?
- How frequently should micro-surveys appear (if analytics are opted into)?
- Should floor previews show exact enemy types or just a difficulty color/rating?
- How does the "just one more floor" design interact with death penalties? (If you die chasing "one more," the penalty feels worse)
- Should there be a "daily challenge" floor variant (rotating, not time-limited)?
