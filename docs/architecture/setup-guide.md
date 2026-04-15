# Development Environment Setup

## Summary

Complete setup guide for developing "A Dungeon in the Middle of Nowhere" with Godot 4 + C#. This project requires the **.NET edition** of Godot (separate download from standard Godot) and the .NET 9 SDK.

## Current State

Environment setup is a prerequisite for all implementation work. The project is currently in docs-only mode — this guide prepares the machine for when coding begins.

## Prerequisites

| Tool | Version | Install | Verify |
|------|---------|---------|--------|
| .NET SDK | 9.0+ | `brew install dotnet` | `dotnet --version` |
| Godot (.NET) | 4.6+ | Download from [godotengine.org](https://godotengine.org/download) (.NET build) | `godot --version` |
| VS Code | Latest | Already installed | `code --version` |
| Git | 2.x+ | Already installed | `git --version` |
| Python 3 | 3.10+ | Already installed | `python3 --version` |

## Install Steps

### 1. .NET 9 SDK

```bash
# Install via Homebrew
brew install dotnet

# Verify
dotnet --version    # Should show 9.0.x
dotnet --list-sdks  # Should list at least one SDK
```

If `brew install dotnet` doesn't work, download directly from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/9.0).

### 2. Godot 4 (.NET Edition)

The standard Godot download does **not** include C# support. You must download the **.NET** build specifically.

1. Go to [godotengine.org/download](https://godotengine.org/download)
2. Under your platform, select the **".NET"** variant (not "Standard")
3. Move to `/Applications/` (macOS)
4. Create a symlink for terminal access:

```bash
# macOS example — adjust path to match the actual .app name
ln -sf "/Applications/Godot_mono.app/Contents/MacOS/Godot" /usr/local/bin/godot

# Verify
godot --version    # Should show 4.6.x.stable.mono
```

The `.mono` suffix in the version string confirms C# support is enabled.

### 3. VS Code Extensions

Install these extensions for C# + Godot development:

```bash
# C# language support (IntelliSense, debugging, refactoring)
code --install-extension ms-dotnettools.csharp

# .NET runtime for extensions
code --install-extension ms-dotnettools.vscode-dotnet-runtime

# Godot integration (scene preview, GDScript support for .tscn files)
code --install-extension geequlim.godot-tools
```

### 4. Project Setup

```bash
cd infinite-dungeon-game

# Configure git hooks
make setup

# Restore NuGet packages
dotnet restore

# Verify build
dotnet build

# Run tests
dotnet test
```

### 5. Verify C# Works in Godot

1. Open the project in Godot (.NET edition)
2. Right-click any node → "Attach Script"
3. In the Language dropdown, confirm **C#** appears as an option
4. Cancel (don't create a script yet — we're in docs-only mode)

If C# is not listed, you're using the standard (non-.NET) Godot build.

## VS Code Configuration

### Recommended settings for Godot C#

Add to `.vscode/settings.json` (project-level):

```json
{
  "dotnet.defaultSolution": "DungeonGame.sln",
  "omnisharp.enableRoslynAnalyzers": true,
  "editor.formatOnSave": true,
  "[csharp]": {
    "editor.defaultFormatter": "ms-dotnettools.csharp",
    "editor.tabSize": 4,
    "editor.insertSpaces": true
  }
}
```

### Godot external editor setup

In Godot: Editor → Editor Settings → Text Editor → External:
- **Use External Editor:** On
- **Exec Path:** `/usr/local/bin/code` (or output of `which code`)
- **Exec Flags:** `{project} --goto {file}:{line}:{col}`

This makes double-clicking a script in Godot open it in VS Code.

## Troubleshooting

### "C# not available in Godot"
You have the standard Godot build. Download the .NET variant from godotengine.org.

### "dotnet: command not found"
The .NET SDK is not in your PATH. Run `brew install dotnet` or add the install location to your shell profile.

### Build errors after NuGet restore
Try `dotnet clean && dotnet restore && dotnet build`. If GdUnit4 has version conflicts, check that your Godot version matches the GdUnit4 compatibility matrix.

### "Godot.NET.Sdk not found"
Your .csproj needs the correct SDK version. The SDK version should match your Godot version:
```xml
<Project Sdk="Godot.NET.Sdk/4.4.0">
```

## Platform Notes

| Platform | Status | Notes |
|----------|--------|-------|
| macOS (Apple Silicon) | Supported | Primary dev platform. Universal binary. |
| macOS (Intel) | Supported | Same as Apple Silicon. |
| Windows | Supported | Use `winget install dotnet-sdk-9` for .NET. |
| Linux | Supported | Use package manager for .NET SDK. |
| Web export | Not supported | C# web export is blocked in Godot 4.6. Desktop only. |
