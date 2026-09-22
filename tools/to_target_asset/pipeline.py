from dataclasses import dataclass, field
from pathlib import Path
from typing import Callable, List, Dict, Any, Tuple, Optional
from PIL import Image

try:
    import numpy as np
    HAS_NUMPY = True
except ImportError:
    HAS_NUMPY = False

try:
    from .rm_bg import remove_background_outer_floodfill
    from . import filters, palette, imgutil, quality
except ImportError:
    from rm_bg import remove_background_outer_floodfill
    import filters, palette, imgutil, quality


@dataclass
class ImageContext:
    src_path: Path
    dest_path: Path
    category: str
    pil_image: Image.Image
    img_rgb: Any = None      # HWC uint8 RGB if numpy available
    alpha: Any = None        # HW uint8 Alpha if numpy available
    metadata: Dict[str, Any] = field(default_factory=dict)
    warnings: List[str] = field(default_factory=list)
    extra_outputs: List[Tuple[Path, Image.Image]] = field(default_factory=list)  # (dest_path, PIL Image)

    def sync_from_pil(self):
        if HAS_NUMPY:
            rgba = np.array(self.pil_image.convert("RGBA"))
            self.img_rgb = rgba[:, :, :3]
            self.alpha = rgba[:, :, 3]

    def sync_to_pil(self):
        if HAS_NUMPY and self.img_rgb is not None and self.alpha is not None:
            rgba = np.dstack([self.img_rgb, self.alpha])
            self.pil_image = Image.fromarray(rgba, mode="RGBA")

    def get_rgba_np(self):
        if HAS_NUMPY and self.img_rgb is not None and self.alpha is not None:
            return np.dstack([self.img_rgb, self.alpha])
        return None

    def set_rgba_np(self, rgba):
        if HAS_NUMPY:
            if rgba.shape[2] == 4:
                self.img_rgb = rgba[:, :, :3]
                self.alpha = rgba[:, :, 3]
            else:
                self.img_rgb = rgba
                self.alpha = np.ones((rgba.shape[0], rgba.shape[1]), dtype=np.uint8) * 255
            self.sync_to_pil()


StepFunc = Callable[[ImageContext], ImageContext]


class Pipeline:
    def __init__(self, steps: List[StepFunc]):
        self.steps = steps

    def execute(self, ctx: ImageContext) -> ImageContext:
        for step in self.steps:
            ctx = step(ctx)
        return ctx


# --- Step Factory Closures ---

def step_remove_bg(tolerance: int = 28) -> StepFunc:
    def step(ctx: ImageContext) -> ImageContext:
        ctx.pil_image = remove_background_outer_floodfill(ctx.pil_image, tolerance=tolerance)
        ctx.sync_from_pil()
        return ctx
    return step


def step_inpaint_edges() -> StepFunc:
    def step(ctx: ImageContext) -> ImageContext:
        if HAS_NUMPY and ctx.img_rgb is not None and ctx.alpha is not None:
            ctx.img_rgb = imgutil._inpaint_rgb_into_transparent(ctx.img_rgb, ctx.alpha)
            ctx.sync_to_pil()
        return ctx
    return step


def step_crop_center(width: int, height: int) -> StepFunc:
    def step(ctx: ImageContext) -> ImageContext:
        if HAS_NUMPY:
            rgba = ctx.get_rgba_np()
            cropped = imgutil.crop_center_np(rgba, width, height)
            ctx.set_rgba_np(cropped)
        else:
            w, h = ctx.pil_image.size
            left = (w - width) // 2
            top = (h - height) // 2
            ctx.pil_image = ctx.pil_image.crop((left, top, left + width, top + height))
            ctx.sync_from_pil()
        return ctx
    return step


def step_kuwahara_filter(radius: int = 3) -> StepFunc:
    def step(ctx: ImageContext) -> ImageContext:
        if HAS_NUMPY:
            filtered = filters.kuwahara_filter(ctx.get_rgba_np(), radius=radius)
            ctx.set_rgba_np(filtered)
        return ctx
    return step


def step_guided_filter(radius: int = 2, eps: float = 0.01) -> StepFunc:
    def step(ctx: ImageContext) -> ImageContext:
        if HAS_NUMPY:
            filtered = filters.guided_filter(ctx.get_rgba_np(), r=radius, eps=eps)
            ctx.set_rgba_np(filtered)
        return ctx
    return step


def step_bilateral_filter(spatial_sigma: float = 5.0, color_sigma: float = 35.0) -> StepFunc:
    def step(ctx: ImageContext) -> ImageContext:
        if HAS_NUMPY:
            filtered = filters.bilateral_filter(ctx.get_rgba_np(), spatial_sigma=spatial_sigma, color_sigma=color_sigma)
            ctx.set_rgba_np(filtered)
        return ctx
    return step


def step_resize(width: int, height: int, mode: str = "inter_area") -> StepFunc:
    def step(ctx: ImageContext) -> ImageContext:
        if HAS_NUMPY:
            resized = imgutil.resize_image(ctx.get_rgba_np(), width, height, mode=mode)
            ctx.set_rgba_np(resized)
        else:
            ctx.pil_image = imgutil.resize_image(ctx.pil_image, width, height)
            ctx.sync_from_pil()
        return ctx
    return step


def step_apply_palette(palette_name: str = "bio_fluo", dither: str = "none", dist: str = "perceptual", strength: float = 0.5) -> StepFunc:
    def step(ctx: ImageContext) -> ImageContext:
        if HAS_NUMPY:
            rgba = ctx.get_rgba_np()
            if dither == "ordered":
                res = palette.apply_ordered_dither(rgba, palette_name=palette_name, dist_type=dist, strength=strength)
            else:
                res = palette.apply_palette(rgba, palette_name=palette_name, dist_type=dist)
            ctx.set_rgba_np(res)
        return ctx
    return step


def step_apply_mask(margin: float = 0.05, bg_hex: str = "00000000") -> StepFunc:
    def step(ctx: ImageContext) -> ImageContext:
        if HAS_NUMPY:
            masked = imgutil.apply_mask(ctx.get_rgba_np(), margin=margin, bg_hex=bg_hex)
            ctx.set_rgba_np(masked)
        return ctx
    return step


def step_make_unachieved_variant(brightness_factor: float = 0.5) -> StepFunc:
    def step(ctx: ImageContext) -> ImageContext:
        if HAS_NUMPY:
            unachieved_rgba = imgutil.make_unachieved_image(ctx.get_rgba_np(), brightness_factor=brightness_factor)
            unachieved_pil = Image.fromarray(unachieved_rgba, mode="RGBA")
        else:
            unachieved_pil = imgutil.make_unachieved_image(ctx.pil_image, brightness_factor=brightness_factor)

        unachieved_path = ctx.dest_path.with_name(f"{ctx.dest_path.stem}_unachieved.png")
        ctx.extra_outputs.append((unachieved_path, unachieved_pil))
        return ctx
    return step


def step_quality_check() -> StepFunc:
    def step(ctx: ImageContext) -> ImageContext:
        if HAS_NUMPY:
            issues = quality.run_checks(ctx.get_rgba_np())
            if issues:
                ctx.warnings.extend(issues)
        return ctx
    return step
