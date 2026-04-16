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
        // Backgrounds
        public static readonly Color BgDark = new(0.059f, 0.067f, 0.090f, 1.0f);       // #0f1117
        public static readonly Color BgPanel = new(0.086f, 0.106f, 0.157f, 1.0f);       // #161b28

        // Text
        public static readonly Color Ink = new(0.925f, 0.941f, 1.0f, 1.0f);             // #ecf0ff (body text)
        public static readonly Color Muted = new(0.557f, 0.600f, 0.729f, 1.0f);         // #8e99ba (dim/disabled)

        // Semantic colors (distinct roles)
        public static readonly Color Accent = new(0.961f, 0.784f, 0.420f, 1.0f);        // #f5c86b (gold — titles, headings, rewards)
        public static readonly Color Action = new(0.318f, 0.557f, 0.957f, 1.0f);        // #518ef4 (blue — buttons, interactive)
        public static readonly Color ActionHover = new(0.420f, 0.655f, 1.0f, 1.0f);     // #6ba7ff (bright blue — hovered button)
        public static readonly Color Danger = new(1.0f, 0.435f, 0.435f, 1.0f);          // #ff6f6f (red — damage, warnings)
        public static readonly Color Safe = new(0.420f, 1.0f, 0.537f, 1.0f);            // #6bff89 (green — heals, success)
        public static readonly Color Info = new(0.290f, 0.910f, 0.910f, 1.0f);          // #4ae8e8 (cyan — info, mana, stats)
        public static readonly Color Player = new(0.557f, 0.839f, 1.0f, 1.0f);          // #8ed6ff (light blue — player)

        // Panel chrome
        public static readonly Color PanelBg = new(0.086f, 0.106f, 0.157f, 0.75f);
        public static readonly Color PanelBgSolid = new(0.086f, 0.106f, 0.157f, 0.92f);
        public static readonly Color PanelBorder = new(0.290f, 0.420f, 0.620f, 0.4f);    // #4a6b9e — blue-grey border
        public static readonly Color PanelBorderBright = new(0.318f, 0.557f, 0.957f, 0.6f); // blue border on focused panels
        public static readonly Color BtnHover = new(0.420f, 0.655f, 1.0f, 1.0f);         // same as ActionHover
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

    /// <summary>Creates a StyleBoxFlat for buttons (blue bg, rounded).</summary>
    public static StyleBoxFlat CreateButtonStyle(bool hover = false)
    {
        var style = new StyleBoxFlat();
        style.BgColor = hover ? Colors.ActionHover : Colors.Action;
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

    public static StyleBoxFlat CreateColoredButtonStyle(Color baseColor, bool bright)
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

    /// <summary>Creates a StyleBoxFlat for focused buttons — gold border on blue bg for high contrast.</summary>
    public static StyleBoxFlat CreateButtonFocusStyle()
    {
        var style = new StyleBoxFlat();
        style.BgColor = Colors.ActionHover;
        style.BorderColor = Colors.Accent;
        style.SetBorderWidthAll(3);
        style.SetCornerRadiusAll(6);
        style.ContentMarginLeft = 16;
        style.ContentMarginRight = 16;
        style.ContentMarginTop = 8;
        style.ContentMarginBottom = 8;
        return style;
    }

    /// <summary>Grabs focus on the first focusable button in a container (recursive). Call after adding buttons.</summary>
    public static void FocusFirstButton(Control container)
    {
        var btn = FindFirstButton(container);
        if (btn != null)
        {
            btn.CallDeferred(Control.MethodName.GrabFocus);
        }
    }

    private static Button? FindFirstButton(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is Button btn && !btn.Disabled && btn.Visible)
                return btn;
            if (child is Control ctrl)
            {
                var found = FindFirstButton(ctrl);
                if (found != null) return found;
            }
        }
        return null;
    }

    /// <summary>
    /// Standard dialog window layout: dark overlay + centered panel + content VBox.
    /// Returns (overlay, content) — add children to content. Caller adds overlay to their node.
    /// Every dialog should use this instead of manually building overlay/center/panel.
    /// </summary>
    public static (ColorRect overlay, VBoxContainer content) CreateDialogWindow(
        float width = 420f, float overlayAlpha = 0.6f)
    {
        var overlay = new ColorRect();
        overlay.Color = new Color(0, 0, 0, overlayAlpha);
        overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        overlay.MouseFilter = Control.MouseFilterEnum.Stop;

        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        overlay.AddChild(center);

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", CreatePanelStyle(0.95f, true));
        panel.CustomMinimumSize = new Vector2(width, 0);
        center.AddChild(panel);

        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 6);
        panel.AddChild(content);

        return (overlay, content);
    }

    /// <summary>Applies standard label styling.</summary>
    public static void StyleLabel(Label label, Color color, int fontSize = FontSizes.Body)
    {
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeFontSizeOverride("font_size", fontSize);
    }

    /// <summary>
    /// Unified slot box style for backpack, bank, and skill bar.
    /// Square with 5px rounded corners, consistent border.
    /// </summary>
    public static StyleBoxFlat CreateSlotStyle(Color bgColor, bool focused)
    {
        var style = new StyleBoxFlat();
        style.BgColor = bgColor;
        style.BorderColor = focused ? Colors.Accent : new Color(Colors.PanelBorder, 0.6f);
        style.SetBorderWidthAll(focused ? 2 : 1);
        style.SetCornerRadiusAll(5);
        style.SetContentMarginAll(4);
        return style;
    }

    /// <summary>Slot style for empty/disabled slots.</summary>
    public static StyleBoxFlat CreateEmptySlotStyle()
    {
        return CreateSlotStyle(new Color(0.08f, 0.08f, 0.12f, 0.6f), false);
    }
}
