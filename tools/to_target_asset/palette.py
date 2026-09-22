try:
    import numpy as np
    HAS_NUMPY = True
except ImportError:
    HAS_NUMPY = False

# Standard Palettes
if HAS_NUMPY:
    PALETTES = {
        "db32": np.array([
            [12, 12, 12], [34, 32, 52], [69, 40, 60], [102, 57, 49], [143, 86, 59], [223, 113, 38], [217, 160, 102], [238, 195, 154],
            [251, 242, 54], [153, 229, 80], [106, 190, 48], [55, 148, 110], [75, 105, 47], [82, 75, 36], [50, 60, 57], [63, 63, 116],
            [48, 96, 130], [91, 110, 225], [99, 155, 255], [95, 205, 228], [203, 219, 252], [244, 244, 244], [155, 173, 183], [132, 126, 135],
            [105, 106, 106], [89, 86, 82], [118, 66, 138], [172, 50, 50], [217, 87, 99], [215, 123, 186], [143, 151, 74], [138, 72, 54]
        ], dtype=np.uint8),
        # Microscopic Bio-Fluorescence Palette (GFP, RFP, DAPI, Hoechst, YFP, Membrane Void)
        "bio_fluo": np.array([
            [0, 0, 0], [10, 16, 26], [0, 255, 136], [46, 204, 113],
            [0, 210, 255], [52, 152, 219], [255, 51, 102], [231, 76, 60],
            [255, 215, 0], [243, 156, 18], [155, 89, 182], [236, 240, 241],
            [26, 188, 156], [22, 160, 133], [41, 128, 185], [192, 57, 43]
        ], dtype=np.uint8),
    }

    BAYER_4X4 = np.array([
        [0, 8, 2, 10],
        [12, 4, 14, 6],
        [3, 11, 1, 9],
        [15, 7, 13, 5]
    ], dtype=np.float32) / 16.0
else:
    PALETTES = {}
    BAYER_4X4 = None


def apply_palette(rgba_np, palette_name: str = "bio_fluo", dist_type: str = "perceptual"):
    """Quantizes colors in rgba_np to palette_name."""
    if not HAS_NUMPY or palette_name not in PALETTES:
        return rgba_np

    pal = PALETTES[palette_name]
    rgb = rgba_np[:, :, :3].astype(np.float32)
    h, w = rgb.shape[:2]
    flat_rgb = rgb.reshape(-1, 3)

    diff = flat_rgb[:, np.newaxis, :] - pal[np.newaxis, :, :].astype(np.float32)
    dist = np.sum(diff ** 2, axis=-1)
    nearest_idx = np.argmin(dist, axis=1)
    quant_rgb = pal[nearest_idx].reshape(h, w, 3)

    if rgba_np.shape[2] == 4:
        return np.dstack([quant_rgb, rgba_np[:, :, 3]])
    return quant_rgb


def apply_ordered_dither(rgba_np, palette_name: str = "bio_fluo", dist_type: str = "perceptual", strength: float = 0.5):
    """Applies Bayer 4x4 ordered dithering."""
    if not HAS_NUMPY or palette_name not in PALETTES:
        return rgba_np

    h, w = rgba_np.shape[:2]
    tile_h = int(np.ceil(h / 4))
    tile_w = int(np.ceil(w / 4))
    bayer = np.tile(BAYER_4X4, (tile_h, tile_w))[:h, :w]
    bayer_offset = (bayer - 0.5) * strength * 64.0

    dithered_rgb = np.clip(rgba_np[:, :, :3].astype(np.float32) + bayer_offset[:, :, np.newaxis], 0, 255).astype(np.uint8)
    if rgba_np.shape[2] == 4:
        dithered_input = np.dstack([dithered_rgb, rgba_np[:, :, 3]])
    else:
        dithered_input = dithered_rgb

    return apply_palette(dithered_input, palette_name=palette_name, dist_type=dist_type)
