using Godot;

namespace DungeonGame.Scenes;

/// <summary>
/// Visual-only projectile tracer. The actual hit is calculated instantly by the
/// combat system (hitscan) — this node just flies to the target position for
/// visual feedback, then despawns. No collision detection needed.
///
/// This is the industry standard for ARPG ranged attacks: instant hit calculation,
/// cosmetic projectile animation. Eliminates all tunneling/offset/timing issues.
/// </summary>
public partial class Projectile : Node2D
{
    private Vector2 _targetPos;
    private float _speed;
    private Sprite2D? _sprite;

    /// <summary>
    /// Spawn a visual tracer that flies from origin to target then disappears.
    /// The DAMAGE is already dealt before this is called — this is purely visual.
    /// </summary>
    public static void SpawnTracer(Node parent, Vector2 origin, Vector2 target,
        float speed, string texturePath, float scale, Color? tint = null)
    {
        var tracer = new Projectile();
        tracer.GlobalPosition = origin;
        tracer._targetPos = target;
        tracer._speed = speed;

        if (ResourceLoader.Exists(texturePath))
        {
            var sprite = new Sprite2D();
            sprite.Texture = GD.Load<Texture2D>(texturePath);
            sprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
            sprite.Scale = new Vector2(scale, scale);
            if (tint.HasValue)
                sprite.Modulate = tint.Value;
            tracer._sprite = sprite;
            tracer.AddChild(sprite);
        }

        // Point sprite toward target
        Vector2 direction = (target - origin).Normalized();
        tracer.Rotation = direction.Angle();

        parent.AddChild(tracer);
    }

    public override void _PhysicsProcess(double delta)
    {
        float step = _speed * (float)delta;
        Vector2 toTarget = _targetPos - GlobalPosition;
        float remaining = toTarget.Length();

        if (remaining <= step)
        {
            // Arrived at target — despawn
            GlobalPosition = _targetPos;
            QueueFree();
            return;
        }

        // Move toward target
        GlobalPosition += toTarget.Normalized() * step;
    }
}
