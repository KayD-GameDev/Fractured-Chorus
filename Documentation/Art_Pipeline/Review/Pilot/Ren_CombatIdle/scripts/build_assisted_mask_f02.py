#!/usr/bin/env python3
from __future__ import annotations

from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[6]
F1 = ROOT / "Assets/FracturedChorus/Art/Characters/Ren/Battle/ren_combat_battle_idle_semichibi_v2_full.png"
OUT_DIR = Path(__file__).resolve().parent.parent
OUT_MASK = OUT_DIR / "ren_combat_idle_fbf_pilot_mask_f02_ASSISTED.png"
OUT_PREVIEW = OUT_DIR / "ren_combat_idle_fbf_pilot_mask_f02_ASSISTED_preview.png"
W, H = 864, 1152


def main() -> None:
    im = Image.open(F1).convert("RGBA")
    arr = np.array(im)
    rgb = arr[:, :, :3].astype(np.float32)
    a = arr[:, :, 3]
    opaque = a > 200

    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    mx = np.maximum(np.maximum(r, g), b)
    mn = np.minimum(np.minimum(r, g), b)
    is_line = (mx < 42) & (mn < 12)
    is_shirt = (g > 130) & (r > 90) & (b > 90)
    is_cyan = (g > r + 20) & (b > r)
    is_violet = (b > 105) & (r > 65) & (g < 95)

    blazer = (
        opaque
        & ~is_line
        & ~is_shirt
        & ~is_cyan
        & ~is_violet
        & (b >= r - 3)
        & (mx < 118)
        & (mn > 13)
    )

    seeds = Image.new("L", (W, H), 0)
    dr = ImageDraw.Draw(seeds)
    dr.pieslice([248, 318, 402, 548], 200, 340, fill=255)
    dr.ellipse([462, 148, 552, 228], fill=255)
    dr.rectangle([318, 368, 378, 498], fill=255)

    seeds_a = np.array(seeds) > 0
    allowed = blazer & seeds_a

    lock = Image.new("L", (W, H), 0)
    ldr = ImageDraw.Draw(lock)
    ldr.rectangle([395, 248, 545, 430], fill=255)
    ldr.rectangle([248, 530, 864, H], fill=255)
    ldr.rectangle([0, 0, W, 118], fill=255)
    ldr.rectangle([545, 300, W, 900], fill=255)
    ldr.rectangle([235, 430, 305, 720], fill=255)
    lock_a = np.array(lock) > 0
    allowed &= ~lock_a

    m = Image.fromarray((allowed * 255).astype(np.uint8), "L")
    m = m.filter(ImageFilter.MinFilter(3))
    mask_u8 = np.where(np.array(m) > 127, 255, 0).astype(np.uint8)
    Image.fromarray(mask_u8, "L").save(OUT_MASK)

    prev = im.copy()
    ov = np.zeros((H, W, 4), np.uint8)
    sel = mask_u8 == 255
    ov[sel] = [75, 235, 115, 130]
    Image.alpha_composite(prev, Image.fromarray(ov, "RGBA")).save(OUT_PREVIEW)

    white = int(sel.sum())
    print(f"Wrote {OUT_MASK}")
    print(f"Preview {OUT_PREVIEW}")
    print(f"white pixels: {white} ({100.0 * white / (W * H):.4f}%)")


if __name__ == "__main__":
    main()
