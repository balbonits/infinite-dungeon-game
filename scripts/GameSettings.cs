namespace DungeonGame;

/// <summary>
/// Static game settings. Persisted to disk in the future.
/// Togglable via settings menu (when built).
/// </summary>
public static class GameSettings
{
    public static bool ShowCombatNumbers { get; set; } = true;
}
