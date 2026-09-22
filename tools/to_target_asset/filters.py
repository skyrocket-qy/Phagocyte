from typing import Optional
from PIL import Image, ImageFilter

try:
    import cv2
    import numpy as np
    HAS_CV2_NUMPY = True
except ImportError:
    HAS_CV2_NUMPY = False


def bilateral_filter(img_np, spatial_sigma: float = 5.0, color_sigma: float = 35.0):
    """Apply Bilateral Filter using OpenCV."""
    if not HAS_CV2_NUMPY:
        return img_np

    if img_np.shape[2] == 4:
        rgb = img_np[:, :, :3]
        alpha = img_np[:, :, 3]
        res_rgb = cv2.bilateralFilter(rgb, d=-1, sigmaColor=color_sigma, sigmaSpace=spatial_sigma)
        return np.dstack([res_rgb, alpha])
    return cv2.bilateralFilter(img_np, d=-1, sigmaColor=color_sigma, sigmaSpace=spatial_sigma)


def kuwahara_filter(img_np, radius: int = 3):
    """
    Apply Kuwahara Filter using vectorized NumPy and OpenCV boxFilter.
    Divides local window into 4 overlapping quadrants, calculates variance per quadrant,
    and assigns pixel color to the mean of the quadrant with minimum variance.
    """
    if not HAS_CV2_NUMPY:
        return img_np

    has_alpha = (img_np.shape[2] == 4)
    if has_alpha:
        alpha = img_np[:, :, 3]
        img_work = img_np[:, :, :3].astype(np.float32)
    else:
        img_work = img_np.astype(np.float32)

    h, w, c = img_work.shape

    def get_quadrant_stats(image, anchor):
        kernel_size = radius + 1
        mean = cv2.boxFilter(image, -1, (kernel_size, kernel_size), anchor=anchor, borderType=cv2.BORDER_REPLICATE)
        sq_mean = cv2.boxFilter(image**2, -1, (kernel_size, kernel_size), anchor=anchor, borderType=cv2.BORDER_REPLICATE)
        var = np.maximum(sq_mean - mean**2, 0)
        return mean, var.sum(axis=2)

    q1_m, q1_v = get_quadrant_stats(img_work, (radius, radius))
    q2_m, q2_v = get_quadrant_stats(img_work, (0, radius))
    q3_m, q3_v = get_quadrant_stats(img_work, (radius, 0))
    q4_m, q4_v = get_quadrant_stats(img_work, (0, 0))

    variances = np.stack([q1_v, q2_v, q3_v, q4_v], axis=-1)
    min_var_idx = np.argmin(variances, axis=-1)

    means = np.stack([q1_m, q2_m, q3_m, q4_m], axis=-2)

    idx_h, idx_w = np.indices((h, w))
    res_rgb = means[idx_h, idx_w, min_var_idx]
    res_rgb = np.clip(res_rgb, 0, 255).astype(np.uint8)

    if has_alpha:
        return np.dstack([res_rgb, alpha])
    return res_rgb


def guided_filter(p_np, guide_np=None, r: int = 2, eps: float = 0.01):
    """
    Apply Guided Filter (He et al.).
    Preserves bio-fluorescent boundaries while smoothing micro-noise.
    """
    if not HAS_CV2_NUMPY:
        return p_np

    if guide_np is None:
        guide_np = p_np

    has_alpha = (p_np.shape[2] == 4)
    if has_alpha:
        alpha = p_np[:, :, 3]
        p = p_np[:, :, :3].astype(np.float32)
        I = guide_np[:, :, :3].astype(np.float32)
    else:
        p = p_np.astype(np.float32)
        I = guide_np.astype(np.float32)

    win_size = (2 * r + 1, 2 * r + 1)

    def box_filter(img):
        return cv2.boxFilter(img, -1, win_size, borderType=cv2.BORDER_REPLICATE)

    mean_I = box_filter(I)
    mean_p = box_filter(p)
    corr_I = box_filter(I * I)
    corr_Ip = box_filter(I * p)

    var_I = corr_I - mean_I * mean_I
    cov_Ip = corr_Ip - mean_I * mean_p

    a = cov_Ip / (var_I + eps)
    b = mean_p - a * mean_I

    mean_a = box_filter(a)
    mean_b = box_filter(b)

    q = mean_a * I + mean_b
    res_rgb = np.clip(q, 0, 255).astype(np.uint8)

    if has_alpha:
        return np.dstack([res_rgb, alpha])
    return res_rgb
