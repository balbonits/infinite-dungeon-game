using Godot;

public static class TestHelper
{
    public static Texture2D LoadPng(string resPath)
    {
        var diskPath = ProjectSettings.GlobalizePath(resPath);
        if (!System.IO.File.Exists(diskPath))
        {
            GD.PrintErr($"  File not found: {diskPath}");
            return null;
        }
        var img = new Image();
        var err = img.Load(diskPath);
        if (err != Error.Ok)
        {
            GD.PrintErr($"  Failed to load: {diskPath} ({err})");
            return null;
        }
        return ImageTexture.CreateFromImage(img);
    }

    public static Panel CreateStyledPanel(string title, Vector2 position, Vector2 size)
    {
        var panel = new Panel();
        panel.Position = position;
        panel.Size = size;

        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.086f, 0.106f, 0.157f, 0.9f);
        style.BorderColor = new Color(0.961f, 0.784f, 0.420f, 0.4f);
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(8);
        panel.AddThemeStyleboxOverride("panel", style);

        var titleLabel = new Label();
        titleLabel.Text = title;
        titleLabel.Position = new Vector2(14, 6);
        titleLabel.AddThemeColorOverride("font_color", new Color(0.961f, 0.784f, 0.420f));
        titleLabel.AddThemeFontSizeOverride("font_size", 13);
        panel.AddChild(titleLabel);

        var sep = new ColorRect();
        sep.Color = new Color(0.961f, 0.784f, 0.420f, 0.3f);
        sep.Position = new Vector2(10, 26);
        sep.Size = new Vector2(size.X - 20, 1);
        panel.AddChild(sep);

        var content = new Label();
        content.Name = "Content";
        content.Position = new Vector2(14, 34);
        content.Size = new Vector2(size.X - 28, size.Y - 46);
        content.AddThemeColorOverride("font_color", new Color(0.925f, 0.941f, 1.0f));
        content.AddThemeFontSizeOverride("font_size", 12);
        panel.AddChild(content);

        return panel;
    }

    public static void CaptureScreenshot(Node node, string name)
    {
        var img = node.GetViewport().GetTexture().GetImage();
        var dir = "res://docs/evidence/iso-tests/";
        var diskDir = ProjectSettings.GlobalizePath(dir);
        if (!DirAccess.DirExistsAbsolute(diskDir))
            DirAccess.MakeDirRecursiveAbsolute(diskDir);
        var path = diskDir + name + ".png";
        img.SavePng(path);
        GD.Print($"[SCREENSHOT] Saved: {path}");
    }

    /// <summary>Load PNG and replace magenta (#FF00FF) with transparent. For ISS sprites.</summary>
    public static Texture2D LoadIssPng(string resPath)
    {
        var diskPath = ProjectSettings.GlobalizePath(resPath);
        if (!System.IO.File.Exists(diskPath)) { GD.PrintErr($"  File not found: {diskPath}"); return null; }
        var img = new Image();
        if (img.Load(diskPath) != Error.Ok) { GD.PrintErr($"  Failed to load: {diskPath}"); return null; }
        for (int y = 0; y < img.GetHeight(); y++)
            for (int x = 0; x < img.GetWidth(); x++)
            {
                var c = img.GetPixel(x, y);
                if (c.R8 >= 240 && c.G8 <= 15 && c.B8 >= 240)
                    img.SetPixel(x, y, new Color(0, 0, 0, 0));
            }
        return ImageTexture.CreateFromImage(img);
    }

    /// <summary>
    /// Build a small isometric floor grid as a scale reference.
    /// Returns the TileMapLayer positioned at the given screen location.
    /// The floor texture is loaded via LoadIssPng to strip magenta.
    /// </summary>
    public static TileMapLayer CreateFloorGrid(Node parent, Vector2 position, int cols = 6, int rows = 4)
    {
        const int TileW = 64;
        const int TileH = 32;
        var tex = LoadIssPng("res://assets/isometric/tiles/stone-soup/floors/floor_rect_gray.png");
        if (tex == null)
        {
            GD.PrintErr("[TestHelper] Could not load floor_rect_gray.png for grid");
            return null;
        }

        var tileSet = new TileSet();
        tileSet.TileShape = TileSet.TileShapeEnum.Isometric;
        tileSet.TileSize = new Vector2I(TileW, TileH);

        var source = new TileSetAtlasSource();
        source.Texture = tex;
        source.TextureRegionSize = new Vector2I(TileW, TileH);
        int sourceId = tileSet.AddSource(source);

        // The floor atlas is 192x64 = 3 cols x 2 rows of 64x32 tiles
        int atlasCols = tex.GetWidth() / TileW;
        int atlasRows = tex.GetHeight() / TileH;
        for (int ax = 0; ax < atlasCols; ax++)
            for (int ay = 0; ay < atlasRows; ay++)
            {
                var coords = new Vector2I(ax, ay);
                if (!source.HasTile(coords))
                    source.CreateTile(coords);
            }

        var map = new TileMapLayer();
        map.TileSet = tileSet;
        map.Position = position;

        // Paint the grid, cycling through atlas tiles
        int tileIdx = 0;
        for (int x = 0; x < cols; x++)
            for (int y = 0; y < rows; y++)
            {
                int ax = tileIdx % atlasCols;
                int ay = (tileIdx / atlasCols) % atlasRows;
                map.SetCell(new Vector2I(x, y), sourceId, new Vector2I(ax, ay));
                tileIdx++;
            }

        parent.AddChild(map);
        return map;
    }

    public static void ShowFloatingText(Node parent, Vector2 worldPos, string text, Color color)
    {
        var label = new Label();
        label.Text = text;
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeFontSizeOverride("font_size", 16);
        label.Position = worldPos - new Vector2(20, 20);
        label.ZIndex = 100;
        parent.AddChild(label);

        var tween = parent.CreateTween();
        tween.TweenProperty(label, "position:y", worldPos.Y - 50, 0.9);
        tween.Parallel().TweenProperty(label, "modulate:a", 0.0, 0.9);
        tween.TweenCallback(Callable.From(label.QueueFree));
    }

    public static void ShowSlashEffect(Node parent, Vector2 pos)
    {
        var slash = new Polygon2D();
        slash.Polygon = new Vector2[] {
            new(-13, -2), new(13, -2), new(13, 2), new(-13, 2)
        };
        slash.Color = new Color(0.961f, 0.784f, 0.420f, 0.95f);
        slash.Position = pos;
        slash.Rotation = (float)GD.RandRange(-1.2, 1.2);
        slash.ZIndex = 50;
        parent.AddChild(slash);

        var tween = parent.CreateTween();
        tween.TweenProperty(slash, "modulate:a", 0.0, 0.15);
        tween.Parallel().TweenProperty(slash, "position:y", pos.Y - 8, 0.15);
        tween.TweenCallback(Callable.From(slash.QueueFree));
    }
}
