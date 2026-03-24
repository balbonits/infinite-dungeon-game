# Save System

## Summary

Game state is saved to a local JSON file automatically at key moments. Players can export/import saves as Base64 strings for backup and sharing.

## Current State

Save/export functionality is referenced in the project design but not yet implemented in the Godot prototype. The design below covers the full specification, ported from the Phaser prototype's `localStorage` approach to Godot's `FileAccess` API.

## Design

### Storage Mechanism

| Property | Phaser (Old) | Godot (New) |
|----------|-------------|-------------|
| API | `localStorage.setItem()` / `localStorage.getItem()` | `FileAccess.open()` / `file.store_string()` |
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
```gdscript
func save_game() -> void:
    var save_data := {
        "version": 1,
        "character": {
            "level": GameState.level,
            "xp": GameState.xp,
            "hp": GameState.hp,
            "max_hp": GameState.max_hp,
        },
        "dungeon": {
            "floor_number": GameState.floor_number,
        }
    }

    var file := FileAccess.open("user://save_data.json", FileAccess.WRITE)
    if file == null:
        push_error("Failed to open save file for writing: %s" % FileAccess.get_open_error())
        return
    file.store_string(JSON.stringify(save_data, "\t"))
    # File is automatically closed when the variable goes out of scope
```

**Load function:**
```gdscript
func load_game() -> bool:
    if not FileAccess.file_exists("user://save_data.json"):
        return false

    var file := FileAccess.open("user://save_data.json", FileAccess.READ)
    if file == null:
        push_error("Failed to open save file for reading: %s" % FileAccess.get_open_error())
        return false

    var json := JSON.new()
    var parse_result := json.parse(file.get_as_text())
    if parse_result != OK:
        push_error("Failed to parse save JSON: %s at line %d" % [json.get_error_message(), json.get_error_line()])
        return false

    var data: Dictionary = json.data
    if not _validate_save_data(data):
        push_error("Save data validation failed")
        return false

    # Apply loaded data to GameState
    GameState.level = data["character"]["level"]
    GameState.xp = data["character"]["xp"]
    GameState.hp = data["character"]["hp"]
    GameState.max_hp = data["character"]["max_hp"]
    GameState.floor_number = data["dungeon"]["floor_number"]

    return true
```

**Validation function:**
```gdscript
func _validate_save_data(data: Dictionary) -> bool:
    # Check required top-level keys
    if not data.has("version"):
        return false
    if not data.has("character"):
        return false
    if not data.has("dungeon"):
        return false

    # Check character fields
    var character: Dictionary = data.get("character", {})
    for key in ["level", "xp", "hp", "max_hp"]:
        if not character.has(key):
            return false

    # Check dungeon fields
    var dungeon: Dictionary = data.get("dungeon", {})
    if not dungeon.has("floor_number"):
        return false

    # Sanity checks on values
    if character["level"] < 1 or character["level"] > 999:
        return false
    if character["hp"] < 0 or character["hp"] > 99999:
        return false
    if character["xp"] < 0:
        return false
    if dungeon["floor_number"] < 1:
        return false

    return true
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
```gdscript
# In GameState autoload:
signal level_up_completed
signal floor_changed

# In save_manager.gd:
func _ready() -> void:
    GameState.level_up_completed.connect(_on_auto_save_trigger)
    GameState.floor_changed.connect(_on_auto_save_trigger)
    EventBus.player_respawned.connect(_on_auto_save_trigger)

func _on_auto_save_trigger() -> void:
    save_game()
```

### Export / Import

Players can export their save as a Base64-encoded string for backup, sharing, or transferring between devices.

| Property | Phaser (Old) | Godot (New) |
|----------|-------------|-------------|
| Encode | `btoa(JSON.stringify(saveData))` | `Marshalls.utf8_to_base64(JSON.stringify(save_data))` |
| Decode | `JSON.parse(atob(base64String))` | `JSON.parse(Marshalls.base64_to_utf8(base64_string))` |
| Clipboard | `navigator.clipboard.writeText()` | `DisplayServer.clipboard_set()` / `DisplayServer.clipboard_get()` |

**Export function:**
```gdscript
func export_save() -> String:
    if not FileAccess.file_exists("user://save_data.json"):
        return ""

    var file := FileAccess.open("user://save_data.json", FileAccess.READ)
    if file == null:
        return ""

    var json_string := file.get_as_text()
    var base64_string := Marshalls.utf8_to_base64(json_string)
    return base64_string
```

**Import function:**
```gdscript
func import_save(base64_string: String) -> bool:
    # Decode Base64 to JSON string
    var json_string := Marshalls.base64_to_utf8(base64_string)
    if json_string.is_empty():
        push_error("Failed to decode Base64 import string")
        return false

    # Parse JSON
    var json := JSON.new()
    if json.parse(json_string) != OK:
        push_error("Import data is not valid JSON")
        return false

    var data: Dictionary = json.data

    # Validate
    if not _validate_save_data(data):
        push_error("Import data failed validation")
        return false

    # Write to save file (overwrites existing save)
    var file := FileAccess.open("user://save_data.json", FileAccess.WRITE)
    if file == null:
        return false
    file.store_string(json_string)

    # Reload
    return load_game()
```

**Copy to clipboard:**
```gdscript
func copy_save_to_clipboard() -> void:
    var base64 := export_save()
    if not base64.is_empty():
        DisplayServer.clipboard_set(base64)

func paste_save_from_clipboard() -> bool:
    var base64 := DisplayServer.clipboard_get()
    if base64.is_empty():
        return false
    return import_save(base64)
```

### Save File Versioning

The `version` field in the save data enables forward migration:

```gdscript
func _migrate_save(data: Dictionary) -> Dictionary:
    var version: int = data.get("version", 0)

    # Version 0 -> 1: Add max_hp field
    if version < 1:
        if data.has("character"):
            var character: Dictionary = data["character"]
            if not character.has("max_hp"):
                character["max_hp"] = 100 + character.get("level", 1) * 8
        data["version"] = 1

    # Future migrations:
    # if version < 2:
    #     ... add new fields, restructure, etc.
    #     data["version"] = 2

    return data
```

Migration is applied during `load_game()` before validation, so old saves are upgraded transparently.

### Safety Rules

These rules protect against data loss:

| Rule | Implementation |
|------|---------------|
| Never overwrite without confirmation | Import shows a confirmation dialog before replacing existing save |
| Import warns about existing progress | If a save file already exists, display: "This will replace your current save (Level X, Floor Y). Continue?" |
| Reject corrupted data | `_validate_save_data()` checks structure and value ranges; invalid data is rejected with an error message |
| Backup before import | Before import overwrites the save file, copy the existing file to `user://save_data_backup.json` |
| Clear error messages | Every failure path produces a specific error via `push_error()` and returns false/empty to the caller |

**Backup before import:**
```gdscript
func _backup_existing_save() -> void:
    if FileAccess.file_exists("user://save_data.json"):
        var src := FileAccess.open("user://save_data.json", FileAccess.READ)
        var content := src.get_as_text()
        var dst := FileAccess.open("user://save_data_backup.json", FileAccess.WRITE)
        dst.store_string(content)
```

## Implementation Notes

- The save manager should be an Autoload singleton (`SaveManager`) so it is accessible from any scene.
- `FileAccess` in Godot 4 uses reference counting -- files are closed automatically when the `FileAccess` variable goes out of scope. No explicit `close()` call is needed.
- `JSON.stringify(data, "\t")` produces human-readable JSON with tab indentation. This makes the save file debuggable by opening it in a text editor.
- `user://` is a virtual path that Godot resolves at runtime. Never hardcode the OS-specific path.
- On web exports (HTML5), `user://` maps to IndexedDB via Emscripten's virtual filesystem. The same `FileAccess` API works, but data persistence depends on browser storage policies.
- The `Marshalls` class is a built-in Godot utility -- no imports or plugins needed.

## Open Questions

- Should there be multiple save slots, or is one permanent character the rule?
- How large can the save data get with 10 cached floors? Is `user://` file storage sufficient for large saves?
- Should cloud save be a future goal (e.g., Steam Cloud, custom backend), or is local-only the design intent?
- How should save versioning work as the game evolves -- automatic migration (as shown) or force-reset on breaking changes?
- Should auto-save show a brief UI indicator ("Game saved") or be completely silent?
- Should the backup file (`save_data_backup.json`) be rotated (keep last N backups) or just one?
