"""Compose mimi_character_icon_bars_elite_v1.png with letter-perfect labels."""
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(r"F:\Unity_Project\Fractured Chorus")
AVATAR = ROOT / "Assets/FracturedChorus/Art/UI/Combat/Characters/Avatars/mimi_enemy_avatar_v1.png"
OUT = ROOT / "Assets/FracturedChorus/Art/UI/Combat/Characters/mimi_character_icon_bars_elite_v1.png"

CANVAS = 1024
DIAMOND = (109, 8, 32, 255)
INK = (8, 8, 8, 255)
WHITE = (255, 255, 255, 255)


def crop_alpha(im: Image.Image, pad: int = 4) -> Image.Image:
    bbox = im.getbbox()
    if bbox is None:
        return im
    l, t, r, b = bbox
    l = max(0, l - pad)
    t = max(0, t - pad)
    r = min(im.width, r + pad)
    b = min(im.height, b + pad)
    return im.crop((l, t, r, b))


def load_font(size: int) -> ImageFont.FreeTypeFont:
    for path in (
        r"C:\Windows\Fonts\ariblk.ttf",
        r"C:\Windows\Fonts\arialbd.ttf",
        r"C:\Windows\Fonts\impact.ttf",
        str(ROOT / "Assets/FracturedChorus/Resources/Fonts/FcDisplayBoldItalic.ttf"),
    ):
        try:
            return ImageFont.truetype(path, size)
        except OSError:
            continue
    return ImageFont.load_default()


def draw_diamond(base: Image.Image) -> None:
    size = 560
    tile = Image.new("RGBA", (size, size), DIAMOND)
    tile = tile.rotate(45, expand=True, resample=Image.Resampling.BICUBIC)
    x = 740 - tile.width // 2
    y = 268 - tile.height // 2
    base.alpha_composite(tile, (x, y))


def paste_bust(base: Image.Image) -> None:
    bust = crop_alpha(Image.open(AVATAR).convert("RGBA"))
    target_h = 780
    scale = target_h / bust.height
    bust = bust.resize((int(bust.width * scale), target_h), Image.Resampling.LANCZOS)
    x = CANVAS - bust.width + 36
    y = 8
    base.alpha_composite(bust, (x, y))


def draw_name_stack(base: Image.Image) -> None:
    draw = ImageDraw.Draw(base)
    title_font = load_font(64)
    name_font = load_font(168)
    draw.text((42, 498), "The Pulse", font=title_font, fill=INK, anchor="lt")
    draw.text((34, 558), "Mimi", font=name_font, fill=INK, anchor="lt")

    bar_x, bar_w, bar_h, gap, stroke = 38, 640, 78, 18, 16
    bar_y = 760
    for i in range(2):
        y = bar_y + i * (bar_h + gap)
        draw.rectangle((bar_x, y, bar_x + bar_w, y + bar_h), fill=INK)
        draw.rectangle(
            (bar_x + stroke, y + stroke, bar_x + bar_w - stroke, y + bar_h - stroke),
            fill=WHITE,
        )


def main() -> None:
    canvas = Image.new("RGBA", (CANVAS, CANVAS), WHITE)
    draw_diamond(canvas)
    paste_bust(canvas)
    draw_name_stack(canvas)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(OUT, "PNG")
    print(f"Wrote {OUT} ({OUT.stat().st_size} bytes)")


if __name__ == "__main__":
    main()
