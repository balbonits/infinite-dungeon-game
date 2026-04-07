# Save System

## Summary

Game state is saved to a local JSON file automatically at key moments. Players can export/import saves as Base64 strings for backup and sharing.

## Current State

Save/export functionality is referenced in the project design but not yet implemented in the Godot prototype. The design below covers the full specification, ported from the Phaser prototype's `localStorage` approach to Godot's `FileAccess` API.

## Design

### Storage Mechanism

| Property | Phaser (Old) | Godot (New) |
|----------|-------------|-------------|
| API | `localStorage.setItem()` / `localStorage.getItem()` | `FileAccess.Open()` / `file.StoreString()` |
| Key / Path | `dungeonGame_save` (localStorage key) | `user://save_data.json` (file path) |
| Format | JSON string | JSON string (pretty-printed with tabs) |
| Location | Browser storage (per-origin) | OS-specific app data directory |

**`user://` resolves to:**

| OS | Path |
|----|------|
| macOS | `~/Library/Application Support/Godot/app_userdata/A Dungeon in the Middle of Nowhere/` |
| Windows | `%APPDATA%\Godot\app_userdata\A Dungeon in the Middle of Nowhere\` |
| Linux | `~/.local/share/godot/app_userdata/A Dungeon in the Middle of Nowhere/` |

The exact directory name is derived from the `application/config/name` value in `project.godot`. Godot creates this directory automatically on first write.

### Save Data Structure

The save captures all persistent state. The structure is a dictionary serialized as JSON:

```json
{
    "version": 1,
    "character": {
        "level": 1,
        "xp": 0,
        "hp": 100,
        "max_hp": 100
    },
    "dungeon": {
        "floor_number": 1
    }
}
```

**Field descriptions:**

| Field | Type | Default | Purpose |
|-------|------|---------|---------|
| `version` | int | 1 | Save format version for future migration |
| `character.level` | int | 1 | Player level |
| `character.xp` | int | 0 | Current XP toward next level |
| `character.hp` | int | 100 | Current hit points |
| `character.max_hp` | int | 100 | Maximum hit points (100 + level * 8) |
| `dungeon.floor_number` | int | 1 | Current dungeon floor |

**Future fields (planned, not yet in schema):**

| Field | Type | Purpose |
|-------|------|---------|
| `character.class` | String | Character class ("warrior", "rogue", "mage") |
| `character.stats` | Dictionary | STR, DEX, INT, VIT, LCK values |
| `character.gold` | int | Currency |
| `character.deepest_floor` | int | Highest floor reached (for records) |
| `inventory.backpack` | Array | Backpack item list |
| `inventory.bank` | Array | Bank storage item list |
| `inventory.special` | Array | Special items (Sacrificial Idol, etc.) |
| `dungeon.floor_cache` | Array | Up to 10 cached floor seeds + layouts |

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

## Open Questions

- Should there be multiple save slots, or is one permanent character the rule?
- How large can the save data get with 10 cached floors? Is `user://` file storage sufficient for large saves?
- Should cloud save be a future goal (e.g., Steam Cloud, custom backend), or is local-only the design intent?
- How should save versioning work as the game evolves -- automatic migration (as shown) or force-reset on breaking changes?
- Should auto-save show a brief UI indicator ("Game saved") or be completely silent?
- Should the backup file (`save_data_backup.json`) be rotated (keep last N backups) or just one?
