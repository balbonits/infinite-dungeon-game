using Godot;
using System.Collections.Generic;
using DungeonGame.Town;
using DungeonGame.UI;

public partial class TestTown2 : Node2D
{
    private const int TileW = 64;
    private const int TileH = 32;
    private const float PlayerSpeed = 120f;
    private const float NpcProximityPx = 48f;

    private Camera2D _camera;
    private TileMapLayer _tileMap;
    private TownData _townData;
    private Node2D _playerMarker;
    private Vector2 _playerWorldPos;
    private NpcPanel _npcPanel;
    private Label _infoLabel;

    // NPC markers and their world positions
    private readonly List<(NpcData npc, Vector2 worldPos, Label label)> _npcMarkers = new();

    // Dungeon entrance world position
    private Vector2 _entranceWorldPos;
    private Label _entranceLabel;

    public override void _Ready()
    {
        // Dark background
        var bg = new ColorRect();
        bg.Color = new Color(0.08f, 0.08f, 0.1f);
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var bgLayer = new CanvasLayer { Layer = -1 };
        bgLayer.AddChild(bg);
        AddChild(bgLayer);

        _camera = GetNode<Camera2D>("Camera2D");
        _camera.Zoom = new Vector2(2, 2);

        // Build town
        _townData = TownLayout.Build();
        BuildTileMap();
        PlaceNpcMarkers();
        PlaceEntranceMarker();
        SpawnPlayer();

        // UI layer
        var ui = new CanvasLayer();
        AddChild(ui);

        // Help panel
        var helpPanel = TestHelper.CreatePanel("TOWN", new Vector2(12, 12), new Vector2(260, 140));
        helpPanel.GetNode<Label>("Content").Text =
            "Arrow keys: move player\n" +
            "+/-: zoom in/out\n" +
            "Walk near NPCs to interact\n" +
            "F12: screenshot | Esc: quit";
        ui.AddChild(helpPanel);

        // Info panel
        var infoPanel = TestHelper.CreatePanel("PLAYER", new Vector2(12, 164), new Vector2(260, 80));
        _infoLabel = infoPanel.GetNode<Label>("Content");
        ui.AddChild(infoPanel);

        // NPC interaction panel (right side)
        _npcPanel = new NpcPanel();
        ui.AddChild(_npcPanel);

        UpdateInfoLabel();
        GD.Print($"[TOWN2] Town loaded: {_townData.Width}x{_townData.Height}, {_townData.Npcs.Count} NPCs");
    }

    private void BuildTileMap()
    {
        // Use cobblestone for town floors, brick for walls (matching dungeon gen pattern)
        var floorTex = TestHelper.LoadIssPng("res://assets/isometric/tiles/stone-soup/floors/floor_pebble_brown.png");
        var wallTex = TestHelper.LoadIssPng("res://assets/isometric/tiles/stone-soup/walls/brick_brown.png");
        if (floorTex == null || wallTex == null)
        {
            GD.PrintErr("[TOWN2] Could not load floor or wall texture");
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

    private void PlaceNpcMarkers()
    {
        if (_tileMap == null) return;

        // NPC type colors
        var typeColors = new Dictionary<NpcType, Color>
        {
            { NpcType.Banker, new Color(1.0f, 0.85f, 0.2f) },      // gold
            { NpcType.Blacksmith, new Color(0.8f, 0.4f, 0.1f) },   // orange
            { NpcType.ItemShop, new Color(0.3f, 0.9f, 0.4f) },     // green
            { NpcType.AdventureGuild, new Color(0.3f, 0.5f, 1.0f) }, // blue
            { NpcType.LevelTeleporter, new Color(0.8f, 0.3f, 0.9f) }, // purple
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
                points[i] = new Vector2(Mathf.Cos(angle) * 8, Mathf.Sin(angle) * 5);
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
            label.AddThemeFontSizeOverride("font_size", 10);
            label.AddThemeColorOverride("font_color", typeColors.GetValueOrDefault(npc.Type, new Color(1, 1, 1)));
            label.Position = worldPos - new Vector2(40, 20);
            label.ZIndex = 100;
            AddChild(label);

            _npcMarkers.Add((npc, worldPos, label));
        }
    }

    private void PlaceEntranceMarker()
    {
        if (_tileMap == null) return;

        var cellPos = new Vector2I(_townData.EntranceX, _townData.EntranceY);
        _entranceWorldPos = _tileMap.MapToLocal(cellPos);

        // Red diamond marker for dungeon entrance
        var marker = new Polygon2D();
        marker.Polygon = new Vector2[]
        {
            new(0, -8), new(10, 0), new(0, 8), new(-10, 0)
        };
        marker.Color = new Color(0.9f, 0.2f, 0.2f);
        marker.Position = _entranceWorldPos;
        marker.ZIndex = 10;
        AddChild(marker);

        _entranceLabel = new Label();
        _entranceLabel.Text = "Dungeon Entrance";
        _entranceLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _entranceLabel.AddThemeFontSizeOverride("font_size", 10);
        _entranceLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.3f, 0.3f));
        _entranceLabel.Position = _entranceWorldPos - new Vector2(55, 20);
        _entranceLabel.ZIndex = 100;
        AddChild(_entranceLabel);
    }

    private void SpawnPlayer()
    {
        if (_tileMap == null) return;

        var spawnCell = new Vector2I(_townData.SpawnX, _townData.SpawnY);
        _playerWorldPos = _tileMap.MapToLocal(spawnCell);

        _playerMarker = new Node2D();
        _playerMarker.ZIndex = 20;

        // Player body (bright circle)
        var body = new Polygon2D();
        var points = new Vector2[12];
        for (int i = 0; i < 12; i++)
        {
            float angle = i * Mathf.Tau / 12;
            points[i] = new Vector2(Mathf.Cos(angle) * 7, Mathf.Sin(angle) * 4.5f);
        }
        body.Polygon = points;
        body.Color = new Color(0.2f, 0.8f, 1.0f);
        _playerMarker.AddChild(body);

        // Player outline/head (smaller bright dot)
        var head = new Polygon2D();
        var headPts = new Vector2[8];
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.Tau / 8;
            headPts[i] = new Vector2(Mathf.Cos(angle) * 3, Mathf.Sin(angle) * 2 - 4);
        }
        head.Polygon = headPts;
        head.Color = new Color(0.4f, 0.9f, 1.0f);
        _playerMarker.AddChild(head);

        _playerMarker.Position = _playerWorldPos;
        AddChild(_playerMarker);

        _camera.Position = _playerWorldPos;
    }

    public override void _Process(double delta)
    {
        HandleMovement(delta);
        HandleCameraZoom();
        CheckNpcProximity();
        UpdateInfoLabel();
    }

    private void HandleMovement(double delta)
    {
        if (_tileMap == null || _playerMarker == null) return;

        // Arrow key polling for movement
        var input = Vector2.Zero;
        if (Input.IsKeyPressed(Key.Left)) input.X -= 1;
        if (Input.IsKeyPressed(Key.Right)) input.X += 1;
        if (Input.IsKeyPressed(Key.Up)) input.Y -= 1;
        if (Input.IsKeyPressed(Key.Down)) input.Y += 1;

        if (input == Vector2.Zero) return;

        input = input.Normalized();

        // Isometric movement: convert screen-space input to iso world movement
        // In isometric, "right" on screen maps to (+1, +1) in tile space
        // and "down" maps to (-1, +1) in tile space
        var isoInput = new Vector2(
            input.X - input.Y,
            (input.X + input.Y) * 0.5f
        );

        var newPos = _playerWorldPos + isoInput * PlayerSpeed * (float)delta;

        // Collision check: convert world pos to tile coords and verify walkable
        var tileCoords = _tileMap.LocalToMap(newPos);
        if (tileCoords.X >= 0 && tileCoords.X < _townData.Width &&
            tileCoords.Y >= 0 && tileCoords.Y < _townData.Height &&
            _townData.Tiles[tileCoords.X, tileCoords.Y] == TownTile.Floor)
        {
            _playerWorldPos = newPos;
            _playerMarker.Position = _playerWorldPos;
            _camera.Position = _playerWorldPos;
        }
    }

    private void HandleCameraZoom()
    {
        // Handled via input events to avoid repeat issues
    }

    private void CheckNpcProximity()
    {
        NpcData nearestNpc = null;
        float nearestDist = float.MaxValue;

        foreach (var (npc, worldPos, _) in _npcMarkers)
        {
            float dist = _playerWorldPos.DistanceTo(worldPos);
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

    private void UpdateInfoLabel()
    {
        if (_infoLabel == null || _tileMap == null) return;
        var tileCoords = _tileMap.LocalToMap(_playerWorldPos);
        _infoLabel.Text = $"Pos: ({tileCoords.X}, {tileCoords.Y})\nGold: {GameState.Player.Gold}g";
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (ev is InputEventKey key && key.Pressed)
        {
            switch (key.Keycode)
            {
                case Key.Equal:
                    _camera.Zoom *= 1.25f;
                    break;
                case Key.Minus:
                    _camera.Zoom /= 1.25f;
                    break;
                case Key.F12:
                    TestHelper.CaptureScreenshot(this, "town2_scene");
                    break;
                case Key.Escape:
                    GetTree().Quit();
                    break;
            }
        }
    }
}
