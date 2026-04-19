# LPC Sprite Recipes (MVP Characters)

**Purpose:** reproducible click-through recipes for generating our 6 MVP character sheets via the LPC Universal Spritesheet Character Generator. Anyone can re-run the recipe and produce the same (or closely-matching) sheet without negotiation.

**Generator (preferred — local, offline, full layer library):**

```bash
cd tools/lpc-generator
npm install         # one-time
npm run dev         # starts Vite dev server, opens http://localhost:<port>
```

The clone lives at `tools/lpc-generator/` (in `.gitignore` — ~855 MB, local only). Upstream: <https://github.com/liberatedpixelcup/Universal-LPC-Spritesheet-Character-Generator>.

**Fallback (no npm / quick check):** hosted version at <https://liberatedpixelcup.github.io/Universal-LPC-Spritesheet-Character-Generator/>. Same UI, same layers, slight hosted-version drift possible.

**Export workflow (per character):**

1. Open the generator. Pick layers in the left column per the recipe below.
2. Click **Download PNG** → full spritesheet (walk / slash / thrust / spellcast / shoot / hurt, 13 cols × ~30 rows, 64×64 per frame).
3. Click **Download credits (CSV or TXT)** — only needs to be saved once at the end, aggregated into `assets/characters/CREDITS.md`.
4. Save the PNG to the `Save as:` path listed in each recipe.

**Authored:** 2026-04-18 (paired with [ADR-007](../../docs/decisions/007-top-down-oga-pivot.md) — top-down + OGA pivot).

---

## Recipe: Warrior (player class)

**Feel:** heavy plate armor, sword + shield, stoic.

| Layer      | Pick                                                           |
| ---------- | -------------------------------------------------------------- |
| Body       | Male · light/tan skin                                          |
| Hair       | Short (e.g. "Bangs Short" or "Messy 2") · dark brown           |
| Beard      | Optional · short stubble                                       |
| Eyes       | Any                                                            |
| Legs       | Armored pants · steel/grey                                     |
| Feet       | Plate boots · steel                                            |
| Torso      | Plate / Chainmail · steel                                      |
| Shoulders  | Plate pauldrons                                                |
| Neck       | Gorget (optional)                                              |
| Head       | Helmet · plate crown (leave visor up so the face still reads)  |
| Cape       | Optional · deep red or none                                    |
| Weapon (L) | Shield · heater, wood+steel                                    |
| Weapon (R) | Longsword · steel                                              |

**Save as:** `assets/characters/player/warrior/warrior_full_sheet.png`

---

## Recipe: Ranger (player class)

**Feel:** light leather, hooded, bow.

| Layer      | Pick                                             |
| ---------- | ------------------------------------------------ |
| Body       | Female · light/olive skin (or male — your call)  |
| Hair       | Long · braid or ponytail · dark brown            |
| Eyes       | Green                                            |
| Legs       | Pants · forest green or brown                    |
| Feet       | Leather boots · brown                            |
| Torso      | Leather jerkin / tunic · green or tan            |
| Belt       | Leather belt                                     |
| Cape       | Hooded cloak · dark green (key identity piece)   |
| Weapon (R) | Bow · longbow or recurve · wood                  |
| Quiver     | Back quiver (if available)                       |

**Save as:** `assets/characters/player/ranger/ranger_full_sheet.png`

---

## Recipe: Mage (player class)

**Feel:** long robe, hood, staff.

| Layer      | Pick                                                      |
| ---------- | --------------------------------------------------------- |
| Body       | Male or female — your call                                |
| Hair       | Long · silver or white (elder mage) OR hidden under hood  |
| Beard      | Optional · long grey (if male elder vibe)                 |
| Eyes       | Blue                                                      |
| Legs       | Robe bottom · royal blue or purple (no visible pants)     |
| Feet       | Cloth shoes / sandals                                     |
| Torso      | Robe top · matching blue/purple                           |
| Cape       | Hood up · matching color                                  |
| Belt       | Rope belt · tan                                           |
| Weapon (R) | Staff · wood with crystal top (any staff variant)         |

**Save as:** `assets/characters/player/mage/mage_full_sheet.png`

---

## Recipe: Blacksmith (NPC)

**Feel:** burly, apron, hammer. **Color theme: black** — dark apron, black beard, blackened-leather accents.

| Layer      | Pick                                                        |
| ---------- | ----------------------------------------------------------- |
| Body       | Male · muscular (if available) · tan/weathered skin         |
| Hair       | Short or bald                                               |
| Beard      | Full beard · brown/black (key identity)                     |
| Legs       | Pants · brown/dark leather                                  |
| Feet       | Heavy boots                                                 |
| Torso      | Leather apron (if available) over bare chest OR brown tunic |
| Wrists     | Leather bracers                                             |
| Weapon (R) | Hammer · blacksmith hammer or maul                          |

**Save as:** `assets/characters/npcs/blacksmith/blacksmith_full_sheet.png`

---

## Recipe: Guild Maid (NPC)

**Feel:** clean service uniform, cheerful, unarmed. **Color theme: yellow / gold** — gold trim, blonde hair, yellow bodice or sash.

| Layer       | Pick                                                               |
| ----------- | ------------------------------------------------------------------ |
| Body        | Female · fair skin                                                 |
| Hair        | Medium · pinned-up or braid · black or blonde                      |
| Eyes        | Any                                                                |
| Legs        | Long skirt · black or dark grey                                    |
| Feet        | Flat shoes · black                                                 |
| Torso       | White blouse + dark vest OR simple dress · black and white palette |
| Accessories | Apron (white) if available — the defining piece                    |
| Weapon      | **None** — leave unarmed                                           |

**Save as:** `assets/characters/npcs/guild_maid/guild_maid_full_sheet.png`

---

## Recipe: Village Chief (NPC)

**Feel:** elder, robed, walking staff. **Color theme: white** — white/silver robes, white beard, white hair.

| Layer       | Pick                                                            |
| ----------- | --------------------------------------------------------------- |
| Body        | Male · pale/weathered skin                                      |
| Hair        | Long grey or white (elderly)                                    |
| Beard       | Long grey (key identity)                                        |
| Eyes        | Any                                                             |
| Legs        | Robe bottom · muted earth tones (brown, tan, deep green)        |
| Feet        | Cloth shoes                                                     |
| Torso       | Robe top · matching earth tone                                  |
| Accessories | Amulet / sash (optional)                                        |
| Weapon (R)  | Walking staff · plain wood (distinct from Mage's crystal staff) |

**Save as:** `assets/characters/npcs/village_chief/village_chief_full_sheet.png`

---

## Notes on re-generation

- **Layer names drift** between generator forks/versions. If a recipe calls for a layer that no longer appears in the UI, pick the closest available match — the "feel" column is the source of truth, not the literal layer name.
- **Color choices** are deliberately loose where style tolerates it; the recipes lock only the silhouette-defining layers (armor / robe / weapon).
- **Hooded variants:** LPC hoods are separate layers and often cover hair entirely — you can skip the hair layer when the hood is up.
- **After regeneration:** always re-download the credits TXT and update `assets/characters/CREDITS.md`, since the contributor list may shift based on which layers were selected.
