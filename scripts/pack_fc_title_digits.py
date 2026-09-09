"""Pack FRACTURED CHORUS title-style digits 0-9 with exact 5px gutters."""
from __future__ import annotations

import uuid
from pathlib import Path

import numpy as np
from PIL import Image

RAW_04 = Path(
    r"C:\Users\admin\.cursor\projects\f-Unity-Project-Fractured-Chorus"
    r"\assets\fc_title_digits_raw_0-4_v1.png"
)
RAW_59 = Path(
    r"C:\Users\admin\.cursor\projects\f-Unity-Project-Fractured-Chorus"
    r"\assets\fc_title_digits_raw_5-9_v1.png"
)
OUT_DIR = Path(
    r"F:\Unity_Project\Fractured Chorus\Assets\FracturedChorus\Art\UI\TitleScreen\Digits"
)
SHEET_NAME = "fc_title_digits_0-9_v1.png"
GUTTER = 5
PAD = 4
TARGET_H = 200
COL_THR = 3


def is_fg(arr: np.ndarray) -> np.ndarray:
    rgb = arr[:, :, :3].astype(np.int16)
    mn = rgb.min(axis=2)
    mx = rgb.max(axis=2)
    near_white = (mn >= 242) & ((mx - mn) <= 18)
    return ~near_white


def to_rgba(im: Image.Image) -> Image.Image:
    rgba = im.convert("RGBA")
    arr = np.array(rgba)
    fg = is_fg(arr)
    arr[..., 3] = np.where(fg, 255, 0)
    # Soften remaining near-white inside glyphs a little so keying doesn't leave halos
    rgb = arr[:, :, :3].astype(np.int16)
    halo = fg & (rgb.min(axis=2) >= 230)
    arr[..., 3] = np.where(halo, 0, arr[..., 3])
    return Image.fromarray(arr, "RGBA")


def column_runs(fg: np.ndarray, expected: int) -> list[tuple[int, int]]:
    col = fg.sum(axis=0)
    runs: list[tuple[int, int]] = []
    in_run = False
    start = 0
    for x, count in enumerate(col):
        if count > COL_THR:
            if not in_run:
                in_run = True
                start = x
        elif in_run:
            in_run = False
            runs.append((start, x - 1))
    if in_run:
        runs.append((start, fg.shape[1] - 1))
    if len(runs) != expected:
        raise SystemExit(f"Expected {expected} digit runs, got {len(runs)}: {runs}")
    return runs


def extract_digits(path: Path, expected: int) -> list[Image.Image]:
    src = to_rgba(Image.open(path))
    arr = np.array(src)
    fg = arr[..., 3] > 0
    runs = column_runs(fg, expected)
    row = fg.sum(axis=1)
    ys = np.where(row > COL_THR)[0]
    y0, y1 = int(ys[0]), int(ys[-1])
    h, w = arr.shape[:2]
    digits: list[Image.Image] = []
    for x0, x1 in runs:
        xa = max(0, x0 - PAD)
        xb = min(w - 1, x1 + PAD)
        ya = max(0, y0 - PAD)
        yb = min(h - 1, y1 + PAD)
        crop = src.crop((xa, ya, xb + 1, yb + 1))
        digits.append(tight_crop(crop))
    return digits


def tight_crop(im: Image.Image) -> Image.Image:
    arr = np.array(im)
    fg = arr[..., 3] > 0
    ys, xs = np.where(fg)
    if len(xs) == 0:
        return im
    x0, x1 = int(xs.min()), int(xs.max())
    y0, y1 = int(ys.min()), int(ys.max())
    x0 = max(0, x0 - 1)
    y0 = max(0, y0 - 1)
    x1 = min(im.width - 1, x1 + 1)
    y1 = min(im.height - 1, y1 + 1)
    return im.crop((x0, y0, x1 + 1, y1 + 1))


def scale_to_height(im: Image.Image, height: int) -> Image.Image:
    if im.height == height:
        return im
    w = max(1, round(im.width * (height / im.height)))
    return im.resize((w, height), Image.Resampling.LANCZOS)


def pack(digits: list[Image.Image]) -> tuple[Image.Image, list[tuple[int, int, int, int]]]:
    scaled = [scale_to_height(d, TARGET_H) for d in digits]
    width = sum(d.width for d in scaled) + GUTTER * (len(scaled) - 1)
    sheet = Image.new("RGBA", (width, TARGET_H), (0, 0, 0, 0))
    rects: list[tuple[int, int, int, int]] = []
    x = 0
    for i, digit in enumerate(scaled):
        sheet.paste(digit, (x, 0), digit)
        rects.append((x, 0, digit.width, TARGET_H))
        x += digit.width
        if i < len(scaled) - 1:
            x += GUTTER
    return sheet, rects


def sprite_meta(path: Path, rects: list[tuple[int, int, int, int]], guid: str) -> str:
    base = path.stem
    ids = [2300000001 + i for i in range(10)]
    name_table = "\n".join(
        f"  - first:\n      213: {ids[i]}\n    second: {base}_{i}" for i in range(10)
    )
    sprites = []
    for i, (x, y, w, h) in enumerate(rects):
        sprites.append(
            f"""    - serializedVersion: 2
      name: {base}_{i}
      rect:
        serializedVersion: 2
        x: {x}
        y: {y}
        width: {w}
        height: {h}
      alignment: 0
      pivot: {{x: 0.5, y: 0.5}}
      border: {{x: 0, y: 0, z: 0, w: 0}}
      customData: 
      outline: []
      physicsShape: []
      tessellationDetail: -1
      bones: []
      spriteID: {i+1:032x}
      internalID: {ids[i]}
      vertices: []
      indices: 
      edges: []
      weights: []"""
        )
    id_table = "\n".join(f"      {base}_{i}: {ids[i]}" for i in range(10))
    return f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable:
{name_table}
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
  isReadable: 1
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
  spriteMode: 2
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
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
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites:
{chr(10).join(sprites)}
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable:
{id_table}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def single_sprite_meta(guid: str) -> str:
    return f"""fileFormatVersion: 2
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
  spriteGenerateFallbackPhysicsShape: 1
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
    textureCompression: 0
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
    spriteID: 
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
"""


def folder_meta(guid: str) -> str:
    return f"""fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def verify_gutters(sheet: Image.Image, rects: list[tuple[int, int, int, int]]) -> None:
    arr = np.array(sheet)
    for i in range(len(rects) - 1):
        x0 = rects[i][0] + rects[i][2]
        x1 = rects[i + 1][0]
        gap = x1 - x0
        if gap != GUTTER:
            raise SystemExit(f"Gutter after digit {i} is {gap}px, expected {GUTTER}")
        strip = arr[:, x0:x1, 3]
        if int(strip.max()) != 0:
            raise SystemExit(f"Gutter after digit {i} is not fully transparent")


def main() -> None:
    digits = extract_digits(RAW_04, 5) + extract_digits(RAW_59, 5)
    if len(digits) != 10:
        raise SystemExit(f"Need 10 digits, got {len(digits)}")
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    sheet, rects = pack(digits)
    if sheet.width > 2048:
        raise SystemExit(f"Sheet too wide: {sheet.size}")
    verify_gutters(sheet, rects)
    sheet_path = OUT_DIR / SHEET_NAME
    sheet.save(sheet_path)
    (OUT_DIR / "CUTS.txt").write_text(
        "\n".join(
            f"{i}: x={r[0]} w={r[2]} h={r[3]} gutter_after={0 if i == 9 else GUTTER}"
            for i, r in enumerate(rects)
        )
        + f"\nsheet={sheet.width}x{sheet.height}\n",
        encoding="utf-8",
    )
    (OUT_DIR / (SHEET_NAME + ".meta")).write_text(
        sprite_meta(sheet_path, rects, uuid.uuid4().hex), encoding="utf-8"
    )
    (OUT_DIR / "Digits.meta" if False else None)
    folder_meta_path = OUT_DIR.with_suffix("") 
    # folder meta sits beside the folder
    (OUT_DIR.parent / "Digits.meta").write_text(
        folder_meta(uuid.uuid4().hex), encoding="utf-8"
    )
    for i, (x, y, w, h) in enumerate(rects):
        frame = sheet.crop((x, y, x + w, y + h))
        frame_path = OUT_DIR / f"fc_title_digit_{i}.png"
        frame.save(frame_path)
        frame_path.with_suffix(".png.meta").write_text(
            single_sprite_meta(uuid.uuid4().hex), encoding="utf-8"
        )
    print("saved", sheet_path, sheet.size)
    print("rects", rects)


if __name__ == "__main__":
    main()
