using Godot;
using System.Collections.Generic;
using DungeonGame.Town;
using DungeonGame.UI;

/// <summary>
/// Gameplay town scene. Renders the 30x30 isometric town grid,
/// places NPC markers and dungeon entrance, spawns the PlayerController,
/// and layers GameplayHud + PauseMenu on a CanvasLayer.
///
/// Tile rendering and NPC proximity logic follow TestTown2 patterns.
/// Key difference: uses PlayerController (CharacterBody2D + Input Map)
/// instead of raw key polling for movement.
/// </summary>
public partial class TownScene : Node2D
{
    private const int TileW = 64;
    private const int TileH = 32;
    private const float NpcProximityPx = 48f;

    private Camera2D _camera;
    private TileMapLayer _tileMap;
    private TownData _townData;
    private PlayerController _player;
    private NpcPanel _npcPanel;

    // NPC markers and their world positions
    private readonly List<(NpcData npc, Vector2 worldPos)> _npcMarkers = new();

    // Dungeon entrance
    private Vector2 _entranceWorldPos;
    private Label _entrancePrompt;

    public override void _Ready()
    {
        // Set game location
        GameState.Location = GameLocation.Town;

        // Dark background
        var bg = new ColorRect();
        bg.Color = new Color(0.08f, 0.08f, 0.1f);
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var bgLayer = new CanvasLayer { Layer = -1 };
        bgLayer.AddChild(bg);
        AddChild(bgLayer);

        _camera = GetNode<Camera2D>("Camera2D");

        // Build town
        _townData = TownLayout.Build();
        BuildTileMap();
        PlaceNpcMarkers();
        PlaceEntranceMarker();
        SpawnPlayer();

        // UI layer (CanvasLayer so it stays fixed on screen)
        var uiLayer = new CanvasLayer();
        AddChild(uiLayer);

        // GameplayHud
        var hud = new GameplayHud();
        uiLayer.AddChild(hud);

        // NPC interaction panel
        _npcPanel = new NpcPanel();
        uiLayer.AddChild(_npcPanel);

        // PauseMenu (initially hidden, toggled by "start" action via its own _UnhandledInput)
        var pauseMenu = new PauseMenu();
        uiLayer.AddChild(pauseMenu);

        // Dungeon entrance prompt (hidden by default)
        _entrancePrompt = new Label();
        _entrancePrompt.Text = "Press S to enter dungeon";
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

        GD.Print($"[TOWN] Town loaded: {_townData.Width}x{_townData.Height}, {_townData.Npcs.Count} NPCs");
    }

    /// <summary>
    /// Build the isometric TileMapLayer from TownData.
    /// Follows the exact same atlas setup as TestTown2.
    /// </summary>
    private void BuildTileMap()
    {
        var floorTex = TestHelper.LoadIssPng("res://assets/isometric/tiles/stone-soup/floors/floor_pebble_brown.png");
        var wallTex = TestHelper.LoadIssPng("res://assets/isometric/tiles/stone-soup/walls/brick_brown.png");
        if (floorTex == null || wallTex == null)
        {
            GD.PrintErr("[TOWN] Could not load floor or wall texture");
            return;
        }

        var tileSet = new TileSet();
        tileSet.TileShape = TileSet.TileShapeEnum.Isometric;
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

        // Source 1: Wall blocks (64x64 isometric cubes)
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

        _tileMap = new TileMapLayer();
        _tileMap.TileSet = tileSet;
        _tileMap.YSortEnabled = true;
        AddChild(_tileMap);

        // Paint floor tiles
        int floorVariants = floorCols * floorRows;
        int floorIdx = 0;
        for (int x = 0; x < _townData.Width; x++)
        {
            for (int y = 0; y < _townData.Height; y++)
            {
                if (_townData.Tiles[x, y] == TownTile.Floor)
                {
                    int ax = floorIdx % floorCols;
                    int ay = (floorIdx / floorCols) % floorRows;
                    _tileMap.SetCell(new Vector2I(x, y), floorSrcId, new Vector2I(ax, ay));
                    floorIdx = (floorIdx + 1) % floorVariants;
                }
            }
        }

        // Paint wall blocks
        int wallIdx = 0;
        for (int x = 0; x < _townData.Width; x++)
        {
            for (int y = 0; y < _townData.Height; y++)
            {
                if (_townData.Tiles[x, y] != TownTile.Wall) continue;
                int ax = wallIdx % wallBlockVariants;
                _tileMap.SetCell(new Vector2I(x, y), wallSrcId, new Vector2I(ax, 0));
                wallIdx = (wallIdx + 1) % wallBlockVariants;
            }
        }
    }

    /// <summary>
    /// Place colored circle markers and name labels for each NPC.
    /// Same visual approach as TestTown2.
    /// </summary>
    private void PlaceNpcMarkers()
    {
        if (_tileMap == null) return;

        var typeColors = new Dictionary<NpcType, Color>
        {
            { NpcType.Banker, new Color(1.0f, 0.85f, 0.2f) },
            { NpcType.Blacksmith, new Color(0.8f, 0.4f, 0.1f) },
            { NpcType.ItemShop, new Color(0.3f, 0.9f, 0.4f) },
            { NpcType.AdventureGuild, new Color(0.3f, 0.5f, 1.0f) },
            { NpcType.LevelTeleporter, new Color(0.8f, 0.3f, 0.9f) },
        };

        foreach (var npc in _townData.Npcs)
        {
            var cellPos = new Vector2I(npc.TileX, npc.TileY);
            var worldPos = _tileMap.MapToLocal(cellPos);

            // NPC circle marker
            var marker = new Polygon2D();
            var points = new Vector2[12];
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.Tau / 12;
                points[i] = new Vector2(Mathf.Cos(angle) * 18, Mathf.Sin(angle) * 12);
            }
            marker.Polygon = points;
            marker.Color = typeColors.GetValueOrDefault(npc.Type, new Color(1, 1, 1));
            marker.Position = worldPos;
            marker.ZIndex = 10;
            AddChild(marker);

            // NPC name label
            var label = new Label();
            label.Text = npc.Name;
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.AddThemeFontSizeOverride("font_size", 13);
            label.AddThemeColorOverride("font_color", typeColors.GetValueOrDefault(npc.Type, new Color(1, 1, 1)));
            label.Position = worldPos - new Vector2(50, 28);
            label.ZIndex = 100;
            AddChild(label);

            _npcMarkers.Add((npc, worldPos));
        }
    }

    /// <summary>
    /// Place the dungeon entrance marker (red diamond).
    /// Same visual approach as TestTown2.
    /// </summary>
    private void PlaceEntranceMarker()
    {
        if (_tileMap == null) return;

        var cellPos = new Vector2I(_townData.EntranceX, _townData.EntranceY);
        _entranceWorldPos = _tileMap.MapToLocal(cellPos);

        // Red diamond marker
        var marker = new Polygon2D();
        marker.Polygon = new Vector2[]
        {
            new(0, -16), new(20, 0), new(0, 16), new(-20, 0)
        };
        marker.Color = new Color(0.9f, 0.2f, 0.2f);
        marker.Position = _entranceWorldPos;
        marker.ZIndex = 10;
        AddChild(marker);

        var label = new Label();
        label.Text = "Dungeon Entrance";
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.AddThemeFontSizeOverride("font_size", 10);
        label.AddThemeColorOverride("font_color", new Color(0.9f, 0.3f, 0.3f));
        label.Position = _entranceWorldPos - new Vector2(55, 20);
        label.ZIndex = 100;
        AddChild(label);
    }

    /// <summary>
    /// Instantiate PlayerController at the town spawn point.
    /// Reparent the Camera2D to follow the player.
    /// </summary>
    private void SpawnPlayer()
    {
        if (_tileMap == null) return;

        var spawnCell = new Vector2I(_townData.SpawnX, _townData.SpawnY);
        var spawnWorld = _tileMap.MapToLocal(spawnCell);

        _player = new PlayerController();
        _player.Position = spawnWorld;
        _player.ZIndex = 20;
        AddChild(_player);

        // Reparent camera to follow the player
        _camera.GetParent().RemoveChild(_camera);
        _player.AddChild(_camera);
        _camera.Position = Vector2.Zero; // camera sits at player origin
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_player == null || _tileMap == null) return;

        // Wall collision: after PlayerController moves via MoveAndSlide,
        // check if the new position is on a wall tile and push back.
        // This follows the same tile-check approach as TestTown2.
        var tileCoords = _player.GetTilePosition(_tileMap);

        bool onWall = false;
        if (tileCoords.X < 0 || tileCoords.X >= _townData.Width ||
            tileCoords.Y < 0 || tileCoords.Y >= _townData.Height)
        {
            onWall = true;
        }
        else if (_townData.Tiles[tileCoords.X, tileCoords.Y] != TownTile.Floor)
        {
            onWall = true;
        }

        if (onWall)
        {
            // Revert to the nearest floor tile center.
            // Find the last known good tile by checking the 4 cardinal neighbors.
            var pos = _tileMap.ToLocal(_player.GlobalPosition);
            var currentTile = _tileMap.LocalToMap(pos);

            // Try to nudge back by checking surrounding tiles
            Vector2I bestTile = new Vector2I(_townData.SpawnX, _townData.SpawnY);
            float bestDist = float.MaxValue;

            Vector2I[] neighbors = {
                currentTile + new Vector2I(1, 0),
                currentTile + new Vector2I(-1, 0),
                currentTile + new Vector2I(0, 1),
                currentTile + new Vector2I(0, -1),
                currentTile + new Vector2I(1, 1),
                currentTile + new Vector2I(-1, -1),
                currentTile + new Vector2I(1, -1),
                currentTile + new Vector2I(-1, 1),
            };

            foreach (var n in neighbors)
            {
                if (n.X >= 0 && n.X < _townData.Width &&
                    n.Y >= 0 && n.Y < _townData.Height &&
                    _townData.Tiles[n.X, n.Y] == TownTile.Floor)
                {
                    var nWorld = _tileMap.MapToLocal(n);
                    float dist = pos.DistanceTo(nWorld);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestTile = n;
                    }
                }
            }

            _player.GlobalPosition = _tileMap.ToGlobal(_tileMap.MapToLocal(bestTile));
        }
    }

    public override void _Process(double delta)
    {
        CheckNpcProximity();
        CheckEntranceProximity();
    }

    /// <summary>
    /// Check player distance to each NPC. Show NpcPanel for the nearest
    /// NPC within proximity range, or hide it if none are nearby.
    /// Same logic as TestTown2.
    /// </summary>
    private void CheckNpcProximity()
    {
        if (_player == null) return;

        NpcData nearestNpc = null;
        float nearestDist = float.MaxValue;
        var playerPos = _player.GlobalPosition;

        foreach (var (npc, worldPos) in _npcMarkers)
        {
            float dist = playerPos.DistanceTo(worldPos);
            if (dist < NpcProximityPx && dist < nearestDist)
            {
                nearestNpc = npc;
                nearestDist = dist;
            }
        }

        if (nearestNpc != null)
        {
            _npcPanel.ShowNpc(nearestNpc);
        }
        else
        {
            _npcPanel.Hide();
        }
    }

    /// <summary>
    /// Check player distance to dungeon entrance.
    /// Show prompt when close, enter dungeon on action_cross.
    /// </summary>
    private void CheckEntranceProximity()
    {
        if (_player == null) return;

        float dist = _player.GlobalPosition.DistanceTo(_entranceWorldPos);
        bool nearEntrance = dist < NpcProximityPx;

        _entrancePrompt.Visible = nearEntrance;

        if (nearEntrance && Input.IsActionJustPressed("action_cross"))
        {
            int floor = GameState.DungeonFloor > 0 ? 1 : 1;
            SceneManager.Instance.GoToDungeon(floor);
        }
    }
}
