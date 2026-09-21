shader_type canvas_item;

// GFP watermark: the opaque JPEG has no alpha channel, so the luma mask
// doubles as transparency (dark texels -> alpha ~0, no visible rectangle).
// Incoming COLOR.a carries the node modulate alpha (breathing pulse driven
// by MainMenu._Process).
uniform vec4 tint : source_color = vec4(0.39, 1.0, 0.85, 1.0);
uniform float strength : hint_range(0.0, 2.0) = 1.25;

void fragment() {
	vec4 tex = texture(TEXTURE, UV);
	float luma = dot(tex.rgb, vec3(0.299, 0.587, 0.114));
	float mask = smoothstep(0.02, 0.25, luma);
	COLOR = vec4(tex.rgb * tint.rgb * strength, mask * COLOR.a);
}
