using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Automated demo that exercises every basic game mechanic.
/// No user input — all events are scripted and triggered sequentially.
/// Each step calls GameSystems methods and logs the result.
/// </summary>
public partial class GameDemo : Node2D
{
    private const int TileSize = 32;
    private const int RoomWidth = 15;
    private const int RoomHeight = 11;
    private const float MoveSpeed = 150f;
    private static readonly string AssetBase =
        "res://assets/tilesets/dungeon-crawl/dcss-full/Dungeon Crawl Stone Soup Full/";

    // Scene nodes
    private Node2D _character;
    private Node2D _entityLayer;
    private CanvasLayer _uiLayer;
    private Label _statsLabel;
    private Label _logLabel;
    private Panel _gameWindow;
    private Panel _shopWindow;
    private Panel _dialogWindow;

    // Demo state
    private readonly List<(float delay, Action action)> _steps = new();
    private int _stepIndex;
    private float _timer;
    private bool _demoComplete;

    // Movement animation
    private bool _isMoving;
    private Vector2 _moveTarget;

    // Active entity sprites (monsters, NPCs, chests)
    private readonly Dictionary<string, Node2D> _entitySprites = new();

    // HP/MP orbs
    private HpMpOrbs _hpMpOrbs;

    // UI - XP bar
    private ProgressBar _xpBar;
    private Label _xpLabel;
    // UI - Toast notifications
    private VBoxContainer _toastContainer;
    // UI - Panels (hidden until showcase)
    private Panel _inventoryPanel;
    private Panel _equipPanel;
    private Panel _settingsPanel;
    // UI - Tooltip
    private Panel _tooltip;
    // UI - Death overlay
    private ColorRect _deathOverlay;
    // UI - Shortcut bar
    private HBoxContainer _shortcutBar;
    // UI - Perf overlay
    private Panel _perfOverlay;
    private Label _perfLabel;
    private bool _perfVisible;

    // Perf benchmarks
    private readonly List<(string name, double usec)> _benchResults = new();

    // On-screen log
    private readonly List<string> _logLines = new();

    // ==================== LIFECYCLE ====================

    public override void _Ready()
    {
        GD.Print("");
        GD.Print("========================================");
        GD.Print("  GAME SYSTEMS DEMO");
        GD.Print("  Automated test of all basic mechanics");
        GD.Print("========================================");
        GD.Print("");

        BuildScene();
        InitializeGame();
        SetupDemoSteps();

        _stepIndex = 0;
        _timer = 1.5f; // initial delay before first step

        // In headless mode, run demo at 10x speed for E2E testing
        if (DisplayServer.GetName() == "headless")
        {
            for (int i = 0; i < _steps.Count; i++)
                _steps[i] = (0.01f, _steps[i].action);
        }
    }

    public override void _Process(double delta)
    {
        if (_demoComplete) return;

        // Update perf overlay every frame
        UpdatePerfOverlay();

        // Animate character movement
        if (_isMoving && _character != null)
        {
            var moveAmount = MoveSpeed * (float)delta;
            var remaining = _character.Position.DistanceTo(_moveTarget);
            if (remaining <= moveAmount)
            {
                _character.Position = _moveTarget;
                _isMoving = false;
            }
            else
            {
                _character.Position += (_moveTarget - _character.Position).Normalized() * moveAmount;
            }
        }

        // Step timer
        _timer -= (float)delta;
        if (_timer <= 0 && _stepIndex < _steps.Count)
        {
            _steps[_stepIndex].action();
            UpdateStatsDisplay();
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
        // Entity layer (holds monsters, NPCs, chests — rendered above floor, below character)
        _entityLayer = new Node2D();
        _entityLayer.ZIndex = 5;
        AddChild(_entityLayer);

        // Room
        DrawFloor();
        DrawWalls();

        // Character (paper doll)
        _character = CreateCharacter();

        // UI overlay
        _uiLayer = new CanvasLayer();
        AddChild(_uiLayer);

        // Stats bar (top)
        _statsLabel = new Label();
        _statsLabel.Position = new Vector2(8, 4);
        _statsLabel.AddThemeColorOverride("font_color", Colors.White);
        _statsLabel.AddThemeFontSizeOverride("font_size", 16);
        _uiLayer.AddChild(_statsLabel);

        // Event log (bottom)
        _logLabel = new Label();
        _logLabel.Position = new Vector2(8, 360);
        _logLabel.AddThemeColorOverride("font_color", new Color(0.75f, 1.0f, 0.75f));
        _logLabel.AddThemeFontSizeOverride("font_size", 12);
        _uiLayer.AddChild(_logLabel);

        // HP/MP orbs (Diablo-style)
        _hpMpOrbs = new HpMpOrbs();
        _hpMpOrbs.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _uiLayer.AddChild(_hpMpOrbs);
        var p0 = GameState.Player;
        _hpMpOrbs.UpdateValues(p0.HP, p0.MaxHP, p0.MP, p0.MaxMP);

        // Styled window UIs (dark fantasy theme matching scene-tree.md HUD spec)
        _gameWindow = CreateStyledWindow("Stats", new Vector2(640, 280), new Vector2(640, 300));
        _uiLayer.AddChild(_gameWindow);

        _shopWindow = CreateStyledWindow("Item Shop", new Vector2(560, 240), new Vector2(800, 440));
        _uiLayer.AddChild(_shopWindow);

        _dialogWindow = CreateStyledWindow("Dialog", new Vector2(460, 860), new Vector2(1000, 140));
        _uiLayer.AddChild(_dialogWindow);

        // XP progress bar (bottom center, between orbs)
        CreateXPBar();

        // Toast notification container (top right)
        CreateToastContainer();

        // Shortcut bar (bottom center, above XP bar)
        CreateShortcutBar();

        // Panels (hidden until UI showcase phase)
        _inventoryPanel = CreateInventoryPanel();
        _uiLayer.AddChild(_inventoryPanel);

        _equipPanel = CreateEquipmentPanel();
        _uiLayer.AddChild(_equipPanel);

        _settingsPanel = CreateSettingsPanel();
        _uiLayer.AddChild(_settingsPanel);

        // Tooltip (hidden)
        CreateTooltip();

        // Death screen overlay (hidden)
        CreateDeathOverlay();

        // Performance monitor overlay (top right)
        CreatePerfOverlay();
    }

    private void InitializeGame()
    {
        GameState.Reset();
        GameState.Player.Name = "Demo Hero";
        GameState.Player.Gold = 150;

        // Starting skill: Slash
        GameState.PlayerSkills.Add(new SkillData
        {
            Name = "Slash",
            Description = "Wide arc melee attack",
            ManaCost = 15,
            BaseDamage = 25,
            Cooldown = 3.0f,
            Level = 1,
        });
    }

    // ==================== DEMO STEPS ====================

    private void SetupDemoSteps()
    {
        // ──────── PHASE 1: TOWN ────────

        Step(1.5f, () =>
        {
            Log("========================================");
            Log("  PHASE 1: TOWN");
            Log("========================================");
        });

        Step(1.0f, () =>
        {
            var p = GameState.Player;
            Log($"[INIT] Player: {p.Name} Lv.{p.Level}");
            Log($"  HP: {p.HP}/{p.MaxHP} | MP: {p.MP}/{p.MaxMP} | Gold: {p.Gold}");
            Log($"  STR:{p.STR} DEX:{p.DEX} INT:{p.INT} VIT:{p.VIT}");
            Log($"  Inventory: {p.Inventory.Count}/{p.InventorySize} slots");
            Log($"  Skills: {GameState.PlayerSkills.Count} ({GameState.PlayerSkills[0].Name})");
        });

        // 1. Movement
        Step(0.8f, () => { Log("[MOVE] Moving UP"); MoveTo(_character.Position + Vector2.Up * 48); });
        Step(0.6f, () => { Log("[MOVE] Moving DOWN"); MoveTo(_character.Position + Vector2.Down * 48); });
        Step(0.6f, () => { Log("[MOVE] Moving LEFT"); MoveTo(_character.Position + Vector2.Left * 48); });
        Step(0.6f, () => { Log("[MOVE] Moving RIGHT"); MoveTo(_character.Position + Vector2.Right * 48); });

        // 2. Open/close game window
        Step(1.0f, () =>
        {
            var p = GameState.Player;
            Log("[UI] Opening Stats Panel...");
            ShowWindow(_gameWindow, $"STR: {p.STR}    DEX: {p.DEX}\nINT: {p.INT}    VIT: {p.VIT}\n\nDamage:  {p.TotalDamage}\nDefense: {p.TotalDefense}\n\nEquipment: {p.Equipment.Count} slots\nInventory: {p.Inventory.Count}/{p.InventorySize}");
        });
        Step(1.0f, () => { Log("[UI] Closing Stats Panel."); HideAllWindows(); });

        // 3. Change setting
        Step(1.0f, () =>
        {
            var result = GameSystems.ChangeTargetPriority(TargetPriority.Strongest);
            Log($"[SETTINGS] {result}");
        });

        // 4. NPC dialog
        Step(0.8f, () =>
        {
            Log("[NPC] Approaching the Old Sage...");
            SpawnEntity("sage", _character.Position + new Vector2(-64, 0), "monster/wizard.png");
        });
        Step(1.2f, () =>
        {
            Log("[DIALOG] Old Sage: \"Welcome, adventurer.\"");
            Log("[DIALOG] Old Sage: \"The dungeon grows more dangerous each floor.\"");
            Log("[DIALOG] Old Sage: \"Buy supplies before you descend.\"");
            ClearWindowIcons(_dialogWindow);
            AddIconToWindow(_dialogWindow, "monster/wizard.png", new Vector2(14, 36), new Vector2(48, 48));
            ShowWindow(_dialogWindow, "         Old Sage:\n         \"Welcome, adventurer. The dungeon grows more\n          dangerous each floor. Buy supplies before you descend.\"");
        });
        Step(0.5f, () => { HideAllWindows(); RemoveEntity("sage"); });

        // 5. Shop — buy items
        Step(0.8f, () =>
        {
            Log("[SHOP] Approaching Item Shop...");
            SpawnEntity("shopkeeper", _character.Position + new Vector2(64, 0), "monster/deep_dwarf.png");
        });
        Step(1.2f, () =>
        {
            var sword = GameSystems.CreateItem("Iron Sword", ItemType.Weapon, EquipSlot.MainHand, damage: 8, value: 50, desc: "+8 damage");
            var (ok1, r1) = GameSystems.BuyItem(sword);
            Log($"[SHOP] {r1}");

            var potion = GameSystems.CreateItem("Health Potion", ItemType.Consumable, EquipSlot.None, hpBonus: 30, value: 10, stackable: true);
            var (ok2, r2) = GameSystems.BuyItem(potion, 3);
            Log($"[SHOP] {r2}");

            var cap = GameSystems.CreateItem("Leather Cap", ItemType.Armor, EquipSlot.Head, defense: 3, value: 20, desc: "+3 defense");
            var (ok3, r3) = GameSystems.BuyItem(cap);
            Log($"[SHOP] {r3}");

            Log($"  Remaining gold: {GameState.Player.Gold}");
            Log($"  Inventory: {GameState.Player.Inventory.Count} items");

            // Show shop window with item icons
            ClearWindowIcons(_shopWindow);
            AddIconToWindow(_shopWindow, "item/weapon/short_sword_1_new.png", new Vector2(14, 40), new Vector2(32, 32));
            AddIconToWindow(_shopWindow, "item/potion/emerald.png", new Vector2(14, 80), new Vector2(32, 32));
            AddIconToWindow(_shopWindow, "item/armor/headgear/helmet_5.png", new Vector2(14, 120), new Vector2(32, 32));
            AddIconToWindow(_shopWindow, "item/gold/gold_pile.png", new Vector2(14, 170), new Vector2(32, 32));
            ShowWindow(_shopWindow,
                "       Iron Sword       +8 dmg       50g\n\n" +
                "       Health Potion x3  +30 HP       30g\n\n" +
                "       Leather Cap       +3 def       20g\n\n" +
                $"       Gold remaining: {GameState.Player.Gold}g");
        });
        Step(0.5f, () => { HideAllWindows(); RemoveEntity("shopkeeper"); });

        // 6. Equip items
        Step(1.0f, () =>
        {
            var sword = GameState.Player.Inventory.Find(i => i.Name == "Iron Sword");
            if (sword != null)
            {
                GameSystems.EquipItem(sword);
                Log($"[EQUIP] Equipped Iron Sword (+8 damage)");
                Log($"  Total damage: {GameState.Player.TotalDamage}");
            }
        });
        Step(0.8f, () =>
        {
            var cap = GameState.Player.Inventory.Find(i => i.Name == "Leather Cap");
            if (cap != null)
            {
                GameSystems.EquipItem(cap);
                Log($"[EQUIP] Equipped Leather Cap (+3 defense)");
                Log($"  Total defense: {GameState.Player.TotalDefense}");
            }
        });

        // ──────── PHASE 2: DUNGEON ────────

        Step(1.5f, () =>
        {
            Log("");
            Log("========================================");
            Log("  PHASE 2: DUNGEON");
            Log("========================================");
            GameSystems.EnterDungeon();
            Log($"[DUNGEON] Entered the dungeon! Floor {GameState.DungeonFloor}");
        });

        Step(0.8f, () => { Log("[MOVE] Exploring the dungeon..."); MoveTo(_character.Position + new Vector2(48, 0)); });

        // 7. World interaction — chest
        Step(1.0f, () =>
        {
            Log("[WORLD] Found a treasure chest!");
            SpawnEntity("chest", _character.Position + new Vector2(48, 0), "dungeon/chest.png");
        });
        Step(1.0f, () =>
        {
            var armor = GameSystems.CreateItem("Leather Armor", ItemType.Armor, EquipSlot.Body, defense: 5, value: 40, desc: "+5 defense");
            GameSystems.AddToInventory(armor);
            Log("[CHEST] Opened chest -> Got Leather Armor (+5 defense)");
            RemoveEntity("chest");
        });
        Step(0.8f, () =>
        {
            var armor = GameState.Player.Inventory.Find(i => i.Name == "Leather Armor");
            if (armor != null)
            {
                GameSystems.EquipItem(armor);
                Log($"[EQUIP] Equipped Leather Armor (+5 defense)");
                Log($"  Total defense: {GameState.Player.TotalDefense}");
            }
        });

        // 8. Combat — Giant Rat (Tier 1)
        Step(1.2f, () =>
        {
            var rat = GameSystems.SpawnMonster("Giant Rat", MonsterTier.Tier1);
            SpawnEntity("rat", _character.Position + new Vector2(64, 0), "monster/animals/rat.png");
            Log($"[SPAWN] A Giant Rat appears!");
            Log($"  HP:{rat.HP}/{rat.MaxHP} | Tier:{(int)rat.Tier} | XP:{rat.XPReward}");
        });

        Step(0.8f, () => { Log("[TARGET] Auto-targeting nearest: Giant Rat"); });

        // 9. Basic attack
        Step(1.0f, () =>
        {
            var rat = GameState.ActiveMonsters.Find(m => m.Name == "Giant Rat");
            var (dmg, crit) = GameSystems.AttackMonster(rat);
            Log($"[ATTACK] Basic attack -> {dmg} damage{(crit ? " CRITICAL!" : "")} to Giant Rat");
            Log($"  Rat HP: {rat.HP}/{rat.MaxHP}");
            if (_entitySprites.TryGetValue("rat", out var ratSprite))
            {
                ShowSlashEffect(ratSprite.Position);
                FlashEntity("rat", Colors.White);
                ShowFloatingText(ratSprite.Position, $"-{dmg}", crit ? Colors.Yellow : Colors.White, crit);
            }
        });

        // 10. Monster attacks player
        Step(1.0f, () =>
        {
            var rat = GameState.ActiveMonsters.Find(m => m.Name == "Giant Rat");
            var dmg = GameSystems.MonsterAttackPlayer(rat);
            Log($"[HIT] Giant Rat attacks -> {dmg} damage to player");
            Log($"  Player HP: {GameState.Player.HP}/{GameState.Player.MaxHP}");
            FlashCharacter(new Color(1, 0.3f, 0.3f));
            ShowFloatingText(_character.Position, $"-{dmg}", Colors.Red);
        });

        // 11. Kill rat
        Step(1.0f, () =>
        {
            var rat = GameState.ActiveMonsters.Find(m => m.Name == "Giant Rat");
            rat.HP = Math.Min(rat.HP, 5); // ensure kill
            var (dmg, crit) = GameSystems.AttackMonster(rat);
            Log($"[ATTACK] Finishing blow -> {dmg} damage -> Giant Rat DEFEATED!");

            // XP + gold + loot
            var (leveled, _) = GameSystems.GainXP(rat.XPReward);
            GameState.Player.Gold += rat.GoldReward;
            Log($"  +{rat.XPReward} XP | +{rat.GoldReward} Gold");
            Log($"  XP: {GameState.Player.XP}/{GameState.Player.XPToNextLevel}");

            var fang = GameSystems.CreateItem("Rat Fang", ItemType.Material, EquipSlot.None, value: 5, desc: "Crafting material");
            GameSystems.AddToInventory(fang);
            Log($"  Loot: Rat Fang (material)");

            RemoveEntity("rat");
            GameState.ActiveMonsters.Remove(rat);
        });

        // 12. Combat — Skeleton Warrior (Tier 2)
        Step(1.2f, () =>
        {
            var skel = GameSystems.SpawnMonster("Skeleton Warrior", MonsterTier.Tier2);
            SpawnEntity("skeleton", _character.Position + new Vector2(64, -16), "monster/undead/skeletons/skeleton_humanoid_large_new.png");
            Log($"[SPAWN] A Skeleton Warrior appears!");
            Log($"  HP:{skel.HP}/{skel.MaxHP} | Tier:{(int)skel.Tier} | XP:{skel.XPReward}");
        });

        // 13. Use skill (MP cost)
        Step(1.0f, () =>
        {
            var skel = GameState.ActiveMonsters.Find(m => m.Name == "Skeleton Warrior");
            var skill = GameState.PlayerSkills[0];
            var (dmg, ok) = GameSystems.UseSkill(skill, skel);
            if (ok)
            {
                Log($"[SKILL] Used {skill.Name}! (-{skill.ManaCost} MP) -> {dmg} damage");
                Log($"  MP: {GameState.Player.MP}/{GameState.Player.MaxMP}");
                Log($"  Skeleton HP: {skel.HP}/{skel.MaxHP}");
                if (_entitySprites.TryGetValue("skeleton", out var skelSprite))
                {
                    ShowSkillBurst(skelSprite.Position, new Color(0.4f, 0.6f, 1.0f));
                    FlashEntity("skeleton", new Color(0.5f, 0.7f, 1.0f));
                    ShowFloatingText(skelSprite.Position, $"-{dmg}", new Color(0.4f, 0.7f, 1.0f));
                }
            }
        });

        // 14. Skeleton attacks
        Step(1.0f, () =>
        {
            var skel = GameState.ActiveMonsters.Find(m => m.Name == "Skeleton Warrior");
            var dmg = GameSystems.MonsterAttackPlayer(skel);
            Log($"[HIT] Skeleton attacks -> {dmg} damage");
            Log($"  Player HP: {GameState.Player.HP}/{GameState.Player.MaxHP}");
        });

        // 15. Use health potion
        Step(1.0f, () =>
        {
            var potion = GameState.Player.Inventory.Find(i => i.Name == "Health Potion");
            if (potion != null)
            {
                var (ok, effect) = GameSystems.UseItem(potion);
                Log($"[ITEM] Used Health Potion -> {effect}");
                Log($"  HP: {GameState.Player.HP}/{GameState.Player.MaxHP} | Potions left: {potion.StackCount}");
                ShowFloatingText(_character.Position, "+30 HP", new Color(0.3f, 1.0f, 0.3f));
            }
        });

        // 16. Kill skeleton
        Step(1.0f, () =>
        {
            var skel = GameState.ActiveMonsters.Find(m => m.Name == "Skeleton Warrior");
            skel.HP = Math.Min(skel.HP, 8);
            var (dmg, crit) = GameSystems.AttackMonster(skel);
            Log($"[ATTACK] Attack -> {dmg} damage{(crit ? " CRITICAL!" : "")} -> Skeleton DEFEATED!");

            var (leveled, _) = GameSystems.GainXP(skel.XPReward);
            GameState.Player.Gold += skel.GoldReward;
            Log($"  +{skel.XPReward} XP | +{skel.GoldReward} Gold");

            var ring = GameSystems.CreateItem("Iron Ring", ItemType.Accessory, EquipSlot.Ring, hpBonus: 10, value: 25, desc: "+10 Max HP");
            GameSystems.AddToInventory(ring);
            Log($"  Loot: Iron Ring (+10 Max HP)");

            RemoveEntity("skeleton");
            GameState.ActiveMonsters.Remove(skel);
        });

        // 17. Level up
        Step(1.5f, () =>
        {
            // Force level up for demo
            int needed = GameState.Player.XPToNextLevel - GameState.Player.XP;
            if (needed > 0) GameSystems.GainXP(needed);

            var p = GameState.Player;
            Log("========================================");
            Log($"  >> LEVEL UP! Now Level {p.Level} <<");
            Log("========================================");
            Log($"  HP: {p.HP}/{p.MaxHP} | MP: {p.MP}/{p.MaxMP}");
            Log($"  +{p.StatPoints} stat points | +{p.SkillPoints} skill points");
            ShowFloatingText(_character.Position + new Vector2(0, -10), "LEVEL UP!", new Color(1, 0.85f, 0.2f), big: true);
            FlashCharacter(new Color(1, 1, 0.5f));
        });

        // 18. Allocate stat point
        Step(1.0f, () =>
        {
            var result = GameSystems.AllocateStatPoint("STR");
            Log($"[STATS] Allocated stat point -> {result}");
            Log($"  New total damage: {GameState.Player.TotalDamage}");
        });

        // 19. Equip ring
        Step(0.8f, () =>
        {
            var ring = GameState.Player.Inventory.Find(i => i.Name == "Iron Ring");
            if (ring != null)
            {
                GameSystems.EquipItem(ring);
                Log($"[EQUIP] Equipped Iron Ring (+10 Max HP)");
                Log($"  Max HP: {GameState.Player.MaxHP}");
            }
        });

        // ──────── PHASE 3: BOSS FIGHT ────────

        Step(1.5f, () =>
        {
            Log("");
            Log("========================================");
            Log("  PHASE 3: BOSS FIGHT");
            Log("========================================");
        });

        // 20. Spawn boss
        Step(1.2f, () =>
        {
            var orc = GameSystems.SpawnMonster("Orc Warlord", MonsterTier.Tier3, canPoison: true);
            SpawnEntity("orc", _character.Position + new Vector2(72, 8), "monster/orc_warrior_new.png");
            Log($"[BOSS] Orc Warlord appears!");
            Log($"  HP:{orc.HP}/{orc.MaxHP} | Tier:{(int)orc.Tier} | XP:{orc.XPReward}");
        });

        // 21. Orc attacks + poison
        Step(1.0f, () =>
        {
            var orc = GameState.ActiveMonsters.Find(m => m.Name == "Orc Warlord");
            var dmg = GameSystems.MonsterAttackPlayer(orc);
            GameSystems.ApplyPoison(3, 3);
            Log($"[HIT] Orc Warlord attacks -> {dmg} damage");
            Log($"[STATUS] Poisoned! (3 dmg/tick, 3 ticks)");
            Log($"  HP: {GameState.Player.HP}/{GameState.Player.MaxHP}");
        });

        // 22. Poison tick
        Step(1.0f, () =>
        {
            int poisonDmg = GameSystems.TickPoison();
            Log($"[POISON] Tick -> {poisonDmg} damage");
            Log($"  HP: {GameState.Player.HP}/{GameState.Player.MaxHP} | Ticks left: {GameState.Player.PoisonTicksLeft}");
            ShowFloatingText(_character.Position, $"-{poisonDmg}", new Color(0.5f, 0.9f, 0.2f));
            _character.Modulate = new Color(0.7f, 1.0f, 0.5f); // poison tint
        });

        // 23. Attack boss
        Step(1.0f, () =>
        {
            var orc = GameState.ActiveMonsters.Find(m => m.Name == "Orc Warlord");
            var (dmg, crit) = GameSystems.AttackMonster(orc);
            Log($"[ATTACK] Attack -> {dmg} damage{(crit ? " CRITICAL!" : "")}");
            Log($"  Orc HP: {orc.HP}/{orc.MaxHP}");
        });

        // 24. Use skill on boss
        Step(1.0f, () =>
        {
            var orc = GameState.ActiveMonsters.Find(m => m.Name == "Orc Warlord");
            var skill = GameState.PlayerSkills[0];
            skill.CooldownRemaining = 0; // reset for demo
            var (dmg, ok) = GameSystems.UseSkill(skill, orc);
            if (ok)
            {
                Log($"[SKILL] {skill.Name} -> {dmg} damage!");
                Log($"  MP: {GameState.Player.MP}/{GameState.Player.MaxMP}");
            }
        });

        // 25. Heal + poison tick
        Step(1.0f, () =>
        {
            var potion = GameState.Player.Inventory.Find(i => i.Name == "Health Potion");
            if (potion != null)
            {
                var (ok, effect) = GameSystems.UseItem(potion);
                Log($"[ITEM] Used Health Potion -> {effect}");
            }
            int poisonDmg = GameSystems.TickPoison();
            if (poisonDmg > 0) Log($"[POISON] Tick -> {poisonDmg} damage");
            Log($"  HP: {GameState.Player.HP}/{GameState.Player.MaxHP}");
        });

        // 26. Kill boss
        Step(1.0f, () =>
        {
            var orc = GameState.ActiveMonsters.Find(m => m.Name == "Orc Warlord");
            orc.HP = Math.Min(orc.HP, 3);
            var (dmg, crit) = GameSystems.AttackMonster(orc);
            Log($"[ATTACK] Final blow -> Orc Warlord DEFEATED!");

            var (leveled, _) = GameSystems.GainXP(orc.XPReward);
            GameState.Player.Gold += orc.GoldReward;
            Log($"  +{orc.XPReward} XP | +{orc.GoldReward} Gold");
            if (leveled) Log("  >> LEVEL UP! <<");

            var blade = GameSystems.CreateItem("Orcish Blade", ItemType.Weapon, EquipSlot.MainHand, damage: 15, value: 100, desc: "Rare drop");
            GameSystems.AddToInventory(blade);
            Log($"  Rare drop: Orcish Blade (+15 damage)");

            RemoveEntity("orc");
            GameState.ActiveMonsters.Remove(orc);
        });

        // 27. Mana regen
        Step(1.0f, () =>
        {
            int restored = GameSystems.RegenMana(12);
            Log($"[REGEN] Mana regeneration -> +{restored} MP");
            Log($"  MP: {GameState.Player.MP}/{GameState.Player.MaxMP}");
        });

        // ──────── PHASE 4: DEATH & RESPAWN ────────

        Step(1.5f, () =>
        {
            Log("");
            Log("========================================");
            Log("  PHASE 4: DEATH & RESPAWN");
            Log("========================================");
        });

        // 28. Spawn two enemies
        Step(1.0f, () =>
        {
            var m1 = GameSystems.SpawnMonster("Dark Knight", MonsterTier.Tier3);
            var m2 = GameSystems.SpawnMonster("Shadow Mage", MonsterTier.Tier3);
            SpawnEntity("knight", _character.Position + new Vector2(56, -24), "monster/death_knight.png");
            SpawnEntity("mage", _character.Position + new Vector2(56, 24), "monster/undead/shadow_new.png");
            Log("[SPAWN] Dark Knight and Shadow Mage appear!");
            Log($"  Two Tier 3 enemies — overwhelming force!");
        });

        // 29. Take fatal damage
        Step(1.0f, () =>
        {
            GameState.Player.HP = 5;
            Log("[HIT] Taking massive damage!");
            Log($"  HP: {GameState.Player.HP}/{GameState.Player.MaxHP} -- CRITICAL!");
        });

        Step(1.0f, () =>
        {
            GameState.Player.HP = 0;
            GameSystems.PlayerDie();
            Log("========================================");
            Log("  >> YOU DIED <<");
            Log("========================================");
            RemoveEntity("knight");
            RemoveEntity("mage");
            // Death fade
            var deathTween = CreateTween();
            deathTween.TweenProperty(_character, "modulate", new Color(0.3f, 0.1f, 0.1f, 0.4f), 0.8);
            ShowFloatingText(_character.Position, "DEATH", Colors.Red, big: true);
            GameState.ActiveMonsters.Clear();
        });

        // 30. Respawn
        Step(1.5f, () =>
        {
            GameSystems.PlayerRespawn();
            _character.Modulate = Colors.White; // restore from death/poison tint
            Log("[RESPAWN] Returned to town with half HP/MP");
            Log($"  HP: {GameState.Player.HP}/{GameState.Player.MaxHP} | MP: {GameState.Player.MP}/{GameState.Player.MaxMP}");
            Log($"  Location: {GameState.Location}");
        });

        // ──────── PHASE 5: WRAP UP ────────

        Step(1.5f, () =>
        {
            Log("");
            Log("========================================");
            Log("  PHASE 5: WRAP UP");
            Log("========================================");
        });

        // 31. Sell loot
        Step(1.0f, () =>
        {
            var fang = GameState.Player.Inventory.Find(i => i.Name == "Rat Fang");
            if (fang != null)
            {
                var (gold, result) = GameSystems.SellItem(fang);
                Log($"[SHOP] {result}");
            }
            Log($"  Gold: {GameState.Player.Gold}");
        });

        // 32. Unequip
        Step(1.0f, () =>
        {
            var ok = GameSystems.UnequipItem(EquipSlot.Ring);
            if (ok) Log("[EQUIP] Unequipped Iron Ring -> back to inventory");
            Log($"  Inventory: {GameState.Player.Inventory.Count}/{GameState.Player.InventorySize}");
            Log($"  Max HP (without ring): {GameState.Player.MaxHP}");
        });

        // 33. Inventory full test
        Step(1.0f, () =>
        {
            Log("[TEST] Attempting to add item to inventory...");
            var junk = GameSystems.CreateItem("Test Item", ItemType.Material, EquipSlot.None, value: 1);
            bool added = GameSystems.AddToInventory(junk);
            Log($"  Add to inventory: {(added ? "SUCCESS" : "FAILED (bag full)")}");
            Log($"  Inventory: {GameState.Player.Inventory.Count}/{GameState.Player.InventorySize}");
        });

        // 34. Exit dungeon
        Step(1.0f, () =>
        {
            GameSystems.ExitDungeon();
            Log($"[DUNGEON] Left the dungeon -> {GameState.Location}");
        });

        // 35. Save
        Step(1.0f, () =>
        {
            var save = GameSystems.SaveGame();
            Log("[SAVE] Game state saved:");
            foreach (var (key, val) in save)
                Log($"  {key}: {val}");
        });

        // ──────── PHASE 6: UI SHOWCASE ────────

        Step(1.5f, () =>
        {
            Log("");
            Log("========================================");
            Log("  PHASE 6: UI SHOWCASE");
            Log("========================================");
            Log("[UI] Demonstrating HUD, menus, and information displays");
        });

        // 37. XP bar
        Step(1.0f, () =>
        {
            Log("[HUD] XP Progress Bar — shows progress toward next level");
            UpdateXPBar();
            // Animate fill from 0 to current
            var p = GameState.Player;
            float target = p.XPToNextLevel > 0 ? (float)p.XP / p.XPToNextLevel * 100f : 0;
            _xpBar.Value = 0;
            var tween = CreateTween();
            tween.TweenProperty(_xpBar, "value", (double)target, 0.8);
        });

        // 38. Toast notifications
        Step(1.2f, () =>
        {
            Log("[HUD] Toast Notifications — slide-in alerts for game events");
            ShowToast("[Kill] Giant Rat defeated! +14 XP", new Color(0.4f, 0.85f, 0.4f));
        });
        Step(0.6f, () => ShowToast("[Loot] Orcish Blade acquired!", new Color(0.961f, 0.784f, 0.420f)));
        Step(0.6f, () => ShowToast("[Level] Level Up! Now Level 2", new Color(0.5f, 0.7f, 1.0f)));
        Step(0.6f, () => ShowToast("[Warning] Low HP - use a potion!", new Color(1.0f, 0.4f, 0.3f)));

        // 39. Shortcut bar
        Step(1.2f, () =>
        {
            Log("[HUD] Shortcut Bar — 8 assignable skill/item slots (L1/R1 + face buttons)");
            SetShortcutIcon(0, "item/potion/emerald.png");
            SetShortcutIcon(1, "item/weapon/short_sword_1_new.png");
            Log("  Slot 1 = Health Potion, Slot 2 = Slash");
        });

        // 40. Inventory grid
        Step(1.5f, () =>
        {
            Log("[PANEL] Inventory Grid — 5x5 slot container with item icons");
            PopulateInventoryGrid();
            _inventoryPanel.Visible = true;
        });

        // 41. Equipment panel
        Step(1.5f, () =>
        {
            Log("[PANEL] Equipment Panel — slot-based gear display with icons");
            PopulateEquipSlots();
            _equipPanel.Visible = true;
        });

        // 42. Tooltip
        Step(1.2f, () =>
        {
            Log("[UI] Tooltip — contextual item information popup");
            string tip = "Iron Sword\nWeapon - Main Hand\n\nDamage: +8\nValue: 50g\n\nA sturdy iron blade.";
            if (GameState.Player.Equipment.TryGetValue(EquipSlot.MainHand, out var sword))
                tip = $"{sword.Name}\n{sword.Type} - {sword.Slot}\n\nDamage: +{sword.Damage}\nValue: {sword.Value}g\n\n{sword.Description}";
            ShowTooltipAt(tip, new Vector2(850, 310));
        });

        // 43. Settings panel
        Step(1.5f, () =>
        {
            _inventoryPanel.Visible = false;
            _equipPanel.Visible = false;
            HideTooltip();
            Log("[PANEL] Settings Panel — sliders, toggles, and dropdowns");
            _settingsPanel.Visible = true;
        });

        // 44. Death screen overlay
        Step(1.5f, () =>
        {
            _settingsPanel.Visible = false;
            Log("[OVERLAY] Death Screen — full-screen modal with restart options");
            _deathOverlay.Visible = true;
            _deathOverlay.Modulate = new Color(1, 1, 1, 0);
            var tween = CreateTween();
            tween.TweenProperty(_deathOverlay, "modulate:a", 1.0f, 0.5);
        });

        // 45. Dismiss death screen
        Step(2.0f, () =>
        {
            Log("[OVERLAY] Dismissing death screen...");
            var tween = CreateTween();
            tween.TweenProperty(_deathOverlay, "modulate:a", 0.0f, 0.5);
            tween.TweenCallback(Callable.From(() => _deathOverlay.Visible = false));
        });

        // ──────── PHASE 7: PERFORMANCE TESTING ────────

        Step(1.5f, () =>
        {
            Log("");
            Log("========================================");
            Log("  PHASE 7: PERFORMANCE TESTING");
            Log("========================================");
            Log("[PERF] Measuring game operations (like Lighthouse for games)");
            _perfVisible = true;
        });

        // 47. Show live perf monitors
        Step(1.0f, () =>
        {
            Log("[PERF] Live Monitors (Godot Performance singleton):");
            Log($"  FPS: {Performance.GetMonitor(Performance.Monitor.TimeFps):F0}");
            Log($"  Frame time: {Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000:F2}ms");
            Log($"  Physics time: {Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * 1000:F2}ms");
            Log($"  Node count: {Performance.GetMonitor(Performance.Monitor.ObjectNodeCount):F0}");
            Log($"  Memory: {Performance.GetMonitor(Performance.Monitor.MemoryStatic) / 1048576:F1}MB");
        });

        // 48. Benchmark: combat calculations
        Step(1.0f, () =>
        {
            Log("[BENCH] Combat calculations (1000 iterations):");

            GameSystems.EnterDungeon();
            var monster = GameSystems.SpawnMonster("BenchRat", MonsterTier.Tier1);

            ulong start = Time.GetTicksUsec();
            for (int i = 0; i < 1000; i++)
            {
                monster.HP = monster.MaxHP;
                GameSystems.AttackMonster(monster);
            }
            double elapsed = Time.GetTicksUsec() - start;
            double perOp = elapsed / 1000.0;

            _benchResults.Add(("AttackMonster x1000", elapsed));
            Log($"  Total: {elapsed:F0}us | Per-op: {perOp:F1}us");

            GameState.ActiveMonsters.Remove(monster);
        });

        // 49. Benchmark: stat recalculation
        Step(1.0f, () =>
        {
            Log("[BENCH] Stat recalculation (1000 iterations):");

            ulong start = Time.GetTicksUsec();
            for (int i = 0; i < 1000; i++)
            {
                GameState.Player.InvalidateStats();
                _ = GameState.Player.TotalDamage;
                _ = GameState.Player.TotalDefense;
            }
            double elapsed = Time.GetTicksUsec() - start;
            double perOp = elapsed / 1000.0;

            _benchResults.Add(("StatRecalc x1000", elapsed));
            Log($"  Total: {elapsed:F0}us | Per-op: {perOp:F1}us");
        });

        // 50. Benchmark: inventory operations
        Step(1.0f, () =>
        {
            Log("[BENCH] Inventory add/remove (500 cycles):");

            ulong start = Time.GetTicksUsec();
            for (int i = 0; i < 500; i++)
            {
                var item = GameSystems.CreateItem($"BenchItem{i}", ItemType.Material, EquipSlot.None, value: 1);
                GameSystems.AddToInventory(item);
            }
            // Clear bench items
            GameState.Player.Inventory.RemoveAll(i => i.Name.StartsWith("BenchItem"));
            double elapsed = Time.GetTicksUsec() - start;

            _benchResults.Add(("InventoryOps x500", elapsed));
            Log($"  Total: {elapsed:F0}us | Per-cycle: {elapsed / 500.0:F1}us");
        });

        // 51. Benchmark: XP/leveling
        Step(1.0f, () =>
        {
            Log("[BENCH] XP gain + level checks (1000 iterations):");

            int savedLevel = GameState.Player.Level;
            int savedXP = GameState.Player.XP;

            ulong start = Time.GetTicksUsec();
            for (int i = 0; i < 1000; i++)
            {
                GameState.Player.Level = 1;
                GameState.Player.XP = 0;
                GameSystems.GainXP(50);
            }
            double elapsed = Time.GetTicksUsec() - start;

            GameState.Player.Level = savedLevel;
            GameState.Player.XP = savedXP;

            _benchResults.Add(("GainXP x1000", elapsed));
            Log($"  Total: {elapsed:F0}us | Per-op: {elapsed / 1000.0:F1}us");
        });

        // 52. Benchmark: entity spawn/remove (visual)
        Step(1.2f, () =>
        {
            Log("[BENCH] Entity spawn + remove (50 sprites):");

            ulong start = Time.GetTicksUsec();
            var tempSprites = new List<Sprite2D>();
            for (int i = 0; i < 50; i++)
            {
                var sprite = new Sprite2D();
                sprite.Texture = CreateColorTexture(Colors.Magenta, 16);
                sprite.Position = new Vector2(
                    GD.RandRange(48, RoomWidth * TileSize - 48),
                    GD.RandRange(48, RoomHeight * TileSize - 48));
                sprite.TextureFilter = TextureFilterEnum.Nearest;
                _entityLayer.AddChild(sprite);
                tempSprites.Add(sprite);
            }
            double spawnTime = Time.GetTicksUsec() - start;

            start = Time.GetTicksUsec();
            foreach (var s in tempSprites) s.QueueFree();
            double removeTime = Time.GetTicksUsec() - start;

            _benchResults.Add(("Spawn 50 sprites", spawnTime));
            _benchResults.Add(("Remove 50 sprites", removeTime));
            Log($"  Spawn: {spawnTime:F0}us | Remove: {removeTime:F0}us");
            Log($"  Per-sprite: spawn {spawnTime / 50:F1}us, remove {removeTime / 50:F1}us");
        });

        // 53. Benchmark: UI panel creation
        Step(1.0f, () =>
        {
            Log("[BENCH] Styled panel creation (20 panels):");

            ulong start = Time.GetTicksUsec();
            var tempPanels = new List<Panel>();
            for (int i = 0; i < 20; i++)
            {
                var panel = CreateStyledWindow($"Bench{i}", new Vector2(100, 100), new Vector2(200, 100));
                _uiLayer.AddChild(panel);
                tempPanels.Add(panel);
            }
            double elapsed = Time.GetTicksUsec() - start;

            foreach (var p in tempPanels) p.QueueFree();

            _benchResults.Add(("Create 20 panels", elapsed));
            Log($"  Total: {elapsed:F0}us | Per-panel: {elapsed / 20:F1}us");
        });

        // 54. Performance scorecard
        Step(1.5f, () =>
        {
            Log("");
            Log("========================================");
            Log("  PERFORMANCE SCORECARD");
            Log("========================================");

            double fps = Performance.GetMonitor(Performance.Monitor.TimeFps);
            double frameMs = Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000;
            double memMb = Performance.GetMonitor(Performance.Monitor.MemoryStatic) / 1048576;
            double nodes = Performance.GetMonitor(Performance.Monitor.ObjectNodeCount);

            // Score each metric (100 = great, 0 = terrible)
            int fpsScore = fps >= 60 ? 100 : fps >= 30 ? 70 : fps >= 15 ? 40 : 10;
            int frameScore = frameMs <= 16.6 ? 100 : frameMs <= 33.3 ? 70 : frameMs <= 66.6 ? 40 : 10;
            int memScore = memMb <= 100 ? 100 : memMb <= 256 ? 80 : memMb <= 512 ? 50 : 20;
            int nodeScore = nodes <= 200 ? 100 : nodes <= 500 ? 80 : nodes <= 1000 ? 50 : 20;
            int overall = (fpsScore + frameScore + memScore + nodeScore) / 4;

            Log($"  FPS:         {fps,6:F0}    [{fpsScore}/100]");
            Log($"  Frame time:  {frameMs,6:F2}ms [{frameScore}/100]");
            Log($"  Memory:      {memMb,6:F1}MB [{memScore}/100]");
            Log($"  Node count:  {nodes,6:F0}    [{nodeScore}/100]");
            Log($"  ─────────────────────────────");
            Log($"  OVERALL:              [{overall}/100]");
            Log("");
            Log("  Benchmarks:");
            foreach (var (name, usec) in _benchResults)
                Log($"    {name,-24} {usec,8:F0}us");
            Log("");

            _perfVisible = false;
        });

        // 55. Final summary
        Step(2.0f, () =>
        {
            var p = GameState.Player;
            Log("");
            Log("========================================");
            Log("  DEMO COMPLETE");
            Log("========================================");
            Log($"  {p.Name} Level {p.Level}");
            Log($"  HP: {p.HP}/{p.MaxHP} | MP: {p.MP}/{p.MaxMP}");
            Log($"  STR:{p.STR} DEX:{p.DEX} INT:{p.INT} VIT:{p.VIT}");
            Log($"  Gold: {p.Gold}");
            Log($"  Damage: {p.TotalDamage} | Defense: {p.TotalDefense}");
            Log($"  Equipment: {p.Equipment.Count} slots | Inventory: {p.Inventory.Count}/{p.InventorySize}");
            Log($"  Skills: {GameState.PlayerSkills.Count}");
            Log("");
            Log("  Systems tested: movement, combat, skills, inventory,");
            Log("  equipment, shop, NPC dialog, chests, leveling, stats,");
            Log("  status effects, death/respawn, settings, save, mana regen");
            Log("");
            Log("  UI tested: XP bar, toast notifications, shortcut bar,");
            Log("  inventory grid, equipment panel, settings panel,");
            Log("  tooltip, death screen overlay, styled windows, HP/MP orbs");
            Log("");
            Log("  Perf tested: FPS, frame time, memory, node count,");
            Log("  combat calc, stat recalc, inventory ops, XP/leveling,");
            Log("  sprite spawn/remove, UI panel creation, scorecard");
            Log("");
            Log("  Auto-quit in 5 seconds...");
        });
    }

    // ==================== HELPERS ====================

    private void Step(float delay, Action action)
    {
        _steps.Add((delay, action));
    }

    private void Log(string message)
    {
        GD.Print(message);
        _logLines.Add(message);
        // Use RemoveRange instead of repeated RemoveAt(0) — single shift vs N shifts
        if (_logLines.Count > 14)
            _logLines.RemoveRange(0, _logLines.Count - 14);
        if (_logLabel != null) _logLabel.Text = string.Join("\n", _logLines);
    }

    private void UpdateStatsDisplay()
    {
        if (_statsLabel == null) return;
        var p = GameState.Player;
        var status = p.Status != StatusEffect.None ? $" [{p.Status}]" : "";
        var floor = GameState.DungeonFloor > 0 ? $" F{GameState.DungeonFloor}" : "";
        _statsLabel.Text = $"HP:{p.HP}/{p.MaxHP}  MP:{p.MP}/{p.MaxMP}  Gold:{p.Gold}  Lv.{p.Level}  {GameState.Location}{floor}{status}";
        _hpMpOrbs?.UpdateValues(p.HP, p.MaxHP, p.MP, p.MaxMP);
        UpdateXPBar();
    }

    // ─── Window UI Methods ───

    private Panel CreateStyledWindow(string title, Vector2 position, Vector2 size)
    {
        var panel = new Panel();
        panel.Position = position;
        panel.Size = size;
        panel.Visible = false;

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
        titleLabel.AddThemeFontSizeOverride("font_size", 15);
        panel.AddChild(titleLabel);

        var sep = new ColorRect();
        sep.Color = new Color(0.961f, 0.784f, 0.420f, 0.3f);
        sep.Position = new Vector2(10, 28);
        sep.Size = new Vector2(size.X - 20, 1);
        panel.AddChild(sep);

        var content = new Label();
        content.Name = "Content";
        content.Position = new Vector2(14, 36);
        content.Size = new Vector2(size.X - 28, size.Y - 48);
        content.AddThemeColorOverride("font_color", new Color(0.925f, 0.941f, 1.0f));
        content.AddThemeFontSizeOverride("font_size", 13);
        panel.AddChild(content);

        return panel;
    }

    private TextureRect AddIconToWindow(Panel window, string spritePath, Vector2 position, Vector2 size)
    {
        var icon = new TextureRect();
        var fullPath = AssetBase + spritePath;
        if (ResourceLoader.Exists(fullPath))
            icon.Texture = GD.Load<Texture2D>(fullPath);
        icon.Position = position;
        icon.Size = size;
        icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        icon.TextureFilter = TextureFilterEnum.Nearest;
        window.AddChild(icon);
        return icon;
    }

    private void ClearWindowIcons(Panel window)
    {
        // Iterate backwards to avoid allocation of a collection list
        for (int i = window.GetChildCount() - 1; i >= 0; i--)
        {
            if (window.GetChild(i) is TextureRect icon)
                icon.QueueFree();
        }
    }

    private void ShowWindow(Panel window, string content)
    {
        if (window == null) return;
        window.GetNode<Label>("Content").Text = content;
        window.Visible = true;
    }

    private void HideAllWindows()
    {
        if (_gameWindow != null) _gameWindow.Visible = false;
        if (_shopWindow != null) _shopWindow.Visible = false;
        if (_dialogWindow != null) _dialogWindow.Visible = false;
        if (_inventoryPanel != null) _inventoryPanel.Visible = false;
        if (_equipPanel != null) _equipPanel.Visible = false;
        if (_settingsPanel != null) _settingsPanel.Visible = false;
        HideTooltip();
    }

    // ─── XP Bar ───

    private void CreateXPBar()
    {
        _xpBar = new ProgressBar();
        _xpBar.Position = new Vector2(760, 1048);
        _xpBar.Size = new Vector2(400, 18);
        _xpBar.MinValue = 0;
        _xpBar.MaxValue = 100;
        _xpBar.Value = 0;
        _xpBar.ShowPercentage = false;

        var bgStyle = new StyleBoxFlat();
        bgStyle.BgColor = new Color(0.06f, 0.06f, 0.12f, 0.85f);
        bgStyle.SetBorderWidthAll(1);
        bgStyle.BorderColor = new Color(0.961f, 0.784f, 0.420f, 0.3f);
        bgStyle.SetCornerRadiusAll(3);
        _xpBar.AddThemeStyleboxOverride("background", bgStyle);

        var fillStyle = new StyleBoxFlat();
        fillStyle.BgColor = new Color(0.961f, 0.784f, 0.420f, 0.6f);
        fillStyle.SetCornerRadiusAll(3);
        _xpBar.AddThemeStyleboxOverride("fill", fillStyle);

        _uiLayer.AddChild(_xpBar);

        _xpLabel = new Label();
        _xpLabel.Position = new Vector2(760, 1048);
        _xpLabel.Size = new Vector2(400, 18);
        _xpLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _xpLabel.VerticalAlignment = VerticalAlignment.Center;
        _xpLabel.AddThemeColorOverride("font_color", new Color(0.92f, 0.92f, 0.95f));
        _xpLabel.AddThemeFontSizeOverride("font_size", 10);
        _uiLayer.AddChild(_xpLabel);
    }

    private void UpdateXPBar()
    {
        if (_xpBar == null) return;
        var p = GameState.Player;
        int needed = p.XPToNextLevel;
        _xpBar.Value = needed > 0 ? (float)p.XP / needed * 100f : 0;
        _xpLabel.Text = $"XP: {p.XP} / {needed}";
    }

    // ─── Toast Notifications ───

    private void CreateToastContainer()
    {
        _toastContainer = new VBoxContainer();
        _toastContainer.Position = new Vector2(1540, 100);
        _toastContainer.Size = new Vector2(360, 600);
        _toastContainer.AddThemeConstantOverride("separation", 8);
        _uiLayer.AddChild(_toastContainer);
    }

    private void ShowToast(string message, Color borderColor)
    {
        var toast = new PanelContainer();
        toast.CustomMinimumSize = new Vector2(340, 0);

        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.086f, 0.106f, 0.157f, 0.92f);
        style.BorderColor = new Color(borderColor.R, borderColor.G, borderColor.B, 0.5f);
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(6);
        style.ContentMarginLeft = 12;
        style.ContentMarginRight = 12;
        style.ContentMarginTop = 8;
        style.ContentMarginBottom = 8;
        toast.AddThemeStyleboxOverride("panel", style);

        var label = new Label();
        label.Text = message;
        label.AddThemeColorOverride("font_color", borderColor);
        label.AddThemeFontSizeOverride("font_size", 13);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        toast.AddChild(label);

        toast.Modulate = new Color(1, 1, 1, 0);
        _toastContainer.AddChild(toast);

        var tween = CreateTween();
        tween.TweenProperty(toast, "modulate:a", 1.0f, 0.25).SetTrans(Tween.TransitionType.Quad);
        tween.TweenInterval(3.0);
        tween.TweenProperty(toast, "modulate:a", 0.0f, 0.4);
        tween.TweenCallback(Callable.From(toast.QueueFree));
    }

    // ─── Shortcut Bar ───

    private void CreateShortcutBar()
    {
        _shortcutBar = new HBoxContainer();
        _shortcutBar.Position = new Vector2(712, 985);
        _shortcutBar.AddThemeConstantOverride("separation", 4);
        _uiLayer.AddChild(_shortcutBar);

        string[] keys = { "Q+Z", "Q+X", "Q+A", "Q+S", "W+Z", "W+X", "W+A", "W+S" };
        for (int i = 0; i < 8; i++)
        {
            var slot = new Panel();
            slot.CustomMinimumSize = new Vector2(56, 56);

            var style = new StyleBoxFlat();
            style.BgColor = new Color(0.06f, 0.07f, 0.12f, 0.85f);
            style.BorderColor = new Color(0.4f, 0.35f, 0.25f, 0.5f);
            style.SetBorderWidthAll(1);
            style.SetCornerRadiusAll(4);
            slot.AddThemeStyleboxOverride("panel", style);

            var numLabel = new Label();
            numLabel.Text = (i + 1).ToString();
            numLabel.Position = new Vector2(3, 1);
            numLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.45f, 0.35f));
            numLabel.AddThemeFontSizeOverride("font_size", 9);
            slot.AddChild(numLabel);

            var keyLabel = new Label();
            keyLabel.Text = keys[i];
            keyLabel.Position = new Vector2(2, 41);
            keyLabel.Size = new Vector2(52, 14);
            keyLabel.HorizontalAlignment = HorizontalAlignment.Center;
            keyLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.45f, 0.35f));
            keyLabel.AddThemeFontSizeOverride("font_size", 8);
            slot.AddChild(keyLabel);

            _shortcutBar.AddChild(slot);
        }
    }

    private void SetShortcutIcon(int slotIndex, string spritePath)
    {
        if (slotIndex < 0 || slotIndex >= _shortcutBar.GetChildCount()) return;
        var slot = _shortcutBar.GetChild(slotIndex) as Panel;
        if (slot == null) return;

        for (int i = slot.GetChildCount() - 1; i >= 0; i--)
            if (slot.GetChild(i) is TextureRect) slot.GetChild(i).QueueFree();

        var icon = new TextureRect();
        var fullPath = AssetBase + spritePath;
        if (ResourceLoader.Exists(fullPath))
            icon.Texture = GD.Load<Texture2D>(fullPath);
        else
            icon.Texture = CreateColorTexture(new Color(0.4f, 0.5f, 0.8f), 32);
        icon.Position = new Vector2(12, 8);
        icon.Size = new Vector2(32, 32);
        icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        icon.TextureFilter = TextureFilterEnum.Nearest;
        slot.AddChild(icon);
    }

    // ─── Inventory Grid ───

    private Panel CreateInventoryPanel()
    {
        var panel = CreateStyledWindow("Inventory (5x5)", new Vector2(420, 250), new Vector2(290, 340));

        var grid = new GridContainer();
        grid.Name = "Grid";
        grid.Columns = 5;
        grid.Position = new Vector2(14, 42);
        grid.AddThemeConstantOverride("h_separation", 4);
        grid.AddThemeConstantOverride("v_separation", 4);
        panel.AddChild(grid);

        for (int i = 0; i < 25; i++)
        {
            var slot = new Panel();
            slot.CustomMinimumSize = new Vector2(48, 48);
            var style = new StyleBoxFlat();
            style.BgColor = new Color(0.05f, 0.06f, 0.10f, 0.8f);
            style.BorderColor = new Color(0.3f, 0.3f, 0.4f, 0.4f);
            style.SetBorderWidthAll(1);
            style.SetCornerRadiusAll(2);
            slot.AddThemeStyleboxOverride("panel", style);
            grid.AddChild(slot);
        }

        var footer = new Label();
        footer.Name = "Footer";
        footer.Position = new Vector2(14, 310);
        footer.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.7f));
        footer.AddThemeFontSizeOverride("font_size", 11);
        panel.AddChild(footer);

        return panel;
    }

    private void PopulateInventoryGrid()
    {
        if (_inventoryPanel == null) return;
        var grid = _inventoryPanel.GetNode<GridContainer>("Grid");
        var inv = GameState.Player.Inventory;

        var iconMap = new Dictionary<string, string>
        {
            { "Health Potion", "item/potion/emerald.png" },
            { "Iron Sword", "item/weapon/short_sword_1_new.png" },
            { "Leather Cap", "item/armor/headgear/helmet_5.png" },
            { "Leather Armor", "item/armor/leather_armour.png" },
            { "Iron Ring", "item/ring/ring_agate.png" },
            { "Orcish Blade", "item/weapon/great_sword_2.png" },
        };

        for (int i = 0; i < 25; i++)
        {
            var slot = grid.GetChild(i) as Panel;
            if (slot == null) continue;

            // Clear existing icons/labels in slot
            for (int j = slot.GetChildCount() - 1; j >= 0; j--)
            {
                var child = slot.GetChild(j);
                if (child is TextureRect || child is Label) child.QueueFree();
            }

            if (i < inv.Count)
            {
                var item = inv[i];
                var icon = new TextureRect();
                if (iconMap.TryGetValue(item.Name, out var spritePath) && ResourceLoader.Exists(AssetBase + spritePath))
                    icon.Texture = GD.Load<Texture2D>(AssetBase + spritePath);
                else
                    icon.Texture = CreateColorTexture(new Color(0.4f, 0.4f, 0.5f), 32);
                icon.Position = new Vector2(8, 4);
                icon.Size = new Vector2(32, 32);
                icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
                icon.TextureFilter = TextureFilterEnum.Nearest;
                slot.AddChild(icon);

                if (item.StackCount > 1)
                {
                    var countLabel = new Label();
                    countLabel.Text = item.StackCount.ToString();
                    countLabel.Position = new Vector2(33, 32);
                    countLabel.AddThemeColorOverride("font_color", Colors.White);
                    countLabel.AddThemeFontSizeOverride("font_size", 10);
                    slot.AddChild(countLabel);
                }

                // Highlight occupied slot border
                var occupied = new StyleBoxFlat();
                occupied.BgColor = new Color(0.07f, 0.08f, 0.14f, 0.8f);
                occupied.BorderColor = new Color(0.5f, 0.45f, 0.3f, 0.5f);
                occupied.SetBorderWidthAll(1);
                occupied.SetCornerRadiusAll(2);
                slot.AddThemeStyleboxOverride("panel", occupied);
            }
        }

        var footer = _inventoryPanel.GetNode<Label>("Footer");
        footer.Text = $"{inv.Count}/{GameState.Player.InventorySize} slots used";
    }

    // ─── Equipment Panel ───

    private Panel CreateEquipmentPanel()
    {
        var panel = CreateStyledWindow("Equipment", new Vector2(730, 250), new Vector2(260, 320));

        string[] slotNames = { "Head", "Body", "Main Hand", "Off Hand", "Ring" };
        EquipSlot[] slots = { EquipSlot.Head, EquipSlot.Body, EquipSlot.MainHand, EquipSlot.OffHand, EquipSlot.Ring };

        for (int i = 0; i < slotNames.Length; i++)
        {
            float yPos = 44 + i * 52;

            var label = new Label();
            label.Text = slotNames[i];
            label.Position = new Vector2(14, yPos + 4);
            label.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.7f));
            label.AddThemeFontSizeOverride("font_size", 11);
            panel.AddChild(label);

            var slotBox = new Panel();
            slotBox.Name = $"Slot_{slots[i]}";
            slotBox.Position = new Vector2(100, yPos);
            slotBox.Size = new Vector2(44, 44);
            var style = new StyleBoxFlat();
            style.BgColor = new Color(0.05f, 0.06f, 0.10f, 0.8f);
            style.BorderColor = new Color(0.3f, 0.3f, 0.4f, 0.4f);
            style.SetBorderWidthAll(1);
            style.SetCornerRadiusAll(2);
            slotBox.AddThemeStyleboxOverride("panel", style);
            panel.AddChild(slotBox);

            var itemLabel = new Label();
            itemLabel.Name = $"ItemLabel_{slots[i]}";
            itemLabel.Position = new Vector2(152, yPos + 4);
            itemLabel.AddThemeColorOverride("font_color", new Color(0.925f, 0.941f, 1.0f));
            itemLabel.AddThemeFontSizeOverride("font_size", 12);
            panel.AddChild(itemLabel);
        }

        return panel;
    }

    private void PopulateEquipSlots()
    {
        if (_equipPanel == null) return;

        var iconMap = new Dictionary<string, string>
        {
            { "Iron Sword", "item/weapon/short_sword_1_new.png" },
            { "Leather Cap", "item/armor/headgear/helmet_5.png" },
            { "Leather Armor", "item/armor/leather_armour.png" },
            { "Orcish Blade", "item/weapon/great_sword_2.png" },
            { "Iron Ring", "item/ring/ring_agate.png" },
        };

        EquipSlot[] slots = { EquipSlot.Head, EquipSlot.Body, EquipSlot.MainHand, EquipSlot.OffHand, EquipSlot.Ring };

        foreach (var slot in slots)
        {
            var slotBox = _equipPanel.GetNodeOrNull<Panel>($"Slot_{slot}");
            var itemLabel = _equipPanel.GetNodeOrNull<Label>($"ItemLabel_{slot}");
            if (slotBox == null || itemLabel == null) continue;

            for (int i = slotBox.GetChildCount() - 1; i >= 0; i--)
                if (slotBox.GetChild(i) is TextureRect) slotBox.GetChild(i).QueueFree();

            if (GameState.Player.Equipment.TryGetValue(slot, out var item))
            {
                itemLabel.Text = item.Name;
                itemLabel.AddThemeColorOverride("font_color", new Color(0.925f, 0.941f, 1.0f));

                var icon = new TextureRect();
                if (iconMap.TryGetValue(item.Name, out var spritePath) && ResourceLoader.Exists(AssetBase + spritePath))
                    icon.Texture = GD.Load<Texture2D>(AssetBase + spritePath);
                else
                    icon.Texture = CreateColorTexture(new Color(0.5f, 0.5f, 0.6f), 32);
                icon.Position = new Vector2(6, 6);
                icon.Size = new Vector2(32, 32);
                icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
                icon.TextureFilter = TextureFilterEnum.Nearest;
                slotBox.AddChild(icon);

                var filled = new StyleBoxFlat();
                filled.BgColor = new Color(0.07f, 0.08f, 0.14f, 0.8f);
                filled.BorderColor = new Color(0.5f, 0.45f, 0.3f, 0.5f);
                filled.SetBorderWidthAll(1);
                filled.SetCornerRadiusAll(2);
                slotBox.AddThemeStyleboxOverride("panel", filled);
            }
            else
            {
                itemLabel.Text = "(empty)";
                itemLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.5f));
            }
        }
    }

    // ─── Settings Panel ───

    private Panel CreateSettingsPanel()
    {
        var panel = CreateStyledWindow("Settings", new Vector2(1020, 250), new Vector2(320, 340));
        float y = 44;
        y = AddSliderSetting(panel, "Music Volume", 80, y);
        y = AddSliderSetting(panel, "SFX Volume", 100, y);
        y = AddToggleSetting(panel, "Damage Numbers", true, y);
        y = AddToggleSetting(panel, "Show Minimap", true, y);
        AddDropdownSetting(panel, "Target Priority", new[] { "Nearest", "Strongest", "Tankiest", "Bosses", "Weakest" }, 0, y);
        return panel;
    }

    private float AddSliderSetting(Panel panel, string label, float value, float yPos)
    {
        var lbl = new Label();
        lbl.Text = label;
        lbl.Position = new Vector2(14, yPos);
        lbl.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.85f));
        lbl.AddThemeFontSizeOverride("font_size", 12);
        panel.AddChild(lbl);

        var slider = new HSlider();
        slider.Position = new Vector2(14, yPos + 20);
        slider.Size = new Vector2(200, 20);
        slider.MinValue = 0;
        slider.MaxValue = 100;
        slider.Value = value;
        panel.AddChild(slider);

        var valLabel = new Label();
        valLabel.Text = $"{(int)value}%";
        valLabel.Position = new Vector2(224, yPos + 18);
        valLabel.AddThemeColorOverride("font_color", new Color(0.961f, 0.784f, 0.420f));
        valLabel.AddThemeFontSizeOverride("font_size", 12);
        panel.AddChild(valLabel);

        return yPos + 50;
    }

    private float AddToggleSetting(Panel panel, string label, bool value, float yPos)
    {
        var check = new CheckButton();
        check.Text = label;
        check.ButtonPressed = value;
        check.Position = new Vector2(14, yPos);
        check.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.85f));
        check.AddThemeFontSizeOverride("font_size", 12);
        panel.AddChild(check);
        return yPos + 36;
    }

    private float AddDropdownSetting(Panel panel, string label, string[] options, int selected, float yPos)
    {
        var lbl = new Label();
        lbl.Text = label;
        lbl.Position = new Vector2(14, yPos);
        lbl.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.85f));
        lbl.AddThemeFontSizeOverride("font_size", 12);
        panel.AddChild(lbl);

        var dropdown = new OptionButton();
        dropdown.Position = new Vector2(14, yPos + 22);
        dropdown.Size = new Vector2(200, 28);
        foreach (var opt in options) dropdown.AddItem(opt);
        dropdown.Selected = selected;
        panel.AddChild(dropdown);

        return yPos + 60;
    }

    // ─── Tooltip ───

    private void CreateTooltip()
    {
        _tooltip = new Panel();
        _tooltip.Size = new Vector2(220, 130);
        _tooltip.Visible = false;

        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.06f, 0.07f, 0.12f, 0.95f);
        style.BorderColor = new Color(0.961f, 0.784f, 0.420f, 0.6f);
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(6);
        _tooltip.AddThemeStyleboxOverride("panel", style);

        var content = new Label();
        content.Name = "Content";
        content.Position = new Vector2(10, 8);
        content.Size = new Vector2(200, 114);
        content.AddThemeColorOverride("font_color", new Color(0.925f, 0.941f, 1.0f));
        content.AddThemeFontSizeOverride("font_size", 12);
        content.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _tooltip.AddChild(content);

        _uiLayer.AddChild(_tooltip);
    }

    private void ShowTooltipAt(string text, Vector2 position)
    {
        if (_tooltip == null) return;
        _tooltip.GetNode<Label>("Content").Text = text;
        _tooltip.Position = position;
        _tooltip.Visible = true;
    }

    private void HideTooltip()
    {
        if (_tooltip != null) _tooltip.Visible = false;
    }

    // ─── Death Screen Overlay ───

    private void CreateDeathOverlay()
    {
        _deathOverlay = new ColorRect();
        _deathOverlay.Position = Vector2.Zero;
        _deathOverlay.Size = new Vector2(1920, 1080);
        _deathOverlay.Color = new Color(0, 0, 0, 0.78f);
        _deathOverlay.Visible = false;

        var center = new CenterContainer();
        center.Position = Vector2.Zero;
        center.Size = new Vector2(1920, 1080);
        _deathOverlay.AddChild(center);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 16);
        center.AddChild(vbox);

        var title = new Label();
        title.Text = "YOU DIED";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeColorOverride("font_color", new Color(1.0f, 0.88f, 0.69f));
        title.AddThemeFontSizeOverride("font_size", 52);
        vbox.AddChild(title);

        var message = new Label();
        message.Text = "The dungeon claims another soul...\n\nYou lost 15% XP and 2 backpack items.";
        message.HorizontalAlignment = HorizontalAlignment.Center;
        message.AddThemeColorOverride("font_color", new Color(0.72f, 0.75f, 0.86f));
        message.AddThemeFontSizeOverride("font_size", 18);
        vbox.AddChild(message);

        var hint = new Label();
        hint.Text = "Press R to restart";
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        hint.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.6f));
        hint.AddThemeFontSizeOverride("font_size", 14);
        vbox.AddChild(hint);

        var btnBox = new CenterContainer();
        var button = new Button();
        button.Text = "  Restart  ";
        button.CustomMinimumSize = new Vector2(140, 40);
        var btnStyle = new StyleBoxFlat();
        btnStyle.BgColor = new Color(0.961f, 0.784f, 0.420f, 0.15f);
        btnStyle.BorderColor = new Color(0.961f, 0.784f, 0.420f, 0.4f);
        btnStyle.SetBorderWidthAll(1);
        btnStyle.SetCornerRadiusAll(6);
        button.AddThemeStyleboxOverride("normal", btnStyle);
        button.AddThemeColorOverride("font_color", new Color(0.961f, 0.784f, 0.420f));
        button.AddThemeFontSizeOverride("font_size", 16);
        btnBox.AddChild(button);
        vbox.AddChild(btnBox);

        _uiLayer.AddChild(_deathOverlay);
    }

    // ─── Performance Overlay ───

    private void CreatePerfOverlay()
    {
        _perfOverlay = new Panel();
        _perfOverlay.Position = new Vector2(1640, 4);
        _perfOverlay.Size = new Vector2(270, 140);

        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.0f, 0.0f, 0.0f, 0.7f);
        style.SetBorderWidthAll(1);
        style.BorderColor = new Color(0.3f, 0.8f, 0.3f, 0.4f);
        style.SetCornerRadiusAll(4);
        _perfOverlay.AddThemeStyleboxOverride("panel", style);

        var title = new Label();
        title.Text = "PERF MONITOR";
        title.Position = new Vector2(8, 4);
        title.AddThemeColorOverride("font_color", new Color(0.3f, 0.9f, 0.3f));
        title.AddThemeFontSizeOverride("font_size", 10);
        _perfOverlay.AddChild(title);

        _perfLabel = new Label();
        _perfLabel.Position = new Vector2(8, 22);
        _perfLabel.Size = new Vector2(254, 110);
        _perfLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.9f, 0.8f));
        _perfLabel.AddThemeFontSizeOverride("font_size", 11);
        _perfOverlay.AddChild(_perfLabel);

        _uiLayer.AddChild(_perfOverlay);
    }

    private void UpdatePerfOverlay()
    {
        if (_perfOverlay == null || _perfLabel == null) return;
        _perfOverlay.Visible = _perfVisible;
        if (!_perfVisible) return;

        double fps = Performance.GetMonitor(Performance.Monitor.TimeFps);
        double processMs = Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000;
        double physicsMs = Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * 1000;
        double memMb = Performance.GetMonitor(Performance.Monitor.MemoryStatic) / 1048576.0;
        double nodes = Performance.GetMonitor(Performance.Monitor.ObjectNodeCount);
        double objects = Performance.GetMonitor(Performance.Monitor.ObjectCount);

        _perfLabel.Text =
            $"FPS:        {fps:F0}\n" +
            $"Frame:      {processMs:F2}ms\n" +
            $"Physics:    {physicsMs:F2}ms\n" +
            $"Nodes:      {nodes:F0}\n" +
            $"Objects:    {objects:F0}\n" +
            $"Memory:     {memMb:F1}MB";
    }

    private double BenchmarkOp(Action op, int iterations)
    {
        ulong start = Time.GetTicksUsec();
        for (int i = 0; i < iterations; i++) op();
        return Time.GetTicksUsec() - start;
    }

    // ─── Visual Feedback Methods ───

    private void ShowFloatingText(Vector2 worldPos, string text, Color color, bool big = false)
    {
        var label = new Label();
        label.Text = text;
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeFontSizeOverride("font_size", big ? 20 : 14);
        label.Position = worldPos - new Vector2(20, 20);
        label.ZIndex = 100;
        AddChild(label);

        var tween = CreateTween();
        tween.TweenProperty(label, "position:y", worldPos.Y - 50, 0.9);
        tween.Parallel().TweenProperty(label, "modulate:a", 0.0, 0.9);
        tween.TweenCallback(Callable.From(label.QueueFree));
    }

    private void FlashEntity(string id, Color color)
    {
        if (!_entitySprites.TryGetValue(id, out var node)) return;
        var tween = CreateTween();
        tween.TweenProperty(node, "modulate", color, 0.06);
        tween.TweenProperty(node, "modulate", Colors.White, 0.14);
    }

    private void FlashCharacter(Color color)
    {
        if (_character == null) return;
        var tween = CreateTween();
        tween.TweenProperty(_character, "modulate", color, 0.06);
        tween.TweenProperty(_character, "modulate", Colors.White, 0.14);
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

    private void ShowSkillBurst(Vector2 pos, Color color)
    {
        var sprite = new Sprite2D();
        sprite.Texture = CreateColorTexture(new Color(color.R, color.G, color.B, 0.5f), 16);
        sprite.Position = pos;
        sprite.ZIndex = 50;
        AddChild(sprite);

        var tween = CreateTween();
        tween.TweenProperty(sprite, "scale", Vector2.One * 3.5f, 0.3);
        tween.Parallel().TweenProperty(sprite, "modulate:a", 0.0, 0.3);
        tween.TweenCallback(Callable.From(sprite.QueueFree));
    }

    private void MoveTo(Vector2 target)
    {
        _moveTarget = target;
        _isMoving = true;
    }

    private void SpawnEntity(string id, Vector2 position, string spritePath)
    {
        var sprite = new Sprite2D();
        var fullPath = AssetBase + spritePath;
        if (ResourceLoader.Exists(fullPath))
            sprite.Texture = GD.Load<Texture2D>(fullPath);
        else
        {
            GD.Print($"  [Sprite not found: {spritePath}, using placeholder]");
            sprite.Texture = CreateColorTexture(Colors.Magenta, 28);
        }
        sprite.Position = position;
        sprite.TextureFilter = TextureFilterEnum.Nearest;
        sprite.ZIndex = 5;
        _entityLayer.AddChild(sprite);
        _entitySprites[id] = sprite;
    }

    private void RemoveEntity(string id)
    {
        if (!_entitySprites.TryGetValue(id, out var node)) return;
        var tween = CreateTween();
        tween.TweenProperty(node, "modulate:a", 0.0, 0.3);
        tween.TweenCallback(Callable.From(node.QueueFree));
        _entitySprites.Remove(id);
    }

    private static ImageTexture CreateColorTexture(Color color, int size = 32)
    {
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        image.Fill(color);
        return ImageTexture.CreateFromImage(image);
    }

    // ==================== ROOM RENDERING (from AssetTest) ====================

    private void DrawFloor()
    {
        var floorTexture = GD.Load<Texture2D>(AssetBase + "dungeon/floor/grey_dirt_0_new.png");
        for (int x = 1; x < RoomWidth - 1; x++)
        {
            for (int y = 1; y < RoomHeight - 1; y++)
            {
                var sprite = new Sprite2D();
                sprite.Texture = floorTexture;
                sprite.Position = new Vector2(x * TileSize + TileSize / 2, y * TileSize + TileSize / 2);
                sprite.TextureFilter = TextureFilterEnum.Nearest;
                AddChild(sprite);
            }
        }
    }

    private void DrawWalls()
    {
        var wallTexture = GD.Load<Texture2D>(AssetBase + "dungeon/wall/brick_dark_0.png");
        for (int x = 0; x < RoomWidth; x++)
        {
            for (int y = 0; y < RoomHeight; y++)
            {
                if (x == 0 || x == RoomWidth - 1 || y == 0 || y == RoomHeight - 1)
                {
                    var sprite = new Sprite2D();
                    sprite.Texture = wallTexture;
                    sprite.Position = new Vector2(x * TileSize + TileSize / 2, y * TileSize + TileSize / 2);
                    sprite.TextureFilter = TextureFilterEnum.Nearest;
                    AddChild(sprite);
                }
            }
        }
    }

    private Node2D CreateCharacter()
    {
        var baseTexture = GD.Load<Texture2D>(AssetBase + "player/base/human_male.png");
        var armorTexture = GD.Load<Texture2D>(AssetBase + "player/body/chainmail.png");
        var weaponTexture = GD.Load<Texture2D>(AssetBase + "player/hand_right/long_sword.png");

        var center = new Vector2(RoomWidth / 2 * TileSize + TileSize / 2, RoomHeight / 2 * TileSize + TileSize / 2);

        var container = new Node2D();
        container.Position = center;
        container.ZIndex = 10;
        AddChild(container);

        var body = new Sprite2D { Texture = baseTexture, TextureFilter = TextureFilterEnum.Nearest, ZIndex = 1 };
        var armor = new Sprite2D { Texture = armorTexture, TextureFilter = TextureFilterEnum.Nearest, ZIndex = 2 };
        var weapon = new Sprite2D { Texture = weaponTexture, TextureFilter = TextureFilterEnum.Nearest, ZIndex = 3 };

        container.AddChild(body);
        container.AddChild(armor);
        container.AddChild(weapon);

        return container;
    }
}
