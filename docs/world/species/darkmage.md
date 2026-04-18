# Species — Dark Mage

## Summary

Zone-4 ranged spellcaster species — high-threat (Tier 3) glass cannon whose silhouette and cast telegraph must read across a crowded encounter so the player can prioritize the right target. Locked via SPEC-SPECIES-DARKMAGE-01 (Phase E). Defines the body plan and stat baseline that the zone-4 boss **Hollow Archon** will inherit (boss spec SPEC-BOSS-HOLLOW-ARCHON-01, Phase F).

## Current State

**Spec status: LOCKED** — authored 2026-04-18 per SPEC-SPECIES-DARKMAGE-01 (Phase E). Drop-table entry already live in `scripts/logic/MonsterDropTable.cs:49` (`material_sig_darkmage`, 7%, Bone thematic). Paired art ticket `ART-DARKMAGE` proposed (not yet authored — no existing ART-* covers zone-4 species redraw); design half locks first so the art half can be authored against a settled silhouette constraint.

Paired with [species-template.md](../species-template.md) (structural contract) and [species-pipeline.md](../../assets/species-pipeline.md) (art pipeline ART-SPEC-02). Neither half ships without the other.

## Design

### 1. Identity

- **Fiction beat:** A robed skeletal mage — once a living scholar — whose skull and bones were saturated with raw magicules during the dungeon's deeper strata, reanimating as a hollow caster that channels purple magicule fire through a staff that is fused to its hand.
- **Role in dungeon ecology:** Immune system — Dark Mages are the mid-late dungeon's active defense layer, standing back from the melee screen and raining spells on intruders who have pushed too deep. They are the first monster in the zone progression whose threat comes from *range* rather than contact, so their role is specifically to make the player stop treating every encounter as a melee brawl.
- **Intended player emotional reaction:** `burst-down-fast` (glass cannon, priority target). Dark Mages hit harder per cast than any Tier 1/2 species and do so at range; ignore them for more than one wind-up and you will eat a spell worth half your HP bar. Their frailty is the payoff — one clean hit drops them, and the feel is "spot the caster, kill the caster, then handle the rest." They are also the first species that teaches the player to read a telegraph and *react* rather than just trade.

---

### 2. Stats

| Field | Floor 3 (Early) | Floor 28 (Mid) | Floor 75 (Deep) |
|---|---|---|---|
| Base HP | 18 | 95 | 420 |
| Base Contact Damage (spell) | 9 | 32 | 95 |
| Base Move Speed (px/s) | 55 | 65 | 75 |
| Base XP Yield | 22 | 85 | 320 |

Notes on the stat shape:
- **Floor 3 is the "Early" reference column per template** but Dark Mages do not spawn until zone 4 (floor 31+). The Early column is filled for template parity and to pin the scaling curve back to a theoretical zone-1 instance; the gameplay-relevant columns are Mid and Deep. Implementation: `Constants.Zones` gates first Dark Mage spawn to zone 4.
- **HP is notably lower than Tier 1 species** (Bat floor-28 HP = 120; Dark Mage floor-28 HP = 95) — intentional: "glass cannon" is defined numerically, not just narratively.
- **Damage is notably higher than any Tier 1/2 species** (Bat floor-28 damage = 14; Goblin floor-28 damage ≈ 18; Dark Mage floor-28 damage = 32 per cast) — roughly 2× the contact-damage Tier-1/2 ceiling. This is the specific number that communicates "new threat class" to the player on first encounter.
- **Move speed is low on purpose** — caster AI does not run down the player; the threat is range + damage, not mobility.
- **XP yield elevated** (roughly 1.75× a comparable Tier-2 species) reflects both the Tier-3 classification and the skill-check payoff of killing them cleanly.

Target TTK (class-appropriate player, level matching floor): **1–2 hits** every floor — Dark Mages die instantly if reached. The feel is "close the distance *or* interrupt the cast, then the fight is already over; fail either and eat a spell that costs you dearly."

Consistency check: §1 reaction (`burst-down-fast`) ↔ §2 TTK (1–2 hits) ↔ §3 AI pattern (`caster` with telegraph) — coherent per template Acceptance Matrix.

---

### 3. AI Pattern

- Pattern: **`caster`** — stationary or slow-drifting; charges spells with an explicit telegraph; interruptible.
- **Telegraph windows:**
  - **Basic bolt cast: 450 ms wind-up** — a purple glow gathers at the staff tip with a small particle flare, then the bolt fires along the line-of-sight to the player. 450 ms is long enough that a player who sees the telegraph can dodge-roll, close the gap, or hard-interrupt with an attack; short enough that a player who ignores the caster pays full price.
  - **AoE slow-field cast: 900 ms wind-up** — used only at low self-HP (below 40%) or when multiple targets are in range. The staff raises, a purple rune forms on the ground under the player, and after the wind-up the tile becomes a brief slow-field. The long 900 ms window is a deliberate "second chance" — if the player missed the 450 ms bolt telegraph, the AoE is the obvious tell that something worse is coming and creates a second opportunity to close and kill.
  - Contact damage on physical touch is N/A — Dark Mages do not melee; if a player closes to melee range the Dark Mage attempts to ranged-kite one step back and cast again at 450 ms. If cornered (no retreat tile), it casts in-place and dies to the next melee hit.
- **Secondary behavior:** If the player enters a configurable "flee radius" (2 tiles) and the Dark Mage has a valid retreat tile, it performs one 1-tile kite-step away then resumes casting. This is the `ranged-kite` "also" reaction — primary is still `caster`, but the kite-step sells the frailty and rewards the player for pressing the advantage.
- **Aggro range:** `chase-always` (current engine default — matches all other species; Dark Mage does not hide or ambush).
- **Leash range:** `never-leash` (current engine default).

Design rationale for telegraph design: The caster is the first species whose combat is *read-react* rather than *position-trade*. The 450 ms basic-cast number is tuned against the player's dodge-roll recovery frames (per [combat.md](../../systems/combat.md)) so that reading the telegraph and dodging is consistently achievable; failing the read is consistently punished. The 900 ms AoE is the teachable-moment telegraph — long enough that even a distracted player notices, so the encounter has a built-in "second chance to learn." Shortening either window below these values would feel cheap; lengthening would feel trivial.

No C# in this section. Reference the existing `scripts/logic/` AI behavior as prior art (current `caster` AI plumbing in `EnemyAi.cs` or successor); keep the spec engine-agnostic.

---

### 4. Drop-Table Hook

- Signature material: **`material_sig_darkmage`**, **7% per kill**. Rate is lower than Tier-1/2 species (10% / 8%) because Tier-3 signature materials are more valuable per-drop — the expected value of signature-drop-per-minute is roughly balanced across tiers, but high-tier species deliver fewer higher-value drops rather than more lower-value ones. See [monster-drops.md](../../systems/monster-drops.md) § Signature Material Drop Rates.
- Thematic generic: **Bone** (60% bone, 20% ore, 20% hide on the generic channel). Rationale: Dark Mages are robed skeletal creatures — the robe shreds and the bones remain; bone is what survives the kill and what a player logically expects to loot. Matches the skeleton-thematic precedent set by `EnemySpecies.Skeleton → Bone` at zone 1.
- Special drop: None at the species level. Hollow Archon boss first-kill drops are defined in SPEC-BOSS-HOLLOW-ARCHON-01 (Phase F) and do not retroactively appear on normal Dark Mage kills.

Matches locked entry in `scripts/logic/MonsterDropTable.cs:49`: `DarkMage → MonsterTier.Three, "material_sig_darkmage", 0.07f, MaterialType.Bone`.

---

### 5. Silhouette Readability Constraint

**Must stand apart from the melee crowd — upright stance, tall pointed hood or skullcap that extends above head-line, raised staff or raised casting-hand visible during wind-up, no shoulders/pauldrons wider than the player sprite's shoulder-line.** Rationale: casters are the highest-priority target in any mixed encounter; if the player cannot spot the Dark Mage at a glance across a group of skeletons, orcs, or wolves, the `burst-down-fast` reaction cannot land and the 450 ms telegraph is wasted — the whole combat-read-react loop collapses into "take unexplained damage from somewhere." The upright-thin-tall silhouette with a raised hand or staff is load-bearing: it is how the player's eye finds the caster inside three seconds of the encounter starting. The purple staff-tip glow during the 450 ms wind-up reinforces this but is not a replacement — silhouette must work even when no cast is charging.

---

### 6. Size / Scale Rule

- Multiplier: **1.00×** (Standard band per template — humanoid).
- Hitbox radius: **12 px** (default `round(12 × 1.00) = 12`).
- Z-offset: **0 px** (ground-bound).

Scale rationale: Dark Mages read as human-scale robed figures. Going Large (1.2×+) would conflict with the "frail glass cannon" fiction and visually bleed the caster into the tank category; going Small (0.8×) would make them disappear inside a pack. Standard is the correct band.

---

### 7. Color-Coding Contract

- Base tint surface: **body only, features exempt**.
- Exempt pixels list:
  - **Eye glow** (two bright purple pixels in the eye sockets — reinforces skeletal caster identity).
  - **Staff-tip glow** (small purple gem/orb at the staff head — load-bearing: this is the pixel cluster that pulses brighter during the 450 ms cast wind-up and signals "cast is charging" at a glance).
  - **Hand-magic FX** (purple particle aura around the casting hand during cast wind-up only — on during the 450 ms telegraph, off otherwise).
- Exempt-pixel implementation note: per bat.md precedent, exempt pixels live on a separate sprite sub-node modulated `Color.White` on top of the tinted body; for the wind-up FX, the hand-aura sub-node visibility is driven by the existing AI cast-state flag so the visual pulse syncs with the telegraph window automatically.

Rationale for exemption: Dark Mage is the archetypal "species defined by a color" case flagged in the template — the purple magicule-fire identity would be erased by level-relative tint at certain level gaps, which would break the silhouette constraint *and* the telegraph read. The three exempt pixel clusters (eyes / staff tip / hand aura) are the minimum set that preserves species identity + cast-telegraph legibility at any level gap.

---

### 8. Art-Spec Pairing

- Paired art ticket: **ART-DARKMAGE** (proposed placeholder — covers Dark Mage sprite redraw under ART-SPEC-02 species pipeline; to be formally opened as an art ticket once this design spec is locked).
- Pairing status: `[ ] art spec locked  [ ] art assets delivered`.

---

## Acceptance Criteria

- [ ] Engineer can implement Dark Mage AI + stats from this spec + `species-template.md` + existing `EnemySpecies.DarkMage` conventions without a clarifying question.
- [ ] Art-lead can compose the PixelLab prompt under ART-DARKMAGE from this spec + ART-SPEC-02's prompt skeleton without a clarifying question.
- [ ] §1 reaction (`burst-down-fast`) matches §2 TTK (1–2 hits) matches §3 AI pattern (`caster` with 450 ms / 900 ms telegraphs).
- [ ] §4 drop-table values match `scripts/logic/MonsterDropTable.cs:49` exactly (`material_sig_darkmage`, `0.07f`, `MaterialType.Bone`, `MonsterTier.Three`).
- [ ] §5 silhouette constraint is the primary PR-review test for the ART-DARKMAGE deliverable (6-up comparison against Skeleton / Goblin / Orc to confirm Dark Mage reads as the caster in any mixed group).
- [ ] §7 color-coding contract preserves purple identity (eyes / staff tip / hand aura) at any level-gap tint; the hand-aura sub-node visibility tracks the cast-state flag so the 450 ms telegraph visual is automatic.

## Implementation Notes

- **Existing C# hooks:** `EnemySpecies.DarkMage` enum value exists; `SpeciesDatabase` hitbox entry exists; `MonsterDropTable` entry exists (`material_sig_darkmage`, 0.07f, Bone, Tier.Three). No new enum, database, or drop-table entries are needed — this spec documents what is already in code.
- **AI telegraph implementation:** the 450 ms and 900 ms wind-up windows are the load-bearing numbers. They should be config-driven constants on the Dark Mage AI node (e.g. `BASIC_CAST_WINDUP_MS = 450`, `AOE_CAST_WINDUP_MS = 900`) so playtesting can tune them without touching logic. AoE trigger condition is "self-HP below 40% OR 2+ targets in range"; this is a gate, not a weight.
- **Staff-tip glow sync:** the hand-magic / staff-tip FX visibility is driven by the AI cast-state flag — no separate animation clock. This keeps the visual telegraph and the gameplay telegraph locked together; tuning one automatically tunes the other.
- **Zone appearance:** Dark Mages spawn from zone 4 (floor 31+) onward per `Constants.Zones`. They do not appear in earlier zones. Hollow Archon (zone 4 floor boss) inherits this species's body plan + base stats per the SPEC-SPECIES-01 → SPEC-BOSS-ART-01 pattern; do not expand this spec to cover boss behavior — boss phase-shifts, unique mechanics, first-kill drops are Phase F in SPEC-BOSS-HOLLOW-ARCHON-01.
- **TTK pressure on Mage class balance:** the 1–2 hits TTK target assumes a class-appropriate Warrior or Ranger. For a Mage meeting a Dark Mage at floor 28, the spell-damage curve per SPEC-MAGIC-COMBAT-FORMULA-01 should still produce 1–2 cast kills on average; if playtesting shows Mages taking 3+ casts to kill their own species-archetype, that's a signal to revisit the INT damage curve, not to adjust this spec's TTK target.

## Open Questions

None — spec is locked.
