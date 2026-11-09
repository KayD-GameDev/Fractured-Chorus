from __future__ import annotations

import sys
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image


def rgb_to_lab(rgb: np.ndarray) -> np.ndarray:
    rgb = rgb.astype(np.float64) / 255.0
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]

    def lin(u: np.ndarray) -> np.ndarray:
        return np.where(u <= 0.04045, u / 12.92, ((u + 0.055) / 1.055) ** 2.4)

    R, G, B = lin(r), lin(g), lin(b)
    X = R * 0.4124564 + G * 0.3575761 + B * 0.1804375
    Y = R * 0.2126729 + G * 0.7151522 + B * 0.0721750
    Z = R * 0.0193339 + G * 0.1191920 + B * 0.9503041
    X, Y, Z = X / 0.95047, Y / 1.00000, Z / 1.08883

    def f(t: np.ndarray) -> np.ndarray:
        return np.where(t > 0.008856, np.cbrt(t), (7.787 * t) + (16.0 / 116.0))

    fx, fy, fz = f(X), f(Y), f(Z)
    L = (116.0 * fy) - 16.0
    a = 500.0 * (fx - fy)
    b = 200.0 * (fy - fz)
    return np.stack([L, a, b], axis=-1)


def lab_to_rgb(lab: np.ndarray) -> np.ndarray:
    L, a, b = lab[..., 0], lab[..., 1], lab[..., 2]
    fy = (L + 16.0) / 116.0
    fx = fy + (a / 500.0)
    fz = fy - (b / 200.0)

    def finv(t: np.ndarray) -> np.ndarray:
        t3 = t**3
        return np.where(t3 > 0.008856, t3, (t - (16.0 / 116.0)) / 7.787)

    X, Y, Z = finv(fx) * 0.95047, finv(fy) * 1.0, finv(fz) * 1.08883
    R = X * 3.2404542 + Y * -1.5371385 + Z * -0.4985314
    G = X * -0.9692660 + Y * 1.8760108 + Z * 0.0415560
    B = X * 0.0556434 + Y * -0.2040259 + Z * 1.0572252

    def gamma(u: np.ndarray) -> np.ndarray:
        return np.where(u <= 0.0031308, 12.92 * u, 1.055 * (u ** (1.0 / 2.4)) - 0.055)

    R, G, B = np.clip(R, 0.0, None), np.clip(G, 0.0, None), np.clip(B, 0.0, None)
    rgb = np.stack([gamma(R), gamma(G), gamma(B)], axis=-1)
    return np.clip(rgb * 255.0, 0, 255).astype(np.uint8)


def is_checker_bg(r: int, g: int, b: int) -> bool:
    if r < 35 and g < 35 and b < 35:
        return True
    if abs(r - g) > 10 or abs(g - b) > 10:
        return False
    if r > 248 and g > 248 and b > 248:
        return True
    return 208 <= r <= 235 and 208 <= g <= 235 and 208 <= b <= 238


def flood_background(rgb: np.ndarray) -> np.ndarray:
    h, w = rgb.shape[:2]
    bg = np.zeros((h, w), dtype=bool)
    q: deque[tuple[int, int]] = deque()
    for x in range(w):
        for y in (0, h - 1):
            if is_checker_bg(int(rgb[y, x, 0]), int(rgb[y, x, 1]), int(rgb[y, x, 2])):
                bg[y, x] = True
                q.append((y, x))
    for y in range(h):
        for x in (0, w - 1):
            if not bg[y, x] and is_checker_bg(int(rgb[y, x, 0]), int(rgb[y, x, 1]), int(rgb[y, x, 2])):
                bg[y, x] = True
                q.append((y, x))
    while q:
        y, x = q.popleft()
        for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0)):
            ny, nx = y + dy, x + dx
            if ny < 0 or ny >= h or nx < 0 or nx >= w or bg[ny, nx]:
                continue
            px = rgb[ny, nx]
            if is_checker_bg(int(px[0]), int(px[1]), int(px[2])):
                bg[ny, nx] = True
                q.append((ny, nx))
    return bg


def roi_mask(h: int, w: int, y0: float, y1: float, x0: float, x1: float) -> np.ndarray:
    m = np.zeros((h, w), dtype=bool)
    m[int(h * y0) : int(h * y1), int(w * x0) : int(w * x1)] = True
    return m


def face_skin_mask(rgb: np.ndarray, region: np.ndarray) -> np.ndarray:
    r = rgb[..., 0].astype(np.float64)
    g = rgb[..., 1].astype(np.float64)
    b = rgb[..., 2].astype(np.float64)
    y = 0.299 * r + 0.587 * g + 0.114 * b
    maxc = np.maximum(np.maximum(r, g), b)
    minc = np.minimum(np.minimum(r, g), b)
    sat = maxc - minc
    not_line = y > 72
    not_hair = y > 85
    not_white_cloth = ~((r > 238) & (g > 238) & (b > 232))
    not_accent = sat < 58
    return region & not_line & not_hair & not_white_cloth & not_accent


def transfer_skin(
    src_rgb: np.ndarray,
    ref_rgb: np.ndarray,
    src_mask: np.ndarray,
    ref_mask: np.ndarray,
) -> np.ndarray:
    src_lab = rgb_to_lab(src_rgb)
    ref_lab = rgb_to_lab(ref_rgb)
    out = src_lab.copy()
    for ch in range(3):
        s = src_lab[..., ch][src_mask]
        t = ref_lab[..., ch][ref_mask]
        if s.size < 64 or t.size < 64:
            raise RuntimeError("Skin ROI too small.")
        s_mean, s_std = float(s.mean()), float(max(s.std(), 1.2))
        t_mean, t_std = float(t.mean()), float(max(t.std(), 1.2))
        mapped = (src_lab[..., ch] - s_mean) * (t_std / s_std) + t_mean
        out[..., ch] = np.where(src_mask, mapped, out[..., ch])
    return lab_to_rgb(out)


def to_rgba(rgb: np.ndarray, bg: np.ndarray) -> Image.Image:
    rgba = np.zeros((rgb.shape[0], rgb.shape[1], 4), dtype=np.uint8)
    rgba[..., :3] = rgb
    rgba[..., 3] = np.where(bg, 0, 255).astype(np.uint8)
    return Image.fromarray(rgba, "RGBA")


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
    semi_path = Path(
        r"C:\Users\Asus\.cursor\projects\d-Question\assets\ren_combat_battle_idle_semichibi_v2.jpg"
    )
    idle_path = Path(
        r"D:\Fractured-Chorus1\Assets\FracturedChorus\Art\Characters\Ren\Battle\ren_combat_battle_idle_v2.png"
    )
    out_path = Path(
        r"D:\Fractured-Chorus1\Assets\FracturedChorus\Art\Characters\Ren\Battle\ren_combat_battle_idle_semichibi_v2.png"
    )
    out_full = out_path.with_name("ren_combat_battle_idle_semichibi_v2_full.png")

    semi = np.array(Image.open(semi_path).convert("RGB"))
    idle_im = Image.open(idle_path).convert("RGBA")
    idle_rgb = np.array(idle_im.convert("RGB"))
    if semi.shape[:2] != idle_rgb.shape[:2]:
        idle_rgb = np.array(
            idle_im.resize((semi.shape[1], semi.shape[0]), Image.Resampling.LANCZOS).convert("RGB")
        )

    h, w = semi.shape[:2]
    face_src = roi_mask(h, w, 0.10, 0.36, 0.20, 0.82)
    face_ref = roi_mask(h, w, 0.12, 0.44, 0.22, 0.78)
    hand_src = roi_mask(h, w, 0.40, 0.72, 0.05, 0.95) & (np.arange(h)[:, None] > int(h * 0.38))
    hand_ref = roi_mask(h, w, 0.42, 0.78, 0.08, 0.92)

    src_mask = face_skin_mask(semi, face_src | hand_src)
    ref_mask = face_skin_mask(idle_rgb, face_ref | hand_ref)

    matched = transfer_skin(semi, idle_rgb, src_mask, ref_mask)
    bg = flood_background(semi)
    rgba = to_rgba(matched, bg)
    rgba.save(out_full, "PNG")
    export_battle_canvas(rgba, out_path)
    print(f"src_skin={int(src_mask.sum())} ref_skin={int(ref_mask.sum())}")
    print(out_full)
    print(out_path)
    return 0


if __name__ == "__main__":
    sys.exit(main())
