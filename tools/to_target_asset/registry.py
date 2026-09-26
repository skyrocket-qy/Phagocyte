from typing import Dict, Optional
from pathlib import Path

try:
    from .pipeline import (
        Pipeline,
        step_remove_bg,
        step_inpaint_edges,
        step_crop_center,
        step_kuwahara_filter,
        step_guided_filter,
        step_bilateral_filter,
        step_resize,
        step_apply_palette,
        step_apply_mask,
        step_make_unachieved_variant,
        step_quality_check
    )
except ImportError:
    from pipeline import (
        Pipeline,
        step_remove_bg,
        step_inpaint_edges,
        step_crop_center,
        step_kuwahara_filter,
        step_guided_filter,
        step_bilateral_filter,
        step_resize,
        step_apply_palette,
        step_apply_mask,
        step_make_unachieved_variant,
        step_quality_check
    )

CATEGORY_PIPELINES: Dict[str, Pipeline] = {
    # Achievement icons: 256x256 master with grayscale+dimmed Steam unachieved variant
    "achievement": Pipeline([
        step_remove_bg(tolerance=28),
        step_kuwahara_filter(radius=2),
        step_resize(256, 256, mode="inter_area"),
        step_make_unachieved_variant(brightness_factor=0.5),
        step_quality_check()
    ]),

    # Skill & Epigenetic Superweapon icons: 128x128
    "skill": Pipeline([
        step_remove_bg(tolerance=28),
        step_inpaint_edges(),
        step_guided_filter(radius=2),
        step_resize(128, 128, mode="inter_area"),
        step_quality_check()
    ]),

    # Passive tree traits & gear: 128x128 with circular mask
    "passive_tree": Pipeline([
        step_remove_bg(tolerance=28),
        step_apply_mask(margin=0.05),
        step_guided_filter(radius=2),
        step_resize(128, 128, mode="inter_area"),
        step_quality_check()
    ]),

    # Gear chamber equipment specimens: 128x128 with circular mask
    # (same specimen-box treatment as passive_tree traits)
    "gear": Pipeline([
        step_remove_bg(tolerance=28),
        step_apply_mask(margin=0.05),
        step_guided_filter(radius=2),
        step_resize(128, 128, mode="inter_area"),
        step_quality_check()
    ]),

    # UI elements & badges: Kuwahara smoothing + quality check
    "ui": Pipeline([
        step_kuwahara_filter(radius=2),
        step_quality_check()
    ])
}


def get_pipeline_for_path(rel_path: Path) -> Pipeline:
    """Resolve category pipeline based on relative path inside gen/."""
    parts = rel_path.parts
    if parts and parts[0] in CATEGORY_PIPELINES:
        return CATEGORY_PIPELINES[parts[0]]

    # Default fallback pipeline
    return Pipeline([
        step_remove_bg(tolerance=28),
        step_resize(128, 128, mode="inter_area")
    ])
