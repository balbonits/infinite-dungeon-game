using Godot;
using System;
using System.Collections.Generic;
using DungeonGame.Dungeon;

public partial class TestBsp : Node2D
{
    private const int GridW = 100;
    private const int GridH = 200;
    private const int TilePixels = 4;

    private Camera2D _camera;
    private TextureRect _gridRect;
    private Label _contentLabel;
    private int _seed;
    private bool _showPartitions;
    private FloorData _floor;
    private BspGenerator _bsp;

    // Distinct colors for room outlines
    private static readonly Color[] RoomColors = {
        new(0.4f, 0.6f, 1.0f), new(1.0f, 0.5f, 0.3f), new(0.5f, 1.0f, 0.5f),
        new(1.0f, 0.8f, 0.2f), new(0.8f, 0.4f, 0.9f), new(0.3f, 0.9f, 0.9f),
        new(1.0f, 0.6f, 0.7f), new(0.6f, 0.8f, 0.4f), new(0.9f, 0.5f, 0.5f),
        new(0.5f, 0.5f, 1.0f), new(1.0f, 0.7f, 0.4f), new(0.7f, 1.0f, 0.6f),
    };

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

        var panel = TestHelper.CreatePanel("BSP ROOM PLACEMENT", new Vector2(12, 12), new Vector2(320, 200));
        panel.GetNode<Label>("Content").Text =
            "Space: regenerate (new seed)\n" +
            "B: toggle BSP partition lines\n" +
            "Arrow keys: pan camera\n" +
            "+/-: zoom in/out\n" +
            "F12: screenshot | Esc: quit";
        ui.AddChild(panel);

        var infoPanel = TestHelper.CreatePanel("INFO", new Vector2(12, 224), new Vector2(320, 120));
        _contentLabel = infoPanel.GetNode<Label>("Content");
        ui.AddChild(infoPanel);

        _seed = (int)(Time.GetTicksMsec() & 0x7FFFFFFF);
        GenerateBsp();
    }

    private void GenerateBsp()
    {
        var rng = new Random(_seed);
        _bsp = new BspGenerator(GridW, GridH, rng);
        _floor = _bsp.Generate();

        RenderGrid();
        UpdateInfo();
        GD.Print($"[BSP] Seed={_seed}, Rooms={_floor.Rooms.Count}, Grid={GridW}x{GridH}");
    }

    private void RenderGrid()
    {
        int imgW = GridW * TilePixels;
        int imgH = GridH * TilePixels;
        var img = Image.CreateEmpty(imgW, imgH, false, Image.Format.Rgba8);

        // Draw tiles
        var wallColor = new Color(30 / 255f, 30 / 255f, 35 / 255f);
        var floorColor = new Color(180 / 255f, 190 / 255f, 200 / 255f);

        for (int x = 0; x < GridW; x++)
        {
            for (int y = 0; y < GridH; y++)
            {
                var c = _floor.Tiles[x, y] == TileType.Floor ? floorColor : wallColor;
                FillTile(img, x, y, c);
            }
        }

        // Draw room outlines
        for (int i = 0; i < _floor.Rooms.Count; i++)
        {
            var room = _floor.Rooms[i];
            Color outlineColor;

            if (room.Kind == RoomKind.Entrance)
                outlineColor = new Color(0.2f, 0.9f, 0.3f);
            else if (room.Kind == RoomKind.Exit)
                outlineColor = new Color(0.9f, 0.2f, 0.2f);
            else
                outlineColor = RoomColors[i % RoomColors.Length];

            DrawRoomOutline(img, room, outlineColor);
        }

        // Draw BSP partition lines if toggled
        if (_showPartitions && _bsp.Root != null)
            DrawPartitions(img, _bsp.Root);

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

    private void DrawRoomOutline(Image img, RoomData room, Color c)
    {
        int x0 = room.X * TilePixels;
        int y0 = room.Y * TilePixels;
        int x1 = (room.X + room.Width) * TilePixels - 1;
        int y1 = (room.Y + room.Height) * TilePixels - 1;
        int imgW = img.GetWidth();
        int imgH = img.GetHeight();

        for (int px = x0; px <= x1; px++)
        {
            if (px >= 0 && px < imgW)
            {
                if (y0 >= 0 && y0 < imgH) img.SetPixel(px, y0, c);
                if (y0 + 1 >= 0 && y0 + 1 < imgH) img.SetPixel(px, y0 + 1, c);
                if (y1 >= 0 && y1 < imgH) img.SetPixel(px, y1, c);
                if (y1 - 1 >= 0 && y1 - 1 < imgH) img.SetPixel(px, y1 - 1, c);
            }
        }
        for (int py = y0; py <= y1; py++)
        {
            if (py >= 0 && py < imgH)
            {
                if (x0 >= 0 && x0 < imgW) img.SetPixel(x0, py, c);
                if (x0 + 1 >= 0 && x0 + 1 < imgW) img.SetPixel(x0 + 1, py, c);
                if (x1 >= 0 && x1 < imgW) img.SetPixel(x1, py, c);
                if (x1 - 1 >= 0 && x1 - 1 < imgW) img.SetPixel(x1 - 1, py, c);
            }
        }
    }

    private void DrawPartitions(Image img, BspNode node)
    {
        if (node.Left == null || node.Right == null) return;

        var yellow = new Color(0.9f, 0.9f, 0.2f, 0.9f);
        int imgW = img.GetWidth();
        int imgH = img.GetHeight();

        // Determine split boundary from children
        bool horizontal = node.Left.Width == node.Width;

        if (horizontal)
        {
            int splitY = node.Left.Y + node.Left.Height;
            int py = splitY * TilePixels;
            if (py >= 0 && py < imgH)
            {
                for (int px = node.X * TilePixels; px < (node.X + node.Width) * TilePixels && px < imgW; px++)
                {
                    // Dashed: 6 on, 4 off
                    if ((px / 6) % 2 == 0)
                        img.SetPixel(px, py, yellow);
                }
            }
        }
        else
        {
            int splitX = node.Left.X + node.Left.Width;
            int px = splitX * TilePixels;
            if (px >= 0 && px < imgW)
            {
                for (int py = node.Y * TilePixels; py < (node.Y + node.Height) * TilePixels && py < imgH; py++)
                {
                    if ((py / 6) % 2 == 0)
                        img.SetPixel(px, py, yellow);
                }
            }
        }

        DrawPartitions(img, node.Left);
        DrawPartitions(img, node.Right);
    }

    private void UpdateInfo()
    {
        int entranceCount = 0, exitCount = 0;
        foreach (var r in _floor.Rooms)
        {
            if (r.Kind == RoomKind.Entrance) entranceCount++;
            if (r.Kind == RoomKind.Exit) exitCount++;
        }
        _contentLabel.Text =
            $"Seed: {_seed}\n" +
            $"Rooms: {_floor.Rooms.Count}\n" +
            $"Grid: {GridW} x {GridH}\n" +
            $"Partitions: {(_showPartitions ? "ON" : "OFF")}";
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (ev is InputEventKey key && key.Pressed)
        {
            switch (key.Keycode)
            {
                case Key.Space:
                    _seed = (int)(Time.GetTicksMsec() & 0x7FFFFFFF);
                    GenerateBsp();
                    break;
                case Key.B:
                    _showPartitions = !_showPartitions;
                    RenderGrid();
                    UpdateInfo();
                    GD.Print($"[BSP] Partitions: {(_showPartitions ? "ON" : "OFF")}");
                    break;
                case Key.Equal:
                    _camera.Zoom *= 1.25f;
                    break;
                case Key.Minus:
                    _camera.Zoom /= 1.25f;
                    break;
                case Key.F12:
                    TestHelper.CaptureScreenshot(this, $"bsp_seed{_seed}");
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
