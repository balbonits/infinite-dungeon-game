using Godot;

namespace DungeonGame.Ui;

/// <summary>
/// Diablo-style orb using the PixelLab-generated orb sprites.
/// Fill level rises/falls behind the transparent glass orb texture.
/// </summary>
public partial class OrbDisplay : Control
{
    private Color _fillColor;
    private float _ratio = 1.0f;
    private string _label = "";
    private Texture2D? _orbTexture;
    private float _orbSize;

    public void Configure(float size, Color fillColor, string texturePath)
    {
        _orbSize = size;
        _fillColor = fillColor;
        CustomMinimumSize = new Vector2(size, size);
        MouseFilter = MouseFilterEnum.Ignore;

        if (ResourceLoader.Exists(texturePath))
            _orbTexture = GD.Load<Texture2D>(texturePath);
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
        float s = _orbSize;

        // 1. Dark background (empty state)
        DrawRect(new Rect2(0, 0, s, s), new Color(0.04f, 0.04f, 0.06f, 0.8f));

        // 2. Colored fill from bottom up
        if (_ratio > 0f)
        {
            float fillHeight = _ratio * s;
            float fillTop = s - fillHeight;

            // Fill gradient — brighter at top of liquid
            Color topColor = new(_fillColor, 0.95f);
            Color bottomColor = new(_fillColor.R * 0.5f, _fillColor.G * 0.5f, _fillColor.B * 0.5f, 0.9f);

            // Draw fill as gradient slices
            for (float y = fillTop; y < s; y += 1f)
            {
                float t = (y - fillTop) / Mathf.Max(1f, fillHeight);
                Color lineColor = topColor.Lerp(bottomColor, t);
                DrawLine(new Vector2(0, y), new Vector2(s, y), lineColor, 1.0f);
            }
        }

        // 3. Orb texture on top (glass frame with transparency)
        if (_orbTexture != null)
        {
            DrawTextureRect(_orbTexture, new Rect2(0, 0, s, s), false);
        }

        // 4. Text label (centered)
        if (!string.IsNullOrEmpty(_label))
        {
            var font = ThemeDB.FallbackFont;
            int fontSize = (int)(s * 0.18f);
            Vector2 textSize = font.GetStringSize(_label, HorizontalAlignment.Center, -1, fontSize);
            Vector2 textPos = new((s - textSize.X) / 2f, s / 2f + textSize.Y / 4f);
            // Shadow
            DrawString(font, textPos + new Vector2(1, 1), _label, HorizontalAlignment.Left, -1, fontSize, new Color(0, 0, 0, 0.8f));
            // Text
            DrawString(font, textPos, _label, HorizontalAlignment.Left, -1, fontSize, Colors.White);
        }
    }
}
