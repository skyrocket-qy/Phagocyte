from typing import List, Optional
from PIL import Image

try:
    import numpy as np
    HAS_NUMPY = True
except ImportError:
    HAS_NUMPY = False


def check_corners(img_np) -> Optional[str]:
    """Check corner pixels for non-transparent / non-B/W content."""
    if not HAS_NUMPY or not hasattr(img_np, "shape"):
        return None

    h, w, c = img_np.shape
    if c != 4:
        return None

    corners = [(0, 0), (0, w - 1), (h - 1, 0), (h - 1, w - 1)]
    for y, x in corners:
        p = img_np[y, x]
        if p[3] < 10:
            continue

        is_black = all(v < 5 for v in p[:3])
        is_white = all(v > 250 for v in p[:3])
        if not (is_black or is_white):
            return f"[WARN] Corner pixel ({x}, {y}) is non-transparent/non-B/W (R{p[0]} G{p[1]} B{p[2]})"
    return None


def check_borders(img_np) -> Optional[str]:
    """Check for border leaks or full border fill."""
    if not HAS_NUMPY or not hasattr(img_np, "shape"):
        return None

    h, w, c = img_np.shape
    if c != 4:
        return None

    dirty = 0
    total = w * 2 + h * 2 - 4
    if total <= 0:
        return None

    def is_bg(p):
        if p[3] == 0:
            return True
        d_white = np.sqrt(np.sum((p[:3].astype(np.float32) - 255)**2))
        d_black = np.sqrt(np.sum(p[:3].astype(np.float32)**2))
        return d_white < 10 or d_black < 10

    for x in range(w):
        if not is_bg(img_np[0, x]):
            dirty += 1
        if h > 1 and not is_bg(img_np[h-1, x]):
            dirty += 1
    for y in range(1, h - 1):
        if not is_bg(img_np[y, 0]):
            dirty += 1
        if w > 1 and not is_bg(img_np[y, w-1]):
            dirty += 1

    ratio = dirty / total
    if 0.1 < ratio < 0.99:
        return f"[WARN] Border leaking ({ratio*100:.1f}% dirty): possible cropping issue"
    elif ratio >= 0.99:
        return "[WARN] Asset fully fills border"
    return None


def check_artifacts(img_np) -> Optional[str]:
    """Check for suspicious gray AI noise artifacts."""
    if not HAS_NUMPY or not hasattr(img_np, "shape"):
        return None

    h, w, c = img_np.shape
    if c != 4:
        return None

    opaque_mask = img_np[:, :, 3] >= 250
    opaque_pixels = img_np[opaque_mask]
    if len(opaque_pixels) == 0:
        return None

    max_v = np.max(opaque_pixels[:, :3], axis=1)
    min_v = np.min(opaque_pixels[:, :3], axis=1)
    diff = max_v - min_v

    suspicious = (diff < 10) & (max_v > 10) & (max_v < 245)
    gray_pixels = np.sum(suspicious)

    ratio = gray_pixels / (w * h)
    if ratio > 0.05:
        return f"[WARN] Possible AI artifacts: {ratio*100:.1f}% suspicious gray pixels"
    return None


def run_checks(img_np) -> List[str]:
    """Run all quality checks on an in-memory image context array."""
    issues = []
    if (res := check_corners(img_np)):
        issues.append(res)
    if (res := check_borders(img_np)):
        issues.append(res)
    if (res := check_artifacts(img_np)):
        issues.append(res)
    return issues
