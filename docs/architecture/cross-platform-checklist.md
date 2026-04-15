# Cross-Platform Checklist

## Summary

Everything beyond "it compiles and runs" that we need for a 7-platform release: PC/Windows, Linux, macOS, iOS, Android, Web Desktop, Web Mobile. This document covers screen handling, save data, store requirements, localization, accessibility, performance, audio, analytics, updates, and testing.

**Critical blocker:** Godot 4.6 C# does NOT support web export (HTML5/WebAssembly). Web Desktop and Web Mobile are blocked until Godot ships this. A prototype was demoed at GodotCon Boston (May 2025) but no stable release exists. Monitor https://github.com/godotengine/godot-proposals/discussions/10310 for status.

**Mobile C# status:** Android and iOS C# export is marked **experimental** in Godot 4.6. Android uses the linux-bionic Mono runtime (arm64/x64 only). iOS uses NativeAOT (.NET 8+, macOS-only build). Both work but have limitations (no Android bindings for some APIs like SSL). The experimental label may be removed in 4.7+.

---

## 1. Screen Resolution & Aspect Ratios

### What We Need

Support every screen shape players actually use, from 4:3 tablets to 21:9 ultrawides, without UI elements being cut off, stretched, or unusably small.

### Aspect Ratios by Platform

| Platform | Common Aspect Ratios | Common Resolutions |
|----------|---------------------|-------------------|
| PC/Windows | 16:9, 16:10, 21:9, 32:9 | 1920x1080, 2560x1440, 3440x1440, 3840x2160 |
| macOS | 16:10 (Retina) | 2560x1600, 2880x1800, 3024x1964, 3456x2234 |
| Linux | 16:9, 16:10 | 1920x1080, 2560x1440 |
| Steam Deck | 16:10 | 1280x800 (LCD), 1280x800 (OLED) |
| Android phones | 19.5:9, 20:9, 21:9 | 1080x2400, 1080x2340 |
| Android tablets | 16:10, 4:3 | 2560x1600, 2048x1536 |
| iPhone | 19.5:9 (14+), 19:9 (older) | 1179x2556, 1290x2796, 1242x2688 |
| iPad | 4:3, ~4.3:3 | 2048x1536, 2360x1640, 2732x2048 |
| Web Desktop | Same as PC | Varies (runs inside browser viewport) |
| Web Mobile | Same as phones/tablets | Varies (runs inside mobile browser) |

### Godot 4 Configuration

**project.godot settings:**

| Setting | Value | Why |
|---------|-------|-----|
| `display/window/size/viewport_width` | 640 | Our base design resolution (pixel art, 32px tiles = 20 tiles wide) |
| `display/window/size/viewport_height` | 360 | 16:9 base at 640 wide |
| `display/window/stretch/mode` | `canvas_items` | Scales the 2D canvas to fill the screen; UI and sprites scale together |
| `display/window/stretch/aspect` | `expand` | Extra screen space on non-16:9 displays shows more game world instead of letterboxing |
| `display/window/stretch/scale_mode` | `integer` (optional) | For crisp pixel art on desktop; disable on mobile for smoother scaling |

**Why `canvas_items` + `expand`:**
- `canvas_items` mode scales the root viewport to match the window, then scales all CanvasItem rendering. This is the recommended mode for 2D games.
- `expand` aspect means wider screens (21:9) see more world horizontally, taller screens (phones in portrait) see more vertically. No black bars. The base 640x360 viewport is always fully visible.
- This combination is explicitly recommended by Godot docs for mobile games with varying aspect ratios.

### Safe Areas (Notches, Cutouts, Rounded Corners)

**Problem:** iPhones have notches/Dynamic Island. Android phones have punch-hole cameras. Both have rounded screen corners. UI placed at screen edges gets obscured.

**Solution:**
- Use `DisplayServer.get_display_safe_area()` to query the safe area at runtime
- All HUD elements (HP bar, minimap, buttons) must be positioned inside the safe area
- The game world can render edge-to-edge (under the notch) -- only UI needs to be inset
- Consider the Notchz addon (Godot Asset Library) for a MarginContainer that auto-adjusts to safe area insets
- **Note:** `OS.get_window_safe_area()` from Godot 3 was replaced; the Godot 4 API is still evolving. Test on real devices.

### Implementation Tasks

| Task | Effort | Blocks Launch? |
|------|--------|---------------|
| Set stretch mode/aspect in project.godot | 15 min | Yes (all platforms) |
| Anchor all UI elements to screen edges using Godot's anchor system | 2-3 days | Yes (mobile) |
| Implement safe area margins for mobile UI | 1 day | Yes (mobile) |
| Test on ultrawide (21:9) to verify no UI overlap | 2 hours | Yes (PC) |
| Test on 4:3 tablet aspect ratio | 2 hours | Yes (mobile) |
| Add resolution/window mode settings (fullscreen, windowed, borderless) | 1 day | Yes (PC) |
| Test integer scaling for pixel-perfect rendering on desktop | 4 hours | No (polish) |

### Platform-Specific Differences

- **PC/Mac/Linux:** Player expects fullscreen toggle, resolution picker, borderless windowed mode
- **Mobile:** Always fullscreen. Orientation locked to landscape. Safe area handling mandatory.
- **Web:** Fills the browser canvas. No resolution picker needed. Must handle browser resize events.
- **Steam Deck:** 1280x800 (16:10). Treat as a small desktop. Verify UI is readable at this resolution.

---

## 2. Save Data & Cloud Sync

### Where Save Data Goes

Our save system uses `user://saves/slot_N.json` (see `docs/systems/save.md`). Godot's `user://` resolves differently per platform:

| Platform | `user://` Path | Storage Type |
|----------|---------------|-------------|
| macOS | `~/Library/Application Support/Godot/app_userdata/{project_name}/` | Local filesystem |
| Windows | `%APPDATA%\Godot\app_userdata\{project_name}\` | Local filesystem |
| Linux | `~/.local/share/godot/app_userdata/{project_name}/` | Local filesystem |
| Android | App-internal storage (sandboxed) | App-private filesystem |
| iOS | App sandbox (`Documents/` or `Library/`) | App-private filesystem |
| Web | IndexedDB (via Emscripten virtual FS) | Browser storage |

**No changes needed to SaveManager code.** `FileAccess.Open("user://...")` works identically on all platforms. Godot handles the path mapping.

### IndexedDB Limits (Web)

| Browser | Per-Origin Quota | Eviction Policy |
|---------|-----------------|----------------|
| Chrome | ~6.6% of free disk (e.g., 6.6 GB on 100 GB free) | LRU eviction in "best-effort" mode |
| Firefox | 10% of disk or 10 GiB (whichever is smaller) | LRU eviction; persistent storage available via `navigator.storage.persist()` |
| Safari | 1 GB per origin | **Aggressive:** Data from origins with no user interaction in 7 days is deleted. Private mode = 0 quota. |

**Our save files are ~50-250 KB per slot, 2.5 MB max for 10 slots.** This is well within all browser limits. However:
- Safari's 7-day eviction is dangerous for web players who take breaks. We should prompt players to use the Export/Import (Base64 clipboard) feature as backup.
- Players in Incognito/Private mode will lose all saves on tab close. Show a warning on first load if `navigator.storage.persist()` fails.
- Users must allow cookies/storage for IndexedDB to work. Show a clear error if storage is unavailable.

### Cloud Save Options

| Platform | Cloud System | Integration Method | Effort |
|----------|-------------|-------------------|--------|
| Steam | Steam Cloud (Auto-Cloud) | GodotSteam plugin + Steamworks config | 1-2 days |
| iOS | iCloud Key-Value Storage | Native plugin / GDExtension | 3-5 days |
| Google Play | Google Play Games (Saved Games API) | Android plugin | 3-5 days |
| Web | None built-in | Custom backend or Export/Import only | N/A or 1-2 weeks |
| Cross-platform | Custom backend (Supabase, Cloudflare Workers) | REST API | 2-4 weeks |

**Recommended approach:**
1. **Phase 1 (launch):** Local saves only + Export/Import for manual backup. Already designed.
2. **Phase 2 (post-launch per platform):** Steam Cloud for PC (cheapest, Steam handles sync). iCloud for iOS. Google Play Saved Games for Android.
3. **Phase 3 (if needed):** Custom backend for true cross-device sync (e.g., play on PC, continue on phone).

**Steam Cloud specifics:**
- Steam Auto-Cloud is the simplest: specify the save directory and file patterns in Steamworks settings. Steam syncs files automatically.
- Caveat: Steam does NOT propagate file deletions across devices. If a player deletes a save slot on one PC, it may reappear from another PC's sync. Need to track deletions with a metadata file.

### Implementation Tasks

| Task | Effort | Blocks Launch? |
|------|--------|---------------|
| Verify `user://` works on Android and iOS | 2 hours (test) | Yes (mobile) |
| Add IndexedDB persistence warning for web | 1 day | Yes (web) |
| Prompt Export/Import on web for save backup | 1 day | Yes (web) |
| Steam Cloud via Auto-Cloud config | 1-2 days | No (post-launch) |
| iCloud integration | 3-5 days | No (post-launch) |
| Google Play Saved Games | 3-5 days | No (post-launch) |

---

## 3. Platform-Specific Store Requirements

### Steam (PC/Mac/Linux)

| Requirement | Details | Effort | Blocks Launch? |
|-------------|---------|--------|---------------|
| Steamworks SDK | Use GodotSteam (GDExtension). Supports Windows, Linux, macOS (x86_64 + arm64). | 1-2 days setup | Yes |
| Steam App ID | Register on Steamworks ($100 fee). Create app, configure store page. | 1 day admin | Yes |
| Achievements | Define in Steamworks backend. Use `Steam.setAchievement()` via GodotSteam. Auto-synced since SDK 1.61. | 2-3 days | No (can add post-launch) |
| Steam Overlay | Works with Compatibility/OpenGL renderer. May flicker with Forward+/Vulkan outside Steam client -- fine when launched from Steam. | 0 (works out of box) | N/A |
| Steam Cloud | Configure in Steamworks App Admin > Cloud tab. Specify `user://saves/` file patterns. | 1-2 days | No |
| Trading Cards | Optional. Requires 100+ owners. | N/A | No |
| Review/Rating | Automatic. No dev action needed. | 0 | N/A |
| Controller support | Mark as "Full Controller Support" if gamepad works for all gameplay. Our input map already supports this. | 1 day (testing) | Yes |

### Google Play Store (Android)

| Requirement | Details | Effort | Blocks Launch? |
|-------------|---------|--------|---------------|
| Developer account | $25 one-time fee | 1 hour | Yes |
| Content rating | Complete IARC questionnaire in Play Console. Our game: violence (fantasy), no IAP, no user-generated content. Likely rated "Teen" / PEGI 12. | 1 hour | Yes |
| Target API level | Must target latest Android API (currently API 35+). Godot 4.6 handles this in export settings. | 0 | Yes |
| Google Play Billing | Only needed if we add IAP. Not needed for a paid or free game without purchases. | N/A unless IAP | No |
| Play Integrity API | Required for apps using certain Google APIs. Not needed if no IAP/online features. | N/A | No |
| 64-bit requirement | Google Play requires arm64. Godot 4.6 C# exports arm64 by default. | 0 | Yes |
| App Bundle (AAB) | Google Play requires AAB format (not APK) for new apps. Godot exports AAB. | 0 | Yes |
| Data safety form | Declare what data is collected. If no analytics: "No data collected." | 30 min | Yes |
| Gradle build | Required for C# export. Enable in Project > Export > Android. | Config only | Yes |
| Privacy policy | Required if targeting children or collecting any data. Host on a web page. | 2 hours | Yes |

### Apple App Store (iOS)

| Requirement | Details | Effort | Blocks Launch? |
|-------------|---------|--------|---------------|
| Developer account | $99/year | Admin | Yes |
| Xcode + macOS | iOS export only works from macOS. Build with Xcode after Godot export. | 0 (we develop on Mac) | Yes |
| App Tracking Transparency | Required since iOS 14.5 if using any tracking/advertising SDKs. If no analytics/ads: not needed. If analytics opted-in: must show ATT prompt before any tracking. | 1 day (if analytics) | Conditional |
| Age rating | New granular ratings (13+, 16+, 18+) since July 2025. Complete questionnaire in App Store Connect. | 30 min | Yes |
| Privacy Nutrition Labels | Declare data types collected in App Store Connect. | 1 hour | Yes |
| In-App Purchases | Only needed if we sell anything. StoreKit integration via plugin. Apple takes 30% (15% for small developers <$1M/yr). | N/A unless IAP | No |
| App Review | Apple reviews every submission. No crashes, no broken features, no placeholder content. Rejection common for: crashes, broken links, incomplete features. Average review: 24-48 hours. | N/A | Yes |
| iOS 26 SDK | Starting April 2026, all submissions must use iOS 26 SDK + Xcode 26. | Stay updated | Yes |
| Code signing | Requires provisioning profiles and certificates from Apple Developer portal. | 2 hours setup | Yes |
| NativeAOT trimming | iOS C# uses NativeAOT. Godot bindings have trimming compatibility issues. Test thoroughly. | 2-3 days testing | Yes |

### Web (itch.io, Newgrounds, PWA)

| Requirement | Details | Effort | Blocks Launch? |
|-------------|---------|--------|---------------|
| **C# web export** | **NOT SUPPORTED in Godot 4.6.** This blocks ALL web distribution. | Blocked upstream | **BLOCKED** |
| itch.io hosting | Upload ZIP of HTML5 export. Free. Set "This file will be played in the browser." | 30 min | Blocked |
| SharedArrayBuffer | Required for threading. Needs COOP/COEP headers. itch.io supports this. Single-threaded export avoids this requirement (recommended since Godot 4.3). | Config | Blocked |
| PWA export | Godot has built-in PWA export option (Service Worker). Enables "install as app" on mobile browsers. | Config toggle | Blocked |
| Max file size | itch.io allows up to 1 GB for browser games. Our game should be well under. | N/A | Blocked |

---

## 4. Localization / Internationalization

### How Godot 4 i18n Works

1. **All player-facing strings** use `Tr("KEY")` in C# (or `tr("KEY")` in GDScript)
2. **TranslationServer** loads translation files at startup and resolves keys to the current locale
3. **Translation files** can be CSV (simple, editable in spreadsheets) or PO/Gettext (industry standard, supports plurals, context)
4. **Auto Translate** property on UI nodes can translate text without code
5. **Godot 4.6** added C# translation parser support: localized text is collected automatically during POT/CSV exports

### Recommended File Format

**CSV for our project.** Reasons:
- Simpler than PO for a small team
- Editable in Google Sheets (shareable with translators)
- One CSV file with columns: `key, en, es, fr, de, ja, zh, ko, pt, ru`
- Godot imports CSV as Translation resources automatically

**File structure:**
```
locales/
  translations.csv          # All strings
  translations.en.translation  # Auto-generated by Godot import
  translations.es.translation
  ...
```

### Priority Languages

Based on Steam market data and indie game ROI research:

| Tier | Languages | Market Coverage | When |
|------|-----------|----------------|------|
| Tier 1 (launch) | English | ~35% Steam users | Day 1 |
| Tier 2 (soon after) | Simplified Chinese, Spanish, Portuguese (BR), Russian, German | +45% coverage (80% cumulative) | Within 3 months |
| Tier 3 (growth) | French, Japanese, Korean, Turkish, Polish | +10% coverage (90% cumulative) | Within 6 months |
| Tier 4 (full) | Italian, Traditional Chinese, Thai, Ukrainian, Arabic, Hindi | Remaining markets | Post 6 months |

**Simplified Chinese is the single highest-ROI language** for indie games on Steam (+2.61% growth, largest non-English market).

### RTL Language Support (Arabic, Hebrew)

Godot 4 has native bidirectional text support via TextServer. However:
- UI nodes do NOT automatically mirror for RTL locales
- `layout_direction` must be set explicitly per Control node or at scene root
- Menu layouts may need RTL-specific scene variants
- **Recommendation:** Defer RTL to Tier 4. Significant UI work required.

### Font Requirements

| Language Group | Font Requirement | File Size Impact |
|---------------|-----------------|-----------------|
| Latin (EN, ES, FR, DE, PT, etc.) | Our primary pixel font | Minimal |
| Cyrillic (RU, UK) | Primary font must include Cyrillic glyphs, or add fallback | +100-500 KB |
| CJK (ZH, JA, KO) | Dedicated CJK font (Noto Sans CJK recommended) | +5-20 MB per variant |
| Arabic/Hebrew | Dedicated RTL font | +1-3 MB |
| Thai | Font with Thai glyphs | +500 KB-1 MB |

**Godot 4 supports font fallback chains:** Primary font > Cyrillic fallback > CJK fallback. When a glyph is missing from the primary font, fallbacks are checked in order. This means CJK fonts only load for CJK text.

**CJK variants matter:** Simplified Chinese, Traditional Chinese, Japanese, and Korean each use different glyph variants for the same codepoints. Use language-specific font overrides.

### Pseudolocalization Testing

Godot has built-in pseudolocalization that simulates localization issues without real translations:
- Expands text length (catches UI overflow)
- Replaces characters with accented versions (catches encoding issues)
- Tests RTL behavior
- Enable in Project Settings > Internationalization > Pseudolocalization

### Implementation Tasks

| Task | Effort | Blocks Launch? |
|------|--------|---------------|
| Wrap all strings in `Tr()` calls | Ongoing (do from start) | Yes (for non-EN) |
| Set up CSV translation file with EN column | 1 day | Yes (for non-EN) |
| Add font fallback chain (Latin > Cyrillic > CJK) | 1 day | Yes (for non-EN) |
| Implement locale picker in Settings menu | 1 day | Yes (for non-EN) |
| Pseudolocalization testing pass | 1 day | No (but do before translations) |
| Commission translations (Tier 2) | External cost + 1-2 weeks | No (post-EN launch OK) |
| RTL layout support | 3-5 days | No (Tier 4) |
| CJK font integration + testing | 2-3 days | No (Tier 2-3) |

### Effort Estimate

**Launch in English only:** 0 extra work (just use `Tr()` from the start for future-proofing).
**Tier 2 (5 languages):** 2-3 weeks engineering + translation costs ($2k-5k professional, or community/AI-assisted).

---

## 5. Accessibility

### What We Already Have (Spec)

Our `docs/systems/accessibility.md` covers:
- Font size scaling (Small/Medium/Large/XL)
- Colorblind palettes (Deuteranopia, Protanopia, Tritanopia)
- High contrast mode
- Reduced motion toggle
- Combat text toggle
- HUD opacity slider

### What's Missing

#### Screen Reader Support

**Godot 4.5+ integrates AccessKit**, providing native screen reader support:
- Windows: NVDA, JAWS, Narrator
- macOS: VoiceOver
- Linux: Orca
- iOS: VoiceOver (native)
- Android: TalkBack

**What we need to do:**
- Set `accessible_name` and `accessible_description` on all interactive UI nodes
- Ensure tab order / focus order is logical for keyboard navigation
- Use `DisplayServer.tts_speak()` for custom announcements (combat events, loot drops, etc.)
- The godot-accessibility-demo project (GitHub: aefren/godot-accessibility-demo) provides a reference implementation for Godot 4.6

#### Remappable Controls

Godot's InputMap supports runtime rebinding:
- `InputMap.action_erase_events("action_name")` removes current binding
- `InputMap.action_add_event("action_name", event)` adds new binding
- Save custom bindings to `user://keybinds.json`
- Load and apply on startup

**Implementation approach:**
1. Settings > Controls shows list of all actions with current key binding
2. Player selects an action, presses desired key
3. System checks for conflicts, saves to file
4. RemapTools addon (Godot Asset Library) handles keyboard + gamepad remapping

#### Additional Accessibility Features

| Feature | Description | Effort | Blocks Launch? |
|---------|-------------|--------|---------------|
| Screen reader labels | `accessible_name` on all UI nodes | 2-3 days | No (post-launch, but do early) |
| Keyboard-only navigation | Full game playable without mouse/touch | Already done (keyboard-first design) | N/A |
| Remappable controls | UI for rebinding keyboard and gamepad | 2-3 days | No (strong nice-to-have) |
| Text-to-speech for combat | Announce damage, loot, level-ups | 1-2 days | No (post-launch) |
| Dyslexia-friendly font option | Add OpenDyslexic or similar as font option | 2 hours | No (post-launch) |
| One-handed mode (mobile) | Rearrange touch controls for one-handed play | 2-3 days | No (post-launch) |
| Subtitle/caption system | For any future voice acting or audio cues | 1 day | No (future) |

### Platform-Specific Differences

- **iOS:** VoiceOver is mandatory for App Store approval of "accessible" apps. Not strictly required for games, but recommended.
- **Android:** TalkBack support via AccessKit. Google Play flags apps that declare accessibility features.
- **Steam:** Steam has accessibility tags. Declaring features improves discoverability.
- **Web:** ARIA attributes on the canvas element. Limited screen reader support for WebGL content.

---

## 6. Performance Budgets

### FPS Targets

| Platform | Target FPS | Minimum Acceptable | Rationale |
|----------|-----------|--------------------|-----------| 
| PC/Mac/Linux | 60 fps | 30 fps | Desktop players expect 60. Indie 2D can easily hit this. |
| Steam Deck | 60 fps | 40 fps (with 40 Hz mode) | Steam Deck supports 40 Hz refresh for battery savings |
| Android | 60 fps (flagship), 30 fps (budget) | 30 fps | Budget phones struggle with 60 fps. Offer quality toggle. |
| iOS | 60 fps | 30 fps | iPhones are powerful enough. iPads too. |
| Web Desktop | 60 fps | 30 fps | Depends on browser/machine. Match desktop. |
| Web Mobile | 30 fps | 24 fps | Browser overhead + mobile hardware. Be realistic. |

### Memory Budgets

| Platform | Max RAM Target | Rationale |
|----------|---------------|-----------|
| PC/Mac/Linux | 2 GB | Generous. Desktop has plenty. |
| Steam Deck | 1 GB | 16 GB total, shared with OS and other apps |
| Android (budget) | 300-512 MB | Low-end phones have 2-3 GB total |
| Android (flagship) | 1 GB | Plenty of headroom |
| iOS | 512 MB-1 GB | iOS aggressively kills background apps |
| Web | 512 MB | Browser tab memory limits vary |

**Our game is 2D pixel art with small tilesets.** Memory usage should be naturally low. Main memory consumers:
- Tileset textures (PNG): ~1-5 MB per biome
- Sprite sheets: ~500 KB-2 MB per entity type
- Save data: ~2.5 MB max (all 10 slots)
- Procedural floor data: ~100-500 KB per cached floor
- Audio: see Section 7

### Renderer Selection

| Renderer | GPU Requirement | Best For | Our Choice |
|----------|----------------|----------|------------|
| Forward+ | Vulkan 1.0 or D3D12 | Desktop 3D. Overkill for 2D. | No |
| Mobile | Vulkan 1.0 or D3D12 | Mobile 3D. Still overkill for 2D. | No |
| **Compatibility** | **OpenGL 3.3 / OpenGL ES 3.0** | **2D games, web, widest hardware support** | **Yes** |

**We already use GL Compatibility.** This is correct. It provides:
- Widest device coverage (works on GPUs from 2010+)
- Required for web export (only renderer that supports WebGL 2)
- Lower memory overhead than Vulkan renderers
- 2D rendering performance was boosted up to 7x in recent Godot versions
- Automatic fallback: if Vulkan is unavailable, engine falls back to Compatibility

### Battery Impact (Mobile)

- **Target:** Less than 15% battery/hour during active gameplay
- **Strategies:**
  - Use Compatibility renderer (lower GPU power draw than Vulkan)
  - Cap FPS to 30 on battery (configurable)
  - Reduce GPU wake frequency during static scenes (Godot 4.6 optimization)
  - Pause rendering when app is backgrounded
  - Half-precision float formats save battery on mobile GPUs

### Implementation Tasks

| Task | Effort | Blocks Launch? |
|------|--------|---------------|
| Add FPS counter to debug panel (already exists via F3) | Done | N/A |
| Profile on minimum-spec Android device | 1 day | Yes (mobile) |
| Profile on oldest supported iPhone (iPhone 8 / SE 2) | 1 day | Yes (iOS) |
| Add quality settings (Low/Medium/High) for mobile | 2 days | Yes (mobile) |
| Implement FPS cap option (30/60/uncapped) | 2 hours | Yes (mobile) |
| Battery-saver mode (cap to 30 fps on battery) | 4 hours | No (nice-to-have) |
| Memory profiling pass on all platforms | 1 day | Yes (mobile) |

---

## 7. Audio

### Format Compatibility

| Format | Godot Support | Use Case | File Size | Cross-Platform? |
|--------|--------------|----------|-----------|----------------|
| **OGG Vorbis** | Full support | Music, ambient loops | Small (compressed) | Yes, all platforms |
| **MP3** | Full support (since Godot 4.x) | Music (alternative to OGG) | Small (compressed) | Yes, all platforms |
| **WAV** | Full support | Short SFX (footsteps, hits, UI clicks) | Large (uncompressed) | Yes, all platforms |

**Recommendation:** OGG for music/ambiance, WAV for short SFX. This is the standard Godot approach and works on all 7 platforms.

**Avoid:** FLAC, AAC, MIDI (not natively supported by Godot's audio import pipeline).

### Web Audio Restrictions

**Critical for web builds:** All modern browsers block audio autoplay until user interaction.

- Chrome, Firefox, Safari all require a click/tap/keypress before audio can play
- Godot's web export handles this automatically: audio context is resumed on first user interaction
- **However:** If our game starts with title music, it will be silent until the player clicks/presses a key
- **Solution:** Add a "Click to Start" splash screen before the title screen. This is standard for web games.
- **Known bug (Godot 4.4+):** Web exports with autoplaying interactive audio streams can crash. Test thoroughly on the target Godot version.

### Mobile Audio Session Management

| Platform | Concern | Handling |
|----------|---------|---------|
| iOS | Audio interrupted by phone calls, Siri, notifications | Godot handles `AVAudioSession` interruptions automatically. Audio resumes when interruption ends. |
| iOS | Silent mode switch | Godot respects the hardware silent switch by default |
| Android | Audio focus (other apps, calls) | Godot handles `AudioManager` focus changes |
| Android | Bluetooth headphone connect/disconnect | Audio route changes handled by OS; Godot follows system audio output |
| Both | App backgrounded | Audio should stop when backgrounded (default Godot behavior). Some players want music to continue -- consider a toggle. |

### Audio Memory Budget

| Content | Format | Estimated Size |
|---------|--------|---------------|
| Music tracks (10-15 tracks) | OGG, ~3 min each | 30-50 MB total |
| SFX library (100-200 effects) | WAV, short clips | 5-15 MB total |
| Ambient loops (5-10 biomes) | OGG, ~2 min each | 10-20 MB total |
| **Total** | | **45-85 MB** |

### Implementation Tasks

| Task | Effort | Blocks Launch? |
|------|--------|---------------|
| Use OGG for music, WAV for SFX (already planned) | Ongoing | Yes |
| Add "Click to Start" screen for web builds | 2 hours | Yes (web) |
| Test audio on iOS with interruptions (calls, Siri) | 2 hours | Yes (iOS) |
| Test audio on Android with Bluetooth headphones | 1 hour | Yes (Android) |
| Add music volume / SFX volume sliders in Settings | 1 day | Yes (all) |
| Add "continue audio when backgrounded" toggle | 2 hours | No (nice-to-have) |

---

## 8. Networking / Analytics

### Crash Reporting

Our analytics doc (`docs/architecture/analytics.md`) already covers crash reporting design. Here are the platform-specific tools:

| Platform | Tool | Integration | Effort |
|----------|------|-------------|--------|
| All (primary) | Sentry for Godot | GDExtension addon (v1.5.0). C++ based. **C# support planned (issue #91) but not shipped yet.** Captures crashes, script errors, hardware info. | 1-2 days |
| All (fallback) | Godot's built-in crash logs | `user://logs/` directory. Check on next launch. No external service needed. | Already exists |
| Steam | Steamworks crash dumps | Automatic for Steam-launched builds. View in Steamworks dashboard. | 0 (free with Steam) |
| Android | Firebase Crashlytics | Requires Firebase SDK plugin. Industry standard for Android. | 2-3 days |
| iOS | Xcode Crash Reports | Automatic via App Store Connect. View in Xcode Organizer. | 0 (free with Apple dev account) |

**Recommended approach:**
1. **Launch:** Use Godot's built-in crash logs + our in-game bug report system (already designed)
2. **Post-launch:** Add Sentry when C# support ships. Free tier covers 5,000 events/month.
3. **Platform-specific:** Steam crash dumps and App Store crash reports come free with those platforms.

### Analytics

Our analytics design is opt-in, GDPR-compliant, and offline-first (see `docs/architecture/analytics.md`). No additional platform-specific work needed for the architecture. The backend choice (Supabase, Cloudflare Workers, etc.) is platform-agnostic.

### GDPR / Privacy Requirements

| Requirement | What It Means For Us | Status |
|-------------|---------------------|--------|
| Opt-in consent | Analytics OFF by default. Explicit user action to enable. | Designed (analytics doc) |
| Right to deletion | "Delete my data" button deletes local telemetry. Backend data deletion via request. | Designed |
| Privacy policy | Plain-language document hosted on game website. Linked from in-game settings. | Needs writing |
| Data safety declarations | Google Play and Apple require declaring what data is collected. | Needs filling out (store submission) |
| Cookie consent (web) | If analytics uses cookies/IndexedDB for tracking: needs consent banner. If no tracking: no banner needed. | Only if web analytics |
| Age gate (COPPA) | If targeting under-13: additional restrictions. Our game is Teen/PEGI 12. Unlikely to need COPPA compliance, but consider. | Probably N/A |
| CCPA (California) | Similar to GDPR. Covered by same opt-in approach. | Covered by GDPR approach |

### Implementation Tasks

| Task | Effort | Blocks Launch? |
|------|--------|---------------|
| Write privacy policy (web page) | 2-3 hours | Yes (mobile stores require it) |
| Fill out Data Safety form (Google Play) | 30 min | Yes (Android) |
| Fill out Privacy Nutrition Labels (Apple) | 30 min | Yes (iOS) |
| Implement Sentry integration | 1-2 days | No (post-launch) |
| Build offline telemetry pipeline (from analytics doc) | 1-2 weeks | No (post-launch) |

---

## 9. Update / Patching

### How Updates Work Per Platform

| Platform | Update Mechanism | Developer Action | Player Experience |
|----------|-----------------|-----------------|-------------------|
| Steam | Steam auto-update | Push new build to Steam depot | Automatic download, player sees "Update available" |
| Google Play | Play Store update | Upload new AAB in Play Console | Auto-update (if enabled) or manual in Play Store |
| Apple App Store | App Store update | Submit new build via Xcode/Transporter. Apple reviews (24-48h). | Auto-update or manual in App Store |
| itch.io (web) | Replace files | Upload new HTML5 build | Instant (next page load gets new version) |
| itch.io (desktop) | itch.io app auto-update | Upload new build, bump version | itch.io app downloads patch |
| Direct download | Player re-downloads | Upload new ZIP to website | Manual download |
| PWA | Service Worker update | Deploy new files to hosting | Browser detects new Service Worker on next visit |

### Delta Patching (Godot 4.6)

Godot 4.6 introduces **delta encoding for Patch PCKs:**
- Patch files include only the changed parts of resources, not entire files
- Real-world result: patch reduced from 8.55 MB to 2.11 MB (75% smaller)
- Must be explicitly enabled in Export > Patching tab
- Slight runtime overhead when patches are loaded (file decompression)
- Uses Semantic Versioning for version tracking
- Excellent for localization updates (adding new language = small patch, not full rebuild)

**When to use PCK patching:**
- Hot-fixing bugs without full redownload
- Adding new content (floors, enemies, items)
- Adding new languages
- NOT a replacement for platform store updates (Steam/Play/App Store handle their own patching)
- Most useful for itch.io desktop builds and direct downloads

### Implementation Tasks

| Task | Effort | Blocks Launch? |
|------|--------|---------------|
| Configure Steam build depots | 1 day | Yes (Steam launch) |
| Configure Play Console release tracks (internal/alpha/beta/production) | 2 hours | Yes (Android) |
| Configure App Store Connect TestFlight + release | 2 hours | Yes (iOS) |
| Test delta PCK patching for itch.io builds | 1 day | No (post-launch) |
| Set up CI/CD pipeline for automated builds (GitHub Actions) | 2-3 days | No (but do before launch) |
| Implement in-game version display (already in debug panel) | Done | N/A |

---

## 10. Testing on Real Devices

### Minimum Test Devices

| Platform | Device | Why This Device |
|----------|--------|----------------|
| **Android (budget)** | Samsung Galaxy A14 or similar (~$150) | Represents the low end. Mali GPU. 3-4 GB RAM. If it runs here, it runs everywhere. |
| **Android (mid-range)** | Samsung Galaxy A54 or Pixel 7a | Represents the bulk of the market. |
| **Android (flagship)** | Samsung Galaxy S24 or Pixel 8 Pro | Verify no issues with latest Snapdragon/Tensor GPU. |
| **Android (tablet)** | Samsung Galaxy Tab A8 or A9 | Test 16:10 aspect ratio + larger screen UI |
| **iPhone (oldest supported)** | iPhone SE 2nd gen or iPhone 8 | Oldest still-supported device. A11 chip. 3 GB RAM. Our low-end iOS target. |
| **iPhone (current)** | iPhone 15 or 16 | Test Dynamic Island, latest iOS. |
| **iPad** | iPad 9th gen or iPad Air | 4:3 aspect ratio. Verify UI scaling. |
| **Steam Deck** | Steam Deck LCD or OLED | 1280x800, 16:10, Linux + Proton. Gamepad-only. |
| **Low-end PC** | Any PC with Intel HD 4000+ or equivalent | Test GL Compatibility renderer on old integrated GPU |

### Remote Testing Services

| Service | Platforms | Free Tier | Best For |
|---------|-----------|-----------|----------|
| Firebase Test Lab | Android (real + virtual), iOS (real) | 5 virtual devices/day, 5 physical devices/day | Quick automated smoke tests |
| BrowserStack | Android, iOS, Web browsers | Free trial only | Manual exploratory testing on 3,500+ devices |
| AWS Device Farm | Android, iOS | 250 device minutes free | Longer automated test runs |
| Samsung Remote Test Lab | Samsung Android devices | Free | Testing on Samsung-specific hardware |

**Recommended approach:**
1. **Primary:** Own 2-3 physical devices (cheap Android, iPhone, Steam Deck)
2. **Supplementary:** Firebase Test Lab free tier for automated smoke tests on diverse Androids
3. **Pre-launch:** BrowserStack trial for manual testing on edge devices

### Godot Remote Debugging

| Method | Supported | Setup |
|--------|-----------|-------|
| USB debugging (Android) | Yes | Enable Developer Options + USB Debugging on device. Connect via USB. Godot exports and installs directly. |
| WiFi debugging (Android) | Partial | Use `adb connect <IP>:5555` after initial USB connection. Godot can deploy over WiFi. |
| USB debugging (iOS) | Yes (via Xcode) | Build in Godot, open Xcode project, deploy to connected device. |
| Remote scene inspector | Yes | Godot's remote scene tree works over USB. Shows live node tree, property values. |
| Print/log output | Yes | `GD.Print()` output visible in Godot's Output panel during remote debug. |

**Known issues:**
- Android remote debug can fail to establish connection even when the app installs successfully
- Godot 4.6 added Android device mirroring directly from the editor (new feature)
- For iOS, the workflow is: Godot export > Xcode open > Xcode deploy. No direct "one-click deploy" like Android.

### Implementation Tasks

| Task | Effort | Blocks Launch? |
|------|--------|---------------|
| Acquire budget Android test device | $100-150 | Yes (mobile) |
| Acquire or borrow iPhone for testing | $200+ or borrow | Yes (iOS) |
| Set up Firebase Test Lab (free tier) | 2 hours | No (but recommended) |
| Test on Steam Deck (acquire or borrow) | $350+ or borrow | Yes (if targeting Steam Deck) |
| Document device test matrix (what was tested where) | 1 day | Yes (QA requirement) |
| Set up Android device mirroring in Godot 4.6 editor | 1 hour | No (dev convenience) |

---

## Priority Summary

### Must Do Before ANY Platform Launch

| Item | Section |
|------|---------|
| Set stretch mode `canvas_items` + aspect `expand` in project.godot | 1 |
| Anchor all UI with Godot's anchor system | 1 |
| Use `Tr()` for all player-facing strings from day 1 | 4 |
| OGG for music, WAV for SFX | 7 |
| Volume sliders in Settings | 7 |
| Privacy policy web page | 8 |

### Must Do Before Desktop Launch (Steam)

| Item | Section |
|------|---------|
| GodotSteam integration (app ID, overlay, achievements) | 3 |
| Resolution/window mode settings | 1 |
| Test on Steam Deck (1280x800) | 10 |
| Configure Steam depots and build pipeline | 9 |
| Verify GL Compatibility renderer on low-end GPU | 6 |

### Must Do Before Mobile Launch

| Item | Section |
|------|---------|
| Safe area handling for notches/cutouts | 1 |
| Touch input system (separate doc exists) | N/A |
| Test C# export on real Android and iOS devices | 10 |
| Profile performance on budget Android | 6 |
| FPS cap option (30/60) | 6 |
| Store listings (Play Console, App Store Connect) | 3 |
| Content ratings (IARC, Apple age rating) | 3 |
| Data safety / privacy nutrition labels | 8 |
| Test audio interruptions (calls, headphones) | 7 |

### Must Do Before Web Launch

| Item | Section |
|------|---------|
| **Wait for Godot C# web export support** | 3 |
| "Click to Start" screen for audio autoplay | 7 |
| IndexedDB persistence warnings | 2 |
| Export/Import save backup prompt | 2 |

### Post-Launch Polish (All Platforms)

| Item | Section |
|------|---------|
| Cloud saves (Steam Cloud, iCloud, Google Play) | 2 |
| Localization (Tier 2-4 languages) | 4 |
| Screen reader accessibility labels | 5 |
| Remappable controls UI | 5 |
| Crash reporting (Sentry) | 8 |
| Analytics pipeline | 8 |
| Delta PCK patching for itch.io updates | 9 |
| Dyslexia-friendly font option | 5 |
| Battery-saver mode | 6 |
| RTL language support | 4 |

---

## Key Links & Resources

### Godot Documentation
- Multiple Resolutions: https://docs.godotengine.org/en/stable/tutorials/rendering/multiple_resolutions.html
- Renderers Overview: https://docs.godotengine.org/en/stable/tutorials/rendering/renderers.html
- Exporting for Android: https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_android.html
- Exporting for iOS: https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_ios.html
- Exporting for Web: https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_web.html
- Internationalizing Games: https://docs.godotengine.org/en/stable/tutorials/i18n/internationalizing_games.html
- Localization with Spreadsheets: https://docs.godotengine.org/en/stable/tutorials/i18n/localization_using_spreadsheets.html
- Importing Audio: https://docs.godotengine.org/en/stable/tutorials/assets_pipeline/importing_audio_samples.html
- InputMap API: https://docs.godotengine.org/en/stable/classes/class_inputmap.html
- General Optimization: https://docs.godotengine.org/en/stable/tutorials/performance/general_optimization.html

### Plugins & Tools
- GodotSteam: https://godotsteam.com/
- Google Play Billing Plugin: https://github.com/godot-sdk-integrations/godot-google-play-billing
- Sentry for Godot: https://github.com/getsentry/sentry-godot
- Notchz (Safe Area): https://godotengine.org/asset-library/asset/3926
- RemapTools (Input Remapping): https://godotengine.org/asset-library/asset/1573
- Godot Accessibility Demo: https://github.com/aefren/godot-accessibility-demo

### Store & Platform
- Steamworks: https://partner.steamgames.com/
- Google Play Console: https://play.google.com/console
- Apple App Store Connect: https://appstoreconnect.apple.com/
- Apple App Store Review Guidelines: https://developer.apple.com/app-store/review/guidelines/
- itch.io: https://itch.io/

### Testing
- Firebase Test Lab: https://firebase.google.com/docs/test-lab
- BrowserStack: https://www.browserstack.com/
- Samsung Remote Test Lab: https://developer.samsung.com/remote-test-lab

### Research References
- Godot Mobile Update (April 2026): https://godotengine.org/article/godot-mobile-update-apr-2026/
- Godot 4.6 Release: https://godotengine.org/releases/4.6/
- C# Platform Support Status: https://godotengine.org/article/platform-state-in-csharp-for-godot-4-2/
- C# Web Export Discussion: https://github.com/godotengine/godot-proposals/discussions/10310
- Steam Localization Language Priority Guide: https://uhiyama-lab.com/en/blog/gamedev/steam-game-localization-language/
- IndexedDB Storage Quotas (MDN): https://developer.mozilla.org/en-US/docs/Web/API/Storage_API/Storage_quotas_and_eviction_criteria
- Godot 4.5 Accessibility: https://www.gamedeveloper.com/programming/godot-4-5-ushers-in-accessibility-features-including-screen-reader-support
- Google Play Billing for Godot: https://github.com/code-with-max/godot-google-play-iapp
- iCloud Saves in Godot: https://stevensplint.com/save-game-synchronization-with-icloud-in-godot/
- Steam Cloud Sync: https://simondalvai.org/blog/save-game-sync/

## Real-World Gotchas (From Shipped Games)

Compiled from StackOverflow, Godot forums, Reddit, and developer post-mortems.

### Mobile Rendering Crashes
- Godot 4.4.1 crashes constantly on Android when using **Mobile renderer** — switch to **Compatibility** (we already use this). Source: [Godot Forum](https://forum.godotengine.org/t/godot-4-crashes-on-mobile-rendering-mode/117533)
- Two shipped games (Kamaeru, Rift Riff, Dec 2025) had ~4% crash rate on mobile due to Vulkan API and GPU driver issues. Fixed in 4.5.2/4.6, crash rate dropped to <1%. Source: [Godot Mobile Update April 2026](https://godotengine.org/article/godot-mobile-update-apr-2026/)

### C# + NativeAOT (iOS)
- NativeAOT does NOT support cross-OS compilation — must build on the target platform (Mac for iOS). Source: [Godot Forum](https://forum.godotengine.org/t/how-to-use-nativeaot-with-godot-c-on-android/75528)
- Working Android project crashes on iOS due to NativeAOT trimming removing code it thinks is unused. Source: [Godot Forum](https://forum.godotengine.org/t/working-android-project-crashes-on-ios/109513)
- SQLite and other native libraries fail on Android — can't find .so files. Source: [Godot Forum](https://forum.godotengine.org/t/sqlite-and-c-on-android/129627)

### UI on Mobile
- No built-in kinetic scrolling for touch — must implement manually for any scrollable list
- No built-in screen transitions between UI panels — must code from scratch (we already have `ScreenTransition.cs`)
- Custom OS dialogs (file pickers, etc.) feel non-native — avoid them

### Save Data
- Steam Auto-Cloud does NOT delete files cross-platform — if a save is deleted on one device, it persists on others. Must handle conflict resolution. Source: [Simon Dalvai](https://simondalvai.org/blog/save-game-sync/)
- Safari deletes IndexedDB data after 7 days of no user interaction (web saves at risk)

### Cloud Save Options

| Platform | Solution | Notes |
|----------|----------|-------|
| Steam | Steam Auto-Cloud or [godot-steam-cloud](https://github.com/softwoolco/godot-steam-cloud) | No delete sync |
| iOS | iCloud Key-Value Storage via [iOS Plugins](https://stevensplint.com/save-game-synchronization-with-icloud-in-godot/) | 3 storage types available |
| Android | Google Play Games Services | Less documented for Godot |
| Cross-platform | [GD-Sync](https://www.gd-sync.com/) cloud storage | Works on all Godot platforms |
| Cross-platform | [GodotBaaS](https://dashboard.godotbaas.com/blog/godot-cloud-saves-guide) | Backend-as-a-service |

### Plugin Ecosystem
- "If there's an existing plugin, it's easy. If not, you're on your own." — quality varies wildly, documentation often poor
- GDScript plugins work in C# projects (they share the Input singleton) but no type safety across the boundary

## Open Questions

1. Should we launch English-only and patch in translations, or delay launch until Tier 2 languages are ready?
2. When should we invest in real test devices vs. relying on emulators/cloud services?
3. Should we block mobile launch until C# mobile export leaves "experimental" status, or ship on experimental?
4. Do we want to support landscape-only or also allow portrait mode for mobile?
5. Should we target Steam Deck verification (Valve's official "Deck Verified" badge)?
6. What's our policy on minimum OS versions? (e.g., Android 8+ or Android 10+? iOS 15+ or iOS 16+?)
7. Should cloud saves be per-platform (Steam Cloud for Steam, iCloud for iOS) or unified via custom backend?
