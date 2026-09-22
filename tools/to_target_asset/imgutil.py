from typing import Union, Tuple
from PIL import Image, ImageEnhance, ImageOps

try:
    import cv2
    import numpy as np
    HAS_CV2_NUMPY = True
except ImportError:
    HAS_CV2_NUMPY = False


def _inpaint_rgb_into_transparent(rgb, alpha):
    """
    Dilate foreground RGB into transparent (alpha == 0) regions so that
    filters and downscaling never sample garbage background colors (black/white).
    This eliminates dark halos and bright fringes at alpha edges.
    """
    if not HAS_CV2_NUMPY:
        return rgb.copy() if hasattr(rgb, "copy") else rgb

    fg_mask = (alpha > 0).astype(np.uint8)

    if fg_mask.all() or not fg_mask.any():
        return rgb.copy()

    inpaint_mask = (1 - fg_mask) * 255
    inpaint_mask = inpaint_mask.astype(np.uint8)

    rgb_bgr = cv2.cvtColor(rgb, cv2.COLOR_RGB2BGR)
    inpainted_bgr = cv2.inpaint(rgb_bgr, inpaint_mask, inpaintRadius=3, flags=cv2.INPAINT_TELEA)
    return cv2.cvtColor(inpainted_bgr, cv2.COLOR_BGR2RGB)


def crop_center_np(img_np, target_w: int, target_h: int):
    """Center-crop an image array to target dimensions, with transparent padding if smaller."""
    if not HAS_CV2_NUMPY:
        return img_np

    h, w, c = img_np.shape
    left = (w - target_w) // 2
    top = (h - target_h) // 2
    right = left + target_w
    bottom = top + target_h

    if left >= 0 and top >= 0 and right <= w and bottom <= h:
        return img_np[top:bottom, left:right]

    res = np.zeros((target_h, target_w, c), dtype=np.uint8)
    src_l = max(0, left)
    src_t = max(0, top)
    src_r = min(w, right)
    src_b = min(h, bottom)

    dst_x = max(0, -left)
    dst_y = max(0, -top)
    crop_w = src_r - src_l
    crop_h = src_b - src_t

    res[dst_y:dst_y+crop_h, dst_x:dst_x+crop_w] = img_np[src_t:src_b, src_l:src_r]
    return res


def resize_image(img_data, width: int, height: int, mode: str = "inter_area"):
    """Resize image using OpenCV or Pillow."""
    if width <= 0 or height <= 0:
        return img_data

    if HAS_CV2_NUMPY and isinstance(img_data, np.ndarray):
        interp = cv2.INTER_AREA if mode == "inter_area" else cv2.INTER_CUBIC
        return cv2.resize(img_data, (width, height), interpolation=interp)
    elif isinstance(img_data, Image.Image):
        return img_data.resize((width, height), resample=Image.Resampling.LANCZOS)
    return img_data


def apply_mask(img_np, margin: float = 0.05, bg_hex: str = "00000000"):
    """Apply a circular mask with margin and background color."""
    if not HAS_CV2_NUMPY:
        return img_np

    h, w = img_np.shape[:2]
    cx, cy = w / 2.0, h / 2.0
    limit_r = min(w, h) / 2.0 - (min(w, h) * margin)

    y, x = np.ogrid[:h, :w]
    dist = np.sqrt((x - cx + 0.5)**2 + (y - cy + 0.5)**2)
    mask = dist > limit_r

    res = img_np.copy()
    if res.shape[2] == 4:
        res[mask, 3] = 0
    return res


def make_unachieved_image(img_input, brightness_factor: float = 0.5):
    """
    Generate grayscale + dimmed unachieved version of an icon.
    Required by Steamworks Partner and in-game gallery locked state.
    """
    if HAS_CV2_NUMPY and isinstance(img_input, np.ndarray):
        has_alpha = (img_input.shape[2] == 4)
        if has_alpha:
            rgb = img_input[:, :, :3]
            alpha = img_input[:, :, 3]
        else:
            rgb = img_input

        gray = (0.299 * rgb[:, :, 0].astype(np.float32) +
                0.587 * rgb[:, :, 1].astype(np.float32) +
                0.114 * rgb[:, :, 2].astype(np.float32)) * brightness_factor
        gray = np.clip(gray, 0, 255).astype(np.uint8)
        gray_rgb = np.dstack([gray, gray, gray])

        if has_alpha:
            return np.dstack([gray_rgb, alpha])
        return gray_rgb
    elif isinstance(img_input, Image.Image):
        rgba = img_input.convert("RGBA")
        r, g, b, a = rgba.split()
        gray = ImageOps.grayscale(rgba)
        enhancer = ImageEnhance.Brightness(gray)
        dimmed = enhancer.enhance(brightness_factor)
        return Image.merge("RGBA", (dimmed, dimmed, dimmed, a))

    return img_input
