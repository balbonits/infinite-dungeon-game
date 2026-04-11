using Godot;
using System.Collections.Generic;

namespace DungeonGame.Ui;

/// <summary>
/// Visual novel-style dialogue box. Character portrait on left, name above,
/// typewriter text effect, advance with action button.
/// Usage: DialogueBox.Instance.Play(lines);
/// </summary>
public partial class DialogueBox : Control
{
    public static DialogueBox Instance { get; private set; } = null!;

    private const float TypewriterSpeed = 0.03f;

    private ColorRect _overlay = null!;
    private PanelContainer _panel = null!;
    private TextureRect _portrait = null!;
    private Label _nameLabel = null!;
    private Label _textLabel = null!;
    private Label _continueHint = null!;
    private Queue<DialogueLine> _lineQueue = new();
    private string _fullText = "";
    private int _visibleChars;
    private float _charTimer;
    private bool _isTyping;
    private bool _isOpen;
    private System.Action? _onComplete;

    public bool IsOpen => _isOpen;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
        MouseFilter = MouseFilterEnum.Ignore;

        // Dim overlay
        _overlay = new ColorRect();
        _overlay.Color = new Color(0, 0, 0, 0.3f);
        _overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        _overlay.Visible = false;
        _overlay.MouseFilter = MouseFilterEnum.Stop;
        AddChild(_overlay);

        // Bottom panel
        _panel = new PanelContainer();
        _panel.AddThemeStyleboxOverride("panel", UiTheme.CreatePanelStyle(0.95f, true));
        _panel.SetAnchorsPreset(LayoutPreset.BottomWide);
        _panel.OffsetTop = -160;
        _panel.OffsetLeft = 40;
        _panel.OffsetRight = -40;
        _panel.OffsetBottom = -20;
        _panel.Visible = false;
        AddChild(_panel);

        var margin = new MarginContainer();
        _panel.AddChild(margin);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 16);
        margin.AddChild(hbox);

        // Portrait
        _portrait = new TextureRect();
        _portrait.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
        _portrait.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        _portrait.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        _portrait.CustomMinimumSize = new Vector2(80, 80);
        hbox.AddChild(_portrait);

        // Text column
        var textVbox = new VBoxContainer();
        textVbox.AddThemeConstantOverride("separation", 4);
        textVbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        hbox.AddChild(textVbox);

        _nameLabel = new Label();
        UiTheme.StyleLabel(_nameLabel, UiTheme.Colors.Accent, UiTheme.FontSizes.Button);
        textVbox.AddChild(_nameLabel);

        _textLabel = new Label();
        UiTheme.StyleLabel(_textLabel, UiTheme.Colors.Ink, UiTheme.FontSizes.Body);
        _textLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _textLabel.SizeFlagsVertical = SizeFlags.ExpandFill;
        textVbox.AddChild(_textLabel);

        _continueHint = new Label();
        _continueHint.Text = Strings.Dialogue.ContinueHint;
        UiTheme.StyleLabel(_continueHint, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        _continueHint.HorizontalAlignment = HorizontalAlignment.Right;
        _continueHint.Visible = false;
        textVbox.AddChild(_continueHint);
    }

    /// <summary>
    /// Play a sequence of dialogue lines. Calls onComplete when all lines are done.
    /// </summary>
    public void Play(DialogueLine[] lines, System.Action? onComplete = null)
    {
        _lineQueue.Clear();
        foreach (var line in lines)
            _lineQueue.Enqueue(line);

        _onComplete = onComplete;
        _isOpen = true;
        _overlay.Visible = true;
        _panel.Visible = true;
        GetTree().Paused = true;
        ShowNextLine();
    }

    private void ShowNextLine()
    {
        if (_lineQueue.Count == 0)
        {
            Close();
            return;
        }

        var line = _lineQueue.Dequeue();
        _nameLabel.Text = line.Speaker;
        _fullText = line.Text;
        _textLabel.Text = "";
        _visibleChars = 0;
        _charTimer = 0;
        _isTyping = true;
        _continueHint.Visible = false;

        // Set portrait if available
        if (!string.IsNullOrEmpty(line.PortraitPath) && ResourceLoader.Exists(line.PortraitPath))
        {
            _portrait.Texture = GD.Load<Texture2D>(line.PortraitPath);
            _portrait.Visible = true;
        }
        else
        {
            _portrait.Visible = false;
        }
    }

    public override void _Process(double delta)
    {
        if (!_isTyping)
            return;

        _charTimer += (float)delta;
        while (_charTimer >= TypewriterSpeed && _visibleChars < _fullText.Length)
        {
            _charTimer -= TypewriterSpeed;
            _visibleChars++;
            _textLabel.Text = _fullText[.._visibleChars];
        }

        if (_visibleChars >= _fullText.Length)
        {
            _isTyping = false;
            _continueHint.Visible = true;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_isOpen)
            return;

        bool actionPressed = @event.IsActionPressed(Constants.InputActions.ActionCross) ||
                             (@event is InputEventKey keyEvent && keyEvent.Pressed &&
                              (keyEvent.Keycode == Key.Space || keyEvent.Keycode == Key.Enter));

        if (!actionPressed)
            return;

        if (_isTyping)
        {
            // Skip typewriter — show full text immediately
            _visibleChars = _fullText.Length;
            _textLabel.Text = _fullText;
            _isTyping = false;
            _continueHint.Visible = true;
        }
        else
        {
            // Advance to next line
            ShowNextLine();
        }

        GetViewport().SetInputAsHandled();
    }

    private void Close()
    {
        _isOpen = false;
        _overlay.Visible = false;
        _panel.Visible = false;
        GetTree().Paused = false;
        _onComplete?.Invoke();
    }
}

/// <summary>
/// A single line of dialogue.
/// </summary>
public record DialogueLine(string Speaker, string Text, string PortraitPath = "");
