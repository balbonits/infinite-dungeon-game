using System.Threading.Tasks;
using Godot;
using Chickensoft.GodotTestDriver.Input;

namespace DungeonGame.Testing;

/// <summary>
/// Keyboard input simulation for tests. Wraps GodotTestDriver extensions.
/// All UI navigation goes through this — never call button.EmitSignal directly.
///
/// Godot's built-in focus system handles arrow keys (ui_up/ui_down) and Enter (ui_accept)
/// at the _gui_input level, so this class only needs to inject raw key events and waits.
/// </summary>
public class InputHelper
{
    private readonly Node _node;
    private readonly SceneTree _tree;

    public InputHelper(Node node)
    {
        _node = node;
        _tree = node.GetTree();
    }

    /// <summary>Wait N physics frames.</summary>
    public async Task WaitFrames(int count)
    {
        for (int i = 0; i < count; i++)
            await _node.ToSignal(_tree, SceneTree.SignalName.PhysicsFrame);
    }

    /// <summary>Wait N seconds (real-time).</summary>
    public async Task WaitSeconds(float seconds)
    {
        await _node.ToSignal(_tree.CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    }

    /// <summary>Press + release a specific keyboard key.</summary>
    public async Task PressKey(Key key)
    {
        var down = new InputEventKey { Keycode = key, Pressed = true };
        Input.ParseInputEvent(down);
        await WaitFrames(2);
        var up = new InputEventKey { Keycode = key, Pressed = false };
        Input.ParseInputEvent(up);
        await WaitFrames(2);
    }

    /// <summary>Press + release an input action (defined in project.godot).</summary>
    public async Task PressAction(string action)
    {
        _node.StartAction(action);
        await WaitFrames(2);
        _node.EndAction(action);
        await WaitFrames(2);
    }

    /// <summary>Hold an input action for N seconds, then release.</summary>
    public async Task HoldAction(string action, float seconds)
    {
        _node.StartAction(action);
        await WaitSeconds(seconds);
        _node.EndAction(action);
    }

    // ── Semantic shortcuts ───────────────────────────────────────────────────

    /// <summary>Arrow down N times (move focus down via Godot's ui_down).</summary>
    public async Task NavDown(int count = 1)
    {
        for (int i = 0; i < count; i++)
            await PressKey(Key.Down);
    }

    /// <summary>Arrow up N times.</summary>
    public async Task NavUp(int count = 1)
    {
        for (int i = 0; i < count; i++)
            await PressKey(Key.Up);
    }

    /// <summary>Arrow right N times.</summary>
    public async Task NavRight(int count = 1)
    {
        for (int i = 0; i < count; i++)
            await PressKey(Key.Right);
    }

    /// <summary>Arrow left N times.</summary>
    public async Task NavLeft(int count = 1)
    {
        for (int i = 0; i < count; i++)
            await PressKey(Key.Left);
    }

    /// <summary>Press Enter (triggers Godot's ui_accept on focused button).</summary>
    public async Task PressEnter() => await PressKey(Key.Enter);

    /// <summary>Press Escape (triggers pause menu toggle / window close).</summary>
    public async Task PressEscape() => await PressKey(Key.Escape);

    /// <summary>Press S key (action_cross — confirm on focused button).</summary>
    public async Task Confirm() => await PressAction(Constants.InputActions.ActionCross);

    /// <summary>Press D key (action_circle — cancel/close).</summary>
    public async Task Cancel() => await PressAction(Constants.InputActions.ActionCircle);

    /// <summary>Press Q key (shoulder_left — previous tab).</summary>
    public async Task TabLeft() => await PressAction(Constants.InputActions.ShoulderLeft);

    /// <summary>Press E key (shoulder_right — next tab).</summary>
    public async Task TabRight() => await PressAction(Constants.InputActions.ShoulderRight);

    // ── Movement ─────────────────────────────────────────────────────────────

    /// <summary>Hold directional keys for N seconds, then release.</summary>
    public async Task Move(Vector2 dir, float seconds)
    {
        if (dir.X > 0.1f) _node.StartAction(Constants.InputActions.MoveRight);
        else if (dir.X < -0.1f) _node.StartAction(Constants.InputActions.MoveLeft);
        if (dir.Y > 0.1f) _node.StartAction(Constants.InputActions.MoveDown);
        else if (dir.Y < -0.1f) _node.StartAction(Constants.InputActions.MoveUp);

        await WaitSeconds(seconds);

        _node.EndAction(Constants.InputActions.MoveRight);
        _node.EndAction(Constants.InputActions.MoveLeft);
        _node.EndAction(Constants.InputActions.MoveDown);
        _node.EndAction(Constants.InputActions.MoveUp);
    }
}
