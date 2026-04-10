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

        // Ground color background (matches floor tile edges so gaps aren't visible)
        var bg = new ColorRect();
        bg.Color = new Color(0.15f, 0.12f, 0.08f); // dark brown matching pebble tiles
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
        GD.Print($"[TOWN] Floor texture: {(floorTex != null ? $"{floorTex.GetWidth()}x{floorTex.GetHeight()}" : "NULL")}");
        GD.Print($"[TOWN] Wall texture: {(wallTex != null ? $"{wallTex.GetWidth()}x{wallTex.GetHeight()}" : "NULL")}");
        if (floorTex == null || wallTex == null)
        {
            GD.PrintErr("[TOWN] Could not load floor or wall texture — tiles will not render!");
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

        // Add physics layer for wall collision
        tileSet.AddPhysicsLayer();
        tileSet.SetPhysicsLayerCollisionLayer(0, 1);
        tileSet.SetPhysicsLayerCollisionMask(0, 0);

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
        GD.Print($"[TOWN] Floor atlas: {floorCols} cols x {floorRows} rows = {floorCols * floorRows} variants");
        int floorVariants = floorCols * floorRows;
        int floorIdx = 0;
        int floorCount = 0;
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
                    floorCount++;
                }
            }
        }
        GD.Print($"[TOWN] Painted {floorCount} floor tiles, {_townData.Width * _townData.Height} total cells");

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

    // Wall collision is handled by TileMap physics layers + MoveAndSlide (FIX-02)
    // No manual _PhysicsProcess wall checking needed.

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
    private NpcData _nearbyNpc;

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

        _nearbyNpc = nearestNpc;

        if (nearestNpc != null && !_npcPanel.Visible)
        {
            // Show prompt — don't open panel until player presses interact
            _entrancePrompt.Text = $"Press S to talk to {nearestNpc.Name}";
            _entrancePrompt.Visible = true;

            if (Input.IsActionJustPressed("action_cross"))
                _npcPanel.ShowNpc(nearestNpc);
        }
        else if (nearestNpc == null)
        {
            // Walked away — hide panel and prompt
            if (_npcPanel.Visible)
                _npcPanel.Hide();
            if (_entrancePrompt.Visible && !_entrancePrompt.Text.Contains("dungeon"))
                _entrancePrompt.Visible = false;
        }
        // Close panel on Circle (D key)
        if (_npcPanel.Visible && Input.IsActionJustPressed("action_circle"))
            _npcPanel.Hide();
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
