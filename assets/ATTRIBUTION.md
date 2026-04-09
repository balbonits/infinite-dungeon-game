# Asset Attribution

All assets in this project use one of the following licenses:
- **CC0 1.0** (Public Domain)
- **CC-BY 3.0** (Attribution required)
- **CC-BY 4.0** (Attribution required)

## Credits

### Tilesets

| Asset | Author | License | Files | Source |
|-------|--------|---------|-------|--------|
| Dungeon Crawl 32x32 Tiles (Full) | Crawl Stone Soup team (multiple artists) | CC0 | 6,029 PNGs — monsters, dungeon, items, players, effects, GUI | [opengameart.org](https://opengameart.org/content/dungeon-crawl-32x32-tiles) |
| Dungeon Crawl 32x32 Tiles (Supplemental) | Crawl Stone Soup team (multiple artists) | CC0 | 3,016 PNGs — additional monsters, dungeon, items, players, effects | [opengameart.org](https://opengameart.org/content/dungeon-crawl-32x32-tiles-supplemental) |
| ProjectUtumno Full Spritesheet | Crawl Stone Soup team | CC0 | 1 PNG spritesheet (all tiles combined) | Included with Dungeon Crawl pack |
| ProjectUtumno Supplemental Spritesheet | Crawl Stone Soup team | CC0 | 1 PNG spritesheet (supplemental tiles combined) | Included with supplemental pack |

**Dungeon Crawl Stone Soup tile artists (CC0 sign-off):**
Eino Keskitalo, David Lawrence Ramsey, Enne Walker, Poor_Yurik, Stefan O'Rear, and others. See `tilesets/dungeon-crawl/dcss-full/Dungeon Crawl Stone Soup Full/README.txt` for full credits.

### UI

| Asset | Author | License | Files | Source |
|-------|--------|---------|-------|--------|
| Pixel UI Pack | Kenney Vleugels (kenney.nl), Lynn Evers | CC0 | 36 PNGs — 9-slice panels, buttons, cursors, spritesheets | [opengameart.org](https://opengameart.org/content/pixel-ui-pack-750-assets) |
| UI Pack: RPG Extension | Kenney Vleugels (kenney.nl) | CC0 | 90 PNGs — RPG-specific UI elements, health bars, inventory slots | [opengameart.org](https://opengameart.org/content/ui-pack-rpg-extension) |

### Icons

| Asset | Author | License | Files | Source |
|-------|--------|---------|-------|--------|
| 496 Pixel Art Icons for Medieval/Fantasy RPG | 7Soul1 | CC0 | 496 PNGs — weapons, armor, potions, materials, misc | [opengameart.org](https://opengameart.org/content/496-pixel-art-icons-for-medievalfantasy-rpg) |
| Armor Icons by Equipment Slot | Clint Bellanger (FLARE project) | CC-BY 3.0 | 1 PNG spritesheet — head, chest, hands, legs, feet (5 tiers each) | [opengameart.org](https://opengameart.org/content/armor-icons-by-equipment-slot) |

### Fonts

| Asset | Author | License | Files | Source |
|-------|--------|---------|-------|--------|
| Tiny RPG - Font Kit I | Gabriel "tiopalada" Lima | CC0 | 4 TTF files — BadgeFont, BrilliantStrength, FineFantasyStrategies, FineFantasyStrategiesItalicNumbers | [tiopalada.itch.io](https://tiopalada.itch.io/tiny-rpg-font-kit-i) / [opengameart.org](https://opengameart.org/content/tiny-rpg-font-kit-i) |

## Asset Inventory Summary

| Category | Pack | File Count | Resolution | Format |
|----------|------|-----------|------------|--------|
| Tilesets | Dungeon Crawl Full | 6,029 | 32x32 | Individual PNGs |
| Tilesets | Dungeon Crawl Supplemental | 3,016 | 32x32 | Individual PNGs |
| Tilesets | Crawl Tiles 2010 | 3,039 | 32x32 | Individual PNGs |
| UI | Pixel UI Pack | 36 | Various | PNGs + spritesheets |
| UI | RPG Extension | 90 | Various | PNGs + spritesheets + SVG |
| Icons | 496 RPG Icons | 496 | 34x34 | Individual PNGs |
| Icons | Armor (FLARE) | 1 | 64x64 per icon | Spritesheet |
| Fonts | Tiny RPG Font Kit | 4 | N/A | TTF |
| **Total** | | **~12,700+** | | |

## Folder Structure

```
assets/
├── ATTRIBUTION.md              ← this file
├── fonts/
│   └── extracted/              ← 4 TTF fonts + preview PNGs
├── icons/
│   ├── armor/                  ← FLARE armor spritesheet
│   └── extracted/              ← 496 RPG item icons
├── tilesets/
│   ├── dungeon-crawl/
│   │   ├── dcss-full/          ← 6,029 tiles (monster/, dungeon/, item/, player/, effect/, gui/)
│   │   ├── crawl-tiles-2010/   ← 3,039 tiles (older version)
│   │   ├── DungeonCrawl_ProjectUtumnoTileset.png  ← full spritesheet
│   │   └── ProjectUtumno_full.png                 ← alt spritesheet
│   └── dungeon-crawl-supplemental/
│       ├── dcss-supplemental/  ← 3,016 tiles (same categories as full)
│       └── ProjectUtumno_supplemental.png         ← supplemental spritesheet
├── tiles/                      ← original prototype tiles (floor.png, wall.png)
├── sprites/                    ← (empty, for future custom sprites)
└── ui/
    ├── pixel-ui-pack/extracted/  ← 9-slice panels, buttons, spritesheets
    └── rpg-extension/extracted/  ← RPG UI elements, health bars, inventory slots
```

## How to Add Credits

When adding a new asset with a CC-BY license, add a row to the relevant table above with:
1. **Asset** — name of the asset pack
2. **Author** — original creator's name or username
3. **License** — one of: CC0, CC-BY 3.0, CC-BY 4.0
4. **Files** — count and description of what's included
5. **Source** — link to the original asset page

CC0 assets do not legally require attribution but are listed here for tracking and good practice.
