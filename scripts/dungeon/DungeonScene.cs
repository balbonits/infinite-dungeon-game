using Godot;
using System;
using System.Collections.Generic;
using DungeonGame.Dungeon;

/// <summary>
/// Main dungeon gameplay scene. Wires together the procedural dungeon,
/// player, enemies, combat, exploration, automap, HUD, and pause menu.
/// This is the core game loop: explore floors, fight enemies, collect loot,
/// descend deeper or return to town.
/// </summary>
public partial class DungeonScene : Node2D
{
    private const int TileW = 64;
    private const int TileH = 32;
    private const float AttackRange = 78f;
    private const float AttackCooldown = 0.42f;
    private const float ExploreRadius = 5;
    private const float ExitProximity = 48f;
    private const float EntranceEdgeProximity = 32f;

    // Monster name pools by tier
    private static readonly string[] Tier1Names = { "Rat", "Bat", "Slime", "Spider" };
    private static readonly string[] Tier2Names = { "Goblin", "Skeleton", "Zombie", "Wolf" };
    private static readonly string[] Tier3Names = { "Ogre", "Wraith", "Dark Knight", "Demon" };

    private Camera2D _camera;
    private FloorCache _floorCache;
    private TileMapLayer _tileMap;
    private FloorData _floor;
    private AStarGrid2D _astar;
    private PlayerController _player;
    private Node2D _entityContainer;
    private DungeonGame.Automap _automap;
    private GameplayHud _hud;

    private readonly List<EnemyEntity> _enemies = new();
    private float _attackTimer;
    private int _lastExploredX = -1;
    private int _lastExploredY = -1;
    private bool _playerDead;
    private float _deathTimer;
    private Label _floorAnnounce;
    private float _floorAnnounceTimer;
    private Label _entrancePrompt;
    private bool _floorTransitioning;

    // Track exit/boss rooms for floor progression
    private RoomData _exitRoom;
    private RoomData _bossRoom;
    private EnemyEntity _bossEnemy;

    public override void _Ready()
    {
        GameState.Location = GameLocation.Dungeon;

        // Ground color background (dark stone grey matching dungeon floor tiles)
        var bg = new ColorRect();
        bg.Color = new Color(0.12f, 0.11f, 0.10f);
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var bgLayer = new CanvasLayer { Layer = -1 };
        bgLayer.AddChild(bg);
        AddChild(bgLayer);

        _camera = GetNode<Camera2D>("Camera2D");

        // Floor cache
        _floorCache = new FloorCache(new DungeonGenerator());

        // Entity container with Y-sort for proper draw ordering
        _entityContainer = new Node2D();
        _entityContainer.YSortEnabled = true;
        AddChild(_entityContainer);

        // UI layer
        var uiLayer = new CanvasLayer();
        AddChild(uiLayer);

        _hud = new GameplayHud();
        uiLayer.AddChild(_hud);

        var pauseMenu = new PauseMenu();
        uiLayer.AddChild(pauseMenu);

        // Floor announcement label (centered, fades out)
        _floorAnnounce = new Label();
        _floorAnnounce.HorizontalAlignment = HorizontalAlignment.Center;
        _floorAnnounce.VerticalAlignment = VerticalAlignment.Center;
        _floorAnnounce.AddThemeFontSizeOverride("font_size", 28);
        _floorAnnounce.AddThemeColorOverride("font_color", new Color(0.961f, 0.784f, 0.420f));
        _floorAnnounce.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.8f));
        _floorAnnounce.AddThemeConstantOverride("shadow_offset_x", 2);
        _floorAnnounce.AddThemeConstantOverride("shadow_offset_y", 2);
        _floorAnnounce.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        _floorAnnounce.GrowHorizontal = Control.GrowDirection.Both;
        _floorAnnounce.GrowVertical = Control.GrowDirection.Both;
        _floorAnnounce.OffsetLeft = -200;
        _floorAnnounce.OffsetRight = 200;
        _floorAnnounce.OffsetTop = -25;
        _floorAnnounce.OffsetBottom = 25;
        _floorAnnounce.Visible = false;
        uiLayer.AddChild(_floorAnnounce);

        // Entrance return prompt
        _entrancePrompt = new Label();
        _entrancePrompt.Text = "Press S to return to town";
        _entrancePrompt.HorizontalAlignment = HorizontalAlignment.Center;
        _entrancePrompt.AddThemeFontSizeOverride("font_size", 12);
        _entrancePrompt.AddThemeColorOverride("font_color", new Color(0.961f, 0.784f, 0.420f));
        _entrancePrompt.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.8f));
        _entrancePrompt.AddThemeConstantOverride("shadow_offset_x", 1);
        _entrancePrompt.AddThemeConstantOverride("shadow_offset_y", 1);
        _entrancePrompt.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterBottom);
        _entrancePrompt.GrowHorizontal = Control.GrowDirection.Both;
        _entrancePrompt.OffsetLeft = -150;
        _entrancePrompt.OffsetRight = 150;
        _entrancePrompt.OffsetTop = -50;
        _entrancePrompt.OffsetBottom = -26;
        _entrancePrompt.Visible = false;
        uiLayer.AddChild(_entrancePrompt);

        // Automap overlay (on its own CanvasLayer so it renders above everything)
        var automapLayer = new CanvasLayer { Layer = 10 };
        AddChild(automapLayer);
        _automap = new DungeonGame.Automap();
        _automap.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        automapLayer.AddChild(_automap);

        // Load the current floor
        if (GameState.DungeonFloor < 1)
            GameState.DungeonFloor = 1;
        LoadFloor(GameState.DungeonFloor);
    }

    // ==================== FLOOR LOADING ====================

    private void LoadFloor(int floorNumber)
    {
        _floorTransitioning = false;

        // Update game state
        GameState.DungeonFloor = floorNumber;
        GameState.ActiveMonsters.Clear();

        // Deterministic seed
        int seed = floorNumber * 31337 + 42;

        // Generate or retrieve from cache
        _floor = _floorCache.GetFloor(floorNumber, seed);

        // Clear previous
        ClearFloor();

        // Build tilemap
        BuildTileMap();

        // Build A* pathfinding grid from floor data (see docs/basics/pathfinding.md)
        _astar = new AStarGrid2D();
        _astar.Region = new Rect2I(0, 0, _floor.Width, _floor.Height);
        _astar.CellSize = new Vector2(TileW, TileH);
        _astar.CellShape = AStarGrid2D.CellShapeEnum.IsometricDown;
        _astar.DiagonalMode = AStarGrid2D.DiagonalModeEnum.OnlyIfNoObstacles;
        _astar.JumpingEnabled = true;
        _astar.Update();

        for (int x = 0; x < _floor.Width; x++)
            for (int y = 0; y < _floor.Height; y++)
                if (_floor.IsWall(x, y))
                    _astar.SetPointSolid(new Vector2I(x, y), true);

        // Find special rooms
        _exitRoom = null;
        _bossRoom = null;
        _bossEnemy = null;
        RoomData entranceRoom = null;

        foreach (var room in _floor.Rooms)
        {
            switch (room.Kind)
            {
                case RoomKind.Entrance: entranceRoom = room; break;
                case RoomKind.Exit: _exitRoom = room; break;
                case RoomKind.Boss: _bossRoom = room; break;
            }
        }

        // Spawn player at entrance room center
        if (_tileMap != null && entranceRoom != null)
        {
            var spawnTile = new Vector2I(entranceRoom.CenterX, entranceRoom.CenterY);
            var spawnWorld = _tileMap.MapToLocal(spawnTile);

            _player = new PlayerController();
            _player.Position = spawnWorld;
            _player.ZIndex = 20;
            AddChild(_player);

            // Reparent camera to follow player
            _camera.GetParent().RemoveChild(_camera);
            _player.AddChild(_camera);
            _camera.Position = Vector2.Zero;
        }

        // Spawn enemies
        SpawnEnemies(floorNumber);

        // Set up automap
        if (entranceRoom != null)
        {
            _floor.MarkExplored(entranceRoom.CenterX, entranceRoom.CenterY, (int)ExploreRadius);
            _automap.SetFloorData(_floor);
            _automap.SetPlayerPosition(entranceRoom.CenterX, entranceRoom.CenterY);
            _lastExploredX = entranceRoom.CenterX;
            _lastExploredY = entranceRoom.CenterY;
        }

        // Show floor announcement
        ShowFloorAnnouncement(floorNumber);

        GD.Print($"[DUNGEON] Floor {floorNumber} loaded: {_floor.Width}x{_floor.Height}, " +
                 $"{_floor.Rooms.Count} rooms, {_enemies.Count} enemies");
    }

    private void ClearFloor()
    {
        // Remove player
        if (_player != null)
        {
            // Get camera back before removing player
            if (_camera.GetParent() == _player)
            {
                _player.RemoveChild(_camera);
                AddChild(_camera);
            }
            _player.QueueFree();
            _player = null;
        }

        // Remove tilemap
        if (_tileMap != null)
        {
            _tileMap.QueueFree();
            _tileMap = null;
        }

        // Remove all enemies
        foreach (var enemy in _enemies)
        {
            if (IsInstanceValid(enemy))
                enemy.QueueFree();
        }
        _enemies.Clear();

        // Clear entity container children
        foreach (var child in _entityContainer.GetChildren())
        {
            if (child is Node node)
                node.QueueFree();
        }
    }

    // ==================== TILE RENDERING ====================

    /// <summary>
    /// Build the TileMapLayer from FloorData.
    /// Copied from TestDungeonGen.BuildTileMap() — same atlas setup,
    /// same floor/wall painting logic.
    /// </summary>
    private void BuildTileMap()
    {
        var floorTex = TestHelper.LoadIssPng("res://assets/isometric/tiles/stone-soup/floors/floor_rect_gray.png");
        var wallTex = TestHelper.LoadIssPng("res://assets/isometric/tiles/stone-soup/walls/brick_gray.png");
        if (floorTex == null || wallTex == null)
        {
            GD.PrintErr("[DUNGEON] Could not load floor or wall texture");
            return;
        }

        var tileSet = new TileSet();
        tileSet.TileShape = TileSet.TileShapeEnum.Isometric;
        tileSet.TileLayout = TileSet.TileLayoutEnum.DiamondDown;
        tileSet.TileSize = new Vector2I(TileW, TileH);

        // Source 0: Floor tiles (64x32 isometric diamonds)
        var floorSource = new TileSetAtlasSource();
        floorSource.Texture = floorTex;
        floorSource.TextureRegionSize = new Vector2I(TileW, TileH);
        int floorSrcId = tileSet.AddSource(floorSource);

        int floorCols = floorTex.GetWidth() / TileW;
        int floorRows = floorTex.GetHeight() / TileH;
        for (int ax = 0; ax < floorCols; ax++)
            for (int ay = 0; ay < floorRows; ay++)
            {
                var coords = new Vector2I(ax, ay);
                if (!floorSource.HasTile(coords))
                    floorSource.CreateTile(coords);
            }

        // Source 1: Wall blocks (64x64 isometric cubes, row 0 of wall sheet)
        var wallSource = new TileSetAtlasSource();
        wallSource.Texture = wallTex;
        wallSource.TextureRegionSize = new Vector2I(64, 64);
        int wallSrcId = tileSet.AddSource(wallSource);

        int wallCols = wallTex.GetWidth() / 64;
        int wallSheetRows = wallTex.GetHeight() / 64;
        for (int ax = 0; ax < wallCols; ax++)
            for (int ay = 0; ay < wallSheetRows; ay++)
            {
                var coords = new Vector2I(ax, ay);
                if (!wallSource.HasTile(coords))
                    wallSource.CreateTile(coords);
            }
        int wallBlockVariants = wallCols;

        // Add physics layer for wall collision (layer 1 = walls)
        tileSet.AddPhysicsLayer();
        tileSet.SetPhysicsLayerCollisionLayer(0, 1); // walls on layer 1
        tileSet.SetPhysicsLayerCollisionMask(0, 0);  // walls don't detect anything

        // Add collision polygon to wall tiles (isometric diamond shape)
        for (int ax = 0; ax < wallCols; ax++)
            for (int ay = 0; ay < wallSheetRows; ay++)
            {
                var tileData = wallSource.GetTileData(new Vector2I(ax, ay), 0);
                if (tileData != null)
                {
                    tileData.AddCollisionPolygon(0);
                    tileData.SetCollisionPolygonPoints(0, 0, new Vector2[] {
                        new(0, -TileH / 2f), new(TileW / 2f, 0),
                        new(0, TileH / 2f), new(-TileW / 2f, 0)
                    });
                }
            }

        _tileMap = new TileMapLayer();
        _tileMap.TileSet = tileSet;
        _tileMap.YSortEnabled = true;
        AddChild(_tileMap);

        // Paint floor tiles
        int floorVariants = floorCols * floorRows;
        int floorIdx = 0;
        for (int x = 0; x < _floor.Width; x++)
        {
            for (int y = 0; y < _floor.Height; y++)
            {
                if (_floor.Tiles[x, y] == TileType.Floor)
                {
                    int ax = floorIdx % floorCols;
                    int ay = (floorIdx / floorCols) % floorRows;
                    _tileMap.SetCell(new Vector2I(x, y), floorSrcId, new Vector2I(ax, ay));
                    floorIdx = (floorIdx + 1) % floorVariants;
                }
            }
        }

        // Paint wall blocks on edge walls (adjacent to at least one floor tile)
        int wallIdx = 0;
        for (int x = 0; x < _floor.Width; x++)
        {
            for (int y = 0; y < _floor.Height; y++)
            {
                if (_floor.Tiles[x, y] != TileType.Wall) continue;

                bool isEdge = _floor.IsFloor(x - 1, y) || _floor.IsFloor(x + 1, y)
                           || _floor.IsFloor(x, y - 1) || _floor.IsFloor(x, y + 1);
                if (!isEdge) continue;

                int ax = wallIdx % wallBlockVariants;
                _tileMap.SetCell(new Vector2I(x, y), wallSrcId, new Vector2I(ax, 0));
                wallIdx = (wallIdx + 1) % wallBlockVariants;
            }
        }
    }

    // ==================== ENEMY SPAWNING ====================

    private void SpawnEnemies(int floorNumber)
    {
        var rng = new Random(floorNumber * 7919 + 13);

        foreach (var room in _floor.Rooms)
        {
            if (room.Kind == RoomKind.Entrance || room.Kind == RoomKind.Exit ||
                room.Kind == RoomKind.Treasure)
                continue;

            if (room.Kind == RoomKind.Boss)
            {
                // Boss room: 1 tier 3 enemy with 3x HP
                SpawnBossEnemy(room, rng);
                continue;
            }

            // Normal/Challenge rooms: 1 + floor/10 enemies, capped at 3
            int count = Math.Min(3, 1 + floorNumber / 10);
            for (int i = 0; i < count; i++)
            {
                SpawnRoomEnemy(room, floorNumber, rng);
            }
        }
    }

    private void SpawnRoomEnemy(RoomData room, int floorNumber, Random rng)
    {
        if (_tileMap == null) return;

        // Pick tier based on floor depth
        MonsterTier tier;
        if (floorNumber >= 20)
            tier = rng.Next(3) == 0 ? MonsterTier.Tier3 : (rng.Next(2) == 0 ? MonsterTier.Tier2 : MonsterTier.Tier1);
        else if (floorNumber >= 10)
            tier = rng.Next(2) == 0 ? MonsterTier.Tier2 : MonsterTier.Tier1;
        else
            tier = MonsterTier.Tier1;

        // Pick name
        string[] names = tier switch
        {
            MonsterTier.Tier1 => Tier1Names,
            MonsterTier.Tier2 => Tier2Names,
            MonsterTier.Tier3 => Tier3Names,
            _ => Tier1Names,
        };
        string name = names[rng.Next(names.Length)];

        bool canPoison = tier >= MonsterTier.Tier2 && rng.NextDouble() < 0.1;

        // Spawn via GameSystems
        var monsterData = GameSystems.SpawnMonster(name, tier, canPoison);

        // Assign archetype from pack composition
        var archetypes = new[] { MonsterArchetype.Melee, MonsterArchetype.Swarmer,
                                  MonsterArchetype.Ranged, MonsterArchetype.Bruiser };
        monsterData.Archetype = archetypes[rng.Next(archetypes.Length)];

        // Roll rarity and apply modifiers
        var rarity = MonsterSpawner.RollRarity(rng);
        monsterData.Rarity = rarity;
        float hpMult = MonsterSpawner.GetHPMultiplier(rarity);
        monsterData.HP = (int)(monsterData.HP * hpMult);
        monsterData.MaxHP = (int)(monsterData.MaxHP * hpMult);
        monsterData.XPReward = (int)(monsterData.XPReward * MonsterSpawner.GetRewardMultiplier(rarity));
        monsterData.GoldReward = (int)(monsterData.GoldReward * MonsterSpawner.GetRewardMultiplier(rarity));

        int modCount = MonsterSpawner.GetModifierCount(rarity, (floorNumber - 1) / 10 + 1);
        if (modCount > 0)
            monsterData.Modifiers = MonsterModifiers.RollModifiers(modCount, rng);

        // Random position within room bounds (on floor tiles)
        int tileX = room.X + rng.Next(room.Width);
        int tileY = room.Y + rng.Next(room.Height);
        // Ensure it's a floor tile
        if (!_floor.IsFloor(tileX, tileY))
        {
            tileX = room.CenterX;
            tileY = room.CenterY;
        }

        var worldPos = _tileMap.MapToLocal(new Vector2I(tileX, tileY));

        var enemy = new EnemyEntity();
        enemy.Init(monsterData, _astar, _tileMap);
        enemy.Position = worldPos;
        enemy.ZIndex = 15;
        _entityContainer.AddChild(enemy);
        _enemies.Add(enemy);
    }

    private void SpawnBossEnemy(RoomData room, Random rng)
    {
        if (_tileMap == null) return;

        string[] bossNames = { "Floor Guardian", "Dungeon Lord", "Shadow Beast" };
        string name = bossNames[rng.Next(bossNames.Length)];

        var monsterData = GameSystems.SpawnMonster(name, MonsterTier.Tier3);
        monsterData.Archetype = MonsterArchetype.Bruiser;
        monsterData.Rarity = MonsterRarity.Named;

        // 3x HP multiplier for boss
        monsterData.HP *= 3;
        monsterData.MaxHP *= 3;
        monsterData.XPReward *= 3;
        monsterData.GoldReward *= 3;

        var worldPos = _tileMap.MapToLocal(new Vector2I(room.CenterX, room.CenterY));

        var enemy = new EnemyEntity();
        enemy.Init(monsterData, _astar, _tileMap);
        enemy.Position = worldPos;
        enemy.ZIndex = 15;
        _entityContainer.AddChild(enemy);
        _enemies.Add(enemy);
        _bossEnemy = enemy;
    }

    // ==================== PHYSICS (WALL COLLISION) ====================

    public override void _PhysicsProcess(double delta)
    {
        if (_player == null || _tileMap == null || _floor == null) return;

        // Wall collision is now handled by TileMap physics layers + MoveAndSlide
        // (see FIX-02: TileSet has physics layer on wall tiles, player has mask=1)
    }

    // ==================== FRAME UPDATE ====================

    public override void _Process(double delta)
    {
        if (_floorTransitioning) return;

        // Attack cooldown
        if (_attackTimer > 0)
            _attackTimer -= (float)delta;

        // Floor announcement fade
        if (_floorAnnounceTimer > 0)
        {
            _floorAnnounceTimer -= (float)delta;
            if (_floorAnnounceTimer <= 0)
                _floorAnnounce.Visible = false;
        }

        // Player death check
        if (GameState.Player.IsDead && !_playerDead)
        {
            _playerDead = true;
            _deathTimer = 2.0f;
            ShowDeathOverlay();
        }

        if (_playerDead)
        {
            _deathTimer -= (float)delta;
            if (_deathTimer <= 0)
            {
                _playerDead = false;
                GameSystems.PlayerRespawn();
                SceneManager.Instance.GoToTown();
            }
            return;
        }

        // Update exploration + automap
        UpdateExploration();

        // Check floor exit
        CheckFloorExit();

        // Check entrance return (floor 1 only)
        CheckEntranceReturn();

        // Clean dead enemies from list
        CleanDeadEnemies();
    }

    // ==================== INPUT ====================

    public override void _UnhandledInput(InputEvent ev)
    {
        if (_playerDead || _floorTransitioning) return;

        if (ev.IsActionPressed("action_cross"))
        {
            // Check if near entrance on floor 1 for town return
            if (GameState.DungeonFloor == 1 && IsNearEntrance())
            {
                SceneManager.Instance.GoToTown();
                GetViewport().SetInputAsHandled();
                return;
            }

            // Attack nearest enemy
            if (_attackTimer <= 0)
            {
                TryAttack();
                GetViewport().SetInputAsHandled();
            }
        }

        if (ev.IsActionPressed("map_toggle"))
        {
            _automap?.CycleMode();
            GetViewport().SetInputAsHandled();
        }
    }

    // ==================== COMBAT ====================

    private void TryAttack()
    {
        if (_player == null) return;

        EnemyEntity nearest = null;
        float nearestDist = float.MaxValue;
        var playerPos = _player.GlobalPosition;

        foreach (var enemy in _enemies)
        {
            if (!IsInstanceValid(enemy)) continue;
            if (enemy.MonsterData == null || enemy.MonsterData.IsDead) continue;

            float dist = playerPos.DistanceTo(enemy.GlobalPosition);
            if (dist <= AttackRange && dist < nearestDist)
            {
                nearestDist = dist;
                nearest = enemy;
            }
        }

        if (nearest == null) return;

        // Attack
        var (damage, crit) = GameSystems.AttackMonster(nearest.MonsterData);
        nearest.ShowDamage(damage, crit);
        _attackTimer = AttackCooldown;

        // Slash effect
        TestHelper.ShowSlashEffect(this, nearest.GlobalPosition);

        // Game feel: screen shake on hit (stronger for crits)
        if (_camera != null)
        {
            float shakeIntensity = crit ? 4f : 2f;
            var shakeTween = CreateTween();
            for (int i = 0; i < 4; i++)
            {
                float decay = 1f - i / 4f;
                float x = (float)GD.RandRange(-shakeIntensity, shakeIntensity) * decay;
                float y = (float)GD.RandRange(-shakeIntensity, shakeIntensity) * decay;
                shakeTween.TweenProperty(_camera, "offset", new Vector2(x, y), 0.03f);
            }
            shakeTween.TweenProperty(_camera, "offset", Vector2.Zero, 0.03f);
        }

        // Game feel: brief hit pause on crit (freeze frame)
        if (crit)
        {
            Engine.TimeScale = 0.1;
            GetTree().CreateTimer(0.05f, true, false, true).Timeout += () => Engine.TimeScale = 1.0;
        }

        // Check if killed
        if (nearest.MonsterData.IsDead)
        {
            nearest.Die();
        }
    }

    // ==================== EXPLORATION ====================

    private void UpdateExploration()
    {
        if (_player == null || _tileMap == null || _floor == null) return;

        var tilePos = _player.GetTilePosition(_tileMap);

        // Only update if player moved to a new tile
        if (tilePos.X == _lastExploredX && tilePos.Y == _lastExploredY) return;

        _lastExploredX = tilePos.X;
        _lastExploredY = tilePos.Y;

        _floor.MarkExplored(tilePos.X, tilePos.Y, (int)ExploreRadius);
        _automap.SetPlayerPosition(tilePos.X, tilePos.Y);
    }

    // ==================== FLOOR EXIT ====================

    private void CheckFloorExit()
    {
        if (_player == null || _tileMap == null) return;

        var tilePos = _player.GetTilePosition(_tileMap);

        // Check Exit room
        if (_exitRoom != null && _exitRoom.Contains(tilePos.X, tilePos.Y))
        {
            // Check if player is near the center of the exit room
            float dist = _player.GlobalPosition.DistanceTo(
                _tileMap.MapToLocal(new Vector2I(_exitRoom.CenterX, _exitRoom.CenterY)));

            if (dist < ExitProximity)
            {
                _floorTransitioning = true;
                ShowFloorCompleteAndAdvance();
                return;
            }
        }

        // Check Boss room (only exit after boss is dead)
        if (_bossRoom != null && _bossRoom.Contains(tilePos.X, tilePos.Y))
        {
            bool bossAlive = _bossEnemy != null && IsInstanceValid(_bossEnemy) &&
                            _bossEnemy.MonsterData != null && !_bossEnemy.MonsterData.IsDead;

            if (!bossAlive)
            {
                float dist = _player.GlobalPosition.DistanceTo(
                    _tileMap.MapToLocal(new Vector2I(_bossRoom.CenterX, _bossRoom.CenterY)));

                if (dist < ExitProximity)
                {
                    _floorTransitioning = true;
                    ShowFloorCompleteAndAdvance();
                    return;
                }
            }
        }
    }

    private void CheckEntranceReturn()
    {
        if (GameState.DungeonFloor != 1) { _entrancePrompt.Visible = false; return; }

        bool near = IsNearEntrance();
        _entrancePrompt.Visible = near;
    }

    private bool IsNearEntrance()
    {
        if (_player == null || _tileMap == null) return false;

        var entranceRoom = _floor?.Rooms.Find(r => r.Kind == RoomKind.Entrance);
        if (entranceRoom == null) return false;

        var entranceWorld = _tileMap.MapToLocal(new Vector2I(entranceRoom.CenterX, entranceRoom.CenterY));
        return _player.GlobalPosition.DistanceTo(entranceWorld) < EntranceEdgeProximity;
    }

    // ==================== UI HELPERS ====================

    private void ShowFloorAnnouncement(int floorNumber)
    {
        _floorAnnounce.Text = $"Floor {floorNumber}";
        _floorAnnounce.Visible = true;
        _floorAnnounce.Modulate = new Color(1, 1, 1, 1);
        _floorAnnounceTimer = 2.0f;

        var tween = CreateTween();
        tween.TweenInterval(1.0);
        tween.TweenProperty(_floorAnnounce, "modulate:a", 0.0, 1.0);
    }

    private void ShowFloorCompleteAndAdvance()
    {
        _floorAnnounce.Text = "Floor Complete!";
        _floorAnnounce.Visible = true;
        _floorAnnounce.Modulate = new Color(1, 1, 1, 1);

        var tween = CreateTween();
        tween.TweenInterval(1.2);
        tween.TweenCallback(Callable.From(() =>
        {
            _floorAnnounce.Visible = false;
            LoadFloor(GameState.DungeonFloor + 1);
        }));
    }

    private void ShowDeathOverlay()
    {
        _floorAnnounce.Text = "You Died";
        _floorAnnounce.Visible = true;
        _floorAnnounce.Modulate = new Color(1, 0.3f, 0.3f, 1);
    }

    private void CleanDeadEnemies()
    {
        _enemies.RemoveAll(e => !IsInstanceValid(e) || (e.MonsterData != null && e.MonsterData.IsDead));
    }
}
