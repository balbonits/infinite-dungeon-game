using Godot;

namespace DungeonGame.Ui;

/// <summary>
/// Shared UI palette and factory methods. Single source of truth for all UI styling.
/// Usage: UiTheme.Colors.Accent, UiTheme.CreatePanel(), UiTheme.StyleButton(button)
/// </summary>
public static class UiTheme
{
    public static class Colors
    {
        public static readonly Color BgDark = new(0.059f, 0.067f, 0.090f, 1.0f);       // #0f1117
        public static readonly Color BgPanel = new(0.086f, 0.106f, 0.157f, 1.0f);       // #161b28
        public static readonly Color Ink = new(0.925f, 0.941f, 1.0f, 1.0f);             // #ecf0ff
        public static readonly Color Muted = new(0.714f, 0.749f, 0.859f, 1.0f);         // #b6bfdb
        public static readonly Color Accent = new(0.961f, 0.784f, 0.420f, 1.0f);        // #f5c86b
        public static readonly Color Danger = new(1.0f, 0.435f, 0.435f, 1.0f);          // #ff6f6f
        public static readonly Color Safe = new(0.420f, 1.0f, 0.537f, 1.0f);            // #6bff89
        public static readonly Color Player = new(0.557f, 0.839f, 1.0f, 1.0f);          // #8ed6ff

        public static readonly Color PanelBg = new(0.086f, 0.106f, 0.157f, 0.75f);
        public static readonly Color PanelBgSolid = new(0.086f, 0.106f, 0.157f, 0.92f);
        public static readonly Color PanelBorder = new(0.961f, 0.784f, 0.420f, 0.3f);
        public static readonly Color PanelBorderBright = new(0.961f, 0.784f, 0.420f, 0.5f);
        public static readonly Color BtnHover = new(0.98f, 0.85f, 0.55f, 1.0f);
    }

    public static class FontSizes
    {
        public const int Small = 11;
        public const int Body = 12;
        public const int Label = 13;
        public const int Button = 16;
        public const int Heading = 20;
        public const int Title = 24;
        public const int HeroTitle = 48;
    }

    /// <summary>Creates a StyleBoxFlat for panels (dark bg, gold border, rounded corners).</summary>
    public static StyleBoxFlat CreatePanelStyle(float alpha = 0.75f, bool brightBorder = false)
    {
        var style = new StyleBoxFlat();
        style.BgColor = new Color(Colors.BgPanel, alpha);
        style.BorderColor = brightBorder ? Colors.PanelBorderBright : Colors.PanelBorder;
        style.SetBorderWidthAll(brightBorder ? 2 : 1);
        style.SetCornerRadiusAll(8);
        style.SetContentMarginAll(brightBorder ? 20 : 10);
        return style;
    }

    /// <summary>Creates a StyleBoxFlat for buttons (gold bg, rounded).</summary>
    public static StyleBoxFlat CreateButtonStyle(bool hover = false)
    {
        var style = new StyleBoxFlat();
        style.BgColor = hover ? Colors.BtnHover : Colors.Accent;
        style.SetCornerRadiusAll(6);
        style.ContentMarginLeft = 16;
        style.ContentMarginRight = 16;
        style.ContentMarginTop = 8;
        style.ContentMarginBottom = 8;
        return style;
    }

    /// <summary>Applies standard (primary/confirm) button styling.</summary>
    public static void StyleButton(Button button, int fontSize = FontSizes.Button)
    {
        button.AddThemeColorOverride("font_color", Colors.BgDark);
        button.AddThemeColorOverride("font_hover_color", Colors.BgDark);
        button.AddThemeColorOverride("font_focus_color", Colors.BgDark);
        button.AddThemeFontSizeOverride("font_size", fontSize);
        button.AddThemeStyleboxOverride("normal", CreateButtonStyle(false));
        button.AddThemeStyleboxOverride("hover", CreateButtonStyle(true));
        button.AddThemeStyleboxOverride("focus", CreateButtonFocusStyle());
        button.CustomMinimumSize = new Vector2(button.CustomMinimumSize.X, 40);
        button.FocusMode = Control.FocusModeEnum.All;
    }

    /// <summary>Danger/destructive action button (red tones).</summary>
    public static void StyleDangerButton(Button button, int fontSize = FontSizes.Button)
    {
        button.AddThemeColorOverride("font_color", Colors.Ink);
        button.AddThemeColorOverride("font_hover_color", Colors.Ink);
        button.AddThemeColorOverride("font_focus_color", Colors.Ink);
        button.AddThemeFontSizeOverride("font_size", fontSize);
        button.AddThemeStyleboxOverride("normal", CreateColoredButtonStyle(Colors.Danger, false));
        button.AddThemeStyleboxOverride("hover", CreateColoredButtonStyle(Colors.Danger, true));
        button.AddThemeStyleboxOverride("focus", CreateColoredButtonStyle(Colors.Danger, true));
        button.CustomMinimumSize = new Vector2(button.CustomMinimumSize.X, 40);
        button.FocusMode = Control.FocusModeEnum.All;
    }

    /// <summary>Secondary/cancel/neutral button (muted tones).</summary>
    public static void StyleSecondaryButton(Button button, int fontSize = FontSizes.Button)
    {
        button.AddThemeColorOverride("font_color", Colors.Ink);
        button.AddThemeColorOverride("font_hover_color", Colors.Ink);
        button.AddThemeColorOverride("font_focus_color", Colors.Ink);
        button.AddThemeFontSizeOverride("font_size", fontSize);
        button.AddThemeStyleboxOverride("normal", CreateColoredButtonStyle(Colors.Muted, false));
        button.AddThemeStyleboxOverride("hover", CreateColoredButtonStyle(Colors.Muted, true));
        button.AddThemeStyleboxOverride("focus", CreateColoredButtonStyle(Colors.Muted, true));
        button.CustomMinimumSize = new Vector2(button.CustomMinimumSize.X, 40);
        button.FocusMode = Control.FocusModeEnum.All;
    }

    private static StyleBoxFlat CreateColoredButtonStyle(Color baseColor, bool bright)
    {
        var style = new StyleBoxFlat();
        style.BgColor = bright ? new Color(baseColor, 0.9f) : new Color(baseColor, 0.7f);
        style.BorderColor = bright ? baseColor : new Color(baseColor, 0.5f);
        style.SetBorderWidthAll(bright ? 2 : 1);
        style.SetCornerRadiusAll(6);
        style.ContentMarginLeft = 16;
        style.ContentMarginRight = 16;
        style.ContentMarginTop = 8;
        style.ContentMarginBottom = 8;
        return style;
    }

    /// <summary>Creates a StyleBoxFlat for focused buttons — high contrast white border for visibility.</summary>
    public static StyleBoxFlat CreateButtonFocusStyle()
    {
        var style = new StyleBoxFlat();
        style.BgColor = Colors.Accent;
        style.BorderColor = Colors.Ink;
        style.SetBorderWidthAll(3);
        style.SetCornerRadiusAll(6);
        style.ContentMarginLeft = 16;
        style.ContentMarginRight = 16;
        style.ContentMarginTop = 8;
        style.ContentMarginBottom = 8;
        return style;
    }

    /// <summary>Grabs focus on the first button in a container. Call after adding buttons.</summary>
    public static void FocusFirstButton(Control container)
    {
        foreach (Node child in container.GetChildren())
        {
            if (child is Button btn && !btn.Disabled)
            {
                btn.CallDeferred(Control.MethodName.GrabFocus);
                return;
            }
        }
    }

    /// <summary>Applies standard label styling.</summary>
    public static void StyleLabel(Label label, Color color, int fontSize = FontSizes.Body)
    {
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeFontSizeOverride("font_size", fontSize);
    }
}
