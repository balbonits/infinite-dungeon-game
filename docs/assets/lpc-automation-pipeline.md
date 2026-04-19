# LPC Character Automation Pipeline

**Status:** Active (replaces PixelLab per [ADR-007](../decisions/007-top-down-oga-pivot.md))
**Owner:** art-lead
**Last updated:** 2026-04-18

## What this is

An automated batch generator that produces LPC-style character spritesheets (walk / slash / thrust / spellcast / shoot / hurt + more animations, 64×64 frames) for every MVP character in the game. Drives the Liberated Pixel Cup Universal Spritesheet Character Generator via Playwright. No clicking, no per-character manual export.

## Architecture

```
tools/lpc-generator/              # Cloned generator (gitignored, ~855 MB)
├── index.html                    # Vite entry point (SPA — mithril.js)
├── sheet_definitions/            # JSON definitions per item (authoritative variant/palette source)
├── spritesheets/                 # Raw layer PNGs
├── palette_definitions/          # Color palette JSON files (body/hair/metal/cloth/etc.)
├── item-metadata.js              # AUTO-GENERATED lookup — use for Name/type_name/required body types
└── game-batch.mjs                # *** Our automation script ***

assets/characters/                # Output PNGs land here
├── player/{warrior,ranger,mage}/{<class>_full_sheet.png, <class>_credits.txt}
├── npcs/{blacksmith,guild_maid,village_chief}/{<role>_full_sheet.png, <role>_credits.txt}
└── CREDITS.md                    # Summary attribution (parsed from per-character TXTs)

assets/md/lpc-sprite-recipes.md   # Human-readable intent (fiction/feel/color theme) per character
docs/decisions/007-top-down-oga-pivot.md   # Why we picked this stack
```

## How to run

```bash
# Terminal 1 — keep dev server up
cd tools/lpc-generator
npm install       # one-time
npm run dev       # serves at http://localhost:5173

# Terminal 2 — run the batch
cd tools/lpc-generator
node game-batch.mjs                 # all recipes
node game-batch.mjs warrior         # single recipe
node game-batch.mjs validation      # smoke test with fixture URL
```

Output PNGs are written directly to the paths in the game repo (`assets/characters/...`).

## How the URL hash format works

The generator serializes state to the URL fragment: everything after `#`. Example:

```
http://localhost:5173/#sex=male&body=Body_Color_light&head=Human_Male_light&hair=Natural_violet&jacket=Iverness_cloak_black&legs=Long_Pants_orange&shoes=Revised_Shoes_bluegray&weapon=Longsword_longsword
```

Format rules:

- `sex=<bodyType>` — one of `male | female | muscular | pregnant | teen | child`
- `<type_name>=<Name>_<variantOrRecolor>` for every layer
  - `type_name` is the category key (e.g. `body`, `hair`, `legs`, `armour`, `clothes`, `jacket`, `beard`, `weapon`, `shoes_toe`)
  - `Name` is the item's display name with spaces replaced by underscores (e.g. `"Long Pants"` → `Long_Pants`)
  - `<variantOrRecolor>` is either a file-variant name (if the layer has explicit variant files) **or** a palette color key (if the layer uses palette recoloring)

The browser loads that URL, `loadSelectionsFromHash()` fires, `window.canvasRenderer` composites every supported animation row, and the download button exports a single PNG.

## Adding a new character — the right way

Do NOT guess layer names. The generator silently drops unmatched layers (producing naked or incomplete characters). Always verify each piece against the data:

### Step 1 — pick an itemId

Candidates live in `item-metadata.js` (148 K lines — grep it). Every entry looks like:

```javascript
"torso_armour_plate": {
  "name": "Plate",
  "type_name": "armour",
  "required": ["male", "female", "teen"],   // body types this renders on
  ...
}
```

Commands:

```bash
# Find all weapons:
grep -B1 '"name":' item-metadata.js | grep -A1 '"weapon_' | head -40

# Find all robes / tunics / cloaks:
grep -B1 '"name":' item-metadata.js | grep -B1 -iE 'robe|tunic|cloak'

# Check required body types for an item:
grep -A 10 '"torso_clothes_robe":' item-metadata.js | head -12
```

### Step 2 — pick a variant or recolor

Two kinds of items:

**A. File-variant items** — variants are explicit PNG files per color. Look in the sheet_definition:

```bash
python3 -c "import json; print(json.load(open('sheet_definitions/legs/pants/pants2.json')).get('variants', []))"
# → []    ← empty means this item uses palette recolor instead

# Or list the actual files to see available colors:
ls spritesheets/legs/pants2/male/walk/
# → black.png  blue.png  brown.png  charcoal.png  forest.png  gray.png  green.png  ...
```

**B. Palette-recolor items** — no variant files; colors come from a palette:

```bash
# Check which palette an item uses:
python3 -c "import json; print(json.load(open('sheet_definitions/torso/armour/torso_armour_plate.json')).get('recolors'))"
# → {"material": "metal", "palettes": ["ulpc", "lpcr", "all.lpcr"]}

# List valid color keys for that palette:
python3 -c "import json; print(list(json.load(open('palette_definitions/metal/metal_ulpc.json')).keys()))"
# → ['ceramic', 'brass', 'copper', 'bronze', 'iron', 'steel', 'silver', 'gold']
```

### Step 3 — construct the hash param

```
<type_name>=<Name_with_underscores>_<variantOrPaletteKey>
```

Examples:

| itemId | Name | type_name | variant source | URL param |
|--------|------|-----------|----------------|-----------|
| `body` | Body Color | `body` | body palette (ulpc) | `body=Body_Color_amber` |
| `torso_armour_plate` | Plate | `armour` | metal palette | `armour=Plate_steel` |
| `torso_clothes_robe` | Robe | `clothes` | file variants | `clothes=Robe_blue` |
| `legs_pants2` | Long Pants | `legs` | file variants | `legs=Long_Pants_gray` |
| `feet_plate_toe_thick` | Thick Plated Toe | `shoes_toe` | metal palette | `shoes_toe=Thick_Plated_Toe_steel` |
| `hair_natural` | Natural | `hair` | hair palette | `hair=Natural_black` |
| `beards_beard` | Basic Beard | `beard` | hair palette | `beard=Basic_Beard_black` |
| `weapon_sword_longsword` | Longsword | `weapon` | file variant "longsword" | `weapon=Longsword_longsword` |
| `weapon_ranged_bow_normal` | Normal | `weapon` | metal palette | `weapon=Normal_iron` |
| `weapon_magic_gnarled` | Gnarled staff | `weapon` | metal palette | `weapon=Gnarled_staff_iron` |

### Step 4 — add to `game-batch.mjs`

Put the recipe in the `RECIPES` object with the hash params + output path. Run `node game-batch.mjs <name>` to test. Verify the output PNG visually — empty animation rows are expected (not every layer supports every animation).

## Known gotchas

1. **`torso_clothes_robe` requires female body** (`required: ["female"]`). Male characters need a different torso (`torso_armour_plate`, `torso_jacket_iverness`, `torso_jacket_frock`, or similar).
2. **`torso_jacket_iverness` only has a `black.png` file variant.** Asking for `Iverness_cloak_green` silently drops the layer.
3. **`torso_clothes_tunic` / `torso_clothes_blouse` are female-only.** No male variants. Use `torso_armour_*` or `torso_jacket_*` for male torsos.
4. **`torso_jacket_frock` (Frock coat) is male-only** and has a full file-variant palette (including `white`). It is the go-to male equivalent of the female Robe when you need a long white/colored upper garment (e.g. elder male characters).
5. **Body palette does NOT include `tan`.** Valid skin tones: `light`, `amber`, `olive`, `taupe`, `bronze`, `brown`. Closest to "tan" is `amber`.
6. **Hair `natural` uses the hair palette.** Valid colors include `blonde`, `gold`, `black`, `white`, `platinum`, `dark_brown`, `chestnut`, etc. — check `palette_definitions/hair/hair_ulpc.json`. `gold` reads noticeably more saturated than `blonde`; use it when you need loud warm-yellow hair.
7. **Aprons stack as a separate `type_name=apron` layer** over whatever torso you picked. `Apron_half_white` over `Blouse_yellow` gives a service-uniform silhouette (female-only). File variants include the full 24-color palette — check each apron JSON for the exact list.
8. **Loud greens live in non-Robe torsos.** `Robe` only offers `forest_green` (muted). `Tunic` / `Blouse` / `Tabard` all offer a bright `green` file variant — pick one of those for a strong ranger/hunter read. Combine with `Long_Pants_leather` (warm brown) to avoid muddying the green read.
9. **Not every layer supports every animation.** Expect empty rows in output sheets for animations like `sit`, `emote`, `watering` where the selected armor/weapon has no frames.
10. **Silent drops.** If a layer param doesn't match an exact item Name, it's dropped without any error. Always visually verify each new recipe's output.
11. **The dev server must stay running.** The batch runner hits `http://localhost:5173` — start it with `npm run dev` in a separate terminal first.
12. **Hash parser is lossy on underscore collisions.** Layer names with spaces become `Name_With_Spaces`, then `_variant` is appended. The parser tries name splits left-to-right, so if `Apron` (base item) and `Apron half` (separate item) both exist with `type_name=apron`, the parser will try `Apron` first — this works only because `Apron`'s variant list doesn't contain the string that would match `half_white`. Be aware that ambiguous Name+variant concatenations can route to the wrong item.

## Credits aggregation

The batch runner clicks both the **Spritesheet (PNG)** and **Credits (TXT)** buttons per character. Each character's spritesheet is paired with a `<name>_credits.txt` in the same output directory (e.g. `assets/characters/player/warrior/warrior_credits.txt`). These per-character TXT files are the authoritative per-layer attribution record.

The summary view at `assets/characters/CREDITS.md` is regenerated by parsing those TXTs and extracting (layers, licenses, unique authors) per character. To refresh it after a recipe change, run the inline Python snippet below from the repo root — it reads each TXT, de-dupes author-name aliases (`Bluecarrot16` ↔ `bluecarrot16`, `Eliza Wyatt (ElizaWy)` ↔ `ElizaWy`), and rewrites the Markdown file:

```bash
cd /path/to/repo && python3 <<'EOF'
from pathlib import Path

chars = [
    ("Warrior", "player/warrior/warrior_credits.txt", "warrior_credits.txt", "player/warrior/"),
    ("Ranger", "player/ranger/ranger_credits.txt", "ranger_credits.txt", "player/ranger/"),
    ("Mage", "player/mage/mage_credits.txt", "mage_credits.txt", "player/mage/"),
    ("Blacksmith", "npcs/blacksmith/blacksmith_credits.txt", "blacksmith_credits.txt", "npcs/blacksmith/"),
    ("Guild Maid", "npcs/guild_maid/guild_maid_credits.txt", "guild_maid_credits.txt", "npcs/guild_maid/"),
    ("Village Chief", "npcs/village_chief/village_chief_credits.txt", "village_chief_credits.txt", "npcs/village_chief/"),
]
AUTHOR_ALIASES = {"Bluecarrot16": "bluecarrot16", "Eliza Wyatt (ElizaWy)": "ElizaWy"}

def parse_credits(path):
    text = Path(path).read_text()
    layers, authors, licenses = [], set(), set()
    section = None
    for line in text.splitlines():
        if not line.startswith("\t") and not line.startswith(" ") and line.strip():
            layers.append(line.strip()); section = None
        else:
            s = line.strip()
            if s.startswith("- Licenses"): section = "lic"
            elif s.startswith("- Authors"): section = "auth"
            elif s.startswith("- Links") or s.startswith("- Note"): section = None
            elif section == "lic" and s.startswith("- "): licenses.add(s[2:])
            elif section == "auth" and s.startswith("- "):
                a = s[2:]; authors.add(AUTHOR_ALIASES.get(a, a))
    return layers, sorted(authors), sorted(licenses)

# ... (build Markdown; see assets/characters/CREDITS.md for the exact template)
EOF
```

The full aggregation script lives inline in the git history of `assets/characters/CREDITS.md` regeneration — reproduce it or write it once into a standalone `tools/lpc-generator/aggregate-credits.py` if you prefer.

**Why per-TXT + summary Markdown (not one big CREDITS.md with every layer inline):** the full TXT is ~110 lines per character (note / license list / author list / link list per layer). Inlining all six would be ~700 lines of mostly-duplicated author lists. The summary keeps `CREDITS.md` readable and points at the TXTs for the full record. License-compliance-wise, both files ship in the repo, so attribution is complete either way.

## Extending the pipeline

Good next-step improvements (not required for MVP):

- **Recipe-as-YAML.** Move recipe authoring out of `game-batch.mjs` into a declarative YAML per character. Keep `game-batch.mjs` as a pure runner.
- **Recipe validator.** A preflight script that parses `item-metadata.js` + palette defs, validates every URL param against real Names/variants, and errors loudly before the Playwright run. Would have prevented our "naked Ranger" regression.
- **Credits CSV export.** Add a Playwright step that clicks the `Credits (TXT)` button and aggregates per-character credits into `assets/characters/CREDITS.md`.
- **`data-*` hooks.** If the generator's mithril DOM structure changes, we can patch `index.html` in-place to add `data-game-*` attributes for stable Playwright selectors (the user pre-authorized this).
- **Decomposing sheets.** LPC outputs one giant multi-anim sheet per character. Godot ingestion may want pre-sliced per-animation PNGs — add a post-processing step if so.

## Where to read more

- URL hash format internals: `tools/lpc-generator/sources/state/hash.js`
- JSON import/export: `tools/lpc-generator/sources/state/json.js`
- Download trigger: `tools/lpc-generator/sources/components/download/Download.js`
- Palette recoloring: `tools/lpc-generator/PALETTE_RECOLOR_GUIDE.md`
- Full licensing terms: `tools/lpc-generator/README.md` + `tools/lpc-generator/CREDITS.csv`
