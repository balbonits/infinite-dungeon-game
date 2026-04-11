using Godot;

namespace DungeonGame.Ui;

/// <summary>
/// Spawns floating combat text (damage numbers, heals, XP) that rises and fades.
/// Standard ARPG style — number appears at position, drifts up, fades out.
/// Call FloatingText.Spawn() from anywhere. Respects GameSettings.ShowCombatNumbers.
/// </summary>
public static class FloatingText
{
    public static void Damage(Node parent, Vector2 position, int amount)
    {
        Spawn(parent, position, Strings.Combat.Damage(amount), UiTheme.Colors.Danger);
    }

    public static void Heal(Node parent, Vector2 position, int amount)
    {
        Spawn(parent, position, Strings.Combat.Heal(amount), FlashFx.Heal);
    }

    public static void Xp(Node parent, Vector2 position, int amount)
    {
        Spawn(parent, position, Strings.Combat.Xp(amount), UiTheme.Colors.Accent);
    }

    public static void Mana(Node parent, Vector2 position, int amount, bool restore)
    {
        Spawn(parent, position, Strings.Combat.Mana(amount, restore), FlashFx.Freeze);
    }

    public static void LevelUp(Node parent, Vector2 position)
    {
        Spawn(parent, position, Strings.Combat.LevelUp, UiTheme.Colors.Accent, 18, 1.2f);
    }

    public static void Spawn(Node parent, Vector2 position, string text, Color color,
        int fontSize = 13, float duration = 0.8f)
    {
        if (!GameSettings.ShowCombatNumbers)
            return;

        var label = new Label();
        label.Text = text;
        label.GlobalPosition = position + new Vector2(0, -40);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 3);
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.HorizontalAlignment = HorizontalAlignment.Center;

        // Small random horizontal offset so stacked numbers don't overlap
        label.GlobalPosition += new Vector2((float)GD.RandRange(-12, 12), 0);

        parent.AddChild(label);

        var tween = label.CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(label, "position:y", label.Position.Y - 30, duration);
        tween.TweenProperty(label, "modulate:a", 0.0f, duration).SetDelay(duration * 0.4f);
        tween.SetParallel(false);
        tween.TweenCallback(Callable.From(label.QueueFree));
    }
}
