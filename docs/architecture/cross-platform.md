# Cross-Platform Export Research

## Summary

Research into exporting this Godot 4.6 + C# (.NET 8) game to Web (browser), Android, and iOS. Currently exports to macOS, Windows, and Linux desktop.

| Platform | Status | Runtime | Effort | Priority |
|----------|--------|---------|--------|----------|
| Desktop (macOS/Win/Linux) | Shipping | CoreCLR | Done | 1 |
| Android | Experimental (functional) | Mono (linux-bionic) | 4-8 weeks | 2 |
| iOS | Experimental (functional) | NativeAOT | 3-5 weeks (on top of Android) | 3 |
| Web (Browser) | NOT SUPPORTED | Mono/WASM prototype | Wait for official support | 4 |

---

## Web (Browser)

### Status: NOT SUPPORTED

Godot 4.6 does not support C# web export. GDScript web export works; the blocker is the .NET runtime.

### Current Progress

- GodotCon Boston (May 2025): Raul Santos demonstrated a working prototype using statically-linked Mono in WASM
- [PR #106125](https://github.com/godotengine/godot/pull/106125) implements web export but remains draft/WIP
- Could ship in "the next Godot release" but no committed timeline

### Prototype Limitations

- ~72 MiB .pck file (~23.8 MiB Brotli-compressed)
- No globalization support (invariant mode only)
- Some .NET APIs (cryptography, certain I/O) don't work
- Single-threaded audio causes glitches during frame drops
- Threading model restrictions — `Task.Wait`, `Monitor.Enter` forbidden in browsers

### Impact on Our Codebase

- Async floor generation would need rearchitecting (no blocking main thread)
- `user://` maps to IndexedDB (async, storage-limited)
- No dedicated audio thread

### Community Workaround

[ComplexRobot's godot-dotnet-web-export](https://github.com/ComplexRobot/godot-dotnet-web-export) — unofficial fork with the PR merged. Requires custom build tooling. Prototyping only, not production.

### Option B: Fork to GDScript for Web

If web is needed before official C# support ships, the project could be forked and ported to GDScript:

- **Fork the repo** → `infinite-dungeon-game-web`
- **Port `scripts/logic/`** to GDScript (pure logic, no Godot API changes)
- **Port `scripts/ui/`** to GDScript (heaviest effort — UI code is the bulk)
- **Port `scripts/autoloads/`** to GDScript
- **Maintain both repos** — C# stays canonical, GDScript web fork cherry-picks design changes

**Estimated effort:** 6-10 weeks for initial port, then ongoing sync cost.

**Trade-off:** High upfront cost + maintenance burden, but unblocks web immediately. Only worth it if web distribution is critical for growth (e.g., itch.io, Newgrounds, or embedding on a website).

### Recommendation

**Wait for official C# web support** unless web distribution is a business priority. If it is, fork to GDScript for web only. Estimated effort once official support ships: 2-4 weeks. Fork effort: 6-10 weeks.

---

## Android

### Status: EXPERIMENTAL (Functional Since Godot 4.2)

C# Android export uses the **linux-bionic Mono runtime** (not CoreCLR). Supports arm64 and x64. Crash rates below 1% for shipped games after fixes in 4.5.2 and 4.6.

### Prerequisites

- JDK 17
- Android SDK with API 30+ (API 35 required for Play Store as of August 2025)
- Android NDK (Side by side), SDK Command-line Tools, CMake
- .NET 8+
- Godot .NET Android export templates
- Debug/release keystore
- Google Play Developer Account ($25 one-time)

### Google Play Requirements

- Target API level 35 for all new apps/updates (August 2025)
- 16 KB page size support for 64-bit devices targeting Android 15+ (November 2025)
- Godot 4.6 + community patches address API 35

### Required Code Changes

| Change | Effort | Notes |
|--------|--------|-------|
| Touch input system (virtual joystick, action buttons) | High | See touch-controls.md |
| UI scaling for small screens (720p-1440p at mobile DPI) | High | Container nodes + anchors |
| Screen orientation lock (landscape) | Low | Export setting |
| Performance profiling on mobile GPUs | Medium | Test on real devices |
| Storage permission handling for saves | Low | Godot handles `user://` |

### Gotchas

- No Android JNI bindings — SSL/HTTPS via native Android APIs will crash. Use Godot's built-in HTTP.
- No C# debugging on device. Debug via logging only.
- APK size: Mono runtime adds 50-100 MiB minimum.
- NativeAOT is theoretically possible but broken. Stick with Mono.
- Some startup crashes on specific Samsung devices (mostly fixed in 4.6).

### Recommendation

**Feasible.** The runtime works. The real effort is UI/UX redesign for touch. Estimated: 4-8 weeks.

---

## iOS

### Status: EXPERIMENTAL (Supported Since Godot 4.2)

iOS uses **.NET NativeAOT** — Apple forbids JIT compilation. All C# is ahead-of-time compiled to native ARM64. Export produces an Xcode project that must be built on a Mac.

### Prerequisites

- Mac (required — no cross-compilation)
- Xcode (latest stable, with iOS support)
- Apple Developer Account ($99/year)
- .NET 8.0+ (NativeAOT iOS support experimental in .NET 8)
- Godot .NET iOS export templates
- Provisioning profile + signing certificate
- App Store Team ID + Bundle Identifier

### Required Code Changes (on top of Android touch/UI work)

| Change | Effort | Notes |
|--------|--------|-------|
| NativeAOT compatibility audit | Medium | No runtime reflection allowed |
| Source generator validation | Medium | Verify serialization survives trimming |
| Safe area / notch handling | Medium | iPhones have notches, Dynamic Island, home indicator |
| App Store metadata | Low | Screenshots, privacy policy, age rating |

### Gotchas

- NativeAOT + reflection = crashes. Assembly size ~18 MiB (can't trim further).
- iOS simulator template only supports x64 — need Rosetta on Apple Silicon or physical device.
- No hot reload. Full NativeAOT recompilation on every change. Much slower iteration.
- App Store review process adds time and uncertainty.
- No C# debugging on device.

### Recommendation

**Feasible, more constrained than Android.** Builds on Android UI/touch work. Estimated: 3-5 weeks on top of Android.

---

## Cross-Cutting Concerns

### Feature Compatibility

| Feature | Desktop | Android | iOS | Web |
|---------|---------|---------|-----|-----|
| CoreCLR runtime | Yes | No (Mono) | No (NativeAOT) | No |
| Full multithreading | Yes | Yes | Yes | Limited |
| Reflection | Yes | Yes | Risky | Risky |
| `System.Text.Json` source gen | Yes | Yes | Yes | Likely |
| File I/O (`user://`) | Native FS | Android storage | iOS sandbox | IndexedDB |
| SSL/HTTPS | Yes | Use Godot HTTP | Test needed | Browser |

### Architectural Recommendations

1. **Platform abstraction for input** — interface-based, swap at startup (keyboard vs touch vs gamepad)
2. **Responsive UI** — Container nodes and anchors, not fixed pixel positions
3. **No runtime reflection** — use source generators everywhere (future-proofs for iOS/Web)
4. **Async over blocking** — use `await` patterns, never `Task.Wait` (future-proofs for Web)
5. **Small save files** — web IndexedDB limits, mobile I/O speed
6. **Minimize `#if` directives** — use interfaces for platform differences

### Priority Order

1. **Desktop** — already shipping, focus here first
2. **Android** — largest mobile audience, functional export pipeline
3. **iOS** — builds on Android work, higher revenue per user, more constraints
4. **Web** — blocked until official C# support ships

---

## Key Links

### Official Docs
- [Exporting for Android](https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_android.html)
- [Exporting for iOS](https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_ios.html)
- [Exporting for Web](https://docs.godotengine.org/en/latest/tutorials/export/exporting_for_web.html)

### C# Platform Status
- [C# platform support in Godot 4.2 (official blog)](https://godotengine.org/article/platform-state-in-csharp-for-godot-4-2/)
- [Godot Mobile update — April 2026](https://godotengine.org/article/godot-mobile-update-apr-2026/)
- [Web .NET prototype at GodotCon Boston](https://godotengine.org/article/live-from-godotcon-boston-web-dotnet-prototype/)

### GitHub Tracking
- [PR #106125: .NET web export (draft)](https://github.com/godotengine/godot/pull/106125)
- [Issue #70796: Re-add C# web export](https://github.com/godotengine/godot/issues/70796)
- [Issue #68153: C# Android support](https://github.com/godotengine/godot/issues/68153)

### Community
- [ComplexRobot's unofficial web export fork](https://github.com/ComplexRobot/godot-dotnet-web-export)
- [Google Play target API requirements](https://developer.android.com/google/play/requirements/target-sdk)
