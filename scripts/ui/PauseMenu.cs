using Godot;

namespace DungeonGame.Ui;

public partial class PauseMenu : Control
{
    public override void _Ready()
    {
        var resumeButton = GetNode<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/ResumeButton");
        var quitButton = GetNode<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/QuitButton");

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
        }
    }

    private void ShowMenu()
    {
        Visible = true;
        GetTree().Paused = true;
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
