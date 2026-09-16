shader_type canvas_item;

uniform float cell_radius = 48.0;
uniform vec4 tint_color : source_color = vec4(0.14, 0.26, 0.42, 0.25);
uniform vec4 rim_color : source_color = vec4(0.75, 0.88, 1.15, 1.0);
uniform float rim_power : hint_range(0.5, 8.0) = 3.5;
uniform float inner_alpha : hint_range(0.0, 1.0) = 0.20;
uniform float flow_speed : hint_range(0.0, 4.0) = 1.0;

varying vec2 local_pos;

void vertex() {
    local_pos = VERTEX;
}

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

// Internal floating vacuoles (微觀懸浮吞噬泡/液泡)
float vacuoles(vec2 uv, float t) {
    vec2 p = uv * 5.0 + vec2(t * 0.08, sin(t * 0.12) * 0.2);
    vec2 id = floor(p);
    vec2 gv = fract(p) - 0.5;
    
    float min_d = 1.0;
    for (int y = -1; y <= 1; y++) {
        for (int x = -1; x <= 1; x++) {
            vec2 offset = vec2(float(x), float(y));
            vec2 cell_id = id + offset;
            float h = hash(cell_id);
            if (h > 0.55) {
                vec2 center = offset + vec2(sin(h * 6.28 + t * 0.35) * 0.2, cos(h * 6.28 + t * 0.35) * 0.2);
                float d = length(gv - center);
                min_d = min(min_d, d);
            }
        }
    }
    // Delicate refractive bubble ring with soft specular center
    float ring = smoothstep(0.18, 0.13, min_d) * smoothstep(0.06, 0.10, min_d);
    float core = smoothstep(0.08, 0.0, min_d) * 0.3;
    return ring * 0.6 + core;
}

void fragment() {
    vec2 pos_norm = local_pos / max(1.0, cell_radius);
    float dist = length(pos_norm);
    float clamped_dist = clamp(dist, 0.0, 1.0);
    
    // 1. Pseudo-3D Curved Dome Normal (Curved Liquid Surface)
    float z = sqrt(max(0.0, 1.0 - clamped_dist * clamped_dist));
    vec3 normal = normalize(vec3(pos_norm.x, pos_norm.y, z * 1.8));
    
    // 2. Microscope Directional Key Light & Glassy Specular Highlight
    vec3 light_dir = normalize(vec3(-0.45, -0.65, 0.75));
    vec3 view_dir = vec3(0.0, 0.0, 1.0);
    vec3 half_vec = normalize(light_dir + view_dir);
    float spec = pow(max(0.0, dot(normal, half_vec)), 48.0) * 0.32;
    vec3 specular_glint = vec3(0.85, 0.92, 1.0) * spec;
    
    // 3. Fluid Granules & Cytosol Streaming
    vec2 flow_uv = pos_norm * 4.0 + vec2(TIME * flow_speed * 0.12, sin(TIME * flow_speed * 0.16 + pos_norm.x * 2.5) * 0.15);
    float n1 = noise(flow_uv);
    float n2 = noise(flow_uv * 2.2 + vec2(2.3, 4.1));
    float granule = (n1 * 0.65 + n2 * 0.35 - 0.5) * 0.05;
    
    // 4. Suspended Micro-Vacuoles
    float vac = vacuoles(pos_norm, TIME * flow_speed);
    
    // 5. Fresnel Meniscus Edge & Phase Shadow
    float rim = pow(clamped_dist, rim_power);
    vec3 edge_reflection = rim_color.rgb * pow(rim, 3.2) * 0.45;
    
    // Phase-contrast shadow right inside the rim for 3D depth
    float phase_shadow = smoothstep(0.65, 0.88, clamped_dist) * (1.0 - smoothstep(0.88, 1.0, clamped_dist)) * 0.10;
    
    // 6. Volumetric Transparency:
    // Core is crystal clear (inner_alpha ~ 0.20) so background tissue and engulfed bacteria are visible!
    // Rim climbs to 0.75 for the refractile border.
    float alpha = mix(inner_alpha, 0.78, pow(rim, 1.8));
    
    // Deep translucent liquid color
    vec3 base_rgb = tint_color.rgb + vec3(granule) - vec3(phase_shadow);
    base_rgb += vec3(vac * 0.22); // Subtle glowing organelle bubbles
    
    vec3 final_rgb = mix(base_rgb, rim_color.rgb * 0.6, rim * 0.4) + specular_glint + edge_reflection;
    
    COLOR = vec4(final_rgb, alpha);
}
