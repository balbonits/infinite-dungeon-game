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

    private int _mapWidth;
    private int _mapHeight;
    private Vector2I _stairsDownPosition;
    private Vector2I _stairsUpPosition;
    private int _floorSourceBaseId;
    private int _wallSourceId;
    private int _floorSourceCount;
    private FloorGenerator? _floorGen;

    public override void _Ready()
    {
        _tileMap = GetNode<TileMapLayer>("TileMapLayer");
        _entities = GetNode<Node2D>("Entities");
        _spawnTimer = GetNode<Timer>("SpawnTimer");

        SetupTileset();
        GenerateFloor();

        // Spawn player at the up-stairs position (where you "arrived" from)
        _player = PlayerScene.Instantiate<CharacterBody2D>();
        _player.GlobalPosition = _tileMap.MapToLocal(_stairsUpPosition) + new Vector2(0, 40);
        _entities.AddChild(_player);

        // Spawn initial enemies — GUARANTEE the minimum count
        SpawnInitialEnemies();

        PlaceStairs();

        // Set compass targets
        UpdateCompass();

        _spawnTimer.Connect(Timer.SignalName.Timeout, new Callable(this, MethodName.OnSpawnTimerTimeout));
        EventBus.Instance.Connect(
            EventBus.SignalName.EnemyDefeated,
            new Callable(this, MethodName.OnEnemyDefeated));
    }

    private void UpdateCompass()
    {
        StairsCompass.Instance?.SetTargets(
            _tileMap.MapToLocal(_stairsDownPosition),
            _tileMap.MapToLocal(_stairsUpPosition));
    }

    private void OnStairsUpEntered(Node2D body)
    {
        if (!body.IsInGroup(Constants.Groups.Player))
            return;
        if (ScreenTransition.Instance.IsTransitioning)
            return;

        if (GameState.Instance.FloorNumber == 1)
        {
            // Floor 1: go straight to town, no dialog needed
            ScreenTransition.Instance.Play(
                Strings.Town.DungeonEntrance,
                () => Scenes.Main.Instance.LoadTown(),
                Strings.Ascend.ReturningToTown);
        }
        else if (!AscendDialog.Instance.IsOpen)
        {
            AscendDialog.Instance.Show();
        }
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

        // Track depth push quest
        var depthQuest = GameState.Instance.Quests.RecordFloorReached(GameState.Instance.FloorNumber);
        if (depthQuest != null)
            Ui.Toast.Instance?.Success($"Quest ready: {depthQuest.Title}");

        // Achievement: deepest floor
        GameState.Instance.Achievements.SetCounter("deepest_floor", GameState.Instance.FloorNumber);

        // Reset floor state
        _floorWiped = false;
        _killCount = 0;

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
        _player.GlobalPosition = _tileMap.MapToLocal(_stairsUpPosition) + new Vector2(0, 40);

        // Reset grace period
        if (_player is Player p)
            p.StartGracePeriod();

        // Recreate stairs with correct labels for the new floor
        _stairsDownNode.QueueFree();
        _stairsUpNode.QueueFree();
        PlaceStairs();

        UpdateCompass();

        // Spawn enemies for new floor — GUARANTEE the minimum count
        SpawnInitialEnemies();
    }

    private void SetupTileset()
    {
        int zone = Constants.Zones.GetZone(GameState.Instance.FloorNumber);
        var (floorPaths, wallPath) = Constants.Assets.GetZoneTheme(zone);

        var tileSet = new TileSet();
        tileSet.TileShape = TileSet.TileShapeEnum.Isometric;
        tileSet.TileSize = Constants.Tiles.TileSize;
        tileSet.AddPhysicsLayer();

        // Load zone floor tile variations
        _floorSourceBaseId = -1;
        _floorSourceCount = 0;

        foreach (string path in floorPaths)
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

        // Fallback to original dungeon tiles if zone tiles not found
        if (_floorSourceCount == 0)
        {
            foreach (string path in Constants.Assets.DungeonFloorTextures)
            {
                if (!ResourceLoader.Exists(path)) continue;
                Texture2D tex = GD.Load<Texture2D>(path);
                var source = new TileSetAtlasSource();
                source.Texture = tex;
                source.TextureRegionSize = Constants.Tiles.TextureRegionSize;
                source.CreateTile(Constants.Tiles.AtlasCoords);
                int id = tileSet.AddSource(source);
                if (_floorSourceBaseId < 0) _floorSourceBaseId = id;
                _floorSourceCount++;
            }
        }

        // Zone wall tile (with fallback)
        string actualWallPath = ResourceLoader.Exists(wallPath) ? wallPath : Constants.Assets.DungeonWallTexture;
        Texture2D wallTexture = GD.Load<Texture2D>(actualWallPath);
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
        int floor = GameState.Instance.FloorNumber;
        int seed = (int)(GD.Randi() ^ (uint)(floor * 7919));

        _floorGen = new FloorGenerator(seed);
        _floorGen.Generate(floor);

        _mapWidth = _floorGen.Width;
        _mapHeight = _floorGen.Height;

        // Paint the grid onto the tilemap.
        // Walls are only placed as a 1-tile border around floor areas — not as solid blocks.
        for (int col = 0; col < _mapWidth; col++)
        {
            for (int row = 0; row < _mapHeight; row++)
            {
                if (_floorGen.Grid[col, row] == FloorGenerator.Tile.Floor)
                {
                    // Weighted variant selection — base tiles dominate for visual cohesion.
                    // variant 0: 50%, variant 1: 25%, remaining variants split 25%.
                    float roll = GD.Randf();
                    int variant;
                    if (_floorSourceCount <= 2 || roll < 0.50f)
                        variant = 0;
                    else if (roll < 0.75f)
                        variant = 1;
                    else
                        variant = 2 + (int)(GD.Randi() % (_floorSourceCount - 2));

                    int sourceId = _floorSourceBaseId + variant;
                    _tileMap.SetCell(new Vector2I(col, row), sourceId, Constants.Tiles.AtlasCoords);
                }
                else if (IsAdjacentToFloor(col, row))
                {
                    _tileMap.SetCell(new Vector2I(col, row), _wallSourceId, Constants.Tiles.AtlasCoords);
                }
                // else: leave empty (no tile) — avoids thick wall blocks
            }
        }

        _stairsUpPosition = _floorGen.EntrancePos;
        _stairsDownPosition = _floorGen.ExitPos;
    }

    private void PlaceStairs()
    {
        int currentFloor = GameState.Instance.FloorNumber;

        _stairsDownNode = CreateStairsObject(Strings.Floor.StairsDown, UiTheme.Colors.Accent,
            Constants.Assets.StairsDownTexture, true);
        _stairsDownNode.GlobalPosition = _tileMap.MapToLocal(_stairsDownPosition);
        _entities.AddChild(_stairsDownNode);

        string upLabel = currentFloor <= 1
            ? Strings.Ascend.ReturnToTown
            : Strings.Floor.StairsUp;
        Color upColor = currentFloor <= 1
            ? UiTheme.Colors.Safe
            : UiTheme.Colors.Muted;
        _stairsUpNode = CreateStairsObject(upLabel, upColor,
            Constants.Assets.StairsUpTexture, false, isStairsUp: true);
        _stairsUpNode.GlobalPosition = _tileMap.MapToLocal(_stairsUpPosition);
        _entities.AddChild(_stairsUpNode);
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

    /// <summary>
    /// Keep spawning until InitialEnemies are actually in the scene tree.
    /// Never exits with fewer than the minimum.
    /// </summary>
    private void SpawnInitialEnemies()
    {
        int target = Constants.Spawning.InitialEnemies;
        int maxAttempts = target * 10; // hard safety cap
        int attempts = 0;

        while (GetTree().GetNodesInGroup(Constants.Groups.Enemies).Count < target && attempts < maxAttempts)
        {
            SpawnEnemy();
            attempts++;
        }
    }

    private void SpawnEnemy()
    {
        var player = GetTree().GetFirstNodeInGroup(Constants.Groups.Player) as Node2D;
        Vector2 playerPos = player?.GlobalPosition ?? _tileMap.MapToLocal(new Vector2I(_mapWidth / 2, _mapHeight / 2));

        Vector2 spawnPos = Vector2.Zero;
        bool foundSafe = false;

        for (int attempt = 0; attempt < Constants.Spawning.MaxSpawnRetries; attempt++)
        {
            spawnPos = GetRandomFloorPosition();
            Vector2I tileCoord = _tileMap.LocalToMap(spawnPos);

            // Must be a floor tile, far from player, AND far from both staircases
            if (!IsFloorTile(tileCoord))
                continue;
            if (spawnPos.DistanceTo(playerPos) < Constants.Spawning.SafeSpawnRadius)
                continue;

            Vector2 stairsUpWorld = _tileMap.MapToLocal(_stairsUpPosition);
            Vector2 stairsDownWorld = _tileMap.MapToLocal(_stairsDownPosition);
            if (spawnPos.DistanceTo(stairsUpWorld) < Constants.Spawning.SafeSpawnRadius)
                continue;
            if (spawnPos.DistanceTo(stairsDownWorld) < Constants.Spawning.SafeSpawnRadius)
                continue;

            foundSafe = true;
            break;
        }

        // Guarantee spawn: if safe position not found, use any valid floor tile
        if (!foundSafe)
        {
            for (int attempt = 0; attempt < Constants.Spawning.MaxSpawnRetries; attempt++)
            {
                spawnPos = GetRandomFloorPosition();
                Vector2I tileCoord = _tileMap.LocalToMap(spawnPos);
                if (IsFloorTile(tileCoord))
                {
                    foundSafe = true;
                    break;
                }
            }
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
        int floor = GameState.Instance.FloorNumber;
        int[] zoneSpecies = Constants.Zones.GetZoneSpecies(floor);

        // Filter to species with assets available
        var available = new System.Collections.Generic.List<int>();
        foreach (int species in zoneSpecies)
        {
            if (species >= 0 && species < Constants.Assets.EnemySpeciesRotations.Length)
            {
                string path = Constants.Assets.EnemySpeciesRotations[species] + "/south.png";
                if (ResourceLoader.Exists(path))
                    available.Add(species);
            }
        }

        return available.Count > 0
            ? available[(int)(GD.Randi() % available.Count)]
            : 0;
    }

    private Vector2 GetRandomFloorPosition()
    {
        // Pick a random floor tile from the generated layout
        for (int attempt = 0; attempt < 50; attempt++)
        {
            int x = (int)(GD.Randi() % _mapWidth);
            int y = (int)(GD.Randi() % _mapHeight);
            if (_floorGen != null && x > 0 && x < _mapWidth - 1 &&
                y > 0 && y < _mapHeight - 1 &&
                _floorGen.Grid[x, y] == FloorGenerator.Tile.Floor)
            {
                return _tileMap.MapToLocal(new Vector2I(x, y));
            }
        }
        // Fallback to center
        return _tileMap.MapToLocal(new Vector2I(_mapWidth / 2, _mapHeight / 2));
    }

    private bool IsAdjacentToFloor(int col, int row)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int nx = col + dx, ny = row + dy;
                if (nx >= 0 && nx < _mapWidth && ny >= 0 && ny < _mapHeight
                    && _floorGen!.Grid[nx, ny] == FloorGenerator.Tile.Floor)
                    return true;
            }
        }
        return false;
    }

    private bool IsFloorTile(Vector2I coords)
    {
        if (coords.X <= 0 || coords.X >= _mapWidth - 1 ||
            coords.Y <= 0 || coords.Y >= _mapHeight - 1)
            return false;
        return _floorGen != null && _floorGen.Grid[coords.X, coords.Y] == FloorGenerator.Tile.Floor;
    }

    private bool _floorWiped;
    private int _killCount;

    private void OnSpawnTimerTimeout()
    {
        if (_floorWiped)
            return; // No spawning after floor wipe until player chooses

        int enemyCount = GetTree().GetNodesInGroup(Constants.Groups.Enemies).Count;
        if (enemyCount < Constants.Spawning.EnemySoftCap)
            SpawnEnemy();
    }

    private async void OnEnemyDefeated(Vector2 position, int tier)
    {
        _killCount++;

        // Track quest progress
        int currentFloor = GameState.Instance.FloorNumber;
        var completedQuest = GameState.Instance.Quests.RecordEnemyKill(currentFloor);
        if (completedQuest != null)
            Toast.Instance?.Success($"Quest ready: {completedQuest.Title}");

        // Wait before checking wipe — gives QueueFree time to process
        await ToSignal(GetTree().CreateTimer(0.1), "timeout");
        if (!IsInsideTree())
            return;

        int remaining = GetTree().GetNodesInGroup(Constants.Groups.Enemies).Count;

        // Floor wipe requires: no enemies left AND at least InitialEnemies were killed
        if (remaining == 0 && !_floorWiped && _killCount >= Constants.Spawning.InitialEnemies)
        {
            _floorWiped = true;
            _spawnTimer.Stop();

            // Track floor clear quest + achievement
            var clearQuest = GameState.Instance.Quests.RecordFloorClear(GameState.Instance.FloorNumber);
            if (clearQuest != null)
                Toast.Instance?.Success($"Quest ready: {clearQuest.Title}");
            GameState.Instance.Achievements.IncrementCounter("floor_wipes");

            // 3 second delay before showing wipe dialog
            await ToSignal(GetTree().CreateTimer(3.0), "timeout");
            if (IsInsideTree() && _floorWiped)
            {
                FloorWipeDialog.Instance?.ShowWipe();
            }
            return;
        }

        // Normal respawn if floor not wiped
        await ToSignal(GetTree().CreateTimer(Constants.Spawning.RespawnDelay), "timeout");
        if (IsInsideTree() && !_floorWiped)
            SpawnEnemy();
    }
}
