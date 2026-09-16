shader_type canvas_item;

uniform float cell_radius = 48.0;
uniform vec4 tint_color : source_color = vec4(0.14, 0.26, 0.42, 0.25);
uniform vec4 rim_color : source_color = vec4(0.75, 0.88, 1.15, 1.0);
uniform float rim_power : hint_range(0.5, 8.0) = 3.2;
uniform float inner_alpha : hint_range(0.0, 1.0) = 0.20;
uniform float flow_speed : hint_range(0.0, 4.0) = 1.0;
uniform float refraction_strength : hint_range(0.0, 0.05) = 0.015;

uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;

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
    vec3 normal = normalize(vec3(pos_norm.x, pos_norm.y, z * 2.0));
    
    // 2. Microscope Key Light & Directional Diffuse (Top-Left Illuminator)
    vec3 light_dir = normalize(vec3(-0.45, -0.65, 0.70));
    float diff = dot(normal, light_dir) * 0.45 + 0.55;
    
    // 3. Dual-Lobe Wet Specular Highlights
    vec3 view_dir = vec3(0.0, 0.0, 1.0);
    vec3 half_vec = normalize(light_dir + view_dir);
    float n_dot_h = max(0.0, dot(normal, half_vec));
    
    // Broad wet liquid sheen
    float spec_broad = pow(n_dot_h, 16.0) * 0.48;
    // Sharp glint gleam
    float spec_sharp = pow(n_dot_h, 64.0) * 0.85;
    vec3 specular_glint = vec3(0.88, 0.95, 1.0) * (spec_broad + spec_sharp);
    
    // 4. Background Optical Lens Refraction (Through translucent cytoplasm)
    vec2 refract_offset = normal.xy * refraction_strength * (1.0 - clamped_dist * 0.35);
    vec4 bg_sample = texture(screen_texture, SCREEN_UV - refract_offset);
    float bg_lum = max(bg_sample.r, max(bg_sample.g, bg_sample.b));
    
    // 5. Fluid Granules & Cytosol Streaming
    vec2 flow_uv = pos_norm * 4.0 + vec2(TIME * flow_speed * 0.12, sin(TIME * flow_speed * 0.16 + pos_norm.x * 2.5) * 0.15);
    float n1 = noise(flow_uv);
    float n2 = noise(flow_uv * 2.2 + vec2(2.3, 4.1));
    float granule = (n1 * 0.65 + n2 * 0.35 - 0.5) * 0.05;
    
    // 6. Suspended Micro-Vacuoles
    float vac = vacuoles(pos_norm, TIME * flow_speed);
    
    // 7. Fresnel Meniscus Edge & Phase Shadow
    float rim = pow(clamped_dist, rim_power);
    vec3 edge_reflection = rim_color.rgb * pow(rim, 3.2) * 0.45;
    
    // Phase-contrast shadow right inside the rim for 3D depth
    float phase_shadow = smoothstep(0.65, 0.88, clamped_dist) * (1.0 - smoothstep(0.88, 1.0, clamped_dist)) * 0.10;
    
    // 8. Volumetric Transparency & Shading Assembly
    float alpha = mix(inner_alpha, 0.78, pow(rim, 1.8));
    
    vec3 base_rgb = (tint_color.rgb * diff) + vec3(granule) - vec3(phase_shadow);
    base_rgb += vec3(vac * 0.22);
    
    // Blend with background refraction if present
    if (bg_lum > 0.02) {
        vec3 refracted_tint = bg_sample.rgb * (tint_color.rgb * 2.5 + vec3(0.2));
        base_rgb = mix(base_rgb, refracted_tint, 0.35);
    }
    
    vec3 final_rgb = mix(base_rgb, rim_color.rgb * 0.6, rim * 0.4) + specular_glint + edge_reflection;
    
    COLOR = vec4(final_rgb, alpha);
}
