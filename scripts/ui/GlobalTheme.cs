using Godot;

namespace DungeonGame.Ui;

/// <summary>
/// Creates and applies a global Godot Theme resource.
/// Applied to the UILayer root so all UI nodes inherit consistent styling.
/// Replaces per-node AddThemeOverride calls with inherited theme values.
/// </summary>
public static class GlobalTheme
{
    public static Theme Create()
    {
        var theme = new Theme();

        // --- Label defaults ---
        theme.SetColor("font_color", "Label", UiTheme.Colors.Ink);
        theme.SetFontSize("font_size", "Label", UiTheme.FontSizes.Body);

        // --- Button ---
        theme.SetColor("font_color", "Button", UiTheme.Colors.BgDark);
        theme.SetFontSize("font_size", "Button", UiTheme.FontSizes.Button);
        theme.SetStylebox("normal", "Button", UiTheme.CreateButtonStyle(false));
        theme.SetStylebox("hover", "Button", UiTheme.CreateButtonStyle(true));
        theme.SetStylebox("pressed", "Button", UiTheme.CreateButtonStyle(false));

        // --- PanelContainer ---
        theme.SetStylebox("panel", "PanelContainer", UiTheme.CreatePanelStyle());

        // --- HSeparator ---
        var sepStyle = new StyleBoxLine();
        sepStyle.Color = UiTheme.Colors.PanelBorder;
        sepStyle.Thickness = 1;
        theme.SetStylebox("separator", "HSeparator", sepStyle);
        theme.SetConstant("separation", "HSeparator", 8);

        return theme;
    }
}
