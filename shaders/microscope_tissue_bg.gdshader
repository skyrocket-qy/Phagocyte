shader_type canvas_item;

uniform vec4 bg_color_deep : source_color = vec4(0.04, 0.05, 0.09, 1.0);
uniform vec4 bg_color_accent : source_color = vec4(0.12, 0.04, 0.07, 1.0);
uniform vec4 fiber_color : source_color = vec4(0.15, 0.22, 0.32, 0.35);
uniform float flow_speed : hint_range(0.0, 2.0) = 0.2;
uniform float fiber_density : hint_range(5.0, 40.0) = 18.0;

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

float fbm(vec2 p) {
    float v = 0.0;
    float a = 0.5;
    mat2 rot = mat2(vec2(0.8, 0.6), vec2(-0.6, 0.8));
    for (int i = 0; i < 3; i++) {
        v += a * noise(p);
        p = rot * p * 2.0 + vec2(100.0);
        a *= 0.5;
    }
    return v;
}

void fragment() {
    vec2 uv = UV * 16.0;
    vec2 drift = vec2(TIME * flow_speed * 0.08, TIME * flow_speed * 0.03);
    
    vec2 q = vec2(fbm(uv + drift), fbm(uv + vec2(5.2, 1.3) - drift * 0.5));
    vec2 r = vec2(fbm(uv + 4.0 * q + vec2(1.7, 9.2) + drift), fbm(uv + 4.0 * q + vec2(8.3, 2.8)));
    float f = fbm(uv + 4.0 * r);
    
    vec3 color = mix(bg_color_deep.rgb, bg_color_accent.rgb, clamp(f * f * 1.5, 0.0, 1.0));
    
    float fibers = sin((uv.x + uv.y * 0.6 + q.x * 2.0) * fiber_density + TIME * flow_speed * 0.2);
    fibers = smoothstep(0.4, 0.95, fibers) * 0.12;
    color += fiber_color.rgb * fibers;
    
    COLOR = vec4(color, 1.0);
}
