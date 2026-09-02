"""Key generated party-card art to alpha and write Unity-ready PNGs."""
from __future__ import annotations

import os
from collections import deque

import cv2
import numpy as np
from PIL import Image, ImageDraw

SRC = r"C:\Users\admin\.cursor\projects\f-Unity-Project-Fractured-Chorus\assets"
AVATAR_DST = r"F:\Unity_Project\Fractured Chorus\Assets\FracturedChorus\Art\UI\Combat\Characters\Avatars"
CHROME_DST = r"F:\Unity_Project\Fractured Chorus\Assets\FracturedChorus\Art\UI\Combat\PartyCard"
DEBUG = r"F:\Unity_Project\Fractured Chorus\Temp"


def flood_key_white(rgb: np.ndarray, luma_min: int = 228, chroma_max: int = 18, tol: int = 36) -> np.ndarray:
    """Flood from image edges through near-white background. Returns keep mask."""
    h, w = rgb.shape[:2]
    mx = rgb.max(axis=2)
    mn = rgb.min(axis=2)
    is_bg = (mx >= luma_min) & ((mx - mn) <= chroma_max)
    visited = np.zeros((h, w), dtype=bool)
    q: deque[tuple[int, int]] = deque()
    for x in range(w):
        for y in (0, h - 1):
            if is_bg[y, x] and not visited[y, x]:
                visited[y, x] = True
                q.append((x, y))
    for y in range(h):
        for x in (0, w - 1):
            if is_bg[y, x] and not visited[y, x]:
                visited[y, x] = True
                q.append((x, y))
    while q:
        x, y = q.popleft()
        for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < w and 0 <= ny < h and not visited[ny, nx] and is_bg[ny, nx]:
                visited[ny, nx] = True
                q.append((nx, ny))
    keep = ~visited
    # Grow keep 1px then erode bg so hair tips aren't eaten.
    keep_u8 = keep.astype(np.uint8) * 255
    keep_u8 = cv2.dilate(keep_u8, np.ones((3, 3), np.uint8), 1)
    alpha = keep_u8
    # Soft edge
    alpha = cv2.GaussianBlur(alpha, (3, 3), 0)
    return alpha


def crop_alpha(rgba: np.ndarray, pad: int = 8) -> np.ndarray:
    alpha = rgba[:, :, 3]
    ys, xs = np.where(alpha > 12)
    if xs.size == 0:
        return rgba
    l, r = max(0, xs.min() - pad), min(rgba.shape[1], xs.max() + 1 + pad)
    t, b = max(0, ys.min() - pad), min(rgba.shape[0], ys.max() + 1 + pad)
    return rgba[t:b, l:r]


def save_rgba(arr: np.ndarray, path: str) -> None:
    os.makedirs(os.path.dirname(path), exist_ok=True)
    Image.fromarray(arr, "RGBA").save(path)
    print("wrote", path, arr.shape[1], "x", arr.shape[0])


def process_avatar(src_name: str, dst_name: str, luma_min: int) -> None:
    rgb = np.array(Image.open(os.path.join(SRC, src_name)).convert("RGB"))
    alpha = flood_key_white(rgb, luma_min=luma_min)
    rgba = np.dstack([rgb, alpha])
    rgba = crop_alpha(rgba, pad=10)
    save_rgba(rgba, os.path.join(AVATAR_DST, dst_name))
    preview = Image.fromarray(rgba, "RGBA").resize((rgba.shape[1] // 3, rgba.shape[0] // 3))
    preview.convert("RGB").save(os.path.join(DEBUG, dst_name.replace(".png", "_keyed.jpg")), quality=85)


def process_jagged_bg() -> None:
    rgb = np.array(Image.open(os.path.join(SRC, "party_card_bg_jagged_v1.png")).convert("RGB"))
    alpha = flood_key_white(rgb, luma_min=220, chroma_max=22)
    # Keep only dark polygon pixels with alpha.
    luma = rgb.mean(axis=2)
    keep = (alpha > 20) & (luma < 80)
    out = np.zeros((rgb.shape[0], rgb.shape[1], 4), dtype=np.uint8)
    out[keep, :3] = rgb[keep]
    out[:, :, 3] = np.where(keep, 255, 0).astype(np.uint8)
    # Close small holes
    closed = cv2.morphologyEx(out[:, :, 3], cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8))
    out[:, :, 3] = closed
    out[closed > 0, :3] = (10, 10, 14)
    out = crop_alpha(out, pad=6)
    save_rgba(out, os.path.join(CHROME_DST, "party_card_bg_jagged_v1.png"))


def process_diamond() -> None:
    rgb = np.array(Image.open(os.path.join(SRC, "party_card_accent_diamond_v1.png")).convert("RGB"))
    alpha = flood_key_white(rgb, luma_min=230, chroma_max=20)
    rgba = np.dstack([rgb, alpha])
    # Force diamond fill to brand cyan-blue so Inspector tint works from a known base.
    keep = alpha > 40
    rgba[keep, 0] = 46
    rgba[keep, 1] = 109
    rgba[keep, 2] = 255
    rgba[keep, 3] = 255
    rgba = crop_alpha(rgba, pad=4)
    save_rgba(rgba, os.path.join(CHROME_DST, "party_card_accent_diamond_v1.png"))


def draw_bar_track() -> None:
    w, h = 512, 72
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    # Slight parallelogram tilt.
    outer = [(18, 14), (502, 8), (494, 58), (10, 64)]
    inner = [(28, 22), (490, 16), (484, 50), (22, 56)]
    draw.polygon(outer, fill=(18, 20, 28, 255))
    draw.polygon(inner, fill=(0, 0, 0, 0))
    draw.line(inner + [inner[0]], fill=(236, 244, 255, 255), width=3)
    # The inner hole must stay transparent — redraw by compositing.
    out = np.array(img)
    # Punch inner polygon to transparent.
    mask = Image.new("L", (w, h), 0)
    ImageDraw.Draw(mask).polygon(inner, fill=255)
    hole = np.array(mask) > 0
    out[hole, 3] = 0
    save_rgba(out, os.path.join(CHROME_DST, "party_card_bar_track_v1.png"))


def main():
    os.makedirs(AVATAR_DST, exist_ok=True)
    os.makedirs(CHROME_DST, exist_ok=True)
    os.makedirs(DEBUG, exist_ok=True)
    process_avatar("ren_party_avatar_v1.png", "ren_party_avatar_v1.png", luma_min=232)
    process_avatar("coda_party_avatar_v1.png", "coda_party_avatar_v1.png", luma_min=238)
    process_avatar("charlotte_party_avatar_v1.png", "charlotte_party_avatar_v1.png", luma_min=232)
    process_jagged_bg()
    process_diamond()
    draw_bar_track()


if __name__ == "__main__":
    main()
