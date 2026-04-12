using Godot;
using System;
using System.Collections.Generic;

namespace DungeonGame.Ui;

/// <summary>
/// Full-featured settings panel with tabbed sub-panels (Gameplay, Display, Audio, Controls).
/// Q/E to switch tabs, Up/Down to navigate settings, S to toggle/adjust.
/// Reusable — opened from both title screen and pause menu.
/// </summary>
public partial class SettingsPanel : Control
{
    public static SettingsPanel? ActiveInstance { get; private set; }

    private ColorRect _overlay = null!;
    private VBoxContainer _content = null!;
    private Label _tabLabel = null!;
    private ScrollContainer _scrollContainer = null!;
    private VBoxContainer _settingsList = null!;
    private Label _hintLabel = null!;

    private int _currentTab;
    private bool _isOpen;
    private Action? _onClose;

    private static readonly string[] TabNames = { "Gameplay", "Display", "Audio", "Controls" };

    public bool IsOpen => _isOpen;

    /// <summary>Create and show the settings panel as a child of the given parent.</summary>
    public static SettingsPanel Open(Node parent, Action? onClose = null)
    {
        var panel = new SettingsPanel();
        panel._onClose = onClose;
        parent.AddChild(panel);
        return panel;
    }

    public override void _Ready()
    {
        ActiveInstance = this;
        _isOpen = true;
        ProcessMode = ProcessModeEnum.Always;

        _overlay = new ColorRect();
        _overlay.Color = new Color(0, 0, 0, 0.7f);
        _overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        _overlay.MouseFilter = MouseFilterEnum.Stop;
        AddChild(_overlay);

        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", UiTheme.CreatePanelStyle(0.95f, true));
        panel.CustomMinimumSize = new Vector2(420, 0);
        center.AddChild(panel);

        _content = new VBoxContainer();
        _content.AddThemeConstantOverride("separation", 8);
        panel.AddChild(_content);

        // Title
        var title = new Label();
        title.Text = "SETTINGS";
        UiTheme.StyleLabel(title, UiTheme.Colors.Accent, UiTheme.FontSizes.Heading);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        _content.AddChild(title);

        // Tab bar (shows current tab name + Q/E hints)
        var tabBar = new HBoxContainer();
        tabBar.AddThemeConstantOverride("separation", 8);
        tabBar.Alignment = BoxContainer.AlignmentMode.Center;
        _content.AddChild(tabBar);

        var leftHint = new Label();
        leftHint.Text = $"[{GetActionKeyName(Constants.InputActions.ShoulderLeft)}]";
        UiTheme.StyleLabel(leftHint, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        tabBar.AddChild(leftHint);

        _tabLabel = new Label();
        _tabLabel.CustomMinimumSize = new Vector2(140, 0);
        _tabLabel.HorizontalAlignment = HorizontalAlignment.Center;
        UiTheme.StyleLabel(_tabLabel, UiTheme.Colors.Action, UiTheme.FontSizes.Label);
        tabBar.AddChild(_tabLabel);

        var rightHint = new Label();
        rightHint.Text = $"[{GetActionKeyName(Constants.InputActions.ShoulderRight)}]";
        UiTheme.StyleLabel(rightHint, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        tabBar.AddChild(rightHint);

        _content.AddChild(new HSeparator());

        // Scrollable settings list
        _scrollContainer = new ScrollContainer();
        _scrollContainer.CustomMinimumSize = new Vector2(0, 300);
        _content.AddChild(_scrollContainer);

        _settingsList = new VBoxContainer();
        _settingsList.AddThemeConstantOverride("separation", 4);
        _scrollContainer.AddChild(_settingsList);

        _content.AddChild(new HSeparator());

        // Hint label
        _hintLabel = new Label();
        _hintLabel.Text = "Up/Down: navigate | S: toggle | D: back";
        UiTheme.StyleLabel(_hintLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        _hintLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _content.AddChild(_hintLabel);

        // Back button
        var backBtn = new Button();
        backBtn.Text = "Back";
        backBtn.CustomMinimumSize = new Vector2(200, 38);
        backBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        backBtn.FocusMode = FocusModeEnum.All;
        UiTheme.StyleSecondaryButton(backBtn, UiTheme.FontSizes.Body);
        backBtn.Connect(BaseButton.SignalName.Pressed, Callable.From(Close));
        _content.AddChild(backBtn);

        BuildTab(0);
    }

    private void BuildTab(int tabIndex)
    {
        _currentTab = tabIndex;
        _tabLabel.Text = $"< {TabNames[tabIndex]} >";

        foreach (Node child in _settingsList.GetChildren())
            child.QueueFree();

        switch (tabIndex)
        {
            case 0: BuildGameplayTab(); break;
            case 1: BuildDisplayTab(); break;
            case 2: BuildAudioTab(); break;
            case 3: BuildControlsTab(); break;
        }

        CallDeferred(MethodName.FocusFirstSetting);
    }

    private void FocusFirstSetting()
    {
        UiTheme.FocusFirstButton(_settingsList);
    }

    // --- Tab Builders ---

    private void BuildGameplayTab()
    {
        AddToggle("Show Damage Numbers", GameSettings.ShowCombatNumbers, v => GameSettings.ShowCombatNumbers = v);
        AddToggle("Show XP Numbers", GameSettings.ShowXpNumbers, v => GameSettings.ShowXpNumbers = v);
        AddToggle("Show Enemy Levels", GameSettings.ShowEnemyLevels, v => GameSettings.ShowEnemyLevels = v);
        AddToggle("Auto-Loot Pickups", GameSettings.AutoLoot, v => GameSettings.AutoLoot = v);
        AddToggle("Confirm Floor Descent", GameSettings.ConfirmFloorDescent, v => GameSettings.ConfirmFloorDescent = v);
        AddToggle("Toast Notifications", GameSettings.ShowToastNotifications, v => GameSettings.ShowToastNotifications = v);
        AddChoice("Difficulty", new[] { "Easy", "Normal", "Hard" }, GameSettings.DifficultyLevel, v => GameSettings.DifficultyLevel = v);
    }

    private void BuildDisplayTab()
    {
        AddToggle("Camera Shake on Damage", GameSettings.CameraShakeOnDamage, v => GameSettings.CameraShakeOnDamage = v);
        AddToggle("Screen Flash Effects", GameSettings.ScreenFlash, v => GameSettings.ScreenFlash = v);
        AddToggle("Show Minimap", GameSettings.ShowMinimap, v => GameSettings.ShowMinimap = v);
        AddToggle("Show Skill Bar", GameSettings.ShowSkillBar, v => GameSettings.ShowSkillBar = v);
        AddToggle("Show HP/MP Orbs", GameSettings.ShowHudOrbs, v => GameSettings.ShowHudOrbs = v);
        AddToggle("Show Stairs Compass", GameSettings.ShowStairsCompass, v => GameSettings.ShowStairsCompass = v);
        AddChoice("UI Scale", new[] { "75%", "100%", "125%", "150%" }, (GameSettings.UiScale - 75) / 25, v => GameSettings.UiScale = 75 + v * 25);
    }

    private void BuildAudioTab()
    {
        AddSlider("Master Volume", GameSettings.MasterVolume, v => GameSettings.MasterVolume = v);
        AddSlider("Music Volume", GameSettings.MusicVolume, v => GameSettings.MusicVolume = v);
        AddSlider("SFX Volume", GameSettings.SfxVolume, v => GameSettings.SfxVolume = v);
        AddSlider("Ambient Volume", GameSettings.AmbientVolume, v => GameSettings.AmbientVolume = v);
        AddToggle("Mute When Unfocused", GameSettings.MuteOnFocusLoss, v => GameSettings.MuteOnFocusLoss = v);
    }

    private void BuildControlsTab()
    {
        AddToggle("Show Control Hints", GameSettings.ShowControlHints, v => GameSettings.ShowControlHints = v);
        AddChoice("Controller Scheme", new[] { "Keyboard", "Gamepad" }, GameSettings.ControllerScheme, v => GameSettings.ControllerScheme = v);

        // Key bindings reference (read-only)
        AddSectionHeader("Key Bindings");
        AddReadOnlyRow("Move", "Arrow Keys");
        AddReadOnlyRow("Confirm / Attack", GetActionKeyName(Constants.InputActions.ActionCross));
        AddReadOnlyRow("Cancel / Back", GetActionKeyName(Constants.InputActions.ActionCircle));
        AddReadOnlyRow("Skill 1", $"{GetActionKeyName(Constants.InputActions.ShoulderLeft)}+{GetActionKeyName(Constants.InputActions.ActionTriangle)}");
        AddReadOnlyRow("Skill 2", $"{GetActionKeyName(Constants.InputActions.ShoulderLeft)}+{GetActionKeyName(Constants.InputActions.ActionCross)}");
        AddReadOnlyRow("Skill 3", $"{GetActionKeyName(Constants.InputActions.ShoulderRight)}+{GetActionKeyName(Constants.InputActions.ActionTriangle)}");
        AddReadOnlyRow("Skill 4", $"{GetActionKeyName(Constants.InputActions.ShoulderRight)}+{GetActionKeyName(Constants.InputActions.ActionCross)}");
        AddReadOnlyRow("Map Toggle", GetActionKeyName(Constants.InputActions.MapToggle));
        AddReadOnlyRow("Pause / Menu", "Esc");
    }

    // --- Widget Helpers ---

    private void AddToggle(string label, bool value, Action<bool> onChanged)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        _settingsList.AddChild(row);

        var lbl = new Label();
        lbl.Text = label;
        UiTheme.StyleLabel(lbl, UiTheme.Colors.Ink, UiTheme.FontSizes.Body);
        lbl.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(lbl);

        var toggle = new CheckButton();
        toggle.ButtonPressed = value;
        toggle.FocusMode = FocusModeEnum.All;
        toggle.Connect(BaseButton.SignalName.Toggled, Callable.From((bool on) => onChanged(on)));
        row.AddChild(toggle);
    }

    private void AddChoice(string label, string[] options, int currentIndex, Action<int> onChanged)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        _settingsList.AddChild(row);

        var lbl = new Label();
        lbl.Text = label;
        UiTheme.StyleLabel(lbl, UiTheme.Colors.Ink, UiTheme.FontSizes.Body);
        lbl.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(lbl);

        int idx = Math.Clamp(currentIndex, 0, options.Length - 1);
        var btn = new Button();
        btn.Text = options[idx];
        btn.CustomMinimumSize = new Vector2(80, 28);
        btn.FocusMode = FocusModeEnum.All;
        UiTheme.StyleSecondaryButton(btn, UiTheme.FontSizes.Small);
        int capturedIdx = idx;
        btn.Connect(BaseButton.SignalName.Pressed, Callable.From(() =>
        {
            capturedIdx = (capturedIdx + 1) % options.Length;
            btn.Text = options[capturedIdx];
            onChanged(capturedIdx);
        }));
        row.AddChild(btn);
    }

    private void AddSlider(string label, int value, Action<int> onChanged)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        _settingsList.AddChild(row);

        var lbl = new Label();
        lbl.Text = label;
        UiTheme.StyleLabel(lbl, UiTheme.Colors.Ink, UiTheme.FontSizes.Body);
        lbl.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(lbl);

        var valLabel = new Label();
        valLabel.Text = $"{value}%";
        valLabel.CustomMinimumSize = new Vector2(40, 0);
        UiTheme.StyleLabel(valLabel, UiTheme.Colors.Info, UiTheme.FontSizes.Small);
        valLabel.HorizontalAlignment = HorizontalAlignment.Right;

        var slider = new HSlider();
        slider.MinValue = 0;
        slider.MaxValue = 100;
        slider.Step = 5;
        slider.Value = value;
        slider.CustomMinimumSize = new Vector2(100, 20);
        slider.FocusMode = FocusModeEnum.All;
        slider.Connect(Godot.Range.SignalName.ValueChanged, Callable.From((double v) =>
        {
            onChanged((int)v);
            valLabel.Text = $"{(int)v}%";
        }));
        row.AddChild(slider);
        row.AddChild(valLabel);
    }

    private void AddSectionHeader(string text)
    {
        _settingsList.AddChild(new HSeparator());
        var lbl = new Label();
        lbl.Text = text;
        UiTheme.StyleLabel(lbl, UiTheme.Colors.Accent, UiTheme.FontSizes.Body);
        lbl.HorizontalAlignment = HorizontalAlignment.Center;
        _settingsList.AddChild(lbl);
    }

    private void AddReadOnlyRow(string action, string key)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        _settingsList.AddChild(row);

        var actionLabel = new Label();
        actionLabel.Text = action;
        UiTheme.StyleLabel(actionLabel, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        actionLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(actionLabel);

        var keyLabel = new Label();
        keyLabel.Text = key;
        UiTheme.StyleLabel(keyLabel, UiTheme.Colors.Ink, UiTheme.FontSizes.Small);
        row.AddChild(keyLabel);
    }

    // --- Input ---

    public void Close()
    {
        _isOpen = false;
        ActiveInstance = null;
        GameSettings.Save();
        _onClose?.Invoke();
        QueueFree();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_isOpen) return;

        if (KeyboardNav.IsCancelPressed(@event))
        {
            Close();
            GetViewport().SetInputAsHandled();
            return;
        }

        // Q/E switch tabs
        if (@event.IsActionPressed(Constants.InputActions.ShoulderLeft))
        {
            BuildTab((_currentTab - 1 + TabNames.Length) % TabNames.Length);
            GetViewport().SetInputAsHandled();
            return;
        }
        if (@event.IsActionPressed(Constants.InputActions.ShoulderRight))
        {
            BuildTab((_currentTab + 1) % TabNames.Length);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (KeyboardNav.HandleInput(@event, _settingsList))
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventKey k && k.Pressed)
            GetViewport().SetInputAsHandled();
    }

    private static string GetActionKeyName(string action)
    {
        var events = InputMap.ActionGetEvents(action);
        foreach (var ev in events)
            if (ev is InputEventKey keyEv)
                return OS.GetKeycodeString(keyEv.Keycode);
        return "?";
    }
}
