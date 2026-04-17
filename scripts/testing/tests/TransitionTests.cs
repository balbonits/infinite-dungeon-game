#if DEBUG
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using DungeonGame.Autoloads;
using DungeonGame.Ui;

namespace DungeonGame.Testing.Tests;

/// <summary>
/// Tests for the ScreenTransition loading screen.
///
/// INVARIANT: whenever a transition culminates in swapping one world (Town/Dungeon)
/// for another, the overlay must be fully opaque BEFORE the new world renders.
/// Otherwise the player briefly sees the new scene "flash" behind a translucent overlay.
///
/// We verify this by hooking <see cref="SceneTree.NodeAdded"/>, detecting when a
/// Town or Dungeon node enters the tree, and sampling the overlay alpha at that moment.
/// </summary>
public class TransitionTests : GameTestBase
{
    private float _alphaWhenWorldAdded = -1f;
    private string _worldAddedKind = "";
    private bool _worldWasAdded;

    public TransitionTests(Node testScene) : base(testScene) { }

    [SetupAll]
    public void SetupAll() => GD.Print("═══ TransitionTests ═══");

    [Setup]
    public void HookWorldAddedSignal()
    {
        _alphaWhenWorldAdded = -1f;
        _worldAddedKind = "";
        _worldWasAdded = false;
        TestScene.GetTree().NodeAdded += OnNodeAdded;
    }

    [Cleanup]
    public void UnhookSignal()
    {
        TestScene.GetTree().NodeAdded -= OnNodeAdded;
    }

    private void OnNodeAdded(Node node)
    {
        // Only capture the FIRST world swap per test
        if (_worldWasAdded) return;

        string name = node.GetType().Name;
        if (name is "Town" or "Dungeon")
        {
            _worldAddedKind = name;
            _worldWasAdded = true;
            _alphaWhenWorldAdded = ScreenTransition.Instance?.OverlayAlpha ?? -1f;
        }
    }

    [Test]
    public async Task ClassSelect_ConfirmLoadsTownWithOpaqueOverlay()
    {
        // Navigate: Splash → New Game → ClassSelect
        await WaitUntil(() => Ui.HasNodeOfType<SplashScreen>(), timeout: 3f, what: "splash appears");

        var newGameBtn = Ui.FindButton("New Game");
        if (newGameBtn is null) { Expect(false, "New Game button missing"); return; }
        newGameBtn.GrabFocus();
        await Input.WaitFrames(3);
        await Input.PressEnter();

        await WaitUntil(() => Ui.HasNodeOfType<ClassSelect>(), timeout: 3f, what: "class select appears");

        // Pick Warrior → Confirm → Town
        await Input.NavRight();            // focus first card (Warrior)
        await Input.WaitSeconds(0.2f);
        await Input.NavDown();             // move to confirm zone
        await Input.WaitSeconds(0.2f);
        await Input.PressEnter();          // press confirm

        // Wait for the Town swap to happen (tween midpoint fires ~0.55s after Play starts)
        await WaitUntil(() => _worldWasAdded, timeout: 5f, what: "Town node added to tree");

        // Core invariant: overlay was opaque when Town rendered
        Expect(_worldAddedKind == "Town", $"The swapped-in world was Town (got {_worldAddedKind})");
        Expect(_alphaWhenWorldAdded >= 0.99f,
            $"Overlay alpha was {_alphaWhenWorldAdded:F3} (≥ 0.99) when Town was added to tree — no flash");
    }

    [Test]
    public async Task ScreenTransition_OverlayStartsTransparent()
    {
        // Between transitions, overlay should be fully transparent so it never blocks gameplay.
        await Input.WaitSeconds(3.0f); // let any pending transition complete
        var t = ScreenTransition.Instance;
        if (t is null) { Expect(false, "ScreenTransition.Instance missing"); return; }

        Expect(t.OverlayAlpha < 0.01f,
            $"Overlay is transparent between transitions (alpha={t.OverlayAlpha:F3})");
        Expect(!t.IsTransitioning,
            "IsTransitioning false between transitions");
    }

    [Test]
    public async Task ScreenTransition_OpaqueDuringMidpointPhase()
    {
        // Invoke a transition manually and sample the overlay mid-phase.
        // We verify that at some point during the transition, overlay reaches ≥ 0.99.
        var t = ScreenTransition.Instance;
        if (t is null) { Expect(false, "ScreenTransition.Instance missing"); return; }
        if (t.IsTransitioning) { Expect(false, "transition already running"); return; }

        float peakAlpha = 0f;
        bool midpointRan = false;
        t.Play("Test", () =>
        {
            // Midpoint callback — should be called when overlay is fully opaque
            midpointRan = true;
            peakAlpha = t.OverlayAlpha;
        }, "Test sub");

        // Wait for midpoint to fire
        await WaitUntil(() => midpointRan, timeout: 2f, what: "midpoint callback fires");

        Expect(peakAlpha >= 0.99f,
            $"Overlay alpha ≥ 0.99 at midpoint (actual={peakAlpha:F3})");

        // Wait for transition to finish so test cleanup is clean
        await WaitUntil(() => !t.IsTransitioning, timeout: 3f, what: "transition completes");
    }

    [CleanupAll]
    public void CleanupAll() => PrintSummary("TransitionTests");
}
#endif
