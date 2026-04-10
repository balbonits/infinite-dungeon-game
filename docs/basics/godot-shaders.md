# Godot 2D Shaders

## Why This Matters
We need visual effects that can't be done with tweens alone: glow outlines on selected enemies, flash effects that work with sprite sheets, fill effects for HP orbs. Shaders run on the GPU and are the right tool for per-pixel visual effects.

## Core Concepts

### What Is a Shader?
A small program that runs on the GPU for every pixel of a sprite. In 2D Godot, shaders are written in Godot Shading Language (similar to GLSL) and attached to CanvasItem materials.

### CanvasItem Shaders (2D)
The shader type for all 2D rendering:

```glsl
shader_type canvas_item;

void fragment() {
    vec4 tex = texture(TEXTURE, UV);  // Sample the sprite texture
    COLOR = tex;  // Output the color
}
```

`fragment()` runs for every visible pixel. You can modify `COLOR` to change what's drawn.

### Common Game Effects

**1. Flash White (Hit Feedback)**
```glsl
shader_type canvas_item;
uniform float flash_amount : hint_range(0.0, 1.0) = 0.0;

void fragment() {
    vec4 tex = texture(TEXTURE, UV);
    COLOR = mix(tex, vec4(1.0, 1.0, 1.0, tex.a), flash_amount);
}
```
Set `flash_amount = 1.0` on hit, tween back to 0.0.

**2. Outline (Selected Entity)**
```glsl
shader_type canvas_item;
uniform vec4 outline_color : source_color = vec4(1.0, 0.84, 0.0, 1.0);
uniform float outline_width : hint_range(0.0, 10.0) = 2.0;

void fragment() {
    vec4 tex = texture(TEXTURE, UV);
    if (tex.a < 0.1) {
        // Check neighbors for non-transparent pixels
        vec2 size = TEXTURE_PIXEL_SIZE * outline_width;
        float a = texture(TEXTURE, UV + vec2(-size.x, 0)).a;
        a = max(a, texture(TEXTURE, UV + vec2(size.x, 0)).a);
        a = max(a, texture(TEXTURE, UV + vec2(0, -size.y)).a);
        a = max(a, texture(TEXTURE, UV + vec2(0, size.y)).a);
        if (a > 0.1) COLOR = outline_color;
        else COLOR = tex;
    } else {
        COLOR = tex;
    }
}
```

**3. HP Orb Fill (Circle Fill from Bottom)**
```glsl
shader_type canvas_item;
uniform float fill_percent : hint_range(0.0, 1.0) = 0.75;
uniform vec4 fill_color : source_color = vec4(0.8, 0.1, 0.1, 1.0);
uniform vec4 empty_color : source_color = vec4(0.15, 0.05, 0.05, 1.0);

void fragment() {
    vec2 uv = UV * 2.0 - 1.0;  // Center: -1 to 1
    float dist = length(uv);
    if (dist > 1.0) discard;
    
    float y_norm = (uv.y + 1.0) / 2.0;  // 0=top, 1=bottom
    COLOR = y_norm >= (1.0 - fill_percent) ? fill_color : empty_color;
}
```

### Applying Shaders in C#
```csharp
var material = new ShaderMaterial();
material.Shader = ResourceLoader.Load<Shader>("res://shaders/flash.gdshader");
sprite.Material = material;

// Animate the uniform
material.SetShaderParameter("flash_amount", 1.0f);
var tween = CreateTween();
tween.TweenMethod(
    Callable.From<float>(v => material.SetShaderParameter("flash_amount", v)),
    1.0f, 0.0f, 0.15f
);
```

### When to Use Shaders vs Tweens

| Effect | Shader | Tween |
|--------|--------|-------|
| Flash white | Yes (per-pixel) | Modulate only works for tint, not full white |
| Outline | Yes (per-pixel neighbor check) | No (can't draw outside sprite bounds) |
| HP fill | Yes (circle math) | Sort of (line-by-line DrawLine, but slower) |
| Fade in/out | No (overkill) | Yes (Modulate alpha) |
| Scale bounce | No | Yes (Scale property) |
| Position drift | No | Yes (Position property) |

**Rule:** Shaders for per-pixel effects. Tweens for property animation.

## Common Mistakes
1. **Shader on every sprite** — each unique shader breaks batching. Share materials where possible.
2. **Complex shaders on many sprites** — GPU-bound performance drop. Keep fragment() simple.
3. **Forgetting to discard transparent pixels** — outline shader creates a filled rectangle instead of following the sprite shape.
4. **Not using TEXTURE_PIXEL_SIZE** — hardcoded pixel sizes break at different sprite resolutions.
5. **Modifying VERTEX in fragment()** — VERTEX is read-only in fragment; use vertex() function instead.

## Checklist
- [ ] Flash shader for hit feedback (instead of Modulate white which tints)
- [ ] Outline shader for entity selection/hover
- [ ] ShaderMaterial shared between similar entities (not duplicated per instance)
- [ ] Shader uniforms animated via Tween for smooth transitions

## Sources
- [Godot Shading Language](https://docs.godotengine.org/en/stable/tutorials/shaders/shader_reference/shading_language.html)
- [Godot CanvasItem Shaders](https://docs.godotengine.org/en/stable/tutorials/shaders/shader_reference/canvas_item_shader.html)
- [Godot Shader Examples](https://godotshaders.com/)
- [The Book of Shaders (intro)](https://thebookofshaders.com/)
