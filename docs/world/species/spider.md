# Species — Spider

## Summary

Zone 3 ambush predator. A low, wide, eight-legged arachnid that lurks in webbed corners until a footstep trips its aggro, then closes in a single hard lunge. Glass-cannon by design: the fantasy is "you stepped into its territory and now you have half a second to kill it or eat the bite." Spec authored via SPEC-SPECIES-SPIDER-01 (Phase E).

## Current State

**Spec status: LOCKED** — authored 2026-04-18 per SPEC-SPECIES-SPIDER-01 (Phase E, roadmap entry #14). Paired art ticket ART-14 covers the re-art batch (Bat / Spider / Wolf) that replaces the current bipedal-humanoid placeholder sprite with a true arachnid silhouette.

Paired with [species-template.md](../species-template.md) (structural contract) and [species-pipeline.md](../../assets/species-pipeline.md) (art pipeline ART-SPEC-02, `.arach` sub-variant). Neither half ships without the other.

Feeds [SPEC-BOSS-CHITIN-MATRIARCH-01](../../spec-roadmap.md) (Phase F) — the Chitin Matriarch inherits this species's body plan, scale floor, and drop-thematic identity, then overlays boss-tier multipliers and phase-shift behavior. **This spec does not define boss behavior** — that belongs to the boss ticket.

## Design

### 1. Identity

- **Fiction beat:** An ordinary cave spider whose silk glands were overfed on raw magicules until the webbing set harder than the rock it anchors to, and its fangs grew a venom that forgets what it was supposed to paralyze.
- **Role in dungeon ecology:** Immune system — Spiders are the dungeon's corner-trap. They do not hunt; they wait for intruders to step into a spun room, then strike. Slower predators (Orcs in zone 5, Dark Mages in zone 4) lean on Spiders to soften anything that makes it that deep.
- **Intended player emotional reaction:** `burst-down-fast` (the Spider reveals itself at the worst moment from the worst angle; the player must collapse it before the lunge lands or accept the trade). Also `cautious-approach` at aggro range on repeat visits — once a player knows a corner is webbed, they slow down entering it.

### 2. Stats

Tier 2 (between Bat's Tier 1 and Orc's Tier 3). Glass-cannon archetype: damage is above Bat, HP is slightly above Bat, move speed is burst-high during lunge window only (see §3).

| Field | Floor 3 (Early) | Floor 28 (Mid) | Floor 75 (Deep) |
|---|---|---|---|
| Base HP | 28 | 180 | 780 |
| Base Contact Damage | 7 | 22 | 62 |
| Base Move Speed (px/s) | 70 idle / 160 lunge | 85 / 185 | 100 / 210 |
| Base XP Yield | 16 | 70 | 260 |

**Floor 3 note:** Spider first appears in zone 3 (floors 21–30), not zone 1, so the floor-3 numbers are extrapolated-baseline (for template consistency with Bat/Skeleton) — they are what a Spider *would* be at floor 3, not what the player will actually encounter. Earliest real encounter is ~floor 21.

Target TTK (class-appropriate melee, level matching floor):
- **Floor 3 (extrapolated):** 1–2 hits. Glass-cannon target; if the player reaches it, it dies instantly.
- **Floor 28 (primary):** 1–2 hits. Feel is "you saw movement in the corner, you swung, it's dead — or you didn't swing, and you're bleeding."
- **Floor 75 (deep):** 2 hits. Scaling preserves the feel; Spiders do not become tanky at depth, they become *faster to reveal* (see §3 aggro-range note).

**Consistency check (§1 ↔ §2):** `burst-down-fast` reaction maps to 1–2 hits per the template's TTK table. ✓

### 3. AI Pattern

- Pattern: **ambush**.
  - Idle state: stationary in corners / against walls / near webbed props, in a crouched pose that reads as "part of the furniture" at aggro-range.
  - Trigger: LOS + proximity (see aggro-range below). On trigger, Spider reveals (brief upward body-rise animation, silhouette flares to full leg-spread) and enters lunge state.
  - Lunge state: burst move speed for ~800 ms toward player, then contact damage on touch, then reverts to idle-chase at standard speed.
  - If not killed within ~3 seconds of reveal, Spider transitions to plain `melee-chase` at standard idle speed — it has shown itself; the ambush is spent.
- Telegraph: **200 ms body-rise flash before lunge** (sprite goes from crouched silhouette to full leg-spread; a short yellow-tint FlashFx cue on the body during the rise). The telegraph is intentionally tight — 200 ms is enough for a player paying attention at aggro range to react, and short enough that an unaware player eats the lunge. Tighter than a caster's 450 ms because the Spider's payoff for being ambushed is *the surprise*, not a heavy hit.
- Aggro range: **120 px** (tighter than the monsters.md `chase-always` default). Spiders do not wake up until you're close — the "step into its room" feel requires a proximity trigger, not global-chase. At deep floors (75+), aggro range scales up to 180 px to compensate for faster player movement tools.
- Leash range: **360 px**. Once revealed and engaged, a Spider will pursue within a 3-tile leash; if the player sprints past and breaks leash, the Spider returns to its corner and re-enters idle (re-ambushable on a return pass). This lets zone 3 feel like a traversable space where individual Spider encounters are skippable, not a chain-pull.

The 120-px aggro + 200-ms telegraph + burst-lunge is what makes this `ambush`, not `melee-chase`. A straight `melee-chase` Spider would be a reskinned Bat.

### 4. Drop-Table Hook

- Signature material: **Chitin Fragment** (`material_sig_spider`), 8% per kill. Matches locked entry in `scripts/logic/MonsterDropTable.cs:47`: `Spider → MonsterTier.Two, "material_sig_spider", 0.08f, MaterialType.Hide`.
- Thematic generic: **Hide** (60% hide, 20% ore, 20% bone on the generic channel).
  - Narrative reading: "Hide" mechanically covers both the Spider's chitinous carapace and the hardened silk shed from its spinnerets. Players see material drops named *Spider Silk* / *Carapace Shard* / *Chitin Plate* in the Hide-family catalog slots; the drop-table's `Hide` bias is a mechanical category, not a fiction category. This is the same pattern Bat uses (leathery wing = Hide).
- Special drop: None at species level. **First-kill-of-floor-30 (zone 3 boss encounter) drops are defined by SPEC-BOSS-CHITIN-MATRIARCH-01, not here.**

### 5. Silhouette Readability Constraint

**Must read as an arachnid from 8 tiles away — silhouette must show at least six of its eight legs fanned outward, a low-profile body no taller than 40% of the canvas, and a footprint wider than it is tall (bottom-heavy, ground-hugging).** Rationale: the fear of stepping into a Spider's territory is the fear of *recognizing it late*. If a Spider reads as a generic blob at aggro-range, the player cannot distinguish it from rubble or a decorative prop, and the ambush feels cheap (the player believes the game spawned an enemy under them). A clearly-arachnid silhouette at aggro-range turns "the game killed me" into "I should have seen that corner before I walked into it" — that's the difference between frustration and the intended caution-onset loop.

### 6. Size / Scale Rule

- Multiplier: **0.75×** (Small band per template).
- Hitbox radius: **9 px** (round(12 × 0.75) = 9).
- Z-offset: **0 px** (ground-dwelling — standard iso Y-sort, no airborne override).

Rationale for 0.75 and not Standard (0.9–1.1×): a Standard-size Spider reads as a *boss* Spider, which undercuts the Chitin Matriarch's silhouette payoff in zone 3. Keeping the regular Spider small leaves the "bigger and badder" visual cue (per SPEC-BOSS-ART-01) available for the boss. Spiders are small-and-many; the Matriarch is one-and-huge.

### 7. Color-Coding Contract

- Base tint surface: **body only, features exempt** (Spiders have signature glowing eye clusters that must survive the level-relative modulate).
- Exempt pixels: **eye cluster** (4–8 small bright pixels on the cephalothorax, grouped as a distinct cluster — not single points like Bat's two eyes). Reads as a concentrated glint at aggro-range, which is what triggers the "corner has eyes" recognition that drives cautious-approach on repeat visits (see §1).
- Exempt-pixel implementation note: separate sprite sub-node modulated `Color.White` on top of the tinted body, per the same technique Bat uses. ART-SPEC-02 prompt should author the eye-cluster layer as a standalone sprite slice to make the carve-out cheap.

### 8. Art-Spec Pairing

- Paired art ticket: **ART-14** (Bat / Spider / Wolf rework batch).
- Pairing status: `[ ] art spec locked  [ ] art assets delivered`.

---

## Acceptance Criteria

- [ ] Engineer can implement the Spider from this spec + `species-template.md` + `EnemySpecies.cs` conventions + `MonsterDropTable.cs` (already populated) without a clarifying question.
- [ ] Art-lead can compose the PixelLab prompt from this spec + ART-SPEC-02's `.arach` sub-variant without a clarifying question.
- [ ] §1 reaction (`burst-down-fast`) matches §2 TTK (1–2 hits) matches §3 AI pattern (`ambush` with 200 ms telegraph + burst lunge).
- [ ] §5 silhouette constraint (eight legs fanned, low profile, wide footprint) is what ART-14 will test against at deliverable review.
- [ ] Chitin Matriarch boss spec (SPEC-BOSS-CHITIN-MATRIARCH-01, Phase F) can inherit §5 silhouette as body-plan baseline and §6 as scale floor (the Matriarch scales up from 0.75 to 1.8×–2.2× per SPEC-BOSS-ART-01) with no retcon on this spec.

## Implementation Notes

- **Existing C# hooks:** `EnemySpecies.Spider` enum value exists; `SpeciesDatabase` hitbox entry exists; `MonsterDropTable` entry exists (line 47, verified). No new enum or database additions needed — the spec documents what's already wired.
- **AI pattern is new.** Current Spider likely uses `melee-chase` baseline (inherited from the generic enemy behavior). Moving to `ambush` will require either (a) a new `ambush`-tagged behavior branch in the existing enemy AI, or (b) an `AmbushAIComponent` attached to Spider specifically. This is an impl-time decision for the implementing team, flagged here so they know the behavior delta before scoping the ticket. Non-Spider species are unaffected.
- **Telegraph FlashFx hook:** the 200-ms body-rise tint reuses the existing FlashFx system (per `FlashFx.cs`) with a short yellow tint on the Spider sprite only. No new FlashFx type is needed — pick an existing `boost`-family flash or add a `telegraph`-variant in its own audit ticket if the existing ones don't read right.
- **Aggro range 120 px** is an override of the monsters.md `chase-always` default. The engine already supports per-species aggro overrides (Bat leaves its at `chase-always`); Spider sets a value. Deep-floor aggro-range scaling (120→180 past floor 75) can be deferred to a balance-pass ticket if the impl team wants the initial Spider to ship with flat 120-px aggro and tune later.
- **Zone appearance:** Spider spawns in zone 3 (Tier 2, floors 21–30) per `Constants.Zones`. Already present in code. Zone 3's other species is Orc (Tier 3), so Spider is the tier-2 "lighter threat" slot in that zone — the player's mental model is "Orcs are the slow heavy threat, Spiders are the fast ambush threat that punishes lazy movement."
- **The signature material id `material_sig_spider`** is the drop-table code-level id. Its player-facing name ("Chitin Fragment") lives in `ItemDatabase` and is not defined here — if `ItemDatabase.Get("material_sig_spider")` returns a def with a different display name, update the item catalog, not this spec.

## Open Questions

None — spec is locked.
