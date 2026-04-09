using Godot;
using System.Collections.Generic;

/// <summary>
/// Unified entity viewer. Same animation/movement/display system for ALL entities.
/// Creatures and heroes only differ in config (sprite paths, frame layout, layers).
///
/// Controls: Up/Down = switch entity, Left/Right = animation,
/// 1-8 = direction, Space = auto-animate, Tab = toggle layers (hero),
/// +/- = zoom, F12 = screenshot, Esc = quit.
/// </summary>
public partial class TestEntity : Node2D
{
    [Export] public string StartEntity = "";

    // ── Shared animation definition ──────────────────────────────────────────
    private struct AnimDef
    {
        public string Name;
        public int[] Frames;
        public AnimDef(string name, params int[] frames) { Name = name; Frames = frames; }
    }

    // ── Entity configuration ─────────────────────────────────────────────────
    private struct EntityConfig
    {
        public string Name;
        public string[] SpritePaths;
        public int Hframes;
        public int Vframes;
        public AnimDef[] Anims;
        public float MainScale;
        public float GridScale;
        public bool HasLayers;
    }

    // ── Isometric tile scaling constants ─────────────────────────────────────
    // ISS floor tiles are 64x32. A humanoid entity should occupy roughly
    // 60-80% of a tile's width (38-48px) and stand slightly taller than one
    // tile height (48-64px). Frame sizes differ between creatures and heroes:
    //
    //   Creature sheets: 1024x1024, 8x8 grid  -> 128x128 per frame
    //   Hero sheets:     2048x1024, 32x8 grid  -> 64x128 per frame
    //
    // Target on-tile width ~40px for humanoids:
    //   Creature GridScale = 40 / 128 = 0.3125  (renders at ~40x40 px)
    //   Hero     GridScale = 40 / 64  = 0.625   (renders at ~40x80 px)
    //
    // Heroes are narrower per-frame (64px wide) so they need a higher scale
    // factor to reach the same on-screen width as creatures.
    private const float CreatureGridScale = 0.3125f;
    private const float HeroGridScale = 0.625f;

    // ── Creature anims (8 cols: stance, walk×3, attack×2, hit, dead) ────────
    private static readonly AnimDef[] CreatureAnims = {
        new("Stance", 0),
        new("Walk", 1, 2, 3),
        new("Attack", 4, 5),
        new("Hit", 6),
        new("Dead", 7),
    };

    // ── Hero anims (32 cols: stance×4, run×8, melee×4, block×2, hit+die×6, cast×4, shoot×4)
    private static readonly AnimDef[] HeroAnims = {
        new("Stance", 0, 1, 2, 3),
        new("Run", 4, 5, 6, 7, 8, 9, 10, 11),
        new("Melee", 12, 13, 14, 15),
        new("Block", 16, 17),
        new("Hit+Die", 18, 19, 20, 21, 22, 23),
        new("Cast", 24, 25, 26, 27),
        new("Shoot", 28, 29, 30, 31),
    };

    private static readonly string[] HeroLayers = { "clothes", "leather_armor", "steel_armor", "longsword", "shield", "male_head1" };
    private static readonly string[] HeroineLayers = { "clothes", "leather_armor", "steel_armor", "longsword", "shield", "head_long" };

    // ── State ────────────────────────────────────────────────────────────────
    private List<EntityConfig> _entities = new();
    private int _entityIndex;
    private int _direction;
    private int _animIndex = 1; // start on walk/run
    private int _frameInAnim;
    private float _animTimer;
    private bool _autoAnimate = true;
    private bool _isHeroine;
    private List<bool> _layerVisible = new() { true, false, false, false, false, true };

    // ── Display ──────────────────────────────────────────────────────────────
    private readonly List<Sprite2D> _mainSprites = new();
    private readonly List<Sprite2D> _gridSprites = new();
    private Camera2D _camera;
    private TileMapLayer _floorGrid;
    private Label _entityLabel;
    private Label _animLabel;
    private Label _dirLabel;
    private Label _infoLabel;
    private Label _layerLabel;

    public override void _Ready()
    {
        var bg = new ColorRect();
        bg.Color = new Color(0.12f, 0.12f, 0.15f);
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var bgLayer = new CanvasLayer { Layer = -1 };
        bgLayer.AddChild(bg);
        AddChild(bgLayer);

        _camera = GetNode<Camera2D>("Camera2D");

        // Build entity list — creatures first, then hero/heroine
        BuildEntityList();

        // Floor grid for scale reference
        _floorGrid = TestHelper.CreateFloorGrid(this, new Vector2(280, -40), 6, 4);
        if (_floorGrid != null) _floorGrid.ZIndex = -1;

        // UI
        var ui = new CanvasLayer();
        AddChild(ui);

        var helpPanel = TestHelper.CreatePanel("ENTITY VIEWER", new Vector2(12, 12), new Vector2(360, 220));
        helpPanel.Visible = true;
        helpPanel.GetNode<Label>("Content").Text =
            "Up/Down: switch entity\n" +
            "Left/Right: change animation\n" +
            "1-8: set direction\n" +
            "Space: toggle auto-animate\n" +
            "Tab: toggle layers (hero only)\n" +
            "+/-: zoom | F12: screenshot | Esc: quit";
        ui.AddChild(helpPanel);

        _entityLabel = new Label();
        _entityLabel.Position = new Vector2(12, 250);
        _entityLabel.AddThemeColorOverride("font_color", new Color(0.78f, 0.67f, 0.43f, 0.9f));
        _entityLabel.AddThemeFontSizeOverride("font_size", 15);
        ui.AddChild(_entityLabel);

        _animLabel = new Label();
        _animLabel.Position = new Vector2(12, 274);
        _animLabel.AddThemeColorOverride("font_color", new Color(0.92f, 0.94f, 1.0f));
        _animLabel.AddThemeFontSizeOverride("font_size", 13);
        ui.AddChild(_animLabel);

        _dirLabel = new Label();
        _dirLabel.Position = new Vector2(12, 294);
        _dirLabel.AddThemeColorOverride("font_color", new Color(0.92f, 0.94f, 1.0f));
        _dirLabel.AddThemeFontSizeOverride("font_size", 13);
        ui.AddChild(_dirLabel);

        _infoLabel = new Label();
        _infoLabel.Position = new Vector2(12, 314);
        _infoLabel.AddThemeColorOverride("font_color", new Color(0.92f, 0.94f, 1.0f));
        _infoLabel.AddThemeFontSizeOverride("font_size", 13);
        ui.AddChild(_infoLabel);

        _layerLabel = new Label();
        _layerLabel.Position = new Vector2(12, 340);
        _layerLabel.AddThemeColorOverride("font_color", new Color(0.92f, 0.94f, 1.0f));
        _layerLabel.AddThemeFontSizeOverride("font_size", 13);
        ui.AddChild(_layerLabel);

        // Find start entity
        int startIdx = 0;
        if (!string.IsNullOrEmpty(StartEntity))
        {
            for (int i = 0; i < _entities.Count; i++)
                if (_entities[i].Name.ToLower() == StartEntity.ToLower()) { startIdx = i; break; }
        }

        if (_entities.Count > 0) LoadEntity(startIdx);
    }

    private void BuildEntityList()
    {
        // Creatures (8x8 sheets, shared anims)
        var creatureDir = "res://assets/isometric/enemies/creatures/";
        var diskDir = ProjectSettings.GlobalizePath(creatureDir);
        if (DirAccess.DirExistsAbsolute(diskDir))
        {
            var dir = DirAccess.Open(diskDir);
            dir.ListDirBegin();
            string file;
            while ((file = dir.GetNext()) != "")
            {
                if (!file.EndsWith(".png") || file.StartsWith(".")) continue;
                var name = file.Replace(".png", "");
                _entities.Add(new EntityConfig {
                    Name = name,
                    SpritePaths = new[] { creatureDir + file },
                    Hframes = 8, Vframes = 8,
                    Anims = CreatureAnims,
                    MainScale = 2f, GridScale = CreatureGridScale,
                    HasLayers = false,
                });
            }
            dir.ListDirEnd();
        }
        _entities.Sort((a, b) => string.Compare(a.Name, b.Name));

        // Hero (32x8 sheets, layered, different anims)
        _entities.Add(new EntityConfig {
            Name = "Hero",
            SpritePaths = BuildHeroPaths(false),
            Hframes = 32, Vframes = 8,
            Anims = HeroAnims,
            MainScale = 3f, GridScale = HeroGridScale,
            HasLayers = true,
        });

        _entities.Add(new EntityConfig {
            Name = "Heroine",
            SpritePaths = BuildHeroPaths(true),
            Hframes = 32, Vframes = 8,
            Anims = HeroAnims,
            MainScale = 3f, GridScale = HeroGridScale,
            HasLayers = true,
        });

        GD.Print($"[ENTITY] Found {_entities.Count} entities ({_entities.Count - 2} creatures + hero + heroine)");
    }

    private string[] BuildHeroPaths(bool heroine)
    {
        var folder = heroine ? "heroine" : "hero";
        var layers = heroine ? HeroineLayers : HeroLayers;
        var paths = new string[layers.Length];
        for (int i = 0; i < layers.Length; i++)
            paths[i] = $"res://assets/isometric/characters/{folder}/{layers[i]}.png";
        return paths;
    }

    // ── Same load/display logic for ALL entities ─────────────────────────────
    private void LoadEntity(int index)
    {
        _entityIndex = index;
        _frameInAnim = 0;
        _animIndex = 1; // default to walk/run

        // Clear old sprites
        foreach (var s in _mainSprites) s.QueueFree();
        foreach (var s in _gridSprites) s.QueueFree();
        _mainSprites.Clear();
        _gridSprites.Clear();

        var cfg = _entities[index];

        // Load each sprite path (1 for creatures, N for hero layers)
        for (int i = 0; i < cfg.SpritePaths.Length; i++)
        {
            var tex = TestHelper.LoadPng(cfg.SpritePaths[i]);
            if (tex == null) { _mainSprites.Add(null); _gridSprites.Add(null); continue; }

            // Main display sprite
            var main = new Sprite2D();
            main.Texture = tex;
            main.Hframes = cfg.Hframes;
            main.Vframes = cfg.Vframes;
            main.TextureFilter = TextureFilterEnum.Nearest;
            main.Position = new Vector2(0, -20);
            main.Scale = new Vector2(cfg.MainScale, cfg.MainScale);
            main.Visible = !cfg.HasLayers || _layerVisible[i];
            AddChild(main);
            _mainSprites.Add(main);

            // Grid-scale sprite — positioned so feet align with the tile center.
            // Sprite2D origin is at the frame center. Shifting up by ~40% of the
            // scaled frame height places the character's feet on the tile diamond.
            var grid = new Sprite2D();
            grid.Texture = tex;
            grid.Hframes = cfg.Hframes;
            grid.Vframes = cfg.Vframes;
            grid.TextureFilter = TextureFilterEnum.Nearest;
            grid.Scale = new Vector2(cfg.GridScale, cfg.GridScale);
            grid.Visible = !cfg.HasLayers || _layerVisible[i];
            if (_floorGrid != null)
            {
                var tilePos = _floorGrid.MapToLocal(new Vector2I(3, 2));
                float frameH = tex.GetHeight() / (float)cfg.Vframes;
                float scaledH = frameH * cfg.GridScale;
                // Offset upward by ~40% of scaled height so feet sit on tile
                grid.Position = _floorGrid.Position + tilePos - new Vector2(0, scaledH * 0.4f);
            }
            AddChild(grid);
            _gridSprites.Add(grid);
        }

        UpdateFrame();
        UpdateLabels();
        GD.Print($"[ENTITY] Loaded: {cfg.Name} ({cfg.SpritePaths.Length} layer(s), {cfg.Hframes}x{cfg.Vframes}, {cfg.Anims.Length} anims)");
    }

    // ── Same animation system for ALL entities ───────────────────────────────
    public override void _Process(double delta)
    {
        if (_entities.Count == 0 || _mainSprites.Count == 0) return;

        var anims = _entities[_entityIndex].Anims;
        if (_autoAnimate && anims[_animIndex].Frames.Length > 1)
        {
            _animTimer += (float)delta;
            if (_animTimer >= 0.125f)
            {
                _animTimer = 0;
                _frameInAnim = (_frameInAnim + 1) % anims[_animIndex].Frames.Length;
                UpdateFrame();
            }
        }
    }

    // ── Same frame update for ALL entities ───────────────────────────────────
    private void UpdateFrame()
    {
        var cfg = _entities[_entityIndex];
        int col = cfg.Anims[_animIndex].Frames[_frameInAnim % cfg.Anims[_animIndex].Frames.Length];
        int frame = _direction * cfg.Hframes + col;

        for (int i = 0; i < _mainSprites.Count; i++)
            if (_mainSprites[i] != null) _mainSprites[i].Frame = frame;
        for (int i = 0; i < _gridSprites.Count; i++)
            if (_gridSprites[i] != null) _gridSprites[i].Frame = frame;
    }

    // ── Same input handling for ALL entities ─────────────────────────────────
    public override void _UnhandledInput(InputEvent ev)
    {
        if (ev is not InputEventKey key || !key.Pressed) return;

        var cfg = _entities.Count > 0 ? _entities[_entityIndex] : default;

        switch (key.Keycode)
        {
            // Switch entity
            case Key.Up:
                if (_entities.Count > 0) LoadEntity((_entityIndex - 1 + _entities.Count) % _entities.Count);
                break;
            case Key.Down:
                if (_entities.Count > 0) LoadEntity((_entityIndex + 1) % _entities.Count);
                break;

            // Switch animation
            case Key.Right:
                _animIndex = (_animIndex + 1) % cfg.Anims.Length;
                _frameInAnim = 0;
                UpdateFrame(); UpdateLabels();
                break;
            case Key.Left:
                _animIndex = (_animIndex - 1 + cfg.Anims.Length) % cfg.Anims.Length;
                _frameInAnim = 0;
                UpdateFrame(); UpdateLabels();
                break;

            // Direction (1-8)
            case Key.Key1: case Key.Key2: case Key.Key3: case Key.Key4:
            case Key.Key5: case Key.Key6: case Key.Key7: case Key.Key8:
                _direction = (int)key.Keycode - (int)Key.Key1;
                UpdateFrame(); UpdateLabels();
                break;

            // Toggle layers (hero only — same key works, no-ops on creatures)
            case Key.Tab:
                if (cfg.HasLayers)
                {
                    for (int i = 0; i < _layerVisible.Count; i++)
                    {
                        _layerVisible[i] = !_layerVisible[i];
                        if (i < _mainSprites.Count && _mainSprites[i] != null) _mainSprites[i].Visible = _layerVisible[i];
                        if (i < _gridSprites.Count && _gridSprites[i] != null) _gridSprites[i].Visible = _layerVisible[i];
                    }
                    UpdateLabels();
                }
                break;

            case Key.Space:
                _autoAnimate = !_autoAnimate;
                break;
            case Key.Equal: _camera.Zoom *= 1.25f; break;
            case Key.Minus: _camera.Zoom /= 1.25f; break;
            case Key.F12:
                TestHelper.CaptureScreenshot(this, $"entity_{cfg.Name.ToLower()}_{cfg.Anims[_animIndex].Name.ToLower()}_dir{_direction}");
                break;
            case Key.Escape: GetTree().Quit(); break;
        }
    }

    private void UpdateLabels()
    {
        var cfg = _entities[_entityIndex];
        var displayName = cfg.Name.Replace("_", " ");
        displayName = char.ToUpper(displayName[0]) + displayName[1..];
        _entityLabel.Text = $"{displayName}  [{_entityIndex + 1}/{_entities.Count}]";
        _animLabel.Text = $"Animation: {cfg.Anims[_animIndex].Name} ({cfg.Anims[_animIndex].Frames.Length} frames)";
        _dirLabel.Text = $"Direction: {_direction + 1}";
        _infoLabel.Text = $"{cfg.Hframes * (cfg.SpritePaths.Length > 0 ? 1 : 0)}×{cfg.Vframes} sheet, {cfg.SpritePaths.Length} layer(s), {cfg.Anims.Length} anims";

        if (cfg.HasLayers)
        {
            var layers = cfg.Name == "Heroine" ? HeroineLayers : HeroLayers;
            var lines = new List<string> { "Layers (Tab to toggle all):" };
            for (int i = 0; i < layers.Length; i++)
                lines.Add($"  {layers[i]}: {(_layerVisible[i] ? "ON" : "off")}");
            _layerLabel.Text = string.Join("\n", lines);
        }
        else
        {
            _layerLabel.Text = "";
        }
    }
}
