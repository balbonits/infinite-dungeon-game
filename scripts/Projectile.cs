using Godot;
using DungeonGame.Ui;

namespace DungeonGame.Scenes;

/// <summary>
/// A projectile that flies toward a target position, damages the first enemy hit, and despawns.
/// Used by Ranger (arrows) and Mage (magic bolts).
/// </summary>
public partial class Projectile : Area2D
{
    private Vector2 _direction;
    private float _speed;
    private int _damage;
    private float _maxDistance;
    private float _traveled;
    private Sprite2D? _sprite;

    public static void Spawn(Node parent, Vector2 origin, Vector2 target, int damage,
        float speed, float maxRange, string texturePath, float scale, Color? tint = null)
    {
        var projectile = new Projectile();
        projectile.GlobalPosition = origin;
        projectile._damage = damage;
        projectile._speed = speed;
        projectile._maxDistance = maxRange;
        projectile._direction = (target - origin).Normalized();

        // Collision
        projectile.CollisionLayer = 0;
        projectile.CollisionMask = Constants.Layers.Enemies;
        projectile.Monitoring = true;

        var shape = new CollisionShape2D();
        var circle = new CircleShape2D();
        circle.Radius = 6.0f;
        shape.Shape = circle;
        projectile.AddChild(shape);

        // Sprite
        if (ResourceLoader.Exists(texturePath))
        {
            var sprite = new Sprite2D();
            sprite.Texture = GD.Load<Texture2D>(texturePath);
            sprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
            sprite.Scale = new Vector2(scale, scale);
            if (tint.HasValue)
                sprite.Modulate = tint.Value;
            projectile._sprite = sprite;
            projectile.AddChild(sprite);
        }

        // Rotate sprite to face direction
        projectile.Rotation = projectile._direction.Angle();

        projectile.Connect(SignalName.BodyEntered, new Callable(projectile, MethodName.OnBodyEntered));

        parent.AddChild(projectile);
    }

    public override void _PhysicsProcess(double delta)
    {
        float step = _speed * (float)delta;
        GlobalPosition += _direction * step;
        _traveled += step;

        if (_traveled >= _maxDistance)
            QueueFree();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup(Constants.Groups.Enemies))
            return;

        body.Call("TakeDamage", _damage);
        FloatingText.Damage(GetParent(), body.GlobalPosition, _damage);
        QueueFree();
    }
}
