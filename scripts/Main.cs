using Godot;
using DungeonGame.Autoloads;

namespace DungeonGame.Scenes;

public partial class Main : Node
{
    private static readonly PackedScene TownScene = GD.Load<PackedScene>(Constants.Assets.TownScene);
    private static readonly PackedScene DungeonScene = GD.Load<PackedScene>(Constants.Assets.DungeonScene);

    public static Main Instance { get; private set; } = null!;

    private Control _deathScreen = null!;
    private Control _classSelect = null!;
    private Node? _currentWorld;

    public override void _Ready()
    {
        Instance = this;
        _deathScreen = GetNode<Control>("UILayer/DeathScreen");

        // Apply global theme to UILayer
        var uiLayer = GetNode<CanvasLayer>("UILayer");
        foreach (Node child in uiLayer.GetChildren())
        {
            if (child is Control control)
                control.Theme = Ui.GlobalTheme.Create();
        }

        GameState.Instance.Connect(
            GameState.SignalName.PlayerDied,
            new Callable(this, MethodName.OnPlayerDied));

        var existingWorld = GetNodeOrNull("Dungeon") ?? GetNodeOrNull("Town");
        existingWorld?.QueueFree();

        // Start with splash screen
        CallDeferred(MethodName.ShowSplashScreen);
    }

    private void ShowSplashScreen()
    {
        var splash = new Ui.SplashScreen();
        splash.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        splash.Connect(Ui.SplashScreen.SignalName.ContinuePressed, Callable.From(() =>
        {
            splash.Visible = false;
            splash.QueueFree();
            ShowClassSelection();
        }));
        GetNode<CanvasLayer>("UILayer").AddChild(splash);
        GetTree().Paused = true;
    }

    private void ShowClassSelection()
    {
        _classSelect = new Ui.ClassSelect();
        _classSelect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        GetNode<CanvasLayer>("UILayer").AddChild(_classSelect);
        GetTree().Paused = true;
    }

    public void LoadTown()
    {
        SwapWorld(TownScene);
    }

    public void LoadDungeon()
    {
        SwapWorld(DungeonScene);
    }

    private void SwapWorld(PackedScene scene)
    {
        if (_currentWorld != null)
        {
            _currentWorld.QueueFree();
            _currentWorld = null;
        }

        _currentWorld = scene.Instantiate();
        AddChild(_currentWorld);
        MoveChild(_currentWorld, 0);
    }

    private void OnPlayerDied()
    {
        _deathScreen.Visible = true;
        GetTree().Paused = true;
    }
}
