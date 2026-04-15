shader_type canvas_item;

// Pulsing glow effect for important objects (stairs, loot, NPCs).
// Adds a colored glow halo that pulses with TIME.
uniform vec4 glow_color : source_color = vec4(1.0, 0.84, 0.0, 0.6);
uniform float glow_width : hint_range(1.0, 6.0) = 2.0;
uniform float pulse_speed : hint_range(0.5, 4.0) = 1.5;
uniform bool enabled = true;

void fragment() {
    vec4 tex = texture(TEXTURE, UV);

    if (!enabled) {
        COLOR = tex;
        return;
    }

    if (tex.a > 0.1) {
        // Opaque pixel — apply pulse brightness
        float pulse = 0.5 + 0.5 * sin(TIME * pulse_speed * 3.14159);
        vec4 glow = vec4(glow_color.rgb * pulse * 0.3, 0.0);
        COLOR = tex + glow;
        COLOR.a = tex.a;
    } else {
        // Transparent pixel — check for nearby opaque to draw glow halo
        vec2 size = TEXTURE_PIXEL_SIZE * glow_width;
        float a = 0.0;
        for (float dx = -1.0; dx <= 1.0; dx += 1.0) {
            for (float dy = -1.0; dy <= 1.0; dy += 1.0) {
                a = max(a, texture(TEXTURE, UV + vec2(dx * size.x, dy * size.y)).a);
            }
        }

        if (a > 0.1) {
            float pulse = 0.5 + 0.5 * sin(TIME * pulse_speed * 3.14159);
            COLOR = vec4(glow_color.rgb, glow_color.a * pulse * a);
        } else {
            COLOR = tex;
        }
    }
}
