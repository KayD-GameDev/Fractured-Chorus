"""Key generated enemy-card busts to alpha and write Unity-ready PNGs."""
from __future__ import annotations

import os
import sys

import cv2
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(__file__))
from process_party_card_art import crop_alpha, flood_key_white, save_rgba

SRC = r"C:\Users\admin\.cursor\projects\f-Unity-Project-Fractured-Chorus\assets"
DST = r"F:\Unity_Project\Fractured Chorus\Assets\FracturedChorus\Art\UI\Combat\Characters\Avatars"


def process(src_name: str, dst_name: str, flip: bool) -> None:
    rgb = np.array(Image.open(os.path.join(SRC, src_name)).convert("RGB"))
    if flip:
        rgb = np.fliplr(rgb)
    alpha = flood_key_white(rgb, luma_min=230)
    rgba = np.dstack([rgb, alpha])
    rgba = crop_alpha(rgba, pad=10)
    save_rgba(rgba, os.path.join(DST, dst_name))


def main() -> None:
    os.makedirs(DST, exist_ok=True)
    process("astra_enemy_avatar_v1.png", "astra_enemy_avatar_v1.png", flip=False)
    process("kiki_enemy_avatar_v1.png", "kiki_enemy_avatar_v1.png", flip=False)
    process("astra_mic_enemy_avatar_v1.png", "astra_mic_enemy_avatar_v1.png", flip=False)
    process("astra_eye_enemy_avatar_v1.png", "astra_eye_enemy_avatar_v1.png", flip=True)


if __name__ == "__main__":
    main()
