from __future__ import annotations

import sys
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw


def key_green_screen(rgb: np.ndarray) -> np.ndarray:
    h, w = rgb.shape[:2]
    r = rgb[..., 0].astype(np.int16)
    g = rgb[..., 1].astype(np.int16)
    b = rgb[..., 2].astype(np.int16)

    def is_green(y: int, x: int) -> bool:
        rr, gg, bb = int(r[y, x]), int(g[y, x]), int(b[y, x])
        return gg > 90 and gg > rr + 45 and gg > bb + 25 and rr < 140

    bg = np.zeros((h, w), dtype=bool)
    q: deque[tuple[int, int]] = deque()
    for x in range(w):
        for y in (0, h - 1):
            if is_green(y, x):
                bg[y, x] = True
                q.append((y, x))
    for y in range(h):
        for x in (0, w - 1):
            if not bg[y, x] and is_green(y, x):
                bg[y, x] = True
                q.append((y, x))
    while q:
        y, x = q.popleft()
        for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0)):
            ny, nx = y + dy, x + dx
            if 0 <= ny < h and 0 <= nx < w and not bg[ny, nx] and is_green(ny, nx):
                bg[ny, nx] = True
                q.append((ny, nx))

    rgba = np.zeros((h, w, 4), dtype=np.uint8)
    rgba[..., :3] = rgb
    rgba[..., 3] = np.where(bg, 0, 255)
    rr = rgba[..., 0].astype(int)
    gg = rgba[..., 1].astype(int)
    bb = rgba[..., 2].astype(int)
    spill = (rgba[..., 3] > 0) & (gg > 70) & (gg > rr + 35) & (gg > bb + 18) & (rr < 120) & (bb < 160)
    top = np.zeros((h, w), dtype=bool)
    top[:90] = True
    spill |= top & (rgba[..., 3] > 0) & (gg > rr + 12) & (gg >= bb) & (gg > 28) & (bb < 160)
    rgba[spill, 3] = 0
    return rgba


def bezier_curve(p0: tuple[float, float], p1: tuple[float, float], p2: tuple[float, float], n: int = 56) -> list[tuple[float, float]]:
    pts: list[tuple[float, float]] = []
    for i in range(n + 1):
        t = i / n
        u = 1 - t
        x = u * u * p0[0] + 2 * u * t * p1[0] + t * t * p2[0]
        y = u * u * p0[1] + 2 * u * t * p1[1] + t * t * p2[1]
        pts.append((x, y))
    return pts


def curve_stays_opaque(
    pts: list[tuple[float, float]],
    base_a: np.ndarray,
) -> bool:
    h, w = base_a.shape[:2]
    for x, y in pts:
        xi, yi = int(round(x)), int(round(y))
        if xi < 0 or xi >= w or yi < 0 or yi >= h:
            return False
        if base_a[yi, xi, 3] < 200:
            return False
    return True


def draw_round_stroke(
    draw: ImageDraw.ImageDraw,
    pts: list[tuple[float, float]],
    width: int,
    fill: tuple[int, int, int, int],
) -> None:
    if len(pts) < 2:
        return
    cap = max(1, width // 2)
    for i in range(len(pts) - 1):
        draw.line([pts[i], pts[i + 1]], fill=fill, width=width)
    for p in (pts[0], pts[-1]):
        draw.ellipse(
            (p[0] - cap, p[1] - cap, p[0] + cap, p[1] + cap),
            fill=fill,
        )


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
    green_path = Path(r"C:\Users\Asus\.cursor\projects\d-Fractured-Chorus1\assets\ren_combat_battle_idle_semichibi_v2_green.jpg")
    full_out = Path(r"D:\Fractured-Chorus1\Assets\FracturedChorus\Art\Characters\Ren\Battle\ren_combat_battle_idle_semichibi_v2_full.png")
    battle_out = Path(r"D:\Fractured-Chorus1\Assets\FracturedChorus\Art\Characters\Ren\Battle\ren_combat_battle_idle_semichibi_v2.png")

    rgb = np.array(Image.open(green_path).convert("RGB"))
    base_a = key_green_screen(rgb)
    base = Image.fromarray(base_a, "RGBA")
    h, w = base_a.shape[:2]

    p0 = (354.0, 220.0)
    candidates = [
        ((358.0, 239.0), (375.0, 264.0)),
        ((359.0, 241.0), (376.0, 266.0)),
        ((359.0, 242.0), (378.0, 268.0)),
        ((360.0, 240.0), (378.0, 272.0)),
        ((359.0, 242.0), (382.0, 274.0)),
    ]
    pts: list[tuple[float, float]] | None = None
    for p1, p2 in candidates:
        cand = bezier_curve(p0, p1, p2, n=80)
        if curve_stays_opaque(cand, base_a):
            pts = cand
            break
    if pts is None:
        p1 = (358.0, 239.0)
        p2 = (375.0, 264.0)
        pts = bezier_curve(p0, p1, p2, n=80)

    trim_y = 250.0
    trimmed = [pts[0]]
    for p in pts[1:]:
        if p[1] > trim_y:
            break
        trimmed.append(p)
    if len(trimmed) < 2:
        trimmed = pts[: max(2, len(pts) // 2)]

    overlay = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay)
    draw_round_stroke(draw, trimmed, width=8, fill=(16, 18, 22, 255))
    draw_round_stroke(draw, trimmed, width=3, fill=(110, 118, 130, 255))

    im = Image.alpha_composite(base, overlay)
    im.save(full_out, "PNG")
    export_battle_canvas(im, battle_out)
    print(full_out)
    print(battle_out)
    return 0


if __name__ == "__main__":
    sys.exit(main())
