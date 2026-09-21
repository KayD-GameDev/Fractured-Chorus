from __future__ import annotations

import uuid
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(r"d:\Fractured-Chorus1")
SRC = Path(r"C:\Users\Asus\.cursor\projects\d-Fractured-Chorus1\assets")
OUT = ROOT / "Assets" / "FracturedChorus" / "VFX" / "ButterflyTransition"
SPRITES = OUT / "Sprites"
PARTICLES = OUT / "Particles"
SOURCE = SPRITES / "_source"

WINGS = [
    ("bt_wing_01_closed_v2.png", "fc_bt_wing_01_closed.png"),
    ("bt_wing_02_quarter.png", "fc_bt_wing_02_quarter.png"),
    ("bt_wing_03_half.png", "fc_bt_wing_03_half.png"),
    ("bt_wing_04_open_v2.png", "fc_bt_wing_04_open.png"),
]

CYAN = (0, 212, 255, 255)
CYAN_SOFT = (140, 217, 255, 255)
WHITE = (234, 251, 255, 255)
LAVENDER = (214, 196, 255, 180)


def new_guid() -> str:
    return uuid.uuid4().hex


def write_sprite_meta(path: Path) -> None:
    guid = new_guid()
    sprite_id = new_guid()
    Path(str(path) + ".meta").write_text(
        f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: {sprite_id}
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""",
        encoding="utf-8",
    )


def magenta_score(rgb: np.ndarray) -> np.ndarray:
    r = rgb[:, :, 0].astype(np.float32)
    g = rgb[:, :, 1].astype(np.float32)
    b = rgb[:, :, 2].astype(np.float32)
    chroma = (r + b) * 0.5 - g
    hot = np.minimum(r, b)
    return (chroma * 1.15 + hot * 0.35) / 255.0


def flood_background(rgb: np.ndarray, score: np.ndarray, threshold: float = 0.42) -> np.ndarray:
    h, w = score.shape
    bg = score >= threshold
    visited = np.zeros((h, w), dtype=bool)
    q: deque[tuple[int, int]] = deque()
    for x in range(w):
        for y in (0, h - 1):
            if bg[y, x] and not visited[y, x]:
                visited[y, x] = True
                q.append((x, y))
    for y in range(h):
        for x in (0, w - 1):
            if bg[y, x] and not visited[y, x]:
                visited[y, x] = True
                q.append((x, y))
    while q:
        x, y = q.popleft()
        for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1), (-1, -1), (1, -1), (-1, 1), (1, 1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < w and 0 <= ny < h and not visited[ny, nx] and bg[ny, nx]:
                visited[ny, nx] = True
                q.append((nx, ny))
    return visited


def key_butterfly(image: Image.Image) -> Image.Image:
    rgba = np.array(image.convert("RGBA"))
    rgb = rgba[:, :, :3]
    score = magenta_score(rgb)
    bg = flood_background(rgb, score)
    keep = (~bg).astype(np.float32)
    fringe = np.clip((0.62 - score) / 0.22, 0.0, 1.0)
    alpha = np.clip(keep * 255.0 * (0.35 + 0.65 * fringe), 0, 255).astype(np.uint8)
    alpha_img = Image.fromarray(alpha, "L").filter(ImageFilter.GaussianBlur(0.8))
    alpha = np.array(alpha_img)
    alpha[keep < 0.5] = np.minimum(alpha[keep < 0.5], (fringe[keep < 0.5] * 40).astype(np.uint8))

    out = rgba.copy()
    r = out[:, :, 0].astype(np.float32)
    g = out[:, :, 1].astype(np.float32)
    b = out[:, :, 2].astype(np.float32)
    mag = np.clip(((r + b) * 0.5 - g) / 255.0, 0.0, 1.0)
    pull = mag * (alpha.astype(np.float32) / 255.0)
    r = r - pull * np.maximum(r - g, 0) * 0.72
    b = b * (1.0 - pull * 0.08) + 18.0 * pull
    g = g + pull * 12.0
    out[:, :, 0] = np.clip(r, 0, 255).astype(np.uint8)
    out[:, :, 1] = np.clip(g, 0, 255).astype(np.uint8)
    out[:, :, 2] = np.clip(b, 0, 255).astype(np.uint8)
    out[:, :, 3] = alpha
    return Image.fromarray(out, "RGBA")


def crop_alpha(image: Image.Image, pad: int = 18) -> Image.Image:
    arr = np.array(image)
    ys, xs = np.where(arr[:, :, 3] > 10)
    if xs.size == 0:
        return image
    left = max(0, int(xs.min()) - pad)
    right = min(arr.shape[1], int(xs.max()) + 1 + pad)
    top = max(0, int(ys.min()) - pad)
    bottom = min(arr.shape[0], int(ys.max()) + 1 + pad)
    return Image.fromarray(arr[top:bottom, left:right], "RGBA")


def save_png(image: Image.Image, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path, "PNG")
    write_sprite_meta(path)
    print(f"wrote {path} {image.size}")


def radial_glow(size: int, inner: tuple[int, int, int, int], outer: tuple[int, int, int, int]) -> Image.Image:
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    cx = cy = size * 0.5
    radius = size * 0.48
    px = img.load()
    for y in range(size):
        for x in range(size):
            d = ((x - cx) ** 2 + (y - cy) ** 2) ** 0.5 / radius
            if d >= 1.0:
                continue
            t = d * d
            a = int(inner[3] * (1.0 - t) ** 2)
            r = int(inner[0] * (1.0 - t) + outer[0] * t)
            g = int(inner[1] * (1.0 - t) + outer[1] * t)
            b = int(inner[2] * (1.0 - t) + outer[2] * t)
            px[x, y] = (r, g, b, a)
    return img.filter(ImageFilter.GaussianBlur(2.2))


def star_dust(size: int = 64) -> Image.Image:
    return radial_glow(size, (234, 251, 255, 255), (0, 212, 255, 0)).filter(ImageFilter.GaussianBlur(0.6))


def glitter_cluster(size: int = 256) -> Image.Image:
    rng = np.random.default_rng(17)
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    for _ in range(42):
        x = float(rng.uniform(16, size - 16))
        y = float(rng.uniform(16, size - 16))
        r = float(rng.uniform(0.8, 2.6))
        col = CYAN_SOFT if rng.random() > 0.35 else WHITE
        a = int(rng.uniform(90, 210))
        draw.ellipse((x - r, y - r, x + r, y + r), fill=(col[0], col[1], col[2], a))
    return img.filter(ImageFilter.GaussianBlur(0.4))


def polygon(size: int, points: list[tuple[float, float]], fill) -> Image.Image:
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    scaled = [(x * size, y * size) for x, y in points]
    draw.polygon(scaled, fill=fill)
    overlay = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    od = ImageDraw.Draw(overlay)
    od.polygon(scaled, outline=(234, 251, 255, 140))
    img = Image.alpha_composite(img, overlay)
    return img.filter(ImageFilter.GaussianBlur(0.35))


def waveform_line(width: int = 512, height: int = 128) -> Image.Image:
    img = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    pts = []
    for i in range(width):
        t = i / max(1, width - 1)
        y = height * 0.5 + np.sin(t * np.pi * 3.2) * height * 0.18 + np.sin(t * np.pi * 9.0) * height * 0.05
        pts.append((i, y))
    draw.line(pts, fill=(140, 217, 255, 210), width=2)
    return img.filter(ImageFilter.GaussianBlur(0.5))


def freq_line(width: int = 256, height: int = 64) -> Image.Image:
    img = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    pts = []
    for i in range(width):
        t = i / max(1, width - 1)
        y = height * 0.55 + np.sin(t * np.pi * 2.1 + 0.4) * height * 0.22
        pts.append((i, y))
    draw.line(pts, fill=(234, 251, 255, 160), width=1)
    return img.filter(ImageFilter.GaussianBlur(0.4))


def music_note(size: int = 128) -> Image.Image:
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    s = size
    head = (s * 0.22, s * 0.58, s * 0.58, s * 0.86)
    draw.ellipse(head, fill=(140, 217, 255, 200))
    stem_x = s * 0.54
    draw.rectangle((stem_x, s * 0.16, stem_x + s * 0.045, s * 0.72), fill=(234, 251, 255, 210))
    flag = [(stem_x + s * 0.04, s * 0.16), (s * 0.82, s * 0.28), (s * 0.78, s * 0.40), (stem_x + s * 0.04, s * 0.30)]
    draw.polygon(flag, fill=(0, 212, 255, 170))
    return img.filter(ImageFilter.GaussianBlur(0.45))


def process_wings() -> None:
    SOURCE.mkdir(parents=True, exist_ok=True)
    for src_name, dst_name in WINGS:
        src = SRC / src_name
        raw = Image.open(src)
        raw.save(SOURCE / src_name)
        keyed = key_butterfly(raw)
        cropped = crop_alpha(keyed, pad=22)
        save_png(cropped, SPRITES / dst_name)


def process_particles() -> None:
    PARTICLES.mkdir(parents=True, exist_ok=True)
    assets = {
        "fc_bt_trail_star_dust.png": star_dust(64),
        "fc_bt_trail_glitter.png": glitter_cluster(256),
        "fc_bt_trail_triangle_lg.png": polygon(
            128,
            [(0.50, 0.10), (0.90, 0.86), (0.10, 0.86)],
            (0, 212, 255, 120),
        ),
        "fc_bt_trail_triangle_sm.png": polygon(
            96,
            [(0.50, 0.14), (0.86, 0.84), (0.14, 0.84)],
            (140, 217, 255, 110),
        ),
        "fc_bt_trail_diamond.png": polygon(
            96,
            [(0.50, 0.08), (0.88, 0.50), (0.50, 0.92), (0.12, 0.50)],
            (214, 196, 255, 90),
        ),
        "fc_bt_trail_shard.png": polygon(
            96,
            [(0.62, 0.08), (0.78, 0.18), (0.38, 0.92), (0.18, 0.80)],
            (140, 217, 255, 100),
        ),
        "fc_bt_trail_waveform.png": waveform_line(),
        "fc_bt_trail_freq.png": freq_line(),
        "fc_bt_trail_note.png": music_note(),
        "fc_bt_glow_bloom.png": radial_glow(256, (234, 251, 255, 150), (0, 212, 255, 0)),
        "fc_bt_glow_rim.png": radial_glow(256, (140, 217, 255, 70), (0, 212, 255, 0)),
    }
    for name, image in assets.items():
        save_png(image, PARTICLES / name)


def main() -> None:
    SPRITES.mkdir(parents=True, exist_ok=True)
    process_wings()
    process_particles()
    (OUT / "Animations").mkdir(parents=True, exist_ok=True)
    (OUT / "Prefabs").mkdir(parents=True, exist_ok=True)
    (OUT / "Materials").mkdir(parents=True, exist_ok=True)
    print("butterfly transition art ready")


if __name__ == "__main__":
    main()
