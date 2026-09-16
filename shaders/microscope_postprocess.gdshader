shader_type canvas_item;

uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;
uniform float vignette_radius : hint_range(0.3, 1.2) = 0.75;
uniform float vignette_softness : hint_range(0.1, 0.8) = 0.45;
uniform float vignette_opacity : hint_range(0.0, 1.0) = 0.65;
uniform float chromatic_aberration : hint_range(0.0, 0.05) = 0.006;
uniform float noise_amount : hint_range(0.0, 0.05) = 0.012;

float hash(vec2 p) {
    return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

void fragment() {
    vec2 center = vec2(0.5, 0.5);
    vec2 diff = SCREEN_UV - center;
    float dist = length(diff);
    
    // Radial Chromatic Aberration
    float ca = pow(dist * 1.4, 2.0) * chromatic_aberration;
    vec2 ca_offset = normalize(diff + vec2(0.0001)) * ca;
    
    float r = texture(screen_texture, SCREEN_UV + ca_offset).r;
    float g = texture(screen_texture, SCREEN_UV).g;
    float b = texture(screen_texture, SCREEN_UV - ca_offset).b;
    vec3 col = vec3(r, g, b);
    
    // Circular Lens Vignette
    float vignette = smoothstep(vignette_radius, vignette_radius - vignette_softness, dist);
    col = mix(col * 0.15, col, mix(1.0, vignette, vignette_opacity));
    
    // Micro-grain
    float grain = (hash(SCREEN_UV * 1000.0 + fract(TIME * 43.12)) - 0.5) * noise_amount;
    col += grain;
    
    COLOR = vec4(col, 1.0);
}
