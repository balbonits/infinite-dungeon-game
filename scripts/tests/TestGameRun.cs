using System;
using System.Collections.Generic;
using System.Linq;
using DungeonGame.Dungeon;
using Godot;

/// <summary>
/// Automated end-to-end test that exercises the full game loop:
/// Init -> Town -> Shop -> Dungeon -> Combat -> Floor Transition -> Save/Load -> Bank -> Backpack -> New Systems.
/// Runs headless (no rendering). Follows the GameDemo step-based pattern.
/// </summary>
public partial class TestGameRun : Node2D
{
    private readonly List<(float delay, Action action)> _steps = new();
    private int _stepIndex;
    private float _timer;
    private bool _complete;

    // Tracking
    private int _assertions;
    private int _assertionsPassed;
    private int _totalKills;
    private Random _rng;

    // Saved state for load verification
    private Dictionary<string, object> _savedData;

    // Dungeon floors
    private FloorData _floor1;
    private FloorData _floor2;

    // Visual components (windowed mode only)
    private bool _isHeadless;
    private RichTextLabel _logLabel;
    private Label _phaseLabel;
    private Label _statsLabel;
    private TileMapLayer _tileMap;
    private Node2D _entityLayer;
    private Camera2D _camera;

    public override void _Ready()
    {
        _rng = new Random(42);
        _isHeadless = DisplayServer.GetName() == "headless";

        if (!_isHeadless)
            SetupVisuals();

        SetupSteps();

        _stepIndex = 0;

        if (_isHeadless)
        {
            _timer = 0.0f;
            for (int i = 0; i < _steps.Count; i++)
                _steps[i] = (0.0f, _steps[i].action);
        }
        else
        {
            _timer = 0.8f;
            for (int i = 0; i < _steps.Count; i++)
                _steps[i] = (0.5f, _steps[i].action);
        }
    }

    private void SetupVisuals()
    {
        // Dark background
        var bg = new ColorRect();
        bg.Color = new Color(0.05f, 0.05f, 0.08f);
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var bgLayer = new CanvasLayer { Layer = -1 };
        bgLayer.AddChild(bg);
        AddChild(bgLayer);

        // Camera for dungeon view (right side of screen)
        _camera = GetNodeOrNull<Camera2D>("Camera2D");
        if (_camera == null)
        {
            _camera = new Camera2D();
            AddChild(_camera);
        }
        _camera.Zoom = new Vector2(1.0f, 1.0f);
        _camera.Position = new Vector2(400, 200);

        // Entity layer for enemies/player markers
        _entityLayer = new Node2D();
        _entityLayer.ZIndex = 10;
        AddChild(_entityLayer);

        // UI overlay
        var ui = new CanvasLayer { Layer = 20 };
        AddChild(ui);

        // Phase title (top center)
        _phaseLabel = new Label();
        _phaseLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _phaseLabel.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
        _phaseLabel.Position = new Vector2(-200, 8);
        _phaseLabel.Size = new Vector2(400, 40);
        _phaseLabel.AddThemeColorOverride("font_color", new Color("#f5c86b"));
        _phaseLabel.AddThemeFontSizeOverride("font_size", 20);
        ui.AddChild(_phaseLabel);

        // Scrolling log panel (left side)
        var logPanel = new PanelContainer();
        logPanel.Position = new Vector2(12, 50);
        logPanel.Size = new Vector2(460, 700);
        var logStyle = new StyleBoxFlat();
        logStyle.BgColor = new Color(0.06f, 0.07f, 0.1f, 0.92f);
        logStyle.BorderColor = new Color(0.961f, 0.784f, 0.420f, 0.25f);
        logStyle.SetBorderWidthAll(1);
        logStyle.SetCornerRadiusAll(6);
        logStyle.SetContentMarginAll(8);
        logPanel.AddThemeStyleboxOverride("panel", logStyle);
        ui.AddChild(logPanel);

        _logLabel = new RichTextLabel();
        _logLabel.BbcodeEnabled = true;
        _logLabel.ScrollFollowing = true;
        _logLabel.FitContent = false;
        _logLabel.AddThemeColorOverride("default_color", new Color("#b6bfdb"));
        _logLabel.AddThemeFontSizeOverride("normal_font_size", 11);
        logPanel.AddChild(_logLabel);

        // Stats bar (bottom)
        _statsLabel = new Label();
        _statsLabel.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        _statsLabel.Position = new Vector2(-300, -30);
        _statsLabel.Size = new Vector2(600, 25);
        _statsLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _statsLabel.AddThemeColorOverride("font_color", new Color("#ecf0ff"));
        _statsLabel.AddThemeFontSizeOverride("font_size", 13);
        ui.AddChild(_statsLabel);

        // Hint label
        var hint = new Label();
        hint.Text = "Esc to quit";
        hint.Position = new Vector2(12, 760);
        hint.AddThemeColorOverride("font_color", new Color(1, 1, 1, 0.3f));
        hint.AddThemeFontSizeOverride("font_size", 11);
        ui.AddChild(hint);
    }

    private void RenderFloor(FloorData floor)
    {
        if (_isHeadless) return;

        // Clear previous
        _tileMap?.QueueFree();
        foreach (var child in _entityLayer.GetChildren())
            child.QueueFree();

        // Load textures
        var floorTex = TestHelper.LoadIssPng("res://assets/isometric/tiles/stone-soup/floors/floor_rect_gray.png");
        var wallTex = TestHelper.LoadIssPng("res://assets/isometric/tiles/stone-soup/walls/brick_gray.png");
        if (floorTex == null || wallTex == null) return;

        var tileSet = new TileSet();
        tileSet.TileShape = TileSet.TileShapeEnum.Isometric;
        tileSet.TileSize = new Vector2I(64, 32);

        // Floor source
        var floorSrc = new TileSetAtlasSource();
        floorSrc.Texture = floorTex;
        floorSrc.TextureRegionSize = new Vector2I(64, 32);
        int floorSrcId = tileSet.AddSource(floorSrc);
        int fCols = floorTex.GetWidth() / 64;
        int fRows = floorTex.GetHeight() / 32;
        for (int ax = 0; ax < fCols; ax++)
            for (int ay = 0; ay < fRows; ay++)
                if (!floorSrc.HasTile(new Vector2I(ax, ay)))
                    floorSrc.CreateTile(new Vector2I(ax, ay));

        // Wall source
        var wallSrc = new TileSetAtlasSource();
        wallSrc.Texture = wallTex;
        wallSrc.TextureRegionSize = new Vector2I(64, 64);
        int wallSrcId = tileSet.AddSource(wallSrc);
        int wCols = wallTex.GetWidth() / 64;
        int wRows = wallTex.GetHeight() / 64;
        for (int ax = 0; ax < wCols; ax++)
            for (int ay = 0; ay < wRows; ay++)
                if (!wallSrc.HasTile(new Vector2I(ax, ay)))
                    wallSrc.CreateTile(new Vector2I(ax, ay));

        _tileMap = new TileMapLayer();
        _tileMap.TileSet = tileSet;
        _tileMap.YSortEnabled = true;
        AddChild(_tileMap);
        MoveChild(_tileMap, 0);

        int fi = 0, fv = fCols * fRows;
        int wi = 0, wv = wCols;
        for (int x = 0; x < floor.Width; x++)
        {
            for (int y = 0; y < floor.Height; y++)
            {
                if (floor.Tiles[x, y] == TileType.Floor)
                {
                    _tileMap.SetCell(new Vector2I(x, y), floorSrcId, new Vector2I(fi % fCols, (fi / fCols) % fRows));
                    fi = (fi + 1) % fv;
                }
                else if (floor.IsFloor(x - 1, y) || floor.IsFloor(x + 1, y) || floor.IsFloor(x, y - 1) || floor.IsFloor(x, y + 1))
                {
                    _tileMap.SetCell(new Vector2I(x, y), wallSrcId, new Vector2I(wi % wv, 0));
                    wi = (wi + 1) % wv;
                }
            }
        }

        // Center camera on entrance
        var entrance = floor.Rooms.FirstOrDefault(r => r.Kind == RoomKind.Entrance);
        if (entrance != null && _tileMap != null)
        {
            _camera.Position = _tileMap.MapToLocal(new Vector2I(entrance.CenterX, entrance.CenterY));
            _camera.Zoom = new Vector2(0.7f, 0.7f);
        }

        // Room labels
        foreach (var room in floor.Rooms)
        {
            var worldPos = _tileMap.MapToLocal(new Vector2I(room.CenterX, room.CenterY));
            var lbl = new Label();
            lbl.Text = room.Kind.ToString();
            lbl.HorizontalAlignment = HorizontalAlignment.Center;
            lbl.AddThemeFontSizeOverride("font_size", 10);
            lbl.Position = worldPos - new Vector2(30, 8);
            lbl.ZIndex = 15;
            Color c = room.Kind switch
            {
                RoomKind.Entrance => new Color(0.3f, 0.9f, 0.4f),
                RoomKind.Exit => new Color(0.9f, 0.3f, 0.3f),
                RoomKind.Boss => new Color(0.9f, 0.2f, 0.9f),
                RoomKind.Treasure => new Color(1.0f, 0.85f, 0.2f),
                RoomKind.Challenge => new Color(1.0f, 0.5f, 0.0f),
                _ => new Color(0.7f, 0.7f, 0.8f)
            };
            lbl.AddThemeColorOverride("font_color", c);
            _entityLayer.AddChild(lbl);
        }
    }

    private void SpawnEnemyMarker(string name, int x, int y, MonsterRarity rarity)
    {
        if (_isHeadless || _tileMap == null) return;
        var worldPos = _tileMap.MapToLocal(new Vector2I(x, y));
        var marker = new Polygon2D();
        marker.Polygon = new Vector2[] { new(0, -8), new(6, 0), new(0, 8), new(-6, 0) };
        marker.Color = rarity switch
        {
            MonsterRarity.Empowered => new Color("#ffde66"),
            MonsterRarity.Named => new Color("#ff6f6f"),
            _ => new Color("#6bff89")
        };
        marker.Position = worldPos;
        marker.ZIndex = 12;
        _entityLayer.AddChild(marker);

        var lbl = new Label();
        lbl.Text = name;
        lbl.AddThemeFontSizeOverride("font_size", 9);
        lbl.AddThemeColorOverride("font_color", marker.Color);
        lbl.Position = worldPos + new Vector2(-20, -18);
        lbl.ZIndex = 15;
        _entityLayer.AddChild(lbl);
    }

    private void ShowFloatingDamage(int damage, bool crit)
    {
        if (_isHeadless || _tileMap == null) return;
        var lbl = new Label();
        lbl.Text = crit ? $"{damage}!" : $"{damage}";
        lbl.AddThemeFontSizeOverride("font_size", crit ? 16 : 12);
        lbl.AddThemeColorOverride("font_color", crit ? new Color("#f5c86b") : new Color("#ffffff"));
        lbl.Position = _camera.Position + new Vector2(_rng.Next(-40, 40), _rng.Next(-30, 10));
        lbl.ZIndex = 20;
        _entityLayer.AddChild(lbl);
        var tw = CreateTween();
        tw.TweenProperty(lbl, "position:y", lbl.Position.Y - 30, 0.4f);
        tw.Parallel().TweenProperty(lbl, "modulate:a", 0.0f, 0.4f);
        tw.TweenCallback(Callable.From(lbl.QueueFree));
    }

    private void UpdateVisualStats()
    {
        if (_isHeadless) return;
        var p = GameState.Player;
        _statsLabel.Text = $"{p.Name} Lv.{p.Level}  HP:{p.HP}/{p.MaxHP}  MP:{p.MP}/{p.MaxMP}  Gold:{p.Gold}  XP:{p.XP}  Kills:{_totalKills}";
    }

    private void SetPhase(string phase)
    {
        if (_isHeadless) return;
        _phaseLabel.Text = phase;
    }

    public override void _Process(double delta)
    {
        if (_complete) return;

        _timer -= (float)delta;
        if (_timer <= 0 && _stepIndex < _steps.Count)
        {
            try
            {
                _steps[_stepIndex].action();
            }
            catch (Exception ex)
            {
                Log($"EXCEPTION at step {_stepIndex}: {ex.Message}");
                Log($"Stack: {ex.StackTrace}");
                Log("=== TEST FAILED ===");
                _complete = true;
                GetTree().Quit(1);
                return;
            }

            _stepIndex++;

            if (_stepIndex < _steps.Count)
                _timer = _steps[_stepIndex].delay;
            else
            {
                _complete = true;
                // Only auto-quit in headless mode; in windowed mode, show summary and wait for Esc
                if (DisplayServer.GetName() == "headless")
                    GetTree().Quit(0);
            }
        }
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (_complete && ev is InputEventKey key && key.Pressed && key.Keycode == Key.Escape)
            GetTree().Quit(0);
    }

    // ==================== LOGGING & ASSERTIONS ====================

    private void Log(string msg)
    {
        GD.Print($"[TEST-GAME] {msg}");
        if (!_isHeadless && _logLabel != null)
        {
            // Color-code different message types
            if (msg.StartsWith("---"))
                _logLabel.AppendText($"[color=#f5c86b]{msg}[/color]\n");
            else if (msg.StartsWith("==="))
                _logLabel.AppendText($"[color=#ff9340][b]{msg}[/b][/color]\n");
            else if (msg.Contains("CRIT"))
                _logLabel.AppendText($"[color=#ffde66]{msg}[/color]\n");
            else if (msg.Contains("FAIL"))
                _logLabel.AppendText($"[color=#ff6f6f]{msg}[/color]\n");
            else if (msg.Contains("Killed") || msg.Contains("LEVEL UP"))
                _logLabel.AppendText($"[color=#6bff89]{msg}[/color]\n");
            else
                _logLabel.AppendText($"{msg}\n");
        }
        UpdateVisualStats();
    }

    private void Assert(bool condition, string label)
    {
        _assertions++;
        if (condition)
        {
            _assertionsPassed++;
        }
        else
        {
            Log($"ASSERT FAILED: {label}");
        }
    }

    // ==================== STEP SETUP ====================

    private void SetupSteps()
    {
        // ── Phase 1: INIT ──
        Step(0.05f, () =>
        {
            SetPhase("FULL GAME LOOP TEST");
            Log("=== FULL GAME LOOP TEST ===");
            Log("");
        });

        Step(0.05f, () =>
        {
            SetPhase("Phase 1: INIT");
            Log("--- Phase 1: INIT ---");
            GameState.Reset();
            GameState.Player.Name = "TestHero";
            var p = GameState.Player;

            Log($"Player: {p.Name} Lv.{p.Level}");
            Log($"  HP: {p.HP}/{p.MaxHP}, MP: {p.MP}/{p.MaxMP}");
            Log($"  Gold: {p.Gold}");
            Log($"  STR:{p.STR} DEX:{p.DEX} INT:{p.INT} VIT:{p.VIT}");

            Assert(p.Name == "TestHero", "Player name is TestHero");
            Assert(p.HP > 0, "Player HP > 0");
            Assert(p.Gold > 0, "Player Gold > 0");
            Assert(p.Level == 1, "Player Level == 1");
        });

        // ── Phase 2: TOWN — NPC SHOPPING ──
        Step(0.05f, () =>
        {
            Log("");
            SetPhase("Phase 2: TOWN — SHOPPING");
            Log("--- Phase 2: TOWN — NPC SHOPPING ---");
            GameState.Location = GameLocation.Town;
            Log("Entered Town");

            int goldBefore = GameState.Player.Gold;

            // Create shop items
            var healthPotion = GameSystems.CreateItem("Health Potion", ItemType.Consumable, EquipSlot.None,
                hpBonus: 50, value: 50, stackable: true, desc: "Restores 50 HP");
            var manaPotion = GameSystems.CreateItem("Mana Potion", ItemType.Consumable, EquipSlot.None,
                mpBonus: 30, value: 50, stackable: true, desc: "Restores 30 MP");

            var (hpOk, hpMsg) = GameSystems.BuyItem(healthPotion);
            Log($"  Buy Health Potion: {hpMsg}");
            Assert(hpOk, "Bought Health Potion");

            var (mpOk, mpMsg) = GameSystems.BuyItem(manaPotion);
            Log($"  Buy Mana Potion: {mpMsg}");
            Assert(mpOk, "Bought Mana Potion");

            int invCount = GameState.Player.Inventory.Count;
            int goldAfter = GameState.Player.Gold;

            Assert(invCount == 2, $"Inventory has 2 items (got {invCount})");
            Assert(goldAfter == goldBefore - 100, $"Gold reduced by 100 (was {goldBefore}, now {goldAfter})");
            Log($"Bought potions, Gold: {goldAfter}, Inventory: {invCount} items");
        });

        // ── Phase 3: ENTER DUNGEON ──
        Step(0.05f, () =>
        {
            Log("");
            SetPhase("Phase 3: ENTER DUNGEON");
            Log("--- Phase 3: ENTER DUNGEON ---");
            GameState.Location = GameLocation.Dungeon;
            GameState.DungeonFloor = 1;

            var gen = new DungeonGenerator();
            _floor1 = gen.Generate(seed: 12345, floorNumber: 1);

            // Render the dungeon floor visually
            RenderFloor(_floor1);

            var (expectedW, expectedH) = DungeonGenerator.CalculateFloorSize(1);
            Log($"Entered Dungeon Floor 1, size {_floor1.Width}x{_floor1.Height}, {_floor1.Rooms.Count} rooms");

            bool hasEntrance = _floor1.Rooms.Any(r => r.Kind == RoomKind.Entrance);
            bool hasExit = _floor1.Rooms.Any(r => r.Kind == RoomKind.Exit || r.Kind == RoomKind.Boss);

            Assert(hasEntrance, "Floor 1 has entrance room");
            Assert(hasExit, "Floor 1 has exit room");
            Assert(_floor1.Width == expectedW, $"Floor 1 width matches CalculateFloorSize ({_floor1.Width} == {expectedW})");
            Assert(_floor1.Height == expectedH, $"Floor 1 height matches CalculateFloorSize ({_floor1.Height} == {expectedH})");
            Assert(_floor1.Rooms.Count > 0, "Floor 1 has rooms");
        });

        // ── Phase 4: SPAWN ENEMIES ──
        Step(0.05f, () =>
        {
            Log("");
            Log("--- Phase 4: SPAWN ENEMIES ---");
            GameState.ActiveMonsters.Clear();

            int normalCount = 0, empoweredCount = 0, namedCount = 0;
            string[] monsterNames = { "Slime", "Goblin", "Skeleton", "Rat", "Bat" };

            var normalRooms = _floor1.Rooms.Where(r => r.Kind == RoomKind.Normal).ToList();
            foreach (var room in normalRooms)
            {
                int spawnCount = _rng.Next(1, 4); // 1-3 monsters
                for (int i = 0; i < spawnCount; i++)
                {
                    string name = monsterNames[_rng.Next(monsterNames.Length)];
                    var rarity = MonsterSpawner.RollRarity(_rng);

                    MonsterTier tier = MonsterTier.Tier1;
                    var monster = GameSystems.SpawnMonster(name, tier);

                    // Apply rarity HP multiplier
                    float hpMult = MonsterSpawner.GetHPMultiplier(rarity);
                    monster.HP = (int)(monster.HP * hpMult);
                    monster.MaxHP = (int)(monster.MaxHP * hpMult);

                    switch (rarity)
                    {
                        case MonsterRarity.Normal: normalCount++; break;
                        case MonsterRarity.Empowered: empoweredCount++; break;
                        case MonsterRarity.Named: namedCount++; break;
                    }

                    Log($"  Spawned {name} [{rarity}] HP:{monster.HP} Dmg:{monster.Damage}");

                    // Roll modifiers for Empowered/Named
                    if (rarity != MonsterRarity.Normal)
                    {
                        int modCount = MonsterSpawner.GetModifierCount(rarity, zone: 1);
                        var mods = MonsterModifiers.RollModifiers(modCount, _rng);
                        var (speed, damage, defense) = MonsterModifiers.GetCombinedEffects(mods);
                        Log($"    Modifiers: [{string.Join(", ", mods)}] speed:{speed:F2}x dmg:{damage:F2}x def:+{defense}");
                    }
                }
            }

            int totalSpawned = GameState.ActiveMonsters.Count;
            Log($"Spawned {totalSpawned} monsters ({normalCount} normal, {empoweredCount} empowered, {namedCount} named)");
            Assert(totalSpawned > 0, "At least 1 monster spawned");
        });

        // ── Phase 5: COMBAT — FLOOR 1 ──
        Step(0.05f, () =>
        {
            Log("");
            Log("--- Phase 5: COMBAT — FLOOR 1 ---");

            var target = GameState.ActiveMonsters.FirstOrDefault(m => !m.IsDead);
            Assert(target != null, "Found a living monster to fight");
            if (target == null) return;

            Log($"Engaging {target.Name} (HP:{target.HP}/{target.MaxHP}, Tier:{target.Tier})");

            int attacks = 0;
            while (!target.IsDead && attacks < 200)
            {
                var (dmg, crit) = GameSystems.AttackMonster(target);
                string critStr = crit ? " CRIT!" : "";
                Log($"  Attack #{attacks + 1}: {dmg} damage{critStr} -> HP:{target.HP}/{target.MaxHP}");

                // Crit system validation
                var critResult = CritSystem.RollCrit(GameState.Player.TotalDamage, WeaponType.Unarmed, _rng);

                // Monster attacks back if alive
                if (!target.IsDead)
                {
                    int monDmg = GameSystems.MonsterAttackPlayer(target);
                    if (GameState.Player.IsDead)
                    {
                        Log($"  Player died! Respawning...");
                        GameSystems.PlayerRespawn();
                        GameState.Location = GameLocation.Dungeon;
                        GameState.DungeonFloor = 1;
                    }
                }

                attacks++;
            }

            Assert(target.IsDead, $"Monster {target.Name} is dead");

            // Grant XP/Gold
            int xpBefore = GameState.Player.XP;
            var (leveled, xpGained) = GameSystems.GainXP(target.XPReward);
            GameState.Player.Gold += target.GoldReward;
            _totalKills++;

            Log($"Killed {target.Name}, gained {target.XPReward} XP, {target.GoldReward} gold");

            // Roll loot
            var loot = ItemGenerator.RollLootDrop((int)target.Tier, GameState.DungeonFloor, _rng);
            if (loot != null)
            {
                GameSystems.AddToInventory(loot);
                Log($"  Loot: {loot.Name} ({loot.Quality} {loot.Type})");
            }
            else
            {
                Log($"  No loot dropped");
            }

            Assert(GameState.Player.XP > 0 || GameState.Player.Level > 1, "Player gained XP or leveled up");
        });

        // ── Phase 6: LEVEL CHECK & CLEAR FLOOR ──
        Step(0.05f, () =>
        {
            Log("");
            Log("--- Phase 6: LEVEL CHECK ---");

            // Force a level up by granting enough XP
            int needed = GameState.Player.XPToNextLevel - GameState.Player.XP;
            if (needed > 0)
            {
                var (leveled, _) = GameSystems.GainXP(needed);
                if (leveled)
                    Log($"LEVEL UP! Now Level {GameState.Player.Level}");
            }

            var p = GameState.Player;
            Log($"Stats: HP:{p.HP}/{p.MaxHP} MP:{p.MP}/{p.MaxMP} XP:{p.XP} Level:{p.Level}");
            Assert(p.Level >= 2, "Player reached at least level 2");

            // Kill remaining monsters (abbreviated)
            int remaining = GameState.ActiveMonsters.Count(m => !m.IsDead);
            foreach (var monster in GameState.ActiveMonsters.Where(m => !m.IsDead))
            {
                while (!monster.IsDead)
                {
                    GameSystems.AttackMonster(monster);
                    if (!monster.IsDead && GameState.Player.HP < 20)
                    {
                        GameState.Player.HP = GameState.Player.MaxHP; // heal for test continuity
                    }
                }
                _totalKills++;
                GameSystems.GainXP(monster.XPReward);
                GameState.Player.Gold += monster.GoldReward;
            }

            Log($"Floor 1 cleared, {_totalKills} enemies defeated");
        });

        // ── Phase 7: FLOOR TRANSITION ──
        Step(0.05f, () =>
        {
            Log("");
            Log("--- Phase 7: FLOOR TRANSITION ---");

            GameState.DungeonFloor = 2;
            var gen = new DungeonGenerator();
            _floor2 = gen.Generate(seed: 54321, floorNumber: 2);

            Log($"Advanced to Floor 2, size {_floor2.Width}x{_floor2.Height}");

            bool hasEntrance = _floor2.Rooms.Any(r => r.Kind == RoomKind.Entrance);
            bool hasExit = _floor2.Rooms.Any(r => r.Kind == RoomKind.Exit || r.Kind == RoomKind.Boss);
            Assert(hasEntrance, "Floor 2 has entrance");
            Assert(hasExit, "Floor 2 has exit");

            // Spawn enemies on floor 2
            GameState.ActiveMonsters.Clear();
            var monster = GameSystems.SpawnMonster("Goblin Scout", MonsterTier.Tier2);
            Log($"  Spawned {monster.Name} HP:{monster.HP} Tier:{monster.Tier}");

            // Kill 1 enemy on floor 2
            while (!monster.IsDead)
            {
                GameSystems.AttackMonster(monster);
                if (!monster.IsDead && GameState.Player.HP < 20)
                    GameState.Player.HP = GameState.Player.MaxHP;
            }
            _totalKills++;
            GameSystems.GainXP(monster.XPReward);
            GameState.Player.Gold += monster.GoldReward;

            Log("Floor 2 combat verified");
        });

        // ── Phase 8: SAVE ON FLOOR 2 ──
        Step(0.05f, () =>
        {
            Log("");
            Log("--- Phase 8: SAVE ON FLOOR 2 ---");

            _savedData = SaveSerializer.Serialize(slot: 1);
            Assert(_savedData != null, "Serialized data is not null");
            Assert(_savedData.Count > 0, "Serialized data is not empty");

            // Verify key fields in serialized data
            var charDict = _savedData["character"] as Dictionary<string, object>;
            Assert(charDict != null, "Character data exists in save");
            Assert(charDict["name"].ToString() == "TestHero", "Saved name is TestHero");

            int savedFloor = Convert.ToInt32(_savedData["dungeon_floor"]);
            Assert(savedFloor == 2, $"Saved floor is 2 (got {savedFloor})");

            int savedLevel = Convert.ToInt32(charDict["level"]);
            Assert(savedLevel >= 2, $"Saved level >= 2 (got {savedLevel})");

            Log($"Game saved on Floor 2 — name:{charDict["name"]}, level:{charDict["level"]}, floor:{savedFloor}");
        });

        // ── Phase 9: SIMULATE LOAD ──
        Step(0.05f, () =>
        {
            Log("");
            Log("--- Phase 9: SIMULATE LOAD ---");

            // Store expected values
            var expectedName = "TestHero";
            var charDict = _savedData["character"] as Dictionary<string, object>;
            int expectedLevel = Convert.ToInt32(charDict["level"]);
            int expectedFloor = Convert.ToInt32(_savedData["dungeon_floor"]);

            // Wipe everything
            GameState.Reset();
            Assert(GameState.Player.Level == 1, "Reset: player is level 1");
            Assert(GameState.Player.Name == "Hero", "Reset: player name is default");
            Log("  State wiped (reset confirmed)");

            // Deserialize
            bool loaded = SaveSerializer.Deserialize(_savedData);
            Assert(loaded, "Deserialization succeeded");

            Assert(GameState.Player.Name == expectedName, $"Loaded name: {GameState.Player.Name}");
            Assert(GameState.Player.Level == expectedLevel, $"Loaded level: {GameState.Player.Level}");
            Assert(GameState.DungeonFloor == expectedFloor, $"Loaded floor: {GameState.DungeonFloor}");

            int invCount = GameState.Player.Inventory.Count;
            Log($"Game loaded — {GameState.Player.Name} Lv.{GameState.Player.Level} Floor {GameState.DungeonFloor}, {invCount} items");
        });

        // ── Phase 10: RETURN TO TOWN ──
        Step(0.05f, () =>
        {
            Log("");
            Log("--- Phase 10: RETURN TO TOWN ---");
            GameSystems.ExitDungeon();
            Assert(GameState.Location == GameLocation.Town, "Location is Town");
            Assert(GameState.DungeonFloor == 0, "Dungeon floor is 0");
            Log("Returned to Town");
        });

        // ── Phase 11: BANK OPERATIONS ──
        Step(0.05f, () =>
        {
            Log("");
            Log("--- Phase 11: BANK OPERATIONS ---");

            var bank = new BankData();

            // Ensure player has at least one item to deposit
            if (GameState.Player.Inventory.Count == 0)
            {
                var testItem = GameSystems.CreateItem("Test Gem", ItemType.Material, EquipSlot.None, value: 10);
                GameSystems.AddToInventory(testItem);
            }

            var itemToDeposit = GameState.Player.Inventory[0];
            string itemName = itemToDeposit.Name;
            int invBefore = GameState.Player.Inventory.Count;

            var (depOk, depMsg) = BankSystem.Deposit(bank, GameState.Player, itemToDeposit);
            Log($"  Deposit: {depMsg}");
            Assert(depOk, "Deposit succeeded");
            Assert(bank.Items.Count >= 1, "Bank has at least 1 item");
            Assert(GameState.Player.Inventory.Count == invBefore - 1, "Inventory reduced by 1");

            // Withdraw
            var bankItem = bank.Items[0];
            var (witOk, witMsg) = BankSystem.Withdraw(bank, GameState.Player, bankItem);
            Log($"  Withdraw: {witMsg}");
            Assert(witOk, "Withdraw succeeded");
            Assert(bank.Items.Count == 0, "Bank is empty");
            Assert(GameState.Player.Inventory.Count == invBefore, "Inventory restored");

            Log("Bank deposit/withdraw verified");
        });

        // ── Phase 12: BACKPACK EXPANSION ──
        Step(0.05f, () =>
        {
            Log("");
            Log("--- Phase 12: BACKPACK EXPANSION ---");

            int cost = BackpackSystem.GetExpansionCost(GameState.Player);
            int sizeBefore = GameState.Player.InventorySize;

            if (GameState.Player.Gold >= cost)
            {
                var (ok, msg) = BackpackSystem.Expand(GameState.Player);
                Assert(ok, "Backpack expansion succeeded");
                Log($"Backpack expanded to {GameState.Player.InventorySize} slots (cost: {cost}g)");
            }
            else
            {
                // Give gold for the test
                GameState.Player.Gold += cost;
                var (ok, msg) = BackpackSystem.Expand(GameState.Player);
                Assert(ok, "Backpack expansion succeeded (with added gold)");
                Log($"Backpack expanded to {GameState.Player.InventorySize} slots (cost: {cost}g)");
            }

            Assert(GameState.Player.InventorySize == sizeBefore + BackpackSystem.SlotsPerExpansion,
                $"Backpack size increased by {BackpackSystem.SlotsPerExpansion}");
        });

        // ── Phase 13: SAVE IN TOWN ──
        Step(0.05f, () =>
        {
            Log("");
            Log("--- Phase 13: SAVE IN TOWN ---");

            Assert(GameState.Location == GameLocation.Town, "Location is Town before save");
            _savedData = SaveSerializer.Serialize(slot: 1);

            Assert(_savedData != null, "Town save data not null");
            int loc = Convert.ToInt32(_savedData["location"]);
            Assert(loc == (int)GameLocation.Town, $"Saved location is Town (got {loc})");

            Log("Saved in Town");
        });

        // ── Phase 14: SIMULATE RELOAD ──
        Step(0.05f, () =>
        {
            Log("");
            Log("--- Phase 14: SIMULATE RELOAD ---");

            GameState.Reset();
            bool loaded = SaveSerializer.Deserialize(_savedData);
            Assert(loaded, "Town reload deserialization succeeded");
            Assert(GameState.Location == GameLocation.Town, "Reloaded location is Town");
            Assert(GameState.Player.Name == "TestHero", "Reloaded name is TestHero");
            Assert(GameState.Player.Level >= 2, $"Reloaded level >= 2 (got {GameState.Player.Level})");

            Log($"Reloaded in Town — all systems verified");
        });

        // ── Phase 15: NEW SYSTEMS VALIDATION ──
        Step(0.05f, () =>
        {
            Log("");
            Log("--- Phase 15: NEW SYSTEMS VALIDATION ---");

            // Test ElementalCombat: fire damage vs target with 50% fire resistance
            var target = new EntityData
            {
                Name = "FireRes Dummy",
                HP = 1000, MaxHP = 1000,
                BaseDefense = 0,
                Resistances = new Resistances { Fire = 50 }
            };
            var elemResult = ElementalCombat.CalculateDamage(100, DamageType.Fire, target, floorNumber: 1);
            Log($"  ElementalCombat: 100 Fire dmg vs 50% fire res -> {elemResult.FinalDamage} final (raw:{elemResult.RawDamage}, res:{elemResult.EffectiveResistance})");
            Assert(elemResult.FinalDamage < 100, "Fire damage reduced by resistance");
            Assert(elemResult.FinalDamage > 0, "Fire damage is positive");

            // Test CritSystem: roll 1000 crits with Dagger, verify rate ~8%
            int critCount = 0;
            int sampleSize = 1000;
            var critRng = new Random(123);
            for (int i = 0; i < sampleSize; i++)
            {
                var cr = CritSystem.RollCrit(50, WeaponType.Dagger, critRng);
                if (cr.IsCrit) critCount++;
            }
            float critRate = critCount / (float)sampleSize * 100f;
            Log($"  CritSystem: Dagger crit rate = {critRate:F1}% over {sampleSize} rolls (expected ~8%)");
            Assert(critRate > 3f && critRate < 15f, $"Dagger crit rate in reasonable range ({critRate:F1}%)");

            // Test MonsterBehavior: state machine chain
            // Idle -> Alert (within aggro range, melee archetype)
            var state = MonsterBehavior.GetNextState(MonsterAIState.Idle, MonsterArchetype.Melee,
                distanceToPlayer: 300f, currentHP: 100, maxHP: 100, alertTimer: 0.3f, cooldownTimer: 0);
            Log($"  MonsterBehavior: Idle + in range -> {state}");
            Assert(state == MonsterAIState.Alert, $"Idle -> Alert (got {state})");

            // Alert -> Chase (timer expired)
            state = MonsterBehavior.GetNextState(MonsterAIState.Alert, MonsterArchetype.Melee,
                distanceToPlayer: 300f, currentHP: 100, maxHP: 100, alertTimer: 0f, cooldownTimer: 0);
            Log($"  MonsterBehavior: Alert + timer 0 -> {state}");
            Assert(state == MonsterAIState.Chase, $"Alert -> Chase (got {state})");

            // Chase -> Attack (within attack range)
            state = MonsterBehavior.GetNextState(MonsterAIState.Chase, MonsterArchetype.Melee,
                distanceToPlayer: 20f, currentHP: 100, maxHP: 100, alertTimer: 0, cooldownTimer: 0);
            Log($"  MonsterBehavior: Chase + close -> {state}");
            Assert(state == MonsterAIState.Attack, $"Chase -> Attack (got {state})");

            // Attack -> Cooldown
            state = MonsterBehavior.GetNextState(MonsterAIState.Attack, MonsterArchetype.Melee,
                distanceToPlayer: 20f, currentHP: 100, maxHP: 100, alertTimer: 0, cooldownTimer: 1.0f);
            Log($"  MonsterBehavior: Attack -> {state}");
            Assert(state == MonsterAIState.Cooldown, $"Attack -> Cooldown (got {state})");

            // Cooldown -> Chase (cooldown expired, melee has no preferred distance)
            state = MonsterBehavior.GetNextState(MonsterAIState.Cooldown, MonsterArchetype.Melee,
                distanceToPlayer: 20f, currentHP: 100, maxHP: 100, alertTimer: 0, cooldownTimer: 0);
            Log($"  MonsterBehavior: Cooldown expired -> {state}");
            Assert(state == MonsterAIState.Chase, $"Cooldown -> Chase (got {state})");

            // Dead state
            state = MonsterBehavior.GetNextState(MonsterAIState.Chase, MonsterArchetype.Melee,
                distanceToPlayer: 20f, currentHP: 0, maxHP: 100, alertTimer: 0, cooldownTimer: 0);
            Log($"  MonsterBehavior: HP=0 -> {state}");
            Assert(state == MonsterAIState.Dead, $"HP 0 -> Dead (got {state})");

            // Test MonsterSpawner: GetArchetypeMix
            var mix = MonsterSpawner.GetArchetypeMix(budget: 10, _rng);
            int totalBudget = mix.Values.Sum();
            Log($"  MonsterSpawner: budget=10 -> {string.Join(", ", mix.Select(kv => $"{kv.Key}:{kv.Value}"))} (total:{totalBudget})");
            Assert(totalBudget == 10, $"Archetype mix sums to budget (got {totalBudget})");
            Assert(mix.Count > 0, "Archetype mix has entries");
        });

        // ── Phase 16: SUMMARY ──
        Step(0.05f, () =>
        {
            Log("");
            Log("=== TEST COMPLETE ===");
            Log($"Total steps: {_stepIndex + 1}");
            Log($"Total assertions: {_assertions} ({_assertionsPassed} passed, {_assertions - _assertionsPassed} failed)");
            Log($"Total kills: {_totalKills}");

            var p = GameState.Player;
            Log($"Final stats: {p.Name} Lv.{p.Level} HP:{p.HP}/{p.MaxHP} MP:{p.MP}/{p.MaxMP} Gold:{p.Gold}");
            Log($"  STR:{p.STR} DEX:{p.DEX} INT:{p.INT} VIT:{p.VIT}");
            Log($"  Inventory: {p.Inventory.Count} items, Equipment: {p.Equipment.Count} slots");

            if (_assertionsPassed == _assertions)
                Log("ALL SYSTEMS OPERATIONAL");
            else
                Log($"WARNING: {_assertions - _assertionsPassed} assertion(s) failed");
        });
    }

    private void Step(float delay, Action action)
    {
        _steps.Add((delay, action));
    }
}
