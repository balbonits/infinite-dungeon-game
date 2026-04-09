using Godot;
using System;
using System.Collections.Generic;
using DungeonGame.Dungeon;

public partial class TestDungeonGen : Node2D
{
    private const int GridW = 100;
    private const int GridH = 200;
    private const int TileW = 64;
    private const int TileH = 32;

    private Camera2D _camera;
    private Label _contentLabel;
    private TileMapLayer _tileMap;
    private int _seed;
    private int _floorNumber = 1;
    private int _viewMode = 1; // 1=full, 2=BSP only, 3=BSP+corridors
    private FloorData _floor;
    private readonly List<Label> _roomLabels = new();

    private static readonly int[] FloorCycle = { 1, 5, 10, 20 };
    private int _floorCycleIdx;

    // Room kind colors for labels
    private static readonly Dictionary<RoomKind, Color> KindColors = new()
    {
        { RoomKind.Normal, new Color(0.92f, 0.94f, 1.0f) },
        { RoomKind.Entrance, new Color(0.3f, 0.9f, 0.4f) },
        { RoomKind.Exit, new Color(0.9f, 0.3f, 0.3f) },
        { RoomKind.Boss, new Color(0.9f, 0.2f, 0.9f) },
        { RoomKind.Treasure, new Color(1.0f, 0.85f, 0.2f) },
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

        var panel = TestHelper.CreatePanel("DUNGEON GENERATOR", new Vector2(12, 12), new Vector2(360, 260));
        panel.GetNode<Label>("Content").Text =
            "Space: generate new floor (new seed)\n" +
            "1: full pipeline result\n" +
            "2: BSP only (no corridors/smoothing)\n" +
            "3: BSP + corridors (no smoothing)\n" +
            "F: cycle floor number (1/5/10/20)\n" +
            "Arrow keys: pan camera\n" +
            "+/-: zoom in/out\n" +
            "F12: screenshot | Esc: quit";
        ui.AddChild(panel);

        var infoPanel = TestHelper.CreatePanel("INFO", new Vector2(12, 284), new Vector2(360, 180));
        _contentLabel = infoPanel.GetNode<Label>("Content");
        ui.AddChild(infoPanel);

        _seed = (int)(Time.GetTicksMsec() & 0x7FFFFFFF);
        GenerateDungeon();
    }

    private void GenerateDungeon()
    {
        ClearMap();

        var rng = new Random(_seed);

        if (_viewMode == 1)
        {
            // Full pipeline
            var gen = new DungeonGenerator(GridW, GridH);
            _floor = gen.Generate(_seed, _floorNumber);
        }
        else if (_viewMode == 2)
        {
            // BSP only
            var bsp = new BspGenerator(GridW, GridH, rng);
            _floor = bsp.Generate();
            _floor.Seed = _seed;
            // Assign room types for visualization
            AssignTypes(rng);
        }
        else
        {
            // BSP + corridors, no smoothing
            var bsp = new BspGenerator(GridW, GridH, rng);
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
        if (floorTex == null)
        {
            GD.PrintErr("[DUNGEON] Could not load floor texture");
            return;
        }

        var tileSet = new TileSet();
        tileSet.TileShape = TileSet.TileShapeEnum.Isometric;
        tileSet.TileSize = new Vector2I(TileW, TileH);

        var source = new TileSetAtlasSource();
        source.Texture = floorTex;
        source.TextureRegionSize = new Vector2I(TileW, TileH);
        int sourceId = tileSet.AddSource(source);

        // Create atlas tiles from the sheet
        int atlasCols = floorTex.GetWidth() / TileW;
        int atlasRows = floorTex.GetHeight() / TileH;
        for (int ax = 0; ax < atlasCols; ax++)
            for (int ay = 0; ay < atlasRows; ay++)
            {
                var coords = new Vector2I(ax, ay);
                if (!source.HasTile(coords))
                    source.CreateTile(coords);
            }

        _tileMap = new TileMapLayer();
        _tileMap.TileSet = tileSet;
        _tileMap.YSortEnabled = true;
        AddChild(_tileMap);

        // Paint floor tiles only (walls remain empty/transparent)
        int tileIdx = 0;
        int totalVariants = atlasCols * atlasRows;
        for (int x = 0; x < GridW; x++)
        {
            for (int y = 0; y < GridH; y++)
            {
                if (_floor.Tiles[x, y] == TileType.Floor)
                {
                    int ax = tileIdx % atlasCols;
                    int ay = (tileIdx / atlasCols) % atlasRows;
                    _tileMap.SetCell(new Vector2I(x, y), sourceId, new Vector2I(ax, ay));
                    tileIdx = (tileIdx + 1) % totalVariants;
                }
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
        int normal = 0, entrance = 0, exit = 0, boss = 0, treasure = 0;
        foreach (var r in _floor.Rooms)
        {
            switch (r.Kind)
            {
                case RoomKind.Normal: normal++; break;
                case RoomKind.Entrance: entrance++; break;
                case RoomKind.Exit: exit++; break;
                case RoomKind.Boss: boss++; break;
                case RoomKind.Treasure: treasure++; break;
            }
        }

        _contentLabel.Text =
            $"Seed: {_seed}\n" +
            $"Floor: {_floorNumber}\n" +
            $"Rooms: {_floor.Rooms.Count}\n" +
            $"Types: {entrance} entrance, {exit} exit,\n" +
            $"  {normal} normal, {boss} boss, {treasure} treasure\n" +
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
        if (pan != Vector2.Zero) _camera.Position += pan;
    }
}
