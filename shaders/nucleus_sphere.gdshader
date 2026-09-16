shader_type canvas_item;

uniform vec4 core_color : source_color = vec4(0.48, 0.22, 0.68, 0.95);
uniform vec4 edge_color : source_color = vec4(0.14, 0.04, 0.22, 1.0);
uniform vec4 highlight_color : source_color = vec4(0.68, 0.36, 0.88, 1.0);
uniform float radius = 16.0;

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

void fragment() {
    vec2 p = local_pos / max(1.0, radius);
    float r = length(p);
    if (r > 1.0) {
        discard;
    }

    // 1. 3D Spherical Normal
    float z = sqrt(max(0.0, 1.0 - r * r));
    vec3 normal = normalize(vec3(p.x, p.y, z * 1.5));

    // 2. Microscope Key Light & Directional Half-Lambert Shading
    vec3 light_dir = normalize(vec3(-0.45, -0.65, 0.65));
    float diff = dot(normal, light_dir) * 0.5 + 0.5;

    vec3 view_dir = vec3(0.0, 0.0, 1.0);
    vec3 half_vec = normalize(light_dir + view_dir);
    float spec = pow(max(0.0, dot(normal, half_vec)), 28.0) * 0.35;

    // 3. Heterochromatin Speckles (染色質深色微顆粒)
    float n1 = noise(p * 8.0);
    float n2 = noise(p * 16.0 + vec2(3.1, 5.7));
    float chromatin = (n1 * 0.6 + n2 * 0.4) * 0.18;

    // 4. Nucleolus (緻密偏心核仁)
    vec2 nucleolus_pos = vec2(-0.22, -0.15);
    float d_nuc = length(p - nucleolus_pos);
    float nucleolus = smoothstep(0.24, 0.08, d_nuc) * 0.30;

    // 5. Radial Hematoxylin Staining (外周深紫核膜，中心受光區明亮)
    float rim = smoothstep(0.25, 0.98, r);
    vec3 base_col = mix(core_color.rgb, edge_color.rgb, rim);
    base_col = mix(base_col, highlight_color.rgb, max(0.0, diff - 0.45) * 0.55);

    // Subtract density from chromatin & nucleolus
    base_col -= vec3(chromatin * 0.75 + nucleolus);

    // Subtle nuclear envelope border
    float envelope = smoothstep(0.88, 0.99, r) * 0.3;
    base_col = mix(base_col, edge_color.rgb * 0.7, envelope);

    // Specular highlight glint
    vec3 final_col = base_col + vec3(spec);

    // Soft anti-aliased edge
    float alpha = smoothstep(1.0, 0.95, r) * core_color.a;

    COLOR = vec4(final_col, alpha);
}
