using System.Collections.Generic;

namespace DungeonGame;

/// <summary>
/// Abstraction over save-file I/O. Production uses <see cref="GodotFileSaveStorage"/>
/// backed by Godot's FileAccess; tests use <see cref="FakeSaveStorage"/> backed by
/// an in-memory dictionary. Enables pure-logic tests of the save/load pipeline.
///
/// Keys are opaque strings — typically <c>user://saves/save_{slot}.json</c> paths,
/// but the abstraction does not require any particular format.
/// </summary>
public interface ISaveStorage
{
    bool Exists(string key);
    string? Read(string key);

    /// <summary>Persist <paramref name="content"/> under <paramref name="key"/>. Returns false on I/O failure.</summary>
    bool Write(string key, string content);

    void Delete(string key);
}

/// <summary>
/// In-memory <see cref="ISaveStorage"/> for tests. Thread-unsafe; single-threaded use only.
/// </summary>
public class FakeSaveStorage : ISaveStorage
{
    private readonly Dictionary<string, string> _store = new();

    public bool Exists(string key) => _store.ContainsKey(key);
    public string? Read(string key) => _store.TryGetValue(key, out var v) ? v : null;
    public bool Write(string key, string content) { _store[key] = content; return true; }
    public void Delete(string key) => _store.Remove(key);

    public int Count => _store.Count;
}
