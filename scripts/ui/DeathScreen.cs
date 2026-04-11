using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Ui;

public partial class DeathScreen : Control
{
    public override void _Ready()
    {
        var restartButton = GetNode<Button>("CenterContainer/VBoxContainer/RestartButton");
        var quitButton = GetNode<Button>("CenterContainer/VBoxContainer/QuitButton");

        restartButton.Connect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnRestartPressed));
        quitButton.Connect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnQuitPressed));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible)
            return;

        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            if (keyEvent.Keycode == Key.R)
            {
                OnRestartPressed();
                GetViewport().SetInputAsHandled();
            }
            else if (keyEvent.Keycode == Key.Escape)
            {
                OnQuitPressed();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private void OnRestartPressed()
    {
        GameState.Instance.Reset();
        Visible = false;
        GetTree().Paused = false;
        Scenes.Main.Instance.LoadTown();
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
