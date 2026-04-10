# Save Systems

## Why This Matters
Our save system serializes GameState to JSON and restores it. But save bugs are insidious — they corrupt silently, lose data on edge cases, and break when the game updates. Understanding save system design prevents data loss and player frustration.

## Core Concepts

### What to Save

| Always Save | Never Save |
|------------|------------|
| Player stats (HP, MP, STR, etc.) | Node references (they change per session) |
| Inventory items (as data, not objects) | Cached/derived values (recalculate on load) |
| Equipment (by slot + item data) | Texture/resource references (reload from path) |
| Floor progress (current floor, deepest) | Timer states (reset on load) |
| Game settings (volume, controls) | UI state (menus, panels) |
| Location (town/dungeon) | Enemy positions (respawn on load) |
| Achievement progress | Animation state |

**Rule:** Save VALUES, not REFERENCES. Save `"MainHand": { "Name": "Iron Sword", "Damage": 8 }`, not `"MainHand": <ObjectReference>`.

### Serialization Formats

| Format | Size | Speed | Human-Readable | Use For |
|--------|------|-------|----------------|---------|
| JSON | Large | Slow | Yes | Save files (debug-friendly) |
| MessagePack | Small | Fast | No | Cache files (performance) |
| SQLite | Medium | Medium | No | Complex queries (leaderboards) |

We use JSON for save files (debug-friendly, human-readable, easy to inspect).

### Save File Structure
Our save format (from `SaveSerializer.cs`):

```json
{
  "version": 1,
  "location": 0,
  "dungeon_floor": 5,
  "character": {
    "name": "TestHero",
    "level": 12,
    "xp": 4500,
    "hp": 180, "max_hp": 200,
    "mp": 80, "max_mp": 100,
    "str": 15, "dex": 10, "int": 8, "vit": 12,
    "gold": 2500,
    "stat_points": 3,
    "skill_points": 2,
    "inventory_size": 30,
    "backpack_expansions": 1
  },
  "inventory": [...],
  "equipment": {...}
}
```

### Version Migration
When the game updates, save format may change. ALWAYS include a version field:

```csharp
if (version < 2)
{
    // v1 → v2: added elemental resistances (default to 0)
    characterDict["fire_res"] = 0;
    characterDict["water_res"] = 0;
}
if (version < 3)
{
    // v2 → v3: renamed "stamina" to "vit"
    characterDict["vit"] = characterDict["sta"];
    characterDict.Remove("sta");
}
```

### Auto-Save Triggers
Save automatically at safe moments:
- Entering town (always safe)
- Floor transition (between floors)
- Before boss fights (player might die)
- On manual save (pause menu)
- On game exit (if possible)

NEVER auto-save during combat (could save a dead state).

### Godot File Paths
Godot uses `user://` for persistent storage:
- macOS: `~/Library/Application Support/Godot/app_userdata/ProjectName/`
- Windows: `%APPDATA%/Godot/app_userdata/ProjectName/`
- Linux: `~/.local/share/godot/app_userdata/ProjectName/`

```csharp
// Save
using var file = FileAccess.Open("user://saves/slot_1.json", FileAccess.ModeFlags.Write);
file.StoreString(jsonString);

// Load
using var file = FileAccess.Open("user://saves/slot_1.json", FileAccess.ModeFlags.Read);
string json = file.GetAsText();
```

## Godot 4 + C# Implementation

```csharp
// Our save round-trip pattern
public static bool SaveToSlot(int slot)
{
    var data = SaveSerializer.Serialize(slot);
    string json = Json.Stringify(data, "  ");
    
    string dir = "user://saves/";
    if (!DirAccess.DirExistsAbsolute(dir))
        DirAccess.MakeDirRecursiveAbsolute(dir);
    
    using var file = FileAccess.Open($"{dir}slot_{slot}.json", FileAccess.ModeFlags.Write);
    if (file == null) return false;
    file.StoreString(json);
    return true;
}

public static bool LoadFromSlot(int slot)
{
    string path = $"user://saves/slot_{slot}.json";
    if (!FileAccess.FileExists(path)) return false;
    
    using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
    string json = file.GetAsText();
    var data = Json.ParseString(json).AsGodotDictionary();
    return SaveSerializer.Deserialize(data);
}
```

## Common Mistakes
1. **Saving object references** — references are invalid after reload (save data values instead)
2. **No version field** — can't migrate old saves when format changes
3. **Auto-saving during combat** — might save a dead/corrupted state
4. **Not validating on load** — corrupted JSON crashes the game instead of showing "save corrupted"
5. **Saving derived values** — MaxHP changes with level; save the formula inputs, not the output
6. **No backup** — write to temp file first, then rename (prevents corruption on crash during write)
7. **Saving too often** — every frame save = performance hit; save at specific triggers only

## Checklist
- [ ] Save file has a `version` field
- [ ] Load validates data before applying (missing fields get defaults)
- [ ] Auto-save triggers at safe moments only (town, floor transition)
- [ ] Manual save from pause menu works
- [ ] Save/load round-trip tested (save → change state → load → verify)
- [ ] Old version saves can be migrated
- [ ] Save files are in `user://` directory

## Sources
- [Godot Saving Games](https://docs.godotengine.org/en/stable/tutorials/io/saving_games.html)
- [Godot FileAccess](https://docs.godotengine.org/en/stable/classes/class_fileaccess.html)
- [Game Programming Patterns: Serialization](https://gameprogrammingpatterns.com/)
- [GDC: Save Systems in Games](https://www.gamedeveloper.com/design/saving-and-loading-games-in-unity)
