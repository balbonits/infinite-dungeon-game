using Godot;

public partial class AssetTest : Node2D
{
    private const int TileSize = 32;
    private const int RoomWidth = 15;
    private const int RoomHeight = 11;
    private const float MoveSpeed = 96f; // pixels per second (3 tiles/sec)
    private const float StepDistance = 64f; // 2 tiles per move

    private Node2D _character;
    private Vector2 _targetPos;
    private bool _isMoving;
    private int _demoStep;
    private float _waitTimer;

    // Scripted demo: direction + label for each step
    private static readonly (Vector2 dir, string label)[] DemoMoves = {
        (Vector2.Up, "Moving UP"),
        (Vector2.Down, "Moving DOWN"),
        (Vector2.Left, "Moving LEFT"),
        (Vector2.Right, "Moving RIGHT"),
        (Vector2.Zero, "Demo complete — auto-quit in 2s"),
    };

    public override void _Ready()
    {
        GD.Print("Input demo — scripted movement test");

        DrawFloor();
        DrawWalls();
        _character = CreateCharacter();
        _targetPos = _character.Position;
        _waitTimer = 0.5f; // brief pause before starting
        _demoStep = 0;
        _isMoving = false;

        if (DisplayServer.GetName() == "headless")
            GetTree().Quit();
    }

    public override void _Process(double delta)
    {
        if (_demoStep >= DemoMoves.Length)
            return;

        // Wait between moves
        if (!_isMoving)
        {
            _waitTimer -= (float)delta;
            if (_waitTimer > 0)
                return;

            // Start next move
            var (dir, label) = DemoMoves[_demoStep];
            GD.Print($"[Step {_demoStep + 1}/{DemoMoves.Length}] {label}");

            if (dir == Vector2.Zero)
            {
                // Final step — wait then quit
                _demoStep++;
                GetTree().CreateTimer(2.0).Timeout += () => GetTree().Quit();
                return;
            }

            _targetPos = _character.Position + dir * StepDistance;
            _isMoving = true;
            return;
        }

        // Move toward target
        var moveAmount = MoveSpeed * (float)delta;
        var remaining = _character.Position.DistanceTo(_targetPos);

        if (remaining <= moveAmount)
        {
            _character.Position = _targetPos;
            _isMoving = false;
            _demoStep++;
            _waitTimer = 1.0f; // 1 second pause between moves
        }
        else
        {
            var direction = (_targetPos - _character.Position).Normalized();
            _character.Position += direction * moveAmount;
        }
    }

    private Node2D CreateCharacter()
    {
        var baseTexture = GD.Load<Texture2D>(
            "res://assets/tilesets/dungeon-crawl/dcss-full/Dungeon Crawl Stone Soup Full/player/base/human_male.png");
        var armorTexture = GD.Load<Texture2D>(
            "res://assets/tilesets/dungeon-crawl/dcss-full/Dungeon Crawl Stone Soup Full/player/body/chainmail.png");
        var weaponTexture = GD.Load<Texture2D>(
            "res://assets/tilesets/dungeon-crawl/dcss-full/Dungeon Crawl Stone Soup Full/player/hand_right/long_sword.png");

        var center = new Vector2(RoomWidth / 2 * TileSize + TileSize / 2, RoomHeight / 2 * TileSize + TileSize / 2);

        // Container node so all layers move together
        var container = new Node2D();
        container.Position = center;
        container.ZIndex = 10;
        AddChild(container);

        var body = new Sprite2D { Texture = baseTexture, TextureFilter = TextureFilterEnum.Nearest, ZIndex = 1 };
        var armor = new Sprite2D { Texture = armorTexture, TextureFilter = TextureFilterEnum.Nearest, ZIndex = 2 };
        var weapon = new Sprite2D { Texture = weaponTexture, TextureFilter = TextureFilterEnum.Nearest, ZIndex = 3 };

        container.AddChild(body);
        container.AddChild(armor);
        container.AddChild(weapon);

        return container;
    }

    private void DrawFloor()
    {
        var floorTexture = GD.Load<Texture2D>(
            "res://assets/tilesets/dungeon-crawl/dcss-full/Dungeon Crawl Stone Soup Full/dungeon/floor/grey_dirt_0_new.png");

        for (int x = 1; x < RoomWidth - 1; x++)
        {
            for (int y = 1; y < RoomHeight - 1; y++)
            {
                var sprite = new Sprite2D();
                sprite.Texture = floorTexture;
                sprite.Position = new Vector2(x * TileSize + TileSize / 2, y * TileSize + TileSize / 2);
                sprite.TextureFilter = TextureFilterEnum.Nearest;
                AddChild(sprite);
            }
        }
    }

    private void DrawWalls()
    {
        var wallTexture = GD.Load<Texture2D>(
            "res://assets/tilesets/dungeon-crawl/dcss-full/Dungeon Crawl Stone Soup Full/dungeon/wall/brick_dark_0.png");

        for (int x = 0; x < RoomWidth; x++)
        {
            for (int y = 0; y < RoomHeight; y++)
            {
                if (x == 0 || x == RoomWidth - 1 || y == 0 || y == RoomHeight - 1)
                {
                    var sprite = new Sprite2D();
                    sprite.Texture = wallTexture;
                    sprite.Position = new Vector2(x * TileSize + TileSize / 2, y * TileSize + TileSize / 2);
                    sprite.TextureFilter = TextureFilterEnum.Nearest;
                    AddChild(sprite);
                }
            }
        }
    }
}
