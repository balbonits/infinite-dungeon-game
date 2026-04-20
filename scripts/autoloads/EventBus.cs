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
    // can subscribe without crosslinking to GameState internals.
    [Signal] public delegate void PlayerDodgedEventHandler(Vector2 position);
    [Signal] public delegate void PlayerBlockedEventHandler(Vector2 position);
    [Signal] public delegate void PlayerPhasedEventHandler(Vector2 position);

    public override void _Ready()
    {
        Instance = this;
    }
}
