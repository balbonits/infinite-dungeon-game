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

    /// <summary>
    /// Press Start 2P (OFL 1.1, commercial OK) per SPEC-UI-FONT-01. Uppercase-
    /// only bitmap font with an 8px native cell; every size on <see cref="FontSizes"/>
    /// below is an integer multiple of 8 so glyphs snap to the pixel grid without
    /// anti-aliasing blur.
    ///
    /// Lazy-loaded from disk on first read. Property accesses happen only at
    /// runtime inside Godot scenes (GlobalTheme.Create, StyleButton/StyleLabel
    /// factories). The unit-test project compiles against GodotSharp but the
    /// UI namespace is excluded from the test assembly's compile set, so these
    /// readers never fire under xUnit — the load path is purely a Godot-
    /// runtime concern.
    /// </summary>
    // Return type is Font (the base class) — the happy path returns a
    // FontFile with pixel-font settings applied; the error path returns
    // ThemeDB.FallbackFont, which is the engine's built-in Font (not
    // necessarily FontFile). Callers only need Font (Theme.DefaultFont and
    // AddThemeFontSizeOverride bind against Font).
    private static Font? _fontFamily;
    public static Font FontFamily
    {
        get
        {
            if (_fontFamily != null) return _fontFamily;
            var loaded = GD.Load<FontFile>("res://assets/fonts/PressStart2P-Regular.ttf");
            if (loaded == null)
            {
                // Operator alert: the font is a required shipping asset per
                // SPEC-UI-FONT-01. Fall back to Godot's built-in engine font
                // (ThemeDB.FallbackFont) so UI stays readable while the
                // error surfaces. Previously cast the fallback to FontFile,
                // which silently null'd out + produced tofu from an empty
                // FontFile — worse than the engine default. Return Font so
                // any engine font class is accepted.
                GD.PrintErr("[UiTheme] PressStart2P-Regular.ttf failed to load. Re-import the font resource; UI renders with Godot's built-in fallback font until fixed.");
                _fontFamily = ThemeDB.FallbackFont ?? new FontFile();
                return _fontFamily;
            }
            // Pixel-font rendering discipline per SPEC-UI-FONT-01:
            // - Antialiasing=None keeps the 8-px native cell crisp.
            // - Hinting=None disables hinting adjustments (PS2P is already
            //   bitmap-pre-aligned; hinting would distort the grid).
            // - SubpixelPositioning=Disabled locks glyphs to whole-pixel
            //   positions — any sub-pixel offset smears the pixel font.
            // - ForceAutohinter=false for completeness (belt-and-suspenders
            //   when shipping on platforms whose freetype defaults changed).
            // These properties live on the FontFile resource and don't rely
            // on the .import sidecar (which isn't version-controlled here).
            // Godot's TextureFilter for the Theme is set to Nearest at the
            // project level (SPEC-UI-HIGH-DPI-01 — sibling spec).
            loaded.Antialiasing = TextServer.FontAntialiasing.None;
            loaded.Hinting = TextServer.Hinting.None;
            loaded.SubpixelPositioning = TextServer.SubpixelPositioning.Disabled;
            loaded.ForceAutohinter = false;
            _fontFamily = loaded;
            return _fontFamily;
        }
    }

    /// <summary>
    /// Font-size ladder per SPEC-UI-FONT-01. All sizes are integer multiples
    /// of 8 so Press Start 2P's 8px native cell renders without sub-pixel
    /// drift. Previous sizes (11/12/13/20) were legacy values that only made
    /// sense for the proportional default font and are replaced here.
    /// </summary>
    public static class FontSizes
    {
        public const int Small = 8;
        public const int Body = 16;
        public const int Label = 16;
        public const int Button = 16;
        public const int Heading = 24;
        public const int Title = 32;
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
        button.AddThemeStyleboxOverride("focus", CreateButtonFocusStyle(new Color(Colors.Danger, 0.9f)));
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
        button.AddThemeStyleboxOverride("focus", CreateButtonFocusStyle(new Color(Colors.Muted, 0.9f)));
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

    private static StyleBoxFlat? _activeTabStyle;
    private static StyleBoxFlat? _inactiveTabStyle;

    /// <summary>Tab button style (active = bright accent, inactive = muted). Cached — shared across all tabs.</summary>
    public static StyleBoxFlat CreateTabStyle(bool active)
    {
        if (active) return _activeTabStyle ??= BuildTabStyle(true);
        return _inactiveTabStyle ??= BuildTabStyle(false);
    }

    private static StyleBoxFlat BuildTabStyle(bool active)
    {
        var style = new StyleBoxFlat();
        if (active)
        {
            style.BgColor = Colors.Action;
            style.BorderColor = Colors.Action;
        }
        else
        {
            style.BgColor = new Color(Colors.BgPanel, 0.6f);
            style.BorderColor = new Color(Colors.Muted, 0.3f);
        }
        style.SetBorderWidthAll(1);
        style.BorderWidthBottom = active ? 3 : 1;
        style.SetCornerRadiusAll(0);
        style.CornerRadiusTopLeft = 4;
        style.CornerRadiusTopRight = 4;
        style.ContentMarginLeft = 8;
        style.ContentMarginRight = 8;
        style.ContentMarginTop = 4;
        style.ContentMarginBottom = 4;
        return style;
    }

    /// <summary>
    /// Creates a StyleBoxFlat for focused buttons — gold border on the supplied
    /// bg color. The canonical focus indicator is the gold border itself; the
    /// base bg stays whatever color the button variant uses so Secondary (grey)
    /// and Danger (red) buttons don't flip to blue on focus.
    /// </summary>
    public static StyleBoxFlat CreateButtonFocusStyle(Color? bgColor = null)
    {
        var style = new StyleBoxFlat();
        style.BgColor = bgColor ?? Colors.ActionHover;
        style.BorderColor = Colors.Accent;
        style.SetBorderWidthAll(3);
        style.SetCornerRadiusAll(6);
        style.ContentMarginLeft = 16;
        style.ContentMarginRight = 16;
        style.ContentMarginTop = 8;
        style.ContentMarginBottom = 8;
        return style;
    }

    /// <summary>
    /// Grabs focus on the first focusable button in a container (recursive).
    /// Returns true if a focusable button was found. Call after adding buttons.
    /// </summary>
    public static bool FocusFirstButton(Control container)
    {
        var btn = FindFirstButton(container);
        if (btn != null)
        {
            btn.CallDeferred(Control.MethodName.GrabFocus);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Tries to focus the first button in `primary`. If nothing focusable is found there
    /// (e.g., empty scroll content), falls back to `fallback` so keyboard users can always
    /// reach some button (like Cancel/Close) via the window's spatial focus navigation.
    /// </summary>
    public static void FocusFirstButtonOrFallback(Control primary, Control fallback)
    {
        if (!FocusFirstButton(primary))
            FocusFirstButton(fallback);
    }

    /// <summary>
    /// Styles a Button as a list row item (used in shop/blacksmith lists, inventory slots):
    /// transparent background, subtle accent highlight on hover, brighter accent on focus,
    /// white text in ALL three states (normal, hover, focus) so the row stays legible
    /// when the focus cursor lands on it. Caller sets Text, size, and signals separately.
    /// </summary>
    public static void StyleListItemButton(Button btn)
    {
        btn.Alignment = Godot.HorizontalAlignment.Left;
        btn.FocusMode = Control.FocusModeEnum.All;

        var normal = new StyleBoxFlat();
        normal.BgColor = new Color(0, 0, 0, 0.01f);
        normal.SetCornerRadiusAll(4);
        normal.ContentMarginLeft = 8;
        btn.AddThemeStyleboxOverride("normal", normal);

        var hover = new StyleBoxFlat();
        hover.BgColor = new Color(Colors.Accent, 0.15f);
        hover.SetCornerRadiusAll(4);
        hover.ContentMarginLeft = 8;
        btn.AddThemeStyleboxOverride("hover", hover);

        var focus = new StyleBoxFlat();
        focus.BgColor = new Color(Colors.Accent, 0.25f);
        focus.SetCornerRadiusAll(4);
        focus.ContentMarginLeft = 8;
        btn.AddThemeStyleboxOverride("focus", focus);

        // Override ALL three font color states so the white text stays readable on the
        // accent-tinted highlight. Without this, the GameWindow Theme's default
        // font_focus_color / font_hover_color (both BgDark) bleed through and make text
        // invisible when the row is focused.
        btn.AddThemeColorOverride("font_color", Colors.Ink);
        btn.AddThemeColorOverride("font_hover_color", Colors.Ink);
        btn.AddThemeColorOverride("font_focus_color", Colors.Ink);
        btn.AddThemeColorOverride("font_pressed_color", Colors.Ink);
        btn.AddThemeFontSizeOverride("font_size", FontSizes.Body);
    }

    private static Button? FindFirstButton(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is Button btn && !btn.Disabled && btn.Visible && btn.FocusMode != Control.FocusModeEnum.None)
                return btn;
            if (child is Control ctrl)
            {
                var found = FindFirstButton(ctrl);
                if (found != null) return found;
            }
        }
        return null;
    }

    private static Theme? _cachedGameTheme;

    /// <summary>
    /// Returns the shared Godot Theme resource with all standard UI styles.
    /// Apply to a root Control (e.g., GameWindow overlay) so all children inherit
    /// consistent button/label/panel styling without per-node overrides.
    /// </summary>
    public static Theme CreateGameTheme() => _cachedGameTheme ??= BuildGameTheme();

    private static Theme BuildGameTheme()
    {
        var theme = new Theme();

        // SPEC-UI-FONT-01: every Theme in the codebase must register PS2P as
        // DefaultFont. GameWindow.Overlay assigns this theme on its branch,
        // which *overrides* the SceneTree-root GlobalTheme for everything
        // under the overlay — so if we omit DefaultFont here, HUD + in-game
        // popups fall back to Godot's built-in proportional font and break
        // the visual contract.
        theme.DefaultFont = FontFamily;
        theme.DefaultFontSize = FontSizes.Body;

        // --- Button (primary/action — blue bg, dark text) ---
        theme.SetStylebox("normal", "Button", CreateButtonStyle(false));
        theme.SetStylebox("hover", "Button", CreateButtonStyle(true));
        theme.SetStylebox("focus", "Button", CreateButtonFocusStyle());
        theme.SetStylebox("pressed", "Button", CreateButtonStyle(true));
        theme.SetStylebox("disabled", "Button", CreateColoredButtonStyle(Colors.Muted, false));
        theme.SetColor("font_color", "Button", Colors.BgDark);
        theme.SetColor("font_hover_color", "Button", Colors.BgDark);
        theme.SetColor("font_focus_color", "Button", Colors.BgDark);
        theme.SetColor("font_pressed_color", "Button", Colors.BgDark);
        theme.SetColor("font_disabled_color", "Button", new Color(Colors.Muted, 0.4f));
        theme.SetFontSize("font_size", "Button", FontSizes.Button);

        // --- Label ---
        theme.SetColor("font_color", "Label", Colors.Ink);
        theme.SetFontSize("font_size", "Label", FontSizes.Body);

        // --- PanelContainer ---
        theme.SetStylebox("panel", "PanelContainer", CreatePanelStyle(0.95f, true));

        // --- HSeparator ---
        var sepStyle = new StyleBoxLine();
        sepStyle.Color = Colors.PanelBorder;
        sepStyle.Thickness = 1;
        theme.SetStylebox("separator", "HSeparator", sepStyle);
        theme.SetConstant("separation", "HSeparator", 8);

        // --- ScrollContainer (hide scrollbar visual noise) ---
        var emptyStylebox = new StyleBoxEmpty();
        theme.SetStylebox("scroll", "ScrollContainer", emptyStylebox);

        return theme;
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

    /// <summary>
    /// Create a control hint bar showing available keys. Hidden if ShowControlHints is off.
    /// Each hint is (actionNameOrRawKey, description). InputMap actions are resolved dynamically.
    /// </summary>
    public static Label CreateHintBar(params (string key, string desc)[] hints)
    {
        var label = new Label();
        if (!GameSettings.ShowControlHints)
        {
            label.Visible = false;
            return label;
        }

        var parts = new System.Collections.Generic.List<string>();
        foreach (var (key, desc) in hints)
        {
            string keyName = Godot.InputMap.HasAction(key) ? GetKeyName(key) : key;
            parts.Add($"[{keyName}] {desc}");
        }
        label.Text = string.Join("   ", parts);
        StyleLabel(label, Colors.Muted, FontSizes.Small);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        return label;
    }

    private static string GetKeyName(string action)
    {
        var events = Godot.InputMap.ActionGetEvents(action);
        foreach (var ev in events)
            if (ev is InputEventKey keyEv)
                return Godot.OS.GetKeycodeString(keyEv.Keycode);
        return "?";
    }
}
