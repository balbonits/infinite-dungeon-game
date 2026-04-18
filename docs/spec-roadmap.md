# Spec Roadmap — What's Next

**Purpose:** durable across compact/clear sessions. Records the prioritized list of specs to author, with dependency reasoning. Update the checkboxes as each spec lands.

**Last updated:** 2026-04-18 (SPEC-MAGIC-COMBAT-FORMULA-01 locked — Option A, INT-only, no density coupling on Mage spell damage. Added canonical "what density does and does not modify" table to magic.md §Density Formula. SPEC-COMBAT-03 stays gated until COMBAT-01/02 impl lands).

**Next up:** Phase E — per-species instance specs, fan-out starting with SPEC-SPECIES-BAT-01 (template exemplar) + SPEC-SPECIES-SKELETON-01 (zone 1, defines Bone Overlord base). All of Phase E depends on Phase B density but that's now locked.

---

## How to read this

- Phases run in order (A → J). Within a phase, specs are mostly serial because each defines a number the next depends on.
- Across phases, some can parallelize once their inputs are locked (E and F can fan out within themselves; G/H/I are mostly independent of each other once their input phases land).
- Each spec lists what it **defines for the next** — the load-bearing handoff to the next ticket. If you do them out of order, that handoff breaks and forces re-edits.

---

## Phase A — Reconciliation (no new design, unify what's drifted)

Zero new design; every downstream spec inherits cleaner numbers. Highest "value per token" specs in the whole roadmap.

- [x] **SPEC-RECONCILE-BRACKETS-01** (AUDIT-09)
   `item-generation.md` says 5 brackets, `depth-gear-tiers.md` says 7, code matches 7. Pick canonical, edit the loser.
   *Defines for next*: which bracket count appears in every loot/affix/quality discussion.
   *Resolution*: Option A — deleted stale §Quality Distribution subsection in `item-generation.md`; replaced with pointer to `depth-gear-tiers.md` §Drop Rates. The 5-bracket system remains canonical for catalog tier and material tier only; 7-bracket system is canonical for quality rolls.

- [x] **SPEC-AFFIX-TIER-LADDER-01** (AUDIT-10) — locked 2026-04-18
   `AffixDatabase.GetMaxTier` claims tiers 5/6 exist; registry only has 1-4. **Decision: Option B — add T5/T6 affixes** covering 8 build-defining families (keen, vicious, sturdy, warding, striking, ruin, bear, swiftness). Full value + cost tables in [items.md §T5 + T6 Affix Ladder](inventory/items.md#t5--t6-affix-ladder-spec-affix-tier-ladder-01). Impl ticket AUDIT-10 can register without further design input.
   *Defines*: affix space size. Depends on #1.

- [x] **SPEC-CRAFTING-QUALITY-LADDER-01** (AUDIT-11)
   `Crafting.RecycleItem` quality-bonus switch missing Masterwork/Mythic/Transcendent.
   *Defines*: recycle gold values designers balance against. Depends on #1 + #2.
   *Resolution*: Option B — geometric ladder (doubles per tier). Normal 0, Superior ×0.25, Elite ×0.5, Masterwork ×1.0, Mythic ×2.0, Transcendent ×4.0 applied to `baseGold = 5 + item.ItemLevel * 2`. Rationale: matches the geometric shape of the craft-cost multiplier (1.0/1.2/1.5/2.0/3.0/5.0) and the "infinite descent, infinite incentive" intent. Canonical formula now lives in `docs/systems/depth-gear-tiers.md` §Interaction with Other Systems → Recycling; `docs/flows/blacksmith.md` Recycle Flow preview updated to match. Impl ticket can copy the switch arms directly without further design. Follow-up (separate spec if raised): whether recycle should also yield materials — currently `RecycleItem` returns gold only.

---

## Phase B — Magic system foundation (biggest design gap)

`magic.md` has 5 open questions; they cascade through 4+ other docs.

- [x] **SPEC-MAGICULE-DENSITY-01** — locked 2026-04-18
   Piecewise formula locked in [magic.md §Density Formula](systems/magic.md#density-formula-spec-magicule-density-01). Linear `density = F/100` for floors 1–100, then exponential `density = 1.0 · k^(F-100)` with `k = 1.032` above. Danger onset at floor 100 (density 1.0); effective ceiling at floors 180–200 (density ≈12–23); beyond stable reach at floor 250 (density ≈110). Doubles every ~22 floors past threshold.
   *Defines for next*: feeds mana drain, `magicule-attunement.md`, `dungeon-pacts.md`, `dungeon-intelligence.md`. **The load-bearing magic number — every Phase B/C/D spec inherits this.**
   *Cross-refs added*: `world/dungeon.md` §Magicule Density Gradient now points at the canonical formula.

- [x] **SPEC-INNATE-MANA-COST-01** — locked 2026-04-18
   Per-Innate level-1 drain locked asymmetric by design role (all on Mage's 200-mana base pool, no regen, no density modifier): **Haste 13.33 mana/s ≈15s uptime** (burst tool), **Sense 6.67 mana/s ≈30s uptime** (exploration tool), **Fortify 3.33 mana/s ≈60s uptime** (held stance). Shared per-level curve: `drain(L) = max(base_drain * 0.96^L, base_drain * 0.25)`. 4% compounded reduction per level; floors at 25% of base at level ~35 (Innates become very cheap at high levels without going to zero). Drain is explicitly **NOT** modified by floor density — Innate cost is the brain's processing cost, not an environmental cost. Full spec + per-level drain/uptime tables in [magic.md §Drain Scaling Per Level (SPEC-INNATE-MANA-COST-01)](systems/magic.md#drain-scaling-per-level-spec-innate-mana-cost-01). Removes Open Question 1 from magic.md.
   *Defines for next*: base cost & scaling that SPEC-INNATE-STACKING-01 must reason over when deciding concurrency rules; also feeds SPEC-MAGIC-COMBAT-FORMULA-01 for mage mana-economy balance. Depends on #4.

- [x] **SPEC-INNATE-STACKING-01** — locked 2026-04-18
   **Decision: Option A — Free stacking.** Haste, Sense, and Fortify may be activated in any combination with no hard concurrency cap. Combined mana drain is the sole governor: at level 1, all-three-active = **23.33 mana/sec** (13.33 + 6.67 + 3.33), which burns a Mage's 200-mana pool in ~8.6 seconds with no regen. Rewards combo plays (Fortify+Haste durable sprinter, Sense+Haste scout, Sense+Fortify cautious explorer) while making blanket-stacking self-punishing via uptime. Mid-game Mage (level-15 Innates, 400-mana pool, 12 mana/s regen) can hold all three near-indefinitely (~2.0 mana/s net drain), which is the build-identity payoff for Attunement/INT investment. Warriors (60 pool) and Rangers (100 pool) naturally pick one or two. Full rule + stacking-combinations table + worked example in [magic.md §Stacking Rule (SPEC-INNATE-STACKING-01)](systems/magic.md#stacking-rule-spec-innate-stacking-01). Removes Open Question 5 from magic.md. **Phase B complete.**
   *Defines for next*: unblocks Phase C — SPEC-SKILL-POINTS-RATE-01 (mage progression rate must match the mana-economy shape now that stacking + drain costs are both locked). Depends on #5.

---

## Phase C — Skills/Abilities completion (kills SKILLS_AND_ABILITIES_SYSTEMS.md TBDs)

- [x] **SPEC-SKILL-POINTS-RATE-01** — locked 2026-04-18 (reconciliation)
   Canonical live spec: [point-economy.md](systems/point-economy.md) (status: LOCKED). SP: 2/level, +1 at milestones. AP: 3/level, +5 at milestones, plus combat-milestone AP and use-based per-category AP (~60/25/15 split). Separate-pools design already resolved the archive's "needs adjustment for separate pools" TBD — Phase C reconciliation pointed the ARCHIVED SKILLS_AND_ABILITIES_SYSTEMS.md §Point Systems at the live spec. No new design work.
   *Defines*: progression speed. Depends on Phase B (mage rate must match mana economy — verified compatible with SPEC-INNATE-MANA-COST-01's drain numbers).

- [x] **SPEC-INNATE-SYNERGIES-01** — locked 2026-04-18 (reconciliation)
   Canonical live spec: [synergy-bonuses.md §Innate Synergies](systems/synergy-bonuses.md#innate-synergies-affect-all-abilities) (status: LOCKED). All four Innates (Haste/Sense/Fortify/Armor) have full per-level bonus ladders (Lv. 5/10/25/50/100); Innate synergies affect ALL Abilities across all categories, not just their own children. Archive's "details TBD" line pointed at the live spec. No new design work.
   *Defines*: build-complexity ceiling. Depends on #6 + #7.

- [x] **SPEC-MASTERY-THRESHOLD-FX-01** — locked 2026-04-18 (reconciliation)
   Canonical live spec: [ability-affinity.md](systems/ability-affinity.md) (status: LOCKED). Four use-based affinity tiers (Familiar/Practiced/Expert/Mastered at 100/500/1,000/5,000 uses) with cumulative cosmetic effects; passive and toggle tracking rules defined; Armor Innate excluded. Archive's "ability-affinity.md TBD" pointed at the live spec. No new design work.
   *Defines*: feedback-loop visuals. Depends on #8.

---

## Phase D — Combat tuning derivatives

- [x] **SPEC-MAGIC-COMBAT-FORMULA-01** — locked 2026-04-18
   **Decision: Option A — INT only, no density coupling.** Mage spell damage continues to scale via `effective_int * 1.2%` per [stats.md §INT](systems/stats.md#int--intelligence-mage-primary). Floor density from SPEC-MAGICULE-DENSITY-01 is explicitly excluded from the damage formula. Rationale: density manifests through the world (enemy stats, environmental pressure, dungeon AI) not through player combat math; coupling Mage damage to density would have scaled Mages past Warriors/Rangers since only Mages consciously channel environmental magicules, creating class imbalance at depth. Added a "what density does and does not modify" table to [magic.md §Density Formula](systems/magic.md#density-formula-spec-magicule-density-01) as canonical cross-reference. Stats.md spell-damage bullet now explicitly states "Spell damage is NOT modified by magicule density" with SPEC pointer.
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
