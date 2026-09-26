#!/usr/bin/env python3
"""
Background removal utility for Phagocyte sprites and assets.
Keys out outer dark (black) and light (white) background boxes using BFS floodfill
from canvas borders, setting connected background pixels to RGBA (0, 0, 0, 0).
Leaves interior dark/light pixels inside cells, gear, and icons intact.
"""

from collections import deque
import math
from typing import List, Tuple, Optional
from PIL import Image


def remove_background_outer_floodfill(
    img: Image.Image,
    tolerance: int = 28,
    seed_points: Optional[List[Tuple[int, int]]] = None
) -> Image.Image:
    """
    Keys out outer background via floodfill starting from canvas borders,
    replacing outer dark/light background pixels with RGBA(0, 0, 0, 0).
    """
    rgba = img.convert("RGBA")
    w, h = rgba.size
    pixels = rgba.load()

    if w == 0 or h == 0:
        return rgba

    # Determine reference seed colors from canvas corners
    corners = [
        pixels[0, 0][:3],
        pixels[w - 1, 0][:3],
        pixels[0, h - 1][:3],
        pixels[w - 1, h - 1][:3]
    ]

    # Calculate average corner RGB
    ref_r = sum(c[0] for c in corners) / 4.0
    ref_g = sum(c[1] for c in corners) / 4.0
    ref_b = sum(c[2] for c in corners) / 4.0

    is_dark_bg = (ref_r < 50 and ref_g < 50 and ref_b < 50)
    is_light_bg = (ref_r > 200 and ref_g > 200 and ref_b > 200)

    # Collect border seeds if none specified
    if not seed_points:
        seed_points = []
        for x in range(w):
            seed_points.append((x, 0))
            seed_points.append((x, h - 1))
        for y in range(h):
            seed_points.append((0, y))
            seed_points.append((w - 1, y))

    visited = set()
    queue = deque()

    def is_bg_pixel(r: int, g: int, b: int, a: int) -> bool:
        if a == 0:
            return True
        dist = math.sqrt((r - ref_r) ** 2 + (g - ref_g) ** 2 + (b - ref_b) ** 2)
        if dist <= tolerance:
            return True
        if is_dark_bg and (r <= tolerance and g <= tolerance and b <= tolerance):
            return True
        if is_light_bg and (r >= (255 - tolerance) and g >= (255 - tolerance) and b >= (255 - tolerance)):
            return True
        return False

    for p in seed_points:
        if p not in visited:
            px_val = pixels[p[0], p[1]]
            if is_bg_pixel(px_val[0], px_val[1], px_val[2], px_val[3]):
                queue.append(p)
                visited.add(p)

    directions = [(1, 0), (-1, 0), (0, 1), (0, -1)]

    while queue:
        cx, cy = queue.popleft()
        pixels[cx, cy] = (0, 0, 0, 0)

        for dx, dy in directions:
            nx, ny = cx + dx, cy + dy
            if 0 <= nx < w and 0 <= ny < h and (nx, ny) not in visited:
                visited.add((nx, ny))
                px_val = pixels[nx, ny]
                if is_bg_pixel(px_val[0], px_val[1], px_val[2], px_val[3]):
                    queue.append((nx, ny))

    return clean_edge_halos(rgba, is_dark_bg=is_dark_bg, is_light_bg=is_light_bg)


def clean_edge_halos(
    img: Image.Image,
    is_dark_bg: bool = True,
    is_light_bg: bool = False,
    max_color_val: int = 20
) -> Image.Image:
    """
    Strips semi-transparent dark or light halos/fringes along outer boundaries adjacent to transparent pixels.
    """
    rgba = img.convert("RGBA")
    w, h = rgba.size
    pixels = rgba.load()

    to_clear = []
    directions = [(1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, -1), (1, -1), (-1, 1)]

    for y in range(h):
        for x in range(w):
            r, g, b, a = pixels[x, y]
            if a > 0:
                is_halo = False
                if is_dark_bg and (r <= max_color_val and g <= max_color_val and b <= max_color_val):
                    is_halo = True
                elif is_light_bg and (r >= (255 - max_color_val) and g >= (255 - max_color_val) and b >= (255 - max_color_val)):
                    is_halo = True

                if is_halo:
                    for dx, dy in directions:
                        nx, ny = x + dx, y + dy
                        if 0 <= nx < w and 0 <= ny < h:
                            if pixels[nx, ny][3] == 0:
                                to_clear.append((x, y))
                                break

    for x, y in to_clear:
        pixels[x, y] = (0, 0, 0, 0)

    return rgba
