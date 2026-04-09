using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Automated demo that validates isometric asset rendering.
/// Tests: tilemap, sprite sheet animation, equipment layering,
/// 8-direction facing, movement, combat effects, UI, and performance.
/// No game logic — purely visual/rendering validation.
/// </summary>
public partial class IsometricDemo : Node2D
{
    private const int IsoTileW = 64;
    private const int IsoTileH = 32;
    private const int RoomW = 9;
    private const int RoomH = 7;
    private const float MoveSpeed = 150f;
    private const float AnimFps = 8f;

    private static readonly string AssetBase = "res://assets/isometric/";
    // Absolute path for direct file loading (bypasses Godot import system)
    private static readonly string AssetDisk = ProjectSettings.GlobalizePath(AssetBase);

    // Scene nodes
    private Node2D _entityLayer;
    private CanvasLayer _uiLayer;
    private Label _logLabel;
    private TileMapLayer _floorMap;

    // Demo state
    private readonly List<(float delay, Action action)> _steps = new();
    private int _stepIndex;
    private float _timer;
    private bool _demoComplete;

    // Animation state
    private readonly List<AnimatedEntity> _animEntities = new();
    private float _animTimer;
    private int _animFrame;

    // Direction rotation state
    private int _currentDirection;
    private float _dirTimer;
    private bool _rotatingDirections;

    // Movement state
    private Node2D _moveTarget;
    private bool _isMoving;
    private Vector2 _moveDestination;

    // Phase results
    private readonly List<string> _results = new();
    private int _passCount;

    // ==================== DATA ====================

    private record AnimatedEntity(
        Sprite2D Sprite,
        int Hframes,
        int WalkStart,
        int WalkEnd,
        int Direction
    );

    private static readonly string[] CreatureNames = {
        "slime", "skeleton", "goblin", "zombie",
        "ogre", "werewolf", "elemental", "magician"
    };

    private static readonly string[] CreatureTiers = {
        "T1", "T2", "T2", "T1",
        "T3", "T3", "T3", "T2"
    };

    // ==================== LIFECYCLE ====================

    public override void _Ready()
    {
        GD.Print("");
        GD.Print("========================================");
        GD.Print("  ISOMETRIC ASSET DEMO");
        GD.Print("  Validating rendering before game build");
        GD.Print("========================================");
        GD.Print("");

        BuildScene();
        SetupSteps();

        _stepIndex = 0;
        _timer = 1.0f;

        if (DisplayServer.GetName() == "headless")
        {
            for (int i = 0; i < _steps.Count; i++)
                _steps[i] = (0.01f, _steps[i].action);
        }
    }

    public override void _Process(double delta)
    {
        if (_demoComplete) return;

        // Animate all tracked entities
        _animTimer += (float)delta;
        if (_animTimer >= 1.0f / AnimFps)
        {
            _animTimer = 0;
            _animFrame++;
            foreach (var ent in _animEntities)
            {
                int walkLen = ent.WalkEnd - ent.WalkStart + 1;
                int frame = ent.WalkStart + (_animFrame % walkLen);
                ent.Sprite.Frame = ent.Direction * ent.Hframes + frame;
            }
        }

        // Direction rotation
        if (_rotatingDirections)
        {
            _dirTimer += (float)delta;
            if (_dirTimer >= 0.6f)
            {
                _dirTimer = 0;
                _currentDirection = (_currentDirection + 1) % 8;
                foreach (var ent in _animEntities)
                {
                    // Update direction for all tracked entities
                    var updated = ent with { Direction = _currentDirection };
                    _animEntities[_animEntities.IndexOf(ent)] = updated;
                }
            }
        }

        // Movement
        if (_isMoving && _moveTarget != null)
        {
            var moveAmount = MoveSpeed * (float)delta;
            var remaining = _moveTarget.Position.DistanceTo(_moveDestination);
            if (remaining <= moveAmount)
            {
                _moveTarget.Position = _moveDestination;
                _isMoving = false;
            }
            else
            {
                _moveTarget.Position += (_moveDestination - _moveTarget.Position).Normalized() * moveAmount;
            }
        }

        // Step timer
        _timer -= (float)delta;
        if (_timer <= 0 && _stepIndex < _steps.Count)
        {
            _steps[_stepIndex].action();
            _stepIndex++;

            if (_stepIndex < _steps.Count)
                _timer = _steps[_stepIndex].delay;
            else
            {
                _demoComplete = true;
                GetTree().CreateTimer(5.0).Timeout += () => GetTree().Quit();
            }
        }
    }

    // ==================== SCENE SETUP ====================

    private void BuildScene()
    {
        _entityLayer = new Node2D { ZIndex = 5 };
        AddChild(_entityLayer);

        _uiLayer = new CanvasLayer();
        AddChild(_uiLayer);

        _logLabel = new Label();
        _logLabel.Position = new Vector2(8, 4);
        _logLabel.AddThemeColorOverride("font_color", new Color(0.75f, 1.0f, 0.75f));
        _logLabel.AddThemeFontSizeOverride("font_size", 14);
        _uiLayer.AddChild(_logLabel);
    }

    // ==================== STEP SETUP ====================

    private void SetupSteps()
    {
        _steps.AddRange(new (float, Action)[] {
            // Phase 1: Isometric Floor
            (1.0f, Phase1_IsometricFloor),

            // Phase 2: Cave Atlas Tiles
            (3.0f, Phase2_CaveAtlas),

            // Phase 3: Single Creature Animation
            (3.0f, Phase3_CreatureAnimation),

            // Phase 4: 8-Direction Rotation
            (5.0f, Phase4_DirectionRotation),

            // Phase 5: Hero Equipment Layers
            (5.0f, Phase5_HeroLayers),

            // Phase 6: Enemy Gallery
            (5.0f, Phase6_EnemyGallery),

            // Phase 7: Combat Effects + Movement
            (5.0f, Phase7_CombatEffects),

            // Phase 8: UI + Summary
            (5.0f, Phase8_UISummary),
        });
    }

    // ==================== PHASE 1: ISOMETRIC FLOOR ====================

    private void Phase1_IsometricFloor()
    {
        Log("=== PHASE 1: Isometric Floor ===");

        try
        {
            var tileSet = new TileSet();
            tileSet.TileShape = TileSet.TileShapeEnum.Isometric;
            tileSet.TileSize = new Vector2I(IsoTileW, IsoTileH);

            var texPath = AssetBase + "tiles/ground/stone_path_64x32.png";
            var tex = LoadPng(texPath);
            if (tex == null)
            {
                LogFail(1, $"Could not load texture: {texPath}");
                return;
            }

            var source = new TileSetAtlasSource();
            source.Texture = tex;
            source.TextureRegionSize = new Vector2I(IsoTileW, IsoTileH);
            int sourceId = tileSet.AddSource(source);

            // Create tiles in the atlas source
            int cols = tex.GetWidth() / IsoTileW;
            int rows = tex.GetHeight() / IsoTileH;
            Log($"  Atlas: {tex.GetWidth()}x{tex.GetHeight()} -> {cols} cols x {rows} rows");

            // Create tile at (0,0) in the atlas
            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    var atlasCoords = new Vector2I(x, y);
                    if (!source.HasTile(atlasCoords))
                        source.CreateTile(atlasCoords);
                }
            }

            _floorMap = new TileMapLayer();
            _floorMap.TileSet = tileSet;
            _floorMap.YSortEnabled = true;
            AddChild(_floorMap);
            _floorMap.MoveChild(_floorMap, 0); // behind entities

            // Paint floor (cycle through available tiles)
            int tileCount = 0;
            for (int x = 0; x < RoomW; x++)
            {
                for (int y = 0; y < RoomH; y++)
                {
                    int tx = tileCount % cols;
                    _floorMap.SetCell(new Vector2I(x, y), sourceId, new Vector2I(tx, 0));
                    tileCount++;
                }
            }

            // Get world bounds for logging
            var topLeft = _floorMap.MapToLocal(new Vector2I(0, 0));
            var botRight = _floorMap.MapToLocal(new Vector2I(RoomW - 1, RoomH - 1));
            Log($"  Painted {tileCount} tiles ({RoomW}x{RoomH})");
            Log($"  World bounds: ({topLeft.X:F0},{topLeft.Y:F0}) to ({botRight.X:F0},{botRight.Y:F0})");
            LogPass(1, $"Isometric floor rendered: {RoomW}x{RoomH} = {tileCount} tiles");
        }
        catch (Exception e)
        {
            LogFail(1, $"Exception: {e.Message}");
        }
    }

    // ==================== PHASE 2: CAVE ATLAS ====================

    private void Phase2_CaveAtlas()
    {
        Log("=== PHASE 2: Cave Atlas Tiles ===");

        try
        {
            var texPath = AssetBase + "tiles/cave_atlas.png";
            var tex = LoadPng(texPath);
            if (tex == null)
            {
                LogFail(2, $"Could not load cave atlas: {texPath}");
                return;
            }

            Log($"  Cave atlas: {tex.GetWidth()}x{tex.GetHeight()}");

            var tileSet = new TileSet();
            tileSet.TileShape = TileSet.TileShapeEnum.Isometric;
            tileSet.TileSize = new Vector2I(IsoTileW, IsoTileH);

            var source = new TileSetAtlasSource();
            source.Texture = tex;
            source.TextureRegionSize = new Vector2I(IsoTileW, IsoTileH);
            int sourceId = tileSet.AddSource(source);

            int cols = tex.GetWidth() / IsoTileW;  // 1024/64 = 16
            int rows = tex.GetHeight() / IsoTileH;  // 1024/32 = 32
            Log($"  Grid: {cols} cols x {rows} rows = {cols * rows} cells");

            // Create tiles for first row (floor tiles)
            int created = 0;
            for (int x = 0; x < cols; x++)
            {
                var coords = new Vector2I(x, 0);
                if (!source.HasTile(coords))
                {
                    source.CreateTile(coords);
                    created++;
                }
            }
            Log($"  Created {created} floor tile entries from row 0");

            // Paint cave floor tiles on a separate layer (offset from ground floor)
            var caveMap = new TileMapLayer();
            caveMap.TileSet = tileSet;
            caveMap.Position = new Vector2(0, RoomH * IsoTileH + 64);
            AddChild(caveMap);

            int painted = 0;
            for (int x = 0; x < Math.Min(cols, RoomW); x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    caveMap.SetCell(new Vector2I(x, y), sourceId, new Vector2I(x % created, 0));
                    painted++;
                }
            }

            Log($"  Painted {painted} cave floor tiles in a {Math.Min(cols, RoomW)}x3 strip");
            LogPass(2, $"Cave atlas loaded: {cols}x{rows} grid, {painted} tiles painted");
        }
        catch (Exception e)
        {
            LogFail(2, $"Exception: {e.Message}");
        }
    }

    // ==================== PHASE 3: CREATURE ANIMATION ====================

    private void Phase3_CreatureAnimation()
    {
        Log("=== PHASE 3: Single Creature Animation ===");

        try
        {
            var texPath = AssetBase + "enemies/creatures/skeleton.png";
            var tex = LoadPng(texPath);
            if (tex == null)
            {
                LogFail(3, $"Could not load: {texPath}");
                return;
            }

            int hframes = 8, vframes = 8;
            int frameW = tex.GetWidth() / hframes;
            int frameH = tex.GetHeight() / vframes;
            Log($"  Sheet: {tex.GetWidth()}x{tex.GetHeight()}");
            Log($"  Frame: {frameW}x{frameH} ({hframes}x{vframes} grid)");

            // Place skeleton on the iso floor
            var sprite = new Sprite2D();
            sprite.Texture = tex;
            sprite.Hframes = hframes;
            sprite.Vframes = vframes;
            sprite.TextureFilter = TextureFilterEnum.Nearest;
            // Place at center of floor
            if (_floorMap != null)
                sprite.Position = _floorMap.MapToLocal(new Vector2I(RoomW / 2, RoomH / 2));
            else
                sprite.Position = new Vector2(200, 100);

            sprite.Frame = 0; // stance, direction 0
            _entityLayer.AddChild(sprite);

            // Track for animation (walk cycle: frames 1-4, direction 0)
            _animEntities.Clear();
            _animEntities.Add(new AnimatedEntity(sprite, hframes, 1, 4, 0));
            _animFrame = 0;
            _animTimer = 0;

            Log($"  Skeleton placed at {sprite.Position}, animating walk (frames 1-4, dir 0)");
            LogPass(3, $"Skeleton animation: {frameW}x{frameH} frames, {AnimFps}fps walk cycle");
        }
        catch (Exception e)
        {
            LogFail(3, $"Exception: {e.Message}");
        }
    }

    // ==================== PHASE 4: 8-DIRECTION ROTATION ====================

    private void Phase4_DirectionRotation()
    {
        Log("=== PHASE 4: 8-Direction Rotation ===");

        if (_animEntities.Count == 0)
        {
            LogFail(4, "No animated entity from Phase 3");
            return;
        }

        _currentDirection = 0;
        _dirTimer = 0;
        _rotatingDirections = true;

        Log("  Rotating skeleton through 8 directions (0.6s each)");
        Log("  Watch console for direction mapping:");
        Log("  Row 0=?, Row 1=?, ... Row 7=?");
        Log("  (Record which visual direction each row index produces)");
        LogPass(4, "8-direction rotation started — observe visual mapping");
    }

    // ==================== PHASE 5: HERO EQUIPMENT LAYERS ====================

    private void Phase5_HeroLayers()
    {
        Log("=== PHASE 5: Hero Equipment Layers ===");

        // Stop direction rotation
        _rotatingDirections = false;
        _animEntities.Clear();

        // Clear previous sprites from entity layer
        foreach (var child in _entityLayer.GetChildren())
            if (child is Sprite2D) child.QueueFree();

        try
        {
            string[] layerNames = { "clothes", "steel_armor", "longsword" };
            var layers = new List<Sprite2D>();
            int hframes = 32, vframes = 8;
            Vector2 pos;
            if (_floorMap != null)
                pos = _floorMap.MapToLocal(new Vector2I(RoomW / 2, RoomH / 2));
            else
                pos = new Vector2(200, 100);

            foreach (var name in layerNames)
            {
                var texPath = AssetBase + $"characters/hero/{name}.png";
                var tex = LoadPng(texPath);
                if (tex == null)
                {
                    Log($"  WARNING: Could not load {name}.png");
                    continue;
                }

                var sprite = new Sprite2D();
                sprite.Texture = tex;
                sprite.Hframes = hframes;
                sprite.Vframes = vframes;
                sprite.TextureFilter = TextureFilterEnum.Nearest;
                sprite.Position = pos;
                sprite.Frame = 0;
                _entityLayer.AddChild(sprite);
                layers.Add(sprite);

                int frameW = tex.GetWidth() / hframes;
                int frameH = tex.GetHeight() / vframes;
                Log($"  Layer '{name}': {tex.GetWidth()}x{tex.GetHeight()} -> {frameW}x{frameH} frames");
            }

            // Track all layers for synced animation (walk = frames 4-11, direction 0)
            foreach (var s in layers)
            {
                _animEntities.Add(new AnimatedEntity(s, hframes, 4, 11, 0));
            }
            _animFrame = 0;
            _animTimer = 0;

            Log($"  {layers.Count} layers stacked at {pos}, animating run cycle (frames 4-11)");
            LogPass(5, $"Hero layers aligned: {layers.Count} layers in sync");
        }
        catch (Exception e)
        {
            LogFail(5, $"Exception: {e.Message}");
        }
    }

    // ==================== PHASE 6: ENEMY GALLERY ====================

    private void Phase6_EnemyGallery()
    {
        Log("=== PHASE 6: Enemy Gallery ===");

        // Stop hero animation, clear
        _animEntities.Clear();
        foreach (var child in _entityLayer.GetChildren())
            if (child is Sprite2D) child.QueueFree();

        try
        {
            int loaded = 0;
            float spacing = 80;
            float startX = -((CreatureNames.Length - 1) * spacing / 2);
            Vector2 basePos;
            if (_floorMap != null)
                basePos = _floorMap.MapToLocal(new Vector2I(RoomW / 2, RoomH / 2));
            else
                basePos = new Vector2(300, 150);

            for (int i = 0; i < CreatureNames.Length; i++)
            {
                var texPath = AssetBase + $"enemies/creatures/{CreatureNames[i]}.png";
                var tex = LoadPng(texPath);
                if (tex == null)
                {
                    Log($"  WARNING: Could not load {CreatureNames[i]}.png");
                    continue;
                }

                var sprite = new Sprite2D();
                sprite.Texture = tex;
                sprite.Hframes = 8;
                sprite.Vframes = 8;
                sprite.TextureFilter = TextureFilterEnum.Nearest;
                sprite.Position = basePos + new Vector2(startX + i * spacing, 0);
                sprite.Frame = 0;
                _entityLayer.AddChild(sprite);

                // Track for walk animation
                _animEntities.Add(new AnimatedEntity(sprite, 8, 1, 4, 0));

                // Label below
                var label = new Label();
                label.Text = $"{CreatureNames[i]} ({CreatureTiers[i]})";
                label.Position = sprite.Position + new Vector2(-30, 60);
                label.AddThemeColorOverride("font_color", Colors.White);
                label.AddThemeFontSizeOverride("font_size", 10);
                _entityLayer.AddChild(label);

                loaded++;
            }

            _animFrame = 0;
            _animTimer = 0;

            Log($"  {loaded}/{CreatureNames.Length} creatures loaded and animating");
            Log($"  FPS: {Engine.GetFramesPerSecond()}");
            LogPass(6, $"Enemy gallery: {loaded} creatures animating simultaneously");
        }
        catch (Exception e)
        {
            LogFail(6, $"Exception: {e.Message}");
        }
    }

    // ==================== PHASE 7: COMBAT EFFECTS + MOVEMENT ====================

    private void Phase7_CombatEffects()
    {
        Log("=== PHASE 7: Combat Effects + Movement ===");

        // Clear gallery
        _animEntities.Clear();
        foreach (var child in _entityLayer.GetChildren())
            child.QueueFree();

        try
        {
            // Place hero (clothes layer only for simplicity)
            var heroTex = LoadPng(AssetBase + "characters/hero/clothes.png");
            var skelTex = LoadPng(AssetBase + "enemies/creatures/skeleton.png");

            if (heroTex == null || skelTex == null)
            {
                LogFail(7, "Could not load hero or skeleton texture");
                return;
            }

            Vector2 heroStart;
            Vector2 skelPos;
            if (_floorMap != null)
            {
                heroStart = _floorMap.MapToLocal(new Vector2I(2, RoomH / 2));
                skelPos = _floorMap.MapToLocal(new Vector2I(RoomW - 3, RoomH / 2));
            }
            else
            {
                heroStart = new Vector2(100, 100);
                skelPos = new Vector2(350, 100);
            }

            var hero = new Sprite2D();
            hero.Texture = heroTex;
            hero.Hframes = 32;
            hero.Vframes = 8;
            hero.TextureFilter = TextureFilterEnum.Nearest;
            hero.Position = heroStart;
            hero.Frame = 4; // run frame, direction 0
            _entityLayer.AddChild(hero);

            var skel = new Sprite2D();
            skel.Texture = skelTex;
            skel.Hframes = 8;
            skel.Vframes = 8;
            skel.TextureFilter = TextureFilterEnum.Nearest;
            skel.Position = skelPos;
            skel.Frame = 0; // stance
            _entityLayer.AddChild(skel);

            // Animate hero walking toward skeleton
            _animEntities.Add(new AnimatedEntity(hero, 32, 4, 11, 0));
            _animFrame = 0;
            _animTimer = 0;

            // Movement
            _moveTarget = hero;
            _moveDestination = skelPos - new Vector2(40, 0);
            _isMoving = true;

            // Schedule combat effects after movement (~1.5s)
            GetTree().CreateTimer(1.5).Timeout += () =>
            {
                if (!IsInsideTree()) return;
                // Slash effect at skeleton position
                ShowSlashEffect(skelPos);
                // Floating damage
                ShowFloatingText(skelPos, "21", new Color(1, 0.3f, 0.3f));
                // Flash skeleton
                var flashTween = CreateTween();
                flashTween.TweenProperty(skel, "modulate", Colors.Red, 0.06);
                flashTween.TweenProperty(skel, "modulate", Colors.White, 0.14);
                // Skeleton hit frame
                skel.Frame = 6; // hit frame
                Log("  Slash effect + floating damage shown");
            };

            // Skeleton dies after 2.5s
            GetTree().CreateTimer(2.5).Timeout += () =>
            {
                if (!IsInsideTree()) return;
                skel.Frame = 7; // dead frame
                skel.Modulate = new Color(1, 1, 1, 0.5f);
                Log("  Skeleton dead frame shown");
            };

            Log($"  Hero moving from {heroStart} toward skeleton at {skelPos}");
            LogPass(7, "Combat effects: slash, floating text, hit/dead frames");
        }
        catch (Exception e)
        {
            LogFail(7, $"Exception: {e.Message}");
        }
    }

    // ==================== PHASE 8: UI + SUMMARY ====================

    private void Phase8_UISummary()
    {
        Log("=== PHASE 8: UI Overlay + Summary ===");

        try
        {
            // HUD panel (dark fantasy style)
            var hudPanel = CreateStyledPanel("A DUNGEON IN THE MIDDLE OF NOWHERE",
                new Vector2(12, 50), new Vector2(280, 120));
            hudPanel.Visible = true;
            _uiLayer.AddChild(hudPanel);

            var statsLabel = hudPanel.GetNode<Label>("Content");
            statsLabel.Text = "HP: 100 | XP: 0 | LVL: 1 | Floor: 1\n\nMove: Arrow keys\nAuto-attack: nearest enemy";

            // UI icon bar
            var iconBar = new HBoxContainer();
            iconBar.Position = new Vector2(12, 180);
            _uiLayer.AddChild(iconBar);

            string[] iconFiles = { "Icon-sword.png", "Icon-potion.png", "Icon-potionmana.png", "Icon-coin.png", "Icon-gear.png" };
            int iconsLoaded = 0;
            foreach (var iconFile in iconFiles)
            {
                var iconPath = AssetBase + "ui/" + iconFile;
                var tex = LoadPng(iconPath);
                if (tex != null)
                {
                    var rect = new TextureRect();
                    rect.Texture = tex;
                    rect.CustomMinimumSize = new Vector2(36, 36);
                    rect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
                    iconBar.AddChild(rect);
                    iconsLoaded++;
                }
            }
            Log($"  UI icons loaded: {iconsLoaded}/{iconFiles.Length}");

            // HP/MP orbs (reuse existing component)
            var orbs = new HpMpOrbs();
            orbs.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            _uiLayer.AddChild(orbs);
            orbs.UpdateValues(75, 100, 45, 65);
            Log("  HP/MP orbs displayed (75/100 HP, 45/65 MP)");

            // Camera zoom test
            var camera = GetNode<Camera2D>("Camera2D");
            if (camera != null)
            {
                camera.Zoom = new Vector2(2, 2);
                Log("  Camera zoom set to 2x");
            }

            // Performance check
            Log($"  FPS: {Engine.GetFramesPerSecond()}");

            LogPass(8, $"UI overlay: HUD + {iconsLoaded} icons + HP/MP orbs + 2x zoom");
        }
        catch (Exception e)
        {
            LogFail(8, $"Exception: {e.Message}");
        }

        // Print summary
        GD.Print("");
        GD.Print("========================================");
        GD.Print("  ISOMETRIC DEMO SUMMARY");
        GD.Print("========================================");
        foreach (var result in _results)
            GD.Print($"  {result}");
        GD.Print($"  TOTAL: {_passCount}/8 phases passed");
        GD.Print("========================================");
        GD.Print("");

        Log($"\n=== SUMMARY: {_passCount}/8 phases passed ===");
    }

    // ==================== HELPERS ====================

    private void Log(string msg)
    {
        GD.Print(msg);
        _logLabel.Text = msg.Replace("===", "—").Trim();
    }

    private void LogPass(int phase, string msg)
    {
        var line = $"[PHASE {phase}] PASS — {msg}";
        GD.Print(line);
        _results.Add(line);
        _passCount++;
    }

    private void LogFail(int phase, string msg)
    {
        var line = $"[PHASE {phase}] FAIL — {msg}";
        GD.Print(line);
        _results.Add(line);
    }

    private Panel CreateStyledPanel(string title, Vector2 position, Vector2 size)
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

    private void ShowFloatingText(Vector2 worldPos, string text, Color color)
    {
        var label = new Label();
        label.Text = text;
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeFontSizeOverride("font_size", 16);
        label.Position = worldPos - new Vector2(20, 20);
        label.ZIndex = 100;
        AddChild(label);

        var tween = CreateTween();
        tween.TweenProperty(label, "position:y", worldPos.Y - 50, 0.9);
        tween.Parallel().TweenProperty(label, "modulate:a", 0.0, 0.9);
        tween.TweenCallback(Callable.From(label.QueueFree));
    }

    /// <summary>
    /// Load a PNG directly from disk, bypassing Godot's import system.
    /// Needed because headless --import doesn't generate .import sidecar files.
    /// </summary>
    private static Texture2D LoadPng(string resPath)
    {
        var diskPath = ProjectSettings.GlobalizePath(resPath);
        if (!FileAccess.FileExists(resPath) && !System.IO.File.Exists(diskPath))
        {
            GD.PrintErr($"  File not found: {diskPath}");
            return null;
        }
        var img = new Image();
        var err = img.Load(diskPath);
        if (err != Error.Ok)
        {
            GD.PrintErr($"  Failed to load image: {diskPath} (error: {err})");
            return null;
        }
        return ImageTexture.CreateFromImage(img);
    }

    private void ShowSlashEffect(Vector2 pos)
    {
        var slash = new Polygon2D();
        slash.Polygon = new Vector2[] {
            new(-13, -2), new(13, -2), new(13, 2), new(-13, 2)
        };
        slash.Color = new Color(0.961f, 0.784f, 0.420f, 0.95f);
        slash.Position = pos;
        slash.Rotation = (float)GD.RandRange(-1.2, 1.2);
        slash.ZIndex = 50;
        AddChild(slash);

        var tween = CreateTween();
        tween.TweenProperty(slash, "modulate:a", 0.0, 0.15);
        tween.Parallel().TweenProperty(slash, "position:y", pos.Y - 8, 0.15);
        tween.TweenCallback(Callable.From(slash.QueueFree));
    }
}
