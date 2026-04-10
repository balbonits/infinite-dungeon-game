using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Sprite alignment tool with sidebar UI.
/// Left panel: categorized sprite library dropdown, asset source selector,
///   alignment controls (offset, scale), save button.
/// Right canvas: sprite on isometric tile diamond with grid.
///
/// Controls:
///   Arrow keys    = nudge offset (hold Shift for 5px)
///   +/-           = adjust scale
///   R             = reset to defaults
///   Space         = toggle auto-animate
///   G             = toggle 3x3 grid
///   1-8           = direction
///   F12           = screenshot
///   Esc           = quit
/// </summary>
public partial class TestSpriteAlign : Node2D
{
    private const int TileW = 64;
    private const int TileH = 32;
    private const float SidebarWidth = 320f;
    private const string AlignmentSavePath = "res://docs/assets/sprite-alignment-data.json";

    // ── Sprite library (built from filesystem scan) ──
    private struct SpriteEntry
    {
        public string Category;    // "characters/hero", "enemies/creatures", etc.
        public string Name;        // "slime", "longsword", etc.
        public string Path;        // full res:// path
        public int Hframes;
        public int Vframes;
        public float DefaultScale;
    }

    private readonly List<SpriteEntry> _library = new();
    private int _selectedIdx;

    // Alignment state
    private float _scale;
    private Vector2 _offset;
    private int _currentFrame;
    private int _currentRow;
    private bool _autoAnimate;
    private bool _showGrid;
    private float _animTimer;
    private string _statusMsg = "";
    private float _statusTimer;

    // Saved alignments: key = sprite path, value = (scale, offsetX, offsetY)
    private readonly Dictionary<string, (float scale, float x, float y)> _savedAlignments = new();

    // Visual
    private Sprite2D _sprite;
    private Camera2D _camera;

    // Sidebar UI
    private OptionButton _categoryDropdown;
    private ItemList _spriteList;
    private Label _infoLabel;
    private Label _statusLabel;
    private SpinBox _scaleSpinBox;
    private SpinBox _offsetXSpinBox;
    private SpinBox _offsetYSpinBox;

    // Category filter
    private readonly List<string> _categories = new();
    private string _activeCategory = "";

    public override void _Ready()
    {
        // Scan asset library
        ScanAssets();

        // Dark background
        var bg = new ColorRect();
        bg.Color = new Color(0.05f, 0.05f, 0.08f);
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var bgLayer = new CanvasLayer { Layer = -1 };
        bgLayer.AddChild(bg);
        AddChild(bgLayer);

        // Camera for canvas area (offset right of sidebar)
        _camera = new Camera2D();
        _camera.Zoom = new Vector2(3.5f, 3.5f);
        _camera.Offset = new Vector2(SidebarWidth / 2, 0);
        AddChild(_camera);

        // The sprite
        _sprite = new Sprite2D();
        _sprite.TextureFilter = TextureFilterEnum.Nearest;
        _sprite.ZIndex = 5;
        AddChild(_sprite);

        // Load saved alignments
        LoadAlignments();

        // Build sidebar
        BuildSidebar();

        // Load first sprite
        if (_library.Count > 0)
            SelectSprite(0);
    }

    // ═══════════════════════════════════════════════════
    // ASSET SCANNING
    // ═══════════════════════════════════════════════════
    private void ScanAssets()
    {
        string basePath = "res://assets/isometric";
        ScanDir(basePath, "");
        _library.Sort((a, b) => string.Compare($"{a.Category}/{a.Name}", $"{b.Category}/{b.Name}"));

        // Build category list
        var cats = _library.Select(s => s.Category).Distinct().OrderBy(c => c).ToList();
        _categories.Add("All");
        _categories.AddRange(cats);
    }

    private void ScanDir(string path, string category)
    {
        var dir = DirAccess.Open(path);
        if (dir == null) return;

        dir.ListDirBegin();
        string fileName;
        while ((fileName = dir.GetNext()) != "")
        {
            if (fileName.StartsWith(".")) continue;

            string fullPath = $"{path}/{fileName}";
            string cat = category.Length > 0 ? category : fileName;

            if (dir.CurrentIsDir())
            {
                // Skip source, tiles directories
                if (fileName == "source") continue;
                string subCat = category.Length > 0 ? $"{category}/{fileName}" : fileName;
                ScanDir(fullPath, subCat);
            }
            else if (fileName.EndsWith(".png"))
            {
                string name = fileName.Replace(".png", "");
                var entry = new SpriteEntry
                {
                    Category = cat,
                    Name = name,
                    Path = fullPath,
                    Hframes = GuessHframes(cat, name),
                    Vframes = GuessVframes(cat, name),
                    DefaultScale = GuessScale(cat, name),
                };
                _library.Add(entry);
            }
        }
        dir.ListDirEnd();
    }

    private int GuessHframes(string cat, string name)
    {
        if (cat.StartsWith("characters")) return 32;
        if (cat.Contains("creatures")) return 8;
        if (cat.StartsWith("enemies") && !cat.Contains("detailed")) return 8;
        if (cat.StartsWith("npcs") && !cat.Contains("parts")) return 8;
        // Tiles: detect from common ISS/SBS sheet sizes
        if (cat.Contains("tiles") || cat.Contains("floors") || cat.Contains("walls"))
        {
            // ISS floor sheets are typically 256x64 (4x2 at 64x32) or similar
            // ISS wall sheets are 512x128 (8x2 at 64x64)
            // Return 1 — we'll show individual tiles via _currentFrame stepping
            return 1;
        }
        return 1;
    }

    private int GuessVframes(string cat, string name)
    {
        if (cat.StartsWith("characters")) return 8;
        if (cat.Contains("creatures")) return 8;
        if (cat.StartsWith("enemies") && !cat.Contains("detailed")) return 8;
        if (cat.StartsWith("npcs") && !cat.Contains("parts")) return 8;
        return 1;
    }

    private float GuessScale(string cat, string name)
    {
        if (cat.StartsWith("characters")) return 0.625f;
        if (cat.Contains("creatures")) return 0.3125f;
        if (cat.StartsWith("enemies")) return 0.3125f;
        // Tiles should render at 1:1 scale (64x32 tiles match the 64x32 diamond)
        if (cat.Contains("tiles") || cat.Contains("floors") || cat.Contains("walls"))
            return 1.0f;
        return 0.5f;
    }

    // ═══════════════════════════════════════════════════
    // SIDEBAR UI
    // ═══════════════════════════════════════════════════
    private void BuildSidebar()
    {
        var ui = new CanvasLayer { Layer = 10 };
        AddChild(ui);

        // Sidebar panel
        var sidebarBg = new Panel();
        sidebarBg.AnchorRight = 0;
        sidebarBg.AnchorBottom = 1;
        sidebarBg.OffsetRight = SidebarWidth;
        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.07f, 0.08f, 0.11f, 0.95f);
        style.BorderColor = new Color(0.961f, 0.784f, 0.420f, 0.2f);
        style.SetBorderWidthAll(0);
        style.BorderWidthRight = 2;
        sidebarBg.AddThemeStyleboxOverride("panel", style);
        ui.AddChild(sidebarBg);

        var margin = new MarginContainer();
        margin.AnchorBottom = 1;
        margin.OffsetRight = SidebarWidth;
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        ui.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        margin.AddChild(vbox);

        // Title
        var title = new Label();
        title.Text = "SPRITE ALIGNMENT TOOL";
        title.AddThemeColorOverride("font_color", new Color("#f5c86b"));
        title.AddThemeFontSizeOverride("font_size", 14);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        // ── Category dropdown ──
        vbox.AddChild(MakeLabel("Category"));
        _categoryDropdown = new OptionButton();
        foreach (var cat in _categories)
            _categoryDropdown.AddItem(cat);
        _categoryDropdown.AddThemeFontSizeOverride("font_size", 11);
        _categoryDropdown.ItemSelected += OnCategorySelected;
        vbox.AddChild(_categoryDropdown);

        // ── Sprite list ──
        vbox.AddChild(MakeLabel("Sprites"));
        _spriteList = new ItemList();
        _spriteList.CustomMinimumSize = new Vector2(0, 200);
        _spriteList.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        _spriteList.AddThemeFontSizeOverride("font_size", 11);
        _spriteList.AddThemeColorOverride("font_color", new Color("#ecf0ff"));
        var listStyle = new StyleBoxFlat();
        listStyle.BgColor = new Color(0.05f, 0.06f, 0.09f);
        listStyle.SetCornerRadiusAll(4);
        _spriteList.AddThemeStyleboxOverride("panel", listStyle);
        _spriteList.ItemSelected += OnSpriteSelected;
        vbox.AddChild(_spriteList);

        PopulateSpriteList("");

        // ── Alignment controls ──
        vbox.AddChild(MakeLabel("Alignment"));

        // Scale
        var scaleRow = new HBoxContainer();
        scaleRow.AddChild(MakeLabel("Scale:", 11, 50));
        _scaleSpinBox = new SpinBox();
        _scaleSpinBox.MinValue = 0.05;
        _scaleSpinBox.MaxValue = 3.0;
        _scaleSpinBox.Step = 0.025;
        _scaleSpinBox.Value = 0.3125;
        _scaleSpinBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _scaleSpinBox.AddThemeFontSizeOverride("font_size", 11);
        _scaleSpinBox.ValueChanged += (_) => { _scale = (float)_scaleSpinBox.Value; UpdateSpriteTransform(); };
        scaleRow.AddChild(_scaleSpinBox);
        vbox.AddChild(scaleRow);

        // Offset X
        var oxRow = new HBoxContainer();
        oxRow.AddChild(MakeLabel("Off X:", 11, 50));
        _offsetXSpinBox = new SpinBox();
        _offsetXSpinBox.MinValue = -100;
        _offsetXSpinBox.MaxValue = 100;
        _offsetXSpinBox.Step = 1;
        _offsetXSpinBox.Value = 0;
        _offsetXSpinBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _offsetXSpinBox.AddThemeFontSizeOverride("font_size", 11);
        _offsetXSpinBox.ValueChanged += (_) => { _offset.X = (float)_offsetXSpinBox.Value; UpdateSpriteTransform(); };
        oxRow.AddChild(_offsetXSpinBox);
        vbox.AddChild(oxRow);

        // Offset Y
        var oyRow = new HBoxContainer();
        oyRow.AddChild(MakeLabel("Off Y:", 11, 50));
        _offsetYSpinBox = new SpinBox();
        _offsetYSpinBox.MinValue = -100;
        _offsetYSpinBox.MaxValue = 100;
        _offsetYSpinBox.Step = 1;
        _offsetYSpinBox.Value = 0;
        _offsetYSpinBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _offsetYSpinBox.AddThemeFontSizeOverride("font_size", 11);
        _offsetYSpinBox.ValueChanged += (_) => { _offset.Y = (float)_offsetYSpinBox.Value; UpdateSpriteTransform(); };
        oyRow.AddChild(_offsetYSpinBox);
        vbox.AddChild(oyRow);

        // Buttons row
        var btnRow = new HBoxContainer();
        btnRow.AddThemeConstantOverride("separation", 6);

        var saveBtn = MakeButton("Save");
        saveBtn.Pressed += OnSavePressed;
        btnRow.AddChild(saveBtn);

        var resetBtn = MakeButton("Reset");
        resetBtn.Pressed += OnResetPressed;
        btnRow.AddChild(resetBtn);

        var screenshotBtn = MakeButton("F12");
        screenshotBtn.Pressed += TakeScreenshot;
        btnRow.AddChild(screenshotBtn);

        vbox.AddChild(btnRow);

        // ── Info ──
        _infoLabel = new Label();
        _infoLabel.AddThemeColorOverride("font_color", new Color("#b6bfdb"));
        _infoLabel.AddThemeFontSizeOverride("font_size", 10);
        _infoLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        vbox.AddChild(_infoLabel);

        // Status message
        _statusLabel = new Label();
        _statusLabel.AddThemeColorOverride("font_color", new Color("#6bff89"));
        _statusLabel.AddThemeFontSizeOverride("font_size", 11);
        _statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(_statusLabel);

        // Controls hint
        var hint = new Label();
        hint.Text = "Arrow: nudge  +/-: scale  1-8: dir\nSpace: animate  G: grid  R: reset";
        hint.AddThemeColorOverride("font_color", new Color(1, 1, 1, 0.25f));
        hint.AddThemeFontSizeOverride("font_size", 10);
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(hint);
    }

    private void PopulateSpriteList(string categoryFilter)
    {
        _spriteList.Clear();
        _activeCategory = categoryFilter;

        for (int i = 0; i < _library.Count; i++)
        {
            var entry = _library[i];
            if (categoryFilter.Length > 0 && categoryFilter != "All" && entry.Category != categoryFilter)
                continue;

            string label = entry.Category == _activeCategory || _activeCategory == "" || _activeCategory == "All"
                ? $"{entry.Category}/{entry.Name}"
                : entry.Name;

            bool hasSaved = _savedAlignments.ContainsKey(entry.Path);
            if (hasSaved) label = "✓ " + label;

            _spriteList.AddItem(label);
            _spriteList.SetItemMetadata(_spriteList.ItemCount - 1, i);
        }

        if (_spriteList.ItemCount > 0)
            _spriteList.Select(0);
    }

    private void OnCategorySelected(long idx)
    {
        string cat = _categories[(int)idx];
        PopulateSpriteList(cat == "All" ? "" : cat);
    }

    private void OnSpriteSelected(long idx)
    {
        int libIdx = (int)_spriteList.GetItemMetadata((int)idx);
        SelectSprite(libIdx);
    }

    private void SelectSprite(int idx)
    {
        if (idx < 0 || idx >= _library.Count) return;
        _selectedIdx = idx;

        var entry = _library[idx];

        // Load saved alignment or defaults
        if (_savedAlignments.TryGetValue(entry.Path, out var saved))
        {
            _scale = saved.scale;
            _offset = new Vector2(saved.x, saved.y);
        }
        else
        {
            _scale = entry.DefaultScale;
            _offset = Vector2.Zero;
        }

        _currentFrame = 0;
        _currentRow = 0;

        var tex = ResourceLoader.Load<Texture2D>(entry.Path);
        if (tex == null) tex = TestHelper.LoadIssPng(entry.Path);

        if (tex != null)
        {
            _sprite.Texture = tex;

            int hf = entry.Hframes;
            int vf = entry.Vframes;

            // Auto-detect tile sheet grid if marked as single frame
            if (hf == 1 && vf == 1 && tex.GetWidth() > 64)
            {
                string cat = entry.Category;
                if (cat.Contains("wall") || cat.Contains("Wall"))
                {
                    // Wall blocks: 64x64 cells
                    hf = tex.GetWidth() / 64;
                    vf = tex.GetHeight() / 64;
                }
                else if (cat.Contains("floor") || cat.Contains("Floor"))
                {
                    // Floor tiles: 64x32 cells
                    hf = tex.GetWidth() / 64;
                    vf = tex.GetHeight() / 32;
                }
                else if (cat.Contains("autotile") || cat.Contains("128x64"))
                {
                    // SBS autotiles: 128x64 cells
                    hf = tex.GetWidth() / 128;
                    vf = tex.GetHeight() / 64;
                }
                else
                {
                    // Generic: try 64x32 first, then 64x64
                    if (tex.GetWidth() % 64 == 0 && tex.GetHeight() % 32 == 0)
                    {
                        hf = tex.GetWidth() / 64;
                        vf = tex.GetHeight() / 32;
                    }
                    else if (tex.GetWidth() % 64 == 0 && tex.GetHeight() % 64 == 0)
                    {
                        hf = tex.GetWidth() / 64;
                        vf = tex.GetHeight() / 64;
                    }
                }
                if (hf < 1) hf = 1;
                if (vf < 1) vf = 1;
            }

            _sprite.Hframes = hf;
            _sprite.Vframes = vf;
            _sprite.Visible = true;

            GD.Print($"[SPRITE-ALIGN] Loaded: {entry.Path} ({tex.GetWidth()}x{tex.GetHeight()}) grid={hf}x{vf}");
        }
        else
        {
            _sprite.Visible = false;
        }

        // Sync spinboxes
        _scaleSpinBox.SetValueNoSignal(_scale);
        _offsetXSpinBox.SetValueNoSignal(_offset.X);
        _offsetYSpinBox.SetValueNoSignal(_offset.Y);

        UpdateSpriteTransform();
        UpdateInfo();
        QueueRedraw();
    }

    private void UpdateSpriteTransform()
    {
        if (!_sprite.Visible) return;
        var entry = _library[_selectedIdx];
        int frame = _currentRow * entry.Hframes + _currentFrame;
        int maxFrame = entry.Hframes * entry.Vframes;
        if (frame < maxFrame) _sprite.Frame = frame;
        _sprite.Scale = new Vector2(_scale, _scale);
        _sprite.Position = _offset;
    }

    private void UpdateInfo()
    {
        var entry = _library[_selectedIdx];
        int frameW = 0, frameH = 0;
        if (_sprite.Texture != null)
        {
            frameW = _sprite.Texture.GetWidth() / entry.Hframes;
            frameH = _sprite.Texture.GetHeight() / entry.Vframes;
        }

        _infoLabel.Text =
            $"{entry.Category}/{entry.Name}\n" +
            $"Grid: {entry.Hframes}x{entry.Vframes}  Frame: {frameW}x{frameH}\n" +
            $"Rendered: {frameW * _scale:F0}x{frameH * _scale:F0}px\n" +
            $"Dir: {_currentRow}  Frame: {_currentFrame}";
    }

    // ═══════════════════════════════════════════════════
    // SAVE / LOAD ALIGNMENTS
    // ═══════════════════════════════════════════════════
    private void OnSavePressed()
    {
        var entry = _library[_selectedIdx];
        _savedAlignments[entry.Path] = (_scale, _offset.X, _offset.Y);
        SaveAlignments();
        ShowStatus($"Saved: {entry.Name} (s={_scale:F3}, x={_offset.X:F0}, y={_offset.Y:F0})");
        PopulateSpriteList(_activeCategory); // refresh ✓ marks
    }

    private void OnResetPressed()
    {
        var entry = _library[_selectedIdx];
        _scale = entry.DefaultScale;
        _offset = Vector2.Zero;
        _scaleSpinBox.SetValueNoSignal(_scale);
        _offsetXSpinBox.SetValueNoSignal(0);
        _offsetYSpinBox.SetValueNoSignal(0);
        UpdateSpriteTransform();
        UpdateInfo();
        ShowStatus("Reset to defaults");
    }

    private void SaveAlignments()
    {
        var dict = new Godot.Collections.Dictionary();
        foreach (var (path, (scale, x, y)) in _savedAlignments)
        {
            var entry = new Godot.Collections.Dictionary();
            entry["scale"] = scale;
            entry["offset_x"] = x;
            entry["offset_y"] = y;
            dict[path] = entry;
        }

        string json = Json.Stringify(dict, "  ");
        string diskPath = ProjectSettings.GlobalizePath(AlignmentSavePath);
        string dir = System.IO.Path.GetDirectoryName(diskPath);
        if (!DirAccess.DirExistsAbsolute(dir))
            DirAccess.MakeDirRecursiveAbsolute(dir);

        using var file = FileAccess.Open(AlignmentSavePath, FileAccess.ModeFlags.Write);
        if (file != null)
        {
            file.StoreString(json);
            GD.Print($"[SPRITE-ALIGN] Alignments saved to {AlignmentSavePath}");
        }
    }

    private void LoadAlignments()
    {
        if (!FileAccess.FileExists(AlignmentSavePath)) return;

        using var file = FileAccess.Open(AlignmentSavePath, FileAccess.ModeFlags.Read);
        if (file == null) return;

        string json = file.GetAsText();
        var parsed = Json.ParseString(json);
        if (parsed.VariantType != Variant.Type.Dictionary) return;

        var dict = parsed.AsGodotDictionary();
        foreach (var key in dict.Keys)
        {
            string path = key.AsString();
            var entry = dict[key].AsGodotDictionary();
            float scale = (float)entry["scale"].AsDouble();
            float x = (float)entry["offset_x"].AsDouble();
            float y = (float)entry["offset_y"].AsDouble();
            _savedAlignments[path] = (scale, x, y);
        }

        GD.Print($"[SPRITE-ALIGN] Loaded {_savedAlignments.Count} alignments");
    }

    private void TakeScreenshot()
    {
        var entry = _library[_selectedIdx];
        var img = GetViewport().GetTexture().GetImage();
        string dir = ProjectSettings.GlobalizePath("res://docs/evidence/sprite-align/");
        if (!DirAccess.DirExistsAbsolute(dir))
            DirAccess.MakeDirRecursiveAbsolute(dir);

        string timestamp = Time.GetDatetimeStringFromSystem().Replace(":", "").Replace("-", "").Replace("T", "_");
        string name = $"{entry.Name}_dir{_currentRow}_f{_currentFrame}_s{_scale:F3}_x{_offset.X:F0}y{_offset.Y:F0}_{timestamp}";
        img.SavePng(dir + name + ".png");
        ShowStatus($"Screenshot: {name}.png");
    }

    private void ShowStatus(string msg)
    {
        _statusMsg = msg;
        _statusTimer = 3f;
        _statusLabel.Text = msg;
    }

    // ═══════════════════════════════════════════════════
    // DRAWING (canvas area)
    // ═══════════════════════════════════════════════════
    public override void _Draw()
    {
        if (_showGrid)
        {
            for (int gx = -1; gx <= 1; gx++)
                for (int gy = -1; gy <= 1; gy++)
                {
                    float cx = (gx - gy) * TileW / 2f;
                    float cy = (gx + gy) * TileH / 2f;
                    DrawIsoDiamond(cx, cy, gx == 0 && gy == 0 ? new Color("#f5c86b") : new Color(0.5f, 0.5f, 0.5f, 0.25f));
                }
        }
        else
        {
            DrawIsoDiamond(0, 0, new Color("#f5c86b"));
        }

        // Crosshair at tile center
        DrawLine(new Vector2(-8, 0), new Vector2(8, 0), new Color(1, 0, 0, 0.5f), 1);
        DrawLine(new Vector2(0, -8), new Vector2(0, 8), new Color(0, 1, 0, 0.5f), 1);
    }

    private void DrawIsoDiamond(float cx, float cy, Color color)
    {
        var t = new Vector2(cx, cy - TileH / 2f);
        var r = new Vector2(cx + TileW / 2f, cy);
        var b = new Vector2(cx, cy + TileH / 2f);
        var l = new Vector2(cx - TileW / 2f, cy);
        DrawLine(t, r, color, 1.5f);
        DrawLine(r, b, color, 1.5f);
        DrawLine(b, l, color, 1.5f);
        DrawLine(l, t, color, 1.5f);
    }

    // ═══════════════════════════════════════════════════
    // INPUT + PROCESS
    // ═══════════════════════════════════════════════════
    public override void _Process(double delta)
    {
        if (_autoAnimate && _sprite.Visible)
        {
            _animTimer += (float)delta;
            if (_animTimer >= 0.15f)
            {
                _animTimer = 0;
                var entry = _library[_selectedIdx];
                _currentFrame = (_currentFrame + 1) % entry.Hframes;
                UpdateSpriteTransform();
                UpdateInfo();
            }
        }

        if (_statusTimer > 0)
        {
            _statusTimer -= (float)delta;
            if (_statusTimer <= 0) _statusLabel.Text = "";
        }
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (ev is not InputEventKey key || !key.Pressed) return;

        float nudge = key.ShiftPressed ? 5f : 1f;
        var entry = _library.Count > 0 ? _library[_selectedIdx] : default;

        switch (key.Keycode)
        {
            // Arrow keys = nudge offset
            case Key.Up:    _offset.Y -= nudge; SyncSpinboxes(); UpdateSpriteTransform(); UpdateInfo(); QueueRedraw(); break;
            case Key.Down:  _offset.Y += nudge; SyncSpinboxes(); UpdateSpriteTransform(); UpdateInfo(); QueueRedraw(); break;
            case Key.Left:  _offset.X -= nudge; SyncSpinboxes(); UpdateSpriteTransform(); UpdateInfo(); QueueRedraw(); break;
            case Key.Right: _offset.X += nudge; SyncSpinboxes(); UpdateSpriteTransform(); UpdateInfo(); QueueRedraw(); break;

            // Scale
            case Key.Equal: _scale += 0.025f; _scaleSpinBox.SetValueNoSignal(_scale); UpdateSpriteTransform(); UpdateInfo(); break;
            case Key.Minus: _scale = Mathf.Max(0.05f, _scale - 0.025f); _scaleSpinBox.SetValueNoSignal(_scale); UpdateSpriteTransform(); UpdateInfo(); break;

            // Direction
            case Key.Key1: _currentRow = 0; UpdateSpriteTransform(); UpdateInfo(); break;
            case Key.Key2: _currentRow = 1; UpdateSpriteTransform(); UpdateInfo(); break;
            case Key.Key3: _currentRow = 2; UpdateSpriteTransform(); UpdateInfo(); break;
            case Key.Key4: _currentRow = 3; UpdateSpriteTransform(); UpdateInfo(); break;
            case Key.Key5: _currentRow = 4; UpdateSpriteTransform(); UpdateInfo(); break;
            case Key.Key6: _currentRow = 5; UpdateSpriteTransform(); UpdateInfo(); break;
            case Key.Key7: _currentRow = 6; UpdateSpriteTransform(); UpdateInfo(); break;
            case Key.Key8: _currentRow = 7; UpdateSpriteTransform(); UpdateInfo(); break;

            // Toggles
            case Key.Space: _autoAnimate = !_autoAnimate; break;
            case Key.G: _showGrid = !_showGrid; QueueRedraw(); break;
            case Key.R: OnResetPressed(); break;

            case Key.F12: TakeScreenshot(); break;
            case Key.Escape: GetTree().Quit(); break;
        }
    }

    private void SyncSpinboxes()
    {
        _offsetXSpinBox.SetValueNoSignal(_offset.X);
        _offsetYSpinBox.SetValueNoSignal(_offset.Y);
    }

    // ═══════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════
    private static Label MakeLabel(string text, int size = 12, float minW = 0)
    {
        var lbl = new Label();
        lbl.Text = text;
        lbl.AddThemeColorOverride("font_color", new Color("#b6bfdb"));
        lbl.AddThemeFontSizeOverride("font_size", size);
        if (minW > 0) lbl.CustomMinimumSize = new Vector2(minW, 0);
        return lbl;
    }

    private static Button MakeButton(string text)
    {
        var btn = new Button();
        btn.Text = text;
        btn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        btn.CustomMinimumSize = new Vector2(0, 30);

        var s = new StyleBoxFlat();
        s.BgColor = new Color(0.12f, 0.14f, 0.20f, 0.9f);
        s.BorderColor = new Color(0.961f, 0.784f, 0.420f, 0.3f);
        s.SetBorderWidthAll(1);
        s.SetCornerRadiusAll(4);
        btn.AddThemeStyleboxOverride("normal", s);

        var h = s.Duplicate() as StyleBoxFlat;
        h.BorderColor = new Color(0.961f, 0.784f, 0.420f, 0.7f);
        h.BgColor = new Color(0.18f, 0.20f, 0.28f, 0.9f);
        btn.AddThemeStyleboxOverride("hover", h);
        btn.AddThemeStyleboxOverride("pressed", h);

        btn.AddThemeColorOverride("font_color", new Color("#ecf0ff"));
        btn.AddThemeColorOverride("font_hover_color", new Color("#f5c86b"));
        btn.AddThemeFontSizeOverride("font_size", 12);
        return btn;
    }
}
