# Canonical Font — Press Start 2P

## Summary

Canonical font for every text surface in the game: **Press Start 2P**. One font, one aesthetic, one source of truth. SPEC-UI-FONT-01 (Phase H). Load-bearing — every downstream UI spec inherits this choice.

## Current State

**Spec status: LOCKED** via SPEC-UI-FONT-01 (Phase H). The project currently uses Godot's default font (a generic sans-serif) with no explicit font declaration in `scripts/ui/UiTheme.cs`. This spec replaces that default with Press Start 2P across every text surface.

## Design

### Why Press Start 2P

- **Aesthetic match with the pixel-art visuals.** The game's art is cartoonish true-iso pixel art (Diablo 1 reference). A pixel-era bitmap font reinforces the retro feel instead of contrasting with it.
- **Iconic retro-arcade recognition.** PS2P is the most-recognized retro pixel font; players' "this is a pixel game" expectation is met immediately.
- **Free and permissive license.** PS2P is released under the SIL Open Font License 1.1 — free for commercial use, no attribution required in-game (optional in credits).

### Known tradeoff — all-caps readability

PS2P is **uppercase-only**. Lowercase letters render as smaller-cap forms that look similar to capitals but at reduced height. This has implications:

- **Long dialogue lines become harder to scan.** The Village Chief's wise-elder voice uses longer sentences ([npc-dialogue-voices.md §Village Chief](../flows/npc-dialogue-voices.md)) — these will need line-breaking and paragraph spacing support to stay readable.
- **Short UI labels work beautifully.** HUD, buttons, menu tabs, tooltips, and title screens all shine in PS2P.
- **Numbers and punctuation are clean.** Damage numbers, gold totals, stat values all render clearly.

**Mitigation for long dialogue:**

- Bump line-spacing to 1.5× font-size (more visual breathing room).
- Cap visible line length at ~50 characters (force word-wrap earlier than default).
- Insert paragraph breaks every ~2-3 sentences in the long-form NPC dialogue (Village Chief).
- If a specific dialogue beat becomes unreadable in all-caps after these mitigations, rewrite it to shorter sentences — the Chief's voice can absorb shorter lines without losing character.

### Size ladder

PS2P is a bitmap font that looks best at integer multiples of its native 8-pixel cell. The existing `UiTheme.FontSizes` ladder has sizes like 11, 12, 13, 16 that don't align cleanly — those render blurry or inconsistent. New ladder:

| Role | Old size | **New size (PS2P)** |
|------|----------|---------------------|
| Small | 11 | **8** |
| Body | 12 | **16** |
| Label | 13 | **16** |
| Button | 16 | **16** |
| Heading | 20 | **24** |
| Title | 24 | **32** |
| HeroTitle | 48 | **48** |

Notes:
- Body / Label / Button all collapse to **16 px** (one cell doubled). This is the readable default; distinguishing Body from Button via weight/color is a better affordance than via font size at pixel scale.
- Small (8 px) is reserved for tooltip footnotes and very tight HUD slots. Use sparingly — at 8 px, legibility drops on dense text.
- HeroTitle (48 px) stays the same — used for "YOU DIED" cinematic text and the splash title only.

### Font asset integration

- **Asset location:** `assets/fonts/PressStart2P-Regular.ttf`. The OFL license file ships alongside at `assets/fonts/PressStart2P-OFL.txt`.
- **Godot import:** import as `DynamicFont` with hinting `None` (bitmap font — hinting would blur), filter `Off` (crisp pixel edges, no bilinear smoothing).
- **UiTheme integration:** expose `FontFamily` on `UiTheme` as a **lazy-loaded** `Font` property (backed by a private cached `Font?`). First read triggers `GD.Load<FontFile>(...)`; subsequent reads return the cached instance. `GlobalTheme.Create()` assigns that shared PS2P instance to the Theme's `DefaultFont`, so every Control under the UILayer inherits it via theme cascading — no per-control `AddThemeFontOverride("font", ...)` calls needed in the common case. The exception is Labels parented under `Node2D` (e.g., stairs labels in `Town.cs` / `Dungeon.cs`): Godot's theme cascade doesn't cross the Control→Node2D boundary, so those require an explicit `AddThemeFontOverride("font", UiTheme.FontFamily)`. The lazy form (vs. `public static readonly`) is intentional: static initialization fires when `UiTheme` is first touched by any code, including the test assembly, which compiles against GodotSharp but never runs Godot's runtime — static `GD.Load` at class-init time throws there. Lazy-load keeps the test assembly green while still giving UI code a single cached instance.
- **Fallback font:** Godot's default sans-serif stays as fallback for unsupported glyphs (primarily non-Latin scripts — PS2P covers Basic Latin + Latin-1 Supplement only). If localized builds need Japanese/Chinese/etc., fallback activates automatically and needs a separate localization spec.

### Application scope

**Everywhere:**
- HUD orbs, stats label, floating combat text, toasts.
- Pause menu tabs, Inventory / Equipment / Skills / Stats panels.
- Dialogue bubbles (all three NPCs) + quest UI.
- Shop / Bank / Forge / Craft / Recycle / Teleport windows.
- Splash screen, class select, death screen.
- Debug console, debug panel, sandbox UI.

**NOT replaced:**
- **In-game sprite/texture text.** If a sprite has text painted into it (e.g., a signpost with "BLACKSMITH" painted on), that text is part of the sprite, not a Label node. Out of scope for this spec.
- **Credits / OFL notice.** The license file is plain text, not a UI element; doesn't use PS2P.

---

## Acceptance Criteria

- [ ] `assets/fonts/PressStart2P-Regular.ttf` + `PressStart2P-OFL.txt` land in the repo.
- [ ] `UiTheme` exposes `FontFamily` (lazy-loaded `Font`, cached after first read — see Integration note above) and every Label/Button/etc. gets it via a central styling helper.
- [ ] `UiTheme.FontSizes` updates to the new ladder (8 / 16 / 24 / 32 / 48) with Body=Label=Button=16.
- [ ] Every existing UI screen renders in PS2P with no font-override escaping back to the engine default.
- [ ] Long-dialogue screens (Village Chief dialogue, Chief quest offers) apply line-spacing ×1.5, ~50-char line cap, paragraph breaks per the mitigation rules.
- [ ] Fallback font registration covers non-Latin glyphs cleanly (shows fallback instead of tofu blocks).
- [ ] License file (`PressStart2P-OFL.txt`) stays with the asset in source control.

## Implementation Notes

- **Font preload:** expose as a lazy-loaded `Font` property with a cached `Font? _fontFamily` backing field. First read triggers `GD.Load<FontFile>("res://assets/fonts/PressStart2P-Regular.ttf")`, applies the pixel-font discipline (`Antialiasing=None`, `Hinting=None`, `SubpixelPositioning=Disabled`, `ForceAutohinter=false`), and caches. Return type is the base `Font` (not `FontFile`) so the error path can fall back to `ThemeDB.FallbackFont` — which is a `Font`, not necessarily a `FontFile` — without tofu-rendering from an empty `FontFile`. All UI code reads from this one property — no duplicate loads.
- **Integer-multiple rule:** PS2P's native cell is 8 px. Any font size must be a multiple of 8 to stay crisp. The size ladder above enforces this; if a future UI spec introduces an odd size (e.g., 14), it either rounds to 16 or gets justified as a deliberate one-off.
- **Filter setting:** set `filter=Off` on the imported `FontFile` resource so pixel edges stay crisp. Bilinear filtering blurs bitmap fonts.
- **SettingsPanel update:** the current `SettingsPanel` tab-button styling (`SettingsPanel.cs:126-139`) uses `UiTheme.FontSizes.Body` — which becomes 16 under the new ladder (up from 12). Visual pass needed to verify the tab-button widths still fit at 16 px.
- **DeathScreen HeroTitle:** `DeathScreen.cs:56` already uses 96 px; under the new ladder that's 48 × 2 = still a valid integer multiple, so no change needed.

## Open Questions

None — spec is locked.

## References

- **License:** [SIL Open Font License 1.1](https://openfontlicense.org/) — free for commercial use, no attribution required in-product.
- **Source:** [Press Start 2P on Google Fonts](https://fonts.google.com/specimen/Press+Start+2P) — designer Cody "CodeMan38" Boisclair, based on the NAMCO arcade font of 1980s arcade cabinets.
