"""Cut character busts out of baked Clear combat cards.

Removes the blue diamond, name sticker, and empty bar slots.
Keeps dark uniforms (no flood into clothing).
Protects white hair by upper-canvas components only.
"""
from __future__ import annotations

import os

import cv2
import numpy as np
from PIL import Image, ImageDraw

SRC_DIR = r"F:\Unity_Project\Fractured Chorus\Assets\FracturedChorus\Art\UI\Combat\Characters"
OUT_DIR = os.path.join(SRC_DIR, "Avatars")
DEBUG_DIR = r"F:\Unity_Project\Fractured Chorus\Temp"

JOBS = [
    ("Ren_Clear_charCard.png", "ren_party_avatar_v1.png"),
    ("Coda_Clear_Card.png", "coda_party_avatar_v1.png"),
    ("Charlott_Clear_CharCard.png", "charlotte_party_avatar_v1.png"),
]


def dilate(mask: np.ndarray, radius: int) -> np.ndarray:
    k = max(1, radius * 2 + 1)
    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (k, k))
    return cv2.dilate(mask.astype(np.uint8), kernel, iterations=1).astype(bool)


def hair_mask(near_white: np.ndarray, y_ui: int) -> np.ndarray:
    num, labels, stats, _ = cv2.connectedComponentsWithStats(
        near_white.astype(np.uint8), connectivity=8
    )
    hair = np.zeros_like(near_white, dtype=bool)
    for i in range(1, num):
        ys, xs = np.where(labels == i)
        if ys.size == 0:
            continue
        above = np.count_nonzero(ys < y_ui)
        if above / ys.size >= 0.45 and ys.mean() < y_ui + 30:
            hair[labels == i] = True
    return hair


def extract_one(src_path: str, dst_path: str, debug_name: str) -> None:
    src = Image.open(src_path).convert("RGBA")
    arr = np.array(src)
    rgb = arr[:, :, :3].astype(np.int16)
    alpha = arr[:, :, 3]
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    mx = np.maximum(np.maximum(r, g), b)
    mn = np.minimum(np.minimum(r, g), b)
    opaque = alpha >= 16
    h, w = alpha.shape
    y_ui = int(h * 0.42)

    diamond = opaque & (b > 90) & (b > r + 28) & (b > g + 8) & ((b - np.maximum(r, g)) > 18)
    diamond = dilate(diamond, 1)

    near_white = opaque & (mx >= 198) & ((mx - mn) <= 44)
    near_black = opaque & (mx <= 50) & ((mx - mn) <= 22)

    hair = hair_mask(near_white, y_ui)
    name_white = near_white & ~hair
    name_white[:y_ui, :] = False
    # Name + bar slots sit in the left-to-center lower area.
    name_white[:, int(w * 0.82) :] = False

    dist = cv2.distanceTransform((~name_white).astype(np.uint8), cv2.DIST_L2, 5)
    outline = (dist <= 26) & near_black & opaque
    outline[: max(0, y_ui - 8), :] = False
    outline &= ~hair

    sticker = name_white | outline
    sticker = dilate(sticker, 3) & opaque & ~hair

    remove = diamond | sticker
    keep = opaque & ~remove

    bgr = cv2.cvtColor(arr[:, :, :3], cv2.COLOR_RGB2BGR)
    clothing = keep & ~dilate(diamond, 3)
    interior = sticker & dilate(clothing, 16) & ~diamond
    inpaint_mask = interior.astype(np.uint8) * 255
    inpainted = cv2.inpaint(bgr, inpaint_mask, 7, cv2.INPAINT_TELEA)
    rgb_out = cv2.cvtColor(inpainted, cv2.COLOR_BGR2RGB)
    out_mx = rgb_out.max(axis=2)
    out_mn = rgb_out.min(axis=2)
    reconstructed_sticker = ((out_mx <= 52) & ((out_mx - out_mn) <= 22)) | (
        (out_mx >= 200) & ((out_mx - out_mn) <= 40)
    )
    clothing_fill = interior & ~reconstructed_sticker

    out = np.zeros_like(arr)
    keep_final = keep | clothing_fill
    out[keep, :3] = arr[keep, :3]
    out[clothing_fill, :3] = rgb_out[clothing_fill]
    out[keep_final, 3] = 255
    silhouette = keep_final.astype(np.uint8) * 255
    blur = cv2.GaussianBlur(silhouette, (3, 3), 0)
    out[:, :, 3] = np.where(keep_final, 255, blur)

    image = Image.fromarray(out, "RGBA")
    bbox = image.getbbox()
    if bbox is None:
        raise RuntimeError(f"Empty extract: {src_path}")
    pad = 12
    l, t, rgt, btm = bbox
    cropped = image.crop((max(0, l - pad), max(0, t - pad), min(w, rgt + pad), min(h, btm + pad)))
    os.makedirs(os.path.dirname(dst_path), exist_ok=True)
    cropped.save(dst_path)

    vis = arr[:, :, :3].copy()
    vis[sticker] = (255, 24, 24)
    vis[diamond] = (40, 80, 255)
    vis[hair] = (32, 210, 72)
    os.makedirs(DEBUG_DIR, exist_ok=True)
    Image.fromarray(vis).resize((360, 360)).save(
        os.path.join(DEBUG_DIR, f"{debug_name}_mask.jpg"), quality=85
    )

    bg = Image.new("RGB", cropped.size, (180, 180, 180))
    d = ImageDraw.Draw(bg)
    cw, ch = cropped.size
    for yy in range(0, ch, 32):
        for xx in range(0, cw, 32):
            if ((xx // 32) + (yy // 32)) % 2 == 0:
                d.rectangle([xx, yy, xx + 32, yy + 32], fill=(120, 120, 120))
    composed = bg.convert("RGBA")
    composed.alpha_composite(cropped)
    composed.convert("RGB").resize((cw // 3, ch // 3)).save(
        os.path.join(DEBUG_DIR, f"{debug_name}_preview.jpg"), quality=85
    )
    print(
        f"wrote {os.path.basename(dst_path)} {cropped.size} "
        f"sticker={int(sticker.sum())} hair={int(hair.sum())} keep={int(keep.sum())}"
    )


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    for src_name, dst_name in JOBS:
        extract_one(
            os.path.join(SRC_DIR, src_name),
            os.path.join(OUT_DIR, dst_name),
            dst_name.replace("_party_avatar_v1.png", ""),
        )


if __name__ == "__main__":
    main()
