using Godot;

namespace DungeonGame.Autoloads;

/// <summary>
/// Save-file management with multi-slot support (UI-02).
///
/// Slot model:
/// - Three save slots (0, 1, 2). Files live at <c>user://saves/save_{slot}.json</c>.
/// - <see cref="GameState.CurrentSaveSlot"/> tracks which slot is active during a run.
/// - Auto-save writes to the current slot.
/// - The legacy <c>autosave.json</c> location is no longer written; saves from pre-UI-02
///   builds can still be read from slot 0 if migrated manually.
///
/// Uses <see cref="ISaveStorage"/> for file I/O so the slot logic is unit-testable
/// (production injects <see cref="GodotFileSaveStorage"/>, tests inject a fake).
/// </summary>
public partial class SaveManager : Node
{
    public static SaveManager Instance { get; private set; } = null!;

    public const int SlotCount = 3;

    // Production save directory. Distinct from the test-sandbox directory
    // below so tests can never clobber real player saves.
    private const string ProductionSaveDir = "user://saves";
    private const string TestSandboxSaveDir = "user://test_saves";

    /// <summary>Active save directory. Defaults to <c>user://saves</c>;
    /// tests flip to <c>user://test_saves</c> via <see cref="UseTestSandbox"/>.</summary>
    public static string SaveDir { get; private set; } = ProductionSaveDir;

    // True after the first UseTestSandbox call in a process. A subsequent
    // scene reload (e.g., ResetToFreshSplash → ReloadCurrentScene → Main._Ready)
    // must NOT re-wipe sandbox files — otherwise a save seeded mid-test is
    // lost when the test reloads.
    private static bool _sandboxInitialized;

    /// <summary>
    /// Route all saves to <c>user://test_saves/</c>. Call at the very top of
    /// any GoDotTest run so fabricated-save fixtures can't overwrite real
    /// player data. Idempotent — safe to call repeatedly.
    ///
    /// First call wipes any pre-existing sandbox files so runs start
    /// deterministic. Subsequent calls in the same process leave the sandbox
    /// intact.
    /// </summary>
    public static void UseTestSandbox()
    {
        SaveDir = TestSandboxSaveDir;
        DirAccess.MakeDirAbsolute(SaveDir);
        if (!_sandboxInitialized)
        {
            WipeSandboxFiles();
            _sandboxInitialized = true;
            GD.Print($"[SaveManager] Using TEST SANDBOX save dir: {SaveDir} (wiped on first init)");
        }
    }

    private static void WipeSandboxFiles()
    {
        using var dir = DirAccess.Open(TestSandboxSaveDir);
        if (dir == null) return;
        foreach (var file in dir.GetFiles())
        {
            if (file.EndsWith(".json"))
                dir.Remove(file);
        }
    }

    /// <summary>
    /// Public test-only: wipe every <c>save_*.json</c> in the sandbox dir.
    /// Safe to call repeatedly; refuses to touch production saves by checking
    /// that SaveDir is currently the sandbox. Called by
    /// <c>GameTestBase.ResetToFreshSplash</c> between tests so the ClassSelect
    /// → save-slot-0 flow in one test doesn't leave slot 0 populated for the
    /// next test — which, after 3 confirmations, filled all slots and made
    /// every subsequent New Game click hit the slots-full dialog instead of
    /// the class-select screen (cascaded through Death/Guild/Npc/PauseMenu).
    /// </summary>
    public static void WipeAllSandboxSaves()
    {
        if (SaveDir != TestSandboxSaveDir) return;
        WipeSandboxFiles();
    }

    /// <summary>Flip back to the real user save directory (post-test teardown).</summary>
    public static void UseProductionSaves()
    {
        SaveDir = ProductionSaveDir;
        DirAccess.MakeDirAbsolute(SaveDir);
    }

    /// <summary>Storage backend. Overridable for tests.</summary>
    public ISaveStorage Storage { get; set; } = new GodotFileSaveStorage();

    public override void _Ready()
    {
        Instance = this;
        DirAccess.MakeDirAbsolute(SaveDir);
    }

    public static string SlotPath(int slotIndex) =>
        $"{SaveDir}/save_{slotIndex}.json";

    // ── Slot-aware API ──────────────────────────────────────────────────

    public bool HasSave(int slotIndex) => Storage.Exists(SlotPath(slotIndex));

    /// <summary>Peek at a slot's save data without restoring it into GameState.</summary>
    public SaveData? PeekSlot(int slotIndex)
    {
        var json = Storage.Read(SlotPath(slotIndex));
        return json == null ? null : SaveSystem.Deserialize(json);
    }

    /// <summary>
    /// Load the given slot into GameState. Updates <see cref="GameState.CurrentSaveSlot"/>
    /// on success. Returns false if the slot is empty or the file is corrupt.
    /// </summary>
    public bool LoadSlot(int slotIndex)
    {
        var data = PeekSlot(slotIndex);
        if (data == null) return false;

        SaveSystem.RestoreState(GameState.Instance, data);
        GameState.Instance.CurrentSaveSlot = slotIndex;
        GD.Print($"Game loaded from slot {slotIndex}");
        return true;
    }

    /// <summary>Capture GameState and write to the given slot. Returns false on I/O failure.</summary>
    public bool SaveToSlot(int slotIndex)
    {
        var data = SaveSystem.CaptureState(GameState.Instance);
        string json = SaveSystem.Serialize(data);
        bool ok = Storage.Write(SlotPath(slotIndex), json);
        if (ok)
            GD.Print($"Game saved to slot {slotIndex}");
        else
            GD.PrintErr($"Game save FAILED for slot {slotIndex}");
        return ok;
    }

    public void DeleteSlot(int slotIndex)
    {
        Storage.Delete(SlotPath(slotIndex));
        GD.Print($"Slot {slotIndex} deleted");
    }

    /// <summary>Index of the first empty slot, or null if all slots are full.</summary>
    public int? FindFirstEmptySlot()
    {
        for (int i = 0; i < SlotCount; i++)
            if (!HasSave(i)) return i;
        return null;
    }

    /// <summary>True if every slot is occupied.</summary>
    public bool AreAllSlotsFull()
    {
        for (int i = 0; i < SlotCount; i++)
            if (!HasSave(i)) return false;
        return true;
    }

    /// <summary>True if at least one slot has a save.</summary>
    public bool AnySaveExists()
    {
        for (int i = 0; i < SlotCount; i++)
            if (HasSave(i)) return true;
        return false;
    }

    // ── Current-slot auto-save convenience ──────────────────────────────

    /// <summary>Save to the current slot (or slot 0 if none is active). Returns false on I/O failure.</summary>
    public bool Save()
    {
        int slot = GameState.Instance.CurrentSaveSlot ?? 0;
        return SaveToSlot(slot);
    }

    /// <summary>Load the current slot (or slot 0 if none is active). True on success.</summary>
    public bool Load()
    {
        int slot = GameState.Instance.CurrentSaveSlot ?? 0;
        return LoadSlot(slot);
    }

    // ── Backwards-compatible helpers ────────────────────────────────────

    /// <summary>
    /// True if any save slot contains data. Use the slot-indexed overload for a specific slot.
    /// Retained so existing splash-screen callers keep working.
    /// </summary>
    public bool HasSave() => AnySaveExists();

    /// <summary>Peek at the first populated slot, or null if none exist.</summary>
    public SaveData? PeekSave()
    {
        for (int i = 0; i < SlotCount; i++)
            if (HasSave(i)) return PeekSlot(i);
        return null;
    }
}
