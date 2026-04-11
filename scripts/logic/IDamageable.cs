namespace DungeonGame;

/// <summary>
/// Shared interface for anything that can take damage (Player, Enemy, destructibles).
/// Replaces unsafe .Call("TakeDamage") string dispatch with type-safe calls.
/// </summary>
public interface IDamageable
{
    void TakeDamage(int amount);
}
