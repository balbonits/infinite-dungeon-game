using Godot;
using System;
using System.Collections.Generic;
using DungeonGame.Dungeon;

public partial class TestDrunkard : Node2D
{
    private const int GridW = 100;
    private const int GridH = 200;
    private const int TilePixels = 4;

    private Camera2D _camera;
    private TextureRect _gridRect;
    private Label _contentLabel;
    private int _seed;
    private bool _loopEnabled = true;

    // Step mode state
    private bool _stepMode;
    private int _stepIndex; // -1 = BSP only, 0..N-1 = agent corridors shown
    private FloorData _bspOnlyFloor;
    private BspGenerator _bsp;
    private DrunkardWalkCarver _carver;
    private List<(RoomData, RoomData)> _siblingPairs;

    // Current rendered data
    private FloorData _floor;
    private int _corridorTileCount;

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

        var panel = TestHelper.CreatePanel("DRUNKARD'S WALK", new Vector2(12, 12), new Vector2(340, 220));
        panel.GetNode<Label>("Content").Text =
            "Space: regenerate (new seed)\n" +
            "Tab: step mode (BSP, then each corridor)\n" +
            "L: toggle loop corridors\n" +
            "Arrow keys: pan camera\n" +
            "+/-: zoom in/out\n" +
            "F12: screenshot | Esc: quit";
        ui.AddChild(panel);

        var infoPanel = TestHelper.CreatePanel("INFO", new Vector2(12, 244), new Vector2(340, 140));
        _contentLabel = infoPanel.GetNode<Label>("Content");
        ui.AddChild(infoPanel);

        _seed = (int)(Time.GetTicksMsec() & 0x7FFFFFFF);
        GenerateFull();
    }

    private void GenerateFull()
    {
        _stepMode = false;
        _stepIndex = -1;

        var rng = new Random(_seed);
        _bsp = new BspGenerator(GridW, GridH, rng);
        var floor = _bsp.Generate();
        floor.Seed = _seed;

        _siblingPairs = _bsp.GetSiblingPairs();
        float loopChance = _loopEnabled ? 0.15f : 0f;
        _carver = new DrunkardWalkCarver(rng, 2000, 0.7f, loopChance);
        _carver.CarveCorridors(floor, _siblingPairs);

        // Save a BSP-only copy for step mode
        SaveBspSnapshot(new Random(_seed));

        _floor = floor;
        CountCorridorTiles();
        RenderGrid();
        UpdateInfo();
        GD.Print($"[DRUNKARD] Seed={_seed}, Rooms={_floor.Rooms.Count}, Corridors={_corridorTileCount}, Agents={_carver.AgentPaths.Count}, Loops={(_loopEnabled ? "ON" : "OFF")}");
    }

    private void SaveBspSnapshot(Random rng)
    {
        var bsp2 = new BspGenerator(GridW, GridH, rng);
        _bspOnlyFloor = bsp2.Generate();
    }

    private void CountCorridorTiles()
    {
        _corridorTileCount = 0;
        for (int x = 0; x < GridW; x++)
            for (int y = 0; y < GridH; y++)
                if (_floor.Tiles[x, y] == TileType.Floor && !_floor.IsInsideAnyRoom(x, y))
                    _corridorTileCount++;
    }

    private void StepForward()
    {
        if (_carver == null) return;

        if (!_stepMode)
        {
            _stepMode = true;
            _stepIndex = -1; // Show BSP only first
            RenderStepView();
            UpdateInfo();
            GD.Print("[DRUNKARD] Step mode: showing BSP only");
            return;
        }

        if (_stepIndex < _carver.AgentPaths.Count - 1)
        {
            _stepIndex++;
            RenderStepView();
            UpdateInfo();
            GD.Print($"[DRUNKARD] Step {_stepIndex + 1}/{_carver.AgentPaths.Count}");
        }
    }

    private void RenderStepView()
    {
        int imgW = GridW * TilePixels;
        int imgH = GridH * TilePixels;
        var img = Image.CreateEmpty(imgW, imgH, false, Image.Format.Rgba8);

        var wallColor = new Color(30 / 255f, 30 / 255f, 35 / 255f);
        var roomColor = new Color(140 / 255f, 170 / 255f, 210 / 255f);

        // Start with BSP rooms
        for (int x = 0; x < GridW; x++)
            for (int y = 0; y < GridH; y++)
            {
                if (_bspOnlyFloor.Tiles[x, y] == TileType.Floor)
                    FillTile(img, x, y, roomColor);
                else
                    FillTile(img, x, y, wallColor);
            }

        // Overlay agent paths up to current step
        if (_stepIndex >= 0)
        {
            var corridorColor = new Color(200 / 255f, 170 / 255f, 110 / 255f);
            for (int i = 0; i <= _stepIndex && i < _carver.AgentPaths.Count; i++)
            {
                foreach (var (px, py) in _carver.AgentPaths[i])
                {
                    if (!_bspOnlyFloor.IsInsideAnyRoom(px, py))
                        FillTile(img, px, py, corridorColor);
                }
            }
        }

        var tex = ImageTexture.CreateFromImage(img);
        _gridRect.Texture = tex;
    }

    private void RenderGrid()
    {
        int imgW = GridW * TilePixels;
        int imgH = GridH * TilePixels;
        var img = Image.CreateEmpty(imgW, imgH, false, Image.Format.Rgba8);

        var wallColor = new Color(30 / 255f, 30 / 255f, 35 / 255f);
        var roomColor = new Color(140 / 255f, 170 / 255f, 210 / 255f);
        var corridorColor = new Color(200 / 255f, 170 / 255f, 110 / 255f);

        for (int x = 0; x < GridW; x++)
        {
            for (int y = 0; y < GridH; y++)
            {
                if (_floor.Tiles[x, y] == TileType.Floor)
                {
                    if (_floor.IsInsideAnyRoom(x, y))
                        FillTile(img, x, y, roomColor);
                    else
                        FillTile(img, x, y, corridorColor);
                }
                else
                {
                    FillTile(img, x, y, wallColor);
                }
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
        string mode = _stepMode
            ? $"Step: {(_stepIndex < 0 ? "BSP only" : $"{_stepIndex + 1}/{_carver.AgentPaths.Count}")}"
            : "Full view";
        _contentLabel.Text =
            $"Seed: {_seed}\n" +
            $"Rooms: {_floor.Rooms.Count}\n" +
            $"Corridor tiles: {_corridorTileCount}\n" +
            $"Agents: {_carver.AgentPaths.Count}\n" +
            $"Loops: {(_loopEnabled ? "ON" : "OFF")} | {mode}";
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (ev is InputEventKey key && key.Pressed)
        {
            switch (key.Keycode)
            {
                case Key.Space:
                    _seed = (int)(Time.GetTicksMsec() & 0x7FFFFFFF);
                    GenerateFull();
                    break;
                case Key.Tab:
                    StepForward();
                    break;
                case Key.L:
                    _loopEnabled = !_loopEnabled;
                    GenerateFull();
                    GD.Print($"[DRUNKARD] Loop corridors: {(_loopEnabled ? "ON" : "OFF")}");
                    break;
                case Key.Equal:
                    _camera.Zoom *= 1.25f;
                    break;
                case Key.Minus:
                    _camera.Zoom /= 1.25f;
                    break;
                case Key.F12:
                    TestHelper.CaptureScreenshot(this, $"drunkard_seed{_seed}");
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
        if (Input.IsKeyPressed(Key.Left)) pan.X -= speed * (float)delta;
        if (Input.IsKeyPressed(Key.Right)) pan.X += speed * (float)delta;
        if (pan != Vector2.Zero) _camera.Position += pan;
    }
}
