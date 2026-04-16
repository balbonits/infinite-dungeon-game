using Godot;
using DungeonGame.Autoloads;
#if DEBUG
using System.Reflection;
using Chickensoft.GoDotTest;
#endif

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

        // Apply global theme to all UI — set on each root Control so it cascades
        var globalTheme = Ui.GlobalTheme.Create();
        var uiLayer = GetNode<CanvasLayer>("UILayer");
        foreach (Node child in uiLayer.GetChildren())
        {
            if (child is Control control)
                control.Theme = globalTheme;
        }

        GameState.Instance.Connect(
            GameState.SignalName.PlayerDied,
            new Callable(this, MethodName.OnPlayerDied));

        var existingWorld = GetNodeOrNull("Dungeon") ?? GetNodeOrNull("Town");
        existingWorld?.QueueFree();

        // Start with splash screen
        CallDeferred(MethodName.ShowSplashScreen);

#if DEBUG
        // After the game boots, run tests if --run-tests was passed
        var testEnv = TestEnvironment.From(OS.GetCmdlineArgs());
        if (testEnv.ShouldRunTests)
            CallDeferred(MethodName.RunTests);
#endif
    }

    private void ShowSplashScreen()
    {
        var splash = new Ui.SplashScreen();
        splash.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        splash.Theme = Ui.GlobalTheme.Create();

        splash.Connect(Ui.SplashScreen.SignalName.NewGamePressed, Callable.From(() =>
        {
            splash.Visible = false;
            splash.QueueFree();
            ShowClassSelection();
        }));

        splash.Connect(Ui.SplashScreen.SignalName.ContinuePressed, Callable.From(() =>
        {
            // Let ScreenTransition cover the splash screen during fade-to-black,
            // then hide splash, load save, and swap in the town — all while the
            // overlay is fully opaque. Prevents the town from flashing into view.
            Ui.ScreenTransition.Instance.Play(
                Strings.Town.Title,
                () =>
                {
                    splash.Visible = false;
                    splash.QueueFree();
                    Autoloads.SaveManager.Instance.Load();
                    GetTree().Paused = false;
                    LoadTown();
                },
                Strings.Town.Arriving);
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
        if (Ui.ScreenTransition.Instance.IsTransitioning)
        {
            // Already inside a transition — do work directly
            DoLoadTown();
        }
        else
        {
            Ui.ScreenTransition.Instance.Play(Strings.Town.Title, DoLoadTown, Strings.Town.Arriving);
        }
    }

    private void DoLoadTown()
    {
        SwapWorld(TownScene);
        Ui.StairsCompass.Instance?.ClearTargets();
        Autoloads.SaveManager.Instance?.Save();
    }

    public void LoadDungeon()
    {
        if (Ui.ScreenTransition.Instance.IsTransitioning)
        {
            DoLoadDungeon();
        }
        else
        {
            int floor = GameState.Instance.FloorNumber;
            Ui.ScreenTransition.Instance.Play(Strings.Floor.FloorNumber(floor), DoLoadDungeon, Strings.Floor.Entering);
        }
    }

    private void DoLoadDungeon()
    {
        SwapWorld(DungeonScene);
        Autoloads.SaveManager.Instance?.Save();
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
        GetTree().Paused = true;
        if (_deathScreen is Ui.DeathScreen ds)
            ds.ShowDeathFlow();
    }

#if DEBUG
    private void RunTests()
    {
        // Attach test runner to the SceneTree root (not Main) so it survives scene changes.
        // GoDotTest uses the passed node as TestScene — if that node is freed during tests
        // (e.g., when ChangeSceneToFile is called during class select confirm), all subsequent
        // tests throw ObjectDisposedException. Root-level attachment avoids this.
        var testRoot = new Node { Name = "TestRoot" };
        GetTree().Root.AddChild(testRoot);
        testRoot.ProcessMode = ProcessModeEnum.Always;
        _ = GoTest.RunTests(Assembly.GetExecutingAssembly(), testRoot);
    }
#endif
}
