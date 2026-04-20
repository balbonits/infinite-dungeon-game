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
        // Note: SpawnTimer.WaitTime is refreshed per-tick in OnSpawnTimerTimeout
        // so pressure-driven SpawnRateModifier shifts apply on the next cycle
        // (modifier evolves over time via DungeonIntelligence.Update).

        SetupTileset();
        GenerateFloor();

        // Spawn player at the up-stairs position (where you "arrived" from)
        _player = PlayerScene.Instantiate<CharacterBody2D>();
        _player.GlobalPosition = _tileMap.MapToLocal(_stairsUpPosition) + new Vector2(0, 40);
        _entities.AddChild(_player);

        // Spawn initial enemies — GUARANTEE the minimum count
        SpawnInitialEnemies();

        // LOOT-01: spawn world containers (Jar / Crate / Chest)
        SpawnContainers();

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

        // Clear all enemies + world containers. Both are floor-scoped
        // (respawn on floor change).
        foreach (Node child in _entities.GetChildren())
        {
            if (child.IsInGroup(Constants.Groups.Enemies) ||
                child.IsInGroup(Constants.Groups.Containers))
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

        // LOOT-01: spawn fresh containers on the new floor.
        SpawnContainers();
    }

    private void SetupTileset()
    {
        int zone = Constants.Zones.GetZone(GameState.Instance.FloorNumber);
        var (floorPaths, wallPath) = Constants.Assets.GetZoneTheme(zone);

        var tileSet = new TileSet();
        tileSet.TileShape = TileSet.TileShapeEnum.Square;
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

    /// <summary>
    /// LOOT-01 / SPEC-LOOT-01: spawn world containers on the current floor
    /// (Jar / Crate / Chest) at the counts produced by ContainerLootTable.
    /// Each container lands on a valid floor tile clear of the player spawn,
    /// both staircases, enemies, and other placed containers.
    ///
    /// Spec Acceptance #2 ("Every floor has ≥1 container"): enforced at both
    /// the roll layer (SpawnCounts min-1 guarantee) AND the placement layer
    /// (see the final fallback in PlaceContainerBatch). A retry-exhausted
    /// strict placement can't leave the floor with zero containers even if
    /// every candidate tile collides with an entity.
    /// </summary>
    private void SpawnContainers()
    {
        int floor = GameState.Instance.FloorNumber;
        var (jars, crates, chests) = ContainerLootTable.SpawnCounts(floor);

        int placed = 0;
        placed += PlaceContainerBatch(ContainerLootTable.ContainerType.Jar, jars);
        placed += PlaceContainerBatch(ContainerLootTable.ContainerType.Crate, crates);
        placed += PlaceContainerBatch(ContainerLootTable.ContainerType.Chest, chests);

        // Min-1 floor guarantee at the placement layer. Spec Acceptance #2
        // cannot ship a floor with zero containers. Two fallback tiers:
        //   1. Relaxed placement — same exclusion zones, no entity-overlap
        //      check. Covers the normal case where random sampling hit
        //      crowded tiles every time.
        //   2. Deterministic stairs-up-offset placement — if even relaxed
        //      can't find a spot in MaxSpawnRetries tries (the pathological
        //      case), drop one jar on a tile offset from the up-stairs by
        //      +2 tiles in both axes. That tile is on-map by construction
        //      (GenerateFloor guarantees stairs sit inside walled rooms with
        //      floor-tile neighbors) and always outside the 1-tile stairs
        //      buffer, satisfying both the min-1 contract AND the
        //      exclusion rules.
        if (placed == 0 && !PlaceContainerRelaxed(ContainerLootTable.ContainerType.Jar))
            PlaceContainerDeterministicFallback(ContainerLootTable.ContainerType.Jar);
    }

    /// <summary>Returns the number of containers actually placed.</summary>
    private int PlaceContainerBatch(ContainerLootTable.ContainerType type, int count)
    {
        int placed = 0;
        for (int i = 0; i < count; i++)
        {
            var pos = FindValidContainerPosition();
            if (pos == null) continue; // skip this one, keep trying the batch.
            var container = Container.Create(type, pos.Value);
            _entities.AddChild(container);
            placed++;
        }
        return placed;
    }

    /// <summary>
    /// Exclusion-zone check shared by strict and relaxed placement. Player
    /// spawn sits at the up-stairs tile per Dungeon._Ready / PerformFloorDescent,
    /// so the same stairs buffer covers both.
    /// </summary>
    private bool IsOutsideExclusionZones(Vector2 pos)
    {
        const float bufferDistance = 48f; // 1-2 tile buffer.
        Vector2 stairsUpWorld = _tileMap.MapToLocal(_stairsUpPosition);
        Vector2 stairsDownWorld = _tileMap.MapToLocal(_stairsDownPosition);
        if (pos.DistanceTo(stairsUpWorld) < bufferDistance) return false;
        if (pos.DistanceTo(stairsDownWorld) < bufferDistance) return false;

        // Player spawn. _player is null for the frame of _Ready before it's
        // instantiated — in that window the spawn position equals the
        // stairs-up tile so the stairsUp buffer above covers it.
        if (_player != null)
        {
            if (pos.DistanceTo(_player.GlobalPosition) < bufferDistance) return false;
        }
        return true;
    }

    private Vector2? FindValidContainerPosition()
    {
        for (int attempt = 0; attempt < Constants.Spawning.MaxSpawnRetries; attempt++)
        {
            Vector2 pos = GetRandomFloorPosition();
            Vector2I tileCoord = _tileMap.LocalToMap(pos);

            if (!IsFloorTile(tileCoord)) continue;
            if (!IsOutsideExclusionZones(pos)) continue;

            // Avoid stacking on an enemy or another container already placed.
            bool occupied = false;
            foreach (Node node in _entities.GetChildren())
            {
                if (node is not Node2D n2d) continue;
                if (!node.IsInGroup(Constants.Groups.Enemies) &&
                    !node.IsInGroup(Constants.Groups.Containers)) continue;
                if (n2d.GlobalPosition.DistanceTo(pos) < 24f)
                {
                    occupied = true;
                    break;
                }
            }
            if (occupied) continue;

            return pos;
        }
        return null;
    }

    /// <summary>
    /// Placement fallback — relaxes the entity-overlap check so the spec's
    /// min-1 container contract can't be violated by an edge-case layout
    /// where every candidate tile collides with an enemy / prior container.
    /// Still enforces the stairs + player-spawn exclusion zones because
    /// dropping a container on stairs would block descent/ascent and
    /// dropping it on spawn would trap the player on floor entry.
    /// Returns true iff a container was placed.
    /// </summary>
    private bool PlaceContainerRelaxed(ContainerLootTable.ContainerType type)
    {
        for (int attempt = 0; attempt < Constants.Spawning.MaxSpawnRetries; attempt++)
        {
            Vector2 pos = GetRandomFloorPosition();
            Vector2I tileCoord = _tileMap.LocalToMap(pos);
            if (!IsFloorTile(tileCoord)) continue;
            if (!IsOutsideExclusionZones(pos)) continue;
            var container = Container.Create(type, pos);
            _entities.AddChild(container);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Deterministic last-resort placement — fires only when both strict
    /// and relaxed random-sampling exhausted retries. Puts one container
    /// on a tile offset from the up-stairs by +2 tiles in both axes. Per
    /// FloorGenerator's room-generation guarantees, the up-stairs sits
    /// inside a walled room with floor-tile neighbors, and a +2/+2 offset
    /// clears the 48px (1-1.5 tile) exclusion buffer. This is the
    /// insurance that spec Acceptance #2 (min-1 containers per floor) is
    /// truly unbreakable — no sequence of random rolls can leave a floor
    /// with zero containers.
    /// </summary>
    private void PlaceContainerDeterministicFallback(ContainerLootTable.ContainerType type)
    {
        var offset = _stairsUpPosition + new Vector2I(2, 2);
        Vector2 pos = _tileMap.MapToLocal(offset);
        // Guard: if the offset isn't a floor tile (very unusual room shape),
        // scan a small spiral around up-stairs for a valid tile.
        if (!IsFloorTile(offset))
        {
            for (int dy = -3; dy <= 3 && !IsFloorTile(offset); dy++)
                for (int dx = -3; dx <= 3 && !IsFloorTile(offset); dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var candidate = _stairsUpPosition + new Vector2I(dx, dy);
                    if (IsFloorTile(candidate))
                    {
                        offset = candidate;
                        pos = _tileMap.MapToLocal(offset);
                        break;
                    }
                }
        }
        var container = Container.Create(type, pos);
        _entities.AddChild(container);
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
        // Refresh the next cycle's wait time against current Intelligence pressure.
        _spawnTimer.WaitTime = Constants.Spawning.SpawnInterval / GameState.Instance.Intelligence.SpawnRateModifier;

        if (_floorWiped)
            return; // No spawning after floor wipe until player chooses

        int enemyCount = GetTree().GetNodesInGroup(Constants.Groups.Enemies).Count;
        if (enemyCount < Constants.Spawning.EnemySoftCap)
            SpawnEnemy();
    }

    private async void OnEnemyDefeated(Vector2 position, int tier)
    {
        // AUDIT-07 guard: hoist the tree check to the top. Pre-fix, we
        // incremented _killCount and fired quest-tracking before checking
        // IsInsideTree — if the dungeon was mid-tear-down (scene unload
        // during class-confirm transition), the mutation hit a freed-ish
        // instance and the signal side effects leaked into the next run.
        // Safer to bail immediately when we're off-tree.
        if (!IsInsideTree())
            return;

        _killCount++;

        // Track quest progress
        int currentFloor = GameState.Instance.FloorNumber;
        var completedQuest = GameState.Instance.Quests.RecordEnemyKill(currentFloor);
        if (completedQuest != null)
            Toast.Instance?.Success($"Quest ready: {completedQuest.Title}");

        // Wait before checking wipe — gives QueueFree time to process.
        // Re-check IsInsideTree after the await since the scene could
        // unload during the 0.1s timer.
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
