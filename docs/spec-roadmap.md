# Spec Roadmap — What's Next

**Purpose:** durable across compact/clear sessions. Records the prioritized list of specs to author, with dependency reasoning. Update the checkboxes as each spec lands.

**Last updated:** 2026-04-18 (after Wave 2 art-spec completion, commit `0eab934`).

**Next up:** Phase A reconciliation specs (3 mechanical edits, no new design, but unblock everything downstream).

---

## How to read this

- Phases run in order (A → J). Within a phase, specs are mostly serial because each defines a number the next depends on.
- Across phases, some can parallelize once their inputs are locked (E and F can fan out within themselves; G/H/I are mostly independent of each other once their input phases land).
- Each spec lists what it **defines for the next** — the load-bearing handoff to the next ticket. If you do them out of order, that handoff breaks and forces re-edits.

---

## Phase A — Reconciliation (no new design, unify what's drifted)

Zero new design; every downstream spec inherits cleaner numbers. Highest "value per token" specs in the whole roadmap.

- [ ] **SPEC-RECONCILE-BRACKETS-01** (AUDIT-09)
   `item-generation.md` says 5 brackets, `depth-gear-tiers.md` says 7, code matches 7. Pick canonical, edit the loser.
   *Defines for next*: which bracket count appears in every loot/affix/quality discussion.

- [ ] **SPEC-AFFIX-TIER-LADDER-01** (AUDIT-10)
   `AffixDatabase.GetMaxTier` claims tiers 5/6 exist; registry only has 1-4. Either add 5/6 affixes or pin GetMaxTier at 4.
   *Defines*: affix space size. Depends on #1.

- [ ] **SPEC-CRAFTING-QUALITY-LADDER-01** (AUDIT-11)
   `Crafting.RecycleItem` quality-bonus switch missing Masterwork/Mythic/Transcendent.
   *Defines*: recycle gold values designers balance against. Depends on #1 + #2.

---

## Phase B — Magic system foundation (biggest design gap)

`magic.md` has 5 open questions; they cascade through 4+ other docs.

- [ ] **SPEC-MAGICULE-DENSITY-01**
   Formula for magicule density vs floor (linear / exponential / curve). Define dangerous-floor threshold.
   *Defines for next*: feeds mana drain, `magicule-attunement.md`, `dungeon-pacts.md`, `dungeon-intelligence.md`. **The load-bearing magic number — every Phase B/C/D spec inherits this.**

- [ ] **SPEC-INNATE-MANA-COST-01**
   Per-Innate (Sense / Fortify / Haste) mana drain at level 1 + scaling per skill level.
   *Defines*: how often a mage can keep an Innate active at floor N. Depends on #4.

- [ ] **SPEC-INNATE-STACKING-01**
   Can Fortify + Haste both run? One-active limit? Self-balancing via mana drain or hard cap?
   *Defines*: mage build space. Depends on #5.

---

## Phase C — Skills/Abilities completion (kills SKILLS_AND_ABILITIES_SYSTEMS.md TBDs)

- [ ] **SPEC-SKILL-POINTS-RATE-01**
   Skill points per level + mastery threshold rates ("needs adjustment for separate pools" TBD).
   *Defines*: progression speed. Depends on Phase B (mage rate must match mana economy).

- [ ] **SPEC-INNATE-SYNERGIES-01**
   "Innate synergies affect ALL abilities at threshold levels (details TBD)" — cross-pool synergy.
   *Defines*: build-complexity ceiling. Depends on #6 + #7.

- [ ] **SPEC-MASTERY-THRESHOLD-FX-01**
   Cosmetic flair at use-based milestones (`ability-affinity.md` TBD details).
   *Defines*: feedback-loop visuals. Depends on #8.

---

## Phase D — Combat tuning derivatives

- [ ] **SPEC-MAGIC-COMBAT-FORMULA-01**
   Mage spell damage scaling — INT-only or INT × density?
   *Defines*: mage damage curve. Depends on #4 (density) + COMBAT-01 (locked).

- [ ] **SPEC-COMBAT-03** (flagged in COMBAT-02)
   Per-archetype tuning: Sword vs Axe vs Hammer; Shortbow vs Longbow vs Crossbow.
   *Defines*: weapon-choice meaningfulness. **Wait until COMBAT-01 + COMBAT-02 are implemented** — needs real equipment to balance against.

---

## Phase E — Per-species instance specs (gated on Phase B for stat scaling)

Each species spec instantiates SPEC-SPECIES-01 template. Each defines its corresponding boss spec's base (boss inherits species body plan + stat baseline).

Parallelizable within the phase, but ALL gated on Phase B (stat snapshots at floor 28 / 75 depend on the magicule density curve).

- [ ] **SPEC-SPECIES-BAT-01** — already exemplar in template; lift to `docs/world/species/bat.md`. Defines body-plan exemption (airborne) + scale band Small.
- [ ] **SPEC-SPECIES-SKELETON-01** (zone 1) — defines Bone Overlord base.
- [ ] **SPEC-SPECIES-WOLF-01** (zone 2) — defines Howling Pack-Father base.
- [ ] **SPEC-SPECIES-SPIDER-01** (zone 3) — defines Chitin Matriarch base.
- [ ] **SPEC-SPECIES-DARKMAGE-01** (zone 4) — defines Hollow Archon base.
- [ ] **SPEC-SPECIES-ORC-01** (zone 5) — defines Warlord of the Fifth base.
- [ ] **SPEC-SPECIES-GOBLIN-01** (zone 7) — defines Iron-Gut Goblin King base.

(Bat at zone 6 boss "Screaming Flight" is a swarm-fused variant — may need its own sub-spec.)

---

## Phase F — Per-boss instance specs (gated on Phase E)

Each fully expands what's currently a "skeleton fill" in `boss-art.md` §7-13. Each boss spec depends on its species spec from Phase E.

- [ ] **SPEC-BOSS-BONE-OVERLORD-01** (zone 1) — needs SPEC-SPECIES-SKELETON-01.
- [ ] **SPEC-BOSS-HOWLING-PACK-FATHER-01** (zone 2) — needs SPEC-SPECIES-WOLF-01.
- [ ] **SPEC-BOSS-CHITIN-MATRIARCH-01** (zone 3) — needs SPEC-SPECIES-SPIDER-01.
- [ ] **SPEC-BOSS-HOLLOW-ARCHON-01** (zone 4) — needs SPEC-SPECIES-DARKMAGE-01.
- [ ] **SPEC-BOSS-WARLORD-FIFTH-01** (zone 5) — needs SPEC-SPECIES-ORC-01.
- [ ] **SPEC-BOSS-SCREAMING-FLIGHT-01** (zone 6) — Bat swarm-fused variant; may need its own species sub-spec first.
- [ ] **SPEC-BOSS-IRON-GUT-GOBLIN-KING-01** (zone 7) — needs SPEC-SPECIES-GOBLIN-01.
- [ ] **SPEC-BOSS-VOLCANO-TYRANT-01** (zone 8) — deep-zone mix, orc-form base.

Each defines: AI behavior tree, phase-shift trigger thresholds, FlashFx hooks, first-kill drop overrides.

---

## Phase G — NPC dialogue & service-menu (blocks NPC-ROSTER-REWIRE-01 impl)

- [ ] **SPEC-NPC-DIALOGUE-VOICES-01**
   Voice conventions: Blacksmith ("pioneer smith learning"), Guild Maid ("crisp service"), Village Chief ("wise elder").
   *Defines*: per-NPC dialogue tone. Required before writing actual trees.

- [ ] **SPEC-VILLAGE-CHIEF-DIALOGUE-01**
   Village Chief is fresh AND is the quest giver — his lines drive quest acceptance flow.
   *Defines*: quest-flow language + first-impression voice.

- [ ] **SPEC-BLACKSMITH-MERGED-MENU-01**
   Blacksmith now has Forge + Craft + Recycle + Shop tabs (4 tabs from 3 prior NPCs' worth of services).
   *Defines*: service-menu UX wireframe before NPC-ROSTER-REWIRE-01 impl.

- [ ] **SPEC-GUILD-MAID-MERGED-MENU-01**
   Guild Maid: Bank + Teleport + (no quest pickup; routes to Village Chief).
   *Defines*: same as above for Guild Maid.

These four collectively unblock NPC-ROSTER-REWIRE-01 implementation.

---

## Phase H — UI canonical decisions (cascades through every text-bearing surface)

- [ ] **SPEC-UI-FONT-01**
   Pick canonical font (Trebuchet / Press Start 2P / Silkscreen / Alagard / etc.).
   *Defines for next*: every text-bearing UI surface — touched once, breaks every screen if changed later. **Highest "do early or pay later" cost** in this phase.

- [ ] **SPEC-UI-HIGH-DPI-01**
   Scaling strategy for Retina / 4K. Depends on #31 (font scaling rules).

- [ ] **SPEC-HUD-LAYOUT-01**
   Element positioning, configurable toggles, stats-panel hotkey.

- [ ] **SPEC-CAMERA-SHAKE-01**
   Damage-scaled shake intensity, screen flash.

- [ ] **SPEC-HITSTOP-01**
   Exact frame count (cosmetic but needs locking).

---

## Phase I — Movement / input completion

- [ ] **SPEC-MOVEMENT-ACCEL-01** — instant vs eased.
- [ ] **SPEC-GAMEPAD-INPUT-01** (FUT-01). Depends on #36.
- [ ] **SPEC-INPUT-REBINDING-UI-01** (FUT-02). Depends on #37.

---

## Phase J — Future / deferrable

- [ ] **SPEC-ART-FX-01** — Bucket K effects redraw (deferred per PO; unblock when iso pivot complete).
- [ ] **SPEC-EXPORT-PLATFORMS-01** — pick first distribution platform (itch.io / Steam / direct).
- [ ] **SPEC-ANALYTICS-BACKEND-01** — pick telemetry stack.
- [ ] **SPEC-I18N-01** — internationalization strategy.
- [ ] **SPEC-AUDIO-01** — explicitly skipped per PO, but on the long horizon.
- [ ] **SPEC-MULTIPLAYER-01** — does not exist; likely intentional, confirm.

---

## Quick session-start checklist

When picking up across sessions:

1. Re-read this file.
2. Find the lowest-numbered unchecked spec in the lowest-letter phase.
3. Check that all its dependencies (the specs above it in the same phase + earlier phases) are checked.
4. If a dep is missing — spec the dep first.
5. After landing a spec: check the box, update **Last updated** stamp, commit with message that cites the roadmap entry.

---

## Out of scope for this roadmap

These are tracked elsewhere; don't duplicate here:

- **Implementation tickets** (COMBAT-01 impl, LOOT-01, ISO-01a-f, NPC-ROSTER-REWIRE-01, SPLASH-BG-REMOVE-01, all ART-* sprite-generation work) — `docs/dev-tracker.md`.
- **Code audit findings** (AUDIT-03 through AUDIT-17, except 09/10/11 which are spec-drift and appear in Phase A above) — `docs/dev-tracker.md` Audit section.
- **Per-PR work** — `docs/dev-journal.md`.
- **Future feature ideas (FUT-*)** beyond what's in Phase J — `docs/dev-tracker.md` Future section.

---

## Status snapshot at last update (2026-04-18)

- Wave 1 + Wave 2 specs complete (foundation + 12 art-pipeline specs across 4 batches).
- 10 commits ahead of `origin/main` awaiting PO push decision.
- All ART-SPEC-* specs locked, IP-clean, ready for asset redraw work.
- `asset-inventory.md` Bucket A-K define delete-before-regen scope per ticket.
- This roadmap covers what's NOT yet specced. Anything not in this file is either (a) already locked or (b) implementation work, not spec work.
