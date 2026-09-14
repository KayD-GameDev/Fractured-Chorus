"""Flood-key black backgrounds on Astra Stage TV Cadence busts. Seed top+sides only."""
from __future__ import annotations

import os
from collections import deque

import cv2
import numpy as np
from PIL import Image

SRC = r"C:\Users\admin\.cursor\projects\f-Unity-Project-Fractured-Chorus\assets"
ART = r"F:\Unity_Project\Fractured Chorus\Assets\FracturedChorus\Art\UI\Combat\Boss\Astra\StageTv"
RES = r"F:\Unity_Project\Fractured Chorus\Assets\FracturedChorus\Resources\UI\Combat\Boss\Astra\StageTv"

FACES = (
    "astra_tv_face_joy_v1.png",
    "astra_tv_face_anger_v1.png",
    "astra_tv_face_love_v1.png",
    "astra_tv_face_hate_v1.png",
    "astra_tv_face_sorrow_v1.png",
)


def flood_key_black(rgb: np.ndarray, luma_max: int = 14, chroma_max: int = 10) -> np.ndarray:
    h, w = rgb.shape[:2]
    mx = rgb.max(axis=2)
    mn = rgb.min(axis=2)
    is_bg = (mx <= luma_max) & ((mx - mn) <= chroma_max)
    visited = np.zeros((h, w), dtype=bool)
    q: deque[tuple[int, int]] = deque()

    def seed(x: int, y: int) -> None:
        if is_bg[y, x] and not visited[y, x]:
            visited[y, x] = True
            q.append((x, y))

    for x in range(w):
        seed(x, 0)
        seed(x, 1)
    for y in range(h):
        seed(0, y)
        seed(1, y)
        seed(w - 1, y)
        seed(w - 2, y)

    while q:
        x, y = q.popleft()
        for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < w and 0 <= ny < h and not visited[ny, nx] and is_bg[ny, nx]:
                visited[ny, nx] = True
                q.append((nx, ny))

    keep = (~visited).astype(np.uint8) * 255
    keep = cv2.dilate(keep, np.ones((3, 3), np.uint8), 1)
    return cv2.GaussianBlur(keep, (3, 3), 0)


def to_square_1024(rgba: np.ndarray) -> np.ndarray:
    alpha = rgba[:, :, 3]
    ys, xs = np.where(alpha > 12)
    if xs.size == 0:
        img = Image.fromarray(rgba, "RGBA")
        return np.array(img.resize((1024, 1024), Image.Resampling.LANCZOS))

    pad = 12
    l, r = max(0, int(xs.min()) - pad), min(rgba.shape[1], int(xs.max()) + 1 + pad)
    t, b = max(0, int(ys.min()) - pad), min(rgba.shape[0], int(ys.max()) + 1 + pad)
    crop = rgba[t:b, l:r]
    h, w = crop.shape[:2]
    side = max(h, w)
    canvas = np.zeros((side, side, 4), dtype=np.uint8)
    y0 = (side - h) // 2
    x0 = (side - w) // 2
    canvas[y0 : y0 + h, x0 : x0 + w] = crop
    img = Image.fromarray(canvas, "RGBA")
    return np.array(img.resize((1024, 1024), Image.Resampling.LANCZOS))


def process(name: str) -> None:
    src = os.path.join(SRC, name)
    rgb = np.array(Image.open(src).convert("RGB"))
    alpha = flood_key_black(rgb)
    rgba = np.dstack([rgb, alpha])
    rgba = to_square_1024(rgba)
    for dst_dir in (ART, RES):
        os.makedirs(dst_dir, exist_ok=True)
        path = os.path.join(dst_dir, name)
        Image.fromarray(rgba, "RGBA").save(path)
        print("wrote", path, "alpha%", int((rgba[:, :, 3] > 12).mean() * 100))


def main() -> None:
    for name in FACES:
        process(name)


if __name__ == "__main__":
    main()
