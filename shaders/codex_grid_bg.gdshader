shader_type canvas_item;

// Faint terminal scan-grid for the fullscreen codex. Procedural 40px cells,
// ~1.5px lines at 5% cyan alpha over the opaque modal background.
uniform vec4 line_color : source_color = vec4(0.0, 0.9, 1.0, 1.0);
uniform float cell_px = 40.0;
uniform float alpha = 0.05;

void fragment() {
	vec2 gv = fract(FRAGCOORD.xy / cell_px);
	float dx = min(gv.x, 1.0 - gv.x);
	float dy = min(gv.y, 1.0 - gv.y);
	float line = clamp((1.0 - smoothstep(0.0, 0.045, dx)) + (1.0 - smoothstep(0.0, 0.045, dy)), 0.0, 1.0);
	COLOR = vec4(line_color.rgb, line * alpha);
}
