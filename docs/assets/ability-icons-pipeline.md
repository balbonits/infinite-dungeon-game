# Ability / Skill / Mastery Icon Pipeline (ART-SPEC-08)

## Summary

Locks the full generation recipe for the **~130 UI icons** covering the skill tree: 27 mastery headers + 12 MVP hotbar abilities + ~90 long-tail abilities across the 103-entry `SkillAbilityDatabase`. Extends the `ICON-UI-64` block from [prompt-templates.md](prompt-templates.md) (ART-SPEC-01 v2, commit `5e9e70f`) — this spec is a category-locked consumer of that block, not a new block definition. All prompts use `create_map_object` with hand-authored 64×64 opaque canvas per `ICON-UI-64`. All prompts follow [prompt-templates.md §11 IP Protection](prompt-templates.md) — zero named-IP invocation, genre-generic pictograph vocabulary, stone-plaque framing only.

Spec strategy: fully lock the **27 mastery mapping table** and the **12 MVP abilities with copy-paste prompts** (the ART-07a deliverable), then publish a reusable per-ability skeleton + pictograph vocabulary so the ~90 long-tail abilities in ART-07b can be authored one-by-one at generation time without re-opening this spec. Implementer picks the best-fit pictograph combo from §4 vocabulary per ability description; they do not invent new pictograph primitives.

Consumed by: **ART-07a** (MVP hotbar: 4 warrior + 4 ranger + 4 mage abilities + 27 mastery icons = 39 icons, unblocks the hotbar + skill-tree UI with real art) and **ART-07b** (long-tail: ~90 remaining ability icons). Engine binding: none — icons are UI-only, no iso anchoring, no Godot sprite offset. Consumed by `Hotbar.cs` + `SkillTreeWindow.cs` via `atlas_manifest.json` lookup per §9.

## Current State

**Spec status: DRAFT (2026-04-17).** Awaiting product-owner review.

`assets/ui/skill-icons/` currently contains **~50 files** that predate this spec: 27 mastery PNGs (mastery_{class}_{name}.png), 12 ability PNGs (ability_{class}_{name}.png), 4 atlas PNGs (atlas_warrior / atlas_ranger / atlas_mage / atlas_innate), and `atlas_manifest.json`. All are slated for **redraw** under ART-07a — existing files were generated under the pre-ART-SPEC-01 pipeline and do not satisfy the `ICON-UI-64` stone-plaque style contract.

`assets/icons/` also contains legacy sheet-based exports (`abilities_icons.png` + .json, `skills_icons.png` + .json, `spells_icons.png` + .json, plus three loose PNGs `skill_arrow.png` / `skill_magic_bolt.png` / `skill_slash.png` and their `.import` siblings). These are **older-pipeline outputs superseded by the per-icon-file + atlas layout in `assets/ui/skill-icons/`** — they are unused by current UI code and are deleted as part of the ART-07a redraw sweep (§11).

[asset-inventory.md §Bucket I](asset-inventory.md) (lines 116–126) is the inventory entry this spec consumes. Bucket I lists the delete-before-regen targets.

## Design

### 1. Icon Family Matrix (~130 icons, 3 categories)

All icons follow this locked matrix. Canvas, palette, pictograph rule, and background contract are inherited from `ICON-UI-64` §Icon background contract in [prompt-templates.md](prompt-templates.md).

| Category | Count | File pattern | Generation ticket |
|---|---|---|---|
| Innate masteries | 4 | `mastery_innate_{name}.png` | ART-07a |
| Warrior masteries | 8 | `mastery_warrior_{name}.png` | ART-07a |
| Ranger masteries | 7 | `mastery_ranger_{name}.png` | ART-07a |
| Mage masteries | 8 | `mastery_mage_{name}.png` | ART-07a |
| MVP abilities (hotbar) | 12 | `ability_{class}_{name}.png` | ART-07a |
| Long-tail abilities | ~90 | `ability_{class}_{name}.png` | ART-07b |
| **Total** | **~129** | — | — |

All icons: **64×64 opaque canvas**, no transparent corners, single-pictograph rule, stone-plaque + gold-trim background. UI-only — no iso anchor, no Godot sprite offset.

### 2. Canvas + Style Contract

Inherited verbatim from [prompt-templates.md `ICON-UI-64`](prompt-templates.md) §Icon background contract:

- **Canvas:** 64 × 64 opaque square. No transparent corners. Icons fill their UI slot completely.
- **Base layer:** dark stone-tile gradient, `#24314a` → `#3c4664`, with a subtle beveled inner border (~2–3 px).
- **Border:** thin gold (`#f5c86b`) trim, 1–2 px wide, clean outer frame — reads as a carved stone plaque.
- **Foreground:** single centered pictograph in near-white (`#ecf0ff`) with mid-gray shading (`#b6bfdb`). One accent color per icon per §3, applied to a focal part of the pictograph (tip / core / outline / pupil).
- **Outline override:** the `single color black outline on characters` clause from the preamble is replaced by "thin gold-trim border on the UI plaque" — this applies to every icon in this pipeline.
- **Style parameter defaults:** `shading = medium shading` (standard), `detailed shading` (mastery headers + ultimate/signature abilities per §3). `detail = medium detail`. `view = high top-down`. `ai_freedom = 500` (batch-consistency — all 130 icons are one cohesive set).
- **Reference aesthetic:** classic genre spell-icon plaque (stone-framed, white pictograph) — no IP replicated, framing and palette are authored fresh for our dungeon-blue UI.

### 3. Per-Category Accent Color Convention

Each icon gets exactly one accent color from this locked table. Accent is applied to the focal part of the pictograph (tip / core / outline / pupil) in the fill-in skeleton.

| Category | Accent | Focal-part convention |
|---|---|---|
| Mastery (all classes + innate) | **Gold (`#f5c86b`)** — passive/tree-header marker | Outline or full-pictograph tint |
| Warrior active ability | **Red (`#ff6f6f`)** — aggressive / physical / combat | Tip (weapon edge) or core (impact burst) |
| Ranger active ability | **Green (`#6bff89`)** — precision / nature / stealth | Tip (arrowhead) or core (eye/focus) |
| Mage active — Fire | **Orange-red (`#ff8a3d`)** | Tip (flame tongue) or core (ember) |
| Mage active — Water / Ice | **Blue-cyan (`#8ed6ff`)** | Core (icicle core) or outline (frost edge) |
| Mage active — Earth | **Brown (`#a07050`)** | Core (stone mass) |
| Mage active — Air / Lightning | **Yellow-white (`#fff7a8`)** | Tip (bolt tip) or outline (spark edge) |
| Mage active — Aether / Light | **Gold-white (`#ffeaa0`)** | Core (radiant center) |
| Mage active — Shadow / Dark | **Purple-black (`#6b3a9a`)** with red (`#ff6f6f`) focal | Purple outline, red focal pupil/core |
| Passive (any class) | **None** — pure white pictograph, no accent | — |
| Ultimate / signature (one per class) | **Gold (`#f5c86b`)** | Full-pictograph tint — reads as prestige |

**Accent selection rule for abilities:** pick accent by **element first** (if ability has an element tag per `SkillAbilityDatabase`), else by **class**. Passives override to "no accent". Ultimate overrides to gold. Mastery always gold regardless of element.

### 4. Pictograph Vocabulary (genre-generic, IP-clean)

**Implementer picks pictographs from this locked set only.** Each ability maps to 1 symbol (preferred) or a composite of 2 symbols (damage+element, defense+element, etc.). No licensed pictographs (no "D2-style flame", no "WoW-style bolt" — those phrases are banned from prompts).

**Damage-dealing primitives:**
- `sword` — straight longsword, vertical or diagonal
- `axe` — single-headed battle-axe, head visible
- `hammer` — warhammer / maul, head visible
- `dagger` — short knife, point visible
- `arrow` — fletched arrow, point up or diagonal
- `orb-projectile` — spherical projectile with motion lines
- `fist` — closed fist knuckles-forward

**Defense primitives:**
- `shield` — kite / round / tower shield silhouette
- `armor` — chestplate silhouette
- `wall` — stone-block wall fragment
- `aura-ring` — concentric circles (protective aura)

**Movement primitives:**
- `running-feet` — paired footprint with motion arc
- `dash-arrow` — forward-pointing chevron with trailing streaks
- `wings` — outspread bird/bat wing pair
- `teleport-swirl` — spiral inward, no flame

**Healing primitives:**
- `heart` — stylized heart
- `cross` — equal-armed cross
- `leaf` — single leaf
- `chalice` — cup with fluid surface

**Utility primitives:**
- `eye` — open eye with pupil
- `hand` — open palm
- `book` — open tome
- `rune-shape` — abstract geometric glyph (no letterforms — triangle / diamond / hexagon / spiral)
- `clock` — circular dial with hands
- `bomb` — sphere with fuse
- `trap` — jagged-jaw snare
- `rope` — coiled rope

**Element tags (composite or standalone):**
- `flame-swirl` — three upward-curling tongues
- `snowflake` — six-arm symmetric snowflake
- `icicle` — downward-pointing spike
- `leaf-wind` — leaf with motion lines
- `lightning-bolt` — zigzag bolt
- `rock` — angular boulder cluster
- `wind-swirl` — horizontal vortex
- `star` — five or six point star (aether/light)
- `skull` — stylized skull (death/curse)
- `droplet` — water/poison droplet
- `feather` — single stylized feather (air/speed)

**Composite rule:** when 2 symbols are needed, place element as focal accent **on** the primary primitive (e.g., flame-swirl *on* the arrowhead tip = Flame Arrow; snowflake *on* the orb-projectile = Frost Bolt). Never side-by-side.

**IP-clean verification:** every pictograph in the vocabulary is a generic silhouette found across all dungeon-crawler genre works. Prompts never cite a specific game's icon style. See §13.

### 5. Per-Mastery Prompt Skeleton (27 icons) — Full Mapping Lock

All 27 masteries use **gold accent** (§3 passive/tree-header marker) and **detailed shading** (mastery headers are prestige-tier icons). Locked pictograph mapping:

| ID | Class | Mastery | Pictograph (from §4) |
|---|---|---|---|
| `mastery_innate_armor` | Innate | Armor | `armor` (chestplate silhouette) |
| `mastery_innate_fortify` | Innate | Fortify | `shield` (round shield) |
| `mastery_innate_haste` | Innate | Haste | `running-feet` |
| `mastery_innate_sense` | Innate | Sense | `eye` |
| `mastery_warrior_bladed` | Warrior | Bladed | Two crossed `sword` |
| `mastery_warrior_blunt` | Warrior | Blunt | `hammer` |
| `mastery_warrior_polearms` | Warrior | Polearms | Single vertical polearm (`sword` elongated — implementer note: describe as "long-hafted spear" in prompt) |
| `mastery_warrior_shields` | Warrior | Shields | `shield` (kite shield) |
| `mastery_warrior_dual_wield` | Warrior | Dual Wield | Two `dagger` crossed |
| `mastery_warrior_unarmed` | Warrior | Unarmed | `fist` |
| `mastery_warrior_discipline` | Warrior | Discipline | `rune-shape` (diamond) inside `aura-ring` |
| `mastery_warrior_intimidation` | Warrior | Intimidation | `skull` |
| `mastery_ranger_bowmanship` | Ranger | Bowmanship | `arrow` on drawn bow silhouette |
| `mastery_ranger_firearms` | Ranger | Firearms | `arrow` with `orb-projectile` trail (IP-clean stand-in — we do not depict firearms per prompt-templates negative list; implementer uses "small crossbow bolt with spark trail") |
| `mastery_ranger_throwing` | Ranger | Throwing | `dagger` with motion arc |
| `mastery_ranger_trapping` | Ranger | Trapping | `trap` |
| `mastery_ranger_cqc` | Ranger | CQC | `fist` with `dagger` |
| `mastery_ranger_sapping` | Ranger | Sapping | `bomb` |
| `mastery_ranger_awareness` | Ranger | Awareness | `eye` with `aura-ring` |
| `mastery_mage_fire` | Mage | Fire | `flame-swirl` |
| `mastery_mage_water` | Mage | Water | `droplet` |
| `mastery_mage_earth` | Mage | Earth | `rock` |
| `mastery_mage_air` | Mage | Air | `wind-swirl` |
| `mastery_mage_aether` | Mage | Aether | `star` |
| `mastery_mage_amplification` | Mage | Amplification | `rune-shape` (triangle) with `aura-ring` |
| `mastery_mage_overcharge` | Mage | Overcharge | `lightning-bolt` in `aura-ring` |
| `mastery_mage_restoration` | Mage | Restoration | `chalice` with `cross` overlay |

**Firearms-mastery IP note:** the `no firearms` clause in the universal negative prompt (§1 preamble) binds the Ranger Firearms mastery to a **stand-in pictograph** (crossbow bolt + spark, not a gun). This is deliberate — the `Firearms` mastery exists in `SkillAbilityDatabase` but our art direction per [prompt-templates.md §11 IP Protection] does not depict modern weapons. Implementer uses the stand-in and does not reopen this decision at generation time.

**Fill-in-the-blank skeleton (inherits `ICON-UI-64`):**

```
[PREAMBLE (verbatim, §1 prompt-templates.md) — with "thin gold-trim border on the UI plaque" substitution]

A 64×64 square UI icon in classic dungeon-crawler spell-icon style. Carved stone-tile background, dark blue-gray (#24314a to #3c4664) gradient with a subtle beveled inner border and a thin gold (#f5c86b) trim frame around the outer edge. Centered pictograph: [MASTERY-SYMBOL from §5 table]. Pictograph rendered in near-white (#ecf0ff) with mid-gray (#b6bfdb) shading, warm gold (#f5c86b) highlight on the outline to mark this as a mastery / skill-tree header icon. No transparent corners — icon fills the full 64×64 square. Prestige-tier detailed shading.

[PALETTE CLAUSE (verbatim, §1c prompt-templates.md)]

[NEGATIVE-PROMPT CLAUSE (verbatim, §1 prompt-templates.md)]
```

Worked examples for three masteries are locked in §8.

### 6. Per-Active-Ability Prompt Skeleton (12 MVP + ~90 long-tail)

**Fill-in-the-blank skeleton:**

```
[PREAMBLE (verbatim, §1 prompt-templates.md) — with "thin gold-trim border on the UI plaque" substitution]

A 64×64 square UI icon in classic dungeon-crawler spell-icon style. Carved stone-tile background, dark blue-gray (#24314a to #3c4664) gradient with a subtle beveled inner border and a thin gold (#f5c86b) trim frame around the outer edge. Centered pictograph: [ABILITY-PICTOGRAPH — 1 primitive or composite from §4 vocabulary]. Pictograph rendered in near-white (#ecf0ff) with mid-gray (#b6bfdb) shading, [ACCENT-COLOR from §3] highlight on the [FOCAL-PART — tip / core / outline / pupil]. No transparent corners — icon fills the full 64×64 square.

[PALETTE CLAUSE (verbatim, §1c prompt-templates.md)]

[NEGATIVE-PROMPT CLAUSE (verbatim, §1 prompt-templates.md)]
```

**MVP (12 abilities) — locked mapping:**

| ID | Class | Ability | Pictograph | Accent | Focal |
|---|---|---|---|---|---|
| `ability_warrior_slash` | Warrior | Slash | `sword` diagonal with motion arc | Red | Tip |
| `ability_warrior_block` | Warrior | Block | `shield` (kite) frontal | Red | Core (shield boss) |
| `ability_warrior_parry` | Warrior | Parry | `sword` deflecting an incoming `arrow` | Red | Tip (sword edge) |
| `ability_warrior_blood_lust` | Warrior | Blood Lust | `fist` with `droplet` (red blood droplet) | Red | Core |
| `ability_ranger_dead_eye` | Ranger | Dead Eye | `eye` with `arrow` crosshair overlay | Green | Pupil |
| `ability_ranger_disengage` | Ranger | Disengage | `dash-arrow` backward (opposite direction) | Green | Tip |
| `ability_ranger_flame_arrow` | Ranger | Flame Arrow | `arrow` with `flame-swirl` on tip | Orange-red (Fire overrides Ranger green per §3 element-first rule) | Tip |
| `ability_ranger_smoke_bomb` | Ranger | Smoke Bomb | `bomb` with `wind-swirl` plume | Green | Core (fuse spark) |
| `ability_mage_fireball` | Mage | Fireball | `flame-swirl` | Orange-red | Tip |
| `ability_mage_frost_bolt` | Mage | Frost Bolt | `orb-projectile` with `snowflake` overlay | Blue-cyan | Core |
| `ability_mage_lightning` | Mage | Lightning | `lightning-bolt` | Yellow-white | Tip |
| `ability_mage_mend` | Mage | Mend | `heart` with `cross` overlay | Green (healing) | Core |

Full copy-paste prompts for these 12 icons are in §7.

**Long-tail (~90 abilities) — extension protocol:**

1. Open `scripts/logic/SkillAbilityDatabase.cs` (or `docs/systems/skills.md`), locate the ability's `name` + `description` + mastery/element tags.
2. Classify: damage / defense / movement / healing / utility (pick category).
3. Pick **primary pictograph** from §4 vocabulary matching the category (e.g., damage ability → pick from damage primitives).
4. If the ability has an element tag (fire / water / air / earth / aether / shadow / poison), pick an **element tag** from §4 and composite it as focal accent on the primary per §4 composite rule.
5. Pick **accent color** from §3: element first, else class. Passive → no accent. Ultimate → gold.
6. Determine **focal-part** (tip for weapons/bolts/arrows, core for orbs/hearts/shields, outline for auras, pupil for eyes).
7. Fill the skeleton with the four slots and generate.
8. File output as `ability_{class}_{snake_case_name}.png` in `assets/ui/skill-icons/`.
9. Re-run atlas compositor script (§9) after batch completion.

Implementer does NOT invent new pictographs. If an ability cannot reduce to the §4 vocabulary, flag it in the generation PR for spec review — do not ship a one-off pictograph.

### 7. Full Copy-Paste Prompts — 12 MVP Abilities (ART-07a)

Each prompt is self-contained. All 12 share the same preamble + palette + negative clauses; only the pictograph / accent / focal lines differ.

**Shared blocks (verbatim, appearing in every prompt):**

*Preamble (with icon outline override):*
> True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, thin gold-trim border on the UI plaque, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

*Palette clause:*
> Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

*Negative-prompt clause:*
> No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing.

---

**#1 — `ability_warrior_slash` (Warrior / Slash):**
> [Preamble]
>
> A 64×64 square UI icon in classic dungeon-crawler spell-icon style. Carved stone-tile background, dark blue-gray (#24314a to #3c4664) gradient with a subtle beveled inner border and a thin gold (#f5c86b) trim frame around the outer edge. Centered pictograph: a diagonal longsword with a crescent motion arc trailing from the blade, stylized and compact. Pictograph rendered in near-white (#ecf0ff) with mid-gray (#b6bfdb) shading, red (#ff6f6f) highlight on the sword tip and along the motion arc. No transparent corners — icon fills the full 64×64 square.
>
> [Palette] [Negatives]

---

**#2 — `ability_warrior_block` (Warrior / Block):**
> [Preamble]
>
> A 64×64 square UI icon in classic dungeon-crawler spell-icon style. Carved stone-tile background, dark blue-gray (#24314a to #3c4664) gradient with a subtle beveled inner border and a thin gold (#f5c86b) trim frame around the outer edge. Centered pictograph: a frontal kite shield silhouette, bold and grounded. Pictograph rendered in near-white (#ecf0ff) with mid-gray (#b6bfdb) shading, red (#ff6f6f) highlight on the central shield boss. No transparent corners — icon fills the full 64×64 square.
>
> [Palette] [Negatives]

---

**#3 — `ability_warrior_parry` (Warrior / Parry):**
> [Preamble]
>
> A 64×64 square UI icon in classic dungeon-crawler spell-icon style. Carved stone-tile background, dark blue-gray (#24314a to #3c4664) gradient with a subtle beveled inner border and a thin gold (#f5c86b) trim frame around the outer edge. Centered pictograph: a longsword held diagonally deflecting an incoming arrow which breaks at the point of contact, compact and readable. Pictograph rendered in near-white (#ecf0ff) with mid-gray (#b6bfdb) shading, red (#ff6f6f) highlight on the sword edge at the deflection point. No transparent corners — icon fills the full 64×64 square.
>
> [Palette] [Negatives]

---

**#4 — `ability_warrior_blood_lust` (Warrior / Blood Lust):**
> [Preamble]
>
> A 64×64 square UI icon in classic dungeon-crawler spell-icon style. Carved stone-tile background, dark blue-gray (#24314a to #3c4664) gradient with a subtle beveled inner border and a thin gold (#f5c86b) trim frame around the outer edge. Centered pictograph: a clenched fist knuckles-forward with a single large droplet (blood) falling from below the wrist. Pictograph rendered in near-white (#ecf0ff) with mid-gray (#b6bfdb) shading, red (#ff6f6f) highlight on the droplet and a faint red wash on the fist core. No transparent corners — icon fills the full 64×64 square.
>
> [Palette] [Negatives]

---

**#5 — `ability_ranger_dead_eye` (Ranger / Dead Eye):**
> [Preamble]
>
> A 64×64 square UI icon in classic dungeon-crawler spell-icon style. Carved stone-tile background, dark blue-gray (#24314a to #3c4664) gradient with a subtle beveled inner border and a thin gold (#f5c86b) trim frame around the outer edge. Centered pictograph: an open eye with a crosshair / arrow overlay aligning on the pupil, focused and precise. Pictograph rendered in near-white (#ecf0ff) with mid-gray (#b6bfdb) shading, green (#6bff89) highlight on the pupil at the crosshair center. No transparent corners — icon fills the full 64×64 square.
>
> [Palette] [Negatives]

---

**#6 — `ability_ranger_disengage` (Ranger / Disengage):**
> [Preamble]
>
> A 64×64 square UI icon in classic dungeon-crawler spell-icon style. Carved stone-tile background, dark blue-gray (#24314a to #3c4664) gradient with a subtle beveled inner border and a thin gold (#f5c86b) trim frame around the outer edge. Centered pictograph: a backward-pointing chevron dash-arrow with trailing motion streaks, reading as "retreat" not "advance". Pictograph rendered in near-white (#ecf0ff) with mid-gray (#b6bfdb) shading, green (#6bff89) highlight on the arrow tip. No transparent corners — icon fills the full 64×64 square.
>
> [Palette] [Negatives]

---

**#7 — `ability_ranger_flame_arrow` (Ranger / Flame Arrow):**
> [Preamble]
>
> A 64×64 square UI icon in classic dungeon-crawler spell-icon style. Carved stone-tile background, dark blue-gray (#24314a to #3c4664) gradient with a subtle beveled inner border and a thin gold (#f5c86b) trim frame around the outer edge. Centered pictograph: a fletched arrow angled diagonally with a compact flame swirl wrapped around the arrowhead. Pictograph rendered in near-white (#ecf0ff) with mid-gray (#b6bfdb) shading, orange-red (#ff8a3d) highlight on the flame tongues at the arrow tip. No transparent corners — icon fills the full 64×64 square.
>
> [Palette] [Negatives]

---

**#8 — `ability_ranger_smoke_bomb` (Ranger / Smoke Bomb):**
> [Preamble]
>
> A 64×64 square UI icon in classic dungeon-crawler spell-icon style. Carved stone-tile background, dark blue-gray (#24314a to #3c4664) gradient with a subtle beveled inner border and a thin gold (#f5c86b) trim frame around the outer edge. Centered pictograph: a spherical bomb with a lit fuse on top and a horizontal wind-swirl plume rising behind it. Pictograph rendered in near-white (#ecf0ff) with mid-gray (#b6bfdb) shading, green (#6bff89) highlight on the fuse spark at the core. No transparent corners — icon fills the full 64×64 square.
>
> [Palette] [Negatives]

---

**#9 — `ability_mage_fireball` (Mage / Fireball):**
> [Preamble]
>
> A 64×64 square UI icon in classic dungeon-crawler spell-icon style. Carved stone-tile background, dark blue-gray (#24314a to #3c4664) gradient with a subtle beveled inner border and a thin gold (#f5c86b) trim frame around the outer edge. Centered pictograph: a flame swirl with three upward-curling tongues, stylized and compact. Pictograph rendered in near-white (#ecf0ff) with mid-gray (#b6bfdb) shading, orange-red (#ff8a3d) highlight on the tip of each flame tongue. No transparent corners — icon fills the full 64×64 square.
>
> [Palette] [Negatives]

---

**#10 — `ability_mage_frost_bolt` (Mage / Frost Bolt):**
> [Preamble]
>
> A 64×64 square UI icon in classic dungeon-crawler spell-icon style. Carved stone-tile background, dark blue-gray (#24314a to #3c4664) gradient with a subtle beveled inner border and a thin gold (#f5c86b) trim frame around the outer edge. Centered pictograph: a spherical orb projectile with a six-arm snowflake overlay centered on the orb, with short motion lines behind. Pictograph rendered in near-white (#ecf0ff) with mid-gray (#b6bfdb) shading, blue-cyan (#8ed6ff) highlight on the snowflake core. No transparent corners — icon fills the full 64×64 square.
>
> [Palette] [Negatives]

---

**#11 — `ability_mage_lightning` (Mage / Lightning):**
> [Preamble]
>
> A 64×64 square UI icon in classic dungeon-crawler spell-icon style. Carved stone-tile background, dark blue-gray (#24314a to #3c4664) gradient with a subtle beveled inner border and a thin gold (#f5c86b) trim frame around the outer edge. Centered pictograph: a single zigzag lightning bolt descending diagonally, bold and compact. Pictograph rendered in near-white (#ecf0ff) with mid-gray (#b6bfdb) shading, yellow-white (#fff7a8) highlight on the bolt tip and along the sparking edges. No transparent corners — icon fills the full 64×64 square.
>
> [Palette] [Negatives]

---

**#12 — `ability_mage_mend` (Mage / Mend):**
> [Preamble]
>
> A 64×64 square UI icon in classic dungeon-crawler spell-icon style. Carved stone-tile background, dark blue-gray (#24314a to #3c4664) gradient with a subtle beveled inner border and a thin gold (#f5c86b) trim frame around the outer edge. Centered pictograph: a stylized heart with an equal-armed cross overlaid at the heart's center. Pictograph rendered in near-white (#ecf0ff) with mid-gray (#b6bfdb) shading, green (#6bff89) highlight on the cross arms at the heart core. No transparent corners — icon fills the full 64×64 square.
>
> [Palette] [Negatives]

### 8. Worked Examples — 3 Masteries (proves the §5 skeleton)

**Example A — `mastery_warrior_bladed` (Warrior / Bladed):**
> [Preamble] (with thin gold-trim icon override)
>
> A 64×64 square UI icon in classic dungeon-crawler spell-icon style. Carved stone-tile background, dark blue-gray (#24314a to #3c4664) gradient with a subtle beveled inner border and a thin gold (#f5c86b) trim frame around the outer edge. Centered pictograph: two crossed longswords forming an X, blades up, hilts down, symmetrical and heraldic. Pictograph rendered in near-white (#ecf0ff) with mid-gray (#b6bfdb) shading, warm gold (#f5c86b) highlight on the sword outlines to mark this as a mastery / skill-tree header icon. No transparent corners — icon fills the full 64×64 square. Prestige-tier detailed shading.
>
> [Palette] [Negatives]

**Example B — `mastery_ranger_bowmanship` (Ranger / Bowmanship):**
> [Preamble] (with thin gold-trim icon override)
>
> A 64×64 square UI icon in classic dungeon-crawler spell-icon style. Carved stone-tile background, dark blue-gray (#24314a to #3c4664) gradient with a subtle beveled inner border and a thin gold (#f5c86b) trim frame around the outer edge. Centered pictograph: a drawn bow with a nocked fletched arrow, bowstring pulled back, arrow pointing right, compact and readable. Pictograph rendered in near-white (#ecf0ff) with mid-gray (#b6bfdb) shading, warm gold (#f5c86b) highlight on the bow outline and the arrow tip. No transparent corners — icon fills the full 64×64 square. Prestige-tier detailed shading.
>
> [Palette] [Negatives]

**Example C — `mastery_mage_fire` (Mage / Fire):**
> [Preamble] (with thin gold-trim icon override)
>
> A 64×64 square UI icon in classic dungeon-crawler spell-icon style. Carved stone-tile background, dark blue-gray (#24314a to #3c4664) gradient with a subtle beveled inner border and a thin gold (#f5c86b) trim frame around the outer edge. Centered pictograph: a flame swirl with three upward-curling tongues, stylized, bold, heraldic (this is the category header, not the Fireball ability — slightly larger and more ornate). Pictograph rendered in near-white (#ecf0ff) with mid-gray (#b6bfdb) shading, warm gold (#f5c86b) highlight on the flame outline (mastery gold overrides the element accent per §3). No transparent corners — icon fills the full 64×64 square. Prestige-tier detailed shading.
>
> [Palette] [Negatives]

**Mastery-vs-ability distinction rule** (from Example C): when a mastery header and an ability share a pictograph root (e.g., Fire mastery + Fireball ability both use `flame-swirl`), the mastery uses **gold accent + detailed shading + heraldic framing** and the ability uses **element accent + medium shading + compact framing**. The icons read differently in the UI grid even with the same symbol.

### 9. Atlas Considerations

**Per-class atlas layout.** Each class gets a single composite atlas PNG packing its mastery + ability icons into a grid for efficient Godot atlas loading:

| Atlas | Cells | Layout | Covers |
|---|---|---|---|
| `atlas_innate.png` | 4 | 4×1 (256 × 64) | 4 innate masteries |
| `atlas_warrior.png` | 8 masteries + ~N abilities | 4-col grid, rows grow with ability count | Warrior masteries + all warrior abilities |
| `atlas_ranger.png` | 7 masteries + ~N abilities | 4-col grid | Ranger masteries + all ranger abilities |
| `atlas_mage.png` | 8 masteries + ~N abilities | 4-col grid | Mage masteries + all mage abilities |

**Cell size:** 64 × 64 (matches the `ICON-UI-64` canvas). **Gap:** 0 (tight pack for `AtlasTexture` region offsets). **Columns:** 4 (existing `atlas_manifest.json` convention — preserved). **Row count grows** as abilities are added in ART-07b.

**Atlas compositing recipe.** PixelLab generates **individual 64×64 PNGs** per icon (one generation per prompt, `create_map_object` at 64×64). A small post-generation script — `scripts/tools/pack_skill_atlas.py` (to be authored in ART-07a, not part of this spec) — reads every `mastery_{class}_*.png` and `ability_{class}_*.png` for a class, packs them into a grid (masteries first, abilities after), writes the atlas PNG, and regenerates `atlas_manifest.json` entries with `{atlas, col, row, x, y, w, h}` per icon. The script is idempotent — rerun it whenever new icons are added.

**Manifest schema** (existing, preserved — see current `assets/ui/skill-icons/atlas_manifest.json`):

```json
{
  "cellSize": 64,
  "cols": 4,
  "gap": 0,
  "icons": {
    "{icon_id}": {
      "atlas": "atlas_{class}",
      "col": <int>, "row": <int>,
      "x": <int>, "y": <int>,
      "w": 64, "h": 64
    }
  }
}
```

Godot UI code consumes this manifest to construct `AtlasTexture` regions per icon ID. No per-icon `.import` files needed — only the atlas PNGs are imported.

### 10. Asset Directory Layout

```
assets/ui/skill-icons/
├── mastery_innate_{armor,fortify,haste,sense}.png        # 4 files
├── mastery_warrior_{bladed,blunt,polearms,shields,
│                   dual_wield,unarmed,discipline,
│                   intimidation}.png                      # 8 files
├── mastery_ranger_{bowmanship,firearms,throwing,
│                  trapping,cqc,sapping,awareness}.png     # 7 files
├── mastery_mage_{fire,water,earth,air,aether,
│                amplification,overcharge,restoration}.png # 8 files
├── ability_warrior_{slash,block,parry,blood_lust,…}.png   # 4 MVP + N long-tail
├── ability_ranger_{dead_eye,disengage,flame_arrow,
│                   smoke_bomb,…}.png                      # 4 MVP + N long-tail
├── ability_mage_{fireball,frost_bolt,lightning,mend,…}.png# 4 MVP + N long-tail
├── atlas_innate.png
├── atlas_warrior.png
├── atlas_ranger.png
├── atlas_mage.png
└── atlas_manifest.json
```

File naming convention: snake_case lowercase, underscores between words, matches `SkillAbilityDatabase` ability IDs verbatim. PNG only — no `.svg`, no `.webp`. Individual PNGs are retained alongside the atlas so the pack script can regenerate atlases idempotently.

### 11. Delete-Before-Regen

Per [asset-inventory.md §Bucket I](asset-inventory.md), the ART-07a redraw deletes these files **before** regeneration (they were generated under the pre-ART-SPEC-01 pipeline and do not satisfy the `ICON-UI-64` stone-plaque contract):

**`assets/ui/skill-icons/` — delete all:**
- All 27 `mastery_*.png` files (existing — pre-style-contract)
- All 12 `ability_*.png` files (existing — pre-style-contract)
- All 4 `atlas_*.png` files + `atlas_manifest.json` (regenerated by pack script after icon regen)

**`assets/icons/` — delete entire directory (legacy older-pipeline exports, unused by current UI code):**
- `abilities_icons.png` + `abilities_icons.json`
- `skills_icons.png` + `skills_icons.json`
- `spells_icons.png` + `spells_icons.json`
- `skill_arrow.png` + `.import`
- `skill_magic_bolt.png` + `.import`
- `skill_slash.png` + `.import`

**Verification after delete:** `ls assets/ui/skill-icons/` returns empty; `ls assets/icons/` returns no such directory; build succeeds. Then run generation sweep per ART-07a.

**Total deletion count:** ~50 files (27 + 12 + 4 + 1 JSON in `skill-icons/`) + 12 files (3 PNGs + 3 JSONs + 3 PNGs + 3 imports in `icons/`) = **~62 files deleted** before regen.

### 12. Thumbnail-Readability Gate (PR Checklist)

Every ART-07a / ART-07b PR must satisfy:

- [ ] Each icon reads its **category** at 32×32 (hotbar size) — open the PNG at half-size in a viewer; pictograph silhouette must be distinguishable.
- [ ] Each icon is **distinguishable from its atlas neighbors** at 32×32 — pull up the class atlas PNG and verify no two cells blur into the same silhouette (e.g., Slash vs. Parry must not read as the same icon at thumbnail scale).
- [ ] Accent color per §3 is the correct one for the icon's category / element.
- [ ] Pictograph is from §4 vocabulary (no one-off symbols).
- [ ] No letterforms / numbers / logos present (negative-prompt verification).
- [ ] Canvas is exactly 64×64, opaque, no transparent corners.
- [ ] Filename matches `{category}_{class}_{snake_case_name}.png` convention.
- [ ] `atlas_manifest.json` regenerated by pack script; manifest entry present for the new icon.

PR reviewer (Copilot or product-owner) can reject on any checklist failure — re-gen with a refined prompt, do not ship partial compliance.

### 13. IP-Clean Verification

This pipeline must not replicate any named IP's icon style. Hard rules:

- **Pictograph vocabulary (§4) is genre-generic.** Every symbol (sword, shield, flame-swirl, snowflake, lightning-bolt, eye, skull, etc.) appears across all dungeon-crawler genre works — none are IP-specific.
- **Prompts never cite a specific game.** Phrases banned from every prompt: "D2-style", "WoW-style", "Diablo icon", "Path of Exile", "Torchlight style", "Last Epoch icon", "Grim Dawn icon". Replace with "classic dungeon-crawler spell-icon style" per the skeleton.
- **Palette is authored** (dungeon-blue `#24314a` / `#3c4664` plaque + gold `#f5c86b` trim) — not copied. Classic genre references often use warm-olive stone plaques; we deliberately substitute dungeon-blue for UI consistency with our game palette (see `ICON-UI-64` background contract note in prompt-templates.md).
- **No firearms** (universal negative-prompt clause). The Ranger Firearms mastery uses a crossbow-bolt-with-spark stand-in per §5.
- **No text / letters / numbers / logos** — accent focal-parts are on pictograph features (tip / core / outline / pupil), never on a letterform.

Verification: git-grep the icon generation PR for banned phrases before merge. No banned phrase should appear in any prompt or the manifest.

### 14. Acceptance Criteria

This spec is complete when all of the following are true:

- [ ] 27 mastery icons are mappable from the §5 table — every mastery in `SkillAbilityDatabase` has a locked pictograph assignment.
- [ ] 12 MVP abilities have full copy-paste prompts in §7 (one prompt per icon, self-contained with preamble + palette + negatives).
- [ ] ~90 long-tail abilities are authorable from the §6 skeleton + §4 vocabulary + §3 accent table without further spec work (extension protocol documented).
- [ ] Atlas compositing recipe documented in §9 (pack script ownership noted; manifest schema preserved).
- [ ] Delete-before-regen list in §11 enumerates every file slated for deletion.
- [ ] Thumbnail-readability gate in §12 is a PR-checkbox list.
- [ ] Zero IP violations — §13 enumerates banned phrases and the verification step.

### 15. Open Questions

*(empty at lock time)*
