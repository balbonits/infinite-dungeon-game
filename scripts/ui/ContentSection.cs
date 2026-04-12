using Godot;

namespace DungeonGame.Ui;

/// <summary>
/// Helpers for building content sections inside scrollable lists.
/// Dividers go AFTER sections (bottom), not before — prevents double
/// dividers when the parent already has a top divider.
/// </summary>
public static class ContentSection
{
    /// <summary>Add a section heading. No divider above — divider below previous section handles separation.</summary>
    public static void AddHeading(Control parent, string title)
    {
        var lbl = new Label();
        lbl.Text = title;
        UiTheme.StyleLabel(lbl, UiTheme.Colors.Accent, UiTheme.FontSizes.Body);
        parent.AddChild(lbl);
    }

    /// <summary>Add a divider (call at END of a section, not beginning).</summary>
    public static void AddDivider(Control parent)
    {
        parent.AddChild(new HSeparator());
    }

    /// <summary>Add a key-value binding row (e.g., "S" → "Confirm").</summary>
    public static void AddBinding(Control parent, string key, string value, float keyWidth = 120f)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);

        var keyLabel = new Label();
        keyLabel.Text = key;
        keyLabel.CustomMinimumSize = new Vector2(keyWidth, 0);
        UiTheme.StyleLabel(keyLabel, UiTheme.Colors.Info, UiTheme.FontSizes.Body);
        row.AddChild(keyLabel);

        var valLabel = new Label();
        valLabel.Text = value;
        UiTheme.StyleLabel(valLabel, UiTheme.Colors.Ink, UiTheme.FontSizes.Body);
        valLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        valLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        row.AddChild(valLabel);

        parent.AddChild(row);
    }

    /// <summary>Add a muted note/description line.</summary>
    public static void AddNote(Control parent, string text)
    {
        var lbl = new Label();
        lbl.Text = $"  {text}";
        UiTheme.StyleLabel(lbl, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        lbl.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        parent.AddChild(lbl);
    }

    /// <summary>Add a read-only info row (muted key, white value).</summary>
    public static void AddInfoRow(Control parent, string label, string value)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);

        var lbl = new Label();
        lbl.Text = label;
        lbl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        UiTheme.StyleLabel(lbl, UiTheme.Colors.Muted, UiTheme.FontSizes.Small);
        row.AddChild(lbl);

        var val = new Label();
        val.Text = value;
        UiTheme.StyleLabel(val, UiTheme.Colors.Ink, UiTheme.FontSizes.Small);
        row.AddChild(val);

        parent.AddChild(row);
    }
}
