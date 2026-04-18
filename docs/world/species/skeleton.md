# Species — Skeleton

## Summary

Grounded melee species native to zone 1, the first real enemy the player fights on the frontier dungeon expedition. Defined by a sword-and-shield silhouette that reads as *deliberate* from aggro-range so the player learns, early and cheaply, that approach and timing matter. The standard-tier base for the Bone Overlord boss (zone 1 — see [boss-art.md §Zone 1 — Bone Overlord](../boss-art.md#zone-1--bone-overlord-fully-worked-example)). Locked via SPEC-SPECIES-SKELETON-01 (Phase E, 2026-04-18).

## Current State

**Spec status: LOCKED** — authored 2026-04-18 per SPEC-SPECIES-SKELETON-01 (Phase E). No paired art ticket exists yet; placeholder `ART-SKELETON` is stubbed here and in dev-tracker for the eventual re-art pass. The current in-game sprite is an unmodified ad-hoc placeholder; silhouette constraint (§5) is the acceptance test a future `ART-SKELETON` PR must clear.

Paired with [species-template.md](../species-template.md) (structural contract) and [species-pipeline.md](../../assets/species-pipeline.md) (art pipeline ART-SPEC-02). Neither half ships without the other.

Existing C# hooks already in place (this spec documents them, it does not redesign them):
- `scripts/logic/EnemySpecies.cs:8` — `Skeleton = 0`.
- `scripts/logic/SpeciesDatabase.cs:22-23` — `SpeciesConfig` entry exists with placeholder scale `0.70f`. This spec sets the target scale to **1.00×** (Standard band per template §6); current code value is a placeholder to be retuned when `ART-SKELETON` lands (see §6).
- `scripts/logic/MonsterDropTable.cs:40` — drop-table row locked: `material_sig_skeleton`, 10%, `MaterialType.Bone`. This spec documents that row; it does not propose changes.

## Design

### 1. Identity

- **Fiction beat:** A reanimated warrior whose bones have drunk enough raw magicules from the zone-1 strata to walk again in the shape of what it used to be — a soldier with sword and shield, patient and disciplined, because the magicules remember the drill more than the man.
- **Role in dungeon ecology:** Immune system — Skeletons are the dungeon's entry-level guards. They stand watch in the first zone and close on anything that walks in, giving the deeper layers time to react.
- **Intended player emotional reaction:** `close-the-gap` (the player should charge in and commit, not sit back — the Skeleton's shield telegraphs moments where striking is wasted and moments where it is clean, rewarding players who read the animation rather than mash).

### 2. Stats

| Field | Floor 3 (Early) | Floor 28 (Mid) | Floor 75 (Deep) |
|---|---|---|---|
| Base HP | 34 | 185 | 820 |
| Base Contact Damage | 5 | 17 | 44 |
| Base Move Speed (px/s) | 62 | 74 | 86 |
| Base XP Yield | 14 | 56 | 210 |

Target TTK (class-appropriate melee, level matching floor): **3–5 hits** every floor — Skeletons are the baseline "solid body, not a tank" fight. The feel is "if you committed to the approach you should win this trade; if you half-committed you'll eat a sword-hit for it."

**Comparison to Bat (zone-1 reference):** at floor 3, Skeleton has ~55% more HP (34 vs 22), ~25% more contact damage (5 vs 4), and ~30% less move speed (62 vs 90) than Bat. This is the deliberate trade — Skeletons are grounded, durable, and slower; Bats are airborne, frail, and fast. The XP yield is slightly higher than Bat at every floor (14 vs 12 early, 56 vs 48 mid, 210 vs 180 deep) to reflect the longer fight.

### 3. AI Pattern

- Pattern: **melee-chase** — chases the player in a straight line and hits on contact. No projectiles, no coordination with other skeletons (that's `pack`, which this spec explicitly does not use — skeletons fight as individuals even when spawned in groups).
- Telegraph: N/A — contact damage, 700 ms hit cooldown per [monsters.md](../monsters.md). No shield-raise or wind-up telegraph in the base species spec; those belong to the Bone Overlord boss variant (see §Implementation Notes).
- Aggro range: `chase-always` (current engine default).
- Leash range: `never-leash` (current engine default).

**Why plain melee-chase and not melee-chase-with-shield-telegraph:** adding a shield-raise wind-up would push TTK target up (the player waits for the opening), which conflicts with `close-the-gap` (the player *is* the opening-maker). Leaving shield-raise to the boss variant keeps the base species a clean, readable first-encounter and gives the Bone Overlord a real escalation to earn.

### 4. Drop-Table Hook

- Signature material: **Skeleton Bone Dust** (`material_sig_skeleton` — see [item-catalog.md § Materials](../../inventory/item-catalog.md)), 10% per kill.
- Thematic generic: **Bone** (60% bone, 20% ore, 20% hide on the generic channel — calcified skeletal frame).
- Special drop: None.

Matches locked entry in `scripts/logic/MonsterDropTable.cs:40`: `Skeleton → MonsterTier.One, material_sig_skeleton, 0.10f, MaterialType.Bone`. This spec documents the existing row; no change proposed.

### 5. Silhouette Readability Constraint

**Must read as an upright armed-humanoid with sword-and-shield asymmetry from 8 tiles away — the two hands must be visibly different (weapon on one side, shield on the other) so the silhouette is distinguishable from a robed caster or a weaponless goblin at a glance.** Rationale: zone-1 is the player's first-ever combat encounter in the dungeon. If every zone-1 enemy reads as "vaguely humanoid lump," the player cannot start learning encounter-level tactics (who-to-hit-first, who's-armed, who-to-kite). Sword-and-shield asymmetry is cheap, legible, and establishes the class vocabulary the dungeon will use for every subsequent armed humanoid.

### 6. Size / Scale Rule

- Multiplier: **1.00×** (Standard band per template §6 — player-scale, the baseline "an enemy the size of me" presentation)
- Hitbox radius: **12 px** (round(12 × 1.00) = 12 — matches the current `HitAreaRadius = 12f` in `SpeciesDatabase.cs:23`; the stale `SpriteScale = 0.70f` is a placeholder to be corrected when `ART-SKELETON` lands)
- Z-offset: **0 px** (grounded — no airborne override)

**Why Standard band and not Small:** the Skeleton is the archetypal baseline enemy. If the player's first fight is against something noticeably smaller than themselves, the "grounded, committed, sword-and-shield trade" fantasy undersells; the fight reads as "squashing a minion" instead of "dueling a soldier." Standard band (player-parity) is what sells a first-zone melee opponent as a peer. Bat is in the Small band because its fantasy is "pest you swat," not "warrior you trade with."

### 7. Color-Coding Contract

- Base tint surface: **body only, features exempt** — the bones themselves carry level-gap tint; two identifying features stay unmodulated.
- Exempt pixels:
  - **Bone-white highlights** along sword edge, shield rim, and forearm bones (a sparse 2–3 pixel cluster per feature). These preserve the "freshly-polished bone, not dust-tinted" identity marker at any level gap.
  - **Eye-socket glow** (2 pixels, pale-purple, per eye socket). Matches the zone-1 species signature joint-glow tone used by Bone Overlord (see [boss-art.md §Zone 1 — Bone Overlord](../boss-art.md#zone-1--bone-overlord-fully-worked-example) — boss amplifies this glow, standard species carries the base).
- Exempt-pixel implementation note: separate sprite sub-node modulated `Color.White` on top of the tinted body — same technique used by Bat's eye-highlight carve-out per [bat.md §7](bat.md#7-color-coding-contract).

### 8. Art-Spec Pairing

- Paired art ticket: **ART-SKELETON** (placeholder — not yet authored; covers first proper Skeleton re-art pass against this spec's silhouette constraint).
- Pairing status: `[ ] art spec locked  [ ] art assets delivered`.

---

## Acceptance Criteria

- [ ] Engineer can implement the Skeleton from this spec + `species-template.md` + `EnemySpecies.cs` conventions without a clarifying question.
- [ ] Art-lead can compose the PixelLab prompt for `ART-SKELETON` from this spec + ART-SPEC-02's prompt skeleton without a clarifying question.
- [ ] §1 reaction (`close-the-gap`) matches §2 TTK (3–5 hits) matches §3 AI (`melee-chase`, no telegraph) per the template's consistency matrix.
- [ ] §5 silhouette constraint (sword-and-shield asymmetry, armed-humanoid read at 8 tiles) is the acceptance test for `ART-SKELETON` deliverables.
- [ ] §2 stats slot in numerically below Wolf (zone 2, Tier 2) and Orc (zone 3, Tier 3) at every sample floor — a zone-1 Tier-1 species should not out-damage or out-HP a later-zone higher-tier species when both are normalized to the same floor. (Cross-check when SPEC-SPECIES-WOLF-01 / SPEC-SPECIES-ORC-01 land.)

## Implementation Notes

- **Existing C# hooks:** `EnemySpecies.Skeleton = 0` exists; `SpeciesDatabase` entry exists; `MonsterDropTable` row exists. No new enum or database additions needed — this spec documents what's already there, with one tuning note on the placeholder scale (§6).
- **Scale retune is `ART-SKELETON`'s problem, not this spec's.** The current `SpriteScale = 0.70f` in `SpeciesDatabase.cs:23` is a placeholder inherited from pre-spec art. When `ART-SKELETON` lands, the new sprite should be authored at the 1.00× target in §6 and the `SpriteScale` constant retuned. Until then, the game ships with the placeholder and it looks smaller than this spec specifies — that's fine, it's a known art-debt item.
- **Bone Overlord boss uses this spec as its base body plan, not its behavior plan.** The standard Skeleton is plain `melee-chase` with no telegraph; the Bone Overlord layers on phase shifts, a 2-phase attack pattern, a unique silhouette cue (fused bone-club + bone-dust aura), and scale 1.8× — all authored separately in [boss-art.md §Zone 1 — Bone Overlord](../boss-art.md#zone-1--bone-overlord-fully-worked-example) and the forthcoming SPEC-BOSS-BONE-OVERLORD-01 (Phase F). Do not expand this species spec to cover boss behavior.
- **Zone appearance:** Skeletons spawn in zone 1 (Tier 1) alongside Bats per `monsters.md` Zone Roster. Zone-1 boss (Bone Overlord) is a skeleton variant at floor 10. Skeletons do not currently appear in any later zone; if a deeper-zone re-use is desired (e.g. zone-5 "barrow-revenant" elite variant), that's a separate species-variant spec, not an expansion here.
- **Stat curve derivation:** the three sample floors (3 / 28 / 75) roughly follow a `base * 1.035^(floor-1)` growth curve on HP, tuned so that floor-3 HP (34) is ~55% above Bat's 22 and floor-75 HP (820) is ~52% above Bat's 540 — Skeleton stays a touch more durable than Bat at every depth, consistent with the grounded/airborne role split. Move-speed growth is flatter (62 → 86 over 72 floors, ~38% total) because slow-and-steady is part of the species identity.

## Open Questions

None — spec is locked.
