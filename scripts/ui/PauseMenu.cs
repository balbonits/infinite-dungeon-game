using Godot;

namespace DungeonGame.Ui;

public partial class PauseMenu : Control
{
    private VBoxContainer _buttonContainer = null!;

    public override void _Ready()
    {
        _buttonContainer = GetNode<VBoxContainer>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer");

        var resumeButton = _buttonContainer.GetNode<Button>("ResumeButton");
        var statsButton = _buttonContainer.GetNode<Button>("StatsButton");
        var mainMenuButton = _buttonContainer.GetNode<Button>("MainMenuButton");
        var quitButton = _buttonContainer.GetNode<Button>("QuitButton");

        resumeButton.FocusMode = FocusModeEnum.All;
        statsButton.FocusMode = FocusModeEnum.All;
        mainMenuButton.FocusMode = FocusModeEnum.All;
        quitButton.FocusMode = FocusModeEnum.All;

        resumeButton.Connect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnResumePressed));
        statsButton.Connect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnStatsPressed));
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

        if (Visible && KeyboardNav.HandleInput(@event, _buttonContainer))
            GetViewport().SetInputAsHandled();
    }

    private void ShowMenu()
    {
        Visible = true;
        GetTree().Paused = true;
        UiTheme.FocusFirstButton(_buttonContainer);
    }

    private void OnStatsPressed()
    {
        Visible = false;
        StatAllocDialog.Instance?.Show();
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
