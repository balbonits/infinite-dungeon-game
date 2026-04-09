using Godot;
using System;
using System.Collections.Generic;
using DungeonGame.Dungeon;

public partial class TestDungeonGen : Node2D
{
    private const int TileW = 64;
    private const int TileH = 32;
    private int _gridW;
    private int _gridH;

    private Camera2D _camera;
    private Label _contentLabel;
    private TileMapLayer _tileMap;
    private int _seed;
    private int _floorNumber = 1;
    private int _viewMode = 1; // 1=full, 2=BSP only, 3=BSP+corridors
    private FloorData _floor;
    private readonly List<Label> _roomLabels = new();
    private DungeonGame.Automap _automap;

    private static readonly int[] FloorCycle = { 1, 5, 10, 11, 20, 30, 50, 100 };
    private int _floorCycleIdx;

    // Room kind colors for labels
    private static readonly Dictionary<RoomKind, Color> KindColors = new()
    {
        { RoomKind.Normal, new Color(0.92f, 0.94f, 1.0f) },
        { RoomKind.Entrance, new Color(0.3f, 0.9f, 0.4f) },
        { RoomKind.Exit, new Color(0.9f, 0.3f, 0.3f) },
        { RoomKind.Boss, new Color(0.9f, 0.2f, 0.9f) },
        { RoomKind.Treasure, new Color(1.0f, 0.85f, 0.2f) },
        { RoomKind.Challenge, new Color(1.0f, 0.5f, 0.0f) },
    };

    public override void _Ready()
    {
        var bg = new ColorRect();
        bg.Color = new Color(0.08f, 0.08f, 0.1f);
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var bgLayer = new CanvasLayer { Layer = -1 };
        bgLayer.AddChild(bg);
        AddChild(bgLayer);

        _camera = GetNode<Camera2D>("Camera2D");
        _camera.Zoom = new Vector2(0.5f, 0.5f);

        // UI
        var ui = new CanvasLayer();
        AddChild(ui);

        var panel = TestHelper.CreatePanel("DUNGEON GENERATOR", new Vector2(12, 12), new Vector2(360, 280));
        panel.GetNode<Label>("Content").Text =
            "Space: generate new floor (new seed)\n" +
            "1: full pipeline result\n" +
            "2: BSP only (no corridors/smoothing)\n" +
            "3: BSP + corridors (no smoothing)\n" +
            "F: cycle floor number (1/5/10/20)\n" +
            "M: cycle map overlay (off/overlay/full)\n" +
            "Arrow keys: pan camera (full map pan)\n" +
            "+/-: zoom in/out\n" +
            "F12: screenshot | Esc: quit";
        ui.AddChild(panel);

        // Automap overlay (on its own CanvasLayer so it renders above everything)
        var automapLayer = new CanvasLayer { Layer = 10 };
        AddChild(automapLayer);
        _automap = new DungeonGame.Automap();
        _automap.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        automapLayer.AddChild(_automap);

        var infoPanel = TestHelper.CreatePanel("INFO", new Vector2(12, 284), new Vector2(360, 180));
        _contentLabel = infoPanel.GetNode<Label>("Content");
        ui.AddChild(infoPanel);

        _seed = (int)(Time.GetTicksMsec() & 0x7FFFFFFF);
        GenerateDungeon();
    }

    private void GenerateDungeon()
    {
        ClearMap();

        // Compute progressive floor size
        var (calcW, calcH) = DungeonGenerator.CalculateFloorSize(_floorNumber);
        _gridW = calcW;
        _gridH = calcH;

        var rng = new Random(_seed);

        if (_viewMode == 1)
        {
            // Full pipeline
            var gen = new DungeonGenerator();
            _floor = gen.Generate(_seed, _floorNumber);
        }
        else if (_viewMode == 2)
        {
            // BSP only
            var bsp = new BspGenerator(_gridW, _gridH, rng);
            _floor = bsp.Generate();
            _floor.Seed = _seed;
            AssignTypes(rng);
        }
        else
        {
            // BSP + corridors, no smoothing
            var bsp = new BspGenerator(_gridW, _gridH, rng);
            _floor = bsp.Generate();
            _floor.Seed = _seed;
            var pairs = bsp.GetSiblingPairs();
            var carver = new DrunkardWalkCarver(rng);
            carver.CarveCorridors(_floor, pairs);
            AssignTypes(rng);
        }

        BuildTileMap();
        AddRoomLabels();
        UpdateInfo();

        // Feed automap: mark all tiles explored for testing, set player at entrance
        if (_automap != null)
        {
            for (int x = 0; x < _floor.Width; x++)
                for (int y = 0; y < _floor.Height; y++)
                    _floor.Explored[x, y] = true;

            _automap.SetFloorData(_floor);

            var entrance = _floor.Rooms.Find(r => r.Kind == RoomKind.Entrance);
            if (entrance != null)
                _automap.SetPlayerPosition(entrance.CenterX, entrance.CenterY);
        }

        string modeName = _viewMode switch { 1 => "Full", 2 => "BSP only", 3 => "BSP+corridors", _ => "?" };
        GD.Print($"[DUNGEON] Seed={_seed}, Floor={_floorNumber}, Mode={modeName}, Rooms={_floor.Rooms.Count}");
    }

    private void AssignTypes(Random rng)
    {
        // Replicate DungeonGenerator's type assignment for consistent preview
        if (_floorNumber % 10 == 0 && _floorNumber > 0)
        {
            foreach (var room in _floor.Rooms)
            {
                if (room.Kind == RoomKind.Normal) { room.Kind = RoomKind.Boss; break; }
            }
        }
        else
        {
            foreach (var room in _floor.Rooms)
            {
                if (room.Kind == RoomKind.Normal && rng.NextDouble() < 0.05)
                { room.Kind = RoomKind.Treasure; break; }
            }
        }
    }

    private void ClearMap()
    {
        if (_tileMap != null) { _tileMap.QueueFree(); _tileMap = null; }
        foreach (var lbl in _roomLabels) lbl.QueueFree();
        _roomLabels.Clear();
    }

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
        int wallBlockVariants = wallCols; // only row 0 = full blocks

        _tileMap = new TileMapLayer();
        _tileMap.TileSet = tileSet;
        _tileMap.YSortEnabled = true;
        AddChild(_tileMap);

        // Paint floor tiles
        int floorVariants = floorCols * floorRows;
        int floorIdx = 0;
        for (int x = 0; x < _gridW; x++)
        {
            for (int y = 0; y < _gridH; y++)
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
        for (int x = 0; x < _gridW; x++)
        {
            for (int y = 0; y < _gridH; y++)
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

    private void AddRoomLabels()
    {
        if (_tileMap == null) return;

        foreach (var room in _floor.Rooms)
        {
            var cellPos = new Vector2I(room.CenterX, room.CenterY);
            var worldPos = _tileMap.MapToLocal(cellPos);

            var label = new Label();
            label.Text = $"{room.Kind}\n{room.Width}x{room.Height}";
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.AddThemeFontSizeOverride("font_size", 13);

            Color labelColor;
            if (!KindColors.TryGetValue(room.Kind, out labelColor))
                labelColor = new Color(0.92f, 0.94f, 1.0f);
            label.AddThemeColorOverride("font_color", labelColor);

            label.Position = worldPos - new Vector2(40, 16);
            label.ZIndex = 100;
            AddChild(label);
            _roomLabels.Add(label);
        }
    }

    private void UpdateInfo()
    {
        string modeName = _viewMode switch { 1 => "Full pipeline", 2 => "BSP only", 3 => "BSP + corridors", _ => "?" };

        // Count room types
        int normal = 0, entrance = 0, exit = 0, boss = 0, treasure = 0, challenge = 0;
        foreach (var r in _floor.Rooms)
        {
            switch (r.Kind)
            {
                case RoomKind.Normal: normal++; break;
                case RoomKind.Entrance: entrance++; break;
                case RoomKind.Exit: exit++; break;
                case RoomKind.Boss: boss++; break;
                case RoomKind.Treasure: treasure++; break;
                case RoomKind.Challenge: challenge++; break;
            }
        }

        _contentLabel.Text =
            $"Seed: {_seed}\n" +
            $"Floor: {_floorNumber}  Size: {_gridW}x{_gridH}\n" +
            $"Rooms: {_floor.Rooms.Count}\n" +
            $"Types: {entrance} entrance, {exit} exit,\n" +
            $"  {normal} normal, {boss} boss, {treasure} treasure,\n" +
            $"  {challenge} challenge\n" +
            $"View: {modeName}";
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (ev is InputEventKey key && key.Pressed)
        {
            switch (key.Keycode)
            {
                case Key.Space:
                    _seed = (int)(Time.GetTicksMsec() & 0x7FFFFFFF);
                    GenerateDungeon();
                    break;
                case Key.Key1:
                    _viewMode = 1;
                    GenerateDungeon();
                    break;
                case Key.Key2:
                    _viewMode = 2;
                    GenerateDungeon();
                    break;
                case Key.Key3:
                    _viewMode = 3;
                    GenerateDungeon();
                    break;
                case Key.F:
                    _floorCycleIdx = (_floorCycleIdx + 1) % FloorCycle.Length;
                    _floorNumber = FloorCycle[_floorCycleIdx];
                    GenerateDungeon();
                    GD.Print($"[DUNGEON] Floor number: {_floorNumber}");
                    break;
                case Key.M:
                    _automap?.CycleMode();
                    GD.Print($"[AUTOMAP] Mode: {_automap?.Mode}");
                    break;
                case Key.Equal:
                    _camera.Zoom *= 1.25f;
                    break;
                case Key.Minus:
                    _camera.Zoom /= 1.25f;
                    break;
                case Key.F12:
                    TestHelper.CaptureScreenshot(this, $"dungeon_seed{_seed}_f{_floorNumber}");
                    break;
                case Key.Escape:
                    GetTree().Quit();
                    break;
            }
        }
    }

    public override void _Process(double delta)
    {
        var pan = Vector2.Zero;
        float speed = 400f;
        if (Input.IsKeyPressed(Key.Up)) pan.Y -= speed * (float)delta;
        if (Input.IsKeyPressed(Key.Down)) pan.Y += speed * (float)delta;
        if (Input.IsKeyPressed(Key.Left)) pan.X -= speed * (float)delta;
        if (Input.IsKeyPressed(Key.Right)) pan.X += speed * (float)delta;

        if (pan != Vector2.Zero)
        {
            if (_automap != null && _automap.Mode == DungeonGame.AutomapMode.FullMap)
                _automap.Pan(pan);
            else
                _camera.Position += pan;
        }
    }
}
