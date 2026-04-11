using Godot;

namespace DungeonGame.Autoloads;

public partial class EventBus : Node
{
    public static EventBus Instance { get; private set; } = null!;

    [Signal] public delegate void EnemyDefeatedEventHandler(Vector2 position, int tier);
    [Signal] public delegate void EnemySpawnedEventHandler(Node enemy);
    [Signal] public delegate void PlayerAttackedEventHandler(Node target);
    [Signal] public delegate void PlayerDamagedEventHandler(int amount, Node source);

    public override void _Ready()
    {
        Instance = this;
    }
}
