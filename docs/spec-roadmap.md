# Spec Roadmap — What's Next

**Purpose:** durable across compact/clear sessions. Records the prioritized list of specs to author, with dependency reasoning. Update the checkboxes as each spec lands.

**Last updated:** 2026-04-18 (Phase F complete — all 8 boss specs locked: Bone Overlord, Howling Pack-Father, Chitin Matriarch, Hollow Archon, Warlord of the Fifth, Screaming Flight, Iron-Gut Goblin King, Volcano Tyrant. Each lives at `docs/world/bosses/<name>.md` and follows the boss-adapted 8-section template. AI behavior trees, phase-shift triggers, FlashFx hooks, first-kill drop overrides all locked per boss).

**Next up:** Phase G — NPC dialogue & service-menu specs (unblocks NPC-ROSTER-REWIRE-01 impl). Four sub-tickets: SPEC-NPC-DIALOGUE-VOICES-01, SPEC-VILLAGE-CHIEF-DIALOGUE-01, SPEC-BLACKSMITH-MERGED-MENU-01, SPEC-GUILD-MAID-MERGED-MENU-01.

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

- [x] **SPEC-SPECIES-BAT-01** — locked 2026-04-18
   Lifted from the worked example in `species-template.md` to [docs/world/species/bat.md](world/species/bat.md). Reaction: `kite-from-range`. AI: `melee-chase` with swooping motion curve. Size: **Small band (0.70×)**, hitbox 8px, z-offset +28px (airborne — iso Y-sort above grounded sprites). Silhouette constraint: spread wings + lifted pose, readable as airborne from 8 tiles away. Paired art: ART-14 (Bat/Spider/Wolf re-art batch). Stats anchored at floors 3/28/75: HP 22/120/540, contact dmg 4/14/38, speed 90/105/120, XP 12/48/180. TTK 2-3 hits across all floors.
- [x] **SPEC-SPECIES-SKELETON-01** (zone 1) — locked 2026-04-18 in [docs/world/species/skeleton.md](world/species/skeleton.md). Reaction `close-the-gap`, AI `melee-chase` (no telegraph — shield-raise escalation reserved for boss), stats anchor 34/185/820 HP at floors 3/28/75 (~55% above Bat reference). Silhouette constraint: upright armed-humanoid with sword-and-shield asymmetry readable at 8 tiles. Scale **1.00× (Standard band)** — moves the species into player-parity from the current 0.70× placeholder; retune is `ART-SKELETON`'s problem when that art ticket lands. Drop-table row already locked in `MonsterDropTable.cs:40` (`material_sig_skeleton` 10%, `Bone` thematic); spec documents it. Paired art ticket `ART-SKELETON` stubbed, not yet authored.
- [x] **SPEC-SPECIES-WOLF-01** (zone 2) — locked 2026-04-18 in [docs/world/species/wolf.md](world/species/wolf.md). Reaction `pack-management`, AI `pack` (coordinated flank-split on aggro; lone survivor falls back to `melee-chase`), scale **1.25× (Large band)**, Tier 2 stats anchor 40/215/960 HP at floors 3/28/75 (~1.8× Bat per-unit; pack-of-3-5 is the real threat). Signature `material_sig_wolf` at 10% / Hide thematic — matches `MonsterDropTable.cs:44`. Exempt pixels: eye glow + fang highlights (pack-predator cues). Silhouette constraint: a 3–5-Wolf pack at 8 tiles must read as a pack with visible spacing, not a blob. Defines Howling Pack-Father base (body plan + `pack` AI + Large-band scale upgraded to Boss band in Phase F). Paired art: ART-14 (Bat/Spider/Wolf re-art batch, still in flight).
- [x] **SPEC-SPECIES-SPIDER-01** (zone 3) — locked 2026-04-18 in [docs/world/species/spider.md](world/species/spider.md). Reaction `burst-down-fast`, AI `ambush` (120 px proximity aggro, 200 ms body-rise telegraph, ~800 ms burst lunge, 360 px leash — revealed Spider falls back to `melee-chase` if not killed in ~3s). Tier 2 stats anchor 28/180/780 HP at floors 3/28/75 (between Bat T1 and Orc T3; glass-cannon with split idle/lunge speed 70|160 → 100|210 px/s). Scale **0.75× (Small band)** to leave the "bigger and badder" silhouette space for the Matriarch. Signature `material_sig_spider` at 8% / Hide thematic — matches `MonsterDropTable.cs:47`. Exempt pixels: 4–8-pixel eye cluster on cephalothorax (drives "corner has eyes" recognition on repeat visits). Silhouette constraint: 6-of-8 legs fanned, low profile (<40% canvas height), wider-than-tall ground-hugging footprint, arachnid-readable at 8 tiles. Defines Chitin Matriarch base (body plan + scale floor 0.75× → Boss band in Phase F + Hide thematic). Paired art: ART-14 (Bat/Spider/Wolf re-art batch, still in flight).
- [x] **SPEC-SPECIES-DARKMAGE-01** (zone 4) — locked 2026-04-18 in [docs/world/species/darkmage.md](world/species/darkmage.md). Reaction `burst-down-fast`, AI `caster` (450 ms basic-bolt wind-up + 900 ms AoE slow-field wind-up at <40% HP; `ranged-kite` 1-tile retreat when player within 2 tiles). Tier 3 stats anchor 18/95/420 HP at floors 3/28/75 (lower HP than Tier-1 Bat; contact damage 9/32/95 ≈ 2× Tier-1/2 ceiling — first species whose threat is *range + damage*, not mobility). Scale **1.00× (Standard band)**, humanoid, ground-bound. Signature `material_sig_darkmage` at 7% / Bone thematic — matches `MonsterDropTable.cs:49` (lower rate because higher per-drop value at Tier 3). Exempt pixels: purple eye glow + staff-tip glow (pulses during 450 ms cast) + hand-magic aura (visible during wind-up only). Silhouette constraint: upright thin-tall stance with hooded/skullcap head extending above head-line and raised staff/hand, must read as caster across a mixed group at 8 tiles. Defines Hollow Archon base (body plan + `caster` AI + staff-tip glow family; boss's unique phase-shifts deferred to SPEC-BOSS-HOLLOW-ARCHON-01 in Phase F). Paired art: **ART-DARKMAGE** (proposed placeholder — no existing art ticket covers zone-4 species redraw).
- [x] **SPEC-SPECIES-ORC-01** (zone 5) — locked 2026-04-18 in [docs/world/species/orc.md](world/species/orc.md). Reaction `cautious-approach`, AI `melee-chase` + **600 ms heavy-swing telegraph** (1.5× contact on the tell), Tier 3 stats anchor 80/520/2,600 HP at floors 3/28/75 (highest species HP per floor in current roster; balanced by slowest speed 55/62/70 px/s). Scale **1.40× (Large band)**, hitbox 17 px. Signature `material_sig_orc` at 10% / **Ore thematic** — matches `MonsterDropTable.cs:46`. Exempt pixels: tusk highlight + weapon glint (preserve "armed brute" identity at every level gap). Silhouette constraint: top-heavy + wider than player bounding box + asymmetric hands (swinging arm visibly larger) — readable at 8 tiles. Defines body plan for **two** future bosses: Warlord of the Fifth (zone 5) and Volcano Tyrant (zone 8, deep-zone orc-form per Phase F). Paired art: ART-ORC (proposed placeholder — no existing art ticket).
- [x] **SPEC-SPECIES-GOBLIN-01** — locked 2026-04-18 in [docs/world/species/goblin.md](world/species/goblin.md). Zones 2 (primary) + 7 (boss-flavor). Reaction `pack-management`, AI `pack` (spread-to-flank when outnumbering, cluster-toward-sibling when outnumbered). Tier 1 stats anchor 20/112/505 HP at floors 3/28/75 — explicitly tuned against Bat (same tier / same 1–2-hit TTK) to invite the comparison: Goblin = slightly lower HP/damage, slower, 3–5 per cluster, ground-based flanker; Bat = slightly higher HP/damage, faster, singles/pairs, airborne harasser. Scale **0.80× (Small band)**, hitbox 10 px, ground (z-offset 0). Signature `material_sig_goblin` at 10% / Ore thematic — matches `MonsterDropTable.cs:43`. Exempt pixels: skin (goblin-green must survive level-relative tint) + eye highlights; scrap-metal gear is NOT exempt (level-tint still lands on majority of silhouette). Silhouette constraint: cluster of 5 goblins must read as 5 distinct bodies at 8 tiles, never as one blob — keep silhouettes narrow, arms close to body. Defines Iron-Gut Goblin King base (zone 7 boss inherits body plan + stat baseline; boss-specific rules deferred to SPEC-BOSS-IRON-GUT-GOBLIN-KING-01 in Phase F). Paired art: **ART-GOBLIN** (proposed placeholder — no existing art ticket covers pack-management silhouette-constraint redraw).

(Bat at zone 6 boss "Screaming Flight" is a swarm-fused variant — may need its own sub-spec.)

---

## Phase F — Per-boss instance specs (gated on Phase E)

Each fully expands what's currently a "skeleton fill" in `boss-art.md` §7-13. Each boss spec depends on its species spec from Phase E.

- [x] **SPEC-BOSS-BONE-OVERLORD-01** (zone 1, floor 10) — locked 2026-04-18 in [docs/world/bosses/bone-overlord.md](world/bosses/bone-overlord.md). 2-phase burst-down-fast. Scale 1.8×, Phase-2 900ms ground-slam AOE at 50% HP. FORGE-01 Tier 1 unique. Save-flag `floor10_boss_skeleton`.
- [x] **SPEC-BOSS-HOWLING-PACK-FATHER-01** (zone 2, floor 20) — locked 2026-04-18 in [docs/world/bosses/howling-pack-father.md](world/bosses/howling-pack-father.md). 2-phase burst-down-fast. Scale 1.8×, Phase-2 summons 2× 1-HP phantom wolves at 50% HP. FORGE-01 Tier 2 unique. Save-flag `floor20_boss_wolf`.
- [x] **SPEC-BOSS-CHITIN-MATRIARCH-01** (zone 3, floor 30) — locked 2026-04-18 in [docs/world/bosses/chitin-matriarch.md](world/bosses/chitin-matriarch.md). 2-phase kite-from-range. Scale 1.8×, Phase-2 summons 3× spiderlings + ground-web 50% slow AOE at 50% HP. FORGE-01 Tier 3 unique. Save-flag `floor30_boss_spider`.
- [x] **SPEC-BOSS-HOLLOW-ARCHON-01** (zone 4, floor 40) — locked 2026-04-18 in [docs/world/bosses/hollow-archon.md](world/bosses/hollow-archon.md). 2-phase caster kite-from-range, airborne (z-offset +24px). Phase-2 adds 1200ms ground-wave AOE at 50% HP. FORGE-01 Tier 4 unique. Save-flag `floor40_boss_darkmage`.
- [x] **SPEC-BOSS-WARLORD-FIFTH-01** (zone 5, floor 50) — locked 2026-04-18 in [docs/world/bosses/warlord-fifth.md](world/bosses/warlord-fifth.md). 2-phase kite-from-range with thrown axes. Scale 2.0×, Phase-2 halves throw cooldown + adds 700ms charge at 50% HP. FORGE-01 Tier 5 unique. Save-flag `floor50_boss_orc`.
- [x] **SPEC-BOSS-SCREAMING-FLIGHT-01** (zone 6, floor 60) — locked 2026-04-18 in [docs/world/bosses/screaming-flight.md](world/bosses/screaming-flight.md). **3-phase** close-the-gap — Phase 1 airborne `ranged-kite`-inverted (z+40), Phase 2 spawns 1-HP bat-fragment adds, Phase 3 ground-collapse (z→0) switches to `melee-chase`. Decision: no separate swarm-fused species sub-spec needed; fusion is boss-only using base Bat body plan. FORGE-01 Tier 5 unique. Save-flag `floor60_boss_bat`.
- [x] **SPEC-BOSS-IRON-GUT-GOBLIN-KING-01** (zone 7, floor 70) — locked 2026-04-18 in [docs/world/bosses/iron-gut-goblin-king.md](world/bosses/iron-gut-goblin-king.md). **3-phase** close-the-gap. Phase-2 iron-slag DOT AOE at 50% HP, Phase-3 turret-mode (stationary projectile firer) at 25% HP. First-kill bundle uniquely includes Zone 1-3 species signature materials (the King has eaten all of them — fiction into mechanic). FORGE-01 Tier 5 unique. Save-flag `floor70_boss_goblin`.
- [x] **SPEC-BOSS-VOLCANO-TYRANT-01** (zone 8, floor 80, deepest in starter roster) — locked 2026-04-18 in [docs/world/bosses/volcano-tyrant.md](world/bosses/volcano-tyrant.md). **3-phase** close-the-gap. Orc species base (shares body plan with zone-5 Warlord but fully differentiated silhouette/aura/mechanics). Scale 2.2× (largest boss), Phase-2 three telegraphed fissure-lines → lava tiles at 50% HP, Phase-3 passive heat-aura DOT within 2 tiles at 25% HP (signature endgame dance). FORGE-01 Tier 5 unique. Save-flag `floor80_boss_orc` (no collision with Warlord's `floor50_boss_orc`).

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
