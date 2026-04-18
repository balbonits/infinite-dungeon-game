# Portrait Pipeline — Class + NPC Portraits (ART-SPEC-09)

## Summary

The generation-side spec for all UI portrait art in the game. Covers two deliverables:

1. **ART-08 — 3 class portraits** (Warrior / Ranger / Mage) at 256×384, consumed by the Class Select screen, the Splash screen, and the Load Game CharacterCard.
2. **ART-09 — 6 NPC dialogue portraits** (Blacksmith / Guild Maid / Village Chief × neutral + conversational) at 256×256, consumed by the `DialogueBox` portrait slot.

This spec extends both the `PORTRAIT-CLASS` and `PORTRAIT-NPC` blocks from [ART-SPEC-01 v2](prompt-templates.md) (commit `5e9e70f`). It inherits the universal preamble's cartoonish style vocabulary, palette clause, and IP protections but **overrides the iso perspective clause** — portraits are 2D UI art, not isometric.

This spec also resolves the deferred `splash_background.png` decision flagged in [asset-inventory.md](asset-inventory.md) Bucket J. Locked decision: **remove `splash_background.png`** and let `SplashScreen.cs` render a compositional dark-gradient backdrop behind the class portraits + logo (new impl ticket `SPLASH-BG-REMOVE-01`).

Paired design specs feed directly into the identity fill:
- [SPEC-PC-ART-01](../world/player-classes-art.md) for class silhouette + color-accent + defining-feature constraints.
- [SPEC-NPC-ART-01](../world/npc-art.md) for NPC identity + defining-feature + service-readability constraints.

## Current State

No portrait assets exist in the repo today. `assets/ui/splash_background.png` + `.import` exist but are slated for removal. The Splash screen currently composites upscaled rotation sprites via `CharacterCard.cs`; those will be replaced with the 3 class portraits this spec authors. The `DialogueBox` portrait slot is currently empty / placeholder and will be fed by the 6 NPC portraits this spec authors.

Per [asset-inventory.md](asset-inventory.md) Bucket J: first-regen bucket, no prior portraits to delete; plus the `splash_background.png` removal.

## Design

### 1. Portraits Are Not Isometric (explicit override)

Class and NPC portraits live in **2D UI only** — the DialogueBox, the Splash screen, the Class Select card, and the Load Game CharacterCard. They do not render in the iso game world. They do not get an iso perspective. They do not get a Godot `Sprite2D.offset` anchor (they are `TextureRect` children inside Control containers).

Portraits **inherit** from ART-SPEC-01's universal style vocabulary:
- Cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions.
- Gritty dark fantasy dungeon-crawler palette (deep blue-gray, cool shadows, warm gold + torch-amber accents, player-blue + danger-red reserved).
- Single-color black outline on characters.
- Pixel-perfect (no anti-aliasing, no smoothing, no sub-pixel shading).

Portraits **override** from ART-SPEC-01:
- The preamble's `"True isometric 2:1 dimetric perspective"` clause is replaced with:
  - Class portraits: `"2D UI portrait, three-quarter facing view"`.
  - NPC portraits: `"2D UI bust portrait, head-on to three-quarter view"`.
- The negative-prompt clause loses its three wrong-iso-perspective exclusions (`no top-down view, no side-view, no three-quarter front-on view`) and instead explicitly excludes iso perspective and full-body action poses (for bust portraits) — see §6 full prompts.

All other ART-SPEC-01 clauses carry over verbatim (palette clamp, cartoonish qualifier, IP Protection §11, pixel-perfect pixel art).

---

### 2. Class Portrait Spec (PORTRAIT-CLASS, 256×384)

**Canvas and framing:**
- 256×384 portrait aspect, transparent background (UI composites its own frame).
- Hero occupies ~85% of canvas height: head near the top edge (with 8–12 px clearance), feet/hem at the bottom edge.
- **Three-quarter facing pose** (NOT head-on flat, NOT side-view) — the hero's body is angled ~30° off the viewer's axis so both shoulders read, with the dominant hand / weapon side nearer the viewer.

**Silhouette and identity carry from [SPEC-PC-ART-01](../world/player-classes-art.md) §§6–8:**
- Warrior: horned full helm + pauldrons + cuirass + shield (held at the side) + longsword + surcoat with player-blue accent.
- Ranger: drawn-up soft hood + leather jerkin + bracers + shortbow held at the side (half-drawn acceptable for visual drama) + quiver strap + player-blue hood lining.
- Mage: pointed wizard hat + long hooded robe + gnarled staff held vertically with player-blue crystal staff-head + belt + spellbook pouch.

Each class portrait must be instantly recognizable as the same character as its corresponding world sprite — same silhouette cue, same palette, same defining features, just at higher detail and in a dramatic hero framing.

**Background:**
- Subtle colored radial gradient matching the class-signature accent color, fading to near-black at the canvas edges.
  - Warrior gradient: deep warm red / rust (`#6b2a2a` center → `#1a1a22` edge).
  - Ranger gradient: deep muted forest green (`#2a6b3a` center → `#1a1a22` edge).
  - Mage gradient: deep player-adjacent blue (`#2a3a6b` center → `#1a1a22` edge; must NOT be `#8ed6ff` — the player-blue stays on the staff crystal, the background is a dimmer cousin).
- **No detailed scenery** — no dungeon walls, no horizon, no fog, no floor. The gradient keeps the canvas clean for UI compositing over any background the Splash / ClassSelect / CharacterCard may use.

**Cartoonish qualifier applies.** Slightly exaggerated heroic proportions (head ~1/6 total height, shoulders broad for Warrior / narrow for Ranger / robe-hidden for Mage), readable features at bust-shot scale. NOT photorealistic concept art, NOT painterly — cartoonish pixel art with bold outlines. This is load-bearing: without the qualifier, PixelLab drifts toward realism at 256×384 resolution because the canvas is large enough to support it.

**PixelLab params (per ART-SPEC-01 PORTRAIT-CLASS block):**

| Param | Value |
|-------|-------|
| Tool | `create_map_object` |
| `width` × `height` | **256 × 384** |
| `view` | `side` |
| `outline` | `single color black outline` |
| `shading` | `detailed shading` |
| `detail` | `high detail` |
| `ai_freedom` | `500` (consistency across the 3-portrait batch is critical) |

Three full copy-paste prompts in §6.

---

### 3. NPC Portrait Spec (PORTRAIT-NPC, 256×256)

**Canvas and framing:**
- 256×256 square aspect, transparent background.
- **Bust shot only**: head + shoulders + upper chest. No arms below the elbow, no hands, no waist, no legs.
- Crown of the head near the top edge (8–12 px clearance). Chin at ~70% canvas height. Shoulders / upper chest fill the bottom 30%.
- **Head-on to slight three-quarter view** (≤20° off-axis). NPCs are addressing the player across a dialogue box — direct gaze reads as conversational.

**Identity carries from [SPEC-NPC-ART-01](../world/npc-art.md) §§6–8:**
- **Blacksmith**: soot-streaked weathered face, bareheaded, rolled-sleeve collar, single horn-scale pauldron visible on the off-shoulder (the adventuring-days memento) with its bronze tie-band, no hat/hood. Age 45–55.
- **Guild Maid**: neat young face, hair tied back (low bun), teleport pendant at the throat (sage green or deep red — NOT player-blue), visible cream/off-white apron collar over deep navy dress. Age 20–30.
- **Village Chief**: elderly weathered face with long white beard (or long white hair if beardless is the PixelLab default), silver chain-of-office across the chest (visible at the collarbone), soft hood resting on the shoulders (NOT raised, NOT pointed). Age 60–80.

**Two expression variants per NPC:**
- **Neutral** (default; shown when the DialogueBox opens and during listening pauses): relaxed mouth closed, relaxed brow, eyes forward.
- **Conversational** (shown during NPC lines): slight expression — mouth parted mid-speech, subtle brow lift, eyes still forward. Same face, same hair, same attire, same framing — only the mouth and brow change.

**Out of scope (deferred to a later ticket if dialogue needs emotional range):** sad, angry, shocked, happy, afraid. Do NOT generate these in this batch. Adding two variants per NPC × 3 NPCs = 6 portraits already. Expanding to a 5-emotion set would be 15 portraits for a feature the current dialogue system does not consume.

**Background:**
- Darker gradient than class portraits, **neutral** (no class-signature color — the NPC is not competing with the PC's color identity).
- Dark blue-gray radial gradient: center `#2a3040`, edge `#14181f`. Uniform across all 3 NPCs × 2 expressions so the DialogueBox reads consistently.
- No scenery, no dungeon detail, no workshop detail. The dialogue box owns the framing; the portrait is a clean bust on a neutral backdrop.

**Cartoonish qualifier applies.** Bold readable facial features (eyes, brows, mouth) at bust-shot scale; slightly exaggerated age cues (Blacksmith's soot + stubble, Guild Maid's youthful cheek / neat hair, Village Chief's deep wrinkles + white beard). NOT photorealistic portrait painting.

**No player-blue anywhere** on any NPC portrait (per [SPEC-NPC-ART-01](../world/npc-art.md) §4). Hard constraint, repeats for emphasis: the Guild Maid's teleport pendant is sage green or deep red, NOT cyan/blue — preserving PC color identity.

**PixelLab params (per ART-SPEC-01 PORTRAIT-NPC block):**

| Param | Value |
|-------|-------|
| Tool | `create_map_object` |
| `width` × `height` | **256 × 256** |
| `view` | `side` |
| `outline` | `single color black outline` |
| `shading` | `detailed shading` |
| `detail` | `high detail` |

Six full copy-paste prompts in §6.

---

### 4. Splash Background Decision (locked: Option 3 — Remove)

`assets/ui/splash_background.png` + `.import` — flagged in [asset-inventory.md](asset-inventory.md) Bucket J as decision-deferred-to-ART-SPEC-09-time. Options analyzed:

| Option | Action | Pro | Con |
|---|---|---|---|
| 1 | Keep as-is | Smallest scope | Style mismatch with redrawn UI + class portraits — existing `splash_background.png` is pre-iso-pivot art, will clash visually |
| 2 | Redraw as widescreen backdrop (dungeon entrance / hero montage) | Most visual richness | Fights the 3 class portraits for attention; another 1-off asset to maintain; likely style drift from the clean portrait set |
| 3 | **Remove** — render a plain dark-gradient via code | Simplest; class portraits carry the visual weight; fewer assets to maintain; guaranteed cohesion | Splash screen background is purely compositional — no illustrated backdrop |

**Locked decision: Option 3 (Remove).**

**Rationale:**
- The 3 class portraits (256×384 each, detailed shading, high detail) are the visual weight of the Splash screen. A separate illustrated background would compete for attention rather than frame the portraits.
- A compositional gradient rendered in code is guaranteed to match whatever palette the class portraits use (since both key off the same `ui-theme.md` hex anchors) — no style drift risk.
- Fewer assets in the repo = fewer assets to maintain when the style evolves.
- Precedent: the HP/MP orbs are also compositional (Modulate fill on a sphere art) — the Splash backdrop follows the same pattern.

**Impl ticket stub: `SPLASH-BG-REMOVE-01`** — see §9 for the detailed stub.

---

### 5. Expression Variants — How Art-Lead Handles

**The question.** Can PixelLab generate `neutral` + `conversational` variants of the same NPC portrait from a single `create_character` call (via an `animate_character` template for expression change), or does it require two separate `create_map_object` calls?

**Answer: two separate `create_map_object` calls per NPC**, identity-locked by careful prompt parity.

**Why not `animate_character`:**
- Portraits use `create_map_object` (static 2D UI art), not `create_character` (skeletal iso sprite). `animate_character` consumes a character-body skeleton that was built by `create_character`; portraits have no skeleton.
- PixelLab's humanoid animation templates are body-motion (walk, run, punch, cast), not facial-expression templates. There is no "expression swap" animation template.
- Attempting to force-fit would cost 20–40 generations per variant (custom animation cost per tool docs) vs. 1 per variant at `create_map_object` — economically indefensible.

**How to lock identity parity between the two variants:**

Both variant prompts for a given NPC MUST share the following segments verbatim:
- Preamble (with portrait override clause).
- Identity-fill segment: role, face description, attire, defining feature, hair, age cue, palette-band.
- Background clause.
- Palette clause.
- Negative-prompt clause.

They differ ONLY in the expression segment:
- Neutral: `"Expression: neutral, mouth closed, relaxed brow, eyes forward."`
- Conversational: `"Expression: conversational, mouth parted mid-speech, subtle brow lift, eyes forward."`

**Consistency check (acceptance gate — see §12):** place both variants side-by-side at 256×256. A reviewer with no prior context must identify them as the same NPC at 100% accuracy. If the identity drifts (different facial features, different attire, different hair), the identity-fill segment did not carry — regenerate with a tighter prompt or an explicit `ai_freedom` reduction.

If PixelLab's `create_map_object` cannot produce sufficiently identity-consistent variants even with verbatim prompt segments, the fallback is to generate one high-quality base portrait per NPC and mark the `conversational` variant as deferred to a future image-editing pass (outside PixelLab). Flag this fallback in the impl ticket if it occurs.

---

### 6. Full Copy-Paste Prompts (9 total)

Each prompt is presented as a single copy-paste block. The preamble opens with the portrait-specific perspective override clause (NOT the iso preamble). The palette clause and negative-prompt clause are inherited verbatim from ART-SPEC-01 §1c + §1 with portrait-specific additions noted inline. Zero IP violations — no named games, no named characters, no named franchises. Cartoonish qualifier appears in every preamble.

**Note on the preamble override:** the first sentence of each portrait preamble below replaces ART-SPEC-01's `"True isometric 2:1 dimetric perspective"` with the portrait-appropriate view ("2D UI portrait, three-quarter facing view" for class portraits; "2D UI bust portrait, head-on to three-quarter view" for NPC portraits). Everything else in the preamble (cartoonish qualifier, bold silhouettes, palette, outline, pixel-perfect) is verbatim from ART-SPEC-01 §1.

---

#### 6.1 Class — Warrior (256×384)

```
2D UI portrait, three-quarter facing view, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A warrior full hero portrait, in the style of a classic dungeon-crawler class-select splash. Stocky and armored build, broad shoulders with exaggerated pauldrons. Three-quarter facing pose: body angled ~30 degrees off the viewer's axis, dominant hand forward. Wearing a plate cuirass with steel pauldrons, greaves, a horned full helm covering the face with two upward-curving horns, and a cloth surcoat visible between the cuirass and belt. Right hand holds a steel longsword angled upward near the shoulder; left arm holds a round metal shield with an iron rim at the side. Defining feature: horned helmet silhouette and the asymmetric shield held at the side, reading as armored mass. Color accent: player blue (#8ed6ff) baked into the visible surcoat panel beneath the cuirass — this is the hero's class signature. Hero occupies ~85% of canvas height, head near top with 8-12 pixels clearance, feet at the bottom edge. Background: subtle radial gradient of deep warm red (#6b2a2a) center fading to near-black (#1a1a22) at the canvas edges, no scenery, no detailed dungeon background.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No iso perspective, no three-quarter top-down angle, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no firearms, no modern clothing, no environmental scenery, no dungeon walls in background, no fog, no floor, no horizon — direct hero portrait, front-facing three-quarter view, clean gradient background only.
```

---

#### 6.2 Class — Ranger (256×384)

```
2D UI portrait, three-quarter facing view, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A ranger full hero portrait, in the style of a classic dungeon-crawler class-select splash. Lean and poised build, narrow compact silhouette with no shoulder armor. Three-quarter facing pose: body angled ~30 degrees off the viewer's axis, dominant bow-hand forward. Wearing a leather jerkin, leather bracers, cloth leggings, soft boots, and a soft hood drawn up with a visible fold above the head. Right hand holds a shortbow at a relaxed half-draw across the body, an arrow nocked but not fully pulled; left hand grips the bowstring near the chest. A quiver strap crosses the back, visible at the off-shoulder. Defining feature: drawn-up hood + shortbow held close to the body + lean compact profile. Color accent: player blue (#8ed6ff) baked into the inner lining of the hood, visible as a thin strip where the hood meets the face. Hero occupies ~85% of canvas height, head near top with 8-12 pixels clearance, feet at the bottom edge. Background: subtle radial gradient of deep muted forest green (#2a6b3a) center fading to near-black (#1a1a22) at the canvas edges, no scenery, no detailed forest background.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No iso perspective, no three-quarter top-down angle, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no firearms, no modern clothing, no environmental scenery, no forest trees in background, no fog, no floor, no horizon — direct hero portrait, front-facing three-quarter view, clean gradient background only.
```

---

#### 6.3 Class — Mage (256×384)

```
2D UI portrait, three-quarter facing view, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A mage full hero portrait, in the style of a classic dungeon-crawler class-select splash. Wiry build hidden under robes, taller silhouette than baseline due to a pointed wizard hat extending above the headline. Three-quarter facing pose: body angled ~30 degrees off the viewer's axis, staff-hand forward. Wearing long hooded robes that reach the feet, a pointed wizard hat (iconic cone shape) worn over the hood, a leather belt with a visible spellbook pouch, and full-length robe hem flaring outward at the bottom. Right hand holds a tall gnarled wooden staff vertically along the body, staff-head reaching near hat-tip height; left hand gestures open toward the viewer in a subtle casting stance. Defining feature: pointed wizard hat + vertical staff with a carved crystal head. Color accent: player blue (#8ed6ff) baked into the staff-head crystal (a small faceted gem at the staff's apex) — this is the hero's class signature and the most prominent blue pixel on the sprite. Robe body is deep blue-gray (#24314a). Hat-tip clearance: the pointed hat must not exceed the canvas top edge — leave 8-12 pixels of canvas space above the hat tip. Hero occupies ~85% of canvas height, head near top, feet/hem at the bottom edge. Background: subtle radial gradient of deep blue (#2a3a6b) center fading to near-black (#1a1a22) at the canvas edges, no scenery, no detailed magical background.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No iso perspective, no three-quarter top-down angle, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no firearms, no modern clothing, no environmental scenery, no magical glowing background, no fog, no floor, no horizon, no floating runes — direct hero portrait, front-facing three-quarter view, clean gradient background only.
```

---

#### 6.4 NPC — Blacksmith, Neutral (256×256)

```
2D UI bust portrait, head-on to slight three-quarter view, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A blacksmith portrait bust, head and shoulders and upper chest only, in the style of a classic dungeon-crawler NPC talk portrait. Weathered middle-aged face (age range 45-55), soot-streaked across the cheeks and forehead, prominent stubble or short beard, bareheaded with short cropped dark hair. Broad shoulders visible at the bottom edge of the frame, wearing a heavy leather apron over a rolled-sleeve linen collar. Defining feature: single horn-scale pauldron on the off-shoulder, a curved bone/horn fragment lashed with leather straps and a visible bronze tie-band (the memento from adventuring days). No hat, no hood, no helmet. Expression: neutral, mouth closed, relaxed brow, eyes forward. Palette band: warm browns (apron leather), iron grey (accents), skin tone with soot smudges, bronze on the pauldron tie-band. No player blue anywhere on the sprite. Bust framing: crown of the head near the top edge with 8-12 pixels clearance, chin at ~70% canvas height, shoulders and upper chest fill the bottom 30% of the canvas. Background: dark blue-gray radial gradient (#2a3040 center to #14181f edge), neutral, no scenery, no workshop background, no forge visible.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No full-body shot, no action pose, no iso perspective, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no firearms, no modern clothing, no environmental scenery, no forge, no anvil, no hammer in the frame, no workshop background — portrait bust only on a neutral dark gradient.
```

---

#### 6.5 NPC — Blacksmith, Conversational (256×256)

```
2D UI bust portrait, head-on to slight three-quarter view, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A blacksmith portrait bust, head and shoulders and upper chest only, in the style of a classic dungeon-crawler NPC talk portrait. Weathered middle-aged face (age range 45-55), soot-streaked across the cheeks and forehead, prominent stubble or short beard, bareheaded with short cropped dark hair. Broad shoulders visible at the bottom edge of the frame, wearing a heavy leather apron over a rolled-sleeve linen collar. Defining feature: single horn-scale pauldron on the off-shoulder, a curved bone/horn fragment lashed with leather straps and a visible bronze tie-band (the memento from adventuring days). No hat, no hood, no helmet. Expression: conversational, mouth parted mid-speech, subtle brow lift, eyes forward. Palette band: warm browns (apron leather), iron grey (accents), skin tone with soot smudges, bronze on the pauldron tie-band. No player blue anywhere on the sprite. Bust framing: crown of the head near the top edge with 8-12 pixels clearance, chin at ~70% canvas height, shoulders and upper chest fill the bottom 30% of the canvas. Background: dark blue-gray radial gradient (#2a3040 center to #14181f edge), neutral, no scenery, no workshop background, no forge visible.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No full-body shot, no action pose, no iso perspective, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no firearms, no modern clothing, no environmental scenery, no forge, no anvil, no hammer in the frame, no workshop background — portrait bust only on a neutral dark gradient.
```

---

#### 6.6 NPC — Guild Maid, Neutral (256×256)

```
2D UI bust portrait, head-on to slight three-quarter view, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A guild maid portrait bust, head and shoulders and upper chest only, in the style of a classic dungeon-crawler NPC talk portrait. Neat young face (age range 20-30), smooth complexion, kind eyes, hair tied back in a low bun visible at the silhouette's upper edges. Professional posture. Wearing a cream/off-white apron collar tied at the throat over a deep navy dress, the apron strap visible over the shoulder. Defining feature: teleport pendant at the throat — a small carved sage-green or deep-red disc on a cord (NOT cyan, NOT player-blue). No hat, no hood, no pointed headwear. Expression: neutral, mouth closed, relaxed brow, eyes forward. Palette band: cream/off-white (apron), deep navy (dress), warm skin tone, brass (pendant setting). No player blue anywhere on the sprite — the teleport pendant is sage green or deep red, never cyan. Bust framing: crown of the head near the top edge with 8-12 pixels clearance, chin at ~70% canvas height, shoulders and upper chest fill the bottom 30% of the canvas. Background: dark blue-gray radial gradient (#2a3040 center to #14181f edge), neutral, no scenery, no guild hall background.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No full-body shot, no action pose, no iso perspective, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no firearms, no modern clothing, no environmental scenery, no guild hall, no ledger visible in frame, no keys visible in frame, no background architecture — portrait bust only on a neutral dark gradient.
```

---

#### 6.7 NPC — Guild Maid, Conversational (256×256)

```
2D UI bust portrait, head-on to slight three-quarter view, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A guild maid portrait bust, head and shoulders and upper chest only, in the style of a classic dungeon-crawler NPC talk portrait. Neat young face (age range 20-30), smooth complexion, kind eyes, hair tied back in a low bun visible at the silhouette's upper edges. Professional posture. Wearing a cream/off-white apron collar tied at the throat over a deep navy dress, the apron strap visible over the shoulder. Defining feature: teleport pendant at the throat — a small carved sage-green or deep-red disc on a cord (NOT cyan, NOT player-blue). No hat, no hood, no pointed headwear. Expression: conversational, mouth parted mid-speech, subtle brow lift, eyes forward. Palette band: cream/off-white (apron), deep navy (dress), warm skin tone, brass (pendant setting). No player blue anywhere on the sprite — the teleport pendant is sage green or deep red, never cyan. Bust framing: crown of the head near the top edge with 8-12 pixels clearance, chin at ~70% canvas height, shoulders and upper chest fill the bottom 30% of the canvas. Background: dark blue-gray radial gradient (#2a3040 center to #14181f edge), neutral, no scenery, no guild hall background.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No full-body shot, no action pose, no iso perspective, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no firearms, no modern clothing, no environmental scenery, no guild hall, no ledger visible in frame, no keys visible in frame, no background architecture — portrait bust only on a neutral dark gradient.
```

---

#### 6.8 NPC — Village Chief, Neutral (256×256)

```
2D UI bust portrait, head-on to slight three-quarter view, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A village chief portrait bust, head and shoulders and upper chest only, in the style of a classic dungeon-crawler NPC talk portrait. Elderly weathered face (age range 60-80), deep wrinkles, kindly wise features, long white beard reaching the collarbone OR long white hair past the shoulders with a beardless weathered face (whichever reads most elderly). A soft cloth hood or cowl rests on the shoulders (NOT raised over the head, NOT pointed). Wearing muted sage-green robes with a cream/ivory collar trim. Defining feature: silver chain-of-office across the chest — a row of 5-7 linked tarnished silver discs visible at the collarbone, the civic-authority signature. No pointed hat, no glowing staff, no crystal accessory. Expression: neutral, mouth closed, relaxed brow, eyes forward, serene authority. Palette band: muted sage green (robe body), cream/ivory (beard/hair/collar trim), tarnished silver (chain-of-office), warm skin tone with deep shadow in wrinkles. No player blue anywhere on the sprite. Bust framing: crown of the head near the top edge with 8-12 pixels clearance, chin at ~70% canvas height, shoulders and upper chest fill the bottom 30% of the canvas. Background: dark blue-gray radial gradient (#2a3040 center to #14181f edge), neutral, no scenery, no village background.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No full-body shot, no action pose, no iso perspective, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no firearms, no modern clothing, no environmental scenery, no staff visible in frame, no walking staff, no crystal, no glow, no pointed wizard hat, no village architecture — portrait bust only on a neutral dark gradient.
```

---

#### 6.9 NPC — Village Chief, Conversational (256×256)

```
2D UI bust portrait, head-on to slight three-quarter view, cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions, readable equipment and features, gritty dark fantasy dungeon-crawler palette, single-color black outline on characters, pixel-perfect pixel art (no anti-aliasing, no smoothing, no sub-pixel shading).

A village chief portrait bust, head and shoulders and upper chest only, in the style of a classic dungeon-crawler NPC talk portrait. Elderly weathered face (age range 60-80), deep wrinkles, kindly wise features, long white beard reaching the collarbone OR long white hair past the shoulders with a beardless weathered face (whichever reads most elderly). A soft cloth hood or cowl rests on the shoulders (NOT raised over the head, NOT pointed). Wearing muted sage-green robes with a cream/ivory collar trim. Defining feature: silver chain-of-office across the chest — a row of 5-7 linked tarnished silver discs visible at the collarbone, the civic-authority signature. No pointed hat, no glowing staff, no crystal accessory. Expression: conversational, mouth parted mid-speech, subtle brow lift, eyes forward, serene authority. Palette band: muted sage green (robe body), cream/ivory (beard/hair/collar trim), tarnished silver (chain-of-office), warm skin tone with deep shadow in wrinkles. No player blue anywhere on the sprite. Bust framing: crown of the head near the top edge with 8-12 pixels clearance, chin at ~70% canvas height, shoulders and upper chest fill the bottom 30% of the canvas. Background: dark blue-gray radial gradient (#2a3040 center to #14181f edge), neutral, no scenery, no village background.

Palette clamp: deep blue-gray stone tones (#24314a, #3c4664), cool shadows, warm gold (#f5c86b) and torch-amber accents only. Gritty, muted, desaturated feel — candle/torch warmth against cold stone, classic dungeon-crawler palette. Player blue accent (#8ed6ff) reserved for player class assets. Danger red (#ff6f6f) reserved for high-threat elements and dark-magic FX.

No full-body shot, no action pose, no iso perspective, no chibi, no modern stylization, no anime facial features, no text, no letters, no numbers, no logos, no watermarks, no photorealism, no firearms, no modern clothing, no environmental scenery, no staff visible in frame, no walking staff, no crystal, no glow, no pointed wizard hat, no village architecture — portrait bust only on a neutral dark gradient.
```

---

### 7. Expression Variant Handling (summary)

Recap of §5 rules applied to the 6 NPC prompts above:

| NPC | Neutral prompt §6.x | Conversational prompt §6.x | Identity-fill parity verified |
|---|---|---|---|
| Blacksmith | 6.4 | 6.5 | Same weathered-45-55, soot, apron, horn-scale pauldron + bronze tie-band, bareheaded |
| Guild Maid | 6.6 | 6.7 | Same neat-20-30, low-bun, apron + navy dress, sage-green/deep-red pendant |
| Village Chief | 6.8 | 6.9 | Same elderly-60-80, white beard/hair, chain-of-office, sage-green robes, hood rests on shoulders |

Each neutral/conversational pair differs ONLY in the Expression sentence. All other fields (face, hair, attire, palette, bust framing, background) are verbatim duplicates.

**Art-lead workflow:**
1. Generate the neutral variant first for each NPC.
2. Visually verify identity carries (face, attire, palette, silhouette).
3. Generate the conversational variant with the identical prompt except for the Expression sentence.
4. Place both variants side-by-side; verify they read as the same character.
5. If identity drifts, reduce `ai_freedom` and regenerate, OR (fallback) mark conversational as deferred per §5.

---

### 8. Asset Directory Layout

Target paths after generation:

```
assets/ui/portraits/
├── class/
│   ├── warrior.png
│   ├── ranger.png
│   ├── mage.png
│   └── metadata.json
└── npcs/
    ├── blacksmith_neutral.png
    ├── blacksmith_conversational.png
    ├── guild_maid_neutral.png
    ├── guild_maid_conversational.png
    ├── village_chief_neutral.png
    ├── village_chief_conversational.png
    └── metadata.json
```

**`metadata.json` for class portraits** (`assets/ui/portraits/class/metadata.json`):

```json
{
  "spec": "ART-SPEC-09 / PORTRAIT-CLASS",
  "canvas_width": 256,
  "canvas_height": 384,
  "view": "2D UI three-quarter facing",
  "portraits": {
    "warrior": "warrior.png",
    "ranger": "ranger.png",
    "mage": "mage.png"
  }
}
```

**`metadata.json` for NPC portraits** (`assets/ui/portraits/npcs/metadata.json`):

```json
{
  "spec": "ART-SPEC-09 / PORTRAIT-NPC",
  "canvas_width": 256,
  "canvas_height": 256,
  "view": "2D UI bust, head-on to slight three-quarter",
  "portraits": {
    "blacksmith": {
      "neutral": "blacksmith_neutral.png",
      "conversational": "blacksmith_conversational.png"
    },
    "guild_maid": {
      "neutral": "guild_maid_neutral.png",
      "conversational": "guild_maid_conversational.png"
    },
    "village_chief": {
      "neutral": "village_chief_neutral.png",
      "conversational": "village_chief_conversational.png"
    }
  }
}
```

The `expression_mapping` in the NPC metadata is the contract the `DialogueBox` reads against: request `neutral` by default, swap to `conversational` when the NPC is mid-line.

---

### 9. Splash Background Removal (SPLASH-BG-REMOVE-01 impl stub)

**Deliverable:**

1. **Asset removal** (single commit):
   - Delete `assets/ui/splash_background.png`.
   - Delete `assets/ui/splash_background.png.import`.

2. **Code change** — update `scripts/ui/SplashScreen.cs`:
   - Remove any `TextureRect` or background texture node that references `splash_background.png`.
   - Add a `ColorRect` (or two stacked `ColorRect`s for a gradient approximation) filling the root Control, rendering a dark-gradient backdrop. Recommended: a `ColorRect` at `#14181f` full-bleed, with an optional second `ColorRect` at `#2a3040` alpha-fading toward the center via a small shader or a radial-gradient texture.
   - Verify the existing class-portrait `ArtRect` / `CharacterCard` node ordering places class portraits above the new gradient backdrop.
   - Verify buttons (Class Select / Load Game / Settings / Quit) remain legible against the gradient.

3. **Scene-file update** — update `scenes/ui/SplashScreen.tscn`:
   - Remove the old background `TextureRect` node.
   - Add the new `ColorRect` node(s) as the bottom-most siblings under the root Control.

4. **Acceptance gate for the impl ticket:**
   - Splash screen renders without `splash_background.png` reference (Godot does not emit a missing-resource warning).
   - Visual result shows 3 class portraits (once ART-08 lands them) on a clean dark-gradient backdrop + game logo + buttons.
   - No style drift vs. DialogueBox (the gradient palette matches).

**Dependency order:** this impl ticket `SPLASH-BG-REMOVE-01` can land independently of ART-08 (class portraits). It does NOT block on portraits — the gradient backdrop is valid with or without portraits present. But the visual "looks right" only after ART-08 lands, so QA should sign off on SPLASH-BG-REMOVE-01 after ART-08 or in the same batch.

---

### 10. Delete-Before-Regen Protocol

Per [asset-inventory.md](asset-inventory.md) Bucket J:

- **Class portraits**: no prior assets — first regen. Nothing to delete on the class-portrait side.
- **NPC portraits**: no prior assets — first regen. Nothing to delete on the NPC-portrait side.
- **Splash background**: `assets/ui/splash_background.png` + `.import` are **removed, not replaced**. This happens as part of `SPLASH-BG-REMOVE-01` (§9), not this spec's impl ticket.

Standard ART-SPEC-* delete-before-regen rules still apply to any future portrait iterations (e.g., if ART-08 v2 redraws the class portraits, delete v1 first, then regen).

---

### 11. Cartoonish Qualifier (explicit reinforcement)

**Do not let PixelLab drift toward realism at 256-pixel resolution.** The canvas is large enough (256×384 for class, 256×256 for NPC) that PixelLab can produce painterly or photorealistic output if the cartoonish qualifier is not load-bearing in the prompt.

Every prompt in §6 opens with `"cartoonish pixel art with bold compact silhouettes and slightly exaggerated proportions"` verbatim from ART-SPEC-01 §1's universal preamble. This clause is non-negotiable.

Concretely, the portraits should look like:
- **Class portraits**: chunky heroic proportions, bold pauldrons/hat/hood, readable weapon silhouette, single-color black outlines, pixel-perfect (no anti-aliasing between color bands). NOT painterly concept art, NOT semi-realistic RPG splash art.
- **NPC portraits**: bold facial features (large readable eyes, strong brows, defined mouth), clear age cues (soot smudges, hair-tied-back silhouette, white beard + wrinkles), single-color black outlines, pixel-perfect. NOT photographic portraits, NOT painterly bust-shots.

If a generated portrait reads as realistic/painterly, regenerate with the cartoonish qualifier emphasized (e.g., add `"chunky exaggerated cartoon proportions"` as an extra beat) or reduce `ai_freedom` to `400`.

---

### 12. Acceptance Criteria

- [ ] All 9 portrait prompts (§§6.1–6.9) are full copy-paste blocks, ready to paste into `create_map_object` without editing.
- [ ] Class portraits are distinguishable from NPC portraits at a glance (hero fullbody three-quarter at 256×384 vs. head-and-shoulders bust at 256×256).
- [ ] Class portraits visually couple to the world sprites per [SPEC-PC-ART-01 §4](../world/player-classes-art.md) — a player seeing the Warrior portrait then the Warrior world sprite recognizes them as the same character.
- [ ] NPC portraits visually couple to the NPC world sprites per [SPEC-NPC-ART-01](../world/npc-art.md) defining-features — a player seeing the Blacksmith portrait then the Blacksmith sprite recognizes them as the same character.
- [ ] Each NPC's neutral + conversational variants read as the same character at 100% reviewer accuracy (§5 identity-parity check).
- [ ] No portrait carries the player-blue (`#8ed6ff`) accent on an NPC (per [SPEC-NPC-ART-01 §4](../world/npc-art.md) hard constraint). The Guild Maid pendant is sage green or deep red, never cyan.
- [ ] Class portraits carry player-blue accent only on the class-signature cloth / hood-lining / staff-head (per SPEC-PC-ART-01 §§6–8).
- [ ] All 9 prompts are free of named IP (no game titles, studio names, franchise character names, signature spell/item names). Cartoonish qualifier appears verbatim in every preamble.
- [ ] `splash_background.png` decision is documented (§4, locked Option 3 — Remove) with impl ticket `SPLASH-BG-REMOVE-01` stubbed (§9).
- [ ] Asset directory layout (§8) is followed: `assets/ui/portraits/class/{warrior,ranger,mage}.png` + `assets/ui/portraits/npcs/{blacksmith,guild_maid,village_chief}_{neutral,conversational}.png` + per-dir `metadata.json`.

## Implementation Notes

- **Portraits are 2D UI, not iso.** Do not anchor portraits via `Sprite2D.offset = -(H/2) - 16`. They are `TextureRect` children inside `Control` containers; sizing/positioning is driven by container layout, not iso anchor math.
- **Expression variants are two separate `create_map_object` calls**, not an `animate_character` job (§5). Budget 6 calls for the 6 NPC portraits + 3 calls for the 3 class portraits = 9 `create_map_object` generations total for this spec's scope.
- **Cartoonish qualifier is the top drift risk at 256-pixel canvases.** Watch for realism/painterly output; regenerate with tighter guidance if it appears (§11).
- **Player-blue on NPCs is the hardest identity rule to enforce.** The Guild Maid's teleport pendant wants to be cyan by genre instinct; the spec locks it to sage green or deep red. Reviewers must flag any cyan/blue pendant as a spec violation.
- **Splash background removal is a separate impl ticket** (`SPLASH-BG-REMOVE-01`) and does not block on this portrait spec's art generation. The two can land in either order; the visual completeness ("the Splash looks right") requires both.
- **Identity consistency across expression variants** is the likely failure mode. If PixelLab cannot produce identity-consistent variants even with verbatim prompt segments, fall back to generating the neutral portrait only and mark conversational as deferred (§5). Flag this fallback in the impl ticket.
- **Mage hat-tip clearance (8–12 px from canvas top)** mirrors the SPEC-PC-ART-01 §8 world-sprite rule, but at portrait scale the canvas is larger (384 px tall vs. 128 px), so clipping risk is lower. Still, enforce the clearance — a hat that touches the canvas top looks amateurish at splash-screen scale.
- **No named-IP in any prompt** (§11 IP Protection hard rule). All 9 prompts above use generic genre-framing terms (`"classic dungeon-crawler class-select splash"`, `"classic dungeon-crawler NPC talk portrait"`) and fresh-authored identity beats per the paired design specs. Reviewers should sweep each prompt before use to confirm zero slippage.

## Open Questions

None — spec is locked.
