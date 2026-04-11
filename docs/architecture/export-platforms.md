# Export Platforms

## Summary

Cross-platform export targets for "A Dungeon in the Middle of Nowhere." The game builds as a native desktop application on macOS, Windows, and Linux. Web and mobile are blocked.

## Current State

- **Development platform:** macOS (Apple Silicon + Intel)
- **Engine:** Godot 4.6 (.NET edition)
- **Renderer:** GL Compatibility (`gl_compatibility` in project.godot)
- **Language:** C# / .NET 8+
- **Export pipeline:** Not yet configured. No export templates installed, no CI/CD.

## Design

### Supported Platforms

| Platform | Status | Runtime | Notes |
|----------|--------|---------|-------|
| macOS | Primary | CoreCLR | Universal binary (Apple Silicon + Intel). Primary dev and test platform. |
| Windows | Supported | CoreCLR | x86_64. Largest desktop audience. |
| Linux | Supported | CoreCLR | x86_64. Godot's native platform. |

### Blocked Platforms

| Platform | Reason | Revisit when |
|----------|--------|--------------|
| Web (HTML5) | Godot 4.6 does not support C# web export. The .NET runtime cannot compile to WebAssembly with Godot's current toolchain. | Godot officially ships C# web export support. |
| Android | Requires touch input system, UI scaling for small screens, and Mono runtime (not CoreCLR). Not targeted. | Desktop version is feature-complete and touch input is designed. |
| iOS | Requires NativeAOT compilation, Apple Developer account, touch input, and UI scaling. Not targeted. | Same as Android. |

### What Makes the Game Cross-Platform

All game code, assets, and scenes are platform-agnostic by default:

- **C# source** compiles identically on all CoreCLR targets
- **PNG assets** are standard image files, no platform-specific formats
- **Scene files (.tscn)** are text-based and portable
- **GL Compatibility renderer** targets OpenGL 3.3 / OpenGL ES 3.0, covering integrated GPUs and older hardware across all desktop platforms
- **Input Map** uses named actions, not raw keycodes (supports rebinding per platform)
- **Save paths** use Godot's `user://` prefix, which maps to OS-appropriate directories automatically

### Export Requirements Per Platform

Each platform needs Godot export templates installed matching the engine version (4.6.x).

**macOS:**
- Export template: `macos.zip` from Godot's download page (or installed via Editor > Manage Export Templates)
- Produces: `.app` bundle (universal binary) or `.dmg`
- Consideration: Unsigned apps trigger Gatekeeper. For distribution, needs code signing + notarization (Apple Developer account, $99/year)

**Windows:**
- Export template: `windows_debug.exe` / `windows_release.exe`
- Produces: `.exe` + `.pck` (or single-file `.exe` with embedded PCK)
- Consideration: Unsigned executables trigger SmartScreen warnings. Code signing certificate recommended for distribution.

**Linux:**
- Export template: `linux_debug.x86_64` / `linux_release.x86_64`
- Produces: Binary executable + `.pck`
- Consideration: No signing needed. Distribute as `.tar.gz` or via platforms like itch.io / Steam.

### Future: CI/CD Export Pipeline

GitHub Actions workflow to automate builds:

- Trigger on tagged releases or manual dispatch
- Install Godot .NET headless + export templates
- Run `dotnet build` and `dotnet test` before export
- Export for all three platforms in parallel
- Upload artifacts (or push to distribution platform)

### Future: Makefile Targets

CLI build targets for local exports:

- `make export-macos` -- export macOS build
- `make export-windows` -- export Windows build
- `make export-linux` -- export Linux build
- `make export-all` -- export all platforms

These will wrap `godot --headless --export-release` commands with the correct preset names.

## Open Questions

- Should export templates be committed to the repo for reproducible builds, or downloaded on-demand in CI?
- When should we set up code signing for macOS and Windows? (Before first public release, or earlier for playtesting?)
- Should we target ARM64 Linux in addition to x86_64?
- Which distribution platform(s) to target first? (itch.io, Steam, direct download)
