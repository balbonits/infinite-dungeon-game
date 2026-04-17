# Ability Affinity

## Summary

Cosmetic-only milestones earned by repeatedly using a specific Ability. The more you use an Ability, the better it looks. No stat bonuses -- pure visual reward for dedication.

See [skills.md](skills.md) for the full Skills & Abilities system design.

## Current State

**Spec status: LOCKED.** Affinity tiers, use thresholds, and visual effects are defined. Exact particle/shader details are implementation-phase art decisions.

---

## Design

### Philosophy

Ability affinity rewards **loyalty to a playstyle**. A player who uses Fireball 5,000 times has a relationship with that ability -- their Fireball should look different from a player who just unlocked it. This is the "favorite weapon" effect: the tool you rely on reflects your experience with it.

No stats. No damage. No advantage. Just visual flair that says "I've been here before."

### Affinity Tiers

| Uses | Tier | Visual Effect |
|------|------|---------------|
| 100 | Familiar | Slightly brighter particles, subtle glow on cast |
| 500 | Practiced | Unique particle color tint (shifts toward mastery color) |
| 1,000 | Expert | Trail effect added to projectiles/swings |
| 5,000 | Mastered | Full visual overhaul -- premium particles, distinct sound variation |

### How It Works

- **Tracked per Ability.** Each Ability has its own use counter. Slash and Thrust track separately even though they share the Bladed mastery.
- **Persistent.** Use counts are saved and never reset. Death, respec, nothing removes them.
- **Cumulative.** Each tier includes all previous tiers' effects. A Mastered Fireball has the glow, the color shift, the trail, AND the full overhaul.
- **Visible to others.** In any future multiplayer/spectate context, other players see your affinity visuals.

### Tier Details

**Familiar (100 uses):** The Ability starts to feel "yours." Particles are slightly brighter, cast animations have a subtle glow edge. Barely noticeable to others, satisfying to you.

**Practiced (500 uses):** The Ability takes on your mastery's color identity. A Practiced Fireball shifts from generic orange to the Fire mastery's signature deep red-gold. A Practiced Slash gets a subtle bladed-gold streak.

**Expert (1,000 uses):** Motion trails. Projectiles leave afterimages. Melee swings leave light trails. Toggle abilities shimmer. This is where other players notice.

**Mastered (5,000 uses):** Full premium treatment. Unique particle systems, distinct cast sounds, impactful screen effects. A Mastered Fireball looks fundamentally different from a regular one -- bigger impact particles, richer flames, a bass thud on impact. This is the "I've thrown 5,000 of these" flex.

### Edge Cases

- **Passive Abilities** (Keen Senses, Rangefinding, etc.): Track "time active" instead of uses. 1 second active = 1 use for affinity purposes.
- **Toggle Abilities** (Tip Toes, Fortify, etc.): Track toggle activations, not time. Toggling on = 1 use.
- **Innate Skills** (Haste, Sense, Fortify): Track activations. Armor is always-on and does NOT have affinity tiers.

---

## Acceptance Criteria

- [ ] Each Ability tracks total uses (persistent, never resets)
- [ ] Affinity tier unlocks at 100, 500, 1,000, and 5,000 uses
- [ ] Visual effects are cumulative (each tier adds to previous)
- [ ] Affinity tier visible in Ability detail/tooltip
- [ ] Particle brightness increase at Familiar (100)
- [ ] Color tint shift at Practiced (500)
- [ ] Trail effects at Expert (1,000)
- [ ] Full visual overhaul at Mastered (5,000)
- [ ] Passive abilities track time active instead of use count
- [ ] Toggle abilities track activations
- [ ] Armor Innate excluded from affinity system
- [ ] No stat bonuses at any tier -- purely cosmetic
