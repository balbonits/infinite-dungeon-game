using Godot;

/// <summary>
/// Isometric player controller using CharacterBody2D.
/// Reads Input Map actions (move_up/down/left/right) and applies
/// the isometric transform from docs/ui/controls.md.
/// Placeholder cyan diamond visual until sprite assets are ready.
/// </summary>
public partial class PlayerController : CharacterBody2D
{
    [Export] public float MoveSpeed = 190f;

    // Isometric transform: screen-space input -> world-space direction.
    // screen-right (1,0) -> iso-southeast (1, 0.5)
    // screen-down  (0,1) -> iso-southwest (-1, 0.5)
    private static readonly Transform2D IsoTransform = new(
        new Vector2(1, 0.5f), new Vector2(-1, 0.5f), Vector2.Zero);

    private Polygon2D _sprite;

    public override void _Ready()
    {
        // Placeholder visual: cyan diamond sized to fit isometric tile (64x32)
        // ~60% of tile width = 38px wide, ~80% of tile height equivalent = 48px tall
        _sprite = new Polygon2D();
        _sprite.Polygon = new Vector2[] {
            new(0, -24), new(19, 0), new(0, 24), new(-19, 0)
        };
        _sprite.Color = new Color(0.557f, 0.839f, 1.0f); // #8ed6ff
        AddChild(_sprite);

        // Collision shape — radius ~1/3 tile width for smooth wall sliding
        var collision = new CollisionShape2D();
        var shape = new CircleShape2D();
        shape.Radius = 20f;
        collision.Shape = shape;
        AddChild(collision);
    }

    public override void _PhysicsProcess(double delta)
    {
        var rawInput = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        if (rawInput == Vector2.Zero)
        {
            Velocity = Vector2.Zero;
            return;
        }

        var worldDir = (IsoTransform * rawInput).Normalized();
        Velocity = worldDir * MoveSpeed;
        MoveAndSlide();
    }

    /// <summary>
    /// Convert current world position to tile coordinates given a TileMapLayer.
    /// Useful for collision checks, NPC proximity, floor lookups, etc.
    /// </summary>
    public Vector2I GetTilePosition(TileMapLayer tileMap)
    {
        return tileMap.LocalToMap(tileMap.ToLocal(GlobalPosition));
    }
}
