using Godot;

/// <summary>
/// Diablo-style HP/MP globe indicators.
/// Red orb (left) for HP, blue orb (right) for MP.
/// Fill level drains/rises based on current values.
/// </summary>
public partial class HpMpOrbs : Control
{
    private const float OrbRadius = 50f;
    private const float OrbMargin = 85f;
    private const float OrbBottomOffset = 85f;
    private const int ArcSegments = 48;

    private int _hp, _maxHp, _mp, _maxMp;
    private float _hpPercent = 1.0f;
    private float _mpPercent = 1.0f;

    // Cached strings to avoid allocation in _Draw()
    private string _hpText = "0/0";
    private string _mpText = "0/0";
    private Vector2 _cachedViewportSize;

    // Colors — dark = empty, bright = filled
    private static readonly Color HpEmpty = new(0.25f, 0.02f, 0.02f);
    private static readonly Color HpFill = new(0.75f, 0.08f, 0.08f);
    private static readonly Color MpEmpty = new(0.02f, 0.02f, 0.28f);
    private static readonly Color MpFill = new(0.08f, 0.15f, 0.8f);
    private static readonly Color BorderOuter = new(0.78f, 0.67f, 0.43f, 0.6f);
    private static readonly Color BorderInner = new(0.78f, 0.67f, 0.43f, 0.35f);
    private static readonly Color Highlight = new(1f, 1f, 1f, 0.12f);
    private static readonly Color LabelColor = new(0.9f, 0.9f, 0.9f);

    public void UpdateValues(int hp, int maxHp, int mp, int maxMp)
    {
        // Skip redraw if nothing changed
        if (_hp == hp && _maxHp == maxHp && _mp == mp && _maxMp == maxMp) return;

        _hp = hp;
        _maxHp = maxHp;
        _mp = mp;
        _maxMp = maxMp;
        _hpPercent = maxHp > 0 ? Mathf.Clamp((float)hp / maxHp, 0f, 1f) : 0f;
        _mpPercent = maxMp > 0 ? Mathf.Clamp((float)mp / maxMp, 0f, 1f) : 0f;
        _hpText = $"{hp}/{maxHp}";
        _mpText = $"{mp}/{maxMp}";
        QueueRedraw();
    }

    public override void _Draw()
    {
        // Cache viewport size — only changes on window resize
        var viewport = GetViewportRect().Size;
        _cachedViewportSize = viewport;

        var hpCenter = new Vector2(OrbMargin, viewport.Y - OrbBottomOffset);
        var mpCenter = new Vector2(viewport.X - OrbMargin, viewport.Y - OrbBottomOffset);

        DrawOrb(hpCenter, HpEmpty, HpFill, _hpPercent, _hpText, "HP");
        DrawOrb(mpCenter, MpEmpty, MpFill, _mpPercent, _mpText, "MP");
    }

    private void DrawOrb(Vector2 center, Color emptyColor, Color fillColor, float fillPercent, string valueText, string label)
    {
        // 1. Empty background
        DrawCircle(center, OrbRadius, emptyColor);

        // 2. Fill from bottom up (horizontal line sweep)
        if (fillPercent > 0.001f)
        {
            if (fillPercent >= 0.999f)
            {
                DrawCircle(center, OrbRadius - 1, fillColor);
            }
            else
            {
                float fillHeight = fillPercent * OrbRadius * 2f;
                float startRelY = OrbRadius - fillHeight;

                for (float ry = startRelY; ry <= OrbRadius; ry += 1.0f)
                {
                    float halfW = Mathf.Sqrt(Mathf.Max(0, OrbRadius * OrbRadius - ry * ry));
                    if (halfW < 0.5f) continue;
                    DrawLine(
                        new Vector2(center.X - halfW, center.Y + ry),
                        new Vector2(center.X + halfW, center.Y + ry),
                        fillColor, 1.0f
                    );
                }
            }
        }

        // 3. Glass highlight (subtle shine at top)
        float hlRadius = OrbRadius * 0.55f;
        var hlCenter = center - new Vector2(0, OrbRadius * 0.2f);
        for (float ry = -hlRadius; ry <= -hlRadius * 0.2f; ry += 1.0f)
        {
            float halfW = Mathf.Sqrt(Mathf.Max(0, hlRadius * hlRadius - ry * ry)) * 0.7f;
            if (halfW < 0.5f) continue;
            DrawLine(
                new Vector2(hlCenter.X - halfW, hlCenter.Y + ry),
                new Vector2(hlCenter.X + halfW, hlCenter.Y + ry),
                Highlight, 1.0f
            );
        }

        // 4. Double border (outer dark, inner lighter — metallic look)
        DrawArc(center, OrbRadius + 2f, 0, Mathf.Tau, ArcSegments, BorderOuter, 3.0f);
        DrawArc(center, OrbRadius, 0, Mathf.Tau, ArcSegments, BorderInner, 2.0f);

        // 5. Value text (centered below orb)
        var font = ThemeDB.FallbackFont;
        var valueSize = font.GetStringSize(valueText, HorizontalAlignment.Left, -1, 14);
        DrawString(font, center + new Vector2(-valueSize.X / 2, OrbRadius + 18), valueText,
            HorizontalAlignment.Left, -1, 14, LabelColor);

        // 6. Label text (centered above orb)
        var labelSize = font.GetStringSize(label, HorizontalAlignment.Left, -1, 12);
        DrawString(font, center + new Vector2(-labelSize.X / 2, -OrbRadius - 8), label,
            HorizontalAlignment.Left, -1, 12, LabelColor);
    }
}
