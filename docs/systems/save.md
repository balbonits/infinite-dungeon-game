# Save System

## Summary

Game state is saved to local JSON files automatically at key moments. 10 independent save slots, one character per slot. Players can export/import saves as Base64 strings for backup and sharing.

## Current State

**Spec status: LOCKED.** Save slot count, schema, auto-save behavior, and export/import are defined and locked.

Save/export functionality is referenced in the project design but not yet implemented in the Godot prototype. The design below covers the full specification, ported from the Phaser prototype's `localStorage` approach to Godot's `FileAccess` API.

## Design

### Storage Mechanism

| Property | Phaser (Old) | Godot (New) |
|----------|-------------|-------------|
| API | `localStorage.setItem()` / `localStorage.getItem()` | `FileAccess.Open()` / `file.StoreString()` |
| Key / Path | `dungeonGame_save` (localStorage key) | `user://saves/slot_N.json` (N = 1-10) |
| Slots | 1 (browser storage) | 10 independent character slots |
| Format | JSON string | JSON string (pretty-printed with tabs) |
| Location | Browser storage (per-origin) | OS-specific app data directory |

**`user://` resolves to:**

| OS | Path |
|----|------|
| macOS | `~/Library/Application Support/Godot/app_userdata/A Dungeon in the Middle of Nowhere/` |
| Windows | `%APPDATA%\Godot\app_userdata\A Dungeon in the Middle of Nowhere\` |
| Linux | `~/.local/share/godot/app_userdata/A Dungeon in the Middle of Nowhere/` |

The exact directory name is derived from the `application/config/name` value in `project.godot`. Godot creates this directory automatically on first write.

### Save Slots

**10 independent save slots.** Each slot holds one character with its own class, stats, inventory, and dungeon progress. Slots are fully independent — deleting one does not affect others.

- File per slot: `user://saves/slot_1.json` through `user://saves/slot_10.json`
- Backup per slot: `user://saves/slot_N_backup.json` (single rolling backup)
- Estimated size per slot: 50-250KB (verbose JSON). 10 slots = 2.5MB max. Trivial.

### Save Data Structure

The save captures **all** persistent state. Verbose format for fast loading — no deferred reads or lazy loading needed.

```json
{
    "version": 2,
    "slot": 1,
    "character": {
        "name": "Player Name",
        "class": "warrior",
        "level": 1,
        "xp": 0,
        "hp": 100,
        "max_hp": 100,
        "mana": 60,
        "max_mana": 60,
        "stats": {
            "str": 8,
            "dex": 6,
            "sta": 7,
            "int": 5
        },
        "free_stat_points": 0,
        "free_skill_points": 0,
        "deepest_floor": 1,
        "gold": 0,
        "rested_xp_pool": 0,
        "last_logout": "2026-04-08T12:00:00Z"
    },
    "skills": {
        "base_skills": {
            "unarmed": { "level": 0, "xp": 0 },
            "bladed": { "level": 0, "xp": 0 }
        },
        "specific_skills": {
            "punch": { "level": 0, "xp": 0 },
            "slash": { "level": 0, "xp": 0 }
        },
        "innate_skills": {
            "haste": { "level": 0, "xp": 0 },
            "sense": { "level": 0, "xp": 0 },
            "fortify": { "level": 0, "xp": 0 }
        }
    },
    "inventory": {
        "backpack": [],
        "bank": [],
        "equipment": {
            "head": null,
            "body": null,
            "neck": null,
            "rings": [null, null, null, null, null, null, null, null, null, null],
            "arms": null,
            "legs": null,
            "feet": null,
            "main_hand": null,
            "off_hand": null,
            "ammo": null
        }
    },
    "dungeon": {
        "floor_number": 1,
        "floor_cache": [],
        "boss_kills": []
    }
}
```

**Field descriptions:**

| Field | Type | Default | Purpose |
|-------|------|---------|---------|
| `version` | int | 2 | Save format version for migration |
| `slot` | int | 1-10 | Which save slot this belongs to |
| `character.name` | string | "" | Player-chosen character name |
| `character.class` | string | "" | "warrior", "ranger", or "mage" |
| `character.level` | int | 1 | Player level |
| `character.xp` | int | 0 | Current XP toward next level |
| `character.hp` | int | 100 | Current hit points |
| `character.max_hp` | int | 100 | Maximum hit points |
| `character.mana` | int | varies | Current mana (class base: W:60, R:100, M:200) |
| `character.max_mana` | int | varies | Maximum mana |
| `character.stats` | dict | base+class | STR, DEX, STA, INT raw values |
| `character.free_stat_points` | int | 0 | Unallocated stat points |
| `character.free_skill_points` | int | 0 | Unallocated skill points |
| `character.deepest_floor` | int | 1 | Highest floor reached (records + Level Teleporter) |
| `character.gold` | int | 0 | Currency |
| `character.rested_xp_pool` | int | 0 | Remaining rested XP bonus pool |
| `character.last_logout` | string | ISO-8601 | Timestamp for rested XP calculation |
| `skills.*` | dict | per-skill | Level and XP for every skill |
| `inventory.backpack` | array | [] | Items in backpack (at risk on death) |
| `inventory.bank` | array | [] | Items in bank (safe, town-only) |
| `inventory.equipment` | dict | all null | Currently equipped items per slot |
| `dungeon.floor_number` | int | 1 | Current dungeon floor |
| `dungeon.floor_cache` | array | [] | Up to 10 cached floor seeds + layouts |
| `dungeon.boss_kills` | array | [] | List of boss floor numbers killed (first-kill tracking) |

### Save API

**Save function:**
```csharp
public void SaveGame()
{
    var saveData = new Godot.Collections.Dictionary
    {
        ["version"] = 1,
        ["character"] = new Godot.Collections.Dictionary
        {
            ["level"] = GameState.Instance.Level,
            ["xp"] = GameState.Instance.Xp,
            ["hp"] = GameState.Instance.Hp,
            ["max_hp"] = GameState.Instance.MaxHp,
        },
        ["dungeon"] = new Godot.Collections.Dictionary
        {
            ["floor_number"] = GameState.Instance.FloorNumber,
        }
    };

    using var file = FileAccess.Open("user://save_data.json", FileAccess.ModeFlags.Write);
    if (file == null)
    {
        GD.PushError($"Failed to open save file for writing: {FileAccess.GetOpenError()}");
        return;
    }
    file.StoreString(Json.Stringify(saveData, "\t"));
}
```

**Load function:**
```csharp
public bool LoadGame()
{
    if (!FileAccess.FileExists("user://save_data.json"))
        return false;

    using var file = FileAccess.Open("user://save_data.json", FileAccess.ModeFlags.Read);
    if (file == null)
    {
        GD.PushError($"Failed to open save file for reading: {FileAccess.GetOpenError()}");
        return false;
    }

    var json = new Json();
    Error parseResult = json.Parse(file.GetAsText());
    if (parseResult != Error.Ok)
    {
        GD.PushError($"Failed to parse save JSON: {json.GetErrorMessage()} at line {json.GetErrorLine()}");
        return false;
    }

    var data = (Godot.Collections.Dictionary)json.Data;
    if (!ValidateSaveData(data))
    {
        GD.PushError("Save data validation failed");
        return false;
    }

    // Apply loaded data to GameState
    var character = (Godot.Collections.Dictionary)data["character"];
    var dungeon = (Godot.Collections.Dictionary)data["dungeon"];
    GameState.Instance.Level = (int)character["level"];
    GameState.Instance.Xp = (int)character["xp"];
    GameState.Instance.Hp = (int)character["hp"];
    GameState.Instance.MaxHp = (int)character["max_hp"];
    GameState.Instance.FloorNumber = (int)dungeon["floor_number"];

    return true;
}
```

**Validation function:**
```csharp
private bool ValidateSaveData(Godot.Collections.Dictionary data)
{
    // Check required top-level keys
    if (!data.ContainsKey("version"))
        return false;
    if (!data.ContainsKey("character"))
        return false;
    if (!data.ContainsKey("dungeon"))
        return false;

    // Check character fields
    var character = data.GetValueOrDefault("character", new Godot.Collections.Dictionary())
        as Godot.Collections.Dictionary ?? new();
    foreach (string key in new[] { "level", "xp", "hp", "max_hp" })
    {
        if (!character.ContainsKey(key))
            return false;
    }

    // Check dungeon fields
    var dungeon = data.GetValueOrDefault("dungeon", new Godot.Collections.Dictionary())
        as Godot.Collections.Dictionary ?? new();
    if (!dungeon.ContainsKey("floor_number"))
        return false;

    // Sanity checks on values
    if ((int)character["level"] < 1 || (int)character["level"] > 999)
        return false;
    if ((int)character["hp"] < 0 || (int)character["hp"] > 99999)
        return false;
    if ((int)character["xp"] < 0)
        return false;
    if ((int)dungeon["floor_number"] < 1)
        return false;

    return true;
}
```

### Auto-Save Triggers

The game saves automatically at these moments (unchanged from the original design):

| Trigger | When | Why |
|---------|------|-----|
| Level up | Immediately after XP threshold is crossed and stats are updated | Preserve level progress |
| Floor transition | After the new floor is loaded and the player is positioned | Preserve floor progress |
| Inventory change | After backpack/bank modifications complete | Preserve item acquisitions |
| Death and respawn | After the respawn process completes and HP is restored | Preserve respawn state |
| Town entry/exit | After the transition between town and dungeon completes | Preserve location state |

**Implementation pattern:**
```csharp
// In GameState autoload:
[Signal]
public delegate void LevelUpCompletedEventHandler();
[Signal]
public delegate void FloorChangedEventHandler();

// In SaveManager.cs:
public override void _Ready()
{
    GameState.Instance.LevelUpCompleted += OnAutoSaveTrigger;
    GameState.Instance.FloorChanged += OnAutoSaveTrigger;
    EventBus.Instance.PlayerRespawned += OnAutoSaveTrigger;
}

private void OnAutoSaveTrigger()
{
    SaveGame();
}
```

### Export / Import

Players can export their save as a Base64-encoded string for backup, sharing, or transferring between devices.

| Property | Phaser (Old) | Godot (New) |
|----------|-------------|-------------|
| Encode | `btoa(JSON.stringify(saveData))` | `Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonString))` |
| Decode | `JSON.parse(atob(base64String))` | `Encoding.UTF8.GetString(Convert.FromBase64String(base64String))` |
| Clipboard | `navigator.clipboard.writeText()` | `DisplayServer.ClipboardSet()` / `DisplayServer.ClipboardGet()` |

**Export function:**
```csharp
public string ExportSave()
{
    if (!FileAccess.FileExists("user://save_data.json"))
        return string.Empty;

    using var file = FileAccess.Open("user://save_data.json", FileAccess.ModeFlags.Read);
    if (file == null)
        return string.Empty;

    string jsonString = file.GetAsText();
    return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(jsonString));
}
```

**Import function:**
```csharp
public bool ImportSave(string base64String)
{
    // Decode Base64 to JSON string
    string jsonString;
    try
    {
        jsonString = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64String));
    }
    catch (FormatException)
    {
        GD.PushError("Failed to decode Base64 import string");
        return false;
    }

    if (string.IsNullOrEmpty(jsonString))
    {
        GD.PushError("Failed to decode Base64 import string");
        return false;
    }

    // Parse JSON
    var json = new Json();
    if (json.Parse(jsonString) != Error.Ok)
    {
        GD.PushError("Import data is not valid JSON");
        return false;
    }

    var data = (Godot.Collections.Dictionary)json.Data;

    // Validate
    if (!ValidateSaveData(data))
    {
        GD.PushError("Import data failed validation");
        return false;
    }

    // Write to save file (overwrites existing save)
    using var file = FileAccess.Open("user://save_data.json", FileAccess.ModeFlags.Write);
    if (file == null)
        return false;
    file.StoreString(jsonString);

    // Reload
    return LoadGame();
}
```

**Copy to clipboard:**
```csharp
public void CopySaveToClipboard()
{
    string base64 = ExportSave();
    if (!string.IsNullOrEmpty(base64))
        DisplayServer.ClipboardSet(base64);
}

public bool PasteSaveFromClipboard()
{
    string base64 = DisplayServer.ClipboardGet();
    if (string.IsNullOrEmpty(base64))
        return false;
    return ImportSave(base64);
}
```

### Save File Versioning

The `version` field in the save data enables forward migration:

```csharp
private Godot.Collections.Dictionary MigrateSave(Godot.Collections.Dictionary data)
{
    int version = data.ContainsKey("version") ? (int)data["version"] : 0;

    // Version 0 -> 1: Add max_hp field
    if (version < 1)
    {
        if (data.ContainsKey("character"))
        {
            var character = (Godot.Collections.Dictionary)data["character"];
            if (!character.ContainsKey("max_hp"))
            {
                int level = character.ContainsKey("level") ? (int)character["level"] : 1;
                character["max_hp"] = 100 + level * 8;
            }
        }
        data["version"] = 1;
    }

    // Future migrations:
    // if (version < 2)
    // {
    //     ... add new fields, restructure, etc.
    //     data["version"] = 2;
    // }

    return data;
}
```

Migration is applied during `load_game()` before validation, so old saves are upgraded transparently.

### Safety Rules

These rules protect against data loss:

| Rule | Implementation |
|------|---------------|
| Never overwrite without confirmation | Import shows a confirmation dialog before replacing existing save |
| Import warns about existing progress | If a save file already exists, display: "This will replace your current save (Level X, Floor Y). Continue?" |
| Reject corrupted data | `ValidateSaveData()` checks structure and value ranges; invalid data is rejected with an error message |
| Backup before import | Before import overwrites the save file, copy the existing file to `user://save_data_backup.json` |
| Clear error messages | Every failure path produces a specific error via `GD.PushError()` and returns false/empty to the caller |

**Backup before import:**
```csharp
private void BackupExistingSave()
{
    if (FileAccess.FileExists("user://save_data.json"))
    {
        using var src = FileAccess.Open("user://save_data.json", FileAccess.ModeFlags.Read);
        string content = src.GetAsText();
        using var dst = FileAccess.Open("user://save_data_backup.json", FileAccess.ModeFlags.Write);
        dst.StoreString(content);
    }
}
```

## Implementation Notes

- The save manager should be an Autoload singleton (`SaveManager`) so it is accessible from any scene.
- `FileAccess` in Godot 4 with C# implements `IDisposable` -- use `using` statements to ensure files are closed deterministically.
- `Json.Stringify(data, "\t")` produces human-readable JSON with tab indentation. This makes the save file debuggable by opening it in a text editor.
- `user://` is a virtual path that Godot resolves at runtime. Never hardcode the OS-specific path.
- On web exports (HTML5), `user://` maps to IndexedDB via Emscripten's virtual filesystem. The same `FileAccess` API works, but data persistence depends on browser storage policies.
- Base64 encoding uses `System.Convert.ToBase64String()` and `System.Convert.FromBase64String()` from the .NET standard library -- no Godot-specific utility needed.

## Resolved Questions

| Question | Decision |
|----------|----------|
| Save slots | 10 independent character slots. One character per slot. |
| Save data size | ~50-250KB per slot. 10 slots = 2.5MB max. Trivial for any platform. |
| Cloud save | Deferred. Not MVP scope. Local-only for now. |
| Save versioning | Automatic migration (pattern defined above). Never force-reset. |
| Auto-save feedback | Silent with brief icon flash (small save icon appears for ~0.5s). No text popup. |
| Backup rotation | Single backup per slot (`slot_N_backup.json`). Overwritten on each new backup. |
