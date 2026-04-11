using Godot;

namespace DungeonGame.Ui;

/// <summary>
/// Reusable sprite flash effects for status feedback.
/// Works on any CanvasItem (Sprite2D, Polygon2D, etc).
/// Call static methods — each one creates a self-cleaning tween.
/// </summary>
public static class FlashFx
{
    // Status effect flash colors
    public static readonly Color Damage = new(1.0f, 0.3f, 0.3f);          // Red
    public static readonly Color Poison = new(0.4f, 0.9f, 0.3f);          // Green
    public static readonly Color Curse = new(0.7f, 0.3f, 0.9f);           // Purple
    public static readonly Color Boost = new(1.0f, 0.9f, 0.3f);           // Yellow
    public static readonly Color Shield = new(1.0f, 1.0f, 1.0f);          // White
    public static readonly Color Freeze = new(0.4f, 0.7f, 1.0f);          // Ice blue
    public static readonly Color Heal = new(0.3f, 1.0f, 0.6f);            // Mint green
    public static readonly Color Crazed = new(1.0f, 0.4f, 0.0f);          // Orange

    /// <summary>Single flash: snap to color, fade back to white.</summary>
    public static void Flash(Node owner, CanvasItem target, Color color, float duration = 0.15f)
    {
        target.Modulate = color;
        var tween = owner.CreateTween();
        tween.TweenProperty(target, "modulate", Colors.White, duration);
    }

    /// <summary>Double pulse: flash twice quickly (good for buffs, crits).</summary>
    public static void DoublePulse(Node owner, CanvasItem target, Color color, float speed = 0.08f)
    {
        var tween = owner.CreateTween();
        tween.TweenProperty(target, "modulate", color, speed);
        tween.TweenProperty(target, "modulate", Colors.White, speed);
        tween.TweenProperty(target, "modulate", color, speed);
        tween.TweenProperty(target, "modulate", Colors.White, speed);
    }

    /// <summary>Alternating flash: two colors pulse back and forth (boost + invincibility).</summary>
    public static void AlternateFlash(Node owner, CanvasItem target, Color colorA, Color colorB, int pulses = 3, float speed = 0.06f)
    {
        var tween = owner.CreateTween();
        for (int i = 0; i < pulses; i++)
        {
            tween.TweenProperty(target, "modulate", colorA, speed);
            tween.TweenProperty(target, "modulate", colorB, speed);
        }
        tween.TweenProperty(target, "modulate", Colors.White, speed);
    }

    /// <summary>Lingering flash: snap to color, hold briefly, then fade out (poison tick, burn).</summary>
    public static void Linger(Node owner, CanvasItem target, Color color, float holdTime = 0.2f, float fadeTime = 0.3f)
    {
        target.Modulate = color;
        var tween = owner.CreateTween();
        tween.TweenInterval(holdTime);
        tween.TweenProperty(target, "modulate", Colors.White, fadeTime);
    }
}
