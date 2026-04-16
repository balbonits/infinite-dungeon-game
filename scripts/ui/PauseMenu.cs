using Godot;

namespace DungeonGame.Ui;

public partial class PauseMenu : Control
{
    private VBoxContainer _buttonContainer = null!;

    public override void _Ready()
    {
        _buttonContainer = GetNode<VBoxContainer>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer");

        var resumeButton = _buttonContainer.GetNode<Button>("ResumeButton");
        var backpackButton = _buttonContainer.GetNode<Button>("BackpackButton");
        var statsButton = _buttonContainer.GetNode<Button>("StatsButton");
        var skillsButton = _buttonContainer.GetNode<Button>("SkillsButton");
        var abilitiesButton = _buttonContainer.GetNode<Button>("AbilitiesButton");
        var ledgerButton = _buttonContainer.GetNode<Button>("LedgerButton");
        var tutorialButton = _buttonContainer.GetNode<Button>("TutorialButton");
        var settingsButton = _buttonContainer.GetNode<Button>("SettingsButton");
        var mainMenuButton = _buttonContainer.GetNode<Button>("MainMenuButton");
        var quitButton = _buttonContainer.GetNode<Button>("QuitButton");

        resumeButton.FocusMode = FocusModeEnum.All;
        backpackButton.FocusMode = FocusModeEnum.All;
        statsButton.FocusMode = FocusModeEnum.All;
        skillsButton.FocusMode = FocusModeEnum.All;
        abilitiesButton.FocusMode = FocusModeEnum.All;
        ledgerButton.FocusMode = FocusModeEnum.All;
        tutorialButton.FocusMode = FocusModeEnum.All;
        settingsButton.FocusMode = FocusModeEnum.All;
        mainMenuButton.FocusMode = FocusModeEnum.All;
        quitButton.FocusMode = FocusModeEnum.All;

        resumeButton.Connect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnResumePressed));
        backpackButton.Connect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnBackpackPressed));
        statsButton.Connect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnStatsPressed));
        skillsButton.Connect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnSkillsPressed));
        abilitiesButton.Connect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnAbilitiesPressed));
        ledgerButton.Connect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnLedgerPressed));
        tutorialButton.Connect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnTutorialPressed));
        settingsButton.Connect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnSettingsPressed));
        mainMenuButton.Connect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnMainMenuPressed));
        quitButton.Connect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnQuitPressed));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
        {
            var deathScreen = GetNode<Control>("../DeathScreen");
            if (deathScreen != null && deathScreen.Visible)
                return;

            if (Visible)
                OnResumePressed();
            else
                ShowMenu();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (!Visible) return;

        if (KeyboardNav.IsCancelPressed(@event))
        {
            OnResumePressed();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (KeyboardNav.HandleConfirm(@event, GetViewport()))
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        // Block ALL input when pause menu is open
        if (@event is InputEventKey k && k.Pressed)
            GetViewport().SetInputAsHandled();
    }

    private void ShowMenu()
    {
        Visible = true;
        GetTree().Paused = true;
        UiTheme.FocusFirstButton(_buttonContainer);
    }

    private void OnBackpackPressed()
    {
        Visible = false;
        BackpackWindow.Instance?.Open();
    }

    private void OnStatsPressed()
    {
        Visible = false;
        StatAllocDialog.Instance?.Show();
    }

    private void OnSkillsPressed()
    {
        Visible = false;
        SkillTreeDialog.Instance?.Show();
    }

    private void OnAbilitiesPressed()
    {
        Visible = false;
        AbilitiesDialog.Instance?.Show();
    }

    private void OnLedgerPressed()
    {
        Visible = false;
        DungeonLedger.Instance?.Show();
    }

    private void OnTutorialPressed()
    {
        Visible = false;
        TutorialPanel.Open(GetParent(), () =>
        {
            Visible = true;
            UiTheme.FocusFirstButton(_buttonContainer);
        });
    }

    private void OnSettingsPressed()
    {
        Visible = false;
        SettingsPanel.Open(GetParent(), () =>
        {
            Visible = true;
            UiTheme.FocusFirstButton(_buttonContainer);
        });
    }

    private void OnMainMenuPressed()
    {
        Visible = false;
        GetTree().Paused = false;
        Autoloads.SaveManager.Instance?.Save();
        GetTree().ReloadCurrentScene();
    }

    private void OnResumePressed()
    {
        Visible = false;
        GetTree().Paused = false;
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
