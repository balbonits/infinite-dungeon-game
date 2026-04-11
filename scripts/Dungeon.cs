using Godot;
using DungeonGame.Autoloads;
using DungeonGame.Ui;

namespace DungeonGame.Scenes;

public partial class Dungeon : Node2D
{
    private static readonly PackedScene PlayerScene = GD.Load<PackedScene>(Constants.Assets.PlayerScene);
    private static readonly PackedScene EnemyScene = GD.Load<PackedScene>(Constants.Assets.EnemyScene);

    private TileMapLayer _tileMap = null!;
    private Node2D _entities = null!;
    private Timer _spawnTimer = null!;
    private Node2D _player = null!;
    private Node2D _stairsDownNode = null!;
    private Node2D _stairsUpNode = null!;

    private int _roomWidth;
    private int _roomHeight;
    private Vector2I _stairsDownPosition;
    private Vector2I _stairsUpPosition;
    private int _floorSourceBaseId;
    private int _wallSourceId;
    private int _floorSourceCount;

    public override void _Ready()
    {
        _tileMap = GetNode<TileMapLayer>("TileMapLayer");
        _entities = GetNode<Node2D>("Entities");
        _spawnTimer = GetNode<Timer>("SpawnTimer");

        SetupTileset();
        GenerateFloor();

        // Spawn player at the up-stairs position (where you "arrived" from)
        _player = PlayerScene.Instantiate<CharacterBody2D>();
        _player.GlobalPosition = _tileMap.MapToLocal(_stairsUpPosition);
        _entities.AddChild(_player);

        // Spawn initial enemies
        for (int i = 0; i < Constants.Spawning.InitialEnemies; i++)
            SpawnEnemy();

        // Create stairs objects with collision
        _stairsDownNode = CreateStairsObject(Strings.Floor.StairsDown, UiTheme.Colors.Accent,
            Constants.Assets.StairsDownTexture, true);
        _stairsDownNode.GlobalPosition = _tileMap.MapToLocal(_stairsDownPosition);
        _entities.AddChild(_stairsDownNode);

        // Floor 1: "Back to Town", deeper floors: "Stairs Up"
        string upLabel = GameState.Instance.FloorNumber == 1
            ? Strings.Ascend.ReturnToTown
            : Strings.Floor.StairsUp;
        _stairsUpNode = CreateStairsObject(upLabel, UiTheme.Colors.Muted,
            Constants.Assets.StairsUpTexture, false, isStairsUp: true);
        _stairsUpNode.GlobalPosition = _tileMap.MapToLocal(_stairsUpPosition);
        _entities.AddChild(_stairsUpNode);

        _spawnTimer.Connect(Timer.SignalName.Timeout, new Callable(this, MethodName.OnSpawnTimerTimeout));
        EventBus.Instance.Connect(
            EventBus.SignalName.EnemyDefeated,
            new Callable(this, MethodName.OnEnemyDefeated));
    }

    private void OnStairsUpEntered(Node2D body)
    {
        if (!body.IsInGroup(Constants.Groups.Player))
            return;
        if (ScreenTransition.Instance.IsTransitioning || AscendDialog.Instance.IsOpen)
            return;

        AscendDialog.Instance.Show();
    }

    private void OnStairsDownEntered(Node2D body)
    {
        if (!body.IsInGroup(Constants.Groups.Player) || ScreenTransition.Instance.IsTransitioning)
            return;

        int nextFloor = GameState.Instance.FloorNumber + 1;
        ScreenTransition.Instance.Play(
            Strings.Floor.FloorNumber(nextFloor),
            () => PerformFloorDescent(),
            Strings.Floor.Descending
        );
    }

    private void PerformFloorDescent()
    {
        GameState.Instance.FloorNumber += 1;

        // Clear all enemies
        foreach (Node child in _entities.GetChildren())
        {
            if (child.IsInGroup(Constants.Groups.Enemies))
                child.QueueFree();
        }

        // Clear old tiles
        _tileMap.Clear();

        // Generate new floor layout
        GenerateFloor();

        // Move player to the up-stairs position (where they "arrive")
        _player.GlobalPosition = _tileMap.MapToLocal(_stairsUpPosition);

        // Reset grace period
        if (_player is Player p)
            p.StartGracePeriod();

        // Reposition stairs objects
        _stairsDownNode.GlobalPosition = _tileMap.MapToLocal(_stairsDownPosition);
        _stairsUpNode.GlobalPosition = _tileMap.MapToLocal(_stairsUpPosition);

        // Spawn enemies for new floor
        for (int i = 0; i < Constants.Spawning.InitialEnemies; i++)
            SpawnEnemy();
    }

    private void SetupTileset()
    {
        var tileSet = new TileSet();
        tileSet.TileShape = TileSet.TileShapeEnum.Isometric;
        tileSet.TileSize = Constants.Tiles.TileSize;
        tileSet.AddPhysicsLayer();

        // Load all floor tile variations as separate atlas sources
        _floorSourceBaseId = -1;
        _floorSourceCount = 0;

        foreach (string path in Constants.Assets.DungeonFloorTextures)
        {
            if (!ResourceLoader.Exists(path))
                continue;

            Texture2D tex = GD.Load<Texture2D>(path);
            var source = new TileSetAtlasSource();
            source.Texture = tex;
            source.TextureRegionSize = Constants.Tiles.TextureRegionSize;
            source.CreateTile(Constants.Tiles.AtlasCoords);
            int id = tileSet.AddSource(source);

            if (_floorSourceBaseId < 0)
                _floorSourceBaseId = id;
            _floorSourceCount++;
        }

        // Wall tile
        Texture2D wallTexture = GD.Load<Texture2D>(Constants.Assets.DungeonWallTexture);
        var wallSource = new TileSetAtlasSource();
        wallSource.Texture = wallTexture;
        wallSource.TextureRegionSize = Constants.Tiles.TextureRegionSize;
        wallSource.CreateTile(Constants.Tiles.AtlasCoords);
        _wallSourceId = tileSet.AddSource(wallSource);

        // Rectangle collision for smooth wall sliding
        TileData wallTileData = wallSource.GetTileData(Constants.Tiles.AtlasCoords, 0);
        wallTileData.AddCollisionPolygon(0);
        wallTileData.SetCollisionPolygonPoints(0, 0, Constants.Tiles.WallCollisionPolygon);

        _tileMap.TileSet = tileSet;
    }

    private void GenerateFloor()
    {
        // Random room size — grows slightly with floor depth
        int floorBonus = Mathf.Min(GameState.Instance.FloorNumber / Constants.FloorScaling.RoomGrowthPerFloors, Constants.FloorScaling.MaxRoomGrowth);
        _roomWidth = (int)GD.RandRange(Constants.FloorScaling.MinRoomSize + floorBonus, Constants.FloorScaling.MaxRoomSize + floorBonus + 1);
        _roomHeight = (int)GD.RandRange(Constants.FloorScaling.MinRoomSize + floorBonus, Constants.FloorScaling.MaxRoomSize + floorBonus + 1);

        // Paint the room
        for (int col = 0; col < _roomWidth; col++)
        {
            for (int row = 0; row < _roomHeight; row++)
            {
                bool isBorder = col == 0 || col == _roomWidth - 1 ||
                                row == 0 || row == _roomHeight - 1;

                if (isBorder)
                {
                    _tileMap.SetCell(new Vector2I(col, row), _wallSourceId, Constants.Tiles.AtlasCoords);
                }
                else
                {
                    // Random floor tile variation
                    int sourceId = _floorSourceBaseId + (int)(GD.Randi() % _floorSourceCount);
                    _tileMap.SetCell(new Vector2I(col, row), sourceId, Constants.Tiles.AtlasCoords);
                }
            }
        }

        // Place stairs with minimum margin from any wall (room for entities around stairs)
        int wallMargin = Constants.FloorScaling.StairsWallMargin;
        int centerCol = _roomWidth / 2;
        int centerRow = _roomHeight / 2;

        if (GD.Randf() > 0.5f)
        {
            _stairsUpPosition = new Vector2I(
                (int)GD.RandRange(wallMargin, centerCol - 2),
                (int)GD.RandRange(wallMargin, centerRow - 2));
            _stairsDownPosition = new Vector2I(
                (int)GD.RandRange(centerCol + 2, _roomWidth - 1 - wallMargin),
                (int)GD.RandRange(centerRow + 2, _roomHeight - 1 - wallMargin));
        }
        else
        {
            _stairsUpPosition = new Vector2I(
                (int)GD.RandRange(centerCol + 2, _roomWidth - 1 - wallMargin),
                (int)GD.RandRange(centerRow + 2, _roomHeight - 1 - wallMargin));
            _stairsDownPosition = new Vector2I(
                (int)GD.RandRange(wallMargin, centerCol - 2),
                (int)GD.RandRange(wallMargin, centerRow - 2));
        }
    }

    private Node2D CreateStairsObject(string labelText, Color labelColor, string texturePath,
        bool isDown, bool isStairsUp = false)
    {
        var root = new Node2D();

        // Sprite (stairs tile art)
        if (ResourceLoader.Exists(texturePath))
        {
            var sprite = new Sprite2D();
            sprite.Texture = GD.Load<Texture2D>(texturePath);
            sprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
            sprite.Offset = new Vector2(0, -16);
            root.AddChild(sprite);
        }

        // Collision body (player bumps into stairs)
        var body = new StaticBody2D();
        body.CollisionLayer = Constants.Layers.Walls;
        var bodyShape = new CollisionShape2D();
        var circle = new CircleShape2D();
        circle.Radius = Constants.Effects.StairsCollisionRadius;
        bodyShape.Shape = circle;
        body.AddChild(bodyShape);
        root.AddChild(body);

        // Trigger area (detects player approach)
        if (isDown || isStairsUp)
        {
            var area = new Area2D();
            area.CollisionLayer = 0;
            area.CollisionMask = Constants.Layers.Player;
            area.Monitoring = true;
            var areaShape = new CollisionShape2D();
            var triggerCircle = new CircleShape2D();
            triggerCircle.Radius = Constants.Effects.StairsTriggerRadius;
            areaShape.Shape = triggerCircle;
            area.AddChild(areaShape);

            if (isDown)
                area.Connect(Area2D.SignalName.BodyEntered, new Callable(this, MethodName.OnStairsDownEntered));
            else
                area.Connect(Area2D.SignalName.BodyEntered, new Callable(this, MethodName.OnStairsUpEntered));

            root.AddChild(area);
        }

        // Label above stairs
        var label = new Label();
        label.Text = labelText;
        label.AddThemeColorOverride("font_color", labelColor);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 3);
        label.AddThemeFontSizeOverride("font_size", 11);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.Position = new Vector2(-30, -48);
        root.AddChild(label);

        return root;
    }

    private void SpawnEnemy()
    {
        var player = GetTree().GetFirstNodeInGroup(Constants.Groups.Player) as Node2D;
        Vector2 playerPos = player?.GlobalPosition ?? _tileMap.MapToLocal(new Vector2I(_roomWidth / 2, _roomHeight / 2));

        Vector2 spawnPos = Vector2.Zero;
        bool foundSafe = false;

        for (int attempt = 0; attempt < Constants.Spawning.MaxSpawnRetries; attempt++)
        {
            spawnPos = GetRandomEdgePosition();
            Vector2I tileCoord = _tileMap.LocalToMap(spawnPos);

            // Must be a floor tile AND far enough from player
            if (!IsFloorTile(tileCoord))
                continue;
            if (spawnPos.DistanceTo(playerPos) < Constants.Spawning.SafeSpawnRadius)
                continue;

            foundSafe = true;
            break;
        }

        if (!foundSafe)
            return;

        var enemy = EnemyScene.Instantiate<Enemy>();

        int floor = GameState.Instance.FloorNumber;
        int minLevel = Constants.FloorScaling.GetMinEnemyLevel(floor);
        int maxLevel = Constants.FloorScaling.GetMaxEnemyLevel(floor);
        enemy.Level = (int)GD.RandRange(minLevel, maxLevel + 1);

        // Random species — only use species that have assets available
        enemy.SpeciesIndex = GetRandomAvailableSpecies();

        enemy.GlobalPosition = spawnPos;
        _entities.AddChild(enemy);

        EventBus.Instance.EmitSignal(EventBus.SignalName.EnemySpawned, enemy);
    }

    private static int GetRandomAvailableSpecies()
    {
        var available = new System.Collections.Generic.List<int>();
        for (int i = 0; i < Constants.Assets.EnemySpeciesRotations.Length; i++)
        {
            string path = Constants.Assets.EnemySpeciesRotations[i] + "/south.png";
            if (ResourceLoader.Exists(path))
                available.Add(i);
        }
        return available.Count > 0
            ? available[(int)(GD.Randi() % available.Count)]
            : 0;
    }

    private Vector2 GetRandomEdgePosition()
    {
        // Spawn inside walls — never on or near wall tiles
        int margin = Constants.Spawning.SpawnWallMargin;
        int edge = (int)(GD.Randi() % 4);
        int spanW = _roomWidth - margin * 2;
        int spanH = _roomHeight - margin * 2;
        if (spanW < 1) spanW = 1;
        if (spanH < 1) spanH = 1;

        Vector2I coords = edge switch
        {
            0 => new Vector2I(margin + (int)(GD.Randi() % spanW), margin),
            1 => new Vector2I(_roomWidth - 1 - margin, margin + (int)(GD.Randi() % spanH)),
            2 => new Vector2I(margin + (int)(GD.Randi() % spanW), _roomHeight - 1 - margin),
            _ => new Vector2I(margin, margin + (int)(GD.Randi() % spanH)),
        };

        return _tileMap.MapToLocal(coords);
    }

    private bool IsFloorTile(Vector2I coords)
    {
        // Floor tiles are inside the border (not row/col 0 or roomSize-1)
        return coords.X > 0 && coords.X < _roomWidth - 1 &&
               coords.Y > 0 && coords.Y < _roomHeight - 1;
    }

    private void OnSpawnTimerTimeout()
    {
        int enemyCount = GetTree().GetNodesInGroup(Constants.Groups.Enemies).Count;
        if (enemyCount < Constants.Spawning.EnemySoftCap)
            SpawnEnemy();
    }

    private async void OnEnemyDefeated(Vector2 position, int tier)
    {
        await ToSignal(GetTree().CreateTimer(Constants.Spawning.RespawnDelay), "timeout");
        if (IsInsideTree())
            SpawnEnemy();
    }
}
