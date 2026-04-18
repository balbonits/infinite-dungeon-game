# Object Pipeline — Per-Biome Environmental Props (ART-SPEC-04)

## Summary

Locks the full environmental-object generation recipe that extends the `OBJ-ISO` block from [prompt-templates.md](prompt-templates.md) (ART-SPEC-01 v2, commit `5e9e70f`). Strategy mirrors the sister tile spec: author the **Dungeon biome deeply** with complete copy-paste prompts, then **template the other 7 biomes** (`dungeon_dark`, `cathedral`, `nether`, `sky_temple`, `volcano`, `water`, `town`) via a palette + motif substitution table. Objects are grouped by **canonical function** (light, structural, sacred, hazard, decor) rather than biome, because the family determines canvas size / anchor rule / animation hooks, while biome only substitutes motif + palette.

All prompts use `create_map_object` with hand-authored canvas dimensions. All prompts follow [prompt-templates.md §11 IP Protection](prompt-templates.md) — zero named-IP invocation, cartoonish pixel-art, stone-catacomb framing only.

**In scope:** light sources (torch / brazier / candelabra / hanging chain+torch / lantern), structural (pillar / carved pillar / rubble / arch), sacred (altar / shrine / statue), hazard (lava geyser / poison pool / fire tile / water puddle), decor (biome-specific thematic props).

**Explicitly out of scope:**
- **Containers** (jar / crate / chest closed+open) — authored under `ART-SPEC-05` (parallel spec).
- **Stairs + doors** — tile slots, authored under [tile-pipeline.md](tile-pipeline.md) (`stairs-up-*`, `stairs-down-*`, `door-arch-*`).
- **NPCs + monsters** — characters, authored under `ART-SPEC-NPC-01` / `ART-SPEC-02`.

Consumed by: ART-13 (environmental object generation per biome). Preceded by [asset-inventory.md §Bucket E](asset-inventory.md) delete sweep (empty bucket — objects are net-new) **and** the [§7 hazard-redraw migration](#7-hazard-redraw-migration) of 6 files from Bucket K. Engine binding: [docs/systems/iso-rendering.md](../systems/iso-rendering.md) SPEC-ISO-01 — every object is a bottom-center-anchored `Node2D` under `Entities`, not a TileMapLayer cell.

## Current State

**Spec status: DRAFT.** Awaiting product-owner review.

No environmental object sprites exist in-repo today. [asset-inventory.md §Bucket E](asset-inventory.md) is an empty placeholder; the 6 hazard-like files under `assets/effects/` ([§Bucket K](asset-inventory.md)) are visual-effect overlays that this spec re-categorizes as iso-anchored map objects (see [§7](#7-hazard-redraw-migration)).

This spec unblocks ART-13 to produce the first full biome pass of environmental props (≥25 objects per biome × 8 biomes). The tile-pipeline's 30-slot atlas and the object-pipeline's per-biome prop set ship independently; the engine binds them both through the `Entities` Y-sort container.

## Design

### 1. Object Family Table

Five canonical families. Each row names the canvas size convention, bottom-center anchor rule, and whether the object supports per-biome motif variation.

| Family | Canonical Canvas | Anchor (`Sprite2D.offset`) | Biome Variation? | Shading Default | Representative Members |
|---|---|---|---|---|---|
| **Light** | 64 × 64 (short) or 64 × 128 (tall candelabra / hanging chain) | Short: `(0, -48)` · Tall: `(0, -80)` | Low — torch looks similar across biomes; palette swaps flame color. | `medium` (standard); `detailed` on hero braziers | wall torch, floor brazier, candelabra, hanging chain+torch, lantern |
| **Structural** | 64 × 128 (pillar, carved pillar); 64 × 64 (rubble pile); 128 × 96 (arch-prop) | Tall: `(0, -80)` · Short: `(0, -48)` · Multi-tile: `(0, -64)` at southernmost cell | **High** — stone / marble / obsidian / wet stone dramatically change silhouette. | `medium` (standard); `detailed` on carved variants | plain pillar, carved pillar, rubble pile, free-standing arch |
| **Sacred** | 128 × 96 (altar — 2×1 multi-tile); 64 × 128 (shrine statue, gargoyle statue) | Multi-tile altar: `(0, -64)` at southernmost cell · Tall statue: `(0, -80)` | **High** — altars and statues are the single strongest biome-identity beat. | `detailed` (always — sacred is a hero-prop category) | altar, shrine-statue, gargoyle-statue |
| **Hazard** | 64 × 64 (short, floor-hugging) | `(0, -48)` | Medium — lava/poison/fire/water are semantically biome-scoped but share canvas. | `medium` | lava geyser, poison pool, fire tile, water puddle |
| **Decor** | 64 × 64 (most) or 64 × 128 (tall: market stall, sign post, crystal cluster) | Short: `(0, -48)` · Tall: `(0, -80)` | **High** — decor is the biome's "flavor layer." | `medium` | bones + skulls, cathedral pews, volcano obsidian slab, water seaweed, town market stall |

**Anchor discipline.** Every family uses bottom-center-of-footprint anchoring, configured by `Sprite2D.offset` in the imported scene. Multi-tile objects (altar, free-standing arch) anchor at the **bottom-center of the southernmost floor cell the object covers**, per SPEC-ISO-01 §Z-Ordering. This is the only anchor convention in the object pipeline — no per-callsite pixel offsets.

**Inheritance.** All canvases, offsets, and shading defaults in this table are **locked values of `OBJ-ISO`** (see [prompt-templates.md §2 Block `OBJ-ISO`](prompt-templates.md) size table). This spec does not introduce new canvases; it enumerates the family-level defaults so ART-13 authors do not have to cross-reference the OBJ-ISO size table for every prompt.

### 2. Per-Object Prompt Skeleton

All object prompts extend the `OBJ-ISO` fill-in-the-blank from [prompt-templates.md §2 `OBJ-ISO`](prompt-templates.md). The skeleton is:

```
[PREAMBLE — verbatim, §1 of prompt-templates.md]

A [BIOME adjective] [OBJECT NAME] rendered in cartoonish isometric pixel-art style. [STATE — lit / unlit / broken / intact]. [MATERIAL — dark wood with iron bands / pitted stone / obsidian slab / pale marble / etc.]. [DEFINING-FEATURE — flame plume / iron brazier bowl / carved rune relief (shape-only, non-letter) / ornate trim]. Object sits at canvas bottom-center, occupies the lower ~90% of the canvas, transparent background, no ground shadow baked in.

[PALETTE CLAUSE — verbatim, §1c of prompt-templates.md]
[BIOME PALETTE HOOK — from §5 substitution table]

[NEGATIVE-PROMPT CLAUSE — verbatim, §1 of prompt-templates.md] No background terrain, no ground, no floor tile — transparent PNG only.
```

**Variant slots per family** (3–4 variants per family, per ART-SPEC-04 acceptance):

- **Light (4):** wall torch · floor brazier · hanging chain + torch · candelabra (pillar candlestand).
- **Structural (3):** plain pillar · carved pillar · rubble pile. *(Free-standing arch is a deferred 4th; not required for the v1 biome pass.)*
- **Sacred (3):** altar (multi-tile) · shrine-statue (small idol on pedestal) · gargoyle-statue (tall perched figure).
- **Hazard (4):** lava geyser · poison pool · fire tile · water puddle.
- **Decor (≥6, biome-specific):** one or two locked-in prompts per biome — Dungeon bones, Cathedral pews, Sky_temple crystal cluster, Volcano obsidian slab, Water seaweed + shell piles, Town market stall + sign post. (Dungeon_Dark and Nether reuse Dungeon's decor vocabulary with palette override — chains, bones, rusted iron.)

**PixelLab params (locked across all object slots, all biomes):**

| Param | Value |
|---|---|
| Tool | `create_map_object` |
| `view` | `high top-down` (per `OBJ-ISO` — the description carries the "true iso" cue; `high top-down` gives the slight top-facing tilt that sits cleanly on a floor diamond) |
| `outline` | `single color outline` |
| `shading` | family default (see §1) |
| `detail` | `medium detail` (standard); `high detail` on sacred + carved-structural |
| `ai_freedom` | `500` (batch-consistency runs) |
| `seed` | **Lock one seed per biome**, reused across every object in that biome. Same discipline as [tile-pipeline.md §1](tile-pipeline.md). |

Canvas `width` × `height` varies per family per §1.

### 3. Dungeon Biome — Full Prompt Library

Every prompt below opens with the universal preamble verbatim and closes with the universal palette clause + dungeon sub-clamp + universal negatives + OBJ-ISO transparent-PNG rider. The dungeon sub-clamp is the one from [prompt-templates.md §2 `TILE-ISO-ATLAS`](prompt-templates.md):

> *"Dungeon biome: stone blue-gray (#24314a / #3c4664), moss green accents, torch-amber highlights in cracks."*

**Dungeon motif vocabulary** (substitution target for other biomes — §5):

- Stone + iron: dark blue-gray stone, iron bands, iron rivets, iron lock plates, rusted chains.
- Light: torch-amber flame, warm gold (#f5c86b) highlights, candle-wax off-white drips.
- Decay: moss-green creep at bases, hairline cracks, rubble chips, faint rune etchings (shape-only, non-letter).
- Cartoonish descriptors: chunky block silhouettes, slightly exaggerated bevels, readable iron rivet shapes.

#### 3.1 Light — `torch-wall` (64 × 64)

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A dungeon wall-mounted torch rendered in cartoonish isometric pixel-art style, lit and actively burning. Dark wooden haft wrapped with iron bands bolted to a chunky iron bracket, the bracket mounted on an implied stone wall face (the wall is NOT drawn — only the torch + bracket). A bright torch-amber flame plume rises from the oil-soaked rag head, warm gold (#f5c86b) highlights on the bracket bolts. Slightly exaggerated cartoonish silhouette — chunky bracket arm, readable flame shape, no wispy realism. Object sits at canvas bottom-center, occupies the lower ~90% of the canvas, transparent background, no ground shadow baked in.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.
Dungeon biome: stone blue-gray (#24314a / #3c4664), moss green accents, torch-amber highlights in cracks.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing. No background terrain, no ground, no floor tile — transparent PNG only.
```

#### 3.2 Light — `brazier-floor` (64 × 64)

```
[PREAMBLE — verbatim]

A dungeon floor-standing brazier rendered in cartoonish isometric pixel-art style, lit with a tall flame plume. Chunky iron bowl on three stubby iron legs, the bowl filled with glowing coals at the base and a torch-amber flame crown at the top. Iron rivets and hammered-bevel edges read clearly in the cartoonish silhouette, warm gold (#f5c86b) highlight on the rim, deep shadow inside the bowl below the flame. Object sits at canvas bottom-center, occupies the lower ~90% of the canvas, transparent background, no ground shadow baked in.

[PALETTE CLAUSE + DUNGEON SUB-CLAMP — verbatim]

[NEGATIVE-PROMPT CLAUSE + OBJ-ISO RIDER — verbatim]
```

#### 3.3 Light — `chain-torch-hanging` (64 × 128)

```
[PREAMBLE — verbatim]

A dungeon hanging chain with a torch-basket rendered in cartoonish isometric pixel-art style, basket lit and swaying slightly. A heavy iron chain drops from the top of the canvas (implied ceiling, not drawn), suspending a small chunky iron brazier-basket filled with burning coals and a torch-amber flame plume. Chain links read as chunky cartoonish loops, iron rivets on the basket, warm gold (#f5c86b) highlight on the basket rim and the lowest chain link. Moss-green tarnish at one point on the chain. Tall prop — the chain fills the upper canvas, the basket + flame sit in the lower ~40%. Object bottom-center, transparent background, no ground shadow baked in.

[PALETTE CLAUSE + DUNGEON SUB-CLAMP — verbatim]

[NEGATIVE-PROMPT CLAUSE + OBJ-ISO RIDER — verbatim]
```

#### 3.4 Light — `candelabra` (64 × 128)

```
[PREAMBLE — verbatim]

A dungeon floor-standing candelabra rendered in cartoonish isometric pixel-art style, three candles lit. A tall wrought-iron pillar-stand with three branching arms, each arm holding a thick ivory candle with a torch-amber flame and off-white wax drips crawling down. Chunky iron base at the floor, slightly exaggerated cartoonish bevels on the branches, warm gold (#f5c86b) highlight on the topmost flame. Deep shadow under the base. Tall prop — the stand and branches fill the full 128px height. Object bottom-center, transparent background, no ground shadow baked in.

[PALETTE CLAUSE + DUNGEON SUB-CLAMP — verbatim]

[NEGATIVE-PROMPT CLAUSE + OBJ-ISO RIDER — verbatim]
```

#### 3.5 Structural — `pillar-plain` (64 × 128)

```
[PREAMBLE — verbatim]

A dungeon plain stone pillar rendered in cartoonish isometric pixel-art style, intact and standing. Chunky dark blue-gray cobblestone stacked in three or four courses with readable mortar joints between them, a simple square capital at the top and a slightly wider base stone at the floor. Moss-green creep at the base, one hairline crack crossing the middle course, torch-amber highlight picking out the capital's top edge. Slightly exaggerated cartoonish stone bevels — chunky and readable, not photoreal. Tall prop — the pillar fills the full 128px height. Object bottom-center, transparent background, no ground shadow baked in.

[PALETTE CLAUSE + DUNGEON SUB-CLAMP — verbatim]

[NEGATIVE-PROMPT CLAUSE + OBJ-ISO RIDER — verbatim]
```

#### 3.6 Structural — `pillar-carved` (64 × 128, detailed shading)

```
[PREAMBLE — verbatim]

A dungeon carved stone pillar rendered in cartoonish isometric pixel-art style, intact and ornate. Chunky dark blue-gray cobblestone courses with a central shaft carved in relief — a band of shape-only geometric motifs (abstract scrollwork, NEVER letterforms) wraps the middle course, an ornate capital at the top with a small gargoyle-face-inspired boss, a wider base plinth at the floor. Warm gold (#f5c86b) highlights on the carved relief and the capital, moss-green in the shadow recesses of the relief band, torch-amber glint on the top edge. Slightly exaggerated cartoonish carving depth — readable shapes, not intricate realism. Tall prop — full 128px height. Object bottom-center, transparent background, no ground shadow baked in.

[PALETTE CLAUSE + DUNGEON SUB-CLAMP — verbatim]

[NEGATIVE-PROMPT CLAUSE + OBJ-ISO RIDER — verbatim]
```

*Use `shading: detailed shading`, `detail: high detail` on this slot.*

#### 3.7 Structural — `rubble-pile` (64 × 64)

```
[PREAMBLE — verbatim]

A dungeon rubble pile rendered in cartoonish isometric pixel-art style, heaped loose on the floor. Five to seven chunky broken stone chunks of varying sizes stacked irregularly — some with mossy edges, one with a visible hairline fracture, one with a faint rune-fragment shape (non-letter). Dark blue-gray stone with moss-green patches at two of the chunks, torch-amber highlight picking out the topmost chunk. Slightly exaggerated cartoonish chunk silhouettes, readable fracture lines. Short prop — pile sits in the lower ~60% of canvas. Object bottom-center, transparent background, no ground shadow baked in.

[PALETTE CLAUSE + DUNGEON SUB-CLAMP — verbatim]

[NEGATIVE-PROMPT CLAUSE + OBJ-ISO RIDER — verbatim]
```

#### 3.8 Sacred — `altar` (128 × 96, multi-tile, detailed shading)

```
[PREAMBLE — verbatim]

A dungeon blood-altar rendered in cartoonish isometric pixel-art style, sacrificial slab on a stepped stone base. A wide rectangular altar top carved from dark blue-gray stone with a shallow channel running down the middle, a muted dark-red (#ff6f6f dimmed to #8a3030) stain in the channel and dripping down one edge. Two-step chunky stone base beneath the top, iron ring mounts at the corners. Moss-green at the base-step corners, torch-amber highlight on the front edge of the altar top, faint shape-only rune motifs carved around the altar's side faces (non-letter). Slightly exaggerated cartoonish bevels — readable chunky silhouette covering two floor diamonds (east-west orientation). Object bottom-center, occupies the lower ~90% of the canvas, transparent background, no ground shadow baked in.

[PALETTE CLAUSE + DUNGEON SUB-CLAMP — verbatim]

[NEGATIVE-PROMPT CLAUSE + OBJ-ISO RIDER — verbatim]
```

*Use `shading: detailed shading`, `detail: high detail` on this slot. Anchor is southernmost floor cell of the 2×1 footprint.*

#### 3.9 Sacred — `shrine-statue` (64 × 128, detailed shading)

```
[PREAMBLE — verbatim]

A dungeon shrine statue rendered in cartoonish isometric pixel-art style, a small stone idol on a chunky pedestal. Cartoonish hooded-figure idol about 60% of the canvas height, carved from dark blue-gray stone with featureless stylized face (shape-only, no anime features), hands clasped at chest level. Chunky square stone pedestal beneath, moss-green creep at the pedestal base, torch-amber highlight on the idol's hood crest, faint shape-only rune motifs carved on the pedestal face (non-letter). Slightly exaggerated cartoonish proportions — big hood, compact body, readable silhouette. Tall prop — full 128px height. Object bottom-center, transparent background, no ground shadow baked in.

[PALETTE CLAUSE + DUNGEON SUB-CLAMP — verbatim]

[NEGATIVE-PROMPT CLAUSE + OBJ-ISO RIDER — verbatim]
```

#### 3.10 Sacred — `gargoyle-statue` (64 × 128, detailed shading)

```
[PREAMBLE — verbatim]

A dungeon gargoyle statue rendered in cartoonish isometric pixel-art style, a perched stone grotesque on a tall plinth. Cartoonish crouched gargoyle — bat-like wings folded, clawed fore-hands gripping the plinth edge, snarling jawed face (shape-only exaggeration, no realistic anatomy), carved from dark blue-gray stone. Chunky square stone plinth beneath, moss-green at the plinth base, torch-amber highlight picking out one wing edge and the gargoyle's crest. Slightly exaggerated cartoonish silhouette — big wings, big claws, chunky readable proportions. Tall prop — full 128px height. Object bottom-center, transparent background, no ground shadow baked in.

[PALETTE CLAUSE + DUNGEON SUB-CLAMP — verbatim]

[NEGATIVE-PROMPT CLAUSE + OBJ-ISO RIDER — verbatim]
```

#### 3.11 Hazard — `lava-geyser` (64 × 64)

```
[PREAMBLE — verbatim]

A dungeon lava geyser hazard rendered in cartoonish isometric pixel-art style, a small fissure in the floor erupting molten orange (#d47a20) and danger-red (#ff6f6f) lava in a short plume. Chunky cracked dark blue-gray stone rim around the fissure, molten glow pooling at the rim and a cartoonish plume rising about 30px above. Warm gold (#f5c86b) highlights on the brightest lava crests, deep shadow under the rim. Short hazard — sits flush on the floor footprint, plume rises within the 64px canvas. Object bottom-center, transparent background, no ground shadow baked in.

[PALETTE CLAUSE + DUNGEON SUB-CLAMP — verbatim]

[NEGATIVE-PROMPT CLAUSE + OBJ-ISO RIDER — verbatim]
```

#### 3.12 Hazard — `poison-pool` (64 × 64)

```
[PREAMBLE — verbatim]

A dungeon poison pool hazard rendered in cartoonish isometric pixel-art style, a shallow sickly-green (#6b8a4a) liquid pool on the floor. Chunky dark blue-gray stone rim around the pool, irregular cartoonish puddle shape with two or three small bubbles breaking the surface, a faint drifting wisp rising from one bubble. Moss-green at the stone rim, one muted dark-red (#8a3030) stain on the rim. Short hazard — sits flush on the floor footprint, pool fills the lower ~50% of canvas. Object bottom-center, transparent background, no ground shadow baked in.

[PALETTE CLAUSE + DUNGEON SUB-CLAMP — verbatim]

[NEGATIVE-PROMPT CLAUSE + OBJ-ISO RIDER — verbatim]
```

#### 3.13 Hazard — `fire-tile` (64 × 64)

```
[PREAMBLE — verbatim]

A dungeon fire hazard patch rendered in cartoonish isometric pixel-art style, a scorched tile section with flames rising. Chunky dark blue-gray stone surface blackened at the center, several small torch-amber and warm-gold (#f5c86b) flame tongues rising from embers in a cartoonish plume reaching about 40px up. Ash-gray scorch marks radiating from the flame base, deep shadow in the blackened center. Short hazard — sits flush on the floor footprint. Object bottom-center, transparent background, no ground shadow baked in.

[PALETTE CLAUSE + DUNGEON SUB-CLAMP — verbatim]

[NEGATIVE-PROMPT CLAUSE + OBJ-ISO RIDER — verbatim]
```

#### 3.14 Hazard — `water-puddle` (64 × 64)

```
[PREAMBLE — verbatim]

A dungeon water puddle hazard rendered in cartoonish isometric pixel-art style, a shallow dark water pool on the floor. Chunky dark blue-gray stone rim around the puddle, irregular cartoonish puddle shape with a pale-cyan (#8ed6ff) highlight ripple across the surface, one moss-green patch at the rim. Wet-stone sheen on the rim stones. Short hazard — sits flush on the floor footprint, pool fills the lower ~50% of canvas. Object bottom-center, transparent background, no ground shadow baked in.

[PALETTE CLAUSE + DUNGEON SUB-CLAMP — verbatim]

[NEGATIVE-PROMPT CLAUSE + OBJ-ISO RIDER — verbatim]
```

#### 3.15 Decor — `bones-pile` (64 × 64)

```
[PREAMBLE — verbatim]

A dungeon bone pile rendered in cartoonish isometric pixel-art style, a small heap of off-white bones and one skull on the floor. Cartoonish chunky bones — a femur, two ribs, a pelvis fragment, one grinning skull with hollow eye sockets (stylized cartoon shapes, no realistic detail) piled irregularly. Off-white (#e8dfc4) bones with dark blue-gray shadow recesses, torch-amber glint on the skull's top edge, muted dark-red (#8a3030) stain on the pelvis. Short prop — pile fills the lower ~60% of canvas. Object bottom-center, transparent background, no ground shadow baked in.

[PALETTE CLAUSE + DUNGEON SUB-CLAMP — verbatim]

[NEGATIVE-PROMPT CLAUSE + OBJ-ISO RIDER — verbatim]
```

#### 3.16 Decor — `chains-wall` (64 × 128)

```
[PREAMBLE — verbatim]

A dungeon wall-mounted chain rendered in cartoonish isometric pixel-art style, a heavy iron chain hanging from an implied wall anchor (wall NOT drawn). Chunky iron chain links cascade from near the top of the canvas down to a manacle and shackle at the lower end. Chain links read as chunky cartoonish loops with visible rust spots, moss-green tarnish at two points, torch-amber highlight on one upper link. Tall prop — chain fills the full 128px height. Object bottom-center, transparent background, no ground shadow baked in.

[PALETTE CLAUSE + DUNGEON SUB-CLAMP — verbatim]

[NEGATIVE-PROMPT CLAUSE + OBJ-ISO RIDER — verbatim]
```

### 4. Full Dungeon Slot List (ART-13 scope)

The 16 Dungeon prompts in §3 are the v1 biome pass. Slot IDs (filename stems under `assets/objects/<family>/dungeon/<slot>.png` per §6):

| # | Family | Slot ID | Canvas | Shading |
|---|---|---|---|---|
| 1 | light | `torch-wall` | 64 × 64 | medium |
| 2 | light | `brazier-floor` | 64 × 64 | medium |
| 3 | light | `chain-torch-hanging` | 64 × 128 | medium |
| 4 | light | `candelabra` | 64 × 128 | medium |
| 5 | structural | `pillar-plain` | 64 × 128 | medium |
| 6 | structural | `pillar-carved` | 64 × 128 | **detailed** |
| 7 | structural | `rubble-pile` | 64 × 64 | medium |
| 8 | sacred | `altar` | 128 × 96 | **detailed** |
| 9 | sacred | `shrine-statue` | 64 × 128 | **detailed** |
| 10 | sacred | `gargoyle-statue` | 64 × 128 | **detailed** |
| 11 | hazard | `lava-geyser` | 64 × 64 | medium |
| 12 | hazard | `poison-pool` | 64 × 64 | medium |
| 13 | hazard | `fire-tile` | 64 × 64 | medium |
| 14 | hazard | `water-puddle` | 64 × 64 | medium |
| 15 | decor | `bones-pile` | 64 × 64 | medium |
| 16 | decor | `chains-wall` | 64 × 128 | medium |

**Total: 16 Dungeon slots × 8 biomes = 128 objects** for the v1 biome pass (some biomes will add one or two unique decor slots — see §5 table; the 16-slot Dungeon set is the minimum baseline). This matches the ART-13 scope budget.

### 5. Per-Biome Substitution Table

The Dungeon slot grid (§4) and per-object prompt structure (§3) are **identical across all 8 biomes**. Only the **palette hook** and **motif hook** substitute. Palette hooks replicate [tile-pipeline.md §3](tile-pipeline.md) so objects and tiles stay coherent within a biome.

| Biome | Palette Hook (replaces Dungeon sub-clamp) | Motif Hook (replaces Dungeon motif vocabulary) | Biome-Unique Decor Addition |
|---|---|---|---|
| `dungeon` | *"Dungeon biome: stone blue-gray (#24314a / #3c4664), moss green accents, torch-amber highlights in cracks."* | Dark blue-gray stone + iron bands + moss-green creep + torch-amber flame + shape-only rune etchings. | — (exemplar) |
| `dungeon_dark` | *"Dungeon dark biome: near-black (#0f1117) base, deeper shadow (#1a1f2e), cold blue highlight (#3c4664) only, no warm accents, no moss."* | Near-black charred stone + blackened iron + cold-blue cracks only + extinguished / cold-blue flames (no amber) + NO moss, NO torch-amber. Chains rust-less near-black. | `skull-impaled-on-spike` (64 × 128) |
| `cathedral` | *"Cathedral biome: pale marble (#c8c0b0) base, warm gold (#f5c86b) trim, stained-glass blue (#8ed6ff) accents, candle-amber glow."* | Pale marble + bronze-gold fittings + angel / winged-relief motifs (shape-only, non-letter) + candle-amber flame + stained-glass-blue accents. Chain-torch becomes "ornate bronze chandelier chain." | `pew-bench` (64 × 64) + `prayer-beads-pile` (64 × 64) |
| `nether` | *"Nether biome: charcoal black base (#1a0d12), blood red (#ff6f6f) accents, sulfurous orange (#d47a20) glow in fissures, sickly green (#6b8a4a) drip."* | Charred / bleeding stone + blackened iron + glowing-orange fissure cracks + sickly-green drip stains + purple-red rune etchings (shape-only). Flames burn sulfurous orange instead of torch-amber. | `impaled-corpse-spike` (64 × 128) |
| `sky_temple` | *"Sky temple biome: pale cloud-white (#ecf0ff) base, sky blue (#8ed6ff) veining, gold (#f5c86b) trim, marble."* | White marble + gold-trim fittings + sky-blue crystal accents + cloud-mist wisps at bases (shape-only) + candle-amber flame. Chain-torch becomes "gold chandelier chain with crystal pendants." | `crystal-cluster` (64 × 128) + `cloud-pedestal` (64 × 64) |
| `volcano` | *"Volcano biome: obsidian black (#1a1a20) base, molten orange/red (#d47a20 / #ff6f6f) cracks, ash-gray (#6b6f7a) ridges, rusted metal brown."* | Obsidian slabs + rusted iron + molten-orange crack glow + ash-gray ridge edges. Flames burn molten-orange. Altar has a central molten-glow pool instead of blood. | `obsidian-slab` (64 × 64) + `rusted-cart` (128 × 96, multi-tile) |
| `water` | *"Water biome: deep aquamarine (#2a5a6e) base, pale cyan (#8ed6ff) foam, algae green (#4a7a4a) accents, wet stone sheen."* | Wet dark stones + bronze-barnacle-crusted iron + algae-green patches + pale-cyan foam highlights + seaweed tendrils (shape-only). Flames replaced with pale-cyan bioluminescent glow on light props. Chains are bronze with barnacle crust. | `seaweed-pile` (64 × 64) + `shell-pile` (64 × 64) |
| `town` | *"Town biome: warm wood browns (#5a3a20), cobbled path gray (#6b6f7a), green grass (#4a7a4a) patches, brass-gold (#f5c86b) trim."* | Warm wood + brass-gold fittings + grass patches at bases + candle-amber flame (cheerful, not grim). Altar reframed as a well or fountain; shrine-statue reframed as a sign post; gargoyle reframed as a weathervane. | `market-stall` (128 × 96, multi-tile) + `sign-post` (64 × 128) + `barrel` (64 × 64) |

**Worked Example 1 — Cathedral `altar` (substitute into Dungeon §3.8):**

```
True isometric 2:1 dimetric perspective, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A cathedral altar rendered in cartoonish isometric pixel-art style, ornate sanctuary slab on a stepped marble base. A wide rectangular altar top carved from pale marble with a gold-trim inlay running around the edge, a shallow channel down the middle with a thin candle-amber highlight along it. Two-step chunky marble base beneath, bronze ring mounts at the corners. Candle-amber glow on the front edge, stained-glass-blue accent pixels on the base, faint shape-only angel-wing motifs carved around the altar's side faces (non-letter). Slightly exaggerated cartoonish bevels — readable chunky silhouette covering two floor diamonds (east-west orientation). Object bottom-center, occupies the lower ~90% of the canvas, transparent background, no ground shadow baked in.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.
Cathedral biome: pale marble (#c8c0b0) base, warm gold (#f5c86b) trim, stained-glass blue (#8ed6ff) accents, candle-amber glow.

No top-down view, no side-view, no three-quarter front-on view, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no off-perspective angles, no firearms, no modern clothing. No background terrain, no ground, no floor tile — transparent PNG only.
```

**Worked Example 2 — Volcano `pillar-plain` (substitute into Dungeon §3.5):** Dungeon motif "dark blue-gray cobblestone + moss-green creep + torch-amber highlight" → **"obsidian-black slabs + rusted iron banding + molten-orange crack glow in one course + ash-gray ridges on the capital."** Palette hook swaps to the Volcano line. Everything else identical.

**Worked Example 3 — Sky_temple `pew-bench`** (biome-unique decor, Cathedral-adjacent — sky_temple also gets pews per its ethereal-cathedral vibe; author by taking the Cathedral `pew-bench` prompt and swapping the palette hook to Sky_temple + changing "bronze" → "gold" and "candle-amber" → "cloud-mist pale-blue").

**Worked Example 4 — Nether `shrine-statue`:** Dungeon motif "hooded-figure idol + moss-green + torch-amber" → **"bleeding-stone horned demon idol + sickly-green drip + sulfurous-orange fissure glow + purple-red shape-only rune etchings on the pedestal."** Palette hook swaps to the Nether line. Everything else identical.

**Palette-override discipline.** Same rule as [tile-pipeline.md §3](tile-pipeline.md): if a generated `dungeon_dark` object includes any moss-green or torch-amber, it is a re-gen. Palette hooks are load-bearing.

### 6. Asset Directory Layout

Per-family root with per-biome subdirectories. Slot ID is the filename stem.

```
assets/objects/
  light/
    dungeon/
      torch-wall.png          brazier-floor.png
      chain-torch-hanging.png candelabra.png
      metadata.json
    dungeon_dark/             # same 4 files + metadata.json
    cathedral/                # 4 files (chain-torch-hanging → "chandelier-chain.png")
    nether/
    sky_temple/
    volcano/
    water/
    town/
  structural/
    dungeon/
      pillar-plain.png        pillar-carved.png       rubble-pile.png
      metadata.json
    dungeon_dark/ cathedral/ nether/ sky_temple/ volcano/ water/ town/
  sacred/
    dungeon/
      altar.png               shrine-statue.png       gargoyle-statue.png
      metadata.json
    dungeon_dark/ cathedral/ nether/ sky_temple/ volcano/ water/ town/
    # NOTE: town/ reframes — altar.png → fountain.png; shrine-statue.png → sign-post.png;
    # gargoyle-statue.png → weathervane.png (town gets no grim sacred props)
  hazard/
    dungeon/
      lava-geyser.png         poison-pool.png
      fire-tile.png           water-puddle.png
      metadata.json
    # Per-biome subdirs ONLY where the hazard semantically fits:
    volcano/      lava-geyser.png + fire-tile.png
    nether/       lava-geyser.png + fire-tile.png + poison-pool.png (sickly-green)
    water/        water-puddle.png (larger, algae variant)
    cathedral/    candle-fire.png  (a "fire-tile" reskin — candle spill)
    # dungeon_dark/sky_temple/town NOT populated for hazard — no biome fit.
  decor/
    dungeon/
      bones-pile.png          chains-wall.png
      metadata.json
    dungeon_dark/
      bones-pile.png          chains-wall.png         skull-impaled-on-spike.png
    cathedral/
      pew-bench.png           prayer-beads-pile.png
    nether/
      bones-pile.png          chains-wall.png         impaled-corpse-spike.png
    sky_temple/
      crystal-cluster.png     cloud-pedestal.png
    volcano/
      obsidian-slab.png       rusted-cart.png
    water/
      seaweed-pile.png        shell-pile.png
    town/
      market-stall.png        sign-post.png           barrel.png
```

**Rationale for family-first layout.** Godot scene-tree code paths load objects by **family + biome** far more often than by biome alone (a spawner for "all light props in current biome" reads `assets/objects/light/<biome>/*.png`). Family-first keeps that load path one directory deep. Biome-first would require globbing across five family dirs per biome lookup.

**Per-biome `metadata.json` (per family):** auto-authored by the ART-13 implementation alongside the sprite downloads. Shape:

```json
{
  "family": "sacred",
  "biome": "cathedral",
  "seed": <locked-biome-seed>,
  "generated_at": "2026-04-17T…",
  "slot_count": 3,
  "objects": {
    "altar":           { "width": 128, "height": 96,  "multi_tile_cells": [[0,0],[1,0]], "anchor_offset": [0, -64] },
    "shrine-statue":   { "width": 64,  "height": 128, "anchor_offset": [0, -80] },
    "gargoyle-statue": { "width": 64,  "height": 128, "anchor_offset": [0, -80] }
  }
}
```

**Download protocol:** use `curl --fail -o assets/objects/<family>/<biome>/<slot-id>.png "<pixellab_url>"` per [art-lead CLAUDE.md guidance](../../.claude/agents/art-lead/CLAUDE.md). Verify file sizes > 0 and dimensions match the family spec (`sips -g pixelWidth -g pixelHeight`) before committing.

### 7. Hazard-Redraw Migration

The existing `assets/effects/` directory ([asset-inventory.md §Bucket K](asset-inventory.md)) holds 18 visual-effect files. Six of them are semantically **map objects** (persistent floor-anchored props), not **particle/overlay effects** (transient screen-space FX). Those six move into the object pipeline; the other twelve stay in `assets/effects/` under the future `ART-SPEC-FX-01` ticket.

**Re-categorize as map objects (move to `assets/objects/hazard/` or `assets/objects/light/`):**

| Current path | New path | Family | Rationale |
|---|---|---|---|
| `assets/effects/torch.png` | `assets/objects/light/dungeon/torch-wall.png` (and per-biome reskins) | light | Persistent wall-mounted prop, bottom-center-anchored, iso-diamond alignment required. |
| `assets/effects/lava_ground.png` | `assets/objects/hazard/volcano/lava-geyser.png` (re-gen per §3.11) | hazard | Floor-hugging persistent hazard. Must align to iso diamond footprint. |
| `assets/effects/water_puddle.png` | `assets/objects/hazard/dungeon/water-puddle.png` (re-gen per §3.14) | hazard | Persistent floor-hugging hazard. |
| `assets/effects/fire_tile.png` | `assets/objects/hazard/dungeon/fire-tile.png` (re-gen per §3.13) | hazard | Persistent floor-hugging hazard. |
| `assets/effects/poison_pool.png` | `assets/objects/hazard/dungeon/poison-pool.png` (re-gen per §3.12) | hazard | Persistent floor-hugging hazard. |
| `assets/effects/ice_ground.png` | `assets/objects/hazard/water/ice-patch.png` (new prompt per §5 water biome, slot added to §4 as decor variant) | hazard | Persistent floor-hugging hazard; currently categorized in-repo as effect but behaves as ground-anchored prop. |

**Stay as visual FX (defer to `ART-SPEC-FX-01`):** `dust_debris.png`, `explosion.png`, `heal_aura.png`, `shadow_void.png`, `sparkle.png`, `shield_bubble.png`, `lightning_strike.png`, `magic_circle.png`, `nether_wisp.png`, `poison_cloud.png`, `volcanic_ash.png`, `cathedral_light.png`. These are transient screen-space overlays, not iso-anchored props.

**Migration protocol:**

1. ART-13's first commit **deletes** the six files listed above from `assets/effects/` (git preserves history).
2. ART-13 regenerates them under the new paths per the object-pipeline prompts in §3 (hazards) and §3.1 (torch).
3. Code references (`Constants.cs` effect paths, `Scene/Effect.tscn` Instantiate calls) are updated to new paths in the same PR. This is in-scope for ART-13 — the art migration must rewire its own code callsites.
4. The remaining twelve `assets/effects/` files stay untouched until `ART-SPEC-FX-01` is opened.

**Decision:** the hazard-redraw split above is **resolved** as of this spec lock. No further ambiguity. If a future effect needs ground-anchoring, add it to the hazard family here; if a current object feels like an overlay, it was mis-categorized and flag a spec revision.

### 8. Delete-Before-Regen Hook

Per [asset-inventory.md §Bucket E](asset-inventory.md) (empty placeholder — objects are net-new) and §7 above:

- **Bucket E:** nothing to delete on first run. Directory `assets/objects/` does not exist yet. ART-13's first commit creates it.
- **Bucket K partial:** delete the six files listed in §7 migration table before regenerating them under `assets/objects/hazard/` and `assets/objects/light/`.

Subsequent ART-13 re-runs (v2 biome additions, hero-prop re-gens) follow the same delete-first protocol: delete the entire family+biome subdirectory (`assets/objects/<family>/<biome>/`) in commit 1, regenerate in commit 2.

### 9. Implementation Notes

- **Family defaults > per-object tuning.** `shading` and `detail` are set at family granularity (§1); only sacred and carved-structural slots override to `detailed` / `high detail`. Do not per-prompt-tune other slots — batch consistency matters more than marginal slot polish.
- **Seed lock is the single most important consistency lever.** Pick the seed from the first successful object in a biome, record it in every family's `metadata.json` for that biome, reuse for every other slot. Same discipline as [tile-pipeline.md §7](tile-pipeline.md) — drift between objects comes mostly from seed drift, not prompt drift.
- **Town biome semantic reframe.** The sacred family's three slots map to non-grim props in Town (altar → fountain, shrine-statue → sign-post, gargoyle → weathervane). This is the one per-biome slot-reframe; everything else uses identical slot IDs across biomes.
- **Hazard family is biome-sparse.** Not every biome populates every hazard slot — see §6 directory layout. A dungeon_dark lava geyser is nonsensical; only populate hazard slots where the biome semantically fits.
- **Multi-tile anchor rule** (altar, market-stall, rusted-cart): anchor at the southernmost floor cell the object covers, per SPEC-ISO-01 §Z-Ordering. This is enforced by `Sprite2D.offset` in the imported .tscn, not at runtime.
- **No ground shadow baked in.** Every prompt closes with "transparent background, no ground shadow baked in" — the `Entities` Y-sort + the floor tile's own shading handle visual grounding. Baked shadows would fight the per-biome floor palette.

## Acceptance Criteria

- [ ] Copy-paste any Dungeon object prompt from §3 into PixelLab's `create_map_object` → get a valid sprite matching the family canvas spec (§1).
- [ ] Per-biome substitution (§5) produces a valid, visually distinct sprite for at least four worked examples (Cathedral altar, Volcano pillar-plain, Sky_temple pew-bench, Nether shrine-statue).
- [ ] All 16 Dungeon slots (§4 table) have complete prompts in §3.
- [ ] Zero named-IP terms in any prompt — no game titles, studio names, franchise character/spell names. Only "stone-catacomb aesthetic" / "classic dungeon-crawler palette" / "classic iso RPG genre" phrasing.
- [ ] Asset directory layout (§6) unambiguously names the target path for every slot × biome combination (family-first, biome-subdir under family, slot-id as filename stem).
- [ ] `dungeon_dark` palette hook is a strict override — generated dungeon_dark objects contain no moss-green and no torch-amber.
- [ ] Per-biome `seed` is locked at first object and reused across all slots for that biome — `metadata.json` records it.
- [ ] Hazard-effects migration (§7) is a documented, resolved decision: six files listed explicitly for re-categorization, twelve files listed explicitly as staying FX.
- [ ] ART-13 PR commit 1 deletes the six migration files listed in §7 before any new object is generated.
- [ ] Multi-tile objects (altar, market-stall, rusted-cart) have `multi_tile_cells` and `anchor_offset` entries in their family metadata.

## Open Questions

None at lock time.
