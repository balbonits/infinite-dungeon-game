using Godot;

/// <summary>
/// Isometric player controller using CharacterBody2D.
/// Uses FLARE hero sprite sheet for visuals (2048x1024, 32x8 grid, 64x128 frames).
/// Reads Input Map actions and applies isometric transform.
/// See: docs/basics/sprites-and-animation.md, docs/basics/collision-and-physics.md
/// </summary>
public partial class PlayerController : CharacterBody2D
{
    [Export] public float MoveSpeed = 190f;

    // Isometric transform: screen-space input → world-space direction
    private static readonly Transform2D IsoTransform = new(
        new Vector2(1, 0.5f), new Vector2(-1, 0.5f), Vector2.Zero);

    // Hero sprite: 2048x1024 sheet, 32 cols x 8 rows, 64x128 per frame
    private const int Hframes = 32;
    private const int Vframes = 8;
    private const float SpriteScale = 0.55f; // 64px * 0.55 = ~35px wide, fits tile

    // Animation frames (per row = direction)
    private static readonly int[] WalkFrames = { 4, 5, 6, 7, 8, 9, 10, 11 };
    private static readonly int[] IdleFrames = { 0 };

    private Sprite2D _sprite;
    private int _direction = 0; // 0=S, 1=SW, 2=W, 3=NW, 4=N, 5=NE, 6=E, 7=SE
    private int _animFrame;
    private float _animTimer;
    private bool _isMoving;

    public override void _Ready()
    {
        // Load hero sprite sheet
        string heroPath = "res://assets/isometric/characters/hero/male_base.png";
        Texture2D tex = null;

        if (ResourceLoader.Exists(heroPath))
            tex = ResourceLoader.Load<Texture2D>(heroPath);

        if (tex == null)
            tex = TestHelper.LoadIssPng(heroPath);

        _sprite = new Sprite2D();
        _sprite.TextureFilter = TextureFilterEnum.Nearest;

        if (tex != null)
        {
            _sprite.Texture = tex;
            _sprite.Hframes = Hframes;
            _sprite.Vframes = Vframes;
            _sprite.Scale = new Vector2(SpriteScale, SpriteScale);
            // Offset so feet land on tile center (bottom 40% of frame)
            float frameH = tex.GetHeight() / Vframes;
            _sprite.Offset = new Vector2(0, -frameH * 0.3f);
        }
        else
        {
            // Fallback: cyan diamond if sprite not found
            GD.PrintErr("[PLAYER] Could not load hero sprite, using placeholder");
            var placeholder = new Polygon2D();
            placeholder.Polygon = new Vector2[] {
                new(0, -24), new(19, 0), new(0, 24), new(-19, 0)
            };
            placeholder.Color = new Color(0.557f, 0.839f, 1.0f);
            AddChild(placeholder);
        }

        AddChild(_sprite);

        // Collision shape — circle matching character visual width
        var collision = new CollisionShape2D();
        var shape = new CircleShape2D();
        shape.Radius = 16f;
        collision.Shape = shape;
        AddChild(collision);
    }

    public override void _PhysicsProcess(double delta)
    {
        var rawInput = Input.GetVector("move_left", "move_right", "move_up", "move_down");

        if (rawInput == Vector2.Zero)
        {
            Velocity = Vector2.Zero;
            _isMoving = false;
            UpdateAnimation(delta);
            return;
        }

        _isMoving = true;

        // Determine direction from input (8 directions)
        _direction = GetDirectionFromInput(rawInput);

        var worldDir = (IsoTransform * rawInput).Normalized();
        Velocity = worldDir * MoveSpeed;
        MoveAndSlide();

        UpdateAnimation(delta);
    }

    private void UpdateAnimation(double delta)
    {
        if (_sprite?.Texture == null) return;

        _animTimer += (float)delta;
        if (_animTimer < 0.1f) return; // 10 FPS animation
        _animTimer = 0;

        int[] frames = _isMoving ? WalkFrames : IdleFrames;
        _animFrame = (_animFrame + 1) % frames.Length;

        int col = frames[_animFrame];
        int frame = _direction * Hframes + col;
        if (frame < Hframes * Vframes)
            _sprite.Frame = frame;
    }

    /// <summary>
    /// Map raw input vector to one of 8 isometric directions.
    /// Input is screen-space (up=negative Y, right=positive X).
    /// </summary>
    private static int GetDirectionFromInput(Vector2 input)
    {
        // Angle from input, mapped to 8 directions
        float angle = Mathf.Atan2(input.Y, input.X);
        // Normalize to 0-8 range
        int dir = Mathf.RoundToInt((angle / Mathf.Tau + 1.0f) * 8) % 8;
        // Map screen directions to FLARE sprite row order:
        // FLARE: 0=S, 1=SW, 2=W, 3=NW, 4=N, 5=NE, 6=E, 7=SE
        // Screen angle 0=right, pi/2=down, pi=left, -pi/2=up
        return dir switch
        {
            0 => 6,  // right → E
            1 => 7,  // down-right → SE
            2 => 0,  // down → S
            3 => 1,  // down-left → SW
            4 => 2,  // left → W
            5 => 3,  // up-left → NW
            6 => 4,  // up → N
            7 => 5,  // up-right → NE
            _ => 0,
        };
    }

    public Vector2I GetTilePosition(TileMapLayer tileMap)
    {
        return tileMap.LocalToMap(tileMap.ToLocal(GlobalPosition));
    }
}
