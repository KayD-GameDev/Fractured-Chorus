"""Generate compact party-card chrome: curved side tubes + jagged bg."""
from __future__ import annotations

import os
import shutil

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = os.path.join(ROOT, "Assets", "FracturedChorus", "Art", "UI", "Combat", "PartyCard")
RES = os.path.join(ROOT, "Assets", "FracturedChorus", "Resources", "UI", "Combat", "PartyCard")


def downsample(mask_hi: np.ndarray, aa: int) -> np.ndarray:
    h, w = mask_hi.shape
    return mask_hi.reshape(h // aa, aa, w // aa, aa).mean(axis=(1, 3))


def curved_tube_mask(width: int, height: int, inset: float = 0.0, aa: int = 4) -> np.ndarray:
    w_hi, h_hi = width * aa, height * aa
    inset_px = inset * aa
    thickness = max(6.0, w_hi * 0.58 - 2.0 * inset_px)
    radius = h_hi * 0.78
    cx = w_hi * 0.52 - radius
    cy = h_hi * 0.5
    max_dy = h_hi * 0.5 - thickness * 0.5 - 3.0 * aa
    theta_max = float(np.arcsin(np.clip(max_dy / radius, 0.05, 0.95)))
    r_outer = radius + thickness * 0.5
    r_inner = radius - thickness * 0.5

    ys, xs = np.mgrid[0:h_hi, 0:w_hi]
    dx = xs.astype(np.float32) - cx
    dy = ys.astype(np.float32) - cy
    dist = np.sqrt(dx * dx + dy * dy)
    ang = np.arctan2(dy, dx)
    body = (dist >= r_inner) & (dist <= r_outer) & (np.abs(ang) <= theta_max)

    cap_r = thickness * 0.5
    for sign in (-1.0, 1.0):
        cap_x = cx + radius * np.cos(sign * theta_max)
        cap_y = cy + radius * np.sin(sign * theta_max)
        body |= (xs - cap_x) ** 2 + (ys - cap_y) ** 2 <= cap_r ** 2

    return downsample(body.astype(np.float32), aa)


def save_rgba(path: str, arr: np.ndarray) -> None:
    os.makedirs(os.path.dirname(path), exist_ok=True)
    Image.fromarray(arr, "RGBA").save(path)
    print("wrote", path, arr.shape[1], "x", arr.shape[0])


def write_tube_track(path: str) -> None:
    width, height = 80, 320
    outer = curved_tube_mask(width, height, inset=0.0)
    inner = curved_tube_mask(width, height, inset=5.5)
    rim = np.clip(outer - inner, 0.0, 1.0)
    edge = np.clip(outer - curved_tube_mask(width, height, inset=1.8), 0.0, 1.0)

    rgba = np.zeros((height, width, 4), dtype=np.uint8)
    body_a = (outer * 255).astype(np.uint8)
    rgba[..., 0] = 16
    rgba[..., 1] = 18
    rgba[..., 2] = 26
    rgba[..., 3] = body_a

    rim_a = rim > 0.12
    rgba[rim_a, 0] = 18
    rgba[rim_a, 1] = 20
    rgba[rim_a, 2] = 28
    rgba[rim_a, 3] = 255

    glow = edge > 0.18
    rgba[glow, 0] = 236
    rgba[glow, 1] = 244
    rgba[glow, 2] = 255
    rgba[glow, 3] = np.clip((edge[glow] * 255).astype(np.int32), 0, 255).astype(np.uint8)
    save_rgba(path, rgba)


def write_tube_fill(path: str) -> None:
    width, height = 80, 320
    fill = curved_tube_mask(width, height, inset=6.0)
    rgba = np.zeros((height, width, 4), dtype=np.uint8)
    rgba[..., 0] = 255
    rgba[..., 1] = 255
    rgba[..., 2] = 255
    rgba[..., 3] = (fill * 255).astype(np.uint8)
    save_rgba(path, rgba)


def jagged_card_silhouette(width: int, height: int) -> np.ndarray:
    """Portrait disc on the left + two tube lobes on the right, jagged P5 rim."""
    mask = Image.new("L", (width, height), 0)
    draw = ImageDraw.Draw(mask)

    pad = int(height * 0.06)
    disc_r = int(height * 0.39)
    cx = pad + disc_r + 6
    cy = int(height * 0.46)

    draw.ellipse([cx - disc_r, cy - disc_r, cx + disc_r, cy + disc_r], fill=255)

    tube_h = int(disc_r * 1.92)
    tube_top = cy - tube_h // 2
    tube_bot = cy + tube_h // 2
    lobe_w = int(width * 0.11)
    x0 = cx + disc_r - 18
    for i in range(2):
        x = x0 + i * (lobe_w + 10)
        draw.rounded_rectangle(
            [x, tube_top + 8, x + lobe_w, tube_bot - 8],
            radius=lobe_w // 2,
            fill=255,
        )

    # Jagged spikes around a bean outline.
    spikes = Image.new("L", (width, height), 0)
    sd = ImageDraw.Draw(spikes)
    n = 28
    bean_cx = cx + disc_r * 0.18
    bean_cy = cy
    rx = disc_r + int(width * 0.16)
    ry = disc_r + 8
    pts = []
    for i in range(n):
        t = (i / n) * np.pi * 2.0
        jag = 1.0 + (0.10 if i % 2 == 0 else -0.06)
        if 0.08 * np.pi < t < 1.15 * np.pi:
            jag += 0.04
        px = bean_cx + np.cos(t) * rx * jag
        py = bean_cy + np.sin(t) * ry * jag
        pts.append((px, py))
    sd.polygon(pts, fill=255)
    combined = Image.composite(spikes, mask, spikes)
    combined = combined.filter(ImageFilter.MaxFilter(5))
    combined = combined.filter(ImageFilter.MinFilter(3))
    return np.array(combined).astype(np.float32) / 255.0


def write_jagged_bg(path: str) -> None:
    width, height = 640, 472
    sil = jagged_card_silhouette(width, height)
    rgba = np.zeros((height, width, 4), dtype=np.uint8)
    keep = sil > 0.12
    rgba[keep, 0] = 10
    rgba[keep, 1] = 10
    rgba[keep, 2] = 14
    rgba[keep, 3] = np.clip(sil[keep] * 255.0, 0, 255).astype(np.uint8)

    # Thin cyan-white edge like the old track chrome.
    sil_img = Image.fromarray((sil * 255).astype(np.uint8), "L")
    dil = sil_img.filter(ImageFilter.MaxFilter(5))
    ero = sil_img.filter(ImageFilter.MinFilter(5))
    edge = np.array(dil, dtype=np.int16) - np.array(ero, dtype=np.int16)
    edge_m = edge > 18
    rgba[edge_m, 0] = 42
    rgba[edge_m, 1] = 48
    rgba[edge_m, 2] = 62
    rgba[edge_m, 3] = np.clip(rgba[edge_m, 3].astype(np.int32) + 80, 0, 255).astype(np.uint8)
    save_rgba(path, rgba)


def copy_to_resources(name: str) -> None:
    src = os.path.join(ART, name)
    dst = os.path.join(RES, name)
    os.makedirs(RES, exist_ok=True)
    shutil.copy2(src, dst)
    print("copied", dst)


def main() -> None:
    os.makedirs(ART, exist_ok=True)
    write_tube_track(os.path.join(ART, "party_card_side_tube_track_v1.png"))
    write_tube_fill(os.path.join(ART, "party_card_side_tube_fill_v1.png"))
    write_jagged_bg(os.path.join(ART, "party_card_bg_jagged_v1.png"))
    for name in (
        "party_card_side_tube_track_v1.png",
        "party_card_side_tube_fill_v1.png",
        "party_card_bg_jagged_v1.png",
    ):
        copy_to_resources(name)


if __name__ == "__main__":
    main()
