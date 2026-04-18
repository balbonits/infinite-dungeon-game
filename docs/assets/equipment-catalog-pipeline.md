# Equipment Catalog Pipeline — Inventory UI Icons (ART-SPEC-06)

## Summary

Locks the generation recipe for the **259 inventory-UI icons** that represent every base item in [docs/inventory/item-catalog.md](../inventory/item-catalog.md). Extends the `ICON-UI-64` block from [prompt-templates.md](prompt-templates.md) (ART-SPEC-01 v2, commit `5e9e70f`) and introduces a sibling block **`ITEM-ICON-64`** for detailed-illustration items (armor / weapons / consumables / materials) that cannot reduce to a single pictograph the way ability icons do.

**This ticket produces prompt skeletons, not 259 individual prompts.** Each of 7 categories gets one fill-in-the-blank template plus one worked example. An implementer generates 259 sprites by filling a template 259 times — each fill is one PixelLab job.

**Equipment icons are UI-only.** Per [SPEC-PC-ART-01](../assets/player-class-pipeline.md) locked decision: "single default sprite per class; equipment changes surface via inventory paperdoll UI only, never on world sprite." These icons appear only in `BackpackWindow`, paperdoll panels, shop listings, tooltip previews — **NOT in the dungeon scene.** No iso perspective. No walking rotations. No directional anchor contract. 64×64 flat UI.

Consumed by: **ART-03** (armor × 75) · **ART-04** (weapon + offhand × 70) · **ART-05** (quivers × 9) · **ART-06** (neck + ring × 55) · and the consumable + material tail (50 icons, rolled into ART-03..06's follow-through per `dev-tracker.md`).

Preceded by: [asset-inventory.md §Bucket G](asset-inventory.md) — currently empty. First regeneration creates; no delete-sweep needed on the first pass. Subsequent regenerations follow delete-first.

Engine binding: none. Icons are `Texture2D` references loaded into `TextureRect` / `ItemSlot` nodes in UI scenes. Asset path is the only engine contract.

## Current State

**Spec status: DRAFT.** Awaiting product-owner review.

No equipment icons exist in the repo today. [asset-inventory.md §Bucket G](asset-inventory.md) is an empty placeholder. Placeholders in the current BackpackWindow are colored rectangles; icon drop-in is a pure additive change.

ITEM-01 ships the data layer (IDs, names, affinities, tier) independent of this spec. ART-03..06 ship visuals on top. A backpack with data-layer items but no icons renders as colored rectangles; a backpack with both renders as the designed UI.

## Design

### 1. Canvas + Style Contract

Two icon-family style variants, both 64×64, both flat UI, both opaque stone-plaque backgrounds inherited from `ICON-UI-64`:

#### `ITEM-ICON-64` (NEW — sibling block to `ICON-UI-64`)

**Purpose.** Detailed-illustration icons for concrete physical items — armor, weapons, offhands, quivers, consumables, materials. An item like "Mega Sword" or "Large Health Potion" is a real object the player sees in their backpack, not a spell pictograph. The illustration IS the icon; pictographic reduction would lose the item's identity.

**Mandatory PixelLab params:**

| Param | Value |
|-------|-------|
| Tool | `create_map_object` |
| `width` × `height` | **64 × 64** |
| `view` | `high top-down` (flat presentation) |
| `outline` | `single color black outline` (item silhouette — differs from `ICON-UI-64`'s thin gold-trim border, which becomes the plaque frame) |
| `shading` | `medium shading` (standard); `detailed shading` (T5 hero items — Dragonite ladder, Archmage tier) |
| `detail` | `medium detail` (standard); `high detail` (T5) |
| `ai_freedom` | `500` (batch-consistency runs — every armor ladder of 5, every weapon family of 5) |

**Canvas contract:**
- 64×64 opaque canvas, **no transparent corners**.
- Base layer: same stone-tile `#24314a` → `#3c4664` gradient plaque as `ICON-UI-64`, but at **lower contrast** (~70% of `ICON-UI-64`'s) — plaque must recede so the item silhouette reads first.
- Border: same thin gold (`#f5c86b`) trim as `ICON-UI-64`, 1–2 px wide.
- Foreground: item illustration centered, filling **~70%** of canvas (leaves ~4–8 px margin on each side inside the gold trim). Item gets its own black outline; the plaque does not.
- No transparent background — icon slot is fully filled.

#### `ICON-UI-64` (INHERITED from ART-SPEC-01, unused in this spec)

Original `ICON-UI-64` (pictograph over stone plaque) is **not used** for any equipment category. All 259 items use `ITEM-ICON-64`. See §1.1 for rationale.

### 1.1 Category → Style assignment

| Category | Count | Style | Rationale |
|---|---|---|---|
| Armor (Head/Body/Arms/Legs/Feet) | 75 | `ITEM-ICON-64` | Class voice lives in silhouette (plate vs leather vs robes). Pictograph would erase class differentiation. |
| Main-hand weapons | 40 | `ITEM-ICON-64` | Weapon archetype (sword/axe/bow/staff) is the identity beat — needs real illustration. |
| Off-hand weapons | 30 | `ITEM-ICON-64` | Same as main-hand. Shield vs knife vs spellbook are silhouette-driven. |
| Quivers | 9 | `ITEM-ICON-64` | Imbue effect reads as colored fletching on a real quiver silhouette. |
| Neck (chains) | 15 | `ITEM-ICON-64` | Pendant shape + stat color is the identity. Small but still illustrative. |
| Ring | 40 | `ITEM-ICON-64` | Band metal + stone shape + focus color. See §1.2 for ring-specific plaque-reduction note. |
| Consumables | 28 | `ITEM-ICON-64` | Potion bottle vs scroll vs bread is illustrative. |
| Materials | 22 | `ITEM-ICON-64` | Ore chunk vs bone vs hide pile is illustrative. |

**Why no `ICON-UI-64` pictograph variant for any of the 259.** Every equipment item is a concrete object, not an action/effect. Pictographs are for *verbs* (fireball, heavy strike, dodge); illustrations are for *nouns* (sword, potion, ring). Mixing both styles in one backpack grid would read as visual inconsistency at thumbnail scale. **Locked decision.**

### 1.2 Ring plaque reduction

Rings at 64×64 risk reading as "a tiny ring on a background" — most of the canvas is plaque, little is ring. Per-category fine-tune: for rings, reduce plaque contrast by a further ~15% and allow the ring illustration to fill **~80%** of canvas (not 70%). The band-and-stone silhouette needs the extra headroom. All other categories stay at 70%.

### 2. Per-Tier Visual Convention (5-Metal Ladder)

The metal ladder (Iron / Steel / Mithril / Orichalcum / Dragonite) is the unified ID-level fantasy for Ore materials, Neck chains, Rings, and all Warrior armor/weapons. Per-tier palette locks are **primary-material sub-clamps** that override the normal `#24314a`/`#3c4664` palette clause for the item silhouette itself (the plaque background stays neutral).

| Tier | Metal name | Primary palette | Shading beat | Readability cue at 32×32 |
|---|---|---|---|---|
| 1 | Iron | Dull gray `#6a6a6a` base, rust-brown `#7a4a2a` weathering at edges | Flat shading, no highlight | "This looks beaten up." |
| 2 | Steel | Polished silver-gray `#9ea4b4` base, cool blue highlight `#b8c8e0` | Medium shading, one crisp highlight | "This looks new." |
| 3 | Mithril | Pale blue-silver `#c8d8ea` base, subtle shimmer `#e4ecf8` highlight | Detailed shading, two-tone shimmer | "This glows faintly." |
| 4 | Orichalcum | Warm gold `#d4a045` base, bronze undertone `#8a5a2a`, green-patina accent `#5a7a4a` on lowlights | Detailed shading, warm-to-cool transition | "This is gold but weird." |
| 5 | Dragonite | Iridescent purple-to-green shift (`#7a4aa8` → `#3a8a5a`), deep shadow `#2a1a3a`, pearl highlight `#e8d8f4` | Highly detailed, three-band iridescence | "This is otherworldly." |

**Every armor and weapon icon pulls its primary metal palette from this table.** The class voice (Warrior plate / Ranger leather / Mage robes) does **not** override metal — a Mithril Helmet and a Mithril Robe both carry the Mithril palette on their metallic trim/clasps even though the robe's main fabric is cloth. For Ranger leather + Mage cloth items, the metal palette applies only to buckles/clasps/embroidery-trim — the main material is leather brown / cloth purple-blue per §4.

**Non-metal tier items** (potions, scrolls, bones, hides, rings with gem-focus) follow per-category palette rules per §4 — the metal ladder does not apply to every T1-T5 ladder in the catalog, only to the ones that name the metals (Warrior armor/weapons, Neck chains, Rings' band metal).

**Readability gate** (§8): the 5 metals must be distinguishable from each other at 64×64 full size AND at 32×32 thumbnail. The palette table is chosen with thumbnail distinguishability in mind — Iron's brown weathering, Steel's blue highlight, Mithril's pale shimmer, Orichalcum's gold, Dragonite's purple are each one clear hue that survives thumbnail downsampling.

### 3. Class-Voice Primary Material Palette (Armor)

Orthogonal to §2. For armor, class voice dictates the **main material** and the metal ladder dictates the trim:

| Class | Main material | Main palette | Silhouette cue |
|---|---|---|---|
| Warrior | Plate metal | = tier metal (§2 primary palette applies to the whole piece) | Thick, chunky, horned/ridged. Overlapping plates visible. |
| Ranger | Leather | Brown base `#5a3a22` with tier-metal buckles and studs | Fitted, supple, visible stitching. Buckles/straps in tier metal. |
| Mage | Cloth / velvet | Purple-blue base `#3a3a6a` with tier-metal trim and embroidery | Flowing, layered, hemmed. Trim in tier metal. |

**How this combines with §2.** A "Mega Gauntlets" (T5 Warrior arms) pulls Dragonite iridescence as the main palette (Warrior = plate = all metal). A "Top-Shelf Braces" (T5 Ranger arms) pulls brown leather as the main palette, with Dragonite-iridescent buckles. A "Archmage's Bangles" (T5 Mage arms) pulls purple-blue cloth/velvet as the main palette, with Dragonite-iridescent embroidery.

Class accent dots (§5) are layered on top of this.

### 4. Per-Category Prompt Skeletons

All prompts inherit the `ITEM-ICON-64` style preamble — same universal preamble as `ICON-UI-64` with the outline clause swapped for the hybrid rule ("black outline on item silhouette, thin gold-trim border around the outer plaque"). All prompts end with the palette clause and negative-prompt clause verbatim from [prompt-templates.md §1c + §1](prompt-templates.md).

#### 4.1 Armor Skeleton (75 sprites)

Slots: Head / Body / Arms / Legs / Feet × Tier 1–5 × Warrior / Ranger / Mage.

```
[PREAMBLE — verbatim, §1 of prompt-templates.md, outline clause swapped per §1 above]

A 64×64 square inventory icon of a [PIECE-SILHOUETTE] in classic dungeon-crawler item-icon style. Carved stone-tile background, dark blue-gray (#24314a to #3c4664) gradient at reduced contrast, thin gold (#f5c86b) trim frame around the outer edge. Item centered, occupying ~70% of canvas, with a single-color black outline. [CLASS-MATERIAL — thick overlapping plate metal / fitted brown leather with buckles / flowing purple-blue cloth with embroidered trim]. Primary material palette follows the [TIER-PALETTE] metal ladder — [TIER-PALETTE-SPECIFIC — e.g., "pale blue-silver with subtle shimmer" for Mithril]. Small [CLASS-ACCENT — blue dot / green knot / gold rune-glyph (shape-only)] marker on the [ACCENT-LOCATION — brow / chest center / wrist cuff / knee / boot tongue]. No transparent corners — icon fills the full 64×64 square.

[PALETTE CLAUSE — verbatim, §1c]
[NEGATIVE-PROMPT CLAUSE — verbatim, §1] No scene background, no character wearing the item, no action pose — item only, item-icon plaque only.
```

**Slot fills:**
- `[PIECE-SILHOUETTE]`: helmet with horns / chestplate with pauldron shoulders / bracers (pair of forearm guards, front-facing) / greaves (leg plates, front-facing) / boots (pair, front-facing toe-up).
- `[CLASS-MATERIAL]`: Warrior=plate, Ranger=leather, Mage=cloth (§3 table full text).
- `[TIER-PALETTE]`: Iron / Steel / Mithril / Orichalcum / Dragonite (§2 table).
- `[CLASS-ACCENT]`: Warrior = blue dot `#8ed6ff`; Ranger = green knot `#6bff89`; Mage = gold rune-glyph (non-letter shape only) `#f5c86b`. One small accent per piece; consistent location per slot (helmet brow, chest center, wrist cuff outer, knee center, boot tongue).

#### 4.2 Weapon Skeleton (40 main + 30 offhand = 70 sprites)

```
[PREAMBLE — verbatim]

A 64×64 square inventory icon of a [WEAPON-FAMILY] in classic dungeon-crawler item-icon style. Carved stone-tile background, dark blue-gray gradient at reduced contrast, thin gold trim frame around the outer edge. Weapon centered on a [POSE] diagonal to fit 64×64, occupying ~70% of canvas, with a single-color black outline. [DEFINING-FEATURE]. Primary material palette follows the [TIER-PALETTE] metal ladder. [CLASS-GRIP — leather-wrapped haft (Warrior) / carved wood with twine (Ranger) / filigreed handle with gem pommel (Mage)]. No transparent corners.

[PALETTE CLAUSE] [NEGATIVE-PROMPT CLAUSE] No scene background, no wielder, no motion lines — weapon only.
```

**Slot fills:**
- `[WEAPON-FAMILY]`: sword / axe / hammer / shortbow / longbow / crossbow / staff / wand / small shield / tower shield / knife / claw (punch-weapon) / grimoire (small leather book, clasped) / codex (thicker tome, ribbon marker).
- `[POSE]`: blade-up vertical (swords), head-sideways (axes/hammers), full-length on NW-to-SE diagonal (bows/staves/wands to fit 64×64), face-front (shields), blade-down diagonal (knives/claws), spine-up with front cover visible (grimoires/codices).
- `[DEFINING-FEATURE]`: straight double-edged blade (sword T1–T2) / curved single-edged (sword T3+) / twin-axehead (axe) / squared blunt head (hammer) / wooden limbs with taut string (shortbow — short, ~50% canvas) / full-length limbs (longbow — ~85% canvas diagonal) / horizontal stock with prod (crossbow) / topped with glowing orb (staff) / small crystal tip (wand) / round central boss with rim studs (small shield) / rectangular body with central ridge (tower shield) / small triangular blade (knife) / four-knuckle spike set (claw) / leather cover with embossed sigil (shape-only) (grimoire/codex).

#### 4.3 Quiver Skeleton (9 sprites)

One prompt, 9 fills — no tier variation.

```
[PREAMBLE — verbatim]

A 64×64 square inventory icon of a leather quiver in classic dungeon-crawler item-icon style. Carved stone-tile background, dark blue-gray gradient at reduced contrast, thin gold trim frame around the outer edge. Quiver centered vertically, occupying ~70% of canvas, with a single-color black outline. Brown leather body with visible stitching and a shoulder strap. Three arrow shafts visible protruding from the top, fletchings facing up. Fletching tips colored [IMBUE-COLOR] — [IMBUE-DESCRIPTOR]. No transparent corners.

[PALETTE CLAUSE] [NEGATIVE-PROMPT CLAUSE] No scene background, no bow, no archer — quiver only.
```

**Imbue table:**

| Imbue | Color | Descriptor |
|---|---|---|
| Basic | neutral wood-gray `#9a8a6a` | plain wood arrowheads visible |
| Hot | flame-amber `#ff8a3a` | small flame curls at tips |
| Cold | ice-blue `#8ed6ff` | frost crystals at tips |
| Heavy | dark iron `#4a4a5a` | thick square heads |
| Nasty | sickly green `#6aa83a` | dripping poison glaze at tips |
| Zap | electric yellow `#f8e04a` | jagged spark lines |
| Quiet | deep shadow-purple `#3a2a4a` | smoke wisps at tips |
| Sharp | silver-white `#d8dce8` | razor-edge gleam |
| Bright | holy-gold `#f5c86b` | radiant halo glow at tips |

#### 4.4 Neck Skeleton (15 sprites)

```
[PREAMBLE — verbatim]

A 64×64 square inventory icon of a [METAL] chain necklace in classic dungeon-crawler item-icon style. Carved stone-tile background, dark blue-gray gradient at reduced contrast, thin gold trim frame around the outer edge. Chain hanging vertically in a wide U-shape, occupying ~70% of canvas, with a single-color black outline. Chain links follow [TIER-PALETTE]. Pendant centered at the bottom of the chain, shaped as a [PENDANT-SHAPE], in [COLOR-BEAT]. No transparent corners.

[PALETTE CLAUSE] [NEGATIVE-PROMPT CLAUSE] No scene background, no wearer — necklace only.
```

**Slot fills:**
- `[METAL]` / `[TIER-PALETTE]`: Iron / Steel / Mithril / Orichalcum / Dragonite (§2).
- `[PENDANT-SHAPE]`: gem-facet teardrop (offense) / amulet-disc with embossed ring (defense) / carved-bone token (utility). Catalog currently has 3 focuses (offense/defense/utility) × 5 tiers = 15 items.
- `[COLOR-BEAT]`: Might/offense = red `#ff6f6f`; Warding/defense = blue `#8ed6ff`; Fortune/utility = gold `#f5c86b`.

#### 4.5 Ring Skeleton (40 sprites)

Note per §1.2 — rings fill ~80% of canvas with reduced plaque contrast.

```
[PREAMBLE — verbatim]

A 64×64 square inventory icon of a [METAL] ring in classic dungeon-crawler item-icon style. Carved stone-tile background, dark blue-gray gradient at reduced contrast (further reduced for ring category), thin gold trim frame around the outer edge. Ring centered, occupying ~80% of canvas, three-quarter view showing band thickness, with a single-color black outline. Band metal follows [BAND-METAL] from the tier ladder. Single [STONE-SHAPE] stone set in the top of the band, colored [FOCUS-COLOR]. Small metallic engraving flourishes on the band flanking the stone. No transparent corners.

[PALETTE CLAUSE] [NEGATIVE-PROMPT CLAUSE] No scene background, no finger, no hand — ring only.
```

**Slot fills:**
- `[METAL]` / `[BAND-METAL]`: Iron / Steel / Mithril / Orichalcum / Dragonite (§2).
- `[STONE-SHAPE]`: round cabochon (STR/Str-adjacent) / oval cabochon (DEX/Haste) / square-cut (STA/Block) / pointed marquise (INT/Crit) / teardrop (Dodge).
- `[FOCUS-COLOR]`:
  - STR = red `#ff6f6f`
  - DEX = green `#6bff89`
  - STA = brown `#a07a5a`
  - INT = blue `#6ab0f8`
  - Crit (Precision) = bright red-orange `#ff9a4a`
  - Haste = yellow `#f8e04a`
  - Dodge (Evasion) = green-silver `#a8e8b8`
  - Block (Bulwark) = deep blue `#4a78c8`

#### 4.6 Consumable Skeleton (28 sprites)

```
[PREAMBLE — verbatim]

A 64×64 square inventory icon of a [TYPE] in classic dungeon-crawler item-icon style. Carved stone-tile background, dark blue-gray gradient at reduced contrast, thin gold trim frame around the outer edge. Item centered, occupying ~70% of canvas, with a single-color black outline. [TYPE-DEFINING-FEATURES]. [COLOR]. [SIZE-INDICATOR]. No transparent corners.

[PALETTE CLAUSE] [NEGATIVE-PROMPT CLAUSE] No scene background, no consumer — item only.
```

**Slot fills:**
- `[TYPE]`: potion-bottle (HP 4 + MP 4 = 8) / rolled scroll (5 buff scrolls + elemental effects) / bomb orb (3 bombs) / food item (3 — bread loaf / stew bowl / feast platter) / bandage cloth roll (2) / vial (2 antidotes) / rune stone (2 teleport) / golden idol statuette (1) / glass flask (2 elixirs).
- `[COLOR]`: red HP / blue MP / gold Might-scroll / teal Focus-scroll / silver Warding-scroll / yellow Haste-scroll / white Sight-scroll / orange Fire-bomb / yellow Shock-bomb / cyan Frost-bomb / brown bread / earth-tone stew / white feast / gray rough-bandage / white fine-bandage / green small-antidote / deep-green strong-antidote / blue town-teleport / red dungeon-teleport / gold idol / violet XP-elixir / green luck-elixir.
- `[SIZE-INDICATOR]`: for the 4 potion sizes (Small / Medium / Large / Greater) — small = short round bottle; medium = taller narrow bottle; large = wide flask with cork; greater = ornate filigreed flask with glowing cap. Applied to both HP and MP potion ladders.

**Note:** Consumables do **not** follow the metal ladder. Each consumable has its own color-by-effect palette.

#### 4.7 Material Skeleton (22 sprites)

```
[PREAMBLE — verbatim]

A 64×64 square inventory icon of a [MATERIAL-SHAPE] in classic dungeon-crawler item-icon style. Carved stone-tile background, dark blue-gray gradient at reduced contrast, thin gold trim frame around the outer edge. Material pile centered, occupying ~70% of canvas, with a single-color black outline. [MATERIAL-COLOR]. No transparent corners.

[PALETTE CLAUSE] [NEGATIVE-PROMPT CLAUSE] No scene background, no container — material only.
```

**Slot fills:**
- `[MATERIAL-SHAPE]`: raw ore chunk (5 ores — T1–T5) / bone fragment pile (5 bones — T1–T5) / folded hide scrap (5 hides — T1–T5) / bone dust vial (signature: skeleton) / goblin tooth (signature: goblin) / echo shard crystal (signature: bat) / wolf pelt (signature: wolf) / orc tusk (signature: orc) / arcane residue vial (signature: dark mage) / chitin fragment (signature: spider).
- `[MATERIAL-COLOR]`: Ores follow the metal ladder (§2). Bones: rough = dull yellow-gray, progressing to top-shelf = pearl-white. Hides: rough = mottled brown, progressing to top-shelf = rich mahogany with a sheen. Signatures follow their thematic color per [item-catalog.md](../inventory/item-catalog.md) table (Bone Dust = pale gray-white; Goblin Tooth = yellow-ivory; Echo Shard = blue-cyan crystal; Wolf Pelt = gray-brown; Orc Tusk = off-white; Arcane Residue = swirling violet; Chitin Fragment = dark green-brown).

### 5. Worked Examples (One per Category)

#### 5.1 Armor — "Iron Helmet" (Warrior, T1, head_warrior_helmet_t1)

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on the item silhouette with a thin gold-trim border around the outer plaque, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A 64×64 square inventory icon of a helmet with horns in classic dungeon-crawler item-icon style. Carved stone-tile background, dark blue-gray (#24314a to #3c4664) gradient at reduced contrast, thin gold (#f5c86b) trim frame around the outer edge. Item centered, occupying ~70% of canvas, with a single-color black outline. Thick overlapping plate metal construction. Primary material palette: dull gray (#6a6a6a) base with rust-brown (#7a4a2a) weathering at the edges — flat shading, no highlight, "this looks beaten up." Small blue dot (#8ed6ff) marker on the brow. No transparent corners — icon fills the full 64×64 square.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing. No scene background, no character wearing the item, no action pose — item only, item-icon plaque only.
```

#### 5.2 Weapon — "Quality Shortie" (Ranger shortbow, T3, mainhand_ranger_shortbow_t3)

```
[PREAMBLE]

A 64×64 square inventory icon of a shortbow in classic dungeon-crawler item-icon style. Carved stone-tile background, dark blue-gray gradient at reduced contrast, thin gold trim frame around the outer edge. Weapon centered on an NW-to-SE diagonal to fit 64×64, occupying ~70% of canvas, with a single-color black outline. Short wooden limbs with a taut bowstring running from tip to tip — limbs short (~50% canvas), curved, carved wood grain visible. Primary material palette: pale blue-silver (#c8d8ea) base on the limb-wraps and nocking points, with subtle shimmer (#e4ecf8) highlight — Mithril tier, "this glows faintly." Carved wood with twine grip at center. No transparent corners.

[PALETTE CLAUSE] [NEGATIVE-PROMPT CLAUSE] No scene background, no wielder, no motion lines — weapon only.
```

#### 5.3 Quiver — "Hot Quiver" (ammo_quiver_hot)

```
[PREAMBLE]

A 64×64 square inventory icon of a leather quiver in classic dungeon-crawler item-icon style. Carved stone-tile background, dark blue-gray gradient at reduced contrast, thin gold trim frame around the outer edge. Quiver centered vertically, occupying ~70% of canvas, with a single-color black outline. Brown leather body with visible stitching and a shoulder strap. Three arrow shafts visible protruding from the top, fletchings facing up. Fletching tips colored flame-amber (#ff8a3a) — small flame curls at tips. No transparent corners.

[PALETTE CLAUSE] [NEGATIVE-PROMPT CLAUSE] No scene background, no bow, no archer — quiver only.
```

#### 5.4 Neck — "Mithril Chain of Might" (neck_t3_offense)

```
[PREAMBLE]

A 64×64 square inventory icon of a Mithril chain necklace in classic dungeon-crawler item-icon style. Carved stone-tile background, dark blue-gray gradient at reduced contrast, thin gold trim frame around the outer edge. Chain hanging vertically in a wide U-shape, occupying ~70% of canvas, with a single-color black outline. Chain links rendered in pale blue-silver (#c8d8ea) with subtle shimmer highlight (#e4ecf8). Pendant centered at the bottom of the chain, shaped as a gem-facet teardrop, in red (#ff6f6f). No transparent corners.

[PALETTE CLAUSE] [NEGATIVE-PROMPT CLAUSE] No scene background, no wearer — necklace only.
```

#### 5.5 Ring — "Dragonite Ring of Precision" (ring_t5_crit)

```
[PREAMBLE]

A 64×64 square inventory icon of a Dragonite ring in classic dungeon-crawler item-icon style. Carved stone-tile background, dark blue-gray gradient at further reduced contrast, thin gold trim frame around the outer edge. Ring centered, occupying ~80% of canvas, three-quarter view showing band thickness, with a single-color black outline. Band metal in iridescent purple-to-green shift (#7a4aa8 → #3a8a5a) with deep shadow (#2a1a3a) and pearl highlight (#e8d8f4) — three-band iridescence, "this is otherworldly." Single pointed marquise-cut stone set in the top of the band, colored bright red-orange (#ff9a4a). Small metallic engraving flourishes on the band flanking the stone. No transparent corners.

[PALETTE CLAUSE] [NEGATIVE-PROMPT CLAUSE] No scene background, no finger, no hand — ring only.
```

#### 5.6 Consumable — "Large Health Potion" (consumable_hp_large)

```
[PREAMBLE]

A 64×64 square inventory icon of a potion-bottle in classic dungeon-crawler item-icon style. Carved stone-tile background, dark blue-gray gradient at reduced contrast, thin gold trim frame around the outer edge. Item centered, occupying ~70% of canvas, with a single-color black outline. Wide flask with cork — glass body showing a red (#c83838) liquid fill-line, small bubble specks. Brown cork stopper. Simple paper label tied with twine (no visible lettering). Large flask size indicator (wider, shorter profile than medium; narrower than "greater"'s ornate filigree). No transparent corners.

[PALETTE CLAUSE] [NEGATIVE-PROMPT CLAUSE] No scene background, no consumer — item only.
```

#### 5.7 Material — "Mithril Ore" (material_ore_t3)

```
[PREAMBLE]

A 64×64 square inventory icon of a raw ore chunk in classic dungeon-crawler item-icon style. Carved stone-tile background, dark blue-gray gradient at reduced contrast, thin gold trim frame around the outer edge. Material pile centered, occupying ~70% of canvas, with a single-color black outline. Three to four rough-hewn crystal-faceted ore chunks clustered on the plaque. Primary material palette: pale blue-silver (#c8d8ea) base with subtle shimmer (#e4ecf8) highlight on crystal facets — Mithril tier. No transparent corners.

[PALETTE CLAUSE] [NEGATIVE-PROMPT CLAUSE] No scene background, no container — material only.
```

### 6. Batch Generation Plan

259 icons × ~30 s per job × (1 / 8 concurrent) ≈ **~16 minutes of wall clock** of raw generation if perfectly parallelized. Real-world: **~60–90 minutes** accounting for queue latency, retries, and rate-limit backoff.

**Generation order (tier-banded, not category-banded).** The 5-metal ladder's per-tier palette must stay cohesive across categories; authoring Tier 1 Iron items across all applicable categories in one burst produces a tighter visual family than authoring all armor and then all weapons:

| Band | Contents | Count |
|---|---|---|
| **Band 1: Tier-free + T1** | Quivers (9) · all consumables (28) · all species-signature materials (7) · T1 armor (15) · T1 weapons (14) · T1 neck (3) · T1 ring (8) · T1 ore/bone/hide material (3) | **87** |
| **Band 2: T2** | T2 armor (15) · T2 weapons (14) · T2 neck (3) · T2 ring (8) · T2 materials (3) | **43** |
| **Band 3: T3** | T3 armor (15) · T3 weapons (14) · T3 neck (3) · T3 ring (8) · T3 materials (3) | **43** |
| **Band 4: T4** | T4 armor (15) · T4 weapons (14) · T4 neck (3) · T4 ring (8) · T4 materials (3) | **43** |
| **Band 5: T5** | T5 armor (15) · T5 weapons (14) · T5 neck (3) · T5 ring (8) · T5 materials (3) — `detailed shading` override | **43** |
| **Total** | | **259** |

**Rate-limit discipline.** PixelLab allows 8 concurrent jobs. Queue at most 8 at a time per band; wait for the band's Δ (delta) to empty before queuing the next. Each band runs 5–20 minutes end-to-end depending on queue depth. Use one fixed `seed` per band so within-band style stays cohesive; advance the seed between bands.

**Weapon-family consistency.** Within a tier band, author all 5 swords (T1→T5 not per tier) if possible — but per the tier-banded order above, this is deferred in favor of per-tier cohesion. Accept slight within-family variance (sword T1 vs T5 may read as different families) as a reasonable tradeoff; the metal ladder is the primary identity cue anyway.

**Resume discipline.** If a band crashes mid-run, the generated icons are already saved to `assets/items/`. Resume by re-running only the missing item IDs against the skeleton — no need to re-author the whole band. Every item ID maps to one filename (§7), so a file-existence check gates resumption.

### 7. Asset Directory Layout

```
assets/items/
├── armor/
│   ├── head_warrior_helmet_t1.png
│   ├── head_warrior_helmet_t2.png
│   ├── ...
│   ├── feet_mage_sandals_t5.png
│   └── metadata.json
├── weapons/
│   ├── mainhand_warrior_sword_t1.png
│   ├── ...
│   ├── offhand_mage_codex_t5.png
│   └── metadata.json
├── quivers/
│   ├── ammo_quiver_basic.png
│   ├── ammo_quiver_hot.png
│   ├── ...
│   └── metadata.json
├── accessories/
│   ├── neck_t1_offense.png
│   ├── ...
│   ├── ring_t5_block.png
│   └── metadata.json
├── consumables/
│   ├── consumable_hp_small.png
│   ├── ...
│   └── metadata.json
└── materials/
    ├── material_ore_t1.png
    ├── ...
    ├── material_sig_spider.png
    └── metadata.json
```

**Filename rule.** Each icon's filename is **exactly** the `ItemDef.ID` from [item-catalog.md](../inventory/item-catalog.md) + `.png`. This is the single load-path contract — `ItemDef.IconPath = "res://assets/items/{category}/{ID}.png"` with `category` derived from the slot. No renaming, no aliasing.

**metadata.json per category dir.** Generated by PixelLab's ZIP export containing the prompt used, seed, params. Not consumed by the engine — reference-only for regeneration traceability. Excluded from the runtime `IconPath` resolver.

**Category → directory map (binding):**

| Category | Directory | Count |
|---|---|---|
| Armor (all slots, all classes) | `assets/items/armor/` | 75 |
| Main-hand + off-hand weapons | `assets/items/weapons/` | 70 |
| Quivers | `assets/items/quivers/` | 9 |
| Neck + Ring | `assets/items/accessories/` | 55 |
| Consumables | `assets/items/consumables/` | 28 |
| Materials (generic + signature) | `assets/items/materials/` | 22 |
| **Total** | | **259** |

### 8. Thumbnail-Readability Gate (PR Checklist)

Every ART-03..06 PR must include a 4×4 sampler screenshot of the generated band rendered at **two sizes**:

- **64×64 (native)** — the full icon. Every icon must read its category (armor silhouette is armor, not a potion; potion bottle is a potion, not a scroll) and its tier metal (Iron vs Steel vs Mithril vs Orichalcum vs Dragonite visibly different).
- **32×32 (backpack grid thumb)** — the icon as it appears in `BackpackWindow`. Category must still read. Tier metal must still read. Class voice (Warrior plate vs Ranger leather vs Mage cloth) must read at 32×32 silhouette.

**Gate failure modes:**
- Two tiers collapse to one color at 32×32 → regenerate the lower-contrast one.
- Ring becomes a hard-to-see bump on a plaque at 32×32 → §1.2 ring plaque reduction not applied correctly; regen.
- Class voice ambiguous (can't tell Warrior helmet from Ranger hood) → §3 main-material palette not applied; regen.
- Any icon's pictograph/illustration reads outside the clamp (neon green, magenta) → regen.

### 9. IP-Clean Discipline

All prompts are genre-generic. No named-IP invocation:
- No item names from licensed games in prompts (e.g., no "Diablo", "Path of Exile", "World of Warcraft"). The catalog's class-voice item names (e.g., "Mega Sword") are our own authorship and are safe — but the **prompt** still describes the item generically ("a helmet with horns" not "a Mega Sword helmet"). The filename carries the canonical ID; the prompt describes the archetype.
- No embossed text, letters, runes-as-letters, or brand marks (per `[NEGATIVE-PROMPT CLAUSE]`'s `no text / no letters / no numbers / no logos`). Runes on armor/weapons are shape-only.
- "Classic dungeon-crawler" / "classic fantasy RPG" phrasing is the allowed abstraction per [prompt-templates.md §11 IP Protection](prompt-templates.md). No single-title invocation.

### 10. Delete-Before-Regen Policy

[asset-inventory.md §Bucket G](asset-inventory.md) is currently empty — this is the **first** generation. No delete-sweep on the first pass. Subsequent regenerations (e.g., if T5 iridescence misses the gate and gets rerolled) follow delete-first:

```bash
# Example: regenerate T3 Mithril armor band
rm assets/items/armor/head_*_t3.png assets/items/armor/body_*_t3.png ...
# Then regenerate from §6 Band 3.
```

Scripts to automate the bucket-G delete-sweep are deferred to ART-03's implementation.

### 11. Acceptance Criteria

- [ ] Any of the 259 items in [item-catalog.md](../inventory/item-catalog.md) / `ItemDatabase.cs` can be authored by filling one of the §4 skeletons — no additional prompt engineering required. (Acceptance gate: pick 10 item IDs at random from `ItemDatabase.cs`; each one maps cleanly to a skeleton fill.)
- [ ] Per-tier metal ladder (Iron / Steel / Mithril / Orichalcum / Dragonite) is visually distinguishable at 64×64 AND at 32×32 thumbnail (per §8 gate).
- [ ] Worked examples in §5 produce expected output when run through PixelLab. (Product-owner gate: run each of the 7 worked examples as a sanity check before batching the remaining 252.)
- [ ] Class voice in armor (Warrior plate vs Ranger leather vs Mage cloth) reads at 32×32 silhouette (per §8 gate).
- [ ] Every generated filename matches its `ItemDef.ID` exactly (per §7).
- [ ] `metadata.json` present in every category dir under `assets/items/` (per §7).
- [ ] Zero IP violations — no named franchise references in any prompt, no rendered text/letters/logos on any icon.
- [ ] BackpackWindow renders actual icons (not colored rectangles) after ART-03..06 land.

### 12. Open Questions

None. All design decisions resolved at spec lock:

- **Style variant per category:** all 259 use `ITEM-ICON-64` (§1.1). `ICON-UI-64` is reserved for ability icons (ART-07a/b); equipment does not use pictograph style.
- **Ring plaque reduction:** codified in §1.2 — rings fill ~80% (not ~70%) with further-reduced plaque contrast. No per-PR decision.
- **Metal ladder non-applicable items:** potions, scrolls, food, bones, hides, signature materials, gems follow per-category palette rules (§4); the metal ladder is Warrior-armor/weapon/neck/ring/ore only.
- **Generation order:** tier-banded, not category-banded (§6). Locked.

## Implementation Notes

- `ItemDef.IconPath` is a new field (not currently in `ItemDef`) — add as a `string` column, defaulted to the category/ID convention in §7. Code-level work belongs to ART-03's implementer, not this spec.
- A regeneration of a single icon (e.g., Steel Helmet looked wrong) is one PixelLab job. No batch required for one-offs.
- The `BackpackWindow` icon renderer should fall back to a placeholder texture (existing colored-rectangle behavior) if a path is missing — this keeps the UI functional during the multi-PR rollout of ART-03..06.
- Signature materials (7) are authored in Band 1 alongside quivers and consumables because they are tier-free and do not fit any metal-ladder batch.

## Changelog

- **2026-04-17 — v1 (ART-SPEC-06):** Initial spec. Locks `ITEM-ICON-64` sibling block, 5-metal palette ladder, 7 prompt skeletons, 7 worked examples, 5-band generation plan, directory layout, readability gate. Unblocks ART-03 / ART-04 / ART-05 / ART-06.
