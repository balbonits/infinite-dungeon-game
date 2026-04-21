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

        // WindowStack is static; if a prior scene-run (esp. in test mode, where
        // ResetToFreshSplash calls ReloadCurrentScene) left a modal open, the
        // stack would still report HasModal=true and block splash input after
        // reload. Clearing here guarantees every Main boot starts with no
        // modal trapping focus. Copilot PR #33 round-7 finding.
        Ui.WindowStack.Clear();

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

#if DEBUG
        // Sandbox MUST be engaged BEFORE ShowSplashScreen is deferred: SplashScreen._Ready
        // reads SaveManager.SaveDir to set Continue-button state, and any test fixture
        // that writes a save does so against whatever SaveDir was when the splash booted.
        // If we moved this into RunTests (also deferred), ShowSplashScreen runs first and
        // sees the production dir — Copilot PR #33 round-3 finding.
        var testEnv = TestEnvironment.From(OS.GetCmdlineArgs());
        if (testEnv.ShouldRunTests)
        {
            Autoloads.SaveManager.UseTestSandbox();
            // Attach the top-of-screen banner that shows current test name.
            Testing.TestProgressOverlay.EnsureAttached(this);
        }
#endif

        // Start with splash screen
        CallDeferred(MethodName.ShowSplashScreen);

#if DEBUG
        // Only spin up RunTests once per process. ResetToFreshSplash (used by
        // test [Setup]s for cross-suite isolation) calls ReloadCurrentScene,
        // which re-enters Main._Ready — without this guard we'd spawn a new
        // GoTest.RunTests for every scene reload, leading to a recursive
        // test-restart loop that never terminates.
        if (testEnv.ShouldRunTests && GetTree().Root.GetNodeOrNull("TestRoot") == null)
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
            GD.Print("[Main] NewGamePressed");
            // Spec: if all 3 save slots are full, block New Game and nudge to Load Game
            // for deletion. See docs/flows/load-game.md § Interaction with New Game.
            // UX: modal dialog with "Open Load Game / Cancel" — toast alone was
            // too easy to miss and looked like a silently broken button.
            var sm = Autoloads.SaveManager.Instance;
            if (sm != null && sm.AreAllSlotsFull())
            {
                GD.Print("[Main] NewGamePressed blocked: all 3 save slots are full.");
                var dialog = Ui.SlotsFullDialog.Create(() => ShowLoadGameScreen(splash));
                // Parent under splash so the dialog is freed whenever splash
                // is freed (New Game → ClassSelection transition, or user quit).
                // Previously parented under UILayer → every blocked-click
                // leaked a hidden SlotsFullDialog (Copilot PR #33 finding).
                // SlotsFullDialog also self-QueueFrees in its button handlers
                // so back-to-back blocked clicks don't stack.
                splash.AddChild(dialog);
                dialog.Open();
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
        // Godot's Theme inheritance does not cross CanvasLayer → Control
        // boundaries, so Control nodes added dynamically under UILayer need
        // an explicit Theme assignment or they fall back to the engine
        // default font (breaks the PS2P visual contract per SPEC-UI-FONT-01).
        _classSelect.Theme = Ui.GlobalTheme.Create();
        GetNode<CanvasLayer>("UILayer").AddChild(_classSelect);
        GetTree().Paused = true;
    }

    /// <summary>
    /// Show the Load Game screen as an overlay on top of the splash. On Back, the
    /// Load Game screen frees itself and the splash is re-shown. On Load, a
    /// ScreenTransition covers everything while the save is restored and town loads.
    /// On SlotDeleted, the current screen is freed and a fresh one is created so
    /// the UI reflects the new slot state without an in-place rebuild race.
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
            // Keyboard focus was on a LoadGameScreen control that's now
            // queue-freed; without this, Enter/S on splash fires nothing and
            // New Game appears broken.
            splash.FocusFirstButton();
        }));

        screen.Connect(Ui.LoadGameScreen.SignalName.SlotDeleted, Callable.From(() =>
        {
            // Tear down this screen and open a fresh one. The in-place rebuild
            // used to race with a subsequent Back press: freed children +
            // CallDeferred(_Ready) could execute after the screen was already
            // queue-freed by the Back handler, orphaning splash focus and
            // leaving New Game unreachable (symptom: silent no-op on click).
            screen.QueueFree();
            // Defer so the current signal dispatch unwinds cleanly before we
            // re-enter ShowLoadGameScreen (which would otherwise add the new
            // screen while the old one is still technically alive).
            CallDeferred(MethodName.ShowLoadGameScreen, splash);
        }));

        screen.Connect(Ui.LoadGameScreen.SignalName.LoadSelected, Callable.From((int slotIndex) =>
        {
            // Cover with a fade-to-black, then swap GameState under the overlay and
            // transition into town. Prevents any flash of splash or stale state.
            // On load failure, preserve the screen + splash so the fade-out reveals
            // a recoverable UI (toast + still-live Load Game screen), instead of
            // stranding the user behind a black overlay with nothing to interact with.
            Ui.ScreenTransition.Instance.Play(
                Strings.Town.Title,
                () =>
                {
                    var loaded = Autoloads.SaveManager.Instance?.LoadSlot(slotIndex) == true;
                    if (!loaded)
                    {
                        Ui.Toast.Instance?.Error($"Failed to load slot {slotIndex + 1}");
                        return;
                    }
                    screen.QueueFree();
                    splash.QueueFree();
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
        // ?? false — missing SaveManager autoload also counts as failure (Copilot
        // R2 finding on PR #16); we never want to silently bypass the toast.
        bool ok = Autoloads.SaveManager.Instance?.Save() ?? false;
        if (!ok)
            Ui.Toast.Instance?.Error("Auto-save failed — progress may be lost");
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
        // ?? false — missing SaveManager autoload also counts as failure (Copilot
        // R2 finding on PR #16); we never want to silently bypass the toast.
        bool ok = Autoloads.SaveManager.Instance?.Save() ?? false;
        if (!ok)
            Ui.Toast.Instance?.Error("Auto-save failed — progress may be lost");
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
        // Sandbox isolation for tests: redirect save I/O to user://test_saves/
        // BEFORE any test can write a save. Without this, SaveManager.SaveToSlot
        // in test fixtures would overwrite the real player's save files under
        // user://saves/. This MUST be set before GoTest.RunTests spins up.
        Autoloads.SaveManager.UseTestSandbox();

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
