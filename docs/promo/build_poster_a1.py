"""A1 poster: key art + DayDreamers footer."""
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import cv2
import numpy as np

ART = r"C:\Users\admin\.cursor\projects\f-Unity-Project-Fractured-Chorus\assets\fractured_chorus_poster_pose_lock.png"
LOGO = r"C:\Users\admin\.cursor\projects\f-Unity-Project-Fractured-Chorus\assets\c__Users_admin_AppData_Roaming_Cursor_User_workspaceStorage_8f918c66735690560589d82aa54e5d70_images_image-63a22129-08cf-4f1d-a6e3-741c39d7f075.png"
OUT = r"F:\Unity_Project\Fractured Chorus\docs\promo\Fractured-Chorus-Poster-A1.png"

# A1 @ 150 dpi
W, H = 3508, 4967
FOOTER_H = 560
NAVY = (26, 42, 92, 255)
NAVY_SOFT = (70, 88, 140, 255)

FONT_B = r"C:\Windows\Fonts\segoeuib.ttf"
FONT_R = r"C:\Windows\Fonts\segoeui.ttf"


def key_black(im, thresh=22):
    im = im.convert("RGBA")
    arr = np.array(im)
    rgb = arr[:, :, :3].astype(np.int16)
    dark = (rgb.max(axis=2) < thresh)
    arr[dark, 3] = 0
    return Image.fromarray(arr)


def inpaint_studio_label(art):
    """Remove the floating 'Day Dreamers Studio' line on the plaza."""
    rgb = np.array(art.convert("RGB"))
    h, w = rgb.shape[:2]
    mask = np.zeros((h, w), np.uint8)
    # Tight band where the studio credit sits, above the white footer.
    mask[948:1036, 500:790] = 255
    # Keep only light lettering so the boy's coat edge is not wiped.
    region = rgb[948:1036, 500:790]
    bright = (region[:, :, 0] > 185) & (region[:, :, 1] > 185) & (region[:, :, 2] > 175)
    mask[948:1036, 500:790] = np.where(bright, 255, 0).astype(np.uint8)
    kernel = np.ones((3, 3), np.uint8)
    mask = cv2.dilate(mask, kernel, iterations=2)
    filled = cv2.inpaint(rgb, mask, 5, cv2.INPAINT_TELEA)
    return Image.fromarray(filled)


def crop_content(art):
    """Drop the generated white credit band."""
    rgb = np.array(art.convert("RGB"))
    h, w = rgb.shape[:2]
    cut = h - 1
    for y in range(h - 1, int(h * 0.7), -1):
        row = rgb[y, ::4]
        white = np.mean((row[:, 0] > 245) & (row[:, 1] > 245) & (row[:, 2] > 245))
        if white < 0.85:
            cut = y
            break
    return art.crop((0, 0, w, cut + 1))


def main():
    art = Image.open(ART)
    art = crop_content(art)

    art_area_h = H - FOOTER_H
    scale = W / art.width
    scaled_h = int(round(art.height * scale))
    art_s = art.resize((W, scaled_h), Image.Resampling.LANCZOS)

    canvas = Image.new("RGB", (W, H), (255, 255, 255))
    if scaled_h >= art_area_h:
        top = scaled_h - art_area_h
        canvas.paste(art_s.crop((0, top, W, scaled_h)), (0, 0))
    else:
        gap = art_area_h - scaled_h
        sky = art_s.crop((0, 0, W, 8)).resize((W, gap), Image.Resampling.BILINEAR)
        canvas.paste(sky, (0, 0))
        canvas.paste(art_s, (0, gap))

    footer = Image.new("RGBA", (W, FOOTER_H), (255, 255, 255, 255))
    draw = ImageDraw.Draw(footer)
    draw.rectangle((0, 0, W, 6), fill=NAVY)

    logo = key_black(Image.open(LOGO))
    # Icon sits above the wordmark; split on the empty row found at y=594.
    icon = logo.crop((70, 150, 980, 590))
    word = logo.crop((40, 600, 1000, 778))
    icon = icon.crop(icon.getbbox())
    word = word.crop(word.getbbox())

    icon_h = 300
    icon = icon.resize((int(icon.width * icon_h / icon.height), icon_h), Image.Resampling.LANCZOS)
    word_h = 168
    word = word.resize((int(word.width * word_h / word.height), word_h), Image.Resampling.LANCZOS)

    group_w = icon.width + 36 + word.width
    # Left cluster occupies the left of the bar; divider near 46%.
    left_zone = int(W * 0.46)
    gx = max(70, (left_zone - group_w) // 2)
    gy = (FOOTER_H - icon_h) // 2 + 8
    footer.alpha_composite(icon, (gx, gy))
    wy = gy + (icon_h - word_h) // 2 + 18
    footer.alpha_composite(word, (gx + icon.width + 36, wy))

    div_x = left_zone
    draw.rectangle((div_x, 90, div_x + 8, FOOTER_H - 80), fill=NAVY)

    # Right: member names / class / genre
    lines = [
        ("MEMBER", "ĐỖ VÕ ĐĂNG KHOA    ·    NGUYỄN HOÀNG THIÊN"),
        ("CLASS", "K25ISTG02"),
        ("GENRE", "TURN BASED, LIFE SIMULATION"),
    ]
    font_label = ImageFont.truetype(FONT_R, 36)
    font_value = ImageFont.truetype(FONT_B, 52)
    rx = div_x + 70
    block_h = 3 * 110
    y0 = (FOOTER_H - block_h) // 2 + 10
    for i, (label, value) in enumerate(lines):
        y = y0 + i * 110
        draw.text((rx, y + 10), label, font=font_label, fill=NAVY_SOFT)
        # Values share one column so the three facts line up.
        draw.text((rx + 230, y), value, font=font_value, fill=NAVY)

    canvas.paste(footer.convert("RGB"), (0, art_area_h))
    canvas.save(OUT, "PNG", dpi=(150, 150))
    # Small preview for QA
    preview = canvas.resize((702, 993), Image.Resampling.LANCZOS)
    preview.save(OUT.replace(".png", "-preview.jpg"), "JPEG", quality=90)
    print("saved", OUT, canvas.size)


if __name__ == "__main__":
    main()
