"""A1 poster using sharp menu fullbody sprites."""
from PIL import Image, ImageDraw, ImageFilter, ImageFont
import cv2
import numpy as np

BG = r"C:\Users\admin\.cursor\projects\f-Unity-Project-Fractured-Chorus\assets\fc_poster_bg_empty.png"
LOGO = r"C:\Users\admin\.cursor\projects\f-Unity-Project-Fractured-Chorus\assets\c__Users_admin_AppData_Roaming_Cursor_User_workspaceStorage_8f918c66735690560589d82aa54e5d70_images_image-63a22129-08cf-4f1d-a6e3-741c39d7f075.png"
TITLE = r"F:\Unity_Project\Fractured Chorus\Assets\FracturedChorus\Art\UI\TitleScreen\SheetV1\logo_fractured_chorus_v2_alpha.png"
CHAR = r"F:\Unity_Project\Fractured Chorus\Assets\FracturedChorus\Art\Characters"
OUT = r"F:\Unity_Project\Fractured Chorus\docs\promo\Fractured-Chorus-Poster-A1.png"

W, H = 3508, 4967
FOOTER_H = 560
NAVY = (26, 42, 92, 255)
NAVY_SOFT = (70, 88, 140, 255)
FONT_B = r"C:\Windows\Fonts\segoeuib.ttf"
FONT_R = r"C:\Windows\Fonts\segoeui.ttf"


def key_white_sprite(path):
    bgr = cv2.imread(path, cv2.IMREAD_COLOR)
    h, w = bgr.shape[:2]
    mask = np.zeros((h + 2, w + 2), np.uint8)
    flags = cv2.FLOODFILL_MASK_ONLY | (255 << 8) | 4
    seeds = [(0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1), (w // 2, 2), (2, h // 2), (w - 3, h // 2)]
    for s in seeds:
        cv2.floodFill(bgr, mask, s, (0, 0, 0), (8, 8, 8), (8, 8, 8), flags)
    alpha = np.where(mask[1:-1, 1:-1] > 0, 0, 255).astype(np.uint8)
    alpha = cv2.erode(alpha, np.ones((2, 2), np.uint8), iterations=1)
    rgb = cv2.cvtColor(bgr, cv2.COLOR_BGR2RGB)
    rgba = np.dstack([rgb, alpha])
    im = Image.fromarray(rgba)
    return im.crop(im.getbbox())


def key_dark(im, thresh=28):
    im = im.convert("RGBA")
    arr = np.array(im)
    dark = arr[:, :, :3].max(axis=2) < thresh
    arr[dark, 3] = 0
    out = Image.fromarray(arr)
    return out.crop(out.getbbox())


def paste(canvas, sprite, x, y):
    canvas.alpha_composite(sprite, (int(x), int(y)))


def shadow(canvas, sprite, x, y):
    a = sprite.split()[-1]
    sh = Image.new("RGBA", sprite.size, (0, 0, 0, 0))
    sh.putalpha(a.point(lambda p: int(p * 0.35)))
    sh = sh.filter(ImageFilter.GaussianBlur(18))
    # squash under the feet
    feet = sh.crop((0, int(sprite.height * 0.92), sprite.width, sprite.height))
    feet = feet.resize((int(sprite.width * 0.72), 36), Image.Resampling.BILINEAR)
    sx = int(x + sprite.width * 0.14)
    sy = int(y + sprite.height - 18)
    canvas.alpha_composite(feet, (sx, sy))


def build_footer():
    footer = Image.new("RGBA", (W, FOOTER_H), (255, 255, 255, 255))
    draw = ImageDraw.Draw(footer)
    draw.rectangle((0, 0, W, 6), fill=NAVY)
    logo = key_dark(Image.open(LOGO), 22)
    # split icon / wordmark on the keyed image using original y ranges is unreliable after crop
    raw = Image.open(LOGO).convert("RGBA")
    arr = np.array(raw)
    dark = arr[:, :, :3].max(axis=2) < 22
    arr[dark, 3] = 0
    full = Image.fromarray(arr)
    icon = full.crop((70, 150, 980, 590))
    word = full.crop((40, 600, 1000, 778))
    icon = icon.crop(icon.getbbox())
    word = word.crop(word.getbbox())
    icon_h = 300
    icon = icon.resize((int(icon.width * icon_h / icon.height), icon_h), Image.Resampling.LANCZOS)
    word_h = 168
    word = word.resize((int(word.width * word_h / word.height), word_h), Image.Resampling.LANCZOS)
    left_zone = int(W * 0.46)
    group_w = icon.width + 36 + word.width
    gx = max(70, (left_zone - group_w) // 2)
    gy = (FOOTER_H - icon_h) // 2 + 8
    footer.alpha_composite(icon, (gx, gy))
    wy = gy + (icon_h - word_h) // 2 + 18
    footer.alpha_composite(word, (gx + icon.width + 36, wy))
    div_x = left_zone
    draw.rectangle((div_x, 90, div_x + 8, FOOTER_H - 80), fill=NAVY)
    lines = [
        ("MEMBER", "ĐỖ VÕ ĐĂNG KHOA    ·    NGUYỄN HOÀNG THIÊN"),
        ("CLASS", "K25ISTG02"),
        ("GENRE", "TURN BASED, LIFE SIMULATION"),
    ]
    font_label = ImageFont.truetype(FONT_R, 36)
    font_value = ImageFont.truetype(FONT_B, 52)
    rx = div_x + 70
    y0 = (FOOTER_H - 3 * 110) // 2 + 10
    for i, (label, value) in enumerate(lines):
        y = y0 + i * 110
        draw.text((rx, y + 10), label, font=font_label, fill=NAVY_SOFT)
        draw.text((rx + 230, y), value, font=font_value, fill=NAVY)
    return footer


def scale_to_h(im, h):
    return im.resize((int(im.width * h / im.height), h), Image.Resampling.LANCZOS)


def main():
    art_h = H - FOOTER_H
    bg = Image.open(BG).convert("RGB").resize((W, art_h), Image.Resampling.LANCZOS)
    canvas = bg.convert("RGBA")

    charlotte = scale_to_h(key_white_sprite(CHAR + r"\Charlotte\School\charlotte_hima_uniform_menu_fullbody_v1.png"), 3180)
    coda = scale_to_h(key_white_sprite(CHAR + r"\Coda\School\coda_hima_uniform_menu_fullbody_v1.png"), 2280)
    ren = scale_to_h(key_white_sprite(CHAR + r"\Ren\School\ren_hima_uniform_menu_fullbody_v1.png"), 3520)

    floor = art_h - 30
    # back to front: Coda, Charlotte, Ren
    shadow(canvas, coda, 1180, floor - coda.height - 40)
    paste(canvas, coda, 1180, floor - coda.height - 40)
    shadow(canvas, charlotte, 40, floor - charlotte.height)
    paste(canvas, charlotte, 40, floor - charlotte.height)
    shadow(canvas, ren, W - ren.width + 40, floor - ren.height)
    paste(canvas, ren, W - ren.width + 40, floor - ren.height)

    title = key_dark(Image.open(TITLE), 18)
    # recolor white letters toward navy so they read on the sky
    tarr = np.array(title)
    ink = tarr[:, :, 3] > 20
    tarr[ink, 0] = 18
    tarr[ink, 1] = 28
    tarr[ink, 2] = 62
    title = Image.fromarray(tarr)
    tw = 1680
    title = title.resize((tw, int(title.height * tw / title.width)), Image.Resampling.LANCZOS)
    paste(canvas, title, (W - title.width) // 2, 70)

    out = Image.new("RGB", (W, H), (255, 255, 255))
    out.paste(canvas.convert("RGB"), (0, 0))
    out.paste(build_footer().convert("RGB"), (0, art_h))
    out.save(OUT, "PNG", dpi=(150, 150))
    out.resize((702, 993), Image.Resampling.LANCZOS).save(
        OUT.replace(".png", "-preview.jpg"), "JPEG", quality=90
    )
    print("ren", ren.size, "charlotte", charlotte.size, "coda", coda.size)


if __name__ == "__main__":
    main()
