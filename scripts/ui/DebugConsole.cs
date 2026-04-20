using Godot;
using System;
using System.Collections.Generic;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

/// <summary>
/// Debug console with cheat commands. Toggle with F4.
/// God mode, free XP/gold/levels, teleport, spawn, give items,
/// show hitboxes, performance stats.
/// </summary>
public partial class DebugConsole : Control
{
    public static DebugConsole? Instance { get; private set; }

    private VBoxContainer _buttonList = null!;
    private Label _statusLabel = null!;
    private ScrollContainer _scroll = null!;
    private bool _isOpen;
    private bool _godMode;
    private bool _showCollisions;
    private bool _showPerf;
    private Label? _perfLabel;

    public bool IsGodMode => _godMode;

    public override void _Ready()
    {
        Instance = this;
        Visible = false;
        ProcessMode = ProcessModeEnum.Always;
        MouseFilter = MouseFilterEnum.Ignore;

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", UiTheme.CreatePanelStyle(0.92f));
        panel.SetAnchorsPreset(LayoutPreset.TopRight);
        panel.Position = new Vector2(-240, 12);
        panel.CustomMinimumSize = new Vector2(220, 0);
        AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);
        panel.AddChild(vbox);

        var title = new Label();
        title.Text = "DEBUG [F4]";
        UiTheme.StyleLabel(title, UiTheme.Colors.Danger, UiTheme.FontSizes.Label);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        _statusLabel = new Label();
        UiTheme.StyleLabel(_statusLabel, UiTheme.Colors.Safe, UiTheme.FontSizes.Small);
        _statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _statusLabel.Text = "";
        vbox.AddChild(_statusLabel);

        vbox.AddChild(new HSeparator());

        _scroll = new ScrollContainer { FollowFocus = true };
        _scroll.CustomMinimumSize = new Vector2(0, 360);
        vbox.AddChild(_scroll);

        _buttonList = new VBoxContainer();
        _buttonList.AddThemeConstantOverride("separation", 3);
        _scroll.AddChild(_buttonList);

        BuildCommands();
    }

    private void BuildCommands()
    {
        AddSection("CHEATS");
        AddCmd("God Mode (toggle)", () =>
        {
            _godMode = !_godMode;
            Status(_godMode ? "GOD MODE ON" : "God mode off");
        });
        AddCmd("+1000 XP", () =>
        {
            GameState.Instance.AwardXp(1000);
            Status("+1000 XP");
        });
        AddCmd("+10000 XP", () =>
        {
            GameState.Instance.AwardXp(10000);
            Status("+10000 XP");
        });
        AddCmd("+5 Levels", () =>
        {
            for (int i = 0; i < 5; i++)
            {
                int needed = Constants.Leveling.GetXpToLevel(GameState.Instance.Level) - GameState.Instance.Xp;
                GameState.Instance.AwardXp(needed);
            }
            Status($"Level {GameState.Instance.Level}");
        });
        AddCmd("+1000 Gold", () =>
        {
            GameState.Instance.PlayerInventory.Gold += 1000;
            GameState.Instance.EmitSignal(GameState.SignalName.StatsChanged);
            Status("+1000 gold");
        });
        AddCmd("+10000 Gold", () =>
        {
            GameState.Instance.PlayerInventory.Gold += 10000;
            GameState.Instance.EmitSignal(GameState.SignalName.StatsChanged);
            Status("+10000 gold");
        });
        AddCmd("Full HP", () =>
        {
            GameState.Instance.Hp = GameState.Instance.MaxHp;
            Status("HP restored");
        });
        AddCmd("Full Mana", () =>
        {
            GameState.Instance.Mana = GameState.Instance.MaxMana;
            Status("Mana restored");
        });
        AddCmd("Full HP + Mana", () =>
        {
            GameState.Instance.Hp = GameState.Instance.MaxHp;
            GameState.Instance.Mana = GameState.Instance.MaxMana;
            Status("HP + Mana restored");
        });
        AddCmd("+10 All Stats", () =>
        {
            var s = GameState.Instance.Stats;
            s.Str += 10; s.Dex += 10; s.Sta += 10; s.Int += 10;
            GameState.Instance.RecomputeDerivedStats(); // COMBAT-01 §5: unified path.
            GameState.Instance.EmitSignal(GameState.SignalName.StatsChanged);
            Status("+10 STR/DEX/STA/INT");
        });

        AddSection("TELEPORT");
        AddCmd("Go to Floor 1", () => TeleportFloor(1));
        AddCmd("Go to Floor 10", () => TeleportFloor(10));
        AddCmd("Go to Floor 25", () => TeleportFloor(25));
        AddCmd("Go to Floor 50", () => TeleportFloor(50));
        AddCmd("Go to Floor 100", () => TeleportFloor(100));
        AddCmd("Go to Town", () =>
        {
            Toggle(); // close console
            Scenes.Main.Instance.LoadTown();
            Status("Town");
        });

        AddSection("SPAWN");
        AddCmd("Kill All Enemies", () =>
        {
            int killed = 0;
            foreach (Node node in GetTree().GetNodesInGroup(Constants.Groups.Enemies))
            {
                if (node is IDamageable dmg)
                    dmg.TakeDamage(999999);
                killed++;
            }
            Status($"Killed {killed}");
        });

        AddSection("ITEMS");
        AddCmd("Give All Shop Items", () =>
        {
            int given = 0;
            foreach (var item in ItemDatabase.All)
            {
                if (GameState.Instance.PlayerInventory.TryAdd(item))
                    given++;
            }
            Status($"Added {given} items");
        });
        AddCmd("+10 Skill Points", () =>
        {
            GameState.Instance.Progression.SkillPoints += 10;
            Status("+10 SP");
        });
        AddCmd("+10 Ability Points", () =>
        {
            GameState.Instance.Progression.AbilityPoints += 10;
            Status("+10 AP");
        });
        AddCmd("+5 Stat Points", () =>
        {
            GameState.Instance.Stats.FreePoints += 5;
            GameState.Instance.EmitSignal(GameState.SignalName.StatsChanged);
            Status("+5 stat pts");
        });

        AddSection("DISPLAY");
        AddCmd("Perf Metrics (toggle)", () =>
        {
            _showPerf = !_showPerf;
            if (_perfLabel == null)
            {
                _perfLabel = new Label();
                _perfLabel.Name = "PerfMetrics";
                _perfLabel.SetAnchorsPreset(LayoutPreset.BottomLeft);
                _perfLabel.Position = new Vector2(130, -40);
                UiTheme.StyleLabel(_perfLabel, UiTheme.Colors.Safe, 9);
                _perfLabel.ProcessMode = ProcessModeEnum.Always;
                GetTree().Root.AddChild(_perfLabel);
            }
            _perfLabel.Visible = _showPerf;
            Status(_showPerf ? "Perf ON" : "Perf off");
        });
        AddCmd("Show Collision Shapes", () =>
        {
            _showCollisions = !_showCollisions;
            GetTree().DebugCollisionsHint = _showCollisions;
            Status(_showCollisions ? "Collisions ON" : "Collisions off");
        });

        AddSection("SAVE");
        AddCmd("Force Save", () =>
        {
            bool ok = SaveManager.Instance?.Save() ?? false;
            Status(ok ? "Saved" : "Save FAILED");
        });
        AddCmd("Reset Save (DANGER)", () =>
        {
            GameState.Instance.Reset();
            Status("Save reset");
        });
    }

    private void TeleportFloor(int floor)
    {
        Toggle(); // close console
        GameState.Instance.FloorNumber = floor;
        Scenes.Main.Instance.LoadDungeon();
        Status($"Floor {floor}");
    }

    private void AddSection(string title)
    {
        var lbl = new Label();
        lbl.Text = $"── {title} ──";
        UiTheme.StyleLabel(lbl, UiTheme.Colors.Accent, UiTheme.FontSizes.Small);
        lbl.HorizontalAlignment = HorizontalAlignment.Center;
        _buttonList.AddChild(lbl);
    }

    private void AddCmd(string label, Action action)
    {
        var btn = new Button();
        btn.Text = label;
        btn.CustomMinimumSize = new Vector2(0, 26);
        btn.FocusMode = FocusModeEnum.All;
        btn.Alignment = HorizontalAlignment.Left;
        UiTheme.StyleButton(btn, UiTheme.FontSizes.Small);
        btn.Connect(BaseButton.SignalName.Pressed, Callable.From(action));
        _buttonList.AddChild(btn);
    }

    private void Status(string msg)
    {
        _statusLabel.Text = msg;
    }

    public void Toggle()
    {
        _isOpen = !_isOpen;
        Visible = _isOpen;
        if (_isOpen)
            UiTheme.FocusFirstButton(_buttonList);
    }

    public override void _Process(double delta)
    {
        // Perf metrics
        if (_showPerf && _perfLabel is { Visible: true })
        {
            int fps = (int)Engine.GetFramesPerSecond();
            float frameMs = 1000f / Mathf.Max(1, fps);
            int objects = (int)Performance.GetMonitor(Performance.Monitor.ObjectCount);
            int nodes = (int)Performance.GetMonitor(Performance.Monitor.ObjectNodeCount);
            int drawCalls = (int)Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame);
            float vram = (float)(Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed) / 1048576.0);
            _perfLabel.Text = $"FPS: {fps} ({frameMs:F1}ms) | Nodes: {nodes} | Draw: {drawCalls} | VRAM: {vram:F1}MB";
        }

        // God mode: keep HP/MP full
        if (_godMode && !GameState.Instance.IsDead)
        {
            GameState.Instance.Hp = GameState.Instance.MaxHp;
            GameState.Instance.Mana = GameState.Instance.MaxMana;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.F4)
        {
            Toggle();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (!_isOpen) return;

        if (KeyboardNav.IsCancelPressed(@event))
        {
            Toggle();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (KeyboardNav.HandleConfirm(@event, GetViewport()))
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventKey k && k.Pressed)
            GetViewport().SetInputAsHandled();
    }
}
