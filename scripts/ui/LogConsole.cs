using Godot;

namespace DungeonGame.Ui;

/// <summary>
/// In-game log viewer. Toggle with F5. Tails the engine's log file
/// (user://logs/godot.log — enabled via project.godot file_logging) so every
/// GD.Print / GD.PrintErr / engine error surfaces in a scrollable panel
/// without requiring a terminal. Useful for visual test debugging and
/// inspecting runtime state during manual play.
/// </summary>
public partial class LogConsole : Control
{
    public static LogConsole? Instance { get; private set; }

    private const string LogPath = "user://logs/godot.log";
    private const double PollInterval = 0.3;

    private RichTextLabel _output = null!;
    private Label _statusLabel = null!;
    private bool _isOpen;
    private ulong _readPos;
    private double _sinceLastPoll;

    public override void _Ready()
    {
        Instance = this;
        Visible = false;
        ProcessMode = ProcessModeEnum.Always;
        MouseFilter = MouseFilterEnum.Stop;
        SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
        CustomMinimumSize = new Vector2(0, 260);

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", UiTheme.CreatePanelStyle(0.92f));
        panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);
        panel.AddChild(vbox);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 12);
        vbox.AddChild(header);

        var title = new Label { Text = "LOGS [F5]" };
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, UiTheme.FontSizes.Label);
        header.AddChild(title);

        _statusLabel = new Label { Text = "" };
        UiTheme.StyleLabel(_statusLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        _statusLabel.SizeFlagsHorizontal = SizeFlags.Expand;
        header.AddChild(_statusLabel);

        var clearBtn = new Button { Text = "Clear" };
        UiTheme.StyleSecondaryButton(clearBtn, UiTheme.FontSizes.Small);
        clearBtn.Pressed += () => { _output.Clear(); };
        header.AddChild(clearBtn);

        var copyBtn = new Button { Text = "Copy Path" };
        UiTheme.StyleSecondaryButton(copyBtn, UiTheme.FontSizes.Small);
        copyBtn.Pressed += () =>
        {
            DisplayServer.ClipboardSet(ProjectSettings.GlobalizePath(LogPath));
            _statusLabel.Text = "log path copied";
        };
        header.AddChild(copyBtn);

        _output = new RichTextLabel
        {
            BbcodeEnabled = false,
            SelectionEnabled = true,
            FocusMode = FocusModeEnum.None,
            ScrollFollowing = true,
        };
        _output.AddThemeFontSizeOverride("normal_font_size", UiTheme.FontSizes.Small);
        _output.SizeFlagsVertical = SizeFlags.ExpandFill;
        _output.CustomMinimumSize = new Vector2(0, 220);
        vbox.AddChild(_output);
    }

    public void Toggle()
    {
        _isOpen = !_isOpen;
        Visible = _isOpen;
        if (_isOpen)
            PollNow();
    }

    public override void _Process(double delta)
    {
        if (!_isOpen) return;
        _sinceLastPoll += delta;
        if (_sinceLastPoll < PollInterval) return;
        _sinceLastPoll = 0;
        PollNow();
    }

    private void PollNow()
    {
        using var f = FileAccess.Open(LogPath, FileAccess.ModeFlags.Read);
        if (f == null)
        {
            _statusLabel.Text = "log file not found";
            return;
        }

        ulong size = f.GetLength();
        if (size < _readPos) _readPos = 0; // file rotated
        if (size == _readPos) return;

        f.Seek(_readPos);
        string chunk = f.GetAsText(skipCr: true);
        _output.AppendText(chunk);
        _readPos = size;
        _statusLabel.Text = $"{size} bytes";
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.F5)
        {
            Toggle();
            GetViewport().SetInputAsHandled();
        }
    }
}
