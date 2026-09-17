shader_type canvas_item;

uniform vec4 vignette_color : source_color = vec4(0.85, 0.06, 0.1, 0.75);
uniform float inner_radius : hint_range(0.2, 1.0) = 0.52;
uniform float outer_radius : hint_range(0.5, 1.5) = 0.98;
uniform float pulse_intensity : hint_range(0.0, 1.0) = 0.0;

void fragment() {
    vec2 uv = SCREEN_UV - vec2(0.5);
    uv.x *= 1.7778; // 16:9 aspect compensation
    float dist = length(uv);
    float vignette = smoothstep(inner_radius, outer_radius, dist);
    COLOR = vec4(vignette_color.rgb, vignette * pulse_intensity * vignette_color.a);
}
