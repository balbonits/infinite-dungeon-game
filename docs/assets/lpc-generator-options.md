# LPC Generator Options Reference

A practical guide for authoring recipes in `tools/lpc-generator/game-batch.mjs`.

## TL;DR

- **Silent drop is the #1 trap.** The generator accepts any hash param without validating it. If the variant file doesn't exist for the chosen `sex`, the layer is dropped from the output without warning. Always cross-check against `tools/lpc-generator/sheet_definitions/`.
- **Recipe shape:** `sex=<body>&<slot>=<Name>_<variant>`. Name comes from the sheet-definition JSON's `"name"` field; variant is either a palette key (e.g. `chestnut`) or a file-variant (e.g. `green.png` → `green`).
- **When in doubt:** run one recipe, open the per-layer `*_credits.txt` it emits, and confirm every slot you specified is listed. Missing slots = silent drop.

## The URL hash format

Recipes are passed as URL hash params:

```
http://localhost:5173/#sex=male&body=Body_Color_amber&hair=Natural_chestnut&weapon=Longsword_longsword
```

- Spaces in names become underscores — `"Round Shield"` (from `round_shield.json`'s `name` field) → `Round_Shield`.
- Palette/variant underscores stay as underscores — `Body_Color_amber` is a three-token string split as `{Name}_{palette}`.

## Body types (`sex=`)

LPC supports these body types (not all slots support all of them):

| Key | Notes |
|---|---|
| `male` | Standard adult male. Most-supported across all slots. |
| `female` | Standard adult female. Female-only clothes/apron variants exist. |
| `muscular` | Broader shoulders / bulkier torso. **Many torso layers don't have a muscular variant** — check JSON. |
| `teen` | Smaller proportions. Most hair + body works; many torso/armour variants don't. |
| `child` | Smallest proportions. Limited slot coverage. |
| `pregnant` | Pregnant body. Unique apron/clothes fits. |
| `skeleton` | Skeletal base. Very limited clothing support. |
| `zombie` | Zombie palette. Similar coverage to skeleton. |

## Slot reference

The slot key (`hair`, `torso`, `legs`, etc.) maps to a `type:` in the sheet definition. One JSON = one selectable "Name", plus all its palette/file variants.

### Slots used in current MVP recipes

| Slot | Sheet-def dir | Notes |
|---|---|---|
| `body` | `body/bodies/` | Always use `Body_Color_<palette>` (palette = light, amber, brown, olive, etc.) |
| `head` | `body/heads/human/<sex>/` | Use `Human_Male_<palette>` / `Human_Female_<palette>`. Palette must match body. |
| `hair` | `hair/` | See [Hair styles](#hair-styles) below. |
| `beard` | `hair/beards/` | Male-only usually. `Basic_Beard_<palette>` or `Winter_Beard_<palette>`. |
| `jacket` | `torso/jacket/` | Outerwear layer (Iverness, Tabard, Frock). **Sex matters** — some are male-only. |
| `clothes` | `torso/clothes/` | Inner torso (Tunic, Blouse, Robe). Most are female-only. |
| `armour` | `torso/armour/` | Plate armor. `Plate_<metal>` (bronze, silver, steel, gold). |
| `apron` | `torso/aprons/` | Overlay apron. `Apron_half_<color>`, `Apron_full_<color>`. Usually female-only. |
| `legs` | `legs/` | Category matters — `pants` supports muscular; `pants2` doesn't. See [Pants trap](#pants-trap). |
| `shoes` | `feet/shoes/` | `Revised_Shoes_<color>`. |
| `shoes_toe` | `feet/accessory/plate_toe_thick/` | Plated-toe overlays. `Thick_Plated_Toe_<metal>`. |
| `weapon` | `weapons/` | Main-hand weapon. See [Weapons](#weapons) below. |
| `shield` | `weapons/shield/` | `Round_Shield_<metal>`, `Kite_Shield_<metal>`. |
| `hat` | `headwear/` | `Hood_<color>`, `Cap_<color>`, etc. |
| `gloves` | `arms/gloves/` | `Gloves_<variant>`. |

### Less-common slots

Scan `sheet_definitions/` for additional slot types: `earrings/`, `nose_piercings/`, `cape/`, `tail/`, `wings/`, `wheelchair/`, etc. Each JSON has a `type:` field that becomes the hash slot key.

## The two variant conventions

LPC variants come in two flavors depending on the layer:

1. **Palette key** (most common) — the variant name maps to a palette applied at runtime by WebGL recolor. Examples: `chestnut`, `amber`, `bronze`, `gold`, `white`.
2. **File variant** — the variant name selects a pre-drawn PNG, not a palette remix. Examples: `Long_Pants_leather` (no "leather" palette exists; `leather.png` is a hand-authored file). File variants are used where the asset includes custom shading that a palette swap would flatten.

**How to tell them apart:** open the sheet-definition JSON. If there's a `variants: [...]` array listing colors, those are file variants. If the JSON just defines a `layer_1.<sex>` path pointing to `walk.png` (no `/color/` in the path), it uses palette recolor and any palette key works.

When in doubt, try both — the generator will silently drop whichever doesn't exist.

## Hair styles

Hair directory: `sheet_definitions/hair/`. Sub-dirs group styles by length/shape — `long/`, `short/`, `braids/`, `pigtails/`, `bob/`, `curly/`, `spiky/`, `afro/`, `bald/`, `extensions/`, `xlong/`.

Each style has a JSON with `"name": "<HashName>"` — that's what goes in `hair=<HashName>_<palette>`.

Examples of current MVP picks:

- `hair=Natural_<palette>` (from `hair_natural.json`) — default straight style
- `hair=Mop_<palette>` — messy warrior cut
- `hair=Lob_<palette>` — shoulder-length bob
- `hair=Bedhead_<palette>` — tousled/scholar look
- `hair=Ponytail_<palette>` — back-tied
- `hair=Balding_<palette>` — receded hairline (use with `beard=` for bald-with-beard)
- `hair=Winter_Beard_<palette>` — actually a beard, but filed under `hair/beards/`

**To discover a style's exact name token:** open its JSON and copy the `"name"` field. Do not guess — `"name"` can differ from the filename (e.g., `hair_long_center_part.json` → `"name": "Long Center Part"` → hash token `Long_Center_Part`).

### Palette keys (hair)

Common palette keys that work with most hair JSONs:

`white, platinum, gold, yellow, blonde, ginger, redhead, copper, orange, chestnut, brown, umber, dark_brown, black, blue, violet, rose, pink, green, teal, turquoise, gray, silver`

Run the generator UI and open any hair entry's color dropdown — the list there is authoritative.

## Weapons

Weapons live under `weapons/<category>/`, typically `sword/`, `blunt/`, `ranged/bow/`, `magic/`, `staff/`.

Recipes use `weapon=<Name>_<variant>`. The quirk: many weapons use **metal palette** (iron, bronze, steel, silver, gold) while a few are file variants where `variant == filename`. Examples:

- `Longsword_longsword` — file variant (there's only one longsword PNG, variant name = filename stem)
- `Gnarled_staff_iron` — palette variant (`Gnarled_staff_*` supports the metal palette)
- `Normal_iron` — the bow layer, file `normal.json` with metal palette
- `Mace_mace` — file variant (one mace PNG)

## Shields

Under `weapons/shield/`. JSON `"name": "Round Shield"` / `"Kite Shield"`. Hash token: `Round_Shield_silver`, `Kite_Shield_bronze`, etc. **Don't forget the `Shield` in the name token** — `Round_silver` will silently drop.

## Pants trap

Two top-level pant categories exist:

- `legs/pants/` — has `male`, `female`, `muscular` variants
- `legs/pants2/` — has `male`, `female`, `thin` variants but **no `muscular`**

If you set `sex=muscular` and use `legs=Long_Pants_black`, that uses `pants2/muscular/walk.png` — which doesn't exist → silent drop → no pants render.

Fix: use `legs=Pants_black` (note: `Pants` not `Long_Pants`) which routes to `pants/` and has a muscular variant.

Always check the JSON's `layer_1` block for your chosen `sex`.

## Torso sex-compatibility matrix (current MVP scope)

| Slot / Item | male | female | muscular |
|---|---|---|---|
| `Iverness_cloak` (jacket) | yes (black only) | no | yes (black only) |
| `Tabard` (jacket) | yes | yes | yes |
| `Frock_coat` (jacket) | yes (white is male-only) | — | yes |
| `Plate` (armour) | yes (all metals) | yes | yes |
| `Tunic` (clothes) | no | yes (green, brown, etc.) | no |
| `Blouse` (clothes) | no | yes | no |
| `Robe` (clothes) | no | yes | no |
| `Apron_half` (apron) | no | yes | no |

When in doubt, read the JSON — the authoritative source is `sheet_definitions/torso/<category>/<item>.json`'s `layer_1` block.

## Debugging a silent drop

Symptoms: the generated sprite is missing a layer you requested (shirtless mage, no pants blacksmith, no shield on warrior).

Diagnostic checklist:

1. Open the per-character `*_credits.txt` generated next to the PNG — every layer that rendered is listed. If your slot isn't there, it was dropped.
2. Open `sheet_definitions/<slot-dir>/<item>.json` and confirm:
   - `"name"` field matches your hash Name exactly (case-sensitive, spaces → underscores).
   - `layer_1.<sex>` path exists (where `<sex>` matches your `sex=` value).
   - For file variants: `variants` array includes the variant you specified.
3. For metal-palette items: try common keys `iron`, `bronze`, `steel`, `silver`, `gold`. Custom keys may exist for specific items.

## Authoring a new recipe

1. Start from an existing MVP recipe in `game-batch.mjs` as a template.
2. Pick `sex` first — everything downstream must support it.
3. For each slot you want filled, open the chosen item's JSON and confirm the `sex` is supported.
4. Add the recipe, run `node tools/lpc-generator/game-batch.mjs <recipe-key>` (dev server must be running at `localhost:5173`).
5. Open the generated `*_credits.txt` and verify every slot listed matches what you specified. Any missing slot = silent drop; diagnose per checklist above.
6. Visually confirm the sprite renders as expected before committing.

## Cross-references

- **Upstream generator:** `tools/lpc-generator/` (vendored, mostly gitignored — only `game-batch.mjs` is tracked).
- **Pipeline overview:** [`lpc-automation-pipeline.md`](lpc-automation-pipeline.md) — covers the Playwright runner, credits aggregation, where output lands.
- **Attribution aggregation:** `assets/characters/CREDITS.md` is the summary of per-character `*_credits.txt` files.
- **ADR-007** (`docs/decisions/ADR-007-*.md`) — why the project uses LPC instead of PixelLab for MVP.

---

*This guide documents the generator as it stands at 2026-04-19. New slots / items added to `sheet_definitions/` after that date won't appear here — the JSONs themselves are always authoritative. If you find a new quirk (another silent-drop trap, a missed compatibility case), extend this doc.*
