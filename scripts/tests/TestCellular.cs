using Godot;
using System;
using System.Collections.Generic;
using DungeonGame.Dungeon;

public partial class TestCellular : Node2D
{
    private const int GridW = 100;
    private const int GridH = 200;
    private const int TilePixels = 4;

    private Camera2D _camera;
    private TextureRect _gridRect;
    private Label _contentLabel;
    private int _seed;
    private int _currentIteration;
    private int _tilesChanged;

    // Snapshots for iteration stepping
    private TileType[,] _preCorridorTiles;
    private FloorData _baseFloor; // BSP + corridors, before any smoothing
    private BspGenerator _bsp;
    private DrunkardWalkCarver _carver;
    private List<(RoomData, RoomData)> _siblingPairs;
    private FloorData _currentFloor;

    public override void _Ready()
    {
        var bg = new ColorRect();
        bg.Color = new Color(0.12f, 0.12f, 0.15f);
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var bgLayer = new CanvasLayer { Layer = -1 };
        bgLayer.AddChild(bg);
        AddChild(bgLayer);

        _camera = GetNode<Camera2D>("Camera2D");
        _camera.Zoom = new Vector2(1.5f, 1.5f);

        _gridRect = new TextureRect();
        _gridRect.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
        AddChild(_gridRect);

        // UI
        var ui = new CanvasLayer();
        AddChild(ui);

        var panel = TestHelper.CreatePanel("CELLULAR AUTOMATA", new Vector2(12, 12), new Vector2(340, 200));
        panel.GetNode<Label>("Content").Text =
            "Space: regenerate (new seed)\n" +
            "Right/Left: step smoothing iterations\n" +
            "Arrow Up/Down: pan camera\n" +
            "+/-: zoom in/out\n" +
            "F12: screenshot | Esc: quit";
        ui.AddChild(panel);

        var infoPanel = TestHelper.CreatePanel("INFO", new Vector2(12, 224), new Vector2(340, 120));
        _contentLabel = infoPanel.GetNode<Label>("Content");
        ui.AddChild(infoPanel);

        _seed = (int)(Time.GetTicksMsec() & 0x7FFFFFFF);
        GenerateBase();
    }

    private void GenerateBase()
    {
        _currentIteration = 0;

        var rng = new Random(_seed);
        _bsp = new BspGenerator(GridW, GridH, rng);
        _baseFloor = _bsp.Generate();
        _baseFloor.Seed = _seed;

        _siblingPairs = _bsp.GetSiblingPairs();
        _carver = new DrunkardWalkCarver(rng);
        _carver.CarveCorridors(_baseFloor, _siblingPairs);

        // Save pre-smoothing snapshot
        _preCorridorTiles = new TileType[GridW, GridH];
        Array.Copy(_baseFloor.Tiles, _preCorridorTiles, _baseFloor.Tiles.Length);

        // Start at iteration 0 (no smoothing)
        ApplySmoothing(0);
        GD.Print($"[CELLULAR] Seed={_seed}, Rooms={_baseFloor.Rooms.Count}");
    }

    private void ApplySmoothing(int iterations)
    {
        _currentIteration = iterations;

        // Rebuild from pre-smoothing base
        _currentFloor = new FloorData(GridW, GridH);
        Array.Copy(_preCorridorTiles, _currentFloor.Tiles, _preCorridorTiles.Length);
        _currentFloor.Rooms = _baseFloor.Rooms;
        _currentFloor.Seed = _seed;

        if (iterations > 0)
        {
            var smoother = new CellularAutomata(iterations, 5);
            smoother.Smooth(_currentFloor);
        }

        // Count changed tiles
        _tilesChanged = 0;
        for (int x = 0; x < GridW; x++)
            for (int y = 0; y < GridH; y++)
                if (_currentFloor.Tiles[x, y] != _preCorridorTiles[x, y])
                    _tilesChanged++;

        RenderGrid();
        UpdateInfo();
        GD.Print($"[CELLULAR] Iteration={_currentIteration}, Changed={_tilesChanged}");
    }

    private void RenderGrid()
    {
        int imgW = GridW * TilePixels;
        int imgH = GridH * TilePixels;
        var img = Image.CreateEmpty(imgW, imgH, false, Image.Format.Rgba8);

        var wallColor = new Color(30 / 255f, 30 / 255f, 35 / 255f);
        var floorColor = new Color(180 / 255f, 190 / 255f, 200 / 255f);
        var changedColor = new Color(220 / 255f, 140 / 255f, 60 / 255f);

        for (int x = 0; x < GridW; x++)
        {
            for (int y = 0; y < GridH; y++)
            {
                bool changed = _currentFloor.Tiles[x, y] != _preCorridorTiles[x, y];
                var currentTile = _currentFloor.Tiles[x, y];

                Color c;
                if (changed)
                    c = changedColor;
                else if (currentTile == TileType.Floor)
                    c = floorColor;
                else
                    c = wallColor;

                FillTile(img, x, y, c);
            }
        }

        var tex = ImageTexture.CreateFromImage(img);
        _gridRect.Texture = tex;
    }

    private void FillTile(Image img, int tx, int ty, Color c)
    {
        int px = tx * TilePixels;
        int py = ty * TilePixels;
        for (int dx = 0; dx < TilePixels; dx++)
            for (int dy = 0; dy < TilePixels; dy++)
                img.SetPixel(px + dx, py + dy, c);
    }

    private void UpdateInfo()
    {
        _contentLabel.Text =
            $"Seed: {_seed}\n" +
            $"Smoothing iteration: {_currentIteration}\n" +
            $"Tiles changed: {_tilesChanged}";
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (ev is InputEventKey key && key.Pressed)
        {
            switch (key.Keycode)
            {
                case Key.Space:
                    _seed = (int)(Time.GetTicksMsec() & 0x7FFFFFFF);
                    GenerateBase();
                    break;
                case Key.Right:
                    if (_currentIteration < 10)
                        ApplySmoothing(_currentIteration + 1);
                    break;
                case Key.Left:
                    if (_currentIteration > 0)
                        ApplySmoothing(_currentIteration - 1);
                    break;
                case Key.Equal:
                    _camera.Zoom *= 1.25f;
                    break;
                case Key.Minus:
                    _camera.Zoom /= 1.25f;
                    break;
                case Key.F12:
                    TestHelper.CaptureScreenshot(this, $"cellular_seed{_seed}_iter{_currentIteration}");
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
        float speed = 300f;
        if (Input.IsKeyPressed(Key.Up)) pan.Y -= speed * (float)delta;
        if (Input.IsKeyPressed(Key.Down)) pan.Y += speed * (float)delta;
        if (pan != Vector2.Zero) _camera.Position += pan;
    }
}
