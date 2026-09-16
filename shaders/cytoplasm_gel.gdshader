shader_type canvas_item;

uniform vec4 tint_color : source_color = vec4(0.18, 0.72, 0.65, 0.55);
uniform vec4 rim_color : source_color = vec4(0.4, 1.8, 2.2, 1.0);
uniform float rim_power : hint_range(0.5, 6.0) = 2.4;
uniform float inner_alpha : hint_range(0.1, 1.0) = 0.42;
uniform float flow_speed : hint_range(0.0, 4.0) = 1.2;

float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(
        mix(hash(i + vec2(0.0, 0.0)), hash(i + vec2(1.0, 0.0)), f.x),
        mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), f.x),
        f.y
    );
}

void fragment() {
    vec2 centered_uv = (UV - vec2(0.5)) * 2.0;
    float dist = length(centered_uv);
    
    // Internal fluid organelle & cytosol streaming
    vec2 flow_uv = UV * 4.0 + vec2(TIME * flow_speed * 0.15, sin(TIME * flow_speed * 0.2 + UV.x * 3.0) * 0.2);
    float n = noise(flow_uv);
    
    // Smooth Fresnel rim towards edge
    float rim = pow(clamp(dist, 0.0, 1.0), rim_power);
    
    // Base cytoplasm body: semi-transparent gel with subtle micro-granules
    vec4 base = tint_color;
    base.rgb += (n - 0.5) * 0.08;
    base.a = mix(inner_alpha, 0.95, rim);
    
    // Add bright bioluminescent rim emission (HDR bloom)
    vec3 emission = rim_color.rgb * pow(rim, 1.8) * 0.75;
    vec3 final_rgb = mix(base.rgb, rim_color.rgb * 0.6, rim * 0.5) + emission;
    
    COLOR = vec4(final_rgb, base.a);
}
