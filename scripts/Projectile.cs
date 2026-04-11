using Godot;
using DungeonGame.Ui;

namespace DungeonGame.Scenes;

/// <summary>
/// Projectile that flies toward a target, damages the first enemy hit, and despawns.
/// Uses manual overlap checking each frame to prevent tunneling through enemies.
/// </summary>
public partial class Projectile : Area2D
{
    private Vector2 _direction;
    private float _speed;
    private int _damage;
    private float _maxDistance;
    private float _traveled;
    private bool _hit;
    private bool _pierces;
    private float _collisionRadius;
    private Sprite2D? _sprite;

    public static void Spawn(Node parent, Vector2 origin, Vector2 target, int damage,
        float speed, float maxRange, string texturePath, float scale, Color? tint = null,
        bool pierces = false)
    {
        var projectile = new Projectile();
        projectile.GlobalPosition = origin;
        projectile._damage = damage;
        projectile._speed = speed;
        projectile._maxDistance = maxRange;
        projectile._direction = (target - origin).Normalized();
        projectile._pierces = pierces;
        projectile._collisionRadius = 10.0f; // Generous hitbox for reliable collision

        // Area2D config — acts as a sensor
        projectile.CollisionLayer = 0;
        projectile.CollisionMask = Constants.Layers.Enemies;
        projectile.Monitoring = true;
        projectile.Monitorable = false;

        var shape = new CollisionShape2D();
        var circle = new CircleShape2D();
        circle.Radius = projectile._collisionRadius;
        shape.Shape = circle;
        projectile.AddChild(shape);

        // Sprite — no offset (rotation would rotate the offset too)
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

        projectile.Rotation = projectile._direction.Angle();

        parent.AddChild(projectile);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_hit && !_pierces)
            return;

        float step = _speed * (float)delta;
        GlobalPosition += _direction * step;
        _traveled += step;

        // Manual overlap check every frame — prevents tunneling
        CheckHits();

        if (_traveled >= _maxDistance)
            QueueFree();
    }

    private void CheckHits()
    {
        var bodies = GetOverlappingBodies();
        foreach (Node2D body in bodies)
        {
            if (_hit && !_pierces)
                return;
            if (!body.IsInGroup(Constants.Groups.Enemies))
                continue;

            body.Call("TakeDamage", _damage);
            FloatingText.Damage(GetParent(), body.GlobalPosition, _damage);

            if (!_pierces)
            {
                _hit = true;
                Monitoring = false;
                QueueFree();
                return;
            }
        }
    }
}
