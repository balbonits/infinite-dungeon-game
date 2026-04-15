shader_type canvas_item;

// Colored outline around sprite edges.
// Works by checking transparent pixels for opaque neighbors.
uniform vec4 outline_color : source_color = vec4(1.0, 0.84, 0.0, 1.0);
uniform float outline_width : hint_range(0.5, 4.0) = 1.0;
uniform bool enabled = false;

void fragment() {
    vec4 tex = texture(TEXTURE, UV);

    if (!enabled) {
        COLOR = tex;
        return;
    }

    if (tex.a < 0.1) {
        // Transparent pixel — check 8 neighbors for opaque pixels
        vec2 size = TEXTURE_PIXEL_SIZE * outline_width;
        float a = 0.0;
        a = max(a, texture(TEXTURE, UV + vec2(-size.x, 0.0)).a);
        a = max(a, texture(TEXTURE, UV + vec2(size.x, 0.0)).a);
        a = max(a, texture(TEXTURE, UV + vec2(0.0, -size.y)).a);
        a = max(a, texture(TEXTURE, UV + vec2(0.0, size.y)).a);
        a = max(a, texture(TEXTURE, UV + vec2(-size.x, -size.y)).a);
        a = max(a, texture(TEXTURE, UV + vec2(size.x, -size.y)).a);
        a = max(a, texture(TEXTURE, UV + vec2(-size.x, size.y)).a);
        a = max(a, texture(TEXTURE, UV + vec2(size.x, size.y)).a);

        if (a > 0.1) {
            COLOR = outline_color;
        } else {
            COLOR = tex;
        }
    } else {
        COLOR = tex;
    }
}
