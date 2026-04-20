using Godot;

namespace DungeonGame.Ui;

/// <summary>
/// Single global Godot Theme applied to UILayer root.
/// All UI nodes inherit these styles automatically — no per-node overrides needed.
/// Uses Godot's theme type variations for button types (Primary, Secondary, Danger).
/// </summary>
public static class GlobalTheme
{
    public static Theme Create()
    {
        var theme = new Theme();

        // SPEC-UI-FONT-01: Press Start 2P as the default font at every text surface.
        // Set the Theme's default_font so every control that doesn't override it
        // picks up PS2P automatically. Per-control font_size overrides are still
        // honored below.
        theme.DefaultFont = UiTheme.FontFamily;
        theme.DefaultFontSize = UiTheme.FontSizes.Body;

        // --- Default Label ---
        theme.SetColor("font_color", "Label", UiTheme.Colors.Ink);
        theme.SetFontSize("font_size", "Label", UiTheme.FontSizes.Body);

        // --- Default Button (Primary / Confirm / Action) — blue for interactive elements ---
        theme.SetColor("font_color", "Button", UiTheme.Colors.Ink);
        theme.SetColor("font_hover_color", "Button", UiTheme.Colors.Ink);
        theme.SetColor("font_focus_color", "Button", UiTheme.Colors.Ink);
        theme.SetColor("font_pressed_color", "Button", UiTheme.Colors.Ink);
        theme.SetColor("font_disabled_color", "Button", new Color(UiTheme.Colors.Muted, 0.4f));
        theme.SetFontSize("font_size", "Button", UiTheme.FontSizes.Button);
        theme.SetStylebox("normal", "Button", CreateButtonStylebox(UiTheme.Colors.Action, false));
        theme.SetStylebox("hover", "Button", CreateButtonStylebox(UiTheme.Colors.ActionHover, false));
        theme.SetStylebox("pressed", "Button", CreateButtonStylebox(UiTheme.Colors.Action, true));
        theme.SetStylebox("focus", "Button", CreateFocusStylebox(UiTheme.Colors.ActionHover));
        theme.SetStylebox("disabled", "Button", CreateButtonStylebox(new Color(UiTheme.Colors.Muted, 0.3f), false));

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

    private static StyleBoxFlat CreateButtonStylebox(Color bgColor, bool pressed)
    {
        var style = new StyleBoxFlat();
        style.BgColor = pressed ? new Color(bgColor, 0.8f) : bgColor;
        style.SetCornerRadiusAll(6);
        style.ContentMarginLeft = 16;
        style.ContentMarginRight = 16;
        style.ContentMarginTop = 8;
        style.ContentMarginBottom = 8;
        return style;
    }

    private static StyleBoxFlat CreateFocusStylebox(Color bgColor)
    {
        var style = new StyleBoxFlat();
        style.BgColor = bgColor;
        style.BorderColor = UiTheme.Colors.Accent; // Gold border on focus — high contrast against blue
        style.SetBorderWidthAll(3);
        style.SetCornerRadiusAll(6);
        style.ContentMarginLeft = 16;
        style.ContentMarginRight = 16;
        style.ContentMarginTop = 8;
        style.ContentMarginBottom = 8;
        return style;
    }
}
