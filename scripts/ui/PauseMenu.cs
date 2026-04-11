using Godot;

namespace DungeonGame.Ui;

public partial class PauseMenu : Control
{
    private VBoxContainer _buttonContainer = null!;

    public override void _Ready()
    {
        _buttonContainer = GetNode<VBoxContainer>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer");

        var resumeButton = _buttonContainer.GetNode<Button>("ResumeButton");
        var quitButton = _buttonContainer.GetNode<Button>("QuitButton");

        resumeButton.FocusMode = FocusModeEnum.All;
        quitButton.FocusMode = FocusModeEnum.All;

        resumeButton.Connect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnResumePressed));
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
