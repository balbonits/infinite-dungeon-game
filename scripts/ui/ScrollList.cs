using Godot;

namespace DungeonGame.Ui;

/// <summary>
/// Reusable scrollable list with keyboard navigation.
/// Wraps ScrollContainer + VBoxContainer. Handles nav and auto-scroll.
/// Horizontal scroll disabled by default.
/// </summary>
public partial class ScrollList : ScrollContainer
{
    private VBoxContainer _list = null!;

    public VBoxContainer List => _list;

    public static ScrollList Create(float minHeight = 320f, int spacing = 4)
    {
        var scroll = new ScrollList();
        scroll.CustomMinimumSize = new Vector2(0, minHeight);
        scroll.HorizontalScrollMode = ScrollMode.Disabled;

        scroll._list = new VBoxContainer();
        scroll._list.AddThemeConstantOverride("separation", spacing);
        scroll._list.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(scroll._list);

        return scroll;
    }

    /// <summary>Clear all children from the list.</summary>
    public void Clear()
    {
        foreach (Node child in _list.GetChildren())
            child.QueueFree();
    }

    /// <summary>Add a node to the list.</summary>
    public void Add(Node item) => _list.AddChild(item);

    /// <summary>Handle keyboard nav within the list. Returns true if handled.</summary>
    public bool HandleInput(InputEvent @event) =>
        KeyboardNav.HandleInput(@event, _list);

    /// <summary>Handle arrow key scrolling for read-only lists (no buttons). Returns true if handled.</summary>
    public bool HandleScrollInput(InputEvent @event)
    {
        if (@event.IsActionPressed(Constants.InputActions.MoveUp))
        {
            ScrollVertical -= 40;
            return true;
        }
        if (@event.IsActionPressed(Constants.InputActions.MoveDown))
        {
            ScrollVertical += 40;
            return true;
        }
        return false;
    }

    /// <summary>Focus the first button in the list.</summary>
    public void FocusFirst()
    {
        UiTheme.FocusFirstButton(_list);
    }

    /// <summary>Reset scroll to top.</summary>
    public void ScrollToTop() => ScrollVertical = 0;
}
