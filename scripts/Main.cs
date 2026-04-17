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
            // Spec: if all 3 save slots are full, block New Game and nudge to Load Game
            // for deletion. See docs/flows/load-game.md § Interaction with New Game.
            var sm = Autoloads.SaveManager.Instance;
            if (sm != null && sm.AreAllSlotsFull())
            {
                Ui.Toast.Instance?.Error(
                    "All save slots are full. Delete a character from Load Game first.");
                return;
            }
            // Reserve the first empty slot as the new character's home. Auto-save targets it.
            if (sm != null)
                Autoloads.GameState.Instance.CurrentSaveSlot = sm.FindFirstEmptySlot();

            splash.Visible = false;
            splash.QueueFree();
            ShowClassSelection();
        }));

        splash.Connect(Ui.SplashScreen.SignalName.ContinuePressed, Callable.From(() =>
        {
            ShowLoadGameScreen(splash);
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

    /// <summary>
    /// Show the Load Game screen as an overlay on top of the splash. On Back, the
    /// Load Game screen frees itself and the splash is re-shown. On Load, a
    /// ScreenTransition covers everything while the save is restored and town loads.
    /// </summary>
    private void ShowLoadGameScreen(Ui.SplashScreen splash)
    {
        splash.Visible = false;

        var screen = new Ui.LoadGameScreen();
        screen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        screen.Theme = Ui.GlobalTheme.Create();

        screen.Connect(Ui.LoadGameScreen.SignalName.BackPressed, Callable.From(() =>
        {
            screen.QueueFree();
            splash.Visible = true;
        }));

        screen.Connect(Ui.LoadGameScreen.SignalName.LoadSelected, Callable.From((int slotIndex) =>
        {
            // Cover with a fade-to-black, then swap GameState under the overlay and
            // transition into town. Prevents any flash of splash or stale state.
            Ui.ScreenTransition.Instance.Play(
                Strings.Town.Title,
                () =>
                {
                    screen.QueueFree();
                    splash.QueueFree();
                    var loaded = Autoloads.SaveManager.Instance?.LoadSlot(slotIndex) == true;
                    if (!loaded)
                    {
                        Ui.Toast.Instance?.Error($"Failed to load slot {slotIndex + 1}");
                        return;
                    }
                    GetTree().Paused = false;
                    LoadTown();
                },
                Strings.Town.Arriving);
        }));

        GetNode<CanvasLayer>("UILayer").AddChild(screen);
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
