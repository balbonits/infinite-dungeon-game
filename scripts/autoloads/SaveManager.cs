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
    private const string SaveDir = "user://saves";

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
