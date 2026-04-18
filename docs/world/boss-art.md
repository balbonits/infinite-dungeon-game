# Boss Visual Identity & Roster (SPEC-BOSS-ART-01)

## Summary

The game-facing visual identity + authoring template for **zone-capstone bosses** — the single deepest denizen that guards the last floor (10, 20, 30, …) of each 10-floor zone. Defines boss fiction, scale band, silhouette rules, phase-shift convention, and the starter roster of 8 bosses (one per zone for zones 1–8). Paired with [ART-SPEC-BOSS-01 — boss sprite pipeline](../assets/boss-pipeline.md), which owns the generation-side half (PixelLab prompts, canvas, palette hex codes). Co-lock criterion: neither half reaches "Ready-for-impl" alone.

## Current State

Bosses exist in drop-table rules (see [monster-drops.md § Boss First-Kill Drops](../systems/monster-drops.md#boss-first-kill-drops)) and in art scope ([dev-tracker ART-10](../dev-tracker.md) — "1 boss per 10-floor zone ≈ 5–10 boss sprites") but have **never had distinct design or art authored**. There is no boss species yet in `EnemySpecies`, no boss art directory, no per-boss stat spec. The current floor-10 miniboss drop bundle fires against whatever the zone's dominant species is, using that species's sprite scaled up — which violates the "bigger and badder" silhouette expectation the drop bundle promises.

This spec fixes that by treating bosses as **species with extra rules** — reusing the 8-section rigor of [SPEC-SPECIES-01](species-template.md) but adapted for boss-unique concerns (scale band, phase shifts, unique-drop pairing, aura FX, zone-capstone placement).

Zone layout (from [SYS-10](../dev-tracker.md) / `Constants.Zones.GetZoneSpecies`):

| Zone | Floors | Species roster | Boss slot |
|---|---|---|---|
| 1 | 1–10 | Skeleton, Bat | Floor 10 |
| 2 | 11–20 | Goblin, Wolf | Floor 20 |
| 3 | 21–30 | Orc, Spider | Floor 30 |
| 4 | 31–40 | Dark Mage mix | Floor 40 |
| 5+ | 41+ | All species | Floor 50, 60, 70, 80… |

Related specs this one pulls from:

- [docs/world/species-template.md](species-template.md) (SPEC-SPECIES-01) — the 8-section authoring rigor adapted here.
- [docs/systems/monster-drops.md](../systems/monster-drops.md) — boss first-kill bundle + bonus-container rule.
- [docs/systems/blacksmith-forge-rng.md](../systems/blacksmith-forge-rng.md) (FORGE-01) — unique-item registry + per-tier pool structure.
- [docs/world/monsters.md](monsters.md) — magicule-warping fiction, tier baseline.
- [docs/systems/color-system.md](../systems/color-system.md) — level-relative tint.
- [docs/systems/iso-rendering.md](../systems/iso-rendering.md) — sprite anchor / Y-sort.
- [docs/assets/prompt-templates.md](../assets/prompt-templates.md) § CHAR-MON-ISO (canvas size rule for bosses — 160×160 per §1a).
- [docs/assets/boss-pipeline.md](../assets/boss-pipeline.md) — paired art half (ART-SPEC-BOSS-01).

## Design

### 1. Boss concept — what a boss IS in our world

**Fiction beat.** A boss is the deepest denizen of its zone — an ordinary creature of the zone's dominant species that has *not died, not moved, and not left its floor* since the dungeon first stirred. Years (or decades, or longer) of sitting at the very bottom of a 10-floor strata, soaking every exhaled magicule that settled down through the levels above, have warped it past any normal tier. Where a standard skeleton flickers with a thin purple sheen at its joints, the Bone Overlord's entire rib cage radiates the stuff like a furnace. Where a wolf pack leader is bigger than its kin, the Howling Pack-Father is the *size of the pack*. Bosses are what a zone's species *becomes* if left to concentrate unchecked. Fresh or returning players fight a first-kill event that rewards a unique relic — a crafted artifact the Blacksmith can forge from a base equivalent to the boss's drop tier, honoring the run in which it was slain.

**Meta-role.** End-of-zone capstone. Each boss appears **only** on its zone's last floor (floor 10, 20, 30, 40, …). The player cannot skip a zone without killing its boss — the stairs down from the boss floor are gated by the boss's death on first clear (Per-save-slot; once dead, stairs are open on subsequent runs even before re-killing). Bosses are the rhythm break that divides the infinite-depth grind into discrete "chapters."

**Player emotional reaction (one per boss).** Each boss picks one of three boss-specific reactions, which extends the five-canonical species vocabulary in SPEC-SPECIES-01:

| Boss reaction | When used | What the fight feels like |
|---|---|---|
| `burst-down-fast` | Zone-1 bosses — player is still shaky on the system | Short fight (20–40s). Boss telegraphs one big attack; player learns to read it; 2–3 phase flips; down it goes. The "first real boss" feel. |
| `kite-from-range` | Mid-zone bosses (zones 3–5) | Medium fight (45–75s). Boss has ranged pressure + mobility. Player must cycle cover and position, not stand-and-trade. |
| `close-the-gap` | Deep-zone bosses (zones 6–8+) | Long attrition fight (90–180s). Boss is a wall — high HP, slow but punishing hits, multiple phase shifts. Player must commit and survive, not dodge-cheese. |

This reaction drives the stat snapshot tier (§3 table below), the phase-shift count, and the silhouette pose choice.

---

### 2. Boss authoring template

Every boss spec fills the 8-section template from [species-template.md](species-template.md) with the boss-specific adaptations below. Sections kept identical to SPEC-SPECIES-01 are marked "unchanged."

#### 1. Identity (adapted)

- **Fiction beat** (one sentence): what this particular creature *was* before decades of magicule saturation, and what it is *now*. Template: "A {zone's dominant species member} that has {sat / ruled / festered} on the {ordinal} floor long enough to {what the corruption produced}."
- **Zone + floor ruled** (one line): e.g. "Zone 1 capstone — floor 10."
- **What corrupts this floor** (one sentence): the ambient phenomenon that fed the boss. E.g. "A crack in the floor bleeds raw bone-dust up from a mass grave below."
- **Primary reaction**: one of `burst-down-fast` / `kite-from-range` / `close-the-gap` per §1 above.

#### 2. Stats (adapted — scaled from species baseline)

Bosses are authored as a **multiplier on top of the standard-tier species stats at their floor**, not independent numbers. This keeps boss power tied to the §2 species stat line and prevents drift when species stats rebalance.

**Boss multipliers (from standard-tier species numbers at the boss's floor):**

| Stat | Multiplier | Rationale |
|---|---|---|
| Base HP | **× 5–8** | The fight takes 20–180 seconds per §1; multiplier varies by reaction band. `burst-down-fast` = 5×, `kite-from-range` = 6×, `close-the-gap` = 8×. |
| Base Contact Damage | **× 2–3** | Hits hurt, but bosses aren't one-shots. `burst-down-fast` = 2×, mid = 2.5×, `close-the-gap` = 3×. |
| Base Move Speed | **× 0.8** | Bosses are **slower** than their species kin. Heft reads through motion — a boss that zips around feels like an elite, not a boss. Exception: any boss whose silhouette explicitly says "agile" (e.g. a giant spider) may hold speed at 1.0×; document the exception in the spec. |
| Base XP Yield | **× 10** | A boss kill is a memorable beat; the XP bump should make the level-up ding land inside or just after the kill. |

**Three sample boss stat snapshots.** Showing the three reaction bands at their intended floors, anchored to the standard-tier species numbers the bosses draw from:

| Boss | Floor | Base Species | Species HP (std) | Boss HP | Species Dmg | Boss Dmg | Species Speed | Boss Speed | Species XP | Boss XP |
|---|---|---|---|---|---|---|---|---|---|---|
| Zone 1 boss (burst-down-fast) | 10 | Skeleton | ~60 | **300** (× 5) | ~8 | **16** (× 2) | 60 | 48 (× 0.8) | ~35 | **350** (× 10) |
| Zone 3 boss (kite-from-range) | 30 | Orc | ~150 | **900** (× 6) | ~18 | **45** (× 2.5) | 55 | 44 (× 0.8) | ~60 | **600** (× 10) |
| Zone 8 boss (close-the-gap) | 80 | (deep-zone mix) | ~600 | **4800** (× 8) | ~45 | **135** (× 3) | 55 | 44 (× 0.8) | ~200 | **2000** (× 10) |

The species-number column uses the SPEC-SPECIES-01 per-species stat line interpolated to the boss's floor. If a boss draws from a species whose spec is not yet filled, document the interpolated species number inline in the boss spec so it's auditable.

**Target TTK** (class-appropriate level-matched melee): 20–40 s (burst), 45–75 s (kite), 90–180 s (close). This is the design target the `phase_shift` timing below anchors to.

#### 3. AI Pattern (adapted — adds `phase_shift`)

Bosses still pick **one** of the five canonical SPEC-SPECIES-01 AI patterns (`melee-chase` / `ranged-kite` / `ambush` / `pack` / `caster`) as their **primary** behavior. But bosses additionally **change behavior at HP thresholds** — this is the phase-shift convention:

**Phase-shift convention (locked):**

- **Phase 1 — 100% → 50% HP.** Boss uses its primary AI pattern. No modifications.
- **Phase 2 — 50% → 25% HP.** Boss adds or modifies one behavior. Visual cue required: particle aura shifts color (e.g. blue → red), or boss emits a one-shot `FlashFx.Flash()` white pulse at threshold. Examples: `melee-chase` boss gains a charge attack; `ranged-kite` boss adds a second projectile angle; `caster` reduces telegraph window by ~30%.
- **Phase 3 (optional — close-the-gap bosses only) — 25% → 0% HP.** Boss adds a second modification or swaps to a secondary pattern. Visual cue required: larger aura shift + screen-edge vignette pulse per [ui/screen-effects](../ui/screen-effects.md) convention.

**Required fields per boss spec:**

- Primary AI pattern (one of the five).
- Phase 2 change (one sentence, behavior + visual cue).
- Phase 3 change (one sentence if the boss uses it; "N/A — 2-phase boss" otherwise).
- Telegraph window (ms) for any non-contact attack; rules from SPEC-SPECIES-01 §3 apply per phase (telegraph may shorten between phases, but must never drop below 200 ms — that's the player-reaction floor).
- Aggro range: `chase-always` is the default; bosses may override with a specific pixel radius if arena-bound (e.g. "does not pursue past floor-10 arena tile set").
- Leash range: bosses use `never-leash` by default; may override if the boss is arena-bound (state the arena radius in px).

**No C#.** Reference `scripts/logic/` behavior as prior art but keep the spec engine-agnostic.

#### 4. Drop-Table Hook (adapted — adds guaranteed unique)

Bosses honor the [monster-drops.md § Boss First-Kill Drops](../systems/monster-drops.md#boss-first-kill-drops) bundle rules AND layer on a **guaranteed unique item** from the FORGE-01 pool on first kill per save slot.

| Field | Value |
|---|---|
| First-kill material bundle | Per the floor-10 / 25 / 50 / 101 / 150+ rows in [monster-drops.md](../systems/monster-drops.md#boss-first-kill-drops). |
| First-kill bonus container | Per the same table (Crate at floor 10/25, Chest at 50/101/150). |
| **First-kill guaranteed unique** | One unique, rolled uniformly from the FORGE-01 tier pool matching the boss's zone tier (see §5 pairing table). Drops **pre-forged** — the player does not spend materials; the boss "drops" a ready-made unique. |
| Repeat-kill drops | Standard species drop table of the boss's base species (no bundle, no unique). Normal material channels apply. |
| Standard species drops (every kill) | The boss also rolls its base species's standard generic + signature material channels per [monster-drops.md § Per-Species Drop Table Structure](../systems/monster-drops.md#per-species-drop-table-structure). |

**Save-flag:** reuses `SaveData.BossFirstKillsConsumed` from [monster-drops.md § Implementation Notes](../systems/monster-drops.md#implementation-notes). Key format extended to `"floor{N}_boss_{species}"` (e.g. `floor10_boss_skeleton`).

#### 5. Silhouette Readability Constraint (adapted — "bigger AND badder")

Bosses MUST pass **two** silhouette tests, where standard species only pass one:

1. **Zone-species distinction.** The boss silhouette must read as **distinct** from every standard-tier member of its zone's species roster at 8 tiles away. A Bone Overlord must not read as "big skeleton" — it must read as "a *thing* that was once a skeleton."
2. **Boss-vs-boss distinction.** Bosses must also be distinguishable from **every other boss** in the game at thumbnail scale. Two bosses in two different zones cannot share the same silhouette even if they come from different species. This prevents the "all tall purple humanoids" problem as the roster grows.

**"Bigger and badder" rule.** First-sight silhouette must read as a boss — not an elite, not a big mob. Required cues (boss must use **at least two**):

- **Scale** — Boss band (1.7–2.5×) per SPEC-SPECIES-01 §6. This alone does not qualify; a simple "big skeleton" is an elite, not a boss.
- **Asymmetry** — e.g. one oversized limb, a broken horn, a lopsided gait. Standard species are broadly symmetric; bosses visibly are not.
- **Additional body part** — extra limbs, secondary head, floating fragments, tail. Signals "mutated past its species."
- **Aura / particle FX** — persistent emission (see §7 color-coding).
- **Crown / regalia / trophy** — signals "king of its floor." Only use for one boss per zone tier to keep distinct.

The boss spec must state the **one-sentence constraint + rationale + the two-or-more cues from the list above** it uses.

#### 6. Size / Scale Rule (adapted — Boss band only)

All bosses sit in the Boss band (1.7–2.5×). Locked rule:

| Boss tier | Scale multiplier |
|---|---|
| Zone 1–4 bosses | **1.8×** (default) |
| Zone 5–7 bosses | **2.0×** |
| Zone 8 boss (deepest in starter roster) | **2.2×** |
| **Final boss of deepest explored zone** (future content, placeholder) | **2.5×** |

Hitbox radius: default = `round(12 * scale_multiplier)` per SPEC-SPECIES-01 convention (so a 1.8× boss has a 22px hitbox, a 2.2× has 26px). A boss may override with a larger hitbox if its silhouette genuinely occupies more footprint (state the radius + reason in the spec).

Z-offset: `0` for ground bosses; airborne bosses state offset per SPEC-SPECIES-01 §6 (e.g. a zone-4 floating dark-mage archon might use +32).

Canvas size: 160×160 per [prompt-templates.md § CHAR-MON-ISO §1a](../assets/prompt-templates.md) (locked in ART-SPEC-01 v2 for boss canvases — standard mobs are 128×128).

#### 7. Color-Coding Contract (adapted — heightened palette + aura)

Bosses use a **heightened palette** relative to their base species:

- **Shadow deepening**: darkest shade on the boss sprite must be 1–2 steps darker than the species's standard shadow.
- **Accent brightening**: the species's signature accent (e.g. skeleton's joint-glow purple, goblin's ear-tip red) is 1–2 steps brighter on the boss, and must appear on **at least twice as many pixel clusters** as the standard species.
- **Persistent aura / particle FX**: every boss has an emission visible from aggro range, tied to its fiction beat. Must be compatible with `FlashFx` types ([combat.md § Damage Feedback](../systems/combat.md)) — i.e. the aura modulates via the same sprite-tint path as `FlashFx.Flash()` so the on-hit white flash overrides it cleanly without visual conflict. Examples: a skeleton boss emits slow-rising bone-dust particles; a wolf boss has a red breath-puff emission.

**Exempt-pixel rule carries over from SPEC-SPECIES-01 §7:** the boss's signature-color pixels stay unmodulated by the level-relative tint system. All bosses must state their exempt pixel list (aura source pixels, eye glow, regalia accents, etc.).

**Phase-shift color cue (if the boss uses phase shifts §3):** the aura color changes at 50% HP (and again at 25% HP for 3-phase bosses). The spec must state each phase's aura color in prose (hex codes are art-lead's half, do not author them here).

#### 8. Art-Spec Pairing

One line identifying the paired `ART-SPEC-BOSS-01` (`docs/assets/boss-pipeline.md`) + lock-state stamp:

```
Paired art ticket: ART-SPEC-BOSS-01 (docs/assets/boss-pipeline.md)
Pairing status: [ ] art spec locked  [ ] art assets delivered
```

A boss spec reaches "Ready-for-impl" only when both checkboxes are ticked, all 8 sections filled, and Open Questions is empty.

---

### Section-by-Section Acceptance Matrix (boss adaptation)

| # | Section | Lock condition |
|---|---|---|
| 1 | Identity | Fiction beat, zone+floor, floor-corruption, primary reaction (one of three) all filled |
| 2 | Stats | Boss multipliers stated + sample-floor numbers computed; reaction ↔ multiplier band consistent |
| 3 | AI Pattern | Primary pattern (one of five), phase-2 change, phase-3 change (or "N/A — 2-phase"), telegraph / aggro / leash fields |
| 4 | Drop-Table Hook | First-kill unique pool tier cited; save-flag key format stated; repeat-kill behavior stated |
| 5 | Silhouette Readability | One-sentence constraint + rationale + two-or-more "bigger and badder" cues listed |
| 6 | Size / Scale | Multiplier (matches tier table), hitbox radius, z-offset, canvas 160×160 |
| 7 | Color-Coding | Heightened-palette prose; aura source + emission behavior; exempt pixels; phase color cues if phased |
| 8 | Art-Spec Pairing | Paired ticket cited, lock-stamp present |

---

### 3. Starter boss roster — 8 bosses, one per zone for zones 1–8

All names are **title-only** per the NPC-naming convention (homage to *Maoyuu Maou Yuusha* — no personal names). The zone-1 boss is fully worked as the template example; zones 2–8 fill every required field in skeleton form with no TBDs.

**Roster at a glance:**

| Zone | Floor | Boss title | Base species | Reaction | Scale |
|---|---|---|---|---|---|
| 1 | 10 | **Bone Overlord** | Skeleton | burst-down-fast | 1.8× |
| 2 | 20 | **Howling Pack-Father** | Wolf (leads goblin pack) | burst-down-fast | 1.8× |
| 3 | 30 | **Chitin Matriarch** | Spider | kite-from-range | 1.8× |
| 4 | 40 | **Hollow Archon** | Dark Mage | kite-from-range | 1.8× |
| 5 | 50 | **Warlord of the Fifth** | Orc | kite-from-range | 2.0× |
| 6 | 60 | **The Screaming Flight** | Bat (swarm-fused) | close-the-gap | 2.0× |
| 7 | 70 | **Iron-Gut Goblin King** | Goblin | close-the-gap | 2.0× |
| 8 | 80 | **Volcano Tyrant** | (deep-zone mix — orc-form) | close-the-gap | 2.2× |

---

#### Zone 1 — Bone Overlord (fully worked example)

**1. Identity**

- **Fiction beat:** A skeleton that has sat at the bottom of the first zone since before the frontier-settlement expedition arrived, soaking bone-dust from a mass grave in the floor below until its rib cage became a furnace of magicule-saturated calcium.
- **Zone + floor ruled:** Zone 1 capstone — floor 10.
- **What corrupts this floor:** A hairline crack in the arena floor vents bone-dust up from a pre-dungeon mass grave buried one stratum deeper; the Overlord has inhaled it for every one of the many years it has stood here.
- **Primary reaction:** `burst-down-fast`.

**2. Stats** (× 5 HP, × 2 damage, × 0.8 speed, × 10 XP — burst-down-fast band)

Base species: Skeleton at floor 10. Standard-tier species numbers interpolated from SPEC-SPECIES-01 §2 curves: HP ≈ 60, Contact Dmg ≈ 8, Move Speed ≈ 60 px/s, XP ≈ 35.

| Stat | Value |
|---|---|
| Base HP | **300** (60 × 5) |
| Base Contact Damage | **16** (8 × 2) |
| Base Move Speed | **48 px/s** (60 × 0.8) |
| Base XP Yield | **350** (35 × 10) |

Target TTK (class-appropriate melee at level 10): **25–35 s.** Fight must fit in roughly one minute of play; player learns the phase-2 tell and finishes.

**3. AI Pattern**

- Primary pattern: **melee-chase** with a heavy windup (the Overlord lumbers, does not rush).
- Telegraph (contact): N/A — 700 ms hit cooldown per monsters.md.
- **Phase 2 (50% HP):** Overlord plants feet and channels a 900 ms ground-slam telegraph; ground cracks around it; AOE damage in a 3-tile radius on release. Visual cue: aura shifts from dim purple to bright red; `FlashFx.Flash()` white pulse on threshold.
- **Phase 3:** N/A — 2-phase boss (burst-down-fast tier).
- Aggro range: arena-bound — does not pursue past the floor-10 arena tile set (state arena radius in art-pipeline spec when built).
- Leash range: arena-bound; if the player leaves the arena, the Overlord regenerates HP at 5%/s until full.

**4. Drop-Table Hook**

- First-kill bundle (from monster-drops.md floor-10 row): 3× Tier 2 generics, 1× Bone Dust, + 1× Crate bonus container at the boss tile.
- **First-kill guaranteed unique:** 1× unique rolled uniformly from the **FORGE-01 Tier 1 pool** (3 uniques — cf. blacksmith-forge-rng.md §10 Tier 1 roster). Drops pre-forged.
- Save-flag key: `floor10_boss_skeleton`.
- Repeat-kill: standard Skeleton species drops (Bone Dust signature 10%, Bone thematic generic 25%). No bundle, no unique.
- Every-kill standard species drops: yes, both channels (independent of first-kill bundle).

**5. Silhouette Readability**

**Constraint:** The Bone Overlord must read as "a skeleton that has *become an altar*" at 8 tiles away — not merely a large skeleton. Rationale: the zone-1 boss is the player's first "this is a real boss" moment; if it reads as just a taller skeleton the burst-down-fast fantasy collapses into "another mob."

**Bigger-and-badder cues (three used):**

- Scale: 1.8× (Boss band).
- Asymmetry: left arm ends in a fused bone-club larger than its own torso; right arm is ordinary skeleton arm.
- Aura/particle FX: persistent bone-dust emission rising from the rib cage (tiny particles, slow upward drift).

Distinguishable from: standard Skeleton (symmetric, no aura, no club), and from every other starter-roster boss (only boss with bone-dust emission + club asymmetry).

**6. Size / Scale**

- Multiplier: **1.8×**.
- Hitbox radius: **22 px** (round(12 × 1.8)).
- Z-offset: **0** (grounded).
- Canvas size: **160 × 160**.

**7. Color-Coding Contract**

- Base tint surface: body only; features exempt.
- Exempt pixels: the rib-cage furnace-glow core (the emission source — a 3×3 cluster at chest-center), the eye sockets (2 pixels each), and the club's bone-dust shimmer (sparse pixels on the club head).
- Heightened-palette prose: shadows are 2 steps darker than the standard Skeleton palette; the skeleton-species signature purple joint-glow is 2 steps brighter AND appears on 4× as many joints (every rib, both shoulders, pelvis) versus the standard Skeleton's single spine-cluster glow.
- Aura: slow-rising bone-dust particles from the rib cage — pale off-white, roughly 2–3 particles per second, drift upward ~40 px before fading. Compatible with `FlashFx.Flash()` — aura tint modulates via the same sprite-tint channel so the on-hit white flash overrides cleanly.
- Phase color cues: Phase 1 — dim purple aura. Phase 2 — aura shifts to bright red on the 50% HP threshold pulse.

**8. Art-Spec Pairing**

- Paired art ticket: **ART-SPEC-BOSS-01** (`docs/assets/boss-pipeline.md`).
- Pairing status: `[ ] art spec locked  [ ] art assets delivered`.

---

#### Zone 2 — Howling Pack-Father (skeleton fill)

**1. Identity.** A wolf that has led the zone-2 pack long enough to grow as large as the pack it rules. Zone 2 capstone — floor 20. Floor corruption: a den of layered scavenged kills where ambient magicule pools in drying blood. Reaction: `burst-down-fast`.

**2. Stats.** Base species Wolf at floor 20: HP ≈ 90, Dmg ≈ 12, Speed ≈ 70, XP ≈ 45. Boss: **HP 450 (×5), Dmg 24 (×2), Speed 56 (×0.8), XP 450 (×10).** Target TTK: 30–40 s. (Speed override: Pack-Father may hold species speed of 70 if art-lead's silhouette shows an agile wolf-shape; spec recommends default 0.8× multiplier to preserve "boss heft.")

**3. AI Pattern.** Primary: `melee-chase` with lunge attacks (short dash telegraph, 400 ms). Phase 2 (50% HP): summons two phantom wolf-sprites (visual-only minions; they deal contact damage but have 1 HP — the "pack" fantasy). Aura color shifts from neutral grey to blood-red; flash pulse. Phase 3: N/A. Aggro: arena-bound. Leash: regen 5%/s outside arena.

**4. Drop-Table Hook.** First-kill bundle: per monster-drops.md floor-25 row is close enough to apply a near-bundle here (floor 20 uses interpolated 3× Tier 2 generics, 2× Wolf Pelt, + 1× Crate). First-kill unique: **FORGE-01 Tier 2 pool** (4 uniques). Save-flag: `floor20_boss_wolf`. Repeat-kill: standard Wolf drops.

**5. Silhouette.** Must read as "a wolf the size of a small cave bear" at 8 tiles. Cues: scale 1.8×; asymmetry (one massively overgrown shoulder hump); aura (low red breath-steam emission at the muzzle).

**6. Size / Scale.** 1.8×, hitbox 22 px, z-offset 0, canvas 160×160.

**7. Color-Coding.** Exempt pixels: eye glow (both eyes, bright yellow), muzzle breath-steam source (3×2 cluster). Heightened palette: shadows 1 step darker; Wolf species signature grey-brown accent 2 steps brighter on the back-ridge fur. Aura: red breath-steam from muzzle, slow pulse. Phase 1 aura pale red; Phase 2 aura deep red.

**8. Art-Spec Pairing.** Paired ART-SPEC-BOSS-01. `[ ] [ ]`.

---

#### Zone 3 — Chitin Matriarch (skeleton fill)

**1. Identity.** A spider that has brooded in zone 3's deepest web-weave so long its egg sacs have fossilized into armor plating. Zone 3 capstone — floor 30. Floor corruption: a collapsed ceiling cavity where magicule-fog pools and settles on silk. Reaction: `kite-from-range`.

**2. Stats.** Base species Spider at floor 30: HP ≈ 150, Dmg ≈ 18, Speed ≈ 65, XP ≈ 60. Boss: **HP 900 (×6), Dmg 45 (×2.5), Speed 52 (×0.8), XP 600 (×10).** Target TTK: 55–70 s.

**3. AI Pattern.** Primary: `ranged-kite` — throws web-globules from a perch; backs off when player closes. Telegraph: 500 ms web-wind-up with a leg-raise tell. Phase 2 (50% HP): summons 3× spiderlings (1-HP adds) + adds a ground-web AOE (slows player by 50% for 2 s). Aura shifts pale-green → bright toxic-green. Phase 3: N/A. Aggro: arena-bound. Leash: regen outside arena.

**4. Drop-Table Hook.** First-kill bundle (interpolated floor-25 row): 4× Tier 3 generics, 2× Chitin Fragment, + 1× Crate. First-kill unique: **FORGE-01 Tier 3 pool** (5 uniques). Save-flag: `floor30_boss_spider`. Repeat-kill: standard Spider drops.

**5. Silhouette.** Must read as "a fortress with legs" at 8 tiles. Cues: scale 1.8×; additional body part (egg-sac-armor plating visibly layered on back); aura (toxic-green drip particles from fangs).

**6. Size / Scale.** 1.8×, hitbox 22 px, z-offset 0, canvas 160×160.

**7. Color-Coding.** Exempt pixels: fang drip-source (2×1 each fang), 8 compound-eye pixels (cluster). Heightened palette: shadows 2 steps darker; Spider species signature chitin-black 1 step brighter with iridescent green highlights 2× denser than standard. Aura: slow toxic-green droplet emission from fangs. Phase 1 aura pale green; Phase 2 aura bright toxic green.

**8. Art-Spec Pairing.** Paired ART-SPEC-BOSS-01. `[ ] [ ]`.

---

#### Zone 4 — Hollow Archon (skeleton fill)

**1. Identity.** A dark mage whose body has mostly disintegrated, now a floating robe held up by the magicule-current it once spent lifetimes channeling. Zone 4 capstone — floor 40. Floor corruption: an ancient spell-circle etched into the floor still faintly active, concentrating ambient magicules vertically through this tile. Reaction: `kite-from-range`.

**2. Stats.** Base species Dark Mage at floor 40: HP ≈ 210, Dmg ≈ 25, Speed ≈ 45, XP ≈ 80. Boss: **HP 1260 (×6), Dmg 62 (×2.5), Speed 36 (×0.8), XP 800 (×10).** Target TTK: 60–75 s.

**3. AI Pattern.** Primary: `caster` — floats in place, channels ranged bolts. Telegraph: 600 ms bolt wind-up with robe-flare tell. Phase 2 (50% HP): adds a ground-wave AOE attack on a 1200 ms telegraph (visible floor-glyph). Aura color shifts dim purple → bright violet. Phase 3: N/A. Aggro: arena-bound. Leash: regen outside arena.

**4. Drop-Table Hook.** First-kill bundle (floor-50 row applies at floor 40 interpolated): 5× Tier 4 generics, 3× Arcane Residue, 1× Bone Dust, + 1× Chest. First-kill unique: **FORGE-01 Tier 4 pool** (6 uniques). Save-flag: `floor40_boss_darkmage`. Repeat-kill: standard Dark Mage drops.

**5. Silhouette.** Must read as "an empty robe that moves with intent" at 8 tiles. Cues: scale 1.8×; additional body part (a floating broken staff orbiting independently of the robe); aura (persistent purple-violet magicule haze).

**6. Size / Scale.** 1.8×, hitbox 22 px, z-offset **+24 px** (airborne — hovers above ground), canvas 160×160.

**7. Color-Coding.** Exempt pixels: staff-head gem (3×3 cluster), robe-interior void (4×4 cluster at "chest" — the hollow), eye void (4 pixels). Heightened palette: shadows 2 steps darker; Dark Mage species signature purple 2 steps brighter and extended to the staff orbit particle trail. Aura: dense violet magicule haze surrounding the robe. Phase 1 dim purple; Phase 2 bright violet.

**8. Art-Spec Pairing.** Paired ART-SPEC-BOSS-01. `[ ] [ ]`.

---

#### Zone 5 — Warlord of the Fifth (skeleton fill)

**1. Identity.** An orc that has survived every champion of zone 5 for generations, wearing the combined regalia of every previous warlord it killed stacked in layers. Zone 5 capstone — floor 50. Floor corruption: centuries of accumulated blood-soaked trophy iron exhale magicule-iron fumes. Reaction: `kite-from-range` (throws weapons, closes briefly).

**2. Stats.** Base species Orc at floor 50: HP ≈ 340, Dmg ≈ 32, Speed ≈ 55, XP ≈ 110. Boss: **HP 2040 (×6), Dmg 80 (×2.5), Speed 44 (×0.8), XP 1100 (×10).** Target TTK: 65–80 s.

**3. AI Pattern.** Primary: `ranged-kite` — throws axes from mid-range, advances briefly to slam if cornered. Telegraph: 500 ms throw wind-up with arm-cock tell. Phase 2 (50% HP): throw cooldown halved; adds a charge attack on a 700 ms wind-up. Aura color shifts iron-grey → hot-iron-orange. Phase 3: N/A. Aggro: arena-bound. Leash: regen outside arena.

**4. Drop-Table Hook.** First-kill bundle (floor-50 row): 5× Tier 4 generics, 3× Orc Tusk, 1× Bone Dust, + 1× Chest. First-kill unique: **FORGE-01 Tier 5 pool** (10 uniques). Save-flag: `floor50_boss_orc`. Repeat-kill: standard Orc drops.

**5. Silhouette.** Must read as "a warrior dragging its own history behind it" at 8 tiles. Cues: scale 2.0×; crown/regalia (stacked-trophy armor — visibly mismatched plates layered on shoulders and back); aura (hot-iron-orange glow from the layered plates' seams).

**6. Size / Scale.** 2.0×, hitbox 24 px, z-offset 0, canvas 160×160.

**7. Color-Coding.** Exempt pixels: iron-glow seams (thin bright lines between every regalia layer), tusk highlight (2 pixels per tusk), eye glow (2 pixels). Heightened palette: shadows 2 steps darker; Orc species signature green-brown skin 1 step darker (the Warlord is *older* than other orcs); iron regalia introduces a new bright orange accent carried by the aura. Aura: slow orange heat-shimmer from armor seams. Phase 1 iron-grey aura; Phase 2 hot-iron-orange.

**8. Art-Spec Pairing.** Paired ART-SPEC-BOSS-01. `[ ] [ ]`.

---

#### Zone 6 — The Screaming Flight (skeleton fill)

**1. Identity.** A bat-swarm that has fused at the edges over decades into a single many-winged, many-mouthed creature — no longer individuals, yet still a flight. Zone 6 capstone — floor 60. Floor corruption: a wind-tunnel floor with updrafts strong enough that magicule-fog never settles; everything here breathes it continuously. Reaction: `close-the-gap`.

**2. Stats.** Base species Bat at floor 60 (interpolated): HP ≈ 400, Dmg ≈ 38, Speed ≈ 110, XP ≈ 140. Boss (×8 / ×3 / ×0.8 / ×10 — close-the-gap band): **HP 3200, Dmg 114, Speed 88, XP 1400.** Target TTK: 100–130 s. (Speed override note: 88 px/s is still higher than the other bosses but below the species baseline of 110 — the fusion-boss is heavier than an individual bat but not as slow as a grounded boss. Acceptable override of the 0.8× base-speed rule; documented.)

**3. AI Pattern.** Primary: `ranged-kite` inverted — flies above the player and dives periodically (the player must close to hit; the boss evades by flying higher rather than retreating laterally). Telegraph: 450 ms dive wind-up with a visible tilt. Phase 2 (50% HP): starts detaching smaller bat-fragments (1-HP adds) that swarm the player; aura shifts pale-grey → screeching-yellow. **Phase 3 (25% HP):** boss collapses into a ground-level thrashing mass — switches AI pattern to `melee-chase` with fast sweeping attacks; aura shifts to raw white-hot. Aggro: arena-bound. Leash: regen outside arena.

**6-8 sections** (stats above; silhouette + rendering below):

**5. Silhouette.** Must read as "a cloud that has teeth" at 8 tiles. Cues: scale 2.0×; additional body part (many independent wing-pairs radiating outward); aura (audible-feel screeching-yellow shimmer around the wing-fringe).

**6. Size / Scale.** 2.0×, hitbox 24 px, z-offset **+40 px** (airborne Phase 1 + 2; drops to 0 in Phase 3 — art must deliver an "ground-collapse" frame at the Phase 3 transition), canvas 160×160.

**7. Color-Coding.** Exempt pixels: many-eye cluster at the core (8+ pixels), wing-fringe shimmer (sparse pixels along every wing edge). Heightened palette: shadows 2 steps darker; Bat species signature leathery-brown 1 step darker on the core; many-eye cluster carries a new saturated yellow. Aura: wing-fringe yellow shimmer. Phase 1 pale grey; Phase 2 screeching yellow; Phase 3 raw white-hot.

**8. Art-Spec Pairing.** Paired ART-SPEC-BOSS-01. `[ ] [ ]`.

**4. Drop-Table Hook.** First-kill bundle (floor-50 row baseline, scaled): 5× Tier 4 generics, 3× Echo Shard, + 1× Chest. First-kill unique: **FORGE-01 Tier 5 pool.** Save-flag: `floor60_boss_bat`. Repeat-kill: standard Bat drops.

---

#### Zone 7 — Iron-Gut Goblin King (skeleton fill)

**1. Identity.** A goblin that has eaten everything. Every weapon, every armor scrap, every carcass, every other goblin. Its stomach is a magicule-forge, and its skin has re-grown around the iron it has swallowed. Zone 7 capstone — floor 70. Floor corruption: the zone's magicule current pools into a specific tile the King has never left; he eats the floor itself. Reaction: `close-the-gap`.

**2. Stats.** Base species Goblin at floor 70 (interpolated): HP ≈ 480, Dmg ≈ 42, Speed ≈ 60, XP ≈ 170. Boss (×8 / ×3 / ×0.8 / ×10): **HP 3840, Dmg 126, Speed 48, XP 1700.** Target TTK: 110–150 s.

**3. AI Pattern.** Primary: `melee-chase` — slow lumber with heavy sweeps. Telegraph: 700 ms belly-slam wind-up. Phase 2 (50% HP): vomits a pool of iron-slag that persists as a damage-over-time zone for 4 s on a 1000 ms telegraph; aura shifts dim-iron-grey → molten-orange. **Phase 3 (25% HP):** King splits its stomach open and becomes a turret — stationary, firing projectiles on a 400 ms telegraph; aura raw-white-hot. Aggro: arena-bound. Leash: regen outside arena.

**5. Silhouette.** Must read as "a goblin that has become a siege engine" at 8 tiles. Cues: scale 2.0×; additional body part (a distended iron-scab belly that visibly bulges more than its torso); crown (a crude iron-circlet melted into the skull); aura (iron-grey heat-shimmer from belly seams).

**6. Size / Scale.** 2.0×, hitbox 24 px, z-offset 0, canvas 160×160.

**7. Color-Coding.** Exempt pixels: belly-seam iron-glow (thin bright horizontal line), circlet highlights (6 pixels), eye glow (2 pixels). Heightened palette: shadows 2 steps darker; Goblin species signature yellow-green 1 step sicklier; iron-slag accent is a new molten-orange. Aura: belly heat-shimmer. Phase 1 dim-iron-grey; Phase 2 molten-orange; Phase 3 raw white-hot.

**8. Art-Spec Pairing.** Paired ART-SPEC-BOSS-01. `[ ] [ ]`.

**4. Drop-Table Hook.** First-kill bundle: 5× Tier 5 generics, 3× Goblin Tooth, 1× signature of every Zone 1–3 species, + 1× Chest. First-kill unique: **FORGE-01 Tier 5 pool.** Save-flag: `floor70_boss_goblin`. Repeat-kill: standard Goblin drops.

---

#### Zone 8 — Volcano Tyrant (skeleton fill)

**1. Identity.** A creature born orc, reshaped by magicules into a humanoid furnace. Stands on a permanent fissure of superheated rock in the deepest explored stratum. Zone 8 capstone — floor 80 (deepest in starter roster). Floor corruption: a magma vent from below feeds the fissure directly; the Tyrant has stood on it long enough to become it. Reaction: `close-the-gap`.

**2. Stats.** Base species (deep-zone mix, orc-form) at floor 80: HP ≈ 600, Dmg ≈ 45, Speed ≈ 55, XP ≈ 200. Boss (×8 / ×3 / ×0.8 / ×10): **HP 4800, Dmg 135, Speed 44, XP 2000.** Target TTK: 120–180 s.

**3. AI Pattern.** Primary: `melee-chase` with delayed heavy swings (900 ms telegraph on hammer-swing). Phase 2 (50% HP): ground erupts in three telegraphed fissure-lines that leave persistent lava tiles for 3 s; aura shifts magma-red → incandescent-orange. **Phase 3 (25% HP):** the Tyrant's body cracks open, revealing its core; it now emits a passive heat-aura that damages the player over time if within 2 tiles; aura raw white-gold. Player must balance attack uptime against burn damage. Aggro: arena-bound. Leash: regen outside arena.

**5. Silhouette.** Must read as "a volcano that stood up" at 8 tiles. Cues: scale 2.2×; asymmetry (one shoulder is a chunk of raw erupting rock, visibly larger than the other); aura (persistent magma-red emission from body cracks); crown/regalia (a jagged obsidian mantle across shoulders).

**6. Size / Scale.** 2.2×, hitbox 26 px, z-offset 0, canvas 160×160.

**7. Color-Coding.** Exempt pixels: body-crack magma (multiple thin bright lines across torso + limbs), obsidian mantle highlights (scattered pixels), eye glow (4 pixels each, bright). Heightened palette: shadows 3 steps darker (this is the deepest boss); Orc signature green-brown supplanted by a new char-black + magma-red duotone. Aura: magma-red body-crack emission. Phase 1 magma-red; Phase 2 incandescent-orange; Phase 3 raw white-gold.

**8. Art-Spec Pairing.** Paired ART-SPEC-BOSS-01. `[ ] [ ]`.

**4. Drop-Table Hook.** First-kill bundle: 5× Arcane materials per type, 5× of any 3 signatures (zone 8 uses the floor-150+ row baseline interpolated), + 2× Chest. First-kill unique: **FORGE-01 Tier 5 pool.** Save-flag: `floor80_boss_orc`. Repeat-kill: standard deep-zone drops.

---

### 4. Unique-drop pairing — zone-to-FORGE-tier mapping (locked)

Each boss's first-kill unique is pulled from the FORGE-01 unique pool at the tier below:

| Zone | Boss floor | FORGE-01 tier pool | Pool size | Rationale |
|---|---|---|---|---|
| 1 | 10 | **Tier 1** | 3 uniques | Tutorial tier — the player's first unique from their first boss. |
| 2 | 20 | **Tier 2** | 4 uniques | Early-game variety. |
| 3 | 30 | **Tier 3** | 5 uniques | Mid-game power jump. |
| 4 | 40 | **Tier 4** | 6 uniques | Late-P1 endgame prep. |
| 5 | 50 | **Tier 5** | 10 uniques | First boss to drop from the endgame pool. |
| 6 | 60 | **Tier 5** | 10 uniques | Endgame churn begins — repeat access to the biggest pool. |
| 7 | 70 | **Tier 5** | 10 uniques | More Tier-5 access. |
| 8 | 80 | **Tier 5** | 10 uniques | Deepest starter-roster boss. |

Zones 5–8 all roll from Tier 5 because FORGE-01 only defines 5 tiers; Tier 5 is the endgame pool by design and the boss floors 50–80+ are where the player is expected to farm for pool completion. This overlap is intentional — a player can get duplicates and that's fine (the unique still has value; and future "re-roll / upgrade" mechanics per FORGE-01 § Open Questions can consume them).

**Rule for future zone additions:** zones 9+ keep drawing from Tier 5 until FORGE-01 adds a Tier 6 pool. At that point this mapping is revisited via a new ticket.

---

### 5. Art-Spec Pairing

Paired art spec: **ART-SPEC-BOSS-01** (`docs/assets/boss-pipeline.md`) — authored in parallel by art-lead. Pipeline spec owns:

- PixelLab prompt templates for each of the 8 bosses, extending `CHAR-MON-ISO`.
- Canvas contract: 160×160 per prompt-templates.md §1a.
- Hex codes for per-boss palette + aura colors.
- Per-boss animation set (idle, walk, attack, phase-shift transition, death).
- Directory convention under `assets/characters/bosses/<boss_id>/`.

**Co-lock criterion:** neither this spec nor the pipeline spec reaches "Ready-for-impl" alone. A boss is implementable only when both halves are locked for that boss. Partial co-lock is acceptable — e.g. Bone Overlord may co-lock while zone-8 Volcano Tyrant remains in-flight.

## Acceptance Criteria

- [ ] Art-lead can author PixelLab prompts for all 8 bosses from this spec + ART-SPEC-BOSS-01 with no further design-lead input.
- [ ] Each of the 8 bosses has a distinct silhouette from (a) its zone's standard species, and (b) every other boss in the roster — verifiable at 64-pixel thumbnail scale.
- [ ] All 8 starter bosses have completely filled templates: zone-1 Bone Overlord fully worked; zones 2–8 have every required field answered (no TBDs, no "author later" placeholders).
- [ ] The §2 stat-multiplier table resolves unambiguously to numbers for every boss given the base species's floor-matched standard-tier stat line — no hand-tuning needed at implementation time.
- [ ] The §4 phase-shift convention (aura color cue + behavior change) is applied consistently across all 8 bosses that use phasing.
- [ ] Every boss names its FORGE-01 tier pool (§4 pairing table) and save-flag key in its drop-table-hook section.
- [ ] An engineer can read this spec + the FORGE-01 unique-pool tables + the base species's stats and wire the boss's `EnemySpecies`-equivalent config (HP / damage / speed / AI pattern / phase thresholds / drop hook) without design clarification.

## Implementation Notes

- **Bosses are not a new `EnemySpecies` value.** They are authored as a boss-flag on an existing species enum entry + a per-boss config record. This keeps the drop-table inheritance clean (the Bone Overlord is still a Skeleton for repeat-kill material drops) and lets the boss's art asset swap in at the correct base species without a new enum case.
- **Phase-shift HP thresholds fire on the damage-application tick** — not interpolated. A boss crossing 50% HP on a single attack fires the Phase-2 cue once; if an attack crosses both 50% and 25% in one tick, fire both cues sequentially with a 300 ms gap (don't swallow either).
- **Arena-bound aggro / leash with 5%/s HP regen outside arena** is a new engine capability the combat/AI layer does not yet have. Flag to combat-lead as a prerequisite for boss implementation — arena tile-set is tracked at the level-gen layer and bosses read that tile-set reference on spawn.
- **First-kill unique drops pre-forged.** The unique is selected via `UniqueItemDatabase.Roll(tier=N)` (uniform over the pool) and dropped onto the boss tile as a normal equipment drop, using the forge path in reverse. No materials are consumed; no Blacksmith interaction is needed. Save-flag check prevents repeat-drop.
- **Zone-1 Bone Overlord is the first boss to implement.** It has the simplest rules (2-phase, melee, grounded) and its unique pool is the smallest (Tier 1 = 3 uniques). Use it as the reference implementation; patterns from it propagate to zones 2–8.
- **Aura / particle FX pipeline** must be defined before boss art generation to ensure the aura source pixel clusters are planned into the sprite. Flag to art-lead for ART-SPEC-BOSS-01.
- **Not in scope:** multi-phase arena transforms (boss room geometry changes mid-fight), co-boss encounters (two bosses simultaneously), pre-boss mini-arenas, boss-only cutscenes, boss-specific music stingers. All potential future work.

## Open Questions

None — spec is locked.
