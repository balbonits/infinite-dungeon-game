using System.IO;
using FluentAssertions;
using Xunit;

namespace DungeonGame.Tests.Unit;

/// <summary>
/// Regression for AUDIT-03. SaveManager.Save() and Load() must refuse when
/// <c>GameState.CurrentSaveSlot</c> is null instead of silently falling
/// through to slot 0 — a fall-through could overwrite an unrelated save
/// if an auto-save fires before the splash flow has reserved a slot.
///
/// Source-level guard rather than behavioral test because <c>SaveManager</c>
/// is a Godot Node that depends on the GameState autoload; the unit test
/// project can't link Godot, so we pin the production source shape directly
/// (same pattern as <see cref="ToastDismissGuardTests"/>).
/// </summary>
public class SaveManagerGuardTests
{
    [Fact]
    public void SaveManager_Save_RefusesNullCurrentSaveSlot()
    {
        string src = ReadRepoSource("scripts/autoloads/SaveManager.cs");

        src.Should().NotContain("CurrentSaveSlot ?? 0",
            "AUDIT-03: Save()/Load() must not fall through to slot 0 via `?? 0` — " +
            "that silently overwrites slot 0 when no character owns a slot. " +
            "Use an explicit null-check that returns false.");

        src.Should().Contain("Save() refused: CurrentSaveSlot is null",
            "Save() must log a distinct error and return false when CurrentSaveSlot " +
            "is null. The log text is part of the contract — operator-visible signal " +
            "that an auto-save fired before slot reservation.");

        src.Should().Contain("Load() refused: CurrentSaveSlot is null",
            "Load() must symmetrically refuse and log when CurrentSaveSlot is null " +
            "(prevents silent slot-0 restore over partial state the caller already held).");
    }

    private static string ReadRepoSource(string relPath)
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            string candidate = Path.Combine(dir.FullName, relPath);
            if (File.Exists(candidate)) return File.ReadAllText(candidate);
            dir = dir.Parent;
        }
        throw new FileNotFoundException($"Could not locate {relPath} walking up from cwd");
    }
}
