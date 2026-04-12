using Godot;

namespace DungeonGame.Ui;

/// <summary>
/// Diablo-style orb that fills/drains based on a current/max ratio.
/// Draws a circular fill from bottom-to-top inside an orb frame.
/// Used for HP (red) and MP (blue) on the HUD.
/// </summary>
public partial class OrbDisplay : Control
{
    private Color _fillColor;
    private Color _emptyColor;
    private Color _frameColor;
    private float _ratio = 1.0f;
    private string _label = "";
    private float _orbSize;

    public void Configure(float size, Color fillColor, Color frameColor)
    {
        _orbSize = size;
        _fillColor = fillColor;
        _emptyColor = new Color(0.06f, 0.06f, 0.10f, 0.9f);
        _frameColor = frameColor;
        CustomMinimumSize = new Vector2(size, size);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public void SetRatio(float current, float max, string label)
    {
        float newRatio = max > 0 ? Mathf.Clamp(current / max, 0f, 1f) : 0f;
        _label = label;
        if (!Mathf.IsEqualApprox(newRatio, _ratio, 0.001f))
        {
            _ratio = newRatio;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        float r = _orbSize / 2f;
        Vector2 center = new(r, r);

        // 1. Empty background circle
        DrawCircle(center, r - 1, _emptyColor);

        // 2. Fill from bottom up — draw as a clipped arc
        if (_ratio > 0f)
        {
            // Calculate the Y cutoff for the fill level
            float fillHeight = _ratio * (_orbSize - 2);
            float fillTop = _orbSize - 1 - fillHeight;

            // Draw fill as horizontal slices within the circle
            for (float y = fillTop; y < _orbSize - 1; y += 1f)
            {
                float dy = y - r;
                float halfWidth = Mathf.Sqrt(Mathf.Max(0, (r - 1) * (r - 1) - dy * dy));
                if (halfWidth > 0)
                {
                    // Slight brightness gradient — brighter at top of fill, darker at bottom
                    float brightness = 0.7f + 0.3f * (1f - (y - fillTop) / Mathf.Max(1, fillHeight));
                    Color lineColor = new(_fillColor, brightness);
                    DrawLine(new Vector2(r - halfWidth, y), new Vector2(r + halfWidth, y), lineColor, 1.5f);
                }
            }

            // Specular highlight — small bright spot near top-left
            float highlightR = r * 0.15f;
            Vector2 highlightPos = center + new Vector2(-r * 0.25f, -r * 0.3f);
            DrawCircle(highlightPos, highlightR, new Color(1, 1, 1, 0.25f));
        }

        // 3. Frame ring
        DrawArc(center, r - 1, 0, Mathf.Tau, 48, _frameColor, 2.0f);

        // 4. Text label (centered)
        if (!string.IsNullOrEmpty(_label))
        {
            var font = ThemeDB.FallbackFont;
            int fontSize = 10;
            Vector2 textSize = font.GetStringSize(_label, HorizontalAlignment.Center, -1, fontSize);
            Vector2 textPos = center - new Vector2(textSize.X / 2, -textSize.Y / 4);
            // Shadow
            DrawString(font, textPos + new Vector2(1, 1), _label, HorizontalAlignment.Left, -1, fontSize, new Color(0, 0, 0, 0.7f));
            // Text
            DrawString(font, textPos, _label, HorizontalAlignment.Left, -1, fontSize, Colors.White);
        }
    }
}
