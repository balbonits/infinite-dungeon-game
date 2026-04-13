#!/usr/bin/env bash
# Build executables for all platforms.
# Requires: Godot .NET editor with export templates installed.
# Usage: ./scripts/build.sh [--godot /path/to/godot]
#
# Output goes to build/ (gitignored).

set -euo pipefail

# Default Godot paths by OS
case "$(uname -s)" in
    Darwin)  GODOT_DEFAULT="/Applications/Godot_mono.app/Contents/MacOS/Godot" ;;
    Linux)   GODOT_DEFAULT="godot" ;;
    MINGW*|MSYS*|CYGWIN*) GODOT_DEFAULT="godot.exe" ;;
    *)       GODOT_DEFAULT="godot" ;;
esac

GODOT="${GODOT_DEFAULT}"

# Parse args
while [[ $# -gt 0 ]]; do
    case "$1" in
        --godot) GODOT="$2"; shift 2 ;;
        *) echo "Unknown arg: $1"; exit 1 ;;
    esac
done

# Verify Godot exists
if ! command -v "$GODOT" &>/dev/null && [[ ! -x "$GODOT" ]]; then
    echo "Error: Godot not found at '$GODOT'"
    echo "Install Godot .NET or pass --godot /path/to/godot"
    exit 1
fi

PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
BUILD_DIR="${PROJECT_DIR}/build"

echo "=== Building Dungeon Game ==="
echo "Godot:   $GODOT"
echo "Project: $PROJECT_DIR"
echo "Output:  $BUILD_DIR"
echo ""

# Clean previous builds
rm -rf "$BUILD_DIR"
mkdir -p "$BUILD_DIR"

# Build C# project first
echo "── Compiling C#..."
dotnet build "${PROJECT_DIR}/DungeonGame.csproj" --nologo -v q 2>&1 | tail -3
echo ""

# Export each platform
FAILED=0

export_platform() {
    local preset="$1"
    local output="$2"
    echo "── Exporting: ${preset}..."
    if "$GODOT" --headless --path "$PROJECT_DIR" --export-debug "$preset" "$output" 2>&1 | tail -3; then
        echo "   ✓ ${preset} → ${output}"
    else
        echo "   ✗ ${preset} failed (missing export template?)"
        FAILED=1
    fi
    echo ""
}

export_platform "macOS"             "${BUILD_DIR}/DungeonGame.app"
export_platform "Windows Desktop"   "${BUILD_DIR}/DungeonGame.exe"
export_platform "Linux"             "${BUILD_DIR}/DungeonGame.x86_64"

# Summary
echo "=== Build Summary ==="
ls -lh "$BUILD_DIR"/*.app "$BUILD_DIR"/*.exe "$BUILD_DIR"/*.x86_64 2>/dev/null || true
echo ""

if [[ $FAILED -eq 0 ]]; then
    echo "✅ All platforms exported to build/"
else
    echo "⚠️  Some exports failed. Install missing export templates via:"
    echo "   Godot Editor → Editor → Manage Export Templates → Download and Install"
    exit 1
fi
