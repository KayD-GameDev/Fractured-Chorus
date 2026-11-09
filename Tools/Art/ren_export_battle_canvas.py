from __future__ import annotations

import sys
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image


def is_checker_grey(r: int, g: int, b: int) -> bool:
    if abs(r - g) > 14 or abs(g - b) > 14:
        return False
    return 190 <= r <= 246


def is_near_white(r: int, g: int, b: int) -> bool:
    return r >= 247 and g >= 247 and b >= 242 and abs(r - g) < 16 and abs(g - b) < 18


def flood_background(rgb: np.ndarray) -> np.ndarray:
    h, w = rgb.shape[:2]
    bg = np.zeros((h, w), dtype=bool)
    q: deque[tuple[int, int]] = deque()
    for x in range(w):
        for y in (0, h - 1):
            px = rgb[y, x]
            if is_checker_grey(int(px[0]), int(px[1]), int(px[2])):
                bg[y, x] = True
                q.append((y, x))
    for y in range(h):
        for x in (0, w - 1):
            if bg[y, x]:
                continue
            px = rgb[y, x]
            if is_checker_grey(int(px[0]), int(px[1]), int(px[2])):
                bg[y, x] = True
                q.append((y, x))
    while q:
        y, x = q.popleft()
        for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0)):
            ny, nx = y + dy, x + dx
            if ny < 0 or ny >= h or nx < 0 or nx >= w or bg[ny, nx]:
                continue
            px = rgb[ny, nx]
            if is_checker_grey(int(px[0]), int(px[1]), int(px[2])):
                bg[ny, nx] = True
                q.append((ny, nx))

    white = np.zeros((h, w), dtype=bool)
    for y in range(h):
        row = rgb[y]
        for x in range(w):
            if bg[y, x]:
                continue
            px = row[x]
            white[y, x] = is_near_white(int(px[0]), int(px[1]), int(px[2]))

    seen = np.zeros((h, w), dtype=bool)
    for y in range(h):
        for x in range(w):
            if not white[y, x] or seen[y, x]:
                continue
            comp: list[tuple[int, int]] = []
            touches = False
            qq: deque[tuple[int, int]] = deque([(y, x)])
            seen[y, x] = True
            while qq:
                cy, cx = qq.popleft()
                comp.append((cy, cx))
                for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0)):
                    ny, nx = cy + dy, cx + dx
                    if ny < 0 or ny >= h or nx < 0 or nx >= w:
                        touches = True
                        continue
                    if bg[ny, nx]:
                        touches = True
                    if white[ny, nx] and not seen[ny, nx]:
                        seen[ny, nx] = True
                        qq.append((ny, nx))
            if touches and len(comp) < 2500:
                for cy, cx in comp:
                    bg[cy, cx] = True

    r = rgb[..., 0].astype(np.int16)
    g = rgb[..., 1].astype(np.int16)
    b = rgb[..., 2].astype(np.int16)
    sat = np.maximum(np.maximum(np.abs(r - g), np.abs(g - b)), np.abs(r - b))
    core = (r < 176) | (sat > 26)
    prot = core.copy()
    for _ in range(22):
        prot[1:] |= prot[:-1]
        prot[:-1] |= prot[1:]
        prot[:, 1:] |= prot[:, :-1]
        prot[:, :-1] |= prot[:, 1:]
    light = (sat < 18) & (r > 198)
    bg |= light & (~prot)
    return bg


def export_battle_canvas(im: Image.Image, dst: Path, target_w: int = 554, canvas_h: int = 1247) -> None:
    bbox = im.getbbox()
    if bbox:
        im = im.crop(bbox)
    new_h = int(im.height * (target_w / im.width))
    im = im.resize((target_w, new_h), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (target_w, canvas_h), (0, 0, 0, 0))
    y_off = canvas_h - im.height - 40
    if y_off < 20:
        im = im.resize((target_w, canvas_h - 60), Image.Resampling.LANCZOS)
        y_off = 30
    canvas.paste(im, (0, y_off), im)
    dst.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(dst, "PNG")


def main() -> int:
    src = Path(sys.argv[1])
    dst = Path(sys.argv[2])
    rgb = np.array(Image.open(src).convert("RGB"))
    bg = flood_background(rgb)
    rgba = np.zeros((rgb.shape[0], rgb.shape[1], 4), dtype=np.uint8)
    rgba[..., :3] = rgb
    rgba[..., 3] = np.where(bg, 0, 255).astype(np.uint8)
    opaque = rgba[..., 3] > 0
    seen = np.zeros(opaque.shape, dtype=bool)
    h, w = opaque.shape
    for y in range(h):
        for x in range(w):
            if not opaque[y, x] or seen[y, x]:
                continue
            comp: list[tuple[int, int]] = []
            qq: deque[tuple[int, int]] = deque([(y, x)])
            seen[y, x] = True
            while qq:
                cy, cx = qq.popleft()
                comp.append((cy, cx))
                for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0)):
                    ny, nx = cy + dy, cx + dx
                    if ny < 0 or ny >= h or nx < 0 or nx >= w or seen[ny, nx] or not opaque[ny, nx]:
                        continue
                    seen[ny, nx] = True
                    qq.append((ny, nx))
            if len(comp) < 800:
                for cy, cx in comp:
                    rgba[cy, cx, 3] = 0
    full = Image.fromarray(rgba, "RGBA")
    full_path = dst.with_name(dst.stem + "_full.png")
    full.save(full_path, "PNG")
    export_battle_canvas(full, dst)
    print(full_path)
    print(dst)
    return 0


if __name__ == "__main__":
    sys.exit(main())
