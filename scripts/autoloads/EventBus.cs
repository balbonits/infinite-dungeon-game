using Godot;

namespace DungeonGame.Autoloads;

public partial class EventBus : Node
{
    public static EventBus Instance { get; private set; } = null!;

    [Signal] public delegate void EnemyDefeatedEventHandler(Vector2 position, int tier);
    [Signal] public delegate void EnemySpawnedEventHandler(Node enemy);
    [Signal] public delegate void PlayerAttackedEventHandler(Node target);
    [Signal] public delegate void PlayerDamagedEventHandler(int amount, Node source);

    // COMBAT-01 §8 mitigation outcomes — wired so FloatingText/audio/camera-shake
    // can subscribe without crosslinking to GameState internals. Parameterless
    // by design: the player is the only subject of these signals, and GameState
    // (which runs TakeDamage) has no Node2D handle on the player. Subscribers
    // that want world position resolve it themselves from the Player group.
    [Signal] public delegate void PlayerDodgedEventHandler();
    [Signal] public delegate void PlayerBlockedEventHandler();
    [Signal] public delegate void PlayerPhasedEventHandler();

    public override void _Ready()
    {
        Instance = this;
    }
}
